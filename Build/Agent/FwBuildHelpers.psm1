<#
.SYNOPSIS
    Shared helper functions for FieldWorks build and test scripts.

.DESCRIPTION
    This module provides common functionality for build.ps1 and test.ps1:
    - Worktree path detection
    - VS environment initialization
    - Conflicting process cleanup
    - Stale obj folder cleanup
    - MSBuild execution helpers

    This is the main entry point that imports specialized sub-modules.

.NOTES
    Import this module at the start of build.ps1 and test.ps1:
    Import-Module "$PSScriptRoot/Build/Agent/FwBuildHelpers.psm1" -Force
#>

# =============================================================================
# Import Sub-Modules
# =============================================================================

$moduleRoot = $PSScriptRoot

# Build environment (VS setup, MSBuild, VSTest)
Import-Module (Join-Path $moduleRoot "FwBuildEnvironment.psm1") -Force

# =============================================================================
# Process Management Functions
# =============================================================================

function Stop-ConflictingProcesses {
    <#
    .SYNOPSIS
        Stops processes that could interfere with builds/tests.
    .DESCRIPTION
        Kills build- and test-related processes that can hold locks on artifacts
        such as FwBuildTasks.dll. Defaults to the current session.

        Implements "Smart Kill" strategy:
        1. Identifies processes by name (msbuild, dotnet, etc.)
        2. Filters by current session ID
        3. If RepoRoot is provided, filters by:
           - Command line containing RepoRoot path
           - Loaded modules (DLLs) within RepoRoot path

        This allows concurrent builds in different worktrees to coexist without
        killing each other's MSBuild nodes.
    #>
    param(
        [string[]]$AdditionalProcessNames = @(),
        [switch]$IncludeOmniSharp,
        [string]$RepoRoot
    )

    $conflicts = @(
        # Managed build/test hosts (Persistent lockers)
        "dotnet", "msbuild", "VBCSCompiler", "vstest.console", "testhost", "FieldWorks"
    )

    if ($IncludeOmniSharp) {
        $conflicts += @("OmniSharp", "OmniSharp.Http", "OmniSharp.Stdio")
    }

    if ($AdditionalProcessNames) {
        $conflicts += $AdditionalProcessNames
    }

    $conflicts = $conflicts | Where-Object { $_ } | Select-Object -Unique

    $currentSessionId = (Get-Process -Id $PID).SessionId

    $processes = foreach ($name in $conflicts) {
        Get-Process -Name $name -ErrorAction SilentlyContinue
    }

    # Always filter by current session
    $processes = $processes | Where-Object { $_.SessionId -eq $currentSessionId }

    # Filter by RepoRoot (Smart Kill) - only kill processes locking files in this repo
    if ($RepoRoot) {
        $processesToKill = @()
        $RepoRoot = $RepoRoot.TrimEnd('\').TrimEnd('/')

        foreach ($p in $processes) {
            if ($p.Id -eq $PID) { continue } # Don't kill self

            $isRelated = $false

            # 1. Check Command Line (fast)
            try {
                $cim = Get-CimInstance Win32_Process -Filter "ProcessId = $($p.Id)" -ErrorAction SilentlyContinue
                if ($cim.CommandLine -and $cim.CommandLine.IndexOf($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    $isRelated = $true
                }
            } catch {}

            # 2. Check Modules (slower, but catches MSBuild nodes holding DLLs)
            if (-not $isRelated) {
                try {
                    # Check if any loaded module is within the RepoRoot
                    if ($p.Modules | Where-Object { $_.FileName -and $_.FileName.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase) }) {
                        $isRelated = $true
                    }
                } catch {}
            }

            if ($isRelated) {
                $processesToKill += $p
            }
        }
        $processes = $processesToKill
    }

    if ($processes) {
        $byName = $processes | Group-Object -Property ProcessName
        foreach ($group in $byName) {
            $count = @($group.Group).Count
            Write-Host "Closing $count stale $($group.Name) process(es)..." -ForegroundColor Yellow
            $group.Group | Stop-Process -Force -ErrorAction SilentlyContinue
        }

        Start-Sleep -Milliseconds 500
    }
}

function Test-IsFileLockError {
    param([Parameter(Mandatory)][System.Management.Automation.ErrorRecord]$ErrorRecord)

    $messages = @()
    $messages += $ErrorRecord.ToString()
    if ($ErrorRecord.Exception) {
        $messages += $ErrorRecord.Exception.Message
        if ($ErrorRecord.Exception.InnerException) {
            $messages += $ErrorRecord.Exception.InnerException.Message
        }
    }

    $lockPatterns = @(
        'used by another process',
        'being used by another process',
        'cannot access the file',
        'file is locked',
        'Access to the path .* denied',
        'sharing violation'
    )

    foreach ($pattern in $lockPatterns) {
        if ($messages -match $pattern) {
            return $true
        }
    }

    return $false
}

function Invoke-WithFileLockRetry {
    param(
        [Parameter(Mandatory)][ScriptBlock]$Action,
        [Parameter(Mandatory)][string]$Context,
        [switch]$IncludeOmniSharp,
        [int]$MaxAttempts = 2
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $retry = $false
        try {
            & $Action
            return
        }
        catch {
            if ($attempt -lt $MaxAttempts -and (Test-IsFileLockError -ErrorRecord $_)) {
                $nextAttempt = $attempt + 1
                Write-Host "[WARN] $Context hit a file lock. Cleaning and retrying (attempt $nextAttempt of $MaxAttempts)..." -ForegroundColor Yellow
                Stop-ConflictingProcesses -IncludeOmniSharp:$IncludeOmniSharp
                Start-Sleep -Seconds 2
                $retry = $true
            }
            else {
                throw
            }
        }

        if (-not $retry) {
            throw
        }
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
    try {
        # Use .NET enumeration for performance (faster than Get-ChildItem -Recurse)
        $staleObjFolders = [System.IO.Directory]::GetDirectories($srcPath, "obj", [System.IO.SearchOption]::AllDirectories)
        if ($staleObjFolders.Length -gt 0) {
            Write-Host "Removing stale per-project obj/ folders ($($staleObjFolders.Length) found)..." -ForegroundColor Yellow
            foreach ($folder in $staleObjFolders) {
                Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
            }
            Write-Host "[OK] Stale obj/ folders cleaned" -ForegroundColor Green
        }
    }
    catch {
        # Ignore enumeration errors (access denied, etc.)
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

function Test-CoffArchiveHeader {
    <#
    .SYNOPSIS
        Validates the COFF archive magic at the start of a .lib file.
    .DESCRIPTION
        Reads the first 8 bytes and checks for the standard "!<arch>\n" header.
        Returns $true when the header matches, $false when it is readable but
        does not match, and $null when the file cannot be opened (skip delete).
    #>
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path $Path -PathType Leaf)) { return $null }

    $expected = "!<arch>\n"
    $buffer = New-Object byte[] ($expected.Length)
    $bytesRead = 0

    try {
        $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
        try {
            $bytesRead = $stream.Read($buffer, 0, $buffer.Length)
        }
        finally {
            $stream.Dispose()
        }
    }
    catch {
        return $null
    }

    if ($bytesRead -lt $expected.Length) { return $false }

    $actual = [System.Text.Encoding]::ASCII.GetString($buffer)
    return $actual -eq $expected
}

function Test-GitTrackedFile {
    <#
    .SYNOPSIS
        Returns $true if the path is tracked by git, $false if untracked, $null on error.
    #>
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$Path
    )

    if (-not (Test-Path $RepoRoot)) { return $null }

    $relPath = $Path
    try {
        $uriRoot = New-Object System.Uri($RepoRoot + [System.IO.Path]::DirectorySeparatorChar)
        $uriPath = New-Object System.Uri($Path)
        $relPath = $uriRoot.MakeRelativeUri($uriPath).ToString().Replace('/', '\')
    }
    catch { }

    $gitExe = "git"
    $arguments = @('-C', $RepoRoot, 'ls-files', '--error-unmatch', $relPath)
    try {
        $p = Start-Process -FilePath $gitExe -ArgumentList $arguments -NoNewWindow -Wait -PassThru -ErrorAction Stop
        return $p.ExitCode -eq 0
    }
    catch {
        return $null
    }
}

# =============================================================================
# Module Exports
# =============================================================================

# Re-export functions from sub-modules plus local functions
Export-ModuleMember -Function @(
    # From FwBuildEnvironment.psm1
    'Initialize-VsDevEnvironment',
    'Get-MSBuildPath',
    'Invoke-MSBuild',
    'Get-VSTestPath',
    'Test-CvtresCompatibility',
    'Get-CvtresDiagnostics',
    # Local functions
    'Stop-ConflictingProcesses',
    'Remove-StaleObjFolders',
    'Test-IsFileLockError',
    'Invoke-WithFileLockRetry'
)
