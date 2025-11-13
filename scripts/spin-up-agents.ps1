<#
Create N git worktrees and one Windows container per worktree for isolated .NET Framework 4.8/C++ builds.

Prereqs:
- Docker Desktop in "Windows containers" mode
- One primary clone on the host (e.g., C:\dev\FieldWorks)
- PowerShell 5+ (Windows)

Typical use:
  $env:FW_WORKTREES_ROOT = "C:\dev\FieldWorks\worktrees"

  # Create agents based on current branch (default FieldWorks.proj build):
  .\scripts\spin-up-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -Count 3

  # Or specify a different base branch:
  .\scripts\spin-up-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -Count 3 -BaseRef origin/release/9.3

  # To prevent VS Code from opening automatically:
  .\scripts\spin-up-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -Count 3 -SkipOpenVSCode

  # To forcibly clean orphaned worktree directories without prompting:
  .\scripts\spin-up-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -Count 3 -ForceCleanup
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$RepoRoot,
  [Parameter(Mandatory=$true)][int]$Count,
  [string]$BaseRef,
  [string]$ImageTag = "fw-build:ltsc2022",
  [string]$WorktreesRoot,
  [string]$SolutionRelPath = "FieldWorks.sln",
  [switch]$RebuildImage,
  [switch]$SkipVsCodeSetup,
  [switch]$ForceVsCodeSetup,
  [switch]$SkipOpenVSCode,
  [switch]$ForceCleanup,
  [string]$ContainerMemory = "4g"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$agentModule = Join-Path $PSScriptRoot 'Agent\AgentInfrastructure.psm1'
Import-Module $agentModule -Force -DisableNameChecking

$vsCodeModule = Join-Path $PSScriptRoot 'Agent\VsCodeControl.psm1'
Import-Module $vsCodeModule -Force -DisableNameChecking

$script:GitFetched = $false

# Default BaseRef to current branch if not specified
if (-not $BaseRef) {
  Push-Location $RepoRoot
  try {
    $currentBranch = git branch --show-current
    if ($currentBranch) {
      $BaseRef = $currentBranch
      Write-Host "Using current branch as base: $BaseRef"
    } else {
      # Detached HEAD state - use HEAD
      $BaseRef = "HEAD"
      Write-Host "Detached HEAD state - using HEAD as base"
    }
  } finally {
    Pop-Location
  }
}

Assert-Tool git
Assert-Tool docker "info"

. (Join-Path $PSScriptRoot 'git-utilities.ps1')

# Color palette for agent workspaces (distinct, high-contrast colors)
function Get-AgentColors {
  param([int]$Index)
  $colors = @(
    @{ Title="#1e3a8a"; Status="#1e40af"; Activity="#1e3a8a"; Name="Blue" },       # Deep blue
    @{ Title="#15803d"; Status="#16a34a"; Activity="#15803d"; Name="Green" },     # Forest green
    @{ Title="#9333ea"; Status="#a855f7"; Activity="#9333ea"; Name="Purple" },    # Purple
    @{ Title="#c2410c"; Status="#ea580c"; Activity="#c2410c"; Name="Orange" },    # Orange
    @{ Title="#be123c"; Status="#e11d48"; Activity="#be123c"; Name="Rose" }      # Rose/Red
  )
  $idx = ($Index - 1) % $colors.Count
  return $colors[$idx]
}

# Verify Windows containers mode
$info = docker info --format '{{json .}}' | ConvertFrom-Json
if ($null -eq $info.OSType -or $info.OSType -ne "windows") {
  Write-Error @"
Docker is in LINUX containers mode (OSType=$($info.OSType)).

To fix:
1. Right-click Docker Desktop in system tray
2. Select "Switch to Windows containers..."
3. Wait for Docker to restart
4. Run this script again

The multi-agent workflow requires Windows containers because FieldWorks needs:
- .NET Framework 4.8 (Windows-only)
- Visual Studio Build Tools
- COM/registry isolation
"@
  throw "Docker must be in Windows containers mode"
}

