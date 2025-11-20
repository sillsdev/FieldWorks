[CmdletBinding()]

param(
	[string]$Configuration = "Debug",
	[string]$Platform = "x64",
	[string[]]$MsBuildArgs = @(),
	[string]$LogFile,
	[switch]$BuildAdditionalApps
)

$ErrorActionPreference = 'Stop'
$msbuildCmdInfo = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuildCmdInfo) {
	$msbuildCmd = $msbuildCmdInfo.Source
}
else {
	$msbuildCmd = 'msbuild'
}

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

	$arch = if ($RequestedPlatform -ieq 'x86') { 'x86' } else { 'amd64' }
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
if ($Platform) {
	switch ($Platform.ToLowerInvariant()) {
		'x86' { $env:arch = 'x86' }
		'x64' { $env:arch = 'x64' }
		default { $env:arch = $Platform }
	}
	Write-Host "Set arch environment variable to: $env:arch" -ForegroundColor Green
}

function Invoke-MSBuildStep {
	param(
		[string[]]$Arguments,
		[string]$Description,
		[string]$LogPath
	)

	Write-Host "Running: $msbuildCmd $($Arguments -join ' ')" -ForegroundColor Cyan
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

Write-Host "Building FieldWorks using MSBuild Traversal SDK (FieldWorks.proj)..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration, Platform: $Platform" -ForegroundColor Cyan

if ($BuildAdditionalApps) {
	Write-Host "Including optional FieldWorks executables (BuildAdditionalApps=true)" -ForegroundColor Yellow
}

# Bootstrap: Build FwBuildTasks first (required by SetupInclude.targets)
Write-Host "🔧 Bootstrapping: Building FwBuildTasks..." -ForegroundColor Yellow
Invoke-MSBuildStep `
	-Arguments @('Build/Src/FwBuildTasks/FwBuildTasks.csproj', '/t:Restore;Build', "/p:Configuration=$Configuration", "/p:Platform=$Platform") `
	-Description 'FwBuildTasks'

# Restore packages
Invoke-MSBuildStep `
	-Arguments @('Build/Orchestrator.proj', '/t:RestorePackages', "/p:Configuration=$Configuration", "/p:Platform=$Platform") `
	-Description 'RestorePackages'

# Build using traversal project
Invoke-MSBuildStep `
	-Arguments (@('FieldWorks.proj', "/p:Configuration=$Configuration", "/p:Platform=$Platform") + $MsBuildArgs + $(if ($BuildAdditionalApps) { '/p:BuildAdditionalApps=true' } else { @() })) `
	-Description "FieldWorks" `
	-LogPath $LogFile

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "Output: Output\$Configuration" -ForegroundColor Cyan