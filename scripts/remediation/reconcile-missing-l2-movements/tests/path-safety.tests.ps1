$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../RemediationSafety.ps1')

function Assert-ThrowsCategory([scriptblock]$Action, [string]$Category) {
    try { & $Action; throw "Expected $Category" } catch { if ($_.Exception.Message -notmatch "^$Category$") { throw } }
}

$repository = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$external = Join-Path ([System.IO.Path]::GetTempPath()) ("reconcile-synthetic-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $external | Out-Null
try {
    Assert-ThrowsCategory { Assert-ProtectedPath -RepositoryRoot $repository -ProtectedDirectory $repository -ArtifactPath (Join-Path $repository 'manifest.json') -WriteRequired } 'RepositoryPathRejected'
    $relativeAlias = [System.IO.Path]::Combine($repository, '..', (Split-Path $repository -Leaf), 'report.json')
    Assert-ThrowsCategory { Assert-ProtectedPath -RepositoryRoot $repository -ProtectedDirectory $external -ArtifactPath $relativeAlias -WriteRequired } 'RepositoryPathRejected'
    Assert-ThrowsCategory { Assert-ProtectedPath -RepositoryRoot $repository -ProtectedDirectory (Join-Path $external 'missing') -ArtifactPath (Join-Path $external 'missing/report.json') -WriteRequired } 'ProtectedDirectoryUnavailable'
    Assert-ThrowsCategory { Assert-ProtectedPath -RepositoryRoot $repository -ProtectedDirectory $external -ArtifactPath (Join-Path $external '../outside.json') -WriteRequired } 'ArtifactOutsideProtectedDirectory'

    $alias = Join-Path $external 'repository-alias'
    New-Item -ItemType SymbolicLink -Path $alias -Target $repository | Out-Null
    Assert-ThrowsCategory { Assert-ProtectedPath -RepositoryRoot $repository -ProtectedDirectory $external -ArtifactPath (Join-Path $alias 'report.json') -WriteRequired } 'RepositoryPathRejected'

    New-Item -ItemType Directory -Path (Join-Path $external 'reports') | Out-Null
    $safe = Assert-ProtectedPath -RepositoryRoot $repository -ProtectedDirectory $external -ArtifactPath (Join-Path $external 'reports/report.json') -WriteRequired
    if ($safe -notmatch 'reports') { throw 'Safe protected path was not returned.' }
    $message = Format-RedactedStatus -Stage 'preflight' -ExecutionId 'opaque-id' -ErrorCategory 'ManifestInvalid' -SensitiveDetail 'synthetic-secret'
    if ($message -ne 'stage=preflight execution=opaque-id category=ManifestInvalid' -or $message -match 'synthetic-secret') { throw 'Console output was not redacted.' }
    Write-Host 'path-safety tests passed'
} finally { Remove-Item -Force -Recurse $external -ErrorAction SilentlyContinue }
