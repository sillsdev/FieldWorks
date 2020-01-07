// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LCMBrowser
{
	/// <summary>
	/// Project identifier for use in the LCMBrowser.
	/// NOTE: This project id doesn't NOT work for remote projects.
	/// </summary>
	public class BrowserProjectId : IProjectIdentifier
	{
		/// <summary />
		public BrowserProjectId(BackendProviderType type, string name)
		{
			Type = type;
			Path = name;
		}

		#region IProjectIdentifier Members

		/// <summary>
		/// Gets a value indicating whether this project is on the local host.
		/// </summary>
		public bool IsLocal => true;

		/// <summary>
		/// Gets or sets the project path (typically a full path to the file) for local projects.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">If the project is on a remote host</exception>
		public string Path { get; set; }

		/// <summary>
		/// Gets the folder that contains the project (will typically be <c>null</c> for
		/// remote projects).
		/// </summary>
		public string ProjectFolder => System.IO.Path.GetDirectoryName(Path);

		/// <summary>
		/// A possibly alternate project path that should be used for things that should be
		/// shared. This includes writing systems, etc. and possibly linked files.
		/// </summary>
		public string SharedProjectFolder => ProjectFolder;

		/// <summary>
		/// Gets the name of the server (will typically be <c>null</c> for a local project).
		/// </summary>
		public string ServerName => null;

		/// <summary>
		/// Gets a token that uniquely identifies the project on its host (whether localhost or
		/// remote Server). This might look like a full path in some situations but should never
		/// be used as a path; use the <see cref="Path"/> property instead.
		/// </summary>
		public string Handle => Name;

		/// <summary>
		/// Gets a token that uniquely identifies the project that can be used for a named pipe.
		/// </summary>
		public string PipeHandle => FwUtils.GeneratePipeHandle(Handle);

		/// <summary>
		/// Gets the project name (typically the project path without an extension or folder)
		/// </summary>
		public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

		/// <summary>
		/// Gets the type of back-end used for storing the project.
		/// </summary>
		public BackendProviderType Type { get; }

		/// <summary>
		/// Gets the UI name of the project (this will typically be formatted as [Name]
		/// for local projects and [Name]-[ServerName] for remote projects).
		/// </summary>
		public string UiName => Name;

		#endregion
	}
}