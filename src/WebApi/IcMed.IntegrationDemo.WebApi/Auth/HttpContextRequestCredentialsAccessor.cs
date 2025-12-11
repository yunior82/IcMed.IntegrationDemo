using System.Text;
using IcMed.IntegrationDemo.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace IcMed.IntegrationDemo.WebApi.Auth;

/// <summary>
/// Reads incoming bearer token or username/password from the current HTTP request.
/// <para>Supports the following headers:</para>
/// <list type="bullet">
/// <item><description><c>Authorization: Bearer {token}</c></description></item>
/// <item><description><c>Authorization: Basic base64(username:password)</c></description></item>
/// <item><description><c>X-IcMed-AccessToken: {token}</c></description></item>
/// <item><description><c>X-IcMed-Username</c>, <c>X-IcMed-Password</c></description></item>
/// </list>
/// </summary>
public sealed class HttpContextRequestCredentialsAccessor(IHttpContextAccessor httpContextAccessor) : IRequestCredentialsAccessor
{
    /// <summary>
    /// Retrieves the incoming bearer token from the HTTP request headers.
    /// 
    /// The method first examines the <c>Authorization</c> header to check if it contains
    /// a <c>Bearer</c> token. If present, it extracts and returns the token value.
    /// If the <c>Authorization</c> header is absent or does not contain a bearer token,
    /// it falls back to reading the <c>X-IcMed-AccessToken</c> header.
    /// </summary>
    /// <returns>
    /// The bearer token string when present; otherwise, <see langword="null"/>.
    /// </returns>
    public string? GetIncomingBearerToken()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return null;
        var headers = ctx.Request.Headers;

        if (headers.TryGetValue("Authorization", out var auth) && !string.IsNullOrWhiteSpace(auth))
        {
            var value = auth.ToString();
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return value["Bearer ".Length..].Trim();
            }
        }

        if (headers.TryGetValue("X-IcMed-AccessToken", out var token) && !string.IsNullOrWhiteSpace(token))
        {
            return token.ToString();
        }

        return null;
    }

    /// <summary>
    /// Retrieves the username and password from the current HTTP request headers.
    /// 
    /// The method attempts to extract credentials from custom headers (<c>X-IcMed-Username</c> and <c>X-IcMed-Password</c>) first.
    /// If these headers are not present or invalid, it falls back to parsing the <c>Authorization</c> header
    /// for Basic Authentication credentials.
    /// </summary>
    /// <returns>
    /// A tuple <c>(username, password)</c> when both are present and non-empty; otherwise <c>(null, null)</c>.
    /// </returns>
    public (string? username, string? password) GetUsernamePassword()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return (null, null);
        var headers = ctx.Request.Headers;

        // Custom headers take precedence
        var username = headers.TryGetValue("X-IcMed-Username", out var u) ? u.ToString() : null;
        var password = headers.TryGetValue("X-IcMed-Password", out var p) ? p.ToString() : null;
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            return (username, password);
        }

        // Try Basic Authorization
        if (!headers.TryGetValue("Authorization", out var auth) || string.IsNullOrWhiteSpace(auth)) return (null, null);
        var value = auth.ToString();
        if (!value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return (null, null);
        var base64 = value["Basic ".Length..].Trim();
        try
        {
            var bytes = Convert.FromBase64String(base64);
            var decoded = Encoding.UTF8.GetString(bytes);
            var idx = decoded.IndexOf(':');
            if (idx > 0)
            {
                var user = decoded[..idx];
                var pass = decoded[(idx + 1)..];
                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                {
                    return (user, pass);
                }
            }
        }
        catch
        {
            // ignore malformed basic header
        }

        return (null, null);
    }
}
