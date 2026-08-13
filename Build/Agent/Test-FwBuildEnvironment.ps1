[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$module = Import-Module (Join-Path $PSScriptRoot 'FwBuildEnvironment.psm1') `
	-Force -PassThru
$environmentLines = @(
	'PATH=C:\toolchain\bin;C:\Windows\System32',
	'VCToolsInstallDir=C:\toolchain\',
	'Path=C:\Windows\System32'
)

$variables = & $module {
	param($Lines)
	ConvertFrom-EnvironmentVariableLines -Lines $Lines
} $environmentLines

$expectedPath = 'C:\toolchain\bin;C:\Windows\System32'
if ($variables.Path -ne $expectedPath) {
	throw "Expected the first case-insensitive PATH value to win. Actual: $($variables.Path)"
}

Write-Output '[OK] FwBuildEnvironment smoke test passed.'
