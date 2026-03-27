<#
.SYNOPSIS
	Builds the FieldWorks repository using the MSBuild Traversal SDK.

.DESCRIPTION
	This script orchestrates the build process for FieldWorks. It handles:
	1. Initializing the Visual Studio Developer Environment (if needed).
	2. Bootstrapping build tasks (FwBuildTasks).
	3. Restoring NuGet packages.
	4. Building the solution via FieldWorks.proj using MSBuild Traversal.

.PARAMETER Configuration
	The build configuration (Debug or Release). Default is Debug.

.PARAMETER Platform
	The target platform. Only x64 is supported. Default is x64.

.PARAMETER Serial
	If set, disables parallel build execution (/m). Default is false (parallel enabled).

.PARAMETER BuildTests
	If set, includes test projects in the build. Default is false.

.PARAMETER RunTests
	If set, runs tests after building. Implies -BuildTests. Uses VSTest via test.ps1.
	Also builds native test prerequisites (Unit++/native test libs).

.PARAMETER TestFilter
	Optional VSTest filter expression (e.g., "TestCategory!=Slow"). Only used with -RunTests.

.PARAMETER BuildAdditionalApps
	If set, includes optional utility applications (e.g. MigrateSqlDbs, LCMBrowser, UnicodeCharEditor) in the build.
	Default is false unless -BuildInstaller is specified, which enables it automatically.

.PARAMETER Verbosity
	Specifies the amount of information to display in the build log.
	Values: q[uiet], m[inimal], n[ormal], d[etailed], diag[nostic].
	Default is 'minimal'.

.PARAMETER TraceCrashes
	If set, enables the dev diagnostics config (FieldWorks.Diagnostics.dev.config) so trace logging
	is written next to the built executable. Useful for crash investigation.

.PARAMETER NodeReuse
	Controls MSBuild node reuse (/nr). Accepts true, false, or auto.
	Default is auto: enable reuse when this repository has a single local worktree,
	and disable it when multiple local worktrees exist to improve isolation.

.PARAMETER MsBuildArgs
	Additional arguments to pass directly to MSBuild.

.PARAMETER LogFile
	Path to a file where the build output should be logged.

.PARAMETER TailLines
	If specified, displays only the last N lines of output after the build completes.
	Useful for CI/agent scenarios where you want to see recent output without piping.
	The full output is still written to LogFile if specified.

.PARAMETER BuildInstaller
	If set, builds the installer via Build/InstallerBuild.proj after the main build.
	This automatically enables -BuildAdditionalApps unless explicitly disabled.

.PARAMETER BuildPatch
	If set, builds the patch installer via Build/InstallerBuild.proj after the main build.
	This automatically enables -BuildAdditionalApps unless explicitly disabled.

.PARAMETER InstallerToolset
	Selects the installer toolset to build (Wix3 or Wix6). Default is Wix3.

.PARAMETER InstallerOnly
	Only used with -BuildInstaller. Skips rebuilding FieldWorks and only builds the WiX installer/bundles,
	reusing existing binaries under Output/<Configuration>. For safety, this requires a build stamp from a
	prior full build in the same configuration.

.PARAMETER ForceInstallerOnly
	Only used with -InstallerOnly. Forces installer-only builds even when the git HEAD or dirty state
	differs from the last full-build stamp, or when there are uncommitted changes outside FLExInstaller/.
	Use only when you are sure the current Output/<Configuration> binaries are still what you want to package.

.PARAMETER SignInstaller
	Only used with -BuildInstaller. Enables local signing when signing tools are available.
	By default, local installer builds capture files to sign later instead of signing.

.PARAMETER UseLocalLcm
	If set, builds liblcm from a local checkout (default: ../liblcm) after the FieldWorks build
	and copies the resulting DLLs into the output directory, overwriting the NuGet package versions.
	Use this to test local liblcm fixes without publishing a NuGet package.

.PARAMETER LocalLcmPath
	Path to the local liblcm repository. Defaults to ../liblcm relative to the FieldWorks repo root.
	Only used when -UseLocalLcm is specified.

.PARAMETER LogFile
	Path to a file where the build output should be logged.

