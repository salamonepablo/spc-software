using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Endpoints;

public static class BranchesEndpoints
{
    public static void MapBranchesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sucursales", GetBranches);
    }

    private static async Task<IResult> GetBranches(SPCDbContext db)
    {
        var sucursales = await db.Branches
            .Where(b => b.IsActive)
            .Select(b => new
            {
                b.Id,
                b.Code,
                b.Name,
                b.PointOfSale
            })
            .ToListAsync();

        return Results.Ok(sucursales);
    }
}
