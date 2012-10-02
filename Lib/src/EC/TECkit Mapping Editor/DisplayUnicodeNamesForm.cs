using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters40;
using System.Diagnostics;               // Debug
using System.Runtime.InteropServices;   // DLLImport
using System.Resources;                 // for ResourceManager

namespace TECkit_Mapping_Editor
{
	public partial class DisplayUnicodeNamesForm : Form
	{
		const string cstrStartingRange = "0000-007F";

		protected enum SendToEditorForms
		{
			eDecimal,
			eHexadecimal,
			eQuotedString,
			eUnicodeValues,
			eUnicodeName
		}

		protected bool m_bIsLhs = false;

		public DisplayUnicodeNamesForm(bool bIsLhs)
		{
			InitializeComponent();
			this.dataGridViewCharacters.Dock = DockStyle.Fill;
			SetHelpProvider();
			m_bIsLhs = bIsLhs;
		}

		protected void SetHelpProvider()
		{
			helpProviderCP.SetHelpString(this.dataGridViewCharacters, Properties.Resources.CPDataGridCharacters);
			helpProviderCP.SetHelpString(groupBoxSendToEditor, Properties.Resources.SendToEditorHelpString);
			helpProviderCP.SetHelpString(comboBoxCodePointRange, Properties.Resources.ComboUnicodeRangeHelpString);
			helpProviderCP.SetHelpString(radioButtonUnicodeSubsets, Properties.Resources.UnicodeRangesHelpString);
		}

		protected bool m_bIsLegacy = false;
		protected Font m_font = null;
		protected Button m_btnZoom = null;
		protected int m_nCodePageLegacy = 0;

		public void Initialize(bool bLegacy, Font font, int nCodePageLegacy)
		{
			if (!Visible)
				Show();

			ResetCharMap(ref m_btnZoom, dataGridViewCharacters);

			m_nCodePageLegacy = nCodePageLegacy;
			m_bIsLegacy = bLegacy;
			m_font = font;
			labelFontName.Text = font.Name;

			if (m_bIsLegacy)
			{
				radioButtonUnicodeValues.Visible = radioButtonUnicodeNames.Visible =
					radioButtonQuotedChars.Visible = false;
				radioButtonDecimal.Checked = true;

				int nBeginningCharacterIndex = 0;
				if (m_nCodePageLegacy == 0)
					m_nCodePageLegacy = 1252; // most likely for a hacked legacy encoding
				else if (m_nCodePageLegacy == EncConverters.cnSymbolFontCodePage)
					nBeginningCharacterIndex = 0xF000;
				InitializeCharMap(m_font, dataGridViewCharacters, nBeginningCharacterIndex, 256, m_nCodePageLegacy, bLegacy);
			}
			else
			{
				if (cstrStartingRange == (string)comboBoxCodePointRange.SelectedItem)
					comboBoxCodePointRange.SelectedIndex = -1;

				radioButtonByCodePoint.Checked = true;
				comboBoxCodePointRange.SelectedItem = cstrStartingRange;   // this triggers the InitializeCharMap
				radioButtonDecimal.Visible = radioButtonHexadecimal.Visible = false;
				groupBoxUnicodeRanges.Visible = flowLayoutPanelRecentRanges.Visible =
					comboBoxCodePointRange.Visible = flowLayoutPanelChooseByRange.Visible = true;
				radioButtonUnicodeNames.Checked = radioButtonByCodePoint.Checked = true;
			}
		}

		protected void ResetCharMap(ref Button btnZoom, DataGridView dataGridViewCharacters)
		{
			RemoveZoom(ref btnZoom);

			// clear the grid's tooltip, so the character tooltip shows up (without the other)
			toolTip.SetToolTip(dataGridViewCharacters, null);

			// remove previous (possible) contents
			dataGridViewCharacters.Rows.Clear();
		}

