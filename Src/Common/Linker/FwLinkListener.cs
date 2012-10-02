// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwLinkListener.cs
// Authorship History: John Hatton
// Last reviewed:
//
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;

namespace SIL.FieldWorks.Linking
{
	/// <summary>
	/// JohnT: I don't understand this fully, but FWIW, this is part of the architecture that
	/// allows FieldWorks applications to be targets of URLs.
	/// If it's not working on a development machine, you may need to add a registry entry
	/// by double-clicking fw\src\Common\FieldWorksLink\HookupSILFWToC-FW-Output-Debug.reg.
	/// Compare fw\src\FdoUi\FdoUiLowLevel\FwLinkReceiver for another key part.
	/// </summary>
	public class FwLinkListener : MarshalByRefObject
	{
		// Track whether Dispose has been called.
		private bool disposed = false;
		protected static int s_port;
		protected static string s_appName;

		public delegate void LinkEventHandler(FwLink link);

		//takes a long time to look for each one, so we should keep this small until we fixed that problem.
		protected const int kMaxApplicationsOfOneType = 2;

		/// <summary>subscribed to this to be informed when there is a link request</summary>
		protected static event LinkEventHandler OnLinkRequest;

		/// <summary>
		/// Clients should instead use the factory method. This is needed by the remoting system.
		/// </summary>
		public FwLinkListener()
		{
		}

		/*		/// <summary>
				///
				/// </summary>
				/// <param name="applicationName"></param>
				/// <returns></returns>
				protected static int GetPortBaseForApplication (string applicationName)
				{
					switch(applicationName)
					{
						case "TestA": return 50;
						case "TestB": return 51;
						case "TestC": return 52;
						case "Flexicon": return 0;
						case "ME": return 1;
						case "IText": return 2;
						case "TE": return 3;
							// "DN" is not listed until it gets the ability to listen to links
						default: throw new ApplicationException (applicationName+ " is not known to the FwLinkListener class.");
					}
				}

				/// <summary>
				///
				/// </summary>
				/// <param name="applicationName"></param>
				/// <returns></returns>
				protected static int GetStartingPortForApplication (string applicationName)
				{
					return (GetPortBaseForApplication(applicationName) * 10) + 50000;
				}
		*/
		static protected string GetPortPath(int port, string appName)
		{
			return "tcp://localhost:"+ port.ToString()+"/"+appName+"/listener";
		}

		/*		/// <summary>
				///
				/// </summary>
				/// <param name="applicationName"></param>
				/// <returns></returns>
				protected static int RegisterNextAvailablePortForApplication (string applicationName)
				{
					string[] paths= new string[kMaxApplicationsOfOneType];
					//find a port that is not in use
					int start =GetStartingPortForApplication(applicationName);
					for(int port = start; port< start + kMaxApplicationsOfOneType;port++)
					{
						try
						{
							ChannelServices.RegisterChannel(new TcpChannel(port));
						}
						catch(System.Net.Sockets.SocketException)
						{
							continue;
						}
						return port;
					}
					throw new ApplicationException("Could not find an open port for this application to use.");
				}

				protected static string[] GetPossiblePathsToApplication(string applicationName)
				{
					int start = GetStartingPortForApplication(applicationName);
					string[] paths= new string[kMaxApplicationsOfOneType];
					for(int port = start; port < start + kMaxApplicationsOfOneType; port++)
					{
						paths[port-start] = GetPortPath(port, applicationName);
					}
					return paths;
				}
		*/

		public static void StopListening()
		{
			FwBroker broker = FwBroker.GetBroker();
			broker.Unregister(s_appName, s_port);
		}

		/// <summary>
		/// a factory method which creates the listener and makes it start listening.
		/// </summary>
		/// <param name="appName"></param>
		/// <returns></returns>

		public static void StartListening(string appName, LinkEventHandler handler)
		{
			s_appName = appName;
			FwBroker broker = FwBroker.GetBroker();


			s_port =broker.RegisterAndGetPort(appName);

			FwLinkListener.OnLinkRequest += handler;

			ChannelServices.RegisterChannel(new TcpChannel(s_port));
			RemotingConfiguration.ApplicationName = appName;

			RemotingConfiguration.RegisterWellKnownServiceType( typeof(FwLinkListener),
				"listener",
				WellKnownObjectMode.Singleton
				);

			//although we don't really need to use it, we create this object now
			//so that we can preload with appropriate properties.
			string path = FwLinkListener.GetPortPath(s_port, appName);
			FwLinkListener listener = (FwLinkListener)Activator.GetObject(typeof(FwLinkListener), path);

			if (listener == null)
				throw new ApplicationException("Could not create the initial listener for this application");

			listener.Test();
		}

		/// <summary>
		/// used by remote clients to see that they actually have a real connection (a kludge)
		/// </summary>
		public void Test()
		{

		}

		/// <summary>
		/// Attempts to find an application which matches the link, and passes the link to that application.
		/// Does not attempt to launch new applications.
		/// </summary>
		/// <param name="link"></param>
		/// <returns>true if it successfully linked to a running application</returns>
		public static  bool AttemptLink(FwLink link)
		{
			try
			{
				FwBroker broker = FwBroker.GetBroker();
				int port =broker.GetRunningApplicationPort(link.ApplicationName);

				//note that this will not fail, even if no one is listening on that channel
				FwLinkListener listener = (FwLinkListener)Activator.GetObject(typeof(FwLinkListener), GetPortPath(port, link.ApplicationName));
				//but this will fail if we did not really connect to a listener
				listener.Request(link);
				return true;
			}
			catch(Exception error)
			{
			}
			return false;
		}

		/// <summary>
		/// called by the FwLink, which may be running in another application.
		/// </summary>
		/// <param name="link"></param>
		public void Request(FwLink link)
		{
			if (OnLinkRequest != null )
			{
				//trigger the event on anyone who has subscribed to it
				OnLinkRequest(link);
			}
		}

		// Implement IDisposable.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
/*		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly
		// or indirectly by a user's code. Managed and unmanaged resources
		// can be disposed.
		// If disposing equals false, the method has been called by the
		// runtime from inside the finalizer and you should not reference
		// other objects. Only unmanaged resources can be disposed.
		private void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if(!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if(disposing)
				{
					Debug.Fail("it is important to call Dispose() on FwLinkListener.");
				}

			}
			disposed = true;
		}
*/
	}
}
