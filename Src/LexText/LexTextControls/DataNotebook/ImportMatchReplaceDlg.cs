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
// File: ImportMatchReplace.cs
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
		XCore.IHelpTopicProvider m_helpTopicProvider;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ImportMatchReplace"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMatchReplaceDlg()
		{
			InitializeComponent();
		}

		public void Initialize(XCore.IHelpTopicProvider helpTopicProvider, string sMatch, string sReplace)
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
