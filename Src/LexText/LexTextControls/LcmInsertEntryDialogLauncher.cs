// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FwAvaloniaDialogs;
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
	/// The LCModel-aware launcher for the reusable Avalonia Insert Entry dialog — the Phase 1 product-side
	/// replacement for the legacy <see cref="InsertEntryDlg"/> in New-UI mode. It is a concrete
	/// <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>: the Avalonia layer (FwAvaloniaDialogs)
	/// stays LCModel-free by exchanging an <see cref="InsertEntryDialogInput"/> (lexeme-form / gloss fields built
	/// for the cache's current writing systems, morph types as guid-keyed <see cref="RegionChoiceOption"/>s, and a
	/// plain <see cref="InsertEntryDialogInput.DeriveMorphType"/> delegate) and an <see cref="InsertEntryPayload"/>
	/// (per-WS form + gloss strings + the chosen morph-type key). This launcher builds that state from the live
	/// cache and, on OK, creates the <c>ILexEntry</c> in ONE undoable step.
	///
	/// Layering mirrors <see cref="LcmChooserDialogLauncher"/>/AvaloniaOptionsDialogLauncher exactly: BuildState /
	/// Apply are internal so the full state mapping + create are unit-testable against a real cache (via
	/// InternalsVisibleTo) without running the modal. Phase 1 scope is lexeme form + gloss + morph type; the
	/// duplicate-match list (P2) and the complex-form / MSA / Create-and-Edit affordances (P3) are deferred.
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
		/// <param name="mediator">The XCore mediator (carried for parity; unused in Phase 1).</param>
		/// <param name="propertyTable">The XCore property table (carried for parity; unused in Phase 1).</param>
		/// <param name="owner">The WinForms host the modal is owned by.</param>
		/// <param name="tssForm">An optional initial lexeme form (e.g. the word the user selected); null/empty starts empty.</param>
		/// <param name="helpProvider">The help provider (carried; Help wiring is P3).</param>
		public static (ILexEntry entry, bool newlyCreated) Show(LcmCache cache, Mediator mediator,
			PropertyTable propertyTable, IWin32Window owner, ITsString tssForm = null,
			IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var launcher = new LcmInsertEntryDialogLauncher(cache, mediator, propertyTable, helpProvider, tssForm);
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
		// (P2) below them — the legacy InsertEntryDlg is similarly tall to fit its similar-entries browser.
		protected override int DialogHeight => 460;

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

			// The optional initial form only seeds the lexeme form when it is a vernacular string (parity with the
			// legacy SetDlgInfo, which routes a non-vernacular initial string to the gloss instead — deferred here:
			// Phase 1 seeds only the vernacular lexeme form, leaving the gloss empty).
			string initialForm = null;
			if (tssForm != null && tssForm.Length > 0)
			{
				var wsForm = TsStringUtils.GetWsAtOffset(tssForm, 0);
				if (wsContainer.CurrentVernacularWritingSystems.Any(ws => ws.Handle == wsForm))
					initialForm = tssForm.Text;
			}

			var lexemeForm = BuildTextField("LexemeForm", "InsertEntry.LexemeForm",
				wsContainer.CurrentVernacularWritingSystems, initialForm,
				FwAvaloniaDialogsStrings.InsertEntryLexemeFormLabel);
			var gloss = BuildTextField("Gloss", "InsertEntry.Gloss",
				wsContainer.CurrentAnalysisWritingSystems, initialForm: null,
				FwAvaloniaDialogsStrings.InsertEntryGlossLabel);

			return new InsertEntryDialogInput
			{
				LexemeForm = lexemeForm,
				Gloss = gloss,
				MorphTypes = BuildMorphTypeOptions(cache),
				InitialMorphTypeKey = MoMorphTypeTags.kguidMorphStem.ToString(),
				DeriveMorphType = form => DeriveMorphType(cache, form),
				SearchMatches = BuildMatchSearch(cache, mediator, propertyTable),
				Prompt = null,
				HelpTopic = null
			};
		}

		/// <summary>
		/// Builds the duplicate-detection ("matching entries") search delegate the dialog drives as the user types
		/// the lexeme form — the P2 lift of <c>InsertEntryDlg.UpdateMatches</c>. It reuses the SAME matching the
		/// legacy EntryGoDlg family uses (the shared <see cref="EntryGoSearchEngine"/> over the live entry repository,
		/// the legacy vernacular citation/lexeme/alternate-form field set) via <see cref="EntryGoLauncherShared"/>,
		/// with NO exclusion (a create flow has no "current" entry to exclude — every match is a candidate the user
		/// may reuse instead of creating a duplicate). Each match maps to a lightweight headword + gloss row. Internal
		/// so the match semantics are unit-testable against a real cache without running the modal.
		///
		/// NOTE on parity: the legacy dialog also searches the GLOSS field and runs MorphServices.EnsureNoMarkers on
		/// the form before searching; here the VM's affix-marker derivation already adjusts the staged form before the
		/// search runs, and the search keys on the vernacular form fields (the duplicate-by-form case that matters for
		/// "do not create a second 'casa'"). The gloss-match column is the one piece deferred — see // PARITY below.
		/// </summary>
		internal static Func<string, IReadOnlyList<EntryGoSearchResult>> BuildMatchSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable)
		{
			// PARITY: the legacy InsertEntrySearchEngine also matches on LexSenseTags.kflidGloss (so typing a gloss
			// surfaces same-gloss entries). This slice reuses the shared EntryGoSearchEngine form-field matching
			// (citation/lexeme/alternate forms) — the duplicate-FORM detection that drives the "do not duplicate this
			// lexeme" case — and defers the gloss column. The matches list still populates from real entries by form.
			return EntryGoLauncherShared.BuildEntrySearch(cache, mediator, propertyTable,
				excludedEntryHvo: 0, engineCacheKey: "AvaloniaInsertEntryMatchSearchEngine", filter: null);
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

		protected override InsertEntryDialogViewModel CreateViewModel(InsertEntryDialogInput state)
		{
			_viewModel = new InsertEntryDialogViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			return _viewModel;
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
		/// Builds a <c>LexEntryComponents</c> from the dialog payload and creates the entry — the Phase 1 lift of
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
			return _cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
		}

		internal static LexEntryComponents BuildEntryComponents(LcmCache cache, InsertEntryPayload payload)
		{
			var components = new LexEntryComponents { MorphType = ResolveMorphType(cache, payload.MorphTypeKey) };

			AddAlternatives(cache, payload.LexemeFormByWs, components.LexemeFormAlternatives,
				cache.DefaultVernWs);
			AddAlternatives(cache, payload.GlossByWs, components.GlossAlternatives, cache.DefaultAnalWs);

			// Phase 1 defers POS/MSA selection (P3): supply the default MSA the morph type implies so the factory
			// can create the entry's sense + MSA, matching the legacy dialog's "no MSA chosen yet" default.
			components.MSA = new SandboxGenericMSA { MsaType = DefaultMsaType(components.MorphType) };
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
