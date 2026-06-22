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
	}
}
