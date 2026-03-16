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
	Enables or disables MSBuild node reuse (/nr). Default is true.

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

.PARAMETER LcmMode
	Controls how FieldWorks resolves liblcm.
	- Auto: use package mode by default, but report whether local Localizations/LCM inputs are ready.
	- Package: force the package-backed path.
	- Local: force the nested Localizations/LCM source-backed path.

.PARAMETER ManagedDebugType
	Optionally overrides the managed project PDB format for this build.
	Use 'portable' for VS Code debugging. Windows PDBs are not supported by the VS Code debugger path used for FieldWorks.

.PARAMETER SkipDependencyCheck
	If set, skips the dependency preflight check that verifies that required SDKs and tools are installed.

.EXAMPLE
	.\build.ps1
	Builds Debug in parallel with minimal logging.

.EXAMPLE
	.\build.ps1 -Configuration Release -BuildTests
	Builds Release including test projects.

.EXAMPLE
	.\build.ps1 -RunTests
	Builds Debug including test projects and runs all tests.

.EXAMPLE
	.\build.ps1 -Serial -Verbosity detailed
	Builds Debug serially with detailed logging.

.EXAMPLE
	.\build.ps1 -LcmMode Local
	Builds FieldWorks against the nested Localizations/LCM checkout.

.EXAMPLE
	.\build.ps1 -LcmMode Local -ManagedDebugType portable
	Builds FieldWorks against the nested Localizations/LCM checkout with portable managed PDBs for VS Code debugging.

.NOTES
	FieldWorks is x64-only. The x86 platform is no longer supported.
#>
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[switch]$Serial,
	[switch]$BuildTests,
	[switch]$RunTests,
	[string]$TestFilter,
	[switch]$BuildAdditionalApps,
	[string]$Project = "FieldWorks.proj",
	[string]$Verbosity = "minimal",
	[bool]$NodeReuse = $true,
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
	[ValidateSet('Auto', 'Package', 'Local')]
	[string]$LcmMode = 'Auto',
	[ValidateSet('portable', 'full', 'pdbonly', 'embedded')]
	[string]$ManagedDebugType,
	[switch]$SkipDependencyCheck
)

$ErrorActionPreference = "Stop"

$platform = 'x64'
$validLcmModes = @('Auto', 'Package', 'Local')

# PowerShell requires single-dash named parameters. Some callers still pass GNU-style
# double-dash options, which bind positionally before the script starts. Normalize the
# common cases here so build.ps1 remains tolerant of that invocation style.
if ($Configuration -like "--*") {
	$doubleDashOption = $Configuration.Substring(2)
	switch ($doubleDashOption) {
		'TraceCrashes' {
			if (-not $TraceCrashes) {
				$TraceCrashes = $true
				$Configuration = 'Debug'
				Write-Output "[WARN] Detected '--TraceCrashes' passed without PowerShell switch parsing. Using -TraceCrashes and defaulting Configuration to Debug."
			}
		}
		'LcmMode' {
			if ([string]::IsNullOrWhiteSpace($TestFilter)) {
				throw "Detected '--LcmMode' without a mode value. Use -LcmMode <Auto|Package|Local>."
			}

			$requestedMode = $TestFilter.Trim()
			if ($requestedMode -notin $validLcmModes) {
				throw "Invalid LCM mode '$requestedMode'. Use -LcmMode with one of: $($validLcmModes -join ', ')."
			}

			$LcmMode = $requestedMode
			$Configuration = 'Debug'
			$TestFilter = ''
			Write-Output "[WARN] Detected '--LcmMode $requestedMode' passed without PowerShell parameter parsing. Using -LcmMode $requestedMode and defaulting Configuration to Debug."
		}
		default {
			throw "Invalid Configuration value '$Configuration'. Use PowerShell parameter syntax like -Configuration Release or -LcmMode Local."
		}
	}
}
# Add WiX to the PATH for installer builds (required for harvesting localizations)
$env:PATH = "$env:WIX/bin;$env:PATH"

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

Stop-ConflictingProcesses -IncludeOmniSharp

$fwTasksSourcePath = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"
$fwTasksDropPath = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"

# =============================================================================
# Environment Setup
# =============================================================================

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

function Get-DebugRebuildCheckPathspecs {
	param(
		[Parameter(Mandatory = $true)][ValidateSet('Package', 'Local')][string]$ResolvedLcmMode
	)

	$pathspecs = @(
		'build.ps1',
		'Directory.Build.props',
		'Directory.Build.targets',
		'Directory.Packages.props',
		'FieldWorks.proj',
		'Build',
		'Src',
		'Lib'
	)

	if ($ResolvedLcmMode -eq 'Local') {
		$pathspecs += @('FieldWorks.LocalLcm.sln', 'Localizations/LCM')
	}
	else {
		$pathspecs += 'FieldWorks.sln'
	}

	return $pathspecs | ForEach-Object { $_ -replace '\\', '/' }
}

