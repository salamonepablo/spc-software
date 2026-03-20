# Current Session Context

**Last Updated:** 2026-03-13
**Branch:** develop
**Version:** 0.2.0

---

## Session Summary

Completed the **English naming convention migration** and finalized CSV-based data migration flow. All 111 tests passing.

Key changes:
- All entity classes now use English names (Customer, Invoice, Product, etc.)
- All services, DTOs, and contracts updated
- API routes remain in Spanish for backwards compatibility
- CSV migration is now the default and required path
- Missing CSV files are auto-generated via `export_access.py`
- Migration runner marked Windows-only to match OleDb dependencies
- Fixed Blazor compilation error in NewInvoice.razor

## What Was Done Today (2026-03-11)

### CSV Migration Flow (Completed)
1. **CSV import enforced** in `SPC.Migration/Program.cs`.
2. **Auto-export** from Access when CSV files are missing.
3. **Windows-only marker** added to avoid OleDb platform warnings.
4. **Test warning** resolved in `AuxiliaryEndpointsTests`.

### English Naming Convention (Completed)
1. **Entity Renames** - 28 models renamed:
   - Cliente -> Customer
   - Producto -> Product
   - Factura/FacturaDetalle -> Invoice/InvoiceDetail
   - Vendedor -> SalesRep
   - Deposito -> Warehouse
   - Rubro -> Category
   - UnidadMedida -> UnitOfMeasure
   - CondicionIva -> TaxCondition
   - ZonaVenta -> SalesZone
   - Remito/RemitoDetalle -> DeliveryNote/DeliveryNoteDetail
   - Presupuesto/PresupuestoDetalle -> Quote/QuoteDetail
   - NotaCredito/NotaCreditoDetalle -> CreditNote/CreditNoteDetail
   - NotaDebito/NotaDebitoDetalle -> DebitNote/DebitNoteDetail
   - Sucursal -> Branch
   - FormaPago -> PaymentMethod
   - CtaCte/MovimientoCtaCte -> CurrentAccount/CurrentAccountMovement

2. **Services Renamed**:
   - ClientesService -> CustomersService
   - ProductosService -> ProductsService
   - FacturasService -> InvoicesService
   - PresupuestosService -> QuotesService
   - NotasCreditoService -> CreditNotesService
   - NotasDebitoService -> DebitNotesService

3. **DbContext Updates**:
   - Added explicit EF Core relationship configurations
   - Fixed navigation property issues after rename

4. **Blazor Fix**:
   - Fixed NewInvoice.razor `@(null)` ambiguity error
   - Changed to `value=""` for proper compilation

### Documentation Updated
- README.md - Added naming convention table
- CHANGELOG.md - Documented v0.2.0 release with full rename details
- context/session_2026-03-10.md - Session log created

## Test Results

```
dotnet test --verbosity minimal
Correctas! - Con error: 0, Superado: 111, Omitido: 0, Total: 111
```

## Project Structure

```
spc-software/
├── SPC.API/           # REST API (ASP.NET Core 10)
├── SPC.Web/           # Blazor Frontend
├── SPC.Shared/        # Shared models (28 entities)
├── SPC.Tests/         # Test suite (111 tests)
├── SPC.Migration/     # Data migration tool
└── context/           # Session context files
```

## Phase Status

| Phase | Status |
|-------|--------|
| Phase 1: Infrastructure | Done |
| Phase 2: Queries (GET) | Done |
| Phase 3: Operations (CRUD) | In Progress |
| Phase 4: Invoicing (AFIP) | Pending |
| Phase 5: Finalization | Pending |

## Useful Commands

```bash
# Run API
cd SPC.API && dotnet run

# Run Web
cd SPC.Web && dotnet run

# Run tests
dotnet test

# Build
dotnet build
```

## Files to Read for Full Context

1. `context/session_2026-03-10.md` - Session history and addendum
2. `README.md` - Project overview
3. `CHANGELOG.md` - Version history

---

## Orchestrator Update (2026-03-13)

- Backups created for agent context files:
   - `AGENTS_OLD.md`
   - `CLAUDE_OLD.md`
- New baseline orchestration files created:
   - `AGENTS.md`
   - `CLAUDE.md`
- Local skills added:
   - `skills/session-sync/SKILL.md`
   - `skills/architecture-guard/SKILL.md`
   - `skills/tdd-feature/SKILL.md`
