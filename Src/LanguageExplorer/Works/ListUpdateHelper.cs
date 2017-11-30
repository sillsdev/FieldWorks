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
		IRecordClerk m_clerk;
		private WaitCursor m_waitCursor = null;
		readonly bool m_fOriginalUpdatingList = false;
		readonly bool m_fOriginalListLoadingSuppressedState = false;
		readonly bool m_fOriginalSkipRecordNavigationState = false;
		private bool m_fOriginalLoadRequestedWhileSuppressed = false;
		readonly bool m_fOriginalSuppressSaveOnChangeRecord;
		readonly ListUpdateHelper m_originalUpdateHelper = null;

		internal ListUpdateHelper(IRecordList list, Control parentForWaitCursor)
			: this(list as IRecordClerk, parentForWaitCursor)
		{
		}

		/// <summary />
		internal ListUpdateHelper(IRecordClerk clerk, Control parentForWaitCursor)
			: this(clerk)
		{
			if (parentForWaitCursor != null)
			{
				m_waitCursor = new WaitCursor(parentForWaitCursor);
			}
		}

		/// <summary />
		internal ListUpdateHelper(IRecordClerk clerk, ListUpdateHelperOptions options)
			: this(clerk, options.ParentForWaitCursor)
		{
			SkipShowRecord = options.SkipShowRecord;
			m_clerk.SuppressSaveOnChangeRecord = options.SuppressSaveOnChangeRecord;
			ClearBrowseListUntilReload = options.ClearBrowseListUntilReload;
			TriggerPendingReloadOnDispose = !options.SuspendPendingReloadOnDispose;
			m_clerk.MyRecordList.UpdatingList = options.SuspendPropChangedDuringModification;
		}

		/// <summary />
		/// <param name="clerk">clerk we want to suspend reloading for. if null, we don't do anything.</param>
		internal ListUpdateHelper(IRecordClerk clerk)
			: this(clerk, clerk != null && clerk.ListLoadingSuppressed)
		{
		}

		/// <summary />
		/// <param name="clerk">clerk we want to suspend reloading for. if null, we don't do anything.</param>
		/// <param name="fWasAlreadySuppressed">Usually, clerk.ListLoadingSuppressed. When we know we just
		/// created the clerk, already in a suppressed state, and want to treat it as if this
		/// list update helper did the suppressing, pass false, even though the list may in fact be already suppressed.</param>
		internal ListUpdateHelper(IRecordClerk clerk, bool fWasAlreadySuppressed)
		{
			m_clerk = clerk;
			if (m_clerk != null)
			{
				m_fOriginalUpdatingList = m_clerk.MyRecordList.UpdatingList;
				m_fOriginalListLoadingSuppressedState = fWasAlreadySuppressed;
				m_fOriginalSkipRecordNavigationState = m_clerk.SkipShowRecord;
				m_fOriginalSuppressSaveOnChangeRecord = m_clerk.SuppressSaveOnChangeRecord;
				m_fOriginalLoadRequestedWhileSuppressed = m_clerk.RequestedLoadWhileSuppressed;
				// monitor whether ReloadList was requested during the life of this ListUpdateHelper
				m_clerk.MyRecordList.RequestedLoadWhileSuppressed = false;

				m_originalUpdateHelper = m_clerk.UpdateHelper;
				// if we're already suppressing the list, we don't want to auto reload since
				// the one who is suppressing the list expects to be able to handle that later.
				// or if the parent clerk is suppressing, we should wait until the parent reloads.
				var parentClerk = clerk.ParentClerk;
				if (m_fOriginalListLoadingSuppressedState ||
				    parentClerk != null && parentClerk.ListLoadingSuppressed)
				{
					m_fTriggerPendingReloadOnDispose = false;
				}
				m_clerk.ListLoadingSuppressedNoSideEffects = true;
				m_clerk.UpdateHelper = this;
			}
		}

		/// <summary>
		/// Indicates whether the list needs to reload as a side effect of PropChanges
		/// </summary>
		internal bool NeedToReloadList => m_clerk.RequestedLoadWhileSuppressed;

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
				return m_clerk != null && m_clerk.SkipShowRecord;
			}
			set
			{
				if (m_clerk != null)
					m_clerk.SkipShowRecord = value;
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
			if (m_clerk != null && !m_clerk.IsDisposed)
			{
				var fHandledReload = false;
				if (m_fTriggerPendingReloadOnDispose && m_clerk.MyRecordList.RequestedLoadWhileSuppressed)
				{
					m_clerk.ListLoadingSuppressed = m_fOriginalListLoadingSuppressedState;
					// if the requested while suppressed flag was reset, we handled it.
					if (m_clerk.MyRecordList.RequestedLoadWhileSuppressed == false)
					{
						fHandledReload = true;
					}
				}
				else
				{
					m_clerk.ListLoadingSuppressedNoSideEffects = m_fOriginalListLoadingSuppressedState;
				}
				// if we didn't handle a pending reload, someone else needs to handle it.
				if (!fHandledReload)
				{
					m_clerk.MyRecordList.RequestedLoadWhileSuppressed |= m_fOriginalLoadRequestedWhileSuppressed;
				}

				m_clerk.MyRecordList.UpdatingList = m_fOriginalUpdatingList;
				// reset this after we possibly reload the list.
				m_clerk.SkipShowRecord = m_fOriginalSkipRecordNavigationState;
				m_clerk.SuppressSaveOnChangeRecord = m_fOriginalSuppressSaveOnChangeRecord;
				m_clerk.UpdateHelper = m_originalUpdateHelper;
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			m_clerk = null;
			m_waitCursor = null;
		}

		#endregion DisposableBase
	}
}