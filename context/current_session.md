# Current Session Context

**Last Updated:** 2026-10-02
**Branch:** feature/remitos-comunes (parent owns integration to main)
**Version:** 1.0.0
**Tests:** R5 functional validation complete; focused tests 27 passed, 5 SQL-skipped; full suite 333 passed, 5 skipped, 2 pre-existing LocalDB/WSL failures. Final incremental API/Web builds: 0 errors; API has 2 NU1903 warnings. `git diff --check` passed. Commit/publication pending; no hash claimed.

## Latest closure — Remitos R5 (2026-10-02)

- Verified explicit local SQL Server `SPC TEST` identity (`09b5ac392dbd`, loopback 1433, ONLINE/read-write; seven migrations). SQL idempotency, conflict isolation, concurrent numbering, injected rollback, and UI stock-on evidence are recorded in `odd/tasks/remitos-comunes.md`.
- Headed UI validated saved list/detail, 3-copy PDF, stock adjustment, and the existing linked-note Payments handoff (no amount and no save). API 5233 and Web 5065 were left running. Existing invoice immutability/no-double-decrement evidence remains; no new linked note or payment was created.
- First SQL test run: 4 passed/1 failed due to harness losing password from opened `ConnectionString`; reused original options, corrected isolated rollback test passed 1/1. Concurrent attempt rejected with zero effects; automatic retry is not guaranteed.
- Verification: focused 27 passed/5 SQL-skipped; full suite 333 passed/5 skipped/2 known LocalDB/WSL failures; incremental API build 0 errors/2 NU1903 warnings; Web build 0 errors/0 warnings; `git diff --check` passed. Importer baseline (203 clean vs 199 working errors) not rerun/repaired.
- Parent owns explicit-path Remitos commit, safe main integration/publication, and safe deletion of merged branches; no new branches. No application changes in this documentation-only continuation. Preserved protected `SPC.API/bin\\Debug/`, old `odd/`, continuity notes and Playwright artifacts. Durable recovery backup: `/home/pablo/spc-backups/spc-remitos-handoff-20261002T214610`.

---


---

## Current Feature — Common Delivery Notes (2026-09-28)

### Scope and constraints
Implemented R1–R4 of the common Remitos MVP: safe status semantics, atomic creation from standalone and L1 invoice flows, no-tracking consultation, three-copy PDF, Web entry/detail/list, and a Payments handoff. Temporary remitos and consignments remain excluded. No production or VB6 edits, commit, or push. Preserved unrelated working-tree changes; only the newly authorized `odd/tasks/remitos-comunes.md` was added/updated under `odd/`.

### Files changed by feature area
- API: forward migration `SPC.API/Migrations/20260928120000_CorrectDeliveryNoteStatusColumnNames.cs`; delivery-note command/query/PDF/font services, endpoints, contracts, DI/package configuration; additive `BranchId` in invoice response/projection.
- Importer: `SPC.Migration/DeliveryNoteImportMapper.cs` and `ImportFromCsv.cs` status mapping.
- PDF assets: embedded Carlito regular/bold font and SIL OFL license under `SPC.API/Assets/Fonts/`.
- Web: `IApiService`/`ApiService`, delivery-note DTOs, `NavMenu.razor`, invoice entry action, and Remitos Index/Create/Detail pages.
- Tests: status semantics/import mapping, command/idempotency/stock, query/PDF, invoice BranchId mapping, and absolute API PDF URL coverage. Detailed task record: `odd/tasks/remitos-comunes.md`.

### Architectural impact
Branch-scoped numbering and idempotent, transactional delivery-note persistence. Invoice-linked notes copy saved invoice lines and do not adjust stock a second time. PDF output reads persisted data only; its font is embedded for Linux portability. Existing Payments is reused via query parameters and is not auto-submitted. The invoice API contract gains `BranchId` additively.

