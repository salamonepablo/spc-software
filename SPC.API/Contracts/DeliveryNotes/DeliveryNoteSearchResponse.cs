namespace SPC.API.Contracts.DeliveryNotes;

public sealed class DeliveryNoteSearchRequest
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

public sealed class DeliveryNoteSearchResponse
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public long DeliveryNoteNumber { get; set; }
    public DateTime DeliveryNoteDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public long? InvoiceNumber { get; set; }
}

public sealed class DeliveryNoteSearchResult
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<DeliveryNoteSearchResponse> Items { get; set; } = [];
}
