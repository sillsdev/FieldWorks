using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.LexText
{
	public partial class RestoreDefaultsDlg : Form
	{
		private const string s_helpTopic = "khtpRestoreDefaults";
		private IFlexApp m_app;

		public RestoreDefaultsDlg()
		{
			InitializeComponent();
		}

		public RestoreDefaultsDlg(IFlexApp app)
			: this()
		{
			m_app = app;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_app, s_helpTopic);
		}

		private void m_btnYes_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Yes;
			Close();
		}
	}
}