		internal unsafe void InitializeCharMap(Font font, MyDataGridView grid, int nBeginningCharacterIndex, int nRangeLength, int nCodePage, bool bIsLegacy)
		{
			grid.DefaultCellStyle.Font = font;

			// for Legacy fonts, we don't do column and row headers and we do 256 chars
			const int cnGridItems = 16; // both row and column count
			int nNumRows = (nRangeLength + cnGridItems - 1) / cnGridItems;
			if (bIsLegacy)
				grid.ColumnHeadersVisible = grid.RowHeadersVisible = false;

			char[] achCharValues, achGlyIdxes;
			GetAllPossibleGlyphIndices(grid, font, nBeginningCharacterIndex, nRangeLength, nCodePage, bIsLegacy, out achCharValues, out achGlyIdxes);

			int nCharIndex = 0, nMaxIndex = Math.Min(achCharValues.Length, achGlyIdxes.Length);
			int jInit = nBeginningCharacterIndex % cnGridItems; // first time thru, skip to the correct character
			for (int i = 0; (i < nNumRows) && (nCharIndex < nMaxIndex); i++)
			{
				int nRowIndex = -1;
				DataGridViewRow theRow = null;
				if (!bIsLegacy)
				{
					nRowIndex = grid.Rows.Add();
					theRow = grid.Rows[nRowIndex];
					theRow.HeaderCell.Value = String.Format("{0:X4}", ((nBeginningCharacterIndex / cnGridItems) * cnGridItems) + (i * cnGridItems));
				}

				for (int j = jInit; j < cnGridItems; j++ )
				{
					nCharIndex = (i * cnGridItems) + j - jInit;
					if (nCharIndex < nMaxIndex)
					{
						// don't bother displaying anything if there isn't a real glyph
						char chGlyphIndex = achGlyIdxes[nCharIndex];
						if (chGlyphIndex != 0xFFFF)
						{
							// delay creating rows until they're needed (so we can skip the first two)
							//  unless we're displaying Unicode, in which case, we *have* to show all rows
							if (nRowIndex == -1)
							{
								nRowIndex = grid.Rows.Add();
								theRow = grid.Rows[nRowIndex];
							}

							// the code point for this character/glyph *is* the character index
							char ch = achCharValues[nCharIndex];    // Convert.ToChar(nCharIndex);
							DataGridViewCell aCell = theRow.Cells[j];
							aCell.Value = ch;
						}
					}
					else
						break;  // break out of the loop, because we're done.
				}

				jInit = 0;  // start at 0 on subsequent rows
			}
		}

		// calls to determine whether a glyph actually exists at certain code points
		//  1) set DataGrid to use the font in question (probably unnecessary)
		//  2) get hWnd of that DG
		//  3) get the DC associated with the DG
		//  4) get the hFont associated with the font
		//  5) SelectObject of the hFont into the DC
		//  6) call GetGlyphIndicies passing a string with all possible code points
		//     this returns an array of chars giving the index of the glyphs or 0xFFFF if none
		//  7) SelectObject on the original font
		//  8) Release the DC
		[DllImport("user32.dll", EntryPoint = "GetDC")]
		public static extern IntPtr GetDC(IntPtr ptr);

		[DllImport("user32.dll", EntryPoint = "ReleaseDC")]
		public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

		[DllImport("gdi32.dll", EntryPoint = "SelectObject")]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hFont);

		[DllImport("gdi32.dll")]
		public static extern unsafe int GetGlyphIndicesW(
			IntPtr hDc,
			char* pTxt,
			Int32 len,
			char* pGlyIdx,
			Int32 fl);

		[DllImport("gdi32.dll")]
		public static extern unsafe int GetGlyphIndicesA(
			IntPtr hDc,
			byte* pTxt,
			Int32 len,
			char* pGlyIdx,
			Int32 fl);

		const Int32 GGI_MARK_NONEXISTING_GLYPHS = 0X0001;

