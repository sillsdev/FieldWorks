// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProjectId.cs
// Responsibility: FW team
using System;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using System.Runtime.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using SysPath = System.IO.Path;

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
		#endregion

		#region Member variables
		private string m_path;
		private FDOBackendProviderType m_type;
		#endregion

		#region Constructors
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectId"/> class for a local
		/// project
		/// </summary>
		/// <param name="name">The project name (project type will be inferred from the
		/// extension if this is a filename).</param>
		/// --------------------------------------------------------------------------------
		public ProjectId(string name)
			: this(GetType("xml", name), name)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectId"/> class.
		/// </summary>
		/// <param name="type">The type of BEP (or <c>null</c> to infer type).</param>
		/// <param name="name">The project name (for local projects, this can be a filename).
		/// </param>
		/// --------------------------------------------------------------------------------
		public ProjectId(string type, string name) :
			this(GetType(type, name), name)
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
			info.GetString(kNameSerializeName))
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectId"/> class.
		/// </summary>
		/// <param name="type">The type of BEP.</param>
		/// <param name="name">The project name (for local projects, this can be a filename).
		/// </param>
		/// ------------------------------------------------------------------------------------
		public ProjectId(FDOBackendProviderType type, string name)
		{
			Debug.Assert(type != FDOBackendProviderType.kMemoryOnly);
			m_type = type;
			m_path = CleanUpNameForType(type, name);
		}
		#endregion

		private static string s_localHostName;

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Type of BEP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOBackendProviderType Type
		{
			get { return m_type; }
			set { m_type = value; }
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
				return m_path;
			}
			set
			{
				m_path = value;
			}
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
				return (FwDirectoryFinder.IsSubFolderOfProjectsDirectory(ProjectFolder) &&
					SysPath.GetExtension(m_path) == FdoFileHelper.ksFwDataXmlFileExtension) ?
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
				return SysPath.GetDirectoryName(m_path);
			}
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
					case FDOBackendProviderType.kXML:
					case FDOBackendProviderType.kSharedXML:
						return (SysPath.GetExtension(Path) != FdoFileHelper.ksFwDataXmlFileExtension) ?
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
				if (ex is StartupException)
					return false;
				// something totally unexpected that we don't know how to handle happened.
				// Don't suppress it.
				throw ex;
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
		/// <exception cref="StartupException">If invalid (e.g., project Name is not set, the
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
				return new StartupException(Properties.Resources.kstidNoProjectName, false);

			switch (Type)
			{
				case FDOBackendProviderType.kXML:
				case FDOBackendProviderType.kSharedXML:
					if (!FileUtils.SimilarFileExists(Path))
						return new StartupException(string.Format(Properties.Resources.kstidFileNotFound, Path));
					break;
				case FDOBackendProviderType.kInvalid:
					return new StartupException(Properties.Resources.kstidInvalidFwProjType);
				default:
					return new NotImplementedException("Unknown type of project.");
			}

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
			return (ProjectFolder.Equals(otherProjectId.ProjectFolder, StringComparison.InvariantCultureIgnoreCase) &&
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
			return (Type == projB.Type && ProjectInfo.ProjectsAreSame(Handle, projB.Handle));
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
			return Type.GetHashCode() ^ (m_path == null ? 0 : m_path.ToLowerInvariant().GetHashCode());
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
		/// <returns>The cleaned up name with the appropriate extension</returns>
		/// ------------------------------------------------------------------------------------
		private static string CleanUpNameForType(FDOBackendProviderType type, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			string ext;

			switch (type)
			{
				case FDOBackendProviderType.kXML:
					ext = FdoFileHelper.ksFwDataXmlFileExtension;
					break;
				default:
					return name;
			}

			if (!SysPath.IsPathRooted(name))
			{
				string sProjName = (SysPath.GetExtension(name) == ext) ? SysPath.GetFileNameWithoutExtension(name) : name;
				name = SysPath.Combine(SysPath.Combine(FwDirectoryFinder.ProjectsDirectory, sProjName), name);
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
		/// ------------------------------------------------------------------------------------
		private static FDOBackendProviderType GetType(string type, string pathname)
		{
			if (!string.IsNullOrEmpty(type))
			{
				switch (type.ToLowerInvariant())
				{
					case "xml": return FDOBackendProviderType.kXML;
					default: return FDOBackendProviderType.kInvalid;
				}
			}

			string ext = SysPath.GetExtension(pathname);
			if (!string.IsNullOrEmpty(ext))
			{
				ext = ext.ToLowerInvariant();
				switch (ext)  // Includes period.
				{
					case FdoFileHelper.ksFwDataXmlFileExtension:
						return FDOBackendProviderType.kXML;
				}
			}
			return FDOBackendProviderType.kXML;
		}
		#endregion
	}
}
