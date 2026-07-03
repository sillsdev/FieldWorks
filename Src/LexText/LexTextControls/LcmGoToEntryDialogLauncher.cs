// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia entry-search ("go") dialog re-skinned as the New-UI
	/// replacement for the legacy plain <see cref="EntryGoDlg"/> (no starting entry, no link/merge side effect):
	/// the "Go to Lexical Entry" command (<see cref="GoLinkEntryDlgListener.OnGotoLexEntry"/>). The user searches
	/// for an entry and the dialog returns the chosen <c>ILexEntry</c>; the caller navigates to it. It REUSES the
	/// existing <see cref="EntryGoDialogViewModel"/>/<see cref="EntryGoDialogView"/> kit exactly like
	/// <see cref="LcmMergeEntryDialogLauncher"/>/<see cref="LcmAddAllomorphDialogLauncher"/>; there is no starting
	/// entry to exclude (parity with the legacy <c>dlg.SetDlgInfo(cache, null, mediator, propertyTable)</c> call)
	/// and no model mutation — this launcher only resolves the chosen entry.
	/// </summary>
	public sealed class LcmGoToEntryDialogLauncher
		: AvaloniaDialogLauncher<EntryGoDialogInput, EntryGoDialogViewModel, LcmGoToEntryDialogLauncher.GoToEntryPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private EntryGoDialogViewModel _viewModel;

		private LcmGoToEntryDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
		}

		/// <summary>The chosen entry, if any.</summary>
		public struct GoToEntryPayload
		{
			public ILexEntry Entry;
		}

		/// <summary>The entry the user chose, or null when cancelled.</summary>
		public ILexEntry SelectedEntry { get; private set; }

		/// <summary>
		/// Shows the Go-to-Lexical-Entry dialog modally over <paramref name="owner"/> and returns the chosen entry
		/// (null when cancelled). The caller navigates to it (the legacy listener's JumpToRecord).
		/// </summary>
		public static ILexEntry Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IWin32Window owner, IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var launcher = new LcmGoToEntryDialogLauncher(cache, mediator, propertyTable, helpProvider);
			var outcome = launcher.Run(owner);
			return !outcome.Accepted ? null : launcher.SelectedEntry;
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => LexTextControls.ksFindLexEntry;
		protected override bool Resizable => true;
		protected override int DialogWidth => 400;
		protected override int DialogHeight => 360;

		protected override EntryGoDialogInput BuildState() =>
			BuildInput(_cache, _mediator, _propertyTable);

		/// <summary>
		/// Builds the LCModel-free <see cref="EntryGoDialogInput"/> for the Go-to-Entry consumer: the legacy
		/// "Find Lexical Entry" title / "Lexical Entries" prompt, no excluded id (no starting entry), and a search
		/// over the shared <see cref="EntryGoSearchEngine"/> (the same matching the legacy EntryGoDlg uses).
		/// Internal so the input + search are unit-testable against a real cache without running the modal.
		/// </summary>
		internal static EntryGoDialogInput BuildInput(LcmCache cache, Mediator mediator, PropertyTable propertyTable)
		{
			return new EntryGoDialogInput
			{
				Title = LexTextControls.ksFindLexEntry,
				SearchPrompt = FwAvaloniaDialogsStrings.EntryGoResultsLabel,
				HelpTopic = "khtpFindLexicalEntry",
				Search = BuildSearch(cache, mediator, propertyTable)
			};
		}

		/// <summary>
		/// The search delegate: the shared <see cref="EntryGoLauncherShared.BuildEntrySearch"/> with NO excluded
		/// entry (the plain Go-to-Entry dialog has no starting entry to exclude). Internal so it is unit-testable.
		/// </summary>
		internal static Func<string, System.Collections.Generic.IReadOnlyList<EntryGoSearchResult>> BuildSearch(
			LcmCache cache, Mediator mediator, PropertyTable propertyTable)
		{
			return EntryGoLauncherShared.BuildEntrySearch(cache, mediator, propertyTable,
				excludedEntryHvo: 0, "AvaloniaGoToEntrySearchEngine", filter: null);
		}

		protected override EntryGoDialogViewModel CreateViewModel(EntryGoDialogInput state)
		{
			_viewModel = new EntryGoDialogViewModel(state);
			_viewModel.HelpRequested += OnHelpRequested;
			return _viewModel;
		}

		protected override AvControl CreateView(EntryGoDialogViewModel viewModel) =>
			new EntryGoDialogView { DataContext = viewModel };

		/// <summary>
		/// Applies the OK result: resolves the chosen entry and exposes it via <see cref="SelectedEntry"/>. No
		/// model mutation here — the caller navigates to the resolved entry.
		/// </summary>
		protected override GoToEntryPayload Apply(EntryGoDialogInput state)
		{
			SelectedEntry = EntryGoLauncherShared.ResolveEntry(_cache, _viewModel?.ChosenId);
			return new GoToEntryPayload { Entry = SelectedEntry };
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
