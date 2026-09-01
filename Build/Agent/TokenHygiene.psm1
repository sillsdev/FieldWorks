<#
.SYNOPSIS
	Shared token-hygiene scanning engine for the FieldWorks Avalonia surface.

.DESCRIPTION
	Detects hardcoded color and spacing/dimension literals in the C# and
	Avalonia XAML source that must instead route through the shared
	FwAvaloniaTheme token system (Src/Common/FwAvaloniaTheme/Tokens/):
	FwColorTokens.axaml's brush/font-size ThemeDictionary, DataTreeTokens.axaml's
	flat layout dimensions, and DialogTheme.axaml's own local Dialog* keys.

	Unlike CommentHygiene.psm1, this module has no diff/added-lines mode:
	Get-TokenHygieneViolations always scans every line of every given file.
	The scoped tree is new code with nothing to grandfather, so there is no
	backlog for an added-lines phase-in to work through and a violation can
	only arrive with a new edit.

.NOTES
	Import this module from token-hygiene.ps1:
	Import-Module "$PSScriptRoot/TokenHygiene.psm1" -Force
#>

Set-StrictMode -Version Latest

function Get-TokenHygieneScopeRoots {
	<#
	.SYNOPSIS
		Repo-relative directory roots the token-hygiene check scans.
	#>
	return @(
		'Src/Common/FwAvalonia',
		'Src/Common/FwAvaloniaDialogs',
		'Src/Common/FwAvaloniaTheme',
		'Src/Common/FwAvaloniaPreviewHost',
		'Src/LexText/LexTextControls/Avalonia',
		'Src/xWorks/Avalonia'
	)
}

function Test-TokenHygieneExcludedPath {
	<#
	.SYNOPSIS
		True when a path is out of scope for token-hygiene scanning: the
		token-resolution/definition files themselves, generated/designer code,
		or a Tests project/directory.

	.DESCRIPTION
		Accepts either an absolute or a repo-relative path (matched by
		suffix/substring so both work). Deliberately a SINGLE function
		covering every exclusion reason -- mirrors comment-hygiene.ps1's own
		Test-ExcludedPath convention -- and is called both when building the
		scanned file list (Get-TokenHygieneScopedFiles) and per-file inside
		Get-TokenHygieneViolations itself, so a caller handing it an arbitrary
		path (a test fixture, say) still gets the real exclusion behavior
		rather than relying on the file-listing layer alone.

		Neither DialogTheme.axaml NOR the Tokens/ dictionaries get a blanket
		path exclusion: both declare their own local tokens but also have
		plain style setters/other content that must stay policed, so only the
		line-level x:Key declaration check in Get-TokenHygieneXmlViolations
		exempts a token's own declaration line, not the whole file.
	#>
	param([Parameter(Mandatory)][string] $Path)

	$normalized = $Path -replace '\\', '/'

	# These files ARE the token plumbing the rest of the surface routes
	# through; their own literals define a token, not bypass one.
	$fullFileAllowlist = @(
		'Src/Common/FwAvalonia/FwThemeResources.cs',
		'Src/Common/FwAvalonia/FwAvaloniaDensity.cs',
		'Src/Common/FwAvalonia/FwSemiDensity.cs',
		'Src/Common/FwAvalonia/CompactDialogStyles.cs',
		'Src/Common/FwAvalonia/FwSurfaceStyles.cs'
	)
	foreach ($suffix in $fullFileAllowlist) {
		if ($normalized.EndsWith($suffix)) { return $true }
	}

	# Tokens/ files are scanned like any other file; only the per-line x:Key exemption
	# below protects a declaration -- the FieldWorks Layer-1-extension boundary.

	if ($normalized -match '\.g\.cs$') { return $true }
	if ($normalized -match 'Designer\.cs$') { return $true }

	# A Tests project/directory anywhere in the path, matched as a path
	# SEGMENT: a file merely named "...Tests.cs" elsewhere stays in scope.
	if ($normalized -match '(?:^|/)[A-Za-z0-9_.]*Tests/') { return $true }

	return $false
}

