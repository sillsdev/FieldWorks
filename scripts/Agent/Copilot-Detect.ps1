[CmdletBinding()]
param(
    [string]$Base,
    [string]$Out
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "python not found in PATH"
}

Push-Location $repoRoot
try {
    $cmd = @('.github/detect_copilot_needed.py', '--strict')
    if ($Base) { $cmd += @('--base', $Base) }
    if ($Out) { $cmd += @('--out', $Out) }
    & python @cmd
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
