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
$wrapperRunner = Join-Path $repoRoot "Build/Agent/Run-ManagedCoverageAssessment.ps1"

if (-not (Test-Path -LiteralPath $wrapperRunner)) {
	throw "Managed coverage wrapper not found: $wrapperRunner"
}

Write-Host "[WARN] Running skill-local coverage script; prefer Build/Agent/Run-ManagedCoverageAssessment.ps1 for terminal use." -ForegroundColor Yellow

if ($NoBuild) {
	& $wrapperRunner -Configuration $Configuration -TestFilter $TestFilter -NoBuild -FocusPath $FocusPath
}
else {
	& $wrapperRunner -Configuration $Configuration -TestFilter $TestFilter -FocusPath $FocusPath
}
if ($LASTEXITCODE -ne 0) {
	throw "Managed coverage assessment failed"
}

Write-Host "[OK] Managed coverage assessment flow complete" -ForegroundColor Green