function Get-TokenHygieneLanguage {
	<#
	.SYNOPSIS
		Classifies a file path as 'CSharp', 'Xml' (.axaml/.xaml), or $null.
	#>
	param([Parameter(Mandatory)][string] $Path)

	if ($Path -match '\.cs$') { return 'CSharp' }
	if ($Path -match '\.(axaml|xaml)$') { return 'Xml' }
	return $null
}

function Get-TokenHygieneScopedFiles {
	<#
	.SYNOPSIS
		Returns absolute paths of every in-scope .cs/.axaml/.xaml file under
		the Avalonia surface roots, tracked or newly created on disk, minus
		excluded paths.

	.DESCRIPTION
		Includes untracked files (git ls-files --others --exclude-standard),
		not only tracked ones: the token system this check enforces can itself
		be new, not-yet-committed work, and a check that only saw tracked files
		would silently pass while the very tree it exists to check goes
		unscanned.
	#>
	param([Parameter(Mandatory)][string] $RepoRoot)

	$roots = Get-TokenHygieneScopeRoots
	# git resolves pathspecs against the CURRENT directory, not $RepoRoot: without
	# this, a run from elsewhere lists nothing and passes for the wrong reason.
	Push-Location -LiteralPath $RepoRoot
	try {
		$trackedRaw = git ls-files -- $roots 2>$null
		$untrackedRaw = git ls-files --others --exclude-standard -- $roots 2>$null
	}
	finally {
		Pop-Location
	}

	$relatives = @($trackedRaw) + @($untrackedRaw) |
		Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
		Sort-Object -Unique

	$result = New-Object System.Collections.Generic.List[string]
	foreach ($relative in $relatives) {
		if ($relative -notmatch '\.(cs|axaml|xaml)$') { continue }
		if (Test-TokenHygieneExcludedPath -Path $relative) { continue }
		$result.Add((Join-Path $RepoRoot ($relative -replace '/', [IO.Path]::DirectorySeparatorChar)))
	}
	return ,$result.ToArray()
}

function Test-TokenHygieneAllZero {
	<#
	.SYNOPSIS
		True when every numeric component of a value is zero.

	.DESCRIPTION
		A bare 0 (or an all-zero Thickness like "0,0,0,0") carries no design
		decision -- it is "nothing", not an unrouted token -- so it is excluded
		from both violation categories. Avalonia accepts either separator for a
		Thickness, so both are split on: "0 0 0 0" is the same value as
		"0,0,0,0", and splitting on the comma alone would try to parse the
		whole string as one double and throw.
	#>
	param([Parameter(Mandatory)][string] $Value)

	foreach ($token in ($Value -split '[, ]')) {
		$trimmed = $token.Trim()
		if ($trimmed.Length -eq 0) { continue }
		$parsed = 0.0
		if (-not [double]::TryParse($trimmed,
			[System.Globalization.NumberStyles]::Float,
			[System.Globalization.CultureInfo]::InvariantCulture, [ref] $parsed)) {
			return $false
		}
		if ($parsed -ne 0) { return $false }
	}
	return $true
}

function New-TokenHygieneViolation {
	param(
		[Parameter(Mandatory)][string] $File,
		[Parameter(Mandatory)][int] $Line,
		[Parameter(Mandatory)][string] $Category,
		[Parameter(Mandatory)][AllowEmptyString()][string] $Text
	)
	return [PSCustomObject]@{ File = $File; Line = $Line; Category = $Category; Text = $Text }
}

