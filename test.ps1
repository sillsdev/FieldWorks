<#
.SYNOPSIS
	Runs tests for the FieldWorks repository.

.DESCRIPTION
	This script orchestrates test execution for FieldWorks. It handles:
	1. Initializing the Visual Studio Developer Environment (if needed).
	2. Running tests via VSTest.console.exe.

.PARAMETER Configuration
	The build configuration to test (Debug or Release). Default is Debug.

.PARAMETER TestFilter
	VSTest filter expression (e.g., "TestCategory!=Slow" or "FullyQualifiedName~FwUtils").

.PARAMETER TestProject
	Path to a specific test project or DLL to run. If not specified, runs all tests.

.PARAMETER NoBuild
	Skip building before running tests. Tests will use existing binaries.

.PARAMETER ListTests
	List available tests without running them.

.PARAMETER Verbosity
	Test output verbosity: q[uiet], m[inimal], n[ormal], d[etailed].
	Default is 'normal'.

.PARAMETER SkipNative
	Skip running native C++ tests. Run only managed tests.

.PARAMETER SkipManaged
	Run only native C++ tests, skipping managed tests.

.PARAMETER AllowAssertDialogs
	Allow FieldWorks Abort/Retry/Ignore assertion dialogs during this local test run.
	Equivalent environment variable: FW_TEST_ALLOW_ASSERT_DIALOGS=1.

.PARAMETER StartedBy
	Optional actor label written to worktree lock metadata (for example: user or agent).
	Defaults to FW_BUILD_STARTED_BY if set; otherwise 'unknown'.

.PARAMETER SkipWorktreeLock
	Internal switch used when test.ps1 is invoked from build.ps1 -RunTests.
	Skips acquiring/releasing the same-worktree lock because the parent build already owns it.

.PARAMETER Coverage
	Collect code coverage via the coverlet.collector "XPlat Code Coverage" data collector (works under
	plain vstest.console.exe on any Visual Studio edition, unlike the VS-Enterprise-only "Code Coverage"
	collector). Writes one coverage.cobertura.xml per test host under Output/<Configuration>/TestResults,
	then renders a merged summary + HTML report via the local ReportGenerator tool
	(Output/<Configuration>/TestResults/CoverageReport).

.EXAMPLE
	.\test.ps1
	Runs all tests in Debug configuration (builds first if needed).

.EXAMPLE
	.\test.ps1 -TestFilter "TestCategory!=Slow"
	Runs all tests except those marked as Slow.

.EXAMPLE
	.\test.ps1 -TestProject "Src/Common/FwUtils/FwUtilsTests"
	Runs tests from the FwUtilsTests project only.

.EXAMPLE
	.\test.ps1 -NoBuild -Verbosity detailed
	Runs tests without building first, with detailed output.

.EXAMPLE
	.\test.ps1 -SkipManaged -TestProject TestGeneric -AllowAssertDialogs
	Runs TestGeneric with interactive FieldWorks assertion dialogs enabled for debugger attachment.

.EXAMPLE
	$env:FW_TEST_ALLOW_ASSERT_DIALOGS = '1'
	.\test.ps1 -SkipManaged -TestProject TestGeneric
	Uses the environment variable equivalent of -AllowAssertDialogs. Clear the variable after debugging.

.EXAMPLE
	.\test.ps1 -TestProject "Src/Common/FwAvalonia/FwAvaloniaTests" -Coverage
	Runs FwAvaloniaTests with code coverage collection and prints a coverage summary.

.NOTES
	FieldWorks is x64-only. Tests run in 64-bit mode.
#>
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[string]$TestFilter = "",
	[string]$TestProject = "",
	[switch]$NoBuild,
	[switch]$ListTests,
	[ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'q', 'm', 'n', 'd')]
	[string]$Verbosity = "normal",
	[switch]$SkipNative,
	[switch]$SkipManaged,
	[switch]$AllowAssertDialogs,
	[switch]$SkipDependencyCheck,
	[switch]$SkipWorktreeLock,
	[switch]$Coverage,
	[ValidateSet('user', 'agent', 'unknown')]
	[string]$StartedBy = 'unknown'
)

$ErrorActionPreference = 'Stop'

if (-not $PSBoundParameters.ContainsKey('StartedBy') -and -not [string]::IsNullOrWhiteSpace($env:FW_BUILD_STARTED_BY)) {
	$startedByFromEnv = $env:FW_BUILD_STARTED_BY.ToLowerInvariant()
	if ($startedByFromEnv -in @('user', 'agent', 'unknown')) {
		$StartedBy = $startedByFromEnv
	}
}

# =============================================================================
# Import Shared Module
# =============================================================================

