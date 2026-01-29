<#
.SYNOPSIS
    Builds the FieldWorks repository using the MSBuild Traversal SDK.

.DESCRIPTION
    This script orchestrates the build process for FieldWorks. It handles:
    1. Initializing the Visual Studio Developer Environment (if needed).
    2. Bootstrapping build tasks (FwBuildTasks).
    3. Restoring NuGet packages.
    4. Building the solution via FieldWorks.proj using MSBuild Traversal.

.PARAMETER Configuration
    The build configuration (Debug or Release). Default is Debug.

.PARAMETER Platform
    The target platform. Only x64 is supported. Default is x64.

.PARAMETER Serial
    If set, disables parallel build execution (/m). Default is false (parallel enabled).

.PARAMETER BuildTests
    If set, includes test projects in the build. Default is false.

.PARAMETER RunTests
    If set, runs tests after building. Implies -BuildTests. Uses VSTest via test.ps1.
    Also builds native test prerequisites (Unit++/native test libs).

.PARAMETER TestFilter
    Optional VSTest filter expression (e.g., "TestCategory!=Slow"). Only used with -RunTests.

.PARAMETER BuildAdditionalApps
    If set, includes optional utility applications (e.g. MigrateSqlDbs, LCMBrowser, UnicodeCharEditor) in the build.
    Default is false unless -BuildInstaller is specified, which enables it automatically.

.PARAMETER Verbosity
    Specifies the amount of information to display in the build log.
    Values: q[uiet], m[inimal], n[ormal], d[etailed], diag[nostic].
    Default is 'minimal'.

.PARAMETER TraceCrashes
    If set, enables the dev diagnostics config (FieldWorks.Diagnostics.dev.config) so trace logging
    is written next to the built executable. Useful for crash investigation.

.PARAMETER NodeReuse
    Enables or disables MSBuild node reuse (/nr). Default is true.

.PARAMETER MsBuildArgs
    Additional arguments to pass directly to MSBuild.

.PARAMETER BuildInstaller
    If set, builds the installer via Build/Orchestrator.proj after the main build.
    This automatically enables -BuildAdditionalApps unless explicitly disabled.

.PARAMETER InstallerToolset
    Selects the installer toolset to build (Wix3 or Wix6). Default is Wix3.

.PARAMETER InstallerOnly
    Only used with -BuildInstaller. Skips rebuilding FieldWorks and only builds the WiX installer/bundles,
    reusing existing binaries under Output/<Configuration>. For safety, this requires a build stamp from a
    prior full build in the same configuration.

.PARAMETER SignInstaller
    Only used with -BuildInstaller. Enables local signing when signing tools are available.
    By default, local installer builds capture files to sign later instead of signing.

.PARAMETER ForceInstallerOnly
    Only used with -InstallerOnly. Forces installer-only builds even when the git HEAD or dirty state
    differs from the last full-build stamp, or when there are uncommitted changes outside FLExInstaller/.
    Use only when you are sure the current Output/<Configuration> binaries are still what you want to package.

.PARAMETER UseLocalLcm
    If set, builds liblcm from a local checkout (default: ../liblcm) after the FieldWorks build
    and copies the resulting DLLs into the output directory, overwriting the NuGet package versions.
    Use this to test local liblcm fixes without publishing a NuGet package.

.PARAMETER LocalLcmPath
    Path to the local liblcm repository. Defaults to ../liblcm relative to the FieldWorks repo root.
    Only used when -UseLocalLcm is specified.

.PARAMETER LogFile
    Path to a file where the build output should be logged.

.PARAMETER TailLines
    If specified, only displays the last N lines of output after the build completes.
    Useful for CI/agent scenarios where you want to see recent output without piping.
    The full output is still written to LogFile if specified.

.EXAMPLE
    .\build.ps1
    Builds Debug x64 in parallel with minimal logging.

.EXAMPLE
    .\build.ps1 -Configuration Release -BuildTests
    Builds Release x64 including test projects.

.EXAMPLE
    .\build.ps1 -RunTests
    Builds Debug x64 including test projects and runs all tests.

.EXAMPLE
    .\build.ps1 -Serial -Verbosity detailed
    Builds Debug x64 serially with detailed logging.