function Get-TokenHygieneCSharpViolations {
	<#
	.SYNOPSIS
		Scans one C# file's lines for hardcoded-color/hardcoded-spacing
		violations.

	.DESCRIPTION
		hardcoded-color: a brush/color CONSTRUCTION (`new SolidColorBrush(`,
		`new ImmutableSolidColorBrush(`, `Color.FromRgb(`, `Color.FromArgb(`,
		`Color.FromUInt32(`, `Color.Parse(`, `SolidColorBrush.Parse(`,
		`Brush.Parse(`) or a bare `Brushes.<Name>`/`Colors.<Name>` reference
		(not qualified by a preceding `.` or word character, so an
		identifier merely ending in "Brushes"/"Brush"/"Colors"/"Color" is not
		mistaken for the static Avalonia.Media class).

		hardcoded-spacing, four independent shapes, each excluding an
		all-zero value the same way:
		  * `new Thickness(...)` / `new CornerRadius(...)` whose arguments
		    are 1-4 bare numeric literals (built from a variable or constant
		    expression is not a literal and is not flagged);
		  * a direct property assignment (`Width = 14`, `MinWidth = 160`,
		    `Spacing = 4`, ...) to a bare numeric literal, whether as an
		    object-initializer member (terminated by `,` or `}`) or a
		    statement (terminated by `;`) -- a single `=` only, so `==`
		    comparisons are never mistaken for assignments;
		  * `new Setter(<Property>, <literal>)` -- the Setter idiom used
		    outside DialogTheme.axaml's declarative styles.
		A whole-line `//` comment is never scanned -- a comment mentioning
		the banned pattern in prose is not code.
	#>
	param(
		[Parameter(Mandatory)][string] $File,
		[Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]] $Lines
	)

	$violations = New-Object System.Collections.ArrayList

	$colorPattern = '\bnew\s+SolidColorBrush\s*\(' +
		'|\bnew\s+ImmutableSolidColorBrush\s*\(' +
		'|\bColor\.FromRgb\s*\(' +
		'|\bColor\.FromArgb\s*\(' +
		'|\bColor\.FromUInt32\s*\(' +
		'|\bColor\.Parse\s*\(' +
		'|\bSolidColorBrush\.Parse\s*\(' +
		'|\bBrush\.Parse\s*\(' +
		'|(?<![\w.])Brushes\.[A-Za-z]+\b' +
		'|(?<![\w.])Colors\.[A-Za-z]+\b'

	$literalGroup = '(-?\d+(?:\.\d+)?)'
	$thicknessPattern = '\bnew\s+Thickness\s*\(\s*(-?\d+(?:\.\d+)?(?:\s*,\s*-?\d+(?:\.\d+)?){0,3})\s*\)'
	$cornerRadiusPattern = '\bnew\s+CornerRadius\s*\(\s*(-?\d+(?:\.\d+)?(?:\s*,\s*-?\d+(?:\.\d+)?){0,3})\s*\)'
	# A single `=` only, so a comparison is never an assignment. The lookahead accepts every
	# real terminator, including a bare end-of-line (a multi-line initializer's last member).
	$propertyAssignPattern = '\b(?:Width|MinWidth|MaxWidth|Height|MinHeight|MaxHeight|FontSize|Spacing' +
		'|Opacity|Margin|Padding|BorderThickness|CornerRadius|RowSpacing|ColumnSpacing)' +
		"\s*(?<![=!<>])=(?!=)\s*(?:\((?:double|float|int)\)\s*)?$literalGroup[dfmDFM]?(?=\s*[;,)}]|\s*`$)"
	$setterLiteralPattern = "\bnew\s+Setter\s*\(\s*[^,()]+,\s*$literalGroup\s*\)"
	$spacingPatterns = @($thicknessPattern, $cornerRadiusPattern, $propertyAssignPattern, $setterLiteralPattern)

	for ($i = 0; $i -lt $Lines.Count; $i++) {
		$line = $Lines[$i]
		$trimmed = $line.Trim()
		if ($trimmed.StartsWith('//')) { continue }
		$lineNumber = $i + 1

		if ($line -match $colorPattern) {
			[void]$violations.Add((New-TokenHygieneViolation $File $lineNumber 'hardcoded-color' $trimmed))
		}

		foreach ($pattern in $spacingPatterns) {
			foreach ($m in [regex]::Matches($line, $pattern)) {
				if (Test-TokenHygieneAllZero -Value $m.Groups[1].Value) { continue }
				[void]$violations.Add((New-TokenHygieneViolation $File $lineNumber 'hardcoded-spacing' $trimmed))
			}
		}
	}

	return ,$violations.ToArray()
}

