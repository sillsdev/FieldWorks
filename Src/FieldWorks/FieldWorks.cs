// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Gecko;
using Microsoft.Win32;
using LanguageExplorer;
using LanguageExplorer.LcmUi;
using LanguageExplorer.SendReceive;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices.BackupRestore;
using SIL.LCModel.DomainServices.DataMigration;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.FieldWorks.LexicalProvider;
using SIL.FieldWorks.PaObjects;
using SIL.FieldWorks.Resources;
using SIL.Keyboarding;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.Utils;
using SIL.Windows.Forms.HtmlBrowser;
using SIL.Windows.Forms.Keyboarding;
using SIL.WritingSystems;

namespace SIL.FieldWorks
{
	#region FieldWorks class
	/// <summary>
	/// Entry point for the FieldWorks process.
	/// </summary>
	public static class FieldWorks
	{
		#region Enumerations
		/// <summary>
		/// Possible values for the previously loaded project
		/// </summary>
		private enum StartupStatus
		{
			/// <summary>The previous startup failed to finish.</summary>
			Failed,
			/// <summary>The previous startup completed successfully.</summary>
			Successful,
			/// <summary>The previous startup is still loading.</summary>
			StillLoading,
		}
		#endregion

		#region Constants
		private const int kStartingPort = 9628; // Pretty arbitrary, but this is what TE used to use.
		private const string kFwRemoteRequest = "FW_RemoteRequest";
		internal const string kPaRemoteRequest = "PA_RemoteRequest";
		#endregion

		#region Static variables

		private static volatile bool s_allowFinalShutdown = true;
		private static volatile bool s_fWaitingForUserOrOtherFw;
		private static volatile bool s_fSingleProcessMode;
		private static volatile ProjectId s_projectId;
		private static bool s_applicationExiting;
		private static bool s_doingRename;
		private static bool s_renameSuccessful;
		private static string s_renameNewName;
		private static IFlexApp s_flexApp;
		private static string s_sWsUser;
		private static FwRegistrySettings s_settingsForLastClosedWindow;
		private static RegistryKey s_flexAppKey;
		private static IFwMainWnd s_activeMainWnd;
		private static FwSplashScreen s_splashScreen;
		private static bool s_noUserInterface;
		private static string s_LinkDirChangedTo;
		private static TcpChannel s_serviceChannel;
		private static int s_servicePort;
		// true if we have no previous reporting settings, typically the first time a version of FLEx that
		// supports usage reporting has been run.
		private static bool s_noPreviousReportingSettings;
		private static ILcmUI s_ui;
		private static FwApplicationSettings s_appSettings;
		private static CompositionContainer s_compositionContainer;
		#endregion

		#region Main Method and Initialization Methods
#if !__MonoCS__
		/// <summary></summary>
		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string fileName);
#endif

		/// <summary>
		/// Sets the ICU_DATA environment variable.
		/// </summary>
		private static void SetIcuDataDirEnvironmentVariable()
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
			{
				return;
			}

