-- Bounded atomic apply transaction for reconciling missing L2 quote movements.
-- Receives session-local temp table #ApprovedTargets (created by the caller) with columns:
--   MovementId int, CustomerId int, DocumentNumber bigint, QuoteId int, BranchId int, ExpectedTotal decimal(18,2)
-- Does not create durable tables, schemas, or records. Transaction rollback only on failure.
SET XACT_ABORT ON
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
BEGIN TRANSACTION

-- Fixed scope declarations (single amendment point).
DECLARE @RequiredTargetCount int = 9;
DECLARE @RequiredCustomerCount int = 4;
DECLARE @RequiredDocumentType int = 20;   -- SPC.Shared.Models.DocumentType.Quote = 20 (PR)
DECLARE @RequiredInitialBudgetAmount decimal(18,2) = 0;

-- Guard 1: Cardinality — exactly 9 targets, 9 distinct movements, 4 distinct customers
DECLARE @targetCount int, @movementCount int, @customerCount int
SELECT @targetCount = COUNT(*),
       @movementCount = COUNT(DISTINCT MovementId),
       @customerCount = COUNT(DISTINCT CustomerId)
FROM #ApprovedTargets

IF @targetCount <> @RequiredTargetCount OR @movementCount <> @RequiredTargetCount OR @customerCount <> @RequiredCustomerCount
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Cardinality guard failed: expected target/movement/customer counts do not match', 1;
END

-- Guard 2: No duplicate MovementId in staging
IF (SELECT COUNT(*) FROM #ApprovedTargets) <> (SELECT COUNT(DISTINCT MovementId) FROM #ApprovedTargets)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Duplicate MovementId detected in staging', 1;
END

-- Guard 3: Re-qualify all targets against live data within the protected transaction.
DECLARE @requalifyCount int
SELECT @requalifyCount = COUNT(*)
FROM #ApprovedTargets AS t
INNER JOIN CurrentAccountMovements AS m WITH (UPDLOCK, HOLDLOCK)
    ON m.Id = t.MovementId
    AND m.CustomerId = t.CustomerId
    AND m.DocumentNumber = t.DocumentNumber
INNER JOIN Quotes AS q WITH (UPDLOCK, HOLDLOCK)
    ON q.Id = t.QuoteId
    AND q.BranchId = t.BranchId
    AND q.CustomerId = t.CustomerId
    AND q.QuoteNumber = t.DocumentNumber
WHERE m.DocumentType = @RequiredDocumentType
    AND m.BudgetAmount = @RequiredInitialBudgetAmount
    AND q.IsVoided = 0

IF @requalifyCount <> @RequiredTargetCount
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Re-qualification guard failed: qualifying row count does not match required target count', 1;
END

-- Guard 4: Each fully linked quote Total must equal the staged ExpectedTotal.
DECLARE @totalMismatch int
SELECT @totalMismatch = COUNT(*)
FROM #ApprovedTargets AS t
INNER JOIN CurrentAccountMovements AS m WITH (UPDLOCK, HOLDLOCK)
    ON m.Id = t.MovementId
    AND m.CustomerId = t.CustomerId
    AND m.DocumentNumber = t.DocumentNumber
INNER JOIN Quotes AS q WITH (UPDLOCK, HOLDLOCK)
    ON q.Id = t.QuoteId
    AND q.BranchId = t.BranchId
    AND q.CustomerId = t.CustomerId
    AND q.QuoteNumber = t.DocumentNumber
WHERE q.Total <> t.ExpectedTotal

IF @totalMismatch > 0
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Source total mismatch: ExpectedTotal does not match authoritative quote Total', 1;
END

-- Step 5: Update qualifying movement BudgetAmount to authoritative Quotes.Total.
UPDATE m SET BudgetAmount = q.Total
FROM CurrentAccountMovements AS m
INNER JOIN #ApprovedTargets AS t
    ON m.Id = t.MovementId
    AND m.CustomerId = t.CustomerId
    AND m.DocumentNumber = t.DocumentNumber
INNER JOIN Quotes AS q
    ON q.Id = t.QuoteId
    AND q.BranchId = t.BranchId
    AND q.CustomerId = t.CustomerId
    AND q.QuoteNumber = t.DocumentNumber
WHERE m.DocumentType = @RequiredDocumentType
    AND m.BudgetAmount = @RequiredInitialBudgetAmount
    AND q.IsVoided = 0
    AND q.Total = t.ExpectedTotal

IF @@ROWCOUNT <> @RequiredTargetCount
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Movement update row count mismatch: does not match required target count', 1;
END

-- Step 6: Derive and update account balances for the 4 scoped customers.
-- BillingBalance remains untouched; SERIALIZABLE isolation protects this complete-ledger aggregation from concurrent writes.
UPDATE a SET BudgetBalance = agg.BudgetBalance, TotalBalance = agg.TotalBalance
FROM CurrentAccounts AS a WITH (UPDLOCK, HOLDLOCK)
INNER JOIN (
    SELECT m.CustomerId,
           SUM(m.BudgetAmount) AS BudgetBalance,
           SUM(m.BillingAmount + m.BudgetAmount) AS TotalBalance
    FROM CurrentAccountMovements AS m WITH (UPDLOCK, HOLDLOCK)
    WHERE m.CustomerId IN (SELECT DISTINCT CustomerId FROM #ApprovedTargets)
    GROUP BY m.CustomerId
) AS agg ON a.CustomerId = agg.CustomerId

IF @@ROWCOUNT <> @RequiredCustomerCount
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Account update row count mismatch: does not match required customer count', 1;
END

COMMIT TRANSACTION
