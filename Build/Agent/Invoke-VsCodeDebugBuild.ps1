[CmdletBinding()]
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',
	[switch]$LocalPalaso,
	[switch]$LocalLcm,
	[switch]$LocalChorus,
	[string]$LocalPackageVersion = '99.0.0-local',
	[ValidateSet('full', 'portable', 'pdbonly', 'embedded')]
	[string]$ManagedDebugType = 'portable'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$outputDir = Join-Path $repoRoot ("Output\{0}" -f $Configuration)
$stampPath = Join-Path $outputDir 'BuildStamp.json'
$runtimeExePath = Join-Path $outputDir 'FieldWorks.exe'

function Get-DebugRebuildCheckPathspecs {
	$pathspecs = @(
		'build.ps1',
		'test.ps1',
		'nuget.config',
		'.vscode',
		'Directory.Build.props',
		'Directory.Build.targets',
		'Directory.Packages.props',
		'Build/SilVersions.props',
		'Build/SilVersions.Local.props',
		'FieldWorks.proj',
		'FieldWorks.sln',
		'Build',
		'Src',
		'Lib'
	)

	return $pathspecs | ForEach-Object { $_ -replace '\\', '/' }
}

function Get-SelectedLocalDependencies {
	$selectedDependencies = @()
	if ($LocalPalaso) {
		$selectedDependencies += 'Palaso'
	}
	if ($LocalLcm) {
		$selectedDependencies += 'Lcm'
	}
	if ($LocalChorus) {
		$selectedDependencies += 'Chorus'
	}

	return $selectedDependencies
}

function Get-RepoEnvironmentVariableName {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName
	)

	switch ($DependencyName) {
		'Palaso' { return 'FW_LOCAL_PALASO' }
		'Lcm' { return 'FW_LOCAL_LCM' }
		'Chorus' { return 'FW_LOCAL_CHORUS' }
		default { throw "Unknown local dependency '$DependencyName'." }
	}
}

function Get-StringHash {
	param(
		[AllowEmptyString()]
		[string]$Value
	)

	$sha256 = [System.Security.Cryptography.SHA256]::Create()
	try {
		$bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
		$hashBytes = $sha256.ComputeHash($bytes)
		return ([System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant())
	}
	finally {
		$sha256.Dispose()
	}
}

function Get-DependencyRepoFingerprint {
	param(
		[Parameter(Mandatory = $true)]
		[string]$RepoPath
	)

	$resolvedRepoPath = [System.IO.Path]::GetFullPath($RepoPath)
	$gitHeadOutput = @(& git -c core.safecrlf=false -c core.autocrlf=false -C $resolvedRepoPath rev-parse HEAD 2>$null)
	if ($LASTEXITCODE -ne 0 -or $gitHeadOutput.Count -eq 0) {
		throw "Could not determine git HEAD for local dependency repo '$resolvedRepoPath'."
	}

	$gitHead = ($gitHeadOutput -join "`n").Trim()
	$statusLines = @(& git -c core.safecrlf=false -c core.autocrlf=false -C $resolvedRepoPath status --porcelain=v1 --untracked-files=all 2>$null)
	if ($LASTEXITCODE -ne 0) {
		throw "Could not determine git status for local dependency repo '$resolvedRepoPath'."
	}

	if ($statusLines.Count -eq 0) {
		return [pscustomobject]@{
			RepoPath = $resolvedRepoPath
			GitHead = $gitHead
			IsDirty = $false
			Fingerprint = "clean:$gitHead"
		}
	}

	$diffText = (@(& git -c core.safecrlf=false -c core.autocrlf=false -C $resolvedRepoPath diff --no-ext-diff --binary HEAD -- . 2>$null) -join "`n")
	if ($LASTEXITCODE -ne 0) {
		throw "Could not determine git diff for local dependency repo '$resolvedRepoPath'."
	}

	$untrackedFileDescriptors = foreach ($statusLine in $statusLines | Where-Object { $_.StartsWith('?? ') }) {
		if ($statusLine.Length -lt 4) {
			continue
		}

		$relativePath = $statusLine.Substring(3)
		$fullPath = Join-Path $resolvedRepoPath $relativePath
		if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
			$fileInfo = Get-Item -LiteralPath $fullPath
			$fileHash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
			"$relativePath|$($fileInfo.Length)|$($fileInfo.LastWriteTimeUtc.Ticks)|$fileHash"
		}
		else {
			"$relativePath|missing"
		}
	}

	$fingerprintSource = @(
		$gitHead,
		($statusLines -join "`n"),
		$diffText,
		($untrackedFileDescriptors -join "`n")
	) -join "`n---`n"

	return [pscustomobject]@{
		RepoPath = $resolvedRepoPath
		GitHead = $gitHead
		IsDirty = $true
		Fingerprint = "dirty:$(Get-StringHash -Value $fingerprintSource)"
	}
}

