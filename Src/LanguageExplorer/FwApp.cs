// Copyright (c) 2002-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace LanguageExplorer
{
	#region FwApp class
	/// ---------------------------------------------------------------------------------------
	/// <remarks>
	/// Base application for .net FieldWorks apps (i.e., replacement for AfApp)
	/// </remarks>
	/// ---------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification = "m_activeMainWindow variable is a reference")]
	public abstract class FwApp : IFlexApp
	{
		#region SuppressedCacheInfo class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper class that contains queued SyncMsgs and a reference count for
		/// Suppress/ResumeSynchronize.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class SuppressedCacheInfo
		{
			/// <summary>Reference count</summary>
			public int Count = 1;
			/// <summary>SyncMsg queue</summary>
			public Queue<SyncMsg> Queue = new Queue<SyncMsg>();
		}
		#endregion

		#region Member variables

		/// <summary>
		/// A picture holder that may be used to retrieve various useful pictures.
		/// </summary>
		public PictureHolder PictureHolder { get; private set; }

		private IHelpTopicProvider m_helpTopicProvider;

		private bool m_fInitialized = false;
		private bool m_fInModalState = false;

		/// <summary></summary>
		protected List<IFwMainWnd> m_rgMainWindows = new List<IFwMainWnd>(1);
		/// <summary>
		/// One of m_rgMainWindows, the one most recently activated.
		/// </summary>
		protected Form m_activeMainWindow;
		/// <summary></summary>
		private int m_nEnableLevel;
		/// <summary>
		/// The FieldWorks manager for dealing with FieldWorks-level stuff.
		/// </summary>
		protected IFieldWorksManager m_fwManager;
		/// <summary></summary>
		protected FwFindReplaceDlg m_findReplaceDlg;

#if DEBUG
		/// <summary></summary>
		protected DebugProcs m_debugProcs;
#endif
		private FwRegistrySettings m_registrySettings;
		private SuppressedCacheInfo m_suppressedCacheInfo;
		/// <summary>
		/// null means that we are not suppressing view refreshes.
		/// True means we're suppressing and we need to do a refresh when finished.
		/// False means we're suppressing, but have no need to do a refresh when finished.
		/// </summary>
		private bool? m_refreshView;

		/// <summary>The find patterns for the find/replace dialog, one for each database.</summary>
		/// <remarks>We need one pattern per database (cache). Otherwise it'll crash when we try to
		/// load the previous search term because the new database has different writing system hvos
		/// (TE-5598).</remarks>
		protected IVwPattern m_findPattern;
		#endregion

		#region Construction and Initializing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for FwApp
		/// </summary>
		/// <param name="fwManager">The FieldWorks manager for dealing with FieldWorks-level
		/// stuff.</param>
		/// <param name="helpTopicProvider">An application-specific help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		protected FwApp(IFieldWorksManager fwManager, IHelpTopicProvider helpTopicProvider)
		{
			PictureHolder = new PictureHolder();
			m_fwManager = fwManager;
			m_helpTopicProvider = helpTopicProvider;
#if DEBUG
			m_debugProcs = new DebugProcs();
#endif
			m_registrySettings = new FwRegistrySettings(this);
			m_registrySettings.LatestAppStartupTime = DateTime.Now.ToUniversalTime().Ticks.ToString();
			m_registrySettings.AddErrorReportingInfo();

			Application.EnterThreadModal += Application_EnterThreadModal;
			Application.LeaveThreadModal += Application_LeaveThreadModal;

			Application.AddMessageFilter(this);
		}

		/// <summary>
		/// Closes and re-opens the argument window, in the same place, as a drastic way of applying new settings.
		/// </summary>
		public virtual void ReplaceMainWindow(IFwMainWnd wndActive)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void DoApplicationInitialization(IProgress progressDlg)
		{
			// Application.EnableVisualStyles();
		}

		/// <summary>
		/// Provides a hook for initializing the cache in application-specific ways.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the initialization was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public abstract bool InitCacheForApp(IThreadedProgress progressDlg);


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just after DoApplicationInitialization(). Allows a separate overide of
		/// loading settings. If you override this you probably also want to
		/// override SaveSettings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void LoadSettings()
		{
			// Keyman has an option to change the system keyboard with a keyman keyboard change.
			// Unfortunately, this messes up changing writing systems in FieldWorks when Keyman
			// is running. The only fix seems to be to turn that option off... (TE-8686)
			try
			{
				using (RegistryKey engineKey = Registry.CurrentUser.OpenSubKey(
					@"Software\Tavultesoft\Keyman Engine", false))
				{
					if (engineKey != null)
					{
						foreach (string version in engineKey.GetSubKeyNames())
						{
							using (RegistryKey keyVersion = engineKey.OpenSubKey(version, true))
							{
								if (keyVersion != null)
								{
									object value = keyVersion.GetValue("switch language with keyboard");
									if (value == null || (int)value != 0)
										keyVersion.SetValue("switch language with keyboard", 0);
								}
							}
						}
					}
				}
			}
			catch (SecurityException)
			{
				// User doesn't have access to the registry key, so just hope the user is fine
				// with what he gets.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save any settings.
		/// </summary>
		/// <remarks>
		/// This is the real place to save settings, as opposed to SaveSettingsNow, which is
		/// a dummy implementation required because (for the sake of the SettingsKey method)
		/// we implement ISettings.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void SaveSettings()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registers events for the main window and adds the main window to the list of
		/// windows. Then shows the window.
		/// </summary>
		/// <param name="fwMainWindow">The new main window.</param>
		/// <param name="wndCopyFrom">Form to copy from, or <c>null</c></param>
		/// ------------------------------------------------------------------------------------
		public void InitAndShowMainWindow(Form fwMainWindow, Form wndCopyFrom)
		{
			CheckDisposed();

			IFwMainWnd fwMainWnd = fwMainWindow as IFwMainWnd;
			Debug.Assert(fwMainWnd != null);
			fwMainWindow.Closing += OnClosingWindow;
			fwMainWindow.Closed += OnWindowClosed;
			m_rgMainWindows.Add(fwMainWnd);
			fwMainWindow.Activated += fwMainWindow_Activated;
			if (fwMainWindow == Form.ActiveForm)
				m_activeMainWindow = fwMainWindow;
			fwMainWindow.HandleDestroyed += fwMainWindow_HandleDestroyed;
			fwMainWindow.Show(); // Show method loads persisted settings for window & controls
			fwMainWindow.Activate(); // This makes main window come to front after splash screen closes

			// adjust position if this is an additional window
			if (wndCopyFrom != null)
			{
				AdjustNewWindowPosition(fwMainWindow, wndCopyFrom);
				// TODO BryanW: see AfMdiMainWnd::CmdWndNew() for other items that need to be
				// coordinated
			}
			else if (fwMainWindow.WindowState != FormWindowState.Maximized)
			{
				// Fix the stored position in case it is off the screen.  This can happen if the
				// user has removed a second monitor, or changed the screen resolution downward,
				// since the last time he ran the program.  (See LT-1083.)
				Rectangle rcNewWnd = fwMainWindow.DesktopBounds;
				//				Rectangle rcScrn = Screen.FromRectangle(rcNewWnd).WorkingArea;
				ScreenUtils.EnsureVisibleRect(ref rcNewWnd);
				fwMainWindow.DesktopBounds = rcNewWnd;
				fwMainWindow.StartPosition = FormStartPosition.Manual;
			}

			((IFwMainWnd)fwMainWindow).InitAndShowClient();

			m_fInitialized = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the new window position - offset right and down from the original.
		/// Also copy the window size, state, and set the StartPosition mode to manual.
		/// </summary>
		/// <param name="wndNew"></param>
		/// <param name="wndCopyFrom"></param>
		/// -----------------------------------------------------------------------------------
		protected void AdjustNewWindowPosition(Form wndNew, Form wndCopyFrom)
		{
			Debug.Assert(wndNew is IFwMainWnd,
				"Form passed as parameter to AdjustNewWindowPosition has to implement IFwMainWnd");
			Debug.Assert(wndCopyFrom is IFwMainWnd,
				"Form passed as parameter to AdjustNewWindowPosition has to implement IFwMainWnd");

			// Get position and size
			Rectangle rcNewWnd = wndCopyFrom.DesktopBounds;

			// However, desktopBounds are not useful when window is maximized; in that case
			// get the info from Persistence instead... NormalStateDesktopBounds
			if (wndCopyFrom.WindowState == FormWindowState.Maximized)
			{
				// Here we subtract twice the caption height, which with the offset below insets it all around.
				rcNewWnd.Width -=  SystemInformation.CaptionHeight * 2;
				rcNewWnd.Height -=  SystemInformation.CaptionHeight * 2;
				// JohnT: this old approach fails if the old window's position has never been
				// persisted. NormalStateDesktopBounds crashes, not finding anything in the
				// property table.
				//				rcNewWnd = ((IFwMainWnd)wndCopyFrom).NormalStateDesktopBounds;
			}

			//Offset right and down
			rcNewWnd.X += SystemInformation.CaptionHeight;
			rcNewWnd.Y += SystemInformation.CaptionHeight;

			// We we will check if we went too far right or down, as Word 2002 checks.
			// If rcNewWnd is beyond bottom or right of screen...
			// Get the working area of the screen on which the new window will be placed.
			//Rectangle rcScrn = Screen.FromRectangle(rcNewWnd).WorkingArea;

			// If our adjusted rcNewWnd is partly off the screen, move it so it is fully
			// on the screen its mostly on. Note: this will only be necessary when the window
			// being copied from is partly off the screen in a single monitor system or
			// spanning multiple monitors in a multiple monitor system.
			ScreenUtils.EnsureVisibleRect(ref rcNewWnd);

			// Set the properties of the new window
			wndNew.DesktopBounds = rcNewWnd;
			wndNew.StartPosition = FormStartPosition.Manual;
			wndNew.WindowState = wndCopyFrom.WindowState;
		}

		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set;}

		/// <summary>
		/// See if the object is being disposed.
		/// </summary>
		/// <remarks>
		/// Don't make a setter for this, since we don't want anyone else to set it.
		/// </remarks>
		public bool BeingDisposed { get; private set;}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwApp()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed || BeingDisposed)
				return;
			BeingDisposed = true;

			if (disposing)
			{
				UpdateAppRuntimeCounter();

				Logger.WriteEvent("Disposing app: " + GetType().Name);
				RegistrySettings.FirstTimeAppHasBeenRun = false;

				// Dispose managed resources here.
				List<IFwMainWnd> mainWnds = new List<IFwMainWnd>(m_rgMainWindows); // Use another array, since m_rgMainWindows may change.
				m_rgMainWindows.Clear(); // In fact, just clear the main array, so the windows won't have to worry so much.
				foreach (IFwMainWnd mainWnd in mainWnds)
				{
					if (mainWnd is Form)
					{
						Form wnd = (Form)mainWnd;
						wnd.Closing -= OnClosingWindow;
						wnd.Closed -= OnWindowClosed;
						wnd.Activated -= fwMainWindow_Activated;
						wnd.HandleDestroyed -= fwMainWindow_HandleDestroyed;
						wnd.Dispose();
					}
					else if (mainWnd is IDisposable)
						((IDisposable)mainWnd).Dispose();
				}
				if (m_findReplaceDlg != null)
					m_findReplaceDlg.Dispose();
#if DEBUG
				if (m_debugProcs != null)
					m_debugProcs.Dispose();
#endif

				ResourceHelper.ShutdownHelper();

				if (m_registrySettings != null)
					m_registrySettings.Dispose();

				Application.EnterThreadModal -= Application_EnterThreadModal;
				Application.LeaveThreadModal -= Application_LeaveThreadModal;

				Application.RemoveMessageFilter(this);
				PictureHolder.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rgMainWindows = null;
			m_activeMainWindow = null;
			m_registrySettings = null;
			m_findPattern = null;
			m_findReplaceDlg = null;
			m_suppressedCacheInfo = null;
			m_refreshView = null;
			PictureHolder = null;
			IsDisposed = true;
			BeingDisposed = false;
		}

		private void UpdateAppRuntimeCounter()
		{
			int csec = RegistrySettings.TotalAppRuntime;
			string sStartup = RegistrySettings.LatestAppStartupTime;
			long start;
			if (!String.IsNullOrEmpty(sStartup) && long.TryParse(sStartup, out start))
			{
				DateTime started = new DateTime(start);
				DateTime finished = DateTime.Now.ToUniversalTime();
				TimeSpan delta = finished - started;
				csec += (int)delta.TotalSeconds;
				RegistrySettings.TotalAppRuntime = csec;
			}
		}

		#endregion IDisposable & Co. implementation

		#region Event handlers related to closing windows (delegates for FwMainWnd)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a window is closed, we need to make sure we close any root boxes that may
		/// be on the window.
		/// </summary>
		/// <param name="sender">Presumably a main window</param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnWindowClosed(object sender, EventArgs e)
		{
			CloseRootBoxes(sender as Control);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively look at all the controls belonging to the specified control and save
		/// the settings for each root box for controls of type ISettings. Then close
		/// the root box for controls of type IRootSite. Ideally IRootSite controls should
		/// close their root boxes in the OnHandleDestroyed event, but since sometimes IRootSite
		/// controls are created but never shown (which means their handle is never created),
		/// we have to close the rootboxes here instead.
		/// </summary>
		/// <param name="ctrl">A main window or any of its descendents</param>
		/// ------------------------------------------------------------------------------------
		private void CloseRootBoxes(Control ctrl)
		{
			if (ctrl != null)
			{
				if (ctrl is ISettings)
					((ISettings)ctrl).SaveSettingsNow();

				if (ctrl is IRootSite)
					((IRootSite)ctrl).CloseRootBox();

				foreach (Control childControl in ctrl.Controls)
					CloseRootBoxes(childControl);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The active main window is closing, so try to find some other main window that can
		/// "own" the find/replace dialog, so it can stay alive.
		/// If we can't find one, then all main windows are going away and we're going
		/// to have to close, too.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnClosingWindow(object sender, CancelEventArgs e)
		{
			if (sender is IFwMainWnd)
			{
				if (FindReplaceDialog != null)
				{
					foreach (IFwMainWnd fwWnd in MainWindows)
					{
						Debug.Assert(fwWnd != null && fwWnd is Form);
						if (fwWnd != sender && fwWnd.ActiveView != null)
						{
							m_findReplaceDlg.SetOwner(fwWnd.ActiveView.CastAsIVwRootSite(),
								(Form)fwWnd, FindPattern);
							return;
						}
					}
					// This should never happen, but, just in case a new owner for
					// the find/replace dialog cannot be found, close it so it doesn't
					// get left hanging around without an owner.
					RemoveFindReplaceDialog();
				}
			}
		}
		#endregion

		#region Other Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the EnterThreadModal event of the Application control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void Application_EnterThreadModal(object sender, EventArgs e)
		{
			m_fInModalState = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the LeaveThreadModal event of the Application control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void Application_LeaveThreadModal(object sender, EventArgs e)
		{
			m_fInModalState = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Note the most recent of our main windows to become active.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void fwMainWindow_Activated(object sender, EventArgs e)
		{
			m_activeMainWindow = (Form)sender;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure a window that's no longer valid isn't considered active.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void fwMainWindow_HandleDestroyed(object sender, EventArgs e)
		{
			if (m_activeMainWindow == sender)
				m_activeMainWindow = null;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has been fully initialized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasBeenFullyInitialized
		{
			get { return m_fInitialized && !IsDisposed && !BeingDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the product executable filename
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public abstract string ProductExecutableFile { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the project specific settings key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		public RegistryKey ProjectSpecificSettingsKey
		{
			get
			{
				Debug.Assert(Cache != null, "The app's cache has not been created yet.");
				using (var regKey = SettingsKey)
				{
					return regKey.CreateSubKey(Cache.ProjectId.Name);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the registry settings for this FwApp
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwRegistrySettings RegistrySettings
		{
			get { return m_registrySettings; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has a modal dialog or message box open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsModalDialogOpen
		{
			get { return m_fInModalState; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the currently active form. We provide this method so that we can override it
		/// in our tests where we don't show a window, and so don't have an active form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual Form ActiveForm
		{
			get { return Form.ActiveForm; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FwFindReplaceDlg
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwFindReplaceDlg FindReplaceDialog
		{
			get
			{
				CheckDisposed();
				if (m_findReplaceDlg != null && m_findReplaceDlg.IsDisposed)
					RemoveFindReplaceDialog(); // This is a HACK for TE-5974!!!

				return m_findReplaceDlg;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Registry key for user settings for this application. Individual applications will
		/// override this.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return FwRegistryHelper.FieldWorksRegistryKey;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SampleDatabase
		{
			get
			{
				return Path.Combine(FwDirectoryFinder.FdoDirectories.ProjectsDirectory, "Sena 3", "Sena 3" + FdoFileHelper.ksFwDataXmlFileExtension);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public abstract string ApplicationName
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the measurement system used in the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MsrSysType MeasurementSystem
		{
			// REVIEW (TimS): Can we remove this property and just use FwRegistrySettings directly?
			get { return (MsrSysType)FwRegistrySettings.MeasurementUnitSetting; }
			set { FwRegistrySettings.MeasurementUnitSetting = (int)value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return (m_fwManager != null) ? m_fwManager.Cache : null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FieldWorks manager for this application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFieldWorksManager FwManager
		{
			get { return m_fwManager; }
		}
		#endregion

		#region FieldWorks Project Dialog handlers

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays a message box asking the user whether or not he wants to open a sample DB.
		/// </summary>
		/// <param name="suggestedProject"></param>
		/// <returns><c>true</c> if user consented to opening the sample database; <c>false</c>
		/// otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ShowFirstTimeMessageDlg(string suggestedProject)
		{
			string sCaption = ResourceHelper.GetResourceString("kstidTrainingAvailable");
			string sMsg = GetResourceString("kstidOpenSampleDbMsg");
			if(MessageBox.Show(sMsg, sCaption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
				MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
			{
				return suggestedProject;
			}
			return String.Empty;
		}

		#endregion

		#region Methods for dealing with main windows
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close all windows (which will shut down the application)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void ExitAppplication()
		{
			if (m_rgMainWindows == null) return;

			for (var i = m_rgMainWindows.Count - 1; i >= 0; i--)
			{
				// Make sure we use a copy of the main windows list since closing a main window
				// will remove it out of the list. (TE-8574)
				List<IFwMainWnd> mainWindows = new List<IFwMainWnd>(m_rgMainWindows);
				foreach (IFwMainWnd mainWnd in mainWindows)
				{
					if (mainWnd is Form && !((Form)mainWnd).IsDisposed)
						mainWnd.Close();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application's find pattern for the find/replace dialog. (If one does not
		/// already exist, a new one is created.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwPattern FindPattern
		{
			get
			{
				CheckDisposed();
				if (m_findPattern == null)
					m_findPattern = VwPatternClass.Create();
				return m_findPattern;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the given window.
		/// </summary>
		/// <param name="iMainWnd">Index (in the internal list of main windows) of the window to
		/// activate</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ActivateWindow(int iMainWnd)
		{
			Form wnd = (Form)MainWindows[iMainWnd];
			wnd.Activate();
			if (wnd.WindowState == FormWindowState.Minimized)
				wnd.WindowState = FormWindowState.Normal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the active form. This is usually the same as Form.ActiveForm, but sometimes
		/// the official active form is something other than one of our main windows, for
		/// example, a dialog or popup menu. This is always one of our real main windows,
		/// which should be something that has a taskbar icon. It is often useful as the
		/// appropriate parent window for a dialog that otherwise doesn't have one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form ActiveMainWindow
		{
			get
			{
				CheckDisposed();
				return m_activeMainWindow;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified IFwMainWnd from the list of windows. If it is ok to close down
		/// the application and the count of main windows is zero, then this method will also
		/// shut down the application.
		/// </summary>
		/// <param name="fwMainWindow">The IFwMainWnd to remove</param>
		/// ------------------------------------------------------------------------------------
		public virtual void RemoveWindow(IFwMainWnd fwMainWindow)
		{
			if (IsDisposed || BeingDisposed)
				return;

			if (!m_rgMainWindows.Contains(fwMainWindow))
				return; // It isn't our window.

			// NOTE: The main window that was passed in is most likely already disposed, so
			// make sure we don't call anything that would throw an ObjectDisposedException!
			m_rgMainWindows.Remove(fwMainWindow);
			Form form = (Form)fwMainWindow;
			form.Activated -= fwMainWindow_Activated;
			form.HandleDestroyed -= fwMainWindow_HandleDestroyed;

			if (m_activeMainWindow == fwMainWindow)
				m_activeMainWindow = null; // Just in case

			if (m_rgMainWindows.Count == 0)
				m_fwManager.ExecuteAsync(m_fwManager.ShutdownApp, this);
		}
		#endregion

		#region Static methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public string GetResourceString(string stid)
		{
			string str = ((IApp)this).ResourceString(stid);
			if (string.IsNullOrEmpty(str))
				str = ResourceHelper.GetResourceString(stid);

			return str;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID. This should normally not be called directly.
		/// Use FwApp.GetResourceString() instead.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>string</returns>
		/// -----------------------------------------------------------------------------------
		string IApp.ResourceString(string stid)
		{
			return ResourceHelper.GetResourceString(stid);
		}

		#endregion

		#region Methods for handling Main app windows
		/// -----------------------------------------------------------------------------------
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
		/// <returns>New instance of main window if successfull; otherwise <c>null</c></returns>
		/// -----------------------------------------------------------------------------------
		public abstract Form NewMainAppWnd(IProgress progressDlg, bool fNewCache,
			Form wndCopyFrom, bool fOpeningNewProject);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Array of main windows that are currently open for this application. This array can
		/// be used (with foreach) to do all the kinds of things that used to require a custom
		/// method in AfApp to loop through the vector of windows and execute some method for
		/// each one (e.g., AreAllWndsOkToChange, SaveAllWndsEdits, etc.).
		/// In C++, was GetMainWindows()
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public List<IFwMainWnd> MainWindows
		{
			get
			{
				CheckDisposed();
				return m_rgMainWindows;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in all of the Main Windows of the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews()
		{
			CheckDisposed();

			if (m_refreshView != null)
				m_refreshView = true;
			else
			{
				foreach (IFwMainWnd wnd in MainWindows)
					wnd.RefreshAllViews();
			}
		}

		/// <summary>
		/// Restart the spell-checking process (e.g. when dictionary changed)
		/// </summary>
		public void RestartSpellChecking()
		{
			foreach (Control wnd in MainWindows)
			{
				RestartSpellChecking(wnd);
			}
		}

		private void RestartSpellChecking(Control root)
		{
			var rootSite = root as RootSite;
			if (rootSite != null)
				rootSite.RestartSpellChecking();
			foreach (Control c in root.Controls)
				RestartSpellChecking(c);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Enable or disable all top-level windows. This allows nesting. In other words,
		/// calling EnableMainWindows(false) twice requires 2 calls to EnableMainWindows(true)
		/// before the top level windows are actually enabled. An example of where not allowing
		/// nesting was a problem was the Tools/Options dialog, which could open a
		/// PossListChooser dialog. Before, when you closed the PossListChooser, you could
		/// select the main window.
		/// </summary>
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		/// -----------------------------------------------------------------------------------
		public void EnableMainWindows(bool fEnable)
		{
			CheckDisposed();

			if (!fEnable)
				m_nEnableLevel--;
			else if (++m_nEnableLevel != 0)
				return;

			// TE-1913: Prevent user from accessing windows that are open to the same project.
			// Originally this was used for importing.
			foreach (IFwMainWnd fwMainWnd in MainWindows)
			{
				if (fwMainWnd is Form)
					((Form)fwMainWnd).Enabled = fEnable;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate a size (width or height) which is 2/3 of the screen area or the minimum
		/// allowable size.
		/// </summary>
		/// <param name="screenSize">Total available width or height (for the screen)</param>
		/// <param name="minSize">Minimum width or height for the window</param>
		/// <returns>The ideal width or height for a cascaded window</returns>
		/// ------------------------------------------------------------------------------------
		public int CascadeSize(int screenSize, int minSize)
		{
			CheckDisposed();

			int retSize = (screenSize * 2) / 3;
			if (retSize < minSize)
				retSize = minSize;
			return retSize;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cascade the windows from top left and resize them to fill 2/3 of the screen area (or
		/// the minimum allowable size).
		/// </summary>
		/// <param name="wndCurr">Current Window (i.e. window whose menu was used to issue the
		/// Cascaede command.</param>
		/// ------------------------------------------------------------------------------------
		public void CascadeWindows(Form wndCurr)
		{
			CheckDisposed();

			// Get the screen in which to cascade.
			Screen scrn = Screen.FromControl(wndCurr);

			Rectangle rcScrnAdjusted = ScreenUtils.AdjustedWorkingArea(scrn);
			Rectangle rcUpperLeft = rcScrnAdjusted;
			rcUpperLeft.Width = CascadeSize(rcUpperLeft.Width, wndCurr.MinimumSize.Width);
			rcUpperLeft.Height = CascadeSize(rcUpperLeft.Height, wndCurr.MinimumSize.Height);
			Rectangle rc = rcUpperLeft;

			foreach (Form wnd in MainWindows)
			{
				// Ignore windows that are on other screens or which are minimized.
				if (scrn.WorkingArea == Screen.FromControl(wnd).WorkingArea &&
					wnd != wndCurr &&
					wnd.WindowState != FormWindowState.Minimized)
				{
					if (wnd.WindowState == FormWindowState.Maximized)
						wnd.WindowState = FormWindowState.Normal;

					wnd.DesktopBounds = rc;
					wnd.Activate();
					rc.Offset(SystemInformation.CaptionHeight, SystemInformation.CaptionHeight);
					if (!rcScrnAdjusted.Contains(rc))
					{
						rc = rcUpperLeft;
					}
				}
			}

			// Make the active window the last one and activate it.
			if (wndCurr.WindowState == FormWindowState.Maximized)
				wndCurr.WindowState = FormWindowState.Normal;
			wndCurr.DesktopBounds = rc;
			wndCurr.Activate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method can be used to do the size and spacing calculations when tiling either
		/// side-by-side or stacked. It calculates two things: 1) The desired width or height
		/// of tiled windows. 2) how many pixels between the left or top edges of tiled windows.
		/// If the calculated width or height of a window is less than the allowable minimum,
		/// then tiled windows will be overlapped.
		/// </summary>
		/// <param name="scrn">The screen where the tiling will take place (only windows on this
		/// screen will actually get tiled).</param>
		/// <param name="screenDimension">The width or height, in pixels, of the display on
		/// which tiling will be performed.</param>
		/// <param name="minWindowDimension">The minimum allowable width or height, in pixels,
		/// of tiled windows.</param>
		/// <param name="desiredWindowDimension">The desired width or height, in pixels, of
		/// tiled windows.</param>
		/// <param name="windowSpacing">The distance, in pixels, between the left or top edge
		/// of each tiled window. If there is only one window, this is undefined.</param>
		/// ------------------------------------------------------------------------------------
		public void CalcTileSizeAndSpacing(Screen scrn, int screenDimension,
			int minWindowDimension, out int desiredWindowDimension, out int windowSpacing)
		{
			CheckDisposed();

			CalcTileSizeAndSpacing(scrn, MainWindows, screenDimension, minWindowDimension,
				out desiredWindowDimension, out windowSpacing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method can be used to do the size and spacing calculations when tiling either
		/// side-by-side or stacked. It calculates two things: 1) The desired width or height
		/// of tiled windows. 2) how many pixels between the left or top edges of tiled windows.
		/// If the calculated width or height of a window is less than the allowable minimum,
		/// then tiled windows will be overlapped.
		/// </summary>
		/// <param name="scrn">The screen where the tiling will take place (only windows on this
		/// screen will actually get tiled).</param>
		/// <param name="windowsToTile">A list of all the windows to tile (including the
		/// current window)</param>
		/// <param name="screenDimension">The width or height, in pixels, of the display on
		/// which tiling will be performed.</param>
		/// <param name="minWindowDimension">The minimum allowable width or height, in pixels,
		/// of tiled windows.</param>
		/// <param name="desiredWindowDimension">The desired width or height, in pixels, of
		/// tiled windows.</param>
		/// <param name="windowSpacing">The distance, in pixels, between the left or top edge
		/// of each tiled window. If there is only one window, this is undefined.</param>
		/// ------------------------------------------------------------------------------------
		private void CalcTileSizeAndSpacing(Screen scrn, List<IFwMainWnd> windowsToTile,
			int screenDimension, int minWindowDimension,
			out int desiredWindowDimension, out int windowSpacing)
		{
			int windowCount = windowsToTile.Count;

			// Don't count windows if they're minimized.
			foreach (Form wnd in windowsToTile)
			{
				if (wnd.WindowState == FormWindowState.Minimized ||
					Screen.FromControl(wnd).WorkingArea != scrn.WorkingArea)
					windowCount--;
			}

			desiredWindowDimension = windowSpacing = screenDimension / windowCount;

			// Check if our desired window width is smaller than the minimum. If so, then
			// calculate what the overlap should be.
			if (desiredWindowDimension < minWindowDimension)
			{
				double overlap = (minWindowDimension * windowCount - screenDimension) /
					(windowCount - 1);

				windowSpacing = minWindowDimension - (int)Math.Round(overlap + 0.5);
				desiredWindowDimension = minWindowDimension;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Arrange the windows top to bottom or left to right.
		/// </summary>
		/// <param name="wndCurr">Current Window (i.e. window whose menu was used to issue a
		/// tile vertical or horizontal command.</param>
		/// <param name="orientation">The value indicating whether to tile side by side or
		/// stacked.</param>
		/// ------------------------------------------------------------------------------------
		public void TileWindows(Form wndCurr, WindowTiling orientation)
		{
			CheckDisposed();

			TileWindows(wndCurr, MainWindows, orientation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Arrange the windows top to bottom or left to right.
		/// </summary>
		/// <param name="wndCurr">Current Window (i.e. window whose menu was used to issue a
		/// tile vertical or horizontal command.</param>
		/// <param name="windowsToTile">A list of all the windows to tile (including the
		/// current window)</param>
		/// <param name="orientation">The value indicating whether to tile side by side or
		/// stacked.</param>
		/// ------------------------------------------------------------------------------------
		public void TileWindows(Form wndCurr, List<IFwMainWnd> windowsToTile,
			WindowTiling orientation)
		{
			CheckDisposed();

			// Get the screen in which to tile.
			Screen scrn = Screen.FromControl(wndCurr);

			int desiredDimension, windowSpacing;

			// At this point, assume the entire screen's working area is the desired size
			// and location for tiled windows, even though it's highly likely this will
			// change below.
			Rectangle rcDesired = scrn.WorkingArea;

			// Get the proper window width or height and the space between the windows
			// as they are tiled.
			if (orientation == WindowTiling.Stacked)
			{
				CalcTileSizeAndSpacing(scrn, windowsToTile, scrn.WorkingArea.Height,
					wndCurr.MinimumSize.Height, out desiredDimension, out windowSpacing);
				rcDesired.Height = desiredDimension;
			}
			else
			{
				CalcTileSizeAndSpacing(scrn, windowsToTile, scrn.WorkingArea.Width,
					wndCurr.MinimumSize.Width, out desiredDimension, out windowSpacing);
				rcDesired.Width = desiredDimension;
			}

			// There is a strange situation when a user's task bar is at the right or top
			// of the primary display. The working area returns the correct rectangle that
			// does not include the task bar. However, we cannot set a widnow's X or Y
			// coordinate to the working area's X or Y. If the window is to be located
			// in the upper left corner next to the task bar, X and Y must be 0.
			rcDesired.X -= ScreenUtils.TaskbarWidth;
			rcDesired.Y -= ScreenUtils.TaskbarHeight;

			// Move the active window to its proper place and size.
			wndCurr.DesktopBounds = rcDesired;

			// Now move the rest of the non minimized windows to their proper place.
			foreach (Form wnd in windowsToTile)
			{
				if (wnd.WindowState == FormWindowState.Maximized)
					wnd.WindowState = FormWindowState.Normal;

				if (wnd != wndCurr && wnd.WindowState != FormWindowState.Minimized &&
					Screen.FromControl(wnd).WorkingArea == scrn.WorkingArea)
				{
					if (orientation == WindowTiling.Stacked)
						rcDesired.Y += windowSpacing;
					else
						rcDesired.X += windowSpacing;

					wnd.DesktopBounds = rcDesired;
				}
			}

			// If there was any overlapping of tiled windows, go from bottom to the top
			// or right to left and activate each window so the tiling looks correct. i.e.
			// Each window is overlapped on its top edge by the window directly on top or left.
			if (windowSpacing != desiredDimension)
			{
				for (int i = windowsToTile.Count - 1; i >= 0; i--)
				{
					if (windowsToTile[i] != wndCurr &&
						((Form)windowsToTile[i]).WindowState != FormWindowState.Minimized &&
						Screen.FromControl((Form)windowsToTile[i]).WorkingArea == scrn.WorkingArea)
					{
						((Form)windowsToTile[i]).Activate();
					}
				}
			}

			// Finally, make the current window active.
			wndCurr.Activate();
		}
		#endregion

		#region Find/Replace Methods
		/// <summary>
		/// This provides a hook for any kind of app that wants to configure the dialog
		/// in some special way. TE wants to disable regular expressions for replace.
		/// </summary>
		protected virtual void ConfigureFindReplacedialog()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close and remove the Find/Replace modeless dialog (result of LT-5702)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveFindReplaceDialog()
		{
			if (m_findReplaceDlg != null)
			{
				// Closing doesn't work as it tries to hide the dlg ..
				// so go for the .. dispose.  It will do it 'base.Dispose()'!
				m_findReplaceDlg.Close();
				m_findReplaceDlg.Dispose();
				m_findReplaceDlg = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the Find/Replace modeless dialog
		/// </summary>
		/// <param name="fReplace"><c>true</c> to make the replace tab active</param>
		/// <param name="rootsite">The view where the find will be conducted</param>
		/// <returns><c>true</c> if the dialog is successfully displayed</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShowFindReplaceDialog(bool fReplace, RootSite rootsite)
		{
			CheckDisposed();

			if (rootsite == null || rootsite.RootBox == null)
				return false;

			int hvoRoot, frag;
			IVwViewConstructor vc;
			IVwStylesheet ss;
			rootsite.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
			if (hvoRoot == 0)
				return false;

			if (FindReplaceDialog == null)
			{
				m_findReplaceDlg = new FwFindReplaceDlg();
				ConfigureFindReplacedialog();
			}

			bool fOverlay = (rootsite.RootBox.Overlay != null);

			if (m_findReplaceDlg.SetDialogValues(rootsite.Cache, FindPattern,
				rootsite, fReplace, fOverlay, rootsite.FindForm(), this, this))
			{
				m_findReplaceDlg.Show();
				return true;
			}
			return false;
		}
		#endregion

		#region Synchronization methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suppress execution of all synchronize messages and store them in a queue instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SuppressSynchronize()
		{
			CheckDisposed();

			if (m_suppressedCacheInfo != null)
				m_suppressedCacheInfo.Count++; // Nested call
			else
				m_suppressedCacheInfo = new SuppressedCacheInfo();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resume execution of synchronize messages. If there are any messages in the queue
		/// execute them now.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResumeSynchronize()
		{
			CheckDisposed();

			if (m_suppressedCacheInfo == null)
				return; // Nothing to do

			m_suppressedCacheInfo.Count--;
			if (m_suppressedCacheInfo.Count > 0)
				return; // Still nested

			BeginUpdate();
			Queue<SyncMsg> messages = m_suppressedCacheInfo.Queue;
			m_suppressedCacheInfo = null;

			bool fProcessUndoRedoAfter = false;
			SyncMsg savedUndoRedo = SyncMsg.ksyncFullRefresh; // Arbitrary
			foreach (SyncMsg synchMsg in messages)
			{
				if (synchMsg == SyncMsg.ksyncUndoRedo)
				{
					// we must process this synch message after all the others
					fProcessUndoRedoAfter = true;
					savedUndoRedo = synchMsg;
					continue;
				}
				// Do the synch
				if (!Synchronize(synchMsg))
				{
					fProcessUndoRedoAfter = false; // Refresh already done, final UndoRedo unnecessary
					break; // One resulted in Refresh everything, ignore other synch msgs.
				}
			}
			if (fProcessUndoRedoAfter)
				Synchronize(savedUndoRedo);

			// NOTE: This code may present a race condition, because there is a slight
			// possibility that a sync message can come to the App at
			// this point and then get cleared from the syncMessages list and never get run.
			EndUpdate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suppress all calls to <see cref="T:RefreshAllViews()"/> until <see cref="T:EndUpdate"/>
		/// is called.
		/// </summary>
		/// <remarks>Used by <see cref="T:ResumeSynchronize"/> to do only one refresh of the
		/// view.</remarks>
		/// ------------------------------------------------------------------------------------
		private void BeginUpdate()
		{
			CheckDisposed();

			Debug.Assert(m_refreshView == null, "Nested BeginUpdate");
			m_refreshView = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do a <see cref="T:RefreshAllViews()"/> if it was called at least once after
		/// <see cref="T:BeginUpdate"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EndUpdate()
		{
			CheckDisposed();

			Debug.Assert(m_refreshView != null, "EndUpdate called without BeginUpdate");

			bool needRefresh = (bool)m_refreshView;
			m_refreshView = null; // Make sure we don't try suppress the following RefreshAllViews()
			if (needRefresh)
				RefreshAllViews();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cycle through the applications main windows and synchronize them with database
		/// changes.
		/// </summary>
		/// <param name="sync">synchronization information record</param>
		/// <returns>false if a RefreshAllViews was performed or presync failed; this suppresses
		/// subsequent sync messages. True to continue processing.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool Synchronize(SyncMsg sync)
		{
			CheckDisposed();

			if (m_suppressedCacheInfo != null)
			{
				Queue<SyncMsg> messages = m_suppressedCacheInfo.Queue;
				if (!messages.Contains(sync))
					messages.Enqueue(sync);
				return true;
			}

			if (sync == SyncMsg.ksyncFullRefresh)
			{
				RefreshAllViews();
				return false;
			}

			foreach (IFwMainWnd wnd in MainWindows)
				wnd.PreSynchronize(sync);

			if (sync == SyncMsg.ksyncWs)
			{
				// REVIEW TeTeam: AfLpInfo::Synchronize calls AfLpInfo::FullRefresh, which
				// clears the cache, loads the styles, loads ws and updates wsf, load project
				// basics, updates LinkedFiles root, load overlays and refreshes possibility
				// lists. I don't think we need to do any of these here.
				RefreshAllViews();
				return false;
			}

			foreach (IFwMainWnd wnd in MainWindows)
			{
				if (!wnd.Synchronize(sync))
				{
					// The window itself was not able to process the message successfully;
					// play safe and refresh everything
					RefreshAllViews();
					return false;
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// To participate in automatic synchronization from the database (calling SyncFromDb
		/// in a useful manner) and application must override this, providing a unique Guid.
		/// Typically this is the Guid defined by a static AppGuid method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual Guid SyncGuid
		{
			get
			{
				CheckDisposed();
				return Guid.Empty;
			}
		}
		#endregion

		#region Other Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the features available for the application
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual Feature[] GetAppFeatures()
		{
			CheckDisposed();

			throw new NotImplementedException("Application needs to override GetAppFeatures()");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For now, app has no settings to save. This method is required
		/// as part of ISettings implementation. Note that the SaveSettings() method is the
		/// appropriate one to modify or override if you really want to save settings. This
		/// one is NEVER called!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

		}

		/// <summary>
		/// Provides an application-wide default for allowed style contexts for windows that
		/// don't have an FwEditingHelper (i.e., all but TE windows at present).
		/// </summary>
		public virtual List<ContextValues> DefaultStyleContexts
		{
			get
			{
				CheckDisposed();

				return null;
			}
		}
		#endregion

		#region IHelpTopicProvider implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a URL identifying a Help topic.
		/// </summary>
		/// <param name="stid">An identifier for the desired Help topic</param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public string GetHelpString(string stid)
		{
			return m_helpTopicProvider.GetHelpString(stid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML help file (.chm) for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string HelpFile
		{
			get { return m_helpTopicProvider.HelpFile; }
		}
		#endregion

		#region IMessageFilter Members

		/// <summary>
		/// Filters out a message before it is dispatched.
		/// </summary>
		/// <param name="m">The message to be dispatched. You cannot modify this message.</param>
		/// <returns>
		/// true to filter the message and stop it from being dispatched; false to allow the message to continue to the next filter or control.
		/// </returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "c is a reference")]
		public virtual bool PreFilterMessage(ref Message m)
		{
			if (m.Msg != (int)Win32.WinMsgs.WM_KEYDOWN && m.Msg != (int)Win32.WinMsgs.WM_KEYUP)
				return false;

			Keys key = ((Keys)(int)m.WParam & Keys.KeyCode);
			// There is a known issue in older versions of Keyman (< 7.1.268) where the KMTip addin sends a 0x88 keystroke
			// in order to communicate changes in state to the Keyman engine. When a button is clicked while a text control
			// has focus, the input language will be switched causing the keystroke to be fired, which in turn disrupts the
			// mouse-click event. In order to workaround this bug for users who have older versions of Keyman, we simply filter
			// out these specific keystroke messages on buttons.
			if (key == Keys.ProcessKey || key == (Keys.Back | Keys.F17) /* 0x88 */)
			{
				Control c = Control.FromHandle(m.HWnd);
				if (c != null && c is ButtonBase)
					return true;
			}

			return false;
		}

		#endregion

		#region Methods for handling LinkedFiles
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applications should override this method to handle incoming links. This method is
		/// called from FieldWorks when a link is requested. It is guaranteed to be on the
		/// correct thread (the thread this application is on) so invoking should not be needed.
		/// Overridden in FwXApp.
		/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
		/// </summary>
		/// <param name="link">The link to handle.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void HandleIncomingLink(FwLinkArgs link)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles an outgoing link request from this application.
		/// </summary>
		/// <param name="link">The link.</param>
		/// ------------------------------------------------------------------------------------
		public void HandleOutgoingLink(FwAppArgs link)
		{
			m_fwManager.HandleLinkRequest(link);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle changes to the LinkedFiles root directory for a language project.
		/// </summary>
		/// <param name="oldLinkedFilesRootDir">The old LinkedFiles root directory.</param>
		/// <returns></returns>
		/// <remarks>This may not be the best place for this method, but I'm not sure there is a
		/// "best place".</remarks>
		/// ------------------------------------------------------------------------------------
		public bool UpdateExternalLinks(string oldLinkedFilesRootDir)
		{
			ILangProject lp = Cache.LanguageProject;
			string sNewLinkedFilesRootDir = lp.LinkedFilesRootDir;
			if (!FileUtils.PathsAreEqual(sNewLinkedFilesRootDir, oldLinkedFilesRootDir))
			{
				List<string> rgFilesToMove = new List<string>();
				// TODO: offer to move or copy existing files.
				foreach (ICmFolder cf in lp.MediaOC)
					CollectMovableFilesFromFolder(cf, rgFilesToMove, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
				foreach (ICmFolder cf in lp.PicturesOC)
					CollectMovableFilesFromFolder(cf, rgFilesToMove, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
				//Get the files which are pointed to by links in TsStrings
				CollectMovableFilesFromFolder(lp.FilePathsInTsStringsOA, rgFilesToMove, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);

				var hyperlinks = StringServices.GetHyperlinksInFolder(Cache, oldLinkedFilesRootDir);
				foreach (var linkInfo in hyperlinks)
				{
					if (!rgFilesToMove.Contains(linkInfo.RelativePath) &&
						FileUtils.SimilarFileExists(Path.Combine(oldLinkedFilesRootDir, linkInfo.RelativePath)) &&
						!FileUtils.SimilarFileExists(Path.Combine(sNewLinkedFilesRootDir, linkInfo.RelativePath)))
					{
						rgFilesToMove.Add(linkInfo.RelativePath);
					}
				}
				if (rgFilesToMove.Count > 0)
				{
					FileLocationChoice action;
					using (MoveOrCopyFilesDlg dlg = new MoveOrCopyFilesDlg())
					{
						dlg.Initialize(rgFilesToMove.Count, oldLinkedFilesRootDir, sNewLinkedFilesRootDir, this);
						DialogResult res = dlg.ShowDialog();
						Debug.Assert(res == DialogResult.OK);
						if (res != DialogResult.OK)
							return false;	// should never happen!
						action = dlg.Choice;
					}
					if (action == FileLocationChoice.Leave) // Expand path
					{
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
							() =>
								{
									foreach (ICmFolder cf in lp.MediaOC)
										ExpandToFullPath(cf, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
									foreach (ICmFolder cf in lp.PicturesOC)
										ExpandToFullPath(cf, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
								});
						// Hyperlinks are always already full paths.
						return false;
					}
					List<string> rgLockedFiles = new List<string>();
					foreach (string sFile in rgFilesToMove)
					{
						string sOldPathname = Path.Combine(oldLinkedFilesRootDir, sFile);
						string sNewPathname = Path.Combine(sNewLinkedFilesRootDir, sFile);
						string sNewDir = Path.GetDirectoryName(sNewPathname);
						if (!Directory.Exists(sNewDir))
							Directory.CreateDirectory(sNewDir);
						Debug.Assert(FileUtils.TrySimilarFileExists(sOldPathname, out sOldPathname));
						if (FileUtils.TrySimilarFileExists(sNewPathname, out sNewPathname))
							File.Delete(sNewPathname);
						try
						{
							if (action == FileLocationChoice.Move)
							{
								//LT-13343 do copy followed by delete to ensure the file gets put in the new location.
								//If the current FLEX record has a picture displayed the File.Delete will fail.
								File.Copy(sOldPathname, sNewPathname);
								File.Delete(sOldPathname);
							}

							else
								File.Copy(sOldPathname, sNewPathname);
						}
						catch (Exception ex)
						{
							Debug.WriteLine(String.Format("{0}: {1}", ex.Message, sOldPathname));
							rgLockedFiles.Add(sFile);
						}
					}
					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
						() => StringServices.FixHyperlinkFolder(hyperlinks, oldLinkedFilesRootDir, sNewLinkedFilesRootDir));

					// If any files failed to be moved or copied above, try again now that we've
					// opened a new window and had more time elapse (and more demand to reuse
					// memory) since the failure.
					if (rgLockedFiles.Count > 0)
					{
						GC.Collect();	// make sure the window is disposed!
						Thread.Sleep(1000);
						foreach (string sFile in rgLockedFiles)
						{
							string sOldPathname = Path.Combine(oldLinkedFilesRootDir, sFile);
							string sNewPathname = Path.Combine(sNewLinkedFilesRootDir, sFile);
							try
							{
								if (action == FileLocationChoice.Move)
									FileUtils.Move(sOldPathname, sNewPathname);
								else
									File.Copy(sOldPathname, sNewPathname);
							}
							catch (Exception ex)
							{
								Debug.WriteLine(String.Format("{0}: {1} (SECOND ATTEMPT)", ex.Message, sOldPathname));
							}
						}
					}
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Build a list of files that can be moved (or copied) to the new LinkedFiles root
		/// directory.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="rgFilesToMove"></param>
		/// <param name="sOldRootDir"></param>
		/// <param name="sNewRootDir"></param>
		private static void CollectMovableFilesFromFolder(ICmFolder folder,
			List<string> rgFilesToMove, string sOldRootDir, string sNewRootDir)
		{
			foreach (var file in folder.FilesOC)
			{
				string sFilepath = file.InternalPath;
				//only select files which have relative paths so they are in the LinkedFilesRootDir
				if (!Path.IsPathRooted(sFilepath))
				{
					// Don't put the same file in more than once!
					if (rgFilesToMove.Contains(sFilepath))
						continue;
					var sOldFilePath = Path.Combine(sOldRootDir, sFilepath);
					if (FileUtils.TrySimilarFileExists(sOldFilePath, out sOldFilePath))
					{
						var sNewFilePath= Path.Combine(sNewRootDir, sFilepath);
						if (FileUtils.TrySimilarFileExists(sNewFilePath, out sNewFilePath))
						{
							//if the file exists in the destination LinkedFiles location, then only copy/move it if
							//file in the source location is newer.
							var dateTimeOfFileSourceFile = File.GetLastWriteTime(sOldFilePath);
							var dateTimeOfFileDestinationFile = File.GetLastWriteTime(sNewFilePath);
							if (dateTimeOfFileSourceFile > dateTimeOfFileDestinationFile)
								rgFilesToMove.Add(sFilepath);
						}
						else
						{
							//if the file does not exist in the destination LinkeFiles location then copy/move it.
							rgFilesToMove.Add(sFilepath);
						}
					}
				}
			}
			foreach (var sub in folder.SubFoldersOC)
				CollectMovableFilesFromFolder(sub, rgFilesToMove, sOldRootDir, sNewRootDir);
		}

		/// <summary>
		/// Expand the internal paths from relative to absolute as needed, since the user
		/// doesn't want to move (or copy) them.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="sOldRootDir"></param>
		/// <param name="sNewRootDir"></param>
		private static void ExpandToFullPath(ICmFolder folder,
			string sOldRootDir, string sNewRootDir)
		{
			foreach (var file in folder.FilesOC)
			{
				string sFilepath = file.InternalPath;
				if (!Path.IsPathRooted(sFilepath))
				{
					if (FileUtils.SimilarFileExists(Path.Combine(sOldRootDir, sFilepath)) &&
						!FileUtils.SimilarFileExists(Path.Combine(sNewRootDir, sFilepath)))
					{
						file.InternalPath = Path.Combine(sOldRootDir, sFilepath);
					}
				}
			}
			foreach (var sub in folder.SubFoldersOC)
				ExpandToFullPath(sub, sOldRootDir, sNewRootDir);
		}
		#endregion

		#region IFeedbackInfoProvider Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// E-mail address for bug reports, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SupportEmailAddress
		{
			get { return GetResourceString("kstidSupportEmail"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// E-mail address for feedback reports, kudos, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string FeedbackEmailAddress
		{
			get { return GetResourceString("kstidSupportEmail"); }
		}

		#endregion
	}
	#endregion
}
