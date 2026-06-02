using SPC.API.Contracts.Quotes;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Quotes.
/// Delegates to IQuoteQueryService (reads) and IQuoteCommandService (writes).
/// </summary>
public static class QuotesEndpoints
{
    public static IEndpointRouteBuilder MapQuotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes")
            .WithTags("Quotes");

        var legacyGroup = app.MapGroup("/api/presupuestos")
            .WithTags("Quotes");

        MapQuoteRoutes(group, includeMetadata: true);
        MapQuoteRoutes(legacyGroup, includeMetadata: false);

        return app;
    }

    private static void MapQuoteRoutes(RouteGroupBuilder group, bool includeMetadata)
    {
        RouteHandlerBuilder builder;

        // ===========================================
        // COMMANDS (delegated to IQuoteCommandService)
        // ===========================================

        // POST /api/quotes - Create new quote
        builder = group.MapPost("/", async (CreateQuoteRequest request, IQuoteCommandService commandService) =>
        {
            try
            {
                var quote = await commandService.CreateAsync(request);
                return Results.Created($"/api/quotes/{quote.Id}", quote);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        if (includeMetadata)
        {
            builder.WithName("CreateQuote")
                .WithDescription("Creates a new quote with pricing calculations");
        }

        // POST /api/quotes/{id}/anular - Void a quote
        builder = group.MapPost("/{id:int}/anular", async (int id, AnularQuoteRequest request, IQuoteCommandService commandService) =>
        {
            var result = await commandService.VoidAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Quote voided successfully" })
                : Results.NotFound(new { error = "Quote not found or already voided" });
        });

        if (includeMetadata)
        {
            builder.WithName("VoidQuote")
                .WithDescription("Voids a quote (soft delete) and reverses current account impact");
        }

        // ===========================================
        // QUERIES (delegated to IQuoteQueryService)
        // ===========================================

        // GET /api/quotes - Get all quotes (paginated)
        builder = group.MapGet("/", async (int? skip, int? take, IQuoteQueryService queryService) =>
        {
            var quotes = await queryService.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(quotes);
        });

        if (includeMetadata)
        {
            builder.WithName("GetQuotes")
                .WithDescription("Returns all quotes (paginated, default 50)");
        }

        // GET /api/quotes/count - Get total count
        builder = group.MapGet("/count", async (IQuoteQueryService queryService) =>
        {
            var count = await queryService.GetCountAsync();
            return Results.Ok(new { total = count });
        });

        if (includeMetadata)
        {
            builder.WithName("GetQuotesCount")
                .WithDescription("Returns total count of quotes");
        }

        // GET /api/quotes/resumen - Get summary statistics
        builder = group.MapGet("/resumen", async (IQuoteQueryService queryService) =>
        {
            var summary = await queryService.GetSummaryAsync();
            return Results.Ok(summary);
        });

        if (includeMetadata)
        {
            builder.WithName("GetQuotesSummary")
                .WithDescription("Returns summary statistics (today, month, year)");
        }

        // GET /api/quotes/{id} - Get quote by ID with details
        builder = group.MapGet("/{id:int}", async (int id, IQuoteQueryService queryService) =>
        {
            var quote = await queryService.GetByIdAsync(id);
            return quote != null
                ? Results.Ok(quote)
                : Results.NotFound(new { error = "Quote not found" });
        });

        if (includeMetadata)
        {
            builder.WithName("GetQuoteById")
                .WithDescription("Returns a quote by ID with all details");
        }

        // GET /api/quotes/by-number/{quoteNumber} - Get quote by document number with details
        builder = group.MapGet("/by-number/{quoteNumber:long}", async (long quoteNumber, IQuoteQueryService queryService) =>
        {
            var quote = await queryService.GetByNumberAsync(quoteNumber);
            return quote != null
                ? Results.Ok(quote)
                : Results.NotFound(new { error = "Quote not found" });
        });

        if (includeMetadata)
        {
            builder.WithName("GetQuoteByNumber")
                .WithDescription("Returns a quote by quote number with all details");
        }

        // GET /api/quotes/cliente/{id} - Get quotes by customer
        builder = group.MapGet("/cliente/{customerId:int}", async (int customerId, IQuoteQueryService queryService) =>
        {
            var quotes = await queryService.GetByCustomerAsync(customerId);
            return Results.Ok(quotes);
        });

        if (includeMetadata)
        {
            builder.WithName("GetQuotesByCustomer")
                .WithDescription("Returns all quotes for a specific customer");
        }

        // GET /api/quotes/fecha?desde=xxx&hasta=xxx - Get quotes by date range
        builder = group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IQuoteQueryService queryService) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;

            var quotes = await queryService.GetByDateRangeAsync(from, to);
            return Results.Ok(quotes);
        });

        if (includeMetadata)
        {
            builder.WithName("GetQuotesByDateRange")
                .WithDescription("Returns quotes in a date range (default: last month)");
        }

        // GET /api/quotes/buscar?termino=xxx - Search quotes
        builder = group.MapGet("/buscar", async (string? termino, IQuoteQueryService queryService) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Search term is required" });

            var quotes = await queryService.SearchAsync(termino);
            return Results.Ok(quotes);
        });

        if (includeMetadata)
        {
            builder.WithName("SearchQuotes")
                .WithDescription("Search quotes by number or customer name");
        }
    }
}
