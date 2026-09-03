$ErrorActionPreference = 'Stop'

# RED contract only: all inputs are synthetic and a later harness must create/dispose SQL Server.
$package = Join-Path $PSScriptRoot '..'
$fixture = Join-Path $PSScriptRoot 'fixtures/synthetic-ledger.sql'
if (-not (Test-Path -LiteralPath $fixture)) { throw 'RED: missing synthetic actual-schema fixture' }
$fixtureSql = Get-Content -LiteralPath $fixture -Raw
foreach ($token in 'CurrentAccountMovements','CurrentAccounts','Quotes','DocumentType int','DocumentNumber bigint','BranchId int','IsVoided bit','decimal(18,2)','(1, 101','(9, ''2026-01-01'',104,20,1009','synthetic non-target','synthetic non-scoped') {
    if (-not $fixtureSql.Contains($token)) { throw "RED: fixture contract missing $token" }
}

$apply = Join-Path $package 'apply.sql'
if (-not (Test-Path -LiteralPath $apply)) { throw 'RED: missing apply.sql' }
$sql = Get-Content -LiteralPath $apply -Raw
foreach ($token in 'SET XACT_ABORT ON','SET TRANSACTION ISOLATION LEVEL SERIALIZABLE','BEGIN TRANSACTION','ROLLBACK TRANSACTION','#ApprovedTargets','MovementId','CustomerId','DocumentNumber','QuoteId','BranchId','ExpectedTotal','@RequiredDocumentType','@RequiredInitialBudgetAmount','@RequiredTargetCount','@RequiredCustomerCount','Quotes','q.QuoteNumber = t.DocumentNumber','IsVoided = 0','UPDATE m SET BudgetAmount','UPDATE a SET BudgetBalance','BillingBalance','TotalBalance','COUNT(DISTINCT','9','4') {
    if (-not $sql.Contains($token)) { throw "RED: apply contract missing $token" }
}
if ($sql -match 'CREATE\s+TABLE|ops\.|Provenance|Claim|Replay|ExecutionId') { throw 'RED: durable control is forbidden' }
Write-Host 'apply RED contract unexpectedly passed'
