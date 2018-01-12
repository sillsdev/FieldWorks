// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary />
	public partial class ImportMatchReplaceDlg : Form
	{
		IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ImportMatchReplace"/> class.
		/// </summary>
		public ImportMatchReplaceDlg()
		{
			InitializeComponent();
		}

		public void Initialize(IHelpTopicProvider helpTopicProvider, string sMatch, string sReplace)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_tbMatch.Text = sMatch == null ? String.Empty : sMatch;
			m_tbReplace.Text = sReplace == null ? String.Empty : sReplace;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportMatchReplace");
		}

		public string Match
		{
			get { return m_tbMatch.Text; }
		}

		public string Replace
		{
			get { return m_tbReplace.Text; }
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}
	}
}
