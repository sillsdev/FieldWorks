using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Warn the user when they choose a custom location for linked files that Send/Receive will not send these files.
	/// </summary>
	public partial class WarningNotUsingDefaultLinkedFilesLocation : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;
		/// <summary>
		/// Warn the user when they choose a custom location for linked files that Send/Receive will not send these files.
		/// </summary>
		public WarningNotUsingDefaultLinkedFilesLocation(IHelpTopicProvider provider)
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
