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
    [switch]$Native
)

$ErrorActionPreference = 'Stop'

# =============================================================================
# Import Shared Module
# =============================================================================

$helpersPath = Join-Path $PSScriptRoot "Build/Agent/FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
    Write-Host "[ERROR] FwBuildHelpers.psm1 not found at $helpersPath" -ForegroundColor Red
    exit 1
}
Import-Module $helpersPath -Force

Stop-ConflictingProcesses -IncludeOmniSharp

# =============================================================================
# Environment Setup
# =============================================================================

$cleanupArgs = @{
    IncludeOmniSharp = $true
    RepoRoot = $PSScriptRoot
}

$testExitCode = 0

try {
    Invoke-WithFileLockRetry -Context "FieldWorks test run" -IncludeOmniSharp -Action {
        # Initialize VS environment
        Initialize-VsDevEnvironment
        Test-CvtresCompatibility

        # Set architecture (x64-only)
        $env:arch = 'x64'

        # Stop conflicting processes
        Stop-ConflictingProcesses @cleanupArgs

        # Clean stale obj folders (only if not building, as build.ps1 does it too)
        if ($NoBuild) {
            Remove-StaleObjFolders -RepoRoot $PSScriptRoot
        }

        # =============================================================================
        # Native Tests Dispatch
        # =============================================================================

        if ($Native) {
            $cppScript = Join-Path $PSScriptRoot "scripts/Agent/Invoke-CppTest.ps1"
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
                & $cppScript -Action $action -TestProject $proj -Configuration $Configuration
                if ($LASTEXITCODE -ne 0) {
                    $overallExitCode = $LASTEXITCODE
                    Write-Host "[ERROR] $proj failed with exit code $LASTEXITCODE" -ForegroundColor Red
                }
            }
            $script:testExitCode = $overallExitCode
            return
        }

        # =============================================================================
        # Build (unless -NoBuild)
        # =============================================================================

        if (-not $NoBuild) {
            $normalizedTestProjectForBuild = $TestProject.Replace('\\', '/').TrimEnd('/')

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
            else {
                Write-Host "Building before running tests..." -ForegroundColor Cyan
                & "$PSScriptRoot\build.ps1" -Configuration $Configuration -BuildTests
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
        # invoked outside the .runsettings flow.
        $env:AssertUiEnabled = 'false'
        $env:AssertExceptionEnabled = 'true'

        $outputDir = Join-Path $PSScriptRoot "Output/$Configuration"

        if ($TestProject) {
            $normalizedTestProject = $TestProject.Replace('\\', '/').TrimEnd('/')

            # Specific project/DLL requested
            if ($normalizedTestProject -match '^Build/Src/FwBuildTasks($|/)' -or $normalizedTestProject -match '/FwBuildTasksTests$' -or $normalizedTestProject -match '^FwBuildTasksTests$') {
                # Build tasks tests live in the FwBuildTasks project (not a separate *Tests project).
                # build.ps1 bootstraps this into BuildTools/FwBuildTasks/<Configuration>/FwBuildTasks.dll.
                $testDlls = @(Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll")
            }
            elseif ($normalizedTestProject -match '(^|/)Lib/src/ScrChecks/ScrChecksTests($|/)') {
                # ScrChecksTests builds under Lib/src and is not copied into Output/<Configuration>.
                $testDlls = @(Join-Path $PSScriptRoot "Lib/src/ScrChecks/ScrChecksTests/bin/x64/$Configuration/net48/ScrChecksTests.dll")
            }
            elseif ($TestProject -match '\.dll$') {
                $testDlls = @(Join-Path $outputDir (Split-Path $TestProject -Leaf))
            }
            else {
                # Assume it's a project path, find the DLL
                $projectName = Split-Path $TestProject -Leaf
                if ($projectName -notmatch 'Tests?$') {
                    $projectName = "${projectName}Tests"
                }
                $testDlls = @(Join-Path $outputDir "$projectName.dll")
            }

            # Fallback: some test projects build into their own bin folder and are not copied into Output/<Configuration>.
            # If the expected Output/<Configuration>/<Name>.dll isn't present, look for bin/x64/<Configuration>/net48/<Name>.dll.
            if ($testDlls.Count -eq 1 -and -not (Test-Path $testDlls[0]) -and ($TestProject -notmatch '\\.dll$')) {
                $projectPathCandidate = Join-Path $PSScriptRoot $TestProject

                $projectDir = $null
                $projectBaseName = $null

                if (Test-Path -LiteralPath $projectPathCandidate -PathType Container) {
                    $projectDir = $projectPathCandidate
                    $projectBaseName = Split-Path $projectDir -Leaf
                }
                elseif (Test-Path -LiteralPath $projectPathCandidate -PathType Leaf) {
                    $projectDir = Split-Path $projectPathCandidate -Parent
                    $projectBaseName = [System.IO.Path]::GetFileNameWithoutExtension($projectPathCandidate)
                }

                if ($projectDir -and $projectBaseName) {
                    $binDll = Join-Path $projectDir "bin/x64/$Configuration/net48/$projectBaseName.dll"
                    if (Test-Path -LiteralPath $binDll -PathType Leaf) {
                        $testDlls = @($binDll)
                    }
                }
            }
        }
        else {
            # Find all test DLLs, excluding:
            # - Test framework DLLs (nunit, Microsoft.*, xunit)
            # - External NuGet package tests (SIL.LCModel.*.Tests) - these test liblcm, not FieldWorks
            $testDlls = Get-ChildItem -Path $outputDir -Filter "*Tests.dll" -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -notmatch '^nunit|^Microsoft|^xunit|^SIL\.LCModel' } |
                Select-Object -ExpandProperty FullName

            # Some test projects (e.g., under Lib/src) are not copied into Output/<Configuration>.
            $scrChecksTestsDll = Join-Path $PSScriptRoot "Lib/src/ScrChecks/ScrChecksTests/bin/x64/$Configuration/net48/ScrChecksTests.dll"
            if (Test-Path $scrChecksTestsDll) {
                $testDlls = @($testDlls + $scrChecksTestsDll | Select-Object -Unique)
            }
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

        $runSettingsPath = Join-Path $PSScriptRoot "Test.runsettings"

        $vstestArgs = @()
        $vstestArgs += $testDlls
        $vstestArgs += "/Platform:x64"
        $vstestArgs += "/Settings:$runSettingsPath"
        $vstestArgs += "/ResultsDirectory:$resultsDir"

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

        # =============================================================================
        # Run Tests
        # =============================================================================

        Write-Host ""
        Write-Host "Running tests..." -ForegroundColor Cyan
        Write-Host "  vstest.console.exe $($vstestArgs -join ' ')" -ForegroundColor DarkGray
        Write-Host ""

        $previousEap = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $vstestOutput = & $vstestPath $vstestArgs 2>&1 | Tee-Object -Variable testOutput
            $script:testExitCode = $LASTEXITCODE
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
                $singleArgs += "/Logger:trx;LogFileName=${dllName}_${timestamp}.trx"
                $singleArgs += "/Logger:console;verbosity=$vstestVerbosity"

                if ($TestFilter) {
                    $singleArgs += "/TestCaseFilter:$TestFilter"
                }

                $singleOutput = & $vstestPath $singleArgs 2>&1 | Tee-Object -Variable singleTestOutput
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
    }
}
finally {
    Stop-ConflictingProcesses @cleanupArgs
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
