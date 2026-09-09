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
foreach ($scenario in 'MalformedStaging','DuplicateStaging','TargetCount','CustomerCount','NonPr','NonzeroL2','MissingQuote','AmbiguousQuote','WrongCustomer','WrongDocument','WrongQuoteId','WrongQuoteDocumentIdentity','WrongBranchId','InvalidQuoteState','SourceTotalMismatch','AtomicRollback','SerializableTransaction','Commit','FullLedger','PreservedIdentity','NonTarget','NonScopedAccount','PostChangeValidation','NonTargetFieldValidation','AddedNonTargetMovementValidation','DeletedNonTargetMovementValidation','AddedNonScopedAccountValidation','DeletedNonScopedAccountValidation') {
    if (-not $harnessText.Contains($scenario)) { throw "RED: SQL behavior scenario missing $scenario" }
}
foreach ($required in 'synthetic','finally','Remove-Item','SqlServer','Reset-SyntheticFixture','Invoke-Scenario','# Valid 6/3 staging data') {
    if (-not $harnessText.Contains($required)) { throw "RED: SQL harness safety contract missing $required" }
}
if ($harnessText -notmatch "function Invoke-Scenario \{[\s\S]*?Reset-SyntheticFixture") {
    throw 'RED: every behavioral scenario must reset the synthetic fixture before execution'
}

# Invoke-Scenario owns the sole baseline reset. Every preservation or post-apply assertion
# then establishes committed valid state through the shared valid-apply helper.
if ($harnessText -notmatch 'function Invoke-ValidApply \{[\s\S]*?New-StagingTable \$script:validStagingValues[\s\S]*?\$batch = "USE \[synthetic_test\];`n\$stagingSql`n\$script:applySql"[\s\S]*?\$r = Invoke-SqlBatch \$batch[\s\S]*?if \(-not \$r\.Success\)') {
    throw 'RED: Invoke-ValidApply must stage, run, and confirm the valid apply.sql batch'
}
foreach ($scenario in 'MalformedStaging','DuplicateStaging','TargetCount','CustomerCount','NonPr','NonzeroL2','MissingQuote','AmbiguousQuote','WrongCustomer','WrongDocument','WrongQuoteId','WrongQuoteDocumentIdentity','WrongBranchId','InvalidQuoteState','SourceTotalMismatch','AtomicRollback','SerializableTransaction','Commit','FullLedger','PreservedIdentity','NonTarget','NonScopedAccount','PostChangeValidation','NonTargetFieldValidation','AddedNonTargetMovementValidation','DeletedNonTargetMovementValidation','AddedNonScopedAccountValidation','DeletedNonScopedAccountValidation') {
    $scenarioStart = $harnessText.IndexOf("Invoke-Scenario '$scenario' {")
    if ($scenarioStart -lt 0) { throw "RED: scenario block missing $scenario" }
    $nextScenarioStart = $harnessText.IndexOf("Invoke-Scenario '", $scenarioStart + 1)
    if ($nextScenarioStart -lt 0) { $nextScenarioStart = $harnessText.Length }
    $scenarioText = $harnessText.Substring($scenarioStart, $nextScenarioStart - $scenarioStart)
    if ($scenarioText -match 'Reset-SyntheticFixture') {
        throw "RED: $scenario must rely on Invoke-Scenario for its baseline reset"
    }
}
foreach ($scenario in 'Commit','FullLedger','PreservedIdentity','NonTarget','NonScopedAccount') {
    $scenarioStart = $harnessText.IndexOf("Invoke-Scenario '$scenario' {")
    $nextScenarioStart = $harnessText.IndexOf("Invoke-Scenario '", $scenarioStart + 1)
    if ($nextScenarioStart -lt 0) { $nextScenarioStart = $harnessText.Length }
    $scenarioText = $harnessText.Substring($scenarioStart, $nextScenarioStart - $scenarioStart)
    if ($scenarioText -notmatch "Invoke-ValidApply '$scenario'") {
        throw "RED: $scenario must stage and successfully apply the valid target set before assertions"
    }
}
# Post-change validation must execute validate.sql with the same session-local approved target
# and before snapshots, rather than being represented by a token or ad-hoc SELECT.
foreach ($required in '$script:validateSql','New-BeforeMovementSnapshot','New-BeforeAccountSnapshot','Invoke-PostChangeValidation','#BeforeMovementSnapshot','#BeforeAccountSnapshot','AddedNonTargetMovementValidation','DeletedNonTargetMovementValidation','AddedNonScopedAccountValidation','DeletedNonScopedAccountValidation') {
    if (-not $harnessText.Contains($required)) { throw "RED: SQL behavior harness missing executable validation contract $required" }
}
foreach ($scenario in 'PostChangeValidation','NonTargetFieldValidation','AddedNonTargetMovementValidation','DeletedNonTargetMovementValidation','AddedNonScopedAccountValidation','DeletedNonScopedAccountValidation') {
    $scenarioStart = $harnessText.IndexOf("Invoke-Scenario '$scenario' {")
    $nextScenarioStart = $harnessText.IndexOf("Invoke-Scenario '", $scenarioStart + 1)
    if ($nextScenarioStart -lt 0) { $nextScenarioStart = $harnessText.Length }
    $scenarioText = $harnessText.Substring($scenarioStart, $nextScenarioStart - $scenarioStart)
    if ($scenarioText -notmatch "Invoke-PostChangeValidation '$scenario'") {
        throw "RED: $scenario must execute validate.sql with session-local before snapshots"
    }
}

