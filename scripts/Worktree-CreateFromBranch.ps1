<#
.SYNOPSIS
    Create (or open) a git worktree for a branch and open it in a new VS Code window.
.DESCRIPTION
    - Worktrees are placed under ../<main-folder-of-repo>.worktrees/<branch_name>
    - If invoked from within an existing worktree, path names are based on the main repo root.
    - If the branch already has a worktree (or the folder already exists as a worktree), it opens that.
    - If a VS Code window already appears to be open for that worktree, it attempts to bring it to the foreground.

    Colorization and workspace generation is delegated to scripts/Setup-WorktreeColor.ps1.
#>

[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$BranchName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$colorizeScript = Join-Path $scriptRepoRoot "scripts\Setup-WorktreeColor.ps1"

function ConvertTo-CanonicalBranchName([string]$name) {
	if ([string]::IsNullOrWhiteSpace($name)) {
		throw "BranchName is empty."
	}
	$name = $name.Trim()
	if ($name.StartsWith("refs/heads/")) {
		return $name.Substring("refs/heads/".Length)
	}
	if ($name.StartsWith("origin/")) {
		return $name.Substring("origin/".Length)
	}
	return $name
}

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
		# Fallback: assume the current toplevel is the main root.
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
		# If Resolve-Path fails (e.g. unusual git state), assume it's already usable.
	}

	# Walk up until we find the .git directory.
	$probe = $commonPath
	while ($true) {
		if ([string]::Equals((Split-Path $probe -Leaf), ".git", [System.StringComparison]::OrdinalIgnoreCase)) {
			break
		}
		$parent = Split-Path $probe -Parent
		if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $probe) {
			# Give up and use current toplevel.
			return $top
		}
		$probe = $parent
	}

	return (Split-Path $probe -Parent)
}

