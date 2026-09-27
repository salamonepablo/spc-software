using SPC.API.Contracts.Payments;

namespace SPC.API.Services;

public interface IPaymentQueryService
{
    Task<PaymentDetailResponse?> GetByPaymentNumberAsync(long paymentNumber, int? customerId = null, int? branchId = null);
    Task<PaymentDetailResponse?> GetByIdAsync(int id);
}
