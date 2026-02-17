<#
.SYNOPSIS
    Single-pass detection and removal of stale DLLs from a FieldWorks output directory.

.DESCRIPTION
    Performs up to two checks in one pass over every DLL in the target directory:

    1. Product major-version check (first-party whitelist)
       Builds a positive list of first-party assembly names from Src/**/*.csproj project names
       and <AssemblyName> overrides.  For every DLL whose basename is in this whitelist, its
       AssemblyVersion major component must match FWMAJOR from Src/MasterVersionInfo.txt.
       This catches stale first-party DLLs left behind from a different FW version
       (e.g. 6.0.0.0 in a 9.x tree).

    2. Staged-vs-reference comparison (when -ReferenceDir is provided)
       Every DLL present in both OutputDir and ReferenceDir is compared by
       AssemblyName.FullName and FileVersion.  This is the most reliable check and catches
       NuGet version drift (LT-22382, e.g. Newtonsoft.Json 13.0.3 vs 13.0.4) as well as
       any configuration-switch staleness.

    Any DLL that fails either check is removed so MSBuild re-copies the correct version.

    Use -ValidateOnly to report mismatches as errors without deleting (used by installer staging
    validation targets).

.PARAMETER OutputDir
    The directory to scan (e.g., Output\Debug, or a staged installer bin dir).

.PARAMETER RepoRoot
    Repository root. Defaults to two levels above this script.

.PARAMETER ValidateOnly
    Report mismatches as errors (exit 1) instead of deleting files.  Suitable for MSBuild
    post-staging validation.

.PARAMETER ReferenceDir
    Optional second directory. When provided, every DLL present in both OutputDir and
    ReferenceDir is compared by AssemblyName.FullName and FileVersion.  Mismatches are
    reported (or cause deletion from OutputDir when -ValidateOnly is not set).

.EXAMPLE
    .\Remove-StaleDlls.ps1 -OutputDir "Output\Debug"
    Pre-build clean pass: removes stale first-party DLLs from the output directory.

.EXAMPLE
    .\Remove-StaleDlls.ps1 -OutputDir "Output\Release" -WhatIf
    Shows what would be removed without deleting.

.EXAMPLE
    .\Remove-StaleDlls.ps1 -OutputDir "BuildDir\...\objects\FieldWorks" -ReferenceDir "Output\Release" -ValidateOnly
    Installer post-staging validation: fails the build if any staged DLL doesn't match the build output.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$OutputDir,

    [string]$RepoRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),

    [switch]$ValidateOnly,

    [string]$ReferenceDir
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Resolve-DirPath ([string]$dir) {
    if ([System.IO.Path]::IsPathRooted($dir)) { return $dir }
    return Join-Path $RepoRoot $dir
}

$outputPath = Resolve-DirPath $OutputDir
if (-not (Test-Path $outputPath)) {
    Write-Verbose "Output directory does not exist: $outputPath"
    return
}

$referencePath = $null
if ($ReferenceDir) {
    $referencePath = Resolve-DirPath $ReferenceDir
    if (-not (Test-Path $referencePath)) {
        Write-Verbose "Reference directory does not exist: $referencePath"
        $referencePath = $null
    }
}

# =============================================================================
# Step 1: Build lookup tables (done once, used for every DLL)
# =============================================================================

# --- 1a. First-party assembly names (whitelist from Src/**/*.csproj) ---
Write-Verbose "Scanning .csproj files for first-party assembly names..."
$firstPartyNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

$srcDir = Join-Path $RepoRoot "Src"
if (Test-Path $srcDir) {
    Get-ChildItem $srcDir -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        # Default assembly name = project filename without extension
        $projName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
        [void]$firstPartyNames.Add($projName)

        try {
            [xml]$csproj = Get-Content $_.FullName -Raw
            # Check for <AssemblyName> override
            $asmNameNode = $csproj.SelectSingleNode("//AssemblyName")
            if ($asmNameNode -and $asmNameNode.InnerText) {
                [void]$firstPartyNames.Add($asmNameNode.InnerText)
            }
        }
        catch {
            Write-Verbose "Could not parse $($_.FullName): $_"
        }
    }
}

Write-Verbose "Found $($firstPartyNames.Count) first-party assembly names"

# --- 1b. Expected product major version from MasterVersionInfo.txt ---
$expectedMajor = $null
$versionInfoPath = Join-Path $RepoRoot "Src\MasterVersionInfo.txt"
if (Test-Path $versionInfoPath) {
    Get-Content $versionInfoPath | ForEach-Object {
        if ($_ -match '^FWMAJOR=(\d+)') {
            $expectedMajor = [int]$Matches[1]
        }
    }
}
if ($null -eq $expectedMajor) {
    Write-Warning "Could not determine FWMAJOR from MasterVersionInfo.txt. Product-version check disabled."
}
else {
    Write-Verbose "Expected product major version: $expectedMajor"
}

