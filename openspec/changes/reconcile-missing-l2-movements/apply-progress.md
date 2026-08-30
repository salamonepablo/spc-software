# Apply Progress: reconcile-missing-l2-movements

## PR 1 preflight slice

**Status consumed:** `changeName=reconcile-missing-l2-movements`, `artifactStore=openspec`, `applyState=ready`, `actionContext=repo-local`, workspace `/home/pablo/Programmes/spc-software`, allowed root workspace only. The approved delivery path is `feature-branch-chain`; this is PR 1 only. Warning consumed: the working tree has unrelated modifications/untracked files, which were preserved and not staged. Live dry-run/data operations, database access, backup, and service control remain forbidden.

**Branch:** `reconcile-l2-preflight` (created from current `main`). No commit, staging, live invocation, database connection, backup, or service operation occurred.

## Completed task and persisted checkbox

- Task 1 is complete and marked `[x]` in `tasks.md`: added only a data-free manifest shape and repository/protected-location boundary documentation.

## Completed implementation

PR 1 tasks 2–5 are complete and marked `[x]` in `tasks.md`. Added PR-1-only generic package files:

- `scripts/remediation/reconcile-missing-l2-movements/manifest.template.json`
- `scripts/remediation/reconcile-missing-l2-movements/README.md`
- `scripts/remediation/reconcile-missing-l2-movements/tests/fixtures/synthetic-ledger.sql`
- `scripts/remediation/reconcile-missing-l2-movements/tests/path-safety.tests.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/tests/preflight.tests.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/RemediationSafety.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/preflight.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/preflight.sql`

The implementation is generic and data-free: canonical path containment rejects repository paths and a symlink alias; reports are protected-only; console status is redacted; manifest controls are fixed to the approved cardinalities without real target data; and the SQL artifact is SELECT-only and parameterized against session-local staging. No actual manifest or operational evidence was created.

## TDD Cycle Evidence

| Task | Test file | Layer | Safety net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 1 | N/A (data-free structural files) | N/A | N/A (new files) | N/A structural | N/A structural | Skipped: one template/document contract | N/A |
| 2 | `tests/path-safety.tests.ps1`, `tests/preflight.tests.ps1` | PowerShell synthetic harness | N/A (new files) | Existing synthetic harnesses executed after `pwsh` installation; path harness exposed a failing PowerShell command-expression parse | Both focused harnesses pass | Repository/alias/missing/outside paths; redaction; valid and negative controls | Strict mode/fail-fast retained; no production defaults |
| 3 | `tests/path-safety.tests.ps1` | PowerShell synthetic harness | N/A (new files) | `Assert-ProtectedPath` failed with `A parameter cannot be found that matches parameter name 'or'` | Fixed command-expression grouping; path harness passes | Repository root, relative alias, symlink alias, missing storage, outside artifact, protected write, redaction | Canonical containment calls use named parameters |
| 4 | `tests/preflight.tests.ps1` | PowerShell synthetic harness | N/A (new files) | Synthetic malformed target-control case failed before validation was added | Manifest shape validation and synthetic launcher preflight pass | Valid case plus approval, count, customer, quote, type, zero-L2, quote cardinality, account, and scope rejection cases | Parameter construction is separated from qualification; `preflight.sql` remains SELECT-only |
| 5 | `tests/preflight.tests.ps1` | PowerShell synthetic harness | N/A (new files) | Missing `New-PreflightSqlParameters` failed by command-not-found | Typed `SqlParameter` binding for document type and decimal initial L2 passes | Decimal zero and independent malformed/duplicate/control/qualification variants pass fail-closed | Added target-shape validation, typed decimal precision/scale, strict/fail-fast helpers |

## Verification

- RED: `PATH="$HOME/.dotnet:$PATH" /home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/path-safety.tests.ps1` → failed before the grouping fix (`parameter name 'or'`); `tests/preflight.tests.ps1` → failed before target-shape validation and typed parameter binding.
- GREEN/TRIANGULATE: the same explicit `pwsh` invocations both pass (`path-safety tests passed`; `preflight tests passed`). They use only disposable synthetic directories under the system temporary directory and clean them in `finally`; no database client/connection, service, backup, manifest, `.env.local`, or operational evidence was accessed.
- Synthetic launcher: explicit `pwsh` invocation of `preflight.ps1` against a generated external temporary synthetic manifest and report directory passed; console output was redacted and the temporary protected directory was removed.
- Required configured test command: `PATH="$HOME/.dotnet:$PATH" dotnet test SPC.Tests/SPC.Tests.csproj -c Release` → 306 passed, 2 pre-existing integration failures: `DocumentTypeMigrationValidationTests.DocumentTypeCatalogVerifier_PassesAfterMigration_AndFailsWhenRequiredCodeInactive` and `DocumentTypeMasterCatalogUpgrade_MigratesLegacyRows_AndRemainsConsistent`; both fail with `PlatformNotSupportedException: LocalDB is not supported on this platform`. This slice does not modify those tests or database behavior. Restore/build completed with existing NU1903 dependency vulnerability warnings.
- `git diff --check` passes. Static SQL scan found no DML/DDL keywords in the PR-1 SQL artifacts.

