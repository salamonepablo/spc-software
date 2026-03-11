using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Stock queries
/// </summary>
public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock")
            .WithTags("Stock");

        // GET /api/stock - Get all stock entries
        group.MapGet("/", async (IStockService service) =>
        {
            var stock = await service.GetAllAsync();
            return Results.Ok(stock);
        })
        .WithName("GetStock")
        .WithDescription("Returns all stock entries by product and warehouse");

        // GET /api/stock/resumen - Get stock summary by product
        group.MapGet("/resumen", async (IStockService service) =>
        {
            var resumen = await service.GetResumenAsync();
            return Results.Ok(resumen);
        })
        .WithName("GetStockResumen")
        .WithDescription("Returns stock summary by product (all warehouses combined)");

        // GET /api/stock/producto/{id} - Get stock for a specific product
        group.MapGet("/producto/{productoId:int}", async (int productoId, IStockService service) =>
        {
            var stock = await service.GetByProductAsync(productoId);
            return Results.Ok(stock);
        })
        .WithName("GetStockByProduct")
        .WithDescription("Returns stock for a specific product across all warehouses");

        // GET /api/stock/deposito/{id} - Get stock in a specific warehouse
        group.MapGet("/deposito/{depositoId:int}", async (int depositoId, IStockService service) =>
        {
            var stock = await service.GetByWarehouseAsync(depositoId);
            return Results.Ok(stock);
        })
        .WithName("GetStockByWarehouse")
        .WithDescription("Returns all stock in a specific warehouse");

        // GET /api/stock/bajominimo - Get products below minimum stock
        group.MapGet("/bajominimo", async (IStockService service) =>
        {
            var stock = await service.GetBajoMinimoAsync();
            return Results.Ok(stock);
        })
        .WithName("GetStockBajoMinimo")
        .WithDescription("Returns products with stock below minimum level");

        // GET /api/stock/buscar?termino=xxx - Search stock
        group.MapGet("/buscar", async (string? termino, IStockService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var stock = await service.SearchAsync(termino);
            return Results.Ok(stock);
        })
        .WithName("SearchStock")
        .WithDescription("Search stock by product code or description");

        return app;
    }
}
