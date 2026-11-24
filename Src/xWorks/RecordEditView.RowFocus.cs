// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Where the keyboard goes when something outside the detail view asks for one of its rows:
	/// Jump to field in the dictionary preview, answered in both detail views, and a record
	/// its clerk opens for editing, answered in the Avalonia view.
	/// </summary>
	public partial class RecordEditView
	{
		// The Jump to field request waiting for its record to be shown, and that record; null
		// and 0 when none waits.
		private JumpToFieldRequest m_pendingFieldJump;
		private int m_pendingFieldJumpRecordHvo;

		// The record whose next show puts the keyboard in its first field; 0 when none.
		private int m_recordWantingKeyboard;

		/// <summary>
		/// The rows the Avalonia view was last asked to give the keyboard to, in the order to
		/// try them; null when it was last shown without such a request.
		/// </summary>
		internal IReadOnlyList<string> RowsAskedToFocus { get; private set; }

		/// <summary>
		/// Answers a <see cref="JumpToFieldRequest"/>: has the clerk show the request's record
		/// when another one is current, then focuses the row the request lands on. A row of a
		/// record still to be shown is focused once that record is. Only the view in the
		/// window that owns its property table answers.
		/// </summary>
		internal void JumpToField(object argument)
		{
			if (IsDisposed || !m_fullyInitialized || !IsInOwningWindow || !(argument is JumpToFieldRequest request))
				return;
			m_pendingFieldJump = null;
			if (request.RecordHvo != 0 && ClerkRecordHvo != request.RecordHvo)
			{
				Clerk.OnJumpToRecord(request.RecordHvo);
				// A clerk that kept its record, as when a filter hides the other one, shows
				// no row of the request's.
				if (ClerkRecordHvo != request.RecordHvo)
					return;
			}
			var recordHvo = ClerkRecordHvo;
			if (IsShowing(recordHvo))
			{
				FocusFieldJumpRow(request);
				return;
			}
			m_pendingFieldJump = request;
			m_pendingFieldJumpRecordHvo = recordHvo;
		}

		// The record the view shows for the clerk's current object; 0 when the clerk has none.
		private int ClerkRecordHvo => ResolveShownRecord(Clerk?.CurrentObject)?.Hvo ?? 0;

		// Whether the active detail view shows that record now.
		private bool IsShowing(int recordHvo)
		{
			if (recordHvo == 0)
				return false;
			var shown = ShouldUseAvaloniaLexiconEdit
				? (m_avaloniaEntryForm?.ShownDetail == null ? null : m_shownRecord)
				: (m_dataTreeInitialized ? m_dataEntryForm?.Root : null);
			return shown != null && shown.IsValidObject && shown.Hvo == recordHvo;
		}

		// Focuses the row of the shown record the request lands on; nothing when none fits.
		private void FocusFieldJumpRow(JumpToFieldRequest request)
		{
			if (!ShouldUseAvaloniaLexiconEdit)
			{
				m_dataEntryForm.JumpToField(request.FieldObjectHvo, request.FieldName, request.FieldValue);
				return;
			}
			var row = FieldJumpRow(m_avaloniaEntryForm.ShownDetail, m_shownRecord, request);
			if (row == null)
				return;
			RowsAskedToFocus = new[] { row.RowAutomationId };
			m_avaloniaEntryForm.FocusRow(RowsAskedToFocus);
		}

		// The waiting Jump to field request when the record now shown is the one it waits
		// for, else null. Either way nothing waits afterwards.
		private JumpToFieldRequest TakePendingFieldJump(ICmObject shown)
		{
			var request = m_pendingFieldJump;
			m_pendingFieldJump = null;
			return request != null && shown != null && shown.Hvo == m_pendingFieldJumpRecordHvo ? request : null;
		}

		// Answers a request that waited for the record the WinForms view has just shown. The
		// tree focuses the slice once its new slices can take focus.
		private void JumpToPendingFieldOnceShown(ICmObject shown)
		{
			var request = TakePendingFieldJump(shown);
			if (request != null)
				FocusFieldJumpRow(request);
		}

		/// <summary>
		/// The row of <paramref name="detail"/> a Jump to field request lands on, by the rule
		/// both detail views share; null when no row fits, or when a row's data cannot be
		/// read, which is logged.
		/// </summary>
		/// <param name="root">The record <paramref name="detail"/> shows.</param>
		internal DetailField FieldJumpRow(DetailModel detail, ICmObject root, JumpToFieldRequest request)
		{
			var repository = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (detail == null || !repository.TryGetObject(request.FieldObjectHvo, out var fieldObject))
				return null;
			try
			{
				var rows = detail.Fields.Select(field => JumpRowOf(field, repository)).ToList();
				var index = DetailFieldJump.FindRow(rows, root, fieldObject, request.FieldName, request.FieldValue);
				return index < 0 ? null : detail.Fields[index];
			}
			catch (Exception e)
			{
				Logger.WriteError("Finding the row for Jump to field failed; no row is focused.", e);
				return null;
			}
		}

		// The row as the shared rule sees it; null for a header or a row with no object.
		private static DetailFieldJumpRow JumpRowOf(DetailField field, ICmObjectRepository repository)
		{
			if (field.Kind == DetailFieldKind.Header || !repository.TryGetObject(field.ObjectHvo, out var rowObject))
				return null;
			return new DetailFieldJumpRow(rowObject, () => field.Field,
				text => DetailFieldJump.ShowsText(rowObject, field.Field, text));
		}

		// The clerk opened a record for editing. The WinForms view answers the clerk's
		// FocusFirstPossibleSlice message itself.
		private void OnRecordWantsKeyboard(int recordHvo)
		{
			if (IsDisposed || !ShouldUseAvaloniaLexiconEdit)
				return;
			if (!IsShowing(ClerkRecordHvo) || Clerk.CurrentObject.Hvo != recordHvo)
			{
				m_recordWantingKeyboard = recordHvo;
				return;
			}
			RowsAskedToFocus = EditorRows(m_avaloniaEntryForm.ShownDetail);
			m_avaloniaEntryForm.FocusRow(RowsAskedToFocus);
		}

		/// <summary>
		/// The rows a show of <paramref name="shown"/> should try giving the keyboard to: the
		/// row a waiting Jump to field request lands on, else the editor rows of a record
		/// opened for editing; null to carry focus across. Nothing waits afterwards.
		/// </summary>
		private IReadOnlyList<string> TakeRowsToFocus(ICmObject shown, DetailModel detail)
		{
			var recordWantingKeyboard = m_recordWantingKeyboard;
			m_recordWantingKeyboard = 0;
			var request = TakePendingFieldJump(shown);
			var row = request == null ? null : FieldJumpRow(detail, shown, request);
			if (row != null)
				return new[] { row.RowAutomationId };
			return recordWantingKeyboard != 0
				&& (shown.Hvo == recordWantingKeyboard || Clerk?.CurrentObject?.Hvo == recordWantingKeyboard)
				? EditorRows(detail)
				: null;
		}

		/// <summary>
		/// The rows of <paramref name="detail"/> that hold an editor, in view order: the rows
		/// to try focusing for a record opened for editing.
		/// </summary>
		internal static IReadOnlyList<string> EditorRows(DetailModel detail)
			=> detail.Fields.Where(f => f.Kind != DetailFieldKind.Header && f.Kind != DetailFieldKind.Unsupported)
				.Select(f => f.RowAutomationId).ToList();
	}
}
