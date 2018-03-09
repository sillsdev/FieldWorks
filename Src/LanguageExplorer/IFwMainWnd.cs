// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for FW main windows to avoid all kinds of exciting circular dependencies
	/// and to allow different apps to implement their main windows totally differently.
	/// </summary>
	/// <remarks>In normal operations, an IFwMainWnd implementation expects to be cast to Form.</remarks>
	internal interface IFwMainWnd : IDisposable, IApplicationIdleEventHandler, IPropertyTableProvider, IPublisherProvider, ISubscriberProvider, IRecordListOwner, IIdleQueueProvider
	{
		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		IRootSite ActiveView { get; }

		/// <summary>
		/// Gets the focused control of the window
		/// </summary>
		Control FocusedControl { get; }

		/// <summary>
		/// Gets the data objects cache.
		/// </summary>
		LcmCache Cache { get; }

		/// <summary>
		/// Initialize the window, before being shown.
		/// </summary>
		/// <param name="windowIsCopy">Window is being copied.</param>
		/// <param name="linkArgs">Optional arguments used to set up the new instance.</param>
		/// <remarks>
		/// This allows for creating all sorts of things used by the implementation.
		/// </remarks>
		void Initialize(bool windowIsCopy = false, FwLinkArgs linkArgs = null);

		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		Rectangle NormalStateDesktopBounds
		{
			get;
		}

		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		void PreSynchronize(SyncMsg sync);

		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		bool Synchronize(SyncMsg sync);

		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		bool OnFinishedInit();

		/// <summary>
		/// Prepare to refresh the main window and its IAreas and ITools.
		/// </summary>
		void PrepareToRefresh();

		/// <summary>
		/// Finish refreshing the main window and its IAreas and ITools.
		/// </summary>
		/// <remarks>
		/// This should call Refresh on real window implementations,
		/// after everything else is done.</remarks>
		void FinishRefresh();

		/// <summary>
		/// Refreshes all the views that belong to this main window
		/// </summary>
		void RefreshAllViews();

		/// <summary>
		/// Closes this instance.
		/// </summary>
		void Close();

		/// <summary>
		/// Save settings.
		/// </summary>
		void SaveSettings();

		/// <summary>
		/// Get the RecordBar (as a Control), or null if not present.
		/// </summary>
		IRecordBar RecordBarControl { get; }

		/// <summary>
		/// Get the TreeView of RecordBarControl, or null if not present, or it is not showng a tree.
		/// </summary>
		TreeView TreeStyleRecordList { get; }

		/// <summary>
		/// Get the ListView of RecordBarControl, or null if not present, or it is not showing a list.
		/// </summary>
		ListView ListStyleRecordList { get; }
	}
}