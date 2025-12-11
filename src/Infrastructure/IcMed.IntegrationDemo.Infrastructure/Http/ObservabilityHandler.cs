using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IcMed.IntegrationDemo.Infrastructure.Http;

/// <summary>
/// Delegating handler that enriches observability for outbound HTTP calls by logging
/// request/response information and adding Activity events. Works alongside OpenTelemetry
/// HttpClient instrumentation.
/// </summary>
internal sealed class ObservabilityHandler(ILogger<ObservabilityHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var activity = Activity.Current;

        logger.LogInformation("HTTP outbound start {Method} {Url}", request.Method, request.RequestUri);

        activity?.AddEvent(new ActivityEvent("http.client.request_start"));
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            sw.Stop();
            logger.LogInformation("HTTP outbound end {Method} {Url} -> {StatusCode} in {ElapsedMs} ms",
                request.Method, request.RequestUri, (int)response.StatusCode, sw.ElapsedMilliseconds);

            activity?.AddTag("http.response_status", ((int)response.StatusCode).ToString());
            activity?.AddEvent(new ActivityEvent("http.client.response_end"));

            return response;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            logger.LogWarning("HTTP outbound timeout {Method} {Url} after {ElapsedMs} ms", request.Method, request.RequestUri, sw.ElapsedMilliseconds);
            activity?.AddTag("exception.type", nameof(TimeoutException));
            activity?.AddEvent(new ActivityEvent("http.client.timeout"));
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "HTTP outbound failure {Method} {Url} after {ElapsedMs} ms", request.Method, request.RequestUri, sw.ElapsedMilliseconds);
            activity?.AddTag("exception.type", ex.GetType().FullName ?? "Exception");
            activity?.AddEvent(new ActivityEvent("http.client.exception"));
            throw;
        }
    }
}
