<#
.SYNOPSIS
Shared configuration and utilities for agent worktree/container management.
Used by spin-up-agents.ps1 and tear-down-agents.ps1.
#>
Set-StrictMode -Version Latest

#region Constants

# Docker container naming
$script:ContainerPrefix = "fw-agent"

# Git branch naming
$script:BranchPrefix = "agents/agent"

# NuGet cache configuration (hybrid architecture)
$script:NuGetVolumeName = "fw-nuget-cache"
$script:NuGetMountPath = "C:\NuGetCache"
$script:NuGetPackagesPath = "C:\NuGetCache\packages"
$script:NuGetHttpCachePath = "C:\NuGetCache\http-cache"
$script:ContainerTempPath = "C:\Temp"  # Container-local, NOT on named volume

# Default container memory (8GB needed for full managed build)
$script:DefaultContainerMemory = "8g"

# Default image tag
$script:DefaultImageTag = "fw-build:ltsc2022"

#endregion

#region Naming Functions

function Get-ContainerName {
    param([Parameter(Mandatory)][int]$Index)
    return "$script:ContainerPrefix-$Index"
}

function Get-BranchName {
    param([Parameter(Mandatory)][int]$Index)
    return "$script:BranchPrefix-$Index"
}

function Get-WorktreePath {
    param(
        [Parameter(Mandatory)][string]$WorktreesRoot,
        [Parameter(Mandatory)][int]$Index
    )
    return Join-Path $WorktreesRoot "agent-$Index"
}

#endregion

#region WorktreesRoot Resolution

function Resolve-WorktreesRoot {
    <#
    .SYNOPSIS
    Resolves the worktrees root directory from parameter, env var, or default.
    #>
    param(
        [string]$WorktreesRoot,
        [Parameter(Mandatory)][string]$RepoRoot,
        [switch]$Create
    )

    if (-not $WorktreesRoot) {
        if ($env:FW_WORKTREES_ROOT) {
            $WorktreesRoot = $env:FW_WORKTREES_ROOT
        } else {
            $WorktreesRoot = Join-Path $RepoRoot "worktrees"
        }
    }

    if ($Create -and -not (Test-Path $WorktreesRoot)) {
        New-Item -ItemType Directory -Force -Path $WorktreesRoot | Out-Null
    }

    if (Test-Path $WorktreesRoot) {
        return (Resolve-Path $WorktreesRoot).Path
    }

    return $WorktreesRoot
}

#endregion

#region Agent Discovery

function Get-AgentIndices {
    <#
    .SYNOPSIS
    Discovers all agent indices from worktrees and git branches.
    #>
    param(
        [Parameter(Mandatory)][string]$WorktreesRoot,
        [Parameter(Mandatory)][string]$RepoRoot
    )

    . (Join-Path (Split-Path $PSScriptRoot -Parent) 'git-utilities.ps1')

    $set = New-Object System.Collections.Generic.HashSet[int]

    # Find worktree directories
    if (Test-Path $WorktreesRoot) {
        Get-ChildItem -Path $WorktreesRoot -Directory -Filter 'agent-*' -ErrorAction SilentlyContinue |
            ForEach-Object {
                if ($_.Name -match '^agent-(\d+)$') {
                    [void]$set.Add([int]$matches[1])
                }
            }
    }

    # Find git branches
    Push-Location $RepoRoot
    try {
        $branches = Invoke-GitSafe @('branch', '--list', 'agents/agent-*') -CaptureOutput
        foreach ($branch in $branches) {
            if ($branch -match 'agents/agent-(\d+)') {
                [void]$set.Add([int]$matches[1])
            }
        }
    } finally {
        Pop-Location
    }

    return ($set | Sort-Object)
}

#endregion

#region Container Discovery

function Get-AgentContainers {
    <#
    .SYNOPSIS
    Lists all fw-agent-* containers (running or stopped).
    #>
    $allContainers = Invoke-DockerSafe @('ps', '-a', '--format', '{{.Names}}') -CaptureOutput
    return @($allContainers | Where-Object { $_ -match '^fw-agent-\d+$' })
}

function Remove-AgentContainers {
    <#
    .SYNOPSIS
    Stops and removes all fw-agent-* containers.
    #>
    param([switch]$Quiet)

    $containers = Get-AgentContainers
    if ($containers.Count -eq 0) {
        if (-not $Quiet) { Write-Host "No fw-agent-* containers found." }
        return
    }

    foreach ($name in $containers) {
        if (-not $Quiet) { Write-Host "Stopping/removing container $name..." }
        Invoke-DockerSafe @('rm', '-f', $name) -Quiet
    }
}

#endregion

#region NuGet Volume Management

function Get-NuGetVolumeName { return $script:NuGetVolumeName }
function Get-NuGetMountPath { return $script:NuGetMountPath }
function Get-NuGetPackagesPath { return $script:NuGetPackagesPath }
function Get-NuGetHttpCachePath { return $script:NuGetHttpCachePath }
function Get-ContainerTempPath { return $script:ContainerTempPath }

function Test-NuGetVolumeExists {
    $existingVolumes = Invoke-DockerSafe @('volume', 'ls', '--format', '{{.Name}}') -CaptureOutput
    return ($existingVolumes -contains $script:NuGetVolumeName)
}

function New-NuGetVolume {
    <#
    .SYNOPSIS
    Creates the NuGet cache volume if it doesn't exist.
    #>
    param([switch]$Force)

    if ($Force -or -not (Test-NuGetVolumeExists)) {
        Write-Host "Creating NuGet cache volume: $script:NuGetVolumeName"
        Invoke-DockerSafe @('volume', 'create', $script:NuGetVolumeName) -Quiet
        return $true
    }
    return $false
}

