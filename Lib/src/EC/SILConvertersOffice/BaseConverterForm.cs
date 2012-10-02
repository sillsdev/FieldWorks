using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
	internal enum FormButtons
	{
		None,
		Next,
		ReplaceOnce,
		ReplaceEvery,
		ReplaceAll,
		Cancel,
		Redo
	}

	internal partial class BaseConverterForm : Form
	{
		protected FontConverter m_aFontPlusEC;

		public BaseConverterForm()
		{
			InitializeComponent();
		}

		public virtual FormButtons Show
			(
			FontConverter aFontPlusEC,
			string strInput,
			string strOutput
			)
		{
			m_aFontPlusEC = aFontPlusEC;

			if (m_aFontPlusEC.Font != null)
				this.textBoxInput.Font = m_aFontPlusEC.Font;

			if (m_aFontPlusEC.RhsFont != null)
				this.textBoxConverted.Font = m_aFontPlusEC.RhsFont;

			InputString = strInput;
			ForwardString = strOutput;

			UpdateLhsUniCodes(InputString, this.labelInputCodePoints);
			UpdateRhsUniCodes(ForwardString, this.labelForwardCodePoints);

			// get some info to show in the title bar
			this.Text = String.Format("{0}: {1}", Connect.cstrCaption, m_aFontPlusEC.ToString());

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

		protected void UpdateLegacyCodes(string strInputString, int cp, Label lableUniCodes)
		{
			// to get the real byte values, we need to first convert it using the def code page
			byte[] aby = EncConverters.GetBytesFromEncoding(cp, strInputString, true);
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
			if (m_aFontPlusEC.DirectableEncConverter.IsLhsLegacy)
				UpdateLegacyCodes(strInputString, m_aFontPlusEC.DirectableEncConverter.GetEncConverter.CodePageInput, lableUniCodes);
			else
				UpdateUniCodes(strInputString, lableUniCodes);
		}

		protected void UpdateRhsUniCodes(string strInputString, Label lableUniCodes)
		{
			// use the EncConverter to determine if this field is Legacy or not.
			if (m_aFontPlusEC.DirectableEncConverter.IsRhsLegacy)
				UpdateLegacyCodes(strInputString, m_aFontPlusEC.DirectableEncConverter.GetEncConverter.CodePageOutput, lableUniCodes);
			else
				UpdateUniCodes(strInputString, lableUniCodes);
		}

		protected void buttonNextWord_Click(object sender, EventArgs e)
		{
			ButtonPressed = FormButtons.Next;
			this.Close();
		}

		protected void buttonReplaceOnce_Click(object sender, EventArgs e)
		{
			ButtonPressed = FormButtons.ReplaceOnce;
			this.Close();
		}

		private void buttonReplaceEvery_Click(object sender, EventArgs e)
		{
			ButtonPressed = FormButtons.ReplaceEvery;
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
		protected virtual void RefreshTextBoxes(DirectableEncConverter aEC)
		{
			ForwardString = aEC.Convert(InputString);
		}

		// keep track of the last textbox that was right-click'd in, so we can handle the ChangeFont request
		//  (I'm surprised I can't get that from the 'sender' of the changeFontToolStripMenuItem_Click)
		protected EcTextBox m_tbLastClicked = null;
		private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			ContextMenuStrip aCMS = (ContextMenuStrip)sender;
			m_tbLastClicked = (EcTextBox)aCMS.SourceControl;
			right2LeftToolStripMenuItem.Checked = (m_tbLastClicked.RightToLeft == RightToLeft.Yes);
		}

		protected void SetLhsFont(Font font)
		{
			m_aFontPlusEC.Font = font;
		}

		protected void SetRhsFont(Font font)
		{
			m_aFontPlusEC.RhsFont = font;
		}

		private void changeFontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (m_tbLastClicked != null)
			{
				fontDialog.Font = m_tbLastClicked.Font;
				if (fontDialog.ShowDialog() == DialogResult.OK)
				{
					m_tbLastClicked.Font = fontDialog.Font;

					// we should set this font into the FontConverter member so that we retain it
					//  across invocations of Show
					if (m_tbLastClicked == this.textBoxInput)
						SetLhsFont(fontDialog.Font);
					else
						SetRhsFont(fontDialog.Font);
				}
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
	}
}