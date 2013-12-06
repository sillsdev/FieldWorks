// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Db4OServerFinder.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// In a separate thread, finds any DB4o servers on the network.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
		Justification = "m_hostListenerSocket is disposed when closed.")]
	internal class Db4OServerFinder
	{
		#region Data members
		public static readonly int ServiceDiscoveryPort = Db4OPorts.ServerPort;
		private readonly int HostDiscoveryBroadcastPort = Db4OPorts.ServerPort;
		private readonly int HostDiscoveryBroadcastReplyPort = Db4OPorts.ReplyPort;

		private readonly Thread m_hostListenerThread;
		private Socket m_hostListenerSocket;
		private readonly object m_syncRoot = new object();
		private readonly Action<string> m_foundServerCallback;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Db4OServerFinder"/> class.
		/// </summary>
		/// <param name="foundServerCallback">The method to call when a server is found
		/// (string parameter is the IP address of the found server)</param>
		/// ------------------------------------------------------------------------------------
		public Db4OServerFinder(Action<string> foundServerCallback)
		{
			m_foundServerCallback = foundServerCallback;

			m_hostListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {Blocking = false};
			EndPoint ep = new IPEndPoint(IPAddress.Any, HostDiscoveryBroadcastReplyPort);
			m_hostListenerSocket.Bind(ep);

			// Start the thread that collects responses from our broadcast.
			m_hostListenerThread = new Thread(ThreadStartListenForServers);
			m_hostListenerThread.Name = "DB4o Server Finder";
			m_hostListenerThread.Start();

			BroadcastToFindHosts();
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Broadcasts to find hosts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BroadcastToFindHosts()
		{
			// On Windows 7, this silently fails if the computer isn't connected to the network.
			// On Windows XP, it eventually (after timeout?) throws a SocketException "A socket
			// operation was attempted to an unreachable host".
			try
			{
				// Send the broadcast.
				using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
					EndPoint iep = new IPEndPoint(IPAddress.Broadcast, HostDiscoveryBroadcastPort);

					sock.SendTo(new byte[] { 0 }, iep);
				}
			}
			catch (SocketException)
			{
				// Don't do anything for now. Maybe a message box?
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Listen to all broadcast responses from servers on the network.
		/// This is run from a separate thread.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ThreadStartListenForServers()
		{
			try
			{
				byte[] data = new byte[1024];
				int cVainAttempts = 0;
				while (true) // keep listening until thread is killed.
				{
					EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
					int recv = 0;
					lock (m_syncRoot)
					{
						if (m_hostListenerSocket == null) // another thread can set this to null.
							break;
						if (m_hostListenerSocket.Available > 0)
							ExceptionHelper.LogAndIgnoreErrors(() => recv = m_hostListenerSocket.ReceiveFrom(data, ref ep));
					}
					if (recv > 0)
					{
						lock (m_syncRoot)
							m_foundServerCallback(((IPEndPoint)ep).Address.ToString());
						cVainAttempts = 0;
					}
					else
					{
						if (cVainAttempts++ > 50)
							break; // if no server has pinged us for 5 seconds, let's hang it up.
						Thread.Sleep(100);
					}
				}
			}
			finally
			{
				CloseSocket();
			}
		}

		public bool IsCompleted
		{
			get { return !m_hostListenerThread.IsAlive; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces the thread to stop (so no more servers will be found).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ForceStop()
		{
			CloseSocket();
			m_hostListenerThread.Join();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the socket.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseSocket()
		{
			lock (m_syncRoot)
			{
				if (m_hostListenerSocket != null)
					m_hostListenerSocket.Close();
				m_hostListenerSocket = null;
			}
		}
		#endregion
	}
}
