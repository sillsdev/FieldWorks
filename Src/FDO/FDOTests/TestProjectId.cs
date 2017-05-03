// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Project identification used for testing purposes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestProjectId : IProjectIdentifier
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TestProjectId"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TestProjectId(FDOBackendProviderType type, string name)
		{
			Type = type;
			Path = name;
		}

		#region IProjectIdentifier implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the UI name of the project (this will typically be formatted as [Name]
		/// for local projects and [Name]-[ServerName] for remote projects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UiName => Name;

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
		public string Handle => Name;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a token that uniquely identifies the project that can be used for a named pipe.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PipeHandle => $"FieldWorks:{Handle}";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the project short name (the project name without an extension or path.
		/// Typically this will be the same as <see cref="Path"/> for remote projects)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the folder that contains the project (can be <c>null</c> for
		/// remote projects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectFolder => System.IO.Path.GetDirectoryName(Path);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A possibly alternate project path that should be used for things that should be
		/// shared. This includes writing systems, etc. and possibly linked files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SharedProjectFolder => ProjectFolder;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the server (can be <c>null</c> for a local project).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ServerName => null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of back-end used for storing the project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOBackendProviderType Type { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this project is on the local host.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLocal => true;
		#endregion
	}
}
