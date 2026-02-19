[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$Platform = 'x64'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

Push-Location $repoRoot
try {
    .\build.ps1 -Configuration $Configuration -Platform $Platform -BuildTests
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    .\Build\Agent\Build-NativeTestExecutables.ps1 -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    .\test.ps1 -Configuration $Configuration -Native -NoBuild
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
