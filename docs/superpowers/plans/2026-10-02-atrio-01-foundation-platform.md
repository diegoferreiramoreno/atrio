# Átrio Foundation & Platform Baseline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the independent Átrio repository with a production-shaped .NET 10 + React 19 foundation that builds, tests, runs with PostgreSQL in Docker, and enforces the approved architectural boundaries before business features are added.

**Architecture:** The repository contains a modular-monolith ASP.NET Core API split into Domain/Application/Infrastructure/Api projects, a React/Vite SPA, one PostgreSQL database, and Docker Compose for local development. This plan deliberately implements platform seams only: dependency direction, error/health/version contracts, persistence wiring, frontend bootstrap infrastructure, development containers and CI.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10, Npgsql, xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql, NetArchTest.Rules, React 19, TypeScript, Vite, React Router, TanStack Query, Vitest, React Testing Library, Docker Compose, PostgreSQL 17.

**Spec:** `docs/superpowers/specs/2026-10-02-atrio-architecture-design.md`

## Global Constraints

- Repository is independent from BiblioFlux.
- Backend uses .NET 10 / C# and Clean Architecture with `Atrio.Domain`, `Atrio.Application`, `Atrio.Infrastructure`, `Atrio.Api`.
- Frontend uses React 19 + TypeScript + Vite.
- PostgreSQL is the only database; no Redis, RabbitMQ, Kafka, Elasticsearch or Kubernetes.
- API base path is `/api/v1`.
- Production architecture is same-origin; development must support `/api` proxying.
- API errors use ProblemDetails with stable `code` and `traceId`.
- Health endpoints are `/health/live` and `/health/ready`.
- IDs use UUID; new domain entities later prefer UUIDv7.
- Configuration and secrets are external to code; no real secrets committed.
- Tests that depend on relational behavior use real PostgreSQL via Testcontainers.
- Code, Docker configuration and paths must be portable from Windows/Docker Desktop to Linux/Docker Engine.
- No product-domain feature is implemented in this plan.

## Review Focus

