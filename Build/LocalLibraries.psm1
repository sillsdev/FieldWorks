Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Ordered, and libpalaso is first on purpose: packing order is significant
# because other libraries may depend on it. Do not alphabetise these.
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
		RepoDirectory   = 'libpalaso'
		VersionProject  = 'SIL.Core/SIL.Core.csproj'
	}
	l10nsharp = @{
		VersionProperty = 'L10NSharpVersion'
		PdbRelativeDir  = 'output/Debug/net48'
		CachePrefixes   = @('l10nsharp')
		EnvVar          = 'L10NSHARP_PATH'
		RepoDirectory   = 'l10nsharp'
		VersionProject  = 'src/L10NSharp/L10NSharp.csproj'
	}
	lcm = @{
		VersionProperty = 'SilLcmVersion'
		PdbRelativeDir  = 'artifacts/Debug/net462'
		CachePrefixes   = @('sil.lcmodel')
		EnvVar          = 'LIBLCM_PATH'
		RepoDirectory   = 'liblcm'
		VersionProject  = 'src/SIL.LCModel/SIL.LCModel.csproj'
	}
	chorus = @{
		VersionProperty = 'SilChorusVersion'
		PdbRelativeDir  = 'output/Debug/net462'
		CachePrefixes   = @('sil.chorus')
		EnvVar          = 'LIBCHORUS_PATH'
		RepoDirectory   = 'chorus'
		VersionProject  = 'src/Chorus/Chorus.csproj'
	}
	machine = @{
		VersionProperty = 'SilMachineVersion'
		PdbRelativeDir  = @(
			'src/SIL.Machine/bin/Debug/netstandard2.0',
			'src/SIL.Machine.Morphology.HermitCrab/bin/Debug/netstandard2.0'
		)
		CachePrefixes   = @('sil.machine')
		EnvVar          = 'SILMACHINE_PATH'
		RepoDirectory   = 'machine'
		# Only the projects FieldWorks uses, which avoids the native CMake
		# dependencies the rest of the repository pulls in.
		PackProjects    = @(
			'src/SIL.Machine/SIL.Machine.csproj',
			'src/SIL.Machine.Morphology.HermitCrab/SIL.Machine.Morphology.HermitCrab.csproj'
		)
	}
}

<#
.SYNOPSIS
	Returns the local NuGet feed directory for this working tree.
.DESCRIPTION
	A machine-wide feed lets one working tree's build delete packages another
	working tree just produced, so the feed defaults inside the repository.
	LOCAL_NUGET_REPO still wins when set, for existing setups.
#>
function Get-FieldWorksLocalFeedPath {
	param([string]$RepositoryRoot)
	if (-not [string]::IsNullOrWhiteSpace($env:LOCAL_NUGET_REPO)) {
		return $env:LOCAL_NUGET_REPO
	}
	return (Join-Path $RepositoryRoot '.localfeed')
}

<#
.SYNOPSIS
	Converts a git branch name into a NuGet pre-release label.
.DESCRIPTION
	SemVer pre-release identifiers allow only ASCII alphanumerics and hyphens,
	so branch separators such as '/' and '_' are replaced. The result is
	truncated because older tooling limited the whole pre-release string.
#>
function ConvertTo-FieldWorksVersionLabel {
	param([string]$BranchName)
	$label = ($BranchName -replace '[^0-9A-Za-z-]', '-').Trim('-')
	$label = $label -replace '-{2,}', '-'
	if ([string]::IsNullOrWhiteSpace($label)) {
		return 'detached'
	}
	if ($label.Length -gt 24) {
		$label = $label.Substring(0, 24).Trim('-')
	}
	return $label.ToLowerInvariant()
}

<#
.SYNOPSIS
	Describes the git state of a local library checkout.
.DESCRIPTION
	Returns the branch label, short commit and whether the tree is dirty. A
	dirty tree cannot be identified by commit, so callers must repack instead of
	trusting the version string. Untracked files count as dirty because a new
	source file changes the build without changing the commit.
