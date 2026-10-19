<#
.SYNOPSIS
	Removes per-user code, data, projects, and ICU overrides that point into a FieldWorks source
	tree.
.DESCRIPTION
	Use -WhatIf to preview. Paths without an ancestor containing FieldWorks.sln
	are preserved. Close installed FieldWorks before applying a repair.
#>
[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'

function Test-FieldWorksSourcePath {
	param($Value)
	if ($Value -isnot [string] -or [string]::IsNullOrWhiteSpace($Value)) {
		return $false
	}
	try {
		if (-not [System.IO.Path]::IsPathRooted($Value) -or
			[System.IO.Path]::GetPathRoot($Value).Length -lt 3) {
			return $false
		}
		$directory = [System.IO.Path]::GetFullPath($Value)
		while ($directory) {
			if (Test-Path -LiteralPath (Join-Path $directory 'FieldWorks.sln') -PathType Leaf) {
				return $true
			}
			$directory = [System.IO.Path]::GetDirectoryName($directory.TrimEnd('\', '/'))
		}
	} catch {
		Write-Warning "Cannot identify source tree for '$Value': $_"
	}
	return $false
}

$companyPath = 'HKCU:\SOFTWARE\SIL'
$fieldWorksPath = Join-Path $companyPath 'FieldWorks\9'
$locations = @{
	$fieldWorksPath = @('RootCodeDir', 'RootDataDir', 'ProjectsDir')
	$companyPath = @()
}
foreach ($registryPath in $locations.Keys) {
	if (-not (Test-Path -LiteralPath $registryPath)) { continue }
	$properties = Get-ItemProperty -LiteralPath $registryPath
	$names = $locations[$registryPath]
	if ($registryPath -eq $companyPath) {
		$names = @($properties.PSObject.Properties.Name | Where-Object { $_ -match '^Icu\d+DataDir$' })
	}
	foreach ($name in $names) {
		$value = $properties.$name
		if (Test-FieldWorksSourcePath $value) {
			if ($PSCmdlet.ShouldProcess("$registryPath\$name = $value", 'Remove developer override')) {
				Remove-ItemProperty -LiteralPath $registryPath -Name $name
			}
		}
	}
}
