// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2006' company='SIL International'>
//    Copyright (c) 2006, SIL International. All Rights Reserved.
// </copyright>
//
// File: FwApp.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;
using System.Security;

namespace SIL.FieldWorks.Common.Framework
{
	// The following three interfaces are used in DetailControls and XWorks.  This seems like a
	// suitably general namespace for them.  Whether they belong in this particular file or
	// not, they need to go somewhere.
	#region IRecordListUpdater interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is implemented to help handle side-effects of changing the contents of an
	/// object that may be stored in a list by providing access to that list.
	/// </summary>
	/// <remarks>Hungarian: rlu</remarks>
	/// ----------------------------------------------------------------------------------------
	public interface IRecordListUpdater
	{
		/// <summary>Set the IRecordChangeHandler object for this list.</summary>
		IRecordChangeHandler RecordChangeHandler { set; }
		/// <summary>Update the list, possibly calling IRecordChangeHandler.Fixup() first.
		/// </summary>
		void UpdateList(bool fRefreshRecord);

		/// <summary>
		/// just update the current record
		/// </summary>
		void RefreshCurrentRecord();
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is implemented to help handle side-effects of changing the contents of an
	/// object that may be stored in a list.  Its single method returns either null or the
	/// list object stored under the given name.
	/// </summary>
	/// <remarks>Hungarian: rlo</remarks>
	/// ----------------------------------------------------------------------------------------
	public interface IRecordListOwner
	{
		/// <summary>Find the IRecordListUpdater object with the given name.</summary>
		IRecordListUpdater FindRecordListUpdater(string name);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is implemented to handle side-effects of changing the contents of an
	/// object that may be stored in a list.  If it is stored in a list, then the Fixup() method
	/// must be called before refreshing the list in order to ensure that those side-effects
	/// have occurred properly before redisplaying.
	/// </summary>
	/// <remarks>Hungarian: rch</remarks>
	/// ----------------------------------------------------------------------------------------
	public interface IRecordChangeHandler : IFWDisposable
	{
		/// <summary>Initialize the object with the record and the list to which it belongs.
		/// </summary>
		void Setup(object /*"record"*/ o, IRecordListUpdater rlu);
		/// <summary>Fix the record for any changes, possibly refreshing the list to which it
		/// belongs.</summary>
		void Fixup(bool fRefreshList);

		/// <summary>
		/// True, if the updater was not null in the Setup call, otherwise false.
		/// </summary>
		bool HasRecordListUpdater
		{
			get;
		}

		/// <summary>
		/// Let users know it is beiong dispsoed
		/// </summary>
		event EventHandler Disposed;
	}
	#endregion

	#region Enumerations
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The different window tiling options
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum WindowTiling
	{
		/// <summary>Top to bottom (horizontal)</summary>
		Stacked,
		/// <summary>Side by side (vertical)</summary>
		SideBySide,
	};

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This is used for menu items and toolbar buttons. Multiple strings for each command
	/// are stored together in the same string resource. See AfApp::GetResourceStr for more
	/// information.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public enum ResourceStringType
	{
		/// <summary></summary>
		krstHoverEnabled,
		/// <summary></summary>
		krstHoverDisabled,
		/// <summary></summary>
		krstStatusEnabled,
		/// <summary></summary>
		krstStatusDisabled,
		/// <summary></summary>
		krstItem,
	};
	#endregion

