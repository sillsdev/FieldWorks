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
// File: PhraseSubstitutionsDlg.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SilUtils.Controls;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog box where user can list words and phrases that should not be treated as part of
	/// the original question for the purpose of parsing into parts.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class PhraseSubstitutionsDlg : Form
	{
		protected TextBox TextControl { get; set; }
		private readonly CustomDropDown m_regexMatchDropDown = new CustomDropDown();
		private readonly CustomDropDown m_regexReplaceDropDown = new CustomDropDown();
		private static readonly Regex s_matchNTimes = new Regex(@"(?<expressionToRepeat>\(.*\)|.)\{(?<minMatches>\d+,)?(?<maxMatches>\d+)\}");
		private static readonly Regex s_matchSubstGroup = new Regex(@"\$((?<numeric>(\d+)|&)|(\{(?<named>\w[a-zA-Z_0-9]*)\}))");
		public const string kEntireMatch = "Entire match";
		private const string kContiguousLettersMatchExpr = @"(\w+)";
		protected readonly string m_sRemoveItem;

		#region Constructor and initialization methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PhraseSubstitutionsDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PhraseSubstitutionsDlg(IEnumerable<Substitution> phraseSubstitutions,
			IEnumerable<string> previewTestPhrases, int iDefaultTestPhrase)
		{
			InitializeComponent();

			m_regexMatchHelper.CreateControl();
			m_btnMatchSingleWord.Width = m_regexMatchHelper.Width - m_btnMatchSingleWord.Left * 2;
			m_dataGridView.Controls.Remove(m_regexMatchHelper);
			m_regexMatchDropDown.AutoClose = false;
			m_regexMatchDropDown.AutoCloseWhenMouseLeaves = false;
			m_regexReplaceDropDown.AutoClose = false;
			m_regexReplaceDropDown.AutoCloseWhenMouseLeaves = false;
			m_regexMatchDropDown.AddControl(m_regexMatchHelper);
			m_regexReplaceDropDown.AddControl(m_regexReplacementHelper);
			m_sRemoveItem = m_cboMatchGroup.Items[0] as string;
			m_cboMatchGroup.Items.Clear();

			foreach (Substitution substitution in phraseSubstitutions)
			{
				m_dataGridView.Rows.Add(substitution.MatchingPattern, substitution.Replacement,
					substitution.IsRegex);
			}

			m_cboPreviewQuestion.Items.AddRange(previewTestPhrases.ToArray());
			if (m_cboPreviewQuestion.Items.Count > 0)
				m_cboPreviewQuestion.SelectedIndex = iDefaultTestPhrase;

			m_txtMatchPrefix.Tag = @"\b{0}";
			m_txtMatchSuffix.Tag = @"{0}\b";
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the substitutions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal IEnumerable<Substitution> Substitutions
		{
			get
			{
				return m_dataGridView.Rows.Cast<DataGridViewRow>().Select(row =>
					GetSubstitutionForRow(row)).Where(sub => sub != null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the match count value from the expression covered, partially covered, or
		/// immediately preceding the selected text in the edit control for the data grid view
		/// cell currently being edited. If there is no explicit match count expression, then
		/// this returns 1. Intended to be used only when the current column is the "Match"
		/// column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ExistingMatchCountValue
		{
			get
			{
				Match match = FindSelectedMatch(s_matchNTimes);
				return match.Success? int.Parse(match.Result("${maxMatches}")) : 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the prefix, if any, from the expression covered, partially covered, or
		/// immediately preceding the selected text in the edit control for the data grid view
		/// cell currently being edited. If there is no prefix expression, then this returns an
		/// empty string. Intended to be used only when the current column is the "Match" column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ExistingPrefix
		{
			get
			{
				return GetExistingAffix((string)m_txtMatchPrefix.Tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the suffix, if any, from the expression covered, partially covered, or
		/// immediately preceding the selected text in the edit control for the data grid view
		/// cell currently being edited. If there is no suffix expression, then this returns an
		/// empty string. Intended to be used only when the current column is the "Match" column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ExistingSuffix
		{
			get
			{
				return GetExistingAffix((string)m_txtMatchSuffix.Tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the match group, if any, from the expression covered, partially covered, or
		/// immediately preceding the selected text in the edit control for the data grid view
		/// cell currently being edited. If there is no match group expression, then this
		/// returns an empty string. Intended to be used only when the current column is the
		/// "Replacement" column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ExistingMatchGroup
		{
			get
			{
				Match match = FindSelectedMatch(s_matchSubstGroup);
				string group = match.Success ? match.Result("${numeric}${named}") : string.Empty;
				return (group == "0" || group == "&") ? kEntireMatch : group;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the EditingControlShowing event of the m_dataGridView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewEditingControlShowingEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
		{
			Debug.Assert(TextControl == null && !m_regexMatchDropDown.Visible && !m_regexReplaceDropDown.Visible);
			if (IsRegEx(m_dataGridView.CurrentRow))
				TextControl = e.Control as DataGridViewTextBoxEditingControl;
			if (TextControl == null)
				return;

			Rectangle cellDisplayRect = m_dataGridView.GetCellDisplayRectangle(
				m_dataGridView.CurrentCell.ColumnIndex, m_dataGridView.CurrentCell.RowIndex, true);

			if (m_dataGridView.CurrentCell.ColumnIndex == colMatch.Index)
			{
				m_regexMatchDropDown.Show(m_dataGridView, cellDisplayRect.Left, cellDisplayRect.Bottom + 1);
			}
			else
			{
				string[] matchGroups = GetMatchGroups((string)m_dataGridView.CurrentRow.Cells[colMatch.Index].Value);
				if (matchGroups.Length > 0)
				{
					m_cboMatchGroup.Items.AddRange(matchGroups);
					string sGroup = ExistingMatchGroup;
					m_cboMatchGroup.Items.Insert(0, sGroup.Length > 0 ? m_sRemoveItem : string.Empty);
					m_regexReplaceDropDown.Show(m_dataGridView, cellDisplayRect.Left, cellDisplayRect.Bottom + 1);
				}
				else
					return;
			}

			TextControl.HideSelection = false;
			TextControl.KeyDown += txtControl_KeyDown;
			TextControl.TextChanged += txtControl_TextChanged;
			TextControl.TextChanged += UpdatePreview;
			TextControl.MouseClick += txtControl_MouseClick;
		}

		void txtControl_TextChanged(object sender, EventArgs e)
		{
			UpdateRegExHelperControls();
		}

		private void txtControl_MouseClick(object sender, MouseEventArgs e)
		{
			UpdateRegExHelperControls();
		}

		private void txtControl_KeyDown(object sender, KeyEventArgs e)
		{
			UpdateRegExHelperControls();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CurrentCellChanged event of the m_dataGridView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_CurrentCellChanged(object sender, EventArgs e)
		{
			if (m_regexMatchDropDown.Visible)
				m_regexMatchDropDown.Close(ToolStripDropDownCloseReason.CloseCalled);
			else if (m_regexReplaceDropDown.Visible)
			{
				m_regexReplaceDropDown.Close(ToolStripDropDownCloseReason.CloseCalled);
				m_cboMatchGroup.Items.Clear();
			}
			else
				return;
			TextControl.KeyDown -= txtControl_KeyDown;
			TextControl.TextChanged -= txtControl_TextChanged;
			TextControl.TextChanged -= UpdatePreview;
			TextControl.MouseClick -= txtControl_MouseClick;
			TextControl = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnMatchSingleWord control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnMatchSingleWord_Click(object sender, EventArgs e)
		{
			int selStart = TextControl.SelectionStart;
			ReplaceSelectedTextInCurrentEditControl(kContiguousLettersMatchExpr);
			TextControl.SelectionStart = selStart + kContiguousLettersMatchExpr.Length;
			TextControl.SelectionLength = 0;
			TextControl.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event for the suffixes or prefix TextBox control.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void SuffixOrPrefixChanged(object sender, EventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			string format = (string)textBox.Tag;
			string sText = textBox.Text.Trim();
			if (sText.Length == 0)
			{
				Match match = FindAffixExpression(format);
				if (match.Success)
				{
					TextControl.Text = match.Result("$`$'");
					TextControl.SelectionStart = match.Index;
					TextControl.SelectionLength = 0;
				}
				return;
			}
			SelectExistingPrefixOrSuffix(format);
			int selRestore = TextControl.SelectionStart + format.IndexOf("{0}");
			ReplaceSelectedTextInCurrentEditControl(string.Format(format, sText));
			TextControl.SelectionStart = selRestore;
			TextControl.SelectionLength = sText.Length;
			UpdateRegExHelperControls();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ValueChanged event of the m_numTimesToMatch control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_numTimesToMatch_ValueChanged(object sender, EventArgs e)
		{
			TextControl.TextChanged -= txtControl_TextChanged;
			UpdateMatchCount((int)m_numTimesToMatch.Value);
			TextControl.TextChanged += txtControl_TextChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboMatchGroup control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void m_cboMatchGroup_SelectedIndexChanged(object sender, EventArgs e)
		{
			TextControl.TextChanged -= txtControl_TextChanged;
			UpdateMatchGroup(m_cboMatchGroup.Text);
			int i = m_cboMatchGroup.SelectedIndex;
			m_cboMatchGroup.SelectedIndexChanged -= m_cboMatchGroup_SelectedIndexChanged;
			m_cboMatchGroup.Items[0] = m_cboMatchGroup.Text != m_sRemoveItem ? m_sRemoveItem : string.Empty;
			m_cboMatchGroup.SelectedIndex = i;
			m_cboMatchGroup.SelectedIndexChanged += m_cboMatchGroup_SelectedIndexChanged;
			TextControl.TextChanged += txtControl_TextChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RowEnter event of the m_dataGridView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_RowEnter(object sender, DataGridViewCellEventArgs e)
		{
			UpdatePreview(e.RowIndex >= 0, m_dataGridView.Rows[e.RowIndex]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the preview.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void UpdatePreview(object sender, EventArgs e)
		{
			UpdatePreview(m_dataGridView.CurrentRow != null, m_dataGridView.CurrentRow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellValueChanged event of the m_dataGridView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			UpdatePreview(m_dataGridView.CurrentRow != null, m_dataGridView.CurrentRow);
		}
		#endregion

		#region Private/protected helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for a prefix or suffix expression (as determined by the format parameter)
		/// covered, partially covered, or immediately preceding the selected text in the edit
		/// control for the data grid view cell currently being edited.Selects the existing
		/// prefix or suffix. If one is found, the selection in the text box edit control is
		/// set to cover the entire expression representing the prefix or suffix, i.e.,
		/// including the regular expression marker.
		/// </summary>
		/// <param name="format">The format.</param>
		/// ------------------------------------------------------------------------------------
		private void SelectExistingPrefixOrSuffix(string format)
		{
			Match match = FindAffixExpression(format);
			if (!match.Success)
				return;
			TextControl.SelectionStart = match.Index;
			TextControl.SelectionLength = match.Length;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the regex preview for the given row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdatePreview(bool enabled, DataGridViewRow row)
		{
			m_grpPreview.Enabled = enabled;
			if (enabled)
			{
				m_lblResult.ForeColor = SystemColors.ControlText;
				Substitution sub;
				if (TextControl == null)
					sub = GetSubstitutionForRow(row);
				else
				{
					string match = (m_dataGridView.CurrentCell.ColumnIndex == colMatch.Index) ?
						TextControl.Text : row.Cells[colMatch.Index].Value as string;
					string replacement = (m_dataGridView.CurrentCell.ColumnIndex == colReplacement.Index) ?
						TextControl.Text : row.Cells[colReplacement.Index].Value as String;
					sub = GetSubstitutionForRow(match, replacement, row);
				}
				if (sub != null)
				{
					if (sub.Valid)
						m_lblResult.Text = sub.RegEx.Replace(m_cboPreviewQuestion.Text, sub.RegExReplacementString);
					else
					{
						m_lblResult.ForeColor = Color.Red;
						m_lblResult.Text = sub.ErrorMessage;
					}
				}
				else
				{
					m_lblResult.Text = m_cboPreviewQuestion.Text;
				}
			}
			else
				m_lblResult.Text = string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the max match count control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateRegExHelperControls()
		{
			if (m_regexMatchDropDown.Visible)
			{
				UpdateMaxMatchCountControl();
				m_txtMatchPrefix.TextChanged -= SuffixOrPrefixChanged;
				m_txtMatchPrefix.Text = ExistingPrefix;
				m_txtMatchPrefix.TextChanged += SuffixOrPrefixChanged;
				m_txtMatchSuffix.TextChanged -= SuffixOrPrefixChanged;
				m_txtMatchSuffix.Text = ExistingSuffix;
				m_txtMatchSuffix.TextChanged += SuffixOrPrefixChanged;
			}
			if (m_regexReplaceDropDown.Visible)
			{
				string sExisting = ExistingMatchGroup;
				m_cboMatchGroup.SelectedIndexChanged -= m_cboMatchGroup_SelectedIndexChanged;
				m_cboMatchGroup.Items[0] = sExisting.Length > 0 ? m_sRemoveItem : string.Empty;
				m_cboMatchGroup.SelectedIndex = m_cboMatchGroup.FindStringExact(sExisting);
				m_cboMatchGroup.SelectedIndexChanged += m_cboMatchGroup_SelectedIndexChanged;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the max match count control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateMaxMatchCountControl()
		{
			m_numTimesToMatch.ValueChanged -= m_numTimesToMatch_ValueChanged;
			m_numTimesToMatch.Value = ExistingMatchCountValue;
			m_numTimesToMatch.ValueChanged += m_numTimesToMatch_ValueChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the match count expression currently selected (or near the selection) in the
		/// edit control for the data grid view cell currently being edited, or inserts a new
		/// match count expression if there isn't one already.
		/// </summary>
		/// <param name="numTimesToMatch">The num times to match.</param>
		/// ------------------------------------------------------------------------------------
		protected void UpdateMatchCount(int numTimesToMatch)
		{
			int insertAt = TextControl.SelectionStart;
			if (insertAt == 0 && TextControl.SelectionLength == 0)
				return;
			int length = 0;
			string text = TextControl.Text;
			Match match = FindSelectedMatch(s_matchNTimes);
			if (match.Success)
			{
				if (numTimesToMatch > 1)
				{
					string minRange = match.Result("${minMatches}");
					if (String.IsNullOrEmpty(minRange))
						minRange = "1,";
					text = match.Result("$`${expressionToRepeat}");
					insertAt = text.Length;
					text += match.Result("{" + minRange + numTimesToMatch + "}");
					length = text.Length - insertAt;
					text += match.Result("$'");
				}
				else
				{
					text = match.Result("$`${expressionToRepeat}");
					insertAt = text.Length;
					length = 0;
					text += match.Result("$'");
				}
			}
			else
			{
				if (TextControl.SelectionLength > 1)
				{
					if (text[insertAt + TextControl.SelectionLength - 1] != ')' || text[insertAt] != '(')
					{
						text = text.Insert(insertAt + TextControl.SelectionLength, ")");
						text = text.Insert(insertAt, "(");
						insertAt += TextControl.SelectionLength + 2;
					}
					else
						insertAt += TextControl.SelectionLength;
				}
				if (numTimesToMatch > 1)
				{
					string sTextToInsert = "{1," + numTimesToMatch + "}";
					length = sTextToInsert.Length;
					text = text.Insert(insertAt, sTextToInsert);
				}
			}
			TextControl.Text = text;
			TextControl.SelectionStart = insertAt;
			TextControl.SelectionLength = length;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the match group substitution expression currently selected (or near the
		/// selection) in the edit control for the data grid view cell currently being edited,
		/// or inserts a new match group substitution expression if there isn't one already.
		/// </summary>
		/// <param name="sGroup">The s group.</param>
		/// ------------------------------------------------------------------------------------
		protected void UpdateMatchGroup(string sGroup)
		{
			if (string.IsNullOrEmpty(sGroup))
				throw new ArgumentException("Parameter must not be null or empty", "sGroup");

			if (sGroup == kEntireMatch)
				sGroup = "&";
			int insertAt = TextControl.SelectionStart;
			int length = sGroup.Length;
			string text = TextControl.Text;
			Match match = FindSelectedMatch(s_matchSubstGroup);
			if (match.Success)
			{
				if (sGroup != m_sRemoveItem)
				{
					text = match.Result(@"$`$$");
					insertAt = text.Length;

					if (!char.IsNumber(sGroup[0]) && sGroup != "&")
					{
						text += "{" + sGroup + "}";
						insertAt++;
					}
					else
					{
						text += sGroup;
					}

					text += match.Result("$'");
				}
				else
				{
					text = match.Result("$`");
					insertAt = text.Length;
					length = 0;
					text += match.Result("$'");
				}
			}
			else
			{
				if (TextControl.SelectionLength > 1)
					text = text.Remove(insertAt, TextControl.SelectionLength);

				if (!char.IsNumber(sGroup[0]) && sGroup != "&")
				{
					text = text.Insert(insertAt, "${" + sGroup + "}");
					insertAt++;
				}
				else
				{
					text = text.Insert(insertAt, "$" + sGroup);
				}
				insertAt++; // Don't want to select the $
			}
			TextControl.Text = text;
			TextControl.SelectionStart = insertAt;
			TextControl.SelectionLength = length;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of strings that can be used to describe the regular expression match
		/// groups found in the given expression.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected string[] GetMatchGroups(string matchExpression)
		{
			try
			{
				Regex r = new Regex(matchExpression);
				string[] matchGroups = r.GetGroupNames();
				matchGroups[0] = kEntireMatch;
				return matchGroups;
			}
			catch (ArgumentException)
			{
				return new string[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified row in the data grid view represents a regular
		/// expression.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsRegEx(DataGridViewRow row)
		{
			return (bool)(row.Cells[colIsRegEx.Index].Value ?? false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an existing prefix or suffix (as determined by the format string) covered,
		/// partially covered, or immediately preceding the selected text in the edit control
		/// for the data grid view cell currently being edited.
		/// </summary>
		/// <param name="format">Format string in the form "\b{0}" (indicating a prefix) or
		/// "{0}\b" (indicating a suffix).</param>
		/// <returns>The (English) text portion of the prefix or suffix, i.e., without the
		/// regular expression marker</returns>
		/// ------------------------------------------------------------------------------------
		private string GetExistingAffix(string format)
		{
			Match match = FindAffixExpression(format);
			return (match.Success) ? match.Result("$1") : string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the prefix or suffix expression covered, partially covered, or immediately
		/// preceding the selected text in the edit control for the data grid view cell
		/// currently being edited.
		/// </summary>
		/// <param name="format">Format string in the form "\b{0}" (indicating a prefix) or
		/// "{0}\b" (indicating a suffix).</param>
		/// <returns>A Match object representing the regular expression for the prefix or
		/// suffix</returns>
		/// ------------------------------------------------------------------------------------
		private Match FindAffixExpression(string format)
		{
			Regex matchPattern = new Regex(string.Format(format.Replace(@"\b", @"\\b"), kContiguousLettersMatchExpr));
			return FindSelectedMatch(matchPattern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the expression covered, partially covered, or immediately preceding the
		/// selected text in the edit control for the data grid view cell currently being edited.
		/// </summary>
		/// <param name="matchPattern">The match pattern.</param>
		/// <returns>A Match object representing the text found.</returns>
		/// ------------------------------------------------------------------------------------
		private Match FindSelectedMatch(Regex matchPattern)
		{
			Match match = matchPattern.Match(TextControl.Text);
			while (match.Success && match.Index + match.Length < TextControl.SelectionStart)
				match = match.NextMatch();
			return (match.Success && match.Index <= TextControl.SelectionStart) ? match : Match.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Substitution object representing the current state of the given row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Substitution GetSubstitutionForRow(DataGridViewRow row)
		{
			return GetSubstitutionForRow(row.Cells[colMatch.Index].Value as string,
				row.Cells[colReplacement.Index].Value as String, row);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Substitution object representing the current state of the given row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Substitution GetSubstitutionForRow(string matchingPattern, string replacement,
			DataGridViewRow row)
		{
			if (matchingPattern == null)
				return null;
			bool matchCase = (bool)(row.Cells[colMatchCase.Index].Value ?? false);
			return new Substitution(matchingPattern, replacement, IsRegEx(row), matchCase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces the currently selected text in the edit control for the data grid view cell
		/// currently being edited.
		/// </summary>
		/// <param name="textToInsert">The text to insert.</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceSelectedTextInCurrentEditControl(string textToInsert)
		{
			string cellValue = TextControl.Text;
			if (cellValue == null)
				return;
			cellValue = cellValue.Remove(TextControl.SelectionStart, TextControl.SelectionLength);
			cellValue = cellValue.Insert(TextControl.SelectionStart, textToInsert);
			TextControl.Text = cellValue;
		}
		#endregion
	}
}