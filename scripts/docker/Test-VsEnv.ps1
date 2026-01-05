<#
.SYNOPSIS
    Tests that the Visual Studio build environment is properly configured.

.DESCRIPTION
    This script verifies that the VS Developer environment is initialized and
    required build tools (nmake, cl, msbuild) are available. Designed to run
    inside Docker containers after VsDevShell.cmd has been called.

.OUTPUTS
    Writes diagnostic information to stdout. Returns exit code 0 on success,
    1 if critical tools are missing.

.EXAMPLE
    # From host, run inside container:
    docker exec fw-agent-1 cmd /c "C:\scripts\VsDevShell.cmd powershell -NoProfile -File C:\scripts\Test-VsEnv.ps1"
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$exitCode = 0

Write-Host "=== Visual Studio Environment Check ===" -ForegroundColor Cyan
Write-Host ""

# Check VCINSTALLDIR
Write-Host "VCINSTALLDIR: " -NoNewline
if ($env:VCINSTALLDIR) {
    Write-Host $env:VCINSTALLDIR -ForegroundColor Green
} else {
    Write-Host "(not set)" -ForegroundColor Red
    $exitCode = 1
}

# Check VSINSTALLDIR
Write-Host "VSINSTALLDIR: " -NoNewline
if ($env:VSINSTALLDIR) {
    Write-Host $env:VSINSTALLDIR -ForegroundColor Green
} else {
    Write-Host "(not set)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Build Tools ===" -ForegroundColor Cyan

# Check for critical build tools
$tools = @(
    @{ Name = 'nmake.exe'; Critical = $true },
    @{ Name = 'cl.exe'; Critical = $true },
    @{ Name = 'msbuild.exe'; Critical = $true },
    @{ Name = 'link.exe'; Critical = $false },
    @{ Name = 'lib.exe'; Critical = $false }
)

foreach ($tool in $tools) {
    $cmd = Get-Command $tool.Name -ErrorAction SilentlyContinue
    Write-Host "$($tool.Name): " -NoNewline
    if ($cmd) {
        Write-Host $cmd.Source -ForegroundColor Green
    } else {
        if ($tool.Critical) {
            Write-Host "(not found - CRITICAL)" -ForegroundColor Red
            $exitCode = 1
        } else {
            Write-Host "(not found)" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Platform ===" -ForegroundColor Cyan
Write-Host "VSCMD_ARG_TGT_ARCH: " -NoNewline
if ($env:VSCMD_ARG_TGT_ARCH) {
    Write-Host $env:VSCMD_ARG_TGT_ARCH -ForegroundColor Green
} else {
    Write-Host "(not set)" -ForegroundColor Yellow
}

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "✓ VS environment is properly configured" -ForegroundColor Green
} else {
    Write-Host "✗ VS environment has issues - build may fail" -ForegroundColor Red
}

exit $exitCode
