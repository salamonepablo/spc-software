# Session Log 2026-09

## 2026-09 - Local L2 Closure

- Date: 2026-09
- Scope: Completed local L2 closure for current-account quote movements.
- Files changed:
  - `context/current_session.md`
  - `context/session_2026-09.md`
- Architectural impact:
  - Local data correction and container storage/access configuration only; no application architecture change recorded.
  - `sql-spc` now uses durable local volume storage with loopback access; rollback remains retained.
- Tests added/updated:
  - Unit: none.
  - Integration: none.
  - Regression: synthetic 28-scenario validation.
- Validation:
  - Cause confirmed: six PR movements lacked L2 values; three zero-total candidates were excluded.
  - Direct local discovery, preflight, apply, and post-apply validation passed for six movements across three customers.
  - User visually accepted six quotes in Current Account.
- Next actions:
  - PR2 generic package closure commits were published.
  - Paused PR3 work was preserved outside the committed closure.
  - R4-001 is a separate production follow-up and does not block local closure.

## 2026-09-10 - Pi / Gentle Tooling Update

- Date: 2026-09-10
- Scope: Continued the stable tooling update from backup `/home/pablo/pi-tooling-backups/20260909T205201Z`.
- Files changed:
  - External private Pi package registry: `/home/pablo/.pi/agent/npm/`
  - Pi package configuration: `/home/pablo/.pi/agent/settings.json`
  - `context/current_session.md`
  - `context/session_2026-09.md`
- Architectural impact:
  - Developer harness only; application architecture and runtime code are unchanged.
- Tests added/updated:
  - None; package configuration update.
- Validation:
  - Pi remains at `0.85.1`.
  - Updated `gentle-pi` from `0.14.0` to `2.5.0` and `gentle-engram` from `0.1.10` to `0.1.12`.
  - `pi list` resolves both updated private packages.
  - Package-local `gentle-ai --version` reports `2.7.0`; integrity metadata is present.
  - The npm installer-script notice is informational: the postinstall completed and the binary is installed; no approval action was needed.
  - Removed retired `pi-subagents-j0k3r` via `pi remove npm:pi-subagents-j0k3r`; `pi list` no longer reports it.
  - Preserved `~/.pi/agent/subagents.json` model-profile customizations unchanged; the configuration remains valid JSON.
- Next actions:
  - Restart Pi to load the updated extensions in a fresh interactive process.

## 2026-09 - Payments Circuit

- Date: 2026-09
- Scope: Recorded completion evidence for the customer Payments circuit and sidebar access.
- Files changed: Payments circuit files and `SPC.Web/Components/Layout/NavMenu.razor`; this log.
- Architectural impact: Payments workflows span API, persistence, and Web UI; no additional architectural change was made in this P2 cleanup.
- Tests added/updated: No tests added in P2. Existing `VoidPayment_IsIdempotent_WhenAlreadyVoided` passed 1/1; prior suite had 316 passes and 2 LocalDB failures under WSL.
- Validation: UI L1 #6986 and L2 #6987 were exercised in disposable SPC TEST; #6987 was voided with one reversal and balances restored. Receipt print/reprint was tested. Current Web build passed with existing CS8601 warning at `SPC.Web/Components/Pages/Payments/Create.razor:92`.
- Next actions: No push; local commit is next.

## 2026-09-28 - Common Delivery Notes R1–R4

