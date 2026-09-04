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
	Assert-True (($buildText -match 'Get-FieldWorksLocalFeedPath') -and `
		($buildText -match 'RestoreAdditionalProjectSources')) `
		'build.ps1 should add the resolved local feed to configured restore sources.'
	Assert-True ($buildText -notmatch '\$env:LOCAL_NUGET_REPO') `
		'build.ps1 should resolve the feed through the module, not the env var.'

	# --- Feed location -----------------------------------------------------
	# A machine-wide feed lets one working tree delete another's fresh packages.
	$savedFeed = $env:LOCAL_NUGET_REPO
	try {
		$env:LOCAL_NUGET_REPO = $null
		Assert-True ((Get-FieldWorksLocalFeedPath -RepositoryRoot 'C:\wt') -eq `
			'C:\wt\.localfeed') `
			'The feed should default inside the working tree.'
		$env:LOCAL_NUGET_REPO = 'C:/legacy/feed'
		Assert-True ((Get-FieldWorksLocalFeedPath -RepositoryRoot 'C:\wt') -eq `
			'C:/legacy/feed') `
			'An explicit LOCAL_NUGET_REPO should still win.'
	}
	finally {
		$env:LOCAL_NUGET_REPO = $savedFeed
	}

	# --- Version labels ----------------------------------------------------
	# SemVer pre-release identifiers allow only alphanumerics and hyphens.
	Assert-True ((ConvertTo-FieldWorksVersionLabel -BranchName 'feature/x_y') -eq `
		'feature-x-y') 'Branch separators should become hyphens.'
	Assert-True ((ConvertTo-FieldWorksVersionLabel -BranchName 'LT-1/2') -eq `
		'lt-1-2') 'Labels should be lowercased.'
	Assert-True ((ConvertTo-FieldWorksVersionLabel `
		-BranchName ('a' * 60)).Length -le 24) 'Long branches should truncate.'
	Assert-True ((ConvertTo-FieldWorksVersionLabel -BranchName '///') -eq `
		'detached') 'A branch with no usable characters should be named.'

	# --- Pack versions ---
	# A clean tree is identified by its commit, so its cache entry may be
	# reused; a dirty tree has no stable identity.
	$cleanState = [pscustomobject]@{
		Branch = 'feature/x'; Label = 'feature-x'; ShortSha = 'abc1234'; IsDirty = $false }
	$dirtyState = [pscustomobject]@{
		Branch = 'feature/x'; Label = 'feature-x'; ShortSha = 'abc1234'; IsDirty = $true }
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2' `
		-SourceState $cleanState) -eq '3.9.2-feature-x.abc1234') `
		'A clean pack should be identified by branch and commit.'
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '11.0.0-beta0178' `
		-SourceState $cleanState) -eq '11.0.0-feature-x.abc1234') `
		'A published pre-release label should be replaced, not appended to.'
	# The core comes from the library's own GitVersion where it has one, so a
	# version bump in the library shows up in the local package.
	$config = Get-FieldWorksLocalLibraryConfig
	Assert-True ($config['lcm'].VersionProject -eq 'src/SIL.LCModel/SIL.LCModel.csproj') `
		'GitVersion libraries should name a project to read their version from.'
	Assert-True (-not $config['machine'].Contains('VersionProject')) `
		'A library without GitVersion should fall back to the consumed version.'
	Assert-True ((Get-FieldWorksLibraryCoreVersion -SourceDirectory 'C:
ope' `
		-LibraryEntry $config['machine'] -FallbackVersion '3.9.2') -eq '3.9.2') `
		'The fallback should be the consumed version core.'
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2' `
		-SourceState $dirtyState) -eq '3.9.2-feature-x.dirty') `
		'A dirty pack should be marked dirty rather than pinned to a commit.'
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2' `
		-SourceState $cleanState) -ne '3.9.2') `
		'A local pack must never reuse the published version string.'

	Assert-True ($managerText -match '-p:Version=\$packVersion') `
		'Pack should stamp the derived version instead of sniffing filenames.'
	Assert-True ($managerText -notmatch 'function Get-PackageVersion') `
		'Pack should not parse versions out of filenames any more.'
	# An earlier local pack is filesystem-sourced, so the pre-pack cleanup
	# already removes it; clearing again would evict published packages too.
	Assert-True (([regex]::Matches($managerText,
		'Clear-FieldWorksLibraryPackageCache')).Count -eq 1) `
		'Only SetVersion mode should clear every cached version of a library.'

	# --- Pack reuse ---
	# A clean commit identifies its contents, so an existing package for that
	# version is the package this pack would produce.
	$reuseFeed = Join-Path $tempRoot 'reusefeed'
	New-Item -ItemType Directory -Force $reuseFeed | Out-Null
	Set-Content -LiteralPath (Join-Path $reuseFeed 'SIL.Machine.3.9.2-b.abc1234.nupkg') `
		-Value 'x'
	Assert-True (Test-FieldWorksPackIsCurrent -LocalRepository $reuseFeed `
		-PackVersion '3.9.2-b.abc1234' -SourceState $cleanState) `
		'An existing package for a clean commit should be reused.'
	Assert-True (-not (Test-FieldWorksPackIsCurrent -LocalRepository $reuseFeed `
		-PackVersion '3.9.2-b.abc1234' -SourceState $dirtyState)) `
		'A dirty tree must never reuse an existing package.'
	Assert-True (-not (Test-FieldWorksPackIsCurrent -LocalRepository $reuseFeed `
		-PackVersion '3.9.2-b.9999999' -SourceState $cleanState)) `
		'A different commit should not be satisfied by another version.'
	Assert-True ($managerText -match 'Reusing the packed') `
		'Pack should report when it reuses an existing package.'
	Assert-True ($managerText -match 'Uncommitted changes force a repack') `
		'Pack should name the paths that force a repack.'

	Assert-True ($config['lcm'].RepoDirectory -eq 'liblcm') `
		'Each library should name its sibling checkout directory.'

	# --- One catalogue, three declarations ---
	# The library names appear in the catalogue and in two ValidateSets, so
	# each must agree with the catalogue.
	$catalogue = @($config.Keys | Sort-Object)
	foreach ($source in @(@{ n = 'build.ps1'; t = $buildText },
			@{ n = 'Manage-LocalLibraries.ps1'; t = $managerText })) {
		$declared = [regex]::Matches($source.t,
			"\[ValidateSet\((?<set>'(?:palaso|lcm|chorus|machine|l10nsharp)'[^)]*)\)\]")
		Assert-True ($declared.Count -ge 1) `
			"$($source.n) should declare the library names."
		if ($declared.Count -ge 1) {
			$names = @([regex]::Matches($declared[0].Groups['set'].Value, "'([^']+)'") |
				ForEach-Object { $_.Groups[1].Value } | Sort-Object)
			Assert-True (($names -join ',') -eq ($catalogue -join ',')) `
				"$($source.n) ValidateSet should match the catalogue exactly."
		}
	}

	# --- Odd cache entries ---
	# Version 1 metadata records no source, and reading a missing property
	# under Set-StrictMode is terminating.
	$oddRoot = Join-Path $tempRoot 'odd'
	$v1 = Join-Path $oddRoot 'packages/sil.lcmodel/1.0.0'
	New-Item -ItemType Directory -Path $v1 -Force | Out-Null
	'{ "version": 1, "contentHash": "abc" }' |
		Set-Content -LiteralPath (Join-Path $v1 '.nupkg.metadata') -Encoding UTF8
	$partial = Join-Path $oddRoot 'packages/sil.lcmodel/2.0.0'
	New-Item -ItemType Directory -Path $partial -Force | Out-Null
	Clear-FieldWorksLocalLibraries -PackagesDirectory (Join-Path $oddRoot 'packages') `
		-LocalRepository (Join-Path $oddRoot 'feed') -Libraries @('lcm')
	Assert-True (Test-Path -LiteralPath $v1) `
		'Version 1 metadata should be preserved, not crash the build.'
	Assert-True (-not (Test-Path -LiteralPath $partial)) `
		'A version directory with no metadata is a partial extraction and should go.'

	# --- Reaching a restore this build does not launch ---
	# A restore run through Exec is a new process, so an override has to be on
	# disk where every process reads it, not on a command line.
	$silVersions = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SilVersions.props') -Raw
	Assert-True ($silVersions -match 'LocalLibraries\.props') `
		'SilVersions.props should import the generated local library overrides.'
	Assert-True ($silVersions.IndexOf('LocalLibraries.props') -gt `
		$silVersions.IndexOf('SilLcmVersion')) `
		'The override must be imported after the defaults so that it wins.'
	$propsPath = Join-Path $tempRoot 'gen/Build/LocalLibraries.props'
	Write-FieldWorksLocalLibraryProps -Path $propsPath `
		-Versions ([ordered]@{ SilLcmVersion = '9.9.9-x.abc1234' }) `
		-LocalRepository 'C:/feed'
	[xml]$generated = Get-Content -LiteralPath $propsPath
	Assert-True ($generated.Project.PropertyGroup.SilLcmVersion -eq '9.9.9-x.abc1234') `
		'The generated overrides should carry the packed version.'
	Assert-True ($generated.Project.PropertyGroup.RestoreAdditionalProjectSources `
		-match 'C:/feed') 'The generated overrides should carry the local feed.'
	Remove-FieldWorksLocalLibraryProps -Path $propsPath
	Assert-True (-not (Test-Path -LiteralPath $propsPath)) `
		'A build selecting no library should remove the overrides.'
	Assert-True ($buildText -match 'Remove-FieldWorksLocalLibraryProps') `
		'Every build should clear a previous selection before restoring.'

	# --- Symbol directories ---
	# A directory that does not exist copies nothing and says nothing, so the
	# configured paths have to match where each library actually writes.
	foreach ($name in $config.Keys) {
		foreach ($relative in @($config[$name].PdbRelativeDir)) {
			Assert-True ($relative -notmatch 'net462$' -or
				$name -in @('palaso', 'lcm', 'chorus')) `
				"$name should not claim a net462 symbol directory."
		}
	}
	Assert-True (@($config['machine'].PdbRelativeDir).Count -eq 2) `
		'A library with per-project output needs one symbol directory per project.'
	Assert-True ($config['l10nsharp'].PdbRelativeDir -eq 'output/Debug/net48') `
		'L10NSharp symbols should come from a framework it actually builds.'
	Assert-True ($managerText -match 'No PDB files found for') `
		'A missing symbol directory should be reported, not passed over.'

	$testText = Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) 'test.ps1') -Raw
	Assert-True ($testText -match 'if \(-not \$TestProject -and -not \$TestFilter\)') `
		'A targeted test run should not also run the local library tests.'

	[xml]$nugetConfig = Get-Content -LiteralPath (
		Join-Path (Split-Path $PSScriptRoot -Parent) 'nuget.config')
	# A local build adds its feed for that build only, so inherited
	# sources can stay and a developer's private feed keeps working.
	Assert-True ($null -eq $nugetConfig.SelectSingleNode(
			'/configuration/packageSources/clear')) `
		'nuget.config should leave inherited package sources in place.'
}
finally {
	if (Test-Path $tempRoot) {
		Remove-Item -LiteralPath $tempRoot -Recurse -Force
	}
}

if ($failures.Count -gt 0) {
	# Write-Host, not Write-Error: this script sets ErrorActionPreference to
	# Stop, which would make the first failure terminating and hide the rest.
	$failures | ForEach-Object { Write-Host $_ -ForegroundColor Red }
	Write-Host ("{0} local library test(s) failed." -f $failures.Count) `
		-ForegroundColor Red
	exit 1
}

Write-Host 'Local library tests passed.' -ForegroundColor Green
