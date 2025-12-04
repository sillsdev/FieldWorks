<#
.SYNOPSIS
    Runs a pre-approved script inside a Docker container.

.DESCRIPTION
    Provides a safe way to execute known scripts inside Docker containers.
    Only scripts in the allowed list can be run, preventing arbitrary command
    execution. Commands are structured to be simple enough for Copilot auto-approval.

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

# Script registry - maps friendly names to container paths
$ScriptRegistry = @{
    'test-vsenv' = @{
        Path = 'C:\scripts\Test-VsEnv.ps1'
        RequiresVsEnv = $true
        Description = 'Test Visual Studio environment'
    }
    'build' = @{
        Path = 'C:\fw\build.ps1'
        RequiresVsEnv = $true
        Description = 'Build FieldWorks'
    }
    'clean' = @{
        Path = 'C:\fw\build.ps1'
        RequiresVsEnv = $true
        Description = 'Clean build artifacts'
        DefaultArgs = @('-Clean')
    }
}

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

# Get script info
$scriptInfo = $ScriptRegistry[$Script]
$scriptPath = $scriptInfo.Path
$requiresVsEnv = $scriptInfo.RequiresVsEnv

# Merge default args with user args
$allArgs = @()
if ($scriptInfo.DefaultArgs) {
    $allArgs += $scriptInfo.DefaultArgs
}
$allArgs += $Arguments

Write-Host "Running '$($scriptInfo.Description)' in container '$ContainerName'..." -ForegroundColor Cyan

# Build the command - keeping it simple for auto-approval
if ($requiresVsEnv) {
    # Use VsDevShell.cmd to initialize environment
    if ($allArgs.Count -gt 0) {
        $argString = $allArgs -join ' '
        docker exec $ContainerName cmd /c "C:\scripts\VsDevShell.cmd powershell -NoProfile -File $scriptPath $argString"
    } else {
        docker exec $ContainerName cmd /c "C:\scripts\VsDevShell.cmd powershell -NoProfile -File $scriptPath"
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
    Write-Host "Script '$Script' failed with exit code $exitCode" -ForegroundColor Red
    exit $exitCode
}

Write-Host ""
Write-Host "✓ '$Script' completed successfully" -ForegroundColor Green
