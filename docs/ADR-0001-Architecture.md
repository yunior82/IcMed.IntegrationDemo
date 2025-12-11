### ADR-0001: Clean Architecture for IcMed Integration Demo

Context
- The system must expose a stable API surface while integrating with the external icMED API. Requirements include resiliency (retry with exponential backoff, timeouts, circuit breakers), token caching, observability (logs, traces, metrics), clear error responses, and the ability to mock responses for offline demos.

Decision
- Use Clean Architecture with four projects: Domain, Application, Infrastructure, WebApi.
  - Domain: Entities and contracts that model icMED concepts without dependencies.
  - Application: Ports (`IIcMedClient`) and use cases; no external dependencies.
  - Infrastructure: External integrations and cross‑cutting concerns:
    - `IcMedHttpClient` (IHttpClientFactory + Polly policies) for live calls
    - `TokenService` (MemoryCache) for OAuth2 bearer token acquisition and caching (password or client_credentials). It now also supports:
      - Per‑request bearer passthrough: if the SPA sends `Authorization: Bearer <token>`, the token is forwarded upstream without caching.
      - Per‑request password exchange: if the SPA sends username/password (Basic or custom headers), a token is requested via Password grant without using the shared cache.
      - Fallback: if no per‑request credentials are present, uses `IcMed:Username`/`IcMed:Password` (Password grant) or falls back to Client Credentials. Cached with configurable skew.
    - `ObservabilityHandler` DelegatingHandler for outbound logging and Activity enrichment
    - Strongly‑typed options (`IcMedOptions`) and DI setup
    - `IcMedMockClient` for deterministic mock data
    - `IRequestCredentialsAccessor` abstraction to read per‑request credentials/tokens; implemented by WebApi.
  - WebApi: Presentation endpoints, Swagger, HealthChecks, global error handling middleware, Serilog, OpenTelemetry.
    - `HttpContextRequestCredentialsAccessor` reads headers from the incoming HTTP request:
      - `Authorization: Bearer <token>`
      - `Authorization: Basic base64(username:password)`
      - `X-IcMed-AccessToken`, `X-IcMed-Username`, `X-IcMed-Password`
    - `AuthController` exposes `POST /api/auth/login` to exchange `{ username, password }` for an access token via Password grant.
- Resilience via Polly on both identity and API clients: exponential retry, timeout, and circuit breaker.
- Token cache with `IMemoryCache`, with configurable skew to proactively refresh before expiry.
- Mock vs Live switch through configuration (`IcMed:UseMocks`).
- OpenTelemetry enabled for ASP.NET Core, HttpClient, and .NET runtime metrics; OTLP exporter is conditionally added when `OpenTelemetry:Otlp:Endpoint` is configured.
- Global error handling middleware converts unhandled exceptions into RFC7807 `ProblemDetails` with correlation ids (requestId, traceId). Swagger documents 200/400/500 outcomes for all endpoints.

Implementation notes
- DI lifetimes: `ITokenService` is registered as Scoped to allow access to per‑request credentials while still caching shared tokens in `IMemoryCache`.
- The Infrastructure registers named HTTP clients `IcMed.Identity` and `IcMed.Api` with the same resiliency policies and a shared `ObservabilityHandler`.
- When `UseMocks=true`, `IIcMedClient` is resolved to `IcMedMockClient` and the live HTTP clients are not called.

Consequences
- Clear separation of concerns eases testing and future changes (e.g., moving token cache to Redis, adding CI, or switching exporters in OpenTelemetry).
- The gateway can be run locally with mocks for demos and switched to live for integration testing without code changes.
- Consistent error responses and rich telemetry enable effective troubleshooting and production readiness.
- The SPA can either perform an explicit login once and reuse the bearer in subsequent calls, or supply credentials on each request. The former is recommended for production.
- Security considerations: avoid sending username/password per request in production; prefer HTTPS and CORS restrictions for the SPA; treat inbound bearer tokens as opaque and do not cache them server‑side.

Status
- Accepted; implemented in the repository.
