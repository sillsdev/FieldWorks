using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
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
			System.Diagnostics.Process.Start("http://www.wesay.org/wiki/ShareWithFLEx");
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			Close();
		}
	}
}
