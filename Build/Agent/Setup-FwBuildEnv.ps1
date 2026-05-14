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
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $scriptDir 'FwBuildEnvironment.psm1') -Force

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

$repoRoot = Resolve-Path "$scriptDir\..\.."

# Set FW_ROOT_CODE_DIR and FW_ROOT_DATA_DIR for DirectoryFinder fallback
# This avoids registry dependencies in CI
$distFiles = Join-Path $repoRoot "DistFiles"
Set-EnvVar -Name "FW_ROOT_CODE_DIR" -Value $distFiles
Set-EnvVar -Name "FW_ROOT_DATA_DIR" -Value $distFiles

$results = @{
	VSPath = $null
	MSBuildPath = $null
	VSTestPath = $null
	Errors = @()
}

# ----------------------------------------------------------------------------
# Find Visual Studio
# ----------------------------------------------------------------------------
Write-Host "--- Locating Visual Studio ---" -ForegroundColor Cyan

$toolchain = Get-VsToolchainInfo -Requires @('Microsoft.Component.MSBuild', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64')
if ($toolchain) {
	$results.VSPath = $toolchain.InstallationPath
	$results.MSBuildPath = $toolchain.MSBuildPath
	$results.VSTestPath = $toolchain.VSTestPath

	if ([string]::IsNullOrWhiteSpace($toolchain.DisplayVersion)) {
		Write-Status "Visual Studio: $($toolchain.InstallationPath)" -Status "OK"
	}
	else {
		Write-Status "Visual Studio $($toolchain.DisplayVersion): $($toolchain.InstallationPath)" -Status "OK"
	}

	# Export installation hints only; build/test scripts still self-initialize via VsDevCmd.
	Set-EnvVar -Name "VSINSTALLDIR" -Value ($toolchain.InstallationPath.TrimEnd('\') + '\')
	if ($toolchain.VcInstallDir) {
		Set-EnvVar -Name "VCINSTALLDIR" -Value ($toolchain.VcInstallDir.TrimEnd('\') + '\')
	}
	if ($toolchain.VCTargetsPath) {
		Set-EnvVar -Name "VCTargetsPath" -Value $toolchain.VCTargetsPath
	}
}
else {
	$vsWhere = Get-VsWherePath
	if ($vsWhere) {
		Write-Status "Visual Studio with MSBuild and C++ tools not found" -Status "FAIL"
		$results.Errors += "Visual Studio with MSBuild and C++ tools not found"
	}
	else {
		Write-Status "vswhere.exe not found" -Status "FAIL"
		$results.Errors += "vswhere.exe not found"
	}
}

# ----------------------------------------------------------------------------
# Find MSBuild
# ----------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Locating MSBuild ---" -ForegroundColor Cyan

if ($results.MSBuildPath) {
	Write-Status "MSBuild: $($results.MSBuildPath)" -Status "OK"
	Add-ToPath -Path (Split-Path -Parent $results.MSBuildPath) | Out-Null
}
else {
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

if ($results.VSTestPath) {
	Add-ToPath -Path (Split-Path -Parent $results.VSTestPath) | Out-Null
	Write-Status "VSTest: $($results.VSTestPath)" -Status "OK"
}
else {
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
	"vstest-path=$($results.VSTestPath)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
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
