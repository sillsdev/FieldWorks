// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
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

			var instructionsHtml = WebUtility.HtmlDecode(LexEdStrings.SendReceiveForTheFirstTimeContent);
			// Strip mailto: links until a proper solution can be implemented for LT-16594.
			if (MiscUtils.IsUnix && instructionsHtml != null)
			{
				instructionsHtml = Regex.Replace(instructionsHtml, "<a href='mailto:.*'>(.*)</a>", "$1");
			}
			htmlControl_Instructions.DocumentText = instructionsHtml;
		}

		private void HelpBtn_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpGetStartedWithSendReceive");
		}
	}
}
