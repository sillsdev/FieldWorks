// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia Add New Sense dialog — the MSA-port Stage 5 product-side
	/// replacement for the legacy <see cref="AddNewSenseDlg"/> in New-UI mode. It is a concrete
	/// <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>: the Avalonia layer (FwAvaloniaDialogs) stays
	/// LCModel-free by exchanging an <see cref="AddNewSenseDialogInput"/> (the read-only citation form, a per-WS gloss
	/// field, and the MSA section's POS nodes / slot provider / initial MsaType) and an <see cref="AddNewSensePayload"/>
	/// (per-WS gloss strings + the chosen <see cref="FwSandboxMsa"/>). This launcher builds that state from the live
	/// cache and, on OK, creates the new <c>ILexSense</c> (gloss + find-or-created MSA) in ONE undoable step.
	///
	/// Layering + the MSA mapping mirror <see cref="LcmInsertEntryDialogLauncher"/> exactly (they share its internal
	/// <c>BuildPosNodes</c>/<c>BuildSlots</c>/<c>BuildSandboxMsa</c> + create-POS routing); BuildState / Apply are
	/// internal so the full state mapping + create are unit-testable against a real cache (via InternalsVisibleTo)
	/// without running the modal. The legacy "find the entry's morph type to seed the MSA class" step is the lift of
	/// <c>AddNewSenseDlg.SetDlgInfo</c>'s <c>MorphTypePreference</c> loop.
	/// </summary>
	public sealed class LcmAddNewSenseDialogLauncher
		: AvaloniaDialogLauncher<AddNewSenseDialogInput, AddNewSenseDialogViewModel, AddNewSensePayload>
	{
		private const string s_helpTopic = "khtpAddNewSense";

		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ILexEntry _entry;
		private readonly ITsString _tssCitationForm;
		private AddNewSenseDialogViewModel _viewModel;
		private IWin32Window _owner;

		private LcmAddNewSenseDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ILexEntry entry, ITsString tssCitationForm)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_entry = entry;
			_tssCitationForm = tssCitationForm;
		}

		/// <summary>
		/// Shows the Add New Sense dialog modally over <paramref name="owner"/> and, on OK, creates the new
		/// <c>ILexSense</c> on <paramref name="entry"/> (gloss + find-or-created MSA) in one undoable step. Returns
		/// the new sense's hvo (the legacy <c>AddNewSenseDlg.GetDlgInfo</c> out-param), or 0 when cancelled.
		/// </summary>
		public static int Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable, ILexEntry entry,
			ITsString tssCitationForm, IWin32Window owner, IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			if (entry == null) throw new ArgumentNullException(nameof(entry));

			var launcher = new LcmAddNewSenseDialogLauncher(cache, mediator, propertyTable, helpProvider, entry,
				tssCitationForm);
			launcher._owner = owner;
			var outcome = launcher.Run(owner);
			if (!outcome.Accepted || outcome.Payload == null)
				return 0;
			return launcher.NewSenseHvo;
		}

		/// <summary>The hvo of the sense created on OK; 0 when cancelled or nothing created.</summary>
		public int NewSenseHvo { get; private set; }

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.AddNewSenseTitle;
		protected override bool Resizable => true;
		// Wide enough to fit the FwMsaGroupBox's three-column affix layout (Affix Type + Attaches to Category +
		// Fills Slot / Changes to Category) without clipping the trailing column caption.
		protected override int DialogWidth => 500;
		protected override int DialogHeight => 360;

		protected override AddNewSenseDialogInput BuildState() =>
			BuildInput(_cache, _entry, _tssCitationForm);

		/// <summary>
		/// Builds the LCModel-free <see cref="AddNewSenseDialogInput"/> from the live cache: the read-only citation
		/// form (from the supplied tss or the entry's headword), a per-analysis-WS gloss field, the project POS
		/// hierarchy + per-POS slot provider (shared with the Insert Entry launcher), and the initial MsaType the
		/// entry's morph type implies (the lift of <c>AddNewSenseDlg.SetDlgInfo</c>'s <c>MorphTypePreference</c> loop:
		/// the first allomorph's morph type drives the MSA class). Internal so the mapping is unit-testable against a
		/// real cache without running the modal.
		/// </summary>
		internal static AddNewSenseDialogInput BuildInput(LcmCache cache, ILexEntry entry, ITsString tssCitationForm)
		{
			var wsContainer = cache.ServiceLocator.WritingSystems;
			var citation = tssCitationForm?.Text;
			if (string.IsNullOrEmpty(citation))
				citation = entry.HeadWord?.Text;

			var gloss = LcmInsertEntryDialogLauncher.BuildTextField("Gloss", "AddNewSense.Gloss",
				wsContainer.CurrentAnalysisWritingSystems, initialForm: null,
				FwAvaloniaDialogsStrings.AddNewSenseGlossLabel);

			// The entry's morph type drives the MSA class the box opens in (the legacy MorphTypePreference loop over
			// AlternateFormsOS — the first allomorph's type wins). Fall back to stem when there is no allomorph.
			var morphTypeGuid = FirstAllomorphMorphTypeGuid(entry);
			var initialMsaType = LcmInsertEntryDialogLauncher.MorphTypeGuidToMsaType(morphTypeGuid);

			return new AddNewSenseDialogInput
			{
				CitationForm = citation,
				Gloss = gloss,
				HelpTopic = s_helpTopic,
				PosNodes = LcmInsertEntryDialogLauncher.BuildPosNodes(cache),
				InitialMsaType = initialMsaType,
				InitialMainPosId = null,
				SlotsForPos = posId => LcmInsertEntryDialogLauncher.BuildSlots(cache, posId, morphTypeGuid),
				// Inflection-class picker (Stage 6): the selected main POS's classes, re-fed when the main POS changes.
				InflectionClassesForPos = posId => LcmInsertEntryDialogLauncher.BuildInflectionClasses(cache, posId),
				InitialInflectionClassId = null,
				// Inflection-feature editor (§19b Stage 2): the selected main POS's inflectable-feature system, re-fed
				// when the main POS changes (infl/deriv). No initial features on the create path.
				InflectionFeaturesForPos = posId => LcmInsertEntryDialogLauncher.BuildInflectionFeatures(cache, posId),
				InitialInflectionFeatures = null
			};
		}

		// The morph-type guid string of the entry's first allomorph (the legacy SetDlgInfo loop), or null.
		internal static string FirstAllomorphMorphTypeGuid(ILexEntry entry)
		{
			foreach (var mf in entry.AlternateFormsOS)
			{
				var mmt = mf.MorphTypeRA;
				if (mmt != null)
					return mmt.Guid.ToString();
			}
			// The lexeme form's morph type is a reasonable fallback (the legacy box also seeds from it via the entry).
			return entry.LexemeFormOA?.MorphTypeRA?.Guid.ToString();
		}

		protected override AddNewSenseDialogViewModel CreateViewModel(AddNewSenseDialogInput state)
		{
			_viewModel = new AddNewSenseDialogViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			_viewModel.CreateNewPosRequested += OnCreateNewPosRequested;
			// §19b Stage 3: wire the inline create-feature / add-value affordances (replacing the deferred no-op).
			_viewModel.CreateNewFeatureRequested += () =>
				LcmInflectionFeatureCreateWiring.CreateFeature(_cache, _owner, _viewModel.MsaGroupBox);
			_viewModel.CreateNewValueRequested += id =>
				LcmInflectionFeatureCreateWiring.AddValue(_cache, _owner, id, _viewModel.MsaGroupBox);
			return _viewModel;
		}

		// Runs the create-POS flow when the user clicks "Create a new Part of Speech..." in either chooser (the same
		// nested-catalog flow the Insert Entry launcher uses), then re-feeds both choosers + selects in the requester.
		private void OnCreateNewPosRequested(FwPosTarget target)
		{
			var node = LcmCreatePartOfSpeechLauncher.CreateInProject(_cache, _owner, _mediator, _propertyTable,
				_helpProvider);
			if (node == null)
				return;
			_viewModel.AcceptCreatedPos(target, node, LcmInsertEntryDialogLauncher.BuildPosNodes(_cache));
		}

		protected override AvControl CreateView(AddNewSenseDialogViewModel viewModel) =>
			new AddNewSenseDialogView { DataContext = viewModel };

		/// <summary>
		/// Applies the OK result: creates the new <c>ILexSense</c> on the entry in ONE undoable step from the
		/// view-model's snapshot (per-WS gloss alternatives + the chosen MSA) — the lift of
		/// <c>AddNewSenseDlg_Closing</c>'s OK branch (create the sense, set its gloss alternatives, assign
		/// <c>SandboxMSA</c> so the factory find-or-creates the matching MSA). Internal so the create is unit-testable
		/// against a real cache inside a UOW.
		/// </summary>
		protected override AddNewSensePayload Apply(AddNewSenseDialogInput state)
		{
			var payload = _viewModel?.Result;
			if (payload == null)
				return AddNewSensePayload.Empty;

			ILexSense newSense = null;
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateNewSense, LexTextControls.ksRedoCreateNewSense,
				_cache.ServiceLocator.GetInstance<IActionHandler>(),
				() => { newSense = CreateNewSense(payload); });
			NewSenseHvo = newSense?.Hvo ?? 0;
			return payload;
		}

		/// <summary>
		/// Creates the sense on the entry and applies the gloss + MSA from the payload — the lift of
		/// <c>AddNewSenseDlg_Closing</c>: add a new sense to the entry, set each non-empty gloss alternative (with the
		/// alternative's OWN writing-system handle, the LT-11950 fix-up), and assign <c>SandboxMSA</c> from the chosen
		/// grammatical info so the model find-or-creates the matching MSA. Internal for unit testing inside a UOW.
		/// </summary>
		internal ILexSense CreateNewSense(AddNewSensePayload payload) => CreateSense(_cache, _entry, payload);

		/// <summary>
		/// Creates the sense on <paramref name="entry"/> and applies the gloss + MSA from <paramref name="payload"/>
		/// — the lift of <c>AddNewSenseDlg_Closing</c> (add a new sense; set each non-empty gloss alternative with its
		/// own writing-system handle, the LT-11950 fix-up; assign <c>SandboxMSA</c> so the model find-or-creates the
		/// matching MSA). Internal static so the create is unit-testable against a real cache inside a UOW without the
		/// modal. The morph type (the entry's first allomorph) drives the default MSA flavor when no explicit MSA was
		/// chosen; the SandboxMsa resolution reuses the Insert Entry launcher so the mapping is identical.
		/// </summary>
		internal static ILexSense CreateSense(LcmCache cache, ILexEntry entry, AddNewSensePayload payload)
		{
			var sense = cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);

			var wsFactory = cache.WritingSystemFactory;
			foreach (var pair in payload.GlossByWs)
			{
				if (string.IsNullOrEmpty(pair.Value))
					continue;
				var ws = wsFactory.GetWsFromStr(pair.Key);
				if (ws == 0)
					ws = cache.DefaultAnalWs;
				sense.Gloss.set_String(ws, TsStringUtils.MakeString(pair.Value, ws));
			}

			var morphType = ResolveMorphType(cache, FirstAllomorphMorphTypeGuid(entry));
			sense.SandboxMSA = LcmInsertEntryDialogLauncher.BuildSandboxMsa(cache, payload.Msa, morphType);
			// Stage 6: SandboxGenericMSA carries no inflection class, so set it on the find-or-created stem MSA AFTER
			// the SandboxMSA assign (the lift of InsertEntryDlg.SetEntryMsa). Same UOW as the create.
			LcmInsertEntryDialogLauncher.ApplyInflectionClass(cache, sense, payload.Msa);
			// §19b Stage 2: rebuild the inflection IFsFeatStruc on the find-or-created infl/deriv MSA from the chosen
			// assignment set, same UOW. A stem MSA (or no features) is a no-op.
			LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(cache, sense, payload.Msa);
			return sense;
		}

		private static IMoMorphType ResolveMorphType(LcmCache cache, string guid)
		{
			var repo = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			if (!string.IsNullOrEmpty(guid) && Guid.TryParse(guid, out var g))
			{
				try { return repo.GetObject(g); }
				catch { /* fall through */ }
			}
			return null;
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
