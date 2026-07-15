// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The product side of the cross-surface drag-and-drop bridge (task 3.14): builds and reads the
	/// shared OS data objects both WinForms (`DoDragDrop`/`AllowDrop`) and Avalonia (`DragDrop`)
	/// surfaces exchange. Text drags reuse the clipboard seam's dual-lane payload
	/// (<see cref="FwTsStringClipboard.CreateDataObject"/>: legacy `"TsString"` rich lane +
	/// `UnicodeText`); object moves carry the framework-neutral
	/// <see cref="FwRecordKeyPayload"/> guid key plus a plain-text label for external drops.
	/// Legacy in-surface reorder DnD (`SliceTreeNode`, `RecordBarTreeHandler`) stays surface-local
	/// and untouched; specific drag interactions land with their editors (6.x/7.x).
	/// </summary>
	public static class FwDragDropData
	{
		/// <summary>Builds the data object for dragging a record (object move/link).</summary>
		public static DataObject CreateRecordDataObject(ICmObject record)
		{
			if (record == null)
				throw new ArgumentNullException(nameof(record));

			var dataObject = new DataObject();
			dataObject.SetData(FwDragDropFormats.RecordKeyFormat,
				new FwRecordKeyPayload(record.Guid).Serialize());
			dataObject.SetData(DataFormats.UnicodeText, record.ShortName ?? record.Guid.ToString());
			return dataObject;
		}

		/// <summary>
		/// Reads a dragged record key and resolves it in the given cache. False when the data is not
		/// a FieldWorks record drag or the object does not exist in this project.
		/// </summary>
		public static bool TryGetRecord(IDataObject dataObject, LcmCache cache, out ICmObject record)
		{
			record = null;
			if (dataObject == null || cache == null)
				return false;
			if (!(dataObject.GetData(FwDragDropFormats.RecordKeyFormat) is string serialized))
				return false;
			if (!FwRecordKeyPayload.TryParse(serialized, out var payload))
				return false;

			return cache.ServiceLocator.ObjectRepository.TryGetObject(payload.ObjectGuid, out record);
		}

		/// <summary>Builds the data object for dragging text (same dual-lane payload as copy/paste).</summary>
		public static DataObject CreateTextDataObject(FwClipboardText payload)
			=> FwTsStringClipboard.CreateDataObject(payload);
	}
}
