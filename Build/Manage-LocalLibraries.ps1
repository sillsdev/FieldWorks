<#
.SYNOPSIS
	Manages local SIL library versions for debugging in FieldWorks.

.DESCRIPTION
	Two modes of operation:

	Build pack mode (one or more source paths and -VersionOutputPath provided):
	  Packs local checkouts of liblcm, libpalaso, chorus, machine, and/or
	  L10NSharp into the local NuGet feed using each library's own version.
	  Detects the versions, writes them for build.ps1, copies PDBs, and clears
	  stale cached packages.

	  Multiple libraries can be packed in a single call. libpalaso is always
	  packed first (other libraries may depend on it).

	SetVersion mode (-Library and -Version, no source paths):
	  Sets the version for a single library in SilVersions.props and clears
	  stale cached packages. Use this to revert to an upstream version or
	  switch to a specific version without packing.

	To revert all libraries: git checkout Build/SilVersions.props

	See Docs/architecture/local-library-debugging.md for the full workflow.

.PARAMETER Palaso
	Switch: include libpalaso in the pack operation.

.PARAMETER PalasoPath
	Path to a local libpalaso checkout. Overrides LIBPALASO_PATH env var.
	Only used when -Palaso is specified.

.PARAMETER Lcm
	Switch: include liblcm in the pack operation.

.PARAMETER LcmPath
	Path to a local liblcm checkout. Overrides LIBLCM_PATH env var.
	Only used when -Lcm is specified.

.PARAMETER Chorus
	Switch: include chorus in the pack operation.

.PARAMETER ChorusPath
	Path to a local chorus checkout. Overrides LIBCHORUS_PATH env var.
	Only used when -Chorus is specified.

.PARAMETER Machine
	Switch: include SIL.Machine in the pack operation.

.PARAMETER MachinePath
	Path to a local machine checkout. Overrides SILMACHINE_PATH env var.
	Only used when -Machine is specified.

.PARAMETER L10nSharp
	Switch: include L10NSharp in the pack operation.

.PARAMETER L10nSharpPath
	Path to a local L10NSharp checkout. Overrides L10NSHARP_PATH env var.
	Only used when -L10nSharp is specified.

.PARAMETER Library
	Which library to set a version for (SetVersion mode only):
	lcm, palaso, chorus, machine, or l10nsharp.

.PARAMETER Version
	Sets the version in SilVersions.props (SetVersion mode). Use to revert
	to an upstream version. Not used in pack mode.

.PARAMETER VersionOutputPath
	Path to write the packed versions to, as JSON keyed by version property.

.EXAMPLE
	.\build.ps1 -LocalLibraries palaso
	Rebuilds libpalaso from LIBPALASO_PATH for this FieldWorks build.

.EXAMPLE
	.\build.ps1 -LocalLibraries palaso,chorus
	Rebuilds libpalaso and chorus from their configured paths for this build.

.EXAMPLE
	.\Build\Manage-LocalLibraries.ps1 -Library palaso -Version 17.0.0
	Sets libpalaso version to 17.0.0 in SilVersions.props (e.g. to revert).
#>
[CmdletBinding()]
param(
	[switch]$Palaso,
	[string]$PalasoPath,

	[switch]$Lcm,
	[string]$LcmPath,

	[switch]$Chorus,
	[string]$ChorusPath,

	[switch]$Machine,
	[string]$MachinePath,

	[switch]$L10nSharp,
	[string]$L10nSharpPath,

	[ValidateSet('palaso', 'lcm', 'chorus', 'machine', 'l10nsharp')]
	[string]$Library,

	[string]$Version,

	[string]$VersionOutputPath,

	[string]$LocalFeedPath
)

