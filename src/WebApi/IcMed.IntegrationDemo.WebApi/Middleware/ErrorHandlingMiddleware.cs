using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace IcMed.IntegrationDemo.WebApi.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs them, and returns RFC7807 ProblemDetails
/// with useful correlation properties (requestId and traceId).
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    /// <summary>
    /// Invokes the middleware to process the current HTTP context. Captures unhandled exceptions,
    /// logs them, and returns a standard RFC7807 ProblemDetails response with diagnostic information
    /// such as request and trace identifiers.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var activityId = Activity.Current?.Id;
        var requestId = context.TraceIdentifier;

        var (status, title) = ex switch
        {
            OperationCanceledException when context.RequestAborted.IsCancellationRequested
                => (StatusCodes.Status499ClientClosedRequest, "Client canceled the request"),
            OperationCanceledException
                => (StatusCodes.Status504GatewayTimeout, "Request timed out"),
            ArgumentException
                => (StatusCodes.Status400BadRequest, "Invalid argument"),
            HttpRequestException { StatusCode: not null } hre => ((int)MapUpstreamToGatewayStatus(hre.StatusCode.Value), "Upstream HTTP error"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        logger.LogError(ex, "Unhandled exception -> {Status} (RequestId={RequestId}, TraceId={TraceId})", status, requestId, activityId);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex.Message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["requestId"] = requestId
            }
        };
        if (!string.IsNullOrWhiteSpace(activityId))
            problem.Extensions["traceId"] = activityId;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;
        var json = JsonSerializer.Serialize(problem);
        await context.Response.WriteAsync(json);
    }

    private static HttpStatusCode MapUpstreamToGatewayStatus(HttpStatusCode upstream)
    {
        // For upstream failures we present a gateway view
        return upstream switch
        {
            HttpStatusCode.NotFound => HttpStatusCode.BadGateway, // 502 default mapping for upstream errors
            HttpStatusCode.BadRequest => HttpStatusCode.BadGateway,
            HttpStatusCode.Unauthorized => HttpStatusCode.BadGateway,
            HttpStatusCode.Forbidden => HttpStatusCode.BadGateway,
            HttpStatusCode.TooManyRequests => (HttpStatusCode)429,
            _ => HttpStatusCode.BadGateway
        };
    }
}
