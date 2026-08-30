$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../RemediationSafety.ps1')

function Assert-InvalidManifest([object]$Manifest, [string]$Category) {
    try { Assert-ManifestControls -Manifest $Manifest | Out-Null; throw "Expected $Category" } catch { if ($_.Exception.Message -ne $Category) { throw } }
}
function New-SyntheticManifest([int]$Targets = 9, [int]$Customers = 4) {
    $rows = 1..$Targets | ForEach-Object { [pscustomobject]@{ movementControl="synthetic-movement-$_"; customerControl="synthetic-customer-" + (($_ - 1) % $Customers); documentControl="synthetic-document-$_"; quoteControl="synthetic-quote-$_"; approved=$true } }
    [pscustomobject]@{ approvalControl='synthetic-approved'; expectedTargetCount=$Targets; expectedCustomerCount=$Customers; targets=@($rows) }
}

$malformed = New-SyntheticManifest; $malformed.approvalControl = ''
Assert-InvalidManifest $malformed 'ManifestApprovalInvalid'
$valid = New-SyntheticManifest
$result = Assert-ManifestControls -Manifest $valid
if ($result.TargetCount -ne 9 -or $result.CustomerCount -ne 4 -or $result.UniqueQuoteCount -ne 9) { throw 'Valid synthetic controls did not qualify.' }
$duplicate = New-SyntheticManifest; $duplicate.targets[1].quoteControl = $duplicate.targets[0].quoteControl
Assert-InvalidManifest $duplicate 'ManifestQuoteNotUnique'
$duplicateMovement = New-SyntheticManifest; $duplicateMovement.targets[1].movementControl = $duplicateMovement.targets[0].movementControl
Assert-InvalidManifest $duplicateMovement 'ManifestMovementNotUnique'
$duplicateDocument = New-SyntheticManifest; $duplicateDocument.targets[1].documentControl = $duplicateDocument.targets[0].documentControl
Assert-InvalidManifest $duplicateDocument 'ManifestDocumentNotUnique'
$unapproved = New-SyntheticManifest; $unapproved.targets[0].approved = $false
Assert-InvalidManifest $unapproved 'ManifestTargetUnapproved'
$malformedTarget = New-SyntheticManifest; $malformedTarget.targets[0].quoteControl = ''
Assert-InvalidManifest $malformedTarget 'ManifestTargetShapeInvalid'
$countDrift = New-SyntheticManifest -Targets 8
Assert-InvalidManifest $countDrift 'ManifestTargetCountInvalid'
$customerDrift = New-SyntheticManifest -Customers 3
Assert-InvalidManifest $customerDrift 'ManifestCustomerCountInvalid'
Write-Host 'preflight tests passed'

