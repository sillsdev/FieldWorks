<#
.SYNOPSIS
    Build and/or run native C++ test executables.

.DESCRIPTION
    Script for building and running native C++ tests.
    Supports both MSBuild (new vcxproj) and nmake (legacy makefile) builds.
    Auto-approvable by Copilot agents.

.PARAMETER Action
    What to do: Build, Run, or BuildAndRun (default).

.PARAMETER TestProject
    Which test: TestGeneric (default) or TestViews.

.PARAMETER Configuration
    Build configuration: Debug (default) or Release.

.PARAMETER BuildSystem
    Build system to use: MSBuild (default, uses vcxproj) or NMake (legacy).

.PARAMETER WorktreePath
    Path to the worktree root. Defaults to current directory.

.EXAMPLE
    .\Invoke-CppTest.ps1 -TestProject TestGeneric
    Build and run TestGeneric using MSBuild.

.EXAMPLE
    .\Invoke-CppTest.ps1 -Action Run -TestProject TestViews
    Run TestViews without rebuilding.

.EXAMPLE
    .\Invoke-CppTest.ps1 -BuildSystem NMake -TestProject TestGeneric
    Build TestGeneric using legacy nmake (requires VsDevCmd).
#>
[CmdletBinding()]
param(
    [ValidateSet('Build', 'Run', 'BuildAndRun')]
    [string]$Action = 'BuildAndRun',

    [ValidateSet('TestGeneric', 'TestViews')]
    [string]$TestProject = 'TestGeneric',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateSet('MSBuild', 'NMake')]
    [string]$BuildSystem = 'MSBuild',

    [string]$WorktreePath,

    [int]$TimeoutSeconds = 300,

    [string[]]$TestArguments,

    [string]$LogPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:LastLocalOutDir = $null

# =============================================================================
# Import Shared Module
# =============================================================================

$helpersPath = Join-Path $PSScriptRoot "../../Build/Agent/FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
    Write-Host "[ERROR] FwBuildHelpers.psm1 not found at $helpersPath" -ForegroundColor Red
    exit 1
}
Import-Module $helpersPath -Force

# =============================================================================
# Environment Setup
# =============================================================================

# Initialize VS environment (needed for NMake and MSBuild)
Initialize-VsDevEnvironment

# Suppress assertion dialog boxes (DebugProcs.dll checks this env var)
# This prevents tests from blocking on MessageBox popups
$env:AssertUiEnabled = 'false'

# Suppress Windows Error Reporting and crash dialogs
# SEM_FAILCRITICALERRORS = 0x0001
# SEM_NOGPFAULTERRORBOX = 0x0002
# SEM_NOOPENFILEERRORBOX = 0x8000
$SetErrorModeSignature = @'
[DllImport("kernel32.dll")]
public static extern uint SetErrorMode(uint uMode);
'@
$Kernel32 = Add-Type -MemberDefinition $SetErrorModeSignature -Name 'Kernel32' -Namespace 'Win32' -PassThru
$oldMode = $Kernel32::SetErrorMode(0x8003)

# Resolve worktree path
if (-not $WorktreePath) {
    $WorktreePath = (Get-Location).Path
}
$WorktreePath = (Resolve-Path $WorktreePath).Path
$sourceWorktreePath = $WorktreePath

# Track both the original mount path and the active path (local clone when enabled)
$activeWorktreePath = $WorktreePath

# Project configuration
$projectConfig = @{
    TestGeneric = @{
        VcxprojPath = 'Src\Generic\Test\TestGeneric.vcxproj'
        MakefilePath = 'Src\Generic\Test'
        MakefileName = 'testGenericLib.mak'
        ExeName = 'testGenericLib.exe'
    }
    TestViews = @{
        VcxprojPath = 'Src\views\Test\TestViews.vcxproj'
        MakefilePath = 'Src\views\Test'
        MakefileName = 'testViews.mak'
        ExeName = 'TestViews.exe'
    }
}

