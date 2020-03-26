// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Warn the user when they choose a custom location for linked files that Send/Receive will not send these files.
	/// </summary>
	internal sealed partial class WarningNotUsingDefaultLinkedFilesLocation : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		internal WarningNotUsingDefaultLinkedFilesLocation(IHelpTopicProvider provider)
		{
			InitializeComponent();
			m_helpTopicProvider = provider;
		}

		private void btn_help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpLinkedFilesWarningDialog");
		}
	}
}
