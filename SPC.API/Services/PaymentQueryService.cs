using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Payments;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Query-side implementation for payment read operations.
/// </summary>
public class PaymentQueryService : IPaymentQueryService
{
    private readonly SPCDbContext _db;

    public PaymentQueryService(SPCDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentDetailResponse?> GetByPaymentNumberAsync(long paymentNumber, int? customerId = null)
    {
        var query = _db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Branch)
            .Include(p => p.Details)
                .ThenInclude(d => d.PaymentMethod)
            .Where(p => p.PaymentNumber == paymentNumber);

        if (customerId.HasValue)
        {
            query = query.Where(p => p.CustomerId == customerId.Value);
        }

        var payment = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        return payment == null ? null : MapToDetail(payment);
    }

    private static PaymentDetailResponse MapToDetail(Payment payment)
    {
        return new PaymentDetailResponse
        {
            Id = payment.Id,
            BranchId = payment.BranchId,
            BranchName = payment.Branch?.Name ?? "",
            PaymentNumber = payment.PaymentNumber,
            PaymentDate = payment.PaymentDate,
            CustomerId = payment.CustomerId,
            CustomerName = payment.Customer?.CompanyName ?? "",
            CustomerCUIT = payment.Customer?.CUIT,
            TotalAmount = payment.TotalAmount,
            AppliesTo = payment.AppliesTo == AccountLineType.Billing ? "Billing" : "Budget",
            IsVoided = payment.IsVoided,
            AppliesToDescription = payment.AppliesToDescription,
            Details = payment.Details
                .OrderBy(d => d.LineNumber)
                .Select(d => new PaymentMethodLineResponse
                {
                    Id = d.Id,
                    LineNumber = d.LineNumber,
                    PaymentMethodId = d.PaymentMethodId,
                    PaymentMethodCode = d.PaymentMethod?.Code ?? "",
                    PaymentMethodDescription = d.PaymentMethod?.Description ?? "",
                    Amount = d.Amount,
                    Notes = d.Notes
                })
                .ToList()
        };
    }
}
