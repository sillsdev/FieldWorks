// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the New-UI "Reference Set Details" dialog (Phase-1 §19g) — the Avalonia
	/// replacement for the WinForms <c>LexReferenceDetailsDlg</c> flow in
	/// <c>LexReferenceMultiSlice.EditReferenceDetails</c>. Seeds the dialog from a lexical reference's
	/// analysis-default-WS name + comment, and on OK writes both back in ONE undoable step (verbatim parity with
	/// the legacy apply: <c>Name.SetAnalysisDefaultWritingSystem</c> / <c>Comment.SetAnalysisDefaultWritingSystem</c>).
	///
	/// The model read/write stays here at the product edge (the view-model is LCModel-free). The
	/// seed-from-reference + apply core (<see cref="ApplyDetails"/>) is internal so the round-trip is
	/// unit-testable against a real cache (via InternalsVisibleTo) without running the modal.
	/// </summary>
	public sealed class LcmLexReferenceDetailsLauncher
		: AvaloniaDialogLauncher<LexReferenceDetailsDialogViewModel, LexReferenceDetailsDialogViewModel,
			LcmLexReferenceDetailsLauncher.DetailsPayload>
	{
		private readonly LcmCache _cache;
		private readonly ILexReference _reference;
		private readonly string _undoText;
		private readonly string _redoText;
		private LexReferenceDetailsDialogViewModel _viewModel;

		private LcmLexReferenceDetailsLauncher(LcmCache cache, ILexReference reference, string undoText, string redoText)
		{
			_cache = cache;
			_reference = reference;
			_undoText = undoText ?? string.Empty;
			_redoText = redoText ?? string.Empty;
		}

		/// <summary>Follow-up signal: whether the details were edited + committed.</summary>
		public struct DetailsPayload
		{
			/// <summary>True when the user clicked OK and the name/comment were written.</summary>
			public bool Committed;
		}

		/// <summary>
		/// Shows the reference-details dialog over <paramref name="owner"/>, seeded from <paramref name="reference"/>,
		/// and on OK writes the edited name + comment back in one undoable step. Returns true when committed.
		/// </summary>
		public static bool Edit(LcmCache cache, ILexReference reference, IWin32Window owner,
			string undoText, string redoText)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			if (reference == null) throw new ArgumentNullException(nameof(reference));
			var launcher = new LcmLexReferenceDetailsLauncher(cache, reference, undoText, redoText);
			var outcome = launcher.Run(owner);
			return outcome.Accepted && outcome.Payload.Committed;
		}

		/// <summary>
		/// Writes the edited name + comment into the reference's analysis-default writing system in one undoable
		/// step (the legacy apply). Internal so the apply is unit-testable against a real cache.
		/// </summary>
		internal static void ApplyDetails(LcmCache cache, ILexReference reference, string name, string comment,
			string undoText, string redoText)
		{
			if (cache == null || reference == null)
				return;
			using (var helper = new UndoableUnitOfWorkHelper(cache.ActionHandlerAccessor,
				undoText ?? string.Empty, redoText ?? string.Empty))
			{
				reference.Name.SetAnalysisDefaultWritingSystem(name ?? string.Empty);
				reference.Comment.SetAnalysisDefaultWritingSystem(comment ?? string.Empty);
				helper.RollBack = false;
			}
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.LexReferenceDetailsTitle;
		protected override int DialogWidth => 360;
		protected override int DialogHeight => 280;

		protected override LexReferenceDetailsDialogViewModel BuildState()
		{
			var name = _reference.Name?.AnalysisDefaultWritingSystem?.Text ?? string.Empty;
			var comment = _reference.Comment?.AnalysisDefaultWritingSystem?.Text ?? string.Empty;
			return new LexReferenceDetailsDialogViewModel(name, comment);
		}

		protected override LexReferenceDetailsDialogViewModel CreateViewModel(LexReferenceDetailsDialogViewModel state)
		{
			_viewModel = state;
			return _viewModel;
		}

		protected override AvControl CreateView(LexReferenceDetailsDialogViewModel viewModel) =>
			new LexReferenceDetailsDialogView { DataContext = viewModel };

		protected override DetailsPayload Apply(LexReferenceDetailsDialogViewModel state)
		{
			ApplyDetails(_cache, _reference, _viewModel?.ChosenName, _viewModel?.ChosenComment, _undoText, _redoText);
			return new DetailsPayload { Committed = true };
		}
	}
}
