// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using SIL.LCModel;
using SIL.LCModel.Utils;
using IPCFramework;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Utility methods for FLExBridge interaction
	/// </summary>
	public static class FLExBridgeHelper
	{
		#region These are the available '-v' parameter options:
		/// <summary>
		/// constant for launching the bridge in Send and Receive mode for full FLEx data set
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file.
		/// Flex Bridge returns 'true', if data changes came in from the S/R, otherwise, 'false' for no changes.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string SendReceive = @"send_receive";
		/// <summary>
		/// constant for launching the bridge in Send and Receive mode for Lift data
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file (which may not actually exist for DB4o projects).
		/// Flex Bridge returns 'true', if data changes came in from the S/R, otherwise, 'false' for no changes.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string SendReceiveLift = @"send_receive_lift";

		/// <summary>
		/// constant for launching the bridge in Obtain project mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the path to the main projects folder where FW projects are located.</para>
		/// <para>Returns the pathname to either the 'fwdata' xml file or the lift file, if FLEx Bridge was able to get a clone. Returns null, if no clone was created.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string Obtain = @"obtain";
		/// <summary>
		/// constant for launching the bridge in Obtain project mode, but only tries to get a new Lift repository for an extant FW project.
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the path to the given (extant) FW project.</para>
		/// <para>Returns the pathname to the lift file, if FLEx Bridge was able to get a clone. Returns null, if no clone was created.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string ObtainLift = @"obtain_lift";

		/// <summary>
		/// constant for launching the bridge in the Conflict\Notes Viewer mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file.</para>
		/// <para>Nothing is returned, since FLEx Bridge's notes dialog will remain open.
		/// As the user selects some note, a call back to FLEx will be done, with the URL for the item to jump to.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string ConflictViewer = @"view_notes";

		/// <summary>
		/// constant for launching the bridge in the Lift Conflict\Notes Viewer mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file.</para>
		/// <para>Nothing is returned, since FLEx Bridge's notes dialog will remain open.
		/// As the user selects some note, a call back to FLEx will be done, with the URL for the item to jump to.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string LiftConflictViewer = @"view_notes_lift";

		/// <summary>
		/// constant for launching the bridge in the undo export mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file. Nothing is returned.</para>
		/// <para>Flex Bridge restores the local working folder to what is in the lift repository, including deleting any new files.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string UndoExportLift = @"undo_export_lift";

		/// <summary>
		/// constant for launching the bridge in the move lift mode
		/// </summary>
		/// <remarks>
		/// <para>Instruct FLEx Bridge to try to move an extant repository from the old location to the new,
		/// if the old one exists. FLEx should not use this option if the new repository already exists.</para>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file. The new repository location is returned, if it was moved, other wise null is returned.</para>
		/// <para>This option must also use the '-g' command line argument which gives FLEx Bridge the language project's guid,
		/// which is used to find the correct lift repository.</para>
		/// </remarks>
		public const string MoveLift = @"move_lift";

		/// <summary>
		/// constant for launching the bridge in the "Check for Updates" mode
		/// </summary>
		/// <remarks>
		/// <para>Instruct FLEx Bridge to show its "Check for Updates" information.</para>
		/// </remarks>
		public const string CheckForUpdates = @"check_for_updates";

		/// <summary>
		/// constant for launching the bridge in the "About FLEx Bridge" mode
		/// </summary>
		/// <remarks>
		/// <para>Instruct FLEx Bridge to show its "About" information.</para>
		/// </remarks>
		public const string AboutFLExBridge = @"about_flex_bridge";

		#endregion End of available '-v' parameter options:

		/// <summary>
		/// constant for locating the nested lift repository (within the "OtherRepositories" path of a project).
		/// See also SIL.FieldWorks.FDO.LcmFileHelper.OtherRepositories
		/// </summary>
		public const string LIFT = "LIFT";

		/// <summary>
		/// The Chorus branch name for LIFT projects must include the LDML version (LT-18674)
		/// </summary>
		public const string LiftVersion = "0.13_ldml3";

		/// <summary>
		/// The FLEx Bridge Data Version is part of the Chorus branch name. It must be the same for all users who are collaborating on a project.
		/// </summary>
		public static string FlexBridgeDataVersion { get; }

		/// <summary/>
		public static Version FlexBridgeVersion { get; }

		/// <summary>
		/// Event handler delegate that passes a jump URL.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public delegate void JumpEventHandler(object sender, FLExJumpEventArgs e);

		/// <summary>
		/// Event to enabled FLExBridgeListener to find out when the Conflict Report title was clicked.
		/// </summary>
		public static event JumpEventHandler FLExJumpUrlChanged;

		private const string FLExBridgeName = @"FLExBridge.exe";

		private static object _waitObject = new object();
		private static bool _flexBridgeTerminated;
		private static Tuple<IIPCHost, Action> _noBlockerHostAndCallback;
		private static bool _receivedChanges; // true if changes merged via FLExBridgeService.BridgeWorkComplete()
		private static string _projectName; // fw proj path via FLExBridgeService.InformFwProjectName()
		private static string _pipeID;

		static FLExBridgeHelper()
		{
			var fbDllWithConstantsPath = Path.Combine(FwDirectoryFinder.FlexBridgeFolder, "LibFLExBridge-ChorusPlugin.dll");
			if (File.Exists(fbDllWithConstantsPath))
			{
				var fbAssemblyWithConstants = Assembly.ReflectionOnlyLoadFrom(fbDllWithConstantsPath);
				FlexBridgeDataVersion = fbAssemblyWithConstants
					.GetType("LibFLExBridgeChorusPlugin.Infrastructure.FlexBridgeConstants")
					?.GetField("FlexBridgeDataVersion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
					// When FLEx Bridge is available but the version cannot be determined (such as for FB 3.1 and earlier),
					// set the data version to an empty string. This will trigger the assert to let developers know if this becomes a problem again,
					// and will also let users know that updating FLEx Bridge will require everyone to update at the same time (LT-20019, LT-20778)
					?.GetRawConstantValue() as string ?? string.Empty;
				FlexBridgeVersion = fbAssemblyWithConstants.GetName().Version;
			}
			else
			{
				FlexBridgeDataVersion = null;
				FlexBridgeVersion = null;
			}
#if DEBUG
			// Don't pester developers who haven't set FLEx Bridge up.
			if (File.Exists(FullFieldWorksBridgePath()))
			{
				// This is not unit testable on build agents because they don't have FLEx Bridge installed.
				Debug.Assert(!string.IsNullOrWhiteSpace(FlexBridgeDataVersion),
					"FLEx Bridge has changed in a way that breaks model change warnings for automatic updates. " +
					"Please put FlexBridgeConstants.FlexBridgeDataVersion back where FLEx is looking.");
			}
#endif
		}

		/// <summary>
		/// Launches the FLExBridge application with the given commands and locks out the FLEx interface until the bridge
		/// is closed.
		/// </summary>
		/// <param name="projectFolder">The entire FieldWorks project folder path.
		/// Must include the project folder and project name with "fwdata" extension.
		/// Empty is OK if not send_receive command.</param>
		/// <param name="userName">the username to use in Chorus commits</param>
		/// <param name="command">obtain, start, send_receive, view_notes</param>
		/// <param name="projectGuid">Optional Lang Project guid, that is only used with the 'move_lift' command</param>
		/// <param name="liftModelVersionNumber">Version of LIFT schema that is supported by FLEx.</param>
		/// <param name="writingSystemId">The id of the first vernacular writing system</param>
		/// <param name="fwmodelVersionNumber">Current FDO model version number</param>
		/// <param name="onNonBlockerCommandComplete">Callback called when a non-blocker command has completed</param>
		/// <param name="changesReceived">true if S/R made changes to the project.</param>
		/// <param name="projectName">Name of the project to be opened after launch returns.</param>
		/// <returns>true if successful, false otherwise</returns>
		public static bool LaunchFieldworksBridge(string projectFolder, string userName, string command, string projectGuid,
			int fwmodelVersionNumber, string liftModelVersionNumber, string writingSystemId, Action onNonBlockerCommandComplete,
			out bool changesReceived, out string projectName)
			{
				return LaunchFieldworksBridge(projectFolder, userName, command, projectGuid, fwmodelVersionNumber, liftModelVersionNumber, writingSystemId, onNonBlockerCommandComplete,
					out changesReceived, out projectName, null, null, null, null);
			}

		/// <summary>
		/// Launches the FLExBridge application with the given commands and locks out the FLEx interface until the bridge
		/// is closed.
		/// </summary>
		/// <param name="projectFolder">The entire FieldWorks project folder path.
		/// Must include the project folder and project name with "fwdata" extension.
		/// Empty is OK if not send_receive command.</param>
		/// <param name="userName">the username to use in Chorus commits</param>
		/// <param name="command">obtain, start, send_receive, view_notes</param>
		/// <param name="projectGuid">Optional Lang Project guid, that is only used with the 'move_lift' command</param>
		/// <param name="liftModelVersionNumber">Version of LIFT schema that is supported by FLEx.</param>
		/// <param name="writingSystemId">The id of the first vernacular writing system</param>
		/// <param name="fwmodelVersionNumber">Current FDO model version number</param>
		/// <param name="onNonBlockerCommandComplete">Callback called when a non-blocker command has completed</param>
		/// <param name="changesReceived">true if S/R made changes to the project.</param>
		/// <param name="projectName">Name of the project to be opened after launch returns.</param>
		/// <param name="projectUri">Full URI of the project, if known beforehand.</param>
		/// <param name="name">The name of the project, if known beforehand.</param>
		/// <param name="credentialsPassword">The authentication credentials which will allow access to the repo to clone, if known beforehand.</param>
		/// <param name="repoIdentifier">The authentication credentials which will allow access to the repo to clone, if known beforehand.</param>
		/// <returns>true if successful, false otherwise</returns>
		public static bool LaunchFieldworksBridge(string projectFolder, string userName, string command, string projectGuid,
			int fwmodelVersionNumber, string liftModelVersionNumber, string writingSystemId, Action onNonBlockerCommandComplete,
			out bool changesReceived, out string projectName, Uri projectUri, string name, string credentialsPassword, string repoIdentifier)
		{
			_pipeID = string.Format(@"SendReceive{0}{1}", projectFolder, command);
			_flexBridgeTerminated = false;
			changesReceived = false;
			var args = "";
			projectName = "";
			_projectName = "";
			string userCredentials = null;
			if (projectUri != null)
			{
				var uriWithoutCredentials = projectUri.AbsoluteUri.Replace(projectUri.UserInfo + "@", "");
				AddArg(ref args, "-uri", uriWithoutCredentials);
				AddArg(ref args, "-project", name);
				AddArg(ref args, "-user", userName);
				AddArg(ref args, "-repositoryIdentifier", repoIdentifier);
				userCredentials = string.Join(":", userName, credentialsPassword);
			}

			var userNameActual = userName;
			if (string.IsNullOrEmpty(userName))
				userNameActual = Environment.UserName; // default so we can always pass something.
			if (userNameActual != null) // Paranoia, hopefully never null
			{
				AddArg(ref args, "-u", userNameActual);
			}
			if (!string.IsNullOrEmpty(projectFolder))
			{    // can S/R multiple projects simultaneously
				AddArg(ref args, "-p", projectFolder);
			}

			AddArg(ref args, "-v", command);

			if (command == SendReceive && FixItAppExists)
			{
				AddArg(ref args, "-f", FixItAppPathname);
			}

			if (!String.IsNullOrEmpty(projectGuid))
			{
				AddArg(ref args, "-g", projectGuid);
			}

			// Add two paths: to FW projDir & FW apps folder. Then, FB won't have to look in a zillion registry entries
			AddArg(ref args, "-projDir", FwDirectoryFinder.ProjectsDirectory);
			AddArg(ref args, "-fwAppsDir", FieldWorksAppsDir);
			// Tell Flex Bridge which model version of data are expected by FLEx.
			AddArg(ref args, "-fwmodel", fwmodelVersionNumber.ToString());
			AddArg(ref args, "-liftmodel", liftModelVersionNumber);
			// current culture may have country etc info after a hyphen. FlexBridge just needs the main language ID.
			// It probably can't ever be null or empty, but let's be as robust as possible.
			var locale = Thread.CurrentThread.CurrentUICulture.Name;

			// We don't use a plain "zh" locale.  Mono etc. need the country code for Chinese.  See FWNX-1255.
			if (locale != "zh-CN")
			{
				locale = string.IsNullOrWhiteSpace(locale) ? "en" : locale.Split('-')[0];
			}
			AddArg(ref args, "-locale", locale);

			if (_noBlockerHostAndCallback != null)
			{
				return false;
			}
			AddArg(ref args, "-pipeID", _pipeID);
			if (!String.IsNullOrEmpty(writingSystemId))
			{
				AddArg(ref args, "-ws", writingSystemId);
			}

			// make a new FLExBridge
			var host = IPCHostFactory.Create();
			host.VerbosityLevel = 1;
			if (!host.Initialize<FLExBridgeService, IFLExBridgeService>("FLExBridgeEndpoint" + _pipeID, AlertFlex, CleanupHost))
				return false;

			LaunchFlexBridge(host, command, args, onNonBlockerCommandComplete, userCredentials, ref changesReceived, ref projectName);

			return true;
		}

		private static void LaunchFlexBridge(IIPCHost host, string command, string args, Action onNonBlockerCommandComplete,
			string userPass, ref bool changesReceived, ref string projectName)
		{
			string flexbridgeLauncher = FullFieldWorksBridgePath();
			if (Platform.IsUnix)
			{
				flexbridgeLauncher = FwDirectoryFinder.FlexBridgeFolder + "/flexbridge";
			}
			if (!File.Exists(flexbridgeLauncher))
			{
				Console.WriteLine("Warning: Attempting to use non-existent flexbridge launcher {0}", flexbridgeLauncher);
			}

			// Launch the bridge process.
			using (var process = new Process())
			{
				var startInfo = new ProcessStartInfo();
				if (userPass != null) startInfo.EnvironmentVariables["CHORUS_CREDENTIALS"] = userPass;
				startInfo.UseShellExecute = false;
				startInfo.FileName = flexbridgeLauncher;
				startInfo.Arguments = args;

				process.StartInfo = startInfo;
				process.Start();
			}

			var nonFlexBlockers = new HashSet<string>
			{
				ConflictViewer,
				LiftConflictViewer,
				AboutFLExBridge,
				CheckForUpdates
			};
			if (nonFlexBlockers.Contains(command))
			{
				// This skips the piping and doesn't pause the Flex UI thread for the
				// two 'view' options and for the 'About Flex Bridge' and 'Check for Updates'.
				// We store the host and a callback so that, when FLExBridge quits, we can kill the host and call the callback.
				_noBlockerHostAndCallback = new Tuple<IIPCHost, Action>(host, onNonBlockerCommandComplete);
			}
			else
			{
				// This uses all the piping and also blocks the Flex UI thread, while Flex Bridge is running.
				using (new WaitCursor())
				{
					// Pause UI thread until FLEx Bridge terminates:
					Monitor.Enter(_waitObject);
					if (_flexBridgeTerminated == false)
						Monitor.Wait(_waitObject, -1);
					Monitor.Exit(_waitObject);

					projectName = _projectName;
					changesReceived = _receivedChanges;
				}
				KillTheHost(host);
			}
		}

		private static void KillTheHost(IIPCHost host)
		{
			// Let the service host cleanup happen in another thread so the user can get on with life.
			var letTheHostDie = new Thread(() =>
				{
					try
					{
						host.Close();
						var disposableHost = host as IDisposable;
						if(disposableHost != null)
							disposableHost.Dispose();
					}
					catch(Exception)
					{
						//we don't care anymore, just die.
					}
				});
			letTheHostDie.Start();
		}

		static IIPCClient _client;

		private static void BeginEmergencyExitChute(string pipeID)
		{
			try
			{
				_client = IPCClientFactory.Create();
				_client.VerbosityLevel = 1;
				_client.Initialize<IFLExService>("FLExEndpoint" + pipeID, _waitObject, CleanupHost);
				_client.RemoteCall("BridgeWorkOngoing", SignalCompletion);
			}
			catch (Exception)
			{
				CleanupHost();
			}
		}

		private static void AddArg(ref string extant, string flag, string value)
		{
			if (!string.IsNullOrEmpty(extant))
			{
				extant += " ";
			}
			extant += flag;
			if (!string.IsNullOrEmpty(value))
			{
				if (value.Any(Char.IsWhiteSpace))
				{
					extant += " \"" + value + "\"";
				}
				else
				{
					extant += " " + value;
				}
			}
		}

		/// <summary>
		/// Returns the full path and filename of the FieldWorksBridge executable
		/// </summary>
		/// <returns></returns>
		public static string FullFieldWorksBridgePath()
		{
			return Path.Combine(FwDirectoryFinder.FlexBridgeFolder, FLExBridgeName);
		}

		/// <summary>
		/// Returns the full path and filename of the FieldWorksBridge executable
		/// </summary>
		/// <returns></returns>
		public static bool FixItAppExists
		{
			get
			{
				var fixitAppPathname = FixItAppPathname;
				return !string.IsNullOrEmpty(fixitAppPathname) && File.Exists(fixitAppPathname);
			}
		}

		/// <summary>
		/// Determines if FLExBridge is installed.
		/// </summary>
		/// <returns><c>true</c> if is flex bridge installed; otherwise, <c>false</c>.</returns>
		public static bool IsFlexBridgeInstalled()
		{
			string fullName = FullFieldWorksBridgePath();
			return FileUtils.FileExists(fullName); // Flex Bridge exe has to exist
		}

		/// <summary>
		/// Answer whether the project appears to have a FLEx repo. This is currently determined by its having a .hg folder.
		/// </summary>
		/// <returns></returns>
		public static bool DoesProjectHaveFlexRepo(IProjectIdentifier projectId)
		{
			// useful to return false on null for some unit tests
			return projectId != null && IsMercurialRepo(projectId.ProjectFolder);
		}

		/// <summary>
		/// Answer whether the project appears to have a LIFT repo.
		/// </summary>
		/// <returns></returns>
		public static bool DoesProjectHaveLiftRepo(IProjectIdentifier projectId)
		{
			// useful to return false on null for some unit tests
			if (projectId == null)
				return false;
			string otherRepoPath = Path.Combine(projectId.ProjectFolder, LcmFileHelper.OtherRepositories);
			if (!Directory.Exists(otherRepoPath))
				return false;
			string liftFolder = Directory.EnumerateDirectories(otherRepoPath, "*_LIFT").FirstOrDefault();
			return !String.IsNullOrEmpty(liftFolder) && IsMercurialRepo(liftFolder);
		}

		private static bool IsMercurialRepo(string path)
		{
			return Directory.Exists(Path.Combine(path, ".hg"));
		}

		/// <summary>
		/// Returns the full path and filename of the FixFwData executable
		/// </summary>
		/// <returns></returns>
		public static string FixItAppPathname
		{
			get
			{
				return Path.Combine(FieldWorksAppsDir, "FixFwData.exe");
			}
		}

		/// <summary>
		/// Returns the full path to where the FieldWorks running apps are located
		/// </summary>
		/// <returns></returns>
		public static string FieldWorksAppsDir
		{
			get
			{
				return Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
			}
		}

		#region Service classes and methods For Bridge calls to FLEx

		/// <summary>
		/// The service class
		/// </summary>
		[ServiceBehavior(UseSynchronizationContext = false)] //Create new threads for the services, don't tie them into the main UI thread.
		private class FLExBridgeService : IFLExBridgeService
		{
			#region Implementation of IFLExBridgeService

			/// <summary>
			/// This method signals that FLExBridge completed normally.
			/// </summary>
			/// <param name="changesReceived">true if the send/receive or other operation resulted in local changes</param>
			public void BridgeWorkComplete(bool changesReceived)
			{
				_receivedChanges = changesReceived;
				AlertFlex();
				CleanupHost();
			}

			public void BridgeReady()
			{
				BeginEmergencyExitChute(_pipeID);
			}

			public void InformFwProjectName(string fwProjectName)
			{
				_projectName = fwProjectName;
			}

			/// <summary>
			/// FLExBridge user clicked on the title of a particular conflict in the conflict report.
			/// </summary>
			/// <param name="jumpUrl">Url of the FLEx object to jump to.</param>
			public void BridgeSentJumpUrl(string jumpUrl)
			{
				if (FLExJumpUrlChanged != null)
					FLExJumpUrlChanged(this, new FLExJumpEventArgs(jumpUrl));
			}
			#endregion
		}

		/// <summary>
		/// Acquire the lock on, and then pulse the wait object
		/// </summary>
		public static void AlertFlex()
		{
			Monitor.Enter(_waitObject); //acquire the lock on the _waitObject
			_flexBridgeTerminated = true;
			Monitor.Pulse(_waitObject); //notify a thread waiting on _waitObject that it may continue.
			Monitor.Exit(_waitObject); //release the lock on the _waitObject so they actually can continue.
		}

		/// <summary>
		/// Service interface for methods in FLEx that FLExBridge can call.
		/// </summary>
		[ServiceContract]
		private interface IFLExBridgeService
		{
			[OperationContract]
			void BridgeWorkComplete(bool changesReceived);

			[OperationContract]
			void BridgeReady();

			[OperationContract]
			void InformFwProjectName(string fwProjectName);

			[OperationContract]
			void BridgeSentJumpUrl(string jumpUrl);
		}

		#endregion
		#region Service classes and methods for FLEx calls to Bridge
		/// <summary>
		/// Service interface for the methods in FLEXBridge that we can call
		/// </summary>
		[ServiceContract]
		private interface IFLExService : IClientChannel
		{
			[OperationContract(AsyncPattern = true)]
			IAsyncResult BeginBridgeWorkOngoing(AsyncCallback callback, object asyncState);

			void EndBridgeWorkOngoing(IAsyncResult result);
		}

		/// <remarks>Called when the connection with FLExBridge terminates (currently ~15 seconds after the user closes the FB window)</remarks>
		static void CleanupHost()
		{
			Console.WriteLine(@"FLExBridgeHelper.CleanupHost()");
			if (_noBlockerHostAndCallback != null)
			{
				KillTheHost(_noBlockerHostAndCallback.Item1);
				if(_noBlockerHostAndCallback.Item2 != null)
					_noBlockerHostAndCallback.Item2();
				_noBlockerHostAndCallback = null;
			}
		}

		static void SignalCompletion()
		{
			Console.WriteLine(@"FLExBridgeHelper.SignalCompletion()");
			_flexBridgeTerminated = true;
		}
		#endregion
	}

	/// <summary>
	/// Event args plus jump URL
	/// </summary>
	public class FLExJumpEventArgs : EventArgs
	{
		private readonly string _jumpUrl;

		/// <summary>
		/// Set up event args with a URL to jump to.
		/// </summary>
		/// <param name="jumpUrl"></param>
		public FLExJumpEventArgs(string jumpUrl)
		{
			_jumpUrl = jumpUrl;
		}

		/// <summary>
		/// URL that FLEx should jump to when processing this event.
		/// </summary>
		public string JumpUrl
		{
			get { return _jumpUrl; }
		}
	}

}
