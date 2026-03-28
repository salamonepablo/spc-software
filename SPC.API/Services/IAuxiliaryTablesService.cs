using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service for querying and managing auxiliary/lookup tables:
/// TaxConditions, SalesReps, SalesZones, Categories, Warehouses, Branches.
/// </summary>
public interface IAuxiliaryTablesService
{
    /// <summary>Returns all tax conditions.</summary>
    Task<List<TaxCondition>> GetTaxConditionsAsync();

    /// <summary>Returns all active sales reps, ordered by name.</summary>
    Task<List<SalesRep>> GetSalesRepsAsync();

    /// <summary>Creates a new sales rep (sets active = true).</summary>
    Task<SalesRep> CreateSalesRepAsync(SalesRep salesRep);

    /// <summary>Returns all active sales zones, ordered by name.</summary>
    Task<List<SalesZone>> GetSalesZonesAsync();

    /// <summary>Returns all active categories, ordered by name.</summary>
    Task<List<Category>> GetCategoriesAsync();

    /// <summary>Returns all active warehouses, ordered by name.</summary>
    Task<List<Warehouse>> GetWarehousesAsync();

    /// <summary>Returns all active branches.</summary>
    Task<List<Branch>> GetBranchesAsync();

    /// <summary>Returns all active document types, ordered by code.</summary>
    Task<List<DocumentTypeMaster>> GetDocumentTypesAsync();
}
