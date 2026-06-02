using SPC.API.Contracts.Invoices;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Invoices.
/// Delegates to IInvoiceQueryService (reads) and IInvoiceCommandService (writes).
/// </summary>
public static class InvoicesEndpoints
{
    public static IEndpointRouteBuilder MapInvoicesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices")
            .WithTags("Invoices");

        var legacyGroup = app.MapGroup("/api/facturas")
            .WithTags("Invoices");

        MapInvoiceRoutes(group, includeMetadata: true);
        MapInvoiceRoutes(legacyGroup, includeMetadata: false);

        return app;
    }

    private static void MapInvoiceRoutes(RouteGroupBuilder group, bool includeMetadata)
    {
        RouteHandlerBuilder builder;

        // ===========================================
        // COMMANDS (delegated to IInvoiceCommandService)
        // ===========================================

        // POST /api/invoices - Create new invoice
        builder = group.MapPost("/", async (CreateInvoiceRequest request, IInvoiceCommandService commandService) =>
        {
            try
            {
                var invoice = await commandService.CreateAsync(request);
                return Results.Created($"/api/invoices/{invoice.Id}", invoice);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        if (includeMetadata)
        {
            builder.WithName("CreateInvoice")
                .WithDescription("Creates a new invoice with full business rule calculations");
        }

        // POST /api/invoices/{id}/anular - Void an invoice
        builder = group.MapPost("/{id:int}/anular", async (int id, AnularInvoiceRequest request, IInvoiceCommandService commandService) =>
        {
            var result = await commandService.VoidAsync(id, request.Motivo);
            return result
                ? Results.Ok(new { message = "Invoice voided successfully" })
                : Results.NotFound(new { error = "Invoice not found or already voided" });
        });

        if (includeMetadata)
        {
            builder.WithName("VoidInvoice")
                .WithDescription("Voids an invoice (soft delete)");
        }

        // ===========================================
        // QUERIES (delegated to IInvoiceQueryService)
        // ===========================================

        // GET /api/invoices - Get all invoices (paginated)
        builder = group.MapGet("/", async (int? skip, int? take, IInvoiceQueryService queryService) =>
        {
            var invoices = await queryService.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(invoices);
        });

        if (includeMetadata)
        {
            builder.WithName("GetInvoices")
                .WithDescription("Returns all invoices (paginated, default 50)");
        }

        // GET /api/invoices/count - Get total count
        builder = group.MapGet("/count", async (IInvoiceQueryService queryService) =>
        {
            var count = await queryService.GetCountAsync();
            return Results.Ok(new { total = count });
        });

        if (includeMetadata)
        {
            builder.WithName("GetInvoicesCount")
                .WithDescription("Returns total count of invoices");
        }

        // GET /api/invoices/resumen - Get invoicing summary
        builder = group.MapGet("/resumen", async (IInvoiceQueryService queryService) =>
        {
            var summary = await queryService.GetSummaryAsync();
            return Results.Ok(summary);
        });

        if (includeMetadata)
        {
            builder.WithName("GetInvoicesSummary")
                .WithDescription("Returns invoicing summary statistics");
        }

        // GET /api/invoices/{id} - Get invoice by ID with details
        builder = group.MapGet("/{id:int}", async (int id, IInvoiceQueryService queryService) =>
        {
            var invoice = await queryService.GetByIdAsync(id);
            return invoice != null
                ? Results.Ok(invoice)
                : Results.NotFound(new { error = "Invoice not found" });
        });

        if (includeMetadata)
        {
            builder.WithName("GetInvoiceById")
                .WithDescription("Returns an invoice by ID with all details");
        }

        // GET /api/invoices/by-document/{invoiceType}/{invoiceNumber} - Get invoice by official document identity
        builder = group.MapGet("/by-document/{invoiceType}/{invoiceNumber:long}", async (
            string invoiceType,
            long invoiceNumber,
            int? pointOfSale,
            int? customerId,
            IInvoiceQueryService queryService) =>
        {
            var invoice = await queryService.GetByDocumentAsync(invoiceType, invoiceNumber, pointOfSale, customerId);
            return invoice != null
                ? Results.Ok(invoice)
                : Results.NotFound(new { error = "Invoice not found" });
        });

        if (includeMetadata)
        {
            builder.WithName("GetInvoiceByDocument")
                .WithDescription("Returns an invoice by official document type, point of sale, and number");
        }

        // GET /api/invoices/cliente/{id} - Get invoices by customer
        builder = group.MapGet("/cliente/{clienteId:int}", async (int clienteId, IInvoiceQueryService queryService) =>
        {
            var invoices = await queryService.GetByCustomerAsync(clienteId);
            return Results.Ok(invoices);
        });

        if (includeMetadata)
        {
            builder.WithName("GetInvoicesByCustomer")
                .WithDescription("Returns all invoices for a specific customer");
        }

        // GET /api/invoices/fecha?desde=xxx&hasta=xxx - Get invoices by date range
        builder = group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IInvoiceQueryService queryService) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;

            var invoices = await queryService.GetByDateRangeAsync(from, to);
            return Results.Ok(invoices);
        });

        if (includeMetadata)
        {
            builder.WithName("GetInvoicesByDateRange")
                .WithDescription("Returns invoices in a date range (default: last month)");
        }

        // GET /api/invoices/buscar?termino=xxx - Search invoices
        builder = group.MapGet("/buscar", async (string? termino, IInvoiceQueryService queryService) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Search term is required" });

            var invoices = await queryService.SearchAsync(termino);
            return Results.Ok(invoices);
        });

        if (includeMetadata)
        {
            builder.WithName("SearchInvoices")
                .WithDescription("Search invoices by number or customer name/CUIT");
        }
    }
}
