<#
.SYNOPSIS
	Covers the local-library version stamp that LT-22728 depends on.

.DESCRIPTION
	Run by test.ps1 -LocalLibraryTests, and directly. The property under test is
	that a locally packed library can never produce the version string of a
	published package, because NuGet keys an extracted package on (id, version).
#>

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

function New-GitCheckout {
	param([string]$Path)
	New-Item -ItemType Directory -Path $Path -Force | Out-Null
	& git -C $Path init --quiet
	& git -C $Path config user.email 'test@example.com'
	& git -C $Path config user.name 'Test'
	Set-Content -LiteralPath (Join-Path $Path 'file.txt') -Value 'one'
	& git -C $Path add -A
	& git -C $Path commit --quiet -m 'initial'
}

try {
	Import-Module (Join-Path $PSScriptRoot 'LocalLibraries.psm1') -Force

	# Get-FieldWorksLocalFeedPath: the default stays inside the working tree, so
	# two worktrees cannot feed each other packages.
	$savedFeed = $env:LOCAL_NUGET_REPO
	try {
		$env:LOCAL_NUGET_REPO = $null
		$fakeRoot = 'C:' + [System.IO.Path]::DirectorySeparatorChar + 'repo'
		$expected = Join-Path $fakeRoot '.localfeed'
		Assert-True ((Get-FieldWorksLocalFeedPath -RepositoryRoot $fakeRoot) -eq $expected) `
			'the feed defaults into the working tree'
		$override = 'D:' + [System.IO.Path]::DirectorySeparatorChar + 'myfeed'
		$env:LOCAL_NUGET_REPO = $override
		Assert-True ((Get-FieldWorksLocalFeedPath -RepositoryRoot $fakeRoot) -eq $override) `
			'an existing LOCAL_NUGET_REPO still wins'
	}
	finally {
		$env:LOCAL_NUGET_REPO = $savedFeed
	}

	# ConvertTo-FieldWorksVersionLabel: a branch name has to survive as a legal
	# NuGet pre-release label.
	Assert-True ((ConvertTo-FieldWorksVersionLabel -BranchName 'feature/LT-22728') -eq
		'feature-lt-22728') 'a slash becomes a dash and the label lowercases'
	Assert-True ((ConvertTo-FieldWorksVersionLabel -BranchName 'a//b') -eq 'a-b') `
		'runs of separators collapse to one dash'
	Assert-True ((ConvertTo-FieldWorksVersionLabel -BranchName '///') -eq 'detached') `
		'a name with no usable characters falls back to detached'
	Assert-True ((ConvertTo-FieldWorksVersionLabel -BranchName ('x' * 40)).Length -le 24) `
		'a long branch name is truncated'
	$truncated = ConvertTo-FieldWorksVersionLabel -BranchName ('ab/' * 20)
	Assert-True (-not $truncated.EndsWith('-')) 'truncation never leaves a trailing dash'

	# Get-FieldWorksLocalPackVersion: the published core version must never be
	# produced on its own, which is the whole point of LT-22728.
	$clean = [pscustomobject]@{ Label = 'main'; ShortSha = 'abc1234'; IsDirty = $false }
	$dirty = [pscustomobject]@{ Label = 'main'; ShortSha = 'abc1234'; IsDirty = $true }
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2' -SourceState $clean) -eq
		'3.9.2-main.gabc1234') 'a clean checkout is stamped with its commit'
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2' -SourceState $dirty) -eq
		'3.9.2-main.dirty') 'uncommitted changes are stamped dirty, not with a commit'
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2' -SourceState $clean) -ne
		'3.9.2') 'the stamp is never the bare published version'
	Assert-True ((Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2-beta' -SourceState $clean) -eq
		'3.9.2-main.gabc1234') 'an existing pre-release suffix is replaced, not appended'

	$numericSha = [pscustomobject]@{ Label = 'main'; ShortSha = '0123456'; IsDirty = $false }
	$numericStamp = Get-FieldWorksLocalPackVersion -CoreVersion '3.9.2' -SourceState $numericSha
	Assert-True ($numericStamp -eq '3.9.2-main.g0123456') `
		'an all-digit sha keeps the g prefix, which SemVer needs to accept it'

	# Get-FieldWorksLibrarySourceState: read from a real checkout.
	$checkout = Join-Path $tempRoot 'lib'
	New-GitCheckout -Path $checkout
	$state = Get-FieldWorksLibrarySourceState -SourceDirectory $checkout
	Assert-True (-not $state.IsDirty) 'a freshly committed checkout is not dirty'
	Assert-True ($state.ShortSha.Length -ge 7) 'the short sha is recorded'
	Assert-True ($state.DirtyPaths.Count -eq 0) 'a clean checkout lists no dirty paths'

	Set-Content -LiteralPath (Join-Path $checkout 'file.txt') -Value 'two'
	$dirtyState = Get-FieldWorksLibrarySourceState -SourceDirectory $checkout
	Assert-True ($dirtyState.IsDirty) 'an edited file makes the checkout dirty'
	Assert-True ($dirtyState.DirtyPaths -contains 'file.txt') 'the dirty path is named'
	$dirtyStamp = Get-FieldWorksLocalPackVersion -CoreVersion '1.0.0' -SourceState $dirtyState
	Assert-True ($dirtyStamp -like '*.dirty') 'a dirty checkout stamps dirty, not a commit'

	$notARepo = Join-Path $tempRoot 'plain'
	New-Item -ItemType Directory -Path $notARepo -Force | Out-Null
	$threw = $false
	try { Get-FieldWorksLibrarySourceState -SourceDirectory $notARepo | Out-Null }
	catch { $threw = $true }
	Assert-True $threw 'a directory that is not a checkout fails rather than guessing'

	# Get-FieldWorksLibraryCoreVersion: without a VersionProject the pinned
	# version supplies the core, and its pre-release suffix is dropped.
	Assert-True ((Get-FieldWorksLibraryCoreVersion -SourceDirectory $checkout `
		-LibraryEntry @{} -FallbackVersion '3.9.2-old.1234567') -eq '3.9.2') `
		'the fallback core drops any existing pre-release suffix'
}
finally {
	if (Test-Path -LiteralPath $tempRoot) {
		Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
	}
}

if ($failures.Count -gt 0) {
	Write-Host "Local library tests failed:" -ForegroundColor Red
	$failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
	exit 1
}

Write-Host "[PASS] Local library version tests" -ForegroundColor Green
exit 0
