// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)


using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public partial class DictionaryConfigurationImportDlg : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;
		public DictionaryConfigurationImportDlg(IHelpTopicProvider helpProvider)
		{
			InitializeComponent();
			m_helpTopicProvider = helpProvider;
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDictConfigManager");
		}
	}
}