1. **PostgreSQL starts late/unavailable:** liveness stays healthy, readiness is unhealthy, and readiness recovers after the database becomes reachable. Covered by Task 4 integration tests.
2. **Unexpected API exception:** response is safe ProblemDetails with `500`, stable `internal_error`, and `traceId`; stack trace is not returned. Covered by Task 3 integration tests.
3. **Layer boundary regression:** Domain cannot reference Application/Infrastructure/Api; Application cannot reference Infrastructure/Api. Covered by Task 2 architecture tests.
4. **Cross-platform execution:** no project/runtime path assumes `C:\` or Windows separators; Docker smoke test is path-neutral. Covered by Task 6.
5. **Frontend API failure:** shell renders a recoverable error state rather than a blank screen when version/bootstrap request fails. Covered by Task 5 component test.

---

## Target Repository Structure

```text
atrio/
├── apps/
│   ├── api/
│   │   ├── Atrio.slnx
│   │   ├── Directory.Build.props
│   │   ├── Directory.Packages.props
│   │   ├── src/
│   │   │   ├── Atrio.Domain/
│   │   │   ├── Atrio.Application/
│   │   │   ├── Atrio.Infrastructure/
│   │   │   └── Atrio.Api/
│   │   └── tests/
│   │       ├── Atrio.Architecture.Tests/
│   │       ├── Atrio.Domain.Tests/
│   │       ├── Atrio.Application.Tests/
│   │       └── Atrio.Api.IntegrationTests/
│   └── web/
│       ├── src/
│       │   ├── app/
│       │   └── shared/
│       └── ...
├── infra/
│   └── caddy/
│       └── Caddyfile.dev
├── docs/
│   └── superpowers/
│       ├── specs/
│       └── plans/
├── .github/
│   └── workflows/
│       └── ci.yml
├── docker-compose.yml
├── .env.example
├── .editorconfig
├── .gitignore
└── README.md
```

---

### Task 1: Repository and build skeleton

**Files:**
- Create: `apps/api/Atrio.slnx`
- Create: `apps/api/Directory.Build.props`
- Create: `apps/api/Directory.Packages.props`
- Create: `apps/api/src/Atrio.Domain/Atrio.Domain.csproj`
- Create: `apps/api/src/Atrio.Application/Atrio.Application.csproj`
- Create: `apps/api/src/Atrio.Infrastructure/Atrio.Infrastructure.csproj`
- Create: `apps/api/src/Atrio.Api/Atrio.Api.csproj`
- Create: `apps/api/tests/Atrio.Domain.Tests/Atrio.Domain.Tests.csproj`
- Create: `apps/api/tests/Atrio.Application.Tests/Atrio.Application.Tests.csproj`
- Create: `apps/api/tests/Atrio.Architecture.Tests/Atrio.Architecture.Tests.csproj`
- Create: `apps/api/tests/Atrio.Api.IntegrationTests/Atrio.Api.IntegrationTests.csproj`
- Create: `apps/web/package.json`
- Create: `apps/web/tsconfig.json`
- Create: `apps/web/vite.config.ts`
- Create: `.editorconfig`
- Create: `.gitignore`

**Interfaces:**
- Consumes: none.
- Produces: buildable .NET solution with project references `Application -> Domain`, `Infrastructure -> Application + Domain`, `Api -> Application + Infrastructure`; buildable React/Vite project and test runner.

- [ ] **Step 1: Create the repository/project manifests and project references**

Use central NuGet package management in `Directory.Packages.props`. Add only packages needed by this plan. Do not add MediatR, AutoMapper, Redis, messaging or product feature packages.

- [ ] **Step 2: Add a backend compilation marker per assembly**

Create:
- `Atrio.Domain/DomainAssemblyMarker.cs`
- `Atrio.Application/ApplicationAssemblyMarker.cs`
- `Atrio.Infrastructure/InfrastructureAssemblyMarker.cs`

Each is a public sealed marker type used only by tests/registration.

- [ ] **Step 3: Add the minimal frontend entrypoint**

Create:
- `apps/web/index.html`
- `apps/web/src/main.tsx`
- `apps/web/src/app/App.tsx`

`App` may render only `Átrio` at this stage.

- [ ] **Step 4: Verify clean builds**

Run:
```bash
dotnet build apps/api/Atrio.slnx
npm --prefix apps/web install
npm --prefix apps/web run build
```

Expected: both builds succeed with zero compile/type errors.

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "chore: bootstrap atrio solution and web app"
```

---

### Task 2: Enforce Clean Architecture dependency rules

**Files:**
- Create: `apps/api/tests/Atrio.Architecture.Tests/LayerDependencyTests.cs`
- Modify: `apps/api/tests/Atrio.Architecture.Tests/Atrio.Architecture.Tests.csproj`

**Interfaces:**
- Consumes: `DomainAssemblyMarker`, `ApplicationAssemblyMarker`, `InfrastructureAssemblyMarker`.
- Produces: executable architecture rules preventing forbidden project/layer dependencies.

- [ ] **Step 1: Write failing architecture tests**

Create tests named:

```csharp
Domain_must_not_depend_on_outer_layers()
Application_must_not_depend_on_infrastructure_or_api()
Infrastructure_must_not_depend_on_api()
```

Assertions must inspect the real assemblies with NetArchTest and fail if forbidden dependencies exist.

- [ ] **Step 2: Run architecture tests**

Run:
```bash
dotnet test apps/api/tests/Atrio.Architecture.Tests/Atrio.Architecture.Tests.csproj
```

Expected: tests initially expose any incorrect references introduced by the scaffold; if scaffold is already clean, deliberately verify the test by temporarily adding a forbidden reference locally, then remove it before continuing.

- [ ] **Step 3: Correct project references so all dependency rules pass**

Final allowed direction:

```text
Domain          → no Atrio project
Application     → Domain
Infrastructure  → Application, Domain
Api             → Application, Infrastructure
```

