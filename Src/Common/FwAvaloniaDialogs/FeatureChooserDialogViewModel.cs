// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the standalone feature-structure chooser dialog (Phase-1 §19b Stage 3) — the Avalonia
	/// replacement for the WinForms <c>MsaInflectionFeatureListDlg</c> (assign inflection feature values to an MSA's
	/// <c>IFsFeatStruc</c>) and <c>PhonologicalFeatureChooserDlg</c> (the phonological feature system). The dialog is
	/// essentially the shared LCModel-free <see cref="FwFeatureStructureEditor"/> hosted over OK/Cancel/Help: the
	/// host feeds the feature system + current assignments, the user picks values, and on OK
	/// <see cref="ApplyChanges"/> snapshots the chosen assignment set into <see cref="Result"/>.
	///
	/// Like the legacy dialogs there is NO OK gate (an empty assignment set — every feature "&lt;None&gt;" — is the
	/// valid "delete the FS / unspecified" outcome). The dialog forwards the editor's inline
	/// <see cref="CreateNewFeatureRequested"/> / <see cref="CreateNewValueRequested"/> affordances to the host
	/// (the launcher opens the create flow and calls <see cref="AcceptCreatedFeature"/> /
	/// <see cref="AcceptCreatedValue"/>); the VM itself performs NO create (it stays LCModel-free).
	/// </summary>
	public partial class FeatureChooserDialogViewModel : DialogViewModelBase
	{
		private readonly FeatureChooserDialogInput _input;

		public FeatureChooserDialogViewModel() : this(new FeatureChooserDialogInput())
		{
		}

		public FeatureChooserDialogViewModel(FeatureChooserDialogInput input)
		{
			_input = input ?? new FeatureChooserDialogInput();

			Title = _input.Title ?? FwAvaloniaDialogsStrings.InflectionFeatureChooserTitle;
			Prompt = _input.Prompt ?? string.Empty;
			HasPrompt = !string.IsNullOrEmpty(Prompt);
			HelpTopic = _input.HelpTopic;
			HasHelp = !string.IsNullOrEmpty(_input.HelpTopic);

			// The owned feature editor: feed the feature system, then seed the current assignments silently (the
			// host's initial-load path — SetAssignments does not raise the change event).
			Editor = new FwFeatureStructureEditor(string.IsNullOrEmpty(_input.AutomationId) ? "Features" : _input.AutomationId);
			Editor.SetNodes(_input.Nodes ?? Array.Empty<FwFeatureNode>());
			Editor.SetAssignments(_input.InitialAssignments ?? Array.Empty<FwFeatureValueAssignment>());

			// Forward the editor's create-feature / create-value affordances to the host (the launcher wires them).
			Editor.CreateNewFeatureRequested += () => CreateNewFeatureRequested?.Invoke();
			Editor.CreateNewValueRequested += id => CreateNewValueRequested?.Invoke(id);
		}

		/// <summary>The dialog window title.</summary>
		public string Title { get; }

		/// <summary>The instruction prompt shown above the feature tree (may be empty).</summary>
		public string Prompt { get; }

		/// <summary>True when there is a non-empty <see cref="Prompt"/> to show.</summary>
		public bool HasPrompt { get; }

		/// <summary>The owned LCModel-free feature-structure editor the view mounts.</summary>
		public FwFeatureStructureEditor Editor { get; }

		/// <summary>The help topic id carried for the Help button.</summary>
		public string HelpTopic { get; }

		/// <summary>True when a <see cref="HelpTopic"/> is present, so the Help button shows.</summary>
		public bool HasHelp { get; }

		/// <summary>The snapshot written on OK (the chosen assignment set). Null until OK runs <see cref="ApplyChanges"/>.</summary>
		public FeatureChooserPayload Result { get; private set; }

		// ----- Help -----

		/// <summary>Raised when the user clicks Help, carrying the <see cref="HelpTopic"/>.</summary>
		public event Action<string> HelpRequested;

		/// <summary>Fires <see cref="HelpRequested"/> with the carried <see cref="HelpTopic"/> (no-op if unsubscribed).</summary>
		[RelayCommand]
		private void Help() => HelpRequested?.Invoke(HelpTopic);

		// ----- create-feature / create-value wiring (mirrors the MSA box forward) -----

		/// <summary>
		/// Raised when the user clicks the inline "Create a new feature..." row in the hosted editor. The host
		/// (the LCModel-aware launcher) opens its create-feature flow and calls <see cref="AcceptCreatedFeature"/>.
		/// </summary>
		public event Action CreateNewFeatureRequested;

		/// <summary>
		/// Raised when the user invokes a closed feature's "Add a value..." affordance, carrying the closed feature's
		/// id. The host opens its add-value flow and calls <see cref="AcceptCreatedValue"/>.
		/// </summary>
		public event Action<string> CreateNewValueRequested;

		/// <summary>
		/// Host callback after a successful create-feature flow: appends the new feature (+ its supplied value
		/// children) to the editor's source list and rebuilds the tree, preserving the current picks.
		/// </summary>
		public void AcceptCreatedFeature(FwFeatureNode created, IReadOnlyList<FwFeatureNode> valueChildren = null)
			=> Editor.AcceptCreatedFeature(created, valueChildren);

		/// <summary>
		/// Host callback after a successful add-value flow: appends the new value under its closed feature and selects
		/// it (so the just-created value becomes the feature's pick), matching the WinForms add-then-assign flow.
		/// </summary>
		public void AcceptCreatedValue(string closedFeatureId, FwFeatureNode createdValue)
			=> Editor.AcceptCreatedValue(closedFeatureId, createdValue);

		// ----- OK (no gate, like the legacy dialogs; empty == unspecified) -----

		/// <summary>Snapshots the chosen feature assignment set from the editor into <see cref="Result"/> on OK.</summary>
		protected override void ApplyChanges()
		{
			Result = new FeatureChooserPayload(Editor?.Assignments);
		}
	}
}
