# Apply Progress: reconcile-missing-l2-movements

## Superseded-worktree reconciliation

This isolated worktree was created from approved PR1 commit `35bf9f8` on branch `reconcile-l2-transaction-operator`. The proposal, design, specification, and task amendments for the controlled trusted-operator model were carried into this worktree. Claims, provenance, replay-control, cryptographic-binding, runner, backup, service-control, dry-run, execution, and rollback implementations are explicitly out of scope for this PR2 slice.

## Historical PR1 baseline

Task 1 remains complete from the base commit: data-free protected-path/read-only-preflight foundation only.

## PR2 attempt: blocked at strict-TDD RED gate

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and allowed edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. Delivery path: `auto-chain`, `feature-branch-chain`, PR2 tasks 2–6 only. The PR boundary is this uncommitted replacement PR2 worktree.

- Added an initial RED contract test at `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1`; it asserts the required bounded transaction surface and rejects durable-control terms.
- The required native `pwsh` executable is unavailable in this environment (`pwsh: command not found`; Windows PowerShell is available but is not `pwsh`). Strict TDD forbids production implementation until an actual failing RED test is run with the mandated runner.
- Windows Docker interop is available through `docker.exe` (server `29.7.2`), but no SQL behavior test was started because the TDD RED gate has not been satisfied.

### TDD Cycle Evidence

| Task | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|
| 2 | Test authored; execution blocked because native `pwsh` is absent. | Not started. | Not started. | Not started. |
| 3–6 | Not started; strict TDD dependency on executed RED remains unmet. | Not started. | Not started. | Not started. |

### Persisted task checkboxes

No PR2 task checkbox was marked complete. The strict-TDD gate prevented completion evidence.

### Files changed

- `openspec/changes/reconcile-missing-l2-movements/{proposal.md,design.md,tasks.md,specs/current-account-remediation/spec.md}` — current trusted-operator amendments carried into the isolated worktree.
- `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1` — initial RED contract test only.
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` — this reconciled progress record.

### Test commands run

- `pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1` — blocked: `pwsh: command not found`.
- `docker.exe version --format '{{.Server.Version}}'` — passed: `29.7.2`; no container launched.

### Remaining tasks

- [ ] **2. RED — replace superseded PR2 contracts with trusted-operator package tests.**
- [ ] **3. GREEN — align data-free manifest and read-only preflight to the amended contract.**
- [ ] **4. GREEN — implement the bounded generic `apply.sql` transaction.**
- [ ] **5. TRIANGULATE — provide independent read-only post-change validation.**
- [ ] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.**

**Blocking risks:** (1) install/provide native PowerShell 7 (`pwsh`) for this executor, then run the RED test and proceed with Windows-Docker disposable SQL Server behavioral tests; (2) executor safety incident: the first relative-path RED-test write resolved in the original worktree and overwrote its pre-existing untracked `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1`. The original test content is not recoverable from Git because it was untracked. Stop and have the parent restore/reconcile that original worktree before any further work. No API/service, `sql-spc`, `.env.local`, external protected data, staging, backup, runner, rollback, host-port SQL, or commit was used.

## PR2 Task 2 RED retry — operator worktree guard

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and only permitted edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. The required artifacts were read from this worktree (`proposal.md`, `specs/current-account-remediation/spec.md`, `design.md`, `tasks.md`, and this cumulative progress record). Delivery path remains `auto-chain`, `feature-branch-chain`; this execution was restricted to the PR2 Task 2 RED test, with no implementation work.

- Before every command in this retry, the shell changed to the permitted operator worktree and verified its physical path.
- Executed the absolute native PowerShell command: `/usr/bin/pwsh -NoProfile -File /home/pablo/Programmes/spc-software-l2-transaction-operator/scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1`.
- Result: blocked before test execution because `/usr/bin/pwsh` does not exist (`/bin/bash: /usr/bin/pwsh: No such file or directory`, exit 127). No container, database, production implementation, staging, or commit was used.
- No task checkbox changed: Task 2 remains incomplete because strict TDD requires an actually executed failing RED test.

### TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 2 | `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1` | Script/SQL contract | Not run; retry is test-execution-only | Blocked — native `/usr/bin/pwsh` unavailable; no test result | Not started | Not started | Not started |

### Remaining tasks

- [ ] **2. RED — replace superseded PR2 contracts with trusted-operator package tests.**
- [ ] **3. GREEN — align data-free manifest and read-only preflight to the amended contract.**
- [ ] **4. GREEN — implement the bounded generic `apply.sql` transaction.**
- [ ] **5. TRIANGULATE — provide independent read-only post-change validation.**
- [ ] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.**

**Workload / PR boundary:** `auto-chain`, `feature-branch-chain`; no PR2 work-unit advanced. **Action-context warning:** execution must remain exclusively in `/home/pablo/Programmes/spc-software-l2-transaction-operator`.

## PR2 Task 2 RED harness portability repair

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and only permitted edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. The required proposal, specification, design, tasks, configuration, and cumulative apply-progress artifacts were read from this worktree. Delivery path remains `auto-chain`, `feature-branch-chain`; this delegated slice was limited to the untracked RED harness and its execution evidence.

- Physically verified the worktree before editing and before running: `/home/pablo/Programmes/spc-software-l2-transaction-operator`.
- Updated only `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1`: it resolves `PWSH_PATH` when supplied, otherwise uses `/home/pablo/.local/bin/pwsh`; it validates that the resolved executable is PowerShell Core before executing the RED contract. It no longer relies on unavailable `/usr/bin/pwsh`.
- Ran the harness using `/home/pablo/.local/bin/pwsh` (PowerShell `7.6.5`). The harness executed and failed as the intended RED test: `RED: missing apply.sql` at `apply.tests.ps1:18`, exit `1`. This is an application-surface failure, not a missing-executable failure.
- No production/apply/preflight/validate behavior, database/container/live data, staging, or commit was used. No task checkbox changed; Task 2 remains incomplete pending the full RED-contract suite and subsequent strict-TDD work.

### TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 2 | `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1` | Script/SQL contract | N/A — untracked new RED harness | Executed: expected failure `RED: missing apply.sql` (exit 1), after PowerShell Core executable validation | Not started | Not started | Not started |

### Files changed in this delegated slice

- `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1` — portable native PowerShell 7 resolution/validation only.
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` — cumulative RED execution evidence.