function Remove-NuGetVolume {
    <#
    .SYNOPSIS
    Removes the NuGet cache volume (forces full re-download on next build).
    #>
    param([switch]$Quiet)

    if (Test-NuGetVolumeExists) {
        if (-not $Quiet) { Write-Host "Removing NuGet cache volume: $script:NuGetVolumeName" }
        Invoke-DockerSafe @('volume', 'rm', $script:NuGetVolumeName) -Quiet
        return $true
    } else {
        if (-not $Quiet) { Write-Host "NuGet cache volume '$script:NuGetVolumeName' not found." }
        return $false
    }
}

#endregion

#region Process Detection (for troubleshooting locks)

function Get-ProcessesReferencingPath {
    <#
    .SYNOPSIS
    Finds processes that reference a given path fragment in their command line or executable path.
    Useful for diagnosing file lock issues.
    #>
    param([Parameter(Mandatory)][string]$PathFragment)

    $resolved = $null
    try {
        if (Test-Path -LiteralPath $PathFragment) {
            $resolved = (Resolve-Path -LiteralPath $PathFragment).Path
        } else {
            $resolved = $PathFragment
        }
    } catch {
        $resolved = $PathFragment
    }

    if (-not $resolved) { return @() }
    $needle = $resolved.ToLowerInvariant()

    try {
        $processes = @(Get-CimInstance Win32_Process -ErrorAction Stop)
    } catch {
        return @()
    }

    $results = @()
    foreach ($proc in $processes) {
        $cmd = $proc.CommandLine
        $exe = $proc.ExecutablePath
        $cmdMatch = $cmd -and $cmd.ToLowerInvariant().Contains($needle)
        $exeMatch = $exe -and $exe.ToLowerInvariant().Contains($needle)
        if ($cmdMatch -or $exeMatch) {
            $results += [pscustomobject]@{
                ProcessId   = $proc.ProcessId
                Name        = $proc.Name
                CommandLine = $cmd
            }
        }
    }
    return @($results)
}

function Write-LockingProcesses {
    <#
    .SYNOPSIS
    Reports processes that may be locking a path.
    #>
    param([Parameter(Mandatory)][string]$PathFragment)

    $procs = @(Get-ProcessesReferencingPath -PathFragment $PathFragment)
    if ($procs.Count -eq 0) {
        Write-Warning "Could not identify a specific process locking $PathFragment."
        return
    }

    Write-Warning "Processes referencing ${PathFragment}:"
    foreach ($proc in $procs) {
        $cmd = if ([string]::IsNullOrWhiteSpace($proc.CommandLine)) {
            '<no command line available>'
        } else {
            $proc.CommandLine
        }
        Write-Warning "  PID $($proc.ProcessId) - $($proc.Name): $cmd"
    }
}

#endregion

#region Agent Colors (for VS Code workspace theming)

function Get-AgentColors {
    <#
    .SYNOPSIS
    Returns a color scheme for an agent index (cycles through 5 distinct colors).
    #>
    param([Parameter(Mandatory)][int]$Index)

    $colors = @(
        @{ Title = "#1e3a8a"; Status = "#1e40af"; Activity = "#1e3a8a"; Name = "Blue" }
        @{ Title = "#15803d"; Status = "#16a34a"; Activity = "#15803d"; Name = "Green" }
        @{ Title = "#9333ea"; Status = "#a855f7"; Activity = "#9333ea"; Name = "Purple" }
        @{ Title = "#c2410c"; Status = "#ea580c"; Activity = "#c2410c"; Name = "Orange" }
        @{ Title = "#be123c"; Status = "#e11d48"; Activity = "#be123c"; Name = "Rose" }
    )
    $idx = ($Index - 1) % $colors.Count
    return $colors[$idx]
}

#endregion

#region JSON Utilities

function ConvertTo-OrderedStructure {
    <#
    .SYNOPSIS
    Recursively converts PSCustomObject/Hashtable to ordered hashtables for consistent JSON output.
    #>
    param([object]$Value)

    if ($null -eq $Value) { return $null }

    if ($Value -is [System.Collections.IDictionary]) {
        $ordered = [ordered]@{}
        foreach ($key in $Value.Keys) {
            $ordered[$key] = ConvertTo-OrderedStructure -Value $Value[$key]
        }
        return $ordered
    }

    if ($Value -is [pscustomobject]) {
        $ordered = [ordered]@{}
        foreach ($prop in $Value.PSObject.Properties) {
            $ordered[$prop.Name] = ConvertTo-OrderedStructure -Value $prop.Value
        }
        return $ordered
    }

    if (($Value -is [System.Collections.IEnumerable]) -and -not ($Value -is [string])) {
        $list = @()
        foreach ($item in $Value) {
            $list += , (ConvertTo-OrderedStructure -Value $item)
        }
        return $list
    }

    return $Value
}

#endregion

# Export all public functions
Export-ModuleMember -Function @(
    # Naming
    'Get-ContainerName',
    'Get-BranchName',
    'Get-WorktreePath',
    # WorktreesRoot
    'Resolve-WorktreesRoot',
    # Discovery
    'Get-AgentIndices',
    'Get-AgentContainers',
    'Remove-AgentContainers',
    # NuGet
    'Get-NuGetVolumeName',
    'Get-NuGetMountPath',
    'Get-NuGetPackagesPath',
    'Get-NuGetHttpCachePath',
    'Get-ContainerTempPath',
    'Test-NuGetVolumeExists',
    'New-NuGetVolume',
    'Remove-NuGetVolume',
    # Process detection
    'Get-ProcessesReferencingPath',
    'Write-LockingProcesses',
    # Colors
    'Get-AgentColors',
    # JSON
    'ConvertTo-OrderedStructure'
)
