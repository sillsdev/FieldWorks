using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FdoUi.Dialogs
{
	/// <summary>
	///
	/// </summary>
	public partial class FilesToRestoreAreOlder : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		///
		/// </summary>
		public FilesToRestoreAreOlder(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();
		}

		/// <summary>
		///
		/// </summary>
		public bool fKeepFilesThatAreNewer
		{
			get { return radio_Keep.Checked; }
		}

		/// <summary>
		///
		/// </summary>
		public bool fOverWriteThatAreNewer
		{
			get { return radio_Overwrite.Checked; }
		}

		private void button_OK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			Close();
		}

		private void button_Cancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			Close();
		}

		private void button_Help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtp-LinkedFilesInBackupAreOlder");//"khtp-LinkedFilesFolder")
		}
	}
}
