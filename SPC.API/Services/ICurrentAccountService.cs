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
    Task<CurrentAccount> RecordMovementAsync(
        int customerId,
        DocumentType documentType,
        long documentNumber,
        decimal billingAmount,
        decimal budgetAmount,
        string? description = null);

    /// <summary>
    /// Gets the current account for a customer, creating it if it doesn't exist.
    /// </summary>
    Task<CurrentAccount> GetOrCreateAccountAsync(int customerId);

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
    Task<CurrentAccount> SetInitialBalanceAsync(
        int customerId,
        decimal billingBalance,
        decimal budgetBalance,
        DateTime? asOfDate = null);

    /// <summary>
    /// Gets the current account for a customer (returns null if not exists).
    /// Includes Customer navigation property.
    /// </summary>
    Task<CurrentAccount?> GetAccountAsync(int customerId);

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
    /// Checks if the DualLineCurrentAccount feature is enabled.
    /// </summary>
    bool IsDualLineEnabled();
}

/// <summary>
/// Result of filtered movements query with pagination info
/// </summary>
public class CurrentAccountMovementsResult
{
    public decimal BillingBalance { get; set; }
    public decimal BudgetBalance { get; set; }
    public decimal TotalBalance { get; set; }
    public List<CurrentAccountMovement> Movements { get; set; } = new();
    public int TotalCount { get; set; }
}
