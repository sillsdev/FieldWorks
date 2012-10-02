using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace SilConvertersXML
{
	public partial class CreateLimitationForm : Form
	{
		protected const char chQuote = '\'';
		protected const char chDoubleQuote = '\"';

		protected List<string> m_strIteratorValues = new List<string>();
		protected System.Collections.Specialized.StringCollection m_astrPreviousConstraints = null;
		protected List<string> m_strPreviousValues = new List<string>();

		public CreateLimitationForm(string strXPathRoot, string strName, XPathNodeIterator xpIterator, bool bAttribute,
			System.Collections.Specialized.StringCollection astrPreviousConstraints)
		{
			InitializeComponent();

			m_astrPreviousConstraints = astrPreviousConstraints;
			this.textBoxXPath.Text = strXPathRoot;
			this.textBoxName.Text = strName;

			while ((xpIterator != null) && xpIterator.MoveNext())
			{
				string strValue = xpIterator.Current.Value;
				if (!m_strIteratorValues.Contains(strValue))
					m_strIteratorValues.Add(strValue);
			}

			if (bAttribute)
				radioButtonSpecificValue.Checked = true;
			else
				radioButtonPresenceOnly.Checked = true;

			// only enabled on subsequent executions
			radioButtonPreviousConstraint.Enabled = (m_astrPreviousConstraints.Count > 0);

			helpProvider.SetHelpString(checkedListBox, Properties.Resources.checkedListBoxHelp);
			helpProvider.SetHelpString(flowLayoutPanelConstraintType, Properties.Resources.flowLayoutPanelConstraintTypeHelp);
		}

		protected void UpdateFilter()
		{
			string strFilter = null;
			if (radioButtonSpecificValue.Checked)
			{
				strFilter = String.Format("{0}[", textBoxXPath.Text);

				int nExtraLength = 0;
				int nCount = checkedListBox.Items.Count;
				for (int nIndex = 0; nIndex < nCount; nIndex++)
				{
					CheckState eCheckState = checkedListBox.GetItemCheckState(nIndex);
					string strValue = (string)checkedListBox.Items[nIndex];
					if (eCheckState == CheckState.Checked)
					{
						char chDelim = (strValue.IndexOf(chDoubleQuote) == -1) ? chDoubleQuote : chQuote;
						strFilter += String.Format("{0} = {2}{1}{2} or ",
							textBoxName.Text, strValue, chDelim);
						nExtraLength = 4;   // amount to strip off if this is the last one
					}
					else if (eCheckState == CheckState.Indeterminate)
					{
						char chDelim = (strValue.IndexOf(chDoubleQuote) == -1) ? chDoubleQuote : chQuote;
						strFilter += String.Format("not({0} = {2}{1}{2}) and ",
							textBoxName.Text, strValue, chDelim);
						nExtraLength = 5;   // amount to strip off if this is the last one
					}
				}

				strFilter = strFilter.Substring(0, strFilter.Length - nExtraLength) + "]";
			}
			else if (radioButtonPreviousConstraint.Checked)
			{
				strFilter = String.Format("{0}[", textBoxXPath.Text);

				int nExtraLength = 0;
				int nCount = checkedListBox.Items.Count;
				for (int nIndex = 0; nIndex < nCount; nIndex++)
				{
					CheckState eCheckState = checkedListBox.GetItemCheckState(nIndex);
					string strValue = (string)checkedListBox.Items[nIndex];
					if (eCheckState == CheckState.Checked)
					{
						strFilter += String.Format("{0} = {1} or ",
							textBoxName.Text, strValue);
						nExtraLength = 4;   // amount to strip off if this is the last one
					}
					else if (eCheckState == CheckState.Indeterminate)
					{
						strFilter += String.Format("not({0} = {1}) and ",
							textBoxName.Text, strValue);
						nExtraLength = 5;   // amount to strip off if this is the last one
					}
				}

				strFilter = strFilter.Substring(0, strFilter.Length - nExtraLength) + "]";
			}
			else if (radioButtonAbsence.Checked)
			{
				strFilter = String.Format("{0}[not({1})]",
					textBoxXPath.Text, textBoxName.Text);
			}
			else if (radioButtonManuallyEntered.Checked)
			{
				strFilter = FilterXPath;
			}
			else
			{
				strFilter = String.Format("{0}[{1}]",
					textBoxXPath.Text, textBoxName.Text);
			}

			this.textBoxFilter.Text = strFilter;
		}

		public string FilterXPath
		{
			get { return this.textBoxFilter.Text; }
			set { this.textBoxFilter.Text = value; }
		}

		private void listBoxAttrValues_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			DoOk();
		}

		protected void DoOk()
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DoOk();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void radioButton_CheckedChanged(object sender, EventArgs e)
		{
			checkedListBox.SelectedIndex = -1;
			checkedListBox.Enabled = false;
			UpdateFilter();
		}

		void radioButtonPreviousConstraint_CheckedChanged(object sender, System.EventArgs e)
		{
			if (radioButtonPreviousConstraint.Checked)
			{
				checkedListBox.Enabled = true;
				checkedListBox.Items.Clear();
				foreach (string strValue in m_astrPreviousConstraints)
					if (!checkedListBox.Items.Contains(strValue))
						checkedListBox.Items.Add(strValue);

				UpdateFilter();
			}
		}

		void radioButtonSpecificValue_CheckedChanged(object sender, System.EventArgs e)
		{
			if (radioButtonSpecificValue.Checked)
			{
				checkedListBox.Enabled = true;
				checkedListBox.Items.Clear();
				foreach (string strValue in m_strIteratorValues)
					if (!checkedListBox.Items.Contains(strValue))
						checkedListBox.Items.Add(strValue);

				UpdateFilter();
			}
		}

		private void radioButtonManuallyEntered_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButtonManuallyEntered.Checked)
			{
				XPathFilterForm dlg = new XPathFilterForm(FilterXPath);
				dlg.Text = "Enter XPath filter expression";
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					FilterXPath = dlg.FilterExpression;
				}

				checkedListBox.SelectedIndex = -1;
				checkedListBox.Enabled = false;
			}
		}

		private void checkedListBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if ((e.KeyCode == Keys.Delete) && (checkedListBox.SelectedIndex != -1))
			{
				string strItem = (string)checkedListBox.SelectedItem;
				DialogResult res = MessageBox.Show(String.Format("Do you want to delete the following constraint from the list?{0}{0}{1}",
					Environment.NewLine, strItem), XMLViewForm.cstrCaption, MessageBoxButtons.YesNo);

				if (res == DialogResult.Yes)
				{
					checkedListBox.Items.Remove(strItem);
					if (m_astrPreviousConstraints.Contains(strItem))
					{
						m_astrPreviousConstraints.Remove(strItem);
					}
				}
			}
		}

		private void checkedListBox_SelectedValueChanged(object sender, EventArgs e)
		{
			UpdateFilter();
		}

		private void checkedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			e.NewValue = CycleState(e.CurrentValue);
		}

		protected CheckState CycleState(CheckState eCurrent)
		{
			CheckState eNewState = CheckState.Unchecked;
			switch (eCurrent)
			{
				case CheckState.Checked:
					eNewState = CheckState.Indeterminate;
					break;
				case CheckState.Indeterminate:
					eNewState = CheckState.Unchecked;
					break;
				case CheckState.Unchecked:
					eNewState = CheckState.Checked;
					break;
			}
			return eNewState;
		}

		private void checkedListBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				int nIndex = checkedListBox.IndexFromPoint(new Point(e.X, e.Y));
				CheckState eState = CycleState(checkedListBox.GetItemCheckState(nIndex));
				checkedListBox.SetItemCheckState(nIndex, eState);
				eState = CycleState(eState);
				checkedListBox.SetItemCheckState(nIndex, eState);
				UpdateFilter();
			}
		}
	}
}