// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>Handler for character changed event.</summary>
	public delegate void CharacterChangedHandler(CharacterGrid grid, string newCharacter);

	/// <summary />
	public class CharacterGrid : DataGridView
	{
		[DllImport("gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "GetGlyphIndices")]
		private static extern uint GetGlyphIndicesWindows(IntPtr hdc, string lpstr, int c, [In, Out] ushort[] pgi, int fl);

		private static uint GetGlyphIndices(IntPtr hdc, string lpstr, int c, [In, Out] ushort[] pgi, int fl)
		{
			if (Platform.IsWindows)
			{
				return GetGlyphIndicesWindows(hdc, lpstr, c, pgi, fl);
			}
			throw new NotSupportedException();
		}

		[DllImport("gdi32.dll", EntryPoint = "SelectObject")]
		private static extern IntPtr SelectObjectWindows(IntPtr hdc, IntPtr hfont);

		private static IntPtr SelectObject(IntPtr hdc, IntPtr hfont)
		{
			if (Platform.IsWindows)
			{
				return SelectObjectWindows(hdc, hfont);
			}
			throw new NotSupportedException();
		}

		/// <summary>Event fired after the selected character in the grid changes.</summary>
		public event CharacterChangedHandler CharacterChanged;

		#region Data members
		private const int kFirstChar = 32;
		private int m_cellWidth = 40;
		private int m_cellHeight = 45;
		private Font m_fntForSpecialChar;
		private CharacterInfoToolTip m_toolTip;
		private IComparer m_sortComparer;
		/// <summary>Stores the characters to be displayed. Each "character" should be a
		/// single base character, followed by zero or more combining characters.</summary>
		private List<string> m_chars;
		private bool m_fSymbolCharSet;
		private readonly Dictionary<string, string> m_specialCharStrings = new Dictionary<string, string>();
		private readonly List<string> m_charsWithMissingGlyphs = new List<string>();
		#endregion

		#region Construction, Initialization, and Disposal
		/// <summary />
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

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				m_fntForSpecialChar?.Dispose();
				m_toolTip?.Dispose();
			}
			m_fntForSpecialChar = null;
			m_toolTip = null;

			base.Dispose(disposing);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Sets the writing system to use for sorting purposes.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public CoreWritingSystemDefinition WritingSystem
		{
			set { m_sortComparer = (value == null ? null : new TsStringComparer(value)); }
		}

		/// <summary>
		/// Gets or sets the font showing in the grid.
		/// </summary>
		public new Font Font
		{
			get
			{
				return base.Font;
			}
			set
			{
				if (base.Font != value)
				{
					base.Font = value;
					var logFont = new LogicalFont(value);
					m_fSymbolCharSet = logFont.IsSymbolCharSet;
					m_chars = null;
					if (IsHandleCreated)
					{
						LoadGrid();
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the character grid should be loaded with
		/// characters from the Font.
		/// </summary>
		[Browsable(true)]
		[DefaultValue(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public bool LoadCharactersFromFont { get; set; } = true;

		/// <summary>
		/// Gets or sets the current character in the grid.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string CurrentCharacter
		{
			get
			{
				var index = GetListIndexFromCellAddress();
				return (index >= 0 && index < Chars.Count ? Chars[index] : string.Empty);
			}
			set
			{
				for (var i = 0; i < Chars.Count; i++)
				{
					if (value == Chars[i])
					{
						var pt = GetCellAddressFromListIndex(i);
						if (pt.X >= 0 && pt.Y >= 0)
						{
							CurrentCell = this[pt.X, pt.Y];
						}
						return;
					}
				}
				if (RowCount > 0 && ColumnCount > 0)
				{
					CurrentCell = this[0, 0];
				}
			}
		}

		/// <summary>
		/// Gets the count of characters displayed in the grid.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private int NumberOfChars => Chars.Count;

		/// <summary>
		/// Gets the number of columns in the grid.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int NumberOfColumns => (int)Math.Floor((double)UsableWidth / m_cellWidth);

		/// <summary>
		/// Gets the number of rows in the grid (based on the number of columns in the grid and
		/// the number of characters in the font).
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int NumberOfRows
		{
			get
			{
				var rows = (int)Math.Ceiling((double)NumberOfChars / NumberOfColumns);
				return (rows > 0 ? rows : 0);
			}
		}

		/// <summary />
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private int UsableWidth => ClientSize.Width;

		#endregion

		#region Public methods

		/// <summary>
		/// Adds a list of characters to display. Each "character" should be a single base
		/// character, followed by zero or more combining characters.
		/// </summary>
		public void AddCharacters(IEnumerable<string> value)
		{
			Debug.Assert(!LoadCharactersFromFont);

			foreach (var ch in value)
			{
				InternalAddCharacter(ch);
			}
			if (m_sortComparer != null)
			{
				Chars.Sort(m_sortComparer.Compare);
			}
			else
			{
				Chars.Sort();
			}

			LoadGrid();
		}

		/// <summary>
		/// Adds the specified character to the grid's list of displayed characters. This is
		/// called by the two public methods AddCharacter and AddCharacters.
		/// </summary>
		private void InternalAddCharacter(string chr)
		{
			// Keep track of those characters that are not control characters and for
			// which there are no representative glyphs in the font.
			if (Win32.AreCharGlyphsInFont(chr, Font))
			{
				if (m_charsWithMissingGlyphs.Contains(chr))
				{
					m_charsWithMissingGlyphs.Remove(chr);
				}
			}
			else if (!Icu.IsControl(chr[0]))
			{
				if (!m_charsWithMissingGlyphs.Contains(chr))
				{
					m_charsWithMissingGlyphs.Add(chr);
				}
			}

			// If we're dealing with a type of space or control character, then figure out
			// display text that is slightly more readable than whatever glyph the font
			// contains for the character, which may not be a glyph at all.
			if ((Icu.IsSpace(chr[0]) || Icu.IsControl(chr[0])) && !m_specialCharStrings.ContainsKey(chr))
			{
				m_specialCharStrings[chr] = GetSpecialCharDisplayText(chr);
			}

			Chars.Add(chr);
		}

		/// <summary>
		/// Gets the display name for the specified space or control character. If no display
		/// name is found, then the U+ value is returned.
		/// </summary>
		public static string GetSpecialCharDisplayText(string chr)
		{
			if (string.IsNullOrEmpty(chr))
			{
				return null;
			}
			var resName = $"kstidSpecialChar{(int)chr[0]:X4}";
			var displayText = Properties.Resources.ResourceManager.GetString(resName);

			return string.IsNullOrEmpty(displayText) ? $"U+{(int)chr[0]:X4}" : displayText;
		}

		/// <summary>
		/// Removes all characters. If characters are loaded from font, they will be re-added
		/// automatically when the control is repainted.
		/// </summary>
		public void RemoveAllCharacters()
		{
			Columns.Clear();
			RowCount = 0;
			m_chars = null;
			m_charsWithMissingGlyphs.Clear();
		}

		/// <summary>
		/// Removes the current character.
		/// </summary>
		public void RemoveCurrentCharacter()
		{
			var index = GetListIndexFromCellAddress();
			if (index > 0)
			{
				if (m_charsWithMissingGlyphs.Contains(Chars[index]))
				{
					m_charsWithMissingGlyphs.Remove(Chars[index]);
				}
				Chars.RemoveAt(index);
				LoadGrid();
			}
		}

		/// <summary>
		/// Removes the specified character from the grid.
		/// </summary>
		/// <param name="chr">The character to remove.</param>
		public void RemoveCharacter(string chr)
		{
			if (Chars.Contains(chr))
			{
				if (m_charsWithMissingGlyphs.Contains(chr))
				{
					m_charsWithMissingGlyphs.Remove(chr);
				}
				Chars.Remove(chr);
				LoadGrid();
			}
		}

		/// <summary>
		/// Removes the selected characters from the grid and returns a list of those that
		/// were removed.
		/// </summary>
		public List<string> RemoveSelectedCharacters()
		{
			if (SelectedCells.Count == 0)
			{
				return null;
			}
			var chrsToRemove = new List<string>();
			foreach (DataGridViewCell cell in SelectedCells)
			{
				chrsToRemove.Add(GetCharacterAt(cell.ColumnIndex, cell.RowIndex));
			}
			foreach (var chr in chrsToRemove)
			{
				if (m_charsWithMissingGlyphs.Contains(chr))
				{
					m_charsWithMissingGlyphs.Remove(chr);
				}
				Chars.Remove(chr);
			}

			LoadGrid();
			return chrsToRemove;
		}

		/// <summary>
		/// Hides the character tool tip if it's showing.
		/// </summary>
		public void HideToolTip()
		{
			m_toolTip.SetToolTip(this, null);
		}

		/// <summary>
		/// Ensures the selected row is visible.
		/// </summary>
		public void EnsureSelectedRowVisible()
		{
			if (CurrentRow != null && !CurrentRow.Displayed)
			{
				FirstDisplayedScrollingRowIndex = CurrentRow.Index;
			}
		}

		#endregion

		#region Overridden Methods

		/// <inheritdoc />
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			LoadGrid();
		}

		/// <inheritdoc />
		protected override void OnCurrentCellChanged(EventArgs e)
		{
			base.OnCurrentCellChanged(e);

			CharacterChanged?.Invoke(this, CurrentCharacter);
		}

		/// <inheritdoc />
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			var i = GetListIndexFromCellAddress(e.ColumnIndex, e.RowIndex);
			var chr = Chars != null && i < Chars.Count && i >= 0 ? Chars[i] : string.Empty;
			string displayText;
			if (m_charsWithMissingGlyphs.Contains(chr))
			{
				this[e.ColumnIndex, e.RowIndex].Tag = Properties.Resources.kimidMissingGlyph;
			}
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

		/// <inheritdoc />
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);
			var cell = this[e.ColumnIndex, e.RowIndex];
			if (cell?.Tag is Font)
			{
				e.CellStyle.Font = (Font)cell.Tag;
			}
			else
			{
				e.CellStyle.Font = Font;
			}
		}

		/// <inheritdoc />
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			base.OnCellPainting(e);

			var img = this[e.ColumnIndex, e.RowIndex].Tag as Image;
			if (img != null)
			{
				// Paint the image that was found in the tag property.
				var x = (e.CellBounds.Width - img.Width) / 2;
				var y = (e.CellBounds.Height - img.Height) / 2;
				e.Graphics.DrawImageUnscaled(img, e.CellBounds.X + x, e.CellBounds.Y + y);
			}
		}

		/// <inheritdoc />
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			LoadGrid(false);
		}

		/// <inheritdoc />
		protected override void OnCellMouseEnter(DataGridViewCellEventArgs e)
		{
			base.OnCellMouseEnter(e);
			var index = GetListIndexFromCellAddress(e.ColumnIndex, e.RowIndex);
			m_toolTip.Show(this, index >= 0 && index < Chars.Count ? Chars[index] : null);
		}

		/// <inheritdoc />
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			var htinfo = HitTest(e.X, e.Y);
			if (htinfo.ColumnIndex < 0 || htinfo.RowIndex < 0)
			{
				m_toolTip.Hide();
			}
		}

		/// <inheritdoc />
		protected override void OnDoubleClick(EventArgs e)
		{
			// Only allow the double click event to get through if the
			// user clicked on a valid cell.
			var idx = GetListIndexFromCellAddress();
			var pt = PointToClient(MousePosition);
			var htinfo = HitTest(pt.X, pt.Y);
			if (idx > 0 && idx < m_chars.Count && htinfo.RowIndex >= 0 && htinfo.ColumnIndex >= 0)
			{
				base.OnDoubleClick(e);
			}
		}

		#endregion

		#region Misc. Private Methods

		/// <summary>
		/// Gets the index into the character list of the character in the current cell.
		/// </summary>
		private int GetListIndexFromCellAddress()
		{
			var pt = CurrentCellAddress;
			return GetListIndexFromCellAddress(pt.X, pt.Y);
		}

		/// <summary>
		/// Gets the index into the character list of the character in the cell using the
		/// specified row and column index.
		/// </summary>
		private int GetListIndexFromCellAddress(int col, int row)
		{
			return (RowCount == 0 || ColumnCount == 0 || col < 0 || row < 0 ? -1 : (ColumnCount * row) + col);
		}

		/// <summary />
		private Point GetCellAddressFromListIndex(int index)
		{
			var pt = new Point(-1, -1);
			if (Chars != null && index >= 0 && index < Chars.Count)
			{
				pt.X = index % ColumnCount;
				pt.Y = index / ColumnCount;
			}

			return pt;
		}

		/// <summary>
		/// Gets the character at the specified row and column in the character grid.
		/// </summary>
		public string GetCharacterAt(int col, int row)
		{
			var i = GetListIndexFromCellAddress(col, row);
			return (i >= 0 && i < m_chars.Count ? m_chars[i] : null);
		}

		/// <summary>
		/// Loads the grid with all the characters. "Loading" is not entirely accurate since
		/// the grid is in virtual mode. More accurately, the grid's number of rows and
		/// columns is being set, as well as the cell size determined.
		/// </summary>
		private void LoadGrid(bool calcCellSize = true)
		{
			if (DesignMode)
			{
				return;
			}
			// Store the previously active cell in order to attempt to
			// restore it after the loading is complete.
			var prevCell = CurrentCellAddress;
			if (prevCell.X < 0 || prevCell.Y < 0)
			{
				prevCell = new Point(0, 0);
			}
			if (calcCellSize)
			{
				CalcCellSize();
			}
			SuspendLayout();

			var colsNeeded = Math.Min(Chars.Count, NumberOfColumns);
			// Remove columns if there are too many.
			while (colsNeeded < ColumnCount)
			{
				Columns.RemoveAt(0);
			}
			// Add columns if there aren't enough.
			while (colsNeeded > ColumnCount)
			{
				Columns.Add(Guid.NewGuid().ToString(), string.Empty);
			}
			// Set the width of each column.
			for (var i = 0; i < ColumnCount; i++)
			{
				Columns[i].Width = m_cellWidth;
			}
			var newRowCount = NumberOfRows;
			// If the new number of rows is now less than it was, make sure the column of
			// the cell to select after the grid is loaded is the last column in the row.
			if (newRowCount < RowCount)
			{
				prevCell.X = ColumnCount - 1;
			}
			RowCount = newRowCount;
			// Set the height of each row.
			for (var i = 0; i < RowCount; i++)
			{
				Rows[i].Height = m_cellHeight;
			}

			ResumeLayout(true);

			if (Chars.Count == 0)
			{
				return;
			}
			Invalidate();

			// Select the cell that was active before loading the grid.
			try
			{
				// Make sure the row we were on before reloading the grid is still valid.
				while (prevCell.Y >= RowCount)
				{
					prevCell.Y--;
				}
				// Make sure the column we were on before reloading the grid is still valid
				// and that it contains a character, since sometimes there are cells at the
				// end of the last row that are empty.
				while (prevCell.X >= ColumnCount || GetCharacterAt(prevCell.X, prevCell.Y) == null)
				{
					prevCell.X--;
				}
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

		/// <summary>
		/// Ensures the chars list is created.
		/// </summary>
		private void EnsureCharsListCreated()
		{
			if (m_chars == null)
			{
				if (LoadCharactersFromFont)
				{
					LoadCharsFromFont();
				}
				else
				{
					m_chars = new List<string>();
				}
			}
		}

		/// <summary>
		/// Load all the characters defined in the font into a character array.
		/// </summary>
		private void LoadCharsFromFont()
		{
			if (DesignMode)
			{
				return;
			}
			if (MiscUtils.IsUnix)
			{
				// on unix we don't support getting Glyphs from the font.
				// The results i m_chars is only currently used be CalcCellSize
				// before being cleared by a call to RemoveAllCharacters.
				m_chars = new List<string>();
				return;
			}

			// Force the control to be created so we can get an hdc.
			if (!IsHandleCreated)
			{
				CreateHandle();
			}
			using (new WaitCursor(this))
			{
				m_chars = new List<string>();

				using (var g = CreateGraphics())
				{
					var hdc = g.GetHdc();
					var hfont = Font.ToHfont();
					var oldFont = SelectObject(hdc, hfont);
					var indices = new ushort[1];

					// Even though a font set can, theoretically, contain more than 65535 character
					// definitions, we're going to exclude upper plane characters from being chosen.
					for (var codePoint = kFirstChar; codePoint < 65534; codePoint++)
					{
						var cp = (char)codePoint;
						var chr = cp.ToString();
						var ret = GetGlyphIndices(hdc, chr, 1, indices, 1);
						if (ret == 1 && indices[0] != 0xFFFF && ShouldLoadFontChar(cp))
						{
							m_chars.Add(chr);
						}
					}

					SelectObject(hdc, oldFont);
					g.ReleaseHdc(hdc);
				}
				CalcCellSize();
			}
		}

		/// <summary>
		/// Determines whether or not to load the specified character in the grid. This method
		/// is only used when the grid is being loaded from a font.
		/// </summary>
		private bool ShouldLoadFontChar(char ch)
		{
			return ch != StringUtils.kChObject && ch != StringUtils.kchReplacement && (Icu.IsSymbol(ch) || Icu.IsPunct(ch) || (m_fSymbolCharSet && (Icu.IsLetter(ch) || Icu.IsNumeric(ch))));
		}

		/// <summary>
		/// Determines the max. height and width of grid cells in order to accomodate the
		/// largest character.
		/// </summary>
		private void CalcCellSize()
		{
			if (m_chars == null || m_chars.Count == 0)
			{
				return;
			}
			m_cellHeight = 0;
			m_cellWidth = 0;
			const int padding = 4;

			foreach (var chr in m_chars)
			{
				var sz = TextRenderer.MeasureText(chr, Font);
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
					var szSpc = TextRenderer.MeasureText(displayText, m_fntForSpecialChar);
					m_cellWidth = Math.Max(szSpc.Width + padding, m_cellWidth);

					// Compare the height of the text from the resource file (using it's
					// font) with the height of the space using the font used for all other
					// characters. Then use the larger of those two values for the Max. method.
					m_cellHeight = Math.Max(m_cellHeight, Math.Max(sz.Height, szSpc.Height) + padding);
				}
			}
		}

		/// <summary>
		/// Gets the set of characters to display. Each "character" should be a single base
		/// character, followed by zero or more combining characters.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private List<string> Chars
		{
			get
			{
				EnsureCharsListCreated();
				return m_chars;
			}
		}

		#endregion

		/// <summary>
		/// Provides an owner-drawn class that displays Unicode values for a text string and the
		/// ICU character name when the text string is a single character.
		/// </summary>
		private sealed class CharacterInfoToolTip : ToolTip
		{
			private Font m_fntTitle;
			private Font m_fntText;
			private string m_text;
			private Rectangle m_rcText;
			private Rectangle m_rcTitle;
			private bool m_showMissingGlyphIcon;
			private readonly Image m_missingGlyphIcon = Properties.Resources.kimidMissingGlyph;

			/// <summary />
			internal CharacterInfoToolTip()
			{
				m_fntText = SystemFonts.IconTitleFont;
				m_fntTitle = new Font(m_fntText, FontStyle.Bold);

				ReshowDelay = 1000;
				AutoPopDelay = 5000;
				OwnerDraw = true;
				Popup += HandlePopup;
				Draw += HandleDraw;
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

				if (disposing)
				{
					m_fntTitle?.Dispose();
				}
				m_fntTitle = null;
				m_fntText = null;

				base.Dispose(disposing);
			}

			/// <summary>
			/// Gets or sets the control over which the tooltip is being displayed.
			/// </summary>
			private Control Control { get; set; }

			/// <summary>
			/// Gets or sets the character font.
			/// </summary>
			private Font CharacterFont { get; set; }

			/// <summary>
			/// Shows tooltip for the specified control and character.
			/// </summary>
			internal void Show(Control ctrl, string chr)
			{
				Show(ctrl, chr, CharacterFont);
			}

			/// <summary>
			/// Shows tooltip for the specified character at the specified coordinate relative
			/// to the grid control specified at construction.
			/// </summary>
			private void Show(Control ctrl, string chr, Font fnt)
			{
				Control = ctrl;

				if (Control == null)
				{
					FwUtils.FwUtils.ErrorBeep();
					return;
				}

				if (CharacterFont == null)
				{
					CharacterFont = Control.Font;
				}

				Hide();

				if (!string.IsNullOrEmpty(chr))
				{
					BuildToolTipContent(chr);
					SetToolTip(Control, m_text);
				}
			}

			/// <summary>
			/// Hides this instance.
			/// </summary>
			internal void Hide()
			{
				if (Control != null)
				{
					SetToolTip(Control, null);
					Hide(Control);
				}
			}

			/// <summary>
			/// Builds the content of the tool tip.
			/// </summary>
			private void BuildToolTipContent(string chr)
			{
				m_text = chr.Length == 1 ? Properties.Resources.kstidChrGridCodepoint : Properties.Resources.kstidChrGridCodepoints;

				// Get the string containing the character codepoints.
				m_text = string.Format(m_text, chr.CharacterCodepoints());

				var name = Icu.GetPrettyICUCharName(chr);

				// Get the name of the character if its length is 1.
				if (!string.IsNullOrEmpty(name))
				{
					name = string.Format(Properties.Resources.kstidChrGridName, name);
					m_text += Environment.NewLine + name;
				}

				m_showMissingGlyphIcon = !Win32.AreCharGlyphsInFont(chr, CharacterFont);

				// If the glyphs for the codepoints are not present in the character
				// grid's font, then use a heading telling the user that.
				ToolTipTitle = (m_showMissingGlyphIcon ? Properties.Resources.kstidChrGridMissingGlyphHdg : Properties.Resources.kstidChrGridNormalHdg);
			}

			/// <summary>
			/// Handles the popup.
			/// </summary>
			private void HandlePopup(object sender, PopupEventArgs e)
			{
				using (var g = Graphics.FromHwnd(e.AssociatedWindow.Handle))
				{
					var sz1 = TextRenderer.MeasureText(g, ToolTipTitle, m_fntTitle);
					var sz2 = TextRenderer.MeasureText(g, m_text, m_fntText);

					m_rcTitle = new Rectangle(10, 10, sz1.Width, sz1.Height);
					m_rcText = new Rectangle(10, m_rcTitle.Bottom + 15, sz2.Width, sz2.Height);

					if (m_showMissingGlyphIcon)
					{
						m_rcTitle.X += (m_missingGlyphIcon.Width + 5);
						sz1.Width += (m_missingGlyphIcon.Width + 5);
						sz1.Height = Math.Max(sz1.Height, m_missingGlyphIcon.Height);
					}

					sz1.Width = Math.Max(sz1.Width, sz2.Width) + 20;
					sz1.Height += (sz2.Height + 35);
					e.ToolTipSize = sz1;
				}
			}

			/// <summary>
			/// Handles the draw.
			/// </summary>
			private void HandleDraw(object sender, DrawToolTipEventArgs e)
			{
				e.DrawBackground();
				e.DrawBorder();

				var frm = Control.FindForm();
				var flags = (frm.RightToLeft == RightToLeft.Yes ? TextFormatFlags.RightToLeft : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter;

				TextRenderer.DrawText(e.Graphics, ToolTipTitle, m_fntTitle, m_rcTitle, SystemColors.InfoText, flags);

				TextRenderer.DrawText(e.Graphics, m_text, m_fntText, m_rcText, SystemColors.InfoText, flags);

				// Draw the icon
				if (m_showMissingGlyphIcon)
				{
					var pt = m_rcTitle.Location;
					pt.X -= (m_missingGlyphIcon.Width + 5);
					if (m_missingGlyphIcon.Height > m_rcTitle.Height)
					{
						pt.Y -= (m_missingGlyphIcon.Height - m_rcTitle.Height) / 2;
					}

					e.Graphics.DrawImageUnscaled(m_missingGlyphIcon, pt);
				}

				// Draw a line separating the title from the text below it.
				var pt1 = new Point(e.Bounds.X + 7, m_rcTitle.Bottom + 7);
				var pt2 = new Point(e.Bounds.Right - 5, m_rcTitle.Bottom + 7);

				using (var br = new LinearGradientBrush(pt1, pt2, SystemColors.InfoText, SystemColors.Info))
				{
					e.Graphics.DrawLine(new Pen(br, 1), pt1, pt2);
				}
			}
		}
	}
}