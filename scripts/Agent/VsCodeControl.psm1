Set-StrictMode -Version Latest

function Resolve-WorkspacePath {
  param([string]$WorkspacePath)
  if (-not $WorkspacePath) { return $null }
  try {
    return (Resolve-Path -LiteralPath $WorkspacePath -ErrorAction Stop).Path
  } catch {
    try {
      return [System.IO.Path]::GetFullPath($WorkspacePath)
    } catch {
      return $WorkspacePath
    }
  }
}

function Get-CodeProcesses {
  try {
    return @(Get-CimInstance -ClassName Win32_Process -Filter "Name = 'Code.exe'" -ErrorAction Stop)
  } catch {
    return @()
  }
}

function Get-CodeStatusOutput {
  try {
    return @(code --status 2>$null)
  } catch {
    return @()
  }
}

function Get-VSCodeProcessIndex {
  param([System.Collections.IEnumerable]$Processes)

  $index = @{}
  if (-not $Processes) { return $index }
  foreach ($proc in $Processes) {
    if ($null -eq $proc -or -not $proc.ProcessId) { continue }
    $index[[int]$proc.ProcessId] = $proc
  }
  return $index
}

function Get-VSCodeRootProcessId {
  param(
    [int]$ProcessId,
    [hashtable]$ProcessIndex
  )

  if ($ProcessId -le 0) { return $ProcessId }

  if (-not $ProcessIndex -or $ProcessIndex.Count -eq 0) {
    $ProcessIndex = Get-VSCodeProcessIndex -Processes (Get-CodeProcesses)
  }

  $current = [int]$ProcessId
  while ($true) {
    $entry = $ProcessIndex[[int]$current]
    if (-not $entry) { break }
    $parentId = [int]$entry.ParentProcessId
    if ($parentId -le 0) { break }
    $parentEntry = $ProcessIndex[[int]$parentId]
    if (-not $parentEntry) { break }
    $current = $parentId
  }

  return $current
}

function Get-VSCodePidsForWorkspaces {
  param([string[]]$WorkspacePaths)

  $resolvedTargets = @()
  foreach ($path in $WorkspacePaths) {
    $resolved = Resolve-WorkspacePath -WorkspacePath $path
    if ($resolved) { $resolvedTargets += $resolved.ToLowerInvariant() }
  }

  if ($resolvedTargets.Count -eq 0) { return @() }

  $matches = @()
  $codeProcesses = Get-CodeProcesses
  if (-not $codeProcesses -or $codeProcesses.Count -eq 0) { return @() }
  $processIndex = Get-VSCodeProcessIndex -Processes $codeProcesses
  $rootSeen = New-Object System.Collections.Generic.HashSet[int]

  foreach ($proc in $codeProcesses) {
    $cmd = $proc.CommandLine
    if (-not $cmd) { continue }
    $cmdLower = $cmd.ToLowerInvariant()
    foreach ($target in $resolvedTargets) {
      if ($cmdLower.Contains($target)) {
        $rootPid = Get-VSCodeRootProcessId -ProcessId $proc.ProcessId -ProcessIndex $processIndex
        if (-not $rootSeen.Add([int]$rootPid)) { break }
        $matches += [pscustomobject]@{
          RootProcessId = $rootPid
          CommandLine = $cmd
        }
        break
      }
    }
  }
  return @($matches)
}

function Get-VSCodeUriArgument {
  param([Parameter(Mandatory=$true)][string]$WorkspacePath)

  $resolved = Resolve-WorkspacePath -WorkspacePath $WorkspacePath
  if (-not $resolved) { throw "Workspace path was not provided." }
  if (-not (Test-Path -LiteralPath $resolved)) {
    throw "Workspace path '$resolved' does not exist."
  }

  $pathType = if (Test-Path -LiteralPath $resolved -PathType Container) { 'Container' } else { 'Leaf' }
  $uri = [System.Uri]::new($resolved)
  $argumentName = if ($pathType -eq 'Container') { '--folder-uri' } else { '--file-uri' }

  return [pscustomobject]@{
    ArgumentName = $argumentName
    Uri = $uri.AbsoluteUri
  }
}

