<#
.SYNOPSIS
	Preflight check for driving FieldWorks (WinForms) through the winforms-mcp MCP server.

.DESCRIPTION
	The winforms-mcp server (@fnrhombus/winforms-mcp) is launched by the MCP client itself
	(Claude Code reads .mcp.json; VS Code reads .vscode/mcp.json). This script does NOT start
	the server — it verifies the prerequisites that make `winforms_launch_app` succeed, and
	prints the exact FieldWorks.exe path to launch. Run it before a parity/screenshot session.

	Checks:
	  1. node + npx are on PATH (the server runs under npx).
	  2. The @fnrhombus/winforms-mcp package resolves on the registry (will be fetched on first use).
	  3. Output/<Configuration>/FieldWorks.exe exists (the app the MCP drives) — else points at build.ps1.
	  4. ICU_DATA is discoverable (FieldWorks needs it; mirrors test.ps1's resolution) — advisory.
	  5. .mcp.json registers winforms-mcp for Claude Code (the fix for "no winforms_* tools").

.PARAMETER Configuration
	Debug or Release. Default Debug.

.PARAMETER PrewarmPackage
	Also run `npx -y @fnrhombus/winforms-mcp --help` once to pre-download the package so the
	first MCP launch is not delayed by the install. Optional.

.EXAMPLE
	.\.claude\skills\fieldworks-winapp\scripts\Preflight-WinFormsMcp.ps1
#>
[CmdletBinding()]
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',
	[switch]$PrewarmPackage
)

$ErrorActionPreference = 'Stop'
# repo root = three levels up from .claude/skills/fieldworks-winapp/scripts
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$ok = $true

function Write-Check([bool]$pass, [string]$label, [string]$detail) {
	$mark = if ($pass) { '[OK]  ' } else { '[FAIL]' }
	$color = if ($pass) { 'Green' } else { 'Red' }
	Write-Host "$mark $label" -ForegroundColor $color
	if ($detail) { Write-Host "        $detail" -ForegroundColor DarkGray }
	if (-not $pass) { $script:ok = $false }
}

Write-Host "winforms-mcp preflight (FieldWorks WinForms control)" -ForegroundColor Cyan
Write-Host "Repo: $repoRoot`n"

# 1. node + npx
$node = (Get-Command node -ErrorAction SilentlyContinue)?.Source
$npx = (Get-Command npx -ErrorAction SilentlyContinue)?.Source
Write-Check ([bool]$node) "node on PATH" $node
Write-Check ([bool]$npx) "npx on PATH" $npx

# 2. package resolves (validate a real semver; a stray error string must not count as success).
# Routed through `cmd /c` because PowerShell mangles the leading '@' of a scoped package name — the
# same Windows quirk that makes the MCP server itself launch via `cmd /c npx` in .mcp.json.
$pkgVersion = $null
try { $pkgVersion = (& cmd /c 'npm view @fnrhombus/winforms-mcp version 2>NUL' | Select-Object -First 1) } catch {}
$pkgOk = ($pkgVersion -match '^\d+\.\d+\.\d+')
Write-Check $pkgOk "@fnrhombus/winforms-mcp resolves" `
	($(if ($pkgOk) { "version $pkgVersion (fetched on first MCP use)" } else { 'npm view failed — check network/registry' }))

# 3. FieldWorks.exe
$exe = Join-Path $repoRoot "Output/$Configuration/FieldWorks.exe"
Write-Check (Test-Path $exe) "FieldWorks.exe built ($Configuration)" `
	($(if (Test-Path $exe) { "winforms_launch_app path: $exe" } else { "missing — run: .\build.ps1 -Configuration $Configuration" }))

# 4. ICU_DATA (advisory; FieldWorks needs ICU at runtime)
$icuOk = $false
if ($env:ICU_DATA -and (Test-Path (Join-Path ($env:ICU_DATA.Split(';')[0]) 'nfc_fw.nrm'))) { $icuOk = $true }
else {
	$cand = Get-ChildItem -Path (Join-Path $repoRoot 'DistFiles') -Directory -Recurse -Filter 'icudt*l' -ErrorAction SilentlyContinue |
		Select-Object -First 1
	if ($cand) { $icuOk = $true; Write-Host "        (set ICU_DATA=$($cand.FullName) if FieldWorks fails to start)" -ForegroundColor DarkGray }
}
Write-Check $icuOk "ICU data discoverable" $(if ($icuOk) { 'advisory' } else { 'advisory — FieldWorks may need ICU_DATA set' })

# 5. .mcp.json registers winforms-mcp for Claude Code
$mcpJson = Join-Path $repoRoot '.mcp.json'
$registered = (Test-Path $mcpJson) -and ((Get-Content $mcpJson -Raw) -match 'winforms-mcp')
Write-Check $registered ".mcp.json registers winforms-mcp (Claude Code)" `
	($(if ($registered) { 'reconnect Claude Code to load the winforms_* tools (approve the project server when prompted)' } else { 'missing — Claude Code will not expose winforms_* tools' }))

if ($PrewarmPackage -and $npx) {
	Write-Host "`nPre-warming the MCP package..." -ForegroundColor Cyan
	try { & npx -y '@fnrhombus/winforms-mcp' --help *> $null } catch {}
	Write-Host "Done." -ForegroundColor DarkGray
}

Write-Host ""
if ($ok) {
	Write-Host "Preflight passed. Reconnect Claude Code, then drive FieldWorks via the winforms_* tools." -ForegroundColor Green
	exit 0
}
else {
	Write-Host "Preflight found problems above. Resolve them, then reconnect Claude Code." -ForegroundColor Red
	exit 1
}
