<#
.SYNOPSIS
	Verifies developer registry repair and launch preflight with isolated registry operations.
#>
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('FwRegistry_' + [guid]::NewGuid())
$repairScript = Join-Path $PSScriptRoot 'Repair-DeveloperRegistry.ps1'
$preflightScript = Join-Path $PSScriptRoot `
	'../.claude/skills/fieldworks-winapp/scripts/Resolve-FieldWorksDevRegistry.ps1'
$registryState = @{ Values = @{}; Removed = @(); Checks = 0 }

function Assert-True([bool]$Condition, [string]$Message) {
	$registryState.Checks++
	if (-not $Condition) { throw $Message }
}

function Test-Path {
	param([string]$LiteralPath, [string]$PathType)
	if ($LiteralPath.StartsWith('HKCU:')) {
		return $registryState.Values.ContainsKey($LiteralPath)
	}
	Microsoft.PowerShell.Management\Test-Path -LiteralPath $LiteralPath -PathType $PathType
}

function Get-ItemProperty {
	param([string]$LiteralPath)
	return [pscustomobject]$registryState.Values[$LiteralPath]
}

function Remove-ItemProperty {
	param([string]$LiteralPath, [string]$Name)
	$registryState.Removed += "$LiteralPath\$Name"
	$registryState.Values[$LiteralPath].Remove($Name)
}

function Remove-Item {
	param([string]$LiteralPath, [switch]$Recurse, [switch]$Force)
	if ($LiteralPath.StartsWith('HKCU:')) {
		$registryState.Removed += $LiteralPath
		foreach ($key in @($registryState.Values.Keys)) {
			if ($key -eq $LiteralPath -or $key.StartsWith($LiteralPath + '\')) { $registryState.Values.Remove($key) }
		}
		return
	}
	Microsoft.PowerShell.Management\Remove-Item -LiteralPath $LiteralPath -Recurse:$Recurse -Force:$Force
}

function Set-ItemProperty { throw 'Launch preflight must not write registry values' }
function Set-DevDirs { throw 'Launch preflight must not realign registry values' }

try {
	$sourceRoot = Join-Path $testRoot 'source tree'
	$distFiles = Join-Path $sourceRoot 'DistFiles'
	$icuPath = Join-Path $distFiles 'Icu70/icudt70l'
	$installPath = Join-Path $testRoot 'installed/DistFiles'
	New-Item -ItemType Directory -Path $icuPath, $installPath -Force | Out-Null
	Set-Content -LiteralPath (Join-Path $sourceRoot 'FieldWorks.sln') -Value ''
	$companyPath = 'HKCU:\SOFTWARE\SIL'
	$fieldWorksPath = Join-Path $companyPath 'FieldWorks\9'
	$encPath = Join-Path $companyPath 'SilEncConverters40'
	$repoPath = Join-Path $companyPath 'EncodingConverterRepository'
	$silfwPath = 'HKCU:\SOFTWARE\Classes\silfw'
	$silfwCommandPath = Join-Path $silfwPath 'shell\open\command'
	$registryState.Values = @{
		$encPath = @{
			RootDir = Join-Path $sourceRoot 'Output\Release'
			DeveloperPluginDir = Join-Path $sourceRoot 'Output\Release\EC\Plugins'
		}
		$repoPath = @{ Registry = Join-Path $distFiles 'SIL\Repository\mappingRegistry.xml' }
		$silfwCommandPath = @{ '(default)' = '"' + (Join-Path $sourceRoot 'Output\Release\FieldWorks.exe') + '" %1' }
		$fieldWorksPath = @{
			RootCodeDir = $distFiles + '\'
			RootDataDir = $distFiles
			ProjectsDir = Join-Path $distFiles 'Projects'
		}
		$companyPath = @{
			Icu70DataDir = $icuPath
			Icu99DataDir = $installPath
			OtherDataDir = $distFiles
		}
	}

	& $repairScript -WhatIf
	Assert-True ($registryState.Removed.Count -eq 0) 'Preview must not remove any values'
	Assert-True ($registryState.Values[$fieldWorksPath].Count -eq 3) 'Preview must retain code/data'
	& $preflightScript
	& $preflightScript -Force
	Assert-True ($registryState.Removed.Count -eq 0) 'Preflight must be read-only, including -Force'
	& $repairScript -Confirm:$false
	Assert-True ($registryState.Removed.Count -eq 7) 'Only source code/data/ICU/converter/silfw overrides are removed'
	Assert-True (-not $registryState.Values[$fieldWorksPath].ContainsKey('RootCodeDir')) `
		'Source code override must be removed'
	Assert-True (-not $registryState.Values[$fieldWorksPath].ContainsKey('RootDataDir')) `
		'Source data override must be removed'
	Assert-True (-not $registryState.Values[$companyPath].ContainsKey('Icu70DataDir')) `
		'Source ICU override must be removed'
	Assert-True ($registryState.Values[$fieldWorksPath].ContainsKey('ProjectsDir')) `
		'Shared projects override must be kept, even inside a source tree'
	Assert-True ($registryState.Values[$companyPath].Icu99DataDir -eq $installPath) `
		'Installed ICU override must be preserved'
	Assert-True ($registryState.Values[$companyPath].ContainsKey('OtherDataDir')) `
		'Unrelated values must be preserved'

	Assert-True (-not $registryState.Values.ContainsKey($silfwCommandPath)) `
		'Source silfw handler must be removed'
	Assert-True (-not $registryState.Values[$encPath].ContainsKey('RootDir')) `
		'Source encoding converter root must be removed'
	Assert-True (-not $registryState.Values[$repoPath].ContainsKey('Registry')) `
		'Source encoding repository must be removed'

	# Installed (non-source) values are kept.
	$registryState.Values[$encPath] = @{ RootDir = $installPath }
	$registryState.Values[$repoPath] = @{ Registry = Join-Path $installPath 'mappingRegistry.xml' }
	$registryState.Values[$silfwCommandPath] = @{ '(default)' = '"' + (Join-Path $installPath 'FieldWorks.exe') + '" %1' }
	& $repairScript -Confirm:$false
	Assert-True ($registryState.Removed.Count -eq 7) 'Installed silfw/encoding values must be kept'
	Assert-True ($registryState.Values.ContainsKey($silfwCommandPath)) 'Installed silfw handler kept'
	& $repairScript -WhatIf | Out-Null
	Assert-True ($registryState.Removed.Count -eq 7) 'Preview must not remove silfw'
	$registryState.Values[$silfwCommandPath] = @{ '(default)' = '"' + (Join-Path $sourceRoot 'Output\Release\FieldWorks.exe') + '" %1' }
	& $repairScript -WhatIf | Out-Null
	Assert-True ($registryState.Values.ContainsKey($silfwCommandPath)) 'Preview must keep a source silfw handler'
	$registryState.Values.Remove($silfwCommandPath)

	$registryState.Values[$fieldWorksPath].RootCodeDir = $installPath
	$registryState.Values[$fieldWorksPath].RootDataDir = 'relative/path'
	& $repairScript -Confirm:$false
	Assert-True ($registryState.Removed.Count -eq 7) 'Installed and relative overrides must be preserved'
	$registryState.Values[$fieldWorksPath].RootDataDir = 42
	& $repairScript -Confirm:$false
	Assert-True ($registryState.Removed.Count -eq 7) 'Non-string overrides must be preserved'
	$registryState.Values[$fieldWorksPath].RootDataDir = 'C:relative'
	& $repairScript -Confirm:$false
	Assert-True ($registryState.Removed.Count -eq 7) 'Drive-relative overrides must be preserved'
	Remove-Item -LiteralPath (Join-Path $sourceRoot 'FieldWorks.sln')
	$registryState.Values[$fieldWorksPath].RootCodeDir = $distFiles
	& $repairScript -Confirm:$false
	Assert-True ($registryState.Removed.Count -eq 7) 'A path without the source marker must be preserved'
	$registryState.Values = @{}
	& $repairScript -Confirm:$false
	& $preflightScript
	Assert-True ($registryState.Removed.Count -eq 7) 'Missing registry keys must be harmless'
	Write-Host "Developer registry tests: $($registryState.Checks) checks passed."
}
finally {
	if ([System.IO.Path]::GetFullPath($testRoot).StartsWith(
		[System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()),
		[System.StringComparison]::OrdinalIgnoreCase)) {
		Remove-Item -LiteralPath $testRoot -Recurse -Force
	}
}
