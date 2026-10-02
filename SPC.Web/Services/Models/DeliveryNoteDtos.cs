namespace SPC.Web.Services.Models;

public sealed class DeliveryNoteSearchRequestDto
{
    public int? BranchId { get; set; }
    public int? CustomerId { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class DeliveryNoteSearchResultDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<DeliveryNoteSummaryDto> Items { get; set; } = new();
}

public sealed class DeliveryNoteSummaryDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public long DeliveryNoteNumber { get; set; }
    public DateTime DeliveryNoteDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public long? InvoiceNumber { get; set; }
}

public sealed class DeliveryNoteDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int OriginalInvoiceBranchId { get; set; }
    public string OriginalInvoiceBranchName { get; set; } = "";
    public int? InvoiceId { get; set; }
    public string? InvoiceType { get; set; }
    public int? OriginalInvoicePointOfSale { get; set; }
    public int? PointOfSale { get; set; }
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
    public List<DeliveryNoteLineDto> Details { get; set; } = new();
}

public sealed class DeliveryNoteLineDto
{
    public int ItemNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public decimal Quantity { get; set; }
}

public sealed class DeliveryNoteNextNumberDto
{
    public int BranchId { get; set; }
    public long NextNumber { get; set; }
    public bool IsAdvisory { get; set; }
}

public sealed class CreateDeliveryNoteDto
{
    public int BranchId { get; set; }
    public long? RequestedDeliveryNoteNumber { get; set; }
    public int? PointOfSale { get; set; }
    public DateTime DeliveryNoteDate { get; set; }
    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public string? BusinessUnit { get; set; }
    public string? Clarification { get; set; }
    public string? Notes { get; set; }
    public int? InvoiceId { get; set; }
    public bool AdjustStock { get; set; }
    public List<CreateDeliveryNoteLineDto> Details { get; set; } = new();
}

public sealed class CreateDeliveryNoteLineDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
}
