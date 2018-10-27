// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Pass-through object for access to FieldWorks from an application. This ensures that
	/// there is only one FieldWorks object in each process.
	/// </summary>
	[Export(typeof(IFieldWorksManager))]
	internal class FieldWorksManager : IFieldWorksManager
	{
		#region IFieldWorksManager Members

		/// <inheritdoc />
		public LcmCache Cache => FieldWorks.Cache;

		/// <inheritdoc />
		public void ShutdownApp(IFlexApp app)
		{
			FieldWorks.ShutdownApp(app);
		}

		/// <inheritdoc />
		public void ExecuteAsync<T>(Action<T> action, T param1)
		{
			FieldWorks.ThreadHelper.InvokeAsync(action, param1);
		}

		/// <inheritdoc />
		public void OpenNewWindowForApp(IFwMainWnd currentWindow)
		{
			if (!FieldWorks.CreateAndInitNewMainWindow(currentWindow, false))
			{
				Debug.Fail("New main window was not created correctly!");
			}
		}

		/// <inheritdoc />
		public void ChooseLangProject()
		{
			var openedProject = FieldWorks.ChooseLangProject();
			if (openedProject != null && !FieldWorks.OpenExistingProject(openedProject))
			{
				Debug.Fail("Failed to open the project specified!");
			}
		}

		/// <inheritdoc />
		public void CreateNewProject()
		{
			var newProject = FieldWorks.CreateNewProject();
			if (newProject != null && !FieldWorks.OpenNewProject(newProject))
			{
				Debug.Fail("Failed to open the new project");
			}
		}

		/// <inheritdoc />
		public void DeleteProject(IHelpTopicProvider helpTopicProvider, Form dialogOwner)
		{
			FieldWorks.DeleteProject(dialogOwner, helpTopicProvider);
		}

		/// <inheritdoc />
		public string BackupProject(Form dialogOwner)
		{
			return FieldWorks.BackupProject(dialogOwner);
		}

		/// <inheritdoc />
		public void RestoreProject(IHelpTopicProvider helpTopicProvider, Form dialogOwner)
		{
			FieldWorks.RestoreProject(dialogOwner, helpTopicProvider);
		}

		/// <inheritdoc />
		public void FileProjectLocation(IApp app, Form dialogOwner)
		{
			FieldWorks.FileProjectLocation(dialogOwner, app, Cache);
		}

		/// <inheritdoc />
		public bool RenameProject(string newName)
		{
			return FieldWorks.RenameProject(newName);
		}

		/// <inheritdoc />
		public void HandleLinkRequest(FwAppArgs link)
		{
			FieldWorks.HandleLinkRequest(link);
		}

		/// <inheritdoc />
		public IFlexApp ReopenProject(string project, FwAppArgs app)
		{
			return FieldWorks.ReopenProject(project, app);
		}
		#endregion
	}
}