## Remaining tasks

No PR 1 implementation tasks remain: tasks 1–5 are visibly `[x]` in the persisted task artifact. PR 2–3 and separate live-operation tasks 6–14 remain unchecked and are outside this delegated PR-1 slice; their exact unchecked lines remain in `tasks.md` and were not altered.

**Workload / PR boundary:** first feature-branch-chain slice only (PR 1: templates, path gates, preflight, synthetic tests). The delivery decision `auto-chain` / `feature-branch-chain` was consumed; no PR-2 work was performed. No design deviation. The only no-regression limitation is the two platform-incompatible LocalDB integration tests noted above.

## PR 1 security review remediation (R1-001, R1-002)

**Status consumed/produced:** `changeName=reconcile-missing-l2-movements`, `artifactStore=both` (OpenSpec directory authoritative), `applyState=ready` because PR 2–3 tasks remain, `actionContext.mode=repo-local`, workspace and allowed edit root `/home/pablo/Programmes/spc-software`. The delivered slice is the explicitly assigned PR-1 remediation on branch `reconcile-l2-preflight`, using the existing `feature-branch-chain`/`auto-chain` delivery decision. Existing unrelated working-tree changes remained untouched. No database client, database, secrets, live operation, service control, branch action, staging, or commit occurred.

### Completed security remediation

- **R1-001:** `preflight.ps1` now maps every caught error through `Resolve-PreflightErrorCategory`; only named non-sensitive categories can be emitted, while all unknown parser/runtime errors become `PreflightFailed`. Raw exception messages are not forwarded to console output. Synthetic regression coverage invokes the launcher with sensitive-looking values in both invalid controls and malformed JSON and proves neither token appears in output.
- **R1-002:** manifest validation now fails closed for duplicate `movementControl`, `documentControl`, and `quoteControl` values. Local launcher output/report is `result=local-validation-passed` with `databaseQualification=pending`; no path claims a database qualification or SQL execution. `preflight.sql` remains SELECT-only and no SQL client is invoked by this launcher.
- Updated package documentation to describe the local-only/pending qualification contract.

**Files changed:** `scripts/remediation/reconcile-missing-l2-movements/RemediationSafety.ps1`, `preflight.ps1`, `README.md`, and `tests/preflight.tests.ps1`.

### TDD Cycle Evidence

| Task | Test file | Layer | Safety net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| R1-001/R1-002 | `tests/preflight.tests.ps1` | PowerShell synthetic harness | Existing preflight and path-safety harnesses passed before edits | Added duplicate movement/document assertions; RED failed with `Expected ManifestMovementNotUnique` | Added uniqueness guards, allow-listed error-category mapper, and local-only result; focused harness passed | Added a malformed JSON sensitive-token case plus control-invalid token case; both prove redaction and fallback category | Kept validation/normalization in pure helper functions; README clarifies output semantics; focused harness remained green |

### Verification

- `pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/preflight.tests.ps1` → pass.
- `pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/path-safety.tests.ps1` → pass.
- `PATH="$HOME/.dotnet:$PATH" dotnet test SPC.Tests/SPC.Tests.csproj -c Release` → 306 passed, 2 pre-existing `PlatformNotSupportedException: LocalDB is not supported on this platform` integration failures (the two existing `DocumentTypeMigrationValidationTests`); no remediation code accesses a database.
- SELECT-only/static scan found no DML/DDL or SQL-client invocation in PR-1 preflight files; `git diff --check` passed.

### Persisted tasks and remaining work

The authoritative `tasks.md` was re-read: PR-1 tasks 1–5 remain visibly marked `[x]`, including the preflight implementation task remediated here. No task checkbox changes were required because this corrects completed PR-1 work; PR-2/PR-3 and operational-gate tasks remain unchecked exactly as recorded in `tasks.md`. No design deviation or Clean Architecture violation: only operational tooling outside application layers changed.
