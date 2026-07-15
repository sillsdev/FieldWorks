// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia chooser dialog — the Phase 1 (flat list) product-side
	/// replacement for the legacy <c>ReallySimpleListChooser</c>/<c>SimpleListChooser</c> (54 call sites). It is a
	/// concrete <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>: the Avalonia layer
	/// (FwAvaloniaDialogs) stays LCModel-free by exchanging a <see cref="ChooserDialogInput"/> (candidates as
	/// guid-keyed <see cref="RegionChoiceOption"/>s) and a <see cref="ChooserDialogResult"/> (chosen guid-string
	/// keys); this launcher builds the candidates from a possibility list and maps the chosen keys back to
	/// <c>ICmObject</c>s via the repository (an empty key =&gt; the "&lt;Empty&gt;" clear, i.e. no object).
	///
	/// <see cref="BuildInput"/>/<see cref="BuildCandidates"/>/<see cref="MapToKeys"/> are the reusable, unit-tested
	/// LCModel-to-<see cref="ChooserDialogInput"/> mapping other launchers mirror (e.g.
	/// <see cref="LcmCreatePartOfSpeechLauncher"/>); <see cref="ChosenObjects"/> exposes the repository-resolved
	/// objects for an accepted selection once a consumer runs the dialog over this launcher's instance members.
	///
	/// STAGING: no production call site exists yet — the 54 legacy chooser sites migrate in later phases, so
	/// this class ships as tested infrastructure (Build*/MapToKeys are exercised by unit tests and mirrored by
	/// the other launchers) rather than dead code; the private constructor keeps it unreachable until wired.
	/// </summary>
	public sealed class LcmChooserDialogLauncher
		: AvaloniaDialogLauncher<ChooserDialogInput, ChooserDialogViewModel, LcmChooserDialogLauncher.ChooserPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ICmPossibilityList _list;
		private readonly ChooserSelectionMode _mode;
		private readonly IEnumerable<ICmObject> _current;
		private readonly bool _allowEmpty;
		private readonly bool _hierarchical;
		private readonly string _prompt;
		private readonly string _helpTopic;

		private LcmChooserDialogLauncher(ICmPossibilityList list, ChooserSelectionMode mode,
			IEnumerable<ICmObject> current, bool allowEmpty, LcmCache cache, Mediator mediator,
			PropertyTable propertyTable, IHelpTopicProvider helpProvider, string prompt, string helpTopic,
			bool hierarchical)
		{
			_list = list;
			_mode = mode;
			_current = current;
			_allowEmpty = allowEmpty;
			_hierarchical = hierarchical;
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_prompt = prompt;
			_helpTopic = helpTopic;
		}

		/// <summary>The follow-up signals from an accepted chooser: the chosen keys and the objects they resolve to.</summary>
		public struct ChooserPayload
		{
			public IReadOnlyList<string> ChosenKeys;
			public IReadOnlyList<ICmObject> ChosenObjects;
		}

		/// <summary>The objects the accepted selection resolved to (empty when cancelled or only "&lt;Empty&gt;" chosen).</summary>
		public IReadOnlyList<ICmObject> ChosenObjects { get; private set; } = new List<ICmObject>();

		// ----- scaffold steps -----

		protected override string DialogTitle => _prompt ?? string.Empty;
		protected override bool Resizable => true;
		protected override int DialogWidth => 360;
		protected override int DialogHeight => 420;

		protected override ChooserDialogInput BuildState() =>
			BuildInput(_list, _mode, _current, _allowEmpty, _prompt, _helpTopic, _hierarchical);

		/// <summary>
		/// Builds the LCModel-free <see cref="ChooserDialogInput"/> from the live domain inputs (candidates from the
		/// list, the initial selection from the current objects, and the simple Phase 1 "require a selection unless
		/// empty is allowed" OK rule). Internal + static so the full state mapping is unit-testable against a real
		/// list without running the modal (mirrors AvaloniaOptionsDialogLauncher.BuildState).
		/// </summary>
		internal static ChooserDialogInput BuildInput(ICmPossibilityList list, ChooserSelectionMode mode,
			IEnumerable<ICmObject> current, bool allowEmpty, string prompt, string helpTopic,
			bool hierarchical = false)
		{
			return new ChooserDialogInput
			{
				Candidates = BuildCandidates(list),
				SelectionMode = mode,
				InitialSelectedKeys = MapToKeys(current),
				AllowEmpty = allowEmpty,
				Hierarchical = hierarchical,
				Prompt = prompt,
				HelpTopic = helpTopic,
				// A single-select atomic field that can be cleared does not force a selection; everything else
				// (and any multi-select that cannot be empty) forbids an empty OK. Phase 1 keeps the simple rule:
				// require a selection unless the field explicitly allows empty.
				ForbidEmptySelection = !allowEmpty
			};
		}

		protected override ChooserDialogViewModel CreateViewModel(ChooserDialogInput state)
		{
			// Capture the VM so Apply (which the scaffold hands only the STATE) can read the chosen keys it
			// snapshotted on OK. Wire Help through the supplied provider (P3 will flesh out goto/help).
			_viewModel = new ChooserDialogViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			return _viewModel;
		}

		protected override AvControl CreateView(ChooserDialogViewModel viewModel) =>
			new ChooserDialogView { DataContext = viewModel };

		protected override ChooserPayload Apply(ChooserDialogInput state)
		{
			// Run executes Apply on the OK path only, AFTER the VM's ApplyChanges has snapshotted ChosenKeys.
			var keys = _viewModel?.ChosenKeys ?? new List<string>();
			var objects = ResolveObjects(keys);
			return new ChooserPayload { ChosenKeys = keys, ChosenObjects = objects };
		}

		// Captured at CreateViewModel time so Apply can read the chosen keys off the VM.
		private ChooserDialogViewModel _viewModel;

		private void OnHelpRequested(string topic)
		{
			// Phase 1: surface the request through the supplied provider when present; full help wiring is P3.
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}

		// ----- candidate building (mirrors RegionValueFactory.BuildPossibilityOptions) -----

		/// <summary>
		/// Walks the possibility list's tree in document order (parent before children) into flat chooser
		/// candidates, carrying hierarchy as <see cref="RegionChoiceOption.Depth"/> (rendered as indentation, not a
		/// tree, in Phase 1). The display name uses the composer's fallback rule — Name.BestAnalysisAlternative,
		/// then ShortName, then the guid — and the key is the item's guid string. Internal so it is unit-testable
		/// against a real list without running the modal.
		/// </summary>
		internal static IReadOnlyList<RegionChoiceOption> BuildCandidates(ICmPossibilityList list)
		{
			var options = new List<RegionChoiceOption>();
			void Add(ICmPossibility possibility, int depth)
			{
				options.Add(new RegionChoiceOption(possibility.Guid.ToString(), DisplayName(possibility), depth));
				foreach (var sub in possibility.SubPossibilitiesOS)
					Add(sub, depth + 1);
			}

			foreach (var possibility in list.PossibilitiesOS)
				Add(possibility, 0);
			return options;
		}

		private static string DisplayName(ICmPossibility possibility) =>
			possibility.Name.BestAnalysisAlternative?.Text ?? possibility.ShortName ?? possibility.Guid.ToString();

		/// <summary>Maps the current objects to their guid-string keys (the initial selection).</summary>
		internal static IReadOnlyList<string> MapToKeys(IEnumerable<ICmObject> current) =>
			(current ?? Enumerable.Empty<ICmObject>())
				.Where(o => o != null)
				.Select(o => o.Guid.ToString())
				.ToList();

		/// <summary>
		/// Resolves chosen guid-string keys back to objects through the repository, dropping the "&lt;Empty&gt;"
		/// key (the empty string) and any key that no longer resolves. Order follows the chosen-key order.
		/// </summary>
		private IReadOnlyList<ICmObject> ResolveObjects(IReadOnlyList<string> keys)
		{
			var objects = new List<ICmObject>();
			if (keys == null || _cache == null)
				return objects;
			var repo = _cache.ServiceLocator.ObjectRepository;
			foreach (var key in keys)
			{
				if (string.IsNullOrEmpty(key))
					continue; // the "<Empty>" clear row resolves to no object
				if (Guid.TryParse(key, out var guid) && repo.TryGetObject(guid, out var obj))
					objects.Add(obj);
			}
			return objects;
		}
	}
}
