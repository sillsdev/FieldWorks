param(
	[Parameter()]
	[string]$InstallerPath = '',

	[Parameter()]
	[string]$WorkRoot = 'C:\FWInstallerTest'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $PSCommandPath
. (Join-Path $scriptRoot 'InstallerParityEvidence.Common.ps1')

if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
	$InstallerPath = Join-Path $scriptRoot 'Wix6Candidate.exe'
}

try {
	Invoke-InstallerParityEvidenceRun -Mode Wix6 -InstallerPath $InstallerPath -WorkRoot $WorkRoot
} finally {
	Write-Output ''
	Read-Host 'Press Enter to close'
}
