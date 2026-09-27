using SPC.API.Contracts.Payments;
using SPC.API.Services;

namespace SPC.API.Endpoints;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");
        var legacyGroup = app.MapGroup("/api/pagos").WithTags("Payments");
        MapPaymentRoutes(group, true);
        MapPaymentRoutes(legacyGroup, false);
        return app;
    }

    private static void MapPaymentRoutes(RouteGroupBuilder group, bool includeMetadata)
    {
        var create = group.MapPost("/", async (
            CreatePaymentRequest request,
            HttpRequest httpRequest,
            IPaymentCommandService commandService) =>
        {
            if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var header) || string.IsNullOrWhiteSpace(header))
                return Results.BadRequest(new { error = "Idempotency-Key es requerido" });

            try
            {
                var result = await commandService.CreateAsync(request, header.ToString());
                return result.IsReplay
                    ? Results.Ok(result.Payment)
                    : Results.Created($"/api/payments/{result.Payment.PaymentNumber}?branchId={result.Payment.BranchId}", result.Payment);
            }
            catch (PaymentIdempotencyConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        if (includeMetadata)
            create.WithName("CreatePayment").WithDescription("Creates an idempotent customer payment");

        var detail = group.MapGet("/{paymentNumber:long}", async (
            long paymentNumber,
            int? customerId,
            int? branchId,
            IPaymentQueryService queryService) =>
        {
            var payment = await queryService.GetByPaymentNumberAsync(paymentNumber, customerId, branchId);
            return payment != null ? Results.Ok(payment) : Results.NotFound(new { error = "Pago no encontrado" });
        });

        if (includeMetadata)
            detail.WithName("GetPaymentByNumber").WithDescription("Returns a payment detail using customer or branch context when required");

            group.MapPost("/{id:int}/anular", async (
                int id,
                VoidPaymentRequest request,
                HttpRequest httpRequest,
                IPaymentCommandService commandService) =>
            {
                try
                {
                    var idempotencyKey = httpRequest.Headers.TryGetValue("Idempotency-Key", out var header)
                        ? header.ToString()
                        : string.Empty;
                    return Results.Ok(await commandService.VoidAsync(id, request.Reason, idempotencyKey));
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            });
    }
}