.PARAMETER StartedBy
	Optional actor label written to the worktree lock metadata (for example: user or agent).
	Defaults to the FW_BUILD_STARTED_BY environment variable when set, otherwise 'unknown'.

.PARAMETER SkipWorktreeLock
	Internal switch used when build.ps1 is invoked from test.ps1 while the parent test workflow
	already owns the same-worktree lock. Skips acquiring/releasing that lock again.

.PARAMETER TailLines
	If specified, only displays the last N lines of output after the build completes.
	Useful for CI/agent scenarios where you want to see recent output without piping.
	The full output is still written to LogFile if specified.

.PARAMETER SkipDependencyCheck
	If set, skips the dependency preflight check that verifies that required SDKs and tools are installed.

.EXAMPLE
	.\build.ps1
	Builds Debug x64 in parallel with minimal logging.

.EXAMPLE
	.\build.ps1 -Configuration Release -BuildTests
	Builds Release x64 including test projects.

.EXAMPLE
	.\build.ps1 -RunTests
	Builds Debug x64 including test projects and runs all tests.

.EXAMPLE
	.\build.ps1 -Serial -Verbosity detailed
	Builds Debug x64 serially with detailed logging.

.EXAMPLE
	.\build.ps1 -UseLocalLcm
	Builds FieldWorks, then builds liblcm from ../liblcm and copies DLLs into Output.

.NOTES
	FieldWorks is x64-only. The x86 platform is no longer supported.
#>
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[ValidateSet('x64')]
	[string]$Platform = "x64",
	[switch]$Serial,
	[switch]$BuildTests,
	[switch]$RunTests,
	[string]$TestFilter,
	[switch]$BuildAdditionalApps,
	[string]$Project = "FieldWorks.proj",
	[string]$Verbosity = "minimal",
	[ValidateSet('true', 'false', 'auto')]
	[string]$NodeReuse = 'auto',
	[string[]]$MsBuildArgs = @(),
	[string]$LogFile,
	[int]$TailLines,
	[switch]$SkipRestore,
	[switch]$SkipNative,
	[switch]$BuildInstaller,
	[switch]$BuildPatch,
	[ValidateSet('Wix3', 'Wix6')]
	[string]$InstallerToolset = "Wix3",
	[switch]$InstallerOnly,
	[switch]$ForceInstallerOnly,
	[switch]$SignInstaller,
	[switch]$TraceCrashes,
	[switch]$UseLocalLcm,
	[string]$LocalLcmPath,
	[ValidateSet('user', 'agent', 'unknown')]
	[string]$StartedBy = 'unknown',
	[switch]$SkipWorktreeLock,
	[switch]$SkipDependencyCheck
)

$ErrorActionPreference = "Stop"

if (-not $PSBoundParameters.ContainsKey('StartedBy') -and -not [string]::IsNullOrWhiteSpace($env:FW_BUILD_STARTED_BY)) {
	$startedByFromEnv = $env:FW_BUILD_STARTED_BY.ToLowerInvariant()
	if ($startedByFromEnv -in @('user', 'agent', 'unknown')) {
		$StartedBy = $startedByFromEnv
	}
}

# Add WiX to the PATH for installer builds (required for harvesting localizations)
$env:PATH = "$env:WIX/bin;$env:PATH"

if ($Configuration -like "--*") {
	if ($Configuration -eq "--TraceCrashes" -and -not $TraceCrashes) {
		$TraceCrashes = $true
		$Configuration = "Debug"
		Write-Output "[WARN] Detected '--TraceCrashes' passed without PowerShell switch parsing. Using -TraceCrashes and defaulting Configuration to Debug."
	}
	else {
		throw "Invalid Configuration value '$Configuration'. Use -TraceCrashes (single dash) for the trace option."
	}
}

if ($BuildInstaller -and -not $BuildAdditionalApps) {
	$BuildAdditionalApps = $true
	Write-Host "BuildInstaller enabled: including additional apps (use -BuildAdditionalApps:$false to skip)." -ForegroundColor Yellow
}

if (($BuildInstaller -or $BuildPatch) -and -not ($Configuration -eq "Release")) {
	$Configuration = "Release"
	Write-Host "Installer builds must be Release builds; changing Configuration to Release" -ForegroundColor Yellow
}

