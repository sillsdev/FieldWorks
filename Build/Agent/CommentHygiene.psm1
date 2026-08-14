<#
.SYNOPSIS
	Shared comment-hygiene scanning engine for FieldWorks source and project files.

.DESCRIPTION
	Implements the mechanical (regex-detectable) banned-content categories
	from the fieldworks-code-commenting skill, the ASCII-punctuation-only rule,
	and a 200-character budget on implementation comments. Judgment-based rules
	(accuracy, WHAT-not-HOW, standalone clarity) are not checked here.

	Scans .cs/.cpp/.h/.hpp/.cc/.cxx/.c/.idl (//, ///), .ps1/.psm1 (#,
	block-comment), and .csproj/.vcxproj/.vcproj/.props/.targets/.proj/
	.axaml/.xaml (<!-- -->) files. Only whole-line comments are scanned; a
	trailing same-line comment is not, and a C-style /* */ block comment is
	not (unlike its XML <!-- --> counterpart, which this module does parse).
	A doc comment, a PowerShell help block, or a file's first Xml <!-- -->
	block is exempt from the length cap -- the last one plays the same
	file/type-summary role as /// or a help block, since Xml has no separate
	doc-comment syntax to mark it by. (This help block cannot spell out that
	block-comment syntax literally -- PowerShell does not nest it, and the
	first close token would end this block early.)

.NOTES
	Import this module from comment-hygiene.ps1, comment-hygiene-repair.ps1,
	and comment-hygiene-blame.ps1:
	Import-Module "$PSScriptRoot/CommentHygiene.psm1" -Force
#>

Set-StrictMode -Version Latest

function Get-CommentHygieneCategories {
	<#
	.SYNOPSIS
		Returns the ordered category-name to regex-pattern map.
	#>
	return [ordered]@{
		# "Stage N"/"this commit" excluded: this codebase's own "stage-1/2" and Commit() are domain terms, not migration framing.
		'process-framing'    = '(?i)\bPhase[\s-]?\d+\b|\blater we\x27ll\b|\bwe\x27ll (?:later|eventually)\b'
		# (?-i:...) forces case-sensitivity despite the earlier (?i); F-keys and T-generics are excluded as letter-plus-digit look-alikes.
		'doc-pointer'        = '\b[\w./-]+\.md\b|(?i)\bsection\s+\d+[a-z]?\b|(?-i:(?<![A-Za-z0-9_+#/-])(?!F(?:1[0-2]|[1-9])\b)(?!T[1-9]\b)[A-Z]\d{1,2}(?![A-Za-z0-9.+#-]))'
		'absence-narration'  = '(?i)\bno longer\b|\bused to\b|\bpreviously (?:read|worked|did)\b|\bwas removed\b|\bwas stale\b|\brenamed from\b|\bfirst shipped\b'
		'cross-file-pointer' = "(?i)\bsee [A-Za-z]+\x27s note\b|\bas documented (?:on|in) [A-Za-z]+\b"
		'provenance'         = '(?i)\bshared by \w+ and \w+\b|\bthe only caller\b|\bthe sole caller\b|\bextracted from\b'
		# Targets only LLM-style punctuation, not all non-ASCII -- FieldWorks comments legitimately quote real script/IPA content.
		'non-ascii-punctuation' = Get-NonAsciiPunctuationPattern
	}
}

function Get-NonAsciiPunctuationPattern {
	<#
	.SYNOPSIS
		Returns a regex matching any single character in Get-NonAsciiReplacementMap's key set.
	#>
	$escaped = (Get-NonAsciiReplacementMap).Keys | ForEach-Object { [regex]::Escape($_) }
	return '(?:' + ($escaped -join '|') + ')'
}

