using System.Reflection;
using IcMed.IntegrationDemo.Infrastructure;
using IcMed.IntegrationDemo.Infrastructure.Auth;
using IcMed.IntegrationDemo.WebApi.Auth;
using IcMed.IntegrationDemo.WebApi.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Options & Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// CORS (for SPA)
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length == 0)
{
    // Sensible default during local dev if not configured
    corsOrigins = ["http://localhost:4200"];
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCors", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              // Do not allow credentials unless you use cookies. For Bearer tokens this is not required.
              .WithExposedHeaders("Location"));
});

// Request credentials accessor (SPA provides creds/tokens via headers)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestCredentialsAccessor, HttpContextRequestCredentialsAccessor>();

// Health Checks
builder.Services.AddHealthChecks();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IcMed Integration Demo API",
        Version = "v1",
        Description = "Clean Architecture API gateway integrating with icMED APIs (workplaces, specialities, physicians, schedule, appointments)."
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("IcMed.IntegrationDemo"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporterIfConfigured(builder.Configuration))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporterIfConfigured(builder.Configuration));

var app = builder.Build();

// Global error handling (produces RFC7807 ProblemDetails and logs)
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// Must be placed between UseRouting and MapControllers
app.UseCors("SpaCors");

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();

internal static class OTelExtensions
{
    /// <summary>
    /// Adds the OTLP exporter to the tracer provider if an endpoint is configured
    /// under configuration key <c>OpenTelemetry:Otlp:Endpoint</c>.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <param name="config">Application configuration.</param>
    /// <returns>The same <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddOtlpExporterIfConfigured(this TracerProviderBuilder builder, IConfiguration config)
    {
        var endpoint = config["OpenTelemetry:Otlp:Endpoint"];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            builder.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint));
        }
        return builder;
    }

    /// <summary>
    /// Adds the OTLP exporter to the meter provider if an endpoint is configured
    /// under configuration key <c>OpenTelemetry:Otlp:Endpoint</c>.
    /// </summary>
    /// <param name="builder">The meter provider builder.</param>
    /// <param name="config">Application configuration.</param>
    /// <returns>The same <see cref="MeterProviderBuilder"/> for chaining.</returns>
    public static MeterProviderBuilder AddOtlpExporterIfConfigured(this MeterProviderBuilder builder, IConfiguration config)
    {
        var endpoint = config["OpenTelemetry:Otlp:Endpoint"];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            builder.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint));
        }
        return builder;
    }
}
