# Session Log 2026-03

## 2026-03-13 - Orchestrator and Skills Baseline

- Scope: Rebuild AI orchestration baseline with backup, new AGENTS.md, and local skills.
- Files changed:
  - AGENTS.md
  - CLAUDE.md
  - skills/README.md
  - skills/session-sync/SKILL.md
  - skills/architecture-guard/SKILL.md
  - skills/tdd-feature/SKILL.md
- Backups created:
  - AGENTS_OLD.md
  - CLAUDE_OLD.md
- Architectural impact: Process-level only (no runtime code behavior changed).
- Tests updated: No.
- Validation: File structure and content updated successfully.
- Follow-ups:
  - Keep appending medium/high-impact changes to this monthly log.
  - Keep context/current_session.md in sync with latest project state.

## 2026-03-13 - Template, PR Checklist, and Bilingual Guide

- Scope: Add operational templates and collaborator guidance for the new orchestrator flow.
- Files changed:
  - context/session_entry_template.md
  - docs/pr-checklist.md
  - AGENTS_BILINGUAL.md
  - AGENTS.md
  - CLAUDE.md
  - context/current_session.md
- Architectural impact: Process-level governance improvement (documentation and workflow constraints only).
- Tests updated: No.
- Validation: New files created and integrated references verified.
- Follow-ups:
  - Use `context/session_entry_template.md` for every medium/high-impact session entry.
  - Apply `docs/pr-checklist.md` before merge decisions.

## 2026-03-13 - Quotes UX Improvements and English Routes

- Scope: Improve Quotes create/list UX, reduce heavy quote listing queries, and switch invoice/quote API/UI routes to English-first naming.
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
  - Presentation and API contract surface updated with English-first routes.
  - Backward compatibility preserved for Spanish routes (`/api/presupuestos`, `/api/facturas`, and existing Spanish page routes).
  - Quote query paths optimized in service layer via projection + no-tracking to reduce list/search overhead.
- Tests updated:
  - Integration: Added English route checks for quotes and invoices endpoints.
  - Regression: Added quote endpoint test to reject zero-total creation requests.
- Validation:
  - `dotnet test --verbosity minimal` passed (132 tests).
  - `dotnet build` passed (0 errors, 0 warnings).
- Follow-ups:
  - Confirm user-perceived latency with real production data volume and refine product loading strategy if needed.

## 2026-03-16 - Architecture Audit + Extract Auxiliary Endpoints

- Scope: Full architecture audit, then extract 6 inline/direct-DbContext endpoint groups into proper service + endpoint files following TDD.
- Routing decision: Explore agent for audit, direct implementation for extraction.
- Files created:
  - SPC.API/Services/IAuxiliaryTablesService.cs
  - SPC.API/Services/AuxiliaryTablesService.cs
  - SPC.API/Endpoints/AuxiliaryTablesEndpoints.cs
  - SPC.Tests/Unit/AuxiliaryTablesServiceTests.cs
- Files modified:
  - SPC.API/Program.cs (removed 53 lines of inline endpoints, added DI registration)
- Files removed:
  - SPC.API/Endpoints/SucursalesEndpoints.cs (superseded)
- Architectural impact:
  - Fixed DIP violation: 6 endpoint groups now use IAuxiliaryTablesService instead of direct SPCDbContext.
  - Fixed SRP violation: Program.cs is now a pure composition root (145 lines, down from 203).
  - No domain or persistence changes. Backward compatibility preserved for all Spanish API routes.
- TDD evidence:
  - RED: 8 unit tests written first, build failed as expected.
  - GREEN: Interface + implementation created, all 8 tests pass.
  - REFACTOR: Inline endpoints removed, dead file deleted.
- Tests added: 8 unit tests for AuxiliaryTablesService (TaxConditions, SalesReps CRUD, SalesZones, Categories, Warehouses, Branches).
- Validation:
  - `dotnet build` -> 0 errors, 0 warnings.
  - `dotnet test` -> 140/140 passed (132 existing + 8 new).
- Follow-ups:
  - Split InvoicesService/QuotesService into query/command services.
  - Introduce IRepository<T> abstraction.
  - Consider SPC.Application + SPC.Infrastructure project separation.

## 2026-03-16 - English Property Naming Convention Completion

- Scope: Rename all 89 Spanish entity properties to English across 13 model files, fix 5 malformed DbSet names, cascade through 60+ files.
- Routing decision: Explore agent (full mapping) + general agent (mass cascade).
- Files changed:
  - SPC.Shared/Models/ (13 files) - All entity properties renamed
  - SPC.API/Data/SPCDbContext.cs - DbSet renames + fluent API + seed data
  - SPC.API/Services/ (10+ files) - All service implementations
  - SPC.API/Contracts/ (11 files) - All request/response DTOs
  - SPC.Web/ (19 files) - ApiService, DTOs, Blazor pages
  - SPC.Tests/ (12 files) - Seed data, unit tests, integration tests
