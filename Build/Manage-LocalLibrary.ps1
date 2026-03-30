<#
.SYNOPSIS
	Manages local SIL library versions for debugging in FieldWorks.

.DESCRIPTION
	Two modes of operation:

	Pack mode (one or more source paths provided):
	  Packs local checkouts of liblcm, libpalaso, and/or chorus into the
	  local NuGet feed using each library's own version. Detects the version
	  from produced packages, updates SilVersions.props to match, copies
	  PDBs, and clears stale cached packages.

	  Multiple libraries can be packed in a single call. libpalaso is always
	  packed first (other libraries may depend on it).

	SetVersion mode (-Library and -Version, no source paths):
	  Sets the version for a single library in SilVersions.props and clears
	  stale cached packages. Use this to revert to an upstream version or
	  switch to a specific version without packing.

	To revert all libraries: git checkout Build/SilVersions.props

	See Docs/architecture/local-library-debugging.md for the full workflow.

.PARAMETER LibPalasoPath
	Path to a local libpalaso checkout. Falls back to LIBPALASO_PATH env var
	if the switch -LibPalaso is used without a path.

.PARAMETER LibLcmPath
	Path to a local liblcm checkout. Falls back to LIBLCM_PATH env var
	if the switch -LibLcm is used without a path.

.PARAMETER ChorusPath
	Path to a local chorus checkout. Falls back to LIBCHORUS_PATH env var
	if the switch -Chorus is used without a path.

.PARAMETER Library
	Which library to set a version for (SetVersion mode only):
	liblcm, libpalaso, or chorus.

.PARAMETER Version
	Sets the version in SilVersions.props (SetVersion mode). Use to revert
	to an upstream version. Not used in pack mode.

.EXAMPLE
	.\Build\Manage-LocalLibrary.ps1 -LibPalasoPath C:\Repos\libpalaso
	Packs libpalaso, detects its version, and updates SilVersions.props.

.EXAMPLE
	.\Build\Manage-LocalLibrary.ps1 -LibPalasoPath C:\Repos\libpalaso -ChorusPath C:\Repos\chorus
	Packs libpalaso first, then chorus.

.EXAMPLE
	.\Build\Manage-LocalLibrary.ps1 -Library libpalaso -Version 17.0.0
	Sets libpalaso version to 17.0.0 in SilVersions.props (e.g. to revert).
