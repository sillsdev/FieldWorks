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

function Get-VsDisplayLabel {
    <#
    .SYNOPSIS
        Formats a Visual Studio instance for build output.
    .DESCRIPTION
        Leads with the installer's product name because it carries the release year
        ('Visual Studio Community 2026'), which the version numbers do not. The
        version is reduced to its product version; a trailing servicing date is
        dropped because VS 2022 reports display versions such as
        '17.14.37 (July 2026)', where the parenthesized year invites the reader to
        mistake a 2022 build for a 2026 one. Falls back to the version alone when
        an instance reports no product name.
    #>
    param(
        [string]$DisplayName,
        [string]$DisplayVersion,
        [string]$InstallationVersion
    )

    $name = "$DisplayName".Trim()

    $version = "$DisplayVersion".Trim()
    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = "$InstallationVersion".Trim()
    }
    if ($version -match '^(?<product>.+?)\s*\([^)]*\)$') {
        $version = $Matches['product'].Trim()
    }

    if (-not [string]::IsNullOrWhiteSpace($name)) {
        if ([string]::IsNullOrWhiteSpace($version)) {
            return $name
        }

        return "$name ($version)"
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        return 'Visual Studio (unknown version)'
    }

    return "Visual Studio $version"
}

function Get-FwToolchainPolicy {
    <#
    .SYNOPSIS
        Returns the repo-controlled FieldWorks toolchain policy.
    .DESCRIPTION
        Reads Build/FieldWorks.Toolchain.props: the supported Visual Studio version
        range plus the per-Visual-Studio-major toolset mapping carried by the
        numbered properties (FwPlatformToolset17, FwVCTargetsVersion18, ...).
        ToolsetsByMajor is keyed by the Visual Studio major version as a string.
    #>
    if ($script:FwToolchainPolicy) {
        return $script:FwToolchainPolicy
    }

    $policyPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'FieldWorks.Toolchain.props'
    $defaultRange = '[17.0,19.0)'
    $defaultToolsets = @{
        '17' = [pscustomobject]@{ VCTargetsVersion = 'v170'; PlatformToolset = 'v143'; DotNetFrameworkSdkVisualStudioVersion = '17.0' }
        '18' = [pscustomobject]@{ VCTargetsVersion = 'v180'; PlatformToolset = 'v145'; DotNetFrameworkSdkVisualStudioVersion = '18.0' }
    }

    if (-not (Test-Path $policyPath)) {
        $script:FwToolchainPolicy = [pscustomobject]@{
            VisualStudioVersionRange = $defaultRange
            ToolsetsByMajor = $defaultToolsets
        }
        return $script:FwToolchainPolicy
    }

    [xml]$policyXml = Get-Content -LiteralPath $policyPath -Raw
    $propertyGroups = @($policyXml.Project.PropertyGroup)

    function Get-PolicyValue {
        param(
            [string]$Name,
            [string]$DefaultValue
        )

        foreach ($propertyGroup in $propertyGroups) {
            $node = $propertyGroup.$Name
            if (-not $node) {
                continue
            }

            # The XML adapter returns a plain string for attribute-less elements
            # and an XmlElement when attributes (e.g. Condition) are present.
            foreach ($candidate in @($node)) {
                $value = if ($candidate -is [System.Xml.XmlElement]) { $candidate.'#text' } else { [string]$candidate }
                if (-not [string]::IsNullOrWhiteSpace($value)) {
                    return $value.Trim()
                }
            }
        }

        return $DefaultValue
    }

    # Majors are discovered from the numbered FwPlatformToolset<major> properties so
    # adding a new Visual Studio major to the policy file needs no script change.
    $toolsets = @{}
    foreach ($propertyGroup in $propertyGroups) {
        foreach ($child in @($propertyGroup.ChildNodes)) {
            if ($child.Name -match '^FwPlatformToolset(\d+)$') {
                $major = $Matches[1]
                if (-not $toolsets.ContainsKey($major)) {
                    $toolsets[$major] = [pscustomobject]@{
                        VCTargetsVersion = Get-PolicyValue -Name "FwVCTargetsVersion$major" -DefaultValue $null
                        PlatformToolset = Get-PolicyValue -Name "FwPlatformToolset$major" -DefaultValue $null
                        DotNetFrameworkSdkVisualStudioVersion = Get-PolicyValue -Name "FwDotNetFrameworkSdkVisualStudioVersion$major" -DefaultValue $null
                    }
                }
            }
        }
    }

    if ($toolsets.Count -eq 0) {
        $toolsets = $defaultToolsets
    }

    $script:FwToolchainPolicy = [pscustomobject]@{
        VisualStudioVersionRange = Get-PolicyValue -Name 'FwVisualStudioVersionRange' -DefaultValue $defaultRange
        ToolsetsByMajor = $toolsets
    }

    return $script:FwToolchainPolicy
}

