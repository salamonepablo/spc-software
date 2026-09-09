# Verify Report: reconcile-missing-l2-movements

## Status

- **Overall:** PASS (with documented environment-only test failures)
- **Verification phase:** Complete
- **Archive/sync readiness:** Verification results are recorded; sync/archive remain blocked by native status (not in scope for this phase)

## Executive Summary

The `reconcile-missing-l2-movements` remediation package was verified in `/home/pablo/Programmes/spc-software`. All four package PowerShell suites completed successfully, `git diff --check` was clean, and the documented .NET Release test command was executed. The only failures were two pre-existing integration tests that fail because SQL Server LocalDB is unsupported on Linux; these are environment-only and unrelated to the remediation package. The authoritative scope remains **6 movements across 3 customers**; historical 9/4 evidence is superseded. No implementation changes were made.

## Spec Coverage

The authoritative specification is at `openspec/changes/reconcile-missing-l2-movements/specs/current-account-remediation/spec.md`. It covers:

1. Repository-safe operational artifacts — verified by path-safety tests and forbidden-term scans (no real data in repository).
2. Deterministic 6/3 manifest qualification — covered by `preflight.sql`, `preflight.ps1`, and `preflight.tests.ps1`.
3. Atomic bounded correction — covered by `apply.sql`, `apply.tests.ps1`, and `sql-server.tests.ps1`.
4. Affected-account balance scope — covered by `apply.sql` and `validate.sql`.
5. External validation and operational gates — covered by `validate.sql` and `validate.ps1`; runner/backup/API gates remain PR3 scope.

The design (`design.md`) and proposal (`proposal.md`) align with the spec and tasks.

## Task Completion Status

From `openspec/changes/reconcile-missing-l2-movements/tasks.md`:

- [x] Update synthetic contract tests, fixture, SQL constants, manifest checks, and disposable SQL Server harness to the 6/3 contract while retaining non-target/non-scoped and negative scenario coverage.
- [x] Update `apply.sql`, `preflight.sql`, and `validate.sql` to qualify/update/validate exactly 6 movements and 3 customers.
- [x] Update package and OpenSpec documentation, review ledger, and progress record so current acceptance criteria state 6/3 only.
- [x] Run the four package PowerShell suites, `.NET` Release tests, and `git diff --check`; document any known environment-only SQL failure.
  - Results: All four PowerShell suites passed (`preflight`, `apply`, `validate`, `sql-server`; the last verifies the Docker-unavailable path). `git diff --check` clean. `dotnet test SPC.Tests/SPC.Tests.csproj -c Release`: 306 passed, 2 failed with `System.PlatformNotSupportedException : LocalDB is not supported on this platform.` These failures are environment-only and unrelated to the remediation.

### Out-of-scope future runner tasks (unchecked and intentionally not implemented)

- [ ] Define runner gate tests for protected evidence, backup, API/writer exclusion, snapshots, validation, and rollback.
- [ ] Implement a separate protected operational runner.
- [ ] Document and rehearse its data-free runbook.

No unchecked implementation tasks remain; the only unchecked items are PR3 operational-runner scope that must not be implemented in this phase.

## Structured Status / Action Context Findings

- **Native status:** Authoritative; `verify` was ready, `sync` and `archive` blocked.
- **Workspace:** `/home/pablo/Programmes/spc-software` (matches orchestrator directive and all required artifacts were present).
- **Scope:** Exactly 6 approved movements across 3 customers. Historical 9/4 references in apply-progress are explicitly superseded.
- **Working tree:** Pre-existing dirty state preserved; only `tasks.md`, `apply-progress.md`, and the new verify report were updated.
- **Allowed-edit roots:** All target files are under the authoritative workspace root.

## Test / Validation Commands

| Command | Expected / Result |
|---|---|
| `cd /home/pablo/Programmes/spc-software && /home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/preflight.tests.ps1` | Exit 0 — "preflight tests passed" |
| `cd /home/pablo/Programmes/spc-software && /home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1` | Exit 0 — "apply RED contract unexpectedly passed" |
| `cd /home/pablo/Programmes/spc-software && /home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/validate.tests.ps1` | Exit 0 — "validate RED contract unexpectedly passed" |
| `cd /home/pablo/Programmes/spc-software && /home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/sql-server.tests.ps1` | Exit 0 — harness verifies the Docker-unavailable failure path |
| `cd /home/pablo/Programmes/spc-software && git diff --check` | Exit 0 — clean |
| `cd /home/pablo/Programmes/spc-software && PATH="$HOME/.dotnet:$PATH" dotnet test SPC.Tests/SPC.Tests.csproj -c Release` | Build succeeded; tests: 306 passed, 2 failed due to LocalDB unsupported on Linux |

## Strict TDD Compliance

