using SPC.API.Contracts.AuxiliaryTables;
using System.Linq;
using SPC.API.Services;
using SPC.Shared.Models;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for auxiliary/lookup tables:
/// TaxConditions, SalesReps, SalesZones, Categories, Warehouses, Branches.
/// Routes are preserved in Spanish for backward compatibility.
/// </summary>
public static class AuxiliaryTablesEndpoints
{
    public static IEndpointRouteBuilder MapAuxiliaryTablesEndpoints(this IEndpointRouteBuilder app)
    {
        // ----- Tax Conditions -----
        app.MapGet("/api/TaxConditions", async (IAuxiliaryTablesService service) =>
        {
            var conditions = await service.GetTaxConditionsAsync();
            return Results.Ok(conditions);
        })
        .WithTags("Auxiliary Tables")
        .WithName("GetTaxConditions")
        .WithDescription("Returns all tax conditions (IVA)");

        // ----- Sales Reps -----
        app.MapGet("/api/vendedores", async (IAuxiliaryTablesService service) =>
        {
            var reps = await service.GetSalesRepsAsync();
            var response = reps.Select(SalesRepResponse.From).ToList();
            return Results.Ok(response);
        })
        .WithTags("Auxiliary Tables")
        .WithName("GetSalesReps")
        .WithDescription("Returns all active sales reps ordered by name");

        app.MapPost("/api/vendedores", async (SalesRep salesRep, IAuxiliaryTablesService service) =>
        {
            var created = await service.CreateSalesRepAsync(salesRep);
            return Results.Created($"/api/vendedores/{created.Id}", SalesRepResponse.From(created));
        })
        .WithTags("Auxiliary Tables")
        .WithName("CreateSalesRep")
        .WithDescription("Creates a new sales rep");

        // ----- Sales Zones -----
        app.MapGet("/api/zonasventas", async (IAuxiliaryTablesService service) =>
        {
            var zones = await service.GetSalesZonesAsync();
            return Results.Ok(zones);
        })
        .WithTags("Auxiliary Tables")
        .WithName("GetSalesZones")
        .WithDescription("Returns all active sales zones ordered by name");

        // ----- Categories -----
        app.MapGet("/api/rubros", async (IAuxiliaryTablesService service) =>
        {
            var categories = await service.GetCategoriesAsync();
            return Results.Ok(categories);
        })
        .WithTags("Auxiliary Tables")
        .WithName("GetCategories")
        .WithDescription("Returns all active categories ordered by name");

        // ----- Warehouses -----
        app.MapGet("/api/depositos", async (IAuxiliaryTablesService service) =>
        {
            var warehouses = await service.GetWarehousesAsync();
            return Results.Ok(warehouses);
        })
        .WithTags("Auxiliary Tables")
        .WithName("GetWarehouses")
        .WithDescription("Returns all active warehouses ordered by name");

        // ----- Branches -----
        app.MapGet("/api/sucursales", async (IAuxiliaryTablesService service) =>
        {
            var branches = await service.GetBranchesAsync();
            return Results.Ok(branches);
        })
        .WithTags("Auxiliary Tables")
        .WithName("GetBranches")
        .WithDescription("Returns all active branches");

        // ----- Document Types -----
        app.MapGet("/api/documenttypes", async (IAuxiliaryTablesService service) =>
        {
            var documentTypes = await service.GetDocumentTypesAsync();
            var response = documentTypes.Select(DocumentTypeResponse.From).ToList();
            return Results.Ok(response);
        })
        .WithTags("Auxiliary Tables")
        .WithName("GetDocumentTypes")
        .WithDescription("Returns all active document types ordered by code");

        return app;
    }
}
