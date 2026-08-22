Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:LibraryConfig = [ordered]@{
	palaso = @{
		VersionProperty = 'SilLibPalasoVersion'
		PdbRelativeDir  = 'output/Debug/net462'
		CachePrefixes   = @(
			'sil.core', 'sil.windows', 'sil.dblbundle', 'sil.writingsystems',
			'sil.dictionary', 'sil.lift', 'sil.lexicon', 'sil.archiving',
			'sil.media', 'sil.scripture', 'sil.testutilities'
		)
		EnvVar          = 'LIBPALASO_PATH'
	}
	l10nsharp = @{
		VersionProperty = 'L10NSharpVersion'
		PdbRelativeDir  = 'output/Debug/net462'
		CachePrefixes   = @('l10nsharp')
		EnvVar          = 'L10NSHARP_PATH'
	}
	lcm = @{
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
	machine = @{
		VersionProperty = 'SilMachineVersion'
		PdbRelativeDir  = 'bin/Debug/netstandard2.0'
		CachePrefixes   = @('sil.machine')
		EnvVar          = 'SILMACHINE_PATH'
		PackProjects    = @(
			'src/SIL.Machine/SIL.Machine.csproj',
			'src/SIL.Machine.Morphology.HermitCrab/SIL.Machine.Morphology.HermitCrab.csproj'
		)
	}
}

function Test-ManagedPackageName {
	param([string]$Name, [string[]]$Prefixes)
	$normalizedName = $Name.ToLowerInvariant()
	foreach ($prefix in $Prefixes) {
		if ($normalizedName -eq $prefix -or $normalizedName.StartsWith("$prefix.")) {
			return $true
		}
	}
	return $false
}

function Test-FilesystemPackageSource {
	param([string]$Source)
	if ([string]::IsNullOrWhiteSpace($Source)) {
		return $false
	}
	$uri = $null
	if ([System.Uri]::TryCreate($Source, [System.UriKind]::Absolute, [ref]$uri)) {
		return $uri.IsFile
	}
	return [System.IO.Path]::IsPathRooted($Source)
}

function Get-SelectedPrefixes {
	param([string[]]$Libraries)
	$selected = if ($Libraries -and $Libraries.Count -gt 0) {
		$Libraries
	}
	else {
		@($script:LibraryConfig.Keys)
	}
	$prefixes = foreach ($library in $selected) {
		if (-not $script:LibraryConfig.Contains($library)) {
			throw "Unknown local library '$library'."
		}
		$script:LibraryConfig[$library].CachePrefixes
	}
	return @($prefixes | Sort-Object -Unique)
}

<#
.SYNOPSIS
	Removes every cached version for the selected local-library groups.
#>
function Clear-FieldWorksLibraryPackageCache {
	param([string]$PackagesDirectory, [string[]]$Libraries)
	if (-not (Test-Path -LiteralPath $PackagesDirectory)) {
		return
	}
	$prefixes = Get-SelectedPrefixes -Libraries $Libraries
	$packageDirectories = @(Get-ChildItem -LiteralPath $PackagesDirectory -Directory |
		Where-Object { Test-ManagedPackageName -Name $_.Name -Prefixes $prefixes })
	foreach ($packageDirectory in $packageDirectories) {
		Remove-Item -LiteralPath $packageDirectory.FullName -Recurse -Force
	}
	if ($packageDirectories.Count -gt 0) {
		Write-Host ("Cleared {0} package cache folders." -f $packageDirectories.Count) `
			-ForegroundColor Yellow
	}
}

<#
.SYNOPSIS
	Returns the configuration for FieldWorks-supported local libraries.
#>
function Get-FieldWorksLocalLibraryConfig {
	return $script:LibraryConfig
}

<#
.SYNOPSIS
	Removes locally sourced cache entries and managed packages from a local feed.
#>
function Clear-FieldWorksLocalLibraries {
	param(
		[string]$PackagesDirectory,
		[string]$LocalRepository,
		[string[]]$Libraries
	)

	$prefixes = Get-SelectedPrefixes -Libraries $Libraries
	$cacheRemovalCount = 0
	$feedRemovalCount = 0

	if (Test-Path -LiteralPath $PackagesDirectory) {
		$packageDirectories = @(Get-ChildItem -LiteralPath $PackagesDirectory -Directory |
			Where-Object { Test-ManagedPackageName -Name $_.Name -Prefixes $prefixes })
		foreach ($packageDirectory in $packageDirectories) {
			foreach ($versionDirectory in @(Get-ChildItem -LiteralPath $packageDirectory.FullName -Directory)) {
				$metadataPath = Join-Path $versionDirectory.FullName '.nupkg.metadata'
				if (-not (Test-Path -LiteralPath $metadataPath)) {
					continue
				}
				try {
					$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
				}
				catch {
					Write-Warning "Could not read NuGet metadata at '$metadataPath'; preserving it."
					continue
				}
				if (Test-FilesystemPackageSource -Source $metadata.source) {
					Remove-Item -LiteralPath $versionDirectory.FullName -Recurse -Force
					$cacheRemovalCount++
				}
			}
			if (@(Get-ChildItem -LiteralPath $packageDirectory.FullName -Force).Count -eq 0) {
				Remove-Item -LiteralPath $packageDirectory.FullName -Force
			}
		}
	}

	if ($LocalRepository -and (Test-Path -LiteralPath $LocalRepository)) {
		$feedPackages = @(Get-ChildItem -LiteralPath $LocalRepository -File |
			Where-Object {
				$_.Extension -in @('.nupkg', '.snupkg') -and
				(Test-ManagedPackageName -Name $_.BaseName -Prefixes $prefixes)
			})
		foreach ($feedPackage in $feedPackages) {
			Remove-Item -LiteralPath $feedPackage.FullName -Force
			$feedRemovalCount++
		}
	}

	if ($cacheRemovalCount -gt 0 -or $feedRemovalCount -gt 0) {
		Write-Host ("Cleared {0} local cache entries and {1} local feed packages." -f `
			$cacheRemovalCount, $feedRemovalCount) -ForegroundColor Yellow
	}
}

Export-ModuleMember -Function Get-FieldWorksLocalLibraryConfig,
	Clear-FieldWorksLocalLibraries, Clear-FieldWorksLibraryPackageCache
