<#
.SYNOPSIS
    Removes staged DLLs that don't match their expected package versions.

.DESCRIPTION
    LT-22382: MSBuild's SkipUnchangedFiles uses AssemblyVersion for comparison. Some packages
    (like Newtonsoft.Json) keep the same AssemblyVersion across releases but change FileVersion.
    This causes stale DLLs to persist in the output folder even after package updates.

    This script reads desired versions from .csproj PackageReference elements, then checks if
    each staged DLL's FileVersion matches the expected version. If not, the DLL is removed so
    the build will copy the correct one.

.PARAMETER OutputDir
    The output directory to check (e.g., Output\Debug or Output\Release).

.PARAMETER RepoRoot
    The root of the repository. Defaults to the parent of this script's folder.

.EXAMPLE
    .\Remove-StaleDlls.ps1 -OutputDir "Output\Debug"

.EXAMPLE
    .\Remove-StaleDlls.ps1 -OutputDir "Output\Release" -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$OutputDir,

    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
)

$ErrorActionPreference = 'Stop'

# Resolve output path
$outputPath = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $RepoRoot $OutputDir }

if (-not (Test-Path $outputPath)) {
    Write-Verbose "Output directory does not exist: $outputPath"
    return
}

# =============================================================================
# Step 1: Build map of package name -> desired version from .csproj files
# =============================================================================

Write-Verbose "Scanning .csproj files for PackageReference elements..."
$desiredVersions = @{}

# Scan all .csproj files for PackageReference elements
Get-ChildItem (Join-Path $RepoRoot "Src") -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        [xml]$csproj = Get-Content $_.FullName -Raw
        $csproj.SelectNodes("//PackageReference") | ForEach-Object {
            $id = $_.GetAttribute("Include")
            $version = $_.GetAttribute("Version")
            if ($id -and $version) {
                # Later entries override earlier ones (allows for project-specific overrides)
                $desiredVersions[$id.ToLowerInvariant()] = $version
            }
        }
    } catch {
        Write-Verbose "Could not parse $($_.FullName): $_"
    }
}

# Also check packages.config for legacy references (lower priority than PackageReference)
$packagesConfigPath = Join-Path $RepoRoot "Build\nuget-common\packages.config"
if (Test-Path $packagesConfigPath) {
    try {
        [xml]$packagesConfig = Get-Content $packagesConfigPath -Raw
        $packagesConfig.SelectNodes("//package") | ForEach-Object {
            $id = $_.GetAttribute("id")
            $version = $_.GetAttribute("version")
            if ($id -and $version) {
                $key = $id.ToLowerInvariant()
                # Only add if not already set by PackageReference
                if (-not $desiredVersions.ContainsKey($key)) {
                    $desiredVersions[$key] = $version
                }
            }
        }
    } catch {
        Write-Verbose "Could not parse packages.config: $_"
    }
}

Write-Verbose "Found $($desiredVersions.Count) package version specifications"

# =============================================================================
# Step 2: Check each staged DLL's FileVersion against expected package version
# =============================================================================

$mismatchCount = 0
$checkedCount = 0

Get-ChildItem "$outputPath\*.dll" -ErrorAction SilentlyContinue | ForEach-Object {
    $stagedDll = $_
    # Derive package name from DLL name (e.g., Newtonsoft.Json.dll -> newtonsoft.json)
    $packageName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name).ToLowerInvariant()

    # Check if we have a desired version for this package
    $desiredVersion = $desiredVersions[$packageName]
    if (-not $desiredVersion) {
        return  # Not a tracked package, skip
    }

    $checkedCount++

    # Get FileVersion from staged DLL
    $fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($stagedDll.FullName).FileVersion

    # Normalize desired version (strip prerelease suffix like -beta0001)
    $normalizedDesired = $desiredVersion -replace '-.*$', ''

    # Check if FileVersion starts with the expected version
    # e.g., FileVersion "13.0.4.30916" should match desired "13.0.4"
    if (-not $fileVersion.StartsWith("$normalizedDesired.")) {
        $mismatchCount++
        if ($PSCmdlet.ShouldProcess($stagedDll.Name, "Remove mismatched DLL (FileVersion=$fileVersion, expected=$desiredVersion.*)")) {
            Remove-Item $stagedDll.FullName -Force
            Write-Host "Removed $($stagedDll.Name) (was $fileVersion, expected $normalizedDesired.*)" -ForegroundColor Yellow
        }
    }
}

if ($mismatchCount -eq 0) {
    Write-Verbose "No mismatched DLLs found (checked $checkedCount)"
} else {
    Write-Host "Removed $mismatchCount mismatched DLL(s)" -ForegroundColor Yellow
}
