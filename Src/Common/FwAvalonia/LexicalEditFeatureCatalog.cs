// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Display metadata (name, description, group) for one tool surface in
	/// <see cref="LexicalEditFeatureCatalog"/>. Purely descriptive — <see cref="ToolName"/> is the same
	/// id <see cref="LexicalEditSurfaceRegistry"/> and <see cref="LexicalEditSurfaceResolver"/> key on.
	/// </summary>
	public sealed class LexicalEditFeatureDescriptor
	{
		public LexicalEditFeatureDescriptor(string toolName, string displayName, string description, string groupName)
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
	/// The single source of truth for which lexical-edit tool surfaces exist and how to describe them to
	/// a user (the "Manage Individual Features" dialog's checkbox list; PR #964 review follow-up). This is
	/// also what <see cref="LexicalEditSurfaceRegistry.DefaultSupportedTools"/> is built from, so the
	/// registry and the user-facing catalog can never drift out of sync — there is exactly one list of
	/// "tools that ship with a working Avalonia surface today."
	/// </summary>
	public static class LexicalEditFeatureCatalog
	{
		public static readonly IReadOnlyList<LexicalEditFeatureDescriptor> Features = new[]
		{
			new LexicalEditFeatureDescriptor("lexiconEdit",
				FwAvaloniaStrings.FeatureLexiconEditName, FwAvaloniaStrings.FeatureLexiconEditDescription,
				FwAvaloniaStrings.FeatureGroupLexicalEntryDialogs),
			new LexicalEditFeatureDescriptor("lexiconEditPopup",
				FwAvaloniaStrings.FeatureLexiconEditPopupName, FwAvaloniaStrings.FeatureLexiconEditPopupDescription,
				FwAvaloniaStrings.FeatureGroupLexicalEntryDialogs),
			new LexicalEditFeatureDescriptor("notebookEdit",
				FwAvaloniaStrings.FeatureNotebookEditName, FwAvaloniaStrings.FeatureNotebookEditDescription,
				FwAvaloniaStrings.FeatureGroupOtherRecordTypes),
			new LexicalEditFeatureDescriptor("posEdit",
				FwAvaloniaStrings.FeaturePosEditName, FwAvaloniaStrings.FeaturePosEditDescription,
				FwAvaloniaStrings.FeatureGroupOtherRecordTypes),
			new LexicalEditFeatureDescriptor("Analyses",
				FwAvaloniaStrings.FeatureAnalysesName, FwAvaloniaStrings.FeatureAnalysesDescription,
				FwAvaloniaStrings.FeatureGroupOtherRecordTypes)
		};

		/// <summary>The bare tool-name ids, in catalog order — what <see cref="LexicalEditSurfaceRegistry"/> registers by default.</summary>
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