- [ ] **Step 4: Run the architecture suite again**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/api
git commit -m "test: enforce clean architecture dependencies"
```

---

### Task 3: Establish the API contract baseline

**Files:**
- Create: `apps/api/src/Atrio.Api/Program.cs`
- Create: `apps/api/src/Atrio.Api/Errors/AtrioProblemDetailsService.cs`
- Create: `apps/api/src/Atrio.Api/Errors/ErrorCodes.cs`
- Create: `apps/api/src/Atrio.Api/Diagnostics/TraceIdMiddleware.cs`
- Create: `apps/api/src/Atrio.Api/System/SystemEndpoints.cs`
- Create: `apps/api/src/Atrio.Api/System/BuildInfo.cs`
- Create: `apps/api/tests/Atrio.Api.IntegrationTests/ApiFactory.cs`
- Create: `apps/api/tests/Atrio.Api.IntegrationTests/System/SystemEndpointTests.cs`
- Create: `apps/api/tests/Atrio.Api.IntegrationTests/Errors/ProblemDetailsTests.cs`

**Interfaces:**
- Consumes: ASP.NET Core only.
- Produces:
  - `GET /api/v1/system/version`
  - uniform ProblemDetails containing `code` and `traceId`
  - `BuildInfo(string Version, string Commit)`
  - per-request trace identifier.

- [ ] **Step 1: Write failing version endpoint test**

Test `GET /api/v1/system/version` returns `200` and JSON fields:

```text
version
commit
```

Values come from configuration/environment, never hardcoded environment-specific values.

- [ ] **Step 2: Write failing unexpected-error ProblemDetails test**

Expose a test-only failing endpoint through `ApiFactory` test configuration. Assert response:

```text
status = 500
code = "internal_error"
traceId is non-empty
```

and body does **not** contain exception stack trace or physical paths.

- [ ] **Step 3: Run targeted tests**

Run:
```bash
dotnet test apps/api/tests/Atrio.Api.IntegrationTests/Atrio.Api.IntegrationTests.csproj
```

Expected: FAIL because API baseline is not implemented.

- [ ] **Step 4: Implement API baseline**

Signatures:

```csharp
public sealed record BuildInfo(string Version, string Commit);

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints);
}

public static class ErrorCodes
{
    public const string InternalError = "internal_error";
}
```

Configure ASP.NET Core ProblemDetails and trace correlation in `Program.cs`. Production error responses must not serialize exception details.

- [ ] **Step 5: Run integration tests**

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/api
git commit -m "feat: add api diagnostics and problem details baseline"
```

---

### Task 4: Add PostgreSQL persistence wiring and health semantics

**Files:**
- Create: `apps/api/src/Atrio.Infrastructure/Persistence/AtrioDbContext.cs`
- Create: `apps/api/src/Atrio.Infrastructure/DependencyInjection.cs`
- Create: `apps/api/src/Atrio.Application/DependencyInjection.cs`
- Create: `apps/api/src/Atrio.Api/Health/HealthEndpoints.cs`
- Modify: `apps/api/src/Atrio.Api/Program.cs`
- Create: `apps/api/tests/Atrio.Api.IntegrationTests/Infrastructure/PostgresContainerFixture.cs`
- Create: `apps/api/tests/Atrio.Api.IntegrationTests/Health/HealthEndpointTests.cs`

**Interfaces:**
- Consumes: configuration key `ConnectionStrings:Atrio`.
- Produces:
  - `AtrioDbContext(DbContextOptions<AtrioDbContext>)`
  - `IServiceCollection AddApplication()`
  - `IServiceCollection AddInfrastructure(IConfiguration)`
  - `GET /health/live`
  - `GET /health/ready`.

- [ ] **Step 1: Write failing health integration tests**

Using Testcontainers PostgreSQL, assert:

```text
/health/live  → 200 while API process is alive
/health/ready → 200 when PostgreSQL is reachable
```

Add a test configuration with an unreachable PostgreSQL connection and assert:

```text
/health/live  → 200
/health/ready → 503
```

Do not expose connection strings or database exception details in response body.

- [ ] **Step 2: Run health tests**

Expected: FAIL.

- [ ] **Step 3: Implement persistence registration**

Exact public signatures:

```csharp
public sealed class AtrioDbContext : DbContext
{
    public AtrioDbContext(DbContextOptions<AtrioDbContext> options);
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration);
}
```

Application project exposes:

```csharp
public static IServiceCollection AddApplication(
    this IServiceCollection services);
```

