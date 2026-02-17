[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[ValidateSet('Wix3', 'Wix6')]
	[string]$Mode,

	[Parameter(Mandatory = $true)]
	[string]$RunId,

	[Parameter(Mandatory = $false)]
	[string]$HyperVEvidenceRoot,

	[Parameter(Mandatory = $false)]
	[string]$SpecEvidenceRoot
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

$repoRoot = Resolve-RepoRoot

if ([string]::IsNullOrWhiteSpace($HyperVEvidenceRoot)) {
	$HyperVEvidenceRoot = Join-Path $repoRoot 'Output\InstallerEvidence\HyperV'
}

$sourceDir = Join-Path (Join-Path $HyperVEvidenceRoot $Mode) $RunId
if (!(Test-Path -LiteralPath $sourceDir)) {
	throw "Hyper-V evidence run folder not found: $sourceDir"
}

if ([string]::IsNullOrWhiteSpace($SpecEvidenceRoot)) {
	$SpecEvidenceRoot = Join-Path $repoRoot 'specs\001-wix-v6-migration\evidence'
}

$modeDir = Join-Path $SpecEvidenceRoot $Mode
$destDir = Join-Path $modeDir $RunId
Ensure-Directory -Path $destDir

Copy-Item -LiteralPath (Join-Path $sourceDir '*') -Destination $destDir -Recurse -Force

$meta = [pscustomobject]@{
	Mode = $Mode
	RunId = $RunId
	PublishedUtc = (Get-Date).ToUniversalTime().ToString('o')
	Source = $sourceDir
	Destination = $destDir
}
$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destDir 'published.json') -Encoding UTF8

Write-Output "Published Hyper-V evidence to: $destDir"
