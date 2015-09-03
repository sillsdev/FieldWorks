using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Interface for a Flex Application, which builds on IApp
	/// </summary>
	public interface IFlexApp : IApp
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
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>New instance of main window if successful; otherwise <c>null</c></returns>
		Form NewMainAppWnd(IProgress progressDlg, bool fNewCache, Form wndCopyFrom, bool fOpeningNewProject);

		/// <summary>
		/// Registers events for the main window and adds the main window to the list of
		/// windows. Then shows the window.
		/// </summary>
		/// <param name="fwMainWindow">The new main window.</param>
		/// <param name="wndCopyFrom">Form to copy from, or <c>null</c></param>
		void InitAndShowMainWindow(Form fwMainWindow, Form wndCopyFrom);

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
		/// Get the FDO cache.
		/// </summary>
		FdoCache Cache { get; }

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
		/// Save any settings.
		/// </summary>
		/// <remarks>
		/// This is the real place to save settings, as opposed to SaveSettingsNow, which is
		/// a dummy implementation required because (for the sake of the SettingsKey method)
		/// we implement ISettings.
		/// </remarks>
		void SaveSettings();

		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		string SampleDatabase { get; }
	}
}