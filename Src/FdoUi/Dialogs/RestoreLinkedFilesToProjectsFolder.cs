using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.FdoUi.Dialogs
{
	/// <summary>
	///
	/// </summary>
	public partial class RestoreLinkedFilesToProjectsFolder : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		///
		/// </summary>
		public RestoreLinkedFilesToProjectsFolder(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();
		}

		/// <summary>
		///
		/// </summary>
		public bool fRestoreLinkedFilesToProjectFolder
		{
			get { return radio_Yes.Checked; }
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
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtp-LinkedFilesFolder");
		}
	}
}
