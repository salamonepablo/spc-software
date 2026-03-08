using SPC.API.Contracts.NotasCredito;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Notas de Credito (Credit Notes)
/// </summary>
public static class NotasCreditoEndpoints
{
    public static IEndpointRouteBuilder MapNotasCreditoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notas-credito")
            .WithTags("NotasCredito");

        // ===========================================
        // CREATE
        // ===========================================

        // POST /api/notas-credito - Create new credit note
        group.MapPost("/", async (CreateNotaCreditoRequest request, INotasCreditoService service) =>
        {
            try
            {
                var note = await service.CreateAsync(request);
                return Results.Created($"/api/notas-credito/{note.Id}", note);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateNotaCredito")
        .WithDescription("Creates a new credit note with full business rule calculations");

        // POST /api/notas-credito/{id}/anular - Void a credit note
        group.MapPost("/{id:int}/anular", async (int id, AnularNotaCreditoRequest request, INotasCreditoService service) =>
        {
            var result = await service.AnularAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Nota de credito anulada correctamente" })
                : Results.NotFound(new { error = "Nota de credito no encontrada o ya anulada" });
        })
        .WithName("AnularNotaCredito")
        .WithDescription("Voids a credit note (soft delete)");

        // ===========================================
        // QUERIES
        // ===========================================

        // GET /api/notas-credito - Get all credit notes (paginated)
        group.MapGet("/", async (int? skip, int? take, INotasCreditoService service) =>
        {
            var notes = await service.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(notes);
        })
        .WithName("GetNotasCredito")
        .WithDescription("Returns all credit notes (paginated, default 50)");

        // GET /api/notas-credito/count - Get total count
        group.MapGet("/count", async (INotasCreditoService service) =>
        {
            var count = await service.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetNotasCreditoCount")
        .WithDescription("Returns total count of credit notes");

        // GET /api/notas-credito/{id} - Get credit note by ID with details
        group.MapGet("/{id:int}", async (int id, INotasCreditoService service) =>
        {
            var note = await service.GetByIdAsync(id);
            return note != null
                ? Results.Ok(note)
                : Results.NotFound(new { error = "Nota de credito no encontrada" });
        })
        .WithName("GetNotaCreditoById")
        .WithDescription("Returns a credit note by ID with all details");

        // GET /api/notas-credito/cliente/{id} - Get credit notes by customer
        group.MapGet("/cliente/{customerId:int}", async (int customerId, INotasCreditoService service) =>
        {
            var notes = await service.GetByCustomerAsync(customerId);
            return Results.Ok(notes);
        })
        .WithName("GetNotasCreditoByCliente")
        .WithDescription("Returns all credit notes for a specific customer");

        // GET /api/notas-credito/factura/{id} - Get credit notes by invoice
        group.MapGet("/factura/{invoiceId:int}", async (int invoiceId, INotasCreditoService service) =>
        {
            var notes = await service.GetByInvoiceAsync(invoiceId);
            return Results.Ok(notes);
        })
        .WithName("GetNotasCreditoByFactura")
        .WithDescription("Returns all credit notes for a specific invoice");

        // GET /api/notas-credito/fecha?desde=xxx&hasta=xxx - Get credit notes by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, INotasCreditoService service) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;
            
            var notes = await service.GetByDateRangeAsync(from, to);
            return Results.Ok(notes);
        })
        .WithName("GetNotasCreditoByFecha")
        .WithDescription("Returns credit notes in a date range (default: last month)");

        // GET /api/notas-credito/buscar?termino=xxx - Search credit notes
        group.MapGet("/buscar", async (string? termino, INotasCreditoService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var notes = await service.SearchAsync(termino);
            return Results.Ok(notes);
        })
        .WithName("SearchNotasCredito")
        .WithDescription("Search credit notes by number or customer name/CUIT");

        return app;
    }
}
