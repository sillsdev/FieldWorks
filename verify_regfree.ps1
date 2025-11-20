[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = 'Stop'

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
		throw 'Visual Studio not found'
	}

	$vsInstallPath = & $vswhereCandidates[0] -latest -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -products * -property installationPath
	if (-not $vsInstallPath) {
		throw 'Visual Studio C++ tools not found'
	}

	$vsDevCmd = Join-Path -Path $vsInstallPath -ChildPath 'Common7\Tools\VsDevCmd.bat'
	if (-not (Test-Path $vsDevCmd)) {
		throw "Unable to locate VsDevCmd.bat under '$vsInstallPath'."
	}

	$arch = if ($RequestedPlatform -ieq 'x86') { 'x86' } else { 'amd64' }

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

	Write-Host '✓ Visual Studio environment initialized successfully' -ForegroundColor Green
}

Initialize-VsDevEnvironment -RequestedPlatform $Platform

# Set arch environment variable
if ($Platform) {
	switch ($Platform.ToLowerInvariant()) {
		'x86' { $env:arch = 'x86' }
		'x64' { $env:arch = 'x64' }
		default { $env:arch = $Platform }
	}
}

Write-Host "Running regFreeCpp target..."
msbuild Build\Src\NativeBuild\NativeBuild.csproj /t:regFreeCpp /p:Configuration=$Configuration /p:Platform=$Platform
