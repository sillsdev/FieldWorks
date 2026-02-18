#!/usr/bin/env pwsh
$ErrorActionPreference = 'Continue'

& "$PSScriptRoot/check-whitespace.ps1"
$ec = $LASTEXITCODE

& "$PSScriptRoot/fix-whitespace.ps1"

exit $ec
