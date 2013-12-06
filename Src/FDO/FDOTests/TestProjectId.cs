// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2010' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TestProjectId.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Project identification used for testing purposes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestProjectId : IProjectIdentifier
	{
		private readonly FDOBackendProviderType m_type;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TestProjectId"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TestProjectId(FDOBackendProviderType type, string name)
		{
			m_type = type;
			Path = name;
		}

		#region IProjectIdentifier implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the UI name of the project (this will typically be formatted as [Name]
		/// for local projects and [Name]-[ServerName] for remote projects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UiName
		{
			get { return Name; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the project path (typically a full path to the file) for local projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Path { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a token that uniquely identifies the project on its host (whether localhost or
		/// remote Server). This might look like a full path in some situations but should never
		/// be used as a path; use the <see cref="Path"/> property instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Handle
		{
			get { return Name; }
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
		/// Gets the project short name (the project name without an extension or path.
		/// Typically this will be the same as <see cref="Path"/> for remote projects)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the folder that contains the project (can be <c>null</c> for
		/// remote projects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectFolder
		{
			get { return System.IO.Path.GetDirectoryName(Path); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A possibly alternate project path that should be used for things that should be
		/// shared. This includes writing systems, etc. and possibly linked files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SharedProjectFolder
		{
			get { return ProjectFolder; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the server (can be <c>null</c> for a local project).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ServerName
		{
			get { return null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of back-end used for storing the project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOBackendProviderType Type
		{
			get { return m_type; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this project is on the local host.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLocal
		{
			get { return true; }
		}
		#endregion
	}
}
