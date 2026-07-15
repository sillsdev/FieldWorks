// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
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
	/// replacement for the legacy <see cref="LinkEntryOrSenseDlg"/> (an <c>EntryGoDlg</c> child): the user searches
	/// for an entry and the dialog returns the chosen <c>ILexEntry</c>. It REUSES the existing
	/// <see cref="EntryGoDialogViewModel"/>/<see cref="EntryGoDialogView"/> kit exactly like
	/// <see cref="LcmMergeEntryDialogLauncher"/>; only the title/prompt text, the search filter, and the on-OK
	/// resolution differ.
	///
	/// The on-OK action of the legacy dialog (creating the lexical reference / link) is performed by the CALLER (the
	/// reference launcher / chooser-command), so this launcher only resolves the chosen entry (exposed via
	/// <see cref="SelectedObject"/>); it performs no model mutation of its own.
	///
	/// The legacy dialog's Entry-vs-Sense radio toggle is now implemented additively on the kit: a call site that
	/// wants senses opts in (<paramref name="allowSenses"/> shows the toggle; <paramref name="sensesOnly"/> locks
	/// it to senses, the legacy <c>SelectSensesOnly</c>). In sense mode the search lists each matching entry's
	/// senses (the legacy m_fwcbSenses) and OK returns the chosen <c>ILexSense</c>; in entry mode (the default,
	/// used by the entry-only callers and the entry side of the toggle) OK returns the chosen <c>ILexEntry</c>.
	/// </summary>
	public sealed class LcmLinkEntryOrSenseDialogLauncher
		: AvaloniaDialogLauncher<EntryGoDialogInput, EntryGoDialogViewModel, LcmLinkEntryOrSenseDialogLauncher.LinkPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ILexEntry _startingEntry;
		private readonly string _helpTopic;
		private readonly bool _allowSenses;
		private readonly bool _sensesOnly;
		private readonly string _title;
		private readonly string _okButtonText;
		private EntryGoDialogViewModel _viewModel;

		private LcmLinkEntryOrSenseDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ILexEntry startingEntry, string helpTopic, bool allowSenses,
			bool sensesOnly, string title, string okButtonText)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_startingEntry = startingEntry;
			_helpTopic = helpTopic;
			// sensesOnly implies senses are allowed (the toggle is shown but locked to "Specific Sense").
			_allowSenses = allowSenses || sensesOnly;
			_sensesOnly = sensesOnly;
			_title = string.IsNullOrEmpty(title) ? FwAvaloniaDialogsStrings.LinkEntryOrSenseTitle : title;
			_okButtonText = okButtonText;
		}

		/// <summary>The chosen entry (the entry path), if any.</summary>
		public struct LinkPayload
		{
			public ICmObject Chosen;
		}

		/// <summary>The object the user chose (an <c>ILexEntry</c> in this slice), or null when cancelled.</summary>
		public ICmObject SelectedObject { get; private set; }

		/// <summary>
		/// Shows the Choose-Lexical-Entry-or-Sense dialog modally over <paramref name="owner"/> and returns the
		/// chosen object (null when cancelled). The caller performs the on-OK action (e.g. creating the link).
		/// <paramref name="allowSenses"/> shows the Entry/Sense toggle so the user can return an entry OR a sense;
		/// <paramref name="sensesOnly"/> locks the dialog to senses (the legacy <c>SelectSensesOnly</c>) and returns
		/// an <c>ILexSense</c>. <paramref name="title"/>/<paramref name="okButtonText"/> override the defaults so a
		/// call site can mirror its legacy <c>WindowParams</c> (e.g. "Identify X Sense" + "Add").
		/// </summary>
		public static ICmObject Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry startingEntry, IWin32Window owner, string helpTopic = "khtpChooseLexicalEntryOrSense",
			IHelpTopicProvider helpProvider = null, bool allowSenses = false, bool sensesOnly = false,
			string title = null, string okButtonText = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var launcher = new LcmLinkEntryOrSenseDialogLauncher(cache, mediator, propertyTable, helpProvider,
				startingEntry, helpTopic, allowSenses, sensesOnly, title, okButtonText);
			var outcome = launcher.Run(owner);
			return !outcome.Accepted ? null : launcher.SelectedObject;
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => _title;
		protected override bool Resizable => true;
		protected override int DialogWidth => 400;
		protected override int DialogHeight => 360;

		protected override EntryGoDialogInput BuildState() =>
			BuildInput(_cache, _mediator, _propertyTable, _startingEntry, _helpTopic, _allowSenses, _sensesOnly,
				_title, _okButtonText);

		/// <summary>
		/// Builds the LCModel-free <see cref="EntryGoDialogInput"/> for the Link-Entry-or-Sense consumer. Internal so
		/// the input + search are unit-testable against a real cache without running the modal. When
		/// <paramref name="allowSenses"/>/<paramref name="sensesOnly"/> are set the input carries the entry/sense
		/// toggle + the mode-aware search; otherwise it stays an entry-only input (parity with the prior behavior).
		/// </summary>
		internal static EntryGoDialogInput BuildInput(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ILexEntry startingEntry, string helpTopic = "khtpChooseLexicalEntryOrSense", bool allowSenses = false,
			bool sensesOnly = false, string title = null, string okButtonText = null)
		{
			var sensesEnabled = allowSenses || sensesOnly;
			return new EntryGoDialogInput
			{
				Title = string.IsNullOrEmpty(title) ? FwAvaloniaDialogsStrings.LinkEntryOrSenseTitle : title,
				OkButtonText = okButtonText,
				SearchPrompt = FwAvaloniaDialogsStrings.EntryGoResultsLabel,
				ExcludedId = startingEntry?.Hvo.ToString(CultureInfo.InvariantCulture),
				HelpTopic = helpTopic,
				ShowEntrySenseToggle = sensesEnabled,
				SensesOnly = sensesOnly,
				Search = BuildSearch(cache, mediator, propertyTable, startingEntry),
				SearchByMode = sensesEnabled
					? BuildSearchByMode(cache, mediator, propertyTable, startingEntry)
					: null
			};
		}

		/// <summary>
		/// The entry-only search delegate: the shared <see cref="EntryGoLauncherShared.BuildEntrySearch"/> excluding
		/// the starting entry (the legacy <c>m_startingEntry</c> cannot be a match). Internal so it is unit-testable.
		/// </summary>
		internal static Func<string, IReadOnlyList<EntryGoSearchResult>> BuildSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable, ILexEntry startingEntry)
		{
			return EntryGoLauncherShared.BuildEntrySearch(cache, mediator, propertyTable,
				startingEntry?.Hvo ?? 0, "AvaloniaLinkEntryOrSenseSearchEngine", filter: null);
		}

		/// <summary>
		/// The mode-aware search delegate (entries in entry mode, the matching entries' senses in sense mode),
		/// excluding the starting entry. Internal so it is unit-testable.
		/// </summary>
		internal static Func<string, bool, IReadOnlyList<EntryGoSearchResult>> BuildSearchByMode(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable, ILexEntry startingEntry)
		{
			return EntryGoLauncherShared.BuildEntryOrSenseSearch(cache, mediator, propertyTable,
				startingEntry?.Hvo ?? 0, "AvaloniaLinkEntryOrSenseSearchEngine");
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
		/// Applies the OK result: resolves the chosen entry and exposes it via <see cref="SelectedObject"/>. No model
		/// mutation here — the caller performs the on-OK action.
		/// </summary>
		protected override LinkPayload Apply(EntryGoDialogInput state)
		{
			SelectedObject = ResolveSelectedObject(_cache, _viewModel?.ChosenId, _viewModel?.ChosenIsSense ?? false);
			return new LinkPayload { Chosen = SelectedObject };
		}

		/// <summary>
		/// Resolves the chosen id to the selected object: an <c>ILexSense</c> when the chosen row was a sense (the
		/// legacy dialog's "Specific Sense" radio path), otherwise an <c>ILexEntry</c> (the Entry radio path).
		/// Internal so it is unit-testable against a real cache.
		/// </summary>
		internal static ICmObject ResolveSelectedObject(LcmCache cache, string chosenId, bool chosenIsSense = false)
		{
			return chosenIsSense
				? (ICmObject)EntryGoLauncherShared.ResolveSense(cache, chosenId)
				: EntryGoLauncherShared.ResolveEntry(cache, chosenId);
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
