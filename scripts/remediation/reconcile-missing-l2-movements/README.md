# Reconcile Missing L2 Movements — Trusted-Operator Remediation Package

> **Scope:** One-time, bounded correction of 9 missing L2 quote movement amounts across 4 customers.

## Repository boundary

This package contains only generic, data-free tooling: parameterized SQL, PowerShell helpers,
synthetic fixtures, and contract tests. It does not contain, create, or retain real operational
data, manifests, approvals, backups, snapshots, reports, or evidence of any kind.

All real operational inputs and outputs reside in an operator-controlled external protected directory.
Path helpers reject any path that resolves within the repository root.

## Trust model

The trusted operator reviews, approves, and supplies the target manifest and operational controls.
Scripts deterministically qualify the supplied data against the database but do not claim
cryptographic binding, nonforgeability, or replay prevention. No permanent database schema,
approval record, or durable control store is introduced by this package.

## Package contents

| File | Purpose |
|------|---------|
| `manifest.template.json` | Data-free shape for the externally approved target manifest |
| `RemediationSafety.ps1` | Protected-path canonicalization, manifest validation, redacted output helpers |
| `preflight.ps1` | Validates protected paths and manifest shape; writes redacted report |
| `preflight.sql` | SELECT-only qualification of session-local staging against application schema |
| `apply.sql` | Bounded atomic transaction: re-qualifies, updates 9 movements, derives 4 account balances |
| `validate.sql` | Read-only post-change validation, including additions/removals outside movement/account scope |
| `validate.ps1` | Validates protected paths, manifest, snapshot presence; writes redacted report |
| `Apply-SyntheticSqlServer.ps1` | Disposable Docker SQL Server harness with 23 behavior scenarios, each reset to an isolated fixture baseline; validates the valid post-apply state and rejects synthetic out-of-scope additions/deletions against session-local before snapshots |
| `tests/` | Synthetic contract tests and fixtures (data-free, disposable only) |

## Data flow

1. The trusted operator prepares the approved manifest and operational controls in protected storage.
2. `preflight.ps1` validates protected paths and manifest shape. `preflight.sql` read-only qualifies session-local staging against `CurrentAccountMovements`, `Quotes`, and `CurrentAccounts`.
3. The external runner (PR3 responsibility) verifies backup, API downtime, and writer exclusion before mutation. **Never run real data without a verified backup and confirmed writer/API exclusion.** This is a non-bypassable operational gate and remains PR3 scope; this PR2 package does not implement a runner or any operational controls.
4. `apply.sql` executes one bounded transaction: re-qualifies all 9 targets, updates `BudgetAmount` to authoritative `Quotes.Total`, derives `BudgetBalance` and `TotalBalance` from the complete ledger for the 4 scoped customers, and rolls back on any guard failure.
5. `validate.sql` independently confirms exactly 9 L2 changes, source-total match, preserved identity/linkage/L1, correct account cache derivation, and no out-of-scope mutation.

## What this package does NOT do

- Create permanent database tables, schemas, approval records, or audit rows
- Implement backup, API lifecycle, writer exclusion, or evidence capture
- Claim cryptographic binding or replay prevention
- Store real operational data in the repository
- Discover a target population (the operator supplies it)

## PR3 responsibilities (separate future runner)

Backup creation/verification, API downtime/writer exclusion, before/after snapshot capture,
evidence retention, API restart through `scripts/run-api-local.sh`, and rollback orchestration
are operational concerns handled by a separate runner that invokes this package only after
its external gates succeed.

## Schema contract

SQL scripts target the actual application columns confirmed from:
- `SPC.Shared/Models/CurrentAccount.cs` — `CurrentAccountMovements` and `CurrentAccounts`
- `SPC.Shared/Models/Quote.cs` — `Quotes` with `Id`, `BranchId`, `QuoteNumber`, `Total`, `IsVoided`
- `SPC.Shared/Models/Enums.cs` — `DocumentType.Quote = 20`
- `SPC.API/Data/SPCDbContext.cs` and migrations

## Running tests

```powershell
# PowerShell package suite. The final command starts the disposable synthetic
# SQL Server container and executes all 23 behavioral scenarios; it is not a token-only check.
pwsh -NoProfile -File tests/preflight.tests.ps1
pwsh -NoProfile -File tests/apply.tests.ps1
pwsh -NoProfile -File tests/validate.tests.ps1
pwsh -NoProfile -File tests/sql-server.tests.ps1

# .NET build and test (from repository root)
dotnet build SPC.slnx -c Release
dotnet test SPC.Tests/SPC.Tests.csproj -c Release
```
