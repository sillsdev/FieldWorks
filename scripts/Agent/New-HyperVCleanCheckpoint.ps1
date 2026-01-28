[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$VMName,

	[Parameter(Mandatory = $true)]
	[string]$CheckpointName,

	[Parameter(Mandatory = $false)]
	[switch]$StopVmFirst,

	# Skip the host-side OS disk validation check.
	# By default, this script will refuse to create a checkpoint if the VM's OS disk has no partitions (RAW),
	# since that typically means Windows hasn't been installed yet.
	[Parameter(Mandatory = $false)]
	[switch]$SkipOsDiskCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
	Import-Module Hyper-V -ErrorAction Stop
} catch {
	throw "Hyper-V PowerShell module is required. Enable Hyper-V and ensure the Hyper-V module is installed. Error: $_"
}

function Test-IsAdministrator {
	$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
	$principal = New-Object System.Security.Principal.WindowsPrincipal($identity)
	return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-OsDiskBaseVhdPath {
	param([Parameter(Mandatory = $true)][string]$VMName)
	$disk = Get-VMHardDiskDrive -VMName $VMName -ErrorAction Stop |
		Sort-Object -Property ControllerType, ControllerNumber, ControllerLocation |
		Select-Object -First 1
	if ($null -eq $disk -or [string]::IsNullOrWhiteSpace($disk.Path)) {
		return $null
	}
	$vhd = Get-VHD -Path $disk.Path -ErrorAction Stop
	if ($vhd.VhdType -eq 'Differencing' -and -not [string]::IsNullOrWhiteSpace($vhd.ParentPath)) {
		return [string]$vhd.ParentPath
	}
	return [string]$disk.Path
}

function Assert-OsDiskLooksInitialized {
	param(
		[Parameter(Mandatory = $true)][string]$VMName
	)

	if ($SkipOsDiskCheck) { return }

	if (-not (Test-IsAdministrator)) {
		Write-Output "[WARN] Skipping OS disk validation (not elevated)."
		Write-Output "[WARN] If the VM boots to 'no operating system was loaded', install Windows first, then recreate the checkpoint."
		return
	}

	$baseVhd = Get-OsDiskBaseVhdPath -VMName $VMName
	if ([string]::IsNullOrWhiteSpace($baseVhd) -or !(Test-Path -LiteralPath $baseVhd -PathType Leaf)) {
		throw "Could not resolve a VHD/VHDX path for VM '$VMName'."
	}

	$mounted = $false
	try {
		Mount-VHD -Path $baseVhd -ReadOnly -ErrorAction Stop | Out-Null
		$mounted = $true
		$img = Get-DiskImage -ImagePath $baseVhd -ErrorAction Stop
		$diskNum = $img.Number
		if ($null -eq $diskNum) { throw "Mounted VHD but DiskImage.Number is null." }

		$disk = Get-Disk -Number $diskNum -ErrorAction Stop
		if ($disk.PartitionStyle -eq 'RAW') {
			throw "VM '$VMName' OS disk is RAW (no partitions). Windows is likely not installed yet; do not checkpoint this state."
		}

		$parts = @(Get-Partition -DiskNumber $diskNum -ErrorAction SilentlyContinue)
		if ($parts.Count -eq 0) {
			throw "VM '$VMName' OS disk has no partitions. Windows is likely not installed yet; do not checkpoint this state."
		}
	} finally {
		if ($mounted) {
			try { Dismount-VHD -Path $baseVhd -ErrorAction SilentlyContinue | Out-Null } catch { }
		}
	}
}

$vm = Get-VM -Name $VMName -ErrorAction Stop

if ($StopVmFirst -and $vm.State -ne 'Off') {
	Write-Output "Stopping VM '$VMName' before checkpoint (state: $($vm.State))"
	Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop
}

if (-not $SkipOsDiskCheck) {
	if ($vm.State -ne 'Off' -and -not $StopVmFirst) {
		Write-Output "[WARN] VM '$VMName' is running; skipping OS disk validation unless you stop it first (-StopVmFirst)."
	} else {
		Assert-OsDiskLooksInitialized -VMName $VMName
	}
}

$existing = Get-VMSnapshot -VMName $VMName -Name $CheckpointName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
	Write-Output "Checkpoint already exists: $CheckpointName"
	return $existing
}

Write-Output "Creating checkpoint '$CheckpointName' for VM '$VMName'"
$cp = Checkpoint-VM -Name $VMName -SnapshotName $CheckpointName -ErrorAction Stop

return $cp
