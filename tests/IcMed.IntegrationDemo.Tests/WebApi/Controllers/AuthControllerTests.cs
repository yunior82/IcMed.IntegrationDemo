using IcMed.IntegrationDemo.Infrastructure.Auth;
using IcMed.IntegrationDemo.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IcMed.IntegrationDemo.Tests.WebApi.Controllers;

/// <summary>
/// Pruebas unitarias para <see cref="IcMed.IntegrationDemo.WebApi.Controllers.AuthController"/>,
/// cubriendo éxito de login, validación y credenciales inválidas.
/// </summary>
public class AuthControllerTests
{
    /// <summary>
    /// Debe devolver 200 OK con el token cuando las credenciales son válidas.
    /// </summary>
    [Fact]
    public async Task Login_ReturnsToken_OnSuccess()
    {
        // Arrange
        var tokenSvc = new Mock<ITokenService>();
        tokenSvc.Setup(s => s.ExchangePasswordAsync("u","p", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("tok", 100));
        var controller = new AuthController(tokenSvc.Object);

        // Act
        var result = await controller.Login(new AuthController.LoginRequest("u","p"), CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<AuthController.TokenResponse>(ok.Value);
        Assert.Equal("tok", body.AccessToken);
        Assert.Equal(100, body.ExpiresIn);
    }

    /// <summary>
    /// Debe retornar un problema de validación cuando falta usuario o contraseña.
    /// </summary>
    [Fact]
    public async Task Login_ReturnsValidationProblem_OnMissingFields()
    {
        // Arrange
        var controller = new AuthController(Mock.Of<ITokenService>());

        // Act
        var result = await controller.Login(new AuthController.LoginRequest("", ""), CancellationToken.None);

        // Assert
        Assert.IsType<ObjectResult>(result); // ValidationProblem returns ObjectResult (ProblemDetails)
    }

    /// <summary>
    /// Debe retornar 401 Unauthorized cuando el servicio de tokens lanza <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public async Task Login_ReturnsUnauthorized_OnInvalidCredentials()
    {
        // Arrange
        var tokenSvc = new Mock<ITokenService>();
        tokenSvc.Setup(s => s.ExchangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bad"));
        var controller = new AuthController(tokenSvc.Object);

        // Act
        var result = await controller.Login(new AuthController.LoginRequest("u","p"), CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
