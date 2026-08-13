<#
.SYNOPSIS
	Diff-scoped comment-hygiene gate for FieldWorks C# and PowerShell comments.

.DESCRIPTION
	Enforces the mechanical banned-content categories, plus a one-line cap on
	implementation comments, against lines a diff ADDS, not the whole
	repository. Legacy comments are never flagged unless their line is
	touched again.

.PARAMETER BaseRef
	Git ref to diff against. Defaults to the PR base in CI
	(GITHUB_EVENT_PULL_REQUEST_BASE_SHA, then GITHUB_BASE_REF), else the
	local merge-base with the origin default branch.

.PARAMETER Full
	Report-only mode: scans every tracked .cs/.ps1 file at HEAD instead of
	the diff, and never exits non-zero.

.PARAMETER List
	Show every violation. Implied by -Full.

.EXAMPLE
	Build/Agent/comment-hygiene.ps1
	Gate the current diff against the local merge-base with the default branch.

.EXAMPLE
	Build/Agent/comment-hygiene.ps1 -Full -List
	Report every mechanical violation in the whole repo, without failing.
#>
[CmdletBinding()]
param(
	[string] $BaseRef,
	[switch] $Full,
	[switch] $List
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Import-Module (Join-Path $PSScriptRoot 'CommentHygiene.psm1') -Force

function Test-ExcludedPath {
	param([string] $Path)
	return ($Path -match '\.g\.cs$') -or ($Path -match 'Designer\.cs$')
}

function Test-Utf8Bom {
	param([string] $Path)
	$bytes = [System.IO.File]::ReadAllBytes($Path)
	return ($bytes.Length -ge 3) -and ($bytes[0] -eq 0xEF) -and ($bytes[1] -eq 0xBB) -and ($bytes[2] -eq 0xBF)
}

function ConvertTo-RepoPath {
	param([string] $RelativePath)
	return Join-Path $repoRoot ($RelativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
}

function Resolve-BaseRef {
	param([string] $Explicit)

	if (-not [string]::IsNullOrWhiteSpace($Explicit)) { return $Explicit }
	if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_EVENT_PULL_REQUEST_BASE_SHA)) { return $env:GITHUB_EVENT_PULL_REQUEST_BASE_SHA }
	if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_BASE_REF)) { return "origin/$env:GITHUB_BASE_REF" }

	$defaultBranch = 'main'
	foreach ($remoteLine in (git remote show origin 2>$null)) {
		if ($remoteLine -match 'HEAD branch:\s*(\S+)') {
			$defaultBranch = $Matches[1]
			break
		}
	}
	return "origin/$defaultBranch"
}

function Get-AddedLineFilter {
	param([string] $Base)

	$diff = git diff --unified=0 "$Base...HEAD" -- '*.cs' '*.ps1' '*.psm1' 2>$null
	if ($LASTEXITCODE -ne 0) {
		throw "git diff against '$Base' failed. Is the base ref fetched? (CI needs fetch-depth: 0.)"
	}

	$filter = @{}
	$currentFile = $null
	$currentLine = 0

	foreach ($rawLine in $diff) {
		if ($rawLine -match '^\+\+\+ b/(.+)$') {
			$currentFile = ConvertTo-RepoPath $Matches[1]
			continue
		}
		if ($rawLine -match '^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@') {
			$currentLine = [int]$Matches[1]
			continue
		}
		if ($null -eq $currentFile) { continue }
		if ($rawLine.StartsWith('+') -and -not $rawLine.StartsWith('+++')) {
			if (-not (Test-ExcludedPath $currentFile)) {
				if (-not $filter.ContainsKey($currentFile)) {
					$filter[$currentFile] = [System.Collections.Generic.HashSet[int]]::new()
				}
				[void]$filter[$currentFile].Add($currentLine)
			}
			$currentLine++
		}
	}

	return $filter
}

function Write-Violation {
	param($Violation)
	$relative = $Violation.File.Substring($repoRoot.Length + 1)
	Write-Host ("  {0}:{1} [{2}] {3}" -f $relative, $Violation.Line, $Violation.Category, $Violation.Text)
}

if ($Full) {
	$files = git ls-files '*.cs' '*.ps1' '*.psm1' | ForEach-Object { ConvertTo-RepoPath $_ } | Where-Object { -not (Test-ExcludedPath $_) }
	$violations = Get-CommentHygieneViolations -Files $files

	Write-Host "comment-hygiene -Full: $($violations.Count) violation(s) across $($files.Count) file(s)"
	foreach ($v in $violations) { Write-Violation $v }
	exit 0
}

$base = Resolve-BaseRef -Explicit $BaseRef
Write-Host "comment-hygiene: scanning lines added since $base"

$lineFilter = Get-AddedLineFilter -Base $base
if ($lineFilter.Count -eq 0) {
	Write-Host 'comment-hygiene: no added .cs/.ps1/.psm1 lines to check.'
	exit 0
}

$violations = Get-CommentHygieneViolations -Files @($lineFilter.Keys) -LineFilter $lineFilter

# CI can't commit a fix back, so only auto-fix where a human can review and commit it.
$isCI = ($env:GITHUB_ACTIONS -eq 'true') -or ($env:CI -eq 'true')

$remainingViolations = New-Object System.Collections.ArrayList
$fixedFiles = @{}
$fixedCount = 0

foreach ($v in $violations) {
	if ($isCI -or $v.Category -ne 'non-ascii-punctuation') {
		[void]$remainingViolations.Add($v)
		continue
	}

	$hadBom = Test-Utf8Bom -Path $v.File
	$fileLines = @(Get-Content -LiteralPath $v.File -Encoding UTF8)
	$fixedLine = Repair-CommentLine -Line $fileLines[$v.Line - 1]
	if ($null -eq $fixedLine) {
		[void]$remainingViolations.Add($v)
		continue
	}

	$fileLines[$v.Line - 1] = $fixedLine
	Set-CommentHygieneFileContent -Path $v.File -Lines $fileLines -Utf8Bom $hadBom
	$fixedFiles[$v.File] = $true
	$fixedCount++
}

$violations = $remainingViolations.ToArray()

if ($fixedCount -gt 0) {
	Write-Host "comment-hygiene: auto-fixed $fixedCount non-ascii-punctuation comment line(s) in $($fixedFiles.Keys.Count) file(s) (review and include in your commit)." -ForegroundColor Yellow
}

if ($violations.Count -eq 0) {
	Write-Host 'comment-hygiene: clean.'
	exit 0
}

Write-Host ''
Write-Host "comment-hygiene: $($violations.Count) violation(s) in added lines" -ForegroundColor Red
foreach ($v in $violations) { Write-Violation $v }
Write-Host ''
Write-Host 'Fix per .claude/skills/fieldworks-code-commenting/SKILL.md, or rewrite the comment.' -ForegroundColor Red
exit 1
