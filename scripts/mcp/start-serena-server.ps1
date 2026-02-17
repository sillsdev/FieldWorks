<#
Starts the Serena MCP server using the repo-specific configuration.
Serena is run via uvx from the official GitHub repository.
#>

[CmdletBinding()]
param(
  [string]$ProjectPath = ".",  # Project directory (contains .serena/project.yml)
  [string]$BindHost = "127.0.0.1",  # Renamed from $Host to avoid conflict with PowerShell's automatic variable
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

# Serena must be run via uvx from GitHub (not from PyPI)
# See: https://github.com/oraios/serena#quick-start
$uvx = Resolve-Executable -Name 'uvx'
if (-not $uvx) {
  throw "uvx not found. Install uv first: https://docs.astral.sh/uv/getting-started/installation/"
}

# Resolve project directory (Serena expects directory, not the yml file)
$resolvedProject = Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop
$projectDir = $resolvedProject.Path

# If user passed the yml file path, get its parent directory
if ($projectDir -match '\.yml$') {
  $projectDir = Split-Path -Parent $projectDir
  # Go up one more level if we're in .serena folder
  if ($projectDir -match '[\\/]\.serena$') {
    $projectDir = Split-Path -Parent $projectDir
  }
}

# Verify .serena/project.yml exists
$projectYml = Join-Path $projectDir ".serena\project.yml"
if (-not (Test-Path -LiteralPath $projectYml)) {
  throw "Serena project config not found at: $projectYml"
}

if ([string]::IsNullOrWhiteSpace($BindHost)) { $BindHost = $null }

if (-not $env:SERENA_API_KEY) {
  Write-Warning "SERENA_API_KEY is not set. Remote Serena operations may fail if authentication is required."
}

# Build the uvx command: uvx --from git+https://github.com/oraios/serena serena start-mcp-server [args]
$invokeArgs = @(
  '--from', 'git+https://github.com/oraios/serena',
  'serena', 'start-mcp-server',
  '--project', $projectDir,
  '--context', 'ide-assistant'
)
if ($BindHost) { $invokeArgs += @('--host', $BindHost) }
if ($Port -gt 0) { $invokeArgs += @('--port', $Port) }
if ($ServerArgs -and $ServerArgs.Count -gt 0) { $invokeArgs += $ServerArgs }

Write-Host "Starting Serena MCP server for project at $projectDir..."
& $uvx @invokeArgs
$exitCode = $LASTEXITCODE
exit $exitCode
