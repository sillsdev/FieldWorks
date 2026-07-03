// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia "Create New Grammatical Info." dialog — the MSA-port
	/// Stage 5 product-side replacement for the legacy <see cref="MsaCreatorDlg"/> in New-UI mode. It is a concrete
	/// <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>: the Avalonia layer stays LCModel-free by
	/// exchanging a <see cref="MsaCreatorDialogInput"/> (the read-only lexical entry + senses summary, the POS nodes /
	/// slot provider, and the box seeded from the existing <c>SandboxGenericMSA</c> / morph type) and a
	/// <see cref="MsaCreatorPayload"/> (the chosen <see cref="FwSandboxMsa"/>). This launcher builds that state from
	/// the live cache and, on OK, resolves the chosen <see cref="FwSandboxMsa"/> back into a real
	/// <c>SandboxGenericMSA</c> (via the shared <see cref="LcmInsertEntryDialogLauncher.BuildSandboxMsa"/>), exposed
	/// as <see cref="ChosenSandboxMsa"/>.
	///
	/// PARITY: the legacy dialog has two consumers with DIFFERENT apply branches — <c>MSAPopupTreeManager</c> does
	/// <c>m_sense.SandboxMSA = dlg.SandboxMSA</c> (find-or-create on a sense) and <c>MSADlgLauncher</c> does
	/// <c>originalMsa.UpdateOrReplace(dlg.SandboxMSA)</c> (modify an existing MSA). To keep ONE undoable step at the
	/// call site exactly as before, this launcher does NOT itself mutate the model: it produces the resolved
	/// <c>SandboxGenericMSA</c> and the caller applies it inside its own UOW (the same assign/update it already ran).
	/// The Show overload returns null on cancel so the caller skips its UOW, mirroring the legacy <c>DialogResult</c>.
	/// </summary>
	public sealed class LcmMsaCreatorDialogLauncher
		: AvaloniaDialogLauncher<MsaCreatorDialogInput, MsaCreatorDialogViewModel, MsaCreatorPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ILexEntry _entry;
		private readonly SandboxGenericMSA _seedMsa;
		private readonly int _hvoOriginalMsa;
		private readonly bool _useForEdit;
		private readonly string _titleForEdit;
		private MsaCreatorDialogViewModel _viewModel;
		private IWin32Window _owner;

		private LcmMsaCreatorDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ILexEntry entry, SandboxGenericMSA seedMsa, int hvoOriginalMsa,
			bool useForEdit, string titleForEdit)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_entry = entry;
			_seedMsa = seedMsa;
			_hvoOriginalMsa = hvoOriginalMsa;
			_useForEdit = useForEdit;
			_titleForEdit = titleForEdit;
		}

		/// <summary>
		/// Shows the "Create New Grammatical Info." dialog modally over <paramref name="owner"/>, seeded from
		/// <paramref name="seedMsa"/> (the legacy <c>sandboxMsa</c>). Returns the chosen <c>SandboxGenericMSA</c> on
		/// OK (the caller applies it — assign to a sense, or UpdateOrReplace an existing MSA — in its own UOW), or
		/// null on cancel. Mirrors <see cref="MsaCreatorDlg.SetDlgInfo"/>'s parameters.
		/// </summary>
		public static SandboxGenericMSA Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry entry, SandboxGenericMSA seedMsa, int hvoOriginalMsa, bool useForEdit, string titleForEdit,
			IWin32Window owner, IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			if (seedMsa == null) throw new ArgumentNullException(nameof(seedMsa));

			var launcher = new LcmMsaCreatorDialogLauncher(cache, mediator, propertyTable, helpProvider, entry,
				seedMsa, hvoOriginalMsa, useForEdit, titleForEdit);
			launcher._owner = owner;
			var outcome = launcher.Run(owner);
			return outcome.Accepted ? launcher.ChosenSandboxMsa : null;
		}

		/// <summary>
		/// Same as <see cref="Show(LcmCache, Mediator, PropertyTable, ILexEntry, SandboxGenericMSA, int, bool, string, IWin32Window, IHelpTopicProvider)"/>
		/// but also returns the LCModel-free <see cref="FwSandboxMsa"/> the box chose (§19b Stage 3): the caller can
		/// round-trip its inflection features onto the resolved MSA via
		/// <see cref="LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(LcmCache, IMoMorphSynAnalysis, FwSandboxMsa)"/>
		/// after it has applied the SandboxGenericMSA (assign-to-sense path). Null + null on cancel.
		/// </summary>
		public static SandboxGenericMSA Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry entry, SandboxGenericMSA seedMsa, int hvoOriginalMsa, bool useForEdit, string titleForEdit,
			IWin32Window owner, IHelpTopicProvider helpProvider, out FwSandboxMsa chosenBoxMsa)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			if (seedMsa == null) throw new ArgumentNullException(nameof(seedMsa));

			var launcher = new LcmMsaCreatorDialogLauncher(cache, mediator, propertyTable, helpProvider, entry,
				seedMsa, hvoOriginalMsa, useForEdit, titleForEdit);
			launcher._owner = owner;
			var outcome = launcher.Run(owner);
			chosenBoxMsa = outcome.Accepted ? launcher.ChosenBoxMsa : null;
			return outcome.Accepted ? launcher.ChosenSandboxMsa : null;
		}

		/// <summary>The chosen grammatical info resolved to a real <c>SandboxGenericMSA</c> on OK; null when cancelled.</summary>
		public SandboxGenericMSA ChosenSandboxMsa { get; private set; }

		/// <summary>The LCModel-free box payload chosen on OK (carries the chosen inflection features); null when cancelled.</summary>
		public FwSandboxMsa ChosenBoxMsa { get; private set; }

		// ----- scaffold steps -----

		// The edit context overrides the title (the legacy useForEdit branch sets Text = titleForEdit).
		protected override string DialogTitle =>
			_useForEdit && !string.IsNullOrEmpty(_titleForEdit) ? _titleForEdit : FwAvaloniaDialogsStrings.MsaCreatorTitle;
		protected override bool Resizable => true;
		// Wide enough to fit the FwMsaGroupBox's three-column affix layout (Affix Type + Attaches to Category +
		// Fills Slot / Changes to Category) without clipping the trailing column caption.
		protected override int DialogWidth => 500;
		protected override int DialogHeight => 320;

		protected override MsaCreatorDialogInput BuildState() =>
			BuildInput(_cache, _entry, _seedMsa, _hvoOriginalMsa, _useForEdit, _titleForEdit);

		/// <summary>
		/// Builds the LCModel-free <see cref="MsaCreatorDialogInput"/> from the live cache: the read-only lexical
		/// entry headword (the legacy <c>m_fwtbCitationForm</c>), the read-only senses summary (the senses whose
		/// MorphoSyntaxAnalysisRA is the original MSA — the legacy <c>m_fwtbSenses</c> loop, only on the edit path),
		/// the project POS hierarchy + per-POS slot provider (shared with the Insert Entry launcher), and the box
		/// seeded from <paramref name="seedMsa"/> (MsaType + POS/slot ids). Internal so the mapping is unit-testable.
		/// </summary>
		internal static MsaCreatorDialogInput BuildInput(LcmCache cache, ILexEntry entry, SandboxGenericMSA seedMsa,
			int hvoOriginalMsa, bool useForEdit, string titleForEdit)
		{
			// Pick the morph type that drives the slot filter (the entry's first allomorph, as the legacy box does).
			var morphTypeGuid = LcmAddNewSenseDialogLauncher.FirstAllomorphMorphTypeGuid(entry);

			return new MsaCreatorDialogInput
			{
				Title = useForEdit && !string.IsNullOrEmpty(titleForEdit)
					? titleForEdit : FwAvaloniaDialogsStrings.MsaCreatorTitle,
				LexicalEntry = entry.HeadWord?.Text,
				Senses = BuildSensesSummary(cache, entry, hvoOriginalMsa),
				HelpTopic = useForEdit ? "khtpEditGrammaticalFunction" : "khtpCreateNewGrammaticalFunction",
				PosNodes = LcmInsertEntryDialogLauncher.BuildPosNodes(cache),
				InitialMsaType = ToFwMsaType(seedMsa.MsaType),
				InitialMainPosId = seedMsa.MainPOS?.Guid.ToString(),
				InitialSecondaryPosId = seedMsa.SecondaryPOS?.Guid.ToString(),
				InitialSlotId = seedMsa.Slot?.Guid.ToString(),
				SlotsForPos = posId => LcmInsertEntryDialogLauncher.BuildSlots(cache, posId, morphTypeGuid),
				// Inflection-class picker (Stage 6): the selected main POS's classes, re-fed when the main POS changes,
				// seeded from the existing stem/deriv-step MSA's inflection class.
				InflectionClassesForPos = posId => LcmInsertEntryDialogLauncher.BuildInflectionClasses(cache, posId),
				InitialInflectionClassId = InflectionClassIdFromExistingMsa(cache, hvoOriginalMsa),
				// Inflection-feature editor (§19b Stage 2): the selected main POS's inflectable-feature system, re-fed
				// when the main POS changes (infl/deriv), seeded from the existing MSA's IFsFeatStruc (the edit path).
				InflectionFeaturesForPos = posId => LcmInsertEntryDialogLauncher.BuildInflectionFeatures(cache, posId),
				InitialInflectionFeatures = InflectionFeaturesFromExistingMsa(cache, hvoOriginalMsa)
			};
		}

		// The read-only senses summary: the short names of the senses whose MSA is the original (the legacy
		// m_fwtbSenses loop, only meaningful on the edit path where hvoOriginalMsa != 0). A comma-joined list.
		internal static string BuildSensesSummary(LcmCache cache, ILexEntry entry, int hvoOriginalMsa)
		{
			if (hvoOriginalMsa == 0)
				return null;
			var msaRepo = cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
			if (!msaRepo.TryGetObject(hvoOriginalMsa, out var original))
				return null;

			var sb = new StringBuilder();
			foreach (var sense in entry.AllSenses)
			{
				if (sense.MorphoSyntaxAnalysisRA == original)
				{
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(sense.ShortName);
				}
			}
			return sb.Length == 0 ? null : sb.ToString();
		}

		// The inflection-class id of the existing MSA being edited (the legacy InsertEntryDlg.SetMsa reads
		// InflectionClassRA off the stem / deriv-step MSA), or null on the create path / non-stem MSA. Seeds the
		// inflection-class picker so an existing class shows pre-selected.
		internal static string InflectionClassIdFromExistingMsa(LcmCache cache, int hvoOriginalMsa)
		{
			if (hvoOriginalMsa == 0)
				return null;
			var msaRepo = cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
			if (!msaRepo.TryGetObject(hvoOriginalMsa, out var original))
				return null;
			switch (original)
			{
				case IMoStemMsa stemMsa:
					return stemMsa.InflectionClassRA?.Guid.ToString();
				case IMoDerivStepMsa derivStepMsa:
					return derivStepMsa.InflectionClassRA?.Guid.ToString();
				default:
					return null;
			}
		}

		/// <summary>
		/// Reads the existing MSA's inflection-feature assignments to seed the editor (§19b Stage 2 edit path) — the
		/// flat <c>(closedFeatureId, valueId)</c> set from an <c>IMoInflAffMsa.InflFeatsOA</c> /
		/// <c>IMoDerivAffMsa.FromMsFeaturesOA</c>, via <see cref="FwFeatureStructureAdapter.ReadAssignments"/>. Empty on
		/// the create path / non-infl-deriv MSA. Internal so the read is unit-testable against a real cache.
		/// </summary>
		internal static System.Collections.Generic.IReadOnlyList<FwFeatureValueAssignment>
			InflectionFeaturesFromExistingMsa(LcmCache cache, int hvoOriginalMsa)
		{
			if (hvoOriginalMsa == 0)
				return null;
			var msaRepo = cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
			if (!msaRepo.TryGetObject(hvoOriginalMsa, out var original))
				return null;
			var fs = FwFeatureStructureAdapter.GetInflectionFeatures(original);
			return FwFeatureStructureAdapter.ReadAssignments(fs);
		}

		// Map the LCModel MsaType to the kit FwMsaType for seeding the box.
		private static FwMsaType ToFwMsaType(MsaType type)
		{
			switch (type)
			{
				case MsaType.kStem: return FwMsaType.Stem;
				case MsaType.kRoot: return FwMsaType.Root;
				case MsaType.kInfl: return FwMsaType.Inflectional;
				case MsaType.kDeriv: return FwMsaType.Derivational;
				case MsaType.kUnclassified: return FwMsaType.Unclassified;
				default: return FwMsaType.Stem;
			}
		}

		protected override MsaCreatorDialogViewModel CreateViewModel(MsaCreatorDialogInput state)
		{
			_viewModel = new MsaCreatorDialogViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			_viewModel.CreateNewPosRequested += OnCreateNewPosRequested;
			// §19b Stage 3: wire the inline create-feature / add-value affordances (replacing the deferred no-op) to
			// the shared LcmCreateFeatureLauncher; on success feed the new node back to the hosted MSA box's editor.
			_viewModel.CreateNewFeatureRequested += () =>
				LcmInflectionFeatureCreateWiring.CreateFeature(_cache, _owner, _viewModel.MsaGroupBox);
			_viewModel.CreateNewValueRequested += id =>
				LcmInflectionFeatureCreateWiring.AddValue(_cache, _owner, id, _viewModel.MsaGroupBox);
			return _viewModel;
		}

		private void OnCreateNewPosRequested(FwPosTarget target)
		{
			var node = LcmCreatePartOfSpeechLauncher.CreateInProject(_cache, _owner, _mediator, _propertyTable,
				_helpProvider);
			if (node == null)
				return;
			_viewModel.AcceptCreatedPos(target, node, LcmInsertEntryDialogLauncher.BuildPosNodes(_cache));
		}

		protected override AvControl CreateView(MsaCreatorDialogViewModel viewModel) =>
			new MsaCreatorDialogView { DataContext = viewModel };

		/// <summary>
		/// Resolves the chosen grammatical info into a real <c>SandboxGenericMSA</c> (the legacy
		/// <c>MsaCreatorDlg.SandboxMSA</c>) via the shared <see cref="LcmInsertEntryDialogLauncher.BuildSandboxMsa"/>;
		/// the caller applies it. Does NOT itself mutate the model — see the PARITY note on the class. Internal so the
		/// resolution is unit-testable against a real cache.
		/// </summary>
		protected override MsaCreatorPayload Apply(MsaCreatorDialogInput state)
		{
			var payload = _viewModel?.Result ?? MsaCreatorPayload.Empty;
			// The morph type drives the default MSA flavor when no explicit MSA was chosen (parity with the box's
			// morph-type-driven default).
			var morphTypeGuid = LcmAddNewSenseDialogLauncher.FirstAllomorphMorphTypeGuid(_entry);
			IMoMorphType morphType = null;
			if (!string.IsNullOrEmpty(morphTypeGuid) && Guid.TryParse(morphTypeGuid, out var g))
			{
				try { morphType = _cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(g); }
				catch { morphType = null; }
			}
			ChosenSandboxMsa = LcmInsertEntryDialogLauncher.BuildSandboxMsa(_cache, payload.Msa, morphType);
			ChosenBoxMsa = payload.Msa;
			// §19b Stage 3: the chosen inflection-feature set now rides back to the caller via ChosenBoxMsa, so a caller
			// that assigns the resolved SandboxGenericMSA to a definite MSA (MSAPopupTreeManager's
			// m_sense.SandboxMSA = ...) can round-trip the features onto that MSA in its own UOW (via the Show out-param
			// overload + LcmInsertEntryDialogLauncher.ApplyInflectionFeatures). PARITY: the MSADlgLauncher
			// UpdateOrReplace path is NOT round-tripped — UpdateOrReplace may REPLACE the MSA with a different instance,
			// so the post-apply feature target is ambiguous within a reasonable slice; it keeps this note. The Insert
			// Entry and Add New Sense launchers (which own their created MSA) rebuild the inflection features fully.
			// PARITY (Stage 6 inflection class): this launcher produces a SandboxGenericMSA for the CALLER to apply
			// (m_sense.SandboxMSA = ... / UpdateOrReplace) and does NOT itself mutate the model (see the class PARITY
			// note). SandboxGenericMSA carries no inflection-class field, so the chosen inflection class is SHOWN +
			// seeded + refreshed in the box here but is not round-tripped through the MsaCreator caller's apply. Fully
			// wiring it would require setting IMoStemMsa.InflectionClassRA on the resolved MSA at the call sites
			// (MSAPopupTreeManager / MSADlgLauncher), which are outside this stage's file ownership. The Insert Entry
			// and Add New Sense launchers (which own their created MSA) DO set the inflection class fully.
			return payload;
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
