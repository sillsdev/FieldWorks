[CmdletBinding()]
param (
    [switch]$FailOnWarning
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path "$PSScriptRoot/../.."
$ReportPath = "$RepoRoot/Output/test-exclusions/validator.json"

Write-Host "Running Test Exclusion Validator..."
Write-Host "Repo Root: $RepoRoot"

Push-Location $RepoRoot
try {
    $PyArgs = @("-m", "scripts.validate_test_exclusions", "--json-report", $ReportPath)
    if ($FailOnWarning) {
        $PyArgs += "--fail-on-warning"
    }

    & python $PyArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Validation failed with exit code $LASTEXITCODE. See report at $ReportPath"
    }

    # Run MSBuild and check for CS0436
    $BuildLog = "$RepoRoot/Output/test-exclusions/build.log"
    Write-Host "Running MSBuild..."
    $BuildArgs = @("FieldWorks.proj", "/m", "/p:Configuration=Debug", "/p:Platform=x64", "/v:minimal", "/nologo", "/fl", "/flp:LogFile=$BuildLog;Verbosity=Normal")

    & msbuild $BuildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE."
    }

    # Analyze log
    Write-Host "Analyzing build log for CS0436..."
    $PyArgs = @("-m", "scripts.validate_test_exclusions", "--analyze-log", $BuildLog)
    & python $PyArgs
    if ($LASTEXITCODE -ne 0) {
        throw "CS0436 analysis failed."
    }

    # Assembly Guard
    $AssemblyPattern = "$RepoRoot/Output/Debug/**/*.dll"
    if (Test-Path "$RepoRoot/Output/Debug") {
        Write-Host "Running Assembly Guard..."
        & "$RepoRoot/scripts/test_exclusions/assembly_guard.ps1" -Assemblies $AssemblyPattern
    } else {
        Write-Warning "Output/Debug not found. Skipping Assembly Guard."
    }
} finally {
    Pop-Location
}
