[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$NativeChanged = 'false',
    [string]$ManagedTestFilter = 'TestCategory!=LongRunning&TestCategory!=ByHand&TestCategory!=SmokeTest&TestCategory!=DesktopRequired'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$nativeChangedBool = $NativeChanged -eq 'true'

$nativeBuildProcess = $null
$nativeBuildLogPath = Join-Path $repoRoot "Output/$Configuration/native-test-build.log"
$nativeBuildErrorPath = "$nativeBuildLogPath.stderr"

Push-Location $repoRoot
try {
    if ($nativeChangedBool) {
        Write-Host 'Native-related changes detected; building native test executables in parallel.' -ForegroundColor Cyan

        New-Item -Path (Split-Path $nativeBuildLogPath -Parent) -ItemType Directory -Force | Out-Null
        if (Test-Path $nativeBuildLogPath) { Remove-Item $nativeBuildLogPath -Force -ErrorAction SilentlyContinue }
        if (Test-Path $nativeBuildErrorPath) { Remove-Item $nativeBuildErrorPath -Force -ErrorAction SilentlyContinue }

        $nativeBuildScript = Join-Path $repoRoot 'Build\Agent\Build-NativeTestExecutables.ps1'
        $nativeBuildProcess = Start-Process -FilePath 'powershell.exe' `
            -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $nativeBuildScript, '-Configuration', $Configuration) `
            -WorkingDirectory $repoRoot `
            -NoNewWindow `
            -PassThru `
            -RedirectStandardOutput $nativeBuildLogPath `
            -RedirectStandardError $nativeBuildErrorPath
    }
    else {
        Write-Host 'No native-related changes detected; running managed tests only.' -ForegroundColor Cyan
    }

    .\test.ps1 -Configuration $Configuration -NoBuild -TestFilter $ManagedTestFilter
    $managedExitCode = $LASTEXITCODE

    $nativeBuildExitCode = 0
    if ($nativeBuildProcess) {
        $nativeBuildProcess.WaitForExit()
        $nativeBuildExitCode = $nativeBuildProcess.ExitCode
        if ($nativeBuildExitCode -ne 0) {
            Write-Host "[ERROR] Native test executable build failed with exit code $nativeBuildExitCode." -ForegroundColor Red
            if (Test-Path $nativeBuildLogPath) {
                Write-Host '--- Native build output (last 60 lines) ---' -ForegroundColor Yellow
                Get-Content -Path $nativeBuildLogPath -Tail 60 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
                Write-Host '--- end output ---' -ForegroundColor Yellow
            }
            if (Test-Path $nativeBuildErrorPath) {
                Write-Host '--- Native build stderr (last 60 lines) ---' -ForegroundColor Yellow
                Get-Content -Path $nativeBuildErrorPath -Tail 60 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_ }
                Write-Host '--- end stderr ---' -ForegroundColor Yellow
            }
        }
    }

    if ($managedExitCode -ne 0) {
        exit $managedExitCode
    }

    if ($nativeBuildExitCode -ne 0) {
        exit $nativeBuildExitCode
    }
}
finally {
    Pop-Location
}
