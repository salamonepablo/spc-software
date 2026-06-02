# Current Session Context

**Last Updated:** 2026-06-02
**Branch:** main
**Version:** 1.0.0
**Tests:** 306 passing (`dotnet test SPC.Tests/SPC.Tests.csproj -c Release`, 2026-06-02).

---

## Session Summary (2026-06-02)

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
