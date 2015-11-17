// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EditQuestion.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class EditQuestionDlg : Form
	{
		private readonly TranslatablePhrase m_question;

		internal string ModifiedPhrase
		{
			get { return m_txtModified.Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:EditQuestion"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditQuestionDlg(TranslatablePhrase question)
		{
			m_question = question;
			InitializeComponent();
			m_txtOriginal.Text = question.OriginalPhrase;
			m_txtModified.Text = question.PhraseInUse;
			Question q = question.QuestionInfo;
			if (q != null && q.AlternateForms != null)
			{
				System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditQuestionDlg));

				foreach (string alternateForm in q.AlternateForms.Skip(1))
				{
					RadioButton newBtn = new RadioButton();
					m_pnlAlternatives.Controls.Add(newBtn);
					resources.ApplyResources(newBtn, "m_rdoAlternative");
					m_pnlAlternatives.SetFlowBreak(newBtn, true);
					newBtn.Text = alternateForm;
					newBtn.CheckedChanged += m_rdoAlternative_CheckedChanged;
				}
				m_rdoAlternative.Text = q.AlternateForms.First();
				return;
			}
			m_pnlAlternatives.Hide();
		}

		private void m_txtModified_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = ((m_txtModified.Text.Length > 0 || m_question.IsUserAdded) && m_txtModified.Text != m_question.PhraseInUse);
			if (!m_pnlAlternatives.Visible)
				return;
			foreach (RadioButton rdoAlt in m_pnlAlternatives.Controls.OfType<RadioButton>())
				rdoAlt.Checked = (rdoAlt.Text == m_txtModified.Text);
		}

		private void m_rdoAlternative_CheckedChanged(object sender, EventArgs e)
		{
			m_txtModified.Text = ((RadioButton)sender).Text;
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			foreach (RadioButton rdoAlt in m_pnlAlternatives.Controls.OfType<RadioButton>())
				rdoAlt.Checked = false;
			m_txtModified.Text = m_txtOriginal.Text;
		}
	}
}