// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free writing-system presentation spec for the entry-search ("go") dialog's search box — the
	/// parity contract for the legacy <c>BaseGoDlg</c>'s vernacular <c>FwTextBox</c>, which renders the typed query
	/// in the writing system's default font, honors its right-to-left script, and switches to its keyboard on focus
	/// (the legacy <c>EditingHelper.SetKeyboardForWs</c> behavior). The launcher derives the values from the live
	/// writing system and the Avalonia layer applies them without ever seeing LCModel. A null spec on
	/// <see cref="EntryGoDialogInput.SearchField"/> leaves the search box entirely at kit defaults.
	/// </summary>
	public sealed class EntryGoSearchFieldSpec
	{
		/// <summary>The font family the query renders in (the writing system's default font); null/empty keeps the
		/// kit's default font.</summary>
		public string FontFamily { get; set; }

		/// <summary>The font size in points; 0 (the default) keeps the kit's default size — the same
		/// zero-means-default convention the detail surface's per-ws value rows use.</summary>
		public double FontSize { get; set; }

		/// <summary>True renders the search box right-to-left (the writing system uses a right-to-left script).</summary>
		public bool RightToLeft { get; set; }

		/// <summary>
		/// Invoked each time the search box gains keyboard focus, so the launcher can activate the writing
		/// system's keyboard (the legacy vernacular FwTextBox switched keyboards on focus). Null means no-op.
		/// </summary>
		public Action Focused { get; set; }
	}
}
