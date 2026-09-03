/* Read-only post-change validation for the bounded L2 reconciliation transaction.
   Consumes session-local temp tables supplied by the caller:
     #ApprovedTargets        (MovementId, CustomerId, DocumentNumber, QuoteId, BranchId, ExpectedTotal)
     #BeforeMovementSnapshot (Id, CustomerId, DocumentType, DocumentNumber, BillingAmount, BudgetAmount, MovementDate, Description)
     #BeforeAccountSnapshot  (Id, CustomerId, BillingBalance, BudgetBalance, TotalBalance)
   No mutation is performed. All checks are SELECT-based with THROW on failure. */

-- Fixed scope declarations (single amendment point).
DECLARE @RequiredTargetCount int = 9;
DECLARE @RequiredCustomerCount int = 4;

-- Validation 1: Exactly 9 L2 changes.
DECLARE @changedCount int;
SELECT @changedCount = COUNT(*)
FROM #BeforeMovementSnapshot AS b
INNER JOIN CurrentAccountMovements AS m ON m.Id = b.Id
INNER JOIN #ApprovedTargets AS t ON t.MovementId = b.Id
WHERE b.BudgetAmount = 0
  AND m.BudgetAmount = t.ExpectedTotal;

IF @changedCount <> @RequiredTargetCount
    THROW 50002, 'Validation failed: expected L2 BudgetAmount change count does not match required target count', 1;

-- Validation 2: Each changed value equals the authoritative quote Total and document identity.
DECLARE @sourceMatchCount int;
SELECT @sourceMatchCount = COUNT(*)
FROM CurrentAccountMovements AS m
INNER JOIN #ApprovedTargets AS t
    ON t.MovementId = m.Id
    AND t.CustomerId = m.CustomerId
    AND t.DocumentNumber = m.DocumentNumber
INNER JOIN Quotes AS q
    ON q.Id = t.QuoteId
    AND q.BranchId = t.BranchId
    AND q.CustomerId = t.CustomerId
    AND q.QuoteNumber = m.DocumentNumber
WHERE m.BudgetAmount = q.Total
  AND q.IsVoided = 0;

IF @sourceMatchCount <> @RequiredTargetCount
    THROW 50002, 'Validation failed: not all BudgetAmount values match authoritative Quotes Total', 1;

-- Validation 3: Preserved identity, linkage, L1, and non-target fields.
DECLARE @preservedCount int;
SELECT @preservedCount = COUNT(*)
FROM #BeforeMovementSnapshot AS b
INNER JOIN CurrentAccountMovements AS m ON m.Id = b.Id
INNER JOIN #ApprovedTargets AS t ON t.MovementId = b.Id
WHERE m.CustomerId = b.CustomerId
  AND m.DocumentType = b.DocumentType
  AND m.DocumentNumber = b.DocumentNumber
  AND m.BillingAmount = b.BillingAmount
  AND m.MovementDate = b.MovementDate
  AND (m.Description = b.Description OR (m.Description IS NULL AND b.Description IS NULL));

IF @preservedCount <> @RequiredTargetCount
    THROW 50002, 'Validation failed: identity linkage L1 or non-target fields were not preserved', 1;