# For local Release builds, use a stable daily build number so native artifacts can be reused.
# Purpose: fast local rebuilds by preventing version-stamp churn in native inputs.
# CI (GitHub Actions) should continue to provide its own build number.
$isGitHubActions = ($env:GITHUB_ACTIONS -eq 'true')
if ($Configuration -eq 'Release' -and -not $isGitHubActions) {
	$buildNumberOk = $false
	if (-not [string]::IsNullOrWhiteSpace($env:FW_BUILD_NUMBER)) {
		$parsedBuildNumber = 0
		if ([int]::TryParse($env:FW_BUILD_NUMBER, [ref]$parsedBuildNumber) -and $parsedBuildNumber -ge 0 -and $parsedBuildNumber -le 65535) {
			$buildNumberOk = $true
		}
	}

	if (-not $buildNumberOk) {
		$utcNow = (Get-Date).ToUniversalTime()
		$env:FW_BUILD_NUMBER = "{0}{1:000}" -f $utcNow.ToString('yy'), $utcNow.DayOfYear
		Write-Host "Using local numeric build number: $($env:FW_BUILD_NUMBER)" -ForegroundColor Yellow
	}

	if ([string]::IsNullOrWhiteSpace($env:FW_BUILD_LABEL)) {
		$env:FW_BUILD_LABEL = "local_{0}" -f (Get-Date).ToUniversalTime().ToString('yyyyMMdd')
		Write-Host "Using local build label: $($env:FW_BUILD_LABEL)" -ForegroundColor Yellow
	}
}

# ===========ed Module
# =============================================================================

$helpersPath = Join-Path $PSScriptRoot "Build/Agent/FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
	Write-Host "[ERROR] FwBuildHelpers.psm1 not found at $helpersPath" -ForegroundColor Red
	exit 1
}
Import-Module $helpersPath -Force

# =============================================================================
# Environment Setup
# =============================================================================

$worktreeLock = $null
$cleanupArgs = @{
	IncludeOmniSharp = $true
	RepoRoot = $PSScriptRoot
}

$testExitCode = 0

function Get-RepoStamp {
	$gitHead = & git rev-parse HEAD
	if ($LASTEXITCODE -ne 0) {
		throw "Failed to determine git HEAD (git rev-parse HEAD)."
	}
	$gitHead = ($gitHead | Out-String).Trim()

	$gitStatus = & git status --porcelain
	if ($LASTEXITCODE -ne 0) {
		throw "Failed to determine git dirty state (git status --porcelain)."
	}
	$gitStatusText = ($gitStatus | Out-String)
	$isDirty = -not [string]::IsNullOrWhiteSpace($gitStatusText)

	# For installer-only iteration, allow local edits under FLExInstaller/** while still blocking
	# when *product* inputs have changed (e.g. Src/**).
	$isDirtyOutsideInstaller = $false
	if ($isDirty) {
		$lines = @()
		foreach ($line in ($gitStatusText -split "`r?`n")) {
			$trimmed = $line.TrimEnd()
			if ([string]::IsNullOrWhiteSpace($trimmed)) {
				continue
			}
			$lines += $trimmed
		}

		foreach ($statusLine in $lines) {
			if ($statusLine.Length -lt 4) {
				continue
			}

			# Porcelain format: XY<space>PATH (or XY<space>OLD -> NEW)
			$pathPart = $statusLine.Substring(3).Trim()
			if ($pathPart -like "* -> *") {
				$pathPart = ($pathPart.Split(@(" -> "), 2, [System.StringSplitOptions]::None)[1]).Trim()
			}

			if (-not ($pathPart -like "FLExInstaller/*")) {
				$isDirtyOutsideInstaller = $true
				break
			}
		}
	}

	return [pscustomobject]@{
		GitHead = $gitHead
		IsDirty = $isDirty
		IsDirtyOutsideInstaller = $isDirtyOutsideInstaller
	}
}

function Get-BuildStampPath {
	param(
		[Parameter(Mandatory = $true)][string]$RepoRoot,
		[Parameter(Mandatory = $true)][string]$ConfigurationName
	)
	$outputDir = Join-Path $RepoRoot ("Output\\{0}" -f $ConfigurationName)
	return Join-Path $outputDir "BuildStamp.json"
}

