<#
.SYNOPSIS
    Configures the FieldWorks build environment on Windows.

.DESCRIPTION
    Sets up environment variables and PATH entries needed for FieldWorks builds.
    Can be run locally for testing or called from GitHub Actions workflows.

    This script is idempotent - safe to run multiple times.

.PARAMETER OutputGitHubEnv
    If specified, outputs environment variables to GITHUB_ENV and GITHUB_PATH
    for use in GitHub Actions. Otherwise, sets them in the current process.

.PARAMETER Verify
    If specified, runs verification checks and exits with non-zero on failure.

.EXAMPLE
    # Local testing - just configure current session
    .\Build\Agent\Setup-FwBuildEnv.ps1

.EXAMPLE
    # GitHub Actions - output to GITHUB_ENV
    .\Build\Agent\Setup-FwBuildEnv.ps1 -OutputGitHubEnv

.EXAMPLE
    # Verify all dependencies are available
    .\Build\Agent\Setup-FwBuildEnv.ps1 -Verify
#>

[CmdletBinding()]
param(
    [switch]$OutputGitHubEnv,
    [switch]$Verify
)

$ErrorActionPreference = 'Stop'

function Write-Status {
    param([string]$Message, [string]$Status = "INFO", [string]$Color = "White")
    $prefix = switch ($Status) {
        "OK"   { "[OK]   "; $Color = "Green" }
        "FAIL" { "[FAIL] "; $Color = "Red" }
        "WARN" { "[WARN] "; $Color = "Yellow" }
        "SKIP" { "[SKIP] "; $Color = "DarkGray" }
        default { "[INFO] " }
    }
    Write-Host "$prefix$Message" -ForegroundColor $Color
}

function Set-EnvVar {
    param([string]$Name, [string]$Value)

    if ($OutputGitHubEnv -and $env:GITHUB_ENV) {
        # GitHub Actions format
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
        Write-Status "Set $Name (GITHUB_ENV)"
    }
    else {
        # Local session
        [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
        Write-Status "Set $Name = $Value"
    }
}

function Add-ToPath {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Status "Path does not exist: $Path" -Status "WARN"
        return $false
    }

    if ($OutputGitHubEnv -and $env:GITHUB_PATH) {
        $Path | Out-File -FilePath $env:GITHUB_PATH -Encoding utf8 -Append
        Write-Status "Added to PATH (GITHUB_PATH): $Path"
    }
    else {
        $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'Process')
        if ($currentPath -notlike "*$Path*") {
            [Environment]::SetEnvironmentVariable('PATH', "$Path;$currentPath", 'Process')
            Write-Status "Added to PATH: $Path"
        }
        else {
            Write-Status "Already in PATH: $Path" -Status "SKIP"
        }
    }
    return $true
}

# ============================================================================
# MAIN SCRIPT
# ============================================================================

Write-Host "=== FieldWorks Build Environment Setup ===" -ForegroundColor Cyan
Write-Host "OutputGitHubEnv: $OutputGitHubEnv"
Write-Host "Verify: $Verify"
Write-Host ""

$results = @{
    VSPath = $null
    MSBuildPath = $null
    Errors = @()
}

# ----------------------------------------------------------------------------
# Find Visual Studio
# ----------------------------------------------------------------------------
Write-Host "--- Locating Visual Studio ---" -ForegroundColor Cyan

$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vsWhere) {
    $vsPath = & $vsWhere -latest -requires Microsoft.Component.MSBuild -products * -property installationPath
    if ($vsPath) {
        Write-Status "Visual Studio: $vsPath" -Status "OK"
        $results.VSPath = $vsPath

        # Set VS environment variables
        Set-EnvVar -Name "VSINSTALLDIR" -Value "$vsPath\"
        Set-EnvVar -Name "VCINSTALLDIR" -Value "$vsPath\VC\"

        # VCTargetsPath for C++ builds
        $vcTargets = Join-Path $vsPath 'MSBuild\Microsoft\VC\v170'
        if (Test-Path $vcTargets) {
            Set-EnvVar -Name "VCTargetsPath" -Value $vcTargets
        }
    }
    else {
        Write-Status "Visual Studio not found via vswhere" -Status "FAIL"
        $results.Errors += "Visual Studio not found"
    }
}
else {
    Write-Status "vswhere.exe not found at: $vsWhere" -Status "FAIL"
    $results.Errors += "vswhere.exe not found"
}

# ----------------------------------------------------------------------------
# Find MSBuild
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Locating MSBuild ---" -ForegroundColor Cyan

$msbuildCandidates = @()
if ($results.VSPath) {
    $msbuildCandidates += Join-Path $results.VSPath 'MSBuild\Current\Bin\MSBuild.exe'
    $msbuildCandidates += Join-Path $results.VSPath 'MSBuild\Current\Bin\amd64\MSBuild.exe'
}
$msbuildCandidates += "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
$msbuildCandidates += "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
$msbuildCandidates += "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
$msbuildCandidates += "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

foreach ($candidate in $msbuildCandidates) {
    if (Test-Path $candidate) {
        $results.MSBuildPath = $candidate
        Write-Status "MSBuild: $candidate" -Status "OK"
        break
    }
}

if (-not $results.MSBuildPath) {
    Write-Status "MSBuild not found" -Status "FAIL"
    $results.Errors += "MSBuild not found"
}

# ----------------------------------------------------------------------------
# Add NETFX tools to PATH
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Configuring PATH ---" -ForegroundColor Cyan

$netfxPaths = @(
    "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools",
    "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools",
    "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools"
)

$foundNetfx = $false
foreach ($p in $netfxPaths) {
    if (Test-Path $p) {
        Add-ToPath -Path $p | Out-Null
        $foundNetfx = $true
        break
    }
}
if (-not $foundNetfx) {
    Write-Status "NETFX tools not found (sn.exe may not work)" -Status "WARN"
}

# ----------------------------------------------------------------------------
# Find VSTest
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Locating VSTest ---" -ForegroundColor Cyan

$vstestPath = $null
if ($results.VSPath) {
    $potentialPath = Join-Path $results.VSPath 'Common7\IDE\CommonExtensions\Microsoft\TestWindow'
    if (Test-Path (Join-Path $potentialPath 'vstest.console.exe')) {
        $vstestPath = $potentialPath
        Add-ToPath -Path $vstestPath | Out-Null
    }
}

if (-not $vstestPath) {
    Write-Status "vstest.console.exe not found" -Status "WARN"
}

# ----------------------------------------------------------------------------
# Output results
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Cyan

# Output key paths for GitHub Actions
if ($OutputGitHubEnv -and $env:GITHUB_OUTPUT) {
    "msbuild-path=$($results.MSBuildPath)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "vs-install-path=$($results.VSPath)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
}

# Return results object for programmatic use
if ($results.Errors.Count -gt 0) {
    Write-Host ""
    Write-Status "Setup completed with errors:" -Status "FAIL"
    foreach ($err in $results.Errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    if ($Verify) {
        exit 1
    }
}
else {
    Write-Status "All environment configuration successful" -Status "OK"
}

return $results
