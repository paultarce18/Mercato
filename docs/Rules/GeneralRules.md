# General Rules (Mercato)

Rules that apply to every Claude session and every tool (Claude app, Claude Code, Claude Desktop)
working in this repository, regardless of phase or task. Project-specific rules (architecture,
naming of Mercato types, module boundaries) go into separate files in this folder.

Where a rule says "Claude", it binds any AI assistant acting on this repository.

---

## 1. Working in this repository — verify what you did

### Rule GR-01: Never report a file change you have not verified

**Trigger:** Claude writes, edits, moves or deletes a file in the repo through any mechanism
(direct file write, file sync from a cloud session, shell command, Claude Code edit).

**Action:** Before telling the user the change is done, **read the file back from the repo**
and confirm the expected content is actually present (grep for the new section, compare size
and timestamp, open the file). A tool reporting "written" or "success" is not proof.
If verification fails, say so, fix it, and verify again. Never claim a change landed based on
the tool's return value alone.

**Why:** On 2026-10-27 a second write to `docs/PLAN.md` was reported as successful by the sync
tool but never reached disk; Claude then twice told the user the content was there. Trust the
file, not the message.

### Rule GR-02: Work inside the repo, not beside it

All deliverables for this project are written directly into `C:\MERCATO Repo` (or the connected
repo folder). Files are not handed over as downloads unless the repo is not reachable, in which
case Claude says so explicitly.

### Rule GR-03: One tool at a time per file

Claude app, Claude Code and Visual Studio may all touch the repo. Do not edit the same file from
two tools in the same moment. Commit (or at least save) before switching tools.

### Rule GR-04: Repo docs are the source of truth

`docs/PLAN.md`, `docs/DECISIONS.md` and this folder are authoritative. Chat history is not.
When a decision is made in chat, write it into the matching doc in the same session.

---

## 2. Agent guardrails — actions on the system

These constrain **actions taken on the machine**, not the writing of source code. They apply in
all permission modes, including auto-accept / automatic mode.

### Rule GR-10: No installation without explicit permission

Claude must never install anything without an explicit "yes" from the user in the current turn:

- NuGet (`dotnet add package`, `dotnet tool install`, `nuget install`)
- npm / yarn / pnpm packages, `npx` of uninstalled packages, Angular CLI schematics (`ng add`)
- Python `pip install`
- System package managers (`winget`, `choco`, `apt`, `brew`)
- MCP servers, IDE extensions, CLI tooling, Docker images pulled outside `docker compose`

Before any install, Claude stops and states **what**, **why**, and the **exact command**, then
waits. Auto-accept mode grants permission to edit and write code only.

### Rule GR-11: No system or security modifications

Claude never modifies the certificate store, the Windows registry, environment variables at
machine/user level, firewall rules, hosts file or services — by any means (shell, PowerShell,
generated code). System-wide changes are prohibited even with a "yes"; the user performs them.
Exception: `dotnet dev-certs https --trust` may be requested via the normal ask-first flow.

### Rule GR-12: Wrapper and generated-code loophole is closed

A rule binds on **intent**, not on the literal command string. `powershell -Command "..."`,
a generated script, or C# code that Claude then executes are all covered by GR-10 / GR-11.
Writing such code into source for the user to run later is allowed; executing it is not.

---

## 3. Git

### Rule GR-20: Never commit or push without explicit approval

`git status`, `git diff`, `git add` are fine. `git commit`, `git push`, `--amend`, rebase,
force-push, tag and branch deletion require an explicit request in the current turn.
A prior approval covers only that specific commit, not follow-up changes. After finishing
edits, Claude stops and reports what changed.

### Rule GR-21: Identity

Commits are authored as `paultarce18` with the personal e-mail configured per repo
(see `docs/PLAN.md` § Source control & identity). Claude never changes the Git identity.

### Rule GR-22: Branch and PR naming

The work-item title, the branch name and the PR title are three different strings. The title is
a sentence; branch and PR are short identifiers derived from it — never pasted verbatim.

- Branch: `<type>/<id>_<ShortDescriptionInPascalCase>`
  `<type>` ∈ `feature`, `bugfix`, `chore`, `docs`, `phase`; `<id>` = GitHub issue number
  (or phase number for phase work, e.g. `phase/02_DomainModelAndDatabase`).
  Description 20–30 characters, PascalCase, no filler words, **30 characters hard maximum**.
