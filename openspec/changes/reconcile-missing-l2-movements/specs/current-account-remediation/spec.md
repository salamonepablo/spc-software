# Current Account Remediation Specification

## Purpose

Define a bounded, one-time trusted-operator correction of documented missing L2 quote amounts while keeping all real operational data and evidence outside the public repository.

## Requirements

### Requirement: Repository-Safe Operational Artifacts

The public repository MUST contain only remediation code, documentation, synthetic fixtures, and data-free templates. It MUST NOT contain or retain real customer, movement, quote, or document identifiers; financial values; manifests; approvals; backups; audit reports; real-data-derived hashes; or preflight, post-change, rollback, or manual-review evidence. All real inputs and outputs MUST reside in an operator-controlled external protected directory.

#### Scenario: Repository artifact review

- GIVEN remediation artifacts are prepared for review
- WHEN repository content is inspected
- THEN it contains no real operational data or evidence

### Requirement: Protected External Evidence

Before execution, a trusted operator MUST provide and review an external protected directory containing the approved manifest and operational evidence. Remediation and validation tooling MUST require this explicit directory for real-data input and output, canonicalize paths, and reject a path that resolves under the repository root. Tooling MUST fail closed if required protected storage is absent, inaccessible, or unsafe.

The protected evidence MUST include the reviewed manifest and approval record, source-document references, before and after snapshots, preflight and post-change results, executor and timestamp, script or version reference, backup reference, and rollback records when applicable. These records are operator-controlled operational evidence, not a permanent database approval, claim, or provenance schema.

#### Scenario: Unsafe evidence location is supplied

- GIVEN a real-data input or output path resolves within the repository root, or protected storage is unavailable
- WHEN preflight, execution, or validation is requested
- THEN the operation MUST fail before database mutation

### Requirement: Deterministic Manifest Qualification

The remediation MUST deterministically qualify the trusted operator's supplied manifest against the live database before mutation. Qualification MUST confirm exactly 9 target `CurrentAccountMovements` across exactly 4 customers; each target MUST have `DocumentType = PR`, `BudgetAmount = 0`, and exactly one linked authoritative quote through its customer and document linkage. Each proposed amount MUST equal that quote's authoritative total. Count, scope, document-type, zero-value, linkage, uniqueness, source-total, or required-evidence mismatch MUST abort before writes.

This qualification MUST validate the supplied evidence and live state, but MUST NOT claim cryptographic binding, nonforgeability, or prevention of a privileged DBA or trusted operator deliberately supplying different staged inputs.

#### Scenario: Supplied manifest qualifies

- GIVEN the trusted operator has reviewed the protected manifest and evidence
- WHEN read-only preflight evaluates it against the live database
- THEN it confirms 9 qualifying targets, 4 customers, and one authoritative quote for each target

#### Scenario: A target does not qualify

- GIVEN a target has a non-`PR` type, nonzero L2 amount, missing or ambiguous quote linkage, or mismatched source total
- WHEN preflight or in-transaction qualification runs
- THEN the remediation MUST abort without changing data

### Requirement: Operational Execution Gates

An operational runner MUST supply successful backup and API-downtime gates before mutation. The runner MUST create and verify a recoverable pre-change `sql-spc` backup, retain its real metadata only in protected external storage, and confirm the local API is stopped. The remediation MUST NOT mutate database data while the API is serving, and MUST NOT proceed when either gate fails.

#### Scenario: Gates permit execution

- GIVEN protected preflight succeeds, the backup is verified, and the API is confirmed stopped
- WHEN the trusted operator requests remediation
- THEN the operation MAY enter its bounded transaction

#### Scenario: A gate fails

- GIVEN backup verification fails or the API remains serving
- WHEN mutation is requested
- THEN no database data MUST change

### Requirement: Atomic Bounded Correction

The trusted operator remediation MUST execute in one bounded database transaction. It MUST repeat target qualification within that transaction, set `BudgetAmount` only for the 9 qualified manifest targets, and set each value only to its uniquely linked authoritative quote total. It MUST preserve movement identity, date, customer, document linkage, description, L1 amount, and all non-target fields. A failed guard, unexpected row count, or balance-result mismatch MUST roll back the entire transaction.

#### Scenario: Valid bounded correction

- GIVEN all operational gates and in-transaction qualifications pass
- WHEN the correction executes
- THEN exactly 9 L2 values change and the transaction commits as one unit

#### Scenario: Unexpected mutation result