		internal unsafe void GetAllPossibleGlyphIndices(MyDataGridView grid, Font font, int nBeginningCharacterIndex, int nNumPossibleChars,
			int nCodePage, bool bIsLegacy, out char[] achFullPossibleChars, out char[] achGlyIdxes)
		{
			IntPtr hWnd = IntPtr.Zero;
			IntPtr hFont = IntPtr.Zero;
			IntPtr hDC = IntPtr.Zero;
			IntPtr hOrigFont = IntPtr.Zero;
			int nCount = 0;

			try
			{
				hWnd = grid.Handle;
				hFont = font.ToHfont();
				hDC = GetDC(hWnd);
				hOrigFont = SelectObject(hDC, hFont);

				char* pGlyIdx = stackalloc char[nNumPossibleChars];

				// legacy (but not symbol) fonts behave differently
				if (bIsLegacy && (nCodePage != EncConverters.cnSymbolFontCodePage))
				{
					// fill a byte array with all possible ansi code points and then turn that into chars via
					//  the default system code page.
					byte[] abyCharValues = new byte[nNumPossibleChars];
					for (int i = 0; i < nNumPossibleChars; i++)
						abyCharValues[i] = (byte)i;

					fixed (byte* pTxt = abyCharValues)
					{
						nCount = GetGlyphIndicesA(hDC, pTxt, nNumPossibleChars, pGlyIdx, GGI_MARK_NONEXISTING_GLYPHS);
					}

					achFullPossibleChars = Encoding.GetEncoding(nCodePage).GetChars(abyCharValues);
				}
				else
				{
					achFullPossibleChars = new char[nNumPossibleChars];
					for (int i = 0; i < nNumPossibleChars; i++)
						achFullPossibleChars[i] = (char)(nBeginningCharacterIndex + i);

					fixed (char* pTxt = achFullPossibleChars)
					{
						nCount = GetGlyphIndicesW(hDC, pTxt, nNumPossibleChars, pGlyIdx, GGI_MARK_NONEXISTING_GLYPHS);
					}
				}

				achGlyIdxes = new char[nCount];
				for (int i = 0; i < nCount; i++)
					achGlyIdxes[i] = pGlyIdx[i];
			}
			finally
			{
				// release what we got
				SelectObject(hDC, hOrigFont);
				ReleaseDC(hWnd, hDC);
			}
		}

		private MouseButtons m_mbButtonDown = MouseButtons.None;

		private void dataGridViewCharacters_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
		{
			OnCellMouseDown(dataGridViewCharacters, ref m_btnZoom, m_font, tableLayoutPanel, OutputForm, m_bIsLegacy, e);
		}

		protected SendToEditorForms OutputForm
		{
			get
			{
				SendToEditorForms eForm;
				if (m_bIsLegacy)
				{
					// this means that there are two radio buttons: Decimal or Hexadecimal
					System.Diagnostics.Debug.Assert((radioButtonDecimal != null) && (radioButtonHexadecimal != null));
					if (radioButtonHexadecimal.Checked)
						eForm = SendToEditorForms.eHexadecimal;
					else
						eForm = SendToEditorForms.eDecimal;
				}
				else
				{
					// this means that there are three radio buttons: Unicode Names, Unicode Values, or Quoted Strings
					System.Diagnostics.Debug.Assert((radioButtonUnicodeNames != null) && (radioButtonUnicodeValues != null) && (radioButtonQuotedChars != null));
					if (radioButtonQuotedChars.Checked)
						eForm = SendToEditorForms.eQuotedString;
					else if (radioButtonUnicodeValues.Checked)
						eForm = SendToEditorForms.eUnicodeValues;
					else
						eForm = SendToEditorForms.eUnicodeName;
				}
				return eForm;
			}
		}

		private void OnCellMouseDown(MyDataGridView dataGridViewCharacters, ref Button btnZoom, Font font, TableLayoutPanel ZoomButtonParent,
			SendToEditorForms eOutputForm, bool bIsLegacy, DataGridViewCellMouseEventArgs e)
		{
			if ((e.RowIndex >= 0) && (e.ColumnIndex >= 0))
			{
				m_mbButtonDown = e.Button;

				DataGridViewCell aCell = dataGridViewCharacters.Rows[e.RowIndex].Cells[e.ColumnIndex];
				if (m_mbButtonDown == MouseButtons.Right)
				{
					// simply selecting the cell will trigger the zoom button update
					//  (but only if it's already activated)
					if (btnZoom == null)
						ShowZoomButton(ref btnZoom, font, ZoomButtonParent, eOutputForm, bIsLegacy, aCell);
					else if (!btnZoom.Visible)
						btnZoom.Show();

					// either way, select the cell
					aCell.Selected = true;
				}
			}
		}

		private void dataGridViewCharacters_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
		{
			if ((e.RowIndex >= 0) && (e.ColumnIndex >= 0))
			{
				// another short-cut: if the Ctrl key is pressed, then do the same as a
				//  double-right-click (which is otherwise a bit cumbersome)
				DataGridViewCell aCell = dataGridViewCharacters.Rows[e.RowIndex].Cells[e.ColumnIndex];
				if (ModifierKeys == Keys.Control)
					SendToSampleBox(dataGridViewCharacters, e);
				else if ((m_mbButtonDown == MouseButtons.Left) && (aCell.Value != null))
					SendToEditor((char)aCell.Value, OutputForm);
			}

			m_mbButtonDown = MouseButtons.None;
		}

