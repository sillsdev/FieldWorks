// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The result fields a matching-list column can display -- the per-column values carried on
	/// <see cref="EntryGoSearchResult"/>. Mirrors the legacy matchingEntries browser's default-visible column
	/// content (areaConfiguration.xml "matchingEntries": Headword + Glosses visible, Lexeme Form on the menu).
	/// </summary>
	public enum EntryGoResultField
	{
		/// <summary>The entry's headword (<see cref="EntryGoSearchResult.Text"/>) -- a vernacular value.</summary>
		Headword,

		/// <summary>The entry's lexeme form (<see cref="EntryGoSearchResult.LexemeForm"/>) -- a vernacular value.</summary>
		LexemeForm,

		/// <summary>The gloss(es) (<see cref="EntryGoSearchResult.Gloss"/>) -- an analysis value.</summary>
		Gloss
	}

	/// <summary>
	/// One column of the entry-search ("go") dialog's persistent matching list -- the LCModel-free presentation
	/// spec for a column of the legacy <c>MatchingObjectsBrowser</c> (the multi-column browse view
	/// <c>BaseGoDlg</c> embeds). The launcher supplies an ordered list of these on
	/// <see cref="EntryGoDialogInput.ResultColumns"/>: a localized <see cref="Header"/>, the
	/// <see cref="EntryGoResultField"/> the column shows, and optional per-column <see cref="Typography"/>
	/// (reusing the <see cref="EntryGoSearchFieldSpec"/> shape) so a vernacular column renders in the vernacular
	/// font and a gloss column in the analysis font. A null/empty list on the input falls back to
	/// <see cref="DefaultColumns"/>.
	/// </summary>
	public sealed class EntryGoResultColumn
	{
		/// <summary>The localized column header text shown in the list's header row.</summary>
		public string Header { get; set; }

		/// <summary>Which result field this column displays in each row.</summary>
		public EntryGoResultField Field { get; set; }

		/// <summary>
		/// Optional writing-system typography for this column's cells (font family/size, right-to-left),
		/// reusing the search-box spec shape; the <see cref="EntryGoSearchFieldSpec.Focused"/> callback is
		/// ignored for columns. Null keeps the shared default text rendering.
		/// </summary>
		public EntryGoSearchFieldSpec Typography { get; set; }

		/// <summary>The column's proportional (star) width share; defaults to an equal share.</summary>
		public double Width { get; set; } = 1;

		/// <summary>
		/// The shared fallback column set when a consumer supplies none: Headword + Glosses with default
		/// typography -- the legacy matchingEntries browser's default-visible columns
		/// (areaConfiguration.xml: "Headword" ws=best vernoranal and "Glosses" ws=best analorvern are the only
		/// columns without visibility="menu"). Headers come from the shared localized strings.
		/// </summary>
		public static IReadOnlyList<EntryGoResultColumn> DefaultColumns() => new[]
		{
			new EntryGoResultColumn
			{
				Header = FwAvaloniaDialogsStrings.EntryGoHeadwordColumnHeader,
				Field = EntryGoResultField.Headword
			},
			new EntryGoResultColumn
			{
				Header = FwAvaloniaDialogsStrings.EntryGoGlossesColumnHeader,
				Field = EntryGoResultField.Gloss
			}
		};
	}
}