function Get-TokenHygieneAttributePattern {
	<#
	.SYNOPSIS
		Regex matching a direct XML attribute usage, e.g. Margin="10".
	#>
	param([Parameter(Mandatory)][string[]] $Names)
	return '\b(?:' + ($Names -join '|') + ')\s*=\s*"(?<val>[^"]*)"'
}

function Get-TokenHygieneSetterPattern {
	<#
	.SYNOPSIS
		Regex matching the Avalonia Style Setter idiom, e.g.
		<Setter Property="Padding" Value="4,2"/>, where the literal never
		appears as a plain XML attribute name.
	#>
	param([Parameter(Mandatory)][string[]] $Names)
	return '<Setter\s+Property\s*=\s*"(?:' + ($Names -join '|') + ')"\s+Value\s*=\s*"(?<val>[^"]*)"'
}

function Get-TokenHygieneXmlCommentMask {
	<#
	.SYNOPSIS
		Returns a bool[] parallel to Lines: $true for every line that is
		part of an Xml `<!-- ... -->` comment (single- or multi-line).

	.DESCRIPTION
		A comment quoting or explaining the banned pattern in prose (or kept
		only as history) is not markup and must never be flagged. Only lines
		that are wholly inside a comment are masked here; a line that mixes
		real markup with a same-line comment is left unmasked, and
		Remove-TokenHygieneXmlComments strips the comment span before the
		line is scanned. Masking such a line whole would let any violation be
		excused by appending a comment to it.
	#>
	param([Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]] $Lines)

	$mask = New-Object bool[] ($Lines.Count)
	$inComment = $false
	for ($i = 0; $i -lt $Lines.Count; $i++) {
		$trimmed = $Lines[$i].Trim()
		if ($inComment) {
			$mask[$i] = $true
			if ($trimmed.Contains('-->')) { $inComment = $false }
			continue
		}
		if ($trimmed.Contains('<!--')) {
			# Masked only when the comment is the whole line. A line that also carries markup
			# is scanned, with the comment span removed first.
			$mask[$i] = $trimmed.StartsWith('<!--')
			if (-not $trimmed.Contains('-->')) { $inComment = $true }
			continue
		}
		$mask[$i] = $false
	}
	return ,$mask
}

function Remove-TokenHygieneXmlComments {
	<#
	.SYNOPSIS
		Returns the line with any `<!-- ... -->` span replaced by a space.

	.DESCRIPTION
		Keeps a same-line comment from excusing the markup beside it, while
		still never scanning the comment's own text. An unterminated `<!--`
		drops the rest of the line, which the multi-line mask then covers.
	#>
	param([Parameter(Mandatory)][AllowEmptyString()][string] $Line)

	$stripped = [regex]::Replace($Line, '<!--.*?-->', ' ')
	$open = $stripped.IndexOf('<!--')
	if ($open -ge 0) { $stripped = $stripped.Substring(0, $open) }
	return $stripped
}