		private void dataGridViewCharacters_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
		{
			OnCellMouseLeave((DataGridView)sender, OutputForm, e);
		}

		private void OnCellMouseLeave(DataGridView dataGridViewCharacters, SendToEditorForms eOutputForm, DataGridViewCellEventArgs e)
		{
			if ((e.RowIndex >= 0) && (e.ColumnIndex >= 0))
			{
				object oValue = dataGridViewCharacters.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
				if ((m_mbButtonDown == MouseButtons.Left) && (oValue != null))
				{
					char ch = (char)dataGridViewCharacters.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
					string str = GetStringForEditorInsertion(ch, eOutputForm);
					DoDragDrop(str, DragDropEffects.Copy);
					m_mbButtonDown = MouseButtons.None;  // so it doesn't leave us in a weird state
				}
			}
		}

		protected void RemoveZoom(ref Button btn)
		{
			if (btn != null)
			{
				btn.Hide();
				btn.Dispose();
				btn = null;    // reset zoom window
			}
		}

		private void ShowZoomButton(ref Button btnZoom, Font font, TableLayoutPanel ZoomButtonParent,
			SendToEditorForms eOutputForm, bool bIsLegacy, DataGridViewCell aCell)
		{
			if (aCell.Value == null)
				return;

			ZoomButtonParent.SuspendLayout();
			if (btnZoom == null)
			{
				btnZoom = new Button();
				Font fontLarge = Program.GetSafeFont(font.Name, font.SizeInPoints * 3);
				btnZoom.Font = fontLarge;
				btnZoom.Anchor = AnchorStyles.None;
				btnZoom.BackColor = System.Drawing.SystemColors.Menu;
				btnZoom.MouseDown += new MouseEventHandler(btnZoom_MouseDown);
				btnZoom.Tag = eOutputForm;
				ZoomButtonParent.Controls.Add(btnZoom, 2, 0);
				ZoomButtonParent.SetRowSpan(btnZoom, 2);
			}

			// initialize it with the space padded character and then get the preferred size
			//  (pad with a space on the left to correctly display 'zero forward offset' glyphs
			//  and on the right, so it's centered and looks nicer).
			btnZoom.Text = String.Format("{0}", aCell.Value.ToString());
			Size sz = btnZoom.GetPreferredSize(btnZoom.Size);
			btnZoom.Size = new Size(sz.Width, sz.Height);
			ZoomButtonParent.ResumeLayout();
			ZoomButtonParent.PerformLayout();

			// then fill it with the details from the clicked character
			string strTooltip = aCell.ToolTipText;
			int nIndexDelimiter = strTooltip.LastIndexOf(';');
			strTooltip = GetCellToolTip(aCell, eOutputForm, bIsLegacy) + "; Click to insert in map";
			toolTip.SetToolTip(btnZoom, strTooltip);
			return;
		}

		void btnZoom_MouseDown(object sender, MouseEventArgs e)
		{
			Button btnZoom = (Button)sender;
			char ch = btnZoom.Text[0];

			if (ModifierKeys == Keys.Control)
				Program.AddCharToSampleBox(ch, m_bIsLhs);

			else if (e.Button == MouseButtons.Left)
				SendToEditor(ch, (SendToEditorForms)btnZoom.Tag);
			else
				btnZoom.Hide();
		}

		protected byte[] CharToByteArr(char ch, int nCodePageLegacy)
		{
			char[] ach = new char[1] { ch };
			if (nCodePageLegacy == EncConverters.cnSymbolFontCodePage)
			{
				byte[] aby = new byte[1];
				aby[0] = (byte)(ch & 0xFF);
				return aby;
			}
			else
				return Encoding.GetEncoding(nCodePageLegacy).GetBytes(ach);
		}

