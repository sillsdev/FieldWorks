// Copyright (c) 2010-2020 SIL International
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
	internal sealed class FieldWorksManager : IFieldWorksManager
	{
		#region IFieldWorksManager Members

		/// <inheritdoc />
		LcmCache IFieldWorksManager.Cache => FieldWorks.Cache;

		/// <inheritdoc />
		void IFieldWorksManager.ShutdownApp(IFlexApp app)
		{
			FieldWorks.ShutdownApp(app);
		}

		/// <inheritdoc />
		void IFieldWorksManager.ExecuteAsync<T>(Action<T> action, T param1)
		{
			FieldWorks.ThreadHelper.InvokeAsync(action, param1);
		}

		/// <inheritdoc />
		void IFieldWorksManager.OpenNewWindowForApp(IFwMainWnd currentWindow)
		{
			if (!FieldWorks.CreateAndInitNewMainWindow(currentWindow, false))
			{
				Debug.Fail("New main window was not created correctly!");
			}
		}

		/// <inheritdoc />
		void IFieldWorksManager.ChooseLangProject()
		{
			var openedProject = FieldWorks.ChooseLangProject();
			if (openedProject != null && !FieldWorks.OpenExistingProject(openedProject))
			{
				Debug.Fail("Failed to open the project specified!");
			}
		}

		/// <inheritdoc />
		void IFieldWorksManager.CreateNewProject()
		{
			var newProject = FieldWorks.CreateNewProject();
			if (newProject != null && !FieldWorks.OpenNewProject(newProject))
			{
				Debug.Fail("Failed to open the new project");
			}
		}

		/// <inheritdoc />
		void IFieldWorksManager.DeleteProject(IHelpTopicProvider helpTopicProvider, Form dialogOwner)
		{
			FieldWorks.DeleteProject(dialogOwner, helpTopicProvider);
		}

		/// <inheritdoc />
		string IFieldWorksManager.BackupProject(Form dialogOwner)
		{
			return FieldWorks.BackupProject(dialogOwner);
		}

		/// <inheritdoc />
		void IFieldWorksManager.RestoreProject(IHelpTopicProvider helpTopicProvider, Form dialogOwner)
		{
			FieldWorks.RestoreProject(dialogOwner, helpTopicProvider);
		}

		/// <inheritdoc />
		void IFieldWorksManager.FileProjectLocation(IApp app, Form dialogOwner)
		{
			FieldWorks.FileProjectLocation(dialogOwner, app, FieldWorks.Cache);
		}

		/// <inheritdoc />
		bool IFieldWorksManager.RenameProject(string newName)
		{
			return FieldWorks.RenameProject(newName);
		}

		/// <inheritdoc />
		void IFieldWorksManager.HandleLinkRequest(FwAppArgs link)
		{
			FieldWorks.HandleLinkRequest(link);
		}

		/// <inheritdoc />
		IFlexApp IFieldWorksManager.ReopenProject(string project, FwAppArgs app)
		{
			return FieldWorks.ReopenProject(project, app);
		}
		#endregion
	}
}