$ErrorActionPreference = "Stop"
Import-Module (Join-Path $PSScriptRoot 'LocalLibraries.psm1') -Force
$LibraryConfig = Get-FieldWorksLocalLibraryConfig
# Pack order: libpalaso first, because other libraries may depend on it. The
# order comes from the catalogue's declaration order, so do not alphabetise it.
$PackOrder = @($LibraryConfig.Keys)
$packedVersions = [ordered]@{}

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

	# Save with XmlWriter to preserve tab indentation (XmlDocument.Save() converts tabs to spaces)
	$writerSettings = New-Object System.Xml.XmlWriterSettings
	$writerSettings.Indent = $true
	$writerSettings.IndentChars = "`t"
	$writerSettings.NewLineChars = "`r`n"
	$writerSettings.Encoding = New-Object System.Text.UTF8Encoding($false) # UTF-8 without BOM
	$writerSettings.OmitXmlDeclaration = -not $versionProps.FirstChild.NodeType.Equals([System.Xml.XmlNodeType]::XmlDeclaration)
	$writer = [System.Xml.XmlWriter]::Create($versionPropsPath, $writerSettings)
	try {
		$versionProps.WriteTo($writer)
	} finally {
		$writer.Close()
	}

	Write-Host "Updated SilVersions.props ($($cfg.VersionProperty) = $NewVersion)" -ForegroundColor Yellow

	Clear-FieldWorksLibraryPackageCache -PackagesDirectory (Join-Path $repoRoot 'packages') `
		-Libraries @($LibName)
}

# --- Copy a library's PDBs next to the build output ---

function Copy-LibrarySymbols {
	param([string]$LibName, [string]$SourceDir)
	$cfg = $LibraryConfig[$LibName]

	$outputDebugDir = Join-Path $repoRoot "Output/Debug"
	$downloadsDir   = Join-Path $repoRoot "Downloads"
	$copied = 0

	# A library that writes per-project output needs one directory per project.
	foreach ($relativeDir in @($cfg.PdbRelativeDir)) {
		$pdbSourceDir = Join-Path $SourceDir $relativeDir
		if (-not (Test-Path $pdbSourceDir)) {
			continue
		}
		$pdbFiles = @(Get-ChildItem -Path $pdbSourceDir -Filter "*.pdb" -File)
		if ($pdbFiles.Count -eq 0) {
			continue
		}
		foreach ($dir in @($outputDebugDir, $downloadsDir)) {
			if (-not (Test-Path $dir)) {
				New-Item -Path $dir -ItemType Directory -Force | Out-Null
			}
		}
		$pdbFiles | Copy-Item -Destination $outputDebugDir -Force
		$pdbFiles | Copy-Item -Destination $downloadsDir -Force
		$copied += $pdbFiles.Count
	}

	if ($copied -gt 0) {
		Write-Host "Copied $copied PDB file(s) to Output/Debug/ and Downloads/..." `
			-ForegroundColor Cyan
	}
	else {
		# Say where we looked: a wrong directory here fails silently otherwise.
		Write-Host ("No PDB files found for {0} under: {1}" -f $LibName,
			((@($cfg.PdbRelativeDir)) -join ', ')) -ForegroundColor Yellow
	}
}

# ---------------------------------------------------------------------------
# Helper: pack a single library
# ---------------------------------------------------------------------------