#>
function Get-FieldWorksLibrarySourceState {
	param([string]$SourceDirectory)

	$branch = (& git -C $SourceDirectory rev-parse --abbrev-ref HEAD 2>$null)
	if ($LASTEXITCODE -ne 0) {
		throw "'$SourceDirectory' is not a git checkout; cannot derive a local version."
	}
	$branch = "$branch".Trim()
	if ($branch -eq 'HEAD') {
		$branch = 'detached'
	}

	$shortSha = "$(& git -C $SourceDirectory rev-parse --short=7 HEAD 2>$null)".Trim()
	$status = @(& git -C $SourceDirectory status --porcelain --untracked-files=normal 2>$null)
	$isDirty = $status.Count -gt 0

	return [pscustomobject]@{
		Branch      = $branch
		Label       = ConvertTo-FieldWorksVersionLabel -BranchName $branch
		ShortSha    = $shortSha
		IsDirty     = $isDirty
		DirtyPaths  = @($status | ForEach-Object { ($_ -replace '^.{2,3}', '').Trim() })
	}
}

<#
.SYNOPSIS
	Reports whether the feed already holds this exact packed version.
.DESCRIPTION
	A version derived from a clean commit identifies its contents, so an
	existing package for it is the same package this pack would produce. A
	dirty tree reuses one version string for changing contents, so its package
	can never stand in.