Register Npgsql and PostgreSQL health check. Do not add business entities or migrations in this task.

- [ ] **Step 4: Implement health endpoint mapping**

Exact endpoints:

```text
GET /health/live
GET /health/ready
```

Liveness must not depend on PostgreSQL. Readiness must.

- [ ] **Step 5: Run integration tests**

Expected: PASS for reachable/unreachable scenarios.

- [ ] **Step 6: Run all backend tests**

```bash
dotnet test apps/api/Atrio.slnx
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add apps/api
git commit -m "feat: add postgres persistence and health checks"
```

---

### Task 5: Build the React application shell and recoverable API state

**Files:**
- Create: `apps/web/src/app/router.tsx`
- Create: `apps/web/src/app/providers/AppProviders.tsx`
- Create: `apps/web/src/app/queryClient.ts`
- Create: `apps/web/src/shared/api/apiClient.ts`
- Create: `apps/web/src/shared/api/problemDetails.ts`
- Create: `apps/web/src/shared/api/systemApi.ts`
- Create: `apps/web/src/app/pages/StartPage.tsx`
- Create: `apps/web/src/app/pages/AppErrorPage.tsx`
- Create: `apps/web/src/app/pages/NotFoundPage.tsx`
- Create: `apps/web/src/app/ErrorBoundary.tsx`
- Create: `apps/web/src/test/setup.ts`
- Create: `apps/web/src/app/pages/StartPage.test.tsx`
- Modify: `apps/web/src/main.tsx`
- Modify: `apps/web/vite.config.ts`

**Interfaces:**
- Consumes: `GET /api/v1/system/version`.
- Produces:
  - central API client with `credentials: "include"`;
  - `ApiProblem` TypeScript type;
  - React Router shell;
  - TanStack Query provider;
  - recoverable startup error UI.

- [ ] **Step 1: Write failing StartPage success test**

Mock `/api/v1/system/version` and assert the page renders:

```text
Átrio
v<version>
```

without hardcoded version.

- [ ] **Step 2: Write failing API failure test**

Mock a 500 ProblemDetails response and assert the user sees a recoverable message plus a `Tentar novamente` action. The test must assert that no blank page is rendered.

- [ ] **Step 3: Run frontend tests**

```bash
npm --prefix apps/web test -- --run
```

Expected: FAIL.

- [ ] **Step 4: Implement the app shell**

`apiClient` must:
- use relative `/api/v1`;
- include cookies;
- deserialize ProblemDetails into `ApiProblem`;
- support `AbortSignal`;
- contain no authentication token storage.

`AppProviders` wires router + TanStack Query. `ErrorBoundary` handles unexpected render failures.

- [ ] **Step 5: Configure Vite `/api` development proxy**

Proxy target comes from environment/configuration with a safe development default; no production hostname is hardcoded.

- [ ] **Step 6: Run frontend test, typecheck and build**

```bash
npm --prefix apps/web test -- --run
npm --prefix apps/web run typecheck
npm --prefix apps/web run build
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add apps/web
git commit -m "feat: add react application shell"
```

---

### Task 6: Add portable Docker development environment

**Files:**
- Create: `docker-compose.yml`
- Create: `apps/api/Dockerfile`
- Create: `apps/web/Dockerfile`
- Create: `infra/caddy/Caddyfile.dev`
- Create: `.env.example`
- Create: `scripts/smoke.ps1`
- Create: `scripts/smoke.sh`
- Modify: `README.md`

**Interfaces:**
- Consumes:
  - `ConnectionStrings__Atrio`
  - build info environment variables
  - PostgreSQL environment variables from local `.env`.
- Produces: `docker compose up -d --build` development stack with `web`, `api`, `db`; Caddy may be included behind a Compose profile for same-origin smoke testing.

- [ ] **Step 1: Create Dockerfiles with multi-stage builds**

API final image contains ASP.NET runtime only. Web final image contains built static assets only. Do not bind-mount source in the production stage.

- [ ] **Step 2: Create the development Compose**

`db` uses PostgreSQL 17 and persistent named volume. API connects using Docker DNS service name, never localhost. Do not publish PostgreSQL outside the host unless the development profile explicitly requires it; document any dev-only port mapping.

