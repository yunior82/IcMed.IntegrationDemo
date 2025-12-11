using System.Net;
using IcMed.IntegrationDemo.Infrastructure.Auth;
using IcMed.IntegrationDemo.Infrastructure.Options;
using IcMed.IntegrationDemo.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace IcMed.IntegrationDemo.Tests.Auth;

/// <summary>
/// Pruebas unitarias para <see cref="IcMed.IntegrationDemo.Infrastructure.Auth.TokenService"/>,
/// cubriendo las rutas principales de obtención de bearer, intercambio por password y manejo de errores.
/// </summary>
public class TokenServiceTests
{
    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://id.example/") };
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient("IcMed.Identity")).Returns(http);
        return mock.Object;
    }

    private static IcMedOptions DefaultOptions() => new()
    {
        IdBaseUrl = "https://id.example",
        ApiBaseUrl = "https://api.example",
        ClientId = "cid",
        ClientSecret = "sec",
        Scope = "openid api",
        TokenSkewSeconds = 1
    };

    /// <summary>
    /// Debe reenviar el bearer recibido en la solicitud (passthrough) sin invocar al endpoint de token.
    /// </summary>
    [Fact]
    public async Task GetBearer_UsesInboundBearer_WhenPresent()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var factory = CreateFactory(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var accessor = new Mock<IRequestCredentialsAccessor>();
        accessor.Setup(a => a.GetIncomingBearerToken()).Returns("XYZ");
        var svc = new TokenService(factory, cache, Options.Create(DefaultOptions()), new NullLogger<TokenService>(), accessor.Object);

        // Act
        var auth = await svc.GetBearerAsync(CancellationToken.None);

        // Assert
        Assert.Equal("Bearer", auth.Scheme);
        Assert.Equal("XYZ", auth.Parameter);
        Assert.Null(handler.LastRequest); // handler not invoked
    }

    /// <summary>
    /// Cuando llegan credenciales por solicitud, debe intercambiarlas por token sin usar la caché compartida.
    /// </summary>
    [Fact]
    public async Task GetBearer_ExchangesPerRequestCredentials_NoCache()
    {
        // Arrange
        int calls = 0;
        var handler = new FakeHttpMessageHandler
        {
            Responder = req =>
            {
                calls++;
                var form = req.Content!.ReadAsStringAsync().Result;
                Assert.Contains("grant_type=password", form);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"TKN\",\"expires_in\":3600}")
                };
            }
        };
        var factory = CreateFactory(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var accessor = new Mock<IRequestCredentialsAccessor>();
        accessor.Setup(a => a.GetUsernamePassword()).Returns(("u","p"));
        var svc = new TokenService(factory, cache, Options.Create(DefaultOptions()), new NullLogger<TokenService>(), accessor.Object);

        // Act
        var a1 = await svc.GetBearerAsync(CancellationToken.None);
        var a2 = await svc.GetBearerAsync(CancellationToken.None);

        // Assert
        Assert.Equal("TKN", a1.Parameter);
        Assert.Equal("TKN", a2.Parameter);
        Assert.Equal(2, calls); // no shared cache for per-request creds
    }

    /// <summary>
    /// Con credenciales en configuración, debe usar password grant y cachear el token.
    /// </summary>
    [Fact]
    public async Task GetBearer_UsesOptionsPassword_AndCaches()
    {
        // Arrange
        int calls = 0;
        var handler = new FakeHttpMessageHandler
        {
            Responder = req =>
            {
                calls++;
                var body = req.Content!.ReadAsStringAsync().Result;
                Assert.Contains("grant_type=password", body);
                Assert.Contains("username=u1", body);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"CFG\",\"expires_in\":120}")
                };
            }
        };
        var opts = DefaultOptions();
        opts.Username = "u1"; opts.Password = "p1";
        var svc = new TokenService(CreateFactory(handler), new MemoryCache(new MemoryCacheOptions()), Options.Create(opts), new NullLogger<TokenService>());

        // Act
        var a1 = await svc.GetBearerAsync(CancellationToken.None);
        var a2 = await svc.GetBearerAsync(CancellationToken.None);

        // Assert
        Assert.Equal("CFG", a1.Parameter);
        Assert.Equal("CFG", a2.Parameter);
        Assert.Equal(1, calls); // cached
    }

    /// <summary>
    /// Sin credenciales de usuario, debe usar client_credentials.
    /// </summary>
    [Fact]
    public async Task GetBearer_UsesClientCredentials_WhenNoUserConfigured()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Responder = req =>
            {
                var body = req.Content!.ReadAsStringAsync().Result;
                Assert.Contains("grant_type=client_credentials", body);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"CC\",\"expires_in\":3600}")
                };
            }
        };
        var svc = new TokenService(CreateFactory(handler), new MemoryCache(new MemoryCacheOptions()), Options.Create(DefaultOptions()), new NullLogger<TokenService>());

        // Act
        var auth = await svc.GetBearerAsync(CancellationToken.None);

        // Assert
        Assert.Equal("CC", auth.Parameter);
    }

    /// <summary>
    /// Debe lanzar <see cref="InvalidOperationException"/> si el endpoint de token responde no exitoso.
    /// </summary>
    [Fact]
    public async Task ExchangePassword_Throws_OnNonSuccess()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("oops")
            }
        };
        var svc = new TokenService(CreateFactory(handler), new MemoryCache(new MemoryCacheOptions()), Options.Create(DefaultOptions()), new NullLogger<TokenService>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExchangePasswordAsync("u","p", CancellationToken.None));
    }

    /// <summary>
    /// Debe lanzar <see cref="InvalidOperationException"/> cuando el JSON carece de <c>access_token</c>.
    /// </summary>
    [Fact]
    public async Task ExchangePassword_Throws_OnMalformedJson()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"no_token\":true}")
            }
        };
        var svc = new TokenService(CreateFactory(handler), new MemoryCache(new MemoryCacheOptions()), Options.Create(DefaultOptions()), new NullLogger<TokenService>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExchangePasswordAsync("u","p", CancellationToken.None));
    }
}
