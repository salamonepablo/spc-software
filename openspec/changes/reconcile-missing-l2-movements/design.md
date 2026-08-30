# Design: Reconcile Missing L2 Quote Movements

## Status and boundaries

This is a one-time operational SQL remediation, not an application feature. It changes no API, UI, quote lifecycle, licensing, configuration, or Clean Architecture boundary. No database, code, script, service, or configuration is changed by this design amendment.

The eventual package is bounded by an **externally approved** manifest to nine `PR` movements across four customers. `CurrentAccountMovements` remains the ledger authority; `CurrentAccounts` balance fields are derived caches recalculated only for those manifest customers. The package must fail closed rather than discover or repair additional historical records.

### Binding public-repository boundary

This repository is public. Version control may contain only generic scripts, documentation, data-free templates, and synthetic test fixtures. It must never contain real customer, movement, document, quote, approval, amount, total, manifest, backup, report, hash/checksum derived from live data, provenance, sign-off, console capture, or rollback evidence.

Every invocation that reads or writes real operational data requires an explicit operator-supplied external protected directory. The directory holds the approved manifest, approvals, backup and restore material, real-data hashes, reports, and all evidence. Repository paths are never defaults or fallbacks for these artifacts. Real provenance is retained in that protected directory and may additionally be recorded in an access-restricted target-database provenance store within the same transaction; it is never exported, copied, or logged to the repository.

## Decisions and contracts

| Decision | Design contract |
|---|---|
| Repository-safe package | The remediation directory contains `manifest.template.json` (data-free shape and placeholder tokens only), generic SQL/PowerShell, a runbook, and fixtures explicitly labeled synthetic. There is no real `manifest.json`, checksum file, evidence directory, backup location, approval, or captured output under the repository root. |
| Protected-location contract | `execute.ps1`, `dry-run.ps1`, `rollback.ps1`, preflight, and validation require an explicit `-ProtectedDirectory` and explicit real input/output paths rooted beneath it. Before opening a database connection, each resolves the repository root and supplied paths to canonical physical paths (including symlink/junction/reparse-point resolution). It rejects the repository root itself and any descendant for every real input, output, temporary output, backup, restore, report, log, or manifest path. It also rejects a missing, inaccessible, non-directory, non-writable where writes are required, traversal/aliasing, or otherwise unsafe protected location. No fallback location exists. |
| Redacted observability | Console and repository-visible logs contain only status, stage, opaque execution correlation ID, and non-sensitive error category. They never echo paths, connection strings, identifiers, amounts, SQL result rows, manifests, hashes, backup details, or approval/sign-off data. Detailed command output and reports are redirected only to protected external evidence. Failures are summarized without sensitive values. |
| Fixed target authority | The real, approved external manifest is the sole target input. It carries the externally approved identities, quote controls, approval reference, and count controls, but none are hard-coded in repository artifacts. The launcher validates its schema and exact controls (nine movements, four customers, nine unique quotes) from the external input. Duplicate, placeholder, changed, missing, unapproved, or malformed entries fail before mutation. |
| Quote resolution | A target movement is resolved only by its approved customer/document linkage across all branches. Candidate quotes are counted before selection; exactly one candidate must exist and match the external manifest's approved quote identity, state, and authoritative total. No branch is inferred or written. |
| Transactional provenance | The protected directory must be writable and all required evidence files must be created/access-tested before the write transaction starts. The transaction records before/after provenance either in the protected external evidence flow and/or, if DBA/security approve it, an access-restricted target-DB `ops` provenance store. Target-DB provenance is written in the same transaction, inaccessible to application writers, has no EF migration/model/runtime dependency, and is never exported to a repository path. Any required provenance write failure rolls back. |
| Derived values | The transaction updates only approved movements' `BudgetAmount`, using each unique authoritative quote total. It recalculates only the four manifest customers' `BudgetBalance` from complete ledger L2 sums and `TotalBalance` from complete ledger L1 plus L2 sums. It does not alter `BillingBalance`, `BillingAmount`, identities, dates, descriptions, document linkage, or non-target movements/accounts. |

## Repository files versus protected external artifacts

Proposed repository additions during a later implementation are generic only:

```text
scripts/remediation/reconcile-missing-l2-movements/
  README.md                         # generic prerequisites, command order, stop/go and rollback runbook
  manifest.template.json            # data-free schema/template; never a live manifest
  preflight.sql                     # parameterized, read-only qualification
  apply.sql                         # parameterized guarded transaction
  validate.sql                      # parameterized, read-only validation
  dry-run.ps1                       # protected-path-gated disposable restore rehearsal
  execute.ps1                       # protected-path-gated production launcher
  rollback.ps1                      # protected-path-gated recovery launcher
  tests/fixtures/*.sql              # synthetic data only
  tests/*.ps1                       # synthetic/path-safety assertions only
```