- Key rename examples:
  - Customer: RazonSocial->CompanyName, NombreFantasia->TradeName, PorcentajeDescuento->DiscountPercent, AlicuotaIIBB->IIBBPercent
  - Product: Codigo->Code, Descripcion->Description, PrecioInvoice->InvoicePrice, PrecioQuote->QuotePrice
  - Invoice: TipoInvoice->InvoiceType, NumeroInvoice->InvoiceNumber, ImporteIVA->VATAmount, Anulada->IsVoided
  - SalesRep: Legajo->EmployeeCode, Nombre->FirstName, PorcentajeComision->CommissionPercent
  - DbSets: CondicionesIva->TaxConditions, SalesRepes->SalesReps, Categorys->Categories, ZonasVenta->SalesZones, UnidadesMedida->UnitsOfMeasure
- Architectural impact:
  - Domain layer now fully English (zero Spanish properties).
  - API JSON responses now use English camelCase.
  - All Spanish API routes preserved for backward compatibility.
- Tests: 140/140 passed (no regressions).
- Validation: `dotnet build` 0 errors, 0 warnings.
- Follow-ups:
  - Create EF migration for production DB column renames.
  - Split InvoicesService/QuotesService.
  - Introduce IRepository<T>.
  - Consider SPC.Application + SPC.Infrastructure separation.

## 2026-03-17 - Spanish Response Language Directive

- Scope: Add mandatory rule requiring assistant responses in Spanish (Rioplatense/Argentine informal) to orchestration files.
- Routing decision: Direct implementation (governance/documentation change only).
- Files changed:
  - AGENTS.md - Added `### 5) Response Language` under Mandatory Project Rules
  - AGENTS_BILINGUAL.md - Added `### Response Language / Idioma de Respuesta` section
- Architectural impact: Process-level governance only. No runtime code changed.
- Tests updated: No (no code changes).
- Validation: No code changes; build/test status unchanged (140/140 passing).
- Follow-ups: Same as previous session.

## 2026-03-17 - Split InvoicesService into Query + Command (CQRS-lite)

- Scope: Split monolithic InvoicesService (414 LOC) into InvoiceQueryService (reads) + InvoiceCommandService (writes). Full TDD workflow.
- Routing decision: Explore agent (full architecture audit) + direct TDD implementation.
- Files created:
  - SPC.API/Services/IInvoiceQueryService.cs (7 query methods)
  - SPC.API/Services/IInvoiceCommandService.cs (2 command methods)
  - SPC.API/Services/InvoiceQueryService.cs
  - SPC.API/Services/InvoiceCommandService.cs
  - SPC.Tests/Unit/InvoiceQueryServiceTests.cs (10 tests)
  - SPC.Tests/Unit/InvoiceCommandServiceTests.cs (9 tests)
- Files modified:
  - SPC.API/Endpoints/FacturasEndpoints.cs (injects IInvoiceQueryService + IInvoiceCommandService)
  - SPC.API/Program.cs (DI updated)
- Files removed:
  - SPC.API/Services/IFacturasService.cs (superseded)
  - SPC.API/Services/FacturasService.cs (superseded)
- Architectural impact:
  - SRP: Query and command responsibilities cleanly separated.
  - DIP: Endpoints depend on focused interfaces.
  - Method names improved: AnularAsync->VoidAsync, GetByFechaAsync->GetByDateRangeAsync, GetResumenAsync->GetSummaryAsync.
  - All Spanish API routes preserved for backward compatibility.
- TDD evidence:
  - RED: 19 tests written first, build failed as expected.
  - GREEN: Implementations created, all 19 pass.
  - REFACTOR: Old monolithic service removed.
- Tests added: 19 unit tests (10 query + 9 command).
- Validation: `dotnet build` 0 errors; `dotnet test` 159/159 passed.
- Follow-ups:
  - F2: Split QuotesService.
  - F3: Split CreditNotesService.
  - F4: Split DebitNotesService.
  - F5: Introduce IRepository<T>.

## 2026-03-17 - Split QuotesService into Query + Command (CQRS-lite)

- Scope: Split monolithic QuotesService (411 LOC) into QuoteQueryService (reads) + QuoteCommandService (writes + current account). Full TDD.
- Routing decision: Direct TDD implementation (same pattern as F1).
- Files created:
  - SPC.API/Services/IQuoteQueryService.cs (7 query methods)
  - SPC.API/Services/IQuoteCommandService.cs (2 command methods)
  - SPC.API/Services/QuoteQueryService.cs
  - SPC.API/Services/QuoteCommandService.cs
  - SPC.Tests/Unit/QuoteQueryServiceTests.cs (10 tests)
  - SPC.Tests/Unit/QuoteCommandServiceTests.cs (9 tests)
