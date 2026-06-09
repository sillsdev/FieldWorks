// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Classifies a legacy editor string the same way <c>SliceFactory.Create</c> does: a fixed set of
	/// known statically-resolved editors, the dynamically loaded constructs, the obsolete ones, a
	/// grouping (null) editor, and everything else as unknown. This lets the typed importer raise
	/// faithful diagnostics for dynamic/unknown/obsolete editors (tasks 3.8 and 4.4) without
	/// constructing any WinForms control.
	/// </summary>
	public static class EditorKindMap
	{
		// Mirrors the case labels in Src/Common/Controls/DetailControls/SliceFactory.cs. Comparison is
		// case-insensitive because DataTree lowercases the editor attribute before dispatch
		// (DataTree.ProcessSubpartNode: editor.ToLower()), so e.g. "MorphTypeAtomicReference" in shipped
		// parts is the known "morphtypeatomicreference" editor.
		private static readonly HashSet<string> KnownEditors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"multistring",
			"defaultvectorreference",
			"defaultvectorreferencedisabled",
			"possvectorreference",
			"semdomvectorreference",
			"string",
			"jtview",
			"summary",
			"enumcombobox",
			"referencecombobox",
			"typeaheadrefatomic",
			"msareferencecombobox",
			"lit",
			"picture",
			"image",
			"checkbox",
			"checkboxwithrefresh",
			"time",
			"int",
			"integer",
			"gendate",
			"morphtypeatomicreference",
			"atomicreferencepos",
			"possatomicreference",
			"atomicreferenceposdisabled",
			"defaultatomicreference",
			"defaultatomicreferencedisabled",
			"derivmsareference",
			"inflmsareference",
			"phoneenvreference",
			"sttext",
			"ghostvector",
			"command"
		};

		private static readonly HashSet<string> DynamicEditors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"custom",
			"customwithparams",
			"autocustom"
		};

		private static readonly HashSet<string> ObsoleteEditors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"message"
		};

		/// <summary>
		/// Classifies <paramref name="rawEditor"/>. A null/empty editor is a grouping node;
		/// dynamic/obsolete/known are matched against the legacy sets; anything else is unknown.
		/// </summary>
		public static EditorClassification Classify(string rawEditor)
		{
			if (string.IsNullOrEmpty(rawEditor))
			{
				return EditorClassification.GroupingNone;
			}

			if (DynamicEditors.Contains(rawEditor))
			{
				return EditorClassification.Dynamic;
			}

			if (ObsoleteEditors.Contains(rawEditor))
			{
				return EditorClassification.Obsolete;
			}

			return KnownEditors.Contains(rawEditor)
				? EditorClassification.Known
				: EditorClassification.Unknown;
		}
	}
}
