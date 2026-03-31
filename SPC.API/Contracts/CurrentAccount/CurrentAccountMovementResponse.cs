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

    /// <summary>
    /// Narrow-column document type short code (required).
    /// </summary>
    public string DocumentTypeShortCode { get; set; } = "OT";

    /// <summary>
    /// Optional display label for UI helpers.
    /// </summary>
    public string? DocumentTypeLabel { get; set; }

    /// <summary>
    /// Optional tooltip for UI helpers.
    /// </summary>
    public string? DocumentTypeTooltip { get; set; }

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

    /// <summary>
    /// Total running balance (L1 + L2) after this movement.
    /// This is recalculated for the filtered period, not stored.
    /// </summary>
    public decimal TotalRunningBalance { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Robust navigation metadata for opening document/detail flows safely.
    /// </summary>
    public CurrentAccountNavigationMetadataResponse Navigation { get; set; } = new();
}