function Test-LocalDependencyStateMatches {
	param(
		[Parameter(Mandatory = $true)]
		[psobject]$Stamp,
		[Parameter(Mandatory = $true)]
		[string[]]$Dependencies
	)

	if (-not ($Stamp.PSObject.Properties.Name -contains 'LocalDependencyStates')) {
		Write-Host "Build stamp is missing local dependency fingerprint metadata. Rebuilding before launch..." -ForegroundColor Yellow
		return $false
	}

	$stampStates = @($Stamp.LocalDependencyStates)
	if ($stampStates.Count -ne $Dependencies.Count) {
		Write-Host "Build stamp local dependency state count does not match the requested debug mode. Rebuilding..." -ForegroundColor Yellow
		return $false
	}

	$statesByDependency = @{}
	foreach ($state in $stampStates) {
		$statesByDependency[[string]$state.DependencyName] = $state
	}

	foreach ($dependency in $Dependencies) {
		if (-not $statesByDependency.ContainsKey($dependency)) {
			Write-Host "Build stamp is missing local dependency state for $dependency. Rebuilding..." -ForegroundColor Yellow
			return $false
		}

		$envVarName = Get-RepoEnvironmentVariableName -DependencyName $dependency
		$repoPath = [Environment]::GetEnvironmentVariable($envVarName)
		if ([string]::IsNullOrWhiteSpace($repoPath) -or -not (Test-Path -LiteralPath $repoPath -PathType Container)) {
			Write-Host "$envVarName is not set to a valid local repo path. Rebuilding before launch..." -ForegroundColor Yellow
			return $false
		}

		$currentFingerprint = Get-DependencyRepoFingerprint -RepoPath $repoPath
		$expectedState = $statesByDependency[$dependency]
		if ([System.IO.Path]::GetFullPath($repoPath) -ne [string]$expectedState.RepoPath -or $currentFingerprint.Fingerprint -ne [string]$expectedState.Fingerprint) {
			Write-Host "Detected local dependency repo changes for $dependency since the last successful $Configuration debug build. Rebuilding..." -ForegroundColor Yellow
			return $false
		}
	}

	return $true
}

