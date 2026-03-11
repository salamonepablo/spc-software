using SPC.API.Contracts.Invoices;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Invoices (Invoices)
/// </summary>
public static class InvoicesEndpoints
{
    public static IEndpointRouteBuilder MapInvoicesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/facturas")
            .WithTags("Invoices");

        // ===========================================
        // CREATE
        // ===========================================

        // POST /api/facturas - Create new invoice
        group.MapPost("/", async (CreateInvoiceRequest request, IInvoicesService service) =>
        {
            try
            {
                var factura = await service.CreateAsync(request);
                return Results.Created($"/api/facturas/{factura.Id}", factura);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateInvoice")
        .WithDescription("Creates a new invoice with full business rule calculations");

        // POST /api/facturas/{id}/anular - Void an invoice
        group.MapPost("/{id:int}/anular", async (int id, AnularInvoiceRequest request, IInvoicesService service) =>
        {
            var result = await service.AnularAsync(id, request.Motivo);
            return result
                ? Results.Ok(new { message = "Invoice anulada correctamente" })
                : Results.NotFound(new { error = "Invoice no encontrada o ya anulada" });
        })
        .WithName("AnularInvoice")
        .WithDescription("Voids an invoice (soft delete)");

        // ===========================================
        // QUERIES
        // ===========================================

        // GET /api/facturas - Get all invoices (paginated)
        group.MapGet("/", async (int? skip, int? take, IInvoicesService service) =>
        {
            var facturas = await service.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(facturas);
        })
        .WithName("GetInvoices")
        .WithDescription("Returns all invoices (paginated, default 50)");

        // GET /api/facturas/count - Get total count
        group.MapGet("/count", async (IInvoicesService service) =>
        {
            var count = await service.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetInvoicesCount")
        .WithDescription("Returns total count of invoices");

        // GET /api/facturas/resumen - Get invoicing summary
        group.MapGet("/resumen", async (IInvoicesService service) =>
        {
            var resumen = await service.GetResumenAsync();
            return Results.Ok(resumen);
        })
        .WithName("GetInvoicesResumen")
        .WithDescription("Returns invoicing summary statistics");

        // GET /api/facturas/{id} - Get invoice by ID with details
        group.MapGet("/{id:int}", async (int id, IInvoicesService service) =>
        {
            var factura = await service.GetByIdAsync(id);
            return factura != null
                ? Results.Ok(factura)
                : Results.NotFound(new { error = "Invoice no encontrada" });
        })
        .WithName("GetInvoiceById")
        .WithDescription("Returns an invoice by ID with all details");

        // GET /api/facturas/cliente/{id} - Get invoices by customer
        group.MapGet("/cliente/{clienteId:int}", async (int clienteId, IInvoicesService service) =>
        {
            var facturas = await service.GetByCustomerAsync(clienteId);
            return Results.Ok(facturas);
        })
        .WithName("GetInvoicesByCustomer")
        .WithDescription("Returns all invoices for a specific customer");

        // GET /api/facturas/fecha?desde=xxx&hasta=xxx - Get invoices by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IInvoicesService service) =>
        {
            var fechaDesde = desde ?? DateTime.Today.AddMonths(-1);
            var fechaHasta = hasta ?? DateTime.Today;
            
            var facturas = await service.GetByFechaAsync(fechaDesde, fechaHasta);
            return Results.Ok(facturas);
        })
        .WithName("GetInvoicesByFecha")
        .WithDescription("Returns invoices in a date range (default: last month)");

        // GET /api/facturas/buscar?termino=xxx - Search invoices
        group.MapGet("/buscar", async (string? termino, IInvoicesService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var facturas = await service.SearchAsync(termino);
            return Results.Ok(facturas);
        })
        .WithName("SearchInvoices")
        .WithDescription("Search invoices by number or customer name/CUIT");

        return app;
    }
}
