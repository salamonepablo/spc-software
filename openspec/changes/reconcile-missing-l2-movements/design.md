# Design: Reconcile Missing L2 Quote Movements

## Status, scope, and trust model

This is a one-time, trusted-operator remediation for a fixed population of nine `PR` movements across four customers. It is not an application feature, a general reconciliation, or an authorization system. It changes no API, UI, EF model/migration, configuration, quote lifecycle, or `scripts/run-api-local.sh` behavior.

The public repository contains a lean operational package only: generic scripts, data-free templates and documentation, and synthetic fixtures/tests. The actual approved manifest, operator approval/signature, pre/post snapshots, hashes, backup metadata/files, reports, console captures, and all execution/rollback evidence are external protected operational records. They must never be committed, copied, or emitted under the repository root.

The trusted operator reviews and signs off the protected manifest and execution controls. Scripts deterministically qualify that supplied evidence against the database, but do not claim to make a privileged DBA/operator's inputs nonforgeable, cryptographically bound, or replay-proof. No permanent target-database approval, claim, provenance, or replay-control schema is introduced.

`CurrentAccountMovements` is the ledger authority. `CurrentAccounts` contains derived caches: only `BudgetBalance` and `TotalBalance` are recalculated, and only for the four manifest customers. `BillingBalance` and all L1 movement values remain untouched.

## Decisions and contracts

| Decision | Contract |
| --- | --- |
| Repository-safe package | Track only generic `README.md`, `manifest.template.json`, path/manifest helpers, parameterized SQL, and explicitly synthetic tests/fixtures under `scripts/remediation/reconcile-missing-l2-movements/`. Templates use placeholders, never real values. |
| External protected directory | Every real invocation receives an explicit operator-controlled protected directory. Path helpers canonicalize the repository root and supplied/derived real-data paths before a database connection; a path in or resolving through the repository root, inaccessible storage, or unsafe containment fails closed. No repository default or fallback exists. |
| External operator controls | The protected directory holds the approved manifest, signed/reviewed approval record, snapshots, hashes, reports, backup reference, and rollback evidence. The operator verifies their presence and reviews/signs the go/no-go controls. These are operational controls, not database records or cryptographic enforcement. |
| Manifest shape and bounded scope | The data-free template describes the identities required to stage each approved target and its source quote (at minimum movement, customer, document, quote, and branch identities plus expected authoritative total), the approval reference, and expected cardinalities. The live manifest must yield exactly 9 distinct movements, 4 customers, and 9 qualified source quotes. It is never persisted by the scripts in the repository or target database. |
| Exact source qualification | Read-only preflight and the transaction both join staged targets to the actual `CurrentAccountMovements` and `Quotes` schema. They require `DocumentType = PR`, `BudgetAmount = 0`, preserved customer/document linkage, the exact staged quote/branch identity, a permitted quote state, and its authoritative `Quotes.Total` equal to the staged expected total. Missing, ambiguous, mismatched, or fabricated source data aborts. No branch is inferred or written. |
| Bounded atomic apply | `apply.sql` receives client-bound, session-local staging (for example `#ApprovedTargets`) and scalar expected counts/values; it does not discover a population or read a repository manifest. With `XACT_ABORT` and one explicit transaction, it repeats qualification, updates exactly 9 `CurrentAccountMovements.BudgetAmount` values, recomputes exactly 4 accounts, asserts row counts and derived results, and commits only when every guard passes. Any failure rolls back all mutation. |
| External evidence and validation | `preflight.sql` and `validate.sql` are read-only. The future operational runner, not `apply.sql`, creates the protected snapshots/reports/hashes and captures detailed evidence. Validation compares the externally retained before snapshot and approved manifest with live data to demonstrate target-only change, source totals, preserved fields/L1, and four-account balance scope. |
| Separation of responsibilities | A separate future runner owns backup creation/verification, API downtime and writer exclusion, external evidence capture, API restart, and rollback orchestration. It must pass those gates before invoking apply, but these platform/operational actions are not responsibilities of the lean SQL package and are not implemented by this design amendment. |

## Repository file plan

The package remains generic and data-free:

```text
scripts/remediation/reconcile-missing-l2-movements/
  README.md                       # operator/runbook contract; no live instructions or values
  manifest.template.json          # placeholders and manifest shape only
  RemediationSafety.ps1           # protected-path and redaction helpers
  preflight.ps1                   # load/shape/path gate and invoke read-only qualification
  preflight.sql                   # parameterized, read-only qualification
  apply.sql                       # parameterized bounded transaction against actual schema
  validate.sql                    # parameterized, read-only post-change checks
  tests/
    *.tests.ps1                   # synthetic behavior and path/redaction tests
    fixtures/*.sql                # synthetic schema/data only
```

