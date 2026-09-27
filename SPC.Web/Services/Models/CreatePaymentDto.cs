namespace SPC.Web.Services.Models;

public class CreatePaymentDto
{
    public int CustomerId { get; set; }
    public int BranchId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string AppliesTo { get; set; } = "Billing";
    public string? Notes { get; set; }
    public List<CreatePaymentDetailDto> Details { get; set; } = new();
}

public class CreatePaymentDetailDto
{
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
