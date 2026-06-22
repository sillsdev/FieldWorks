// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Builds the product Lexical Edit region model from the typed view definition plus live LCModel
	/// values (task 4.8). This is the typed-definition-backed replacement for
	/// <see cref="LexicalEditPocMapper"/>: structure is expressed as a <see cref="ViewDefinitionModel"/>
	/// (the same IR vocabulary the XML importer produces), and this type only supplies values via
	/// <see cref="IRegionValueProvider"/>. The first-slice definition is authored here for the LexEntry
	/// identity fields; the next step is to compile it from the live layout inventory so the region scales
	/// to the full layout. Values are read on the UI thread; write-back goes through the LCModel edit
	/// session (tasks 6.x), not this builder.
	/// </summary>
	public sealed class LexicalEditRegionBuilder : IRegionValueProvider
	{
		private const string LexemeFormField = "LexemeForm";
		private const string GlossField = "Gloss";
		private const string MorphTypeField = "MorphType";

		private readonly ILexEntry _entry;

		private LexicalEditRegionBuilder(ILexEntry entry)
		{
			_entry = entry;
		}

		/// <summary>
		/// Builds a region model for the current record, or null if it is not a <see cref="ILexEntry"/>
		/// (the caller then shows an explicit unsupported state). <paramref name="cache"/> is reserved for
		/// the writing-system/font service that will replace the placeholder ws abbreviations.
		/// </summary>
		public static LexicalEditRegionModel Build(ICmObject obj, LcmCache cache)
		{
			if (!(obj is ILexEntry entry))
				return null;

			var definition = BuildFirstSliceDefinition();
			var provider = new LexicalEditRegionBuilder(entry);
			return LexicalEditRegionMapper.FromViewDefinition(definition, provider);
		}

		/// <summary>
		/// The typed view definition for the LexEntry identity first slice, expressed in the IR vocabulary
		/// with stable ids, writing-system metadata, accessibility ids, and product routing. Authored for
		/// now; replace with a live layout compile (ViewDefinitionCompiler) as the region grows.
		/// </summary>
		internal static ViewDefinitionModel BuildFirstSliceDefinition()
		{
			var roots = new List<ViewNode>
			{
				Leaf("LexEntry/identity/#0", "Lexeme Form", LexemeFormField, "multistring", "vernacular", "LexemeFormEditor"),
				Leaf("LexEntry/identity/#1", "Morph Type", MorphTypeField, "morphtypeatomicreference", null, "MorphTypeChooser"),
				Leaf("LexEntry/identity/#2", "Gloss", GlossField, "multistring", "analysis", "SenseGlossEditor")
			};

			return new ViewDefinitionModel("LexEntry", "identity", "detail", roots, Array.Empty<ViewDiagnostic>());
		}

		private static ViewNode Leaf(string stableId, string label, string field, string editor, string ws, string automationId)
			=> new ViewNode(stableId, ViewNodeKind.Field, label, null, field, editor,
				EditorClassification.Known, ws, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
				localizationKey: null, automationId: automationId, routing: SurfaceRouting.Product);

		/// <inheritdoc />
		public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
		{
			switch (fieldNode.Field)
			{
				case LexemeFormField:
					return new List<RegionWsValue> { new RegionWsValue("vern", GetLexemeFormText()) };
				case GlossField:
					return new List<RegionWsValue> { new RegionWsValue("anal", GetFirstSenseGloss()) };
				default:
					return new List<RegionWsValue>();
			}
		}

		/// <inheritdoc />
		public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode)
		{
			if (fieldNode.Field != MorphTypeField)
				return new List<RegionChoiceOption>();

			return new List<RegionChoiceOption>
			{
				new RegionChoiceOption("stem", "stem"),
				new RegionChoiceOption("root", "root"),
				new RegionChoiceOption("prefix", "prefix"),
				new RegionChoiceOption("suffix", "suffix")
			};
		}

		/// <inheritdoc />
		public string GetSelectedOptionKey(ViewNode fieldNode)
		{
			return fieldNode.Field == MorphTypeField ? GetMorphTypeKey() : null;
		}

		private string GetLexemeFormText()
		{
			var lexemeText = _entry.LexemeFormOA?.Form != null
				? _entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text
				: string.Empty;
			if (string.IsNullOrEmpty(lexemeText))
				lexemeText = _entry.CitationForm.VernacularDefaultWritingSystem.Text;
			return lexemeText ?? string.Empty;
		}

		private string GetFirstSenseGloss()
		{
			if (_entry.SensesOS.Count == 0)
				return string.Empty;
			return _entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text ?? string.Empty;
		}

		private string GetMorphTypeKey()
		{
			var type = _entry.LexemeFormOA?.MorphTypeRA;
			if (type == null)
				return "stem";
			if (type.Guid == MoMorphTypeTags.kguidMorphPrefix)
				return "prefix";
			if (type.Guid == MoMorphTypeTags.kguidMorphSuffix)
				return "suffix";
			if (type.Guid == MoMorphTypeTags.kguidMorphRoot || type.Guid == MoMorphTypeTags.kguidMorphBoundRoot)
				return "root";
			return "stem";
		}
	}
}