function Test-TokenHygieneResourceDeclaration {
	<#
	.SYNOPSIS
		True when the line declares a resource whose own value is the literal.

	.DESCRIPTION
		`<SolidColorBrush x:Key="Fw..." Color="#69696B"/>` is a token
		definition: the literal there is the point. A layout element that
		merely carries an x:Key -- `<Border x:Key="X" Background="#F00"
		Margin="40"/>` -- is not, and exempting it would let any violation be
		excused by adding a key. Only the primitive resource types are
		exempt.
	#>
	param([Parameter(Mandatory)][AllowEmptyString()][string] $Line)

	if ($Line -notmatch '\bx:Key\s*=') { return $false }
	$resourceTypes = @('SolidColorBrush', 'ImmutableSolidColorBrush', 'LinearGradientBrush',
		'Color', 'Thickness', 'CornerRadius', 'GridLength', 'BoxShadows', 'FontFamily',
		'x:Double', 'sys:Double', 'system:Double', 'x:String', 'x:Int32')
	foreach ($type in $resourceTypes) {
		if ($Line -match ('<' + [regex]::Escape($type) + '\b')) { return $true }
	}
	return $false
}

function Get-TokenHygieneXmlViolations {
	<#
	.SYNOPSIS
		Scans one .axaml/.xaml file's lines for hardcoded-color/
		hardcoded-spacing violations.

	.DESCRIPTION
		Checks both the direct-attribute form (`<TextBlock Margin="10"/>`) and
		the Setter/Property/Value idiom
		(`<Setter Property="Padding" Value="4,2"/>`), since DialogTheme.axaml's
		Style blocks use the latter exclusively. A value that starts with `{`
		(`{StaticResource ...}`, `{DynamicResource ...}`, `{Binding ...}`) is a
		resource/binding reference, not a literal, and is never flagged. A
		line declaring a resource (`x:Key="..."`) is the token itself, not a
		bypass of it, and is skipped entirely -- this is what lets
		DialogTheme.axaml declare its own local Dialog* tokens without being
		fully excluded. Every line inside an Xml comment
		(Get-TokenHygieneXmlCommentMask) is skipped too.
	#>
	param(
		[Parameter(Mandatory)][string] $File,
		[Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]] $Lines
	)

	$violations = New-Object System.Collections.ArrayList
	$spacingNames = @('Margin', 'Padding', 'Width', 'Height', 'Spacing', 'FontSize', 'MinHeight', 'MinWidth',
		'MaxWidth', 'MaxHeight', 'RowSpacing', 'ColumnSpacing', 'StrokeThickness', 'CornerRadius', 'BorderThickness',
		'RowDefinitions', 'ColumnDefinitions')
	$colorNames = @('Color', 'Background', 'Foreground', 'BorderBrush', 'Fill', 'Stroke', 'SelectionBrush', 'CaretBrush')

	# A Thickness (1, 2 or 4 components) or a scalar double. Either separator,
	# since comma-only missed Margin="8 4 8 4". "Auto" and "*" never match.
	$numericValue = '^-?\d+(?:\.\d+)?(?:\s*[, ]\s*-?\d+(?:\.\d+)?){0,3}$'
	# A grid definition mixes literals with Auto/*; flag it when any component is a literal
	# length, since those are the sizing values that belong in a token.
	$gridNames = @('RowDefinitions', 'ColumnDefinitions')
	$gridLiteral = '(?:^|[, ])\s*-?\d+(?:\.\d+)?\s*(?:$|[, ])'

	$spacingPatterns = @(
		(Get-TokenHygieneAttributePattern -Names $spacingNames),
		(Get-TokenHygieneSetterPattern -Names $spacingNames)
	)
	$colorPatterns = @(
		(Get-TokenHygieneAttributePattern -Names $colorNames),
		(Get-TokenHygieneSetterPattern -Names $colorNames)
	)

	$commentMask = Get-TokenHygieneXmlCommentMask -Lines $Lines

	for ($i = 0; $i -lt $Lines.Count; $i++) {
		if ($commentMask[$i]) { continue }

		$lineNumber = $i + 1
		$trimmed = $Lines[$i].Trim()
		# Scan the markup only: a same-line comment must not excuse it.
		$line = Remove-TokenHygieneXmlComments -Line $Lines[$i]

		if (Test-TokenHygieneResourceDeclaration -Line $line) { continue }

		$colorHit = $false
		foreach ($pattern in $colorPatterns) {
			foreach ($m in [regex]::Matches($line, $pattern)) {
				$val = $m.Groups['val'].Value.Trim()
				if ($val.Length -eq 0 -or $val.StartsWith('{')) { continue }
				$colorHit = $true
			}
		}
		if ($colorHit) {
			[void]$violations.Add((New-TokenHygieneViolation $File $lineNumber 'hardcoded-color' $trimmed))
		}

		$spacingHit = $false
		foreach ($pattern in $spacingPatterns) {
			foreach ($m in [regex]::Matches($line, $pattern)) {
				$val = $m.Groups['val'].Value.Trim()
				if ($val.Length -eq 0 -or $val.StartsWith('{')) { continue }
				if ($val -notmatch $numericValue) { continue }
				if (Test-TokenHygieneAllZero -Value $val) { continue }
				$spacingHit = $true
			}
		}
		if (-not $spacingHit) {
			foreach ($m in [regex]::Matches($line,
				(Get-TokenHygieneAttributePattern -Names $gridNames))) {
				$val = $m.Groups['val'].Value.Trim()
				if ($val.Length -eq 0 -or $val.StartsWith('{')) { continue }
				if ($val -match $gridLiteral) { $spacingHit = $true }
			}
		}
		if ($spacingHit) {
			[void]$violations.Add((New-TokenHygieneViolation $File $lineNumber 'hardcoded-spacing' $trimmed))
		}
	}

	return ,$violations.ToArray()
}

