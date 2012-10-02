// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportDateFormatDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ImportDateFormatDlg : Form
	{
		private XCore.IHelpTopicProvider m_helpTopicProvider;
		bool m_fGenDate;
		// This example DateTime value must match that found in DateFieldOptions.cs!
		DateTime m_dtExample = new DateTime(1999, 3, 29, 15, 30, 45);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ImportDateFormatDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportDateFormatDlg()
		{
			InitializeComponent();
		}

		public void Initialize(string sFormat, XCore.IHelpTopicProvider helpTopicProvider, bool fGenDate)
		{
			m_tbFormat.Text = sFormat;
			m_helpTopicProvider = helpTopicProvider;
			m_fGenDate = fGenDate;
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
				if (String.IsNullOrEmpty(m_tbFormat.Text))
					m_tbExample.Text = String.Empty;
				else
					m_tbExample.Text = m_dtExample.ToString(m_tbFormat.Text);
			}
			catch
			{
				m_tbExample.Text = LexTextControls.ksERROR;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			SIL.FieldWorks.Common.FwUtils.ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportDateFormatDlg");
		}

		public string Format
		{
			get { return m_tbFormat.Text; }
		}
	}
}