- New monthly session log initialized:
   - `context/session_2026-03.md`

Policy reinforced:
- Medium/high-impact changes must be documented in both `context/current_session.md` and `context/session_YYYY-MM.md`.
- Clean Architecture, SOLID, TDD, and English code naming are mandatory defaults.

---

## Orchestrator Tooling Update (2026-03-13)

- Added reusable logging template:
   - `context/session_entry_template.md`
- Added merge-quality checklist:
   - `docs/pr-checklist.md`
- Added bilingual collaborator guide:
   - `AGENTS_BILINGUAL.md`
- Integrated references in:
   - `AGENTS.md`
   - `CLAUDE.md`

Result:
- Session logging, PR validation, and bilingual onboarding are now first-class orchestrator assets.

---

## Session Update (2026-03-13) - Quotes UX and English Routes

- Scope: Improve Quotes usability/performance, switch invoices/quotes endpoints to English-first paths, and simplify quote number display.
- Files changed:
  - SPC.API/Endpoints/PresupuestosEndpoints.cs
  - SPC.API/Endpoints/FacturasEndpoints.cs
  - SPC.API/Services/PresupuestosService.cs
  - SPC.API/Program.cs
  - SPC.API/Contracts/Presupuestos/PresupuestoContracts.cs
  - SPC.Web/Services/ApiService.cs
  - SPC.Web/Components/Pages/Presupuestos/Create.razor
  - SPC.Web/Components/Pages/Presupuestos/Index.razor
  - SPC.Web/Components/Pages/Facturas/Index.razor
  - SPC.Web/Components/Pages/Facturas/NewInvoice.razor
  - SPC.Web/Components/Layout/NavMenu.razor
  - SPC.Web/Components/Pages/Home.razor
  - SPC.Tests/Integration/PresupuestosEndpointsTests.cs
  - SPC.Tests/Integration/FacturasEndpointsTests.cs
- Architectural impact:
  - Presentation: Blazor routes/navigation and Quotes form UX flow updated.
  - Application/API: English-first route groups (`/api/quotes`, `/api/invoices`) added with Spanish legacy aliases preserved.
  - Domain rules unchanged; service query strategy optimized with projection and no-tracking for quote list/search endpoints.
- TDD/tests:
  - Added integration coverage for new English API routes while retaining existing Spanish-route tests.
  - Added regression test for quote creation rejecting zero-total documents.
- Validation:
  - `dotnet test --verbosity minimal` -> Passed (132/132).
  - `dotnet build` -> Passed (0 errors, 0 warnings).
- Follow-ups:
  - Validate UX on a production-like dataset focusing on quote-create latency and list rendering behavior.

---

## Session Update (2026-03-16) - Extract Auxiliary Endpoints + Architecture Audit

### Routing Decision
- Codebase exploration via Explore agent, then direct implementation with TDD workflow.

### Architecture Audit Completed
- Full audit across all layers: Layer Separation (D), Domain Purity (C), DIP (D), SRP (C+), Contracts (B), Tests (B-), Naming (D).
- Top 5 priorities identified: split SPC.API into layers, introduce repositories, rename Spanish properties, extract inline endpoints, split large services.

### Extract Inline Endpoints (Completed via TDD)
- **Scope**: Extract 5 inline endpoints from `Program.cs` and 1 from `SucursalesEndpoints.cs` into proper `IAuxiliaryTablesService` + `AuxiliaryTablesEndpoints`.
- **Files created**:
  - `SPC.API/Services/IAuxiliaryTablesService.cs` - Interface with 7 methods
  - `SPC.API/Services/AuxiliaryTablesService.cs` - Implementation
  - `SPC.API/Endpoints/AuxiliaryTablesEndpoints.cs` - Consolidated endpoint module
  - `SPC.Tests/Unit/AuxiliaryTablesServiceTests.cs` - 8 unit tests
- **Files modified**:
  - `SPC.API/Program.cs` - Removed 53 lines of inline endpoints, added DI registration, removed unused import
- **Files removed**:
  - `SPC.API/Endpoints/SucursalesEndpoints.cs` - Superseded by AuxiliaryTablesEndpoints
