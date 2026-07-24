// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Shared ViewDefinitionModel fixtures for the browse-table test group (formerly reimplemented verbatim as
	/// a private TwoColumnDefinition() in each test fixture; PR #964 review §6 cleanup #4).
	/// </summary>
	internal static class ViewDefinitionTestBuilders
	{
		/// <summary>
		/// The "LexEntry" browse definition with two Field columns (Lexeme Form / vernacular, Gloss / analysis)
		/// used across the browse-table test group. <paramref name="fieldType"/> defaults to "multistring";
		/// pass "string" for fixtures exercising the single-string editor classification path.
		/// </summary>
		public static ViewDefinitionModel TwoColumnBrowseDefinition(string fieldType = "multistring") => new ViewDefinitionModel(
			"LexEntry", "browse", "browse",
			new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", fieldType,
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", fieldType,
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			},
			new List<ViewDiagnostic>());
	}
}
