<#
.SYNOPSIS
    Visual Studio and build tool environment helpers for FieldWorks.

.DESCRIPTION
    Provides VS environment initialization, MSBuild execution, and
    VSTest path discovery.

.NOTES
    Used by FwBuildHelpers.psm1 - do not import directly.
#>

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

function Get-CvtresDiagnostics {
    <#
    .SYNOPSIS
        Returns details about the cvtres.exe resolved in the current session.
    #>
    $result = [ordered]@{
        Path = $null
        IsVcToolset = $false
        IsDotNetFramework = $false
    }

    $cmd = Get-Command "cvtres.exe" -ErrorAction SilentlyContinue
    if ($cmd) {
        $result.Path = $cmd.Source
        $lower = $result.Path.ToLowerInvariant()
        $result.IsVcToolset = $lower -match "[\\/]vc[\\/]tools[\\/]msvc[\\/][^\\/]+[\\/]bin[\\/]hostx64[\\/]x64[\\/]cvtres\.exe$"
        $result.IsDotNetFramework = $lower -match "windows[\\/]microsoft\.net[\\/]framework"
        return $result
    }

    if ($env:VCINSTALLDIR) {
        $candidates = Get-ChildItem -Path (Join-Path $env:VCINSTALLDIR "Tools\MSVC\*") -Filter cvtres.exe -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending
        if ($candidates -and $candidates.Count -gt 0) {
            $result.Path = $candidates[0].FullName
            $lower = $result.Path.ToLowerInvariant()
            $result.IsVcToolset = $lower -match "[\\/]vc[\\/]tools[\\/]msvc[\\/][^\\/]+[\\/]bin[\\/]hostx64[\\/]x64[\\/]cvtres\.exe$"
            $result.IsDotNetFramework = $lower -match "windows[\\/]microsoft\.net[\\/]framework"
        }
    }

    return $result
}

function Test-CvtresCompatibility {
    <#
    .SYNOPSIS
        Emits warnings if cvtres.exe resolves to a non-VC toolset binary.
    #>
    $diag = Get-CvtresDiagnostics

    if (-not $diag.Path) {
        Write-Host "[WARN] cvtres.exe not found after VS environment setup. Toolchain may be incomplete." -ForegroundColor Yellow
        return
    }

    if ($diag.IsDotNetFramework) {
        Write-Host "[WARN] cvtres.exe resolves to a .NET Framework path. Prefer the VC toolset version (Hostx64\\x64). $($diag.Path)" -ForegroundColor Yellow
    }
    elseif (-not $diag.IsVcToolset) {
        Write-Host "[WARN] cvtres.exe is not from the VC toolset Hostx64\\x64 folder. Confirm PATH ordering. $($diag.Path)" -ForegroundColor Yellow
    }
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
    'Initialize-VsDevEnvironment',
	'Test-CvtresCompatibility',
    'Get-MSBuildPath',
    'Invoke-MSBuild',
    'Get-VSTestPath'
)
