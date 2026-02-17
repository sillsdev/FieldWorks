<#
.SYNOPSIS
    Validate OpenSpec cross-references between spec files and AGENTS.md.

.DESCRIPTION
    Scans OpenSpec specs for AGENTS.md forward refs and verifies:
    - Target files exist
    - Anchors exist
    - Back-refs are present in AGENTS.md "Referenced By" sections
    Scans AGENTS.md back-refs and verifies:
    - Target specs exist
    - Anchors exist
    - Forward refs are present in specs

.EXITCODES
    0: All refs valid
    1: Broken refs found
    2: Orphaned refs found (missing bidirectional pair)
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
    # Remove punctuation but keep spaces to preserve GitHub-style double hyphens.
    $clean = $clean -replace '[^A-Za-z0-9 \-]', ''
    $clean = $clean.ToLowerInvariant()
    $slug = $clean -replace '\s', '-'
    return $slug
}

function Get-FrontMatterAnchors {
    param([string[]]$Lines)

    $anchors = @()
    if ($Lines.Count -lt 2) {
        return $anchors
    }

    if ($Lines[0].Trim() -ne '---') {
        return $anchors
    }

    $endIndex = -1
    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i].Trim() -eq '---') {
            $endIndex = $i
            break
        }
    }

    if ($endIndex -lt 0) {
        return $anchors
    }

    $inAnchors = $false
    for ($i = 1; $i -lt $endIndex; $i++) {
        $line = $Lines[$i]
        if ($line -match '^\s*anchors\s*:\s*$') {
            $inAnchors = $true
            continue
        }

        if ($inAnchors) {
            if ($line -match '^\s*-\s*(\S+)\s*$') {
                $anchors += $matches[1]
                continue
            }

            if ($line -match '^\s*\S') {
                $inAnchors = $false
            }
        }
    }

    return $anchors
}

$anchorCache = @{}

function Get-FileAnchors {
    param([string]$Path)

    if ($anchorCache.ContainsKey($Path)) {
        return $anchorCache[$Path]
    }

    $lines = Get-Content -Path $Path -Encoding UTF8
    $anchors = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($anchor in (Get-FrontMatterAnchors -Lines $lines)) {
        $null = $anchors.Add($anchor)
    }

    $inFence = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) {
            continue
        }
        if ($line -match '^(#+)\s+(.+)$') {
            $heading = $matches[2]
            $slug = Get-AnchorSlug -Heading $heading
            if ($slug) {
                $null = $anchors.Add($slug)
            }
        }
    }

    $anchorCache[$Path] = $anchors
    return $anchors
}

function Resolve-LinkTarget {
    param(
        [string]$SourcePath,
        [string]$Href
    )

    if ($Href -match '^\w+://') {
        return $null
    }

    if ($Href -match '^#') {
        return $null
    }

    $decoded = [System.Uri]::UnescapeDataString($Href)
    $parts = $decoded.Split('#', 2)

    if ($parts.Count -lt 2) {
        return $null
    }

    $pathPart = $parts[0].Trim()
    $anchor = $parts[1].Trim()

    if (-not $pathPart -or -not $anchor) {
        return $null
    }

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path (Split-Path $SourcePath) $pathPart))

    return [pscustomobject]@{
        FullPath = $fullPath
        RelPath = Get-RepoRelativePath -Path $fullPath
        Anchor = $anchor
    }
}

function Get-LinkMatches {
    param([string]$Line)

    $regex = '(?<!!)\[(?<text>[^\]]+)\]\((?<href>[^)]+)\)'
    return [regex]::Matches($Line, $regex)
}

function Get-SpecForwardRefs {
    param([string]$SpecPath)

    $lines = Get-Content -Path $SpecPath -Encoding UTF8
    $currentSectionAnchor = ''
    $refs = @()

    $inFence = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) {
            continue
        }
        if ($line -match '^(#+)\s+(.+)$') {
            $headingText = $matches[2].Trim()
            if ($headingText -notin @('References', 'Referenced By')) {
                $currentSectionAnchor = Get-AnchorSlug -Heading $headingText
            }
        }

        foreach ($match in (Get-LinkMatches -Line $line)) {
            $href = $match.Groups['href'].Value
            if ($href -notmatch 'AGENTS\.md#') {
                continue
            }

            $target = Resolve-LinkTarget -SourcePath $SpecPath -Href $href
            if (-not $target) {
                continue
            }

            $refs += [pscustomobject]@{
                SpecPath = $SpecPath
                SpecRel = Get-RepoRelativePath -Path $SpecPath
                SpecAnchor = $currentSectionAnchor
                LineNumber = $i + 1
                AgentRel = $target.RelPath
                AgentFull = $target.FullPath
                AgentAnchor = $target.Anchor
            }
        }
    }

    return $refs
}

