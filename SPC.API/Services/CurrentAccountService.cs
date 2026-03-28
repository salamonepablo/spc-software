using Microsoft.EntityFrameworkCore;
using SPC.API.Data;
using SPC.Shared.Licensing;
using SPC.Shared.Models;

namespace SPC.API.Services;

/// <summary>
/// Service for managing customer current account balances.
/// Implements dual-line accounting when licensed.
/// </summary>
public class CurrentAccountService : ICurrentAccountService
{
    private readonly SPCDbContext _db;
    private readonly ILicenseService _licenseService;

    public CurrentAccountService(SPCDbContext db, ILicenseService licenseService)
    {
        _db = db;
        _licenseService = licenseService;
    }

    /// <inheritdoc />
    public bool IsDualLineEnabled()
    {
        return _licenseService.IsFeatureEnabled(Features.DualLineCurrentAccount);
    }

    /// <inheritdoc />
    public async Task<CurrentAccount> GetOrCreateAccountAsync(int customerId)
    {
        var account = await _db.CurrentAccounts
            .FirstOrDefaultAsync(ca => ca.CustomerId == customerId);

        if (account == null)
        {
            account = new CurrentAccount
            {
                CustomerId = customerId,
                BillingBalance = 0,
                BudgetBalance = 0,
                TotalBalance = 0,
                LastUpdated = DateTime.Now
            };
            _db.CurrentAccounts.Add(account);
            await _db.SaveChangesAsync();
        }

        return account;
    }

    /// <inheritdoc />
    public async Task<CurrentAccount> RecordMovementAsync(
        int customerId,
        DocumentType documentType,
        long documentNumber,
        decimal billingAmount,
        decimal budgetAmount,
        string? description = null)
    {
        // Get or create the customer's current account
        var account = await GetOrCreateAccountAsync(customerId);

        // Check if dual-line is enabled
        var dualLineEnabled = IsDualLineEnabled();

        // Update balances
        // Billing (L1) always updates
        account.BillingBalance += billingAmount;

        // Budget (L2) only updates if dual-line is enabled
        if (dualLineEnabled)
        {
            account.BudgetBalance += budgetAmount;
        }

        // Total is always the sum
        account.TotalBalance = account.BillingBalance + account.BudgetBalance;
        account.LastUpdated = DateTime.Now;

        // Record the movement for history
        var movement = new CurrentAccountMovement
        {
            MovementDate = DateTime.Now,
            CustomerId = customerId,
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            BillingAmount = billingAmount,
            BudgetAmount = dualLineEnabled ? budgetAmount : 0, // Only record if enabled
            BillingRunningBalance = account.BillingBalance,
            BudgetRunningBalance = account.BudgetBalance,
            Description = description
        };

        _db.CurrentAccountMovements.Add(movement);
        await _db.SaveChangesAsync();

        return account;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CurrentAccountMovement>> GetMovementsAsync(int customerId, int skip = 0, int take = 50)
    {
        return await _db.CurrentAccountMovements
            .Where(m => m.CustomerId == customerId)
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CurrentAccount?> GetAccountAsync(int customerId)
    {
        return await _db.CurrentAccounts
            .Include(ca => ca.Customer)
            .FirstOrDefaultAsync(ca => ca.CustomerId == customerId);
    }

    /// <inheritdoc />
    public async Task<CurrentAccountMovementsResult> GetMovementsFilteredAsync(
        int customerId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? line,
        int skip = 0,
        int take = 50)
    {
        // Limit take to 200
        take = Math.Min(take, 200);

        // Get account balances
        var account = await _db.CurrentAccounts
            .FirstOrDefaultAsync(ca => ca.CustomerId == customerId);

        // Build query
        var query = _db.CurrentAccountMovements
            .Where(m => m.CustomerId == customerId);

        // Date filters
        if (dateFrom.HasValue)
        {
            query = query.Where(m => m.MovementDate >= dateFrom.Value);
        }
        if (dateTo.HasValue)
        {
            // Include the entire end date
            var endOfDay = dateTo.Value.Date.AddDays(1);
            query = query.Where(m => m.MovementDate < endOfDay);
        }

        // Line filter
        if (line == 1)
        {
            query = query.Where(m => m.BillingAmount != 0);
        }
        else if (line == 2)
        {
            query = query.Where(m => m.BudgetAmount != 0);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Get paginated movements
        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return new CurrentAccountMovementsResult
        {
            BillingBalance = account?.BillingBalance ?? 0,
            BudgetBalance = account?.BudgetBalance ?? 0,
            TotalBalance = account?.TotalBalance ?? 0,
            Movements = movements,
            TotalCount = totalCount
        };
    }
}
