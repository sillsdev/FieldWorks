<#
.SYNOPSIS
    Sets a unique window color for the current VS Code workspace/worktree.
.DESCRIPTION
    Generates a color based on the workspace path hash and writes it to
    .vscode/settings.json.
    - If in a Git Worktree: Applies colors to Title Bar, Status Bar, and Activity Bar.
    - If in Main Repo: Removes these color customizations.
    Intended to be run as a "folderOpen" task in VS Code.
#>

$ErrorActionPreference = "Stop"

# Get the repo root (parent of scripts/)
$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$vscodeDir = Join-Path $repoRoot ".vscode"
$settingsPath = Join-Path $vscodeDir "settings.json"
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

# Ensure .vscode exists if we need to write
if ($isWorktree -and -not (Test-Path $vscodeDir)) {
    New-Item -ItemType Directory -Path $vscodeDir -Force | Out-Null
}

# Load existing settings
$settings = $null
if (Test-Path $settingsPath) {
    try {
        $content = Get-Content $settingsPath -Raw
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $settings = $content | ConvertFrom-Json
        }
    } catch {
        Write-Warning "Could not parse existing settings.json. Starting fresh."
    }
}

if ($null -eq $settings) {
    $settings = New-Object PSObject
}

# Ensure workbench.colorCustomizations exists
if (-not $settings.PSObject.Properties["workbench.colorCustomizations"]) {
    $settings | Add-Member -MemberType NoteProperty -Name "workbench.colorCustomizations" -Value (New-Object PSObject)
}

$colors = $settings."workbench.colorCustomizations"
# Handle case where it might be a Hashtable (from fresh parse)
if ($colors -is [System.Collections.Hashtable]) {
    $newColors = New-Object PSObject
    foreach ($key in $colors.Keys) {
        $newColors | Add-Member -MemberType NoteProperty -Name $key -Value $colors[$key]
    }
    $settings."workbench.colorCustomizations" = $newColors
    $colors = $newColors
}

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

    # Generate color from path hash
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($repoRoot))
    $r = $hashBytes[0]; $g = $hashBytes[1]; $b = $hashBytes[2]
    $colorHex = "#{0:X2}{1:X2}{2:X2}" -f $r, $g, $b

    # Determine text color (contrast)
    $luminance = (0.299 * $r + 0.587 * $g + 0.114 * $b)
    $textColor = if ($luminance -gt 128) { "#000000" } else { "#FFFFFF" }
    # For inactive foreground, use slightly transparent version of text color
    $inactiveColor = if ($textColor -eq "#000000") { "#00000099" } else { "#FFFFFF99" }

    Write-Host "Worktree detected. Applying color $colorHex to $repoRoot"

    # Helper to set property
    function Set-Color($name, $val) {
        $colors | Add-Member -MemberType NoteProperty -Name $name -Value $val -Force
    }

    Set-Color "titleBar.activeBackground" $colorHex
    Set-Color "titleBar.activeForeground" $textColor
    Set-Color "titleBar.inactiveBackground" $colorHex
    Set-Color "titleBar.inactiveForeground" $inactiveColor

    Set-Color "statusBar.background" $colorHex
    Set-Color "statusBar.foreground" $textColor

    Set-Color "activityBar.background" $colorHex
    Set-Color "activityBar.foreground" $textColor
    Set-Color "activityBar.inactiveForeground" $inactiveColor

} else {
    # --- CLEAR COLORS ---
    Write-Host "Main repo (or non-worktree) detected. Clearing managed colors."

    foreach ($key in $managedKeys) {
        if ($colors.PSObject.Properties[$key]) {
            $colors.PSObject.Properties.Remove($key)
        }
    }
}

# Save settings
$settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
