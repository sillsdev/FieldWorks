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

if (-not $containerName -or -not $containerPath -or -not $solution) {
  throw "Agent config missing required fields (ContainerName, ContainerPath, SolutionRelPath)."
}

function Invoke-ContainerMsBuild {
  param(
    [string]$Command,
    [string]$ContainerName
  )

  $args = @('exec',$ContainerName,'powershell','-NoProfile','-c',$Command)
  Invoke-DockerSafe $args | ForEach-Object { Write-Output $_ }
}

switch ($Action) {
  'Build' {
    $cmd = "msbuild `"$containerPath\\$solution`" /m /p:Configuration=$Configuration /p:Platform=$Platform"
    Invoke-ContainerMsBuild -Command $cmd -ContainerName $containerName
  }
  'Clean' {
    $cmd = "msbuild `"$containerPath\\$solution`" /t:Clean /m /p:Configuration=$Configuration /p:Platform=$Platform"
    Invoke-ContainerMsBuild -Command $cmd -ContainerName $containerName
  }
  'Test' {
    $testScript = @"
$testDlls = Get-ChildItem -Recurse -Include *Tests.dll,*Test.dll,*Spec.dll -Path '$containerPath' -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match '\\bin\\(Debug|Release)\\' }
if ($testDlls) {
  & 'C:\\BuildTools\\Common7\\IDE\\CommonExtensions\\Microsoft\\TestWindow\\vstest.console.exe' @($testDlls.FullName)
} else {
  Write-Host 'No test DLLs found.'
}
"@
    Invoke-DockerSafe @('exec',$containerName,'powershell','-NoProfile','-c',$testScript) | ForEach-Object { Write-Output $_ }
  }
  default {
    throw "Unsupported action: $Action"
  }
}
