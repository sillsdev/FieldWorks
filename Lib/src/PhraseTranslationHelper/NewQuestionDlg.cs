// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NewQuestionDlg.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class NewQuestionDlg : Form
	{
		private readonly string m_scrReference;
		internal Question NewQuestion
		{
			get
			{
				return new Question(m_scrReference, chkNoEnglish.Checked ? null : m_txtEnglish.Text,
					m_txtAnswer.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NewQuestionDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public NewQuestionDlg(string scrReference)
		{
			m_scrReference = scrReference;
			InitializeComponent();

			lblReference.Text = String.Format(lblReference.Text, scrReference);
		}

		private void m_txtEnglish_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = (m_txtEnglish.Text.Length > 0 || chkNoEnglish.Checked);
		}

		private void chkNoEnglish_CheckedChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = (m_txtEnglish.Text.Length > 0 || chkNoEnglish.Checked);
			m_txtEnglish.Enabled = !chkNoEnglish.Checked;
		}
	}
}