-- Validation 4: Exactly 4 account cache changes derived from full ledgers.
DECLARE @validAccountCount int;
SELECT @validAccountCount = COUNT(*)
FROM #BeforeAccountSnapshot AS b
INNER JOIN CurrentAccounts AS a ON a.CustomerId = b.CustomerId
INNER JOIN (
    SELECT m.CustomerId,
           SUM(m.BudgetAmount) AS LedgerBudget,
           SUM(m.BillingAmount + m.BudgetAmount) AS LedgerTotal
    FROM CurrentAccountMovements AS m
    WHERE m.CustomerId IN (SELECT DISTINCT CustomerId FROM #ApprovedTargets)
    GROUP BY m.CustomerId
) AS agg ON agg.CustomerId = a.CustomerId
WHERE a.BudgetBalance = agg.LedgerBudget
  AND a.TotalBalance = agg.LedgerTotal
  AND a.BillingBalance = b.BillingBalance;

IF @validAccountCount <> @RequiredCustomerCount
    THROW 50002, 'Validation failed: account cache change count does not match required customer count', 1;

-- Validation 5a: Non-scoped accounts must be completely unchanged.
DECLARE @nonScopedChanged int;
SELECT @nonScopedChanged = COUNT(*)
FROM #BeforeAccountSnapshot AS b
INNER JOIN CurrentAccounts AS a ON a.CustomerId = b.CustomerId
WHERE b.CustomerId NOT IN (SELECT DISTINCT CustomerId FROM #ApprovedTargets)
  AND (a.BudgetBalance <> b.BudgetBalance
       OR a.TotalBalance <> b.TotalBalance
       OR a.BillingBalance <> b.BillingBalance);

IF @nonScopedChanged > 0
    THROW 50002, 'Validation failed: non-scoped account was modified', 1;

-- Validation 5b: Non-target movements must be completely unchanged.
DECLARE @nonTargetChanged int;
SELECT @nonTargetChanged = COUNT(*)
FROM #BeforeMovementSnapshot AS b
INNER JOIN CurrentAccountMovements AS m ON m.Id = b.Id
WHERE b.Id NOT IN (SELECT MovementId FROM #ApprovedTargets)
  AND (m.BudgetAmount <> b.BudgetAmount
       OR m.BillingAmount <> b.BillingAmount
       OR m.CustomerId <> b.CustomerId
       OR m.DocumentNumber <> b.DocumentNumber);

IF @nonTargetChanged > 0
    THROW 50002, 'Validation failed: non-target movement was modified', 1;

-- Validation 5c: Additions and removals outside the approved movement scope are forbidden.
DECLARE @AddedNonTargetMovements int, @DeletedNonTargetMovements int;
SELECT @AddedNonTargetMovements = COUNT(*)
FROM CurrentAccountMovements AS m
WHERE NOT EXISTS (SELECT 1 FROM #BeforeMovementSnapshot AS b WHERE b.Id = m.Id)
  AND NOT EXISTS (SELECT 1 FROM #ApprovedTargets AS t WHERE t.MovementId = m.Id);

SELECT @DeletedNonTargetMovements = COUNT(*)
FROM #BeforeMovementSnapshot AS b
WHERE NOT EXISTS (SELECT 1 FROM #ApprovedTargets AS t WHERE t.MovementId = b.Id)
  AND NOT EXISTS (SELECT 1 FROM CurrentAccountMovements AS m WHERE m.Id = b.Id);

IF @AddedNonTargetMovements > 0 OR @DeletedNonTargetMovements > 0
    THROW 50002, 'Validation failed: non-target movement set changed after apply', 1;

-- Validation 5d: Additions and removals outside the scoped account set are forbidden.
DECLARE @AddedNonScopedAccounts int, @DeletedNonScopedAccounts int;
SELECT @AddedNonScopedAccounts = COUNT(*)
FROM CurrentAccounts AS a
WHERE NOT EXISTS (SELECT 1 FROM #BeforeAccountSnapshot AS b WHERE b.CustomerId = a.CustomerId)
  AND NOT EXISTS (SELECT 1 FROM #ApprovedTargets AS t WHERE t.CustomerId = a.CustomerId);

SELECT @DeletedNonScopedAccounts = COUNT(*)
FROM #BeforeAccountSnapshot AS b
WHERE NOT EXISTS (SELECT 1 FROM #ApprovedTargets AS t WHERE t.CustomerId = b.CustomerId)
  AND NOT EXISTS (SELECT 1 FROM CurrentAccounts AS a WHERE a.CustomerId = b.CustomerId);

IF @AddedNonScopedAccounts > 0 OR @DeletedNonScopedAccounts > 0
    THROW 50002, 'Validation failed: non-scoped account set changed after apply', 1;

-- Cardinality confirmation.
DECLARE @movementCardinality int, @customerCardinality int;
SELECT @movementCardinality = COUNT(DISTINCT MovementId),
       @customerCardinality = COUNT(DISTINCT CustomerId)
FROM #ApprovedTargets;

IF @movementCardinality <> @RequiredTargetCount OR @customerCardinality <> @RequiredCustomerCount
    THROW 50002, 'Validation failed: staging cardinality mismatch', 1;

SELECT 'L2ChangeCount' AS ValidationStep, 'Passed' AS Result, @changedCount AS Value
UNION ALL
SELECT 'SourceTotalMatch', 'Passed', @sourceMatchCount
UNION ALL
SELECT 'PreservedFields', 'Passed', @preservedCount
UNION ALL
SELECT 'AccountCacheDerived', 'Passed', @validAccountCount
UNION ALL
SELECT 'NonScopedUnchanged', 'Passed', @nonScopedChanged
UNION ALL
SELECT 'NonTargetUnchanged', 'Passed', @nonTargetChanged
UNION ALL
SELECT 'AddedNonTargetMovements', 'Passed', @AddedNonTargetMovements
UNION ALL
SELECT 'DeletedNonTargetMovements', 'Passed', @DeletedNonTargetMovements
UNION ALL
SELECT 'AddedNonScopedAccounts', 'Passed', @AddedNonScopedAccounts
UNION ALL
SELECT 'DeletedNonScopedAccounts', 'Passed', @DeletedNonScopedAccounts
UNION ALL
SELECT 'StagingCardinality', 'Passed', @movementCardinality;
