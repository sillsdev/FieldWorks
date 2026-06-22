<#
.SYNOPSIS
	Plug in the Virtual Display Driver's virtual monitor by restarting its device (self-elevating).

.DESCRIPTION
	The VDD device (ROOT\DISPLAY) can be installed and loaded yet have NO monitor attached (so the desktop
	arrangement shows only the physical screen). IddCx creates the monitor(s) configured in
	C:\VirtualDisplayDriver\vdd_settings.xml (<monitors><count>) when the device (re)starts. This script
	disables then re-enables the device to make the monitor appear. Restarting a PnP device needs admin, so
	the script self-elevates (one UAC prompt). It only touches the VDD device, not the physical GPU.

	After this, the off-screen-monitor invisible path works: launch FieldWorks on the console desktop, move
	its window onto the virtual monitor (positioned off the physical screen), then attach + capture.

.PARAMETER Elevated
	Internal — set when the script relaunches itself elevated.

.OUTPUTS
	Writes the post-restart screen list to <repo>\Output\ManualEvidence\vdd-monitors.txt and prints it.
#>
[CmdletBinding()]
param([switch]$Elevated, [switch]$Disable)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$resultFile = Join-Path $repoRoot 'Output\ManualEvidence\vdd-monitors.txt'
New-Item -ItemType Directory -Force -Path (Split-Path $resultFile) | Out-Null

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)

if (-not $isAdmin) {
	Write-Host "Restarting the virtual-display device needs admin — approve the UAC prompt..." -ForegroundColor Cyan
	$exe = (Get-Process -Id $PID).Path   # pwsh or powershell
	if (Test-Path $resultFile) { Remove-Item $resultFile -Force }
	$relArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Elevated')
	if ($Disable) { $relArgs += '-Disable' }
	Start-Process -FilePath $exe -Verb RunAs -Wait -ArgumentList $relArgs
	if (Test-Path $resultFile) {
		Write-Host "`n=== Monitors after restart ===" -ForegroundColor Green
		Get-Content $resultFile | ForEach-Object { Write-Host $_ }
	}
	else {
		Write-Host "[WARN] No result written — elevation may have been declined." -ForegroundColor Yellow
	}
	return
}

# --- elevated body ---
$dev = Get-PnpDevice -FriendlyName '*Virtual Display*' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $dev) { 'ERROR: Virtual Display device not found' | Set-Content $resultFile; return }

if ($Disable) {
	Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false
	Start-Sleep -Seconds 3
}
else {
	Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
	Start-Sleep -Seconds 2
	Enable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false
	Start-Sleep -Seconds 4
}

Add-Type -AssemblyName System.Windows.Forms
$lines = [System.Windows.Forms.Screen]::AllScreens | ForEach-Object {
	"{0}  Primary={1}  Bounds={2}" -f $_.DeviceName, $_.Primary, $_.Bounds.ToString()
}
$lines | Set-Content -LiteralPath $resultFile -Encoding UTF8
