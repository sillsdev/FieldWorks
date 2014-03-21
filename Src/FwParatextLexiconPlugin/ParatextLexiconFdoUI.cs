using System;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconFdoUI : IFdoUI
	{
		// Singleton
		private static readonly ParatextLexiconFdoUI s_instance = new ParatextLexiconFdoUI();
		private ParatextLexiconFdoUI(){}
		public static ParatextLexiconFdoUI Instance
		{
			get { return s_instance; }
		}

		public bool ConflictingSave()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			return (bool)SynchronizeInvoke.Invoke(new Func<bool>(() => MessageBox.Show(
				"There is a problem with the connection to the lexicon.  Usually, this occurs when there is a problem with the FwRemoteDatabaseConnectorService.  Would you like to attempt to reestablish the connection?",
				"Lexicon Connection Lost", MessageBoxButtons.YesNo) == DialogResult.Yes), null);
		}

		public FileSelection ChooseFilesToUse()
		{
			throw new NotImplementedException();
		}

		public bool RestoreLinkedFilesInProjectFolder()
		{
			throw new NotImplementedException();
		}

		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			throw new NotImplementedException();
		}

		public void DisplayMessage(MessageType type, string message, string caption, string helpTopic)
		{
			throw new NotImplementedException();
		}

		public void ReportException(Exception error, bool isLethal)
		{
			throw new NotImplementedException();
		}

		public void ReportDuplicateGuids(string errorText)
		{
			throw new NotImplementedException();
		}

		public bool Retry(string msg, string caption)
		{
			throw new NotImplementedException();
		}

		public bool OfferToRestore(string projectPath, string backupPath)
		{
			throw new NotImplementedException();
		}

		public void Exit()
		{
			System.Windows.Forms.Application.Exit();
		}

		public ISynchronizeInvoke SynchronizeInvoke
		{
			get
			{
				Form form = Form.ActiveForm;
				if (form != null)
					return form;
				if (System.Windows.Forms.Application.OpenForms.Count > 0)
					return System.Windows.Forms.Application.OpenForms[0];
				return null;
			}
		}
		public DateTime LastActivityTime { get; private set; }
	}
}
