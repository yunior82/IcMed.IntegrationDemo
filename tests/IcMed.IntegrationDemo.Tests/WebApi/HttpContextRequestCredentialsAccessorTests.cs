using System;
using IcMed.IntegrationDemo.WebApi.Auth;
using Microsoft.AspNetCore.Http;
using FluentAssertions;

namespace IcMed.IntegrationDemo.Tests.WebApi;

/// <summary>
/// Pruebas unitarias para <see cref="IcMed.IntegrationDemo.WebApi.Auth.HttpContextRequestCredentialsAccessor"/>,
/// verificando extracción de bearer y credenciales desde encabezados soportados.
/// </summary>
public class HttpContextRequestCredentialsAccessorTests
{
    /// <summary>
    /// Debe leer el token de <c>Authorization: Bearer</c>.
    /// </summary>
    [Fact]
    public void GetIncomingBearerToken_FromAuthorizationBearer()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = "Bearer ABC";
        var accessor = new HttpContextRequestCredentialsAccessor(new HttpContextAccessor { HttpContext = ctx });
        accessor.GetIncomingBearerToken().Should().Be("ABC");
    }

    /// <summary>
    /// Debe leer el token del encabezado <c>X-IcMed-AccessToken</c>.
    /// </summary>
    [Fact]
    public void GetIncomingBearerToken_FromCustomHeader()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-IcMed-AccessToken"] = "XYZ";
        var accessor = new HttpContextRequestCredentialsAccessor(new HttpContextAccessor { HttpContext = ctx });
        accessor.GetIncomingBearerToken().Should().Be("XYZ");
    }

    /// <summary>
    /// Debe extraer usuario y contraseña de los encabezados personalizados.
    /// </summary>
    [Fact]
    public void GetUsernamePassword_FromCustomHeaders()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-IcMed-Username"] = "u";
        ctx.Request.Headers["X-IcMed-Password"] = "p";
        var accessor = new HttpContextRequestCredentialsAccessor(new HttpContextAccessor { HttpContext = ctx });
        var (u,p) = accessor.GetUsernamePassword();
        u.Should().Be("u");
        p.Should().Be("p");
    }

    /// <summary>
    /// Debe decodificar credenciales de <c>Authorization: Basic</c>.
    /// </summary>
    [Fact]
    public void GetUsernamePassword_FromBasicAuthorization()
    {
        var ctx = new DefaultHttpContext();
        var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("alice:secret"));
        ctx.Request.Headers.Authorization = $"Basic {basic}";
        var accessor = new HttpContextRequestCredentialsAccessor(new HttpContextAccessor { HttpContext = ctx });
        var (u,p) = accessor.GetUsernamePassword();
        u.Should().Be("alice");
        p.Should().Be("secret");
    }

    /// <summary>
    /// Debe ignorar encabezados Basic mal formados.
    /// </summary>
    [Fact]
    public void GetUsernamePassword_MalformedBasic_Ignored()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = "Basic not-base64";
        var accessor = new HttpContextRequestCredentialsAccessor(new HttpContextAccessor { HttpContext = ctx });
        var (u,p) = accessor.GetUsernamePassword();
        u.Should().BeNull();
        p.Should().BeNull();
    }
}
