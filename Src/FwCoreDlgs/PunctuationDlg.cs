using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml.Serialization;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Widgets;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region PunctuationDlg class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog box for specifying the punctuation rules for a given language.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class PunctuationDlg : Form
	{
		#region Constants
		private const int kiTabMatchedPairs = 0;
		private const int kiTabQuotations = 1;
		private const int kiTabPatterns = 2;

		private const int kiColOpen = 0;
		private const int kiColClose = 1;
		private const int kiColPermitParaSpanning = 2;

		private const int kiColPattern = 0;
		private const int kiColContextPosition = 1;
		private const int kiColPatternCount = 2;
		private const int kiColPatternValid = 3;

		private const int kiColLevel = 0;
		private const int kiColOpeningQMark = 1;
		private const int kiColClosingQMark = 2;
		#endregion

		#region Data members
		private readonly FdoCache m_cache;
		private readonly LanguageDefinition m_langDef;
		private readonly ILgCharacterPropertyEngine m_chrPropEng;

		private readonly string m_currScrProjLabel =
			ResourceHelper.GetResourceString("kstidCurrentScriptureProject");

		private int m_gridRowHeight;
		private Font m_fntVern;
		private IHelpTopicProvider m_helpTopicProvider;
		private TextBox m_txtCellEdit;
		private MatchedPairList m_matchedPairList;
		private PuncPatternsList m_patternList;
		private QuotationMarksList m_qmCurrentList;
		private QuotationMarksList m_qmCustomBackupList;
		private QuotationMarksList m_qmLanguageBackupList;
		private QuotationLangList m_quotationMarkLangs;
		private ValidCharacters m_validChars;
		private CharacterInfoToolTip m_charInfoToolTip;
		#endregion

		#region Constructors, initialization and disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PunctuationDlg()
		{
			InitializeComponent();
			SortPuncPatternGridOnColumn(kiColPatternValid, SortOrder.Descending);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PunctuationDlg(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			LanguageDefinition langDef, string wsName, Guid chkGuid)
			: this()
		{
			m_cache = cache;
			m_langDef = langDef;
			m_helpTopicProvider = helpTopicProvider;
			if (string.IsNullOrEmpty(wsName))
				throw new ArgumentException("Writing system name must not be null or empty.", "wsName");

			m_validChars = ValidCharacters.Load(m_langDef);
			m_matchedPairList = MatchedPairList.Load(m_langDef.WritingSystem.MatchedPairs, wsName);
			m_patternList = PuncPatternsList.Load(m_langDef.WritingSystem.PunctuationPatterns, wsName);
			m_chrPropEng = m_cache.UnicodeCharProps;

			m_lblWsName.Text = string.Format(m_lblWsName.Text, wsName);
			lblNumberLevels.Text = string.Format(lblNumberLevels.Text, wsName);
			m_fntVern = new Font(m_langDef.WritingSystem.DefaultBodyFont, 12);
			gridMatchedPairs.RowsDefaultCellStyle.Font = m_fntVern;
			colPattern.DefaultCellStyle.Font = m_fntVern;
			m_gridRowHeight = Math.Max(m_fntVern.Height, gridMatchedPairs.Font.Height) + 2;

			tpgPatterns.Controls.Remove(gridPatterns);
			contextCtrl.Dock = DockStyle.Fill;
			contextCtrl.Cache = m_cache;
			contextCtrl.LanguageDefinition = m_langDef;
			contextCtrl.ContextFont = m_fntVern;
			contextCtrl.TokenGrid = gridPatterns;

			if (m_langDef.WritingSystem.RightToLeft)
				contextCtrl.SetRightToLeft();

			colPatternValid.TrueValue = PuncPatternStatus.Valid;
			colPatternValid.FalseValue = PuncPatternStatus.Invalid;
			colPatternValid.IndeterminateValue = PuncPatternStatus.Unknown;

			gridMatchedPairs.RowCount = m_matchedPairList.Count + 1;
			colOpen.HeaderCell.SortGlyphDirection = SortOrder.Ascending;
			SortMatchedPairsGrid();
			gridPatterns.RowCount = m_patternList.Count;

			gridQMarks.Columns[kiColOpeningQMark].DefaultCellStyle.Font = m_fntVern;
			gridQMarks.Columns[kiColClosingQMark].DefaultCellStyle.Font = m_fntVern;

			m_charInfoToolTip = new CharacterInfoToolTip();
			LoadQuotationMarkLanguagesCombo();
			LoadWritingSystemsQuotationMarks(wsName);
			UpdateDlgParaContinuationSettings();

			if (chkGuid == StandardCheckIds.kguidPunctuation)
				tabPunctuation.SelectedIndex = kiTabPatterns;
			else if (chkGuid == StandardCheckIds.kguidQuotations)
				tabPunctuation.SelectedIndex = kiTabQuotations;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed;
		/// otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();

				if (m_fntVern != null)
					m_fntVern.Dispose();

				m_fntVern = null;
			}

			base.Dispose(disposing);
		}

		#endregion

		#region Matched Pairs Grid methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridMatchedPairs_CellValueNeeded(object sender,
			DataGridViewCellValueEventArgs e)
		{
			if (e.RowIndex < m_matchedPairList.Count)
			{
				MatchedPair pair = m_matchedPairList[e.RowIndex];
				switch (e.ColumnIndex)
				{
					case kiColOpen: e.Value = pair.Open; break;
					case kiColClose: e.Value = pair.Close; break;
					case kiColPermitParaSpanning: e.Value = pair.PermitParaSpanning; break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridMatchedPairs_CellValuePushed(object sender,
			DataGridViewCellValueEventArgs e)
		{
			// Add a new row in the matched pair list if we
			// don't have one for the cell value being pushed.
			if (e.RowIndex == m_matchedPairList.Count)
			{
				m_matchedPairList.Add(new MatchedPair());
				gridMatchedPairs.InvalidateColumn(kiColPermitParaSpanning);
			}

			MatchedPair pair = m_matchedPairList[e.RowIndex];

			switch (e.ColumnIndex)
			{
				case kiColOpen: pair.Open = e.Value as string; break;
				case kiColClose: pair.Close = e.Value as string; break;
				case kiColPermitParaSpanning: pair.PermitParaSpanning = (bool)e.Value; break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// After an open or close value is changed, invalidate its associated codepoint cell
		/// so the Unicode value is updated too.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridMatchedPairs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == kiColOpen || e.ColumnIndex == kiColClose)
				gridMatchedPairs.InvalidateCell(e.ColumnIndex + 1, e.RowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add one or more empty matched pairs objects to our list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridMatchedPairs_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			// Use RowCount - 1 so the new row isn't counted in our list (which it is in the grid).
			while (m_matchedPairList.Count < gridMatchedPairs.RowCount - 1)
				m_matchedPairList.Add(new MatchedPair());

			gridMatchedPairs.InvalidateColumn(kiColPermitParaSpanning);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove one or more empty matched pairs objects from our list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridMatchedPairs_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			if (e.RowIndex < m_matchedPairList.Count)
				m_matchedPairList.RemoveAt(e.RowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows a character information tooltip on the open or closed cells of the matched
		/// pairs grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridMatchedPairs_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (m_matchedPairList == null)
				return;

			MatchedPair pair = m_matchedPairList[e.RowIndex];
			if (pair == null)
			{
				m_charInfoToolTip.Hide();
				return;
			}

			string chr = null;
			if (e.ColumnIndex == kiColOpen)
				chr = pair.Open;
			else if (e.ColumnIndex == kiColClose)
				chr = pair.Close;

			if (chr != null)
			{
				m_charInfoToolTip.Show(gridMatchedPairs, chr,
					gridMatchedPairs.Columns[e.ColumnIndex].DefaultCellStyle.Font);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the character information tooltip when the mouse moves away from one of the
		/// grid cells. The tooltip doesn't always need to be hidden (since not every cell
		/// has one showing), but hiding it anyway doesn't hurt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleGridCellMouseLeave(object sender, DataGridViewCellEventArgs e)
		{
			if (m_charInfoToolTip != null)
				m_charInfoToolTip.Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle click in the header row of the Matched Pairs grid to sort by the column the
		/// user clicks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridMatchedPairs_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || e.ColumnIndex < 0 || m_matchedPairList == null)
				return;

			MatchedPair pair = m_matchedPairList[gridMatchedPairs.CurrentCellAddress.Y];
			gridMatchedPairs.EndEdit();

			foreach (DataGridViewColumn col in gridMatchedPairs.Columns)
			{
				if (col.Index != e.ColumnIndex)
					col.HeaderCell.SortGlyphDirection = SortOrder.None;
				else
				{
					col.HeaderCell.SortGlyphDirection =
						(col.HeaderCell.SortGlyphDirection == SortOrder.Ascending) ?
						SortOrder.Descending : SortOrder.Ascending;
				}
			}

			SortMatchedPairsGrid();

			if (pair != null)
			{
				for (int i = 0; i < gridMatchedPairs.RowCount; i++)
				{
					if (pair == m_matchedPairList[i])
					{
						gridMatchedPairs.CurrentCell =
							gridMatchedPairs[gridMatchedPairs.CurrentCellAddress.X, i];
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the matched pairs grid column that has the sort glyph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SortMatchedPairsGrid()
		{
			int sortedCol = SortedMatchedPairsColumn;

			if (m_matchedPairList == null || sortedCol == -1)
				return;

			SortOrder sortOrder = gridMatchedPairs.Columns[sortedCol].HeaderCell.SortGlyphDirection;

			switch (sortedCol)
			{
				case 0: m_matchedPairList.Sort(sortOrder, MatchedPairList.OpenComparer); break;
				case 1: m_matchedPairList.Sort(sortOrder, MatchedPairList.OpenCodeComparer); break;
				case 2: m_matchedPairList.Sort(sortOrder, MatchedPairList.CloseComparer); break;
				case 3: m_matchedPairList.Sort(sortOrder, MatchedPairList.CloseCodeComparer); break;
				case 4: m_matchedPairList.Sort(sortOrder, MatchedPairList.ClosedByParaComparer); break;
			}

			gridMatchedPairs.Invalidate();
		}

		#endregion

		#region Patterns Grid methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridPatterns_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			Debug.Assert(e.RowIndex >= 0);
			Debug.Assert(e.RowIndex < m_patternList.Count);
			PuncPattern pattern = m_patternList[e.RowIndex];

			switch (e.ColumnIndex)
			{
				case kiColPattern: e.Value = pattern.Pattern.Trim(); break;
				case kiColContextPosition: e.Value = GetLocalizedContextPosition(pattern.ContextPos); break;
				case kiColPatternValid: e.Value = pattern.Status; break;
				case kiColPatternCount: e.Value = (contextCtrl.ContextInfoExists ?
					pattern.Count.ToString() :
					Properties.Resources.kstidNoTokenOccurrencesCount);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a localized string representing the context position (word-medial, etc.).
		/// </summary>
		/// <param name="contextPos">The context position value.</param>
		/// ------------------------------------------------------------------------------------
		private string GetLocalizedContextPosition(ContextPosition contextPos)
		{
			return Properties.Resources.ResourceManager.GetString("kstid" + contextPos);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridPatterns_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			if (e.RowIndex >= 0 && e.RowIndex < m_patternList.Count && e.ColumnIndex == kiColPatternValid)
				m_patternList[e.RowIndex].Status = (PuncPatternStatus)e.Value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellMouseEnter event of the gridPatterns control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridPatterns_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (m_patternList == null || m_charInfoToolTip == null)
				return;

			PuncPattern pattern = m_patternList[e.RowIndex];
			if (e.ColumnIndex != kiColPattern || pattern == null || string.IsNullOrEmpty(pattern.Pattern))
			{
				m_charInfoToolTip.Hide();
				return;
			}

			m_charInfoToolTip.UnicodeValueTextConstructed +=
				m_charInfoToolTip_UnicodeValueTextConstructed;

			m_charInfoToolTip.Show(gridPatterns, pattern.Pattern.Trim(),
				gridPatterns.Columns[kiColPattern].DefaultCellStyle.Font);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This event is fired when the character info. tooltip is being displayed over
		/// a punctuation pattern cell. When that happens, we want to convert any Unicode
		/// values of space with the word space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_charInfoToolTip_UnicodeValueTextConstructed(object sender,
			Control ctrl, ref string text)
		{
			m_charInfoToolTip.UnicodeValueTextConstructed -=
				m_charInfoToolTip_UnicodeValueTextConstructed;

			// Replace the Unicode values for space with the word for space. This is because
			// when punctuation patterns are discovered and put into the list, those having
			// any type of space between two adajacent quotation marks are converted to 0x20.
			// For example when a thin space or En space is found between two adjacent single
			// and double opening quotation marks, it is replaced with a 0x20. Therefore,
			// showing the Unicode value of 0x20 doesn't necessarily represent the original
			// data accurately. So, just use the word for space without being specifid as to
			// what sort of space it is.
			text = text.Replace(StringUtils.GetUnicodeValueString(' '),
				Properties.Resources.kstidPuncPatternTipSpaceReplacment);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle click in the header row of the Punctuation Patterns grid to sort by the
		/// column the user clicks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridPatterns_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			int iCol = e.ColumnIndex;
			if (e.Button == MouseButtons.Left && iCol >= 0 && m_patternList != null)
				SortPuncPatternGridOnColumn(iCol);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the punctuation pattern grid on the given column. If already sorted on that
		/// column, the sort direction will be toggled.
		/// </summary>
		/// <param name="iCol">The index of the column.</param>
		/// ------------------------------------------------------------------------------------
		private void SortPuncPatternGridOnColumn(int iCol)
		{
			DataGridViewColumn col = gridPatterns.Columns[iCol];
			SortPuncPatternGridOnColumn(iCol,
				(col.HeaderCell.SortGlyphDirection == SortOrder.Ascending) ?
				SortOrder.Descending : SortOrder.Ascending);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the punctuation pattern grid on the given column in the given direction.
		/// </summary>
		/// <param name="iCol">The index of the column.</param>
		/// <param name="direction">The sort direction.</param>
		/// ------------------------------------------------------------------------------------
		private void SortPuncPatternGridOnColumn(int iCol, SortOrder direction)
		{
			gridPatterns.CommitEdit(DataGridViewDataErrorContexts.Commit);

			foreach (DataGridViewColumn col in gridPatterns.Columns)
				col.HeaderCell.SortGlyphDirection = (col.Index != iCol ? SortOrder.None : direction);

			SortPuncPatternGrid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the punctuation pattern grid column that has the sort glyph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SortPuncPatternGrid()
		{
			int sortedCol = SortedPuncPatternColumn;

			if (m_patternList == null || sortedCol == -1)
				return;

			SortOrder sortOrder = gridPatterns.Columns[sortedCol].HeaderCell.SortGlyphDirection;

			switch (sortedCol)
			{
				case 0: m_patternList.Sort(sortOrder, PuncPatternsList.PatternComparer); break;
				case 1: m_patternList.Sort(sortOrder, PuncPatternsList.ContextComparer); break;
				case 2: m_patternList.Sort(sortOrder, PuncPatternsList.CountComparer); break;
				case 3: m_patternList.Sort(sortOrder, PuncPatternsList.StatusComparer); break;
			}

			gridPatterns.Invalidate();

			if (gridPatterns.RowCount > 0)
				gridPatterns.CurrentCell = gridPatterns[0, 0];

			contextCtrl.RefreshContextGrid();
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sorted column in the matched pairs list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int SortedMatchedPairsColumn
		{
			get
			{
				foreach (DataGridViewColumn col in gridMatchedPairs.Columns)
				{
					if (col.HeaderCell.SortGlyphDirection != SortOrder.None)
						return col.Index;
				}

				return -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sorted column in the punctuation patterns list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int SortedPuncPatternColumn
		{
			get
			{
				foreach (DataGridViewColumn col in gridPatterns.Columns)
				{
					if (col.HeaderCell.SortGlyphDirection != SortOrder.None)
						return col.Index;
				}

				return -1;
			}
		}

		#endregion

		#region Generic grid methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RowHeightInfoNeeded event of the grids.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleRowHeightInfoNeeded(object sender, DataGridViewRowHeightInfoNeededEventArgs e)
		{
			e.Height = m_gridRowHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When going into the edit mode in the matched pairs or quotation marks grid, hook
		/// up things so we can monitor what characters are entered into those cells.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
		{
			DataGridView grid = sender as DataGridView;
			if (grid == null)
				return;

			m_txtCellEdit = e.Control as TextBox;
			if (m_txtCellEdit == null)
				return;

			m_txtCellEdit.TextChanged += CellEditTextBoxTextChanged;
			grid.CellEndEdit += GridCellEndEdit;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// After one of the cells on the matched pairs or quotations grid goes out of edit
		/// mode, then unhook the events that monitor what characters the user enters into
		/// a cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GridCellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			DataGridView grid = sender as DataGridView;
			if (grid != null)
				grid.CellEndEdit -= GridCellEndEdit;

			if (m_txtCellEdit != null)
			{
				m_txtCellEdit.TextChanged -= CellEditTextBoxTextChanged;
				m_txtCellEdit = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the characters the user enters into the matched pairs or quotation marks
		/// grid are punctuation, spaces, symbols or control characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CellEditTextBoxTextChanged(object sender, EventArgs e)
		{
			Debug.Assert(sender == m_txtCellEdit);
			int ichSel = m_txtCellEdit.SelectionStart;
			StringBuilder bldr = new StringBuilder();
			foreach (char ch in m_txtCellEdit.Text)
			{
				int code = (int)ch;
				string category = (m_langDef == null) ? null : m_langDef.GetOverrideCharCategory(ch);
				if ((category != null &&
					(category == "Cf" ||
					(m_txtCellEdit.MaxLength > 1 && (category == "Zs" || category == "Zl")) ||
					category[0] == 'S' ||
					category[0] == 'P' )) ||
					(m_chrPropEng.get_IsControl(code) ||
					(m_txtCellEdit.MaxLength > 1 && m_chrPropEng.get_IsSeparator(code)) ||
					m_chrPropEng.get_IsSymbol(code) ||
					m_chrPropEng.get_IsPunctuation(code)))
				{
					bldr.Append(ch);
				}
				else
				{
					ichSel--;
				}
			}

			if (m_txtCellEdit.Text != bldr.ToString())
			{
				System.Media.SystemSounds.Beep.Play();
				m_txtCellEdit.TextChanged -= CellEditTextBoxTextChanged;
				m_txtCellEdit.Text = bldr.ToString();
				m_txtCellEdit.TextChanged += CellEditTextBoxTextChanged;
				m_txtCellEdit.SelectionStart = Math.Min(ichSel, bldr.Length);
			}
		}

		#endregion

		#region Misc. events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnHelp control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			string helpTopicKey = null;

			switch (tabPunctuation.SelectedIndex)
			{
				case kiTabMatchedPairs:	helpTopicKey = "khtpPunctuationMatchingPairs"; break;
				case kiTabPatterns: helpTopicKey = "khtpPunctuationPatterns"; break;
				case kiTabQuotations: helpTopicKey = "khtpPunctuationQuotationMarks"; break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.FormClosing"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			if (e.Cancel || DialogResult == DialogResult.Cancel)
				return;

			e.Cancel = true;

			if (tabPunctuation.SelectedIndex != kiTabQuotations)
			{
				if (!ValidateMatchedPairs(m_matchedPairList, true))
					return;
				if (!ValidateQuotationMarks(m_qmCurrentList.TrimmedList))
					return;
			}
			else
			{
				if (!ValidateQuotationMarks(m_qmCurrentList.TrimmedList))
					return;
				if (!ValidateMatchedPairs(m_matchedPairList, true))
					return;
			}

			e.Cancel = false;
			m_langDef.WritingSystem.MatchedPairs = m_matchedPairList.XmlString;
			m_langDef.PunctuationPatterns = m_patternList.XmlString;
			m_qmCurrentList = GetQuotationMarkInfo();
			m_langDef.QuotationMarks = (m_qmCurrentList == null ? null : m_qmCurrentList.XmlString);
			UpdateValidCharactersList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate matched pair list to ensure that all matched pairs have both an opening
		/// and closing entry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ValidateMatchedPairs(MatchedPairList pairList, bool showMsg)
		{
			foreach (MatchedPair pair in pairList)
			{
				if (string.IsNullOrEmpty(pair.Open) || string.IsNullOrEmpty(pair.Close))
				{
					tabPunctuation.SelectedIndex = kiTabMatchedPairs;

					if (showMsg)
					{
						MessageBox.Show(this,
							Properties.Resources.kstidMatchingPairsErrorMessage,
							Properties.Resources.kstidMatchingPairsErrorCaption,
							MessageBoxButtons.OK, MessageBoxIcon.Error);
					}

					return false;
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Vailidates the quotation marks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ValidateQuotationMarks(QuotationMarksList qmarks)
		{
			string msg = null;
			int iGap = qmarks.FindGap();

			// First check for gaps
			if (iGap > 0)
			{
				// Found a gap
				msg = string.Format(Properties.Resources.kstidQuotationMarksGapMsg, iGap);
			}
			else
			{
				for (int i = 0; i < qmarks.Levels; i++)
				{
					if (!qmarks[i].IsComplete)
					{
						// Found an incomplete level
						msg = string.Format(Properties.Resources.kstidQuotationMarksIncompleteMsg, i + 1);
						break;
					}
				}

				if (msg == null)
				{
					QuotationMarksList.InvalidComboInfo error = qmarks.InvalidOpenerCloserCombinations;
					if (error != null)
					{
						// Found 2 levels that have invalid opener and closer combinations
						int openingLevel = (error.LowerLevelIsOpener ? error.LowerLevel : error.UpperLevel);
						int closingLevel = (error.LowerLevelIsOpener ? error.UpperLevel : error.LowerLevel);
						msg = Properties.Resources.kstidQuotationMarkOpenerIsCloserInOtherLevelMsg;
						msg = string.Format(msg, openingLevel + 1, closingLevel + 1, error.QMark);
					}
				}
			}

			if (msg != null)
			{
				tabPunctuation.SelectedIndex = kiTabQuotations;
				MessageBox.Show(this, msg, Properties.Resources.kstidQuotationMarksErrorCaption,
					MessageBoxButtons.OK);

				return false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the matched pairs and punctuation patterns are in the writing system's
		/// list of valid characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateValidCharactersList()
		{
			bool validCharsUpdated = false;
			ValidCharacters validChars = ValidCharacters.Load(m_langDef);
			if (validChars == null)
				return;

			foreach (MatchedPair pair in m_matchedPairList)
			{
				if (validChars.AddCharacter(pair.Open) != ValidCharacterType.None)
					validCharsUpdated = true;

				if (validChars.AddCharacter(pair.Close) != ValidCharacterType.None)
					validCharsUpdated = true;
			}

			foreach (PuncPattern pattern in m_patternList)
			{
				if (pattern.Valid)
				{
					foreach (char chr in pattern.Pattern)
					{
						if (validChars.AddCharacter(chr.ToString()) != ValidCharacterType.None)
							validCharsUpdated = true;
					}
				}
			}

			if (validCharsUpdated)
			{
				StringUtils.UpdatePUACollection(m_langDef, validChars.AllCharacters);
				m_langDef.ValidChars = validChars.XmlString;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move user to new row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnAddMatchedPair_Click(object sender, EventArgs e)
		{
			gridMatchedPairs.CurrentCell =
				gridMatchedPairs[kiColOpen, gridMatchedPairs.NewRowIndex];

			gridMatchedPairs.BeginEdit(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnRemoveMatchedPair_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow row in gridMatchedPairs.SelectedRows)
			{
				if (row.Index != gridMatchedPairs.NewRowIndex)
					gridMatchedPairs.Rows.Remove(row);
			}

			gridMatchedPairs.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Before scanning, setup the check used for scanning and the parameters it needs
		/// for scanning in inventory mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void contextCtrl_BeforeTextTokenSubStringsLoaded(string filename)
		{
			contextCtrl.CheckToRun = "PunctuationCheck";
			contextCtrl.CheckParameters["PunctCheckLevel"] = "Intermediate";
			contextCtrl.CheckParameters["PunctWhitespaceChar"] =
				ResourceHelper.GetResourceString("kstidPunctCheckWhitespaceChar").Substring(0, 1);

			if (m_qmCurrentList == null || m_qmCurrentList.Levels == 0)
				return;

			QuotationMarksList qmarks = GetQuotationMarkInfo();
			if (qmarks == null)
				return;

			contextCtrl.CheckParameters["QuotationMarkInfo"] = qmarks.XmlString;

			StringBuilder bldr = new StringBuilder();

			for (int i = 0; i < qmarks.Levels; i++)
			{
				bldr.Append(qmarks.QMarksList[i].Opening);
				bldr.Append(" ");
				bldr.Append(qmarks.QMarksList[i].Closing);
				bldr.Append(" ");
			}

			bldr.Length--;
			contextCtrl.CheckParameters["ValidQuotationMarks"] = bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context info for the given row in the context control.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="sKey">The key into CharContextCtrl.m_contextInfoLists.</param>
		/// <param name="sConcordanceItem">The vernacular data (character or punctuation
		/// pattern) that will appear concorded in the context grid.</param>
		/// ------------------------------------------------------------------------------------
		private void contextCtrl_GetContextInfo(int index, out string sKey, out string sConcordanceItem)
		{
			try
			{
				PuncPattern pattern = m_patternList[index];
				// REVIEW (DavidO): This logic must match the logic in ContextInfo.Key. What we
				// really want to have here is the ContextInfo object associated wth this row.
				sKey = pattern.Pattern + pattern.ContextPos;
				sConcordanceItem = pattern.Pattern;
			}
			catch
			{
				sKey = null;
				sConcordanceItem = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills the pattern grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FillPatternGrid(List<TextTokenSubstring> tokenSubstrings)
		{
			if (tokenSubstrings == null || tokenSubstrings.Count == 0)
			{
				Debug.Fail("The TextTokenSubStringsLoaded event should not be raised if no tokens were found.");
				return;
			}

			using (new WaitCursor(this))
			{
				gridPatterns.Rows.Clear();
				Dictionary<string, PuncPattern> patterns = new Dictionary<string, PuncPattern>();
				foreach (PuncPattern pat in m_patternList)
				{
					if (pat.Status != PuncPatternStatus.Unknown)
					{
						pat.Count = 0;
						patterns[pat.Pattern] = pat;
					}
				}

				m_patternList.Clear();
				contextCtrl.ResetContextLists();

				int cIndeterminatePatterns = 0;

				foreach (TextTokenSubstring txtTokSub in tokenSubstrings)
				{
					string contextItem = txtTokSub.InventoryText;

					PuncPattern pattern;
					if (patterns.TryGetValue(contextItem, out pattern))
						pattern.Count++;
					else
					{
						pattern = new PuncPattern();
						pattern.Count = 1;
						pattern.Pattern = contextItem;
						pattern.ContextPos = GetContextPosition(contextItem);
						patterns[contextItem] = pattern;
						cIndeterminatePatterns++;
					}

					contextCtrl.AddContextInfo(new ContextInfo(pattern, txtTokSub));
				}

				foreach (PuncPattern pattern in patterns.Values)
					m_patternList.Add(pattern);

				if (SortedPuncPatternColumn == -1)
					colPattern.HeaderCell.SortGlyphDirection = SortOrder.Ascending;

				switch (cIndeterminatePatterns)
				{
					case 0:
						contextCtrl.ScanMsgLabelText = FwCoreDlgs.PunctuationPatternsScanResultsNone;
						break;
					case 1:
						contextCtrl.ScanMsgLabelText = FwCoreDlgs.PunctuationPatternsScanResultsSingular;
						break;
					default:
						contextCtrl.ScanMsgLabelText = string.Format(FwCoreDlgs.PunctuationPatternsScanResultsPlural,
						cIndeterminatePatterns);
						break;
				}

				gridPatterns.RowCount = m_patternList.Count;
				SortPuncPatternGrid();
				gridPatterns.Invalidate();
			}
		}

		#endregion

		#region Data source reading methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the context position based on whether the pattern begins and/or ends with a
		/// whitespace indicator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ContextPosition GetContextPosition(string pattern)
		{
			if (string.IsNullOrEmpty(pattern))
				return ContextPosition.Undefined;

			if (pattern.Length > 1)
			{
				bool fStartsWithWhitespace = (pattern[0] == ContextInfo.s_chPunctWhitespace);
				int ichLastChar = pattern.Length - 1;
				bool fEndsWithWhitespace = (pattern[ichLastChar] == ContextInfo.s_chPunctWhitespace);

				if (fStartsWithWhitespace)
					return fEndsWithWhitespace ? ContextPosition.Isolated : ContextPosition.WordInitial;

				if (fEndsWithWhitespace)
					return ContextPosition.WordFinal;
			}

			// If any of the punctuation characters in the pattern are not
			// word-forming, then return Word-breaking. (See TE-7820);
			foreach (char chr in pattern)
			{
				if (!m_validChars.IsWordForming(chr))
					return ContextPosition.WordBreaking;
			}

			return ContextPosition.WordMedial;
		}

		#endregion

		#region Quotation tab handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a quotation mark list info. object from the settings on the dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private QuotationMarksList GetQuotationMarkInfo()
		{
			QuotationMarksList qmarks = null;

			if (m_qmCurrentList == null || m_qmCurrentList.Levels == 0)
				return null;

			qmarks = m_qmCurrentList.Copy().TrimmedList;
			if (qmarks.Levels == 0)
				return null;

			if (!chkParaContinuation.Checked)
			{
				qmarks.ContinuationType = ParagraphContinuationType.None;
				qmarks.ContinuationMark = ParagraphContinuationMark.None;
				return qmarks;
			}

			qmarks.ContinuationMark = (rbOpening.Checked ?
				ParagraphContinuationMark.Opening : ParagraphContinuationMark.Closing);

			if (rbRequireAll.Checked)
				qmarks.ContinuationType = ParagraphContinuationType.RequireAll;
			else if (rbInnermost.Checked)
				qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			else
				qmarks.ContinuationType = ParagraphContinuationType.RequireOutermost;

			return qmarks;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the radio buttons on the quotations tab based on info. from the DB.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateDlgParaContinuationSettings()
		{
			if (m_qmCurrentList == null || m_qmCurrentList.Levels == 0)
				return;

			chkParaContinuation.Checked =
				(m_qmCurrentList.ContinuationType != ParagraphContinuationType.None &&
				m_qmCurrentList.ContinuationMark != ParagraphContinuationMark.None);

			if (chkParaContinuation.Checked)
			{
				rbOpening.Checked = (m_qmCurrentList.ContinuationMark == ParagraphContinuationMark.Opening);
				rbClosing.Checked = (m_qmCurrentList.ContinuationMark == ParagraphContinuationMark.Closing);
				rbRequireAll.Checked = (m_qmCurrentList.ContinuationType == ParagraphContinuationType.RequireAll);
				rbInnermost.Checked = (m_qmCurrentList.ContinuationType == ParagraphContinuationType.RequireInnermost);
				rbOutermost.Checked = (m_qmCurrentList.ContinuationType == ParagraphContinuationType.RequireOutermost);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadQuotationMarkLanguagesCombo()
		{
			m_quotationMarkLangs = QuotationLangList.Load();
			if (m_quotationMarkLangs == null)
				return;

			// Make sure the quotation mark glyphs are found in the vernacular
			// font. If not, then don't show those quotation marks to the user.
			for (int i = m_quotationMarkLangs.Count - 1; i >= 0; i--)
			{
				QuotationLang qlang = m_quotationMarkLangs[i];

				string combinedMarks =
					(qlang.FirstOpen ?? string.Empty) +	(qlang.FirstClose ?? string.Empty) +
					(qlang.SecondOpen ?? string.Empty) + (qlang.SecondClose ?? string.Empty);

				if (!string.IsNullOrEmpty(combinedMarks) &&
					!Win32.AreCharGlyphsInFont(combinedMarks, m_fntVern))
				{
					m_quotationMarkLangs.RemoveAt(i);
				}
			}

			cboQuotationLangs.Items.AddRange(m_quotationMarkLangs.ToArray());
			m_quotationMarkLangs.AddCustomItem();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the controls on the quotation marks tab with the quotation mark info.
		/// from the writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadWritingSystemsQuotationMarks(string wsName)
		{
			m_qmCurrentList = QuotationMarksList.Load(m_langDef.QuotationMarks, wsName);
			chkParaContinuation.Checked =
				(m_qmCurrentList.ContinuationType != ParagraphContinuationType.None);

			// Make a backup of the list just deserialized.
			m_qmCustomBackupList = m_qmCurrentList.Copy();
			m_qmCustomBackupList.EnsureLevelExists(1);

			if (m_qmCurrentList.LocaleOfLangUsedFrom != null)
				m_qmLanguageBackupList = m_qmCurrentList.Copy();

			QuotationLang qlang = m_quotationMarkLangs.FindLangFromQMList(m_qmCurrentList, null, true);
			SetQuotationLangComboWithoutEvents(qlang);
			spinLevels.ValueChanged -= spinLevels_ValueChanged;
			spinLevels.Value = m_qmCurrentList.Levels;
			spinLevels.ValueChanged += spinLevels_ValueChanged;
			RefreshQuotationMarksGrid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ValueChanged event of the spinLevels control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void spinLevels_ValueChanged(object sender, EventArgs e)
		{
			int level = (int)spinLevels.Value;

			while (m_qmCurrentList.Levels > level)
				m_qmCurrentList.RemoveLastLevel();

			if (m_qmCurrentList.Levels < level)
			{
				m_qmCurrentList.EnsureLevelExists(level);

				// If the backup copy has enough levels, then copy the info. from it to
				// put in the level we just added to the current list.
				if (m_qmCustomBackupList.Levels >= level)
				{
					m_qmCurrentList[level - 1].Opening = m_qmCustomBackupList[level - 1].Opening;
					m_qmCurrentList[level - 1].Closing = m_qmCustomBackupList[level - 1].Closing;
				}
			}

			SetQuotationLangComboWithoutEvents(null);
			RefreshQuotationMarksGrid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the quotation marks grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RefreshQuotationMarksGrid()
		{
			gridQMarks.RowCount = m_qmCurrentList.Levels;
			gridQMarks.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows a character information tooltip on the opening and closing quotation marks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridQMarks_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (m_qmCurrentList == null)
				return;

			QuotationMarks qm = m_qmCurrentList[e.RowIndex];
			if (qm == null)
			{
				m_charInfoToolTip.Hide();
				return;
			}

			string chr = null;
			if (e.ColumnIndex == kiColOpeningQMark)
				chr = qm.Opening;
			else if (e.ColumnIndex == kiColClosingQMark)
				chr = qm.Closing;

			if (chr != null)
			{
				m_charInfoToolTip.Show(gridQMarks, chr,
					gridQMarks.Columns[e.ColumnIndex].DefaultCellStyle.Font);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridQMarks_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			QuotationMarks qm = m_qmCurrentList[e.RowIndex];
			if (qm != null)
			{
				switch (e.ColumnIndex)
				{
					case kiColLevel: e.Value = (e.RowIndex + 1).ToString(); break;
					case kiColOpeningQMark: e.Value = qm.Opening; break;
					case kiColClosingQMark: e.Value = qm.Closing; break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridQMarks_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			QuotationMarks qm = m_qmCurrentList[e.RowIndex];
			if (qm == null)
				return;

			string value = e.Value as string;
			bool changeToCustomLocale = false;

			switch (e.ColumnIndex)
			{
				case kiColOpeningQMark:
					if (qm.Opening != value)
					{
						qm.Opening = (value ?? string.Empty);
						changeToCustomLocale = true;
					}

					break;

				case kiColClosingQMark:
					if (qm.Closing != value)
					{
						qm.Closing = (value ?? string.Empty);
						changeToCustomLocale = true;
					}

					break;
			}

			// Make a backup copy of the current settings.
			m_qmCustomBackupList = m_qmCurrentList.Copy();

			if (changeToCustomLocale)
				SetQuotationLangComboWithoutEvents(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetQuotationLangComboWithoutEvents(QuotationLang qlang)
		{
			// Determine whether or not the current list matches the quotation marks for the
			// last language the user picked from the list. If so, then make that the current
			// language in the combo. box.
			if (qlang == null)
			{
				qlang = m_quotationMarkLangs.FindLangFromQMList(m_qmCurrentList,
					m_qmLanguageBackupList == null ? null :
					m_qmLanguageBackupList.LocaleOfLangUsedFrom, false);
			}

			m_qmCurrentList.LocaleOfLangUsedFrom = (qlang != null ? qlang.LocaleId : null);
			cboQuotationLangs.SelectionChangeCommitted -= cboQuotationLangs_SelectionChangeCommitted;
			cboQuotationLangs.SelectedItem = qlang;
			cboQuotationLangs.SelectionChangeCommitted += cboQuotationLangs_SelectionChangeCommitted;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void cboQuotationLangs_SelectionChangeCommitted(object sender, EventArgs e)
		{
			// Get the selected quotation sytem.
			QuotationLang qlang = cboQuotationLangs.SelectedItem as QuotationLang;

			if (qlang != null)
			{
				qlang.CopyTo(m_qmCurrentList);
				spinLevels.ValueChanged -= spinLevels_ValueChanged;
				spinLevels.Value = m_qmCurrentList.Levels;
				spinLevels.ValueChanged += spinLevels_ValueChanged;
				RefreshQuotationMarksGrid();
				m_qmLanguageBackupList = m_qmCurrentList.Copy();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void cboQuotationLangs_MeasureItem(object sender, MeasureItemEventArgs e)
		{
			e.ItemHeight = Math.Max(m_fntVern.Height, cboQuotationLangs.Font.Height);
			e.ItemWidth = cboQuotationLangs.DropDownWidth - 2;	// Account for borders.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void cboQuotationLangs_DrawItem(object sender, DrawItemEventArgs e)
		{
			e.DrawBackground();

			bool selected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);
			QuotationLang qlang = (e.Index < 0 ?
				m_quotationMarkLangs.CustomItem : m_quotationMarkLangs[e.Index]);

			// Build a string showing the primary quotation marks with a character
			// between them -- probably an ellipsis.
			string fmt = Properties.Resources.kstidQuotationPairFormat;
			string marks = string.Format(fmt, qlang.FirstOpen, qlang.FirstClose);

			// Check if there were secondary quotation marks specified.
			if (qlang.SecondOpen != null && qlang.SecondClose != null)
			{
				// Tack on the secondary quotation marks.
				string smarks = string.Format(fmt, qlang.SecondOpen, qlang.SecondClose);
				marks = string.Format(Properties.Resources.kstidQuotationSetFormat, marks, smarks);

				// Check if there were tertiary quotation marks specified.
				if (qlang.ThirdOpen != null && qlang.ThirdClose != null)
				{
					// Tack on the secondary quotation marks.
					smarks = string.Format(fmt, qlang.ThirdOpen, qlang.ThirdClose);
					marks = string.Format(Properties.Resources.kstidQuotationSetFormat, marks, smarks);
				}
			}

			TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
			flags |= (RightToLeft == RightToLeft.No ? TextFormatFlags.Left :
				TextFormatFlags.RightToLeft | TextFormatFlags.Right);

			string langName = qlang.ToString();
			Font fnt = cboQuotationLangs.Font;
			Color clrText = (selected ? SystemColors.HighlightText : SystemColors.WindowText);
			Rectangle rc = e.Bounds;
			TextRenderer.DrawText(e.Graphics, langName, fnt, rc, clrText, flags);

			// Only draw the quotation marks in the drop-down portion of the combo. box,
			// not the combo. box portion of the control. Also only draw the quotation
			// marks if the item is not the custom item.
			if ((e.State & DrawItemState.ComboBoxEdit) != DrawItemState.ComboBoxEdit &&
				qlang != m_quotationMarkLangs.CustomItem)
			{
				// Draw the quotation marks in the default vern. font.
				int dx = TextRenderer.MeasureText(langName, fnt).Width;
				if (RightToLeft == RightToLeft.Yes)
					rc.Width -= (dx + 8);
				else
					rc.X += (dx + 8);

				TextRenderer.DrawText(e.Graphics, marks, m_fntVern, rc, clrText, flags);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the chkParaContinuation control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void chkParaContinuation_CheckedChanged(object sender, EventArgs e)
		{
			grpParaCont.Enabled = chkParaContinuation.Checked;
		}

		#endregion


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{

			base.OnLoad(e);
			WritingSystemPropertiesDialog.DisableSaveIfRemoteDb(this, m_cache,
				m_langDef.LocaleName);
		}
	}

	#endregion

	#region QuotationWsList class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("AvailableQuotationLanguages")]
	public class QuotationLangList : List<QuotationLang>
	{
		internal static string s_customName = Properties.Resources.kstidCustomQuotationMarksName;

		private static string s_file =
			Path.Combine(DirectoryFinder.FWCodeDirectory, "QuotationLanguages.xml");

		private QuotationLang m_customItem;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static QuotationLangList Load()
		{
			QuotationLangList list = XmlSerializationHelper.DeserializeFromFile<QuotationLangList>(s_file);

			if (list != null)
				list.LoadDisplayNames();
			else
				list = new QuotationLangList();

			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the custom item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddCustomItem()
		{
			m_customItem = new QuotationLang();
			m_customItem.Name = s_customName;
			m_customItem.LocaleId = s_customName;
			Add(m_customItem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use ICU to find the display names for all the locale Ids found in the
		/// QuotationLang objects in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadDisplayNames()
		{
			string uilocale = Application.CurrentCulture.TwoLetterISOLanguageName;
			uilocale = uilocale.Replace('-', '_');

			// Go through the list of languages for which a quotation system has
			// been specified (from the xml file) and get each one's UI display name.
			foreach (QuotationLang qlang in this)
			{
				string displayName;
				Icu.UErrorCode uerr;
				Icu.GetDisplayName(qlang.LocaleId, uilocale, out displayName, out uerr);
				if (uerr == Icu.UErrorCode.U_ZERO_ERROR && !string.IsNullOrEmpty(displayName))
					qlang.Name = displayName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public QuotationLang CustomItem
		{
			get { return m_customItem; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first QuotationLang object in the list with the specified localeId.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public QuotationLang this[string localeId]
		{
			get
			{
				if (string.IsNullOrEmpty(localeId))
					localeId = s_customName;

				foreach (QuotationLang qlang in this)
				{
					if (qlang.LocaleId == localeId)
						return qlang;
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the QuotationLang whose quotation marks are the same as those in the
		/// specified QuotationMarksList.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public QuotationLang FindLangFromQMList(QuotationMarksList qmarks,
			string preferredLocale, bool compareLocale)
		{
			if (qmarks == null || string.IsNullOrEmpty(qmarks.LocaleOfLangUsedFrom))
				qmarks.LocaleOfLangUsedFrom = s_customName;

			QuotationLang firstMatch = null;

			foreach (QuotationLang qlang in this)
			{
				if (qlang.Equals(qmarks, compareLocale))
				{
					if (preferredLocale == null)
						return qlang;

					if (firstMatch == null || preferredLocale == qlang.LocaleId)
						firstMatch = qlang;
				}
			}

			return firstMatch;
		}
	}

	#endregion

	#region QuotationLang class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("Lang")]
	public class QuotationLang
	{
		/// <summary></summary>
		[XmlAttribute]
		public int Alternate = 0;
		/// <summary></summary>
		public string Name;

		private readonly QuotationMarksList m_internalQmList = new QuotationMarksList();

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the locale id.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string LocaleId
		{
			get { return m_internalQmList.LocaleOfLangUsedFrom; }
			set { m_internalQmList.LocaleOfLangUsedFrom = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the first level opening quotation mark.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string FirstOpen
		{
			get
			{
				return (m_internalQmList.Levels >= 1 && m_internalQmList[0].Opening != null ?
					m_internalQmList[0].Opening : null);
			}
			set
			{
				m_internalQmList.EnsureLevelExists(1);
				m_internalQmList[0].Opening = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the first level closing quotation mark.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string FirstClose
		{
			get
			{
				return (m_internalQmList.Levels >= 1 && m_internalQmList[0].Closing != null ?
					m_internalQmList[0].Closing : null);
			}
			set
			{
				m_internalQmList.EnsureLevelExists(1);
				m_internalQmList[0].Closing = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the second level opening quotation mark.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string SecondOpen
		{
			get
			{
				return (m_internalQmList.Levels >= 2 && m_internalQmList[1].Opening != null ?
					m_internalQmList[1].Opening : null);
			}
			set
			{
				m_internalQmList.EnsureLevelExists(2);
				m_internalQmList[1].Opening = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the second level closing quotation mark.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string SecondClose
		{
			get
			{
				return (m_internalQmList.Levels >= 2 && m_internalQmList[1].Closing != null ?
					m_internalQmList[1].Closing : null);
			}
			set
			{
				m_internalQmList.EnsureLevelExists(2);
				m_internalQmList[1].Closing = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the third level opening quotation mark.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string ThirdOpen
		{
			get
			{
				return (m_internalQmList.Levels >= 3 && m_internalQmList[2].Opening != null ?
					m_internalQmList[2].Opening : null);
			}
			set
			{
				m_internalQmList.EnsureLevelExists(3);
				m_internalQmList[2].Opening = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the third level closing quotation mark.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string ThirdClose
		{
			get
			{
				return (m_internalQmList.Levels >= 3 && m_internalQmList[2].Closing != null ?
					m_internalQmList[2].Closing : null);
			}
			set
			{
				m_internalQmList.EnsureLevelExists(3);
				m_internalQmList[2].Closing = value;
			}
		}

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			string displayName = (Name ?? LocaleId);

			return (Alternate == 0 ? displayName :
				string.Format(Properties.Resources.kstidQuotationLangAltNameFmt,
					displayName, Alternate));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a new QuotationMarksList containing the values from this QuotationLang object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(QuotationMarksList targetList)
		{
			Debug.Assert(targetList != null);

			targetList.Clear();
			targetList.EnsureLevelExists(Levels);
			for (int i = 0; i < Levels; i++)
			{
				targetList[i].Opening = m_internalQmList[i].Opening;
				targetList[i].Closing = m_internalQmList[i].Closing;
			}

			targetList.LocaleOfLangUsedFrom =
				(Name == QuotationLangList.s_customName ? null : LocaleId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of distict quotation levels in the langauge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Levels
		{
			get { return m_internalQmList.Levels; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the specified quotation marks list with the quotation marks in this
		/// quotation marks langauge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Equals(QuotationMarksList qmarks, bool compareLocale)
		{
			if (compareLocale)
			{
				if (qmarks.LocaleOfLangUsedFrom == QuotationLangList.s_customName &&
					!string.IsNullOrEmpty(LocaleId))
				{
					return false;
				}

				if (qmarks.LocaleOfLangUsedFrom != LocaleId)
					return false;
			}

			if (qmarks.Levels < m_internalQmList.Levels || m_internalQmList.Levels == 0)
				return false;

			for (int ilev = 0; ilev < qmarks.Levels; ilev++)
			{
				int iInternalLev = (ilev % m_internalQmList.Levels);
				if (!qmarks[ilev].Equals(m_internalQmList[iInternalLev]))
					return false;
			}

			return true;
		}

		#endregion
	}

	#endregion
}
