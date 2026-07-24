// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
	/// PARITY: like the legacy flow's <c>CreateAllomorphTypeMismatchDlg</c>, when the morpheme type deduced from the
	/// typed form's punctuation disagrees with the chosen entry's existing forms the launcher asks the user first
	/// (an injectable confirmation seam defaulting to an <see cref="FwMessageBox"/> Yes/No warning with the legacy
	/// wording): No adds nothing, Yes ensures an appropriate stem/unclassified-affix MSA exists and then creates
	/// the allomorph. See <see cref="PerformAddAllomorph"/>.
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
		private IWin32Window _owner;

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
			// Remember the owner so the type-mismatch confirmation (Apply) can parent its modal message box.
			launcher._owner = owner;
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
		/// step, asking the type-mismatch confirmation first when the deduced morpheme type disagrees with the
		/// entry's existing forms. The created/reused allomorph is exposed via <see cref="Allomorph"/> (null when
		/// the user declines the mismatch confirmation).
		/// </summary>
		protected override AddAllomorphPayload Apply(EntryGoDialogInput state)
		{
			var entry = EntryGoLauncherShared.ResolveEntry(_cache, _viewModel?.ChosenId);
			if (entry != null && _tssForm != null)
			{
				Allomorph = PerformAddAllomorph(_cache, entry, _tssForm, DefaultConfirmTypeMismatch);
				ChosenEntry = Allomorph != null ? entry : null;
			}
			return new AddAllomorphPayload { Allomorph = Allomorph, Entry = ChosenEntry };
		}

		/// <summary>
		/// The production type-mismatch confirmation: a modal Yes/No warning box parented to the remembered owner,
		/// carrying the legacy <c>CreateAllomorphTypeMismatchDlg</c> title + warning + question. Returns true only
		/// when the user confirms (Yes). A delegate seam so <see cref="PerformAddAllomorph"/> is
		/// unit-testable without spinning the modal.
		/// </summary>
		private bool DefaultConfirmTypeMismatch(string warning, string question)
		{
			var message = warning + Environment.NewLine + question;
			return FwMessageBox.Show(_owner, message, FwAvaloniaDialogsStrings.AddAllomorphMismatchTitle,
				FwMessageBoxButtons.YesNo, FwMessageBoxIcon.Warning) == FwMessageBoxResult.Yes;
		}

		/// <summary>
		/// Adds the typed form to <paramref name="entry"/> as an allomorph, mirroring the legacy
		/// <c>SandboxBase.RunAddNewAllomorphDlg</c> tail. When the morpheme type deduced from the form's punctuation
		/// disagrees with the entry's existing forms (<see cref="HasTypeMismatch"/>), the user is asked via
		/// <paramref name="confirmTypeMismatch"/> (warning, question) first: declining returns null with no change;
		/// confirming ensures an appropriate stem/unclassified-affix MSA exists and creates the allomorph, in ONE
		/// undoable step. Without a mismatch, a matching allomorph is reused (the legacy "Use Allomorph" path) or a
		/// new one is created. Internal + static so the flow is unit-testable with a stub confirmation.
		/// </summary>
		internal static IMoForm PerformAddAllomorph(LcmCache cache, ILexEntry entry, ITsString tssForm,
			Func<string, string, bool> confirmTypeMismatch = null)
		{
			if (HasTypeMismatch(cache, entry, tssForm, out var inferredType, out var strippedForm))
			{
				GetMismatchMessages(entry, inferredType, strippedForm, out var warning, out var question);
				if (confirmTypeMismatch == null || !confirmTypeMismatch(warning, question))
					return null; // the user declined (the legacy No/close): nothing is added.

				// The legacy Yes path: ensure an appropriate MSA exists, then ALWAYS create a new form (the legacy
				// flow never reuses a matching allomorph once the type disagrees), in one undoable step.
				IMoForm created = null;
				UndoableUnitOfWorkHelper.Do(FwAvaloniaDialogsStrings.AddAllomorphUndo,
					FwAvaloniaDialogsStrings.AddAllomorphRedo,
					cache.ServiceLocator.GetInstance<IActionHandler>(),
					() =>
					{
						EnsureMsaForMorphType(cache, entry, inferredType);
						created = MorphServices.MakeMorph(entry, tssForm);
					});
				return created;
			}

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

		/// <summary>
		/// The legacy <c>AddAllomorphDlg.HandleMatchingSelectionChanged</c> mismatch condition: the morpheme type
		/// deduced from the typed form's punctuation (<c>MorphServices.FindMorphType</c>) matches none of the
		/// entry's allomorphs (same type or ambiguous with it, walked exactly like the legacy loop), and the entry
		/// has a lexeme form (the legacy gate for popping the warning). Also yields the deduced type and the form
		/// text stripped of its morpheme markers for the messages. Internal so it is unit-testable.
		/// </summary>
		internal static bool HasTypeMismatch(LcmCache cache, ILexEntry entry, ITsString tssForm,
			out IMoMorphType inferredType, out string strippedForm)
		{
			inferredType = null;
			strippedForm = tssForm?.Text;
			if (entry?.LexemeFormOA == null || string.IsNullOrEmpty(strippedForm))
				return false;
			var form = strippedForm;
			inferredType = MorphServices.FindMorphType(cache, ref form, out _);
			strippedForm = form;
			if (inferredType == null)
				return false; // no deducible type: the legacy hvoType==0 case never warns.

			// The legacy loop verbatim: a form-match resets the type-match (unless the SAME form also matches the
			// type) and stops the walk; any type-compatible allomorph before that satisfies the type.
			var matchingType = false;
			foreach (var mf in entry.AllAllomorphs)
			{
				var matchingForm = mf.Form.VernacularDefaultWritingSystem.Text == strippedForm;
				if (matchingForm)
					matchingType = false;
				if (mf.MorphTypeRA != null
					&& (inferredType.Hvo == mf.MorphTypeRA.Hvo || inferredType.IsAmbiguousWith(mf.MorphTypeRA)))
				{
					matchingType = true;
				}
				if (matchingForm)
					break;
			}
			return !matchingType;
		}

		/// <summary>
		/// Ensures <paramref name="entry"/> carries an MSA appropriate for <paramref name="morphType"/> before an
		/// allomorph of that type is added — the legacy Yes-path tail: stem-like morph types (root/stem/clitic/
		/// particle/phrase) get a <c>IMoStemMsa</c> when none exists; every other type gets a
		/// <c>IMoUnclassifiedAffixMsa</c> when none exists. Must run inside an open unit of work. Internal so it is
		/// unit-testable against a real cache.
		/// </summary>
		internal static void EnsureMsaForMorphType(LcmCache cache, ILexEntry entry, IMoMorphType morphType)
		{
			var haveStemMsa = entry.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>().Any();
			var haveUnclassifiedMsa = entry.MorphoSyntaxAnalysesOC.OfType<IMoUnclassifiedAffixMsa>().Any();
			switch (morphType.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphBoundRoot:
				case MoMorphTypeTags.kMorphBoundStem:
				case MoMorphTypeTags.kMorphClitic:
				case MoMorphTypeTags.kMorphEnclitic:
				case MoMorphTypeTags.kMorphProclitic:
				case MoMorphTypeTags.kMorphStem:
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphParticle:
				case MoMorphTypeTags.kMorphPhrase:
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
					if (!haveStemMsa)
						entry.MorphoSyntaxAnalysesOC.Add(
							cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create());
					break;
				default:
					if (!haveUnclassifiedMsa)
						entry.MorphoSyntaxAnalysesOC.Add(
							cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create());
					break;
			}
		}

		// Builds the two confirmation messages with the legacy wording: the warning names the entry and its lexeme
		// form's morph type; the question names the deduced type and the (marker-stripped) typed form.
		private static void GetMismatchMessages(ILexEntry entry, IMoMorphType inferredType, string strippedForm,
			out string warning, out string question)
		{
			var entryForm = entry.HeadWord?.Text;
			if (string.IsNullOrEmpty(entryForm))
				entryForm = FwAvaloniaDialogsStrings.AddAllomorphNoForm;
			var entryType = entry.LexemeFormOA?.MorphTypeRA?.Name?.BestAnalysisAlternative?.Text
				?? FwAvaloniaDialogsStrings.AddAllomorphNoMorphType;
			var newType = inferredType?.Name?.BestAnalysisAlternative?.Text
				?? FwAvaloniaDialogsStrings.AddAllomorphNoMorphType;
			warning = string.Format(CultureInfo.CurrentCulture,
				FwAvaloniaDialogsStrings.AddAllomorphMismatchWarning, entryForm, entryType);
			question = string.Format(CultureInfo.CurrentCulture,
				FwAvaloniaDialogsStrings.AddAllomorphMismatchQuestion, newType, strippedForm);
		}

		private void OnHelpRequested(string topic)
		{
			if (_helpProvider == null || string.IsNullOrEmpty(topic))
				return;
			ShowHelp.ShowHelpTopic(_helpProvider, topic);
		}
	}
}
