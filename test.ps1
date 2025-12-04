<#
.SYNOPSIS
    Runs tests for the FieldWorks repository.

.DESCRIPTION
    This script orchestrates test execution for FieldWorks. It handles:
    1. Auto-detecting worktrees and respawning inside Docker containers.
    2. Initializing the Visual Studio Developer Environment (if needed).
    3. Running tests via VSTest.console.exe.

    When running in a worktree (e.g., fw-worktrees/agent-1), the script will
    automatically detect if a corresponding Docker container (fw-agent-1) is
    running and respawn inside the container for proper COM/registry isolation.

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

.PARAMETER NoDocker
    If set, bypasses automatic Docker container detection and runs locally.

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

    Worktree tests automatically use Docker containers when available for
    proper COM/registry isolation.
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
    [switch]$NoDocker
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

# =============================================================================
# Docker Container Auto-Detection for Worktrees
# =============================================================================

if (-not $NoDocker -and -not (Test-InsideContainer)) {
    $agentNum = Get-WorktreeAgentNumber
    if ($null -ne $agentNum) {
        $containerName = "fw-agent-$agentNum"
        if (Test-DockerContainerRunning -ContainerName $containerName) {
            # Build arguments for container execution
            $containerArgs = New-ContainerArgumentString -Parameters @{
                Configuration = $Configuration
                TestFilter = $TestFilter
                TestProject = $TestProject
                NoBuild = $NoBuild.IsPresent
                ListTests = $ListTests.IsPresent
                Verbosity = $Verbosity
            } -Defaults @{
                Configuration = 'Debug'
                Verbosity = 'normal'
            }

            Invoke-InContainer -ScriptName "test.ps1" -Arguments $containerArgs -AgentNumber $agentNum
            exit 0
        }
        else {
            Write-Host "[WARN] Worktree agent-$agentNum detected but container '$containerName' is not running" -ForegroundColor Yellow
            Write-Host "   Running tests locally (use 'scripts/spin-up-agents.ps1' to start containers)" -ForegroundColor Yellow
            Write-Host ""
        }
    }
}

# =============================================================================
# Environment Setup
# =============================================================================

# Initialize VS environment
Initialize-VsDevEnvironment

# Set architecture (x64-only)
$env:arch = 'x64'

# Stop conflicting processes
Stop-ConflictingProcesses

# Clean stale obj folders
Remove-StaleObjFolders -RepoRoot $PSScriptRoot

# =============================================================================
# Build (unless -NoBuild)
# =============================================================================

if (-not $NoBuild) {
    Write-Host "Building before running tests..." -ForegroundColor Cyan
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration -BuildTests -NoDocker
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Build failed. Fix build errors before running tests." -ForegroundColor Red
        exit 1
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
    # Find all test DLLs
    $testDlls = Get-ChildItem -Path $outputDir -Filter "*Tests.dll" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notmatch '^nunit|^Microsoft|^xunit' } |
        Select-Object -ExpandProperty FullName
}

if (-not $testDlls -or $testDlls.Count -eq 0) {
    Write-Host "[ERROR] No test assemblies found in $outputDir" -ForegroundColor Red
    Write-Host "   Run with -BuildTests first: .\build.ps1 -BuildTests" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found $($testDlls.Count) test assembly(ies)" -ForegroundColor Cyan

# =============================================================================
# Find VSTest
# =============================================================================

$vstestPath = Get-VSTestPath

if (-not $vstestPath) {
    Write-Host "[ERROR] vstest.console.exe not found" -ForegroundColor Red
    Write-Host "   Install Visual Studio Build Tools with test components or add vstest to PATH" -ForegroundColor Yellow
    exit 1
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

& $vstestPath $vstestArgs
$testExitCode = $LASTEXITCODE

# =============================================================================
# Report Results
# =============================================================================

if ($testExitCode -eq 0) {
    Write-Host ""
    Write-Host "[PASS] All tests passed" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "[FAIL] Some tests failed (exit code: $testExitCode)" -ForegroundColor Red
}

exit $testExitCode
