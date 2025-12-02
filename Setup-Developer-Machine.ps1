# Setup-Developer-Machine.ps1
# One-stop setup script for FieldWorks development environment on Windows.
# Run this script in an elevated PowerShell prompt to install required tools.
#
# Usage:
#   .\Setup-Developer-Machine.ps1                    # Install everything
#   .\Setup-Developer-Machine.ps1 -SkipVSCheck       # Skip Visual Studio check
#   .\Setup-Developer-Machine.ps1 -WhatIf            # Show what would be installed
#
# Prerequisites (must be installed manually):
#   - Visual Studio 2022 with:
#     - .NET desktop development workload
#     - Desktop development with C++ workload (including ATL/MFC)
#   - Git for Windows
#
# Note: Serena MCP language servers (Microsoft's Roslyn C# server and clangd for C++)
# auto-download on first use. No manual installation needed for Serena support.

[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$SkipVSCheck,       # Skip Visual Studio installation check
    [switch]$Force              # Force reinstall even if already present
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FieldWorks Developer Machine Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Require elevation for Machine-level PATH changes
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Warning "This script should be run as Administrator for Machine-level PATH updates."
    Write-Warning "User-level PATH will be used instead (may require manual adjustment)."
    $useUserPath = $true
} else {
    $useUserPath = $false
}

#region Prerequisites Check

Write-Host "`n--- Checking Prerequisites ---" -ForegroundColor Yellow

# Check Git
$git = Get-Command git -ErrorAction SilentlyContinue
if ($git) {
    Write-Host "[OK] Git: $((git --version) -replace 'git version ','')" -ForegroundColor Green
} else {
    Write-Host "[MISSING] Git - Please install from https://git-scm.com/" -ForegroundColor Red
    exit 1
}

# Check Visual Studio 2022
if (-not $SkipVSCheck) {
    $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vsWhere) {
        $vsInstall = & $vsWhere -latest -property installationPath 2>$null
        if ($vsInstall) {
            $vsVersion = & $vsWhere -latest -property catalog_productDisplayVersion 2>$null
            Write-Host "[OK] Visual Studio 2022: $vsVersion" -ForegroundColor Green

            # Check required workloads
            $workloads = & $vsWhere -latest -property catalog_productLineVersion 2>$null
            Write-Host "     Location: $vsInstall" -ForegroundColor Gray
        } else {
            Write-Host "[MISSING] Visual Studio 2022 - Please install with:" -ForegroundColor Red
            Write-Host "         - .NET desktop development workload" -ForegroundColor Red
            Write-Host "         - Desktop development with C++ workload" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "[MISSING] Visual Studio 2022 - Please install from https://visualstudio.microsoft.com/" -ForegroundColor Red
        exit 1
    }
}

#endregion

#region Tool Installation

Write-Host "`n--- Installing Development Tools ---" -ForegroundColor Yellow

# Determine install locations (use standard paths, not C:\ root for dev machines)
$toolsBase = "$env:LOCALAPPDATA\FieldWorksTools"
if (-not (Test-Path $toolsBase)) {
    New-Item -ItemType Directory -Path $toolsBase -Force | Out-Null
}

# Check what's already installed
$wixInstalled = (Test-Path 'C:\Wix311') -or (Test-Path "$toolsBase\Wix311") -or (Get-Command candle.exe -ErrorAction SilentlyContinue)

# WiX Toolset 3.11.x
if ($wixInstalled -and -not $Force) {
    Write-Host "[OK] WiX Toolset already installed" -ForegroundColor Green
} else {
    if ($PSCmdlet.ShouldProcess("WiX Toolset 3.11.x", "Install")) {
        Write-Host "Installing WiX Toolset 3.11.x..." -ForegroundColor Cyan
        $wixPath = "$toolsBase\Wix311"
        $tempZip = "$env:TEMP\wix311.zip"
        Invoke-WebRequest -Uri 'https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311-binaries.zip' -OutFile $tempZip
        Expand-Archive -LiteralPath $tempZip -DestinationPath $wixPath -Force
        Remove-Item $tempZip -Force
        Write-Host "[OK] WiX Toolset installed to $wixPath" -ForegroundColor Green
    }
}

