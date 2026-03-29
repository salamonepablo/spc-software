namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for a payment detail.
/// </summary>
public class PaymentDetailDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public long PaymentNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerCUIT { get; set; }
    public decimal TotalAmount { get; set; }
    public string AppliesTo { get; set; } = "";
    public bool IsVoided { get; set; }
    public string? AppliesToDescription { get; set; }
    public List<PaymentMethodLineDto> Details { get; set; } = new();
}

public class PaymentMethodLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int PaymentMethodId { get; set; }
    public string PaymentMethodCode { get; set; } = "";
    public string PaymentMethodDescription { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
