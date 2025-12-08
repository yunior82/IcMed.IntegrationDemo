using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Domain.Entities;
using IcMed.IntegrationDemo.Infrastructure.Auth;
using IcMed.IntegrationDemo.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IcMed.IntegrationDemo.Infrastructure.Clients;

/// <summary>
/// Live implementation of <see cref="IIcMedClient"/> that calls the real icMED API
/// using <see cref="IHttpClientFactory"/> and a cached bearer token from <see cref="ITokenService"/>.
/// </summary>
internal sealed class IcMedHttpClient : IIcMedClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenService _tokenService;
    private readonly IcMedOptions _options;
    private readonly ILogger<IcMedHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates a new <see cref="IcMedHttpClient"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to resolve named HTTP clients.</param>
    /// <param name="tokenService">Service that provides the bearer token.</param>
    /// <param name="options">Strongly typed integration options.</param>
    /// <param name="logger">Logger.</param>
    public IcMedHttpClient(IHttpClientFactory httpClientFactory, ITokenService tokenService, IOptions<IcMedOptions> options, ILogger<IcMedHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates an authorized HTTP client for the icMED API.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Configured <see cref="HttpClient"/> with bearer token.</returns>
    private async Task<HttpClient> CreateApiClientAsync(CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient("IcMed.Api");
        http.DefaultRequestHeaders.Authorization = await _tokenService.GetBearerAsync(ct).ConfigureAwait(false);
        return http;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Workplace>> GetWorkplacesAsync(CancellationToken ct)
    {
        var http = await CreateApiClientAsync(ct);
        var url = new Uri(new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/"), "api/Workplaces");
        _logger.LogInformation("Calling icMED GET {Url}", url);
        using var resp = await http.GetAsync(url, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("icMED GET {Url} failed: {Status} {Body}", url, (int)resp.StatusCode, Truncate(body));
            throw new HttpRequestException($"Upstream icMED returned {(int)resp.StatusCode}", null, resp.StatusCode);
        }
        var items = JsonSerializer.Deserialize<List<Workplace>>(body, JsonOptions) ?? new List<Workplace>();
        _logger.LogInformation("icMED GET {Url} returned {Count} workplaces", url, items.Count);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Speciality>> GetSpecialitiesAsync(long workplaceId, CancellationToken ct)
    {
        var http = await CreateApiClientAsync(ct);
        var url = new Uri(new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/"), $"api/specialities?workplaceId={workplaceId}");
        _logger.LogInformation("Calling icMED GET {Url}", url);
        using var resp = await http.GetAsync(url, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("icMED GET {Url} failed: {Status} {Body}", url, (int)resp.StatusCode, Truncate(body));
            throw new HttpRequestException($"Upstream icMED returned {(int)resp.StatusCode}", null, resp.StatusCode);
        }
        var items = JsonSerializer.Deserialize<List<Speciality>>(body, JsonOptions) ?? new List<Speciality>();
        _logger.LogInformation("icMED GET {Url} returned {Count} specialities", url, items.Count);
        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Physician>> GetPhysiciansAsync(long workplaceId, long specialityId, CancellationToken ct)
    {
        var http = await CreateApiClientAsync(ct);
        var url = new Uri(new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/"), $"api/physicians?workplaceId={workplaceId}&specialityId={specialityId}");
        _logger.LogInformation("Calling icMED GET {Url}", url);
        using var resp = await http.GetAsync(url, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("icMED GET {Url} failed: {Status} {Body}", url, (int)resp.StatusCode, Truncate(body));
            throw new HttpRequestException($"Upstream icMED returned {(int)resp.StatusCode}", null, resp.StatusCode);
        }
        var items = JsonSerializer.Deserialize<List<Physician>>(body, JsonOptions) ?? new List<Physician>();
        _logger.LogInformation("icMED GET {Url} returned {Count} physicians", url, items.Count);
        return items;
    }

    /// <inheritdoc />
    public async Task<Schedule> GetScheduleAsync(long physicianId, long subOfficeId, long referenceDate, string currentView, CancellationToken ct)
    {
        var http = await CreateApiClientAsync(ct);
        var url = new Uri(new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/"), $"api/physicians/{physicianId}/schedule/{referenceDate}?subOfficeId={subOfficeId}&currentView={currentView}");
        _logger.LogInformation("Calling icMED GET {Url}", url);
        using var resp = await http.GetAsync(url, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("icMED GET {Url} failed: {Status} {Body}", url, (int)resp.StatusCode, Truncate(body));
            throw new HttpRequestException($"Upstream icMED returned {(int)resp.StatusCode}", null, resp.StatusCode);
        }
        var schedule = JsonSerializer.Deserialize<Schedule>(body, JsonOptions) ?? new Schedule(0, Array.Empty<Interval>(), Array.Empty<Interval>());
        _logger.LogInformation("icMED GET {Url} returned schedule with slotSpan {Slot}", url, schedule.SlotSpan);
        return schedule;
    }

    /// <inheritdoc />
    public async Task<AppointmentResponse> CreateAppointmentAsync(AppointmentRequest request, CancellationToken ct)
    {
        var http = await CreateApiClientAsync(ct);
        var url = new Uri(new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/"), "api/AppointmentsA");
        _logger.LogInformation("Calling icMED POST {Url}", url);
        using var resp = await http.PostAsJsonAsync(url, request, JsonOptions, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("icMED POST {Url} failed: {Status} {Body}", url, (int)resp.StatusCode, Truncate(body));
            throw new HttpRequestException($"Upstream icMED returned {(int)resp.StatusCode}", null, resp.StatusCode);
        }
        var response = JsonSerializer.Deserialize<AppointmentResponse>(body, JsonOptions);
        if (response is null)
        {
            _logger.LogError("icMED POST {Url} returned empty body", url);
            throw new InvalidOperationException("Empty appointment response");
        }
        _logger.LogInformation("Appointment created upstream with Id={Id} Status={Status}", response.Id, response.Status);
        return response;
    }

    private static string Truncate(string? input, int max = 2048)
        => string.IsNullOrEmpty(input) ? string.Empty : (input.Length <= max ? input : input.Substring(0, max));
}
