// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IFwMainWnd.cs
// Responsibility: FW Team
// Last reviewed:
// --------------------------------------------------------------------------------------------
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for FW main windows to avoid all kinds of exciting circular dependencies
	/// and to allow different apps to implement their main windows totally differently.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public interface IFwMainWnd : IxWindow
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
		LcmCache Cache { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application to which this main window belongs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FwApp App { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add corresponding stuff to the sidebar, View menu,
		/// etc. Subclasses must override this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InitAndShowClient();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window synchronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		/// ------------------------------------------------------------------------------------
		void Synchronize();

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