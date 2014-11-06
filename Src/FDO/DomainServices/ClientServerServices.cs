// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ClientServerServices.cs
// Responsibility: FW Team

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using FwRemoteDatabaseConnector;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region ClientServerServices class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ClientServerServices exposes functions that support the possibility of having a second backend
	/// (in addition to the standard XmlBackend) available to the user.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ClientServerServices
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current instance. Apart from testing, this is constant, and expresses the
		/// current configuration of backends.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IClientServerServices Current { get; internal set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the implementation for the static constructor. It is in a separate method
		/// to allow tests to reset it (using reflection).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetCurrentToDb4OBackend(IFdoUI ui, IFdoDirectories dirs)
		{
			// This is the "one line" that should need to be changed to configure a different backend :-).
			// Typically a new implementation of IClientServerServices will be needed, as well as the backend itself.
			Current = new Db4OClientServerServices(ui, dirs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default extension for the backend type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetExtension(this FDOBackendProviderType type)
		{
			switch (type)
			{
				case FDOBackendProviderType.kDb4oClientServer: return FdoFileHelper.ksFwDataDb4oFileExtension;
				case FDOBackendProviderType.kXML: return FdoFileHelper.ksFwDataXmlFileExtension;
				default: throw new InvalidEnumArgumentException("type", (int)type, typeof(FDOBackendProviderType));
			}
		}
	}
	#endregion

	#region IClientServerServices interface
	/// <summary>
	/// Interface that defines behaviors related to accessing projects on a (possibly) remote server.
	/// A single instance of this defines, in effect, the combination of backends currently supported.
	/// </summary>
	public interface IClientServerServices
	{
		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the remote servers
		/// on which the user might be able to open projects.  The local server may be the only one,
		/// and always will be if a client-server backend is not configured.
		/// </summary>
		/// <param name="foundServer">Callback that is invoked when a server is found
		/// (string parameter is the IP address of the found server).</param>
		void BeginFindServers(Action<string> foundServer);

		/// <summary>
		/// Forcibly ends the task of finding servers begun by BeginFindServers without waiting
		/// for it to complete normally.
		/// </summary>
		void ForceEndFindServers();

		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the projects
		/// on the specified host.
		/// </summary>
		/// <param name="host">The host to search</param>
		/// <param name="foundProject">Callback that is invoked when a project is found
		/// (string parameter is the name of the project).</param>
		/// <param name="exceptionCallback">Callback to handle any exceptions that happen when
		/// getting the list of projects (parameter is the exception that occured).</param>
		/// <param name="showLocalProjects">true if we want to show local fwdata projects</param>
		void BeginFindProjects(string host, Action<string> foundProject,
			Action<Exception> exceptionCallback, bool showLocalProjects);

		/// <summary>
		/// Forcibly ends the task of finding projects begun by BeginFindProjects without waiting
		/// for it to complete normally.
		/// </summary>
		void ForceEndFindProjects();

		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the projects which
		/// can be opened on the specified (possibly remote) server. If the name passed is the name of the local
		/// server, it returns the projects being shared by that computer if ShareMyProjects is true;
		/// otherwise, it returns the XML/fwdata projects in the ProjectsDirectory.
		/// </summary>
		string[] ProjectNames(string serverName);

		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the projects which
		/// can be opened on the specified (possibly remote) server. If the name passed is the name of the local
		/// server, it returns the projects being shared by that computer if ShareMyProjects is true;
		/// otherwise, it returns the XML/fwdata projects in the ProjectsDirectory.
		/// if refresh is true, then the remote server is asked to refresh is project cache.
		/// </summary>
		string[] ProjectNames(string serverName, bool refresh);

		/// <summary>
		/// Finds all clients that are using specified project on the specified server.
		/// </summary>
		string[] ListConnectedClients(string serverName, string project);

		/// <summary>
		/// Get the corresponding local services.
		/// </summary>
		ILocalClientServerServices Local { get; }

		/// <summary>
		/// This function is used to obtain a lock, to prevent two clients doing something at the same time.
		/// The current use is to prevent two clients from both running the parser at once; hence, it is
		/// to be expected that the lock may be held for some time. If some other client already has the lock,
		/// it returns null (possibly after a short wait). The caller should Dispose of the token when
		/// exclusive access is no longer needed (ideally,
		/// using (ClientServerServices.Current.GetExclusiveModeToken(cache, "my function")) {...}
		/// A single-user implementation may just return a trivial IDisposable without checking anything.
		/// </summary>
		IDisposable GetExclusiveModeToken(FdoCache cache, string id);

		/// <summary>
		/// Returns the number of other users currently connected
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		int CountOfOtherUsersConnected(FdoCache cache);
	}
	#endregion

	// Todo JohnT: probably a similar special exception if we cannot Restore because people are connected.
	// Maybe it should just be a ProjectSharingException?

	#region NonRecoverableConnectionLostException class

	/// <summary>
	/// Exception thrown when client looses connection to it server,
	/// and couldn't reconnect.
	/// </summary>
	public class NonRecoverableConnectionLostException : ApplicationException
	{
	}

	#endregion

	#region ILocalClientServerServices interface
	/// <summary>
	/// Methods that relate to client-server tasks performed on the local machine
	/// </summary>
	public interface ILocalClientServerServices
	{
		/// <summary>
		/// Gets whether projects on this machine are being shared.
		/// </summary>
		bool ShareMyProjects { get; }

		/// <summary>
		/// Turns project sharing on or off.
		/// </summary>
		/// <param name="fShare">if set to <c>true</c>, turn sharing on.</param>
		/// <param name="progress">The progress dialog.</param>
		/// <returns>Indication of whether the request was performed successfully.</returns>
		bool SetProjectSharing(bool fShare, IThreadedProgress progress);

		/// <summary>
		/// Return true if the specified project (in the specified parent directory) will be
		/// converted as part of the process of converting the projects in projectsDirectory to shared.
		/// </summary>
		bool WillProjectBeConverted(string projectPath, string parentDirectory, string projectsDirectory);

		// REVIEW (TomH): The name of this interface member should not have db4o in it.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the specified project (assumed to be in the current Projects directory)
		/// to the current backend. This is used in Restore and New Project and when turning
		/// ShareMyProjects on.
		/// </summary>
		/// <param name="progressDlg">The progress dialog for getting a message box owner and/or
		/// ensuring we're invoking on the uI thread.</param>
		/// <param name="filename">The full path of the existing XML file for the project</param>
		/// <returns>The project identifier, typically the path to the converted file (or the
		/// original, if not configured for the client-server backend)</returns>
		/// ------------------------------------------------------------------------------------
		string ConvertToDb4oBackendIfNeeded(IThreadedProgress progressDlg, string filename);

		/// <summary>
		/// Copies the specified project (assumed to be in the current Projects directory)
		/// from the current backend to XML. This is used in Backup and when turning
		/// ShareMyProjects off.
		/// </summary>
		/// <param name="source">The source cache</param>
		/// <param name="destDir">The destination directory for the converted copy if a
		/// conversion needs to be done.</param>
		string CopyToXmlFile(FdoCache source, string destDir);

		/// <summary>
		/// Return the full ID that should be used to open the specified project, which is in the
		/// local projects directory.
		/// </summary>
		string IdForLocalProject(string projectName);

		/// <summary>
		/// Gets the default type of backend.
		/// </summary>
		FDOBackendProviderType DefaultBackendType { get; }

		/// <summary>
		/// Finds Client server projects that are in use.
		/// </summary>
		/// <returns>A collection of projects that are running on this local server.</returns>
		string[] ListOpenProjects();

		/// <summary>
		/// Finds all clients that are using specified project.
		/// </summary>
		/// <param name="project">Name of the Client server project</param>
		/// <returns>A collection of hostname/ipaddress that are connected to this project</returns>
		string[] ListConnectedClients(string project);

		/// <summary>
		/// Finds all clients, except local host, that are using specified project.
		/// </summary>
		/// <param name="project">Name of the Client server project</param>
		/// <returns>A collection of remote hostname/ipaddress that are connected to this project</returns>
		string[] ListRemoteConnectedClients(string project);

		/// <summary>
		/// Force the list of project names to be reloaded.  (This is needed after deleting
		/// a project.)
		/// </summary>
		void RefreshProjectNames();
	}
	#endregion

	#region Db4OClientServerServices class
	/// <summary>
	/// The master class that configures an FDO system where the Db4o backend provides client-server services.
	/// </summary>
	internal class Db4OClientServerServices : IClientServerServices
	{
		private Db4OServerFinder m_serverFinder;
		private FwProjectFinder m_projectFinder;
		private readonly IFdoDirectories m_dirs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Db4OClientServerServices"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Db4OClientServerServices(IFdoUI ui, IFdoDirectories dirs)
		{
			m_dirs = dirs;
			Local = new Db4OLocalClientServerServices(ui, dirs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the remote servers
		/// on which the user might be able to open projects.  The local server may be the only one,
		/// and always will be if a client-server backend is not configured.
		/// </summary>
		/// <param name="foundServer">Callback that is invoked when a server is found
		/// (string parameter is the IP address of the found server).</param>
		/// ------------------------------------------------------------------------------------
		public void BeginFindServers(Action<string> foundServer)
		{
			if (m_serverFinder != null && !m_serverFinder.IsCompleted)
				throw new InvalidOperationException("Can not start a new find servers before the previous one finishes.");
			m_serverFinder = new Db4OServerFinder(foundServer);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forcibly ends the task of finding servers begun by BeginFindServers without waiting
		/// for it to complete normally.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ForceEndFindServers()
		{
			if (m_serverFinder != null)
			{
				m_serverFinder.ForceStop();
				m_serverFinder = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the projects
		/// on the specified host.
		/// </summary>
		/// <param name="host">The host to search</param>
		/// <param name="foundProject">Callback that is invoked when a project is found
		/// (string parameter is the name of the project).</param>
		/// <param name="exceptionCallback">Callback to handle any exceptions that happen when
		/// getting the list of projects (parameter is the exception that occured).</param>
		/// <param name="showLocalProjects">true if we want to show local fwdata projects</param>
		/// ------------------------------------------------------------------------------------
		public void BeginFindProjects(string host, Action<string> foundProject,
			Action<Exception> exceptionCallback, bool showLocalProjects)
		{
			if (m_projectFinder != null)
				throw new InvalidOperationException("Can not start a new find projects before the previous one finishes.");
			m_projectFinder = new FwProjectFinder(host, foundProject, () => m_projectFinder = null,
				exceptionCallback, showLocalProjects, m_dirs.ProjectsDirectory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forcibly ends the task of finding projects begun by BeginFindProjects without waiting
		/// for it to complete normally.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ForceEndFindProjects()
		{
			if (m_projectFinder != null)
			{
				m_projectFinder.ForceStop();
				m_projectFinder = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the projects which
		/// can be opened on the specified (possibly remote) server. If the name passed is the name of the local
		/// server, it returns the projects being shared by that computer if ShareMyProjects is true;
		/// otherwise, it returns the XML projects in the ProjectsDirectory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] ProjectNames(string serverName)
		{
			return ProjectNames(serverName, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to populate the File Open dialog, this returns the names of the projects which
		/// can be opened on the specified (possibly remote) server. If the name passed is the name of the local
		/// server, it returns the projects being shared by that computer if ShareMyProjects is true;
		/// otherwise, it returns the XML/fwdata projects in the ProjectsDirectory.
		/// if refresh is true, then the remote server is asked to refresh is project cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] ProjectNames(string serverName, bool refresh)
		{
			try
			{
				if (string.IsNullOrEmpty(serverName))
					throw new ArgumentException();

				Db4oServerInfo info = Db4oClientServerBackendProvider.GetDb4OServerInfo(serverName,
					Db4OServerFinder.ServiceDiscoveryPort);

				if (refresh)
					info.RefreshServerList();

				return info.ListServers();
			}
			catch (SocketException)
			{
				// Protect against invalid or not running servers being passed to ProjectNames.
				return new String[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds all clients that are using specified project on the specified server.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] ListConnectedClients(string serverName, string project)
		{
			if (string.IsNullOrEmpty(serverName))
				throw new ArgumentException("server name cannot be empty", "serverName");
			if (string.IsNullOrEmpty(project))
				throw new ArgumentException("project name cannot be empty", "project");

			return Db4oClientServerBackendProvider.GetDb4OServerInfo(serverName,
					Db4OServerFinder.ServiceDiscoveryPort).ListConnectedClients(project);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the corresponding local services.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILocalClientServerServices Local { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function is used to obtain a lock, to prevent two clients doing something at the same time.
		/// The current use is to prevent two clients from both running the parser at once; hence, it is
		/// to be expected that the lock may be held for some time. If some other client already has the lock,
		/// it returns null (possibly after a short wait). The caller should Dispose of the token when
		/// exclusive access is no longer needed (ideally,
		/// using (ClientServerServices.Current.GetExclusiveModeToken(cache, "my function")) {...}
		/// A single-user implementation may just return a trivial IDisposable without checking anything.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IDisposable GetExclusiveModeToken(FdoCache cache, string id)
		{
			var bep = cache.ServiceLocator.GetInstance<IDataReader>() as Db4oClientServerBackendProvider;
			if (bep == null)
			{
				// The only other backend we support is an XML file, and we always have exclusive access.
				// So we always succeed, and there is nothing to do when the lock is no longer needed.
				return new TrivialDisposable();
			}
			if (bep.Lock(id))
			{
				// We got the semaphore, and will release it when disposed.
				return new SemaphoreHolder(bep, id);
			}
			// Someone else has it locked.
			return null;
		}

		/// <summary>
		/// Returns the number of other users currently connected
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <returns>The number of other users currently connected</returns>
		public int CountOfOtherUsersConnected(FdoCache cache)
		{
			if (cache == null) // Can happen when creating a new project when editing the WS properties. (FWR-2981)
				return 0;

			var bep = cache.ServiceLocator.GetInstance<IDataReader>() as Db4oClientServerBackendProvider;
			if (bep == null)
				return 0; // XML backend, can't currently have other users connected.
			var serverName = bep.ProjectId.ServerName;
			var projectName = bep.ProjectId.Name;
			var otherUsers = ListConnectedClients(serverName, projectName);
			return otherUsers.Length - 1; // Assume this is connected!
		}

		#region TrivialDisposable class
		private sealed class TrivialDisposable: IDisposable
		{
			public void Dispose()
			{
			}
		}
		#endregion

		#region SemaphoreHolder class
		private sealed class SemaphoreHolder : IDisposable
		{
			private Db4oClientServerBackendProvider m_bep;
			private string m_id;
			public SemaphoreHolder(Db4oClientServerBackendProvider bep, string id)
			{
				m_bep = bep;
				m_id = id;
			}
			~SemaphoreHolder()
			{
				Dispose(false);
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool properly)
			{
				System.Diagnostics.Debug.WriteLineIf(!properly, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (properly)
				{
					m_bep.Unlock(m_id);
				}
			}
		}
		#endregion
	}
	#endregion

	#region Db4OLocalClientServerServices class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The helper class that configures local servers in an FDO system where the backed can be
	/// either a Db4o backend that provides client-server services or a single-user XML-based
	/// backend.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class Db4OLocalClientServerServices : ILocalClientServerServices
	{
		internal const string kLocalService = "localhost";
		internal const string ksDoNotShareProjectTxt = "do_not_share_project.txt";
		private readonly IFdoUI m_ui;
		private readonly IFdoDirectories m_dirs;

		public Db4OLocalClientServerServices(IFdoUI ui, IFdoDirectories dirs)
		{
			m_ui = ui;
			m_dirs = dirs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether projects on this machine are being shared.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShareMyProjects
		{
			get
			{
				try
				{
					return LocalDb4OServerInfoConnection != null && LocalDb4OServerInfoConnection.AreProjectShared()
						// Sharing can only be turned on if this is true, because of complications when the ProjectsDirectory
						// seen by FieldWorks is different from the one seen by the FwRemoteDatabaseConnectorService.
						// However, I think it is conceivable that sharing is apparently turned on though the directories
						// are different. For one thing, it may have been turned on before this patch was released.
						// Also, while one user on the machine turned it on, another user may already have changed
						// their personal projects directory. I'm not sure whether such a user will even see that the
						// first user has turned on sharing. But in any case, the second user (who did not turn sharing on,
						// and has their own projects folder) will just go on seeing their own unshared projects.
						&& m_dirs.ProjectsDirectory == m_dirs.DefaultProjectsDirectory;
				}
				catch (SocketException)
				{
					// If service isn't running then assume projects aren't shared.
					return false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Turns project sharing on or off.
		/// </summary>
		/// <param name="fShare">if set to <c>true</c>, turn sharing on.</param>
		/// <param name="progress">The progress dialog.</param>
		/// ------------------------------------------------------------------------------------
		public bool SetProjectSharing(bool fShare, IThreadedProgress progress)
		{
			Db4oServerInfo serverInfo = null;
			do
			{
				try
				{
					serverInfo = LocalDb4OServerInfoConnection;
				}
				catch (SocketException)
				{
				}
			}
			while (serverInfo == null &&
				m_ui.Retry(string.Format(Strings.ksLocalConnectorServiceNotStarted, "FwRemoteDatabaseConnectorService"),
				fShare ? Strings.ksConvertingToShared : Strings.ksConvertingToNonShared));

			if (serverInfo == null || serverInfo.AreProjectShared() == fShare)
				return false;

			LocalDb4OServerInfoConnection.ShareProjects(fShare);
			if (LocalDb4OServerInfoConnection.AreProjectShared() != fShare)
					return false; // failed to change it
			if (fShare)
			{
				// Turning sharing on.
				if (!ConvertAllProjectsToDb4o(progress))
				{
					LocalDb4OServerInfoConnection.ShareProjects(false); // could not switch
					return false;
				}
			}
			else
			{
				if (!ConvertAllProjectsToXml(progress))
				{
					// If ConvertAllProjectsToXml failed then leave sharing on.
					LocalDb4OServerInfoConnection.ShareProjects(true);
					return false;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the specified project (in the specified parent directory) will be
		/// converted as part of the process of converting the projects in projectsDirectory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool WillProjectBeConverted(string projectPath, string parentDirectory, string projectsDirectory)
		{
			if (parentDirectory != projectsDirectory)
				return false;
			var projectFolder = Path.GetDirectoryName(projectPath);
			var suppressPath = Path.Combine(projectFolder, ksDoNotShareProjectTxt);
			return !File.Exists(suppressPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If any clients are still connected to any shared projects, display connected clients to user
		/// and allow the user to either recheck (wait) or cancel.
		/// TODO: allow the user to force disconnect clients.
		/// TODO: prevent new connections while in this shutdown phase.
		/// TODO: as an enhancement connected clients could be messaged and asked to disconnect.
		/// </summary>
		/// <returns>false if clients are still connected.</returns>
		/// ------------------------------------------------------------------------------------
		private bool EnsureNoClientsAreConnected()
		{
			var localService = LocalDb4OServerInfoConnection;
			if (localService == null)
				return true;
			string[] connectedClients = localService.ListConnectedClients();

			while (connectedClients.Length > 0)
			{
				StringBuilder connectedClientsMsg = new StringBuilder();
				foreach (var client in connectedClients)
				{
					connectedClientsMsg.AppendFormat("{2}{0} : {1}", client, Dns.GetHostEntry(client).HostName, Environment.NewLine);
				}

				if (!m_ui.Retry(String.Format(Strings.ksAllProjectsMustDisconnectClients, connectedClientsMsg),
					Strings.ksAllProjectsMustDisconnectCaption))
					return false;

				connectedClients = localService.ListConnectedClients();
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If any clients are still connected to the server file, display connected clients to user
		/// give the user an option to recheck (wait), or cancel.
		/// TODO: allow the user to force disconnect clients.
		/// TODO: prevent new connections while in this shutdown phase.
		/// TODO: as an enhancement connected clients could be messaged and asked to disconnect.
		/// </summary>
		/// <param name="projectName">project name.</param>
		/// <returns>false if clients are still connected.</returns>
		/// ------------------------------------------------------------------------------------
		private bool EnsureNoClientsAreConnected(string projectName)
		{
			Db4oServerInfo localService = LocalDb4OServerInfoConnection;
			if (localService == null)
				return true;
			string[] connectedClients = localService.ListConnectedClients(projectName);
			while (!localService.StopServer(projectName) || connectedClients.Length > 0)
			{
				StringBuilder connectedClientsMsg = new StringBuilder();
				foreach (string client in connectedClients)
					connectedClientsMsg.AppendFormat("{2}{0} : {1}", client, Dns.GetHostEntry(client).HostName, Environment.NewLine);

				if (!WarnOfOtherConnectedClients(projectName, connectedClientsMsg.ToString()))
					return false;

				connectedClients = localService.ListConnectedClients(projectName);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Warns the of other connected clients.
		/// </summary>
		/// <param name="projectName">Name of the project.</param>
		/// <param name="connectedClientsMsg">The message to show about the connected clients.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private bool WarnOfOtherConnectedClients(string projectName, string connectedClientsMsg)
		{
			var msg = String.Format(Strings.ksMustDisconnectClients, projectName, connectedClientsMsg);
			var caption = String.Format(Strings.ksMustDisconnectCaption, projectName);

			return m_ui.Retry(msg, caption);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts all projects to XML.
		/// </summary>
		/// <returns><c>true</c> if successful; <c>false</c> if other clients are connected,
		/// which prevents conversion of shared projects.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ConvertAllProjectsToXml(IThreadedProgress progressDlg)
		{
			if (!EnsureNoClientsAreConnected())
				return false;

			progressDlg.Title = Strings.ksConvertingToNonShared;
			progressDlg.AllowCancel = false;
			progressDlg.Maximum = Directory.GetDirectories(m_dirs.ProjectsDirectory).Count();
			progressDlg.RunTask(true, ConvertAllProjectsToXmlTask);
			return true;
		}

		private object ConvertAllProjectsToXmlTask(IThreadedProgress progress, object[] args)
		{
			foreach (string projectFolder in Directory.GetDirectories(m_dirs.ProjectsDirectory))
			{
				var projectName = Path.GetFileName(projectFolder);
				var projectPath = Path.Combine(projectFolder, FdoFileHelper.GetDb4oDataFileName(projectName));
				progress.Message = Path.GetFileNameWithoutExtension(projectPath);
				if (File.Exists(projectPath))
				{
					try
					{
						// The zero in the object array is for db4o and causes it not to open a port.
						// This is fine since we aren't yet trying to start up on this restored database.
						// The null says we are creating the file on the local host.
						using (FdoCache tempCache = FdoCache.CreateCacheFromExistingData(
							new SimpleProjectId(FDOBackendProviderType.kDb4oClientServer, projectPath), "en", new SilentFdoUI(progress.SynchronizeInvoke),
							m_dirs, new FdoSettings(), progress))
						{
							CopyToXmlFile(tempCache, tempCache.ProjectId.ProjectFolder);
						// Enhance JohnT: how can we tell this succeeded?
						}
						File.Delete(projectPath); // only if we converted successfully; otherwise will throw.
					}
					catch (Exception e)
					{
						ReportConversionError(projectPath, e);
					}
				}
				progress.Step(1);
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts all projects to db4o.
		/// </summary>
		/// <param name="progressDlg">The progress dialog box.</param>
		/// ------------------------------------------------------------------------------------
		private bool ConvertAllProjectsToDb4o(IThreadedProgress progressDlg)
		{
			progressDlg.Title = Strings.ksConvertingToShared;
			progressDlg.AllowCancel = false;
			progressDlg.Maximum = Directory.GetDirectories(m_dirs.ProjectsDirectory).Count();
			return (bool)progressDlg.RunTask(true, ConvertAllProjectsToDb4o);
		}

		private object ConvertAllProjectsToDb4o(IThreadedProgress progress, object[] args)
		{
			for (; ; )
			{
				string projects = "";
				foreach (string projectFolder in Directory.GetDirectories(m_dirs.ProjectsDirectory))
				{
					var projectName = Path.GetFileName(projectFolder);
					var projectPath = Path.Combine(projectFolder, FdoFileHelper.GetXmlDataFileName(projectName));
					var suppressPath = Path.Combine(projectFolder, ksDoNotShareProjectTxt);
					if (!File.Exists(projectPath) || File.Exists(suppressPath))
						continue; // not going to convert, it isn't a problem.
					if (XMLBackendProvider.IsFileLocked(projectPath))
						projects = projects + projectName + ", ";
				}
				if (projects.Length == 0)
					break;
				projects = projects.Substring(0, projects.Length - ", ".Length);

				if (!m_ui.Retry(string.Format(Strings.ksMustCloseProjectsToShare, projects), Strings.ksConvertingToShared))
					return false;
			}
			foreach (string projectFolder in Directory.GetDirectories(m_dirs.ProjectsDirectory))
			{
				string projectName = Path.GetFileName(projectFolder);
				string projectPath = Path.Combine(projectFolder, FdoFileHelper.GetXmlDataFileName(projectName));
				var suppressPath = Path.Combine(projectFolder, ksDoNotShareProjectTxt);
				progress.Message = Path.GetFileNameWithoutExtension(projectPath);
				if (File.Exists(projectPath) && !File.Exists(suppressPath))
				{
					try
					{
						ConvertToDb4oBackendIfNeeded(progress, projectPath);
					}
					catch (Exception e)
					{
						ReportConversionError(projectPath, e);
					}
				}
				progress.Step(1);
			}
			return true;
		}

		private void ReportConversionError(string projectPath, Exception e)
		{
			string message;
			if (e is FdoNewerVersionException)
			{
				message = string.Format(Strings.ksConvertFailedNewerVersion, Path.GetFileName(projectPath),
				Path.GetDirectoryName(projectPath));
			}
			else
			{
				message = string.Format(Strings.ksConvertFailedDetails, Path.GetFileName(projectPath),
					Path.GetDirectoryName(projectPath), e.Message);
			}
			m_ui.DisplayMessage(MessageType.Error, message, Strings.ksCannotConvert, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the specified project (assumed to be in the current Projects directory)
		/// to the current backend. This is used in Restore and New Project and when turning
		/// ShareMyProjects on.
		/// </summary>
		/// <param name="progressDlg">The progress dialog (for getting a message box owner and/or
		/// ensuring we're invoking on the UI thread).</param>
		/// <param name="xmlFilename">The full path of the existing XML file for the project</param>
		/// <returns>The project identifier, typically the path to the converted file (or the
		/// original, if not configured for the client-server backend)</returns>
		/// ------------------------------------------------------------------------------------
		public string ConvertToDb4oBackendIfNeeded(IThreadedProgress progressDlg, string xmlFilename)
		{
			if (!ShareMyProjects)
				return xmlFilename; // no conversion needed.
			string desiredPath = Path.ChangeExtension(xmlFilename, FdoFileHelper.ksFwDataDb4oFileExtension);
			if (!EnsureNoClientsAreConnected(Path.GetFileNameWithoutExtension(desiredPath)))
				return null; // fail

			try
			{
				using (FdoCache tempCache = FdoCache.CreateCacheFromExistingData(new SimpleProjectId(FDOBackendProviderType.kXML, xmlFilename),
					"en", new SilentFdoUI(progressDlg.SynchronizeInvoke), m_dirs, new FdoSettings(), progressDlg))
				{

				// The zero in the object array is for db4o and causes it not to open a port.
				// This is fine since we aren't yet trying to start up on this restored database.
				// The null says we are creating the file on the local host.
					using (FdoCache copyCache = FdoCache.CreateCacheCopy(new SimpleProjectId(FDOBackendProviderType.kDb4oClientServer, desiredPath),
						"en", new SilentFdoUI(progressDlg.SynchronizeInvoke), m_dirs, new FdoSettings(), tempCache))
					{
						copyCache.ServiceLocator.GetInstance<IDataStorer>().Commit(new HashSet<ICmObjectOrSurrogate>(), new HashSet<ICmObjectOrSurrogate>(), new HashSet<ICmObjectId>());
						// Enhance JohnT: how can we tell this succeeded?
					}
				}
			}
			catch (Exception)
			{
				// If we couldn't convert it, try not to leave a corrupted file around.
				File.Delete(desiredPath);
				throw;
			}

			File.Delete(xmlFilename);
			return desiredPath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the specified project (assumed to be in the current Projects directory)
		/// from the current backend to XML. This is used in Backup and when turning
		/// ShareMyProjects off. Returns the name of the temp file IF ANY; returns null
		/// if no copy was needed.
		/// </summary>
		/// <param name="source">The source cache</param>
		/// <param name="destDir">The destination directory for the converted copy if a
		/// conversion needs to be done.</param>
		/// ------------------------------------------------------------------------------------
		public string CopyToXmlFile(FdoCache source, string destDir)
		{
			IDataStorer dataStorer = source.ServiceLocator.GetInstance<IDataStorer>();
			if (dataStorer is XMLBackendProvider)
				return null; // already XML, no new file created.

			var newFilePath = Path.Combine(destDir, FdoFileHelper.GetXmlDataFileName(source.ProjectId.Name));
			if (File.Exists(newFilePath))
				File.Delete(newFilePath); // Can't create a new file with FDO if the file already exists.
			try
			{
				using (FdoCache copyCache = FdoCache.CreateCacheCopy(new SimpleProjectId(FDOBackendProviderType.kXML, newFilePath),
					"en", new SilentFdoUI(source.ServiceLocator.GetInstance<IFdoUI>().SynchronizeInvoke), m_dirs, new FdoSettings(), source))
				{
					copyCache.ServiceLocator.GetInstance<IDataStorer>().Commit(
						new HashSet<ICmObjectOrSurrogate>(),
						new HashSet<ICmObjectOrSurrogate>(),
						new HashSet<ICmObjectId>());
					// Enhance JohnT: how can we tell this succeeded?
					return newFilePath;
				}
			}
			catch (Exception)
			{
				File.Delete(newFilePath);
				throw;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the full ID that should be used to open the specified project, which is in the
		/// local projects directory. The input project name must NOT have an extension already.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string IdForLocalProject(string projectName)
		{
			// Project Name must not have an extension. Can't use ChangeExtension because
			// the project name might contain some other period.
			Debug.Assert(!projectName.EndsWith(".fwdata") && !projectName.EndsWith(".fwdb"));
			string projectDirectory = Path.Combine(m_dirs.ProjectsDirectory, projectName);
			var result = Path.Combine(projectDirectory, projectName + DefaultBackendType.GetExtension());
			if (!File.Exists(result))
			{
				// See if the other version exists. If so return an ID for that.
				if (DefaultBackendType.GetExtension() != ".fwdata")
				{
					var altResult = Path.ChangeExtension(result, "fwdata");
					if (File.Exists(altResult))
						return altResult;
				}
		}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default type of backend.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOBackendProviderType DefaultBackendType
		{
			get { return ShareMyProjects ? FDOBackendProviderType.kDb4oClientServer : FDOBackendProviderType.kXML; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds Client server projects that are in use.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] ListOpenProjects()
		{
			return LocalDb4OServerInfoConnection.ListRunningServers();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds all clients that are using specified project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] ListConnectedClients(string project)
		{
			if (string.IsNullOrEmpty(project))
				throw new ArgumentException();

			return LocalDb4OServerInfoConnection.ListConnectedClients(project);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds all clients, except local host, that are using specified project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] ListRemoteConnectedClients(string project)
		{
			if (string.IsNullOrEmpty(project))
				throw new ArgumentException();

			Db4oServerInfo localService = LocalDb4OServerInfoConnection;
			return localService.ListConnectedClients(project).Where(x => !localService.IsLocalHost(x)).ToArray();
		}

		internal static bool IsLocalServiceRunning()
		{
			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
			return tcpConnInfoArray.Any(endPoint => endPoint.Port == Db4OServerFinder.ServiceDiscoveryPort);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the server information for the local DB4o server service.
		/// </summary>
		/// <exception cref="SocketException">If the service is not running.</exception>
		/// ------------------------------------------------------------------------------------
		internal static Db4oServerInfo LocalDb4OServerInfoConnection
		{
			get
			{
				// Optimization, This speeds things up if service isn't running.
				return (IsLocalServiceRunning() ?
					Db4oClientServerBackendProvider.GetDb4OServerInfo(kLocalService, Db4OServerFinder.ServiceDiscoveryPort) : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Force the list of project names to be reloaded.  (This is needed after deleting
		/// a project.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshProjectNames()
		{
			try
			{
				if (LocalDb4OServerInfoConnection == null)
					return;

				LocalDb4OServerInfoConnection.RefreshServerList();
			}
			catch (SocketException)
			{
			}
		}
	}
	#endregion
}
