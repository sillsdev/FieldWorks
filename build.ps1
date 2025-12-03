<#
.SYNOPSIS
	Builds the FieldWorks repository using the MSBuild Traversal SDK.

.DESCRIPTION
	This script orchestrates the build process for FieldWorks. It handles:
	1. Auto-detecting worktrees and respawning inside Docker containers.
	2. Initializing the Visual Studio Developer Environment (if needed).
	3. Bootstrapping build tasks (FwBuildTasks).
	4. Restoring NuGet packages.
	5. Building the solution via FieldWorks.proj using MSBuild Traversal.

	When running in a worktree (e.g., fw-worktrees/agent-1), the script will
	automatically detect if a corresponding Docker container (fw-agent-1) is
	running and respawn the build inside the container for proper COM/registry
	isolation. Use -NoDocker to bypass this behavior.

.PARAMETER Configuration
	The build configuration (Debug or Release). Default is Debug.

.PARAMETER Platform
	The target platform. Only x64 is supported. Default is x64.

.PARAMETER Serial
	If set, disables parallel build execution (/m). Default is false (parallel enabled).

.PARAMETER BuildTests
	If set, includes test projects in the build. Default is false.

.PARAMETER RunTests
	If set, runs tests after building. Implies -BuildTests. Uses VSTest via Run-VsTests.ps1.

.PARAMETER TestFilter
	Optional VSTest filter expression (e.g., "TestCategory!=Slow"). Only used with -RunTests.

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

.PARAMETER NoDocker
	If set, bypasses automatic Docker container detection and runs locally.
	Use this when you want to build directly on the host even in a worktree.

.EXAMPLE
	.\build.ps1
	Builds Debug x64 in parallel with minimal logging.

.EXAMPLE
	.\build.ps1 -Configuration Release -BuildTests
	Builds Release x64 including test projects.

.EXAMPLE
	.\build.ps1 -RunTests
	Builds Debug x64 including test projects and runs all tests.

.EXAMPLE
	.\build.ps1 -Serial -Verbosity detailed
	Builds Debug x64 serially with detailed logging.

.EXAMPLE
	.\build.ps1 -NoDocker
	Builds locally even when in a worktree with an available container.

.NOTES
	FieldWorks is x64-only. The x86 platform is no longer supported.

	Worktree builds automatically use Docker containers when available for
	proper COM/registry isolation. The container must be started first using
	scripts/spin-up-agents.ps1.
#>
[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	# x64-only build (x86 is no longer supported)
	[ValidateSet('x64')]
	[string]$Platform = "x64",
	[switch]$Serial,
	[switch]$BuildTests,
	[switch]$RunTests,
	[string]$TestFilter,
	[switch]$BuildAdditionalApps,
	[string]$Verbosity = "minimal",
	[bool]$NodeReuse = $true,
	[string[]]$MsBuildArgs = @(),
	[string]$LogFile,
	[switch]$NoDocker
)

$ErrorActionPreference = 'Stop'

# --- 0. Docker Container Auto-Detection for Worktrees ---

function Test-InsideContainer {
	# Check if we're already running inside a container
	# The container has C:\BuildTools and specific environment markers
	return (Test-Path 'C:\BuildTools') -or ($env:FW_CONTAINER -eq 'true')
}

function Get-WorktreeAgentNumber {
	# Detect if we're in a worktree path like "fw-worktrees/agent-N" or "worktrees/agent-N"
	$currentPath = (Get-Location).Path
	if ($currentPath -match '[/\\](?:fw-)?worktrees[/\\]agent-(\d+)') {
		return [int]$Matches[1]
	}
	return $null
}