- Files modified:
  - SPC.API/Endpoints/PresupuestosEndpoints.cs (dual injection)
  - SPC.API/Program.cs (DI updated)
- Files removed:
  - SPC.API/Services/IPresupuestosService.cs (superseded)
  - SPC.API/Services/PresupuestosService.cs (superseded)
- Architectural impact:
  - SRP: Query and command responsibilities cleanly separated. Current account logic stays in command service.
  - DIP: Endpoints depend on focused interfaces.
  - Method names improved: AnularAsync->VoidAsync, GetResumenAsync->GetSummaryAsync.
  - All Spanish API routes preserved.
- TDD evidence: RED (19 tests first) -> GREEN (implementations) -> REFACTOR (cleanup).
- Tests added: 19 unit tests (10 query + 9 command).
- Validation: `dotnet build` 0 errors; `dotnet test` 178/178 passed.
- Follow-ups:
  - F3: Split CreditNotesService.
  - F4: Split DebitNotesService.
  - F5: Introduce IRepository<T>.

- Scope: Add mandatory rule requiring assistant responses in Spanish (Rioplatense/Argentine informal) to orchestration files.
- Routing decision: Direct implementation (governance/documentation change only).
- Files changed:
  - AGENTS.md - Added `### 5) Response Language` under Mandatory Project Rules
  - AGENTS_BILINGUAL.md - Added `### Response Language / Idioma de Respuesta` section
- Architectural impact: Process-level governance only. No runtime code changed.
- Tests updated: No (no code changes).
- Validation: No code changes; build/test status unchanged (140/140 passing).
- Follow-ups: Same as previous session.

## 2026-03-19 - UI Presupuestos Autocomplete (Phase 1-3)

- Scope: Typeahead customer/product search for Quotes/Invoices, expose product prices in API, SalesRep Name mapping, and nowrap amounts.
- Routing decision: sdd-apply with TDD + architecture guard for API/UI changes.
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
  - Presentation updated (shared typeahead + create pages + lists).
  - Application/API updated (DTOs, endpoint mapping, search criteria/caps).
  - Domain unchanged; Clean Architecture boundaries preserved.
- TDD evidence:
  - RED: SalesRepResponseTests failed due to missing response type.
  - GREEN: Added SalesRepResponse mapping and product pricing fields; search tests green.
  - REFACTOR: Consolidated typeahead usage and nowrap styling in UI.
- Tests added/updated:
  - Unit: CustomersServiceTests, ProductsServiceTests, AuxiliaryTablesServiceTests (SalesRepResponse).
  - Integration: ClientesEndpointsTests, ProductosEndpointsTests.
- Validation:
  - dotnet test --filter "SalesRepResponseTests" (pass)
  - dotnet test --filter "CustomersServiceTests" (pass)
  - dotnet test --filter "ProductsServiceTests" (pass)
  - dotnet test --filter "BuscarCustomers_ReturnsMatchingCustomers_WhenSearchByInternalId" (pass)
  - dotnet test --filter "BuscarProducts_DoesNotReturn_WhenSearchBySupplierCodeOnly" (pass)
  - dotnet test --filter "BuscarProducts_IncludesPriceFields_InResponse" (pass)
- Risks / Follow-ups:
  - Phase 4: cleanup/refactor + manual UI verification (debounce/min length, nowrap).

## 2026-03-20 - Product Price Selection UX (Quotes + Invoices)

- Scope: Ensure product selection applies price on click/enter/blur exact match, remove duplicate price display, and preserve manual price edits.
- Files changed:
  - SPC.Web/Components/Shared/TypeaheadInput.razor
  - SPC.Web/Components/Pages/Presupuestos/Create.razor
  - SPC.Web/Components/Pages/Facturas/NewInvoice.razor
- Architectural impact: Presentation-only behavior changes; no domain or application layer changes.
- Tests updated: No (no Blazor test harness in SPC.Tests).
- Validation: Not run (UI-only change).
- Follow-ups:
  - Manual UX verification for typeahead exact-match selection and price override behavior.

## 2026-03-20 - Restore SucursalesEndpoints

- Scope: Restore deleted legacy endpoint file for branches (sucursales).
- Files changed:
  - SPC.API/Endpoints/SucursalesEndpoints.cs
- Architectural impact:
  - Presentation/API: Restored endpoint module file; no domain changes.
  - Backward compatibility: Preserves Spanish legacy route support.
- Tests updated: No.
- Validation: Not run (file restore only).
- Follow-ups: None.
