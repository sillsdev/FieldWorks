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
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia entry-search ("go") dialog, wired as the FIRST concrete
	/// consumer of that kit dialog: Merge Entry (the New-UI replacement for the legacy <see cref="MergeEntryDlg"/>,
	/// itself an <c>EntryGoDlg</c> child). It is a concrete
	/// <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>: the Avalonia layer (FwAvaloniaDialogs)
	/// stays LCModel-free by exchanging an <see cref="EntryGoDialogInput"/> (configurable title/OK/prompt text + a
	/// plain <see cref="EntryGoDialogInput.Search"/> delegate returning lightweight <see cref="EntryGoSearchResult"/>
	/// rows) and an <see cref="EntryGoDialogResult"/> (the chosen hvo string).
	///
	/// The search delegate reuses the SAME matching the legacy dialog uses — the shared
	/// <see cref="EntryGoSearchEngine"/> over the live <c>ILexEntryRepository</c> with the legacy
	/// <see cref="EntryGoDlg.GetFields"/> field set — wrapped to EXCLUDE the current entry (parity with the legacy
	/// <c>MergeEntryDlg.MergeEntrySearchEngine.FilterResults</c>: you cannot merge an entry with itself). On OK it
	/// resolves the chosen hvo back to an <c>ILexEntry</c> and performs the merge exactly as the legacy
	/// <c>MergeEntryDlgListener</c> did — <c>survivor.MergeObject(currentEntry, fLoseNoTextData)</c> in ONE undoable
	/// step.
	///
	/// Layering mirrors <see cref="LcmInsertEntryDialogLauncher"/>/<see cref="LcmChooserDialogLauncher"/>: BuildInput
	/// / search / merge are internal so the search semantics + merge are unit-testable against a real cache (via
	/// InternalsVisibleTo) without running the modal.
	/// </summary>
	public sealed class LcmMergeEntryDialogLauncher
		: AvaloniaDialogLauncher<EntryGoDialogInput, EntryGoDialogViewModel, LcmMergeEntryDialogLauncher.MergePayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ILexEntry _currentEntry;
		private readonly bool _loseNoTextData;
		private EntryGoDialogViewModel _viewModel;
		private IWin32Window _owner;

		private LcmMergeEntryDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ILexEntry currentEntry, bool loseNoTextData)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_currentEntry = currentEntry;
			_loseNoTextData = loseNoTextData;
		}

		/// <summary>The follow-up signals from an accepted Merge dialog: the chosen (survivor) entry, if any.</summary>
		public struct MergePayload
		{
			public ILexEntry Survivor;
		}

		/// <summary>The entry the current entry was merged into (the survivor), or null when cancelled.</summary>
		public ILexEntry Survivor { get; private set; }

		/// <summary>
		/// Shows the Merge Entry dialog modally over <paramref name="owner"/> and, on OK, merges
		/// <paramref name="currentEntry"/> INTO the chosen entry (the survivor) in ONE undoable step — exactly the
		/// legacy <c>MergeObject</c> semantics. Returns the survivor (null when cancelled). The caller refreshes /
		/// jumps to the survivor (the legacy listener's JumpToRecord).
		/// </summary>
		public static ILexEntry Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry currentEntry, IWin32Window owner, bool loseNoTextData = true,
			IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

			var launcher = new LcmMergeEntryDialogLauncher(cache, mediator, propertyTable, helpProvider,
				currentEntry, loseNoTextData);
			// Remember the owner so the post-commit merge confirmation (Apply) can parent its modal message box.
			launcher._owner = owner;
			var outcome = launcher.Run(owner);
			if (!outcome.Accepted)
				return null;
			return launcher.Survivor;
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.MergeTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 400;
		protected override int DialogHeight => 360;

		protected override EntryGoDialogInput BuildState() =>
			BuildInput(_cache, _mediator, _propertyTable, _currentEntry);

		/// <summary>
		/// Builds the LCModel-free <see cref="EntryGoDialogInput"/> for the Merge consumer: the Merge title /
		/// "Merge" OK button / "Lexical Entries" prompt, the current entry's hvo as the excluded id, the starting
		/// headword as the initial query (LT-3017 parity), and a search delegate over the shared
		/// <see cref="EntryGoSearchEngine"/> (the legacy matching) wrapped to exclude the current entry. Internal so
		/// the input + search semantics are unit-testable against a real cache without running the modal.
		/// </summary>
		internal static EntryGoDialogInput BuildInput(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry currentEntry)
		{
			var search = BuildSearch(cache, mediator, propertyTable, currentEntry);
			return new EntryGoDialogInput
			{
				Title = FwAvaloniaDialogsStrings.MergeTitle,
				OkButtonText = FwAvaloniaDialogsStrings.MergeOkButton,
				SearchPrompt = FwAvaloniaDialogsStrings.EntryGoResultsLabel,
				ExcludedId = currentEntry.Hvo.ToString(CultureInfo.InvariantCulture),
				InitialQuery = currentEntry.HomographForm,
				HelpTopic = "khtpMergeEntry",
				Search = search
			};
		}

		/// <summary>
		/// Builds the search delegate the dialog drives: it reuses the shared <see cref="EntryGoSearchEngine"/> (the
		/// SAME matching the legacy EntryGoDlg uses, cached on the property table like the legacy dialog) wrapped to
		/// EXCLUDE the current entry's hvo, runs the legacy <see cref="EntryGoDlg.GetFields"/> field set for the
		/// current vernacular default WS, and maps each matched hvo to a lightweight <see cref="EntryGoSearchResult"/>
		/// (headword + a gloss preview). Internal so it is unit-testable against a real cache.
		/// </summary>
		internal static Func<string, IReadOnlyList<EntryGoSearchResult>> BuildSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable, ILexEntry currentEntry)
		{
			var engine = GetMergeSearchEngine(cache, mediator, propertyTable, currentEntry.Hvo);
			var ws = cache.DefaultVernWs;
			var repo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			return query =>
			{
				var fields = GetSearchFields(query ?? string.Empty, ws);
				var hvos = engine.Search(fields);
				var results = new List<EntryGoSearchResult>();
				foreach (var hvo in hvos)
				{
					if (hvo == currentEntry.Hvo)
						continue; // belt-and-braces on top of the engine's FilterResults
					if (!repo.TryGetObject(hvo, out var entry))
						continue;
					results.Add(new EntryGoSearchResult(
						hvo.ToString(CultureInfo.InvariantCulture),
						HeadwordText(entry),
						DescriptionText(entry)));
				}
				// Sort by headword for a stable, readable list (the legacy browser sorts its columns too).
				return results.OrderBy(r => r.Text, StringComparer.CurrentCulture).ToList();
			};
		}

		/// <summary>
		/// Gets (creating + caching on the property table, like the legacy dialog) a search engine that uses the
		/// legacy EntryGoDlg matching but excludes <paramref name="currentEntryHvo"/> — the merge invariant. When no
		/// property table is supplied (tests) a fresh engine is built.
		/// </summary>
		private static MergeEntrySearchEngine GetMergeSearchEngine(LcmCache cache, Mediator mediator,
			PropertyTable propertyTable, int currentEntryHvo)
		{
			MergeEntrySearchEngine engine;
			if (propertyTable != null)
			{
				engine = (MergeEntrySearchEngine)SearchEngine.Get(mediator, propertyTable,
					"AvaloniaMergeEntrySearchEngine", () => new MergeEntrySearchEngine(cache));
			}
			else
			{
				engine = new MergeEntrySearchEngine(cache);
			}
			engine.CurrentEntryHvo = currentEntryHvo;
			return engine;
		}

		/// <summary>
		/// The legacy <see cref="EntryGoDlg.GetFields"/> field set, simplified to the vernacular forms (citation /
		/// lexeme / alternate) for the merge search — the same fields the merge dialog primes with for the default
		/// vernacular WS. Internal + static so the field building is unit-testable.
		/// </summary>
		internal static IEnumerable<SearchField> GetSearchFields(string str, int ws)
		{
			var tssKey = TsStringUtils.MakeString(str, ws);
			yield return new SearchField(LexEntryTags.kflidCitationForm, tssKey);
			yield return new SearchField(LexEntryTags.kflidLexemeForm, tssKey);
			yield return new SearchField(LexEntryTags.kflidAlternateForms, tssKey);
		}

		private static string HeadwordText(ILexEntry entry)
			=> entry.HeadWord?.Text ?? entry.HomographForm ?? entry.Hvo.ToString(CultureInfo.InvariantCulture);

		// A short gloss preview for the description pane: the best-analysis gloss of the first sense, if any.
		private static string DescriptionText(ILexEntry entry)
		{
			var firstSense = entry.SensesOS.FirstOrDefault();
			var gloss = firstSense?.Gloss?.BestAnalysisAlternative?.Text;
			return string.IsNullOrEmpty(gloss) ? HeadwordText(entry) : $"{HeadwordText(entry)} : {gloss}";
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
		/// Applies the commit-on-select result: resolves the chosen hvo to the survivor entry and — because the merge
		/// is semi-destructive (it merges the current entry INTO the chosen one and cannot be undone past this UOW) —
		/// CONFIRMS before acting. Only on confirmation does it merge the current entry into the survivor in ONE
		/// undoable step (the exact legacy <c>MergeObject</c> operation; see
		/// <c>MergeEntryDlgListener.RunMergeEntryDialog</c>); Cancel aborts with no merge. The survivor is exposed via
		/// <see cref="Survivor"/> (null when cancelled). The other Add/Link* consumers act immediately with no confirm.
		/// </summary>
		protected override MergePayload Apply(EntryGoDialogInput state)
		{
			var survivor = ResolveEntry(_viewModel?.ChosenId);
			Survivor = ConfirmAndMerge(_cache, survivor, _currentEntry, _loseNoTextData, DefaultConfirm);
			return new MergePayload { Survivor = Survivor };
		}

		/// <summary>
		/// The production merge-confirmation: a modal Yes/No message box parented to the remembered owner form, seeded
		/// from the canonical legacy "Entry X will be merged into Y" wording. Returns true only when the user confirms
		/// (Yes). Factored to a <see cref="Func{T1,T2,TResult}"/> seam so <see cref="ConfirmAndMerge"/> is
		/// unit-testable without spinning the modal.
		/// </summary>
		private bool DefaultConfirm(ILexEntry current, ILexEntry survivor)
		{
			var message = string.Format(CultureInfo.CurrentCulture, FwAvaloniaDialogsStrings.MergeConfirm,
				HeadwordText(current), HeadwordText(survivor));
			var result = FwMessageBox.Show(_owner, message, FwAvaloniaDialogsStrings.MergeTitle,
				FwMessageBoxButtons.YesNo, FwMessageBoxIcon.Warning);
			return result == FwMessageBoxResult.Yes;
		}

		/// <summary>
		/// Confirms the (semi-destructive) merge and performs it only on confirmation: when <paramref name="survivor"/>
		/// is a real, different entry and <paramref name="confirm"/> returns true, merges <paramref name="current"/>
		/// INTO <paramref name="survivor"/> (in ONE undoable step via <see cref="PerformMerge"/>, the production
		/// default) and returns the survivor; otherwise (no survivor, the same entry, or a declined confirm) returns
		/// null with no merge. Internal so the confirm-gating is unit-testable against a real cache by passing a stub
		/// <paramref name="confirm"/> (assert the merge runs only when it returns true) without spinning the modal; the
		/// optional <paramref name="merge"/> seam lets a test substitute a spy for <see cref="PerformMerge"/> so it can
		/// assert the merge is reached on confirm without opening a (nested) unit of work.
		/// </summary>
		internal static ILexEntry ConfirmAndMerge(LcmCache cache, ILexEntry survivor,
			ILexEntry current, bool loseNoTextData, Func<ILexEntry, ILexEntry, bool> confirm,
			Action<ILexEntry, ILexEntry> merge = null)
		{
			if (survivor == null || survivor == current)
				return null;
			if (confirm != null && !confirm(current, survivor))
				return null; // the user declined the merge confirmation — abort, no merge.
			if (merge != null)
				merge(survivor, current);
			else
				PerformMerge(cache, survivor, current, loseNoTextData);
			return survivor;
		}

		private ILexEntry ResolveEntry(string id)
		{
			if (string.IsNullOrEmpty(id) || _cache == null)
				return null;
			if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hvo))
				return null;
			return _cache.ServiceLocator.GetInstance<ILexEntryRepository>().TryGetObject(hvo, out var entry)
				? entry
				: null;
		}

		/// <summary>
		/// Performs the merge in ONE undoable step, identical to the legacy listener: the survivor absorbs the
		/// current entry and its DateModified is bumped. Internal + static so the merge is unit-testable against a
		/// real cache inside a UOW.
		/// </summary>
		internal static void PerformMerge(LcmCache cache, ILexEntry survivor, ILexEntry currentEntry,
			bool loseNoTextData)
		{
			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoMergeEntry, LexTextControls.ksRedoMergeEntry,
				cache.ActionHandlerAccessor,
				() =>
				{
					survivor.MergeObject(currentEntry, loseNoTextData);
					survivor.DateModified = DateTime.Now;
				});
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}

		/// <summary>
		/// The merge-flavored search engine: the legacy EntryGoDlg matching with the current entry filtered out of
		/// the results (you cannot merge an entry with itself). A direct lift of
		/// <c>MergeEntryDlg.MergeEntrySearchEngine</c> so the Avalonia path uses the identical search semantics.
		/// </summary>
		private sealed class MergeEntrySearchEngine : EntryGoSearchEngine
		{
			public int CurrentEntryHvo { private get; set; }

			public MergeEntrySearchEngine(LcmCache cache) : base(cache)
			{
			}

			protected override IEnumerable<int> FilterResults(IEnumerable<int> results)
			{
				return results == null ? null : results.Where(hvo => hvo != CurrentEntryHvo);
			}
		}
	}
}
