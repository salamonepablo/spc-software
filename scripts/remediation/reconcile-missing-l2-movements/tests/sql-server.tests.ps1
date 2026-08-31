$ErrorActionPreference = 'Stop'

# RED behavior matrix for a future disposable SQL Server harness. It MUST accept only
# synthetic database/container settings and delete its database/container/output in finally.
$package = Join-Path $PSScriptRoot '..'
$fixture = Join-Path $PSScriptRoot 'fixtures/synthetic-ledger.sql'
$apply = Join-Path $package 'apply.sql'
$harness = Join-Path $package 'Apply-SyntheticSqlServer.ps1'
if (-not (Test-Path -LiteralPath $fixture)) { throw 'RED: missing synthetic fixture' }
if (-not (Test-Path -LiteralPath $apply)) { throw 'RED: missing apply.sql for disposable behavior tests' }
if (-not (Test-Path -LiteralPath $harness)) { throw 'RED: missing disposable SQL Server harness' }
$harnessText = Get-Content -LiteralPath $harness -Raw
foreach ($scenario in 'MalformedStaging','DuplicateStaging','TargetCount','CustomerCount','NonPr','NonzeroL2','MissingQuote','AmbiguousQuote','WrongCustomer','WrongDocument','WrongQuoteId','WrongQuoteDocumentIdentity','WrongBranchId','InvalidQuoteState','SourceTotalMismatch','AtomicRollback','SerializableTransaction','Commit','FullLedger','PreservedIdentity','NonTarget','NonScopedAccount','PostChangeValidation') {
    if (-not $harnessText.Contains($scenario)) { throw "RED: SQL behavior scenario missing $scenario" }
}
foreach ($required in 'synthetic','finally','Remove-Item','SqlServer') {
    if (-not $harnessText.Contains($required)) { throw "RED: SQL harness safety contract missing $required" }
}
Write-Host 'SQL Server RED contract unexpectedly passed'
