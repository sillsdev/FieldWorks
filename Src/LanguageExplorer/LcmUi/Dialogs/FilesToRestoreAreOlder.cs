// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.LcmUi.Dialogs
{
	/// <summary>
	///
	/// </summary>
	public partial class FilesToRestoreAreOlder : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		public FilesToRestoreAreOlder(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();
		}

		/// <summary />
		public bool KeepFilesThatAreNewer => radio_Keep.Checked;

		/// <summary />
		public bool OverWriteThatAreNewer => radio_Overwrite.Checked;

		private void button_OK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void button_Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void button_Help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtp-LinkedFilesInBackupAreOlder");//"khtp-LinkedFilesFolder")
		}
	}
}