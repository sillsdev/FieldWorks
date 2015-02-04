using System;
using System.Windows.Forms;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public partial class ChooseTextWritingSystemDlg : Form
	{
		private HelpProvider helpProvider;
		private const string s_helpTopic = "khtpBaselineTextWs";

		private int m_ws;
		private IHelpTopicProvider m_helpTopicProvider;

		public ChooseTextWritingSystemDlg()
		{
			InitializeComponent();

			helpProvider = new HelpProvider();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate the combo box and default to the ws from the current text.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="wsCurrent">The ws current.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(FdoCache cache, IHelpTopicProvider helpTopicProvider, int wsCurrent)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_ws = wsCurrent;
			int iSel = 0;
			foreach (var ws in cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				m_cbWritingSystems.Items.Add(ws);
				if (ws.Handle == wsCurrent)
					iSel = m_cbWritingSystems.Items.Count - 1;
			}
			m_cbWritingSystems.SelectedIndex = iSel;

			helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		private void m_cbWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_ws = ((WritingSystem) m_cbWritingSystems.SelectedItem).Handle;
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			m_ws = ((WritingSystem) m_cbWritingSystems.SelectedItem).Handle;
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		public int TextWs
		{
			get { return m_ws; }
		}
	}
}
