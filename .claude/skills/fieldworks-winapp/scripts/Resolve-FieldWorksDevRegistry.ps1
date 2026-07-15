<#
.SYNOPSIS
	Ensure the FieldWorks dev registry (RootCodeDir/RootDataDir) points at THIS worktree before launching,
	auto-realigning when it is safe to do so.

.DESCRIPTION
	A FieldWorks dev build resolves its code/config (DistFiles: parts, layouts, configuration) from
	HKCU\SOFTWARE\SIL\FieldWorks\9 RootCodeDir/RootDataDir. When those point at a DIFFERENT worktree than
	the exe being launched, the main window can fail to build (blank window, empty UIA tree) — so this
	skill aligns them to the running worktree before launch.

	Because that registry is shared across all worktrees, this script will NOT clobber it if the other
	worktree is actively relying on it. "Actively using it" =
	  (a) a FieldWorks.exe process is currently running from the other worktree's tree, OR
	  (b) the FieldWorks registry key was last written within the last 24 hours (FieldWorks writes these
	      dirs on startup, so a recent write ≈ a recent launch from some worktree).
	If neither holds, the script realigns the registry to this worktree automatically. If either holds,
	it prints `RESULT=ASK_USER` and changes nothing, so the caller can ask the user before realigning.

.PARAMETER Force
	Realign regardless of the active-use check (use after the user approves).

.OUTPUTS
	A final line `RESULT=<ALREADY_ALIGNED|REALIGNED|ASK_USER|NO_REGISTRY>` for the caller to branch on.
#>
[CmdletBinding()]
param([switch]$Force)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$thisDist = Join-Path $repoRoot 'DistFiles'
$regPath = 'HKCU:\SOFTWARE\SIL\FieldWorks\9'

function Norm([string]$p) { if ([string]::IsNullOrWhiteSpace($p)) { return '' } try { return ([System.IO.Path]::GetFullPath($p)).TrimEnd('\').ToLowerInvariant() } catch { return $p.TrimEnd('\').ToLowerInvariant() } }

if (-not (Test-Path $regPath)) {
	Write-Host "[OK] No FieldWorks dev registry key yet; the exe will set it on first launch." -ForegroundColor Green
	Write-Host "RESULT=NO_REGISTRY"; return
}

$props = Get-ItemProperty -Path $regPath
$curCode = [string]$props.RootCodeDir
$curData = [string]$props.RootDataDir
Write-Host "This worktree DistFiles : $thisDist"
Write-Host "Registry RootCodeDir    : $curCode"
Write-Host "Registry RootDataDir    : $curData"

if ((Norm $curCode) -eq (Norm $thisDist) -and (Norm $curData) -eq (Norm $thisDist)) {
	Write-Host "[OK] Registry already points at this worktree." -ForegroundColor Green
	Write-Host "RESULT=ALREADY_ALIGNED"; return
}

function Set-DevDirs([string]$dist) {
	Set-ItemProperty -Path $regPath -Name RootCodeDir -Value $dist
	Set-ItemProperty -Path $regPath -Name RootDataDir -Value $dist
}

if ($Force) {
	Set-DevDirs $thisDist
	Write-Host "[OK] (-Force) Realigned RootCodeDir/RootDataDir to this worktree." -ForegroundColor Green
	Write-Host "RESULT=REALIGNED"; return
}

# The "other" worktree the registry currently points at (parent of its DistFiles).
$otherRoot = if ($curCode) { (Split-Path $curCode -Parent) } else { '' }

# (a) Active now: a FieldWorks.exe running from the other worktree's tree.
$activeProc = $null
try {
	$activeProc = Get-CimInstance Win32_Process -Filter "Name='FieldWorks.exe'" -ErrorAction SilentlyContinue |
		Where-Object { $_.ExecutablePath -and $otherRoot -and (Norm $_.ExecutablePath).StartsWith((Norm $otherRoot)) } |
		Select-Object -First 1
} catch {}

# (b) Used in the last 24h: the registry key's last-write time (FieldWorks writes these dirs on startup).
$recent = $false
try {
	$sig = @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class FwRegLW {
  [DllImport("advapi32.dll", SetLastError=true)]
  public static extern int RegQueryInfoKey(SafeRegistryHandle hKey, IntPtr c, IntPtr cc, IntPtr r,
    IntPtr sk, IntPtr msk, IntPtr mc, IntPtr v, IntPtr mvn, IntPtr mvl, IntPtr sd,
    out System.Runtime.InteropServices.ComTypes.FILETIME ft);
}
'@
	if (-not ('FwRegLW' -as [type])) { Add-Type -TypeDefinition $sig }
	$key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('SOFTWARE\SIL\FieldWorks\9')
	$ft = New-Object System.Runtime.InteropServices.ComTypes.FILETIME
	if ([FwRegLW]::RegQueryInfoKey($key.Handle, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [IntPtr]::Zero, [ref]$ft) -eq 0) {
		# FILETIME fields are signed Int32; reinterpret their raw bits as unsigned before combining.
		$high = [System.BitConverter]::ToUInt32([System.BitConverter]::GetBytes($ft.dwHighDateTime), 0)
		$low = [System.BitConverter]::ToUInt32([System.BitConverter]::GetBytes($ft.dwLowDateTime), 0)
		$lastWrite = [DateTime]::FromFileTimeUtc((([long]$high) -shl 32) -bor [long]$low)
		Write-Host "Registry key last written: $($lastWrite.ToLocalTime())"
		$recent = $lastWrite -gt (Get-Date).ToUniversalTime().AddHours(-24)
	}
	$key.Dispose()
} catch { Write-Host "[WARN] Could not read registry key write time: $_" -ForegroundColor Yellow }

if ($activeProc) {
	Write-Host "[HOLD] FieldWorks is running from the other worktree (PID $($activeProc.ProcessId)): $($activeProc.ExecutablePath)" -ForegroundColor Yellow
	Write-Host "RESULT=ASK_USER"; return
}
if ($recent) {
	Write-Host "[HOLD] The dev registry was used within the last 24h (another worktree may be active)." -ForegroundColor Yellow
	Write-Host "RESULT=ASK_USER"; return
}

Set-DevDirs $thisDist
Write-Host "[OK] Other worktree not active and not used in 24h — realigned the dev registry to this worktree." -ForegroundColor Green
Write-Host "RESULT=REALIGNED"
