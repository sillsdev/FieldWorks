<#
.SYNOPSIS
Creates a Docker container for an existing agent worktree.

.DESCRIPTION
This script spins up a Docker container for a specific worktree, enabling
isolated builds and tests. It updates the agent configuration to use the container.

.EXAMPLE
.\scripts\spin-up-container.ps1 -Index 1
Creates container fw-agent-1 for the first agent worktree.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][int]$Index,
    [string]$RepoRoot,
    [string]$WorktreesRoot,
    [string]$ImageTag = "fw-build:ltsc2022",
    [string]$ContainerMemory = "8g"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$agentModule = Join-Path $PSScriptRoot 'Agent\AgentInfrastructure.psm1'
Import-Module $agentModule -Force -DisableNameChecking

$configModule = Join-Path $PSScriptRoot 'Agent\AgentConfiguration.psm1'
Import-Module $configModule -Force -DisableNameChecking

# Resolve roots
if (-not $RepoRoot) {
    $RepoRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
}
$RepoRoot = (Resolve-Path $RepoRoot).Path

$WorktreesRoot = Resolve-WorktreesRoot -WorktreesRoot $WorktreesRoot -RepoRoot $RepoRoot

$agentPath = Get-WorktreePath -WorktreesRoot $WorktreesRoot -Index $Index
if (-not (Test-Path $agentPath)) {
    throw "Worktree for agent $Index not found at $agentPath"
}

# Build image if missing
function Ensure-Image {
  param([string]$Tag)
  $images = Invoke-DockerSafe @('images','--format','{{.Repository}}:{{.Tag}}') -CaptureOutput
  $exists = $images | Where-Object { $_ -eq $Tag }
  if (-not $exists) {
    Write-Host "Building image $Tag..."
    $buildWrapper = Join-Path $PSScriptRoot "docker-build-ipv4.ps1"
    $dockerfilePath = Join-Path $RepoRoot "Dockerfile.windows"
    & $buildWrapper -RegistryHosts @("mcr.microsoft.com") -BuildArgs @("-t", $Tag, "-f", $dockerfilePath, $RepoRoot)
  }
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

  if ($state) {
    $containerExists = $true
    $containerRunning = $state -match "Up "
  }

  # NuGet cache strategy: FULLY ISOLATED per container
  # No shared volumes. Each container has its own local cache.

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
      "--isolation=hyperv",
      "--memory",$ContainerMemory,
      "--workdir",$containerAgentPath
    )

    foreach ($entry in $driveMappings.GetEnumerator()) {
      $args += @("-v","$($entry.Key):$($entry.Value)")
    }

    $containerPackagesPath = Get-NuGetPackagesPath
    $containerHttpCachePath = Get-NuGetHttpCachePath
    $containerTempPath = Get-ContainerTempPath
    $args += @(
      "-e","NUGET_PACKAGES=$containerPackagesPath",
      "-e","NUGET_HTTP_CACHE_PATH=$containerHttpCachePath",
      "-e","TEMP=$containerTempPath",
      "-e","TMP=$containerTempPath",
      $ImageTag,
      "powershell","-NoLogo","-ExecutionPolicy","Bypass","-Command","New-Item -ItemType Directory -Force -Path '$containerPackagesPath','$containerHttpCachePath','$containerTempPath' | Out-Null; while (`$true) { Start-Sleep -Seconds 3600 }"
    )
    Invoke-DockerSafe $args -Quiet
  }

  return @{ Name=$name; ContainerPath=$containerAgentPath; UseContainer=$true }
}

Ensure-Image -Tag $ImageTag
$ct = Ensure-Container -Index $Index -AgentPath $agentPath -RepoRoot $RepoRoot -WorktreesRoot $WorktreesRoot

# Update config.json
$configDir = Join-Path $agentPath ".fw-agent"
$configPath = Join-Path $configDir "config.json"
if (Test-Path $configPath) {
    $config = Get-Content -Path $configPath -Raw | ConvertFrom-Json
    $config.UseContainer = $true
    $config.ContainerName = $ct.Name
    $config.ContainerPath = $ct.ContainerPath
    $config | ConvertTo-Json -Depth 4 | Set-Content -Path $configPath
    Write-Host "Updated $configPath to use container $($ct.Name)"
} else {
    Write-Warning "Config file not found at $configPath"
}
