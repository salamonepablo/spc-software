# Implementation Tasks: Reconcile Missing L2 Quote Movements

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 850–1,150 (lean SQL package, disposable actual-schema SQL Server tests, and separate runner) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 complete → PR 2 lean data-free SQL package/tests → PR 3 runner, evidence, and rollback integration |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

> **Non-negotiable boundary:** repository artifacts are generic code, data-free templates, and explicitly synthetic tests only. Never commit, create, copy, or emit real manifests, identifiers, amounts, approvals, backup metadata/files, snapshots, hashes, reports, console captures, or rollback evidence under the canonical repository root. All real operational inputs and outputs belong in an explicit operator-controlled protected directory outside it.

## Scope and chain boundaries

- Change only `scripts/remediation/reconcile-missing-l2-movements/` and its synthetic harness/runbook. Do not change `SPC.*`, EF models/migrations, API/UI contracts, L2 configuration, or `scripts/run-api-local.sh`.
- PR 2 is a **clean replacement** for the prior over-scoped plan: no durable approval/claim/provenance/replay-control table, schema, migration, database record, or cryptographic/nonforgeability assertion. It does not implement backup, API lifecycle, writer exclusion, evidence capture, or rollback orchestration.
- The lean package receives externally approved target data only through client-bound session-local staging; it neither discovers a population nor retains the manifest in the repository or target database. The trusted operator/evidence process is external.
- Tests use only disposable SQL Server resources and synthetic data. Before writing fixtures/joins, confirm the actual names, types, document semantics, and linkage from `SPC.Shared/Models/CurrentAccount.cs`, `SPC.Shared/Models/Quote.cs`, `SPC.API/Data/SPCDbContext.cs`, and the applicable `SPC.API/Migrations/*.cs`; record generic findings in the package README, never live values.

## PR 1 — completed repository-safe foundation

- [x] **1. Provide the data-free package and read-only preflight foundation.** `manifest.template.json`, `README.md`, `RemediationSafety.ps1`, `preflight.ps1`, `preflight.sql`, and synthetic path/preflight tests establish explicit protected-directory handling and a SELECT-only baseline. **Verification:** existing synthetic path/redaction checks pass and repository review finds no real artifacts. **Rollback:** remove only generic artifacts and disposable synthetic output.

## PR 2 — clean lean SQL package and disposable actual-schema tests

- [x] **2. RED — replace superseded PR2 contracts with trusted-operator package tests.** Amend `scripts/remediation/reconcile-missing-l2-movements/tests/apply.tests.ps1`, `preflight.tests.ps1`, `validate.tests.ps1`, `sql-server.tests.ps1`, and `tests/fixtures/*.sql`. Build the disposable SQL Server fixture from the confirmed application schema contract and use nine synthetic `PR`/zero-L2 targets across four synthetic customers. Specify failures for unsafe protected paths, malformed/duplicate staging, counts other than 9/4, non-`PR`, nonzero L2, missing/ambiguous or wrong customer/document/quote/branch linkage, invalid quote state, and source-total mismatch. Specify commit, full-ledger derived balance, preserved-field, and rollback assertions. **Verification:** tests are RED against the clean PR2 surface, use no real connection/data, and dispose database/container/output. **Rollback:** delete only disposable synthetic resources.

- [x] **3. GREEN — align data-free manifest and read-only preflight to the amended contract.** Amend `manifest.template.json`, `RemediationSafety.ps1`, `preflight.ps1`, `preflight.sql`, and associated tests. Require an explicit protected directory; canonicalize repository, input, output, and derived paths before database access; reject repository containment, aliases/reparse points where supported, absent/inaccessible storage, and unsafe containment. Validate manifest shape, required external-control presence, and staged identities/expected authoritative totals without persisting a manifest, approval, hash, claim, provenance, or replay record. Make `preflight.sql` parameterized and SELECT-only against the confirmed schema, qualifying exactly nine distinct movements/four customers and exactly one authoritative linked quote per target. **Verification:** valid synthetic staging qualifies; every shape/path/count/linkage/state/total failure fails before mutation; output is redacted and protected-only. **Rollback:** read-only; remove disposable output.

- [x] **4. GREEN — implement the bounded generic `apply.sql` transaction.** Create/amend `apply.sql` plus only the session-local staging invocation required by the synthetic harness. Use parameterized client-bound staging, `XACT_ABORT`, one explicit transaction, and appropriate locks; requalify the same 9/4 bounded scope in-transaction. Update only qualifying `CurrentAccountMovements.BudgetAmount` values to their uniquely linked authoritative `Quotes.Total`, assert exactly nine movement updates, then derive complete-ledger L2 and L1+L2 totals and update only `CurrentAccounts.BudgetBalance`/`TotalBalance` for the four staged customers, asserting exactly four accounts. Abort and roll back on every guard, row-count, preservation, or derived-result failure. Do not create durable tables/schemas/records or perform any backup, service, writer, approval, or evidence action. **Verification:** actual disposable SQL Server behavior tests prove atomic commit and no partial movement/account mutation for each guard failure; prove L1, movement identity/date/description/linkage, non-target movements, non-scoped accounts, and `BillingBalance` are unchanged. **Rollback:** transaction rollback only; no inverse update.

