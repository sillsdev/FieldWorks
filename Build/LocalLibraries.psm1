<#
.SYNOPSIS
	Derives the version string a locally packed SIL library is stamped with.

.DESCRIPTION
	A local pack must never produce the version string a published package
	already uses. NuGet keys an extracted package on (id, version) and, once
	unpacked into the repository's packages folder, never consults the .nupkg
	again -- so a local build sharing the published version keeps satisfying
	restores after its .nupkg is deleted. Stamping the pack with the source
	state makes that collision impossible. See LT-22728.
#>

Set-StrictMode -Version Latest

function Get-FieldWorksLocalFeedPath {
	param([string]$RepositoryRoot)

	# LOCAL_NUGET_REPO still wins, so an existing setup keeps working. The
	# default lives in the working tree: no machine-level state, and two
	# worktrees cannot feed each other packages.
	if ($env:LOCAL_NUGET_REPO) {
		return $env:LOCAL_NUGET_REPO
	}
	return (Join-Path $RepositoryRoot '.localfeed')
}

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

	return [pscustomobject]@{
		Branch     = $branch
		Label      = ConvertTo-FieldWorksVersionLabel -BranchName $branch
		ShortSha   = $shortSha
		IsDirty    = $status.Count -gt 0
		DirtyPaths = @($status | ForEach-Object { ($_ -replace '^.{2,3}', '').Trim() })
	}
}

function Get-FieldWorksLibraryCoreVersion {
	param([string]$SourceDirectory, [hashtable]$LibraryEntry, [string]$FallbackVersion)

	$fallbackCore = ($FallbackVersion -split '-', 2)[0]
	if (-not $LibraryEntry.Contains('VersionProject')) {
		return $fallbackCore
	}
	$project = Join-Path $SourceDirectory $LibraryEntry.VersionProject
	if (-not (Test-Path -LiteralPath $project)) {
		Write-Warning "Version project '$project' not found; using $fallbackCore."
		return $fallbackCore
	}

	# -restore first: GetVersion comes from the GitVersion package, which a
	# checkout that has never been built does not have yet.
	$probed = & dotnet msbuild $project -restore -t:GetVersion `
		-getProperty:GitVersion_MajorMinorPatch -v:q -nologo 2>$null
	$probed = @($probed | Where-Object { $_ -match '^\d+\.\d+\.\d+$' })
	if ($LASTEXITCODE -ne 0 -or $probed.Count -eq 0) {
		Write-Warning "Could not read a GitVersion version from '$project'; using $fallbackCore."
		return $fallbackCore
	}
	return $probed[-1].Trim()
}

function Get-FieldWorksLocalPackVersion {
	param([string]$CoreVersion, [pscustomobject]$SourceState)

	$core = ($CoreVersion -split '-', 2)[0]
	if ($SourceState.IsDirty) {
		return "$core-$($SourceState.Label).dirty"
	}
	return "$core-$($SourceState.Label).$($SourceState.ShortSha)"
}

Export-ModuleMember -Function Get-FieldWorksLocalFeedPath, ConvertTo-FieldWorksVersionLabel,
	Get-FieldWorksLibrarySourceState, Get-FieldWorksLibraryCoreVersion,
	Get-FieldWorksLocalPackVersion
