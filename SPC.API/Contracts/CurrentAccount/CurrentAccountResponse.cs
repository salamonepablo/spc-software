namespace SPC.API.Contracts.CurrentAccount;

/// <summary>
/// Response DTO for Customer Current Account balance
/// </summary>
public class CurrentAccountResponse
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
