using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IcMed.IntegrationDemo.Infrastructure.Options;

namespace IcMed.IntegrationDemo.Infrastructure.Auth;

/// <summary>
/// Retrieves OAuth2 access tokens from the icMED identity server and caches
/// them in memory until just before expiry. The service supports both
/// client credentials and password grants based on configured options.
/// </summary>
internal sealed class TokenService : ITokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IcMedOptions _options;
    private readonly ILogger<TokenService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates a new <see cref="TokenService"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create the identity HTTP client.</param>
    /// <param name="cache">In-memory cache used to store the bearer token.</param>
    /// <param name="options">Strongly typed integration options.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public TokenService(IHttpClientFactory httpClientFactory, IMemoryCache cache, IOptions<IcMedOptions> options, ILogger<TokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthenticationHeaderValue> GetBearerAsync(CancellationToken ct)
    {
        var cacheKey = $"icmed_token::{_options.Scope}";
        if (_cache.TryGetValue<AuthenticationHeaderValue>(cacheKey, out var auth))
        {
            _logger.LogDebug("Token cache HIT for scope {Scope}", _options.Scope);
            return auth!;
        }

        _logger.LogInformation("Token cache MISS for scope {Scope}. Requesting new token...", _options.Scope);
        var started = DateTimeOffset.UtcNow;
        var token = await RequestTokenAsync(ct).ConfigureAwait(false);
        var elapsedMs = (DateTimeOffset.UtcNow - started).TotalMilliseconds;
        var expiresIn = TimeSpan.FromSeconds(Math.Max(60, token.expires_in - _options.TokenSkewSeconds));

        auth = new AuthenticationHeaderValue("Bearer", token.access_token);
        _cache.Set(cacheKey, auth, expiresIn);
        _logger.LogInformation("Token acquired in {Elapsed} ms. Caching for {CacheSeconds} seconds (skew {Skew}s)",
            elapsedMs, (int)expiresIn.TotalSeconds, _options.TokenSkewSeconds);
        return auth;
    }

    /// <summary>
    /// Performs the HTTP request to obtain a new access token from the identity server.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The access token and the expiry in seconds.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the token endpoint returns a non-success status or token is missing.</exception>
    private async Task<(string access_token, int expires_in)> RequestTokenAsync(CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient("IcMed.Identity");
        var form = new List<KeyValuePair<string, string>>
        {
            new("client_id", _options.ClientId),
            new("client_secret", _options.ClientSecret),
            new("scope", _options.Scope)
        };

        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            form.Add(new("grant_type", "password"));
            form.Add(new("username", _options.Username));
            form.Add(new("password", _options.Password));
        }
        else
        {
            // Fallback to client_credentials if username/password not configured
            form.Add(new("grant_type", "client_credentials"));
        }

        using var content = new FormUrlEncodedContent(form);

        var tokenEndpoint = new Uri(new Uri(_options.IdBaseUrl.TrimEnd('/') + "/"), "connect/token");
        using var req = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = content
        };
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
            var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Missing access_token");
            var expiresIn = root.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;
            return (accessToken, expiresIn);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to parse token response: {Body}", Truncate(body));
            throw;
        }
    }

    private static string Truncate(string? input, int max = 2048)
        => string.IsNullOrEmpty(input) ? string.Empty : (input.Length <= max ? input : input.Substring(0, max));
}
