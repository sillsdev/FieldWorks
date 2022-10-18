// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/DictionaryConfigurationImportDlg.cs
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
||||||| f013144d5:Src/xWorks/DictionaryConfigurationImportDlg.cs
using XCore;
using SIL.LCModel.Utils;
using System.Drawing;
=======
using SIL.PlatformUtilities;
using XCore;
>>>>>>> develop:Src/xWorks/DictionaryConfigurationImportDlg.cs

namespace LanguageExplorer.DictionaryConfiguration
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
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/DictionaryConfigurationImportDlg.cs
			if (Platform.IsUnix)
||||||| f013144d5:Src/xWorks/DictionaryConfigurationImportDlg.cs

			if (MiscUtils.IsUnix)
=======

			if (Platform.IsUnix)
>>>>>>> develop:Src/xWorks/DictionaryConfigurationImportDlg.cs
			{
				var optimalWidthOnMono = 582;
				MinimumSize = new Size(optimalWidthOnMono, MinimumSize.Height);
			}
		}

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}
	}
}