- **Architectural impact**:
  - DIP: All auxiliary endpoints now use `IAuxiliaryTablesService` instead of direct `SPCDbContext` access.
  - SRP: `Program.cs` reduced from 203 to 145 lines (composition root only).
  - Backward compatibility: All Spanish API routes preserved (`/api/condicionesiva`, `/api/vendedores`, `/api/zonasventas`, `/api/rubros`, `/api/depositos`, `/api/sucursales`).
- **TDD Evidence**:
  - RED: Tests written first for `IAuxiliaryTablesService` (8 tests, build failed as expected)
  - GREEN: Interface + implementation created, all 8 tests pass
  - REFACTOR: Inline endpoints removed from Program.cs, SucursalesEndpoints deleted
- **Validation**:
  - `dotnet build` -> 0 errors, 0 warnings
  - `dotnet test` -> 140/140 passed (132 existing + 8 new)
- **Follow-ups**:
  - Split InvoicesService/QuotesService into query/command services
  - Introduce repository interfaces
  - Consider splitting SPC.API into SPC.Application + SPC.Infrastructure

---

## Session Update (2026-03-16) - English Property Naming Convention Completion

### Routing Decision
- Explore agent for full property mapping, general agent for mass cascade rename.

### Scope
- Renamed 89 Spanish entity properties to English across 13 model files.
- Fixed 5 malformed/Spanish DbSet names in SPCDbContext.
- Cascaded all renames through 60+ files across all layers.

### Files Changed

#### Domain Layer (SPC.Shared/Models/) - 13 files
- `Cliente.cs` (Customer) - 15 properties
- `Producto.cs` (Product) - 11 properties
- `Factura.cs` (Invoice) - 18 properties
- `FacturaDetalle.cs` (InvoiceDetail) - 5 properties
- `Remito.cs` (DeliveryNote) - 12 properties
- `RemitoDetalle.cs` (DeliveryNoteDetail) - 2 properties
- `Vendedor.cs` (SalesRep) - 15 properties
- `Deposito.cs` (Warehouse) - 5 properties
- `CondicionIva.cs` (TaxCondition) - 3 properties
- `Rubro.cs` (Category) - 3 properties
- `ZonaVenta.cs` (SalesZone) - 3 properties
- `UnidadMedida.cs` (UnitOfMeasure) - 2 properties
- `Stock.cs` (Stock) - 1 property

#### Infrastructure (SPC.API/Data/) - 1 file
- `SPCDbContext.cs` - 5 DbSet renames + all seed data and fluent API config

#### Application (SPC.API/Services/) - 10+ files
- All service implementations and interfaces

#### Contracts (SPC.API/Contracts/) - 11 files
- All request/response DTOs

#### Presentation (SPC.Web/) - 19 files
- ApiService, IApiService, all Web DTOs, all Blazor pages

#### Tests (SPC.Tests/) - 12 files
- SPCWebApplicationFactory seed data, all unit and integration tests

### Architectural Impact
- Domain layer now fully English - zero Spanish property names remain in entities.
- DbSet names corrected: `TaxConditions`, `SalesReps`, `SalesZones`, `Categories`, `UnitsOfMeasure`.
- API JSON responses now use English camelCase property names.
- Backward compatibility: All Spanish API routes still preserved.
- EF migration for column renames still pending (InMemory database used in tests).

### Validation
- `dotnet build` -> 0 errors, 0 warnings
- `dotnet test` -> 140/140 passed

### Follow-ups
- Create EF migration for database column renames (production DB alignment)
- Split InvoicesService/QuotesService into query/command services
- Introduce IRepository<T> abstraction
- Consider SPC.Application + SPC.Infrastructure project separation

---

## Session Update (2026-03-17) - Spanish Response Language Directive

### Routing Decision
- Direct implementation — governance/documentation change only, no code changes.

### Scope
- Added mandatory rule `### 5) Response Language` to `AGENTS.md` requiring assistant responses in Spanish (Rioplatense/Argentine informal).
- Updated `AGENTS_BILINGUAL.md` with matching bilingual section.

### Files Changed
- `AGENTS.md` - Added rule 5 (Response Language) under Mandatory Project Rules
- `AGENTS_BILINGUAL.md` - Added Response Language / Idioma de Respuesta section

### Architectural Impact
- Process-level only. No runtime code changed. No build/test impact.
- Clear separation maintained: code identifiers in English (rule 4), user-facing communication in Spanish (rule 5).

### Validation
- No code changes; build/test status unchanged (140/140 passing).

