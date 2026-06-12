// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// Density tokens chosen to match the compact WinForms DataTree baseline (the "density"
	/// half of the fidelity question). Centralized so the parity comparison can tune one place.
	/// </summary>
	public static class PocDensity
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

		/// <summary>Compact margin around the slice.</summary>
		public static readonly Thickness SliceMargin = new Thickness(4, 2, 4, 2);

		// ---- Visual-fidelity tokens (section 12), sampled from the committed legacy render
		// ---- baseline + DataTree/Slice constants. One place to tune the legacy look.

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

		/// <summary>Legacy splitter width (Slice.cs SplitterWidth = 5).</summary>
		public const double SplitterWidth = 5.0;

		// ---- Popup density tokens: the legacy WinForms menus/choosers are far denser than the
		// ---- Fluent theme defaults; option pickers and context menus mirror that spacing.

		/// <summary>Compact padding of one option row in the option picker (legacy menu spacing).</summary>
		public static readonly Thickness OptionItemPadding = new Thickness(6, 2, 6, 2);

		/// <summary>The option picker's list cap: off-screen content scrolls instead of growing.</summary>
		public const double OptionListMaxHeight = 320.0;

		/// <summary>Compact context-menu item padding (legacy WinForms menu density, not Fluent).</summary>
		public static readonly Thickness MenuItemPadding = new Thickness(8, 3, 8, 3);

		/// <summary>Compact context-menu item height floor (legacy items are ~22px, Fluent ~32px).</summary>
		public const double MenuItemMinHeight = 22.0;
	}
}
