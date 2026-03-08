using SPC.API.Contracts.NotasDebito;
using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for Notas de Debito (Debit Notes)
/// </summary>
public static class NotasDebitoEndpoints
{
    public static IEndpointRouteBuilder MapNotasDebitoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notas-debito")
            .WithTags("NotasDebito");

        // ===========================================
        // CREATE
        // ===========================================

        // POST /api/notas-debito - Create new debit note
        group.MapPost("/", async (CreateNotaDebitoRequest request, INotasDebitoService service) =>
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
        .WithName("CreateNotaDebito")
        .WithDescription("Creates a new debit note with full business rule calculations");

        // POST /api/notas-debito/{id}/anular - Void a debit note
        group.MapPost("/{id:int}/anular", async (int id, AnularNotaDebitoRequest request, INotasDebitoService service) =>
        {
            var result = await service.AnularAsync(id, request.Reason);
            return result
                ? Results.Ok(new { message = "Nota de debito anulada correctamente" })
                : Results.NotFound(new { error = "Nota de debito no encontrada o ya anulada" });
        })
        .WithName("AnularNotaDebito")
        .WithDescription("Voids a debit note (soft delete)");

        // ===========================================
        // QUERIES
        // ===========================================

        // GET /api/notas-debito - Get all debit notes (paginated)
        group.MapGet("/", async (int? skip, int? take, INotasDebitoService service) =>
        {
            var notes = await service.GetAllAsync(skip ?? 0, take ?? 50);
            return Results.Ok(notes);
        })
        .WithName("GetNotasDebito")
        .WithDescription("Returns all debit notes (paginated, default 50)");

        // GET /api/notas-debito/count - Get total count
        group.MapGet("/count", async (INotasDebitoService service) =>
        {
            var count = await service.GetCountAsync();
            return Results.Ok(new { total = count });
        })
        .WithName("GetNotasDebitoCount")
        .WithDescription("Returns total count of debit notes");

        // GET /api/notas-debito/{id} - Get debit note by ID with details
        group.MapGet("/{id:int}", async (int id, INotasDebitoService service) =>
        {
            var note = await service.GetByIdAsync(id);
            return note != null
                ? Results.Ok(note)
                : Results.NotFound(new { error = "Nota de debito no encontrada" });
        })
        .WithName("GetNotaDebitoById")
        .WithDescription("Returns a debit note by ID with all details");

        // GET /api/notas-debito/cliente/{id} - Get debit notes by customer
        group.MapGet("/cliente/{customerId:int}", async (int customerId, INotasDebitoService service) =>
        {
            var notes = await service.GetByCustomerAsync(customerId);
            return Results.Ok(notes);
        })
        .WithName("GetNotasDebitoByCliente")
        .WithDescription("Returns all debit notes for a specific customer");

        // GET /api/notas-debito/fecha?desde=xxx&hasta=xxx - Get debit notes by date range
        group.MapGet("/fecha", async (DateTime? desde, DateTime? hasta, INotasDebitoService service) =>
        {
            var from = desde ?? DateTime.Today.AddMonths(-1);
            var to = hasta ?? DateTime.Today;
            
            var notes = await service.GetByDateRangeAsync(from, to);
            return Results.Ok(notes);
        })
        .WithName("GetNotasDebitoByFecha")
        .WithDescription("Returns debit notes in a date range (default: last month)");

        // GET /api/notas-debito/buscar?termino=xxx - Search debit notes
        group.MapGet("/buscar", async (string? termino, INotasDebitoService service) =>
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Results.BadRequest(new { error = "Debe proporcionar un termino de busqueda" });

            var notes = await service.SearchAsync(termino);
            return Results.Ok(notes);
        })
        .WithName("SearchNotasDebito")
        .WithDescription("Search debit notes by number or customer name/CUIT");

        return app;
    }
}
