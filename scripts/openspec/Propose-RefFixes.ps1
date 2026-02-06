<#
.SYNOPSIS
    Propose fixes for broken or missing OpenSpec references.

.DESCRIPTION
    Scans specs and AGENTS.md for cross-references, reports issues, and can
    apply safe fixes (anchor corrections, missing ref blocks) with --Apply.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [switch]$Apply
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
        return $rel.Replace('\\', '/')
    }

    return $full.Replace('\\', '/')
}

function Get-AnchorSlug {
    param([string]$Heading)

    $clean = $Heading.Trim()
    $clean = $clean -replace '[^A-Za-z0-9 \-]', ''
    $clean = $clean.ToLowerInvariant()
    return ($clean -replace '\s', '-')
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

function Get-FileAnchors {
    param([string]$Path)

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

function Get-SectionRanges {
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
        if ($line -match '^(#{2,3})\s+(.+)$') {
            $level = $matches[1].Length
            $title = $matches[2].Trim()
            $slug = Get-AnchorSlug -Heading $title
            $sections += [pscustomobject]@{
                Index = $i
                Level = $level
                Title = $title
                Anchor = $slug
            }
        }
    }

    return $sections
}

function Find-SectionRange {
    param(
        [string[]]$Lines,
        [string]$Anchor
    )

    $sections = Get-SectionRanges -Lines $Lines
    $match = $sections | Where-Object { $_.Anchor -eq $Anchor } | Select-Object -First 1
    if (-not $match) {
        return $null
    }

    $endIndex = $Lines.Count
    foreach ($section in $sections) {
        if ($section.Index -le $match.Index) {
            continue
        }
        if ($section.Level -le $match.Level) {
            $endIndex = $section.Index
            break
        }
    }

    return [pscustomobject]@{
        Start = $match.Index
        End = $endIndex
        Title = $match.Title
    }
}

function Get-ClosestAnchor {
    param(
        [string]$Anchor,
        [System.Collections.Generic.HashSet[string]]$Anchors
    )

    if ($Anchors.Contains($Anchor)) {
        return $Anchor
    }

    $normalized = ($Anchor -replace '-', '').ToLowerInvariant()
    foreach ($candidate in $Anchors) {
        if (($candidate -replace '-', '').ToLowerInvariant() -eq $normalized) {
            return $candidate
        }
    }

    return $null
}

function Get-RelativeLink {
    param(
        [string]$FromFile,
        [string]$ToFile
    )

    $fromDir = Split-Path -Path $FromFile
    $fromUri = New-Object System.Uri(($fromDir + [System.IO.Path]::DirectorySeparatorChar))
    $toUri = New-Object System.Uri($ToFile)
    $relative = $fromUri.MakeRelativeUri($toUri).ToString()
    return [System.Uri]::UnescapeDataString($relative).Replace('\\', '/')
}

function Insert-ReferenceBlock {
    param(
        [string]$Path,
        [string]$SectionAnchor,
        [string]$HeadingText,
        [string]$LinkText,
        [string]$LinkHref,
        [string]$BlockHeading
    )

    $lines = Get-Content -Path $Path -Encoding UTF8
    $range = Find-SectionRange -Lines $lines -Anchor $SectionAnchor
    if (-not $range) {
        return $false
    }

    $blockIndex = -1
    $insertIndex = $range.End
    for ($i = $range.Start + 1; $i -lt $range.End; $i++) {
        if ($lines[$i] -match ('^#{3,4}\s+' + [regex]::Escape($BlockHeading) + '\s*$')) {
            $blockIndex = $i
            $insertIndex = $i + 1
            for ($j = $i + 1; $j -lt $range.End; $j++) {
                if ($lines[$j] -match '^\s*-\s+') {
                    $insertIndex = $j + 1
                    continue
                }
                if ($lines[$j] -match '^#{2,4}\s+') {
                    break
                }
                if ($lines[$j].Trim() -eq '') {
                    $insertIndex = $j + 1
                    break
                }
            }
            break
        }
    }

    $bullet = "- [$LinkText]($LinkHref) - $HeadingText"
    $existing = $lines | Where-Object { $_ -eq $bullet }
    if ($existing) {
        return $false
    }

    $newLines = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($i -eq $insertIndex) {
            if ($blockIndex -lt 0) {
                $newLines.Add('')
                $newLines.Add("### $BlockHeading")
                $newLines.Add('')
            }
            $newLines.Add($bullet)
        }
        $newLines.Add($lines[$i])
    }

    Set-Content -Path $Path -Value $newLines -Encoding UTF8
    return $true
}

