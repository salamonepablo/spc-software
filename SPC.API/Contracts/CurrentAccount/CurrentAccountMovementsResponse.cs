namespace SPC.API.Contracts.CurrentAccount;

/// <summary>
/// Wrapper response for paginated Current Account movements
/// </summary>
public class CurrentAccountMovementsResponse
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
    public List<CurrentAccountMovementResponse> Movements { get; set; } = new();

    /// <summary>
    /// Total count of movements (for pagination)
    /// </summary>
    public int TotalCount { get; set; }
}
