# Current Session Context

**Last Updated:** 2026-03-11
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
