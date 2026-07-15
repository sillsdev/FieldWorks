// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
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
	/// replacement for the legacy <see cref="LinkAllomorphDlg"/> (an <c>EntryGoDlg</c> child): the user searches for
	/// an entry and the dialog returns one of that entry's allomorphs (an <c>IMoForm</c>). It REUSES the existing
	/// <see cref="EntryGoDialogViewModel"/>/<see cref="EntryGoDialogView"/> kit exactly like
	/// <see cref="LcmMergeEntryDialogLauncher"/>; only the title/OK/prompt text, the search filter, and the on-OK
	/// resolution differ.
	///
	/// The on-OK action of the legacy dialog (adding the chosen allomorph to the ad-hoc co-prohibition) is performed
	/// by the CALLER (the reference launcher's <c>AddItem</c>), so this launcher only resolves the chosen entry to an
	/// <c>IMoForm</c> (exposed via <see cref="SelectedObject"/>); it performs no model mutation of its own.
	///
	/// The search applies the legacy <c>LinkAllomorphDlg.FilterLexEntry</c> rule (drop entries whose forms are ALL
	/// abstract, since an abstract-only entry has no concrete allomorph to choose).
	///
	/// PARITY: the legacy dialog shows a combo of the chosen entry's NON-ABSTRACT forms (lexeme form first, then
	/// alternates) and defaults to the first; this slice resolves to that same first form. Picking a non-first
	/// allomorph from a multi-allomorph entry is deferred (the kit result list carries one row per entry) — see the
	/// // PARITY comment on <see cref="ResolveSelectedAllomorph"/>.
	/// </summary>
	public sealed class LcmLinkAllomorphDialogLauncher
		: AvaloniaDialogLauncher<EntryGoDialogInput, EntryGoDialogViewModel, LcmLinkAllomorphDialogLauncher.LinkAllomorphPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ILexEntry _startingEntry;
		private EntryGoDialogViewModel _viewModel;

		private LcmLinkAllomorphDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ILexEntry startingEntry)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_startingEntry = startingEntry;
		}

		/// <summary>The chosen allomorph, if any.</summary>
		public struct LinkAllomorphPayload
		{
			public IMoForm Allomorph;
		}

		/// <summary>The allomorph the user chose (the first non-abstract form of the chosen entry), or null.</summary>
		public IMoForm SelectedObject { get; private set; }

		/// <summary>
		/// Shows the Choose-Allomorph dialog modally over <paramref name="owner"/> and returns the chosen allomorph
		/// (null when cancelled). The caller performs the on-OK action (e.g. the reference launcher's
		/// <c>AddItem</c>).
		/// </summary>
		public static IMoForm Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry startingEntry, IWin32Window owner, IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var launcher = new LcmLinkAllomorphDialogLauncher(cache, mediator, propertyTable, helpProvider, startingEntry);
			var outcome = launcher.Run(owner);
			return !outcome.Accepted ? null : launcher.SelectedObject;
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.LinkAllomorphTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 400;
		protected override int DialogHeight => 360;

		protected override EntryGoDialogInput BuildState() =>
			BuildInput(_cache, _mediator, _propertyTable, _startingEntry);

		/// <summary>
		/// Builds the LCModel-free <see cref="EntryGoDialogInput"/> for the Link-Allomorph consumer. Internal so the
		/// input + search are unit-testable against a real cache without running the modal.
		/// </summary>
		internal static EntryGoDialogInput BuildInput(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry startingEntry)
		{
			return new EntryGoDialogInput
			{
				Title = FwAvaloniaDialogsStrings.LinkAllomorphTitle,
				SearchPrompt = FwAvaloniaDialogsStrings.EntryGoResultsLabel,
				ExcludedId = startingEntry?.Hvo.ToString(System.Globalization.CultureInfo.InvariantCulture),
				HelpTopic = "hktpInsertAllomorphChooseAllomorph",
				Search = BuildSearch(cache, mediator, propertyTable, startingEntry)
			};
		}

		/// <summary>
		/// The search delegate: the shared <see cref="EntryGoLauncherShared.BuildEntrySearch"/> with the legacy
		/// <c>LinkAllomorphDlg.FilterLexEntry</c> rule applied — entries whose forms are ALL abstract are dropped
		/// (they have no concrete allomorph to choose). Internal so it is unit-testable against a real cache.
		/// </summary>
		internal static Func<string, IReadOnlyList<EntryGoSearchResult>> BuildSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable, ILexEntry startingEntry)
		{
			return EntryGoLauncherShared.BuildEntrySearch(cache, mediator, propertyTable,
				startingEntry?.Hvo ?? 0, "AvaloniaLinkAllomorphSearchEngine", HasConcreteAllomorph);
		}

		/// <summary>
		/// The legacy <c>LinkAllomorphDlg.FilterLexEntry</c> rule, inverted to a keep-predicate: keep the entry only
		/// when it has at least one NON-abstract form (lexeme form or alternate). Internal so it is unit-testable.
		/// </summary>
		internal static bool HasConcreteAllomorph(ILexEntry entry)
		{
			if (entry == null)
				return false;
			var lf = entry.LexemeFormOA;
			if (lf != null && !lf.IsAbstract)
				return true;
			return entry.AlternateFormsOS.Any(allo => !allo.IsAbstract);
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
		/// Applies the OK result: resolves the chosen entry to its (first non-abstract) allomorph and exposes it via
		/// <see cref="SelectedObject"/>. No model mutation here — the caller performs the on-OK action.
		/// </summary>
		protected override LinkAllomorphPayload Apply(EntryGoDialogInput state)
		{
			SelectedObject = ResolveSelectedAllomorph(_cache, _viewModel?.ChosenId);
			return new LinkAllomorphPayload { Allomorph = SelectedObject };
		}

		/// <summary>
		/// Resolves the chosen entry's id to an allomorph. PARITY: the legacy LinkAllomorphDlg builds a combo of the
		/// entry's NON-abstract forms — the lexeme form first (if not abstract), then the non-abstract alternates —
		/// and default-selects the FIRST item; this slice returns that same first form. Choosing a non-first
		/// allomorph is deferred (the kit's one-row-per-entry result list cannot carry per-allomorph rows cleanly).
		/// Internal so it is unit-testable against a real cache.
		/// </summary>
		internal static IMoForm ResolveSelectedAllomorph(LcmCache cache, string entryId)
		{
			var entry = EntryGoLauncherShared.ResolveEntry(cache, entryId);
			if (entry == null)
				return null;
			var lf = entry.LexemeFormOA;
			if (lf != null && !lf.IsAbstract)
				return lf;
			return entry.AlternateFormsOS.FirstOrDefault(allo => !allo.IsAbstract);
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
