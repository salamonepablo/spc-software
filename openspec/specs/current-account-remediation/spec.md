# Current Account Remediation Specification

## Purpose

Define a bounded, one-time trusted-operator correction of missing L2 quote amounts while keeping all real operational data and evidence outside the public repository.

## Requirements

### Requirement: Repository-safe operational artifacts

The repository MUST contain only remediation code, documentation, synthetic fixtures, and data-free templates. It MUST NOT contain real customer, movement, quote, document, financial, manifest, approval, backup, report, hash, or operational-evidence data. Real inputs and outputs MUST reside in an external protected directory.

### Requirement: Deterministic 6/3 manifest qualification

The remediation MUST qualify the trusted operator's supplied manifest before mutation. Qualification MUST confirm exactly **6** target `CurrentAccountMovements` across exactly **3** customers. Each target MUST be `DocumentType = PR`, have `BudgetAmount = 0`, preserve its customer/document linkage, resolve to exactly one non-voided authoritative quote, and have a proposed amount equal to that quote total. Any count, scope, type, zero-value, linkage, uniqueness, state, source-total, or evidence mismatch MUST abort before writes.

#### Scenario: Supplied manifest qualifies

- GIVEN protected operator evidence contains an approved manifest
- WHEN read-only preflight evaluates it
- THEN it confirms 6 qualifying targets, 3 customers, and one authoritative quote for every target

### Requirement: Atomic bounded correction

The remediation MUST execute in one bounded transaction. It MUST repeat qualification in that transaction, set `BudgetAmount` only for the 6 qualified manifest targets to their uniquely linked authoritative quote totals, and preserve movement identity, date, customer/document linkage, description, L1 amount, and all non-target fields. A failed guard or unexpected result MUST roll back all mutation.

#### Scenario: Valid correction

- GIVEN all operational gates and in-transaction qualifications pass
- WHEN the correction executes
- THEN exactly 6 L2 values change and the transaction commits as one unit

### Requirement: Affected-account balance scope

Only the 3 customers represented by the qualified manifest may have `BudgetBalance` and `TotalBalance` recalculated from their complete movement ledgers. `BillingBalance`, L1 values, and accounts outside those customers MUST remain unchanged.

### Requirement: External validation and operational gates

Protected external snapshots and read-only validation MUST demonstrate exactly 6 and only 6 L2 changes, matching authoritative totals, preserved target/non-target fields, and derived account caches only for the 3 scoped customers. A separate operational runner MUST provide backup and API/writer-exclusion gates, evidence capture, restart, manual review of all 6 movements/3 balances, and rollback. The package MUST NOT implement those operations.

### Non-goals and compatibility

The remediation MUST NOT globally reconcile data, alter non-target movements or non-scoped accounts, fabricate amounts, introduce application behavior changes, store real data under the repository root, or add permanent approval/claim/provenance controls. API/UI/domain/EF contracts, quote lifecycle, public L2 default, and local L2 opt-in remain unchanged.

## Acceptance criteria

1. Tooling rejects real-data paths under the repository root and repository artifacts contain no real operational data.
2. Preflight blocks mutation unless exactly 6 PR zero-L2 movements across 3 customers uniquely resolve to authoritative quote totals.
3. One transaction changes only those 6 L2 values and in-scope L2/total balances, or changes nothing.
4. L1 values and all out-of-scope movements/accounts remain unchanged.
5. Real backup, snapshot, validation, manual-review, and rollback evidence remains externally protected.
