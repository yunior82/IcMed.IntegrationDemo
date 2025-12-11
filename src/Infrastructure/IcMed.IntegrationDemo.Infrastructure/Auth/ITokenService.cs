using System.Net.Http.Headers;

namespace IcMed.IntegrationDemo.Infrastructure.Auth;

/// <summary>
/// Provides access to a cached OAuth2 bearer token used for authenticating
/// requests against the icMED APIs.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Gets a valid <see cref="AuthenticationHeaderValue"/> with a cached bearer token.
    /// If the cache is empty or the token is near expiry, a new token will be requested
    /// from the identity provider.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The authentication header to set on outbound HTTP requests.</returns>
    Task<AuthenticationHeaderValue> GetBearerAsync(CancellationToken ct);

    /// <summary>
    /// Exchanges the provided username/password using OAuth2 Password grant and returns the token payload.
    /// This does not use nor populate the cache.
    /// </summary>
    /// <param name="username">User name.</param>
    /// <param name="password">Password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple containing access_token and expires_in seconds.</returns>
    Task<(string access_token, int expires_in)> ExchangePasswordAsync(string username, string password, CancellationToken ct);
}
