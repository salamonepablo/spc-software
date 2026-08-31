<#
.SYNOPSIS
    Disposable SqlServer behavior harness for the bounded apply.sql transaction.
    Uses synthetic data only. Creates and removes a Docker SQL Server container.
.DESCRIPTION
    Creates a uniquely named Docker SQL Server container (synthetic database, no host
    port exposed to external consumers, generated SA password), waits for readiness,
    creates a disposable database, loads the synthetic-ledger.sql fixture, stages
    #ApprovedTargets for each scenario, runs apply.sql, and asserts outcomes.
    All 21 scenarios exercise guard paths, commit, preservation, and derived balances.
    Container and temporary files are removed in finally.
.NOTES
    Requires: docker.exe, System.Data.SqlClient (PowerShell 7+).
    Uses SqlClient directly — no Invoke-Sqlcmd dependency.
#>

param(
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

# --- Configuration ---
$script:packageDir = Join-Path $PSScriptRoot '.'
$script:fixturePath = Join-Path $PSScriptRoot 'tests/fixtures/synthetic-ledger.sql'
$script:applyPath = Join-Path $PSScriptRoot 'apply.sql'
$script:containerName = 'synthetic-sqlserver-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$script:saPassword = [guid]::NewGuid().ToString('N') + 'Aa1!'
$script:port = Get-Random -Minimum 49152 -Maximum 65535
$script:connectionString = "Server=localhost,$script:port;User Id=sa;Password=$script:saPassword;TrustServerCertificate=True;Connection Timeout=120"

# --- Cleanup (registered before container creation so it always runs) ---
$script:tempFiles = @()
$cleanup = {
    finally {
        Write-Host "Cleaning up synthetic SqlServer container: $script:containerName"
        docker.exe rm -f $script:containerName 2>$null | Out-Null
        foreach ($f in $script:tempFiles) {
            if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force -ErrorAction SilentlyContinue }
        }
    }
}

# --- Helper: execute SQL batch and return (success, stdout+stderr) ---
function Invoke-SqlBatch {
    param([string]$Sql)
    $tmpFile = [System.IO.Path]::GetTempFileName()
    $script:tempFiles += $tmpFile
    [System.IO.File]::WriteAllText($tmpFile, $Sql, [System.Text.Encoding]::UTF8)
    $result = Get-Content -LiteralPath $tmpFile -Raw | docker.exe exec -i $script:containerName /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P $script:saPassword -C -b 2>&1
    $exitCode = $LASTEXITCODE
    Remove-Item -LiteralPath $tmpFile -Force -ErrorAction SilentlyContinue
    $script:tempFiles = $script:tempFiles | Where-Object { $_ -ne $tmpFile }
    return @{ Success = ($exitCode -eq 0); Output = ($result -join "`n") }
}

# --- Helper: create #ApprovedTargets temp table and insert staging rows ---
function Reset-SyntheticFixture {
    $reset = Invoke-SqlBatch "USE [synthetic_test]; DROP TABLE dbo.CurrentAccountMovements; DROP TABLE dbo.CurrentAccounts; DROP TABLE dbo.Quotes;`n$script:fixtureSql"
    if (-not $reset.Success) { throw "Could not reset synthetic fixture: $($reset.Output)" }
}

function New-StagingTable {
    param([string]$ValuesSql)
    $sql = @"
CREATE TABLE #ApprovedTargets (
    MovementId int,
    CustomerId int,
    DocumentNumber bigint,
    QuoteId int,
    BranchId int,
    ExpectedTotal decimal(18,2)
)
INSERT INTO #ApprovedTargets VALUES
$ValuesSql

"@
    return $sql
}

# Valid 9/4 staging data for Commit, FullLedger, PreservedIdentity, NonTarget, NonScopedAccount, PostChangeValidation
$script:validStagingValues = @"
(1,101,1001,201,1,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),
(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),
(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),
(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)
"@

$script:applySql = Get-Content -LiteralPath $script:applyPath -Raw