### Follow-ups
- Same as previous session (EF migration, service split, IRepository, project separation).

---

## Session Update (2026-03-17) - Split InvoicesService into Query + Command (CQRS-lite)

### Routing Decision
- Full architecture audit via Explore agent, then direct TDD implementation.

### Scope
- Split monolithic `InvoicesService` (414 LOC) into `InvoiceQueryService` (read-only) + `InvoiceCommandService` (write operations).
- Full TDD workflow: RED (tests first) -> GREEN (implementation) -> REFACTOR (cleanup).

### Files Created
- `SPC.API/Services/IInvoiceQueryService.cs` - Query interface (7 methods)
- `SPC.API/Services/IInvoiceCommandService.cs` - Command interface (2 methods)
- `SPC.API/Services/InvoiceQueryService.cs` - Query implementation
- `SPC.API/Services/InvoiceCommandService.cs` - Command implementation
- `SPC.Tests/Unit/InvoiceQueryServiceTests.cs` - 10 unit tests
- `SPC.Tests/Unit/InvoiceCommandServiceTests.cs` - 9 unit tests

### Files Modified
- `SPC.API/Endpoints/FacturasEndpoints.cs` - Updated to inject IInvoiceQueryService + IInvoiceCommandService
- `SPC.API/Program.cs` - Replaced IInvoicesService DI with IInvoiceQueryService + IInvoiceCommandService

### Files Removed
- `SPC.API/Services/IFacturasService.cs` - Superseded by IInvoiceQueryService + IInvoiceCommandService
- `SPC.API/Services/FacturasService.cs` - Superseded by InvoiceQueryService + InvoiceCommandService

### Architectural Impact
- **SRP**: Query operations (7 methods) cleanly separated from command operations (2 methods + business logic)
- **DIP**: Endpoints now depend on two focused interfaces instead of one monolithic interface
- **OCP**: Can extend queries independently from commands
- Backward compatibility: All API routes preserved (/api/invoices and /api/facturas legacy)
- Method names improved to English: `AnularAsync` -> `VoidAsync`, `GetByFechaAsync` -> `GetByDateRangeAsync`, `GetResumenAsync` -> `GetSummaryAsync`

### TDD Evidence
- RED: 19 unit tests written first (10 query + 9 command), build failed as expected
- GREEN: Implementations created, all 19 new tests pass
- REFACTOR: Old monolithic service removed, endpoint variable names cleaned up

### Validation
- `dotnet build` -> 0 errors, 0 warnings
- `dotnet test` -> 159/159 passed (140 existing + 19 new)

### Follow-ups
- F3: Split CreditNotesService into Query + Command
- F4: Split DebitNotesService into Query + Command
- F5: Introduce IRepository<T> abstraction

---

## Session Update (2026-03-17) - Split QuotesService into Query + Command (CQRS-lite)

### Routing Decision
- Direct TDD implementation (same pattern as F1).

### Scope
- Split monolithic `QuotesService` (411 LOC) into `QuoteQueryService` (read-only) + `QuoteCommandService` (write operations with current account impact).

### Files Created
- `SPC.API/Services/IQuoteQueryService.cs` - Query interface (7 methods)
- `SPC.API/Services/IQuoteCommandService.cs` - Command interface (2 methods)
- `SPC.API/Services/QuoteQueryService.cs` - Query implementation
- `SPC.API/Services/QuoteCommandService.cs` - Command implementation
- `SPC.Tests/Unit/QuoteQueryServiceTests.cs` - 10 unit tests
- `SPC.Tests/Unit/QuoteCommandServiceTests.cs` - 9 unit tests

### Files Modified
- `SPC.API/Endpoints/PresupuestosEndpoints.cs` - Updated to inject IQuoteQueryService + IQuoteCommandService
- `SPC.API/Program.cs` - Replaced IQuotesService DI with IQuoteQueryService + IQuoteCommandService

### Files Removed
- `SPC.API/Services/IPresupuestosService.cs` - Superseded
- `SPC.API/Services/PresupuestosService.cs` - Superseded

### Architectural Impact
- **SRP**: Query (7 methods) and command (2 methods + business logic + current account) cleanly separated
- **DIP**: Endpoints depend on focused interfaces
- Method names improved: `AnularAsync` -> `VoidAsync`, `GetResumenAsync` -> `GetSummaryAsync`
- Backward compatibility: All API routes preserved (/api/quotes and /api/presupuestos legacy)

