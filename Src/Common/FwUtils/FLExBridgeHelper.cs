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
		//public constants for the various views/actions (the -v param)
		/// <summary>
		/// constant for launching the bridge in Send and Receive mode
		/// </summary>
		public const string SendReceive = @"send_receive";
		/// <summary>
		/// constant for launching the bridge in Obtain project mode
		/// </summary>
		public const string Obtain = @"obtain";
		/// <summary>
		/// constant for launching the bridge in the Conflict\Notes Viewer mode
		/// </summary>
		public const string ConflictViewer = @"view_notes";

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
		private static object conflictHost;
		private static bool _receivedChanges; // true if changes merged via FLExBridgeService.BridgeWorkComplete()
		private static string _projectName; // fw proj path via FLExBridgeService.InformFwProjectName()

		/// <summary>
		/// Launches the FLExBridge application with the given commands and locks out the FLEx interface until the bridge
		/// is closed.
		/// </summary>
		/// <param name="projectFolder">optional</param>
		/// <param name="userName"></param>
		/// <param name="command"></param>
		/// <param name="changesReceived">true if S/R made changes to the project.</param>
		/// <param name="projectName">Name of the project to be opened after launch returns.</param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ServiceHost gets disposed in KillTheHost()")]
		public static bool LaunchFieldworksBridge(string projectFolder, string userName, string command,
			out bool changesReceived, out string projectName)
		{
			changesReceived = false;
			string args = "";
			projectName = "";
			if (userName != null)
			{
				AddArg(ref args, "-u", userName);
			}
			if (projectFolder != null)
			{
				AddArg(ref args, "-p", projectFolder);
			}
			AddArg(ref args, "-v", command);
			ServiceHost host = null;
			try
			{
				host = new ServiceHost(typeof(FLExBridgeService),
												   new[] {new Uri("net.pipe://localhost/FLExBridgeEndpoint")});

				//open host ready for business
				host.AddServiceEndpoint(typeof(IFLExBridgeService), new NetNamedPipeBinding(), "FLExPipe");
				host.Open();
			}
			catch (InvalidOperationException) // Can happen if Conflict Report is open and we try to run FLExBridge again.
			{
				if (host != null)
					((IDisposable)host).Dispose();
				return false; // Unsuccessful startup. Caller should report duplicate bridge launch.
			}
			//Start up a thread to wait until the bridge work is completed.
			Thread waitOnBridgeThread = new Thread(WaitOnBridgeMethod);
			waitOnBridgeThread.Start();
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
				//Join the thread so that messages are still pumped but we halt until FieldworksBridge awakes us.
				waitOnBridgeThread.Join();
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
			Thread letTheHostDie = new Thread(() => { host.Close(); ((IDisposable)host).Dispose(); });
			letTheHostDie.Start();
		}

		private static void LaunchConflictViewer(string args)
		{
			using (Process.Start(FullFieldWorksBridgePath(), args)) {} // don't bother with all the pipes for the Conflict Report
		}

		/// <summary>
		/// This method will block and do nothing until it is notified that the bridge has exited (normally or abnormally)
		/// </summary>
		private static void WaitOnBridgeMethod()
		{
			Monitor.Enter(waitObject); //claim\acquire the lock on the waitObject no other threads may acquire a lock until it is released
			try
			{
				//wait until we are notified that the bridge is listening.
				Monitor.Wait(waitObject, -1); // infinite timeout
				BeginEmergencyExitChute();
				while (true)
				{
					try
					{
						//wait for a notify\pulse event and release the lock to other threads, re-aquires the lock before continueing.
						Monitor.Wait(waitObject, -1); // infinite timeout
						break;
					}
					catch (ThreadInterruptedException)
					{
						continue; //This bizarre case is usually the result of spurious hardware interrupts, we still want to wait
					}
					//all other exceptions should bust out of this method
				}
			}
			finally
			{
				Monitor.Exit(waitObject); //release the lock on waitObject
			}
		}

		private static void BeginEmergencyExitChute()
		{
			using (var factory = new ChannelFactory<IFLExServiceChannel>(new NetNamedPipeBinding(),
				new EndpointAddress("net.pipe://localhost/FLExEndpoint/FLExPipe")))
			{
				var channelClient = factory.CreateChannel();
				channelClient.OperationTimeout = TimeSpan.MaxValue;
				channelClient.BeginBridgeWorkOngoing(WorkDoneCallback, channelClient);
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
					KillTheHost((ServiceHost)conflictHost);
			}

			public void BridgeReady()
			{
				AlertFLEx();
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
					KillTheHost((ServiceHost)conflictHost);
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