function Get-AgentsBackRefs {
    param([string]$AgentPath)

    $lines = Get-Content -Path $AgentPath -Encoding UTF8
    $currentSectionAnchor = ''
    $inReferencedBy = $false
    $refs = @()

    $inFence = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) {
            continue
        }

        if ($line -match '^(#+)\s+(.+)$') {
            $level = $matches[1].Length
            $headingText = $matches[2].Trim()

            $inReferencedBy = $headingText -eq 'Referenced By'

            if ($headingText -notin @('Referenced By', 'References')) {
                if ($level -le 2) {
                    $currentSectionAnchor = Get-AnchorSlug -Heading $headingText
                }
            }
        }

        if (-not $inReferencedBy) {
            continue
        }

        foreach ($match in (Get-LinkMatches -Line $line)) {
            $href = $match.Groups['href'].Value
            if ($href -notmatch 'openspec/specs/.+\.md#') {
                continue
            }

            $target = Resolve-LinkTarget -SourcePath $AgentPath -Href $href
            if (-not $target) {
                continue
            }

            $refs += [pscustomobject]@{
                AgentPath = $AgentPath
                AgentRel = Get-RepoRelativePath -Path $AgentPath
                AgentAnchor = $currentSectionAnchor
                LineNumber = $i + 1
                SpecRel = $target.RelPath
                SpecFull = $target.FullPath
                SpecAnchor = $target.Anchor
            }
        }
    }

    return $refs
}

$specRoot = Join-Path $RepoRoot 'openspec\specs'
$specFiles = @()
if (Test-Path $specRoot) {
    $specFiles = Get-ChildItem -Path $specRoot -Recurse -Filter '*.md' -File
}
$agentFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter 'AGENTS.md' -File

Write-Host 'Validating OpenSpec cross-references...'
Write-Host ''
Write-Host ("Scanning {0} spec files..." -f $specFiles.Count)
Write-Host ("Scanning {0} AGENTS.md files..." -f $agentFiles.Count)
Write-Host ''

$forwardRefs = @()
foreach ($spec in $specFiles) {
    $forwardRefs += Get-SpecForwardRefs -SpecPath $spec.FullName
}

$backRefs = @()
foreach ($agent in $agentFiles) {
    $backRefs += Get-AgentsBackRefs -AgentPath $agent.FullName
}

$forwardMap = @{}
foreach ($ref in $forwardRefs) {
    $key = "{0}|{1}|{2}|{3}" -f $ref.SpecRel, $ref.SpecAnchor, $ref.AgentRel, $ref.AgentAnchor
    $forwardMap[$key] = $ref
}

$backMap = @{}
foreach ($ref in $backRefs) {
    $key = "{0}|{1}|{2}|{3}" -f $ref.SpecRel, $ref.SpecAnchor, $ref.AgentRel, $ref.AgentAnchor
    $backMap[$key] = $ref
}

$broken = @()
$orphans = @()
$brokenCount = 0
$orphanCount = 0

foreach ($ref in $forwardRefs) {
    if (-not (Test-Path $ref.AgentFull)) {
        $brokenCount++
        $broken += @(
            "[BROKEN] {0}:{1}" -f $ref.SpecRel, $ref.LineNumber,
            "         -> {0}#{1}" -f $ref.AgentRel, $ref.AgentAnchor,
            '         Target file not found.'
        )
        continue
    }

    $agentAnchors = Get-FileAnchors -Path $ref.AgentFull
    if (-not $agentAnchors.Contains($ref.AgentAnchor)) {
        $brokenCount++
        $broken += @(
            "[BROKEN] {0}:{1}" -f $ref.SpecRel, $ref.LineNumber,
            "         -> {0}#{1}" -f $ref.AgentRel, $ref.AgentAnchor,
            '         Anchor not found.'
        )
        continue
    }

    $key = "{0}|{1}|{2}|{3}" -f $ref.SpecRel, $ref.SpecAnchor, $ref.AgentRel, $ref.AgentAnchor
    if (-not $backMap.ContainsKey($key)) {
        $orphanCount++
        $orphans += @(
            "[ORPHAN] {0}:{1} #{2}" -f $ref.SpecRel, $ref.LineNumber, $ref.SpecAnchor,
            "         Missing back-ref in {0}." -f $ref.AgentRel
        )
    }
}

