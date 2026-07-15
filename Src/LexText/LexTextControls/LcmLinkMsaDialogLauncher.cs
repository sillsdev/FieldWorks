// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia entry-search ("go") dialog re-skinned as the New-UI
	/// replacement for the legacy <see cref="LinkMSADlg"/> (an <c>EntryGoDlg</c> child): the user searches for an
	/// entry and the dialog returns one of that entry's grammatical-info / MSA objects (an
	/// <c>IMoMorphSynAnalysis</c>). It REUSES the existing <see cref="EntryGoDialogViewModel"/>/
	/// <see cref="EntryGoDialogView"/> kit exactly like <see cref="LcmMergeEntryDialogLauncher"/>; only the
	/// title/OK/prompt text, the search filter, and the on-OK resolution differ.
	///
	/// The on-OK action of the legacy dialog (adding the chosen MSA to the ad-hoc co-prohibition) is performed by
	/// the CALLER (the reference launcher's <c>AddItem</c>), so this launcher's job is only to resolve the chosen
	/// entry to an <c>IMoMorphSynAnalysis</c> (exposed via <see cref="SelectedObject"/>); it performs no model
	/// mutation of its own.
	///
	/// PARITY: the legacy dialog shows a combo of ALL of the chosen entry's MSAs and defaults to the first
	/// (<c>m_fwcbFunctions.SelectedItem = Items[0]</c>); this slice resolves to that same first MSA. Picking a
	/// non-first MSA from a multi-MSA entry is deferred (the kit result list carries one row per entry, not per
	/// MSA) — see the // PARITY comment on <see cref="ResolveSelectedMsa"/>.
	/// </summary>
	public sealed class LcmLinkMsaDialogLauncher
		: AvaloniaDialogLauncher<EntryGoDialogInput, EntryGoDialogViewModel, LcmLinkMsaDialogLauncher.LinkMsaPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ILexEntry _startingEntry;
		private EntryGoDialogViewModel _viewModel;

		private LcmLinkMsaDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ILexEntry startingEntry)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_startingEntry = startingEntry;
		}

		/// <summary>The chosen grammatical-info / MSA object, if any.</summary>
		public struct LinkMsaPayload
		{
			public IMoMorphSynAnalysis Msa;
		}

		/// <summary>The MSA the user chose (the first MSA of the chosen entry), or null when cancelled.</summary>
		public IMoMorphSynAnalysis SelectedObject { get; private set; }

		/// <summary>
		/// Shows the Choose-Morpheme-and-Grammatical-Info dialog modally over <paramref name="owner"/> and returns
		/// the chosen MSA (null when cancelled). The caller performs the on-OK action (e.g. the reference
		/// launcher's <c>AddItem</c>).
		/// </summary>
		public static IMoMorphSynAnalysis Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry startingEntry, IWin32Window owner, IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var launcher = new LcmLinkMsaDialogLauncher(cache, mediator, propertyTable, helpProvider, startingEntry);
			var outcome = launcher.Run(owner);
			return !outcome.Accepted ? null : launcher.SelectedObject;
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.LinkMsaTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 400;
		protected override int DialogHeight => 360;

		protected override EntryGoDialogInput BuildState() =>
			BuildInput(_cache, _mediator, _propertyTable, _startingEntry);

		/// <summary>
		/// Builds the LCModel-free <see cref="EntryGoDialogInput"/> for the Link-MSA consumer: the legacy title /
		/// shared OK / "Lexical Entries" prompt, the starting entry's hvo as the excluded id (the legacy
		/// <c>m_startingEntry</c> cannot be a match), and a search over the shared <see cref="EntryGoSearchEngine"/>.
		/// Internal so the input + search are unit-testable against a real cache without running the modal.
		/// </summary>
		internal static EntryGoDialogInput BuildInput(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry startingEntry)
		{
			return new EntryGoDialogInput
			{
				Title = FwAvaloniaDialogsStrings.LinkMsaTitle,
				SearchPrompt = FwAvaloniaDialogsStrings.EntryGoResultsLabel,
				ExcludedId = startingEntry?.Hvo.ToString(CultureInfo.InvariantCulture),
				HelpTopic = "khtpInsertMorphemeChooseFunction",
				Search = BuildSearch(cache, mediator, propertyTable, startingEntry)
			};
		}

		/// <summary>
		/// The search delegate: reuses the shared <see cref="EntryGoSearchEngine"/> (the SAME matching the legacy
		/// EntryGoDlg uses) over the live repository, excluding the starting entry, and maps each matched entry to a
		/// lightweight row. Internal so it is unit-testable against a real cache.
		/// </summary>
		internal static Func<string, IReadOnlyList<EntryGoSearchResult>> BuildSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable, ILexEntry startingEntry)
		{
			return EntryGoLauncherShared.BuildEntrySearch(cache, mediator, propertyTable,
				startingEntry?.Hvo ?? 0, "AvaloniaLinkMsaSearchEngine", filter: null);
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
		/// Applies the OK result: resolves the chosen entry to its (first) MSA and exposes it via
		/// <see cref="SelectedObject"/>. No model mutation here — the caller performs the on-OK action.
		/// </summary>
		protected override LinkMsaPayload Apply(EntryGoDialogInput state)
		{
			SelectedObject = ResolveSelectedMsa(_cache, _viewModel?.ChosenId);
			return new LinkMsaPayload { Msa = SelectedObject };
		}

		/// <summary>
		/// Resolves the chosen entry's id to a grammatical-info / MSA object. PARITY: the legacy LinkMSADlg builds a
		/// combo of ALL of the entry's <c>MorphoSyntaxAnalysesOC</c> and default-selects the FIRST item; this slice
		/// returns that same first MSA. Choosing a non-first MSA is deferred (the kit's one-row-per-entry result
		/// list cannot carry per-MSA rows cleanly). Internal so it is unit-testable against a real cache.
		/// </summary>
		internal static IMoMorphSynAnalysis ResolveSelectedMsa(LcmCache cache, string entryId)
		{
			var entry = EntryGoLauncherShared.ResolveEntry(cache, entryId);
			return entry?.MorphoSyntaxAnalysesOC.FirstOrDefault();
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
