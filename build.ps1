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

.EXAMPLE
	.\build.ps1
	Builds Debug x64 in parallel with minimal logging.

.EXAMPLE
	.\build.ps1 -Configuration Release -BuildTests
	Builds Release x64 including test projects.

.EXAMPLE
	.\build.ps1 -Serial -Verbosity detailed
	Builds Debug x64 serially with detailed logging.
#>
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[string]$Platform = "x64",
	[switch]$Serial,
	[switch]$BuildTests,
	[switch]$BuildAdditionalApps,
	[string]$Verbosity = "minimal",
	[bool]$NodeReuse = $true,
	[string[]]$MsBuildArgs = @(),
	[string]$LogFile
)

$ErrorActionPreference = 'Stop'

# --- 1. Environment Setup ---

# Determine MSBuild path
$msbuildCmdInfo = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuildCmdInfo) {
	$msbuildCmd = $msbuildCmdInfo.Source
}
else {
	$msbuildCmd = 'msbuild'
}

# Initialize Visual Studio Environment
function Initialize-VsDevEnvironment {
	param(
		[string]$RequestedPlatform
	)

	if ($env:OS -ne 'Windows_NT') {
		return
	}

	if ($env:VCINSTALLDIR) {
		Write-Host '✓ Visual Studio environment already initialized' -ForegroundColor Green
		return
	}

	Write-Host '🔧 Initializing Visual Studio Developer environment...' -ForegroundColor Yellow
	$vswhereCandidates = @()
	if ($env:ProgramFiles) {
		$pfVswhere = Join-Path -Path $env:ProgramFiles -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
		if (Test-Path $pfVswhere) {
			$vswhereCandidates += $pfVswhere
		}
	}
	$programFilesX86 = ${env:ProgramFiles(x86)}
	if ($programFilesX86) {
		$pf86Vswhere = Join-Path -Path $programFilesX86 -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
		if (Test-Path $pf86Vswhere) {
			$vswhereCandidates += $pf86Vswhere
		}
	}

	if (-not $vswhereCandidates) {
		Write-Host ''
		Write-Host '❌ ERROR: Visual Studio 2017+ not found' -ForegroundColor Red
		Write-Host '   Native C++ builds require Visual Studio with required workloads' -ForegroundColor Red
		Write-Host ''
		Write-Host '   Install from: https://visualstudio.microsoft.com/downloads/' -ForegroundColor Yellow
		Write-Host '   Required workloads:' -ForegroundColor Yellow
		Write-Host '     - Desktop development with C++' -ForegroundColor Yellow
		Write-Host '     - .NET desktop development' -ForegroundColor Yellow
		Write-Host ''
		throw 'Visual Studio not found'
	}

	$vsInstallPath = & $vswhereCandidates[0] -latest -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -products * -property installationPath
	if (-not $vsInstallPath) {
		Write-Host ''
		Write-Host '❌ ERROR: Visual Studio found but missing required C++ tools' -ForegroundColor Red
		Write-Host '   Please install the "Desktop development with C++" workload' -ForegroundColor Red
		Write-Host ''
		throw 'Visual Studio C++ tools not found'
	}

	$vsDevCmd = Join-Path -Path $vsInstallPath -ChildPath 'Common7\Tools\VsDevCmd.bat'
	if (-not (Test-Path $vsDevCmd)) {
		throw "Unable to locate VsDevCmd.bat under '$vsInstallPath'."
	}

	if ($RequestedPlatform -eq 'x86') {
		throw "x86 build is no longer supported."
	}
	$arch = 'amd64'
	$vsVersion = Split-Path (Split-Path (Split-Path (Split-Path $vsInstallPath))) -Leaf
	Write-Host "   Found Visual Studio $vsVersion at: $vsInstallPath" -ForegroundColor Gray
	Write-Host "   Setting up environment for $arch..." -ForegroundColor Gray

	$cmdArgs = "`"$vsDevCmd`" -no_logo -arch=$arch -host_arch=$arch && set"
	$envOutput = & cmd.exe /c $cmdArgs 2>&1
	if ($LASTEXITCODE -ne 0) {
		Write-Host ''
		Write-Host "❌ ERROR: VsDevCmd.bat failed with exit code $LASTEXITCODE" -ForegroundColor Red
		throw 'Failed to initialize Visual Studio environment'
	}

	foreach ($line in $envOutput) {
		$parts = $line -split '=', 2
		if ($parts.Length -eq 2 -and $parts[0]) {
			Set-Item -Path "Env:$($parts[0])" -Value $parts[1]
		}
	}

	if (-not $env:VCINSTALLDIR) {
		Write-Host ''
		Write-Host '❌ ERROR: VCINSTALLDIR not set after initialization' -ForegroundColor Red
		Write-Host '   This usually means the C++ tools are not properly installed' -ForegroundColor Red
		throw 'Visual Studio C++ environment not configured'
	}

	Write-Host '✓ Visual Studio environment initialized successfully' -ForegroundColor Green
	Write-Host "   VCINSTALLDIR: $env:VCINSTALLDIR" -ForegroundColor Gray
}

Initialize-VsDevEnvironment -RequestedPlatform $Platform

# Help legacy MSBuild tasks distinguish platform-specific assets.
# Set this AFTER Initialize-VsDevEnvironment to ensure it's not overwritten
if ($Platform -eq 'x86') {
	throw "x86 build is no longer supported."
}
$env:arch = 'x64'
Write-Host "Set arch environment variable to: $env:arch" -ForegroundColor Green

# --- 2. Build Configuration ---

# Determine logical core count for CL_MPCount
if ($env:CL_MPCount) {
	$mpCount = $env:CL_MPCount
}
else {
	# Default to 8 or number of processors if less
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
$finalMsBuildArgs += "/p:CL_MPCount=$mpCount"

if ($BuildTests) {
	$finalMsBuildArgs += "/p:BuildTests=true"
}

if ($BuildAdditionalApps) {
	$finalMsBuildArgs += "/p:BuildAdditionalApps=true"
}

# Add user-supplied args
$finalMsBuildArgs += $MsBuildArgs

function Invoke-MSBuildStep {
	param(
		[string[]]$Arguments,
		[string]$Description,
		[string]$LogPath
	)

	# Only print the command once, concisely
	Write-Host "Running $Description..." -ForegroundColor Cyan
	# Write-Host "& $msbuildCmd $Arguments" -ForegroundColor DarkGray

	if ($LogPath) {
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

# --- 3. Execution ---

Write-Host "Building FieldWorks..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration | Platform: $Platform | Parallel: $(-not $Serial) | Tests: $BuildTests" -ForegroundColor Cyan

if ($BuildAdditionalApps) {
	Write-Host "Including optional FieldWorks executables" -ForegroundColor Yellow
}

# Bootstrap: Build FwBuildTasks first (required by SetupInclude.targets)
# Note: FwBuildTasks is small, so we use minimal args here to keep it quiet and fast
Invoke-MSBuildStep `
	-Arguments @('Build/Src/FwBuildTasks/FwBuildTasks.csproj', '/t:Restore;Build', "/p:Configuration=$Configuration", "/p:Platform=$Platform", "/v:quiet", "/nologo") `
	-Description 'FwBuildTasks (Bootstrap)'

# Restore packages
Invoke-MSBuildStep `
	-Arguments @('Build/Orchestrator.proj', '/t:RestorePackages', "/p:Configuration=$Configuration", "/p:Platform=$Platform", "/v:quiet", "/nologo") `
	-Description 'RestorePackages'

# Build using traversal project
Invoke-MSBuildStep `
	-Arguments (@('FieldWorks.proj') + $finalMsBuildArgs) `
	-Description "FieldWorks Solution" `
	-LogPath $LogFile

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "Output: Output\$Configuration" -ForegroundColor Cyan