// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// The renderable category of a legacy editor string -- the ONE home for the
	/// editor-string -> category knowledge that the detail composer's dispatch switch and
	/// <c>DetailModelProjector</c>'s kind classification both consume -- neither keeps a
	/// copy of its own. Consumers may still refine a category by LCModel field type
	/// (e.g. the composer's <c>CellarPropertyType</c> dispatch for <see cref="Other"/>); only the
	/// editor-string knowledge itself lives here.
	/// </summary>
	public enum DetailEditorCategory
	{
		/// <summary>A null/empty editor: a grouping node, no renderable field of its own.</summary>
		Grouping,

		/// <summary>A (multi-)writing-system text editor (<c>multistring</c>/<c>string</c>).</summary>
		Text,

		/// <summary>The morph-type chooser (<c>morphtypeatomicreference</c>) with its stem/affix guard.</summary>
		MorphTypeChooser,

		/// <summary>An atomic-reference editor that renders as a chooser row.</summary>
		AtomicReferenceChooser,

		/// <summary>The sense grammatical-info (MSA) chooser (<c>msaReferenceComboBox</c> and the
		/// deriv/infl MSA variants): a Part-of-Speech chooser whose write find-or-creates the MSA on
		/// the owning entry (legacy <c>MSAReferenceComboBoxSlice</c>).</summary>
		MsaChooser,

		/// <summary>A <c>summary</c> slice: a section header row in legacy too.</summary>
		Summary,

		/// <summary>A <c>lit</c> slice: the label IS the content.</summary>
		Literal,

		/// <summary>A <c>picture</c>/<c>image</c> slice.</summary>
		Picture,

		/// <summary>A <c>jtview</c> embedded formatted view.</summary>
		EmbeddedView,

		/// <summary>A <c>command</c> slice (button row).</summary>
		Command,

		/// <summary>
		/// An <c>enumcombobox</c> slice: legacy presents a CLOSED combo over the layout's
		/// stringList labels (<c>EnumComboSlice</c>), never free-form input.
		/// </summary>
		EnumCombo,

		/// <summary>Anything else: consumers resolve by LCModel field type (or treat as text).</summary>
		Other
	}

	/// <summary>
	/// Classifies a legacy editor string the same way <c>SliceFactory.Create</c> does: a fixed set of
	/// known statically-resolved editors, the dynamically loaded constructs, the obsolete ones, a
	/// grouping (null) editor, and everything else as unknown. This lets the typed importer raise
	/// faithful diagnostics for dynamic/unknown/obsolete editors (tasks 3.8 and 4.4) without
	/// constructing any WinForms control. Also the single home of the named editor-string
	/// constants and the <see cref="ClassifyDetailFieldKind"/> category API the detail views
	/// dispatch on.
	/// </summary>
	public static class EditorKindMap
	{
		/// <summary>The legacy <c>multistring</c> editor.</summary>
		public const string MultiStringEditor = "multistring";

		/// <summary>The legacy <c>string</c> editor.</summary>
		public const string StringEditor = "string";

		/// <summary>The legacy <c>morphtypeatomicreference</c> editor.</summary>
		public const string MorphTypeAtomicReferenceEditor = "morphtypeatomicreference";

		/// <summary>The legacy <c>summary</c> editor.</summary>
		public const string SummaryEditor = "summary";

		/// <summary>The legacy <c>lit</c> editor.</summary>
		public const string LiteralEditor = "lit";

		/// <summary>The legacy <c>picture</c> editor.</summary>
		public const string PictureEditor = "picture";

		/// <summary>The legacy <c>image</c> editor.</summary>
		public const string ImageEditor = "image";

		/// <summary>The legacy <c>jtview</c> editor.</summary>
		public const string JtViewEditor = "jtview";

		/// <summary>The legacy <c>command</c> editor.</summary>
		public const string CommandEditor = "command";

		/// <summary>The legacy <c>enumcombobox</c> editor.</summary>
		public const string EnumComboBoxEditor = "enumcombobox";

		/// <summary>The dynamically resolved per-type custom-field editor (<c>autocustom</c>).</summary>
		public const string AutoCustomEditor = "autocustom";

		// Mirrors the case labels in Src/Common/Controls/DetailControls/SliceFactory.cs. Comparison is
		// case-insensitive because DataTree lowercases the editor attribute before dispatch
		// (DataTree.ProcessSubpartNode: editor.ToLower()), so e.g. "MorphTypeAtomicReference" in shipped
		// parts is the known "morphtypeatomicreference" editor.
		private static readonly HashSet<string> KnownEditors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			MultiStringEditor,
			"defaultvectorreference",
			"defaultvectorreferencedisabled",
			"possvectorreference",
			"semdomvectorreference",
			StringEditor,
			JtViewEditor,
			SummaryEditor,
			EnumComboBoxEditor,
			"referencecombobox",
			"typeaheadrefatomic",
			"msareferencecombobox",
			LiteralEditor,
			PictureEditor,
			ImageEditor,
			"checkbox",
			"checkboxwithrefresh",
			"time",
			"int",
			"integer",
			"gendate",
			MorphTypeAtomicReferenceEditor,
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
			CommandEditor
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

		/// <summary>
		/// Maps a legacy editor string onto its renderable <see cref="DetailEditorCategory"/> --
		/// the one editor-string dispatch table the composer's field switch and the mapper's kind
		/// classification share. Case-insensitive like the legacy DataTree dispatch
		/// (<c>editor.ToLower()</c>). Editors not named here are <see cref="DetailEditorCategory.Other"/>:
		/// consumers refine those by LCModel field type (the composer's <c>WalkOtherField</c>)
		/// or render them as text (the first-slice mapper).
		/// </summary>
		public static DetailEditorCategory ClassifyDetailFieldKind(string rawEditor)
		{
			if (string.IsNullOrEmpty(rawEditor))
			{
				return DetailEditorCategory.Grouping;
			}

			switch (rawEditor.ToLowerInvariant())
			{
				case MultiStringEditor:
				case StringEditor:
					return DetailEditorCategory.Text;
				case MorphTypeAtomicReferenceEditor:
					return DetailEditorCategory.MorphTypeChooser;
				case SummaryEditor:
					return DetailEditorCategory.Summary;
				case LiteralEditor:
					return DetailEditorCategory.Literal;
				case PictureEditor:
				case ImageEditor:
					return DetailEditorCategory.Picture;
				case JtViewEditor:
					return DetailEditorCategory.EmbeddedView;
				case CommandEditor:
					return DetailEditorCategory.Command;
				case EnumComboBoxEditor:
					// A closed enum combo must never degrade to a free-form int
					// editor that can persist invalid enum values.
					return DetailEditorCategory.EnumCombo;
				case "atomicreferencepos":
				case "atomicreferenceposdisabled":
				case "possatomicreference":
				case "defaultatomicreference":
				case "defaultatomicreferencedisabled":
					return DetailEditorCategory.AtomicReferenceChooser;
				case "msareferencecombobox":
				case "derivmsareference":
				case "inflmsareference":
					return DetailEditorCategory.MsaChooser;
				default:
					return DetailEditorCategory.Other;
			}
		}
	}
}
