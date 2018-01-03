// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using System.Drawing;

namespace LanguageExplorer.Works
{
	public partial class DictionaryConfigurationImportDlg : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		internal string HelpTopic { get; set; }

		public DictionaryConfigurationImportDlg(IHelpTopicProvider helpProvider)
		{
			InitializeComponent();
			m_helpTopicProvider = helpProvider;
			// Clear away example text
			explanationLabel.Text = string.Empty;

			if (MiscUtils.IsUnix)
			{
				var optimalWidthOnMono = 582;
				MinimumSize = new Size(optimalWidthOnMono, MinimumSize.Height);
			}
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}
	}
}