### Validation
- Focused R1–R4 unit tests passed 24/24 in the independent final run.
- API Release build: 0 errors, 28 warnings. Web Release build: 0 errors, 1 warning. `git diff --check` passed.
- Existing NU1903 advisories remain (`Microsoft.OpenApi` 2.4.1 / GHSA-v5pm-xwqc-g5wc; `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 / GHSA-2m69-gcr7-jv3q). PDFsharp CS0618 and existing `Payments/Create.razor` CS8601 warnings remain.
- `SPC.Migration` is an existing/baseline build failure: clean HEAD archive had 203 errors, working tree had 199; no broad repair attempted.

### R5 — Local TEST integration (historical checkpoint, superseded by 2026-10-02 closure)
- Verified WSL2 Ubuntu Docker Desktop integration: Docker client/server 29.7.2; `sql-spc` publishes only `127.0.0.1:1433`, volume `spc-sql-data`. Read-only SQL identity check confirmed server `09b5ac392dbd`, database `SPC TEST`, ONLINE/read-write. No Desktop UI change or reinstall was needed. The safe launcher is process-level explicit configuration; `scripts/run-api-local.sh` hardcodes `Database=SPC` and was not used.
- Reviewed the five applied migration IDs and schema preconditions before any migration. API startup on explicit TEST config applied only `20260928120000_CorrectDeliveryNoteStatusColumnNames` and `20260928130000_AddDeliveryNoteIdempotencyAndBranchSequence`; post-start history has seven IDs and expected sequence/idempotency columns/index. API returned HTTP 200.
- Web runs on 5065, API on 5233, API `BaseUrl` points to localhost:5233. Headed Playwright session `spc-remitos` remains open on Remito detail ID 9651; another tab remains open on standalone Remito PDF ID 9650.
- UI created TEST-only marked standalone Remito ID 9650/number 296012 with stock adjustment off and linked Remito ID 9651/number 26445114 from L1 invoice A 0002-00010490. Changed Remito branch to 5 from invoice branch number 2; proposal refreshed. List search/detail confirmed both records. Reopened invoice modal showed original invoice number/branch/lines/total unchanged.
- Standalone saved-data PDF opened and downloaded through visible UI. Chromium showed 3 pages: ORIGINAL, DUPLICADO, TRIPLICADO. Download: `/tmp/spc-remitos-playwright/remito-296012.pdf`; third-copy screenshot: `/tmp/spc-remitos-playwright/remito-9650-third-page.png`.
- Read-only SQL before/after linked note confirmed product code 75 remained 100.00 in warehouse 1 and product code 110 had no stock row; no double decrement. Payments handoff displayed original invoice customer, internal `branchId=1` (invoice branch number 2), `appliesTo=Billing`, invoice/remito reference, no amount query parameter; form amount remained 0 and no payment was submitted.
- No production/VB6 touched, no Payments created, no source/test changes, no test/build suite rerun, no commit/push. Only observed browser console error was a nonblocking `favicon.ico` 404. No test record cleanup performed.

### Historical R5 checkpoint — remaining checks at 2026-09-28 (superseded)
- SQL Server-backed concurrent branch-number conflict, idempotent retry, and failed-transaction rollback remain unverified; no SQL test harness was run.
- Standalone stock-adjustment-on runtime behavior intentionally not exercised to avoid modifying stock in the TEST copy; only opt-out path was verified.
- `SPC.Migration` baseline failure is unchanged (clean HEAD 203 errors; working tree 199); no evidence showed it was needed for R5, and no repair was attempted.
- API/Web and headed browser are intentionally left running per request.

## Session Summary (2026-09)

Completed local L2 closure for current-account quote movements.

### Completed
- Cause confirmed: six PR movements lacked L2 values; three zero-total candidates were excluded.
- Direct local correction covered six movements across three customers.
- Synthetic 28-scenario validation and direct local discovery, preflight, apply, and validation passed.
- User visually accepted all six quotes in Current Account.
- `sql-spc` now uses a durable local volume with loopback access; rollback remains retained.
- PR2 generic package closure commits were published; paused PR3 work was preserved outside the committed closure.

### Validation
- Synthetic 28-scenario validation passed.
- Direct local discovery, preflight, apply, and post-apply validation passed.
- User visual acceptance confirmed six Current Account quotes.

### Next / Follow-up
- R4-001 remains a separate production follow-up and does not block this local closure.

### Payments Circuit
- UI L1 #6986 and L2 #6987 were exercised in disposable SPC TEST; #6987 was voided with one reversal and account balances restored. Receipt print/reprint was tested.
- Existing `VoidPayment_IsIdempotent_WhenAlreadyVoided` passed 1/1. Earlier full suite: 316 passed; 2 LocalDB failures under WSL.
- Current Web build passed with existing CS8601 warning at `SPC.Web/Components/Pages/Payments/Create.razor:92`.
- No push; local commit is next.

---

## Tooling Update (2026-09-10)

### Completed
- Restored the stable Pi tooling baseline from backup `/home/pablo/pi-tooling-backups/20260909T205201Z`.
- Confirmed Pi was already updated to `0.85.1`.
- Updated private Pi packages: `gentle-pi` `0.14.0` -> `2.5.0` and `gentle-engram` `0.1.10` -> `0.1.12`.
- Confirmed the bundled package-local `gentle-ai` binary at version `2.7.0` and its installer integrity metadata.
- Removed the retired `pi-subagents-j0k3r` package with `pi remove npm:pi-subagents-j0k3r` to resolve the Gentle Agents startup notice.
- Preserved the global `~/.pi/agent/subagents.json` model-profile customizations unchanged.

### Validation
- `pi list` resolves both updated private packages.
- `pi --version` and global npm inventory report `0.85.1`.
- `gentle-ai --version` reports `2.7.0`.
- The installer-script notice remains informational; the package-local binary was installed successfully, so no script approval is required.
- `pi list` now reports only `gentle-pi`, `gentle-engram`, and `pi-web-access`; no legacy package registration remains and `subagents.json` is valid JSON with its original hash.

### Follow-up
- Restart Pi before using the new extensions in an interactive session; the current process cannot reload them safely.

---

## Session Summary (2026-06-16)

Inspected failed `/sdd-init` state and restored the valid OpenSpec config baseline.

### Completed
- Confirmed the failed `/sdd-init` overwrote `openspec/config.yaml` with an incorrect Node.js/TypeScript/Python profile and disabled strict TDD.
- Restored `openspec/config.yaml` from `openspec/OLD/config_20260616.yaml`, which correctly identifies the repository as .NET 10 / C# with xUnit tests.
- Verified no Node.js/Python project markers exist at repository depth checked (`package.json`, `pyproject.toml`, etc. absent).
- Confirmed repository test baseline remains green: 306/306 passing.

### Current Working Tree Notes
- `openspec/config.yaml` is restored to match the tracked baseline.
- Removed the untracked `openspec/OLD/` backup directory after user approval.

---

## Previous Session Summary (2026-06-02)

Completed **OpenSpec context reset / SDD init baseline**.

### Completed
- Removed the existing incorrect OpenSpec baseline that identified the repo as Node.js/TypeScript/Python.
- Recreated OpenSpec context for the actual project stack: .NET 10, C#, ASP.NET Core Minimal APIs, Blazor Server, EF Core, SQLite, xUnit, FluentAssertions.
- Captured user-confirmed SDD session defaults:
  - Execution mode: interactive
  - Artifact store: openspec
  - PR strategy: single-pr-default
  - Review budget: 400 changed lines
- Enabled strict TDD expectations for medium/high-impact code changes.
- Added project-level OpenSpec context in `openspec/project.md`.
- Confirmed Engram CLI availability and existing `spc-software` memory database.
- Saved current Pi/Gentleman handoff and OpenSpec reset summaries to Engram.
- Exported project memories to `.engram/` via `engram sync --project spc-software`.
- Ran test suite successfully: 298/298 passing.
- Configured Pi Engram MCP integration through `gentle-engram` and `pi-mcp-adapter`.
- Fixed current account navigation metadata regression and reran full test suite: 299/299 passing.
- Updated current account document opening UX so movement document links open in a new browser tab, preserving the current account origin page.
- Optimized quote navigation from current account: quote movements now open `/quotes/{quoteNumber}` directly, with a direct quote-by-number API endpoint and numeric search optimization.
- Extended direct official-document navigation to invoices, credit notes, and debit notes using document type, point of sale, document number, and customer filters where available.
- Improved invoice detail tax breakdown display and current account range guardrail feedback in Web UI.
- Removed current account maximum date-span rejection: users can request full customer history; large result sets are returned fully with a warning.
- Fixed NC/ND fallback detail routes for historical movements that have type + number but no point-of-sale in the current account description.
- Added tax breakdown footer to credit note and debit note detail tables.

### Files Changed
- `openspec/config.yaml`
- `openspec/project.md`
- `.engram/manifest.json`
- `.engram/chunks/090e382d.jsonl.gz`
- `.engram/chunks/9ec0d509.jsonl.gz`
- `.engram/chunks/1d4f0a2d.jsonl.gz`
- `SPC.API/Endpoints/CurrentAccountEndpoints.cs`
- `SPC.Tests/Integration/CurrentAccountEndpointsTests.cs`
- `SPC.Web/Components/Pages/CuentaCorriente/Index.razor`
- `SPC.Tests/Unit/CurrentAccountSearchFlowComponentLogicTests.cs`
- `SPC.API/Endpoints/PresupuestosEndpoints.cs`
- `SPC.API/Endpoints/FacturasEndpoints.cs`
- `SPC.API/Endpoints/NotasCreditoEndpoints.cs`
- `SPC.API/Endpoints/NotasDebitoEndpoints.cs`
- `SPC.API/Services/IQuoteQueryService.cs`
- `SPC.API/Services/QuoteQueryService.cs`
- `SPC.API/Services/IInvoiceQueryService.cs`
- `SPC.API/Services/InvoiceQueryService.cs`
- `SPC.API/Services/ICreditNoteQueryService.cs`
- `SPC.API/Services/CreditNoteQueryService.cs`
- `SPC.API/Services/IDebitNoteQueryService.cs`
- `SPC.API/Services/DebitNoteQueryService.cs`
- `SPC.API/Services/OfficialDocumentSearchParser.cs`
- `SPC.Tests/Integration/PresupuestosEndpointsTests.cs`
- `SPC.Tests/Integration/FacturasEndpointsTests.cs`
- `SPC.Tests/Integration/NotasCreditoEndpointsTests.cs`
- `SPC.Tests/Integration/NotasDebitoEndpointsTests.cs`
- `SPC.Web/Components/Pages/Presupuestos/Index.razor`
- `SPC.Web/Components/Pages/Facturas/Index.razor`
- `SPC.Web/Components/Pages/CuentaCorriente/Index.razor`
- `SPC.API/Services/CurrentAccountService.cs`
- `SPC.API/Services/CurrentAccount/CurrentAccountGuardrailOptions.cs`
- `SPC.API/appsettings.json`
- `SPC.Web/Components/Pages/CreditNotes/Detail.razor`
- `SPC.Web/Components/Pages/DebitNotes/Detail.razor`
- `SPC.Tests/Unit/CurrentAccountServiceTests.cs`
- `SPC.Web/Components/Pages/CreditNotes/Detail.razor`
- `SPC.Web/Components/Pages/DebitNotes/Detail.razor`
- `SPC.Web/Services/ApiService.cs`
- `SPC.Web/Services/IApiService.cs`
- `context/current_session.md`
- `context/session_2026-06.md`

### Architectural Impact
- Process/artifact-only change; no runtime code behavior changed.
- Clean Architecture guidance is now reflected in OpenSpec context.

### Validation
- Confirmed OpenSpec now contains the recreated config and project context files.
- `engram stats` found existing memory database at `C:\Users\Pablo\.engram/engram.db` with project `spc-software`.
- `engram sync --project spc-software` created `.engram/chunks/090e382d.jsonl.gz`, `.engram/chunks/9ec0d509.jsonl.gz`, and `.engram/manifest.json`.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after OpenSpec reset: 298/298 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after current account fix: 299/299 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after new-tab UX change: 300/300 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after quote direct navigation optimization: 301/301 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after official-document navigation expansion: 305/305 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after invoice tax display / range guardrail UI adjustment: 305/305 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after removing max date-span rejection: 306/306 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after NC/ND fallback route fix: 306/306 tests.
- `dotnet test SPC.Tests/SPC.Tests.csproj -c Release` passed after NC/ND tax breakdown display: 306/306 tests.
- Current working branch verified as `main`.

### Current Account Navigation Fix
- Fixed pending issue from 2026-03-31: navigation metadata now uses the resolved document type short code instead of the original `DocumentType` enum.
- Historical movements stored as `DocumentType.Other` but inferred as `FA`, `FB`, `NCA`, `NCB`, `NDA`, `NDB`, `PR`, or `PG` now expose openable navigation metadata.
- Added regression integration test for a legacy imported invoice movement.
- TDD evidence:
  - RED: `GetCurrentAccountMovements_UsesResolvedDocumentType_ForNavigationMetadata` failed because navigation was `initial-balance`.
  - GREEN: endpoint passes `resolvedType.ShortCode` into navigation metadata mapping.
  - VERIFY: full suite passed, 299/299 tests.

### Current Account New-Tab Navigation UX
- Changed current account movement document opening from internal `NavigationManager.NavigateTo` navigation to native links with `target="_blank"` and `rel="noopener noreferrer"`.
- This keeps the current account page open so users can continue browsing other source documents without losing filters/data context.
- TDD evidence:
  - RED: `OpenDocument_OpensTargetRouteInNewBrowserTab` initially captured the new-tab requirement before implementation.
  - GREEN/REFACTOR: replaced JS/navigation handling with native anchor links for immediate browser-managed tab opening.
  - VERIFY: full suite passed, 301/301 tests.

### Quote Direct Navigation and Search Optimization
- Investigated user-provided video/server log: quote navigation was slow because `/quotes?search=<number>` loaded the quote list and triggered `/api/quotes/buscar`, which sometimes took 18-30 seconds and returned 500.
- Current account quote metadata now routes to `/quotes/{quoteNumber}` instead of `/quotes?search={quoteNumber}`.
- Added `GET /api/quotes/by-number/{quoteNumber}` and Web `GetQuoteByNumberAsync` for direct quote detail loading.
- Added `/quotes/{QuoteNumber:long}` page route that loads the quote detail directly and skips list summary/search startup work.
- Optimized numeric quote search to match `QuoteNumber` only, avoiding expensive customer `Contains` predicates for numeric document searches.
- TDD evidence:
  - RED: Added integration coverage for quote-by-number detail retrieval and numeric exact quote search.
  - GREEN: Implemented API/service/Web route changes and current account route mapping.
  - VERIFY: focused impacted tests passed 20/20; full suite passed 301/301 tests.

### Official Document Direct Navigation Expansion
- Factura, NC, and ND movement routes now use official document identity when available: type `A/B`, point of sale, document number, and `customerId`.
- Current account descriptions like `Factura A 0002-00009866`, `Nota de Crédito A 0002-00008001`, and `Nota de Débito B 0003-00008101` are parsed to build direct routes.
- Added direct invoice API lookup: `/api/invoices/by-document/{invoiceType}/{invoiceNumber}?pointOfSale=...&customerId=...`.
- Extended NC/ND number endpoints and Web detail pages to accept `voucherType` and `pointOfSale` filters.
- Official formatted searches such as `A 0002-00009866`, `NC A 0002-00000001`, and `ND A 0002-00000001` now resolve by exact document identity instead of broad customer `Contains` searches.
- TDD evidence:
  - RED: Added/updated integration tests for official invoice lookup/search, NC/ND official search/filtering, and current account direct routes.
  - GREEN: Implemented parser, query filters, direct routes, and Web API/page wiring.
  - VERIFY: focused impacted tests passed 17/17; full suite passed 305/305 tests.

### Invoice Detail Tax Breakdown and Current Account Range Feedback
- Invoice detail modal now displays IVA discriminado, IVA incluido, and Percepción IIBB rows when present, before the total.
- No retention amount exists in current invoice DTO/model; only available invoice tax breakdown fields are VAT, included VAT, and IIBB perception.
- Web API service now handles current account `400 BadRequest` guardrail responses as expected warnings instead of logging them as failed fetch exceptions.
- Current account page displays a warning when the selected date range is rejected by guardrails.
- Validation: full suite passed, 305/305 tests.

### Current Account Full-History Range Policy
- Removed `MaxRangeDays` rejection from current account range searches.
- `MaxRows` now acts as a large-result warning threshold, not as a truncation/rejection limit.
- When a search exceeds the configured threshold, API returns all movements with `GuardrailMode = "warning"`, `WarningCode = "LARGE_RESULT"`, and a message indicating the result may be slow/large.
- UI shows the warning but still renders the full unpaginated history for scrolling.
- Validation: focused current-account tests passed 55/55; full suite passed 306/306 tests.

### NC/ND Fallback Route Fix
- User screenshot showed `/credit-notes/A/529?customerId=1370` rendering Blazor `Not Found`.
- Cause: current account fallback route can include voucher type and document number without point-of-sale, but NC/ND detail pages only accepted number-only or type + point-of-sale + number.
- Added NC/ND routes for type + number fallback:
  - `/credit-notes/{VoucherType}/{CreditNoteNumber:long}`
  - `/debit-notes/{VoucherType}/{DebitNoteNumber:long}`
- Validation: focused NC/ND/current-account tests passed 17/17; full suite passed 306/306 tests.

### NC/ND Tax Breakdown Display
- Credit note and debit note detail tables now show footer rows for subtotal, IVA, Percepción IIBB, discount, and total when those values are present.
- This matches invoice detail behavior and explains why line subtotals differ from the final document total.
- Validation: focused NC/ND endpoint tests passed 16/16; full suite passed 306/306 tests.

### Known Issues / Follow-ups
- No known current account navigation metadata issue remains from the 2026-03-31 pending item.

---

## Previous Session (2026-03-31)

Completed **document-type-inference-fix** - Fixed inference logic for historical current account movements with DocumentType=Other.

### Completed
- Enhanced `DocumentTypeResolver.InferFromDescription()` with precedence-based pattern matching.
- Changed from "exactly 1 match" to prioritized type resolution: Factura > NC > ND > Presupuesto > Pago.
- Added robust pattern detection for abbreviations (`nc `, `nd `, `fact`, etc.).
- Added comprehensive unit tests for inference scenarios including ambiguity and subcodes A/B.
- 298 tests passing (+25 new), build clean (0 errors, 0 warnings).

### Key Paths
- API: `SPC.API/`
- Web: `SPC.Web/`
- Tests: `SPC.Tests/`
- Models: `SPC.Shared/Models/`

### Commands
```bash
dotnet build SPC.slnx -c Release
dotnet test SPC.Tests/SPC.Tests.csproj -c Release
```


## Scope split and continuation — 2026-10-02

This is the active SPC 2.0 checkout: /home/pablo/Programmes/spc-software, feature/remitos-comunes, HEAD 2e3833b. Legacy VB6/Access reference and maintenance live under C:\Trabajos Activos (SPC-Core, SPC-Minimal, SPC-Retail). Keep Legacy session logs separate. Generate/review .NET Pi prompts here, under context/Prompts when saved. Read context/SPC_SCOPE_AND_CONTINUITY.md for the full inventory.

Engram #323 confirms the last recorded TEST warehouse finding. User-provided Pi handoff reports a later temporary association of warehouse 1 to Gabriel Peralta (salesperson 3) and 50 fictitious units of product code 110. These later values were not measured today; stock adjustment from the UI remains pending. Verify explicit SPC TEST identity and current quantities before resuming R5. Retry/idempotency, concurrency/duplicate and rollback validation remain open. No source/data changes, builds/tests, commit or push in this organization task.

Historical organization checkpoint (2026-10-02, superseded by the R5 closure above): user clarified all new .NET work with Pi must ultimately be on main. At that checkpoint, no branch switch/commit/merge/push had occurred and R5 evidence was still pending. Latest authorized scope now permits Remitos-only explicit-path commit, safe main integration/publication, and safe deletion of merged local branches; parent owns those actions.
