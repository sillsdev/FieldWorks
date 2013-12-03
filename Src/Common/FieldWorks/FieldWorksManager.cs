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
// File: FieldWorksManager.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Pass-through object for access to FieldWorks from an application. This ensures that
	/// there is only one FieldWorks object in each process.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FieldWorksManager : IFieldWorksManager
	{
		#region IFieldWorksManager Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return FieldWorks.Cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shut down the specified application, which will be disposed of immediately.
		/// If no other applications are running, then FieldWorks will also be shut down.
		/// </summary>
		/// <param name="app">The application to shut down.</param>
		/// ------------------------------------------------------------------------------------
		public void ShutdownApp(FwApp app)
		{
			FieldWorks.ShutdownApp(app, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the specified method asynchronously. The method will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// <param name="action">The action to execute</param>
		/// <param name="param1">The first parameter of the action.</param>
		/// ------------------------------------------------------------------------------------
		public void ExecuteAsync<T>(Action<T> action, T param1)
		{
			FieldWorks.ThreadHelper.InvokeAsync(action, param1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a new main window for the specified application.
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="wndToCopyFrom">The window to copy information from (optional).</param>
		/// ------------------------------------------------------------------------------------
		public void OpenNewWindowForApp(FwApp app, Form wndToCopyFrom)
		{
			if (!FieldWorks.CreateAndInitNewMainWindow(app, false, wndToCopyFrom, false))
			{
				Debug.Fail("New main window was not created correctly!");
			}
		}

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
		public void ChooseLangProject(FwApp app, Form dialogOwner)
		{
			Debug.Assert(dialogOwner is IFwMainWnd, "OpenExistingProject cannot use this window for copying");

			ProjectId openedProject = FieldWorks.ChooseLangProject(dialogOwner, app);
			if (openedProject != null && !FieldWorks.OpenExistingProject(openedProject, app, dialogOwner))
			{
				Debug.Fail("Failed to open the project specified!");
			}
		}

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
		public void CreateNewProject(FwApp app, Form dialogOwner)
		{
			ProjectId newProject = FieldWorks.CreateNewProject(dialogOwner, app, app);
			if (newProject != null && !FieldWorks.OpenNewProject(newProject, app.ApplicationName))
			{
				Debug.Fail("Failed to open the new project");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user delete any FW databases that are not currently open
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// ------------------------------------------------------------------------------------
		public void DeleteProject(FwApp app, Form dialogOwner)
		{
			FieldWorks.DeleteProject(dialogOwner, app);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user backup any FW databases that are not currently open
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// <returns>The path to the backup file, or <c>null</c> if the user cancels the
		/// backup</returns>
		/// ------------------------------------------------------------------------------------
		public string BackupProject(FwApp app, Form dialogOwner)
		{
			return FieldWorks.BackupProject(dialogOwner, app);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore a project.
		/// </summary>
		/// <param name="fwApp">The FieldWorks application.</param>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// ------------------------------------------------------------------------------------
		public void RestoreProject(FwApp fwApp, Form dialogOwner)
		{
			FieldWorks.RestoreProject(dialogOwner, fwApp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Archive selected project files using RAMP
		/// </summary>
		/// <param name="fwApp">The FieldWorks application</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// <returns>The list of the files to archive, or <c>null</c> if the user cancels the
		/// archive dialog</returns>
		/// ------------------------------------------------------------------------------------
		public List<string> ArchiveProjectWithRamp(FwApp fwApp, Form dialogOwner)
		{
			return FieldWorks.ArchiveProjectWithRamp(dialogOwner, fwApp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore a project.
		/// </summary>
		/// <param name="fwApp">The FieldWorks application.</param>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// ------------------------------------------------------------------------------------
		public void FileProjectSharingLocation(FwApp fwApp, Form dialogOwner)
		{
			FieldWorks.FileProjectSharingLocation(dialogOwner, fwApp);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rename the project used by this FieldWorks to the specified new name.
		/// </summary>
		/// <returns>True if the rename was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool RenameProject(string newName, FwApp app)
		{
			return FieldWorks.RenameProject(newName, app);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a link request. This is expected to handle determining the correct
		/// application to start up on the correct project and passing the link to any newly
		/// started application.
		/// </summary>
		/// <param name="link">The link.</param>
		/// ------------------------------------------------------------------------------------
		public void HandleLinkRequest(FwAppArgs link)
		{
			FieldWorks.HandleLinkRequest(link);
		}

		/// <summary>
		/// Reopens the given FLEx project. This may be necessary if some external process modified the project data.
		/// Currently used when FLExBridge modifies our project during a Send/Receive
		/// </summary>
		/// <param name="project">The project name to re-open</param>
		/// <param name="app"></param>
		public FwApp ReopenProject(string project, FwAppArgs app)
		{
			return FieldWorks.ReopenProject(project, app);
		}
		#endregion
	}
}
