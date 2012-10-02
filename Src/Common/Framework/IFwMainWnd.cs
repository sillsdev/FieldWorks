// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IFwMainWnd.cs
// Responsibility: FW Team
// Last reviewed:
// --------------------------------------------------------------------------------------------
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Framework
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for FW main windows to avoid all kinds of exciting circular dependencies
	/// and to allow different apps to implement their main windows totally differently.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public interface IFwMainWnd
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IRootSite ActiveView { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the data objects cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FdoCache Cache { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application to which this main winodw belongs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FwApp App { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add correspnding stuff to the sidebar, View menu,
		/// etc. Subclasses must override this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InitAndShowClient();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Rectangle NormalStateDesktopBounds
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		void PreSynchronize(SyncMsg sync);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		/// ------------------------------------------------------------------------------------
		bool Synchronize(SyncMsg sync);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool OnFinishedInit();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		bool OnEditFind(object args);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views that belong to this main window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RefreshAllViews();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Close();
	}
}