#>
[CmdletBinding()]
param(
	[string]$LibPalasoPath,
	[string]$LibLcmPath,
	[string]$ChorusPath,

	[ValidateSet('liblcm', 'libpalaso', 'chorus')]
	[string]$Library,

	[string]$Version
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Library-specific configuration
# ---------------------------------------------------------------------------

$LibraryConfig = @{
	libpalaso = @{
		VersionProperty = 'SilLibPalasoVersion'
		PdbRelativeDir  = 'output/Debug/net462'
		CachePrefixes   = @(
			'sil.core', 'sil.windows', 'sil.dblbundle', 'sil.writingsystems',
			'sil.dictionary', 'sil.lift', 'sil.lexicon', 'sil.archiving',
			'sil.media', 'sil.scripture', 'sil.testutilities'
		)
		EnvVar          = 'LIBPALASO_PATH'
	}
	liblcm = @{
		VersionProperty = 'SilLcmVersion'
		PdbRelativeDir  = 'artifacts/Debug/net462'
		CachePrefixes   = @('sil.lcmodel')
		EnvVar          = 'LIBLCM_PATH'
	}
	chorus = @{
		VersionProperty = 'SilChorusVersion'
		PdbRelativeDir  = 'output/Debug/net462'
		CachePrefixes   = @('sil.chorus')
		EnvVar          = 'LIBCHORUS_PATH'
	}
}

# Pack order: libpalaso first (other libraries may depend on it)
$PackOrder = @('libpalaso', 'liblcm', 'chorus')

# ---------------------------------------------------------------------------
# Read SilVersions.props
# ---------------------------------------------------------------------------

$repoRoot = Split-Path $PSScriptRoot -Parent
$versionPropsPath = Join-Path $PSScriptRoot "SilVersions.props"
if (-not (Test-Path $versionPropsPath)) {
	throw "SilVersions.props not found at $versionPropsPath"
}

[xml]$versionProps = Get-Content -LiteralPath $versionPropsPath

# ---------------------------------------------------------------------------
# Helper: get version node for a library
# ---------------------------------------------------------------------------

function Get-VersionNode {
	param([string]$LibName)
	$cfg = $LibraryConfig[$LibName]
	$node = $versionProps.SelectSingleNode(
		"//PropertyGroup[@Label='SIL Ecosystem Versions']/$($cfg.VersionProperty)")
	if (-not $node) {
		throw "Could not find <$($cfg.VersionProperty)> in SilVersions.props"
	}
	return $node
}

# ---------------------------------------------------------------------------
# Helper: update SilVersions.props and clear stale cached packages
# ---------------------------------------------------------------------------

function Update-VersionAndClearCache {
	param([string]$LibName, [string]$NewVersion)
	$cfg = $LibraryConfig[$LibName]
	$node = Get-VersionNode $LibName
	$node.InnerText = $NewVersion
	$versionProps.Save($versionPropsPath)
	Write-Host "Updated SilVersions.props ($($cfg.VersionProperty) = $NewVersion)" -ForegroundColor Yellow

	$packagesDir = Join-Path $repoRoot "packages"
	if (Test-Path $packagesDir) {
		$patterns = $cfg.CachePrefixes | ForEach-Object { "$packagesDir/$_*" }
		$stale = @(Get-ChildItem -Path $patterns -Directory -ErrorAction SilentlyContinue)
		if ($stale.Count -gt 0) {
			$stale | Remove-Item -Recurse -Force
			Write-Host "Cleared $($stale.Count) stale package folder(s) from packages/." -ForegroundColor Yellow
		}
	}
}

# ---------------------------------------------------------------------------
# Helper: extract version from a .nupkg filename
# ---------------------------------------------------------------------------
# Split on '.', find the first segment starting with a digit — everything
# from there onward (minus .nupkg) is the version.
# E.g. SIL.Windows.Forms.Keyboarding.18.0.0-beta.nupkg → 18.0.0-beta

function Get-PackageVersion {
	param([string]$FileName)
	$base = $FileName -replace '\.nupkg$', ''
	$segments = $base -split '\.'
	for ($i = 0; $i -lt $segments.Count; $i++) {
		if ($segments[$i] -match '^\d') {
			return ($segments[$i..($segments.Count - 1)] -join '.')
		}
	}
	return $null
}

# ---------------------------------------------------------------------------
# Helper: pack a single library
# ---------------------------------------------------------------------------

function Invoke-PackLibrary {
	param([string]$LibName, [string]$SourceDir, [string]$LocalRepo)

	$cfg = $LibraryConfig[$LibName]
	$node = Get-VersionNode $LibName

	Write-Host ""
	Write-Host "========================================" -ForegroundColor Cyan
	Write-Host "Packing $LibName" -ForegroundColor Cyan
	Write-Host "  Source:     $SourceDir" -ForegroundColor Cyan
	Write-Host "  Current:    $($node.InnerText.Trim())" -ForegroundColor Cyan
	Write-Host "  Output:     $LocalRepo" -ForegroundColor Cyan
	Write-Host "========================================" -ForegroundColor Cyan

	# Record timestamp before pack so we can find newly-produced packages
	$packStart = Get-Date

	Write-Host "Running dotnet pack..." -ForegroundColor Cyan
	$packArgs = @(
		'pack'
		$SourceDir
		'-c', 'Debug'
		"-p:IncludeSymbols=true"
		"-p:SymbolPackageFormat=snupkg"
		'--output', $LocalRepo
	)

	& dotnet @packArgs
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet pack failed for $LibName."
	}

	# Find .nupkg files created after pack started (exclude .snupkg and test pkgs)
	$newPackages = @(
		Get-ChildItem -Path $LocalRepo -Filter "*.nupkg" -File |
			Where-Object { $_.LastWriteTime -ge $packStart -and $_.Extension -eq '.nupkg' -and $_.Name -notmatch 'tests' }
	)

	if ($newPackages.Count -eq 0) {
		$currentVer = (Get-VersionNode $LibName).InnerText.Trim()
		Write-Host ""
		Write-Host "WARNING: No new .nupkg files were produced for $LibName." -ForegroundColor Yellow
		Write-Host "  The library version may not have changed since the last pack." -ForegroundColor Yellow
		Write-Host "  Current version in SilVersions.props: $currentVer" -ForegroundColor Yellow
		Write-Host "  Skipping version update for $LibName." -ForegroundColor Yellow
		return
	}

	Write-Host "New packages found:" -ForegroundColor Gray
	$newPackages | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Gray }

	$detectedVersions = @($newPackages | ForEach-Object { Get-PackageVersion $_.Name } |
		Where-Object { $_ } | Sort-Object -Unique)

	Write-Host "Detected version(s): $($detectedVersions -join ', ')" -ForegroundColor Gray

	if ($detectedVersions.Count -eq 0) {
		throw "Could not parse version from produced packages: $($newPackages.Name -join ', ')"
	}
	if ($detectedVersions.Count -gt 1) {
		Write-Host "WARNING: Multiple versions detected in produced packages:" -ForegroundColor Red
		$detectedVersions | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
		throw "Expected all packages to share one version. Clean $LocalRepo and retry."
	}

	$packVersion = $detectedVersions[0]
	Write-Host ""
	Write-Host "Pack complete ($($newPackages.Count) package(s), version $packVersion)." -ForegroundColor Green

	# Update SilVersions.props and clear cache
	Update-VersionAndClearCache -LibName $LibName -NewVersion $packVersion
	Write-Host "To revert: git checkout Build/SilVersions.props" -ForegroundColor Yellow

	# Copy PDB files to Output/Debug/ and Downloads/
	$pdbSourceDir = Join-Path $SourceDir $cfg.PdbRelativeDir

	if (Test-Path $pdbSourceDir) {
		$outputDebugDir = Join-Path $repoRoot "Output/Debug"
		$downloadsDir   = Join-Path $repoRoot "Downloads"

		foreach ($dir in @($outputDebugDir, $downloadsDir)) {
			if (-not (Test-Path $dir)) {
				New-Item -Path $dir -ItemType Directory -Force | Out-Null
			}
		}

		$pdbFiles = @(Get-ChildItem -Path $pdbSourceDir -Filter "*.pdb" -File)
		if ($pdbFiles.Count -gt 0) {
			Write-Host "Copying $($pdbFiles.Count) PDB file(s) to Output/Debug/ and Downloads/..." -ForegroundColor Cyan
			$pdbFiles | Copy-Item -Destination $outputDebugDir -Force
			$pdbFiles | Copy-Item -Destination $downloadsDir -Force
		}
		else {
			Write-Host "No PDB files found in $pdbSourceDir" -ForegroundColor Yellow
		}
	}
	else {
		Write-Host "PDB source directory not found: $pdbSourceDir (PDBs will only be in .snupkg)" -ForegroundColor Yellow
	}

	Write-Host ""
	Write-Host "[OK] $LibName packed successfully." -ForegroundColor Green
}