function Get-GitStatusForDebugRebuildCheck {
	param(
		[Parameter(Mandatory = $true)][string[]]$Pathspecs
	)

	$gitArgs = @('status', '--porcelain=v1', '--untracked-files=all', '--') + $Pathspecs
	$statusOutput = & git @gitArgs
	if ($LASTEXITCODE -ne 0) {
		throw "Failed to determine git status snapshot for build stamp."
	}

	return @($statusOutput | ForEach-Object { $_.TrimEnd() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-BuildStampPath {
	param(
		[Parameter(Mandatory = $true)][string]$RepoRoot,
		[Parameter(Mandatory = $true)][string]$ConfigurationName
	)
	$outputDir = Join-Path $RepoRoot ("Output\\{0}" -f $ConfigurationName)
	return Join-Path $outputDir "BuildStamp.json"
}

function Get-LocalLcmState {
	param(
		[Parameter(Mandatory = $true)][string]$RepoRoot,
		[Parameter(Mandatory = $true)][string]$ConfigurationName
	)

	$nestedRoot = Join-Path $RepoRoot 'Localizations\LCM'
	$localSolution = Join-Path $RepoRoot 'FieldWorks.LocalLcm.sln'
	$lcmSolution = Join-Path $nestedRoot 'LCM.sln'
	$artifactsDir = Join-Path $nestedRoot ("artifacts\{0}\net462" -f $ConfigurationName)
	$buildTasksPath = Join-Path $artifactsDir 'SIL.LCModel.Build.Tasks.dll'

	return [pscustomobject]@{
		NestedRoot = $nestedRoot
		LocalSolutionPath = $localSolution
		LcmSolutionPath = $lcmSolution
		ArtifactsDir = $artifactsDir
		NestedRootExists = (Test-Path $nestedRoot)
		LocalSolutionExists = (Test-Path $localSolution)
		LcmSolutionExists = (Test-Path $lcmSolution)
		ArtifactsReady = (Test-Path $buildTasksPath)
		BuildTasksPath = $buildTasksPath
	}
}

function Resolve-LcmMode {
	param(
		[Parameter(Mandatory = $true)][ValidateSet('Auto', 'Package', 'Local')][string]$RequestedMode,
		[Parameter(Mandatory = $true)][string]$ProjectArgument
	)

	if ($RequestedMode -eq 'Local') {
		return 'Local'
	}

	if ($RequestedMode -eq 'Package') {
		return 'Package'
	}

	if ([System.IO.Path]::GetFileName($ProjectArgument) -ieq 'FieldWorks.LocalLcm.sln') {
		return 'Local'
	}

	return 'Package'
}

try {
	Invoke-WithFileLockRetry -Context "FieldWorks build" -IncludeOmniSharp -Action {
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

		# Parallelism
		if (-not $Serial) {
			$finalMsBuildArgs += "/m"
		}

		# Verbosity & Logging
		$finalMsBuildArgs += "/v:$Verbosity"
		$finalMsBuildArgs += "/nologo"
		$finalMsBuildArgs += "/consoleloggerparameters:Summary"

		# Node Reuse
		$finalMsBuildArgs += "/nr:$($NodeReuse.ToString().ToLower())"

		# Properties
		$finalMsBuildArgs += "/p:Configuration=$Configuration"
		$finalMsBuildArgs += "/p:Platform=$platform"
		if ($SkipNative) {
			$finalMsBuildArgs += "/p:SkipNative=true"
		}
		if ($ManagedDebugType) {
			$finalMsBuildArgs += "/p:DebugSymbols=true"
			$finalMsBuildArgs += "/p:DebugType=$ManagedDebugType"
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

		$localLcmState = Get-LocalLcmState -RepoRoot $PSScriptRoot -ConfigurationName $Configuration
		$resolvedLcmMode = Resolve-LcmMode -RequestedMode $LcmMode -ProjectArgument $Project
		$useLocalLcmSource = ($resolvedLcmMode -eq 'Local')
		$restoreSolution = if ($useLocalLcmSource) { $localLcmState.LocalSolutionPath } else { Join-Path $PSScriptRoot 'FieldWorks.sln' }

		Write-Host "LCM mode: $resolvedLcmMode (requested: $LcmMode)" -ForegroundColor Cyan
		if ($ManagedDebugType) {
			Write-Host "Managed debug symbols: $ManagedDebugType" -ForegroundColor Cyan
		}
		Write-Host "Local LCM checkout: $(if ($localLcmState.LcmSolutionExists) { 'ready' } elseif ($localLcmState.NestedRootExists) { 'partial' } else { 'missing' }) at $($localLcmState.NestedRoot)" -ForegroundColor Cyan
		Write-Host "Local LCM artifacts: $(if ($localLcmState.ArtifactsReady) { 'ready' } else { 'missing' }) at $($localLcmState.ArtifactsDir)" -ForegroundColor Cyan
		if ($LcmMode -eq 'Auto' -and -not $useLocalLcmSource -and $localLcmState.NestedRootExists) {
			Write-Host "Auto mode kept the package-backed path. Use -LcmMode Local to build against Localizations/LCM." -ForegroundColor Yellow
		}

		if ($useLocalLcmSource) {
			if (-not $localLcmState.LocalSolutionExists) {
				throw "Local LCM mode requested but FieldWorks.LocalLcm.sln was not found at $($localLcmState.LocalSolutionPath)."
			}
			if (-not $localLcmState.LcmSolutionExists) {
				throw "Local LCM mode requested but the nested liblcm checkout was not found at $($localLcmState.LcmSolutionPath)."
			}
			if (-not $localLcmState.ArtifactsReady) {
				Write-Host "Local LCM build tasks are missing from $($localLcmState.ArtifactsDir). The build will bootstrap them from source." -ForegroundColor Yellow
			}
		}

		$finalMsBuildArgs += "/p:UseLocalLcmSource=$($useLocalLcmSource.ToString().ToLowerInvariant())"
		$installerMsBuildArgs += "/p:UseLocalLcmSource=$($useLocalLcmSource.ToString().ToLowerInvariant())"

		# =============================================================================
		# Build Execution
		# =============================================================================

		Write-Host ""
		Write-Host "Building FieldWorks..." -ForegroundColor Cyan
		Write-Host "Project: $projectPath" -ForegroundColor Cyan
		Write-Host "Configuration: $Configuration | Parallel: $(-not $Serial) | Tests: $($BuildTests -or $RunTests)" -ForegroundColor Cyan

		if ($BuildAdditionalApps) {
			Write-Host "Including optional FieldWorks executables" -ForegroundColor Yellow
		}

		# Bootstrap: Build FwBuildTasks first (required by SetupInclude.targets)
		$fwBuildTasksOutputDir = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/"
		Invoke-MSBuild `
			-Arguments @('Build/Src/FwBuildTasks/FwBuildTasks.csproj', '/t:Restore;Build', "/p:Configuration=$Configuration", "/p:Platform=$platform", `
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
			& dotnet restore $restoreSolution /p:NoWarn=NU1903 /p:DisableWarnForInvalidRestoreProjects=true "/p:Configuration=$Configuration" "/p:Platform=$platform" "/p:UseLocalLcmSource=$($useLocalLcmSource.ToString().ToLowerInvariant())" --verbosity quiet
			if ($LASTEXITCODE -ne 0) {
				throw "NuGet package restore failed for $([System.IO.Path]::GetFileName($restoreSolution))"
			}
			Write-Host "Package restore complete." -ForegroundColor Green
		} else {
			Write-Host "Skipping package restore (-SkipRestore)" -ForegroundColor Yellow
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
			$platformMismatch = ($stamp.PSObject.Properties.Name -contains 'Platform') -and ($stampPlatform -ne $platform)
			if (($stampConfig -ne $Configuration) -or $platformMismatch) {
				$stampDescription = if ($platformMismatch) {
					"Configuration='$stampConfig' Platform='$stampPlatform'"
				}
				else {
					"Configuration='$stampConfig'"
				}

				throw "-InstallerOnly stamp mismatch: stamp is $stampDescription but this run is Configuration='$Configuration'. Run a full build in this configuration."
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
			$relevantDebugPathspecs = Get-DebugRebuildCheckPathspecs -ResolvedLcmMode $resolvedLcmMode
			$relevantDebugStatus = Get-GitStatusForDebugRebuildCheck -Pathspecs $relevantDebugPathspecs
			$stampObject = [pscustomobject]@{
				Configuration = $Configuration
				Platform = $platform
				RequestedLcmMode = $LcmMode
				ResolvedLcmMode = $resolvedLcmMode
				UseLocalLcmSource = $useLocalLcmSource
				ManagedDebugType = $(if ($ManagedDebugType) { $ManagedDebugType } else { '' })
				GitHead = $repoStamp.GitHead
				IsDirty = $repoStamp.IsDirty
				IsDirtyOutsideInstaller = $repoStamp.IsDirtyOutsideInstaller
				RelevantDebugPathspecs = $relevantDebugPathspecs
				RelevantDebugStatus = $relevantDebugStatus
				TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
			}

			$stampPath = Get-BuildStampPath -RepoRoot $PSScriptRoot -ConfigurationName $Configuration
			$stampObject | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $stampPath -Encoding UTF8

			Write-Host ""
			Write-Host "[OK] Build complete!" -ForegroundColor Green
			Write-Host "Output: Output\$Configuration" -ForegroundColor Cyan
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

	# =============================================================================
	# Test Execution (Optional)
	# =============================================================================

	if ($RunTests) {
		Write-Host ""
		Write-Host "Running tests..." -ForegroundColor Cyan

		$testArgs = @("-Configuration", $Configuration, "-NoBuild")
		if ($TestFilter) {
			$testArgs += @("-TestFilter", $TestFilter)
		}

		Stop-ConflictingProcesses @cleanupArgs
		& "$PSScriptRoot\test.ps1" @testArgs
		$testExitCode = $LASTEXITCODE
		if ($testExitCode -ne 0) {
			Write-Warning "Some tests failed. Check output above for details."
		}
	}
}
finally {
	# Kill any lingering build processes that might hold file locks
	Stop-ConflictingProcesses @cleanupArgs
}

if ($testExitCode -ne 0) {
	exit $testExitCode
}
