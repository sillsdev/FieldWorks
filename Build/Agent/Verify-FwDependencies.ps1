<#
.SYNOPSIS
    Verifies that all FieldWorks build dependencies are available.

.DESCRIPTION
    Checks for required tools and SDKs needed to build FieldWorks.
    Can be run locally for testing or called from GitHub Actions workflows.

    Expected dependencies (typically pre-installed on windows-latest):
    - Visual Studio 2022 with Desktop & C++ workloads
    - MSBuild
    - .NET Framework 4.8.1 SDK & Targeting Pack
    - Windows SDK
    - WiX Toolset v6 (installer builds restore via NuGet)
    - NuGet CLI
    - .NET SDK 8.x+

.PARAMETER FailOnMissing
    If specified, exits with non-zero code if any required dependency is missing.

.PARAMETER IncludeOptional
    If specified, also checks optional dependencies like clangd for Serena.

.EXAMPLE
    # Quick check
    .\Build\Agent\Verify-FwDependencies.ps1

.EXAMPLE
    # Strict check for CI
    .\Build\Agent\Verify-FwDependencies.ps1 -FailOnMissing

.EXAMPLE
    # Include Serena dependencies
    .\Build\Agent\Verify-FwDependencies.ps1 -IncludeOptional
#>

[CmdletBinding()]
param(
    [switch]$FailOnMissing,
    [switch]$IncludeOptional
)

$ErrorActionPreference = 'Stop'

function Test-Dependency {
    param(
        [string]$Name,
        [scriptblock]$Check,
        [string]$Required = "Required"
    )

    try {
        $result = & $Check
        if ($result) {
            Write-Host "[OK]   $Name" -ForegroundColor Green
            if ($result -is [string] -and $result.Length -gt 0 -and $result.Length -lt 100) {
                Write-Host "       $result" -ForegroundColor DarkGray
            }
            return @{ Name = $Name; Found = $true; Info = $result }
        }
        else {
            throw "Check returned null/false"
        }
    }
    catch {
        $color = if ($Required -eq "Required") { "Red" } else { "Yellow" }
        $status = if ($Required -eq "Required") { "[FAIL]" } else { "[WARN]" }
        Write-Host "$status $Name" -ForegroundColor $color
        Write-Host "       $_" -ForegroundColor DarkGray
        return @{ Name = $Name; Found = $false; Required = ($Required -eq "Required"); Error = $_.ToString() }
    }
}

# ============================================================================
# MAIN SCRIPT
# ============================================================================

Write-Host "=== FieldWorks Dependency Verification ===" -ForegroundColor Cyan
Write-Host ""

$results = @()

# ----------------------------------------------------------------------------
# Required Dependencies
# ----------------------------------------------------------------------------
Write-Host "--- Required Dependencies ---" -ForegroundColor Cyan

# .NET Framework 4.8.1 Targeting Pack
$results += Test-Dependency -Name ".NET Framework 4.8.1 Targeting Pack" -Check {
    $path = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1"
    if (Test-Path $path) { return $path }
    throw "Not found at $path"
}

# Windows SDK
$results += Test-Dependency -Name "Windows SDK" -Check {
    $path = "${env:ProgramFiles(x86)}\Windows Kits\10\Include"
    if (Test-Path $path) {
        $versions = (Get-ChildItem $path -Directory | Sort-Object Name -Descending | Select-Object -First 3).Name -join ', '
        return "Versions: $versions"
    }
    throw "Not found at $path"
}

# Visual Studio / MSBuild
$results += Test-Dependency -Name "Visual Studio 2022" -Check {
    $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vsWhere)) { throw "vswhere.exe not found" }
    $vsPath = & $vsWhere -latest -requires Microsoft.Component.MSBuild -products * -property installationPath
    if (-not $vsPath) { throw "No VS installation with MSBuild found" }
    $version = & $vsWhere -latest -property catalog_productDisplayVersion
    return "Version $version at $vsPath"
}

# MSBuild
$results += Test-Dependency -Name "MSBuild" -Check {
    $msbuild = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($msbuild) {
        $version = (& msbuild.exe -version -nologo 2>$null | Select-Object -Last 1)
        return "Version $version"
    }
    # Try via vswhere
    $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    $vsPath = & $vsWhere -latest -requires Microsoft.Component.MSBuild -products * -property installationPath 2>$null
    if ($vsPath) {
        $msbuildPath = Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'
        if (Test-Path $msbuildPath) {
            return "Found at $msbuildPath (not in PATH)"
        }
    }
    throw "MSBuild not found in PATH or VS installation"
}

