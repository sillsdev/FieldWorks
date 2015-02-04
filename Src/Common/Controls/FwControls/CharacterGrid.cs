// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CharacterGrid.cs
// Responsibility: TE Team

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CharacterGrid : DataGridView, IFWDisposable
	{
#if !__MonoCS__
		[DllImport("gdi32.dll", CharSet=CharSet.Auto)]
		private static extern uint GetGlyphIndices(IntPtr hdc, string lpstr, int c,
			[In, Out] ushort[] pgi, int fl);
#else
		private static uint GetGlyphIndices(IntPtr hdc, string lpstr, int c, [In, Out] ushort[] pgi, int fl)
		{
			throw new NotImplementedException();
		}
#endif

#if !__MonoCS__
		[DllImport("gdi32.dll", EntryPoint="SelectObject")]
		private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hfont);
#else
		private static IntPtr SelectObject(IntPtr hdc, IntPtr hfont)
		{
			throw new NotImplementedException();
		}
#endif

		/// <summary>Handler for character changed event.</summary>
		public delegate void CharacterChangedHandler(CharacterGrid grid, string newCharacter);

		/// <summary>Event fired after the selected character in the grid changes.</summary>
		public event CharacterChangedHandler CharacterChanged;

		#region Data members
		private const int kFirstChar = 32;

		private int m_cellWidth = 40;
		private int m_cellHeight = 45;
		private bool m_loadCharactersFromFont = true;
		private ILgCharacterPropertyEngine m_cpe;
		private Font m_fntForSpecialChar;
		private CharacterInfoToolTip m_toolTip;
		private IComparer m_sortComparer = null;

		/// <summary>Stores the characters to be displayed. Each "character" should be a
		/// single base character, followed by zero or more combining characters.</summary>
		private List<string> m_chars;

		private bool m_fSymbolCharSet = false;
		private readonly Dictionary<string, string> m_specialCharStrings = new Dictionary<string, string>();
		private readonly List<string> m_charsWithMissingGlyphs = new List<string>();
		#endregion

		#region Construction, Initialization, and Disposal
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterGrid"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="Added a TODO-Linux comment")]
		public CharacterGrid()
		{
			DoubleBuffered = true;
			AllowUserToAddRows = false;
			AllowUserToDeleteRows = false;
			AllowUserToResizeColumns = false;
			AllowUserToResizeRows = false;
			MultiSelect = false;
			ReadOnly = true;
			ColumnHeadersVisible = false;
			RowHeadersVisible = false;
			VirtualMode = true; // TODO-Linux: VirtualMode is not supported in Mono
			ShowCellToolTips = false;
			BorderStyle = BorderStyle.None;
			DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			BackColor = SystemColors.Window;
			Name = "CharacterGrid";

			m_fntForSpecialChar = new Font(SystemFonts.IconTitleFont.FontFamily, 8f);
			m_toolTip = new CharacterInfoToolTip();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (m_fntForSpecialChar != null)
					m_fntForSpecialChar.Dispose();
				if (m_toolTip != null)
					m_toolTip.Dispose();
			}
					m_fntForSpecialChar = null;
			m_toolTip = null;

			base.Dispose( disposing );
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the writing system to use for sorting purposes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public WritingSystem WritingSystem
		{
			set { m_sortComparer = (value == null ? null : new TsStringComparer(value)); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image used in the tooltip to indicate the character doesn't not have a
		/// corresponding glyph in the font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public static Image MissingGlyphImage
		{
			get { return Properties.Resources.kimidMissingGlyph; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font showing in the grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new Font Font
		{
			get
			{
				CheckDisposed();
				return base.Font;
			}
			set
			{
				CheckDisposed();
				if (base.Font != value)
				{
					base.Font = value;
					LogicalFont logFont = new LogicalFont(value);
					m_fSymbolCharSet = logFont.IsSymbolCharSet;
					m_chars = null;
					if (IsHandleCreated)
						LoadGrid();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width (in pixels) of each cell in which a character is displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CellWidth
		{
			get
			{
				CheckDisposed();
				return m_cellWidth;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height (in pixels) of each cell in which a character is displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CellHeight
		{
			get
			{
				CheckDisposed();
				return m_cellHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the character grid should be loaded with
		/// characters from the Font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(true)]
		[DefaultValue(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public bool LoadCharactersFromFont
		{
			get
			{
				CheckDisposed();
				return m_loadCharactersFromFont;
			}
			set
			{
				CheckDisposed();
				m_loadCharactersFromFont = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current character in the grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string CurrentCharacter
		{
			get
			{
				CheckDisposed();
				int index = GetListIndexFromCellAddress();
				return (index >= 0 && index < Chars.Count ? Chars[index] : string.Empty);
			}
			set
			{
				CheckDisposed();
				for (int i = 0; i < Chars.Count; i++)
				{
					if (value == Chars[i])
					{
						Point pt = GetCellAddressFromListIndex(i);
						if (pt.X >= 0 && pt.Y >= 0)
							CurrentCell = this[pt.X, pt.Y];

						return;
					}
				}

				if (RowCount > 0 && ColumnCount > 0)
					CurrentCell = this[0, 0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the count of characters displayed in the grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private int NumberOfChars
		{
			get
			{
				CheckDisposed();
				return Chars.Count;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of columns in the grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int NumberOfColumns
		{
			get
			{
				CheckDisposed();
				return (int)Math.Floor((double)UsableWidth / m_cellWidth);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of rows in the grid (based on the number of columns in the grid and
		/// the number of characters in the font).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int NumberOfRows
		{
			get
			{
				CheckDisposed();
				int rows = (int)Math.Ceiling((double)NumberOfChars / NumberOfColumns);
				return (rows > 0 ? rows : 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private int UsableWidth
		{
			get
			{
				CheckDisposed();
				return ClientSize.Width; // -SystemInformation.VerticalScrollBarWidth - 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ILgCharacterPropertyEngine used when loading the grid from a font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ILgCharacterPropertyEngine CharPropEngine
		{
			get { CheckDisposed(); return m_cpe; }
			set
			{
				CheckDisposed();
				if (m_cpe != value)
				{
					m_cpe = value;
					if (IsHandleCreated)
					{
						RemoveAllCharacters();
						LoadGrid();
					}
				}
			}
		}

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a list of characters to display. Each "character" should be a single base
		/// character, followed by zero or more combining characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddCharacters(IEnumerable<string> value)
		{
			CheckDisposed();
			Debug.Assert(!m_loadCharactersFromFont);

			foreach (string ch in value)
				InternalAddCharacter(ch);

			if (m_sortComparer != null)
				Chars.Sort(m_sortComparer.Compare);
			else
				Chars.Sort();

			LoadGrid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified character to the grid's list of displayed characters. This is
		/// called by the two public methods AddCharacter and AddCharacters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InternalAddCharacter(string chr)
		{
			// Keep track of those characters that are not control characters and for
			// which there are no representative glyphs in the font.
			if (Win32.AreCharGlyphsInFont(chr, Font))
			{
				if (m_charsWithMissingGlyphs.Contains(chr))
					m_charsWithMissingGlyphs.Remove(chr);
			}
			else if (!Icu.IsControl(chr[0]))
			{
				if (!m_charsWithMissingGlyphs.Contains(chr))
					m_charsWithMissingGlyphs.Add(chr);
			}

			// If we're dealing with a type of space or control character, then figure out
			// display text that is slightly more readable than whatever glyph the font
			// contains for the character, which may not be a glyph at all.
			if ((Icu.IsSpace(chr[0]) || Icu.IsControl(chr[0])) &&
				!m_specialCharStrings.ContainsKey(chr))
			{
				m_specialCharStrings[chr] = GetSpecialCharDisplayText(chr);
			}

			Chars.Add(chr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the display name for the specified space or control character. If no display
		/// name is found, then the U+ value is retured.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetSpecialCharDisplayText(string chr)
		{
			if (string.IsNullOrEmpty(chr))
				return null;

			string resName = string.Format("kstidSpecialChar{0:X4}", (int)chr[0]);
			string displayText = Properties.Resources.ResourceManager.GetString(resName);

			return (string.IsNullOrEmpty(displayText) ?
				string.Format("U+{0:X4}", (int)chr[0]) : displayText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all characters. If characters are loaded from font, they will be readded
		/// automatically when the control is repainted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveAllCharacters()
		{
			Columns.Clear();
			RowCount = 0;
			m_chars = null;
			m_charsWithMissingGlyphs.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the current character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveCurrentCharacter()
		{
			int index = GetListIndexFromCellAddress();
			if (index > 0)
			{
				if (m_charsWithMissingGlyphs.Contains(Chars[index]))
					m_charsWithMissingGlyphs.Remove(Chars[index]);

				Chars.RemoveAt(index);
				LoadGrid();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified character from the grid.
		/// </summary>
		/// <param name="chr">The character to remove.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveCharacter(string chr)
		{
			if (Chars.Contains(chr))
			{
				if (m_charsWithMissingGlyphs.Contains(chr))
					m_charsWithMissingGlyphs.Remove(chr);

				Chars.Remove(chr);
				LoadGrid();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Removes the selected characters from the grid and returns a list of those that
		/// were removed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public List<string> RemoveSelectedCharacters()
		{
			if (SelectedCells.Count == 0)
				return null;

			List<string> chrsToRemove = new List<string>();

			foreach (DataGridViewCell cell in SelectedCells)
				chrsToRemove.Add(GetCharacterAt(cell.ColumnIndex, cell.RowIndex));

			foreach (string chr in chrsToRemove)
			{
				if (m_charsWithMissingGlyphs.Contains(chr))
					m_charsWithMissingGlyphs.Remove(chr);

				Chars.Remove(chr);
			}

			LoadGrid();
			return chrsToRemove;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the character tool tip if it's showing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void HideToolTip()
		{
			m_toolTip.SetToolTip(this, null);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the selected row is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EnsureSelectedRowVisible()
		{
			if (CurrentRow != null && !CurrentRow.Displayed)
				FirstDisplayedScrollingRowIndex = CurrentRow.Index;
		}

		#endregion

		#region Overridden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.HandleCreated"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			LoadGrid();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnCurrentCellChanged(EventArgs e)
		{
			base.OnCurrentCellChanged(e);

			if (CharacterChanged != null)
				CharacterChanged(this, CurrentCharacter);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			int i = GetListIndexFromCellAddress(e.ColumnIndex, e.RowIndex);
			string chr = (Chars != null && i < Chars.Count && i >= 0 ? Chars[i] : string.Empty);
			string displayText;

			if (m_charsWithMissingGlyphs.Contains(chr))
				this[e.ColumnIndex, e.RowIndex].Tag = Properties.Resources.kimidMissingGlyph;
			else if (m_specialCharStrings.TryGetValue(chr, out displayText))
			{
				e.Value = displayText;
				this[e.ColumnIndex, e.RowIndex].Tag = m_fntForSpecialChar;
			}
			else
			{
				e.Value = chr;
				this[e.ColumnIndex, e.RowIndex].Tag = null;
			}

			base.OnCellValueNeeded(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);
			DataGridViewCell cell = this[e.ColumnIndex, e.RowIndex];
			if (cell != null && cell.Tag is Font)
				e.CellStyle.Font = cell.Tag as Font;
			else
				e.CellStyle.Font = this.Font;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.CellPainting"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			base.OnCellPainting(e);

			Image img = this[e.ColumnIndex, e.RowIndex].Tag as Image;
			if (img != null)
			{
				// Paint the image that was found in the tag property.
				int x = (e.CellBounds.Width - img.Width) / 2;
				int y = (e.CellBounds.Height - img.Height) / 2;
				e.Graphics.DrawImageUnscaled(img, e.CellBounds.X + x, e.CellBounds.Y + y);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			LoadGrid(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the cell's tooltip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellMouseEnter(DataGridViewCellEventArgs e)
		{
			base.OnCellMouseEnter(e);
			int index = GetListIndexFromCellAddress(e.ColumnIndex, e.RowIndex);
			m_toolTip.Show(this, index >= 0 && index < Chars.Count ? Chars[index] : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Turn off the tooltip if the mouse is still on the grid but not on any cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			HitTestInfo htinfo = HitTest(e.X, e.Y);
			if (htinfo.ColumnIndex < 0 || htinfo.RowIndex < 0)
				m_toolTip.Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prevents double-click events from getting passed on when the user doesn't
		/// double-click on a valid cell.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDoubleClick(EventArgs e)
		{
			// Only allow the double click event to get through if the
			// user clicked on a valid cell.
			int i = GetListIndexFromCellAddress();
			Point pt = PointToClient(MousePosition);
			HitTestInfo htinfo = HitTest(pt.X, pt.Y);
			if (i > 0 && i < m_chars.Count && htinfo.RowIndex >= 0 && htinfo.ColumnIndex >= 0)
				base.OnDoubleClick(e);
		}

		#endregion

		#region Misc. Private Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index into the character list of the character in the current cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int GetListIndexFromCellAddress()
		{
			Point pt = CurrentCellAddress;
			return GetListIndexFromCellAddress(pt.X, pt.Y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index into the character list of the character in the cell using the
		/// specified row and column index.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int GetListIndexFromCellAddress(int col, int row)
		{
			return (RowCount == 0 || ColumnCount == 0 || col < 0 || row < 0 ?
				-1 : (ColumnCount * row) + col);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Point GetCellAddressFromListIndex(int index)
		{
			Point pt = new Point(-1, -1);

			if (Chars != null && index >= 0 && index < Chars.Count)
			{
				pt.X = index % ColumnCount;
				pt.Y = index / ColumnCount;
			}

			return pt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character at the specified row and column in the character grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GetCharacterAt(int col, int row)
		{
			int i = GetListIndexFromCellAddress(col, row);
			return (i >= 0 && i < m_chars.Count ? m_chars[i] : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadGrid()
		{
			LoadGrid(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the grid with all the characters. "Loading" is not entirely accurate since
		/// the grid is in virtual mode. More accurately, the grid's number of rows and
		/// columns is being set, as well as the cell size determined.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadGrid(bool calcCellSize)
		{
			if (DesignMode)
				return;

			// Store the previously active cell in order to attempt to
			// restore it after the loading is complete.
			Point prevCell = CurrentCellAddress;
			if (prevCell.X < 0 || prevCell.Y < 0)
				prevCell = new Point(0, 0);

			if (calcCellSize)
				CalcCellSize();

			SuspendLayout();

			int colsNeeded = Math.Min(Chars.Count, NumberOfColumns);

			// Remove columns if there are too many.
			while (colsNeeded < ColumnCount)
				Columns.RemoveAt(0);

			// Add columns if there aren't enough.
			while (colsNeeded > ColumnCount)
				Columns.Add(Guid.NewGuid().ToString(), string.Empty);

			// Set the width of each column.
			for (int i = 0; i < ColumnCount; i++)
				Columns[i].Width = m_cellWidth;

			int newRowCount = NumberOfRows;

			// If the new number of rows is now less than it was, make sure the column of
			// the cell to select after the grid is loaded is the last column in the row.
			if (newRowCount < RowCount)
				prevCell.X = ColumnCount - 1;

			RowCount = newRowCount;

			// Set the height of each row.
			for (int i = 0; i < RowCount; i++)
				Rows[i].Height = m_cellHeight;

			ResumeLayout(true);

			if (Chars.Count == 0)
				return;

			Invalidate();

			// Select the cell that was active before loading the grid.
			try
			{
				// Make sure the row we were on before reloading the grid is still valid.
				while (prevCell.Y >= RowCount)
					prevCell.Y--;

				// Make sure the column we were on before reloading the grid is still valid
				// and that it contains a character, since sometimes there are cells at the
				// end of the last row that are empty.
				while (prevCell.X >= ColumnCount || GetCharacterAt(prevCell.X, prevCell.Y) == null)
					prevCell.X--;

				CurrentCell = this[prevCell.X, prevCell.Y];
			}
			catch
			{
				CurrentCell = this[0, 0];
			}

			// Finally, select only one cell (i.e. remove any range selection
			// there might have been before reloading the grid.
			ClearSelection(CurrentCellAddress.X, CurrentCellAddress.Y, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the chars list is created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EnsureCharsListCreated()
		{
			if (m_chars == null)
			{
				if (LoadCharactersFromFont)
					LoadCharsFromFont();
				else
					m_chars = new List<string>();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all the characters defined in the font into a character array.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadCharsFromFont()
		{
			if (DesignMode)
				return;

			if (MiscUtils.IsUnix)
			{
				// on unix we don't support getting Glyph's from the font.
				// The results i m_chars is only currently used be CalcCellSize
				// before being cleared by a call to RemoveAllCharacters.
				m_chars = new List<string>();
				return;
			}

			// Force the control to be created so we can get an hdc.
			if (!IsHandleCreated)
				CreateHandle();

			using (new WaitCursor(this))
			{
				m_chars = new List<string>();

				using (Graphics g = CreateGraphics())
				{
					IntPtr hdc = g.GetHdc();
					IntPtr hfont = Font.ToHfont();
					IntPtr oldFont = SelectObject(hdc, hfont);

					ushort[] indices = new ushort[1];

					// Even though a font set can, theoretically, contain more than 65535 character
					// definitions, we're going to exclude upper plane characters from being chosen.
					for (int codePoint = kFirstChar; codePoint < 65534; codePoint++)
					{
						char cp = (char)codePoint;
						string chr = cp.ToString();
						uint ret = GetGlyphIndices(hdc, chr, 1, indices, 1);
						if (ret == 1 && indices[0] != 0xFFFF && ShouldLoadFontChar(cp))
							m_chars.Add(chr);
					}

					SelectObject(hdc, oldFont);
					g.ReleaseHdc(hdc);
				}
				CalcCellSize();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not to load the specified character in the grid. This method
		/// is only used when the grid is being loaded from a font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ShouldLoadFontChar(char ch)
		{
			if (ch == StringUtils.kChObject || ch == StringUtils.kchReplacement)
				return false;

			if (m_cpe == null)
			{
				return ((m_fSymbolCharSet || !char.IsLetterOrDigit(ch)) &&
					!char.IsWhiteSpace(ch) && !char.IsControl(ch));
			}

			UcdProperty ucdProp = UcdProperty.GetInstance(m_cpe.get_GeneralCategory(ch));
			string sUcdRep = ucdProp.UcdRepresentation;

			if (string.IsNullOrEmpty(sUcdRep))
				return false;

			char charCat = sUcdRep.ToUpperInvariant()[0];
			return charCat == 'S' || charCat == 'P' ||
				(m_fSymbolCharSet && (charCat == 'L' || charCat == 'N'));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the max. height and width of grid cells in order to accomdate the
		/// largest character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CalcCellSize()
		{
			if (m_chars == null || m_chars.Count == 0)
				return;

			m_cellHeight = 0;
			m_cellWidth = 0;
			int padding = 4;

			foreach (string chr in m_chars)
			{
				Size sz = TextRenderer.MeasureText(chr, Font);
				string displayText;

				if (!m_specialCharStrings.TryGetValue(chr, out displayText))
				{
					m_cellHeight = Math.Max(sz.Height + padding, m_cellHeight);
					m_cellWidth = Math.Max(sz.Width + padding, m_cellWidth);
				}
				else
				{
					// When the character is a space or control character, then we want to
					// display the text associated with it that we've read from the resource
					// file. Therefore, measure the length of the word for space using the
					// font with which we'll format it's cell.
					Size szSpc = TextRenderer.MeasureText(displayText, m_fntForSpecialChar);
					m_cellWidth = Math.Max(szSpc.Width + padding, m_cellWidth);

					// Compare the height of the text from the resource file (using it's
					// font) with the height of the space using the font used for all other
					// characters. Then use the larger of those two values for the Max. method.
					m_cellHeight = Math.Max(m_cellHeight,
						Math.Max(sz.Height, szSpc.Height) + padding);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the set of characters to display. Each "character" should be a single base
		/// character, followed by zero or more combining characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private List<string> Chars
		{
			get
			{
				CheckDisposed();
				EnsureCharsListCreated();
				return m_chars;
			}
		}

		#endregion
	}
}
