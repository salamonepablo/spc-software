namespace SPC.Web.Services.Models;

public class CreateInvoiceDto
{
    public int BranchId { get; set; }
    public string InvoiceType { get; set; } = "B";
    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal IIBBPercent { get; set; }
    public string? SalesCondition { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceDetailDto> Details { get; set; } = new();
}

public class CreateInvoiceDetailDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal? VATPercent { get; set; }
}
