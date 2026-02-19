[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$Platform = 'x64',
    [string]$ManagedTestFilter = 'TestCategory!=LongRunning&TestCategory!=ByHand&TestCategory!=SmokeTest&TestCategory!=DesktopRequired'
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

    .\test.ps1 -Configuration $Configuration -NoBuild -TestFilter $ManagedTestFilter
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
