[CmdletBinding()]
param(
    [string]$Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Push-Location $repoRoot
try {
    .\Build\scripts\Invoke-CppTest.ps1 -Action Build -TestProject TestGeneric -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "TestGeneric native build failed with exit code $LASTEXITCODE"
    }

    .\Build\scripts\Invoke-CppTest.ps1 -Action Build -TestProject TestViews -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "TestViews native build failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