function Get-CommentBody {
	<#
	.SYNOPSIS
		Returns the text after // or ///, or $null if the line is not a whole-line C-family comment.

	.DESCRIPTION
		Shared by every C-family language this module scans (C#, C/C++, IDL):
		all four use identical // and /// line-comment syntax.
	#>
	param([Parameter(Mandatory)][AllowEmptyString()][string] $Line)

	$trimmed = $Line.Trim()
	if ($trimmed.StartsWith('///')) { return $trimmed.Substring(3) }
	if ($trimmed.StartsWith('//')) { return $trimmed.Substring(2) }
	return $null
}

function Get-CommentHygieneLanguage {
	<#
	.SYNOPSIS
		Classifies a file path into a comment syntax family by extension.

	.OUTPUTS
		'PowerShell', 'CLike' (C#/C/C++/IDL), 'Xml' (project files and
		Avalonia views), or $null for an unrecognized extension.
	#>
	param([Parameter(Mandatory)][string] $Path)

	if ($Path -match '\.(ps1|psm1)$') { return 'PowerShell' }
	if ($Path -match '\.(cs|cpp|cxx|cc|c|h|hpp|idl)$') { return 'CLike' }
	if ($Path -match '\.(csproj|vcxproj|vcproj|props|targets|proj|axaml|xaml)$') { return 'Xml' }
	return $null
}

function Get-NonAsciiReplacementMap {
	<#
	.SYNOPSIS
		Returns the ordered map of non-ASCII characters to their ASCII replacement text.

	.DESCRIPTION
		Keys are built from [char] code points, not the backtick-u{} escape
		(that escape is PowerShell 6+ only; under Windows PowerShell 5.1 the
		backtick is silently dropped and the literal text "u{2014}" remains,
		so the map would never match a real em dash).
	#>
	$map = [ordered]@{}
	$map[[string][char]0x2014] = '--'
	$map[[string][char]0x2013] = '-'
	$map[[string][char]0x2192] = '->'
	$map[[string][char]0x2190] = '<-'
	$map[[string][char]0x2194] = '<->'
	$map[[string][char]0x2026] = '...'
	$map[[string][char]0x22ee] = '...'
	$map[[string][char]0x2022] = '-'
	$map[[string][char]0x00d7] = 'x'
	$map[[string][char]0x2018] = "'"
	$map[[string][char]0x2019] = "'"
	$map[[string][char]0x201c] = '"'
	$map[[string][char]0x201d] = '"'
	$map[[string][char]0x00a7] = 'Section'
	return $map
}

function Set-CommentHygieneFileContent {
	<#
	.SYNOPSIS
		Writes lines back to a file with an explicit BOM choice.

	.DESCRIPTION
		Uses System.Text.UTF8Encoding directly instead of
		Set-Content -Encoding utf8BOM/utf8NoBOM: those encoding names are
		PowerShell 7+ only and throw a parameter-binding error under
		Windows PowerShell 5.1.

	.PARAMETER Utf8Bom
		Whether the written file should carry a UTF-8 byte-order mark.
	#>
	param(
		[Parameter(Mandatory)][string] $Path,
		[Parameter(Mandatory)][AllowEmptyCollection()][string[]] $Lines,
		[Parameter(Mandatory)][bool] $Utf8Bom
	)

	$encoding = [System.Text.UTF8Encoding]::new($Utf8Bom)
	[System.IO.File]::WriteAllLines($Path, $Lines, $encoding)
}

function Repair-CommentLine {
	<#
	.SYNOPSIS
		Applies the ASCII replacement map to a whole-line comment.

	.PARAMETER Line
		A single raw source line.

	.OUTPUTS
		The fixed line if the line is a whole-line // or # comment and every
		non-ascii-punctuation character in it is covered by
		Get-NonAsciiReplacementMap; otherwise $null (not a whole-line comment,
		or an unmapped non-ascii-punctuation character remains). Any other
		non-ASCII content (real script/IPA text) is left untouched and does
		not block repair -- only the specific characters in the replacement
		map are ever in scope.

		Only ever called on lines Get-CommentHygieneViolations already
		classified as a comment for that file's language, so a bare `#`
		here is never a PowerShell string or a C# directive -- the caller
		guarantees that, this function does not re-derive the file's language.
	#>
	param([Parameter(Mandatory)][AllowEmptyString()][string] $Line)

	$trimmed = $Line.Trim()
	$prefixLength = $Line.Length - $Line.TrimStart().Length
	$leadingWhitespace = $Line.Substring(0, $prefixLength)

	$prefix = $null
	$body = $null
	if ($trimmed.StartsWith('///')) { $prefix = '///'; $body = $trimmed.Substring(3) }
	elseif ($trimmed.StartsWith('//')) { $prefix = '//'; $body = $trimmed.Substring(2) }
	elseif ($trimmed.StartsWith('#') -and -not $trimmed.StartsWith('#>')) { $prefix = '#'; $body = $trimmed.Substring(1) }

	if ($null -eq $prefix) { return $null }

	$fixedBody = $body
	$replacementMap = Get-NonAsciiReplacementMap
	foreach ($key in $replacementMap.Keys) {
		$fixedBody = $fixedBody -replace [regex]::Escape($key), $replacementMap[$key]
	}

	if ($fixedBody -match (Get-NonAsciiPunctuationPattern)) { return $null }

	return "$leadingWhitespace$prefix$fixedBody"
}

function Get-CommentLineClassification {
	<#
	.SYNOPSIS
		Classifies every line of a file as an implementation comment (subject
		to the 200-character budget), an exempt doc/help comment, or neither.

	.PARAMETER Lines
		The file's lines.

	.PARAMETER Language
		'PowerShell' (#, block-comment), 'CLike' (//, /// -- C#/C/C++/IDL),
		or 'Xml' (<!-- -->  -- project files, Avalonia views). A bare # is
		never treated as a comment for a CLike file, so a preprocessor
		directive (#region, #if) is never misread as one.

	.OUTPUTS
		A hashtable with parallel arrays Kinds ('impl', 'exempt', or $null
		per line) and Bodies (comment text per line, or $null). Xml's first
		<!-- --> block in the file is 'exempt' -- the same role a C# /// or
		PowerShell help block plays, since XML has no separate doc-comment
		syntax to mark a file/type-level summary. Every later Xml comment is
		'impl', subject to the length budget.
	#>
	param(
		[Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]] $Lines,
		[Parameter(Mandatory)][ValidateSet('PowerShell', 'CLike', 'Xml')][string] $Language
	)

	$kinds = New-Object 'object[]' $Lines.Count
	$bodies = New-Object 'object[]' $Lines.Count
	$inHelpBlock = $false
	$inXmlComment = $false
	$xmlCommentBlockCount = 0
	$currentXmlCommentKind = $null

	for ($i = 0; $i -lt $Lines.Count; $i++) {
		$trimmed = $Lines[$i].Trim()

		if ($Language -eq 'PowerShell') {
			if ($inHelpBlock) {
				$kinds[$i] = 'exempt'
				$bodies[$i] = $trimmed
				if ($trimmed.EndsWith('#>')) { $inHelpBlock = $false }
				continue
			}
			if ($trimmed.StartsWith('<#')) {
				$kinds[$i] = 'exempt'
				$bodies[$i] = $trimmed.Substring(2).TrimEnd('#', '>', ' ')
				if (-not $trimmed.EndsWith('#>')) { $inHelpBlock = $true }
				continue
			}
			if ($trimmed.StartsWith('#')) {
				$kinds[$i] = 'impl'
				$bodies[$i] = $trimmed.Substring(1)
				continue
			}
		}
		elseif ($Language -eq 'Xml') {
			if ($inXmlComment) {
				$kinds[$i] = $currentXmlCommentKind
				$endIndex = $trimmed.IndexOf('-->')
				if ($endIndex -ge 0) {
					$bodies[$i] = $trimmed.Substring(0, $endIndex)
					$inXmlComment = $false
				}
				else {
					$bodies[$i] = $trimmed
				}
				continue
			}
			if ($trimmed.StartsWith('<!--')) {
				$currentXmlCommentKind = if ($xmlCommentBlockCount -eq 0) { 'exempt' } else { 'impl' }
				$xmlCommentBlockCount++
				$kinds[$i] = $currentXmlCommentKind
				$rest = $trimmed.Substring(4)
				$endIndex = $rest.IndexOf('-->')
				if ($endIndex -ge 0) {
					$bodies[$i] = $rest.Substring(0, $endIndex)
				}
				else {
					$bodies[$i] = $rest
					$inXmlComment = $true
				}
				continue
			}
		}
		else {
			# Inlined from Get-CommentBody: this loop runs every line of every diffed file on
			# every build, where a per-line function call plus its own redundant Trim measurably
			# slows the gate down.
			if ($trimmed.StartsWith('///')) {
				$kinds[$i] = 'exempt'
				$bodies[$i] = $trimmed.Substring(3)
				continue
			}
			if ($trimmed.StartsWith('//')) {
				$kinds[$i] = 'impl'
				$bodies[$i] = $trimmed.Substring(2)
				continue
			}
		}

		$kinds[$i] = $null
		$bodies[$i] = $null
	}

	return @{ Kinds = $kinds; Bodies = $bodies }
}

function Get-CommentHygieneViolations {
	<#
	.SYNOPSIS
		Scans the given files for mechanical comment-hygiene violations.

	.PARAMETER Files
		Absolute paths to files to scan. Files whose extension
		Get-CommentHygieneLanguage does not recognize are skipped.

	.PARAMETER LineFilter
		Optional hashtable mapping an absolute file path to a
		HashSet[int] of 1-based line numbers to check. Omit to scan every
		line in every file.

	.OUTPUTS
		One PSCustomObject per violation: File, Line, Category, Text.
		Category 'comment-too-long' additionally covers a run of
		consecutive implementation-comment lines whose combined text
		exceeds a character budget; a doc comment or a PowerShell help
		block is exempt from that one.
	#>
	param(
		[Parameter(Mandatory)][string[]] $Files,
		[hashtable] $LineFilter
	)

	$categories = Get-CommentHygieneCategories
	$violations = New-Object System.Collections.ArrayList
	$maxImplCommentChars = 200

	foreach ($file in $Files) {
		if (-not (Test-Path -LiteralPath $file)) { continue }

		$language = Get-CommentHygieneLanguage -Path $file
		if ($null -eq $language) { continue }

		$allowedLines = $null
		if ($LineFilter -and $LineFilter.ContainsKey($file)) {
			$allowedLines = $LineFilter[$file]
		}

		# File.ReadAllLines, not Get-Content: ~50x faster on a large file, and this gate runs
		# every build. Still BOM-safe: StreamReader sniffs a real BOM even given an explicit
		# encoding.
		$lines = [System.IO.File]::ReadAllLines($file, [System.Text.Encoding]::UTF8)
		$classification = Get-CommentLineClassification -Lines $lines -Language $language
		$kinds = $classification.Kinds
		$bodies = $classification.Bodies

		for ($i = 0; $i -lt $lines.Count; $i++) {
			if ($null -eq $kinds[$i]) { continue }
			$lineNumber = $i + 1
			if ($allowedLines -and -not $allowedLines.Contains($lineNumber)) { continue }

			foreach ($category in $categories.Keys) {
				if ($bodies[$i] -match $categories[$category]) {
					[void]$violations.Add([PSCustomObject]@{
						File     = $file
						Line     = $lineNumber
						Category = $category
						Text     = $bodies[$i].Trim()
					})
				}
			}

			# Xml-only: XML forbids a literal "--" anywhere in comment content, not just next to
			# "-->", so the usual em-dash-to-"--" fix is invalid here; use a single "-" instead.
			if ($language -eq 'Xml' -and $bodies[$i] -match '--') {
				[void]$violations.Add([PSCustomObject]@{
					File     = $file
					Line     = $lineNumber
					Category = 'xml-illegal-double-hyphen'
					Text     = $bodies[$i].Trim()
				})
			}
		}

		$blockStart = -1
		$blockLength = 0
		for ($i = 0; $i -le $lines.Count; $i++) {
			$isImplLine = ($i -lt $lines.Count) -and ($kinds[$i] -eq 'impl')
			if ($isImplLine) {
				if ($blockStart -lt 0) { $blockStart = $i }
				$blockLength++
				continue
			}

			if ($blockLength -gt 0) {
				$blockIndexes = $blockStart..($blockStart + $blockLength - 1)
				$totalChars = [int]($blockIndexes | ForEach-Object { $bodies[$_].Trim().Length } | Measure-Object -Sum).Sum
				if ($totalChars -gt $maxImplCommentChars) {
					# Blame only this diff's contribution: skip if the untouched lines alone already exceeded budget.
					$untouchedChars = 0
					if ($allowedLines) {
						$untouchedIndexes = $blockIndexes | Where-Object { -not $allowedLines.Contains($_ + 1) }
						if ($untouchedIndexes) {
							$untouchedChars = [int]($untouchedIndexes | ForEach-Object { $bodies[$_].Trim().Length } | Measure-Object -Sum).Sum
						}
					}
					if ($untouchedChars -le $maxImplCommentChars) {
						[void]$violations.Add([PSCustomObject]@{
							File     = $file
							Line     = $blockStart + 1
							Category = 'comment-too-long'
							Text     = ("{0} chars: {1}" -f $totalChars, $bodies[$blockStart].Trim())
						})
					}
				}
			}
			$blockStart = -1
			$blockLength = 0
		}
	}

	# The unary comma prevents PowerShell's pipeline from unrolling a
	# zero- or one-element array into $null or a bare scalar on return.
	return ,$violations.ToArray()
}

Export-ModuleMember -Function @(
	'Get-CommentHygieneCategories',
	'Get-CommentBody',
	'Get-CommentHygieneLanguage',
	'Get-CommentLineClassification',
	'Get-CommentHygieneViolations',
	'Get-NonAsciiReplacementMap',
	'Get-NonAsciiPunctuationPattern',
	'Repair-CommentLine',
	'Set-CommentHygieneFileContent'
)
