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
            Write-Host "Building before running tests..." -ForegroundColor Cyan
            & "$PSScriptRoot\build.ps1" -Configuration $Configuration -BuildTests
            if ($LASTEXITCODE -ne 0) {
                Write-Host "[ERROR] Build failed. Fix build errors before running tests." -ForegroundColor Red
                $script:testExitCode = $LASTEXITCODE
                return
            }
            Write-Host ""
        }

        # =============================================================================
        # Find Test Assemblies
        # =============================================================================

        $outputDir = Join-Path $PSScriptRoot "Output/$Configuration"

        if ($TestProject) {
            # Specific project/DLL requested
            if ($TestProject -match '\.dll$') {
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
        }
        else {
            # Find all test DLLs, excluding:
            # - Test framework DLLs (nunit, Microsoft.*, xunit)
            # - External NuGet package tests (SIL.LCModel.*.Tests) - these test liblcm, not FieldWorks
            $testDlls = Get-ChildItem -Path $outputDir -Filter "*Tests.dll" -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -notmatch '^nunit|^Microsoft|^xunit|^SIL\.LCModel' } |
                Select-Object -ExpandProperty FullName
        }

        if (-not $testDlls -or $testDlls.Count -eq 0) {
            Write-Host "[ERROR] No test assemblies found in $outputDir" -ForegroundColor Red
            Write-Host "   Run with -BuildTests first: .\build.ps1 -BuildTests" -ForegroundColor Yellow
            $script:testExitCode = 1
            return
        }

        Write-Host "Found $($testDlls.Count) test assembly(ies)" -ForegroundColor Cyan

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

        $vstestArgs = @()
        $vstestArgs += $testDlls
        $vstestArgs += "/Platform:x64"
        $vstestArgs += "/Settings:`"$PSScriptRoot\Test.runsettings`""
        $vstestArgs += "/ResultsDirectory:`"$outputDir\TestResults`""

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
            $vstestArgs += "/TestCaseFilter:`"$TestFilter`""
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

        $vstestOutput = & $vstestPath $vstestArgs 2>&1 | Tee-Object -Variable testOutput
        $script:testExitCode = $LASTEXITCODE

        if ($script:testExitCode -ne 0) {
            $outputText = ($testOutput | Out-String)
            if ($outputText -match 'used by another process|file is locked|cannot access the file') {
                throw "Detected possible file is locked during vstest execution."
            }
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
