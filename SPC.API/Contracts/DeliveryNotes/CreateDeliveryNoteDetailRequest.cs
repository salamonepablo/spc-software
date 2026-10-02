namespace SPC.API.Contracts.DeliveryNotes;

public sealed class CreateDeliveryNoteDetailRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
}
