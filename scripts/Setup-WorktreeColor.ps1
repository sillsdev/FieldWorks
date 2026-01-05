<#
.SYNOPSIS
    Sets a unique window color for the current VS Code workspace/worktree.
.DESCRIPTION
    Chooses a color from a fixed 8-color palette based on the workspace path hash.

    Uses the VS Code workspace (.code-workspace) paradigm for worktree overrides:
    - Reads the tracked base workspace file: fw.code-workspace
    - Writes a worktree-local workspace file: fw.worktree.code-workspace (git-ignored)

    - If in a Git Worktree: Applies colors to Title Bar, Status Bar, and Activity Bar.
    - If in Main Repo: Removes these color customizations.
    Intended to be run as a "folderOpen" task in VS Code.
#>

$ErrorActionPreference = "Stop"

# Get the repo root (parent of scripts/)
$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$worktreeWorkspacePath = Join-Path $repoRoot "fw.worktree.code-workspace"
$gitPath = Join-Path $repoRoot ".git"

# Check if we are in a worktree or main repo
$isWorktree = $false
if (Test-Path $gitPath -PathType Leaf) {
    # .git is a file in worktrees (pointing to the main repo gitdir)
    $isWorktree = $true
} elseif (Test-Path $gitPath -PathType Container) {
    # .git is a directory in the main repo
    $isWorktree = $false
} else {
    Write-Warning "No .git found at $gitPath. Assuming not a worktree."
    $isWorktree = $false
}

function Ensure-PSObjectProperty($obj, $name) {
    if (-not $obj.PSObject.Properties[$name]) {
        $obj | Add-Member -MemberType NoteProperty -Name $name -Value (New-Object PSObject)
    }
}

function Ensure-PSObject($val) {
    if ($null -eq $val) {
        return (New-Object PSObject)
    }
    if ($val -is [System.Collections.Hashtable]) {
        $obj = New-Object PSObject
        foreach ($key in $val.Keys) {
            $obj | Add-Member -MemberType NoteProperty -Name $key -Value $val[$key]
        }
        return $obj
    }
    return $val
}

function Parse-HexRgb($hex) {
    $h = $hex.Trim()
    if ($h.StartsWith("#")) {
        $h = $h.Substring(1)
    }
    if ($h.Length -ne 6) {
        throw "Expected 6-digit hex color, got '$hex'"
    }
    $r = [Convert]::ToInt32($h.Substring(0, 2), 16)
    $g = [Convert]::ToInt32($h.Substring(2, 2), 16)
    $b = [Convert]::ToInt32($h.Substring(4, 2), 16)
    return @{ r = $r; g = $g; b = $b }
}

# Base workspace configuration (embedded)
$baseWorkspace = New-Object PSObject
$baseWorkspace | Add-Member -MemberType NoteProperty -Name "folders" -Value @(@{ path = "." })
$baseWorkspace | Add-Member -MemberType NoteProperty -Name "settings" -Value (New-Object PSObject)

$baseWorkspace = Ensure-PSObject $baseWorkspace
Ensure-PSObjectProperty $baseWorkspace "settings"
$baseSettings = Ensure-PSObject $baseWorkspace.settings
$baseWorkspace.settings = $baseSettings

# Define the keys we manage
$managedKeys = @(
    "titleBar.activeBackground", "titleBar.activeForeground",
    "titleBar.inactiveBackground", "titleBar.inactiveForeground",
    "statusBar.background", "statusBar.foreground",
    "activityBar.background", "activityBar.foreground",
    "activityBar.inactiveForeground"
)

if ($isWorktree) {
    # --- APPLY COLORS ---

    # Choose color from a fixed palette (Loading.io: lloyds)
    # Source: https://loading.io/color/feature/lloyds
    # Note: The palette on the page has 9 colors; we use the 8 brand colors and exclude the neutral "#1e1e1e".
    $palette = @(
        "#d81f2a", # red
        "#ff9900", # orange
        "#e0d86e", # yellow
        "#9ea900", # green
        "#6ec9e0", # light blue
        "#007ea3", # blue
        "#9e4770", # magenta
        "#631d76"  # purple
    )

    $md5 = [System.Security.Cryptography.MD5]::Create()
    $hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($repoRoot))
    $colorHex = $palette[($hashBytes[0] % $palette.Length)]
    $rgb = Parse-HexRgb $colorHex
    $r = $rgb.r; $g = $rgb.g; $b = $rgb.b

    # Determine text color (contrast)
    $luminance = (0.299 * $r + 0.587 * $g + 0.114 * $b)
    $textColor = if ($luminance -gt 128) { "#000000" } else { "#FFFFFF" }
    # For inactive foreground, use slightly transparent version of text color
    $inactiveColor = if ($textColor -eq "#000000") { "#00000099" } else { "#FFFFFF99" }

    Write-Host "Worktree detected. Applying color $colorHex to $repoRoot"

    $colorCustomizations = New-Object PSObject
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "titleBar.activeBackground" -Value $colorHex -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "titleBar.activeForeground" -Value $textColor -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "titleBar.inactiveBackground" -Value $colorHex -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "titleBar.inactiveForeground" -Value $inactiveColor -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "statusBar.background" -Value $colorHex -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "statusBar.foreground" -Value $textColor -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "activityBar.background" -Value $colorHex -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "activityBar.foreground" -Value $textColor -Force
    $colorCustomizations | Add-Member -MemberType NoteProperty -Name "activityBar.inactiveForeground" -Value $inactiveColor -Force

    # Build worktree-local workspace file (based on fw.code-workspace)
    $worktreeWorkspace = New-Object PSObject
    if ($baseWorkspace.PSObject.Properties["folders"]) {
        $worktreeWorkspace | Add-Member -MemberType NoteProperty -Name "folders" -Value $baseWorkspace.folders
    } else {
        $worktreeWorkspace | Add-Member -MemberType NoteProperty -Name "folders" -Value @(@{ path = "." })
    }

    $settings = Ensure-PSObject $baseWorkspace.settings
    $settings | Add-Member -MemberType NoteProperty -Name "workbench.colorCustomizations" -Value (Ensure-PSObject $settings."workbench.colorCustomizations") -Force

    foreach ($key in $managedKeys) {
        # Remove any existing managed keys to keep behavior deterministic
        if ($settings."workbench.colorCustomizations".PSObject.Properties[$key]) {
            $settings."workbench.colorCustomizations".PSObject.Properties.Remove($key)
        }
    }

    foreach ($prop in $colorCustomizations.PSObject.Properties) {
        $settings."workbench.colorCustomizations" | Add-Member -MemberType NoteProperty -Name $prop.Name -Value $prop.Value -Force
    }

    $worktreeWorkspace | Add-Member -MemberType NoteProperty -Name "settings" -Value $settings

    $worktreeWorkspace | ConvertTo-Json -Depth 10 | Set-Content $worktreeWorkspacePath
    Write-Host "Wrote worktree-local workspace file: $worktreeWorkspacePath"
    Write-Host "Tip: Open it via VS Code: File -> Open Workspace from File..."

} else {
    # --- CLEAR COLORS ---
    Write-Host "Main repo (or non-worktree) detected. Clearing managed colors."

    if (Test-Path $worktreeWorkspacePath) {
        Remove-Item -Path $worktreeWorkspacePath -Force
    }
}
