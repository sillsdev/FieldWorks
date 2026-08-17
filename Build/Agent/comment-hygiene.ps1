<#
.SYNOPSIS
	Diff-scoped comment-hygiene check for FieldWorks source and project comments.

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

.PARAMETER Advisory
	Report violations and exit 0 instead of failing. Under GitHub Actions each
	violation is also emitted as a warning annotation, so it lands on the pull
	request's diff without breaking the build. build.ps1 and test.ps1 pass this
	whenever -CommentHygiene was not requested.

.PARAMETER ReportPath
	Write the scan result to this path as JSON: the base ref, the advisory
	flag, and one record per violation carrying a repo-relative path, line,
	category, and comment text. A run that skips its scan writes nothing, so
	the file is present only when a scan produced it.

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
	[switch] $List,
	[switch] $Advisory,
	[string] $ReportPath
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

	# Reads local origin/HEAD, not `git remote show origin`, which hits the network
	# every run. --quiet keeps git's stderr out of PowerShell's error stream, where
	# it terminates instead of falling through.
	$originHead = git rev-parse --verify --quiet --abbrev-ref origin/HEAD
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

	# git diff reports tracked files only, so a new file an agent has not staged yet
	# would go unscanned. Every line of an untracked in-scope file counts as added.
	$untracked = git ls-files --others --exclude-standard -- $scopedGlobs 2>$null
	if ($LASTEXITCODE -eq 0) {
		foreach ($relative in $untracked) {
			if ([string]::IsNullOrWhiteSpace($relative)) { continue }
			$path = ConvertTo-RepoPath $relative
			if ((Test-ExcludedPath $path) -or -not (Test-Path -LiteralPath $path)) { continue }

			$lineCount = ([System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)).Count
			if ($lineCount -eq 0) { continue }
			if (-not $filter.ContainsKey($path)) {
				$filter[$path] = [System.Collections.Generic.HashSet[int]]::new()
			}
			for ($i = 1; $i -le $lineCount; $i++) { [void]$filter[$path].Add($i) }
		}
	}

	return $filter
}

function Write-Violation {
	param($Violation)
	$relative = $Violation.File.Substring($repoRoot.Length + 1)
	Write-Host ("  {0}:{1} [{2}] {3}" -f $relative, $Violation.Line, $Violation.Category, $Violation.Text)

	# An annotation puts the violation on the pull request's diff, where a
	# reviewer sees it without the job failing. Forward slashes: GitHub matches
	# annotation paths against the repository's own separator.
	if ($Advisory -and $env:GITHUB_ACTIONS -eq 'true') {
		Write-Host ("::warning file={0},line={1},title=comment-hygiene ({2})::{3}" -f `
			($relative -replace '\\', '/'), $Violation.Line, $Violation.Category, $Violation.Text)
	}
}

function Write-ScanReport {
	<#
	.SYNOPSIS
		Writes the scan result to -ReportPath as JSON. Does nothing when the
		caller asked for no report.

	.PARAMETER Base
		The ref the scan diffed against, recorded verbatim in the report.
	#>
	param(
		[object[]] $Violations,
		[string] $Base
	)

	if ([string]::IsNullOrWhiteSpace($ReportPath)) { return }

	# PowerShell's current location and .NET's working directory can differ; New-Item
	# reads the first and WriteAllText the second, so anchor a relative path once.
	$path = $ReportPath
	if (-not [System.IO.Path]::IsPathRooted($path)) {
		$path = Join-Path (Get-Location).Path $path
	}

	$directory = Split-Path -Path $path -Parent
	if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
		New-Item -ItemType Directory -Path $directory -Force | Out-Null
	}

	$records = New-Object System.Collections.ArrayList
	foreach ($v in $Violations) {
		[void]$records.Add([ordered]@{
			file     = ($v.File.Substring($repoRoot.Length + 1) -replace '\\', '/')
			line     = $v.Line
			category = $v.Category
			text     = $v.Text
		})
	}

	$report = [ordered]@{
		base           = $Base
		advisory       = [bool]$Advisory
		violationCount = $records.Count
		violations     = @($records.ToArray())
	}

	# UTF8Encoding($false): a byte-order mark ahead of the opening brace makes the file
	# unparsable to a plain JSON reader.
	[System.IO.File]::WriteAllText($path, ($report | ConvertTo-Json -Depth 5),
		(New-Object System.Text.UTF8Encoding($false)))
	Write-Host "comment-hygiene: wrote $($records.Count) violation record(s) to $path"
}

if ($Full) {
	$files = git ls-files $scopedGlobs | ForEach-Object { ConvertTo-RepoPath $_ } | Where-Object { -not (Test-ExcludedPath $_) }
	$violations = Get-CommentHygieneViolations -Files $files

	Write-Host "comment-hygiene -Full: $($violations.Count) violation(s) across $($files.Count) file(s)"
	foreach ($v in $violations) { Write-Violation $v }
	Write-ScanReport -Violations $violations -Base 'HEAD'
	exit 0
}

# Several steps invoke this script over one tree, which would annotate every violation
# once per step. The first run marks the job, and presetting the marker suppresses
# reporting for a whole job.
if ($env:GITHUB_ACTIONS -eq 'true') {
	if ($env:FW_COMMENT_HYGIENE_REPORTED -eq '1') {
		Write-Host 'comment-hygiene: reporting suppressed for this job.'
		exit 0
	}
	if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_ENV)) {
		# UTF8Encoding($false): appending a BOM mid-file would corrupt the
		# environment file the runner parses when the step ends.
		[System.IO.File]::AppendAllText($env:GITHUB_ENV, "FW_COMMENT_HYGIENE_REPORTED=1`n",
			(New-Object System.Text.UTF8Encoding($false)))
	}
}

$base = Resolve-BaseRef -Explicit $BaseRef
Write-Host "comment-hygiene: scanning lines added since $base"

$lineFilter = Get-AddedLineFilter -Base $base
if ($lineFilter.Count -eq 0) {
	Write-Host 'comment-hygiene: no added lines in scope to check.'
	Write-ScanReport -Violations @() -Base $base
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
Write-ScanReport -Violations $violations -Base $base

if ($fixedCount -gt 0 -or $wrappedCount -gt 0) {
	Write-Host ("comment-hygiene: auto-fixed {0} punctuation and re-wrapped {1} over-long comment line(s) in {2} file(s) (review and include in your commit)." -f $fixedCount, $wrappedCount, $fixedFiles.Keys.Count) -ForegroundColor Yellow
}

if ($violations.Count -eq 0) {
	Write-Host 'comment-hygiene: clean.'
	exit 0
}

Write-Host ''

if ($Advisory) {
	Write-Host "comment-hygiene: $($violations.Count) advisory violation(s) in added lines" -ForegroundColor Yellow
	foreach ($v in $violations) { Write-Violation $v }
	Write-Host ''
	Write-Host 'Advisory only. Pass -CommentHygiene to build.ps1 or test.ps1 to enforce these.' -ForegroundColor Yellow
	exit 0
}

Write-Host "comment-hygiene: $($violations.Count) violation(s) in added lines" -ForegroundColor Red
foreach ($v in $violations) { Write-Violation $v }
Write-Host ''
Write-Host 'Fix per .claude/skills/fieldworks-code-commenting/SKILL.md, or rewrite the comment.' -ForegroundColor Red
exit 1