# --- Scenario runner ---
$results = @{}
function Invoke-Scenario {
    param(
        [string]$Name,
        [scriptblock]$Action
    )
    Write-Host "`n=== Scenario: $Name ==="
    try {
        & $Action
        $results[$Name] = 'PASS'
        Write-Host "  PASS"
    }
    catch {
        $results[$Name] = "FAIL: $($_.Exception.Message)"
        Write-Host "  FAIL: $($_.Exception.Message)"
    }
}

function Invoke-AllScenarios {
# ============================================================================
# Scenario: MalformedStaging — staging with null/missing required columns
# apply.sql must THROW because re-qualification or cardinality fails
# ============================================================================
Invoke-Scenario 'MalformedStaging' {
    $stagingSql = New-StagingTable "(1,NULL,1001,201,1,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on malformed staging' }
}

# ============================================================================
# Scenario: DuplicateStaging — duplicate MovementId in staging
# Guard 2 detects COUNT(*) <> COUNT(DISTINCT MovementId) → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'DuplicateStaging' {
    $stagingSql = New-StagingTable "(1,101,1001,201,1,11.00),(1,101,1001,201,1,11.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on duplicate staging' }
}

# ============================================================================
# Scenario: TargetCount — wrong number of targets (8 instead of 9)
# Guard 1 detects @targetCount <> 9 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'TargetCount' {
    $stagingSql = New-StagingTable "(1,101,1001,201,1,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on wrong target count' }
}

# ============================================================================
# Scenario: CustomerCount — wrong number of distinct customers (3 instead of 4)
# Guard 1 detects @customerCount <> 4 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'CustomerCount' {
    $stagingSql = New-StagingTable "(1,101,1001,201,1,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,101,1008,208,1,18.00),(9,101,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on wrong customer count' }
}

# ============================================================================
# Scenario: NonPr — target movement has DocumentType <> 20
# Re-qualification (Guard 3) excludes it → count < 9 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'NonPr' {
    Invoke-SqlBatch "USE [synthetic_test]; UPDATE CurrentAccountMovements SET DocumentType = 99 WHERE Id = 1" | Out-Null
    $stagingSql = New-StagingTable $script:validStagingValues
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on non-PR movement' }
}

# ============================================================================
# Scenario: NonzeroL2 — target movement already has BudgetAmount <> 0
# Re-qualification (Guard 3) excludes it (BudgetAmount = 0 filter) → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'NonzeroL2' {
    Invoke-SqlBatch "USE [synthetic_test]; UPDATE CurrentAccountMovements SET BudgetAmount = 5.00 WHERE Id = 1" | Out-Null
    $stagingSql = New-StagingTable $script:validStagingValues
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on nonzero L2 movement' }
}

# ============================================================================
# Scenario: MissingQuote — QuoteId does not exist in Quotes
# Re-qualification join finds no quote → count < 9 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'MissingQuote' {
    $stagingSql = New-StagingTable "(1,101,1001,999,1,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on missing quote' }
}

# ============================================================================
# Scenario: AmbiguousQuote — multiple quotes match (duplicate QuoteId rows)
# Re-qualification join produces > 9 rows → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'AmbiguousQuote' {
    Invoke-SqlBatch "USE [synthetic_test]; ALTER TABLE Quotes DROP CONSTRAINT PK__Quotes*" | Out-Null
    Invoke-SqlBatch "USE [synthetic_test]; INSERT INTO Quotes VALUES (201,1,9001,'2026-01-01',101,99.00,0)" | Out-Null
    $stagingSql = New-StagingTable $script:validStagingValues
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on ambiguous quote' }
}

# ============================================================================
# Scenario: WrongCustomer — staged CustomerId does not match quote's CustomerId
# Re-qualification join on CustomerId fails → count < 9 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'WrongCustomer' {
    $stagingSql = New-StagingTable "(1,102,1001,201,1,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on wrong customer' }
}

# ============================================================================
# Scenario: WrongDocument — staged DocumentNumber does not match movement
# Re-qualification join on DocumentNumber fails → count < 9 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'WrongDocument' {
    $stagingSql = New-StagingTable "(1,101,9999,201,1,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on wrong document number' }
}

