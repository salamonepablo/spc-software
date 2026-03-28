namespace SPC.API.Contracts.CurrentAccount;

/// <summary>
/// Response DTO for a single Current Account movement
/// </summary>
public class CurrentAccountMovementResponse
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }

    /// <summary>
    /// Document type name (Invoice, Quote, CreditNote, DebitNote, Payment, etc.)
    /// </summary>
    public string DocumentType { get; set; } = "";

    /// <summary>
    /// Document type code for UI styling (e.g., color coding payments in red)
    /// </summary>
    public int DocumentTypeCode { get; set; }

    public long DocumentNumber { get; set; }

    /// <summary>
    /// Line 1 amount (positive = debit, negative = credit)
    /// </summary>
    public decimal BillingAmount { get; set; }

    /// <summary>
    /// Line 2 amount (positive = debit, negative = credit)
    /// </summary>
    public decimal BudgetAmount { get; set; }

    /// <summary>
    /// Running balance for Line 1 after this movement
    /// </summary>
    public decimal BillingRunningBalance { get; set; }

    /// <summary>
    /// Running balance for Line 2 after this movement
    /// </summary>
    public decimal BudgetRunningBalance { get; set; }

    public string? Description { get; set; }
}
