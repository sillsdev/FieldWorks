<#
.SYNOPSIS
	Forces FieldWorks to launch in LEGACY (WinForms) UI mode. Run this BEFORE winforms_launch_app.

.DESCRIPTION
	This skill scrapes the legacy WinForms surface for "truth" screenshots, workflows, and behaviour
	(the Avalonia surface is captured separately, headless). The WinForms UIA2 MCP can ONLY see WinForms:
	if FieldWorks comes up in New (Avalonia) UI mode, the element tree is empty and PrintWindow renders a
	blank window — the process looks healthy but nothing is visible to the MCP.

	UI mode is the `UIMode` application setting (default "Legacy"), persisted by libpalaso's
	CrossPlatformSettingsProvider at:
	    %LOCALAPPDATA%\SIL\SIL FieldWorks\<version>\user.config
	(NOT the per-exe `FieldWorks.exe_Url_*` files, which hold other settings). When that value is "New",
	FieldWorks launches Avalonia. This script sets it to "Legacy" in every existing FieldWorks settings
	store (and, if the exe's own version store is missing, creates it), so the next launch is WinForms.

	Idempotent and safe to run every time. SIDE EFFECT: this also flips the developer's own persisted
	UI mode to Legacy — re-select New in Tools ▸ Options (or re-run with -RestoreNew) when you want the
	Avalonia surface back interactively.

.PARAMETER Configuration
	Debug or Release — used only to locate the exe and read its version. Default Debug.

.PARAMETER RestoreNew
	Instead of forcing Legacy, set UIMode back to New (developer convenience).

.EXAMPLE
	.\.claude\skills\fieldworks-winapp\scripts\Set-FieldWorksLegacyMode.ps1
#>
[CmdletBinding()]
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',
	[switch]$RestoreNew
)

$ErrorActionPreference = 'Stop'
$target = if ($RestoreNew) { 'New' } else { 'Legacy' }
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$storeRoot = Join-Path $env:LOCALAPPDATA 'SIL\SIL FieldWorks'

# The setting lives under <userSettings>/<SIL.FieldWorks.Common.FwUtils.Properties.Settings>/setting[@name='UIMode']/value
function Set-UIMode([string]$configPath, [string]$value) {
	[xml]$doc = Get-Content -LiteralPath $configPath -Raw
	$settings = $doc.SelectSingleNode("//userSettings/*[local-name()='SIL.FieldWorks.Common.FwUtils.Properties.Settings']")
	if ($null -eq $settings) { return $false }
	$node = $settings.SelectSingleNode("setting[@name='UIMode']")
	if ($null -eq $node) {
		$node = $doc.CreateElement('setting')
		$node.SetAttribute('name', 'UIMode')
		$node.SetAttribute('serializeAs', 'String')
		$v = $doc.CreateElement('value'); $v.InnerText = $value
		[void]$node.AppendChild($v)
		[void]$settings.AppendChild($node)
	}
	else {
		$v = $node.SelectSingleNode('value')
		if ($null -eq $v) { $v = $doc.CreateElement('value'); [void]$node.AppendChild($v) }
		$v.InnerText = $value
	}
	$doc.Save($configPath)
	return $true
}

$changed = @()
if (Test-Path $storeRoot) {
	foreach ($cfg in Get-ChildItem -LiteralPath $storeRoot -Recurse -Filter 'user.config' -ErrorAction SilentlyContinue) {
		try { if (Set-UIMode $cfg.FullName $target) { $changed += $cfg.FullName } }
		catch { Write-Host "[WARN] Could not update $($cfg.FullName): $_" -ForegroundColor Yellow }
	}
}

# Ensure the running exe's OWN version store has the setting (covers a freshly-built version with no store yet).
$exe = Join-Path $repoRoot "Output/$Configuration/FieldWorks.exe"
if (Test-Path $exe) {
	$ver = (Get-Item $exe).VersionInfo.FileVersion
	if ($ver) {
		$verCfg = Join-Path $storeRoot "$ver\user.config"
		if (-not (Test-Path $verCfg)) {
			New-Item -ItemType Directory -Force -Path (Split-Path $verCfg) | Out-Null
			@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <sectionGroup name="userSettings" type="System.Configuration.UserSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089">
      <section name="SIL.FieldWorks.Common.FwUtils.Properties.Settings" type="System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
    </sectionGroup>
  </configSections>
  <userSettings>
    <SIL.FieldWorks.Common.FwUtils.Properties.Settings>
      <setting name="UIMode" serializeAs="String">
        <value>$target</value>
      </setting>
    </SIL.FieldWorks.Common.FwUtils.Properties.Settings>
  </userSettings>
</configuration>
"@ | Set-Content -LiteralPath $verCfg -Encoding UTF8
			$changed += "$verCfg (created)"
		}
		elseif ($changed -notcontains $verCfg) {
			if (Set-UIMode $verCfg $target) { $changed += $verCfg }
		}
	}
}

if ($changed.Count -eq 0) {
	Write-Host "[OK] No FieldWorks settings store found; default UI mode is Legacy — nothing to do." -ForegroundColor Green
}
else {
	Write-Host "[OK] UIMode set to '$target' in:" -ForegroundColor Green
	$changed | ForEach-Object { Write-Host "       $_" -ForegroundColor DarkGray }
}
Write-Host "Launch FieldWorks now (winforms_launch_app); it will start in $target UI mode." -ForegroundColor Cyan
