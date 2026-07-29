// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia Insert Entry dialog — the replacement for the legacy
	/// <see cref="InsertEntryDlg"/> in New-UI mode. It is a concrete
	/// <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>: the Avalonia layer (FwAvaloniaDialogs)
	/// stays LCModel-free by exchanging an <see cref="InsertEntryDialogInput"/> (lexeme-form / gloss fields built
	/// for the cache's current writing systems, morph types as guid-keyed <see cref="RegionChoiceOption"/>s, and a
	/// plain <see cref="InsertEntryDialogInput.DeriveMorphType"/> delegate) and an <see cref="InsertEntryPayload"/>
	/// (per-WS form + gloss strings + the chosen morph-type key). This launcher builds that state from the live
	/// cache and, on OK, creates the <c>ILexEntry</c> in ONE undoable step.
	///
	/// Layering mirrors <see cref="LcmChooserDialogLauncher"/>/AvaloniaOptionsDialogLauncher exactly: BuildState /
	/// Apply are internal so the full state mapping + create are unit-testable against a real cache (via
	/// InternalsVisibleTo) without running the modal.
	/// </summary>
	public sealed class LcmInsertEntryDialogLauncher
		: AvaloniaDialogLauncher<InsertEntryDialogInput, InsertEntryDialogViewModel, InsertEntryPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ITsString _tssForm;
		private InsertEntryDialogViewModel _viewModel;
		// The WinForms host the Insert Entry modal is owned by — captured so the nested create-POS modal (raised from
		// the MSA box's "Create a new Part of Speech..." affordance) can open over it.
		private IWin32Window _owner;

		private LcmInsertEntryDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ITsString tssForm)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_tssForm = tssForm;
		}

		/// <summary>
		/// Shows the Insert Entry dialog modally over <paramref name="owner"/> and, on OK, creates the new
		/// <c>ILexEntry</c> in one undoable step. Returns the created entry and whether it was newly created
		/// (mirrors the legacy <c>InsertEntryDlg.GetDialogInfo</c> out-params); a cancelled dialog returns
		/// <c>(null, false)</c>.
		/// </summary>
		/// <param name="cache">The live LCModel cache.</param>
		/// <param name="mediator">The XCore mediator.</param>
		/// <param name="propertyTable">The XCore property table.</param>
		/// <param name="owner">The WinForms host the modal is owned by.</param>
		/// <param name="tssForm">An optional initial lexeme form (e.g. the word the user selected); null/empty starts empty.</param>
		/// <param name="helpProvider">The help provider.</param>
		public static (ILexEntry entry, bool newlyCreated) Show(LcmCache cache, Mediator mediator,
			PropertyTable propertyTable, IWin32Window owner, ITsString tssForm = null,
			IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var launcher = new LcmInsertEntryDialogLauncher(cache, mediator, propertyTable, helpProvider, tssForm);
			launcher._owner = owner;
			var outcome = launcher.Run(owner);
			if (!outcome.Accepted || outcome.Payload == null)
				return (null, false);
			// newlyCreated is false for the "Go to similar entry" outcome (an existing entry was chosen), true for a
			// Create — mirroring the legacy InsertEntryDlg.GetDialogInfo out-params.
			return (launcher.CreatedEntry, launcher.WasNewlyCreated);
		}

		/// <summary>
		/// The entry the dialog resolved on OK: either the newly created entry (Create path) or the EXISTING entry
		/// the user picked from the matching-entries pane (the "Go to similar entry" path). Null when cancelled or
		/// nothing was resolved. <see cref="WasNewlyCreated"/> distinguishes the two.
		/// </summary>
		public ILexEntry CreatedEntry { get; private set; }

		/// <summary>
		/// True when <see cref="CreatedEntry"/> was newly created (Create path); false when it is an existing entry
		/// the user chose from the matching-entries pane (the legacy m_fNewlyCreated out-param). Defaults to false.
		/// </summary>
		public bool WasNewlyCreated { get; private set; }

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.InsertEntryTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 420;
		// Tall enough for the three fields + morph-type picker AND the duplicate-detection "matching entries" pane
		// below them — the legacy InsertEntryDlg is similarly tall to fit its similar-entries browser.
		protected override int DialogHeight => 460;

		/// <summary>
		/// Reads the remembered client size from the SAME registry location + value names the legacy InsertEntryDlg
		/// persisted (<c>FieldWorksRegistryKey\LingCmnDlgs</c> InsertWidth/InsertHeight — :1603-1618), so a
		/// user's remembered size carries across the New/Legacy dialogs. Null (no stored value) uses the defaults.
		/// </summary>
		protected override System.Drawing.Size? GetRememberedSize()
		{
			try
			{
				using (var regKey = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("LingCmnDlgs"))
				{
					var w = regKey?.GetValue("InsertWidth") as int?;
					var h = regKey?.GetValue("InsertHeight") as int?;
					if (w.HasValue && h.HasValue && w.Value > 0 && h.Value > 0)
						return new System.Drawing.Size(w.Value, h.Value);
				}
			}
			catch
			{
				// Size persistence must never take down the dialog; fall back to the defaults.
			}
			return null;
		}

		/// <summary>Records the final client size to the legacy registry location (parity with InsertEntryDlg_Closed).</summary>
		protected override void OnRememberedSizeChanged(System.Drawing.Size size)
		{
			try
			{
				using (var regKey = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("LingCmnDlgs"))
				{
					regKey?.SetValue("InsertWidth", size.Width);
					regKey?.SetValue("InsertHeight", size.Height);
				}
			}
			catch
			{
				// Best-effort persistence; ignore registry failures.
			}
		}

		protected override InsertEntryDialogInput BuildState() =>
			BuildInput(_cache, _tssForm, _mediator, _propertyTable);

		/// <summary>
		/// Builds the LCModel-free <see cref="InsertEntryDialogInput"/> from the live cache: a per-vernacular-WS
		/// lexeme-form field (seeded from <paramref name="tssForm"/> when it is a vernacular string), a
		/// per-analysis-WS gloss field, the morph-type options, the default (stem) morph-type selection, and the
		/// live affix-marker → morph-type derivation. Internal so the full state mapping is unit-testable against a
		/// real cache without running the modal (mirrors LcmChooserDialogLauncher.BuildInput).
		/// </summary>
		internal static InsertEntryDialogInput BuildInput(LcmCache cache, ITsString tssForm,
			Mediator mediator = null, PropertyTable propertyTable = null)
		{
			var wsContainer = cache.ServiceLocator.WritingSystems;

			// The optional initial form routes by writing system, mirroring the legacy SetDlgInfo (:848-871): a
			// VERNACULAR initial string seeds the lexeme form and shifts focus to the gloss (the form is done); an
			// ANALYSIS initial string seeds the gloss and keeps focus on the (empty) lexeme form. A string in neither
			// current set seeds nothing (focus stays on the lexeme form).
			string initialForm = null;
			string initialGloss = null;
			var initialFocus = InsertEntryInitialFocus.LexemeForm;
			if (tssForm != null && tssForm.Length > 0)
			{
				var wsForm = TsStringUtils.GetWsAtOffset(tssForm, 0);
				if (wsContainer.CurrentVernacularWritingSystems.Any(ws => ws.Handle == wsForm))
				{
					initialForm = tssForm.Text;
					initialFocus = InsertEntryInitialFocus.Gloss;
				}
				else if (wsContainer.CurrentAnalysisWritingSystems.Any(ws => ws.Handle == wsForm))
				{
					initialGloss = tssForm.Text;
					initialFocus = InsertEntryInitialFocus.LexemeForm;
				}
			}

			var lexemeForm = BuildTextField("LexemeForm", "InsertEntry.LexemeForm",
				wsContainer.CurrentVernacularWritingSystems, initialForm,
				FwAvaloniaDialogsStrings.InsertEntryLexemeFormLabel);
			var gloss = BuildTextField("Gloss", "InsertEntry.Gloss",
				wsContainer.CurrentAnalysisWritingSystems, initialGloss,
				FwAvaloniaDialogsStrings.InsertEntryGlossLabel);

			return new InsertEntryDialogInput
			{
				LexemeForm = lexemeForm,
				Gloss = gloss,
				InitialFocus = initialFocus,
				MorphTypes = BuildMorphTypeOptions(cache),
				InitialMorphTypeKey = MoMorphTypeTags.kguidMorphStem.ToString(),
				DeriveMorphType = form => DeriveMorphType(cache, form),
				// The LCModel-aware OK-time morphology validation (CheckMorphType + CircumfixProblem + invalid-form
				// parse), surfaced inline + gating OK.
				ValidateMorphology = (forms, morphTypeKey) => ValidateMorphology(cache, forms, morphTypeKey),
				// Re-mark the lexeme form with the chosen type's affix markers on an explicit morph-type pick
				// (FormWithMarkers parity).
				ApplyMorphTypeMarkers = (morphTypeKey, form) => ApplyMorphTypeMarkers(cache, morphTypeKey, form),
				SearchMatches = BuildMatchSearch(cache, mediator, propertyTable),
				// Grammatical-info (MSA) section: the project POS hierarchy + the morph-type → MsaType map +
				// the per-POS slot provider, so the LCModel-free FwMsaGroupBox can drive its layout live.
				PosNodes = BuildPosNodes(cache),
				MorphTypeToMsaType = BuildMorphTypeToMsaTypeMap(cache),
				InitialMsaType = MorphTypeToMsaType(MoMorphTypeTags.kguidMorphStem.ToString()),
				InitialMainPosId = null,
				SlotsForPos = posId => BuildSlots(cache, posId, MoMorphTypeTags.kguidMorphStem.ToString()),
				// Inflection-class picker: the selected main POS's classes, re-fed when the main POS changes.
				InflectionClassesForPos = posId => BuildInflectionClasses(cache, posId),
				InitialInflectionClassId = null,
				// Inflection-feature editor: the selected main POS's inflectable-feature system, re-fed
				// when the main POS changes (infl/deriv). No initial features on the create path.
				InflectionFeaturesForPos = posId => BuildInflectionFeatures(cache, posId),
				InitialInflectionFeatures = null,
				// Complex Form Type picker (WinForms m_cbComplexFormType parity, LT-21666): the project's complex-form
				// types + the morph-type → gating map (the data lift of EnableComplexFormTypeCombo). The dialog opens at
				// "<Not Applicable>" (the legacy SelectedIndex 0), so no initial complex-form type.
				ComplexFormTypes = BuildComplexFormTypeOptions(cache),
				InitialComplexFormTypeKey = null,
				ComplexFormGatingByMorphType = BuildComplexFormGatingMap(cache),
				Prompt = null,
				// The Help button opens the same topic the legacy dialog uses (InsertEntryDlg s_helpTopic :81).
				HelpTopic = "khtpInsertEntry"
			};
		}

		/// <summary>
		/// Builds the project's parts-of-speech hierarchy as a flat, document-order, depth-tagged
		/// <see cref="FwPosNode"/> list from <c>cache.LangProject.PartsOfSpeechOA</c> (mirroring how
		/// <c>LcmChooserDialogLauncher.BuildCandidates</c> folds a possibility list). The id is the POS guid string
		/// (round-tripped back through the repository on commit); the display name uses the best-analysis fallback and
		/// the abbreviation is carried for the row. Internal so the POS feed is unit-testable against a real cache.
		/// </summary>
		internal static IReadOnlyList<FwPosNode> BuildPosNodes(LcmCache cache)
		{
			var nodes = new List<FwPosNode>();
			void Add(IPartOfSpeech pos, int depth)
			{
				nodes.Add(new FwPosNode(pos.Guid.ToString(), PosName(pos), depth,
					pos.Abbreviation?.BestAnalysisAlternative?.Text));
				foreach (var sub in pos.SubPossibilitiesOS.OfType<IPartOfSpeech>())
					Add(sub, depth + 1);
			}

			foreach (var pos in cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.OfType<IPartOfSpeech>())
				Add(pos, 0);
			return nodes;
		}

		private static string PosName(IPartOfSpeech pos)
			=> pos.Name.BestAnalysisAlternative?.Text ?? pos.ShortName ?? pos.Guid.ToString();

		/// <summary>
		/// Builds the morph-type guid string → <see cref="FwMsaType"/> map — the data the kit uses to drive the MSA
		/// box's layout live without LCModel. It mirrors the WinForms <c>MSAGroupBox.MorphTypePreference</c> switch:
		/// stem/bound-stem/phrase → Stem; clitic/root family → Root; the affix family → Unclassified (the box then
		/// lets the user refine to Inflectional/Derivational via the affix-type combo). Built over the project's
		/// actual morph types so every option in the picker has a mapping. Internal for unit testing.
		/// </summary>
		internal static IReadOnlyDictionary<string, FwMsaType> BuildMorphTypeToMsaTypeMap(LcmCache cache)
		{
			var map = new Dictionary<string, FwMsaType>(StringComparer.Ordinal);
			foreach (var type in cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
				.OfType<IMoMorphType>())
			{
				map[type.Guid.ToString()] = MorphTypeToMsaType(type.Guid.ToString());
			}
			return map;
		}

		/// <summary>
		/// The morph-type-guid → <see cref="FwMsaType"/> rule (the lift of MSAGroupBox.MorphTypePreference's switch),
		/// exposed so the sibling MSA-section launchers (Add New Sense, MSA Creator) seed the box's initial class from
		/// an entry's morph type the same way. Internal for reuse + unit testing.
		/// </summary>
		internal static FwMsaType MorphTypeGuidToMsaType(string morphTypeGuid) => MorphTypeToMsaType(morphTypeGuid);

		// The morph-type-guid → FwMsaType rule, lifted verbatim from MSAGroupBox.MorphTypePreference's switch.
		private static FwMsaType MorphTypeToMsaType(string morphTypeGuid)
		{
			switch (morphTypeGuid)
			{
				case MoMorphTypeTags.kMorphStem:
				case MoMorphTypeTags.kMorphBoundStem:
				case MoMorphTypeTags.kMorphPhrase:
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
					return FwMsaType.Stem;
				case MoMorphTypeTags.kMorphProclitic:
				case MoMorphTypeTags.kMorphClitic:
				case MoMorphTypeTags.kMorphEnclitic:
				case MoMorphTypeTags.kMorphParticle:
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphBoundRoot:
					return FwMsaType.Root;
				default:
					// The affix family (prefix/suffix/infix/...): the box opens Unclassified, then the user refines.
					return FwMsaType.Unclassified;
			}
		}

		/// <summary>
		/// Builds the inflectional-affix slot options for a main POS (guid string), filtered by the morph type's
		/// prefixal/suffixal nature — the lift of <c>MSAGroupBox.GetSlots</c>/<c>ResetSlotCombo</c>. A prefixal-and-
		/// suffixal (or unknown) morph type yields every affix slot; otherwise the matching subset. Each slot's id is
		/// its guid string (round-tripped back on commit). Internal so the slot feed is unit-testable.
		/// </summary>
		internal static IReadOnlyList<FwInflectionSlot> BuildSlots(LcmCache cache, string posId, string morphTypeGuid)
		{
			if (string.IsNullOrEmpty(posId) || !Guid.TryParse(posId, out var posGuid))
				return Array.Empty<FwInflectionSlot>();
			IPartOfSpeech pos;
			try
			{
				pos = cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(posGuid);
			}
			catch
			{
				return Array.Empty<FwInflectionSlot>();
			}

			IEnumerable<IMoInflAffixSlot> slots;
			IMoMorphType morphType = null;
			if (!string.IsNullOrEmpty(morphTypeGuid) && Guid.TryParse(morphTypeGuid, out var mtGuid))
			{
				try { morphType = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(mtGuid); }
				catch { morphType = null; }
			}

			if (morphType == null)
			{
				slots = pos.AllAffixSlots;
			}
			else
			{
				var isPrefixal = MorphServices.IsPrefixishType(cache, morphType.Hvo);
				var isSuffixal = MorphServices.IsSuffixishType(cache, morphType.Hvo);
				slots = (isPrefixal && isSuffixal)
					? pos.AllAffixSlots
					: DomainObjectServices.GetSomeSlots(cache, pos.AllAffixSlots, isPrefixal);
			}

			return slots
				.Select(s => new FwInflectionSlot(s.Guid.ToString(), s.Name.BestAnalysisAlternative?.Text ?? s.ShortName))
				.Where(s => !string.IsNullOrEmpty(s.Name))
				.ToList();
		}

		/// <summary>
		/// Builds the inflection-class options for a main POS (guid string) — the lift of the WinForms
		/// <c>InflectionClassPopupTreeManager</c> tree, but scoped to the SINGLE selected POS (the box's inflection
		/// class is the selected main POS's class). Walks <c>IPartOfSpeech.InflectionClassesOC</c> and the nested
		/// <c>IMoInflClass.SubclassesOC</c> in document order, tagging each with its nesting depth so the picker can
		/// indent subclasses. Each class's id is its guid string (round-tripped back on commit). An empty/unknown POS
		/// yields no classes (the box still shows the "&lt;None&gt;" row). Internal so the feed is unit-testable.
		/// </summary>
		internal static IReadOnlyList<FwInflectionClass> BuildInflectionClasses(LcmCache cache, string posId)
		{
			if (string.IsNullOrEmpty(posId) || !Guid.TryParse(posId, out var posGuid))
				return Array.Empty<FwInflectionClass>();
			IPartOfSpeech pos;
			try
			{
				pos = cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(posGuid);
			}
			catch
			{
				return Array.Empty<FwInflectionClass>();
			}

			var result = new List<FwInflectionClass>();
			void Add(IMoInflClass cls, int depth)
			{
				var name = cls.Name.BestAnalysisAlternative?.Text ?? cls.ShortName;
				if (!string.IsNullOrEmpty(name))
					result.Add(new FwInflectionClass(cls.Guid.ToString(), name, depth));
				foreach (var sub in cls.SubclassesOC)
					Add(sub, depth + 1);
			}

			foreach (var cls in pos.InflectionClassesOC)
				Add(cls, 0);
			return result;
		}

		/// <summary>
		/// Builds the inflection-feature SYSTEM for a main POS (guid string) as a flat, document-order, depth-tagged
		/// <see cref="FwFeatureNode"/> list — the lift of
		/// <c>MsaInflectionFeatureListDlg.PopulateTreeFromPos</c> via <see cref="FwFeatureStructureAdapter.BuildNodes"/>:
		/// the POS's (and its parent POSes') <c>InflectableFeatsRC</c>, closed features expanded to their values, complex
		/// features expanded to their nested features. An empty/unknown POS yields no nodes. Shared by all three MSA-section
		/// launchers + unit-testable against a real cache.
		/// </summary>
		internal static IReadOnlyList<FwFeatureNode> BuildInflectionFeatures(LcmCache cache, string posId)
		{
			var pos = ResolvePos(cache, posId);
			return FwFeatureStructureAdapter.BuildNodes(pos);
		}

		/// <summary>
		/// Rebuilds the inflection <c>IFsFeatStruc</c> on a sense's morpheme MSA from the chosen inflection-feature
		/// assignment set — the create-side parity of <c>MsaInflectionFeatureListDlg_Closing</c>.
		/// Scoped to <c>IMoInflAffMsa.InflFeatsOA</c> / <c>IMoDerivAffMsa.FromMsFeaturesOA</c> (the surface the box edits);
		/// other MSA flavours are a no-op. Resolves the feature-system nodes from the MSA's own POS (deterministic, so the
		/// commit need not carry the live node list), then writes/clears the FS in the caller's UOW. Internal static so
		/// both create paths share it and it is unit-testable inside a UOW.
		/// </summary>
		internal static void ApplyInflectionFeatures(LcmCache cache, ILexSense sense, FwSandboxMsa chosen)
		{
			if (sense == null || chosen == null)
				return;
			ApplyInflectionFeatures(cache, sense.MorphoSyntaxAnalysisRA, chosen);
		}

		/// <summary>
		/// Rebuilds the inflection <c>IFsFeatStruc</c> on a morpheme MSA from the chosen assignment set (see the sense
		/// overload). Used directly by the MSA Creator caller path. Internal static for reuse + unit testing.
		/// </summary>
		internal static void ApplyInflectionFeatures(LcmCache cache, IMoMorphSynAnalysis msa, FwSandboxMsa chosen)
		{
			if (cache == null || msa == null || chosen == null)
				return;
			var pos = FwFeatureStructureAdapter.GetInflectionFeaturePos(msa);
			var nodes = FwFeatureStructureAdapter.BuildNodes(pos);
			FwFeatureStructureAdapter.ApplyInflectionFeatures(cache, msa, nodes, chosen.InflectionFeatures);
		}

		/// <summary>
		/// Builds the duplicate-detection ("matching entries") search delegate the dialog drives as the user types
		/// the lexeme form or gloss — the lift of <c>InsertEntryDlg.UpdateMatches</c> + <c>GetFields</c>. It uses the
		/// SAME engine the legacy dialog uses (<see cref="InsertEntrySearchEngine"/> over the live entry repository),
		/// keyed on the legacy field set: the vernacular citation/lexeme/alternate FORMS (the "do not create a second
		/// 'casa'" case) AND the analysis GLOSS (legacy <c>:1030-1031</c> — typing a gloss surfaces same-gloss entries).
		/// A create flow has no "current" entry to exclude, so every match is a reuse candidate. Each match maps to a
		/// lightweight headword + gloss row. Internal so the match semantics are unit-testable against a real cache.
		/// </summary>
		internal static Func<string, string, IReadOnlyList<EntryGoSearchResult>> BuildMatchSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable)
		{
			var engine = GetMatchSearchEngine(cache, mediator, propertyTable);
			var vernWs = cache.DefaultVernWs;
			var analWs = cache.DefaultAnalWs;
			var repo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			return (form, gloss) =>
			{
				form = form ?? string.Empty;
				gloss = gloss ?? string.Empty;
				if (string.IsNullOrEmpty(form) && string.IsNullOrEmpty(gloss))
					return Array.Empty<EntryGoSearchResult>();

				// The legacy GetFields set: the vernacular form fields keyed on the typed form, plus the gloss field
				// keyed on the typed gloss (added only when a gloss is present, as the legacy GetFields does).
				var fields = new List<SearchField>();
				var formKey = TsStringUtils.MakeString(form, vernWs);
				fields.Add(new SearchField(LexEntryTags.kflidCitationForm, formKey));
				fields.Add(new SearchField(LexEntryTags.kflidLexemeForm, formKey));
				fields.Add(new SearchField(LexEntryTags.kflidAlternateForms, formKey));
				if (!string.IsNullOrEmpty(gloss))
					fields.Add(new SearchField(LexSenseTags.kflidGloss, TsStringUtils.MakeString(gloss, analWs)));

				var results = new List<EntryGoSearchResult>();
				foreach (var hvo in engine.Search(fields))
				{
					if (!repo.TryGetObject(hvo, out var entry))
						continue;
					results.Add(new EntryGoSearchResult(
						hvo.ToString(System.Globalization.CultureInfo.InvariantCulture),
						EntryGoLauncherShared.HeadwordText(entry))
					{
						LexemeForm = EntryGoLauncherShared.LexemeFormText(entry),
						Gloss = EntryGoLauncherShared.GlossesText(entry)
					});
				}
				return results.OrderBy(r => r.Text, StringComparer.CurrentCulture).ToList();
			};
		}

		// Gets (creating + caching on the property table, like the legacy dialog) the InsertEntrySearchEngine — the
		// same engine the legacy InsertEntryDlg uses, which searches the vernacular forms AND the analysis gloss. When
		// no property table is supplied (tests) a fresh engine is built.
		private static InsertEntrySearchEngine GetMatchSearchEngine(LcmCache cache, Mediator mediator,
			PropertyTable propertyTable)
		{
			if (propertyTable == null)
				return new InsertEntrySearchEngine(cache);
			return (InsertEntrySearchEngine)SearchEngine.Get(mediator, propertyTable,
				"AvaloniaInsertEntryMatchSearchEngine", () => new InsertEntrySearchEngine(cache));
		}

		/// <summary>
		/// Builds a per-writing-system text field: one <see cref="RegionWsValue"/> row per writing system, seeded
		/// empty unless <paramref name="initialForm"/> is supplied (then the first/default WS row carries it). The
		/// row's WsTag (the IETF tag) is the key the in-memory edit context stages each alternative under — and the
		/// key Apply reads back to build the per-WS LexEntryComponents alternatives.
		/// </summary>
		internal static LexicalEditRegionField BuildTextField(string field, string automationId,
			IEnumerable<CoreWritingSystemDefinition> writingSystems, string initialForm, string label)
		{
			var values = new List<RegionWsValue>();
			var seeded = false;
			foreach (var ws in writingSystems)
			{
				var seedThis = !seeded && !string.IsNullOrEmpty(initialForm);
				values.Add(new RegionWsValue(ws.Abbreviation, seedThis ? initialForm : string.Empty,
					ws.DefaultFontName, 0, ws.RightToLeftScript, ws.Id));
				if (seedThis)
					seeded = true;
			}

			return new LexicalEditRegionField(field, label, field, null, RegionFieldKind.Text,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification), automationId, field,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting), values,
				new List<RegionChoiceOption>(), selectedOptionKey: null, isEditable: true);
		}

		/// <summary>
		/// Builds the morph-type options (key = morph-type guid string, name = best-analysis display name) from
		/// the project's morph types, in sorted display order — the legacy "Any" morph-type filter (every type).
		/// </summary>
		internal static IReadOnlyList<RegionChoiceOption> BuildMorphTypeOptions(LcmCache cache)
		{
			var types = cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
				.Cast<IMoMorphType>()
				.OrderBy(MorphTypeName, StringComparer.CurrentCulture)
				.ToList();
			return types
				.Select(t => new RegionChoiceOption(t.Guid.ToString(), MorphTypeName(t)))
				.ToList();
		}

		private static string MorphTypeName(IMoMorphType type)
			=> type.Name.BestAnalysisAlternative?.Text ?? type.ShortName ?? type.Guid.ToString();

		/// <summary>
		/// Builds the complex-form type options (key = complex-entry-type guid string, name = best-analysis display
		/// name) from <c>LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities</c>, in sorted display order — the
		/// lift of the WinForms <c>m_cbComplexFormType</c> fill (which sorts the possibilities and prepends the
		/// "&lt;Not Applicable&gt;" item; the kit prepends that row itself). Internal so the feed is unit-testable.
		/// </summary>
		internal static IReadOnlyList<RegionChoiceOption> BuildComplexFormTypeOptions(LcmCache cache)
		{
			return cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities
				.OfType<ILexEntryType>()
				.OrderBy(ComplexFormTypeName, StringComparer.CurrentCulture)
				.Select(t => new RegionChoiceOption(t.Guid.ToString(), ComplexFormTypeName(t)))
				.ToList();
		}

		private static string ComplexFormTypeName(ILexEntryType type)
			=> type.Name.BestAnalysisAlternative?.Text ?? type.ShortName ?? type.Guid.ToString();

		/// <summary>
		/// Builds the morph-type-guid → <see cref="ComplexFormGating"/> map — the data lift of the WinForms
		/// <c>InsertEntryDlg.EnableComplexFormTypeCombo</c> switch (LT-21666). Bound-root/root disable the picker and
		/// force "&lt;Not Applicable&gt;"; phrase/discontiguous-phrase enable it but keep the selection; every other
		/// morph type enables it and resets to "&lt;Not Applicable&gt;". Built over the project's actual morph types
		/// so every option in the morph-type picker has a mapping. Internal for unit testing.
		/// </summary>
		internal static IReadOnlyDictionary<string, ComplexFormGating> BuildComplexFormGatingMap(LcmCache cache)
		{
			var map = new Dictionary<string, ComplexFormGating>(StringComparer.Ordinal);
			foreach (var type in cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
				.OfType<IMoMorphType>())
			{
				map[type.Guid.ToString()] = ComplexFormGatingForMorphType(type.Guid.ToString());
			}
			return map;
		}

		// The morph-type-guid → ComplexFormGating rule, lifted verbatim from EnableComplexFormTypeCombo's switch.
		private static ComplexFormGating ComplexFormGatingForMorphType(string morphTypeGuid)
		{
			switch (morphTypeGuid)
			{
				case MoMorphTypeTags.kMorphBoundRoot:
				case MoMorphTypeTags.kMorphRoot:
					return ComplexFormGating.DisabledNotApplicable;
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
				case MoMorphTypeTags.kMorphPhrase:
					return ComplexFormGating.EnabledKeepSelection;
				default:
					return ComplexFormGating.EnabledNotApplicable;
			}
		}

		/// <summary>
		/// The live affix-marker → morph-type derivation, lifted from the legacy
		/// <c>InsertEntryDlg.tbLexicalForm_TextChanged</c>: given the current best lexeme form it returns the
		/// derived morph-type guid string + the marker-adjusted form. Empty form keeps the default stem; a leading
		/// affix marker (GetTypeIfMatchesPrefix) derives prefix/suffix/etc and may adjust the form; a single
		/// character is a stem; longer forms run FindMorphType. A null typeKey means "leave the selection". Static
		/// so it is unit-testable against a real cache.
		/// </summary>
		internal static (string typeKey, string adjustedForm) DeriveMorphType(LcmCache cache, string form)
		{
			form = form?.Trim() ?? string.Empty;
			if (form.Length == 0)
				return (MoMorphTypeTags.kguidMorphStem.ToString(), form);

			IMoMorphType mmt;
			var adjusted = form;
			var prefixMatch = MorphServices.GetTypeIfMatchesPrefix(cache, form, out var sAdjusted);
			if (prefixMatch != null)
			{
				mmt = prefixMatch;
				if (form != sAdjusted)
					adjusted = sAdjusted;
			}
			else if (form.Length == 1)
			{
				mmt = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphStem);
			}
			else
			{
				try
				{
					var newForm = form;
					mmt = MorphServices.FindMorphType(cache, ref newForm, out _);
				}
				catch
				{
					// An invalid form (the legacy ksInvalidForm path) leaves the selection unchanged.
					return (null, form);
				}
			}

			return (mmt?.Guid.ToString(), adjusted);
		}

		/// <summary>
		/// The LCModel-aware OK-time morphology validation — the lift of <c>InsertEntryDlg</c>'s
		/// <c>CheckMorphType</c> (:1439) + <c>CircumfixProblem</c> (:1494) + the <c>ksInvalidForm</c> parse guard
		/// (:1681). Given the staged per-writing-system lexeme forms (keyed by WS tag) and the chosen morph-type key it
		/// returns which morphology problem the form/type combination has (if any). The empty-form case is handled by
		/// the dialog's own empty-form gate, so an empty best form returns <see cref="InsertEntryMorphValidation.Valid"/>
		/// here. Static so it is unit-testable against a real cache.
		/// </summary>
		internal static InsertEntryMorphValidation ValidateMorphology(LcmCache cache,
			IReadOnlyDictionary<string, string> formsByWs, string morphTypeKey)
		{
			var best = BestFormOf(formsByWs);
			if (string.IsNullOrEmpty(best))
				return InsertEntryMorphValidation.Valid; // empty handled by the empty-form gate

			var chosen = ResolveMorphType(cache, morphTypeKey);

			// The ksInvalidForm parse guard: FindMorphType throws on a malformed form (e.g. mismatched circumfix
			// markers). FindMorphType strips the markers off the ref form, which CheckMorphType then reuses.
			var strippedForm = best;
			IMoMorphType found;
			try
			{
				found = MorphServices.FindMorphType(cache, ref strippedForm, out _);
			}
			catch
			{
				return InsertEntryMorphValidation.InvalidForm;
			}

			if (!CheckMorphType(chosen, found, best, strippedForm))
				return InsertEntryMorphValidation.InvalidLexForm;

			if (CircumfixProblem(cache, chosen, formsByWs))
				return InsertEntryMorphValidation.IncompleteCircumfix;

			return InsertEntryMorphValidation.Valid;
		}

		// The lift of InsertEntryDlg.CheckMorphType (:1439-1488): does the morph type FindMorphType deduced from the
		// typed markers agree with the chosen morph type? The interfix/bound-root special cases mirror the legacy
		// switch; the fallback tolerates a user who typed markers that make two distinct types look identical (LT-12378).
		private static bool CheckMorphType(IMoMorphType chosen, IMoMorphType deduced, string originalForm,
			string strippedForm)
		{
			if (chosen == null || deduced == null)
				return true; // nothing resolvable to disagree about; do not block OK
			bool result;
			switch (chosen.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphCircumfix:
				case MoMorphTypeTags.kMorphPhrase:
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
				case MoMorphTypeTags.kMorphStem:
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphParticle:
				case MoMorphTypeTags.kMorphClitic:
					result = deduced.Guid == MoMorphTypeTags.kguidMorphStem
						|| deduced.Guid == MoMorphTypeTags.kguidMorphPhrase;
					break;
				case MoMorphTypeTags.kMorphBoundRoot:
					result = deduced.Guid == MoMorphTypeTags.kguidMorphBoundStem;
					break;
				case MoMorphTypeTags.kMorphSuffixingInterfix:
					result = deduced.Guid == MoMorphTypeTags.kguidMorphSuffix;
					break;
				case MoMorphTypeTags.kMorphPrefixingInterfix:
					result = deduced.Guid == MoMorphTypeTags.kguidMorphPrefix;
					break;
				case MoMorphTypeTags.kMorphInfixingInterfix:
					result = deduced.Guid == MoMorphTypeTags.kguidMorphInfix;
					break;
				default:
					result = deduced.Equals(chosen);
					break;
			}
			if (result)
				return true;
			// The predicted form does not match, but the form the user chose would look identical (LT-12378).
			var expected = deduced.Prefix + strippedForm + deduced.Postfix;
			return expected == originalForm;
		}

		// The lift of InsertEntryDlg.CircumfixProblem (:1494-1521): a circumfix needs a left AND right part (two
		// morphemes separated by a space/period) in EVERY non-blank writing-system alternative; otherwise it is
		// incomplete. Non-circumfix morph types never have this problem.
		private static bool CircumfixProblem(LcmCache cache, IMoMorphType chosen,
			IReadOnlyDictionary<string, string> formsByWs)
		{
			if (chosen == null || chosen.Guid != MoMorphTypeTags.kguidMorphCircumfix)
				return false;
			if (formsByWs == null)
				return false;
			var wsFactory = cache.WritingSystemFactory;
			foreach (var pair in formsByWs)
			{
				var text = pair.Value?.Trim();
				if (string.IsNullOrEmpty(text))
					continue;
				var ws = wsFactory.GetWsFromStr(pair.Key);
				if (ws == 0)
					ws = cache.DefaultVernWs;
				var tss = TsStringUtils.MakeString(text, ws);
				if (!StringServices.GetCircumfixLeftAndRightParts(cache, tss, out _, out _))
					return true;
			}
			return false;
		}

		/// <summary>
		/// The LCModel-aware "re-mark the form with the morph type's markers" — the lift of
		/// <c>cbMorphType_SelectedIndexChanged</c>'s <c>BestForm = m_morphType.FormWithMarkers(BestForm)</c> (:1709).
		/// A circumfix is returned unchanged (as the legacy leaves it, since a circumfix mixes prefix/infix/suffix
		/// markers). An unresolvable key returns the form unchanged. Static so it is unit-testable against a real cache.
		/// </summary>
		internal static string ApplyMorphTypeMarkers(LcmCache cache, string morphTypeKey, string form)
		{
			form = form ?? string.Empty;
			var mt = ResolveMorphType(cache, morphTypeKey);
			if (mt == null || mt.Guid == MoMorphTypeTags.kguidMorphCircumfix)
				return form;
			return mt.FormWithMarkers(form);
		}

		// The best (first non-empty, trimmed) staged form across the alternatives — the launcher-side mirror of the
		// VM's BestStagedForm (used by the validation delegate, which receives the whole per-WS bag).
		private static string BestFormOf(IReadOnlyDictionary<string, string> formsByWs)
		{
			if (formsByWs == null)
				return string.Empty;
			foreach (var pair in formsByWs)
			{
				var text = pair.Value?.Trim();
				if (!string.IsNullOrEmpty(text))
					return text;
			}
			return string.Empty;
		}

		protected override InsertEntryDialogViewModel CreateViewModel(InsertEntryDialogInput state)
		{
			_viewModel = new InsertEntryDialogViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			// Wire the inline "Create a new Part of Speech..." affordance. The
			// VM raises CreateNewPosRequested with which chooser fired (main vs secondary); on it we run the
			// create-POS flow and, on success, refresh BOTH choosers + select the new POS in the requesting one.
			_viewModel.CreateNewPosRequested += OnCreateNewPosRequested;
			// Wire the inline create-feature / add-value affordances to the shared LcmCreateFeatureLauncher; on
			// success feed the new node back to the box's editor.
			_viewModel.CreateNewFeatureRequested += () =>
				LcmInflectionFeatureCreateWiring.CreateFeature(_cache, _owner, _viewModel.MsaGroupBox);
			_viewModel.CreateNewValueRequested += id =>
				LcmInflectionFeatureCreateWiring.AddValue(_cache, _owner, id, _viewModel.MsaGroupBox);
			return _viewModel;
		}

		/// <summary>
		/// Runs the create-POS flow when the user clicks "Create a new Part of Speech..." in either POS chooser.
		/// Opens the master-category catalog as a nested modal over
		/// the Insert Entry dialog (<see cref="LcmCreatePartOfSpeechLauncher"/>); on a created/chosen POS it re-feeds
		/// the freshly rebuilt project POS hierarchy to BOTH choosers (so the new category appears in each) and then
		/// hands the new node to the REQUESTING chooser (<see cref="FwPosTarget"/>) so it adds + selects it — the
		/// New-UI parity of POSPopupTreeManager re-loading the tree and selecting the new POS after MasterCategoryListDlg.
		/// </summary>
		private void OnCreateNewPosRequested(FwPosTarget target)
		{
			var node = LcmCreatePartOfSpeechLauncher.CreateInProject(_cache, _owner, _mediator, _propertyTable,
				_helpProvider);
			if (node == null)
				return; // user cancelled the catalog chooser

			// Re-feed the rebuilt project POS hierarchy (now including the new POS at its real depth) to BOTH choosers
			// and select it in the chooser that requested the create — the New-UI parity of POSPopupTreeManager
			// re-loading the tree + selecting the new POS after MasterCategoryListDlg returns.
			_viewModel.AcceptCreatedPos(target, node, BuildPosNodes(_cache));
		}

		protected override AvControl CreateView(InsertEntryDialogViewModel viewModel) =>
			new InsertEntryDialogView { DataContext = viewModel };

		/// <summary>
		/// Applies the OK result: creates the new <c>ILexEntry</c> in ONE undoable step from the view-model's
		/// snapshot (per-WS lexeme form + gloss alternatives + chosen morph type). Returns the same payload the
		/// view-model produced; the created entry is exposed via <see cref="CreatedEntry"/>.
		/// </summary>
		protected override InsertEntryPayload Apply(InsertEntryDialogInput state)
		{
			var payload = _viewModel?.Result;
			if (payload == null)
				return InsertEntryPayload.Empty;

			// "Go to similar entry": the user chose an EXISTING matching entry, so use it instead of creating a
			// duplicate (the legacy m_fNewlyCreated = false outcome). Resolve the chosen id to the live entry; the
			// caller jumps to it. No undoable create runs. NewlyCreated stays false (see WasNewlyCreated).
			if (!string.IsNullOrEmpty(payload.ChosenExistingEntryId))
			{
				CreatedEntry = ResolveEntry(_cache, payload.ChosenExistingEntryId);
				WasNewlyCreated = false;
				return payload;
			}

			ILexEntry newEntry = null;
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateEntry, LexTextControls.ksRedoCreateEntry,
				_cache.ServiceLocator.GetInstance<IActionHandler>(),
				() => { newEntry = CreateNewEntry(payload); });
			CreatedEntry = newEntry;
			WasNewlyCreated = newEntry != null;
			return payload;
		}

		/// <summary>Resolves an entry-id (legacy hvo string) back to the live <c>ILexEntry</c>, or null.</summary>
		internal static ILexEntry ResolveEntry(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || cache == null)
				return null;
			if (!int.TryParse(id, System.Globalization.NumberStyles.Integer,
				System.Globalization.CultureInfo.InvariantCulture, out var hvo))
				return null;
			return cache.ServiceLocator.GetInstance<ILexEntryRepository>().TryGetObject(hvo, out var entry)
				? entry
				: null;
		}

		/// <summary>
		/// Builds a <c>LexEntryComponents</c> from the dialog payload and creates the entry — the lift of
		/// InsertEntryDlg's BuildEntryComponentsDTO + CreateNewEntryInternal (~1548-1601). The morph type comes from
		/// the chosen key; the lexeme-form and gloss alternatives are rebuilt per writing system from the payload's
		/// per-WS strings. LT-11950: each alternative's TsString is rebuilt with the alternative's OWN writing
		/// system handle (TsStringUtils.MakeString(text, ws)) rather than trusting a possibly-mismatched ws carried
		/// on copied text — the same fix-up the legacy CollectValuesFromMultiStringControl applies. Internal so the
		/// create is unit-testable against a real cache inside a UOW.
		/// </summary>
		internal ILexEntry CreateNewEntry(InsertEntryPayload payload)
		{
			var components = BuildEntryComponents(_cache, payload);
			var entry = _cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
			// SandboxGenericMSA carries no inflection class, so set it on the find-or-created stem MSA AFTER
			// creation (the lift of InsertEntryDlg.SetEntryMsa, which sets IMoStemMsa.InflectionClassRA on the new
			// sense's MSA). Same UOW as the create (this runs inside the caller's UndoableUnitOfWorkHelper.Do).
			ApplyInflectionClass(_cache, entry, payload.Msa);
			// SandboxGenericMSA carries no inflection features either; rebuild the inflection IFsFeatStruc
			// on the find-or-created infl/deriv MSA from the chosen assignment set, same UOW as the create. A stem MSA
			// (or no features) is a no-op.
			if (payload.Msa != null && entry.SensesOS.Count > 0)
				ApplyInflectionFeatures(_cache, entry.SensesOS[0], payload.Msa);
			// Complex Form Type (WinForms m_cbComplexFormType parity, LT-21666): when the user chose a real
			// complex-form type, add a complex-form ILexEntryRef carrying it — the lift of CreateNewEntryInternal's
			// m_fComplexForm branch. Same UOW as the create. "<Not Applicable>" (a null/empty key) adds nothing.
			ApplyComplexFormType(_cache, entry, payload.ComplexFormTypeKey);
			return entry;
		}

		/// <summary>
		/// Sets the chosen inflection class (the box's <see cref="FwSandboxMsa.InflectionClassId"/>) on the entry's
		/// first sense's STEM MSA — the lift of <c>InsertEntryDlg.SetEntryMsa</c> (set
		/// <c>IMoStemMsa.InflectionClassRA</c> after the sense/MSA are created). PARITY: only the stem/root MSA is
		/// covered (derivational from/to inflection classes are out of scope). A null/unresolvable id leaves
		/// the class null (the "&lt;None&gt;" pick). Internal static so the set is unit-testable inside a UOW.
		/// </summary>
		internal static void ApplyInflectionClass(LcmCache cache, ILexEntry entry, FwSandboxMsa chosen)
		{
			if (entry == null || chosen == null || string.IsNullOrEmpty(chosen.InflectionClassId))
				return;
			if (entry.SensesOS.Count == 0)
				return;
			ApplyInflectionClass(cache, entry.SensesOS[0], chosen);
		}

		/// <summary>
		/// Sets the chosen inflection class on a sense's STEM MSA (the lift of <c>InsertEntryDlg.SetEntryMsa</c>'s
		/// <c>IMoStemMsa</c> branch). Resolves the id to an <c>IMoInflClass</c> and assigns <c>InflectionClassRA</c>;
		/// a non-stem MSA or an unresolvable id is a no-op. Internal static so both create paths (Insert Entry / Add
		/// New Sense) share it and it is unit-testable inside a UOW.
		/// </summary>
		internal static void ApplyInflectionClass(LcmCache cache, ILexSense sense, FwSandboxMsa chosen)
		{
			if (sense == null || chosen == null || string.IsNullOrEmpty(chosen.InflectionClassId))
				return;
			if (sense.MorphoSyntaxAnalysisRA is IMoStemMsa stemMsa)
			{
				var inflClass = ResolveInflectionClass(cache, chosen.InflectionClassId);
				if (inflClass != null)
					stemMsa.InflectionClassRA = inflClass;
			}
		}

		/// <summary>
		/// Adds a complex-form <c>ILexEntryRef</c> to the new entry carrying the chosen complex-form type — the lift
		/// of <c>InsertEntryDlg.CreateNewEntryInternal</c>'s <c>m_fComplexForm</c> branch (LT-21666): create the ref,
		/// add it to <c>EntryRefsOS</c>, add the resolved <c>ILexEntryType</c> to <c>ComplexEntryTypesRS</c>, and set
		/// <c>RefType = krtComplexForm</c>. No components are added (the WinForms dialog adds none). A null/empty or
		/// unresolvable key is a no-op (the "&lt;Not Applicable&gt;" pick). Runs inside the caller's UOW. Internal
		/// static so the create is unit-testable against a real cache.
		/// </summary>
		internal static void ApplyComplexFormType(LcmCache cache, ILexEntry entry, string complexFormTypeKey)
		{
			if (entry == null || string.IsNullOrEmpty(complexFormTypeKey))
				return;
			var type = ResolveComplexFormType(cache, complexFormTypeKey);
			if (type == null)
				return;
			var ler = cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			entry.EntryRefsOS.Add(ler);
			ler.ComplexEntryTypesRS.Add(type);
			ler.RefType = LexEntryRefTags.krtComplexForm;
		}

		private static ILexEntryType ResolveComplexFormType(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
				return null;
			try { return cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(guid); }
			catch { return null; }
		}

		private static IMoInflClass ResolveInflectionClass(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
				return null;
			try { return cache.ServiceLocator.GetInstance<IMoInflClassRepository>().GetObject(guid); }
			catch { return null; }
		}

		internal static LexEntryComponents BuildEntryComponents(LcmCache cache, InsertEntryPayload payload)
		{
			var components = new LexEntryComponents { MorphType = ResolveMorphType(cache, payload.MorphTypeKey) };

			AddAlternatives(cache, payload.LexemeFormByWs, components.LexemeFormAlternatives,
				cache.DefaultVernWs);
			AddAlternatives(cache, payload.GlossByWs, components.GlossAlternatives, cache.DefaultAnalWs);

			// Build the real SandboxGenericMSA from the dialog's chosen grammatical info (the lift of
			// InsertEntryDlg's m_msaGroupBox.SandboxMSA). The LexEntryFactory.Create FIND-OR-CREATEs the matching MSA
			// on the entry's first sense from this descriptor (POS + slot/secondary POS), exactly as the WinForms
			// dialog does. When no MSA was chosen (older callers / no MSA section) fall back to the morph-type default.
			components.MSA = BuildSandboxMsa(cache, payload.Msa, components.MorphType);
			return components;
		}

		/// <summary>
		/// Resolves the dialog's LCModel-free <see cref="FwSandboxMsa"/> (MsaType + POS/slot ids) into a real
		/// <c>SandboxGenericMSA</c> the factory uses to find-or-create the sense's MSA — the parity of
		/// <c>MSAGroupBox.SandboxMSA</c>: only the fields relevant to the MsaType are populated, the POS/slot ids are
		/// resolved back through the repositories by guid, and an unresolvable id is simply dropped (the &lt;Any&gt;
		/// pick). A null descriptor falls back to the morph-type's default MSA flavor. Internal for unit testing.
		/// </summary>
		internal static SandboxGenericMSA BuildSandboxMsa(LcmCache cache, FwSandboxMsa chosen, IMoMorphType morphType)
		{
			if (chosen == null)
				return new SandboxGenericMSA { MsaType = DefaultMsaType(morphType) };

			var msa = new SandboxGenericMSA { MsaType = ToLcmMsaType(chosen.MsaType, morphType) };
			var mainPos = ResolvePos(cache, chosen.MainPosId);
			switch (msa.MsaType)
			{
				case MsaType.kRoot:
				case MsaType.kStem:
				case MsaType.kUnclassified:
					msa.MainPOS = mainPos;
					break;
				case MsaType.kInfl:
					msa.MainPOS = mainPos;
					var slot = ResolveSlot(cache, chosen.SlotId);
					if (slot != null)
						msa.Slot = slot;
					break;
				case MsaType.kDeriv:
					msa.MainPOS = mainPos;
					msa.SecondaryPOS = ResolvePos(cache, chosen.SecondaryPosId);
					break;
			}
			return msa;
		}

		// Maps the kit FwMsaType to the LCModel MsaType. FwMsaType.NotSet falls back to the morph-type default.
		private static MsaType ToLcmMsaType(FwMsaType type, IMoMorphType morphType)
		{
			switch (type)
			{
				case FwMsaType.Stem: return MsaType.kStem;
				case FwMsaType.Root: return MsaType.kRoot;
				case FwMsaType.Inflectional: return MsaType.kInfl;
				case FwMsaType.Derivational: return MsaType.kDeriv;
				case FwMsaType.Unclassified: return MsaType.kUnclassified;
				default: return DefaultMsaType(morphType);
			}
		}

		private static IPartOfSpeech ResolvePos(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
				return null;
			try { return cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(guid); }
			catch { return null; }
		}

		private static IMoInflAffixSlot ResolveSlot(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
				return null;
			try { return cache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(guid); }
			catch { return null; }
		}

		// LT-11950 fix-up: rebuild each alternative's TsString with its OWN writing-system handle, never a ws that
		// might have ridden along on copied text. The payload is keyed by the alternative's IETF tag; we resolve
		// that to the handle here. Empty/unresolvable alternatives are dropped (only non-empty rows were staged).
		private static void AddAlternatives(LcmCache cache, IReadOnlyDictionary<string, string> byWsTag,
			IList<ITsString> collector, int fallbackWs)
		{
			var wsFactory = cache.WritingSystemFactory;
			foreach (var pair in byWsTag)
			{
				if (string.IsNullOrEmpty(pair.Value))
					continue;
				var ws = wsFactory.GetWsFromStr(pair.Key);
				if (ws == 0)
					ws = fallbackWs;
				collector.Add(TsStringUtils.MakeString(pair.Value, ws));
			}
		}

		private static IMoMorphType ResolveMorphType(LcmCache cache, string key)
		{
			var repo = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			if (!string.IsNullOrEmpty(key) && Guid.TryParse(key, out var guid))
			{
				try
				{
					return repo.GetObject(guid);
				}
				catch
				{
					// fall through to the default stem
				}
			}
			return repo.GetObject(MoMorphTypeTags.kguidMorphStem);
		}

		// The default MSA flavor a morph type implies (parity with MSAGroupBox's morph-type-driven default): roots
		// take a root/stem MSA, affixes an unclassified-affix MSA, everything else a stem MSA.
		private static MsaType DefaultMsaType(IMoMorphType morphType)
		{
			if (morphType == null)
				return MsaType.kStem;
			switch (morphType.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphPrefix:
				case MoMorphTypeTags.kMorphInfix:
				case MoMorphTypeTags.kMorphSuffix:
				case MoMorphTypeTags.kMorphSimulfix:
				case MoMorphTypeTags.kMorphSuprafix:
				case MoMorphTypeTags.kMorphCircumfix:
				case MoMorphTypeTags.kMorphInfixingInterfix:
				case MoMorphTypeTags.kMorphPrefixingInterfix:
				case MoMorphTypeTags.kMorphSuffixingInterfix:
					return MsaType.kUnclassified;
				default:
					return MsaType.kStem;
			}
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