function Test-DockerContainerRunning {
	param([string]$ContainerName)

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

function Invoke-BuildInContainer {
	param(
		[int]$AgentNumber,
		[hashtable]$BuildParams
	)

	$containerName = "fw-agent-$AgentNumber"
	Write-Host "🐳 Detected worktree agent-$AgentNumber with running container '$containerName'" -ForegroundColor Cyan
	Write-Host "   Respawning build inside Docker container for COM/registry isolation..." -ForegroundColor Gray

	# Get the container's working directory (already set to the mapped worktree path)
	$containerWorkDir = docker inspect --format '{{.Config.WorkingDir}}' $containerName 2>$null
	if (-not $containerWorkDir) {
		# Fallback: compute the mapped path (C:\fw-mounts\<drive>\path\to\worktree)
		$currentPath = (Get-Location).Path
		$drive = $currentPath.Substring(0, 1).ToUpper()
		$pathWithoutDrive = $currentPath.Substring(3)  # Skip "C:\"
		$containerWorkDir = "C:\fw-mounts\$drive\$pathWithoutDrive"
	}

	# Build the command to run inside container
	$innerArgs = @()
	if ($BuildParams.Configuration -ne 'Debug') { $innerArgs += "-Configuration $($BuildParams.Configuration)" }
	if ($BuildParams.Serial) { $innerArgs += '-Serial' }
	if ($BuildParams.BuildTests) { $innerArgs += '-BuildTests' }
	if ($BuildParams.BuildAdditionalApps) { $innerArgs += '-BuildAdditionalApps' }
	if ($BuildParams.Verbosity -ne 'minimal') { $innerArgs += "-Verbosity $($BuildParams.Verbosity)" }
	if (-not $BuildParams.NodeReuse) { $innerArgs += '-NodeReuse:$false' }
	if ($BuildParams.MsBuildArgs.Count -gt 0) {
		$quotedArgs = $BuildParams.MsBuildArgs | ForEach-Object { "`"$_`"" }
		$innerArgs += "-MsBuildArgs @($($quotedArgs -join ','))"
	}
	# Always add -NoDocker to prevent infinite recursion
	$innerArgs += '-NoDocker'

	Write-Host "   Container working dir: $containerWorkDir" -ForegroundColor DarkGray
	Write-Host "   Container command: .\build.ps1 $($innerArgs -join ' ')" -ForegroundColor DarkGray
	Write-Host "" -ForegroundColor Gray

	# Clean container-local intermediate files to ensure fresh build state
	# (C:\Temp\Obj is container-local storage, separate from the mounted Obj/ folder)
	Write-Host "   Cleaning container intermediate files..." -ForegroundColor DarkGray
	docker exec $containerName powershell -NoProfile -Command "if (Test-Path 'C:\Temp\Obj') { Remove-Item -Recurse -Force 'C:\Temp\Obj' -ErrorAction SilentlyContinue }" 2>$null

	# Execute in container using VsDevShell.cmd to initialize VS environment (vcvarsall.bat x64)
	# VsDevShell.cmd runs vcvarsall.bat x64 and then executes the command passed as arguments
	# Use cmd /S /C to properly invoke the batch file, then PowerShell for the build

	# Execute in container with real-time output streaming
	# Use -i (interactive) without -t (tty) to allow output to flow through PowerShell
	# Direct invocation (not Start-Process) streams stdout/stderr in real-time

	$psCmd = "cd '$containerWorkDir'; .\build.ps1 $($innerArgs -join ' ')"
	& docker exec -i $containerName cmd /S /C "C:\scripts\VsDevShell.cmd powershell -NoProfile -Command `"$psCmd`""
	$exitCode = $LASTEXITCODE

	if ($exitCode -ne 0) {
		throw "Container build failed with exit code $exitCode"
	}

	Write-Host ""
	Write-Host "✓ Container build completed successfully" -ForegroundColor Green
	exit 0
}

# Check for worktree + container scenario (unless -NoDocker or already in container)
if (-not $NoDocker -and -not (Test-InsideContainer)) {
	$agentNum = Get-WorktreeAgentNumber
	if ($null -ne $agentNum) {
		$containerName = "fw-agent-$agentNum"
		if (Test-DockerContainerRunning -ContainerName $containerName) {
			# Respawn inside container
			$buildParams = @{
				Configuration = $Configuration
				Serial = $Serial.IsPresent
				BuildTests = $BuildTests.IsPresent
				BuildAdditionalApps = $BuildAdditionalApps.IsPresent
				Verbosity = $Verbosity
				NodeReuse = $NodeReuse
				MsBuildArgs = $MsBuildArgs
			}
			Invoke-BuildInContainer -AgentNumber $agentNum -BuildParams $buildParams
			# Invoke-BuildInContainer exits, so we won't reach here
		}
		else {
			Write-Host "⚠️  Worktree agent-$agentNum detected but container '$containerName' is not running" -ForegroundColor Yellow
			Write-Host "   Building locally (use 'scripts/spin-up-agents.ps1' to start containers)" -ForegroundColor Yellow
			Write-Host "   Or use -NoDocker to suppress this warning" -ForegroundColor DarkGray
			Write-Host ""
		}
	}
}

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

	# x64-only build (x86 is no longer supported)
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
# x64-only build (x86 is no longer supported)
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

# MSB3568: Suppress "Duplicate resource name" warnings from .resx files.
# These occur when the Visual Studio WinForms Designer saves duplicate metadata entries
# (e.g., >>controlName.Name, >>controlName.Type) due to merges or designer quirks.
# The duplicates are harmless at runtime (ResourceManager overwrites with identical values).
#
# To fix properly (removes duplicates from .resx files):
#   1. Open the affected form (e.g., PicturePropertiesDialog.cs) in Visual Studio Designer
#   2. Make a trivial change (move a control 1px and back)
#   3. Save the form - the Designer re-serializes and removes duplicates
#   OR manually delete duplicate <data name=">>..."> blocks from the .resx XML files.
#
# Suppressed in quiet/minimal verbosity to reduce build log noise.
if ($Verbosity -match '^(quiet|minimal|q|m)$') {
	$finalMsBuildArgs += "/p:NoWarn=MSB3568"
}

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

function Check-ConflictingProcesses {
	$conflicts = @("FieldWorks", "msbuild", "cl", "link", "nmake")
	$isContainer = Test-InsideContainer

	foreach ($name in $conflicts) {
		$processes = Get-Process -Name $name -ErrorAction SilentlyContinue
		if ($processes) {
			$count = @($processes).Count
			if ($isContainer) {
				# In a container, automatically kill stale processes (no user interaction)
				Write-Host "🧹 Cleaning up $count stale $name process(es) in container..." -ForegroundColor Yellow
				$processes | Stop-Process -Force -ErrorAction SilentlyContinue
				Start-Sleep -Milliseconds 500
			}
			else {
				# On host, ask user
				Write-Host "$name is currently running ($count process(es))." -ForegroundColor Yellow
				$confirmation = Read-Host "Do you want to close it? (Y/N)"
				if ($confirmation -match "^[Yy]") {
					Write-Host "Closing $name..." -ForegroundColor Yellow
					$processes | Stop-Process -Force
					Start-Sleep -Seconds 1
				} else {
					Write-Host "Continuing without closing $name." -ForegroundColor Yellow
				}
			}
		}
	}

	# In container, also kill any orphaned VBCSCompiler instances
	if ($isContainer) {
		$vbcs = Get-Process -Name "VBCSCompiler" -ErrorAction SilentlyContinue
		if ($vbcs) {
			Write-Host "🧹 Cleaning up VBCSCompiler process(es)..." -ForegroundColor Yellow
			$vbcs | Stop-Process -Force -ErrorAction SilentlyContinue
		}
	}
}

# --- 3. Execution ---

Check-ConflictingProcesses

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

# --- 4. Test Execution (Optional) ---
if ($RunTests) {
	Write-Host ""
	Write-Host "Running tests..." -ForegroundColor Cyan

	$testScript = Join-Path $PSScriptRoot "Build\Agent\Run-VsTests.ps1"
	if (-not (Test-Path $testScript)) {
		Write-Warning "Test runner script not found: $testScript"
		Write-Warning "Run tests manually with: .\Build\Agent\Run-VsTests.ps1 -All"
	} else {
		$testArgs = @{
			OutputDir = "Output\$Configuration"
			All = $true
		}
		if ($TestFilter) {
			$testArgs.Filter = $TestFilter
		}

		& $testScript @testArgs
		if ($LASTEXITCODE -ne 0) {
			Write-Warning "Some tests failed. Check output above for details."
		}
	}
}