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

		public static string Save => Resources.GetString("ksSave");

		public static string Cancel => Resources.GetString("ksCancel");

		public static string UndoEditEntry => Resources.GetString("ksUndoEditEntry");

		public static string RedoEditEntry => Resources.GetString("ksRedoEditEntry");

		public static string LexemeFormRequired => Resources.GetString("ksLexemeFormRequired");

		public static string LexicalEditRegionName => Resources.GetString("ksLexicalEditRegionName");

		public static string AvaloniaHostName => Resources.GetString("ksAvaloniaHostName");

		public static string GhostAddPromptFormat => Resources.GetString("ksGhostAddPrompt");

		public static string Copy => Resources.GetString("ksCopy");

		/// <summary>"Remove" — reference-vector item context command (6.3).</summary>
		public static string Remove => Resources.GetString("ksRemove");

		/// <summary>"Add item" — reference-vector add-slot launcher name (6.3).</summary>
		public static string AddItem => Resources.GetString("ksAddItem");
	}
}
