using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconPluginFdoUI : IFdoUI
	{
		// Singleton
		private static readonly ParatextLexiconPluginFdoUI s_instance = new ParatextLexiconPluginFdoUI();
		private ParatextLexiconPluginFdoUI(){}
		public static ParatextLexiconPluginFdoUI Instance
		{
			get { return s_instance; }
		}

		public bool ConflictingSave()
		{
			SynchronizeInvoke.Invoke(new Action(() => MessageBox.Show(Strings.ksConflictingSaveText,
				Strings.ksConflictingSaveCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning)), null);
			return true;
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			return (bool) SynchronizeInvoke.Invoke(new Func<bool>(() => MessageBox.Show(Strings.ksConnectionLostText,
				Strings.ksConnectionLostCaption, MessageBoxButtons.YesNo) == DialogResult.Yes), null);
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
			return (bool) SynchronizeInvoke.Invoke(new Func<bool>(() => MessageBox.Show(
				Strings.ksRestoreLinkedFilesInProjectFolderText, Strings.ksRestoreLinkedFilesInProjectFolderCaption,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes), null);
		}

		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			var result = (DialogResult) SynchronizeInvoke.Invoke(new Func<DialogResult>(() => MessageBox.Show(
				Strings.ksCannotRestoreLinkedFilesToOriginalLocationText, Strings.ksCannotRestoreLinkedFilesToOriginalLocationCaption,
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)), null);
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
			SynchronizeInvoke.Invoke(new Action(() => MessageBox.Show(message, caption, MessageBoxButtons.OK, icon)), null);
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
			return (bool) SynchronizeInvoke.Invoke(new Func<bool>(() => MessageBox.Show(msg, caption,
				MessageBoxButtons.RetryCancel, MessageBoxIcon.None) == DialogResult.Retry), null);
		}

		public bool OfferToRestore(string projectPath, string backupPath)
		{
			return (bool) SynchronizeInvoke.Invoke(new Func<bool>(() => MessageBox.Show(
				String.Format(Strings.ksOfferToRestoreText, projectPath, File.GetLastWriteTime(projectPath),
					backupPath, File.GetLastWriteTime(backupPath)),
				Strings.ksOfferToRestoreCaption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Error) == DialogResult.Yes), null);
		}

		public void Exit()
		{
			Application.Exit();
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
			get
			{
				// this effectively disables saving on idle
				return DateTime.Now;
			}
		}
	}
}
