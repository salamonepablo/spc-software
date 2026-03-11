using SPC.API.Contracts.Quotes;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Quotes (Quotes)
/// </summary>
public static class QuotesEndpoints
{
    public static IEndpointRouteBuilder MapQuotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/presupuestos")
            .WithTags("Quotes");

        // ===========================================
        // CREATE
        // ===========================================

        // POST /api/presupuestos - Create new quote
        group.MapPost("/", async (CreateQuoteRequest request, IQuotesService service) =>
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
        .WithName("CreateQuote")
        .WithDescription("Creates a new quote with pricing calculations");

        // POST /api/presupuestos/{id}/anular - Void a quote
        group.MapPost("/{id:int}/anular", async (int id, AnularQuoteRequest request, IQuotesService service) =>
        {
            var result = await service.AnularAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Quote anulado correctamente" })
                : Results.NotFound(new { error = "Quote no encontrado o ya anulado" });
        })
        .WithName("AnularQuote")
        .WithDescription("Voids a quote (soft delete)");

        // ===========================================
        // QUERIES
        // ===========================================

        // GET /api/presupuestos - Get all quotes (paginated)
        group.MapGet("/", async (int? skip, int? take, IQuotesService service) =>
        {
            var quotes = await service.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(quotes);
        })
        .WithName("GetQuotes")
        .WithDescription("Returns all quotes (paginated, default 50)");

        // GET /api/presupuestos/count - Get total count
        group.MapGet("/count", async (IQuotesService service) =>
        {
            var count = await service.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetQuotesCount")
        .WithDescription("Returns total count of quotes");

        // GET /api/presupuestos/{id} - Get quote by ID with details
        group.MapGet("/{id:int}", async (int id, IQuotesService service) =>
        {
            var quote = await service.GetByIdAsync(id);
            return quote != null
                ? Results.Ok(quote)
                : Results.NotFound(new { error = "Quote no encontrado" });
        })
        .WithName("GetQuoteById")
        .WithDescription("Returns a quote by ID with all details");

        // GET /api/presupuestos/cliente/{id} - Get quotes by customer
        group.MapGet("/cliente/{customerId:int}", async (int customerId, IQuotesService service) =>
        {
            var quotes = await service.GetByCustomerAsync(customerId);
            return Results.Ok(quotes);
        })
        .WithName("GetQuotesByCustomer")
        .WithDescription("Returns all quotes for a specific customer");

        // GET /api/presupuestos/fecha?desde=xxx&hasta=xxx - Get quotes by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IQuotesService service) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;
            
            var quotes = await service.GetByDateRangeAsync(from, to);
            return Results.Ok(quotes);
        })
        .WithName("GetQuotesByFecha")
        .WithDescription("Returns quotes in a date range (default: last month)");

        // GET /api/presupuestos/buscar?termino=xxx - Search quotes
        group.MapGet("/buscar", async (string? termino, IQuotesService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var quotes = await service.SearchAsync(termino);
            return Results.Ok(quotes);
        })
        .WithName("SearchQuotes")
        .WithDescription("Search quotes by number or customer name");

        return app;
    }
}
