namespace SPC.API.Contracts.DeliveryNotes;

public sealed class DeliveryNoteNextNumberResponse
{
    public int BranchId { get; set; }
    public long NextNumber { get; set; }
    public bool IsAdvisory { get; set; } = true;
}
