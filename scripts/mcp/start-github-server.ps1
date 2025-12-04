<#
Starts the Model Context Protocol GitHub server for this repository.
Relies on the official @modelcontextprotocol/server-github package.
#>

[CmdletBinding()]
param(
  [string]$RepoSlug = "sillsdev/FieldWorks",
  [string]$PackageName = "@modelcontextprotocol/server-github",
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

if ([string]::IsNullOrWhiteSpace($RepoSlug) -or $RepoSlug -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
  throw "RepoSlug must be in the form owner/name."
}

if ([string]::IsNullOrWhiteSpace($PackageName)) {
  throw "PackageName cannot be empty."
}

if ([string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
  throw "GITHUB_TOKEN is not set. Create a personal access token with repo scope and export it before starting the MCP server."
}

$launcher = Resolve-Executable -Name 'npx'
if (-not $launcher) {
  throw "npx was not found on PATH. Install Node.js 18+ and ensure npx is available."
}

$arguments = @('--yes',$PackageName,'--repo',$RepoSlug)
if ($ServerArgs -and $ServerArgs.Count -gt 0) {
  $arguments += $ServerArgs
}

Write-Host "Starting GitHub MCP server for $RepoSlug using $PackageName..."
& $launcher @arguments
$exitCode = $LASTEXITCODE
exit $exitCode
