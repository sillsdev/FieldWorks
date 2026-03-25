<#
.SYNOPSIS
	Packs a locally-built SIL library into the local NuGet feed for debugging in FieldWorks.

.DESCRIPTION
	Builds and packs liblcm, libpalaso, or chorus in Debug configuration, using the exact
	version from Build/SilVersions.props so no version edits are needed in FieldWorks.
	The resulting .nupkg/.snupkg are placed in LOCAL_NUGET_REPO, PDB files are copied
	to Output/Debug/ and Downloads/, and stale cached packages are cleared so the next
	NuGet restore picks up the local build.

	See Docs/architecture/local-library-debugging.md for the full workflow.

.PARAMETER Library
	Which library to pack: liblcm, libpalaso, or chorus.

.PARAMETER SourcePath
	Path to the library's local checkout. Falls back to environment variables
	LIBLCM_PATH, LIBPALASO_PATH, or LIBCHORUS_PATH if not specified.

.EXAMPLE
	.\Build\Pack-LocalLibrary.ps1 -Library libpalaso -SourcePath C:\Repos\libpalaso

.EXAMPLE
	$env:LIBLCM_PATH = "C:\Repos\liblcm"
	.\Build\Pack-LocalLibrary.ps1 -Library liblcm
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[ValidateSet('liblcm', 'libpalaso', 'chorus')]
	[string]$Library,

	[string]$SourcePath
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Library-specific configuration
# ---------------------------------------------------------------------------

$LibraryConfig = @{
	liblcm = @{
		VersionProperty = 'SilLcmVersion'
		PdbRelativeDir  = 'artifacts/Debug/net462'
		CachePrefixes   = @('sil.lcmodel')
		EnvVar          = 'LIBLCM_PATH'
	}
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
	chorus = @{
		VersionProperty = 'SilChorusVersion'
		PdbRelativeDir  = 'output/Debug/net462'
		CachePrefixes   = @('sil.chorus')
		EnvVar          = 'LIBCHORUS_PATH'
	}
}

$config = $LibraryConfig[$Library]

# ---------------------------------------------------------------------------
# Resolve source path
# ---------------------------------------------------------------------------

if (-not $SourcePath) {
	$envValue = [System.Environment]::GetEnvironmentVariable($config.EnvVar)
	if ($envValue) {
		$SourcePath = $envValue
	}
	else {
		throw "No -SourcePath provided and the $($config.EnvVar) environment variable is not set."
	}
}

if (-not (Test-Path $SourcePath)) {
	throw "Source path does not exist: $SourcePath"
}

# ---------------------------------------------------------------------------
# Resolve LOCAL_NUGET_REPO
# ---------------------------------------------------------------------------

$localRepo = $env:LOCAL_NUGET_REPO
if (-not $localRepo) {
	throw "The LOCAL_NUGET_REPO environment variable is not set. Set it to a folder path (e.g. C:\localnugetpackages)."
}

if (-not (Test-Path $localRepo)) {
	Write-Host "Creating local NuGet repo folder: $localRepo" -ForegroundColor Yellow
	New-Item -Path $localRepo -ItemType Directory -Force | Out-Null
}

# ---------------------------------------------------------------------------
# Read version from SilVersions.props
# ---------------------------------------------------------------------------

$repoRoot = Split-Path $PSScriptRoot -Parent
$versionPropsPath = Join-Path $PSScriptRoot "SilVersions.props"
if (-not (Test-Path $versionPropsPath)) {
	throw "SilVersions.props not found at $versionPropsPath"
}

[xml]$versionProps = Get-Content -LiteralPath $versionPropsPath
$versionNode = $versionProps.SelectSingleNode("//PropertyGroup[@Label='SIL Ecosystem Versions']/$($config.VersionProperty)")
if (-not $versionNode) {
	throw "Could not find <$($config.VersionProperty)> in SilVersions.props"
}
$version = $versionNode.InnerText.Trim()

Write-Host ""
Write-Host "Pack-LocalLibrary" -ForegroundColor Cyan
Write-Host "  Library:    $Library" -ForegroundColor Cyan
Write-Host "  Source:     $SourcePath" -ForegroundColor Cyan
Write-Host "  Version:    $version" -ForegroundColor Cyan
Write-Host "  Output:     $localRepo" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# dotnet pack
# ---------------------------------------------------------------------------

Write-Host "Running dotnet pack..." -ForegroundColor Cyan

$packArgs = @(
	'pack'
	'-c', 'Debug'
	"-p:IncludeSymbols=true"
	"-p:SymbolPackageFormat=snupkg"
	"-p:Version=$version"
	'--output', $localRepo
)

& dotnet @packArgs --project $SourcePath
if ($LASTEXITCODE -ne 0) {
	throw "dotnet pack failed for $Library."
}

Write-Host ""
Write-Host "Pack complete." -ForegroundColor Green

# ---------------------------------------------------------------------------
# Copy PDB files to Output/Debug/ and Downloads/
# ---------------------------------------------------------------------------

$pdbSourceDir = Join-Path $SourcePath $config.PdbRelativeDir

if (Test-Path $pdbSourceDir) {
	$outputDebugDir = Join-Path $repoRoot "Output/Debug"
	$downloadsDir   = Join-Path $repoRoot "Downloads"

	foreach ($dir in @($outputDebugDir, $downloadsDir)) {
		if (-not (Test-Path $dir)) {
			New-Item -Path $dir -ItemType Directory -Force | Out-Null
		}
	}

	$pdbFiles = Get-ChildItem -Path $pdbSourceDir -Filter "*.pdb" -File
	if ($pdbFiles.Count -gt 0) {
		Write-Host "Copying $($pdbFiles.Count) PDB file(s) to Output/Debug/ and Downloads/..." -ForegroundColor Cyan
		foreach ($pdb in $pdbFiles) {
			Copy-Item -Path $pdb.FullName -Destination $outputDebugDir -Force
			Copy-Item -Path $pdb.FullName -Destination $downloadsDir -Force
		}
	}
	else {
		Write-Host "No PDB files found in $pdbSourceDir" -ForegroundColor Yellow
	}
}
else {
	Write-Host "PDB source directory not found: $pdbSourceDir (PDBs will only be in .snupkg)" -ForegroundColor Yellow
}

# ---------------------------------------------------------------------------
# Clear stale cached packages so next restore picks up the local build
# ---------------------------------------------------------------------------

$packagesDir = Join-Path $repoRoot "packages"
if (Test-Path $packagesDir) {
	$cleared = 0
	foreach ($prefix in $config.CachePrefixes) {
		$matches = Get-ChildItem -Path $packagesDir -Directory -Filter "$prefix*" -ErrorAction SilentlyContinue
		foreach ($dir in $matches) {
			Remove-Item -Path $dir.FullName -Recurse -Force
			$cleared++
		}
	}
	if ($cleared -gt 0) {
		Write-Host "Cleared $cleared stale package folder(s) from packages/." -ForegroundColor Yellow
	}
}

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "[OK] Local $Library packages are ready in $localRepo" -ForegroundColor Green
Write-Host "Run .\build.ps1 to build FieldWorks with the local packages." -ForegroundColor Cyan
