[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$DiffReportPath,

	[Parameter(Mandatory = $false)]
	[string]$OutputPath
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

function Get-SectionLines {
	param(
		[Parameter(Mandatory = $true)][string[]]$Lines,
		[Parameter(Mandatory = $true)][string]$Header
	)

	$start = [Array]::IndexOf($Lines, $Header)
	if ($start -lt 0) { return @() }

	$result = New-Object System.Collections.Generic.List[string]
	for ($i = $start + 1; $i -lt $Lines.Length; $i++) {
		$line = $Lines[$i]
		if ([string]::IsNullOrWhiteSpace($line)) {
			break
		}
		$result.Add($line)
	}

	return $result.ToArray()
}

$repoRoot = Resolve-RepoRoot

if (!(Test-Path -LiteralPath $DiffReportPath)) {
	throw "Diff report not found: $DiffReportPath"
}

$DiffReportPath = (Resolve-Path -LiteralPath $DiffReportPath).Path
$lines = Get-Content -LiteralPath $DiffReportPath -Encoding UTF8

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
	$featureDir = Join-Path $repoRoot 'specs\001-wix-v6-migration'
	$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
	$OutputPath = Join-Path $featureDir ("wix6-parity-fix-plan-${stamp}.md")
}

$parent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($parent)) {
	Ensure-Directory -Path $parent
}

$uninstall = Get-SectionLines -Lines $lines -Header 'Uninstall entries'
$registry = Get-SectionLines -Lines $lines -Header 'Registry'
$files = Get-SectionLines -Lines $lines -Header 'Files'

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# WiX6 Parity Fix Plan (generated)")
$md.Add("")
$md.Add("Source diff report:")
$md.Add("- $DiffReportPath")
$md.Add("")
$md.Add("## Triage rules")
$md.Add("- Treat `+` as present only in WiX6 (candidate).")
$md.Add("- Treat `-` as missing in WiX6 (present in WiX3 baseline).")
$md.Add("- Treat `~` as changed between baseline and candidate.")
$md.Add("")

$md.Add("## Uninstall entries")
if ($uninstall.Count -eq 0) {
	$md.Add("- No data found in report section.")
} else {
	foreach ($l in $uninstall) {
		if ($l -match '^\s*[\+\-~]') {
			$md.Add("- $l")
		}
	}
}
$md.Add("")

$md.Add("## Registry")
if ($registry.Count -eq 0) {
	$md.Add("- No data found in report section.")
} else {
	foreach ($l in $registry) {
		if ($l -match '^\s*[\+\-~]') {
			$md.Add("- $l")
		}
	}
}
$md.Add("")

$md.Add("## Files")
if ($files.Count -eq 0) {
	$md.Add("- No data found in report section.")
} else {
	foreach ($l in $files) {
		if ($l -match '^\s*[\+\-~]') {
			$md.Add("- $l")
		}
	}
}
$md.Add("")

$md.Add("## Next steps")
$md.Add("- For each `-` item, identify the WiX6 authoring location that should create it (Component/ComponentGroup/Feature/CA).")
$md.Add("- For each unexpected `+` item, identify what is over-installing and remove/condition it.")
$md.Add("- Re-run the parity driver after each fix cluster to keep diffs small.")

Set-Content -LiteralPath $OutputPath -Value ($md -join [Environment]::NewLine) -Encoding UTF8
Write-Output "Wrote fix plan: $OutputPath"
return $OutputPath
