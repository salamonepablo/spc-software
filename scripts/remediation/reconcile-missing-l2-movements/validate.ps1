[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProtectedDirectory,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$BeforeMovementSnapshotPath,
    [Parameter(Mandatory)][string]$BeforeAccountSnapshotPath,
    [Parameter(Mandatory)][string]$ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RemediationSafety.ps1')

Invoke-RemediationLauncher -Stage 'validation' -PackageRoot $PSScriptRoot `
    -ProtectedDirectory $ProtectedDirectory -ManifestPath $ManifestPath -ReportPath $ReportPath `
    -AdditionalArtifactPaths @($BeforeMovementSnapshotPath, $BeforeAccountSnapshotPath) `
    -BuildReport {
        param($controls)
        ConvertTo-Json @{
            stage = 'validation'
            result = 'local-validation-passed'
            databaseValidation = 'pending'
            targetCount = $controls.TargetCount
            customerCount = $controls.CustomerCount
        }
    }
