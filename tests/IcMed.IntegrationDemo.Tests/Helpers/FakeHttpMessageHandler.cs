using System.Net;

namespace IcMed.IntegrationDemo.Tests.Helpers;

/// <summary>
/// Test helper <see cref="HttpMessageHandler"/> that returns a configurable response
/// and captura la última <see cref="HttpRequestMessage"/> enviada.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    /// <summary>
    /// Obtiene la última solicitud HTTP recibida por el handler durante una prueba.
    /// </summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>
    /// Delegado que permite definir dinámicamente la respuesta HTTP a devolver por prueba.
    /// Si no se establece, devuelve 200 OK con cuerpo <c>{}</c>.
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage>? Responder { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{}")
    };

    /// <summary>
    /// Intercepta el envío de la solicitud, guarda la última petición y devuelve la respuesta configurada.
    /// </summary>
    /// <param name="request">Solicitud HTTP simulada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Respuesta HTTP configurada para la prueba.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var resp = Responder?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        return Task.FromResult(resp);
    }
}
