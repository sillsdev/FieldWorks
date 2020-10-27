// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	internal sealed partial class CantRestoreLinkedFilesToOriginalLocation : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		internal CantRestoreLinkedFilesToOriginalLocation(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();
		}

		/// <summary />
		internal bool RestoreLinkedFilesToProjectFolder => radio_Thanks.Checked;

		/// <summary />
		internal bool DoNotRestoreLinkedFiles => radio_NoThanks.Checked;

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