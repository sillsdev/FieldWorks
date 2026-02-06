<#
.SYNOPSIS
    Sync anchors frontmatter for AGENTS.md files.

.DESCRIPTION
    Parses AGENTS.md headings (## and ###), generates GitHub-style anchor slugs,
    and compares/updates the anchors: block in YAML frontmatter.

.PARAMETER RepoRoot
    Repository root path. Defaults to script root/../..

.PARAMETER Check
    Report mismatches only (exit 1 if any mismatch).

.PARAMETER Fix
    Update anchors in place when mismatched.

.PARAMETER Init
    Add anchors block to files that lack it.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [switch]$Check,
    [switch]$Fix,
    [switch]$Init
)

$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
}

function Get-AnchorSlug {
    param([string]$Heading)

    $clean = $Heading.Trim()
    $clean = $clean -replace '[^A-Za-z0-9 \-]', ''
    $clean = $clean.ToLowerInvariant()
    return ($clean -replace '\s', '-')
}

function Get-FrontMatterRange {
    param([string[]]$Lines)

    if ($Lines.Count -lt 2) {
        return $null
    }

    if ($Lines[0].Trim() -ne '---') {
        return $null
    }

    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i].Trim() -eq '---') {
            return [pscustomobject]@{
                Start = 0
                End = $i
            }
        }
    }

    return $null
}

function Get-AnchorBlockRange {
    param(
        [string[]]$Lines,
        [int]$StartIndex
    )

    $anchorStart = -1
    $anchorEnd = -1
    $inAnchors = $false

    for ($i = $StartIndex; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        if ($line -match '^\s*anchors\s*:\s*$') {
            $anchorStart = $i
            $inAnchors = $true
            continue
        }

        if ($inAnchors) {
            if ($line -match '^\s*-\s*\S+') {
                $anchorEnd = $i
                continue
            }

            if ($line -match '^\s*\S') {
                break
            }
        }
    }

    if ($anchorStart -ge 0) {
        if ($anchorEnd -lt $anchorStart) {
            $anchorEnd = $anchorStart
        }
        return [pscustomobject]@{ Start = $anchorStart; End = $anchorEnd }
    }

    return $null
}

function Get-ExistingAnchors {
    param(
        [string[]]$Lines,
        [int]$StartIndex
    )

    $anchorBlock = Get-AnchorBlockRange -Lines $Lines -StartIndex $StartIndex
    if (-not $anchorBlock) {
        return @()
    }

    $anchors = @()
    for ($i = $anchorBlock.Start + 1; $i -le $anchorBlock.End; $i++) {
        $line = $Lines[$i]
        if ($line -match '^\s*-\s*(\S+)\s*$') {
            $anchors += $matches[1]
        }
    }

    return $anchors
}

function Get-HeadingAnchors {
    param([string[]]$Lines)

    $anchors = New-Object System.Collections.Generic.List[string]
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($line in $Lines) {
        if ($line -match '^(#{2,3})\s+(.+)$') {
            $heading = $matches[2]
            $slug = Get-AnchorSlug -Heading $heading
            if ($slug -and -not $seen.Contains($slug)) {
                $anchors.Add($slug)
                $null = $seen.Add($slug)
            }
        }
    }

    return $anchors
}

function Update-FileAnchors {
    param(
        [string]$Path,
        [switch]$Check,
        [switch]$Fix,
        [switch]$Init
    )

    $lines = Get-Content -Path $Path -Encoding UTF8
    $front = Get-FrontMatterRange -Lines $lines

    if (-not $front) {
        if ($Init) {
            $anchors = Get-HeadingAnchors -Lines $lines
            $frontMatter = @('---', '---', '')
            $anchorLines = @('anchors:') + ($anchors | ForEach-Object { "  - $_" }) + @('')
            $newLines = $frontMatter + $anchorLines + $lines
            Set-Content -Path $Path -Value $newLines -Encoding UTF8
            return $true
        }
        return $false
    }

    $searchStart = $front.End + 1
    $headingAnchors = Get-HeadingAnchors -Lines $lines
    $existingAnchors = Get-ExistingAnchors -Lines $lines -StartIndex $searchStart
    $anchorBlock = Get-AnchorBlockRange -Lines $lines -StartIndex $searchStart

    $matches = ($existingAnchors.Count -eq $headingAnchors.Count)
    if ($matches) {
        for ($i = 0; $i -lt $existingAnchors.Count; $i++) {
            if ($existingAnchors[$i] -ne $headingAnchors[$i]) {
                $matches = $false
                break
            }
        }
    }

    if ($matches) {
        return $false
    }

    if ($Check -and -not $Fix) {
        return $true
    }

    if (-not $Fix -and -not $Init) {
        return $false
    }

    $anchorLines = @('anchors:') + ($headingAnchors | ForEach-Object { "  - $_" })

    if ($anchorBlock) {
        $before = $lines[0..($anchorBlock.Start - 1)]
        $after = $lines[($anchorBlock.End + 1)..($lines.Count - 1)]
        $newLines = @()
        $newLines += $before
        $newLines += $anchorLines
        $newLines += $after
    } else {
        $before = $lines[0..$front.End]
        $after = $lines[($front.End + 1)..($lines.Count - 1)]
        $newLines = @()
        $newLines += $before
        $newLines += $anchorLines
        $newLines += $after
    }

    Set-Content -Path $Path -Value $newLines -Encoding UTF8
    return $true
}

$agentFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter 'AGENTS.md' -File
$mismatches = 0

foreach ($agent in $agentFiles) {
    $changed = Update-FileAnchors -Path $agent.FullName -Check:$Check -Fix:$Fix -Init:$Init
    if ($changed) {
        $mismatches++
        Write-Host ("Anchors out of sync: {0}" -f $agent.FullName)
    }
}

if ($Check -and $mismatches -gt 0) {
    exit 1
}

exit 0