$config = $projectConfig[$TestProject]
$outputDir = Join-Path $WorktreePath "Output\$Configuration"
$exePath = Join-Path $outputDir $config.ExeName

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "C++ Test: $TestProject" -ForegroundColor Cyan
Write-Host "Action: $Action" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Build System: $BuildSystem" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

function Get-MsBuildExecutable {
    $buildToolsMsbuild = 'C:\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
    if (Test-Path $buildToolsMsbuild) { return $buildToolsMsbuild }
    return 'msbuild'
}

function Build-FwBuildTasks {
    Write-Host "[INFO] Building FwBuildTasks helper assembly..." -ForegroundColor Yellow
    $tasksProj = Join-Path $WorktreePath 'Build\Src\FwBuildTasks\FwBuildTasks.csproj'
    $msbuild = Get-MsBuildExecutable
    $args = @(
        '/restore',
        "$tasksProj",
        '/p:Configuration={0}' -f $Configuration,
        '/p:Platform=x64',
        '/nologo'
    )
    $cmd = "cd /d `"$WorktreePath`" && $msbuild $($args -join ' ')"
    cmd /c $cmd
    if ($LASTEXITCODE -ne 0) { throw "FwBuildTasks build failed with exit code $LASTEXITCODE" }
}

function Build-NativeArtifacts {
    Write-Host "[INFO] Building native artifacts (NativeBuild.csproj)..." -ForegroundColor Yellow
    Build-FwBuildTasks
    $nativeProj = Join-Path $WorktreePath 'Build\Src\NativeBuild\NativeBuild.csproj'
    $msbuild = Get-MsBuildExecutable
    $args = @(
        "$nativeProj",
        '/p:Configuration={0}' -f $Configuration,
        '/p:Platform=x64',
        '/nologo',
        '/m'
    )
    $cmd = "cd /d `"$WorktreePath`" && $msbuild $($args -join ' ')"
    cmd /c $cmd
    if ($LASTEXITCODE -ne 0) { throw "Native build failed with exit code $LASTEXITCODE" }
}

