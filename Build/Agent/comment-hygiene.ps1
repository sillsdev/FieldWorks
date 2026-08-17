<#
.SYNOPSIS
	Diff-scoped comment-hygiene gate for FieldWorks source and project comments.

.DESCRIPTION
	Enforces the mechanical banned-content categories, plus a one-line cap on
	implementation comments, against lines a diff ADDS, not the whole
	repository. Legacy comments are never flagged unless their line is
	touched again. Covers C#/C/C++/IDL, PowerShell, and the XML comments in
	project files and Avalonia views -- see Get-CommentHygieneLanguage.

.PARAMETER BaseRef
	Git ref to diff against. Defaults to the PR base in CI
	(GITHUB_EVENT_PULL_REQUEST_BASE_SHA, then GITHUB_BASE_REF), else the
	local merge-base with the origin default branch.

.PARAMETER Full
	Report-only mode: scans every tracked file in scope at HEAD instead of
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

# Kept in sync with Get-CommentHygieneLanguage's recognized extensions.
$scopedGlobs = @(
	'*.cs', '*.ps1', '*.psm1',
	'*.cpp', '*.cxx', '*.cc', '*.c', '*.h', '*.hpp', '*.idl',
	'*.csproj', '*.vcxproj', '*.vcproj', '*.props', '*.targets', '*.proj', '*.axaml', '*.xaml'
)

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

	# git rev-parse against the local origin/HEAD ref, not `git remote show origin`: the
	# latter contacts the remote (a real network round-trip) on every local gate run.
	$originHead = git rev-parse --abbrev-ref origin/HEAD 2>$null
	if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($originHead)) { return $originHead.Trim() }
	return 'origin/main'
}

function Get-AddedLineFilter {
	param([string] $Base)

	# Diff the merge base against the WORKING TREE, not HEAD: the scan reads files
	# from disk, so a committed filter points at stale lines once an auto-fix
	# rewrites one. CI's tree matches HEAD either way.
	$mergeBase = git merge-base $Base HEAD 2>$null
	if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($mergeBase)) {
		throw "git merge-base against '$Base' failed. Is the base ref fetched? (CI needs fetch-depth: 0.)"
	}

	$diff = git diff --unified=0 $mergeBase.Trim() -- $scopedGlobs 2>$null
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
	$files = git ls-files $scopedGlobs | ForEach-Object { ConvertTo-RepoPath $_ } | Where-Object { -not (Test-ExcludedPath $_) }
	$violations = Get-CommentHygieneViolations -Files $files

	Write-Host "comment-hygiene -Full: $($violations.Count) violation(s) across $($files.Count) file(s)"
	foreach ($v in $violations) { Write-Violation $v }
	exit 0
}

$base = Resolve-BaseRef -Explicit $BaseRef
Write-Host "comment-hygiene: scanning lines added since $base"

$lineFilter = Get-AddedLineFilter -Base $base
if ($lineFilter.Count -eq 0) {
	Write-Host 'comment-hygiene: no added lines in scope to check.'
	exit 0
}

$violations = Get-CommentHygieneViolations -Files @($lineFilter.Keys) -LineFilter $lineFilter

# CI can't commit a fix back, so only auto-fix where a human can review and commit it.
$isCI = ($env:GITHUB_ACTIONS -eq 'true') -or ($env:CI -eq 'true')

$remainingViolations = New-Object System.Collections.ArrayList
$fixedFiles = @{}
$fixedCount = 0
$wrappedCount = 0

$autoFixable = @('non-ascii-punctuation', 'comment-line-too-long')
$pending = New-Object System.Collections.ArrayList
foreach ($v in $violations) {
	if ($isCI -or ($autoFixable -notcontains $v.Category)) {
		[void]$remainingViolations.Add($v)
		continue
	}
	[void]$pending.Add($v)
}

$editorConfig = Get-CommentHygieneEditorConfig -RepoRoot $repoRoot

# Per file, bottom-up: re-wrapping adds lines and shifts every line number below
# it. Within one line the punctuation fix runs first, since it changes the width
# that decides whether a wrap is still needed.
$sortOrder = @(
	@{ Expression = 'Line'; Descending = $true },
	@{ Expression = { if ($_.Category -eq 'non-ascii-punctuation') { 0 } else { 1 } }; Descending = $false }
)

foreach ($group in ($pending | Group-Object File)) {
	$path = $group.Name
	$language = Get-CommentHygieneLanguage -Path $path
	$hadBom = Test-Utf8Bom -Path $path
	$fileLines = New-Object 'System.Collections.Generic.List[string]'
	$fileLines.AddRange([string[]][System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8))
	$changed = $false

	foreach ($v in ($group.Group | Sort-Object $sortOrder)) {
		$index = $v.Line - 1

		if ($v.Category -eq 'non-ascii-punctuation') {
			$fixedLine = Repair-CommentLine -Line $fileLines[$index]
			if ($null -eq $fixedLine) { [void]$remainingViolations.Add($v); continue }
			$fileLines[$index] = $fixedLine
			$changed = $true
			$fixedCount++
			continue
		}

		$width = Get-CommentDisplayWidth -Line $fileLines[$index] -TabWidth $editorConfig.TabWidth
		if ($width -le $editorConfig.MaxLineLength) { continue }

		$wrapped = Format-CommentLineWrap -Line $fileLines[$index] -Language $language `
			-MaxWidth $editorConfig.MaxLineLength -TabWidth $editorConfig.TabWidth
		if ($null -eq $wrapped) { [void]$remainingViolations.Add($v); continue }

		$fileLines.RemoveAt($index)
		$fileLines.InsertRange($index, [string[]]$wrapped)
		$changed = $true
		$wrappedCount++
	}

	if ($changed) {
		Set-CommentHygieneFileContent -Path $path -Lines $fileLines.ToArray() -Utf8Bom $hadBom
		$fixedFiles[$path] = $true
	}
}

$violations = $remainingViolations.ToArray()

if ($fixedCount -gt 0 -or $wrappedCount -gt 0) {
	Write-Host ("comment-hygiene: auto-fixed {0} punctuation and re-wrapped {1} over-long comment line(s) in {2} file(s) (review and include in your commit)." -f $fixedCount, $wrappedCount, $fixedFiles.Keys.Count) -ForegroundColor Yellow
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
