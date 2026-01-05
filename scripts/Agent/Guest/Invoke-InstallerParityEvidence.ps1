param(
	[Parameter(Mandatory)]
	[ValidateSet('Wix3','Wix6')]
	[string]$Mode,

	[Parameter(Mandatory)]
	[ValidateNotNullOrEmpty()]
	[string]$InstallerPath,

	[Parameter()]
	[string]$WorkRoot = 'C:\FWInstallerTest',

	[Parameter()]
	[string[]]$InstallArguments = @('/quiet','/norestart'),

	[Parameter()]
	[int]$SleepSecondsAfterInstall = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $PSCommandPath
. (Join-Path $scriptRoot 'InstallerParityEvidence.Common.ps1')

$invokeArgs = @{
	Mode = $Mode
	InstallerPath = $InstallerPath
	WorkRoot = $WorkRoot
	InstallArguments = $InstallArguments
	SleepSecondsAfterInstall = $SleepSecondsAfterInstall
}

Invoke-InstallerParityEvidenceRun @invokeArgs