- [ ] **Step 3: Add cross-platform smoke scripts**

Both scripts perform equivalent checks:

```text
GET /health/live  == 200
GET /health/ready == 200
GET /api/v1/system/version == 200
GET web root == 200
```

No script may contain absolute Windows paths.

- [ ] **Step 4: Run the Docker smoke test**

```bash
docker compose up -d --build
```

Then run the platform-appropriate smoke script.

Expected: all checks PASS.

- [ ] **Step 5: Reboot/recreate smoke**

Run:

```bash
docker compose down
docker compose up -d
```

Expected: stack becomes healthy without manual database/API ordering intervention.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml apps infra scripts .env.example README.md
git commit -m "chore: add portable docker development stack"
```

---

### Task 7: Add CI gates and developer documentation

**Files:**
- Create: `.github/workflows/ci.yml`
- Modify: `README.md`
- Create: `docs/development.md`
- Create: `docs/architecture.md`

**Interfaces:**
- Consumes: all build/test commands from Tasks 1–6.
- Produces: automated PR gates and concise developer runbook.

- [ ] **Step 1: Define CI backend job**

Run:

```bash
dotnet restore apps/api/Atrio.slnx
dotnet build apps/api/Atrio.slnx --no-restore
dotnet test apps/api/Atrio.slnx --no-build
```

Integration tests must have Docker/Testcontainers available.

- [ ] **Step 2: Define CI frontend job**

Run:

```bash
npm ci --prefix apps/web
npm --prefix apps/web run typecheck
npm --prefix apps/web test -- --run
npm --prefix apps/web run build
```

- [ ] **Step 3: Define container build/smoke job**

Build API and Web Docker images and execute the health/version smoke path. The job must fail when readiness never becomes healthy within a bounded timeout.

- [ ] **Step 4: Document local development**

`docs/development.md` must contain exact commands for:
- backend tests;
- frontend tests;
- Compose startup;
- smoke verification;
- shutdown/cleanup;
- environment-file setup from `.env.example`.

Do not include real credentials.

- [ ] **Step 5: Document architectural dependency direction**

`docs/architecture.md` records:

```text
Domain ← Application ← Infrastructure
                    ↖ Api consumes Application + Infrastructure
```

and links to the full architecture spec.

- [ ] **Step 6: Run the complete local gate**

```bash
dotnet build apps/api/Atrio.slnx
dotnet test apps/api/Atrio.slnx
npm --prefix apps/web run typecheck
npm --prefix apps/web test -- --run
npm --prefix apps/web run build
docker compose up -d --build
```

Run smoke script.

Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git add .github README.md docs
git commit -m "ci: add atrio quality gates and developer runbook"
```

---

## Plan Completion Gate

The Foundation plan is complete only when all of the following are true:

- `dotnet build apps/api/Atrio.slnx` passes.
- `dotnet test apps/api/Atrio.slnx` passes.
- architecture dependency tests pass.
- PostgreSQL Testcontainers integration tests pass.
- frontend typecheck/tests/build pass.
- Docker stack starts from a clean `docker compose up -d --build`.
- liveness succeeds independently of database readiness.
- readiness accurately reflects PostgreSQL availability.
- version endpoint returns configured version/commit.
- unexpected errors produce safe ProblemDetails with traceId.
- web app shows recoverable API failure state.
- no real secret is committed.
- no absolute Windows path exists in runtime/deployment config.
- CI reproduces the same gates.

## Self-review notes

- **Spec coverage for this subproject:** repository topology, .NET/React stack, Clean Architecture boundaries, PostgreSQL wiring, ProblemDetails, health/version endpoints, cross-platform Docker baseline and CI are covered. Product-domain functionality is intentionally assigned to later plans in the roadmap.
- **Type consistency:** public signatures introduced here are the platform seams later plans must consume: `AtrioDbContext`, `AddApplication`, `AddInfrastructure`, `BuildInfo`, central frontend `apiClient`.
- **Deferred by design:** authentication, CSRF implementation, migrations with business tables, Caddy production TLS, notifications, storage and domain modules are not partially implemented here; each has a dedicated later plan.
