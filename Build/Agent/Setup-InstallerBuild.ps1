<#
.SYNOPSIS
    Sets up the environment and validates prerequisites for building FieldWorks installers.

.DESCRIPTION
    This script prepares a development machine for building FieldWorks base and patch installers.
    It validates WiX Toolset installation, clones required helper repositories, downloads base
    build artifacts for patch builds, and sets necessary registry keys.

.PARAMETER ValidateOnly
    Only validate the environment without making changes.

.PARAMETER SetupPatch
    Download and extract base build artifacts needed for patch installer builds.

.PARAMETER BaseRelease
    GitHub release tag for base build artifacts (default: build-1188).

.PARAMETER Force
    Force re-download of base build artifacts even if they exist.

.EXAMPLE
    .\Setup-InstallerBuild.ps1
    # Validates environment and sets up for base installer builds

.EXAMPLE
    .\Setup-InstallerBuild.ps1 -SetupPatch
    # Also downloads base build artifacts for patch installer builds

.EXAMPLE
    .\Setup-InstallerBuild.ps1 -ValidateOnly
    # Only checks prerequisites without making changes

.NOTES
    For full developer machine setup, run Setup-Developer-Machine.ps1 first.
    Installers are built with WiX Toolset v6 (SDK-style .wixproj) restored via NuGet.
#>