function Build-ViewsInterfacesArtifacts {
    Write-Host "[INFO] Generating ViewsInterfaces artifacts..." -ForegroundColor Yellow
    $viewsProj = Join-Path $WorktreePath 'Src\Common\ViewsInterfaces\ViewsInterfaces.csproj'
    $msbuild = Get-MsBuildExecutable
    $args = @(
        '/restore',
        "$viewsProj",
        '/p:Configuration={0}' -f $Configuration,
        '/p:Platform=x64',
        '/nologo',
        '/v:minimal'
    )
    $cmd = "cd /d `"$WorktreePath`" && $msbuild $($args -join ' ')"
    cmd /c $cmd
    if ($LASTEXITCODE -ne 0) { throw "ViewsInterfaces build failed with exit code $LASTEXITCODE" }
}

function Ensure-TestViewsPrerequisites {
    if ($TestProject -ne 'TestViews') { return }
    $fwKernelHeader = Join-Path $WorktreePath "Output\$Configuration\Common\FwKernelTlb.h"
    $viewsObj = Join-Path $WorktreePath "Obj\$Configuration\Views\autopch\VwRootBox.obj"
    if ((Test-Path $fwKernelHeader) -and (Test-Path $viewsObj)) { return }

    Write-Host "[INFO] Missing native artifacts or generated headers required for TestViews." -ForegroundColor Yellow
    Build-NativeArtifacts
    Build-ViewsInterfacesArtifacts
}

function Invoke-Build {
    # Ensure native runtime dependencies (DebugProcs, ICU DLLs, etc.) exist before building the test exe
    $needsNative = @(
        (Test-Path (Join-Path $outputDir 'DebugProcs.dll')),
        (Test-Path (Join-Path $outputDir 'icuin70.dll')),
        (Test-Path (Join-Path $outputDir 'icuuc70.dll'))
    ) -notcontains $true
    if ($needsNative) {
        Write-Host "[INFO] Native runtime artifacts missing for $TestProject; building NativeBuild.csproj..." -ForegroundColor Yellow
        Build-NativeArtifacts
    }

    if ($BuildSystem -eq 'MSBuild') {
        $vcxproj = Join-Path $WorktreePath $config.VcxprojPath

        Write-Host "WorktreePath: $WorktreePath" -ForegroundColor Gray
        Write-Host "vcxproj path: $vcxproj" -ForegroundColor Gray
        if (-not (Test-Path $vcxproj)) {
            Write-Host "[ERROR] vcxproj not found at resolved path" -ForegroundColor Red
        }

        if (-not (Test-Path $vcxproj)) {
            throw "vcxproj not found: $vcxproj. Has the project been converted from Makefile?"
        }

        Ensure-TestViewsPrerequisites

        Write-Host "`nBuilding with MSBuild..." -ForegroundColor Yellow

        # Force x64 platform to avoid Win32 default when building from host
        $env:Platform = 'x64'

        # Prefer the BuildTools MSBuild inside the container to match VCINSTALLDIR
        $msbuild = "msbuild"
        $buildToolsMsbuild = 'C:\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
        if (Test-Path $buildToolsMsbuild) {
            $msbuild = $buildToolsMsbuild
        }

        $msbuildOutArgs = @()
        $localOutDir = $null

        $msbuildArgs = @(
            $config.VcxprojPath,
            '/p:Configuration={0}' -f $Configuration,
            '/p:Platform=x64',
            '/p:LinkIncremental=false',
            '/v:minimal',
            '/nologo'
        ) + $msbuildOutArgs
        $msbuildCmd = "$msbuild $($msbuildArgs -join ' ')"
        Write-Host "(cd $WorktreePath) $msbuildCmd" -ForegroundColor Gray

        $cmdLine = "cd /d `"$WorktreePath`" && $msbuildCmd"
        cmd /c $cmdLine
        if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }

        if ($localOutDir) {
            $script:LastLocalOutDir = $localOutDir
            # Ensure output directory exists in the local clone
            if (-not (Test-Path $outputDir)) {
                New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
            }

            $artifactNames = @(
                $config.ExeName,
                ($config.ExeName -replace '\.exe$', '.pdb'),
                ($config.ExeName -replace '\.exe$', '.lib'),
                ($config.ExeName -replace '\.exe$', '.exp')
            )

            foreach ($name in $artifactNames) {
                $src = Join-Path $localOutDir $name
                if (Test-Path $src) {
                    try {
                        Copy-Item -Path $src -Destination (Join-Path $outputDir $name) -Force
                    }
                    catch {
                        Write-Host "[WARN] Could not copy $name to Output (in use?): $_" -ForegroundColor Yellow
                    }
                }
            }

            # Also copy ICU DLLs from lib/x64 if they exist (required for runtime)
            $localLibX64 = Join-Path $localOutDir "lib\x64"
            if (Test-Path $localLibX64) {
                $destLibX64 = Join-Path $outputDir "lib\x64"
                if (-not (Test-Path $destLibX64)) {
                    New-Item -ItemType Directory -Force -Path $destLibX64 | Out-Null
                }
                try {
                    Copy-Item -Path (Join-Path $localLibX64 "icu*.dll") -Destination $destLibX64 -Force
                }
                catch {
                    Write-Host "[WARN] Could not copy ICU DLLs to Output (in use?): $_" -ForegroundColor Yellow
                }
            }
        }
    }
    else {
        # NMake build (legacy)
        $makeDir = Join-Path $WorktreePath $config.MakefilePath
        $makefile = $config.MakefileName
        $buildType = if ($Configuration -eq 'Debug') { 'd' } else { 'r' }

        Write-Host "`nBuilding with NMake (legacy)..." -ForegroundColor Yellow

        # VsDevCmd is already initialized by Initialize-VsDevEnvironment

        $cmd = "cd /d `"$makeDir`" && nmake /nologo BUILD_CONFIG=$Configuration BUILD_TYPE=$buildType BUILD_ROOT=$WorktreePath\ BUILD_ARCH=x64 /f $makefile"
        Write-Host "cmd /c `"$cmd`"" -ForegroundColor Gray
        cmd /c $cmd
        if ($LASTEXITCODE -ne 0) { throw "NMake failed with exit code $LASTEXITCODE" }
    }

    Write-Host "`nBuild completed successfully." -ForegroundColor Green
}

