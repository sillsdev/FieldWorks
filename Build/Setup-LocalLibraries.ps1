<#
.SYNOPSIS
	Prepares local library checkouts for a FieldWorks build.

.DESCRIPTION
	Takes one or more <library>:<branch> pairs, finds each library's git
	checkout, and makes the branch available as a worktree. A branch that is
	already checked out somewhere is used where it is: git refuses to check one
	branch out twice, and another worktree may hold work in progress. Nothing is
	ever switched in a checkout that already has a branch, so no work is lost.

	The command reports the resolved paths. It never prompts, so it behaves the
	same when run unattended.

.PARAMETER Library
	One or more <name>:<branch> pairs, for example lcm:my-fix.

.PARAMETER Path
	Optional explicit checkout path, valid when a single library is named.

.EXAMPLE
	.\Build\Setup-LocalLibraries.ps1 -Library lcm:my-fix
	Makes the liblcm branch my-fix available and reports its path.

.EXAMPLE
	.\Build\Setup-LocalLibraries.ps1 -Library lcm:my-fix,palaso:my-fix
	Prepares two libraries in one call.
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string[]]$Library,

	[string]$Path
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'LocalLibraries.psm1') -Force

$repoRoot = Split-Path $PSScriptRoot -Parent
if ($Path -and $Library.Count -gt 1) {
	throw '-Path applies to a single library; name one library or drop -Path.'
}

$resolved = [ordered]@{}
foreach ($request in $Library) {
	$parts = $request -split ':', 2
	if ($parts.Count -ne 2 -or -not $parts[1]) {
		throw "Expected <library>:<branch> but got '$request' (for example lcm:my-fix)."
	}
	$name = $parts[0].Trim()
	$branch = $parts[1].Trim()

	$repo = Resolve-FieldWorksLibraryRepo -Library $name -RepositoryRoot $repoRoot `
		-ExplicitPath $Path
	Write-Host ""
	Write-Host "$name" -ForegroundColor Cyan
	Write-Host "  Checkout: $($repo.Path)  (from $($repo.Source))" -ForegroundColor Gray

	$worktree = Resolve-FieldWorksLibraryWorktree -RepositoryPath $repo.Path `
		-Branch $branch -Create
	$verb = if ($worktree.Created) { 'created' } else { 'already present' }
	Write-Host "  Branch:   $branch ($verb)" -ForegroundColor Gray
	Write-Host "  Worktree: $($worktree.Path)" -ForegroundColor Green

	$upstream = Get-FieldWorksLibraryUpstreamState -WorktreePath $worktree.Path
	if ($upstream.HasUpstream -and ($upstream.Behind -gt 0 -or $upstream.Ahead -gt 0)) {
		# Reported, not merged: a fetch cannot disturb the working tree but a
		# merge can, and this worktree may not be the one the caller is editing.
		Write-Host ("  Upstream: behind {0}, ahead {1} — reconcile it yourself" -f
			$upstream.Behind, $upstream.Ahead) -ForegroundColor Yellow
	}

	$state = Get-FieldWorksLibrarySourceState -SourceDirectory $worktree.Path
	if ($state.IsDirty) {
		Write-Host ("  Note:     {0} uncommitted change(s); builds will repack" -f
			$state.DirtyPaths.Count) -ForegroundColor Yellow
	}

	$resolved[$name] = $worktree.Path
}

Write-Host ""
Write-Host "Point this build at the worktrees:" -ForegroundColor Cyan
foreach ($name in $resolved.Keys) {
	$envVar = (Get-FieldWorksLocalLibraryConfig)[$name].EnvVar
	Write-Host ("  `$env:{0} = '{1}'" -f $envVar, $resolved[$name])
}
Write-Host ""
Write-Host ("Then build with: .\build.ps1 -LocalLibraries {0}" -f
	($resolved.Keys -join ',')) -ForegroundColor Cyan
