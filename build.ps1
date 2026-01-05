<#
.SYNOPSIS
    Builds the FieldWorks repository using the MSBuild Traversal SDK.

.DESCRIPTION
    This script orchestrates the build process for FieldWorks. It handles:
    1. Auto-detecting worktrees and respawning inside Docker containers.
    2. Initializing the Visual Studio Developer Environment (if needed).
    3. Bootstrapping build tasks (FwBuildTasks).
    4. Restoring NuGet packages.
    5. Building the solution via FieldWorks.proj using MSBuild Traversal.

    When running in a worktree (e.g., fw-worktrees/agent-1), the script will
    automatically detect if a corresponding Docker container (fw-agent-1) is
    running and respawn the build inside the container for proper COM/registry
    isolation. Use -NoDocker to bypass this behavior.

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
    If set, includes optional utility applications (e.g. MigrateSqlDbs, LCMBrowser) in the build. Default is false.

.PARAMETER Verbosity
    Specifies the amount of information to display in the build log.
    Values: q[uiet], m[inimal], n[ormal], d[etailed], diag[nostic].
    Default is 'minimal'.

.PARAMETER NodeReuse
    Enables or disables MSBuild node reuse (/nr). Default is true.

.PARAMETER MsBuildArgs
    Additional arguments to pass directly to MSBuild.

.PARAMETER LogFile
    Path to a file where the build output should be logged.

.PARAMETER TailLines
    If specified, only displays the last N lines of output after the build completes.
    Useful for CI/agent scenarios where you want to see recent output without piping.
    The full output is still written to LogFile if specified.

.PARAMETER NoDocker
    If set, bypasses automatic Docker container detection and runs locally.
    Use this when you want to build directly on the host even in a worktree.

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
    .\build.ps1 -NoDocker
    Builds locally even when in a worktree with an available container.

.NOTES
    FieldWorks is x64-only. The x86 platform is no longer supported.

    Worktree builds automatically use Docker containers when available for
    proper COM/registry isolation. The container must be started first using
    scripts/spin-up-agents.ps1.
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
    [string]$Verbosity = "minimal",
    [bool]$NodeReuse = $true,
    [string[]]$MsBuildArgs = @(),
    [string]$LogFile,
    [int]$TailLines,
    [switch]$NoDocker,
    [switch]$SkipRestore,
    [switch]$SkipNative
)

$ErrorActionPreference = "Stop"

# =============================================================================
# Import Shared Module
# =============================================================================

$helpersPath = Join-Path $PSScriptRoot "Build/Agent/FwBuildHelpers.psm1"
if (-not (Test-Path $helpersPath)) {
    Write-Host "[ERROR] FwBuildHelpers.psm1 not found at $helpersPath" -ForegroundColor Red
    exit 1
}
Import-Module $helpersPath -Force

$insideContainer = Test-InsideContainer
if (-not $insideContainer) {
    Stop-ConflictingProcesses -CrossContainers -IncludeOmniSharp
}

$fwTasksSourcePath = if ($insideContainer) {
    "C:\Temp\BuildTools\FwBuildTasks\$Configuration\FwBuildTasks.dll"
} else {
    Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"
}
$fwTasksDropPath = Join-Path $PSScriptRoot "BuildTools/FwBuildTasks/$Configuration/FwBuildTasks.dll"

# =============================================================================
# Docker Container Auto-Detection for Worktrees
# =============================================================================