# NuGet CLI
$results += Test-Dependency -Name "NuGet CLI" -Check {
    $nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
    if ($nuget) {
        $version = (& nuget.exe help 2>&1 | Select-Object -First 1)
        return $version
    }
    throw "nuget.exe not found in PATH"
}

# .NET SDK
$results += Test-Dependency -Name ".NET SDK" -Check {
    $dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($dotnet) {
        $version = (& dotnet.exe --version 2>&1)
        return "Version $version"
    }
    throw "dotnet.exe not found in PATH"
}

# WiX Toolset (v6)
# Installer projects use WixToolset.Sdk via NuGet restore; no global WiX 3.x install is required.
$results += Test-Dependency -Name "WiX Toolset (v6 via NuGet)" -Required "Optional" -Check {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $wixProj = Join-Path $repoRoot "FLExInstaller\wix6\FieldWorks.Installer.wixproj"
    if (-not (Test-Path $wixProj)) {
        throw "Installer project not found: $wixProj"
    }

    $wixProjText = Get-Content -LiteralPath $wixProj -Raw
    if ($wixProjText -match "WixToolset\\.Sdk") {
        return "Configured in $wixProj (restored during build)"
    }

    throw "WixToolset.Sdk not referenced in $wixProj"
}

# ----------------------------------------------------------------------------
# Optional Dependencies (for Serena MCP)
# ----------------------------------------------------------------------------
if ($IncludeOptional) {
    Write-Host ""
    Write-Host "--- Optional Dependencies (Serena MCP) ---" -ForegroundColor Cyan

    # Python
    $results += Test-Dependency -Name "Python" -Required "Optional" -Check {
        $python = Get-Command python.exe -ErrorAction SilentlyContinue
        if (-not $python) { $python = Get-Command python3.exe -ErrorAction SilentlyContinue }
        if ($python) {
            $version = (& $python.Source --version 2>&1)
            return $version
        }
        throw "python.exe not found in PATH"
    }

    # uv (Python package manager)
    $results += Test-Dependency -Name "uv (Python package manager)" -Required "Optional" -Check {
        $uv = Get-Command uv -ErrorAction SilentlyContinue
        if ($uv) {
            $version = (& uv --version 2>&1)
            return $version
        }
        throw "uv not found - install with: winget install astral-sh.uv"
    }

    # clangd (C++ language server)
    $results += Test-Dependency -Name "clangd (C++ language server)" -Required "Optional" -Check {
        $clangd = Get-Command clangd -ErrorAction SilentlyContinue
        if ($clangd) {
            $version = (& clangd --version 2>&1 | Select-Object -First 1)
            return $version
        }
        throw "clangd not found - Serena will auto-download if needed"
    }

    # Serena project config
    $results += Test-Dependency -Name "Serena project config" -Required "Optional" -Check {
        $configPath = ".serena/project.yml"
        if (Test-Path $configPath) {
            return "Found at $configPath"
        }
        throw "No .serena/project.yml - Serena not configured for this repo"
    }
}

# ----------------------------------------------------------------------------
# Summary
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan

$required = $results | Where-Object { $_.Required -ne $false }
$missing = $required | Where-Object { -not $_.Found }
$optional = $results | Where-Object { $_.Required -eq $false }

$totalRequired = ($required | Measure-Object).Count
$foundRequired = ($required | Where-Object { $_.Found } | Measure-Object).Count

Write-Host "Required: $foundRequired / $totalRequired found"

if ($IncludeOptional) {
    $totalOptional = ($optional | Measure-Object).Count
    $foundOptional = ($optional | Where-Object { $_.Found } | Measure-Object).Count
    Write-Host "Optional: $foundOptional / $totalOptional found"
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing required dependencies:" -ForegroundColor Red
    foreach ($m in $missing) {
        Write-Host "  - $($m.Name)" -ForegroundColor Red
    }

    if ($FailOnMissing) {
        Write-Host ""
        Write-Host "Exiting with error (FailOnMissing specified)" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host ""
    Write-Host "All required dependencies are available!" -ForegroundColor Green
}

return $results
