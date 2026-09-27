using SPC.API.Contracts.Payments;

namespace SPC.API.Services;

public interface IPaymentCommandService
{
    Task<PaymentCreationResult> CreateAsync(CreatePaymentRequest request, string idempotencyKey);
    Task<PaymentDetailResponse> VoidAsync(int paymentId, string? reason, string idempotencyKey);
}

public sealed record PaymentCreationResult(PaymentDetailResponse Payment, bool IsReplay);
