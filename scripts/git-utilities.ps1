Set-StrictMode -Version Latest

<#
Shared helpers for invoking git safely and reasoning about worktrees.
These helpers encapsulate the defensive behavior we need when paths or branches
get out of sync so callers stay tidy.
#>

function Invoke-GitSafe {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$true)][string[]]$Arguments,
    [switch]$Quiet,
    [switch]$CaptureOutput
  )

  $previousEap = $ErrorActionPreference
  $output = @()
  try {
    $ErrorActionPreference = 'Continue'
    $output = @( & git @Arguments 2>&1 )
    $exitCode = $LASTEXITCODE
  } finally {
    $ErrorActionPreference = $previousEap
  }

  if ($exitCode -ne 0) {
    $message = "git $($Arguments -join ' ') failed with exit code $exitCode"
    if ($output) {
      $message += "`n$output"
    }
    throw $message
  }

  if ($CaptureOutput) {
    return $output
  }

  if (-not $Quiet -and $output) {
    return $output
  }
}

function Get-GitWorktrees {
  [CmdletBinding()]
  param()

  $lines = Invoke-GitSafe @('worktree','list','--porcelain') -CaptureOutput
  $result = @()
  $current = @{}

  foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line.Split(' ',2)
    $key = $parts[0]
    $value = $parts[1]

    switch ($key) {
      'worktree' {
        if ($current.Keys.Count -gt 0) {
          $result += [PSCustomObject]$current
          $current = @{}
        }
        $rawPath = $value.Trim()
        $fullPath = [System.IO.Path]::GetFullPath($rawPath)
        $current = @{ RawPath = $rawPath; FullPath = $fullPath; Flags = @() }
      }
      'HEAD' { $current.Head = $value.Trim() }
      'branch' { $current.Branch = $value.Trim() }
      'detached' { $current.Detached = $true }
      'locked' { $current.Flags += 'locked' }
      'prunable' { $current.Flags += 'prunable' }
      default {
        # Preserve unknown keys for troubleshooting
        $current[$key] = $value.Trim()
      }
    }
  }

  if ($current.Keys.Count -gt 0) {
    $result += [PSCustomObject]$current
  }

  return $result
}

function Get-GitWorktreeForBranch {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$true)][string]$Branch
  )

  $branchRef = if ($Branch.StartsWith('refs/')) { $Branch } else { "refs/heads/$Branch" }
  return (Get-GitWorktrees | Where-Object { $_.Branch -eq $branchRef })
}

function Remove-GitWorktreePath {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$true)][string]$WorktreePath
  )

  try {
    Invoke-GitSafe @('worktree','remove','--force','--',$WorktreePath) -Quiet
    return
  } catch {
    $message = $_
    if (Detach-GitWorktreeMetadata -WorktreePath $WorktreePath -VerboseMode ($PSBoundParameters['Verbose'] -or $VerbosePreference -ne 'SilentlyContinue')) {
      try {
        Invoke-GitSafe @('worktree','prune','--expire=now') -Quiet
      } catch {}
      Write-Warning "git worktree remove failed for $WorktreePath; completed metadata-only detach instead. Original error: $message"
      return
    }

    try {
      Invoke-GitSafe @('worktree','prune','--expire=now') -Quiet
    } catch {}
    throw
  }
}

function Detach-GitWorktreeMetadata {
  param(
    [Parameter(Mandatory=$true)][string]$WorktreePath,
    [bool]$VerboseMode = $false
  )

  $gitPointer = Join-Path $WorktreePath '.git'
  if (-not (Test-Path -LiteralPath $gitPointer)) { return $false }

  try {
    $content = Get-Content -LiteralPath $gitPointer -Raw -ErrorAction Stop
  } catch {
    return $false
  }

  if ($content -notmatch 'gitdir:\s*(.+)') { return $false }
  $dirPath = $matches[1].Trim()
  $resolvedDir = $null
  try {
    if (Test-Path -LiteralPath $dirPath) {
      $resolvedDir = (Resolve-Path -LiteralPath $dirPath).Path
    } else {
      $resolvedDir = (Resolve-Path -LiteralPath ([System.IO.Path]::Combine($WorktreePath,$dirPath)) -ErrorAction SilentlyContinue).Path
    }
  } catch {
    $resolvedDir = $null
  }

  if ($resolvedDir -and (Test-Path -LiteralPath $resolvedDir)) {
    if ($VerboseMode) { Write-Warning "Falling back to metadata-only detach for $WorktreePath (removing $resolvedDir)" }
    Remove-Item -Recurse -Force $resolvedDir -ErrorAction SilentlyContinue
  }

  Remove-Item -LiteralPath $gitPointer -Force -ErrorAction SilentlyContinue
  return $true
}

function Prune-GitWorktreesNow {
  [CmdletBinding()]
  param()

  Invoke-GitSafe @('worktree','prune','--expire=now') -Quiet
}

function Test-GitBranchExists {
  [CmdletBinding()]
  param([Parameter(Mandatory=$true)][string]$Branch)

  $output = @(Invoke-GitSafe @('branch','--list',$Branch) -CaptureOutput)
  return ($output.Count -gt 0)
}

function Reset-GitBranchToRef {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$true)][string]$Branch,
    [Parameter(Mandatory=$true)][string]$Ref
  )

  # Ensures the agent branch is a mirror of the desired base ref.
  Invoke-GitSafe @('branch','-f',$Branch,$Ref) -Quiet
}
