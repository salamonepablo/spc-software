using SPC.API.Services;

namespace SPC.API.Endpoints;

public static class BranchesEndpoints
{
    public static void MapBranchesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sucursales", GetBranches);
    }

    private static async Task<IResult> GetBranches(IAuxiliaryTablesService auxiliaryTablesService)
    {
        var sucursales = (await auxiliaryTablesService.GetBranchesAsync())
            .Select(b => new
            {
                b.Id,
                b.Code,
                b.Name,
                b.PointOfSale
            })
            .ToList();

        return Results.Ok(sucursales);
    }
}
