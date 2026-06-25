// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the standalone PHONOLOGICAL-feature chooser dialog (Phase-1 §19b Stage 3) — the
	/// New-UI replacement for the WinForms <see cref="PhonologicalFeatureChooserDlg"/> the phonological-features slice
	/// opens. It bridges LCModel ↔ the LCModel-free <see cref="FeatureChooserDialogViewModel"/>: it builds the
	/// PHONOLOGICAL feature system (every closed feature in <c>PhFeatureSystemOA</c> + its values, the flat case —
	/// <see cref="FwFeatureStructureAdapter.BuildPhonologicalNodes"/>) + the current assignment set, shows the chooser,
	/// and on OK rebuilds the <c>IFsFeatStruc</c> from the chosen assignments in ONE undoable step.
	///
	/// Unlike the inflection chooser there is NO empty-FS delete (the legacy <c>PhonologicalFeatureChooserDlg_Closing</c>
	/// leaves an emptied FS as-is). The inline create-feature affordance is wired to <see cref="LcmCreateFeatureLauncher"/>
	/// targeting the phonological feature system (a new closed feature + its default +/- values, the
	/// MasterPhonologicalFeatureListDlg parity).
	///
	/// PARITY (§19b Stage 3): the legacy dialog can also drive phonological-RULE feature CONSTRAINTS (agree/disagree
	/// polarity over <c>IPhFeatureConstraint</c>), used only from the rule-formula control. That polarity surface is
	/// NOT ported here (the New-UI gate covers the slice's plain feature-assignment case); the rule-formula call site
	/// keeps the legacy dialog. The plain value-assignment case (the phoneme / NC-features slice) is fully wired.
	/// </summary>
	public sealed class LcmPhonologicalFeatureChooserLauncher
		: AvaloniaDialogLauncher<FeatureChooserDialogInput, FeatureChooserDialogViewModel, FeatureChooserPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ICmObject _owner;
		private readonly int _owningFlid;
		private IFsFeatStruc _fs;
		private IReadOnlyList<FwFeatureNode> _nodes;
		private FeatureChooserDialogViewModel _viewModel;
		private IWin32Window _ownerWindow;

		private LcmPhonologicalFeatureChooserLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, IFsFeatStruc fs, ICmObject owner, int owningFlid)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_fs = fs;
			_owner = owner;
			_owningFlid = owningFlid;
		}

		/// <summary>
		/// Shows the phonological-feature chooser modally over <paramref name="ownerWindow"/>, editing
		/// <paramref name="fs"/> (an existing <c>IFsFeatStruc</c>, or null to create one on
		/// <paramref name="owner"/>.<paramref name="owningFlid"/>). On OK rebuilds the FS in one undoable step and
		/// returns it; returns null on cancel (the caller leaves the FS unchanged). Mirrors
		/// <see cref="PhonologicalFeatureChooserDlg.SetDlgInfo"/>'s owner parameters.
		/// </summary>
		public static IFsFeatStruc Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IFsFeatStruc fs, ICmObject owner, int owningFlid, IWin32Window ownerWindow,
			IHelpTopicProvider helpProvider, out bool accepted)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			var launcher = new LcmPhonologicalFeatureChooserLauncher(cache, mediator, propertyTable, helpProvider, fs,
				owner, owningFlid);
			launcher._ownerWindow = ownerWindow;
			var outcome = launcher.Run(ownerWindow);
			accepted = outcome.Accepted;
			return outcome.Accepted ? launcher.ResultFs : fs;
		}

		/// <summary>The resulting FS after an accepted OK; the pre-existing FS otherwise.</summary>
		public IFsFeatStruc ResultFs { get; private set; }

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.PhonologicalFeatureChooserTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 360;
		protected override int DialogHeight => 460;

		protected override FeatureChooserDialogInput BuildState() => BuildInput(_cache, _fs);

		/// <summary>
		/// Builds the LCModel-free <see cref="FeatureChooserDialogInput"/> for the phonological feature system: the
		/// flat node list (<see cref="FwFeatureStructureAdapter.BuildPhonologicalNodes"/>) and the current assignment
		/// set read from <paramref name="fs"/>. Internal so the mapping is unit-testable against a real cache.
		/// </summary>
		internal FeatureChooserDialogInput BuildInput(LcmCache cache, IFsFeatStruc fs)
		{
			_nodes = FwFeatureStructureAdapter.BuildPhonologicalNodes(cache);
			return new FeatureChooserDialogInput
			{
				Title = FwAvaloniaDialogsStrings.PhonologicalFeatureChooserTitle,
				Prompt = FwAvaloniaDialogsStrings.PhonologicalFeatureChooserPrompt,
				AutomationId = "PhonFeatures",
				Nodes = _nodes,
				InitialAssignments = FwFeatureStructureAdapter.ReadAssignments(fs),
				HelpTopic = "khtpToolPhonologicalFeaturesChooser"
			};
		}

		protected override FeatureChooserDialogViewModel CreateViewModel(FeatureChooserDialogInput state)
		{
			_viewModel = new FeatureChooserDialogViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			_viewModel.CreateNewFeatureRequested += OnCreateNewFeatureRequested;
			_viewModel.CreateNewValueRequested += OnCreateNewValueRequested;
			return _viewModel;
		}

		private void OnCreateNewFeatureRequested()
		{
			var node = LcmCreateFeatureLauncher.CreateFeature(_cache, FeatureSystemKind.Phonological, _ownerWindow,
				out var children);
			if (node == null)
				return;
			_viewModel.AcceptCreatedFeature(node, children);
		}

		private void OnCreateNewValueRequested(string closedFeatureId)
		{
			var node = LcmCreateFeatureLauncher.AddValue(_cache, FeatureSystemKind.Phonological, closedFeatureId,
				_ownerWindow);
			if (node == null)
				return;
			_viewModel.AcceptCreatedValue(closedFeatureId, node);
		}

		protected override AvControl CreateView(FeatureChooserDialogViewModel viewModel) =>
			new FeatureChooserDialogView { DataContext = viewModel };

		/// <summary>
		/// Rebuilds the phonological <c>IFsFeatStruc</c> from the chosen assignment set in ONE undoable step — the
		/// parity of <c>PhonologicalFeatureChooserDlg_Closing</c>'s OK branch. Creates the FS on the owner when needed;
		/// clears + rebuilds it. NO empty-FS delete (the legacy phonological dialog leaves an emptied FS as-is).
		/// </summary>
		protected override FeatureChooserPayload Apply(FeatureChooserDialogInput state)
		{
			var payload = _viewModel?.Result ?? FeatureChooserPayload.Empty;
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoInsertInflectionFeature,
				LexTextControls.ksRedoInsertInflectionFeature, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				ResultFs = FwFeatureStructureAdapter.ApplyFeaturesToOwner(_cache, _fs, _owner, _owningFlid,
					_nodes, payload.Assignments, deleteWhenEmpty: false);
			});
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