		private string GetStringForEditorInsertion(char ch, SendToEditorForms eOutputForm)
		{
			string str = null;
			switch (eOutputForm)
			{
				case SendToEditorForms.eDecimal:
					byte[] abyDec = CharToByteArr(ch, m_nCodePageLegacy);
					foreach (byte byDec in abyDec)
						str += String.Format("{0:D} ", (int)byDec);
					str = str.Substring(0, str.Length - 1);
					break;

				case SendToEditorForms.eHexadecimal:
					byte[] abyHex = CharToByteArr(ch, m_nCodePageLegacy);
					foreach (byte byHex in abyHex)
						str += String.Format("0x{0:X2} ", (int)byHex);
					str = str.Substring(0, str.Length - 1);
					break;

				case SendToEditorForms.eUnicodeValues:
					str = String.Format("U+{0:X4}", (int)ch);
					break;

				case SendToEditorForms.eQuotedString:
					str = String.Format("\"{0}\"", ch);
					break;

				case SendToEditorForms.eUnicodeName:
					str = Program.GetUnicodeName(ch);
					break;

				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}

			return str;
		}

		private void SendToEditor(char ch, SendToEditorForms eOutputForm)
		{
			Program.AddStringToEditor(GetStringForEditorInsertion(ch, eOutputForm));
		}

		private void DisplayUnicodeNamesForm_ResizeEnd(object sender, EventArgs e)
		{
			/*
			// manually (since I can't figure out how to make it work automatically) set
			//  the position of the character map after resizing
			if (!dataGridViewCharactersLhs.IsVerticalScrollBarVisible)
				AdjustLayoutRow();
			*/

			Program.SetBoundsClue((m_bIsLhs) ? TECkitMapEditorForm.cstrCodePointFormClueLhs : TECkitMapEditorForm.cstrCodePointFormClueRhs, Bounds);
		}

		/*
		private void AdjustLayoutRow()
		{
			int nTotalHeight = 0;
			foreach (DataGridViewRow aRow in dataGridViewCharactersLhs.Rows)
				nTotalHeight += aRow.Height;

			RowStyle aRowStyle = tableLayoutPanelRhs.RowStyles[2];
			aRowStyle.SizeType = SizeType.Absolute;
			aRowStyle.Height = 5 + nTotalHeight + dataGridViewCharactersLhs.Margin.Vertical + toolStripStatusLabel.Height;

			aRowStyle = tableLayoutPanelRhs.RowStyles[1];
			aRowStyle.SizeType = SizeType.Percent;
			aRowStyle.Height = 100;
			tableLayoutPanelRhs.PerformLayout();
		}
		*/

		private void dataGridViewCharacters_SelectionChanged(object sender, EventArgs e)
		{
			OnSelectionChanged((DataGridView)sender, ref m_btnZoom, m_font, tableLayoutPanel, OutputForm, m_bIsLegacy);
		}

		private void OnSelectionChanged(DataGridView dataGridViewCharacters, ref Button btnZoom, Font font,
			TableLayoutPanel ZoomButtonParent, SendToEditorForms eOutputForm, bool bIsLegacy)
		{
			if (dataGridViewCharacters.SelectedCells.Count > 0)
			{
				DataGridViewCell aCell = dataGridViewCharacters.SelectedCells[0];
				this.toolStripStatusLabel.Text = GetCellToolTip(aCell, eOutputForm, bIsLegacy);

				if ((btnZoom != null) && btnZoom.Visible)
					ShowZoomButton(ref btnZoom, font, ZoomButtonParent, eOutputForm, bIsLegacy, aCell);
			}
		}

		private void dataGridViewCharacters_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
		{
			OnCellMouseEnter((DataGridView)sender, e);
		}

		private void OnCellMouseEnter(DataGridView dataGridViewCharacters, DataGridViewCellEventArgs e)
		{
			if ((e.RowIndex >= 0) && (e.ColumnIndex >= 0))
			{
				if (m_mbButtonDown == MouseButtons.Right)
				{
					// selecting it will trigger the re-show of the zoom button and updating the status bar
					DataGridViewCell aCell = dataGridViewCharacters.Rows[e.RowIndex].Cells[e.ColumnIndex];
					aCell.Selected = true;
				}
			}
		}

