// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FDOBackendProvider.cs
// Responsibility: FW Team

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security;
using System.Threading;
using Db4objects.Db4o;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.FieldWorks.FDO.DomainServices;
using System.Runtime.Remoting.Channels;

namespace FwRemoteDatabaseConnector
{
	/// <summary>
	/// Singlton object that get created by .NET remoting.
	/// </summary>
	public class Db4oServerInfo : MarshalByRefObject
	{
		/// <summary>
		/// Stores the filenames of all the db40 Servers
		/// </summary>
		protected List<string> m_allServers;

		/// <summary>
		/// gets which port to start the next db4o server on.
		/// </summary>
		protected int NextPort
		{
			get
			{
				// Search for the lowest avalible port that is 4488 or above.
				int possiblePort = Db4OPorts.StartingPort;
				while (m_runningServers.Where(x => x.Value.m_port == possiblePort).Count() > 0)
					possiblePort++;

				return possiblePort;
			}
		}

		#region Cached information about running databases.
		/// <summary> Maps db4o project name (not filename!) to port it is listening on</summary>
		protected Dictionary<string, RunningServerInfo> m_runningServers = new Dictionary<string, RunningServerInfo>();

		/// <summary> Cached infomation about the db4o database servers that are running. </summary>
		protected struct RunningServerInfo
		{
			/// <summary> port which db4o server is listening on </summary>
			public int m_port;

			/// <summary> db4o server instance</summary>
			public IObjectServer m_objectServer;
		}

		#endregion

		/// <summary></summary>
		public Db4oServerInfo()
		{
			RemotingServer.ServerObject = this;
			IsRunning = true;
		}

		internal void PopulateServerList()
		{
			m_allServers = new List<string>();

			string projectsDir = RemotingServer.Directories.ProjectsDirectory;

			if (!Directory.Exists(projectsDir))
				throw new DirectoryNotFoundException(String.Format(Strings.ksWarningProjectFolderNotFoundOnServer, projectsDir));

			string[] files = Directory.GetFiles(projectsDir, "*" + FdoFileHelper.ksFwDataDb4oFileExtension);
			m_allServers.AddRange(files);

			// search sub dirs
			string[] dirs = Directory.GetDirectories(projectsDir);
			foreach (var dir in dirs)
			{
				files = Directory.GetFiles(dir, "*" + FdoFileHelper.ksFwDataDb4oFileExtension);
				m_allServers.AddRange(files);
			}
		}

		/// <summary>
		/// If the connected client isn't localhost throws SecurityException
		/// </summary>
		internal void EnsureClientIsLocalHost()
		{
			IPAddress clientIpAddress = CallContextExtensions.GetIpAddress();

			if (clientIpAddress == null)
			{
				throw new SecurityException("This method can only be executed on the server. ClientIPAddress not set.");
			}

			if (IsLocalHost(clientIpAddress.ToString()))
				return;

			throw new SecurityException(String.Format("This method can only be executed on the server. ClientIPAddress is {0}", clientIpAddress));
		}


		#region public remotable methods

		/// <summary>
		/// Determine if a given host is local host or not.
		/// </summary>
		/// <param name="hostname"> host name or ipaddress</param>
		/// <returns>True if passed argument means localhost</returns>
		public bool IsLocalHost(string hostname)
		{
			if (hostname == "127.0.0.1") // Windows standard address of localhost.
				return true;

			if (hostname == "0.0.0.0") // Linux "any ipaddress of localhost"
				return true;

			try
			{
				var dnsHostName = Dns.GetHostEntry(hostname).HostName;
				if (dnsHostName == Dns.GetHostEntry(Dns.GetHostName()).HostName)
					return true;

				if (dnsHostName == "127.0.0.1")
					return true;

				if (Dns.GetHostAddresses(Dns.GetHostName()).Any(x => x.ToString() == hostname))
					return true;
#if __MonoCS__
				// On Linux 127.0.0.1 isn't the same as system host name.
				if (dnsHostName == Dns.GetHostEntry("127.0.0.1").HostName)
					return true;

				if (Dns.GetHostAddresses("127.0.0.1").Any(x => x.ToString() == hostname))
					return true;
#endif
			}
			catch(System.Net.Sockets.SocketException) {}

			return false;
		}

