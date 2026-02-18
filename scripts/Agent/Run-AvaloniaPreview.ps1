<#
.SYNOPSIS
  Builds and launches the FieldWorks Avalonia Preview Host.

.DESCRIPTION
  Provides a fast way to view Avalonia modules without running the full FieldWorks app.
  Modules register via [assembly: FwPreviewModule(...)] and may optionally provide sample data.

.EXAMPLE
  .\scripts\Agent\Run-AvaloniaPreview.ps1

.EXAMPLE
  .\scripts\Agent\Run-AvaloniaPreview.ps1 -Module advanced-entry -Data sample
#>

[CmdletBinding()]
param(
	[string]$Module = "advanced-entry",
	[ValidateSet("empty", "sample")]
	[string]$Data = "empty",
	[ValidateSet("Debug", "Release")]
	[string]$Configuration = "Debug",
	[switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$projectPath = Join-Path $repoRoot "Src\Common\FwAvaloniaPreviewHost\FwAvaloniaPreviewHost.csproj"

$moduleProjectPath = $null
if ($Module -and $Module.ToLowerInvariant() -eq "advanced-entry") {
	$moduleProjectPath = Join-Path $repoRoot "Src\LexText\AdvancedEntry.Avalonia\AdvancedEntry.Avalonia.csproj"
}

$helpersPath = Join-Path $repoRoot "Build\Agent\FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
	throw "FwBuildHelpers.psm1 not found at $helpersPath"
}
Import-Module $helpersPath -Force

Initialize-VsDevEnvironment
Test-CvtresCompatibility
$env:arch = 'x64'

Write-Host "Restoring Avalonia Preview Host ($Configuration)..." -ForegroundColor Cyan
Invoke-MSBuild -Arguments @(
	$projectPath,
	'/t:Restore',
	"/p:Configuration=$Configuration",
	'/p:Platform=x64',
	'/v:minimal',
	'/nologo'
) -Description 'FwAvaloniaPreviewHost (Restore)'

Write-Host "Building Avalonia Preview Host ($Configuration)..." -ForegroundColor Cyan
Invoke-MSBuild -Arguments @(
	$projectPath,
	'/t:Build',
	"/p:Configuration=$Configuration",
	'/p:Platform=x64',
	'/v:minimal',
	'/nologo'
) -Description 'FwAvaloniaPreviewHost (Build)'

if ($moduleProjectPath) {
	Write-Host "Building module '$Module' ($Configuration)..." -ForegroundColor Cyan
	Invoke-MSBuild -Arguments @(
		$moduleProjectPath,
		'/t:Build',
		"/p:Configuration=$Configuration",
		'/p:Platform=x64',
		'/v:minimal',
		'/nologo'
	) -Description "Module $Module (Build)"
}

$exeCandidates = @(
	(Join-Path $repoRoot "Output\$Configuration\FwAvaloniaPreviewHost.exe"),
	(Join-Path $repoRoot "Src\Common\FwAvaloniaPreviewHost\bin\$Configuration\net8.0-windows\FwAvaloniaPreviewHost.exe")
)

$exePath = $exeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $exePath) {
	throw "Preview host exe not found after build. Checked: $($exeCandidates -join '; ')"
}

if ($BuildOnly) {
	Write-Host "[OK] Build complete (BuildOnly)." -ForegroundColor Green
	exit 0
}

Write-Host "Launching: $exePath" -ForegroundColor Cyan
Push-Location $repoRoot
try {
	& $exePath --module $Module --data $Data
}
finally {
	Pop-Location
}
exit $LASTEXITCODE
