[CmdletBinding()]

param(
	[string]$Targets = "all",
	[string]$Configuration = "Debug",
	[string]$Platform = "x64",
	[string[]]$MsBuildArgs = @(),
	[string]$LogFile,
	[switch]$UseTraversal  # Use new MSBuild Traversal SDK approach
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

	if ($env:OS -ne 'Windows_NT' -or $env:VCINSTALLDIR) {
		return
	}

	Write-Host 'Locating Visual Studio build tools...' -ForegroundColor Yellow
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
		throw 'Unable to locate vswhere.exe. Run this script from a Developer Command Prompt or install the Visual Studio Build Tools.'
	}

	$vsInstallPath = & $vswhereCandidates[0] -latest -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -products * -property installationPath
	if (-not $vsInstallPath) {
		throw 'vswhere.exe could not find a Visual Studio installation with the VC toolset. Install the required workloads or run from a configured developer prompt.'
	}

	$vsDevCmd = Join-Path -Path $vsInstallPath -ChildPath 'Common7\Tools\VsDevCmd.bat'
	if (-not (Test-Path $vsDevCmd)) {
		throw "Unable to locate VsDevCmd.bat under '$vsInstallPath'."
	}

	$arch = if ($RequestedPlatform -ieq 'x86') { 'x86' } else { 'x64' }
	Write-Host "Initializing Visual Studio environment (-arch=$arch)..." -ForegroundColor Yellow
	$cmdArgs = "`"$vsDevCmd`" -no_logo -arch=$arch -host_arch=$arch && set"
	$envOutput = & cmd.exe /c $cmdArgs
	if (-not $envOutput) {
		throw 'Failed to initialize the Visual Studio developer environment.'
	}

	foreach ($line in $envOutput) {
		$parts = $line -split '=', 2
		if ($parts.Length -eq 2 -and $parts[0]) {
			Set-Item -Path "Env:$($parts[0])" -Value $parts[1]
		}
	}

	if (-not $env:VCINSTALLDIR) {
		throw 'VSDevCmd completed but VCINSTALLDIR is still missing; ensure Visual Studio C++ tools are installed.'
	}
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

if ($UseTraversal) {
	Write-Host "Using MSBuild Traversal SDK approach (dirs.proj)..." -ForegroundColor Cyan

	# Restore packages first
	Invoke-MSBuildStep `
		-Arguments @('Build/FieldWorks.proj', '/t:RestorePackages', "/p:Configuration=$Configuration", "/p:Platform=$Platform") `
		-Description 'RestorePackages'

	# Build using traversal project
	Invoke-MSBuildStep `
		-Arguments (@('dirs.proj', "/p:Configuration=$Configuration", "/p:Platform=$Platform") + $MsBuildArgs) `
		-Description "Traversal build" `
		-LogPath $LogFile
}
else {
	Write-Host "Using legacy build approach (FieldWorks.proj)..." -ForegroundColor Yellow

	Invoke-MSBuildStep `
		-Arguments @('Build/Src/FwBuildTasks/FwBuildTasks.csproj', '/t:Restore;Build', "/p:Configuration=$Configuration", '/p:Platform=AnyCPU') `
		-Description 'FwBuildTasks build'

	Invoke-MSBuildStep `
		-Arguments @('Build/FieldWorks.proj', '/t:RestorePackages', "/p:Configuration=$Configuration", "/p:Platform=$Platform") `
		-Description 'RestorePackages'

	# CheckDevelopmentPropertiesFile is optional and may fail if MSBuild.Extension.Pack isn't installed
	try {
		Invoke-MSBuildStep `
			-Arguments @('Build/FieldWorks.proj', '/t:CheckDevelopmentPropertiesFile', "/p:Configuration=$Configuration", "/p:Platform=$Platform") `
			-Description 'CheckDevelopmentPropertiesFile'
	}
	catch {
		Write-Host "Warning: CheckDevelopmentPropertiesFile failed (this is optional): $_" -ForegroundColor Yellow
	}

	Invoke-MSBuildStep `
		-Arguments @('Build/FieldWorks.proj', '/t:refreshTargets', "/p:Configuration=$Configuration", "/p:Platform=$Platform") `
		-Description 'refreshTargets'

	Invoke-MSBuildStep `
		-Arguments (@('Build/FieldWorks.proj', "/t:$Targets", "/p:Configuration=$Configuration", "/p:Platform=$Platform") + $MsBuildArgs) `
		-Description "FieldWorks target '$Targets'" `
		-LogPath $LogFile
}