# Note: Serena MCP language servers auto-download on first use:
# - C# (csharp): Microsoft.CodeAnalysis.LanguageServer (Roslyn) from Azure NuGet
# - C++ (cpp): clangd from GitHub releases
# No manual installation needed!
Write-Host ""
Write-Host "[INFO] Serena language servers (C# Roslyn, clangd) auto-download on first use" -ForegroundColor Gray

#endregion

#region PATH Configuration

Write-Host "`n--- Configuring PATH ---" -ForegroundColor Yellow

$pathsToAdd = @()

# WiX
$wixPath = if (Test-Path 'C:\Wix311') { 'C:\Wix311' } elseif (Test-Path "$toolsBase\Wix311") { "$toolsBase\Wix311" } else { $null }
if ($wixPath) { $pathsToAdd += $wixPath }

# Update PATH
$pathScope = if ($useUserPath) { 'User' } else { 'Machine' }
$currentPath = [Environment]::GetEnvironmentVariable('PATH', $pathScope)

foreach ($p in $pathsToAdd) {
    if ($currentPath -notlike "*$p*") {
        if ($PSCmdlet.ShouldProcess("$pathScope PATH", "Add $p")) {
            $currentPath = "$currentPath;$p"
            Write-Host "[ADD] $p" -ForegroundColor Cyan
        }
    } else {
        Write-Host "[OK] Already in PATH: $p" -ForegroundColor Green
    }
}

if ($PSCmdlet.ShouldProcess("$pathScope PATH", "Save changes")) {
    [Environment]::SetEnvironmentVariable('PATH', $currentPath, $pathScope)
    # Also update current session
    $env:PATH = "$env:PATH;$($pathsToAdd -join ';')"
}

#endregion

#region Environment Variables

Write-Host "`n--- Configuring Environment Variables ---" -ForegroundColor Yellow

if ($wixPath) {
    $currentWix = [Environment]::GetEnvironmentVariable('WIX', $pathScope)
    if ($currentWix -ne $wixPath) {
        if ($PSCmdlet.ShouldProcess("WIX environment variable", "Set to $wixPath")) {
            [Environment]::SetEnvironmentVariable('WIX', $wixPath, $pathScope)
            $env:WIX = $wixPath
            Write-Host "[SET] WIX = $wixPath" -ForegroundColor Cyan
        }
    } else {
        Write-Host "[OK] WIX already set" -ForegroundColor Green
    }
}

#endregion

#region Verification

Write-Host "`n--- Verification ---" -ForegroundColor Yellow

# Refresh PATH for this session
$env:PATH = [Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('PATH', 'User')

$allGood = $true

# Check WiX
$candle = Get-Command candle.exe -ErrorAction SilentlyContinue
if ($candle) {
    Write-Host "[OK] WiX (candle.exe): found" -ForegroundColor Green
} else {
    Write-Host "[WARN] WiX (candle.exe) not found in PATH - restart terminal and try again" -ForegroundColor Yellow
    $allGood = $false
}

#endregion

Write-Host "`n========================================" -ForegroundColor Cyan
if ($allGood) {
    Write-Host " Setup Complete!" -ForegroundColor Green
} else {
    Write-Host " Setup Complete (restart terminal for PATH changes)" -ForegroundColor Yellow
}
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Restart VS Code (or your terminal) for PATH changes to take effect"
Write-Host "  2. Clone the repository: git clone https://github.com/sillsdev/FieldWorks"
Write-Host "  3. Build: .\build.ps1"
Write-Host ""
Write-Host "For Serena MCP support, see Docs/mcp.md" -ForegroundColor Gray
