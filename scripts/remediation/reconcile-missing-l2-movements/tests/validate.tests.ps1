$ErrorActionPreference = 'Stop'

# RED contract: validation is independent, read-only, session-bound, and has no evidence store.
$package = Join-Path $PSScriptRoot '..'
foreach ($file in 'validate.ps1','validate.sql') {
    if (-not (Test-Path -LiteralPath (Join-Path $package $file))) { throw "RED: missing $file" }
}
$sql = Get-Content -LiteralPath (Join-Path $package 'validate.sql') -Raw
foreach ($token in '#ApprovedTargets','#BeforeMovementSnapshot','#BeforeAccountSnapshot','CurrentAccountMovements','CurrentAccounts','Quotes','BudgetAmount','BillingAmount','DocumentNumber','CustomerId','QuoteId','BranchId','ExpectedTotal','q.QuoteNumber = m.DocumentNumber','BudgetBalance','TotalBalance','COUNT(DISTINCT','9','4') {
    if (-not $sql.Contains($token)) { throw "RED: validate contract missing $token" }
}
if ($sql -match '\b(INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP)\b') { throw 'RED: validation must be read-only' }
if ($sql -match 'ops\.|Provenance|Claim|Replay|ExecutionId') { throw 'RED: durable control is forbidden' }
Write-Host 'validate RED contract unexpectedly passed'
