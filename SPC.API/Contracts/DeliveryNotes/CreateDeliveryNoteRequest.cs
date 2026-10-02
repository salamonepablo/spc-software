namespace SPC.API.Contracts.DeliveryNotes;

public sealed class CreateDeliveryNoteRequest
{
    public int BranchId { get; set; }
    public long? RequestedDeliveryNoteNumber { get; set; }
    public int? PointOfSale { get; set; }
    public DateTime DeliveryNoteDate { get; set; } = DateTime.Today;
    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public string? BusinessUnit { get; set; }
    public string? Clarification { get; set; }
    public string? Notes { get; set; }
    public int? InvoiceId { get; set; }
    public bool AdjustStock { get; set; } = true;
    public List<CreateDeliveryNoteDetailRequest> Details { get; set; } = [];
    // Kept for direct command callers; HTTP callers should use the Idempotency-Key header.
    public string? IdempotencyKey { get; set; }
}
