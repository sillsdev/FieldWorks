[CmdletBinding()]
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',
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

	$currentHead = (Invoke-Git -Arguments @('rev-parse', 'HEAD'))[0]
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

	& powershell.exe @buildArgs
	if ($LASTEXITCODE -ne 0) {
		exit $LASTEXITCODE
	}
}

if (-not (Test-Path $stampPath) -or -not (Test-Path $runtimeExePath)) {
	Write-Host "No successful $Configuration debug build stamp found. Building before launch..." -ForegroundColor Yellow
	Invoke-DebugBuild
	exit 0
}

$stamp = Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json
$localDependencyMatches = ($stamp.PSObject.Properties.Name -contains 'LocalDependencies') -and (@($stamp.LocalDependencies).Count -eq 0)
$debugTypeMatches = ($stamp.PSObject.Properties.Name -contains 'ManagedDebugType') -and ($stamp.ManagedDebugType -eq $ManagedDebugType)

if (-not $localDependencyMatches -or -not $debugTypeMatches) {
	Write-Host "Build stamp mode does not match requested VS Code debug mode. Rebuilding..." -ForegroundColor Yellow
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