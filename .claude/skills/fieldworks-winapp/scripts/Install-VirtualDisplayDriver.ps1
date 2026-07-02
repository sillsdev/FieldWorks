<#
.SYNOPSIS
	Bootstrap the Virtual Display Driver (VDD) so FieldWorks can render/capture on a headless (invisible)
	desktop. Required for INVISIBLE WinForms scraping — FieldWorks' native Views (GDI) engine will not
	complete its main window on a display-less desktop, so a virtual monitor must be present.

.DESCRIPTION
	Source: https://github.com/VirtualDrivers/Virtual-Display-Driver (community, SignPath-signed).
	The supported install is the **VDD.Control** app (download → run → click Install); IddCx display
	drivers also require a software device node the control app creates, so there is no robust pure-CLI
	silent install. This script automates everything around that one click:
	  1. Detects an existing virtual display (idempotent — skips if already present).
	  2. Downloads the latest VDD.Control release asset from GitHub into <repo>\tools\vdd\.
	  3. Extracts it and ensures the config dir C:\VirtualDisplayDriver\ exists.
	  4. With -RunControl, launches VDD.Control ELEVATED (UAC) so you can click Install once.
	  5. Verifies a virtual monitor appeared.

	IMPORTANT (tested 2026-06-22): VDD does NOT make winforms-mcp HEADLESS (CreateDesktop) render
	FieldWorks — a created desktop is not bound to a display, so the virtual monitor (which serves the
	console/active desktop) does not reach it; headless stays blank. VDD's value is the alternative
	INVISIBLE path: run FieldWorks on the CONSOLE desktop with a virtual monitor positioned OFF the
	physical screen, then `winforms_attach_to_process` + capture. See references/headless-rendering.md.

.PARAMETER RunControl
	After download/extract, launch VDD.Control elevated for the one-time Install click.

.PARAMETER Force
	Re-download / re-run even if a virtual display is already detected.

.EXAMPLE
	.\.claude\skills\fieldworks-winapp\scripts\Install-VirtualDisplayDriver.ps1            # prepare + instructions
	.\.claude\skills\fieldworks-winapp\scripts\Install-VirtualDisplayDriver.ps1 -RunControl  # also launch the installer (UAC)
#>
[CmdletBinding()]
param([switch]$RunControl, [switch]$Force)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$toolsDir = Join-Path $repoRoot 'tools\vdd'
$repoApi = 'https://api.github.com/repos/VirtualDrivers/Virtual-Display-Driver/releases/latest'
$knownNames = '*Virtual Display*', '*VirtualDisplay*', '*MttVDD*', '*IddSample*'

function Test-VirtualDisplay {
	foreach ($n in $knownNames) {
		$d = Get-PnpDevice -FriendlyName $n -ErrorAction SilentlyContinue | Where-Object Status -eq 'OK'
		if ($d) { return $d | Select-Object -First 1 }
	}
	return $null
}

$existing = Test-VirtualDisplay
if ($existing -and -not $Force -and -not $RunControl) {
	Write-Host "[OK] The Virtual Display Driver is installed: $($existing.FriendlyName)" -ForegroundColor Green
	Write-Host "IMPORTANT: This does NOT make winforms-mcp HEADLESS (CreateDesktop) render FieldWorks — a"
	Write-Host "created desktop is not bound to a display, so the virtual monitor doesn't reach it (tested:"
	Write-Host "headless stays blank). VDD enables the INVISIBLE path differently: run FieldWorks on the"
	Write-Host "CONSOLE desktop with a virtual monitor positioned OFF your physical screen, then attach +"
	Write-Host "capture (the window renders because the console desktop is display-bound, and you don't see"
	Write-Host "it because it's on an off-screen monitor). Enable/position the monitor via VDD.Control or"
	Write-Host "C:\VirtualDisplayDriver\vdd_settings.xml. See references/headless-rendering.md."
	Write-Host "RESULT=ALREADY_INSTALLED"; return
}

Write-Host "Bootstrapping Virtual Display Driver (VDD) for headless FieldWorks capture..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null

# 1. Resolve the latest VDD.Control asset.
Write-Host "Querying latest release of VirtualDrivers/Virtual-Display-Driver..."
$headers = @{ 'User-Agent' = 'fw-winapp-skill'; 'Accept' = 'application/vnd.github+json' }
$rel = Invoke-RestMethod -Uri $repoApi -Headers $headers
$asset = $rel.assets | Where-Object { $_.name -like 'VDD.Control*.zip' } | Select-Object -First 1
if (-not $asset) { throw "No VDD.Control asset found in release $($rel.tag_name)." }
$zip = Join-Path $toolsDir $asset.name
Write-Host "Release $($rel.tag_name): $($asset.name) ($([math]::Round($asset.size/1MB,1)) MB)"

# 2. Download (skip if already have this version's zip).
if ((Test-Path $zip) -and -not $Force) {
	Write-Host "Already downloaded: $zip"
}
else {
	Write-Host "Downloading to $zip ..."
	Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -Headers $headers
}

# 3. Extract.
$extractDir = Join-Path $toolsDir ([System.IO.Path]::GetFileNameWithoutExtension($asset.name))
if (Test-Path $extractDir) { Remove-Item -Recurse -Force $extractDir }
Write-Host "Extracting..."
Expand-Archive -LiteralPath $zip -DestinationPath $extractDir -Force

# 4. Ensure the config directory the driver reads exists (it ships a default vdd_settings.xml on install).
$cfgDir = 'C:\VirtualDisplayDriver'
if (-not (Test-Path $cfgDir)) {
	try { New-Item -ItemType Directory -Force -Path $cfgDir | Out-Null; Write-Host "Created $cfgDir" }
	catch { Write-Host "[WARN] Could not create $cfgDir (needs admin); VDD.Control will create it." -ForegroundColor Yellow }
}

$control = Get-ChildItem -Path $extractDir -Recurse -Filter '*.exe' -ErrorAction SilentlyContinue |
	Where-Object { $_.Name -match 'Control|VDD' } | Select-Object -First 1
if (-not $control) { $control = Get-ChildItem -Path $extractDir -Recurse -Filter '*.exe' | Select-Object -First 1 }

Write-Host ""
Write-Host "[OK] VDD.Control prepared at: $($control.FullName)" -ForegroundColor Green
Write-Host "Driver install requires ADMIN and a one-time click (signed IddCx driver + device-node creation)."

if ($RunControl -and $control) {
	Write-Host "Launching VDD.Control elevated (approve the UAC prompt, then click Install)..." -ForegroundColor Cyan
	Start-Process -FilePath $control.FullName -Verb RunAs
	Write-Host "After it reports installed, re-run this script (no args) to verify the virtual monitor."
	Write-Host "RESULT=LAUNCHED_CONTROL"; return
}

Write-Host ""
Write-Host "NEXT STEP (manual, one time):" -ForegroundColor Yellow
Write-Host "  Run elevated:  Start-Process '$($control.FullName)' -Verb RunAs   (then click Install)"
Write-Host "  Or re-run:     $PSCommandPath -RunControl"
Write-Host "Then re-run this script with no args to confirm, set .mcp.json HEADLESS=true, and reconnect."
Write-Host "RESULT=PREPARED"
