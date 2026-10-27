# CLAUDE.md

This file provides guidance to Claude (Claude Code, Claude Desktop, Claude app sessions linked to this folder) when working in this repository.

## Read at every session start — mandatory

Before doing anything else in a session, read all three, in this order:

1. `docs/Rules/*.md` — every rule file; they override default behaviour.
2. `docs/PLAN.md` — the plan, architecture, stack and current phase status.
3. `docs/DECISIONS.md` — what has been decided and built so far (ADRs + phase log).

Do not start phase work, generate code, or answer design questions from chat memory alone; the repo docs are the source of truth (GR-04).

## Rules

- `docs/Rules/GeneralRules.md` — cross-cutting rules: verify every file write by reading it back (GR-01), no installs or system changes without explicit permission (GR-10/11), never commit or push unless asked in the current turn (GR-20), English for everything repo-bound, no secrets and no employer code in the repo (GR-30/31), C# standards, exceptions, logging, KISS/SOLID, testing, safe renames.
- Further rule files (architecture, naming, module boundaries) will be added to `docs/Rules/` as phases land; the mandate above applies to all of them.

## Project status

Mercato is an online marketplace with multi-vendor order fulfillment, built as a portfolio/learning project. The repository is at the very start: there is no source code, build system, or tests yet — only planning docs. Update this file with build/lint/test commands and architecture notes once Phase 1 (setup & solution structure) lands.

## Key documents

- `docs/PLAN.md` — project plan: goal, working mode, source control & identity, domain (roles, entities, workflows), architecture, tech stack, repository layout, phase table with status, out of scope, open questions.
- `docs/DECISIONS.md` — source of truth for what was decided and built. Holds the ADR table (`ADR-NNN`) and a per-phase log (Status / Built / Decisions / Known issues / TODO / Next).
- `docs/Rules/` — rule files (see above).

## Working conventions

- The user does setup steps (creating projects in Visual Studio, references, connection strings, Docker config) by hand with step-by-step guidance; explain the concept before the step. Bulk code generation only when the user asks for it.
- At the end of every phase, append a phase-log entry to `docs/DECISIONS.md` (copy the existing block template) and update the phase's Status column in `docs/PLAN.md` §7. The same entry is also pasted into the Claude project knowledge, so keep it self-contained.
- Record significant architectural choices as a new ADR row in `docs/DECISIONS.md`, including the alternatives considered.
- Work proceeds phase by phase in the order listed in `docs/PLAN.md` §7; avoid pulling in later-phase concerns (e.g. Keycloak, messaging, Kubernetes) before their phase.
- Git: repo `github.com/paultarce18/Mercato`, owner and commit author `paultarce18` with personal e-mail; never commit automatically (GR-20).

## Intended architecture (from PLAN.md)

- Modular monolith core (Identity, Catalog, Ordering, Inventory; one SQL Server DB) with capabilities later extracted into microservices (Payments → SQL Server, Shipping → PostgreSQL, Notifications → Redis) behind a YARP gateway, communicating via Kafka events and a saga in the Worker (Phase 8–9). `Mercato.Contracts` holds versioned events/DTOs and is the only cross-service reference.
- Auth evolves in two steps: Identity + JWT first (Phase 4), then Keycloak / OIDC (Phase 7).
- Angular frontend (Phase 5); Docker (6) → Kubernetes (13) → Azure (14). CI/CD: GitHub Actions primary, Azure DevOps secondary (12).
