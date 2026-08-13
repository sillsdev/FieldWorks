<#
.SYNOPSIS
	Fixture-based tests for CommentHygiene.psm1.

.DESCRIPTION
	One true-positive and one near-miss per category. Run directly:
	pwsh -File Build/Agent/CommentHygiene.Tests.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'CommentHygiene.psm1') -Force

function Get-Codepoint {
	<#
	.SYNOPSIS
		Builds a real Unicode character from a code point, portable across
		PowerShell 7 and Windows PowerShell 5.1.

	.DESCRIPTION
		Fixture strings must not use the backtick-u{} escape: it is
		PowerShell 6+ only, and under 5.1 it silently degrades to the
		literal text "u{2014}" instead of throwing -- exactly the bug this
		suite exists to catch in the module, so the fixtures cannot carry
		it themselves.
	#>
	param([int] $Codepoint)
	if ($Codepoint -gt 0xFFFF) { return [char]::ConvertFromUtf32($Codepoint) }
	return [string][char]$Codepoint
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("CommentHygieneTests_" + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

$failures = New-Object System.Collections.ArrayList

function Assert-Category {
	param([string] $Name, [string] $Line, [string] $ExpectedCategory, [string] $Extension = '.cs')

	$file = Join-Path $tempDir "$Name$Extension"
	Set-Content -LiteralPath $file -Value $Line -Encoding UTF8
	$violations = Get-CommentHygieneViolations -Files @($file)
	$hit = $violations | Where-Object { $_.Category -eq $ExpectedCategory }
	if (-not $hit) {
		[void]$script:failures.Add("FAIL [$Name]: expected category '$ExpectedCategory' for line: $Line")
	}
}

function Assert-Clean {
	param([string] $Name, [string] $Line, [string] $Extension = '.cs')

	$file = Join-Path $tempDir "$Name$Extension"
	Set-Content -LiteralPath $file -Value $Line -Encoding UTF8
	$violations = Get-CommentHygieneViolations -Files @($file)
	if ($violations.Count -gt 0) {
		$hitCategories = ($violations | ForEach-Object { $_.Category }) -join ','
		[void]$script:failures.Add("FAIL [$Name]: expected no violations for line: $Line -- got $hitCategories")
	}
}

function Assert-CategoryLines {
	param([string] $Name, [string[]] $Lines, [string] $ExpectedCategory, [string] $Extension = '.cs')

	$file = Join-Path $tempDir "$Name$Extension"
	Set-Content -LiteralPath $file -Value $Lines -Encoding UTF8
	$violations = Get-CommentHygieneViolations -Files @($file)
	$hit = $violations | Where-Object { $_.Category -eq $ExpectedCategory }
	if (-not $hit) {
		[void]$script:failures.Add("FAIL [$Name]: expected category '$ExpectedCategory' for lines: $($Lines -join ' / ')")
	}
}

function Assert-CleanLines {
	param([string] $Name, [string[]] $Lines, [string] $Extension = '.cs')

	$file = Join-Path $tempDir "$Name$Extension"
	Set-Content -LiteralPath $file -Value $Lines -Encoding UTF8
	$violations = Get-CommentHygieneViolations -Files @($file)
	if ($violations.Count -gt 0) {
		$hitCategories = ($violations | ForEach-Object { $_.Category }) -join ','
		[void]$script:failures.Add("FAIL [$Name]: expected no violations for lines: $($Lines -join ' / ') -- got $hitCategories")
	}
}

function Assert-Repair {
	param([string] $Name, [string] $Line, [string] $ExpectedFixedLine)

	$actual = Repair-CommentLine -Line $Line
	if ($actual -ne $ExpectedFixedLine) {
		[void]$script:failures.Add("FAIL [$Name]: expected repaired line '$ExpectedFixedLine', got '$actual' for input: $Line")
	}
}

function Assert-Unrepairable {
	param([string] $Name, [string] $Line)

	$actual = Repair-CommentLine -Line $Line
	if ($null -ne $actual) {
		[void]$script:failures.Add("FAIL [$Name]: expected `$null (unrepairable) for line: $Line -- got '$actual'")
	}
}

# process-framing -- skill worked example
Assert-Category 'phase-framing' '// Phase 3 test (b): picking a style applies it to the selection' 'process-framing'
Assert-Clean    'phase-clean'   '// Applies the selected style to the current selection'

# process-framing must not fire on "stage"/"commit" here: this codebase has a real two-step stage-then-commit UI pattern, distinct from migration-phase framing.
Assert-Clean    'stage-domain-term' '// STAGE 1 -- the pick already populated the auxiliary picker.'
Assert-Clean    'commit-domain-term' '// Capture everything staged since the last boundary -- that is what this commit "writes".'

# doc-pointer -- skill worked example
Assert-Category 'doc-pointer-md' '// winforms-free-lexeme-editor.md D1: a plugin-claimed custom slice renders its plugin''s own control' 'doc-pointer'
Assert-Clean    'doc-pointer-clean' '// LT-22351: a plugin-claimed custom slice renders its plugin''s own control'

# doc-pointer finding-code must stay case-sensitive: "m3" is a real LCM field name (IMoStemMsa), not a design-doc finding code, despite the (?i) flag earlier in the pattern.
Assert-Clean    'doc-pointer-lowercase-field' '// Seed text matches the canonical field label (the m3 InflectionClass field label).'

# doc-pointer finding-code must not fire on function keys or generic type-parameter names, which share the letter-plus-digit shape but are ordinary code vocabulary, not design-doc codes.
Assert-Clean    'doc-pointer-function-key' '// Whether it came from a legacy view, F5/RefreshAllViews-driven, or something else.'
Assert-Clean    'doc-pointer-generic-param' '// Factored to a Func<T1,T2,TResult> seam so the caller can inject either path.'

# absence-narration -- skill worked example
Assert-Category 'absence-no-longer' '// An ORC run no longer forces the whole value read-only.' 'absence-narration'
Assert-Clean    'absence-clean' '// An ORC run does not force the whole value read-only.'

# cross-file-pointer
Assert-Category 'cross-file' "// See BulkEditBar's note about ownership checks." 'cross-file-pointer'
Assert-Clean    'cross-file-clean' '// Ownership checks run before every write in this method.'

# provenance
Assert-Category 'provenance' '// This helper is shared by BulkEditBar and RecordClerk.' 'provenance'
Assert-Clean    'provenance-clean' '// Applies the pending edit to every selected row.'

# non-ascii-punctuation
Assert-Category 'non-ascii-punctuation' ("// Uses an em dash {0} inline." -f (Get-Codepoint 0x2014)) 'non-ascii-punctuation'
Assert-Clean    'non-ascii-punctuation-clean' '// Uses a double hyphen -- inline.'

# non-ascii-punctuation targets only Western-typography punctuation, not non-ASCII in general --
# real non-English script or IPA/emoji content must not fire.
Assert-Clean    'non-ascii-punctuation-cyrillic' ("// Folds the Cyrillic letter {0} into the wrong letter group." -f (Get-Codepoint 0x0493))
Assert-Clean    'non-ascii-punctuation-emoji'    ('// The input string is "x{0}y" (a surrogate pair).' -f (Get-Codepoint 0x1F600))

# Repair-CommentLine -- mapped characters produce a fixed ASCII line
Assert-Repair 'repair-em-dash'   ("// Uses an em dash {0} inline." -f (Get-Codepoint 0x2014))   '// Uses an em dash -- inline.'
Assert-Repair 'repair-arrow'     ("// Flows left {0} right." -f (Get-Codepoint 0x2192))          '// Flows left -> right.'
Assert-Repair 'repair-ellipsis'  ("// And so on {0}" -f (Get-Codepoint 0x2026))                  '// And so on ...'
Assert-Repair 'repair-bullet'    ("// {0} first item" -f (Get-Codepoint 0x2022))                 '// - first item'
Assert-Repair 'repair-multiply'  ("// A {0} B grid" -f (Get-Codepoint 0x00d7))                   '// A x B grid'
Assert-Repair 'repair-doc-slash' ("/// Uses an em dash {0} inline." -f (Get-Codepoint 0x2014))   '/// Uses an em dash -- inline.'

# Detection and repair share one character set by construction (the pattern is built from
# the replacement map's keys), so there is no "detected but unmapped" character to fail on.
$cjkLine = "// Some text with {0} inline." -f (Get-Codepoint 0x4e2d)
Assert-Repair 'repair-noop-cjk' $cjkLine $cjkLine

# Repair-CommentLine -- not a whole-line comment at all
Assert-Unrepairable 'repair-not-a-comment' ("int x = 1; // trailing {0} comment" -f (Get-Codepoint 0x2014))

# PowerShell (#) gets the same categories as C# (//) -- the gap that let this tooling's own comments ship unscanned.
Assert-Category 'ps-phase-framing' '# Phase 3 test: picking a style applies it to the selection' 'process-framing' '.ps1'
Assert-Category 'ps-non-ascii-punctuation' ("# Uses an em dash {0} inline." -f (Get-Codepoint 0x2014)) 'non-ascii-punctuation' '.ps1'
Assert-Repair 'repair-ps-em-dash' ("# Uses an em dash {0} inline." -f (Get-Codepoint 0x2014)) '# Uses an em dash -- inline.'

# Guards against a regression back to the PS7-only backtick-u{} escape.
$emDash = Get-Codepoint 0x2014
if (-not (Get-NonAsciiReplacementMap).Contains($emDash)) {
	[void]$script:failures.Add("FAIL [non-ascii-map-real-codepoint]: Get-NonAsciiReplacementMap does not key on the real em dash character (PSVersion $($PSVersionTable.PSVersion))")
}

# comment-too-long fires on a 200-char budget across the whole block, not a line count -- a
# doc comment or PowerShell help block is exempt and may run long-form regardless of length.
Assert-CategoryLines 'too-long-cs' @(
	'// This explains the first reason the approach was chosen over the alternative approach taken here for this specific case.',
	'// This explains a second reason that would not fit on the first line at all today either, adding more detail.'
) 'comment-too-long'
Assert-CategoryLines 'too-long-single-line-cs' @(
	'// This one very long line explains the reasoning all by itself without wrapping and keeps going for quite a while past what used to be the one-line cap until it is clearly over the two hundred character budget on its own.'
) 'comment-too-long'
Assert-CleanLines 'one-line-cs' @('// A single reason, on a single line, is exactly the budget.')
Assert-CleanLines 'under-budget-multiline-cs' @(
	'// A short first reason for the approach, stated plainly.',
	'// A short second reason that rounds out the explanation.'
)
Assert-CleanLines 'doc-comment-long-cs' @(
	'/// A public API doc comment may run several lines when the contract genuinely needs it,',
	'/// because a reader hovering the symbol has nowhere else to find this.'
)
Assert-CategoryLines 'too-long-ps' @(
	'# This explains the first reason the approach was chosen over the alternative approach taken here for this specific case.',
	'# This explains a second reason that would not fit on the first line at all today either, adding more detail.'
) 'comment-too-long' '.ps1'
Assert-CleanLines 'help-block-long-ps' @(
	'<#',
	'.SYNOPSIS',
	'    Comment-based help is allowed to run long-form, the PowerShell equivalent of a doc comment.',
	'#>'
) '.ps1'

Remove-Item -LiteralPath $tempDir -Recurse -Force

if ($failures.Count -gt 0) {
	Write-Host ''
	foreach ($f in $failures) { Write-Host $f -ForegroundColor Red }
	Write-Host ''
	Write-Host "$($failures.Count) test(s) failed." -ForegroundColor Red
	exit 1
}

Write-Host 'All CommentHygiene tests passed.' -ForegroundColor Green
exit 0
