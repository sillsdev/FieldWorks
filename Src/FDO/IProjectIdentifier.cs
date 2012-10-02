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
// File: IProjectIdentifier.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An interface that represents a FieldWorks project for a back-end provider
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IProjectIdentifier
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the UI name of the project (this will typically be formatted as [Name]
		/// for local projects and [Name]-[ServerName] for remote projects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string UiName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the project path for local projects (this is a full path to the file
		/// for local file-based BEPs and just a name for client-server BEPs).
		/// </summary>
		/// <exception cref="InvalidOperationException">If the project is on a remote host</exception>
		/// ------------------------------------------------------------------------------------
		string Path { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a token that uniquely identifies the project on its host (whether localhost or
		/// remote Server). This might look like a full path in some situations but should never
		/// be used as a path; use the <see cref="Path"/> property instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Handle { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the project name (no extension or folder)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Name { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the folder that contains the project file for a local project or the folder
		/// where local settings will be saved for remote projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ProjectFolder { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A possibly alternate project path that should be used for things that should be
		/// shared. This includes writing systems, etc. and possibly linked files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string SharedProjectFolder { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the server (will typically be <c>null</c> for a local project).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ServerName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of back-end used for storing the project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FDOBackendProviderType Type { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this project is on the local host.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsLocal { get; }
	}
}
