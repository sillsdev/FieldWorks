using System;
using System.IO;
using System.Windows.Forms;
using FwRemoteDatabaseConnector;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test functionality related to Client-Server operation
	/// TODO: In reality these unit tests assume db4o client server. Do something about this.
	/// </summary>
	[TestFixture]
	public class ClientServerServicesTests : BaseTest
	{
		// Get created for each unit test
		private Db4oServerInfo m_db4OServerInfo;

		private string m_oldProjectDirectory;

		private IThreadedProgress m_progress;

		///<summary></summary>
		[SetUp]
		public void StartFwRemoteDatabaseConnector()
		{
			// Change the Project Directory to some temporary directory to ensure, other units tests don't add projects
			// which would slow these tests down.
			m_oldProjectDirectory = DirectoryFinder.ProjectsDirectory;
			DirectoryFinder.ProjectsDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(DirectoryFinder.ProjectsDirectory);

			RemotingServer.Start();

			var connectString = String.Format("tcp://{0}:{1}/FwRemoteDatabaseConnector.Db4oServerInfo", "127.0.0.1", 3333);
			m_db4OServerInfo = (Db4oServerInfo)Activator.GetObject(typeof(Db4oServerInfo), connectString);

			// Arbitrary method call to ensure db4oServerInfo is created on server.
			m_db4OServerInfo.AreProjectShared();

			m_progress = new DummyProgressDlg();
		}

		///<summary></summary>
		[TearDown]
		public void StopFwRemoteDatabaseConnector()
		{
			RemotingServer.Stop();

			Directory.Delete(DirectoryFinder.ProjectsDirectory, true);
			DirectoryFinder.ProjectsDirectory = m_oldProjectDirectory;

			m_db4OServerInfo = null;
		}

		/// <summary></summary>
		[Test]
		public void BeginFindServers_NullArgument_DoesNotThrowException()
		{
			ClientServerServices.Current.BeginFindServers(null);
			ClientServerServices.Current.ForceEndFindServers();
		}

		/// <summary></summary>
		[Test]
		public void ProjectNames_NullServer_ThrowsArgumentExeception()
		{
			Assert.Throws(typeof(ArgumentException), () => ClientServerServices.Current.ProjectNames(null));
		}

		/// <summary></summary>
		[Test]
		public void ProjectNames_LocalhostServiceIsRunning_ProjectsReturned()
		{
			Assert.IsTrue(ClientServerServices.Current.Local.SetProjectSharing(true, m_progress));

			using (var db4OServerFile = new TemporaryDb4OServerFile(m_db4OServerInfo))
			{
				db4OServerFile.StartServer();

				Assert.Greater(ClientServerServices.Current.ProjectNames("127.0.0.1").Length, 0,
							   "At least one project should have been found.");

				db4OServerFile.StopServer();
			}
		}

		/// <summary></summary>
		[Test]
		public void ShareMyProjects_TurningShareMyProjectOff_ShareMyProjectsReturnedFalse()
		{
			Assert.IsTrue(ClientServerServices.Current.Local.SetProjectSharing(false, m_progress));
			Assert.AreEqual(false, ClientServerServices.Current.Local.ShareMyProjects);
		}

		/// <summary></summary>
		[Test]
		public void ShareMyProjects_TurningShareMyProjectOn_ShareMyProjectsReturnedTrue()
		{
			Assert.IsTrue(ClientServerServices.Current.Local.SetProjectSharing(true, m_progress));
			Assert.AreEqual(true, ClientServerServices.Current.Local.ShareMyProjects);
		}

		/// <summary></summary>
		[Test]
		public void ConvertToCurrentBackend_ConvertFromFwDataToClientServer_ValidFwDataFileIsProduced()
		{
			// TODO: Implement this test.
		}

		/// <summary></summary>
		[Test]
		public void IdForLocalProject_SimpleNameProjectsAreNotShared_ReturnedFilenameHasFwdataExtenstionAndExistsInProjectDirectory()
		{
			Assert.IsTrue(ClientServerServices.Current.Local.SetProjectSharing(false, m_progress));
			string filename = ClientServerServices.Current.Local.IdForLocalProject("tom");

			// Assert ends with .fwdata
			Assert.AreEqual(FwFileExtensions.ksFwDataXmlFileExtension, Path.GetExtension(filename));

			// Check file is in ProjectDirectory.
			Assert.IsTrue(DirectoryFinder.IsSubFolderOfProjectsDirectory(Path.GetDirectoryName(filename)));
		}

		/// <summary></summary>
		[Test]
		public void ListOpenProjects_NoConnectedClients_ReturnedEmptyCollection()
		{
			Assert.AreEqual(0, ClientServerServices.Current.Local.ListOpenProjects().Length);
		}

		/// <summary></summary>
		[Test]
		public void ListConnectedClients_EmptyStringArg_ThrowsArgumentExeception()
		{
			Assert.Throws(typeof(ArgumentException), () => ClientServerServices.Current.Local.ListConnectedClients(String.Empty));
		}

		/// <summary></summary>
		[Test]
		public void ListConnectedClients_ProjectNameWithNoConnectedClients_ThrowsArgumentExeception()
		{
			var connectedClients = ClientServerServices.Current.Local.ListConnectedClients("SomeMadeupNonExistantProject");
			Assert.AreEqual(0, connectedClients.Length);
		}

		/// <summary></summary>
		[Test]
		public void ListRemoteConnectedClients_EmptyStringArg_ThrowsArgumentException()
		{
			Assert.Throws(typeof(ArgumentException), () => ClientServerServices.Current.Local.ListRemoteConnectedClients(String.Empty));
		}

		/// <summary></summary>
		[Test]
		public void ListRemoteConnectedClients_ProjectNameWithNoConnectedClients_ThrowsArgumentException()
		{
			var connectedClients = ClientServerServices.Current.Local.ListRemoteConnectedClients("SomeMadeupNonExistantProject");
			Assert.AreEqual(0, connectedClients.Length);
		}
	}
}