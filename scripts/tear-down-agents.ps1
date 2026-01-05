<#
Stop and remove fw-agent-* containers and optionally clean up worktrees/caches.

SAFETY: This script will ERROR and refuse to remove worktrees that have uncommitted
changes, preventing accidental data loss. Use -ForceRemoveDirty only if you're certain
you want to discard uncommitted work.

NuGet Cache Strategy (Hybrid):
- Containers use a Docker named volume 'fw-nuget-cache' for SHARED caches:
  - C:\NuGetCache\packages\    - global-packages (shared across all agents)
  - C:\NuGetCache\http-cache\  - HTTP cache (shared across all agents)
- TEMP/TMP is container-local (C:\Temp) for isolation during extraction
- Named volumes don't have the MoveFile() bug that affects bind mounts
- Use -RemoveNuGetVolume to remove the shared NuGet cache volume (forces full re-download)

# Examples
#   .\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks"
#     (Stops all fw-agent-* containers but leaves worktrees/branches.)
#
#   .\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -RemoveWorktrees
#     (Also removes agents/agent-* worktrees and git branches.)
#     (Will ERROR if any worktree has uncommitted changes.)
#
#   .\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -RemoveNuGetVolume
#     (Removes the fw-nuget-cache Docker volume - forces full package re-download.)
#
#   .\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -RemoveWorktrees -ForceRemoveDirty
#     (⚠️ DANGEROUS: Removes worktrees even with uncommitted changes - DATA LOSS!)
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [int]$Count,
    [string]$WorktreesRoot,
    [switch]$RemoveWorktrees,
    [switch]$RemoveNuGetVolume,
    [switch]$ForceRemoveDirty
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

#region Module Imports

. (Join-Path $PSScriptRoot 'git-utilities.ps1')

$agentModule = Join-Path $PSScriptRoot 'Agent\AgentInfrastructure.psm1'
Import-Module $agentModule -Force -DisableNameChecking

$configModule = Join-Path $PSScriptRoot 'Agent\AgentConfiguration.psm1'
Import-Module $configModule -Force -DisableNameChecking

$vsCodeModule = Join-Path $PSScriptRoot 'Agent\VsCodeControl.psm1'
Import-Module $vsCodeModule -Force -DisableNameChecking

#endregion

#region Initialization

$RepoRoot = (Resolve-Path $RepoRoot).Path

# Verify Docker is available
Invoke-DockerSafe @('info') -Quiet

# Resolve worktrees root
$WorktreesRoot = Resolve-WorktreesRoot -WorktreesRoot $WorktreesRoot -RepoRoot $RepoRoot
$worktreesRootExists = Test-Path $WorktreesRoot

if (-not $worktreesRootExists -and $RemoveWorktrees) {
    Write-Warning "Worktrees root '$WorktreesRoot' not found. Skipping worktree deletion."
}

#endregion

#region Helper Functions

function Test-WorktreeHasUncommittedChanges {
    param([Parameter(Mandatory)][string]$WorktreePath)

    if (-not (Test-Path -LiteralPath $WorktreePath)) { return $false }

    $gitSentinel = Join-Path $WorktreePath '.git'
    if (-not (Test-Path -LiteralPath $gitSentinel)) { return $false }

    Push-Location $WorktreePath
    try {
        try {
            $status = Invoke-GitSafe @('status', '--porcelain') -CaptureOutput
        } catch {
            Write-Warning "Failed to check git status for ${WorktreePath}: $_"
            return $false
        }
        $changedLines = @($status | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        return $changedLines.Count -gt 0
    } finally {
        Pop-Location
    }
}

#endregion

#region Main Logic

# Determine which agent indices to process
$explicitCountProvided = $PSBoundParameters.ContainsKey('Count') -and $Count -gt 0
if ($explicitCountProvided) {
    $targetIndices = 1..$Count
} else {
    $targetIndices = Get-AgentIndices -WorktreesRoot $WorktreesRoot -RepoRoot $RepoRoot
}

# Remove ALL fw-agent-* containers (not just 1 to Count)
Remove-AgentContainers

# Remove worktrees if requested
if ($RemoveWorktrees) {
    if (@($targetIndices).Length -eq 0) {
        Write-Host "No agent worktrees detected."
    } else {
        Push-Location $RepoRoot
        try {
            foreach ($i in $targetIndices) {
                $branch = Get-BranchName -Index $i
                $wtPath = Get-WorktreePath -WorktreesRoot $WorktreesRoot -Index $i

                if (Test-Path -LiteralPath $wtPath) {
                    $hasChanges = Test-WorktreeHasUncommittedChanges -WorktreePath $wtPath
                    if ($hasChanges -and -not $ForceRemoveDirty) {
                        throw @"
Worktree agent-$i has uncommitted changes at: $wtPath

To protect your work, tear-down will NOT remove this worktree.

Options:
1. Commit or stash your changes in the worktree, then re-run tear-down
2. Push your changes to a remote branch for safekeeping
3. Use -ForceRemoveDirty to override (WARNING: This will DELETE uncommitted work)

To check what's uncommitted:
  cd '$wtPath'
  git status
"@
                    }
                    Write-Host "Detaching worktree agent-$i (no uncommitted changes detected)."
                } else {
                    Write-Host "Worktree agent-$i not found on disk; only detaching branch metadata."
                }

                # Remove worktree registration
                $wtRecord = Get-GitWorktreeForBranch -Branch $branch
                if ($wtRecord) {
                    Write-Host "Removing registered worktree $($wtRecord.FullPath)"
                    try {
                        Remove-GitWorktreePath -WorktreePath $wtRecord.RawPath
                    } catch {
                        Write-Warning "Failed to detach worktree $($wtRecord.FullPath): $_"
                        Write-LockingProcesses -PathFragment $wtRecord.FullPath
                    }
                }

                # Remove directory
                if (Test-Path -LiteralPath $wtPath) {
                    Write-Host "Removing worktree directory $wtPath"
                    Remove-Item -LiteralPath $wtPath -Recurse -Force -ErrorAction SilentlyContinue
                }

                # Remove branch
                if (Test-GitBranchExists -Branch $branch) {
                    Write-Host "Deleting branch $branch"
                    Invoke-GitSafe @('branch', '-D', $branch) -Quiet
                }
            }

            Prune-GitWorktreesNow
        } finally {
            Pop-Location
        }
    }
}

# Remove NuGet cache volume if requested
if ($RemoveNuGetVolume) {
    Remove-NuGetVolume

    # Also remove any legacy .cache folder if it exists
    $legacyCachePath = Join-Path $WorktreesRoot ".cache"
    if (Test-Path $legacyCachePath) {
        Write-Host "Removing legacy .cache folder: $legacyCachePath"
        Remove-Item -Recurse -Force $legacyCachePath -ErrorAction SilentlyContinue
    }
}

Write-Host "Teardown complete."

#endregion