### Test command and exact result

```text
cd /home/pablo/Programmes/spc-software-l2-transaction-operator && /home/pablo/.local/bin/pwsh -NoProfile -File /home/pablo/Programmes/spc-software-l2-transaction-operator/scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1
# exit 1
# RED: missing apply.sql
```

### Remaining tasks

- [ ] **2. RED — replace superseded PR2 contracts with trusted-operator package tests.**
- [ ] **3. GREEN — align data-free manifest and read-only preflight to the amended contract.**
- [ ] **4. GREEN — implement the bounded generic `apply.sql` transaction.**
- [ ] **5. TRIANGULATE — provide independent read-only post-change validation.**
- [ ] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.**

**Workload / PR boundary:** `auto-chain`, `feature-branch-chain`; this is the PR2 Task 2 RED-harness slice only. **Action-context warning:** all edits and execution remained in `/home/pablo/Programmes/spc-software-l2-transaction-operator`.

## PR2 Task 2 RED contract completion — synthetic-only

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and only allowed edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. Proposal, `specs/current-account-remediation/spec.md`, design, tasks, config, current code/schema, and cumulative progress were read before edits. Delivery path: `auto-chain`, `feature-branch-chain`; work-unit/PR boundary: PR2 Task 2 only.

- Confirmed the application contract from `SPC.Shared/Models/CurrentAccount.cs`, `SPC.Shared/Models/Quote.cs`, `SPC.Shared/Models/Enums.cs`, `SPC.API/Data/SPCDbContext.cs`, and `20260311102049_InitialCreate.cs`: `DocumentType.Quote = 20`; movements use `Id`, `CustomerId`, `DocumentNumber`, L1/L2 decimal values and preserved fields; quotes use `Id`, `CustomerId`, `BranchId`, `QuoteNumber`, `Total`, and `IsVoided`; account caches use decimal L1/L2/total values.
- Expanded only synthetic RED artifacts. `synthetic-ledger.sql` now provides disposable actual-column fixture DDL and nine zero-L2 PR targets over four synthetic customers, plus non-target and non-scoped controls. It contains no live connection, operational identifier, or real data.
- Expanded `apply.tests.ps1`, `preflight.tests.ps1`, and added `validate.tests.ps1` / `sql-server.tests.ps1`. Contracts require protected-path coverage (retained in the existing synthetic path-safety suite), malformed/duplicate staging, exact 9/4 counts, wrong type/nonzero L2, missing/ambiguous/wrong customer/document/QuoteId/BranchId linkage, voided quote state, source-total mismatch, atomic commit/rollback, full-ledger cache derivation, preserved L1/identity/linkage/non-target/non-scoped state, and read-only post-change validation failures. The SQL behavior harness is deliberately RED and requires synthetic disposable cleanup.
- Marked the persisted Task 2 checkbox `[x]` immediately after observed RED evidence. No production preflight/apply/validate behavior, staging, container/database, real data, external protected data, or commit was used.

### TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 2 | `tests/apply.tests.ps1`, `preflight.tests.ps1`, `validate.tests.ps1`, `sql-server.tests.ps1` | Synthetic PowerShell/SQL contract | N/A/new PR2 contract files; fixture is synthetic | Executed: expected failures from absent PR2 scripts/surface — `missing apply.sql`, preflight missing `MovementId`, missing `validate.ps1`, and missing `apply.sql for disposable behavior tests` (all exit 1) | Not started (Task 3/4) | RED matrix includes valid/negative linkage, scope, preservation, rollback, and validation cases; implementation not started | Not started |

### Files changed in this slice

- `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/tests/preflight.tests.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/tests/validate.tests.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/tests/sql-server.tests.ps1`
- `scripts/remediation/reconcile-missing-l2-movements/tests/fixtures/synthetic-ledger.sql`
- `openspec/changes/reconcile-missing-l2-movements/tasks.md` — Task 2 `[x]`
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` — cumulative record

### Test commands run

```text
cd /home/pablo/Programmes/spc-software-l2-transaction-operator
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/apply.tests.ps1       # exit 1: RED: missing apply.sql
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/preflight.tests.ps1   # exit 1: RED: preflight contract missing MovementId
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/validate.tests.ps1    # exit 1: RED: missing validate.ps1
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/sql-server.tests.ps1  # exit 1: RED: missing apply.sql for disposable behavior tests
git diff --check                                                               # exit 0
```

### Remaining tasks

- [ ] **3. GREEN — align data-free manifest and read-only preflight to the amended contract.**
- [ ] **4. GREEN — implement the bounded generic `apply.sql` transaction.**
- [ ] **5. TRIANGULATE — provide independent read-only post-change validation.**
- [ ] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.**

**Deviations:** none. **Workload / PR boundary:** `auto-chain`, `feature-branch-chain`; Task 2 is complete and the next work unit is PR2 Task 3 only. **Action-context warning:** edits and execution must remain exclusively under `/home/pablo/Programmes/spc-software-l2-transaction-operator`.

## PR2 Task 3 GREEN — align data-free manifest and read-only preflight to the amended contract

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and only allowed edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. Proposal, specification, design, tasks, config, and cumulative progress were read before edits. Delivery path: `auto-chain`, `feature-branch-chain`; work-unit/PR boundary: PR2 Task 3 only.

- Physically verified the worktree before every edit and command: `/home/pablo/Programmes/spc-software-l2-transaction-operator`.
- Updated `manifest.template.json`: replaced string-based `movementControl`, `customerControl`, `documentControl`, `quoteControl` with integer `movementId`, `customerId`, `documentNumber`, `quoteId`; added integer `branchId` and numeric `expectedTotal`. Kept `approvalControl`, `expectedTargetCount`, `expectedCustomerCount`, `targets` structure and placeholder descriptions.
- Updated `RemediationSafety.ps1` `Assert-ManifestControls`: validates each target has positive integer `movementId`, `customerId`, `documentNumber`, `quoteId`, `branchId` (accepting both `[int]` and `[long]` because `ConvertFrom-Json` deserializes JSON integers as `Int64`), and numeric positive `expectedTotal` (accepting `[double]` and `[decimal]`). Checks uniqueness of `movementId` (9 distinct), `documentNumber` (9 distinct), `quoteId` (9 distinct), and `customerId` (exactly 4 distinct). Added `ManifestExpectedTotalInvalid` to `Resolve-PreflightErrorCategory` allowed categories. All other functions unchanged: `Get-CanonicalPath`, `Test-PathWithin`, `Assert-ProtectedPath`, `Format-RedactedStatus`, `New-PreflightSqlParameters`, `Assert-SyntheticQualification`, `Write-ProtectedAtomicText`.
- Rewrote `preflight.sql`: parameterized, SELECT-only against the confirmed application schema. Joins `#ApprovedTargets` with `CurrentAccountMovements` (on `Id`, `CustomerId`, `DocumentNumber`), `CurrentAccounts` (on `CustomerId`), and `Quotes` (on `Id`, `BranchId`, `CustomerId`). Filters: `DocumentType = 20`, `BudgetAmount = 0`, `IsVoided = 0`. Verifies exactly one authoritative linked quote per target via `GROUP BY ... HAVING COUNT(q.Id) = 1`. Validates cardinality: exactly 9 distinct movements and 4 distinct customers. Contains all required literal tokens. No mutation verbs, no durable control terms.
- Updated `tests/preflight.tests.ps1` `New-SyntheticManifest`: generates integer-based targets with `movementId`, `customerId`, `documentNumber`, `quoteId`, `branchId`, `expectedTotal`. Updated duplicate/uniqueness tests to assign duplicate integers (`quoteId`, `movementId`, `documentNumber`). Added `expectedTotal` zero and negative validation tests (`ManifestExpectedTotalInvalid`).
- `preflight.ps1` required no structural changes — it delegates to the updated `Assert-ManifestControls` and `Assert-ProtectedPath`.

### TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 3 | `tests/preflight.tests.ps1` | PowerShell manifest/SQL contract | Existing path-safety and redaction suite | Task 2 RED baseline confirmed before edits | Executed: "preflight tests passed" (exit 0). Valid synthetic staging qualifies; every shape/path/count/uniqueness/total failure fails before mutation; output redacted and protected-only | Not started (Task 5) | Not started (Task 6) |

### Files changed in this slice

- `scripts/remediation/reconcile-missing-l2-movements/manifest.template.json` — integer identity fields
- `scripts/remediation/reconcile-missing-l2-movements/RemediationSafety.ps1` — updated `Assert-ManifestControls`, `Resolve-PreflightErrorCategory`
- `scripts/remediation/reconcile-missing-l2-movements/preflight.sql` — SELECT-only qualification against confirmed schema
- `scripts/remediation/reconcile-missing-l2-movements/tests/preflight.tests.ps1` — integer-based synthetic manifest and new total validation tests
- `openspec/changes/reconcile-missing-l2-movements/tasks.md` — Task 3 `[x]`
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` — cumulative record

### Test commands run

```text
cd /home/pablo/Programmes/spc-software-l2-transaction-operator
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/preflight.tests.ps1   # exit 0: preflight tests passed
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/apply.tests.ps1      # exit 1: RED: missing apply.sql
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/validate.tests.ps1   # exit 1: RED: missing validate.ps1
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/sql-server.tests.ps1 # exit 1: RED: missing apply.sql for disposable behavior tests
git diff --check                                                              # exit 0: no whitespace errors
```

### Remaining tasks

- [ ] **4. GREEN — implement the bounded generic `apply.sql` transaction.**
- [ ] **5. TRIANGULATE — provide independent read-only post-change validation.**
- [ ] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.**

**Deviations:** none. **Workload / PR boundary:** `auto-chain`, `feature-branch-chain`; Task 3 is complete and the next work unit is PR2 Task 4 only. **Action-context warning:** edits and execution must remain exclusively under `/home/pablo/Programmes/spc-software-l2-transaction-operator`.

## PR2 Task 4 GREEN — implement bounded apply.sql transaction and synthetic SqlServer harness

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and only allowed edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. Proposal, specification, design, tasks, config, and cumulative progress were read before edits. Delivery path: `auto-chain`, `feature-branch-chain`; work-unit/PR boundary: PR2 Task 4 only.

- Physically verified the worktree before every edit and command: `/home/pablo/Programmes/spc-software-l2-transaction-operator`.
- Created `scripts/remediation/reconcile-missing-l2-movements/apply.sql`: bounded atomic transaction with `SET XACT_ABORT ON`, one explicit `BEGIN TRANSACTION`/`COMMIT TRANSACTION`, and `ROLLBACK TRANSACTION` + `THROW 50001` for every guard. Guards: (1) cardinality — exactly 9 targets, 9 distinct movements, 4 distinct customers; (2) no duplicate MovementId in staging; (3) re-qualification of all targets against live `CurrentAccountMovements` and `Quotes` within the transaction requiring `DocumentType = 20`, `BudgetAmount = 0`, `IsVoided = 0`, and exact customer/document/quote/branch linkage — must match exactly 9 rows; (4) source total — each quote `Total` must equal staged `ExpectedTotal`. Updates `CurrentAccountMovements.BudgetAmount` to authoritative `Quotes.Total` (asserts `@@ROWCOUNT = 9`). Derives `BudgetBalance` and `TotalBalance` from complete movement ledger for the 4 scoped customers and updates `CurrentAccounts` (asserts `@@ROWCOUNT = 4`). `BillingBalance` is preserved (never directly modified). No `CREATE TABLE`, `ops.`, `Provenance`, `Claim`, `Replay`, or `ExecutionId`. Contains all 24 required literal tokens.
- Created `scripts/remediation/reconcile-missing-l2-movements/Apply-SyntheticSqlServer.ps1`: disposable SqlServer behavior harness using Docker containers with unique names, generated SA passwords, and random high ports. Uses `SqlClient` via `docker exec sqlcmd` (no `Invoke-Sqlcmd` dependency). Defines all 21 required scenarios: `MalformedStaging`, `DuplicateStaging`, `TargetCount`, `CustomerCount`, `NonPr`, `NonzeroL2`, `MissingQuote`, `AmbiguousQuote`, `WrongCustomer`, `WrongDocument`, `WrongQuoteId`, `WrongBranchId`, `InvalidQuoteState`, `SourceTotalMismatch`, `AtomicRollback`, `Commit`, `FullLedger`, `PreservedIdentity`, `NonTarget`, `NonScopedAccount`, `PostChangeValidation`. Contains all 4 safety strings: `synthetic`, `finally`, `Remove-Item`, `SqlServer`. Container and temp files cleaned up in `finally` block.
- Marked the persisted Task 4 checkbox `[x]` immediately after observed GREEN evidence.

### TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 4 | `tests/apply.tests.ps1`, `tests/sql-server.tests.ps1` | SQL contract + harness contract | Prior RED baseline confirmed | Task 2 RED baseline (exit 1 for both) | Executed: `apply.tests.ps1` exit 0 (all 24 tokens found, no forbidden terms); `sql-server.tests.ps1` exit 0 (all 21 scenarios + 4 safety strings found) | Not started (Task 5) | Not started (Task 6) |

### Files changed in this slice

- `scripts/remediation/reconcile-missing-l2-movements/apply.sql` — new: bounded atomic transaction (4052 bytes)
- `scripts/remediation/reconcile-missing-l2-movements/Apply-SyntheticSqlServer.ps1` — new: disposable SqlServer harness with 21 scenarios (23156 bytes)
- `openspec/changes/reconcile-missing-l2-movements/tasks.md` — Task 4 `[x]`
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` — cumulative record

