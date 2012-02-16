using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;

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
		public const string ConflictViewer = "view_notes";

		private const string FLExBridgeName = @"FieldWorksBridge.exe";

		private static object waitObject = new object();
		private static bool receivedChanges;

		/// <summary>
		/// Launches the FLExBridge application with the given commands
		/// </summary>
		/// <param name="projectFolder">optional</param>
		/// <param name="userName"></param>
		/// <param name="command"></param>
		public static bool LaunchFieldworksBridge(string projectFolder, string userName, string command)
		{
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
			Process.Start(FullFieldWorksBridgePath(), args);
			using (ServiceHost host = new ServiceHost(typeof(FLExBridgeService),
											   new[] { new Uri("net.pipe://localhost/FLExBridgeEndpoint") }))
			{
				//open host ready for business
				host.AddServiceEndpoint(typeof (IFLExBridgeService), new NetNamedPipeBinding(), "FLExPipe");
				host.Open();
				Thread waitOnBridgeThread = new Thread(WaitOnBridgeMethod);
				waitOnBridgeThread.Start();
				Cursor.Current = Cursors.WaitCursor;
				waitOnBridgeThread.Join();
					//Join the thread so that messages are still pumped but we halt until FieldworksBridge awakes us.
				Cursor.Current = Cursors.Default;
				host.Close();
			}
			return receivedChanges;
		}

		/// <summary>
		/// This method will block and do nothing until it is notified that the bridge has exited (normally or abnormally)
		/// </summary>
		private static void WaitOnBridgeMethod()
		{
			Monitor.Enter(waitObject); //claim\acquire the lock on the waitObject no other threads may acquire a lock until it is released
			try
			{
				while (true)
				{
					try
					{
						//wait for a notify\pulse event and release the lock to other threads, re-aquires the lock before continueing.
						Monitor.Wait(waitObject);
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

		[ServiceBehavior(UseSynchronizationContext = false)]
		private class FLExBridgeService : IFLExBridgeService
		{
			#region Implementation of IFLExBridgeService

			public void BridgeWorkComplete(bool changesReceived)
			{
				receivedChanges = changesReceived;
				Monitor.Enter(waitObject); //acquire the lock on the waitObject
				Monitor.Pulse(waitObject); //notify a thread waiting on waitObject that it may continue.
				Monitor.Exit(waitObject); //release the lock on the waitObject so they actually can continue.
			}

			#endregion
		}

		[ServiceContract]
		private interface IFLExBridgeService
		{
			[OperationContract]
			void BridgeWorkComplete(bool changesReceived);
		}
	}
}
