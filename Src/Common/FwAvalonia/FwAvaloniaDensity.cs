// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Shared density and style tokens chosen to match the compact WinForms DataTree baseline.
	/// Centralized so parity tuning lands in one place.
	///
	/// PRECONDITION: every resource-backed property below resolves lazily via
	/// <see cref="FwThemeResources"/>, which reads <see cref="Avalonia.Application.Current"/> --
	/// it throws (<see cref="System.InvalidOperationException"/> if the Application has not
	/// started yet, or an invalid-cast exception if a key resolves to the wrong CLR type) rather
	/// than returning a stale or default value. Only call these from code that runs after the
	/// Avalonia Application has initialized (e.g. a dialog/view constructor at display time), not
	/// from a static field initializer or other type-load-time code path.
	/// </summary>
	public static class FwAvaloniaDensity
	{
		/// <summary>
		/// Width of the field label column: the legacy WinForms default (DataTree.cs
		/// m_sliceSplitPositionBase = 150).
		/// </summary>
		public static double LabelColumnWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_LabelColumnWidth);

		/// <summary>
		/// Floor width of the writing-system abbreviation gutter column, 60px, matching the
		/// legacy Slice.MaxAbbrevWidth cap; a view widens it up to <see cref="WsAbbrevMaxWidth"/>
		/// for longer abbreviations.
		/// </summary>
		public static double WsAbbrevWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_WsAbbrevWidth);

		/// <summary>
		/// Ceiling on the abbreviation gutter column, so one pathological abbreviation cannot eat
		/// the pane.
		/// </summary>
		public static double WsAbbrevMaxWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_WsAbbrevMaxWidth);

		/// <summary>
		/// The clear separation kept between the writing-system abbreviation and the value (the
		/// abbreviation's trailing margin inside its gutter column), so the raised label reads as
		/// distinct.
		/// </summary>
		public static double WsAbbrevGutter => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_WsAbbrevGutter);

		/// <summary>Vertical spacing between writing-system rows within a field.</summary>
		public static double RowSpacing => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_RowSpacing);

		/// <summary>Vertical spacing between fields.</summary>
		public static double FieldSpacing => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_FieldSpacing);

		/// <summary>Compact padding inside text editors.</summary>
		public static Thickness EditorPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_EditorPadding);

		/// <summary>
		/// Compact list/table row height (legacy XMLViews rows are ~17px), replacing the taller
		/// Fluent ListBoxItem floor. Also the row-height budget the checkbox/radio glyph sizes
		/// below are derived from.
		/// </summary>
		public static double BrowseRowMinHeight => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_BrowseRowMinHeight);

		/// <summary>
		/// The DETERMINISTIC, GLOBAL checkbox glyph-box size (px), a fixed function of the
		/// surface font ( <see cref="FwSurfaceStyles.SurfaceFontSize"/> ): the box reads about as
		/// tall as a capital letter. <see cref="FwSemiDensity"/> retargets Semi's
		/// CheckBoxBoxWidth/Height resource tokens to this value, so a checkbox never inflates a
		/// browse/list/tree/table row past the text-row height ( <see cref="BrowseRowMinHeight"/>
		/// = 18).
		/// </summary>
		public const double CheckboxBoxSize = 14d;

		/// <summary>
		/// The gap between a checkbox box and its label text, so the words never butt against the
		/// box (the deterministic CheckBox template uses this as the box->label spacing). ~6px
		/// reads as a clear gap at the surface font size, matching the breathing room a radio
		/// button has.
		/// </summary>
		public const double CheckboxLabelGap = 6d;

		/// <summary>
		/// The DETERMINISTIC, GLOBAL radio-button outer-ring size (px), the radio counterpart of
		/// <see cref="CheckboxBoxSize"/> -- the same 14px so a radio and a checkbox read at the
		/// same density and neither inflates a row past the text line.
		/// <see cref="FwSemiDensity"/> retargets Semi's RadioButtonIconRadius resource token to
		/// this value.
		/// </summary>
		public const double RadioBoxSize = CheckboxBoxSize;

		/// <summary>
		/// A small amount of visual distance between adjacent logical control GROUPS (e.g. a
		/// radio group and the checkbox group that follows it in FilterForDialogView), so the
		/// groups read as distinct rather than butting together. ~8px of extra top whitespace,
		/// optionally paired with a thin grey 1px separator ( <see cref="SliceRuleBrush"/> ) for
		/// the clearest cases.
		/// </summary>
		public static double GroupSeparation => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_GroupSeparation);

		/// <summary>
		/// The selected browse/table row fill -- the legacy pale blue (XmlBrowseViewBaseVc
		/// kclrBackgroundSelRow 0xFFE6D7 = RGB 215,230,255) rather than the Fluent accent, so the
		/// whole selected row (including the first column) reads as highlighted like the WinForms
		/// browse.
		/// </summary>
		public static Avalonia.Media.IBrush SelectedRowBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwSelectedRowBrush);

		/// <summary>Compact margin around the slice.</summary>
		public static Thickness SliceMargin => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_SliceMargin);

		/// <summary>Slice label text colour: DimGray (#696969), measured from the legacy
		/// baseline.</summary>
		public static Avalonia.Media.IBrush LabelBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwLabelBrush);

		/// <summary>Slice label size: legacy 10pt (Slice.cs m_fontLabel).</summary>
		public const double LabelFontSize = 13.0;

		/// <summary>Writing-system abbreviation colour: #404040, measured from the legacy
		/// baseline.</summary>
		public static Avalonia.Media.IBrush WsAbbrevBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwWsAbbrevBrush);

		/// <summary>Writing-system abbreviation size (smaller than content, legacy style).</summary>
		public const double WsAbbrevFontSize = 11.0;

		/// <summary>The 1px rule between slices (DataTree.PaintLinesBetweenSlices, Color.LightGray).</summary>
		public static Avalonia.Media.IBrush SliceRuleBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwSliceRuleBrush);

		/// <summary>
		/// The thin grid line between browse rows and columns (the legacy XMLViews table draws
		/// faint cell separators); a touch lighter than LightGray so the grid reads as structure,
		/// not decoration.
		/// </summary>
		public static Avalonia.Media.IBrush BrowseGridLineBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwBrowseGridLineBrush);

		/// <summary>
		/// The browse table surface fill -- plain white like the legacy XMLViews browse. Resolves
		/// Semi's own base-surface role (SemiColorBackground0) directly: it is White in Light, an
		/// exact match with no FieldWorks divergence needed.
		/// </summary>
		public static Avalonia.Media.IBrush BrowseBackgroundBrush => FwThemeResources.RequireBrush("SemiColorBackground0");

		/// <summary>Legacy splitter width (Slice.cs SplitterWidth = 5).</summary>
		public static double SplitterWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_SplitterWidth);

		/// <summary>Compact padding of one option row in the option picker (legacy menu spacing).</summary>
		public static Thickness OptionItemPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_OptionItemPadding);

		/// <summary>The option picker's list cap: off-screen content scrolls instead of growing.</summary>
		public static double OptionListMaxHeight => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_OptionListMaxHeight);

		/// <summary>Compact context-menu item padding (legacy WinForms menu density, not Fluent).</summary>
		public static Thickness MenuItemPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_MenuItemPadding);

		/// <summary>Compact context-menu item height floor (legacy items are ~22px, Fluent ~32px).</summary>
		public static double MenuItemMinHeight => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_MenuItemMinHeight);

		/// <summary>
		/// The option picker panel surface (a light selection panel, not a menu). Resolves Semi's
		/// own base-surface role (SemiColorBackground0) directly, matching
		/// <see cref="BrowseBackgroundBrush"/> .
		/// </summary>
		public static Avalonia.Media.IBrush PickerBackgroundBrush => FwThemeResources.RequireBrush("SemiColorBackground0");

		/// <summary>The option picker panel border.</summary>
		public static Avalonia.Media.IBrush PickerBorderBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwPickerBorderBrush);

		/// <summary>
		/// The text color for the owned pickers, paired with the concrete
		/// <see cref="PickerBackgroundBrush"/> surface. Resolves Semi's own default-text role
		/// (SemiColorText0) directly: at #1c1f23 it is close enough to the prior
		/// FieldWorks-specific #1a1a1a that no divergence is warranted, and "default text on a
		/// light surface" is exactly what this property needs.
		/// </summary>
		public static Avalonia.Media.IBrush PickerForegroundBrush => FwThemeResources.RequireBrush("SemiColorText0");

		/// <summary>
		/// Inline validation-error text in the detail edit footer. Resolves Semi's own danger
		/// role (SemiColorDanger) directly.
		/// </summary>
		/// <remarks>This is a visible change from the prior Firebrick #B22222, and unlike
		/// FwLabelBrush it does not preserve a measured legacy value, because there is none:
		/// #B22222 appears in none of the 17 committed baseline images, so it was a chosen
		/// value rather than a sampled one. With nothing measured to preserve, Semi's role
		/// wins.</remarks>
		public static Avalonia.Media.IBrush ValidationErrorBrush => FwThemeResources.RequireBrush("SemiColorDanger");

		/// <summary>The heavy 2px rule above top-level section headers (legacy heavy separator).</summary>
		public static Avalonia.Media.IBrush SectionRuleBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwSectionRuleBrush);

		/// <summary>
		/// The horizontal indent applied per hierarchy level in an indented possibility list /
		/// POS tree row (the legacy chooser tree's per-depth inset). One source of truth so the
		/// tree picker and the option picker's depth-indented rows indent identically.
		/// </summary>
		public static double TreeIndentPerLevel => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_TreeIndentPerLevel);

		/// <summary>
		/// Compact width of the collapsed dropdown chooser (POS picker and similar) so the
		/// collapsed control reads as a field-sized box rather than shrinking to its current
		/// text.
		/// </summary>
		public static double DropdownMinWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_DropdownMinWidth);

		/// <summary>
		/// Fully transparent fill for a hit-test-only/hover-surface panel. Not theme-resolved,
		/// unlike the other brushes here: Transparent has no light/dark variance to justify a
		/// resource round-trip, and Avalonia's own Brushes.Transparent singleton is what callers
		/// comparing brush equality (e.g. visual-parity tests) expect to see, not a
		/// same-color-but-different-instance SolidColorBrush from a resource dictionary.
		/// </summary>
		public static Avalonia.Media.IBrush TransparentBrush => Avalonia.Media.Brushes.Transparent;

		/// <summary>
		/// Command-link blue for inline hotlink-style buttons. Resolves Semi's own link role
		/// (SemiColorLink) directly: "hyperlink" is exactly what this property needs, with no
		/// FieldWorks-specific divergence to justify.
		/// </summary>
		public static Avalonia.Media.IBrush HotlinkBrush => FwThemeResources.RequireBrush("SemiColorLink");

		/// <summary>Text for an unavailable/disabled option row in an owned picker.</summary>
		public static Avalonia.Media.IBrush DisabledOptionBrush => FwThemeResources.RequireBrush(GeneratedTokenKeys.FwDisabledOptionBrush);

		/// <summary>Margin around the inline validation-error message under a slice.</summary>
		public static Thickness ValidationMessageMargin => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_ValidationMessageMargin);

		/// <summary>Margin above/below the heavy section-header rule.</summary>
		public static Thickness SectionRuleMargin => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_SectionRuleMargin);

		/// <summary>Horizontal padding for a small flat span-acting button.</summary>
		public static Thickness CompactButtonPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_CompactButtonPadding);

		/// <summary>Small trailing gap between an inline item and the content that follows
		/// it.</summary>
		public static Thickness TrailingItemGap => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_TrailingItemGap);

		/// <summary>Padding for a small square glyph-only button.</summary>
		public static Thickness IconButtonPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_IconButtonPadding);

		/// <summary>
		/// Margin around the legacy VwSeparatorBox-style vertical bar between reference-vector
		/// items.
		/// </summary>
		public static Thickness SeparatorBarMargin => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_SeparatorBarMargin);

		/// <summary>A small trailing right-gap between a row's leading content and a trailing
		/// affordance.</summary>
		public static Thickness TrailingGap => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_TrailingGap);

		/// <summary>Uniform 1px border for a compact bordered host.</summary>
		public static Thickness HairlineBorderThickness => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_HairlineBorderThickness);

		/// <summary>Uniform compact padding for a bordered host's inner content.</summary>
		public static Thickness TightPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_TightPadding);

		/// <summary>Top margin separating a control group from the group above it.</summary>
		public static Thickness OptionGroupTopMargin => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_OptionGroupTopMargin);

		/// <summary>Padding for a prominent option-picker action row.</summary>
		public static Thickness OptionRowPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_OptionRowPadding);

		/// <summary>Small leading indent for a glyph that follows an option-picker
		/// label.</summary>
		public static Thickness OptionIndentMargin => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_OptionIndentMargin);

		/// <summary>Top-only 1px hairline border.</summary>
		public static Thickness TopHairlineBorderThickness => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_TopHairlineBorderThickness);

		/// <summary>Bottom-only 1px hairline border.</summary>
		public static Thickness BottomHairlineBorderThickness => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_BottomHairlineBorderThickness);

		/// <summary>Left-only 2px rule marking a structured-text paragraph row's
		/// boundary.</summary>
		public static Thickness ParagraphRuleBorderThickness => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_ParagraphRuleBorderThickness);

		/// <summary>Padding for one structured-text paragraph row.</summary>
		public static Thickness ParagraphRowPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_ParagraphRowPadding);

		/// <summary>Padding for a small hover-revealed chip/affordance.</summary>
		public static Thickness HoverChipPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_HoverChipPadding);

		/// <summary>
		/// Padding for one list/browse row of read-only content built from code. Kept numerically
		/// equal to DialogListBoxItemPadding in DialogTheme.axaml -- CHANGE BOTH TOGETHER.
		/// </summary>
		public static Thickness ListRowPadding => FwThemeResources.RequireThickness(GeneratedTokenKeys.DataTree_ListRowPadding);

		/// <summary>The legacy 1px inter-slice rule (DataTree.PaintLinesBetweenSlices).</summary>
		public static double SliceRuleHeight => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_SliceRuleHeight);

		/// <summary>The heavier 2px rule above a top-level section header.</summary>
		public static double SectionRuleHeight => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_SectionRuleHeight);

		/// <summary>Minimum width of the inline external-link URL prompt textbox.</summary>
		public static double LinkUrlMinWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_LinkUrlMinWidth);

		/// <summary>Horizontal gap between the link-prompt URL box and its Apply
		/// button.</summary>
		public static double LinkPromptGap => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_LinkPromptGap);

		/// <summary>
		/// Horizontal gap between a chooser field's value text and its trailing configure-gear
		/// glyph.
		/// </summary>
		public static double ChooserGearGap => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_ChooserGearGap);

		/// <summary>Width of the legacy VwSeparatorBox-style vertical bar between
		/// reference-vector items.</summary>
		public static double SeparatorBarWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_SeparatorBarWidth);

		/// <summary>
		/// Corner radius for a compact bordered host (option/POS picker frame, MSA/feature group
		/// box); pairs with <see cref="HairlineBorderThickness"/> and <see cref="TightPadding"/>
		/// .
		/// </summary>
		public static CornerRadius PickerCornerRadius => FwThemeResources.RequireCornerRadius(GeneratedTokenKeys.DataTree_PickerCornerRadius);

		/// <summary>
		/// Minimum width of the option/POS picker's own selection panel -- distinct from
		/// <see cref="DropdownMinWidth"/> , which sizes the COLLAPSED dropdown chooser.
		/// </summary>
		public static double PickerMinWidth => FwThemeResources.RequireDouble(GeneratedTokenKeys.DataTree_PickerMinWidth);

		/// <summary>
		/// The DETERMINISTIC, GLOBAL small-glyph icon size (px), the gear/kebab counterpart of
		/// <see cref="CheckboxBoxSize"/> -- the same 14px so every small glyph (checkbox, radio,
		/// gear, kebab) reads at one density and none inflates a row past the text-row height.
		/// </summary>
		public const double IconGlyphSize = CheckboxBoxSize;
	}
}
