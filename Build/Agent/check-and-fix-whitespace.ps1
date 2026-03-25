#!/usr/bin/env pwsh
$ErrorActionPreference = 'Continue'

if (Test-Path -LiteralPath 'check-results.log') {
	Remove-Item -LiteralPath 'check-results.log' -Force
}

& "$PSScriptRoot/check-whitespace.ps1"
$ec = $LASTEXITCODE

if ($ec -eq 0 -or (Test-Path -LiteralPath 'check-results.log')) {
	& "$PSScriptRoot/fix-whitespace.ps1"
}
else {
	Write-Error 'Whitespace checker failed before producing results; skipping fixer.'
}

exit $ec
