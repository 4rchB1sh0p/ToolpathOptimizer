# AGENTS
# Agent instructions

## Mission
Build a safe, testable, explainable toolpath analysis and constrained sequencing product. Read `ARCHITECTURE.md`, `docs/design-docs/core-beliefs.md`, `docs/product-specs/mvp.md`, `docs/SECURITY.md`, and the active execution plan before modifying anything.

## Non-negotiable rules
1. Never claim machine safety, collision avoidance, ISO compliance, or machining-time savings without verification.
2. Optimisation is **advisory**. Never emit machine-ready G-code or auto-deploy a path in the MVP.
3. Use SI internal units (metres, seconds); define explicit import/export units and reject ambiguity.
4. Keep Core pure: no filesystem, networking, database, AI, logging framework or web dependencies.
5. Use deterministic seeds/tie-breaking; preserve original inputs and report provenance, constraints and algorithm version.
6. Reject invalid/unsupported inputs explicitly; no silent approximation or dropped constraints.
7. No AI in correctness-critical calculations; optional AI summaries must cite computed results and be clearly labelled non-authoritative.
8. Treat imported geometry and metadata as untrusted; enforce quotas, parsing limits and tenant isolation.
9. Every behavior change needs tests and documentation; update architecture decisions when crossing boundaries.
10. Do not copy employer-owned code, internal file formats, machine data or confidential artifacts.

## Workflow
Propose a small plan; implement one vertical slice; run formatter, tests, static analysis and security checks; record decisions, limitations and outstanding risks. Prefer standard .NET hosting, DI, options validation, cancellation, TimeProvider, structured logging and immutable domain types. Keep command-line exit codes stable. Do not invent benchmark results.

## Repository scope
The scaffold exists (`ToolpathOptimizer.slnx`, D-019). Markdown under `docs/` remains the design source of truth; keep code and docs in sync in the same change.



## Default Behavior
- Before generating or editing code, read `.github/clean-coding-practices.md`.
- Follow that file as the default coding standard for this repository.
- Keep implementations small, readable, and consistent with existing project patterns.

## Documentation Behavior
- Before generating or editing docs, read `.github/documentation-instructions.md`.
- Keep all docs in `docs/` concise and to the point.

## Decision Logging Behavior
- Before changing code, read the relevant sections of `docs/Decisions.md` and respect accepted decisions; flag any change that contradicts one.
- After every code change that involves a meaningful decision, append an entry to `docs/Decisions.md` section 8 (Decision Log) in the same change, using its template.
- Meaningful decisions include: adding, removing or upgrading a library/package; choosing one library, pattern, primitive or protocol over another; concurrency, lifetime, error-handling, retry, security or performance tradeoffs; public contract or configuration-format changes; deliberate deviations from `.github/clean-coding-practices.md` or legacy behaviour.
- Each entry states the decision, why, the rejected alternatives, and the accepted tradeoff. Mark unconfirmed reasons as `[Inferred]`.
- Number entries sequentially and never delete them; mark replaced entries `Superseded by D-NNN` and update the affected baseline rows (sections 2–7).
- Record newly found debt in section 7, and remove it there once fixed (with a log entry).
- Skip trivial changes (renames, formatting, typo fixes) that involve no real choice.

## Testing Behavior
- Name each test source file and its test class after the production class under test, with a `Tests` suffix. For example, use `SqlDriverTests.cs` for tests of `SqlDriver`.
- Name test projects after the system under test plus a single-token test-type suffix:
  - Unit tests: `<ProjectUnderTest>.UnitTests` (for example, `ToolpathOptimizer.Core.UnitTests`).
  - Integration tests: `<Scope>.IntegrationTests` (for example, `ToolpathOptimizer.Core.IntegrationTests` for cross-component suites).
  - Keep the test type as one word (`UnitTests`, `IntegrationTests`), never a dot-separated clause like `Integration.Tests`.
- Keep the project's root namespace aligned with its assembly name (folder, `.csproj`, and namespaces must match).

## Priority
1. User request
2. `.github/clean-coding-practices.md`
3. `.github/documentation-instructions.md`
4. Accepted decisions in `docs/Decisions.md`
5. Existing repository conventions
