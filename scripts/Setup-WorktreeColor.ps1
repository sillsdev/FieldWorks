<#
.SYNOPSIS
    Sets a unique window color for the current VS Code workspace/worktree.
.DESCRIPTION
    Chooses a color from a fixed 8-color palette based on the workspace path hash.

    Uses the VS Code workspace (.code-workspace) paradigm for worktree overrides:
    - Base workspace configuration is embedded in this script
    - Writes a worktree-local workspace file: fw.worktree.code-workspace (git-ignored)

    - If in a Git Worktree: Applies colors to Title Bar, Status Bar, and Activity Bar.
    - If in Main Repo: Removes these color customizations.
    Intended to be run as a "folderOpen" task in VS Code.
#>

param(
	# VS Code expands ${workspaceFile} when running inside a .code-workspace.
	# When opening a folder, this is typically empty.
	[string]$VSCodeWorkspaceFile = "",

	# Apply: write/remove the worktree-local workspace file for the current repoRoot.
	# Launch: pick a worktree (if multiple) and open it in a new VS Code window.
	[ValidateSet("Apply", "Launch")]
	[string]$Action = "Apply",

	# Optional explicit worktree path (skips picker). Useful for scripting.
	[string]$WorktreePath = ""
)

$ErrorActionPreference = "Stop"

# Get the repo root (parent of scripts/)
$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName

function Normalize-PathForComparison([string]$path) {
	if ([string]::IsNullOrWhiteSpace($path)) {
		return ""
	}
	try {
		# Avoid Resolve-Path here because the workspace file may not exist yet.
		return ([System.IO.Path]::GetFullPath($path)).ToLowerInvariant()
	}
	catch {
		return $path.ToLowerInvariant()
	}
}

function Get-IsWorktree([string]$rootPath) {
	$gitPath = Join-Path $rootPath ".git"
	if (Test-Path $gitPath -PathType Leaf) {
		# .git is a file in worktrees (pointing to the main repo gitdir)
		return $true
	}
	if (Test-Path $gitPath -PathType Container) {
		# .git is a directory in the main repo
		return $false
	}
	Write-Warning "No .git found at $gitPath. Assuming not a worktree."
	return $false
}

function Get-WorktreeWorkspacePath([string]$rootPath) {
	return (Join-Path $rootPath "fw.worktree.code-workspace")
}

function Get-GitWorktrees([string]$anyRepoRoot) {
	# git worktree list --porcelain format example:
	# worktree C:/path
	# HEAD <sha>
	# branch refs/heads/foo
	#
	# worktree C:/path2
	$lines = @()
	try {
		$lines = & git -C $anyRepoRoot worktree list --porcelain 2>$null
	}
	catch {
		throw "Failed to run 'git worktree list'. Is git available on PATH?"
	}

	if ($null -eq $lines -or $lines.Count -eq 0) {
		return @()
	}

	$worktrees = New-Object System.Collections.Generic.List[object]
	$current = $null

	foreach ($line in $lines) {
		if ([string]::IsNullOrWhiteSpace($line)) {
			if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path)) {
				$worktrees.Add($current)
			}
			$current = $null
			continue
		}

		if ($line.StartsWith("worktree ")) {
			if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path)) {
				$worktrees.Add($current)
			}
			$current = [pscustomobject]@{
				Path = $line.Substring("worktree ".Length)
				Branch = ""
			}
			continue
		}

		if ($null -eq $current) {
			continue
		}

		if ($line.StartsWith("branch ")) {
			$current.Branch = $line.Substring("branch ".Length)
			continue
		}

		if ($line -eq "detached") {
			$current.Branch = "(detached)"
			continue
		}
	}

	if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path)) {
		$worktrees.Add($current)
	}

	return $worktrees.ToArray()
}

