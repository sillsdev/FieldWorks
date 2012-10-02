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
// File: FwBroker.cs
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

using System.Collections;

namespace SIL.FieldWorks.Linking
{

	/// <summary>
	/// this class gives us a way for FieldWorks applications to find each other
	/// said that they can communicate over tcp ports.
	/// </summary>
	public class FwBroker : MarshalByRefObject
	{
		protected int m_nextPortToAssign = 6000;
		protected System.Collections.Hashtable m_registeredApplications;

		/// <summary>
		/// the port that the broker itself is listening on.
		/// </summary>
		static public int Port
		{
			get
			{
				return 5999;
			}
		}

		/// <summary>
		/// Clients should instead use the factory method. This is needed by the remoting system.
		/// </summary>
		public FwBroker()
		{
			m_registeredApplications= new System.Collections.Hashtable(20);
		}

		/// <summary>
		/// call this occasionally to check on things and decide if the process should exit
		/// </summary>
		/// <returns>true if the process should exit</returns>
		public bool Check()
		{
			//if there are no longer any registered applications, that means the user has
			//exited the last FieldWorks application, so we should quit, too.
			return (m_registeredApplications.Count == 0);
		}

		public static string ObjectUri
		{
			get
			{
				return "fwbroker";
			}
		}

		public static string Url
		{
			get {return "tcp://localhost:"+ Port.ToString()+"/"+ObjectUri;}
		}

		public static FwBroker GetBroker()
		{
			//about tempChannel: this is the result of a better frustrated attempts to
			//understanding control automatic vs. explicit channel creation.
			//our current design calls for the broker to tell us what port we should use.
			//however, even in talking to the broker, we apparently get a port set up
			//by .net, automatically. This then prevents us from moving to the port we were assigned
			//by the broker. So, for now, the best clue to have come up with is to
			//just grab an arbitrary, hardcoded port temporarily for use in getting in touch with the broker.
			//then, we can register this port before relief this method. If, when this method
			//is entered, we already have a channel registered, then don't bother with the temporary
			//port at all.

			TcpChannel tempChannel=null;
			try
			{
				if(ChannelServices.RegisteredChannels.Length == 0)
				{
					ChannelServices.RegisterChannel(new TcpChannel(7129));
					tempChannel = (TcpChannel)ChannelServices.RegisteredChannels[0];
				}

				FwBroker broker = (FwBroker) Activator.GetObject(typeof(FwBroker), Url);

				//unfortunately, that GetObject call will appear to have succeeded even if it
				//actually did not get us anything useful. The only way I have figured out
				//how to check that it really work is to actually make a call on the proxy it returned.

				try
				{
					broker.Test();
					return broker;
				}
				catch (Exception error)
				{
					Process p = Process.Start(@"C:\WW\Src\Common\Linker\Broker\bin\Debug\Broker.exe");
					broker =(FwBroker) Activator.GetObject(typeof(FwBroker), Url);
					broker.Test();
					return broker;

				}
			}
			finally
			{
				if(tempChannel != null)
					ChannelServices.UnregisterChannel(tempChannel);
			}
			return null;
		}

		/// <summary>
		/// used by remote clients to see that they actually have a real connection (a kludge)
		/// </summary>
		public void Test()
		{

		}

		/// <summary>
		/// register this instance of the client, so their clients can find them,
		/// and get a TCP port to use
		/// </summary>
		/// <param name="applicationName"></param>
		/// <returns></returns>
		public int RegisterAndGetPort(string applicationName)
		{
			if(PortsForApp(applicationName) == null)
				m_registeredApplications[applicationName] = new System.Collections.ArrayList();
			//notice that we don't bother reusing imports.
			PortsForApp(applicationName).Add(m_nextPortToAssign);
			int x = m_nextPortToAssign;
			++m_nextPortToAssign;
			Console.WriteLine("Assigning "+applicationName+" --> " +x.ToString());
			return x;
		}

		/// <summary>
		/// client should call this before exiting, otherwise other clients will
		/// try to find them on the port which they are not using anymore.
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="port"></param>
		public void Unregister(string applicationName, int port)
		{
			Debug.Assert(PortsForApp(applicationName) != null);
			PortsForApp(applicationName).Remove(port);

			//this is important to do, so that we know when we no longer have any
			//running registered applications, and we can exit this process altogether.
			if(PortsForApp(applicationName).Count == 0)
				m_registeredApplications.Remove(applicationName);

			Console.WriteLine("Revoking "+applicationName+" : " +port.ToString());

		}

		private ArrayList PortsForApp(string applicationName)
		{
			return ((ArrayList)m_registeredApplications[applicationName]);
		}

		public int GetRunningApplicationPort(string applicationName)
		{
			Console.WriteLine("Got request for "+applicationName);
			if(PortsForApp(applicationName) == null)
			{
				ListPorts();
				return 0; //no applications of that type have ever registered
			}

			ArrayList list = PortsForApp(applicationName);
			if (list.Count < 1)
			{
				ListPorts();
				return 0; //no applications of that type are registered
			}

			Console.WriteLine("Returning "+((int)list[0]).ToString()+" for "+applicationName);

			//for now we just returned the longest-lived application of this type
			return (int)list[0];
		}

		protected void ListPorts()
		{
			foreach(string applicationName in m_registeredApplications.Keys)
			{
				Console.Write(applicationName +" ");
				ArrayList list = PortsForApp(applicationName);
				foreach(int port in list)
				{
					Console.Write(port +" ");
				}
				Console.WriteLine ("");
			}
		}
	}
}
