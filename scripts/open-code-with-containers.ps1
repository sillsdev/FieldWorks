param(
  [Parameter(Mandatory=$true)][string]$WorktreePath,
  [Parameter(Mandatory=$true)][string]$ContainerName,
  [string]$WorkspaceFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command code -ErrorAction SilentlyContinue)) {
  throw "VS Code command-line launcher 'code' not found in PATH. Install the CLI (View -> Command Palette -> 'Shell Command: Install 'code' command') and retry."
}

$target = if ($WorkspaceFile -and (Test-Path $WorkspaceFile)) { $WorkspaceFile } else { $WorktreePath }

$quotedTarget = '"{0}"' -f $target
Start-Process "cmd.exe" "/c set FW_AGENT_CONTAINER=$ContainerName && code -n $quotedTarget"