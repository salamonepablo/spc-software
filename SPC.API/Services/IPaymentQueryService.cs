using SPC.API.Contracts.Payments;

namespace SPC.API.Services;

/// <summary>
/// Query-side interface for payment read operations.
/// </summary>
public interface IPaymentQueryService
{
    /// <summary>
    /// Gets a payment by payment number. If customerId is provided, filters to that customer.
    /// </summary>
    Task<PaymentDetailResponse?> GetByPaymentNumberAsync(long paymentNumber, int? customerId = null);
}