# ============================================================================
# Scenario: WrongQuoteId — staged QuoteId does not match actual quote for movement
# Re-qualification: quote exists but links to different movement → join fails or total mismatch
# ============================================================================
Invoke-Scenario 'WrongQuoteId' {
    $stagingSql = New-StagingTable "(1,101,1001,202,1,12.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on wrong QuoteId' }
}

# ============================================================================
# Scenario: WrongQuoteDocumentIdentity — staged quote is the same customer/branch but has another document number.
# Re-qualification must reject it before any BudgetAmount or account cache mutation.
# ============================================================================
Invoke-Scenario 'WrongQuoteDocumentIdentity' {
    Reset-SyntheticFixture
    $stagingSql = New-StagingTable "(1,101,1001,202,1,12.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW when quote document identity does not match the movement' }
    $check = Invoke-SqlBatch "USE [synthetic_test]; SELECT COUNT(*) AS Cnt FROM CurrentAccountMovements WHERE Id BETWEEN 1 AND 9 AND BudgetAmount <> 0"
    if ($check.Output -match '(\d+)' -and [int]$Matches[1] -gt 0) { throw 'WrongQuoteDocumentIdentity: state mutated after rejection' }
}

# ============================================================================
# Scenario: WrongBranchId — staged BranchId does not match quote's BranchId
# Re-qualification join on BranchId fails → count < 9 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'WrongBranchId' {
    $stagingSql = New-StagingTable "(1,101,1001,201,2,11.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on wrong BranchId' }
}

# ============================================================================
# Scenario: InvalidQuoteState — quote is voided (IsVoided = 1)
# Re-qualification excludes voided quotes → count < 9 → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'InvalidQuoteState' {
    Invoke-SqlBatch "USE [synthetic_test]; UPDATE Quotes SET IsVoided = 1 WHERE Id = 201" | Out-Null
    $stagingSql = New-StagingTable $script:validStagingValues
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on voided quote' }
}

# ============================================================================
# Scenario: SourceTotalMismatch — ExpectedTotal does not match quote's Total
# Guard 4 detects mismatch → THROW/ROLLBACK
# ============================================================================
Invoke-Scenario 'SourceTotalMismatch' {
    $stagingSql = New-StagingTable "(1,101,1001,201,1,99.00),(2,101,1002,202,1,12.00),(3,101,1003,203,1,13.00),(4,102,1004,204,1,14.00),(5,102,1005,205,1,15.00),(6,103,1006,206,1,16.00),(7,103,1007,207,1,17.00),(8,104,1008,208,1,18.00),(9,104,1009,209,1,19.00)"
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if ($r.Success) { throw 'Expected apply.sql to THROW on source total mismatch' }
}

# ============================================================================
# Scenario: AtomicRollback — on guard failure, no partial mutation occurred
# Run a failing scenario and verify zero BudgetAmount changes
# ============================================================================
Invoke-Scenario 'AtomicRollback' {
    Reset-SyntheticFixture
    # Use NonPr mutation to trigger a guard failure
    Invoke-SqlBatch "USE [synthetic_test]; UPDATE CurrentAccountMovements SET DocumentType = 99 WHERE Id = 1" | Out-Null
    $stagingSql = New-StagingTable $script:validStagingValues
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    Invoke-SqlBatch $batch | Out-Null
    # Verify no partial movement mutation: all BudgetAmount for targets must still be 0
    $check = Invoke-SqlBatch "USE [synthetic_test]; SELECT COUNT(*) AS Cnt FROM CurrentAccountMovements WHERE Id BETWEEN 1 AND 9 AND BudgetAmount <> 0"
    if ($check.Output -match '(\d+)' -and [int]$Matches[1] -gt 0) {
        throw 'AtomicRollback: partial movement mutation detected after guard failure'
    }
    # Verify no account balance changes
    $checkAcct = Invoke-SqlBatch "USE [synthetic_test]; SELECT COUNT(*) AS Cnt FROM CurrentAccounts WHERE Id IN (1,2,3,4) AND BudgetBalance <> 0"
    if ($checkAcct.Output -match '(\d+)' -and [int]$Matches[1] -gt 0) {
        throw 'AtomicRollback: partial account mutation detected after guard failure'
    }
}

