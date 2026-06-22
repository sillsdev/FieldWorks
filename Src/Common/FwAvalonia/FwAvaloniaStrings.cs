// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Resources;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Localized product-facing strings for the Avalonia lexical-edit surfaces (task 6.11). Standard
	/// .resx-backed resources (Crowdin-compatible); automation ids remain nonlocalized constants in
	/// code, never resource lookups.
	/// </summary>
	public static class FwAvaloniaStrings
	{
		private static readonly ResourceManager Resources =
			new ResourceManager("FwAvalonia.FwAvaloniaStrings", typeof(FwAvaloniaStrings).Assembly);

		public static string NoEntrySelected => Resources.GetString("ksNoEntrySelected");

		public static string EntryTypeUnsupported => Resources.GetString("ksEntryTypeUnsupported");

		public static string UnsupportedEditor => Resources.GetString("ksUnsupportedEditor");

		/// <summary>
		/// Tooltip on a text row whose value carries an embedded object (ORC) the managed editor
		/// holds read-only (task 8.3 deferral — embedded-object editing rides a later wave).
		/// </summary>
		public static string EmbeddedObjectReadOnly => Resources.GetString("ksEmbeddedObjectReadOnly");

		public static string Save => Resources.GetString("ksSave");

		public static string Cancel => Resources.GetString("ksCancel");

		public static string UndoEditEntry => Resources.GetString("ksUndoEditEntry");

		public static string RedoEditEntry => Resources.GetString("ksRedoEditEntry");

		public static string LexemeFormRequired => Resources.GetString("ksLexemeFormRequired");

		public static string LexicalEditRegionName => Resources.GetString("ksLexicalEditRegionName");

		public static string AvaloniaHostName => Resources.GetString("ksAvaloniaHostName");

		/// <summary>Accessible name for the browse table's check-all checkbox column (3c).</summary>
		public static string SelectAllRows => Resources.GetString("ksSelectAllRows");

		/// <summary>"Show All" — the browse filter preset that clears a column's blank-aware filter (3c).</summary>
		public static string FilterShowAll => Resources.GetString("ksFilterShowAll");

		/// <summary>"Blanks" — browse filter preset narrowing a column to rows whose cell is blank (3c).</summary>
		public static string FilterBlanks => Resources.GetString("ksFilterBlanks");

		/// <summary>"Non-blanks" — browse filter preset narrowing a column to rows whose cell is non-blank (3c).</summary>
		public static string FilterNonBlanks => Resources.GetString("ksFilterNonBlanks");

		public static string GhostAddPromptFormat => Resources.GetString("ksGhostAddPrompt");

		public static string Copy => Resources.GetString("ksCopy");

		/// <summary>"Remove" — reference-vector item context command (6.3).</summary>
		public static string Remove => Resources.GetString("ksRemove");

		/// <summary>"Add item" — reference-vector add-slot launcher name (6.3).</summary>
		public static string AddItem => Resources.GetString("ksAddItem");

		/// <summary>"Type to search" — the search-backed add slot's type-ahead watermark (D3).</summary>
		public static string SearchPrompt => Resources.GetString("ksSearchPrompt");

		/// <summary>"Add note" — the Chorus notes bar's add affordance (D2).</summary>
		public static string ChorusAddNote => Resources.GetString("ksChorusAddNote");

		/// <summary>"Add message" — append-message watermark in a Chorus note flyout (D2).</summary>
		public static string ChorusAddMessage => Resources.GetString("ksChorusAddMessage");

		/// <summary>"OK" — confirm button of the Chorus notes flyouts (D2).</summary>
		public static string ChorusOk => Resources.GetString("ksChorusOk");

		/// <summary>"Resolved" — the resolve toggle of a Chorus note flyout (D2).</summary>
		public static string ChorusResolved => Resources.GetString("ksChorusResolved");

		/// <summary>Accessible name of the "..." dialog-launcher button (D4).</summary>
		public static string LaunchDialog => Resources.GetString("ksLaunchDialog");

		/// <summary>Tooltip of a disabled launcher button: no host dialog service (D4).</summary>
		public static string LauncherUnavailable => Resources.GetString("ksLauncherUnavailable");

		/// <summary>"{0} settings" — accessible name of a chooser's hover-revealed settings gear.</summary>
		public static string FieldSettingsFormat => Resources.GetString("ksFieldSettings");

		/// <summary>
		/// "Edit the {0} list" — label/tooltip of a configure-gear jump derived from the row's
		/// possibility list (the legacy chooser dialog's "Edit the … list" link text).
		/// </summary>
		public static string EditListFormat => Resources.GetString("ksEditListFormat");

		/// <summary>"Lexeme Form" — first-slice row label (compiled override and authored fallback).</summary>
		public static string LexemeFormLabel => Resources.GetString("ksLexemeFormLabel");

		/// <summary>"Morph Type" — first-slice row label (authored fallback).</summary>
		public static string MorphTypeLabel => Resources.GetString("ksMorphTypeLabel");

		/// <summary>"Gloss" — first-slice row label (authored fallback).</summary>
		public static string GlossLabel => Resources.GetString("ksGlossLabel");
	}
}
