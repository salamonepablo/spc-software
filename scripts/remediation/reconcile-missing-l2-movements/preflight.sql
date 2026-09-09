/* SELECT-only preflight qualification against the confirmed application schema.
   An authorized client binds externally validated targets in #ApprovedTargets
   with columns: MovementId, CustomerId, DocumentNumber, QuoteId, BranchId, ExpectedTotal.
   Expected cardinality: 6 distinct movements, 3 distinct customers.
   Each target must link to exactly one authoritative, non-voided PR quote. */

-- Step 1: Verify target cardinality before database qualification
DECLARE @DistinctMovements int;
DECLARE @DistinctCustomers int;
DECLARE @TargetCount int;

SELECT @TargetCount = COUNT(*),
       @DistinctMovements = COUNT(DISTINCT MovementId),
       @DistinctCustomers = COUNT(DISTINCT CustomerId)
FROM #ApprovedTargets;

-- Cardinality guard: exactly 6 distinct movements and 3 distinct customers
IF @TargetCount <> 6 OR @DistinctMovements <> 6 OR @DistinctCustomers <> 3
BEGIN
    SELECT 'CardinalityMismatch' AS QualificationResult,
           @TargetCount AS TargetCount,
           @DistinctMovements AS DistinctMovements,
           @DistinctCustomers AS DistinctCustomers;
    RETURN;
END

-- Step 2: Join targets with CurrentAccountMovements and Quotes to qualify each row
SELECT
    t.MovementId,
    t.CustomerId,
    t.DocumentNumber,
    t.QuoteId,
    t.BranchId,
    t.ExpectedTotal,
    m.Id AS MatchedMovementId,
    m.DocumentType,
    m.BudgetAmount,
    q.Id AS MatchedQuoteId,
    q.IsVoided,
    q.Total AS AuthoritativeTotal,
    CASE
        WHEN m.Id IS NULL THEN 'MovementNotFound'
        WHEN q.Id IS NULL THEN 'QuoteNotFound'
        WHEN q.IsVoided <> 0 THEN 'QuoteVoided'
        WHEN q.Total <> t.ExpectedTotal THEN 'ExpectedTotalMismatch'
        ELSE 'Qualified'
    END AS QualificationResult
FROM #ApprovedTargets AS t
INNER JOIN CurrentAccountMovements AS m
    ON m.Id = t.MovementId
    AND m.CustomerId = t.CustomerId
    AND m.DocumentNumber = t.DocumentNumber
INNER JOIN CurrentAccounts AS a
    ON a.CustomerId = t.CustomerId
INNER JOIN Quotes AS q
    ON q.Id = t.QuoteId
    AND q.BranchId = t.BranchId
    AND q.CustomerId = t.CustomerId
    AND q.QuoteNumber = t.DocumentNumber
WHERE m.DocumentType = 20
  AND m.BudgetAmount = 0
  AND q.IsVoided = 0;

-- Step 3: Verify exactly one authoritative linked quote per target
SELECT t.MovementId, t.QuoteId, COUNT(q.Id) AS QuoteMatchCount
FROM #ApprovedTargets AS t
INNER JOIN Quotes AS q
    ON q.Id = t.QuoteId
    AND q.BranchId = t.BranchId
    AND q.CustomerId = t.CustomerId
    AND q.QuoteNumber = t.DocumentNumber
WHERE q.IsVoided = 0
GROUP BY t.MovementId, t.QuoteId
HAVING COUNT(q.Id) <> 1;
