# Current Session Context

**Last Updated:** 2026-03-08 21:00
**Branch:** develop
**Version:** 0.2.0 (unreleased)

---

## Session Summary

Implemented CRUD endpoints for Invoices (Facturas), Credit Notes, Debit Notes, and Quotes (Presupuestos) with complete business rules for pricing, discounts, and VAT calculations following Argentine tax regulations.

Started creating Blazor UI forms for creating new invoices.

Key features:
- **Factura A**: Net prices + VAT discriminated + IIBB perception
- **Factura B**: Final prices with VAT included, shows "IVA Contenido"
- **IIBB Perception**: Only if company is agent AND customer has AlicuotaIIBB > 0
- **Multi-level discounts**: Customer default → Document → Line level
- **VAT immutability**: VAT% stored at document creation

## What Was Done Today (2026-03-08)

### API & Business Logic (Completed)
1. **CompanySettings Entity**
   - Created entity for company-level tax agent status
   - IsIIBBPerceptionAgent, IsIVAWithholdingAgent flags

2. **Cliente Updated**
   - Added AlicuotaIIBB, ProvinciaPadronIIBB for AFIP padrón

3. **PricingService Enhanced**
   - CalculateDocumentTypeA() for Factura A
   - CalculateDocumentTypeB() for Factura B (IVA Contenido)

4. **FacturasService Updated**
   - Selects correct price field based on document type
   - Handles IIBB perception logic

5. **Tests**
   - Fixed Factura A vs B test expectations
   - 111 tests passing

### Blazor UI (In Progress)
1. **NewInvoice.razor** - Form for creating new invoices
   - Type A/B selection
   - Customer, product, seller selection
   - Real-time totals calculation
   - Auto-select price based on invoice type

2. **API Service Updates**
   - Added CreateFacturaAsync method
   - Added GetSucursalesAsync method
   - Updated DTOs with new fields

3. **Sucursales Endpoint**
   - Created /api/sucursales endpoint

## In Progress
- Fix Blazor compilation issue with lambda in @onclick handlers
- Complete NewInvoice.razor form

## Test Results

```
dotnet test --no-restore
Correctas! - Con error: 0, Superado: 111, Omitido: 0, Total: 111
```

## Project Structure

```
spc-software/
├── SPC.API/           # REST API (ASP.NET Core 10)
├── SPC.Web/          # Blazor Frontend
├── SPC.Shared/       # Shared models (28 entities)
├── SPC.Tests/        # Test suite (111 tests)
├── SPC.Migration/    # Data migration tool
└── context/          # Session context files
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

1. `context/session_2026-03- Today's detailed session
08.md` -2. `README.md` - Project overview
3. `CHANGELOG.md` - Version history
