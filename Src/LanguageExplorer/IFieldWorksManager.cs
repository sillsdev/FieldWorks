// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for handling FieldWorks-level (i.e. above the application level) stuff.
	/// This includes:
	/// - Handling/creating of projects
	/// - Creating/managing the LcmCache
	/// - Handling FieldWorks-level synchronization messages
	/// </summary>
	public interface IFieldWorksManager
	{
		/// <summary>
		/// Gets the cache.
		/// </summary>
		LcmCache Cache { get; }

		/// <summary>
		/// Shutdowns the specified application. The application will be disposed of immediately.
		/// If no other applications are running, then FieldWorks will also be shutdown.
		/// </summary>
		/// <param name="app">The application to shut down.</param>
		void ShutdownApp(IFlexApp app);

		/// <summary>
		/// Executes the specified method asynchronously. The method will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// <param name="action">The action to execute</param>
		/// <param name="param1">The first parameter of the action.</param>
		void ExecuteAsync<T>(Action<T> action, T param1);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a new main window for the specified application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void OpenNewWindowForApp();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user chooses a language project and opens it. If the project is already
		/// open in a FieldWorks process, then the request is sent to the running FieldWorks
		/// process and a new window is opened for that project. Otherwise a new FieldWorks
		/// process is started to handle the project request.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ChooseLangProject();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user create a new language project and opens it. If the project is already
		/// open in a FieldWorks process, then the request is sent to the running FieldWorks
		/// process and a new window is opened for that project. Otherwise a new FieldWorks
		/// process is started to handle the new project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void CreateNewProject();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user delete any FW databases that are not currently open
		/// </summary>
		/// <param name="helpTopicProvider">The application's help provider.</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// ------------------------------------------------------------------------------------
		void DeleteProject(IHelpTopicProvider helpTopicProvider, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user backup any FW databases that are not currently open
		/// </summary>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// <returns>The path to the backup file, or <c>null</c> if the user cancels the
		/// backup</returns>
		/// ------------------------------------------------------------------------------------
		string BackupProject(Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore a project.
		/// </summary>
		/// <param name="helpTopicProvider">The FieldWorks application's help topic provider.</param>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// ------------------------------------------------------------------------------------
		void RestoreProject(IHelpTopicProvider helpTopicProvider, Form dialogOwner);

		/// <summary>
		/// Reopens the given FLEx project. This may be necessary if some external process modified the project data.
		/// Currently used when FLExBridge modifies our project during a Send/Receive
		/// </summary>
		/// <param name="project">The project name to re-open</param>
		/// <param name="app"></param>
		IFlexApp ReopenProject(string project, FwAppArgs app);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="app">The FieldWorks application.</param>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// ------------------------------------------------------------------------------------
		void FileProjectLocation(IApp app, Form dialogOwner);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rename the project used by this FieldWorks to the specified new name.
		/// </summary>
		/// <param name="newName">The new name</param>
		/// <returns>True if the rename was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		bool RenameProject(string newName);

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