foreach ($ref in $backRefs) {
    if (-not (Test-Path $ref.SpecFull)) {
        $brokenCount++
        $broken += @(
            "[BROKEN] {0}:{1} #{2}" -f $ref.AgentRel, $ref.LineNumber, $ref.AgentAnchor,
            "         -> {0}#{1}" -f $ref.SpecRel, $ref.SpecAnchor,
            '         Target spec not found.'
        )
        continue
    }

    $specAnchors = Get-FileAnchors -Path $ref.SpecFull
    if (-not $specAnchors.Contains($ref.SpecAnchor)) {
        $brokenCount++
        $broken += @(
            "[BROKEN] {0}:{1} #{2}" -f $ref.AgentRel, $ref.LineNumber, $ref.AgentAnchor,
            "         -> {0}#{1}" -f $ref.SpecRel, $ref.SpecAnchor,
            '         Anchor not found.'
        )
        continue
    }

    $key = "{0}|{1}|{2}|{3}" -f $ref.SpecRel, $ref.SpecAnchor, $ref.AgentRel, $ref.AgentAnchor
    if (-not $forwardMap.ContainsKey($key)) {
        $orphanCount++
        $orphans += @(
            "[ORPHAN] {0}:{1} #{2}" -f $ref.AgentRel, $ref.LineNumber, $ref.AgentAnchor,
            "         Spec missing forward ref: {0}#{1}" -f $ref.SpecRel, $ref.SpecAnchor
        )
    }
}

$graph = @{}
function Add-Edge {
    param([string]$From, [string]$To)

    if (-not $graph.ContainsKey($From)) {
        $graph[$From] = New-Object System.Collections.Generic.List[string]
    }

    if (-not $graph[$From].Contains($To)) {
        $graph[$From].Add($To)
    }
}

foreach ($ref in $forwardRefs) {
    Add-Edge -From $ref.SpecRel -To $ref.AgentRel
}

foreach ($ref in $backRefs) {
    Add-Edge -From $ref.AgentRel -To $ref.SpecRel
}

$cycleSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$visited = @{}
$onStack = @{}
$stack = New-Object System.Collections.Generic.List[string]

function Visit-Node {
    param([string]$Node)

    $visited[$Node] = $true
    $onStack[$Node] = $true
    $stack.Add($Node)

    if ($graph.ContainsKey($Node)) {
        foreach ($neighbor in $graph[$Node]) {
            if (-not $visited.ContainsKey($neighbor)) {
                Visit-Node -Node $neighbor
            } elseif ($onStack.ContainsKey($neighbor)) {
                $startIndex = $stack.IndexOf($neighbor)
                if ($startIndex -ge 0) {
                    $cycle = $stack[$startIndex..($stack.Count - 1)] + $neighbor
                    if ($cycle.Count -ge 4) {
                        $cycleSet.Add(($cycle -join ' -> ')) | Out-Null
                    }
                }
            }
        }
    }

    $stack.RemoveAt($stack.Count - 1)
    $onStack.Remove($Node)
}

foreach ($node in $graph.Keys) {
    if (-not $visited.ContainsKey($node)) {
        Visit-Node -Node $node
    }
}

if ($broken.Count -gt 0) {
    Write-Host 'ERRORS:'
    foreach ($line in $broken) {
        Write-Host "  $line"
    }
    Write-Host ''
}

if ($orphans.Count -gt 0) {
    Write-Host 'ORPHANS:'
    foreach ($line in $orphans) {
        Write-Host "  $line"
    }
    Write-Host ''
}

if ($cycleSet.Count -gt 0) {
    Write-Host 'WARNINGS:'
    foreach ($cycle in $cycleSet) {
        Write-Host "  [CYCLE] $cycle"
    }
    Write-Host ''
}

$errorCount = $brokenCount + $orphanCount
Write-Host ("Summary: {0} errors, {1} warnings" -f $errorCount, $cycleSet.Count)

if ($brokenCount -gt 0) {
    exit 1
}

if ($orphanCount -gt 0) {
    exit 2
}

exit 0
