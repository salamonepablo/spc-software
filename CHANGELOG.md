# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versioning

- **MAJOR** version: Incompatible API changes
- **MINOR** version: New functionality (backwards compatible)
- **PATCH** version: Bug fixes (backwards compatible)

## [Unreleased]

_No unreleased changes._

## [1.0.0] - 2026-03-31

### Major Release - Current Account Module Complete

This release marks the first stable version of SPC Software with the Current Account module fully implemented and verified against the legacy system.

### Added

#### Current Account Module
- **DocumentTypeResolver**: Maps known document types to canonical codes (FA, NC, ND, PG, PR, etc.)
- **DocumentTypeCatalogVerifier**: Startup validation ensuring catalog integrity
- **Explicit search flow**: UI requires date range selection + button click (no auto-load)
- **Full period retrieval**: Removed fixed `take=50` limit for complete movement history
- **Movement drill-down**: Navigate from movements to payment/note details
- **Balance verification**: Saldos match legacy system exactly (L1, L2, Total)

#### API Endpoints
- `GET /api/cuenta-corriente/{customerId}` - Account summary
- `GET /api/cuenta-corriente/{customerId}/movements` - Movements with date range
- `GET /api/cuenta-corriente/{customerId}/balance` - Current balance (L1/L2)
- `POST /api/cuenta-corriente/{customerId}/initial-balance` - Set initial balance

#### Architecture
- CQRS-lite pattern: Services split into Query + Command
- DocumentTypeMaster entity with catalog seed data
- Two EF Core migrations for catalog upgrade and guardrails

#### Testing
- 298 tests (up from 111)
- Unit tests for DocumentTypeResolver
- Unit tests for CurrentAccountService
- Integration tests for CurrentAccountEndpoints
- DocumentTypeCatalogValidation tests

#### UI
- Blazor UI for Products, Invoices, Quotes, Stock
- Current Account view with movement list and balance display
- Document type badges with color coding

### Changed
- CSV migration is now the default and required path for data imports
- Automatic CSV generation via `SPC.Migration/export_access.py` when files are missing
- SQL Server migrations consolidated under `SPC.API/Migrations`

### Fixed
- Document types no longer collapse to "Otros" (OT)
- Test nullability warning in `AuxiliaryEndpointsTests`

## [0.2.0] - 2026-03-10

### Major Changes

#### English Naming Convention
- **Renamed all Spanish identifiers to English** across 101+ files
- Class names: `Cliente` -> `Customer`, `Factura` -> `Invoice`, `Producto` -> `Product`
- Services: `ClientesService` -> `CustomersService`, `FacturasService` -> `InvoicesService`
- DTOs: All contracts updated to use English names
- API routes remain in Spanish for backwards compatibility (`/api/clientes`, `/api/facturas`)

#### Entity Renames
| Spanish | English |
|---------|---------|
| Cliente | Customer |
| Producto | Product |
| Factura / FacturaDetalle | Invoice / InvoiceDetail |
| Vendedor | SalesRep |
| Deposito | Warehouse |
| Rubro | Category |
| UnidadMedida | UnitOfMeasure |
| CondicionIva | TaxCondition |
| ZonaVenta | SalesZone |
| Remito / RemitoDetalle | DeliveryNote / DeliveryNoteDetail |
| Presupuesto / PresupuestoDetalle | Quote / QuoteDetail |
| NotaCredito / NotaCreditoDetalle | CreditNote / CreditNoteDetail |
| NotaDebito / NotaDebitoDetalle | DebitNote / DebitNoteDetail |
| Sucursal | Branch |
| FormaPago | PaymentMethod |
| CtaCte / MovimientoCtaCte | CurrentAccount / CurrentAccountMovement |

### Added
- SQL Server LocalDB migration (from SQLite)
- 12 new entity models:
  - Branch (sucursales)
  - CustomerAddress (multiple delivery addresses)
  - PaymentMethod (cash, check, transfer, barter)
  - Quote / QuoteDetail (presupuestos)
  - CreditNote / CreditNoteDetail (notas de credito)
  - DebitNote / DebitNoteDetail (notas de debito fiscal)
  - InternalDebitNote / InternalDebitNoteDetail (debitos internos)
  - Payment / PaymentDetail (pagos)
  - CurrentAccount / CurrentAccountMovement (cuenta corriente dual)
  - Consignment / ConsignmentDetail (consignaciones)
  - CasualDeliveryNote / CasualDeliveryNoteDetail (remitos temporales)
  - StockMovement / StockMovementDetail (movimientos de stock)
