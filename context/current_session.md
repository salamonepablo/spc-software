# Current Session Context

**Last Updated:** 2026-03-31
**Branch:** develop
**Version:** 0.2.0
**Tests:** 298 passing

---

## Session Summary (2026-03-31)

Completed **document-type-inference-fix** - Fixed inference logic for historical current account movements with DocumentType=Other.

### Completed
- Enhanced DocumentTypeResolver.InferFromDescription() with precedence-based pattern matching
- Changed from "exactly 1 match" to prioritized type resolution (Factura > NC > ND > Presupuesto > Pago)
- Added robust pattern detection for abbreviations ("nc ", "nd ", "fact", etc.)
- Added comprehensive unit tests for inference scenarios including ambiguity and subcodes A/B
- 298 tests passing (+25 new), build clean (0 errors, 0 warnings)

### Files Changed
- `SPC.API/Services/CurrentAccount/DocumentTypeResolver.cs` - Precedence-based inference logic
- `SPC.Tests/Unit/DocumentTypeInferenceTests.cs` - 15 inference test cases (NEW)
- `SPC.Tests/Unit/CurrentAccountDocumentTypeResolverTests.cs` - Updated for new behavior

### Known Issues
- **Navigation Bug**: BuildNavigationMetadata uses original DocumentType enum value (99=Other) instead of resolved type, causing canOpen=false for historical movements that should be navigable. Fix pending for next session.

### Previous Session (2026-03-29)
- Period-aware running balance calculation
- Period initial/final balance calculation (sum movements before dateFrom)
- Recalculate running balance dynamically for filtered period
- UI: Saldo Inicial/Final del Período above/below grid
- Single "Saldo Parcial" column (TotalRunningBalance)
- Default filter: last 12 months

---

## Quick Reference

### Key Paths
- API: `SPC.API/`
- Web: `SPC.Web/`
- Tests: `SPC.Tests/`
- Models: `SPC.Shared/Models/`

### Commands
```bash
dotnet build SPC.slnx -c Release
dotnet test SPC.Tests/SPC.Tests.csproj -c Release
engram search "sdd/" --project "spc-software"
```

### Current Account Files
- Service: `SPC.API/Services/CurrentAccountService.cs`
- Resolver: `SPC.API/Services/CurrentAccount/DocumentTypeResolver.cs`
- Endpoint: `SPC.API/Endpoints/CurrentAccountEndpoints.cs`
- UI: `SPC.Web/Components/Pages/CuentaCorriente/Index.razor`
- Tests: `SPC.Tests/Unit/CurrentAccountServiceTests.cs`
- Tests: `SPC.Tests/Unit/DocumentTypeInferenceTests.cs`
