using IcMed.IntegrationDemo.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace IcMed.IntegrationDemo.WebApi.Controllers;

/// <summary>
/// Provides authentication-related APIs, including getting an OAuth2 access token
/// by exchanging user credentials against the icMED identity server.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ITokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Represents a request to log in by providing user credentials
    /// (username and password) for authentication.
    /// </summary>
    public sealed record LoginRequest(string Username, string Password);

    /// <summary>
    /// Represents the response containing an OAuth2 access token, its expiration time,
    /// and the token type, obtained after successfully authenticating
    /// against the icMED identity server.
    /// </summary>
    public sealed record TokenResponse(string AccessToken, int ExpiresIn, string TokenType = "Bearer");

    /// <summary>
    /// Exchanges provided username/password for an OAuth2 access token against icMED identity server.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationProblem("Username and Password are required.");
        }

        try
        {
            var (accessToken, expiresIn) = await tokenService.ExchangePasswordAsync(request.Username, request.Password, ct);
            return Ok(new TokenResponse(accessToken, expiresIn));
        }
        catch (InvalidOperationException ex)
        {
            // likely invalid credentials
            return Unauthorized(new { error = ex.Message });
        }
    }
}
