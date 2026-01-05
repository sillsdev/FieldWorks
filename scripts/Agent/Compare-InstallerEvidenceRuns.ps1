[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$Wix3EvidenceDir,

	[Parameter(Mandatory = $true)]
	[string]$Wix6EvidenceDir,

	[Parameter(Mandatory = $false)]
	[string]$ReportPath,

	[Parameter(Mandatory = $false)]
	[switch]$FailOnDifferences
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
	$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
	return $repoRoot.Path
}

function Ensure-Directory {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Assert-DirectoryExists {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path -PathType Container)) {
		throw "Directory not found: $Path"
	}
}

$repoRoot = Resolve-RepoRoot

Assert-DirectoryExists -Path $Wix3EvidenceDir
Assert-DirectoryExists -Path $Wix6EvidenceDir

$wix3After = Join-Path $Wix3EvidenceDir 'snapshot-after-install.json'
$wix6After = Join-Path $Wix6EvidenceDir 'snapshot-after-install.json'

if (!(Test-Path -LiteralPath $wix3After)) { throw "Missing: $wix3After" }
if (!(Test-Path -LiteralPath $wix6After)) { throw "Missing: $wix6After" }

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
	$root = Split-Path -Parent $Wix6EvidenceDir
	$compareDir = Join-Path $root 'compare'
	Ensure-Directory -Path $compareDir

	$wix3RunId = Split-Path -Leaf $Wix3EvidenceDir
	$wix6RunId = Split-Path -Leaf $Wix6EvidenceDir
	$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
	$ReportPath = Join-Path $compareDir ("wix3_${wix3RunId}__vs__wix6_${wix6RunId}__${stamp}.txt")
}

$compareArgs = @{
	BeforeSnapshotPath = $wix3After
	AfterSnapshotPath = $wix6After
	ReportPath = $ReportPath
}
if ($FailOnDifferences) { $compareArgs.FailOnDifferences = $true }

& (Join-Path $repoRoot 'scripts\Agent\Compare-InstallerSnapshots.ps1') @compareArgs

Write-Output "Wrote comparison report: $ReportPath"
return $ReportPath