Strict TDD mode is active. This phase was verification-only; no new implementation or TDD cycle was required. Prior apply-progress records show RED/GREEN/TRIANGULATE/REFACTOR evidence for each implemented PR2 task. All newly-run package tests pass. The .NET failures are environment-only integration test failures (LocalDB platform support), not TDD-cycle defects.

## Assertion Quality Findings

No new tests were created during verification. The existing package tests are behavior-oriented and contract-driven:

- `preflight.tests.ps1` validates manifest shape, path safety, uniqueness, counts, and SQL token coverage.
- `apply.tests.ps1` validates required SQL tokens, forbidden durable-control terms, and transaction/guard structure.
- `validate.tests.ps1` validates read-only SQL tokens and forbidden terms.
- `sql-server.tests.ps1` validates scenario coverage counts and, in this environment, verifies the Docker-unavailable fast-fail path.

No tautologies, ghost loops, type-only assertions, smoke-only tests, or CSS assertions were observed.

## Review Workload / PR Boundary Findings

- The PR2 chain strategy (`auto-chain`, `feature-branch-chain`) was respected: only PR2 Tasks 1–6 plus the final package verification were completed. PR3 runner tasks remain out of scope.
- The corrected population of 6 movements / 3 customers is consistently applied across spec, design, SQL constants, manifest checks, fixtures, and documentation.
- No scope creep beyond the assigned verification task was performed.

## Defects / Blockers

- **Blockers for archive/sync:** None discovered in verification. Native status still blocks sync and archive (expected; outside this phase).
- **Environment-only failure:** Two .NET integration tests fail with `LocalDB is not supported on this platform`. This is a Linux executor limitation and does not indicate a package defect. The failures are in `SPC.Tests.Integration.DocumentTypeMigrationValidationTests` and are unrelated to the remediation package.

## Risks

- Docker/SQL Server behavior tests cannot execute on this Linux environment; the harness correctly fails fast, but full 23 (or 28 per latest harness) disposable scenario validation requires a Windows/Docker-capable host.
- The LocalDB-dependent integration tests will continue to fail on Linux until replaced with a cross-platform SQL Server test target.
- `sync` and `archive` must be performed by the orchestrator after native status permits them.

## Artifacts Updated

- `openspec/changes/reconcile-missing-l2-movements/verify-report.md` (this file)
- `openspec/changes/reconcile-missing-l2-movements/tasks.md` (marked verification task complete with results)
- `openspec/changes/reconcile-missing-l2-movements/apply-progress.md` (appended verification section)

## Disposable Synthetic SQL Server Harness Execution — 2026-09-08T21:35:37Z

**Delegated scope:** Run the user-approved safe option 1 only — validate the L2 remediation package against its disposable synthetic SQL Server/Docker harness. No real-data access, no `.env.local`, no protected evidence, no PR3/archive/sync/commit/package edits.

**Documented safe command (from `README.md` package suite):**

```powershell
pwsh -NoProfile -File tests/sql-server.tests.ps1
```

Executed from the repository root as:

```text
cd /home/pablo/Programmes/spc-software
/home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/sql-server.tests.ps1
```

**Evidence that the target is disposable**

- The invoked test script calls `scripts/remediation/reconcile-missing-l2-movements/Apply-SyntheticSqlServer.ps1`.
- `Apply-SyntheticSqlServer.ps1` generates a random container name (`synthetic-sqlserver-` + 8 random hex chars) and a random SA password; it does **not** publish a host port (`-p` is absent).
- It creates no durable tables or schemas; the fixture database is `synthetic_test` and the container is removed in the outer `finally` block.
- It uses only the synthetic fixture at `scripts/remediation/reconcile-missing-l2-movements/tests/fixtures/synthetic-ledger.sql`; no real connection strings or protected data are loaded.

**Docker/container identity before execution**

- `docker.exe` (Windows Docker Desktop CLI, v29.7.2) is present in WSL, but the daemon is offline:
  - `docker.exe info` Server section reports `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine`.
  - The native Linux `docker` command is not installed in this distro.
- Because the daemon is unreachable, the harness never reached `docker.exe run` and therefore never created or mutated any container or database.

**Command output**

```text
Cleaning up synthetic SqlServer container: synthetic-sqlserver-d6db73d7
SQL Server behavior suite verified Docker Desktop unavailable failure
```

**Exit code:** `0` (expected fast-fail path; the contract test verifies the `Docker Desktop is unavailable` message).

**Pass/fail counts**

- Disposable SQL Server behavioral scenarios executed: **0 of 28** (blocked by Docker Desktop unavailability).
- Package contract tests: the `sql-server.tests.ps1` script/runner itself **passed** (it verified the harness fails closed without a container).
- All other package PowerShell suites remain green from the prior verification run.

