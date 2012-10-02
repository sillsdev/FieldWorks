using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.IText
{
	public partial class ChooseTextWritingSystemDlg : Form
	{
		private System.Windows.Forms.HelpProvider helpProvider;
		private const string s_helpTopic = "khtpBaselineTextWs";

		private int m_ws;

		public ChooseTextWritingSystemDlg()
		{
			InitializeComponent();

			helpProvider = new System.Windows.Forms.HelpProvider();
			helpProvider.HelpNamespace = FwApp.App.HelpFile;
			helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
			helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
		}

		/// <summary>
		/// Populate the combo box and default to the ws from the current text.
		/// </summary>
		/// <param name="cache"></param>
		public void Initialize(FdoCache cache, int wsCurrent)
		{
			m_ws = wsCurrent;
			int iSel = 0;
			foreach (ILgWritingSystem lws in cache.LangProject.VernWssRC)
			{
				m_cbWritingSystems.Items.Add(lws);
				if (lws.Hvo == wsCurrent)
					iSel = m_cbWritingSystems.Items.Count - 1;
			}
			m_cbWritingSystems.SelectedIndex = iSel;
		}

		private void m_cbWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_ws = (m_cbWritingSystems.SelectedItem as ILgWritingSystem).Hvo;
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
			m_ws = (m_cbWritingSystems.SelectedItem as ILgWritingSystem).Hvo;
			this.Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}

		public int TextWs
		{
			get { return m_ws; }
		}
	}
}
