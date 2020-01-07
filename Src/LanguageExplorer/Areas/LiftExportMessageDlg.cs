// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.Areas
{
	public partial class LiftExportMessageDlg : Form
	{
		public LiftExportMessageDlg(string sMsg, MessageBoxButtons btns)
		{
			InitializeComponent();
			m_tbMessage.Text = sMsg;
			if (btns == MessageBoxButtons.YesNo)
			{
				m_btnOK.Text = LanguageExplorerResources.ksYes;
				m_btnCancel.Text = LanguageExplorerResources.ksNo;
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
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}