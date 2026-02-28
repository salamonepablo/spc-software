# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Versioning

- **MAJOR** version: Incompatible API changes
- **MINOR** version: New functionality (backwards compatible)
- **PATCH** version: Bug fixes (backwards compatible)

## [Unreleased]

### Planned
- SQL Server migration
- Complete Blazor UI for Customers and Products
- Invoice endpoints with stock logic
- AFIP electronic invoicing integration
- Windows Authentication implementation

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
| 0.1.0 | 2026-02-28 | Initial release - Project structure and basic CRUD |

---

## Links

- [Repository](https://github.com/salamonepablo/spc-software)
- [Issues](https://github.com/salamonepablo/spc-software/issues)

[Unreleased]: https://github.com/salamonepablo/spc-software/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/salamonepablo/spc-software/releases/tag/v0.1.0