function Invoke-PackLibrary {
	param([string]$LibName, [string]$SourceDir, [string]$LocalRepo)

	$cfg = $LibraryConfig[$LibName]
	$node = Get-VersionNode $LibName

	$packTargets = if ($cfg.PackProjects -and $cfg.PackProjects.Count -gt 0) {
		@($cfg.PackProjects | ForEach-Object { Join-Path $SourceDir $_ })
	} else { @($SourceDir) }

	# NuGet resolves an extracted (id, version) before consulting a folder feed,
	# so a local pack must never reuse the published version string.
	$sourceState = Get-FieldWorksLibrarySourceState -SourceDirectory $SourceDir
	$coreVersion = Get-FieldWorksLibraryCoreVersion -SourceDirectory $SourceDir `
		-LibraryEntry $cfg -FallbackVersion $node.InnerText.Trim()
	$packVersion = Get-FieldWorksLocalPackVersion `
		-CoreVersion $coreVersion -SourceState $sourceState

	Write-Host ""
	Write-Host "========================================" -ForegroundColor Cyan
	Write-Host "Packing $LibName" -ForegroundColor Cyan
	Write-Host "  Source:     $SourceDir" -ForegroundColor Cyan
	$dirtyNote = if ($sourceState.IsDirty) { ' (dirty)' } else { '' }
	Write-Host "  Branch:     $($sourceState.Branch)$dirtyNote" -ForegroundColor Cyan
	Write-Host "  Commit:     $($sourceState.ShortSha)" -ForegroundColor Cyan
	Write-Host "  Current:    $($node.InnerText.Trim())" -ForegroundColor Cyan
	Write-Host "  Packing as: $packVersion" -ForegroundColor Cyan
	Write-Host "  Output:     $LocalRepo" -ForegroundColor Cyan
	Write-Host "========================================" -ForegroundColor Cyan

	if ($sourceState.IsDirty) {
		# Name the paths: while the tree is dirty every build repacks, so the
		# way to a reusable package is to commit or remove these.
		$shown = @($sourceState.DirtyPaths | Select-Object -First 5)
		Write-Host ("Uncommitted changes force a repack ({0}):" -f
			$sourceState.DirtyPaths.Count) -ForegroundColor Yellow
		$shown | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
		if ($sourceState.DirtyPaths.Count -gt $shown.Count) {
			Write-Host ("  ... and {0} more" -f
				($sourceState.DirtyPaths.Count - $shown.Count)) -ForegroundColor Yellow
		}
	}
	elseif (Test-FieldWorksPackIsCurrent -LocalRepository $LocalRepo `
			-PackVersion $packVersion -SourceState $sourceState) {
		Write-Host "Reusing the packed $packVersion (commit unchanged)." `
			-ForegroundColor Green
		$script:packedVersions[$cfg.VersionProperty] = $packVersion
		Copy-LibrarySymbols -LibName $LibName -SourceDir $SourceDir
		return
	}

	# Only the library being repacked loses its local packages.
	Clear-FieldWorksLocalLibraries -PackagesDirectory (Join-Path $repoRoot 'packages') `
		-LocalRepository $LocalRepo -Libraries @($LibName)

	# Build first: a package may include output from a framework its own project
	# does not build, which pack alone does not produce.
	$commonBuildArgs = @(
		'-c', 'Debug'
		"-p:Version=$packVersion"
		'-p:DisableGitVersionTask=true'
		"-p:RestoreAdditionalProjectSources=$LocalRepo"
	)
	Write-Host "Running dotnet build..." -ForegroundColor Cyan
	foreach ($buildTarget in $packTargets) {
		& dotnet build $buildTarget @commonBuildArgs
		if ($LASTEXITCODE -ne 0) {
			throw "dotnet build failed for $LibName ($buildTarget)."
		}
	}

	Write-Host "Running dotnet pack..." -ForegroundColor Cyan
	$commonPackArgs = @(
		'-c', 'Debug'
		"-p:Version=$packVersion"
		# GitVersion.MsBuild assigns Version inside a target, which outranks a
		# command-line property, so it must be switched off for the stamp to hold.
		'-p:DisableGitVersionTask=true'
		"-p:IncludeSymbols=true"
		"-p:SymbolPackageFormat=snupkg"
		"-p:RestoreAdditionalProjectSources=$LocalRepo"
		'--output', $LocalRepo
	)

	foreach ($packTarget in $packTargets) {
		& dotnet pack $packTarget @commonPackArgs
		if ($LASTEXITCODE -ne 0) {
			throw "dotnet pack failed for $LibName ($packTarget)."
		}
	}

	# Verify the stamped version actually reached the feed.
	$produced = @(
		Get-ChildItem -Path $LocalRepo -Filter "*.$packVersion.nupkg" -File |
			Where-Object { $_.Name -notmatch 'tests' }
	)
	if ($produced.Count -eq 0) {
		throw ("dotnet pack produced no package for $LibName at version " +
			"$packVersion. Inspect $LocalRepo.")
	}

	Write-Host ""
	Write-Host "Pack complete ($($produced.Count) package(s), version $packVersion)." `
		-ForegroundColor Green

	$script:packedVersions[$cfg.VersionProperty] = $packVersion

	Copy-LibrarySymbols -LibName $LibName -SourceDir $SourceDir

	Write-Host ""
	Write-Host "[OK] $LibName packed successfully." -ForegroundColor Green
}

