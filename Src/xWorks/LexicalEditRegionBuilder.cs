// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Builds the product Lexical Edit region model from the typed view definition plus live LCModel
	/// values (tasks 4.8/4.10). Structure comes from <see cref="LexicalEditFirstSlice"/>, which compiles
	/// the shipped layout inventory through <c>ViewDefinitionCompiler</c>; the authored definition
	/// remains only as an explicit, diagnosed fallback. This type supplies values via
	/// <see cref="IRegionValueProvider"/>: text from the entry, and morph-type chooser options sourced
	/// from the project's LCModel morph-type possibility list (no hardcoded option set). Values are read
	/// on the UI thread; write-back goes through the LCModel edit session (tasks 6.x), not this builder.
	/// </summary>
	public sealed class LexicalEditRegionBuilder : IRegionValueProvider
	{
		// Field names as they appear in the compiled shipped layouts (MoForm AsLexemeForm slice,
		// MoForm MorphTypeBasic slice, LexSense GlossAllA slice).
		private const string LexemeFormField = "Form";
		private const string GlossField = "Gloss";
		private const string MorphTypeField = "MorphType";

		private static readonly Lazy<ViewDefinitionModel> FirstSliceDefinition =
			new Lazy<ViewDefinitionModel>(CompileOrFallback);

		private readonly ILexEntry _entry;
		private readonly LcmCache _cache;

		private LexicalEditRegionBuilder(ILexEntry entry, LcmCache cache)
		{
			_entry = entry;
			_cache = cache;
		}

		/// <summary>
		/// Builds a region model for the current record, or null if it is not a <see cref="ILexEntry"/>
		/// (the caller then shows an explicit unsupported state).
		/// </summary>
		public static LexicalEditRegionModel Build(ICmObject obj, LcmCache cache)
		{
			if (!(obj is ILexEntry entry))
				return null;

			var provider = new LexicalEditRegionBuilder(entry, cache);
			return LexicalEditRegionMapper.FromViewDefinition(FirstSliceDefinition.Value, provider);
		}

		/// <summary>
		/// Task 4.10: compile the first-slice definition from the live shipped layout inventory. The
		/// authored definition (which carries an `authored-fallback` diagnostic) is used only when the
		/// layout directory is unavailable or a shipped layout no longer yields the expected nodes.
		/// </summary>
		private static ViewDefinitionModel CompileOrFallback()
		{
			string partsDirectory = null;
			try
			{
				partsDirectory = FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer\Configuration\Parts");
			}
			catch (ApplicationException)
			{
				// No FieldWorks code directory in this environment (bare harness); use the fallback.
			}

			return LexicalEditFirstSlice.CompileFromLayoutDirectory(partsDirectory)
				?? LexicalEditFirstSlice.AuthoredFallback();
		}

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
			var options = new List<RegionChoiceOption>();
			if (fieldNode.Field != MorphTypeField)
				return options;

			// Task 4.10: chooser options come from the project's morph-type possibility list, keyed by
			// guid, so every project-defined morph type (phrase, clitic, infix, ...) is offered instead
			// of a hardcoded subset.
			var morphTypes = _cache.LangProject.LexDbOA?.MorphTypesOA;
			if (morphTypes == null)
				return options;

			AddPossibilities(morphTypes.PossibilitiesOS, options);
			return options;
		}

		/// <inheritdoc />
		public string GetSelectedOptionKey(ViewNode fieldNode)
		{
			if (fieldNode.Field != MorphTypeField)
				return null;

			return _entry.LexemeFormOA?.MorphTypeRA?.Guid.ToString();
		}

		private static void AddPossibilities(IEnumerable<ICmPossibility> possibilities, List<RegionChoiceOption> options)
		{
			foreach (var possibility in possibilities)
			{
				var name = possibility.Name.BestAnalysisAlternative?.Text;
				if (string.IsNullOrEmpty(name))
					name = possibility.Name.BestVernacularAlternative?.Text ?? possibility.Guid.ToString();
				options.Add(new RegionChoiceOption(possibility.Guid.ToString(), name));
				AddPossibilities(possibility.SubPossibilitiesOS, options);
			}
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
	}
}