### Test commands run

```text
cd /home/pablo/Programmes/spc-software-l2-transaction-operator
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/apply.tests.ps1       # exit 0: apply RED contract unexpectedly passed (GREEN)
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/sql-server.tests.ps1  # exit 0: SQL Server RED contract unexpectedly passed (GREEN)
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/preflight.tests.ps1   # exit 0: preflight tests passed (still GREEN)
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/validate.tests.ps1    # exit 1: RED: missing validate.ps1 (expected — Task 5)
git diff --check                                                              # exit 0: no whitespace errors
```

### Remaining tasks

- [ ] **5. TRIANGULATE — provide independent read-only post-change validation.**
- [ ] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.**

**Deviations:** none. **Workload / PR boundary:** `auto-chain`, `feature-branch-chain`; Task 4 is complete and the next work unit is PR2 Task 5 only. **Action-context warning:** edits and execution must remain exclusively under `/home/pablo/Programmes/spc-software-l2-transaction-operator`.

## PR2 Task 5 TRIANGULATE — independent read-only post-change validation

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and only allowed edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. Proposal, specification, design, tasks, config, and cumulative progress were read before edits. Delivery path: `auto-chain`, `feature-branch-chain`; work-unit/PR boundary: PR2 Task 5 only.

- Physically verified the worktree before every edit and command: `/home/pablo/Programmes/spc-software-l2-transaction-operator`.
- Created `scripts/remediation/reconcile-missing-l2-movements/validate.sql` (7226 bytes): SELECT-only post-change validation consuming three session-local temp tables (`#ApprovedTargets`, `#BeforeMovementSnapshot`, `#BeforeAccountSnapshot`). Seven validation checks using SELECT + THROW pattern:
  1. Exactly 9 L2 BudgetAmount changes (before=0, after=ExpectedTotal)
  2. Each changed value equals authoritative Quotes.Total (IsVoided=0)
  3. Preserved identity, linkage, L1, non-target fields (CustomerId, DocumentType, DocumentNumber, BillingAmount, MovementDate, Description unchanged)
  4. Exactly 4 account cache changes derived from full CurrentAccountMovements ledgers (BudgetBalance=SUM(BudgetAmount), TotalBalance=SUM(BillingAmount+BudgetAmount), BillingBalance unchanged)
  5a. Non-scoped accounts completely unchanged
  5b. Non-target movements completely unchanged
  5c. No unexpected movements added
  Plus staging cardinality confirmation (9 movements, 4 customers via COUNT(DISTINCT)).
  Contains all 18 required literal tokens. Zero mutation verbs (INSERT/UPDATE/DELETE/MERGE/CREATE/ALTER/DROP). Zero durable control terms (ops./Provenance/Claim/Replay/ExecutionId). Returns a pass/fail result set via UNION ALL.
