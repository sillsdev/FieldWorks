Set-StrictMode -Version Latest

function Assert-Tool {
  param(
    [Parameter(Mandatory=$true)][string]$Name,
    [string]$CheckArgs = "--version"
  )

  try {
    & $Name $CheckArgs | Out-Null
  } catch {
    throw "Required tool '$Name' not found in PATH."
  }
}

function Invoke-DockerSafe {
  param(
    [Parameter(Mandatory=$true)][string[]]$Arguments,
    [switch]$Quiet,
    [switch]$CaptureOutput
  )

  $previousEap = $ErrorActionPreference
  $output = @()
  try {
    $ErrorActionPreference = 'Continue'
    $output = @( & docker @Arguments 2>&1 )
    $exitCode = $LASTEXITCODE
  } finally {
    $ErrorActionPreference = $previousEap
  }

  if ($exitCode -ne 0) {
    $message = "docker $($Arguments -join ' ') failed with exit code $exitCode"
    if ($output) { $message += "`n$output" }
    throw $message
  }

  if ($CaptureOutput) { return $output }
  if (-not $Quiet -and $output) { return $output }
}

function Get-GitDirectory {
  param([Parameter(Mandatory=$true)][string]$Path)

  $gitPath = Join-Path $Path ".git"
  if (-not (Test-Path -LiteralPath $gitPath -PathType Any)) {
    throw "No .git directory found at $Path. Ensure -RepoRoot points to the primary FieldWorks clone."
  }

  if (Test-Path -LiteralPath $gitPath -PathType Container) {
    return (Resolve-Path -LiteralPath $gitPath -ErrorAction Stop).Path
  }

  $gitContent = Get-Content -LiteralPath $gitPath -Raw -Force
  if ($gitContent -match 'gitdir:\s*(.+)') {
    $gitDir = $matches[1].Trim()
    if (-not [System.IO.Path]::IsPathRooted($gitDir)) {
      $gitDir = Join-Path $Path $gitDir
    }
    return (Resolve-Path $gitDir).Path
  }

  throw "Unable to resolve gitdir from $gitPath."
}

function Ensure-GitExcludePatterns {
  param(
    [Parameter(Mandatory=$true)][string]$GitDir,
    [Parameter(Mandatory=$true)][string[]]$Patterns
  )

  $excludeFile = Join-Path $GitDir "info\\exclude"
  $excludeDir = Split-Path $excludeFile -Parent

  if (-not (Test-Path $excludeDir)) {
    New-Item -ItemType Directory -Force -Path $excludeDir | Out-Null
  }
  if (-not (Test-Path $excludeFile)) {
    New-Item -ItemType File -Force -Path $excludeFile | Out-Null
  }

  $content = Get-Content $excludeFile -ErrorAction SilentlyContinue
  foreach ($pattern in $Patterns) {
    if (-not $pattern) { continue }
    if ($content -notcontains $pattern) {
      Add-Content -Path $excludeFile -Value $pattern
      Write-Host "Added '$pattern' to $excludeFile"
    }
  }
}

function Get-DriveRoot {
  param([Parameter(Mandatory=$true)][string]$Path)

  $full = [System.IO.Path]::GetFullPath($Path)
  return [System.IO.Path]::GetPathRoot($full)
}

function Get-RelativePath {
  param(
    [Parameter(Mandatory=$true)][string]$From,
    [Parameter(Mandatory=$true)][string]$To
  )

  $fromFull = (Resolve-Path -LiteralPath $From).Path
  $toFull = (Resolve-Path -LiteralPath $To).Path
  $fromDrive = Get-DriveRoot $fromFull
  $toDrive = Get-DriveRoot $toFull

  if ($fromDrive -ne $toDrive) { return $null }

  if (-not $fromFull.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
    $fromFull += [System.IO.Path]::DirectorySeparatorChar
  }

  $fromUri = New-Object System.Uri($fromFull)
  $toUri = New-Object System.Uri($toFull)
  $relativeUri = $fromUri.MakeRelativeUri($toUri)
  $relative = [Uri]::UnescapeDataString($relativeUri.ToString()).Replace('/',[System.IO.Path]::DirectorySeparatorChar)
  return $relative
}

