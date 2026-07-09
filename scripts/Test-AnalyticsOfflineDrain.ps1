#Requires -RunAsAdministrator
<#
.SYNOPSIS
	Smoke-tests SIL.DesktopAnalytics' durable Mixpanel spool (EventSpool +
	MixpanelEventSender) against a real, per-process network block.

.DESCRIPTION
	Unlike the old NetworkInterface.GetIsNetworkAvailable() gate, the durable
	MixpanelClient decides "offline" from real HTTP send failures. That means a
	per-process outbound firewall rule against just the test executable -- not a
	whole-machine network outage -- is now an accurate way to prove
	accumulate-while-blocked / drain-when-unblocked. This script:

	1. Builds DesktopAnalytics.net's SampleApp (a small console app that fires
	   two Track() calls with SampleApp's own internal wait/shutdown sequence).
	2. Blocks SampleApp.exe's outbound traffic with a scoped firewall rule.
	3. Runs SampleApp once fully blocked and confirms both events were
	   submitted but NOT delivered (they stay in the on-disk spool).
	4. Removes the block.
	5. Runs SampleApp again and confirms it delivers MORE events than it
	   itself submitted -- proof the leftover spooled events from the blocked
	   run were drained, not lost.

	The firewall rule is always removed, even on failure (see try/finally).
	No host-wide network state is ever touched.

	Both runs pass SampleApp's `c=false` flag to skip its built-in AllowTracking
	on/off demo: toggling AllowTracking calls PurgeQueuedEvents, which empties
	the entire on-disk spool immediately -- including events left over from the
	blocked run -- so with that demo left on, step 5's assertion could never
	actually pass (leftover events get purged, not delivered, before the next
	scheduled flush tick).

.PARAMETER DesktopAnalyticsPath
	Path to a local DesktopAnalytics.net checkout. Defaults to the
	DESKTOPANALYTICS_PATH env var used by Build/Manage-LocalLibraries.ps1.

