using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SPC.API.Data;
using SPC.API.Services.CurrentAccount;
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
    private readonly CurrentAccountGuardrailOptions _guardrailOptions;

    public CurrentAccountService(
        SPCDbContext db,
        ILicenseService licenseService,
        IOptions<CurrentAccountGuardrailOptions>? guardrailOptions = null)
    {
        _db = db;
        _licenseService = licenseService;
        _guardrailOptions = guardrailOptions?.Value ?? new CurrentAccountGuardrailOptions();
    }

    /// <inheritdoc />
    public bool IsDualLineEnabled()
    {
        return _licenseService.IsFeatureEnabled(Features.DualLineCurrentAccount);
    }

    /// <inheritdoc />
    public async Task<SPC.Shared.Models.CurrentAccount> GetOrCreateAccountAsync(int customerId)
    {
        var account = await _db.CurrentAccounts
            .FirstOrDefaultAsync(ca => ca.CustomerId == customerId);

        if (account == null)
        {
            account = new SPC.Shared.Models.CurrentAccount
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
    public async Task<SPC.Shared.Models.CurrentAccount> RecordMovementAsync(
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
    public async Task<SPC.Shared.Models.CurrentAccount> SetInitialBalanceAsync(
        int customerId,
        decimal billingBalance,
        decimal budgetBalance,
        DateTime? asOfDate = null)
    {
        // Check if account already exists with movements
        var existingMovements = await _db.CurrentAccountMovements
            .Where(m => m.CustomerId == customerId)
            .AnyAsync();

        if (existingMovements)
        {
            throw new InvalidOperationException(
                $"Customer {customerId} already has movements. Cannot set initial balance.");
        }

        // Get or create account
        var account = await _db.CurrentAccounts
            .FirstOrDefaultAsync(ca => ca.CustomerId == customerId);

        var effectiveDate = asOfDate ?? DateTime.Now;

        if (account == null)
        {
            account = new SPC.Shared.Models.CurrentAccount
            {
                CustomerId = customerId,
                BillingBalance = billingBalance,
                BudgetBalance = budgetBalance,
                TotalBalance = billingBalance + budgetBalance,
                LastUpdated = effectiveDate
            };
            _db.CurrentAccounts.Add(account);
        }
        else
        {
            account.BillingBalance = billingBalance;
            account.BudgetBalance = budgetBalance;
            account.TotalBalance = billingBalance + budgetBalance;
            account.LastUpdated = effectiveDate;
        }

        // Create the "Saldo inicial" movement
        var movement = new CurrentAccountMovement
        {
            MovementDate = effectiveDate,
            CustomerId = customerId,
            DocumentType = DocumentType.Other,
            DocumentNumber = 0,
            BillingAmount = billingBalance,
            BudgetAmount = budgetBalance,
            BillingRunningBalance = billingBalance,
            BudgetRunningBalance = budgetBalance,
            Description = "Saldo inicial"
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
            .OrderBy(m => m.MovementDate)
            .ThenBy(m => m.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SPC.Shared.Models.CurrentAccount?> GetAccountAsync(int customerId)
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

        // Calculate initial balance: sum of all movements BEFORE the period start
        var (initialBilling, initialBudget) = await CalculateInitialBalanceAsync(customerId, dateFrom);

        // Build query for the period
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

        // Get paginated movements (ascending order: oldest first for proper running balance display)
        var movements = await query
            .OrderBy(m => m.MovementDate)
            .ThenBy(m => m.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // Recalculate running balances from initial balance
        RecalculateRunningBalances(movements, initialBilling, initialBudget);

        // Calculate final balance from the last movement's running balance
        var finalBilling = movements.Count > 0 ? movements[^1].BillingRunningBalance : initialBilling;
        var finalBudget = movements.Count > 0 ? movements[^1].BudgetRunningBalance : initialBudget;

        var rangeDays = 0;
        if (dateFrom.HasValue && dateTo.HasValue)
        {
            rangeDays = (dateTo.Value.Date - dateFrom.Value.Date).Days + 1;
        }

        return new CurrentAccountMovementsResult
        {
            BillingBalance = account?.BillingBalance ?? 0,
            BudgetBalance = account?.BudgetBalance ?? 0,
            TotalBalance = account?.TotalBalance ?? 0,
            InitialBillingBalance = initialBilling,
            InitialBudgetBalance = initialBudget,
            InitialTotalBalance = initialBilling + initialBudget,
            FinalBillingBalance = finalBilling,
            FinalBudgetBalance = finalBudget,
            FinalTotalBalance = finalBilling + finalBudget,
            Movements = movements,
            TotalCount = totalCount,
            ReturnedCount = movements.Count,
            RangeDays = Math.Max(rangeDays, 0)
        };
    }

    /// <inheritdoc />
    public async Task<CurrentAccountMovementsResult> GetMovementsByRangeAsync(
        int customerId,
        DateTime dateFrom,
        DateTime dateTo,
        int? line,
        CancellationToken cancellationToken = default)
    {
        var account = await _db.CurrentAccounts
            .FirstOrDefaultAsync(ca => ca.CustomerId == customerId, cancellationToken);

        var normalizedFrom = dateFrom.Date;
        var normalizedTo = dateTo.Date;
        var rangeDays = (normalizedTo - normalizedFrom).Days + 1;

        if (rangeDays <= 0)
        {
            return CreateGuardrailResult(account, "rejected", "INVALID_RANGE", "dateTo must be greater than or equal to dateFrom", rangeDays);
        }

        if (rangeDays > _guardrailOptions.MaxRangeDays)
        {
            return CreateGuardrailResult(account, "rejected", "RANGE_TOO_WIDE", $"Selected range exceeds the maximum allowed span of {_guardrailOptions.MaxRangeDays} days", rangeDays);
        }

        var (initialBilling, initialBudget) = await CalculateInitialBalanceAsync(customerId, normalizedFrom);

        var query = _db.CurrentAccountMovements
            .Where(m => m.CustomerId == customerId && m.MovementDate >= normalizedFrom && m.MovementDate < normalizedTo.AddDays(1));

        if (line == 1)
        {
            query = query.Where(m => m.BillingAmount != 0);
        }
        else if (line == 2)
        {
            query = query.Where(m => m.BudgetAmount != 0);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount > _guardrailOptions.MaxRows &&
            string.Equals(_guardrailOptions.OverflowMode, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            return CreateGuardrailResult(account, "rejected", "TOO_MANY_ROWS", $"Search would return {totalCount} rows, above configured max {_guardrailOptions.MaxRows}", rangeDays);
        }

        var orderedQuery = query
            .OrderBy(m => m.MovementDate)
            .ThenBy(m => m.Id);

        var guardrailApplied = false;
        var guardrailMode = "none";
        string? warningCode = null;
        string? warningMessage = null;

        List<CurrentAccountMovement> movements;
        if (totalCount > _guardrailOptions.MaxRows)
        {
            guardrailApplied = true;
            guardrailMode = "truncated";
            warningCode = "TOO_MANY_ROWS";
            warningMessage = $"Result truncated to configured max {_guardrailOptions.MaxRows} rows";
            movements = await orderedQuery.Take(_guardrailOptions.MaxRows).ToListAsync(cancellationToken);
        }
        else
        {
            movements = await orderedQuery.ToListAsync(cancellationToken);
        }

        RecalculateRunningBalances(movements, initialBilling, initialBudget);

        var finalBilling = movements.Count > 0 ? movements[^1].BillingRunningBalance : initialBilling;
        var finalBudget = movements.Count > 0 ? movements[^1].BudgetRunningBalance : initialBudget;

        return new CurrentAccountMovementsResult
        {
            BillingBalance = account?.BillingBalance ?? 0,
            BudgetBalance = account?.BudgetBalance ?? 0,
            TotalBalance = account?.TotalBalance ?? 0,
            InitialBillingBalance = initialBilling,
            InitialBudgetBalance = initialBudget,
            InitialTotalBalance = initialBilling + initialBudget,
            FinalBillingBalance = finalBilling,
            FinalBudgetBalance = finalBudget,
            FinalTotalBalance = finalBilling + finalBudget,
            Movements = movements,
            TotalCount = totalCount,
            ReturnedCount = movements.Count,
            RangeDays = rangeDays,
            GuardrailApplied = guardrailApplied,
            GuardrailMode = guardrailMode,
            WarningCode = warningCode,
            WarningMessage = warningMessage
        };
    }

    private static CurrentAccountMovementsResult CreateGuardrailResult(
        SPC.Shared.Models.CurrentAccount? account,
        string mode,
        string warningCode,
        string warningMessage,
        int rangeDays)
    {
        return new CurrentAccountMovementsResult
        {
            BillingBalance = account?.BillingBalance ?? 0,
            BudgetBalance = account?.BudgetBalance ?? 0,
            TotalBalance = account?.TotalBalance ?? 0,
            InitialBillingBalance = 0,
            InitialBudgetBalance = 0,
            InitialTotalBalance = 0,
            FinalBillingBalance = 0,
            FinalBudgetBalance = 0,
            FinalTotalBalance = 0,
            TotalCount = 0,
            ReturnedCount = 0,
            RangeDays = rangeDays,
            GuardrailApplied = true,
            GuardrailMode = mode,
            WarningCode = warningCode,
            WarningMessage = warningMessage,
            Movements = []
        };
    }

    /// <summary>
    /// Calculates the sum of all movements before the given date.
    /// Returns (billingSum, budgetSum).
    /// </summary>
    private async Task<(decimal billing, decimal budget)> CalculateInitialBalanceAsync(int customerId, DateTime? dateFrom)
    {
        if (!dateFrom.HasValue)
        {
            // No date filter means show all movements from start, so initial balance is 0
            return (0, 0);
        }

        var sums = await _db.CurrentAccountMovements
            .Where(m => m.CustomerId == customerId && m.MovementDate < dateFrom.Value)
            .GroupBy(m => 1)
            .Select(g => new
            {
                Billing = g.Sum(m => m.BillingAmount),
                Budget = g.Sum(m => m.BudgetAmount)
            })
            .FirstOrDefaultAsync();

        return (sums?.Billing ?? 0, sums?.Budget ?? 0);
    }

    /// <summary>
    /// Recalculates running balances in-memory starting from the initial balance.
    /// Modifies the movements list in place.
    /// </summary>
    private static void RecalculateRunningBalances(List<CurrentAccountMovement> movements, decimal initialBilling, decimal initialBudget)
    {
        var runningBilling = initialBilling;
        var runningBudget = initialBudget;

        foreach (var movement in movements)
        {
            runningBilling += movement.BillingAmount;
            runningBudget += movement.BudgetAmount;
            movement.BillingRunningBalance = runningBilling;
            movement.BudgetRunningBalance = runningBudget;
        }
    }
}
