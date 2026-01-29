<#
.SYNOPSIS
    Git helper for cross-worktree and cross-branch operations.

.DESCRIPTION
    Encapsulates common git operations that would otherwise require pipes or
    complex commands that don't auto-approve. Designed for use by Copilot agents.

.PARAMETER Action
    The git operation to perform:
    - show: Show file contents from a specific ref
    - diff: Compare files between refs or worktrees
    - log: Show commit history
    - blame: Show line-by-line authorship
    - search: Search for text in files (git grep)
    - branches: List branches matching pattern
    - files: List files in a tree

.PARAMETER Ref
    Git ref (branch, tag, commit) to operate on. Default: HEAD

.PARAMETER Path
    File path within the repository.

.PARAMETER Pattern
    Search pattern for 'search' action or branch pattern for 'branches' action.

.PARAMETER RepoPath
    Path to git repository. Default: current directory or main FieldWorks repo.

.PARAMETER HeadLines
    Number of lines to show from the beginning. Default: 0 (all)

.PARAMETER TailLines
    Number of lines to show from the end. Default: 0 (all)

.PARAMETER Context
    Lines of context for search results. Default: 3

.EXAMPLE
    .\Git-Search.ps1 -Action show -Ref "release/9.3" -Path "Output/Common/ViewsTlb.h" -HeadLines 5

.EXAMPLE
    .\Git-Search.ps1 -Action search -Pattern "IVwGraphics" -Ref "HEAD" -Path "Src/"

.EXAMPLE
    .\Git-Search.ps1 -Action diff -Ref "release/9.3..HEAD" -Path "Src/Common"

.EXAMPLE
    .\Git-Search.ps1 -Action branches -Pattern "feature/*"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('show', 'diff', 'log', 'blame', 'search', 'branches', 'files')]
    [string]$Action,

    [string]$Ref = 'HEAD',

    [string]$Path,

    [string]$Pattern,

    [string]$RepoPath,

    [int]$HeadLines = 0,

    [int]$TailLines = 0,

    [int]$Context = 3

    ,

    [ValidateSet('oneline', 'fuller')]
    [string]$LogStyle = 'oneline'

    ,

    [int]$MaxCount = 20
)

$ErrorActionPreference = 'Stop'

# Determine repository path
if (-not $RepoPath) {
    # Try to find FieldWorks main repo
    $candidates = @(
        'C:\Users\johnm\Documents\repos\FieldWorks',
        'C:\Users\johnm\Documents\repos\fw-worktrees\main',
        (Get-Location).Path
    )
    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate '.git')) {
            $RepoPath = $candidate
            break
        }
    }
}

if (-not $RepoPath -or -not (Test-Path $RepoPath)) {
    throw "Repository path not found. Specify -RepoPath explicitly."
}

function Invoke-GitCommand {
    param([string[]]$Arguments)

    $result = & git -C $RepoPath @Arguments 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        $errorMsg = $result | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { $_.ToString() }
        if ($errorMsg) {
            Write-Error "Git command failed: $errorMsg"
        }
        return $null
    }

    # Convert to string array
    $lines = @($result | ForEach-Object { $_.ToString() })
    return $lines
}

function Format-Output {
    param([string[]]$Lines)

    if (-not $Lines -or $Lines.Count -eq 0) {
        return
    }

    $total = $Lines.Count

    if ($HeadLines -gt 0 -and $TailLines -gt 0) {
        # Show head and tail
        if ($total -le ($HeadLines + $TailLines)) {
            $Lines | ForEach-Object { Write-Output $_ }
        } else {
            $Lines | Select-Object -First $HeadLines | ForEach-Object { Write-Output $_ }
            Write-Output "... ($($total - $HeadLines - $TailLines) lines omitted) ..."
            $Lines | Select-Object -Last $TailLines | ForEach-Object { Write-Output $_ }
        }
    }
    elseif ($HeadLines -gt 0) {
        if ($total -gt $HeadLines) {
            $Lines | Select-Object -First $HeadLines | ForEach-Object { Write-Output $_ }
            Write-Output "... ($($total - $HeadLines) more lines) ..."
        } else {
            $Lines | ForEach-Object { Write-Output $_ }
        }
    }
    elseif ($TailLines -gt 0) {
        if ($total -gt $TailLines) {
            Write-Output "... ($($total - $TailLines) lines omitted) ..."
            $Lines | Select-Object -Last $TailLines | ForEach-Object { Write-Output $_ }
        } else {
            $Lines | ForEach-Object { Write-Output $_ }
        }
    }
    else {
        $Lines | ForEach-Object { Write-Output $_ }
    }
}

switch ($Action) {
    'show' {
        if (-not $Path) {
            throw "Path is required for 'show' action"
        }
        $refPath = "${Ref}:${Path}"
        $lines = Invoke-GitCommand @('show', $refPath)
        Format-Output $lines
    }

    'diff' {
        $args = @('diff', '--stat')
        if ($Ref) { $args += $Ref }
        if ($Path) { $args += '--'; $args += $Path }
        $lines = Invoke-GitCommand $args
        Format-Output $lines
    }

    'log' {
        if ($LogStyle -eq 'fuller') {
            $args = @('log', '--pretty=fuller')
        } else {
            $args = @('log', '--oneline')
        }

        if ($MaxCount -gt 0) {
            $args += @('-n', $MaxCount)
        }
        if ($Ref -and $Ref -ne 'HEAD') { $args += $Ref }
        if ($Path) { $args += '--'; $args += $Path }
        $lines = Invoke-GitCommand $args
        Format-Output $lines
    }

    'blame' {
        if (-not $Path) {
            throw "Path is required for 'blame' action"
        }
        $args = @('blame', '--line-porcelain')
        if ($Ref -and $Ref -ne 'HEAD') { $args += $Ref }
        $args += '--'
        $args += $Path
        $lines = Invoke-GitCommand $args
        Format-Output $lines
    }

    'search' {
        if (-not $Pattern) {
            throw "Pattern is required for 'search' action"
        }
        $args = @('grep', '-n', "-C$Context", $Pattern)
        if ($Ref -and $Ref -ne 'HEAD') { $args += $Ref }
        if ($Path) { $args += '--'; $args += $Path }
        $lines = Invoke-GitCommand $args
        Format-Output $lines
    }

    'branches' {
        $args = @('branch', '-a')
        if ($Pattern) {
            $args += '--list'
            $args += $Pattern
        }
        $lines = Invoke-GitCommand $args
        Format-Output $lines
    }

    'files' {
        $args = @('ls-tree', '--name-only', '-r')
        if ($Ref) { $args += $Ref } else { $args += 'HEAD' }
        if ($Path) { $args += '--'; $args += $Path }
        $lines = Invoke-GitCommand $args
        Format-Output $lines
    }
}
