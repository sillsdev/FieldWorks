[CmdletBinding()]
param(
    [string]$Base,
    [string]$Paths
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "python not found in PATH"
}

Push-Location $repoRoot
try {
    $cmd = @('.github/check_copilot_docs.py', '--only-changed', '--fail')
    if ($Base) { $cmd += @('--base', $Base) }
    if ($Paths) { $cmd += @('--paths', $Paths) }
    & python @cmd
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
