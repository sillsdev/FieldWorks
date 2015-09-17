// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public partial class FLExBridgeFirstSendReceiveInstructionsDlg : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		public FLExBridgeFirstSendReceiveInstructionsDlg(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;

			InitializeComponent();

			// Strip mailto: links until a proper solution can be implemented for LT-16594.
			if (!MiscUtils.IsUnix)
				return;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FLExBridgeFirstSendReceiveInstructionsDlg));
			var documentText = resources.GetString("htmlControl_Instructions.DocumentText");
			var documentTextWithNoMailtoLinks = Regex.Replace(documentText, "<a href='mailto:.*'>(.*)</a>", "$1");
			this.htmlControl_Instructions.DocumentText = documentTextWithNoMailtoLinks;
		}

		private void HelpBtn_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpGetStartedWithSendReceive");
		}
	}
}
