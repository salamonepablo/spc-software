# Proposal: Reconcile Missing L2 Quote Movements

## Intent

Correct a bounded, documented historical data defect in the Argentine deployment's dual-line current account. Nine approved `PR` (quote) movements have a missing L2 (`BudgetAmount`) value despite each having one uniquely linked source quote. `CurrentAccountMovements` remains the canonical, auditable source of truth for current-account balances; no correction may introduce an amount without documentary support.

The correction population is fixed at **9 movements** across **4 customers**. Real target identities, source amounts, totals, and approvals are externally approved operational evidence, not repository content. This change repairs L2 and total derived balances only for that population; it is not a general historical reconciliation.

## Data-protection constraint (binding)

This is a public repository. **No real operational or customer data may be committed to or stored under the repository root.** This includes customer IDs or names; document or quote IDs/numbers; financial amounts; real target manifests; backups; audit reports; hashes/checksums tied to real data; approvals; and any preflight, post-change, provenance, or rollback evidence containing real data.

The repository may contain only code/scripts, documentation, synthetic fixtures, and templates without real data. Before execution, the operator must provide an explicit path to an operator-controlled external protected directory for all real inputs and outputs, including the approved manifest, approvals, backup metadata, and evidence. Executable remediation/validation tooling must reject an input or output path that resolves inside the repository root, and must fail closed if the required protected location is absent, inaccessible, or unsafe.

## Scope

### In scope

- A one-time, reviewed remediation process limited to the externally approved set of nine `DocumentType = PR` movements with zero L2 amount across four customers.
- Repository-safe scripts, templates, and documentation only; all real target manifests, approvals, reports, backup metadata, and provenance remain in the external protected directory.
- For every external-manifest target, verify before mutation that it remains approved, is `PR`, has zero L2 amount, resolves through its customer/document linkage to exactly one quote, and uses that quote's authoritative total.
- Atomically set each target movement's `BudgetAmount` to its uniquely linked quote total while preserving movement identity, date, customer, document linkage, L1 amount, and all non-target fields.
- Recalculate L2 (`BudgetBalance`) and total (`TotalBalance`) derived balances from the complete ledgers of only the four affected customers. L1 (`BillingBalance`) remains derived from its existing ledger and is not normalized.
- Record real execution provenance only externally: target identifiers, source-document references, before/after values, reason, executor, timestamp, approved script/version reference, and backup identifier.
- Provide repeatable preflight and post-change validation tooling constrained by the external approved manifest and four-customer scope.

### Explicit non-goals

- Do **not** commit, write, or retain real remediation data or evidence in the repository.
- Do **not** globally reconcile, sanitize, or overwrite historical Access-imported ledger-versus-stored-balance discrepancies for other customers.
- Do **not** change any movement outside the externally approved nine-movement target manifest.
- Do **not** invent amounts, infer non-unique quote matches, or correct records missing documentary support.
- Do **not** change quote lifecycle/business rules, L2 licensing behavior, public GitHub defaults, API/UI contracts, or unrelated account balances.
- The public repository's disabled `DualLineCurrentAccount` default remains intentional. The local Argentine launcher remains the explicit L2 opt-in.

## Affected areas

| Area | Change/impact |
|---|---|
| `sql-spc` / `SPC` database | One atomic, bounded correction to the externally approved nine movement L2 values and derived balances for four customers. |
| `CurrentAccountMovements` | Canonical ledger rows retain identity and document linkage; only approved missing `BudgetAmount` values are corrected. |
| `CurrentAccounts` | L2 and total cached/derived balances are recomputed only for affected customers; L1 is not normalized. |
| Operational scripts/evidence | Guarded remediation and validation tooling must use an explicit external protected directory and prevent repository-root data paths. |
| `scripts/run-api-local.sh` and configuration | No change expected; it already explicitly enables L2 for the local Argentine deployment. |

## Safety guards and execution plan

1. **Provide protected external inputs.** The operator supplies an explicit, approved external protected directory. Tooling canonicalizes paths and aborts if any real input/output path is within the repository root.
2. **Prepare and approve externally.** Generate and retain the live preflight report and real target manifest only in that directory. The report must confirm exactly 9 target movements, 4 distinct customers, and 9 unique quote matches. Any count, uniqueness, document type, zero-value, quote-state, or approved-total mismatch aborts before writes.
3. **Create a recoverable backup externally.** Take and verify a pre-change `sql-spc` backup. Store backup metadata, verification results, and all references only in the protected directory. Do not proceed if backup creation or verification fails.
4. **Briefly stop the API.** Stop only the local API after confirming the backup, preventing concurrent current-account writes. Do not modify database data while the API is serving.
5. **Execute one transaction.** Run the approved parameterized remediation in a single SQL transaction. It re-runs target guards inside the transaction, updates only the externally approved nine rows, recomputes only the four affected customers' L2 and total balances, emits provenance solely to the protected directory, and rolls back on any unexpected result.
6. **Validate before and after commit.** Confirm only the intended L2 changes, no L1 changes, the expected four accounts only, authoritative quote totals, and balance derivations. Retain real results solely as protected external evidence.
7. **Restart and manually validate.** Restart with the existing local L2-enabled launcher. Retain manual validation records for all nine movements and four balances only in the protected directory.
8. **Backup retention.** Retain the protected external backup until manual validation completes. Delete it only after explicit user confirmation; no automated cleanup is permitted.