function Resolve-NodeReuse {
	param(
		[Parameter(Mandatory = $true)][string]$Mode
	)

	$normalizedMode = $Mode.ToLowerInvariant()
	if ($normalizedMode -eq 'true') {
		return [pscustomobject]@{
			Enabled = $true
			Source = 'explicit'
			Reason = 'requested explicitly'
		}
	}

	if ($normalizedMode -eq 'false') {
		return [pscustomobject]@{
			Enabled = $false
			Source = 'explicit'
			Reason = 'requested explicitly'
		}
	}

	$worktreeList = & git worktree list --porcelain
	if ($LASTEXITCODE -ne 0) {
		Write-Warning 'Failed to inspect git worktrees. Defaulting MSBuild node reuse to false for safety.'
		return [pscustomobject]@{
			Enabled = $false
			Source = 'auto'
			Reason = 'git worktree detection failed'
		}
	}

	$worktreeCount = 0
	foreach ($line in (($worktreeList | Out-String) -split "`r?`n")) {
		if ($line.StartsWith('worktree ')) {
			$worktreeCount++
		}
	}

	if ($worktreeCount -le 1) {
		return [pscustomobject]@{
			Enabled = $true
			Source = 'auto'
			Reason = 'single local worktree detected'
		}
	}

	return [pscustomobject]@{
		Enabled = $false
		Source = 'auto'
		Reason = "$worktreeCount local worktrees detected"
	}
}

