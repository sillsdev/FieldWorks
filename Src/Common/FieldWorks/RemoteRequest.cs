// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RemoteRequest.cs
// Responsibility: FW Team
using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Handle requests from other instances of TE
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class RemoteRequest : MarshalByRefObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see whether this instance is connected to the requested project. If so,
		/// starts up the requested app or activates a main window for that app if already
		/// running.
		/// </summary>
		/// <param name="projectId">The requested project ID.</param>
		/// <param name="args">The application arguments</param>
		/// <returns>The result of checking to see if the specified project matches the
		/// project this instance is running</returns>
		/// ------------------------------------------------------------------------------------
		public ProjectMatch HandleOpenProjectRequest(ProjectId projectId, FwAppArgs args)
		{
			ProjectMatch isMyProject = FieldWorks.GetProjectMatchStatus(projectId);
			if (isMyProject != ProjectMatch.ItsMyProject)
				return isMyProject;

			FieldWorks.KickOffAppFromOtherProcess(args);
			return ProjectMatch.ItsMyProject; // The request has been handled at this point
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see whether this instance is connected to the requested project. If so,
		/// attempts to do a restore using the specified restore settings.
		/// </summary>
		/// <param name="restoreSettings">The restore settings.</param>
		/// <returns>True if the project belonged to this instance and the restore was
		/// successful, false otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public bool HandleRestoreProjectRequest(FwRestoreProjectSettings restoreSettings)
		{
			ProjectMatch isMyProject = FieldWorks.GetProjectMatchStatus(
				new ProjectId(restoreSettings.Settings.FullProjectPath, null));
			if (isMyProject != ProjectMatch.ItsMyProject)
				return false;

			FieldWorks.HandleRestoreRequest(restoreSettings);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the specified link request.
		/// </summary>
		/// <param name="link">The link.</param>
		/// <returns>True if the link was successfully handled, false otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public bool HandleLinkRequest(FwAppArgs link)
		{
			ProjectMatch isMyProject = FieldWorks.GetProjectMatchStatus(
				new ProjectId(link.DatabaseType, link.Database));
			if (isMyProject != ProjectMatch.ItsMyProject)
				return false;

			FieldWorks.FollowLink(link);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Brings the current main form (probably a progress dialog) to the front so the
		/// user can see it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void BringMainFormToFront()
		{
			if (Form.ActiveForm == null && Application.OpenForms.Count > 0)
			{
				try
				{
					Application.OpenForms[0].BringToFront();
				}
				catch
				{
					// It's possible the form may have been closed or something...
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle an external request to close all main windows.
		/// </summary>
		/// <returns>false because we'll want every process to do this</returns>
		/// ------------------------------------------------------------------------------------
		public bool CloseAllMainWindows()
		{
			FieldWorks.CloseAllMainWindows();
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used by clients to check if service is alive.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsAlive()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether FW is in "single process mode".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InSingleProcessMode()
		{
			return FieldWorks.InSingleProcessMode;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the project name of this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectName
		{
			get { return FieldWorks.Cache.ProjectId.UiName; }
		}
	}
}