# Normalize paths
$RepoRoot = (Resolve-Path $RepoRoot).Path
Assert-VolumeSupportsBindMount -Path $RepoRoot
if (-not $PSBoundParameters.ContainsKey('WorktreesRoot')) {
  if ($env:FW_WORKTREES_ROOT) { $WorktreesRoot = $env:FW_WORKTREES_ROOT } else { $WorktreesRoot = Join-Path $RepoRoot "worktrees" }
}
# Create worktrees root directory
New-Item -ItemType Directory -Force -Path $WorktreesRoot | Out-Null
$WorktreesRoot = (Resolve-Path $WorktreesRoot).Path
Assert-VolumeSupportsBindMount -Path $WorktreesRoot

$repoGitDir = Get-GitDirectory -Path $RepoRoot
Ensure-GitExcludePatterns -GitDir $repoGitDir -Patterns @('worktrees/')

# Clean up dangling worktrees and Docker resources before starting
Write-Host "Checking for dangling worktrees..."
Push-Location $RepoRoot
try {
  Prune-GitWorktreesNow
} finally {
  Pop-Location
}

# Build image if missing or forced
function Ensure-Image {
  param([string]$Tag)
  $images = Invoke-DockerSafe @('images','--format','{{.Repository}}:{{.Tag}}') -CaptureOutput
  $exists = $images | Where-Object { $_ -eq $Tag }
  if ($RebuildImage -or -not $exists) {
    Write-Host "Building image $Tag..."

    # Use docker-build-ipv4.ps1 wrapper to work around Windows 11 IPv6 issues
    $buildWrapper = Join-Path $PSScriptRoot "docker-build-ipv4.ps1"
    $dockerfilePath = Join-Path $RepoRoot "Dockerfile.windows"

    # Retry logic for transient network failures
    $maxRetries = 3
    $retryDelay = 5
    $attempt = 1

    while ($attempt -le $maxRetries) {
      try {
        # Call wrapper with explicit registry hosts parameter, then build args
        & $buildWrapper -RegistryHosts @("mcr.microsoft.com") -BuildArgs @("-t", $Tag, "-f", $dockerfilePath, $RepoRoot)
        if ($LASTEXITCODE -eq 0) {
          Write-Host "Image built successfully."
          return
        }
      } catch {
        # Catch any exceptions during build
      }

      if ($attempt -lt $maxRetries) {
        Write-Warning "Build attempt $attempt failed. Retrying in $retryDelay seconds..."
        Start-Sleep -Seconds $retryDelay
        $attempt++
        $retryDelay *= 2  # Exponential backoff
      } else {
        Write-Error "Failed to build image after $maxRetries attempts. Check your internet connection and Docker network settings."
        throw "Docker build failed"
      }
    }
  } else {
    Write-Host "Image $Tag already exists."
  }
}

function Reset-AgentWorktree {
  param(
    [Parameter(Mandatory=$true)][string]$WorktreePath,
    [Parameter(Mandatory=$true)][string]$ResetRef
  )

  if (-not (Test-Path -LiteralPath $WorktreePath)) { return }

  Push-Location $WorktreePath
  try {
    Invoke-GitSafe @('reset','--hard',$ResetRef) -Quiet
    Invoke-GitSafe @('clean','-xfd') -Quiet
  } finally {
    Pop-Location
  }
}

function Clear-AgentDirectory {
  param(
    [Parameter(Mandatory=$true)][string]$AgentPath
  )

  if (-not (Test-Path -LiteralPath $AgentPath)) { return }

  $items = Get-ChildItem -LiteralPath $AgentPath -Force -ErrorAction SilentlyContinue
  foreach ($item in $items) {
    try {
      Remove-Item -LiteralPath $item.FullName -Recurse -Force -ErrorAction Stop
    } catch {
      $err = $_
      throw "Failed to clear '$($item.FullName)' while reinitializing ${AgentPath}: $err"
    }
  }
}

