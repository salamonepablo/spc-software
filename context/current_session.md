# Current Session Context

**Last Updated:** 2026-03-04 19:20
**Branch:** develop
**Version:** 0.2.0 (unreleased)

---

## Session Summary

Completed CSV-based bulk migration from Access to SQL Server LocalDB. Added CSV importer, `--csv` switch, and skip logging for rows with missing Tipo/Nro. Updated CSV export to skip rows with missing Tipo/Nro and log them.

## What Was Done Today (2026-03-04)

1. **CSV Migration Flow**
   - Implemented CSV importer for all document tables
   - Added `--csv` switch in `SPC.Migration/Program.cs`
   - Added skip log for rows with missing Tipo/Nro

2. **CSV Export Validation**
   - Updated `export_access.py` to skip rows missing Tipo/Nro
   - Added `export_skipped_rows.log` for export audit

3. **Data Migration Completed**
   - Full document, payments, and current account import via CSV
   - Notas de Crédito now skip rows with missing Tipo/Nro and log them

## Migration Counts (SQL Server)

| Table | Records |
|------|---------|
| Clientes | 1451 |
| Productos | 125 |
| FacturaC / FacturaD | 10875 / 15651 |
| RemitoC / RemitoD | 9649 / 14386 |
| PresupuestoC / PresupuestoD | 32238 / 51610 |
| NotaCreditoC / NotaCreditoD | 689 / 726 |
| NotaDebitoC / NotaDebitoD | 137 / 253 |
| NotaDebitoIC / NotaDebitoID | 24 / 24 |
| ConsignacionesC / ConsignacionesD | 166 / 442 |
| PagoC / PagoD | 44285 / 59510 |
| CtaCte | 1438 |
| MovimientosCtaCte | 89255 |

## Current State

- **Branch:** develop
- **Database:** SQL Server LocalDB (SPC)
- **CSV Migration:** Completed with skip log at `SPC.Migration/data/migration_skipped_rows.log`
- **CSV Export:** Skips missing Tipo/Nro rows with log at `SPC.Migration/data/export_skipped_rows.log`

### Project Structure
```
spc-software/
├── SPC.API/           # REST API (ASP.NET Core 10)
├── SPC.Web/           # Blazor Frontend (template only)
├── SPC.Shared/        # Shared models (26 entities)
├── SPC.Tests/         # Test suite (39 tests)
├── SPC.Migration/     # Data migration tool (placeholder)
└── context/           # Session context files
```

## Phase 1 Progress

| Task | Status |
|------|--------|
| Configure SQL Server | Done |
| Update EF Core provider | Done |
| Create migrations | Done |
| Create entity models | Done (26 models) |
| Create enums | Done |
| Update DbContext | Done |
| Apply migration | Done |
| Add test suite | Done |
| Data migration script (Access -> SQL) | Done (CSV importer) |

## Phase 2 (Next)

- Re-export CSVs and confirm no skips
- Verify data in Blazor UI

## Useful Commands

```bash
# Run CSV migration
dotnet run --project SPC.Migration -- --csv
```

## Important Decisions Made

| Decision | Rationale |
|----------|-----------|
| InMemory DB for tests | Faster, isolated, no SQL Server dependency |
| Manual seed in tests | InMemory doesn't run HasData from migrations |
| Skip Migrate() in Testing | Migrate() is relational-only, breaks InMemory |
| Unique prefixes in search tests | Avoid cross-test data pollution |

## Files to Read for Full Context

1. `CLAUDE.md` - Complete AI guidelines
2. `README.md` - Project overview
3. `CHANGELOG.md` - Version history
4. `context/session_2026-03-02.md` - Today's detailed session