No tracked `execute`, backup, service-control, evidence, or rollback runner is required for this remediation package. If a future runner is added, it is a separately reviewed operational integration that invokes the package only after its external gates succeed. It must continue to keep all real artifacts outside the repository.

## Data flow

1. The trusted operator creates/reviews/signs the real manifest and required controls in protected storage, and supplies that external directory explicitly.
2. Path/manifest tooling rejects repository-contained or unsafe paths, validates only the manifest shape and required external control presence, and stages its values only for the invoking process/database session. It does not retain a copy in the repository or database.
3. `preflight.sql` read-only qualifies the staged targets against `CurrentAccountMovements`, `Quotes`, and `CurrentAccounts`. Its detailed result is captured by the future runner in protected storage.
4. The future runner verifies backup readiness, stops the API/excludes writers, captures the protected before snapshot, and obtains the operator's go/no-go review. Failure is a no-go before `apply.sql`.
5. `apply.sql` begins its single transaction and re-qualifies all staged targets. It updates only qualified movement `BudgetAmount` fields to the authoritative quote totals. It then derives each scoped account's L2 sum from its complete movement ledger and sets `BudgetBalance` and `TotalBalance` from ledger L1 plus L2 sums. It asserts 9 movement and 4 account updates, preserves non-target/L1 fields, and rolls back on any failed assertion.
6. The future runner captures protected after snapshots/reports/hashes, runs `validate.sql`, and has the trusted operator review/sign the result. Only after successful validation does it restart through the existing local L2-enabled launcher and collect the required manual review evidence.

## SQL transaction contract

`apply.sql` is intentionally a database-only, parameterized operation against the actual tables and columns already used by the application: `CurrentAccountMovements`, `Quotes`, and `CurrentAccounts`. Session-local staging contains only the externally approved target rows for that invocation.

Within one transaction it must:

- reject unexpected expected counts, duplicate targets, or a staged scope other than 9 movements/4 customers;
- lock/re-read the staged movement, source quote, and scoped account rows sufficiently to prevent an inconsistent qualification/update window;
- require each movement's ID, customer, document linkage, `PR` type, and zero initial L2 value to match staging;
- require the exact quote/branch row to retain matching customer/document/quote linkage, allowed state, and authoritative total;
- update `BudgetAmount` only for the qualified target IDs and assert exactly 9 rows;
- aggregate the complete movement ledger only for the four scoped customers, update only their `BudgetBalance` and `TotalBalance`, and assert exactly 4 rows; and
- roll back on a qualification, row-count, preserved-field, or derived-balance failure.

It must not create tables, schemas, durable claims, audit/provenance rows, approval rows, hashes, or replay records. It must not implement backup, API lifecycle, approval signing, or evidence retention.

## Validation, tests, and review

Synthetic tests and fixtures must cover:

- repository/path containment and redaction failures, including canonical aliases/symlinks where supported;
- valid synthetic nine-target/four-customer qualification and commit;
- malformed or duplicate staging; incorrect counts; non-`PR`, nonzero-L2, missing, ambiguous, wrong-customer/document, wrong-branch, invalid-state, and total-mismatch sources;
- rollback with no movement/account mutation on every transactional guard or row-count failure;
- preservation of movement identity, dates, descriptions, linkage, L1 amounts, non-target movements, and non-scoped accounts; and
- correct L2/total derivation from the full ledgers of only the four scoped customers.

Tests must not require or simulate a permanent operational control store. They use only disposable SQL Server resources and synthetic artifacts. Review confirms actual schema names/joins before use, the bounded transaction, data-free repository inspection, the external protected-directory contract, and the explicit boundary between package tooling and the future runner.

## Rollout and rollback

Implementation is separate from live execution. Before a live run, the trusted operator reviews/signs protected manifest and evidence controls; the future runner verifies backup, API downtime, writer exclusion, preflight, and evidence readiness. The public `DualLineCurrentAccount` default remains disabled; restart uses the unchanged local `scripts/run-api-local.sh` opt-in.

A failed preflight, runner gate, or in-transaction assertion makes the operation a no-go; the transaction rolls back and the API remains stopped until understood. For a post-commit validation or manual-review failure, the future runner stops the API, restores the verified protected pre-change backup, verifies the protected original snapshot and database state, captures rollback evidence externally, then restarts only when approved. The backup is retained externally until successful manual validation and explicit user authorization to delete it.
