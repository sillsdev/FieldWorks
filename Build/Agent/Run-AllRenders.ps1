<#
.SYNOPSIS
    Runs the render-focused test suites for FieldWorks.

.DESCRIPTION
    Executes the DetailControls render tests and the RootSite render tests sequentially.
    This script is intentionally outside the product project graph so developers can use a
    single entrypoint without introducing a meta test project into the repository architecture.

.PARAMETER Scope
    Which render suites to run. Default is All.

.PARAMETER Configuration
    The build configuration to test. Default is Debug.

.PARAMETER NoBuild
    Skip building before running tests.

.PARAMETER Verbosity
    Test output verbosity: quiet, minimal, normal, detailed.

.PARAMETER SkipDependencyCheck
    Skip dependency preflight checks inside test.ps1.
#>
[CmdletBinding()]
param(
    [ValidateSet('All', 'DetailControls', 'RootSite')]
    [string]$Scope = 'All',
    [string]$Configuration = 'Debug',
    [switch]$NoBuild,
    [ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'q', 'm', 'n', 'd')]
    [string]$Verbosity = 'normal',
    [switch]$SkipDependencyCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$buildScript = Join-Path $repoRoot 'build.ps1'
$testScript = Join-Path $repoRoot 'test.ps1'

if (-not (Test-Path $buildScript)) {
    throw "build.ps1 not found at $buildScript"
}

if (-not (Test-Path $testScript)) {
    throw "test.ps1 not found at $testScript"
}

$requestedScopes = switch ($Scope) {
    'All' { @('DetailControls', 'RootSite') }
    default { @($Scope) }
}

function Invoke-RenderSuite {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$BuildProject,
        [Parameter(Mandatory = $true)]
        [string]$TestProject,
        [Parameter(Mandatory = $true)]
        [string[]]$TestFilters
    )

    if (-not $NoBuild) {
        Write-Output "Building $Name render tests..."

        $buildArgs = @{
            Configuration = $Configuration
            Project = $BuildProject
            Verbosity = $Verbosity
        }

        if ($SkipDependencyCheck) {
            $buildArgs['SkipDependencyCheck'] = $true
        }

        & $buildScript @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "$Name render build failed with exit code $LASTEXITCODE."
        }
    }

    foreach ($testFilter in $TestFilters) {
        Write-Output "Running $Name render tests with filter '$testFilter'..."

        $testArgs = @{
            Configuration = $Configuration
            TestProject = $TestProject
            TestFilter = $testFilter
            Verbosity = $Verbosity
            NoBuild = $true
            SkipDependencyCheck = $true
        }

        & $testScript @testArgs
        if ($LASTEXITCODE -ne 0) {
            throw "$Name render tests failed with exit code $LASTEXITCODE for filter '$testFilter'."
        }
    }

    Write-Output "[OK] $Name render tests passed."
}

foreach ($requestedScope in $requestedScopes) {
    switch ($requestedScope) {
        'DetailControls' {
            Invoke-RenderSuite `
                -Name 'DetailControls' `
                -BuildProject 'Src/Common/Controls/DetailControls/DetailControlsTests/DetailControlsTests.csproj' `
                -TestProject 'Src/Common/Controls/DetailControls/DetailControlsTests' `
                -TestFilters 'FullyQualifiedName~DataTreeRenderTests'
        }
        'RootSite' {
            Invoke-RenderSuite `
                -Name 'RootSite' `
                -BuildProject 'Src/Common/RootSite/RootSiteTests/RootSiteTests.csproj' `
                -TestProject 'Src/Common/RootSite/RootSiteTests' `
                -TestFilters @(
                    'FullyQualifiedName~RenderBaselineTests',
                    'FullyQualifiedName~RenderTimingSuiteTests',
                    'FullyQualifiedName~RenderVerifyTests'
                )
        }
    }
}

Write-Output 'All requested render suites passed.'