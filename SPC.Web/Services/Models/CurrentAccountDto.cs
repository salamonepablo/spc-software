namespace SPC.Web.Services.Models;

/// <summary>
/// DTO for Customer Current Account balance from API
/// </summary>
public class CurrentAccountDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CUIT { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>
    /// Line 1 balance (Billing: Invoices, Credit Notes, Debit Notes, Payments)
    /// </summary>
    public decimal BillingBalance { get; set; }

    /// <summary>
    /// Line 2 balance (Budget: Quotes, Quote Payments)
    /// </summary>
    public decimal BudgetBalance { get; set; }

    /// <summary>
    /// Total balance (BillingBalance + BudgetBalance)
    /// </summary>
    public decimal TotalBalance { get; set; }

    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// DTO for a single Current Account movement
/// </summary>
public class CurrentAccountMovementDto
{
    public int Id { get; set; }
    public DateTime MovementDate { get; set; }

    /// <summary>
    /// Document type name in Spanish (Factura A, Nota de Credito, etc.)
    /// </summary>
    public string DocumentType { get; set; } = "";

    /// <summary>
    /// Document type code for UI styling
    /// </summary>
    public int DocumentTypeCode { get; set; }

    /// <summary>
    /// Required short code for compact `Tipo Doc.` column.
    /// </summary>
    public string DocumentTypeShortCode { get; set; } = "OT";

    /// <summary>
    /// Optional label for display helper.
    /// </summary>
    public string? DocumentTypeLabel { get; set; }

    /// <summary>
    /// Optional tooltip for display helper.
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
    /// Recalculated for the filtered period.
    /// </summary>
    public decimal TotalRunningBalance { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Optional navigation metadata from API. Null-safe for backward compatibility.
    /// </summary>
    public CurrentAccountNavigationMetadataDto? Navigation { get; set; }
}

/// <summary>
/// Navigation metadata for Current Account movement target.
/// </summary>
public class CurrentAccountNavigationMetadataDto
{
    public string TargetType { get; set; } = "other";
    public string TargetKind { get; set; } = "other";
    public string? TargetRoute { get; set; }
    public string? TargetId { get; set; }
    public bool CanOpen { get; set; }
    public string? DisabledReason { get; set; }
}

/// <summary>
/// DTO for paginated Current Account movements response.
/// Includes period-specific initial and final balances.
/// </summary>
public class CurrentAccountMovementsDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Current Line 1 balance (not period-specific)
    /// </summary>
    public decimal BillingBalance { get; set; }

    /// <summary>
    /// Current Line 2 balance (not period-specific)
    /// </summary>
    public decimal BudgetBalance { get; set; }

    /// <summary>
    /// Current total balance (not period-specific)
    /// </summary>
    public decimal TotalBalance { get; set; }

    /// <summary>
    /// L1 balance at the START of the filtered period
    /// </summary>
    public decimal InitialBillingBalance { get; set; }

    /// <summary>
    /// L2 balance at the START of the filtered period
    /// </summary>
    public decimal InitialBudgetBalance { get; set; }

    /// <summary>
    /// Total balance at the START of the filtered period
    /// </summary>
    public decimal InitialTotalBalance { get; set; }

    /// <summary>
    /// L1 balance at the END of the filtered period
    /// </summary>
    public decimal FinalBillingBalance { get; set; }

    /// <summary>
    /// L2 balance at the END of the filtered period
    /// </summary>
    public decimal FinalBudgetBalance { get; set; }

    /// <summary>
    /// Total balance at the END of the filtered period
    /// </summary>
    public decimal FinalTotalBalance { get; set; }

    /// <summary>
    /// Paginated list of movements
    /// </summary>
    public List<CurrentAccountMovementDto> Movements { get; set; } = new();

    /// <summary>
    /// Total count of movements (for pagination)
    /// </summary>
    public int TotalCount { get; set; }

    public bool GuardrailApplied { get; set; }
    public string GuardrailMode { get; set; } = "none";
    public string? WarningCode { get; set; }
    public string? WarningMessage { get; set; }
    public int ReturnedCount { get; set; }
    public int RangeDays { get; set; }
}
