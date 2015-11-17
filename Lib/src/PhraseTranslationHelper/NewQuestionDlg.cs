// ---------------------------------------------------------------------------------------------
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
		private readonly Question m_baseQuestion;
		internal Question NewQuestion
		{
			get
			{
				return new Question(m_baseQuestion, chkNoEnglish.Checked ? null : m_txtEnglish.Text,
					m_txtAnswer.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NewQuestionDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public NewQuestionDlg(Question baseQuestion)
		{
			m_baseQuestion = baseQuestion;
			InitializeComponent();

			lblReference.Text = String.Format(lblReference.Text, m_baseQuestion.ScriptureReference);
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