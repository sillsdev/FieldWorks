// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ValidCharactersDlg.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Media;
using SIL.FieldWorks.FDO.Cellar;
using SILUBS.SharedScrUtils;

using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for specifying the valid characters for a FieldWorks writing system
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ValidCharactersDlg : Form
	{
		#region ValidCharGridsManager class
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected internal class ValidCharGridsManager : IDisposable
		{
			internal EventHandler CharacterGridGotFocus;
			internal EventHandler CharacterGridSelectionChanged;

			private CharacterGrid m_gridWordForming;
			private CharacterGrid m_gridNumbers;
			private CharacterGrid m_gridOther;
			private CharacterGrid m_currGrid;

			private ValidCharacters m_validChars;
			private LanguageDefinition m_langDef;
			private readonly ContextMenuStrip m_cmnu;
			private readonly ToolStripMenuItem m_cmnuTreatAsWrdForming;
			private readonly ToolStripMenuItem m_cmnuTreatAsNotWrdForming;
			private readonly ToolStripSeparator m_cmnuSep;
			private readonly ToolStripMenuItem m_cmnuRemove;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Constructs a new instance of the <see cref="ValidCharGridsManager"/> class.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public ValidCharGridsManager()
			{
				m_cmnu = new ContextMenuStrip();
				m_cmnuTreatAsWrdForming = new ToolStripMenuItem(Properties.Resources.kstidTreatAsWrdForming);
				m_cmnuTreatAsNotWrdForming = new ToolStripMenuItem(Properties.Resources.kstidTreatAsOther);
				m_cmnuSep = new ToolStripSeparator();
				m_cmnuRemove = new ToolStripMenuItem(Properties.Resources.kstidRemoveValidChar);

				m_cmnuTreatAsWrdForming.Click += HandleTreatAsClick;
				m_cmnuTreatAsNotWrdForming.Click += HandleTreatAsClick;
				m_cmnuRemove.Click += HandleRemoveClick;

				m_cmnu.Items.Add(m_cmnuTreatAsWrdForming);
				m_cmnu.Items.Add(m_cmnuTreatAsNotWrdForming);
				m_cmnu.Items.Add(m_cmnuSep);
				m_cmnu.Items.Add(m_cmnuRemove);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes the valid characters explorer bar with three valid character grids.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal void Init(CharacterGrid gridWf, CharacterGrid gridOther, CharacterGrid gridNum,
				LanguageDefinition langDef)
			{
				m_langDef = langDef;

				gridWf.BackgroundColor = SystemColors.Window;
				gridNum.BackgroundColor = SystemColors.Window;
				gridOther.BackgroundColor = SystemColors.Window;

				gridWf.MultiSelect = true;
				gridNum.MultiSelect = true;
				gridOther.MultiSelect = true;

				gridWf.CellPainting += HandleGridCellPainting;
				gridNum.CellPainting += HandleGridCellPainting;
				gridOther.CellPainting += HandleGridCellPainting;

				gridWf.Enter += HandleGridEnter;
				gridNum.Enter += HandleGridEnter;
				gridOther.Enter += HandleGridEnter;

				gridWf.CellFormatting += HandleCellFormatting;
				gridNum.CellFormatting += HandleCellFormatting;
				gridOther.CellFormatting += HandleCellFormatting;

				gridWf.CellMouseClick += HandleCharGridCellMouseClick;
				gridNum.CellMouseClick += HandleCharGridCellMouseClick;
				gridOther.CellMouseClick += HandleCharGridCellMouseClick;

				gridWf.SelectionChanged += HandleCharGridSelectionChanged;
				gridOther.SelectionChanged += HandleCharGridSelectionChanged;

				m_gridWordForming = gridWf;
				m_gridNumbers = gridNum;
				m_gridOther = gridOther;
				m_validChars = ValidCharacters.Load(langDef);

				RefreshCharacterGrids(ValidCharacterType.All);
			}

			#region IDisposable Members
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Releases the unmanaged resources used by the
			/// <see cref="T:System.Windows.Forms.Control"/> and its child controls and optionally
			/// releases the managed resources.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void Dispose()
			{
				m_gridWordForming.CellPainting -= HandleGridCellPainting;
				m_gridNumbers.CellPainting -= HandleGridCellPainting;
				m_gridOther.CellPainting -= HandleGridCellPainting;

				m_gridWordForming.Enter -= HandleGridEnter;
				m_gridNumbers.Enter -= HandleGridEnter;
				m_gridOther.Enter -= HandleGridEnter;

				m_gridWordForming.CellFormatting -= HandleCellFormatting;
				m_gridNumbers.CellFormatting -= HandleCellFormatting;
				m_gridOther.CellFormatting -= HandleCellFormatting;

				m_gridWordForming.CellMouseClick -= HandleCharGridCellMouseClick;
				m_gridNumbers.CellMouseClick -= HandleCharGridCellMouseClick;
				m_gridOther.CellMouseClick -= HandleCharGridCellMouseClick;

				m_gridWordForming.SelectionChanged -= HandleCharGridSelectionChanged;
				m_gridOther.SelectionChanged -= HandleCharGridSelectionChanged;
			}

			#endregion

			#region Properties
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the current grid.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal CharacterGrid CurrentGrid
			{
				get { return m_currGrid; }
				set
				{
					if ((value == m_gridWordForming || value == m_gridNumbers ||
						value == m_gridOther) && value != m_currGrid)
					{
						m_currGrid = value;
						m_currGrid.EnsureSelectedRowVisible();
					}
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the current character in the current grid.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal string CurrentCharacter
			{
				get { return (m_currGrid != null ? m_currGrid.CurrentCharacter : null); }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether all the character grids are empty.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal bool IsEmpty
			{
				get
				{
					return (m_gridWordForming.RowCount == 0 && m_gridNumbers.RowCount == 0 &&
						m_gridOther.RowCount == 0);
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether or not the current grid is the grid for word
			/// forming characters and whether or not the selected cells in that grid are allowed
			/// to be moved to the symbols, punctuation and other characters grid.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal bool CanMoveToPuncSymbolList
			{
				get
				{
					return (CurrentGrid == m_gridWordForming &&
						AreSelectedCharsOther(m_gridWordForming));
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether or not the current grid is the grid for symbols,
			/// punctuation and other characters and if whether or not the selected cells in that
			/// grid are allowed to be moved to the word forming list.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal bool CanMoveToWordFormingList
			{
				get { return (CurrentGrid == m_gridOther && AreSelectedCharsOther(m_gridOther)); }
			}

			#endregion

			#region Private and internal methods
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether or not all the selected characters in the specified grid fall
			/// into the category of punctuation or symbol characters.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private bool AreSelectedCharsOther(CharacterGrid grid)
			{
				if (grid == null || grid == m_gridNumbers || grid.SelectedCells.Count == 0)
					return false;

				foreach (DataGridViewCell cell in grid.SelectedCells)
				{
					string chr = grid.GetCharacterAt(cell.ColumnIndex, cell.RowIndex);
					if (string.IsNullOrEmpty(chr) || chr.Length <= 0)
						return false;

					if (!m_validChars.CanBeWordFormingOverride(chr))
						return false;
				}

				return true;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Updates the language definition's PUA chars and valid characters.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal void Save()
			{
				// Make sure all the custom PUA characters specified in the valid characters
				// list are also added to the language definition's PUA collection.
				StringUtils.UpdatePUACollection(m_langDef, m_validChars.AllCharacters);
				m_langDef.ValidChars = m_validChars.XmlString;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Adds the specified character to the list of valid characters.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected internal virtual void AddCharacter(string chr)
			{
				AddCharacter(chr, ValidCharacterType.DefinedUnknown, false);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Adds the specified character to the list of valid characters.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected internal virtual void AddCharacter(string chr, ValidCharacterType type,
				bool makeReceivingGridCurrent)
			{
				ValidCharacterType retType = m_validChars.AddCharacter(chr, type);
				RefreshCharacterGrids(retType);

				if (makeReceivingGridCurrent || CurrentGrid == null)
				{
					switch (retType)
					{
						case ValidCharacterType.WordForming: CurrentGrid = m_gridWordForming; break;
						case ValidCharacterType.Numeric: CurrentGrid = m_gridNumbers; break;
						case ValidCharacterType.Other: CurrentGrid = m_gridOther; break;
						default: return;
					}

					CurrentGrid.CurrentCharacter = chr;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Adds the list of characters to the list of valid characters.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal void AddCharacters(List<string> chrs)
			{
				RefreshCharacterGrids(m_validChars.AddCharacters(chrs));

				if (CurrentGrid == null)
				{
					if (m_gridWordForming.RowCount > 0)
						CurrentGrid = m_gridWordForming;
					else if (m_gridNumbers.RowCount > 0)
						CurrentGrid = m_gridNumbers;
					else if (m_gridOther.RowCount > 0)
						CurrentGrid = m_gridOther;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Clears all the valid characters.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal void Reset()
			{
				m_validChars.Reset();
				m_currGrid = null;
				RefreshCharacterGrids(ValidCharacterType.All);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Adds the specified valid character to one of the valid character grids.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private void RefreshCharacterGrids(ValidCharacterType type)
			{
				if ((type & ValidCharacterType.WordForming) != 0)
				{
					m_gridWordForming.RemoveAllCharacters();
					m_gridWordForming.AddCharacters(m_validChars.WordFormingCharacters, m_langDef);
				}

				if ((type & ValidCharacterType.Numeric) != 0)
				{
					m_gridNumbers.RemoveAllCharacters();
					m_gridNumbers.AddCharacters(m_validChars.NumericCharacters, m_langDef);
				}

				if ((type & ValidCharacterType.Other) != 0)
				{
					m_gridOther.RemoveAllCharacters();
					m_gridOther.AddCharacters(m_validChars.OtherCharacters, m_langDef);
				}
			}
			#endregion

			#region Event handlers
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Display a context menu when the user clicks on one of the valid character grids.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private void HandleCharGridCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
			{
				CharacterGrid grid = sender as CharacterGrid;
				if (grid == null || e.Button != MouseButtons.Right)
					return;

				// Make sure we have a character.
				string chr = grid.GetCharacterAt(e.ColumnIndex, e.RowIndex);
				if (string.IsNullOrEmpty(chr))
					return;

				grid.HideToolTip();

				DataGridViewCell clickedCell = grid[e.ColumnIndex, e.RowIndex];
				if (!grid.SelectedCells.Contains(clickedCell))
					grid.CurrentCharacter = chr;

				if (grid != CurrentGrid)
					CurrentGrid = grid;

				m_cmnuTreatAsNotWrdForming.Visible = false;
				m_cmnuTreatAsWrdForming.Visible = false;
				m_cmnuSep.Visible = false;

				if (AreSelectedCharsOther(grid))
				{
					m_cmnuSep.Visible = true;
					m_cmnuTreatAsWrdForming.Visible = (grid == m_gridOther);
					m_cmnuTreatAsNotWrdForming.Visible = (grid != m_gridOther);
				}

				m_cmnu.Show(Control.MousePosition);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Handles the selection changed event for the word forming and other grids.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			void HandleCharGridSelectionChanged(object sender, EventArgs e)
			{
				if (CharacterGridSelectionChanged != null)
					CharacterGridSelectionChanged(sender, e);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Handles the grid enter.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			void HandleGridEnter(object sender, EventArgs e)
			{
				m_currGrid = sender as CharacterGrid;

				if (CharacterGridGotFocus != null)
					CharacterGridGotFocus(sender, e);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Handles the cell formatting.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			void HandleCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
			{
				if (sender == m_currGrid)
				{
					e.CellStyle.SelectionBackColor = m_currGrid.DefaultCellStyle.SelectionBackColor;
					e.CellStyle.SelectionForeColor = m_currGrid.DefaultCellStyle.SelectionForeColor;
				}
				else
				{
					e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
					e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Paint the background of word-forming override characters differently from all the
			/// other characters.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private void HandleGridCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
			{
				CharacterGrid grid = sender as CharacterGrid;
				string chr = grid.GetCharacterAt(e.ColumnIndex, e.RowIndex);
				DataGridViewPaintParts paintParts = e.PaintParts;

				if (grid != m_currGrid || string.IsNullOrEmpty(chr))
					paintParts &= ~DataGridViewPaintParts.Focus;

				if (grid == m_gridWordForming)
				{
					// Owner-draw the background when the character is a word-forming
					// override (i.e. was moved from the punct./symbol/other list).
					if (m_validChars.IsWordFormingOverride(chr))
					{
						DrawWordFormingGridCellBackground(e);

						// When painting the rest of the parts below, then exclude the background.
						paintParts &= ~DataGridViewPaintParts.Background;
					}
				}

				e.Paint(e.CellBounds, paintParts);
				e.Handled = true;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Draws the word forming grid cell's background when it's character represents a
			/// word-forming override character.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private void DrawWordFormingGridCellBackground(DataGridViewCellPaintingEventArgs e)
			{
				bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
				Color clrSelBack = m_gridWordForming.DefaultCellStyle.SelectionBackColor;

				Color clr1 = (selected ? ColorHelper.CalculateColor(
					Color.White, clrSelBack, 120) : m_gridWordForming.BackgroundColor);

				Color clr2 = (selected ? e.CellStyle.SelectionBackColor :
					ColorHelper.CalculateColor(Color.White, clrSelBack, 100));

				// Use a gradient fill for the background.
				using (LinearGradientBrush br = new LinearGradientBrush(e.CellBounds, clr1, clr2, 90))
					e.Graphics.FillRectangle(br, e.CellBounds);
			}
			#endregion

			#region Context menu events
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Handles the click on one of the "Treat as..." context menu items.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private void HandleTreatAsClick(object sender, EventArgs e)
			{
				MoveSelectedChars();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Moves the selected characters from the current grid to the opposing grid.
			/// </summary>
			/// <returns>The character grid to which the character was moved.</returns>
			/// --------------------------------------------------------------------------------
			public CharacterGrid MoveSelectedChars()
			{
				CharacterGrid gridFrom = CurrentGrid;
				CharacterGrid gridTo = (gridFrom == m_gridOther ? m_gridWordForming : m_gridOther);

				if (gridFrom == null || gridTo == null || gridFrom.SelectedCells.Count == 0)
					return null;

				List<string> selChars = new List<string>();
				foreach (DataGridViewCell cell in gridFrom.SelectedCells)
					selChars.Add(CurrentGrid.GetCharacterAt(cell.ColumnIndex, cell.RowIndex));

				m_validChars.MoveBetweenWordFormingAndOther(selChars, gridFrom == m_gridOther);
				gridTo.AddCharacters(gridFrom.RemoveSelectedCharacters(), m_langDef);
				return gridTo;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Handles the Click event of the remove context menu or Remove button.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal void HandleRemoveClick(object sender, EventArgs e)
			{
				// The grid from which we're removing a character is in the tag property of the
				// context menu. The character being moved is in the tag property of the grid.
				if (CurrentGrid == null || CurrentGrid.SelectedCells.Count == 0)
					return;

				m_validChars.RemoveCharacters(CurrentGrid.RemoveSelectedCharacters());
			}

			#endregion
		}

		#endregion

		#region Data members
		private readonly FdoCache m_cache;
		private readonly LanguageDefinition m_langDef;
		private readonly ILgCharacterPropertyEngine m_chrPropEng;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private Font m_fntForSpecialChar;

		// Members needed for From Data tab

		private const int kiTabBasedOn = 0;
		private const int kiTabManual = 1;
		private const int kiTabData = 2;
		private const int kiTabUnicode = 3;
		private const int kiCharCol = 0;
		private const int kiCharCodeCol = 1;
		private const int kiCharCountCol = 2;
		private const int kiCharValidCol = 3;

		// Maximum occurrence of any one character in this dialog.
		private const int kMaxOccurrences = 25;

		private static SortOrder s_inventorySortOrder = SortOrder.Ascending;

		/// <summary>a count for each character occurrence</summary>
		private Dictionary<string, int> m_characterCount;

		private List<CharacterInventoryRow> m_inventoryRows;
		private TsStringComparer m_inventoryCharComparer = null;
		/// <summary>Protected to facilitate testing</summary>
		protected ValidCharGridsManager m_validCharsGridMngr = new ValidCharGridsManager();

		#endregion

		#region Contructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ValidCharactersDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ValidCharactersDlg()
		{
			InitializeComponent();

			splitContainerOuter.Panel2MinSize = splitValidCharsOuter.Left +
				(btnTreatAsWrdForming.Right - btnTreatAsPunct.Left);

			// Save the format string for these labels in their tags.
			lblFirstCharCode.Tag = lblFirstCharCode.Text;
			lblFirstCharCode.Text = string.Empty;
			lblLastCharCode.Tag = lblLastCharCode.Text;
			lblLastCharCode.Text = string.Empty;

			gridCharInventory.DefaultCellStyle.SelectionBackColor =
				ColorHelper.CalculateColor(SystemColors.Window, SystemColors.Highlight, 150);

			gridCharInventory.DefaultCellStyle.SelectionForeColor = SystemColors.WindowText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ValidCharactersDlg"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider"></param>
		/// <param name="langDef">The language definition of the writing system for which this
		/// dialog is setting the valid characters</param>
		/// <param name="wsName">The name of the writing system for which this dialog is setting
		/// the valid characters</param>
		/// ------------------------------------------------------------------------------------
		public ValidCharactersDlg(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			LanguageDefinition langDef, string wsName) : this()
		{
			m_cache = cache;
			m_langDef = langDef;
			m_helpTopicProvider = helpTopicProvider;
			if (string.IsNullOrEmpty(wsName))
				throw new ArgumentException("Parameter must not be null or empty.", "wsName");

			m_chrPropEng = m_cache.UnicodeCharProps;

			m_lblWsName.Text = string.Format(m_lblWsName.Text, wsName);

			// TE-6839: Temporarily remove Unicode tab (not yet implemented).
			tabCtrlAddFrom.TabPages.Remove(tabCtrlAddFrom.TabPages[kiTabUnicode]);

			m_fntForSpecialChar = new Font(SystemFonts.IconTitleFont.FontFamily, 8f);
			Font fnt = new Font(m_langDef.WritingSystem.DefaultBodyFont, 16);

			chrGridWordForming.Font = fnt;
			chrGridNumbers.Font = fnt;
			chrGridOther.Font = fnt;
			txtManualCharEntry.Font = fnt;
			txtFirstChar.Font = fnt;
			txtLastChar.Font = fnt;

			lblFirstCharCode.Top = txtFirstChar.Bottom + 5;
			lblLastCharCode.Top = txtLastChar.Bottom + 5;
			lblFirstChar.Top = txtFirstChar.Top +
				(txtFirstChar.Height - lblFirstChar.Height) / 2;
			lblLastChar.Top = txtLastChar.Top +
				(txtLastChar.Height - lblLastChar.Height) / 2;

			fnt = new Font(m_langDef.WritingSystem.DefaultBodyFont, 12);
			colChar.DefaultCellStyle.Font = fnt;

			tabData.Controls.Remove(chkSelectAll);
			tabData.Controls.Remove(gridCharInventory);
			gridCharInventory.Controls.Add(chkSelectAll);

			contextCtrl.Dock = DockStyle.Fill;
			contextCtrl.Cache = m_cache;
			contextCtrl.LanguageDefinition = m_langDef;
			contextCtrl.ContextFont = fnt;
			contextCtrl.CheckToRun = "CharactersCheck";
			contextCtrl.ListValidator = RemoveInvalidCharacters;
			contextCtrl.TokenGrid = gridCharInventory;

			if (m_langDef.WritingSystem.RightToLeft)
				contextCtrl.SetRightToLeft();

			colChar.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
			gridCharInventory.AutoGenerateColumns = false;

			colStatus.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

			m_validCharsGridMngr.Init(chrGridWordForming, chrGridOther, chrGridNumbers, m_langDef);
			m_validCharsGridMngr.CharacterGridGotFocus += HandleCharGridGotFocus;
			m_validCharsGridMngr.CharacterGridSelectionChanged += HandleCharGridSelChange;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
				if (m_fntForSpecialChar != null)
				{
					m_fntForSpecialChar.Dispose();
					m_fntForSpecialChar = null;
				}
			}

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the valid characters dialog box. Return true if OK was chosen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool RunDialog(FdoCache cache, IWin32Window owner, IHelpTopicProvider helpTopicProvider)
		{
			ILgWritingSystemFactory lgwsf = cache.LanguageWritingSystemFactoryAccessor;
			int hvoWs = cache.DefaultVernWs;
			IWritingSystem ws = lgwsf.get_EngineOrNull(hvoWs);
			LanguageDefinition langDef = new LanguageDefinition(ws);
			LgWritingSystem lgWs = new LgWritingSystem(cache, hvoWs);
			langDef.ValidChars = ws.ValidChars;

			string sWsName = lgWs.Name.UserDefaultWritingSystem;

			if (String.IsNullOrEmpty(sWsName))
				sWsName = lgWs.Name.BestAnalysisAlternative.Text;

			if (String.IsNullOrEmpty(sWsName) || sWsName == lgWs.Name.NotFoundTss.Text)
				sWsName = lgWs.Name.BestVernacularAlternative.Text;

			using (ValidCharactersDlg dlg =
				new ValidCharactersDlg(cache, helpTopicProvider, langDef, sWsName))
			{
				if (dlg.ShowDialog(owner) == DialogResult.OK)
				{
					ws.ValidChars = langDef.ValidChars;
					ws.SaveIfDirty(cache.DatabaseAccessor);
					langDef.Serialize();
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sorted inventory column.
		/// </summary>
		/// <value>The sorted inventory column.</value>
		/// ------------------------------------------------------------------------------------
		private int SortedInventoryColumn
		{
			get
			{
				foreach (DataGridViewColumn col in gridCharInventory.Columns)
				{
					if (col.HeaderCell.SortGlyphDirection != SortOrder.None)
						return col.Index;
				}

				return -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ICU locale of the similar (base) writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SimilarWritingSystemLocale
		{
			get { return m_langDef.BaseLocale; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the similar (base) writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SimilarWritingSystemName
		{
			get
			{
				if (String.IsNullOrEmpty(SimilarWritingSystemLocale))
					return null;
				ILgWritingSystemFactory qwsf = m_langDef.WritingSystem.WritingSystemFactory;
				string userWsLocale = qwsf.GetStrFromWs(m_cache.DefaultUserWs);
				string name;
				Icu.UErrorCode errorCode;
				Icu.GetDisplayName(SimilarWritingSystemLocale, userWsLocale, out name, out errorCode);
				return (errorCode == Icu.UErrorCode.U_ZERO_ERROR) ? name : null;
			}
		}

		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			// Unsubs
			chrGridWordForming.CharacterChanged -= HandleCharGridCharacterChanged;
			chrGridNumbers.CharacterChanged -= HandleCharGridCharacterChanged;
			chrGridOther.CharacterChanged -= HandleCharGridCharacterChanged;
			base.OnClosing(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Load event of the ValidCharactersDlg control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			//Set btnSililarWs to the language of m_langDef if it is one that LocaleMenuButton
			//displays in it's list.
			try
			{
				if (!btnSimilarWs.IsCustomLocale(m_langDef.LocaleAbbr, m_langDef.RootRb))
					btnSimilarWs.SelectedLocaleId = m_langDef.LocaleAbbr;
			}
			finally
			{
				m_langDef.ReleaseRootRb();
			}

			//for now the Similar Writing System radio button is invisible so do not enable or disable it.
			//when it is made Visible again and used a method may need to be created here.
			//SimilarWsEnableDisable();
			tabControlAddFrom_SelectedIndexChanged(null, e);

			cboSortOrder.SelectedIndex = 0;

			btnRemoveAll.Enabled = !m_validCharsGridMngr.IsEmpty;
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);

			// TE-8606: Disable editing writing system properties for remote users
			WritingSystemPropertiesDialog.DisableSaveIfRemoteDb(this, m_cache,
				m_langDef.LocaleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnBrowseLangFile control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnBrowseLangFile_Click(object sender, EventArgs e)
		{
			string paratext = ResourceHelper.GetResourceString("kstidParatextLanguageFiles");
			string toolbox = ResourceHelper.GetResourceString("kstidToolboxLanguageFiles");
			m_openFileDialog.Filter = String.Format("{0} ({1})|{1}|{2} ({3})|{3}",
				paratext, "*.lds", toolbox, "*.lng");

			if (m_openFileDialog.ShowDialog() == DialogResult.OK)
				txtLanguageFile.Text = m_openFileDialog.FileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Enter event of the txtLanguageFile control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void txtLanguageFile_Enter(object sender, EventArgs e)
		{
			rdoLanguageFile.Checked = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnRemoveChar control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnRemoveChar_Click(object sender, EventArgs e)
		{
			m_validCharsGridMngr.HandleRemoveClick(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the rdoSimilarWs control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void rdoSimilarWs_TextChanged(object sender, EventArgs e)
		{
			rdoSimilarWs.Height = rdoSimilarWs.PreferredSize.Height;
			if (rdoSimilarWs.PreferredSize.Width > rdoSimilarWs.Width)
				rdoSimilarWs.Height += rdoSimilarWs.PreferredSize.Height;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnAddCharacters control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnAddCharacters_Click(object sender, EventArgs e)
		{
			List<string> chars;

			// ENHANCE: Make each tab's add code a different method.
			switch (tabCtrlAddFrom.SelectedIndex)
			{
				case kiTabBasedOn:
					if (btnSimilarWs.SelectedLocaleId != null)
						AddExemplarChars(btnSimilarWs.SelectedLocaleId);
					break;

				case kiTabManual:
					if (rbSingleChar.Checked)
						AddSingleCharacter(txtManualCharEntry);
					else if (rbCharRange.Checked)
						AddRangeOfCharacters();
					else
						AddSingleCharacter(txtUnicodeValue);

					tabControlAddFrom_SelectedIndexChanged(null, null);
					break;

				case kiTabData:
					if (gridCharInventory.RowCount > 0)
					{
						chars = new List<string>();
						foreach (CharacterInventoryRow charRow in m_inventoryRows)
						{
							if (charRow.IsValid)
								chars.Add(charRow.Character);
						}

						m_validCharsGridMngr.AddCharacters(chars);
					}

					break;

				case kiTabUnicode:
					break;
			}

			btnRemoveAll.Enabled = !m_validCharsGridMngr.IsEmpty;
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a single character to the list of valid characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddSingleCharacter(TextBox txt)
		{
			Debug.Assert(txt.Text.Length > 0);

			try
			{
				string chr = null;
				bool fClearText = false;

				if (txt == txtManualCharEntry)
				{
					chr = m_chrPropEng.NormalizeD(txtManualCharEntry.Text);
					fClearText = true;
				}
				else
				{
					// Adding Unicode code point
					int codepoint;
					if (int.TryParse(txt.Text, NumberStyles.HexNumber, null, out codepoint))
						chr = ((char)codepoint).ToString();
					if (m_chrPropEng.get_IsMark(chr[0]))
					{
						ShowMessageBox(FwCoreDlgs.kstidLoneDiacriticNotValid);
						return;
					}
				}

				ValidCharacterType type = GetCharacterType(chr);
				if (type == ValidCharacterType.None)
				{
					ShowMessageBox(ResourceHelper.GetResourceString("kstidUndefinedCharacterMsg"));
				}
				else
				{
					m_validCharsGridMngr.AddCharacter(chr, type, true);
					fClearText = true;
				}

				if (fClearText)
					txt.Text = String.Empty;
			}
			finally
			{
				txt.Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a range of characters to the valid characters list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddRangeOfCharacters()
		{
			Debug.Assert(txtFirstChar.Text.Length > 0);
			Debug.Assert(txtLastChar.Text.Length > 0);
			List<string> chars = new List<string>();

			// Make sure first and last characters are in correct order.
			int first = txtFirstChar.Text[0];
			int last = txtLastChar.Text[0];
			if (first > last)
			{
				first = last;
				last = txtFirstChar.Text[0];
			}

			using (new WaitCursor(this))
			{
				List<string> undefinedChars = new List<string>();
				for (int i = first; i <= last; i++)
				{
					string chr = ((char)i).ToString();

					try
					{
						chr = m_chrPropEng.NormalizeD(chr);
					}
					catch
					{
					}

					if (StringUtils.IsCharacterDefined(chr))
						chars.Add(chr);
					else if (undefinedChars.Count < 7)
					{
						string codepoint = i.ToString("x4").ToUpperInvariant();
						undefinedChars.Add(codepoint);
					}
				}

				// If there are some undefined characters, then tell the user what
				// they are and remove them from the list of valid characters.
				if (undefinedChars.Count > 0)
				{
					string msg = ResourceHelper.GetResourceString(
						"kstidUndefinedCharactersMsg") + "\n\n";

					for (int i = 0; i < 6 && i < undefinedChars.Count; i++)
						msg += ("U+" + undefinedChars[i] + '\n');

					if (undefinedChars.Count > 6)
						msg += "...";
					else
						msg = msg.TrimEnd('\n');
					ShowMessageBox(msg);
				}

				if (chars.Count > 0)
					m_validCharsGridMngr.AddCharacters(chars);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the radio button controls on the Based On tab.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void BasedOnRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			// Have to toggle the checked state of the other radio button manually because they
			// don't share the same parent (in order to make layout easier).
			if (sender == rdoSimilarWs)
				rdoLanguageFile.Checked = !rdoSimilarWs.Checked;
			else
				rdoSimilarWs.Checked = !rdoLanguageFile.Checked;

			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnOk control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, EventArgs e)
		{
			m_validCharsGridMngr.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure only hex digits are allowed in the txtUnicodeValue text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtUnicodeValue_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (int)Keys.Back)
				return;

			int chr = e.KeyChar;
			if ((chr >= '0' && chr <= '9') ||
				(chr >= 'a' && chr <= 'f') ||
				(chr >= 'A' && chr <= 'F'))
			{
				if (chr >= 'a')
					e.KeyChar = (char)(chr - 0x20);

				return;
			}

			e.KeyChar = (char)0;
			e.Handled = true;
			IssueBeep();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the txtUnicodeValue control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void txtUnicodeValue_TextChanged(object sender, EventArgs e)
		{
			tabControlAddFrom_SelectedIndexChanged(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the txtManualCharEntry control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void txtManualCharEntry_TextChanged(object sender, EventArgs e)
		{
			if (txtManualCharEntry.Text.Length > 0)
			{
				string origCharsKd = m_chrPropEng.NormalizeD(txtManualCharEntry.Text);
				int savSelStart = txtManualCharEntry.SelectionStart;
				string newChars = StringUtils.ValidateCharacterSequence(origCharsKd, m_langDef, m_chrPropEng);

				if (newChars.Length == 0)
				{
					string s = origCharsKd.Trim();
					if (s.Length > 0 && m_chrPropEng.get_IsMark(s[0]))
						ShowMessageBox(FwCoreDlgs.kstidLoneDiacriticNotValid);
					else
						IssueBeep();
				}

				// Update the text in the textbox
				txtManualCharEntry.TextChanged -= txtManualCharEntry_TextChanged;
				txtManualCharEntry.Text = newChars;
				txtManualCharEntry.TextChanged += txtManualCharEntry_TextChanged;
				txtManualCharEntry.SelectionStart =
					(savSelStart >= newChars.Length ? newChars.Length : savSelStart);
			}

			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtFirstChar_TextChanged(object sender, EventArgs e)
		{
			VerifyCharInRange(txtFirstChar, lblFirstCharCode);
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtLastChar_TextChanged(object sender, EventArgs e)
		{
			VerifyCharInRange(txtLastChar, lblLastCharCode);
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the contents of the text box is a base character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyCharInRange(TextBox textbox, Label lbl)
		{
			string txt = m_chrPropEng.NormalizeD(textbox.Text);
			int chrCode = (txt.Length >= 1 ? txt[0] : 0);

			if (txt.Length > 1 || (chrCode > 0 && m_chrPropEng.get_IsMark(chrCode)))
			{
				IssueBeep();
				lbl.ForeColor = Color.Red;
				lbl.Text = ResourceHelper.GetResourceString("kstidNotBaseCharErrorMsg");
				textbox.Tag = "Error";
				return;
			}

			lbl.Text = (chrCode == 0 ? string.Empty :
				string.Format(lbl.Tag as string, chrCode));

			textbox.Tag = null;
			lbl.ForeColor = SystemColors.ControlText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to retrieve a set of ValidChars (ExemplarCharacters) from ICU for the language
		/// associated with the IcuLocale parameter and add those to the valid characters grids.
		/// </summary>
		/// <param name="IcuLocale"></param>
		/// <returns>Space-delimited set of characters</returns>
		/// ------------------------------------------------------------------------------------
		internal void AddExemplarChars(string IcuLocale)
		{
			string chrs = ExemplarCharactersHelper.GetValidCharsForLocale(IcuLocale, m_chrPropEng);
			if (!string.IsNullOrEmpty(chrs))
			{
				// Normalize and attempt to parse the space-delimited string into individual
				// characters and add them to the character grids.
				chrs = m_chrPropEng.NormalizeD(chrs);
				m_validCharsGridMngr.AddCharacters(StringUtils.ParseCharString(chrs, " ", m_chrPropEng));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the enabled state of the add button.
		/// </summary>
		/// <param name="sender">The source of the event (not used).</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data (not used).</param>
		/// ------------------------------------------------------------------------------------
		private void tabControlAddFrom_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool fUseWsKeyboard = false;
			switch (tabCtrlAddFrom.SelectedIndex)
			{
				case kiTabBasedOn:
					if (rdoLanguageFile.Checked)
						btnAddCharacters.Enabled = (txtLanguageFile.Text.Length > 0);
					else
						btnAddCharacters.Enabled = (btnSimilarWs.SelectedLocaleId != null);
					break;

				case kiTabManual:
					fUseWsKeyboard = !rbUnicodeValue.Checked;
					btnAddCharacters.Enabled =
						((rbUnicodeValue.Checked && txtUnicodeValue.Text.Length > 0) ||
						(rbSingleChar.Checked && txtManualCharEntry.Text.Length > 0) ||
						(rbCharRange.Checked &&	txtFirstChar.Text.Length > 0 &&
						txtLastChar.Text.Length > 0 && txtFirstChar.Tag == null &&
						txtLastChar.Tag == null));
					break;

				case kiTabData:
					btnAddCharacters.Enabled = (gridCharInventory.RowCount > 0);
					LocateSelectAllCheckBox();
					break;

				case kiTabUnicode:
					break;
			}
			if (fUseWsKeyboard)
				KeyboardHelper.ActivateKeyboard(m_langDef.WritingSystem.Locale);
			else
				KeyboardHelper.ActivateDefaultKeyboard();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void tabCtrlAddFrom_ClientSizeChanged(object sender, EventArgs e)
		{
			// There's a bug in .Net that prevents tab page backgrounds from being
			// painted properly when the tab control gets larger.
			tabCtrlAddFrom.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnRemoveAll control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnRemoveAll_Click(object sender, EventArgs e)
		{
			m_validCharsGridMngr.Reset();
			btnRemoveChar.Enabled = btnRemoveAll.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event fired when the current character changes in one of the grids.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleCharGridCharacterChanged(CharacterGrid grid, string newCharacter)
		{
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles one of the character grids in the explorer bar expanding or collapsing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleCharGridCharExplorerItemStateChanged(SimpleExplorerBar expBar,
			ExplorerBarItem item)
		{
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the event fired when one of the character grids gains focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleCharGridGotFocus(object sender, EventArgs e)
		{
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
			UpdateTreatAsButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the event fired when one of the character grids gains focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleCharGridSelChange(object sender, EventArgs e)
		{
			UpdateTreatAsButtons();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Enter event of the txtManualCharEntry control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleCharTextBoxEnter(object sender, EventArgs e)
		{
			AcceptButton = btnAddCharacters;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Enter event of the txtManualCharEntry control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleCharTextBoxLeave(object sender, EventArgs e)
		{
			AcceptButton = btnOk;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the chkSelectAll control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void chkSelectAll_CheckedChanged(object sender, EventArgs e)
		{
			if (m_inventoryRows == null)
				return;

			if (chkSelectAll.Focused)
				gridCharInventory.Focus();

			gridCharInventory.CommitEdit(DataGridViewDataErrorContexts.Commit);

			// This is a pain. For some reason, after changing the value of a check box
			// in one of the rows and immediately clicking on chkSelectAll, the value
			// in the current cell's check box doesn't change to match the value of
			// chkSelectAll. I found the problem can be solved by moving the focus
			// from the check box cell to another one in the same row.
			if (gridCharInventory.CurrentCell != null &&
				gridCharInventory.CurrentCellAddress.X == kiCharValidCol)
			{
				gridCharInventory.CurrentCell =
					gridCharInventory[kiCharCountCol, gridCharInventory.CurrentCellAddress.Y];
			}

			foreach (CharacterInventoryRow row in m_inventoryRows)
				row.IsValid = chkSelectAll.Checked;

			gridCharInventory.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the location of the select all check box keeps its proper location when
		/// columns change size.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewColumnEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			LocateSelectAllCheckBox();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RowHeightChanged event of the gridCharInventory control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewRowEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_ColumnHeadersHeightChanged(object sender, EventArgs e)
		{
			LocateSelectAllCheckBox();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines location of chkSelectAll on the "Valid?" column heading and positions it
		/// there.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LocateSelectAllCheckBox()
		{
			Rectangle rc =
				gridCharInventory.GetCellDisplayRectangle(colStatus.Index, -1, false);

			Point pt = new Point(rc.X + 3, rc.Y + (rc.Height - chkSelectAll.Height) / 2);
			if (!VisualStyleInformation.IsEnabledByUser &&
				(gridCharInventory.ColumnHeadersBorderStyle == DataGridViewHeaderBorderStyle.Raised ||
				gridCharInventory.ColumnHeadersBorderStyle == DataGridViewHeaderBorderStyle.Sunken))
			{
				pt.Y++;
			}

			chkSelectAll.Location = pt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ColumnHeaderMouseClick event of the gridCharInventory control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellMouseEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_ColumnHeaderMouseClick(object sender,
			DataGridViewCellMouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || e.ColumnIndex < 0 || m_inventoryRows == null)
				return;

			foreach (DataGridViewColumn col in gridCharInventory.Columns)
			{
				if (col.Index != e.ColumnIndex)
					col.HeaderCell.SortGlyphDirection = SortOrder.None;
				else
				{
					col.HeaderCell.SortGlyphDirection =
						(col.HeaderCell.SortGlyphDirection == SortOrder.Ascending) ?
						SortOrder.Descending : SortOrder.Ascending;

					s_inventorySortOrder = col.HeaderCell.SortGlyphDirection;
				}
			}

			SortInventoryGrid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the inventory grid column that has the sort glyph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SortInventoryGrid()
		{
			if (m_inventoryRows == null)
				return;

			//		gridCharInventory.CommitEdit(DataGridViewDataErrorContexts.Commit);

			switch (SortedInventoryColumn)
			{
				case 0: m_inventoryRows.Sort(InventoryCharComparer); break;
				case 1: m_inventoryRows.Sort(InventoryCharCodeComparer); break;
				case 2: m_inventoryRows.Sort(InventoryCountComparer); break;
				case 3: m_inventoryRows.Sort(InventoryIsValidComparer); break;
			}

			gridCharInventory.Invalidate();

			if (gridCharInventory.RowCount > 0)
				gridCharInventory.CurrentCell = gridCharInventory[0, 0];

			contextCtrl.RefreshContextGrid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellContentClick event of the gridCharInventory control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex != colStatus.Index || m_inventoryRows == null)
				return;

			gridCharInventory.CommitEdit(DataGridViewDataErrorContexts.Commit);
			bool firstRowChecked = m_inventoryRows[0].IsValid;
			foreach (CharacterInventoryRow row in m_inventoryRows)
			{
				if (row.IsValid != firstRowChecked)
				{
					chkSelectAll.CheckedChanged -= chkSelectAll_CheckedChanged;
					chkSelectAll.CheckState = CheckState.Indeterminate;
					chkSelectAll.CheckedChanged += chkSelectAll_CheckedChanged;
					return;
				}
			}

			chkSelectAll.CheckedChanged -= chkSelectAll_CheckedChanged;
			chkSelectAll.CheckState = firstRowChecked ? CheckState.Checked : CheckState.Unchecked;
			chkSelectAll.CheckedChanged += chkSelectAll_CheckedChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_inventoryRows != null && m_inventoryRows.Count > 0 && e.RowIndex >= 0 &&
				e.RowIndex < m_inventoryRows.Count && e.ColumnIndex == kiCharValidCol)
			{
				m_inventoryRows[e.RowIndex].IsValid = (bool)e.Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_inventoryRows != null && m_inventoryRows.Count > 0 &&
				e.RowIndex >= 0 && e.RowIndex < m_inventoryRows.Count)
			{
				int i = e.RowIndex;

				switch (e.ColumnIndex)
				{
					case kiCharCol:
						string chr = m_inventoryRows[i].Character;
						if (!Icu.IsSpace(chr[0]) && !Icu.IsControl(chr[0]))
						{
							e.Value = chr;
							gridCharInventory[e.ColumnIndex, e.RowIndex].Tag = null;
						}
						else
						{
							e.Value = CharacterGrid.GetSpecialCharDisplayText(chr);
							gridCharInventory[e.ColumnIndex, e.RowIndex].Tag = m_fntForSpecialChar;
						}
						return;

					case kiCharCodeCol: e.Value = m_inventoryRows[i].CharacterCodes; return;
					case kiCharCountCol: e.Value = m_inventoryRows[i].Count; return;
					case kiCharValidCol: e.Value = m_inventoryRows[i].IsValid; return;
				}
			}

			e.Value = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellFormatting event of the gridCharInventory control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellFormattingEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			DataGridViewCell cell = gridCharInventory[e.ColumnIndex, e.RowIndex];
			if (cell != null && cell.Tag is Font)
				e.CellStyle.Font = cell.Tag as Font;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get rid of the cell's focus rectangle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridCharInventory_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			DataGridViewPaintParts parts = DataGridViewPaintParts.All;
			parts &= ~DataGridViewPaintParts.Focus;
			e.Paint(e.CellBounds, parts);
			e.Handled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle selecting one of the options on the manual entry tab
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleCheckedChanged(object sender, EventArgs e)
		{
			grpSingle.Enabled = rbSingleChar.Checked;
			grpCharRange.Enabled = rbCharRange.Checked;
			grpUnicodeValue.Enabled = rbUnicodeValue.Checked;

			if (grpUnicodeValue.Enabled)
				ActiveControl = txtUnicodeValue;
			else
				ActiveControl = (grpSingle.Enabled ? txtManualCharEntry : txtFirstChar);

			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When btnSimilarWs.LocaleSelected is set to None we need to diable Add button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnSimilarWs_LocaleSelected(object sender, EventArgs e)
		{
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnTreatAsPunct control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleTreatAsClick(object sender, EventArgs e)
		{
			string currChar = m_validCharsGridMngr.CurrentGrid.CurrentCharacter;
			CharacterGrid gridTo = m_validCharsGridMngr.MoveSelectedChars();
			UpdateTreatAsButtons();

			if (gridTo != null)
			{
				gridTo.Focus();
				gridTo.CurrentCharacter = currChar;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled state of the "treat as..." buttons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateTreatAsButtons()
		{
			btnTreatAsPunct.Enabled = m_validCharsGridMngr.CanMoveToPuncSymbolList;
			btnTreatAsWrdForming.Enabled = m_validCharsGridMngr.CanMoveToWordFormingList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnHelp control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			string helpTopicKey = null;

			switch (tabCtrlAddFrom.SelectedIndex)
			{
				case kiTabBasedOn:
					helpTopicKey = "khtpValidCharsTabBasedOn";
					break;
				case kiTabManual:
					helpTopicKey = "khtpValidCharsTabManual";
					break;
				case kiTabData:
					helpTopicKey = "khtpValidCharsTabData";
					break;
				case kiTabUnicode:		//This tab is not currently visible so this help topic does not exist yet.
					helpTopicKey = "khtpValidCharsTabUnicode";
					break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Closed"/> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			KeyboardHelper.ActivateDefaultKeyboard();
		}
		#endregion

		#region Delegates
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the invalid characters.
		/// </summary>
		/// <param name="list">The list.</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveInvalidCharacters(List<TextTokenSubstring> list)
		{
			// Getting one cpe and properly disposing it instead of creating a new cpe for every
			// character in the Bible solves TE-8420, plus it is many, many times faster.
			ILgCharacterPropertyEngine cpe = m_cache.UnicodeCharProps;
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (!StringUtils.IsValidChar(list[i].InventoryText, m_langDef, cpe))
					list.RemoveAt(i);
			}
			if (Marshal.IsComObject(cpe))
				Marshal.ReleaseComObject(cpe);
		}
		#endregion

		#region Getting characters from a data source and filling the grid.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void contextCtrl_TextTokenSubStringsLoaded(List<TextTokenSubstring> tokenSubstrings)
		{
			FillInventoryGrid(tokenSubstrings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context info for the given row in the context control.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="sKey">The key into CharContextCtrl.m_contextInfoLists.</param>
		/// <param name="sConcordanceItem">The vernacular data (character) that will appear
		/// concorded in the context grid.</param>
		/// ------------------------------------------------------------------------------------
		private void contextCtrl_GetContextInfo(int index, out string sKey, out string sConcordanceItem)
		{
			try
			{
				sKey = sConcordanceItem = m_inventoryRows[index].Character;
			}
			catch
			{
				sKey = sConcordanceItem = null;
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the inventory grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillInventoryGrid(List<TextTokenSubstring> tokenSubstrings)
		{
			using (new WaitCursor(this))
			{
				contextCtrl.ResetContextLists();
				m_characterCount = new Dictionary<string, int>();
				gridCharInventory.RowCount = 0;

				if (tokenSubstrings == null)
					return;

				Dictionary<string, string> normalizedChars = new Dictionary<string, string>();
				Dictionary<string, CharacterInventoryRow> rows = new Dictionary<string, CharacterInventoryRow>();
				m_inventoryRows = new List<CharacterInventoryRow>();

				foreach (TextTokenSubstring txtTokSub in tokenSubstrings)
				{
					string chr;
					if (!normalizedChars.TryGetValue(txtTokSub.Text, out chr))
					{
						chr = m_chrPropEng.NormalizeD(txtTokSub.Text);
						if (chr == "\n" || chr == "\r" || !StringUtils.IsCharacterDefined(chr))
							chr = string.Empty;

						normalizedChars.Add(txtTokSub.Text, chr);
					}

					if (chr == string.Empty)
						continue;

					// Determine how many times this character has occurred previously and update
					// the counts for characters.
					int charCount;
					bool fAddOccurrence = true;
					if (m_characterCount.TryGetValue(chr, out charCount))
					{
						m_characterCount[chr]++;
						fAddOccurrence = (charCount < kMaxOccurrences);
					}
					else
						m_characterCount.Add(chr, 1); // First occurrence of this character

					// Only add the character occurrence to the list on the dialog if we have not
					// exceeded the threshold for this character.
					CharacterInventoryRow row;
					if (!rows.TryGetValue(chr, out row))
					{
						row = new CharacterInventoryRow(chr);
						rows[chr] = row;
						m_inventoryRows.Add(row);
						row.Count = m_characterCount[chr];
					}
					else
						row.Count = m_characterCount[chr];

					if (fAddOccurrence)
					{
						contextCtrl.AddContextInfo(new ContextInfo(chr, txtTokSub));
					}
				}

				int iSortedCol = SortedInventoryColumn;
				chkSelectAll.CheckedChanged -= chkSelectAll_CheckedChanged;
				gridCharInventory.RowCount = m_inventoryRows.Count;
				gridCharInventory.Columns[iSortedCol].HeaderCell.SortGlyphDirection = s_inventorySortOrder;
				SortInventoryGrid();
				chkSelectAll.Checked = true;
				chkSelectAll.CheckedChanged += chkSelectAll_CheckedChanged;

				if (gridCharInventory.RowCount > 0)
					gridCharInventory.CurrentCell = gridCharInventory[0, 0];

				tabControlAddFrom_SelectedIndexChanged(null, null);
				chkSelectAll.Enabled = (gridCharInventory.RowCount > 0);
			}
		}
		#endregion

		#region Character inventory sorting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares character inventory rows on the character field.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int InventoryCharComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			if (m_inventoryCharComparer == null)
				m_inventoryCharComparer = new TsStringComparer(m_langDef.IcuLocaleOriginal);

			return (s_inventorySortOrder == SortOrder.Ascending ?
				m_inventoryCharComparer.Compare(x.Character, y.Character) :
				m_inventoryCharComparer.Compare(y.Character, x.Character));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares character inventory rows on the character codes fields.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int InventoryCharCodeComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return (s_inventorySortOrder == SortOrder.Ascending ?
				x.CharacterCodes.CompareTo(y.CharacterCodes) :
				y.CharacterCodes.CompareTo(x.CharacterCodes));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares character inventory rows on the count fields.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int InventoryCountComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return (s_inventorySortOrder == SortOrder.Ascending ?
				x.Count.CompareTo(y.Count) :
				y.Count.CompareTo(x.Count));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares character inventory rows on the IsValid fields.
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int InventoryIsValidComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return (s_inventorySortOrder == SortOrder.Ascending ?
				x.IsValid.CompareTo(y.IsValid) :
				y.IsValid.CompareTo(x.IsValid));
		}

		#endregion

		#region Virtual methods to support testing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows a message box to warn the user about an invalid operation.
		/// </summary>
		/// <param name="message">The message for the user.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowMessageBox(string message)
		{
			MessageBox.Show(this, message, Application.ProductName, MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Issues a warning beep when the user performs an illegal operation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void IssueBeep()
		{
			SystemSounds.Beep.Play();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the specified character is defined as a PUA character in m_langDef, returns its
		/// character type; otherwise, returns a value that indicates whether it is a valid
		/// character as defined by the Unicode Standard.
		/// </summary>
		/// <param name="chr">The character (may consist of more than one Unicode codepoint.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual ValidCharacterType GetCharacterType(string chr)
		{
			ValidCharacterType chrType = m_langDef.GetOverrideCharType(chr);
			if (chrType != ValidCharacterType.None)
				return chrType;
			return StringUtils.IsCharacterDefined(chr) ? ValidCharacterType.DefinedUnknown : ValidCharacterType.None;
		}
		#endregion
	}

	#region CharacterInventoryRow class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for a single row in the character inventory grid.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CharacterInventoryRow
	{
		private readonly string m_chr;
		private bool m_isValid = true;
		private int m_charCount;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterInventoryRow"/> class.
		/// </summary>
		/// <param name="chr">The character for this inventory row.</param>
		/// ------------------------------------------------------------------------------------
		public CharacterInventoryRow(string chr)
		{
			m_chr = chr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Character
		{
			get { return m_chr; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the number of times the character occurs in the data source.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count
		{
			get { return m_charCount; }
			set { m_charCount = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Unicode character codes that represent the character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CharacterCodes
		{
			get { return StringUtils.CharacterCodepoints(m_chr); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the character should be added to the valid
		/// characters list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsValid
		{
			get { return m_isValid; }
			set { m_isValid = value; }
		}
	}

	#endregion
}
