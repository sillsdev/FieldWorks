<#
.SYNOPSIS
    Migrate FieldWorks csproj files to NuGet Central Package Management (CPM).

.DESCRIPTION
    Strips the Version attribute from all PackageReference elements in csproj files
    under Src/. After migration, versions are centrally managed in Directory.Packages.props.

    Build/Src/ and FLExInstaller/ are excluded (they opt out of CPM via their own
    Directory.Packages.props files).

.PARAMETER RootPath
    Root directory to scan for csproj files. Defaults to Src/ relative to repo root.

.PARAMETER Validate
    Instead of migrating, scan for any remaining Version= attributes and report them.
    Exits with code 1 if any are found.

.EXAMPLE
    # Dry run — show what would change
    .\Migrate-ToCpm.ps1 -WhatIf

    # Execute migration
    .\Migrate-ToCpm.ps1

    # Validate post-migration
    .\Migrate-ToCpm.ps1 -Validate
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RootPath,
    [switch]$Validate
)

$ErrorActionPreference = 'Stop'

# Default to Src/ relative to repo root (two levels up from scripts/Agent/)
if (-not $RootPath) {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
    $RootPath = Join-Path $repoRoot 'Src'
}

if (-not (Test-Path $RootPath)) {
    Write-Error "Root path not found: $RootPath"
    return
}

# Regex: match Version="..." (with optional whitespace around =) on a PackageReference element.
# Captures everything before the Version attribute so we can keep it.
# Handles both self-closing /> and multi-line elements.
$pattern = '(<PackageReference\s[^>]*?)\s+Version\s*=\s*"[^"]*"'

if ($Validate) {
    Write-Host "Validating CPM compliance in: $RootPath" -ForegroundColor Cyan
    $remaining = Get-ChildItem -Path $RootPath -Filter '*.csproj' -Recurse |
        ForEach-Object {
            $matches = Select-String -Path $_.FullName -Pattern '<PackageReference\s[^>]*Version\s*=' -AllMatches
            foreach ($m in $matches) {
                # Skip VersionOverride (intentional per-project overrides)
                if ($m.Line -match 'VersionOverride') { continue }
                [PSCustomObject]@{
                    File = $_.FullName
                    Line = $m.LineNumber
                    Content = $m.Line.Trim()
                }
            }
        }

    if ($remaining) {
        Write-Warning "Found $($remaining.Count) PackageReference entries still containing Version="
        foreach ($r in $remaining) {
            Write-Host "  $($r.File):$($r.Line): $($r.Content)" -ForegroundColor Yellow
        }
        exit 1
    }
    Write-Host "`n[OK] All PackageReference entries are versionless (CPM compliant)" -ForegroundColor Green
    exit 0
}

Write-Host "Migrating csproj files to CPM in: $RootPath" -ForegroundColor Cyan
Write-Host ""

$totalFiles = 0
$totalRefs = 0
$errors = @()

Get-ChildItem -Path $RootPath -Filter '*.csproj' -Recurse | ForEach-Object {
    $file = $_
    try {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $original = $content

        # Count matches before replacing
        $matchCount = ([regex]::Matches($content, $pattern)).Count

        if ($matchCount -eq 0) { return }

        # Remove Version="..." from PackageReference elements
        $content = [regex]::Replace($content, $pattern, '$1')

        if ($content -ne $original) {
            $relativePath = $file.FullName.Substring($RootPath.Length).TrimStart('\', '/')
            if ($PSCmdlet.ShouldProcess($relativePath, "Remove Version from $matchCount PackageReference(s)")) {
                [System.IO.File]::WriteAllText($file.FullName, $content)
                Write-Host "  $($relativePath): $matchCount reference(s) updated" -ForegroundColor Green
            } else {
                Write-Host "  $($relativePath): $matchCount reference(s) would be updated" -ForegroundColor Yellow
            }
            $totalFiles++
            $totalRefs += $matchCount
        }
    } catch {
        $errors += "$($file.FullName): $($_.Exception.Message)"
        Write-Warning "Error processing $($file.FullName): $($_.Exception.Message)"
    }
}

Write-Host ""
if ($errors.Count -gt 0) {
    Write-Warning "$($errors.Count) file(s) had errors"
}
Write-Host "[OK] Updated $totalRefs PackageReference(s) across $totalFiles file(s)" -ForegroundColor Green
