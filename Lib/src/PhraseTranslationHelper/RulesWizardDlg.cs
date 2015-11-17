// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RulesWizardDlg.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class RulesWizardDlg : Form
	{
		private RenderingSelectionRule m_rule;
		private readonly Action<bool> m_selectKeyboard;

		private Func<string, bool> ValidateName { get; set; }

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RulesWizardDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RulesWizardDlg(RenderingSelectionRule rule, IEnumerable<string> allWordsInQuestions,
			Action<bool> selectKeyboard, Func<string, bool> nameValidator)
		{
			InitializeComponent();

			m_rdoSuffix.Tag = m_pnlSuffixDetails;
			m_rdoPrefix.Tag = m_pnlPrefixDetails;
			m_rdoPreceedingWord.Tag = m_pnlPrecedingWordDetails;
			m_rdoFollowingWord.Tag = m_pnlFollowingWordDetails;
			m_rdoUserDefinedQuestionCriteria.Tag = m_pnlUserDefinedRuleDetails;
			m_rdoRenderingHasSuffix.Tag = m_pnlVernacularSuffix;
			m_rdoRenderingHasPrefix.Tag = m_pnlVernacularPrefix;
			m_rdoUserDefinedRenderingCriteria.Tag = m_pnlUserDefinedRenderingMatch;

			foreach (string word in allWordsInQuestions)
			{
				m_cboFollowingWord.Items.Add(word);
				m_cboPrecedingWord.Items.Add(word);
			}

			m_rule = rule;
			m_selectKeyboard = selectKeyboard;
			ValidateName = nameValidator;
			m_txtName.Text = m_rule.Name;

			switch (m_rule.QuestionMatchCriteriaType)
			{
				case RenderingSelectionRule.QuestionMatchType.Undefined:
					Text = Properties.Resources.kstidEditRuleCaption;
					m_rdoSuffix.Checked = true; // default;
					//SetDetails(m_cboSuffix, string.Empty);
					return;
				case RenderingSelectionRule.QuestionMatchType.Suffix:
					m_rdoSuffix.Checked = true;
					SetDetails(m_cboSuffix, m_rule.QuestionMatchSuffix);
					break;
				case RenderingSelectionRule.QuestionMatchType.Prefix:
					m_rdoPrefix.Checked = true;
					SetDetails(m_cboPrefix, m_rule.QuestionMatchPrefix);
					break;
				case RenderingSelectionRule.QuestionMatchType.PrecedingWord:
					m_rdoPreceedingWord.Checked = true;
					SetDetails(m_cboPrecedingWord, m_rule.QuestionMatchPrecedingWord);
					break;
				case RenderingSelectionRule.QuestionMatchType.FollowingWord:
					m_rdoFollowingWord.Checked = true;
					SetDetails(m_cboFollowingWord, m_rule.QuestionMatchFollowingWord);
					break;
				case RenderingSelectionRule.QuestionMatchType.Custom:
					m_rdoUserDefinedQuestionCriteria.Checked = true;
					m_txtQuestionMatchRegEx.Text = m_rule.QuestionMatchingPattern;
					break;
			}

			switch (m_rule.RenderingMatchCriteriaType)
			{
				case RenderingSelectionRule.RenderingMatchType.Undefined: // default
				case RenderingSelectionRule.RenderingMatchType.Suffix:
					m_rdoRenderingHasSuffix.Checked = true;
					m_txtVernacularSuffix.Text = m_rule.RenderingMatchSuffix;
					break;
				case RenderingSelectionRule.RenderingMatchType.Prefix:
					m_txtVernacularPrefix.Text = m_rule.RenderingMatchPrefix;
					break;
				case RenderingSelectionRule.RenderingMatchType.Custom:
					m_txtRenderingMatchRegEx.Text = m_rule.RenderingMatchingPattern;
					break;
			}
		}

		private static void SetDetails(ComboBox cbo, string details)
		{
			if (string.IsNullOrEmpty(details))
				cbo.SelectedIndex = -1;
			else
			{
				int index = cbo.FindStringExact(details);
				if (index >= 0 || cbo.DropDownStyle == ComboBoxStyle.DropDownList)
					cbo.SelectedIndex = index;
				cbo.Text = details;
			}
		}
		#endregion

		#region Event handlers
		private void OptionCheckedChanged(object sender, System.EventArgs e)
		{
			RadioButton btn = (RadioButton)sender;
			Panel panel = (Panel)btn.Tag;
			panel.Visible = btn.Checked;
			UpdateStatus();
		}

		private void m_cboSuffix_TextChanged(object sender, EventArgs e)
		{
			m_rule.QuestionMatchSuffix = m_cboSuffix.Text;
			UpdateStatus();
		}

		private void m_pnlSuffixDetails_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlSuffixDetails.Visible)
				m_cboSuffix_TextChanged(m_cboSuffix, e);
		}

		private void m_cboPrefix_TextChanged(object sender, EventArgs e)
		{
			m_rule.QuestionMatchPrefix = m_cboPrefix.Text;
			UpdateStatus();
		}

		private void m_pnlPrefixDetails_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlPrefixDetails.Visible)
				m_cboPrefix_TextChanged(m_cboSuffix, e);
		}

		private void m_cboPrecedingWord_TextChanged(object sender, EventArgs e)
		{
			m_rule.QuestionMatchPrecedingWord = m_cboPrecedingWord.Text;
			UpdateStatus();
		}

		private void m_pnlPrecedingWordDetails_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlPrecedingWordDetails.Visible)
				m_cboPrecedingWord_TextChanged(m_cboSuffix, e);
		}

		private void m_cboFollowingWord_TextChanged(object sender, EventArgs e)
		{
			m_rule.QuestionMatchFollowingWord = m_cboFollowingWord.Text;
			UpdateStatus();
		}

		private void m_pnlFollowingWordDetails_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlFollowingWordDetails.Visible)
				m_cboFollowingWord_TextChanged(m_cboSuffix, e);
		}

		private void m_txtQuestionMatchRegEx_TextChanged(object sender, EventArgs e)
		{
			m_rule.QuestionMatchingPattern = m_txtQuestionMatchRegEx.Text;
			UpdateStatus();
		}

		private void m_pnlUserDefinedRuleDetails_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlUserDefinedRuleDetails.Visible)
				m_txtQuestionMatchRegEx_TextChanged(m_txtQuestionMatchRegEx, e);
		}

		private void m_txtVernacularSuffix_TextChanged(object sender, EventArgs e)
		{
			m_rule.RenderingMatchSuffix = m_txtVernacularSuffix.Text;
			UpdateStatus();
		}

		private void m_pnlVernacularSuffix_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlVernacularSuffix.Visible)
				m_txtVernacularSuffix_TextChanged(m_txtVernacularSuffix, e);
		}

		private void m_txtVernacularPrefix_TextChanged(object sender, EventArgs e)
		{
			m_rule.RenderingMatchPrefix = m_txtVernacularPrefix.Text;
			UpdateStatus();
		}

		private void m_pnlVernacularPrefix_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlVernacularPrefix.Visible)
				m_txtVernacularPrefix_TextChanged(m_txtVernacularPrefix, e);
		}

		private void m_txtRenderingMatchRegEx_TextChanged(object sender, EventArgs e)
		{
			m_rule.RenderingMatchingPattern = m_txtRenderingMatchRegEx.Text;
			UpdateStatus();
		}

		private void m_pnlUserDefinedRenderingMatch_VisibleChanged(object sender, EventArgs e)
		{
			if (m_pnlUserDefinedRenderingMatch.Visible)
				m_txtRenderingMatchRegEx_TextChanged(m_txtRenderingMatchRegEx, e);
		}

		private void VernacularTextBox_Enter(object sender, EventArgs e)
		{
			if (m_selectKeyboard != null)
				m_selectKeyboard(true);
		}

		private void VernacularTextBox_Leave(object sender, EventArgs e)
		{
			if (m_selectKeyboard != null)
				m_selectKeyboard(false);
		}

		private void m_txtName_Validating(object sender, System.ComponentModel.CancelEventArgs e)
		{
			string name = m_txtName.Text.Trim();
			if (name.Length == 0)
			{
				MessageBox.Show(Properties.Resources.kstidRuleNameRequired, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				e.Cancel = true;
			}
			else if (!ValidateName(name))
			{
				MessageBox.Show(Properties.Resources.kstidRuleNameRequired, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				e.Cancel = true;
			}
			m_rule.Name = name;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (!m_rule.Valid)
			{
				string errorPartA = (m_rule.ErrorMessageQ != null) ?
					Properties.Resources.kstidInvalidQuestionCondition + Environment.NewLine + m_rule.ErrorMessageQ :
					Properties.Resources.kstidInvalidRenderingCondition + Environment.NewLine + m_rule.ErrorMessageR;

				switch (MessageBox.Show(errorPartA + Environment.NewLine + Properties.Resources.kstidFixConditionNow,
					Properties.Resources.kstidInvalidRegularExpressionCaption, MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Stop))
				{
					case DialogResult.Yes:
						return;
					case DialogResult.No:
						DialogResult = DialogResult.OK; break;
					case DialogResult.Cancel:
						DialogResult = DialogResult.Cancel; break;
				}
			}
			else
				DialogResult = DialogResult.OK;
			Close();
		}
		#endregion

		#region Private helper methods
		private void UpdateStatus()
		{
			m_lblDescription.Text = m_rule.Description;
			btnOk.Enabled = m_txtName.Text != string.Empty &&
				m_rule.QuestionMatchCriteriaType != RenderingSelectionRule.QuestionMatchType.Undefined;
		}
		#endregion
	}
}