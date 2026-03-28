using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service implementation for auxiliary/lookup table operations.
/// Consolidates query logic previously scattered across inline endpoints in Program.cs
/// and SucursalesEndpoints.cs.
/// </summary>
public class AuxiliaryTablesService : IAuxiliaryTablesService
{
    private readonly SPCDbContext _db;

    public AuxiliaryTablesService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<List<TaxCondition>> GetTaxConditionsAsync()
    {
        return await _db.TaxConditions.ToListAsync();
    }

    public async Task<List<SalesRep>> GetSalesRepsAsync()
    {
        return await _db.SalesReps
            .Where(v => v.IsActive)
            .OrderBy(v => v.FirstName)
            .ToListAsync();
    }

    public async Task<SalesRep> CreateSalesRepAsync(SalesRep salesRep)
    {
        salesRep.IsActive = true;
        _db.SalesReps.Add(salesRep);
        await _db.SaveChangesAsync();
        return salesRep;
    }

    public async Task<List<SalesZone>> GetSalesZonesAsync()
    {
        return await _db.SalesZones
            .Where(z => z.IsActive)
            .OrderBy(z => z.Name)
            .ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _db.Categories
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<List<Warehouse>> GetWarehousesAsync()
    {
        return await _db.Warehouses
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<Branch>> GetBranchesAsync()
    {
        return await _db.Branches
            .Where(b => b.IsActive)
            .ToListAsync();
    }

    public async Task<List<DocumentTypeMaster>> GetDocumentTypesAsync()
    {
        return await _db.DocumentTypes
            .Where(d => d.IsActive)
            .OrderBy(d => d.Code)
            .ToListAsync();
    }
}
