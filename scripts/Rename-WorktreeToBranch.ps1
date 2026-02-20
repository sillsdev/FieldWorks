<#
.SYNOPSIS
    Rename (move) the current git worktree folder to match the current branch name.
.DESCRIPTION
    Uses 'git worktree move' so git keeps tracking the worktree.

    The target path is:
        ../<main-folder-of-repo>.worktrees/<branch_name>

    If run from inside a worktree, the main repo root is used for base paths.
#>

[CmdletBinding()]
param(
	# Print the actions that would be taken, but do not move anything.
	[switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$colorizeScript = Join-Path $scriptRepoRoot "scripts\Setup-WorktreeColor.ps1"

function Get-RepoRoot([string]$anyPathInRepo) {
	$top = & git -C $anyPathInRepo rev-parse --show-toplevel 2>$null
	if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($top)) {
		throw "Not a git repo (or git missing). Path: $anyPathInRepo"
	}
	return $top.Trim()
}

function Get-MainRepoRoot([string]$anyPathInRepo) {
	$top = Get-RepoRoot $anyPathInRepo
	$common = & git -C $anyPathInRepo rev-parse --git-common-dir 2>$null
	if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($common)) {
		return $top
	}

	$common = $common.Trim()
	$commonPath = $common
	if (-not [System.IO.Path]::IsPathRooted($commonPath)) {
		$commonPath = Join-Path $top $commonPath
	}
	try {
		$commonPath = (Resolve-Path -LiteralPath $commonPath).Path
	}
	catch {
	}

	$probe = $commonPath
	while ($true) {
		if ([string]::Equals((Split-Path $probe -Leaf), ".git", [System.StringComparison]::OrdinalIgnoreCase)) {
			break
		}
		$parent = Split-Path $probe -Parent
		if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $probe) {
			return $top
		}
		$probe = $parent
	}

	return (Split-Path $probe -Parent)
}

function Get-WorktreesRoot([string]$mainRepoRoot) {
	$repoName = Split-Path $mainRepoRoot -Leaf
	$repoParent = Split-Path $mainRepoRoot -Parent
	return (Join-Path $repoParent ("{0}.worktrees" -f $repoName))
}

function Convert-BranchToRelativePath([string]$branch) {
	$segments = @($branch -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
	if ($segments.Length -eq 0) {
		throw "Invalid branch name: '$branch'"
	}
	return $segments
}

function ConvertTo-CanonicalPath([string]$path) {
	if ([string]::IsNullOrWhiteSpace($path)) {
		return ""
	}
	try {
		return ([System.IO.Path]::GetFullPath($path)).ToLowerInvariant()
	}
	catch {
		return $path.ToLowerInvariant()
	}
}

function Get-GitWorktrees([string]$mainRepoRoot) {
	$lines = @(& git -C $mainRepoRoot worktree list --porcelain 2>$null)
	if ($LASTEXITCODE -ne 0) {
		throw "Failed to list git worktrees."
	}
	if ($null -eq $lines -or $lines.Length -eq 0) {
		return @()
	}

	$worktrees = New-Object System.Collections.Generic.List[object]
	$current = $null

	foreach ($line in $lines) {
		if ([string]::IsNullOrWhiteSpace($line)) {
			if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path)) {
				$worktrees.Add($current)
			}
			$current = $null
			continue
		}

		if ($line.StartsWith("worktree ")) {
			if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path)) {
				$worktrees.Add($current)
			}
			$current = [pscustomobject]@{ Path = $line.Substring("worktree ".Length); Branch = "" }
			continue
		}

		if ($null -eq $current) {
			continue
		}

		if ($line.StartsWith("branch ")) {
			$current.Branch = $line.Substring("branch ".Length)
			continue
		}

		if ($line -eq "detached") {
			$current.Branch = "(detached)"
			continue
		}
	}

	if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path)) {
		$worktrees.Add($current)
	}

	return $worktrees.ToArray()
}

$currentWorktreeRoot = Get-RepoRoot $scriptRepoRoot
$mainRepoRoot = Get-MainRepoRoot $currentWorktreeRoot

$branch = (& git -C $currentWorktreeRoot rev-parse --abbrev-ref HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($branch)) {
	throw "Failed to get current branch name."
}
if ($branch -eq "HEAD") {
	throw "Detached HEAD; cannot rename worktree to branch."
}

$worktreesRoot = Get-WorktreesRoot $mainRepoRoot
$desiredPath = $worktreesRoot
foreach ($seg in (Convert-BranchToRelativePath $branch)) {
	$desiredPath = Join-Path $desiredPath $seg
}

if ((ConvertTo-CanonicalPath $currentWorktreeRoot) -eq (ConvertTo-CanonicalPath $desiredPath)) {
	Write-Host "Worktree already matches branch name: $desiredPath"
	return
}

$worktrees = Get-GitWorktrees $mainRepoRoot
$normalizedDesired = ConvertTo-CanonicalPath $desiredPath
foreach ($wt in $worktrees) {
	if ((ConvertTo-CanonicalPath $wt.Path) -eq $normalizedDesired) {
		throw "Another worktree already uses the target path: $desiredPath"
	}
}

New-Item -ItemType Directory -Path (Split-Path $desiredPath -Parent) -Force | Out-Null

Write-Host "Moving worktree..."
Write-Host "  From: $currentWorktreeRoot"
Write-Host "  To:   $desiredPath"

if ($DryRun) {
	Write-Host "[DRYRUN] Would run: git worktree move <current> <desired>"
	return
}

& git -C $mainRepoRoot worktree move $currentWorktreeRoot $desiredPath
if ($LASTEXITCODE -ne 0) {
	throw "git worktree move failed."
}

# Ensure the worktree-local workspace file exists at the new location.
if (Test-Path $colorizeScript -PathType Leaf) {
	& $colorizeScript -Action Apply -WorktreePath $desiredPath -VSCodeWorkspaceFile ""
}

Write-Host "Done. Worktree moved to: $desiredPath"