.PARAMETER ApiSecret
	Mixpanel project token to use for this test run. If not supplied (and
	DESKTOPANALYTICS_TEST_API_SECRET is not set), this script reads
	FieldWorks' own DEBUG-build analytics key directly out of
	Src/Common/FieldWorks/FieldWorks.cs at run time (the "analyticsKey"
	constant under #if DEBUG) -- the same dev/test Mixpanel project every
	Debug FieldWorks build already reports to, kept separate from the
	RELEASE key. The key is read from that single source of truth each run,
	never duplicated as a literal in this script. Override with
	-ApiSecret/DESKTOPANALYTICS_TEST_API_SECRET to point elsewhere (e.g. a
	local WireMock stub's own placeholder token).

.PARAMETER ClientType
	DesktopAnalytics client type to exercise. Defaults to Mixpanel, which is
	the only client with the new durable spool as of this writing.

.PARAMETER Configuration
	Build configuration for SampleApp. Defaults to Debug.

.PARAMETER SkipClean
	Skip clearing the pre-existing on-disk spool for this API secret before
	the test. Without this switch, the spool directory for ApiSecret is
	deleted first so the two-run assertion below is deterministic.

.EXAMPLE
	.\scripts\Test-AnalyticsOfflineDrain.ps1 -DesktopAnalyticsPath C:\Repos\DesktopAnalytics.net
#>
[CmdletBinding()]
param(
	[string]$DesktopAnalyticsPath = $env:DESKTOPANALYTICS_PATH,

	[string]$ApiSecret = $env:DESKTOPANALYTICS_TEST_API_SECRET,

	[ValidateSet('Mixpanel', 'Segment')]
	[string]$ClientType = 'Mixpanel',

	[string]$Configuration = 'Debug',

	[switch]$SkipClean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsElevated {
	$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
	$principal = [Security.Principal.WindowsPrincipal]::new($identity)
	return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

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

function Get-SpoolPath {
	param([string]$Secret)

	$sha256 = [Security.Cryptography.SHA256]::Create()
	try {
		$hashBytes = $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($Secret))
	}
	finally {
		$sha256.Dispose()
	}
	# Matches EventSpool.HashApiKey: first 8 bytes, lowercase hex.
	$hex = -join ($hashBytes[0..7] | ForEach-Object { $_.ToString('x2') })
	return Join-Path $env:LOCALAPPDATA "SIL\DesktopAnalytics\spool\$hex"
}

function Invoke-SampleApp {
	param(
		[string]$ExePath,
		[string]$Secret,
		[string]$ClientType,
		[string]$StdOutPath
	)

	# c=false: skip SampleApp's built-in AllowTracking on/off demo. Toggling AllowTracking calls
	# PurgeQueuedEvents, which empties the ENTIRE on-disk spool immediately -- including any events
	# left over from a prior (blocked) run -- so with the toggle left on, this two-phase test could
	# never actually observe a drain: the leftover events get silently purged before the next
	# scheduled flush tick, not delivered. See SampleApp/Program.cs's "c[onsentToggle]" flag.
	$process = Start-Process -FilePath $ExePath `
		-ArgumentList @($Secret, $ClientType, 'c=false') `
		-NoNewWindow -Wait -PassThru `
		-RedirectStandardOutput $StdOutPath

	if ($process.ExitCode -ne 0) {
		throw "SampleApp exited with code $($process.ExitCode); see $StdOutPath"
	}

	$stdout = Get-Content -Path $StdOutPath -Raw
	$match = [regex]::Match($stdout, 'Succeeded:\s*(\d+);\s*Submitted:\s*(\d+);\s*Failed:\s*(\d+)')
	if (-not $match.Success) {
		Write-Output $stdout
		throw "Could not find a 'Succeeded: N; Submitted: N; Failed: N' line in SampleApp output (see above)."
	}

	return [PSCustomObject]@{
		Succeeded = [int]$match.Groups[1].Value
		Submitted = [int]$match.Groups[2].Value
		Failed    = [int]$match.Groups[3].Value
		StdOut    = $stdout
	}
}

if (-not (Test-IsElevated)) {
	throw "This script must run elevated (New-NetFirewallRule requires admin). Re-run from an elevated PowerShell session."
}

if ([string]::IsNullOrWhiteSpace($DesktopAnalyticsPath)) {
	throw "DesktopAnalyticsPath was not supplied and DESKTOPANALYTICS_PATH is not set."
}
if (-not (Test-Path $DesktopAnalyticsPath)) {
	throw "DesktopAnalyticsPath does not exist: $DesktopAnalyticsPath"
}
$sampleAppProj = Join-Path $DesktopAnalyticsPath 'src\SampleApp\SampleApp.csproj'
if (-not (Test-Path $sampleAppProj)) {
	throw "SampleApp project not found at $sampleAppProj"
}

if ([string]::IsNullOrWhiteSpace($ApiSecret)) {
	$fieldWorksRepoRoot = Split-Path -Parent $PSScriptRoot
	$ApiSecret = Get-FieldWorksDebugAnalyticsKey -RepoRoot $fieldWorksRepoRoot
	if ([string]::IsNullOrWhiteSpace($ApiSecret)) {
		throw "ApiSecret was not supplied, DESKTOPANALYTICS_TEST_API_SECRET is not set, and the DEBUG analytics key could not be read from Src\Common\FieldWorks\FieldWorks.cs under $fieldWorksRepoRoot. Pass -ApiSecret explicitly."
	}
	Write-Output "[INFO] No -ApiSecret supplied; using FieldWorks' own DEBUG-build Mixpanel key read from FieldWorks.cs (separate dev/test project from RELEASE)."
}

Write-Output "[INFO] Building SampleApp ($Configuration)..."
dotnet build $sampleAppProj -c $Configuration | Out-Null
if ($LASTEXITCODE -ne 0) {
	throw "dotnet build failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $DesktopAnalyticsPath "src\SampleApp\bin\$Configuration\net462\SampleApp.exe"
if (-not (Test-Path $exePath)) {
	throw "Built SampleApp.exe not found at $exePath"
}
Write-Output "[OK] Built $exePath"

$spoolPath = Get-SpoolPath -Secret $ApiSecret
Write-Output "[INFO] Spool directory for this API secret: $spoolPath"

if (-not $SkipClean -and (Test-Path $spoolPath)) {
	Write-Output "[INFO] Clearing pre-existing spool for a deterministic run..."
	Remove-Item -Path $spoolPath -Recurse -Force -Confirm:$false
}

$ruleName = 'FieldWorks-AnalyticsOfflineDrain-SmokeTest'
$tempDir = Join-Path ([IO.Path]::GetTempPath()) "AnalyticsOfflineDrain-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

$overallPass = $true

try {
	# Remove any stale rule from a previous interrupted run before adding a fresh one.
	Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue |
		Remove-NetFirewallRule -Confirm:$false -ErrorAction SilentlyContinue

	Write-Output "[INFO] Blocking outbound traffic for SampleApp.exe only (per-process, not host-wide)..."
	New-NetFirewallRule -DisplayName $ruleName -Direction Outbound -Program $exePath `
		-Action Block -Profile Any | Out-Null

	Write-Output "[INFO] Phase 1: running SampleApp while blocked (~45s)..."
	$blockedStdOut = Join-Path $tempDir 'phase1-blocked.log'
	$phase1 = Invoke-SampleApp -ExePath $exePath -Secret $ApiSecret -ClientType $ClientType -StdOutPath $blockedStdOut
	Write-Output "[INFO] Phase 1 result: Submitted=$($phase1.Submitted) Succeeded=$($phase1.Succeeded) Failed=$($phase1.Failed)"

	$phase1Ok = ($phase1.Submitted -gt 0) -and ($phase1.Succeeded -eq 0) -and ($phase1.Failed -eq 0)
	if ($phase1Ok) {
		Write-Output "[OK] Phase 1: events were submitted but not delivered while blocked -- accumulated in the spool as expected."
	}
	else {
		Write-Output "[FAIL] Phase 1: expected Submitted>0, Succeeded=0, Failed=0 while blocked. Got Submitted=$($phase1.Submitted) Succeeded=$($phase1.Succeeded) Failed=$($phase1.Failed)."
		$overallPass = $false
	}

	Write-Output "[INFO] Removing the per-process block..."
	Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue |
		Remove-NetFirewallRule -Confirm:$false

	Write-Output "[INFO] Phase 2: running SampleApp again, now unblocked (~45s)..."
	$unblockedStdOut = Join-Path $tempDir 'phase2-unblocked.log'
	$phase2 = Invoke-SampleApp -ExePath $exePath -Secret $ApiSecret -ClientType $ClientType -StdOutPath $unblockedStdOut
	Write-Output "[INFO] Phase 2 result: Submitted=$($phase2.Submitted) Succeeded=$($phase2.Succeeded) Failed=$($phase2.Failed)"

	$expectedLeftover = $phase1.Submitted - $phase1.Succeeded - $phase1.Failed
	$phase2Ok = ($phase2.Succeeded -ge ($phase2.Submitted + $expectedLeftover)) -and ($phase2.Failed -eq 0)
	if ($phase2Ok) {
		Write-Output "[OK] Phase 2: delivered $($phase2.Succeeded) events against only $($phase2.Submitted) submitted this run -- the $expectedLeftover event(s) spooled during Phase 1 were drained."
	}
	else {
		Write-Output "[FAIL] Phase 2: expected Succeeded >= Submitted + $expectedLeftover leftover event(s), Failed=0. Got Submitted=$($phase2.Submitted) Succeeded=$($phase2.Succeeded) Failed=$($phase2.Failed)."
		$overallPass = $false
	}

	if (Test-Path $spoolPath) {
		$remaining = Get-ChildItem -Path $spoolPath -File -ErrorAction SilentlyContinue
		Write-Output "[INFO] Spool directory now contains $($remaining.Count) file(s)."
	}
}
finally {
	Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue |
		Remove-NetFirewallRule -Confirm:$false -ErrorAction SilentlyContinue
	Write-Output "[INFO] Firewall rule '$ruleName' removed (or was already absent)."
	Write-Output "[INFO] Logs kept at: $tempDir"
}

if ($overallPass) {
	Write-Output "[OK] PASS: accumulate-while-blocked / drain-when-unblocked confirmed via a per-process firewall rule."
	exit 0
}
else {
	Write-Output "[FAIL] One or more assertions failed. See details above and logs in $tempDir."
	exit 1
}
