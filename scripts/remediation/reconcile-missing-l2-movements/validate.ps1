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
$repositoryRoot = (Get-CanonicalPath (Join-Path $PSScriptRoot '../../..'))
$executionId = [guid]::NewGuid().ToString('N')
try {
    $null = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $ManifestPath
    $null = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $BeforeMovementSnapshotPath
    $null = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $BeforeAccountSnapshotPath
    $report = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $ReportPath -WriteRequired

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) { throw 'ManifestUnavailable' }
    if (-not (Test-Path -LiteralPath $BeforeMovementSnapshotPath -PathType Leaf)) { throw 'BeforeMovementSnapshotUnavailable' }
    if (-not (Test-Path -LiteralPath $BeforeAccountSnapshotPath -PathType Leaf)) { throw 'BeforeAccountSnapshotUnavailable' }

    $controls = Assert-ManifestControls -Manifest (Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json)

    Write-ProtectedAtomicText -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $report -Content (ConvertTo-Json @{
        stage = 'validation'
        result = 'local-validation-passed'
        databaseValidation = 'pending'
        targetCount = $controls.TargetCount
        customerCount = $controls.CustomerCount
    })
    Write-Host (Format-RedactedStatus -Stage 'validation' -ExecutionId $executionId -ErrorCategory 'None')
} catch {
    $errorCategory = Resolve-PreflightErrorCategory -ErrorMessage ([string]$_.Exception.Message)
    Write-Host (Format-RedactedStatus -Stage 'validation' -ExecutionId $executionId -ErrorCategory $errorCategory)
    exit 1
}
