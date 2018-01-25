// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Implementation of method for making cells in chart row.
	/// </summary>
	internal class MakeCellsMethod
	{
		private readonly ChartRowEnvDecorator m_vwenv;
		private readonly int m_hvoRow; // Hvo of the IConstChartRow representing a row in the chart.
		private readonly IConstChartRow m_row;
		private readonly LcmCache m_cache;
		private readonly ConstChartVc m_this; // original 'this' object of the refactored method.
		private readonly ConstChartBody m_chart;
		private int[] m_cellparts;
		/// <summary>
		/// Column for which cell is currently open (initially not for any column)
		/// </summary>
		private int m_hvoCurCellCol = 0;
		/// <summary>
		/// Index (display) of last column for which we have made (at least opened) a cell.
		/// </summary>
		private int m_iLastColForWhichCellExists = -1;
		/// <summary>
		/// Index of cellpart to insert clause bracket before; gets reset if we find an auto-missing-marker col first.
		/// </summary>
		private int m_icellPartOpenClause = -1;
		/// <summary>
		/// Index of cellpart to insert clause bracket after (unless m_icolLastAutoMissing is a later column).
		/// </summary>
		private int m_icellPartCloseClause = -1;
		/// <summary>
		/// Number of cellparts output in current cell.
		/// </summary>
		private int m_cCellPartsInCurrentCell = 0;
		private int m_icellpart = 0;
		/// <summary>
		/// Index of last column where automatic missing markers are put.
		/// </summary>
		private int m_icolLastAutoMissing = -1;
		/// <summary>
		/// Stores the TsString displayed for missing markers (auto or user)
		/// </summary>
		private ITsString m_missMkr;

		#region Repository member variables

		private IConstChartRowRepository m_rowRepo;
		private IConstituentChartCellPartRepository m_partRepo;

		#endregion

		/// <summary>
		/// Make one.
		/// </summary>
		public MakeCellsMethod(ConstChartVc baseObj, LcmCache cache, IVwEnv vwenv, int hvo)
		{
			m_this = baseObj;
			m_cache = cache;
			m_rowRepo = m_cache.ServiceLocator.GetInstance<IConstChartRowRepository>();
			m_partRepo = m_cache.ServiceLocator.GetInstance<IConstituentChartCellPartRepository>();

			// Decorator makes sure that things get put out in the right order if chart is RtL
			m_chart = baseObj.m_chart;
			m_vwenv = new ChartRowEnvDecorator(vwenv);

			m_hvoRow = hvo;
			m_row = m_rowRepo.GetObject(m_hvoRow);
		}

		private void SetupMissingMarker()
		{
			m_missMkr = TsStringUtils.MakeString(LanguageExplorerResources.ksMissingMarker, m_cache.DefaultAnalWs);
		}

		/// <summary>
		/// Main entry point, makes the cells.
		/// </summary>
		public void Run(bool fRtL)
		{
			SetupMissingMarker();
			// If the CellsOS of the row changes, we need to regenerate.
			var rowFlidArray = new[] { ConstChartRowTags.kflidCells,
				ConstChartRowTags.kflidClauseType,
				ConstChartRowTags.kflidEndParagraph,
				ConstChartRowTags.kflidEndSentence };
			NoteRowDependencies(rowFlidArray);

			m_vwenv.IsRtL = fRtL;

			MakeRowLabelCell();

			MakeMainCellParts(); // Make all the cell parts between row label and note.

			MakeNoteCell();

			FlushDecorator();
		}

		private void FlushDecorator()
		{
			m_vwenv.FlushDecorator();
		}

		private void MakeNoteCell()
		{
			OpenNoteCell();
			m_vwenv.AddStringProp(ConstChartRowTags.kflidNotes, m_this);
			m_vwenv.CloseTableCell();
		}

		private void MakeMainCellParts()
		{
			m_cellparts = m_row.CellsOS.ToHvoArray();

			if (m_row.StartDependentClauseGroup)
			{
				FindCellPartToStartDependentClause();
			}

			if (m_row.EndDependentClauseGroup)
			{
				FindCellPartToEndDependentClause();
			}

			// Main loop over CellParts in this row
			for (m_icellpart = 0; m_icellpart < m_cellparts.Length; m_icellpart++)
			{
				var hvoCellPart = m_cellparts[m_icellpart];

				// If the column or merge properties of the cell changes, we need to regenerate.
				var cellPartFlidArray = new[]
				{
					ConstituentChartCellPartTags.kflidColumn,
					ConstituentChartCellPartTags.kflidMergesBefore,
					ConstituentChartCellPartTags.kflidMergesAfter
				};
				NoteCellDependencies(cellPartFlidArray, hvoCellPart);

				ProcessCurrentCellPart(hvoCellPart);
			}
			CloseCurrentlyOpenCell();
			// Make any leftover empty cells.
			MakeEmptyCells(m_chart.AllColumns.Length - m_iLastColForWhichCellExists - 1);
		}

		private void ProcessCurrentCellPart(int hvoCellPart)
		{
			var cellPart = m_partRepo.GetObject(hvoCellPart);
			var hvoColContainingCellPart = cellPart.ColumnRA.Hvo;
			if (hvoColContainingCellPart == 0)
			{
				// It doesn't belong to any column! Maybe the template got edited and the column
				// was deleted? Arbitrarily assign it to the first column...logic below
				// may change to the current column if any.
				hvoColContainingCellPart = m_chart.AllColumns[0].Hvo;
				ReportAndFixBadCellPart(hvoCellPart, m_chart.AllColumns[0]);
			}
			if (hvoColContainingCellPart == m_hvoCurCellCol)
			{
				// same column; just add to the already-open cell
				AddCellPartToCell(cellPart);
				return;
			}
			//var ihvoNewCol = m_chart.DisplayFromLogical(GetIndexOfColumn(hvoColContainingCellPart));
			var ihvoNewCol = GetIndexOfColumn(hvoColContainingCellPart);
			if (ihvoNewCol < m_iLastColForWhichCellExists || ihvoNewCol >= m_chart.AllColumns.Length)
			{
				//Debug.Fail(string.Format("Cell part : {0} Chart AllColumns length is: {1} ihvoNewCol is: {2}", cellPart.Guid, m_chart.AllColumns.Length, ihvoNewCol));
				// pathological case...cell part is out of order or its column has been deleted.
				// Maybe the user re-ordered the columns??
				// Anyway, we'll let it go into the current cell.
				var column = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(m_hvoCurCellCol);
				ReportAndFixBadCellPart(hvoCellPart, column);
				AddCellPartToCell(cellPart);
				return;
			}

			// changed column (or started first column). Close the current cell if one is open, and figure out
			// how many cells wide the new one needs to be.
			CloseCurrentlyOpenCell();
			var ccolsAvailableUpToCurrent = ihvoNewCol - m_iLastColForWhichCellExists;
			m_hvoCurCellCol = hvoColContainingCellPart;
			if (cellPart.MergesBefore)
			{
				// Make one cell covering all the columns not already occupied, up to and including the current one.
				// If in fact merging is occurring, align it in the appropriate cell.
				if (ccolsAvailableUpToCurrent > 1)
				{
					m_vwenv.set_IntProperty((int) FwTextPropType.ktptAlign, (int) FwTextPropVar.ktpvEnum, (int) FwTextAlign.ktalTrailing);
				}
				MakeDataCell(ccolsAvailableUpToCurrent);
				m_iLastColForWhichCellExists = ihvoNewCol;
			}
			else
			{
				// Not merging left, first fill in any extra, empty cells.
				MakeEmptyCells(ccolsAvailableUpToCurrent - 1);
				// We have created all cells before ihvoNewCol; need to decide how many to merge right.
				var ccolsNext = 1;
				if (cellPart.MergesAfter)
				{
					// Determine how MANY cells it can use. Find the next CellPart in a different column, if any.
					// It's column determines how many cells are empty. If it merges before, consider
					// giving it a column to merge.
					var iNextColumn = m_chart.AllColumns.Length; // by default can use all remaining columns.
					for (var icellPartNextCol = m_icellpart + 1; icellPartNextCol < m_cellparts.Length; icellPartNextCol++)
					{
						var hvoCellPartInNextCol = m_cellparts[icellPartNextCol];
						var nextColCellPart = m_partRepo.GetObject(hvoCellPartInNextCol);
						var hvoColContainingNextCellPart = nextColCellPart.ColumnRA.Hvo;
						if (hvoColContainingCellPart == hvoColContainingNextCellPart)
						{
							continue;
						}
						iNextColumn = GetIndexOfColumn(hvoColContainingNextCellPart);
						// But, if the next column merges before, and there are at least two empty column,
						// give it one of them.
						if (iNextColumn > ihvoNewCol + 2 && nextColCellPart.MergesBefore)
						{
							iNextColumn--; // use one for the merge before.
						}
						break; // found the first cell in a different column, stop.
					}
					ccolsNext = iNextColumn - ihvoNewCol;
				}
				MakeDataCell(ccolsNext);
				m_iLastColForWhichCellExists = ihvoNewCol + ccolsNext - 1;
			}
			m_cCellPartsInCurrentCell = 0; // none in this cell yet.
			AddCellPartToCell(cellPart);
		}

		private void FindCellPartToEndDependentClause()
		{
			var icellPart = m_cellparts.Length - 1;
			while (icellPart >= 0 && !GoesInsideClauseBrackets(m_cellparts[icellPart]))
			{
				icellPart--;
			}

			m_icellPartCloseClause = icellPart >= 0 ? icellPart : m_cellparts.Length - 1;

			// Find the index of the column with the CellPart before the close bracket (plus 1), or if none, start at col 0.
			var icol = 0;
			if (0 <= m_icellPartCloseClause && m_icellPartCloseClause < m_cellparts.Length)
			{
				var cellPart = m_partRepo.GetObject(m_cellparts[m_icellPartCloseClause]);
				icol = GetIndexOfColumn(cellPart.ColumnRA.Hvo) + 1;
			}
			// starting from there find the last column that has the auto-missing property.
			m_icolLastAutoMissing = -1;
			for (; icol < m_chart.AllColumns.Length; icol++)
			{
				if (m_chart.Logic.ColumnHasAutoMissingMarkers(icol))
				{
					m_icolLastAutoMissing = icol;
				}
			}
			// If we found a subsequent auto-missing column, disable putting the close bracket after the CellPart,
			// it will go after the auto-missing-marker instead.
			if (m_icolLastAutoMissing != -1)
			{
				m_icellPartCloseClause = -1; // terminate after auto-marker.
			}
		}

		private void FindCellPartToStartDependentClause()
		{
			var icellPart = 0;
			while (icellPart < m_cellparts.Length && !GoesInsideClauseBrackets(m_cellparts[icellPart]))
			{
				icellPart++;
			}
			m_icellPartOpenClause = icellPart < m_cellparts.Length ? icellPart : 0;
		}

		private void NoteCellDependencies(int[] cellPartFlidArray, int hvoCellPart)
		{
			var cArray = cellPartFlidArray.Length;
			var hvoArray = new int[cArray];
			for (var i = 0; i < cArray; i++)
			{
				hvoArray[i] = hvoCellPart;
			}

			m_vwenv.NoteDependency(hvoArray, cellPartFlidArray, cArray);
		}

		private void NoteRowDependencies(int[] rowFlidArray)
		{
			var cArray = rowFlidArray.Length;
			var hvoArray = new int[cArray];
			for (var i = 0; i < cArray; i++)
			{
				hvoArray[i] = m_hvoRow;
			}

			m_vwenv.NoteDependency(hvoArray, rowFlidArray, cArray);
		}

		/// <summary>
		/// Report that a CellPart has been detected that has no column, or that is out of order.
		/// We will arbitrarily put it into column hvoCol.
		/// </summary>
		private void ReportAndFixBadCellPart(int hvo, ICmPossibility column)
		{
			if (!m_chart.BadChart)
			{
				MessageBox.Show(LanguageExplorerResources.ksFoundAndFixingInvalidDataCells,
					LanguageExplorerResources.ksInvalidInternalConstituentChartData,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				m_chart.BadChart = true;
			}

			// Suppress Undo handling...we may fix lots of these, it doesn't make sense for the user to
			// try to Undo it, since it would just get fixed again when we display the chart again.
			var actionHandler = m_cache.ActionHandlerAccessor;
			actionHandler.BeginNonUndoableTask();
			try
			{
				var part = m_partRepo.GetObject(hvo);
				part.ColumnRA = column;
			}
			finally
			{
				actionHandler.EndNonUndoableTask();
			}
		}

		/// <summary>
		/// Answer true if the CellPart should go inside the clause bracketing (if any).
		/// </summary>
		private bool GoesInsideClauseBrackets(int hvoPart)
		{
			if (m_chart.Logic.IsWordGroup(hvoPart))
			{
				return true;
			}
			int dummy;
			if (m_chart.Logic.IsClausePlaceholder(hvoPart, out dummy))
			{
				return false;
			}
			return !IsListRef(hvoPart);
		}

		private void AddCellPartToCell(IConstituentChartCellPart cellPart)
		{
			var fSwitchBrackets = m_chart.IsRightToLeft && !(cellPart is IConstChartWordGroup);
			if (m_cCellPartsInCurrentCell != 0)
			{
				m_vwenv.AddString(m_this.SpaceString);
			}
			m_cCellPartsInCurrentCell++;
			if (m_icellpart == m_icellPartOpenClause && !fSwitchBrackets)
			{
				AddOpenBracketBeforeDepClause();
			}
			// RightToLeft weirdness because non-wordgroup stuff doesn't work right!
			if (m_icellpart == m_icellPartCloseClause && fSwitchBrackets)
			{
				AddCloseBracketAfterDepClause();
			}

			if (ConstituentChartLogic.IsMovedText(cellPart))
			{
				m_vwenv.AddObj(cellPart.Hvo, m_this, ConstChartVc.kfragMovedTextCellPart);
			}
			// Is its target a CmPossibility?
			else if (IsListRef(cellPart))
			{
				// If we're about to add our first CellPart and its a ConstChartTag, see if AutoMissingMarker flies.
				if (m_cCellPartsInCurrentCell == 1 && m_chart.Logic.ColumnHasAutoMissingMarkers(m_iLastColForWhichCellExists))
				{
					InsertAutoMissingMarker(m_iLastColForWhichCellExists);
					m_cCellPartsInCurrentCell++;
				}
				m_vwenv.AddObj(cellPart.Hvo, m_this, ConstChartVc.kfragChartListItem);
			}
			// Is its target a user's missing marker (not auto)
			else if (IsMissingMkr(cellPart))
			{
				m_vwenv.AddString(m_missMkr);
			}
			else
			{
				m_vwenv.AddObj(cellPart.Hvo, m_this, ConstChartVc.kfragCellPart);
			}
			if (m_icellpart == m_icellPartCloseClause && !fSwitchBrackets)
			{
				AddCloseBracketAfterDepClause();
			}
			// RightToLeft weirdness because non-wordgroup stuff doesn't work right!
			if (m_icellpart == m_icellPartOpenClause && fSwitchBrackets)
			{
				AddOpenBracketBeforeDepClause();
			}
		}

		private void AddCloseBracketAfterDepClause()
		{
			var key = ConstChartVc.GetRowStyleName(m_row);
			if (m_chart.IsRightToLeft)
			{
				m_this.AddRtLCloseBracketWithRLMs(m_vwenv, key);
			}
			else
			{
				m_this.InsertCloseBracket(m_vwenv, key);
			}
		}

		private void AddOpenBracketBeforeDepClause()
		{
			var key = ConstChartVc.GetRowStyleName(m_row);
			if (m_chart.IsRightToLeft)
			{
				m_this.AddRtLOpenBracketWithRLMs(m_vwenv, key);
			}
			else
			{
				m_this.InsertOpenBracket(m_vwenv, key);
			}
		}

		/// <summary>
		/// This retrieves logical column index in the RTL case.
		/// </summary>
		private int GetIndexOfColumn(int hvoCol)
		{
			int ihvoNewCol;
			//Enhance: GJM -- This routine used to save time by starting from the last column
			// for which a cell existed. But in the RTL case, things get complicated.
			// For now, I'm just using a generic search through all the columns.
			// If this causes a bottle-neck, we may need to loop in reverse for RTL text.
			var startIndex = m_iLastColForWhichCellExists + 1;
			//var startIndex = 0;
			for (ihvoNewCol = startIndex; ihvoNewCol < m_chart.AllColumns.Length; ihvoNewCol++)
			{
				if (hvoCol == m_chart.AllColumns[ihvoNewCol].Hvo)
				{
					break;
				}
			}
			return ihvoNewCol;
		}

		private void CloseCurrentlyOpenCell()
		{
			if (m_hvoCurCellCol == 0)
			{
				return;
			}
			m_vwenv.CloseParagraph();
			m_vwenv.CloseTableCell();
		}

		private void MakeRowLabelCell()
		{
			OpenRowNumberCell(m_vwenv);
			m_vwenv.AddStringProp(ConstChartRowTags.kflidLabel, m_this);
			m_vwenv.CloseTableCell();
		}

		internal static void OpenRowNumberCell(IVwEnv vwenv)
		{
			// Row number cell should not be editable [LT-7744].
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			// Row decorator reverses this if chart is RTL.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 500);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Black));

			vwenv.OpenTableCell(1, 1);
		}

		private void MakeEmptyCells(int count)
		{
			for (var i = 0; i < count; i++)
			{
				var icol = i + m_iLastColForWhichCellExists + 1; // display column index
				OpenStandardCell(icol, 1);
				//if (m_chart.Logic.ColumnHasAutoMissingMarkers(m_chart.LogicalFromDisplay(icol)))
				if (m_chart.Logic.ColumnHasAutoMissingMarkers(icol))
				{
					m_vwenv.OpenParagraph();
					InsertAutoMissingMarker(icol);
					m_vwenv.CloseParagraph();
				}
				m_vwenv.CloseTableCell();
			}
		}

		private void InsertAutoMissingMarker(int icol)
		{
			// RightToLeft weirdness because non-wordgroup stuff doesn't work right!
			if (icol == m_icolLastAutoMissing && m_chart.IsRightToLeft)
			{
				AddCloseBracketAfterDepClause();
			}
			if (m_icellPartOpenClause == m_icellpart && !m_chart.IsRightToLeft)
			{
				AddOpenBracketBeforeDepClause();
				m_icellPartOpenClause = -1; // suppresses normal open and in any subsequent auto-missing cells.
			}
			m_vwenv.AddString(m_missMkr);
			if (m_icellPartOpenClause == m_icellpart && m_chart.IsRightToLeft)
			{
				AddOpenBracketBeforeDepClause();
				m_icellPartOpenClause = -1; // suppresses normal open and in any subsequent auto-missing cells.
			}

			if (icol == m_icolLastAutoMissing && !m_chart.IsRightToLeft)
			{
				AddCloseBracketAfterDepClause();
			}
		}

		private void MakeDataCell(int ccols)
		{
			var icol = GetIndexOfColumn(m_hvoCurCellCol);
			OpenStandardCell(icol, ccols);
			m_vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
			m_vwenv.OpenParagraph();
		}

		private void OpenStandardCell(int icol, int ccols)
		{
			if (m_chart.Logic.IsHighlightedCell(m_row.IndexInOwner, icol))
			{
				// use m_vwenv.set_IntProperty to set ktptBackColor for cells where the ChOrph could be inserted
				m_vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.LightGreen));
			}
			OpenStandardCell(m_vwenv, ccols, m_chart.Logic.GroupEndIndices.Contains(icol));
		}

		private void OpenNoteCell()
		{
			// LT-8545 remaining niggle; Note shouldn't be formatted.
			// A small change to the XML config file ensures it's not underlined either.
			m_this.ApplyFormatting(m_vwenv, "normal");
			OpenStandardCell(m_vwenv, 1, false);
		}

		internal static void OpenStandardCell(IVwEnv vwenv, int ccols, bool fEndOfGroup)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, (fEndOfGroup ? 1500 : 500));
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(fEndOfGroup ? Color.Black : Color.LightGray));
			vwenv.OpenTableCell(1, ccols);
		}

		/// <summary>
		/// Return true if the CellPart is a ConstChartTag (which in a CellPart list makes it
		/// a reference to a CmPossibility), also known as a generic marker. But we still
		/// want to return false if the Tag is null, because then its a "Missing" marker.
		/// This version takes the hvo of the CellPart.
		/// </summary>
		private bool IsListRef(int hvoCellPart)
		{
			return IsListRef(m_partRepo.GetObject(hvoCellPart));
		}

		/// <summary>
		/// Return true if the CellPart is a ConstChartTag (which in a CellPart list makes it
		/// a reference to a CmPossibility), also known as a generic marker. But we still
		/// want to return false if the Tag is null, because then its a "Missing" marker.
		/// This version takes the actual CellPart object.
		/// </summary>
		private static bool IsListRef(IConstituentChartCellPart cellPart)
		{
			return (cellPart as IConstChartTag)?.TagRA != null;
		}

		/// <summary>
		/// Return true if the CellPart is a ConstChartTag, but the Tag is null,
		/// because then its a "Missing" marker.
		/// Takes the actual CellPart object.
		/// </summary>
		private static bool IsMissingMkr(IConstituentChartCellPart cellPart)
		{
			var part = cellPart as IConstChartTag;
			return part != null && part.TagRA == null;
		}
	}
}