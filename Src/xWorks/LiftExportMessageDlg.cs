using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	public partial class LiftExportMessageDlg : Form
	{
		public LiftExportMessageDlg(string sMsg, MessageBoxButtons btns)
		{
			InitializeComponent();
			m_tbMessage.Text = sMsg;
			if (btns == MessageBoxButtons.YesNo)
			{
				m_btnOK.Text = xWorksStrings.ksYes;
				m_btnCancel.Text = xWorksStrings.ksNo;
			}
		}

		private void m_linkWeSay_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (System.Diagnostics.Process.Start("https://docs.google.com/document/d/1F6jBscOEOonPpx_z6R927fw79zMTOXzzxAYUmnbK9Gw"))
			{
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			Close();
		}
	}
}
