<#
.SYNOPSIS
    Rebuild test projects to ensure binding redirects are generated.

.DESCRIPTION
    After changes to Directory.Build.props (like adding DependencyModel reference),
    test projects need to be rebuilt to regenerate their .dll.config binding redirects.
    This script identifies test projects without the required redirects and rebuilds them.

.PARAMETER Force
    Rebuild all test projects, not just those missing binding redirects.

.PARAMETER DryRun
    Show which projects would be rebuilt without actually rebuilding.

.EXAMPLE
    .\Rebuild-TestProjects.ps1
    Rebuilds only test projects missing binding redirects.

.EXAMPLE
    .\Rebuild-TestProjects.ps1 -Force
    Rebuilds all test projects.

.EXAMPLE
    .\Rebuild-TestProjects.ps1 -DryRun
    Shows which projects need rebuilding without doing it.
#>
[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# Find repo root
$repoRoot = $PSScriptRoot
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot "FieldWorks.sln"))) {
    $repoRoot = Split-Path $repoRoot -Parent
}
if (-not $repoRoot) {
    Write-Error "Could not find repository root (FieldWorks.sln)"
    exit 1
}

$outputDir = Join-Path $repoRoot "Output\Debug"
$srcDir = Join-Path $repoRoot "Src"

# Find test DLL configs without DependencyModel binding redirect
Write-Host "Checking test assemblies for binding redirects..." -ForegroundColor Cyan

$needsRebuild = @()

if ($Force) {
    # Rebuild all
    $testConfigs = Get-ChildItem $outputDir -Filter "*Tests.dll.config" -ErrorAction SilentlyContinue
    $needsRebuild = $testConfigs | ForEach-Object { $_.Name -replace '\.dll\.config$', '' }
} else {
    # Check which ones are missing DependencyModel redirect
    $testConfigs = Get-ChildItem $outputDir -Filter "*Tests.dll.config" -ErrorAction SilentlyContinue
    foreach ($config in $testConfigs) {
        $hasRedirect = (Get-Content $config.FullName | Select-String "DependencyModel").Count -gt 0
        if (-not $hasRedirect) {
            $needsRebuild += $config.Name -replace '\.dll\.config$', ''
        }
    }
}

if ($needsRebuild.Count -eq 0) {
    Write-Host "All test assemblies have proper binding redirects." -ForegroundColor Green
    exit 0
}

Write-Host "Found $($needsRebuild.Count) test project(s) needing rebuild:" -ForegroundColor Yellow
$needsRebuild | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

if ($DryRun) {
    Write-Host ""
    Write-Host "Dry run - no changes made." -ForegroundColor Cyan
    exit 0
}

Write-Host ""
Write-Host "Rebuilding..." -ForegroundColor Cyan

$succeeded = 0
$failed = 0

foreach ($projectName in $needsRebuild) {
    $csproj = Get-ChildItem -Path $srcDir -Recurse -Filter "$projectName.csproj" | Select-Object -First 1

    if (-not $csproj) {
        Write-Warning "Could not find $projectName.csproj"
        $failed++
        continue
    }

    Write-Host "  Building $($csproj.Name)..." -ForegroundColor Gray -NoNewline

    $buildOutput = dotnet build $csproj.FullName -c Debug --no-incremental -v q 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host " OK" -ForegroundColor Green
        $succeeded++
    } else {
        Write-Host " FAILED" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""
Write-Host "Rebuilt: $succeeded succeeded, $failed failed" -ForegroundColor $(if ($failed -gt 0) { "Yellow" } else { "Green" })

# Verify
Write-Host ""
Write-Host "Verifying binding redirects..." -ForegroundColor Cyan

$stillMissing = @()
foreach ($projectName in $needsRebuild) {
    $configPath = Join-Path $outputDir "$projectName.dll.config"
    if (Test-Path $configPath) {
        $hasRedirect = (Get-Content $configPath | Select-String "DependencyModel").Count -gt 0
        if (-not $hasRedirect) {
            $stillMissing += $projectName
        }
    }
}

if ($stillMissing.Count -gt 0) {
    Write-Warning "Still missing binding redirects: $($stillMissing -join ', ')"
    exit 1
} else {
    Write-Host "All rebuilt projects now have proper binding redirects." -ForegroundColor Green
}

exit 0
