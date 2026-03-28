# Pull Request Checklist

Use this checklist before merge to `develop` or `main`.

## Architecture
- [ ] Dependency direction remains inward (Presentation -> Application -> Domain -> Infrastructure).
- [ ] Domain has no infrastructure/framework coupling.
- [ ] Business rules are implemented in services/use cases, not endpoints/UI.

## SOLID
- [ ] SRP: modified classes have a single clear responsibility.
- [ ] OCP/DIP: extension points use abstractions/interfaces where needed.
- [ ] LSP/ISP: contracts remain substitutable and focused.

## TDD and Tests
- [ ] RED: expected behavior was captured with failing test(s).
- [ ] GREEN: minimal implementation added to pass tests.
- [ ] REFACTOR: code was improved without breaking tests.
- [ ] Unit/integration tests were added or updated for the change.
- [ ] Relevant test suites pass locally.

## English Code Convention
- [ ] New/changed code identifiers are in English (classes, methods, variables, DTOs, entities, constants, table/column names).
- [ ] Spanish is used only for legacy-compatible API routes or required user-facing labels.

## Backward Compatibility and Safety
- [ ] Public contracts were preserved or explicitly versioned/documented.
- [ ] No destructive data/process changes were introduced without migration/rollback notes.

## Session Logging (Mandatory for Medium/High Impact)
- [ ] `context/current_session.md` updated.
- [ ] `context/session_YYYY-MM.md` updated.
- [ ] Entry includes scope, changed files, tests, and validation evidence.

## Validation Commands (suggested)
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] Coverage command if core services were impacted.

## Stabilization Evidence (veamos-en-que-punto-estamos)

Date: 2026-03-28

| Slice | Concern | Commit Hash | Gate Evidence | SDD Refs | Session Refs |
|---|---|---|---|---|---|
| A | CQRS-lite split for invoices/quotes/credit/debit + endpoint delegation boundaries | `pending-uncommitted-worktree` | `dotnet build SPC.slnx` PASS; `dotnet test SPC.slnx --verbosity minimal` PASS; architecture guard PASS except legacy `SPC.API/Endpoints/SucursalesEndpoints.cs` refactored to service injection | `proposal` `spec` `design` `tasks` `state` | `context/current_session.md` + `context/session_2026-03.md` |
| B | Current-account vertical stabilization (endpoints/services/UI client/tests) | `pending-uncommitted-worktree` | `dotnet test SPC.Tests/SPC.Tests.csproj --filter "FullyQualifiedName~CurrentAccountServiceTests"` PASS (23/23); full build/test PASS | `proposal` `spec` `design` `tasks` `state` | `context/current_session.md` + `context/session_2026-03.md` |
| C | DocumentType + migration safety (`20260320000032_SplitServicesCQRSLite`) | `pending-uncommitted-worktree` | Apply: `dotnet ef database update 20260320000032_SplitServicesCQRSLite` PASS; Rollback: `dotnet ef database update 20260311102049_InitialCreate` PASS; Re-apply PASS; full build/test PASS | `proposal` `spec` `design` `tasks` `state` | `context/current_session.md` + `context/session_2026-03.md` |
| D | Traceability/docs/state synchronization | `n/a` | Checklist updated; session logs updated; apply-progress + DAG state updated in Engram | `proposal` `spec` `design` `tasks` `state` | `context/current_session.md` + `context/session_2026-03.md` |
