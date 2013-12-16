// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SimpleProjectId.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------

using System;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Very simple implementation of <see cref="IProjectIdentifier"/> for use when creating
	/// temporary caches that don't need to do much (e.g. used in switching projects to a new
	/// backend).
	/// This simple implementation can NOT be used for remote projects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class SimpleProjectId : IProjectIdentifier
	{
		private readonly FDOBackendProviderType m_type;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleProjectId"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleProjectId(FDOBackendProviderType type, string name)
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
		/// Gets or sets the project path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Path { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a token that uniquely identifies the project. This might look like a full path
		/// but should never be used as a path; use the <see cref="Path"/> property instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Handle
		{
			get { return Path; }
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
		/// Gets the project name (typically the project path without an extension or folder)
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the folder that contains the project (will typically be <c>null</c> for
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
		/// Gets the name of the server (will typically be <c>null</c> for a local project).
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
