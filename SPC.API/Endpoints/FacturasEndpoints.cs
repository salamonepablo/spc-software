using SPC.API.Contracts.Facturas;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Facturas (Invoices)
/// </summary>
public static class FacturasEndpoints
{
    public static IEndpointRouteBuilder MapFacturasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/facturas")
            .WithTags("Facturas");

        // ===========================================
        // CREATE
        // ===========================================

        // POST /api/facturas - Create new invoice
        group.MapPost("/", async (CreateFacturaRequest request, IFacturasService service) =>
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
        .WithName("CreateFactura")
        .WithDescription("Creates a new invoice with full business rule calculations");

        // POST /api/facturas/{id}/anular - Void an invoice
        group.MapPost("/{id:int}/anular", async (int id, AnularFacturaRequest request, IFacturasService service) =>
        {
            var result = await service.AnularAsync(id, request.Motivo);
            return result
                ? Results.Ok(new { message = "Factura anulada correctamente" })
                : Results.NotFound(new { error = "Factura no encontrada o ya anulada" });
        })
        .WithName("AnularFactura")
        .WithDescription("Voids an invoice (soft delete)");

        // ===========================================
        // QUERIES
        // ===========================================

        // GET /api/facturas - Get all invoices (paginated)
        group.MapGet("/", async (int? skip, int? take, IFacturasService service) =>
        {
            var facturas = await service.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(facturas);
        })
        .WithName("GetFacturas")
        .WithDescription("Returns all invoices (paginated, default 50)");

        // GET /api/facturas/count - Get total count
        group.MapGet("/count", async (IFacturasService service) =>
        {
            var count = await service.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetFacturasCount")
        .WithDescription("Returns total count of invoices");

        // GET /api/facturas/resumen - Get invoicing summary
        group.MapGet("/resumen", async (IFacturasService service) =>
        {
            var resumen = await service.GetResumenAsync();
            return Results.Ok(resumen);
        })
        .WithName("GetFacturasResumen")
        .WithDescription("Returns invoicing summary statistics");

        // GET /api/facturas/{id} - Get invoice by ID with details
        group.MapGet("/{id:int}", async (int id, IFacturasService service) =>
        {
            var factura = await service.GetByIdAsync(id);
            return factura != null
                ? Results.Ok(factura)
                : Results.NotFound(new { error = "Factura no encontrada" });
        })
        .WithName("GetFacturaById")
        .WithDescription("Returns an invoice by ID with all details");

        // GET /api/facturas/cliente/{id} - Get invoices by customer
        group.MapGet("/cliente/{clienteId:int}", async (int clienteId, IFacturasService service) =>
        {
            var facturas = await service.GetByClienteAsync(clienteId);
            return Results.Ok(facturas);
        })
        .WithName("GetFacturasByCliente")
        .WithDescription("Returns all invoices for a specific customer");

        // GET /api/facturas/fecha?desde=xxx&hasta=xxx - Get invoices by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IFacturasService service) =>
        {
            var fechaDesde = desde ?? DateTime.Today.AddMonths(-1);
            var fechaHasta = hasta ?? DateTime.Today;
            
            var facturas = await service.GetByFechaAsync(fechaDesde, fechaHasta);
            return Results.Ok(facturas);
        })
        .WithName("GetFacturasByFecha")
        .WithDescription("Returns invoices in a date range (default: last month)");

        // GET /api/facturas/buscar?termino=xxx - Search invoices
        group.MapGet("/buscar", async (string? termino, IFacturasService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var facturas = await service.SearchAsync(termino);
            return Results.Ok(facturas);
        })
        .WithName("SearchFacturas")
        .WithDescription("Search invoices by number or customer name/CUIT");

        return app;
    }
}
