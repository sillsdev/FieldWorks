using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Utility methods for FLExBridge interaction
	/// </summary>
	public class FLExBridgeHelper
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
		/// <para>Returns the pathname to either the 'fwdata' xml file or the lift file, if FLEx Bridge was able to get a clone. Returns null, if no clone was created.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string ObtainLift = @"obtain_lift";

		/// <summary>
		/// constant for launching the bridge in the Conflict\Notes Viewer mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file.</para>
		/// <para>Nothing is returned, since FLEx Bridge's notes dialog will remain open.
		/// As the user selects some note, a call back FELx will be done, with the URL for the item to jump to.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string ConflictViewer = @"view_notes"; // REVIEW (JohnT): Do we need anohter option for viewing the lift notes 'view_notes_lift'?

		/// <summary>
		/// constant for launching the bridge in the undo export mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file. Nothing is returned.</para>
		/// <para>Flex Bridge restores the local working folder to what is in the lift repository, including deleting any new files.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string UndoExportLift = @"undo_export_lift";

		#endregion End of available '-v' parameter options:

		// The two paths of a path that locate the Lift repository within a FLEx project.
		/// <summary>
		/// constant for locating the nested lift repository (part 1 of 2)
		/// </summary>
		public const string OtherRepositories = @"OtherRepositories";
		/// <summary>
		/// constant for locating the nested lift repository (part 2 of 2)
		/// </summary>
		public const string LIFT = @"LIFT";

		/// <summary>
		/// Project name grafted to the pipe URI so multiple projects can S/R simultaneously
		/// </summary>
		private static string _sFwProjectName = "";

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

		private static object waitObject = new object();
		private static bool flexBridgeTerminated;
		private static object conflictHost;
		private static bool _receivedChanges; // true if changes merged via FLExBridgeService.BridgeWorkComplete()
		private static string _projectName; // fw proj path via FLExBridgeService.InformFwProjectName()

		/// <summary>
		/// Launches the FLExBridge application with the given commands and locks out the FLEx interface until the bridge
		/// is closed.
		/// </summary>
		/// <param name="projectFolder">The entire FieldWorks project folder path.
		/// Must include the project folder and project name with "fwdata" extension.
		/// Empty is OK if not send_receive command.</param>
		/// <param name="userName">TBD: someone should explain what this is for</param>
		/// <param name="command">obtain, start, send_receive, view_notes</param>
		/// <param name="changesReceived">true if S/R made changes to the project.</param>
		/// <param name="projectName">Name of the project to be opened after launch returns.</param>
		/// <returns>true if successful, false otherwise</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ServiceHost gets disposed in KillTheHost()")]
		public static bool LaunchFieldworksBridge(string projectFolder, string userName, string command,
			out bool changesReceived, out string projectName)
		{
			flexBridgeTerminated = false;
			changesReceived = false;
			string args = "";
			projectName = "";
			_projectName = "";
			_sFwProjectName = "";
			var userNameActual = userName;
			if (string.IsNullOrEmpty(userName))
				userNameActual = Environment.UserName; // default so we can always pass something.
			if (userNameActual != null) // Paranoia, hopefully never null
			{
				AddArg(ref args, "-u", userNameActual);
			}
			if (!String.IsNullOrEmpty(projectFolder))
			{    // can S/R multiple projects simultaneously
				AddArg(ref args, "-p", projectFolder);
				_sFwProjectName = Path.GetFileNameWithoutExtension(projectFolder);
			}
			AddArg(ref args, "-v", command);
			if (conflictHost != null)
			{
				return false;
			}

			// make a new FLExBridge
			ServiceHost host = null;
			try
			{
				host = new ServiceHost(typeof (FLExBridgeService),
										new[] {new Uri("net.pipe://localhost/FLExBridgeEndpoint" + _sFwProjectName)});

				//open host ready for business
				host.AddServiceEndpoint(typeof (IFLExBridgeService), new NetNamedPipeBinding(), "FLExPipe");
				host.Open();
			}
			catch (InvalidOperationException) // Can happen if Conflict Report is open and we try to run FLExBridge again.
			{
				if (host != null)
					((IDisposable) host).Dispose();
				return false; // Unsuccessful startup. Caller should report duplicate bridge launch.
			}
			catch (AddressAlreadyInUseException) // Can happen if FLExBridge has been launched and we try to launch FLExBridge again.
			{
				// host is normally not null for this exception, but there is no pipe to dispose
				return false; // Unsuccessful startup. Caller should report duplicate bridge launch.
			}
			//Launch the bridge process.
			if (command == ConflictViewer)
			{
				LaunchConflictViewer(args); // launching separately here avoids blocking FLEx while viewer is open.
				conflictHost = host; // so we can kill the host when the bridge quits
			}
			else
			{
				//Launch the bridge process.
				using (Process.Start(FullFieldWorksBridgePath(), args))
				{
				}
				Cursor.Current = Cursors.WaitCursor;

				// Pause UI thread until FLEx Bridge terminates:
				Monitor.Enter(waitObject);
				if (flexBridgeTerminated == false)
					Monitor.Wait(waitObject, -1);
				Monitor.Exit(waitObject);

				projectName = _projectName;
				changesReceived = _receivedChanges;
				Cursor.Current = Cursors.Default;
				KillTheHost(host);
			}
			return true;
		}

		private static void KillTheHost(ServiceHost host)
		{
			// Let the service host cleanup happen in another thread so the user can get on with life.
			Thread letTheHostDie = new Thread(() =>
												{
													try
													{
														host.Close();
														((IDisposable) host).Dispose();
													}
													catch(Exception)
													{
														//we don't care anymore, just die.
													}
												});
			letTheHostDie.Start();
		}

		private static void LaunchConflictViewer(string args)
		{
			using (Process.Start(FullFieldWorksBridgePath(), args))
			{   // don't bother with all the pipes for the Conflict Report
			}
		}

		private static void BeginEmergencyExitChute()
		{
			var factory = new ChannelFactory<IFLExServiceChannel>
				(new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/FLExEndpoint" + _sFwProjectName + "/FLExPipe"));
			var channelClient = factory.CreateChannel();
			channelClient.OperationTimeout = TimeSpan.MaxValue;
			channelClient.BeginBridgeWorkOngoing(WorkDoneCallback, channelClient);
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
				bool hasWhitespace;
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
			return Path.Combine(DirectoryFinder.FlexBridgeFolder, FLExBridgeName);
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
				AlertFLEx();
				if (conflictHost != null)
				{
					KillTheHost((ServiceHost) conflictHost);
					conflictHost = null;
				}
			}

			public void BridgeReady()
			{
				BeginEmergencyExitChute();
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

			/// <summary>
			/// Acquire the lock on, and then pulse the wait object
			/// </summary>
			private void AlertFLEx()
			{
				Monitor.Enter(waitObject); //acquire the lock on the waitObject
				flexBridgeTerminated = true;
				Monitor.Pulse(waitObject); //notify a thread waiting on waitObject that it may continue.
				Monitor.Exit(waitObject); //release the lock on the waitObject so they actually can continue.
			}
			#endregion
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
		private interface IFLExService
		{
			[OperationContract(AsyncPattern = true)]
			IAsyncResult BeginBridgeWorkOngoing(AsyncCallback callback, object asyncState);

			void EndBridgeWorkOngoing(IAsyncResult result);
		}

		/// <summary>
		/// This interface combines the service and channel objects so a factory can give us a useful oboject
		/// </summary>
		private interface IFLExServiceChannel : IFLExService, IClientChannel
		{
		}

		/// <summary>
		/// This callback mostly serves to help us terminate in exceptional cases.
		/// It is not reliable for return data because it is asynchronous, and FLExBridge might close before we retrieve the data
		/// </summary>
		/// <param name="iar"></param>
		private static void WorkDoneCallback(IAsyncResult iar)
		{
			Monitor.Enter(waitObject);
			flexBridgeTerminated = true;
			try
			{
				Monitor.Pulse(waitObject);
				((IFLExServiceChannel)iar.AsyncState).EndBridgeWorkOngoing(iar);
			}
			catch(CommunicationException)
			{
				//Something went wrong with the communication to the Bridge. Possibly it died unexpectedly, wake up FLEx
				Monitor.Pulse(waitObject);
			}
			catch (Exception e)
			{
				Logger.WriteError(e); //Write the log entry, but likely not important
			}
			finally
			{
				if (conflictHost != null)
				{
					KillTheHost((ServiceHost) conflictHost);
					conflictHost = null;
				}
				Monitor.Exit(waitObject);
			}
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