## Data validation plan

### Pre-change gates

- The external-manifest target query returns exactly 9 rows and 4 customers.
- Every target is `PR` with `BudgetAmount = 0` and matches exactly one quote by customer and quote number.
- Authoritative source totals and approval controls match the external manifest.
- Capture each affected customer's complete ledger sums and stored L1/L2/total balances externally, without treating pre-existing non-scope discrepancies as defects to repair.
- Confirm backup integrity and retain any real-data verification values only outside the repository.
- Confirm every real-data input and output location resolves outside the repository root.

### Post-change checks

- Exactly 9 and only 9 `BudgetAmount` fields changed; each equals its approved unique source quote total.
- Original movement/document linkage, IDs, dates, customer references, descriptions, and L1 amounts are unchanged.
- For each affected customer, `BudgetBalance` equals `SUM(CurrentAccountMovements.BudgetAmount)` and `TotalBalance` equals the ledger-derived L1 plus L2 amount; `BillingBalance` is not altered.
- No `CurrentAccounts` row outside the four affected customers changed.
- Re-run the read-only audit: the approved targets are resolved, unique-match controls hold, and protected external evidence contains before/after results and provenance.
- With the API restarted through `scripts/run-api-local.sh`, manually review all nine document links/movements and four customer balance presentations; store real review evidence externally only.

## Acceptance criteria

1. Tooling requires an explicit external protected directory for real inputs/outputs and fails when a path resolves under the repository root.
2. No real customer, document, quote, financial, manifest, backup, audit, approval, real-data hash, or operational-evidence data is committed to or stored in the repository; repository artifacts are limited to code, documentation, synthetic fixtures, and data-free templates.
3. The remediation is blocked unless protected external preflight evidence confirms exactly 9 approved PR movements, 4 affected customers, and 9 unique source quotes.
4. Each corrected `BudgetAmount` equals its uniquely linked authoritative quote total; no amount is fabricated or selected from an ambiguous/missing match.
5. The remediation changes only the nine approved movements and derived L2/total balances for the four affected customers; all L1 values remain untouched.
6. The correction is atomic: a failed guard, update count, balance calculation, protected-evidence write, or path-safety validation leaves no partial mutation.
7. Original ledger/document linkage is preserved, and durable protected external evidence records the required provenance and backup reference.
8. Post-change validation demonstrates ledger-derived L2 and total balances for each affected customer and documents pre-existing out-of-scope discrepancies without changing them.
9. The API is stopped during mutation, restarted afterwards with the local L2-enabled launcher, and all 9 movements/4 accounts receive manual validation.
10. The backup remains protected externally until the user explicitly confirms deletion after manual validation.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Leakage of real data into the public repository | Keep all real manifests, reports, backups, approvals, hashes tied to real data, and evidence in an explicit protected external directory; reject repository-root paths; use only synthetic fixtures and templates in-repo. |
| Incorrect or ambiguous source amount | Require an externally approved manifest, unique customer-plus-quote-number match, and fixed-count controls; abort on mismatch. |
| Partial or concurrent data mutation | Verified backup, API downtime, one transaction, in-transaction revalidation, and row-count assertions. |
| Accidentally normalizing legacy discrepancies | Fixed external target set/customer scope; reject out-of-scope updates; report but do not repair unrelated differences. |
| Lost auditability from direct historical update | Preserve movement linkage and retain immutable before/after provenance plus backup reference in protected external storage. |
| Incorrect cached balances | Recompute only affected L2/total balances from the complete ledger and validate each result before/after restart. |
| Operational recovery delay | Keep the verified protected backup until manual sign-off; prepare rollback instructions before execution. |

## Rollback

If validation fails before transaction commit, roll back the transaction and keep the API stopped until the failure is understood. If a post-commit or manual-validation failure is found, stop the API, restore the verified pre-change `sql-spc` backup from the protected external directory, verify database integrity and the original protected target/balance snapshot, then restart through the local L2-enabled launcher. Preserve remediation and rollback evidence only in protected external storage. Backup deletion requires explicit user confirmation only after successful manual validation.

## Forecast review workload

This is a small, high-risk data operation rather than a broad code feature. Review focuses on path-safety enforcement, the externally approved fixed target manifest, source-document uniqueness, transaction/guard correctness, protected before/after evidence, and rollback readiness. Expected manual business review covers all 9 corrected movements, their source quotes, and 4 affected customer balance views. No review workload is allocated to a global historical reconciliation because it is explicitly out of scope.

## Architecture and delivery notes

No application behavior or architecture change is proposed. The existing local launcher explicitly enables the established Argentine L2 workflow, while the public default stays disabled by design. If supporting automation is added, it remains an operational data-remediation concern, avoids endpoint/UI business logic, uses English identifiers, and has deterministic path-safety, preflight, and validation coverage. It must accept the protected external path explicitly and must never embed, copy, emit, or default real operational data into the repository.

## Proposal question round

The proposal's scope and data-protection amendment were explicitly confirmed: retain the bounded 9-movement/4-customer remediation, but treat all real target data solely as externally approved evidence. The binding product/operational rule is that the public repository contains no sensitive or real operational data; the protected external directory is the sole location for real manifests, approvals, backup metadata, reports, and evidence.
