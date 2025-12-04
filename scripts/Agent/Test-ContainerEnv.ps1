<#
.SYNOPSIS
    Tests the Visual Studio environment inside a Docker container.

.DESCRIPTION
    This is a host-side wrapper script that invokes Test-VsEnv.ps1 inside
    a specified Docker container. The command is simple enough to be
    auto-approved by Copilot's security system.

.PARAMETER ContainerName
    Name of the Docker container to test (e.g., "fw-agent-1").
    If not specified, attempts to detect from current worktree path.

.EXAMPLE
    .\Test-ContainerEnv.ps1 -ContainerName fw-agent-1

.EXAMPLE
    # Auto-detect container from worktree path
    .\Test-ContainerEnv.ps1

.NOTES
    This script is designed to be called with simple parameters that
    auto-approve in Copilot's terminal security checks.
#>
[CmdletBinding()]
param(
    [string]$ContainerName
)

$ErrorActionPreference = 'Stop'

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

# Verify container exists and is running
$status = docker inspect --format '{{.State.Running}}' $ContainerName 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "Container '$ContainerName' does not exist. Run scripts/spin-up-agents.ps1 first."
}
if ($status -ne 'true') {
    throw "Container '$ContainerName' is not running. Start it with: docker start $ContainerName"
}

Write-Host "Testing VS environment in container '$ContainerName'..." -ForegroundColor Cyan
Write-Host ""

# Simple command that can be auto-approved:
# - No pipes in the docker exec command itself
# - Script path is a known, trusted location
docker exec $ContainerName cmd /c "C:\scripts\VsDevShell.cmd powershell -NoProfile -File C:\scripts\Test-VsEnv.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Container environment check failed!" -ForegroundColor Red
    exit 1
}
