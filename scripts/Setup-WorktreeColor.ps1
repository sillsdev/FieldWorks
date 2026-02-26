<#
.SYNOPSIS
    Sets a unique window color for the current VS Code workspace/worktree.
.DESCRIPTION
	Chooses a color from a fixed deterministic palette using the lowest free slot.

    Uses the VS Code workspace (.code-workspace) paradigm for worktree overrides:
    - Base workspace configuration is embedded in this script
	- Writes a worktree-local workspace file: <branch>.code-workspace (git-ignored)

    - If in a Git Worktree: Applies colors to Title Bar, Status Bar, and Activity Bar.
    - If in Main Repo: Removes these color customizations.
    Intended to be run as a "folderOpen" task in VS Code.
#>

param(
	# VS Code expands ${workspaceFile} when running inside a .code-workspace.
	# When opening a folder, this is typically empty.
	[string]$VSCodeWorkspaceFile = "",

	# Optional explicit color slot override (0-based index into palette).
	[Nullable[int]]$ColorIndex = $null,

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

function Get-WorktreeWorkspaceLegacyPath([string]$rootPath) {
	return (Join-Path $rootPath "fw.worktree.code-workspace")
}

function Get-GitBranchName([string]$repoRoot) {
	# Prefer symbolic-ref so detached HEAD doesn't return the literal string "HEAD".
	try {
		$branch = @(& git -C $repoRoot symbolic-ref --quiet --short HEAD 2>$null)
		if ($LASTEXITCODE -eq 0 -and $branch.Length -gt 0 -and -not [string]::IsNullOrWhiteSpace($branch[0])) {
			return $branch[0].Trim()
		}
	}
	catch {
		# Fall through to detached handling
	}

	# Detached HEAD: use short SHA so the workspace file name is stable and informative.
	try {
		$sha = @(& git -C $repoRoot rev-parse --short HEAD 2>$null)
		if ($LASTEXITCODE -eq 0 -and $sha.Length -gt 0 -and -not [string]::IsNullOrWhiteSpace($sha[0])) {
			return ("detached-{0}" -f $sha[0].Trim())
		}
	}
	catch {
		# ignore
	}

	return "detached"
}

function ConvertTo-SafeWorkspaceFileStem([string]$name) {
	if ([string]::IsNullOrWhiteSpace($name)) {
		return "fw.worktree"
	}
	$stem = $name.Trim()

	# Prevent accidental subfolders (e.g. feature/foo) and other invalid filename chars.
	$stem = $stem -replace "[\\/]", "-"
	$stem = $stem -replace "\s+", "-"
	foreach ($ch in [System.IO.Path]::GetInvalidFileNameChars()) {
		$escaped = [Regex]::Escape([string]$ch)
		$stem = $stem -replace $escaped, "-"
	}
	$stem = $stem.Trim(' ', '.', '-')
	if ([string]::IsNullOrWhiteSpace($stem)) {
		return "fw.worktree"
	}
	return $stem
}

function Get-WorktreeWorkspacePath([string]$rootPath) {
	$branchName = Get-GitBranchName $rootPath
	$fileStem = ConvertTo-SafeWorkspaceFileStem $branchName
	return (Join-Path $rootPath ("{0}.code-workspace" -f $fileStem))
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
		# Wrap in @() so a single line still becomes an array under StrictMode.
		$lines = @(& git -C $anyRepoRoot worktree list --porcelain 2>$null)
	}
	catch {
		throw "Failed to run 'git worktree list'. Is git available on PATH?"
	}

	if ($null -eq $lines -or $lines.Length -eq 0) {
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
	$worktrees = @($worktrees)
	if ($null -eq $worktrees -or $worktrees.Length -eq 0) {
		throw "No worktrees found."
	}

	if ($worktrees.Length -eq 1) {
		return $worktrees[0]
	}

	Write-Host "Select a worktree:"
	for ($i = 0; $i -lt $worktrees.Length; $i++) {
		$w = $worktrees[$i]
		$branch = if ([string]::IsNullOrWhiteSpace($w.Branch)) { "" } else { " [$($w.Branch)]" }
		Write-Host ("  {0}) {1}{2}" -f ($i + 1), $w.Path, $branch)
	}

	while ($true) {
		$raw = Read-Host ("Enter selection (1-{0})" -f $worktrees.Length)
		[int]$idx = 0
		if ([int]::TryParse($raw, [ref]$idx) -and $idx -ge 1 -and $idx -le $worktrees.Length) {
			return $worktrees[$idx - 1]
		}
		Write-Warning "Invalid selection."
	}
}

function Add-PSObjectPropertyIfMissing($obj, $name) {
	if (-not $obj.PSObject.Properties[$name]) {
		$obj | Add-Member -MemberType NoteProperty -Name $name -Value (New-Object PSObject)
	}
}

function ConvertTo-PSObject($val) {
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

function ConvertFrom-HexRgb($hex) {
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

function ConvertTo-RelativeLuminance([int]$r, [int]$g, [int]$b) {
	$rScaled = $r / 255.0
	$gScaled = $g / 255.0
	$bScaled = $b / 255.0

	$rLin = if ($rScaled -le 0.03928) { $rScaled / 12.92 } else { [Math]::Pow((($rScaled + 0.055) / 1.055), 2.4) }
	$gLin = if ($gScaled -le 0.03928) { $gScaled / 12.92 } else { [Math]::Pow((($gScaled + 0.055) / 1.055), 2.4) }
	$bLin = if ($bScaled -le 0.03928) { $bScaled / 12.92 } else { [Math]::Pow((($bScaled + 0.055) / 1.055), 2.4) }

	return (0.2126 * $rLin + 0.7152 * $gLin + 0.0722 * $bLin)
}

function Get-ContrastRatio([double]$l1, [double]$l2) {
	$bright = [Math]::Max($l1, $l2)
	$dark = [Math]::Min($l1, $l2)
	return (($bright + 0.05) / ($dark + 0.05))
}

function Get-ForegroundColorsForBackground([string]$backgroundHex) {
	$rgb = ConvertFrom-HexRgb $backgroundHex
	$lBackground = ConvertTo-RelativeLuminance -r $rgb.r -g $rgb.g -b $rgb.b
	$contrastWhite = Get-ContrastRatio -l1 $lBackground -l2 1.0
	$contrastBlack = Get-ContrastRatio -l1 $lBackground -l2 0.0

	$textColor = if ($contrastWhite -ge $contrastBlack) { "#FFFFFF" } else { "#000000" }
	$inactiveColor = if ($textColor -eq "#000000") { "#000000B3" } else { "#FFFFFFB3" }

	return @{
		TextColor = $textColor
		InactiveColor = $inactiveColor
		ContrastWhite = [Math]::Round($contrastWhite, 2)
		ContrastBlack = [Math]::Round($contrastBlack, 2)
	}
}

function Remove-JsonComments([string]$text) {
	if ([string]::IsNullOrEmpty($text)) {
		return ""
	}

	$sb = New-Object System.Text.StringBuilder
	$inString = $false
	$escapeNext = $false
	$inLineComment = $false
	$inBlockComment = $false

	for ($i = 0; $i -lt $text.Length; $i++) {
		$ch = $text[$i]
		$next = if ($i + 1 -lt $text.Length) { $text[$i + 1] } else { [char]0 }

		if ($inLineComment) {
			if ($ch -eq "`n") {
				$inLineComment = $false
				[void]$sb.Append($ch)
			}
			continue
		}

		if ($inBlockComment) {
			if ($ch -eq '/' -and $next -eq '*') {
				# Nested block comment start; treat as content of comment.
				continue
			}
			if ($ch -eq '*' -and $next -eq '/') {
				$inBlockComment = $false
				$i++
			}
			continue
		}

		if ($inString) {
			[void]$sb.Append($ch)
			if ($escapeNext) {
				$escapeNext = $false
				continue
			}
			if ($ch -eq '\\') {
				$escapeNext = $true
				continue
			}
			if ($ch -eq '"') {
				$inString = $false
			}
			continue
		}

		# Not in string
		if ($ch -eq '"') {
			$inString = $true
			[void]$sb.Append($ch)
			continue
		}

		if ($ch -eq '/' -and $next -eq '/') {
			$inLineComment = $true
			$i++
			continue
		}

		if ($ch -eq '/' -and $next -eq '*') {
			$inBlockComment = $true
			$i++
			continue
		}

	[void]$sb.Append($ch)
	}

	return $sb.ToString()
}

function Remove-JsonTrailingCommas([string]$text) {
	if ([string]::IsNullOrEmpty($text)) {
		return ""
	}

	$sb = New-Object System.Text.StringBuilder
	$inString = $false
	$escapeNext = $false

	for ($i = 0; $i -lt $text.Length; $i++) {
		$ch = $text[$i]
		if ($inString) {
			[void]$sb.Append($ch)
			if ($escapeNext) {
				$escapeNext = $false
				continue
			}
			if ($ch -eq '\\') {
				$escapeNext = $true
				continue
			}
			if ($ch -eq '"') {
				$inString = $false
			}
			continue
		}

		if ($ch -eq '"') {
			$inString = $true
			[void]$sb.Append($ch)
			continue
		}

		if ($ch -eq ',') {
			$j = $i + 1
			while ($j -lt $text.Length) {
				$look = $text[$j]
				if ($look -eq ' ' -or $look -eq "`t" -or $look -eq "`r" -or $look -eq "`n") {
					$j++
					continue
				}
				break
			}
			if ($j -lt $text.Length) {
				$nextNonWs = $text[$j]
				if ($nextNonWs -eq '}' -or $nextNonWs -eq ']') {
					continue
				}
			}
		}

		[void]$sb.Append($ch)
	}

	return $sb.ToString()
}

function ConvertFrom-JsoncFile([string]$path) {
	if (-not (Test-Path $path -PathType Leaf)) {
		return $null
	}
	$raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8
	$noComments = Remove-JsonComments $raw
	$clean = Remove-JsonTrailingCommas $noComments
	return ($clean | ConvertFrom-Json)
}

# Base workspace configuration (embedded)
$baseWorkspace = New-Object PSObject
$baseWorkspace | Add-Member -MemberType NoteProperty -Name "folders" -Value @(@{ path = "." })
$baseWorkspace | Add-Member -MemberType NoteProperty -Name "settings" -Value (New-Object PSObject)

$baseWorkspace = ConvertTo-PSObject $baseWorkspace
Add-PSObjectPropertyIfMissing $baseWorkspace "settings"
$baseSettings = ConvertTo-PSObject $baseWorkspace.settings
$baseWorkspace.settings = $baseSettings

# Define the keys we manage
$managedKeys = @(
	"titleBar.activeBackground", "titleBar.activeForeground",
	"titleBar.inactiveBackground", "titleBar.inactiveForeground",
	"statusBar.background", "statusBar.foreground",
	"activityBar.background", "activityBar.foreground",
	"activityBar.inactiveForeground"
)

# Deterministic worktree palette (16 colors), ordered to cycle hue families early.
$worktreeColorPalette = @(
	"#d64541", # Bold Red
	"#2f6fdd", # Bold Blue
	"#2e8b57", # Rich Green
	"#c9a227", # Subdued Yellow
	"#7b4ab2", # Purple
	"#e67e22", # Orange
	"#1f9d9d", # Teal
	"#c2529f", # Magenta
	"#b13f63", # Raspberry
	"#3a9ec9", # Sky Blue
	"#365f9c", # Denim Blue
	"#8f5a2a", # Walnut
	"#3a6f3a", # Forest Green
	"#4f4f4f", # Charcoal
	"#7a6fa8", # Muted Violet
	"#2f4858"  # Deep Slate
)

function Get-WorktreeColorPalette() {
	return @($worktreeColorPalette)
}

function ConvertTo-ColorIndex([object]$value, [int]$paletteLength) {
	if ($null -eq $value) {
		return $null
	}

	[int]$idx = 0
	if ($value -is [int]) {
		$idx = $value
	}
	else {
		if (-not [int]::TryParse(($value.ToString()), [ref]$idx)) {
			return $null
		}
	}

	if ($idx -lt 0 -or $idx -ge $paletteLength) {
		return $null
	}

	return $idx
}

function Get-ColorIndexFromSettings($settings, [string[]]$palette) {
	if ($null -eq $settings) {
		return $null
	}

	if ($settings.PSObject.Properties["fieldworks.worktreeColorIndex"]) {
		$idx = ConvertTo-ColorIndex -value $settings."fieldworks.worktreeColorIndex" -paletteLength $palette.Length
		if ($null -ne $idx) {
			return $idx
		}
	}

	if (
		$settings.PSObject.Properties["workbench.colorCustomizations"] -and
		$null -ne $settings."workbench.colorCustomizations" -and
		$settings."workbench.colorCustomizations".PSObject.Properties["titleBar.activeBackground"]
	) {
		$hex = $settings."workbench.colorCustomizations"."titleBar.activeBackground"
		if (-not [string]::IsNullOrWhiteSpace($hex)) {
			for ($i = 0; $i -lt $palette.Length; $i++) {
				if ([string]::Equals($palette[$i], $hex, [System.StringComparison]::OrdinalIgnoreCase)) {
					return $i
				}
			}
		}
	}

	return $null
}

function Get-UsedColorIndices([string]$mainRepoRoot, [string]$targetRoot, [string[]]$palette) {
	$used = @{}
	$normalizedTargetRoot = Normalize-PathForComparison $targetRoot
	$worktrees = @(Get-GitWorktrees $mainRepoRoot)

	foreach ($wt in $worktrees) {
		if ($null -eq $wt -or [string]::IsNullOrWhiteSpace($wt.Path)) {
			continue
		}

		$normalizedPath = Normalize-PathForComparison $wt.Path
		if ($normalizedPath -eq $normalizedTargetRoot) {
			continue
		}

		$workspacePath = Get-WorktreeWorkspacePath $wt.Path
		if (-not (Test-Path $workspacePath -PathType Leaf)) {
			continue
		}

		try {
			$workspace = ConvertFrom-JsoncFile $workspacePath
			if ($null -eq $workspace -or -not $workspace.PSObject.Properties["settings"]) {
				continue
			}

			$idx = Get-ColorIndexFromSettings -settings $workspace.settings -palette $palette
			if ($null -ne $idx) {
				$used[[int]$idx] = $true
			}
		}
		catch {
			Write-Warning "Failed to inspect color settings in $workspacePath"
		}
	}

	return $used
}

function Select-WorktreeColorIndex(
	[string]$mainRepoRoot,
	[string]$targetRoot,
	[Nullable[int]]$requestedColorIndex,
	[string[]]$palette
) {
	if ($null -ne $requestedColorIndex) {
		if ($requestedColorIndex.Value -lt 0 -or $requestedColorIndex.Value -ge $palette.Length) {
			throw "ColorIndex '$($requestedColorIndex.Value)' is out of range. Valid range: 0-$($palette.Length - 1)."
		}
		return $requestedColorIndex.Value
	}

	$targetWorkspacePath = Get-WorktreeWorkspacePath $targetRoot
	if (Test-Path $targetWorkspacePath -PathType Leaf) {
		try {
			$existingWorkspace = ConvertFrom-JsoncFile $targetWorkspacePath
			if ($null -ne $existingWorkspace -and $existingWorkspace.PSObject.Properties["settings"]) {
				$existingIdx = Get-ColorIndexFromSettings -settings $existingWorkspace.settings -palette $palette
				if ($null -ne $existingIdx) {
					return $existingIdx
				}
			}
		}
		catch {
			Write-Warning "Failed to parse existing workspace file $targetWorkspacePath while selecting color index."
		}
	}

	$used = Get-UsedColorIndices -mainRepoRoot $mainRepoRoot -targetRoot $targetRoot -palette $palette
	for ($i = 0; $i -lt $palette.Length; $i++) {
		if (-not $used.ContainsKey($i)) {
			return $i
		}
	}

	$md5 = [System.Security.Cryptography.MD5]::Create()
	$hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($targetRoot))
	return ($hashBytes[0] % $palette.Length)
}

function Write-WorktreeWorkspaceFile(
	[Parameter(Mandatory = $true)][string]$targetRoot,
	[Parameter(Mandatory = $true)][bool]$isWorkspaceLoaded,
	[Nullable[int]]$requestedColorIndex = $null
) {
	$worktreeWorkspacePath = Get-WorktreeWorkspacePath $targetRoot
	$legacyWorkspacePath = Get-WorktreeWorkspaceLegacyPath $targetRoot

	$palette = Get-WorktreeColorPalette
	$resolvedColorIndex = Select-WorktreeColorIndex -mainRepoRoot $repoRoot -targetRoot $targetRoot -requestedColorIndex $requestedColorIndex -palette $palette
	$colorHex = $palette[$resolvedColorIndex]
	$foreground = Get-ForegroundColorsForBackground -backgroundHex $colorHex
	$textColor = $foreground.TextColor
	$inactiveColor = $foreground.InactiveColor

	Write-Host "Worktree detected. Applying color index $resolvedColorIndex ($colorHex) to $targetRoot; contrast white=$($foreground.ContrastWhite), black=$($foreground.ContrastBlack), using $textColor"

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

	# Merge repo/workspace settings into the generated workspace so opening via *.code-workspace
	# preserves the same VS Code experience as opening the folder/workspace directly.
	#
	# IMPORTANT:
	# - We prefer the existing generated workspace file (if present) to avoid overwriting
	#   previously-merged settings on subsequent runs.
	# - If VS Code provides a workspace file path (${workspaceFile}), prefer its settings.
	# - Otherwise, fall back to the invoking repo's .vscode/settings.json (not the target
	#   worktree's), so all worktrees inherit the same base settings.
	#
	# (Some settings do not apply at "workspace-folder" scope in multi-root workspaces.)
	$repoSettingsSource = $null
	$existingWorkspaceSettings = $null

	# 1) Capture settings from an existing generated worktree workspace file (overlay).
	if (Test-Path $worktreeWorkspacePath -PathType Leaf) {
		try {
			$existingWorkspace = ConvertFrom-JsoncFile $worktreeWorkspacePath
			if ($null -ne $existingWorkspace -and $existingWorkspace.PSObject.Properties["settings"]) {
				$existingWorkspaceSettings = $existingWorkspace.settings
			}
		}
		catch {
			Write-Warning "Failed to parse existing workspace file $worktreeWorkspacePath; continuing."
			$existingWorkspaceSettings = $null
		}
	}

	# 2) If VS Code told us which workspace file is loaded, use its settings as the base.
	if (-not [string]::IsNullOrWhiteSpace($VSCodeWorkspaceFile) -and (Test-Path $VSCodeWorkspaceFile -PathType Leaf)) {
		try {
			$loadedWorkspace = ConvertFrom-JsoncFile $VSCodeWorkspaceFile
			if ($null -ne $loadedWorkspace -and $loadedWorkspace.PSObject.Properties["settings"]) {
				$repoSettingsSource = $loadedWorkspace.settings
			}
		}
		catch {
			Write-Warning "Failed to parse VS Code workspace file $VSCodeWorkspaceFile; continuing."
			$repoSettingsSource = $null
		}
	}

	# 3) Fall back to invoking repo's folder settings as the base.
	if ($null -eq $repoSettingsSource) {
		$repoSettingsPath = Join-Path $repoRoot ".vscode\\settings.json"
		try {
			$repoSettings = ConvertFrom-JsoncFile $repoSettingsPath
			if ($null -ne $repoSettings) {
				$repoSettingsSource = $repoSettings
			}
		}
		catch {
			Write-Warning "Failed to parse $repoSettingsPath; continuing without merging repo settings."
			$repoSettingsSource = $null
		}
	}

	# Start with base settings (repo/workspace settings if available), then overlay any existing
	# generated-workspace settings to preserve user tweaks.
	$settings = if ($null -ne $repoSettingsSource) { ConvertTo-PSObject $repoSettingsSource } else { ConvertTo-PSObject $baseWorkspace.settings }
	if ($null -ne $existingWorkspaceSettings) {
		$overlay = ConvertTo-PSObject $existingWorkspaceSettings
		foreach ($p in $overlay.PSObject.Properties) {
			$settings | Add-Member -MemberType NoteProperty -Name $p.Name -Value $p.Value -Force
		}
	}
	$existingColorCustomizations = $null
	if ($settings.PSObject.Properties["workbench.colorCustomizations"]) {
		$existingColorCustomizations = $settings."workbench.colorCustomizations"
	}
	$settings | Add-Member -MemberType NoteProperty -Name "workbench.colorCustomizations" -Value (ConvertTo-PSObject $existingColorCustomizations) -Force

	foreach ($key in $managedKeys) {
		# Remove any existing managed keys to keep behavior deterministic
		if ($settings."workbench.colorCustomizations".PSObject.Properties[$key]) {
			$settings."workbench.colorCustomizations".PSObject.Properties.Remove($key)
		}
	}

	foreach ($prop in $colorCustomizations.PSObject.Properties) {
		$settings."workbench.colorCustomizations" | Add-Member -MemberType NoteProperty -Name $prop.Name -Value $prop.Value -Force
	}

	$settings | Add-Member -MemberType NoteProperty -Name "fieldworks.worktreeColorIndex" -Value $resolvedColorIndex -Force

	# Marker flag: true only when this task is running inside the generated workspace file
	$settings | Add-Member -MemberType NoteProperty -Name "fieldworks.workspaceLoaded" -Value $isWorkspaceLoaded -Force

	$worktreeWorkspace | Add-Member -MemberType NoteProperty -Name "settings" -Value $settings

	$worktreeWorkspace | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $worktreeWorkspacePath
	Write-Host "Wrote worktree-local workspace file: $worktreeWorkspacePath"

	# Best-effort cleanup of legacy workspace filename.
	if ($legacyWorkspacePath -ne $worktreeWorkspacePath -and (Test-Path $legacyWorkspacePath -PathType Leaf)) {
		try {
			Remove-Item -Path $legacyWorkspacePath -Force
			Write-Host "Removed legacy workspace file: $legacyWorkspacePath"
		}
		catch {
			Write-Warning "Failed to remove legacy workspace file: $legacyWorkspacePath"
		}
	}

	return $worktreeWorkspacePath
}

switch ($Action) {
	"Launch" {
		$worktrees = @(Get-GitWorktrees $repoRoot)
		if ($worktrees.Length -eq 0) {
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

		$workspaceFile = Write-WorktreeWorkspaceFile -targetRoot $targetRoot -isWorkspaceLoaded:$false -requestedColorIndex $ColorIndex

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
		$legacyWorkspacePath = Get-WorktreeWorkspaceLegacyPath $targetRoot

		$normalizedPassedWorkspaceFile = Normalize-PathForComparison $VSCodeWorkspaceFile
		$normalizedWorktreeWorkspacePath = Normalize-PathForComparison $worktreeWorkspacePath
		$isWorktreeWorkspaceLoaded = ($normalizedPassedWorkspaceFile -ne "") -and ($normalizedPassedWorkspaceFile -eq $normalizedWorktreeWorkspacePath)

		if (Get-IsWorktree $targetRoot) {
			[void](Write-WorktreeWorkspaceFile -targetRoot $targetRoot -isWorkspaceLoaded:$isWorktreeWorkspaceLoaded -requestedColorIndex $ColorIndex)
			if (-not $isWorktreeWorkspaceLoaded) {
				Write-Host "Workspace file created. To apply colors, open: $worktreeWorkspacePath"
			}
			else {
				Write-Host "Worktree workspace is loaded; colors should be active."
			}
		}
		else {
			Write-Host "Main repo (or non-worktree) detected. Clearing managed colors."
			# Only remove the legacy file here; branch-named workspaces live only in worktrees.
			if (Test-Path $legacyWorkspacePath -PathType Leaf) {
				Remove-Item -Path $legacyWorkspacePath -Force
			}
		}
		break
	}
}
