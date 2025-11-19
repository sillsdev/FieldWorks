#!/usr/bin/env pwsh
# Common PowerShell functions analogous to common.sh

function Get-RepoRoot {
	try {
		$result = git rev-parse --show-toplevel 2>$null
		if ($LASTEXITCODE -eq 0) {
			return $result
		}
	}
 catch {
		# Git command failed
	}

	# Fall back to script location for non-git repos
	return (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path
}

function Get-CurrentBranch {
	# First check if SPECIFY_FEATURE environment variable is set
	if ($env:SPECIFY_FEATURE) {
		return $env:SPECIFY_FEATURE
	}

	# Then check git if available
	try {
		$result = git rev-parse --abbrev-ref HEAD 2>$null
		if ($LASTEXITCODE -eq 0) {
			return $result
		}
	}
 catch {
		# Git command failed
	}

	# For non-git repos, try to find the latest feature directory
	$repoRoot = Get-RepoRoot
	$specsDir = Join-Path $repoRoot "specs"

	if (Test-Path $specsDir) {
		$latestFeature = ""
		$highest = 0

		Get-ChildItem -Path $specsDir -Directory | ForEach-Object {
			if ($_.Name -match '^(\d{3})-') {
				$num = [int]$matches[1]
				if ($num -gt $highest) {
					$highest = $num
					$latestFeature = $_.Name
				}
			}
		}

		if ($latestFeature) {
			return $latestFeature
		}
	}

	# Final fallback
	return "main"
}

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

function Test-HasGit {
	try {
		git rev-parse --show-toplevel 2>$null | Out-Null
		return ($LASTEXITCODE -eq 0)
	}
 catch {
		return $false
	}
}

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

function Get-FeatureDir {
	param([string]$RepoRoot, [string]$Branch)
	$featureName = Normalize-FeatureBranchName -Branch $Branch
	if ([string]::IsNullOrWhiteSpace($featureName)) {
		$featureName = $Branch
	}
	$relativePath = Join-Path 'specs' $featureName
	Join-Path $RepoRoot $relativePath
}

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
		TASKS          = Join-Path $featureDir 'tasks.md'
		RESEARCH       = Join-Path $featureDir 'research.md'
		DATA_MODEL     = Join-Path $featureDir 'data-model.md'
		QUICKSTART     = Join-Path $featureDir 'quickstart.md'
		CONTRACTS_DIR  = Join-Path $featureDir 'contracts'
	}
}

function Test-FileExists {
	param([string]$Path, [string]$Description)
	if (Test-Path -Path $Path -PathType Leaf) {
		Write-Output "  ✓ $Description"
		return $true
	}
 else {
		Write-Output "  ✗ $Description"
		return $false
	}
}

function Test-DirHasFiles {
	param([string]$Path, [string]$Description)
	if ((Test-Path -Path $Path -PathType Container) -and (Get-ChildItem -Path $Path -ErrorAction SilentlyContinue | Where-Object { -not $_.PSIsDirectory } | Select-Object -First 1)) {
		Write-Output "  ✓ $Description"
		return $true
	}
 else {
		Write-Output "  ✗ $Description"
		return $false
	}
}