#>
function Test-FieldWorksPackIsCurrent {
	param([string]$LocalRepository, [string]$PackVersion, [pscustomobject]$SourceState)
	if ($SourceState.IsDirty) {
		return $false
	}
	if (-not (Test-Path -LiteralPath $LocalRepository)) {
		return $false
	}
	return @(Get-ChildItem -LiteralPath $LocalRepository `
		-Filter "*.$PackVersion.nupkg" -File -ErrorAction SilentlyContinue).Count -gt 0
}

<#
.SYNOPSIS
	Returns the Major.Minor.Patch a library gives itself.
.DESCRIPTION
	Read from the library's own GitVersion so a version bump in the library is
	reflected in the local package. Libraries without GitVersion have no
	VersionProject and fall back to the version FieldWorks consumes.
#>
function Get-FieldWorksLibraryCoreVersion {
	param([string]$SourceDirectory, [hashtable]$LibraryEntry, [string]$FallbackVersion)

	$fallbackCore = ($FallbackVersion -split '-', 2)[0]
	if (-not $LibraryEntry.Contains('VersionProject')) {
		return $fallbackCore
	}
	$project = Join-Path $SourceDirectory $LibraryEntry.VersionProject
	if (-not (Test-Path -LiteralPath $project)) {
		Write-Warning ("Version project '$project' not found; using $fallbackCore.")
		return $fallbackCore
	}

	# -restore first: GetVersion comes from the GitVersion package, which a
	# checkout that has never been built does not have yet.
	$probed = & dotnet msbuild $project -restore -t:GetVersion `
		-getProperty:GitVersion_MajorMinorPatch -v:q -nologo 2>$null
	$probed = @($probed | Where-Object { $_ -match '^\d+\.\d+\.\d+$' })
	if ($LASTEXITCODE -ne 0 -or $probed.Count -eq 0) {
		Write-Warning ("Could not read a GitVersion version from '$project'; " +
			"using $fallbackCore.")
		return $fallbackCore
	}
	return $probed[-1].Trim()
}

<#
.SYNOPSIS
	Builds the pre-release version for a locally packed library.
.DESCRIPTION
	A clean tree is identified by its commit, so repacking the same commit
	reuses the cached package correctly. A dirty tree has no stable identity,
	so it is labelled 'dirty' and the caller repacks every time.
#>
function Get-FieldWorksLocalPackVersion {
	param([string]$CoreVersion, [pscustomobject]$SourceState)

	$core = ($CoreVersion -split '-', 2)[0]
	if ($SourceState.IsDirty) {
		return "$core-$($SourceState.Label).dirty"
	}
	return "$core-$($SourceState.Label).$($SourceState.ShortSha)"
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
					# No metadata means extraction never finished, so nothing here can
					# be trusted and restore will replace it.
					Remove-Item -LiteralPath $versionDirectory.FullName -Recurse -Force
					$cacheRemovalCount++
					continue
				}
				$source = $null
				try {
					$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
					# Version 1 metadata records no source; reading it under
					# Set-StrictMode is a terminating error, so it stays guarded.
					$source = $metadata.PSObject.Properties['source']
					if ($source) { $source = [string]$source.Value }
				}
				catch {
					Write-Warning "Could not read NuGet metadata at '$metadataPath'; preserving it."
					continue
				}
				if (Test-FilesystemPackageSource -Source $source) {
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

<#
.SYNOPSIS
	Converts a branch name into a directory name.
.DESCRIPTION
	Branch names may contain path separators, which cannot appear in a single
	directory name. Unlike a version label this keeps the full name, because
	the directory is how a developer recognises the worktree.
#>
function ConvertTo-FieldWorksWorktreeName {
	param([string]$BranchName)
	$name = $BranchName
	foreach ($invalid in [System.IO.Path]::GetInvalidFileNameChars()) {
		$name = $name.Replace($invalid, '-')
	}
	return ($name -replace '-{2,}', '-').Trim('-')
}

<#
.SYNOPSIS
	Locates a local checkout of a library.
.DESCRIPTION
	Prefers an explicit path, then a sibling of the FieldWorks checkout, then
	the legacy environment variable. Throws with the ways to fix it rather than
	prompting, so unattended builds fail instead of waiting for input.
#>
function Resolve-FieldWorksLibraryRepo {
	param([string]$Library, [string]$RepositoryRoot, [string]$ExplicitPath)

	$entry = $script:LibraryConfig[$Library]
	if (-not $entry) {
		throw "Unknown local library '$Library'."
	}

	# Siblings sit beside the main checkout, which is not the parent of a
	# worktree, so ask git where the repository itself lives.
	$mainRoot = $RepositoryRoot
	$commonDir = & git -C $RepositoryRoot rev-parse --path-format=absolute `
		--git-common-dir 2>$null
	if ($LASTEXITCODE -eq 0 -and $commonDir) {
		$mainRoot = Split-Path ($commonDir.Trim()) -Parent
	}

	$candidates = [ordered]@{}
	if ($ExplicitPath) { $candidates['the path given'] = $ExplicitPath }
	$sibling = Join-Path (Split-Path $mainRoot -Parent) $entry.RepoDirectory
	$candidates["the sibling checkout $sibling"] = $sibling
	$fromEnv = [System.Environment]::GetEnvironmentVariable($entry.EnvVar)
	if ($fromEnv) { $candidates["$($entry.EnvVar)"] = $fromEnv }

	foreach ($source in $candidates.Keys) {
		$path = $candidates[$source]
		if (Test-Path -LiteralPath (Join-Path $path '.git')) {
			return [pscustomobject]@{ Path = (Resolve-Path -LiteralPath $path).Path
				Source = $source }
		}
	}

	throw ("No git checkout of '$Library' was found. Clone it beside FieldWorks " +
		"as '$sibling', or set $($entry.EnvVar), or pass an explicit path.")
}

<#
.SYNOPSIS
	Makes a worktree of a library branch available, or explains what is missing.
.DESCRIPTION
	An existing worktree on the branch is used as it stands, because git refuses
	to check one branch out twice and because another worktree may hold work in
	progress. Otherwise a worktree is created under .tmp/worktrees. The branch is
	never switched in a checkout that already has one, so no work can be lost.
#>
function Resolve-FieldWorksLibraryWorktree {
	param([string]$RepositoryPath, [string]$Branch, [switch]$Create)

	& git -C $RepositoryPath fetch --quiet 2>$null | Out-Null

	# Discovery, not creation: the branch may already be checked out somewhere,
	# which is both the fast path and the only path git allows.
	$listing = @(& git -C $RepositoryPath worktree list --porcelain 2>$null)
	$currentPath = $null
	foreach ($line in $listing) {
		if ($line -like 'worktree *') { $currentPath = $line.Substring(9).Trim() }
		elseif ($line -eq "branch refs/heads/$Branch") {
			return [pscustomobject]@{ Path = $currentPath; Created = $false }
		}
	}

	$hasLocal = & git -C $RepositoryPath rev-parse --verify --quiet "refs/heads/$Branch"
	$hasRemote = & git -C $RepositoryPath rev-parse --verify --quiet "refs/remotes/origin/$Branch"
	if (-not $hasLocal -and -not $hasRemote) {
		throw ("Branch '$Branch' does not exist in $RepositoryPath, locally or on " +
			"origin. Create it there first, or name an existing branch.")
	}

	$target = Join-Path $RepositoryPath (Join-Path '.tmp/worktrees' `
		(ConvertTo-FieldWorksWorktreeName -BranchName $Branch))
	if (-not $Create) {
		throw ("Branch '$Branch' has no worktree in $RepositoryPath. Run with " +
			"-SetupLocalLibraries to create one at $target.")
	}

	# Their .gitignore belongs to another team, so exclude .tmp for this clone
	# only. --git-common-dir is relative to the caller unless asked otherwise.
	$commonDir = & git -C $RepositoryPath rev-parse --path-format=absolute `
		--git-common-dir 2>$null
	if ($LASTEXITCODE -eq 0 -and $commonDir) {
		$excludePath = Join-Path $commonDir.Trim() 'info/exclude'
		$excluded = if (Test-Path -LiteralPath $excludePath) {
			@(Get-Content -LiteralPath $excludePath)
		} else { @() }
		if (-not @($excluded | Where-Object { $_.Trim() -eq '.tmp/' -or
				$_.Trim() -eq '.tmp/worktrees/' })) {
			Add-Content -LiteralPath $excludePath -Value '.tmp/'
			Write-Host "  Excluded .tmp/ for this clone only." -ForegroundColor Gray
		}
	}

	$addArgs = if ($hasLocal) { @($target, $Branch) }
		else { @('-b', $Branch, $target, "origin/$Branch") }
	& git -C $RepositoryPath worktree add @addArgs 2>&1 | Out-Null
	if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $target)) {
		throw "Could not create a worktree for '$Branch' at $target."
	}
	return [pscustomobject]@{ Path = $target; Created = $true }
}

<#
.SYNOPSIS
	Describes how a checkout stands against its upstream branch.
#>
function Get-FieldWorksLibraryUpstreamState {
	param([string]$WorktreePath)
	$counts = & git -C $WorktreePath rev-list --left-right --count '@{upstream}...HEAD' 2>$null
	if ($LASTEXITCODE -ne 0 -or -not $counts) {
		return [pscustomobject]@{ HasUpstream = $false; Behind = 0; Ahead = 0 }
	}
	$parts = -split $counts
	return [pscustomobject]@{ HasUpstream = $true
		Behind = [int]$parts[0]; Ahead = [int]$parts[1] }
}

<#
.SYNOPSIS
	Writes the generated property file that selects local libraries.
.DESCRIPTION
	Written to disk rather than passed on a command line because a restore
	launched through Exec starts a new MSBuild process, which does not inherit
	global properties. SilVersions.props imports this file, so every process
	that resolves a version sees the same answer.
#>
function Write-FieldWorksLocalLibraryProps {
	param([string]$Path, [hashtable]$Versions, [string]$LocalRepository)

	$lines = New-Object System.Collections.Generic.List[string]
	$lines.Add('<Project>')
	$lines.Add("`t<!-- Generated by build.ps1 -LocalLibraries. Do not edit or commit. -->")
	$lines.Add("`t<PropertyGroup Label=`"Local Library Overrides`">")
	foreach ($name in $Versions.Keys) {
		$lines.Add("`t`t<$name>$($Versions[$name])</$name>")
	}
	if ($LocalRepository) {
		$lines.Add("`t`t<RestoreAdditionalProjectSources>" +
			"`$(RestoreAdditionalProjectSources);$LocalRepository" +
			'</RestoreAdditionalProjectSources>')
	}
	$lines.Add("`t</PropertyGroup>")
	$lines.Add('</Project>')

	$directory = Split-Path $Path -Parent
	if ($directory -and -not (Test-Path -LiteralPath $directory)) {
		New-Item -Path $directory -ItemType Directory -Force | Out-Null
	}
	Set-Content -LiteralPath $Path -Value $lines -Encoding UTF8
}

