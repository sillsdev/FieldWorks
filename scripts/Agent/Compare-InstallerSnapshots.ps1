[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$BeforeSnapshotPath,

	[Parameter(Mandatory = $true)]
	[string]$AfterSnapshotPath,

	[Parameter(Mandatory = $false)]
	[string]$ReportPath,

	[Parameter(Mandatory = $false)]
	[int]$MaxListItems = 200,

	[Parameter(Mandatory = $false)]
	[switch]$FailOnDifferences
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-Snapshot {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		throw "Snapshot not found: $Path"
	}
	$content = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
	return $content | ConvertFrom-Json -ErrorAction Stop
}

function To-StringSet {
	param([Parameter(Mandatory = $true)][object[]]$Items)
	$set = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
	foreach ($item in $Items) { $null = $set.Add([string]$item) }
	return $set
}

function Diff-Sets {
	param(
		[Parameter(Mandatory = $true)]$Before,
		[Parameter(Mandatory = $true)]$After
	)

	$added = New-Object System.Collections.Generic.List[string]
	$removed = New-Object System.Collections.Generic.List[string]

	foreach ($v in $After) {
		if (-not $Before.Contains($v)) { $added.Add($v) }
	}
	foreach ($v in $Before) {
		if (-not $After.Contains($v)) { $removed.Add($v) }
	}

	return [pscustomobject]@{ Added = $added; Removed = $removed }
}

$before = Read-Snapshot -Path $BeforeSnapshotPath
$after = Read-Snapshot -Path $AfterSnapshotPath

$reportLines = New-Object System.Collections.Generic.List[string]
$reportLines.Add("Installer snapshot diff")
$reportLines.Add("Before: $BeforeSnapshotPath")
$reportLines.Add("After:  $AfterSnapshotPath")
$reportLines.Add("")

# Uninstall entries
$beforeProducts = @()
foreach ($p in ($before.UninstallEntries | ForEach-Object { $_ })) {
	$beforeProducts += ([string]$p.DisplayName)
}
$afterProducts = @()
foreach ($p in ($after.UninstallEntries | ForEach-Object { $_ })) {
	$afterProducts += ([string]$p.DisplayName)
}

$diffProducts = Diff-Sets -Before (To-StringSet -Items $beforeProducts) -After (To-StringSet -Items $afterProducts)
$reportLines.Add("Uninstall entries")
$reportLines.Add(("  Added: {0}, Removed: {1}" -f $diffProducts.Added.Count, $diffProducts.Removed.Count))

foreach ($name in ($diffProducts.Added | Select-Object -First $MaxListItems)) {
	$reportLines.Add("    + $name")
}
foreach ($name in ($diffProducts.Removed | Select-Object -First $MaxListItems)) {
	$reportLines.Add("    - $name")
}
$reportLines.Add("")

# Registry roots (value diffs)
$reportLines.Add("Registry")
$regDiffCount = 0

$beforeRegByPath = @{}
foreach ($r in ($before.Registry | ForEach-Object { $_ })) { $beforeRegByPath[[string]$r.Path] = $r }
$afterRegByPath = @{}
foreach ($r in ($after.Registry | ForEach-Object { $_ })) { $afterRegByPath[[string]$r.Path] = $r }

$allRegPaths = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($k in $beforeRegByPath.Keys) { $null = $allRegPaths.Add($k) }
foreach ($k in $afterRegByPath.Keys) { $null = $allRegPaths.Add($k) }

foreach ($path in ($allRegPaths | Sort-Object)) {
	$beforeValues = @{}
	if ($beforeRegByPath.ContainsKey($path)) { $beforeValues = $beforeRegByPath[$path].Values }
	$afterValues = @{}
	if ($afterRegByPath.ContainsKey($path)) { $afterValues = $afterRegByPath[$path].Values }

	$allNames = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)
	foreach ($n in $beforeValues.Keys) { $null = $allNames.Add([string]$n) }
	foreach ($n in $afterValues.Keys) { $null = $allNames.Add([string]$n) }

	foreach ($n in $allNames) {
		$bn = $beforeValues[$n]
		$an = $afterValues[$n]
		if ($null -eq $bn -and $null -ne $an) {
			$regDiffCount++
			if ($regDiffCount -le $MaxListItems) { $reportLines.Add("  + $path\\$n = $an") }
			continue
		}
		if ($null -ne $bn -and $null -eq $an) {
			$regDiffCount++
			if ($regDiffCount -le $MaxListItems) { $reportLines.Add("  - $path\\$n (was $bn)") }
			continue
		}
		if ([string]$bn -ne [string]$an) {
			$regDiffCount++
			if ($regDiffCount -le $MaxListItems) { $reportLines.Add("  ~ $path\\$n: '$bn' -> '$an'") }
		}
	}
}

$reportLines.Add(("  Total value diffs: {0}" -f $regDiffCount))
$reportLines.Add("")

# Files
$reportLines.Add("Files")
$beforeFileSet = New-Object 'System.Collections.Generic.Dictionary[string, string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($f in ($before.Files | ForEach-Object { $_ })) {
	$beforeFileSet[[string]$f.Path] = ('{0}:{1}' -f $f.Length, $f.LastWriteTimeUtc)
}
$afterFileSet = New-Object 'System.Collections.Generic.Dictionary[string, string]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($f in ($after.Files | ForEach-Object { $_ })) {
	$afterFileSet[[string]$f.Path] = ('{0}:{1}' -f $f.Length, $f.LastWriteTimeUtc)
}

$addedFiles = New-Object System.Collections.Generic.List[string]
$removedFiles = New-Object System.Collections.Generic.List[string]
$changedFiles = New-Object System.Collections.Generic.List[string]

foreach ($p in $afterFileSet.Keys) {
	if (-not $beforeFileSet.ContainsKey($p)) { $addedFiles.Add($p); continue }
	if ($beforeFileSet[$p] -ne $afterFileSet[$p]) { $changedFiles.Add($p) }
}
foreach ($p in $beforeFileSet.Keys) {
	if (-not $afterFileSet.ContainsKey($p)) { $removedFiles.Add($p) }
}

$reportLines.Add(("  Added: {0}, Removed: {1}, Changed: {2}" -f $addedFiles.Count, $removedFiles.Count, $changedFiles.Count))
foreach ($p in ($addedFiles | Sort-Object | Select-Object -First $MaxListItems)) { $reportLines.Add("    + $p") }
foreach ($p in ($removedFiles | Sort-Object | Select-Object -First $MaxListItems)) { $reportLines.Add("    - $p") }
foreach ($p in ($changedFiles | Sort-Object | Select-Object -First $MaxListItems)) { $reportLines.Add("    ~ $p") }

$report = ($reportLines -join [Environment]::NewLine)
Write-Output $report

if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
	$parent = Split-Path -Parent $ReportPath
	if (-not [string]::IsNullOrWhiteSpace($parent) -and !(Test-Path -LiteralPath $parent)) {
		$null = New-Item -ItemType Directory -Force -Path $parent
	}
	Set-Content -LiteralPath $ReportPath -Value $report -Encoding UTF8
	Write-Output "Wrote report: $ReportPath"
}

$hasDiff = ($diffProducts.Added.Count -gt 0) -or ($diffProducts.Removed.Count -gt 0) -or ($regDiffCount -gt 0) -or ($addedFiles.Count -gt 0) -or ($removedFiles.Count -gt 0) -or ($changedFiles.Count -gt 0)

if ($FailOnDifferences -and $hasDiff) {
	exit 1
}

exit 0
