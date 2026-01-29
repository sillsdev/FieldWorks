param(
	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]$VMName = 'FwInstallerTest',

	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]$GuestWorkRoot = 'C:\\FWInstallerTest',

	[Parameter()]
	[string]$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss'),

	[Parameter()]
	[string]$Wix3BaselineInstallerPath = 'C:\\ProgramData\\FieldWorks\\HyperV\\Installers\\Wix3Baseline\\FieldWorks_9.2.11.1_Online_x64.exe',

	[Parameter()]
	[string]$Wix6CandidateInstallerPath = 'FLExInstaller\\bin\\x64\\Release\\FieldWorksBundle.exe',

	[Parameter()]
	[string]$GuestEvidenceScriptPath = '',

	[Parameter()]
	[string]$GuestEvidenceCommonScriptPath = '',

	[Parameter()]
	[string]$GuestEvidenceWix3ScriptPath = '',

	[Parameter()]
	[string]$GuestEvidenceWix6ScriptPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $PSCommandPath
if ([string]::IsNullOrWhiteSpace($GuestEvidenceScriptPath)) {
	$GuestEvidenceScriptPath = Join-Path $scriptRoot 'Guest\Invoke-InstallerParityEvidence.ps1'
}

if ([string]::IsNullOrWhiteSpace($GuestEvidenceCommonScriptPath)) {
	$GuestEvidenceCommonScriptPath = Join-Path $scriptRoot 'Guest\InstallerParityEvidence.Common.ps1'
}

if ([string]::IsNullOrWhiteSpace($GuestEvidenceWix3ScriptPath)) {
	$GuestEvidenceWix3ScriptPath = Join-Path $scriptRoot 'Guest\Invoke-InstallerParityEvidence-Wix3.ps1'
}

if ([string]::IsNullOrWhiteSpace($GuestEvidenceWix6ScriptPath)) {
	$GuestEvidenceWix6ScriptPath = Join-Path $scriptRoot 'Guest\Invoke-InstallerParityEvidence-Wix6.ps1'
}

function Resolve-File([string]$path, [string]$label) {
	if ([string]::IsNullOrWhiteSpace($path)) {
		throw "$label path is empty"
	}

	# Allow relative paths from repo root / current directory.
	$resolved = Resolve-Path -LiteralPath $path -ErrorAction SilentlyContinue
	if (-not $resolved) {
		$resolved = Resolve-Path -Path $path -ErrorAction SilentlyContinue
	}
	if (-not $resolved) {
		throw "$label not found: $path"
	}

	$resolved | Select-Object -First 1 -ExpandProperty Path
}

$baselineHostPath = Resolve-File -path $Wix3BaselineInstallerPath -label 'WiX3 baseline installer'
$candidateHostPath = Resolve-File -path $Wix6CandidateInstallerPath -label 'WiX6 candidate bundle'
$guestScriptHostPath = Resolve-File -path $GuestEvidenceScriptPath -label 'Guest evidence script'

$vm = Get-VM -Name $VMName -ErrorAction Stop
if ($vm.State -ne 'Running') {
	throw "VM '$VMName' must be Running to copy files via Guest Service Interface. Current state: $($vm.State). Start the VM and wait for Windows to reach the logon screen, then re-run."
}

$gsi = Get-VMIntegrationService -VMName $VMName -Name 'Guest Service Interface' -ErrorAction Stop
if (-not $gsi.Enabled) {
	throw "Guest Service Interface is not enabled for VM '$VMName'. Enable it in Hyper-V Manager (VM Settings > Integration Services) or run Enable-VMIntegrationService."
}

$guestRunRoot = Join-Path $GuestWorkRoot (Join-Path 'ParityPayload' $RunId)

$baselineGuestPath = Join-Path $guestRunRoot 'Wix3Baseline.exe'
$candidateGuestPath = Join-Path $guestRunRoot 'Wix6Candidate.exe'
$scriptGuestPath = Join-Path $guestRunRoot 'Invoke-InstallerParityEvidence.ps1'
$commonGuestPath = Join-Path $guestRunRoot 'InstallerParityEvidence.Common.ps1'
$wix3GuestPath = Join-Path $guestRunRoot 'Invoke-InstallerParityEvidence-Wix3.ps1'
$wix6GuestPath = Join-Path $guestRunRoot 'Invoke-InstallerParityEvidence-Wix6.ps1'

Copy-VMFile -VMName $VMName -SourcePath $baselineHostPath -DestinationPath $baselineGuestPath -FileSource Host -CreateFullPath -Force
Copy-VMFile -VMName $VMName -SourcePath $candidateHostPath -DestinationPath $candidateGuestPath -FileSource Host -CreateFullPath -Force
Copy-VMFile -VMName $VMName -SourcePath $guestScriptHostPath -DestinationPath $scriptGuestPath -FileSource Host -CreateFullPath -Force
Copy-VMFile -VMName $VMName -SourcePath (Resolve-File -path $GuestEvidenceCommonScriptPath -label 'Guest common evidence script') -DestinationPath $commonGuestPath -FileSource Host -CreateFullPath -Force
Copy-VMFile -VMName $VMName -SourcePath (Resolve-File -path $GuestEvidenceWix3ScriptPath -label 'Guest Wix3 evidence script') -DestinationPath $wix3GuestPath -FileSource Host -CreateFullPath -Force
Copy-VMFile -VMName $VMName -SourcePath (Resolve-File -path $GuestEvidenceWix6ScriptPath -label 'Guest Wix6 evidence script') -DestinationPath $wix6GuestPath -FileSource Host -CreateFullPath -Force

Write-Output "Copied WiX3 baseline to: $baselineGuestPath"
Write-Output "Copied WiX6 candidate to: $candidateGuestPath"
Write-Output "Copied guest evidence script to: $scriptGuestPath"
Write-Output "Copied guest common evidence script to: $commonGuestPath"
Write-Output "Copied guest Wix3 double-click script to: $wix3GuestPath"
Write-Output "Copied guest Wix6 double-click script to: $wix6GuestPath"
Write-Output "GuestRunRoot=$guestRunRoot"
