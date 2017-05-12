// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IProjectIdentifier.cs
// Responsibility: FW Team

using System;

namespace SIL.FieldWorks.FDO
{
	#region FDOBackendProviderType enum
	/// <summary>
	/// Supported backend data providers.
	/// </summary>
	public enum FDOBackendProviderType
	{
		/// <summary>
		/// An invalid type
		/// </summary>
		kInvalid,

		/// <summary>
		/// A FieldWorks XML file.
		/// </summary>
		/// <remarks>uses XMLBackendProvider</remarks>
		kXML,

		/// <summary>
		/// A mostly 'do nothing' backend.
		/// This backend is used where there is no actual backend data store on the hard drive.
		/// This could be used for tests, for instance, that create all FDO test data themselves.
		/// </summary>
		/// <remarks>uses MemoryOnlyBackendProvider</remarks>
		kMemoryOnly,

		/// <summary>
		/// A FieldWorks XML file.
		/// This has an actual backend data store on the hard drive, but does not use a real
		/// repository of writing systems. There is probably no legitimate reason to use this
		/// except for testing the XML BEP.
		/// </summary>
		/// <remarks>uses XMLBackendProvider</remarks>
		kXMLWithMemoryOnlyWsMgr,

		/// <summary>
		/// A FieldWorks XML file that can be accessed by multiple local clients.
		/// </summary>
		kSharedXML = 104,

		/// <summary>
		/// A FieldWorks XML file that can be accessed by multiple local clients.
		/// This has an actual backend data store on the hard drive, but does not use a real
		/// repository of writing systems. There is probably no legitimate reason to use this
		/// except for testing the shared XML BEP.
		/// </summary>
		kSharedXMLWithMemoryOnlyWsMgr = 105
	};
	#endregion

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
		/// Gets a token that uniquely identifies the project that can be used for a named pipe.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string PipeHandle { get; }

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
		/// Gets the type of back-end used for storing the project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FDOBackendProviderType Type { get; }
	}
}