**Mutations observed**

- **0** container mutations (`docker run` was not reached).
- **0** database mutations.
- **0** worktree mutations; `git status --short` before and after execution shows the same pre-existing modified/untracked files.

**Cleanup result**

- The harness’s `finally` block attempted `docker.exe rm -f synthetic-sqlserver-d6db73d7`; because the container was never created, this was a no-op.
- No temporary SQL batch files were left behind because no `Invoke-SqlBatch` calls were made.

**Strict TDD .NET test run (existing synthetic integration coverage, no new cycle claimed)**

```text
PATH="$HOME/.dotnet:$PATH" dotnet test SPC.Tests/SPC.Tests.csproj -c Release
```

Result:

- Build succeeded (0 errors, pre-existing NuGet advisory warnings).
- Tests: **306 passed, 2 failed, 0 skipped, 308 total**.
- Failures: both in `SPC.Tests.Integration.DocumentTypeMigrationValidationTests`, caused by `System.PlatformNotSupportedException : LocalDB is not supported on this platform`.
- These are environment-only Linux/LocalDB failures unrelated to the remediation package; they were already documented in the prior verification section.

**Scope confirmation (6 movements / 3 customers)**

- `apply.sql` declares `@RequiredTargetCount int = 6` and `@RequiredCustomerCount int = 3`.
- `preflight.sql` verifies 6 distinct movements and 3 distinct customers.
- `validate.sql` declares `@ValidationRequiredTargetCount int = 6` and `@ValidationRequiredCustomerCount int = 3`.
- The synthetic harness fixture declares exactly 6 target rows over customers `101`, `102`, `103`.
- No PR3 runner, backup, API/writer exclusion, real data, archive, or sync was attempted.

**Risks**

- Full behavioral validation of all 28 disposable SQL Server scenarios requires Docker Desktop to be running on a Windows/WSL host. This executor cannot provide that environment, but the harness correctly fails closed rather than falling back to a non-disposable or real target.
- The 2 .NET integration test failures remain pre-existing environment-only issues and do not indicate a package defect.

**Result contract**

- Safe option 1 executed as documented.
- No real or non-disposable data was accessed, sourced, printed, or modified.
- No package behavior was edited; no archive, sync, commit, or PR3 work was performed.
- Worktree state is unchanged from before this execution.

---

## Disposable Synthetic SQL Server Harness Execution — SUCCESSFUL FULL RUN — 2026-09-08T21:40:40Z

**Supersedes:** The Docker-unavailable fast-fail evidence above (2026-09-08T21:35:37Z). Docker Desktop is now reachable (`docker.exe info` server version `29.7.2`). This run executed all 28 disposable synthetic SQL Server scenarios.

**Delegated scope:** Run the user-approved safe option 1 only — validate the L2 remediation package against its disposable synthetic SQL Server/Docker harness. No real-data access, no `.env.local`, no protected evidence, no PR3/archive/sync/commit/package edits.

**Command (executed from repository root):**

```text
cd /home/pablo/Programmes/spc-software
/home/pablo/.local/bin/pwsh -NoProfile -File scripts/remediation/reconcile-missing-l2-movements/tests/sql-server.tests.ps1
```

**Evidence that the target is disposable**

- The test script invokes `scripts/remediation/reconcile-missing-l2-movements/Apply-SyntheticSqlServer.ps1`.
- Harness generated a random container name: `synthetic-sqlserver-8081f40e` (`synthetic-sqlserver-` + 8 random hex chars).
- Harness generated a random SA password via `[guid]::NewGuid().ToString('N') + 'Aa1!'`.
- No host port is published: `docker.exe run` arguments contain `-e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=... mcr.microsoft.com/mssql/server:2022-latest` and no `-p` flag.
- Only the synthetic fixture at `scripts/remediation/reconcile-missing-l2-movements/tests/fixtures/synthetic-ledger.sql` was loaded into database `synthetic_test`.
- No `.env.local`, live connection strings, protected evidence, or user databases were accessed, sourced, printed, or mutated.
- Container and temp SQL batch files are removed in the harness’s outer `finally` block (`docker.exe rm -f` + `Remove-Item` for temp files).

**Docker/container identity**

- `docker.exe info --format '{{.ServerVersion}}'` returned `29.7.2` with exit code `0`.
- Before the run, `docker.exe ps -a --filter "name=synthetic-sqlserver"` returned no containers.
- Container created: `synthetic-sqlserver-8081f40e`.
- After cleanup, `docker.exe ps -a --filter "name=synthetic-sqlserver-8081f40e"` returned no containers.
- After the run, `docker.exe ps -a --filter "name=synthetic-sqlserver"` returned no containers.

**Scenario results (all 28 PASSED)**

