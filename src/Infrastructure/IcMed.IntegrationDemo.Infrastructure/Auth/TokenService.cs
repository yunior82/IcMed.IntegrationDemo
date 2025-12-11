using System.Net.Http.Headers;
using System.Text.Json;
using IcMed.IntegrationDemo.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IcMed.IntegrationDemo.Infrastructure.Auth;

/// <summary>
/// Retrieves OAuth2 access tokens from the icMED identity server and caches
/// them in memory until just before expiry. The service supports both
/// client credentials and password grants based on configured options.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IcMedOptions _options;
    private readonly ILogger<TokenService> _logger;
    private readonly IRequestCredentialsAccessor? _requestCredentialsAccessor;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates a new <see cref="TokenService"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create the identity HTTP client.</param>
    /// <param name="cache">In-memory cache used to store the bearer token.</param>
    /// <param name="options">Strongly typed integration options.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public TokenService(IHttpClientFactory httpClientFactory, IMemoryCache cache, IOptions<IcMedOptions> options, ILogger<TokenService> logger, IRequestCredentialsAccessor? requestCredentialsAccessor = null)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _requestCredentialsAccessor = requestCredentialsAccessor;
    }

    /// <inheritdoc />
    public async Task<AuthenticationHeaderValue> GetBearerAsync(CancellationToken ct)
    {
        // 1) If the SPA already supplied a Bearer token in the inbound request, just forward it.
        var inboundBearer = _requestCredentialsAccessor?.GetIncomingBearerToken();
        if (!string.IsNullOrWhiteSpace(inboundBearer))
        {
            _logger.LogDebug("Using inbound bearer token from request headers");
            return new AuthenticationHeaderValue("Bearer", inboundBearer);
        }

        // 2) If username/password are provided per request, exchange them without caching.
        var (reqUser, reqPass) = _requestCredentialsAccessor?.GetUsernamePassword() ?? (null, null);
        if (!string.IsNullOrWhiteSpace(reqUser) && !string.IsNullOrWhiteSpace(reqPass))
        {
            _logger.LogInformation("Per-request credentials provided. Exchanging for token (no cache)...");
            var tokenPw = await ExchangePasswordAsync(reqUser!, reqPass!, ct).ConfigureAwait(false);
            return new AuthenticationHeaderValue("Bearer", tokenPw.access_token);
        }

        var cacheKey = $"icmed_token::{_options.Scope}";
        if (_cache.TryGetValue<AuthenticationHeaderValue>(cacheKey, out var auth))
        {
            _logger.LogDebug("Token cache HIT for scope {Scope}", _options.Scope);
            return auth!;
        }

        _logger.LogInformation("Token cache MISS for scope {Scope}. Requesting new token...", _options.Scope);
        var started = DateTimeOffset.UtcNow;
        var tokenResp = await RequestTokenAsync(null, null, ct).ConfigureAwait(false);
        var elapsedMs = (DateTimeOffset.UtcNow - started).TotalMilliseconds;
        var expiresIn = TimeSpan.FromSeconds(Math.Max(60, tokenResp.expires_in - _options.TokenSkewSeconds));

        auth = new AuthenticationHeaderValue("Bearer", tokenResp.access_token);
        _cache.Set(cacheKey, auth, expiresIn);
        _logger.LogInformation("Token acquired in {Elapsed} ms. Caching for {CacheSeconds} seconds (skew {Skew}s)",
            elapsedMs, (int)expiresIn.TotalSeconds, _options.TokenSkewSeconds);
        return auth;
    }

    /// <summary>
    /// Performs the HTTP request to get a new access token from the identity server.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The access token and the expiry in seconds.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the token endpoint returns a non-success status or token is missing.</exception>
    private async Task<(string access_token, int expires_in)> RequestTokenAsync(string? username, string? password, CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient("IcMed.Identity");
        var form = new List<KeyValuePair<string, string>>
        {
            new("client_id", _options.ClientId),
            new("client_secret", _options.ClientSecret),
            new("scope", _options.Scope)
        };

        // Prefer explicit parameters (per-request or from caller), fallback to options
        var u = username ?? _options.Username;
        var p = password ?? _options.Password;
        if (!string.IsNullOrWhiteSpace(u) && !string.IsNullOrWhiteSpace(p))
        {
            form.Add(new KeyValuePair<string, string>("grant_type", "password"));
            form.Add(new KeyValuePair<string, string>("username", u));
            form.Add(new KeyValuePair<string, string>("password", p));
        }
        else
        {
            // Fallback to client_credentials if username/password not configured
            form.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
        }

        using var content = new FormUrlEncodedContent(form);

        var tokenEndpoint = new Uri(new Uri(_options.IdBaseUrl.TrimEnd('/') + "/"), "connect/token");
        using var req = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        req.Content = content;
        req.Headers.Accept.ParseAdd("application/json");

        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Token request failed: {StatusCode} {Body}", (int)resp.StatusCode, Truncate(body));
            throw new InvalidOperationException($"Failed to obtain access token. HTTP {(int)resp.StatusCode}");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("access_token", out var atEl) || string.IsNullOrWhiteSpace(atEl.GetString()))
            {
                throw new InvalidOperationException("Missing access_token");
            }
            var accessToken = atEl.GetString()!;
            var expiresIn = root.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;
            return (accessToken, expiresIn);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to parse token response: {Body}", Truncate(body));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(string access_token, int expires_in)> ExchangePasswordAsync(string username, string password, CancellationToken ct)
    {
        return await RequestTokenAsync(username, password, ct).ConfigureAwait(false);
    }

    private static string Truncate(string? input, int max = 2048)
        => string.IsNullOrEmpty(input) ? string.Empty : (input.Length <= max ? input : input[..max]);
}
