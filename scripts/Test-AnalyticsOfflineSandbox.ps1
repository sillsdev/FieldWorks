<#
.SYNOPSIS
	Runs DesktopAnalytics.net's SampleApp inside a Windows Sandbox with
	networking fully disabled, to confirm the durable Mixpanel spool never
	falsely reports delivery when there is genuinely no network route.

.DESCRIPTION
	Complementary to Test-AnalyticsOfflineDrain.ps1, which blocks traffic at
	the firewall (packets dropped, adapter stays "up"). This script instead
	boots a disposable Windows Sandbox guest with no virtual NIC at all
	(<Networking>Disable</Networking>), so the guest has no route to
	anywhere -- while the host machine's own network is completely
	untouched. It exists to guard against a regression where delivery
	success is inferred from NetworkInterface.GetIsNetworkAvailable() rather
	than a real per-event send outcome.

	Windows Sandbox is ephemeral and has no live network toggle, so this is
	a "born offline, stays offline" check, not an accumulate/drain
	transition test -- use Test-AnalyticsOfflineDrain.ps1 for the
	transition. Expected outcome here: every event is submitted, none are
	delivered, none are poison-dropped.

	A small run.cmd is generated into a temp, read-only-mapped folder
	alongside the built SampleApp.exe; it launches SampleApp with the
	supplied API secret, greps its "Succeeded: N; Submitted: N; Failed: N"
	line, writes a result.txt into a separate writable-mapped folder, then
	shuts the sandbox down so this script can detect completion. The temp
	directory (which briefly contains the API secret in run.cmd) is deleted
	afterward.

.PARAMETER DesktopAnalyticsPath
	Path to a local DesktopAnalytics.net checkout. Defaults to the
	DESKTOPANALYTICS_PATH env var.

