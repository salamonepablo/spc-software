using SPC.API.Contracts.DebitNotes;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Notas de Debito (Debit Notes)
/// Uses CQRS-lite pattern: separate query and command services.
/// </summary>
public static class DebitNotesEndpoints
{
    public static IEndpointRouteBuilder MapDebitNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notas-debito")
            .WithTags("DebitNotes");

        // ===========================================
        // COMMANDS (write operations)
        // ===========================================

        // POST /api/notas-debito - Create new debit note
        group.MapPost("/", async (CreateDebitNoteRequest request, IDebitNoteCommandService commandService) =>
        {
            try
            {
                var note = await commandService.CreateAsync(request);
                return Results.Created($"/api/notas-debito/{note.Id}", note);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateDebitNote")
        .WithDescription("Creates a new debit note with full business rule calculations");

        // POST /api/notas-debito/{id}/anular - Void a debit note
        group.MapPost("/{id:int}/anular", async (int id, AnularDebitNoteRequest request, IDebitNoteCommandService commandService) =>
        {
            var result = await commandService.VoidAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Nota de debito IsVoided correctamente" })
                : Results.NotFound(new { error = "Nota de debito no encontrada o ya IsVoided" });
        })
        .WithName("AnularDebitNote")
        .WithDescription("Voids a debit note (soft delete)");

        // ===========================================
        // QUERIES (read operations)
        // ===========================================

        // GET /api/notas-debito - Get all debit notes (paginated)
        group.MapGet("/", async (int? skip, int? take, IDebitNoteQueryService queryService) =>
        {
            var notes = await queryService.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(notes);
        })
        .WithName("GetDebitNotes")
        .WithDescription("Returns all debit notes (paginated, default 50)");

        // GET /api/notas-debito/count - Get total count
        group.MapGet("/count", async (IDebitNoteQueryService queryService) =>
        {
            var count = await queryService.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetDebitNotesCount")
        .WithDescription("Returns total count of debit notes");

        // GET /api/notas-debito/number/{debitNoteNumber} - Get debit note by document number
        group.MapGet("/number/{debitNoteNumber:long}", async (
            long debitNoteNumber,
            int? customerId,
            IDebitNoteQueryService queryService) =>
        {
            var note = await queryService.GetByNumberAsync(debitNoteNumber, customerId);
            return note != null
                ? Results.Ok(note)
                : Results.NotFound(new { error = "Nota de debito no encontrada" });
        })
        .WithName("GetDebitNoteByNumber")
        .WithDescription("Returns a debit note by document number and optional customer filter");

        // GET /api/notas-debito/{id} - Get debit note by ID with details
        group.MapGet("/{id:int}", async (int id, IDebitNoteQueryService queryService) =>
        {
            var note = await queryService.GetByIdAsync(id);
            return note != null
                ? Results.Ok(note)
                : Results.NotFound(new { error = "Nota de debito no encontrada" });
        })
        .WithName("GetDebitNoteById")
        .WithDescription("Returns a debit note by ID with all details");

        // GET /api/notas-debito/cliente/{id} - Get debit notes by customer
        group.MapGet("/cliente/{customerId:int}", async (int customerId, IDebitNoteQueryService queryService) =>
        {
            var notes = await queryService.GetByCustomerAsync(customerId);
            return Results.Ok(notes);
        })
        .WithName("GetDebitNotesByCustomer")
        .WithDescription("Returns all debit notes for a specific customer");

        // GET /api/notas-debito/fecha?desde=xxx&hasta=xxx - Get debit notes by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IDebitNoteQueryService queryService) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;

            var notes = await queryService.GetByDateRangeAsync(from, to);
            return Results.Ok(notes);
        })
        .WithName("GetDebitNotesByFecha")
        .WithDescription("Returns debit notes in a date range (default: last month)");

        // GET /api/notas-debito/buscar?termino=xxx - Search debit notes
        group.MapGet("/buscar", async (string? termino, IDebitNoteQueryService queryService) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var notes = await queryService.SearchAsync(termino);
            return Results.Ok(notes);
        })
        .WithName("SearchDebitNotes")
        .WithDescription("Search debit notes by number or customer name/CUIT");

        return app;
    }
}
