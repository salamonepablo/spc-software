# Implementation Tasks: Reconcile Missing L2 Quote Movements

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 850–1,150 (generic SQL/PowerShell, synthetic fixtures, and runbook; no application code) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1: templates, path gates, preflight, synthetic tests → PR 2: guarded apply, validation, synthetic transaction tests → PR 3: dry-run/rollback launchers and data-free runbook |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

> **Hard boundary:** this public repository may contain only generic code, documentation, data-free templates, and explicitly synthetic fixtures. Do not add a real manifest, client/document identifiers, financial values or totals, approval/sign-off, backup or restore material, live-data hash/checksum, report, console capture, provenance, or rollback evidence anywhere below the repository root. Every real input and output belongs in an explicit operator-controlled protected directory outside the canonical repository root. Live operation is separately gated after a successful dry-run.

## Scope and delivery boundaries

- Add only `scripts/remediation/reconcile-missing-l2-movements/`; do not modify `SPC.*`, configuration, API/UI behavior, EF migrations, or `scripts/run-api-local.sh`.
- The eventual SQL Server target and its schema/backup procedure are discovered read-only by authorized operators. Do not commit discovery output, connection details, or live-schema reports.
- Every launcher must require `-ProtectedDirectory` and explicit real artifact paths beneath it. Before a database connection, canonicalize repository, protected, and derived artifact paths (including symlink/junction/reparse-point resolution); reject the repository root and descendants, aliases/traversal, missing/inaccessible/unsafe paths, and writes outside protected storage. No repository fallback exists.
- Repository-visible console/log output is redacted to stage, opaque execution ID, and non-sensitive error category. Detailed output is written only to protected external storage; tests may assert this using synthetic values.

## PR 1 — repository-safe contract, RED preflight tests, GREEN qualification

- [x] **1. Define the generic package contract and data-free manifest template.** Add `scripts/remediation/reconcile-missing-l2-movements/manifest.template.json` with placeholder tokens and schema/control shape only, plus `README.md` sections that distinguish in-repository templates/synthetic fixtures from externally protected real artifacts. Name required external artifact categories (approved manifest, approvals, backup/restore material, reports, provenance, and review evidence) without supplying real paths, samples, identifiers, totals, hashes, or sign-offs. **Verify:** repository review confirms no `manifest.json`, evidence directory, live-data checksum, approval record, or captured output is added. **Rollback:** remove only generic additions; no operational state exists.

- [x] **2. RED — add synthetic fixture and path/redaction test harness.** Add `tests/fixtures/*.sql` containing clearly labeled synthetic ledger/quote/account cases, and `tests/path-safety.tests.ps1` plus `tests/preflight.tests.ps1`. Cover repository-root/descendant paths, relative aliases, symlinks/junctions/reparse points, missing/non-writable protected storage, unsafe derived temporary/report/log paths, and redacted console output. Add synthetic qualification cases for duplicate/missing/ambiguous quote, wrong type, nonzero L2, malformed/unapproved manifest control, count drift, missing account, and non-target scope. **Verify:** RED tests fail before helpers/preflight exist and never connect to a live database or persist output below the repository root. **Rollback:** delete disposable synthetic test databases/temporary directories outside the repository.

- [x] **3. GREEN — implement shared protected-path and redacted-observability helpers.** Add the smallest PowerShell helper(s) under the package and wire the preflight entry point to require explicit external paths, canonical containment checks, protected-directory access tests, protected-only atomic output creation, and redacted failures. It must reject before reading a real manifest or opening a database connection. **Verify:** path-safety tests pass, including alias/reparse-point cases and proof that sensitive synthetic values are not echoed. **Rollback:** no database mutation; remove only protected test output.

- [x] **4. GREEN — implement parameterized, SELECT-only preflight.** Add `preflight.sql` and a generic launcher/adapter that loads only the external approved manifest into session-local/in-memory staging, validates its template schema and fixed controls (nine movements, four customers, nine unique quotes), and performs no DML/DDL. Validate target type, initial zero L2, approval/control integrity, one authoritative quote by approved customer/document linkage across all branches, account existence, and complete-ledger balance inputs; report other candidates only externally and never expand scope. **Verify:** valid synthetic fixture passes; each negative fixture fails with no data change; all detailed synthetic reports are outside the repository. **Rollback:** read-only.

- [x] **5. TRIANGULATE/REFACTOR — stabilize PR 1 interfaces.** Consolidate manifest parsing, decimal handling, SQL-client parameter binding, and synthetic snapshot assertions without introducing production defaults or embedding real controls. Apply `Set-StrictMode` and fail-fast behavior. **Verify:** independently alter each synthetic manifest control/quote condition and confirm fail-closed behavior; run `git diff --check`. **Rollback:** no live state.

## PR 2 — RED/GREEN guarded correction and independent validation

- [ ] **6. RED — extend synthetic transaction tests.** In `tests/apply.tests.ps1` and synthetic fixtures, specify baseline snapshots and expected rollback for guard failure, quote/control drift, missing account, unexpected movement/account counts, and required protected-provenance write failure. Specify valid-case invariants: only approved synthetic targets receive L2 changes; movement identity/linkage and L1 fields remain unchanged; only scoped synthetic accounts receive derived L2/total updates. **Verify:** tests fail before `apply.sql`; all test data and snapshots are synthetic and external to repository output locations. **Rollback:** discard synthetic disposable database.

