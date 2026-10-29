# Mercato – Decisions & Phase Log

> Source of truth for what was decided and what was built.
> Append a new entry at the end of every phase. Paste the same entry into the Claude project knowledge.

## Architecture decision records

| ID | Date | Decision | Why | Alternatives considered |
|----|------|----------|-----|-------------------------|
| ADR-001 | 2026-10-27 | Target **.NET 10 (LTS)**, pinned with `global.json` (`version 10.0.100`, `rollForward latestFeature`). | The machine also has the .NET 11 RC SDK, which would otherwise be picked as the newest; a portfolio project should build on the released LTS with the same SDK on every machine and in CI. `latestFeature` accepts any 10.0.x SDK, never 11. | .NET 11 RC (pre-release tooling, breaks CI parity); no pin (SDK choice depends on the machine). |
| ADR-002 | 2026-10-27 | **LF line endings** in the repository, enforced by `.gitattributes` (`* text=auto eol=lf`); `.bat/.cmd/.ps1` keep CRLF. `.editorconfig` sets `end_of_line = lf`. | Linux containers, CI runners and Docker builds see consistent files; no CRLF/LF noise in diffs between Windows and Linux. Visual Studio handles LF natively. | CRLF (Windows default; noisy diffs in Docker/CI, `#!` scripts break). |
| ADR-003 | 2026-10-28 | Solution file format **`.slnx`** (`Mercato.slnx`). | Default in Visual Studio 2026 and the .NET 10 SDK; plain XML, one line per project, readable diffs and no GUID merge conflicts. Supported by `dotnet`, VS 2022 17.13+, Rider, GitHub Actions, Azure Pipelines. Reversible with `dotnet sln migrate`. | Legacy `.sln` (GUID-heavy text format, kept only for compatibility). |
| ADR-004 | 2026-10-28 | **Central Package Management**: all NuGet versions in `Directory.Packages.props`; project files reference packages without `Version`. | One place to bump versions; two projects can never disagree on a package version; required for the multi-project/multi-service layout. | Versions per `.csproj` (drifts as the project count grows). |
| ADR-005 | 2026-10-28 | **Shared build settings** in `Directory.Build.props`: `net10.0`, `Nullable`, `ImplicitUsings`, `Deterministic`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`, `GenerateDocumentationFile` (+ `NoWarn CS1591`). Style rules in `.editorconfig`. Deviation: `IDE0058` (expression value never used) off because ASP.NET/host builder APIs are fluent. | GeneralRules §6/§13 ask for analyzers at `latest-recommended` with warnings as errors; one file means every current and future project (services, tests, tools) inherits them. `GenerateDocumentationFile` is needed for IDE0005 (unused usings) to run in the build; CS1591 is suppressed so XML docs are not mandatory on every public member. | Settings per project (drift); analyzers as warnings only (ignored over time). |
| ADR-006 | 2026-10-27 | **Two requirements documents**: `REQUIREMENTS.md` is the final goal; `REQUIREMENTS-V1.md` is the build target — every module, minimal use cases, three roles. Same requirement IDs in both; items migrate one at a time via the growth path in V1 §8. `ARCHITECTURE.md` is shared by both (V1 reduces footprint, not structure). | Learning project: the architecture and technologies are the point, not functional completeness. A thin end-to-end path through every module gets to the technology work (events, saga, gateway, Docker, K8s) fastest. | One full requirements doc (too much to build before reaching the interesting technology); a V1-specific architecture (would have to be rebuilt when growing). |
| ADR-007 | 2026-10-29 | **Angular 22** skeleton in `web/mercato-web` via `ng new`: standalone components, signals, SCSS, no SSR, Vitest, Prettier, npm, `--skip-git` (one repository). Angular's generated best-practices guidance adopted as `docs/Rules/AngularRules.md`; the generated `CLAUDE.md`/`AGENTS.md`/`.mcp.json` are deleted. | Current Angular major, matches PLAN.md §5 (standalone, signals). Rules belong in `docs/Rules/`, one `CLAUDE.md` per repo, no MCP server registered by a generator (GR-10). | SSR (not needed for an app behind an API); CSS instead of SCSS (Material prefers SCSS); keeping the nested AI files (duplicate instructions, silent MCP registration). |

## Phase log

### Phase 1 – Setup & solution structure
- **Status:** Done 2026-10-29. Unit-test projects deferred to Phase 11 (Testing) at the user's request.
- **Built:**
  - Root: `global.json` (.NET 10 pin), `.gitignore`, `.gitattributes` (LF), `.editorconfig` (GeneralRules §6 as enforceable rules; IDE0058 off), `Directory.Build.props` (shared settings, analyzers `latest-recommended`, warnings as errors, `EnforceCodeStyleInBuild`, `GenerateDocumentationFile` + `NoWarn CS1591`), `Directory.Packages.props` (CPM), `Mercato.slnx`, `README.md`, `CLAUDE.md`.
  - `src/`: `Mercato.Domain`, `Mercato.Contracts` (no references), `Mercato.Application` (→ Domain, Contracts), `Mercato.Infrastructure` (→ Application), `Mercato.Api` (ASP.NET Core Web API, minimal APIs, OpenAPI; → Application, Infrastructure), `Mercato.Worker` (Worker Service; → Application, Infrastructure). Class libraries are empty shells. Template code rewritten to the rules (explicit types, weather sample removed, sealed `Worker` with constructor injection, `ConfigureAwait(false)`, PascalCase log placeholders, 10 s heartbeat). Whole solution builds with 0 warnings, `dotnet format --verify-no-changes` clean.
  - `web/mercato-web`: Angular 22.2 skeleton (`ng new`, standalone, signals, SCSS, no SSR, Vitest, Prettier); `npm run build` clean. Not part of the `.slnx`.
  - `docs/`: `REQUIREMENTS.md` (final goal), `REQUIREMENTS-V1.md` (build target, growth path), `ARCHITECTURE.md` (deployables, dependency rule TR-01…08, modules TR-10…15, request/event flows, data ownership, cross-cutting, environments, evolution path, V1 footprint), `LearningProcess.md` (why layers/modules, use case → code method), `Rules/AngularRules.md`.
  - Git: public repo `github.com/paultarce18/Mercato`, `main`, per-repo identity `paultarce18` + personal e-mail. Commits `bb3f752`, `88bd9e8`, plus the Phase 1 closing commit.
- **Decisions:** ADR-001…007. Working agreement: user creates projects/references by hand in Visual Studio with the concept explained first; Claude writes config/bulk files only on request and reads every write back from disk (GR-01); Claude can build a staged copy of the solution with its own .NET 10 SDK to reproduce errors. Minimal APIs for the Api template; controllers vs. minimal APIs decided for real in Phase 3.
- **Known issues / TODO:**
  - Commit `88bd9e8` message not in Conventional Commits form (GR-22); apply the format from now on.
  - `docs/ToDoTemp.md` is tracked and empty — keep or ignore deliberately.
  - The file-sync tool once reported a write as successful that did not land (2026-10-27); caught by the GR-01 read-back. Keep reading back.
  - `Mercato.Api` has no `UserSecretsId` yet — add in Phase 2 when the connection string is first needed.
  - Angular: `angular.json` budgets and `environments/` files are template defaults; adjust in Phase 5.
- **Next:** Phase 2 — Domain model & database: aggregates for the V1 footprint (ARCHITECTURE.md §15), EF Core + SQL Server in Docker, first migration, seed data, SSMS connected. New chat per PLAN.md §2. First GR-10 approvals there: EF Core packages, SQL Server Docker image.

### Phase 2 – Domain model & database
- **Status:**
- **Built:**
- **Decisions:**
- **Known issues / TODO:**
- **Next:**

<!-- Copy the block above for each new phase. -->
