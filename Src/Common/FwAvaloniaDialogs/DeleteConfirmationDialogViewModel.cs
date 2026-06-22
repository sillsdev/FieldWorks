// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the delete-confirmation dialog (Phase-1 §19g) — the LCModel-free Avalonia replacement
	/// for the WinForms <c>ConfirmDeleteObjectDlg</c>. The LCModel-aware launcher (<c>LcmDeleteObjectLauncher</c>)
	/// populates it from the object being deleted: the affected-object <see cref="Summary"/> (the legacy
	/// <c>DeletionTextTSS</c>), an optional secondary <see cref="Note"/> (the orphan / sequence-collection /
	/// tree wording the slice passes for relations), and whether the object <see cref="CanDelete"/>.
	///
	/// Parity with the legacy dialog:
	///  * the affirmative button is "Delete" (not "OK") and is gated on <see cref="CanDelete"/>;
	///  * when the object cannot be deleted, the bottom question ("Do you want to continue…?") is hidden and
	///    Delete is disabled — the dialog becomes an informational "cannot delete" message.
	/// On accept the launcher runs the caller-supplied removal in one undoable step; on cancel nothing happens.
	/// </summary>
	public partial class DeleteConfirmationDialogViewModel : DialogViewModelBase
	{
		private readonly bool _canDelete;

		public DeleteConfirmationDialogViewModel()
			: this(string.Empty, null, true)
		{
		}

		/// <param name="summary">The affected-object summary (legacy <c>DeletionTextTSS</c>); shown at the top.</param>
		/// <param name="note">Optional secondary note (orphan / sequence-collection / tree wording); hidden when empty.</param>
		/// <param name="canDelete">When false, Delete is disabled and the confirm question is hidden.</param>
		public DeleteConfirmationDialogViewModel(string summary, string note, bool canDelete)
		{
			Summary = summary ?? string.Empty;
			Note = note ?? string.Empty;
			_canDelete = canDelete;
		}

		/// <summary>The top message ("You are deleting the following item:"), localized.</summary>
		public string TopMessage => FwAvaloniaDialogsStrings.DeleteConfirmationTopMessage;

		/// <summary>The affected-object summary (the legacy <c>DeletionTextTSS</c>).</summary>
		public string Summary { get; }

		/// <summary>The optional secondary note (orphan / relation wording); empty when there is none.</summary>
		public string Note { get; }

		/// <summary>True when the secondary <see cref="Note"/> should be shown.</summary>
		public bool HasNote => !string.IsNullOrWhiteSpace(Note);

		/// <summary>The bottom confirm question; shown only when <see cref="CanDelete"/>.</summary>
		public string BottomQuestion => FwAvaloniaDialogsStrings.DeleteConfirmationBottomQuestion;

		/// <summary>The affirmative-button caption ("Delete"), localized.</summary>
		public string DeleteButtonText => FwAvaloniaDialogsStrings.DeleteConfirmationDelete;

		/// <summary>True when the object can be deleted; gates the Delete button and the confirm question.</summary>
		public bool CanDelete => _canDelete;

		/// <summary>Delete (OK) is gated on the object being deletable — parity with the legacy enabled-state.</summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (!_canDelete)
				yield return string.Empty; // a non-empty error count disables OK; no message shown for "cannot delete"
		}
	}
}