- [x] **5. TRIANGULATE — provide independent read-only post-change validation.** Amend `validate.sql`, `validate.ps1`, their tests, and synthetic fixtures so validation consumes externally supplied/session-bound approved target and before-snapshot data but stores no evidence in-repo or in the target database. Verify exactly nine and only nine L2 changes, each authoritative source total, preserved identity/linkage/L1/non-target fields, and only four allowed account L2/total cache changes derived from full ledgers. **Verification:** actual disposable SQL Server tests pass for the valid synthetic post-state and fail for altered L1/linkage, source mismatch, out-of-scope change, or incorrect cache derivation; the scripts remain read-only. **Rollback:** read-only; dispose synthetic resources.

- [x] **6. REFACTOR/review — make PR2 reviewable without resurrecting permanent controls.** Consolidate synthetic SQL/staging assertions and update `README.md` with the generic external-directory, trusted-operator, session-local-staging, SQL transaction, validation, and explicit PR3-responsibility boundary. Remove any prior PR2 wording/code/test expectation for execution IDs, durable claims, provenance, zero-before provenance, replay protection, restricted operational stores, or cryptographic binding. **Verification:** run the package PowerShell/disposable-SQL-Server suite, `git diff --check`, `dotnet build SPC.slnx -c Release`, and `dotnet test SPC.Tests/SPC.Tests.csproj -c Release`; inspect tracked changes for sensitive data and forbidden durable-control artifacts. **Rollback:** reject PR2 until tests and repository-safety review pass.

## PR 3 — runner backup/API/writer-exclusion/evidence/rollback implementation

- [ ] **7. RED — define operational-runner gate tests.** Add `tests/launcher-gates.tests.ps1` for planned `dry-run.ps1`, `execute.ps1`, and `rollback.ps1`, using mocks/disposable synthetic resources only. Cover required protected directory/approval/evidence readiness, preflight failure, verified-backup failure, running API or writer-exclusion failure, snapshot/report failure, validation failure, and post-commit manual-review failure. **Verification:** each failed gate prevents apply/restart as applicable and retains no repository output. **Rollback:** remove external synthetic material.

- [ ] **8. GREEN — implement the separate protected operational runner.** Add `dry-run.ps1`, `execute.ps1`, and `rollback.ps1`. Before apply, require explicit safe protected storage, successful preflight, recoverable `sql-spc` backup creation/verification, API downtime/writer exclusion, protected before snapshot, and trusted-operator go/no-go. Capture after snapshots, reports, hashes, approvals, and rollback records only externally; invoke lean `apply.sql` only after gates pass. On post-commit validation/manual-review failure, stop the API, restore the verified protected backup, verify the protected original snapshot/database state, and retain the backup until explicit user-authorized deletion. **Verification:** launcher tests demonstrate gates, protected-only evidence, restore flow, and no inverse update. **Rollback:** use the verified protected backup; never auto-delete it.

- [ ] **9. TRIANGULATE/REFACTOR — complete the data-free runner runbook and rehearsal.** Update `README.md` and add blank checklist templates only if necessary. Document generic prerequisites, external-only artifact inventory, go/no-go order, backup/API/writer controls, validation/manual review of 9 movements and 4 accounts, restart through unchanged `scripts/run-api-local.sh`, and rollback decision tree. **Verification:** an all-synthetic tabletop rehearsal completes with no repository evidence; package and .NET validation commands pass. **Rollback:** documentation-only.

## Separate authorization gate — not PR completion

- [ ] **10. Authorize external dry-run, then separately authorize a live operation.** DBA/security/business/application/trusted-operator reviewers inspect real artifacts only in protected storage and rehearse against an approved disposable restore. A live run requires the PR3 gates, guarded transaction, external post-validation, manual review, and retained protected evidence. **Go/no-go:** any failed guard/review is no-go; post-commit/manual-review failure restores the verified protected backup before restart.

## Completion evidence

- [ ] PR2 has RED → GREEN → TRIANGULATE → REFACTOR evidence from actual-schema, disposable SQL Server tests for the lean, data-free, bounded transaction and read-only validation.
- [ ] PR2 has no permanent provenance/claim/replay-control implementation or requirement.
- [ ] PR3 has runner evidence for backup, API downtime/writer exclusion, protected external evidence, restart, and rollback; real evidence is never a PR artifact.
- [ ] Repository review confirms no sensitive data and no application architecture/configuration change.
