### ADR-0001: Clean Architecture for IcMed Integration Demo

Context
- The system must expose a stable API surface while integrating with the external icMED API. Requirements include resiliency (retry with exponential backoff, timeouts, circuit breakers), token caching, observability (logs, traces, metrics), clear error responses, and the ability to mock responses for offline demos.

Decision
- Use Clean Architecture with four projects: Domain, Application, Infrastructure, WebApi.
  - Domain: Entities and contracts that model icMED concepts without dependencies.
  - Application: Ports (`IIcMedClient`) and use cases; no external dependencies.
  - Infrastructure: External integrations and cross‑cutting concerns:
    - `IcMedHttpClient` (IHttpClientFactory + Polly policies) for live calls
    - `TokenService` (MemoryCache) for OAuth2 bearer token acquisition and caching (password or client_credentials)
    - `ObservabilityHandler` DelegatingHandler for outbound logging and Activity enrichment
    - Strongly‑typed options (`IcMedOptions`) and DI setup
    - `IcMedMockClient` for deterministic mock data
  - WebApi: Presentation endpoints, Swagger, HealthChecks, global error handling middleware, Serilog, OpenTelemetry.
- Resilience via Polly on both identity and API clients: exponential retry, timeout, and circuit breaker.
- Token cache with `IMemoryCache`, with configurable skew to proactively refresh before expiry.
- Mock vs Live switch through configuration (`IcMed:UseMocks`).
- OpenTelemetry enabled for ASP.NET Core, HttpClient, and .NET runtime metrics; OTLP exporter is conditionally added when `OpenTelemetry:Otlp:Endpoint` is configured.
- Global error handling middleware converts unhandled exceptions into RFC7807 `ProblemDetails` with correlation ids (requestId, traceId). Swagger documents 200/400/500 outcomes for all endpoints.

Consequences
- Clear separation of concerns eases testing and future changes (e.g., moving token cache to Redis, adding CI, or switching exporters in OpenTelemetry).
- The gateway can be run locally with mocks for demos and switched to live for integration testing without code changes.
- Consistent error responses and rich telemetry enable effective troubleshooting and production readiness.

Status
- Accepted; implemented in the repository.
