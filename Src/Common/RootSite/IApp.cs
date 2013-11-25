// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IApp.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Interface for application.
	/// TODO: The only place this interface is used in RootSite is in SyncUndoAction. The only
	/// place that SyncUndoAction is used is in FrameWork. This means that IApp could be moved
	/// to a better place.
	/// </summary>
	public interface IApp
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		string ResourceString(string stid);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Registry key for settings for this application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		RegistryKey SettingsKey { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the measurement system used in the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		MsrSysType MeasurementSystem { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the active form. This is usually the same as Form.ActiveForm, but sometimes
		/// the official active form is something other than one of our main windows, for
		/// example, a dialog or popup menu. This is always one of our real main windows,
		/// which should be something that has a taskbar icon. It is often useful as the
		/// appropriate parent window for a dialog that otherwise doesn't have one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Form ActiveMainWindow { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ApplicationName { get; }

		/// <summary>
		/// A place to get various pictures.
		/// </summary>
		PictureHolder PictureHolder { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in all of the Main Windows of the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RefreshAllViews();

		/// <summary>
		/// Restart the spell-checking process (e.g. when dictionary changed)
		/// </summary>
		void RestartSpellChecking();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cycle through the applications main windows and synchronize them with database
		/// changes.
		/// </summary>
		/// <param name="sync">synchronization information record</param>
		/// <returns><c>true</c> to continue processing; set to <c>false</c> to prevent
		/// processing of subsequent sync messages. </returns>
		/// ------------------------------------------------------------------------------------
		bool Synchronize(SyncMsg sync);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// To participate in automatic synchronization from the database (calling SyncFromDb
		/// in a useful manner) and application must override this, providing a unique Guid.
		/// Typically this is the Guid defined by a static AppGuid method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Guid SyncGuid { get; }

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Enable or disable all top-level windows. This allows nesting. In other words,
		/// calling EnableMainWindows(false) twice requires 2 calls to EnableMainWindows(true)
		/// before the top level windows are actually enabled.
		/// </summary>
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		/// -----------------------------------------------------------------------------------
		void EnableMainWindows(bool fEnable);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes and disposes of the find replace dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RemoveFindReplaceDialog();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the Find/Replace modeless dialog
		/// </summary>
		/// <param name="fReplace"><c>true</c> to make the replace tab active</param>
		/// <param name="rootsite">The view where the find will be conducted</param>
		/// <returns><c>true</c> if the dialog is successfully displayed</returns>
		/// ------------------------------------------------------------------------------------
		bool ShowFindReplaceDialog(bool fReplace, RootSite rootsite);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles an outgoing link request from this application.
		/// </summary>
		/// <param name="link">The link.</param>
		/// ------------------------------------------------------------------------------------
		void HandleOutgoingLink(FwAppArgs link);

		/// <summary>
		/// Gets the support email address.
		/// </summary>
		/// <value>The support email address.</value>
		string SupportEmailAddress { get; }
	}
}
