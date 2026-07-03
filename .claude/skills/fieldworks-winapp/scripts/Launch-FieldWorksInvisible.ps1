<#
.SYNOPSIS
	DEFAULT launcher for FieldWorks WinForms capture on this dev machine: launch on the console desktop and
	move the window onto the VDD virtual monitor, so it renders but is invisible to you.

.DESCRIPTION
	FieldWorks' native Views (GDI) engine needs a display-bound desktop, so winforms-mcp HEADLESS
	(CreateDesktop) cannot render it (see references/headless-rendering.md). This launcher uses the working
	invisible path instead:
	  1. Forces Legacy UI mode (Set-FieldWorksLegacyMode.ps1) and aligns the dev registry to this worktree
	     (Resolve-FieldWorksDevRegistry.ps1).
	  2. Requires a Virtual Display Driver monitor to be plugged (one-time, via VDD Control —
	     Install-VirtualDisplayDriver.ps1 -RunControl). A virtual monitor has no physical panel, so any
	     window placed on it is invisible to the user.
	  3. Launches FieldWorks on the console desktop with `-db "<project>"` (ProcessStartInfo quotes the
	     space correctly; do NOT pass -app — it pops a blocking usage dialog).
	  4. Waits for the main window, then moves/maximizes it onto the virtual (non-primary) monitor.
	  5. Prints `PID=<n>`; the caller then `winforms_attach_to_process` + captures.

	FieldWorks remembers window placement, so after the first run it tends to reopen on the virtual monitor.

.PARAMETER Project       Project name (default 'Sena 3').
.PARAMETER Configuration Debug/Release (default Debug).
.PARAMETER LoadWaitSec   Max seconds to wait for the main window (default 120).
#>
[CmdletBinding()]
param([string]$Project = 'Sena 3', [ValidateSet('Debug','Release')][string]$Configuration = 'Debug', [int]$LoadWaitSec = 120)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path

# 1. Legacy + registry alignment (idempotent).
& (Join-Path $PSScriptRoot 'Set-FieldWorksLegacyMode.ps1') *> $null
& (Join-Path $PSScriptRoot 'Resolve-FieldWorksDevRegistry.ps1') *> $null

# 2. Require a virtual (non-primary) monitor.
Add-Type -AssemblyName System.Windows.Forms
$virt = [System.Windows.Forms.Screen]::AllScreens | Where-Object { -not $_.Primary } | Select-Object -First 1
if (-not $virt) {
	Write-Host "[ERROR] No virtual/secondary monitor found. Plug one once via:" -ForegroundColor Red
	Write-Host "        .\.claude\skills\fieldworks-winapp\scripts\Install-VirtualDisplayDriver.ps1 -RunControl"
	Write-Host "RESULT=NO_VIRTUAL_MONITOR"; exit 1
}
$b = $virt.Bounds
Write-Host "Virtual monitor: $($virt.DeviceName) Bounds=$b (window goes here — off your physical screen)."

# 3. Launch on the console desktop.
$exe = Join-Path $repoRoot "Output/$Configuration/FieldWorks.exe"
if (-not (Test-Path $exe)) { Write-Host "[ERROR] Not built: $exe (run .\build.ps1)"; exit 1 }
$psi = [System.Diagnostics.ProcessStartInfo]::new($exe)
$psi.ArgumentList.Add('-db'); $psi.ArgumentList.Add($Project); $psi.UseShellExecute = $true
$proc = [System.Diagnostics.Process]::Start($psi)
Write-Host "Launched FieldWorks PID=$($proc.Id) (-db `"$Project`"); waiting for main window..."

# 4. Wait for the main window, then move it onto the virtual monitor.
if (-not ('FwWin' -as [type])) {
	Add-Type @'
using System;using System.Runtime.InteropServices;
public static class FwWin{
 [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h,int x,int y,int w,int ht,bool repaint);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int cmd);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
}
'@
}
$deadline = (Get-Date).AddSeconds($LoadWaitSec)
$hwnd = [IntPtr]::Zero
while ((Get-Date) -lt $deadline) {
	$proc.Refresh()
	if ($proc.HasExited) { Write-Host "[ERROR] FieldWorks exited (code $($proc.ExitCode)) — check launch args."; exit 1 }
	if ($proc.MainWindowHandle -ne [IntPtr]::Zero -and -not [string]::IsNullOrEmpty($proc.MainWindowTitle)) {
		$hwnd = $proc.MainWindowHandle; break
	}
	Start-Sleep -Milliseconds 750
}
if ($hwnd -eq [IntPtr]::Zero) { Write-Host "[WARN] Main window not detected in $LoadWaitSec s; leaving as-is. PID=$($proc.Id)"; Write-Host "RESULT=NO_WINDOW"; exit 1 }

[void][FwWin]::ShowWindow($hwnd, 9)   # SW_RESTORE
[void][FwWin]::MoveWindow($hwnd, $b.X, $b.Y, $b.Width, $b.Height, $true)
[void][FwWin]::ShowWindow($hwnd, 3)   # SW_MAXIMIZE (maximizes on the monitor it now sits on)
Write-Host "[OK] FieldWorks moved onto the virtual monitor (invisible). Title: $($proc.MainWindowTitle)" -ForegroundColor Green
Write-Host "PID=$($proc.Id)"
Write-Host "RESULT=READY"
