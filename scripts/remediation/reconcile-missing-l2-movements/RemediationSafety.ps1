Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CanonicalPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $segments = $fullPath.TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).Split([System.IO.Path]::DirectorySeparatorChar, [System.StringSplitOptions]::RemoveEmptyEntries)
    $current = [System.IO.Path]::GetPathRoot($fullPath)
    foreach ($segment in $segments) {
        $current = Join-Path $current $segment
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                $current = $item.ResolveLinkTarget($true).FullName
            }
        }
    }
    [System.IO.Path]::GetFullPath($current)
}

function Test-PathWithin {
    param([Parameter(Mandatory)][string]$Child, [Parameter(Mandatory)][string]$Parent)
    $comparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
    $normalParent = $Parent.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $normalChild = $Child.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    ($normalChild.Equals($normalParent, $comparison)) -or ($normalChild.StartsWith($normalParent + [System.IO.Path]::DirectorySeparatorChar, $comparison))
}

function Assert-ProtectedPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$ProtectedDirectory,
        [Parameter(Mandatory)][string]$ArtifactPath,
        [switch]$WriteRequired
    )

    $repository = Get-CanonicalPath $RepositoryRoot
    if (-not (Test-Path -LiteralPath $ProtectedDirectory -PathType Container)) { throw 'ProtectedDirectoryUnavailable' }
    $protected = Get-CanonicalPath $ProtectedDirectory
    $artifact = Get-CanonicalPath $ArtifactPath
    if ((Test-PathWithin -Child $protected -Parent $repository) -or (Test-PathWithin -Child $artifact -Parent $repository)) { throw 'RepositoryPathRejected' }
    if (-not (Test-PathWithin -Child $artifact -Parent $protected)) { throw 'ArtifactOutsideProtectedDirectory' }
    if ($WriteRequired) {
        try {
            $probe = Join-Path $protected ('.access-' + [guid]::NewGuid().ToString('N'))
            [System.IO.File]::WriteAllText($probe, '')
            Remove-Item -LiteralPath $probe -Force
        } catch { throw 'ProtectedDirectoryUnavailable' }
    }
    return $artifact
}

function Resolve-PreflightErrorCategory {
    param([Parameter(Mandatory)][string]$ErrorMessage)

    $allowedCategories = @(
        'ProtectedDirectoryUnavailable',
        'RepositoryPathRejected',
        'ArtifactOutsideProtectedDirectory',
        'ManifestUnavailable',
        'ManifestApprovalInvalid',
        'ManifestTargetCountInvalid',
        'ManifestCustomerCountInvalid',
        'ManifestTargetShapeInvalid',
        'ManifestTargetUnapproved',
        'ManifestMovementNotUnique',
        'ManifestDocumentNotUnique',
        'ManifestQuoteNotUnique',
        'ManifestExpectedTotalInvalid',
        'BeforeMovementSnapshotUnavailable',
        'BeforeAccountSnapshotUnavailable'
    )
    if ($allowedCategories -contains $ErrorMessage) { return $ErrorMessage }
    return 'PreflightFailed'
}

function Format-RedactedStatus {
    param([Parameter(Mandatory)][string]$Stage, [Parameter(Mandatory)][string]$ExecutionId, [Parameter(Mandatory)][string]$ErrorCategory, [string]$SensitiveDetail)
    "stage=$Stage execution=$ExecutionId category=$ErrorCategory"
}

function Assert-ManifestControls {
    param([Parameter(Mandatory)]$Manifest)
    if ([string]::IsNullOrWhiteSpace([string]$Manifest.approvalControl)) { throw 'ManifestApprovalInvalid' }
    if ($Manifest.expectedTargetCount -ne 9 -or @($Manifest.targets).Count -ne 9) { throw 'ManifestTargetCountInvalid' }
    foreach ($target in @($Manifest.targets)) {
        foreach ($propertyName in @('movementId', 'customerId', 'documentNumber', 'quoteId', 'branchId')) {
            $property = $target.PSObject.Properties[$propertyName]
            if ($null -eq $property -or ($property.Value -isnot [int] -and $property.Value -isnot [long]) -or $property.Value -le 0) { throw 'ManifestTargetShapeInvalid' }
        }
        $expectedTotalProperty = $target.PSObject.Properties['expectedTotal']
        if ($null -eq $expectedTotalProperty -or -not ($expectedTotalProperty.Value -is [double] -or $expectedTotalProperty.Value -is [decimal]) -or $expectedTotalProperty.Value -le 0) { throw 'ManifestExpectedTotalInvalid' }
        if ($null -eq $target.PSObject.Properties['approved'] -or $target.approved -isnot [bool]) { throw 'ManifestTargetShapeInvalid' }
    }
    $customers = @($Manifest.targets | ForEach-Object customerId | Select-Object -Unique)
    if ($Manifest.expectedCustomerCount -ne 4 -or $customers.Count -ne 4) { throw 'ManifestCustomerCountInvalid' }
    if (@($Manifest.targets | Where-Object { -not $_.approved }).Count -ne 0) { throw 'ManifestTargetUnapproved' }
    if (@($Manifest.targets | ForEach-Object movementId | Select-Object -Unique).Count -ne 9) { throw 'ManifestMovementNotUnique' }
    if (@($Manifest.targets | ForEach-Object documentNumber | Select-Object -Unique).Count -ne 9) { throw 'ManifestDocumentNotUnique' }
    if (@($Manifest.targets | ForEach-Object quoteId | Select-Object -Unique).Count -ne 9) { throw 'ManifestQuoteNotUnique' }
    [pscustomobject]@{ TargetCount = 9; CustomerCount = 4; UniqueMovementCount = 9; UniqueDocumentCount = 9; UniqueQuoteCount = 9 }
}

