using System;
using System.Diagnostics;
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

		private const string FLExBridgeName = @"FLExBridge.exe";

		private static object waitObject = new object();
		private static string sjumpUrl;
		private static bool _receivedChanges; // true if changes merged via FLExBridgeService.BridgeWorkComplete()
		private static string _projectName; // fw proj path via FLExBridgeService.InformFwProjectName()

		/// <summary>
		/// Launches the FLExBridge application with the given commands and locks out the FLEx interface until the bridge
		/// is closed.
		/// </summary>
		/// <param name="projectFolder">optional</param>
		/// <param name="userName"></param>
		/// <param name="command"></param>
		/// <param name="projectName">Name of the project to be opened after launch returns.</param>
		/// <param name="url">If this out param is other than null, it is a link to jump to in FLEx.</param>
		public static bool LaunchFieldworksBridge(string projectFolder, string userName, string command, out string projectName, out string url)
		{
			sjumpUrl = "";
			string args = "";
			if (userName != null)
			{
				AddArg(ref args, "-u", userName);
			}
			if (projectFolder != null)
			{
				AddArg(ref args, "-p", projectFolder);
			}
			AddArg(ref args, "-v", command);
			using (ServiceHost host = new ServiceHost(typeof (FLExBridgeService),
				new[] {new Uri("net.pipe://localhost/FLExBridgeEndpoint")}))
			{
				//open host ready for business
				host.AddServiceEndpoint(typeof(IFLExBridgeService), new NetNamedPipeBinding(), "FLExPipe");
				host.Open();
				//Start up a thread to wait until the bridge work is completed.
				Thread waitOnBridgeThread = new Thread(WaitOnBridgeMethod);
				waitOnBridgeThread.Start();
				//Launch the bridge process.
				using (Process.Start(FullFieldWorksBridgePath(), args));
				Cursor.Current = Cursors.WaitCursor;
				waitOnBridgeThread.Join();
				//Join the thread so that messages are still pumped but we halt until FieldworksBridge awakes us.
				projectName = _projectName;
				Cursor.Current = Cursors.Default;
				//let the service host cleanup happen in another thread so the user can get on with life.
				Thread letTheHostDie = new Thread(host.Close);
				letTheHostDie.Start();

				url = sjumpUrl;
				return _receivedChanges;
			}
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
				using (var factory = new ChannelFactory<IFLExServiceChannel>(new NetNamedPipeBinding(),
					new EndpointAddress("net.pipe://localhost/FLExEndpoint/FLExPipe")))
				{
					var channelClient = factory.CreateChannel();
					channelClient.OperationTimeout = TimeSpan.MaxValue;
					channelClient.BeginBridgeWorkOngoing(WorkDoneCallback, channelClient);
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
			}
			finally
			{
				Monitor.Exit(waitObject); //release the lock on waitObject
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
			/// This method signals that FLExBridge completed normally, but with a URL to jump to.
			/// </summary>
			/// <param name="changesReceived">true if the send/receive or other operation resulted in local changes</param>
			/// <param name="jumpUrl">If we use this method, it's because the user clicked on a conflict title link.</param>
			public void BridgeWorkComplete(bool changesReceived, string jumpUrl)
			{
				_receivedChanges = changesReceived;
				sjumpUrl = jumpUrl;
				AlertFLEx();
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
			void BridgeWorkComplete(bool changesReceived, string jumpUrl);

			[OperationContract]
			void BridgeReady();

			[OperationContract]
			void InformFwProjectName(string fwProjectName);
		}

		#endregion
		#region Service classes and methods for FLEx calls to Bridge
		/// <summary>
		/// Service interface for the methods in FLEXBridge that we can call
		/// </summary>
		[ServiceContractAttribute]
		private interface IFLExService
		{

			[OperationContract]
			void BridgeWorkOngoing();

			[OperationContractAttribute(AsyncPattern = true)]
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
		/// This client
		/// </summary>
		private class FLExServiceClient : ClientBase<IFLExService>, IFLExService
		{
			public void BridgeWorkOngoing()
			{
				Channel.BridgeWorkOngoing();
			}

			public IAsyncResult BeginBridgeWorkOngoing(AsyncCallback callback, object asyncState)
			{
				return Channel.BeginBridgeWorkOngoing(callback, asyncState);
			}

			public void EndBridgeWorkOngoing(IAsyncResult result)
			{
				Channel.EndBridgeWorkOngoing(result);
			}
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
				Monitor.Exit(waitObject);
			}
		}

		#endregion
	}
}
