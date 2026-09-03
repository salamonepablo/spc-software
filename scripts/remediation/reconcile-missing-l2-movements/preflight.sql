/* SELECT-only. An authorized client binds externally validated targets in #ApprovedTargets. */
SELECT
    target.MovementControl,
    target.CustomerControl,
    target.DocumentControl,
    target.QuoteControl,
    CASE WHEN target.IsApproved = 1 THEN 1 ELSE 0 END AS ApprovalControlValid
FROM #ApprovedTargets AS target
WHERE target.DocumentType = @RequiredDocumentType
  AND target.InitialBudgetAmount = @RequiredInitialBudgetAmount;
