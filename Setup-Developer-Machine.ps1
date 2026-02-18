# Setup-Developer-Machine.ps1
# One-stop setup script for FieldWorks development environment on Windows.
# Run this script in an elevated PowerShell prompt to install required tools.
#
# Usage:
#   .\Setup-Developer-Machine.ps1                    # Install everything
#   .\Setup-Developer-Machine.ps1 -SkipVSCheck       # Skip Visual Studio check
#   .\Setup-Developer-Machine.ps1 -InstallerDeps     # Also clone installer helper repos
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
    [switch]$Force,             # Force reinstall even if already present
    [switch]$InstallerDeps      # Clone/link installer helper repositories
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

# WiX Toolset
# This worktree builds installers with WiX v6 via NuGet PackageReference (restored during build).
# No separate WiX 3.x installation (candle/light) is required.
Write-Host "[INFO] WiX Toolset v6 is restored via NuGet during build (no WiX 3 install needed)" -ForegroundColor Gray

# Note: Serena MCP language servers auto-download on first use:
# - C# (csharp): Microsoft.CodeAnalysis.LanguageServer (Roslyn) from Azure NuGet
# - C++ (cpp): clangd from GitHub releases
# No manual installation needed!
Write-Host ""
Write-Host "[INFO] Serena language servers (C# Roslyn, clangd) auto-download on first use" -ForegroundColor Gray

#endregion

#region Installer Dependencies (Optional)

