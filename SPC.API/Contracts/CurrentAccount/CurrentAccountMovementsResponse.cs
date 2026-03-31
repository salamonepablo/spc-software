namespace SPC.API.Contracts.CurrentAccount;

/// <summary>
/// Wrapper response for paginated Current Account movements.
/// Includes period-specific initial and final balances for correct running balance display.
/// </summary>
public class CurrentAccountMovementsResponse
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
    public List<CurrentAccountMovementResponse> Movements { get; set; } = new();

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