function Get-SpecForwardRefs {
    param([string]$SpecPath)

    $lines = Get-Content -Path $SpecPath -Encoding UTF8
    $currentSectionAnchor = ''
    $currentSectionTitle = ''
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
                $currentSectionTitle = $headingText
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
                SpecTitle = $currentSectionTitle
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
    $currentSectionTitle = ''
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
                    $currentSectionTitle = $headingText
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
                AgentTitle = $currentSectionTitle
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

$fixes = New-Object System.Collections.Generic.List[object]

foreach ($ref in $forwardRefs) {
    if (-not (Test-Path $ref.AgentFull)) {
        $fixes.Add([pscustomobject]@{
            Type = 'MissingTarget'
            Message = "Target file not found: {0}" -f $ref.AgentRel
        })
        continue
    }

    $agentAnchors = Get-FileAnchors -Path $ref.AgentFull
    if (-not $agentAnchors.Contains($ref.AgentAnchor)) {
        $suggested = Get-ClosestAnchor -Anchor $ref.AgentAnchor -Anchors $agentAnchors
        if ($suggested) {
            $fixes.Add([pscustomobject]@{
                Type = 'AnchorFix'
                Path = $ref.SpecPath
                Rel = $ref.SpecRel
                Line = $ref.LineNumber
                Old = $ref.AgentAnchor
                New = $suggested
                Message = ("Anchor fix in {0}:{1} -> #" -f $ref.SpecRel, $ref.LineNumber) + $suggested
            })
        }
        continue
    }

    $key = "{0}|{1}|{2}|{3}" -f $ref.SpecRel, $ref.SpecAnchor, $ref.AgentRel, $ref.AgentAnchor
    if (-not $backMap.ContainsKey($key)) {
        $fixes.Add([pscustomobject]@{
            Type = 'MissingBackRef'
            SpecRel = $ref.SpecRel
            SpecFull = $ref.SpecPath
            SpecAnchor = $ref.SpecAnchor
            SpecTitle = $ref.SpecTitle
            AgentRel = $ref.AgentRel
            AgentFull = $ref.AgentFull
            AgentAnchor = $ref.AgentAnchor
            Message = "Missing back-ref in {0}" -f $ref.AgentRel
        })
    }
}

foreach ($ref in $backRefs) {
    if (-not (Test-Path $ref.SpecFull)) {
        $fixes.Add([pscustomobject]@{
            Type = 'MissingTarget'
            Message = "Target file not found: {0}" -f $ref.SpecRel
        })
        continue
    }

    $specAnchors = Get-FileAnchors -Path $ref.SpecFull
    if (-not $specAnchors.Contains($ref.SpecAnchor)) {
        $suggested = Get-ClosestAnchor -Anchor $ref.SpecAnchor -Anchors $specAnchors
        if ($suggested) {
            $fixes.Add([pscustomobject]@{
                Type = 'AnchorFix'
                Path = $ref.AgentPath
                Rel = $ref.AgentRel
                Line = $ref.LineNumber
                Old = $ref.SpecAnchor
                New = $suggested
                Message = ("Anchor fix in {0}:{1} -> #" -f $ref.AgentRel, $ref.LineNumber) + $suggested
            })
        }
        continue
    }

    $key = "{0}|{1}|{2}|{3}" -f $ref.SpecRel, $ref.SpecAnchor, $ref.AgentRel, $ref.AgentAnchor
    if (-not $forwardMap.ContainsKey($key)) {
        $fixes.Add([pscustomobject]@{
            Type = 'MissingForwardRef'
            SpecRel = $ref.SpecRel
            SpecFull = $ref.SpecFull
            SpecAnchor = $ref.SpecAnchor
            AgentRel = $ref.AgentRel
            AgentFull = $ref.AgentPath
            AgentAnchor = $ref.AgentAnchor
            AgentTitle = $ref.AgentTitle
            Message = "Missing forward-ref in {0}" -f $ref.SpecRel
        })
    }
}

if ($fixes.Count -eq 0) {
    Write-Host 'No fixes proposed.'
    exit 0
}

Write-Host ("Proposed fixes for {0} issues:" -f $fixes.Count)
Write-Host ''

$index = 1
foreach ($fix in $fixes) {
    Write-Host ("FIX {0}: {1}" -f $index, $fix.Type)
    Write-Host ("  {0}" -f $fix.Message)
    if ($fix.Type -eq 'AnchorFix') {
        Write-Host ("  File: {0}:{1}" -f $fix.Rel, $fix.Line)
        Write-Host ("  Old:  #{0}" -f $fix.Old)
        Write-Host ("  New:  #{0}" -f $fix.New)
    }
    $index++
    Write-Host ''
}

if (-not $Apply) {
    exit 0
}

$applied = 0
foreach ($fix in $fixes) {
    if ($fix.Type -eq 'AnchorFix') {
        $lines = Get-Content -Path $fix.Path -Encoding UTF8
        $lineIndex = $fix.Line - 1
        if ($lineIndex -ge 0 -and $lineIndex -lt $lines.Count) {
            $oldToken = "#{0}" -f $fix.Old
            $newToken = "#{0}" -f $fix.New
            if ($lines[$lineIndex] -match [regex]::Escape($oldToken)) {
                $lines[$lineIndex] = $lines[$lineIndex] -replace [regex]::Escape($oldToken), $newToken
                Set-Content -Path $fix.Path -Value $lines -Encoding UTF8
                $applied++
            }
        }
        continue
    }

    if ($fix.Type -eq 'MissingBackRef') {
        $specFull = $fix.SpecFull
        $agentFull = $fix.AgentFull
        $specPath = Join-Path $RepoRoot $fix.SpecRel
        $linkPath = Get-RelativeLink -FromFile $agentFull -ToFile $specPath
        $linkHref = "{0}#{1}" -f $linkPath, $fix.SpecAnchor
        $heading = $fix.SpecTitle
        if (-not $heading) {
            $heading = $fix.SpecAnchor
        }
        if (Insert-ReferenceBlock -Path $agentFull -SectionAnchor $fix.AgentAnchor -HeadingText $heading -LinkText $heading -LinkHref $linkHref -BlockHeading 'Referenced By') {
            $applied++
        }
        continue
    }

    if ($fix.Type -eq 'MissingForwardRef') {
        $agentPath = Join-Path $RepoRoot $fix.AgentRel
        $linkPath = Get-RelativeLink -FromFile $fix.SpecFull -ToFile $agentPath
        $linkHref = "{0}#{1}" -f $linkPath, $fix.AgentAnchor
        $heading = $fix.AgentTitle
        if (-not $heading) {
            $heading = $fix.AgentAnchor
        }
        if (Insert-ReferenceBlock -Path $fix.SpecFull -SectionAnchor $fix.SpecAnchor -HeadingText $heading -LinkText $heading -LinkHref $linkHref -BlockHeading 'References') {
            $applied++
        }
    }
}

Write-Host ("Applied {0} fixes." -f $applied)
