using System.Net;
using IcMed.IntegrationDemo.Application.Abstractions;
using IcMed.IntegrationDemo.Infrastructure.Auth;
using IcMed.IntegrationDemo.Infrastructure.Clients;
using IcMed.IntegrationDemo.Infrastructure.Http;
using IcMed.IntegrationDemo.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace IcMed.IntegrationDemo.Infrastructure;

/// <summary>
/// Provides DI registration helpers for the Infrastructure layer, wiring
/// options, token service, HTTP clients with resiliency, and the icMED client
/// (mock or live depending on configuration).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure services required for calling icMED.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">Application configuration used to bind <see cref="IcMedOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IcMedOptions>()
            .Bind(configuration.GetSection(IcMedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddTransient<ObservabilityHandler>();
        services.AddHttpClient("IcMed.Identity", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<IcMedOptions>>().Value;
            client.BaseAddress = new Uri(opts.IdBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetTimeoutPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddHttpMessageHandler<ObservabilityHandler>();

        services.AddHttpClient("IcMed.Api", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<IcMedOptions>>().Value;
            client.BaseAddress = new Uri(opts.ApiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetTimeoutPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddHttpMessageHandler<ObservabilityHandler>();

        services.AddScoped<ITokenService, TokenService>();

        // Switchable client by UseMocks
        services.AddTransient<IIcMedClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<IcMedOptions>>().Value;
            if (opts.UseMocks)
            {
                return new IcMedMockClient();
            }
            return ActivatorUtilities.CreateInstance<IcMedHttpClient>(sp);
        });

        return services;
    }

    /// <summary>
    /// Creates an exponential backoff retry policy for transient HTTP failures
    /// and 429 (Too Many Requests).
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => (int)msg.StatusCode == (int)HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// Builds a circuit breaker policy that opens after several consecutive
    /// transient failures and remains open for a short period.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Builds a timeout policy for outbound HTTP calls.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15));
    }
}
