# Current Account Remediation Specification

## Purpose

Define a bounded, auditable one-time correction of documented missing L2 quote amounts in the Argentine current-account ledger while keeping all real operational data and evidence outside the public repository.

## Requirements

### Requirement: Repository-Safe Operational Artifacts

The repository MUST contain only remediation code, documentation, synthetic fixtures, and data-free templates. It MUST NOT contain or retain real manifests; customer, movement, quote, or document identifiers; financial totals or amounts; approvals; backup metadata; audit reports; real-data checksums or sign-offs; or preflight, post-change, provenance, rollback, or manual-review evidence. All such real inputs and outputs MUST reside in an operator-controlled external protected directory.

#### Scenario: Repository artifact review

- GIVEN remediation artifacts are prepared for review
- WHEN repository content is inspected
- THEN it contains only code, documentation, synthetic fixtures, and data-free templates and contains no real operational data or evidence

### Requirement: Protected Path and Output Safety

Remediation and validation tooling MUST require an explicit external protected directory for every real-data input and output, including the approved manifest, approvals, reports, backup metadata, and evidence. Tooling MUST canonicalize each path and reject any path that resolves under the repository root. It MUST fail closed when protected storage is absent, inaccessible, or unsafe, and MUST NOT print sensitive values to the console or repository logs.

#### Scenario: Repository-root path is supplied

- GIVEN an operator supplies a manifest, evidence, backup, or report path that resolves within the repository root
- WHEN preflight, execution, or validation begins
- THEN the operation MUST fail before database mutation or real-data output and MUST not emit sensitive values to console or repository logs

#### Scenario: Protected storage is unavailable

- GIVEN the required external protected directory is absent, inaccessible, or unsafe
- WHEN an operation requiring real inputs or outputs is requested
- THEN the operation MUST fail closed without database mutation

### Requirement: Externally Approved Fixed Target Qualification

The remediation MUST operate only on the externally approved manifest of nine `CurrentAccountMovements` records across exactly four customers. Every external-manifest target MUST be approved, have `DocumentType = PR`, have `BudgetAmount = 0` before mutation, and resolve by its customer and document linkage to exactly one authoritative quote. Source totals and approval controls MUST be validated against the external manifest without hard-coding real identifiers, totals, or control values into public artifacts. Missing, ambiguous, changed, or non-qualifying evidence MUST prevent mutation.

#### Scenario: Externally approved targets qualify

- GIVEN the protected external manifest, externally approved controls, and live database
- WHEN preflight evaluates target and quote controls
- THEN it confirms exactly 9 qualifying movements, 4 customers, and 9 unique quotes using the external controls

#### Scenario: A target no longer qualifies

- GIVEN any external-manifest target is unapproved, has a non-`PR` type, nonzero L2 amount, absent or ambiguous quote, or fails an external control
- WHEN preflight or in-transaction validation runs
- THEN the remediation MUST abort before changing data

### Requirement: Preflight, Backup, and API Downtime Gates

The remediation MUST produce a read-only preflight report in the external protected directory before writes. The report MUST record externally controlled target, source-document, complete-ledger, and stored-balance validations for the four affected customers without copying real values to the repository or console logs. It MUST create and verify a recoverable pre-change `sql-spc` backup, retaining its real metadata and verification results only externally, and stop the local API only after backup verification succeeds. The remediation MUST NOT mutate database data while the API is serving.

#### Scenario: Verified external backup permits execution preparation

- GIVEN all external preflight controls pass and a recoverable backup has passed verification
- WHEN the API is stopped
- THEN the operation MAY proceed to its transactional correction

#### Scenario: Backup or downtime gate fails

- GIVEN backup creation or verification fails, protected evidence cannot be recorded, or the API remains serving
- WHEN execution is requested
- THEN the remediation MUST make no database mutation

### Requirement: Atomic Bounded Correction

The correction MUST execute in one database transaction and MUST repeat external-manifest target qualification controls inside that transaction. It MUST set `BudgetAmount` only for the nine externally approved movements and only to each movement's uniquely linked authoritative quote total. It MUST preserve each corrected movement's identity, date, customer, document linkage, description, L1 amount, and all non-target fields. Any failed guard, unexpected affected-row count, balance validation failure, protected-evidence write failure, or path-safety validation failure MUST roll back the entire transaction.

#### Scenario: Valid atomic correction

- GIVEN the API is stopped, protected storage is safe, and all in-transaction guards pass
- WHEN the correction executes
- THEN exactly 9 `BudgetAmount` values change and the transaction commits as one unit

#### Scenario: Unexpected mutation result