### TDD Evidence
- RED: 19 unit tests written first, build failed
- GREEN: Implementations created, all pass
- REFACTOR: Old monolithic service removed

### Validation
- `dotnet build` -> 0 errors, 0 warnings
- `dotnet test` -> 178/178 passed (159 existing + 19 new)

### Follow-ups
- F3: Split CreditNotesService
- F4: Split DebitNotesService
- F5: Introduce IRepository<T>

---

## Session Update (2026-03-19) - UI Presupuestos Autocomplete (Phase 1-3)

- Scope: Add typeahead customer/product search for Quotes/Invoices, expose product prices in API, and show SalesRep Name; apply nowrap for amounts.
- Files changed:
  - SPC.API/Contracts/AuxiliaryTables/SalesRepResponse.cs
  - SPC.API/Contracts/Productos/ProductoResponse.cs
  - SPC.API/Endpoints/AuxiliaryTablesEndpoints.cs
  - SPC.API/Endpoints/ClientesEndpoints.cs
  - SPC.API/Endpoints/ProductosEndpoints.cs
  - SPC.API/Services/ClientesService.cs
  - SPC.API/Services/ProductosService.cs
  - SPC.Tests/Unit/CustomersServiceTests.cs
  - SPC.Tests/Unit/ProductsServiceTests.cs
  - SPC.Tests/Unit/AuxiliaryTablesServiceTests.cs
  - SPC.Tests/Integration/ClientesEndpointsTests.cs
  - SPC.Tests/Integration/ProductosEndpointsTests.cs
  - SPC.Web/Components/Shared/TypeaheadInput.razor
  - SPC.Web/Components/Pages/Presupuestos/Create.razor
  - SPC.Web/Components/Pages/Facturas/NewInvoice.razor
  - SPC.Web/Components/Pages/Presupuestos/Index.razor
  - SPC.Web/Components/Pages/Facturas/Index.razor
  - SPC.Web/Services/IApiService.cs
  - SPC.Web/Services/ApiService.cs
- Architectural impact:
  - Presentation: new shared typeahead component; Quotes/Invoices create UI updated.
  - Application/API: SalesRepResponse DTO added; product response extended; search criteria/caps enforced.
  - Domain unchanged; Spanish routes preserved.
- TDD/tests:
  - Added unit/integration tests for customer/product search and SalesRepResponse name trimming.
- Validation:
  - dotnet test --filter "SalesRepResponseTests" (pass)
  - dotnet test --filter "CustomersServiceTests" (pass)
  - dotnet test --filter "ProductsServiceTests" (pass)
  - dotnet test --filter "BuscarCustomers_ReturnsMatchingCustomers_WhenSearchByInternalId" (pass)
  - dotnet test --filter "BuscarProducts_DoesNotReturn_WhenSearchBySupplierCodeOnly" (pass)
  - dotnet test --filter "BuscarProducts_IncludesPriceFields_InResponse" (pass)
- Follow-ups:
  - Phase 4: refactor/verify per SDD tasks (cleanup + manual UI check).

---

## Session Update (2026-03-20) - Product Price Selection UX (Quotes + Invoices)

- Scope: Ensure product selection applies price on click/enter/blur exact match, remove duplicate price display, and preserve manual price edits.
- Files changed:
  - SPC.Web/Components/Shared/TypeaheadInput.razor
  - SPC.Web/Components/Pages/Presupuestos/Create.razor
  - SPC.Web/Components/Pages/Facturas/NewInvoice.razor
- Architectural impact:
  - Presentation-only behavior changes; no domain or application layer changes.
- Tests added/updated: No (no Blazor test harness in SPC.Tests).
- Validation: Not run (UI-only change).
- Follow-ups:
   - Manual UX verification for typeahead exact-match selection and price override behavior.

---

## Session Update (2026-03-20) - Restore SucursalesEndpoints

- Scope: Restore deleted legacy endpoint file for branches (sucursales).
- Files changed:
  - `SPC.API/Endpoints/SucursalesEndpoints.cs`
- Architectural impact:
  - Presentation/API: Restored endpoint module file; no domain changes.
  - Backward compatibility: Preserves Spanish legacy route support.
- Tests added/updated: No.
- Validation: Not run (file restore only).
- Follow-ups:
  - None.