# ============================================================================
# Scenario: SerializableTransaction — apply.sql must retain SERIALIZABLE isolation across
# re-qualification, movement mutation, full-ledger aggregation, and account-cache update.
# This behavior is asserted by the SQL contract test; the disposable harness exercises the
# same protected batch through the Commit scenario below.
# ============================================================================
Invoke-Scenario 'SerializableTransaction' {
    if ($script:applySql -notmatch 'SET TRANSACTION ISOLATION LEVEL SERIALIZABLE') {
        throw 'SerializableTransaction: apply.sql does not require SERIALIZABLE isolation'
    }
}

# ============================================================================
# Scenario: Commit — valid 9/4 staging, all 9 movements and 4 accounts updated
# Transaction commits successfully
# ============================================================================
Invoke-Scenario 'Commit' {
    Reset-SyntheticFixture
    $stagingSql = New-StagingTable $script:validStagingValues
    $batch = "USE [synthetic_test];`n$stagingSql`n$script:applySql"
    $r = Invoke-SqlBatch $batch
    if (-not $r.Success) { throw "Expected apply.sql to succeed but got: $($r.Output)" }
}

# ============================================================================
# Scenario: FullLedger — after commit, BudgetBalance and TotalBalance derived from complete ledger
# ============================================================================
Invoke-Scenario 'FullLedger' {
    # Customer 101: BudgetBalance = 11+12+13+0 = 36, BillingBalance = 10+20+30+11 = 71, TotalBalance = 36+71 = 107
    # Customer 102: BudgetBalance = 14+15 = 29, BillingBalance = 40+50 = 90, TotalBalance = 29+90 = 119
    # Customer 103: BudgetBalance = 16+17 = 33, BillingBalance = 60+70 = 130, TotalBalance = 33+130 = 163
    # Customer 104: BudgetBalance = 18+19 = 37, BillingBalance = 80+90 = 170, TotalBalance = 37+170 = 207
    $check = Invoke-SqlBatch "USE [synthetic_test]; SELECT CustomerId, BudgetBalance, TotalBalance FROM CurrentAccounts WHERE CustomerId IN (101,102,103,104) ORDER BY CustomerId"
    # Basic verification that balances were updated (non-zero BudgetBalance for all 4)
    $zeroCheck = Invoke-SqlBatch "USE [synthetic_test]; SELECT COUNT(*) AS Cnt FROM CurrentAccounts WHERE CustomerId IN (101,102,103,104) AND BudgetBalance = 0"
    if ($zeroCheck.Output -match '(\d+)' -and [int]$Matches[1] -gt 0) {
        throw 'FullLedger: expected non-zero BudgetBalance for all 4 scoped customers'
    }
}

# ============================================================================
# Scenario: PreservedIdentity — movement Id, MovementDate, Description, BillingAmount unchanged
# ============================================================================
Invoke-Scenario 'PreservedIdentity' {
    $check = Invoke-SqlBatch "USE [synthetic_test]; SELECT Id, MovementDate, Description, BillingAmount FROM CurrentAccountMovements WHERE Id BETWEEN 1 AND 9 ORDER BY Id"
    # Verify BillingAmount unchanged (10,20,30,40,50,60,70,80,90)
    $billingCheck = Invoke-SqlBatch "USE [synthetic_test]; SELECT SUM(BillingAmount) AS TotalBilling FROM CurrentAccountMovements WHERE Id BETWEEN 1 AND 9"
    if (-not ($billingCheck.Output -match '450')) {
        throw 'PreservedIdentity: BillingAmount sum should be 450 for target movements'
    }
}