function Test-BuildOutputsMatchStamp {
	param(
		[Parameter(Mandatory = $true)]
		[psobject]$Stamp,
		[Parameter(Mandatory = $true)]
		[string]$OutputDirectory,
		[Parameter(Mandatory = $true)]
		[string]$RuntimeExe,
		[Parameter(Mandatory = $true)]
		[string]$ManagedDebugTypeValue
	)

	if (-not ($Stamp.PSObject.Properties.Name -contains 'TimestampUtc') -or [string]::IsNullOrWhiteSpace($Stamp.TimestampUtc)) {
		Write-Host "Build stamp is missing its completion timestamp. Rebuilding before launch..." -ForegroundColor Yellow
		return $false
	}

	if (-not (Test-Path -LiteralPath $RuntimeExe -PathType Leaf)) {
		Write-Host "FieldWorks.exe is missing from the debug output. Rebuilding before launch..." -ForegroundColor Yellow
		return $false
	}

	if ($Stamp.TimestampUtc -is [DateTime]) {
		$stampTimestamp = ([DateTime]$Stamp.TimestampUtc).ToUniversalTime()
	}
	else {
		$stampTimestampOffset = [DateTimeOffset]::MinValue
		if (-not [DateTimeOffset]::TryParse([string]$Stamp.TimestampUtc, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind, [ref]$stampTimestampOffset)) {
			Write-Host "Build stamp timestamp could not be parsed. Rebuilding before launch..." -ForegroundColor Yellow
			return $false
		}
		$stampTimestamp = $stampTimestampOffset.UtcDateTime
	}

	$expectedPdbPath = [System.IO.Path]::ChangeExtension($RuntimeExe, '.pdb')
	if ($ManagedDebugTypeValue -eq 'portable' -and -not (Test-Path -LiteralPath $expectedPdbPath -PathType Leaf)) {
		Write-Host "Portable debug launch expects $(Split-Path $expectedPdbPath -Leaf) next to FieldWorks.exe. Rebuilding before launch..." -ForegroundColor Yellow
		return $false
	}

	$trackedOutputPaths = @($RuntimeExe)
	if (Test-Path -LiteralPath $expectedPdbPath -PathType Leaf) {
		$trackedOutputPaths += $expectedPdbPath
	}

	foreach ($trackedOutputPath in $trackedOutputPaths) {
		$fileInfo = Get-Item -LiteralPath $trackedOutputPath
		if ($fileInfo.LastWriteTimeUtc -gt $stampTimestamp) {
			Write-Host "Detected newer launch outputs than the last stamped VS Code debug build. Rebuilding before launch..." -ForegroundColor Yellow
			return $false
		}
	}

	return $true
}

