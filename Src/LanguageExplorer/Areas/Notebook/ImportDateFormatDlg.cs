// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary />
	public partial class ImportDateFormatDlg : Form
	{
		private IHelpTopicProvider m_helpTopicProvider;

		// This example DateTime value must match that found in DateFieldOptions.cs!
		private DateTime m_dtExample = new DateTime(1999, 3, 29, 15, 30, 45);

		/// <summary />
		public ImportDateFormatDlg()
		{
			InitializeComponent();
		}

		public void Initialize(string sFormat, IHelpTopicProvider helpTopicProvider, bool fGenDate)
		{
			m_tbFormat.Text = sFormat;
			m_helpTopicProvider = helpTopicProvider;
			ApplyFormat();
		}

		private void m_btnApply_Click(object sender, EventArgs e)
		{
			ApplyFormat();
		}

		private void ApplyFormat()
		{
			try
			{
				m_tbExample.Text = string.IsNullOrEmpty(m_tbFormat.Text) ? string.Empty : m_dtExample.ToString(m_tbFormat.Text);
			}
			catch
			{
				m_tbExample.Text = LanguageExplorerControls.ksERROR;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportDateFormatDlg");
		}

		public string Format => m_tbFormat.Text;
	}
}