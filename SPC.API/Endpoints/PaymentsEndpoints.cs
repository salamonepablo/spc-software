using SPC.API.Services;

namespace SPC.API.Endpoints;

/// <summary>
/// Endpoint module for payment detail queries.
/// </summary>
public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments");
        var legacyGroup = app.MapGroup("/api/pagos")
            .WithTags("Payments");

        MapPaymentRoutes(group, includeMetadata: true);
        MapPaymentRoutes(legacyGroup, includeMetadata: false);

        return app;
    }

    private static void MapPaymentRoutes(RouteGroupBuilder group, bool includeMetadata)
    {
        RouteHandlerBuilder builder;

        builder = group.MapGet("/{paymentNumber:long}", async (
            long paymentNumber,
            int? customerId,
            IPaymentQueryService queryService) =>
        {
            var payment = await queryService.GetByPaymentNumberAsync(paymentNumber, customerId);
            return payment != null
                ? Results.Ok(payment)
                : Results.NotFound(new { error = "Pago no encontrado" });
        });

        if (includeMetadata)
        {
            builder
                .WithName("GetPaymentByNumber")
                .WithDescription("Returns a payment detail by payment number and optional customer filter");
        }
    }
}
