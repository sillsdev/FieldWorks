// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using DialogAdapters;
using Icu;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.FieldWorks.Resources;
using SIL.Keyboarding;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;
using SIL.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Dialog for specifying the valid characters for a FieldWorks writing system
	/// </summary>
	internal partial class ValidCharactersDlg : Form
	{
		#region ValidCharGridsManager class

		/// <summary />
		protected internal class ValidCharGridsManager : IDisposable
		{
			internal EventHandler CharacterGridGotFocus;
			internal EventHandler CharacterGridSelectionChanged;
			private CharacterGrid m_gridWordForming;
			private CharacterGrid m_gridOther;
			private CharacterGrid m_currGrid;
			private ValidCharacters m_validChars;
			private CoreWritingSystemDefinition m_ws;
			private readonly ContextMenuStrip m_cmnu;
			private readonly ToolStripMenuItem m_cmnuTreatAsWrdForming;
			private readonly ToolStripMenuItem m_cmnuTreatAsNotWrdForming;
			private readonly ToolStripSeparator m_cmnuSep;

			/// <summary />
			internal ValidCharGridsManager()
			{
				m_cmnu = new ContextMenuStrip();
				m_cmnuTreatAsWrdForming = new ToolStripMenuItem(Properties.Resources.kstidTreatAsWrdForming);
				m_cmnuTreatAsNotWrdForming = new ToolStripMenuItem(Properties.Resources.kstidTreatAsOther);
				m_cmnuSep = new ToolStripSeparator();
				var cmnuRemove = new ToolStripMenuItem(Properties.Resources.kstidRemoveValidChar);

				m_cmnuTreatAsWrdForming.Click += HandleTreatAsClick;
				m_cmnuTreatAsNotWrdForming.Click += HandleTreatAsClick;
				cmnuRemove.Click += HandleRemoveClick;

				m_cmnu.Items.Add(m_cmnuTreatAsWrdForming);
				m_cmnu.Items.Add(m_cmnuTreatAsNotWrdForming);
				m_cmnu.Items.Add(m_cmnuSep);
				m_cmnu.Items.Add(cmnuRemove);
			}

			/// <summary>
			/// Initializes the valid characters explorer bar with three valid character grids.
			/// </summary>
			internal void Init(CharacterGrid gridWf, CharacterGrid gridOther, CoreWritingSystemDefinition ws, IApp app)
			{
				m_ws = ws;

				gridWf.Font = new Font(ws.DefaultFontName, gridWf.Font.Size);
				gridOther.Font = new Font(ws.DefaultFontName, gridOther.Font.Size);

				gridWf.BackgroundColor = SystemColors.Window;
				gridOther.BackgroundColor = SystemColors.Window;

				gridWf.MultiSelect = true;
				gridOther.MultiSelect = true;

				gridWf.CellPainting += HandleGridCellPainting;
				gridOther.CellPainting += HandleGridCellPainting;

				gridWf.Enter += HandleGridEnter;
				gridOther.Enter += HandleGridEnter;

				gridWf.CellFormatting += HandleCellFormatting;
				gridOther.CellFormatting += HandleCellFormatting;

				gridWf.CellMouseClick += HandleCharGridCellMouseClick;
				gridOther.CellMouseClick += HandleCharGridCellMouseClick;

				gridWf.SelectionChanged += HandleCharGridSelectionChanged;
				gridOther.SelectionChanged += HandleCharGridSelectionChanged;

				m_gridWordForming = gridWf;
				m_gridOther = gridOther;
				m_validChars = ValidCharacters.Load(ws);

				RefreshCharacterGrids(ValidCharacterType.All);
			}

			#region Disposable stuff

			/// <summary />
			~ValidCharGridsManager()
			{
				Dispose(false);
			}

			/// <summary />
			private bool IsDisposed { get; set; }

			/// <inheritdoc />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary />
			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					// dispose managed objects
					if (m_gridWordForming != null)
					{
						m_gridWordForming.CellPainting -= HandleGridCellPainting;
						m_gridWordForming.Enter -= HandleGridEnter;
						m_gridWordForming.CellFormatting -= HandleCellFormatting;
						m_gridWordForming.CellMouseClick -= HandleCharGridCellMouseClick;
						m_gridWordForming.SelectionChanged -= HandleCharGridSelectionChanged;
						m_gridWordForming.Dispose();
					}

					if (m_gridOther != null)
					{
						m_gridOther.CellPainting -= HandleGridCellPainting;
						m_gridOther.Enter -= HandleGridEnter;
						m_gridOther.CellFormatting -= HandleCellFormatting;
						m_gridOther.CellMouseClick -= HandleCharGridCellMouseClick;
						m_gridOther.SelectionChanged -= HandleCharGridSelectionChanged;
						m_gridOther.Dispose();
					}
					m_currGrid?.Dispose();
					m_cmnu?.Dispose();
				}
				// dispose unmanaged objects
				m_gridWordForming = null;
				m_gridOther = null;
				m_currGrid = null;
				IsDisposed = true;
			}
			#endregion

			#region Properties

			/// <summary>
			/// Gets or sets the current grid.
			/// </summary>
			internal CharacterGrid CurrentGrid
			{
				get => m_currGrid;
				set
				{
					if ((value == m_gridWordForming || value == m_gridOther) && value != m_currGrid)
					{
						m_currGrid = value;
						m_currGrid.EnsureSelectedRowVisible();
					}
				}
			}

			/// <summary>
			/// Gets the current character in the current grid.
			/// </summary>
			internal string CurrentCharacter => m_currGrid?.CurrentCharacter;

			/// <summary>
			/// Gets a value indicating whether all the character grids are empty.
			/// </summary>
			internal bool IsEmpty => m_gridWordForming.RowCount == 0 && m_gridOther.RowCount == 0;

			/// <summary>
			/// Gets a value indicating whether or not the current grid is the grid for word
			/// forming characters and whether or not the selected cells in that grid are allowed
			/// to be moved to the symbols, punctuation and other characters grid.
			/// </summary>
			internal bool CanMoveToPuncSymbolList => (CurrentGrid == m_gridWordForming && AreSelectedCharsOther(m_gridWordForming));

			/// <summary>
			/// Gets a value indicating whether or not the current grid is the grid for symbols,
			/// punctuation and other characters and if whether or not the selected cells in that
			/// grid are allowed to be moved to the word forming list.
			/// </summary>
			internal bool CanMoveToWordFormingList => CurrentGrid == m_gridOther && AreSelectedCharsOther(m_gridOther);
			#endregion

			#region Private and internal methods

			/// <summary>
			/// Determines whether or not all the selected characters in the specified grid fall
			/// into the category of punctuation or symbol characters.
			/// </summary>
			private bool AreSelectedCharsOther(CharacterGrid grid)
			{
				if (grid == null || grid.SelectedCells.Count == 0)
				{
					return false;
				}
				foreach (DataGridViewCell cell in grid.SelectedCells)
				{
					var chr = grid.GetCharacterAt(cell.ColumnIndex, cell.RowIndex);
					if (string.IsNullOrEmpty(chr) || chr.Length <= 0)
					{
						return false;
					}
					if (!m_validChars.CanBeWordFormingOverride(chr))
					{
						return false;
					}
				}

				return true;
			}

			/// <summary>
			/// Updates the language definition's PUA chars and valid characters.
			/// </summary>
			internal void Save()
			{
				m_validChars.SaveTo(m_ws);
			}

			/// <summary>
			/// Adds the specified character to the list of valid characters.
			/// </summary>
			protected internal void AddCharacter(string chr)
			{
				AddCharacter(chr, ValidCharacterType.DefinedUnknown, false);
			}

			/// <summary>
			/// Adds the specified character to the list of valid characters.
			/// </summary>
			protected internal virtual void AddCharacter(string chr, ValidCharacterType type, bool makeReceivingGridCurrent)
			{
				var retType = m_validChars.AddCharacter(chr, type);
				RefreshCharacterGrids(retType);

				if (makeReceivingGridCurrent || CurrentGrid == null)
				{
					switch (retType)
					{
						case ValidCharacterType.WordForming:
							CurrentGrid = m_gridWordForming;
							break;
						case ValidCharacterType.Other:
							CurrentGrid = m_gridOther;
							break;
						default:
							return;
					}

					CurrentGrid.CurrentCharacter = chr;
				}
			}

			/// <summary>
			/// Adds the list of characters to the list of valid characters.
			/// </summary>
			internal void AddCharacters(IEnumerable<string> chrs)
			{
				RefreshCharacterGrids(m_validChars.AddCharacters(chrs));

				if (CurrentGrid == null)
				{
					if (m_gridWordForming.RowCount > 0)
					{
						CurrentGrid = m_gridWordForming;
					}
					else if (m_gridOther.RowCount > 0)
					{
						CurrentGrid = m_gridOther;
					}
				}
			}

			/// <summary>
			/// Clears all the valid characters.
			/// </summary>
			internal void Reset()
			{
				m_validChars.Reset();
				m_currGrid = null;
				RefreshCharacterGrids(ValidCharacterType.All);
			}

			/// <summary>
			/// Adds the specified valid character to one of the valid character grids.
			/// </summary>
			private void RefreshCharacterGrids(ValidCharacterType type)
			{
				if ((type & ValidCharacterType.WordForming) != 0)
				{
					m_gridWordForming.RemoveAllCharacters();
					m_gridWordForming.AddCharacters(m_validChars.WordFormingCharacters);
				}

				if ((type & ValidCharacterType.Other) != 0)
				{
					m_gridOther.RemoveAllCharacters();
					m_gridOther.AddCharacters(m_validChars.OtherCharacters);
				}
			}
			#endregion

			#region Event handlers

			/// <summary>
			/// Display a context menu when the user clicks on one of the valid character grids.
			/// </summary>
			private void HandleCharGridCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
			{
				if (!(sender is CharacterGrid grid) || e.Button != MouseButtons.Right)
				{
					return;
				}
				// Make sure we have a character.
				var chr = grid.GetCharacterAt(e.ColumnIndex, e.RowIndex);
				if (string.IsNullOrEmpty(chr))
				{
					return;
				}
				grid.HideToolTip();
				var clickedCell = grid[e.ColumnIndex, e.RowIndex];
				if (!grid.SelectedCells.Contains(clickedCell))
				{
					grid.CurrentCharacter = chr;
				}
				CurrentGrid = grid;
				m_cmnuTreatAsNotWrdForming.Visible = false;
				m_cmnuTreatAsWrdForming.Visible = false;
				m_cmnuSep.Visible = false;

				if (AreSelectedCharsOther(grid))
				{
					m_cmnuSep.Visible = true;
					m_cmnuTreatAsWrdForming.Visible = grid == m_gridOther;
					m_cmnuTreatAsNotWrdForming.Visible = grid != m_gridOther;
				}

				m_cmnu.Show(MousePosition);
			}

			/// <summary>
			/// Handles the selection changed event for the word forming and other grids.
			/// </summary>
			private void HandleCharGridSelectionChanged(object sender, EventArgs e)
			{
				CharacterGridSelectionChanged?.Invoke(sender, e);
			}

			/// <summary>
			/// Handles the grid enter.
			/// </summary>
			private void HandleGridEnter(object sender, EventArgs e)
			{
				m_currGrid = sender as CharacterGrid;
				CharacterGridGotFocus?.Invoke(sender, e);
			}

			/// <summary>
			/// Handles the cell formatting.
			/// </summary>
			private void HandleCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
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

			/// <summary>
			/// Paint the background of word-forming override characters differently from all the
			/// other characters.
			/// </summary>
			private void HandleGridCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
			{
				var grid = (CharacterGrid)sender;
				var chr = grid.GetCharacterAt(e.ColumnIndex, e.RowIndex);
				var paintParts = e.PaintParts;

				if (grid != m_currGrid || string.IsNullOrEmpty(chr))
				{
					paintParts &= ~DataGridViewPaintParts.Focus;
				}
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

			/// <summary>
			/// Draws the word forming grid cell's background when it's character represents a
			/// word-forming override character.
			/// </summary>
			private void DrawWordFormingGridCellBackground(DataGridViewCellPaintingEventArgs e)
			{
				var selected = (e.State & DataGridViewElementStates.Selected) != 0;
				var clrSelBack = m_gridWordForming.DefaultCellStyle.SelectionBackColor;
				var clr1 = selected ? ColorHelper.CalculateColor(Color.White, clrSelBack, 120) : m_gridWordForming.BackgroundColor;
				var clr2 = selected ? e.CellStyle.SelectionBackColor : ColorHelper.CalculateColor(Color.White, clrSelBack, 100);
				// Use a gradient fill for the background.
				using (var br = new LinearGradientBrush(e.CellBounds, clr1, clr2, 90))
				{
					e.Graphics.FillRectangle(br, e.CellBounds);
				}
			}
			#endregion

			#region Context menu events

			/// <summary>
			/// Handles the click on one of the "Treat as..." context menu items.
			/// </summary>
			private void HandleTreatAsClick(object sender, EventArgs e)
			{
				MoveSelectedChars();
			}

			/// <summary>
			/// Moves the selected characters from the current grid to the opposing grid.
			/// </summary>
			/// <returns>The character grid to which the character was moved.</returns>
			internal CharacterGrid MoveSelectedChars()
			{
				var gridFrom = CurrentGrid;
				var gridTo = gridFrom == m_gridOther ? m_gridWordForming : m_gridOther;
				if (gridFrom == null || gridTo == null || gridFrom.SelectedCells.Count == 0)
				{
					return null;
				}
				var selChars = new List<string>();
				foreach (DataGridViewCell cell in gridFrom.SelectedCells)
				{
					selChars.Add(CurrentGrid.GetCharacterAt(cell.ColumnIndex, cell.RowIndex));
				}
				m_validChars.MoveBetweenWordFormingAndOther(selChars, gridFrom == m_gridOther);
				gridTo.AddCharacters(gridFrom.RemoveSelectedCharacters());
				return gridTo;
			}

			/// <summary>
			/// Handles the Click event of the remove context menu or Remove button.
			/// </summary>
			internal void HandleRemoveClick(object sender, EventArgs e)
			{
				// The grid from which we're removing a character is in the tag property of the
				// context menu. The character being moved is in the tag property of the grid.
				if (CurrentGrid == null || CurrentGrid.SelectedCells.Count == 0)
				{
					return;
				}
				m_validChars.RemoveCharacters(CurrentGrid.RemoveSelectedCharacters());
			}

			#endregion
		}

		#endregion

		#region Data members
		private readonly CoreWritingSystemDefinition m_ws;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly IApp m_app;
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
		private TsStringComparer m_inventoryCharComparer;
		/// <summary>Protected to facilitate testing</summary>
		protected ValidCharGridsManager m_validCharsGridMngr;

		/// <summary>Hold a reference to the writing system manager</summary>
		private WritingSystemManager m_wsManager;

		private OpenFileDialogAdapter m_openFileDialog;
		private CheckBoxColumnHeaderHandler m_chkBoxColHdrHandler;

		#endregion

		#region Contructors

		/// <summary />
		internal ValidCharactersDlg()
		{
			m_validCharsGridMngr = CreateValidCharGridsManager();

			InitializeComponent();
			AccessibleName = GetType().Name;

			m_openFileDialog = new OpenFileDialogAdapter();
			m_openFileDialog.Title = FwCoreDlgs.kstidLanguageFileBrowser;
			splitContainerOuter.Panel2MinSize = splitValidCharsOuter.Left + (btnTreatAsWrdForming.Right - btnTreatAsPunct.Left);

			// Save the format string for these labels in their tags.
			lblFirstCharCode.Tag = lblFirstCharCode.Text;
			lblFirstCharCode.Text = string.Empty;
			lblLastCharCode.Tag = lblLastCharCode.Text;
			lblLastCharCode.Text = string.Empty;

			gridCharInventory.DefaultCellStyle.SelectionBackColor = ColorHelper.CalculateColor(SystemColors.Window, SystemColors.Highlight, 150);

			gridCharInventory.DefaultCellStyle.SelectionForeColor = SystemColors.WindowText;
		}

		/// <summary />
		/// <param name="cache">The cache. Can be <c>null</c> if called from New Project
		/// dialog.</param>
		/// <param name="wsContainer">The LCM writing system container. Can't be
		/// <c>null</c>.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="ws">The language definition of the writing system for which this
		/// dialog is setting the valid characters. Can not be <c>null</c>.</param>
		/// <param name="wsName">The name of the writing system for which this dialog is setting
		/// the valid characters. Can not be <c>null</c> or <c>String.Empty</c>.</param>
		internal ValidCharactersDlg(LcmCache cache, IWritingSystemContainer wsContainer, IHelpTopicProvider helpTopicProvider, IApp app, CoreWritingSystemDefinition ws, string wsName)
			: this()
		{
			m_ws = ws;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			if (string.IsNullOrEmpty(wsName))
			{
				throw new ArgumentException("Parameter must not be null or empty.", nameof(wsName));
			}
			if (cache != null)
			{
				m_wsManager = cache.ServiceLocator.WritingSystemManager;
			}
			m_lblWsName.Text = string.Format(m_lblWsName.Text, wsName);

			// TE-6839: Temporarily remove Unicode tab (not yet implemented).
			tabCtrlAddFrom.TabPages.Remove(tabCtrlAddFrom.TabPages[kiTabUnicode]);

			m_fntForSpecialChar = new Font(SystemFonts.IconTitleFont.FontFamily, 8f);
			var fnt = new Font(m_ws.DefaultFontName, 16);

			// Review - each of the following Font property set causes LoadCharsFromFont
			// to be executed which is an expensive operation as it pinvokes GetGlyphIndices
			// repeatedly.
			chrGridWordForming.Font = fnt;
			chrGridOther.Font = fnt;
			txtManualCharEntry.Font = fnt;
			txtFirstChar.Font = fnt;
			txtLastChar.Font = fnt;

			lblFirstCharCode.Top = txtFirstChar.Bottom + 5;
			lblLastCharCode.Top = txtLastChar.Bottom + 5;
			lblFirstChar.Top = txtFirstChar.Top + (txtFirstChar.Height - lblFirstChar.Height) / 2;
			lblLastChar.Top = txtLastChar.Top + (txtLastChar.Height - lblLastChar.Height) / 2;

			fnt = new Font(m_ws.DefaultFontName, 12);
			colChar.DefaultCellStyle.Font = fnt;

			tabData.Controls.Remove(gridCharInventory);
			var gridcol = gridCharInventory.Columns[3];
			m_chkBoxColHdrHandler = new CheckBoxColumnHeaderHandler(gridcol)
			{
				Label = gridcol.HeaderText
			};
			gridcol.HeaderText = string.Empty;

			contextCtrl.Initialize(cache, wsContainer, m_ws, m_app, fnt, gridCharInventory);
			contextCtrl.Dock = DockStyle.Fill;
			contextCtrl.CheckToRun = CheckType.Characters;

			colChar.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
			gridCharInventory.AutoGenerateColumns = false;

			colStatus.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;

			m_validCharsGridMngr.Init(chrGridWordForming, chrGridOther, m_ws, m_app);
			m_validCharsGridMngr.CharacterGridGotFocus += HandleCharGridGotFocus;
			m_validCharsGridMngr.CharacterGridSelectionChanged += HandleCharGridSelChange;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "********** Missing Dispose() call for " + GetType().Name + ". **********");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				m_openFileDialog?.Dispose();
				m_fntForSpecialChar?.Dispose();
				m_validCharsGridMngr?.Dispose();
				m_chkBoxColHdrHandler?.Dispose();
				components?.Dispose();
			}

			m_fntForSpecialChar = null;
			m_openFileDialog = null;
			m_validCharsGridMngr = null;
			m_chkBoxColHdrHandler = null;
			m_inventoryCharComparer = null;
			m_openFileDialog = null;

			base.Dispose(disposing);

		}

		/// <summary>
		/// Creates a new ValidCharGridsManager. This method is here so we can override it in tests.
		/// </summary>
		protected virtual ValidCharGridsManager CreateValidCharGridsManager()
		{
			return new ValidCharGridsManager();
		}

		/// <summary>
		/// Display the valid characters dialog box. Return true if OK was chosen.
		/// </summary>
		internal static bool RunDialog(LcmCache cache, IApp app, IWin32Window owner, IHelpTopicProvider helpTopicProvider)
		{
			var ws = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			using (var dlg = new ValidCharactersDlg(cache, cache.ServiceLocator.WritingSystems, helpTopicProvider, app, ws, ws.DisplayLabel))
			{
				if (dlg.ShowDialog(owner) == DialogResult.OK)
				{
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the sorted inventory column.
		/// </summary>
		private int SortedInventoryColumn
		{
			get
			{
				foreach (DataGridViewColumn col in gridCharInventory.Columns)
				{
					if (col.HeaderCell.SortGlyphDirection != SortOrder.None)
					{
						return col.Index;
					}
				}
				return -1;
			}
		}

		/// <summary>
		/// Gets the Writing System Manager. If necessary it creates a temporary one that gets
		/// disposed later on.
		/// </summary>
		private WritingSystemManager WsManager
		{
			get
			{
				if (m_wsManager == null)
				{
					Debug.Assert(m_ws != null);
					m_wsManager = FwUtils.CreateWritingSystemManager();
					m_wsManager.Set(m_ws);
				}
				return m_wsManager;
			}
		}
		#endregion

		#region Event Handlers

		/// <inheritdoc />
		protected override void OnClosing(CancelEventArgs e)
		{
			// Unsubs
			chrGridWordForming.CharacterChanged -= HandleCharGridCharacterChanged;
			chrGridOther.CharacterChanged -= HandleCharGridCharacterChanged;
			base.OnClosing(e);
		}

		/// <inheritdoc />
		protected override void OnLoad(EventArgs e)
		{
			//Set btnSililarWs to the language of m_langDef if it is one that LocaleMenuButton
			//displays in it's list.
			var code = m_ws.Language.Code;
			if (!btnSimilarWs.IsCustomLocale(code))
			{
				btnSimilarWs.SelectedLocaleId = code;
			}
			//for now the Similar Writing System radio button is invisible so do not enable or disable it.
			//when it is made Visible again and used a method may need to be created here.
			tabControlAddFrom_SelectedIndexChanged(null, e);

			cboSortOrder.SelectedIndex = 0;

			btnRemoveAll.Enabled = !m_validCharsGridMngr.IsEmpty;
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
		}

		/// <remarks>
		/// REVIEW (Hasso) 2017.07: this button and most other controls in the Based On tab are apparently never visible. If we want to maintain the
		/// import functionality from Paratext and Toolbox, we should make these controls viewable--although it seems that functionality
		/// has moved to the From Data tab (see CharContextControl). (REVIEW) so it is likely safe to remove these invisible controls entirely.
		/// </remarks>
		private void btnBrowseLangFile_Click(object sender, EventArgs e)
		{
			var paratext = ResourceHelper.GetResourceString("kstidParatextLanguageFiles");
			var toolbox = ResourceHelper.GetResourceString("kstidToolboxLanguageFiles");
			m_openFileDialog.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(string.Format("{0} ({1})|{1}|{2} ({3})|{3}", paratext, "*.lds", toolbox, "*.lng"));
			if (m_openFileDialog.ShowDialog() == DialogResult.OK)
			{
				txtLanguageFile.Text = m_openFileDialog.FileName;
			}
		}

		/// <summary>
		/// Handles the Enter event of the txtLanguageFile control.
		/// </summary>
		private void txtLanguageFile_Enter(object sender, EventArgs e)
		{
			rdoLanguageFile.Checked = true;
		}

		/// <summary>
		/// Handles the Click event of the btnRemoveChar control.
		/// </summary>
		private void btnRemoveChar_Click(object sender, EventArgs e)
		{
			m_validCharsGridMngr.HandleRemoveClick(sender, e);
		}

		/// <summary>
		/// Handles the TextChanged event of the rdoSimilarWs control.
		/// </summary>
		private void rdoSimilarWs_TextChanged(object sender, EventArgs e)
		{
			rdoSimilarWs.Height = rdoSimilarWs.PreferredSize.Height;
			if (rdoSimilarWs.PreferredSize.Width > rdoSimilarWs.Width)
			{
				rdoSimilarWs.Height += rdoSimilarWs.PreferredSize.Height;
			}
		}

		/// <summary>
		/// Handles the Click event of the btnAddCharacters control.
		/// </summary>
		private void btnAddCharacters_Click(object sender, EventArgs e)
		{
			// ENHANCE: Make each tab's add code a different method.
			switch (tabCtrlAddFrom.SelectedIndex)
			{
				case kiTabBasedOn:
					if (btnSimilarWs.SelectedLocaleId != null)
					{
						AddExemplarChars(btnSimilarWs.SelectedLocaleId);
					}
					break;
				case kiTabManual:
					if (rbSingleChar.Checked)
					{
						AddSingleCharacter(txtManualCharEntry);
					}
					else if (rbCharRange.Checked)
					{
						AddRangeOfCharacters();
					}
					else
					{
						AddSingleCharacter(txtUnicodeValue);
					}
					tabControlAddFrom_SelectedIndexChanged(null, null);
					break;
				case kiTabData:
					if (gridCharInventory.RowCount > 0)
					{
						m_validCharsGridMngr.AddCharacters(m_inventoryRows.Where(charRow => charRow.IsValid).Select(charRow => charRow.Character).ToList());
					}
					break;
				case kiTabUnicode:
					break;
			}

			btnRemoveAll.Enabled = !m_validCharsGridMngr.IsEmpty;
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
		}

		/// <summary>
		/// Adds a single character to the list of valid characters.
		/// </summary>
		protected void AddSingleCharacter(FwTextBox txt)
		{
			Debug.Assert(txt.Text.Length > 0);

			try
			{
				string chr = null;
				var fClearText = false;
				if (txt == txtManualCharEntry)
				{
					chr = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(txtManualCharEntry.Text);
					fClearText = true;
				}
				else
				{
					// Adding Unicode code point
					if (int.TryParse(txt.Text, NumberStyles.HexNumber, null, out var codepoint))
					{
						chr = ((char)codepoint).ToString(CultureInfo.InvariantCulture);
						if (Character.IsMark(chr[0]))
						{
							ShowMessageBox(FwCoreDlgs.kstidLoneDiacriticNotValid);
							return;
						}
					}
				}

				var type = GetCharacterType(chr);
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
				{
					txt.Text = string.Empty;
				}
			}
			finally
			{
				txt.Focus();
			}
		}

		/// <summary>
		/// Adds a range of characters to the valid characters list.
		/// </summary>
		private void AddRangeOfCharacters()
		{
			Debug.Assert(txtFirstChar.Text.Length > 0);
			Debug.Assert(txtLastChar.Text.Length > 0);
			var chars = new List<string>();

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
				var undefinedChars = new List<string>();
				for (var i = first; i <= last; i++)
				{
					var chr = ((char)i).ToString(CultureInfo.InvariantCulture);
					try
					{
						if (!string.IsNullOrEmpty(chr))
						{
							chr = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(chr);
						}
					}
					catch
					{
					}

					if (TsStringUtils.IsCharacterDefined(chr))
					{
						chars.Add(chr);
					}
					else if (undefinedChars.Count < 7)
					{
						var codepoint = i.ToString("x4").ToUpperInvariant();
						undefinedChars.Add(codepoint);
					}
				}

				// If there are some undefined characters, then tell the user what
				// they are and remove them from the list of valid characters.
				if (undefinedChars.Count > 0)
				{
					var bldr = new StringBuilder();
					bldr.AppendLine(ResourceHelper.GetResourceString("kstidUndefinedCharactersMsg"));
					bldr.AppendLine();

					for (var i = 0; i < 6 && i < undefinedChars.Count; i++)
					{
						bldr.AppendLine("U+" + undefinedChars[i]);
					}
					if (undefinedChars.Count > 6)
					{
						bldr.Append("...");
					}
					ShowMessageBox(bldr.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
				}

				if (chars.Count > 0)
				{
					m_validCharsGridMngr.AddCharacters(chars);
				}
			}
		}

		/// <summary>
		/// Handles the CheckedChanged event of the radio button controls on the Based On tab.
		/// </summary>
		private void BasedOnRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			// Have to toggle the checked state of the other radio button manually because they
			// don't share the same parent (in order to make layout easier).
			if (sender == rdoSimilarWs)
			{
				rdoLanguageFile.Checked = !rdoSimilarWs.Checked;
			}
			else
			{
				rdoSimilarWs.Checked = !rdoLanguageFile.Checked;
			}
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// <summary>
		/// Handles the Click event of the btnOk control.
		/// </summary>
		private void btnOk_Click(object sender, EventArgs e)
		{
			m_validCharsGridMngr.Save();
		}

		/// <summary>
		/// Make sure only hex digits are allowed in the txtUnicodeValue text box.
		/// </summary>
		private void txtUnicodeValue_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (int)Keys.Back)
			{
				return;
			}
			int chr = e.KeyChar;
			if (chr >= '0' && chr <= '9' || chr >= 'a' && chr <= 'f' || chr >= 'A' && chr <= 'F')
			{
				if (chr >= 'a')
				{
					e.KeyChar = (char)(chr - 0x20);
				}
				return;
			}

			e.KeyChar = (char)0;
			e.Handled = true;
			IssueBeep();
		}

		/// <summary>
		/// Handles the TextChanged event of the txtUnicodeValue control.
		/// </summary>
		private void txtUnicodeValue_TextChanged(object sender, EventArgs e)
		{
			tabControlAddFrom_SelectedIndexChanged(null, null);
		}

		/// <summary>
		/// Handles the TextChanged event of the txtManualCharEntry control.
		/// </summary>
		private void txtManualCharEntry_TextChanged(object sender, EventArgs e)
		{
			if (txtManualCharEntry.Text.Length > 0)
			{
				var origCharsKd = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(txtManualCharEntry.Text);
				var savSelStart = txtManualCharEntry.SelectionStart;
				var newChars = TsStringUtils.ValidateCharacterSequence(origCharsKd);

				if (newChars.Length == 0)
				{
					var s = origCharsKd.Trim();
					if (s.Length > 0 && Character.IsMark(s[0]))
					{
						ShowMessageBox(FwCoreDlgs.kstidLoneDiacriticNotValid);
					}
					else
					{
						IssueBeep();
					}
				}
				// Update the text in the textbox
				txtManualCharEntry.TextChanged -= txtManualCharEntry_TextChanged;
				// Throw away any uncommitted typing.
				txtManualCharEntry.Selection?.RootBox?.DestroySelection();
				txtManualCharEntry.Text = newChars;
				txtManualCharEntry.TextChanged += txtManualCharEntry_TextChanged;
				txtManualCharEntry.SelectionStart = savSelStart >= newChars.Length ? newChars.Length : savSelStart;
			}

			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// <summary />
		private void txtFirstChar_TextChanged(object sender, EventArgs e)
		{
			VerifyCharInRange(txtFirstChar, lblFirstCharCode);
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// <summary />
		private void txtLastChar_TextChanged(object sender, EventArgs e)
		{
			VerifyCharInRange(txtLastChar, lblLastCharCode);
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// <summary>
		/// Verifies the contents of the text box is a base character.
		/// </summary>
		private void VerifyCharInRange(FwTextBox textbox, Label lbl)
		{
			var txt = textbox.Text.Length >= 1 ? CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(textbox.Text) : string.Empty;
			var chrCode = txt.Length >= 1 ? txt[0] : 0;
			if (txt.Length > 1 || chrCode > 0 && Character.IsMark(chrCode))
			{
				IssueBeep();
				lbl.ForeColor = Color.Red;
				lbl.Text = ResourceHelper.GetResourceString("kstidNotBaseCharErrorMsg");
				textbox.Tag = "Error";
				return;
			}

			lbl.Text = chrCode == 0 ? string.Empty : string.Format((string)lbl.Tag, chrCode);
			textbox.Tag = null;
			lbl.ForeColor = SystemColors.ControlText;
		}

		/// <summary>
		/// Try to retrieve a set of ValidChars (ExemplarCharacters) from ICU for the language
		/// associated with the IcuLocale parameter and add those to the valid characters grids.
		/// </summary>
		/// <param name="icuLocale"></param>
		/// <returns>Space-delimited set of characters</returns>
		internal void AddExemplarChars(string icuLocale)
		{
			if (icuLocale == null)
			{
				return;
			}
			var chars = new List<string>();
			foreach (var c in UnicodeSet.ToCharacters(CustomIcu.GetExemplarCharacters(icuLocale)))
			{
				chars.Add(c.Normalize(NormalizationForm.FormD));
				// ENHANCE (Hasso) 2022.02: use CaseFunctions (checks for CaseAlias, but users can still add any character that's already uppercase)
				chars.Add(UnicodeString.ToUpper(c, icuLocale).Normalize(NormalizationForm.FormD));
			}
			m_validCharsGridMngr.AddCharacters(chars);
		}

		/// <summary>
		/// Sets the enabled state of the add button.
		/// </summary>
		private void tabControlAddFrom_SelectedIndexChanged(object sender, EventArgs e)
		{
			var fUseWsKeyboard = false;
			switch (tabCtrlAddFrom.SelectedIndex)
			{
				case kiTabBasedOn:
					if (rdoLanguageFile.Checked)
					{
						btnAddCharacters.Enabled = (txtLanguageFile.Text.Length > 0);
					}
					else
					{
						btnAddCharacters.Enabled = (btnSimilarWs.SelectedLocaleId != null);
					}
					break;
				case kiTabManual:
					fUseWsKeyboard = !rbUnicodeValue.Checked;
					btnAddCharacters.Enabled = rbUnicodeValue.Checked && txtUnicodeValue.Text.Length > 0 || rbSingleChar.Checked && txtManualCharEntry.Text.Length > 0 ||
						rbCharRange.Checked && txtFirstChar.Text.Length > 0 && txtLastChar.Text.Length > 0 && txtFirstChar.Tag == null && txtLastChar.Tag == null;
					break;
				case kiTabData:
					btnAddCharacters.Enabled = (gridCharInventory.RowCount > 0);
					break;
				case kiTabUnicode:
					break;
			}
			if (fUseWsKeyboard)
			{
				m_ws.LocalKeyboard.Activate();
			}
			else
			{
				Keyboard.Controller.ActivateDefaultKeyboard();
			}
		}

		/// <summary />
		private void tabCtrlAddFrom_ClientSizeChanged(object sender, EventArgs e)
		{
			// There's a bug in .Net that prevents tab page backgrounds from being
			// painted properly when the tab control gets larger.
			tabCtrlAddFrom.Invalidate();
		}

		/// <summary>
		/// Handles the Click event of the btnRemoveAll control.
		/// </summary>
		private void btnRemoveAll_Click(object sender, EventArgs e)
		{
			m_validCharsGridMngr.Reset();
			btnRemoveChar.Enabled = btnRemoveAll.Enabled = false;
		}

		/// <summary>
		/// Event fired when the current character changes in one of the grids.
		/// </summary>
		private void HandleCharGridCharacterChanged(CharacterGrid grid, string newCharacter)
		{
			// Can happen in tests.
			if (m_validCharsGridMngr != null)
			{
				btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
			}
		}

		/// <summary>
		/// Handles the event fired when one of the character grids gains focus.
		/// </summary>
		private void HandleCharGridGotFocus(object sender, EventArgs e)
		{
			btnRemoveChar.Enabled = !string.IsNullOrEmpty(m_validCharsGridMngr.CurrentCharacter);
			UpdateTreatAsButtons();
		}

		/// <summary>
		/// Handles the event fired when one of the character grids gains focus.
		/// </summary>
		private void HandleCharGridSelChange(object sender, EventArgs e)
		{
			UpdateTreatAsButtons();
		}

		/// <summary>
		/// Handles the Enter event of the txtManualCharEntry control.
		/// </summary>
		private void HandleCharTextBoxEnter(object sender, EventArgs e)
		{
			AcceptButton = btnAddCharacters;

			// Ensure keyboard associated for writing system is active is used in the FwTextBox's.
			var textBox = sender as FwTextBox;
			Debug.Assert(textBox != null); // This event handler should only be used for FwTextBox's
			if (textBox == null)
			{
				return;
			}
			textBox.WritingSystemFactory = WsManager;
			// Get WS Code from WsManager instead of using Handle - ws might not be completely
			// set up (LT-19904)
			var wsCode = WsManager.GetWsFromStr(m_ws.Id);
			textBox.WritingSystemCode = wsCode;
		}

		/// <summary>
		/// Handles the Enter event of the txtManualCharEntry control.
		/// </summary>
		private void HandleCharTextBoxLeave(object sender, EventArgs e)
		{
			AcceptButton = btnOk;
		}

		/// <summary>
		/// Handles the ColumnHeaderMouseClick event of the gridCharInventory control.
		/// </summary>
		private void gridCharInventory_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || e.ColumnIndex < 0 || m_inventoryRows == null)
			{
				return;
			}
			if (m_chkBoxColHdrHandler.IsClickInCheckBox(e))
			{
				return;
			}
			foreach (DataGridViewColumn col in gridCharInventory.Columns)
			{
				if (col.Index != e.ColumnIndex)
				{
					col.HeaderCell.SortGlyphDirection = SortOrder.None;
				}
				else
				{
					col.HeaderCell.SortGlyphDirection = col.HeaderCell.SortGlyphDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
					s_inventorySortOrder = col.HeaderCell.SortGlyphDirection;
				}
			}

			SortInventoryGrid();
		}

		/// <summary>
		/// Sorts the inventory grid column that has the sort glyph.
		/// </summary>
		private void SortInventoryGrid()
		{
			if (m_inventoryRows == null)
			{
				return;
			}

			switch (SortedInventoryColumn)
			{
				case 0:
					m_inventoryRows.Sort(InventoryCharComparer);
					break;
				case 1:
					m_inventoryRows.Sort(InventoryCharCodeComparer);
					break;
				case 2:
					m_inventoryRows.Sort(InventoryCountComparer);
					break;
				case 3:
					m_inventoryRows.Sort(InventoryIsValidComparer);
					break;
			}

			gridCharInventory.Invalidate();

			if (gridCharInventory.RowCount > 0)
			{
				gridCharInventory.CurrentCell = gridCharInventory[0, 0];
			}
			contextCtrl.RefreshContextGrid();
		}

		/// <summary />
		private void gridCharInventory_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_inventoryRows != null && m_inventoryRows.Count > 0 && e.RowIndex >= 0 && e.RowIndex < m_inventoryRows.Count && e.ColumnIndex == kiCharValidCol)
			{
				m_inventoryRows[e.RowIndex].IsValid = (bool)e.Value;
			}
		}

		/// <summary />
		private void gridCharInventory_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			if (m_inventoryRows != null && m_inventoryRows.Count > 0 && e.RowIndex >= 0 && e.RowIndex < m_inventoryRows.Count)
			{
				var i = e.RowIndex;
				switch (e.ColumnIndex)
				{
					case kiCharCol:
						var chr = m_inventoryRows[i].Character;
						if (!Character.IsSpace(chr[0]) && !Character.IsControl(chr[0]))
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
					case kiCharCodeCol:
						e.Value = m_inventoryRows[i].CharacterCodes;
						return;
					case kiCharCountCol:
						e.Value = m_inventoryRows[i].Count;
						return;
					case kiCharValidCol:
						e.Value = m_inventoryRows[i].IsValid;
						return;
				}
			}

			e.Value = null;
		}

		/// <summary>
		/// Handles the CellFormatting event of the gridCharInventory control.
		/// </summary>
		private void gridCharInventory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (gridCharInventory[e.ColumnIndex, e.RowIndex]?.Tag is Font font)
			{
				e.CellStyle.Font = font;
			}
		}

		/// <summary>
		/// Get rid of the cell's focus rectangle.
		/// </summary>
		private void gridCharInventory_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			var parts = DataGridViewPaintParts.All;
			parts &= ~DataGridViewPaintParts.Focus;
			e.Paint(e.CellBounds, parts);
			e.Handled = true;
		}

		/// <summary>
		/// Handle selecting one of the options on the manual entry tab
		/// </summary>
		private void HandleCheckedChanged(object sender, EventArgs e)
		{
			grpSingle.Enabled = rbSingleChar.Checked;
			grpCharRange.Enabled = rbCharRange.Checked;
			grpUnicodeValue.Enabled = rbUnicodeValue.Checked;

			ActiveControl = grpUnicodeValue.Enabled ? txtUnicodeValue : grpSingle.Enabled ? txtManualCharEntry : txtFirstChar;
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// <summary>
		/// When btnSimilarWs.LocaleSelected is set to None we need to disable Add button.
		/// </summary>
		private void btnSimilarWs_LocaleSelected(object sender, EventArgs e)
		{
			tabControlAddFrom_SelectedIndexChanged(sender, e);
		}

		/// <summary>
		/// Handles the Click event of the btnTreatAsPunct control.
		/// </summary>
		private void HandleTreatAsClick(object sender, EventArgs e)
		{
			var currChar = m_validCharsGridMngr.CurrentGrid.CurrentCharacter;
			var gridTo = m_validCharsGridMngr.MoveSelectedChars();
			UpdateTreatAsButtons();

			if (gridTo != null)
			{
				gridTo.Focus();
				gridTo.CurrentCharacter = currChar;
			}
		}

		/// <summary>
		/// Updates the enabled state of the "treat as..." buttons.
		/// </summary>
		private void UpdateTreatAsButtons()
		{
			btnTreatAsPunct.Enabled = m_validCharsGridMngr.CanMoveToPuncSymbolList;
			btnTreatAsWrdForming.Enabled = m_validCharsGridMngr.CanMoveToWordFormingList;
		}

		/// <summary>
		/// Handles the Click event of the btnHelp control.
		/// </summary>
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
				case kiTabUnicode:      //This tab is not currently visible so this help topic does not exist yet.
					helpTopicKey = "khtpValidCharsTabUnicode";
					break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
		}

		/// <inheritdoc />
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Keyboard.Controller.ActivateDefaultKeyboard();
		}
		#endregion

		#region Getting characters from a data source and filling the grid.

		/// <summary />
		private void contextCtrl_TextTokenSubStringsLoaded(List<TextTokenSubstring> tokenSubstrings)
		{
			FillInventoryGrid(tokenSubstrings);
		}

		/// <summary>
		/// Gets the context info for the given row in the context control.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="sKey">The key into CharContextCtrl.m_contextInfoLists.</param>
		/// <param name="sConcordanceItem">The vernacular data (character) that will appear
		/// concorded in the context grid.</param>
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

		/// <summary>
		/// Fills the inventory grid.
		/// </summary>
		private void FillInventoryGrid(List<TextTokenSubstring> tokenSubstrings)
		{
			using (new WaitCursor(this))
			{
				contextCtrl.ResetContextLists();
				m_characterCount = new Dictionary<string, int>();
				gridCharInventory.RowCount = 0;

				if (tokenSubstrings == null)
				{
					return;
				}
				var normalizedChars = new Dictionary<string, string>();
				var rows = new Dictionary<string, CharacterInventoryRow>();
				m_inventoryRows = new List<CharacterInventoryRow>();

				foreach (var txtTokSub in tokenSubstrings)
				{
					if (!normalizedChars.TryGetValue(txtTokSub.Text, out var chr))
					{
						chr = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(txtTokSub.Text);
						if (chr == "\n" || chr == "\r" || !TsStringUtils.IsCharacterDefined(chr) || !TsStringUtils.IsValidChar(chr))
						{
							chr = string.Empty;
						}
						normalizedChars.Add(txtTokSub.Text, chr);
					}
					if (chr == string.Empty)
					{
						continue;
					}
					// Determine how many times this character has occurred previously and update
					// the counts for characters.
					var fAddOccurrence = true;
					if (m_characterCount.TryGetValue(chr, out var charCount))
					{
						m_characterCount[chr]++;
						fAddOccurrence = (charCount < kMaxOccurrences);
					}
					else
					{
						m_characterCount.Add(chr, 1); // First occurrence of this character
					}
					// Only add the character occurrence to the list on the dialog if we have not
					// exceeded the threshold for this character.
					if (!rows.TryGetValue(chr, out var row))
					{
						row = new CharacterInventoryRow(chr);
						rows[chr] = row;
						m_inventoryRows.Add(row);
						row.Count = m_characterCount[chr];
					}
					else
					{
						row.Count = m_characterCount[chr];
					}
					if (fAddOccurrence)
					{
						contextCtrl.AddContextInfo(new ContextInfo(chr, txtTokSub));
					}
				}

				var iSortedCol = SortedInventoryColumn;
				gridCharInventory.RowCount = m_inventoryRows.Count;
				gridCharInventory.Columns[iSortedCol].HeaderCell.SortGlyphDirection = s_inventorySortOrder;
				SortInventoryGrid();

				if (gridCharInventory.RowCount > 0)
				{
					gridCharInventory.CurrentCell = gridCharInventory[0, 0];
				}
				tabControlAddFrom_SelectedIndexChanged(null, null);
			}
		}
		#endregion

		#region Character inventory sorting methods

		/// <summary>
		/// Compares character inventory rows on the character field.
		/// </summary>
		private int InventoryCharComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			switch (x)
			{
				case null when y == null:
					return 0;
				case null:
					return -1;
			}
			if (y == null)
			{
				return 1;
			}
			if (m_inventoryCharComparer == null)
			{
				m_inventoryCharComparer = new TsStringComparer(m_ws);
			}
			return s_inventorySortOrder == SortOrder.Ascending ? m_inventoryCharComparer.Compare(x.Character, y.Character) : m_inventoryCharComparer.Compare(y.Character, x.Character);
		}

		/// <summary>
		/// Compares character inventory rows on the character codes field.
		/// </summary>
		private static int InventoryCharCodeComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			switch (x)
			{
				case null when y == null:
					return 0;
				case null:
					return -1;
			}
			if (y == null)
			{
				return 1;
			}
			return s_inventorySortOrder == SortOrder.Ascending ? x.CharacterCodes.CompareTo(y.CharacterCodes) : y.CharacterCodes.CompareTo(x.CharacterCodes);
		}

		/// <summary>
		/// Compares character inventory rows on the count field.
		/// </summary>
		private static int InventoryCountComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			switch (x)
			{
				case null when y == null:
					return 0;
				case null:
					return -1;
			}
			if (y == null)
			{
				return 1;
			}
			return s_inventorySortOrder == SortOrder.Ascending ? x.Count.CompareTo(y.Count) : y.Count.CompareTo(x.Count);
		}

		/// <summary>
		/// Compares character inventory rows on the IsValid field.
		/// </summary>
		private int InventoryIsValidComparer(CharacterInventoryRow x, CharacterInventoryRow y)
		{
			switch (x)
			{
				case null when y == null:
					return 0;
				case null:
					return -1;
			}
			if (y == null)
			{
				return 1;
			}
			var retval = s_inventorySortOrder == SortOrder.Ascending ? x.IsValid.CompareTo(y.IsValid) : y.IsValid.CompareTo(x.IsValid);
			// Use the character as a secondary sort field, since many if not most comparisons
			// will yield a 0 (same value), which could randomize a large number of items.
			return retval == 0 ? InventoryCharComparer(x, y) : retval;
		}

		#endregion

		#region Virtual methods to support testing

		/// <summary>
		/// Shows a message box to warn the user about an invalid operation.
		/// </summary>
		protected virtual void ShowMessageBox(string message)
		{
			MessageBoxUtils.Show(this, message, m_app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		/// <summary>
		/// Issues a warning beep when the user performs an illegal operation.
		/// </summary>
		protected virtual void IssueBeep()
		{
			FwUtils.ErrorBeep();
		}

		/// <summary>
		/// If the specified character is defined as a PUA character in m_langDef, returns its
		/// character type; otherwise, returns a value that indicates whether it is a valid
		/// character as defined by the Unicode Standard.
		/// </summary>
		/// <param name="chr">The character (may consist of more than one Unicode codepoint.</param>
		protected virtual ValidCharacterType GetCharacterType(string chr)
		{
			return TsStringUtils.IsCharacterDefined(chr) ? ValidCharacterType.DefinedUnknown : ValidCharacterType.None;
		}
		#endregion

		/// <summary>
		/// Class for a single row in the character inventory grid.
		/// </summary>
		private sealed class CharacterInventoryRow
		{
			/// <summary />
			internal CharacterInventoryRow(string chr)
			{
				Character = chr;
			}

			/// <summary>
			/// Gets the character.
			/// </summary>
			internal string Character { get; }

			/// <summary>
			/// Gets or sets the number of times the character occurs in the data source.
			/// </summary>
			internal int Count { get; set; }

			/// <summary>
			/// Gets the Unicode character codes that represent the character.
			/// </summary>
			internal string CharacterCodes => Character.CharacterCodepoints();

			/// <summary>
			/// Gets a value indicating whether the character should be added to the valid
			/// characters list.
			/// </summary>
			internal bool IsValid { get; set; } = true;
		}

		/// <summary>
		/// This class draws a checkbox in a column header and lets the user check/uncheck the
		/// check box, firing an event when they do so. IMPORTANT: This class must be instantiated
		/// after the column has been added to a DataGridView control.
		/// </summary>
		private sealed class CheckBoxColumnHeaderHandler : IDisposable
		{
			private DataGridViewColumn m_col;
			private DataGridView m_grid;
			private Size m_szCheckBox;
			private CheckState m_state = CheckState.Checked;
			private StringFormat m_stringFormat;

			/// <summary>
			/// Get/set the label to be used in addition to the checkbox.
			/// </summary>
			internal string Label { get; set; }

			/// <summary />
			internal CheckBoxColumnHeaderHandler(DataGridViewColumn col)
			{
				Debug.Assert(col != null);
				Debug.Assert(col is DataGridViewCheckBoxColumn);
				Debug.Assert(col.DataGridView != null);

				m_col = col;
				m_grid = col.DataGridView;
				m_grid.HandleDestroyed += HandleHandleDestroyed;
				m_grid.CellPainting += HandleHeaderCellPainting;
				m_grid.CellMouseMove += HandleHeaderCellMouseMove;
				m_grid.ColumnHeaderMouseClick += HandleHeaderCellMouseClick;
				m_grid.CellContentClick += HandleDataCellCellContentClick;
				m_grid.Scroll += HandleGridScroll;
				m_grid.RowsAdded += HandleGridRowsAdded;
				m_grid.RowsRemoved += HandleGridRowsRemoved;

				if (!Application.RenderWithVisualStyles)
				{
					m_szCheckBox = new Size(13, 13);
				}
				else
				{
					var element = VisualStyleElement.Button.CheckBox.CheckedNormal;
					var renderer = new VisualStyleRenderer(element);
					using (var g = m_grid.CreateGraphics())
					{
						m_szCheckBox = renderer.GetPartSize(g, ThemeSizeType.True);
					}
				}

				m_stringFormat = new StringFormat(StringFormat.GenericTypographic)
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center,
					Trimming = StringTrimming.EllipsisCharacter
				};
				m_stringFormat.FormatFlags |= StringFormatFlags.NoWrap;
			}

			/// <summary />
			~CheckBoxColumnHeaderHandler()
			{
				Dispose(false);
			}

			/// <summary />
			private bool IsDisposed { get; set; }

			/// <inheritdoc />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary>
			/// In addition to disposing m_stringFormat, we should also clear out all the event
			/// handlers  we added to m_grid in the constructor.
			/// </summary>
			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					m_stringFormat?.Dispose();
					if (m_grid != null && !m_grid.IsDisposed)
					{
						m_grid.HandleDestroyed -= HandleHandleDestroyed;
						m_grid.CellPainting -= HandleHeaderCellPainting;
						m_grid.CellMouseMove -= HandleHeaderCellMouseMove;
						m_grid.ColumnHeaderMouseClick -= HandleHeaderCellMouseClick;
						m_grid.CellContentClick -= HandleDataCellCellContentClick;
						m_grid.Scroll -= HandleGridScroll;
						m_grid.RowsAdded -= HandleGridRowsAdded;
						m_grid.RowsRemoved -= HandleGridRowsRemoved;
					}
				}
				Label = null;
				m_stringFormat = null;
				m_grid = null;
				m_col = null;
				IsDisposed = true;
			}

			/// <summary>
			/// Gets or sets the state of the column header's check box.
			/// </summary>
			private CheckState HeadersCheckState
			{
				get => m_state;
				set
				{
					m_state = value;
					m_grid.InvalidateCell(m_col.HeaderCell);
				}
			}

			/// <summary />
			private void HandleHandleDestroyed(object sender, EventArgs e)
			{
				m_grid.HandleDestroyed -= HandleHandleDestroyed;
				m_grid.CellPainting -= HandleHeaderCellPainting;
				m_grid.CellMouseMove -= HandleHeaderCellMouseMove;
				m_grid.ColumnHeaderMouseClick -= HandleHeaderCellMouseClick;
				m_grid.CellContentClick -= HandleDataCellCellContentClick;
				m_grid.Scroll -= HandleGridScroll;
				m_grid.RowsAdded -= HandleGridRowsAdded;
				m_grid.RowsRemoved -= HandleGridRowsRemoved;
			}

			/// <summary />
			private void HandleGridRowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
			{
				UpdateHeadersCheckStateFromColumnsValues();
			}

			/// <summary />
			private void HandleGridRowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
			{
				UpdateHeadersCheckStateFromColumnsValues();
			}

			/// <summary />
			private void HandleGridScroll(object sender, ScrollEventArgs e)
			{
				if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
				{
					var rc = m_grid.ClientRectangle;
					rc.Height = m_grid.ColumnHeadersHeight;
					m_grid.Invalidate(rc);
				}
			}

			/// <summary />
			private void UpdateHeadersCheckStateFromColumnsValues()
			{
				var foundOneChecked = false;
				var foundOneUnChecked = false;
				foreach (DataGridViewRow row in m_grid.Rows)
				{
					var cellValue = row.Cells[m_col.Index].Value;
					if (!(cellValue is bool))
					{
						continue;
					}
					var chked = (bool)cellValue;
					if (!foundOneChecked && chked)
					{
						foundOneChecked = true;
					}
					else if (!foundOneUnChecked && !chked)
					{
						foundOneUnChecked = true;
					}
					if (foundOneChecked && foundOneUnChecked)
					{
						HeadersCheckState = CheckState.Indeterminate;
						return;
					}
				}

				HeadersCheckState = foundOneChecked ? CheckState.Checked : CheckState.Unchecked;
			}

			/// <summary />
			private void UpdateColumnsDataValuesFromHeadersCheckState()
			{
				foreach (DataGridViewRow row in m_grid.Rows)
				{
					if (row.Cells[m_col.Index] == m_grid.CurrentCell && m_grid.IsCurrentCellInEditMode)
					{
						m_grid.EndEdit();
					}
					row.Cells[m_col.Index].Value = (m_state == CheckState.Checked);
				}
			}

			#region Mouse move and click handlers

			/// <summary>
			/// Handles toggling the selected state of an item in the list.
			/// </summary>
			private void HandleDataCellCellContentClick(object sender, DataGridViewCellEventArgs e)
			{
				if (e.RowIndex >= 0 && e.ColumnIndex == m_col.Index)
				{
					var currCellValue = (bool)m_grid[e.ColumnIndex, e.RowIndex].Value;
					m_grid[e.ColumnIndex, e.RowIndex].Value = !currCellValue;
					UpdateHeadersCheckStateFromColumnsValues();
				}
			}

			/// <summary />
			private void HandleHeaderCellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
			{
				if (e.RowIndex >= 0 || e.ColumnIndex != m_col.Index)
				{
					return;
				}
				if (!IsClickInCheckBox(e))
				{
					return;
				}
				HeadersCheckState = HeadersCheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
				m_grid.InvalidateCell(m_col.HeaderCell);
				UpdateColumnsDataValuesFromHeadersCheckState();
			}

			/// <summary />
			private void HandleHeaderCellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
			{
				if (e.ColumnIndex == m_col.Index && e.RowIndex < 0)
				{
					m_grid.InvalidateCell(m_col.HeaderCell);
				}
			}

			#endregion

			#region Painting methods

			/// <summary />
			private void HandleHeaderCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
			{
				if (e.RowIndex >= 0 || e.ColumnIndex != m_col.Index)
				{
					return;
				}
				var rcCell = HeaderRectangle;
				if (rcCell.IsEmpty)
				{
					return;
				}
				var rcBox = GetCheckBoxRectangle(rcCell);
				if (Application.RenderWithVisualStyles)
				{
					DrawVisualStyleCheckBox(e.Graphics, rcBox);
				}
				else
				{
					var state = ButtonState.Checked;
					switch (HeadersCheckState)
					{
						case CheckState.Unchecked:
							state = ButtonState.Normal;
							break;
						case CheckState.Indeterminate:
							state |= ButtonState.Inactive;
							break;
					}
					ControlPaint.DrawCheckBox(e.Graphics, rcBox, state | ButtonState.Flat);
				}
				if (string.IsNullOrEmpty(Label))
				{
					return;
				}
				e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				using (var brush = new SolidBrush(m_grid.ForeColor))
				{
					var sz = e.Graphics.MeasureString(Label, m_grid.Font, new Point(0, 0), m_stringFormat).ToSize();
					var dy2 = (int)Math.Floor((rcCell.Height - sz.Height) / 2f);
					if (dy2 < 0)
					{
						dy2 = 0;
					}
					var rcText = new Rectangle(rcBox.X + rcBox.Width + 3, rcCell.Y + dy2, rcCell.Width - (rcBox.Width + 6), Math.Min(sz.Height, rcCell.Height));
					e.Graphics.DrawString(Label, m_grid.Font, brush, rcText, m_stringFormat);
				}
			}

			private Rectangle HeaderRectangle
			{
				get
				{
					var rcCell = m_grid.GetCellDisplayRectangle(m_col.Index, -1, false);
					if (rcCell.IsEmpty)
					{
						return rcCell;
					}
					// At this point, we know at least part of the header cell is visible, therefore,
					// force the rectangle's width to that of the column's.
					rcCell.X = rcCell.Right - m_col.Width;

					// Subtract one so as not to include the left border in the width.
					rcCell.Width = m_col.Width - 1;
					return rcCell;
				}
			}

			private Rectangle GetCheckBoxRectangle(Rectangle rcCell)
			{
				var dx = 3;
				if (string.IsNullOrEmpty(Label))
				{
					dx = (int)Math.Floor((rcCell.Width - m_szCheckBox.Width) / 2f);
				}
				var dy = (int)Math.Floor((rcCell.Height - m_szCheckBox.Height) / 2f);
				return new Rectangle(rcCell.X + dx, rcCell.Y + dy, m_szCheckBox.Width, m_szCheckBox.Height);
			}

			///<summary>
			/// Check whether this mouse click was inside our checkbox display rectangle.
			///</summary>
			internal bool IsClickInCheckBox(DataGridViewCellMouseEventArgs e)
			{
				if (e.ColumnIndex != m_col.Index || e.RowIndex >= 0)
				{
					return false;
				}
				var rcCell = HeaderRectangle;
				if (rcCell.IsEmpty)
				{
					return false;
				}
				var rcBox = GetCheckBoxRectangle(rcCell);
				var minX = rcBox.X - rcCell.X;
				var maxX = minX + rcBox.Width;
				var minY = rcBox.Y - rcCell.Y;
				var maxY = minY + rcBox.Height;
				return e.X >= minX && e.X < maxX && e.Y >= minY && e.Y < maxY;
			}

			/// <summary />
			private void DrawVisualStyleCheckBox(IDeviceContext g, Rectangle rcBox)
			{
				var isHot = rcBox.Contains(m_grid.PointToClient(Control.MousePosition));
				var element = VisualStyleElement.Button.CheckBox.CheckedNormal;
				switch (HeadersCheckState)
				{
					case CheckState.Unchecked:
						element = isHot ? VisualStyleElement.Button.CheckBox.UncheckedHot : VisualStyleElement.Button.CheckBox.UncheckedNormal;
						break;
					case CheckState.Indeterminate:
						element = isHot ? VisualStyleElement.Button.CheckBox.MixedHot : VisualStyleElement.Button.CheckBox.MixedNormal;
						break;
					default:
						{
							if (isHot)
							{
								element = VisualStyleElement.Button.CheckBox.CheckedHot;
							}
							break;
						}
				}

				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(g, rcBox);
			}

			#endregion
		}
	}
}