function Pick-Worktree([object[]]$worktrees) {
	if ($null -eq $worktrees -or $worktrees.Count -eq 0) {
		throw "No worktrees found."
	}

	if ($worktrees.Count -eq 1) {
		return $worktrees[0]
	}

	Write-Host "Select a worktree:" 
	for ($i = 0; $i -lt $worktrees.Count; $i++) {
		$w = $worktrees[$i]
		$branch = if ([string]::IsNullOrWhiteSpace($w.Branch)) { "" } else { " [$($w.Branch)]" }
		Write-Host ("  {0}) {1}{2}" -f ($i + 1), $w.Path, $branch)
	}

	while ($true) {
		$raw = Read-Host ("Enter selection (1-{0})" -f $worktrees.Count)
		[int]$idx = 0
		if ([int]::TryParse($raw, [ref]$idx) -and $idx -ge 1 -and $idx -le $worktrees.Count) {
			return $worktrees[$idx - 1]
		}
		Write-Warning "Invalid selection."
	}
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
	return @{
		r = $r
		g = $g
		b = $b
	}
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

function Write-WorktreeWorkspaceFile([
	Parameter(Mandatory = $true)][string]$targetRoot,
	[Parameter(Mandatory = $true)][bool]$isWorkspaceLoaded
) {
	$worktreeWorkspacePath = Get-WorktreeWorkspacePath $targetRoot

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
	$hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($targetRoot))
	$colorHex = $palette[($hashBytes[0] % $palette.Length)]
	$rgb = Parse-HexRgb $colorHex
	$r = $rgb.r; $g = $rgb.g; $b = $rgb.b

	# Determine text color (contrast)
	$luminance = (0.299 * $r + 0.587 * $g + 0.114 * $b)
	$textColor = if ($luminance -gt 128) { "#000000" } else { "#FFFFFF" }
	# For inactive foreground, use slightly transparent version of text color
	$inactiveColor = if ($textColor -eq "#000000") { "#00000099" } else { "#FFFFFF99" }

	Write-Host "Worktree detected. Applying color $colorHex to $targetRoot"

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

	# Build worktree-local workspace file
	$worktreeWorkspace = New-Object PSObject
	if ($baseWorkspace.PSObject.Properties["folders"]) {
		$worktreeWorkspace | Add-Member -MemberType NoteProperty -Name "folders" -Value $baseWorkspace.folders
	}
	else {
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

	# Marker flag: true only when this task is running inside the generated workspace file
	$settings | Add-Member -MemberType NoteProperty -Name "fieldworks.workspaceLoaded" -Value $isWorkspaceLoaded -Force

	$worktreeWorkspace | Add-Member -MemberType NoteProperty -Name "settings" -Value $settings

	$worktreeWorkspace | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $worktreeWorkspacePath
	Write-Host "Wrote worktree-local workspace file: $worktreeWorkspacePath"

	return $worktreeWorkspacePath
}

switch ($Action) {
	"Launch" {
		$worktrees = Get-GitWorktrees $repoRoot
		if ($worktrees.Count -eq 0) {
			throw "No worktrees found in this repo."
		}

		$selected = $null
		if (-not [string]::IsNullOrWhiteSpace($WorktreePath)) {
			$selected = [pscustomobject]@{ Path = $WorktreePath; Branch = "" }
		}
		else {
			$selected = Pick-Worktree $worktrees
		}

		$targetRoot = $selected.Path
		if (-not (Test-Path $targetRoot -PathType Container)) {
			throw "Selected worktree path does not exist: $targetRoot"
		}

		if (-not (Get-IsWorktree $targetRoot)) {
			throw "Selected path does not look like a git worktree (expected .git file): $targetRoot"
		}

		$workspaceFile = Write-WorktreeWorkspaceFile -targetRoot $targetRoot -isWorkspaceLoaded:$false

		$codeCmd = "code"
		try {
			Write-Host "Opening worktree in a new VS Code window..."
			& $codeCmd "--new-window" $workspaceFile
		}
		catch {
			Write-Warning "Could not launch VS Code automatically. Please open $workspaceFile manually."
		}
		break
	}

	"Apply" {
		$targetRoot = if (-not [string]::IsNullOrWhiteSpace($WorktreePath)) { $WorktreePath } else { $repoRoot }
		$worktreeWorkspacePath = Get-WorktreeWorkspacePath $targetRoot

		$normalizedPassedWorkspaceFile = Normalize-PathForComparison $VSCodeWorkspaceFile
		$normalizedWorktreeWorkspacePath = Normalize-PathForComparison $worktreeWorkspacePath
		$isWorktreeWorkspaceLoaded = ($normalizedPassedWorkspaceFile -ne "") -and ($normalizedPassedWorkspaceFile -eq $normalizedWorktreeWorkspacePath)

		if (Get-IsWorktree $targetRoot) {
			[void](Write-WorktreeWorkspaceFile -targetRoot $targetRoot -isWorkspaceLoaded:$isWorktreeWorkspaceLoaded)
			if (-not $isWorktreeWorkspaceLoaded) {
				Write-Host "Workspace file created. To apply colors, open: $worktreeWorkspacePath"
			}
			else {
				Write-Host "Worktree workspace is loaded; colors should be active."
			}
		}
		else {
			Write-Host "Main repo (or non-worktree) detected. Clearing managed colors."
			if (Test-Path $worktreeWorkspacePath) {
				Remove-Item -Path $worktreeWorkspacePath -Force
			}
		}
		break
	}
}