$parameters = New-PreflightSqlParameters -RequiredDocumentType 'PR' -RequiredInitialBudgetAmount ([decimal]'0.00')
if ($parameters.Count -ne 2 -or $parameters[0].ParameterName -ne '@RequiredDocumentType' -or $parameters[0].Value -ne 'PR' -or $parameters[1].ParameterName -ne '@RequiredInitialBudgetAmount' -or $parameters[1].DbType -ne [System.Data.DbType]::Decimal -or [decimal]$parameters[1].Value -ne [decimal]0) { throw 'Preflight SQL parameters were not bound safely.' }
$qualified = Assert-SyntheticQualification -Candidate ([pscustomobject]@{ documentType='PR'; budgetAmount=0; quoteMatches=1; accountExists=$true; inManifest=$true })
if (-not $qualified) { throw 'Qualified synthetic candidate was rejected.' }
$protectedDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('preflight-security-' + [guid]::NewGuid().ToString('N'))
try {
    $null = New-Item -ItemType Directory -Path $protectedDirectory
    $sensitiveToken = 'synthetic-secret-token-should-not-leak'
    $invalidManifest = New-SyntheticManifest -Targets 8
    $invalidManifest.approvalControl = $sensitiveToken
    $manifestPath = Join-Path $protectedDirectory 'manifest.json'
    $reportPath = Join-Path $protectedDirectory 'report.json'
    $invalidManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -NoNewline
    $preflightScript = Join-Path $PSScriptRoot '../preflight.ps1'
    $output = & /home/pablo/.local/bin/pwsh -NoProfile -File $preflightScript -ProtectedDirectory $protectedDirectory -ManifestPath $manifestPath -ReportPath $reportPath 2>&1 | Out-String
    if ($LASTEXITCODE -ne 1) { throw 'Malformed manifest did not fail.' }
    if ($output -match [regex]::Escape($sensitiveToken)) { throw 'Sensitive manifest content leaked to preflight output.' }
    if ($output -notmatch 'category=ManifestTargetCountInvalid') { throw 'Preflight did not emit an allow-listed error category.' }

    $malformedToken = 'synthetic-malformed-secret-token-should-not-leak'
    $malformedManifestPath = Join-Path $protectedDirectory 'malformed-manifest.json'
    Set-Content -LiteralPath $malformedManifestPath -Value ('{"approvalControl":"' + $malformedToken + '"') -NoNewline
    $malformedOutput = & /home/pablo/.local/bin/pwsh -NoProfile -File $preflightScript -ProtectedDirectory $protectedDirectory -ManifestPath $malformedManifestPath -ReportPath $reportPath 2>&1 | Out-String
    if ($LASTEXITCODE -ne 1) { throw 'Malformed JSON manifest did not fail.' }
    if ($malformedOutput -match [regex]::Escape($malformedToken)) { throw 'Sensitive malformed input leaked to preflight output.' }
    if ($malformedOutput -notmatch 'category=PreflightFailed') { throw 'Malformed JSON did not use the fallback allow-listed category.' }

    $validManifestPath = Join-Path $protectedDirectory 'valid-manifest.json'
    $validReportPath = Join-Path $protectedDirectory 'valid-report.json'
    (New-SyntheticManifest | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $validManifestPath -NoNewline
    $validOutput = & /home/pablo/.local/bin/pwsh -NoProfile -File $preflightScript -ProtectedDirectory $protectedDirectory -ManifestPath $validManifestPath -ReportPath $validReportPath 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw 'Valid local manifest validation failed.' }
    if ($validOutput -notmatch 'category=None' -or $validOutput -match 'qualified') { throw 'Local manifest validation falsely claimed database qualification.' }
    $report = Get-Content -LiteralPath $validReportPath -Raw | ConvertFrom-Json
    if ($report.result -ne 'local-validation-passed' -or $report.databaseQualification -ne 'pending') { throw 'Report did not identify database qualification as pending.' }
} finally {
    if (Test-Path -LiteralPath $protectedDirectory) { Remove-Item -LiteralPath $protectedDirectory -Recurse -Force }
}

foreach ($candidate in @(
    [pscustomobject]@{ documentType='XX'; budgetAmount=0; quoteMatches=1; accountExists=$true; inManifest=$true },
    [pscustomobject]@{ documentType='PR'; budgetAmount=1; quoteMatches=1; accountExists=$true; inManifest=$true },
    [pscustomobject]@{ documentType='PR'; budgetAmount=0; quoteMatches=0; accountExists=$true; inManifest=$true },
    [pscustomobject]@{ documentType='PR'; budgetAmount=0; quoteMatches=2; accountExists=$true; inManifest=$true },
    [pscustomobject]@{ documentType='PR'; budgetAmount=0; quoteMatches=1; accountExists=$false; inManifest=$true },
    [pscustomobject]@{ documentType='PR'; budgetAmount=0; quoteMatches=1; accountExists=$true; inManifest=$false }
)) {
    try { Assert-SyntheticQualification -Candidate $candidate | Out-Null; throw 'Expected qualification rejection.' } catch { if ($_.Exception.Message -notmatch '^Qualification') { throw } }
}