- GIVEN an update affects other than the externally approved nine movements or a required validation fails
- WHEN the transaction evaluates the result
- THEN it MUST roll back with no partial ledger or account-balance change

### Requirement: Affected-Account Derived Balance Scope

For only the four customers represented by the external approved manifest, the remediation MUST derive `BudgetBalance` from the complete `CurrentAccountMovements` ledger and MUST derive `TotalBalance` from the ledger-derived L1 plus L2 amounts. It MUST NOT alter `BillingBalance`, normalize L1, or change a `CurrentAccounts` row outside those four customers. Pre-existing out-of-scope ledger-versus-stored-balance discrepancies MUST be reported only as protected external evidence and MUST NOT be corrected.

#### Scenario: Derived balances are recomputed in scope

- GIVEN the nine externally approved L2 amounts are corrected
- WHEN account balances are recalculated
- THEN each affected account's `BudgetBalance` equals its complete-ledger L2 sum and `TotalBalance` equals its ledger-derived L1 plus L2 amount

#### Scenario: Unrelated balance discrepancy exists

- GIVEN another customer has a historical balance discrepancy
- WHEN the remediation runs
- THEN that customer's movements and stored balances MUST remain unchanged

### Requirement: Protected External Provenance and Evidence

The remediation MUST retain real operational evidence only in the external protected directory and make it reviewable with the verified backup by authorized operators. The external evidence MUST include target and source-document references, before and after values, reason, executor, timestamp, approved script or version reference, approval controls, backup reference and verification result, and preflight and post-change results. Repository artifacts and console or repository logs MUST contain no sensitive values, real-data-derived checksum material, or sign-off material.

#### Scenario: Authorized review of protected evidence

- GIVEN a successful or failed remediation attempt
- WHEN an authorized reviewer examines its protected external evidence and backup
- THEN the reviewer can determine the target set, documentary sources, mutation outcome, validation results, and recovery reference without requiring real evidence in the repository

### Requirement: Post-Change Validation and Manual Review

Before commit and after commit, validation MUST use externally approved controls to demonstrate that exactly nine and only nine L2 fields changed; every changed value equals its approved unique quote total; required movement identity and linkage fields and all L1 values are unchanged; and only the four allowed accounts have derived L2 and total changes. The read-only audit and manual review records MUST remain protected external evidence. After restart through `scripts/run-api-local.sh`, manual review MUST cover all nine movement/document links and all four customer balance presentations.

#### Scenario: Post-change controls pass

- GIVEN the correction committed
- WHEN post-change validation runs
- THEN it records successful movement, balance, scope, and documentary controls only in protected external evidence before the API is restarted

#### Scenario: Manual review finds a defect

- GIVEN the API has restarted through the local launcher
- WHEN review of any required movement link or balance presentation fails
- THEN rollback procedures MUST be initiated and rollback evidence retained only externally

### Requirement: Rollback and Backup Retention

If a failure occurs before commit, the remediation MUST roll back and keep the API stopped until understood. If a post-commit or manual-validation failure occurs, operators MUST stop the API, restore the verified pre-change backup from the external protected directory, verify database integrity and the original protected target and balance snapshot, restart through `scripts/run-api-local.sh`, and retain remediation and rollback evidence externally. The backup MUST be retained through manual validation and MUST NOT be deleted automatically; deletion requires explicit user confirmation after successful manual validation.

#### Scenario: Post-commit recovery

- GIVEN a post-commit validation or manual-review failure
- WHEN recovery is performed
- THEN the verified pre-change database state is restored, verified, and documented in protected external evidence before API restart

### Requirement: L2 Enablement Compatibility

The remediation MUST NOT change quote lifecycle rules, API or UI contracts, or L2 licensing configuration. The public GitHub default for `DualLineCurrentAccount` MUST remain disabled, and the local Argentine deployment MUST continue to explicitly enable L2 through its existing local launcher.

#### Scenario: Configuration remains intentionally differentiated

- GIVEN the remediation artifacts are prepared or executed
- WHEN public and local configuration are reviewed
- THEN the public default remains disabled and `scripts/run-api-local.sh` remains the explicit local L2 opt-in

## Non-Goals

The remediation MUST NOT perform a global or historical reconciliation, alter any movement outside the externally approved nine records, fabricate amounts or choose an ambiguous source, repair unrelated Access-imported discrepancies, change unrelated account balances, introduce application architecture or behavior changes, or store real operational data or evidence under the repository root.

## Backward Compatibility Notes

Existing movement identity, document linkage, L1 values, API/UI contracts, quote business rules, public GitHub L2 default, and local Argentine L2 opt-in behavior MUST remain compatible and unchanged.
