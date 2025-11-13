<#
Stop and remove fw-agent-* containers and optionally clean up agents/* caches.
Worktrees themselves are preserved for reuse (we still warn about uncommitted changes
unless -ForceRemoveDirty is specified).

# Examples
#   .\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks"
#     (Stops all fw-agent-* containers but leaves worktrees/branches.)
#
#   .\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -RemoveWorktrees -RemoveNuGetCaches
#     (Also removes agents/agent-* worktrees, git branches, and per-agent NuGet caches.)
#
#   .\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -RemoveWorktrees -ForceRemoveDirty
#     (Removes worktrees without prompting even if uncommitted changes exist.)
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$RepoRoot,
  [int]$Count,
  [string]$WorktreesRoot,
  [switch]$RemoveWorktrees,
  [switch]$RemoveNuGetCaches,
  [switch]$ForceRemoveDirty
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path $RepoRoot).Path

. (Join-Path $PSScriptRoot 'git-utilities.ps1')

$agentModule = Join-Path $PSScriptRoot 'Agent\AgentInfrastructure.psm1'
Import-Module $agentModule -Force -DisableNameChecking

$vsCodeModule = Join-Path $PSScriptRoot 'Agent\VsCodeControl.psm1'
Import-Module $vsCodeModule -Force -DisableNameChecking

Invoke-DockerSafe @('info') -Quiet

if (-not $PSBoundParameters.ContainsKey('WorktreesRoot')) {
  if ($env:FW_WORKTREES_ROOT) {
    $WorktreesRoot = $env:FW_WORKTREES_ROOT
  } else {
    $WorktreesRoot = Join-Path $RepoRoot "worktrees"
  }
}

$worktreesRootExists = Test-Path $WorktreesRoot
if ($worktreesRootExists) {
  $WorktreesRoot = (Resolve-Path $WorktreesRoot).Path
} elseif ($RemoveWorktrees) {
  Write-Warning "Worktrees root '$WorktreesRoot' not found. Skipping worktree deletion."
}

function Get-ProcessesReferencingPath {
  param([string]$PathFragment)

  $resolved = Resolve-WorkspacePath -WorkspacePath $PathFragment
  if (-not $resolved) { return @() }
  $needle = $resolved.ToLowerInvariant()

  try {
    $processes = @(Get-CimInstance Win32_Process -ErrorAction Stop)
  } catch {
    return @()
  }

  $matches = @()
  foreach ($proc in $processes) {
    $cmd = $proc.CommandLine
    $exe = $proc.ExecutablePath
    $cmdMatch = $cmd -and $cmd.ToLowerInvariant().Contains($needle)
    $exeMatch = $exe -and $exe.ToLowerInvariant().Contains($needle)
    if ($cmdMatch -or $exeMatch) {
      $matches += [pscustomobject]@{
        ProcessId = $proc.ProcessId
        Name = $proc.Name
        CommandLine = $cmd
      }
    }
  }
  return @($matches)
}

function Report-LockingProcesses {
  param([string]$PathFragment)

  $matches = @(Get-ProcessesReferencingPath -PathFragment $PathFragment)
  if ($matches.Count -eq 0) {
    Write-Warning ("Could not identify a specific process locking {0}." -f $PathFragment)
    return
  }

  Write-Warning ("Processes referencing {0}:" -f $PathFragment)
  foreach ($proc in $matches) {
    $cmd = if ([string]::IsNullOrWhiteSpace($proc.CommandLine)) { '<no command line available>' } else { $proc.CommandLine }
    Write-Warning ("  PID {0} - {1}: {2}" -f $proc.ProcessId, $proc.Name, $cmd)
  }
}

function Test-WorktreeHasUncommittedChanges {
  param([string]$WorktreePath)

  if (-not (Test-Path -LiteralPath $WorktreePath)) { return $false }
  $gitSentinel = Join-Path $WorktreePath '.git'
  if (-not (Test-Path -LiteralPath $gitSentinel)) { return $false }

  Push-Location $WorktreePath
  try {
    try {
      $status = Invoke-GitSafe @('status','--porcelain') -CaptureOutput
    } catch {
      Write-Warning ("Failed to check git status for {0}: {1}" -f $WorktreePath, $_)
      return $false
    }
    $changedLines = @($status | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    return $changedLines.Count -gt 0
  } finally {
    Pop-Location
  }
}

function Get-AgentIndices {
  param(
    [string]$WorktreesRoot,
    [string]$RepoRoot
  )

  $set = New-Object System.Collections.Generic.HashSet[int]

  if (Test-Path $WorktreesRoot) {
    Get-ChildItem -Path $WorktreesRoot -Directory -Filter 'agent-*' -ErrorAction SilentlyContinue |
      ForEach-Object {
        if ($_.Name -match '^agent-(\d+)$') { [void]$set.Add([int]$matches[1]) }
      }
  }

  $cacheRoot = Join-Path $RepoRoot ".nuget"
  if (Test-Path $cacheRoot) {
    Get-ChildItem -Path $cacheRoot -Directory -Filter 'packages-agent-*' -ErrorAction SilentlyContinue |
      ForEach-Object {
        if ($_.Name -match 'packages-agent-(\d+)$') { [void]$set.Add([int]$matches[1]) }
      }
  }

  Push-Location $RepoRoot
  try {
    $branches = Invoke-GitSafe @('branch','--list','agents/agent-*') -CaptureOutput
    foreach ($branch in $branches) {
      if ($branch -match 'agents/agent-(\d+)') { [void]$set.Add([int]$matches[1]) }
    }
  } finally {
    Pop-Location
  }

  return ($set | Sort-Object)
}

$explicitCountProvided = $PSBoundParameters.ContainsKey('Count') -and $Count -gt 0
if ($explicitCountProvided) {
  $targetIndices = 1..$Count
} else {
  $targetIndices = Get-AgentIndices -WorktreesRoot $WorktreesRoot -RepoRoot $RepoRoot
}

# Remove ALL fw-agent-* containers (not just 1 to Count)
$allContainers = Invoke-DockerSafe @('ps','-a','--format','{{.Names}}') -CaptureOutput
$agentContainers = @($allContainers | Where-Object { $_ -match '^fw-agent-\d+$' })
if (@($agentContainers).Length -gt 0) {
  foreach ($name in $agentContainers) {
    Write-Host "Stopping/removing container $name..."
    Invoke-DockerSafe @('rm','-f',$name) -Quiet
  }
} else {
  Write-Host "No fw-agent-* containers found."
}

$removeCaches = $RemoveWorktrees -or $RemoveNuGetCaches

  if ($RemoveWorktrees -or $removeCaches) {
  if (@($targetIndices).Length -eq 0) {
    Write-Host "No agent worktrees or caches detected."
  } else {
    Push-Location $RepoRoot
    try {
      foreach ($i in $targetIndices) {
        $branch = "agents/agent-$i"
        $wtPath = Join-Path $WorktreesRoot "agent-$i"
        if ($RemoveWorktrees) {
          if (Test-Path -LiteralPath $wtPath) {
            $hasChanges = Test-WorktreeHasUncommittedChanges -WorktreePath $wtPath
            if ($hasChanges -and -not $ForceRemoveDirty) {
              Write-Warning "Worktree agent-$i has uncommitted changes; content will remain but tracking will be detached (use -ForceRemoveDirty to skip this warning)."
            }
            Write-Host "Detaching worktree agent-$i while leaving $wtPath on disk."
          } else {
            Write-Host "Worktree agent-$i not found on disk; only detaching branch metadata."
          }

          $wtRecord = Get-GitWorktreeForBranch -Branch $branch
          if ($wtRecord) {
            Write-Host "Removing registered worktree $($wtRecord.FullPath)"
            try {
              Remove-GitWorktreePath -WorktreePath $wtRecord.RawPath
            } catch {
              Write-Warning ("Failed to detach worktree {0}: {1}" -f $wtRecord.FullPath, $_)
              Report-LockingProcesses -PathFragment $wtRecord.FullPath
            }
          }

          if (Test-GitBranchExists -Branch $branch) {
            Write-Host "Deleting branch $branch"
            Invoke-GitSafe @('branch','-D',$branch) -Quiet
          }
        }

        if ($removeCaches) {
          $cachePath = Join-Path $RepoRoot (".nuget\\packages-agent-$i")
          if (Test-Path $cachePath) {
            Write-Host "Removing NuGet cache $cachePath"
            Remove-Item -Recurse -Force $cachePath
          }
        }
      }

      if ($RemoveWorktrees) {
        Prune-GitWorktreesNow
      }
    } finally {
      Pop-Location
    }
  }
}
Write-Host "Teardown complete."
