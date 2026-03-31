using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service for managing customer current account balances.
/// Handles dual-line accounting: Billing (L1) and Budget (L2).
/// </summary>
public interface ICurrentAccountService
{
    /// <summary>
    /// Records a movement and updates the customer's current account balance.
    /// Only updates Budget balance if DualLineCurrentAccount feature is enabled.
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="documentType">Type of document (Invoice, Quote, CreditNote, etc.)</param>
    /// <param name="documentNumber">Document number</param>
    /// <param name="billingAmount">Amount to add/subtract from Billing balance (L1)</param>
    /// <param name="budgetAmount">Amount to add/subtract from Budget balance (L2)</param>
    /// <param name="description">Optional description for the movement</param>
    /// <returns>The updated CurrentAccount</returns>
    Task<SPC.Shared.Models.CurrentAccount> RecordMovementAsync(
        int customerId,
        DocumentType documentType,
        long documentNumber,
        decimal billingAmount,
        decimal budgetAmount,
        string? description = null);

    /// <summary>
    /// Gets the current account for a customer, creating it if it doesn't exist.
    /// </summary>
    Task<SPC.Shared.Models.CurrentAccount> GetOrCreateAccountAsync(int customerId);

    /// <summary>
    /// Gets all movements for a customer, ordered by date ascending (oldest first).
    /// </summary>
    Task<IEnumerable<CurrentAccountMovement>> GetMovementsAsync(int customerId, int skip = 0, int take = 50);

    /// <summary>
    /// Sets the initial balance for a customer's current account.
    /// Creates a "Saldo inicial" movement as the first entry.
    /// Should only be called once per customer when migrating or setting up.
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="billingBalance">Initial L1 (Billing) balance</param>
    /// <param name="budgetBalance">Initial L2 (Budget) balance</param>
    /// <param name="asOfDate">The date for the initial balance (defaults to now)</param>
    /// <returns>The created CurrentAccount</returns>
    Task<SPC.Shared.Models.CurrentAccount> SetInitialBalanceAsync(
        int customerId,
        decimal billingBalance,
        decimal budgetBalance,
        DateTime? asOfDate = null);

    /// <summary>
    /// Gets the current account for a customer (returns null if not exists).
    /// Includes Customer navigation property.
    /// </summary>
    Task<SPC.Shared.Models.CurrentAccount?> GetAccountAsync(int customerId);

    /// <summary>
    /// Gets movements with filters and pagination. Returns movements and total count.
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="dateFrom">Optional start date filter</param>
    /// <param name="dateTo">Optional end date filter</param>
    /// <param name="line">1=Billing only, 2=Budget only, null=All</param>
    /// <param name="skip">Pagination offset</param>
    /// <param name="take">Pagination limit (max 200)</param>
    Task<CurrentAccountMovementsResult> GetMovementsFilteredAsync(
        int customerId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? line,
        int skip = 0,
        int take = 50);

    /// <summary>
    /// Gets all movements for a fixed date range with guardrails.
    /// This path does not apply fixed UI pagination caps.
    /// </summary>
    Task<CurrentAccountMovementsResult> GetMovementsByRangeAsync(
        int customerId,
        DateTime dateFrom,
        DateTime dateTo,
        int? line,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the DualLineCurrentAccount feature is enabled.
    /// </summary>
    bool IsDualLineEnabled();
}

/// <summary>
/// Result of filtered movements query with pagination info.
/// Includes period-specific initial and final balances for correct running balance display.
/// </summary>
public class CurrentAccountMovementsResult
{
    /// <summary>Current account L1 balance (not period-specific)</summary>
    public decimal BillingBalance { get; set; }
    /// <summary>Current account L2 balance (not period-specific)</summary>
    public decimal BudgetBalance { get; set; }
    /// <summary>Current account total balance (not period-specific)</summary>
    public decimal TotalBalance { get; set; }

    /// <summary>L1 balance at the START of the filtered period (sum of all movements before dateFrom)</summary>
    public decimal InitialBillingBalance { get; set; }
    /// <summary>L2 balance at the START of the filtered period</summary>
    public decimal InitialBudgetBalance { get; set; }
    /// <summary>Total balance at the START of the filtered period</summary>
    public decimal InitialTotalBalance { get; set; }

    /// <summary>L1 balance at the END of the filtered period</summary>
    public decimal FinalBillingBalance { get; set; }
    /// <summary>L2 balance at the END of the filtered period</summary>
    public decimal FinalBudgetBalance { get; set; }
    /// <summary>Total balance at the END of the filtered period</summary>
    public decimal FinalTotalBalance { get; set; }

    public List<CurrentAccountMovement> Movements { get; set; } = new();
    public int TotalCount { get; set; }

    public bool GuardrailApplied { get; set; }
    public string GuardrailMode { get; set; } = "none";
    public string? WarningCode { get; set; }
    public string? WarningMessage { get; set; }
    public int ReturnedCount { get; set; }
    public int RangeDays { get; set; }
}
