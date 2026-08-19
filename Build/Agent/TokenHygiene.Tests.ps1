<#
.SYNOPSIS
	Fixture-based tests for TokenHygiene.psm1.

.DESCRIPTION
	One true-positive and one true-negative per violation category, plus
	the allow-list and comment-blanking behavior the check depends on to
	avoid flagging its own plumbing or prose. Run directly:
	pwsh -File Build/Agent/TokenHygiene.Tests.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'TokenHygiene.psm1') -Force

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# Deliberately avoids the substring "Tests" in this directory name: that would trip the
# check's own *Tests* path exclusion and silently skip every fixture file below.
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("TokenHygieneFixtures_" + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

$failures = New-Object System.Collections.ArrayList

function Assert-TokenCategory {
	param([string] $Name, [string[]] $Lines, [string] $ExpectedCategory, [string] $Extension = '.cs')

	$file = Join-Path $tempDir "$Name$Extension"
	Set-Content -LiteralPath $file -Value $Lines -Encoding UTF8
	$violations = Get-TokenHygieneViolations -Files @($file)
	$hit = $violations | Where-Object { $_.Category -eq $ExpectedCategory }
	if (-not $hit) {
		[void]$script:failures.Add("FAIL [$Name]: expected category '$ExpectedCategory' for: $($Lines -join ' / ')")
	}
}

function Assert-TokenClean {
	param([string] $Name, [string[]] $Lines, [string] $Extension = '.cs')

	$file = Join-Path $tempDir "$Name$Extension"
	Set-Content -LiteralPath $file -Value $Lines -Encoding UTF8
	$violations = Get-TokenHygieneViolations -Files @($file)
	if ($violations.Count -gt 0) {
		$hitCategories = ($violations | ForEach-Object { "$($_.Category)@$($_.Line)" }) -join ','
		[void]$script:failures.Add("FAIL [$Name]: expected no violations for: $($Lines -join ' / ') -- got $hitCategories")
	}
}

function Assert-ExcludedPath {
	param([string] $Name, [string] $Path, [bool] $Expected)

	$actual = Test-TokenHygieneExcludedPath -Path $Path
	if ($actual -ne $Expected) {
		[void]$script:failures.Add("FAIL [$Name]: expected Test-TokenHygieneExcludedPath('$Path') = $Expected, got $actual")
	}
}

# ---- hardcoded-color: C# ----

Assert-TokenCategory 'cs-color-solidcolorbrush' @(
	'var brush = new SolidColorBrush(Colors.Red);'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-fromrgb' @(
	'var color = Color.FromRgb(0x69, 0x69, 0x69);'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-bare-brushes' @(
	'control.Background = Brushes.Black;'
) 'hardcoded-color'

Assert-TokenClean 'cs-color-clean-token' @(
	'control.Background = FwThemeResources.RequireBrush("FwLabelBrush");'
)

Assert-TokenClean 'cs-color-clean-density' @(
	'control.Background = FwAvaloniaDensity.LabelBrush;'
)

# A comment mentioning the banned pattern is not code -- must not be flagged.
Assert-TokenClean 'cs-color-comment-not-flagged' @(
	'// Avoid new SolidColorBrush(Color.FromRgb(1, 2, 3)) here.'
)

Assert-TokenCategory 'cs-color-immutable-solidcolorbrush' @(
	'var brush = new ImmutableSolidColorBrush(Colors.Red);'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-fromargb' @(
	'var color = Color.FromArgb(255, 0, 0, 0);'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-fromuint32' @(
	'var color = Color.FromUInt32(0xFF000000);'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-parse' @(
	'var color = Color.Parse("#ABCDEF");'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-solidcolorbrush-parse' @(
	'var brush = SolidColorBrush.Parse("Red");'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-brush-parse' @(
	'var brush = Brush.Parse("Red");'
) 'hardcoded-color'

Assert-TokenCategory 'cs-color-bare-colors' @(
	'control.Fill = Colors.Red;'
) 'hardcoded-color'

Assert-TokenClean 'cs-color-clean-density-not-colors-suffix' @(
	'control.Background = MyColors.Red;'
)

# ---- hardcoded-spacing: C# ----

Assert-TokenCategory 'cs-spacing-thickness' @(
	'var margin = new Thickness(4, 2, 4, 2);'
) 'hardcoded-spacing'

Assert-TokenClean 'cs-spacing-thickness-zero' @(
	'var margin = new Thickness(0);'
)

Assert-TokenClean 'cs-spacing-thickness-nonliteral' @(
	'var margin = new Thickness(labelGap, fieldGap, labelGap, fieldGap);'
)

Assert-TokenClean 'cs-spacing-clean-token' @(
	'var margin = FwAvaloniaDensity.SliceMargin;'
)

Assert-TokenCategory 'cs-spacing-cornerradius' @(
	'CornerRadius = new CornerRadius(3);'
) 'hardcoded-spacing'

Assert-TokenClean 'cs-spacing-cornerradius-zero' @(
	'CornerRadius = new CornerRadius(0);'
)

Assert-TokenCategory 'cs-spacing-property-assign-comma' @(
	'MinWidth = 220,'
) 'hardcoded-spacing'

Assert-TokenCategory 'cs-spacing-property-assign-semicolon' @(
	'MinWidth = 180;'
) 'hardcoded-spacing'

Assert-TokenCategory 'cs-spacing-property-assign-brace' @(
	'var rule = new Border { Background = Brush, Height = 1 };'
) 'hardcoded-spacing'

Assert-TokenClean 'cs-spacing-property-assign-variable' @(
	'MinWidth = wsAbbrevColumnWidth,'
)

Assert-TokenClean 'cs-spacing-property-assign-expression' @(
	'MinWidth = FwAvaloniaDensity.DropdownMinWidth + 20,'
)

Assert-TokenClean 'cs-spacing-property-assign-equality' @(
	'if (control.Width == 14) { DoSomething(); }'
)

Assert-TokenClean 'cs-spacing-property-assign-zero' @(
	'MinHeight = 0,'
)

Assert-TokenCategory 'cs-spacing-setter-literal' @(
	'theme.Setters.Add(new Setter(Foo.BarProperty, 12));'
) 'hardcoded-spacing'

Assert-TokenClean 'cs-spacing-setter-variable' @(
	'theme.Setters.Add(new Setter(ListBoxItem.PaddingProperty, padding));'
)

Assert-TokenClean 'cs-spacing-setter-zero' @(
	'theme.Setters.Add(new Setter(Layoutable.MinHeightProperty, 0.0));'
)

# ---- hardcoded-color: XAML ----

Assert-TokenCategory 'xaml-color-background' @(
	'<Border Background="Red"/>'
) 'hardcoded-color' '.axaml'

Assert-TokenCategory 'xaml-color-setter' @(
	'<Style Selector="Border"><Setter Property="Foreground" Value="#404040"/></Style>'
) 'hardcoded-color' '.axaml'

Assert-TokenClean 'xaml-color-clean-staticresource' @(
	'<Border Background="{StaticResource FwBrowseBackgroundBrush}"/>'
) '.axaml'

Assert-TokenClean 'xaml-color-clean-dynamicresource' @(
	'<Border BorderBrush="{DynamicResource FwDialogFieldBorderBrush}"/>'
) '.axaml'

# A resource declaration is not a usage -- must not be flagged even though it carries
# a literal Color value.
Assert-TokenClean 'xaml-color-clean-declaration' @(
	'<SolidColorBrush x:Key="FwLabelBrush" Color="#696969"/>'
) '.axaml'

# A comment quoting the banned pattern is not markup -- must not be flagged.
Assert-TokenClean 'xaml-color-comment-not-flagged' @(
	'<!-- Example: Background="Red" is what this check exists to catch. -->',
	'<Border Background="{StaticResource FwBrowseBackgroundBrush}"/>'
) '.axaml'

# A multi-line XML comment blanks every line it spans, not just the first.
Assert-TokenClean 'xaml-color-multiline-comment-not-flagged' @(
	'<!--',
	'    Old markup used Background="Red" here; kept only as history.',
	'-->',
	'<Border Background="{StaticResource FwBrowseBackgroundBrush}"/>'
) '.axaml'

# ---- hardcoded-spacing: XAML ----

Assert-TokenCategory 'xaml-spacing-padding' @(
	'<TextBox Padding="4,2"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenCategory 'xaml-spacing-setter' @(
	'<Style Selector="TextBox"><Setter Property="MinHeight" Value="24"/></Style>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenClean 'xaml-spacing-clean-staticresource' @(
	'<TextBox Padding="{StaticResource DialogTextBoxPadding}"/>'
) '.axaml'

Assert-TokenClean 'xaml-spacing-clean-zero' @(
	'<Border Margin="0"/>'
) '.axaml'

Assert-TokenClean 'xaml-spacing-clean-zero-thickness' @(
	'<Border Margin="0,0,0,0"/>'
) '.axaml'

Assert-TokenClean 'xaml-spacing-clean-auto' @(
	'<ColumnDefinition Width="Auto"/>'
) '.axaml'

Assert-TokenClean 'xaml-spacing-clean-star' @(
	'<ColumnDefinition Width="*"/>'
) '.axaml'

Assert-TokenClean 'xaml-spacing-clean-declaration' @(
	'<Thickness x:Key="DataTree.SliceMargin">4,2,4,2</Thickness>'
) '.axaml'

Assert-TokenCategory 'xaml-spacing-maxheight' @(
	'<ListBox MaxHeight="120"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenCategory 'xaml-spacing-maxwidth' @(
	'<TextBox MaxWidth="200"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenCategory 'xaml-spacing-rowspacing' @(
	'<Grid RowSpacing="4"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenCategory 'xaml-spacing-columnspacing' @(
	'<Grid ColumnSpacing="6"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenCategory 'xaml-spacing-strokethickness' @(
	'<Path StrokeThickness="2"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenCategory 'xaml-spacing-cornerradius' @(
	'<Border CornerRadius="3"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenCategory 'xaml-spacing-borderthickness' @(
	'<Border BorderThickness="1"/>'
) 'hardcoded-spacing' '.axaml'

Assert-TokenClean 'xaml-spacing-clean-maxheight-staticresource' @(
	'<ListBox MaxHeight="{StaticResource InsertEntryMatchesListMaxHeight}"/>'
) '.axaml'

# ---- hardcoded-color: XAML (widened names) ----

Assert-TokenCategory 'xaml-color-fill' @(
	'<Path Fill="Red"/>'
) 'hardcoded-color' '.axaml'

Assert-TokenCategory 'xaml-color-stroke' @(
	'<Path Stroke="Blue"/>'
) 'hardcoded-color' '.axaml'

Assert-TokenCategory 'xaml-color-selectionbrush' @(
	'<TextBox SelectionBrush="Red"/>'
) 'hardcoded-color' '.axaml'

Assert-TokenCategory 'xaml-color-caretbrush' @(
	'<TextBox CaretBrush="Blue"/>'
) 'hardcoded-color' '.axaml'

# ---- allow-list ----

Assert-ExcludedPath 'excluded-theme-resources' (Join-Path $repoRoot 'Src/Common/FwAvalonia/FwThemeResources.cs') $true
Assert-ExcludedPath 'excluded-density' (Join-Path $repoRoot 'Src/Common/FwAvalonia/FwAvaloniaDensity.cs') $true
Assert-ExcludedPath 'excluded-semi-density' (Join-Path $repoRoot 'Src/Common/FwAvalonia/FwSemiDensity.cs') $true
Assert-ExcludedPath 'excluded-compact-dialog-styles' (Join-Path $repoRoot 'Src/Common/FwAvalonia/CompactDialogStyles.cs') $true
Assert-ExcludedPath 'excluded-surface-styles' (Join-Path $repoRoot 'Src/Common/FwAvalonia/FwSurfaceStyles.cs') $true
Assert-ExcludedPath 'excluded-tests-dir' (Join-Path $repoRoot 'Src/Common/FwAvalonia/FwAvaloniaTests/SomeTest.cs') $true
Assert-ExcludedPath 'excluded-designer' (Join-Path $repoRoot 'Src/Common/FwAvaloniaDialogs/Foo.Designer.cs') $true
Assert-ExcludedPath 'excluded-generated' (Join-Path $repoRoot 'Src/Common/FwAvaloniaDialogs/Foo.g.cs') $true

# DialogTheme.axaml and the Tokens/ dictionaries get no path-level exclusion; only a
# declaration's own line is exempt (proven below), so other literals stay policed.
Assert-ExcludedPath 'not-excluded-dialog-theme' (Join-Path $repoRoot 'Src/Common/FwAvaloniaDialogs/DialogTheme.axaml') $false
Assert-ExcludedPath 'not-excluded-tokens-dir' (Join-Path $repoRoot 'Src/Common/FwAvaloniaTheme/Tokens/FwColorTokens.axaml') $false
Assert-ExcludedPath 'not-excluded-tokens-subdir' (Join-Path $repoRoot 'Src/Common/FwAvaloniaTheme/Tokens/DataTree/DataTreeTokens.axaml') $false
Assert-ExcludedPath 'not-excluded-ordinary-cs' (Join-Path $repoRoot 'Src/Common/FwAvalonia/Detail/DataTree.cs') $false

# A token's own x:Key declaration line stays clean via the per-line exemption, regardless
# of which directory the file lives in.
Assert-TokenClean 'xaml-color-token-declaration-clean' @(
	'<SolidColorBrush x:Key="FwLabelBrush" Color="#696969"/>'
) '.axaml'

# ---- scope roots ----

$scopeRoots = Get-TokenHygieneScopeRoots
foreach ($expected in @(
	'Src/Common/FwAvalonia',
	'Src/Common/FwAvaloniaDialogs',
	'Src/Common/FwAvaloniaTheme',
	'Src/Common/FwAvaloniaPreviewHost',
	'Src/LexText/LexTextControls/Avalonia',
	'Src/xWorks/Avalonia'
)) {
	if ($scopeRoots -notcontains $expected) {
		[void]$failures.Add("FAIL [scope-roots]: expected '$expected' in Get-TokenHygieneScopeRoots")
	}
}

# --- Evasions closed after review. Each of these passed clean before the narrowing, so each
# --- one is a regression test for a way the check could be defeated on purpose or by accident.

# An x:Key on a layout element is not a token declaration: only the primitive resource types
# are exempt, so this must still be flagged on both counts.
Assert-TokenCategory 'xaml-xkey-on-layout-element-color' @(
	'<Border x:Key="X" Background="#FF0000" Margin="40" Padding="12,8"/>'
) 'hardcoded-color' '.axaml'
Assert-TokenCategory 'xaml-xkey-on-layout-element-spacing' @(
	'<Border x:Key="X" Background="#FF0000" Margin="40" Padding="12,8"/>'
) 'hardcoded-spacing' '.axaml'

# A same-line comment must not excuse the markup beside it.
Assert-TokenCategory 'xaml-trailing-comment-does-not-excuse' @(
	'<Border Background="#00FF00"/> <!-- note -->'
) 'hardcoded-color' '.axaml'

# Avalonia accepts space-separated Thickness, so a comma-only numeric check missed this.
Assert-TokenCategory 'xaml-space-separated-thickness' @(
	'<Border Margin="8 4 8 4"/>'
) 'hardcoded-spacing' '.axaml'

# Grid sizing literals belong in a token; Auto and * beside them are fine.
Assert-TokenCategory 'xaml-grid-definition-literals' @(
	'<Grid RowDefinitions="40,Auto" ColumnDefinitions="220,*"/>'
) 'hardcoded-spacing' '.axaml'
Assert-TokenClean 'xaml-grid-definition-no-literals' @(
	'<Grid RowDefinitions="Auto,*" ColumnDefinitions="Auto,*"/>'
) '.axaml'

# C# properties review named as missing from the assignment list.
Assert-TokenCategory 'cs-opacity-literal' @(
	'var x = new Border { Opacity = 0.45, };'
) 'hardcoded-spacing'
Assert-TokenCategory 'cs-margin-literal' @(
	'control.Margin = 8;'
) 'hardcoded-spacing'

# A cast or a numeric suffix must not smuggle a literal past the assignment pattern.
Assert-TokenCategory 'cs-cast-literal' @(
	'control.Height = (double)18;'
) 'hardcoded-spacing'
Assert-TokenCategory 'cs-suffix-literal' @(
	'control.MinWidth = 160.0m;'
) 'hardcoded-spacing'

# A genuine primitive resource declaration stays exempt -- the literal there is the token.
Assert-TokenClean 'xaml-primitive-resource-declaration-still-clean' @(
	'<Thickness x:Key="FwDialogPadding">12,8</Thickness>',
	'<SolidColorBrush x:Key="FwLabelBrush" Color="#696969"/>'
) '.axaml'

Remove-Item -LiteralPath $tempDir -Recurse -Force

if ($failures.Count -gt 0) {
	Write-Host ''
	foreach ($f in $failures) { Write-Host $f -ForegroundColor Red }
	Write-Host ''
	Write-Host "$($failures.Count) test(s) failed." -ForegroundColor Red
	exit 1
}

Write-Host 'All TokenHygiene tests passed.' -ForegroundColor Green
exit 0
