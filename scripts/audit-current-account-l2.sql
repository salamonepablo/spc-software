-- Read-only audit for quote (PR) movements that were recorded with no L2 amount.
-- DocumentType 20 is Quote/PR. This script contains SELECT statements only.

-- Detail: zero-L2 quote movements. A movement has no branch, so quote values are
-- shown only when CustomerId plus QuoteNumber resolves to exactly one quote.
WITH DeduplicatedZeroL2QuoteMovements AS
(
    SELECT
        movement.Id,
        movement.MovementDate,
        movement.CustomerId,
        movement.DocumentNumber,
        movement.BillingAmount,
        movement.BudgetAmount,
        movement.BillingRunningBalance,
        movement.BudgetRunningBalance,
        movement.Description,
        ROW_NUMBER() OVER (PARTITION BY movement.Id ORDER BY movement.Id) AS MovementRowNumber
    FROM dbo.CurrentAccountMovements AS movement
    WHERE movement.DocumentType = 20
      AND movement.BudgetAmount = 0
),
ZeroL2QuoteMovements AS
(
    SELECT
        Id,
        MovementDate,
        CustomerId,
        DocumentNumber,
        BillingAmount,
        BudgetAmount,
        BillingRunningBalance,
        BudgetRunningBalance,
        Description
    FROM DeduplicatedZeroL2QuoteMovements
    WHERE MovementRowNumber = 1
),
QuoteCandidates AS
(
    SELECT
        quote.CustomerId,
        quote.QuoteNumber,
        COUNT(*) AS QuoteCandidateCount
    FROM dbo.Quotes AS quote
    GROUP BY quote.CustomerId, quote.QuoteNumber
)
SELECT
    movement.Id AS MovementId,
    movement.MovementDate,
    movement.CustomerId,
    customer.CompanyName AS CustomerName,
    movement.DocumentNumber AS QuoteNumber,
    movement.BillingAmount,
    movement.BudgetAmount,
    movement.BillingRunningBalance,
    movement.BudgetRunningBalance,
    movement.Description,
    COALESCE(candidate.QuoteCandidateCount, 0) AS QuoteCandidateCount,
    CASE COALESCE(candidate.QuoteCandidateCount, 0)
        WHEN 0 THEN 'No matching quote'
        WHEN 1 THEN 'Unique matching quote'
        ELSE 'Ambiguous matching quotes'
    END AS QuoteMatchStatus,
    CASE WHEN candidate.QuoteCandidateCount = 1 THEN quote.Id END AS QuoteId,
    CASE WHEN candidate.QuoteCandidateCount = 1 THEN quote.QuoteDate END AS QuoteDate,
    CASE WHEN candidate.QuoteCandidateCount = 1 THEN quote.Total END AS QuoteTotal,
    CASE WHEN candidate.QuoteCandidateCount = 1 THEN quote.IsVoided END AS IsVoided,
    account.BillingBalance AS StoredBillingBalance,
    account.BudgetBalance AS StoredBudgetBalance,
    account.TotalBalance AS StoredTotalBalance
FROM ZeroL2QuoteMovements AS movement
LEFT JOIN dbo.Customers AS customer
    ON customer.Id = movement.CustomerId
LEFT JOIN QuoteCandidates AS candidate
    ON candidate.CustomerId = movement.CustomerId
   AND candidate.QuoteNumber = movement.DocumentNumber
LEFT JOIN dbo.Quotes AS quote
    ON quote.CustomerId = movement.CustomerId
   AND quote.QuoteNumber = movement.DocumentNumber
   AND candidate.QuoteCandidateCount = 1
LEFT JOIN dbo.CurrentAccounts AS account
    ON account.CustomerId = movement.CustomerId
ORDER BY movement.MovementDate, movement.Id;

-- Aggregate: use only deduplicated movement rows so branch-scoped quote candidates
-- cannot duplicate movement counts or monetary totals.
WITH DeduplicatedZeroL2QuoteMovements AS
(
    SELECT
        movement.Id,
        movement.CustomerId,
        movement.BillingAmount,
        movement.BudgetAmount,
        ROW_NUMBER() OVER (PARTITION BY movement.Id ORDER BY movement.Id) AS MovementRowNumber
    FROM dbo.CurrentAccountMovements AS movement
    WHERE movement.DocumentType = 20
      AND movement.BudgetAmount = 0
),
ZeroL2QuoteMovements AS
(
    SELECT
        Id,
        CustomerId,
        BillingAmount,
        BudgetAmount
    FROM DeduplicatedZeroL2QuoteMovements
    WHERE MovementRowNumber = 1
)
SELECT
    COUNT(*) AS ZeroL2QuoteMovementCount,
    COUNT(DISTINCT CustomerId) AS AffectedCustomerCount,
    COALESCE(SUM(BillingAmount), 0) AS RecordedBillingAmount,
    COALESCE(SUM(BudgetAmount), 0) AS RecordedL2Amount
FROM ZeroL2QuoteMovements;

-- Reconciliation: stored balances against amounts derived from the complete movement ledger.
WITH LedgerBalances AS
(
    SELECT
        CustomerId,
        COALESCE(SUM(BillingAmount), 0) AS LedgerBillingBalance,
        COALESCE(SUM(BudgetAmount), 0) AS LedgerBudgetBalance
    FROM dbo.CurrentAccountMovements
    GROUP BY CustomerId
)
SELECT
    COALESCE(account.CustomerId, ledger.CustomerId) AS CustomerId,
    customer.CompanyName AS CustomerName,
    account.BillingBalance AS StoredBillingBalance,
    ledger.LedgerBillingBalance,
    COALESCE(account.BillingBalance, 0) - COALESCE(ledger.LedgerBillingBalance, 0) AS BillingDifference,
    account.BudgetBalance AS StoredBudgetBalance,
    ledger.LedgerBudgetBalance,
    COALESCE(account.BudgetBalance, 0) - COALESCE(ledger.LedgerBudgetBalance, 0) AS BudgetDifference,
    account.TotalBalance AS StoredTotalBalance,
    COALESCE(ledger.LedgerBillingBalance, 0) + COALESCE(ledger.LedgerBudgetBalance, 0) AS LedgerTotalBalance,
    COALESCE(account.TotalBalance, 0)
        - (COALESCE(ledger.LedgerBillingBalance, 0) + COALESCE(ledger.LedgerBudgetBalance, 0)) AS TotalDifference
FROM dbo.CurrentAccounts AS account
FULL OUTER JOIN LedgerBalances AS ledger
    ON ledger.CustomerId = account.CustomerId
LEFT JOIN dbo.Customers AS customer
    ON customer.Id = COALESCE(account.CustomerId, ledger.CustomerId)
ORDER BY CustomerId;
