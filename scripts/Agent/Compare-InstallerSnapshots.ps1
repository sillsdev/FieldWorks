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

function New-StringSet {
	param([Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items)
	$set = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
	foreach ($item in $Items) { $null = $set.Add([string]$item) }
	return ,$set
}

function Get-ArrayProperty {
	param(
		[Parameter(Mandatory = $true)]
		$Object,
		[Parameter(Mandatory = $true)]
		[string]$Name
	)

	$property = $Object.PSObject.Properties[$Name]
	if ($null -eq $property -or $null -eq $property.Value) { return @() }

	return @($property.Value | ForEach-Object { $_ })
}

function ConvertTo-StringDictionary {
	param([Parameter(Mandatory = $false)]$Value)

	$dictionary = New-Object 'System.Collections.Generic.Dictionary[string, string]' ([System.StringComparer]::OrdinalIgnoreCase)
	if ($null -eq $Value) { return ,$dictionary }

	if ($Value -is [System.Collections.IDictionary]) {
		foreach ($key in $Value.Keys) {
			$dictionary[[string]$key] = [string]$Value[$key]
		}

		return ,$dictionary
	}

	foreach ($property in $Value.PSObject.Properties) {
		$dictionary[[string]$property.Name] = [string]$property.Value
	}

	return ,$dictionary
}

function Compare-Sets {
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

function Get-DependencyKey {
	param([Parameter(Mandatory = $true)]$Dependency)

	$key = [string]$Dependency.ProviderKey
	if ([string]::IsNullOrWhiteSpace($key)) { $key = [string]$Dependency.KeyPath }

	return ('{0}|{1}' -f [string]$Dependency.Root, $key)
}

function Get-DependencyDescription {
	param([Parameter(Mandatory = $true)]$Dependency)

	$displayName = [string]$Dependency.DisplayName
	if ([string]::IsNullOrWhiteSpace($displayName)) { $displayName = [string]$Dependency.DefaultValue }
	if ([string]::IsNullOrWhiteSpace($displayName)) { $displayName = [string]$Dependency.ProviderKey }

	$version = [string]$Dependency.Version
	if ([string]::IsNullOrWhiteSpace($version)) { return $displayName }

	return ('{0} {1}' -f $displayName, $version)
}

function Get-DependencyComparableValue {
	param([Parameter(Mandatory = $true)]$Dependency)

	$dependents = New-Object System.Collections.Generic.List[string]
	foreach ($dependent in (Get-ArrayProperty -Object $Dependency -Name 'Dependents')) {
		$dependents.Add([string]$dependent.KeyName)
	}

	$dependentText = (($dependents | Sort-Object) -join ',')
	return ('DisplayName={0};Version={1};Default={2};Dependents={3}' -f [string]$Dependency.DisplayName, [string]$Dependency.Version, [string]$Dependency.DefaultValue, $dependentText)
}

$before = Read-Snapshot -Path $BeforeSnapshotPath
$after = Read-Snapshot -Path $AfterSnapshotPath

$reportLines = New-Object System.Collections.Generic.List[string]
$reportLines.Add("Installer snapshot diff")
$reportLines.Add(('Before: {0}' -f $BeforeSnapshotPath))
$reportLines.Add(('After:  {0}' -f $AfterSnapshotPath))
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

$diffProducts = Compare-Sets -Before (New-StringSet -Items $beforeProducts) -After (New-StringSet -Items $afterProducts)
$reportLines.Add("Uninstall entries")
$reportLines.Add(("  Added: {0}, Removed: {1}" -f $diffProducts.Added.Count, $diffProducts.Removed.Count))

foreach ($name in ($diffProducts.Added | Select-Object -First $MaxListItems)) {
	$reportLines.Add("    + $name")
}
foreach ($name in ($diffProducts.Removed | Select-Object -First $MaxListItems)) {
	$reportLines.Add("    - $name")
}
$reportLines.Add("")

# Burn dependency provider entries
$reportLines.Add("Burn dependency providers")
$beforeDependencies = Get-ArrayProperty -Object $before -Name 'BurnDependencyEntries'
$afterDependencies = Get-ArrayProperty -Object $after -Name 'BurnDependencyEntries'

$beforeDependenciesByKey = @{}
foreach ($dependency in $beforeDependencies) { $beforeDependenciesByKey[(Get-DependencyKey -Dependency $dependency)] = $dependency }
$afterDependenciesByKey = @{}
foreach ($dependency in $afterDependencies) { $afterDependenciesByKey[(Get-DependencyKey -Dependency $dependency)] = $dependency }

$addedDependencies = New-Object System.Collections.Generic.List[object]
$removedDependencies = New-Object System.Collections.Generic.List[object]
$changedDependencies = New-Object System.Collections.Generic.List[object]

foreach ($key in $afterDependenciesByKey.Keys) {
	if (-not $beforeDependenciesByKey.ContainsKey($key)) {
		$addedDependencies.Add($afterDependenciesByKey[$key])
		continue
	}

	$beforeComparable = Get-DependencyComparableValue -Dependency $beforeDependenciesByKey[$key]
	$afterComparable = Get-DependencyComparableValue -Dependency $afterDependenciesByKey[$key]
	if ($beforeComparable -ne $afterComparable) {
		$changedDependencies.Add($afterDependenciesByKey[$key])
	}
}

foreach ($key in $beforeDependenciesByKey.Keys) {
	if (-not $afterDependenciesByKey.ContainsKey($key)) {
		$removedDependencies.Add($beforeDependenciesByKey[$key])
	}
}

$reportLines.Add(("  Added: {0}, Removed: {1}, Changed: {2}" -f $addedDependencies.Count, $removedDependencies.Count, $changedDependencies.Count))
foreach ($dependency in ($addedDependencies | Sort-Object ProviderKey | Select-Object -First $MaxListItems)) {
	$reportLines.Add(("    + {0} [{1}]" -f (Get-DependencyDescription -Dependency $dependency), [string]$dependency.ProviderKey))
}
foreach ($dependency in ($removedDependencies | Sort-Object ProviderKey | Select-Object -First $MaxListItems)) {
	$reportLines.Add(("    - {0} [{1}]" -f (Get-DependencyDescription -Dependency $dependency), [string]$dependency.ProviderKey))
}
foreach ($dependency in ($changedDependencies | Sort-Object ProviderKey | Select-Object -First $MaxListItems)) {
	$reportLines.Add(("    ~ {0} [{1}]" -f (Get-DependencyDescription -Dependency $dependency), [string]$dependency.ProviderKey))
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
	$beforeValues = ConvertTo-StringDictionary
	if ($beforeRegByPath.ContainsKey($path)) { $beforeValues = ConvertTo-StringDictionary -Value $beforeRegByPath[$path].Values }
	$afterValues = ConvertTo-StringDictionary
	if ($afterRegByPath.ContainsKey($path)) { $afterValues = ConvertTo-StringDictionary -Value $afterRegByPath[$path].Values }

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
			if ($regDiffCount -le $MaxListItems) { $reportLines.Add(("  ~ {0}\{1}: '{2}' -> '{3}'" -f $path, $n, $bn, $an)) }
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

$hasDiff = ($diffProducts.Added.Count -gt 0) -or ($diffProducts.Removed.Count -gt 0) -or ($addedDependencies.Count -gt 0) -or ($removedDependencies.Count -gt 0) -or ($changedDependencies.Count -gt 0) -or ($regDiffCount -gt 0) -or ($addedFiles.Count -gt 0) -or ($removedFiles.Count -gt 0) -or ($changedFiles.Count -gt 0)

if ($FailOnDifferences -and $hasDiff) {
	exit 1
}

exit 0