- Date: 2026-09-28
- Scope: Implemented common Remitos MVP slices R1–R4 on `feature/remitos-comunes`: corrective status semantics/importer mapping; atomic standalone and invoice-linked creation; read-only paged query/detail; saved-data three-copy PDF; Blazor list/create/detail; invoice action; Payments handoff.
- Files changed: API migration/status mapper, delivery-note command/query/PDF services, endpoints/contracts and DI; invoice response BranchId projection; embedded Carlito fonts/OFL; Web API client/DTOs, navigation, invoice and Remitos pages; focused API/service tests. Detailed paths and remaining checks are tracked in `odd/tasks/remitos-comunes.md`.
- Architectural impact: branch-scoped numbering and idempotent transactional save; invoice-linked notes copy invoice lines without a second stock adjustment; PDF reads persisted data with embedded fonts; additive invoice BranchId supports the default branch; Payments receives customer, original invoice branch, Billing, and reference only, with no amount or automatic submission.
- Tests added/updated: R1 status/import focused tests; R2 command and in-memory endpoint tests; R3 query/PDF tests; R4 invoice BranchId and absolute PDF URL tests.
- Validation: independent R1–R4 focused run passed 24/24; API Release build 0 errors/28 warnings; Web Release build 0 errors/1 warning; `git diff --check` passed. NU1903 advisories remain for Microsoft.OpenApi 2.4.1 (`GHSA-v5pm-xwqc-g5wc`) and SQLitePCLRaw.lib.e_sqlite3 2.1.11 (`GHSA-2m69-gcr7-jv3q`); PDFsharp CS0618 and existing Payments CS8601 warnings remain. The `SPC.Migration` project baseline has 203 errors at clean HEAD; no broad repair was attempted.
- Follow-ups: R5 resumed after confirming local Docker Desktop/WSL access to isolated `SPC TEST`; see the following entry. No commit/push, production/VB6 changes, or cleanup of test records.

## 2026-09-28 - Common Delivery Notes R5 Local TEST Verification

- Date: 2026-09-28
- Scope: Recovered the prior local SQL Server TEST configuration from Engram, verified WSL Docker Desktop integration and database identity, applied only the reviewed Remitos migrations, and exercised the Remitos visible UI flows.
- Files changed: `odd/tasks/remitos-comunes.md`, `context/current_session.md`, `context/session_2026-09.md`. R5 did not change source or test files.
- Architectural impact: No application code change. The local `SPC TEST` schema now has the two planned Remitos migrations; production and VB6 were untouched.
- Tests added/updated: None. Existing focused R1–R4 run was not repeated. No SQL Server race/idempotency/rollback harness was run.
- Validation:
  - WSL2 Ubuntu Docker Desktop client/server 29.7.2; `sql-spc` at loopback `127.0.0.1:1433`. SQL identity: server `09b5ac392dbd`, database `SPC TEST`, ONLINE/read-write.
  - Explicit TEST API startup applied only `20260928120000_CorrectDeliveryNoteStatusColumnNames` and `20260928130000_AddDeliveryNoteIdempotencyAndBranchSequence`; history now contains seven migrations and expected sequence/idempotency schema. API responded HTTP 200.
  - Web connected to API at 5233 and served on 5065. Headed Playwright created standalone note ID 9650/number 296012 (stock adjustment off) and L1-linked note ID 9651/number 26445114 from invoice A 0002-00010490 after selecting branch number 5.
  - List search/detail worked. Invoice number/branch/lines/total remained unchanged. Read-only stock checks before/after linked creation showed product code 75 at 100.00 in warehouse 1 and no stock row for code 110 in both reads.
  - Saved-data PDF was viewed through UI as three pages labeled ORIGINAL/DUPLICADO/TRIPLICADO and downloaded to `/tmp/spc-remitos-playwright/remito-296012.pdf`; third-page evidence `/tmp/spc-remitos-playwright/remito-9650-third-page.png`.
  - Payments handoff opened the existing form with original invoice customer, internal branch ID 1 (invoice branch number 2), L1/Billing and invoice/remito reference. No amount parameter or automatic submission; form left unsaved. No payment was created.
  - One nonblocking API `favicon.ico` 404 appeared in the browser console. No source/build/test command ran for R5.
- Risks / follow-ups: SQL Server concurrent uniqueness, idempotent retry, and transaction rollback remain unverified. The standalone stock-adjustment-on path was intentionally skipped to avoid changing TEST inventory. `SPC.Migration` baseline failure remains untouched (clean HEAD 203 errors; worktree 199). API PID 51711, Web PID 52070, and headed Playwright session `spc-remitos` are intentionally left open. No commits, pushes, payment records, test-record cleanup, production access, or VB6 changes.