function Invoke-Git {
	param(
		[Parameter(Mandatory = $true)][string[]]$Arguments
	)

	$output = & git @Arguments
	if ($LASTEXITCODE -ne 0) {
		throw "Git command failed: git $($Arguments -join ' ')"
	}

	return @($output | ForEach-Object { $_.TrimEnd() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-GitStatusForDebugRebuildCheck {
	param(
		[Parameter(Mandatory = $true)][string[]]$Pathspecs
	)

	return Invoke-Git -Arguments (@('status', '--porcelain=v1', '--untracked-files=all', '--') + $Pathspecs)
}

function Test-GitStateRequiresDebugRebuild {
	param(
		[Parameter(Mandatory = $true)][psobject]$Stamp,
		[Parameter(Mandatory = $true)][string[]]$Pathspecs
	)

	# We only skip the prelaunch build when we can prove the last successful debug build
	# still matches the source inputs for this debug session. The proof has three parts:
	# 1. the stamp recorded the same debug mode inputs we are asking for now,
	# 2. no relevant commits have landed since the stamped HEAD, and
	# 3. the current relevant worktree status still matches the stamped worktree status.
	# If any of those checks fail, we rebuild before launching so debugging does not start
	# against stale binaries or symbols.

	if (-not ($Stamp.PSObject.Properties.Name -contains 'GitHead') -or [string]::IsNullOrWhiteSpace($Stamp.GitHead)) {
		Write-Host "Build stamp is missing Git head metadata. Rebuilding before launch..." -ForegroundColor Yellow
		return $true
	}

	if (-not ($Stamp.PSObject.Properties.Name -contains 'RelevantDebugPathspecs') -or -not ($Stamp.PSObject.Properties.Name -contains 'RelevantDebugStatus')) {
		Write-Host "Build stamp is missing Git-based debug metadata. Rebuilding before launch..." -ForegroundColor Yellow
		return $true
	}

	$stampPathspecs = @($Stamp.RelevantDebugPathspecs)
	if (($stampPathspecs.Count -ne $Pathspecs.Count) -or (@($stampPathspecs) -join "`n") -ne ($Pathspecs -join "`n")) {
		Write-Host "Build stamp inputs do not match the requested VS Code debug mode. Rebuilding..." -ForegroundColor Yellow
		return $true
	}

	$currentHeadOutput = @(Invoke-Git -Arguments @('rev-parse', 'HEAD'))
	$currentHead = [string]($currentHeadOutput -join '')
	if ($currentHead -ne $Stamp.GitHead) {
		$committedChanges = Invoke-Git -Arguments (@('diff', '--name-only', "$($Stamp.GitHead)..$currentHead", '--') + $Pathspecs)
		if ($committedChanges.Count -gt 0) {
			Write-Host "Detected committed changes since the last successful $Configuration debug build. Rebuilding before launch..." -ForegroundColor Yellow
			return $true
		}
	}

	$currentStatus = Get-GitStatusForDebugRebuildCheck -Pathspecs $Pathspecs
	$stampStatus = @($Stamp.RelevantDebugStatus)
	if (($stampStatus.Count -ne $currentStatus.Count) -or (($stampStatus -join "`n") -ne ($currentStatus -join "`n"))) {
		Write-Host "Detected working tree changes since the last successful $Configuration debug build. Rebuilding before launch..." -ForegroundColor Yellow
		return $true
	}

	return $false
}

function Invoke-DebugBuild {
	$buildArgs = @(
		'-NoProfile',
		'-ExecutionPolicy',
		'Bypass',
		'-File',
		(Join-Path $repoRoot 'build.ps1'),
		'-Configuration',
		$Configuration,
		'-ManagedDebugType',
		$ManagedDebugType
	)

	if ($LocalPalaso) {
		$buildArgs += '-LocalPalaso'
	}
	if ($LocalLcm) {
		$buildArgs += '-LocalLcm'
	}
	if ($LocalChorus) {
		$buildArgs += '-LocalChorus'
	}
	if ($LocalPalaso -or $LocalLcm -or $LocalChorus) {
		$buildArgs += @('-LocalPackageVersion', $LocalPackageVersion)
	}

	& powershell.exe @buildArgs
	if ($LASTEXITCODE -ne 0) {
		exit $LASTEXITCODE
	}
}

$selectedLocalDependencies = Get-SelectedLocalDependencies

if (-not (Test-Path $stampPath) -or -not (Test-Path $runtimeExePath)) {
	Write-Host "No successful $Configuration debug build stamp found. Building before launch..." -ForegroundColor Yellow
	Invoke-DebugBuild
	exit 0
}

$stamp = Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json
$localDependencyMatches = ($stamp.PSObject.Properties.Name -contains 'LocalDependencies') -and ((@($stamp.LocalDependencies) -join "`n") -eq ($selectedLocalDependencies -join "`n"))
$localPackageVersionMatches = ($stamp.PSObject.Properties.Name -contains 'LocalPackageVersion') -and ([string]$stamp.LocalPackageVersion -eq $(if ($selectedLocalDependencies.Count -gt 0) { $LocalPackageVersion } else { '' }))
$debugTypeMatches = ($stamp.PSObject.Properties.Name -contains 'ManagedDebugType') -and ($stamp.ManagedDebugType -eq $ManagedDebugType)

if (-not $localDependencyMatches -or -not $localPackageVersionMatches -or -not $debugTypeMatches) {
	Write-Host "Build stamp mode does not match requested VS Code debug mode. Rebuilding..." -ForegroundColor Yellow
	Invoke-DebugBuild
	exit 0
}

if (-not (Test-BuildOutputsMatchStamp -Stamp $stamp -OutputDirectory $outputDir -RuntimeExe $runtimeExePath -ManagedDebugTypeValue $ManagedDebugType)) {
	Invoke-DebugBuild
	exit 0
}

if ($selectedLocalDependencies.Count -gt 0 -and -not (Test-LocalDependencyStateMatches -Stamp $stamp -Dependencies $selectedLocalDependencies)) {
	Invoke-DebugBuild
	exit 0
}

$pathspecsToCheck = Get-DebugRebuildCheckPathspecs
if (Test-GitStateRequiresDebugRebuild -Stamp $stamp -Pathspecs $pathspecsToCheck) {
	Invoke-DebugBuild
	exit 0
}

Write-Host "[OK] No relevant changes since the last successful $Configuration debug build. Skipping prelaunch build." -ForegroundColor Green
exit 0