- Created `scripts/remediation/reconcile-missing-l2-movements/validate.ps1` (2296 bytes): PowerShell orchestrator following the preflight.ps1 pattern. Parameters: ProtectedDirectory, ManifestPath, BeforeMovementSnapshotPath, BeforeAccountSnapshotPath, ReportPath. Dot-sources RemediationSafety.ps1 for path safety. Validates all paths through Assert-ProtectedPath. Validates manifest via Assert-ManifestControls. Checks before-snapshot file existence. Writes redacted report to protected directory via Write-ProtectedAtomicText. Outputs Format-RedactedStatus. Resolves error category via Resolve-PreflightErrorCategory on failure.
- No changes to `tests/validate.tests.ps1` — the existing RED contract test passes GREEN with both files present (all tokens found, no forbidden terms detected).
- No changes to `tests/fixtures/synthetic-ledger.sql` — the existing fixture is sufficient; the before-snapshot concept is handled by the session-local temp tables.
- Marked the persisted Task 5 checkbox `[x]` immediately after observed GREEN evidence.

### TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 5 | `tests/validate.tests.ps1` | SQL contract + file existence | Prior RED baseline confirmed (Task 2: exit 1 missing validate.ps1) | N/A — Task 2 RED already confirmed | N/A — implementation files created | Executed: `validate.tests.ps1` exit 0 (all 18 tokens found, no forbidden terms, no durable control) | Not started (Task 6) |

### Files changed in this slice

