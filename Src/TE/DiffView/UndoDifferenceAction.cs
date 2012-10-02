// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoDifferenceAction.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	#region UndoDifferenceAction class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Currently not used - combinations of undo/redo that combine major differences with this
	/// simpler difference get confused because of the cloning of the list in the major difference.
	/// This can cause the remove from a list to fail.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class UndoDifferenceAction: UndoRefreshAction
	{
		private Difference m_differenceClone;
		private Difference m_differenceRef;
		private DifferenceList m_reviewedList;
		private DifferenceList m_differenceList;
		private int m_index;
		private bool m_fReverted;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="differenceList"></param>
		/// <param name="reviewedList"></param>
		/// <param name="fRevert"><code>true</code> if we are reverting the difference (as
		/// opposed to keeping the current). If <c>true</c> a revert action is being created</param>
		/// ------------------------------------------------------------------------------------
		public UndoDifferenceAction(DifferenceList differenceList, DifferenceList reviewedList,
			bool fRevert)
		{
			m_differenceList = differenceList;
			m_reviewedList = reviewedList;
			m_differenceRef = m_differenceList.CurrentDifference;
			m_differenceClone = m_differenceList.CurrentDifference.Clone();
			m_index = m_differenceList.CurrentDifferenceIndex;
			m_fReverted = fRevert;
		}

		#region Overrides of UndoActionBase

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns always <c>true</c>
		/// </summary>
		/// <returns>always <c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			m_reviewedList.Remove(m_differenceRef);
			m_differenceList.Insert(m_index, m_differenceClone);
			// move to the difference that was just inserted
			System.Diagnostics.Debug.Assert(m_index < m_differenceList.Count);
			m_differenceList.CurrentDifferenceIndex = m_index;

			// Only adjust following diff details if it was actually reverted and the current diff
			// type is not a para style change. For a para style change, the text of the paragraph
			// doesn't change, so we shouldn't adjust anything (TE-6336).
			if (m_fReverted && (m_differenceClone.DiffType != DifferenceType.ParagraphStyleDifference))
			{
				int offset = (m_differenceClone.IchLimCurr - m_differenceClone.IchMinCurr) -
					(m_differenceClone.IchLimRev - m_differenceClone.IchMinRev);
				m_differenceList.AdjustFollowingDiffsForUndo(m_index, m_differenceClone.HvoCurr, offset);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			if (m_fReverted)
			{
				int offset = (m_differenceClone.IchLimRev - m_differenceClone.IchMinRev) -
					(m_differenceClone.IchLimCurr - m_differenceClone.IchMinCurr);
				m_differenceList.AdjustFollowingDiffsForUndo(m_index, m_differenceClone.HvoCurr, offset);
			}

			m_differenceList.Remove(m_differenceClone);
			m_reviewedList.Add(m_differenceClone);
			m_differenceRef = m_differenceClone;

			// This moves to the difference immediately following the difference that was just
			// removed from the list. This may not be the final design. But, until further
			// customer comments, this will just have to be.
			if (m_index < m_differenceList.Count)
				m_differenceList.CurrentDifferenceIndex = m_index;
			return true;
		}

		#endregion
	}
	#endregion

	#region UndoMajorDifferenceAction class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// UndoAction for the diff dialog where the change is big enough that we have to make a
	/// copy of the entire list so that the undo/redo will work correctly.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class UndoMajorDifferenceAction : UndoActionBase
	{
		/// <summary>The current difference list in the diff dialog</summary>
		private BookMerger m_bookMerger;
		/// <summary>Copy of the original difference list</summary>
		private DifferenceList m_undoDifferenceListClone;
		private DifferenceList m_undoReviewListClone;
		private DifferenceList m_redoDifferenceListClone;
		private DifferenceList m_redoReviewListClone;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="merger"></param>
		/// ------------------------------------------------------------------------------------
		public UndoMajorDifferenceAction(BookMerger merger)
		{
			m_bookMerger = merger;
			m_undoDifferenceListClone = m_bookMerger.Differences.Clone();
			m_undoReviewListClone = m_bookMerger.ReviewedDiffs.Clone();
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns always <c>true</c>
		/// </summary>
		/// <returns>always <c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			// If the bookMerger is disposed, it is probably being called outside the Diff dialog,
			// in which case, the difference lists are no longer needed.
			if (m_bookMerger.IsDisposed)
				return true;
			m_redoDifferenceListClone = m_bookMerger.Differences.Clone();
			m_redoReviewListClone = m_bookMerger.ReviewedDiffs.Clone();
			AdjustCurrentIndex(m_redoDifferenceListClone, m_undoDifferenceListClone);
			UndoRedoAction(m_undoDifferenceListClone, m_undoReviewListClone);
			m_undoDifferenceListClone = null;
			m_undoReviewListClone = null;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust current index of destination list to be same as source list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdjustCurrentIndex(DifferenceList destList, DifferenceList srcList)
		{
			if (srcList.CurrentDifferenceIndex < destList.Count)
				destList.CurrentDifferenceIndex = srcList.CurrentDifferenceIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces contents of difference lists in BookMerger with saved contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UndoRedoAction(DifferenceList savedDiffList, DifferenceList savedReviewList)
		{
			ReplaceListContents(m_bookMerger.Differences, savedDiffList);
			ReplaceListContents(m_bookMerger.ReviewedDiffs, savedReviewList);

			// move to the difference that was just inserted
			System.Diagnostics.Debug.Assert(
				savedDiffList.CurrentDifferenceIndex < savedDiffList.Count);
			m_bookMerger.Differences.CurrentDifferenceIndex = savedDiffList.CurrentDifferenceIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces contents of a difference list with saved contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ReplaceListContents(DifferenceList diffList, DifferenceList savedList)
		{
			diffList.Clear();
			foreach (Difference diff in savedList)
				diffList.Add(diff.Clone());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redo cannot currently be done.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			// If the bookMerger is disposed, it is probably being called outside the Diff dialog,
			// in which case, the difference lists are no longer needed.
			if (m_bookMerger.IsDisposed)
				return true;
			m_undoDifferenceListClone = m_bookMerger.Differences.Clone();
			m_undoReviewListClone = m_bookMerger.ReviewedDiffs.Clone();
			AdjustCurrentIndex(m_undoDifferenceListClone, m_redoDifferenceListClone);
			UndoRedoAction(m_redoDifferenceListClone, m_redoReviewListClone);
			m_redoDifferenceListClone = null;
			m_redoReviewListClone = null;
			return true;
		}

		#endregion
	}
	#endregion
}
