[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][ValidateSet('Build','Clean','Test')]
  [string]$Action,
  [string]$Configuration = 'Debug',
  [string]$Platform = 'x64',
  [string]$WorktreePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $WorktreePath) {
  $WorktreePath = (Get-Location).Path
}

$WorktreePath = (Resolve-Path $WorktreePath).Path

$modulePath = Join-Path (Split-Path -Parent $PSCommandPath) 'AgentInfrastructure.psm1'
Import-Module $modulePath -Force -DisableNameChecking

$configPath = Join-Path $WorktreePath '.fw-agent\config.json'
if (-not (Test-Path -LiteralPath $configPath)) {
  throw "Agent config missing at $configPath. Re-run scripts/spin-up-agents.ps1 to regenerate."
}

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$containerName = $config.ContainerName
$containerPath = $config.ContainerPath
$solution = $config.SolutionRelPath
$useContainer = if ($null -ne $config.UseContainer) { $config.UseContainer } else { $true }
$nuGetCache = $config.NuGetCachePath

if (-not $containerPath -or -not $solution) {
  throw "Agent config missing required fields (ContainerPath, SolutionRelPath)."
}

if ($useContainer -and -not $containerName) {
  throw "Agent config specifies UseContainer=true but ContainerName is missing."
}

function Invoke-ContainerMsBuild {
  param(
    [string]$Command,
    [string]$ContainerName
  )

  $args = @('exec',$ContainerName,'powershell','-NoProfile','-c',$Command)
  Invoke-DockerSafe $args | ForEach-Object { Write-Output $_ }
}

function Invoke-LocalMsBuild {
  param(
    [string]$Command
  )

  if ($nuGetCache) {
    $env:NUGET_PACKAGES = $nuGetCache
    Write-Host "Using isolated NuGet cache: $nuGetCache" -ForegroundColor Gray
  }

  # Try to find msbuild if not in path
  $msbuild = "msbuild"
  if (-not (Get-Command msbuild -ErrorAction SilentlyContinue)) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
      $vsPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
      if ($vsPath) { $msbuild = $vsPath }
    }
  }

  # Execute command locally using Start-Process to handle paths with spaces correctly
  $process = Start-Process -FilePath $msbuild -ArgumentList $Command -NoNewWindow -Wait -PassThru
  if ($process.ExitCode -ne 0) {
    throw "MSBuild failed with exit code $($process.ExitCode)"
  }
}

switch ($Action) {
  'Build' {
    $cmd = "`"$containerPath\\$solution`" /m /p:Configuration=$Configuration /p:Platform=$Platform"
    if ($useContainer) {
      Invoke-ContainerMsBuild -Command "msbuild $cmd" -ContainerName $containerName
    } else {
      Invoke-LocalMsBuild -Command $cmd
    }
  }
  'Clean' {
    $cmd = "`"$containerPath\\$solution`" /t:Clean /m /p:Configuration=$Configuration /p:Platform=$Platform"
    if ($useContainer) {
      Invoke-ContainerMsBuild -Command "msbuild $cmd" -ContainerName $containerName
    } else {
      Invoke-LocalMsBuild -Command $cmd
    }
  }
  'Test' {
    # Container test script uses VSINSTALLDIR env var (set by Post-Install-Setup.ps1) or falls back to hardcoded path
    $testScript = @"
`$testDlls = Get-ChildItem -Recurse -Include *Tests.dll,*Test.dll,*Spec.dll -Path '$containerPath' -ErrorAction SilentlyContinue | Where-Object { `$_.FullName -match '\\bin\\(Debug|Release)\\' }
if (`$testDlls) {
  `$vstestPath = if (`$env:VSINSTALLDIR) { Join-Path `$env:VSINSTALLDIR 'Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' } else { 'C:\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' }
  & `$vstestPath @(`$testDlls.FullName)
} else {
  Write-Host 'No test DLLs found.'
}
"@
    if ($useContainer) {
      Invoke-DockerSafe @('exec',$containerName,'powershell','-NoProfile','-c',$testScript) | ForEach-Object { Write-Output $_ }
    } else {
      # Local test execution
      $vstest = "vstest.console.exe"
      if (-not (Get-Command vstest.console.exe -ErrorAction SilentlyContinue)) {
         $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
         if (Test-Path $vswhere) {
            $vsPath = & $vswhere -latest -requires Microsoft.VisualStudio.Component.TestTools.Core -find **\vstest.console.exe
            if ($vsPath) { $vstest = $vsPath }
         }
      }

      if (-not (Test-Path $vstest) -and -not (Get-Command $vstest -ErrorAction SilentlyContinue)) {
          # Fallback to dotnet test if vstest is not found?
          # For now, just warn or throw.
          Write-Warning "vstest.console.exe not found. Attempting to use 'dotnet test' might be an option in future."
          throw "vstest.console.exe not found. Please install Visual Studio Test Tools."
      }

      $testDlls = Get-ChildItem -Recurse -Include *Tests.dll,*Test.dll,*Spec.dll -Path $containerPath -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match '\\bin\\(Debug|Release)\\' }
      if ($testDlls) {
        $process = Start-Process -FilePath $vstest -ArgumentList $testDlls.FullName -NoNewWindow -Wait -PassThru
        if ($process.ExitCode -ne 0) {
            throw "Tests failed with exit code $($process.ExitCode)"
        }
      } else {
        Write-Host 'No test DLLs found.'
      }
    }
  }
  default {
    throw "Unsupported action: $Action"
  }
}