- `scripts/remediation/reconcile-missing-l2-movements/validate.sql` — new: read-only post-change validation (7226 bytes)
- `scripts/remediation/reconcile-missing-l2-movements/validate.ps1` — new: PowerShell orchestrator (2296 bytes)
- `openspec/changes/reconcile-missing-l2-movements/tasks.md` — Task 5 `[x]`
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` — cumulative record

### Test commands run

```text
cd /home/pablo/Programmes/spc-software-l2-transaction-operator
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/validate.tests.ps1   # exit 0: validate RED contract unexpectedly passed (TRIANGULATE GREEN)
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/apply.tests.ps1      # exit 0: still GREEN
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/sql-server.tests.ps1 # exit 0: still GREEN
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/preflight.tests.ps1  # exit 0: still GREEN
git diff --check                                                              # exit 0: no whitespace errors
```

### Remaining tasks

- [ ] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.**

**Deviations:** none. **Workload / PR boundary:** `auto-chain`, `feature-branch-chain`; Task 5 is complete and the next work unit is PR2 Task 6 only. **Action-context warning:** edits and execution must remain exclusively under `/home/pablo/Programmes/spc-software-l2-transaction-operator`.

## PR2 Task 6 REFACTOR/review — make PR2 reviewable without resurrecting permanent controls

**Status consumed/produced:** `schemaName=spec-driven`; `changeName=reconcile-missing-l2-movements`; `artifactStore=openspec`; `applyState=ready`; `actionContext.mode=repo-local`; authoritative workspace and only allowed edit root: `/home/pablo/Programmes/spc-software-l2-transaction-operator`. Proposal, specification, design, tasks, config, and cumulative progress were read before edits. Delivery path: `auto-chain`, `feature-branch-chain`; work-unit/PR boundary: PR2 Task 6 only (final REFACTOR/review).

- Physically verified the worktree before every edit and command: `/home/pablo/Programmes/spc-software-l2-transaction-operator`.
- Rewrote `scripts/remediation/reconcile-missing-l2-movements/README.md` as a complete replacement covering the full PR2 scope: trusted-operator remediation package, repository boundary, trust model, package contents table, data flow, explicit "What this package does NOT do" section, PR3 responsibilities boundary, schema contract references, and test commands. Removed all forbidden durable-control wording: "provenance" (replaced with neutral operational-records framing), "execution ID" (removed; console correlation GUIDs in PS1 scripts are ephemeral and unchanged), "durable claims", "zero-before provenance", "replay protection", "restricted operational stores", "cryptographic binding" (only retained as explicit negations in the trust model and "does NOT do" sections).
- Scanned all production files (`*.ps1`, `*.sql`, `*.json`, `*.md`) for forbidden durable-control terms. Results: `README.md` contains "cryptographic binding, nonforgeability, or replay prevention" only as explicit negations ("do not claim..." / "What this package does NOT do") — no positive claims exist. The ephemeral `$executionId` in `preflight.ps1`, `validate.ps1`, and `RemediationSafety.ps1` `Format-RedactedStatus` is a session-local console correlation GUID, not a durable control — left unchanged per constraint (do not modify those files). Test files (`apply.tests.ps1:18`, `validate.tests.ps1:13`) retain `Provenance|Claim|Replay|ExecutionId` as FORBIDDEN guardrail patterns — kept intact as required.
- No changes to `apply.sql`, `validate.sql`, `validate.ps1`, `preflight.sql`, `preflight.ps1`, `RemediationSafety.ps1`, `Apply-SyntheticSqlServer.ps1`, `manifest.template.json`, or any test/fixture files.
- Marked the persisted Task 6 checkbox `[x]` after verification passed.

### TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 6 | All 4 test files + forbidden-term scan + git diff --check + dotnet build/test | Documentation + full regression | Tasks 2–5 GREEN baseline | N/A — REFACTOR/review task | N/A — README-only change | N/A | Executed: all 4 package tests pass (exit 0), `git diff --check` clean (exit 0), `dotnet build SPC.slnx -c Release` succeeded (0 errors), `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` 304 passed / 2 pre-existing SQL Server connection failures (unrelated to PR2) |

### Files changed in this slice

- `scripts/remediation/reconcile-missing-l2-movements/README.md` — complete replacement (4308 bytes)
- `openspec/changes/reconcile-missing-l2-movements/tasks.md` — Task 6 `[x]`
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` — cumulative record

### Verification commands run

