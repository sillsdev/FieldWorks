using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using ECInterfaces;
using SilEncConverters40;

namespace SFMConv
{
	public enum FormButtons
	{
		None,
		Next,
		Replace,
		ReplaceAll,
		Cancel,
		Redo
	}

	public partial class BaseConverterForm : Form
	{
		protected DirectableEncConverter m_aEC = null;

		public BaseConverterForm()
		{
			InitializeComponent();
		}

		public virtual FormButtons Show
			(
			string strInput,
			string strOutput,
			DirectableEncConverter aEC,
			Font fontLhs,
			Font fontRhs,
			string strSFM,
			string strDocName,
			bool bShowErrorWarning
			)
		{
			m_aEC = aEC;
			this.textBoxInput.Font = fontLhs;
			this.textBoxConverted.Font = fontRhs;
			this.Text = String.Format("{3}{0}: '{1}' field in {2}",
				SCConvForm.cstrCaption, strSFM, strDocName,
				(bShowErrorWarning) ? "Potential error detected: " : null);

			InputString = strInput;
			ForwardString = strOutput;

			UpdateLhsUniCodes(InputString, this.labelInputCodePoints);
			UpdateRhsUniCodes(ForwardString, this.labelForwardCodePoints);

			ShowDialog();

			return ButtonPressed;
		}

		public string InputString
		{
			get { return this.textBoxInput.Text; }
			set { this.textBoxInput.Text = value; }
		}

		public string ForwardString
		{
			get { return this.textBoxConverted.Text; }
			set { this.textBoxConverted.Text = value; }
		}

		protected FormButtons m_btnPressed = FormButtons.None;
		public FormButtons ButtonPressed
		{
			get { return m_btnPressed; }
			set { m_btnPressed = value; }
		}

		public bool SkipIdenticalValues
		{
			get { return checkBoxSkipIdenticalForms.Checked; }
		}

		public DirectableEncConverter EncConverter
		{
			get { return m_aEC; }
		}

		public Font LhsFont
		{
			get { return textBoxInput.Font; }
		}

		public Font RhsFont
		{
			get { return textBoxConverted.Font; }
		}

		protected void UpdateLegacyCodes(string strInputString, Label lableUniCodes)
		{
			// to get the real byte values, we need to first convert it using the def code page
			byte[] aby = Encoding.Default.GetBytes(strInputString);
			string strWhole = null;
			foreach (byte by in aby)
				strWhole += String.Format("{0:D3} ", (int)by);

			lableUniCodes.Text = strWhole;
		}

		protected void UpdateUniCodes(string strInputString, Label lableUniCodes)
		{
			string strWhole = null;
			foreach (char ch in strInputString)
				strWhole += String.Format("{0:X4} ", (int)ch);

			lableUniCodes.Text = strWhole;
		}

		protected void UpdateLhsUniCodes(string strInputString, Label lableUniCodes)
		{
			// use the EncConverter to determine if this field is Legacy or not.
			if (m_aEC.IsLhsLegacy)
				UpdateLegacyCodes(strInputString, lableUniCodes);
			else
				UpdateUniCodes(strInputString, lableUniCodes);
		}

		protected void UpdateRhsUniCodes(string strInputString, Label lableUniCodes)
		{
			// use the EncConverter to determine if this field is Legacy or not.
			if (m_aEC.IsRhsLegacy)
				UpdateLegacyCodes(strInputString, lableUniCodes);
			else
				UpdateUniCodes(strInputString, lableUniCodes);
		}

		protected void buttonNextWord_Click(object sender, EventArgs e)
		{
			ButtonPressed = FormButtons.Next;
			this.Close();
		}

		protected void buttonReplace_Click(object sender, EventArgs e)
		{
			ButtonPressed = FormButtons.Replace;
			this.Close();
		}

		private void buttonReplaceAll_Click(object sender, EventArgs e)
		{
			ButtonPressed = FormButtons.ReplaceAll;
			this.Close();
		}

		protected void buttonCancel_Click(object sender, EventArgs e)
		{
			ButtonPressed = FormButtons.Cancel;
			Close();
		}

		protected void textBoxInput_TextChanged(object sender, EventArgs e)
		{
			UpdateLhsUniCodes(InputString, this.labelInputCodePoints);
		}

		void textBoxConverted_TextChanged(object sender, System.EventArgs e)
		{
			UpdateRhsUniCodes(ForwardString, this.labelForwardCodePoints);
		}

		// allow these to be overidden by sub-class forms (e.g. to add a round-trip refresh as well)
		protected virtual void RefreshTextBoxes(IEncConverter aEC)
		{
			aEC.DirectionForward = true;
			ForwardString = aEC.Convert(InputString);
		}

		// keep track of the last textbox that was right-click'd in, so we can handle the ChangeFont request
		//  (I'm surprised I can't get that from the 'sender' of the changeFontToolStripMenuItem_Click)
		protected TextBox m_tbLastClicked = null;
		private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			ContextMenuStrip aCMS = (ContextMenuStrip)sender;
			m_tbLastClicked = (TextBox)aCMS.SourceControl;
			right2LeftToolStripMenuItem.Checked = (m_tbLastClicked.RightToLeft == RightToLeft.Yes);
		}

		private void changeFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				fontDialog.Font = m_tbLastClicked.Font;
				if (fontDialog.ShowDialog() == DialogResult.OK)
					m_tbLastClicked.Font = fontDialog.Font;
			}
		}

		private void right2LeftToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				ToolStripMenuItem aMenuItem = (ToolStripMenuItem)sender;
				m_tbLastClicked.RightToLeft = (aMenuItem.Checked) ? RightToLeft.Yes : RightToLeft.No;
			}
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.Undo();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				if (m_tbLastClicked.SelectionLength == 0)
					m_tbLastClicked.SelectAll();
				m_tbLastClicked.Cut();
			}
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				if (m_tbLastClicked.SelectionLength == 0)
					m_tbLastClicked.SelectAll();
				m_tbLastClicked.Copy();
			}
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.Paste();
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.Clear();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
				m_tbLastClicked.SelectAll();
		}

		private void buttonDebug_Click(object sender, EventArgs e)
		{
			IEncConverter aEC = m_aEC.GetEncConverter;
			bool bOrigValue = aEC.Debug;
			aEC.Debug = true;

			RefreshTextBoxes(aEC);

			aEC.Debug = bOrigValue;
		}

		private void buttonRefresh_Click(object sender, EventArgs e)
		{
			RefreshTextBoxes(m_aEC.GetEncConverter);
		}

		private void BaseConverterForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			// if the user clicks the 'x' in the corner, treat that as Cancel
			if (ButtonPressed == FormButtons.None)
				ButtonPressed = FormButtons.Cancel;

			e.Cancel = true;
			this.Hide();
		}
	}
}