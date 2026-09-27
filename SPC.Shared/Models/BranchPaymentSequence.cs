namespace SPC.Shared.Models;

/// <summary>
/// Per-branch, database-backed payment number allocator.
/// </summary>
public class BranchPaymentSequence
{
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public long NextPaymentNumber { get; set; }
}
