# Mercato – Project Plan

> Online marketplace with multi-vendor order fulfillment.
> Modular monolith core + extracted microservices. Portfolio / learning project.
> Last updated: 2026-10-27

## 1. Goal

Build Mercato from scratch as a portfolio project that touches the technologies a full-stack .NET enterprise developer uses — deeply enough to explain each one in an interview — at zero cost, and deployable so it can be used from any machine.

## 2. Working mode

- **One repo** (`C:\MERCATO Repo`), one solution, monorepo for all services.
- **One Claude chat per phase**, inside the "Mercato" project. Context lives in the repo: `docs/PLAN.md`, `docs/DECISIONS.md`, `CLAUDE.md`.
- **Setup is done by hand in Visual Studio** (creating projects, picking project types, references, connection strings, Docker config) with step-by-step guidance and the concept explained before each step. Claude Code / direct file writes are used for bulk code only once a step is understood.
- **End of every phase:** phase entry appended to `docs/DECISIONS.md`, Status column updated below, commit, Claude project knowledge refreshed.

### Source control & identity

- **Git hosting:** GitHub, **public** repo `github.com/paultarce18/Mercato`, owned by the personal user account `paultarce18` — never inside the Palfinger AG (or any employer) organization.
- **Commit author:** `paultarce18` with a personal e-mail, set per repo (`git config user.email`, no `--global`) so the portfolio history is not tied to an employer address. Add the personal e-mail as the primary address on the GitHub account so the account survives a job change.
- **Push credential:** `paultarce18` via Git Credential Manager. The Visual Studio sign-in account (Palfinger, for Copilot/Claude licenses) is unrelated to Git identity and stays as is.
- **Tools:** commits from Visual Studio (Git Changes window); GitHub Desktop optional; Claude Code may run `git` too. One tool at a time per commit.
- **CI/CD hosting:** GitHub Actions is primary (free on public repos, GHCR for images). An Azure DevOps project (personal Microsoft account, free tier) is connected to the same GitHub repo for a secondary Azure Pipelines build + Azure Boards, so both systems are learned (Phase 12).
- Open point to check with the employer: policy on personal projects built with company-licensed tooling.

## 3. Domain

### Roles

Admin, Vendor, Warehouse Operator, Courier, Customer, Support Agent.
**Vendor is the tenant:** every vendor-owned row carries `VendorId`; authorization enforces it (vendors see only their products/orders/payouts; support sees everything but cannot change prices).

### Core entities

Vendor, Product, Inventory, Cart, Order, VendorOrder (an order is split per vendor), OrderItem, Payment, Shipment, Return, Review, Notification.

### Workflows

- **Order saga:** `Placed → PaymentAuthorized → SplitPerVendor → Picked → Shipped → Delivered → ReturnRequested → Refunded`, with compensation (e.g. refund if stock reservation fails).
- **Stock reservation:** timed cart holds, "last unit" concurrency (optimistic concurrency, Redis locks).
- **External systems are simulated** by a console app (`tools/Mercato.Simulator`): payment-provider webhooks, warehouse scans, courier tracking events.
- **Scheduled jobs:** cart expiry, vendor payouts, abandoned-cart emails, search-index rebuild.

## 4. Architecture

### Core (modular monolith)

`Mercato.Api` with `Mercato.Domain` / `Mercato.Application` / `Mercato.Infrastructure` (Clean Architecture, inward-only references). Modules: Identity, Catalog, Ordering, Inventory. One SQL Server database.

### Extracted services

Each has its own database, container and pipeline, and references only `Mercato.Contracts`:

| Service | Store | Responsibility |
|---|---|---|
| `Mercato.Payments` | SQL Server | authorize/capture/refund via simulated provider, webhooks |
| `Mercato.Shipping` | PostgreSQL (polyglot on purpose) | shipments, courier tracking |
| `Mercato.Notifications` | Redis | consumes events, SignalR push, email (logged) |

### Communication & events

- `Mercato.Gateway` (YARP): single entry point, routing, rate limiting, edge token validation.
- `Mercato.Worker`: order saga + scheduled jobs.
- `Mercato.Contracts`: versioned event schemas and DTOs.
- Kafka between all parts; outbox pattern; idempotent consumers.
- Service-to-service auth with client-credentials tokens; distributed tracing end to end.

## 5. Technology stack (all free)

### Backend
.NET 10, ASP.NET Core Web API, C#, CQRS + MediatR (or hand-written mediator), FluentValidation.

### Frontend
Angular (current major), standalone components, signals, Angular Material, HTTP interceptors, route guards, lazy-loaded features, `angular-oauth2-oidc`, SignalR client.

### Authentication & authorization
Phase 4: ASP.NET Core Identity + own JWT with refresh tokens. Phase 7: Keycloak (OAuth2/OIDC, Authorization Code + PKCE). Role-, policy- and resource-based authorization.

