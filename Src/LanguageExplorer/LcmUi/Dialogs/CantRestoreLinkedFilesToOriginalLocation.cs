// Copyright (c) 2010-2020 SIL International
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
	public partial class CantRestoreLinkedFilesToOriginalLocation : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		public CantRestoreLinkedFilesToOriginalLocation(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();
		}

		/// <summary />
		public bool RestoreLinkedFilesToProjectFolder => radio_Thanks.Checked;

		/// <summary />
		public bool DoNotRestoreLinkedFiles => radio_NoThanks.Checked;

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
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtp-LinkedFilesFolder");
		}
	}
}