.EXAMPLE
    .\build.ps1 -UseLocalLcm
    Builds FieldWorks, then builds liblcm from ../liblcm and copies DLLs into Output.

.NOTES
    FieldWorks is x64-only. The x86 platform is no longer supported.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [ValidateSet('x64')]
    [string]$Platform = "x64",
    [switch]$Serial,
    [switch]$BuildTests,
    [switch]$RunTests,
    [string]$TestFilter,
    [switch]$BuildAdditionalApps,
    [string]$Project = "FieldWorks.proj",
    [string]$Verbosity = "minimal",
    [bool]$NodeReuse = $true,
    [string[]]$MsBuildArgs = @(),
    [string]$LogFile,
    [int]$TailLines,
    [switch]$SkipRestore,
    [switch]$SkipNative,
    [switch]$BuildInstaller,
    [ValidateSet('Wix3', 'Wix6')]
    [string]$InstallerToolset = "Wix3",
    [switch]$InstallerOnly,
    [switch]$ForceInstallerOnly,
    [switch]$SignInstaller,
    [switch]$TraceCrashes,
    [switch]$UseLocalLcm,
    [string]$LocalLcmPath
)

$ErrorActionPreference = "Stop"

if ($Configuration -like "--*") {
    if ($Configuration -eq "--TraceCrashes" -and -not $TraceCrashes) {
        $TraceCrashes = $true
        $Configuration = "Debug"
        Write-Output "[WARN] Detected '--TraceCrashes' passed without PowerShell switch parsing. Using -TraceCrashes and defaulting Configuration to Debug."
    }
    else {
        throw "Invalid Configuration value '$Configuration'. Use -TraceCrashes (single dash) for the trace option."
    }
}

if ($BuildInstaller -and -not $BuildAdditionalApps) {
    $BuildAdditionalApps = $true
    Write-Host "BuildInstaller enabled: including additional apps (use -BuildAdditionalApps:$false to skip)." -ForegroundColor Yellow
}

# For local Release builds, use a stable daily build number so native artifacts can be reused.
# Purpose: fast local rebuilds by preventing version-stamp churn in native inputs.
# CI (GitHub Actions) should continue to provide its own build number.
$isGitHubActions = ($env:GITHUB_ACTIONS -eq 'true')
if ($Configuration -eq 'Release' -and -not $isGitHubActions) {
    $buildNumberOk = $false
    if (-not [string]::IsNullOrWhiteSpace($env:FW_BUILD_NUMBER)) {
        $parsedBuildNumber = 0
        if ([int]::TryParse($env:FW_BUILD_NUMBER, [ref]$parsedBuildNumber) -and $parsedBuildNumber -ge 0 -and $parsedBuildNumber -le 65535) {
            $buildNumberOk = $true
        }
    }

    if (-not $buildNumberOk) {
        $utcNow = (Get-Date).ToUniversalTime()
        $env:FW_BUILD_NUMBER = "{0}{1:000}" -f $utcNow.ToString('yy'), $utcNow.DayOfYear
        Write-Host "Using local numeric build number: $($env:FW_BUILD_NUMBER)" -ForegroundColor Yellow
    }

    if ([string]::IsNullOrWhiteSpace($env:FW_BUILD_LABEL)) {
        $env:FW_BUILD_LABEL = "local_{0}" -f (Get-Date).ToUniversalTime().ToString('yyyyMMdd')
        Write-Host "Using local build label: $($env:FW_BUILD_LABEL)" -ForegroundColor Yellow
    }
}

# ===========ed Module
# =============================================================================

$helpersPath = Join-Path $PSScriptRoot "Build/Agent/FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
    Write-Host "[ERROR] FwBuildHelpers.psm1 not found at $helpersPath" -ForegroundColor Red
    exit 1
}
Import-Module $helpersPath -Force

Stop-ConflictingProcesses -IncludeOmniSharp

$fwTasksSourcePath = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"
$fwTasksDropPath = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"

# =============================================================================
# Environment Setup
# =============================================================================

$cleanupArgs = @{
    IncludeOmniSharp = $true
    RepoRoot = $PSScriptRoot
}

$testExitCode = 0

