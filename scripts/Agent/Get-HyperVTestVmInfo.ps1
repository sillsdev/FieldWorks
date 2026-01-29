[CmdletBinding()]
param(
	[Parameter(Mandatory = $false)]
	[string]$VMName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

try {
	Import-Module Hyper-V -ErrorAction Stop
} catch {
	throw "Hyper-V PowerShell module is required. Enable Hyper-V and ensure the Hyper-V module is installed. Error: $_"
}

if ([string]::IsNullOrWhiteSpace($VMName)) {
	$vms = @(Get-VM -ErrorAction Stop)
	Write-Output ("VM Count: {0}" -f $vms.Count)
	foreach ($vm in $vms) {
		Write-Output ("{0} - {1}" -f $vm.Name, $vm.State)
	}
	return
}

$vm = Get-VM -Name $VMName -ErrorAction Stop
Write-Output ("VM: {0}" -f $vm.Name)
Write-Output ("State: {0}" -f $vm.State)
Write-Output ("Generation: {0}" -f $vm.Generation)

$snapshots = @(Get-VMSnapshot -VMName $VMName -ErrorAction SilentlyContinue)
if ($null -eq $snapshots -or $snapshots.Count -eq 0) {
	Write-Output "Snapshots: (none)"
	return
}

Write-Output ("Snapshots: {0}" -f $snapshots.Count)
foreach ($s in $snapshots) {
	Write-Output ("- {0}" -f $s.Name)
}
