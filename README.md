# Mercato

Online marketplace with multi-vendor order fulfillment — a portfolio / learning project that
walks the path from a well-structured **modular monolith** to **extracted microservices**
(Kafka events, saga, YARP gateway), with an Angular frontend, Docker, Kubernetes, CI/CD and an
Azure deployment.

> Status: **Phase 1 of 15 done** (setup & solution structure). See `docs/PLAN.md` §7 for the phase
> table and `docs/DECISIONS.md` for what has been decided and built.

## Stack

| Area | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API (minimal APIs), Worker Service, C# |
| Architecture | Clean Architecture layers × DDD modules (Identity, Catalog, Inventory, Ordering, Payments, Shipping, Notifications) |
| Data | EF Core + SQL Server (monolith), PostgreSQL (Shipping), Redis |
| Messaging | Kafka, outbox pattern, saga in the Worker |
| Frontend | Angular 22, standalone components, signals, Angular Material |
| Auth | ASP.NET Core Identity + JWT → Keycloak (OIDC, PKCE) |
| Ops | Docker Compose → Kubernetes (Helm) → Azure (Container Apps, Bicep); GitHub Actions + Azure Pipelines |

## Repository layout

```
Mercato.slnx, global.json, Directory.Build.props, Directory.Packages.props, .editorconfig
docs/      PLAN.md, DECISIONS.md, REQUIREMENTS-V1.md (build target), REQUIREMENTS.md (final goal),
           ARCHITECTURE.md, LearningProcess.md, Rules/
src/       Mercato.Domain, Mercato.Contracts, Mercato.Application, Mercato.Infrastructure,
           Mercato.Api, Mercato.Worker
web/       mercato-web (Angular)
services/  tests/  tools/  deploy/  .github/   (later phases)
```

Dependency rule: `Domain` and `Contracts` reference nothing; `Application` → Domain, Contracts;
`Infrastructure` → Application; `Api` and `Worker` → Application + Infrastructure. Details in
`docs/ARCHITECTURE.md`.

## Build and run

Prerequisites: .NET 10 SDK (pinned by `global.json`), Node.js 24, Git.

```powershell
# backend
dotnet build Mercato.slnx                      # analyzers on, warnings are errors
dotnet run --project src/Mercato.Api           # OpenAPI document at /openapi/v1.json (Development)
dotnet run --project src/Mercato.Worker        # logs a heartbeat every 10 s

# frontend
cd web/mercato-web
npm install
npm start                                      # http://localhost:4200
```

## Documents

- `docs/PLAN.md` — goal, working mode, domain, architecture, stack, phases.
- `docs/REQUIREMENTS-V1.md` — what is being built first; `docs/REQUIREMENTS.md` — the final goal.
- `docs/ARCHITECTURE.md` — deployables, projects, modules, request/event flow, data ownership.
- `docs/DECISIONS.md` — architecture decision records and the per-phase log.
- `docs/Rules/` — engineering rules (general, Angular).
- `docs/LearningProcess.md` — notes on the reasoning behind the design, for re-reading.

## Author

`paultarce18` — personal project, not affiliated with any employer.