function Invoke-VSCodeCommandForWorkspace {
  param(
    [Parameter(Mandatory=$true)][string]$WorkspacePath,
    [Parameter(Mandatory=$true)][string]$CommandId,
    [switch]$ReuseWindow
  )

  $uriArg = Get-VSCodeUriArgument -WorkspacePath $WorkspacePath
  if (-not (Get-Command code -ErrorAction SilentlyContinue)) {
    throw "VS Code CLI ('code') not found in PATH. Install the shell command from VS Code before continuing."
  }

  $args = @($uriArg.ArgumentName,$uriArg.Uri,'--command',$CommandId)
  if ($ReuseWindow) { $args = @('--reuse-window') + $args }

  Write-Host "Running: code $($args -join ' ')"
  $output = & code @args 2>&1
  $exitCode = $LASTEXITCODE
  if ($exitCode -ne 0) {
    Write-Warning ("VS Code CLI exited with code {0}. Output: {1}" -f $exitCode, ($output -join [Environment]::NewLine))
    return $false
  }
  return $true
}

function Test-VSCodeWorkspaceOpen {
  param([Parameter(Mandatory=$true)][string]$WorkspacePath)
  $matches = @(Get-VSCodePidsForWorkspaces -WorkspacePaths @($WorkspacePath))
  return $matches.Length -gt 0
}

function Wait-VSCodeWorkspaceState {
  param(
    [Parameter(Mandatory=$true)][string]$WorkspacePath,
    [bool]$ShouldBeOpen,
    [int]$MaxWaitSeconds = 15
  )

  $resolved = Resolve-WorkspacePath -WorkspacePath $WorkspacePath
  $deadline = [DateTime]::UtcNow.AddSeconds([Math]::Max(1,$MaxWaitSeconds))
  while ($true) {
    $isOpen = Test-VSCodeWorkspaceOpen -WorkspacePath $resolved
    if ($isOpen -eq $ShouldBeOpen) { return $true }
    if ([DateTime]::UtcNow -ge $deadline) { return $false }
    Start-Sleep -Milliseconds 500
  }
}

function Close-VSCodeWorkspaces {
  param(
    [string[]]$WorkspacePaths,
    [int]$MaxWaitSeconds = 15
  )

  if (-not $WorkspacePaths -or $WorkspacePaths.Count -eq 0) { return }

  foreach ($workspace in $WorkspacePaths) {
    $resolved = Resolve-WorkspacePath -WorkspacePath $workspace
    if (-not $resolved) { continue }
    if (-not (Test-Path -LiteralPath $resolved)) {
      Write-Host "Skipping VS Code close request because '$resolved' no longer exists."
      continue
    }

    Write-Host "Requesting VS Code to close window for '$resolved' via workbench.action.closeWindow"
    $requested = Invoke-VSCodeCommandForWorkspace -WorkspacePath $resolved -CommandId 'workbench.action.closeWindow' -ReuseWindow
    if (-not $requested) {
      Write-Warning "VS Code command invocation failed for '$resolved'."
      continue
    }

    if (Wait-VSCodeWorkspaceState -WorkspacePath $resolved -ShouldBeOpen:$false -MaxWaitSeconds $MaxWaitSeconds) {
      Write-Host "Confirmed VS Code closed '$resolved'."
    } else {
      Write-Warning "Timed out waiting for VS Code to close '$resolved'."
    }
  }
}

function Open-AgentVsCodeWindow {
  param(
    [int]$Index,
    [Parameter(Mandatory=$true)][string]$AgentPath,
    [Parameter(Mandatory=$true)][string]$ContainerName,
    [string]$WorkspaceFile
  )

  $workspaceOverride = $null
  if ($WorkspaceFile -and (Test-Path $WorkspaceFile)) {
    $workspaceOverride = (Resolve-Path $WorkspaceFile).Path
  } else {
    $candidate = Join-Path $AgentPath "agent-$Index.code-workspace"
    if (Test-Path $candidate) {
      $workspaceOverride = (Resolve-Path $candidate).Path
    }
  }

  $launcherDir = Split-Path $PSScriptRoot -Parent
  $launcher = Join-Path $launcherDir 'open-code-with-containers.ps1'
  $resolvedLauncher = (Resolve-Path $launcher).Path
  $invokeArgs = @{
    WorktreePath = $AgentPath
    ContainerName = $ContainerName
  }
  if ($workspaceOverride) {
    $invokeArgs.WorkspaceFile = $workspaceOverride
  }
  & $resolvedLauncher @invokeArgs
}

Export-ModuleMember -Function Resolve-WorkspacePath,Get-CodeProcesses,Get-CodeStatusOutput,Get-VSCodeProcessIndex,Get-VSCodeRootProcessId,Get-VSCodePidsForWorkspaces,Get-VSCodeUriArgument,Invoke-VSCodeCommandForWorkspace,Test-VSCodeWorkspaceOpen,Wait-VSCodeWorkspaceState,Close-VSCodeWorkspaces,Open-AgentVsCodeWindow
