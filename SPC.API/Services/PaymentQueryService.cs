using Microsoft.EntityFrameworkCore;
using SPC.API.Contracts.Payments;
using SPC.API.Data;
using SPC.Shared.Models;

namespace SPC.API.Services;

public class PaymentQueryService : IPaymentQueryService
{
    private readonly SPCDbContext _db;

    public PaymentQueryService(SPCDbContext db) => _db = db;

    public async Task<PaymentDetailResponse?> GetByPaymentNumberAsync(long paymentNumber, int? customerId = null, int? branchId = null)
    {
        var query = DetailQuery().Where(payment => payment.PaymentNumber == paymentNumber);
        if (customerId.HasValue)
            query = query.Where(payment => payment.CustomerId == customerId.Value);
        if (branchId.HasValue)
            query = query.Where(payment => payment.BranchId == branchId.Value);

        var payments = await query.Take(2).ToListAsync();
        return payments.Count == 1 ? MapToDetail(payments[0]) : null;
    }

    public async Task<PaymentDetailResponse?> GetByIdAsync(int id)
    {
        var payment = await DetailQuery().SingleOrDefaultAsync(item => item.Id == id);
        return payment == null ? null : MapToDetail(payment);
    }

    private IQueryable<Payment> DetailQuery() => _db.Payments
        .Include(payment => payment.Customer)
        .Include(payment => payment.Branch)
        .Include(payment => payment.Details)
            .ThenInclude(detail => detail.PaymentMethod);

    private static PaymentDetailResponse MapToDetail(Payment payment) => new()
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
        Details = payment.Details.OrderBy(detail => detail.LineNumber).Select(detail => new PaymentMethodLineResponse
        {
            Id = detail.Id,
            LineNumber = detail.LineNumber,
            PaymentMethodId = detail.PaymentMethodId,
            PaymentMethodCode = detail.PaymentMethod?.Code ?? "",
            PaymentMethodDescription = detail.PaymentMethod?.Description ?? "",
            Amount = detail.Amount,
            Notes = detail.Notes
        }).ToList()
    };
}