- Enums: VoucherType, DocumentType, AccountLineType, AddressType, PaymentMethodType
- Licensing system with feature flags (DualLineCurrentAccount, MultiBranch)
- License API endpoint (/api/license)
- Comprehensive test suite (111 tests):
  - Integration tests for all API endpoints
  - Unit tests for LicenseService
  - SPCWebApplicationFactory for isolated testing
- Seed data for Branches and PaymentMethods
- Full CRUD for Invoices, Credit Notes, Debit Notes, Quotes
- Business rules for Invoice A vs B, IIBB perception, multi-level discounts
- PricingService for document calculations
- CompanySettings entity for tax agent configuration

### Changed
- Updated Customer model with navigation to new documents
- Updated SalesRep model with Legajo, CUIL, personal data
- Updated Warehouse model with AssociatedSalesRep (for trucks)
- Updated Invoice model with Branch, discounts, DeliveryNotes navigation
- Updated DeliveryNote model with Branch, BusinessUnit
- DbContext now includes all entity configurations with decimal precision
- Explicit EF Core relationship configurations added to SPCDbContext

### Fixed
- Blazor NewInvoice.razor compilation error (null ambiguity in input value)

### Technical
- Switched from SQLite to SQL Server LocalDB
- Added Microsoft.EntityFrameworkCore.SqlServer
- EF Core migrations for SQL Server
- InMemory database for test isolation
- All 111 tests passing after rename

### Planned
- Blazor UI for Products and Invoicing
- AFIP/ARCA electronic invoicing integration
- Windows Authentication implementation
- PDF generation with QR codes

---

## [0.1.0] - 2026-02-28

### Added
- Initial project structure with 3 projects:
  - `SPC.API` - REST API with Minimal APIs
  - `SPC.Shared` - Shared models library
  - `SPC.Web` - Blazor Server frontend
- Entity Framework Core 10 with SQLite
- Basic models:
  - Customer (Cliente)
  - Product (Producto)
  - Invoice / InvoiceDetail (Factura / FacturaDetalle)
  - DeliveryNote / DeliveryNoteDetail (Remito / RemitoDetalle)
  - Stock
  - TaxCondition (CondicionIva)
  - SalesRep (Vendedor)
  - SalesZone (ZonaVenta)
  - Category (Rubro)
  - UnitOfMeasure (UnidadMedida)
  - Warehouse (Deposito)
- Customer API endpoints (full CRUD)
- Product API endpoints (GET, POST)
- Auxiliary data endpoints (TaxConditions, SalesReps, SalesZones, Categories, Warehouses)
- Seed data for TaxConditions, UnitsOfMeasure, Warehouses, Categories
- CORS configuration for Blazor consumption
- Project documentation:
  - `README.md` - Project documentation
  - `CHANGELOG.md` - Version history
  - `CLAUDE.md` - AI assistant guidelines
  - `.gitignore` - Git ignore rules for .NET
- Original Access database documentation in `/docs/`

### Technical Details
- .NET 10.0
- Entity Framework Core 10.0.3
- SQLite for development database
- Minimal API pattern (no controllers)

---

## Version History Summary

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0 | 2026-03-31 | Current Account module, 298 tests, CQRS-lite, full UI |
| 0.2.0 | 2026-03-10 | English naming convention, 28 models, 111 tests, full CRUD |
| 0.1.0 | 2026-02-28 | Initial release - Project structure and basic CRUD |

---

## Links

- [Repository](https://github.com/salamonepablo/spc-software)
- [Issues](https://github.com/salamonepablo/spc-software/issues)

[Unreleased]: https://github.com/salamonepablo/spc-software/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/salamonepablo/spc-software/compare/v0.2.0...v1.0.0
[0.2.0]: https://github.com/salamonepablo/spc-software/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/salamonepablo/spc-software/releases/tag/v0.1.0
