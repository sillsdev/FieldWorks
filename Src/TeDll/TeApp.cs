// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: TeApp.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Implementation of TeApp
// </remarks>
//
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// handle requests from other instances of TE
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class RemoteRequest : MarshalByRefObject
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Load a new project based on the given commandline arguments
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void LoadProject(string[] args)
		{
			FwMainWnd mainWindow = (FwMainWnd)FwApp.App.ActiveMainWindow;
			if (mainWindow != null)
				mainWindow.Invoke(mainWindow.m_openDelegate, new object[] { args });
		}
	}

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// The FieldWorks Translation Editor.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class TeApp : FwApp, IApp
	{
		/// <summary>Provides an array of the Features available in TE</summary>
		private static SIL.FieldWorks.FDO.Feature[] s_AppFeatures;

		/// <summary>Saves a table of notes windows -- one for each cache.</summary>
		private static Dictionary<FdoCache, NotesMainWnd> s_notesWindoes = new Dictionary<FdoCache, NotesMainWnd>();

		// TODO: test for unique GUID for each application
		/// <summary>Unique identification for each instance of a TE application</summary>
		private Guid m_syncGuid = Guid.NewGuid();

		#region Construction and Initializing
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// TeApp Constructor takes command line arguments
		/// </summary>
		///
		/// <param name="rgArgs">Command-line arguments</param>
		/// -----------------------------------------------------------------------------------
		public TeApp(string[] rgArgs) : base(rgArgs)
		{
			if (s_notesWindoes == null)
				s_notesWindoes = new Dictionary<FdoCache, NotesMainWnd>();
#if DEBUG
			CmObject.s_checkValidity = true;
#endif

			ParagraphCounterManager.ParagraphCounterType = typeof(TeParaCounter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DoApplicationInitialization()
		{
			base.DoApplicationInitialization();
			CleanupRegistry();
			CleanupOldFiles();
			//InitializeMessageDialogs();
			ScrReference.InitializeVersification(DirectoryFinder.GetFWCodeSubDirectory(
				"Translation Editor"), false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// At one time, we created registry keys HKEY_CURRENT_USER\HKEY_CURRENT_USER...
		/// inadvertently. Now, we want to clean them up if they still exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CleanupRegistry()
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey("HKEY_CURRENT_USER", true);
			if (key != null)
			{
				DeleteRegistryKey(key);
				Registry.CurrentUser.DeleteSubKey("HKEY_CURRENT_USER");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively delete a registry key
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		private void DeleteRegistryKey(RegistryKey key)
		{
			// find all the subkeys and delete them recursively.
			foreach (string subKeyName in key.GetSubKeyNames())
			{
				DeleteRegistryKey(key.OpenSubKey(subKeyName, true));
				key.DeleteSubKey(subKeyName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get rid of old obsolete files from previous versions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CleanupOldFiles()
		{
			try
			{
				string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
					+ Path.DirectorySeparatorChar + "SIL"
					+ Path.DirectorySeparatorChar + Application.ProductName;

				List<string> oldFilesList = new List<string>(Directory.GetFiles(path, "TE.TBDef.tb*.xml"));
				string oldDiffFile = Path.Combine(path, "TBDef.DiffView.tbDiffView.xml");
				if (File.Exists(oldDiffFile))
					oldFilesList.Add(oldDiffFile);

				foreach (string oldFile in oldFilesList)
				{
					File.SetAttributes(oldFile, FileAttributes.Normal);
					File.Delete(oldFile);
				}
			}
			catch { /* Ignore any failures. */ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Run()
		{
			CheckDisposed();

			try
			{
				// Create a remoting server to listen for events from other instances.
				TcpChannel channel = new TcpChannel(9628);
				ChannelServices.RegisterChannel(channel, false);

				RemotingConfiguration.RegisterWellKnownServiceType(
					typeof(RemoteRequest),
					"TE_RemoteRequest",
					WellKnownObjectMode.Singleton);
			}
			catch
			{
			}

			Options.AddErrorReportingInfo();
			base.Run();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays a message box asking the user whether or not he wants to open a sample DB.
		/// </summary>
		/// <returns><c>true</c> if user consented to opening the sample database; <c>false</c>
		/// otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool ShowFirstTimeMessageDlg()
		{
			CloseSplashScreen();
			using (TrainingAvailable dlg = new TrainingAvailable())
			{
				return (dlg.ShowDialog() == DialogResult.Yes);
			}
		}
		const string WorkspaceFile = "TeLibronixWorkspace.xml";

		/// <summary>
		/// Overridden to save the libronix setting (and the workspace).
		/// </summary>
		/// <param name="key"></param>
		protected override void SaveSettings(RegistryKey key)
		{
			base.SaveSettings(key);
			key.SetValue("AutoStartLibronix", AutoStartLibronix);
			if (AutoStartLibronix)
				LibronixLinker.LibronixWorkspaceManager.SaveWorkspace(WorkspaceLocation());
		}

		private static string WorkspaceLocation()
		{
			return Path.Combine(DirectoryFinder.DataDirectory, WorkspaceFile);
		}


		/// <summary>
		/// Overridden to load the libronix setting and implement it.
		/// </summary>
		/// <param name="key"></param>
		protected override void LoadSettings(RegistryKey key)
		{
			base.LoadSettings(key);
			AutoStartLibronix = ((string)key.GetValue("AutoStartLibronix", "false")) == "True";
			if (AutoStartLibronix)
				LibronixLinker.LibronixWorkspaceManager.RestoreIfNotRunning(WorkspaceLocation());
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Application entry point. If TE isn't already running, an instance of the app is
		/// created.
		/// </summary>
		/// <param name="rgArgs">Command-line arguments</param>
		/// <returns>0</returns>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			try
			{
				// Enable visual styles. Ignored on Windows 2000. Needs to be called before
				// we create any controls!
				Application.EnableVisualStyles();

				// REVIEW (EberhardB): Do we need the following line? .NET 2.0 puts it in if you
				// create a new project.
				// Application.SetCompatibleTextRenderingDefault(false);

				if (ExistingProcess != null)
				{
					try
					{
						IntPtr hWndMain = ExistingProcess.MainWindowHandle;
						if (hWndMain != (IntPtr)0)
						{
							Win32.SetForegroundWindow(hWndMain);
							//Win32.WINDOWPLACEMENT placementInfo = new Win32.WINDOWPLACEMENT();
							Win32.WINDOWPLACEMENT placementInfo;
							Win32.GetWindowPlacement(hWndMain, out placementInfo);
							if (placementInfo.showCmd == (uint)Win32.WINDOWPLACEMENT.ShowWindowCommands.SW_SHOWMINIMIZED)
							{
								//placementInfo.length = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.WINDOWPLACEMENT));
								//placementInfo.showCmd = (uint)Win32.WINDOWPLACEMENT.ShowWindowCommands.SW_NORMAL;
								//placementInfo.showCmd = (uint)Win32.WINDOWPLACEMENT.ShowWindowCommands.SW_SHOWNORMAL;
								//placementInfo.showCmd = (uint)Win32.WINDOWPLACEMENT.ShowWindowCommands.SW_SHOW;
								//placementInfo.showCmd = (uint)Win32.WINDOWPLACEMENT.ShowWindowCommands.SW_SHOWDEFAULT;
								placementInfo.showCmd = (uint)Win32.WINDOWPLACEMENT.ShowWindowCommands.SW_RESTORE;
								Win32.SetWindowPlacement(hWndMain, ref placementInfo);
							}
							// This didn't work: Form mainWnd = (Form)Control.FromHandle(hWndMain);
							// ActivateWindow(mainWnd);
						}

						// If there is a command line project to load, tell the other
						// instance to load the project
						if (rgArgs != null && rgArgs.Length > 0)
						{
							// Create a channel for communicating w/ the main instance
							// of TE.
							TcpChannel chan = new TcpChannel();
							ChannelServices.RegisterChannel(chan, false);

							// Create an instance of the remote object
							RemoteRequest requestor = (RemoteRequest)Activator.GetObject(
								typeof(RemoteRequest),
								"tcp://localhost:9628/TE_RemoteRequest");

							requestor.LoadProject(rgArgs);
						}
					}
					catch
					{
						// The other instance does not have a window handle.  It is either in the
						// process of starting up or shutting down.
					}
				}
				else
				{
					Logger.Init();
					Logger.WriteEvent("Starting app");
					// Invoke does nothing directly, but causes BroadcastEventWindow to be initialized
					// on this thread to prevent race conditions on shutdown.See TE-975
					SystemEvents.InvokeOnEventsThread(new DoNothingDelegate(DoNothing));

					// Using the 'using' gizmo will call Dispose on app,
					// which in turn will call Dispose for all FdoCache objects,
					// which will release all of the COM objects it connects to.
					// Doing both in this stacked fashion will cause both to be disposed, as well.
					using (TeApp appTranslationEditor = new TeApp(rgArgs))
					{
						SIL.Utils.ErrorReporter.EmailAddress = "TeErrors@sil.org";
						appTranslationEditor.Run();
					}
				}
				return 0;
			}
			catch (Exception e)
			{
				// This should catch any remaining exceptions that HandleUnhandledException()
				// and HandleTopLevelError() don't catch so that we can display the
				// "Green Screen".
				DisplayError(e, null);
				return -1;
			}
		}

		private delegate void DoNothingDelegate();
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy method to be used for InvokeOnEventsThread which is used as a way to initialize
		/// the Broadcast window and prevent errors on shutdown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DoNothing()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides a hook for initializing a cache in special ways. For example,
		/// LexTextApp sets up a CreateModifyTimeManager.
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InitCache(FdoCache cache)
		{
			base.InitCache(cache);

			// Use Scripture specific types instead of generic ones.
			cache.MapType(typeof(StTxtPara), typeof(ScrTxtPara));
			cache.MapType(typeof(StFootnote), typeof(ScrFootnote));
		}
		#endregion

		#region IDisposable override
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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed || BeingDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				TeResourceHelper.ShutDownTEResourceHelper();
				if (s_notesWindoes != null)
				{
					foreach (NotesMainWnd noteWnd in s_notesWindoes.Values)
					{
						// The superclass Dispose method will zap it main windows.
						if (MainWindows != null && !MainWindows.Contains(noteWnd))
							noteWnd.Close();
					}
					s_notesWindoes.Clear();
				}
				Logger.WriteEvent("Disposing app");
				Logger.ShutDown();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			s_AppFeatures = null;

			base.Dispose(disposing);

			// NB: Do this after the call to the base method, as any TeMainWnds will want to access them.
			//s_notesWindoes = null; // Don't null the hashtable, since it doesn't get added by an instance
		}
		#endregion IDisposable override

		#region TeApp Properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Guid for the application (used for uniquely identifying DB items that "belong" to
		/// this app.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static Guid AppGuid
		{
			get	{return TeResourceHelper.TeAppGuid;}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The RegistryKey for this application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		override public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return base.SettingsKey.CreateSubKey("Translation Editor");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML help file (.chm) for Translation Editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string HelpFile
		{
			get
			{
				CheckDisposed();

				return DirectoryFinder.FWCodeDirectory +
					@"\Helps\FieldWorks_Translation_Editor_Help.chm";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string SampleDatabase
		{
			get
			{
				CheckDisposed();
				return "Sena 3";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// To participate in automatic synchronization from the database (calling SyncFromDb
		/// in a useful manner) and application must override this, providing a unique Guid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Guid SyncGuid
		{
			get
			{
				CheckDisposed();
				return m_syncGuid;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether a cache can control the progress bar on the splash
		/// screen. We do our own progress movement, so we don't want the cache to control it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CacheCanControlSplashScreenProgress
		{
			get { return false; }
		}
		#endregion

		#region IHelpTopicProvider implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a help file URL or topic
		/// </summary>
		/// <param name="stid"></param>
		/// <param name="iKey"></param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetHelpString(string stid, int iKey)
		{
			CheckDisposed();

			// First check if the stid starts with the marker that tells us the user is wanting
			// help on a particular style displayed in the styles combo box. If so, then find
			// the correct URL for the help topic of that style's example.
			if (stid.StartsWith("style:"))
				return TeStylesXmlAccessor.GetHelpTopicForStyle(stid.Substring(6));

			string helpString = TeResourceHelper.GetHelpString(stid);

			if (helpString != null)
				return helpString;

			return base.GetHelpString(stid, iKey);
		}
		#endregion

		#region Miscellaneous Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This provides a hook for any kind of app that wants to configure the dialog
		/// in some special way. TE wants to disable regular expressions for replace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ConfigureFindReplacedialog()
		{
			m_findReplaceDlg.DisableReplacePatternMatching = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the notes window associated with the specified cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns>The notes window associated with the specified cache. If there is no
		/// notes window for the specified cache, null is returned.</returns>
		/// ------------------------------------------------------------------------------------
		public static NotesMainWnd GetNotesWndForCache(FdoCache cache)
		{
			return s_notesWindoes.ContainsKey(cache) ? s_notesWindoes[cache] : null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a notes window for a specified cache to the list of notes windows stored for
		/// this application.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wnd"></param>
		/// ------------------------------------------------------------------------------------
		public static void AddNotesWndForCache(FdoCache cache, NotesMainWnd wnd)
		{
			if (s_notesWindoes.ContainsKey(cache))
			{
				NotesMainWnd oldValue = s_notesWindoes[cache];
				oldValue.Close();
			}
			s_notesWindoes[cache] = wnd;
			wnd.Closing += new System.ComponentModel.CancelEventHandler(HandleNotesWindowClosing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure a closing notes window gets removed from the table of notes windows.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private static void HandleNotesWindowClosing(object sender,
			System.ComponentModel.CancelEventArgs e)
		{
			if (sender is NotesMainWnd)
			{
				NotesMainWnd gonner = sender as NotesMainWnd;
				s_notesWindoes.Remove(gonner.Cache);
				gonner.Closing -= new System.ComponentModel.CancelEventHandler(HandleNotesWindowClosing);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the help urls for each tab on the styles dialog (currently 5 tabs)
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string[] GetStyleDlgHelpUrls()
		{
			CheckDisposed();

			return new string[] {
									TeResourceHelper.GetHelpString("khtpTeStylesGeneral"),
									TeResourceHelper.GetHelpString("khtpTeStylesFont"),
									TeResourceHelper.GetHelpString("khtpTeStylesParagraph"),
									TeResourceHelper.GetHelpString("khtpTeStylesBullets"),
									TeResourceHelper.GetHelpString("khtpTeStylesBorder")};
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main Translation Editor window
		/// </summary>
		///
		/// <param name="cache">Instance of the FW Data Objects cache that the new main window
		/// will use for accessing the database.</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>New instance of TeMainWnd if Scripture data has been successfully loaded;
		/// null, otherwise</returns>
		/// -----------------------------------------------------------------------------------
		protected override Form NewMainAppWnd(FdoCache cache, bool fNewCache, Form wndCopyFrom,
			bool fOpeningNewProject)
		{
			if (!LoadData(cache, fOpeningNewProject))
				return null;

			if (fNewCache)
			{
				ILangProject lp = cache.LangProject;
				// Loop through the Vernacular WS and initialize them
				foreach (ILgWritingSystem ws in lp.VernWssRC)
					cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws.Hvo);

				// Loop through the Analysis WS and initialize them
				foreach (ILgWritingSystem ws in lp.AnalysisWssRC)
					cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws.Hvo);

				// Since getting the engine for a WS can actually cause it to get updated from
				// the local XML file, we now need to clear the info about them, so we won't be
				// using stale data from the cache.
				cache.VwCacheDaAccessor.ClearInfoAboutAll(lp.VernWssRC.HvoArray, lp.VernWssRC.Count,
					VwClearInfoAction.kciaRemoveObjectAndOwnedInfo);
				cache.VwCacheDaAccessor.ClearInfoAboutAll(lp.AnalysisWssRC.HvoArray, lp.AnalysisWssRC.Count,
					VwClearInfoAction.kciaRemoveObjectAndOwnedInfo);

				// Make sure this DB uses the current stylesheet version, note categories & and key terms list
				if (!fOpeningNewProject)
				{
					IAdvInd4 progressDlg = m_SplashScreenWnd != null ? m_SplashScreenWnd.ProgressBar as IAdvInd4 : null;
					TeScrInitializer.EnsureProjectComponentsValid(cache, progressDlg);
				}
				// Do any other needed initialization here.

			}

			// TE-1913: Prevent user from accessing windows that are open to the same project.
			// Originally this was used for importing.
			foreach (Form wnd in MainWindows)
			{
				if (!wnd.Enabled && (wnd is FwMainWnd && ((FwMainWnd)wnd).Cache == cache))
				{
					MessageBox.Show("This project is locked by another window. Please try again later.",
						"Project Locked", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return null;
				}
			}
			return NewTeMainWnd(cache, wndCopyFrom);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main Translation Editor window
		/// </summary>
		/// <param name="cache">Instance of the FW Data Objects cache that the new main window
		/// will use for accessing the database.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <returns>New instance of TeMainWnd</returns>
		///
		/// <remarks>This is virtual to support subclasses (esp. for testing)</remarks>
		/// -----------------------------------------------------------------------------------
		protected virtual TeMainWnd NewTeMainWnd(FdoCache cache, Form wndCopyFrom)
		{
			Logger.WriteEvent(string.Format("Creating new TeMainWnd for {0}\\{1}",
				cache.ServerName, cache.DatabaseName));
			return new TeMainWnd(cache, wndCopyFrom);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Preloads all the data needed by TE and checks to make sure that the information
		/// that TE needs to run (styles, Key terms, etc.) is up to date.
		/// </summary>
		/// <param name="cache">FDO cache for accessing DB</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>true if data loaded successfully; false, otherwise</returns>
		/// -----------------------------------------------------------------------------------
		protected bool LoadData(FdoCache cache, bool fOpeningNewProject)
		{
			// Load the Scripture Books
			//LangProject lp = cache.LangProject;

			// Temporary code.... Uncomment this line if the only thing you need to do is
			// reload the styles list.
			//			TeStylesXmlAccessor.CreateFactoryScrStyles(lp.TranslatedScriptureOA, null);

			// temporary code to blow away the user views for TE
			//			foreach (UserView view in cache.UserViewSpecs.GetUserViews(TeApp.AppGuid))
			//			{
			//				cache.DeleteObject(view.Hvo);
			//			}

			// Temporary code.... Remove after PHM, JAS, and JUD are imported into TestLangProj
			//			lp.TranslatedScriptureOA.DefaultImportSettings = null;

			// Temporary code.... Remove after New Styles are created
			//			lp.TranslatedScriptureOA = null;

			if (!TeScrInitializer.Initialize(cache, m_SplashScreenWnd))
				return false;

			ScriptureChangeWatcher.Create(cache);
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>string</returns>
		/// -----------------------------------------------------------------------------------
		string IApp.ResourceString(string stid)
		{
			CheckDisposed();

			return TeResourceHelper.GetResourceString(stid);
		}

		#endregion

		#region Proposed Methods for User Profiles  //SarahD

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the features available for TE
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override Feature[] GetAppFeatures()
		{
			CheckDisposed();

			//SarahD
			//TODO:  load the strings from TeStrings.resx
			s_AppFeatures = new SIL.FieldWorks.FDO.Feature[] {
																 new SIL.FieldWorks.FDO.Feature(1, "Feature1", 1),
																 new SIL.FieldWorks.FDO.Feature(2, "Feature2", 1),
																 new SIL.FieldWorks.FDO.Feature(3, "Feature3", 1),
																 new SIL.FieldWorks.FDO.Feature(4, "Feature4", 3),
																 new SIL.FieldWorks.FDO.Feature(5, "Feature5", 3),
																 new SIL.FieldWorks.FDO.Feature(6, "Feature6", 3),
																 new SIL.FieldWorks.FDO.Feature(7, "Feature7", 5),
																 new SIL.FieldWorks.FDO.Feature(8, "Feature8", 5),
																 new SIL.FieldWorks.FDO.Feature(9, "Feature9", 5)};
			return s_AppFeatures;
		}

		#endregion
	}
}
