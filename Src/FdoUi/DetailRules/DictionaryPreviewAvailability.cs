// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// Where View > Show Dictionary Preview is enabled: in the Lexicon area tools whose edit
	/// view sits under a dictionary preview.
	/// </summary>
	public static class DictionaryPreviewAvailability
	{
		private static readonly string[] s_tools = { "simpleLexiconEdit", "lexiconTestEdit", "lexiconEdit", "lexiconFullEdit" };

		/// <summary>
		/// Whether the preview toggle is enabled in area <paramref name="areaChoice"/> with
		/// <paramref name="lexiconTool"/> the Lexicon area's current tool.
		/// </summary>
		public static bool IsOffered(string areaChoice, string lexiconTool)
			=> areaChoice == "lexicon" && s_tools.Contains(lexiconTool);
	}
}
