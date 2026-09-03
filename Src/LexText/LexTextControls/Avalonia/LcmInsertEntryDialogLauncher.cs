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
using SIL.FieldWorks.Common.FwAvalonia.Detail;
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
	/// The LCModel-aware launcher for the reusable Avalonia Insert Entry dialog -- the
	/// replacement for the legacy
	/// <see cref="InsertEntryDlg"/> in New-UI mode. It is a concrete
	/// <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>: the Avalonia layer (FwAvaloniaDialogs)
	/// stays LCModel-free by exchanging an <see cref="InsertEntryDlgInput"/> (lexeme-form / gloss fields built
	/// for the cache's current writing systems, morph types as guid-keyed <see cref="DetailChoiceOption"/>s, and a
	/// plain <see cref="InsertEntryDlgInput.DeriveMorphType"/> delegate) and an <see cref="InsertEntryDlgPayload"/>
	/// (per-WS form + gloss strings + the chosen morph-type key). This launcher builds that state from the live
	/// cache and, on OK, creates the <c>ILexEntry</c> in ONE undoable step.
	///
	/// Layering mirrors <see cref="LcmChooserDialogLauncher"/>/AvaloniaOptionsDialogLauncher exactly: BuildState /
	/// Apply are internal so the full state mapping + create are unit-testable against a real cache (via
	/// InternalsVisibleTo) without running the modal.
	/// </summary>
	public sealed class LcmInsertEntryDialogLauncher
		: AvaloniaDialogLauncher<InsertEntryDlgInput, InsertEntryDlgViewModel, InsertEntryDlgPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ITsString _tssForm;
		private InsertEntryDlgViewModel _viewModel;
		// The WinForms host the Insert Entry modal is owned by -- captured so the nested
		// create-POS modal (raised from
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
			// Create -- mirroring the legacy InsertEntryDlg.GetDialogInfo out-params.
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
		// below them -- the legacy InsertEntryDlg is similarly tall to fit its similar-entries
		// browser.
		protected override int DialogHeight => 460;

		/// <summary>
		/// Reads the remembered client size from the SAME registry location + value names the legacy InsertEntryDlg
		/// persisted (<c>FieldWorksRegistryKey\LingCmnDlgs</c> InsertWidth/InsertHeight --
		/// :1603-1618), so a
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

		protected override InsertEntryDlgInput BuildState() =>
			BuildInput(_cache, _tssForm, _mediator, _propertyTable);

		/// <summary>
		/// Builds the LCModel-free <see cref="InsertEntryDlgInput"/> from the live cache: a per-vernacular-WS
		/// lexeme-form field (seeded from <paramref name="tssForm"/> when it is a vernacular string), a
		/// per-analysis-WS gloss field, the morph-type options, the default (stem) morph-type selection, and the
		/// live affix-marker -> morph-type derivation. Internal so the full state mapping is
		/// unit-testable against a
		/// real cache without running the modal (mirrors LcmChooserDialogLauncher.BuildInput).
		/// </summary>
		internal static InsertEntryDlgInput BuildInput(LcmCache cache, ITsString tssForm,
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

			return new InsertEntryDlgInput
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
				// Grammatical-info (MSA) section: the project POS hierarchy + the morph-type ->
				// MsaType map +
				// the per-POS slot provider, so the LCModel-free MSAGroupBox can drive its layout live.
				PosNodes = GrammaticalInfoProjection.BuildPosNodes(cache),
				MorphTypeToMsaType = GrammaticalInfoProjection.BuildMorphTypeToMsaTypeMap(cache),
				InitialMsaType = GrammaticalInfoProjection.MorphTypeGuidToMsaType(
					MoMorphTypeTags.kguidMorphStem.ToString()),
				InitialMainPosId = null,
				SlotsForPos = posId => GrammaticalInfoProjection.BuildSlots(cache, posId, MoMorphTypeTags.kguidMorphStem.ToString()),
				// Inflection-class picker: the selected main POS's classes, re-fed when the main POS changes.
				InflectionClassesForPos = posId => GrammaticalInfoProjection.BuildInflectionClasses(cache, posId),
				InitialInflectionClassId = null,
				// Inflection-feature editor: the selected main POS's inflectable-feature system,
				// re-fed
				// when the main POS changes (infl/deriv). No initial features on the create path.
				InflectionFeaturesForPos = posId => GrammaticalInfoProjection.BuildInflectionFeatures(cache, posId),
				InitialInflectionFeatures = null,
				// Complex Form Type picker (WinForms m_cbComplexFormType, LT-21666): the
				// project's complex-form types plus morph-type gating map. Opens at
				// "<Not Applicable>" (legacy SelectedIndex 0): no initial type.
				ComplexFormTypes = BuildComplexFormTypeOptions(cache),
				InitialComplexFormTypeKey = null,
				ComplexFormGatingByMorphType = BuildComplexFormGatingMap(cache),
				Prompt = null,
				// The Help button opens the same topic the legacy dialog uses (InsertEntryDlg s_helpTopic :81).
				HelpTopic = "khtpInsertEntry"
			};
		}

		/// <summary>
		/// Builds the duplicate-detection ("matching entries") search delegate the dialog drives as the user types
		/// the lexeme form or gloss -- the lift of <c>InsertEntryDlg.UpdateMatches</c> +
		/// <c>GetFields</c>. It uses the
		/// SAME engine the legacy dialog uses (<see cref="InsertEntrySearchEngine"/> over the live entry repository),
		/// keyed on the legacy field set: the vernacular citation/lexeme/alternate FORMS (the "do not create a second
		/// 'casa'" case) AND the analysis GLOSS (legacy <c>:1030-1031</c> -- typing a gloss
		/// surfaces same-gloss entries).
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

		// Gets the InsertEntrySearchEngine, same engine legacy InsertEntryDlg
		// uses to search vernacular forms and analysis gloss -- cached on the
		// property table; no property table (tests) builds a fresh one.
		private static InsertEntrySearchEngine GetMatchSearchEngine(LcmCache cache, Mediator mediator,
			PropertyTable propertyTable)
		{
			if (propertyTable == null)
				return new InsertEntrySearchEngine(cache);
			return (InsertEntrySearchEngine)SearchEngine.Get(mediator, propertyTable,
				"AvaloniaInsertEntryMatchSearchEngine", () => new InsertEntrySearchEngine(cache));
		}

		/// <summary>
		/// Builds a per-writing-system text field: one <see cref="DetailWsValue"/> row per writing system, seeded
		/// empty unless <paramref name="initialForm"/> is supplied (then the first/default WS row carries it). The
		/// row's WsTag (the IETF tag) is the key the in-memory edit context stages each
		/// alternative under -- and the
		/// key Apply reads back to build the per-WS LexEntryComponents alternatives.
		/// </summary>
		internal static DetailField BuildTextField(string field, string automationId,
			IEnumerable<CoreWritingSystemDefinition> writingSystems, string initialForm, string label)
		{
			var values = new List<DetailWsValue>();
			var seeded = false;
			foreach (var ws in writingSystems)
			{
				var seedThis = !seeded && !string.IsNullOrEmpty(initialForm);
				values.Add(new DetailWsValue(ws.Abbreviation, seedThis ? initialForm : string.Empty,
					ws.DefaultFontName, 0, ws.RightToLeftScript, ws.Id));
				if (seedThis)
					seeded = true;
			}

			return new DetailField(field, label, field, null, DetailFieldKind.Text,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification), automationId, field,
				default(SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.HostRouting), values,
				new List<DetailChoiceOption>(), selectedOptionKey: null, isEditable: true);
		}

		/// <summary>
		/// Builds the morph-type options (key = morph-type guid string, name = best-analysis
		/// display name) from
		/// the project's morph types, in sorted display order -- the legacy "Any" morph-type
		/// filter (every type).
		/// </summary>
		internal static IReadOnlyList<DetailChoiceOption> BuildMorphTypeOptions(LcmCache cache)
		{
			var types = cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
				.Cast<IMoMorphType>()
				.OrderBy(MorphTypeName, StringComparer.CurrentCulture)
				.ToList();
			return types
				.Select(t => new DetailChoiceOption(t.Guid.ToString(), MorphTypeName(t)))
				.ToList();
		}

		private static string MorphTypeName(IMoMorphType type)
			=> type.Name.BestAnalysisAlternative?.Text ?? type.ShortName ?? type.Guid.ToString();

		/// <summary>
		/// Builds the complex-form type options (key = complex-entry-type guid string, name = best-analysis display
		/// name) from <c>LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities</c>, in sorted
		/// display order -- the
		/// lift of the WinForms <c>m_cbComplexFormType</c> fill (which sorts the possibilities and prepends the
		/// "&lt;Not Applicable&gt;" item; the dialog prepends that row itself). Internal so the feed is unit-testable.
		/// </summary>
		internal static IReadOnlyList<DetailChoiceOption> BuildComplexFormTypeOptions(LcmCache cache)
		{
			return cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities
				.OfType<ILexEntryType>()
				.OrderBy(ComplexFormTypeName, StringComparer.CurrentCulture)
				.Select(t => new DetailChoiceOption(t.Guid.ToString(), ComplexFormTypeName(t)))
				.ToList();
		}

		private static string ComplexFormTypeName(ILexEntryType type)
			=> type.Name.BestAnalysisAlternative?.Text ?? type.ShortName ?? type.Guid.ToString();

		/// <summary>
		/// Builds the morph-type-guid -> <see cref="ComplexFormGating"/> map -- the data lift of
		/// the WinForms
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

		// The morph-type-guid -> ComplexFormGating rule, lifted verbatim from
		// EnableComplexFormTypeCombo's switch.
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
		/// The live affix-marker -> morph-type derivation, lifted from the legacy
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
		/// The LCModel-aware OK-time morphology validation -- the lift of <c>InsertEntryDlg</c>'s
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
		/// The LCModel-aware "re-mark the form with the morph type's markers" -- the lift of
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

		// The best (first non-empty, trimmed) staged form across the alternatives -- the
		// launcher-side mirror of the
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

		protected override InsertEntryDlgViewModel CreateViewModel(InsertEntryDlgInput state)
		{
			_viewModel = new InsertEntryDlgViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			// Wire the inline "Create a new Part of Speech..." affordance. The event
			// carries which chooser fired (main or secondary); on success both
			// choosers refresh and the requesting one selects the new POS.
			_viewModel.CreateNewPosRequested += OnCreateNewPosRequested;
			// Wire the inline create-feature / add-value affordances to the shared
			// LcmCreateFeatureLauncher; on
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
		/// hands the new node to the REQUESTING chooser (<see cref="FwPosTarget"/>) so it adds +
		/// selects it -- the
		/// New-UI parity of POSPopupTreeManager re-loading the tree and selecting the new POS after MasterCategoryListDlg.
		/// </summary>
		private void OnCreateNewPosRequested(FwPosTarget target)
		{
			var node = LcmCreatePartOfSpeechLauncher.CreateInProject(_cache, _owner, _mediator, _propertyTable,
				_helpProvider);
			if (node == null)
				return; // user cancelled the catalog chooser

			// Re-feed the rebuilt POS hierarchy (including the new POS at its real
			// depth) to BOTH choosers, selecting it in the requesting chooser --
			// mirrors POSPopupTreeManager after MasterCategoryListDlg returns.
			_viewModel.AcceptCreatedPos(target, node, GrammaticalInfoProjection.BuildPosNodes(_cache));
		}

		protected override AvControl CreateView(InsertEntryDlgViewModel viewModel) =>
			new InsertEntryDlgView { DataContext = viewModel };

		/// <summary>
		/// Applies the OK result: creates the new <c>ILexEntry</c> in ONE undoable step from the view-model's
		/// snapshot (per-WS lexeme form + gloss alternatives + chosen morph type). Returns the same payload the
		/// view-model produced; the created entry is exposed via <see cref="CreatedEntry"/>.
		/// </summary>
		protected override InsertEntryDlgPayload Apply(InsertEntryDlgInput state)
		{
			var payload = _viewModel?.Result;
			if (payload == null)
				return InsertEntryDlgPayload.Empty;

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

		/// <summary>Creates the entry for <paramref name="payload"/> inside the caller's undo
		/// task.</summary>
		internal ILexEntry CreateNewEntry(InsertEntryDlgPayload payload) => CreateNewEntry(_cache, payload);

		/// <summary>
		/// Builds a <c>LexEntryComponents</c> from the dialog payload and creates the
		/// entry -- the lift of InsertEntryDlg's BuildEntryComponentsDTO +
		/// CreateNewEntryInternal (~1548-1601). The morph type comes from the chosen key;
		/// the lexeme-form and gloss alternatives are rebuilt per writing system from the
		/// payload's per-WS strings. LT-11950: each alternative's TsString is rebuilt with
		/// the alternative's OWN writing system handle (TsStringUtils.MakeString(text, ws))
		/// rather than trusting a possibly-mismatched ws carried on copied text -- the same
		/// fix-up the legacy CollectValuesFromMultiStringControl applies. The four steps
		/// below are one ordering contract: create, then inflection class, then inflection
		/// features, then complex-form type, all inside the caller's undo task. Static so
		/// that contract is the thing the tests call.
		/// </summary>
		internal static ILexEntry CreateNewEntry(LcmCache cache, InsertEntryDlgPayload payload)
		{
			var components = BuildEntryComponents(cache, payload);
			var entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
			// SandboxGenericMSA carries no inflection class, so set it on the find-or-created stem MSA AFTER
			// creation (the lift of InsertEntryDlg.SetEntryMsa, which sets IMoStemMsa.InflectionClassRA on the new
			// sense's MSA). Same UOW as the create (this runs inside the caller's UndoableUnitOfWorkHelper.Do).
			GrammaticalInfoProjection.ApplyInflectionClass(cache, entry, payload.Msa);
			// SandboxGenericMSA carries no inflection features either; rebuild the inflection IFsFeatStruc
			// on the find-or-created infl/deriv MSA from the chosen assignment set, same UOW as the create. A stem MSA
			// (or no features) is a no-op.
			if (payload.Msa != null && entry.SensesOS.Count > 0)
				GrammaticalInfoProjection.ApplyInflectionFeatures(cache, entry.SensesOS[0], payload.Msa);
			// Complex Form Type (WinForms m_cbComplexFormType, LT-21666): a real chosen
			// type adds a complex-form ILexEntryRef carrying it, same UOW as the create.
			// "<Not Applicable>" (null/empty key) adds nothing.
			ApplyComplexFormType(cache, entry, payload.ComplexFormTypeKey);
			return entry;
		}

		/// <summary>
		/// Adds a complex-form <c>ILexEntryRef</c> to the new entry carrying the chosen
		/// complex-form type -- the lift
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

		internal static LexEntryComponents BuildEntryComponents(LcmCache cache, InsertEntryDlgPayload payload)
		{
			var components = new LexEntryComponents { MorphType = ResolveMorphType(cache, payload.MorphTypeKey) };

			AddAlternatives(cache, payload.LexemeFormByWs, components.LexemeFormAlternatives,
				cache.DefaultVernWs);
			AddAlternatives(cache, payload.GlossByWs, components.GlossAlternatives, cache.DefaultAnalWs);

			// Build the real SandboxGenericMSA from the dialog's chosen grammatical info (the lift of
			// InsertEntryDlg's m_msaGroupBox.SandboxMSA). The LexEntryFactory.Create FIND-OR-CREATEs the matching MSA
			// on the entry's first sense from this descriptor (POS + slot/secondary POS), exactly as the WinForms
			// dialog does. When no MSA was chosen (older callers / no MSA section) fall back to the morph-type default.
			components.MSA = GrammaticalInfoProjection.BuildSandboxMsa(cache, payload.Msa, components.MorphType);
			return components;
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

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
