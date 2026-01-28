param(
	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]$VMName = 'FwInstallerTest',

	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]$CheckpointName = 'fresh-install',

	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]$GuestWorkRoot = 'C:\\FWInstallerTest',

	[Parameter()]
	[string]$Wix3BaselineInstallerPath = 'C:\\ProgramData\\FieldWorks\\HyperV\\Installers\\Wix3Baseline\\FieldWorks_9.2.11.1_Online_x64.exe',

	[Parameter()]
	[string]$Wix6CandidateInstallerPath = 'FLExInstaller\\bin\\x64\\Release\\FieldWorksBundle.exe'
	,
	[Parameter()]
	[switch]$ForceRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$vm = Get-VM -Name $VMName -ErrorAction Stop

$CheckpointName = $CheckpointName.Trim()

$snapshots = @(Get-VMSnapshot -VMName $VMName -ErrorAction SilentlyContinue)

if (-not $snapshots -or $snapshots.Count -eq 0) {
	throw "VM '$VMName' has no checkpoints. Create a clean checkpoint in Hyper-V Manager and retry."
}

$targetCheckpointName = $CheckpointName
$matching = @($snapshots | Where-Object { $_.Name -eq $CheckpointName })

if ($matching.Count -eq 0) {
	$manual = @($snapshots | Where-Object { -not $_.IsAutomaticCheckpoint })
	if ($manual.Count -eq 1) {
		$targetCheckpointName = $manual[0].Name
		Write-Output "Requested checkpoint '$CheckpointName' not found. Auto-selecting the only manual checkpoint: '$targetCheckpointName'."
	} else {
		$available = ($snapshots | ForEach-Object { $_.Name }) -join "`n  - "
		throw "Unable to find checkpoint '$CheckpointName' for VM '$VMName'. Available checkpoints:`n  - $available`nRe-run with -CheckpointName set to one of the names above."
	}
}

Write-Output "Available checkpoints for '$VMName':"
$snapshots | ForEach-Object { Write-Output ("  - " + $_.Name) }

Write-Output "Restoring checkpoint '$targetCheckpointName' for VM '$VMName'..."

if (-not $ForceRestore) {
	Write-Output "WARNING: This will restore checkpoint '$targetCheckpointName' and discard any VM changes since that checkpoint."
	Write-Output "Re-run with -ForceRestore when you're ready."
	return
}

# Ensure VM is not running during restore.
if ($vm.State -ne 'Off') {
	Write-Output "Stopping VM '$VMName' (current state: $($vm.State))..."
	Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop
}

Restore-VMSnapshot -VMName $VMName -Name $targetCheckpointName -Confirm:$false -ErrorAction Stop

Write-Output "Starting VM '$VMName'..."
Start-VM -Name $VMName -ErrorAction Stop | Out-Null

# Give Windows a moment to boot enough for Guest Services copy.
Start-Sleep -Seconds 5

Write-Output "Staging payload into guest via Guest Service Interface..."
$copyScript = Join-Path $PSScriptRoot 'Copy-HyperVParityPayload.ps1'

$copyArgs = @{
	VMName = $VMName
	GuestWorkRoot = $GuestWorkRoot
	Wix3BaselineInstallerPath = $Wix3BaselineInstallerPath
	Wix6CandidateInstallerPath = $Wix6CandidateInstallerPath
}

& $copyScript @copyArgs

Write-Output ''
Write-Output 'Next (inside the VM):'
Write-Output '1) If needed, run the one-time ExecutionPolicy step from specs/001-wix-v6-migration/HYPERV_INSTRUCTIONS.md'
Write-Output '2) Double-click Invoke-InstallerParityEvidence-Wix3.ps1 (then restore checkpoint and repeat for Wix6)'
