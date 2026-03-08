using SPC.API.Contracts.Presupuestos;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Presupuestos (Quotes)
/// </summary>
public static class PresupuestosEndpoints
{
    public static IEndpointRouteBuilder MapPresupuestosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/presupuestos")
            .WithTags("Presupuestos");

        // ===========================================
        // CREATE
        // ===========================================

        // POST /api/presupuestos - Create new quote
        group.MapPost("/", async (CreatePresupuestoRequest request, IPresupuestosService service) =>
        {
            try
            {
                var quote = await service.CreateAsync(request);
                return Results.Created($"/api/presupuestos/{quote.Id}", quote);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreatePresupuesto")
        .WithDescription("Creates a new quote with pricing calculations");

        // POST /api/presupuestos/{id}/anular - Void a quote
        group.MapPost("/{id:int}/anular", async (int id, AnularPresupuestoRequest request, IPresupuestosService service) =>
        {
            var result = await service.AnularAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Presupuesto anulado correctamente" })
                : Results.NotFound(new { error = "Presupuesto no encontrado o ya anulado" });
        })
        .WithName("AnularPresupuesto")
        .WithDescription("Voids a quote (soft delete)");

        // ===========================================
        // QUERIES
        // ===========================================

        // GET /api/presupuestos - Get all quotes (paginated)
        group.MapGet("/", async (int? skip, int? take, IPresupuestosService service) =>
        {
            var quotes = await service.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(quotes);
        })
        .WithName("GetPresupuestos")
        .WithDescription("Returns all quotes (paginated, default 50)");

        // GET /api/presupuestos/count - Get total count
        group.MapGet("/count", async (IPresupuestosService service) =>
        {
            var count = await service.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetPresupuestosCount")
        .WithDescription("Returns total count of quotes");

        // GET /api/presupuestos/{id} - Get quote by ID with details
        group.MapGet("/{id:int}", async (int id, IPresupuestosService service) =>
        {
            var quote = await service.GetByIdAsync(id);
            return quote != null
                ? Results.Ok(quote)
                : Results.NotFound(new { error = "Presupuesto no encontrado" });
        })
        .WithName("GetPresupuestoById")
        .WithDescription("Returns a quote by ID with all details");

        // GET /api/presupuestos/cliente/{id} - Get quotes by customer
        group.MapGet("/cliente/{customerId:int}", async (int customerId, IPresupuestosService service) =>
        {
            var quotes = await service.GetByCustomerAsync(customerId);
            return Results.Ok(quotes);
        })
        .WithName("GetPresupuestosByCliente")
        .WithDescription("Returns all quotes for a specific customer");

        // GET /api/presupuestos/fecha?desde=xxx&hasta=xxx - Get quotes by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IPresupuestosService service) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;
            
            var quotes = await service.GetByDateRangeAsync(from, to);
            return Results.Ok(quotes);
        })
        .WithName("GetPresupuestosByFecha")
        .WithDescription("Returns quotes in a date range (default: last month)");

        // GET /api/presupuestos/buscar?termino=xxx - Search quotes
        group.MapGet("/buscar", async (string? termino, IPresupuestosService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var quotes = await service.SearchAsync(termino);
            return Results.Ok(quotes);
        })
        .WithName("SearchPresupuestos")
        .WithDescription("Search quotes by number or customer name");

        return app;
    }
}