		private string GetCellToolTip(DataGridViewCell aCell, SendToEditorForms eOutputForm, bool bIsLegacy)
		{
			if (aCell.Value != null)
			{
				char ch = (char)aCell.Value;
				string str;
				if (bIsLegacy)
				{
					str = String.Format("char: {0}, dec: \"{1}\", hex: \"{2}\"",
						GetStringForEditorInsertion(ch, SendToEditorForms.eQuotedString),
						GetStringForEditorInsertion(ch, SendToEditorForms.eDecimal),
						GetStringForEditorInsertion(ch, SendToEditorForms.eHexadecimal));
				}
				else
				{
					str = String.Format("char: {0}, name: \"{1}\", value: \"{2}\"",
						GetStringForEditorInsertion(ch, SendToEditorForms.eQuotedString),
						GetStringForEditorInsertion(ch, SendToEditorForms.eUnicodeName),
						GetStringForEditorInsertion(ch, SendToEditorForms.eUnicodeValues));
				}
				return str;
			}
			else
				return null;
		}

		private void dataGridViewCharacters_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if ((e.RowIndex >= 0) && (e.ColumnIndex >= 0))
			{
				SendToSampleBox((DataGridView)sender, e);
			}
		}

		private void SendToSampleBox(DataGridView dataGridViewCharacters, DataGridViewCellMouseEventArgs e)
		{
			// if the grid is right-double-clicked, then interpret that as send it to the sample box
			DataGridViewCell aCell = dataGridViewCharacters.Rows[e.RowIndex].Cells[e.ColumnIndex];
			if (aCell.Value != null)
				Program.AddCharToSampleBox((char)aCell.Value, m_bIsLhs);
		}

		private void DisplayUnicodeNamesForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}

		private void comboBoxCodePointRange_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			ComboBox cbCodePointRange = (ComboBox)sender;
			OnCbSelectedIndexChanged(cbCodePointRange, dataGridViewCharacters, ref m_btnZoom, m_font, m_bIsLegacy);
		}

		private void OnCbSelectedIndexChanged(ComboBox cbCodePointRange, MyDataGridView dataGridViewCharacters,
			ref Button btnZoom, Font font, bool bIsLegacy)
		{
			string strValue = (string)cbCodePointRange.SelectedItem;
			if (String.IsNullOrEmpty(strValue))
				return;

			ResetCharMap(ref btnZoom, dataGridViewCharacters);

			RadioButton btnToCheck;
			UnicodeSubset aUS;
			if ((m_aUSM != null) && m_aUSM.TryGetValue(strValue, out aUS))
			{
				btnToCheck = radioButtonUnicodeSubsets;
				foreach (KeyValuePair<int, int> aRangeLength in aUS)
					InitializeCharMap(font, dataGridViewCharacters, aRangeLength.Key, aRangeLength.Value, 0, bIsLegacy);
			}
			else if (radioButtonByCodePoint.Checked)
			{
				btnToCheck = radioButtonByCodePoint;
				int nBeginningIndex = Convert.ToInt32(strValue.Substring(0, 4), 16);
				InitializeCharMap(font, dataGridViewCharacters, nBeginningIndex, 0x80, 0, bIsLegacy);
			}
			else
				return;

			// first see if this range has been added to our list of recently used ranges
			foreach (RadioButton btnUnicodeRange in flowLayoutPanelRecentRanges.Controls)
				if (btnUnicodeRange.Text == strValue)
				{
					flowLayoutPanelRecentRanges.Controls.SetChildIndex(btnUnicodeRange, 0);
					btnUnicodeRange.Checked = true;
					return; // found, so just return
				}

			// otherwise, add a radio button for this range so we can more easily go back to it
			RadioButton btn = new RadioButton();
			btn.AutoSize = true;
			btn.Name = "radioButton" + strValue;
			btn.TabStop = true;
			btn.Text = strValue;
			btn.UseVisualStyleBackColor = true;
			btn.Click += new EventHandler(btnUnicodeRange_Click);

			flowLayoutPanelRecentRanges.Controls.Add(btn);
			flowLayoutPanelRecentRanges.Controls.SetChildIndex(btn, 0);
			btn.Checked = true;
			btn.Tag = btnToCheck;
		}

		private void btnUnicodeRange_Click(object sender, EventArgs e)
		{
			RadioButton btn = (RadioButton)sender;
			if (btn.Text != (string)comboBoxCodePointRange.SelectedItem)
			{
				RadioButton btnToCheck = (RadioButton)btn.Tag;
				btnToCheck.Checked = true;
				comboBoxCodePointRange.SelectedItem = btn.Text;
			}
		}
	}
}