			// We read the registry value and set an environment variable ICU_DATA here so that
			// FwKernelInterfaces.dll is independent of WinForms.
			var icuDirValueName = $"Icu{LCModel.Core.Text.Icu.Version}DataDir";
			using(var userKey = RegistryHelper.CompanyKey)
			using(var machineKey = RegistryHelper.CompanyKeyLocalMachine)
			{
				string dir = null;
				if (userKey?.GetValue(icuDirValueName) != null)
				{
					dir = userKey.GetValue(icuDirValueName, dir) as string;
				}
				else if (machineKey?.GetValue(icuDirValueName) != null)
				{
					dir = machineKey.GetValue(icuDirValueName, dir) as string;
				}
				if (!string.IsNullOrEmpty(dir))
				{
					Environment.SetEnvironmentVariable("ICU_DATA", dir);
			}
		}
		}

		/// <summary>
		/// The main entry point for the FieldWorks executable.
		/// </summary>
		/// <param name="rgArgs">The command line arguments.</param>
		[STAThread]
		private static int Main(string[] rgArgs)
		{
			// Enable the message box to be able to attach a new FW process to a debugger.
			//MessageBox.Show(@"Attach to your debugger now.", @"Attach Now", MessageBoxButtons.OK);

			FwUtils.InCrashedState = false;
			Thread.CurrentThread.Name = "Main thread";
			Logger.Init(FwUtils.ksSuiteName);

			Icu.Wrapper.ConfineIcuVersions(54);
			LcmCache.NewerWritingSystemFound += ComplainToUserAboutNewWs;
			FwRegistryHelper.Initialize();

			try
			{
				// Create main catalogs.
				var globalPartsAggregateCatalog = new AggregateCatalog();
				var scopedChildAggregateCatalog = new AggregateCatalog();
				var compositionScopeDefinition = globalPartsAggregateCatalog.AsScope(scopedChildAggregateCatalog.AsScope());
				s_compositionContainer = new CompositionContainer(compositionScopeDefinition);
				// Add parts to globally scoped catalog.
				var globalTypes = LanguageExplorerCompositionServices.GetGloballyAvailableTypes();
				globalTypes.Add(typeof(FieldWorksManager));
				globalPartsAggregateCatalog.Catalogs.Add(new TypeCatalog(globalTypes));
				// Add parts to windows level scoped catalog
				scopedChildAggregateCatalog.Catalogs.Add(new TypeCatalog(LanguageExplorerCompositionServices.GetWindowScopedTypes()));

				#region Initialize XULRunner - required to use the geckofx WebBrowser Control (GeckoWebBrowser).
				var exePath = Path.GetDirectoryName(Application.ExecutablePath);
				var firefoxPath = Environment.GetEnvironmentVariable("XULRUNNER");
				if (string.IsNullOrEmpty(firefoxPath))
				{
					firefoxPath = Path.Combine(exePath, "Firefox");
				}
				Xpcom.Initialize(firefoxPath);
				GeckoPreferences.User["gfx.font_rendering.graphite.enabled"] = true;
				// Set default browser for XWebBrowser to use GeckoFX.
				// This can still be changed per instance by passing a parameter to the constructor.
				XWebBrowser.DefaultBrowserType = XWebBrowser.BrowserType.GeckoFx;
#endregion Initialize XULRunner

				Logger.WriteEvent("Starting app");
				SetGlobalExceptionHandler();
				SetupErrorReportInformation();

				// Invoke does nothing directly, but causes BroadcastEventWindow to be initialized
				// on this thread to prevent race conditions on shutdown.See TE-975
				// See http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=911603&SiteID=1
				// TODO-Linux: uses mono feature that is not implemented. What are the implications of this? Review.
				if (MiscUtils.IsDotNet)
				{
					SystemEvents.InvokeOnEventsThread(new Action(DoNothing));
				}

				ThreadHelper = new ThreadHelper();

				// ENHANCE (TimS): Another idea for ensuring that we have only one process started for
				// this project is to use a Mutex. They can be used for cross-process resource access
				// and would probably be less error-prone then our current implementation since it
				// doesn't use TCP connections which can get hampered by firewalls. We would probably still
				// need our current listener functionality for communicating with the other FW process,
				// so it may not buy us much.
				// See http://kristofverbiest.blogspot.com/2008/11/creating-single-instance-application.html.

				// Make sure we do this ASAP. If another FieldWorks.exe is started we need
				// to make sure it can find this one to ask about its project. (FWR-595)
				CreateRemoteRequestListener();

#if DEBUG
				WriteExecutablePathSettingForDevs();
#endif

				if (IsInSingleFWProccessMode())
				{
					Logger.WriteEvent("Exiting: Detected single process mode");
					return 0;
				}

				if (MigrateProjectsTo70())
				{
					Logger.WriteEvent("Migration to Version 7 was still needed.");
				}

				// Enable visual styles. Ignored on Windows 2000. Needs to be called before
				// we create any controls! Unfortunately, this alone is not good enough. We
				// also need to use a manifest, because some ListView and TreeView controls
				// in native code do not have icons if we just use this method. This is caused
				// by a bug in XP.
				Application.EnableVisualStyles();

				// Set ICU_DATA environment variable
				SetIcuDataDirEnvironmentVariable();

				// initialize ICU
				LCModel.Core.Text.Icu.InitIcuDataDir();

				// initialize the SLDR
				Sldr.Initialize();

				// initialize Palaso keyboarding
				KeyboardController.Initialize();

				var appArgs = new FwAppArgs(rgArgs);
				s_noUserInterface = appArgs.NoUserInterface;
				InAppServerMode = appArgs.AppServerMode;

				s_ui = new FwLcmUI(GetHelpTopicProvider(), ThreadHelper);

				s_appSettings = new FwApplicationSettings();
				s_appSettings.DeleteCorruptedSettingsFilesIfPresent();
				s_appSettings.UpgradeIfNecessary();

				var reportingSettings = s_appSettings.Reporting;
				if (reportingSettings == null)
				{
					// Note: to simulate this, currently it works to delete all subfolders of
					// (e.g.) C:\Users\thomson\AppData\Local\SIL\FieldWorks.exe_Url_tdkbegygwiuamaf3mokxurci022yv1kn
					// That guid may depend on version or something similar; it's some artifact of how the Settings persists.
					s_noPreviousReportingSettings = true;
					reportingSettings = new ReportingSettings();
					s_appSettings.Reporting = reportingSettings; //to avoid a defect in Settings rely on the Save in the code below
				}

				// Allow develpers and testers to avoid cluttering our analytics by setting an environment variable (FEEDBACK = false)
				var feedbackEnvVar = Environment.GetEnvironmentVariable("FEEDBACK");
				if (feedbackEnvVar != null)
				{
					reportingSettings.OkToPingBasicUsageData = feedbackEnvVar.ToLower().Equals("true") || feedbackEnvVar.ToLower().Equals("yes");
				}

				// Note that in FLEx we are using this flag to indicate whether we can send usage data at all.
				// Despite its name, Cambell says this is the original intent (I think there may have been
				// some thought of adding flags one day to control sending more detailed info, but if 'basic
				// navigation' is suppressed nothing is sent). May want to consider renaming to something like
				// OkToPingAtAll, but that affects other Palaso clients.
				// The usage reporter does not currently send anything at all if the flag is false, but to make
				// sure, we don't even initialize reporting if it is false.
				// (Note however that it starts out true. Thus, typically a few pings will be sent
				// on the very first startup, before the user gets a chance to disable it.)
				if (reportingSettings.OkToPingBasicUsageData)
				{
					UsageReporter.Init(reportingSettings, "flex.palaso.org", "UA-39238981-3",
#if DEBUG
						true
#else
						false
#endif
						);
					// Init updates various things in the ReportingSettings, such as the number of times
					// the application has been launched and the 'previous' version.
					s_appSettings.Save();
				}

				// e.g. the first time the user runs FW9, we need to copy a bunch of registry keys
				// from HKCU/Software/SIL/FieldWorks/7.0 -> FieldWorks/9 or
				// from HKCU/Software/SIL/FieldWorks/8 -> FieldWorks/9 and
				// from HKCU/Software/WOW6432Node/SIL/FieldWorks -> HKCU/Software/SIL/FieldWorks
				FwRegistryHelper.UpgradeUserSettingsIfNeeded();

				if (appArgs.ShowHelp)
				{
					ShowCommandLineHelp();
					return 0;
				}
				else if (!string.IsNullOrEmpty(appArgs.ChooseProjectFile))
				{
					var projId = ChooseLangProject();
					if (projId == null)
					{
						return 1; // User probably canceled
					}
					try
					{
						// Use PipeHandle because this will probably be used to locate a named pipe using
						// PipeHandle as the identifier.
						File.WriteAllText(appArgs.ChooseProjectFile, projId.Handle, Encoding.UTF8);
					}
					catch (Exception e)
					{
						Logger.WriteError(e);
						return 2;
					}
					return 0;
				}

				if (!SetUICulture(appArgs))
				{
					return 0; // Error occurred and user chose not to continue.
				}

				if (FwRegistryHelper.FieldWorksRegistryKeyLocalMachine == null && FwRegistryHelper.FieldWorksRegistryKey == null)
				{
					// See LT-14461. Some users have managed to get their computers into a state where
					// neither HKML nor HKCU registry entries can be read. We don't know how this is possible.
					// This is so far the best we can do.
					var expected = "HKEY_LOCAL_MACHINE/Software/SIL/FieldWorks/" + FwRegistryHelper.FieldWorksRegistryKeyName;
					MessageBoxUtils.Show(string.Format(Properties.Resources.ksHklmProblem, expected), Properties.Resources.ksHklmCaption);
					return 0;
				}

				if (!string.IsNullOrEmpty(appArgs.BackupFile))
				{
					LaunchRestoreFromCommandLine(appArgs);
					if (s_flexApp == null)
					{
						return 0; // Restore was cancelled or failed, or another process took care of it.
					}
					if (!string.IsNullOrEmpty(s_LinkDirChangedTo))
					{
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => Cache.LangProject.LinkedFilesRootDir = s_LinkDirChangedTo);
					}
				}
				else if (!LaunchApplicationFromCommandLine(appArgs))
				{
					return 0; // Didn't launch, but probably not a serious error
				}

				// Create a listener for this project for applications using FLEx as a LexicalProvider.
				LexicalProviderManager.StartLexicalServiceProvider(s_projectId, Cache);

				if (MiscUtils.IsMono)
				{
					UglyHackForXkbIndicator();
				}

				// Application was started successfully, so start the message loop
				Application.Run();
			}
			catch (ApplicationException ex)
			{
				MessageBox.Show(ex.Message, FwUtils.ksSuiteName);
				return 2;
			}
			catch (Exception ex)
			{
				SafelyReportException(ex, s_activeMainWnd, true);
				return 2;
			}
			finally
			{
				StaticDispose();
				if (Xpcom.IsInitialized)
				{
					// The following line appears to be necessary to keep Xpcom.Shutdown()
					// from triggering a scary looking "double free or corruption" message most
					// of the time.  But the Xpcom.Shutdown() appears to be needed to keep the
					// program from hanging around sometimes after it supposedly exits.
					// Doing the shutdown here seems cleaner than using an ApplicationExit
					// delegate.
					var foo = new GeckoWebBrowser();
					Xpcom.Shutdown(); // REVIEW pH 2016.07: likely not necessary with Gecko45
				}
			}
			return 0;
		}

		/// <summary>
		/// For some reason, setting an Xkb keyboard for the first time doesn't work well inside
		/// FieldWorks.  The keyboard is actually set (although it may take effect only after the
		/// first one or two keystrokes), but the indicator on the system icon bar does not change.
		/// Setting several Xkb keyboards at this point seems to fix the problem for when the first
		/// one is set different than the default keyboard.  This hack is not guaranteed to work,
		/// but it does seem to help in most scenarios.  See FWNX-1299.
		/// </summary>
		/// <remarks>
		/// If you can think of a better solution, by all means replace this ugly hack!  It took
		/// me a day of work to come up with even this much.  I tried setting the multiple keyboards
		/// in succession inside Palaso.UI.WindowsForms.Keyboarding.Linux.XkbKeyboardAdaptor.ReinitLocales()
		/// but it didn't work doing it there for some reason.
		/// </remarks>
		private static void UglyHackForXkbIndicator()
		{
			foreach (var ws in Cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				ws.LocalKeyboard?.Activate();
			}
			Keyboard.Controller.ActivateDefaultKeyboard();
		}

		/// <summary>
		/// Launches the application when requested from the command-line.
		/// </summary>
		/// <param name="appArgs">The application command-line arguments.</param>
		private static bool LaunchApplicationFromCommandLine(FwAppArgs appArgs)
		{
			// Get the application requested on the command line
			if (!CreateApp(appArgs))
			{
				return false;
			}

			// Get the project the user wants to open and attempt to launch it.
			var projectId = DetermineProject(appArgs);
			if (projectId != null && IsSharedXmlBackendNeeded(projectId))
			{
				projectId.Type = BackendProviderType.kSharedXML;
			}

			// s_projectId can be non-null if the user decided to restore a project from
			// the Welcome to Fieldworks dialog. (FWR-2146)
			if (s_projectId == null && !LaunchProject(appArgs, ref projectId))
			{
				return false;
			}

			// The project was successfully loaded so store it. This will let any other
			// FieldWorks processes that are waiting on us be able to continue.
			s_projectId = projectId;

			// Warn user about failed Lift import if necessary.
			var liftFolder = CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			if (LiftImportFailureServices.GetFailureStatus(liftFolder) != ImportFailureStatus.NoImportNeeded)
			{
				MessageBox.Show(LanguageExplorerResources.LiftSRFailureDetectedOnStartupMessage,
					LanguageExplorerResources.LiftSRFailureDetectedOnStartupTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}

			if (s_noUserInterface)
			{
				// We should have a main window by now, so the help button on the dialog
				// will work if needed.
				CheckForMovingExternalLinkDirectory(GetOrCreateApplication(appArgs));
			}

			return true;
		}

		private static bool IsSharedXmlBackendNeeded(ProjectId projectId)
		{
			return projectId.Type == BackendProviderType.kXML && ParatextHelper.GetAssociatedProject(projectId) != null;
		}

		/// <summary>
		/// Creates the application requested on the command line.
		/// </summary>
		/// <param name="appArgs">The command-line arguments.</param>
		/// <returns>Indication of whether application was successfully created.</returns>
		private static bool CreateApp(FwAppArgs appArgs)
		{
			var app = GetOrCreateApplication(appArgs);
			if (app == null)
			{
				return false; // We can't do much without an application to start
			}
			Debug.Assert(!app.HasBeenFullyInitialized);

			Logger.WriteEvent("Created application: " + app.GetType().Name);
			return true;
		}

		/// <summary>
		/// Launches a restore project when requested from the command-line.
		/// </summary>
		/// <param name="appArgs">The application command-line arguments.</param>
		private static void LaunchRestoreFromCommandLine(FwAppArgs appArgs)
		{
			RestoreProject(null, appArgs.BackupFile);
		}

		/// <summary>
		/// Sets the UI culture.
		/// </summary>
		/// <param name="args">The application arguments</param>
		private static bool SetUICulture(FwAppArgs args)
		{
			// Try the UI locale found on the command-line (if any).
			var locale = args.Locale;
			// If that doesn't exist, try the UI locale found in the registry.
			if (string.IsNullOrEmpty(locale))
			{
				locale = (string)FwRegistryHelper.FieldWorksRegistryKey.GetValue(FwRegistryHelper.UserLocaleValueName, string.Empty);
			}
			// If that doesn't exist, try the current system UI locale set at program startup
			// This is typically en-US, but we want this to match en since our English localizations use en.
			if (string.IsNullOrEmpty(locale) && Thread.CurrentThread.CurrentUICulture != null)
			{
				locale = Thread.CurrentThread.CurrentUICulture.Name;
				if (locale.StartsWith("en-"))
				{
					locale = "en";
			}
			}
			// If that doesn't exist, just use English ("en").
			if (string.IsNullOrEmpty(locale))
			{
				locale = "en";
			}
			else if (locale != "en")
			{
				// Check whether the desired locale has a localization, ignoring the
				// country code if necessary.  Fall back to English ("en") if no
				// localization exists.
				var rgsLangs = GetAvailableLangsFromSatelliteDlls();
				if (!rgsLangs.Contains(locale))
				{
					var originalLocale = locale;
					var idx = locale.IndexOf('-');
					if (idx > 0)
					{
						locale = locale.Substring(0, idx);
					}
					if (!rgsLangs.Contains(locale))
					{
						if (MessageBox.Show(string.Format(Properties.Resources.kstidFallbackToEnglishUi, originalLocale),
							Application.ProductName, MessageBoxButtons.YesNo) == DialogResult.No)
						{
							return false;
						}
						locale = "en";
						FwRegistryHelper.FieldWorksRegistryKey.SetValue(FwRegistryHelper.UserLocaleValueName, locale);
					}
				}
			}
			if (locale != Thread.CurrentThread.CurrentUICulture.Name)
			{
				Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
			}

			s_sWsUser = Thread.CurrentThread.CurrentUICulture.Name;
			return true;
		}

		/// <summary>
		/// Get the available localizations.
		/// </summary>
		private static List<string> GetAvailableLangsFromSatelliteDlls()
		{
			// Get the folder in which the program file is stored.
			var sDllLocation = Path.GetDirectoryName(Application.ExecutablePath);

			// Get all the sub-folders in the program file's folder.
			var rgsDirs = Directory.GetDirectories(sDllLocation);

			// Go through each sub-folder and if at least one file in a sub-folder ends
			// with ".resource.dll", we know the folder stores localized resources and the
			// name of the folder is the culture ID for which the resources apply. The
			// name of the folder is stripped from the path and used to add a language
			// to the list.
			return rgsDirs.Where(dir => Directory.GetFiles(dir, "*.resources.dll").Length > 0).Select(dir => Path.GetFileName(dir)).ToList();
		}

		/// <summary>
		/// Dummy method to be used for InvokeOnEventsThread which is used as a way to initialize
		/// the Broadcast window and prevent errors on shutdown.
		/// </summary>
		public static void DoNothing()
		{
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets a value indicating whether FieldWorks can be automatically shut down (as happens
		/// after 30 minutes when running in server mode).
		/// </summary>
		internal static bool ProcessCanBeAutoShutDown
		{
			get
			{
				if (!s_allowFinalShutdown)
				{
					return false; // operation in process without FLEx window open
				}

				return s_flexApp == null || !s_flexApp.MainWindows.Any();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether FieldWorks should stay running even when
		/// all main windows are closed because it is acting as a server for another
		/// application.
		/// </summary>
		internal static bool InAppServerMode { get; set; }

		/// <summary>
		/// Gets the support e-mail address.
		/// </summary>
		private static string SupportEmail => s_flexApp?.SupportEmailAddress;

		/// <summary>
		/// Gets a value indicating whether FW is in "single process mode".
		/// </summary>
		internal static bool InSingleProcessMode => s_fSingleProcessMode;

		/// <summary>
		/// Gets the project associated with this FieldWorks process.
		/// </summary>
		internal static ProjectId Project => s_projectId;

		/// <summary>
		/// Gets the cache used by this FieldWorks instance.
		/// </summary>
		internal static LcmCache Cache { get; private set; }

		/// <summary>
		/// Gets the thread helper used for invoking actions on the main UI thread.
		/// </summary>
		internal static ThreadHelper ThreadHelper { get; private set; }

		/// <summary>
		/// Check to see if there are other instances of the same application running.
		/// </summary>
		/// <returns>List of existing FieldWorks processes being run by this same user.</returns>
		private static List<Process> ExistingProcesses
		{
			get
			{
				var existingProcesses = new List<Process>();
				var thisProcess = Process.GetCurrentProcess();
				try
				{
					var thisProcessName = Assembly.GetExecutingAssembly().GetName().Name;
					var thisSid = FwUtils.GetUserForProcess(thisProcess);
					var processes = Process.GetProcessesByName(thisProcessName).ToList();
					if (MiscUtils.IsUnix)
					{
						processes.AddRange(Process.GetProcesses().Where(p => p.ProcessName.Contains("mono")
							&& p.Modules.Cast<ProcessModule>().Any(m => m.ModuleName == (thisProcessName + ".exe"))));
					}
					existingProcesses.AddRange(processes.Where(procCurr => procCurr.Id != thisProcess.Id && thisSid == FwUtils.GetUserForProcess(procCurr)));
				}
				catch (Exception ex)
				{
					Logger.WriteEvent("Got exception in FieldWorks.ExistingProcess: ");
					Logger.WriteError(ex);
					Debug.Fail("Got exception in FieldWorks.ExistingProcess", ex.Message);
				}
				return existingProcesses;
			}
		}
		#endregion

		/// <summary>
		/// Starts the specified FieldWorks application.
		/// </summary>
		/// <param name="rgArgs">The command-line arguments.</param>
		/// <returns>The new process if started, otherwise, null</returns>
		private static Process StartFwAppAndGetProcess(params string[] rgArgs)
		{
			var bldr = new StringBuilder();

			if (rgArgs.Length == 1 && !rgArgs[0].StartsWith("-"))
			{
				// Assume that the user wants that argument to be the project name
				bldr.Append(" -" + FwAppArgs.kProject);
			}
			foreach (var arg in rgArgs)
			{
				bldr.Append(" ");
				var fAddQuotes = (arg.IndexOf(' ') >= 0); // add quotes around parameters with spaces
				if (fAddQuotes)
				{
					bldr.Append("\"");
				}

				bldr.Append(arg);

				if (fAddQuotes)
				{
					bldr.Append("\"");
			}
			}
			try
			{
				var path = FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase);
				var startInfo = new ProcessStartInfo(path, bldr.ToString())
				{
					UseShellExecute = false,
					WorkingDirectory = Path.GetDirectoryName(path) ?? string.Empty
				};
				return Process.Start(startInfo);
			}
			catch (Exception exception)
			{
				// I (TomH) would rather know about the exception than silently failing.
#if DEBUG
				MessageBox.Show(exception.ToString());
#endif
			}

			// Something went very wrong :(
			return null;
		}

		/// <summary>
		/// Starts the specified FieldWorks application.
		/// </summary>
		/// <param name="rgArgs">The command-line arguments.</param>
		/// <returns>True if the process was successfully started, false otherwise</returns>
		private static bool StartFwApp(params string[] rgArgs)
		{
			var newProcess = StartFwAppAndGetProcess(rgArgs);

			if (newProcess == null)
			{
				// Something went very wrong :(
				return false;
			}
			newProcess.Dispose();
			return true;
		}

		/// <summary>
		/// Get a list of all projects (by ProjectName) currently open by processes on the local
		/// machine.
		/// </summary>
		private static IEnumerable<string> ProjectsInUseLocally
		{
			get
			{
				var projects = new List<string>
		{
					Cache.ProjectId.UiName // be sure to include myself!
				};
			RunOnRemoteClients(kFwRemoteRequest, requestor =>
			{
				projects.Add(requestor.ProjectName);
				return false;
			});

			return projects;
		}
		}

		#region Cache Creation and Handling
		/// <summary>
		/// Creates a cache used for accessing the specified project.
		/// </summary>
		/// <param name="projectId">The project id.</param>
		/// <returns>
		/// A new LcmCache used for accessing the specified project, or null, if a
		/// cache could not be created.
		/// </returns>
		private static LcmCache CreateCache(ProjectId projectId)
		{
			Debug.Assert(projectId.IsValid);

			WriteSplashScreen(string.Format(Properties.Resources.kstidLoadingProject, projectId.UiName));
			var owner = s_splashScreen != null ? s_splashScreen.Form : Form.ActiveForm;
			using (var progressDlg = new ProgressDialogWithTask(owner))
			{
				var cache = LcmCache.CreateCacheFromExistingData(projectId, s_sWsUser, s_ui, FwDirectoryFinder.LcmDirectories, CreateLcmSettings(), progressDlg);
				EnsureValidLinkedFilesFolder(cache);
				// Make sure every project has one of these. (Getting it has a side effect if it does not exist.)
				// Crashes have been caused by trying to create it at an unsafe time (LT-15695).
				var dummy = cache.LangProject.DefaultPronunciationWritingSystem;
				cache.ProjectNameChanged += ProjectNameChanged;
				cache.ServiceLocator.GetInstance<IUndoStackManager>().OnSave += FieldWorks_OnSave;

				SetupErrorPropertiesNeedingCache(cache);
				EnsureDefaultCollationsPresent(cache);
				return cache;
			}
		}

		private static void EnsureDefaultCollationsPresent(LcmCache cache)
		{
			var nullCollationWs = new StringBuilder();
			foreach (var ws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				if (ws == null || ws.DefaultCollation != null)
				{
					continue;
				}
					ws.DefaultCollation = new IcuRulesCollationDefinition("standard");
					nullCollationWs.Append(ws.DisplayLabel + ",");
				}
			if (nullCollationWs.Length > 0)
			{
				nullCollationWs = nullCollationWs.Remove(nullCollationWs.Length - 1, 1);
				var message = string.Format(ResourceHelper.GetResourceString("kstidMissingDefaultCollation"), nullCollationWs);
				MessageBox.Show(message);
			}
			cache.ServiceLocator.WritingSystemManager.Save();
		}

		/// <summary>
		/// Ensure a valid folder for LangProject.LinkedFilesRootDir.  When moving projects
		/// between systems, the stored value may become hopelessly invalid.  See FWNX-1005
		/// for an example of the havoc than can ensue.
		/// </summary>
		/// <remarks>This method gets called when we open the LCM cache.</remarks>
		private static void EnsureValidLinkedFilesFolder(LcmCache cache)
		{
			// If the location of the LinkedFilesRootDir was changed when this project was restored just now;
			// overwrite the location that was restored from the fwdata file.
			if (!string.IsNullOrEmpty(s_LinkDirChangedTo) && !cache.LangProject.LinkedFilesRootDir.Equals(s_LinkDirChangedTo))
			{
				NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () => cache.LangProject.LinkedFilesRootDir = s_LinkDirChangedTo);
			}

			if (MiscUtils.RunningTests)
			{
				return;
			}

			var linkedFilesFolder = cache.LangProject.LinkedFilesRootDir;
			var defaultFolder = LcmFileHelper.GetDefaultLinkedFilesDir(cache.ProjectId.ProjectFolder);
			EnsureValidLinkedFilesFolderCore(linkedFilesFolder, defaultFolder);

			if (Directory.Exists(linkedFilesFolder))
			{
				return;
			}
			MessageBox.Show(string.Format(Properties.Resources.ksInvalidLinkedFilesFolder, linkedFilesFolder), Properties.Resources.ksErrorCaption);
				using (var folderBrowserDlg = new FolderBrowserDialogAdapter())
				{
					folderBrowserDlg.Description = Properties.Resources.ksLinkedFilesFolder;
					folderBrowserDlg.RootFolder = Environment.SpecialFolder.Desktop;
					folderBrowserDlg.SelectedPath = Directory.Exists(defaultFolder) ? defaultFolder : cache.ProjectId.ProjectFolder;
					if (folderBrowserDlg.ShowDialog() == DialogResult.OK)
				{
						linkedFilesFolder = folderBrowserDlg.SelectedPath;
				}
					else
					{
						FileUtils.EnsureDirectoryExists(defaultFolder);
						linkedFilesFolder = defaultFolder;
					}
				}
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor, () => { cache.LangProject.LinkedFilesRootDir = linkedFilesFolder; });
		}

		/// <summary>
		/// Create the specified Linked Files directory only if it's the default Linked Files directory. See FWNX-1092, LT-14491.
		/// </summary>
		/// <remarks>This is only internal, because tests use it.</remarks>
		internal static void EnsureValidLinkedFilesFolderCore(string linkedFilesFolder, string defaultLinkedFilesFolder)
		{
			if (linkedFilesFolder == defaultLinkedFilesFolder)
			{
				FileUtils.EnsureDirectoryExists(defaultLinkedFilesFolder);
		}
		}

		/// <summary>
		/// When non-trivial (user-visible) changes are saved for a project, we want to record
		/// that as the most recent interesting project to open for the current main window's app.
		/// </summary>
		private static void FieldWorks_OnSave(object sender, SaveEventArgs e)
		{
			if (!e.UndoableChanges)
			{
				return;
			}
			var settings = s_settingsForLastClosedWindow;
			if (settings == null)
			{
				var activeWnd = s_activeMainWnd ?? Form.ActiveForm as IFwMainWnd;
				if (activeWnd == null || s_flexApp?.RegistrySettings == null)
				{
					return;
				}
				Debug.Assert(activeWnd.Cache == e.Cache && e.Cache == Cache);
				settings = s_flexApp.RegistrySettings;
			}

			// We recently closed a window of this application; record it as having recently-saved changes
			// for this project.
			settings.LatestProject = e.Cache.ProjectId.Handle;
		}

		/// <summary>
		/// Commits the and disposes the LcmCache. This is usually called on a separate thread.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters passed in to the caller.</param>
		/// <returns>Always <c>null</c></returns>
		private static object CommitAndDisposeCache(IThreadedProgress progressDlg, object[] parameters)
		{
			progressDlg.Message = ResourceHelper.GetResourceString("kstidShutdownSaveMessage");
			//ENHANCE: if (about to restore and not doing a backup of existing project first) then
			// we improve efficiency by skipping the step of saving the data
			// Save any changes that have happened since the last commit on the cache
			try
			{
				Cache.ServiceLocator.GetInstance<IUndoStackManager>().StopSaveTimer();
				Cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
				if (s_doingRename)
				{
					progressDlg.Message = Properties.Resources.kstidRenamingProject;
					// Give the disk and system time to update. For some reason this is
					// needed after doing a save.
					Thread.Sleep(2000);
					s_renameSuccessful = Cache.RenameDatabase(s_renameNewName);
			}
			}
			finally
			{
				// Even if an exception is thrown during saving, we still want to dispose of
				// the cache (we'll probably be disposing it later anyways). (FWR-3179)
				Cache.Dispose();
				Cache = null; // Don't try to use it again
			}
			return null;
		}
		#endregion

		#region Top-level exception handling
		/// <summary>
		/// Sets the exception handler.
		/// </summary>
		private static void SetGlobalExceptionHandler()
		{
			// Set exception handler. Needs to be done before we create splash screen
			// (don't understand why, but otherwise some exceptions don't get caught)
			// Using Application.ThreadException rather than
			// AppDomain.CurrentDomain.UnhandledException has the advantage that the program
			// doesn't necessarily ends - we can ignore the exception and continue.
			Application.ThreadException += HandleTopLevelError;

			// we also want to catch the UnhandledExceptions for all the cases that
			// ThreadException don't catch, e.g. in the startup.
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
		}

		/// <summary>
		/// Catches and displays otherwise unhandled exception, especially those that happen
		/// during startup of the application before we show our main window.
		/// </summary>
		private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception)
			{
				DisplayError(e.ExceptionObject as Exception, e.IsTerminating);
			}
			else
			{
				DisplayError(new ApplicationException($"Got unknown exception: {e.ExceptionObject}"), false);
			}
		}

		/// <summary>
		/// Catches and displays a otherwise unhandled exception.
		/// </summary>
		private static void HandleTopLevelError(object sender, ThreadExceptionEventArgs eventArgs)
		{
			if (FwUtils.IsUnsupportedCultureException(eventArgs.Exception)) // LT-8248
			{
				Logger.WriteEvent("Unsupported culture: " + eventArgs.Exception.Message);
				return;
			}

			// If we can't recover the connection, we want to 'handle' it at this high level by exiting without
			// displaying a message.
			if (DisplayError(eventArgs.Exception, false))
			{
				FwUtils.InCrashedState = true;
				Application.Exit();

				// just to be sure
				Thread.Sleep(5000); // 5s
				using (var process = Process.GetCurrentProcess())
				{
					process.Kill();
			}
		}
		}

		/// <summary>
		/// Shows the error message of the exception to the user.
		/// </summary>
		/// <param name="exception"></param>
		/// <param name="fTerminating"><c>true</c> if the application already knows that it is
		/// terminating, otherwise <c>false</c>.</param>
		/// <returns><c>true</c> to exit application, <c>false</c> to continue</returns>
		private static bool DisplayError(Exception exception, bool fTerminating)
		{
			if (!ThreadHelper.InvokeRequired)
			{
				return DisplayError(exception, s_activeMainWnd);
			}

			ThreadHelper.Invoke(!fTerminating, () => DisplayError(exception, s_activeMainWnd));
				// We got called from a different thread, maybe the Finalizer thread. Anyways,
				// it's never ok to exit the app in this case so we return fTerminating.
				return fTerminating;
			}

		/// <summary>
		/// Displays the error.
		/// </summary>
		/// <param name="exception">The exception.</param>
		/// <param name="parent">The parent.</param>
		/// <returns><c>true</c> to exit application, <c>false</c> to continue</returns>
		private static bool DisplayError(Exception exception, IFwMainWnd parent)
		{
			try
			{
				// To disable displaying a message box, put
				// <add key="ShowUI" value="False"/>
				// in the <appSettings> section of the .config file (see MSDN for details).
				if (ShowUI)
				{
					var fIsLethal = !(exception is FwConfigurationException || exception is ContinuableErrorException || exception.InnerException is ContinuableErrorException);
					if (!SafelyReportException(exception, parent, fIsLethal))
					{
						return false;
					}
						// User chose to exit the application. Make sure that the program can be
						// properly shut down after displaying the exception. (FWR-3179)
						ResetStateForForcedShutdown();
						return true;
					}

				// Make sure that the program can be properly shut down after displaying the exception. (FWR-3179)
				ResetStateForForcedShutdown();

				if (exception is ExternalException && (uint)(((ExternalException)exception).ErrorCode) == 0x8007000E) // E_OUTOFMEMORY
				{
					Trace.Assert(false, ResourceHelper.GetResourceString("kstidMiscError"), ResourceHelper.GetResourceString("kstidOutOfMemory"));
					return true;
				}

				Debug.Assert(exception.Message != string.Empty || exception is COMException, "Oops - we got an empty exception description. Change the code to handle that!");

				var innerE = ExceptionHelper.GetInnerMostException(exception);
				var strMessage = ResourceHelper.GetResourceString("kstidProgError") + ResourceHelper.GetResourceString("kstidFatalError");

				var strReport = string.Format(ResourceHelper.GetResourceString("kstidGotException"),
					SupportEmail, exception.Source, Version,
					ExceptionHelper.GetAllExceptionMessages(exception), innerE.Source,
					innerE.TargetSite.Name, ExceptionHelper.GetAllStackTraces(exception));
				Trace.Assert(false, strMessage, strReport);
			}
			catch
			{
				// we ignore any exceptions that might happen during reporting this error
			}
			return true;
		}

		/// <summary>
		/// FieldWorks version
		/// </summary>
		private static string Version
		{
			get
			{
				var assembly = Assembly.GetEntryAssembly();
				var attributes = (assembly == null) ? null : assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				return attributes != null && attributes.Length > 0 ? ((AssemblyFileVersionAttribute) attributes[0]).Version : Application.ProductVersion;
			}
		}

		/// <summary>
		/// Resets the state of FieldWorks to allow a forced shutdown. Should only be called
		/// when a lethal unhandled exception is thrown.
		/// </summary>
		private static void ResetStateForForcedShutdown()
		{
			s_applicationExiting = true;
			s_allowFinalShutdown = true;
			InAppServerMode = false;
		}

		/// <summary>
		/// Report an exception 'safely'. That is, minimise the chance that some exception is
		/// going to be thrown during the report, which will throw us right out of the program
		/// without the chance to copy information about the original error.
		/// One way we do this is to stop all the mediators we can find from processing messages.
		/// </summary>
		/// <returns>True if the exception was lethal and the user chose to exit,
		/// false otherise</returns>
		private static bool SafelyReportException(Exception error, IFwMainWnd parent, bool isLethal)
		{
				// Be very, very careful about changing stuff here. Code here MUST not throw exceptions,
				// even when the application is in a crashed state. For example, error reporting failed
				// before I added the static registry keys, because getting App.SettingsKey failed somehow.
				var appKey = FwRegistryHelper.FieldWorksRegistryKey;
			if (parent != null  && s_flexApp != null)
			{
					appKey = s_flexAppKey;
			}
				return ErrorReporter.ReportException(error, appKey, SupportEmail, parent as Form, isLethal);
			}

		/// <summary>
		/// Gets the setting for displaying error message boxes. The value is retrieved from
		/// the .config file.
		/// </summary>
		/// <remarks>
		/// To disable displaying an error message box, put
		/// <code>&lt;add key="ShowUI" value="False"/></code>
		/// in the &lt;appSettings> section of the .config file (see MSDN for details).
		/// </remarks>
		private static bool ShowUI
		{
			get
			{
				try
				{
					var sShowUI = System.Configuration.ConfigurationManager.AppSettings["ShowUI"];
					if (!string.IsNullOrEmpty(sShowUI))
					{
						return Convert.ToBoolean(sShowUI);
				}
				}
				catch
				{
					// This is only used when bringing up the error dialog. We don't want to bother
					// with this exception since we have no idea what state the application is in.
				}
				return true;
			}
		}
		#endregion

		#region Splash screen
		/// <summary>
		/// Displays the splash screen
		/// </summary>
		private static void ShowSplashScreen()
		{
			s_splashScreen = new FwSplashScreen
			{
				ProductExecutableAssembly = Assembly.GetExecutingAssembly()
			};
			s_splashScreen.Show(!FwRegistrySettings.DisableSplashScreenSetting, s_noUserInterface);
			s_splashScreen.Refresh();
		}

		/// <summary>
		/// Closes the splash screen
		/// </summary>
		private static void CloseSplashScreen()
		{
			if (s_splashScreen == null)
			{
				return;
			}
				s_splashScreen.Close();
				s_splashScreen.Dispose();
				s_splashScreen = null;
			}

		/// <summary>
		/// Write to the splash screen
		/// </summary>
		/// <param name="msg">Text to display</param>
		private static void WriteSplashScreen(string msg)
		{
			if (s_splashScreen == null)
			{
				return;
			}
				// Set the splash screen message
				s_splashScreen.Message = msg;
				s_splashScreen.Refresh();
			}

		#endregion

		#region Internal Project Handling Methods
		/// <summary>
		/// Determines the project that will be run by reading the command-line parameters.
		/// If no project is found on the command-line parameters, then the Welcome to
		/// FieldWorks dialog is displayed and the user can choose a project from there.
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <returns>The project to run, or null if no project could be determined</returns>
		private static ProjectId DetermineProject(FwAppArgs args)
		{
			// Get project information from one of four places, in this order of preference:
			// 1. Command-line arguments
			// 2. Sample DB (if this is the first time this app has been run)
			// 3. Registry (if last startup was successful)
			// 4. Ask the user
			//
			// Except that with the new Welcome dialog, 2 through 4 are lumped into the Welcome dialog
			// functionality. If the user checks the "...always open the last edited project..." checkbox,
			// we will try to do that and only show the dialog if we fail.
			// If we try to use command-line arguments and it fails, we will use the Welcome dialog
			// to help the user figure out what to do next.
			var projId = new ProjectId(args.DatabaseType, args.Database);
			StartupException projectOpenError;
			if (TryCommandLineOption(projId, out projectOpenError))
			{
				return projId;
			}

			// If this app hasn't been run before, ask user about opening sample DB.
			var app = GetOrCreateApplication(args);
			if (app.RegistrySettings.FirstTimeAppHasBeenRun)
			{
				return ShowWelcomeDialog(args, app, null, projectOpenError);
			}

			// Valid project information was not passed on the command-line, so try looking in
			// the registry for the last-run project.
			var previousStartupStatus = GetPreviousStartupStatus(app);
			var latestProject = app.RegistrySettings.LatestProject;
			if ((string.IsNullOrEmpty(projId.Name) || projectOpenError != null) && previousStartupStatus != StartupStatus.Failed && !string.IsNullOrEmpty(latestProject))
			{
				// User didn't specify a project or gave bad command-line args,
				// so set projId to the last successfully opened project.
				projId = GetBestGuessProjectId(latestProject);
			}
			else if (previousStartupStatus == StartupStatus.Failed && !string.IsNullOrEmpty(latestProject))
			{
				// The previous project failed to open, so notify the user.
				projectOpenError = new StartupException(string.Format(Properties.Resources.kstidUnableToOpenLastProject, "FLEx", latestProject));
			}

			var fOpenLastEditedProject = GetAutoOpenRegistrySetting(app);

			if (fOpenLastEditedProject && projId.IsValid && projectOpenError == null &&
			    previousStartupStatus == StartupStatus.Successful)
			{
				return projId;
			}

			// No valid command line args, not the first time we've run the program,
			// and we aren't set to auto-open the last project, so give user options to open/create a project.
			return ShowWelcomeDialog(args, app, projId, projectOpenError);
		}

		private static bool GetAutoOpenRegistrySetting(IFlexApp app)
		{
			Debug.Assert(app != null);
			return app.RegistrySettings.AutoOpenLastEditedProject;
		}

		private static ProjectId GetBestGuessProjectId(string latestProject)
		{
			// From the provided project, return the best possible ProjectId object.
			return new ProjectId(latestProject);
		}

		/// <summary>
		/// Returns true if valid command-line args created this projectId.
		/// Returns false with no exception if no -db arg was given.
		/// Returns false with exception if invalid args were given.
		/// </summary>
		private static bool TryCommandLineOption(ProjectId projId, out StartupException exception)
		{
			exception = null;
			if (string.IsNullOrEmpty(projId.Name))
			{
				return false;
			}
			var ex = projId.GetExceptionIfInvalid();
			if (ex is StartupException)
			{
				exception = (StartupException)ex;
				return false; // Invalid command-line arguments supplied.
			}
			if (ex == null)
			{
				return true; // If valid command-line arguments are supplied, we go with that.
			}
			throw ex; // Something totally unexpected happened, don't suppress it.
		}

		/// <summary>
		/// Attempts to launch the specified project on the specified application..
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <param name="projectId">The project id.</param>
		/// <returns>
		/// True if the project was launched successfully from this process, false
		/// if the project could not be loaded or if control was passed to another FieldWorks
		/// process.
		/// </returns>
		private static bool LaunchProject(FwAppArgs args, ref ProjectId projectId)
		{
			while (true)
			{
				if (projectId == null)
				{
					Logger.WriteEvent("User has decided to quit");
					return false;
				}
				Logger.WriteEvent("User requested project " + projectId.UiName + " for BEP " + projectId.Type);

				// Look to see if another FieldWorks process is already running that is for
				// the requested project. If so, then transfer the request to that process.
				if (TryFindExistingProcess(projectId, args))
				{
					Logger.WriteEvent("Found FieldWorks.exe for project " + projectId.UiName + " for BEP " + projectId.Type);
					return false; // Found another process for this project, so we're done.
				}

				// Now that we know what project to load and it is openable. Start a new
				// log file for that project.
				Logger.WriteEvent("Transferring log to project log file " + projectId.UiName);
				Logger.Init(projectId.UiName);

				var app = GetOrCreateApplication(args);
				try
				{
					return InitializeFirstApp(app, projectId);
				}
				catch (StartupException e)
				{
					if (Cache != null)
					{
						Cache.Dispose();
						Cache = null;
					}
					Logger.Init(FwUtils.ksSuiteName);
					projectId = ShowWelcomeDialog(args, app, projectId, e);
				}
			}
		}

		/// <summary>
		/// Opens the specified (newly-created) project. This may start a new FieldWorks.exe
		/// process to handle the project if no FieldWorks processes are running for the
		/// specified project.
		/// </summary>
		/// <param name="projectId">The project id.</param>
		internal static bool OpenNewProject(ProjectId projectId)
		{
			Guard.AgainstNull(projectId, nameof(projectId));
			Debug.Assert(!projectId.Equals(s_projectId));

			return OpenProjectWithNewProcess(projectId);
		}

		/// <summary>
		/// Opens the specified project. This may start a new FieldWorks.exe process to handle
		/// the project if no FieldWorks processes are running for the specified project.
		/// </summary>
		/// <param name="projectId">The project id.</param>
		/// <returns>True if successful, false otherwise</returns>
		internal static bool OpenExistingProject(ProjectId projectId)
		{
			Guard.AgainstNull(projectId, nameof(projectId));

			if (projectId.Equals(s_projectId))
			{
				// We're trying to open this same project. Just open a new window for the
				// specified application
				return CreateAndInitNewMainWindow(s_activeMainWnd, false);
			}

			if (TryFindExistingProcess(projectId, new FwAppArgs(projectId.Handle, null, Guid.Empty)))
			{
				Logger.WriteEvent("Found existing FieldWorks.exe for project " + projectId.UiName + ". BEP:" + projectId.Type);
				return true; // Found another process for this project, so we're done.
			}

			return OpenProjectWithNewProcess(projectId);
		}

		/// <summary>
		/// Opens the specified project by starting a new FieldWorks.exe process.
		/// </summary>
		/// <param name="project">The project ID.</param>
		/// <param name="otherArgs">Other command-line arguments to pass to the new FieldWorks
		/// process.</param>
		/// <returns>True if the project was opened, false otherwise</returns>
		private static bool OpenProjectWithNewProcess(ProjectId project, params string[] otherArgs)
		{
			return OpenProjectWithNewProcess(project.Handle, otherArgs);
		}

		/// <summary>
		/// Opens the specified project by starting a new FieldWorks.exe process.
		/// </summary>
		/// <param name="projectName">The name of the project.</param>
		/// <param name="otherArgs">Other command-line arguments to pass to the new FieldWorks
		/// process.</param>
		/// <returns>True if the project was opened, false otherwise</returns>
		internal static bool OpenProjectWithNewProcess(string projectName, params string[] otherArgs)
		{
			Logger.WriteEvent("Starting new FieldWorks.exe process for project " + projectName);
			var args = new List<string>
			{
				"-" + FwAppArgs.kProject,
				projectName
			};
			args.AddRange(otherArgs);
			return StartFwApp(args.ToArray());
		}

		/// <summary>
		/// Opens the specified project by starting a new FieldWorks.exe process.
		/// </summary>
		/// <param name="projectName">The name of the project.</param>
		/// <param name="otherArgs">Other command-line arguments to pass to the new FieldWorks
		/// process.</param>
		/// <returns>The new process if started, otherwise, null</returns>
		internal static Process OpenProjectWithRealNewProcess(string projectName, params string[] otherArgs)
		{
			Logger.WriteEvent("Starting new FieldWorks.exe process for project " + projectName);
			var args = new List<string>
			{
				"-" + FwAppArgs.kProject,
				projectName
			};
			args.AddRange(otherArgs);
			return StartFwAppAndGetProcess(args.ToArray());
		}

		/// <summary>
		/// Rename the database.
		/// </summary>
		/// <param name="dbNewName">new basename desired</param>
		/// <returns>True if the rename was successful, false otherwise</returns>
		internal static bool RenameProject(string dbNewName)
		{
			// TODO (FWR-722): Also move project-specific registry settings
			var projId = s_projectId;
			s_doingRename = true;
			s_renameSuccessful = false;
			s_renameNewName = dbNewName;
			try
			{
				// Although this code looks strange, the rename actually takes place after the
				// saving of the data (see CommitAndDisposeCache). The reason for this is that
				// closing all of the main windows causes the cache to be disposed (or will
				// during the next Windows message pump). We needed to have the rename happen
				// after all the main windows are closed, but before the cache is disposed.
				// The only semi-clean way of doing that is to do what we did. (FWR-3179)
				ExecuteWithAppsShutDown(() => s_projectId ?? projId);
			}
			finally
			{
				s_doingRename = false;
				s_renameNewName = null;
			}

			if (s_renameSuccessful)
			{
				s_flexApp.RegistrySettings.LatestProject = projId.Handle;
			}
			return s_renameSuccessful;
		}

		/// <summary>
		/// Handles a project name change.
		/// </summary>
		/// <param name="sender">The LCM cache (should be the same as our static one).</param>
		private static void ProjectNameChanged(LcmCache sender)
		{
			Debug.Assert(sender == Cache);
			// The ProjectId should have already been updated (as a result of the rename deep
			// in LCM because we pass the ProjectId by reference and it gets set in the BEP),
			// however, generally, we aren't guaranteed that this behavior actually takes place,
			// so we need to do it here to make sure our reference is updated.
			s_projectId.Path = Cache.ProjectId.Path;
			// Update the path in the writing system manager so that it won't crash trying to
			// write to a nonexistent folder.
			Cache.ServiceLocator.WritingSystemManager.LocalStoreFolder = Path.Combine(s_projectId.ProjectFolder, "WritingSystemStore");
		}
		#endregion

		#region Project UI handling methods
		/// <summary>
		/// Displays fieldworks welcome dialog.
		/// </summary>
		/// <param name="args">The command-line (probably) arguments used to launch FieldWorks</param>
		/// <param name="startingApp">The help topic provider and source of registry values.</param>
		/// <param name="lastProjectId">The project stored in the registry as the last edited project.</param>
		/// <param name="exception">Exception thrown if the previously requested project could not be opened.</param>
		/// <returns>
		/// A ProjectId object if an option has been chosen which results in a valid
		/// project being opened; <c>null</c> if the user chooses to exit.
		/// </returns>
		private static ProjectId ShowWelcomeDialog(FwAppArgs args, IFlexApp startingApp, ProjectId lastProjectId, StartupException exception)
		{
			CloseSplashScreen();

			var helpTopicProvider = startingApp as IHelpTopicProvider;

			// Use the last edited project as the base guess for which project we'll open.
			var projectToTry = lastProjectId;

			// Continue to ask for an option until one is selected.
			s_fWaitingForUserOrOtherFw = true;
			do
			{
				if (exception != null)
				{
					if (projectToTry != null)
					{
						Logger.WriteEvent("Problem opening " + projectToTry.UiName + ".");
					}
					Logger.WriteError(exception);
				}

				// Put this here (i.e. inside the do loop) so any exceptions
				// will be logged before terminating.
				if (s_noUserInterface)
				{
					return null;
				}

				// If we changed our projectToTry below and we're coming through again,
				// reset our projectId.
				projectToTry = lastProjectId;

				using (var dlg = new WelcomeToFieldWorksDlg(helpTopicProvider, exception, s_noPreviousReportingSettings))
				{
					if (exception != null)
					{
						dlg.ShowErrorLabelHideLink();
					}
					else
					{
						if (projectToTry != null && projectToTry.IsValid)
						{
							dlg.ProjectLinkUiName = projectToTry.Name;
							dlg.SetFirstOrLastProjectText(false);
						}
						else
						{
							var sampleProjId = GetSampleProjectId(startingApp);
							if (sampleProjId != null)
							{
								dlg.ProjectLinkUiName = sampleProjId.Name;
								dlg.SetFirstOrLastProjectText(true);
								// LT-13943 - forgot to set this variable, which made it not be able to open
								// the sample db.
								projectToTry = new ProjectId(startingApp.SampleDatabase);
							}
							else // user didn't install Sena 3!
							{
								projectToTry = null;
							}
						}
						if (projectToTry != null)
						{
							dlg.ShowLinkHideErrorLabel();
						}
						else
						{
							dlg.ShowErrorLabelHideLink();
					}
					}
					var gotAutoOpenSetting = false;
					if (startingApp.RegistrySettings != null) // may be null if disposed after canceled restore.
					{
					dlg.OpenLastProjectCheckboxIsChecked = GetAutoOpenRegistrySetting(startingApp);
						gotAutoOpenSetting = true;
					}
					dlg.StartPosition = FormStartPosition.CenterScreen;
					dlg.ShowDialog();
					exception = null;
					// We get the app each time through the loop because a failed Restore operation can dispose it.
					var app = GetOrCreateApplication(args);
					if (gotAutoOpenSetting)
					{
					app.RegistrySettings.AutoOpenLastEditedProject = dlg.OpenLastProjectCheckboxIsChecked;
					}
					switch (dlg.DlgResult)
					{
						case WelcomeToFieldWorksDlg.ButtonPress.New:
							projectToTry = CreateNewProject();
							Debug.Assert(projectToTry == null || projectToTry.IsValid);
							break;
						case WelcomeToFieldWorksDlg.ButtonPress.Open:
							projectToTry = ChooseLangProject();
							try
							{
								projectToTry?.AssertValid();
							}
							catch (StartupException e)
							{
								exception = e;
							}
							break;
						case WelcomeToFieldWorksDlg.ButtonPress.Link:
							// LT-13943 - this guard keeps the projectToTry from getting blasted by a null when it has
							// a useful projectId (like the initial sample db the first time FLEx is run).
							if (lastProjectId != null && !lastProjectId.Equals(projectToTry))
							{
								projectToTry = lastProjectId; // just making sure!
							}
							Debug.Assert(projectToTry.IsValid);
							break;
						case WelcomeToFieldWorksDlg.ButtonPress.Restore:
							s_allowFinalShutdown = false;
							RestoreProject(null, app);
							s_allowFinalShutdown = true;
							projectToTry = s_projectId; // Restore probably used this process
							break;
						case WelcomeToFieldWorksDlg.ButtonPress.Exit:
							return null; // Should cause the FW process to exit later
						case WelcomeToFieldWorksDlg.ButtonPress.Receive:
							if (!FwNewLangProject.CheckProjectDirectory(null, helpTopicProvider))
							{
								break;
							}
							ObtainedProjectType obtainedProjectType;
							projectToTry = null; // If the user cancels the send/receive, this null will result in a return to the welcome dialog.
							// Hard to say what Form.ActiveForm is here. The splash and welcome dlgs are both gone.
							var projectDataPathname = ObtainProjectMethod.ObtainProjectFromAnySource(Form.ActiveForm, helpTopicProvider, out obtainedProjectType);
							if (!string.IsNullOrEmpty(projectDataPathname))
							{
								projectToTry = new ProjectId(BackendProviderType.kXML, projectDataPathname);
								var activeWindow = startingApp.ActiveMainWindow;
								if (activeWindow != null)
								{
									var activeWindowInterface = (IFwMainWnd)activeWindow;
									activeWindowInterface.PropertyTable.SetProperty(CommonBridgeServices.LastBridgeUsed,
										obtainedProjectType == ObtainedProjectType.Lift ? CommonBridgeServices.LiftBridge : CommonBridgeServices.FLExBridge,
										SettingsGroup.LocalSettings,
										true,
										false);
								}
							}
							break;
						case WelcomeToFieldWorksDlg.ButtonPress.Import:
							projectToTry = CreateNewProject();
							if (projectToTry != null)
							{
							var projectLaunched = LaunchProject(args, ref projectToTry);
								if (projectLaunched)
							{
								s_projectId = projectToTry; // Window is open on this project, we must not try to initialize it again.
								var mainWindow = Form.ActiveForm;
									if (mainWindow is IFwMainWnd)
								{
										((IFwMainWnd)mainWindow).Publisher.Publish("SFMImport", null);
								}
								else
								{
									return null;
								}
							}
							else
							{
								return null;
							}
							}
							break;
					}
				}
			}
			while (projectToTry == null || !projectToTry.IsValid);

			Logger.WriteEvent("Project selected in Welcome dialog: " + projectToTry);

			s_fWaitingForUserOrOtherFw = false;
			return projectToTry;
		}

		private static ProjectId GetSampleProjectId(IFlexApp app)
		{
			ProjectId sampleProjId = null;
			if (app?.SampleDatabase != null)
			{
				sampleProjId = new ProjectId(app.SampleDatabase);
			}
			if (sampleProjId == null || !sampleProjId.IsValid)
			{
				return null;
			}
			return sampleProjId;
		}

		/// <summary>
		/// Lets the user select an existing language project.
		/// </summary>
		/// <returns>The chosen project, or null if no project was chosen</returns>
		internal static ProjectId ChooseLangProject()
		{
			if (!FwNewLangProject.CheckProjectDirectory(s_flexApp.ActiveMainWindow, s_flexApp))
			{
				return null;
			}
			using (var dlg = new ChooseLangProjectDialog(s_flexApp, false))
			{
				dlg.ShowDialog(s_flexApp.ActiveMainWindow);
				var app = s_flexApp as IApp;
				var activeWindow = app.ActiveMainWindow;
				if (activeWindow != null && dlg.ObtainedProjectType != ObtainedProjectType.None)
				{
					var activeWindowInterface = (IFwMainWnd)activeWindow;
					activeWindowInterface.PropertyTable.SetProperty(CommonBridgeServices.LastBridgeUsed,
						dlg.ObtainedProjectType == ObtainedProjectType.Lift ? CommonBridgeServices.LiftBridge : CommonBridgeServices.FLExBridge,
						SettingsGroup.LocalSettings,
						true, false);
				}

				if (dlg.DialogResult != DialogResult.OK)
				{
					return null;
				}
					var projId = new ProjectId(dlg.Project);
					if (IsSharedXmlBackendNeeded(projId))
				{
						projId.Type = BackendProviderType.kSharedXML;
				}
					return projId;
			}
		}

		/// <summary>
		/// Lets the user create a new project
		/// </summary>
		/// <returns>
		/// The project that was created (and needs to be loaded), or null if the user
		/// canceled.
		/// </returns>
		internal static ProjectId CreateNewProject()
		{
			using (var dlg = new FwNewLangProject())
			{
				dlg.SetDialogProperties(s_flexApp);
				switch (dlg.DisplayDialog(s_flexApp.ActiveMainWindow))
				{
					case DialogResult.OK:
						if (dlg.IsProjectNew)
						{
							return new ProjectId(dlg.GetDatabaseFile());
						}
						else
						{
							// The user tried to create a new project which already exists and
							// then choose to open the project. Therefore open the project and return
							// null for the ProjectId so the caller of this method does not try to
							// create a new project.
							var projectId = new ProjectId(dlg.GetDatabaseFile());
							OpenExistingProject(projectId);
							return null;
						}
					case DialogResult.Abort:
						// If we get an Abort it means that we got an exception in the dialog (e.g.
						// in the OnLoad method). We can't just catch that exception here (probably
						// because of the extra message loop the dialog has), so we close the dialog
						// and return Abort.
						MessageBox.Show(s_flexApp.ActiveMainWindow,
							ResourceHelper.GetResourceString("kstidNewProjError"),
							ResourceHelper.GetResourceString("kstidMiscError"));
						break;
				}
			}
			return null;
		}

		/// <summary>
		/// Lets the user delete any FW databases that are not currently open
		/// </summary>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		internal static void DeleteProject(Form dialogOwner, IHelpTopicProvider helpTopicProvider)
		{
			var projectsInUse = new HashSet<string>(ProjectsInUseLocally);
			using (var dlg = new FwDeleteProjectDlg(projectsInUse))
			{
				dlg.SetDialogProperties(helpTopicProvider);
				dlg.ShowDialog(dialogOwner);
			}
		}

		/// <summary>
		/// Backup the project.
		/// </summary>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// <returns>The path to the backup file, or <c>null</c></returns>
		internal static string BackupProject(Form dialogOwner)
		{
			using (var dlg = new BackupProjectDlg(Cache, s_flexApp))
			{
				if (dlg.ShowDialog(dialogOwner) == DialogResult.OK)
				{
					return dlg.BackupFilePath;
				}
			}
			return null;
		}

		/// <summary>
		/// Restore a project.
		/// </summary>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// <param name="backupFile">The file to restore from.</param>
		private static void RestoreProject(Form dialogOwner, string backupFile)
		{
			BackupFileSettings settings = null;
			if (!RestoreProjectDlg.HandleRestoreFileErrors(null, backupFile, () => settings = new BackupFileSettings(backupFile, true)))
			{
				return;
			}

			using (var dlg = new RestoreProjectDlg(settings, GetHelpTopicProvider()))
			{
				dlg.ShowInTaskbar = true;
				if (dlg.ShowDialog(dialogOwner) != DialogResult.OK)
				{
					return;
				}

				HandleRestoreRequest(dialogOwner, new FwRestoreProjectSettings(dlg.Settings));
			}
		}

		/// <summary>
		/// Restore a project.
		/// </summary>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// <param name="helpTopicProvider">The FieldWorks application's help topic provider.</param>
		internal static void RestoreProject(Form dialogOwner, IHelpTopicProvider helpTopicProvider)
		{
			var databaseName = (Cache != null) ? Cache.ProjectId.Name : string.Empty;
			using (var dlg = new RestoreProjectDlg(databaseName, helpTopicProvider))
			{
				if (dlg.ShowDialog(dialogOwner) != DialogResult.OK)
				{
					return;
				}

				HandleRestoreRequest(dialogOwner, new FwRestoreProjectSettings(dlg.Settings));
			}
		}
		#endregion

		#region Project location methods
		/// <summary>
		/// Displays the Project Location dialog box
		/// </summary>
		/// <param name="dialogOwner">The form that should be used as the dialog owner.</param>
		/// <param name="app">The FieldWorks application from with this command was initiated.
		/// </param>
		/// <param name="cache"></param>
		internal static void FileProjectLocation(Form dialogOwner, IApp app, LcmCache cache)
		{
			using (var dlg = new ProjectLocationDlg(app, cache))
			{
			if (dlg.ShowDialog(dialogOwner) != DialogResult.OK)
				{
				return;
				}
				var projectPath = cache.ProjectId.Path;
				var parentDirectory = Path.GetDirectoryName(cache.ProjectId.ProjectFolder);
				var projectsDirectory = FwDirectoryFinder.ProjectsDirectory;
				if (!MiscUtils.IsUnix)
				{
#if JASON_TO_REVIEW
					// REVIEW: These two modified paths are not actually used anywhere. Is that a problem?
#endif
					parentDirectory = parentDirectory.ToLowerInvariant();
					projectsDirectory = projectsDirectory.ToLowerInvariant();
				}

				UpdateProjectsLocation(dlg.ProjectsFolder, app, projectPath);
				}
			}

		/// <summary>
		/// Updates the projects default directory.
		/// </summary>
		/// <param name="newFolderForProjects">The new folder for projects.</param>
		/// <param name="app">used to get parent window of dialog</param>
		/// <param name="projectPath">path to the current project</param>
		private static void UpdateProjectsLocation(string newFolderForProjects, IApp app, string projectPath)
		{
			if (newFolderForProjects == null || newFolderForProjects == FwDirectoryFinder.ProjectsDirectory || !FileUtils.EnsureDirectoryExists(newFolderForProjects))
		{
				return;
			}

			bool fMoveFiles;
			using (var dlg = new MoveProjectsDlg(app))
			{
				fMoveFiles = dlg.ShowDialog(app.ActiveMainWindow) == DialogResult.Yes;
			}
			var oldFolderForProjects = FwDirectoryFinder.ProjectsDirectory;
			try
			{
				FwDirectoryFinder.ProjectsDirectory = newFolderForProjects;
			}
			catch (Exception)
			{
				MessageBox.Show(Form.ActiveForm, Properties.Resources.ksChangeProjectLocationFailedDetails, Properties.Resources.ksChangeProjectLocationFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return; // don't move files!!
			}
			if (!fMoveFiles)
			{
				return;
			}
				var oldProjectId = (ProjectId)Cache.ProjectId;
				ExecuteWithAllFwProcessesShutDown(() => MoveProjectFolders(oldFolderForProjects, newFolderForProjects, projectPath, oldProjectId));
			}

		/// <summary>
		/// Check whether we must copy the project folders and files because the new Projects
		/// folder is on a different device than the old Projects folder.
		/// </summary>
		private static bool MustCopyFoldersAndFiles(string oldFolderForProjects, string newFolderForProjects)
		{
			var driveMounts = GetDriveMountList();
			var oldPath = oldFolderForProjects;
			if (!Path.IsPathRooted(oldPath))
			{
				try   { oldPath = Path.GetFullPath(oldPath); }
				catch { return true; }		// better safe than sorry if we can't tell the drives
			}
			var newPath = newFolderForProjects;
			if (!Path.IsPathRooted(newPath))
			{
				try   { newPath =  Path.GetFullPath(newPath); }
				catch { return true; }
			}
			string oldRoot = null;
			string newRoot = null;
			if (!MiscUtils.IsUnix)
			{
				oldPath = oldPath.ToLowerInvariant();
				newPath = newPath.ToLowerInvariant();
			}
			foreach (var mount in driveMounts)
			{
				if (oldRoot == null && oldPath.StartsWith(mount))
				{
					oldRoot = mount;
				}
				if (newRoot == null && newPath.StartsWith(mount))
				{
					newRoot = mount;
				}
				if (oldRoot != null && newRoot != null)
				{
					return oldRoot != newRoot;
			}
			}
			return true;	// shouldn't ever get here, but be safe if we do.
		}

		private static List<string> GetDriveMountList()
		{
			// TODO-Linux: GetDrives() on Mono is only implemented for Linux.
			var allDrives = DriveInfo.GetDrives();
			var driveMounts = new List<string>();
			foreach (var driveInfo in allDrives)
			{
				// TODO-Linux: IsReady always returns true on Mono
				if (!driveInfo.IsReady || driveInfo.AvailableFreeSpace == 0)
				{
					continue;
				}
				switch (driveInfo.DriveType)
				{
					case DriveType.Fixed:
					case DriveType.Network:
					case DriveType.Removable:
						if (MiscUtils.IsUnix)
						{
							driveMounts.Add(driveInfo.Name + (driveInfo.Name.EndsWith("/") ? "" : "/"));	// ensure terminated with a slash
						}
						else
						{
							driveMounts.Add(driveInfo.Name.ToLowerInvariant());		// Windows produces C:\ D:\ etc.
						}
						break;
				}
			}
			driveMounts.Sort(longestFirst);
			return driveMounts;
		}

		/// <summary>
		/// Compare the strings first by length (longest first), then normally if the lengths
		/// are equal.
		/// </summary>
		private static int longestFirst(string a, string b)
		{
			if (a == null)
			{
				a = string.Empty;
			}
			if (b == null)
			{
				b = string.Empty;
			}
			if (a.Length > b.Length)
			{
				return -1;
			}
			return a.Length < b.Length ? 1 : a.CompareTo(b);
		}

		/// <summary>
		/// Moves all of the projects in the specified old project folder (including any
		/// subfolders) to the specified new project folder.
		/// </summary>
		/// <param name="oldFolderForProjects">The old folder that held the projects.</param>
		/// <param name="newFolderForProjects">The new folder that will hold the projects.</param>
		/// <param name="projectPath">The project path.</param>
		/// <param name="oldProjectId">The Id of the old project (to use if it has not moved)</param>
		/// <returns>The ProjectId for the project to load after the move is completed</returns>
		private static ProjectId MoveProjectFolders(string oldFolderForProjects, string newFolderForProjects, string projectPath, ProjectId oldProjectId)
		{
			var rgErrors = new List<string>();
			var fCopy = MustCopyFoldersAndFiles(oldFolderForProjects, newFolderForProjects);
			using (var progressDlg = new ProgressDialogWithTask(ThreadHelper))
			{
				var subDirs = Directory.GetDirectories(oldFolderForProjects);
				progressDlg.Maximum = subDirs.Length;
				progressDlg.AllowCancel = false;
				progressDlg.Title = string.Format(Properties.Resources.ksMovingProjectsCaption, oldFolderForProjects, newFolderForProjects);
				// Move all of the files and folders
				progressDlg.RunTask(true, (progressDialog, dummy) =>
				{
					foreach (var subdir in subDirs)
					{
						try
						{
							var sub = Path.GetFileName(subdir);
							// If the project folder is not known to be ours don't move the folder.
							// This is some protection against moving vital folders if the user does something silly like
							// making C:\ the project folder. See FWR-3371.
							var destDirName = Path.Combine(newFolderForProjects, sub);
							if (!IsFieldWorksProjectFolder(subdir))
							{
								// It might still be a folder which is the name of a remote server,
								// and contains local settings for projects on that server.
								// Check each of its subfolders, and move/copy the ones that appear to be settings, if any.
								var movedSomething = false;
								foreach (var subsubdir in Directory.GetDirectories(subdir))
								{
									if (!IsFieldWorksSettingsFolder(subsubdir))
									{
										continue;
									}
									// We found a project folder one level down. This ought not so to be.
									// Maybe we are doing some bizarre move into one of our own subfolders, and this
									// was already moved earlier in the main loop? Anyway don't move it, it's not just a settings folder.
									if (IsFieldWorksProjectFolder(subsubdir))
									{
										continue;
									}
									movedSomething = true;
									var leafName = Path.GetFileName(subsubdir);
									var subDirDest = Path.Combine(destDirName, leafName);
									Directory.CreateDirectory(destDirName); // may be redundant (will be after first time) but cheap.
									if (fCopy)
									{
										CopyDirectory(subsubdir, subDirDest);
										Directory.Delete(subsubdir, true);
									}
									else
									{
										Directory.Move(subsubdir, subDirDest);
									}
								}
								// If we moved something and left nothing behind delete the parent folder.
								// (We do a fresh GetDirectories just in case bizarrely we are moving INTO one of our own subfolders,
								// and thus even though we moved everything something is still there).
								if (movedSomething && Directory.GetDirectories(subdir).Length == 0)
								{
									Directory.Delete(subdir);
								}
								continue; // with more top-level directories in the projects folder.
							}
							progressDialog.Message = string.Format(Properties.Resources.ksMovingProject, sub);
							progressDialog.Step(1);
							if (fCopy)
							{
								// Recursively copy each subfolder.
								CopyDirectory(subdir, destDirName);
								Directory.Delete(subdir, true);
							}
							else
							{
								Directory.Move(subdir, destDirName);
							}
						}
						catch (Exception e)
						{
							rgErrors.Add($"{Path.GetFileNameWithoutExtension(subdir)} - {e.Message}");
						}
					}
					return null;
				});
			}
			if (rgErrors.Count > 0)
			{
				// Show the user any errors that occured while doing the move
				var bldr = new StringBuilder();
				bldr.AppendLine(Properties.Resources.ksCannotMoveProjects);
				foreach (var err in rgErrors)
				{
					bldr.AppendLine(err);
				}
				bldr.Append(Properties.Resources.ksYouCanTryToMoveProjects);
				MessageBox.Show(bldr.ToString(), Properties.Resources.ksProblemsMovingProjects);
			}
			if (MiscUtils.IsUnix)
			{
				if (!projectPath.StartsWith(oldFolderForProjects))
				{
					return oldProjectId;
				}
					// This is perhaps a temporary workaround.  On Linux, FwDirectoryFinder.ProjectsDirectory
					// isn't returning the updated value, but rather the original value.  This seems to
					// last for the duration of the program, but if you exit and restart the program, it
					// gets the correct (updated) value!?
					// (On the other hand, I somewhat prefer this code which is fairly straightforward and
				// obvious to depending on calling some static method hidden in the depths of LCM.)
				var projFileName = Path.GetFileName(projectPath);
				var projName = Path.GetFileNameWithoutExtension(projectPath);
				var path = Path.Combine(Path.Combine(newFolderForProjects, projName), projFileName);
					return new ProjectId(path);
				}
			return projectPath.StartsWith(oldFolderForProjects, StringComparison.InvariantCultureIgnoreCase) ? new ProjectId(projectPath) : oldProjectId;
		}

		/// <summary>
		/// Return true if a folder belongs to FieldWorks, that is, it is one that we recognize has
		/// having a proper place in the Projects folder.
		/// </summary>
		private static bool IsFieldWorksProjectFolder(string projectFolder)
		{
			// If it contains a matching fwdata file it is a project folder.
			return File.Exists(Path.ChangeExtension(Path.Combine(projectFolder, Path.GetFileName(projectFolder)), LcmFileHelper.ksFwDataXmlFileExtension));
		}

		private static bool IsFieldWorksSettingsFolder(string projectFolder)
		{
			return Directory.Exists(Path.Combine(projectFolder, LcmFileHelper.ksConfigurationSettingsDir));
		}

		/// <summary>
		/// Copies all files and folders in the specified old directory to the specified new
		/// directory
		/// </summary>
		/// <param name="oldDir">The old directory.</param>
		/// <param name="newDir">The new directory.</param>
		private static void CopyDirectory(string oldDir, string newDir)
		{
			if (!Directory.Exists(newDir))
			{
				Directory.CreateDirectory(newDir);
			}
			foreach (var filePath in Directory.GetFiles(oldDir))
			{
				File.Copy(filePath, Path.Combine(newDir, Path.GetFileName(filePath)), true);
			}
			foreach (var subdir in Directory.GetDirectories(oldDir))
			{
				CopyDirectory(subdir, Path.Combine(newDir, Path.GetFileName(subdir)));
				// Don't need to worry about deleting here, it will happen in original caller.
			}
		}
#endregion

#region Project Migration Methods
		/// <summary>
		/// Migrates the user's databases to FieldWorks 7.0+ if they haven't yet migrated
		/// successfully (and the user actually wants to migrate).
		/// </summary>
		/// <returns><c>True</c> if a migration was needed (i.e. the registry value is still
		/// set, <c>false</c> otherwise.</returns>
		private static bool MigrateProjectsTo70()
		{
			var regValue = (string)FwRegistryHelper.FieldWorksRegistryKey.GetValue("MigrationTo7Needed", bool.FalseString);
			if (!(regValue != null && bool.Parse(regValue)))
			{
				return false;
			}

			var res = MessageBox.Show(Properties.Resources.ksDoYouWantToMigrate, Properties.Resources.ksMigrateProjects, MessageBoxButtons.YesNo);

			// See FWR-3767 for details when this line only occurred if the migrate process completed without error.
			FwRegistryHelper.FieldWorksRegistryKey.DeleteValue("MigrationTo7Needed");

			if (res != DialogResult.Yes)
			{
				return true;
			}
				try
				{
					// TODO (TimS): We should probably put FW into single process mode for these
					// migrations. It would probably be very bad to have two processes attempting to
					// do migrations at the same time.
				var info = new ProcessStartInfo(FwDirectoryFinder.MigrateSqlDbsExe)
				{
					UseShellExecute = false
				};
				using (var proc = Process.Start(info))
					{
						proc.WaitForExit();
						if (proc.ExitCode < 0)
					{
							throw new Exception(Properties.Resources.ksMigratingProjectsFailed);
					}
						if (proc.ExitCode > 0)
					{
						throw new Exception(string.Format(Properties.Resources.ksProjectsFailedToMigrate, proc.ExitCode));
					}
					}
				}
				catch (Exception ex)
				{
				MessageBox.Show(Properties.Resources.ksErrorMigratingProjects + Environment.NewLine + ex.Message);
			}
			return true;
		}
#endregion

#region Backup/Restore-related methods
		/// <summary>
		/// Handles a request to restore a project. This method is thread safe.
		/// </summary>
		/// <param name="restoreSettings">The restore arguments.</param>
		internal static void HandleRestoreRequest(FwRestoreProjectSettings restoreSettings)
		{
			HandleRestoreRequest(s_activeMainWnd as Form, restoreSettings);
		}

		/// <summary>
		/// Handles a request to restore a project. This method is thread safe.
		/// </summary>
		/// <param name="dialogOwner">A form that can be used as an owner for progress dialog/
		/// message box.</param>
		/// <param name="restoreSettings">The restore arguments.</param>
		internal static void HandleRestoreRequest(Form dialogOwner, FwRestoreProjectSettings restoreSettings)
		{
			ThreadHelper.Invoke(() =>
			{
				// Determine if we need to start a new process for the restore
				if (s_projectId != null && s_projectId.IsSameLocalProject(new ProjectId(restoreSettings.Settings.FullProjectPath)))
				{
					// We need to invoke so that the mediator that processed the restore menu item
					// can be safely disposed of (and everything that it holds on to can be released).
					// If we don't do this, the memory held in the IdentityMap will stay around because
					// of remaining references.
					ThreadHelper.InvokeAsync(RestoreCurrentProject, restoreSettings, dialogOwner);
				}
				else if (!TryFindRestoreHandler(restoreSettings))
				{
					if (s_projectId == null)
					{
						// No other FieldWorks process was running that could handle the request.
						// However, we don't know what project we are opening yet, so just use the
						// restored project as our project. (this happens if restoring from the
						// command-line)
						RestoreCurrentProject(restoreSettings, dialogOwner);
					}
					else
					{
						// No other FieldWorks process was running that could handle the request, so
						// start a brand new process for the project.
						// Since we know that no other process are running on this project, we can
						// safely do a backup using a new cache. (FWR-3344)
						if (restoreSettings.Settings.BackupOfExistingProjectRequested && !BackupProjectForRestore(restoreSettings, null, dialogOwner))
						{
							return;
						}

						var settings = restoreSettings.Settings;
						OpenProjectWithNewProcess(settings.ProjectName, "-" + FwAppArgs.kRestoreFile, settings.Backup.File, "-" + FwAppArgs.kRestoreOptions, settings.CommandLineOptions);
					}
				}
			});
		}

		/// <summary>
		/// Restores from the backup specified in restoreSettings, replacing the current version
		/// of this project (requires restarting apps and the cache).
		/// </summary>
		/// <param name="restoreSettings">The restore settings.</param>
		/// <param name="dialogOwner">A form that can be used as an owner for progress dialog/
		/// message box.</param>
		private static void RestoreCurrentProject(FwRestoreProjectSettings restoreSettings, Form dialogOwner)
		{
			// When we get here we can safely do the backup of the project because we either
			// have no cache (and no other process has this project open), or we are the
			// process that has this project open. (FWR-3344)
			if (restoreSettings.Settings.BackupOfExistingProjectRequested && !BackupProjectForRestore(restoreSettings, Cache, dialogOwner))
			{
				return;
			}

			ExecuteWithAppsShutDown(() =>
			{
				bool retry;
				do
				{
					retry = false;
					try
					{
						var restoreService = new ProjectRestoreService(restoreSettings.Settings, s_ui, FwDirectoryFinder.ConverterConsoleExe, FwDirectoryFinder.DbExe);
						Logger.WriteEvent("Restoring from " + restoreSettings.Settings.Backup.File);
						if (RestoreProjectDlg.HandleRestoreFileErrors(null, restoreSettings.Settings.Backup.File, () => DoRestore(restoreService)))
						{
							s_LinkDirChangedTo = restoreService.LinkDirChangedTo;
							return s_projectId ?? new ProjectId(restoreSettings.Settings.FullProjectPath);
						}
					}
					catch (CannotConvertException e)
					{
						MessageBoxUtils.Show(e.Message, ResourceHelper.GetResourceString("ksRestoreFailed"));
					}
					catch (MissingOldFwException e)
					{
						using (var dlg = new MissingOldFieldWorksDlg(restoreSettings.Settings,
							e.HaveOldFieldWorks, e.HaveFwSqlServer))
						{
							retry = (dlg.ShowDialog() == DialogResult.Retry);
						}
					}
					catch (FailedFwRestoreException e)
					{
						MessageBoxUtils.Show(Properties.Resources.ksRestoringOldFwBackupFailed, Properties.Resources.ksFailed);
					}
				}
				while (retry);

				return null;
			});
		}

		/// <summary>
		/// Does the restore.
		/// </summary>
		/// <param name="restoreService">The restore service.</param>
		private static void DoRestore(ProjectRestoreService restoreService)
		{
			using (var progressDlg = new ProgressDialogWithTask(ThreadHelper))
			{
				restoreService.RestoreProject(progressDlg);
		}
		}

		/// <summary>
		/// Backs up the project that is about to be restored.
		/// </summary>
		/// <param name="restoreSettings">The restore settings.</param>
		/// <param name="existingCache">The existing cache for the project to backup, or null
		/// to create a new cache for the project defined in the restore settings.</param>
		/// <param name="dialogOwner">A form that can be used as an owner for progress dialog/
		/// message box.</param>
		/// <returns><c>true</c> to indicate that it is okay to proceed with the restore;
		/// <c>false</c> to indicate that the backup failed and the user wasn't comfortable
		/// with just blindly going ahead with a restore that could potentially leave him in
		/// tears.</returns>
		private static bool BackupProjectForRestore(FwRestoreProjectSettings restoreSettings, LcmCache existingCache, Form dialogOwner)
		{
			using (var progressDlg = new ProgressDialogWithTask(dialogOwner))
			{
				var cache = existingCache ?? LcmCache.CreateCacheFromExistingData(new ProjectId(restoreSettings.Settings.FullProjectPath), s_sWsUser, s_ui, FwDirectoryFinder.LcmDirectories, CreateLcmSettings(), progressDlg);

				try
				{
					var versionInfoProvider = new VersionInfoProvider(Assembly.GetExecutingAssembly(), false);
					var backupSettings = new BackupProjectSettings(cache, restoreSettings.Settings, FwDirectoryFinder.DefaultBackupDirectory, versionInfoProvider.MajorVersion)
					{
						DestinationFolder = FwDirectoryFinder.DefaultBackupDirectory
					};

					var backupService = new ProjectBackupService(cache, backupSettings);
					string backupFile;
					if (!backupService.BackupProject(progressDlg, out backupFile))
					{
						var msg = string.Format(FwCoreDlgs.FwCoreDlgs.ksCouldNotBackupSomeFiles, string.Join(", ", backupService.FailedFiles.Select(Path.GetFileName)));
						if (MessageBox.Show(msg, FwCoreDlgs.FwCoreDlgs.ksWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) !=
						    DialogResult.Yes)
						{
							File.Delete(backupFile);
					}
				}
				}
				catch (FwBackupException e)
				{
					if (MessageBox.Show(dialogOwner,
						string.Format(FwCoreDlgs.FwCoreDlgs.ksBackupErrorCreatingZipfile, e.ProjectName, e.Message) +
						Environment.NewLine + Environment.NewLine + Properties.Resources.ksBackupErrorDuringRestore,
						FwCoreDlgs.FwCoreDlgs.ksBackupErrorCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
						DialogResult.No)
					{
						return false;
					}
				}
				finally
				{
					if (existingCache == null) // We created a new cache so we need to dispose of it
					{
						cache.Dispose();
				}
			}
			}
			return true;
		}

		private static LcmSettings CreateLcmSettings()
		{
			var settings = new LcmSettings();
			var sharedXmlBackendCommitLogSize = 0;
			if (FwRegistryHelper.FieldWorksRegistryKey != null)
			{
				sharedXmlBackendCommitLogSize = (int)FwRegistryHelper.FieldWorksRegistryKey.GetValue("SharedXMLBackendCommitLogSize", 0);
			}
			if (sharedXmlBackendCommitLogSize == 0 && FwRegistryHelper.FieldWorksRegistryKeyLocalMachine != null)
			{
				sharedXmlBackendCommitLogSize = (int)FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.GetValue("SharedXMLBackendCommitLogSize", 0);
			}
			if (sharedXmlBackendCommitLogSize > 0)
			{
				settings.SharedXMLBackendCommitLogSize = sharedXmlBackendCommitLogSize;
			}
			settings.UpdateGlobalWSStore = s_appSettings.UpdateGlobalWSStore;
			return settings;
		}

		/// <summary>
		/// Attempts to find another FieldWorks process that can handle a restore for the
		/// specified restore project settings.
		/// </summary>
		/// <param name="settings">The restore project settings.</param>
		/// <returns>True if another process was found and that process handled the restore,
		/// false otherwise</returns>
		private static bool TryFindRestoreHandler(FwRestoreProjectSettings settings)
		{
			return RunOnRemoteClients(kFwRemoteRequest, requestor => requestor.HandleRestoreProjectRequest(settings));
		}
#endregion

#region Link Handling Methods
		/// <summary>
		/// Handles a link request. This handles determining the correct application to start
		/// up and for the correct project. This method is thread safe.
		/// </summary>
		/// <param name="link">The link.</param>
		internal static void HandleLinkRequest(FwAppArgs link)
		{
			ThreadHelper.Invoke(() =>
			{
				Debug.Assert(s_projectId != null, "We shouldn't try to handle a link request until an application is started");
				var linkedProject = new ProjectId(link.DatabaseType, link.Database);
				if (IsSharedXmlBackendNeeded(linkedProject))
				{
					linkedProject.Type = BackendProviderType.kSharedXML;
				}
				if (linkedProject.Equals(s_projectId))
				{
					FollowLink(link);
				}
				else if (!TryFindLinkHandler(link))
				{
					// No other FieldWorks process was running that could handle the request, so
					// start a brand new process for the project requested by the link.
					OpenProjectWithNewProcess(linkedProject, "-" + FwLinkArgs.kLink, link.ToString());
				}
			});
		}

		/// <summary>
		/// Follows the specified link request for this project.
		/// </summary>
		/// <param name="link">The link request.</param>
		internal static void FollowLink(FwAppArgs link)
		{
			// Make sure the application that needs to handle the link is created.
			KickOffAppFromOtherProcess(link);

			// FWR-2504 Maybe I'm missing something but "KickOffAppFromOtherProcess(link)"
			// seems to activate the link already in "InitializeApp()". If so, FwAppArgs
			// will have been cleared out by now. I'll leave this in, since there may be
			// other cases where the link information survives and we need to follow it now.
			if (!link.HasLinkInformation)
			{
				return;
			}
			var app = s_flexApp;
			Debug.Assert(app != null && app.HasBeenFullyInitialized, "KickOffAppFromOtherProcess should create the application needed");
				// Let the application handle the link
				app.HandleIncomingLink(link);
			}

		/// <summary>
		/// Attempts to find another FieldWorks process that can handle the specified link.
		/// </summary>
		/// <param name="link">The link.</param>
		/// <returns>True if to correct process was found to handle the link, false otherwise
		/// </returns>
		private static bool TryFindLinkHandler(FwAppArgs link)
		{
			return RunOnRemoteClients(kFwRemoteRequest, requestor => (requestor.HandleLinkRequest(link)));
		}

		/// <summary>
		/// Checks if this is the ninth time for the program to be run and then ask the user
		/// if they want to move their old external link files to the FW 7.0 or later location
		/// </summary>
		/// <param name="app">The application.</param>
		private static void CheckForMovingExternalLinkDirectory(IFlexApp app)
		{
			// Don't crash here if we have a data problem -- that may be due to another issue that
			// would be masked by throwing at this point.  (See FWR-3849.)
			// app.Cache will be null if we couldn't open the project (FWNX-684)
			if (app?.Cache?.ProjectId == null)
			{
				return;
			}
			// Check that we're on the ninth launch of Flex, and that it hasn't been
			// launched nine or more times already.
			var launchesFlex = 0;
			if (RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksRegistryKey, "Language Explorer"))
			{
				using (var keyFlex = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("Language Explorer"))
				{
					if (keyFlex != null)
					{
						int.TryParse(keyFlex.GetValue("launches", "0") as string, out launchesFlex);
					}
				}
			}
			if (launchesFlex != 9)
			{
				return;
			}
			using (var rk = Registry.LocalMachine.OpenSubKey("Software\\SIL\\FieldWorks"))
			{
				string oldDir = null;
				if (rk != null)
				{
					oldDir = rk.GetValue("RootDataDir") as string;
				}
				if (oldDir == null)
				{
					// e.g. "C:\\ProgramData\\SIL\\FieldWorks"
					oldDir = FwDirectoryFinder.CommonAppDataFolder("FieldWorks");
				}
				oldDir = oldDir.TrimEnd(new [] {Path.PathSeparator});
				var newDir = app.Cache.LangProject.LinkedFilesRootDir;
				newDir = newDir.TrimEnd(new [] {Path.PathSeparator});
				// This isn't foolproof since the currently open project on the 9th time may
				// not even be one that was migrated. But it will probably work for most users.
				if (newDir.ToLowerInvariant() != oldDir.ToLowerInvariant())
				{
					return;
				}
					// TODO-Linux: Help is not implemented in Mono
				var res = MessageBox.Show(Properties.Resources.ksProjectLinksStillOld,
						Properties.Resources.ksReviewLocationOfLinkedFiles,
						MessageBoxButtons.YesNo, MessageBoxIcon.None,
						MessageBoxDefaultButton.Button1, 0, app.HelpFile,
						"/User_Interface/Menus/File/Project_Properties/Review_the_location_of_Linked_Files.htm");
				if (res != DialogResult.Yes)
				{
					return;
				}
				MoveExternalLinkDirectoryAndFiles(app);
			}
		}

		/// <summary>
		/// Moves the external link directory and files to the new FW 7.0 or later location.
		/// </summary>
		/// <param name="app">The application.</param>
		private static void MoveExternalLinkDirectoryAndFiles(IApp app)
		{
			var sLinkedFilesRootDir = app.Cache.LangProject.LinkedFilesRootDir;
			NonUndoableUnitOfWorkHelper.Do(app.Cache.ActionHandlerAccessor, () =>
			{
				app.Cache.LangProject.LinkedFilesRootDir = LcmFileHelper.GetDefaultLinkedFilesDir(app.Cache.ProjectId.ProjectFolder);
			});
			app.UpdateExternalLinks(sLinkedFilesRootDir);
		}
#endregion

#region Event Handlers
		/// <summary>
		/// Handles the Activated event of FieldWorks Main Windows.
		/// </summary>
		/// <param name="sender">The main window that just got activated.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> Not used.</param>
		private static void FwMainWindowActivated(object sender, EventArgs e)
		{
			if (sender is IFwMainWnd)
			{
				s_activeMainWnd = (IFwMainWnd)sender;
			}
		}

		/// <summary>
		/// Handles the Closing event of FieldWorks Main Windows.
		/// </summary>
		/// <param name="sender">The main window that is closing.</param>
		/// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> Not used.</param>
		private static void FwMainWindowClosing(object sender, CancelEventArgs e)
		{
			if (s_activeMainWnd != sender)
			{
				return;
			}
				// Remember the settings, so that, if we end up saving some changes
				// related to it, we can record the last saved project.
			s_settingsForLastClosedWindow = s_flexApp.RegistrySettings;
				// Make sure the closing main window is not considered the active main window
				s_activeMainWnd = null;
			}
#endregion

#region Window Handling Methods

		/// <summary>
		/// Creates a new main window and initializes it. The specified App is responsible for
		/// creating the proper main window type.
		/// </summary>
		/// <param name="currentWindow"></param>
		/// <param name="fNewCache"><c>true</c> if we didn't reuse an existing cache</param>
		/// <returns>True if the main window was create and initialized successfully</returns>
		internal static bool CreateAndInitNewMainWindow(IFwMainWnd currentWindow, bool fNewCache)
		{
			WriteSplashScreen(s_flexApp.GetResourceString("kstidInitWindow"));

			Form fwMainWindowAsForm;
			IFwMainWnd fwMainWindowAsIFwMainWnd;
			try
			{
				// Construct the new window, of the proper derived type
				fwMainWindowAsForm = s_flexApp.NewMainAppWnd(s_splashScreen, fNewCache, currentWindow);
				fwMainWindowAsIFwMainWnd = (IFwMainWnd)fwMainWindowAsForm;
				// It seems to get activated before we connect the Activate event. But it IS active by now;
				// so just record it now as the active one.
				s_activeMainWnd = fwMainWindowAsIFwMainWnd;
				using (new DataUpdateMonitor(fwMainWindowAsForm, "Migrating Dictionary Configuration Settings"))
				{
					s_activeMainWnd.Publisher.Publish("MigrateOldConfigurations", null);
				}
				EnsureValidReversalIndexConfigFile(s_flexApp.Cache);
				s_activeMainWnd.PropertyTable.SetProperty("AppSettings", s_appSettings, false, false);
			}
			catch (StartupException ex)
			{
				// REVIEW: Can this actually happen when just creating a new main window?
				CloseSplashScreen();
				MessageBox.Show(ex.Message, Properties.Resources.ksErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			CloseSplashScreen();

			if (!fwMainWindowAsIFwMainWnd.OnFinishedInit())
			{
				return false;	// did not initialize properly!
			}

			fwMainWindowAsForm.Activated += FwMainWindowActivated;
			fwMainWindowAsForm.Closing += FwMainWindowClosing;
			return true;
		}

		private static void EnsureValidReversalIndexConfigFile(LcmCache cache)
		{
			var wsMgr = cache.ServiceLocator.WritingSystemManager;
			cache.DomainDataByFlid.BeginNonUndoableTask();
			ReversalIndexServices.CreateOrRemoveReversalIndexConfigurationFiles(wsMgr, cache, FwDirectoryFinder.DefaultConfigurations, FwDirectoryFinder.ProjectsDirectory, cache.LangProject.ShortName);
			cache.DomainDataByFlid.EndNonUndoableTask();
		}

		/// <summary>
		/// Closes all main windows for the specified application ignoring any errors that
		/// occur.
		/// </summary>
		/// <param name="app">The application.</param>
		private static void CloseAllMainWindowsForApp(IFlexApp app)
		{
			if (app == null)
			{
				return;
			}
			foreach (var mainWnd in app.MainWindows.OfType<Form>())
			{
				if (mainWnd.IsDisposed)
				{
					continue;	// This can happen in renaming.
				}
				// This is typically used if an exception happens to gracefully close any
				// open main windows so we need to ignore any errors because we have no
				// idea what state the application is in.
				var wnd = mainWnd;
				mainWnd.Invoke((Action) (() => ExceptionHelper.LogAndIgnoreErrors(wnd.Close)));
			}
		}
#endregion

#region Application Management Methods
		/// <summary>
		/// Starts or activates an application requested from another process. This method is
		/// thread safe.
		/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
		/// </summary>
		/// <param name="args">The application arguments</param>
		internal static void KickOffAppFromOtherProcess(FwAppArgs args)
		{
			ThreadHelper.Invoke(() =>
			{
				// Get the new application first so it can give us the application name, etc.
				var app = GetOrCreateApplication(args);
				if (app == null)
				{
					return;
				}

				if (app.HasBeenFullyInitialized)
				{
					// The application is already running so make sure we don't try re-initialize it
					if (app.MainWindows.Count == 0)
					{
						ApplicationBusyDialog.ShowOnSeparateThread(args, ApplicationBusyDialog.WaitFor.WindowToActivate, app, null);
					}
					else
					{
						app.ActivateWindow(0);
						if (args.HasLinkInformation)
						{
							app.HandleIncomingLink(args);
					}
					}
					return;
				}

				if (!InAppServerMode)
				{
					return;
				}
					// Make sure the cache is initialized for the application.
				using (var dlg = new ProgressDialogWithTask(ThreadHelper))
				{
						InitializeApp(app, dlg);
				}
			});
		}

		/// <summary>
		/// Gets an existing app or creates a new one for the application type specified on
		/// the command line.
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <returns>An app to use (can be null if no valid app was specified)</returns>
		private static IFlexApp GetOrCreateApplication(FwAppArgs args)
		{
			if (s_flexApp != null)
			{
			return s_flexApp;
		}
			s_flexApp = s_compositionContainer.GetExportedValue<IFlexApp>();
			s_flexApp.FwAppArgs = args;
			s_flexAppKey = s_flexApp.SettingsKey;
				return s_flexApp;
		}

		/// <summary>
		/// Attempt to initialize the specified application on the specified project. This also
		/// means creating the cache and doing application-specific initializing for the cache.
		/// </summary>
		/// <param name="app">The application to initialize.</param>
		/// <param name="projectId">The project id.</param>
		/// <returns>True if the application was successfully initialized, false otherwise</returns>
		private static bool InitializeFirstApp(IFlexApp app, ProjectId projectId)
		{
			Debug.Assert(Cache == null && s_projectId == null, "This should only get called once");
			Debug.Assert(projectId != null, "Should have exited the program");

			using (var process = Process.GetCurrentProcess())
			{
				app.RegistrySettings.LoadingProcessId = process.Id;
			}
			if (string.IsNullOrEmpty(app.RegistrySettings.LatestProject))
			{
				// Until something gets saved, we will keep track of the first project opened.
				app.RegistrySettings.LatestProject = projectId.Handle;
			}

			UsageEmailDialog.IncrementLaunchCount(app.SettingsKey); // count launches for bug reporting

			ShowSplashScreen();

			try
			{
				// Create the cache and let the application init the cache for what it needs
				Cache = CreateCache(projectId);
				Debug.Assert(Cache != null, "At this point we should know which project to load and have loaded it!");

				if (s_noUserInterface || InitializeApp(app, s_splashScreen))
				{
					app.RegistrySettings.LoadingProcessId = 0;
					return true;
				}
			}
			catch (StartupException sue)
			{
				if (MiscUtils.IsUnix && sue.InnerException is UnauthorizedAccessException)
				{
					// Tell Mono user he/she needs to logout and log back in
					MessageBox.Show(ResourceHelper.GetResourceString("ksNeedToJoinFwGroup"));
				}
				throw;
			}
			catch (LcmDataMigrationForbiddenException)
			{
				// tell the user to close all other applications using this project
				MessageBox.Show(ResourceHelper.GetResourceString("kstidDataMigrationProhibitedText"),
					ResourceHelper.GetResourceString("kstidDataMigrationProhibitedCaption"), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch (LcmInitializationException fie)
			{
				throw new StartupException(fie.Message, fie);
			}
			finally
			{
				CloseSplashScreen();
			}

			return false;
		}

		/// <summary>
		/// Initializes the specified application and creates a new main window for it. Also
		/// does application-specific cache initialization.
		/// </summary>
		/// <param name="app">The application</param>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the application was started successfully, false otherwise</returns>
		private static bool InitializeApp(IFlexApp app, IThreadedProgress progressDlg)
		{
			using (new DataUpdateMonitor(null, "Application Initialization"))
			{
				app.DoApplicationInitialization(progressDlg);
			}
			using (new DataUpdateMonitor(null, "Loading Application Settings"))
			{
				app.LoadSettings();
			}

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(Cache.ServiceLocator.GetInstance<IActionHandler>()))
			using (new DataUpdateMonitor(null, "Application Cache Initialization"))
			{
				try
				{
					if (!app.InitCacheForApp(progressDlg))
					{
						throw new StartupException(Properties.Resources.kstidCacheInitFailure);
				}
				}
				catch (Exception e)
				{
					if (e is StartupException)
				{
					throw;
				}
					throw new StartupException(Properties.Resources.kstidCacheInitFailure, e, true);
				}
				undoHelper.RollBack = false;
			}

			if (Cache.ServiceLocator.GetInstance<IUndoStackManager>().HasUnsavedChanges)
			{
				if (progressDlg != null)
				{
					progressDlg.Message = string.Format(Properties.Resources.kstidSaving, Cache.ProjectId.UiName);
					progressDlg.IsIndeterminate = true;
				}
				Cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
				if (progressDlg != null)
				{
					progressDlg.IsIndeterminate = false;
			}
			}

			return CreateAndInitNewMainWindow(s_activeMainWnd, true);
		}

		/// <summary>
		/// Shutdowns the specified application. The application will be disposed of immediately.
		/// If no other applications are running, then FieldWorks will also be shutdown.
		/// </summary>
		/// <param name="app">The application to shut down.</param>
		internal static void ShutdownApp(IFlexApp app)
		{
			if (app != s_flexApp)
			{
				throw new ArgumentException("Application must belong to this FieldWorks", nameof(app));
			}

			if (s_activeMainWnd != null && app.MainWindows.Contains(s_activeMainWnd))
			{
				// The application that owns the active main window is being disposed. This
				// means that the window is, most likely, already disposed.
				s_activeMainWnd = null;
			}

			RecordLastAppForProject();

			// Make sure we do this after we set the variables to null to keep a race condition
			// from happening where we want to GetOrCreateApplication() for the app that is
			// being disposed.
			try
			{
				app.Dispose();
				s_flexApp = null;
			}
			catch
			{
				// continue shutdown even with an exception. It's possible we're shutting down because
				// of a crash and we don't know what state the application is in.
			}

			ExitIfNoAppsRunning();
		}

		/// <summary>
		/// If no applications are running anymore, just shut down the whole process
		/// </summary>
		private static void ExitIfNoAppsRunning()
		{
			if (s_flexApp == null || s_flexApp.MainWindows.Count == 0)
			{
				ExitCleanly();
			}
		}

		/// <summary>
		/// Attempts to shut down the specified application as nicely as possible ignoring any
		/// errors that occur.
		/// </summary>
		/// <param name="app">The application.</param>
		private static void GracefullyShutDownApp(IFlexApp app)
		{
			if (app == null)
			{
				return;
			}
			// This is typically used if an exception happens so we need to ignore any errors
			// because we have no idea what state the application is in.
			ExceptionHelper.LogAndIgnoreErrors(() => ShutdownApp(app));
		}

		/// <summary>
		/// Records the last running application for the current project.
		/// </summary>
		private static void RecordLastAppForProject()
		{
			if (Cache == null || Cache.IsDisposed)
			{
				return; // too late
			}
			if (s_flexApp != null)
			{
				return; // this isn't the last one to shut down, not time to record.
			}

			var settingsFolder = Path.Combine(Cache.ProjectId.ProjectFolder, LcmFileHelper.ksConfigurationSettingsDir);
			try
			{
				Directory.CreateDirectory(settingsFolder);
			}
			catch (IOException)
			{
				// No great harm done if we can't write this marker
			}
			catch (SecurityException)
			{
				// Likewise
			}
			catch (UnauthorizedAccessException)
			{
				// and another
			}
		}
#endregion

#region Remote Process Handling Methods
		/// <summary>
		/// Determines whether any FW process is in "single user mode".
		/// </summary>
		private static bool IsInSingleFWProccessMode()
		{
			return RunOnRemoteClients(kFwRemoteRequest, requestor =>
			{
				Func<bool> invoker = requestor.InSingleProcessMode;
				var ar = invoker.BeginInvoke(null, null);
				while (!ar.IsCompleted)
				{
					if (!ar.AsyncWaitHandle.WaitOne(9000, false))
					{
						return false; // Just continue on
				}
				}
				// We can now ask for the answer.
				if (invoker.EndInvoke(ar))
				{
					requestor.BringMainFormToFront();
					return true; // Should kill this process
				}
				return false; // Need to check the other FW processes
			});
		}

		/// <summary>
		/// Tries to find an existing FieldWorks process that is running the specified project.
		/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
		/// </summary>
		/// <param name="project">The project we want to conect to.</param>
		/// <param name="args">The application arguments.</param>
		/// <returns>
		/// True if an existing process was found with the specified project and
		/// control was released to the found process, false otherwise
		/// </returns>
		private static bool TryFindExistingProcess(ProjectId project, FwAppArgs args)
		{
			return RunOnRemoteClients(kFwRemoteRequest, requestor =>
			{
				ProjectMatch isMyProject;
				Func<ProjectId, FwAppArgs, ProjectMatch> invoker = requestor.HandleOpenProjectRequest;
				var start = DateTime.Now;
				do
				{
					var ar = invoker.BeginInvoke(project, args, null, null);
					while (!ar.IsCompleted)
					{
						s_fWaitingForUserOrOtherFw = true;
						// Wait until this process knows which project it is loading.
						if (!ar.AsyncWaitHandle.WaitOne(9000, false))
						{
							// timed out.
							if (MessageBox.Show(Properties.Resources.kstidFieldWorksDidNotRespond, Properties.Resources.kstidStartupProblem,
								MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
							{
								return true;
							}
						}
					}
					// We can now ask for the answer.
					isMyProject = invoker.EndInvoke(ar);

					if (isMyProject == ProjectMatch.SingleProcessMode)
					{
						Logger.WriteEvent("WEIRD! Detected single FW process mode while this process is trying to open a project.");
						Debug.Fail("We don't think this can happen, but it's no big deal.");
						return true; // Should kill this process
					}
					if (DateTime.Now - start > new TimeSpan(0, 0, 10))
					{
						// Some other process apparently keeps telling us it doesn't know. It's probably stuck in this same loop,
						// waiting for us!
						MessageBox.Show(Properties.Resources.kstidFieldWorksRespondedNotSure, Properties.Resources.kstidStartupProblem,
							MessageBoxButtons.OK, MessageBoxIcon.Warning);
						return true; // pretends some other process has the project opened and is handling the request; this process will quit
					}
				} while (isMyProject == ProjectMatch.DontKnowYet);

				s_fWaitingForUserOrOtherFw = false;
				return (isMyProject == ProjectMatch.ItsMyProject);
			});
		}

		/// <summary>
		/// Creates a remoting server to listen for events from other instances.
		/// </summary>
		private static void CreateRemoteRequestListener()
		{
			IDictionary dict = new Hashtable(2);
			dict["name"] = "FW App Instance Listener";
			var maxPort = kStartingPort + 100;
			Exception lastException = null;
			var fFoundAvailablePort = false;
			for (var port = kStartingPort; !fFoundAvailablePort && port < maxPort; port++)
			{
				try
				{
					dict["port"] = port;

					// Set up the server channel.
					var instanceListener = new TcpChannel(dict, null, null);
					ChannelServices.RegisterChannel(instanceListener, false);
					RemotingConfiguration.ApplicationName = FwUtils.ksSuiteName;
					RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteRequest), kFwRemoteRequest, WellKnownObjectMode.Singleton);
					RemotingConfiguration.RegisterWellKnownServiceType(typeof(PaRemoteRequest), kPaRemoteRequest, WellKnownObjectMode.Singleton);

					fFoundAvailablePort = true;
					s_serviceChannel = instanceListener;
					s_servicePort = port;
					Logger.WriteEvent("Listening on port " + port);
				}
				catch (Exception e)
				{
					Logger.WriteEvent("Attempt to listen on port " + port + " failed.");
					Logger.WriteError(e);
					// Keep trying different ports until we find an available one.
					lastException = e;
				}
			}
			if (!fFoundAvailablePort)
			{
				throw new RemotingException("Could not find any available port for listening.", lastException);
		}
		}

		/// <summary>
		/// Runs the specified delegate on any other FieldWorks processes that are found.
		/// If no other FieldWorks processes are found, then the delegate is not called.
		/// </summary>
		/// <param name="requestType">e.g. FW_RemoteRequest</param>
		/// <param name="whatToRun">The deleage to run.</param>
		/// <returns>True if the delegate was called and it ran successfully, false otherwise
		/// </returns>
		internal static bool RunOnRemoteClients(string requestType, Func<RemoteRequest, bool> whatToRun)
		{
			var processes = ExistingProcesses;
			if (processes.Count == 0)
			{
				return false;
			}

			// REVIEW: Need to see if trying this many ports causes performance problems
			var maxPort = kStartingPort + processes.Count * 4;

			// Based on the requested project, decide if we need to start a new one or use the
			// one we already have.
			var channelConfiguration = new Hashtable
			{
				["name"] = "FW Process Remote Request"
			};
			// name is need to make connection unique
			var processesToBeChecked = processes.Count;
			for (var port = kStartingPort; processesToBeChecked > 0 && port < maxPort; port++)
			{
				var chan = new TcpChannel(channelConfiguration, null, null);
				try
				{
					// Create a channel for communicating w/ the process.
					ChannelServices.RegisterChannel(chan, false);

					if (s_servicePort == port)
					{
						continue; // no need to check our service port
					}

					// Create an instance of the remote object
					var requestor = CreateRequestor(port, requestType);
					if (requestor == null)
					{
						continue;
					}

					// Let the delegate do whatever it needs to with the RemoteRequest
					if (whatToRun(requestor))
					{
						return true; // The delegate got what it needed
					}

					processesToBeChecked--;
				}
				catch
				{
					// The process is most likely not listening on the specified port. In
					// which case we want to ignore the error and try the next port.
				}
				finally
				{
					ChannelServices.UnregisterChannel(chan);
				}
			}

			return false;
		}

		/// <summary>
		/// Remote request calls can hang if the service is no longer active but the port is still
		/// in the listening state. This method calls the IsAlive method on the requestor to verify
		/// that it is valid.
		/// </summary>
		/// <param name="port">The port.</param>
		/// <param name="requestType">Type of the request.</param>
		/// <returns></returns>
		private static RemoteRequest CreateRequestor(int port, string requestType)
		{
			var requestor = (RemoteRequest)Activator.GetObject(typeof(RemoteRequest), "tcp://localhost:" + port + "/" + requestType);
			Func<bool> invoker = requestor.IsAlive;
			var ar = invoker.BeginInvoke(null, null);
			if (!ar.AsyncWaitHandle.WaitOne(1000, false))
			{
				return null;
			}

			invoker.EndInvoke(ar);
			return requestor;
		}

#endregion

#region Other Private/Internal Methods
		/// <summary>
		/// Releases unmanaged and managed resources
		/// </summary>
		private static void StaticDispose()
		{
			InAppServerMode = false; // Make sure the cache can be cleaned up
			LexicalProviderManager.StaticDispose(); // Must be done before disposing the cache
			if (s_serviceChannel != null)
			{
				ChannelServices.UnregisterChannel(s_serviceChannel);
				s_serviceChannel = null;
			}

			KeyboardController.Shutdown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}

			GracefullyShutDown();

			ThreadHelper?.Dispose();
			ThreadHelper = null;

			FwRegistrySettings.Release();

			s_compositionContainer?.Dispose();
			s_compositionContainer = null;
		}

#if DEBUG
		/// <summary>
		/// Writes the current executable path to the registry. This is used for external
		/// applications to read on developer machines since the RootCodeDir on developer's
		/// machines point to distfiles.
		/// </summary>
		private static void WriteExecutablePathSettingForDevs()
		{
			try
			{
				using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\SIL\FieldWorks\8"))
				{
					key.SetValue("FwExeDir", Path.GetDirectoryName(Application.ExecutablePath));
				}
			}
			catch
			{
				// Ignore any errors trying to do this since we just want this for developers
			}
		}
#endif

		/// <summary>
		/// Gets a HelpTopicProvider for the specified application if possible. Falls back to
		/// getting the HelpTopicProvider for another application if the requested one is not
		/// installed. Will not return null (but it could throw an exception if no FW app is
		/// installed).
		/// </summary>
		internal static IHelpTopicProvider GetHelpTopicProvider()
		{
			return s_flexApp ?? s_compositionContainer.GetExportedValue<IHelpTopicProvider>();
		}

		/// <summary>
		/// Sets information about the application and the computer the user is running on
		/// to the ErrorReporter so that it can be reported with a crash.
		/// </summary>
		private static void SetupErrorReportInformation()
		{
			var version = Version;
			if (version != null)
			{
				// Extract the fourth (and final) field of the version to get a date value.
				var ich = version.IndexOf('.');
				if (ich >= 0)
				{
					ich = version.IndexOf('.', ich + 1);
				}
				if (ich >= 0)
				{
					ich = version.IndexOf('.', ich + 1);
				}
				if (ich >= 0)
				{
					var iDate = Convert.ToInt32(version.Substring(ich + 1));
					if (iDate > 0)
					{
						var oadate = Convert.ToDouble(iDate);
						var dt = DateTime.FromOADate(oadate);
						version += $"  {dt:yyyy/MM/dd}";
					}
				}
#if DEBUG
				version += "  (Debug version)";
#endif
				ErrorReporter.AddProperty("Version", version);
			}
			ErrorReporter.AddProperty("CommandLine", Environment.CommandLine);
			ErrorReporter.AddProperty("CurrentDirectory", Environment.CurrentDirectory);
			ErrorReporter.AddProperty("MachineName", Environment.MachineName);
			ErrorReporter.AddProperty("OSVersion", Environment.OSVersion.ToString());
			ErrorReporter.AddProperty("OSRelease", ErrorReport.GetOperatingSystemLabel());
			if (MiscUtils.IsUnix)
			{
				var packageVersions = LinuxPackageUtils.FindInstalledPackages("fieldworks-applications*");
				if (packageVersions.Any())
				{
					var packageVersion = packageVersions.First();
					ErrorReporter.AddProperty("PackageVersion", $"{packageVersion.Key} {packageVersion.Value}");
				}
			}
			ErrorReporter.AddProperty("CLR version", Environment.Version.ToString());
			var mem = MiscUtils.GetPhysicalMemoryBytes() / 1048576;
			ErrorReporter.AddProperty("PhysicalMemory", mem + " Mb");
			ulong diskSize;
			ulong diskFree;
			var cDisks = MiscUtils.GetDiskDriveStats(out diskSize, out diskFree);
			diskFree /= 1073742;  // 1024*1024*1024/1000 matches drive properties in Windows
			diskSize /= 1073742;
			ErrorReporter.AddProperty("LocalDiskCount", cDisks.ToString());
			ErrorReporter.AddProperty("FwProgramDiskSize", diskSize + " Mb");
			ErrorReporter.AddProperty("FwProgramDiskFree", diskFree + " Mb");
			// TODO-Linux: WorkingSet always returns 0 on Mono.
			ErrorReporter.AddProperty("WorkingSet", Environment.WorkingSet.ToString());
			ErrorReporter.AddProperty("UserDomainName", Environment.UserDomainName);
			ErrorReporter.AddProperty("UserName", Environment.UserName);
			ErrorReporter.AddProperty("SystemDirectory", Environment.SystemDirectory);
			ErrorReporter.AddProperty("Culture", System.Globalization.CultureInfo.CurrentCulture.ToString());
			using (var bm = new Bitmap(10, 10))
			{

				ErrorReporter.AddProperty("ScreenDpiX", bm.HorizontalResolution.ToString());
				ErrorReporter.AddProperty("ScreenDpiY", bm.VerticalResolution.ToString());
			}
		}

		/// <summary>
		/// Sets information about the current database to the ErrorReporter so that it can
		/// be reported with a crash.
		/// </summary>
		private static void SetupErrorPropertiesNeedingCache(LcmCache cache)
		{
			ErrorReporter.AddProperty("ProjectName", cache.ProjectId.Name);
			ErrorReporter.AddProperty("ProjectHandle", cache.ProjectId.Handle);
			ErrorReporter.AddProperty("ProjectObjectCount", cache.ServiceLocator.GetInstance<ICmObjectRepository>().Count.ToString());
			if (File.Exists(cache.ProjectId.Path))
			{
				var info = new FileInfo(cache.ProjectId.Path);
				ErrorReporter.AddProperty("ProjectModified", info.LastWriteTime.ToString());
				ErrorReporter.AddProperty("ProjectFileSize", info.Length.ToString());
			}
			else
			{
				ErrorReporter.AddProperty("ProjectModified", "unknown--probably not a local file");
				ErrorReporter.AddProperty("ProjectFileSize", "unknown--probably not a local file");
			}
		}

		/// <summary>
		/// Executes the requested action with ALL FW applications temporarily shut down and
		/// then (re)starts the applications in a sensible order. At the end, we're guaranteed
		/// to have at least one app started or FieldWorks will be shut down.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		private static void ExecuteWithAllFwProcessesShutDown(Func<ProjectId> action)
		{
			s_fSingleProcessMode = true;
			try
			{
				// Try to shut down other instances of FieldWorks gracefully so that their data
				// folders can be moved.
				RunOnRemoteClients(kFwRemoteRequest, requestor => requestor.CloseAllMainWindows());
				var processes = ExistingProcesses;
				foreach (var proc in processes)
				{
					if (!proc.HasExited)
					{
						proc.CloseMainWindow();
					}
					if (!proc.HasExited)
					{
						proc.Kill();
						proc.WaitForExit();
					}
					proc.Close();
				}
				ExecuteWithAppsShutDown(action);
			}
			finally
			{
				s_fSingleProcessMode = false;
			}
		}

		/// <summary>
		/// Returns some active, non-disposed application if possible, otherwise, null. Currently Flex is preferred
		/// if both are re-opened.
		/// </summary>
		internal static IFlexApp ReopenProject(string project, FwAppArgs appArgs)
		{
			ExecuteWithAppsShutDown(() =>
				{
					try
					{
						HandleLinkRequest(appArgs);
						return s_projectId;
					}
					catch (Exception e)
					{
						//This is not good.
					}
					return null;
				});
			return s_flexApp;
		}

		/// <summary>
		/// Executes the requested action with all FW applications temporarily shut down and
		/// then (re)starts the applications in a sensible order. At the end, we're guaranteed
		/// to have at least one app started or FieldWorks will be shut down.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		private static void ExecuteWithAppsShutDown(Func<ProjectId> action)
		{
			var allowFinalShutdownOrigValue = s_allowFinalShutdown;
			s_allowFinalShutdown = false; // don't shutdown when we close all windows!

			// Now shut down everything (windows, apps, cache, etc.)
			GracefullyShutDown();

			if (s_applicationExiting)
			{
				// Something bad must have happened because we are shutting down. There is no point in
				// executing the action or in restarting the applications since we have no idea what
				// state the applications/data are in. (FWR-3179)
				Debug.Assert(s_allowFinalShutdown, "If something bad happened, we should be allowing application shutdown");
				return;
			}

			try
			{
				// Run the action
				var projId = action();

				if (projId == null)
				{
					return;
				}

				s_projectId = null; // Needs to be null in InitializeFirstApp

				// Restart the default app from which the action was kicked off
				var app = GetOrCreateApplication(new FwAppArgs(projId.Handle, string.Empty, Guid.Empty));
				if (!InitializeFirstApp(app, projId))
				{
					return;
				}

				s_projectId = projId; // Process needs to know its project
			}
			finally
			{
				s_allowFinalShutdown = allowFinalShutdownOrigValue; // mustn't suppress any longer (unless we already were).
				ExitIfNoAppsRunning();
			}
		}

		/// <summary>
		/// Attempts to shut down as nicely as possible. Typically this method doesn't do
		/// anything when the application is shut down normally (e.g. the user has closed
		/// all of the open main windows) because the applications will have already been
		/// disposed of. Thus, any exceptions that are thrown are ignored by this method as
		/// there is no guarantee that the application is in a valid state.
		/// </summary>
		/// <remarks>Any exeptions that are thrown are logged to the log file.</remarks>
		internal static void GracefullyShutDown()
		{
			// Give any open main windows a chance to close normally before being forcibly
			// disposed.
			CloseAllMainWindowsForApp(s_flexApp);

			// Its quite possible that there are some important messages to process.
			// (e.g., an FwApp.RemoveWindow asynchronous call from FwMainWnd.Dispose)
			// These need to be handled before we shut down the applications or race conditions
			// might occur. (FWR-1687)
			ExceptionHelper.LogAndIgnoreErrors(Application.DoEvents);

			GracefullyShutDownApp(s_flexApp);

			// If FieldWorks was in app server mode, there is a chance that the apps could have
			// already been shut down, but the cache is still running. In this case, we need
			// to shut down the cache explicitly.
			ExitCleanly();

			if (!s_noUserInterface)
			{
				Debug.Assert(s_flexApp == null, "The FLEx app did not get properly cleaned up");
				Debug.Assert(Cache == null, "The cache did not get properly cleaned up");
			}
		}

		/// <summary>
		/// Cleanly exits the FieldWorks process
		/// </summary>
		private static void ExitCleanly()
		{
			if (InAppServerMode)
			{
				return; // Continue running even when all apps are shut down
			}

			if (s_allowFinalShutdown)
			{
				Logger.WriteEvent("Shutting down");
			}

			// To be safe - this method might get called recursively (explicitly and from
			// Application.Exit() below again). Also could be called when the startup process
			// did not complete...we picked an app from the command line, but another process
			// was already handling that project.
			if (Cache != null && !Cache.IsDisposed)
			{
				DataUpdateMonitor.ClearSemaphore();

				using (var progressDlg = new ProgressDialogWithTask(ThreadHelper))
				{
					progressDlg.Title = string.Format(ResourceHelper.GetResourceString("kstidShutdownCaption"), Cache.ProjectId.UiName);
					progressDlg.AllowCancel = false;
					progressDlg.IsIndeterminate = true;
					var stackMgr = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
					if (stackMgr.HasUnsavedChanges)
					{
						progressDlg.RunTask(true, CommitAndDisposeCache);
					}
					else
					{
						// For whatever reasons the progress dialog sometimes got closed while
						// the worker was still busy which caused a hang.
						CommitAndDisposeCache(progressDlg, null);
					}
				}
			}

			// This has to be done to zap anything in it even during a restart triggered by S/R.
			SingletonsContainer.Release();

			if (s_allowFinalShutdown)
			{
				Logger.ShutDown();
				Application.Exit();
			}
		}

		/// <summary>
		/// Determines the match status for the project used by this FieldWorks process and
		/// the specified project. This method is thread-safe.
		/// </summary>
		/// <param name="projectId">The project to test.</param>
		/// <returns>
		/// The result of checking to see if the specified project matches the project used
		/// by this FieldWorks processinstance is running
		/// </returns>
		internal static ProjectMatch GetProjectMatchStatus(ProjectId projectId)
		{
			if (s_fSingleProcessMode)
			{
				return ProjectMatch.SingleProcessMode;
			}

			if (s_fWaitingForUserOrOtherFw)
			{
				return ProjectMatch.WaitingForUserOrOtherFw;
			}

			var thisProjectId = s_projectId; // Store in temp variable for thread safety
			if (thisProjectId == null)
			{
				return ProjectMatch.DontKnowYet;
			}

			return thisProjectId.Equals(projectId) ? ProjectMatch.ItsMyProject : ProjectMatch.ItsNotMyProject;
		}

		/// <summary>
		/// Gets the status of the previous attempt to startup the specified application.
		/// </summary>
		/// <param name="app">The application.</param>
		private static StartupStatus GetPreviousStartupStatus(IFlexApp app)
		{
			var loadingId = app.RegistrySettings.LoadingProcessId;
			if (loadingId > 0)
			{
				// The last attempt to load the application never finished. We need to decide
				// if it didn't finish because of a crash or if its still in the process of
				// loading.
				return ExistingProcesses.Any(process => process.Id == loadingId) ? StartupStatus.StillLoading : StartupStatus.Failed;
			}
			return StartupStatus.Successful;
		}

		/// <summary>
		/// Shows help for command line options.
		/// </summary>
		private static void ShowCommandLineHelp()
		{
			var exeName = Path.GetFileName(Assembly.GetEntryAssembly().CodeBase);
			var helpMessage = string.Format("{0}, Version {1}{3}{3}Usage: {2} [options]{3}{3}" +
				"Options:{3}" +
				"-" + FwAppArgs.kHelp + "\t\tCommand-line usage help{3}" +
				"-" + FwAppArgs.kProject + " <project>\tThe project name{3}" +
				"-" + FwAppArgs.kLocale + " <culture>\tCulture abbreviation{3}" +
				"-" + FwAppArgs.kRestoreFile + " <backup>\tThe fwbackup file{3}" +
				"-" + FwAppArgs.kRestoreOptions + " <flags>\tString indicating optional files to restore{3}" +
				"\tThe flags parameter has form \"clsf\", where:{3}" +
				"\tc - Configuration files{3}" +
				"\tl - Linked files (audio-visual media, pictures, etc.){3}" +
				"\ts - Spelling dictionary files{3}" +
				"\tf - Supporting files (fonts, keyboards, converters){3}",
				Application.ProductName, Application.ProductVersion, exeName, Environment.NewLine);
			// TODO: Add 'link' and 'x' help

			MessageBox.Show(helpMessage, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>
		/// Ask user whether to use newer version of writing systems (presumably changed by some
		/// other Palaso app or some other FW project).
		/// </summary>
		/// <param name="wsLabel">The display name (and other information) for the updated
		/// writing systems (a list of them, possibly).</param>
		/// <param name="projectName">Name of the project where we might switch to the newer writing system.</param>
		/// <returns><c>true</c> to accept newer version; <c>false</c> otherwise</returns>
		private static bool ComplainToUserAboutNewWs(string wsLabel, string projectName)
		{
			// Assume they want the WS updated when we're not supposed to show a UI.
			if (s_noUserInterface)
			{
				return true;
			}

			var text = string.Format(Properties.Resources.kstidGlobalWsChangedMsg, wsLabel, projectName);
			var caption = Properties.Resources.kstidGlobalWsChangedCaption;
			var owner = s_splashScreen != null ? s_splashScreen.Form : Form.ActiveForm;

			return ThreadHelper.ShowMessageBox(owner, text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
		}

		/// <summary>
		/// Close any main windows.  This is needed to implement changing the projects folder
		/// location when multiple projects are running.  (See FWR-2287.)
		/// </summary>
		internal static void CloseAllMainWindows()
		{
			CloseAllMainWindowsForApp(s_flexApp);
		}
#endregion
	}
#endregion

#region WindowsInstallerQuery Class
#if !__MonoCS__

	///<summary>
	/// Class to find out some details about the current FW installation.
	///</summary>
	public static class WindowsInstallerQuery
	{
		private const string InstallerProductCode = "{8E80F1ED-826A-46d5-A59A-D8A203F2F0D9}";
		private const string InstalledProductNameProperty = "InstalledProductName";

		private const int ErrorMoreData = 234;
		private const int ErrorUnknownProduct = 1605;

		[DllImport("msi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int MsiGetProductInfo(string product, string property, StringBuilder valueBuf, ref int cchValueBuf);

		[DllImport("msi.dll", CharSet = CharSet.Unicode)]
		internal static extern uint MsiOpenProduct(string szProduct, out int hProduct);

		[DllImport("msi.dll", CharSet = CharSet.Unicode)]
		internal static extern uint MsiGetFeatureInfo(int hProduct, string szFeature, out uint lpAttributes, StringBuilder lpTitleBuf, ref uint cchTitleBuf, StringBuilder lpHelpBuf, ref uint cchHelpBuf);

		/// <summary>
		/// Check the installer status to see if FW is installed on the user's machine.
		/// If not, it can be assumed we are running on a developer's machine.
		/// </summary>
		/// <returns>True if this is an installed version</returns>
		public static bool IsThisInstalled()
		{
			string productName;

			var status = GetProductInfo(InstalledProductNameProperty, out productName);

			return status != ErrorUnknownProduct;
		}

		private static int GetProductInfo(string propertyName, out string propertyValue)
		{
			var sbBuffer = new StringBuilder();
			var len = sbBuffer.Capacity;
			sbBuffer.Length = 0;

			var status = MsiGetProductInfo(InstallerProductCode, propertyName, sbBuffer, ref len);
			if (status == ErrorMoreData)
			{
				len++;
				sbBuffer.EnsureCapacity(len);
				status = MsiGetProductInfo(InstallerProductCode, InstalledProductNameProperty, sbBuffer, ref len);
			}

			propertyValue = sbBuffer.ToString();

			return status;
		}
	}

#endif
#endregion

}
