<#
.SYNOPSIS
	Removes per-user code, data, and ICU overrides that point into a FieldWorks source
	tree.
.DESCRIPTION
	Use -WhatIf to preview. ProjectsDir, DeveloperPluginDir, and paths without an
	ancestor containing FieldWorks.sln are preserved. Close installed FieldWorks before applying a
	repair.
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
	$fieldWorksPath = @('RootCodeDir', 'RootDataDir')
	$companyPath = @()
	(Join-Path $companyPath 'SilEncConverters40') = @('RootDir')
	(Join-Path $companyPath 'EncodingConverterRepository') = @('Registry')
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

# The silfw:// handler a dev build registered launches that build's exe, so it goes as a whole
# key.
$silfwPath = 'HKCU:\SOFTWARE\Classes\silfw'
$silfwCommandPath = Join-Path $silfwPath 'shell\open\command'
if (Test-Path -LiteralPath $silfwCommandPath) {
	$command = (Get-ItemProperty -LiteralPath $silfwCommandPath).'(default)'
	if ($command -is [string] -and $command -match '^\s*"?([^"]+?\.exe)"?(\s|$)') {
		$exePath = $Matches[1]
		if (Test-FieldWorksSourcePath $exePath) {
			if ($PSCmdlet.ShouldProcess("$silfwPath (command = $command)", 'Remove developer silfw:// handler')) {
				Remove-Item -LiteralPath $silfwPath -Recurse -Force
			}
		}
	}
}