	#region FwApp class
	/// ---------------------------------------------------------------------------------------
	/// <remarks>
	/// Base application for .net FieldWorks apps (i.e., replacement for AfApp)
	/// </remarks>
	/// ---------------------------------------------------------------------------------------
	abstract public class FwApp : IApp, ISettings, IFWDisposable, IBackupDelegates,
		IFwTool, IHelpTopicProvider, IMessageFilter
	{
		#region SuppressedCacheInfo
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper class that contains queued SyncInfo events and a reference count for
		/// Suppress/ResumeSynchronize.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class SuppressedCacheInfo
		{
			/// <summary>Reference count</summary>
			public int Count = 1;
			/// <summary>SyncInfo queue</summary>
			public Queue<SyncInfo> Queue = new Queue<SyncInfo>();
		}
		#endregion

		#region Member variables
		private static FwApp s_app;
		/// <summary>We use this control to display the error reporting dialog on the main
		/// thread. Otherwise we get a crash when we try to copy the error to the clipboard
		/// when this gets called from the Finalizer (which runs on a separate thread)</summary>
		private Control m_HelperControl;
		private delegate bool DisplayErrorMethod(Exception e, bool fTerminating);

		private bool m_fOkToClose = true; // temporarily suppress close app during close window.
		private bool m_fSuppressClose; // special case, suppresses quit during backup.
		int m_nLastSync; // id of last sync record in database processed by SyncFromDb.
		/// <summary></summary>
		protected Dictionary<string, List<String>> m_commandLineArgs = new Dictionary<string, List<String>>();
		/// <summary></summary>
		protected List<IFwMainWnd> m_rgMainWindows = new List<IFwMainWnd>(1);
		/// <summary>
		/// One of m_rgMainWindows, the one most recently activated.
		/// </summary>
		protected Form m_activeMainWindow;
		/// <summary></summary>
		private int m_nEnableLevel;
		/// <summary></summary>
		protected static System.Resources.ResourceManager s_Resources;
		/// <summary></summary>
		protected static string s_sWsUser = string.Empty;
		/// <summary></summary>
		protected Dictionary<string, FdoCache> m_caches = new Dictionary<string, FdoCache>();
		/// <summary>Splash Screen</summary>
		protected IFwSplashScreen m_SplashScreenWnd;
		/// <summary></summary>
		protected FwFindReplaceDlg m_findReplaceDlg;
		private bool m_autoStartLibronix;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value for the AutoStartLibronix setting (Used only by TE).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AutoStartLibronix
		{
			get { return m_autoStartLibronix; }
			set { m_autoStartLibronix = value; }
		}
#if DEBUG
		/// <summary></summary>
		protected DebugProcs m_debugProcs;
#endif
		private Dictionary<FdoCache, SuppressedCacheInfo> m_suppressedCaches =
			new Dictionary<FdoCache, SuppressedCacheInfo>();
		private Dictionary<FdoCache, bool> m_refreshViewCaches = new Dictionary<FdoCache, bool>();
		/// <summary>The find patterns for the find/replace dialog, one for each database.</summary>
		/// <remarks>We need one pattern per database (cache). Otherwise it'll crash when we try to
		/// load the previous search term because the new database has different writing system hvos
		/// (TE-5598).</remarks>
		protected Dictionary<FdoCache, IVwPattern> m_findPatterns = new Dictionary<FdoCache, IVwPattern>();
		#endregion

		delegate void FakeDelegate();

		#region Construction and Initializing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwApp(): this(null)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for FwApp takes an array of command-line arguments.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FwApp(string[] rgArgs)
		{
#if DEBUG
			m_debugProcs = new DebugProcs();
#endif
			SetGlobalExceptionHandler();

			if (s_app != null)
			{
				throw new InvalidOperationException("Multiple instances of the FwApp object " +
					"are not allowed.");
				// The following code is pointless since right away we catch the exception we might throw.
				// If s_app is not null then this is clearly a case where we didn't call Dispose(),
				// so it is legitimate to throw an InvalidOperationException. The commented out code
				// was added in changelist 10709.
				//// If s_app is pointing to something, make sure it's still a valid object
				//// by referencing the ResourceString() method. If that doesn't cause an
				//// error, then it's not valid to instantiate another application object.
				//// The dispose method sets s_app to null but there might be a case where
				//// we try to instantiate an FwApp before the garbage collection has taken
				//// place on a previous instantiation (e.g. in tests).
				//try
				//{
				//    if (FwApp.GetResourceString(string.Empty) == string.Empty)
				//    {
				//        throw new InvalidOperationException("Multiple instances of the FwApp object " +
				//            "are not allowed");
				//    }
				//}
				//catch
				//{
				//}
			}

			s_app = this;

			// Fix for TE-975.
			// Force the broadcast window to be initialized in the current thread.
			// See http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=911603&SiteID=1
			FakeDelegate fd = delegate() {};
			Microsoft.Win32.SystemEvents.InvokeOnEventsThread(fd);

			m_HelperControl = new Control();
			m_HelperControl.CreateControl();

			Application.AddMessageFilter(this);

			// Store the command line arguments for use by each new application window.
			try
			{
				m_commandLineArgs = ParseCommandLine(rgArgs);
				SetUICulture();
			}
			catch (Exception e)
			{
				ErrorReporter.ReportException(e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the UI culture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetUICulture()
		{
			List<string> culture;
			string locale = SettingsKey.GetValue("UserWs", string.Empty) as string;
			string origLocale = Thread.CurrentThread.CurrentUICulture.Name;
			bool valid = false;
			bool validationAttempt = false;

			if (m_commandLineArgs.TryGetValue("locale", out culture))
			{
				// Try the UI locale was found on the command-line;
				origLocale = culture[0];
				locale = origLocale.Replace('_', '-');
				valid = ValidateCulture(locale);
				validationAttempt = true;
			}
			else if (!string.IsNullOrEmpty(locale))
			{
				// Try the UI locale found in the registry.
				origLocale = locale;
				locale = locale.Replace('_', '-');
				valid = ValidateCulture(locale);
				validationAttempt = true;
			}

			if (!valid)
			{
				// Try the default OS UI language. Fallback to US English if that fails.
				locale = Thread.CurrentThread.CurrentUICulture.Name;
				if (ValidateCulture(locale))
					valid = !validationAttempt;
				else
					locale = "en-US";
			}

			Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(locale);

			if (!valid)
			{
				string fmt = ResourceHelper.GetResourceString("kstidUILangNotFound");
				MessageBox.Show(string.Format(fmt, origLocale), Application.ProductName,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			s_sWsUser = Thread.CurrentThread.CurrentUICulture.Name.Replace('-', '_');

			// The thread writing system may be fine now, but the writing system we
			// store and throw around must be defined in ICU. Therefore, make sure
			// the we have a region in ICU that matches the thread's UI culture region.
			// If not, then fall back to only the country.
			while (!Icu.IsValidFwWritingSystem(s_sWsUser))
			{
				int i = s_sWsUser.LastIndexOf('_');
				if (i < 0)
				{
					// We should never get here. :o)
					s_sWsUser = "en";
					return;
				}

				s_sWsUser = s_sWsUser.Remove(i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates whether or not the specified culture ID has a valid langauge definition
		/// file.
		/// </summary>
		/// <param name="cultureId">The culture id to validate.</param>
		/// <returns>True if Windows and FieldWorks recognize the language as valid.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ValidateCulture(string cultureId)
		{
			CultureInfo ci = MiscUtils.GetCultureForWs(cultureId);
			if (ci != null)
			{
				// Now that we've verified that .Net recognizes the culture Id
				// as a valid culture, we need to verify that FieldWorks has a
				// valid language definition for it.
				if (Icu.IsValidFwWritingSystem(ci.Name.Replace('-', '_')))
					return true;

				if (Icu.IsValidFwWritingSystem(ci.TwoLetterISOLanguageName))
					return true;

				if (Icu.IsValidFwWritingSystem(ci.ThreeLetterISOLanguageName))
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the exception handler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void SetGlobalExceptionHandler()
		{
			// Set exception handler. Needs to be done before we create splash screen
			// (don't understand why, but otherwise some exceptions don't get caught)
			// Using Application.ThreadException rather than
			// AppDomain.CurrentDomain.UnhandledException has the advantage that the program
			// doesn't necessarily ends - we can ignore the exception and continue.
			Application.ThreadException += new ThreadExceptionEventHandler(HandleTopLevelError);

			// we also want to catch the UnhandledExceptions for all the cases that
			// ThreadException don't catch, e.g. in the startup.
			AppDomain.CurrentDomain.UnhandledException +=
				new UnhandledExceptionEventHandler(HandleUnhandledException);
		}



		[DllImport("kernel32.dll")]
		private extern static IntPtr LoadLibrary(string fileName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the program
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Run()
		{
			CheckDisposed();

			try
			{
				Thread.CurrentThread.Name = "Main thread";

				// JohnT: this allows us to use Graphite in all in-process controls, even those
				// we don't have custom versions of.
				LoadLibrary("multiscribe.dll");

				if (m_commandLineArgs.ContainsKey("help"))
				{
					ShowCommandLineHelp();
					return;
				}

				if (DateTime.Now > DropDeadDate)
				{
					MessageBox.Show(GetResourceString("kstidDropDead"), null, MessageBoxButtons.OK,
						MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
						MessageBoxOptions.ServiceNotification);
					return;
				}
				FwRegistrySettings.LatestAppStartupTime = DateTime.Now.ToUniversalTime().Ticks.ToString();

				object[] attributes;
				Assembly assembly = Assembly.GetEntryAssembly();
				if (assembly != null)
				{
					attributes =
						assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
					string version;
					if (attributes != null && attributes.Length > 0)
						version = ((AssemblyFileVersionAttribute)attributes[0]).Version;
					else
						version = Application.ProductVersion;
					// Extract the fourth (and final) field of the version to get a date value.
					int ich = version.IndexOf('.');
					if (ich >= 0)
						ich = version.IndexOf('.', ich + 1);
					if (ich >= 0)
						ich = version.IndexOf('.', ich + 1);
					if (ich >= 0)
					{
						int iDate = System.Convert.ToInt32(version.Substring(ich + 1));
						if (iDate > 0)
						{
							double oadate = System.Convert.ToDouble(iDate);
							DateTime dt = DateTime.FromOADate(oadate);
							version += string.Format("  {0}", dt.ToString("yyyy/MM/dd"));
						}
					}
#if DEBUG
					version += "  (Debug version)";
#endif
					ErrorReporter.AddProperty("Version", version);
				}
				ErrorReporter.AddProperty("CommandLine", System.Environment.CommandLine);
				ErrorReporter.AddProperty("CurrentDirectory", System.Environment.CurrentDirectory);
				ErrorReporter.AddProperty("MachineName", System.Environment.MachineName);
				ErrorReporter.AddProperty("OSVersion", System.Environment.OSVersion.ToString());
				ErrorReporter.AddProperty("CLR version", System.Environment.Version.ToString());
				ulong mem = MiscUtils.GetPhysicalMemoryBytes() / 1048576;
				ErrorReporter.AddProperty("PhysicalMemory", mem.ToString() + " Mb");
				ulong diskSize;
				ulong diskFree;
				int cDisks = MiscUtils.GetDiskDriveStats(out diskSize, out diskFree);
				diskFree /= 1048576;
				diskSize /= 1048576;
				ErrorReporter.AddProperty("LocalDiskCount", cDisks.ToString());
				ErrorReporter.AddProperty("TotalLocalDiskSize", diskSize.ToString() + " Mb");
				ErrorReporter.AddProperty("TotalLocalDiskFree", diskFree.ToString() + " Mb");
				ErrorReporter.AddProperty("WorkingSet", System.Environment.WorkingSet.ToString());
				ErrorReporter.AddProperty("UserDomainName", System.Environment.UserDomainName);
				ErrorReporter.AddProperty("UserName", System.Environment.UserName);
				ErrorReporter.AddProperty("SystemDirectory", System.Environment.SystemDirectory);
				ErrorReporter.AddProperty("Culture", System.Globalization.CultureInfo.CurrentCulture.ToString());
				using (Bitmap bm = new Bitmap(10, 10))
				{

					ErrorReporter.AddProperty("ScreenDpiX", bm.HorizontalResolution.ToString());
					ErrorReporter.AddProperty("ScreenDpiY", bm.VerticalResolution.ToString());
				}
				FwRegistrySettings.AddErrorReportingInfo();
				UsageEmailDialog.IncrementLaunchCount();	// count launches for bug reporting

				if (!FwRegistrySettings.DisableSplashScreenSetting)
					ShowSplashScreen();
				DoApplicationInitialization();
				NewMainWindow(null, false);
				// don't rely on return value of NewMainWindow - that could be disposed if the
				// user chooses not to display an empty project and opens a different one instead.
				if (ActiveMainWindow != null)
				{
					if (!RunningTests())
					{
						// Check schedule for backup reminder.
						DIFwBackupDb backupDb = FwBackupClass.Create();
						backupDb.Init(this, ActiveMainWindow.Handle.ToInt32());
						backupDb.CheckForMissedSchedules(this);
						backupDb.Close();
					}
					Application.Run();
				}
			}
			catch (ApplicationException ex)
			{
				MessageBox.Show(ex.Message);
			}
			catch (Exception ex)
			{
				ErrorReporter.ReportException(ex);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void DoApplicationInitialization()
		{
			// Keyman has an option to change the system keyboard with a keyman keyboard change.
			// Unfortunately, this messes up changing writing systems in FieldWorks when Keyman
			// is running. The only fix seems to be to turn that option off... (TE-8686)
			try
			{
				RegistryKey engineKey = Registry.CurrentUser.OpenSubKey(
					@"Software\Tavultesoft\Keyman Engine", false);
				if (engineKey != null)
				{
					foreach (string version in engineKey.GetSubKeyNames())
					{
						RegistryKey keyVersion = engineKey.OpenSubKey(version, true);
						if (keyVersion != null)
						{
							object value = keyVersion.GetValue("switch language with keyboard");
							if (value == null || (int)value != 0)
								keyVersion.SetValue("switch language with keyboard", 0);
						}
					}
				}
			}
			catch (SecurityException)
			{
				// User doesn't have access to the registry key, so just hope the user is fine
				// with what he gets.
			}

			// Application.EnableVisualStyles();
			LoadSettings(SettingsKey);
			Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
		}

		void Application_ApplicationExit(object sender, EventArgs e)
		{
			Application.ApplicationExit -= new EventHandler(Application_ApplicationExit);
			SaveSettings(SettingsKey);
		}

		/// <summary>
		/// First (and currently only) step in the default DoApplicationInitialization, allows
		/// a separate overide of loading settings. If you override this you probably also want
		/// to override SaveSettings.
		/// </summary>
		protected virtual void LoadSettings(RegistryKey key)
		{
		}

		/// <summary>
		/// Triggered by Application.ApplicationExit, this is a possible override point for
		/// saving any settings to be loaded by LoadSettings.
		/// This is the real place to save settings, as opposed to SaveSettingsNow, which is
		/// a dummy implementation required because (for the sake of the SettingsKey method)
		/// we implement ISettings.
		/// </summary>
		protected virtual void SaveSettings(RegistryKey key)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is called when an application is started and any time the user wants to open
		/// an additional main window. It attempts to create and show a new main application
		/// window with a connection to the DB.
		/// </summary>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>The newly created MainWindow as a Form is successful, null otherwise.
		/// </returns>
		/// <exception cref="ApplicationException">Could not establish a connection to the
		/// database</exception>
		/// -----------------------------------------------------------------------------------
		public Form NewMainWindow(Form wndCopyFrom, bool fOpeningNewProject)
		{
			CheckDisposed();

			return NewMainWindow(wndCopyFrom, fOpeningNewProject, false);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Enhanced form of method (needed for test code).
		/// </summary>
		/// <param name="wndCopyFrom"></param>
		/// <param name="fOpeningNewProject"></param>
		/// <param name="fBypassInstall">If true, disable cache from calling InstallLanguage</param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public Form NewMainWindow(Form wndCopyFrom, bool fOpeningNewProject, bool fBypassInstall)
		{
			CheckDisposed();

			Debug.Assert(wndCopyFrom == null || wndCopyFrom is IFwMainWnd,
				"Form passed as parameter to NewMainWindow must implement IFwMainWnd");

			// get the cache
			FdoCache cache = null;
			bool fNewCache = false;
			Form fwMainWindow = null;
			string project = null;
			try
			{
				if (wndCopyFrom != null)
				{
					cache = ((IFwMainWnd)wndCopyFrom).Cache;
					if (fBypassInstall)
					{
						ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
						if (wsf != null)
							wsf.BypassInstall = true;
					}
				}
				else
				{
					if (FwRegistrySettings.StartupSuccessfulSetting)
					{
						// Creating the original window.
						// Get the cache name from command line args or Registry.
						Debug.Assert(m_commandLineArgs != null);
						cache = GetCache();
						fNewCache = true;

						// Should be set to true in CreateNewMainWindow() after
						// creating a new main window and showing it.
						FwRegistrySettings.StartupSuccessfulSetting = false;
					}

					// If there is no language project, then possibly show a
					// message asking user about opening a sample DB.
					if (cache == null || cache.LangProject == null)
					{
						fNewCache = ShowFirstTimeMessage(out cache);
						// we have to add the newly created cache to the Hashtable
						if (fNewCache)
							GetCache(ref cache);
					}
					if (fBypassInstall && cache != null)
					{
						ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
						if (wsf != null)
							wsf.BypassInstall = true;
					}
					if (cache == null || cache.LangProject == null)
					{
						// If there is no language project, then ask what should be done.
						CloseSplashScreen();
						if (ShowWelcomeDialog())
							return ActiveMainWindow;
						else
							return null;
					}
					if (cache.LangProject.CurAnalysisWssRS.Count == 0)
					{
						CloseSplashScreen();
						MessageBox.Show(GetResourceString("kstidNoAnalysisWss"),
							GetResourceString("kstidWarning"));
						cache.LangProject.CurAnalysisWssRS.Append(
							cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en"));
						cache.LangProject.CacheDefaultWritingSystems();
					}
					if (cache.LangProject.CurVernWssRS.Count == 0)
					{
						CloseSplashScreen();
						int hvoDefVern = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
						if (cache.LangProject.VernWssRC.Count > 0)
							hvoDefVern = cache.LangProject.VernWssRC.HvoArray[0];
						MessageBox.Show(GetResourceString("kstidNoVernWritingSystems"),
							GetResourceString("kstidWarning"));
						cache.LangProject.CurVernWssRS.Append(hvoDefVern);
						cache.LangProject.CacheDefaultWritingSystems();
					}
				}
				project = cache.LangProject.Name.BestAnalysisVernacularAlternative.Text;
				fwMainWindow = CreateNewMainWindow(cache, fNewCache, wndCopyFrom, fOpeningNewProject);
				if (fNewCache && fwMainWindow != null)
					cache.Save(); // Prevent initial undo item
			}
			catch (Exception e)
			{
				Logger.WriteEvent("Very bad error happened in FwApp.NewMainWindow:");
				Logger.WriteError(e);
				if (cache != null)
				{
					// Remove the cache from our Hashtable so that TE really closes when
					// user tries to close it - otherwise our disposed cache keeps hanging
					// around and prevents TE from closing (doesn't show a form, but is still
					// running). This causes all kinds of interesting problems...
					try
					{
						if (fwMainWindow != null)
						{
							fwMainWindow.Close();
							fwMainWindow = null;
						}
					}
					catch
					{
					}
					RemoveFdoCache(cache); // also calls cache.Dispose()
				}
				if (project == null || e is NullReferenceException)
					throw;
				else
				{
					throw new ApplicationException(
						string.Format(GetResourceString("kstidOpenProjError"), project, e.Message),
						e);
				}
			}
			finally
			{
				CloseSplashScreen();
			}

			return fwMainWindow;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a new main window using command line arguments.
		/// </summary>
		/// <param name="args"></param>
		/// <remarks>This method is used when another instance of TE is started with
		/// command line arguments. It will pass those command line arguments to this instance
		/// to open the new project.</remarks>
		/// ------------------------------------------------------------------------------------
		public Form NewMainWindow(string[] args)
		{
			CheckDisposed();

			m_commandLineArgs = ParseCommandLine(args);
			return NewMainWindow(null, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks to see whether the sample DB is available and, if so, displays a message box
		/// asking the user whether or not he wants to open it.
		/// (TE-3473)
		/// </summary>
		/// <param name="cache">(output)</param>
		/// <returns><c>true</c> if a connection to the sample DB is made; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool ShowFirstTimeMessage(out FdoCache cache)
		{
			cache = null;
			// Need to set this in case the user starts up a new main window from the
			// Advertisement prompt or welcome dialog.
			FwRegistrySettings.StartupSuccessfulSetting = true;

			if (string.IsNullOrEmpty(SampleDatabase) || !FwRegistrySettings.FirstTimeAppHasBeenRun)
				return false;

			// First, attempt a connection to the sample DB before offering the user the
			// option of opening it.
			if (CheckDbVerCompatibility(MiscUtils.LocalServerName, SampleDatabase))
				cache = FdoCache.Create(SampleDatabase);
			if (cache != null && ShowFirstTimeMessageDlg())
				return true;

			if (cache != null)
				cache.Dispose();

			cache = null;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays a message box asking the user whether or not he wants to open a sample DB.
		/// </summary>
		/// <returns><c>true</c> if user consented to opening the sample database; <c>false</c>
		/// otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ShowFirstTimeMessageDlg()
		{
			string sCaption = GetResourceString("kstidTrainingAvailable");
			string sMsg = GetResourceString("kstidOpenSampleDbMsg");
			return (MessageBox.Show(sMsg, sCaption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
				MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays fieldworks welcome dialog.
		/// </summary>
		/// <returns><c>true</c> if an option has been chose which results in a valid
		/// project being opened; <c>false</c> if the user chooses to exit.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShowWelcomeDialog()
		{
			CheckDisposed();

			// Continue to ask for an option until one is selected.
			DialogResult result = DialogResult.Cancel;
			while (result == DialogResult.Cancel)
			{

				NoProjectFoundDlg dlg = new NoProjectFoundDlg(this);

				//using (NoProjectFoundDlg dlg = new NoProjectFoundDlg(this))
				//{
					dlg.StartPosition = FormStartPosition.CenterScreen;
					DialogResult welcomeResult = dlg.ShowDialog();

					switch (dlg.DlgResult)
					{
						case NoProjectFoundDlg.ButtonPress.New:
							result = NewProjectDialog(dlg);
							break;
						case NoProjectFoundDlg.ButtonPress.Open:
							result = OpenProjectDialog(dlg, null, null);
							break;
						case NoProjectFoundDlg.ButtonPress.Restore:
							DIFwBackupDb backupSystem = FwBackupClass.Create();
							backupSystem.Init(this, dlg.Handle.ToInt32());
							result = (backupSystem.UserConfigure(this, true) == 4 ?
								DialogResult.OK : DialogResult.Cancel);
							backupSystem.Close();
							break;
						case NoProjectFoundDlg.ButtonPress.Exit:
							return false;
					}
				//}

					dlg.Dispose();
					dlg = null;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [last main WND closed].
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LastMainWndClosed(Form frm)
		{
			ShowWelcomeDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new main window. This method is called from
		/// <see cref="SIL.FieldWorks.Common.Framework.FwApp.NewMainWindow(System.Windows.Forms.Form, bool)"/>
		/// and <see cref="ReopenDbAndOneWindow"/>
		/// </summary>
		/// <param name="cache">Cache</param>
		/// <param name="fNewCache"><c>true</c> if we didn't reuse an existing cache</param>
		/// <param name="wndCopyFrom">Form to copy from, or <c>null</c></param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a new project</param>
		/// <returns>The new main window</returns>
		/// ------------------------------------------------------------------------------------
		private Form CreateNewMainWindow(FdoCache cache, bool fNewCache, Form wndCopyFrom,
			bool fOpeningNewProject)
		{
			string databaseName = MiscUtils.IsServerLocal(cache.ServerName) ? cache.DatabaseName :
				cache.ServerMachineName + "\\" + cache.DatabaseName;

			WriteSplashScreen(
				string.Format(GetResourceString("kstidLoadingProject"), databaseName));
			// construct the new window, of the proper derived type
			Form fwMainWindow = NewMainAppWnd(cache, fNewCache, wndCopyFrom, fOpeningNewProject);
			if (fwMainWindow == null)
			{
				// if we couldn't create a new main window but earlier we created a new cache
				// we better remove it, otherwise the app won't ever exit. (Usually this is
				// done when the main window gets closed, but since we don't have a window
				// this doesn't happen)
				if (fNewCache)
					RemoveFdoCache(cache);
				return null;
			}

			WriteSplashScreen(GetResourceString("kstidInitWindow"));

			using (new DataUpdateMonitor(fwMainWindow, cache.MainCacheAccessor, null, "Creating new main window"))
			{
				InitAndShowMainWindow(fwMainWindow, wndCopyFrom);

				// Set to true to tell the app that the window was created successfully.
			}

			CloseSplashScreen();

			if (!((IFwMainWnd)fwMainWindow).OnFinishedInit())
				return null;	// did not initialize properly!

			FwRegistrySettings.StartupSuccessfulSetting = true;

			return fwMainWindow;
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
			fwMainWindow.Closing += new CancelEventHandler(OnClosingWindow);
			fwMainWindow.Closed += new EventHandler(OnWindowClosed);
			m_rgMainWindows.Add(fwMainWnd);
			fwMainWindow.Activated += new EventHandler(fwMainWindow_Activated);
			if (fwMainWindow == Form.ActiveForm)
				m_activeMainWindow = fwMainWindow;
			fwMainWindow.HandleDestroyed += new EventHandler(fwMainWindow_HandleDestroyed);

			// finalize and show the new window
			fwMainWindow.Show(); // Show method loads persisted settings for window & controls
			fwMainWindow.Activate(); // This makes main window come to front after splash screen closes

			if (fwMainWindow is FwMainWnd)
				((FwMainWnd)fwMainWindow).HandleActivation();

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

			// Anything we do here we don't want the user to be able to undo!
			using (new SuppressSubTasks(fwMainWnd.Cache))
			{
				((IFwMainWnd)fwMainWindow).InitAndShowClient();
			}
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
			Rectangle rcScrn = Screen.FromRectangle(rcNewWnd).WorkingArea;

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
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// True, if the object is being disposed.
		/// </summary>
		/// <remarks>
		/// Don't even think of making this anything but private,
		/// since we don't want subclasses to set it.
		/// </remarks>
		private bool m_beingDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// See if the object is being disposed.
		/// </summary>
		/// <remarks>
		/// Don't make a setter for this, since we don't want anyone else to set it.
		/// </remarks>
		public bool BeingDisposed
		{
			get { return m_beingDisposed; }
		}

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed || m_beingDisposed)
				return;
			m_beingDisposed = true;

			UpdateAppRuntimeCounter();

			if (disposing)
			{
				FwRegistrySettings.FirstTimeAppHasBeenRun = false;

				// Dispose managed resources here.
				Logger.ShutDown();
				Application.ThreadException -= new ThreadExceptionEventHandler(HandleTopLevelError);

				List<IFwMainWnd> mainWnds = new List<IFwMainWnd>(m_rgMainWindows); // Use another array, since m_rgMainWindows may change.
				m_rgMainWindows.Clear(); // In fact, just clear the main array, so the windows won't have to worry so much.
				foreach (IFwMainWnd mainWnd in mainWnds)
				{
					if (mainWnd is Form)
					{
						Form wnd = (Form)mainWnd;
						wnd.Closing -= new CancelEventHandler(OnClosingWindow);
						wnd.Closed -= new EventHandler(OnWindowClosed);
						wnd.Activated -= new EventHandler(fwMainWindow_Activated);
						wnd.HandleDestroyed -= new EventHandler(fwMainWindow_HandleDestroyed);
						wnd.Dispose();
					}
					else if (mainWnd is IDisposable)
						((IDisposable)mainWnd).Dispose();
				}
				if (m_caches != null)
				{
					foreach (FdoCache cache in m_caches.Values)
						cache.Dispose();
					m_caches.Clear();
				}
				if (m_findReplaceDlg != null)
					m_findReplaceDlg.Dispose();
#if DEBUG
				if (m_debugProcs != null)
					m_debugProcs.Dispose();
#endif
				// Close the splash screen if, for some reason, it's still hanging around. It
				// really shouldn't still be around by this time, except when some testing code
				// instantiates FwApp objects. This will make sure the splash screen goes away
				// when the FwApp object goes out of scope.
				CloseSplashScreen();
				ResourceHelper.ShutdownHelper();
				if (m_rgMainWindows != null)
					m_rgMainWindows.Clear();
				if (m_commandLineArgs != null)
					m_commandLineArgs.Clear();
				if (m_suppressedCaches != null)
					m_suppressedCaches.Clear();
				if (m_refreshViewCaches != null)
					m_refreshViewCaches.Clear();
				if (m_findPatterns != null)
					m_findPatterns.Clear();

				Application.RemoveMessageFilter(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rgMainWindows = null;
			m_activeMainWindow = null;
			m_commandLineArgs = null;
			m_SplashScreenWnd = null;
			m_findPatterns = null;
			m_findReplaceDlg = null;
			m_suppressedCaches = null;
			m_refreshViewCaches = null;
#if DEBUG
			m_debugProcs = null;
#endif
			m_caches = null;
			s_app = null;

			m_isDisposed = true;
			m_beingDisposed = false;
		}

		private void UpdateAppRuntimeCounter()
		{
			int csec = FwRegistrySettings.TotalAppRuntime;
			string sStartup = FwRegistrySettings.LatestAppStartupTime;
			long start;
			if (!String.IsNullOrEmpty(sStartup) && long.TryParse(sStartup, out start))
			{
				DateTime started = new DateTime(start);
				DateTime finished = DateTime.Now.ToUniversalTime();
				TimeSpan delta = finished - started;
				csec += (int)delta.TotalSeconds;
				FwRegistrySettings.TotalAppRuntime = csec;
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
		virtual protected void OnClosingWindow(object sender, CancelEventArgs e)
		{
			if (sender is FwMainWnd)
			{
				FwMainWnd wnd = (FwMainWnd)sender;
				while (wnd.Cache.IsBusy || m_suppressedCaches.ContainsKey(wnd.Cache))
					Application.DoEvents();

				Logger.WriteEvent(string.Format("Exiting {0} for {1}\\{2}", wnd.Name,
					wnd.Cache.ServerName, wnd.Cache.DatabaseName));
			}

			if (sender is IFwMainWnd)
			{
				if (FindReplaceDialog != null)
				{
					foreach (IFwMainWnd fwWnd in MainWindows)
					{
						if (fwWnd != null && fwWnd != sender && fwWnd.ActiveView != null && fwWnd is Form)
						{
							m_findReplaceDlg.SetOwner(fwWnd.Cache,
								fwWnd.ActiveView.CastAsIVwRootSite(), (fwWnd as Form).Handle,
								FindPattern(fwWnd.Cache));
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

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set to false to keep the App from closing even if there are no windows left open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OkToCloseApp
		{
			set
			{
				CheckDisposed();

				m_fOkToClose = value;
				if ((!m_fSuppressClose) && m_fOkToClose && m_caches.Count == 0)
					Application.Exit();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set to suppress app closing during backup or similar operation that will
		/// eventually re-open a window. Much the same as OkToCloseApp, but that may be
		/// set false and true again during window closing, resulting in exit if SuppressCloseApp
		/// has not been set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SuppressCloseApp
		{
			set
			{
				CheckDisposed();

				m_fSuppressClose = value;
				if ((!m_fSuppressClose) && m_fOkToClose && m_caches.Count == 0)
					Application.Exit();
			}
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
		/// Gets the active view of the active main window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private RootSite ActiveView
		{
			get
			{
				FwMainWnd mainWnd = Form.ActiveForm as FwMainWnd;
				RootSite rootSite = null;
				if (mainWnd != null)
					rootSite = mainWnd.ActiveView as RootSite;
				return rootSite;
			}
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
		/// Check to see if there is another instance of the same application running. All
		/// applications should call this method in their Main() method before creating a new
		/// instance of the App.
		/// </summary>
		///
		/// <returns>Existing process if application is already running, else null.</returns>
		/// -----------------------------------------------------------------------------------
		protected static Process ExistingProcess
		{
			get
			{
				Process thisProcess = Process.GetCurrentProcess();
				try
				{
					string thisSid = BasicUtils.GetUserForProcess(thisProcess);
					foreach (Process procCurr in Process.GetProcessesByName(thisProcess.ProcessName))
					{
						if (procCurr.Id != thisProcess.Id &&
							procCurr.MainModule.ModuleName.ToLower() ==
							thisProcess.MainModule.ModuleName.ToLower() &&
							thisSid == BasicUtils.GetUserForProcess(procCurr))
						{
							return procCurr;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Got exception in FwApp.ExisitingProcess: " + ex.Message);
				}
				return null;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Registry key for settings for this application. Individual applications will
		/// override this.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		virtual public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML help file (.chm) for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public abstract string HelpFile
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public abstract string SampleDatabase
		{
			get;
		}

		///***********************************************************************************
		/// <summary>
		/// App has no window creation option. This method is required as part of ISettings
		/// implementation.
		/// </summary>
		/// <value>By default, returns false</value>
		///***********************************************************************************
		[Browsable(false)]
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ICU Locale of the user interface writing system (from resources)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string UserWs
		{
			get
			{
				// If the writing system hasn't been set then set it. Normally, this should
				// get set in the FwApp constructor. But since this is a static property
				// that may not be true. When it is not, it usually means the property is being
				// accessed via a test.
				if (string.IsNullOrEmpty(s_sWsUser))
					s_sWsUser = "en";

				return s_sWsUser;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the measurement system used in the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static MsrSysType MeasurementSystem
		{
			get { return (MsrSysType)FwRegistrySettings.MeasurementUnitSetting; }
			set { FwRegistrySettings.MeasurementUnitSetting = (int)value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the setting for displaying error message boxes. The value is retrieved from
		/// the .config file.
		/// </summary>
		/// <remarks>
		/// To disable displaying an error message box, put
		/// <code>&lt;add key="ShowUI" value="False"/></code>
		/// in the &lt;appSettings> section of the .config file (see MSDN for details).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public static bool ShowUI
		{
			get
			{
				string strShowUI =
					System.Configuration.ConfigurationManager.AppSettings["ShowUI"];
				bool fShowUI = true;
				try
				{
					if (strShowUI != null)
						fShowUI = Convert.ToBoolean(strShowUI);
				}
				catch
				{
				}
				return fShowUI;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The date after which the program stops working.
		/// </summary>
		/// <remarks><p>Derived classes should override this property if they want to implement
		/// a drop dead date. The default value 1/1/3000 means works unlimited.</p>
		/// <p>If you override this property, you might also want to provide your own
		/// string for kstidDropDead in your derived class.</p></remarks>
		/// ------------------------------------------------------------------------------------
		public virtual DateTime DropDeadDate
		{
			get
			{
				CheckDisposed();
				return new DateTime(3000, 1, 1);
			}
		}
		#endregion

		#region FieldWorks Project Dialog handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the New Project dialog
		/// </summary>
		/// <param name="newWindowToAdjustFrom"></param>
		/// <returns>The result from FwNewLangProject dialog.</returns>
		/// ------------------------------------------------------------------------------------
		public DialogResult NewProjectDialog(Form newWindowToAdjustFrom)
		{
			bool fAssignedOkToCloseApp = newWindowToAdjustFrom != null;
			try
			{
				if (fAssignedOkToCloseApp)
					OkToCloseApp = false;
				DialogResult result;

				using (FwNewLangProject dlg = new FwNewLangProject())
				{
					dlg.SetDialogProperties(this);
					result = dlg.DisplayDialog(newWindowToAdjustFrom);

					// Make sure we don't pass in a window that isn't a main window.
					// This can happen from the Welcome to Fieldworks dialog.
					if (!(newWindowToAdjustFrom is IFwMainWnd))
						newWindowToAdjustFrom = null;

					if (result == DialogResult.OK)
					{
						if (dlg.IsProjectNew)
						{
							ReplaceCommandLine(new string[] { "-db", dlg.DatabaseName });
							Form wnd = NewMainWindow(null, true);
							if (wnd != null && newWindowToAdjustFrom != null)
								AdjustNewWindowPosition(wnd, newWindowToAdjustFrom);
						}
						else
						{
							// Only attempt to open the project if it isn't already open.
							if (dlg.Project != null && !dlg.Project.InUse)
							{
								try
								{
									string serverName = (newWindowToAdjustFrom is FwMainWnd ?
										((FwMainWnd)newWindowToAdjustFrom).Cache.ServerName :
										MiscUtils.LocalServerName);
									CreateProjectWindow(serverName, dlg.Project.DatabaseName);
								}
								catch (ApplicationException e)
								{
									ErrorReporter.ReportException(e, newWindowToAdjustFrom, false);
								}
							}
						}
					}
					else if (result == DialogResult.Abort)
					{
						// If we get an Abort it means that we got an exception in the dialog (e.g.
						// in the OnLoad method). We can't just catch that exception here (probably
						// because of the extra message loop the dialog has), so we close the dialog
						// and return Abort.
						MessageBox.Show(newWindowToAdjustFrom,
							GetResourceString("kstidNewProjError"),
							GetResourceString("kstidMiscError"));
					}
				}
				return result;
			}
			finally
			{
				if (fAssignedOkToCloseApp)
					OkToCloseApp = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the New Project dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected DialogResult DeleteProjectDialog(Form newWindowToAdjustFrom)
		{
			using (FwDeleteProjectDlg dlg = new FwDeleteProjectDlg())
			{
				dlg.SetDialogProperties(this);
				return dlg.ShowDialog();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="newWindowToAdjustFrom"></param>
		/// <param name="serverName"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public DialogResult OpenProjectDialog(Form newWindowToAdjustFrom, string serverName,
			ILgWritingSystemFactory wsf)
		{
			CheckDisposed();

			bool haveProject, haveSubitem;
			IOpenFWProjectDlg dlg = OpenFWProjectDlgClass.Create();
			dlg.WritingSystemFactory = wsf;

			uint handle = newWindowToAdjustFrom == null ? 0 :
				(uint)newWindowToAdjustFrom.Handle.ToInt32();
			string helpString = HelpFile + "::/" + GetHelpString("khtpOpenProject", 0);
			dlg.Show(Logger.Stream, serverName, MiscUtils.LocalServerName, UserWs,
				handle, false, 0, helpString);

			int hvoProj, hvoSubitem;
			string project, database, server, name;
			Guid guid;

			dlg.GetResults(out haveProject, out hvoProj, out project, out database,
				out server, out guid, out haveSubitem, out hvoSubitem, out name);
			if (haveProject)
			{
				Form wnd = null;

				// Make sure we don't pass in a window that isn't a main window.
				// This can happen from the Welcome to Fieldworks dialog.
				if (!(newWindowToAdjustFrom is IFwMainWnd))
					newWindowToAdjustFrom = null;
				try
				{
					wnd = CreateProjectWindow(server, database);
				}
				catch (ApplicationException e)
				{
					ErrorReporter.ReportException(e, newWindowToAdjustFrom, false);
				}

				if (wnd != null && newWindowToAdjustFrom != null)
					AdjustNewWindowPosition(wnd, newWindowToAdjustFrom);
				else if (wnd == null && ActiveMainWindow == null)
					haveProject = false;
			}
			return haveProject ? DialogResult.OK : DialogResult.Cancel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a project window with the specified project.
		/// </summary>
		/// <param name="server">server where project is located</param>
		/// <param name="database">file containing project</param>
		/// <remarks>The calling method should handle the exception thrown if unable to open
		/// the project and/or create a new window.</remarks>
		/// <returns>window which is created (or null if unable to create the window)</returns>
		/// ------------------------------------------------------------------------------------
		private Form CreateProjectWindow(string server, string database)
		{
			CheckDisposed();

			// Note: Use ToLowerInvariant because lowercase of SILFW in Turkish is not silfw.
			if (!server.ToLowerInvariant().EndsWith("\\silfw"))
				server += "\\silfw";

			if (!CheckDbVerCompatibility(server, database))
				return null;

			ReplaceCommandLine(new string[]{"-db", database, "-c", server});
			return NewMainWindow(null, false);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close all windows and shut down the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void ExitAppplication()
		{
			if (m_rgMainWindows != null)
			{
				// Make sure we use a copy of the main windows list since closing a main window
				// will remove it out of the list. (TE-8574)
				List<IFwMainWnd> mainWindows = new List<IFwMainWnd>(m_rgMainWindows);
				foreach (IFwMainWnd mainWnd in mainWindows)
				{
					if (mainWnd is Form && !((Form)mainWnd).IsDisposed)
						mainWnd.Close();
				}
				m_rgMainWindows.Clear();
			}
		}

		#region Methods for dealing with main windows
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application's find pattern for the find/replace dialog. (If one does not
		/// already exist, a new one is created.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwPattern FindPattern(FdoCache cache)
		{
			CheckDisposed();

			if (!m_findPatterns.ContainsKey(cache))
				m_findPatterns.Add(cache, VwPatternClass.Create());

			Debug.Assert(m_findPatterns[cache] != null);

			return m_findPatterns[cache];
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
		/// Removes the specified IFwMainWnd from the list of windows and removes the cache
		/// associated with it.
		/// </summary>
		/// <param name="fwMainWindow">The IFwMainWnd to remove</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveWindow(IFwMainWnd fwMainWindow)
		{
			CheckDisposed();

			FdoCache wndCache = null;
			RemoveWindow(fwMainWindow, out wndCache);
			if (wndCache != null)
				RemoveFdoCache(wndCache);
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
		/// Removes the specified IFwMainWnd from the list of windows, returning the cache
		/// associated with it if the cache should be removed separately.  There are situations
		/// where the cache must not be removed at the time the window is removed, but where
		/// the code that uses the cache invalidates fwMainWindow.m_mediator, which stores the
		/// access to the cache.
		/// </summary>
		/// <param name="fwMainWindow">The IFwMainWnd to remove</param>
		/// <param name="wndCache">returns null or FdoCache object to pass to RemoveFdoCache()
		/// later.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveWindow(IFwMainWnd fwMainWindow, out FdoCache wndCache)
		{
			CheckDisposed();

			wndCache = null;
			Form form = (Form)fwMainWindow;
			if (!m_rgMainWindows.Contains(fwMainWindow) || form.Disposing || form.IsDisposed)
			{
				return; // It isn't our window. Or it is dead or dying.
			}

			FdoCache mainWndCache = fwMainWindow.Cache;
			bool fCacheStillInUse = false;

			// Look for the cache in the other main windows.
			foreach (IFwMainWnd mainWnd in MainWindows)
			{
				if (mainWnd != null && mainWnd != fwMainWindow)
					fCacheStillInUse |= (mainWnd.Cache == mainWndCache);
			}
			if (!fCacheStillInUse)
				wndCache = mainWndCache;

			m_rgMainWindows.Remove(fwMainWindow);
			if (m_fOkToClose && m_rgMainWindows.Count == 0)
			{
				wndCache = null;
				RemoveFdoCache(mainWndCache);
			}
			if (m_activeMainWindow == fwMainWindow)
				m_activeMainWindow = null;
			Form oldForm = fwMainWindow as Form;
			if (oldForm != null)
			{
				// JohnT: being paranoid here, surely it MUST be a form?
				// Assuming that's so, we no longer want to know about it in the unlikely
				// event of its being activated again, and hence don't need to know about
				// its handle being destroyed.
				oldForm.Activated -= new EventHandler(fwMainWindow_Activated);
				oldForm.HandleDestroyed -= new EventHandler(fwMainWindow_HandleDestroyed);
			}
		}
		#endregion

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Produces a case-insensitive key (by converting everything to lower) for
		/// accessing the hash of caches.
		/// </summary>
		/// <param name="sServer"></param>
		/// <param name="sDbName"></param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		protected string MakeKey(string sServer, string sDbName)
		{
			return String.Format("{0}:{1}", sServer.ToLowerInvariant(), sDbName.ToLowerInvariant());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified FdoCache cleanly, saving it first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveFdoCache(FdoCache wndCache)
		{
			CheckDisposed();

			// To be safe - this method might get called recursively (explicitly and from
			// Application.Exit() below again).
			if (wndCache.IsDisposed)
				return;

			Debug.Assert(wndCache != null);
			wndCache.Save();
			DataUpdateMonitor.RemoveDataAccess(wndCache.MainCacheAccessor);
			m_caches.Remove(MakeKey(wndCache.ServerName, wndCache.DatabaseName));
			wndCache.Dispose();

			// If the last cache was removed, then exit the application
			if ((!m_fSuppressClose) && m_fOkToClose && m_caches.Count == 0)
			{
				EditingHelper.ClearTsStringClipboard();
				Application.Exit();
			}
		}

		#region Command-line processing methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows help for command line options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShowCommandLineHelp()
		{
			MessageBox.Show(CommandLineHelpText, Application.ProductName, MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the command line help text.
		/// </summary>
		/// <value>The command line help text.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual string CommandLineHelpText
		{
			get
			{
				string exeName = Path.GetFileName(Assembly.GetEntryAssembly().CodeBase);
				string message = string.Format("{0}, Version {1}\n\nUsage: {2} [options]\n\n" +
					"Options:\n-c <computer>\tComputer name (aka the SQL server)\n" +
					"-db <database>\tDatabase name\n" +
					"-locale <culture>\tCulture abbreviation\n" +
					"-help\t\t(also -? and -h) Command line usage help",
					Application.ProductName, Application.ProductVersion, exeName);
				return message;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Parse the command line, and return the results in a hashtable. The hash key is the
		/// option tag. The hashtable value is an array that holds the parameter values.
		/// The array may be empty for cases where the option tag is all there is (e.g.,
		/// -Embedding). "filename" is a reserved key (i.e., it need not be explicitly
		/// supplied on the command line) for the case where there is no switch, but there is
		/// an argument.
		///
		/// Current set of options:
		/// -c    ComputerName	(aka the SQL server)
		/// -db DatabaseName
		/// -embedding Start up as an OLE automation server
		/// -automation Start up as an OLE automation server (future)
		/// -install Install ???
		/// -uninstall Uninstall ???
		/// -pt Print to
		/// -p Print
		/// -help (also -? and -h Command line usage help
		/// -filename (internal for the 'switchless' filename or major object)
		/// -link URL (as composed at some point by FwLink)
		/// -locale CultureAbbr
		/// </summary>
		///
		/// <param name="rgArgs">Array of strings containing the command-line arguments. In
		/// general, the command-line will be parsed into whitespace-separated tokens, but
		/// double-quotes (") can be used to create a multiple-word token that will appear in
		/// the array as a single argument. One argument in this array can represent either a
		/// key, a value, or both (since whitespace is not required between keys and values.
		/// </param>
		///
		/// <exception cref="ArgumentException">Incorrectly formed command line. Caller should
		/// alert user to correct command-line structure.
		/// </exception>
		///	-----------------------------------------------------------------------------------
		protected Dictionary<string, List<String>> ParseCommandLine(string[] rgArgs)
		{
			Dictionary<string, List<String>> dictArgs = new Dictionary<string, List<String>>();
			if (rgArgs == null || rgArgs.Length == 0)
				return dictArgs;

			string sKey = "";
			List<String> values = new List<String>();

			foreach (string sArg in rgArgs)
			{
				int iCurrChar = 0;
				if (sArg[iCurrChar] ==  '-' || sArg[iCurrChar] ==  '/') // Start of option
				{
					// It turns out that -db"Lela-Teli Sample" and "-dbLela-Teli Sample"
					// come through looking identical. I don't think we really need to
					// throw an exception anywa, since either way, the intent is obvious,
					// so why consider it illegal.
					//					if (sArg.IndexOf(' ') > 0)
					//					{
					//						// The quote apparently surrounds the entire thing, as in:
					//						//	"-n Parts Of Speech"
					//						throw new ArgumentException();
					//					}
					if (values.Count > 0)
					{
						dictArgs.Add(sKey, values);
						sKey = "";
						values = new List<String>();
					}
					else if (sKey.Length > 0)
					{
						// Found a tag in the previous pass, but it has no argument, so save it in
						// the map with a value of an empty vector, before processing current tag.
						dictArgs.Add(sKey, null);
						sKey = "";
					}
					++iCurrChar; // Increment counter

					// The user may have just put an argument right next to the marker,
					// so we need to split the tag from the argument at this point.
					sKey = CommandLineSwitch(sArg, ref iCurrChar);
				}
				if (iCurrChar < sArg.Length)
				{
					// Now process argument(s) to option (there can be several) or argument that
					// does not have an explicit option key.
					if ((sKey == "filename") && (values.Count > 0))
						throw new ArgumentException(); // Second argument not allowed here.
					values.Add(sArg.Substring(iCurrChar));
					// There may not be a key, in case this is the first argument, as in Worldpad
					// wanting to open a file.
					if (sKey.Length == 0)
						sKey = "filename";
				}
			}
			// Save final tag.
			if (sKey.Length > 0)
			{
				dictArgs.Add(sKey, values);
			}

			return dictArgs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace the command line with new parameters
		/// </summary>
		/// <param name="newCommandLine"></param>
		/// ------------------------------------------------------------------------------------
		public void ReplaceCommandLine(string[] newCommandLine)
		{
			CheckDisposed();

			m_commandLineArgs = ParseCommandLine(newCommandLine);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Checks given string argument, starting at position iCurrChar for one of the
		/// standard approved multi-character command line switches. If not found, it assumes
		/// this is a single-character switch. Specific applications can override or extend
		/// this functionality if additional or different multi-character switches are needed.
		/// </summary>
		///
		/// <param name='sArg'>Command-line argument being processed</param>
		/// <param name='iCurrChar'>Zero-based index indicating first character in sArg to be
		/// considered when looking for switch. Typically, the initial value will be 1, since
		/// character 0 will usually be a - or /. This parameter is returned to the caller
		/// incremented by the number of characters in the switch.</param>
		/// -----------------------------------------------------------------------------------
		virtual protected string CommandLineSwitch(string sArg, ref int iCurrChar)
		{
			if (String.Compare(sArg, iCurrChar, "pt", 0, 2, true) == 0)
			{
				iCurrChar += 2;
				return "pt";
			}
			else if (String.Compare(sArg, iCurrChar, "automation", 0, 10, true) == 0)
			{
				iCurrChar += 10;
				return "automation";
			}
			else if (String.Compare(sArg, iCurrChar, "embedding", 0, 9, true) == 0)
			{
				iCurrChar += 9;
				return "embedding";
			}
			else if (String.Compare(sArg, iCurrChar, "uninstall", 0, 9, true) == 0)
			{
				iCurrChar += 9;
				return "uninstall";
			}
			else if (String.Compare(sArg, iCurrChar, "install", 0, 7, true) == 0)
			{
				iCurrChar += 7;
				return "install";
			}
			else if (String.Compare(sArg, iCurrChar, "help", 0, 4, true) == 0)
			{
				iCurrChar += 4;
				return "help";
			}
			else if (String.Compare(sArg, iCurrChar, "db", 0, 2, true) == 0)
			{
				iCurrChar += 2;
				return "db";
			}
			else if (String.Compare(sArg, iCurrChar, "link", 0, 4, true) == 0)
			{
				iCurrChar += 4;
				return "link";
			}
			else if (String.Compare(sArg, iCurrChar, "locale", 0, 6, true) == 0)
			{
				iCurrChar += 6;
				return "locale";
			}
			else if (sArg.Length > iCurrChar)
			{
				// It is a single character tag.
				string sKey = sArg.Substring(iCurrChar, 1).ToLowerInvariant();
				if (sKey == "?" || sKey == "h")	// Variants of help.
					sKey = "help";
				++iCurrChar;
				return sKey;
			}
			else
			{
				throw new ArgumentException();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the server and name to be used for establishing a database connection. First
		/// try to get this info based on the command-line arguments. If not given, try using
		/// the info in the registry. Otherwise, just get the first FW database on the local
		/// server.
		/// </summary>
		/// <returns>A new FdoCache, based on options, or null, if not found.</returns>
		/// -----------------------------------------------------------------------------------
		private FdoCache GetCache()
		{
			Debug.Assert(m_commandLineArgs != null);

			// REVIEW JohnW: If user supplies multiple servers and/or databases, what should we
			// do? It seems like we should open up multiple instances of the tool, one for each
			// DB. What if the user supplies two different servers and three different DB's or
			// something crazy like that?
			// Answer from RandyR: The specs essentially say that if there are multiple
			// instances of a switch, then the last one "wins",
			// (e.g., the others are discarded). This would probably hold for multiple
			// arguments within one switch. The only exception is for the switchless argument
			// (filename). In this case the application is supposed to be Worldpad,
			// and not a normal FW app.

			// Get a connection information from one of three places,
			// in this order of preference:
			// 1. command line options
			// 2. Registry
			// 3. Default values.
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			string sCurrentAttempt;
			// If there are any command line args, use that information.
			// Otherwise, try the Registry.
			string sNextAttempt = (m_commandLineArgs.Count == 0) ? "Registry" : "CommandLine";
			string sServer = null;
			string sDbName = null;
			string sRootObjName = null;
			while (cacheOptions.Count == 0)
			{
				sCurrentAttempt = sNextAttempt;
				switch (sCurrentAttempt)
				{
					case "CommandLine":
					{
						List<string> arg = null;
						if (m_commandLineArgs.ContainsKey("c"))
							arg = m_commandLineArgs["c"];
						sServer = HasValue(arg) ? arg[0] : null;

						if (m_commandLineArgs.ContainsKey("db"))
							arg = m_commandLineArgs["db"];
						else
							arg = null;
						sDbName = HasValue(arg) ? arg[0] : null;

						if (m_commandLineArgs.ContainsKey("filename"))
							arg = m_commandLineArgs["filename"];
						else
							arg = null;
						sRootObjName = HasValue(arg) ? arg[0] : null;

						sNextAttempt = "Registry";
						break;
					}
					case "Registry":
					{
						sServer = (string)SettingsKey.GetValue("LatestDatabaseServer");
						sDbName = (string)SettingsKey.GetValue("LatestDatabaseName");
						sRootObjName = (string)SettingsKey.GetValue("LatestRootObjectName");
						sNextAttempt = "Default";
						break;
					}
					default:
					{
						sServer = MiscUtils.LocalServerName;
						sDbName = null;
						sRootObjName = null;
						break;
					}
				}
				// Try to fill a default root object name. It needs to be done here,
				// rather than within the FdoCache's Create method, since
				// the cache has no way to get to the default for this item,
				// as it has for all other parameters.
				if (sRootObjName == null)
					sRootObjName = GetDefaultRootObjectName();

				// Populate htCacheOptions with whatever we have, if anything.
				if (sServer != null)
					cacheOptions.Add("c", sServer);
				if (sDbName != null)
					cacheOptions.Add("db", sDbName);
				if (sRootObjName != null)
					cacheOptions.Add("filename", sRootObjName);
			}

			// Make sure that the database server is running.
			sServer = FdoCache.InitMSDE(sServer);
			FdoCache cache = null;
			// Try to find extant cache.
			if (sDbName == null)
				sDbName = ""; // Don't pass null (e.g., no dbname found) to MakeKey or it will throw.
			if (sServer != null && sDbName != null)
			{
				string key = MakeKey(sServer, sDbName);
				if (m_caches.ContainsKey(key))
					cache = m_caches[key];
			}
			if (cache == null)	// None found, so try to make a new one.
			{
				if (CheckDbVerCompatibility(sServer, sDbName))
				{
					GetCache(sServer, sDbName, out cache);
				}
			}
			return cache;	// After all of this, it may be still be null, so watch out.
		}

		private bool HasValue(List<string> args)
		{
			return args != null
				&& args.Count > 0
				&& args[0].Length > 0;
		}

		/// <summary>
		/// Provides a hook for initializing a cache in special ways. For example,
		/// LexTextApp sets up a CreateModifyTimeManager.
		/// </summary>
		/// <param name="cache"></param>
		protected virtual void InitCache(FdoCache cache)
		{
			// Set the default user ws if we know one.
			// This is especially important because (as of 12 Feb 2008) we are not localizing the resource string
			// in the Language DLL which controls the default UI language.
			if (!string.IsNullOrEmpty(s_sWsUser))
				cache.LanguageWritingSystemFactoryAccessor.UserWs = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(s_sWsUser);
		}

		/// <summary>
		/// Return a cache for the specified server and database, along with a boolean indicating
		/// whether it is newly created or already existed.
		/// </summary>
		/// <param name="sServer"></param>
		/// <param name="sDatabase"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		protected bool GetCache(string sServer, string sDatabase, out FdoCache cache)
		{
			bool isNewCache = false;
			string key = MakeKey(sServer, sDatabase);
			if (m_caches.ContainsKey(key))
			{
				// Use extant one.
				cache = m_caches[key];
			}
			else
			{
				// Make a new cache and note it in the hash.
				cache = CreateAndCheckCache(sServer, sDatabase);
				InitCache(cache);
				isNewCache = true;
			}
			SetCacheProgressBar(cache);
			return isNewCache;
		}

		private FdoCache CreateAndCheckCache(string sServer, string sDatabase)
		{
			FdoCache cache;
			cache = FdoCache.Create(sServer, sDatabase, null);
			if (cache != null)
			{
				CheckCache(cache);
				m_caches[MakeKey(sServer, sDatabase)] = cache;
			}
			return cache;
		}

		/// <summary>
		/// Provides a hook for issuing warnings to the user when opening a database.
		/// </summary>
		protected virtual void CheckCache(FdoCache cache)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the given cache is already being used.
		/// Use the one that we have, if available,
		/// which will reset the 'cache' parameter.
		/// Deprecated--(JohnT)...this approach involves creating and destroying a cache
		/// unnecessarily and this seems to cause problems (see LT-7176). Better to use
		/// the overload that takes a server and database name.
		/// </summary>
		/// <param name="cache">Reference to an FdoCache.</param>
		/// <returns>True if the cache is not already being used, otherwise false.</returns>
		/// -----------------------------------------------------------------------------------
		protected bool GetCache(ref FdoCache cache)
		{
			Debug.Assert(cache != null);

			bool isNewCache = false;
			string key = MakeKey(cache.ServerName, cache.DatabaseName);
			if (m_caches.ContainsKey(key))
			{
				// Use extant one.
				cache.Dispose();
				cache = m_caches[key];
			}
			else
			{
				// Cache the new cache in the hash.
				m_caches[key] = cache;
				isNewCache = true;
			}
			SetCacheProgressBar(cache);
			return isNewCache;
		}

		private void SetCacheProgressBar(FdoCache cache)
		{
			if (cache != null && m_SplashScreenWnd != null && CacheCanControlSplashScreenProgress)
			{
				// set progress dialog so that cache can report data loading progress
				cache.ProgressBar = m_SplashScreenWnd.ProgressBar;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clear all data in all caches.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public void ClearAllCaches()
		{
			CheckDisposed();

			foreach (FdoCache cache in m_caches.Values)
				cache.ClearAllData();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clear all data in all caches that match the input parameters.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public void ClearAllCaches(string sDatabase, string sServer)
		{
			CheckDisposed();

			if (sDatabase == null)
			{
				ClearAllCaches();
			}
			else
			{
				foreach (FdoCache cache in m_caches.Values)
				{
					if (cache.DatabaseName.ToLowerInvariant() == sDatabase &&
						cache.ServerName.ToLowerInvariant() == sServer)
					{
						cache.ClearAllData();
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the default root object.
		/// This implementation returns null, but an application subclass
		/// should override it to return a name (e.g., CLE would return
		/// a default list to edit).
		/// </summary>
		/// <returns>
		/// The name of the default root object, or null,
		/// if no meaningful name exists for an application.
		/// </returns>
		/// -----------------------------------------------------------------------------------
		protected virtual string GetDefaultRootObjectName()
		{
			return null;
		}
		#endregion

		#region Static methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Reference to the one and only application object (static).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		static public FwApp App
		{
			get {return s_app;}
			set
			{
				// Setting this via the property setter should only be done in the tests.
				Debug.Assert(value != null);
				s_app = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates a TsString from a resource ID.
		/// </summary>
		/// <param name='stid'>String resource id.</param>
		/// <param name='ws'>writing system id.</param>
		/// <returns>TsString</returns>
		/// -----------------------------------------------------------------------------------
		static public ITsString GetResourceTss(string stid, int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeString(ResourceHelper.GetResourceString(stid), ws);
			return tss;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public static string GetResourceString(string stid)
		{
			string str = string.Empty;

			if (App != null)
				str = ((IApp)App).ResourceString(stid);

			if (str == null || str == string.Empty)
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
		///
		/// <param name="cache">Instance of the FW Data Objects cache that the new main window
		/// will use for accessing the database.</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>New instance of main window if successfull; otherwise <c>null</c></returns>
		/// -----------------------------------------------------------------------------------
		protected abstract Form NewMainAppWnd(FdoCache cache, bool fNewCache, Form wndCopyFrom,
			bool fOpeningNewProject);

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Method to reopen after a Database Restore or other major database update.
		/// </summary>
		///
		/// <param name='sSvrName'>Name of the server hosting the database.</param>
		/// <param name='sDbName'>Name of the database to open.</param>
		/// <returns>Created form</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual Form ReopenDbAndOneWindow( string sSvrName, string sDbName)
		{
			// get the cache
			bool fNewCache = false;
			string key = MakeKey(sSvrName, sDbName);
			if (!m_caches.ContainsKey(key))
			{
				fNewCache = true;
				FdoCache cache = CreateAndCheckCache(sSvrName, sDbName);
				if (cache == null || cache.LangProject == null)
					throw new ApplicationException(FrameworkStrings.CouldNotReEstablishConnectionToDatabase);
				m_caches[key] = cache;
				InitCache(cache);

				// setup the cache to send sync messages to other apps
				cache.MakeDbSyncRecords(SyncGuid);
			}

			Form newWnd = CreateNewMainWindow(m_caches[key], fNewCache, null, false);
			// now close any dummy windows that had been installed to keep the app from exiting.
			foreach (IFwMainWnd mainWnd in MainWindows.ToArray())
			{
				if (mainWnd is FwDummyWnd && mainWnd != newWnd)
					mainWnd.Close();
			}
			return newWnd;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close any windows associated with a database, save the database, clear all caches,
		/// and shutdown the connection to the database.
		/// </summary>
		///
		/// <param name="sServer">Server of the database to close</param>
		/// <param name="sDbName">Name of the database to close</param>
		/// <param name='fOkToClose'>True to close the application if there are no further
		/// connections after the requested connection is closed. False leaves the application
		/// open.</param>
		///
		/// <returns>True if any windows were closed.</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool CloseDbAndWindows(string sServer, string sDbName,
			bool fOkToClose)
		{
			string key = MakeKey(sServer, sDbName);
			if (!m_caches.ContainsKey(key))
				return false; // Requested DB is not open.
			FdoCache cache = m_caches[key];
			m_fOkToClose = fOkToClose;
			bool fClosedWindow = false;

			// Close any windows associated with this database.
			// NOTE: This has to go backwards, because the window will remove itself from this
			// vector when it handles the close message.
			for (int i = m_rgMainWindows.Count - 1; i >= 0; i--)
			{
				IFwMainWnd fwMainWindow = m_rgMainWindows[i];
				if (fwMainWindow.Cache == cache)
				{
					if (!fOkToClose && m_rgMainWindows.Count == 1)
					{
						// create a dummy window so that the system doesn't try to exit the app.
						// when all the other main windows close.
						Form dummyWnd = AddDummyMainWnd(null, null);
						dummyWnd.Update();
					}
					// Saving of DB happens as a side-effect when last window closes. See
					// FwApp.RemoveWindow to see this code.
					if (fwMainWindow is FwMainWnd)
						(fwMainWindow as FwMainWnd).ReallyClose = true;	// See TE-7713.
					fwMainWindow.Close();
					fClosedWindow = true;
				}
			}

			if (!fOkToClose && m_rgMainWindows.Count == 1)
			{
				Form frm = (Form)m_rgMainWindows[0];
				if (frm != null && !(frm is FwMainWnd) && !frm.Visible)
				{
					frm.Visible = true;
					frm.Update();
				}
			}
			m_fOkToClose = true;
			return fClosedWindow;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Install a new dummy main window when we need to prevent the app from exiting during
		/// a radical database operation (e.g. backup / restore).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		/// <param name="title">title for the window. null to use default</param>
		/// <param name="message">message for the window. null to use default</param>
		public Form AddDummyMainWnd(string title, string message)
		{
			CheckDisposed();

			FwDummyWnd dummy = new FwDummyWnd(title, message);
			FwApp.App.MainWindows.Add(dummy);
			return dummy;
		}

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

			RefreshAllViews(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in all of the Main Windows of the app.
		/// </summary>
		/// <param name="cache">cache that has been updated. Only need to refresh views
		/// that have the same cache.</param>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews(FdoCache cache)
		{
			CheckDisposed();

			if (cache != null && m_refreshViewCaches.ContainsKey(cache))
			{
				m_refreshViewCaches[cache] = true;
			}
			else
			{
				foreach (IFwMainWnd wnd in MainWindows)
				{
					if (wnd.Cache == cache || cache == null)
						wnd.RefreshAllViews();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Usually <c>false</c>, but can be set to <c>true</c> while running unit tests
		/// </summary>
		/// <returns><c>true</c> if running tests, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool RunningTests()
		{
			// If the real application is ever installed in a path that includes nunit, then
			// this will return true and the app. won't run properly. But what are the chances
			// of that?...
			return (Application.ExecutablePath.ToLowerInvariant().IndexOf("nunit") != -1);
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
		///
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		/// -----------------------------------------------------------------------------------
		public void EnableMainWindows (bool fEnable)
		{
			CheckDisposed();

			if (!fEnable)
				m_nEnableLevel--;
			else if (++m_nEnableLevel != 0)
				return;

			foreach (IFwMainWnd fwMainWnd in MainWindows)
				fwMainWnd.EnableWindow(fEnable);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables or disables main windows associated with the same project (cache).
		/// </summary>
		/// <param name="cache">project cache</param>
		/// <param name="fEnableWindows">enable or disable windows</param>
		/// ------------------------------------------------------------------------------------
		public void EnableSameProjectWindows(FdoCache cache, bool fEnableWindows)
		{
			CheckDisposed();

			// TE-1913: Prevent user from accessing windows that are open to the same project.
			// Originally this was used for importing.
			foreach (Form wnd in MainWindows)
			{
				if (((IFwMainWnd)wnd).Cache == cache)
					wnd.Enabled = fEnableWindows;
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
		/// Close and remove the Find/Replace modeless dialog
		/// (result of LT-5702)
		/// </summary>
		/// <returns>true</returns>
		/// ------------------------------------------------------------------------------------
		public bool RemoveFindReplaceDialog()
		{
			if (m_findReplaceDlg != null)
			{
				// Closing doesn't work as it tries to hide the dlg ..
				// so go for the .. dispose.  It will do it 'base.Dispose()'!
				m_findReplaceDlg.Close();
				m_findReplaceDlg.Dispose();
				m_findReplaceDlg = null;
			}
			return true;
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

			if (m_findReplaceDlg.SetDialogValues(rootsite.Cache, FindPattern(rootsite.Cache),
				rootsite, fReplace, fOverlay, UserWs, rootsite.ParentForm.Handle, this, this))
			{
				m_findReplaceDlg.Show();
				return true;
			}
			return false;
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
		/// Returns the help urls for each tab on the styles dialog (currently 5 tabs)
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string[] GetStyleDlgHelpUrls()
		{
			CheckDisposed();

			return new string[] {"", "", "", "", ""};
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Catches and displays otherwise unhandled exception, especially those that happen
		/// during startup of the application before we show our main window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception)
				DisplayError(e.ExceptionObject as Exception, e.IsTerminating);
			else
				DisplayError(new ApplicationException(string.Format("Got unknown exception: {0}",
					e.ExceptionObject)), false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Catches and displays a otherwise unhandled exception.
		/// </summary>
		/// <param name="sender">sender</param>
		/// <param name="eventArgs">Exception</param>
		/// <remarks>previously <c>AfApp::HandleTopLevelError</c></remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void HandleTopLevelError(object sender, ThreadExceptionEventArgs eventArgs)
		{
			if (BasicUtils.IsUnsupportedCultureException(eventArgs.Exception)) // LT-8248
			{
				Logger.WriteEvent("Unsupported culture: " + eventArgs.Exception.Message);
				return;
			}

			if (DisplayError(eventArgs.Exception, false))
			{
				Application.Exit();

				// just to be sure
				Thread.Sleep(5000); // 5s
				Process.GetCurrentProcess().Kill();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the error message of the exception to the user.
		/// </summary>
		/// <param name="exception"></param>
		/// <param name="fTerminating"><c>true</c> if the application already knows that it is
		/// terminating, otherwise <c>false</c>.</param>
		/// <returns><c>true</c> to exit application, <c>false</c> to continue</returns>
		/// ------------------------------------------------------------------------------------
		protected bool DisplayError(Exception exception, bool fTerminating)
		{
			if (m_HelperControl.InvokeRequired)
			{
				// We got called from a different thread, maybe the Finalizer thread. Anyways,
				// it's never ok to exit the app in this case so we return fTerminating.
				if (fTerminating)
				{
					m_HelperControl.Invoke(new DisplayErrorMethod(DisplayError), exception,
						fTerminating);
				}
				else
					m_HelperControl.BeginInvoke(new DisplayErrorMethod(DisplayError), exception);
				return fTerminating;
			}
			else
			{
				Form form = (m_rgMainWindows.Count > 0 ? m_rgMainWindows[0] as Form : null);
				return DisplayError(exception, form);
			}
		}

		/// <summary>
		/// Report an exception 'safely'. That is, minimise the chance that some exception is
		/// going to be thrown during the report, which will throw us right out of the program
		/// without the chance to copy information about the original error.
		/// One way we do this is to stop all the mediators we can find from processing messages.
		/// (For example: some crashes
		/// </summary>
		/// <param name="error"></param>
		/// <param name="parent"></param>
		/// <param name="isLethal"></param>
		private static void SafelyReportException(Exception error, Form parent, bool isLethal)
		{
			FwApp instance = FwApp.App;
			if (instance == null)
			{
				ErrorReporter.ReportException(error, parent, isLethal); // all we can do
				return;
			}

			Dictionary<IFwMainWnd, bool> oldPrsMsgsVals = new Dictionary<IFwMainWnd, bool>();
			try
			{
				foreach (IFwMainWnd mainWnd in instance.m_rgMainWindows)
				{
					if (mainWnd is FwMainWnd)
					{
						oldPrsMsgsVals[mainWnd] = ((FwMainWnd)mainWnd).Mediator.ProcessMessages;
						((FwMainWnd)mainWnd).Mediator.ProcessMessages = false;
					}
				}

				ErrorReporter.ReportException(error, parent, isLethal);
			}
			finally
			{
				foreach (IFwMainWnd mainWnd in instance.m_rgMainWindows)
				{
					if (mainWnd is FwMainWnd)
						((FwMainWnd)mainWnd).Mediator.ProcessMessages = oldPrsMsgsVals[mainWnd];
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the error.
		/// </summary>
		/// <param name="exception">The exception.</param>
		/// <param name="parent">The parent parent.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected static bool DisplayError(Exception exception, Form parent)
		{
			try
			{
				// To disable displaying a message box, put
				// <add key="ShowUI" value="False"/>
				// in the <appSettings> section of the .config file (see MSDN for details).
				bool fShowUI = ShowUI;

				if (exception is ExternalException
					&& (uint)(((ExternalException)exception).ErrorCode) == 0x8007000E) // E_OUTOFMEMORY
				{
					if (fShowUI)
						SafelyReportException(exception, parent, true);
					else
					{
						Trace.Assert(false, FwApp.GetResourceString("kstidMiscError"),
							FwApp.GetResourceString("kstidOutOfMemory"));
					}
				}
				else
				{
					Debug.Assert(exception.Message != string.Empty || exception is COMException,
						"Oops - we got an empty exception description. Change the code to handle that!");

					if (fShowUI)
					{
						bool fIsLethal = !(exception is SIL.Utils.ConfigurationException ||
							exception is ContinuableErrorException ||
							exception.InnerException is ContinuableErrorException);
						SafelyReportException(exception, parent, fIsLethal);
						return false;
					}
					else
					{
						Exception innerE = ExceptionHelper.GetInnerMostException(exception);
						string strMessage = FwApp.GetResourceString("kstidProgError")
							+ FwApp.GetResourceString("kstidFatalError");
						string strVersion;
						Assembly assembly = Assembly.GetEntryAssembly();
						object[] attributes = null;
						if (assembly != null)
							attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
						if (attributes != null && attributes.Length > 0)
							strVersion = ((AssemblyFileVersionAttribute)attributes[0]).Version;
						else
							strVersion = Application.ProductVersion;

						string strReport = string.Format(FwApp.GetResourceString("kstidGotException"),
							FwApp.GetResourceString("kstidSupportEmail"), exception.Source, strVersion,
							ExceptionHelper.GetAllExceptionMessages(exception), innerE.Source,
							innerE.TargetSite.Name, ExceptionHelper.GetAllStackTraces(exception));
						Trace.Assert(false, strMessage, strReport);
					}
				}
			}
			catch
			{
				// we ignore any exceptions that might happen during reporting this error
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suppress execution of all synchronize messages and store them in a queue instead.
		/// </summary>
		/// <param name="cache">The cache whose synchronization messages we should ignore
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void SuppressSynchronize(FdoCache cache)
		{
			CheckDisposed();

			if (m_suppressedCaches.ContainsKey(cache))
				m_suppressedCaches[cache].Count++;
			else
				m_suppressedCaches.Add(cache, new SuppressedCacheInfo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resume execution of synchronize messages. If there are any messages in the queue
		/// execute them now.
		/// </summary>
		/// <param name="cache">The cache whose synchronization messages that should no longer
		/// be ignored</param>
		/// ------------------------------------------------------------------------------------
		public void ResumeSynchronize(FdoCache cache)
		{
			CheckDisposed();

			if (!m_suppressedCaches.ContainsKey(cache))
				return;

			m_suppressedCaches[cache].Count--;
			if (m_suppressedCaches[cache].Count > 0)
				return;

			BeginUpdate(cache);
			Queue<SyncInfo> messages = m_suppressedCaches[cache].Queue;
			m_suppressedCaches.Remove(cache);

			bool fProcessUndoRedoAfter = false;
			SyncInfo savedUndoRedo = new SyncInfo(SyncMsg.ksyncNothing, 0, 0); //struct, not an obj; can't be a null
			foreach (SyncInfo synchInfo in messages)
			{
				if (synchInfo.msg == SyncMsg.ksyncUndoRedo)
				{
					// we must process this synch message after all the others
					fProcessUndoRedoAfter = true;
					savedUndoRedo = synchInfo;
					continue;
				}
				// Do the synch
				if (!Synchronize(synchInfo, cache))
				{
					fProcessUndoRedoAfter = false; // Refresh already done, final UndoRedo unnecessary
					break; // One resulted in Refresh everything, ignore other synch msgs.
				}
			}
			if (fProcessUndoRedoAfter)
				Synchronize(savedUndoRedo, cache);

			// NOTE: This code may present a race condition, because there is a slight
			// possibility that a sync message can come to the App at
			// this point and then get cleared from the syncMessages list and never get run.
			EndUpdate(cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suppress all calls to <see cref="RefreshAllViews()"/> until <see cref="EndUpdate"/>
		/// is called.
		/// </summary>
		/// <param name="cache"></param>
		/// <remarks>Used by <see cref="ResumeSynchronize"/> to do only one refresh of the
		/// view.</remarks>
		/// ------------------------------------------------------------------------------------
		public void BeginUpdate(FdoCache cache)
		{
			CheckDisposed();

			// No need to do the assert, since 'Add' will throw an exception,
			// if the cache is already in the Dictionary,
			// or if 'cache' is null.
			//Debug.Assert(!m_refreshViewCaches.ContainsKey(cache), "Nested BeginUpdate");
			m_refreshViewCaches.Add(cache, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do a <see cref="RefreshAllViews()"/> if it was called at least once after
		/// <see cref="BeginUpdate"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndUpdate(FdoCache cache)
		{
			CheckDisposed();

			// No need for assert, since it will throw an exception if it isn't there.
			//Debug.Assert(m_refreshViewCaches.ContainsKey(cache), "EndUpdate called without BeginUpdate");
			bool needsRefreshed = m_refreshViewCaches[cache];
			m_refreshViewCaches.Remove(cache);
			if (needsRefreshed)
				RefreshAllViews(cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cycle through the applications main windows and synchronize them with database
		/// changes.
		/// </summary>
		/// <param name="sync">synchronization information record</param>
		/// <param name="cache">database cache</param>
		/// <returns>false if a refreshall was performed or presync failed; this suppresses
		/// subsequent sync messages. True to continue processing.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool Synchronize(SyncInfo sync, FdoCache cache)
		{
			CheckDisposed();

			if (m_suppressedCaches.ContainsKey(cache))
			{
				Queue<SyncInfo> messages = m_suppressedCaches[cache].Queue;
				if (!messages.Contains(sync))
					messages.Enqueue(sync);
				return true;
			}

			cache.StoreSync(SyncGuid, sync);

			if (sync.msg == SyncMsg.ksyncFullRefresh || sync.msg == SyncMsg.ksyncCustomField)
			{
				RefreshAllViews(cache);
				return false;
			}

			foreach (IFwMainWnd wnd in MainWindows)
			{
				if (wnd.Cache == cache && !wnd.PreSynchronize(sync))
					return false;
			}

			if (sync.msg == SyncMsg.ksyncWs)
			{
				// REVIEW TeTeam: AfLpInfo::Synchronize calls AfLpInfo::FullRefresh, which
				// clears the cache, loads the styles, loads ws and updates wsf, load project
				// basics, updates external link root, load overlays and refreshes possibility
				// lists. I don't think we need to do any of these here.
				RefreshAllViews(cache);
				return false;
			}
			else if (sync.msg == SyncMsg.ksyncPromoteEntry)
			{
				// Review: Write code here to deal with this case. Look at
				// AfLpInfo::Syncronize to see what's necessary.
				// This would be relevant to an application that uses subentries (like Data Notebook--
				// if it used FwApp).
			}
			else if (sync.msg == SyncMsg.ksyncSimpleEdit)
			{
				// Use the UpdatePropIfCached method to update anything that changed that we care about.
				// Todo: need to get Synchronize called for each new syncinfo in DB on window activate.
				IVwOleDbDa odd = cache.VwOleDbDaAccessor;
				int hvo = sync.hvo;
				int flid = sync.flid;
				FieldType iType = cache.GetFieldType(flid);

				switch(iType)
				{
					case FieldType.kcptMultiString:
					case FieldType.kcptMultiUnicode:
					case FieldType.kcptMultiBigString:
					case FieldType.kcptMultiBigUnicode:
						// Try all active WS to see if cached. (Pathologically, if some wss are used for both,
						// they will be updated twice.)
						foreach (int ws in cache.LangProject.VernWssRC.HvoArray)
							odd.UpdatePropIfCached(hvo, flid, (int)iType, ws);
						foreach (int ws in cache.LangProject.AnalysisWssRC.HvoArray)
							odd.UpdatePropIfCached(hvo, flid, (int)iType, ws);
						// This will usually prevent a double-update; pathologically, one might still happen
						// if the user ws is in the analysis or vernacular lists but is not the first analysis one.
						if (cache.DefaultUserWs != cache.DefaultAnalWs)
							odd.UpdatePropIfCached(hvo, flid, (int)iType, cache.DefaultUserWs);
						break;
					case 0:
						// This is very rare but possible.  Do nothing since kcptNull is not a real data
						// type, hence cannot have any data.
						break;
					default:
						odd.UpdatePropIfCached(hvo, flid, (int)iType, 0);
						break;
				}
				return true;
			}

			foreach (IFwMainWnd wnd in MainWindows)
			{
				if (wnd.Cache == cache && !wnd.Synchronize(sync))
				{
					// The window itself was not able to process the message successfully;
					// play safe and refresh everything
					RefreshAllViews(cache);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read one int from a column in a result set. Indexes are 1-based.
		/// </summary>
		/// <param name="odc"></param>
		/// <param name="icol"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int ReadColVal(IOleDbCommand odc, uint icol)
		{
			bool fIsNull;
			uint cbSpaceTaken;
			uint uintSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
			uint[] uIds;
			using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
			{
				odc.GetColValue(icol, rgHvo, uintSize, out cbSpaceTaken, out fIsNull, 0);
				uIds = (uint[])MarshalEx.NativeToArray(rgHvo, 1, typeof(uint));
			}
			return (int)uIds[0];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all current Sync records from the database and process them.
		/// For this to work (since it should skip records created by this app),
		/// the caller must override SyncGuid.
		/// </summary>
		/// <returns>true if a full refresh was performed, otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		public bool SyncFromDb()
		{
			CheckDisposed();

			Guid syncGuid = SyncGuid;
			if (syncGuid == Guid.Empty)
				return false; // app does not handle automatic synchronization.
			bool fDidFullRefresh = false; // once set true, we skip everything except updating current.
			foreach(FdoCache cache in m_caches.Values)
			{
				// This query returns outstanding sync requests, with duplicate msg/objid/objflid
				// triples eliminated, in order by their id.
				string sql = string.Format("select max(id) id1, msg, objid, objflid from sync$" +
					" where id > {0} and LpInfoId != '{1}' group by msg, objid, objflid order by id1",
					m_nLastSync, syncGuid);
				IOleDbCommand odc = null;
				try
				{
					bool fMoreRows;
					try
					{
						cache.DatabaseAccessor.CreateCommand(out odc);
						odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
						odc.GetRowset(0);
						odc.NextRow(out fMoreRows);
					}
					catch (Exception)
					{
						// JohnT: It appears from TE-6552 that this query can sometimes fail in the very first
						// launch of TE after installation. Possibly somehow the window gets activated before
						// SqlServer is really ready? Anyway, missing one sync, especially at that point, should
						// not hurt, so just ignore our inability to get the sync records.
						return false;
					}

					while(fMoreRows)
					{
						SyncInfo sync;
						m_nLastSync = ReadColVal(odc, 1);
						if (!fDidFullRefresh)
						{
							sync.msg = (SyncMsg) ReadColVal(odc, 2);
							sync.hvo = ReadColVal(odc, 3);
							sync.flid = ReadColVal(odc, 4);
							// We don't want to create a new sync record for the property we're
							// processing!
							cache.SetIgnoreSync(sync.hvo, sync.flid);
							// Process the sync message. If it resulted in a full refresh, skip the
							// rest.
							fDidFullRefresh = !Synchronize(sync, cache);
						}
						odc.NextRow(out fMoreRows);
					}
					cache.SetIgnoreSync(0, 0); // Re-enable making sync recs for all props.
				}
				finally
				{
					DbOps.ShutdownODC(ref odc);
				}
			}

			return fDidFullRefresh;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets application version.
		/// </summary>
		/// <remarks>This is done as method so it can be overridden for testing</remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual int AppVersion
		{
			get {return (int)DbVersion.kdbAppVersion;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether a cache can control the progress bar on the splash
		/// screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CacheCanControlSplashScreenProgress
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows user warning when application version is older than database version.
		/// </summary>
		/// <param name="dbName"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowOldAppWarning(string dbName)
		{
			string msg = string.Format(GetResourceString("kstidOldAppWarningMsg"), dbName);
			MessageBox.Show(msg, GetResourceString("kstidOldAppWarningCaption"),
				MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
				MessageBoxOptions.ServiceNotification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows user warning when database version can't be upgraded because it was
		/// created by an intermediate version of the application.
		/// </summary>
		/// <param name="dbName"></param>
		/// <param name="intermediateApp">true if the app is intermediate, false if
		/// the DB is intermediate</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowNoUpgradeWarning(string dbName, bool intermediateApp)
		{
			string stringID = intermediateApp ? "kstidNoUpgradeMsgByApp" : "kstidNoUpgradeMsgByDB";
			string msg = string.Format(GetResourceString(stringID), dbName);
			MessageBox.Show(msg, GetResourceString("kstidNoUpgradeCaption"),
				MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
				MessageBoxOptions.ServiceNotification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Asks user to confirm that database upgrade should be performed.
		/// </summary>
		/// <param name="svrName"></param>
		/// <param name="dbName"></param>
		/// <returns>true if user chooses to upgrade database</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ShouldUpgradeDatabase(string svrName, string dbName )
		{
			int dbVersion = GetDbVersion(svrName, dbName);
			string msg;

			// first make sure the DB isn't on another machine
			if (!svrName.StartsWith(SystemInformation.ComputerName))
			{
				msg = string.Format( GetResourceString("kstidOldDbVersionRemoteMsg"),
					dbName, svrName, dbVersion, AppVersion, Environment.NewLine);

				// if it is show a msg saying that it can't be upgraded
				MessageBox.Show(msg, GetResourceString("kstidOldDbVersionCaption"),
					MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1,
					MessageBoxOptions.ServiceNotification);
				return false;
			}

			//REVIEW (Mark B.): This piece was commented out because we see little reason to ask
			//for confirmation since the Upgrade process is necessary to anything that calls this
			//and so it is not normally a decision that the user should make -- it should always be
			//Yes.
//			msg = string.Format( GetResourceString("kstidOldDbVersionUpdateMsg"),
//				dbName, dbVersion, AppVersion, Environment.NewLine);
//			DialogResult userChoice = MessageBox.Show(msg,GetResourceString("kstidOldDbVersionCaption"),
//				MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
//				MessageBoxOptions.ServiceNotification);

//			return userChoice == DialogResult.Yes;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks compatability of application version with the version of the dababase.
		/// </summary>
		/// <param name="svrName">Name of database server</param>
		/// <param name="dbName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CheckDbVerCompatibility(string svrName, string dbName)
		{
			CheckDisposed();

			int dbVersion = GetDbVersion(svrName, dbName);

			// Could not open database - calling dialog should handle this
			if (dbVersion < 0)
				return false;

			if (dbVersion == AppVersion)
				return true;

			// Is user running an old version of the App against a newer database?
			if (dbVersion > AppVersion)
			{
				ShowOldAppWarning(dbName);
				return false;
			}

			// Before we upgrade the database, close the splash screen so the progress dialog
			// for upgrading will be on top.
			CloseSplashScreen();

			// Let user decide if database should be upgraded
			if (!ShouldUpgradeDatabase(svrName, dbName))
				return false;

			// Upgrade the database
			try
			{
				IMigrateData migrateData = MigrateDataClass.Create();
				migrateData.Migrate(dbName, AppVersion, Logger.Stream);
			}
			catch
			{
				// Could not migrate for some reason:
				return false;
			}
			return true;
		}
		#endregion

		#region Methods to implement IBackupDelegates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get default backup directory from registry settings.
		/// </summary>
		/// <returns>default backup directory</returns>
		/// ------------------------------------------------------------------------------------
		public string GetDefaultBackupDirectory()
		{
			CheckDisposed();

			return FwRegistrySettings.BackupDirectorySetting;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set default backup directory.
		/// </summary>
		/// <param name="directoryName">backup default directory</param>
		/// ------------------------------------------------------------------------------------
		public void SetDefaultBackupDirectory(string directoryName)
		{
			CheckDisposed();

			FwRegistrySettings.BackupDirectorySetting = directoryName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the local server name (e.g., ls-zook\\SILFW)
		/// </summary>
		/// <returns>the local server name</returns>
		/// ------------------------------------------------------------------------------------
		public string GetLocalServer_Bkupd()
		{
			CheckDisposed();

			return MiscUtils.LocalServerName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a stream interface for backup/restore logging, if it is not null.
		/// </summary>
		/// <returns>a stream interface for backup/restore logging, or null</returns>
		/// ------------------------------------------------------------------------------------
		public IStream GetLogPointer_Bkupd()
		{
			CheckDisposed();
			return Logger.Stream;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call SaveData on each of the app's main windows which is connected to the specified
		/// database.
		/// </summary>
		/// <param name="sServer">Name of the server</param>
		/// <param name="sDbName">Name of the database to be saved</param>
		/// ------------------------------------------------------------------------------------
		public void SaveAllData_Bkupd(string sServer, string sDbName)
		{
			CheckDisposed();

			// See if any of the application main windows connect to this database, so that we
			// can save its data if necessary.
			foreach (IFwMainWnd fwMainWindow in m_rgMainWindows)
			{
				// If the database names match.
				if (fwMainWindow.Cache.IsSameConnection(sServer, sDbName))
				{
					// Save data in this window
					fwMainWindow.SaveData();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check each of the app's main windows for the specified database.
		/// </summary>
		/// <param name="sServer">Name of the server</param>
		/// <param name="sDbName">Name of the database to be saved</param>
		/// <returns>True if any window connects to this database</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsDbOpen_Bkupd(string sServer, string sDbName)
		{
			CheckDisposed();

			// See if any of the application main windows connect to this database.
			foreach (IFwMainWnd fwMainWindow in m_rgMainWindows)
			{
				// If the database names match.
				if (fwMainWindow.Cache.IsSameConnection(sServer, sDbName))
				{
					return true;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls CloseDbAndWindows method.
		/// </summary>
		/// <param name="svrName">Name of the server hosting the database</param>
		/// <param name="dbName">Name of the database to close</param>
		/// <param name="fOkToClose">True to close the application if there are no further
		/// connections after the requested connection is closed. False leaves the application
		/// open.</param>
		/// <returns>True if any windows were closed</returns>
		/// ------------------------------------------------------------------------------------
		public bool CloseDbAndWindows_Bkupd(string svrName,	string dbName, bool fOkToClose)
		{
			CheckDisposed();

			return CloseDbAndWindows(svrName, dbName, fOkToClose);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to reopen after a Database Restore or other major database update.
		/// </summary>
		/// <param name="svrName">Name of the server hosting the database</param>
		/// <param name="dbName">Name of the database</param>
		/// ------------------------------------------------------------------------------------
		public void ReopenDbAndOneWindow_Bkupd(string svrName, string dbName)
		{
			CheckDisposed();

			ReopenDbAndOneWindow(svrName, dbName);
		}

		/// <summary>
		/// Rename the database, first closing any open windows.
		/// </summary>
		/// <param name="svrName"></param>
		/// <param name="dbOldName"></param>
		/// <param name="dbNewName"></param>
		public bool RenameProject(string svrName, string dbOldName, string dbNewName)
		{
			CheckDisposed();
			if (dbOldName.ToLowerInvariant() == dbNewName.ToLowerInvariant())
			{
				MessageBox.Show(FrameworkStrings.kstidNameDiffersOnlyInCase,
					FrameworkStrings.kstidWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;		// nothing to do!
			}
			if (!MiscUtils.IsServerLocal(svrName))
			{
				MessageBox.Show(FrameworkStrings.kstidProjectOnAnotherComputer,
					FrameworkStrings.kstidWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;		// can't rename remote databases!
			}
			CloseDbAndWindows(svrName, dbOldName, false);
			bool fRenamed = SIL.FieldWorks.FDO.LangProj.LangProject.RenameProject(dbOldName, dbNewName);
			ReopenDbAndOneWindow(svrName, fRenamed ? dbNewName : dbOldName);
			if (!fRenamed)
			{
				// Rename failed -- ensure the project name is the same as the database name.
				FdoCache cache;
				GetCache(svrName, dbOldName, out cache);
				if (cache.LangProject.Name.UserDefaultWritingSystem != cache.DatabaseName)
					cache.LangProject.Name.UserDefaultWritingSystem = cache.DatabaseName;
			}
			return fRenamed;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// REVIEW DavidO: Increment the count of objects (currently, typically FwTool objects) an
		/// application has made available to other processes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void IncExportedObjects_Bkupd()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// REVIEW DavidO: Decrement the count of objects (currently, typically FwTool objects) an
		/// application has made available to other processes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DecExportedObjects_Bkupd()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the specified database
		/// </summary>
		/// <param name="svrName">Name of the server hosting the database (uses local server if
		/// this is empty)</param>
		/// <param name="dbName">Name of the database</param>
		/// <returns>the version number of the specified database</returns>
		/// ------------------------------------------------------------------------------------
		public int GetDbVersion(string svrName, string dbName)
		{
			// if no database name is given then no version can be obtained
			int version = -1;
			if (dbName == null || dbName == string.Empty)
				return version;

			// if no server was given then start on the local machine
			if (svrName == null || svrName == string.Empty)
				svrName = MiscUtils.LocalServerName;

			version = GetDbVersion_Internal(svrName, dbName);
			return (version == 3) ? 5000 : version;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the specified database
		/// </summary>
		/// <param name="svrName">Name of the server hosting the database</param>
		/// <param name="dbName">Name of the database</param>
		/// <returns>the version number of the specified database</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual int GetDbVersion_Internal(string svrName, string dbName)
		{
			Debug.Assert(!string.IsNullOrEmpty(svrName));
			Debug.Assert(!string.IsNullOrEmpty(dbName));

			int version = -1;

			using (SqlConnection sqlConMaster = new SqlConnection(
				string.Format("Server={0}; Database={1}; User ID = sa; Password=inscrutable;" +
					   "Connect Timeout = 30; Pooling=false;", svrName, dbName)))
			{
				SqlDataReader sqlreader = null;
				try
				{
					sqlConMaster.Open();
					SqlCommand sqlComm = sqlConMaster.CreateCommand();

					string sSql = "select DbVer from Version$";
					sqlComm.CommandText = sSql;
					sqlreader = sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult);
					if (sqlreader.Read())
						version = sqlreader.GetInt32(0);
				}
				catch
				{
					// ignore exceptions - returned version will be -1
				}
				finally
				{
					if (sqlreader != null)
						sqlreader.Close();
					sqlConMaster.Close();
				}
			}
			return version;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the version of the specified database
		/// </summary>
		/// <param name="svrName">Name of the server hosting the database (uses local server if
		/// this is empty)</param>
		/// <param name="dbName">Name of the database</param>
		/// <param name="version">New version number of the specified database</param>
		/// ------------------------------------------------------------------------------------
		static public void SetDbVersion(string svrName, string dbName, int version)
		{
			if (svrName == null || svrName == string.Empty)
			{
				svrName = MiscUtils.LocalServerName;
			}
			using (SqlConnection sqlConMaster = new SqlConnection(
				string.Format("Server={0}; Database={1}; User ID = sa; Password=inscrutable;" +
					   "Pooling=false;", svrName, dbName)))
			{
				sqlConMaster.Open();
				SqlCommand sqlComm = sqlConMaster.CreateCommand();

				string sSql = string.Format("update Version$ set DbVer = {0}", version);
				sqlComm.CommandText = sSql;
				int nbrRowsUpdated = sqlComm.ExecuteNonQuery();
				Debug.Assert(nbrRowsUpdated == 1, "Update of database version failed");
				sqlConMaster.Close();
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for compatibility between the application and the database. If they match
		/// then return true. Otherwise false.
		/// </summary>
		/// <param name="svrName">Name of the server hosting the database (uses local server if
		/// this is empty)</param>
		/// <param name="dbName">Name of the database</param>
		/// <returns>true if the app and db are compatible. false if they are
		/// incompatible (after raising an error message)</returns>
		/// ------------------------------------------------------------------------------------
		public bool CheckDbVerCompatibility_Bkupd(string svrName, string dbName)
		{
			CheckDisposed();

			return CheckDbVerCompatibility(svrName, dbName);
		}

		#endregion

		// We don't need a Name property - use Application.ProductName instead and set
		// [assembly: AssemblyProduct("Translation Editor")] in AssemblyInfo.cs

		#region IHelpTopicProvider implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a help file URL or topic
		/// </summary>
		/// <param name="stid"></param>
		/// <param name="iKey"></param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetHelpString(string stid, int iKey)
		{
			CheckDisposed();

			return ResourceHelper.GetHelpString(stid);
		}
		#endregion

		#region Splash screen
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void ShowSplashScreen()
		{
			CheckDisposed();

			m_SplashScreenWnd = new FwSplashScreen();
			m_SplashScreenWnd.Show();
			m_SplashScreenWnd.Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void CloseSplashScreen()
		{
			CheckDisposed();

			// Close the splash screen
			if (m_SplashScreenWnd != null)
			{
				// We have to clear out the reference to the progress bar on the splash screen
				// before we close the splash screen otherwise we're in trouble
				foreach (FdoCache cache in m_caches.Values)
				{
					if (cache.ProgressBar == m_SplashScreenWnd.ProgressBar)
						cache.ProgressBar = null;
				}

				m_SplashScreenWnd.Close();
				m_SplashScreenWnd = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write to the splash screen
		/// </summary>
		/// <param name="msg">Text to display</param>
		/// ------------------------------------------------------------------------------------
		public virtual void WriteSplashScreen(string msg)
		{
			CheckDisposed();

			if (m_SplashScreenWnd != null)
			{
				// Set the splash screen message
				m_SplashScreenWnd.Message = msg;
				m_SplashScreenWnd.Refresh();
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a main window on a particular object in a particular database. Will fail if
		/// the specified tool cannot handle the specified top-level object. Returns a value
		/// which can be used to identify the particular window in subsequent calls.
		/// </summary>
		/// <param name="bstrServerName">Name of the MSDE/SQLServer computer.</param>
		/// <param name="bstrDbName">Name of the database.</param>
		/// <param name="hvoLangProj">Which languate project within the database.</param>
		/// <param name="hvoMainObj">The top-level object on which to open the window.</param>
		/// <param name="encUi">The user-interface writing system.</param>
		/// <param name="nTool">A tool-dependent identifier of which tool to use.</param>
		/// <param name="nParam">Another tool-dependent parameter.</param>
		/// <param name="pidNew">Process id of the new main window's process.</param>
		/// <returns>The newly created window.</returns>
		/// ------------------------------------------------------------------------------------
		private Form NewMainWndInternal(string bstrServerName, string bstrDbName, int hvoLangProj,
			int hvoMainObj, int encUi, int nTool, int nParam, out int pidNew)
		{
			// TODO: AppCore\AfFwTool.cpp checks to see if an instance of IFwTool already
			// exists and re-uses that. We may have to do something similar if we reactivate
			// FwExplorer.

			Form newWindow;
			try
			{
				// Display the splash screen.
				ShowSplashScreen();

				newWindow = ReopenDbAndOneWindow(bstrServerName, bstrDbName);
			}
			catch(Exception e)
			{
				// not localized because this method is never called.
				throw new ApplicationException("Creation of main window failed", e);
			}
			finally
			{
				CloseSplashScreen();
			}

			if (newWindow == null)
				// not localized because this method is never called.
				throw new ApplicationException("Creation of main window failed");

			pidNew = Process.GetCurrentProcess().Id;
			return newWindow;
		}

		#region IFwTool Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare application to enter or leave a state in which an app-modal process can be
		/// performed by disabling/enabling all main windows associated with this tool.
		/// </summary>
		/// <param name="fModalState">If true, this will enter the modal state. Otherwise it will leave modal
		/// state.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetAppModalState(bool fModalState)
		{
			CheckDisposed();

			foreach (Control ctrl in m_rgMainWindows)
				ctrl.Enabled = !fModalState;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a main window on a particular object in a particular database. Will fail if
		/// the specified tool cannot handle the specified top-level object. Returns a value
		/// which can be used to identify the particular window in subsequent calls.
		/// </summary>
		/// <param name="bstrServerName">Name of the MSDE/SQLServer computer.</param>
		/// <param name="bstrDbName">Name of the database.</param>
		/// <param name="hvoLangProj">Which languate project within the database.</param>
		/// <param name="hvoMainObj">The top-level object on which to open the window.</param>
		/// <param name="encUi">The user-interface writing system.</param>
		/// <param name="nTool">A tool-dependent identifier of which tool to use.</param>
		/// <param name="nParam">Another tool-dependent parameter.</param>
		/// <param name="pidNew">Process id of the new main window's process.</param>
		/// <returns>Handle to the newly created window.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int NewMainWnd(string bstrServerName, string bstrDbName, int hvoLangProj,
			int hvoMainObj, int encUi, int nTool, int nParam, out int pidNew)
		{
			CheckDisposed();

			// TODO: AppCore\AfFwTool.cpp checks to see if an instance of IFwTool already
			// exists and re-uses that. We may have to do something similar if we reactivate
			// FwExplorer.

			Form newWindow = NewMainWndInternal(bstrServerName, bstrDbName, hvoLangProj,
				hvoMainObj, encUi, nTool, nParam, out pidNew);
			Debug.Assert(newWindow != null); // if we can't open one, we throw an exception
			return newWindow.Handle.ToInt32();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows launching from the FwExplorer. Caller assumes that a new window gets created
		/// and becomes the current one.
		/// If this fails, implementation should throw an exception.
		/// </summary>
		/// <param name="bstrServerName">Name of the MSDE/SQLServer computer.</param>
		/// <param name="bstrDbName">Name of the database.</param>
		/// <param name="hvoLangProj">Which languate project within the database.</param>
		/// <param name="hvoMainObj">The top-level object on which to open the window.</param>
		/// <param name="encUi">The user-interface writing system.</param>
		/// <param name="nTool">A tool-dependent identifier of which tool to use.</param>
		/// <param name="nParam">Another tool-dependent parameter.</param>
		/// <param name="rghvo">Pointer to an array of object ids.</param>
		/// <param name="chvo">Number of object ids in rghvo.</param>
		/// <param name="rgflid">Pointer to an array of flids.</param>
		/// <param name="cflid">Number of flids in rgflid.</param>
		/// <param name="ichCur">Cursor offset from beginning of field.</param>
		/// <param name="nView">The view to display when showing the first object. Use -1 to
		/// use the first view.</param>
		/// <param name="pidNew">Process id of the new main window's process.</param>
		/// <returns>Handle to the newly created window.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int NewMainWndWithSel(string bstrServerName, string bstrDbName,
			int hvoLangProj, int hvoMainObj, int encUi, int nTool, int nParam, int[] rghvo,
			int chvo, int[] rgflid, int cflid, int ichCur, int nView, out int pidNew)
		{
			CheckDisposed();

			throw new NotImplementedException("NewMainWndWithSel is not implemented yet");
			//			IFwMainWnd newMainWindow = NewMainWndInternal(bstrServerName, bstrDbName,
			//				hvoLangProj,  hvoMainObj, encUi, nTool, nParam, out pidNew) as IFwMainWnd;
			//
			//			if (newMainWindow != null)
			//			{
			//				IVwRootBox rootBox = newMainWindow.ActiveView.CastAsIVwRootSite().RootBox;
			//				rootBox.MakeTextSelection(/* figure out the arguments */);
			//			}
			//
			//			return ((Form)newMainWindow).Handle.ToInt32();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ask a main window to close. May return <c>true</c> if closing the window requires
		/// the user to confirm a save, and the user says cancel. In this case the caller should
		/// normally abort whatever required the window to close.
		/// If the window is not found (invalid handle, or a handle to a window that already
		/// closed), an exception is thrown.
		/// </summary>
		/// <param name="htool">Window handle</param>
		/// <returns><c>true</c> if closing of window was aborted, otherwise <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool CloseMainWnd(int htool)
		{
			CheckDisposed();

			IntPtr handle = new IntPtr(htool);
			Form mainWindow = Control.FromHandle(handle) as Form;
			if (mainWindow == null)
				// not localized because this method is never called.
				throw new ApplicationException("Can't find window");

			mainWindow.Close();
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close any windows associated with a database, save the database, clear all caches,
		/// and shutdown the connection to the database.
		/// </summary>
		///
		/// <param name="sServer">Server of the database to close</param>
		/// <param name="sDbName">Name of the database to close</param>
		/// <param name='fOkToClose'>True to close the application if there are no further
		/// connections after the requested connection is closed. False leaves the application
		/// open.</param>
		/// -----------------------------------------------------------------------------------
		void IFwTool.CloseDbAndWindows(string sServer, string sDbName, bool fOkToClose)
		{
			CheckDisposed();

			CloseDbAndWindows(sServer, sDbName, fOkToClose);
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

		/// <summary>
		/// Note the most recent of our main windows to become active.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void fwMainWindow_Activated(object sender, EventArgs e)
		{
			m_activeMainWindow = (Form) sender;
		}

		/// <summary>
		/// Make sure a window that's no longer valid isn't considered active.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void fwMainWindow_HandleDestroyed(object sender, EventArgs e)
		{
			if (m_activeMainWindow == sender)
				m_activeMainWindow = null;
		}

		/// <summary>
		/// Handle changes to the external links root directory for a language project.
		/// </summary>
		/// <param name="sOldExtLinkRootDir"></param>
		/// <param name="proj"></param>
		/// <remarks>This may not be the best place for this method, but I'm not sure there is a
		/// "best place".</remarks>
		public static bool UpdateExternalLinks(string sOldExtLinkRootDir, ILangProject proj)
		{
			string sNewExtLinkRootDir = proj.ExtLinkRootDir;
			// caseless comparison may not be valid on non-Microsoft OS!
			if (!sNewExtLinkRootDir.Equals(sOldExtLinkRootDir, StringComparison.InvariantCultureIgnoreCase))
			{
				List<string> rgFilesToMove = new List<string>();
				// TODO: offer to move or copy existing files.
				foreach (ICmFolder cf in proj.MediaOC)
					CollectMovableFilesFromFolder(cf, rgFilesToMove, sOldExtLinkRootDir, sNewExtLinkRootDir);
				foreach (ICmFolder cf in proj.PicturesOC)
					CollectMovableFilesFromFolder(cf, rgFilesToMove, sOldExtLinkRootDir, sNewExtLinkRootDir);
				if (rgFilesToMove.Count > 0)
				{
					FileLocationChoice action;
					using (MoveOrCopyFilesDlg dlg = new MoveOrCopyFilesDlg())
					{
						dlg.Initialize(rgFilesToMove.Count, sOldExtLinkRootDir, sNewExtLinkRootDir, FwApp.App);
						DialogResult res = dlg.ShowDialog();
						Debug.Assert(res == DialogResult.OK);
						if (res != DialogResult.OK)
							return false;	// should never happen!
						action = dlg.Choice;
					}
					if (action == FileLocationChoice.Leave) // Expand path
					{
						foreach (ICmFolder cf in proj.MediaOC)
							ExpandToFullPath(cf, sOldExtLinkRootDir, sNewExtLinkRootDir);
						foreach (ICmFolder cf in proj.PicturesOC)
							ExpandToFullPath(cf, sOldExtLinkRootDir, sNewExtLinkRootDir);
						return false;
					}
					// We need to ensure that none of the picture files is currently being
					// displayed, as that locks the file.  The easiest way to achieve this is
					// to shutdown the main window while we're moving the files.
					string sServer = proj.Cache.ServerName;
					string sDbname = proj.Cache.DatabaseName;
					FwApp.App.CloseDbAndWindows(sServer, sDbname, false);
					GC.Collect();	// make sure the window is disposed!
					Thread.Sleep(1000);
					List<string> rgLockedFiles = new List<string>();
					foreach (string sFile in rgFilesToMove)
					{
						string sOldPathname = Path.Combine(sOldExtLinkRootDir, sFile);
						string sNewPathname = Path.Combine(sNewExtLinkRootDir, sFile);
						string sNewDir = Path.GetDirectoryName(sNewPathname);
						if (!Directory.Exists(sNewDir))
							Directory.CreateDirectory(sNewDir);
						Debug.Assert(File.Exists(sOldPathname));
						Debug.Assert(!File.Exists(sNewPathname));
						try
						{
							if (action == FileLocationChoice.Move)
								File.Move(sOldPathname, sNewPathname);
							else
								File.Copy(sOldPathname, sNewPathname);
						}
						catch (Exception ex)
						{
							Debug.WriteLine(String.Format("{0}: {1}", ex.Message, sOldPathname));
							rgLockedFiles.Add(sFile);
						}
					}
					FwApp.App.ReopenDbAndOneWindow(sServer, sDbname);
					// If any files failed to be moved or copied above, try again now that we've
					// opened a new window and had more time elapse (and more demand to reuse
					// memory) since the failure.
					if (rgLockedFiles.Count > 0)
					{
						GC.Collect();	// make sure the window is disposed!
						Thread.Sleep(1000);
						foreach (string sFile in rgLockedFiles)
						{
							string sOldPathname = Path.Combine(sOldExtLinkRootDir, sFile);
							string sNewPathname = Path.Combine(sNewExtLinkRootDir, sFile);
							try
							{
								if (action == FileLocationChoice.Move)
									File.Move(sOldPathname, sNewPathname);
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
		/// Build a list of files that can be moved (or copied) to the new external links root
		/// directory.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="rgFilesToMove"></param>
		/// <param name="sOldRootDir"></param>
		/// <param name="sNewRootDir"></param>
		private static void CollectMovableFilesFromFolder(ICmFolder folder,
			List<string> rgFilesToMove, string sOldRootDir, string sNewRootDir)
		{
			foreach (ICmFile file in folder.FilesOC)
			{
				string sFilepath = file.InternalPath;
				if (!Path.IsPathRooted(sFilepath))
				{
					if (File.Exists(Path.Combine(sOldRootDir, sFilepath)) &&
						!File.Exists(Path.Combine(sNewRootDir, sFilepath)))
					{
						rgFilesToMove.Add(sFilepath);
					}
				}
			}
			foreach (ICmFolder sub in folder.SubFoldersOC)
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
			foreach (ICmFile file in folder.FilesOC)
			{
				string sFilepath = file.InternalPath;
				if (!Path.IsPathRooted(sFilepath))
				{
					if (File.Exists(Path.Combine(sOldRootDir, sFilepath)) &&
						!File.Exists(Path.Combine(sNewRootDir, sFilepath)))
					{
						file.InternalPath = Path.Combine(sOldRootDir, sFilepath);
					}
				}
			}
			foreach (ICmFolder sub in folder.SubFoldersOC)
				ExpandToFullPath(sub, sOldRootDir, sNewRootDir);
		}
	}
	#endregion
}
