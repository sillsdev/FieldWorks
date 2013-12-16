// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UndoDifferenceAction.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.TE
{
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
		public override bool IsDataChange
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
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
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
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
