<#
.SYNOPSIS
    Shared helper functions for FieldWorks build and test scripts.

.DESCRIPTION
    This module provides common functionality for build.ps1 and test.ps1:
    - Docker container detection and execution for worktrees
    - Worktree path detection
    - VS environment initialization
    - Conflicting process cleanup
    - Stale obj folder cleanup
    - MSBuild execution helpers

.NOTES
    Import this module at the start of build.ps1 and test.ps1:
    Import-Module "$PSScriptRoot/Build/Agent/FwBuildHelpers.psm1" -Force
#>

# =============================================================================
# Container Detection Functions
# =============================================================================

function Test-InsideContainer {
    <#
    .SYNOPSIS
        Checks if we're running inside a Docker container.
    #>
    return (Test-Path 'C:\BuildTools') -or ($env:FW_CONTAINER -eq 'true') -or ($env:DOTNET_RUNNING_IN_CONTAINER -eq 'true')
}

function Get-WorktreeAgentNumber {
    <#
    .SYNOPSIS
        Detects if we're in a worktree path and returns the agent number.
    .DESCRIPTION
        Matches paths like "fw-worktrees/agent-N" or "worktrees/agent-N".
    .OUTPUTS
        [int] The agent number, or $null if not in a worktree.
    #>
    $currentPath = (Get-Location).Path
    if ($currentPath -match '[/\\](?:fw-)?worktrees[/\\]agent-(\d+)') {
        return [int]$Matches[1]
    }
    return $null
}

function Test-DockerContainerRunning {
    <#
    .SYNOPSIS
        Checks if a Docker container is running.
    #>
    param([Parameter(Mandatory)][string]$ContainerName)

    $dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerCmd) { return $false }

    try {
        $status = docker inspect --format '{{.State.Running}}' $ContainerName 2>$null
        return $status -eq 'true'
    }
    catch {
        return $false
    }
}

function Get-ContainerWorkDir {
    <#
    .SYNOPSIS
        Gets the container working directory for the current worktree.
    #>
    param([Parameter(Mandatory)][string]$ContainerName)

    $containerWorkDir = docker inspect --format '{{.Config.WorkingDir}}' $ContainerName 2>$null
    if (-not $containerWorkDir) {
        # Fallback: compute the mapped path (C:\fw-mounts\<drive>\path\to\worktree)
        $currentPath = (Get-Location).Path
        $drive = $currentPath.Substring(0, 1).ToUpper()
        $pathWithoutDrive = $currentPath.Substring(3)  # Skip "C:\"
        $containerWorkDir = "C:\fw-mounts\$drive\$pathWithoutDrive"
    }
    return $containerWorkDir
}

# =============================================================================
# Container Execution Functions
# =============================================================================

function Invoke-InContainer {
    <#
    .SYNOPSIS
        Executes a script inside a Docker container with VS environment.
    .DESCRIPTION
        Handles container detection, VS environment setup, and encoded command execution.
        Used by both build.ps1 and test.ps1 for worktree builds.
    .PARAMETER ScriptName
        The script to run (e.g., "build.ps1" or "test.ps1").
    .PARAMETER Arguments
        Arguments to pass to the script (as a single string).
    .PARAMETER AgentNumber
        The agent number (detected from worktree path).
    .PARAMETER CleanIntermediateFiles
        If true, cleans C:\Temp\Obj before execution. Default is true.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$ScriptName,
        [string]$Arguments = '',
        [Parameter(Mandatory)]
        [int]$AgentNumber,
        [bool]$CleanIntermediateFiles = $true
    )

    $containerName = "fw-agent-$AgentNumber"
    $containerWorkDir = Get-ContainerWorkDir -ContainerName $containerName

    Write-Host "[DOCKER] Detected worktree agent-$AgentNumber with running container '$containerName'" -ForegroundColor Cyan
    Write-Host "   Respawning inside Docker container for COM/registry isolation..." -ForegroundColor Gray
    Write-Host "   Container working dir: $containerWorkDir" -ForegroundColor DarkGray
    Write-Host "   Container command: .\$ScriptName $Arguments" -ForegroundColor DarkGray
    Write-Host ""

    # Clean container-local intermediate files to ensure fresh state
    if ($CleanIntermediateFiles) {
        Write-Host "   Cleaning container intermediate files..." -ForegroundColor DarkGray
        # Clean both C:\Temp\Obj (container-local) and any stale per-project obj folders
        docker exec $containerName powershell -NoProfile -Command @"
            if (Test-Path 'C:\Temp\Obj') { Remove-Item -Recurse -Force 'C:\Temp\Obj' -ErrorAction SilentlyContinue }
            `$srcPath = '$containerWorkDir\Src'
            if (Test-Path `$srcPath) {
                Get-ChildItem -Path `$srcPath -Filter 'obj' -Directory -Recurse -ErrorAction SilentlyContinue |
                    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            }
            `$libPath = '$containerWorkDir\Lib'
            if (Test-Path `$libPath) {
                Get-ChildItem -Path `$libPath -Filter 'obj' -Directory -Recurse -ErrorAction SilentlyContinue |
                    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            }
"@ 2>$null
    }

    # Build the command with -NoDocker to prevent recursion
    # Capture exit code inside container and return it explicitly
    $psCmd = "Set-Location '$containerWorkDir'; .\$ScriptName $Arguments -NoDocker; exit `$LASTEXITCODE"
    $encodedCmd = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($psCmd))

    # Execute with VS environment
    & docker exec $containerName cmd /S /C "C:\scripts\VsDevShell.cmd powershell -NoProfile -EncodedCommand $encodedCmd"
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        throw "Container execution failed with exit code $exitCode"
    }

    Write-Host ""
    Write-Host "[OK] Container execution completed successfully" -ForegroundColor Green
}

