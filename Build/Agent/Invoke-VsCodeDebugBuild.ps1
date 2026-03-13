[CmdletBinding()]
param(
	[ValidateSet('Package', 'Local')]
	[string]$LcmMode,
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

function Test-RelevantPathChanged {
	param(
		[Parameter(Mandatory = $true)][string]$Path,
		[Parameter(Mandatory = $true)][datetime]$SinceUtc
	)

	if (-not (Test-Path $Path)) {
		return $false
	}

	if ((Get-Item $Path).PSIsContainer) {
		$excludedSegments = @('\.git\', '\.vs\', '\Output\', '\Obj\', '\obj\', '\bin\', '\packages\', '\artifacts\')
		foreach ($item in Get-ChildItem -Path $Path -Recurse -Force -ErrorAction SilentlyContinue) {
			$fullPath = $item.FullName
			$skip = $false
			foreach ($segment in $excludedSegments) {
				if ($fullPath.Contains($segment)) {
					$skip = $true
					break
				}
			}

			if ($skip) {
				continue
			}

			if ($item.LastWriteTimeUtc -gt $SinceUtc) {
				return $true
			}
		}

		return $false
	}

	return (Get-Item $Path).LastWriteTimeUtc -gt $SinceUtc
}

function Invoke-DebugBuild {
	$buildArgs = @(
		'-NoProfile',
		'-ExecutionPolicy',
		'Bypass',
		'-File',
		(Join-Path $repoRoot 'build.ps1'),
		'-LcmMode',
		$LcmMode,
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
$stampTimeUtc = [DateTime]::Parse($stamp.TimestampUtc).ToUniversalTime()
$resolvedLcmMode = if ($LcmMode -eq 'Local') { 'Local' } else { 'Package' }

$modeMatches = ($stamp.PSObject.Properties.Name -contains 'ResolvedLcmMode') -and ($stamp.ResolvedLcmMode -eq $resolvedLcmMode)
$debugTypeMatches = ($stamp.PSObject.Properties.Name -contains 'ManagedDebugType') -and ($stamp.ManagedDebugType -eq $ManagedDebugType)

if (-not $modeMatches -or -not $debugTypeMatches) {
	Write-Host "Build stamp mode does not match requested VS Code debug mode. Rebuilding..." -ForegroundColor Yellow
	Invoke-DebugBuild
	exit 0
}

$pathsToCheck = @(
	(Join-Path $repoRoot 'build.ps1'),
	(Join-Path $repoRoot 'Directory.Build.props'),
	(Join-Path $repoRoot 'Directory.Build.targets'),
	(Join-Path $repoRoot 'Directory.Packages.props'),
	(Join-Path $repoRoot 'FieldWorks.proj'),
	(Join-Path $repoRoot 'Build'),
	(Join-Path $repoRoot 'Src'),
	(Join-Path $repoRoot 'Lib')
)

if ($resolvedLcmMode -eq 'Local') {
	$pathsToCheck += @(
		(Join-Path $repoRoot 'FieldWorks.LocalLcm.sln'),
		(Join-Path $repoRoot 'Localizations\LCM')
	)
}
else {
	$pathsToCheck += (Join-Path $repoRoot 'FieldWorks.sln')
}

foreach ($pathToCheck in $pathsToCheck) {
	if (Test-RelevantPathChanged -Path $pathToCheck -SinceUtc $stampTimeUtc) {
		Write-Host "Detected changes since the last successful $Configuration debug build. Rebuilding before launch..." -ForegroundColor Yellow
		Invoke-DebugBuild
		exit 0
	}
}

Write-Host "[OK] No relevant changes since the last successful $Configuration debug build. Skipping prelaunch build." -ForegroundColor Green
exit 0