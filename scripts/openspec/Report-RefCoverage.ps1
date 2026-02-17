<#
.SYNOPSIS
    Report OpenSpec coverage for AGENTS.md files.

.DESCRIPTION
    Counts how many top-level sections in AGENTS.md are referenced by specs and
    prints a summary report.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
}

function Get-RepoRelativePath {
    param([string]$Path)

    $full = (Resolve-Path $Path).Path
    $root = (Resolve-Path $RepoRoot).Path

    if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        $rel = $full.Substring($root.Length).TrimStart('\', '/')
        return $rel.Replace('\', '/')
    }

    return $full.Replace('\', '/')
}

function Get-AnchorSlug {
    param([string]$Heading)

    $clean = $Heading.Trim()
    $clean = $clean -replace '[^A-Za-z0-9 \-]', ''
    $clean = $clean.ToLowerInvariant()
    return ($clean -replace '\s', '-')
}

function Get-Sections {
    param([string[]]$Lines)

    $sections = @()
    $inFence = $false
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) {
            continue
        }
        if ($line -match '^(#{2})\s+(.+)$') {
            $title = $matches[2].Trim()
            if ($title -eq 'Change Log (auto)') {
                continue
            }
            $sections += [pscustomobject]@{
                Index = $i
                Title = $title
                Anchor = Get-AnchorSlug -Heading $title
            }
        }
    }

    return $sections
}

function Get-ReferencedSections {
    param([string[]]$Lines)

    $sections = Get-Sections -Lines $Lines
    $referenced = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    $currentAnchor = ''
    $inReferencedBy = $false
    $inFence = $false
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) {
            continue
        }
        if ($line -match '^(#{2})\s+(.+)$') {
            $title = $matches[2].Trim()
            $currentAnchor = Get-AnchorSlug -Heading $title
        }
        if ($line -match '^#{3,4}\s+Referenced By\s*$') {
            $inReferencedBy = $true
            continue
        }
        if ($line -match '^#{2,4}\s+' -and $line -notmatch '^#{3,4}\s+Referenced By\s*$') {
            $inReferencedBy = $false
        }
        if ($inReferencedBy -and $currentAnchor) {
            $null = $referenced.Add($currentAnchor)
        }
    }

    return [pscustomobject]@{
        Sections = $sections
        Referenced = $referenced
    }
}

$agentFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter 'AGENTS.md' -File

$fully = @()
$partial = @()
$none = @()

foreach ($agent in $agentFiles) {
    $lines = Get-Content -Path $agent.FullName -Encoding UTF8
    $data = Get-ReferencedSections -Lines $lines
    $total = $data.Sections.Count
    $count = $data.Referenced.Count

    $rel = Get-RepoRelativePath -Path $agent.FullName

    if ($count -eq 0) {
        $none += $rel
        continue
    }

    if ($count -ge $total -and $total -gt 0) {
        $fully += [pscustomobject]@{ Path = $rel; Count = $count }
    } else {
        $partial += [pscustomobject]@{ Path = $rel; Count = $count; Total = $total }
    }
}

Write-Host 'OpenSpec Reference Coverage Report'
Write-Host '=================================='
Write-Host ''

Write-Host 'Fully covered (has refs + back-refs):'
foreach ($item in $fully) {
    Write-Host ("  OK {0} ({1} sections referenced)" -f $item.Path, $item.Count)
}
if ($fully.Count -eq 0) {
    Write-Host '  (none)'
}
Write-Host ''

Write-Host 'Partially covered:'
foreach ($item in $partial) {
    Write-Host ("  ~ {0} ({1} of {2} sections referenced)" -f $item.Path, $item.Count, $item.Total)
}
if ($partial.Count -eq 0) {
    Write-Host '  (none)'
}
Write-Host ''

Write-Host 'Not covered:'
foreach ($item in $none) {
    Write-Host ("  o {0}" -f $item)
}
if ($none.Count -eq 0) {
    Write-Host '  (none)'
}
Write-Host ''

Write-Host ("Summary: {0} covered, {1} partial, {2} uncovered" -f $fully.Count, $partial.Count, $none.Count)
