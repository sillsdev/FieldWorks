// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware launcher for the New-UI delete-confirmation dialog (Phase-1 §19g) — the Avalonia
	/// replacement for the WinForms <c>ConfirmDeleteObjectDlg</c> flow used by the lexical-edit delete paths
	/// (entry/sense detail delete, the LexReference relation deletes, the browse Delete tab). The caller supplies
	/// the affected-object <c>summary</c> (the legacy <c>DeletionTextTSS</c> text), an optional <c>note</c> (the
	/// orphan / sequence-collection / tree wording the slice already builds), whether the object
	/// <c>canDelete</c>, and the actual removal as an <see cref="Action"/>. On OK (and only when deletable) the
	/// launcher runs that removal in ONE undoable step and returns Accepted; on Cancel nothing happens.
	///
	/// The model write stays here at the product edge (the dialog view-model is LCModel-free). The removal action
	/// is passed verbatim from the call site so the launcher does not need to know the relation kind — the call
	/// site keeps its existing <c>TargetsRS.Remove</c> / <c>DeleteObj</c> logic, just inside the launcher's UOW.
	/// </summary>
	public sealed class LcmDeleteObjectLauncher
		: AvaloniaDialogLauncher<DeleteConfirmationDialogViewModel, DeleteConfirmationDialogViewModel,
			LcmDeleteObjectLauncher.DeletePayload>
	{
		private readonly string _summary;
		private readonly string _note;
		private readonly bool _canDelete;
		private readonly string _title;
		private DeleteConfirmationDialogViewModel _viewModel;

		private LcmDeleteObjectLauncher(string summary, string note, bool canDelete, string title)
		{
			_summary = summary ?? string.Empty;
			_note = note ?? string.Empty;
			_canDelete = canDelete;
			_title = string.IsNullOrEmpty(title) ? FwAvaloniaDialogsStrings.DeleteConfirmationDelete : title;
		}

		/// <summary>Follow-up signal: whether the user confirmed a (possible) delete.</summary>
		public struct DeletePayload
		{
			/// <summary>True when the user clicked Delete on a deletable object.</summary>
			public bool Confirmed;
		}

		/// <summary>
		/// Shows the delete-confirmation dialog over <paramref name="owner"/> and, on confirm (and only when
		/// <paramref name="canDelete"/>), runs <paramref name="removeInUow"/> inside ONE undoable step using
		/// <paramref name="undoText"/>/<paramref name="redoText"/>. Returns true when the delete was confirmed and
		/// run; false on Cancel or a non-deletable object.
		/// </summary>
		public static bool Confirm(LcmCache cache, IWin32Window owner, string summary, string note, bool canDelete,
			string title, string undoText, string redoText, Action removeInUow)
		{
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			var launcher = new LcmDeleteObjectLauncher(summary, note, canDelete, title);
			var outcome = launcher.Run(owner);
			if (!outcome.Accepted || !canDelete)
				return false;

			RunDelete(cache, undoText, redoText, removeInUow);
			return true;
		}

		/// <summary>
		/// Runs the caller-supplied removal in one undoable step. Internal so the delete core is unit-testable
		/// against a real cache without running the modal.
		/// </summary>
		internal static void RunDelete(LcmCache cache, string undoText, string redoText, Action removeInUow)
		{
			if (cache == null || removeInUow == null)
				return;
			UndoableUnitOfWorkHelper.Do(
				undoText ?? string.Empty, redoText ?? string.Empty,
				cache.ServiceLocator.GetInstance<IActionHandler>(), removeInUow);
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => _title;
		protected override int DialogWidth => 400;
		protected override int DialogHeight => 240;

		protected override DeleteConfirmationDialogViewModel BuildState() =>
			new DeleteConfirmationDialogViewModel(_summary, _note, _canDelete);

		protected override DeleteConfirmationDialogViewModel CreateViewModel(DeleteConfirmationDialogViewModel state)
		{
			_viewModel = state;
			return _viewModel;
		}

		protected override AvControl CreateView(DeleteConfirmationDialogViewModel viewModel) =>
			new DeleteConfirmationDialogView { DataContext = viewModel };

		protected override DeletePayload Apply(DeleteConfirmationDialogViewModel state) =>
			new DeletePayload { Confirmed = _canDelete };
	}
}
