using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public partial class FLExBridgeFirstSendReceiveInstructionsDlg : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		public FLExBridgeFirstSendReceiveInstructionsDlg(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;

			InitializeComponent();
		}

		private void HelpBtn_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpGetStartedWithSendReceive");
		}
	}
}