The operator provides a protected directory outside the canonical repository root. Its internal layout and all names are created or selected there; it contains the real manifest, approvals, evidence, backup/restore references or files, detailed SQL/client output, and review records. Scripts must not copy any of those files into the repository, even temporarily. Generated files use protected-directory temporary locations and atomic rename there; cleanup must not move data through repository paths.

## Operational data flow

1. **Validate protected paths before any live action.** The launcher derives the canonical repository root from its own location, canonicalizes `-ProtectedDirectory` and every derived/explicit artifact path, verifies containment beneath the protected directory and non-containment beneath the repository root, and tests required access controls. It creates a protected execution directory with an opaque ID. Path validation, protected-storage setup, manifest read, report creation, or redirection failure is a no-go with no database mutation.
2. **Load and qualify only the external manifest.** The launcher reads the external approved manifest, validates its data-free template schema and external approval controls, and stages it only in process/session-local memory or database temporary structures. `preflight.sql` performs no DML. It confirms fixed target/customer/quote cardinalities, `PR` type, zero L2, unique candidate quote resolution, identity/state/total controls, complete-ledger sums, and current stored values. Detailed output is written only to protected evidence; console output is redacted. Other potential zero-L2 records may be reported only in protected evidence and never added to scope.
3. **Back up and stop writes.** After protected preflight passes, create and verify a recoverable pre-change database backup in controlled external storage. Save backup identity, verification result, restore rehearsal results, and any hashes only as protected evidence. Failure prevents API stop or mutation. Then stop the local API, verify it is unavailable, and confirm no other writer/job/session can alter the target. Do not mutate while the API is serving.
4. **Run one guarded transaction.** `apply.sql` uses `XACT_ABORT`, an explicit transaction, serializable isolation, and locked reads of manifest targets, quote ranges, and affected accounts. It repeats all path-independent manifest, qualification, cardinality, identity, quote, scope, and derived-balance guards within the transaction. It captures protected provenance, updates only the approved L2 values, asserts the exact target change set and expected movement/account affected-row counts, recalculates the scoped balance caches, verifies protected fields remain unchanged, and commits only if every guard and required provenance write succeeds. Any error rolls back all changes.
5. **Validate and restart.** With the API still stopped, `validate.sql` independently verifies target resolution, exact change scope, authoritative totals, preserved movement/L1 fields, scoped account derivations, and provenance availability. Detailed results and manual-review material remain external. Only after validation passes may the operator restart using `scripts/run-api-local.sh` and review all approved movement links and affected balance presentations. The public L2 default remains disabled; the existing local launcher remains the explicit opt-in.

## Failure handling and rollback

- A path-safety, protected-storage, manifest, approval, preflight, backup, API-downtime, guard, row-count, validation, or required-provenance failure is fail-closed. Before commit, the transaction rolls back and the API stays stopped until the failure is understood.
- A post-commit validation or manual-review failure requires stopping the API, restoring the verified pre-change backup from external protected storage, verifying integrity and the original protected snapshot, recording recovery evidence only externally (and in approved restricted target-DB provenance if used), then restarting through `scripts/run-api-local.sh`.
- Never use an inverse ad-hoc update. No automated backup deletion is allowed; the protected backup and evidence remain until successful manual validation and explicit user authorization for deletion.

## Test and dry-run plan

Automated tests must use only synthetic fixtures and templates. They verify that:

- all real-data operations require an explicit protected directory;
- canonical-path checks reject repository-root paths, descendants, relative aliases, symlinks/junctions/reparse points, unsafe derived output paths, unavailable/inaccessible storage, and any attempt to emit real output to console or repository logs;
- a valid synthetic fixture follows bounded qualification, transaction, balance derivation, validation, and rollback behavior;
- invalid synthetic controls (duplicate/missing/ambiguous quote, non-PR, nonzero L2, approval/control drift, missing account, unexpected counts, provenance failure) produce no mutation; and
- snapshot comparisons prove protected fields, non-manifest movements, non-scoped accounts, and L1 balances remain unchanged.

`dry-run.ps1` uses a verified backup restored to a disposable environment only after the same protected-path gates pass. All real restore reports and output remain in the protected directory. A live operation remains separately authorized after DBA, security, application-maintainer, business-owner, and operator review of external evidence and a rollback rehearsal.

## Architecture, review, and rollout

Operational scripts stay outside Presentation, Application, Domain, and Infrastructure and introduce no application dependency. There are no proposed application architecture changes or Clean Architecture violations. If target-DB provenance is selected, it is an operational, least-privilege store only, requires DBA/security review, and is not represented as an EF migration.

Implementation and live rollout are separate. Before implementation, update tasks to use only `manifest.template.json` and synthetic fixtures in version control and to require the protected-path/redaction gates above. Before any live execution, reviewers must approve the external manifest and evidence location, backup/restore readiness, transaction guards, external evidence retention, and rollback owner. This design amendment itself performs none of those actions.