- PR title: `<id>_<Name>` — no type prefix, no spaces.
- Default source branch: `main`. If the user names one, use it verbatim.
- Commit messages: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`),
  imperative mood, English.

Examples: `feature/14_AddVendorOrderSplit` → PR `14_AddVendorOrderSplit`;
`bugfix/31_FixCartHoldExpiry` → PR `31_FixCartHoldExpiry`.

---

## 4. Language

Chat in the language the user writes in, turn by turn. **Everything repo-bound is English**:
code, comments, commit messages, PR titles/descriptions, docs, rule files, issue comments.
If unsure whether an artefact is repo-bound, default to English.

---

## 5. Confidentiality & secrets

### Rule GR-30: No employer code or data

This is a personal portfolio project. Claude never copies code, configuration, data, documents
or proprietary conventions from employer or client repositories (e.g. anything under
`C:\PALDIAG Repo`) into Mercato. Generic engineering rules may be carried over in rewritten,
project-neutral form (this file is the result of that); concrete code may not.

### Rule GR-31: No secrets in the repo

Connection strings with real passwords, JWT signing keys, API keys, Keycloak client secrets,
Azure credentials and `.env` files with real values never enter Git. Use `dotnet user-secrets`
locally, environment variables in Docker/Kubernetes, GitHub Secrets in pipelines. Committed
`appsettings*.json` contain placeholders or local-only development defaults. Claude refuses to
write a real secret into a tracked file and says why.

### Rule GR-32: Do not echo secrets

If a secret appears in a file or output, Claude does not repeat it back in chat.

---

## 6. C# coding standards

Carried over as team-style conventions, adapted to a modern .NET 10 codebase. Changing any of
these is allowed via an ADR in `docs/DECISIONS.md`.

### Naming & structure
- **One top-level type per file; file name = type name.** Nested types stay with their owner.
- Private fields `camelCase` with no underscore prefix; public/internal members `PascalCase`;
  built-in aliases (`int`, `string`) never `Int32`, `String`.
- **Async methods end with `Async`**, regardless of modifiers.
- **Identifiers are at least 3 letters** — no `i`, `x`, `e`, `ex`, `p`: use `index`, `item`,
  `exception`, `product`. Applies to lambdas, loops, catch variables, out/pattern variables.
- Usings sorted, unused removed; file-scoped namespaces; `ImplicitUsings` on.
- Attributes on their own line; modifiers in canonical order (`public static readonly`).
- Member order: constants / static readonly → static fields → instance fields → events →
  constructors → properties → methods → nested types.

### Types & members
- Prefer explicit types where the type is not obvious from the right-hand side; `var` is
  allowed only when the type is evident (`new`, casts, literals). Never `var` in `foreach`
  over non-obvious collections.
- Fields `readonly` when only assigned in the constructor; auto-properties get-only when never
  written outside the constructor; prefer object/collection initializers.
- No C# `field` keyword; use an explicit backing field when one is needed.
- No logic in constructors beyond assignment and argument validation.

### Casts
- Never an unchecked cast when the runtime type is not guaranteed. Use `is not T` + throw
  `InvalidOperationException` (contract violation) or `is T` + handle (expected case).
  Convert once at the boundary and store the typed result.

### Async
- `ConfigureAwait(false)` in library / non-UI code (Domain, Application, Infrastructure, Workers).
- No `async void` except framework event handlers. Fire-and-forget tasks carry their own
  try/catch.

### Static system APIs
- Wrap time, file system, random, environment and process access behind injectable interfaces
  (`TimeProvider`, `IFileSystem`, …) so they can be substituted in tests. Never call `DateTime.Now`
  in domain or application code.

### Encoding
- Spaces, never tabs. UTF-8. Line endings as defined in `.editorconfig` / `.gitattributes`
  (decide in Phase 1, record as ADR). Claude re-checks encoding after bulk file generation.

---

## 7. Exceptions

> An exception is either handled meaningfully, wrapped into a domain exception with the original
> as `InnerException`, or allowed to reach the global handler. It is never silently swallowed.

- **EXC-01** Validate arguments with `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`,
  `ArgumentOutOfRangeException.ThrowIf…` at public boundaries.
- **EXC-02** Custom exceptions only for **domain** failures (`OrderNotFoundException`,
  `InsufficientStockException`); never for programming errors — those are `InvalidOperationException` / `ArgumentException`.
- **EXC-03** Name `[Domain]Exception`, derive from a single project base (`MercatoException`), keep them serializable-free (no legacy ctor boilerplate on .NET 10).
- **EXC-04** Never swallow silently. A catch that does not propagate must log the exception object
  (Debug/Trace) and explain in a comment why it is safe. Exceptions: cancellation (EXC-08), and
  documented loop-swallow with rate limiting.
- **EXC-05** Broad `catch (Exception)` only at **boundaries**: middleware, background-service loops,
  message consumers, top-level handlers.
- **EXC-06** Wrap low-level exceptions with `InnerException`; rethrow with `throw;`, never `throw ex;`.
- **EXC-07** `using` / `finally` for cleanup.
- **EXC-08** Handle cancellation explicitly and quietly: catch `OperationCanceledException` first, do not log it as an error.
- **EXC-09** Log the exception **object**, not just `Message`; choose the level per § 8.
- **EXC-10** Rate-limit exception logging inside loops.
- **EXC-11** No exceptions for control flow — use `Try…`, `Result` types or lookups.
- **EXC-12** Let unhandled exceptions reach the global exception middleware (→ ProblemDetails).

---

## 8. Logging

Structured logging through `ILogger<T>` (abstraction), Serilog as the sink. Inject the logger;
never a static logger. Message templates with named placeholders, never string interpolation.

| Level | Use for |
|---|---|
| `Trace` | Very high-frequency internals (per-message, per-row). Infrastructure only. |
| `Debug` | Developer interest: handler entry/exit, timings, outgoing HTTP calls. |
| `Information` | Business events: order placed, payment authorized, user logged in, job run. Never in tight loops. |
| `Warning` | Unexpected but handled: validation failures, retries, recovered exceptions. |
| `Error` | Handled exception where the operation failed but the process is healthy. |
| `Critical` | Unhandled exceptions / process cannot continue. Global handlers only. |

- Only `Information` and above are enabled in non-development environments by default.
- **Never log secrets or personal data**: tokens, passwords, full card numbers, e-mail bodies.
  Log identifiers (`OrderId`, `UserId`), not payloads.
- Every log line in request scope carries the correlation ID (middleware does this; handlers do not add it manually).
- Do not add sinks or routing in code; configure them in `appsettings` / Serilog configuration.

---

## 9. Design: KISS, SOLID and when to decouple

> Add complexity only to solve a problem you have today. Decouple when coupling causes pain
> today; keep it simple when the pain is hypothetical.

### Keep it simple
- **K-01** No interface for a single implementation — unless it is a justified seam
  (testability of an external dependency, a port in Clean Architecture, a published contract).
- **K-02** No empty marker / pass-through interfaces or base classes.
- **K-03** No forced no-op members — the abstraction does not fit; reshape it.
- **K-04** Indirection proportional to the problem: no factory/strategy/mediator around trivial logic.
- **K-05** No speculative configurability (YAGNI). Hard-code the single value until a second caller exists.
- **K-06** Flatten control flow: guard clauses and early returns over deep nesting.
- **K-07** Prefer the smallest construct that reads clearly.
- **K-08** Small duplication beats premature abstraction. Extract on the third occurrence, not the second.
- **K-09** Do not refactor outside the task's scope. A bugfix fixes the bug. Note refactoring candidates in `DECISIONS.md` TODO instead.
- **K-10** Stable, working code is not a refactoring target because "it could be cleaner".
- **K-11** No collateral surface area: do not add properties/parameters to unrelated classes.

### SOLID (applied, not recited)
- **SRP** one reason to change per class; handlers do one use case.
- **OCP** extend via new handlers/strategies, not by growing `switch` on type.
- **LSP** derived types honour the whole contract; no stub-or-throw members.
- **ISP** segregate optional capability instead of forcing no-ops; but do not split a cohesive interface just to chase member count.
- **DIP** Application depends on abstractions it owns (ports); Infrastructure implements them.

### Decouple when
- **D-01** cross-domain side effects live in shared code → move behind an event or a port;
- **D-02** growing `if (type == …)` branches → polymorphism / strategy;
- **D-03** a dependency blocks a test → extract exactly that one interface, nothing more;
- **D-04** two people cannot work on the same file → split along domain boundaries.
- **Cost check before any decoupling:** what pain exists today, what is the smallest change that removes it, and what does it cost in new types/files/registrations? If the answer is "none today", do not decouple.

### Interfaces as contracts
- Document what the signature cannot say (null behaviour, thrown exceptions, ordering); do not document the obvious.
- Return `IReadOnlyList<T>` / `IReadOnlyCollection<T>` from contracts, not `List<T>`.
- Keep presentation concerns out of domain/application contracts.
- A published contract (`Mercato.Contracts`, API DTOs) is a promise: breaking changes get a new version, never an in-place edit (see API/event versioning in `docs/PLAN.md`).

---

## 10. Testing

### Test levels
> The level of a test is the depth at which mocking starts.
- **Unit test:** the class under test is real; its direct dependencies (level −1) are substituted.
- **Integration test:** level −1 is real too (real handler + real DbContext + real database via Testcontainers); only external adapters (payment provider, e-mail, message broker when not under test) are substituted.
- A class with no injected dependencies has unit tests only.
- Assert the **outcome** (state, returned data, what reached the next layer), not merely "the mock was called".
- Name: `MethodOrBehaviour_Scenario_ExpectedResult`. Place unit tests in `*.Tests`, integration tests in `*.IntegrationTests`.
- After adding tests, verify the test **count** changed as expected, not just that the run is green.

### Testable classes
- **T-01** Dependencies injected via constructor, never constructed inside.
- **T-02** Dependencies are abstractions or substitutable types; wrap untestable framework types.
- **T-03** No hidden dependencies or static mutable state; no `DateTime.Now`, `Random`, `File.*` directly.
- **T-04** No logic in constructors.
- **T-05** Single responsibility.
- **T-06** No `async void`.
- **T-07** Behaviour observable through the public API — no reflection in tests, no `InternalsVisibleTo` as a habit.

---

## 11. Safe renames and bulk edits

- **SR-01** Rename the exact compound identifiers, never the bare concept word.
- **SR-02** Discover references with a **substring** search (not word-boundary), across `.cs`, `.ts`, `.html`, `.json`, `.yml`, `.md`, `.csproj`, `.sql`. Include migrations and Angular code.
- **SR-03** Preserve file encoding and line endings in bulk edits.
- **SR-04** Keep file name = type name and folder = namespace leaf; move the file with the type; delete emptied folders.
- **SR-05** Do not rename data instances, seed data or sibling concepts that legitimately keep the old word.
- **SR-06** After any rename touching a shared project (`Contracts`, `Domain`), build the **whole solution** and run the full test suite before reporting done.

---

## 12. Feature flags (when introduced)

- **FF-01** Flag names are `public const string` in one `FeatureFlags` class per module; no magic strings at call sites.
- **FF-02** Never cache `IsEnabled()` in a field; evaluate at the point of use.
- **FF-03** Access the feature manager via constructor injection, never via service locator.
- **FF-04** Remove a flag (and its dead branch) once the feature is fully rolled out or cancelled; track removal as a TODO in `DECISIONS.md`.

---

## 13. Code analysis

Use the .NET analyzers at `latest-recommended` with warnings as errors (set in
`Directory.Build.props`). Keep SonarQube-style defaults (cognitive complexity ≤ 15, ≤ 7
parameters, inheritance depth ≤ 5) unless an ADR raises a threshold with a reason. Do not
suppress a warning without a justification comment.

---

## Quick reference

| Situation | Do | Rule |
|---|---|---|
| Wrote/edited a file | Read it back from the repo before saying it's done | GR-01 |
| Need a package/tool | Stop; state what/why/command; wait for "yes" | GR-10 |
| Registry / cert / env var / service change | Don't; user does it | GR-11 |
| Finished edits | Report; do not commit | GR-20 |
| New branch | `<type>/<id>_ShortPascal` (≤ 30 chars) | GR-22 |
| Anything written to the repo | English | § 4 |
| Secret shows up | Not in Git, not echoed | GR-31/32 |
| Tempted to add an interface/factory | Ask: what pain today? | K-01, D-cost check |
| Exception caught | Log the object, wrap or rethrow with `throw;` | § 7 |
| Rename | Substring search, build whole solution | § 11 |
