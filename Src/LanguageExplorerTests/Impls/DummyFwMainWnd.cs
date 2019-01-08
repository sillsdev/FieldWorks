// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorerTests.Impls
{
	/// <summary>
	/// A bare bones implementation of IFwMainWnd.
	/// </summary>
	/// <remarks>
	/// Methods are implemented only when needed for some test.
	/// </remarks>
	internal sealed class DummyFwMainWnd : Form, IFwMainWnd
	{
		#region Implementation of IDisposable
		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		void IDisposable.Dispose()
		{ }
		#endregion

		#region Implementation of IPropertyTableProvider
		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		IPropertyTable IPropertyTableProvider.PropertyTable
		{
			get { throw new NotImplementedException(); }
		}
		#endregion

		#region Implementation of IPublisherProvider
		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		IPublisher IPublisherProvider.Publisher
		{
			get { throw new NotImplementedException(); }
		}
		#endregion

		#region Implementation of ISubscriberProvider
		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		ISubscriber ISubscriberProvider.Subscriber
		{
			get { throw new NotImplementedException(); }
		}
		#endregion

		#region Implementation of IRecordListOwner
		/// <summary>Find the IRecordListUpdater object with the given name.</summary>
		IRecordListUpdater IRecordListOwner.FindRecordListUpdater(string name)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Implementation of IIdleQueueProvider
		/// <summary>
		/// Get the IdleQueue instance, which is a singleton per IFwMainWnd instance.
		/// </summary>
		IdleQueue IIdleQueueProvider.IdleQueue
		{
			get { throw new NotImplementedException(); }
		}
		#endregion

		#region Implementation of IFwMainWnd
		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		IRootSite IFwMainWnd.ActiveView
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Gets the focused control of the window
		/// </summary>
		Control IFwMainWnd.FocusedControl
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Gets the data objects cache.
		/// </summary>
		LcmCache IFwMainWnd.Cache
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Create the client windows and add corresponding stuff to the sidebar, View menu,  etc.
		/// </summary>
		void IFwMainWnd.Initialize(bool windowIsCopy, FwLinkArgs linkArgs)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		Rectangle IFwMainWnd.NormalStateDesktopBounds
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Called just before a window synchronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">synchronization message</param>
		void IFwMainWnd.PreSynchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a window synchronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">synchronization message</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		bool IFwMainWnd.Synchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		bool IFwMainWnd.OnFinishedInit()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Prepare to refresh the main window and its IAreas and ITools.
		/// </summary>
		void IFwMainWnd.PrepareToRefresh()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Finish refreshing the main window and its IAreas and ITools.
		/// </summary>
		/// <remarks>
		/// This should call Refresh on real window implementations,
		/// after everything else is done.</remarks>
		void IFwMainWnd.FinishRefresh()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Refreshes all the views that belong to this main window
		/// </summary>
		void IFwMainWnd.RefreshAllViews()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		void IFwMainWnd.Close()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		void IFwMainWnd.SaveSettings()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		void IApplicationIdleEventHandler.SuspendIdleProcessing()
		{
		}

		/// <summary>
		/// See SuspendIdleProcessing.
		/// </summary>
		void IApplicationIdleEventHandler.ResumeIdleProcessing()
		{
		}

		/// <summary>
		/// Get the RecordBar (as a Control), or null if not present.
		/// </summary>
		IRecordBar IFwMainWnd.RecordBarControl => null;

		/// <summary>
		/// Get the TreeView of RecordBarControl, or null if not present, or it is not showing a tree.
		/// </summary>
		TreeView IFwMainWnd.TreeStyleRecordList => null;

		/// <summary>
		/// Get the ListView of RecordBarControl, or null if not present, or it is not showing a list.
		/// </summary>
		ListView IFwMainWnd.ListStyleRecordList => null;

		#endregion

		#region Implementation of IVwNotifyChange
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
		}
		#endregion
	}
}