using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconPluginFdoUI : IFdoUI
	{
		private readonly SynchronizeInvokeWrapper m_synchronizeInvoke;
		private readonly UserActivityMonitor m_activityMonitor;

		public ParatextLexiconPluginFdoUI(ActivationContextHelper activationContext)
		{
			m_synchronizeInvoke = new SynchronizeInvokeWrapper(activationContext);
			m_activityMonitor = new UserActivityMonitor();
			m_activityMonitor.StartMonitoring();
		}

		public bool ConflictingSave()
		{
			SynchronizeInvoke.Invoke(() => MessageBox.Show(Strings.ksConflictingSaveText,
				Strings.ksConflictingSaveCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning));
			return true;
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			return SynchronizeInvoke.Invoke(() => MessageBox.Show(Strings.ksConnectionLostText,
				Strings.ksConnectionLostCaption, MessageBoxButtons.YesNo) == DialogResult.Yes);
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

		public void Exit()
		{
			Application.Exit();
		}

		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_synchronizeInvoke; }
		}

		public DateTime LastActivityTime
		{
			get { return m_activityMonitor.LastActivityTime; }
		}

		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification="m_activationContext is a reference")]
		private class SynchronizeInvokeWrapper : ISynchronizeInvoke
		{
			private readonly ActivationContextHelper m_activationContext;

			public SynchronizeInvokeWrapper(ActivationContextHelper activationContext)
			{
				m_activationContext = activationContext;
			}

			private ISynchronizeInvoke SynchronizeInvoke
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

			public IAsyncResult BeginInvoke(Delegate method, object[] args)
			{
				return SynchronizeInvoke.BeginInvoke(new Func<object>(() =>
				{
					using (m_activationContext.Activate())
						return method.DynamicInvoke(args);
				}), null);
			}

			public object EndInvoke(IAsyncResult result)
			{
				return SynchronizeInvoke.EndInvoke(result);
			}

			public object Invoke(Delegate method, object[] args)
			{
				return SynchronizeInvoke.Invoke(new Func<object>(() =>
				{
					using (m_activationContext)
						return method.DynamicInvoke(args);
				}), null);
			}

			public bool InvokeRequired
			{
				get { return SynchronizeInvoke.InvokeRequired; }
			}
		}
	}
}
