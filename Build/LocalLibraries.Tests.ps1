Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$failures = New-Object System.Collections.ArrayList
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
	'FieldWorksLocalLibrariesTests_' + [System.Guid]::NewGuid().ToString('N'))

function Assert-True {
	param([bool]$Condition, [string]$Message)
	if (-not $Condition) {
		[void]$script:failures.Add("FAIL: $Message")
	}
}

function Write-PackageMetadata {
	param([string]$VersionDirectory, [string]$Source)
	New-Item -ItemType Directory -Path $VersionDirectory -Force | Out-Null
	@{ version = 2; contentHash = 'test'; source = $Source } |
		ConvertTo-Json | Set-Content -LiteralPath (
			Join-Path $VersionDirectory '.nupkg.metadata') -Encoding UTF8
}

try {
	$packagesDirectory = Join-Path $tempRoot 'packages'
	$localRepository = Join-Path $tempRoot 'feed'
	New-Item -ItemType Directory -Path $localRepository -Force | Out-Null

	$localMachine = Join-Path $packagesDirectory 'sil.machine\3.9.2'
	$publishedMachine = Join-Path $packagesDirectory 'sil.machine\3.9.3'
	$unrelatedPackage = Join-Path $packagesDirectory 'example.package\1.0.0'
	Write-PackageMetadata -VersionDirectory $localMachine -Source $localRepository
	Write-PackageMetadata -VersionDirectory $publishedMachine `
		-Source 'https://api.nuget.org/v3/index.json'
	Write-PackageMetadata -VersionDirectory $unrelatedPackage -Source $localRepository

	Set-Content -LiteralPath (Join-Path $localRepository 'SIL.Machine.3.9.2.nupkg') `
		-Value 'local package'
	Set-Content -LiteralPath (
		Join-Path $localRepository 'SIL.Machine.Morphology.HermitCrab.3.9.2.snupkg') `
		-Value 'local symbols'
	Set-Content -LiteralPath (Join-Path $localRepository 'Example.Package.1.0.0.nupkg') `
		-Value 'unrelated package'
	$managedFeedPackages = @(
		'SIL.Core.18.0.0.nupkg',
		'SIL.LCModel.11.0.0.nupkg',
		'SIL.Chorus.LibChorus.6.0.0.nupkg',
		'L10NSharp.10.0.0.nupkg'
	)
	foreach ($packageName in $managedFeedPackages) {
		Set-Content -LiteralPath (Join-Path $localRepository $packageName) `
			-Value 'managed package'
	}

	Import-Module (Join-Path $PSScriptRoot 'LocalLibraries.psm1') -Force
	$config = Get-FieldWorksLocalLibraryConfig
	Assert-True ($config.Keys.Count -eq 5) 'The catalogue should contain five libraries.'
	foreach ($library in @('palaso', 'lcm', 'chorus', 'machine', 'l10nsharp')) {
		Assert-True $config.Contains($library) "The catalogue should contain $library."
	}

	Clear-FieldWorksLocalLibraries -PackagesDirectory $packagesDirectory `
		-LocalRepository $localRepository

	Assert-True (-not (Test-Path $localMachine)) `
		'Cleanup should remove cache entries restored from a filesystem source.'
	Assert-True (Test-Path $publishedMachine) `
		'Cleanup should preserve cache entries restored from an HTTP source.'
	Assert-True (Test-Path $unrelatedPackage) `
		'Cleanup should preserve packages outside the managed library catalogue.'
	Assert-True (-not (Test-Path (
		Join-Path $localRepository 'SIL.Machine.3.9.2.nupkg'))) `
		'Cleanup should remove managed packages from the local feed.'
	Assert-True (-not (Test-Path (
		Join-Path $localRepository 'SIL.Machine.Morphology.HermitCrab.3.9.2.snupkg'))) `
		'Cleanup should remove managed symbol packages from the local feed.'
	Assert-True (Test-Path (Join-Path $localRepository 'Example.Package.1.0.0.nupkg')) `
		'Cleanup should preserve unrelated packages in the local feed.'
	foreach ($packageName in $managedFeedPackages) {
		Assert-True (-not (Test-Path (Join-Path $localRepository $packageName))) `
			"Cleanup should remove $packageName."
	}

	Clear-FieldWorksLibraryPackageCache -PackagesDirectory $packagesDirectory `
		-Libraries @('machine')
	Assert-True (-not (Test-Path $publishedMachine)) `
		'Selected packing should evict a published cache entry with the same version.'
	Assert-True (Test-Path $unrelatedPackage) `
		'Selected packing should preserve cache entries outside its package family.'

	$managerText = Get-Content -LiteralPath (
		Join-Path $PSScriptRoot 'Manage-LocalLibraries.ps1') -Raw
	Assert-True ($managerText -match '\$VersionOutputPath') `
		'Manage-LocalLibraries should accept a packed-version output path.'
	Assert-True ($managerText -match 'Import-Module.+LocalLibraries\.psm1') `
		'Manage-LocalLibraries should import the shared library catalogue.'
	Assert-True ($managerText -match 'Clear-FieldWorksLocalLibraries') `
		'Manage-LocalLibraries should use the shared cache cleanup.'
	Assert-True ($managerText -match 'Clear-FieldWorksLibraryPackageCache') `
		'Pack mode should evict selected package families before local restore.'
	Assert-True ($managerText -match 'Packing local libraries is build-scoped') `
		'Direct pack mode should direct callers to build.ps1.'
	Assert-True ($managerText -notmatch 'dotnet nuget add source') `
		'Manage-LocalLibraries should not persist a user-level NuGet source.'
	Assert-True ($managerText -match 'RestoreAdditionalProjectSources=\$LocalRepo') `
		'Local pack restores should receive the local feed for dependent libraries.'

	$buildText = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\build.ps1') -Raw
	Assert-True ($buildText -match '\[string\[\]\]\$LocalLibraries') `
		'build.ps1 should accept a LocalLibraries array.'
	Assert-True ($buildText -match 'Clear-FieldWorksLocalLibraries') `
		'build.ps1 should clean unselected local libraries before restore.'
	Assert-True ($buildText -match 'VersionOutputPath') `
		'build.ps1 should consume non-persistent packed version output.'
	Assert-True (($buildText -match 'LOCAL_NUGET_REPO') -and `
		($buildText -match 'RestoreAdditionalProjectSources')) `
		'build.ps1 should add the local feed to configured restore sources.'

	[xml]$nugetConfig = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\nuget.config')
	Assert-True ($null -ne $nugetConfig.SelectSingleNode('/configuration/packageSources/clear')) `
		'nuget.config should clear inherited user-level package sources.'
}
finally {
	if (Test-Path $tempRoot) {
		Remove-Item -LiteralPath $tempRoot -Recurse -Force
	}
}

if ($failures.Count -gt 0) {
	$failures | ForEach-Object { Write-Error $_ }
	exit 1
}

Write-Host 'Local library tests passed.' -ForegroundColor Green
