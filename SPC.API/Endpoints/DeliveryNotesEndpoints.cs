using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.DeliveryNotes;
using SPC.API.Services;

namespace SPC.API.Endpoints;

public static class DeliveryNotesEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-notes").WithTags("Delivery Notes");
        group.MapGet("/", async ([AsParameters] DeliveryNoteSearchRequest request, IDeliveryNoteQueryService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.SearchAsync(request, cancellationToken)); }
            catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        }).WithName("SearchDeliveryNotes");
        group.MapGet("/{id:int}", async (int id, IDeliveryNoteQueryService service, CancellationToken cancellationToken) =>
        {
            var note = await service.GetByIdAsync(id, cancellationToken);
            return note is null ? Results.NotFound() : Results.Ok(note);
        }).WithName("GetDeliveryNote");
        group.MapGet("/{id:int}/pdf", async (int id, IDeliveryNoteQueryService query, IDeliveryNotePdfService pdf, HttpContext context, CancellationToken cancellationToken) =>
        {
            var note = await query.GetByIdAsync(id, cancellationToken);
            if (note is null) return Results.NotFound();
            context.Response.Headers.ContentDisposition = $"inline; filename=remito-{note.DeliveryNoteNumber}.pdf";
            return Results.File(pdf.Create(note), "application/pdf");
        }).WithName("ViewDeliveryNotePdf");
        group.MapGet("/{id:int}/pdf/download", async (int id, IDeliveryNoteQueryService query, IDeliveryNotePdfService pdf, CancellationToken cancellationToken) =>
        {
            var note = await query.GetByIdAsync(id, cancellationToken);
            return note is null ? Results.NotFound() : Results.File(pdf.Create(note), "application/pdf", $"remito-{note.DeliveryNoteNumber}.pdf");
        }).WithName("DownloadDeliveryNotePdf");
        group.MapGet("/next-number", async (int branchId, IDeliveryNoteCommandService service) =>
        {
            try { return Results.Ok(await service.GetNextNumberAsync(branchId)); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithName("GetNextDeliveryNoteNumber");
        group.MapPost("/", async (CreateDeliveryNoteRequest request, HttpRequest http, IDeliveryNoteCommandService service) =>
        {
            var key = http.Headers.TryGetValue("Idempotency-Key", out var value) ? value.ToString() : request.IdempotencyKey;
            try
            {
                var (note, replay) = await service.CreateAsync(request, key ?? "");
                return replay ? Results.Ok(note) : Results.Created($"/api/delivery-notes/{note.Id}", note);
            }
            catch (DeliveryNoteIdempotencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
            catch (DeliveryNoteNumberConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                return Results.Conflict(new { error = "El número de remito o la clave de idempotencia ya fue utilizado" });
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithName("CreateDeliveryNote");
        return app;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