function Get-RepoStamp {
    $gitHead = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to determine git HEAD (git rev-parse HEAD)."
    }
    $gitHead = ($gitHead | Out-String).Trim()

    $gitStatus = & git status --porcelain
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to determine git dirty state (git status --porcelain)."
    }
    $gitStatusText = ($gitStatus | Out-String)
    $isDirty = -not [string]::IsNullOrWhiteSpace($gitStatusText)

    # For installer-only iteration, we want to allow local edits under FLExInstaller/**
    # while still blocking when *product* inputs have changed (e.g., Src/**).
    $isDirtyOutsideInstaller = $false
    if ($isDirty) {
        $lines = @()
        foreach ($line in ($gitStatusText -split "`r?`n")) {
            $trimmed = $line.TrimEnd()
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                continue
            }
            $lines += $trimmed
        }

        foreach ($statusLine in $lines) {
            if ($statusLine.Length -lt 4) {
                continue
            }

            # Porcelain format: XY<space>PATH (or XY<space>OLD -> NEW)
            $pathPart = $statusLine.Substring(3).Trim()
            if ($pathPart -like "* -> *") {
                $pathPart = ($pathPart.Split(@(" -> "), 2, [System.StringSplitOptions]::None)[1]).Trim()
            }

            if (-not ($pathPart -like "FLExInstaller/*")) {
                $isDirtyOutsideInstaller = $true
                break
            }
        }
    }

    return [pscustomobject]@{
        GitHead = $gitHead
        IsDirty = $isDirty
        IsDirtyOutsideInstaller = $isDirtyOutsideInstaller
    }
}

function Get-BuildStampPath {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$ConfigurationName
    )
    $outputDir = Join-Path $RepoRoot ("Output\\{0}" -f $ConfigurationName)
    return Join-Path $outputDir "BuildStamp.json"
}

