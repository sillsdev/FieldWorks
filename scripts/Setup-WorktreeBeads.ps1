<#
.SYNOPSIS
    Initialize a per-worktree isolated beads database.
.DESCRIPTION
    Creates a .beads directory inside the target worktree and runs bd init using
    BEADS_DIR so each worktree has its own isolated database.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$WorktreePath = "",

    [Parameter(Mandatory = $false)]
    [string]$BeadsDirName = ".beads",

    [Parameter(Mandatory = $false)]
    [string]$Prefix = "",

    [switch]$SetupExclude,

    # Print actions without running bd init.
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) {
        $path = (Get-Location).Path
    }
    $top = & git -C $path rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($top)) {
        throw "Not a git repo (or git missing). Path: $path"
    }
    return $top.Trim()
}

function Get-MainRepoRoot([string]$anyPathInRepo) {
    $top = Resolve-RepoRoot $anyPathInRepo
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

$worktreeRoot = Resolve-RepoRoot $WorktreePath
$beadsDir = Join-Path $worktreeRoot $BeadsDirName
$initRoot = $worktreeRoot
$gitPointer = Join-Path $worktreeRoot ".git"
if (Test-Path $gitPointer -PathType Leaf) {
    $initRoot = Get-MainRepoRoot $worktreeRoot
}

$bdCommand = Get-Command bd -ErrorAction SilentlyContinue
if ($null -eq $bdCommand) {
    Write-Warning "Beads CLI (bd) not found on PATH; skipping beads init."
    return
}

$beadsDb = Join-Path $beadsDir "beads.db"
$beadsJsonl = Join-Path $beadsDir "issues.jsonl"
$beadsConfig = Join-Path $beadsDir "config.yaml"

if (
    (Test-Path $beadsDb -PathType Leaf -ErrorAction SilentlyContinue) -or
    (Test-Path $beadsJsonl -PathType Leaf -ErrorAction SilentlyContinue)
) {
    return
}

if ($DryRun) {
    Write-Host "[DRYRUN] Would initialize beads at: $beadsDir"
    return
}

New-Item -ItemType Directory -Path $beadsDir -Force | Out-Null

$env:BEADS_DIR = $beadsDir

$initArgs = @("init")
if (-not [string]::IsNullOrWhiteSpace($Prefix)) {
    $initArgs += @("--prefix", $Prefix)
}
if ($SetupExclude) {
    $initArgs += "--setup-exclude"
}

Write-Host "Initializing beads in $beadsDir"
Push-Location $initRoot
try {
    & bd --db $beadsDb @initArgs
}
finally {
    Pop-Location
}
if ($LASTEXITCODE -ne 0) {
    throw "bd init failed."
}

if (-not (Test-Path $beadsDb -PathType Leaf -ErrorAction SilentlyContinue)) {
    throw "Beads database was not created at: $beadsDb"
}