- GIVEN a guard fails or an update would affect a row outside the qualified targets
- WHEN the transaction evaluates the result
- THEN it MUST roll back without partial ledger or account-balance mutation

### Requirement: Affected-Account Derived Balance Scope

For only the 4 customers represented by the qualified manifest, the remediation MUST derive `BudgetBalance` from the complete `CurrentAccountMovements` ledger and `TotalBalance` from ledger-derived L1 plus L2 amounts. It MUST NOT alter `BillingBalance`, normalize L1, or change a `CurrentAccounts` row outside those customers. Pre-existing out-of-scope discrepancies MUST be recorded only in protected external evidence and MUST NOT be corrected.

#### Scenario: Derived balances are recalculated in scope

- GIVEN the 9 qualified L2 amounts are corrected
- WHEN affected account balances are recalculated
- THEN each affected `BudgetBalance` equals its complete-ledger L2 sum and `TotalBalance` equals its ledger-derived L1 plus L2 amount

### Requirement: External Snapshot Validation and Manual Review

The process MUST capture before and after snapshots in protected external storage and perform post-change validation there. Validation MUST demonstrate that exactly 9 and only 9 L2 fields changed; each changed value equals its uniquely linked authoritative quote total; movement identity and linkage, L1 values, and non-target fields are unchanged; and only the 4 allowed accounts have derived L2 and total changes. After the API restarts through `scripts/run-api-local.sh`, manual review MUST cover all 9 movement/document links and 4 customer balance presentations, with real results retained only externally.

#### Scenario: Post-change validation passes

- GIVEN the transaction committed
- WHEN the read-only post-change validation compares protected snapshots and live data
- THEN it records successful change, scope, source, and balance checks only in protected external evidence

#### Scenario: Manual review identifies a defect

- GIVEN the API has restarted through the local launcher
- WHEN required movement or balance review fails
- THEN rollback procedures MUST be initiated and documented only in protected external evidence

### Requirement: Rollback and Backup Retention

If validation fails before commit, the transaction MUST roll back and the API MUST remain stopped until the failure is understood. If post-commit validation or manual review fails, operators MUST stop the API, restore the verified pre-change backup from protected external storage, verify database integrity and the original protected snapshots, and restart through `scripts/run-api-local.sh`. The backup MUST remain protected through manual validation, MUST NOT be deleted automatically, and MAY be deleted only after explicit user confirmation following successful manual validation.

#### Scenario: Post-commit recovery

- GIVEN post-commit validation or manual review fails
- WHEN recovery is performed
- THEN the verified pre-change state MUST be restored and verified before API restart

### Requirement: L2 Enablement Compatibility

The remediation MUST NOT change quote lifecycle rules, API or UI contracts, or L2 licensing configuration. The public GitHub default for `DualLineCurrentAccount` MUST remain disabled, and the local Argentine deployment MUST continue to explicitly enable L2 through its existing local launcher.

#### Scenario: Configuration remains intentionally differentiated

- GIVEN remediation artifacts are reviewed or executed
- WHEN public and local configuration are reviewed
- THEN the public default remains disabled and `scripts/run-api-local.sh` remains the explicit local L2 opt-in

## Non-Goals

The remediation MUST NOT perform a global reconciliation; alter movements outside the qualified 9 records; fabricate amounts or choose ambiguous sources; repair unrelated discrepancies; change unrelated account balances; introduce application behavior changes; store real operational data under the repository root; add permanent database approval, claim, or provenance schema; or claim nonforgeable or cryptographic protection against a privileged DBA/operator.

## Acceptance Criteria

1. Repository artifacts contain no real operational data or evidence, and tooling rejects real-data paths under the repository root.
2. A trusted operator reviews the external protected manifest and evidence before execution.
3. Deterministic preflight blocks mutation unless exactly 9 PR zero-L2 movements across 4 customers each resolve to one authoritative quote total.
4. A verified backup and confirmed API downtime gate supplied by the operational runner are required before mutation.
5. One transaction changes only the 9 qualified `BudgetAmount` values and in-scope L2 and total balances, or changes nothing.
6. Before and after snapshots, post-validation, manual review, and any rollback evidence remain only in protected external storage.
7. L1 values and all out-of-scope movements and accounts remain unchanged.
8. The public `DualLineCurrentAccount` default remains disabled while the existing local launcher remains the explicit L2 opt-in.

## Backward Compatibility Notes

Movement identity and linkage, L1 values, API/UI contracts, quote business rules, the public GitHub L2 default, and the local Argentine L2 opt-in behavior MUST remain unchanged.
