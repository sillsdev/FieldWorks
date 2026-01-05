<#
.SYNOPSIS
    Runs a pre-approved script inside a Docker container.

.DESCRIPTION
    Provides a safe way to execute known scripts inside Docker containers.
    Only scripts in the allowed list can be run, preventing arbitrary command
    execution. Commands are structured to be simple enough for Copilot auto-approval.

    The script automatically detects the container's working directory by:
    1. Querying the container's WorkingDir configuration
    2. Falling back to computing the fw-mounts path from the host path

.PARAMETER ContainerName
    Name of the Docker container (e.g., "fw-agent-1").
    If not specified, auto-detects from worktree path.

.PARAMETER Script
    Name of the script to run. Must be one of the allowed scripts:
    - test-vsenv: Tests Visual Studio environment configuration
    - build: Runs the build script
    - clean: Cleans build artifacts

.PARAMETER Arguments
    Additional arguments to pass to the script.

.EXAMPLE
    .\Invoke-InContainer.ps1 -Script test-vsenv

.EXAMPLE
    .\Invoke-InContainer.ps1 -ContainerName fw-agent-2 -Script build -Arguments "-Configuration","Release"
#>
[CmdletBinding()]
param(
    [string]$ContainerName,

    [Parameter(Mandatory = $true)]
    [ValidateSet('test-vsenv', 'build', 'clean')]
    [string]$Script,

    [string[]]$Arguments = @()
)

$ErrorActionPreference = 'Stop'

# =============================================================================
# Import Shared Module
# =============================================================================

$helpersPath = Join-Path $PSScriptRoot "..\..\Build\Agent\FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
    Write-Host "[ERROR] FwBuildHelpers.psm1 not found at $helpersPath" -ForegroundColor Red
    exit 1
}
Import-Module $helpersPath -Force

# =============================================================================
# Script Registry
# =============================================================================

# Script registry - maps friendly names to relative paths and options
# Paths are relative to the container working directory (detected dynamically)
$ScriptRegistry = @{
    'test-vsenv' = @{
        # test-vsenv is a container-level script, not repo-relative
        AbsolutePath = 'C:\scripts\Test-VsEnv.ps1'
        RequiresVsEnv = $true
        Description = 'Test Visual Studio environment'
    }
    'build' = @{
        RelativePath = 'build.ps1'
        RequiresVsEnv = $true
        Description = 'Build FieldWorks'
    }
    'clean' = @{
        RelativePath = 'build.ps1'
        RequiresVsEnv = $true
        Description = 'Clean build artifacts'
        DefaultArgs = @('-Clean')
    }
}

# =============================================================================
# Container Detection
# =============================================================================

# Auto-detect container name from worktree path if not specified
if (-not $ContainerName) {
    $currentPath = (Get-Location).Path
    if ($currentPath -match '[/\\](?:fw-)?worktrees[/\\]agent-(\d+)') {
        $ContainerName = "fw-agent-$($Matches[1])"
        Write-Host "Auto-detected container: $ContainerName" -ForegroundColor Gray
    } else {
        throw "ContainerName not specified and could not auto-detect from path: $currentPath"
    }
}

# Verify container is running
$status = docker inspect --format '{{.State.Running}}' $ContainerName 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "Container '$ContainerName' does not exist. Run scripts/spin-up-agents.ps1 first."
}
if ($status -ne 'true') {
    throw "Container '$ContainerName' is not running. Start it with: docker start $ContainerName"
}

# =============================================================================
# Determine Script Path
# =============================================================================

# Get script info
$scriptInfo = $ScriptRegistry[$Script]
$requiresVsEnv = $scriptInfo.RequiresVsEnv

# Determine the full script path in the container
if ($scriptInfo.AbsolutePath) {
    # Container-level script with absolute path (e.g., test-vsenv)
    $scriptPath = $scriptInfo.AbsolutePath
} else {
    # Repo-relative script - need to resolve container working directory
    $containerWorkDir = Get-ContainerWorkDir -ContainerName $ContainerName
    $scriptPath = Join-Path $containerWorkDir $scriptInfo.RelativePath
    # Normalize path separators for Windows
    $scriptPath = $scriptPath -replace '/', '\'
}

# Merge default args with user args
$allArgs = @()
if ($scriptInfo.DefaultArgs) {
    $allArgs += $scriptInfo.DefaultArgs
}
$allArgs += $Arguments

Write-Host "Running '$($scriptInfo.Description)' in container '$ContainerName'..." -ForegroundColor Cyan
Write-Host "   Script path: $scriptPath" -ForegroundColor DarkGray

# =============================================================================
# Execute in Container
# =============================================================================

# Build the command - keeping it simple for auto-approval
if ($requiresVsEnv) {
    # Use VsDevShell.cmd to initialize environment
    # For build/clean scripts, add -NoDocker to prevent recursion
    $noDockerSuffix = ''
    if ($Script -in @('build', 'clean')) {
        $noDockerSuffix = ' -NoDocker'
    }

    if ($allArgs.Count -gt 0) {
        $argString = $allArgs -join ' '
        docker exec $ContainerName cmd /c "C:\scripts\VsDevShell.cmd powershell -NoProfile -File $scriptPath $argString$noDockerSuffix"
    } else {
        docker exec $ContainerName cmd /c "C:\scripts\VsDevShell.cmd powershell -NoProfile -File $scriptPath$noDockerSuffix"
    }
} else {
    if ($allArgs.Count -gt 0) {
        $argString = $allArgs -join ' '
        docker exec $ContainerName powershell -NoProfile -File $scriptPath $argString
    } else {
        docker exec $ContainerName powershell -NoProfile -File $scriptPath
    }
}

$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "[FAIL] Script '$Script' failed with exit code $exitCode" -ForegroundColor Red
    exit $exitCode
}

Write-Host ""
Write-Host "[OK] '$Script' completed successfully" -ForegroundColor Green
