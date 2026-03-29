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
/// DTO for paginated Current Account movements response
/// </summary>
public class CurrentAccountMovementsDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Current Line 1 balance
    /// </summary>
    public decimal BillingBalance { get; set; }

    /// <summary>
    /// Current Line 2 balance
    /// </summary>
    public decimal BudgetBalance { get; set; }

    /// <summary>
    /// Current total balance
    /// </summary>
    public decimal TotalBalance { get; set; }

    /// <summary>
    /// Paginated list of movements
    /// </summary>
    public List<CurrentAccountMovementDto> Movements { get; set; } = new();

    /// <summary>
    /// Total count of movements (for pagination)
    /// </summary>
    public int TotalCount { get; set; }
}
