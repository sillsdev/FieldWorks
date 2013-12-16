// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProjectId.cs
// Responsibility: FW team

using System;
using System.Diagnostics;
using System.Net;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using System.Runtime.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using System.Net.Sockets;
using SysPath = System.IO.Path;
using System.Security;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents the identifying information for a FW project (which may or may not actually
	/// exist)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class ProjectId : ISerializable, IProjectIdentifier
	{
		#region Constants
		private const string kTypeSerializeName = "Type";
		private const string kNameSerializeName = "Name";
		private const string kNameSerializeServer = "Server";
		private const string kLocalHostIp = "127.0.0.1";
		#endregion

		#region Member variables
		private string m_path;
		private readonly FDOBackendProviderType m_type;
		private readonly string m_serverName;
		#endregion

		#region Constructors
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectId"/> class for a local
		/// project
		/// </summary>
		/// <param name="name">The project name (project type will be inferred from the
		/// extension if this is a filename and server is null).</param>
		/// <param name="server">The remote host name or <c>null</c> for a local project.
		/// </param>
		/// --------------------------------------------------------------------------------
		public ProjectId(string name, string server)
			: this(GetType(null, name, server), name, server)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectId"/> class.
		/// </summary>
		/// <param name="type">The type of BEP (or <c>null</c> to infer type).</param>
		/// <param name="name">The project name (for local projects, this can be a filename).
		/// </param>
		/// <param name="server">The remote host name or <c>null</c> for a local project.
		/// </param>
		/// --------------------------------------------------------------------------------
		public ProjectId(string type, string name, string server) :
			this(GetType(type, name, server), name, server)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectId"/> class when called for
		/// deserialization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ProjectId(SerializationInfo info, StreamingContext context) :
			this((FDOBackendProviderType)info.GetValue(kTypeSerializeName, typeof(FDOBackendProviderType)),
			info.GetString(kNameSerializeName), info.GetString(kNameSerializeServer))
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectId"/> class.
		/// </summary>
		/// <param name="type">The type of BEP.</param>
		/// <param name="name">The project name (for local projects, this can be a filename).
		/// </param>
		/// <param name="server">The remote host name or <c>null</c> for a local project.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public ProjectId(FDOBackendProviderType type, string name, string server)
		{
			Debug.Assert(type != FDOBackendProviderType.kMemoryOnly);
			m_type = type;
			m_path = CleanUpNameForType(type, name, server);
			if (Type == FDOBackendProviderType.kDb4oClientServer)
				m_serverName = ResolveServer(server);
		}
		#endregion

		private static string s_localHostName;

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the fully-qualified name of the local host (Dns.GetHostName() might not be
		/// fully-qualified)
		/// </summary>
		/// <remarks>JohnT: changed this so it only does the real retrieval once. This method
		/// is probably not called enough to make this an important optimization, but the
		/// problems noted in LT-11653 seem to indicate that there are 'wrong' moments
		/// or threads on which to call GetHostName(); we get spurious exceptions claiming
		/// we don't have the needed privilege for DNS operations. Fortunately it is
		/// called at a safe moment early in startup when it works correctly.
		/// This is not very robust since I don't really know why it fails sometimes,
		/// therefore I don't know how to make sure that the one time we actually call it
		/// will succeed. But it fixed the problem.</remarks>
		/// ------------------------------------------------------------------------------------
		private static string LocalHostName
		{
			get
			{
				if (s_localHostName == null)
				{
					string hostName = Dns.GetHostName();
					string fullHostName = Dns.GetHostEntry(hostName).HostName;

					s_localHostName = string.IsNullOrEmpty(fullHostName) ? hostName : fullHostName;
				}
				return s_localHostName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Type of BEP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOBackendProviderType Type
		{
			get { return m_type; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the project path (typically a full path to the file) for local projects.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">If the project is on a remote
		/// host</exception>
		/// ------------------------------------------------------------------------------------
		public string Path
		{
			get
			{
				if (!IsLocal)
					throw new InvalidOperationException("Can not get the project path for a project on a remote server.");
				return m_path;
			}
			set
			{
				if (!IsLocal)
					throw new InvalidOperationException("Can not set the project path for a project on a remote server.");
				m_path = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the server (will typically be <c>null</c> for a local project).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ServerName
		{
			get { return m_serverName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a token that uniquely identifies the project on its host (whether localhost or
		/// remote Server). This might look like a full path in some situations but should never
		/// be used as a path; use the <see cref="Path"/> property instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Handle
		{
			get
			{
				return !IsLocal || (DirectoryFinder.IsSubFolderOfProjectsDirectory(ProjectFolder) &&
					SysPath.GetExtension(m_path) == ClientServerServices.Current.Local.DefaultBackendType.GetExtension()) ?
					Name : m_path;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a token that uniquely identifies the project that can be used for a named pipe.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PipeHandle
		{
			get { return FwUtils.GeneratePipeHandle(Handle); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a token that uniquely identifies the project that can be used for a named pipe,
		/// but that does not contain the preceding application identifier (i.e. 'FieldWorks:').
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ShortPipeHandle
		{
			get { return PipeHandle.Substring(FwUtils.ksSuiteName.Length + 1); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the project name (typically the project path without an extension or folder)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return SysPath.GetFileNameWithoutExtension(m_path); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the folder that contains the project file for a local project or the folder
		/// where local settings will be saved for remote projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectFolder
		{
			get
			{
				return IsLocal ? SysPath.GetDirectoryName(m_path) :
					SysPath.Combine(SysPath.Combine(DirectoryFinder.ProjectsDirectory, ServerName), Name);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A possibly alternate project path that should be used for things that should be
		/// shared. This includes writing systems, etc. and possibly linked files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SharedProjectFolder
		{
			get
			{
				if (IsLocal)
					return ProjectFolder;
				// TODO-Linux FWNX-446: Implement alternative way of getting path to shared folder
				// Currently assumes projects also exist in the local Project Directory.
				string baseDir = (MiscUtils.IsUnix) ? DirectoryFinder.ProjectsDirectory :
					@"\\" + ServerName + @"\Projects";
				return SysPath.Combine(baseDir, Name);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this project is on the local host.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLocal
		{
			get { return IsServerLocal(ServerName); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the UI name of the project (this will typically be formatted as [Name]
		/// for local projects and [Name]-[ServerName] for remote projects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UiName
		{
			get
			{
				switch (Type)
				{
					case FDOBackendProviderType.kDb4oClientServer:
						if (IsLocal)
							return Name;

						string hostName = ServerName;
						try
						{
							string localName = LocalHostName;
							if (localName != hostName)
							{
								// If both names are fully-qualified domain names (for example,
								// ls-mcconnel.dallas.sil.org), and both are in the same domain, strip
								// off the domain (.dallas.sil.org for the example).
								int idxLocal = localName.IndexOf('.');
								int idxHost = hostName.IndexOf('.');
								if (idxLocal > 0 && idxHost > 0 &&
									localName.Substring(idxLocal).Equals(hostName.Substring(idxHost), StringComparison.InvariantCultureIgnoreCase))
								{
									hostName = hostName.Substring(0, idxHost);
								}
							}
						}
						catch (SocketException)
						{
							// Ignore any errors attempting to get a better host name
						}
						return string.Format(Properties.Resources.ksProjectNameAndServerFmt, Name, hostName);
					case FDOBackendProviderType.kXML:
						return (SysPath.GetExtension(Path) != FwFileExtensions.ksFwDataXmlFileExtension) ?
							SysPath.GetFileName(Path) : Name;
					case FDOBackendProviderType.kInvalid:
						return string.Empty;
					default:
						Debug.Fail("Need to handle getting the project name for this BEP");
						return string.Empty;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the identification info in this project is a
		/// valid FW project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsValid
		{
			get
			{
				var ex = GetExceptionIfInvalid();
				if (ex == null)
					return true;
				if (ex is FwStartupException)
					return false;
				// something totally unexpected that we don't know how to handle happened.
				// Don't suppress it.
				throw ex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the BEP type as a string (The same string that is used for determining the
		/// FDOBackendProviderType from the command line).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TypeString
		{
			get
			{
				switch (Type)
				{
					case FDOBackendProviderType.kDb4oClientServer: return "db4oCS";
					case FDOBackendProviderType.kXML: return "xml";
					default: return string.Empty;
				}
			}
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception if this ProjectId is not valid. Avoid using this and catching
		/// the exception when in doubt...use only when it is really an error for it to be invalid.
		///
		/// here.
		/// </summary>
		/// <exception cref="FwStartupException">If invalid (e.g., project Name is not set, the
		/// XML file can not be found, etc.)</exception>
		/// ------------------------------------------------------------------------------------
		public void AssertValid()
		{
			var ex = GetExceptionIfInvalid();
			if (ex == null)
				return;
			throw ex;
		}

		/// <summary>
		/// Return an appropriate exception to throw if the project is expected to be valid and
		/// is not. This is a basic test for what could reasonably be a
		/// FieldWorks project. No checking to see if the project is openable is actually done.
		/// (For example, the file must exist, but it's contents are not checked.)
		/// </summary>
		/// <returns></returns>
		internal Exception GetExceptionIfInvalid()
		{
			if (string.IsNullOrEmpty(Name))
				return new FwStartupException(Properties.Resources.kstidNoProjectName, false);

			switch (Type)
			{
				case FDOBackendProviderType.kDb4oClientServer:
					try
					{
						Dns.GetHostEntry(ServerName);
					}
					catch (SocketException e)
					{
						return new FwStartupException(String.Format(Properties.Resources.kstidInvalidServer, ServerName, e.Message), e);
					}
					catch (Exception ex)
					{
						return ex;
					}
					break;
				case FDOBackendProviderType.kXML:
					if (!FileUtils.SimilarFileExists(Path))
						return new FwStartupException(string.Format(Properties.Resources.kstidFileNotFound, Path));
					break;
				case FDOBackendProviderType.kInvalid:
					return new FwStartupException(Properties.Resources.kstidInvalidFwProjType);
				default:
					return new NotImplementedException("Unknown type of project.");
			}

			// Check this after checking for other valid information (e.g. if the server is
			// not available, we want to show that error, not this error).
			if (!FileUtils.DirectoryExists(SharedProjectFolder))
				return new FwStartupException(String.Format(Properties.Resources.kstidCannotAccessProjectPath, SharedProjectFolder));
			return null; // valid
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare this ProjectId to another ProjectId return true if they point to the same
		/// local project, but ignoring the file extension (because one of the projects is
		/// expected to be a newly restored XML project).
		/// For example c:\TestLangProj.fwdata and c:\TestLangProj.fwdb would be equal.
		/// </summary>
		/// <param name="otherProjectId">The other project id.</param>
		/// ------------------------------------------------------------------------------------
		public bool IsSameLocalProject(ProjectId otherProjectId)
		{
			return (IsLocal && otherProjectId.IsLocal &&
				ProjectFolder.Equals(otherProjectId.ProjectFolder, StringComparison.InvariantCultureIgnoreCase) &&
				ProjectInfo.ProjectsAreSame(Name, otherProjectId.Name));
		}
		#endregion

		#region Object Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			ProjectId projB = obj as ProjectId;
			if (projB == null)
				throw new ArgumentException("Argument is not a ProjectId.", "obj");
			return (Type == projB.Type && ProjectInfo.ProjectsAreSame(Handle, projB.Handle) &&
				((ServerName == null && projB.ServerName == null) ||
				(ServerName != null && projB.ServerName != null &&
				ServerName.Equals(projB.ServerName, StringComparison.InvariantCultureIgnoreCase))));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return Type.GetHashCode() ^ (m_path == null ? 0 : m_path.ToLowerInvariant().GetHashCode()) ^
				(m_serverName == null ? 0 : m_serverName.GetHashCode());
		}
		#endregion

		#region ISerializable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the
		/// data needed to serialize the target object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(kNameSerializeName, m_path);
			info.AddValue(kTypeSerializeName, Type);
			info.AddValue(kNameSerializeServer, ServerName);
		}
		#endregion

		#region Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans the name of the project given the project type. (e.g. For an XML type, this
		/// will ensure that the name is rooted and ends with the correct extension)
		/// </summary>
		/// <param name="type">The type of the project.</param>
		/// <param name="name">The name of the project.</param>
		/// <param name="server">The name or IP address of the server (or <c>null</c> for a
		/// local project).</param>
		/// <returns>The cleaned up name with the appropriate extension</returns>
		/// ------------------------------------------------------------------------------------
		private static string CleanUpNameForType(FDOBackendProviderType type, string name, string server)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			string ext;

			switch (type)
			{
				case FDOBackendProviderType.kXML:
					ext = FwFileExtensions.ksFwDataXmlFileExtension;
					break;
				case FDOBackendProviderType.kDb4oClientServer:
					if (!IsServerLocal(server))
						return name;
					ext = FwFileExtensions.ksFwDataDb4oFileExtension;
					break;
				default:
					return name;
			}

			if (!SysPath.IsPathRooted(name))
			{
				string sProjName = (SysPath.GetExtension(name) == ext) ? SysPath.GetFileNameWithoutExtension(name) : name;
				name = SysPath.Combine(SysPath.Combine(DirectoryFinder.ProjectsDirectory, sProjName), name);
			}
			// If the file doesn't have the expected extension and exists with the extension or
			// does not exist without it, we add the expected extension.
			if (SysPath.GetExtension(name) != ext && (FileUtils.SimilarFileExists(name + ext) || !FileUtils.SimilarFileExists(name)))
				name += ext;
			return name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the BEP type from the given type; otherwise, infer it from the pathname
		/// extension/server.
		/// </summary>
		/// <param name="type">The type string.</param>
		/// <param name="pathname">The pathname.</param>
		/// <param name="server">The server.</param>
		/// ------------------------------------------------------------------------------------
		private static FDOBackendProviderType GetType(string type, string pathname, string server)
		{
			if (!string.IsNullOrEmpty(type))
			{
				switch (type.ToLowerInvariant())
				{
					case "db4ocs": return FDOBackendProviderType.kDb4oClientServer;
					case "xml": return FDOBackendProviderType.kXML;
					default: return FDOBackendProviderType.kInvalid;
				}
			}

			if (!string.IsNullOrEmpty(server))
				return FDOBackendProviderType.kDb4oClientServer;

			string ext = SysPath.GetExtension(pathname);
			if (!string.IsNullOrEmpty(ext))
			{
				ext = ext.ToLowerInvariant();
				switch (ext)  // Includes period.
				{
					case ".db4o": // for historical purposes
					case FwFileExtensions.ksFwDataDb4oFileExtension:
						return FDOBackendProviderType.kDb4oClientServer;
					case FwFileExtensions.ksFwDataXmlFileExtension:
						return FDOBackendProviderType.kXML;
				}
			}
			return ClientServerServices.Current.Local.DefaultBackendType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to resolve the specified server using the current DNS server.
		/// </summary>
		/// <param name="server">The server to resolve (host name or IP address).</param>
		/// <returns>The resolved host name of the specifed server</returns>
		/// ------------------------------------------------------------------------------------
		private static string ResolveServer(string server)
		{
			try
			{
				if (String.IsNullOrEmpty(server))
					return LocalHostName;

				// This looks strange, but is needed for consistency when comparing different
				// ProjectIds to each other and for comparing with the local host.
				return Dns.GetHostEntry(server).HostName;
			}
			catch (SocketException)
			{
				// Problem finding the server so let the AssertValid method handle the real problem
				// and report it to the user.
			}
			catch (SecurityException)
			{
				// Problem accessing the DNS routines (probably as a result of getting called
				// from another process when attempting to determine if a process is running
				// with a particular project open).
			}

			return server;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified server is considered to be a local server.
		/// </summary>
		/// <param name="serverName">Name of the server to check.</param>
		/// ------------------------------------------------------------------------------------
		private static bool IsServerLocal(string serverName)
		{
			return string.IsNullOrEmpty(serverName) || serverName == LocalHostName ||
				serverName.ToLowerInvariant() == FwLinkArgs.kLocalHost || serverName == kLocalHostIp ||
				// Workaround for Ubuntu bug: 127.0.0.1 is reported as localhost.localdomain instead of just localhost
				serverName == Dns.GetHostEntry(kLocalHostIp).HostName;
		}
		#endregion
	}
}
