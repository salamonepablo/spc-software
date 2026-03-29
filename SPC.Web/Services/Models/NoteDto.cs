namespace SPC.Web.Services.Models;

public class CreditNoteDetailDto
{
    public int Id { get; set; }
    public string VoucherType { get; set; } = "";
    public int PointOfSale { get; set; }
    public long CreditNoteNumber { get; set; }
    public DateTime CreditNoteDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerCUIT { get; set; }
    public int? SalesRepId { get; set; }
    public string? SalesRepName { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VATPercent { get; set; }
    public decimal VATAmount { get; set; }
    public decimal IIBBPercent { get; set; }
    public decimal IIBBAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string? CAE { get; set; }
    public DateTime? CAEExpirationDate { get; set; }
    public bool IsVoided { get; set; }
    public int ItemCount { get; set; }
    public string? SalesCondition { get; set; }
    public string? Notes { get; set; }
    public List<CreditNoteLineDto> Details { get; set; } = new();
}

public class CreditNoteLineDto
{
    public int Id { get; set; }
    public int ItemNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
}

public class DebitNoteDetailDto
{
    public int Id { get; set; }
    public string VoucherType { get; set; } = "";
    public int PointOfSale { get; set; }
    public long DebitNoteNumber { get; set; }
    public DateTime DebitNoteDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerCUIT { get; set; }
    public int? SalesRepId { get; set; }
    public string? SalesRepName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VATPercent { get; set; }
    public decimal VATAmount { get; set; }
    public decimal IIBBPercent { get; set; }
    public decimal IIBBAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string? CAE { get; set; }
    public DateTime? CAEExpirationDate { get; set; }
    public bool IsVoided { get; set; }
    public int ItemCount { get; set; }
    public string? SalesCondition { get; set; }
    public string? Notes { get; set; }
    public List<DebitNoteLineDto> Details { get; set; } = new();
}

public class DebitNoteLineDto
{
    public int Id { get; set; }
    public int ItemNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
}
