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
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using XCore;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the reusable Avalonia entry-search ("go") dialog re-skinned as the New-UI
	/// replacement for the legacy <see cref="AddAllomorphDlg"/> (an <c>EntryGoDlg</c> child): the user searches for
	/// an entry, and on OK the typed form is added as an allomorph to the chosen entry. It REUSES the existing
	/// <see cref="EntryGoDialogViewModel"/>/<see cref="EntryGoDialogView"/> kit exactly like
	/// <see cref="LcmMergeEntryDialogLauncher"/>; only the title/OK/prompt text, the search filter, and the on-OK
	/// operation differ. Unlike the Link* launchers (whose on-OK action is performed by the caller), this launcher
	/// performs its OWN model mutation — adding the allomorph in one undoable step.
	///
	/// On OK it resolves the chosen entry and, mirroring the legacy
	/// <c>SandboxBase.RunAddNewAllomorphDlg</c> tail: if the entry already has an allomorph matching the typed form
	/// it is reused (<c>MorphServices.FindMatchingAllomorph</c>); otherwise a new <c>IMoForm</c> is created on the
	/// entry from the typed full form (<c>MorphServices.MakeMorph</c>), in ONE undoable step.
	///
	/// PARITY: the legacy flow ALSO pops a type-mismatch warning dialog (<c>CreateAllomorphTypeMismatchDlg</c>) when
	/// the morpheme type deduced from the typed form's punctuation disagrees with the entry's existing forms, and on
	/// confirmation ensures an appropriate MSA exists before creating the allomorph. That warning + MSA-creation
	/// branch is deferred here (it needs another modal + new model operations not exposed by the kit contract); this
	/// slice creates/reuses the allomorph without the mismatch prompt. See the // PARITY comment on
	/// <see cref="PerformAddAllomorph"/>.
	/// </summary>
	public sealed class LcmAddAllomorphDialogLauncher
		: AvaloniaDialogLauncher<EntryGoDialogInput, EntryGoDialogViewModel, LcmAddAllomorphDialogLauncher.AddAllomorphPayload>
	{
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly IHelpTopicProvider _helpProvider;
		private readonly ITsString _tssForm;
		private EntryGoDialogViewModel _viewModel;

		private LcmAddAllomorphDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			IHelpTopicProvider helpProvider, ITsString tssForm)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_helpProvider = helpProvider;
			_tssForm = tssForm;
		}

		/// <summary>The created/reused allomorph and the entry it was added to, if any.</summary>
		public struct AddAllomorphPayload
		{
			public IMoForm Allomorph;
			public ILexEntry Entry;
		}

		/// <summary>The allomorph created or reused on OK, or null when cancelled.</summary>
		public IMoForm Allomorph { get; private set; }

		/// <summary>The entry the allomorph was added to, or null when cancelled.</summary>
		public ILexEntry ChosenEntry { get; private set; }

		/// <summary>
		/// Shows the Find-Entry-to-Add-Allomorph dialog modally over <paramref name="owner"/> and, on OK, adds the
		/// typed form as an allomorph to the chosen entry in one undoable step. Returns the created/reused allomorph
		/// (null when cancelled).
		/// </summary>
		public static IMoForm Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable, ITsString tssForm,
			IWin32Window owner, IHelpTopicProvider helpProvider = null)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var launcher = new LcmAddAllomorphDialogLauncher(cache, mediator, propertyTable, helpProvider, tssForm);
			var outcome = launcher.Run(owner);
			return !outcome.Accepted ? null : launcher.Allomorph;
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.AddAllomorphTitle;
		protected override bool Resizable => true;
		protected override int DialogWidth => 400;
		protected override int DialogHeight => 360;

		protected override EntryGoDialogInput BuildState() =>
			BuildInput(_cache, _mediator, _propertyTable, _tssForm);

		/// <summary>
		/// Builds the LCModel-free <see cref="EntryGoDialogInput"/> for the Add-Allomorph consumer: the legacy title /
		/// "Add Allomorph..." OK / "Lexical Entries" prompt, the typed form as the initial query (the legacy dialog
		/// launches primed with the form being added), and a search over the shared <see cref="EntryGoSearchEngine"/>
		/// (no entry is excluded — any entry may receive the allomorph). Internal so the input + search are
		/// unit-testable against a real cache without running the modal.
		/// </summary>
		internal static EntryGoDialogInput BuildInput(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			ITsString tssForm)
		{
			return new EntryGoDialogInput
			{
				Title = FwAvaloniaDialogsStrings.AddAllomorphTitle,
				OkButtonText = FwAvaloniaDialogsStrings.AddAllomorphOkButton,
				SearchPrompt = FwAvaloniaDialogsStrings.EntryGoResultsLabel,
				InitialQuery = tssForm?.Text,
				HelpTopic = "khtpFindEntryToAddAllomorph",
				Search = BuildSearch(cache, mediator, propertyTable)
			};
		}

		/// <summary>
		/// The search delegate: the shared <see cref="EntryGoLauncherShared.BuildEntrySearch"/> with NO excluded
		/// entry (the Add-Allomorph dialog has no starting entry to exclude). Internal so it is unit-testable.
		/// </summary>
		internal static Func<string, IReadOnlyList<EntryGoSearchResult>> BuildSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable)
		{
			return EntryGoLauncherShared.BuildEntrySearch(cache, mediator, propertyTable,
				excludedEntryHvo: 0, "AvaloniaAddAllomorphSearchEngine", filter: null);
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
		/// Applies the OK result: resolves the chosen entry and adds the typed form as an allomorph in ONE undoable
		/// step. The created/reused allomorph is exposed via <see cref="Allomorph"/>.
		/// </summary>
		protected override AddAllomorphPayload Apply(EntryGoDialogInput state)
		{
			var entry = EntryGoLauncherShared.ResolveEntry(_cache, _viewModel?.ChosenId);
			if (entry != null && _tssForm != null)
			{
				Allomorph = PerformAddAllomorph(_cache, entry, _tssForm);
				ChosenEntry = entry;
			}
			return new AddAllomorphPayload { Allomorph = Allomorph, Entry = ChosenEntry };
		}

		/// <summary>
		/// Adds the typed form to <paramref name="entry"/> as an allomorph in ONE undoable step: if a matching
		/// allomorph already exists it is reused (<c>FindMatchingAllomorph</c>, the legacy "Use Allomorph" path),
		/// otherwise a new <c>IMoForm</c> is created from the form (<c>MakeMorph</c>). Returns the created/reused
		/// allomorph.
		///
		/// PARITY: the legacy AddAllomorphDlg flow first pops a <c>CreateAllomorphTypeMismatchDlg</c> warning when
		/// the deduced morpheme type disagrees with the entry's existing forms, and (on confirm) ensures an
		/// appropriate stem/affix MSA exists before creating the allomorph. That extra modal + MSA-creation branch is
		/// deferred; this slice creates/reuses the allomorph directly. Internal + static so the add is unit-testable
		/// against a real cache inside a UOW.
		/// </summary>
		internal static IMoForm PerformAddAllomorph(LcmCache cache, ILexEntry entry, ITsString tssForm)
		{
			var existing = MorphServices.FindMatchingAllomorph(entry, tssForm);
			if (existing != null)
				return existing;

			IMoForm allomorph = null;
			UndoableUnitOfWorkHelper.Do(FwAvaloniaDialogsStrings.AddAllomorphUndo,
				FwAvaloniaDialogsStrings.AddAllomorphRedo,
				cache.ServiceLocator.GetInstance<IActionHandler>(),
				() => { allomorph = MorphServices.MakeMorph(entry, tssForm); });
			return allomorph;
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
