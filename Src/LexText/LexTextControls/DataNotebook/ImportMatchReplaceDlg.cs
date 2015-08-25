// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportMatchReplace.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ImportMatchReplaceDlg : Form
	{
		IHelpTopicProvider m_helpTopicProvider;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ImportMatchReplace"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
			SIL.FieldWorks.Common.FwUtils.ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportMatchReplace");
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