function Get-VsInstallationInfo {
    <#
    .SYNOPSIS
        Returns installation metadata for the newest Visual Studio instance in the
        supported version range.
    .DESCRIPTION
        Selection is prefer-newest: when the newest in-range instance is missing a
        required component this function throws with install instructions instead of
        falling back to an older instance, so the toolchain a machine builds with is
        deterministic. Returns $null when vswhere or any in-range instance is absent.
    #>
    param(
        [string[]]$Requires = @(),
        [string]$VersionRange = ''
    )

    $vsWhere = Get-VsWherePath
    if (-not $vsWhere) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($VersionRange)) {
        $VersionRange = (Get-FwToolchainPolicy).VisualStudioVersionRange
    }

    $baseArgs = @('-latest', '-products', '*')
    if (-not [string]::IsNullOrWhiteSpace($VersionRange)) {
        $baseArgs += '-version'
        $baseArgs += $VersionRange
    }

    $installationPath = & $vsWhere @baseArgs -property installationPath
    if (-not $installationPath) {
        return $null
    }

    $installationVersion = & $vsWhere @baseArgs -property installationVersion

    if ($Requires -and $Requires.Count -gt 0) {
        $qualifiedArgs = $baseArgs + @('-requires') + $Requires
        $qualifiedPath = & $vsWhere @qualifiedArgs -property installationPath
        if (-not $qualifiedPath -or -not [string]::Equals("$qualifiedPath", "$installationPath", [System.StringComparison]::OrdinalIgnoreCase)) {
            $requiresList = $Requires -join ', '
            throw ("Visual Studio $installationVersion at '$installationPath' is the newest installation in the supported range $VersionRange, " +
                "but it is missing required components: $requiresList. FieldWorks builds with the newest supported Visual Studio and does not " +
                "fall back to an older one. Open the Visual Studio Installer and add the missing workloads/components (the repo-root .vsconfig " +
                "lists everything FieldWorks needs), then retry.")
        }
    }

    $displayVersion = & $vsWhere @baseArgs -property catalog_productDisplayVersion
    $displayName = & $vsWhere @baseArgs -property displayName

    $visualStudioMajor = $null
    if ("$installationVersion" -match '^(\d+)\.') {
        $visualStudioMajor = $Matches[1]
    }

    return [pscustomobject]@{
        VsWherePath = $vsWhere
        InstallationPath = $installationPath
        InstallationVersion = $installationVersion
        VisualStudioMajor = $visualStudioMajor
        DisplayName = $displayName
        DisplayVersion = $displayVersion
        DisplayLabel = Get-VsDisplayLabel -DisplayName "$displayName" -DisplayVersion "$displayVersion" -InstallationVersion "$installationVersion"
    }
}

