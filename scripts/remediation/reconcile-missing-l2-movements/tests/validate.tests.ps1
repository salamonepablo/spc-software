$ErrorActionPreference = 'Stop'

# RED contract: validation is independent, read-only, session-bound, and has no evidence store.
$package = Join-Path $PSScriptRoot '..'
foreach ($file in 'validate.ps1','validate.sql') {
    if (-not (Test-Path -LiteralPath (Join-Path $package $file))) { throw "RED: missing $file" }
}
$sql = Get-Content -LiteralPath (Join-Path $package 'validate.sql') -Raw
foreach ($token in '#ApprovedTargets','#BeforeMovementSnapshot','#BeforeAccountSnapshot','CurrentAccountMovements','CurrentAccounts','Quotes','BudgetAmount','BillingAmount','DocumentNumber','CustomerId','QuoteId','BranchId','ExpectedTotal','q.QuoteNumber = m.DocumentNumber','BudgetBalance','TotalBalance','COUNT(DISTINCT','@ValidationRequiredTargetCount int = 6','@ValidationRequiredCustomerCount int = 3','AddedNonTargetMovements','DeletedNonTargetMovements','AddedNonScopedAccounts','DeletedNonScopedAccounts') {
    if (-not $sql.Contains($token)) { throw "RED: validate contract missing $token" }
}

# Validation 5b must protect every non-target movement field promised by the snapshot contract.
$nonTargetValidationStart = $sql.IndexOf('-- Validation 5b:')
$nonTargetValidationEnd = $sql.IndexOf('-- Validation 5c:', $nonTargetValidationStart)
if ($nonTargetValidationStart -lt 0 -or $nonTargetValidationEnd -lt 0) { throw 'RED: validation 5b boundaries are missing' }
$nonTargetValidation = $sql.Substring($nonTargetValidationStart, $nonTargetValidationEnd - $nonTargetValidationStart)
foreach ($field in 'm.DocumentType <> b.DocumentType','m.MovementDate <> b.MovementDate','m.Description <> b.Description','m.Description IS NULL AND b.Description IS NOT NULL','m.Description IS NOT NULL AND b.Description IS NULL') {
    if (-not $nonTargetValidation.Contains($field)) { throw "RED: validation 5b does not preserve $field" }
}
if ($sql -match '\b(INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)\b') { throw 'RED: validation must be read-only' }
if ($sql -match 'ops\.|Provenance|Claim|Replay|ExecutionId') { throw 'RED: durable control is forbidden' }
Write-Host 'validate RED contract unexpectedly passed'
