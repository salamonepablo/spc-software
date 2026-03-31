using SPC.API.Contracts.CreditNotes;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Notas de Credito (Credit Notes)
/// Uses CQRS-lite pattern: separate query and command services.
/// </summary>
public static class CreditNotesEndpoints
{
    public static IEndpointRouteBuilder MapCreditNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notas-credito")
            .WithTags("CreditNotes");

        // ===========================================
        // COMMANDS (write operations)
        // ===========================================

        // POST /api/notas-credito - Create new credit note
        group.MapPost("/", async (CreateCreditNoteRequest request, ICreditNoteCommandService commandService) =>
        {
            try
            {
                var note = await commandService.CreateAsync(request);
                return Results.Created($"/api/notas-credito/{note.Id}", note);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateCreditNote")
        .WithDescription("Creates a new credit note with full business rule calculations");

        // POST /api/notas-credito/{id}/anular - Void a credit note
        group.MapPost("/{id:int}/anular", async (int id, AnularCreditNoteRequest request, ICreditNoteCommandService commandService) =>
        {
            var result = await commandService.VoidAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Nota de credito IsVoided correctamente" })
                : Results.NotFound(new { error = "Nota de credito no encontrada o ya IsVoided" });
        })
        .WithName("AnularCreditNote")
        .WithDescription("Voids a credit note (soft delete)");

        // ===========================================
        // QUERIES (read operations)
        // ===========================================

        // GET /api/notas-credito - Get all credit notes (paginated)
        group.MapGet("/", async (int? skip, int? take, ICreditNoteQueryService queryService) =>
        {
            var notes = await queryService.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(notes);
        })
        .WithName("GetCreditNotes")
        .WithDescription("Returns all credit notes (paginated, default 50)");

        // GET /api/notas-credito/count - Get total count
        group.MapGet("/count", async (ICreditNoteQueryService queryService) =>
        {
            var count = await queryService.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetCreditNotesCount")
        .WithDescription("Returns total count of credit notes");

        // GET /api/notas-credito/number/{creditNoteNumber} - Get credit note by document number
        group.MapGet("/number/{creditNoteNumber:long}", async (
            long creditNoteNumber,
            int? customerId,
            ICreditNoteQueryService queryService) =>
        {
            var note = await queryService.GetByNumberAsync(creditNoteNumber, customerId);
            return note != null
                ? Results.Ok(note)
                : Results.NotFound(new { error = "Nota de credito no encontrada" });
        })
        .WithName("GetCreditNoteByNumber")
        .WithDescription("Returns a credit note by document number and optional customer filter");

        // GET /api/notas-credito/{id} - Get credit note by ID with details
        group.MapGet("/{id:int}", async (int id, ICreditNoteQueryService queryService) =>
        {
            var note = await queryService.GetByIdAsync(id);
            return note != null
                ? Results.Ok(note)
                : Results.NotFound(new { error = "Nota de credito no encontrada" });
        })
        .WithName("GetCreditNoteById")
        .WithDescription("Returns a credit note by ID with all details");

        // GET /api/notas-credito/cliente/{id} - Get credit notes by customer
        group.MapGet("/cliente/{customerId:int}", async (int customerId, ICreditNoteQueryService queryService) =>
        {
            var notes = await queryService.GetByCustomerAsync(customerId);
            return Results.Ok(notes);
        })
        .WithName("GetCreditNotesByCustomer")
        .WithDescription("Returns all credit notes for a specific customer");

        // GET /api/notas-credito/factura/{id} - Get credit notes by invoice
        group.MapGet("/factura/{invoiceId:int}", async (int invoiceId, ICreditNoteQueryService queryService) =>
        {
            var notes = await queryService.GetByInvoiceAsync(invoiceId);
            return Results.Ok(notes);
        })
        .WithName("GetCreditNotesByInvoice")
        .WithDescription("Returns all credit notes for a specific invoice");

        // GET /api/notas-credito/fecha?desde=xxx&hasta=xxx - Get credit notes by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, ICreditNoteQueryService queryService) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;

            var notes = await queryService.GetByDateRangeAsync(from, to);
            return Results.Ok(notes);
        })
        .WithName("GetCreditNotesByFecha")
        .WithDescription("Returns credit notes in a date range (default: last month)");

        // GET /api/notas-credito/buscar?termino=xxx - Search credit notes
        group.MapGet("/buscar", async (string? termino, ICreditNoteQueryService queryService) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var notes = await queryService.SearchAsync(termino);
            return Results.Ok(notes);
        })
        .WithName("SearchCreditNotes")
        .WithDescription("Search credit notes by number or customer name/CUIT");

        return app;
    }
}
