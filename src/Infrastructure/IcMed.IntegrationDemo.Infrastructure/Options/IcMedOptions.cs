namespace IcMed.IntegrationDemo.Infrastructure.Options;

/// <summary>
/// Strongly-typed options bound from configuration section <see cref="SectionName"/>
/// to configure icMED integration and resiliency parameters.
/// </summary>
public sealed class IcMedOptions
{
    /// <summary>
    /// Name of the configuration section containing these options.
    /// </summary>
    public const string SectionName = "IcMed";

    /// <summary>
    /// Base URL of the identity server used to obtain access tokens.
    /// </summary>
    public string IdBaseUrl { get; set; } = "https://id.icmed.ro";

    /// <summary>
    /// Base URL of the icMED API (resource server).
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api2.icmed.ro";

    /// <summary>
    /// OAuth2 client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 requested scopes (space-separated).
    /// </summary>
    public string Scope { get; set; } = "openid appointments_anon";

    /// <summary>
    /// Username for password grant flow (optional). If empty, client credentials is used.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for password grant flow (optional).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// When true, the application uses in-memory mock client instead of calling the live API.
    /// </summary>
    public bool UseMocks { get; set; } = false;

    /// <summary>
    /// HTTP client timeout for outbound calls (seconds).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Number of retries for transient errors.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Allowed failures before opening the circuit breaker.
    /// </summary>
    public int CircuitBreakerFailures { get; set; } = 5;

    /// <summary>
    /// Duration in seconds the circuit remains open before half-open attempts.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Seconds to subtract from token expiry when caching, to proactively refresh before expiration.
    /// </summary>
    public int TokenSkewSeconds { get; set; } = 60;
}
