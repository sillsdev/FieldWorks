// Copyright (c) 2015-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Discourse
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
		private readonly ConstChartBody m_body;
		private int[] m_cellParts;
		/// <summary>
		/// Column for which cell is currently open (initially not for any column)
		/// </summary>
		private int m_hvoCurCellCol;
		/// <summary>
		/// Index (display) of last column for which we have made (at least opened) a cell.
		/// </summary>
		private int m_iLastColForWhichCellExists = -1;
		/// <summary>
		/// Index of cell part to insert clause bracket before; gets reset if we find an auto-missing-marker col first.
		/// </summary>
		private int m_iCellPartOpenClause = -1;
		/// <summary>
		/// Index of cell part to insert clause bracket after (unless m_iColLastAutoMissing is a later column).
		/// </summary>
		private int m_iCellPartCloseClause = -1;
		/// <summary>
		/// Number of cell parts output in current cell.
		/// </summary>
		private int m_cCellPartsInCurrentCell;
		private int m_iCellPart;
		/// <summary>
		/// Index of last column where automatic missing markers are put.
		/// </summary>
		private int m_iColLastAutoMissing = -1;
		/// <summary>
		/// Stores the TsString displayed for missing markers (auto or user)
		/// </summary>
		private ITsString m_missMkr;

		#region Repository member variables


		private readonly IConstituentChartCellPartRepository m_partRepo;

		#endregion

		/// <summary/>
		public MakeCellsMethod(ConstChartVc baseObj, LcmCache cache, IVwEnv vwenv, int hvo)
		{
			m_this = baseObj;
			m_cache = cache;
			var rowRepo = m_cache.ServiceLocator.GetInstance<IConstChartRowRepository>();
			m_partRepo = m_cache.ServiceLocator.GetInstance<IConstituentChartCellPartRepository>();

			// Decorator makes sure that things get put out in the right order if chart is RtL
			m_body = baseObj.m_body;
			m_vwenv = new ChartRowEnvDecorator(vwenv);

			m_hvoRow = hvo;
			m_row = rowRepo.GetObject(m_hvoRow);
		}

		private void SetupMissingMarker()
		{
			m_missMkr = TsStringUtils.MakeString(DiscourseStrings.ksMissingMarker, m_cache.DefaultAnalWs);
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

			if (!(m_body.Chart.NotesColumnOnRight ^ fRtL))
				MakeNoteCell();

			MakeRowLabelCell();

			MakeMainCellParts(); // Make all the cell parts between row label and note.

			if (m_body.Chart.NotesColumnOnRight ^ fRtL)
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
			m_cellParts = m_row.CellsOS.ToHvoArray();

			if (m_row.StartDependentClauseGroup)
				FindCellPartToStartDependentClause();

			if (m_row.EndDependentClauseGroup)
				FindCellPartToEndDependentClause();

			// Main loop over CellParts in this row
			for (m_iCellPart = 0; m_iCellPart < m_cellParts.Length; m_iCellPart++)
			{
				var hvoCellPart = m_cellParts[m_iCellPart];

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
			MakeEmptyCells(m_body.AllColumns.Length - m_iLastColForWhichCellExists - 1);
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
				hvoColContainingCellPart = m_body.AllColumns[0].Hvo;
				ReportAndFixBadCellPart(hvoCellPart, m_body.AllColumns[0]);
			}
			if (hvoColContainingCellPart == m_hvoCurCellCol)
			{
				// same column; just add to the already-open cell
				AddCellPartToCell(cellPart);
				return;
			}
			var iHvoNewCol = GetIndexOfColumn(hvoColContainingCellPart);
			if (iHvoNewCol < m_iLastColForWhichCellExists || iHvoNewCol >= m_body.AllColumns.Length)
			{
				//Debug.Fail(string.Format("Cell part : {0} Chart AllColumns length is: {1} iHvoNewCol is: {2}", cellPart.Guid, m_chart.AllColumns.Length, iHvoNewCol));
				// pathological case...cell part is out of order or its column has been deleted.
				// Maybe the user re-ordered the columns??
				// Anyway, we'll let it go into the current cell.
				if (!m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().TryGetObject(m_hvoCurCellCol, out var column))
				{
					column = m_body.AllColumns[0];
				}
				ReportAndFixBadCellPart(hvoCellPart, column);
				if (hvoColContainingCellPart == m_hvoCurCellCol)
				{
					// same column; just add to the already-open cell
					AddCellPartToCell(cellPart);
					return;
				}
			}

			// changed column (or started first column). Close the current cell if one is open, and figure out
			// how many cells wide the new one needs to be.
			CloseCurrentlyOpenCell();
			var cColsAvailableUpToCurrent = iHvoNewCol - m_iLastColForWhichCellExists;
			m_hvoCurCellCol = hvoColContainingCellPart;
			if (cellPart.MergesBefore)
			{
				// Make one cell covering all the columns not already occupied, up to and including the current one.
				// If in fact merging is occurring, align it in the appropriate cell.
				if (cColsAvailableUpToCurrent > 1)
				{
					m_vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
						(int)FwTextAlign.ktalTrailing);
				}
				MakeDataCell(cColsAvailableUpToCurrent);
				m_iLastColForWhichCellExists = iHvoNewCol;
			}
			else
			{
				// Not merging left, first fill in any extra, empty cells.
				MakeEmptyCells(cColsAvailableUpToCurrent - 1);
				// We have created all cells before iHvoNewCol; need to decide how many to merge right.
				var cColsNext = 1;
				if (cellPart.MergesAfter)
				{
					// Determine how MANY cells it can use. Find the next CellPart in a different column, if any.
					// It's column determines how many cells are empty. If it merges before, consider giving it a column to merge.
					var iNextColumn = m_body.AllColumns.Length; // by default can use all remaining columns.
					for (var iCellPartNextCol = m_iCellPart + 1; iCellPartNextCol < m_cellParts.Length; iCellPartNextCol++)
					{
						var hvoCellPartInNextCol = m_cellParts[iCellPartNextCol];
						var nextColCellPart = m_partRepo.GetObject(hvoCellPartInNextCol);
						var hvoColContainingNextCellPart = nextColCellPart.ColumnRA.Hvo;
						if (hvoColContainingCellPart == hvoColContainingNextCellPart)
							continue;
						iNextColumn = GetIndexOfColumn(hvoColContainingNextCellPart);
						// But, if the next column merges before, and there are at least two empty column, give it one of them.
						if (iNextColumn > iHvoNewCol + 2 && nextColCellPart.MergesBefore)
							iNextColumn--; // use one for the merge before.
						break; // found the first cell in a different column, stop.
					}
					cColsNext = iNextColumn - iHvoNewCol;
				}
				MakeDataCell(cColsNext);
				m_iLastColForWhichCellExists = iHvoNewCol + cColsNext - 1;
			}
			m_cCellPartsInCurrentCell = 0; // none in this cell yet.
			AddCellPartToCell(cellPart);
		}

		private void FindCellPartToEndDependentClause()
		{
			var iCellPart = m_cellParts.Length - 1;
			while (iCellPart >= 0 && !GoesInsideClauseBrackets(m_cellParts[iCellPart]))
				iCellPart--;

			m_iCellPartCloseClause = iCellPart >= 0 ? iCellPart : m_cellParts.Length - 1;

			// Find the index of the column with the CellPart before the close bracket (plus 1), or if none, start at col 0.
			var iCol = 0;
			if (0 <= m_iCellPartCloseClause && m_iCellPartCloseClause < m_cellParts.Length)
			{
				var cellPart = m_partRepo.GetObject(m_cellParts[m_iCellPartCloseClause]);
				iCol = GetIndexOfColumn(cellPart.ColumnRA.Hvo) + 1;
			}
			// starting from there find the last column that has the auto-missing property.
			m_iColLastAutoMissing = -1;
			for (; iCol < m_body.AllColumns.Length; iCol++)
				if (m_body.Logic.ColumnHasAutoMissingMarkers(iCol))
					m_iColLastAutoMissing = iCol;
			// If we found a subsequent auto-missing column, disable putting the close bracket after the CellPart,
			// it will go after the auto-missing-marker instead.
			if (m_iColLastAutoMissing != -1)
				m_iCellPartCloseClause = -1; // terminate after auto-marker.
		}

		private void FindCellPartToStartDependentClause()
		{
			var iCellPart = 0;
			while (iCellPart < m_cellParts.Length && !GoesInsideClauseBrackets(m_cellParts[iCellPart]))
				iCellPart++;
			m_iCellPartOpenClause = iCellPart < m_cellParts.Length ? iCellPart : 0;
		}

		private void NoteCellDependencies(int[] cellPartFlidArray, int hvoCellPart)
		{
			var cArray = cellPartFlidArray.Length;
			var hvoArray = new int[cArray];
			for (var i = 0; i < cArray; i++)
				hvoArray[i] = hvoCellPart;

			m_vwenv.NoteDependency(hvoArray, cellPartFlidArray, cArray);
		}

		private void NoteRowDependencies(int[] rowFlidArray)
		{
			var cArray = rowFlidArray.Length;
			var hvoArray = new int[cArray];
			for (var i = 0; i < cArray; i++)
				hvoArray[i] = m_hvoRow;

			m_vwenv.NoteDependency(hvoArray, rowFlidArray, cArray);
		}

		/// <summary>
		/// Report that a CellPart has been detected that has no column, or that is out of order.
		/// We will arbitrarily put it into column hvoCol.
		/// </summary>
		private void ReportAndFixBadCellPart(int hvo, ICmPossibility column)
		{
			if (!m_body.BadChart)
			{
				MessageBox.Show(DiscourseStrings.ksFoundAndFixingInvalidDataCells,
					DiscourseStrings.ksInvalidInternalConstituentChartData,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				m_body.BadChart = true;
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
			if (m_body.Logic.IsWordGroup(hvoPart))
				return true;
			int dummy;
			if (m_body.Logic.IsClausePlaceholder(hvoPart, out dummy))
				return false;
			return !IsListRef(hvoPart);
		}

		private void AddCellPartToCell(IConstituentChartCellPart cellPart)
		{
			var fSwitchBrackets = m_body.IsRightToLeft && !(cellPart is IConstChartWordGroup);
			if (m_cCellPartsInCurrentCell != 0)
				m_vwenv.AddString(m_this.SpaceString);
			m_cCellPartsInCurrentCell++;
			if (m_iCellPart == m_iCellPartOpenClause && !fSwitchBrackets)
			{
				AddOpenBracketBeforeDepClause();
			}
			// RightToLeft weirdness because non-WordGroup stuff doesn't work right!
			if (m_iCellPart == m_iCellPartCloseClause && fSwitchBrackets)
			{
				AddCloseBracketAfterDepClause();
			}
			if (ConstituentChartLogic.IsMovedText(cellPart))
				m_vwenv.AddObj(cellPart.Hvo, m_this, ConstChartVc.kfragMovedTextCellPart);
			// Is its target a CmPossibility?
			else if (IsListRef(cellPart))
			{
				// If we're about to add our first CellPart and its a ConstChartTag, see if AutoMissingMarker flies.
				if (m_cCellPartsInCurrentCell == 1 && m_body.Logic.ColumnHasAutoMissingMarkers(m_iLastColForWhichCellExists))
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
			if (m_iCellPart == m_iCellPartCloseClause && !fSwitchBrackets)
			{
				AddCloseBracketAfterDepClause();
			}
			// RightToLeft weirdness because non-WordGroup stuff doesn't work right!
			if (m_iCellPart == m_iCellPartOpenClause && fSwitchBrackets)
			{
				AddOpenBracketBeforeDepClause();
			}
		}

		private void AddCloseBracketAfterDepClause()
		{
			var key = ConstChartVc.GetRowStyleName(m_row);
			if (m_body.IsRightToLeft)
				m_this.AddRtLCloseBracketWithRLMs(m_vwenv, key);
			else
				m_this.InsertCloseBracket(m_vwenv, key);
		}

		private void AddOpenBracketBeforeDepClause()
		{
			var key = ConstChartVc.GetRowStyleName(m_row);
			if (m_body.IsRightToLeft)
				m_this.AddRtLOpenBracketWithRLMs(m_vwenv, key);
			else
				m_this.InsertOpenBracket(m_vwenv, key);
		}

		/// <summary>
		/// This retrieves logical column index in the RTL case.
		/// </summary>
		private int GetIndexOfColumn(int hvoCol)
		{
			int iHvoNewCol;
			//Enhance: GJM -- This routine used to save time by starting from the last column
			// for which a cell existed. But in the RTL case, things get complicated.
			// For now, I'm just using a generic search through all the columns.
			// If this causes a bottle-neck, we may need to loop in reverse for RTL text.
			var startIndex = m_iLastColForWhichCellExists + 1;
			for (iHvoNewCol = startIndex; iHvoNewCol < m_body.AllColumns.Length; iHvoNewCol++)
			{
				if (hvoCol == m_body.AllColumns[iHvoNewCol].Hvo)
					break;
			}
			return iHvoNewCol;
		}

		private void CloseCurrentlyOpenCell()
		{
			if (m_hvoCurCellCol == 0)
				return;
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
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			// Row decorator reverses this if chart is RTL.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 500);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Black));

			vwenv.OpenTableCell(1, 1);
		}

		private void MakeEmptyCells(int count)
		{
			for (var i = 0; i < count; i++)
			{
				var iCol = i + m_iLastColForWhichCellExists + 1; // display column index
				OpenStandardCell(iCol, 1);
				if (m_body.Logic.ColumnHasAutoMissingMarkers(iCol))
				{
					m_vwenv.OpenParagraph();
					InsertAutoMissingMarker(iCol);
					m_vwenv.CloseParagraph();
				}
				m_vwenv.CloseTableCell();
			}
		}

		private void InsertAutoMissingMarker(int iCol)
		{
			// RightToLeft weirdness because non-WordGroup stuff doesn't work right!
			if (iCol == m_iColLastAutoMissing && m_body.IsRightToLeft)
				AddCloseBracketAfterDepClause();
			if (m_iCellPartOpenClause == m_iCellPart && !m_body.IsRightToLeft)
			{
				AddOpenBracketBeforeDepClause();
				m_iCellPartOpenClause = -1; // suppresses normal open and in any subsequent auto-missing cells.
			}
			m_vwenv.AddString(m_missMkr);
			if (m_iCellPartOpenClause == m_iCellPart && m_body.IsRightToLeft)
			{
				AddOpenBracketBeforeDepClause();
				m_iCellPartOpenClause = -1; // suppresses normal open and in any subsequent auto-missing cells.
			}
			if (iCol == m_iColLastAutoMissing && !m_body.IsRightToLeft)
				AddCloseBracketAfterDepClause();
		}

		private void MakeDataCell(int cCols)
		{
			var iCol = GetIndexOfColumn(m_hvoCurCellCol);
			OpenStandardCell(iCol, cCols);
			m_vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
			m_vwenv.OpenParagraph();
		}

		private void OpenStandardCell(int iCol, int cCols)
		{
			if (m_body.Logic.IsHighlightedCell(m_row.IndexInOwner, iCol))
			{
				// use m_vwenv.set_IntProperty to set ktptBackColor for cells where the ChOrph could be inserted
				m_vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
					(int)FwTextPropVar.ktpvDefault,
					(int)ColorUtil.ConvertColorToBGR(Color.LightGreen));
			}

			var columnsRow = m_body.Logic.ColumnsAndGroups.Headers.Last();
			OpenStandardCell(m_vwenv, cCols, columnsRow[iCol].IsLastInGroup);
		}

		private void OpenNoteCell()
		{
			// LT-8545 remaining niggle; Note shouldn't be formatted.
			// A small change to the XML config file ensures it's not underlined either.
			m_vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
					(int)FwTextPropVar.ktpvMilliPoint, 1500);
			m_vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
					(int)FwTextPropVar.ktpvDefault,
					(int)ColorUtil.ConvertColorToBGR(Color.Black));
			m_this.ApplyFormatting(m_vwenv, "normal");
			m_vwenv.OpenTableCell(1, 1);
		}

		internal static void OpenStandardCell(IVwEnv vwenv, int cCols, bool fEndOfGroup)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
				(int)FwTextPropVar.ktpvMilliPoint,
				(fEndOfGroup ? 1500 : 500));
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(fEndOfGroup ? Color.Black : Color.LightGray));
			vwenv.OpenTableCell(1, cCols);
		}

		/// <summary>
		/// Return true if the CellPart is a ConstChartTag (which in a CellPart list makes it
		/// a reference to a CmPossibility), also known as a generic marker. But we still
		/// want to return false if the Tag is null, because then its a "Missing" marker.
		/// This version takes the hvo of the CellPart.
		/// </summary>
		private bool IsListRef(int hvoCellPart)
		{
			var cellPart = m_partRepo.GetObject(hvoCellPart);
			return IsListRef(cellPart);
		}

		/// <summary>
		/// Return true if the CellPart is a ConstChartTag (which in a CellPart list makes it
		/// a reference to a CmPossibility), also known as a generic marker. But we still
		/// want to return false if the Tag is null, because then its a "Missing" marker.
		/// This version takes the actual CellPart object.
		/// </summary>
		private static bool IsListRef(IConstituentChartCellPart cellPart)
		{
			return cellPart is IConstChartTag part && part.TagRA != null;
		}

		/// <summary>
		/// Return true if the CellPart is a ConstChartTag, but the Tag is null,
		/// because then its a "Missing" marker.
		/// Takes the actual CellPart object.
		/// </summary>
		private static bool IsMissingMkr(IConstituentChartCellPart cellPart)
		{
			return cellPart is IConstChartTag part && part.TagRA == null;
		}
	}
}