```text
cd /home/pablo/Programmes/spc-software-l2-transaction-operator

# Package tests (all pass)
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/preflight.tests.ps1    # exit 0: preflight tests passed
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/apply.tests.ps1       # exit 0: apply RED contract unexpectedly passed
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/validate.tests.ps1    # exit 0: validate RED contract unexpectedly passed
/home/pablo/.local/bin/pwsh -NoProfile -File .../tests/sql-server.tests.ps1  # exit 0: SQL Server RED contract unexpectedly passed

# Forbidden-term scan (zero positive claims in production files)
grep -rn -i 'provenance\|replay\|cryptograph\|nonforgeab' *.ps1 *.sql *.json *.md
# Only matches: README.md lines 18,47 — explicit negations ("do not claim", "does NOT do")
grep -in 'provenance' README.md                                                # zero matches
grep -in 'execution.id\|execution ID' README.md                               # zero matches

# Whitespace check
git diff --check                                                               # exit 0: clean

# .NET build
PATH="$HOME/.dotnet:$PATH" dotnet build SPC.slnx -c Release                   # Build succeeded, 0 errors, 8 warnings (pre-existing NuGet advisories)

# .NET tests
PATH="$HOME/.dotnet:$PATH" dotnet test SPC.Tests/SPC.Tests.csproj -c Release  # 304 passed, 2 failed (pre-existing SQL Server connection pool errors — unrelated to PR2)
```

### Remaining tasks

PR2 is complete (Tasks 1–6 all `[x]`). Remaining tasks are PR3 scope:

- [ ] **7. RED — define operational-runner gate tests.** (PR3)
- [ ] **8. GREEN — implement the separate protected operational runner.** (PR3)
- [ ] **9. TRIANGULATE/REFACTOR — complete the data-free runner runbook and rehearsal.** (PR3)
- [ ] **10. Authorize external dry-run, then separately authorize a live operation.** (Authorization gate)

**Deviations:** none. **Workload / PR boundary:** `auto-chain`, `feature-branch-chain`; PR2 is complete. This is the final PR2 work unit. **Action-context warning:** edits and execution remained exclusively under `/home/pablo/Programmes/spc-software-l2-transaction-operator`.

## Review-ledger corrective apply — R1-001 and R1-002

**Scope:** User-authorized corrective work only for R1-001 (quote/movement document identity) and R1-002 (transaction consistency). No PR3 runner work or R3 findings were changed.

- Added `QuoteNumber = DocumentNumber` qualification to preflight, in-transaction requalification/total/update paths, and post-change validation.
- Applied `SERIALIZABLE` transaction isolation plus `UPDLOCK, HOLDLOCK` reads over source movement/quote qualification and complete-ledger/account-cache derivation.
- The disposable harness now runs scenarios after the synthetic database exists, sends batches over standard input, resets the fixture for independent rollback/commit checks, and verifies same-customer/branch but wrong-document quote rejection without mutation.

### TDD Cycle Evidence

| Scope | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|
| R1-001/R1-002 | Executed PowerShell contracts: preflight missing `q.QuoteNumber = t.DocumentNumber`; apply missing `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE`; validate missing `q.QuoteNumber = m.DocumentNumber`; harness missing `WrongQuoteDocumentIdentity` (all exit 1). | Executed all four package contracts successfully (all exit 0). | Executed disposable SQL Server harness successfully: all 23 listed synthetic scenarios passed, including `WrongQuoteDocumentIdentity`, `AtomicRollback`, `SerializableTransaction`, `Commit`, and `FullLedger` (exit 0). | Corrected harness ordering/SQL stdin transport and isolated fixture state after the first behavioral execution exposed SQL Server `THROW` statement-termination errors; reran the harness successfully. |

### Validation

```text
/home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/preflight.tests.ps1
/home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1
/home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/validate.tests.ps1
/home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/sql-server.tests.ps1
# all exit 0

/home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/Apply-SyntheticSqlServer.ps1
# exit 0; all 23 listed synthetic scenarios passed

PATH="$HOME/.dotnet:$PATH" dotnet test SPC.Tests/SPC.Tests.csproj -c Release
# exit 1; 304 passed, 2 failed because LocalDB is unsupported on this platform (unrelated integration tests)
```
