using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading;
using FwRemoteDatabaseConnector;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace FwRemoteDatabaseConnectorService
{
	public partial class FwRemoteDatabaseConnectorService : ServiceBase
	{
		private const string ksSharedProjectKey = "ProjectShared";

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
			RemotingServer.Start(FwDirectoryFinder.RemotingTcpServerConfigFile, FwDirectoryFinder.FdoDirectories, GetSharedProject, SetSharedProject);
			m_clientListenerThread = new Thread(ThreadStartListenForClients);
			m_clientListenerThread.Start();
		}

		private static bool GetSharedProject()
		{
			bool result;
			FwRegistryHelper.MigrateVersion7ValueIfNeeded();
			var value = FwRegistryHelper.FieldWorksRegistryKey.GetValue(ksSharedProjectKey, "false");
			return (bool.TryParse((string)value, out result) && result);
		}

		private static void SetSharedProject(bool v)
		{
			FwRegistryHelper.FieldWorksRegistryKey.SetValue(ksSharedProjectKey, v);
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