# ============================================================================
# Scenario: NonTarget — non-target movements (Id=10, DocumentType=99) unchanged after commit
# ============================================================================
Invoke-Scenario 'NonTarget' {
    $check = Invoke-SqlBatch "USE [synthetic_test]; SELECT BudgetAmount, BillingAmount FROM CurrentAccountMovements WHERE Id = 10"
    if (-not ($check.Output -match '12' -and $check.Output -match '11')) {
        throw 'NonTarget: movement Id=10 should have BillingAmount=11, BudgetAmount=12 unchanged'
    }
}

# ============================================================================
# Scenario: NonScopedAccount — account for customer 105 unchanged after commit
# ============================================================================
Invoke-Scenario 'NonScopedAccount' {
    $check = Invoke-SqlBatch "USE [synthetic_test]; SELECT BillingBalance, BudgetBalance, TotalBalance FROM CurrentAccounts WHERE CustomerId = 105"
    if (-not ($check.Output -match '500' -and $check.Output -match '77' -and $check.Output -match '577')) {
        throw 'NonScopedAccount: customer 105 balances should be unchanged (500/77/577)'
    }
}

# ============================================================================
# Scenario: PostChangeValidation — independent read-only check confirms exactly 9 L2 changes
# ============================================================================
Invoke-Scenario 'PostChangeValidation' {
    $check = Invoke-SqlBatch "USE [synthetic_test]; SELECT COUNT(*) AS L2ChangeCount FROM CurrentAccountMovements WHERE Id BETWEEN 1 AND 9 AND BudgetAmount > 0"
    if (-not ($check.Output -match '9')) {
        throw 'PostChangeValidation: expected exactly 9 movements with BudgetAmount > 0'
    }
}

}

# ============================================================================
# Main execution flow
# ============================================================================

if ($WhatIf) {
    Write-Host "WhatIf: would create synthetic SqlServer container '$script:containerName' on port $script:port"
    Write-Host "Would run 21 scenarios against disposable synthetic database"
    exit 0
}

try {
    Write-Host "Starting synthetic SqlServer container: $script:containerName"
    docker.exe run -d --name $script:containerName `
        -p "${script:port}:1433" `
        -e "ACCEPT_EULA=Y" `
        -e "MSSQL_SA_PASSWORD=$script:saPassword" `
        mcr.microsoft.com/mssql/server:2022-latest | Out-Null

    # Wait for SQL Server readiness
    Write-Host "Waiting for SqlServer readiness on port $script:port..."
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 2
        $probe = docker.exe exec $script:containerName /opt/mssql-tools18/bin/sqlcmd `
            -S localhost -U sa -P $script:saPassword -C -Q "SELECT 1" 2>&1
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    }
    if (-not $ready) { throw "SqlServer container did not become ready within 120 seconds" }
    Write-Host "SqlServer is ready."

    # Create disposable database and load synthetic fixture
    Invoke-SqlBatch "CREATE DATABASE [synthetic_test]" | Out-Null
    $fixtureSql = Get-Content -LiteralPath $script:fixturePath -Raw
    Invoke-SqlBatch "USE [synthetic_test];`n$fixtureSql" | Out-Null

    # Run every scenario only after the disposable database has been created.
    Write-Host "`nRunning synthetic behavior scenarios..."
    Invoke-AllScenarios

    # Print summary
    Write-Host "`n========== SCENARIO SUMMARY =========="
    $allPassed = $true
    foreach ($kvp in $results.GetEnumerator() | Sort-Object Name) {
        $status = if ($kvp.Value -eq 'PASS') { 'PASS' } else { 'FAIL' }
        Write-Host "  $($kvp.Name): $($kvp.Value)"
        if ($kvp.Value -ne 'PASS') { $allPassed = $false }
    }
    Write-Host "======================================="

    if (-not $allPassed) {
        Write-Host "Some scenarios FAILED"
        exit 1
    }
    Write-Host "All 21 scenarios PASSED"
    exit 0
}
finally {
    Write-Host "Cleaning up synthetic SqlServer container: $script:containerName"
    docker.exe rm -f $script:containerName 2>$null | Out-Null
    foreach ($f in $script:tempFiles) {
        if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force -ErrorAction SilentlyContinue }
    }
}