[CmdletBinding()]
param(
    [switch]$ValidateOnly,
    [switch]$SetupPatch,
    [string]$BaseRelease = "build-1188",
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FieldWorks Installer Build Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$issues = @()
$warnings = @()

#region WiX Toolset Validation

Write-Host "--- Checking WiX Toolset (v6 via NuGet) ---" -ForegroundColor Yellow

$installerProject = Join-Path $repoRoot "FLExInstaller\FieldWorks.Installer.wixproj"
$bundleProject = Join-Path $repoRoot "FLExInstaller\FieldWorks.Bundle.wixproj"

if (Test-Path $installerProject) {
    Write-Host "[OK] Installer project: $installerProject" -ForegroundColor Green
} else {
    $issues += "Missing installer project: $installerProject"
}

if (Test-Path $bundleProject) {
    Write-Host "[OK] Bundle project: $bundleProject" -ForegroundColor Green
} else {
    $issues += "Missing bundle project: $bundleProject"
}

Write-Host "[INFO] WiX v6 tools are restored during build (no candle.exe/light.exe required)" -ForegroundColor Gray

$heatFromRepoPackages = Join-Path $repoRoot "packages\wixtoolset.heat\6.0.0\tools\net472\x64\heat.exe"
if (Test-Path $heatFromRepoPackages) {
    Write-Host "[OK] Heat.exe (WixToolset.Heat v6) found: $heatFromRepoPackages" -ForegroundColor Green
} else {
    Write-Host "[INFO] Heat.exe (WixToolset.Heat) not found yet; it will be restored on first installer build" -ForegroundColor Gray
}

#endregion

#region Visual Studio / MSBuild Validation

Write-Host "`n--- Checking Visual Studio / MSBuild ---" -ForegroundColor Yellow

$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vsInstall = $null
$vsDevEnvActive = $false

if (Test-Path $vsWhere) {
    $vsInstall = & $vsWhere -latest -property installationPath 2>$null
    if ($vsInstall) {
        $vsVersion = & $vsWhere -latest -property catalog_productDisplayVersion 2>$null
        Write-Host "[OK] Visual Studio 2022: $vsVersion" -ForegroundColor Green

        # Check for MSBuild
        $msbuildPath = Join-Path $vsInstall "MSBuild\Current\Bin\MSBuild.exe"
        if (Test-Path $msbuildPath) {
            Write-Host "[OK] MSBuild found: $msbuildPath" -ForegroundColor Green
        } else {
            $issues += "MSBuild not found in VS installation"
        }

        # Check for VsDevCmd
        $vsDevCmd = Join-Path $vsInstall "Common7\Tools\VsDevCmd.bat"
        $launchVsDevShell = Join-Path $vsInstall "Common7\Tools\Launch-VsDevShell.ps1"
        if ((Test-Path $vsDevCmd) -or (Test-Path $launchVsDevShell)) {
            Write-Host "[OK] VS Developer environment scripts available" -ForegroundColor Green
        }

        # Check if VS Developer environment is active (nmake in PATH)
        $nmake = Get-Command nmake.exe -ErrorAction SilentlyContinue
        if ($nmake) {
            Write-Host "[OK] VS Developer environment active (nmake in PATH)" -ForegroundColor Green
            $vsDevEnvActive = $true
        } else {
            # Check if nmake exists in VS installation
            $nmakePath = Join-Path $vsInstall "VC\Tools\MSVC\*\bin\Hostx64\x64\nmake.exe"
            $nmakeExists = Get-ChildItem -Path $nmakePath -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($nmakeExists) {
                Write-Host "[WARN] VS Developer environment NOT active" -ForegroundColor Yellow
                Write-Host "       nmake.exe exists but is not in PATH" -ForegroundColor Yellow
                Write-Host "       Run builds from VS Developer Command Prompt or use:" -ForegroundColor Yellow
                Write-Host "       cmd /c `"call `"$vsDevCmd`" -arch=amd64 && msbuild ...`"" -ForegroundColor Cyan
                $warnings += "VS Developer environment not active (nmake not in PATH)"
            } else {
                Write-Host "[MISSING] C++ build tools (nmake.exe) not found" -ForegroundColor Red
                Write-Host "          Install 'Desktop development with C++' workload in VS Installer" -ForegroundColor Red
                $issues += "C++ build tools not installed (nmake.exe missing)"
            }
        }
    } else {
        $issues += "Visual Studio 2022 not installed"
    }
} else {
    $issues += "Visual Studio Installer not found"
}

#endregion

#region Helper Repositories

Write-Host "`n--- Checking Helper Repositories ---" -ForegroundColor Yellow

$helperRepos = @(
    @{ Name = "FwHelps"; Path = "DistFiles/Helps"; Required = $true },
    @{ Name = "FwLocalizations"; Path = "Localizations"; Required = $true },
    @{ Name = "liblcm"; Path = "Localizations/LCM"; Required = $true }
)

$missingRepos = @()
foreach ($repo in $helperRepos) {
    $fullPath = Join-Path $repoRoot $repo.Path
    $gitPath = Join-Path $fullPath ".git"
    $isJunction = (Test-Path $fullPath) -and ((Get-Item $fullPath -Force -ErrorAction SilentlyContinue).Attributes -band [IO.FileAttributes]::ReparsePoint)

    if ((Test-Path $gitPath) -or $isJunction) {
        $status = if ($isJunction) { "junction" } else { "git repo" }
        Write-Host "[OK] $($repo.Name): $($repo.Path) ($status)" -ForegroundColor Green
    } else {
        Write-Host "[MISSING] $($repo.Name): $($repo.Path)" -ForegroundColor Red
        $missingRepos += $repo
        if ($repo.Required) {
            $issues += "Missing helper repository: $($repo.Name)"
        }
    }
}

if ($missingRepos.Count -gt 0 -and -not $ValidateOnly) {
    Write-Host "`n[INFO] Missing repositories can be cloned with:" -ForegroundColor Cyan
    Write-Host "       .\Setup-Developer-Machine.ps1 -InstallerDeps" -ForegroundColor Cyan
}

#endregion

#region Registry Key for WiX Temp Files

Write-Host "`n--- Checking WiX Registry Configuration ---" -ForegroundColor Yellow

$regPaths = @(
    "HKLM:\SOFTWARE\Microsoft\.NETFramework\AppContext",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\.NETFramework\AppContext"
)
$valueName = "Switch.System.DisableTempFileCollectionDirectoryFeature"

$regKeySet = $true
foreach ($path in $regPaths) {
    if (Test-Path $path) {
        $value = Get-ItemProperty -Path $path -Name $valueName -ErrorAction SilentlyContinue
        if ($value -and $value.$valueName -eq "true") {
            Write-Host "[OK] Registry key set: $path" -ForegroundColor Green
        } else {
            $regKeySet = $false
        }
    } else {
        $regKeySet = $false
    }
}

if (-not $regKeySet) {
    if ($ValidateOnly) {
        $warnings += "WiX temp file registry key not set (may cause build errors)"
        Write-Host "[WARN] WiX temp file registry key not set" -ForegroundColor Yellow
        Write-Host "       This may cause 'DisableTempFileCollectionDirectoryFeature' errors" -ForegroundColor Yellow
        Write-Host "       Run this command in an elevated (Admin) PowerShell to fix:" -ForegroundColor Yellow
        Write-Host '       $paths = @("HKLM:\SOFTWARE\Microsoft\.NETFramework\AppContext", "HKLM:\SOFTWARE\WOW6432Node\Microsoft\.NETFramework\AppContext"); foreach ($path in $paths) { if (-not (Test-Path $path)) { New-Item -Path $path -Force | Out-Null }; New-ItemProperty -Path $path -Name "Switch.System.DisableTempFileCollectionDirectoryFeature" -Value "true" -Type String -Force | Out-Null }; Write-Host "Registry keys set successfully"' -ForegroundColor Cyan
    } else {
        Write-Host "[INFO] Setting WiX temp file registry key (requires admin)..." -ForegroundColor Cyan
        $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        if ($isAdmin) {
            foreach ($path in $regPaths) {
                if (-not (Test-Path $path)) {
                    New-Item -Path $path -Force | Out-Null
                }
                New-ItemProperty -Path $path -Name $valueName -Value "true" -Type String -Force | Out-Null
            }
            Write-Host "[OK] Registry keys set successfully" -ForegroundColor Green
        } else {
            $warnings += "Run as Administrator to set WiX registry keys"
            Write-Host "[WARN] Cannot set registry keys without Administrator privileges" -ForegroundColor Yellow
        }
    }
}

#endregion

#region Patch Build Setup

if ($SetupPatch) {
    Write-Host "`n--- Setting Up Patch Build Prerequisites ---" -ForegroundColor Yellow

    $buildDir = Join-Path $repoRoot "BuildDir"
    $procRunnerDir = Join-Path $repoRoot "FLExInstaller\Shared\ProcRunner\ProcRunner\bin\Release\net48"
    $artifactsDir = Join-Path $repoRoot "base-artifacts"

    # Check if artifacts already exist
    $buildDirExists = (Test-Path $buildDir) -and (Test-Path "$buildDir\version")
    $procRunnerExists = Test-Path "$procRunnerDir\ProcRunner.exe"

    if ($buildDirExists -and $procRunnerExists -and -not $Force) {
        Write-Host "[OK] Base build artifacts already present" -ForegroundColor Green
        Write-Host "     BuildDir: $buildDir" -ForegroundColor Gray
        Write-Host "     ProcRunner: $procRunnerDir" -ForegroundColor Gray
    } else {
        if ($ValidateOnly) {
            $warnings += "Base build artifacts not found (needed for patch builds)"
            Write-Host "[WARN] Base build artifacts not found" -ForegroundColor Yellow
        } else {
            Write-Host "[INFO] Downloading base build artifacts from $BaseRelease..." -ForegroundColor Cyan

            # Check for gh CLI
            $gh = Get-Command gh -ErrorAction SilentlyContinue
            if (-not $gh) {
                Write-Host "[ERROR] GitHub CLI (gh) not found. Install from https://cli.github.com/" -ForegroundColor Red
                $issues += "GitHub CLI not installed (required for downloading artifacts)"
            } else {
                # Create temp directory for downloads
                if (-not (Test-Path $artifactsDir)) {
                    New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
                }

                try {
                    # Download BuildDir.zip
                    Write-Host "  Downloading BuildDir.zip..." -ForegroundColor Gray
                    gh release download $BaseRelease --repo sillsdev/FieldWorks --pattern "BuildDir.zip" --dir $artifactsDir --clobber

                    # Download ProcRunner.zip
                    Write-Host "  Downloading ProcRunner.zip..." -ForegroundColor Gray
                    gh release download $BaseRelease --repo sillsdev/FieldWorks --pattern "ProcRunner.zip" --dir $artifactsDir --clobber

                    # Extract BuildDir.zip
                    $buildDirZip = Join-Path $artifactsDir "BuildDir.zip"
                    if (Test-Path $buildDirZip) {
                        Write-Host "  Extracting BuildDir.zip..." -ForegroundColor Gray
                        if (Test-Path $buildDir) { Remove-Item $buildDir -Recurse -Force }
                        Expand-Archive -Path $buildDirZip -DestinationPath $buildDir -Force
                        Write-Host "[OK] BuildDir extracted to $buildDir" -ForegroundColor Green
                    }

                    # Extract ProcRunner.zip
                    $procRunnerZip = Join-Path $artifactsDir "ProcRunner.zip"
                    if (Test-Path $procRunnerZip) {
                        Write-Host "  Extracting ProcRunner.zip..." -ForegroundColor Gray
                        if (-not (Test-Path $procRunnerDir)) {
                            New-Item -ItemType Directory -Path $procRunnerDir -Force | Out-Null
                        }
                        Expand-Archive -Path $procRunnerZip -DestinationPath $procRunnerDir -Force
                        Write-Host "[OK] ProcRunner extracted to $procRunnerDir" -ForegroundColor Green
                    }
                } catch {
                    Write-Host "[ERROR] Failed to download/extract artifacts: $_" -ForegroundColor Red
                    $issues += "Failed to download base build artifacts"
                }
            }
        }
    }
}

#endregion

#region Summary

Write-Host "`n========================================" -ForegroundColor Cyan

if ($issues.Count -eq 0) {
    Write-Host " Environment Ready!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan

    if ($warnings.Count -gt 0) {
        Write-Host "`nWarnings:" -ForegroundColor Yellow
        foreach ($w in $warnings) {
            Write-Host "  - $w" -ForegroundColor Yellow
        }
    }

    Write-Host "`nTo build installers:" -ForegroundColor White

    if ($vsDevEnvActive) {
        # VS Developer environment is active, show simple commands
        Write-Host ""
        Write-Host "  # Restore packages" -ForegroundColor Gray
        Write-Host "  msbuild Build/Orchestrator.proj /t:RestorePackages /p:Configuration=Debug /p:Platform=x64" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  # Build base installer" -ForegroundColor Gray
        Write-Host "  msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m /v:n" -ForegroundColor Cyan
        Write-Host ""

        if ($SetupPatch) {
            Write-Host "  # Build patch installer" -ForegroundColor Gray
            Write-Host "  msbuild Build/Orchestrator.proj /t:BuildPatchInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m /v:n" -ForegroundColor Cyan
            Write-Host ""
        }
    } else {
        # Need to wrap commands with VsDevCmd
        Write-Host ""
        Write-Host "  # Option 1: Open VS Developer Command Prompt and run commands there" -ForegroundColor Gray
        Write-Host "  # Option 2: Use these one-liner commands from any PowerShell:" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  # Restore packages" -ForegroundColor Gray
        Write-Host '  cmd /c "call ""C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"" -arch=amd64 >nul && msbuild Build/Orchestrator.proj /t:RestorePackages /p:Configuration=Debug /p:Platform=x64"' -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  # Build base installer" -ForegroundColor Gray
        Write-Host '  cmd /c "call ""C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"" -arch=amd64 >nul && msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m /v:n"' -ForegroundColor Cyan
        Write-Host ""

        if ($SetupPatch) {
            Write-Host "  # Build patch installer" -ForegroundColor Gray
            Write-Host '  cmd /c "call ""C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"" -arch=amd64 >nul && msbuild Build/Orchestrator.proj /t:BuildPatchInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m /v:n"' -ForegroundColor Cyan
            Write-Host ""
        }
    }

    if (-not $SetupPatch) {
        Write-Host "  # For patch builds, run: .\Build\Agent\Setup-InstallerBuild.ps1 -SetupPatch" -ForegroundColor Gray
        Write-Host ""
    }

    Write-Host "Output location: BuildDir/" -ForegroundColor Gray
    exit 0
} else {
    Write-Host " Setup Incomplete" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan

    Write-Host "`nIssues found:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  - $issue" -ForegroundColor Red
    }

    if ($warnings.Count -gt 0) {
        Write-Host "`nWarnings:" -ForegroundColor Yellow
        foreach ($w in $warnings) {
            Write-Host "  - $w" -ForegroundColor Yellow
        }
    }

    Write-Host "`nTo fix:" -ForegroundColor White
    Write-Host "  1. Run .\Setup-Developer-Machine.ps1 -InstallerDeps" -ForegroundColor Cyan
    Write-Host "  2. Re-run this script" -ForegroundColor Cyan
    exit 1
}

#endregion
