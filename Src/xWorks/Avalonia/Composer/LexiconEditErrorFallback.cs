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
	/// values. Structure comes from <see cref="LexiconFirstSlice"/>, which compiles
	/// the shipped layout inventory through <c>ViewDefinitionCompiler</c>; the authored definition
	/// remains only as an explicit, diagnosed fallback. This type supplies values via
	/// <see cref="IRegionValueProvider"/>: text from the entry, and morph-type chooser options sourced
	/// from the project's LCModel morph-type possibility list (no hardcoded option set). Values are read
	/// on the UI thread; write-back goes through the LCModel edit session, not this builder.
	/// </summary>
	public sealed class LexiconEditErrorFallback : IRegionValueProvider
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

		private LexiconEditErrorFallback(ILexEntry entry, LcmCache cache)
		{
			_entry = entry;
			_cache = cache;
		}

		/// <summary>
		/// Builds a region model for the current record, or null if it is not a <see cref="ILexEntry"/>
		/// (the caller then shows an explicit unsupported state).
		/// </summary>
		public static RegionModel Build(ICmObject obj, LcmCache cache)
		{
			if (!(obj is ILexEntry entry))
				return null;

			var provider = new LexiconEditErrorFallback(entry, cache);
			return RegionModelProjector.FromViewDefinition(FirstSliceDefinition.Value, provider);
		}

		/// <summary>
		/// Compiles the first-slice definition from the live shipped layout inventory. The
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

			return LexiconFirstSlice.CompileFromLayoutDirectory(partsDirectory)
				?? LexiconFirstSlice.AuthoredFallback();
		}

		/// <inheritdoc />
		public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
		{
			switch (fieldNode.Field)
			{
				case LexemeFormField:
					return GetLexemeFormValues();
				case GlossField:
					return GetGlossValues();
				default:
					return new List<RegionWsValue>();
			}
		}

		// Multi-WS read path: one row per *current* writing system — the same
		// "all vernacular"/"all analysis" semantics the compiled slice definitions carry — rendered
		// with the project's per-WS default font so both surfaces show the same record consistently.
		// The per-ws row projection is the shared RegionValueFactory recipe (the
		// composer uses the same one); only the text reads live here.
		private IReadOnlyList<RegionWsValue> GetLexemeFormValues()
		{
			return RegionValueFactory.BuildMultiWsValues(
				_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems, ws =>
				{
					var text = _entry.LexemeFormOA?.Form?.get_String(ws.Handle);
					if ((text == null || text.Length == 0) && ws.Handle == _cache.DefaultVernWs)
						text = _entry.CitationForm.get_String(ws.Handle); // legacy fallback, default ws only
					return text;
				}, _cache.WritingSystemFactory);
		}

		private IReadOnlyList<RegionWsValue> GetGlossValues()
		{
			if (_entry.SensesOS.Count == 0)
				return new List<RegionWsValue>();

			var gloss = _entry.SensesOS[0].Gloss;
			return RegionValueFactory.BuildMultiWsValues(
				_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems,
				ws => gloss.get_String(ws.Handle), _cache.WritingSystemFactory);
		}

		/// <inheritdoc />
		public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode)
		{
			if (fieldNode.Field != MorphTypeField)
				return new List<RegionChoiceOption>();

			// Chooser options come from the project's morph-type possibility list, keyed by
			// guid, so every project-defined morph type (phrase, clitic, infix, ...) is offered instead
			// of a hardcoded subset.
			var morphTypes = _cache.LangProject.LexDbOA?.MorphTypesOA;
			if (morphTypes == null)
				return new List<RegionChoiceOption>();

			// The shared flattener (document order, hierarchy as Depth, and the
			// composer's name-fallback rule — an analysis→vernacular fallback is
			// subsumed by ShortName's own legacy resolution; see RegionValueFactory).
			return RegionValueFactory.BuildPossibilityOptions(morphTypes, flat: false);
		}

		/// <inheritdoc />
		public string GetSelectedOptionKey(ViewNode fieldNode)
		{
			if (fieldNode.Field != MorphTypeField)
				return null;

			return _entry.LexemeFormOA?.MorphTypeRA?.Guid.ToString();
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