function Ensure-RelativeGitDir {
  param(
    [Parameter(Mandatory=$true)][string]$WorktreePath,
    [Parameter(Mandatory=$true)][string]$RepoRoot,
    [Parameter(Mandatory=$true)][string]$WorktreeName
  )

  $gitFile = Join-Path $WorktreePath ".git"
  if (-not (Test-Path -LiteralPath $gitFile)) { return }

  $target = Join-Path (Join-Path $RepoRoot ".git\worktrees") $WorktreeName
  if (-not (Test-Path -LiteralPath $target)) { return }

  $relative = Get-RelativePath -From $WorktreePath -To $target
  if (-not $relative) {
    throw "RepoRoot ($RepoRoot) and worktree ($WorktreePath) must reside on the same drive for container usage. Move them to a common drive or set FW_WORKTREES_ROOT accordingly."
  }

  $normalized = $relative.Replace('\\','/')
  $desired = "gitdir: $normalized"
  $current = (Get-Content -Raw -LiteralPath $gitFile -ErrorAction SilentlyContinue).Trim()
  if ($current -ne $desired) {
    Set-Content -Path $gitFile -Value $desired -Encoding ASCII
  }
}

function Assert-VolumeSupportsBindMount {
  param([Parameter(Mandatory=$true)][string]$Path)

  $drive = Get-DriveRoot $Path
  if (-not $drive) { return }
  $driveInfo = New-Object System.IO.DriveInfo($drive)
  $fs = $driveInfo.DriveFormat
  if ($fs -and $fs -ne 'NTFS') {
    throw "Path '$Path' resides on a $fs volume ($drive). Windows containers can only bind-mount NTFS drives. Move this folder to an NTFS drive (e.g., C:) or configure FW_WORKTREES_ROOT on an NTFS volume before running spin-up."
  }
}

function Get-DriveIdentifier {
  param([Parameter(Mandatory=$true)][string]$DriveRoot)
  return ($DriveRoot.Substring(0,1).ToUpperInvariant())
}

function Convert-ToContainerPath {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][hashtable]$DriveMappings
  )

  $drive = Get-DriveRoot $Path
  if (-not $drive) { return $Path }
  $containerRoot = $DriveMappings[$drive]
  if (-not $containerRoot) { return $Path }
  $relative = $Path.Substring($drive.Length)
  if ([string]::IsNullOrWhiteSpace($relative)) {
    return $containerRoot
  }
  return Join-Path $containerRoot $relative
}

function Get-DockerInspectObject {
  param([Parameter(Mandatory=$true)][string]$Name)

  $output = Invoke-DockerSafe @('inspect',$Name) -CaptureOutput
  if (-not $output) { return $null }
  $json = ($output -join "`n")
  $parsed = $json | ConvertFrom-Json
  if ($parsed -is [System.Array]) {
    return $parsed[0]
  }
  return $parsed
}

function Set-FileContentIfChanged {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Content,
    [string]$Encoding = 'utf8'
  )

  $existing = $null
  if (Test-Path -LiteralPath $Path) {
    $existing = Get-Content -LiteralPath $Path -Raw -ErrorAction SilentlyContinue
  }

  if ($existing -eq $Content) {
    return $false
  }

  $dir = Split-Path $Path -Parent
  if ($dir -and -not (Test-Path $dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
  }

  Set-Content -Path $Path -Value $Content -Encoding $Encoding
  return $true
}

Export-ModuleMember -Function Assert-Tool,Invoke-DockerSafe,Get-GitDirectory,Ensure-GitExcludePatterns,Get-DriveRoot,Get-RelativePath,Ensure-RelativeGitDir,Assert-VolumeSupportsBindMount,Get-DriveIdentifier,Convert-ToContainerPath,Get-DockerInspectObject,Set-FileContentIfChanged