- [ ] **7. GREEN — implement `apply.sql` as a bounded guarded transaction.** Use parameterized session staging, `XACT_ABORT`, explicit `SERIALIZABLE` transaction, and locked reads. Re-run target, quote uniqueness/authority, scope, and row-count guards inside the transaction; update only approved manifest targets’ `BudgetAmount`; recalculate only manifest customers’ `BudgetBalance` and `TotalBalance` from full ledgers; preserve `BillingBalance` and all protected fields. Require protected evidence output creation/access testing before mutation; if DBA/security approve target-DB provenance, keep it least-privilege, in-transaction, outside EF/application dependencies, and never export it to a repository path. **Verify:** valid synthetic case commits the bounded changes; every failure test proves value-equivalent rollback. **Rollback:** transaction rollback; no inverse update.

- [ ] **8. GREEN/TRIANGULATE — implement independent read-only validation.** Add `validate.sql` and its launcher path to recheck external-manifest controls, exact scoped changes, source totals, preserved movement/L1 fields, scoped account derivations, and required provenance availability. It must support synthetic pre-change/restored and post-change states, return nonzero on mismatch, redact console output, and write detailed results only below `-ProtectedDirectory`. **Verify:** valid and restored synthetic states pass as appropriate; altered non-target/account/provenance synthetic cases fail. **Rollback:** read-only.

- [ ] **9. REFACTOR — security and architecture review of PR 2.** Review `apply.sql`, SQL client invocation, permissions, lock scope, fail-closed controls, and optional provenance with DBA/security/application maintainers. Confirm scripts remain operational tooling outside Clean Architecture and no EF migration, model, endpoint, UI, configuration, or launcher change is introduced. Store real review/sign-off only externally; commit at most a data-free checklist/template. **Verify:** `git diff --check`; run the synthetic PowerShell suite and `dotnet build SPC.slnx -c Release` / configured `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` as no-regression checks. **Rollback:** reject the PR if unresolved safety or boundary concerns remain.

## PR 3 — RED/GREEN dry-run, rollback tooling, and data-free operations guide

- [ ] **10. RED — specify launcher safety tests.** Add `tests/launcher-gates.tests.ps1` for `dry-run.ps1`, `execute.ps1`, and `rollback.ps1`: absent/unsafe protected directory, repository-contained or aliased input/output, inaccessible evidence path, malformed/changed external control, preflight failure, backup/verify failure, API still serving, active writer, restore failure, and protected-evidence write failure. Mock process/service/backup operations or use synthetic disposable instances only. **Verify:** every gate prevents `apply.sql`; no live endpoint/service/database is contacted and no output is retained in-repo. **Rollback:** remove only external synthetic test material.

- [ ] **11. GREEN — implement protected-path-gated launchers.** Add `dry-run.ps1`, `execute.ps1`, and `rollback.ps1`. Each validates canonical paths and protected storage before any live action, uses opaque execution correlation only in console output, and redirects detailed client/backup/report/provenance output exclusively to external protected storage. `execute.ps1` must require passing external preflight, verified external backup, API downtime/no-writer confirmation, and protected evidence readiness before applying. `dry-run.ps1` restores only to an approved disposable environment after the same gates. `rollback.ps1` uses only the verified protected backup and never an inverse ad-hoc update or automated backup deletion. **Verify:** launcher-gate suite passes with mocks/synthetic targets; review confirms no default/output/temp path is in the repository. **Rollback:** launcher failure is no-mutation; recovery is the external verified-backup procedure.

- [ ] **12. TRIANGULATE/REFACTOR — publish a data-free runbook and rehearsal checklist.** Complete package `README.md` with generic prerequisites, roles, external-only artifact categories, command order, path/redaction checks, preflight/backup/API-stop/transaction/validation gates, restart through unchanged `scripts/run-api-local.sh`, manual-review scope, rollback decision tree, and explicit no-automated-deletion rule. Add only blank/data-free checklist templates if needed. Require authorized reviewers to inspect actual manifests, backup/restore results, reports, hashes, and sign-offs externally—not in pull requests or repository files. **Verify:** tabletop exercise with synthetic values completes without repository evidence or mutation. **Rollback:** documentation-only.

## Separate live-operation gate — not part of implementation completion

- [ ] **13. Authorize and perform dry-run externally.** After all three PRs merge in the feature-branch chain, authorized DBA, security, business, application-maintainer, and operator reviewers inspect the real external manifest, protected location, backup/restore readiness, and redacted tooling behavior outside the repository. Run the same protected-path-gated process only against an approved disposable restore environment; retain all real outputs/evidence externally. **Go/no-go:** any failed guard or review is no-go and requires correction/rehearsal; no live API stop or live mutation occurs in this task.

- [ ] **14. Separately authorize any live operation after dry-run.** A release owner explicitly approves the live window only after external dry-run evidence and rollback readiness review. The operator follows the runbook: verified protected backup, API stop/no writers, one guarded transaction, external post-change validation, restart via unchanged `scripts/run-api-local.sh`, and external manual review of all approved movements/accounts. Retain backup/evidence externally until later explicit user authorization for deletion. **Go/no-go and rollback:** post-commit/manual-review failure stops the API and restores the verified external backup; never commit live reports, identifiers, totals, hashes, or sign-offs.

## Completion evidence

- [ ] PR 1–3 retain only reviewable code, data-free templates, synthetic fixtures, and synthetic test output outside repository paths; each PR includes RED → GREEN → TRIANGULATE → REFACTOR evidence from synthetic/mocked runs.
- [ ] Validate no real operational data/evidence is tracked with repository inspection and `git diff --check`; run configured build/test commands as applicable.
- [ ] Treat external evidence review and dry-run as prerequisites to a separately authorized live operation, not artifacts to commit or attach to the PR chain.
