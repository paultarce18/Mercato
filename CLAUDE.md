# CLAUDE.md

This file provides guidance to Claude (Claude Code, Claude Desktop, Claude app sessions linked to this folder) when working in this repository.

## Read at every session start — mandatory

Before doing anything else in a session, read all, in this order:

1. `docs/Rules/*.md` — every rule file (`GeneralRules.md`, `AngularRules.md`); they override default behaviour.
2. `docs/PLAN.md` — the plan, architecture, stack and current phase status.
3. `docs/DECISIONS.md` — what has been decided and built so far (ADRs + phase log).
4. `docs/REQUIREMENTS-V1.md` — what is being built now; `docs/REQUIREMENTS.md` is the final goal; `docs/ARCHITECTURE.md` — how it is structured (projects, modules, dependency rule, flows).

Do not start phase work, generate code, or answer design questions from chat memory alone; the repo docs are the source of truth (GR-04).

## Rules

- `docs/Rules/GeneralRules.md` — cross-cutting rules: verify every file write by reading it back (GR-01), no installs or system changes without explicit permission (GR-10/11), never commit or push unless asked in the current turn (GR-20), Conventional Commits and branch naming (GR-22), English for everything repo-bound, no secrets and no employer code in the repo (GR-30/31), C# standards, exceptions, logging, KISS/SOLID, testing, safe renames.
- `docs/Rules/AngularRules.md` — Angular coding standard for `web/mercato-web` (Angular's own best-practices guidance plus Mercato additions).
- Further rule files (architecture, naming, module boundaries) will be added to `docs/Rules/` as phases land; the mandate above applies to all of them.

## Project status

Mercato is an online marketplace with multi-vendor order fulfillment, built as a portfolio/learning project. **Phase 1 (setup & solution structure) is done; Phase 2 (domain model & database) is next.** The solution, six core projects and the Angular skeleton exist and build clean; no domain code yet. Test projects were deferred to Phase 11.

## Build, test, run

Requires the .NET 10 SDK (pinned by `global.json`; run commands from the repo root so the pin applies) and Node.js 24 for the frontend.

```powershell
dotnet build Mercato.slnx                 # whole solution; analyzers on, warnings are errors
dotnet format Mercato.slnx --verify-no-changes   # .editorconfig compliance (run before committing)
dotnet test Mercato.slnx                  # once tests/ exists (Phase 11)
dotnet run --project src/Mercato.Api      # API: https://localhost:7xxx, OpenAPI at /openapi/v1.json in Development
dotnet run --project src/Mercato.Worker   # Worker: logs a heartbeat every 10 s
```

```powershell
cd web/mercato-web
npm install                               # once per clone
npm run build                             # production build to dist/
npm start                                 # dev server on http://localhost:4200
npm test                                  # Vitest
npx prettier --check .                    # formatting
```

In Visual Studio 2026: open `Mercato.slnx`, Build → Rebuild Solution (expect 0 warnings). Start both `Mercato.Api` and `Mercato.Worker` via "Multiple startup projects" when both are needed. The Angular app is not part of the solution; use a terminal or VS Code for it.

## Solution layout

```
Mercato.slnx, global.json, Directory.Build.props (shared settings), Directory.Packages.props (all NuGet versions)
src/   Mercato.Domain, Mercato.Contracts            → reference nothing
       Mercato.Application                          → Domain, Contracts
       Mercato.Infrastructure                       → Application
       Mercato.Api (Web API), Mercato.Worker (Worker Service) → Application, Infrastructure
web/mercato-web   Angular 22 (standalone, signals, SCSS, Vitest); own package.json, .gitignore, .editorconfig, .prettierrc
tests/ (Phase 11)   services/, tools/, deploy/ (later phases)
```

Dependency rule and module layout: `docs/ARCHITECTURE.md` §4–5 (TR-01…TR-15). Project files carry no versions and no framework settings — those live in the two `Directory.*.props` files at the root; a `.csproj` only states what differs (package references, project references, `UserSecretsId`).

## Code conventions in force

`.editorconfig` encodes GeneralRules §6 and is enforced in the build (`EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`): file-scoped namespaces, explicit types unless the type is evident (`var` only for `new`, casts, literals), braces always, private fields `camelCase` without underscore, `Async` suffix, `I`-prefixed interfaces, `ConfigureAwait(false)` outside UI/API code, PascalCase log placeholders, no primary constructors. `IDE0058` is off (fluent builder APIs). When a template or generated file trips a rule, fix the code to the rule; do not suppress without a justification comment (GeneralRules §13).

## Key documents

- `docs/PLAN.md` — project plan: goal, working mode, source control & identity, domain, architecture, tech stack, repository layout, phase table with status, out of scope, open questions.
- `docs/DECISIONS.md` — source of truth for what was decided and built. ADR table (`ADR-NNN`) and a per-phase log (Status / Built / Decisions / Known issues / TODO / Next).
- `docs/REQUIREMENTS-V1.md` — the version being built: every module, minimal use cases, three roles, growth path in §8.
- `docs/REQUIREMENTS.md` — the final functional goal (all roles, returns, reviews, payouts…).
- `docs/ARCHITECTURE.md` — technical requirements: deployables, projects and dependency rule, modules, request/event flow, data ownership, cross-cutting concerns, environments, evolution path, V1 footprint.
- `docs/LearningProcess.md` — explanations captured during the phases (why the layers/modules, how a use case becomes code); append a section per topic when the user asks to save an explanation.
- `docs/Rules/` — rule files (see above).

## Working conventions

- The user does setup steps (creating projects in Visual Studio, references, connection strings, Docker config) by hand with step-by-step guidance; explain the concept before the step. Bulk code generation only when the user asks for it.
- Tooling generators (`ng new`, `ng add`, templates) may drop AI config files (`CLAUDE.md`, `AGENTS.md`, `.mcp.json`) into sub-folders. Do not keep them there: fold useful content into `docs/Rules/` and delete the files, so the repo has one `CLAUDE.md` (this one) and no silently registered MCP servers (GR-10).
- Claude may write config and bulk files directly into the repo when asked; every write is read back from disk before it is reported (GR-01). A session linked through Claude Desktop has file access but no shell on the user's machine; builds are then run by the user in VS, or by Claude on a staged copy with its own .NET SDK.
- At the end of every phase, append a phase-log entry to `docs/DECISIONS.md` (copy the existing block template) and update the phase's Status column in `docs/PLAN.md` §7. The same entry is also pasted into the Claude project knowledge, so keep it self-contained.
- Record significant architectural choices as a new ADR row in `docs/DECISIONS.md`, including the alternatives considered.
- Work proceeds phase by phase in the order listed in `docs/PLAN.md` §7; avoid pulling in later-phase concerns (e.g. Keycloak, messaging, Kubernetes) before their phase.
- Git: repo `github.com/paultarce18/Mercato`, owner and commit author `paultarce18` with personal e-mail; never commit automatically (GR-20); Conventional Commits (GR-22).

## Intended architecture (from PLAN.md / ARCHITECTURE.md)

- Modular monolith core (Identity, Catalog, Ordering, Inventory; one SQL Server DB) with capabilities later extracted into microservices (Payments → SQL Server, Shipping → PostgreSQL, Notifications → Redis) behind a YARP gateway, communicating via Kafka events and a saga in the Worker (Phase 8–9). `Mercato.Contracts` holds versioned events/DTOs and is the only cross-service reference.
- Auth evolves in two steps: Identity + JWT first (Phase 4), then Keycloak / OIDC (Phase 7).
- Angular frontend (Phase 5); Docker (6) → Kubernetes (13) → Azure (14). CI/CD: GitHub Actions primary, Azure DevOps secondary (12).