# ===========================================================================
# Build the list of libraries to pack (in order)
# ===========================================================================

# Map switches and explicit paths to library names
$switchMap = @{
	palaso = @{ Enabled = [bool]$Palaso;  ExplicitPath = $PalasoPath }
	lcm    = @{ Enabled = [bool]$Lcm;     ExplicitPath = $LcmPath }
	chorus    = @{ Enabled = [bool]$Chorus;   ExplicitPath = $ChorusPath }
	machine   = @{ Enabled = [bool]$Machine;  ExplicitPath = $MachinePath }
	l10nsharp = @{ Enabled = [bool]$L10nSharp; ExplicitPath = $L10nSharpPath }
}

# Resolve source paths: explicit path > env var (only when switch is set)
$toPack = [ordered]@{}
foreach ($lib in $PackOrder) {
	$sw = $switchMap[$lib]
	if (-not $sw.Enabled) { continue }

	$cfg = $LibraryConfig[$lib]
	$path = $sw.ExplicitPath
	if (-not $path) {
		$path = [System.Environment]::GetEnvironmentVariable($cfg.EnvVar)
	}
	if (-not $path) {
		throw "-$($lib) was specified but no path was provided and $($cfg.EnvVar) is not set."
	}
	if (-not (Test-Path $path)) {
		throw "Source path for $lib does not exist: $path"
	}
	$toPack[$lib] = $path
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
	if (-not $VersionOutputPath) {
		throw "Packing local libraries is build-scoped. Run .\build.ps1 -LocalLibraries <name>."
	}

	$localRepo = $LocalFeedPath
	if (-not $localRepo) {
		$localRepo = Get-FieldWorksLocalFeedPath -RepositoryRoot $repoRoot
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
	$versionOutputDirectory = Split-Path $VersionOutputPath -Parent
	if ($versionOutputDirectory -and -not (Test-Path $versionOutputDirectory)) {
		New-Item -Path $versionOutputDirectory -ItemType Directory -Force | Out-Null
	}
	$packedVersions | ConvertTo-Json | Set-Content -LiteralPath $VersionOutputPath `
		-Encoding UTF8

	# The property file, not the command line, is what a nested restore can see.
	Write-FieldWorksLocalLibraryProps `
		-Path (Get-FieldWorksLocalLibraryPropsPath -RepositoryRoot $repoRoot) `
		-Versions $packedVersions -LocalRepository $localRepo

	Write-Host ""
	Write-Host "========================================" -ForegroundColor Green
	Write-Host "[OK] Selected local libraries packed for this build." -ForegroundColor Green
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
	throw "Nothing to do. Use .\build.ps1 -LocalLibraries <name> to pack local libraries, or -Library and -Version to set a version.`nExamples:`n  .\build.ps1 -LocalLibraries palaso`n  .\build.ps1 -LocalLibraries palaso,chorus`n  .\build.ps1 -LocalLibraries machine`n  .\Build\Manage-LocalLibraries.ps1 -Library l10nsharp -Version 10.0.0`n  .\Build\Manage-LocalLibraries.ps1 -Library palaso -Version 17.0.0"
}