function New-PreflightSqlParameters {
    param(
        [Parameter(Mandatory)][string]$RequiredDocumentType,
        [Parameter(Mandatory)][decimal]$RequiredInitialBudgetAmount
    )

    $documentType = [System.Data.SqlClient.SqlParameter]::new('@RequiredDocumentType', [System.Data.SqlDbType]::NVarChar, 16)
    $documentType.Value = $RequiredDocumentType
    $budgetAmount = [System.Data.SqlClient.SqlParameter]::new('@RequiredInitialBudgetAmount', [System.Data.SqlDbType]::Decimal)
    $budgetAmount.Precision = 18
    $budgetAmount.Scale = 4
    $budgetAmount.Value = $RequiredInitialBudgetAmount
    return @($documentType, $budgetAmount)
}

function Assert-SyntheticQualification {
    param([Parameter(Mandatory)]$Candidate)
    if (-not $Candidate.inManifest) { throw 'QualificationOutsideManifest' }
    if ($Candidate.documentType -ne 'PR') { throw 'QualificationDocumentTypeInvalid' }
    if ([decimal]$Candidate.budgetAmount -ne [decimal]0) { throw 'QualificationBudgetAmountInvalid' }
    if ($Candidate.quoteMatches -ne 1) { throw 'QualificationQuoteNotUnique' }
    if (-not $Candidate.accountExists) { throw 'QualificationAccountMissing' }
    return $true
}

function Write-ProtectedAtomicText {
    param([Parameter(Mandatory)][string]$RepositoryRoot, [Parameter(Mandatory)][string]$ProtectedDirectory, [Parameter(Mandatory)][string]$ArtifactPath, [Parameter(Mandatory)][string]$Content)
    $artifact = Assert-ProtectedPath -RepositoryRoot $RepositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $ArtifactPath -WriteRequired
    $parent = Split-Path -Parent $artifact
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { throw 'ProtectedDirectoryUnavailable' }
    $temporary = "$artifact.$([guid]::NewGuid().ToString('N')).tmp"
    $null = Assert-ProtectedPath -RepositoryRoot $RepositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $temporary -WriteRequired
    [System.IO.File]::WriteAllText($temporary, $Content)
    Move-Item -LiteralPath $temporary -Destination $artifact -Force
}

function Invoke-RemediationLauncher {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Stage,
        [Parameter(Mandatory)][string]$PackageRoot,
        [Parameter(Mandatory)][string]$ProtectedDirectory,
        [Parameter(Mandatory)][string]$ManifestPath,
        [Parameter(Mandatory)][string]$ReportPath,
        [string[]]$AdditionalArtifactPaths = @(),
        [Parameter(Mandatory)][scriptblock]$BuildReport
    )

    $repositoryRoot = Get-CanonicalPath (Join-Path $PackageRoot '../../..')
    $executionId = [guid]::NewGuid().ToString('N')
    try {
        $null = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $ManifestPath
        foreach ($extra in $AdditionalArtifactPaths) {
            $null = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $extra
        }
        $report = Assert-ProtectedPath -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $ReportPath -WriteRequired

        if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) { throw 'ManifestUnavailable' }
        foreach ($extra in $AdditionalArtifactPaths) {
            if (-not (Test-Path -LiteralPath $extra -PathType Leaf)) {
                $label = (Split-Path -Leaf $extra) -replace '\.[^.]*$',''
                throw "${label}Unavailable"
            }
        }

        $controls = Assert-ManifestControls -Manifest (Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json)
        $reportContent = & $BuildReport $controls
        Write-ProtectedAtomicText -RepositoryRoot $repositoryRoot -ProtectedDirectory $ProtectedDirectory -ArtifactPath $report -Content $reportContent
        Write-Host (Format-RedactedStatus -Stage $Stage -ExecutionId $executionId -ErrorCategory 'None')
    } catch {
        $errorCategory = Resolve-PreflightErrorCategory -ErrorMessage ([string]$_.Exception.Message)
        Write-Host (Format-RedactedStatus -Stage $Stage -ExecutionId $executionId -ErrorCategory $errorCategory)
        exit 1
    }
}
