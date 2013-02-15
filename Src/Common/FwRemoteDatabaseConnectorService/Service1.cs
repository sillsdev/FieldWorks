using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading;
using FwRemoteDatabaseConnector;
using SIL.FieldWorks.FDO.DomainServices;

namespace FwRemoteDatabaseConnectorService
{
	public partial class FwRemoteDatabaseConnectorService : ServiceBase
	{
		private Thread m_clientListenerThread;
		private Socket m_clientListenerSocket;

		public FwRemoteDatabaseConnectorService()
		{
			InitializeComponent();
		}

		internal void ThreadStartListenForClients()
		{
			try
			{
				m_clientListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

				var iep = new IPEndPoint(IPAddress.Any, Db4OPorts.ServerPort);
				m_clientListenerSocket.Bind(iep);
				var ep = (EndPoint)iep;

				while (m_clientListenerSocket != null)
				{
					var data = new byte[1024];
					m_clientListenerSocket.ReceiveFrom(data, ref ep);
					Reply((IPEndPoint)ep);
				}
			}
			catch
			{
				// Ignore non clean close down.
			}
		}

		void Reply(IPEndPoint ep)
		{
			using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
				var iep = new IPEndPoint(ep.Address, Db4OPorts.ReplyPort);
				sock.SendTo(new byte[] { 0 }, iep);
			}
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				RemotingServer.Start();
			}
			catch (Exception e)
			{
				// TODO: remove this debugging message.
				System.Windows.Forms.MessageBox.Show(e.ToString());
			}

			m_clientListenerThread = new Thread(ThreadStartListenForClients);
			m_clientListenerThread.Start();
		}

		protected override void OnStop()
		{
			if (m_clientListenerSocket != null)
			{
				Socket temp = m_clientListenerSocket;
				m_clientListenerSocket = null;
				temp.Close();
			}
		}
	}
}