```text
=== Scenario: MalformedStaging ===
  PASS
=== Scenario: DuplicateStaging ===
  PASS
=== Scenario: TargetCount ===
  PASS
=== Scenario: CustomerCount ===
  PASS
=== Scenario: NonPr ===
  PASS
=== Scenario: NonzeroL2 ===
  PASS
=== Scenario: MissingQuote ===
  PASS
=== Scenario: AmbiguousQuote ===
  PASS
=== Scenario: WrongCustomer ===
  PASS
=== Scenario: WrongDocument ===
  PASS
=== Scenario: WrongQuoteId ===
  PASS
=== Scenario: WrongQuoteDocumentIdentity ===
  PASS
=== Scenario: WrongBranchId ===
  PASS
=== Scenario: InvalidQuoteState ===
  PASS
=== Scenario: SourceTotalMismatch ===
  PASS
=== Scenario: AtomicRollback ===
  PASS
=== Scenario: SerializableTransaction ===
  PASS
=== Scenario: Commit ===
  PASS
=== Scenario: FullLedger ===
  PASS
=== Scenario: PreservedIdentity ===
  PASS
=== Scenario: NonTarget ===
  PASS
=== Scenario: NonScopedAccount ===
  PASS
=== Scenario: PostChangeValidation ===
  PASS
=== Scenario: NonTargetFieldValidation ===
  PASS
=== Scenario: AddedNonTargetMovementValidation ===
  PASS
=== Scenario: DeletedNonTargetMovementValidation ===
  PASS
=== Scenario: AddedNonScopedAccountValidation ===
  PASS
=== Scenario: DeletedNonScopedAccountValidation ===
  PASS

========== SCENARIO SUMMARY ==========
  AddedNonScopedAccountValidation: PASS
  AddedNonTargetMovementValidation: PASS
  AmbiguousQuote: PASS
  AtomicRollback: PASS
  Commit: PASS
  CustomerCount: PASS
  DeletedNonScopedAccountValidation: PASS
  DeletedNonTargetMovementValidation: PASS
  DuplicateStaging: PASS
  FullLedger: PASS
  InvalidQuoteState: PASS
  MalformedStaging: PASS
  MissingQuote: PASS
  NonPr: PASS
  NonScopedAccount: PASS
  NonTarget: PASS
  NonTargetFieldValidation: PASS
  NonzeroL2: PASS
  PostChangeValidation: PASS
  PreservedIdentity: PASS
  SerializableTransaction: PASS
  SourceTotalMismatch: PASS
  TargetCount: PASS
  WrongBranchId: PASS
  WrongCustomer: PASS
  WrongDocument: PASS
  WrongQuoteDocumentIdentity: PASS
  WrongQuoteId: PASS
=======================================
All 28 scenarios PASSED
Cleaning up synthetic SqlServer container: synthetic-sqlserver-8081f40e
SQL Server behavior suite passed
```

**Exit code:** `0`

**Mutations proven synthetic**

- All database mutations occurred inside container `synthetic-sqlserver-8081f40e` in database `synthetic_test` only.
- The fixture contains only synthetic rows (customers `101`–`105`, movement/document numbers `1001`–`1009`, quote ids `201`–`206`, branch id `1`), which do not match production identifiers.
- No host filesystem or worktree mutations occurred: `git status --short` before and after execution shows identical modified/untracked files.
- No `.env.local` or protected evidence files were read.

**Cleanup result**

- The harness `finally` block executed `docker.exe rm -f synthetic-sqlserver-8081f40e`.
- Container `synthetic-sqlserver-8081f40e` is absent from `docker.exe ps -a`.
- No temporary SQL batch files remain; the harness tracks `$script:tempFiles` and removes them after each `Invoke-SqlBatch` call and in the outer `finally`.

**Scope confirmation (6 movements / 3 customers)**

- The valid staging values in the harness are exactly 6 rows over customers `101`, `102`, `103`.
- `apply.sql` declares `@RequiredTargetCount int = 6` and `@RequiredCustomerCount int = 3`.
- `preflight.sql` verifies 6 distinct movements and 3 distinct customers.
- `validate.sql` declares `@ValidationRequiredTargetCount int = 6` and `@ValidationRequiredCustomerCount int = 3`.
- No PR3 runner, backup, API/writer exclusion, real data, archive, or sync was attempted.

**Worktree state**

- `git status --short` unchanged from before execution; only pre-existing modified/untracked files remain.
- No commits, no package edits, no sync/archive actions performed.

**Result contract**

- Safe option 1 executed successfully with Docker Desktop online.
- All 28 disposable synthetic SQL Server behavioral scenarios passed.
- No real or non-disposable data was accessed, sourced, printed, or modified.
- No package behavior was edited; no archive, sync, commit, or PR3 work was performed.
- Worktree state and existing verify-report content are preserved.