# ===========================================================================
# Build the list of libraries to pack (in order)
# ===========================================================================

# Map parameter paths to library names
$paramPaths = @{
	libpalaso = $LibPalasoPath
	liblcm    = $LibLcmPath
	chorus    = $ChorusPath
}

# Resolve source paths from parameters or environment variables
$toPack = [ordered]@{}
foreach ($lib in $PackOrder) {
	$cfg = $LibraryConfig[$lib]
	$path = $paramPaths[$lib]
	if (-not $path) {
		$path = [System.Environment]::GetEnvironmentVariable($cfg.EnvVar)
	}
	if ($path) {
		if (-not (Test-Path $path)) {
			throw "Source path for $lib does not exist: $path"
		}
		$toPack[$lib] = $path
	}
}

# ===========================================================================
# Determine mode
# ===========================================================================

if ($toPack.Count -gt 0) {
	# -----------------------------------------------------------------------
	# Pack mode
	# -----------------------------------------------------------------------
	if ($Version) {
		Write-Host "WARNING: -Version is ignored in pack mode (version is detected from produced packages)." -ForegroundColor Yellow
	}

	$localRepo = $env:LOCAL_NUGET_REPO
	if (-not $localRepo) {
		throw "The LOCAL_NUGET_REPO environment variable is not set. Set it to a folder path (e.g. C:\localnugetpackages)."
	}
	if (-not (Test-Path $localRepo)) {
		Write-Host "Creating local NuGet repo folder: $localRepo" -ForegroundColor Yellow
		New-Item -Path $localRepo -ItemType Directory -Force | Out-Null
	}

	Write-Host ""
	Write-Host "Libraries to pack: $($toPack.Keys -join ', ')" -ForegroundColor Cyan

	foreach ($lib in $toPack.Keys) {
		Invoke-PackLibrary -LibName $lib -SourceDir $toPack[$lib] -LocalRepo $localRepo
	}

	Write-Host ""
	Write-Host "========================================" -ForegroundColor Green
	Write-Host "[OK] All libraries packed. Run .\build.ps1 to build." -ForegroundColor Green
	Write-Host "========================================" -ForegroundColor Green
}
elseif ($Library -and $Version) {
	# -----------------------------------------------------------------------
	# SetVersion mode
	# -----------------------------------------------------------------------
	$node = Get-VersionNode $Library

	Write-Host ""
	Write-Host "Manage-LocalLibrary (SetVersion)" -ForegroundColor Cyan
	Write-Host "  Library:  $Library" -ForegroundColor Cyan
	Write-Host "  Current:  $($node.InnerText.Trim())" -ForegroundColor Cyan
	Write-Host "  New:      $Version" -ForegroundColor Cyan
	Write-Host ""

	Update-VersionAndClearCache -LibName $Library -NewVersion $Version

	Write-Host ""
	Write-Host "[OK] $Library version set to $Version" -ForegroundColor Green
	Write-Host "Run .\build.ps1 to restore and build with the new version." -ForegroundColor Cyan
}
else {
	throw "Nothing to do. Provide source paths to pack, or -Library and -Version to set a version.`nExamples:`n  .\Build\Manage-LocalLibrary.ps1 -LibPalasoPath C:\Repos\libpalaso`n  .\Build\Manage-LocalLibrary.ps1 -Library libpalaso -Version 17.0.0"
}
