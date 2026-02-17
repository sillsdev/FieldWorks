[CmdletBinding()]
param(
    [string]$DetectJson,
    [string]$Out,
    [string]$Base
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "python not found in PATH"
}

Push-Location $repoRoot
try {
    $cmd = @('.github/plan_copilot_updates.py')
    if ($DetectJson) { $cmd += @('--detect-json', $DetectJson) }
    if ($Base) { $cmd += @('--fallback-base', $Base) }
    if ($Out) { $cmd += @('--out', $Out) }
    & python @cmd
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