function Invoke-Run {
    Write-Host "`nRunning $($config.ExeName)..." -ForegroundColor Yellow

    # Add Debug CRT to PATH if running in Debug configuration (required for VCRUNTIME140D.dll)
    if ($Configuration -eq 'Debug') {
        $debugCrt = Get-ChildItem -Path "C:\BuildTools\VC\Redist\MSVC" -Filter "Microsoft.VC*.DebugCRT" -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*\x64\*" -and $_.FullName -like "*\debug_nonredist\*" } |
            Select-Object -First 1 -ExpandProperty FullName

        if ($debugCrt) {
            Write-Host "Adding Debug CRT to PATH: $debugCrt" -ForegroundColor Gray
            $env:PATH = "$debugCrt;$env:PATH"
        }
    }

    # Ensure ICU DLLs are in place
    $icuSource = Join-Path $outputDir "lib\x64"
    $icuDlls = Get-ChildItem -Path $icuSource -Filter "icu*70.dll" -ErrorAction SilentlyContinue
    if ($icuDlls) {
        foreach ($dll in $icuDlls) {
            $dest = Join-Path $outputDir $dll.Name
            if (-not (Test-Path $dest)) {
                Write-Host "Copying $($dll.Name) to output directory..." -ForegroundColor Gray
                Copy-Item $dll.FullName $dest
            }
        }
    }

    if (-not (Test-Path $exePath)) {
        throw "Test executable not found: $exePath. Run with -Action Build first."
    }

    if (-not $LogPath) {
        $LogPath = Join-Path $outputDir "$($config.ExeName).log"
    }

    $errorLogPath = "$LogPath.stderr"

    # Ensure native tests resolve paths consistently in containers/CI
    $distFiles = Join-Path $activeWorktreePath 'DistFiles'
    $sourceDistFiles = if ($sourceWorktreePath -and (Test-Path (Join-Path $sourceWorktreePath 'DistFiles'))) { Join-Path $sourceWorktreePath 'DistFiles' } else { $null }

    # Backfill DistFiles into the active clone when missing critical assets
    $requiredAssets = @(
        @{ Rel = 'XceedZip.dll' },
        @{ Rel = 'Templates\NewLangProj.fwdata' }
    )

    if (-not (Test-Path $distFiles) -and $sourceDistFiles) {
        Write-Host "[INFO] DistFiles missing in active worktree; copying from source" -ForegroundColor Yellow
        Copy-Item -Path $sourceDistFiles -Destination $activeWorktreePath -Recurse -Force
    }

    foreach ($asset in $requiredAssets) {
        $target = Join-Path $distFiles $asset.Rel
        if (-not (Test-Path $target)) {
            if ($sourceDistFiles) {
                $sourceAsset = Join-Path $sourceDistFiles $asset.Rel
                if (Test-Path $sourceAsset) {
                    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
                    Copy-Item $sourceAsset $target -Force
                }
            }
        }
    }

    if (-not (Test-Path $distFiles)) {
        throw "DistFiles not found in active worktree ($activeWorktreePath) and no source copy available."
    }

    $env:FW_ROOT_CODE_DIR = $distFiles
    $icuDir = Join-Path $distFiles 'Icu70'
    if (Test-Path $icuDir) {
        $env:FW_ICU_DATA_DIR = $icuDir
        $env:ICU_DATA = $icuDir
    }
    Write-Host "FW_ROOT_CODE_DIR=$($env:FW_ROOT_CODE_DIR)" -ForegroundColor Gray
    if ($env:FW_ICU_DATA_DIR) { Write-Host "FW_ICU_DATA_DIR=$($env:FW_ICU_DATA_DIR)" -ForegroundColor Gray }
    $zipPath = Join-Path $distFiles 'XceedZip.dll'
    Write-Host "XceedZip present: $(Test-Path $zipPath)" -ForegroundColor Gray

    $runExePath = $exePath
    if ($null -ne $script:LastLocalOutDir) {
        $potentialPath = Join-Path $script:LastLocalOutDir $config.ExeName
        if (Test-Path $potentialPath) {
            $runExePath = $potentialPath
        }
    }

    if (Test-Path $LogPath) {
        Remove-Item $LogPath -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $errorLogPath) {
        Remove-Item $errorLogPath -Force -ErrorAction SilentlyContinue
    }

    # Ensure no stale test process is holding the exe open
    $exeProcessName = [System.IO.Path]::GetFileNameWithoutExtension($config.ExeName)
    try {
        Get-Process -Name $exeProcessName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    } catch {}

    # Stage required runtime assets into the output directory for reliable lookup
    $assetsToStage = @(
        @{ Source = Join-Path $distFiles 'XceedZip.dll'; Destination = Join-Path $outputDir 'XceedZip.dll' },
        @{ Source = Join-Path $distFiles 'Templates\NewLangProj.fwdata'; Destination = Join-Path $outputDir 'Templates\NewLangProj.fwdata' }
    )
    foreach ($asset in $assetsToStage) {
        if (Test-Path $asset.Source) {
            New-Item -ItemType Directory -Force -Path (Split-Path $asset.Destination) | Out-Null
            Copy-Item $asset.Source $asset.Destination -Force
        }
    }

    $argumentList = @()
    if ($TestArguments) {
        $argumentList += $TestArguments
    }

    Write-Host "Running: $runExePath $($argumentList -join ' ')" -ForegroundColor Gray
    Write-Host "Logging to: $LogPath" -ForegroundColor Gray

    $startInfo = @{
        FilePath = $runExePath
        WorkingDirectory = $outputDir
        RedirectStandardOutput = $LogPath
        RedirectStandardError = $errorLogPath
        NoNewWindow = $true
        PassThru = $true
    }

    if ($argumentList.Count -gt 0) {
        $startInfo.ArgumentList = $argumentList
    }

    $process = Start-Process @startInfo
    $timedOut = $false
    try {
        Wait-Process -Id $process.Id -Timeout $TimeoutSeconds -ErrorAction Stop
    }
    catch {
        $timedOut = $true
        Write-Host "Test run exceeded timeout (${TimeoutSeconds}s); terminating process..." -ForegroundColor Red
        try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
    }

    $process.Refresh()
    $exitCode = if ($process.HasExited) { $process.ExitCode } else { -1 }

    $logTail = @()
    if (Test-Path $LogPath) {
        if (Test-Path $errorLogPath) {
            Add-Content -Path $LogPath -Value "`n--- stderr ---"
            Get-Content -Path $errorLogPath -ErrorAction SilentlyContinue | Add-Content -Path $LogPath
        }
        $logTail = Get-Content -Path $LogPath -Tail 40 -ErrorAction SilentlyContinue
        Write-Host "--- Test output (last 40 lines) ---" -ForegroundColor Yellow
        $logTail | ForEach-Object { Write-Host $_ }
        Write-Host "--- end output ---" -ForegroundColor Yellow
    }

    Write-Host ""
    if ($timedOut) {
        Write-Host "Tests terminated due to timeout (${TimeoutSeconds}s)." -ForegroundColor Red
    }
    elseif ($exitCode -eq 0) {
        Write-Host "All tests passed! (exit code: 0)" -ForegroundColor Green
    }
    else {
        Write-Host "Tests failed with exit code: $exitCode" -ForegroundColor Red
        if ($exitCode -gt 0) {
            Write-Host "($exitCode test(s) failed)" -ForegroundColor Red
        }
    }

    return $exitCode
}

# Execute requested action
$result = 0
try {
    switch ($Action) {
        'Build' {
            Invoke-Build
        }
        'Run' {
            $result = Invoke-Run
        }
        'BuildAndRun' {
            Invoke-Build
            $result = Invoke-Run
        }
    }
}
catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    exit 1
}

exit $result
