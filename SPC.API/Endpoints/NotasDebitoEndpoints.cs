using SPC.API.Contracts.DebitNotes;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Notas de Debito (Debit Notes)
/// </summary>
public static class DebitNotesEndpoints
{
    public static IEndpointRouteBuilder MapDebitNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notas-debito")
            .WithTags("DebitNotes");

        // ===========================================
        // CREATE
        // ===========================================

        // POST /api/notas-debito - Create new debit note
        group.MapPost("/", async (CreateDebitNoteRequest request, IDebitNotesService service) =>
        {
            try
            {
                var note = await service.CreateAsync(request);
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
        group.MapPost("/{id:int}/anular", async (int id, AnularDebitNoteRequest request, IDebitNotesService service) =>
        {
            var result = await service.AnularAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Nota de debito anulada correctamente" })
                : Results.NotFound(new { error = "Nota de debito no encontrada o ya anulada" });
        })
        .WithName("AnularDebitNote")
        .WithDescription("Voids a debit note (soft delete)");

        // ===========================================
        // QUERIES
        // ===========================================

        // GET /api/notas-debito - Get all debit notes (paginated)
        group.MapGet("/", async (int? skip, int? take, IDebitNotesService service) =>
        {
            var notes = await service.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(notes);
        })
        .WithName("GetDebitNotes")
        .WithDescription("Returns all debit notes (paginated, default 50)");

        // GET /api/notas-debito/count - Get total count
        group.MapGet("/count", async (IDebitNotesService service) =>
        {
            var count = await service.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetDebitNotesCount")
        .WithDescription("Returns total count of debit notes");

        // GET /api/notas-debito/{id} - Get debit note by ID with details
        group.MapGet("/{id:int}", async (int id, IDebitNotesService service) =>
        {
            var note = await service.GetByIdAsync(id);
            return note != null
                ? Results.Ok(note)
                : Results.NotFound(new { error = "Nota de debito no encontrada" });
        })
        .WithName("GetDebitNoteById")
        .WithDescription("Returns a debit note by ID with all details");

        // GET /api/notas-debito/cliente/{id} - Get debit notes by customer
        group.MapGet("/cliente/{customerId:int}", async (int customerId, IDebitNotesService service) =>
        {
            var notes = await service.GetByCustomerAsync(customerId);
            return Results.Ok(notes);
        })
        .WithName("GetDebitNotesByCustomer")
        .WithDescription("Returns all debit notes for a specific customer");

        // GET /api/notas-debito/fecha?desde=xxx&hasta=xxx - Get debit notes by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, IDebitNotesService service) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;
            
            var notes = await service.GetByDateRangeAsync(from, to);
            return Results.Ok(notes);
        })
        .WithName("GetDebitNotesByFecha")
        .WithDescription("Returns debit notes in a date range (default: last month)");

        // GET /api/notas-debito/buscar?termino=xxx - Search debit notes
        group.MapGet("/buscar", async (string? termino, IDebitNotesService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var notes = await service.SearchAsync(termino);
            return Results.Ok(notes);
        })
        .WithName("SearchDebitNotes")
        .WithDescription("Search debit notes by number or customer name/CUIT");

        return app;
    }
}
