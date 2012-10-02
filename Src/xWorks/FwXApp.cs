// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FxApp.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Resources;
using System.Reflection;
using System.IO;
using System.Threading;

using XCore;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for XApp.
	/// </summary>
	abstract public class FwXApp : FwApp
	{
		#region Data Members

		/// <summary></summary>
		protected FwLinkReceiver m_linkReceiver;

		#endregion // Data Members

		#region Properties

		/// <summary>
		/// Get the name of the product.
		/// </summary>
		/// <remarks>Subclasses should override this to get the proper product name.</remarks>
		public virtual string ProductName
		{
			get
			{
				CheckDisposed();
				return "Generic xWorks Application";
			}
		}

		/// <summary>
		/// Get the pathname of the default XML configuration file,
		/// which is used if none was provided in the "-x" command line option.
		/// </summary>
		/// <remarks>Subclasses should override this to get the proper XML configuration pathname.</remarks>
		public virtual string DefaultConfigurationPathname
		{
			get
			{
				CheckDisposed();

				// TODO: See if there can be a generic config file for all of xWorks.
				// Maybe something like an editor of LangProject.
				Debug.Assert(false);
				return "";
			}
		}

		public override bool Synchronize(SyncInfo sync, FdoCache cache)
		{
			CheckDisposed();

			if (sync.msg == SyncMsg.ksyncUndoRedo || sync.msg == SyncMsg.ksyncFullRefresh)
			{
				OnMasterRefresh(null);
				return true;
			}
			return base.Synchronize (sync, cache);
		}

		/// <summary>
		/// Call RefreshDisplay for every child control (recursively) which implements it.
		/// </summary>
		public void RefreshDisplay(FdoCache cache)
		{
			foreach (IFwMainWnd window in m_rgMainWindows)
			{
				FwXWindow xwnd = window as FwXWindow;
				if (xwnd != null && xwnd.Cache == cache)
					xwnd.RefreshDisplay();
			}
		}


		/// <summary>
		/// This is the one (and should be only) handler for the user Refresh command.
		/// Refresh wants to first clean up the cache, then give things like Clerks a
		/// chance to reload stuff (calling the old OnRefresh methods), then give
		/// windows a chance to redisplay themselves.
		/// </summary>
		public void OnMasterRefresh(object sender)
		{
			CheckDisposed();
			// Susanna asked that refresh affect only the currently active project, which is
			// what the string and List variables below attempt to handle.  See LT-6444.
			string sDatabase = null;
			string sServer = null;
			FwXWindow activeWnd = ActiveForm as FwXWindow;
			if (activeWnd != null)
			{
				sDatabase = activeWnd.Cache.DatabaseName.ToLowerInvariant();
				sServer = activeWnd.Cache.ServerName.ToLowerInvariant();
			}
			List<FwXWindow> rgxw = new List<FwXWindow>();
			foreach (IFwMainWnd wnd in MainWindows)
			{
				FwXWindow xwnd = wnd as FwXWindow;
				if (xwnd != null)
				{
					if (sDatabase == null ||
						(xwnd.Cache.DatabaseName.ToLowerInvariant() == sDatabase &&
						xwnd.Cache.ServerName.ToLowerInvariant() == sServer))
					{
						xwnd.PrepareToRefresh();
						rgxw.Add(xwnd);
					}
				}
			}
			FwApp.App.ClearAllCaches(sDatabase, sServer);
			foreach (FwXWindow xwnd in rgxw)
			{
				if (activeWnd != xwnd)
					xwnd.FinishRefresh();
			}

			// LT-3963: active window changes as a result of a refresh.
			// Make sure focus doesn't switch to another FLEx application / window also
			// make sure the application focus isn't lost all together.
			// ALSO, after doing a refresh with just a single application / window,
			// the application would loose focus and you'd have to click into it to
			// get that back, this will reset that too.
			if (activeWnd != null)
			{
				// Refresh it last, so its saved settings get restored.
				activeWnd.FinishRefresh();
				activeWnd.Activate();
			}
		}

		/*
		/// <summary>
		/// LT-3963 message handler
		/// Set the active form to the parameter value.
		/// </summary>
		/// <param name="param">form to make active</param>
		/// <returns>true to stop the broadcast chain</returns>
		public bool OnMakeSureActiveWindow(object param)
		{
			CheckDisposed();

			FwXWindow activeWnd = (FwXWindow)param;
			if (activeWnd != null)
			{
				if (!activeWnd.IsDisposed &&	// not disposed yet AND
					 activeWnd.Visible &&		// visible AND
					!activeWnd.Focused &&		// not currently focused AND
					activeWnd.CanFocus)			// can be
				{
					activeWnd.Activate();	// make it the active form
				}
			}

			return true;
		}*/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the XML file as stream
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual Stream ConfigurationStream
		{
			get
			{
				Debug.Assert(false, "Subclasses must overrride this property.");
				return null;
			}
		}

		/// <summary>
		/// Gets the registry settings key name for the application.
		/// </summary>
		/// <remarks>Subclasses should override this, or all its settings will go in "FwXapp".</remarks>
		protected virtual string SettingsKeyName
		{
			get
			{
				Debug.Assert(false, "Subclasses must overrride this property.");
				return "";
			}
		}

		#endregion // Properties

		#region Construction and Initializing

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="arguments">Command line arguments.</param>
		public FwXApp(string[] arguments) : base(arguments)
		{
			try
			{
				m_linkReceiver = FwLinkReceiver.StartReceiving(Application.ProductName,
					new FwLinkReceiver.LinkEventHandler(OnIncomingLink));
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message,
					String.Format(xWorksStrings.ProblemStarting0, this.ProductName),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}
		/// <summary>
		/// needed for automated tests
		/// </summary>
		/// <param name="arguments"></param>
		public FwXApp() : base(new string[]{})
		{
			// don't set this here - m_SplashScreenWnd is still uninitialized!
			//m_SplashScreenWnd.ProdName = "Testing";
		}

		#endregion // Construction and Initializing

		public void OnIncomingLink(FwLink link)
		{
			CheckDisposed();

			if (m_rgMainWindows.Count == 0)
				return;

			FwXWindow wnd = m_rgMainWindows[0] as FwXWindow;
			Debug.Assert(wnd != null);
			wnd.Invoke(wnd.IncomingLinkHandler, new Object[]{link});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the currently active form. We provide this method so that we can override it
		/// in our tests where we don't show a window, and so don't have an active form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override Form ActiveForm
		{
			get
			{
				if (base.ActiveForm != null)
					return base.ActiveForm;
				foreach (FwXWindow wnd in m_rgMainWindows)
				{
					if (wnd.ContainsFocus)
						return wnd;
				}
				if (m_rgMainWindows.Count > 0)
					return (Form)m_rgMainWindows[0];
				return null;
			}
		}

		public void HandleIncomingLink(FwLink link)
		{
			CheckDisposed();

			FwXWindow fwxwnd = null;
			string server = link.Server.Replace(".", Environment.MachineName);
			// = FwLink.RestoreServerFromURL(link.Server).Replace(".", Environment.MachineName);
			Debug.Assert(server != null && server != String.Empty);
			string database = link.Database;
			Debug.Assert(database != null && database != String.Empty);
			string key = MakeKey(server, database);
			if (!m_caches.ContainsKey(key))
			{
				// Add command line info.
				Dictionary<string, List<String>> oldTable = m_commandLineArgs; // Save original args.
				m_commandLineArgs = new Dictionary<string, List<String>>();
				List<String> list = new List<String>();
				list.Add(server);
				m_commandLineArgs.Add("c", list);
				list = new List<String>();
				list.Add(database);
				m_commandLineArgs.Add("db", list);
				list = new List<String>();
				list.Add(link.ToString());
				m_commandLineArgs.Add("link", list);
				Form frm = ActiveForm;
				fwxwnd = (FwXWindow)NewMainWindow(null, false);
				AdjustNewWindowPosition(fwxwnd, frm);
				m_commandLineArgs = oldTable; // Restore oringinal args.
			}
			else
			{
				FdoCache cache = m_caches[key];
				// Get window that uses the given DB.
				foreach (FwXWindow wnd in m_rgMainWindows)
				{
					if (wnd.Cache == cache)
					{
						fwxwnd = wnd;
						break;
					}
				}
			}
			fwxwnd.Mediator.SendMessage("FollowLink", link);
			bool topmost = fwxwnd.TopMost;
			fwxwnd.TopMost = true;
			fwxwnd.TopMost = topmost;
			fwxwnd.Activate();
		}

		#region ISettings implementation

		// Imherited implementation is just fine for: KeepWindowSizePos.

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The RegistryKey for this application.
		/// </summary>
		///***********************************************************************************
		public override RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return base.SettingsKey.CreateSubKey(SettingsKeyName);
			}
		}

		#endregion // ISettings implementation

		#region Message Delegates

		/// <summary>
		/// Select an existing language project.
		/// </summary>
		/// <param name="activeWindow"></param>
		public void ChooseLangProject(FwXWindow activeWindow)
		{
			CheckDisposed();

			Debug.Assert(activeWindow != null);
			FdoCache cache = activeWindow.Cache;
			Debug.Assert(cache != null);

			// Results parms for the dlg.
			bool fHaveProject;
			int hvoProj;
			string sProject;
			Guid guid;
			bool fHaveSubitem;
			int hvoSubitem;
			string sUserWs = cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(cache.DefaultUserWs);	//"en";
			string sName;
			string sServer = (cache != null) ? cache.ServerName : MiscUtils.LocalServerName;
			string sDatabase;

			IOpenFWProjectDlg dlg = OpenFWProjectDlgClass.Create();
			dlg.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			uint hwnd = activeWindow == null ? 0 : (uint)activeWindow.Handle.ToInt32();
			dlg.Show(Logger.Stream, sServer, MiscUtils.LocalServerName, sUserWs, hwnd, false, 0,
				FwApp.App.HelpFile + "::/" + FwApp.App.GetHelpString("khtpFWOpenProject", 0));
			dlg.GetResults(out fHaveProject, out hvoProj, out sProject,
				out sDatabase, out sServer, out guid, out fHaveSubitem, out hvoSubitem, out sName);
			System.Runtime.InteropServices.Marshal.ReleaseComObject(dlg);

			if (fHaveProject)
			{
				// make sure the server ends in "\\SILFW"
				// Note lowercase of SILFW in Turkish is not silfw.
				if (sServer !=null && sServer.ToLowerInvariant().EndsWith("\\silfw") == false)
					sServer += "\\SILFW";	// append it

				// The GetCache call will switch the 'cache' var to an extant FdoCache,
				// if the app already has a connection on open on that server and DB.

				if (CheckDbVerCompatibility(sServer, sDatabase))
				{
					bool isNewCache = GetCache(sServer, sDatabase, out cache);
					// save the local and global settings for windows sharing this cache.
					if (!isNewCache)
					{
						foreach (IFwMainWnd mainWindow in m_rgMainWindows)
						{
							if (mainWindow.Cache == cache &&
								mainWindow is FwXWindow &&
								mainWindow != activeWindow)
							{
								(mainWindow as FwXWindow).SaveSettings();
							}
						}
					}
					// save the settings for activeWindow last.
					activeWindow.SaveSettings();
					Form fwMainWindow = NewMainAppWnd(cache, isNewCache, null, false);
					m_rgMainWindows.Add((IFwMainWnd)fwMainWindow);
					AdjustNewWindowPosition(fwMainWindow, activeWindow);
					fwMainWindow.Show();
					((IFwMainWnd)fwMainWindow).InitAndShowClient();
				}
			}
		}

		#endregion // Message Delegates

		#region FwApp required methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main X window
		/// </summary>
		///
		/// <param name="cache">Instance of the FW Data Objects cache that the new main window
		/// will use for accessing the database.</param>
		/// <param name="isNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override Form NewMainAppWnd(FdoCache cache, bool isNewCache, Form wndCopyFrom,
			bool fOpeningNewProject)
		{
			// Review: I put in this exception catching block because
			// the exception handler is not been invoked...until we figure out why
			// not, this is better than just having the raw exception in the users face.
			try
			{
				if (isNewCache)
				{
					// TODO: Do any needed initialization here.
				}
				Stream iconStream = ApplicationIconStream;
				Debug.Assert(iconStream != null, "Couldn't find the specified application icon as a resource.");
				string configFile;
				if (m_commandLineArgs.ContainsKey("x"))
				{
					configFile = m_commandLineArgs["x"][0];
				}
				else
				{
					configFile = DirectoryFinder.GetFWCodeFile(DefaultConfigurationPathname);
					//					configFile = (string)SettingsKey.GetValue("LatestConfigurationFile",
					//						Path.Combine(DirectoryFinder.FWCodeDirectory,
					//						DefaultConfigurationPathname));
					if (!File.Exists(configFile))
						configFile = null;
				}
				FwXWindow result;
				if (configFile != null)
					result = new FwXWindow(cache, wndCopyFrom, iconStream, configFile, false);
				else
				{
					// try to load from stream
					return new FwXWindow(cache, wndCopyFrom, iconStream, ConfigurationStream);
				}
				if (isNewCache)
				{
					// Must be done after reading properties from init table.
					if (result.PropertyTable.GetBoolProperty("SendSync", false) && SyncGuid != Guid.Empty)
						cache.MakeDbSyncRecords(SyncGuid);
				}

				if (m_commandLineArgs.ContainsKey("link"))
					result.StartupAtURL(m_commandLineArgs["link"][0]);

				return result;
			}
			catch (Exception error)
			{
				HandleTopLevelError(this, new System.Threading.ThreadExceptionEventArgs(error));
				return null;
			}
		}

		/// <summary>
		/// override this with the name of your icon
		/// This icon file should be included in the assembly, and its "build action" should be set to "embedded resource"
		/// </summary>
		protected virtual string ApplicationIconName
		{
			get { return "app.ico"; }
		}

		/// <summary>
		/// Get a stream on the application icon.
		/// </summary>
		protected System.IO.Stream ApplicationIconStream
		{
			get
			{
				System.IO.Stream iconStream = null;
				System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(GetType());
				string expectedIconName = ApplicationIconName.ToLowerInvariant();
				foreach(string resourcename in assembly.GetManifestResourceNames())
				{
					if(resourcename.ToLowerInvariant().EndsWith(expectedIconName))
					{
						iconStream = assembly.GetManifestResourceStream(resourcename);
						Debug.Assert(iconStream != null, "Could not load the " + ApplicationIconName + " resource.");
						break;
					}
				}
				return iconStream;
			}
		}

		#endregion // FwApp required methods

		#region Other methods

		protected override void DoApplicationInitialization()
		{
			base.DoApplicationInitialization();
			if (IsTEInstalled)
			{
				ScrReference.InitializeVersification(DirectoryFinder.GetFWCodeSubDirectory(
					"Translation Editor"), false);
			}
			//usage report
			SIL.Utils.UsageEmailDialog.DoTrivialUsageReport("FLEXUsage@sil.org", xWorksStrings.ThankYouForCheckingOutFlex, new int[] { 1 });
			SIL.Utils.UsageEmailDialog.DoTrivialUsageReport("FLEXUsage@sil.org", xWorksStrings.HaveLaunchedFLEXTenTimes, new int[] { 10 });
			SIL.Utils.UsageEmailDialog.DoTrivialUsageReport("FLEXUsage@sil.org", xWorksStrings.HaveLaunchedFLEXFortyTimes, new int[] { 40 });
		}
		/// <summary>
		/// Indicated whether TE is installed or not;
		/// </summary>
		public static bool IsTEInstalled
		{
			get { return MiscUtils.IsTEInstalled; }
		}

		#endregion // Other methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the exception handler.
		/// </summary>
		/// <remarks> we override this because for a configurable application, sometimes you just
		/// want to tell the person writing a configuration that they made a typo.</remarks>
		/// ------------------------------------------------------------------------------------
		protected override void SetGlobalExceptionHandler()
		{
			Application.ThreadException += new ThreadExceptionEventHandler(HandleTopLevelError);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Catches and displays a otherwise unhandled exception.
		/// </summary>
		/// <param name="sender">sender</param>
		/// <param name="eventArgs">Exception</param>
		/// <remarks> we override this because for a configurable application, sometimes you just
		/// want to tell the person writing a configuration that they made a typo.</remarks>
		/// ------------------------------------------------------------------------------------
		protected override void HandleTopLevelError(object sender, ThreadExceptionEventArgs eventArgs)
		{
			CheckDisposed();

			if (BasicUtils.IsUnsupportedCultureException(eventArgs.Exception)) // LT-8248
			{
				Logger.WriteEvent("Unsupported culture: " + eventArgs.Exception.Message);
				return;
			}

			ErrorReporter.ReportException (eventArgs.Exception);
			//			if (eventArgs.Exception is ConfigurationException)
			//				((ConfigurationException)eventArgs.Exception).ShowDialog();
			//			else
			//				base.HandleTopLevelError(sender, eventArgs);
		}
	}
}