function Get-VsToolchainInfo {
    <#
    .SYNOPSIS
        Returns derived toolchain paths for the selected Visual Studio instance.
    .DESCRIPTION
        Combines the selected installation with the policy's per-major toolset
        mapping (PlatformToolset, VC targets folder, .NET Framework SDK version).
        Throws when the selected Visual Studio major has no mapping in
        Build/FieldWorks.Toolchain.props.
    #>
    param(
        [string[]]$Requires = @('Microsoft.Component.MSBuild')
    )

    $vsInfo = Get-VsInstallationInfo -Requires $Requires
    if (-not $vsInfo) {
        return $null
    }

    $toolchainPolicy = Get-FwToolchainPolicy
    $visualStudioMajor = $vsInfo.VisualStudioMajor
    $toolset = $null
    if ($visualStudioMajor -and $toolchainPolicy.ToolsetsByMajor.ContainsKey($visualStudioMajor)) {
        $toolset = $toolchainPolicy.ToolsetsByMajor[$visualStudioMajor]
    }
    if (-not $toolset) {
        throw ("Visual Studio $($vsInfo.InstallationVersion) at '$($vsInfo.InstallationPath)' has no toolchain mapping in Build/FieldWorks.Toolchain.props. " +
            "Add FwPlatformToolset$visualStudioMajor, FwVCTargetsVersion$visualStudioMajor, and FwDotNetFrameworkSdkVisualStudioVersion$visualStudioMajor entries for it.")
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

    $vcTargetsPath = $null
    if (-not [string]::IsNullOrWhiteSpace($toolset.VCTargetsVersion)) {
        $vcTargetsPath = Join-Path $installationPath (Join-Path 'MSBuild\Microsoft\VC' $toolset.VCTargetsVersion)
        if (-not (Test-Path $vcTargetsPath)) {
            $vcTargetsPath = $null
        }
    }

    return [pscustomobject]@{
        VsWherePath = $vsInfo.VsWherePath
        InstallationPath = $installationPath
        InstallationVersion = $vsInfo.InstallationVersion
        VisualStudioMajor = $visualStudioMajor
        DisplayName = $vsInfo.DisplayName
        DisplayVersion = $vsInfo.DisplayVersion
        DisplayLabel = $vsInfo.DisplayLabel
        VisualStudioVersionRange = $toolchainPolicy.VisualStudioVersionRange
        VsDevCmdPath = $vsDevCmdPath
        MSBuildPath = $msbuildPath
        VSTestPath = $vsTestPath
        VcInstallDir = $vcInstallDir
        VCTargetsPath = $vcTargetsPath
        PlatformToolset = $toolset.PlatformToolset
        DotNetFrameworkSdkVisualStudioVersion = $toolset.DotNetFrameworkSdkVisualStudioVersion
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

    $variables = ConvertFrom-EnvironmentVariableLines -Lines $envOutput

    return [pscustomobject]@{
        Toolchain = $toolchain
        Variables = [pscustomobject]$variables
    }
}

function ConvertFrom-EnvironmentVariableLines {
    param(
        [string[]]$Lines
    )

    $variables = [ordered]@{}
    foreach ($line in $Lines) {
        $parts = $line -split '=', 2
        if ($parts.Length -eq 2 -and $parts[0] -and -not $variables.Contains($parts[0])) {
            $variables.Add($parts[0], $parts[1])
        }
    }

    return [pscustomobject]$variables
}

function Set-ProcessEnvironmentVariables {
    param(
        [pscustomobject]$Variables
    )

    $existingVariables = [System.Environment]::GetEnvironmentVariables('Process')
    foreach ($variable in $Variables.PSObject.Properties) {
        $namesToRemove = Get-EnvironmentVariableNamesToRemove `
            -ExistingNames $existingVariables.Keys -IncomingName $variable.Name
        foreach ($existingName in $namesToRemove) {
            # A case-duplicate group can list the same underlying Windows
            # variable more than once; removing the first already clears it.
            [System.Environment]::SetEnvironmentVariable($existingName, $null)
        }

        Set-Item -Path "Env:$($variable.Name)" -Value $variable.Value
    }
}

function Get-EnvironmentVariableNamesToRemove {
    param(
        [string[]]$ExistingNames,
        [string]$IncomingName
    )

    foreach ($existingName in $ExistingNames) {
        if ([string]::Equals($existingName, $IncomingName, [System.StringComparison]::OrdinalIgnoreCase)) {
            $existingName
        }
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
        $policyRange = (Get-FwToolchainPolicy).VisualStudioVersionRange
        Write-Host ''
        if (-not $vsWhere) {
            Write-Host '[ERROR] vswhere.exe not found; no Visual Studio installation is detectable' -ForegroundColor Red
            Write-Host '   Install Visual Studio from: https://visualstudio.microsoft.com/downloads/' -ForegroundColor Yellow
            throw 'Visual Studio not found'
        }

        Write-Host "[ERROR] No Visual Studio installation found in the supported version range $policyRange (Visual Studio 2022 or 2026)" -ForegroundColor Red
        Write-Host '   Install Visual Studio 2026 (preferred) or 2022 with the workloads listed in the repo-root .vsconfig' -ForegroundColor Yellow
        throw 'Visual Studio not found'
    }

    # x64-only build
    $arch = 'amd64'
    $vsInstallPath = $vsToolchain.InstallationPath
    Write-Host "   Found $($vsToolchain.DisplayLabel) at: $vsInstallPath" -ForegroundColor Gray
    Write-Host "   Setting up environment for $arch..." -ForegroundColor Gray

    $vsEnvironment = Get-VsDevEnvironmentVariables -Architecture $arch -HostArchitecture $arch
    Set-ProcessEnvironmentVariables -Variables $vsEnvironment.Variables

    if (-not (Test-VsDevEnvironmentActive)) {
        throw 'Visual Studio C++ environment not configured'
    }

    Ensure-PreferredVcToolPath

    Write-Host '[OK] Visual Studio environment initialized successfully' -ForegroundColor Green
    Write-Host "   VCINSTALLDIR: $env:VCINSTALLDIR" -ForegroundColor Gray
    Write-Host "   PlatformToolset: $($vsToolchain.PlatformToolset)" -ForegroundColor Gray
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
	'Get-CvtresDiagnostics',
    'Get-MSBuildPath',
    'Invoke-MSBuild',
    'Get-VSTestPath'
)
