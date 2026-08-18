<#
.SYNOPSIS
	Smoke test for Build-CommentHygieneComment.ps1. Run directly under both
	PowerShell 7 and Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Build-CommentHygieneComment.ps1'
$workspace = Join-Path ([System.IO.Path]::GetTempPath()) ('BuildCommentHygieneCommentTest-' + [guid]::NewGuid().ToString('N'))

function Assert-True {
	param(
		[Parameter(Mandatory = $true)]
		[bool]$Condition,
		[Parameter(Mandatory = $true)]
		[string]$Message
	)

	if (-not $Condition) {
		throw $Message
	}
}

function New-Report {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $true)]
		[AllowEmptyCollection()]
		[object[]]$Violations
	)

	$report = [ordered]@{
		base = 'origin/main'
		advisory = $true
		violationCount = $Violations.Count
		violations = @($Violations)
	}

	$directory = Split-Path -Path $Path -Parent
	if (-not (Test-Path -LiteralPath $directory)) {
		New-Item -ItemType Directory -Path $directory -Force | Out-Null
	}

	$encoding = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::WriteAllText($Path, ($report | ConvertTo-Json -Depth 5), $encoding)
}

try {
	$violationsDirectory = Join-Path $workspace 'with-violations'
	$cleanDirectory = Join-Path $workspace 'clean'

	# A pipe and a backtick in the flagged text: both would break the table row or
	# its code span if the renderer passed them through.
	$awkward = 'Chooses `Name` when the flag is set | otherwise the abbreviation'
	$violations = @(
		[ordered]@{ file = 'Src/xWorks/RecordEditView.cs'; line = 42; category = 'comment-too-long'; text = $awkward }
		[ordered]@{ file = 'Build/Agent/Sample.ps1'; line = 7; category = 'doc-pointer'; text = 'See the design note' }
		[ordered]@{ file = 'Src/xWorks/xWorks.csproj'; line = 3; category = 'xml-illegal-double-hyphen'; text = 'Keeps the pack step honest' }
	)

	$violationsReportPath = Join-Path $violationsDirectory 'report.json'
	$violationsCommentPath = Join-Path $violationsDirectory 'comment.md'
	$violationsOutputPath = Join-Path $violationsDirectory 'github-output.txt'
	New-Report -Path $violationsReportPath -Violations $violations

	$violationsResult = & $scriptPath -ReportPath $violationsReportPath -CommentPath $violationsCommentPath `
		-GitHubOutputPath $violationsOutputPath -MaxListed 2 -Repository 'sillsdev/FieldWorks' `
		-RunId '123456' -ServerUrl 'https://github.com' -Sha '0123456789abcdef0123456789abcdef01234567'

	Assert-True (Test-Path -LiteralPath $violationsCommentPath) 'Expected the helper to write the comment markdown file.'
	Assert-True ($violationsResult.HasViolations -eq $true) 'Expected the helper to report violations.'
	Assert-True ($violationsResult.ViolationCount -eq 3) 'Expected the helper to report every violation in the count.'

	$comment = Get-Content -LiteralPath $violationsCommentPath -Raw
	Assert-True ($comment.Contains('3 comment-style violation(s) in the lines this branch adds since `origin/main`.')) `
		'Expected the comment to open with the violation count and the base ref.'
	Assert-True ($comment.Contains('[Src/xWorks/RecordEditView.cs:42](https://github.com/sillsdev/FieldWorks/blob/0123456789abcdef0123456789abcdef01234567/Src/xWorks/RecordEditView.cs#L42)')) `
		'Expected each row to link the file and line at the head commit.'
	Assert-True ($comment.Contains('otherwise the abbreviation')) 'Expected the flagged comment text in the row.'
	Assert-True (-not ($comment -match '(?m)^\| \[Src/xWorks/xWorks\.csproj')) 'Expected -MaxListed to cap the table rows.'
	Assert-True ($comment.Contains('1 more not listed here -- see [this check''s log](https://github.com/sillsdev/FieldWorks/actions/runs/123456).')) `
		'Expected the capped remainder to point at the run log.'

	# Every table row has to keep exactly the four pipes its three columns need, so
	# a pipe inside the flagged text stays escaped rather than splitting the row.
	foreach ($line in ($comment -split '\r?\n')) {
		if (-not $line.StartsWith('| [')) { continue }
		$barePipes = ([regex]::Matches($line, '(?<!\\)\|')).Count
		Assert-True ($barePipes -eq 4) "Expected 4 unescaped pipes in the table row, found $barePipes in: $line"
	}

	$violationsOutput = Get-Content -LiteralPath $violationsOutputPath -Raw
	Assert-True ($violationsOutput.Contains('has_violations=true')) 'Expected has_violations=true in the GitHub step output.'
	Assert-True ($violationsOutput.Contains('violation_count=3')) 'Expected violation_count in the GitHub step output.'
	Assert-True ($violationsOutput.Contains('comment_path=')) 'Expected comment_path in the GitHub step output.'

	$cleanReportPath = Join-Path $cleanDirectory 'report.json'
	$cleanCommentPath = Join-Path $cleanDirectory 'comment.md'
	$cleanOutputPath = Join-Path $cleanDirectory 'github-output.txt'
	New-Report -Path $cleanReportPath -Violations @()

	$cleanResult = & $scriptPath -ReportPath $cleanReportPath -CommentPath $cleanCommentPath `
		-GitHubOutputPath $cleanOutputPath -Repository 'sillsdev/FieldWorks' -RunId '123457' `
		-ServerUrl 'https://github.com' -Sha 'fedcba9876543210fedcba9876543210fedcba98'

	Assert-True ($cleanResult.HasViolations -eq $false) 'Expected the helper to report a clean scan.'
	$cleanComment = Get-Content -LiteralPath $cleanCommentPath -Raw
	Assert-True ($cleanComment.Contains('No comment-style violations in the lines this branch adds since `origin/main`.')) `
		'Expected the clean comment to say so plainly.'
	Assert-True ((Get-Content -LiteralPath $cleanOutputPath -Raw).Contains('has_violations=false')) `
		'Expected has_violations=false in the GitHub step output.'

	# A report is written whenever the scan runs, so its absence is a broken
	# pipeline rather than a clean tree.
	$missingReportThrew = $false
	try {
		& $scriptPath -ReportPath (Join-Path $workspace 'absent\report.json') -GitHubOutputPath $cleanOutputPath
	}
	catch {
		$missingReportThrew = $true
	}
	Assert-True $missingReportThrew 'Expected a missing report to fail loudly.'
}
finally {
	if (Test-Path -LiteralPath $workspace) {
		Remove-Item -LiteralPath $workspace -Recurse -Force
	}
}

Write-Output '[OK] Build-CommentHygieneComment smoke test passed.'
exit 0
