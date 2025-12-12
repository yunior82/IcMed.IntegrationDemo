IcMed Integration Demo (Clean Architecture, .NET 8)

Overview
- Senior‑level API gateway to icMED: workplaces, specialities, physicians, schedules, and appointment creation.
- Clean Architecture layers: Domain, Application, Infrastructure, WebApi.
- Resilience: IHttpClientFactory + Polly (exponential retry, timeout, circuit breaker) for Identity and icMED API clients.
- Auth: OAuth2 (password or client_credentials) with in‑memory token caching and configurable skew. Supports SPA login via `POST /api/auth/login` and per‑request credentials/tokens via headers.
- Observability: Serilog + OpenTelemetry (ASP.NET Core, HttpClient, Runtime). Optional OTLP exporter via config.
- Error handling: Global middleware returns RFC7807 ProblemDetails with correlation (requestId, traceId).
- Swagger/OpenAPI with 200/400/500 response docs; HealthChecks.
- Mock vs. Live calls switchable via config.

Prerequisites
- .NET 8 SDK/Runtime installed and active on PATH.

Run
- Build: `dotnet build IcMed.IntegrationDemo.sln`
- CLI run: `dotnet run --project src/WebApi/IcMed.IntegrationDemo.WebApi/IcMed.IntegrationDemo.WebApi.csproj`
- Swagger UI: `http://localhost:5211/swagger`
- Health check: `http://localhost:5211/health`

Run/Debug from IDE
- Rider/VS: the WebApi includes launch settings at `src/WebApi/IcMed.IntegrationDemo.WebApi/Properties/launchSettings.json`.
  - Rider: Run → Edit Configurations → + → .NET → “.NET Launch Settings Profile” → select `IcMed.IntegrationDemo.WebApi`.
  - Visual Studio: Set `IcMed.IntegrationDemo.WebApi` as Startup Project and F5.

Configuration (appsettings.json → IcMed section)
- `UseMocks`: true to use the in‑memory mock client; false to call the live API.
- `IdBaseUrl`: `https://id.icmed.ro`
- `ApiBaseUrl`: `https://api2.icmed.ro`
- `ClientId` / `ClientSecret` / `Scope`: credentials provided by icMED.
- `Username` / `Password`: optional for password grant. If empty, uses `client_credentials`.
- Resilience knobs:
  - `TimeoutSeconds` (default 15)
  - `RetryCount` (default 3, exponential backoff)
  - `CircuitBreakerFailures` (default 5)
  - `CircuitBreakerDurationSeconds` (default 30)
  - `TokenSkewSeconds` (default 60) – refresh token ahead of expiry

OpenTelemetry
- Enabled for ASP.NET Core, HttpClient, and .NET runtime metrics.
- Set `OpenTelemetry:Otlp:Endpoint` to export traces/metrics via OTLP (e.g., `http://localhost:4317`).

Logging
- Serilog is configured from `appsettings.json` and writes to console by default.

Endpoints (Gateway)
- GET `/api/workplaces`
- GET `/api/specialities?workplaceId={id}`
- GET `/api/physicians?workplaceId={id}&specialityId={id}`
- GET `/api/physicians/{physicianId}/schedule/{referenceDate}?subOfficeId={id}&currentView=day|week`
- POST `/api/appointments`
- POST `/api/auth/login`

Responses & errors
- Success: 200 with the corresponding DTO.
- Validation/client errors: 400 ProblemDetails.
- Unexpected/upstream errors: 500 or gateway‑mapped codes (e.g., 502/504) as ProblemDetails.

Docker (optional)
- Build: `docker build -t icmed-demo -f src/WebApi/IcMed.IntegrationDemo.WebApi/Dockerfile .`
- Run with compose: `docker compose up --build`

Troubleshooting
- If the app fails to launch saying .NET 8 is missing while 9.x is present, ensure `.NET 8` is installed and `C:\Program Files\dotnet` is first on PATH (ahead of `C:\Users\<you>\.dotnet`).

Authentication
- Option A: Explicit login from SPA
  1. Call `POST /api/auth/login` with JSON body:
     ```json
     { "username": "<user>", "password": "<pass>" }
     ```
  2. Store the returned `access_token` and send it on subsequent requests as `Authorization: Bearer <access_token>`.

- Option B: Without using the login endpoint
  - Send credentials per request using one of:
    - `Authorization: Basic base64(username:password)`
    - `X-IcMed-Username: <user>` and `X-IcMed-Password: <pass>`
  - Or, if you already have a token, send it as:
    - `Authorization: Bearer <token>`
    - `X-IcMed-AccessToken: <token>`

Behavior
- If a Bearer token is provided inbound, the API forwards it upstream (no caching).
- If username/password are provided per request, the API exchanges them for a token via Password grant (no shared cache), then calls icMED.
- If no per-request credentials are present, the API uses `appsettings.json` (`IcMed:Username`/`IcMed:Password`) for Password grant, or falls back to Client Credentials; these flows use in-memory caching with skew.

Demo credentials (when using mocks)
- By default, the API is configured with `UseMocks=true` and demo credentials:
  - Username: `Admin`
  - Password: `Admin`
- With mocks enabled, logging in with these credentials will return a fake token and the API will serve mock data via `IcMedMockClient` (no upstream calls).
- To switch to the live API, set `IcMed:UseMocks` to `false` and provide real credentials (either via `POST /api/auth/login` from the SPA or via configuration `IcMed:Username/Password`).

Security notes
- Prefer Option A (explicit login + Bearer) in production. Avoid sending credentials on every request.
- Ensure HTTPS for all environments; configure CORS appropriately for the SPA origin.
