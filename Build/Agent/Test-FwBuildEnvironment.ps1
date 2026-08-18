[CmdletBinding()]
param(
	[switch]$ChildProcess
)

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

$namesToRemove = & $module {
	Get-EnvironmentVariableNamesToRemove `
		-ExistingNames @('PATH', 'Path', 'PATHEXT') -IncomingName 'PATH'
}
if ((@($namesToRemove) -join ',') -ne 'PATH,Path') {
	throw "Expected both PATH casings to be removed. Actual: $(@($namesToRemove) -join ',')"
}

if ($ChildProcess) {
	& $module {
		param($Variables)
		Set-ProcessEnvironmentVariables -Variables $Variables
	} $variables

	$pathLines = & C:\Windows\System32\cmd.exe /d /c 'set path'
	$pathVariableLines = @($pathLines | Where-Object { $_ -match '^PATH=' })
	if ($pathVariableLines.Count -ne 1) {
		throw "Expected one case-insensitive PATH entry. Actual count: $($pathVariableLines.Count)"
	}

	$null = ([System.Diagnostics.ProcessStartInfo]::new()).EnvironmentVariables
	Write-Output '[OK] FwBuildEnvironment child-process smoke test passed.'
	exit 0
}

$pwshPath = [System.Environment]::ProcessPath
& $pwshPath -NoProfile -File $PSCommandPath -ChildProcess
if ($LASTEXITCODE -ne 0) {
	throw "Expected isolated child-process smoke test to pass. Exit code: $LASTEXITCODE"
}

Write-Output '[OK] FwBuildEnvironment smoke test passed.'
