// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// A Method Object used to merge the contents of chart cells
	/// </summary>
	internal class MergeCellContentsMethod
	{
		readonly ConstituentChartLogic m_logic;
		private readonly IConstChartMovedTextMarkerRepository m_mtmRepo;
		readonly ChartLocation m_srcCell;
		readonly ChartLocation m_dstCell;
		readonly bool m_forward;
		List<IConstituentChartCellPart> m_cellPartsSrc;
		List<IConstituentChartCellPart> m_cellPartsDest;
		List<IConstChartWordGroup> m_wordGroupsSrc;
		List<IConstChartWordGroup> m_wordGroupsDest;
		IConstChartWordGroup m_wordGroupToMerge;
		IConstChartWordGroup m_wordGroupToMergeWith;

		public MergeCellContentsMethod(ConstituentChartLogic logic, ChartLocation srcCell, ChartLocation dstCell, bool forward)
		{
			m_logic = logic;
			m_srcCell = srcCell;
			m_dstCell = dstCell;
			m_forward = forward;
			m_mtmRepo = Cache.ServiceLocator.GetInstance<IConstChartMovedTextMarkerRepository>();
		}

		private LcmCache Cache => m_logic.Cache;

		private IConstChartRow SrcRow => m_srcCell.Row;

		private int HvoSrcRow => m_srcCell.HvoRow;

		private int SrcColIndex => m_srcCell.ColIndex;

		private IConstChartRow DstRow => m_dstCell.Row;

		private int HvoDstRow => m_dstCell.HvoRow;

		private int DstColIndex => m_dstCell.ColIndex;

		/// <summary>
		/// Remove the word group from m_wordGroupsSrc (and its Row).
		/// </summary>
		/// <param name="part"></param>
		/// <param name="indexDest">keeps track of Destination insertion point</param>
		private int RemoveSourceWordGroup(IConstChartWordGroup part, int indexDest)
		{
			var irow = SrcRow.IndexInOwner;
			m_cellPartsSrc.Remove(part);
			m_wordGroupsSrc.Remove(part);
			SrcRow.CellsOS.Remove(part);
			if (SrcRow.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
			{
				RenumberRowsFromDeletedRow(irow);
				return 0;
			}
			if ((HvoDstRow == HvoSrcRow) && m_forward)
			{
				indexDest--; // To compensate for lost item(WordGroup) in row.Cells
			}
			return indexDest;
		}

		private void RenumberRowsFromDeletedRow(int irow)
		{
			m_logic.RenumberRows(ConstituentChartLogic.DecrementRowSafely(irow), false);
		}

		/// <summary>
		/// Remove the MovedTextMarker from wordGroupsToMergeWith (and its Row).
		/// </summary>
		/// <param name="mtm"></param>
		/// <param name="indexDest">keeps track of Destination insertion point</param>
		void RemoveRedundantMTMarker(IConstChartMovedTextMarker mtm, ref int indexDest)
		{
			m_cellPartsDest.Remove(mtm);
			DstRow.CellsOS.Remove(mtm);
			if ((HvoDstRow == HvoSrcRow) && m_forward)
			{
				indexDest--; // To compensate for lost item(WordGroup) in row.Cells; is a preposed marker
			}
		}

		internal void Run()
		{
			// Get markers and WordGroups from source and verify that source cell has some data.
			m_cellPartsSrc = m_logic.PartsInCell(m_srcCell);
			m_wordGroupsSrc = ConstituentChartLogic.CollectCellWordGroups(m_cellPartsSrc);
			if (m_wordGroupsSrc.Count == 0)
			{
				return; // no words to move!!
			}

			// If the destination contains a missing marker get rid of it! Don't try to 'merge' with it.
			m_logic.RemoveMissingMarker(m_dstCell);

			// Get markers and WordGroups from destination
			int indexDest; // This will keep track of where we are in rowDst.CellsOS
			m_cellPartsDest = m_logic.CellPartsInCell(m_dstCell, out indexDest);
			// indexDest is now the index in the destination row's CellsOS of the first WordGroup in the destination cell.
			m_wordGroupsDest = ConstituentChartLogic.CollectCellWordGroups(m_cellPartsDest);

			// Here is where we need to check to see if the destination cell contains any movedText markers
			// for text in the source cell. If it does, the marker goes away, the movedText feature goes away,
			// and that MAY leave us ready to do a PreMerge in the source cell prior to merging the cells.
			if (CheckForRedundantMTMarkers(ref indexDest))
			{
				indexDest = TryPreMerge(indexDest);
			}

			// The above check for MTMarkers may cause this 'if' to be true, but we may still need to delete objects
			if (m_cellPartsDest.Count == 0)
			{
				// The destination is completely empty. Just move the m_wordGroupsSrc.
				if (HvoSrcRow == HvoDstRow)
				{
					// This is where we worry about reordering SrcRow.CellsOS if other items (tags?) exist between
					// the m_wordGroupsSrc and the destination (since non-data stuff doesn't move).
					// Moving forward past a chart Tag.
					if (m_wordGroupsSrc.Count != m_cellPartsSrc.Count && m_forward && m_wordGroupsSrc[0].Hvo == m_cellPartsSrc[0].Hvo)
					{
						MoveWordGroupsToEndOfCell(indexDest); // in preparation to moving data to next cell
					}
					// This is where we worry about reordering SrcRow.CellsOS if other items (MovedTextMrkr?) exist between
					// the m_wordGroupsSrc and the destination (since non-data stuff doesn't move).
					// Moving back past a MovedTextMarker.
					if (m_wordGroupsSrc.Count != m_cellPartsSrc.Count && !m_forward && m_wordGroupsSrc[0].Hvo != m_cellPartsSrc[0].Hvo)
					{
						MoveWordGroupsToBeginningOfCell(indexDest); // in preparation to moving data to previous cell
					}
					m_logic.ChangeColumn(m_wordGroupsSrc.ToArray(), m_logic.AllMyColumns[DstColIndex], SrcRow);
				}
				else
				{
					if (SrcColIndex != DstColIndex)
					{
						m_logic.ChangeColumnInternal(m_wordGroupsSrc.ToArray(), m_logic.AllMyColumns[DstColIndex]);
					}

					// Enhance GordonM: If we ever allow markers between WordGroups, this [& ChangeRow()] will need to change.
					MoveCellPartsToDestRow(indexDest);
				}
				return;
			}

			if (m_logic.IsPreposedMarker(m_cellPartsDest[0]))
			{
				indexDest++; // Insertion point should be after preposed marker
			}

			// Set up possible coalescence of WordGroups.
			PrepareForMerge();

			// At this point 'm_wordGroupToMerge' and 'm_wordGroupToMergeWith' are the only WordGroups that might actually
			// coalesce. Neither is guaranteed to be non-null (either one could be movedText, which is not mergeable).
			// If either is null, there will be no coalescence.
			// But there may well be other word groups in 'm_wordGroupsSrc' that will need to move.

			if (m_wordGroupToMerge != null)
			{
				if (m_wordGroupToMergeWith != null)
				{
					// Merge the word groups and delete the empty one.
					indexDest = MergeTwoWordGroups(indexDest);
				}
				else
				{
					// Move the one(s) we have. This is accomplished by just leaving it in
					// the list (now m_wordGroupsSrc).
					// Enhance JohnT: Possibly we may eventually have different rules about
					// where in the destination cell to put it?
				}
			}

			if (m_wordGroupsSrc.Count == 0)
			{
				return;
			}

			// Change column of any surviving items in m_wordGroupsSrc.
			if (SrcColIndex != DstColIndex)
			{
				m_logic.ChangeColumnInternal(m_wordGroupsSrc.ToArray(), m_logic.AllMyColumns[DstColIndex]);
			}

			// If we're on the same row and there aren't any intervening markers, we're done.
			if (SrcRow.Hvo == DstRow.Hvo && (indexDest == m_wordGroupsSrc[0].IndexInOwner + m_wordGroupsSrc.Count))
			{
				return;
			}

			indexDest = FindWhereToInsert(indexDest); // Needs an accurate 'm_wordGroupsDest'
			MoveCellPartsToDestRow(indexDest);

			// Review: what should we do about dependent clause markers pointing at the destination row?
		}

		/// <summary>
		/// Merge the word groups and delete the empty one, maintaining an accurate index
		/// in the destination.
		/// </summary>
		private int MergeTwoWordGroups(int indexDest)
		{
			m_logic.MoveAnalysesBetweenWordGroups(m_wordGroupToMerge, m_wordGroupToMergeWith);

			// remove WordGroup from source lists and delete, keeping accurate destination index
			return RemoveSourceWordGroup(m_wordGroupToMerge, indexDest);
		}

		/// <summary>
		/// Moves the WordGroups to the end of the source cell in preparation to move them to the next cell.
		/// Solves confusion in row.Cells
		/// Situation: moving forward in same row, nothing in destination cell.
		/// </summary>
		private void MoveWordGroupsToEndOfCell(int iDest)
		{
			var istart = m_wordGroupsSrc[0].IndexInOwner;
			Debug.Assert(0 < iDest && iDest >= istart, "Bad destination index.");
			// Does MoveTo work when the src and dest sequences are the same? Yes.
			SrcRow.CellsOS.MoveTo(istart, istart + m_wordGroupsSrc.Count - 1, SrcRow.CellsOS, iDest);
		}

		/// <summary>
		/// Moves the WordGroups to the beginning of the source cell in preparation to move them to the previous cell.
		/// Solves confusion in row.Cells
		/// Situation: moving back in same row, nothing in destination cell.
		/// </summary>
		private void MoveWordGroupsToBeginningOfCell(int iDest)
		{
			var istart = m_wordGroupsSrc[0].IndexInOwner;
			Debug.Assert(-1 < iDest && iDest <= istart, "Bad destination index.");
			// Does MoveTo work when going backwards? Yes.
			SrcRow.CellsOS.MoveTo(istart, istart + m_wordGroupsSrc.Count - 1, SrcRow.CellsOS, iDest);
		}

		private int TryPreMerge(int indexDest)
		{
			// Look through source WordGroups to see if any can be merged now that we've removed
			// a movedText feature prior to moving to a neighboring cell.
			var flastWasNotMT = false;
			for (var iSrc = 0; iSrc < m_wordGroupsSrc.Count; iSrc++)
			{
				var currWordGrp = m_wordGroupsSrc[iSrc];
				if (ConstituentChartLogic.IsMovedText(currWordGrp))
				{
					flastWasNotMT = false;
					continue;
				}
				if (flastWasNotMT)
				{
					// Merge this WordGroup with the last one and delete this one
					var destWordGrp = m_wordGroupsSrc[iSrc - 1];
					m_logic.MoveAnalysesBetweenWordGroups(currWordGrp, destWordGrp);
					// mark WordGroup for delete and remove from source lists, keeping accurate destination index
					indexDest = RemoveSourceWordGroup(currWordGrp, indexDest);
					iSrc--;
				}
				flastWasNotMT = true;
			}
			return indexDest;
		}

		private bool CheckForRedundantMTMarkers(ref int indexDest)
		{
			var found = false;
			for (var iDes = 0; iDes < m_cellPartsDest.Count; )
			{
				// Not foreach because we might delete from m_cellPartsDest
				var part = m_cellPartsDest[iDes];

				// The source cell part is still in its old row and column, so we can detect this directly.
				if (IsMarkerForCell(part, m_srcCell))
				{
					var part1 = part as IConstChartMovedTextMarker;
					// Turn off feature in source
					//TurnOffMovedTextFeatureFromMarker(part1, SrcRow);
					// Take out movedText marker, keep accurate destination index
					RemoveRedundantMTMarker(part1, ref indexDest);
					found = true;
					continue;
				}
				iDes++; // only if we do NOT clobber it.
			}
			return found;
		}

		/// <summary>
		/// Moves all WordGroups in m_wordGroupsSrc from SrcRow to DstRow.
		/// </summary>
		/// <param name="indexDest">Index in rowDst.Cells where insertion should occur.</param>
		private void MoveCellPartsToDestRow(int indexDest)
		{
			var irow = SrcRow.IndexInOwner;
			var istart = m_wordGroupsSrc[0].IndexInOwner;
			var iend = m_wordGroupsSrc[m_wordGroupsSrc.Count - 1].IndexInOwner;
			SrcRow.CellsOS.MoveTo(istart, iend, DstRow.CellsOS, indexDest);
			if (SrcRow.Hvo == (int) SpecialHVOValues.kHvoObjectDeleted)
			{
				RenumberRowsFromDeletedRow(irow);
			}
		}

		private void PrepareForMerge()
		{
			// Destination cell has something in it, but it might not be a WordGroup!
			if (m_wordGroupsDest.Count == 0)
			{
				m_wordGroupToMergeWith = null;
			}
			else
			{
				m_wordGroupToMergeWith = m_forward ? m_wordGroupsDest[0] : m_wordGroupsDest[m_wordGroupsDest.Count - 1];

				if (ConstituentChartLogic.IsMovedText(m_wordGroupToMergeWith))
				{
					// Can't merge with movedText, must append/insert merging WordGroup instead.
					m_wordGroupToMergeWith = null;
				}
			}

			m_wordGroupToMerge = m_forward ? m_wordGroupsSrc[m_wordGroupsSrc.Count - 1] : m_wordGroupsSrc[0];

			if (ConstituentChartLogic.IsMovedText(m_wordGroupToMerge))
			{
				// Can't merge with movedText, must append/insert merging WordGroup instead.
				m_wordGroupToMerge = null;
			}
		}

		/// <summary>
		/// Return true if srcPart is a moved text marker with its destination in the specified cell.
		/// </summary>
		private bool IsMarkerForCell(IConstituentChartCellPart srcPart, ChartLocation cell)
		{
			// typically it is null if the thing we're testing isn't the right kind of CellPart.
			var target = (srcPart as IConstChartMovedTextMarker)?.WordGroupRA;
			if (target == null || target.ColumnRA != m_logic.AllMyColumns[cell.ColIndex])
			{
				return false;
			}
			return cell.Row.CellsOS.Contains(target);
		}

		/// <summary>
		/// If we find a moved text marker pointing at 'target', delete it.
		/// </summary>
		private void CleanUpMovedTextMarkerFor(IConstChartWordGroup target)
		{
			IConstChartRow row;
			var marker = FindMovedTextMarkerFor(target, out row);
			if (marker == null)
			{
				return;
			}
			row.CellsOS.Remove(marker);
		}

		private IConstChartMovedTextMarker FindMovedTextMarkerFor(IConstChartWordGroup target, out IConstChartRow rowTarget)
		{
			// If we find a moved text marker pointing at 'target', return it and (through the 'out' var) its row.
			// Enhance JohnT: it MIGHT be faster (in long texts) to use a back ref. Or we might limit the
			// search to nearby rows...
			var myMTM = m_mtmRepo.AllInstances().FirstOrDefault(mtm => mtm.WordGroupRA == target);
			if (myMTM != null)
			{
				rowTarget = myMTM.Owner as IConstChartRow;
				return myMTM;
			}
			rowTarget = null;
			return null;
		}

		/// <summary>
		/// Returns index of where in destination row's Cells sequence we want to insert
		/// remaining source WordGroups. Uses m_wordGroupsDest list. And m_forward and DstRow
		/// and m_cellPartsDest and indexDest.
		/// </summary>
		/// <param name="indexDest">Enters as the index in row.Cells of the beginning of the destination cell.</param>
		/// <returns></returns>
		private int FindWhereToInsert(int indexDest)
		{
			Debug.Assert(0 <= indexDest && indexDest <= DstRow.CellsOS.Count);
			if (m_cellPartsDest.Count == 0)
			{
				return indexDest; // If indexDest == Cells.Count, we should take this branch.
			}

			// Enhance GordonM: If we ever allow other markers before the first WordGroup or
			// we allow markers between WordGroups, this will need to change.
			if (m_forward)
			{
				return indexDest;
			}
			return indexDest + m_wordGroupsDest.Count;
		}
	}
}