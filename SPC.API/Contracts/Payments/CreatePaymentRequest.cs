namespace SPC.API.Contracts.Payments;

public class CreatePaymentRequest
{
    public int CustomerId { get; set; }
    public int BranchId { get; set; }
    public DateTime PaymentDate { get; set; }
    public string AppliesTo { get; set; } = "";
    public string? Notes { get; set; }
    public List<CreatePaymentDetailRequest> Details { get; set; } = new();
}

public class CreatePaymentDetailRequest
{
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