### API
`Asp.Versioning`, OpenAPI/Swagger per version, ProblemDetails, custom middleware (exception handling, correlation ID, request logging, rate limiting), health checks, Polly resilience.

### Messaging & background processing
Kafka in Docker (Kafka protocol — Azure Event Hubs compatible), outbox pattern, `BackgroundService` / Quartz in the Worker.

### Data stores
EF Core code-first + SQL Server (Docker image, SSMS as client), PostgreSQL (Shipping), Redis (cache, distributed locks).

### Containers & orchestration
Multi-stage Dockerfiles; docker-compose for all infra + apps; Kubernetes locally (Docker Desktop / kind) with a Helm chart; AKS as "know it, don't run it".

### CI/CD & deployment
GitHub Actions (public repo) as primary: one workflow per deployable with path filters, PR gate, images to GitHub Container Registry. Azure DevOps (Azure Pipelines `azure-pipelines.yml` + Azure Boards) connected to the GitHub repo as secondary, to learn both. Azure: Container Apps (free grant), Azure SQL free offer, Static Web Apps for Angular, Bicep IaC.

### Testing & observability
xUnit, Shouldly, NSubstitute, `WebApplicationFactory` + Testcontainers, architecture tests, Angular unit tests, optional Playwright. Serilog + Seq, OpenTelemetry traces/metrics.

## 6. Repository layout

```
C:\MERCATO Repo\
├─ Mercato.sln, Directory.Build.props, Directory.Packages.props, .editorconfig, .gitignore
├─ CLAUDE.md, README.md
├─ docs/            PLAN.md, DECISIONS.md
├─ src/             Mercato.Contracts, Mercato.Gateway, Mercato.Domain, Mercato.Application,
│                   Mercato.Infrastructure, Mercato.Api, Mercato.Worker
├─ services/        Mercato.Payments, Mercato.Shipping, Mercato.Notifications
├─ tests/           Mercato.Domain.Tests, Mercato.Application.Tests,
│                   Mercato.Api.IntegrationTests, Mercato.Architecture.Tests
├─ tools/           Mercato.Simulator
├─ web/             mercato-web (Angular)
├─ deploy/          compose/, k8s/, azure/
└─ .github/workflows/
```

Dependency rule: Domain → nothing; Application → Domain, Contracts; Infrastructure → Application; Api/Worker → Application + Infrastructure; services → Contracts only. Enforced by `Mercato.Architecture.Tests`.

## 7. Phases

Phases 1–7 build the monolith; 8–9 extract the services afterwards, deliberately, the way real teams do.

| # | Phase | End state | Status | Chat / notes |
|---|-------|-----------|--------|--------------|
| 1 | Setup & solution structure | Solution with core projects, shared build props, Git repo on GitHub, Angular skeleton | | |
| 2 | Domain model & database | Aggregates, EF Core, SQL Server in Docker, first migration, seed data, SSMS connected | | |
| 3 | API core | CQRS handlers, validation, versioning, OpenAPI UI, middleware, ProblemDetails, logging | | |
| 4 | Auth v1 (Identity + JWT) | Identity, JWT + refresh tokens, roles & policies, vendor tenancy rules | | |
| 5 | Angular app | Layout, login, catalog/cart/orders screens per role, interceptors, guards | | |
| 6 | Docker | Dockerfiles, compose with SQL/Kafka/Redis/Seq + API + Web | | |
| 7 | Auth v2 (Keycloak / OIDC) | Keycloak, PKCE, claims → policies | | |
| 8 | Events, saga & Payments service | Kafka, outbox, Worker saga, first extracted service, Simulator | | |
| 9 | Gateway, Shipping & Notifications services | YARP, two more services, contracts versioning, service-to-service auth | | |
| 10 | Observability & cross-cutting | OpenTelemetry across services, Redis caching, health checks, resilience | | |
| 11 | Testing | Unit, integration with Testcontainers, architecture tests | | |
| 12 | CI/CD | GitHub Actions per service, PR gate, images to GHCR; Azure Pipelines + Boards on the same repo | | |
| 13 | Kubernetes | Helm chart, local cluster, Worker scaled to 2 to show the lock | | |
| 14 | Azure deployment | Bicep, Container Apps, Azure SQL, Static Web Apps, public URL | | |
| 15 | Polish & documentation | README with diagrams, demo data, interview talking points | | |

## 8. Out of scope

Mobile app, real payment provider, real email delivery (logged instead), invoicing/accounting, recommendation engine. Multi-repo split and publishing `Mercato.Contracts` as a NuGet package: optional extra at the very end.

## 9. Open questions

- MediatR vs. hand-written mediator (decide in Phase 3).
- Keycloak vs. Entra ID for the cloud deployment (decide in Phase 14).
