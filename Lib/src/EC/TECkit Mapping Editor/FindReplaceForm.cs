using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TECkit_Mapping_Editor
{
	public partial class FindReplaceForm : Form
	{
		protected RichTextBox m_rtb;
		private int m_nCurPos = -1;

		public FindReplaceForm(RichTextBox rtb)
		{
			InitializeComponent();
			m_rtb = rtb;
		}

		public int InsertionPoint
		{
			get { return m_nCurPos; }
			set { m_nCurPos = value; }
		}

		public RichTextBoxFinds MatchCase
		{
			get { return (this.checkBoxMatchCase.Checked) ? RichTextBoxFinds.MatchCase : RichTextBoxFinds.None; }
		}

		public bool IgnoreCase
		{
			get
			{
				return ((MatchCase & RichTextBoxFinds.MatchCase) != RichTextBoxFinds.MatchCase);
			}
		}

		public RichTextBoxFinds WholeWords
		{
			get { return (this.checkBoxWholeWords.Checked) ? RichTextBoxFinds.WholeWord : RichTextBoxFinds.None; }
		}

		public RichTextBoxFinds SearchUp
		{
			get { return (this.checkBoxSearchUp.Checked) ? RichTextBoxFinds.Reverse : RichTextBoxFinds.None; }
		}

		public string FindWhat
		{
			get { return comboBoxFindWhat.Text; }
			set { comboBoxFindWhat.Text = value; }
		}

		public string ReplaceWith
		{
			get { return comboBoxReplaceWith.Text; }
			set { comboBoxReplaceWith.Text = value; }
		}

		public RichTextBoxFinds SearchOptions
		{
			get
			{
				return MatchCase | WholeWords | SearchUp;
			}
		}

		private void buttonFindNext_Click(object sender, EventArgs e)
		{
			CallFindNext();
		}

		internal void CallFindNext()
		{
			int nStart = -1;
			int nEnd = -1;
			if (this.checkBoxSearchUp.Checked)
			{
				nStart = 0;
				nEnd = Math.Max(InsertionPoint - 1, 0);
			}
			else
			{
				nStart = Math.Min(InsertionPoint + 1, m_rtb.TextLength);
				nEnd = m_rtb.TextLength;
			}

			int nIndex = m_rtb.Find(this.comboBoxFindWhat.Text, nStart, nEnd, SearchOptions);
			if (nIndex == -1)
			{
				System.Media.SystemSounds.Asterisk.Play();
				// Console.Beep(); // not found, so beep

				if (this.checkBoxSearchUp.Checked)
					InsertionPoint = m_rtb.Text.Length;
				else
					InsertionPoint = -1;    // start over at the top
			}
			else
			{
				InsertionPoint = nIndex;

				// put this at the head of the combo box
				AddTextToComboBox(comboBoxFindWhat);
				m_rtb.Focus();
			}
		}

		private void FindReplaceForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}

		private void buttonReplace_Click(object sender, EventArgs e)
		{
			if( InsertionPoint > 0 )
			{
				// make sure we can actually find this same spot given the currently selected options
				// (in case the user just turned on 'Whole Words')
				int nLen = comboBoxFindWhat.Text.Length;
				int nIndex = m_rtb.Find(comboBoxFindWhat.Text, InsertionPoint, InsertionPoint + nLen, SearchOptions);
				if (nIndex == InsertionPoint)
				{
					m_rtb.Select(InsertionPoint, comboBoxFindWhat.Text.Length);

					if (String.Compare(m_rtb.SelectedText, comboBoxFindWhat.Text, IgnoreCase) == 0)
					{
						m_rtb.SelectedText = comboBoxReplaceWith.Text;
					}
				}
			}

			// put this at the head of the combo box
			AddTextToComboBox(comboBoxReplaceWith);

			// search for the next occurrence
			buttonFindNext_Click(sender, e);
		}

		private void AddTextToComboBox(ComboBox comboBox)
		{
			// put this at the head of the combo box
			if (comboBox.Items.Contains(comboBox.Text))
				comboBox.Items.Remove(comboBox.Text);
			comboBox.Items.Insert(0, comboBox.Text);
		}

		private void buttonReplaceAll_Click(object sender, EventArgs e)
		{
			bool bFoundSomething = false;
			RichTextBoxFinds options = MatchCase | WholeWords;

			for(int nIndex = m_rtb.Find(comboBoxFindWhat.Text, options);
				nIndex != -1;
				nIndex = m_rtb.Find(comboBoxFindWhat.Text, ++nIndex, options))
			{
				m_rtb.Select(nIndex, comboBoxFindWhat.Text.Length);

				if (String.Compare(m_rtb.SelectedText, comboBoxFindWhat.Text, IgnoreCase) == 0)
				{
					m_rtb.SelectedText = comboBoxReplaceWith.Text;
					bFoundSomething = true;
				}
			}

			if (!bFoundSomething)
				Console.Beep();

			m_rtb.Focus();
		}

		private void FindReplaceForm_Activated(object sender, EventArgs e)
		{
			// put the focus in the FindWhat box when we're activated
			this.comboBoxFindWhat.Focus();
		}

		private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.CallFindNext();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void FindReplaceForm_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				this.Close();
		}
	}
}