$helpersPath = Join-Path $PSScriptRoot "Build/Agent/FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
	Write-Host "[ERROR] FwBuildHelpers.psm1 not found at $helpersPath" -ForegroundColor Red
	exit 1
}
Import-Module $helpersPath -Force

function Test-EnvironmentSwitchEnabled {
	param(
		[string]$Name
	)

	$value = [Environment]::GetEnvironmentVariable($Name)
	if ([string]::IsNullOrWhiteSpace($value)) {
		return $false
	}

	return @('1', 'true', 'yes', 'on') -contains $value.Trim().ToLowerInvariant()
}

function Set-TestAssertDialogEnvironment {
	param(
		[bool]$AllowDialogs
	)

	if ($AllowDialogs) {
		$env:AssertUiEnabled = 'true'
		$env:AssertExceptionEnabled = 'false'
		$env:FW_TEST_MODE = '0'
		$env:FW_TEST_ALLOW_ASSERT_DIALOGS = '1'
		return
	}

	$env:AssertUiEnabled = 'false'
	$env:AssertExceptionEnabled = 'true'
	$env:FW_TEST_MODE = '1'
}

function New-TestRunSettingsForAssertDialogMode {
	param(
		[string]$SourcePath,
		[string]$Configuration,
		[bool]$AllowDialogs
	)

	if (-not $AllowDialogs) {
		return $SourcePath
	}

	[xml]$runSettings = Get-Content -LiteralPath $SourcePath -Raw
	$environmentVariables = $runSettings.RunSettings.RunConfiguration.EnvironmentVariables
	$environmentVariables.AssertUiEnabled = 'true'
	$environmentVariables.AssertExceptionEnabled = 'false'
	$environmentVariables.FW_TEST_MODE = '0'

	$allowNode = $environmentVariables.FW_TEST_ALLOW_ASSERT_DIALOGS
	if ($allowNode) {
		$allowNode = $environmentVariables.SelectSingleNode('FW_TEST_ALLOW_ASSERT_DIALOGS')
		$allowNode.InnerText = '1'
	}
	else {
		$allowNode = $runSettings.CreateElement('FW_TEST_ALLOW_ASSERT_DIALOGS')
		$allowNode.InnerText = '1'
		[void]$environmentVariables.AppendChild($allowNode)
	}

	$runSettingsDir = Join-Path $PSScriptRoot "Output/$Configuration"
	if (-not (Test-Path $runSettingsDir)) {
		New-Item -ItemType Directory -Force -Path $runSettingsDir | Out-Null
	}

	$runSettingsPath = Join-Path $runSettingsDir 'Test.allow-assert-dialogs.runsettings'
	$runSettings.Save($runSettingsPath)
	return $runSettingsPath
}

$allowAssertDialogsForRun = $AllowAssertDialogs -or (Test-EnvironmentSwitchEnabled -Name 'FW_TEST_ALLOW_ASSERT_DIALOGS')