try {
    Invoke-WithFileLockRetry -Context "FieldWorks build" -IncludeOmniSharp -Action {
        # Initialize Visual Studio Developer environment
        Initialize-VsDevEnvironment
        Test-CvtresCompatibility

        # Set architecture environment variable (x64-only)
        $env:arch = 'x64'
        Write-Host "Set arch environment variable to: $env:arch" -ForegroundColor Green

        # Stop conflicting processes before the build
        Stop-ConflictingProcesses @cleanupArgs

        $projectPath = $Project
        $rootedProjectPath = Join-Path $PSScriptRoot $Project
        if (-not (Test-Path $projectPath) -and (Test-Path $rootedProjectPath)) {
            $projectPath = $rootedProjectPath
        }
        if (-not (Test-Path $projectPath)) {
            throw "Project path '$Project' was not found. Pass a path relative to the repo root or an absolute path."
        }

        # Clean stale per-project obj/ folders
        Remove-StaleObjFolders -RepoRoot $PSScriptRoot

        # =============================================================================
        # Build Configuration
        # =============================================================================

        # Determine logical core count for CL_MPCount
        if ($env:CL_MPCount) {
            $mpCount = $env:CL_MPCount
        }
        else {
            $mpCount = 8
            if ($env:NUMBER_OF_PROCESSORS) {
                $procCount = [int]$env:NUMBER_OF_PROCESSORS
                if ($procCount -lt 8) { $mpCount = $procCount }
            }
        }

        # Construct MSBuild arguments
        $finalMsBuildArgs = @()

        # Parallelism
        if (-not $Serial) {
            $finalMsBuildArgs += "/m"
        }

        # Verbosity & Logging
        $finalMsBuildArgs += "/v:$Verbosity"
        $finalMsBuildArgs += "/nologo"
        $finalMsBuildArgs += "/consoleloggerparameters:Summary"

        # Node Reuse
        $finalMsBuildArgs += "/nr:$($NodeReuse.ToString().ToLower())"

        # Properties
        $finalMsBuildArgs += "/p:Configuration=$Configuration"
        $finalMsBuildArgs += "/p:Platform=$Platform"
        if ($SkipNative) {
            $finalMsBuildArgs += "/p:SkipNative=true"
        }
        if ($TraceCrashes) {
            $finalMsBuildArgs += "/p:UseDevTraceConfig=true"
        }
        $finalMsBuildArgs += "/p:CL_MPCount=$mpCount"
        if ($env:FW_TRACE_LOG) {
            $finalMsBuildArgs += "/p:FW_TRACE_LOG=`"$($env:FW_TRACE_LOG)`""
        }

        if ($BuildTests -or $RunTests) {
            $finalMsBuildArgs += "/p:BuildTests=true"
            $finalMsBuildArgs += "/p:BuildNativeTests=true"
        }

        if ($BuildAdditionalApps) {
            $finalMsBuildArgs += "/p:BuildAdditionalApps=true"
        }

        # Add user-supplied args
        $finalMsBuildArgs += $MsBuildArgs

        # =============================================================================
        # Build Execution
        # =============================================================================

        Write-Host ""
        Write-Host "Building FieldWorks..." -ForegroundColor Cyan
        Write-Host "Project: $projectPath" -ForegroundColor Cyan
        Write-Host "Configuration: $Configuration | Platform: $Platform | Parallel: $(-not $Serial) | Tests: $($BuildTests -or $RunTests)" -ForegroundColor Cyan

        if ($BuildAdditionalApps) {
            Write-Host "Including optional FieldWorks executables" -ForegroundColor Yellow
        }

        # Bootstrap: Build FwBuildTasks first (required by SetupInclude.targets)
        $fwBuildTasksOutputDir = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/"
        Invoke-MSBuild `
            -Arguments @('Build/Src/FwBuildTasks/FwBuildTasks.csproj', '/t:Restore;Build', "/p:Configuration=$Configuration", "/p:Platform=$Platform", "/p:FwBuildTasksOutputPath=$fwBuildTasksOutputDir", "/p:SkipFwBuildTasksAssemblyCheck=true", "/p:SkipFwBuildTasksUsingTask=true", "/p:SkipGenerateFwTargets=true", "/p:SkipSetupTargets=true", "/v:quiet", "/nologo") `
            -Description 'FwBuildTasks (Bootstrap)'

        if (-not (Test-Path $fwTasksSourcePath)) {
            throw "Failed to build FwBuildTasks. Expected $fwTasksSourcePath to exist."
        }

        if ($fwTasksSourcePath -ne $fwTasksDropPath) {
            $dropDir = Split-Path $fwTasksDropPath -Parent
            if (-not (Test-Path $dropDir)) {
                New-Item -Path $dropDir -ItemType Directory -Force | Out-Null
            }
            Copy-Item -Path $fwTasksSourcePath -Destination $fwTasksDropPath -Force
        }

        # Restore packages
        if (-not $SkipRestore) {
            Invoke-MSBuild `
                -Arguments @('Build/Orchestrator.proj', '/t:RestorePackages', "/p:Configuration=$Configuration", "/p:Platform=$Platform", "/v:quiet", "/nologo") `
                -Description 'RestorePackages'
        } else {
            Write-Host "Skipping package restore (-SkipRestore)" -ForegroundColor Yellow
        }

        if ($InstallerOnly) {
            if (-not $BuildInstaller) {
                throw "-InstallerOnly requires -BuildInstaller."
            }

            $stampPath = Get-BuildStampPath -RepoRoot $PSScriptRoot -ConfigurationName $Configuration
            if (-not (Test-Path $stampPath)) {
                throw "-InstallerOnly requested but no build stamp was found at '$stampPath'. Run a full build once (without -InstallerOnly) to create it."
            }

            $stamp = Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json
            $current = Get-RepoStamp

            $stampConfig = $stamp.Configuration
            $stampPlatform = $stamp.Platform
            if (($stampConfig -ne $Configuration) -or ($stampPlatform -ne $Platform)) {
                throw "-InstallerOnly stamp mismatch: stamp is Configuration='$stampConfig' Platform='$stampPlatform' but this run is Configuration='$Configuration' Platform='$Platform'. Run a full build in this configuration/platform."
            }

            $headChanged = ($stamp.GitHead -ne $current.GitHead)
            if ((-not $ForceInstallerOnly) -and ($headChanged -or $current.IsDirtyOutsideInstaller)) {
                throw "-InstallerOnly refused: product inputs may have changed since the last full build stamp. Run a full build, or use -ForceInstallerOnly if you are sure Output\\$Configuration is still correct."
            }

            Write-Host ""
            Write-Host "Skipping product build (-InstallerOnly). Reusing Output\\$Configuration." -ForegroundColor Yellow
        }
        else {
            # Build using traversal project
            Invoke-MSBuild `
                -Arguments (@($projectPath) + $finalMsBuildArgs) `
                -Description "FieldWorks Solution" `
                -LogPath $LogFile `
                -TailLines $TailLines

            $stampDir = Join-Path $PSScriptRoot ("Output\\{0}" -f $Configuration)
            if (-not (Test-Path $stampDir)) {
                New-Item -Path $stampDir -ItemType Directory -Force | Out-Null
            }

            $repoStamp = Get-RepoStamp
            $stampObject = [pscustomobject]@{
                Configuration = $Configuration
                Platform = $Platform
                GitHead = $repoStamp.GitHead
                IsDirty = $repoStamp.IsDirty
                IsDirtyOutsideInstaller = $repoStamp.IsDirtyOutsideInstaller
                TimestampUtc = (Get-Date).ToUniversalTime().ToString('o')
            }

            $stampPath = Get-BuildStampPath -RepoRoot $PSScriptRoot -ConfigurationName $Configuration
            $stampObject | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $stampPath -Encoding UTF8

            Write-Host ""
            Write-Host "[OK] Build complete!" -ForegroundColor Green
            Write-Host "Output: Output\$Configuration" -ForegroundColor Cyan
        }

        # Copy local LCM assemblies if requested
        if ($UseLocalLcm) {
            Write-Host ""
            Write-Host "Applying local LCM assemblies..." -ForegroundColor Cyan

            $lcmCopyScript = Join-Path $PSScriptRoot "scripts\Agent\Copy-LocalLcm.ps1"
            $lcmArgs = @{
                Configuration = $Configuration
                BuildLcm = $true
                SkipConfirm = $true
            }
            if ($LocalLcmPath) {
                $lcmArgs['LcmRoot'] = $LocalLcmPath
            }

            & $lcmCopyScript @lcmArgs
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to copy local LCM assemblies."
            }
        }

        if ($BuildInstaller) {
            Write-Host ""
            Write-Host "Building Installer..." -ForegroundColor Cyan

            if (-not $isGitHubActions) {
                if ($SignInstaller) {
                    Write-Host "Signing enabled for local installer build." -ForegroundColor Yellow
                    $env:FILESTOSIGNLATER = $null
                }
                else {
                    $defaultSignList = Join-Path $PSScriptRoot "Output\files-to-sign.txt"
                    if ([string]::IsNullOrWhiteSpace($env:FILESTOSIGNLATER)) {
                        $env:FILESTOSIGNLATER = $defaultSignList
                    }
                    Write-Host "Signing disabled for local build; capturing files to $env:FILESTOSIGNLATER" -ForegroundColor Yellow
                }
            }

            $installerCleanArg = "/p:InstallerCleanProductOutputs=false"
            if ($isGitHubActions) {
                $installerCleanArg = "/p:InstallerCleanProductOutputs=true"
            }

            Invoke-MSBuild `
                -Arguments @('Build/Orchestrator.proj', '/t:BuildInstaller', "/p:Configuration=$Configuration", "/p:Platform=$Platform", '/p:config=release', "/p:InstallerToolset=$InstallerToolset", $installerCleanArg) `
                -Description 'Installer Build'

            Write-Host "[OK] Installer build complete!" -ForegroundColor Green
        }
    }

    # =============================================================================
    # Test Execution (Optional)
    # =============================================================================

    if ($RunTests) {
        Write-Host ""
        Write-Host "Running tests..." -ForegroundColor Cyan

        $testArgs = @("-Configuration", $Configuration, "-NoBuild")
        if ($TestFilter) {
            $testArgs += @("-TestFilter", $TestFilter)
        }

        Stop-ConflictingProcesses @cleanupArgs
        & "$PSScriptRoot\test.ps1" @testArgs
        $testExitCode = $LASTEXITCODE
        if ($testExitCode -ne 0) {
            Write-Warning "Some tests failed. Check output above for details."
        }
    }
}
finally {
    # Kill any lingering build processes that might hold file locks
    Stop-ConflictingProcesses @cleanupArgs
}

if ($testExitCode -ne 0) {
    exit $testExitCode
}
