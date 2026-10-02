namespace SPC.Shared.Models;

public sealed class BranchDeliveryNoteSequence
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public long NextDeliveryNoteNumber { get; set; }
}