if (-not $NoDocker -and -not $insideContainer) {
    $agentNum = Get-WorktreeAgentNumber
    if ($null -ne $agentNum) {
        $containerName = "fw-agent-$agentNum"
        if (Test-DockerContainerRunning -ContainerName $containerName) {
            # Build arguments for container execution
            $containerArgs = New-ContainerArgumentString -Parameters @{
                Configuration = $Configuration
                Serial = $Serial.IsPresent
                BuildTests = $BuildTests.IsPresent
                RunTests = $RunTests.IsPresent
                TestFilter = $TestFilter
                BuildAdditionalApps = $BuildAdditionalApps.IsPresent
                Verbosity = $Verbosity
                NodeReuse = $NodeReuse
                LogFile = $LogFile
                TailLines = $TailLines
                SkipRestore = $SkipRestore.IsPresent
                SkipNative = $SkipNative.IsPresent
            } -Defaults @{
                Configuration = 'Debug'
                Verbosity = 'minimal'
                NodeReuse = $true
                TailLines = 0
            }

            # Handle MsBuildArgs array separately if present
            if ($MsBuildArgs.Count -gt 0) {
                $quotedArgs = $MsBuildArgs | ForEach-Object { "'$($_ -replace "'", "''")'" }
                $containerArgs += " -MsBuildArgs @($($quotedArgs -join ','))"
            }

            Invoke-InContainer -ScriptName "build.ps1" -Arguments $containerArgs -AgentNumber $agentNum
            exit 0
        }
        else {
            Write-Host "[WARN] Worktree agent-$agentNum detected but container '$containerName' is not running" -ForegroundColor Yellow
            Write-Host "   Building locally (use 'scripts/spin-up-agents.ps1' to start containers)" -ForegroundColor Yellow
            Write-Host ""
        }
    }
}

# =============================================================================
# Environment Setup
# =============================================================================

$allSessionsKill = Test-InsideContainer
$crossContainerKill = -not $allSessionsKill
$cleanupArgs = @{
    AllSessions = $allSessionsKill
    CrossContainers = $crossContainerKill
    IncludeOmniSharp = $true
}

$testExitCode = 0

try {
    Invoke-WithFileLockRetry -Context "FieldWorks build" -AllSessions:$allSessionsKill -CrossContainers:$crossContainerKill -IncludeOmniSharp -Action {
        # Initialize Visual Studio Developer environment
        Initialize-VsDevEnvironment
        Test-CvtresCompatibility

        # Set architecture environment variable (x64-only)
        $env:arch = 'x64'
        Write-Host "Set arch environment variable to: $env:arch" -ForegroundColor Green

        # Stop conflicting processes before the build
        Stop-ConflictingProcesses @cleanupArgs

        if ($insideContainer) {
            $fwTasksOut = 'C:\Temp\BuildTools\FwBuildTasks\Debug'
            if (Test-Path $fwTasksOut) {
                Write-Host "Cleaning stale FwBuildTasks output in container..." -ForegroundColor Yellow
                Remove-Item -Path $fwTasksOut -Recurse -Force -ErrorAction SilentlyContinue
            }
        } else {
            # Clean stale per-project obj/ folders (Host only - containers use C:\Temp\Obj)
            Remove-StaleObjFolders -RepoRoot $PSScriptRoot
        }

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
        if ($insideContainer) {
            # Disable shared compilation in containers to prevent file locks on bind mounts
            $finalMsBuildArgs += "/p:UseSharedCompilation=false"
        }
        if ($SkipNative) {
            $finalMsBuildArgs += "/p:SkipNative=true"
        }
        $finalMsBuildArgs += "/p:CL_MPCount=$mpCount"

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
        Write-Host "Configuration: $Configuration | Platform: $Platform | Parallel: $(-not $Serial) | Tests: $($BuildTests -or $RunTests)" -ForegroundColor Cyan

        if ($BuildAdditionalApps) {
            Write-Host "Including optional FieldWorks executables" -ForegroundColor Yellow
        }

        # Bootstrap: Build FwBuildTasks first (required by SetupInclude.targets)
        Invoke-MSBuild `
            -Arguments @('Build/Src/FwBuildTasks/FwBuildTasks.csproj', '/t:Restore;Build', "/p:Configuration=$Configuration", "/p:Platform=$Platform", "/p:SkipFwBuildTasksAssemblyCheck=true", "/p:SkipFwBuildTasksUsingTask=true", "/p:SkipGenerateFwTargets=true", "/p:SkipSetupTargets=true", "/v:quiet", "/nologo") `
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

        # Build using traversal project
        Invoke-MSBuild `
            -Arguments (@('FieldWorks.proj') + $finalMsBuildArgs) `
            -Description "FieldWorks Solution" `
            -LogPath $LogFile `
            -TailLines $TailLines

        Write-Host ""
        Write-Host "[OK] Build complete!" -ForegroundColor Green
        Write-Host "Output: Output\$Configuration" -ForegroundColor Cyan
    }

    # =============================================================================
    # Test Execution (Optional)
    # =============================================================================

    if ($RunTests) {
        Write-Host ""
        Write-Host "Running tests..." -ForegroundColor Cyan

        $testArgs = @("-Configuration", $Configuration, "-NoBuild", "-NoDocker")
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
