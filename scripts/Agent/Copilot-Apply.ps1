[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Plan,
    [string]$Folders
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "python not found in PATH"
}

Push-Location $repoRoot
try {
    $cmd = @('.github/copilot_apply_updates.py', '--plan', $Plan)
    if ($Folders) { $cmd += @('--folders', $Folders) }
    & python @cmd
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
