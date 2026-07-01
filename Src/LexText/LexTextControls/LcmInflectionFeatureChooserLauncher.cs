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
	/// The LCModel-aware launcher for the standalone INFLECTION-feature chooser dialog (Phase-1 §19b Stage 3) — the
	/// New-UI replacement for the WinForms <see cref="MsaInflectionFeatureListDlg"/> the inflection-features slice
	/// opens. It is a concrete <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/> that bridges LCModel ↔
	/// the LCModel-free <see cref="FeatureChooserDialogViewModel"/>: it builds the POS's inflectable-feature system
	/// (<see cref="FwFeatureStructureAdapter.BuildNodes"/>, the lift of <c>PopulateTreeFromPos</c>) + the current
	/// assignment set (read from the existing <c>IFsFeatStruc</c>), shows the chooser, and on OK rebuilds the
	/// <c>IFsFeatStruc</c> from the chosen assignments in ONE undoable step — including the LT-13596 empty-FS delete
	/// when nothing is chosen. The inline create-feature / add-value affordances are wired to
	/// <see cref="LcmCreateFeatureLauncher"/> (the New-UI parity of the WinForms MasterInflectionFeatureListDlg link),
	/// each running its own undoable step then feeding the new node back to the editor.
	///
	/// Layering mirrors <see cref="LcmMsaCreatorDialogLauncher"/>: <see cref="BuildInput"/> is internal so the
	/// node/assignment mapping is unit-testable against a real cache; the modal loop is desktop-only (covered by the
	/// headless FeatureChooserDialogTests).
	/// </summary>
	public sealed class LcmInflectionFeatureChooserLauncher
		: AvaloniaDialogLauncher<FeatureChooserDialogInput, FeatureChooserDialogViewModel, FeatureChooserPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly IPartOfSpeech _pos;
		private readonly ICmObject _owner;
		private readonly int _owningFlid;
		private IFsFeatStruc _fs;
		private IReadOnlyList<FwFeatureNode> _nodes;
		private FeatureChooserDialogViewModel _viewModel;
		private IWin32Window _ownerWindow;

		private LcmInflectionFeatureChooserLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, IPartOfSpeech pos, IFsFeatStruc fs, ICmObject owner, int owningFlid)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_pos = pos;
			_fs = fs;
			_owner = owner;
			_owningFlid = owningFlid;
		}

		/// <summary>
		/// Shows the inflection-feature chooser modally over <paramref name="ownerWindow"/> for a part of speech's
		/// inflectable features, editing <paramref name="fs"/> (an existing <c>IFsFeatStruc</c>, or null to create one
		/// on <paramref name="owner"/>.<paramref name="owningFlid"/>). On OK rebuilds the FS in one undoable step and
		/// returns it (null when the user emptied it — LT-13596). Returns null on cancel (the caller leaves the FS
		/// unchanged). Mirrors <see cref="MsaInflectionFeatureListDlg.SetDlgInfo"/>'s POS/owner parameters.
		/// </summary>
		public static IFsFeatStruc Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IPartOfSpeech pos, IFsFeatStruc fs, ICmObject owner, int owningFlid, IWin32Window ownerWindow,
			IHelpTopicProvider helpProvider, out bool accepted)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			var launcher = new LcmInflectionFeatureChooserLauncher(cache, mediator, propertyTable, helpProvider, pos,
				fs, owner, owningFlid);
			launcher._ownerWindow = ownerWindow;
			var outcome = launcher.Run(ownerWindow);
			accepted = outcome.Accepted;
			return outcome.Accepted ? launcher.ResultFs : fs;
		}

		/// <summary>
		/// Convenience overload for the standalone inflection-feature slice (the WinForms
		/// <c>MsaInflectionFeatureListDlgLauncher</c> path): derives the POS from the owning object + flid (the lift of
		/// <c>GetPosFromCmObjectAndFlid</c>) and edits the FS owned at <paramref name="owner"/>.<paramref name="owningFlid"/>
		/// (or an existing <paramref name="fs"/>). Returns the resulting FS (null when emptied — LT-13596) on OK, or the
		/// pre-existing FS on cancel.
		/// </summary>
		public static IFsFeatStruc ShowForOwner(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IFsFeatStruc fs, ICmObject owner, int owningFlid, IWin32Window ownerWindow, IHelpTopicProvider helpProvider,
			out bool accepted)
		{
			var pos = FwFeatureStructureAdapter.GetInflectionFeaturePos(owner, owningFlid);
			return Show(cache, mediator, propertyTable, pos, fs, owner, owningFlid, ownerWindow, helpProvider,
				out accepted);
		}

		/// <summary>The resulting FS after an accepted OK (null when emptied/deleted); the pre-existing FS otherwise.</summary>
		public IFsFeatStruc ResultFs { get; private set; }

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.InflectionFeatureChooserTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 360;
		protected override int DialogHeight => 460;

		protected override FeatureChooserDialogInput BuildState() => BuildInput(_cache, _pos, _fs);

		/// <summary>
		/// Builds the LCModel-free <see cref="FeatureChooserDialogInput"/> for a POS's inflectable features: the
		/// depth-tagged feature system (<see cref="FwFeatureStructureAdapter.BuildNodes"/>) and the current assignment
		/// set read from <paramref name="fs"/> (<see cref="FwFeatureStructureAdapter.ReadAssignments"/>). Internal so
		/// the mapping is unit-testable against a real cache without running the modal.
		/// </summary>
		internal FeatureChooserDialogInput BuildInput(LcmCache cache, IPartOfSpeech pos, IFsFeatStruc fs)
		{
			_nodes = FwFeatureStructureAdapter.BuildNodes(pos);
			return new FeatureChooserDialogInput
			{
				Title = FwAvaloniaDialogsStrings.InflectionFeatureChooserTitle,
				Prompt = FwAvaloniaDialogsStrings.InflectionFeatureChooserPrompt,
				AutomationId = "InflFeatures",
				Nodes = _nodes,
				InitialAssignments = FwFeatureStructureAdapter.ReadAssignments(fs),
				HelpTopic = "khtpInflectionFeatureChooser"
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

		// Run the create-feature flow (the New-UI parity of the MasterInflectionFeatureListDlg link), then re-feed the
		// rebuilt node system + add the new feature to the editor.
		private void OnCreateNewFeatureRequested()
		{
			var node = LcmCreateFeatureLauncher.CreateFeature(_cache, FeatureSystemKind.Inflection, _ownerWindow,
				out var children);
			if (node == null)
				return;
			_viewModel.AcceptCreatedFeature(node, children);
		}

		// Run the add-value flow for the given closed feature, then add + select the new value in the editor.
		private void OnCreateNewValueRequested(string closedFeatureId)
		{
			var node = LcmCreateFeatureLauncher.AddValue(_cache, FeatureSystemKind.Inflection, closedFeatureId,
				_ownerWindow);
			if (node == null)
				return;
			_viewModel.AcceptCreatedValue(closedFeatureId, node);
		}

		protected override AvControl CreateView(FeatureChooserDialogViewModel viewModel) =>
			new FeatureChooserDialogView { DataContext = viewModel };

		/// <summary>
		/// Rebuilds the inflection <c>IFsFeatStruc</c> from the chosen assignment set in ONE undoable step — the
		/// create-side parity of <c>MsaInflectionFeatureListDlg_Closing</c>. Creates the FS on the owner when there are
		/// assignments and none exists; clears + rebuilds it; deletes it (LT-13596) when the set is empty. The node
		/// system the editor used (captured in BuildInput) recovers the complex-feature ancestry.
		/// </summary>
		protected override FeatureChooserPayload Apply(FeatureChooserDialogInput state)
		{
			var payload = _viewModel?.Result ?? FeatureChooserPayload.Empty;
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoInsertInflectionFeature,
				LexTextControls.ksRedoInsertInflectionFeature, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				ResultFs = FwFeatureStructureAdapter.ApplyFeaturesToOwner(_cache, _fs, _owner, _owningFlid,
					_nodes, payload.Assignments, deleteWhenEmpty: true);
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
