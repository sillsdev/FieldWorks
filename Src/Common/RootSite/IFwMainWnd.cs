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
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Interface for FwMainWnd
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for FwMainWnd that FwApp uses.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public interface IFwMainWnd
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IRootSite ActiveView
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the data objects cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FdoCache Cache
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ApplicationName
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add correspnding stuff to the sidebar, View menu,
		/// etc. Subclasses must override this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InitAndShowClient();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable or disable this window.
		/// </summary>
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		/// ------------------------------------------------------------------------------------
		void EnableWindow(bool fEnable);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save all data in this window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SaveData();

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
		/// <param name="sync">syncronization information record</param>
		/// ------------------------------------------------------------------------------------
		bool PreSynchronize(SyncInfo sync);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization information record</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		/// ------------------------------------------------------------------------------------
		bool Synchronize(SyncInfo sync);

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
