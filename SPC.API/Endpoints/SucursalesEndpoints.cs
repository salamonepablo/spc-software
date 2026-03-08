using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Endpoints;

public static class SucursalesEndpoints
{
    public static void MapSucursalesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sucursales", GetSucursales);
    }

    private static async Task<IResult> GetSucursales(SPCDbContext db)
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