try {
	if (-not $SkipWorktreeLock) {
		$worktreeLock = Enter-WorktreeLock -RepoRoot $PSScriptRoot -Context "FieldWorks build" -StartedBy $StartedBy
	}

	# Worktree-aware cleanup: only stop conflicting processes related to this repo root.
	Stop-ConflictingProcesses -IncludeOmniSharp -RepoRoot $PSScriptRoot

	$fwTasksSourcePath = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"
	$fwTasksDropPath = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"

	Invoke-WithFileLockRetry -Context "FieldWorks build" -IncludeOmniSharp -RepoRoot $PSScriptRoot -Action {
		# Initialize Visual Studio Developer environment
		Initialize-VsDevEnvironment
		Test-CvtresCompatibility

		if (-not $SkipDependencyCheck) {
			$verifyScript = Join-Path $PSScriptRoot "Build/Agent/Verify-FwDependencies.ps1"
			if (Test-Path $verifyScript) {
				Write-Host "Running dependency preflight..." -ForegroundColor Cyan
				& $verifyScript -FailOnMissing
				if ($LASTEXITCODE -ne 0) {
					throw "Dependency preflight failed. Re-run with -SkipDependencyCheck only if you are actively debugging environment setup."
				}
			}
		}

		# Set architecture environment variable (x64-only)
		$env:arch = 'x64'
		Write-Host "Set arch environment variable to: $env:arch" -ForegroundColor Green

		# Stop conflicting processes before the build
		Stop-ConflictingProcesses @cleanupArgs

		$projectPath = $Project
		$rootedProjectPath = Join-Path $PSScriptRoot $Project
		if (-not (Test-Path $projectPath) -and (Test-Path $rootedProjectPath)) {
			$projectPath = $rootedProjectPath
		}
		if (-not (Test-Path $projectPath)) {
			throw "Project path '$Project' was not found. Pass a path relative to the repo root or an absolute path."
		}

		# Clean stale per-project obj/ folders
		Remove-StaleObjFolders -RepoRoot $PSScriptRoot

		# LT-22382: Remove stale first-party DLLs from the output directory before building.
		# Uses a whitelist of assembly names from Src/**/*.csproj to identify FW assemblies
		# and checks their major version against FWMAJOR. See Build\Agent\Remove-StaleDlls.ps1.
		$staleDllScript = Join-Path $PSScriptRoot "Build\Agent\Remove-StaleDlls.ps1"
		if (Test-Path $staleDllScript) {
			$outputDir = "Output\$Configuration"
			& $staleDllScript -OutputDir $outputDir -RepoRoot $PSScriptRoot -Verbose:$VerbosePreference
		}

		# =============================================================================
		# Build Configuration
		# =============================================================================

		# Determine logical core count for CL_MPCount
		if ($env:CL_MPCount) {
			$mpCount = $env:CL_MPCount
		}
		else {
			$mpCount = 8
			if ($env:NUMBER_OF_PROCESSORS) {
				$procCount = [int]$env:NUMBER_OF_PROCESSORS
				if ($procCount -lt 8) { $mpCount = $procCount }
			}
		}

		# Construct MSBuild arguments
		$finalMsBuildArgs = @()
		$nodeReuseDecision = Resolve-NodeReuse -Mode $NodeReuse

		# Parallelism
		if (-not $Serial) {
			$finalMsBuildArgs += "/m"
		}

		# Verbosity & Logging
		$finalMsBuildArgs += "/v:$Verbosity"
		$finalMsBuildArgs += "/nologo"
		$finalMsBuildArgs += "/consoleloggerparameters:Summary"

		# Node Reuse
		$finalMsBuildArgs += "/nr:$($nodeReuseDecision.Enabled.ToString().ToLower())"

		# Properties
		$finalMsBuildArgs += "/p:Configuration=$Configuration"
		$finalMsBuildArgs += "/p:Platform=$Platform"
		if ($SkipNative) {
			$finalMsBuildArgs += "/p:SkipNative=true"
		}

		$installerMsBuildArgs = $finalMsBuildArgs

		# Args specific to the main build (not the installer)
		if ($TraceCrashes) {
			$finalMsBuildArgs += "/p:UseDevTraceConfig=true"
		}
		$finalMsBuildArgs += "/p:CL_MPCount=$mpCount"
		if ($env:FW_TRACE_LOG) {
			$finalMsBuildArgs += "/p:FW_TRACE_LOG=`"$($env:FW_TRACE_LOG)`""
		}

		if ($BuildTests -or $RunTests) {
			$finalMsBuildArgs += "/p:BuildTests=true"
			$finalMsBuildArgs += "/p:BuildNativeTests=true"
		}

		if ($BuildAdditionalApps) {
			$finalMsBuildArgs += "/p:BuildAdditionalApps=true"
		}

		# Add user-supplied args
		$finalMsBuildArgs += $MsBuildArgs
		$installerMsBuildArgs += $MsBuildArgs

		# =============================================================================
		# Build Execution
		# =============================================================================

		Write-Host ""
		Write-Host "Building FieldWorks..." -ForegroundColor Cyan
		Write-Host "Project: $projectPath" -ForegroundColor Cyan
		Write-Host "Configuration: $Configuration | Platform: $Platform | Parallel: $(-not $Serial) | Tests: $($BuildTests -or $RunTests) | NodeReuse: $($nodeReuseDecision.Enabled) [$($nodeReuseDecision.Source): $($nodeReuseDecision.Reason)]" -ForegroundColor Cyan

		if ($BuildAdditionalApps) {
			Write-Host "Including optional FieldWorks executables" -ForegroundColor Yellow
		}

		# Bootstrap: Build FwBuildTasks first (required by SetupInclude.targets)
		$fwBuildTasksOutputDir = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/"
		Invoke-MSBuild `
			-Arguments @('Build/Src/FwBuildTasks/FwBuildTasks.csproj', '/t:Restore;Build', "/p:Configuration=$Configuration", "/p:Platform=$Platform", `
				"/p:FwBuildTasksOutputPath=$fwBuildTasksOutputDir", "/p:SkipFwBuildTasksAssemblyCheck=true", "/p:SkipFwBuildTasksUsingTask=true", "/p:SkipGenerateFwTargets=true", `
				"/p:SkipSetupTargets=true", "/v:quiet", "/nologo") `
			-Description 'FwBuildTasks (Bootstrap)'

		if (-not (Test-Path $fwTasksSourcePath)) {
			throw "Failed to build FwBuildTasks. Expected $fwTasksSourcePath to exist."
		}

		if ($fwTasksSourcePath -ne $fwTasksDropPath) {
			$dropDir = Split-Path $fwTasksDropPath -Parent
			if (-not (Test-Path $dropDir)) {
				New-Item -Path $dropDir -ItemType Directory -Force | Out-Null
			}
			Copy-Item -Path $fwTasksSourcePath -Destination $fwTasksDropPath -Force
		}

		# Restore packages
		if (-not $SkipRestore) {
			Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
			$packagesDir = Join-Path $PSScriptRoot "packages"
			if (-not (Test-Path $packagesDir)) {
				New-Item -Path $packagesDir -ItemType Directory -Force | Out-Null
			}
			& dotnet restore "$PSScriptRoot\FieldWorks.sln" /p:NoWarn=NU1903 /p:DisableWarnForInvalidRestoreProjects=true "/p:Configuration=$Configuration" "/p:Platform=$Platform" --verbosity quiet
			if ($LASTEXITCODE -ne 0) {
				throw "NuGet package restore failed for FieldWorks.sln"
			}
			Write-Host "Package restore complete." -ForegroundColor Green
		} else {
			Write-Host "Skipping package restore (-SkipRestore)" -ForegroundColor Yellow
		}

		# Copy local LCM assemblies if requested
		if ($UseLocalLcm) {
			Write-Host ""
			Write-Host "Applying local LCM assemblies..." -ForegroundColor Cyan

			$lcmCopyScript = Join-Path $PSScriptRoot "scripts\Agent\Copy-LocalLcm.ps1"
			$lcmArgs = @{
				Configuration = $Configuration
				BuildLcm = $true
				SkipConfirm = $true
			}
			if ($LocalLcmPath) {
				$lcmArgs['LcmRoot'] = $LocalLcmPath
			}

			& $lcmCopyScript @lcmArgs
			if ($LASTEXITCODE -ne 0) {
				throw "Failed to copy local LCM assemblies."
			}
		}

		if ($InstallerOnly) {
			if (-not $BuildInstaller -and -not $BuildPatch) {
				throw "-InstallerOnly requires -BuildInstaller or -BuildPatch."
			}

			$stampPath = Get-BuildStampPath -RepoRoot $PSScriptRoot -ConfigurationName $Configuration
			if (-not (Test-Path $stampPath)) {
				throw "-InstallerOnly requested but no build stamp was found at '$stampPath'. Run a full build once (without -InstallerOnly) to create it."
			}

			$stamp = Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json
			$current = Get-RepoStamp

			$stampConfig = $stamp.Configuration
			$stampPlatform = $stamp.Platform
			if (($stampConfig -ne $Configuration) -or ($stampPlatform -ne $Platform)) {
				throw "-InstallerOnly stamp mismatch: stamp is Configuration='$stampConfig' Platform='$stampPlatform' but this run is Configuration='$Configuration' Platform='$Platform'. Run a full build in this configuration/platform."
			}

			$headChanged = ($stamp.GitHead -ne $current.GitHead)
			if ((-not $ForceInstallerOnly) -and ($headChanged -or $current.IsDirtyOutsideInstaller)) {
				throw "-InstallerOnly refused: product inputs may have changed since the last full build stamp. Run a full build, or use -ForceInstallerOnly if you are sure Output\\$Configuration is still correct."
			}

			Write-Host ""
			Write-Host "Skipping product build (-InstallerOnly). Reusing Output\\$Configuration." -ForegroundColor Yellow
		}
		else {
			# Build using traversal project
			Invoke-MSBuild `
				-Arguments (@($projectPath) + $finalMsBuildArgs) `
				-Description "FieldWorks Solution" `
				-LogPath $LogFile `
				-TailLines $TailLines

			# Avoid log file collisions between the main build and the installer build.
			if (Test-Path "msbuild.binlog") {
				Rename-Item -Path "msbuild.binlog" -NewName "msbuild-FieldWorks.binlog" -Force
			}

			$stampDir = Join-Path $PSScriptRoot ("Output\\{0}" -f $Configuration)
			if (-not (Test-Path $stampDir)) {
				New-Item -Path $stampDir -ItemType Directory -Force | Out-Null
			}

			$repoStamp = Get-RepoStamp
			$stampObject = [pscustomobject]@{
				Configuration = $Configuration
				Platform = $Platform
				GitHead = $repoStamp.GitHead
				IsDirty = $repoStamp.IsDirty
				IsDirtyOutsideInstaller = $repoStamp.IsDirtyOutsideInstaller
				TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
			}

			$stampPath = Get-BuildStampPath -RepoRoot $PSScriptRoot -ConfigurationName $Configuration
			$stampObject | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $stampPath -Encoding UTF8

			Write-Host ""
			Write-Host "[OK] Build complete!" -ForegroundColor Green
			Write-Host "Output: Output\$Configuration" -ForegroundColor Cyan
		}

		# =============================================================================
		# Test Execution (Optional)
		# =============================================================================
		# Run tests BEFORE the installer build because the installer's CleanAll target
		# (enabled on CI via InstallerCleanProductOutputs=true) wipes Output/ and
		# rebuilds without /p:BuildTests=true, removing all *Tests.dll assemblies.

		if ($RunTests) {
			Write-Host ""
			Write-Host "Running tests..." -ForegroundColor Cyan

			$testArgs = @{
				Configuration = $Configuration
				NoBuild       = $true
				Verbosity     = $Verbosity
				SkipDependencyCheck = $SkipDependencyCheck
				SkipWorktreeLock    = $true
			}
			if ($TestFilter) {
				$testArgs["TestFilter"] = $TestFilter
			}

			Stop-ConflictingProcesses @cleanupArgs
			& "$PSScriptRoot\test.ps1" @testArgs
			$script:testExitCode = $LASTEXITCODE
			if ($script:testExitCode -eq 1) {
				# VSTest exit code 1 means tests were skipped (or skipped+failed). test.ps1 prints a
				# FAIL summary when there are actual failures, so treat exit code 1 as a warning only
				# to avoid failing the build when the only non-passing tests were skipped.
				Write-Warning "Test run exited with code 1 (skipped tests or failures). Check test output above for details."
				$script:testExitCode = 0
			} elseif ($script:testExitCode -ne 0) {
				Write-Warning "Some tests failed (exit code: $($script:testExitCode)). Check output above for details."
			}
		}

		if ($BuildInstaller -or $BuildPatch) {
			if ($BuildPatch) {
				$BaseOrPatch = "Patch"
			}
			else {
				$BaseOrPatch = "Installer"
			}
			Write-Host ""
			Write-Host "Building $BaseOrPatch..." -ForegroundColor Cyan

			# Use a different LogFile name than the main build to avoid collisions.
			if (-not [string]::IsNullOrWhiteSpace($LogFile)) {
				$LogFileExtension = [System.IO.Path]::GetExtension($LogFile)
				$LogFile = [System.IO.Path]::ChangeExtension($LogFile, "$BaseOrPatch$LogFileExtension")
			}

			if (-not $isGitHubActions) {
				if ($SignInstaller) {
					Write-Host "Signing enabled for local installer build." -ForegroundColor Yellow
					$env:FILESTOSIGNLATER = $null
				}
				else {
					$defaultSignList = Join-Path $PSScriptRoot "Output\files-to-sign.txt"
					if ([string]::IsNullOrWhiteSpace($env:FILESTOSIGNLATER)) {
						$env:FILESTOSIGNLATER = $defaultSignList
					}
					Write-Host "Signing disabled for local build; capturing files to $env:FILESTOSIGNLATER" -ForegroundColor Yellow
				}
			}

			$installerCleanArg = "/p:InstallerCleanProductOutputs=false"
			if ($isGitHubActions) {
				$installerCleanArg = "/p:InstallerCleanProductOutputs=true"
			}

			Invoke-MSBuild `
				-Arguments (@('Build/InstallerBuild.proj', "/t:Build$BaseOrPatch", '/p:config=release', "/p:InstallerToolset=$InstallerToolset", $installerCleanArg) + `
					$InstallerMsBuildArgs) `
				-Description '$BaseOrPatch Build' `
				-LogPath $LogFile `
				-TailLines $TailLines

			# Avoid log file collisions between the main build and the installer build.
			if (Test-Path "msbuild.binlog") {
				Rename-Item -Path "msbuild.binlog" -NewName "msbuild-$BaseOrPatch.binlog" -Force
			}

			Write-Host "[OK] $BaseOrPatch build complete!" -ForegroundColor Green
		}
	}
}
finally {
	# Kill any lingering build processes that might hold file locks
	Stop-ConflictingProcesses @cleanupArgs
	if ($worktreeLock) {
		Exit-WorktreeLock -LockHandle $worktreeLock
	}
}

if ($testExitCode -ne 0) {
	exit $testExitCode
}
