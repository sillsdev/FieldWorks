[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$Wix3RunId,

	[Parameter(Mandatory = $true)]
	[string]$Wix6RunId,

	[Parameter(Mandatory = $false)]
	[string]$SpecEvidenceRoot,

	[Parameter(Mandatory = $false)]
	[switch]$FailOnDifferences
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
	$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
	return $repoRoot.Path
}

function New-DirectoryIfMissing {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

$repoRoot = Resolve-RepoRoot

if ([string]::IsNullOrWhiteSpace($SpecEvidenceRoot)) {
	$SpecEvidenceRoot = Join-Path $repoRoot 'specs\001-wix-v6-migration\evidence'
}

$wix3Dir = Join-Path (Join-Path $SpecEvidenceRoot 'Wix3') $Wix3RunId
$wix6Dir = Join-Path (Join-Path $SpecEvidenceRoot 'Wix6') $Wix6RunId

if (!(Test-Path -LiteralPath $wix3Dir)) { throw "WiX3 evidence not found: $wix3Dir" }
if (!(Test-Path -LiteralPath $wix6Dir)) { throw "WiX6 evidence not found: $wix6Dir" }

$wix3After = Join-Path $wix3Dir 'snapshot-after-install.json'
$wix6After = Join-Path $wix6Dir 'snapshot-after-install.json'

if (!(Test-Path -LiteralPath $wix3After)) { throw "Missing: $wix3After" }
if (!(Test-Path -LiteralPath $wix6After)) { throw "Missing: $wix6After" }

$compareDir = Join-Path $SpecEvidenceRoot 'compare'
New-DirectoryIfMissing -Path $compareDir

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$reportPath = Join-Path $compareDir ("wix3_${Wix3RunId}__vs__wix6_${Wix6RunId}__${stamp}.txt")

$compareArgs = @{
	BeforeSnapshotPath = $wix3After
	AfterSnapshotPath = $wix6After
	ReportPath = $reportPath
}
if ($FailOnDifferences) { $compareArgs.FailOnDifferences = $true }

& (Join-Path $repoRoot 'scripts\Agent\Compare-InstallerSnapshots.ps1') @compareArgs

Write-Output "Wrote comparison report: $reportPath"
