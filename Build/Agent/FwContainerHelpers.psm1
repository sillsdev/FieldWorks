<#
.SYNOPSIS
    Docker container helpers for FieldWorks worktree builds.

.DESCRIPTION
    Provides container detection, path resolution, and script execution
    for worktree builds inside Docker containers.

.NOTES
    Used by FwBuildHelpers.psm1 - do not import directly.
#>

# =============================================================================
# Container Detection Functions
# =============================================================================

function Test-InsideContainer {
    <#
    .SYNOPSIS
        Checks if we're running inside a Docker container.
    .DESCRIPTION
        Uses environment variables set by the container image or docker run.
        FW_CONTAINER is set in Post-Install-Setup.ps1 (baked into image).
        DOTNET_RUNNING_IN_CONTAINER is the standard .NET convention.
    #>
    return ($env:FW_CONTAINER -eq 'true') -or ($env:DOTNET_RUNNING_IN_CONTAINER -eq 'true')
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
        $drive = $currentPath.Substring(0, 1).ToLower()
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
        If true, cleans C:\Temp\Obj before execution. Default is false.
        Since container builds use container-local paths (C:\Temp\Obj) that don't
        overlap with host builds ($(FwRoot)Obj), cleaning is not needed for isolation.
        Only set to true if you want a full rebuild (e.g., after switching branches).
    #>
    param(
        [Parameter(Mandatory)]
        [string]$ScriptName,
        [string]$Arguments = '',
        [Parameter(Mandatory)]
        [int]$AgentNumber,
        [bool]$CleanIntermediateFiles = $false
    )

    $containerName = "fw-agent-$AgentNumber"
    $containerWorkDir = Get-ContainerWorkDir -ContainerName $containerName

    # Capture the current commit on the host; fall back to container git if unavailable
    $hostCommit = ''
    try {
        $hostCommit = git rev-parse HEAD 2>$null
        if ($hostCommit) { $hostCommit = $hostCommit.Trim() }
    }
    catch {
        $hostCommit = ''
    }
    $safeCommit = if ($hostCommit -and ($hostCommit -match '^[0-9a-fA-F]{7,}$')) { $hostCommit } else { '' }

    Write-Host "[DOCKER] Detected worktree agent-$AgentNumber with running container '$containerName'" -ForegroundColor Cyan
    Write-Host "   Respawning inside Docker container for COM/registry isolation..." -ForegroundColor Gray
    Write-Host "   Container working dir: $containerWorkDir" -ForegroundColor DarkGray
    Write-Host "   Container command: .\$ScriptName $Arguments" -ForegroundColor DarkGray
    Write-Host ""

    # NOTE: We do NOT kill host processes here. Container builds use container-local output paths
    # (C:\Temp\Obj, C:\Temp\BuildTools) that don't conflict with host builds or Serena/OmniSharp.
    # This allows simultaneous host builds (main repo) and container builds (worktrees).

    # Kill stale build processes in container - it's a disposable build container, be aggressive!
    # This prevents MSB3026/MSB3027 file lock errors from orphaned dotnet/msbuild processes
    $cleanupCommand = @"
        # Managed build/test processes
        `$processNames = @('dotnet', 'msbuild', 'VBCSCompiler', 'vstest.console', 'testhost', 'FieldWorks')
        # Native C++ build processes
        `$processNames += @('cl', 'link', 'lib', 'nmake', 'cvtres', 'rc', 'midl', 'tracker', 'vctip', 'ml64')
        foreach (`$name in `$processNames) {
            Get-Process -Name `$name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        }
        Start-Sleep -Milliseconds 300
"@

    Write-Host "   Killing stale build processes in container..." -ForegroundColor DarkGray
    docker exec $containerName powershell -NoProfile -Command $cleanupCommand 2>$null

    # Smart clean: detect when incremental builds are safe vs when a full clean is needed
    #
    # Scenarios:
    # 1. Same commit as last build -> No clean, fast incremental (MSBuild handles timestamps)
    # 2. New commit, old commit is ancestor -> No clean (normal commits/pulls, timestamps valid)
    # 3. Old commit not reachable (rebase/reset) -> Clean (history rewritten, timestamps unreliable)
    # 4. Different branch -> Clean (switching context)
    # 5. No marker file -> Clean (first build or marker deleted)
    #
    # We only store the commit hash. MSBuild's timestamp-based incremental build handles
    # the actual file change detection - we just need to know if timestamps are trustworthy.

    Write-Host "   Checking build state..." -ForegroundColor DarkGray
    $cleanResult = docker exec $containerName powershell -NoProfile -Command @"
        `$markerPath = 'C:\Temp\Obj\.fw-build-marker'
        `$repoPath = '$containerWorkDir'

        # Prefer host-provided commit hash; fall back to container git if missing
        `$currentCommit = '$safeCommit'
        if (-not `$currentCommit) {
            Push-Location `$repoPath
            `$currentCommit = git rev-parse HEAD 2>`$null
            Pop-Location
        }
        if (-not `$currentCommit) {
            Write-Output 'clean:git-error'
            return
        }

        # Check if marker exists
        if (-not (Test-Path `$markerPath)) {
            Write-Output 'clean:no-marker'
            return
        }

        `$savedCommit = (Get-Content `$markerPath -Raw -ErrorAction SilentlyContinue).Trim()
        if (-not `$savedCommit -or `$savedCommit.Length -lt 7 -or `$savedCommit -notmatch '^[0-9a-fA-F]+$') {
            Write-Output 'clean:invalid-marker'
            return
        }

        # Same commit - no clean needed
        if (`$savedCommit -eq `$currentCommit) {
            Write-Output 'incremental:same-commit'
            return
        }

        # Different commit - check if old commit is reachable (ancestor of current)
        # If reachable, this is a normal forward progression (commit, pull, merge)
        # and MSBuild timestamps are still valid
        Push-Location `$repoPath
        `$isAncestor = git merge-base --is-ancestor `$savedCommit `$currentCommit 2>`$null
        `$ancestorCheck = `$LASTEXITCODE
        Pop-Location

        if (`$ancestorCheck -eq 0) {
            # Old commit is ancestor - safe for incremental, just update marker
            Write-Output 'incremental:ancestor'
            return
        }

        # Old commit not reachable - history was rewritten (rebase, reset, etc.)
        # Timestamps may not reflect actual changes, need full clean
        Write-Output 'clean:history-rewritten'
"@ 2>$null

    # If docker exec fails or returns nothing, default to clean for safety
    if (-not $cleanResult) {
        $cleanResult = 'clean:marker-error'
    }

    $cleanNeeded = $cleanResult -like 'clean:*'
    $reason = switch -Wildcard ($cleanResult) {
        'clean:no-marker' { 'first build' }
        'clean:history-rewritten' { 'history rewritten (rebase/reset)' }
        'clean:git-error' { 'git state unavailable' }
        'clean:invalid-marker' { 'marker unreadable/blank' }
        'clean:marker-error' { 'marker check failed' }
        'incremental:same-commit' { $null }
        'incremental:ancestor' { $null }
        default { 'unknown state' }
    }

    if ($CleanIntermediateFiles) {
        $cleanNeeded = $true
        $reason = 'explicit request'
    }

    if ($cleanNeeded) {
        Write-Host "   Cleaning container intermediate files ($reason)..." -ForegroundColor DarkGray
        # Clean both C:\Temp\Obj (container-local) and any stale per-project obj folders
        # Also clean temporary .res files (GUID-named) that VC++ creates in Src folders
        docker exec $containerName powershell -NoProfile -Command @"
            if (Test-Path 'C:\Temp\Obj') { Remove-Item -Recurse -Force 'C:\Temp\Obj' -ErrorAction SilentlyContinue }
            `$srcPath = '$containerWorkDir\Src'
            if (Test-Path `$srcPath) {
                Get-ChildItem -Path `$srcPath -Filter 'obj' -Directory -Recurse -ErrorAction SilentlyContinue |
                    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
                # Clean temporary .res files (GUID-named resource files from VC++ builds)
                Get-ChildItem -Path `$srcPath -Filter '{*}.res' -File -Recurse -ErrorAction SilentlyContinue |
                    Remove-Item -Force -ErrorAction SilentlyContinue
            }
            `$libPath = '$containerWorkDir\Lib'
            if (Test-Path `$libPath) {
                Get-ChildItem -Path `$libPath -Filter 'obj' -Directory -Recurse -ErrorAction SilentlyContinue |
                    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            }
"@ 2>$null
    } else {
        Write-Host "   Incremental build ($cleanResult)" -ForegroundColor DarkGray
    }

    # Always update the marker to current commit
    docker exec $containerName powershell -NoProfile -Command @"
        New-Item -ItemType Directory -Path 'C:\Temp\Obj' -Force | Out-Null
        `$repoPath = '$containerWorkDir'

        `$currentCommit = '$safeCommit'
        if (-not `$currentCommit) {
            Push-Location `$repoPath
            `$currentCommit = git rev-parse HEAD 2>`$null
            Pop-Location
        }

        if (`$currentCommit) {
            `$currentCommit | Set-Content 'C:\Temp\Obj\.fw-build-marker' -NoNewline
        }
"@ 2>$null

    # Build the command with -NoDocker to prevent recursion
    # Capture exit code inside container and return it explicitly
    # Container already has NUGET_PACKAGES env var set (via spin-up-agents.ps1) - don't override it
    # NuGet has trouble creating packages in bind-mounted folders due to atomic file operations
    # Suppress progress and information streams to avoid XML serialization noise in docker exec output
    # The 6>&1 redirects Information stream to stdout (prevents CLIXML serialization of Write-Host)
    $psCmd = "`$ProgressPreference='SilentlyContinue'; Set-Location '$containerWorkDir'; .\$ScriptName $Arguments -NoDocker 6>&1; exit `$LASTEXITCODE"
    $encodedCmd = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($psCmd))

    # Execute with VS environment
    try {
        & docker exec $containerName cmd /S /C "C:\scripts\VsDevShell.cmd powershell -NoProfile -EncodedCommand $encodedCmd"
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0) {
            throw "Container execution failed with exit code $exitCode"
        }

        Write-Host ""
        Write-Host "[OK] Container execution completed successfully" -ForegroundColor Green
    }
    finally {
        # Post-run cleanup to release file locks before next host invocation
        docker exec $containerName powershell -NoProfile -Command $cleanupCommand 2>$null
    }
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
# Module Exports
# =============================================================================

Export-ModuleMember -Function @(
    'Test-InsideContainer',
    'Get-WorktreeAgentNumber',
    'Test-DockerContainerRunning',
    'Get-ContainerWorkDir',
    'Invoke-InContainer',
    'New-ContainerArgumentString'
)