function New-ContainerArgumentString {
    <#
    .SYNOPSIS
        Builds an argument string for container script execution.
    .DESCRIPTION
        Converts a hashtable of parameters to a command-line argument string.
    .PARAMETER Parameters
        Hashtable of parameter names to values.
    .PARAMETER Defaults
        Hashtable of default values (parameters matching defaults are omitted).
    #>
    param(
        [Parameter(Mandatory)]
        [hashtable]$Parameters,
        [hashtable]$Defaults = @{}
    )

    $argList = @()
    foreach ($key in $Parameters.Keys) {
        $value = $Parameters[$key]
        $default = $Defaults[$key]

        # Skip if value matches default
        if ($null -ne $default -and $value -eq $default) { continue }

        # Skip null/empty values
        if ($null -eq $value -or $value -eq '') { continue }

        # Handle different types
        if ($value -is [switch] -or $value -is [bool]) {
            if ($value) { $argList += "-$key" }
        }
        elseif ($value -is [array]) {
            # Array parameters need special handling
            $quotedItems = $value | ForEach-Object { "'$($_ -replace "'", "''")'" }
            $argList += "-$key @($($quotedItems -join ','))"
        }
        elseif ($value -is [string] -and $value.Contains(' ')) {
            $argList += "-$key '$value'"
        }
        else {
            $argList += "-$key $value"
        }
    }
    return $argList -join ' '
}

# =============================================================================
# Process Management Functions
# =============================================================================

function Stop-ConflictingProcesses {
    <#
    .SYNOPSIS
        Stops processes that could interfere with builds/tests.
    .DESCRIPTION
        Kills msbuild, cl, link, nmake, VBCSCompiler processes in the current session.
        Only affects processes in the current user session, not other users or containers.
    #>
    $conflicts = @("FieldWorks", "msbuild", "cl", "link", "nmake")
    $currentSessionId = (Get-Process -Id $PID).SessionId

    foreach ($name in $conflicts) {
        $processes = Get-Process -Name $name -ErrorAction SilentlyContinue |
            Where-Object { $_.SessionId -eq $currentSessionId }
        if ($processes) {
            $count = @($processes).Count
            Write-Host "Closing $count stale $name process(es)..." -ForegroundColor Yellow
            $processes | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
        }
    }

    # Kill orphaned VBCSCompiler instances (Roslyn compiler server)
    $vbcsProcesses = Get-Process -Name "VBCSCompiler" -ErrorAction SilentlyContinue |
        Where-Object { $_.SessionId -eq $currentSessionId }
    if ($vbcsProcesses) {
        Write-Host "Cleaning up VBCSCompiler process(es)..." -ForegroundColor Yellow
        $vbcsProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

# =============================================================================
# Cleanup Functions
# =============================================================================

function Remove-StaleObjFolders {
    <#
    .SYNOPSIS
        Removes stale per-project obj/ folders from Src/.
    .DESCRIPTION
        Since SDK migration, intermediate output uses centralized Obj/ folder.
        Old per-project obj/ folders cause CS0579 duplicate attribute errors.
    #>
    param([Parameter(Mandatory)][string]$RepoRoot)

    $srcPath = Join-Path $RepoRoot "Src"
    $staleObjFolders = Get-ChildItem -Path $srcPath -Filter "obj" -Directory -Recurse -ErrorAction SilentlyContinue
    if ($staleObjFolders) {
        $folderCount = $staleObjFolders.Count
        Write-Host "Removing stale per-project obj/ folders ($folderCount found)..." -ForegroundColor Yellow
        $staleObjFolders | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "[OK] Stale obj/ folders cleaned" -ForegroundColor Green
    }

    # Check for stale Output/Common/ (pre-configuration-aware build artifacts)
    # After migration, COM artifacts are in Output/$(Configuration)/Common/ instead of Output/Common/
    $staleCommonDir = Join-Path (Join-Path $RepoRoot "Output") "Common"
    if (Test-Path $staleCommonDir) {
        Write-Host "Removing stale Output/Common/ folder (migrated to Output/<Configuration>/Common/)..." -ForegroundColor Yellow
        Remove-Item -Path $staleCommonDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "[OK] Stale Output/Common/ cleaned" -ForegroundColor Green
    }
}

# =============================================================================
# VS Environment Functions
# =============================================================================

function Initialize-VsDevEnvironment {
    <#
    .SYNOPSIS
        Initializes the Visual Studio Developer environment.
    .DESCRIPTION
        Sets up environment variables for native C++ compilation (x64 only).
        Safe to call multiple times - will skip if already initialized.
    #>
    if ($env:OS -ne 'Windows_NT') {
        return
    }

    if ($env:VCINSTALLDIR) {
        Write-Host '[OK] Visual Studio environment already initialized' -ForegroundColor Green
        return
    }

    Write-Host 'Initializing Visual Studio Developer environment...' -ForegroundColor Yellow

    $vswhereCandidates = @()
    if ($env:ProgramFiles) {
        $pfVswhere = Join-Path -Path $env:ProgramFiles -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path $pfVswhere) { $vswhereCandidates += $pfVswhere }
    }
    $programFilesX86 = ${env:ProgramFiles(x86)}
    if ($programFilesX86) {
        $pf86Vswhere = Join-Path -Path $programFilesX86 -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path $pf86Vswhere) { $vswhereCandidates += $pf86Vswhere }
    }

    if (-not $vswhereCandidates) {
        Write-Host ''
        Write-Host '[ERROR] Visual Studio 2017+ not found' -ForegroundColor Red
        Write-Host '   Install from: https://visualstudio.microsoft.com/downloads/' -ForegroundColor Yellow
        throw 'Visual Studio not found'
    }

    $vsInstallPath = & $vswhereCandidates[0] -latest -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -products * -property installationPath
    if (-not $vsInstallPath) {
        Write-Host ''
        Write-Host '[ERROR] Visual Studio found but missing required C++ tools' -ForegroundColor Red
        Write-Host '   Please install the "Desktop development with C++" workload' -ForegroundColor Yellow
        throw 'Visual Studio C++ tools not found'
    }

    $vsDevCmd = Join-Path -Path $vsInstallPath -ChildPath 'Common7\Tools\VsDevCmd.bat'
    if (-not (Test-Path $vsDevCmd)) {
        throw "Unable to locate VsDevCmd.bat under '$vsInstallPath'."
    }

    # x64-only build
    $arch = 'amd64'
    $vsVersion = Split-Path (Split-Path (Split-Path (Split-Path $vsInstallPath))) -Leaf
    Write-Host "   Found Visual Studio $vsVersion at: $vsInstallPath" -ForegroundColor Gray
    Write-Host "   Setting up environment for $arch..." -ForegroundColor Gray

    $cmdArgs = "`"$vsDevCmd`" -no_logo -arch=$arch -host_arch=$arch && set"
    $envOutput = & cmd.exe /c $cmdArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to initialize Visual Studio environment'
    }

    foreach ($line in $envOutput) {
        $parts = $line -split '=', 2
        if ($parts.Length -eq 2 -and $parts[0]) {
            Set-Item -Path "Env:$($parts[0])" -Value $parts[1]
        }
    }

    if (-not $env:VCINSTALLDIR) {
        throw 'Visual Studio C++ environment not configured'
    }

    Write-Host '[OK] Visual Studio environment initialized successfully' -ForegroundColor Green
    Write-Host "   VCINSTALLDIR: $env:VCINSTALLDIR" -ForegroundColor Gray
}

# =============================================================================
# MSBuild Helper Functions
# =============================================================================

function Get-MSBuildPath {
    <#
    .SYNOPSIS
        Gets the path to MSBuild.exe.
    .DESCRIPTION
        Returns the MSBuild command, either from PATH or 'msbuild' as fallback.
    #>
    $msbuildCmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuildCmd) {
        return $msbuildCmd.Source
    }
    return 'msbuild'
}

function Invoke-MSBuild {
    <#
    .SYNOPSIS
        Executes MSBuild with proper error handling.
    .DESCRIPTION
        Runs MSBuild with the specified arguments and handles errors appropriately.
    .PARAMETER Arguments
        Array of arguments to pass to MSBuild.
    .PARAMETER Description
        Human-readable description of the build step.
    .PARAMETER LogPath
        Optional path to write build output to a log file.
    .PARAMETER TailLines
        If specified, only displays the last N lines of output.
    #>
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [Parameter(Mandatory)]
        [string]$Description,
        [string]$LogPath = '',
        [int]$TailLines = 0
    )

    $msbuildCmd = Get-MSBuildPath
    Write-Host "Running $Description..." -ForegroundColor Cyan

    if ($TailLines -gt 0) {
        # Capture all output, optionally log to file, then display tail
        $output = & $msbuildCmd $Arguments 2>&1 | ForEach-Object { $_.ToString() }
        $exitCode = $LASTEXITCODE

        if ($LogPath) {
            $logDir = Split-Path -Parent $LogPath
            if ($logDir -and -not (Test-Path $logDir)) {
                New-Item -Path $logDir -ItemType Directory -Force | Out-Null
            }
            $output | Out-File -FilePath $LogPath -Encoding utf8
        }

        # Display last N lines
        $totalLines = $output.Count
        if ($totalLines -gt $TailLines) {
            Write-Host "... ($($totalLines - $TailLines) lines omitted, showing last $TailLines) ..." -ForegroundColor DarkGray
            $output | Select-Object -Last $TailLines | ForEach-Object { Write-Host $_ }
        }
        else {
            $output | ForEach-Object { Write-Host $_ }
        }

        $LASTEXITCODE = $exitCode
    }
    elseif ($LogPath) {
        $logDir = Split-Path -Parent $LogPath
        if ($logDir -and -not (Test-Path $logDir)) {
            New-Item -Path $logDir -ItemType Directory -Force | Out-Null
        }
        & $msbuildCmd $Arguments | Tee-Object -FilePath $LogPath
    }
    else {
        & $msbuildCmd $Arguments
    }

    if ($LASTEXITCODE -ne 0) {
        $errorMsg = "MSBuild failed during $Description with exit code $LASTEXITCODE"
        if ($LASTEXITCODE -eq -1073741819) {
            $errorMsg += " (0xC0000005 - Access Violation). This indicates a crash in native code during build."
        }
        throw $errorMsg
    }
}

# =============================================================================
# VSTest Helper Functions
# =============================================================================

function Get-VSTestPath {
    <#
    .SYNOPSIS
        Finds vstest.console.exe in PATH or known locations.
    .DESCRIPTION
        First checks PATH, then falls back to known VS installation paths.
    #>

    # Try PATH first (setup scripts add vstest to PATH)
    $vstestFromPath = Get-Command "vstest.console.exe" -ErrorAction SilentlyContinue
    if ($vstestFromPath) {
        return $vstestFromPath.Source
    }

    # Fall back to known installation paths
    $programFilesX86 = ${env:ProgramFiles(x86)}
    if (-not $programFilesX86) { $programFilesX86 = "C:\Program Files (x86)" }

    $vstestCandidates = @(
        # BuildTools (Docker containers)
        "$programFilesX86\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe",
        "C:\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe",
        # TestAgent (sometimes installed separately)
        "$programFilesX86\Microsoft Visual Studio\2022\TestAgent\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe",
        # Full VS installations
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
    )

    foreach ($candidate in $vstestCandidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

# =============================================================================
# Module Exports
# =============================================================================

Export-ModuleMember -Function @(
    # Container detection
    'Test-InsideContainer',
    'Get-WorktreeAgentNumber',
    'Test-DockerContainerRunning',
    'Get-ContainerWorkDir',
    # Container execution
    'Invoke-InContainer',
    'New-ContainerArgumentString',
    # Process management
    'Stop-ConflictingProcesses',
    # Cleanup
    'Remove-StaleObjFolders',
    # VS environment
    'Initialize-VsDevEnvironment',
    # Build helpers
    'Get-MSBuildPath',
    'Invoke-MSBuild',
    # Test helpers
    'Get-VSTestPath'
)
