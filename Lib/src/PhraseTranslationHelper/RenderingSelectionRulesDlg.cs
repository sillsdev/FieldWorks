// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RenderingSelectionRulesDlg.cs
//
// Some icons used in this dialog box were downloaded from http://www.iconfinder.com
// The Add Rule icon was developed by Yusuke Kamiyamane and is covered by this Creative Commons
// License: http://creativecommons.org/licenses/by/3.0/
// The Copy Rule icon was developed by Momenticons and is covered by this Creative Commons
// License: http://creativecommons.org/licenses/by/3.0/
// The Delete Rule icon was developed by Rodolphe and is covered by the GNU General Public
// License: http://www.gnu.org/copyleft/gpl.html
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class RenderingSelectionRulesDlg : Form
	{
		private readonly Action<bool> m_selectKeyboard;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RenderingSelectionRulesDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RenderingSelectionRulesDlg(IEnumerable<RenderingSelectionRule> rules,
			Action<bool> selectKeyboard)
		{
			m_selectKeyboard = selectKeyboard;
			InitializeComponent();

			toolStrip1.Renderer = new NoToolStripBorderRenderer();

			if (rules != null && rules.Any())
			{
				foreach (RenderingSelectionRule rule in rules)
					m_listRules.Items.Add(rule, !rule.Disabled);
				m_listRules.SelectedIndex = 0;
			}
			btnEdit.Enabled = btnCopy.Enabled = btnDelete.Enabled = (m_listRules.SelectedIndex >= 0);
		}

		public IEnumerable<RenderingSelectionRule> Rules
		{
			get { return m_listRules.Items.Cast<RenderingSelectionRule>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnNew control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnNew_Click(object sender, System.EventArgs e)
		{
			int i = 1;
			string name = string.Format(Properties.Resources.kstidNewSelectionRuleNameTemplate, i);

			Func<string, bool> nameIsUnique = n => !m_listRules.Items.Cast<RenderingSelectionRule>().Any(r => r.Name == n);
			while (!nameIsUnique(name))
				name = string.Format(Properties.Resources.kstidNewSelectionRuleNameTemplate, ++i);

			RenderingSelectionRule rule = new RenderingSelectionRule(name);

			using (RulesWizardDlg dlg = new RulesWizardDlg(rule, Word.AllWords, m_selectKeyboard, nameIsUnique))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_listRules.SelectedIndex = m_listRules.Items.Add(rule);
					if (rule.Valid)
						m_listRules.SetItemChecked(m_listRules.SelectedIndex, true);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnEdit control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnEdit_Click(object sender, EventArgs e)
		{
			RenderingSelectionRule rule = m_listRules.SelectedItem as RenderingSelectionRule;
			string origName = rule.Name;
			string origQ = rule.QuestionMatchingPattern;
			string origR = rule.RenderingMatchingPattern;
			Func<string, bool> nameIsUnique = n => !m_listRules.Items.Cast<RenderingSelectionRule>().Where(r => r != rule).Any(r => r.Name == n);
			using (RulesWizardDlg dlg = new RulesWizardDlg(rule, Word.AllWords, m_selectKeyboard, nameIsUnique))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					if (!rule.Valid)
						m_listRules.SetItemChecked(m_listRules.SelectedIndex, false);

					m_listRules.Invalidate();
				}
				else
				{
					rule.Name = origName;
					rule.QuestionMatchingPattern = origQ;
					rule.RenderingMatchingPattern = origR;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnCopy control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnCopy_Click(object sender, EventArgs e)
		{
			int iOrigRule = m_listRules.SelectedIndex;
			RenderingSelectionRule origRule = m_listRules.SelectedItem as RenderingSelectionRule;
			RenderingSelectionRule newRule = new RenderingSelectionRule(origRule.QuestionMatchingPattern, origRule.RenderingMatchingPattern);

			int i = 1;
			string name = string.Format(Properties.Resources.kstidCopiedSelectionRuleNameTemplate, origRule.Name, string.Empty);

			Func<string, bool> nameIsUnique = n => !m_listRules.Items.Cast<RenderingSelectionRule>().Any(r => r.Name == n);
			while (!nameIsUnique(name))
				name = string.Format(Properties.Resources.kstidCopiedSelectionRuleNameTemplate, origRule.Name, "(" + i++ + ")");

			newRule.Name = name;

			using (RulesWizardDlg dlg = new RulesWizardDlg(newRule, Word.AllWords, m_selectKeyboard, nameIsUnique))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_listRules.SelectedIndex = m_listRules.Items.Add(newRule);
					if (newRule.Valid)
						m_listRules.SetItemChecked(m_listRules.SelectedIndex, m_listRules.GetItemChecked(iOrigRule));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnDelete control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnDelete_Click(object sender, EventArgs e)
		{
			int i = m_listRules.SelectedIndex;
			m_listRules.Items.RemoveAt(i);
			if (m_listRules.Items.Count > 0)
				m_listRules.SelectedIndex = m_listRules.Items.Count > i ? i : i - 1;
		}

		private void m_listRules_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			RenderingSelectionRule rule = m_listRules.SelectedItem as RenderingSelectionRule;
			btnEdit.Enabled = btnCopy.Enabled = btnDelete.Enabled = (rule != null);
			if (rule != null)
				m_lblDescription.Text = rule.Description;
		}

		private void m_listRules_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			((RenderingSelectionRule)m_listRules.Items[e.Index]).Disabled = e.NewValue == CheckState.Unchecked;
		}
	}

	public class NoToolStripBorderRenderer : ToolStripProfessionalRenderer
	{
		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			// Eat this event.
		}
	}

}