// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Shared density and style tokens chosen to match the compact WinForms DataTree baseline.
	/// Centralized so parity tuning lands in one place.
	/// </summary>
	public static class FwAvaloniaDensity
	{
		/// <summary>Width of the field label column.</summary>
		public const double LabelColumnWidth = 96d;

		/// <summary>Width of the small writing-system abbreviation gutter.</summary>
		public const double WsAbbrevWidth = 28d;

		/// <summary>Vertical spacing between writing-system rows within a field.</summary>
		public const double RowSpacing = 1d;

		/// <summary>Vertical spacing between fields.</summary>
		public const double FieldSpacing = 2d;

		/// <summary>Compact padding inside text editors.</summary>
		public static readonly Thickness EditorPadding = new Thickness(3, 1, 3, 1);

		/// <summary>Compact padding for a browse/table row container (no vertical padding; height comes
		/// from <see cref="BrowseRowMinHeight"/>), matching the legacy XMLViews row inset.</summary>
		public static readonly Thickness BrowseRowPadding = new Thickness(3, 0, 3, 0);

		/// <summary>Compact browse/table row height (legacy XMLViews rows are ~17px), replacing the
		/// taller Fluent ListBoxItem floor.</summary>
		public const double BrowseRowMinHeight = 18d;

		/// <summary>The selected browse/table row fill — the legacy pale blue (XmlBrowseViewBaseVc
		/// kclrBackgroundSelRow 0xFFE6D7 = RGB 215,230,255) rather than the Fluent accent, so the whole
		/// selected row (including the first column) reads as highlighted like the WinForms browse.</summary>
		public static readonly Avalonia.Media.IBrush SelectedRowBrush =
			new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0xD7, 0xE6, 0xFF));

		/// <summary>Compact margin around the slice.</summary>
		public static readonly Thickness SliceMargin = new Thickness(4, 2, 4, 2);

		/// <summary>Slice label text (legacy label hue from the committed baseline pixels).</summary>
		public static readonly Avalonia.Media.IBrush LabelBrush =
			new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x66, 0x66, 0xB8));

		/// <summary>Slice label size: legacy 10pt (Slice.cs m_fontLabel).</summary>
		public const double LabelFontSize = 13.0;

		/// <summary>Writing-system abbreviation: small raised blue (legacy AbbreviationTextProperties).</summary>
		public static readonly Avalonia.Media.IBrush WsAbbrevBrush =
			new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x46, 0x82, 0xB4));

		/// <summary>Writing-system abbreviation size (smaller than content, legacy style).</summary>
		public const double WsAbbrevFontSize = 11.0;

		/// <summary>The 1px rule between slices (DataTree.PaintLinesBetweenSlices, Color.LightGray).</summary>
		public static readonly Avalonia.Media.IBrush SliceRuleBrush = Avalonia.Media.Brushes.LightGray;

		/// <summary>The thin grid line between browse rows and columns (the legacy XMLViews table draws
		/// faint cell separators); a touch lighter than LightGray so the grid reads as structure, not chrome.</summary>
		public static readonly Avalonia.Media.IBrush BrowseGridLineBrush =
			new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0xDC, 0xDC, 0xDC));

		/// <summary>The browse table surface fill — plain white like the legacy XMLViews browse, rather
		/// than the Fluent panel tint.</summary>
		public static readonly Avalonia.Media.IBrush BrowseBackgroundBrush = Avalonia.Media.Brushes.White;

		/// <summary>Legacy splitter width (Slice.cs SplitterWidth = 5).</summary>
		public const double SplitterWidth = 5.0;

		/// <summary>Compact padding of one option row in the option picker (legacy menu spacing).</summary>
		public static readonly Thickness OptionItemPadding = new Thickness(6, 2, 6, 2);

		/// <summary>The option picker's list cap: off-screen content scrolls instead of growing.</summary>
		public const double OptionListMaxHeight = 320.0;

		/// <summary>Compact context-menu item padding (legacy WinForms menu density, not Fluent).</summary>
		public static readonly Thickness MenuItemPadding = new Thickness(8, 3, 8, 3);

		/// <summary>Compact context-menu item height floor (legacy items are ~22px, Fluent ~32px).</summary>
		public const double MenuItemMinHeight = 22.0;

		/// <summary>The option picker panel surface (a light selection panel, not a menu).</summary>
		public static readonly Avalonia.Media.IBrush PickerBackgroundBrush = Avalonia.Media.Brushes.White;

		/// <summary>The option picker panel border.</summary>
		public static readonly Avalonia.Media.IBrush PickerBorderBrush = Avalonia.Media.Brushes.LightGray;

		/// <summary>Inline validation-error text in the region edit footer.</summary>
		public static readonly Avalonia.Media.IBrush ValidationErrorBrush = Avalonia.Media.Brushes.Firebrick;

		/// <summary>The heavy 2px rule above top-level section headers (legacy heavy separator).</summary>
		public static readonly Avalonia.Media.IBrush SectionRuleBrush = Avalonia.Media.Brushes.LightGray;
	}
}