if ($InstallerDeps) {
    Write-Host "`n--- Setting Up Installer Dependencies ---" -ForegroundColor Yellow

    # Detect if we're in a git worktree
    $gitDir = git rev-parse --git-dir 2>$null
    $isWorktree = $gitDir -and (Test-Path "$gitDir/commondir")

    # Determine the shared repos location
    # For worktrees: use parent's parent (e.g., fw-worktrees -> repos)
    # For regular clones: use parent directory
    if ($isWorktree) {
        $repoRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
        Write-Host "[INFO] Git worktree detected. Helper repos will be cloned to: $repoRoot" -ForegroundColor Gray
    } else {
        $repoRoot = Split-Path -Parent $scriptDir
        Write-Host "[INFO] Standard clone. Helper repos will be cloned to subdirectories." -ForegroundColor Gray
    }

    # Helper repo definitions: name, git URL, target subdirectory in FW repo
    $helperRepos = @(
        @{ Name = "FwHelps"; Url = "https://github.com/sillsdev/FwHelps.git"; SubDir = "DistFiles/Helps" },
        @{ Name = "FwLocalizations"; Url = "https://github.com/sillsdev/FwLocalizations.git"; SubDir = "Localizations" }
    )

    foreach ($repo in $helperRepos) {
        $targetPath = Join-Path $scriptDir $repo.SubDir

        # Check if it's already a valid git repo with correct remote, or a junction
        $isJunction = (Test-Path $targetPath) -and ((Get-Item $targetPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)
        $isValidGitRepo = $false
        if ((Test-Path $targetPath) -and (Test-Path "$targetPath/.git")) {
            $remote = git -C $targetPath remote get-url origin 2>$null
            $isValidGitRepo = $remote -and ($remote -like "*$($repo.Name)*" -or $remote -like "*$($repo.Url)*")
        }

        if ($isJunction -or $isValidGitRepo) {
            Write-Host "[OK] $($repo.SubDir) already exists" -ForegroundColor Green
            continue
        }

        # Remove invalid/empty directory if it exists
        if (Test-Path $targetPath) {
            Write-Host "[WARN] Removing invalid $($repo.SubDir) directory..." -ForegroundColor Yellow
            Remove-Item $targetPath -Recurse -Force
        }

        if ($isWorktree) {
            # Clone to shared location and create junction
            $sharedPath = Join-Path $repoRoot $repo.Name

            if (-not (Test-Path $sharedPath)) {
                if ($PSCmdlet.ShouldProcess($repo.Name, "Clone to $sharedPath")) {
                    Write-Host "Cloning $($repo.Name) to shared location..." -ForegroundColor Cyan
                    $oldErrorAction = $ErrorActionPreference
                    $ErrorActionPreference = 'Continue'
                    git clone $repo.Url $sharedPath 2>&1 | Out-Host
                    $cloneExitCode = $LASTEXITCODE
                    $ErrorActionPreference = $oldErrorAction
                    if ($cloneExitCode -ne 0) {
                        Write-Host "[ERROR] Failed to clone $($repo.Name)" -ForegroundColor Red
                        continue
                    }
                }
            } else {
                Write-Host "[OK] $($repo.Name) already cloned at $sharedPath" -ForegroundColor Green
            }

            # Create junction to the shared clone
            if ((Test-Path $sharedPath) -and $PSCmdlet.ShouldProcess($repo.SubDir, "Create junction to $sharedPath")) {
                $parentDir = Split-Path -Parent $targetPath
                if (-not (Test-Path $parentDir)) {
                    New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
                }
                New-Item -ItemType Junction -Path $targetPath -Target $sharedPath -Force | Out-Null
                Write-Host "[OK] Created junction: $($repo.SubDir) -> $sharedPath" -ForegroundColor Green
            }
        } else {
            # Standard clone: clone directly into subdirectory
            if ($PSCmdlet.ShouldProcess($repo.Name, "Clone to $targetPath")) {
                Write-Host "Cloning $($repo.Name)..." -ForegroundColor Cyan
                $parentDir = Split-Path -Parent $targetPath
                if (-not (Test-Path $parentDir)) {
                    New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
                }
                git clone $repo.Url $targetPath 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "[OK] Cloned $($repo.Name) to $targetPath" -ForegroundColor Green
                } else {
                    Write-Host "[ERROR] Failed to clone $($repo.Name)" -ForegroundColor Red
                }
            }
        }
    }

    # Special case: liblcm goes inside Localizations
    $lcmTarget = Join-Path $scriptDir "Localizations/LCM"
    $localizationsPath = Join-Path $scriptDir "Localizations"
    if ((Test-Path $localizationsPath) -and -not (Test-Path $lcmTarget)) {
        if ($isWorktree) {
            $sharedLcm = Join-Path $repoRoot "liblcm"
            if (-not (Test-Path $sharedLcm)) {
                if ($PSCmdlet.ShouldProcess("liblcm", "Clone to $sharedLcm")) {
                    Write-Host "Cloning liblcm to shared location..." -ForegroundColor Cyan
                    git clone https://github.com/sillsdev/liblcm.git $sharedLcm 2>&1 | Out-Null
                }
            }
            if (Test-Path $sharedLcm) {
                if ($PSCmdlet.ShouldProcess("Localizations/LCM", "Create junction to $sharedLcm")) {
                    New-Item -ItemType Junction -Path $lcmTarget -Target $sharedLcm -Force | Out-Null
                    Write-Host "[OK] Created junction: Localizations/LCM -> $sharedLcm" -ForegroundColor Green
                }
            }
        } else {
            if ($PSCmdlet.ShouldProcess("liblcm", "Clone to $lcmTarget")) {
                Write-Host "Cloning liblcm..." -ForegroundColor Cyan
                git clone https://github.com/sillsdev/liblcm.git $lcmTarget 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "[OK] Cloned liblcm to $lcmTarget" -ForegroundColor Green
                }
            }
        }
    } elseif (Test-Path $lcmTarget) {
        Write-Host "[OK] Localizations/LCM already exists" -ForegroundColor Green
    }
}

#endregion

#region PATH Configuration

Write-Host "`n--- Configuring PATH ---" -ForegroundColor Yellow

$pathsToAdd = @()

# VSTest (Visual Studio 2022)
$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vsWhere) {
    $vsInstall = & $vsWhere -latest -property installationPath 2>$null
    if ($vsInstall) {
        $vstestPath = Join-Path $vsInstall 'Common7\IDE\CommonExtensions\Microsoft\TestWindow'
        if (Test-Path (Join-Path $vstestPath 'vstest.console.exe')) {
            $pathsToAdd += $vstestPath
        }
    }
}

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

Write-Host "[INFO] WIX env var is not required for WiX v6 SDK builds" -ForegroundColor Gray

#endregion

#region Verification

Write-Host "`n--- Verification ---" -ForegroundColor Yellow

# Refresh PATH for this session
$env:PATH = [Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('PATH', 'User')

$allGood = $true

# WiX (v6) is acquired via NuGet restore; no PATH verification needed.
Write-Host "[OK] WiX v6: acquired via NuGet restore during build" -ForegroundColor Green

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
Write-Host "To build installers, run: .\Setup-Developer-Machine.ps1 -InstallerDeps" -ForegroundColor Gray
Write-Host "For Serena MCP support, see Docs/mcp.md" -ForegroundColor Gray