		/// <summary>
		/// TODO make this thread safe
		/// </summary>
		public string[] ListServers()
		{
			// Only show Projects/Servers if Projects are shared.
			if (!RemotingServer.SharedProjectsGetter())
				return Enumerable.Empty<string>().ToArray();

			if (m_allServers == null)
			{
				PopulateServerList();
				Debug.Assert(m_allServers != null);
			}

			return m_allServers.ToArray();
		}

		/// <summary>
		/// TODO make this thread safe
		/// </summary>
		public string[] ListRunningServers()
		{
			return m_runningServers.Keys.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a db4o database file of given name.
		/// </summary>
		/// <param name="projectName">the desired name.</param>
		/// ------------------------------------------------------------------------------------
		public void CreateServerFile(string projectName)
		{
			string projectDir = Path.Combine(RemotingServer.Directories.ProjectsDirectory, projectName);
			string newFilename = Path.Combine(projectDir, FdoFileHelper.GetDb4oDataFileName(projectName));

			// Ensure directory exists.
			Directory.CreateDirectory(projectDir);

			using (FileStream stream = File.Create(newFilename))
				stream.Close();

			PopulateServerList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instruct the Service to start a db4o server.
		/// </summary>
		/// <param name="projectName">Name of the project to start</param>
		/// <param name="port">If succesfully started then set to the port number of the running
		/// database</param>
		/// <param name="errorinfo">Will be set to the exception if server failed to start</param>
		/// <returns>true if sucessfully started server</returns>
		/// ------------------------------------------------------------------------------------
		public bool StartServer(string projectName, out int port, out Exception errorinfo)
		{
			errorinfo = null;

			Debug.Assert(projectName == Path.GetFileName(projectName));

			// Whether the default local file is currently xml or db4o, starting up a db4o service requires
			// the db4o extension. (IdForLocalProject can be wrong during the process of conversion.)
			string filename = Path.ChangeExtension(ClientServerServices.Current.Local.IdForLocalProject(projectName),
				FDOBackendProviderType.kDb4oClientServer.GetExtension());
			if (!File.Exists(filename))
			{
				errorinfo = new ArgumentException("Specified filename does not exist: " + filename);
				port = 0;
				return false;
			}

			// stop race condition with m_runningServers list.
			lock (this)
			{
				RunningServerInfo info;
				if (m_runningServers.TryGetValue(projectName, out info))
				{
					port = info.m_port;
					return true; // server should already by running
				}

				// Db4o in client server mode doesn't support multiple databases using a single
				// port. So we have to use a port for each database that is running on this
				// host.
				port = NextPort;

				string sWsUser = Thread.CurrentThread.CurrentUICulture.Name;

				try
				{
					IObjectServer db4oserver = Db4oClientServerBackendProvider.StartLocalDb4oClientServer(filename, port);
					m_runningServers.Add(projectName, new RunningServerInfo { m_port = port, m_objectServer = db4oserver});
				}
				catch (Db4objects.Db4o.Ext.DatabaseFileLockedException databaseFileLockedException)
				{
					errorinfo = new ApplicationException(String.Format(
						"Another process already has file {0} open and locked. Try Killing Fieldworks Applications on the server.",
						filename));
					return false;
				}
				catch (Exception e)
				{
					errorinfo = e;
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Get all the CmObjectSurrogates in the database.
		/// </summary>
		/// <param name="projectName"></param>
		/// <returns>a compressed memory block of all the CmObjectSurroagates raw data.</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Ext() returns a reference")]
		public byte[] GetCmObjectSurrogates(string projectName)
		{
			RunningServerInfo info;
			if (!m_runningServers.TryGetValue(projectName, out info))
				throw new ApplicationException(String.Format("db4o server for project {0} not running.", projectName));

			Db4oClientServerBackendProvider.CmObjectSurrogateTypeHandler.CachedProjectInformation = new CachedProjectInformation();

			var objs = info.m_objectServer.Ext().ObjectContainer().Query<CmObjectSurrogate>().ToArray();

			Debug.Assert(Db4oClientServerBackendProvider.CmObjectSurrogateTypeHandler.CachedProjectInformation.Compressor != null,
				"Could a database have no CmObjectSurrogates?");

			// Deactivate all objects that the Query call just activated.
			// REVIEW (FWR-3288): Possibly implement caching in the db4o server process.
			foreach (var cmObjectSurrogate in objs)
			{
				info.m_objectServer.Ext().ObjectContainer().Deactivate(cmObjectSurrogate, 1);
			}

			// flush Compressor to ensure CompressedMemoryStream is up-to-date.
			Db4oClientServerBackendProvider.CmObjectSurrogateTypeHandler.CachedProjectInformation.Compressor.Dispose();
			byte[] ret = Db4oClientServerBackendProvider.CmObjectSurrogateTypeHandler.CachedProjectInformation.CompressedMemoryStream.ToArray();

			Db4oClientServerBackendProvider.CmObjectSurrogateTypeHandler.CachedProjectInformation.Dispose();
			Db4oClientServerBackendProvider.CmObjectSurrogateTypeHandler.CachedProjectInformation = null;

			return ret;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inform the server that the db4o file is not longer in use by the client. The client
		/// should have already closed its db4o socket connection before calling this method.
		/// </summary>
		/// <param name="projectName">Name of the project to stop</param>
		/// <returns>
		/// returns false if was unable to stop server (if other clients are connected etc.)
		/// returns true if server has stopped or is already stopped.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Ext() returns a reference")]
		public bool StopServer(string projectName)
		{
			Debug.Assert(!string.IsNullOrEmpty(projectName) && projectName == Path.GetFileName(projectName));

			RunningServerInfo info;
			if (!m_runningServers.TryGetValue(projectName, out info))
			{
				// Server not running
				return true;
			}

			// only close if there are no clients conencted
			if (info.m_objectServer.Ext().ClientCount() > 0)
				return false;

			m_runningServers.Remove(projectName);
			info.m_objectServer.Close();
			info.m_objectServer.Dispose();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether projects are shared. (Should only be called on local
		/// machine.)
		/// </summary>
		/// <returns>
		/// true if project are being shared from this machine.
		/// </returns>
		/// <exception cref="SecurityException">If called from a remote machine.</exception>
		/// ------------------------------------------------------------------------------------
		public bool AreProjectShared()
		{
			EnsureClientIsLocalHost();
			return RemotingServer.SharedProjectsGetter();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables/disables sharing of projects from this machine.
		/// </summary>
		/// <param name="enableSharingOfProjects">if set to <c>true</c> enable sharing of
		/// projects; otherwise, disable sharing.</param>
		/// <exception cref="SecurityException">if called from a remote machine or if current
		/// user does not have permission to write to HKLM</exception>
		/// ------------------------------------------------------------------------------------
		public void ShareProjects(bool enableSharingOfProjects)
		{
			EnsureClientIsLocalHost();
			RemotingServer.SharedProjectsSetter(enableSharingOfProjects);
		}

		/// <summary>
		/// Allows querying all the connected clients for all projects.
		/// Intentionally not using IEnumerable across remoting boundaries
		/// to prevent any possible bad performance.
		/// </summary>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Ext() returns a reference")]
		public string[] ListConnectedClients()
		{
			var ports = new List<int>();
			foreach (var port in m_runningServers.Values)
			{
				if (port.m_objectServer.Ext().ClientCount() > 0)
					ports.Add(port.m_port);
			}
			return ListUniqueAddressConnectedToPorts(ports);
		}

		internal string[] ListUniqueAddressConnectedToPort(int port)
		{
			return ListUniqueAddressConnectedToPorts(new List<int> {port});
		}

		internal string[] ListUniqueAddressConnectedToPorts(IEnumerable ports)
		{
			var remoteIpAddressCollection = new List<string>();

			foreach (int searchPort in ports)
			{
				IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
				TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

				foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
				{
#if __MonoCS__
					if (tcpi.LocalEndPoint.Port == searchPort && (tcpi.State == TcpState.Established || tcpi.State == TcpState.LastAck))
#else
					if (tcpi.LocalEndPoint.Port == searchPort && tcpi.State == TcpState.Established)
#endif
					{
						string remoteIpAddress = tcpi.RemoteEndPoint.Address.ToString();
						if (!remoteIpAddressCollection.Contains(remoteIpAddress))
							remoteIpAddressCollection.Add(remoteIpAddress);
					}
				}
			}

			return remoteIpAddressCollection.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the connected clients for a single project
		/// </summary>
		/// <remarks>Intentionally not using IEnumerable across remoting boundaries to prevent
		/// any possible bad performance.</remarks>
		/// <param name="projectName">Name of the db4o project.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Ext() returns a reference")]
		public string[] ListConnectedClients(string projectName)
		{
			Debug.Assert(!string.IsNullOrEmpty(projectName) && projectName == Path.GetFileName(projectName));

			RunningServerInfo info;
			// If server isn't running or db4o reports no connected clients, don't bother looking.
			if (!m_runningServers.TryGetValue(projectName, out info) ||
				info.m_objectServer.Ext().ClientCount() == 0)
			{
				return new string[0];
			}

			return ListUniqueAddressConnectedToPort(info.m_port);
		}

		/// <summary>
		/// Reload the list of servers.
		/// </summary>
		public void RefreshServerList()
		{
			PopulateServerList();
		}


		/// <summary>
		/// reset back to initial state.
		/// This is only used by unittests.
		/// </summary>
		internal void ShutdownAllServers()
		{
			foreach(var info in m_runningServers.Values)
			{
				info.m_objectServer.Close();
				info.m_objectServer.Dispose();
			}

			m_runningServers.Clear();
			m_allServers = null;
		}

		internal bool IsRunning { get; set; }

		#endregion
	}

	/// <summary>
	/// Manages the .NET remoting server configuration.
	/// This is not to be used by clients.
	/// </summary>
	public class RemotingServer
	{
		/// <summary>
		///
		/// </summary>
		public static Db4oServerInfo ServerObject
		{
			set; protected get;
		}

		/// <summary>
		/// This delegate is used to retrieve the projects directory.
		/// </summary>
		internal static IFdoDirectories Directories { get; private set; }

		/// <summary>
		/// Gets the shared projects getter.
		/// </summary>
		internal static Func<bool> SharedProjectsGetter { get; private set; }

		/// <summary>
		/// Gets the shared projects setter.
		/// </summary>
		internal static Action<bool> SharedProjectsSetter { get; private set; }

			/// <summary>
		/// Start an instance of the .NET remoting server.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="Added TODO-Linux")]
		public static void Start(string remotingTcpServerConfigFile, IFdoDirectories dirs, Func<bool> sharedProjectsGetter, Action<bool> sharedProjectsSetter)
		{
			// check if we are already running.
			if (ServerObject != null && ServerObject.IsRunning)
				return;

			Directories = dirs;
			SharedProjectsGetter = sharedProjectsGetter;
			SharedProjectsSetter = sharedProjectsSetter;

			if (ChannelServices.RegisteredChannels.Length > 0)
			{
				var tcpChannel = ChannelServices.GetChannel("tcp");
				if (tcpChannel is TcpChannel)
				{
					// Channel got registered before. Don't do it again
					return;
				}
				if (tcpChannel != null)
				{
					// We probably have a TcpClientChannel which we can't use, so get rid of it.
					// This can happen in tests when NUnit runs under .NET 2.0 but has to kick
					// off a nunit-agent process that can run the tests under .NET 4.0. For some
					// reason in this case we get a TcpClientChannel and so our tests fail
					// unless we unregister the client channel.
					ChannelServices.UnregisterChannel(tcpChannel);
				}
			}

			if (ServerObject == null)
			{
				// TODO: currently running with no security
				// TODO-Linux: security support has not been implemented in Mono
				RemotingConfiguration.Configure(remotingTcpServerConfigFile, false);
			}
			else
			{
				ServerObject.IsRunning = true;
			}
		}

		/// <summary>
		/// Hard stop the .NET remoting server. Unit test should be the only
		/// things that need to do this.
		/// </summary>
		public static void Stop()
		{
			if (ServerObject != null)
			{
				ServerObject.ShutdownAllServers();
				ServerObject.IsRunning = false;
			}
		}
	}
}
