// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public partial class ChooseTextWritingSystemDlg : Form
	{
		private HelpProvider helpProvider;
		private const string s_helpTopic = "khtpBaselineTextWs";
		private IHelpTopicProvider m_helpTopicProvider;

		public ChooseTextWritingSystemDlg()
		{
			InitializeComponent();

			helpProvider = new HelpProvider();
		}

		/// <summary>
		/// Populate the combo box and default to the ws from the current text.
		/// </summary>
		public void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, int wsCurrent)
		{
			m_helpTopicProvider = helpTopicProvider;
			TextWs = wsCurrent;
			var iSel = 0;
			foreach (var ws in cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				m_cbWritingSystems.Items.Add(ws);
				if (ws.Handle == wsCurrent)
				{
					iSel = m_cbWritingSystems.Items.Count - 1;
				}
			}
			m_cbWritingSystems.SelectedIndex = iSel;
			helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		private void m_cbWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			TextWs = ((CoreWritingSystemDefinition)m_cbWritingSystems.SelectedItem).Handle;
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			TextWs = ((CoreWritingSystemDefinition)m_cbWritingSystems.SelectedItem).Handle;
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		public int TextWs { get; private set; }
	}
}