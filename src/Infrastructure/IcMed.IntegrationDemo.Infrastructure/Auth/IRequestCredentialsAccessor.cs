using System.Security.Claims;

namespace IcMed.IntegrationDemo.Infrastructure.Auth;

/// <summary>
/// Accesses per-request credentials or tokens provided by the SPA via HTTP headers.
/// </summary>
public interface IRequestCredentialsAccessor
{
    /// <summary>
    /// When the client already has an access token and sends it as Bearer in the Authorization header,
    /// this returns the raw token string. Returns null if not present.
    /// </summary>
    string? GetIncomingBearerToken();

    /// <summary>
    /// Tries to read username/password provided by the SPA (via Basic Authorization header or custom headers).
    /// Returns (username, password) or (null, null) when not supplied.
    /// </summary>
    (string? username, string? password) GetUsernamePassword();
}