$expectedScenarioCount = 28
$actualScenarioCount = ([regex]::Matches($harnessText, "(?m)^\s*Invoke-Scenario '")).Count
if ($actualScenarioCount -ne $expectedScenarioCount) {
    throw "RED: harness scenario count is $actualScenarioCount, expected $expectedScenarioCount"
}
foreach ($required in "All $expectedScenarioCount scenarios execute", "Would run $expectedScenarioCount scenarios", "All $expectedScenarioCount scenarios PASSED") {
    if (-not $harnessText.Contains($required)) { throw "RED: harness scenario summary missing $required" }
}

$readme = Get-Content -LiteralPath (Join-Path $package 'README.md') -Raw
foreach ($required in 'Apply-SyntheticSqlServer.ps1',"$expectedScenarioCount behavior scenarios",'Never run real data without a verified backup and confirmed writer/API exclusion','PR3 scope') {
    if (-not $readme.Contains($required)) { throw "RED: README contract missing $required" }
}

# The docker-exec harness must not publish a host port, and docker run failure must stop before readiness polling.
if ($harnessText -match '(?m)^\s*-p\s+') { throw 'RED: docker-exec harness must not publish a host port' }
$dockerRunIndex = $harnessText.IndexOf('docker.exe run -d')
$dockerRunExitCheckIndex = $harnessText.IndexOf('$dockerRunExitCode', $dockerRunIndex)
$readinessIndex = $harnessText.IndexOf('# Wait for SQL Server readiness')
if ($dockerRunExitCheckIndex -lt $dockerRunIndex -or $dockerRunExitCheckIndex -ge $readinessIndex) {
    throw 'RED: docker run failure must be checked before readiness polling'
}

# FullLedger must assert the exact derived balances, including non-target movement 7 in customer 103's ledger.
foreach ($required in 'CustomerId IN (101,102,103)','(101,48.00,119.00)','(102,29.00,119.00)','(103,16.00,146.00)','FullLedger: ledger-derived balances do not match expected values') {
    if (-not $harnessText.Contains($required)) { throw "RED: FullLedger exact-balance contract missing $required" }
}

# The valid six-target success path must prove excluded movements and customer 104 are unchanged.
$nonTargetScenarioStart = $harnessText.IndexOf("Invoke-Scenario 'NonTarget' {")
$nonTargetScenarioEnd = $harnessText.IndexOf("Invoke-Scenario '", $nonTargetScenarioStart + 1)
$nonTargetScenario = $harnessText.Substring($nonTargetScenarioStart, $nonTargetScenarioEnd - $nonTargetScenarioStart)
foreach ($required in 'Id IN (7,8,9)','CustomerId = 104','NonTarget: excluded movements 7, 8, and 9 changed','NonTarget: customer 104 changed') {
    if (-not $nonTargetScenario.Contains($required)) { throw "RED: valid success-path preservation assertion missing $required" }
}

# The harness uses docker exec/sqlcmd; it must not claim a SqlClient dependency or retain an
# unused connection string.
foreach ($forbidden in 'System.Data.SqlClient','$script:connectionString','Uses SqlClient directly') {
    if ($harnessText.Contains($forbidden)) { throw "RED: harness must not contain misleading SqlClient content: $forbidden" }
}

# Docker daemon availability must be checked before attempting docker run, preventing the suite
# from waiting for SQL Server readiness after a daemon connection failure.
$dockerCheckIndex = $harnessText.IndexOf('$dockerInfo = docker.exe info')
$dockerRunIndex = $harnessText.IndexOf('docker.exe run -d')
if ($dockerCheckIndex -lt 0 -or $dockerCheckIndex -ge $dockerRunIndex) {
    throw 'RED: harness must verify Docker Desktop availability before docker run'
}
if (-not $harnessText.Contains('Docker Desktop is unavailable')) {
    throw 'RED: harness must report a clear Docker Desktop unavailable error'
}

# The documented package suite invokes this script, so this contract test must exercise the
# disposable SQL Server behavior harness rather than only inspect its source tokens.
try {
    $harnessOutput = & $harness 2>&1
    $harnessExitCode = $LASTEXITCODE
}
catch {
    $harnessOutput = $_ | Out-String
    $harnessExitCode = 1
}
if ($harnessExitCode -ne 0) {
    if ($harnessOutput -notmatch 'Docker Desktop is unavailable') {
        throw "SQL Server behavioral harness failed with exit code ${harnessExitCode}: $harnessOutput"
    }
    Write-Host 'SQL Server behavior suite verified Docker Desktop unavailable failure'
    exit 0
}
Write-Host 'SQL Server behavior suite passed'
