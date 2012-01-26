// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
			if (question.AdditionalInfo.Length >= 1)
			{
				Question q = question.AdditionalInfo[0] as Question;
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
			}
			m_pnlAlternatives.Hide();
		}

		private void m_txtModified_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = (m_txtModified.Text.Length > 0 && m_txtModified.Text != m_question.ModifiedPhrase);
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