function Get-IsWorktree([string]$rootPath) {
	$gitPath = Join-Path $rootPath ".git"
	return (Test-Path $gitPath -PathType Leaf)
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

function Convert-BranchToRelativePath([string]$branch) {
	# Allow feature/foo to become ...\feature\foo
	$segments = $branch -split '[\\/]'
	$segments = $segments | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
	if ($segments.Count -eq 0) {
		throw "Invalid branch name: '$branch'"
	}
	return $segments
}

function Get-WorktreesRoot([string]$mainRepoRoot) {
	$repoName = Split-Path $mainRepoRoot -Leaf
	$repoParent = Split-Path $mainRepoRoot -Parent
	return (Join-Path $repoParent ("{0}.worktrees" -f $repoName))
}

function Get-GitWorktrees([string]$mainRepoRoot) {
	$lines = & git -C $mainRepoRoot worktree list --porcelain 2>$null
	if ($LASTEXITCODE -ne 0) {
		throw "Failed to list git worktrees."
	}
	if ($null -eq $lines -or $lines.Count -eq 0) {
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

function ConvertTo-CanonicalWorktreeBranch([string]$branchRef) {
	if ([string]::IsNullOrWhiteSpace($branchRef)) {
		return ""
	}
	if ($branchRef.StartsWith("refs/heads/")) {
		return $branchRef.Substring("refs/heads/".Length)
	}
	return $branchRef
}

function Test-BranchExists([string]$mainRepoRoot, [string]$ref) {
	& git -C $mainRepoRoot show-ref --verify --quiet $ref 2>$null
	return ($LASTEXITCODE -eq 0)
}

function Set-VsCodeWindowForegroundIfOpen([string]$worktreePath, [string]$branchName) {
	# Best-effort: match window title by folder name or branch name.
	$folderName = Split-Path $worktreePath -Leaf
	$needles = @($folderName, $branchName) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

	$procs = @(Get-Process -Name "Code" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 })
	if ($procs.Count -eq 0) {
		return $false
	}

	$windowMatches = @()
	foreach ($p in $procs) {
		$title = $p.MainWindowTitle
		if ([string]::IsNullOrWhiteSpace($title)) {
			continue
		}
		foreach ($n in $needles) {
			if ($title -like "*$n*") {
				$windowMatches += $p
				break
			}
		}
	}

	if ($windowMatches.Count -eq 0) {
		return $false
	}

	try {
		Add-Type -Namespace FieldWorks -Name Win32 -MemberDefinition @"
using System;
using System.Runtime.InteropServices;
public static class Win32 {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}
"@ -ErrorAction SilentlyContinue

		$target = $windowMatches[0]
		# 9 = SW_RESTORE
		[void][FieldWorks.Win32]::ShowWindowAsync($target.MainWindowHandle, 9)
		[void][FieldWorks.Win32]::SetForegroundWindow($target.MainWindowHandle)
		Write-Host "VS Code already open for this worktree; focusing existing window."
		return $true
	}
	catch {
		Write-Warning "VS Code already appears to be open, but focusing failed."
		throw "VS Code already exists for that worktree."
	}
}

$branch = ConvertTo-CanonicalBranchName $BranchName
$mainRepoRoot = Get-MainRepoRoot $scriptRepoRoot

$worktreesRoot = Get-WorktreesRoot $mainRepoRoot
$desiredPath = $worktreesRoot
foreach ($seg in (Convert-BranchToRelativePath $branch)) {
	$desiredPath = Join-Path $desiredPath $seg
}

$worktrees = Get-GitWorktrees $mainRepoRoot

$normalizedDesired = ConvertTo-CanonicalPath $desiredPath

$existing = $null
foreach ($wt in $worktrees) {
	if (-not [string]::IsNullOrWhiteSpace($wt.Branch)) {
		$wtBranch = ConvertTo-CanonicalWorktreeBranch $wt.Branch
		if ($wtBranch -eq $branch) {
			$existing = $wt
			break
		}
	}
}

if ($null -eq $existing) {
	foreach ($wt in $worktrees) {
		if ((ConvertTo-CanonicalPath $wt.Path) -eq $normalizedDesired) {
			$existing = $wt
			break
		}
	}
}

$targetWorktreePath = $null
if ($null -ne $existing) {
	$targetWorktreePath = $existing.Path
	Write-Host "Found existing worktree for branch '$branch': $targetWorktreePath"
}
elseif (Test-Path $desiredPath -PathType Container -ErrorAction SilentlyContinue) {
	if (Get-IsWorktree $desiredPath) {
		$targetWorktreePath = $desiredPath
		Write-Host "Found existing worktree folder for branch '$branch': $targetWorktreePath"
	}
	else {
		throw "Folder exists but is not a git worktree: $desiredPath"
	}
}
else {
	# Create worktree
	New-Item -ItemType Directory -Path $worktreesRoot -Force | Out-Null
	New-Item -ItemType Directory -Path (Split-Path $desiredPath -Parent) -Force | Out-Null

	$localRef = "refs/heads/$branch"
	$remoteRef = "refs/remotes/origin/$branch"

	$hasLocal = Test-BranchExists -mainRepoRoot $mainRepoRoot -ref $localRef
	$hasRemote = $false
	if (-not $hasLocal) {
		$hasRemote = Test-BranchExists -mainRepoRoot $mainRepoRoot -ref $remoteRef
	}

	if ($hasLocal) {
		Write-Host "Creating worktree at $desiredPath for existing local branch '$branch'..."
		& git -C $mainRepoRoot worktree add $desiredPath $branch
	}
	elseif ($hasRemote) {
		Write-Host "Creating worktree at $desiredPath for remote branch 'origin/$branch'..."
		& git -C $mainRepoRoot worktree add -b $branch $desiredPath "origin/$branch"
	}
	else {
		Write-Host "Branch '$branch' not found; creating new branch and worktree at $desiredPath..."
		& git -C $mainRepoRoot worktree add -b $branch $desiredPath
	}

	if ($LASTEXITCODE -ne 0) {
		throw "git worktree add failed."
	}

	$targetWorktreePath = $desiredPath
}

if (-not (Test-Path $targetWorktreePath -PathType Container)) {
	throw "Target worktree path does not exist: $targetWorktreePath"
}

# If VS Code is already open for this worktree, try to focus it.
if (Set-VsCodeWindowForegroundIfOpen -worktreePath $targetWorktreePath -branchName $branch) {
	return
}

# Open a new VS Code window for the worktree workspace file (colorized).
if (-not (Test-Path $colorizeScript -PathType Leaf)) {
	throw "Missing colorize script: $colorizeScript"
}

& $colorizeScript -Action Launch -WorktreePath $targetWorktreePath
