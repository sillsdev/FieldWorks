// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IFieldWorksManager.cs
// Responsibility: FW Team

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for handling FieldWorks-level (i.e. above the application level) stuff.
	/// This includes:
	/// - Handling/creating of projects
	/// - Creating/managing the FdoCache
	/// - Handling FieldWorks-level synchronization messages
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFieldWorksManager
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FdoCache Cache { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shutdowns the specified application. The application will be disposed of immediately.
		/// If no other applications are running, then FieldWorks will also be shutdown.
		/// </summary>
		/// <param name="app">The application to shut down.</param>
		/// ------------------------------------------------------------------------------------
		void ShutdownApp(FwApp app);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the specified method asynchronously. The method will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// <param name="action">The action to execute</param>
		/// <param name="param1">The first parameter of the action.</param>
		/// ------------------------------------------------------------------------------------
		void ExecuteAsync<T>(Action<T> action, T param1);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a new main window for the specified application.
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="wndToCopyFrom">The window to copy information from (optional).</param>
		/// ------------------------------------------------------------------------------------
		void OpenNewWindowForApp(FwApp app, Form wndToCopyFrom);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user chooses a language project and opens it. If the project is already
		/// open in a FieldWorks process, then the request is sent to the running FieldWorks
		/// process and a new window is opened for that project. Otherwise a new FieldWorks
		/// process is started to handle the project request.
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner for the dialog.</param>
		/// ------------------------------------------------------------------------------------
		void ChooseLangProject(FwApp app, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user create a new language project and opens it. If the project is already
		/// open in a FieldWorks process, then the request is sent to the running FieldWorks
		/// process and a new window is opened for that project. Otherwise a new FieldWorks
		/// process is started to handle the new project.
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner for the dialog.</param>
		/// ------------------------------------------------------------------------------------
		void CreateNewProject(FwApp app, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user delete any FW databases that are not currently open
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// ------------------------------------------------------------------------------------
		void DeleteProject(FwApp app, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user backup any FW databases that are not currently open
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// <returns>The path to the backup file, or <c>null</c> if the user cancels the
		/// backup</returns>
		/// ------------------------------------------------------------------------------------
		string BackupProject(FwApp app, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore a project.
		/// </summary>
		/// <param name="fwApp">The FieldWorks application.</param>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// ------------------------------------------------------------------------------------
		void RestoreProject(FwApp fwApp, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user archive data using RAMP
		/// </summary>
		/// <param name="fwApp">The application</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// <returns>The list of the files to archive, or <c>null</c> if the user cancels the
		/// archive dialog</returns>
		/// ------------------------------------------------------------------------------------
		List<string> ArchiveProjectWithRamp(FwApp fwApp, Form dialogOwner);

		/// <summary>
		/// Reopens the given FLEx project. This may be necessary if some external process modified the project data.
		/// Currently used when FLExBridge modifies our project during a Send/Receive
		/// </summary>
		/// <param name="project">The project name to re-open</param>
		/// <param name="app"></param>
		FwApp ReopenProject(string project, FwAppArgs app);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="fwApp">The FieldWorks application.</param>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// ------------------------------------------------------------------------------------
		void FileProjectSharingLocation(FwApp fwApp, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rename the project used by this FieldWorks to the specified new name.
		/// </summary>
		/// <param name="newName">The new name</param>
		/// <param name="app">The calling application</param>
		/// <returns>True if the rename was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		bool RenameProject(string newName, FwApp app);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a link request. This is expected to handle determining the correct
		/// application to start up on the correct project and passing the link to any newly
		/// started application.
		/// </summary>
		/// <param name="link">The link.</param>
		/// ------------------------------------------------------------------------------------
		void HandleLinkRequest(FwAppArgs link);
	}
}
