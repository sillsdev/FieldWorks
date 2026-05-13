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

function Get-VsWherePath {
    <#
    .SYNOPSIS
        Returns the path to the Microsoft-provided vswhere executable.
    #>
    $candidates = @()
    if ($env:ProgramFiles) {
        $candidates += (Join-Path -Path $env:ProgramFiles -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe')
    }

    $programFilesX86 = ${env:ProgramFiles(x86)}
    if ($programFilesX86) {
        $candidates += (Join-Path -Path $programFilesX86 -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe')
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Get-VsInstallationInfo {
    <#
    .SYNOPSIS
        Returns installation metadata for the latest matching Visual Studio instance.
    #>
    param(
        [string[]]$Requires = @()
    )

    $vsWhere = Get-VsWherePath
    if (-not $vsWhere) {
        return $null
    }

    $vsWhereArgs = @('-latest', '-products', '*')
    if ($Requires -and $Requires.Count -gt 0) {
        $vsWhereArgs += '-requires'
        $vsWhereArgs += $Requires
    }

    $installationPath = & $vsWhere @vsWhereArgs -property installationPath
    if (-not $installationPath) {
        return $null
    }

    $displayVersion = & $vsWhere @vsWhereArgs -property catalog_productDisplayVersion

    return [pscustomobject]@{
        VsWherePath = $vsWhere
        InstallationPath = $installationPath
        DisplayVersion = $displayVersion
    }
}

function Get-VsToolchainInfo {
    <#
    .SYNOPSIS
        Returns derived toolchain paths for the latest matching Visual Studio instance.
    #>
    param(
        [string[]]$Requires = @('Microsoft.Component.MSBuild')
    )

    $vsInfo = Get-VsInstallationInfo -Requires $Requires
    if (-not $vsInfo) {
        return $null
    }

    $installationPath = $vsInfo.InstallationPath
    $vsDevCmdPath = Join-Path $installationPath 'Common7\Tools\VsDevCmd.bat'
    if (-not (Test-Path $vsDevCmdPath)) {
        $vsDevCmdPath = $null
    }

    $msbuildCandidates = @(
        (Join-Path $installationPath 'MSBuild\Current\Bin\amd64\MSBuild.exe'),
        (Join-Path $installationPath 'MSBuild\Current\Bin\MSBuild.exe')
    )
    $msbuildPath = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    $vsTestPath = Join-Path $installationPath 'Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe'
    if (-not (Test-Path $vsTestPath)) {
        $vsTestPath = $null
    }

    $vcInstallDir = Join-Path $installationPath 'VC'
    if (-not (Test-Path $vcInstallDir)) {
        $vcInstallDir = $null
    }

    $vcTargetsPath = Join-Path $installationPath 'MSBuild\Microsoft\VC\v170'
    if (-not (Test-Path $vcTargetsPath)) {
        $vcTargetsPath = $null
    }

    return [pscustomobject]@{
        VsWherePath = $vsInfo.VsWherePath
        InstallationPath = $installationPath
        DisplayVersion = $vsInfo.DisplayVersion
        VsDevCmdPath = $vsDevCmdPath
        MSBuildPath = $msbuildPath
        VSTestPath = $vsTestPath
        VcInstallDir = $vcInstallDir
        VCTargetsPath = $vcTargetsPath
    }
}

function Get-VsDevEnvironmentVariables {
    <#
    .SYNOPSIS
        Returns the environment variables produced by VsDevCmd.bat.
    #>
    param(
        [string]$Architecture = 'amd64',
        [string]$HostArchitecture = 'amd64',
        [string[]]$Requires = @('Microsoft.Component.MSBuild', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64')
    )

    $toolchain = Get-VsToolchainInfo -Requires $Requires
    if (-not $toolchain) {
        return $null
    }

    if (-not $toolchain.VsDevCmdPath) {
        throw "Unable to locate VsDevCmd.bat under '$($toolchain.InstallationPath)'."
    }

    $cmdArgs = "`"$($toolchain.VsDevCmdPath)`" -no_logo -arch=$Architecture -host_arch=$HostArchitecture && set"
    $envOutput = & cmd.exe /c $cmdArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to initialize Visual Studio environment'
    }

    $variables = [ordered]@{}
    foreach ($line in $envOutput) {
        $parts = $line -split '=', 2
        if ($parts.Length -eq 2 -and $parts[0]) {
            $variables[$parts[0]] = $parts[1]
        }
    }

    return [pscustomobject]@{
        Toolchain = $toolchain
        Variables = [pscustomobject]$variables
    }
}

function Get-ActiveVcToolBinPath {
    <#
    .SYNOPSIS
        Returns the HostX64\x64 tool bin directory for the active VC toolset.
    #>
    if (-not [string]::IsNullOrWhiteSpace($env:VCToolsInstallDir)) {
        $preferred = Join-Path $env:VCToolsInstallDir 'bin\HostX64\x64'
        if (Test-Path (Join-Path $preferred 'cl.exe')) {
            return $preferred
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($env:VCINSTALLDIR)) {
        $legacy = Join-Path $env:VCINSTALLDIR 'bin'
        if (Test-Path (Join-Path $legacy 'cl.exe')) {
            return $legacy
        }
    }

    return $null
}

function Test-VsDevEnvironmentActive {
    <#
    .SYNOPSIS
        Returns true when a full VsDevCmd environment is already active.
    #>
    if ($env:OS -ne 'Windows_NT') {
        return $false
    }

    if ([string]::IsNullOrWhiteSpace($env:VSCMD_VER) -or [string]::IsNullOrWhiteSpace($env:VCToolsInstallDir)) {
        return $false
    }

    $activeVcToolPath = Get-ActiveVcToolBinPath
    if (-not $activeVcToolPath) {
        return $false
    }

    $cl = Get-Command 'cl.exe' -ErrorAction SilentlyContinue
    $nmake = Get-Command 'nmake.exe' -ErrorAction SilentlyContinue
    if (-not $cl -or -not $nmake) {
        return $false
    }

    $normalizedToolPath = $activeVcToolPath.TrimEnd('\')
    $clDirectory = (Split-Path -Parent $cl.Source).TrimEnd('\')
    $nmakeDirectory = (Split-Path -Parent $nmake.Source).TrimEnd('\')

    return [string]::Equals($clDirectory, $normalizedToolPath, [System.StringComparison]::OrdinalIgnoreCase) -and
        [string]::Equals($nmakeDirectory, $normalizedToolPath, [System.StringComparison]::OrdinalIgnoreCase)
}

function Ensure-PreferredVcToolPath {
    <#
    .SYNOPSIS
        Moves the active HostX64\x64 MSVC bin directory to the front of PATH.
    #>
    $preferred = Get-ActiveVcToolBinPath
    if (-not $preferred) {
        return
    }

    $pathEntries = @()
    if (-not [string]::IsNullOrWhiteSpace($env:PATH)) {
        $pathEntries = $env:PATH -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    }

    $filteredEntries = $pathEntries | Where-Object {
        -not [string]::Equals($_.TrimEnd('\'), $preferred.TrimEnd('\'), [System.StringComparison]::OrdinalIgnoreCase)
    }

    $env:PATH = (@($preferred) + $filteredEntries) -join ';'
}

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

    if (Test-VsDevEnvironmentActive) {
        Ensure-PreferredVcToolPath
        Write-Host '[OK] Visual Studio environment already initialized' -ForegroundColor Green
        return
    }

    if ($env:VCINSTALLDIR -or $env:VCToolsInstallDir -or $env:VSCMD_VER) {
        Write-Host '[WARN] Partial Visual Studio environment detected. Reinitializing...' -ForegroundColor Yellow
    }

    Write-Host 'Initializing Visual Studio Developer environment...' -ForegroundColor Yellow

    $vsToolchain = Get-VsToolchainInfo -Requires @('Microsoft.Component.MSBuild', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64')

    if (-not $vsToolchain) {
        $vsWhere = Get-VsWherePath
        Write-Host ''
        if (-not $vsWhere) {
            Write-Host '[ERROR] Visual Studio 2017+ not found' -ForegroundColor Red
            Write-Host '   Install from: https://visualstudio.microsoft.com/downloads/' -ForegroundColor Yellow
            throw 'Visual Studio not found'
        }

        Write-Host '[ERROR] Visual Studio found but missing required C++ tools' -ForegroundColor Red
        Write-Host '   Please install the "Desktop development with C++" workload' -ForegroundColor Yellow
        throw 'Visual Studio C++ tools not found'
    }

    # x64-only build
    $arch = 'amd64'
    $vsInstallPath = $vsToolchain.InstallationPath
    $vsVersion = if ([string]::IsNullOrWhiteSpace($vsToolchain.DisplayVersion)) {
        Split-Path (Split-Path (Split-Path (Split-Path $vsInstallPath))) -Leaf
    }
    else {
        $vsToolchain.DisplayVersion
    }
    Write-Host "   Found Visual Studio $vsVersion at: $vsInstallPath" -ForegroundColor Gray
    Write-Host "   Setting up environment for $arch..." -ForegroundColor Gray

    $vsEnvironment = Get-VsDevEnvironmentVariables -Architecture $arch -HostArchitecture $arch
    foreach ($variable in $vsEnvironment.Variables.PSObject.Properties) {
        Set-Item -Path "Env:$($variable.Name)" -Value $variable.Value
    }

    if (-not (Test-VsDevEnvironmentActive)) {
        throw 'Visual Studio C++ environment not configured'
    }

    Ensure-PreferredVcToolPath

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

    $toolchain = Get-VsToolchainInfo -Requires @('Microsoft.Component.MSBuild')
    if ($toolchain -and $toolchain.MSBuildPath) {
        return $toolchain.MSBuildPath
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

    $toolchain = Get-VsToolchainInfo -Requires @('Microsoft.Component.MSBuild')
    if ($toolchain -and $toolchain.VSTestPath) {
        return $toolchain.VSTestPath
    }

    return $null
}

# =============================================================================
# Module Exports
# =============================================================================

Export-ModuleMember -Function @(
    'Get-VsWherePath',
    'Get-VsInstallationInfo',
    'Get-VsToolchainInfo',
    'Get-VsDevEnvironmentVariables',
    'Test-VsDevEnvironmentActive',
    'Initialize-VsDevEnvironment',
	'Test-CvtresCompatibility',
    'Get-MSBuildPath',
    'Invoke-MSBuild',
    'Get-VSTestPath'
)
