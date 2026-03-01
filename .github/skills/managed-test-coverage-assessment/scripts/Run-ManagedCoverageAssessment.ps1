#!/usr/bin/env pwsh
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[string]$TestFilter = "FullyQualifiedName~DetailControls",
	[switch]$NoBuild,
	[string]$FocusPath = "Src\\Common\\Controls\\DetailControls\\"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../../../..")
$coverageRunner = Join-Path $repoRoot "Build/Agent/Run-TestCoverage.ps1"
$assessmentRunner = Join-Path $PSScriptRoot "Assess-CoverageGaps.ps1"

if (-not (Test-Path -LiteralPath $coverageRunner)) {
	throw "Coverage runner not found: $coverageRunner"
}
if (-not (Test-Path -LiteralPath $assessmentRunner)) {
	throw "Assessment runner not found: $assessmentRunner"
}

if ($NoBuild) {
	& $coverageRunner -Configuration $Configuration -TestFilter $TestFilter -NoBuild -FocusPath $FocusPath
}
else {
	& $coverageRunner -Configuration $Configuration -TestFilter $TestFilter -FocusPath $FocusPath
}
if ($LASTEXITCODE -ne 0) {
	throw "Coverage collection failed"
}

& $assessmentRunner -Configuration $Configuration -FocusPath $FocusPath
if ($LASTEXITCODE -ne 0) {
	throw "Coverage gap assessment failed"
}

Write-Host "[OK] Managed coverage assessment flow complete" -ForegroundColor Green
