// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks
{
	/// <summary />
	public partial class MoveProjectsDlg : Form
	{
		IHelpTopicProvider m_helpTopicProvider;
		private HelpProvider m_helpProvider;
		private string m_helpTopic = "khtpMoveProjectDlgNewLocation";

		/// <summary />
		public MoveProjectsDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			m_helpTopicProvider = helpTopicProvider;
			InitHelp();
		}

		private void m_btnYes_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Yes;
			Close();
		}

		private void m_btnNo_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.No;
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		private void InitHelp()
		{
			// Only enable the Help button if we have a help topic for the fieldName
			if (m_helpTopicProvider != null)
			{
				string keyword;
				m_btnHelp.Enabled = HelpTopicIsValid(m_helpTopic, out keyword);
				if (m_btnHelp.Enabled)
				{
					if (m_helpProvider == null)
					{
						m_helpProvider = new HelpProvider();
					}
					m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
					m_helpProvider.SetHelpKeyword(this, keyword);
					m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
				}
			}
		}

		private bool HelpTopicIsValid(string helpTopic, out string keyword)
		{
			keyword = m_helpTopicProvider.GetHelpString(helpTopic);
			return keyword != null;
		}
	}
}