# Create a worktree for an agent
function Ensure-Worktree {
  param([int]$Index)

  if (-not $script:GitFetched) {
    Invoke-GitSafe @('fetch','--all','--prune') -Quiet
    $script:GitFetched = $true
  }

  $branch = "agents/agent-$Index"
  $target = Join-Path $WorktreesRoot "agent-$Index"
  $fullTarget = [System.IO.Path]::GetFullPath($target)
  $resolvedTarget = $null

  Push-Location $RepoRoot
  try {
    $worktrees = Get-GitWorktrees
    $branchRef = "refs/heads/$branch"
    $thisWorktree = $worktrees | Where-Object { $_.FullPath -ieq $fullTarget }
    $branchWorktree = $worktrees | Where-Object { $_.Branch -eq $branchRef }
    $isRegistered = $null -ne $thisWorktree

    # Check if directory exists and has content
    $dirExists = Test-Path $target
    $isEmpty = $true
    if ($dirExists) {
      $items = Get-ChildItem -Path $target -Force -ErrorAction SilentlyContinue | Select-Object -First 1
      $isEmpty = $null -eq $items
    }

    if ($isRegistered -and $dirExists -and -not $isEmpty) {
      # Worktree is registered and has content - reset to requested base
      Write-Host "Worktree exists and is registered: $target"
      Reset-AgentWorktree -WorktreePath $target -ResetRef $BaseRef
    } elseif ($isRegistered -and (-not $dirExists -or $isEmpty)) {
      # Worktree is registered but directory is missing or empty - repair it
      Write-Host "Repairing worktree $target (directory missing or empty)..."
      Remove-GitWorktreePath -WorktreePath $thisWorktree.RawPath
      Prune-GitWorktreesNow
      $isRegistered = $false
      $branchWorktree = $null
    } elseif (-not $isRegistered -and $dirExists) {
      if (-not $ForceCleanup) {
        throw "Directory $target exists but is not a registered worktree. Close any VS Code windows, run tear-down with -RemoveWorktrees, or rerun spin-up with -ForceCleanup to reset it."
      }

      Write-Host "Directory $target exists but is not registered; reinitializing worktree in place (contents will be reset)."
      try {
        Detach-GitWorktreeMetadata -WorktreePath $target | Out-Null
      } catch {
        $err = $_
        Write-Warning "Failed to remove stale git metadata from ${target}: $err"
      }

      Clear-AgentDirectory -AgentPath $target
    }

    # Create worktree if needed
    if (-not $isRegistered) {
      $branchWorktree = Get-GitWorktreeForBranch -Branch $branch
      if ($branchWorktree -and $branchWorktree.FullPath -ne $fullTarget) {
        throw "Branch '$branch' is already attached to worktree '$($branchWorktree.FullPath)'. Remove it (git worktree remove --force -- `"$($branchWorktree.RawPath)`") before continuing."
      }

      $addArgs = @('worktree','add')
      if (Test-Path -LiteralPath $target) { $addArgs += '--force' }
      $addArgs += $target

      if (Test-GitBranchExists -Branch $branch) {
        Write-Host "Resetting $branch to $BaseRef before reuse..."
        Reset-GitBranchToRef -Branch $branch -Ref $BaseRef
        Write-Host "Creating worktree $target with existing branch $branch..."
        $addArgs += $branch
      } else {
        Write-Host "Creating worktree $target with new branch $branch from $BaseRef..."
        $addArgs += @('-b',$branch,$BaseRef)
      }

      Invoke-GitSafe $addArgs -Quiet
    }

    if (-not (Test-Path $target)) {
      throw "Worktree path '$target' was not created. Check git output above for errors."
    }
    Ensure-RelativeGitDir -WorktreePath $target -RepoRoot $RepoRoot -WorktreeName "agent-$Index"
    $resolvedTarget = (Resolve-Path -LiteralPath $target).Path

    if (-not $isRegistered) {
      Reset-AgentWorktree -WorktreePath $resolvedTarget -ResetRef $BaseRef
    }
  } finally {
    Pop-Location
  }

  return @{ Branch = $branch; Path = $resolvedTarget }
}

# Start or reuse a container per agent
function Ensure-Container {
  param(
    [int]$Index,
    [string]$AgentPath,
    [string]$RepoRoot,
    [string]$WorktreesRoot
  )
  $name = "fw-agent-$Index"

  # Per-agent NuGet cache folder on host
  $nugetHost = Join-Path $RepoRoot (".nuget\packages-agent-$Index")
  New-Item -ItemType Directory -Force -Path $nugetHost | Out-Null

  $driveMappings = @{}
  foreach ($path in @($AgentPath,$RepoRoot,$WorktreesRoot)) {
    if (-not $path) { continue }
    $drive = Get-DriveRoot $path
    if (-not $drive) { continue }
    if (-not $driveMappings.ContainsKey($drive)) {
      $driveId = Get-DriveIdentifier $drive
      $containerRoot = Join-Path "C:\fw-mounts" $driveId
      $driveMappings[$drive] = $containerRoot
    }
  }

  $containerAgentPath = Convert-ToContainerPath -Path $AgentPath -DriveMappings $driveMappings

  $escapedName = [regex]::Escape($name)
  $states = Invoke-DockerSafe @('ps','-a','--format','{{.Names}} {{.Status}}') -CaptureOutput
  $state = $states | Where-Object { $_ -match "^$escapedName\b" }
  $containerExists = $false
  $containerRunning = $false
  $needsRecreate = $false
  $inspect = $null
  if ($state) {
    $containerExists = $true
    $containerRunning = $state -match "Up "
    try {
      $inspect = Get-DockerInspectObject -Name $name
    } catch {
      Write-Warning "Failed to inspect container $name. Recreating."
      $needsRecreate = $true
    }

    if ($inspect) {
      if ($inspect.State -and $inspect.State.Status -eq 'running') {
        $containerRunning = $true
      }

      if ($inspect.Config -and $inspect.Config.WorkingDir -and ($inspect.Config.WorkingDir -ne $containerAgentPath)) {
        Write-Warning "Container $name working directory '$($inspect.Config.WorkingDir)' does not match expected '$containerAgentPath'. Recreating."
        $needsRecreate = $true
      }

      if ($inspect.State -and $inspect.State.Error) {
        Write-Warning "Container $name previously failed to start: $($inspect.State.Error). Recreating."
        $needsRecreate = $true
      }

      if ($inspect.HostConfig -and $inspect.HostConfig.Binds) {
        foreach ($bind in $inspect.HostConfig.Binds) {
          if (-not $bind) { continue }
          $sourcePath = $null
          if ($bind -match '^(?<src>[A-Za-z]:[^:]*?):(?<dest>.+)$') {
            $sourcePath = $matches.src
          } else {
            $parts = $bind.Split(':',2)
            $sourcePath = $parts[0]
          }
          if (-not (Test-Path -LiteralPath $sourcePath)) {
            Write-Warning "Container $name references missing host path '$sourcePath'. Recreating."
            $needsRecreate = $true
            break
          }
        }
      }
    }
  }

  if ($needsRecreate -and $containerExists) {
    Write-Host "Removing stale container $name..."
    if ($containerRunning) {
      Invoke-DockerSafe @('stop',$name) -Quiet
      $containerRunning = $false
    }
    Invoke-DockerSafe @('rm',$name) -Quiet
    $containerExists = $false
  }

  if ($containerExists) {
    if (-not $containerRunning) {
      Write-Host "Starting existing container $name..."
      Invoke-DockerSafe @('start',$name) -Quiet
    } else {
      Write-Host "Container $name already running."
    }
  } else {
    $args = @(
      "run","-d",
      "--name",$name,
      "--isolation=process",
      "--memory",$ContainerMemory,
      "--workdir",$containerAgentPath
    )

    foreach ($entry in $driveMappings.GetEnumerator()) {
      $args += @("-v","$($entry.Key):$($entry.Value)")
    }

    $args += @(
      "-v","${nugetHost}:C:\.nuget\packages",
      "-e","NUGET_PACKAGES=C:\.nuget\packages",
      $ImageTag,
      "powershell","-NoLogo","-ExecutionPolicy","Bypass","-Command",'while ($true) { Start-Sleep -Seconds 3600 }'
    )
    Invoke-DockerSafe $args -Quiet
  }

  return @{ Name=$name; NuGetCache=$nugetHost; ContainerPath=$containerAgentPath }
}

function Write-Tasks {
  param(
    [int]$Index,
    [string]$AgentPath,
    [string]$ContainerName,
    [string]$ContainerAgentPath,
    [string]$SolutionRelPath,
    [hashtable]$Colors,
    [string]$RepoRoot,
    [switch]$Force
  )
  $worktreeGitDir = Get-GitDirectory -Path $AgentPath
  Ensure-GitExcludePatterns -GitDir $worktreeGitDir -Patterns @('.vscode/','.fw-agent/','agent-*.code-workspace')

  $vscode = Join-Path $AgentPath ".vscode"
  New-Item -ItemType Directory -Force -Path $vscode | Out-Null

  $tplPath = Join-Path $PSScriptRoot "templates\tasks.template.json"
  $content = Get-Content -Raw -Path $tplPath
  $out = Join-Path $vscode "tasks.json"
  if (-not $Force -and (Test-Path -LiteralPath $out)) {
    Write-Host "Found existing tasks at $out; skipping regeneration (use -ForceVsCodeSetup to overwrite)."
  } elseif (Set-FileContentIfChanged -Path $out -Content $content) {
    Write-Host "Updated $out"
  } else {
    Write-Host "Tasks already up to date at $out"
  }

  # Create workspace file with color customization embedded
  $workspace = @{
    "folders" = @(
      @{ "path" = "." }
    )
    "settings" = @{
      "fw.agent.solutionPath" = $SolutionRelPath
      "fw.agent.containerName" = $ContainerName
      "fw.agent.containerPath" = $ContainerAgentPath
      "fw.agent.repoRoot" = $RepoRoot
      "workbench.colorCustomizations" = @{
        "titleBar.activeBackground" = $Colors.Title
        "titleBar.activeForeground" = "#ffffff"
        "titleBar.inactiveBackground" = $Colors.Title
        "titleBar.inactiveForeground" = "#cccccc"
        "statusBar.background" = $Colors.Status
        "statusBar.foreground" = "#ffffff"
        "activityBar.background" = $Colors.Activity
        "activityBar.foreground" = "#ffffff"
      }
    }
  }

  $workspaceJson = $workspace | ConvertTo-Json -Depth 4
  $workspaceOut = Join-Path $AgentPath "agent-$Index.code-workspace"
  if (Set-FileContentIfChanged -Path $workspaceOut -Content $workspaceJson) {
    Write-Host "Updated $workspaceOut (theme: $($Colors.Name))"
  } else {
    Write-Host "Workspace already up to date at $workspaceOut (theme: $($Colors.Name))"
  }
}

function Write-AgentConfig {
  param(
    [string]$AgentPath,
    [string]$SolutionRelPath,
    [hashtable]$Container,
    [string]$RepoRoot
  )

  $configDir = Join-Path $AgentPath ".fw-agent"
  New-Item -ItemType Directory -Force -Path $configDir | Out-Null

  $config = @{
    "ContainerName" = $Container.Name
    "ContainerPath" = $Container.ContainerPath
    "SolutionRelPath" = $SolutionRelPath
    "RepositoryRoot" = $RepoRoot
  } | ConvertTo-Json -Depth 4

  $configPath = Join-Path $configDir "config.json"
  if (Set-FileContentIfChanged -Path $configPath -Content $config) {
    Write-Host "Updated $configPath"
  } else {
    Write-Host "Agent config already up to date at $configPath"
  }

  $worktreeGitDir = Get-GitDirectory -Path $AgentPath
  Ensure-GitExcludePatterns -GitDir $worktreeGitDir -Patterns @('.fw-agent/','agent-*.code-workspace')
}

Ensure-Image -Tag $ImageTag

$agents = @()
for ($i=1; $i -le $Count; $i++) {
  $wt = Ensure-Worktree -Index $i
  $ct = Ensure-Container -Index $i -AgentPath $wt.Path -RepoRoot $RepoRoot -WorktreesRoot $WorktreesRoot
  Write-AgentConfig -AgentPath $wt.Path -SolutionRelPath $SolutionRelPath -Container $ct -RepoRoot $RepoRoot
  $colors = Get-AgentColors -Index $i
  if (-not $SkipVsCodeSetup) { Write-Tasks -Index $i -AgentPath $wt.Path -ContainerName $ct.Name -ContainerAgentPath $ct.ContainerPath -SolutionRelPath $SolutionRelPath -Colors $colors -RepoRoot $RepoRoot -Force:$ForceVsCodeSetup }
  if (-not $SkipOpenVSCode) {
    $workspaceTarget = Join-Path $wt.Path "agent-$i.code-workspace"
    if (-not (Test-Path -LiteralPath $workspaceTarget)) {
      $workspaceTarget = $wt.Path
    }

    if (Test-VSCodeWorkspaceOpen -WorkspacePath $workspaceTarget) {
      Write-Host "VS Code already open for agent-$i; skipping new window launch."
    } else {
      Open-AgentVsCodeWindow -Index $i -AgentPath $wt.Path -ContainerName $ct.Name
    }
  }
  $agents += [pscustomobject]@{
    Index = $i
    Worktree = $wt.Path
    Branch = $wt.Branch
    Container = $ct.Name
    Theme = $colors.Name
  }
}

$agents | Format-Table -AutoSize
Write-Host "Done. Open each worktree in VS Code; use the generated tasks to build inside its container."