function Add-UniquePath {
	param(
		[System.Collections.Generic.List[string]]$Paths,
		[string]$Path
	)

	if ([string]::IsNullOrWhiteSpace($Path)) {
		return
	}

	$fullPath = [System.IO.Path]::GetFullPath($Path)
	foreach ($existingPath in $Paths) {
		if ([string]::Equals($existingPath, $fullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
			return
		}
	}

	$Paths.Add($fullPath)
}

function Get-CentralPackageVersion {
	param(
		[string]$PackagesPropsPath,
		[string]$PackageName
	)

	if (-not (Test-Path -LiteralPath $PackagesPropsPath -PathType Leaf)) {
		return $null
	}

	try {
		[xml]$packagesProps = Get-Content -LiteralPath $PackagesPropsPath -Raw
		$packageNode = $packagesProps.Project.ItemGroup.PackageVersion |
			Where-Object { $_.Include -eq $PackageName -or $_.Update -eq $PackageName } |
			Select-Object -Last 1

		if ($packageNode) {
			return $packageNode.Version
		}
	}
	catch {
		Write-Host "[WARN] Could not read $PackagesPropsPath for $PackageName version." -ForegroundColor Yellow
	}

	return $null
}

function Get-NUnitTestAdapterPaths {
	param(
		[string]$RepoRoot,
		[string[]]$TestDlls
	)

	$adapterPaths = New-Object System.Collections.Generic.List[string]

	$packagesPropsPath = Join-Path $RepoRoot 'Directory.Packages.props'
	$adapterVersion = Get-CentralPackageVersion -PackagesPropsPath $packagesPropsPath -PackageName 'NUnit3TestAdapter'
	if (-not [string]::IsNullOrWhiteSpace($adapterVersion)) {
		$adapterPath = Join-Path $RepoRoot "packages/nunit3testadapter/$adapterVersion/build/net462"
		if (Test-Path -LiteralPath (Join-Path $adapterPath 'NUnit3.TestAdapter.dll') -PathType Leaf) {
			Add-UniquePath -Paths $adapterPaths -Path $adapterPath
			return $adapterPaths.ToArray()
		}
	}

	foreach ($testDll in $TestDlls) {
		if ([string]::IsNullOrWhiteSpace($testDll)) {
			continue
		}

		$testDir = Split-Path $testDll -Parent
		if ($testDir -and
			(Test-Path -LiteralPath (Join-Path $testDir 'NUnit3.TestAdapter.dll') -PathType Leaf) -and
			(Test-Path -LiteralPath (Join-Path $testDir 'nunit.engine.dll') -PathType Leaf)) {
			Add-UniquePath -Paths $adapterPaths -Path $testDir
		}
	}

	if ($adapterPaths.Count -gt 0) {
		return $adapterPaths.ToArray()
	}

	$packagesRoot = Join-Path $RepoRoot 'packages/nunit3testadapter'
	if (Test-Path -LiteralPath $packagesRoot -PathType Container) {
		$packageDirs = Get-ChildItem -LiteralPath $packagesRoot -Directory -ErrorAction SilentlyContinue |
			Sort-Object Name -Descending

		foreach ($packageDir in $packageDirs) {
			$adapterPath = Join-Path $packageDir.FullName 'build/net462'
			if (Test-Path -LiteralPath (Join-Path $adapterPath 'NUnit3.TestAdapter.dll') -PathType Leaf) {
				Add-UniquePath -Paths $adapterPaths -Path $adapterPath
				break
			}
		}
	}

	return $adapterPaths.ToArray()
}

function Get-CoverageAdapterPath {
	param(
		[string]$RepoRoot
	)

	# vstest.console.exe (unlike `dotnet test`) does not auto-discover data collectors from NuGet
	# packages; the coverlet.collector build folder must be passed explicitly via /TestAdapterPath,
	# same as the NUnit adapter above.
	$packagesPropsPath = Join-Path $RepoRoot 'Directory.Packages.props'
	$version = Get-CentralPackageVersion -PackagesPropsPath $packagesPropsPath -PackageName 'coverlet.collector'
	if ([string]::IsNullOrWhiteSpace($version)) {
		return $null
	}

	$adapterPath = Join-Path $RepoRoot "packages/coverlet.collector/$version/build/netstandard2.0"
	if (Test-Path -LiteralPath (Join-Path $adapterPath 'coverlet.collector.dll') -PathType Leaf) {
		return $adapterPath
	}

	return $null
}

# =============================================================================
# Environment Setup
# =============================================================================

$worktreeLock = $null
$cleanupArgs = @{
	IncludeOmniSharp = $true
	RepoRoot = $PSScriptRoot
}

$testExitCode = 0

try {
	if (-not $SkipWorktreeLock) {
		$worktreeLock = Enter-WorktreeLock -RepoRoot $PSScriptRoot -Context "FieldWorks test run" -StartedBy $StartedBy
	}

	# Worktree-aware cleanup: only stop conflicting processes related to this repo root.
	Stop-ConflictingProcesses -IncludeOmniSharp -RepoRoot $PSScriptRoot

	Invoke-WithFileLockRetry -Context "FieldWorks test run" -IncludeOmniSharp -RepoRoot $PSScriptRoot -Action {
		# Initialize VS environment
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

		# Set architecture (x64-only)
		$env:arch = 'x64'

		Set-TestAssertDialogEnvironment -AllowDialogs $allowAssertDialogsForRun
		if ($allowAssertDialogsForRun) {
			Write-Host "[WARN] Interactive assertion dialogs are enabled for this local test run." -ForegroundColor Yellow
		}

		# Stop conflicting processes
		Stop-ConflictingProcesses @cleanupArgs

		# Clean stale obj folders (only if not building, as build.ps1 does it too)
		if ($NoBuild) {
			Remove-StaleObjFolders -RepoRoot $PSScriptRoot
		}

		# =============================================================================
		# Native Tests Dispatch
		# =============================================================================

		$script:nativeErrorMessages = @()
		if (-not $SkipNative) {
			$cppScript = Join-Path $PSScriptRoot "Build/scripts/Invoke-CppTest.ps1"
			if (-not (Test-Path $cppScript)) {
				Write-Host "[ERROR] Native test script not found at $cppScript" -ForegroundColor Red
				$script:testExitCode = 1
				return
			}

			$action = if ($NoBuild) { 'Run' } else { 'BuildAndRun' }

			# Map TestProject to Invoke-CppTest expectations
			$projectsToRun = @()
			if ($TestProject) {
				if ($TestProject -match 'TestViews') { $projectsToRun += 'TestViews' }
				elseif ($TestProject -match 'TestGeneric') { $projectsToRun += 'TestGeneric' }
				else {
					Write-Host "[WARN] Unknown native project '$TestProject'. Defaulting to TestGeneric." -ForegroundColor Yellow
					$projectsToRun += 'TestGeneric'
				}
			}
			else {
				$projectsToRun += 'TestGeneric', 'TestViews'
			}

			$overallExitCode = 0
			foreach ($proj in $projectsToRun) {
				Write-Host "Dispatching $proj to Invoke-CppTest.ps1..." -ForegroundColor Cyan
				$cppTestArgs = @{
					Action = $action
					TestProject = $proj
					Configuration = $Configuration
				}
				if ($allowAssertDialogsForRun) {
					$cppTestArgs.AllowAssertDialogs = $true
				}
				& $cppScript @cppTestArgs
				if ($LASTEXITCODE -ne 0) {
					$overallExitCode = $LASTEXITCODE
					$message = "$proj failed with exit code $LASTEXITCODE"
					$script:nativeErrorMessages += $message
					Write-Host "[ERROR] $message" -ForegroundColor Red
				}
			}
			$script:testExitCode = $overallExitCode

			if ($SkipManaged) {
				return
			}
		} elseif ($SkipManaged) {
			Write-Host "[EXCLAMATION] Are you sure you don't want to run any tests?'" -ForegroundColor Red
			exit 1
		}

		# =============================================================================
		# Build (unless -NoBuild)
		# =============================================================================

		if (-not $NoBuild) {
			$normalizedTestProjectForBuild = $TestProject.Replace('\', '/').TrimEnd('/')

			if ($TestProject -and ($normalizedTestProjectForBuild -match '^Build/Src/FwBuildTasks($|/)' -or $normalizedTestProjectForBuild -match '/FwBuildTasksTests$' -or $normalizedTestProjectForBuild -match '^FwBuildTasksTests$')) {
				Write-Host "Building FwBuildTasks before running tests..." -ForegroundColor Cyan

				$fwBuildTasksOutputDir = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/"
				$fwBuildTasksIntermediateDir = Join-Path $PSScriptRoot "Obj/Build/Src/FwBuildTasks/$Configuration/"
				$fwBuildTasksIntermediateDirX64 = Join-Path $PSScriptRoot "Obj/Build/Src/FwBuildTasks/x64/$Configuration/"

				foreach ($dirToClean in @($fwBuildTasksIntermediateDir, $fwBuildTasksIntermediateDirX64, $fwBuildTasksOutputDir)) {
					if (Test-Path $dirToClean) {
						try {
							Remove-Item -LiteralPath $dirToClean -Recurse -Force -ErrorAction Stop
						}
						catch {
							Write-Host "[ERROR] Failed to clean $dirToClean before rebuilding FwBuildTasks." -ForegroundColor Red
							throw
						}
					}
				}
				New-Item -Path $fwBuildTasksOutputDir -ItemType Directory -Force | Out-Null

				Invoke-MSBuild `
					-Arguments @(
						'Build/Src/FwBuildTasks/FwBuildTasks.csproj',
						'/t:Restore;Clean;Build',
						"/p:Configuration=$Configuration",
						'/p:Platform=AnyCPU',
						"/p:FwBuildTasksOutputPath=$fwBuildTasksOutputDir",
						'/p:SkipFwBuildTasksAssemblyCheck=true',
						'/p:SkipFwBuildTasksUsingTask=true',
						'/p:SkipGenerateFwTargets=true',
						'/p:SkipSetupTargets=true',
						'/nr:false',
						'/v:minimal',
						'/nologo'
					) `
					-Description 'FwBuildTasks (Tests)'
				Write-Host ""
			}
			elseif ($TestProject -and ($normalizedTestProjectForBuild -match '(^|/)Src/InstallValidator/InstallValidatorTests($|/InstallValidatorTests\.csproj$)')) {
				Write-Host "Building InstallValidatorTests before running tests..." -ForegroundColor Cyan

				Invoke-MSBuild `
					-Arguments @(
						'Src/InstallValidator/InstallValidatorTests/InstallValidatorTests.csproj',
						'/t:Restore;Build',
						"/p:Configuration=$Configuration",
						'/p:Platform=x64',
						'/nr:false',
						'/v:minimal',
						'/nologo'
					) `
					-Description 'InstallValidatorTests'

				Write-Host ""
			}
			else {
				Write-Host "Building before running tests..." -ForegroundColor Cyan
				# This nested call runs while test.ps1 already owns the same-worktree lock.
				# Pass -SkipWorktreeLock explicitly so the build path does not depend on the
				# current '&' invocation sharing the same thread and Windows mutex recursion.
				& "$PSScriptRoot\build.ps1" -Configuration $Configuration -BuildTests -SkipWorktreeLock
				if ($LASTEXITCODE -ne 0) {
					Write-Host "[ERROR] Build failed. Fix build errors before running tests." -ForegroundColor Red
					$script:testExitCode = $LASTEXITCODE
					return
				}
				Write-Host ""
			}
		}

		# =============================================================================
		# Find Test Assemblies
		# =============================================================================

		# =============================================================================
		# Prevent modal dialogs during tests
		# =============================================================================

		# FieldWorks native + managed assertion infrastructure may show modal UI unless
		# explicitly disabled. Ensure the test host inherits these settings even when
		# invoked outside the .runsettings flow, unless a local debugging run opted back in.
		Set-TestAssertDialogEnvironment -AllowDialogs $allowAssertDialogsForRun

		$outputDir = Join-Path $PSScriptRoot "Output/$Configuration"

		if ($TestProject) {
			$normalizedTestProject = $TestProject.Replace('\', '/').TrimEnd('/')

			# Specific project/DLL requested
			if ($normalizedTestProject -match '^Build/Src/FwBuildTasks($|/)' -or $normalizedTestProject -match '/FwBuildTasksTests$' -or $normalizedTestProject -match '^FwBuildTasksTests$') {
				# Build tasks tests live in the FwBuildTasks project (not a separate *Tests project).
				# build.ps1 bootstraps this into BuildTools/FwBuildTasks/<Configuration>/FwBuildTasks.dll.
				$testDlls = @(Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll")
			}
			elseif ($TestProject -match '\.dll$') {
				$testDlls = @(Join-Path $outputDir (Split-Path $TestProject -Leaf))
			}
			else {
				# Assume it's a project path, find the DLL
				$projectName = Split-Path $TestProject -Leaf
				if ($projectName -match '\.csproj$') {
					$projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectName)
				}
				if ($projectName -notmatch 'Tests?$') {
					$projectName = "${projectName}Tests"
				}
				$testDlls = @(Join-Path $outputDir "$projectName.dll")
			}
		}
		else {
			# Find all test DLLs, excluding:
			# - Test framework DLLs (nunit, Microsoft.*, xunit)
			# - External NuGet package tests (SIL.LCModel.*.Tests) - these test liblcm, not FieldWorks
			# - SIL.WritingSystems.Tests - NuGet-delivered libpalaso test DLL compiled against
			#   NUnit 3.13.3; loading it causes binding-redirect failures (not a FieldWorks test)
			$testDlls = Get-ChildItem -Path $outputDir -Filter "*Tests.dll" -ErrorAction SilentlyContinue |
				Where-Object { $_.Name -notmatch '^nunit|^Microsoft|^xunit|^SIL\.LCModel|^SIL\.WritingSystems\.Tests' } |
				Select-Object -ExpandProperty FullName
		}

		$missingTestDlls = @($testDlls | Where-Object { -not (Test-Path $_) })
		if ($missingTestDlls.Count -gt 0) {
			Write-Host "[ERROR] One or more requested test assemblies were not found:" -ForegroundColor Red
			foreach ($missing in $missingTestDlls) {
				Write-Host "  - $missing" -ForegroundColor Red
			}
			Write-Host "   If this is a build tasks test, run: .\\build.ps1 -Configuration $Configuration" -ForegroundColor Yellow
			$script:testExitCode = 1
			return
		}

		if (-not $testDlls -or $testDlls.Count -eq 0) {
			Write-Host "[ERROR] No test assemblies found in $outputDir" -ForegroundColor Red
			Write-Host "   Run with -BuildTests first: .\build.ps1 -BuildTests" -ForegroundColor Yellow
			$script:testExitCode = 1
			return
		}

		Write-Host "Found $($testDlls.Count) test assembly(ies)" -ForegroundColor Cyan

		# =============================================================================
		# Ensure activation context manifests are present
		# =============================================================================

		# Many tests rely on ActivationContextHelper("FieldWorks.Tests.manifest") (and related manifests)
		# being present in the working directory. When a test assembly lives outside Output/<Configuration>
		# (e.g., Lib/src/*/bin), copy the manifests so reg-free COM activation works.
		$manifestFiles = Get-ChildItem -Path $outputDir -Filter "*.manifest" -ErrorAction SilentlyContinue
		if ($manifestFiles -and $manifestFiles.Count -gt 0) {
			foreach ($testDll in $testDlls) {
				$testDir = Split-Path $testDll -Parent
				if ($testDir -and ($testDir.TrimEnd('\\') -ne $outputDir.TrimEnd('\\'))) {
					foreach ($manifest in $manifestFiles) {
						$dest = Join-Path $testDir $manifest.Name
						if (-not (Test-Path -LiteralPath $dest -PathType Leaf)) {
							Copy-Item -LiteralPath $manifest.FullName -Destination $dest -Force
						}
					}
				}
			}
		}

		# =============================================================================
		# Find VSTest
		# =============================================================================

		$vstestPath = Get-VSTestPath

		if (-not $vstestPath) {
			Write-Host "[ERROR] vstest.console.exe not found" -ForegroundColor Red
			Write-Host "   Install Visual Studio Build Tools with test components or add vstest to PATH" -ForegroundColor Yellow
			$script:testExitCode = 1
			return
		}

		Write-Host "Found vstest.console.exe: $vstestPath" -ForegroundColor Gray

		# =============================================================================
		# Build VSTest Arguments
		# =============================================================================

		$resultsDir = Join-Path $outputDir "TestResults"
		if (-not (Test-Path $resultsDir)) {
			New-Item -Path $resultsDir -ItemType Directory -Force | Out-Null
		}

		# =============================================================================
		# ICU_DATA setup (dev/test convenience)
		# =============================================================================

		function Test-IcuDataDir([string]$dir) {
			if ([string]::IsNullOrWhiteSpace($dir)) { return $false }

			# Some machines may have ICU_DATA set to a list. Prefer the first entry.
			$firstDir = $dir.Split(';') | Select-Object -First 1
			if (-not (Test-Path -LiteralPath $firstDir -PathType Container)) { return $false }

			return (Test-Path -LiteralPath (Join-Path $firstDir 'nfc_fw.nrm') -PathType Leaf) -and
				   (Test-Path -LiteralPath (Join-Path $firstDir 'nfkc_fw.nrm') -PathType Leaf)
		}

		$icuDataNeedsConfig = -not (Test-IcuDataDir $env:ICU_DATA)
		if ($icuDataNeedsConfig) {
			try {
				$distFiles = Join-Path $PSScriptRoot 'DistFiles'
				if (Test-Path $distFiles) {
					$icuDataDir = $null

					$icuRoots = Get-ChildItem -Path $distFiles -Directory -Filter 'Icu*' -ErrorAction SilentlyContinue
					foreach ($icuRoot in $icuRoots) {
						$candidate = Get-ChildItem -Path $icuRoot.FullName -Directory -Filter 'icudt*l' -ErrorAction SilentlyContinue | Select-Object -First 1
						if ($candidate) {
							$icuDataDir = $candidate.FullName
							break
						}
					}

					if (-not $icuDataDir) {
						$candidate = Get-ChildItem -Path $distFiles -Directory -Filter 'icudt*l' -ErrorAction SilentlyContinue | Select-Object -First 1
						if ($candidate) {
							$icuDataDir = $candidate.FullName
						}
					}

					if ($icuDataDir) {
						$env:FW_ICU_DATA_DIR = $icuDataDir
						$env:ICU_DATA = $icuDataDir
						Write-Host "Configured ICU_DATA=$icuDataDir" -ForegroundColor Gray
					}
					elseif ($env:ICU_DATA) {
						Write-Host "ICU_DATA is set but invalid (missing nfc_fw.nrm/nfkc_fw.nrm): $($env:ICU_DATA)" -ForegroundColor Yellow
					}
				}
			}
			catch {
				# Best-effort: tests may still run on machines where ICU_DATA is already configured.
			}
		}

		$runSettingsSourcePath = Join-Path $PSScriptRoot "Test.runsettings"
		$runSettingsPath = New-TestRunSettingsForAssertDialogMode `
			-SourcePath $runSettingsSourcePath `
			-Configuration $Configuration `
			-AllowDialogs $allowAssertDialogsForRun

		$vstestArgs = @()
		$vstestArgs += $testDlls
		$vstestArgs += "/Platform:x64"
		$vstestArgs += "/Settings:$runSettingsPath"
		$vstestArgs += "/ResultsDirectory:$resultsDir"

		$nunitAdapterPaths = @(Get-NUnitTestAdapterPaths -RepoRoot $PSScriptRoot -TestDlls $testDlls)
		foreach ($adapterPath in $nunitAdapterPaths) {
			$vstestArgs += "/TestAdapterPath:$adapterPath"
		}

		if ($Coverage) {
			$coverageAdapterPath = Get-CoverageAdapterPath -RepoRoot $PSScriptRoot
			if ($coverageAdapterPath) {
				$vstestArgs += "/TestAdapterPath:$coverageAdapterPath"
			}
			else {
				Write-Host "[WARN] -Coverage requested but coverlet.collector build folder was not found under packages/coverlet.collector. Run a build first so it restores." -ForegroundColor Yellow
			}
		}

		# Logger configuration - verbosity goes with the console logger
		$verbosityMap = @{
			'quiet' = 'quiet'; 'q' = 'quiet'
			'minimal' = 'minimal'; 'm' = 'minimal'
			'normal' = 'normal'; 'n' = 'normal'
			'detailed' = 'detailed'; 'd' = 'detailed'
		}
		$vstestVerbosity = $verbosityMap[$Verbosity]
		$vstestArgs += "/Logger:trx"
		$vstestArgs += "/Logger:console;verbosity=$vstestVerbosity"

		if ($TestFilter) {
			$vstestArgs += "/TestCaseFilter:$TestFilter"
		}

		if ($ListTests) {
			$vstestArgs += "/ListTests"
		}

		if ($Coverage) {
			$vstestArgs += '/Collect:XPlat Code Coverage'
		}

		# =============================================================================
		# Run Tests
		# =============================================================================

		Write-Host ""
		Write-Host "Running tests..." -ForegroundColor Cyan
		foreach ($adapterPath in $nunitAdapterPaths) {
			Write-Host "  NUnit adapter path: $adapterPath" -ForegroundColor DarkGray
		}
		Write-Host "  vstest.console.exe $($vstestArgs -join ' ')" -ForegroundColor DarkGray
		Write-Host ""

		$previousEap = $ErrorActionPreference
		$ErrorActionPreference = 'Continue'
		try {
			& $vstestPath $vstestArgs 2>&1 | Tee-Object -Variable testOutput
			# Don't overwrite a non-zero exit code from native tests with a zero exit code from these tests.
			if ($LASTEXITCODE -ne 0) {
				$script:testExitCode = $LASTEXITCODE
			}
		}
		finally {
			$ErrorActionPreference = $previousEap
		}

		$vstestLogPath = Join-Path $resultsDir "vstest.console.log"
		try {
			$testOutput | Out-File -FilePath $vstestLogPath -Encoding UTF8
			Write-Host "VSTest output log: $vstestLogPath" -ForegroundColor Gray
		}
		catch {
			Write-Host "[WARN] Failed to write VSTest output log to $vstestLogPath" -ForegroundColor Yellow
		}

		if ($script:testExitCode -ne 0) {
			$outputText = ($testOutput | Out-String)
			if ($outputText -match 'used by another process|file is locked|cannot access the file') {
				throw "Detected possible file is locked during vstest execution."
			}
		}

		# =============================================================================
		# Workaround: multi-assembly VSTest may fail with exit code -1 and minimal output
		# =============================================================================

		if (-not $ListTests -and $testDlls.Count -gt 1 -and $script:testExitCode -eq -1) {
			Write-Host "[WARN] vstest.console.exe returned exit code -1 with multiple test assemblies. Retrying per-assembly to isolate failures." -ForegroundColor Yellow

			$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
			$overallExitCode = 0

			foreach ($testDll in $testDlls) {
				$dllName = [System.IO.Path]::GetFileNameWithoutExtension($testDll)
				Write-Host ""
				Write-Host "Running tests in $dllName..." -ForegroundColor Cyan

				$singleArgs = @()
				$singleArgs += $testDll
				$singleArgs += "/Platform:x64"
				$singleArgs += "/Settings:$runSettingsPath"
				$singleArgs += "/ResultsDirectory:$resultsDir"
				foreach ($adapterPath in $nunitAdapterPaths) {
					$singleArgs += "/TestAdapterPath:$adapterPath"
				}
				if ($Coverage -and $coverageAdapterPath) {
					$singleArgs += "/TestAdapterPath:$coverageAdapterPath"
				}
				$singleArgs += "/Logger:trx;LogFileName=${dllName}_${timestamp}.trx"
				$singleArgs += "/Logger:console;verbosity=$vstestVerbosity"

				if ($TestFilter) {
					$singleArgs += "/TestCaseFilter:$TestFilter"
				}

				if ($Coverage) {
					$singleArgs += '/Collect:XPlat Code Coverage'
				}

				& $vstestPath $singleArgs 2>&1 | Tee-Object -Variable singleTestOutput
				$singleExitCode = $LASTEXITCODE
				if ($singleExitCode -ne 0 -and $overallExitCode -eq 0) {
					$overallExitCode = $singleExitCode
				}

				$singleLogPath = Join-Path $resultsDir "vstest.${dllName}.console.log"
				try {
					$singleTestOutput | Out-File -FilePath $singleLogPath -Encoding UTF8
				}
				catch {
					Write-Host "[WARN] Failed to write VSTest output log to $singleLogPath" -ForegroundColor Yellow
				}

				if ($singleExitCode -ne 0) {
					$singleOutputText = ($singleTestOutput | Out-String)
					if ($singleOutputText -match 'used by another process|file is locked|cannot access the file') {
						throw "Detected possible file is locked during vstest execution."
					}
				}
			}

			$script:testExitCode = $overallExitCode
		}

		# =============================================================================
		# Coverage report (only when -Coverage was requested)
		# =============================================================================

		if ($Coverage -and -not $ListTests) {
			$coverageFiles = Get-ChildItem -Path $resultsDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
			if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
				Write-Host "[WARN] -Coverage was requested but no coverage.cobertura.xml files were produced under $resultsDir." -ForegroundColor Yellow
			}
			else {
				Write-Host ""
				Write-Host "Generating coverage report from $($coverageFiles.Count) file(s)..." -ForegroundColor Cyan
				$coverageReportDir = Join-Path $resultsDir "CoverageReport"
				$coverageReportsGlob = Join-Path $resultsDir "*/coverage.cobertura.xml"
				try {
					# Restore the local tool manifest first (cheap no-op once already restored) so a fresh
					# checkout/CI runner doesn't just warn and skip the report.
					& dotnet tool restore 2>&1 | Out-Null
					& dotnet tool run reportgenerator "-reports:$coverageReportsGlob" "-targetdir:$coverageReportDir" "-reporttypes:TextSummary;Html" 2>&1 |
						Where-Object { $_ -notmatch '^\d{4}-\d\d-\d\dT' } # drop ReportGenerator's timestamped progress lines
					$summaryPath = Join-Path $coverageReportDir "Summary.txt"
					if (Test-Path -LiteralPath $summaryPath) {
						Write-Host ""
						Get-Content -LiteralPath $summaryPath | Write-Host
						Write-Host ""
						Write-Host "Full HTML coverage report: $(Join-Path $coverageReportDir 'index.html')" -ForegroundColor Gray
					}
				}
				catch {
					Write-Host "[WARN] ReportGenerator failed (is '.config/dotnet-tools.json' restored? run 'dotnet tool restore'): $_" -ForegroundColor Yellow
				}
			}
		}
	}
	}
	finally {
		Stop-ConflictingProcesses @cleanupArgs
		if ($worktreeLock) {
			Exit-WorktreeLock -LockHandle $worktreeLock
		}
	}

# =============================================================================
# Failure Summary (always print to terminal when there are failures)
# =============================================================================

$vstestLogPath = Join-Path $PSScriptRoot "Output/$Configuration/TestResults/vstest.console.log"
if ($testExitCode -ne 0 -and (Test-Path $vstestLogPath)) {
	Write-Host ""
	Write-Host "========== FAILURE SUMMARY ==========" -ForegroundColor Red

	if ($script:nativeErrorMessages.Count -gt 0) {
		Write-Host "  Native test failures:" -ForegroundColor Red
		foreach ($msg in $script:nativeErrorMessages) {
			Write-Host "    - $msg" -ForegroundColor Red
		}
		Write-Host "=====================================" -ForegroundColor Red
	}

	$logLines = Get-Content $vstestLogPath
	$failedTests = @()
	for ($i = 0; $i -lt $logLines.Count; $i++) {
		if ($logLines[$i] -match '^\s+Failed\s+(\S.*)') {
			$testName = $Matches[1].Trim()
			$errorMsg = ""
			# Look ahead for "Error Message:" line
			if ($i + 2 -lt $logLines.Count -and $logLines[$i + 1] -match '^\s+Error Message:') {
				$errorMsg = $logLines[$i + 2].Trim()
			}
			$failedTests += [PSCustomObject]@{ Test = $testName; Error = $errorMsg }
		}
	}

	if ($failedTests.Count -gt 0) {
		# Group by error message for a compact summary
		$groups = $failedTests | Group-Object Error | Sort-Object Count -Descending
		foreach ($grp in $groups) {
			Write-Host ""
			Write-Host "  [$($grp.Count) failure(s)] $($grp.Name)" -ForegroundColor Yellow
			# Show up to 5 test names per group
			$shown = 0
			foreach ($item in $grp.Group) {
				if ($shown -ge 5) {
					Write-Host "    ... and $($grp.Count - 5) more" -ForegroundColor DarkGray
					break
				}
				Write-Host "    - $($item.Test)" -ForegroundColor Gray
				$shown++
			}
		}
		Write-Host ""
		Write-Host "  Total: $($failedTests.Count) failed test(s)" -ForegroundColor Red
	}

	Write-Host "=====================================" -ForegroundColor Red
	Write-Host "  Full log for managed tests: $vstestLogPath" -ForegroundColor Gray
	if (-not $SkipNative) {
		$nativeLogPath = Join-Path $PSScriptRoot "Output/$Configuration/<SuiteName>.exe.log"
		Write-Host "  Logs for each native test suite: $nativeLogPath" -ForegroundColor Gray
	}
}

if ($testExitCode -eq 0) {
	Write-Host ""
	Write-Host "[PASS] All tests passed" -ForegroundColor Green
}
else {
	Write-Host ""
	Write-Host "[FAIL] Some tests failed (exit code: $testExitCode)" -ForegroundColor Red
}

exit $testExitCode
