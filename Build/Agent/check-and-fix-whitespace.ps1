#!/usr/bin/env pwsh
$ErrorActionPreference = 'Continue'

$resultsLogPath = 'check-results.log'
$checkSucceededCode = 0
$checkFoundIssuesCode = 2

if (Test-Path -LiteralPath $resultsLogPath) {
	Remove-Item -LiteralPath $resultsLogPath -Force
}

& "$PSScriptRoot/check-whitespace.ps1"
$ec = $LASTEXITCODE

if ($ec -eq $checkSucceededCode -or $ec -eq $checkFoundIssuesCode) {
	& "$PSScriptRoot/fix-whitespace.ps1"
}
else {
	Write-Error 'Whitespace checker failed unexpectedly; skipping fixer.'
}

exit $ec