<#
.SYNOPSIS
	Removes the generated local library property file, if it is present.
#>
function Remove-FieldWorksLocalLibraryProps {
	param([string]$Path)
	if (Test-Path -LiteralPath $Path) {
		Remove-Item -LiteralPath $Path -Force
		Write-Host 'Removed local library overrides; using published versions.' `
			-ForegroundColor Yellow
	}
}

<#
.SYNOPSIS
	Returns the path of the generated local library property file.
#>
function Get-FieldWorksLocalLibraryPropsPath {
	param([string]$RepositoryRoot)
	return (Join-Path $RepositoryRoot 'Build/LocalLibraries.props')
}

Export-ModuleMember -Function Get-FieldWorksLocalLibraryConfig,
	Clear-FieldWorksLocalLibraries, Clear-FieldWorksLibraryPackageCache,
	Get-FieldWorksLocalFeedPath, ConvertTo-FieldWorksVersionLabel,
	Get-FieldWorksLibrarySourceState, Get-FieldWorksLocalPackVersion,
	Get-FieldWorksLibraryCoreVersion, Test-FieldWorksPackIsCurrent,
	ConvertTo-FieldWorksWorktreeName, Resolve-FieldWorksLibraryRepo,
	Resolve-FieldWorksLibraryWorktree, Get-FieldWorksLibraryUpstreamState,
	Write-FieldWorksLocalLibraryProps, Remove-FieldWorksLocalLibraryProps,
	Get-FieldWorksLocalLibraryPropsPath
