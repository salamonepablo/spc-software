namespace SPC.API.Contracts.DeliveryNotes;

public sealed class DeliveryNoteResponse
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int OriginalInvoiceBranchId { get; set; }
    public int? InvoiceId { get; set; }
    public long DeliveryNoteNumber { get; set; }
    public long? InvoiceNumber { get; set; }
    public string? InvoiceType { get; set; }
    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }
    public DateTime DeliveryNoteDate { get; set; }
    public List<CreateDeliveryNoteDetailRequest> Details { get; set; } = [];
}
