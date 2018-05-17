// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for a Flex Application, which builds on IApp
	/// </summary>
	internal interface IFlexApp : IApp
	{
		/// <summary>
		/// Get the main windows.
		/// </summary>
		List<IFwMainWnd> MainWindows { get; }

		/// <summary>
		/// Activate the given window.
		/// </summary>
		/// <param name="iMainWnd">Index (in the internal list of main windows) of the window to
		/// activate</param>
		void ActivateWindow(int iMainWnd);

		/// <summary>
		/// Creates a new instance of the main window
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use, if needed (can be null).</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <returns>New instance of main window if successful; otherwise <c>null</c></returns>
		Form NewMainAppWnd(IProgress progressDlg, bool fNewCache, IFwMainWnd wndCopyFrom);

		/// <summary />
		void InitializePartInventories(IProgress progressDlg, bool fLoadUserOverrides);

		/// <summary>
		/// Closes and re-opens the argument window, in the same place, as a drastic way of applying new settings.
		/// </summary>
		void ReplaceMainWindow(IFwMainWnd wndActive);

		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use.</param>
		void DoApplicationInitialization(IProgress progressDlg);

		/// <summary>
		/// Called just after DoApplicationInitialization() to load the settings.
		/// </summary>
		void LoadSettings();

		/// <summary>
		/// Provides a hook for initializing the cache in application-specific ways.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the initialization was successful, false otherwise</returns>
		bool InitCacheForApp(IThreadedProgress progressDlg);

		/// <summary>
		/// Gets the FieldWorks manager for this application.
		/// </summary>
		IFieldWorksManager FwManager { get; }

		/// <summary>
		/// Removes the specified IFwMainWnd from the list of windows. If it is ok to close down
		/// the application and the count of main windows is zero, then this method will also
		/// shut down the application.
		/// </summary>
		/// <param name="fwMainWindow">The IFwMainWnd to remove</param>
		void RemoveWindow(IFwMainWnd fwMainWindow);

		/// <summary>
		/// Gets a value indicating whether this instance has a modal dialog or message box open.
		/// </summary>
		bool IsModalDialogOpen { get; }

		/// <summary>
		/// Gets the full path of the product executable filename
		/// </summary>
		string ProductExecutableFile { get; }

		/// <summary>
		/// Gets a value indicating whether this instance has been fully initialized.
		/// </summary>
		bool HasBeenFullyInitialized { get; }

		/// <summary>
		/// Gets the registry settings for this FlexApp
		/// </summary>
		FwRegistrySettings RegistrySettings { get; }

		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		string GetResourceString(string stid);

		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		string SampleDatabase { get; }

		/// <summary>
		/// Command line arguments.
		/// </summary>
		FwAppArgs FwAppArgs { set; }

		/// <summary>
		/// Gets the classname used for setting the WM_CLASS on Linux
		/// </summary>
		string WindowClassName { get; }
	}
}