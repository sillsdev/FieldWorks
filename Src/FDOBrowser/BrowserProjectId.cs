// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BrowserProjectId.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace FDOBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Project identifier for use in the FDOBrowser.
	/// NOTE: This project id doesn't NOT work for remote projects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BrowserProjectId : IProjectIdentifier
	{
		private readonly FDOBackendProviderType m_type;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BrowserProjectId"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BrowserProjectId(FDOBackendProviderType type, string name)
		{
			m_type = type;
			Path = name;
		}

		#region IProjectIdentifier Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this project is on the local host.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLocal
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the project path (typically a full path to the file) for local projects.
		/// </summary>
		/// <exception cref="T:System.InvalidOperationException">If the project is on a remote host</exception>
		/// ------------------------------------------------------------------------------------
		public string Path { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the folder that contains the project (will typically be <c>null</c> for
		/// remote projects).
		/// </summary>
		/// <value></value>
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
		/// Gets the project name (typically the project path without an extension or folder)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
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
		/// Gets the UI name of the project (this will typically be formatted as [Name]
		/// for local projects and [Name]-[ServerName] for remote projects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UiName
		{
			get { return Name; }
		}

		#endregion
	}
}
