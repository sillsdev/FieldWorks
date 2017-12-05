// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.ObjectModel;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// This class helps manage multiple changes to a record list.
	/// By default, it will suspend full Reloads initiated by PropChanged until we finish.
	/// During dispose, we'll ReloadList if we tried to reload the list via PropChanged.
	/// </summary>
	public class ListUpdateHelper : DisposableBase
	{
		IRecordList m_recordList;
		private WaitCursor m_waitCursor = null;
		readonly bool m_fOriginalUpdatingList = false;
		readonly bool m_fOriginalListLoadingSuppressedState = false;
		readonly bool m_fOriginalSkipRecordNavigationState = false;
		private bool m_fOriginalLoadRequestedWhileSuppressed = false;
		readonly bool m_fOriginalSuppressSaveOnChangeRecord;
		readonly ListUpdateHelper m_originalUpdateHelper = null;

		/// <summary />
		internal ListUpdateHelper(IRecordList recordList, Control parentForWaitCursor)
			: this(recordList)
		{
			if (parentForWaitCursor != null)
			{
				m_waitCursor = new WaitCursor(parentForWaitCursor);
			}
		}

		/// <summary />
		internal ListUpdateHelper(IRecordList recordList, ListUpdateHelperOptions options)
			: this(recordList, options.ParentForWaitCursor)
		{
			SkipShowRecord = options.SkipShowRecord;
			m_recordList.SuppressSaveOnChangeRecord = options.SuppressSaveOnChangeRecord;
			ClearBrowseListUntilReload = options.ClearBrowseListUntilReload;
			TriggerPendingReloadOnDispose = !options.SuspendPendingReloadOnDispose;
			m_recordList.UpdatingList = options.SuspendPropChangedDuringModification;
		}

		/// <summary />
		/// <param name="recordList">clerk we want to suspend reloading for. if null, we don't do anything.</param>
		internal ListUpdateHelper(IRecordList recordList)
			: this(recordList, recordList != null && recordList.ListLoadingSuppressed)
		{
		}

		/// <summary />
		/// <param name="recordList">clerk we want to suspend reloading for. if null, we don't do anything.</param>
		/// <param name="fWasAlreadySuppressed">Usually, clerk.ListLoadingSuppressed. When we know we just
		/// created the clerk, already in a suppressed state, and want to treat it as if this
		/// list update helper did the suppressing, pass false, even though the list may in fact be already suppressed.</param>
		internal ListUpdateHelper(IRecordList recordList, bool fWasAlreadySuppressed)
		{
			m_recordList = recordList;
			if (m_recordList != null)
			{
				m_fOriginalUpdatingList = m_recordList.UpdatingList;
				m_fOriginalListLoadingSuppressedState = fWasAlreadySuppressed;
				m_fOriginalSkipRecordNavigationState = m_recordList.SkipShowRecord;
				m_fOriginalSuppressSaveOnChangeRecord = m_recordList.SuppressSaveOnChangeRecord;
				m_fOriginalLoadRequestedWhileSuppressed = m_recordList.RequestedLoadWhileSuppressed;
				// monitor whether ReloadList was requested during the life of this ListUpdateHelper
				m_recordList.RequestedLoadWhileSuppressed = false;

				m_originalUpdateHelper = m_recordList.UpdateHelper;
				// if we're already suppressing the list, we don't want to auto reload since
				// the one who is suppressing the list expects to be able to handle that later.
				// or if the parent clerk is suppressing, we should wait until the parent reloads.
				var parentList = recordList.ParentList;
				if (m_fOriginalListLoadingSuppressedState ||
				    parentList != null && parentList.ListLoadingSuppressed)
				{
					m_fTriggerPendingReloadOnDispose = false;
				}
				m_recordList.ListLoadingSuppressedNoSideEffects = true;
				m_recordList.UpdateHelper = this;
			}
		}

		/// <summary>
		/// Indicates whether the list needs to reload as a side effect of PropChanges
		/// </summary>
		internal bool NeedToReloadList => m_recordList.RequestedLoadWhileSuppressed;

		/// <summary>
		/// Indicate that we want to clear browse items while we are
		/// waiting for a pending reload, so that the display will not
		/// try to access invalid objects.
		/// </summary>
		internal bool ClearBrowseListUntilReload { get; set; }

		/// <summary>
		/// Some user actions (e.g. editing) should not result in record navigation
		/// because it may cause the editing pane to disappear
		/// (thus losing the user's place in editing). This is used by ListUpdateHelper
		/// to skip record navigations while such user actions are taking place.
		/// </summary>
		internal bool SkipShowRecord
		{
			get
			{
				return m_recordList != null && m_recordList.SkipShowRecord;
			}
			set
			{
				if (m_recordList != null)
					m_recordList.SkipShowRecord = value;
			}
		}

		/// <summary>
		/// Set to false if you don't want to automatically reload pending reload OnDispose.
		/// true, by default.
		/// </summary>
		private bool m_fTriggerPendingReloadOnDispose = true;
		internal bool TriggerPendingReloadOnDispose
		{
			get { return m_fTriggerPendingReloadOnDispose; }
			set { m_fTriggerPendingReloadOnDispose = value; }
		}

		/// <summary>
		/// The list was successfully restored (from a persisted sort sequence).
		/// We should NOT sort it when disposed, nor restore an original flag indicating it needed sorting.
		/// </summary>
		internal void ListWasRestored()
		{
			m_fTriggerPendingReloadOnDispose = false;
			m_fOriginalLoadRequestedWhileSuppressed = false;
		}
		#region DisposableBase Members

		protected override void DisposeManagedResources()
		{
			m_waitCursor?.Dispose();
			if (m_recordList != null)
			{
				var fHandledReload = false;
				if (m_fTriggerPendingReloadOnDispose && m_recordList.RequestedLoadWhileSuppressed)
				{
					m_recordList.ListLoadingSuppressed = m_fOriginalListLoadingSuppressedState;
					// if the requested while suppressed flag was reset, we handled it.
					if (m_recordList.RequestedLoadWhileSuppressed == false)
					{
						fHandledReload = true;
					}
				}
				else
				{
					m_recordList.ListLoadingSuppressedNoSideEffects = m_fOriginalListLoadingSuppressedState;
				}
				// if we didn't handle a pending reload, someone else needs to handle it.
				if (!fHandledReload)
				{
					m_recordList.RequestedLoadWhileSuppressed |= m_fOriginalLoadRequestedWhileSuppressed;
				}

				m_recordList.UpdatingList = m_fOriginalUpdatingList;
				// reset this after we possibly reload the list.
				m_recordList.SkipShowRecord = m_fOriginalSkipRecordNavigationState;
				m_recordList.SuppressSaveOnChangeRecord = m_fOriginalSuppressSaveOnChangeRecord;
				m_recordList.UpdateHelper = m_originalUpdateHelper;
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			m_recordList = null;
			m_waitCursor = null;
		}

		#endregion DisposableBase
	}
}