using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using IcMed.IntegrationDemo.Domain.Entities;
using IcMed.IntegrationDemo.Infrastructure.Auth;
using IcMed.IntegrationDemo.Infrastructure.Clients;
using IcMed.IntegrationDemo.Infrastructure.Options;
using IcMed.IntegrationDemo.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace IcMed.IntegrationDemo.Tests.Infrastructure;

/// <summary>
/// Pruebas unitarias para <see cref="IcMed.IntegrationDemo.Infrastructure.Clients.IcMedHttpClient"/>,
/// validando URLs, parámetros de consulta, manejo de errores y parseo de respuestas.
/// </summary>
public class IcMedHttpClientTests
{
    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient("IcMed.Api")).Returns(http);
        return mock.Object;
    }

    private static IcMedOptions DefaultOptions() => new()
    {
        ApiBaseUrl = "https://api.example",
        IdBaseUrl = "https://id.example"
    };

    private static ITokenService Token(string value)
    {
        var m = new Mock<ITokenService>();
        m.Setup(x => x.GetBearerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AuthenticationHeaderValue("Bearer", value));
        return m.Object;
    }

    /// <summary>
    /// Debe invocar <c>/api/Workplaces</c> y deserializar correctamente la lista de centros.
    /// </summary>
    [Fact]
    public async Task GetWorkplaces_CallsExpectedUrl_AndParses()
    {
        var handler = new FakeHttpMessageHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"id\":1,\"name\":\"A\"}]")
            }
        };
        var client = new IcMedHttpClient(CreateFactory(handler), Token("abc"), Options.Create(DefaultOptions()), new NullLogger<IcMedHttpClient>());

        var items = await client.GetWorkplacesAsync(CancellationToken.None);

        Assert.Equal("/api/Workplaces", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Single(items);
        Assert.Equal(1, items[0].Id);
    }

    /// <summary>
    /// Debe enviar el parámetro <c>workplaceId</c> en la query y parsear la respuesta.
    /// </summary>
    [Fact]
    public async Task GetSpecialities_PassesQueryParams_AndParses()
    {
        var handler = new FakeHttpMessageHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("[{\"id\":2,\"name\":\"S\"}]") } };
        var http = new IcMedHttpClient(CreateFactory(handler), Token("t"), Options.Create(DefaultOptions()), new NullLogger<IcMedHttpClient>());

        var list = await http.GetSpecialitiesAsync(10, CancellationToken.None);
        Assert.Equal("/api/specialities", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("workplaceId=10", handler.LastRequest!.RequestUri!.Query.TrimStart('?'));
        Assert.Single(list);
        Assert.Equal(2, list[0].Id);
    }

    /// <summary>
    /// Debe propagar una <see cref="HttpRequestException"/> cuando el upstream devuelve un estado no exitoso.
    /// </summary>
    [Fact]
    public async Task GetPhysicians_Error_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpMessageHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.BadGateway)
            { Content = new StringContent("err") }
        };
        var http = new IcMedHttpClient(CreateFactory(handler), Token("t"), Options.Create(DefaultOptions()), new NullLogger<IcMedHttpClient>());
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetPhysiciansAsync(1, 2, CancellationToken.None));
    }

    /// <summary>
    /// Debe enviar el body del appointment en POST y deserializar correctamente la respuesta.
    /// </summary>
    [Fact]
    public async Task CreateAppointment_PostsBody_AndParses()
    {
        var reqSeen = string.Empty;
        var handler = new FakeHttpMessageHandler
        {
            Responder = req =>
            {
                reqSeen = req.Content!.ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":123,\"status\":2}")
                };
            }
        };

        var http = new IcMedHttpClient(CreateFactory(handler), Token("t"), Options.Create(DefaultOptions()), new NullLogger<IcMedHttpClient>());
        var response = await http.CreateAppointmentAsync(new AppointmentRequest(
            ConsultReason: "consult",
            FirstName: "John",
            LastName: "Doe",
            Observations: "none",
            PhoneNo: "+40111222333",
            From: DateTime.UtcNow,
            To: DateTime.UtcNow.AddMinutes(30),
            IsActive: true,
            OfficeId: 100,
            SubOfficeId: 200,
            SpecialityId: 300,
            WorkplaceId: 400,
            PhysicianId: 1,
            PatientCode: "P001"
        ), CancellationToken.None);
        Assert.Contains("\"physicianId\":1", reqSeen);
        Assert.Equal(123, response.Id);
    }
}
