[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProtectedDirectory,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RemediationSafety.ps1')

Invoke-RemediationLauncher -Stage 'preflight' -PackageRoot $PSScriptRoot `
    -ProtectedDirectory $ProtectedDirectory -ManifestPath $ManifestPath -ReportPath $ReportPath `
    -BuildReport {
        param($controls)
        ConvertTo-Json @{
            stage = 'preflight'
            result = 'local-validation-passed'
            databaseQualification = 'pending'
            targetCount = $controls.TargetCount
            customerCount = $controls.CustomerCount
        }
    }
