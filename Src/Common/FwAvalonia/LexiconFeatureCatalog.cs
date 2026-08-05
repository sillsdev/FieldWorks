// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Display metadata (name, description, group) for one tool in
	/// <see cref="LexiconFeatureCatalog"/>. Purely descriptive — <see cref="ToolName"/> is the same
	/// id <see cref="UIFrameworkRegistry"/> and <see cref="UIFrameworkResolver"/> key on.
	/// </summary>
	public sealed class LexiconFeatureDescriptor
	{
		public LexiconFeatureDescriptor(string toolName, string displayName, string description, string groupName)
		{
			ToolName = toolName;
			DisplayName = displayName;
			Description = description;
			GroupName = groupName;
		}

		public string ToolName { get; }
		public string DisplayName { get; }
		public string Description { get; }
		public string GroupName { get; }
	}

	/// <summary>
	/// The single source of truth for which tools ship an Avalonia detail view and how to describe them to
	/// a user. <see cref="UIFrameworkRegistry.DefaultSupportedTools"/> is built from this list, so the
	/// registry and the user-facing catalog can never drift out of sync — there is exactly one list of
	/// "tools that ship with working Avalonia support today."
	/// </summary>
	public static class LexiconFeatureCatalog
	{
		public static readonly IReadOnlyList<LexiconFeatureDescriptor> Features = new[]
		{
			new LexiconFeatureDescriptor("lexiconEdit",
				FwAvaloniaStrings.FeatureLexiconEditName, FwAvaloniaStrings.FeatureLexiconEditDescription,
				FwAvaloniaStrings.FeatureGroupLexicalEntryDialogs),
			new LexiconFeatureDescriptor("lexiconEditPopup",
				FwAvaloniaStrings.FeatureLexiconEditPopupName, FwAvaloniaStrings.FeatureLexiconEditPopupDescription,
				FwAvaloniaStrings.FeatureGroupLexicalEntryDialogs),
			new LexiconFeatureDescriptor("notebookEdit",
				FwAvaloniaStrings.FeatureNotebookEditName, FwAvaloniaStrings.FeatureNotebookEditDescription,
				FwAvaloniaStrings.FeatureGroupOtherRecordTypes),
			new LexiconFeatureDescriptor("posEdit",
				FwAvaloniaStrings.FeaturePosEditName, FwAvaloniaStrings.FeaturePosEditDescription,
				FwAvaloniaStrings.FeatureGroupOtherRecordTypes)
		};

		/// <summary>The bare tool-name ids, in catalog order — what <see cref="UIFrameworkRegistry"/> registers by default.</summary>
		public static readonly IReadOnlyList<string> ToolNames = BuildToolNames();

		private static IReadOnlyList<string> BuildToolNames()
		{
			var names = new string[Features.Count];
			for (var i = 0; i < Features.Count; i++)
				names[i] = Features[i].ToolName;
			return names;
		}
	}
}
