// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconPluginLcmUI : ILcmUI
	{
		private readonly UserActivityMonitor m_activityMonitor;

		public ParatextLexiconPluginLcmUI()
		{
			m_activityMonitor = new UserActivityMonitor();
			m_activityMonitor.StartMonitoring();
		}

		public bool ConflictingSave()
		{
			SynchronizeInvoke.Invoke(() => MessageBox.Show(Strings.ksConflictingSaveText,
				Strings.ksConflictingSaveCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning));
			return true;
		}

		public FileSelection ChooseFilesToUse()
		{
			using (var dlg = new FilesToRestoreAreOlder())
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (dlg.fKeepFilesThatAreNewer)
					{
						return FileSelection.OkKeepNewer;
					}
					if (dlg.fOverWriteThatAreNewer)
					{
						return FileSelection.OkUseOlder;
					}
				}
				return FileSelection.Cancel;
			}
		}

		public bool RestoreLinkedFilesInProjectFolder()
		{
			return SynchronizeInvoke.Invoke(() => MessageBox.Show(
				Strings.ksRestoreLinkedFilesInProjectFolderText, Strings.ksRestoreLinkedFilesInProjectFolderCaption,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
		}

		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			DialogResult result = SynchronizeInvoke.Invoke(() => MessageBox.Show(
				Strings.ksCannotRestoreLinkedFilesToOriginalLocationText, Strings.ksCannotRestoreLinkedFilesToOriginalLocationCaption,
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question));
			switch (result)
			{
				case DialogResult.Yes:
					return YesNoCancel.OkYes;
				case DialogResult.No:
					return YesNoCancel.OkNo;
			}
			return YesNoCancel.Cancel;
		}

		public void DisplayMessage(MessageType type, string message, string caption, string helpTopic)
		{
			var icon = MessageBoxIcon.Information;
			switch (type)
			{
				case MessageType.Error:
					icon = MessageBoxIcon.Error;
					break;
				case MessageType.Info:
					icon = MessageBoxIcon.Information;
					break;
				case MessageType.Warning:
					icon = MessageBoxIcon.Warning;
					break;
			}
			SynchronizeInvoke.Invoke(() => MessageBox.Show(message, caption, MessageBoxButtons.OK, icon));
		}

		public void DisplayCircularRefBreakerReport(string report, string caption)
		{
			var icon = MessageBoxIcon.Information;
			SynchronizeInvoke.Invoke(() => MessageBox.Show(report, caption, MessageBoxButtons.OK, icon));
		}

		public void ReportException(Exception error, bool isLethal)
		{
			// do nothing
		}

		public void ReportDuplicateGuids(string errorText)
		{
			// do nothing
		}

		public bool Retry(string msg, string caption)
		{
			return SynchronizeInvoke.Invoke(() => MessageBox.Show(msg, caption,
				MessageBoxButtons.RetryCancel, MessageBoxIcon.None) == DialogResult.Retry);
		}

		public bool OfferToRestore(string projectPath, string backupPath)
		{
			return SynchronizeInvoke.Invoke(() => MessageBox.Show(
				String.Format(Strings.ksOfferToRestoreText, projectPath, File.GetLastWriteTime(projectPath),
					backupPath, File.GetLastWriteTime(backupPath)),
				Strings.ksOfferToRestoreCaption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Error) == DialogResult.Yes);
		}

		public ISynchronizeInvoke SynchronizeInvoke
		{
			get
			{
				Form form = Form.ActiveForm;
				if (form != null)
					return form;
				if (Application.OpenForms.Count > 0)
					return Application.OpenForms[0];
				return null;
			}
		}

		public DateTime LastActivityTime
		{
			get { return m_activityMonitor.LastActivityTime; }

		}
	}
}
