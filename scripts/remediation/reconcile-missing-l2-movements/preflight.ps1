[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProtectedDirectory,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RemediationSafety.ps1')
$repositoryRoot = (Get-CanonicalPath (Join-Path $PSScriptRoot '../../..'))
$executionId = [guid]::NewGuid().ToString('N')
try {
    $manifest = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $ManifestPath
    $report = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $ReportPath -WriteRequired
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw 'ManifestUnavailable' }
    $controls = Assert-ManifestControls -Manifest (Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json)
    Write-ProtectedAtomicText -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $report -Content (ConvertTo-Json @{ stage = 'preflight'; result = 'local-validation-passed'; databaseQualification = 'pending'; targetCount = $controls.TargetCount; customerCount = $controls.CustomerCount })
    Write-Host (Format-RedactedStatus -Stage 'preflight' -ExecutionId $executionId -ErrorCategory 'None')
} catch {
    $errorCategory = Resolve-PreflightErrorCategory -ErrorMessage ([string]$_.Exception.Message)
    Write-Host (Format-RedactedStatus -Stage 'preflight' -ExecutionId $executionId -ErrorCategory $errorCategory)
    exit 1
}