# =============================================================================
# Step 2: Single pass over every DLL
# =============================================================================

$problems = [System.Collections.Generic.List[string]]::new()
$removedCount = 0
$checkedProduct = 0

Get-ChildItem "$outputPath\*.dll" -ErrorAction SilentlyContinue | ForEach-Object {
    $dll = $_
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($dll.Name)

    # --- Check A: Product major-version match (first-party DLLs only) ---
    if (($null -ne $expectedMajor) -and $firstPartyNames.Contains($baseName)) {
        try {
            $asmName = [System.Reflection.AssemblyName]::GetAssemblyName($dll.FullName)
            $asmVersion = $asmName.Version
        }
        catch {
            # Not a managed assembly (native DLL) — skip
            Write-Verbose "Skipping non-managed: $($dll.Name)"
            return
        }

        # Skip assemblies with version 0.0.0.0 (auto-generated or unversioned)
        if ($asmVersion.Major -eq 0 -and $asmVersion.Minor -eq 0) {
            return
        }

        $checkedProduct++

        if ($asmVersion.Major -ne $expectedMajor) {
            $msg = "$($dll.Name): AssemblyVersion=$asmVersion, expected major=$expectedMajor (product)"
            $problems.Add($msg)
            if (-not $ValidateOnly) {
                if ($PSCmdlet.ShouldProcess($dll.Name, "Remove (wrong product version: AssemblyVersion=$asmVersion, expected major=$expectedMajor)")) {
                    Remove-Item $dll.FullName -Force
                    $removedCount++
                    Write-Host "  Removed $msg" -ForegroundColor Yellow
                }
            }
            return
        }
    }

    # --- Check B: Staged-vs-reference comparison (when -ReferenceDir provided) ---
    if ($referencePath) {
        $refDll = Join-Path $referencePath $dll.Name
        if (Test-Path $refDll) {
            # Compare AssemblyName.FullName (catches strong-name/version mismatches)
            try {
                $stagedAsm = [System.Reflection.AssemblyName]::GetAssemblyName($dll.FullName)
                $refAsm    = [System.Reflection.AssemblyName]::GetAssemblyName($refDll)
                if ($stagedAsm.FullName -ne $refAsm.FullName) {
                    $msg = "$($dll.Name): staged=$($stagedAsm.FullName), build=$($refAsm.FullName) (assembly mismatch)"
                    $problems.Add($msg)
                    if (-not $ValidateOnly) {
                        if ($PSCmdlet.ShouldProcess($dll.Name, "Remove (staged/build assembly mismatch)")) {
                            Remove-Item $dll.FullName -Force
                            $removedCount++
                            Write-Host "  Removed $msg" -ForegroundColor Yellow
                        }
                    }
                    return
                }
            }
            catch {
                # One or both are native — fall through to FileVersion check
            }

            # Compare FileVersion (catches same-AssemblyVersion NuGet bumps like Newtonsoft.Json)
            $stagedFV = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll.FullName).FileVersion
            $refFV    = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($refDll).FileVersion
            if ($stagedFV -ne $refFV) {
                $msg = "$($dll.Name): staged FileVersion=$stagedFV, build FileVersion=$refFV (drift)"
                $problems.Add($msg)
                if (-not $ValidateOnly) {
                    if ($PSCmdlet.ShouldProcess($dll.Name, "Remove (staged/build FileVersion drift)")) {
                        Remove-Item $dll.FullName -Force
                        $removedCount++
                        Write-Host "  Removed $msg" -ForegroundColor Yellow
                    }
                }
            }
        }
    }
}

# =============================================================================
# Step 3: Report results
# =============================================================================

$totalChecked = $checkedProduct
if ($problems.Count -eq 0) {
    Write-Verbose ("No stale DLLs found (first-party checked={0})" -f $checkedProduct)
}
elseif ($ValidateOnly) {
    Write-Host ""
    Write-Host ("Stale DLL validation failed - {0} problem(s):" -f $problems.Count) -ForegroundColor Red
    foreach ($p in $problems) {
        Write-Host "  $p" -ForegroundColor Red
    }
    exit 1
}
else {
    Write-Host ("Removed {0} stale DLL(s) (first-party checked={1})" -f $removedCount, $checkedProduct) -ForegroundColor Yellow
}
