<#
.SYNOPSIS
    Runs performance metrics and regression tests for GenerateAssemblyInfo validation.

.DESCRIPTION
    Executes:
    1. Release build timing (T021)
    2. Regression tests (T020)

    Outputs artifacts to Output/GenerateAssemblyInfo/
#>
param(
    [string]$RepoRoot = $PSScriptRoot\..\..,
    [string]$Output = "$PSScriptRoot\..\..\Output\GenerateAssemblyInfo"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path $RepoRoot
$OutputDir = New-Item -ItemType Directory -Path $Output -Force

# T021: Build Metrics
Write-Host "Starting Release Build Timing..." -ForegroundColor Cyan
$timer = [System.Diagnostics.Stopwatch]::StartNew()

# Clean first to ensure fair timing? Or incremental?
# The task says "running pre/post ... timings".
# Usually we want clean build for baseline, or incremental if that's the concern.
# Let's do a standard build.
& msbuild "$RepoRoot\FieldWorks.sln" /m /p:Configuration=Release /v:m
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
}

$timer.Stop()
$buildTime = $timer.Elapsed.TotalSeconds
Write-Host "Build completed in $buildTime seconds." -ForegroundColor Green

$metrics = @{
    timestamp = (Get-Date).ToString("u")
    build_duration_seconds = $buildTime
    configuration = "Release"
}
$metrics | ConvertTo-Json | Out-File "$OutputDir\build-metrics.json" -Encoding utf8

# T020: Regression Tests
Write-Host "Starting Regression Tests..." -ForegroundColor Cyan
$testDir = New-Item -ItemType Directory -Path "$OutputDir\tests" -Force

# We use the standard test target
# Note: This might take a long time.
& msbuild "$RepoRoot\FieldWorks.sln" /t:Test /p:Configuration=Debug /p:ContinueOnError=true /p:TestResultsDir="$testDir"
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Some tests failed. Check $testDir"
} else {
    Write-Host "All tests passed." -ForegroundColor Green
}

Write-Host "Verification complete. Artifacts in $OutputDir" -ForegroundColor Cyan
