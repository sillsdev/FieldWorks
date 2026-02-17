<#
.SYNOPSIS
    Run VSTest for FieldWorks test assemblies with proper result parsing.

.DESCRIPTION
    This script runs vstest.console.exe on specified test DLLs and parses the results
    to provide clear pass/fail/skip counts. It handles the InIsolation mode configured
    in Test.runsettings and properly interprets exit codes.

.PARAMETER TestDlls
    Array of test DLL names (e.g., "FwUtilsTests.dll") or paths.
    If just names are provided, looks in Output\Debug by default.

.PARAMETER OutputDir
    Directory containing test DLLs. Defaults to Output\Debug.

.PARAMETER Filter
    Optional VSTest filter expression (e.g., "TestCategory!=Slow").

.PARAMETER Rebuild
    If specified, rebuilds the test projects before running tests.

.PARAMETER All
    If specified, runs all *Tests.dll files found in OutputDir.

.EXAMPLE
    .\Run-VsTests.ps1 -TestDlls FwUtilsTests.dll
    Runs FwUtilsTests.dll and shows results.

.EXAMPLE
    .\Run-VsTests.ps1 -TestDlls FwUtilsTests.dll,xCoreTests.dll
    Runs multiple test DLLs and shows aggregate results.

.EXAMPLE
    .\Run-VsTests.ps1 -All
    Runs all test DLLs in Output\Debug.

.EXAMPLE
    .\Run-VsTests.ps1 -TestDlls FwUtilsTests.dll -Rebuild
    Rebuilds the test project first, then runs tests.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$TestDlls,

    [string]$OutputDir,

    [string]$Filter,

    [switch]$Rebuild,

    [switch]$All
)

$ErrorActionPreference = 'Continue'  # Don't stop on stderr output from vstest

# Find repo root (where FieldWorks.sln is)
$repoRoot = $PSScriptRoot
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot "FieldWorks.sln"))) {
    $repoRoot = Split-Path $repoRoot -Parent
}
if (-not $repoRoot) {
    Write-Error "Could not find repository root (FieldWorks.sln)"
    exit 1
}

# Set defaults
if (-not $OutputDir) {
    $OutputDir = Join-Path $repoRoot "Output\Debug"
}

$runSettings = Join-Path $repoRoot "Test.runsettings"
$vsTestPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

if (-not (Test-Path $vsTestPath)) {
    # Try BuildTools path
    $vsTestPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
}

if (-not (Test-Path $vsTestPath)) {
    Write-Error "vstest.console.exe not found. Install Visual Studio 2022 or Build Tools."
    exit 1
}

# Collect test DLLs
if ($All) {
    $TestDlls = Get-ChildItem $OutputDir -Filter "*Tests.dll" |
                Where-Object { $_.Name -notmatch "\.resources\." } |
                Select-Object -ExpandProperty Name
    Write-Host "Found $($TestDlls.Count) test assemblies" -ForegroundColor Cyan
}

if (-not $TestDlls -or $TestDlls.Count -eq 0) {
    Write-Host "Usage: Run-VsTests.ps1 [-TestDlls] <dll1,dll2,...> [-All] [-Rebuild] [-Filter <expr>]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  Run-VsTests.ps1 FwUtilsTests.dll"
    Write-Host "  Run-VsTests.ps1 FwUtilsTests.dll,xCoreTests.dll"
    Write-Host "  Run-VsTests.ps1 -All"
    Write-Host "  Run-VsTests.ps1 FwUtilsTests.dll -Rebuild"
    exit 0
}

# Rebuild if requested
if ($Rebuild) {
    Write-Host "Rebuilding test projects..." -ForegroundColor Cyan
    foreach ($dll in $TestDlls) {
        $dllName = [System.IO.Path]::GetFileNameWithoutExtension($dll)
        $csprojPattern = Join-Path $repoRoot "Src\**\$dllName.csproj"
        $csproj = Get-ChildItem -Path (Join-Path $repoRoot "Src") -Recurse -Filter "$dllName.csproj" | Select-Object -First 1

        if ($csproj) {
            Write-Host "  Building $($csproj.Name)..." -ForegroundColor Gray
            $buildOutput = dotnet build $csproj.FullName -c Debug --no-incremental -v q 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Build failed for $($csproj.Name)"
                $buildOutput | Write-Host
            }
        }
    }
    Write-Host ""
}

# Run tests
$totalPassed = 0
$totalFailed = 0
$totalSkipped = 0
$results = @()

Write-Host "Running tests..." -ForegroundColor Cyan
Write-Host ""

foreach ($dll in $TestDlls) {
    # Resolve full path
    if (-not [System.IO.Path]::IsPathRooted($dll)) {
        $dllPath = Join-Path $OutputDir $dll
    } else {
        $dllPath = $dll
    }

    if (-not (Test-Path $dllPath)) {
        Write-Warning "Not found: $dll"
        continue
    }

    $dllName = [System.IO.Path]::GetFileName($dllPath)

    # Build arguments
    $args = @($dllPath, "/Settings:$runSettings")
    if ($Filter) {
        $args += "/TestCaseFilter:$Filter"
    }

    # Run vstest
    $output = & $vsTestPath @args 2>&1

    # Parse results
    $passed = ($output | Select-String "^\s+Passed").Count
    $failed = ($output | Select-String "^\s+Failed").Count
    $skipped = ($output | Select-String "^\s+Skipped").Count
    $exitCode = $LASTEXITCODE

    $totalPassed += $passed
    $totalFailed += $failed
    $totalSkipped += $skipped

    # Determine status
    if ($failed -gt 0) {
        $status = "FAIL"
        $color = "Red"
    } elseif ($passed -eq 0 -and $skipped -eq 0) {
        $status = "NONE"
        $color = "Yellow"
    } else {
        $status = "PASS"
        $color = "Green"
    }

    # Output result
    $resultLine = "{0,-40} {1,6} passed, {2,4} failed, {3,4} skipped  [{4}]" -f $dllName, $passed, $failed, $skipped, $status
    Write-Host $resultLine -ForegroundColor $color

    $results += [PSCustomObject]@{
        DLL = $dllName
        Passed = $passed
        Failed = $failed
        Skipped = $skipped
        Status = $status
        Output = $output
    }
}

# Summary
Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
$summaryLine = "TOTAL: {0} passed, {1} failed, {2} skipped" -f $totalPassed, $totalFailed, $totalSkipped
if ($totalFailed -gt 0) {
    Write-Host $summaryLine -ForegroundColor Red
    $exitCode = 1
} else {
    Write-Host $summaryLine -ForegroundColor Green
    $exitCode = 0
}

# Show failures if any
if ($totalFailed -gt 0) {
    Write-Host ""
    Write-Host "Failed tests:" -ForegroundColor Red
    foreach ($r in $results | Where-Object { $_.Failed -gt 0 }) {
        Write-Host ""
        Write-Host "=== $($r.DLL) ===" -ForegroundColor Yellow
        $r.Output | Select-String "^\s+Failed" -Context 0,5 | ForEach-Object { Write-Host $_ }
    }
}

exit $exitCode