function Get-TokenHygieneViolations {
	<#
	.SYNOPSIS
		Scans the given files for hardcoded-color/hardcoded-spacing
		violations.

	.PARAMETER Files
		Absolute (or repo-relative) paths to scan. A file whose extension
		Get-TokenHygieneLanguage does not recognize, or that
		Test-TokenHygieneExcludedPath excludes, is skipped -- this function
		re-checks the exclusion itself rather than trusting the caller, so a
		test fixture path or an ad-hoc file list still gets the real
		exclusion behavior.

	.OUTPUTS
		One PSCustomObject per violation: File, Line, Category ('hardcoded-color'
		or 'hardcoded-spacing'), Text (the trimmed source line).
	#>
	param([Parameter(Mandatory)][AllowEmptyCollection()][string[]] $Files)

	$violations = New-Object System.Collections.ArrayList
	foreach ($file in $Files) {
		if (-not (Test-Path -LiteralPath $file)) { continue }
		if (Test-TokenHygieneExcludedPath -Path $file) { continue }
		$language = Get-TokenHygieneLanguage -Path $file
		if ($null -eq $language) { continue }

		# File.ReadAllLines, not Get-Content: this check scans every line of
		# every in-scope file on every build, not just a diff.
		$lines = [System.IO.File]::ReadAllLines($file, [System.Text.Encoding]::UTF8)

		$fileViolations = if ($language -eq 'CSharp') {
			Get-TokenHygieneCSharpViolations -File $file -Lines $lines
		}
		else {
			Get-TokenHygieneXmlViolations -File $file -Lines $lines
		}
		foreach ($v in $fileViolations) { [void]$violations.Add($v) }
	}

	# The unary comma prevents PowerShell's pipeline from unrolling a
	# zero- or one-element array into $null or a bare scalar on return.
	return ,$violations.ToArray()
}

Export-ModuleMember -Function @(
	'Get-TokenHygieneScopeRoots',
	'Test-TokenHygieneExcludedPath',
	'Get-TokenHygieneLanguage',
	'Get-TokenHygieneScopedFiles',
	'Test-TokenHygieneAllZero',
	'Get-TokenHygieneCSharpViolations',
	'Get-TokenHygieneXmlCommentMask',
	'Get-TokenHygieneXmlViolations',
	'Get-TokenHygieneViolations'
)
