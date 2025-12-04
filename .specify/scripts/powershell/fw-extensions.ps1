#!/usr/bin/env pwsh
# FieldWorks extensions to spec-kit PowerShell functions
# This file should be dot-sourced AFTER common.ps1 to override/extend functions
#
# These extensions provide:
# 1. Branch naming normalization (strips prefixes like specs/, feature/, etc.)
# 2. Stricter branch validation requiring prefixes
# 3. FEATURE_NAME property in path environment
#
# Usage:
#   . "$PSScriptRoot/common.ps1"
#   . "$PSScriptRoot/fw-extensions.ps1"  # Apply FW customizations

<#
.SYNOPSIS
    Normalizes a feature branch name by stripping common prefixes.

.DESCRIPTION
    Removes prefixes like 'refs/heads/', 'specs/', 'spec/', 'feature/', 'speckit/', etc.
    from a branch name to get the core feature identifier (e.g., "005-my-feature").

.PARAMETER Branch
    The branch name to normalize.

.EXAMPLE
    Normalize-FeatureBranchName -Branch "specs/005-my-feature"
    # Returns: "005-my-feature"
#>
function Normalize-FeatureBranchName {
    param([string]$Branch)

    if (-not $Branch) {
        return ''
    }

    $normalized = $Branch -replace '^refs/heads/', ''
    $normalized = $normalized.Trim()

    $prefixes = @('specs/', 'spec/', 'speckit/', 'feature/', 'features/', 'feat/')
    foreach ($prefix in $prefixes) {
        if ($normalized.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $normalized = $normalized.Substring($prefix.Length)
            break
        }
    }

    if ($normalized -like '*/*') {
        $normalized = $normalized.Split('/')[-1]
    }

    return $normalized
}

<#
.SYNOPSIS
    Validates that a branch follows FieldWorks feature branch naming conventions.

.DESCRIPTION
    Enforces that feature branches use one of these prefixes: spec/, specs/, feature/, speckit/
    and that the normalized name starts with a 3-digit identifier (e.g., 005-feature-name).

.PARAMETER Branch
    The branch name to validate.

.PARAMETER HasGit
    Whether the repository has git available.

.OUTPUTS
    Returns $true if valid, $false otherwise. Writes error messages to stdout.
#>
function Test-FeatureBranch {
    param(
        [string]$Branch,
        [bool]$HasGit = $true
    )

    $allowedPrefixesPattern = '^(spec|specs|feature|speckit)/'

    # For non-git repos, we cannot rename branches automatically; just warn and continue.
    if (-not $HasGit) {
        if ($Branch -notmatch $allowedPrefixesPattern) {
            Write-Warning "[specify] Branch '$Branch' does not include a supported prefix (spec/, specs/, feature/, speckit/). Defaulting to legacy behavior because repository is not managed by git."
            $normalizedBranch = $Branch
        }
    }

    if ($HasGit -and $Branch -notmatch $allowedPrefixesPattern) {
        Write-Output "ERROR: Feature branches must use one of these prefixes: spec/, specs/, feature/, speckit/. Current branch: $Branch"
        Write-Output "Rename via: git checkout -b specs/005-your-feature && git branch -D $Branch"
        return $false
    }

    if (-not $normalizedBranch) {
        $normalizedBranch = Normalize-FeatureBranchName $Branch
    }

    if ($normalizedBranch -notmatch '^[0-9]{3}-') {
        Write-Output "ERROR: Normalized feature branch '$normalizedBranch' must begin with a 3-digit identifier (e.g., 005-feature-name)."
        Write-Output "Rename the branch to specs/$normalizedBranch"
        return $false
    }
    return $true
}

<#
.SYNOPSIS
    Gets the feature directory path, using normalized branch names.

.DESCRIPTION
    Overrides the upstream Get-FeatureDir to use Normalize-FeatureBranchName
    so that branches like "specs/005-my-feature" map to "specs/005-my-feature" directory.

.PARAMETER RepoRoot
    The repository root path.

.PARAMETER Branch
    The current branch name.
#>
function Get-FeatureDir {
    param([string]$RepoRoot, [string]$Branch)
    $featureName = Normalize-FeatureBranchName -Branch $Branch
    if ([string]::IsNullOrWhiteSpace($featureName)) {
        $featureName = $Branch
    }
    $relativePath = Join-Path 'specs' $featureName
    Join-Path $RepoRoot $relativePath
}

<#
.SYNOPSIS
    Gets feature paths environment with FEATURE_NAME included.

.DESCRIPTION
    Extends the upstream Get-FeaturePathsEnv to include FEATURE_NAME property
    which contains the normalized branch name.
#>
function Get-FeaturePathsEnv {
    $repoRoot = Get-RepoRoot
    $currentBranch = Get-CurrentBranch
    $hasGit = Test-HasGit
    $normalizedBranch = Normalize-FeatureBranchName $currentBranch
    $featureDir = Get-FeatureDir -RepoRoot $repoRoot -Branch $currentBranch

    [PSCustomObject]@{
        REPO_ROOT      = $repoRoot
        CURRENT_BRANCH = $currentBranch
        FEATURE_NAME   = $normalizedBranch
        HAS_GIT        = $hasGit
        FEATURE_DIR    = $featureDir
        FEATURE_SPEC   = Join-Path $featureDir 'spec.md'
        IMPL_PLAN      = Join-Path $featureDir 'plan.md'
        FEATURE_TASKS  = Join-Path $featureDir 'tasks.md'
        FEATURE_LOG    = Join-Path $featureDir 'log.md'
        SOURCE_DIR     = Join-Path $featureDir 'source'
    }
}

# Export message to confirm extensions are loaded
Write-Verbose "[fw-extensions] FieldWorks spec-kit extensions loaded"