.PARAMETER ApiSecret
	Mixpanel project token to use for this test run. If not supplied (and
	DESKTOPANALYTICS_TEST_API_SECRET is not set), this script reads
	FieldWorks' own DEBUG-build analytics key directly out of
	Src/Common/FieldWorks/FieldWorks.cs at run time (the "analyticsKey"
	constant under #if DEBUG) -- the same dev/test Mixpanel project every
	Debug FieldWorks build already reports to. Doesn't matter much here
	either way: with no guest NIC at all, no connection is ever attempted
	regardless of token validity.

.PARAMETER ClientType
	DesktopAnalytics client type to exercise. Defaults to Mixpanel.

.PARAMETER Configuration
	Build configuration for SampleApp. Defaults to Debug.

.PARAMETER TimeoutSeconds
	How long to wait for the sandbox to finish and write result.txt before
	giving up. Defaults to 120 (SampleApp's own run is ~45s plus sandbox
	boot time).

.EXAMPLE
	.\scripts\Test-AnalyticsOfflineSandbox.ps1 -DesktopAnalyticsPath C:\Repos\DesktopAnalytics.net
#>
[CmdletBinding()]
param(
	[string]$DesktopAnalyticsPath = $env:DESKTOPANALYTICS_PATH,

	[string]$ApiSecret = $env:DESKTOPANALYTICS_TEST_API_SECRET,

	[ValidateSet('Mixpanel', 'Segment')]
	[string]$ClientType = 'Mixpanel',

	[string]$Configuration = 'Debug',

	[int]$TimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-FieldWorksDebugAnalyticsKey {
	# Reads the DEBUG-build Mixpanel key directly out of FieldWorks.cs (single source of
	# truth) rather than duplicating it as a literal anywhere in this script.
	param([string]$RepoRoot)

	$fieldWorksCs = Join-Path $RepoRoot 'Src\Common\FieldWorks\FieldWorks.cs'
	if (-not (Test-Path $fieldWorksCs)) {
		return $null
	}

	$content = Get-Content -Path $fieldWorksCs -Raw
	$match = [regex]::Match($content, '#if DEBUG\s+const string analyticsKey = "([0-9a-fA-F]+)"')
	if (-not $match.Success) {
		return $null
	}
	return $match.Groups[1].Value
}

if ([string]::IsNullOrWhiteSpace($DesktopAnalyticsPath)) {
	throw "DesktopAnalyticsPath was not supplied and DESKTOPANALYTICS_PATH is not set."
}
if (-not (Test-Path $DesktopAnalyticsPath)) {
	throw "DesktopAnalyticsPath does not exist: $DesktopAnalyticsPath"
}

if ([string]::IsNullOrWhiteSpace($ApiSecret)) {
	$fieldWorksRepoRoot = Split-Path -Parent $PSScriptRoot
	$ApiSecret = Get-FieldWorksDebugAnalyticsKey -RepoRoot $fieldWorksRepoRoot
	if ([string]::IsNullOrWhiteSpace($ApiSecret)) {
		throw "ApiSecret was not supplied, DESKTOPANALYTICS_TEST_API_SECRET is not set, and the DEBUG analytics key could not be read from Src\Common\FieldWorks\FieldWorks.cs under $fieldWorksRepoRoot. Pass -ApiSecret explicitly."
	}
	Write-Output "[INFO] No -ApiSecret supplied; using FieldWorks' own DEBUG-build Mixpanel key read from FieldWorks.cs (separate dev/test project from RELEASE)."
}

$feature = Get-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -ErrorAction SilentlyContinue
if (-not $feature -or $feature.State -ne 'Enabled') {
	throw "Windows Sandbox is not enabled on this machine. Enable it via: Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All (requires a restart), or Settings > Apps > Optional features > Windows Sandbox."
}

$wsbExe = Get-Command WindowsSandbox.exe -ErrorAction SilentlyContinue
if (-not $wsbExe) {
	throw "WindowsSandbox.exe was not found on PATH even though the feature reports Enabled. A restart may be required after enabling it."
}

$sampleAppProj = Join-Path $DesktopAnalyticsPath 'src\SampleApp\SampleApp.csproj'
if (-not (Test-Path $sampleAppProj)) {
	throw "SampleApp project not found at $sampleAppProj"
}

Write-Output "[INFO] Building SampleApp ($Configuration)..."
dotnet build $sampleAppProj -c $Configuration | Out-Null
if ($LASTEXITCODE -ne 0) {
	throw "dotnet build failed with exit code $LASTEXITCODE"
}

$exeSourceDir = Join-Path $DesktopAnalyticsPath "src\SampleApp\bin\$Configuration\net462"
$exePath = Join-Path $exeSourceDir 'SampleApp.exe'
if (-not (Test-Path $exePath)) {
	throw "Built SampleApp.exe not found at $exePath"
}
Write-Output "[OK] Built $exePath"

$tempDir = Join-Path ([IO.Path]::GetTempPath()) "AnalyticsOfflineSandbox-$([Guid]::NewGuid().ToString('N'))"
$binDir = Join-Path $tempDir 'bin'
$outDir = Join-Path $tempDir 'out'
New-Item -ItemType Directory -Path $binDir -Force | Out-Null
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$guestBin = 'C:\AnalyticsTest\bin'
$guestOut = 'C:\AnalyticsTest\out'

try {
	Write-Output "[INFO] Staging SampleApp build output and a launcher into a temp folder (contains the test API secret; deleted at the end)..."
	Copy-Item -Path (Join-Path $exeSourceDir '*') -Destination $binDir -Recurse -Force

	# check-result.ps1: a real script FILE, not a fragile inline `powershell -Command`
	# one-liner with several layers of cmd.exe/PowerShell quote-escaping (the previous
	# design). That inline form failed silently -- run.cmd still reached `shutdown`
	# (proving the guest ran to completion), but result.txt never appeared, which a
	# batch file's lack of any error propagation between lines would hide either way.
	# A plain script file removes the escaping risk entirely and wraps its body in
	# try/catch so ANY failure still writes a diagnosable message to $ResultPath
	# instead of silently leaving nothing behind.
	$checkResultPath = Join-Path $binDir 'check-result.ps1'
	$checkResultContent = @'
param(
	[string]$LogPath,
	[string]$ResultPath
)
try {
	$content = Get-Content -Path $LogPath -Raw -ErrorAction Stop
	$m = [regex]::Match($content, 'Succeeded:\s*(\d+);\s*Submitted:\s*(\d+);\s*Failed:\s*(\d+)')
	if (-not $m.Success) {
		"FAIL: no stats line found in $LogPath. Content: $content" | Out-File -FilePath $ResultPath -Encoding utf8
		return
	}
	$succeeded = [int]$m.Groups[1].Value
	$submitted = [int]$m.Groups[2].Value
	$failed = [int]$m.Groups[3].Value
	$pass = ($submitted -gt 0) -and ($succeeded -eq 0) -and ($failed -eq 0)
	$status = if ($pass) { 'PASS' } else { 'FAIL' }
	"${status}: Submitted=$submitted Succeeded=$succeeded Failed=$failed" | Out-File -FilePath $ResultPath -Encoding utf8
}
catch {
	"FAIL: check-result.ps1 threw: $_" | Out-File -FilePath $ResultPath -Encoding utf8
}
'@
	Set-Content -Path $checkResultPath -Value $checkResultContent -Encoding UTF8

	# run.cmd: launch SampleApp with the secret, run check-result.ps1 to write a
	# PASS/FAIL result.txt to the writable mapped folder, then shut the guest down so
	# the host-side wait loop below can detect completion without manual interaction.
	# marker.txt records SampleApp's own exit code as an extra diagnostic in case
	# result.txt is still missing (e.g. SampleApp itself crashed before ever logging a
	# stats line). The `ping` line between the write and `shutdown` is a deliberate
	# delay (the traditional cmd.exe sleep -- `timeout` refuses to run with
	# redirected/non-interactive input, which is exactly this context): Windows
	# Sandbox's mapped folders are a shared-folder sync back to the host, not a local
	# write, and `shutdown /s /t 0` tears the guest down immediately, so without a
	# buffer here the guest could in principle disconnect before a write has actually
	# propagated to the host.
	$runCmdPath = Join-Path $binDir 'run.cmd'
	$runCmdLines = @(
		'@echo off'
		"`"$guestBin\SampleApp.exe`" `"$ApiSecret`" $ClientType > `"$guestOut\sampleapp.log`" 2>&1"
		"echo SAMPLEAPP_EXITCODE=%ERRORLEVEL% > `"$guestOut\marker.txt`""
		"powershell -NoProfile -ExecutionPolicy Bypass -File `"$guestBin\check-result.ps1`" -LogPath `"$guestOut\sampleapp.log`" -ResultPath `"$guestOut\result.txt`""
		'ping -n 6 127.0.0.1 >nul'
		'shutdown /s /t 0'
	)
	Set-Content -Path $runCmdPath -Value $runCmdLines -Encoding ASCII

	$wsbPath = Join-Path $tempDir 'offline-check.wsb'
	$wsbContent = @"
<Configuration>
  <Networking>Disable</Networking>
  <MappedFolders>
    <MappedFolder>
      <HostFolder>$binDir</HostFolder>
      <SandboxFolder>$guestBin</SandboxFolder>
      <ReadOnly>true</ReadOnly>
    </MappedFolder>
    <MappedFolder>
      <HostFolder>$outDir</HostFolder>
      <SandboxFolder>$guestOut</SandboxFolder>
      <ReadOnly>false</ReadOnly>
    </MappedFolder>
  </MappedFolders>
  <LogonCommand>
    <Command>cmd /c $guestBin\run.cmd</Command>
  </LogonCommand>
</Configuration>
"@
	Set-Content -Path $wsbPath -Value $wsbContent -Encoding UTF8

	Write-Output "[INFO] Launching Windows Sandbox (networking disabled; host network is untouched)..."
	Start-Process -FilePath $wsbPath | Out-Null

	$resultPath = Join-Path $outDir 'result.txt'
	Write-Output "[INFO] Waiting up to $TimeoutSeconds s for the sandbox to finish and shut down..."
	$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
	while (-not (Test-Path $resultPath) -and (Get-Date) -lt $deadline) {
		Start-Sleep -Seconds 2
	}

	if (-not (Test-Path $resultPath)) {
		throw "Timed out after $TimeoutSeconds s waiting for $resultPath. The sandbox may still be running -- check for a WindowsSandboxRemoteSession window."
	}

	$result = Get-Content -Path $resultPath -Raw
	Write-Output "[INFO] Sandbox result: $result"

	if ($result -match '^PASS') {
		Write-Output "[OK] PASS: with no guest NIC at all, events were submitted but never reported delivered."
		exit 0
	}
	else {
		Write-Output "[FAIL] Sandbox reported: $result"
		exit 1
	}
}
finally {
	Start-Sleep -Seconds 3  # let the sandbox fully release its mapped-folder handles after shutdown

	# The bin folder holds run.cmd with the API secret embedded in cleartext -- always
	# remove that promptly regardless of outcome.
	Remove-Item -Path $binDir -Recurse -Force -ErrorAction SilentlyContinue

	if (Test-Path (Join-Path $outDir 'result.txt')) {
		Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
		Write-Output "[INFO] Cleaned up temp folder (including the API secret it briefly held)."
	}
	else {
		# No result.txt: keep the (secret-free) out folder around so marker.txt/sampleapp.log
		# can be inspected instead of losing all diagnostics to an unconditional delete, as
		# happened on earlier timed-out runs.
		Write-Output "[INFO] No result.txt was found; kept $outDir for diagnostics (marker.txt has SampleApp's exit code, sampleapp.log has its output). The API-secret-bearing bin folder was still removed."
	}
}
