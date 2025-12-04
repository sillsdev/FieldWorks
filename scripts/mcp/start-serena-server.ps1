<#
Starts the Serena MCP server using the repo-specific configuration.
Automatically locates the Serena CLI via `serena`, `uvx serena`, or `uv run serena`.
#>

[CmdletBinding()]
param(
  [string]$ProjectPath = ".serena/project.yml",
  [string]$Host = "127.0.0.1",
  [int]$Port = 0,
  [string[]]$ServerArgs = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-Executable {
  param([Parameter(Mandatory=$true)][string]$Name)

  $command = Get-Command $Name -ErrorAction SilentlyContinue
  if (-not $command) { return $null }
  return $command.Source
}

function Resolve-SerenaLauncher {
  $options = @(
    @{ Name = 'serena'; Prefix = @() },
    @{ Name = 'uvx'; Prefix = @('serena') },
    @{ Name = 'uv'; Prefix = @('run','serena') }
  )

  foreach ($option in $options) {
    $executable = Resolve-Executable -Name $option.Name
    if ($executable) {
      return @{ Command = $executable; Prefix = $option.Prefix }
    }
  }

  throw "Unable to locate the Serena CLI. Install Serena (pipx install serena-cli), or ensure uv/uvx is on PATH."
}

$resolvedProject = Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop
if ([string]::IsNullOrWhiteSpace($Host)) { $Host = $null }

if (-not $env:SERENA_API_KEY) {
  Write-Warning "SERENA_API_KEY is not set. Remote Serena operations may fail if authentication is required."
}

$launcher = Resolve-SerenaLauncher
$invokeArgs = @()
if ($launcher.Prefix) { $invokeArgs += $launcher.Prefix }
$invokeArgs += @('serve','--project',$resolvedProject.Path)
if ($Host) { $invokeArgs += @('--host',$Host) }
if ($Port -gt 0) { $invokeArgs += @('--port',$Port) }
if ($ServerArgs -and $ServerArgs.Count -gt 0) { $invokeArgs += $ServerArgs }

Write-Host "Starting Serena MCP server with project $($resolvedProject.Path)..."
& $launcher.Command @invokeArgs
$exitCode = $LASTEXITCODE
exit $exitCode
