namespace SPC.API.Contracts.DeliveryNotes;

public sealed class DeliveryNoteDetailResponse
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int OriginalInvoiceBranchId { get; set; }
    public string? OriginalInvoiceBranchName { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceType { get; set; }
    public int? OriginalInvoicePointOfSale { get; set; }
    public int PointOfSale { get; set; }
    public long DeliveryNoteNumber { get; set; }
    public DateTime DeliveryNoteDate { get; set; }
    public long? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerTaxId { get; set; }
    public int? SalesRepId { get; set; }
    public string? SalesRepName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public string? BusinessUnit { get; set; }
    public string? Clarification { get; set; }
    public string? Notes { get; set; }
    public List<DeliveryNoteLineResponse> Details { get; set; } = [];
}

public sealed class DeliveryNoteLineResponse
{
    public int ItemNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public decimal Quantity { get; set; }
}
