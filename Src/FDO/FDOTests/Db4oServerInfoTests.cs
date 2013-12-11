using System;
using System.IO;
using System.Linq;
using System.Net;
using Db4objects.Db4o;
using Db4objects.Db4o.Config.Encoding;
using Db4objects.Db4o.CS;
using Db4objects.Db4o.CS.Config;
using FwRemoteDatabaseConnector;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region HelperClasses for dboServerInfo unit tests.

	/// <summary>Connects to a local db4o server on the specified port</summary>
	public class DummyLocalConnectedClient : IDisposable
	{
		const string User = "db4oUser";
		const string Password = "db4oPassword";

		/// <summary></summary>
		public DummyLocalConnectedClient(int port)
		{
			IClientConfiguration config = Db4oClientServer.NewClientConfiguration();
			config.Common.StringEncoding = StringEncodings.Utf8();
			Client = Db4oClientServer.OpenClient(config, "127.0.0.1", port, User, Password);
		}

		private IObjectContainer Client { get; set; }

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~DummyLocalConnectedClient()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary> Closes the client connection.</summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
			Client.Close();
			Client.Dispose();

			// Give the server chance to disconnect.
			// if the next assert randomly fails then change how this is done
			System.Threading.Thread.Sleep(300);
		}
			IsDisposed = true;
		}
		#endregion
	}
	/// <summary> Creates a temporary db4o Server file</summary>
	public class TemporaryDb4OServerFile : IDisposable
	{
		/// <summary> </summary>
		public TemporaryDb4OServerFile(Db4oServerInfo db4OServerInfo)
		{
			Db4OServerInfo = db4OServerInfo;

			ProjectName = String.Format("file{0}", DateTime.Now.Ticks);
			Db4OServerInfo.CreateServerFile(ProjectName);
		}

		/// <summary> Start the temporary Db4o server file</summary>
		public void StartServer()
		{
			int port;
			Exception e;
			ServerRunning = Db4OServerInfo.StartServer(ProjectName, out port, out e);
			Port = port;
			Exception = e;
			Assert.IsTrue(ServerRunning);
		}

		/// <summary> Stop the temporary d4o server file</summary>
		public void StopServer()
		{
			Assert.IsTrue(Db4OServerInfo.StopServer(ProjectName));
			ServerRunning = false;
		}

		/// <summary> Connect a Dummy client to the running server file.</summary>
		public DummyLocalConnectedClient ConnectADummyClient()
		{
			if (Port == 0)
				throw new ApplicationException("Port not set - Starting db4o server may have failed.");

			return new DummyLocalConnectedClient(Port);
		}

		private bool ServerRunning { get; set; }

		/// <summary> </summary>
		public string ProjectName { get; protected set; }

		/// <summary> </summary>
		public int Port { get; protected set; }

		/// <summary> </summary>
		public Exception Exception { get; protected set; }

		/// <summary> </summary>
		private Db4oServerInfo Db4OServerInfo { get; set; }

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~TemporaryDb4OServerFile()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary> Deletes the temporary db4o Server file</summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
			if (ServerRunning)
				StopServer();

			string fullPath = ClientServerServices.Current.Local.IdForLocalProject(ProjectName);
			File.Delete(fullPath);
			Directory.Delete(Path.GetDirectoryName(fullPath), true);
		}
			IsDisposed = true;
		}
		#endregion
	}
	#endregion


	/// <summary>
	/// Test functionality related to Client-Server operation
	/// </summary>
	[TestFixture]
	public class Db4oServerInfoTests : BaseTest
	{
		// Get created a fresh for each unit test
		private Db4oServerInfo m_db4OServerInfo;
		private bool m_sharedProject;

		///<summary></summary>
		[SetUp]
		public void StartFwRemoteDatabaseConnector()
		{
			ClientServerServices.SetCurrentToDb4OBackend(new DummyFdoUI(), FwDirectoryFinder.FdoDirectories,
				() => FwDirectoryFinder.ProjectsDirectory == FwDirectoryFinder.ProjectsDirectoryLocalMachine);
			m_sharedProject = true;
			RemotingServer.Start(FwDirectoryFinder.RemotingTcpServerConfigFile, FwDirectoryFinder.FdoDirectories, () => m_sharedProject, v => m_sharedProject = v);

			var connectString = String.Format("tcp://{0}:{1}/FwRemoteDatabaseConnector.Db4oServerInfo",
				"localhost", Db4OPorts.ServerPort);
			m_db4OServerInfo = (Db4oServerInfo)Activator.GetObject(typeof(Db4oServerInfo), connectString);
		}

		///<summary></summary>
		[TearDown]
		public void StopFwRemoteDatabaseConnector()
		{
			m_db4OServerInfo = null;
			RemotingServer.Stop();
		}

		///<summary></summary>
		[Test]
		public void ListServers_UnknownNumberOfServers_ReturnsAllServersInProjectsDirectory()
		{
			int projectsCount = Directory.GetFiles(FwDirectoryFinder.ProjectsDirectory, "*" + FdoFileHelper.ksFwDataDb4oFileExtension,
				SearchOption.AllDirectories).Count();

			m_db4OServerInfo.RefreshServerList();

			Assert.AreEqual(projectsCount, m_db4OServerInfo.ListServers().Count(),
				String.Format("ListServer should return all the db4o projects in the FwDirectoryFinder.ProjectsDirectory : {0}", FwDirectoryFinder.ProjectsDirectory));
		}

		///<summary></summary>
		[Test]
		public void ListRunningServers_NoRunningServers_ReturnsEmptyCollection()
		{
			Assert.AreEqual(0, m_db4OServerInfo.ListRunningServers().Count());
		}

		///<summary></summary>
		[Test]
		[Ignore("Cannot make this test work without writing file in real project directory or changing HKLM ProjectsFolder, since ShareMyProjects returns false")]
		public void CreateServerFile_ServerFileDoesNotExist_ReturnsFullNameOfServerFileAndCreatesServerFile()
		{
			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				Assert.IsTrue(File.Exists(ClientServerServices.Current.Local.IdForLocalProject(db4OServerFile.ProjectName)),
					"File does not exist");
			}
		}

		///<summary></summary>
		[Test]
		public void StartServer_ValidNonRunningServerFile_ReturnsTrue()
		{
			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				int port;
				Exception e;
				bool success = m_db4OServerInfo.StartServer(db4OServerFile.ProjectName, out port, out e);


				Assert.IsTrue(success, "db4o server should have been able to start");
				Assert.IsNull(e);
				Assert.AreNotEqual(0, port, "return port should be non zero");

				// Cleanup
				m_db4OServerInfo.StopServer(db4OServerFile.ProjectName);
			}
		}

		///<summary></summary>
		[Test]
		public void StartServer_FileDoesNotExist_ReturnsFalse()
		{
			const string nonexistantfile = "FileDoesNotExists";
			Assert.IsFalse(File.Exists(nonexistantfile));

			int port;
			Exception e;
			bool success = m_db4OServerInfo.StartServer(nonexistantfile, out port, out e);
			Assert.AreEqual(typeof(ArgumentException), e.GetType());

			Assert.IsFalse(success);
			Assert.IsFalse(File.Exists(nonexistantfile));
		}

		///<summary></summary>
		[Test]
		public void StopServer_ServerNotRunning_ReturnsTrue()
		{
			Assert.IsTrue(m_db4OServerInfo.StopServer("SomeNonRunningServer"));
		}

		///<summary></summary>
		[Test]
		public void StopServer_ServerIsRunning_ReturnsTrueAndServerIsNotRunning()
		{
			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				int port;
				Exception e;
				bool success = m_db4OServerInfo.StartServer(db4OServerFile.ProjectName, out port, out e);

				Assert.IsTrue(success);

				Assert.IsTrue(m_db4OServerInfo.ListRunningServers().Contains(db4OServerFile.ProjectName));
				Assert.IsTrue(m_db4OServerInfo.StopServer(db4OServerFile.ProjectName), "Server should have stopped");
				Assert.IsFalse(m_db4OServerInfo.ListRunningServers().Contains(db4OServerFile.ProjectName));
			}
		}

		///<summary></summary>
		[Test]
		public void StopServer_ServerIsRunningWithConnectedClient_ReturnsFalse()
		{
			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				db4OServerFile.StartServer();

				using (var dummyLocalConnectedClient = db4OServerFile.ConnectADummyClient())
				{
					Assert.IsFalse(m_db4OServerInfo.StopServer(db4OServerFile.ProjectName),
						"Server should not stop because a client is connected");
				}

				Assert.IsTrue(m_db4OServerInfo.StopServer(db4OServerFile.ProjectName),
					"Server should stop because client is not connected - (this may be a unit test timing issue)");
			}
		}

		///<summary></summary>
		[Test]
		public void AreProjectShared_ProjectsAreShared_ReturnsTrue()
		{
			// Setup state
			m_db4OServerInfo.ShareProjects(true);

			// Test
			Assert.IsTrue(m_db4OServerInfo.AreProjectShared());
		}

		///<summary></summary>
		[Test]
		public void ShareProjects_TurningOnSharedProjects_ProjectsAreShared()
		{
			m_db4OServerInfo.ShareProjects(true);
			Assert.IsTrue(m_db4OServerInfo.AreProjectShared());
		}

		///<summary></summary>
		[Test]
		public void AreProjectShared_ProjectsAreNotShared_ReturnsFalse()
		{
			m_db4OServerInfo.ShareProjects(false);
			Assert.IsFalse(m_db4OServerInfo.AreProjectShared());
		}

		///<summary></summary>
		[Test]
		public void ListConnectedClients_NoConnectedClients_ReturnsEmptyCollection()
		{
			Assert.AreEqual(0, m_db4OServerInfo.ListConnectedClients().Count());
		}

		///<summary></summary>
		[Test]
		public void ListConnectedClients_SingleRunningServerWithNoConnectedClients_ReturnsEmptyCollection()
		{
			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				db4OServerFile.StartServer();

				// The test.
				Assert.AreEqual(0, m_db4OServerInfo.ListConnectedClients(db4OServerFile.ProjectName).Count());

				db4OServerFile.StopServer();
			}
		}


		///<summary></summary>
		[Test]
		public void ListConnectedClients_SingleConnectedClient_ReturnsSingleClientIpAddress()
		{
			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				db4OServerFile.StartServer();

				int existingConnectedClientsCount = m_db4OServerInfo.ListConnectedClients().Count();

				using (var dummyClient = db4OServerFile.ConnectADummyClient())
				{
					// The test.
					Assert.AreEqual(existingConnectedClientsCount + 1,
						m_db4OServerInfo.ListConnectedClients(db4OServerFile.ProjectName).Count());
				}

				db4OServerFile.StopServer();
			}
		}

		///<summary></summary>
		[Test]
		public void ListConnectedClients_SingleConnectedClientDoNotSpecifyProjectName_ReturnsSingleClientIpAddress()
		{
			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				db4OServerFile.StartServer();

				int existingConnectedClientsCount = m_db4OServerInfo.ListConnectedClients().Count();

				using (var dummyClient = db4OServerFile.ConnectADummyClient())
				{
					// The test.
					Assert.AreEqual(existingConnectedClientsCount + 1, m_db4OServerInfo.ListConnectedClients().Count());
				}

				db4OServerFile.StopServer();
			}
		}

		///<summary></summary>
		[Test]
		public void IsLocalHost_WindowsLocalHostIpAddress_ReturnsTrue()
		{
			Assert.IsTrue(m_db4OServerInfo.IsLocalHost("127.0.0.1"));
		}

		///<summary></summary>
		[Test]
		public void IsLocalHost_LocalHostByName_ReturnsTrue()
		{
			Assert.IsTrue(m_db4OServerInfo.IsLocalHost("localhost"));
		}

		///<summary></summary>
		[Test]
		public void IsLocalHost_HostByName_ReturnsTrue()
		{
			Assert.IsTrue(m_db4OServerInfo.IsLocalHost(Dns.GetHostName()));
		}

		///<summary></summary>
		[Test]
		public void IsLocalHost_NonLocalHost_ReturnsFalse()
		{
			Assert.IsFalse(m_db4OServerInfo.IsLocalHost("MyRemoteHost"));
		}
	}
}
