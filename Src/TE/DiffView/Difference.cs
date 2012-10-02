// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Difference.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	#region DifferenceList class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class maintains a list of difference objects and also a current index.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DifferenceList : IEnumerable<Difference>, IEnumerable
	{
		private const int kDiffIndexNotFound = -2;

		private int m_iCurrentIndex;
		private readonly List<Difference> m_List;
		private bool m_fListIsSorted = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a list of differences
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DifferenceList()
		{
			m_iCurrentIndex = -1;
			m_List = new List<Difference>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this difference list
		/// </summary>
		/// <returns>The cloned difference list</returns>
		/// ------------------------------------------------------------------------------------
		public DifferenceList Clone()
		{
			DifferenceList diffList = new DifferenceList();
			diffList.m_iCurrentIndex = m_iCurrentIndex;
			foreach (Difference diff in m_List)
				diffList.Add(diff.Clone());
			diffList.m_fListIsSorted = m_fListIsSorted;

			return diffList;
		}

		#region Basic Methods for a list
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if there is a next difference after the current one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsNextAvailable
		{
			get { return m_iCurrentIndex < Count - 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if there is a previous difference before the current one
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPrevAvailable
		{
			get { return m_iCurrentIndex > 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move to the first difference and return it if there is one.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Difference MoveFirst()
		{
			if (Count == 0)
				return null;
			m_iCurrentIndex = 0;
			return m_List[m_iCurrentIndex];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move to the next difference and return it if there is one.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Difference MoveNext()
		{
			if (!IsNextAvailable)
				return null;
			return m_List[++m_iCurrentIndex];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move to the previous difference and return it if there is one.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Difference MovePrev()
		{
			if (!IsPrevAvailable)
				return null;
			return m_List[--m_iCurrentIndex];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current difference or null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Difference CurrentDifference
		{
			get
			{
				if (m_iCurrentIndex < 0 || m_iCurrentIndex >= m_List.Count)
					return null;
				return m_List[m_iCurrentIndex];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the index into the list of the current difference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentDifferenceIndex
		{
			get { return m_iCurrentIndex; }
			set
			{
				if (value < 0)
					value = 0;
				m_iCurrentIndex = Math.Min(value, Count - 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		public void Insert(int index, Difference diff)
		{
			if (index < 0 || index >= m_List.Count)
				m_List.Add(diff);
			else
				m_List.Insert(index, diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="diff"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int IndexOf(Difference diff)
		{
			return m_List.IndexOf(diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a difference to the list.
		/// </summary>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		public void Add(Difference diff)
		{
			m_List.Add(diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a range of differences to the list.
		/// </summary>
		/// <param name="diffListToAdd"></param>
		/// ------------------------------------------------------------------------------------
		public void AddRange(List<Difference> diffListToAdd)
		{
			m_List.AddRange(diffListToAdd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a range of differences to the list.
		/// </summary>
		/// <param name="diffListToAdd"></param>
		/// ------------------------------------------------------------------------------------
		public void AddRange(DifferenceList diffListToAdd)
		{
			foreach (Difference diff in diffListToAdd)
				m_List.Add(diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a difference to the list.
		/// </summary>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		public void Remove(Difference diff)
		{
			m_List.Remove(diff);
			// Make sure the current index is still valid after the remove
			if (m_iCurrentIndex >= Count)
				m_iCurrentIndex = Count - 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clear out the list of differences
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Clear()
		{
			m_List.Clear();
			m_iCurrentIndex = -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the list of differences
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Sort()
		{
			m_List.Sort();
			m_fListIsSorted = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the list of differences, if it's not in order.
		/// Also maintain a reasonable CurrentDifference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SortIfNeeded()
		{
			if (!m_fListIsSorted)
			{
				int index = CurrentDifferenceIndex;
				Difference current = CurrentDifference;

				Sort();

				// Stay at the start if we were at the start. Otherwise...
				if (index != 0)
				{
					// go to the former difference if possible.
					if (current != null)
						CurrentDifferenceIndex = IndexOf(current);
					else
						CurrentDifferenceIndex = 0;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the number of items in the difference list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count
		{
			get { return m_List.Count; }
		}
		#endregion

		#region Adjust/Fix Methods, used by BookMerger.ReplaceCurrentWithRevision
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a change is made to the text of a diff, all of the following diffs in the same
		/// paragraph need to have the offsets adjusted to account for the change in text length.
		/// </summary>
		/// <remarks>This Adjust method is used for diff types that represent a change within a
		/// paragraph. The basic UndoDifferenceAction can undo adjustments made this way.
		/// However, more complex "structural change' diffs need more complex adjustments
		/// (other methods below), and a UndoMajorDifferenceAction.</remarks>
		/// <param name="givenRootDiff">Start with this diff in the list. Must be a root diff.</param>
		/// <param name="offset">character offset amount</param>
		/// ------------------------------------------------------------------------------------
		public void AdjustFollowingOffsets(Difference givenRootDiff, int offset)
		{
			AdjustFollowingOffsets(givenRootDiff, givenRootDiff.ParaCurr, offset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a change is made to the text of a diff, all of the following diffs in the same
		/// paragraph need to have the offsets adjusted to account for the change in text length.
		/// </summary>
		/// <param name="rootDiff">Start with this diff in the list. Must be a root diff.</param>
		/// <param name="paraAdjust">The paragraph where the text was changed,
		/// and thus where following diffs must be adjusted.</param>
		/// <param name="offset">character offset amount</param>
		/// <remarks>This Adjust method is used in specific circumstances for Paragraph Structure
		/// diff types that represent change within a collection of paragraphs.
		/// The paraAdjust is the id for the paragraph in the last subdifference, where the
		/// final text change was made.</remarks>
		/// ------------------------------------------------------------------------------------
		public void AdjustFollowingOffsets(Difference rootDiff, IScrTxtPara paraAdjust, int offset)
		{
			// adjust the offsets of the following diffs that refer to the same paragraph
			int diffIndex = IndexOf(rootDiff);

			AdjustFollowingDiffsDetails(diffIndex, paraAdjust, -1, null, offset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the diffs following the given difference index, for undo.
		/// </summary>
		/// <param name="diffIndex">the index in the difference list beyond which we will
		/// adjust diffs</param>
		/// <param name="paraCurr">the original Current para</param>
		/// <param name="offset">the amount to add to the Current char positions</param>
		/// ------------------------------------------------------------------------------------
		public void AdjustFollowingDiffsForUndo(int diffIndex, IScrTxtPara paraCurr, int offset)
		{
			AdjustFollowingDiffsDetails(diffIndex, paraCurr, -1, null, offset);
		}

		// The following are private methods that do much of the  details

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function - Gets the index of the first diff with the given paragraph hvo
		/// and an ichMin at or beyond the givenIchMin.
		/// This is used in special cases when an hvo and ich are known, but different
		/// from the given diff's hvoCurr and ichMinCurr. Or if the givenDiff is a subDiff
		/// and thus is not a root diff in the master list.
		/// </summary>
		/// <param name="givenPara">The given para.</param>
		/// <param name="givenIchMin">The given ichMin.</param>
		/// <returns>the index of the first matching diff in the list, or kDiffIndexNotFound
		/// if not found</returns>
		/// ------------------------------------------------------------------------------------
		private int GetFirstDiffIndex(IScrTxtPara givenPara, int givenIchMin)
		{
			for (int diffIndex = 0; diffIndex < m_List.Count; diffIndex++)
			{
				Difference diff = m_List[diffIndex];
				if (diff.ParaCurr == givenPara && diff.IchMinCurr >= givenIchMin)
					return diffIndex;
			}
			// given diff was not found
			return kDiffIndexNotFound;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For following root differences in the master list that refer to the given paragraph:
		///  * replace the Current paragraph hvo if requested, and
		///  * add an offset to the "current" char position information.
		/// Optionally, we limit our work to those diffs within a given ichLimit.
		/// </summary>
		/// <remarks>This code depends on the m_fListIsSorted flag being accurate. Any code
		/// that changes the paragraph hvo of a diff should set this flag to false.</remarks>
		/// <param name="diffIndex">the index in the difference list beyond which we will
		/// adjust diffs</param>
		/// <param name="paraCurr">the original Current para</param>
		/// <param name="ichLimit">the character position limit for adjusting diffs,
		///   or -1 if no limit within this paragraph. Used only for "VerseMovedBack" diffs.</param>
		/// <param name="paraNew">new Current para, or null if no change needed</param>
		/// <param name="offset">the amount to add to the Current char positions</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustFollowingDiffsDetails(int diffIndex, IScrTxtPara paraCurr, int ichLimit,
			IScrTxtPara paraNew, int offset)
		{
			int iDiff = diffIndex;
			// Must start AFTER the given diff Index
			while (++iDiff < m_List.Count)
			{
				Difference diff = m_List[iDiff];
				bool fMatchFound = false;

				// does this base diff refer to the same current paragraph?
				if (diff.ParaCurr == paraCurr)
				{
					fMatchFound = true;
					AdjustDiff(diff, ichLimit, paraNew, offset, false);

					Debug.Assert(diff.DiffType != DifferenceType.StanzaBreakAddedToCurrent,
						"We don't want to move anything into a stanza break paragraph");
					//if (diff.DiffType == DifferenceType.ParagraphAddedToCurrent)
					//{
						// we need to change diffType to VerseAdded so that text moved to this paragraph during
						// this revert won't be deleted. TE-7096, TE-7070
						//diff.DiffType = DifferenceType.VerseAddedToCurrent;
					//}
				}

				if (diff.HasParaSubDiffs)
				{
					foreach (Difference subDiff in diff.SubDiffsForParas)
					{
						// does this subdiff refer to the same current paragraph?
						if (subDiff.ParaCurr == paraCurr)
						{
							fMatchFound = true;
							AdjustDiff(subDiff, ichLimit, paraNew, offset, true);
						}
					}
				}

				if (diff.ParaCurr == paraCurr && diff.DiffType == DifferenceType.ParagraphAddedToCurrent)
				{
					// We need to turn this into a more complex difference so the text moved to this paragraph
					// during this revert won't be deleted. TE-7096, TE-7099, TE-7070. This is somewhat of
					// a hack, because we don't normally expect a paragraph split to be a subdiff.
					// See corresponding code in BookMerger.ReplaceCurrentWithRevision_CopyParaStructure
					// that handles this kind of subdiff.
					diff.DiffType = DifferenceType.ParagraphStructureChange;
					diff.SubDiffsForParas = new List<Difference>(2);
					diff.SubDiffsForParas.Add(new Difference(diff.ParaCurr, diff.IchMinCurr,
						diff.IchLimCurr, diff.ParaRev, diff.IchMinRev, diff.IchLimRev,
						DifferenceType.VerseAddedToCurrent, diff.StyleNameCurr,
						diff.StyleNameRev, diff.WsNameCurr, diff.WsNameRev));
					diff.SubDiffsForParas.Add(new Difference(diff.ParaCurr, diff.IchMinCurr,
						diff.IchMinCurr, null, -1, -1, DifferenceType.ParagraphSplitInCurrent,
						null, null, null, null));
				}

				// if there was no match in the root diff or subdiffsforparas...
				if (!fMatchFound)
				{
					// and if the list is sorted, we are beyond the matching diffs and we can quit.
					if (m_fListIsSorted)
						break;
				}
			}

			// as one last thing for TE-7096, TE-7070 we must also fix the PREVIOUS diff type in
			// some situations
			if (diffIndex > 0)
			{
				Difference prevDiff = m_List[diffIndex - 1];
				if (prevDiff.ParaCurr == paraCurr &&
					(prevDiff.DiffType == DifferenceType.ParagraphAddedToCurrent))
				{
					// we need to change diffType to VerseAdded so that text moved to this
					// paragraph during this revert won't be deleted.
					prevDiff.DiffType = DifferenceType.VerseAddedToCurrent;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the given Difference, if it is within an optional given ichLimit:
		///  * replace the Current paragraph hvo if requested, and
		///  * add an offset to the "current" char position information.
		/// </summary>
		/// <param name="diff">The given Difference</param>
		/// <param name="ichLimit">the character position limit for adjusting diffs,
		///   or -1 if no limit within this paragraph</param>
		/// <param name="paraNew">The new para, or null if no change</param>
		/// <param name="offset">The offset to add to the ich's.</param>
		/// <param name="fIsSubdiff">True if the difference is a subdiff, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		private static void AdjustDiff(Difference diff, int ichLimit, IScrTxtPara paraNew,
			int offset, bool fIsSubdiff)
		{
			// if the ichLimit criteria is specified and we are within it...
			if (ichLimit == -1 || diff.IchMinCurr < ichLimit)
			{
				// Fix the paragraph if requested and adjust the character indexes
				// Only adjust the limit (not the min) if this is a paragraph style
				// difference because for paragraph style differences, the min and lim
				// should span the entire paragraph contents, so the min should already be 0.
				// Because the subdiffs can hold information in addition to being a subdiff
				// (e.g. in a paragraph split), we need to always adjust the offsets of subdiffs
				bool fAdjustMin = (diff.DiffType != DifferenceType.ParagraphStyleDifference || fIsSubdiff);
				diff.CurrLocation = new DiffLocation(paraNew ?? diff.ParaCurr,
					fAdjustMin ? Math.Max(diff.IchMinCurr + offset, 0) : diff.IchMinCurr,
					Math.Max(diff.IchLimCurr + offset, 0));
			}
		}

		//Note: when following Fix methods are used, an UndoMajorDifferenceAction is also required.
		// (see also: DiffDialog.IsMajorDifference(); maybe that method should go here in DifferenceList)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a paragraph is split or merged, all of the following diffs for the same
		/// original paragraph need to have the new para hvo, and have their ich's adjusted too.
		/// </summary>
		/// <remarks>A diff that has section info, not para info, for the Current book -
		/// will have to supply the paraCurr. Other diffs have the paraCurr info in the
		/// givenDiff, but also need the new hvo substituted in the applicable diffs.</remarks>
		/// <param name="givenDiff">the Difference for the paragraph split that has been done</param>
		/// <param name="paraCurr">the original Current para</param>
		/// <param name="paraNew">new Current para, or <c>null</c> if no change</param>
		/// <param name="offset">character offset amount</param>
		/// ------------------------------------------------------------------------------------
		public void FixFollowingParaDiffs(Difference givenDiff, IScrTxtPara paraCurr,
			IScrTxtPara paraNew, int offset)
		{
			int diffIndex = IndexOf(givenDiff);

			// adjust the the following diffs that refer to the given paragraph
			AdjustFollowingDiffsDetails(diffIndex, paraCurr, -1, paraNew, offset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse is moved back, we need to fix the other diffs that refer to the verse
		/// moved, as well as the diffs following where the verse was and where it was inserted.
		/// </summary>
		/// <remarks>Call this method after the verse has been inserted in its new place and
		/// deleted from it's former place.</remarks>
		/// <param name="verseMovedDiff">The given VerseMoved diff.</param>
		/// ------------------------------------------------------------------------------------
		public void FixDiffsForVerseMovedBack(Difference verseMovedDiff)
		{
			int verseLen = verseMovedDiff.IchLimCurr - verseMovedDiff.IchMinCurr;
			Debug.Assert(verseMovedDiff.DiffType == DifferenceType.VerseMoved);
			Debug.Assert(verseMovedDiff.ParaMovedFrom != null);
			Debug.Assert(verseMovedDiff.ParaCurr != null);

			// Background: Reverting a "VerseMoved" diff involves:
			//  * deleting the verse in its original pargraph at the diff's "Current" range
			//  * inserting the verse back at the diff's "MovedFrom" paragraph and position

			// All of the diffs at or following the 'MovedFrom' insertion position for the same paragraph
			//  need to have their ich's increased by the length of the insertion.
			int diffIndex = GetFirstDiffIndex(verseMovedDiff.ParaMovedFrom, verseMovedDiff.IchMovedFrom);
			if (diffIndex != kDiffIndexNotFound)
			{
				// adjust the diffs that refer to the paragraph inserted into
				// (note we decrement the diffIndex; we want to adjust the first matching diff)
				AdjustFollowingDiffsDetails(diffIndex - 1, verseMovedDiff.ParaMovedFrom, -1, null, verseLen);
			}

			// All the diffs for the verse moved
			//  need to have the new paragraph hvo, and ich's adjusted
			diffIndex = GetFirstDiffIndex(verseMovedDiff.ParaCurr, verseMovedDiff.IchMinCurr);
			if (diffIndex != kDiffIndexNotFound)
			{
				// fix the diffs that refer to the moved verse
				// (note we decrement the diffIndex; we want to adjust the first matching diff)
				int adjust = verseMovedDiff.IchMovedFrom - verseMovedDiff.IchMinCurr;
				AdjustFollowingDiffsDetails(diffIndex - 1, verseMovedDiff.ParaCurr, verseMovedDiff.IchLimCurr,
					verseMovedDiff.ParaMovedFrom, adjust);
				// if we moved the verse to a different paragraph, the list of diffs may now be
				// out of order.
				if (verseMovedDiff.ParaCurr != verseMovedDiff.ParaMovedFrom)
					m_fListIsSorted = false;
			}

			// All of the diffs following the 'Current' deletion position for the same paragraph
			//  need to have their ich's decreased by the length of the deletion.
			diffIndex = GetFirstDiffIndex(verseMovedDiff.ParaCurr, verseMovedDiff.IchMinCurr);
			if (diffIndex != kDiffIndexNotFound)
			{
				// adjust the diffs that refer to the paragraph where the verse was deleted
				// (note we decrement the diffIndex; we want to adjust the first matching diff)
				AdjustFollowingDiffsDetails(diffIndex - 1, verseMovedDiff.ParaCurr, -1, null, -verseLen);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fix the Current paragraph hvo (and ichMin, ichLim) for all differences that refer
		/// to a given Current paragraph.
		/// This is needed when the given paragraph is about to be deleted by the Diff tool.
		/// But be careful - this method may well modify the diff object that provided the
		/// given hvoCurr that the user has decided to delete.
		/// </summary>
		/// <param name="givenParaCurr">the paragraph that will become obsolete.</param>
		/// <param name="paraNew">the new paragraph.</param>
		/// <param name="ichMinNew">new character offset for the beginning of the difference
		/// in the new paragraph.</param>
		/// <param name="ichLimNew">new character offset for the end of the difference in the
		/// new paragraph</param>
		/// <remarks>This method should not modify m_iCurrentIndex, because it is utilized by the
		/// Diff tool UI.</remarks>
		/// ------------------------------------------------------------------------------------
		public void FixCurrParaHvosAndSetIch(IScrTxtPara givenParaCurr, IScrTxtPara paraNew,
			int ichMinNew, int ichLimNew)
		{
			// Go through the whole Difference list, fixing any references to the given Current para
			foreach (Difference diff in m_List)
			{
				if (diff.ParaCurr == givenParaCurr)
					diff.CurrLocation = new DiffLocation(paraNew, ichMinNew, ichLimNew);

				// if this difference has subdifferences representing paragraphs...
				if (diff.HasParaSubDiffs)
				{
					// we need to scan through them and update diffs for any paragraphs that also
					// refer to the paragraph that is about to be deleted.
					foreach (Difference subDiff in diff.SubDiffsForParas)
					{
						if (subDiff.ParaCurr == givenParaCurr)
							subDiff.CurrLocation = new DiffLocation(paraNew, ichMinNew, ichLimNew);
					}
				}
			}
		}

//        /// ------------------------------------------------------------------------------------
//        /// <summary>
//        /// Fix the Current paragraph hvo (and ichMin, ichLim) for all differences that refer
//        /// to a given Current paragraph.
//        /// This is needed when the given paragraph is about to be deleted by the Diff tool.
//        /// But be careful - this method may well modify the diff object that provided the
//        /// given hvoCurr that the user has decided to delete.
//        /// </summary>
//        /// <param name="givenParaCurrHvo">the paragraph hvo that will become obsolete.</param>
//        /// <param name="hvoNew">the new paragraph hvo.</param>
//        /// <param name="ichStart">The start character offset where we will begin updating
//        /// paragraph hvos.</param>
//        /// <param name="ichMinNew">new character offset for the beginning of the difference
//        /// in the new paragraph, or -1 to retain the original value.</param>
//        /// <param name="ichLimNew">new character offset for the end of the difference in the
//        /// new paragraph, or -1 to retain the original value</param>
//        /// <param name="fUpdateCurrentDiff">if <c>true</c> update the current difference;
//        /// otherwise update all differences except the current</param>
//        /// <remarks>This method should not modify m_iCurrentIndex, because it is utilized by the
//        /// Diff tool UI.</remarks>
//        /// ------------------------------------------------------------------------------------
//// probably use FixFollowingParaDiffs()
//        public void FixCurrParaHvos(int givenParaCurrHvo, int hvoNew, int ichStart, int ichMinNew,
//            int ichLimNew, bool fUpdateCurrentDiff)
//        {
//            // Go through the Difference list, fixing any references to the given Current para
//            foreach (Difference diff in m_List)
//            {
//                if (diff == CurrentDifference && !fUpdateCurrentDiff)
//                    continue; // we don't want to update our current difference
//                if (diff.HvoCurr == givenParaCurrHvo && diff.IchMinCurr >= ichStart)
//                {
//                    diff.HvoCurr = hvoNew;
//                    if (diff.HasParaSubDiffs)
//                    {
//                        foreach (Difference subDiff in diff.SubDiffsForParas)
//                        {
//                            if (subDiff.HvoCurr == givenParaCurrHvo && subDiff.IchMinCurr >= ichStart)
//                                subDiff.HvoCurr = hvoNew;
//                        }
//                    }
//                    if (ichMinNew >= 0)
//                        diff.IchMinCurr = ichMinNew;
//                    if (ichLimNew >= 0)
//                        diff.IchLimCurr = ichLimNew;
//                }
//            }
//        }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For adjacent ParagraphMissingInCurrent differences that refer to the given diff's
		/// Curr paragraph:  fix the Current paragraph hvo, ichMin, and ichLim.
		/// This is needed when a paragraph has just been inserted by the Diff tool
		/// (in non-Scripture for now, maybe maybe Scripture later),
		/// so that adjacent inserted paragraphs will maintain the same order.
		/// </summary>
		/// <remarks>This method should not modify m_iCurrentIndex, because it is utilized by the
		/// Diff tool UI.</remarks>
		/// <param name="givenDiff">the ParaMissingInCurr diff by which a paragraph
		/// has just been inserted in the Current</param>
		/// <param name="insertAfter">true to look at diffs after the current diff, false to
		/// look at diffs before the current diff</param>
		/// <param name="newPara"></param>
		/// <param name="ichMinNew"></param>
		/// <param name="ichLimNew"></param>
		/// ------------------------------------------------------------------------------------
		public void FixMissingCurrParaDestIP(Difference givenDiff, bool insertAfter, IScrTxtPara newPara,
			int ichMinNew, int ichLimNew)
		{
			// get the index of the given diff
			int diffIndex = IndexOf(givenDiff);

			// determine the range of diffs we need to process
			int indexMin;
			int indexLim;
			if (insertAfter)
			{
				indexMin = diffIndex + 1;
				indexLim = m_List.Count;
			}
			else
			{
				indexMin = 0;
				indexLim = diffIndex;
			}

			// Go through the selected portion of the Difference list, fixing any references
			// to the given Current para for any of the MissingInCurrent diffs.
			for (int i = indexMin; i < indexLim; i++)
			{
				Difference diff = m_List[i];
				if ((diff.DiffType == DifferenceType.ParagraphMissingInCurrent ||
					diff.DiffType == DifferenceType.SectionHeadMissingInCurrent ||
					diff.DiffType == DifferenceType.ParagraphStructureChange ||
					diff.DiffType == DifferenceType.SectionMissingInCurrent) &&
					diff.ParaCurr == givenDiff.ParaCurr)
				{
					diff.CurrLocation = new DiffLocation(newPara, ichMinNew, ichLimNew);
					if (diff.DiffType == DifferenceType.ParagraphStructureChange)
					{
						foreach (Difference subDiff in diff.SubDiffsForParas)
						{
							if (subDiff.ParaCurr == givenDiff.ParaCurr)
								subDiff.CurrLocation = new DiffLocation(newPara, ichMinNew, ichLimNew);
						}
					}
				}
			}
		}
		#endregion

		#region IEnumerable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the enumerator for enumerating through the list of differences
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerator<Difference> GetEnumerator()
		{
			return m_List.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion
	}
	#endregion

	#region DifferenceType enum
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Each DifferenceType is a bit field used to classify the type of difference that was
	/// found in comparing a specific portion of the Current and Revision books.
	/// The DiffernceType bits are used in Difference.diffType .
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[Flags]
	public enum DifferenceType : int
	{
		/// <summary>Identical verses</summary>
		NoDifference = 0,

		// The following types are set by BookMerger.CompareVerseText() .
		// Diffs of these types may have several bits may set simultaneously.
		// They may have sub-diffs to identify objects within the difference range, such
		// as footnotes or pictures.
		/// <summary>Text of verse in current and revision differ
		/// -or- a verse bridge is different (in which case the whole accumulation of verses
		/// is marked as a difference, and specific text and char style diffs are not marked)</summary>
		TextDifference = 1 << 0, //i.e. 1
		/// <summary>Style of text in verse in current and revision differ</summary>
		CharStyleDifference = 1 << 1,
		/// <summary>Multiple runs in current and revision differ by character style</summary>
		MultipleCharStyleDifferences = 1 << 2,
		/// <summary>A Footnote difference</summary>
		FootnoteDifference = 1 << 3, // i.e. 8
		/// <summary>A footnote is missing in current book</summary>
		FootnoteMissingInCurrent = 1 << 4,
		/// <summary>A footnote is added to current book (i.e., not in the revision)</summary>
		FootnoteAddedToCurrent = 1 << 5,
		/// <summary>A Picture difference</summary>
		PictureDifference = 1 << 6, // i.e. 64
		/// <summary>A picture is missing in current book</summary>
		PictureMissingInCurrent = 1 << 7,
		/// <summary>A picture is added to current book (i.e., not in the revision)</summary>
		PictureAddedToCurrent = 1 << 8,

		// These types are used when entire verses are found to exist in only one book.
		// Diffs of these (and following) types use the single DifferenceType bit exclusively.
		// They have no sub-diff objects; but in special cases a Moved/Added/Missing subDiff is
		//  part of a larger complex root diff.
		/// <summary>Verse not found in current book</summary>
		VerseMissingInCurrent = 1 << 9, // i.e. 512
		/// <summary>Verse added to current book (i.e., not in the revision)</summary>
		VerseAddedToCurrent = 1 << 10, // i.e. 1024
		/// <summary>Verse moved within or between paragraphs or sections.</summary>
		VerseMoved = 1 << 11, // i.e. 2048

		// The following types are used when paragraphs or larger amounts of text are restructured.
		/// <summary>Style of paragraph in current and revision differ</summary>
		ParagraphStyleDifference = 1 << 12, // i.e. 4096
		/// <summary>An entire paragraph is missing in current book</summary>
		ParagraphMissingInCurrent = 1 << 13, //i.e. 8192
		/// <summary>An entire paragraph is added to current book (i.e., not in the revision)</summary>
		ParagraphAddedToCurrent = 1 << 14, //i.e. 16384
		/// <summary>A stanza break is missing in current book</summary>
		StanzaBreakMissingInCurrent = 1 << 15, //i.e. 32768
		/// <summary>A stanza break is added to current book</summary>
		StanzaBreakAddedToCurrent = 1 << 16, //i.e. 65536
		///// <summary>An empty paragraph is missing in current book</summary>
		//EmptyParagraphMissingInCurrent = 1 << 15, //i.e. 32768
		///// <summary>An empty paragraph is added to current book</summary>
		//EmptyParagraphAddedToCurrent = 1 << 16, //i.e. 65536
		/// <summary>A paragraph is merged in the current book that was split in the revision</summary>
		ParagraphMergedInCurrent = 1 << 17, //i.e. 131072
		/// <summary>A paragraph is split in the current that was together in the revision</summary>
		ParagraphSplitInCurrent = 1 << 18,  // i.e. 262144
		/// <summary>Paragraphs in current and revision have multiple splits and/or merges</summary>
		ParagraphStructureChange = 1 << 19, // i.e. 524288
		// <summary>An entire paragraph moved within or between sections.</summary>
		// ParagraphMoved = 1 << 18,  // i.e. 262144

		/// <summary>A section head in the revision is missing in the current (sections were split/merged)</summary>
		SectionHeadMissingInCurrent = 1 << 20, // i.e. 1048576
		/// <summary>A section head has been added to the current book (sections were split/merged)</summary>
		SectionHeadAddedToCurrent = 1 << 21, //i.e. 2097152
		/// <summary>One or more entire sections are missing in the current book</summary>
		SectionMissingInCurrent = 1 << 22, // i.e. 4194304
		/// <summary>One or more entire sections have been added to the current book</summary>
		SectionAddedToCurrent = 1 << 23, //i.e. 8388608
		/// <summary>Writing system of text in in current and revision differ</summary>
		WritingSystemDifference = 1 << 24, //i.e. 16777216
		/// <summary>Multiple runs in current and revision differ by writing system</summary>
		MultipleWritingSystemDifferences = 1 << 25, //i.e. 33554432

		// ATTENTION: when you add new DifferenceType's, evaluate what's needed to Undo a revert
		//  of that diffType, and update DiffDialog.IsMajorDifference()
		// Also, consider whether the new type needs to be added to the list in
		//  DiffDialog.IsDataLossDifference()
	}
	#endregion

	#region Difference class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Class to encapsulate the information about a single difference within a paragraph
	/// between two revisions.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class Difference : IComparable
	{
		#region Member variables

		/// <summary>
		/// static variable 's_sortByCurrent' describes whether diffs should be ordered
		/// according to their location in the Current book (which is the default),
		/// or the Revision book
		/// </summary>
		public static bool s_sortByCurrent = true;

		/// <summary>The starting reference for this difference</summary>
		private BCVRef m_refStart;
		/// <summary>The ending reference for this difference</summary>
		private BCVRef m_refEnd;
		/// <summary>The type for this difference</summary>
		private DifferenceType m_diffType;

		/// <summary>
		/// For difference types that deal with one or more sections in a book, these arrays
		/// hold those sections.
		/// (Paragraph hvo and ich stuff are ignored for a book when the sections are here.)
		/// </summary>
		private List<IScrSection> m_SectionsCurr;
		private List<IScrSection> m_SectionsRev;

		/// <summary>The place in the current paragraph to which this difference applies.</summary>
		private DiffLocation m_CurrLoc;
		/// <summary>The place in the revision paragraph to which this difference applies.</summary>
		private DiffLocation m_RevLoc;
		/// <summary>For VerseMoved differences, this records where the verse was moved from in the
		/// same book. This is always a single point, never a range of text.</summary>
		private DiffLocation m_MovedFrom;

		/// <summary>The name of the style in the current paragraph. For missing/added paras this would
		/// be the paragraph style. For text differences, this would be the character style.</summary>
		private string m_styleNameCurr = null;
		/// <summary>The name of the style in the revision paragraph. For missing/added paras this would
		/// be the paragraph style. For text differences, this would be the character style.</summary>
		private string m_styleNameRev = null;
		/// <summary>The name of the writing system in the current paragraph, if this difference
		/// represents a single writing system difference</summary>
		private string m_WsNameCurr;
		/// <summary>The name of the writing system in the revision paragraph, if this difference
		/// represents a single writing system difference</summary>
		private string m_WsNameRev;
		/// <summary> The ParaNodeMap for the Revision paragraph. </summary>
		private ParaNodeMap m_paraNodeMapRev;
		/// <summary> The ParaNodeMap for the Current paragraph. </summary>
		private ParaNodeMap m_paraNodeMapCurr;

		/// <summary>A sequence of Differences that are part of this root difference
		/// that describe differences for embedded objects (i.e. footnotes, pictures).</summary>
		/// <seealso cref="SubDiffsForORCs"/>
		private List<Difference> m_subDiffsORCs;

		/// <summary>A sequence of Differences that are part of this root difference
		/// that describe differences that extend over more than one paragraph.
		/// NOTE: These sub differences may also describe when verses are moved.</summary>
		/// <seealso cref="SubDiffsForParas"/>
		private List<Difference> m_subDiffsParas;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct an instance of the <see cref="Difference"/> class that has everything
		/// needed for comparison of two ranges of text.
		/// </summary>
		/// <param name="start">Start reference</param>
		/// <param name="end">End reference</param>
		/// <param name="paraCurr">The paragraph with the current difference.</param>
		/// <param name="ichMinCurr">The starting character index of the current difference.</param>
		/// <param name="ichLimCurr">The character limit of the current difference.</param>
		/// <param name="paraRev">The paragraph with the revision difference.</param>
		/// <param name="ichMinRev">The starting character index of the revision difference.</param>
		/// <param name="ichLimRev">The character limit of the revision difference.</param>
		/// <param name="diffType">type of Difference</param>
		/// <param name="subDiffsForORCs">The list of differences that are part of this root diff
		/// and describe differences in ORCs, or the objects to which the ORCS refer.</param>
		/// <param name="subDiffsForParas">The list of differences that are part of this root diff
		/// and describe differences in paragraphs in a paragraph merge, split or structure
		/// difference.</param>
		/// <param name="sCharStyleNameCurr">The character style name in the current version.</param>
		/// <param name="sCharStyleNameRev">The character style name in the saved or imported
		/// version.</param>
		/// <param name="sWsNameCurr">The writing system name in the current version.</param>
		/// <param name="sWsNameRev">The writing system name in the saved or imported version.</param>
		/// <param name="mapCurr">the paragraph node map for the Current</param>
		/// <param name="mapRev">the paragraph node map for the Revision</param>
		/// ------------------------------------------------------------------------------------
		public Difference(BCVRef start, BCVRef end,
			IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev,
			DifferenceType diffType,
			List<Difference> subDiffsForORCs, List<Difference> subDiffsForParas,
			string sCharStyleNameCurr, string sCharStyleNameRev,
			string sWsNameCurr, string sWsNameRev, ParaNodeMap mapCurr, ParaNodeMap mapRev)
		{
			m_refStart = new BCVRef(start);
			m_refEnd = new BCVRef(end);
			m_diffType = diffType;
			if (paraCurr != null)
				m_CurrLoc = new DiffLocation(paraCurr, ichMinCurr, ichLimCurr);
			if (paraRev != null)
				m_RevLoc = new DiffLocation(paraRev, ichMinRev, ichLimRev);
			m_subDiffsORCs = subDiffsForORCs;
			m_subDiffsParas = subDiffsForParas;
			m_styleNameCurr = sCharStyleNameCurr;
			m_styleNameRev = sCharStyleNameRev;
			m_WsNameCurr = sWsNameCurr;
			m_WsNameRev = sWsNameRev;
			m_paraNodeMapCurr = mapCurr;
			m_paraNodeMapRev = mapRev;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct an instance of the <see cref="Difference"/> class used for creating a
		/// sub-difference (which can't itself have sub-differences).
		/// Note also, that this difference, because it is a sub-difference, does not need
		/// ParaNodeMap maps.
		/// </summary>
		/// <param name="paraCurr">The paragraph with the current difference.</param>
		/// <param name="ichMinCurr">The starting character index of the current difference.</param>
		/// <param name="ichLimCurr">The character limit of the current difference.</param>
		/// <param name="paraRev">The paragraph with the revision difference.</param>
		/// <param name="ichMinRev">The starting character index of the revision difference.</param>
		/// <param name="ichLimRev">The character limit of the revision difference.</param>
		/// <param name="diffType">Type of the difference.</param>
		/// <param name="sCharStyleNameCurr">The character style name for the current difference.</param>
		/// <param name="sCharStyleNameRev">The character style name for the revision difference.</param>
		/// <param name="sWsNameCurr">The writing system name in the current version.</param>
		/// <param name="sWsNameRev">The writing system name in the saved or imported version.</param>
		/// ------------------------------------------------------------------------------------
		public Difference(IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev, DifferenceType diffType,
			string sCharStyleNameCurr, string sCharStyleNameRev,
			string sWsNameCurr, string sWsNameRev)
			: this(new BCVRef(), new BCVRef(),  paraCurr, ichMinCurr, ichLimCurr, paraRev,
			ichMinRev, ichLimRev, diffType, null, null, sCharStyleNameCurr, sCharStyleNameRev,
			sWsNameCurr, sWsNameRev, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct an instance of the <see cref="Difference"/> class used for creating a
		/// one-sided sub-difference (which can't itself have sub-differences or character
		/// style differences or text differences). Note also, that this difference,
		/// because it is a sub-difference, does not need ParaNodeMap maps.
		/// </summary>
		/// <param name="paraCurr">The paragraph with the current difference.</param>
		/// <param name="ichLimCurr">The character limit of the current difference.</param>
		/// <param name="paraRev">The paragraph with the revision difference.</param>
		/// <param name="ichLimRev">The character limit of the revision difference.</param>
		/// ------------------------------------------------------------------------------------
		public Difference(IScrTxtPara paraCurr, int ichLimCurr, IScrTxtPara paraRev, int ichLimRev)
			: this(new BCVRef(), new BCVRef(), paraCurr, 0, ichLimCurr, paraRev, 0, ichLimRev,
			DifferenceType.NoDifference, null, null, null, null, null, null, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct an instance of the <see cref="Difference"/> class with no char style
		/// or sub-diff info.
		/// </summary>
		/// <param name="start">Start reference</param>
		/// <param name="end">End reference</param>
		/// <param name="diffType">Type of the difference.</param>
		/// <param name="paraCurr">the current difference.</param>
		/// <param name="ichMinCurr">The starting character index of the current difference.</param>
		/// <param name="ichLimCurr">The character limit of the current difference.</param>
		/// <param name="paraRev">the revision difference.</param>
		/// <param name="ichMinRev">The starting character index of the revision difference.</param>
		/// <param name="ichLimRev">The character limit of the revision difference.</param>
		/// <param name="mapCurr">The map curr.</param>
		/// <param name="mapRev">The map rev.</param>
		/// ------------------------------------------------------------------------------------
		public Difference(BCVRef start, BCVRef end, DifferenceType diffType,
			IScrTxtPara paraCurr, int ichMinCurr, int ichLimCurr,
			IScrTxtPara paraRev, int ichMinRev, int ichLimRev,
			ParaNodeMap mapCurr, ParaNodeMap mapRev)
			: this(start, end,
			paraCurr, ichMinCurr, ichLimCurr,
			paraRev, ichMinRev, ichLimRev,
			diffType, null, null, null, null, null, null,
			mapCurr, mapRev)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Difference"/> class.
		/// This is valid only for Missing/Added Section/SectionHead/Para diff types.
		/// </summary>
		/// <param name="start">The verse ref start.</param>
		/// <param name="end">The verse ref end.</param>
		/// <param name="type">Type of the diff.</param>
		/// <param name="sectionsAdded">the sections(s) that were added-
		/// or the section owning the section head</param>
		/// <param name="paraDest">The destination paragraph</param>
		/// <param name="ichDest">The character index in the destination paragraph,
		/// where the added items could be inserted in the other book.</param>
		/// ------------------------------------------------------------------------------------
		public Difference(BCVRef start, BCVRef end, DifferenceType type,
			IEnumerable<IScrSection> sectionsAdded, IScrTxtPara paraDest, int ichDest)
		{
			m_refStart = new BCVRef(start);
			m_refEnd = new BCVRef(end);
			m_diffType = type;

			List<IScrSection> sectionList = null;
			// Set the info for the 'Added' side of the diff
			if (DiffType == DifferenceType.SectionAddedToCurrent || DiffType == DifferenceType.SectionHeadAddedToCurrent)
				sectionList = m_SectionsCurr = new List<IScrSection>(sectionsAdded);
			else if (DiffType == DifferenceType.SectionMissingInCurrent || DiffType == DifferenceType.SectionHeadMissingInCurrent)
				sectionList = m_SectionsRev = new List<IScrSection>(sectionsAdded);
			if (sectionList != null)
			{
				IScrSection firstSection = sectionList[0];
				IScrTxtPara firstHeadingPara = (IScrTxtPara)firstSection.HeadingOA[0];
				if (DiffType == DifferenceType.SectionAddedToCurrent ||
					DiffType == DifferenceType.SectionHeadAddedToCurrent)
					m_paraNodeMapCurr = new ParaNodeMap(firstHeadingPara);
				else
					m_paraNodeMapRev = new ParaNodeMap(firstHeadingPara);
			}
			//elseif (paragraph stuff to be added...)
			else
				throw new Exception("Difference ctor - diffType must be Missing/Added Section/SectionHead/Para");

			// Set the info for the 'Destination' side of the diff (where the 'Added' items
			// would go if inserted in the other book).
			SetDestinationIP(paraDest, ichDest);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Construct an instance of the <see cref="Difference"/> class, using two ScrVerse
		/// objects.
		/// </summary>
		/// <param name="verseCurr">The ScrVerse containing information about the verse
		/// in the "current" version</param>
		/// <param name="verseRev">The ScrVerse containing information about the verse
		/// in the (previous) revision</param>
		/// <param name="diffType"></param>
		/// --------------------------------------------------------------------------------
		public Difference(ScrVerse verseCurr, ScrVerse verseRev, DifferenceType diffType)
		{
			// Set the difference type
			m_diffType = diffType;

			// Set the start and end reference
			if ((DiffType &
				(DifferenceType.VerseMissingInCurrent | DifferenceType.ParagraphMissingInCurrent |
				DifferenceType.StanzaBreakMissingInCurrent)) != 0)
			{
				m_refStart = verseRev.StartRef;
				m_refEnd = verseRev.EndRef;
			}
			else if ((DiffType &
				(DifferenceType.VerseAddedToCurrent | DifferenceType.ParagraphAddedToCurrent |
				DifferenceType.StanzaBreakAddedToCurrent)) != 0)
			{
				m_refStart = verseCurr.StartRef;
				m_refEnd = verseCurr.EndRef;
			}
			else
			{
				m_refStart = Math.Min(verseCurr.StartRef, verseRev.StartRef);
				m_refEnd = Math.Max(verseCurr.EndRef, verseRev.EndRef);
			}

			// Set the Current paragraph stuff
			if (verseCurr != null)
			{
				m_CurrLoc = new DiffLocation(verseCurr);
				m_paraNodeMapCurr = verseCurr.ParaNodeMap;
			}

			// Set the Revision paragraph stuff
			if (verseRev != null)
			{
				m_RevLoc = new DiffLocation(verseRev);
				m_paraNodeMapRev = verseRev.ParaNodeMap;
			}
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixes the para structure root info by getting the character offset from the first
		/// subdiff. The root diff does not have this information when it initially creates
		/// a para merge/split or para structure change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FixParaStructureRootInfo()
		{
			Debug.Assert(HasParaSubDiffs);
			Difference firstSubdiff = SubDiffsForParas[0];

			// we'll have the root info point to the beginning of the first subdifference
			CurrLocation = new DiffLocation(firstSubdiff.ParaCurr, firstSubdiff.IchMinCurr, firstSubdiff.IchMinCurr);
			RevLocation = new DiffLocation(firstSubdiff.ParaRev, firstSubdiff.IchMinRev, firstSubdiff.IchMinRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the Current is the source.
		/// </summary>
		/// <param name="diffType">Type of the difference--should be a missing or added diff.</param>
		/// <returns><c>true</c> if the difference was added to current; <c>false</c> if the
		/// difference is missing in current</returns>
		/// <exception cref="InvalidOperationException">thrown if diffType is not added to current
		/// or missing in current.</exception>
		/// ------------------------------------------------------------------------------------
		public static bool CurrentIsSource(DifferenceType diffType)
		{
			if (diffType == DifferenceType.FootnoteAddedToCurrent ||
				diffType == DifferenceType.ParagraphAddedToCurrent ||
				diffType == DifferenceType.StanzaBreakAddedToCurrent ||
				diffType == DifferenceType.PictureAddedToCurrent ||
				diffType == DifferenceType.SectionAddedToCurrent ||
				diffType == DifferenceType.SectionHeadAddedToCurrent ||
				diffType == DifferenceType.VerseAddedToCurrent)
			{
				return true;
			}

			if (diffType == DifferenceType.FootnoteMissingInCurrent ||
				diffType == DifferenceType.ParagraphMissingInCurrent ||
				diffType == DifferenceType.StanzaBreakMissingInCurrent ||
				diffType == DifferenceType.PictureMissingInCurrent ||
				diffType == DifferenceType.SectionHeadMissingInCurrent ||
				diffType == DifferenceType.SectionMissingInCurrent ||
				diffType == DifferenceType.VerseMissingInCurrent)
			{
				return false;
			}

			throw new InvalidOperationException(string.Format(
				"Difference type {0} incompatible. Difference type must be added to or missing in current.",
				diffType.ToString()));
		}
		#endregion

		#region Clone, Equals, Accumulate, etc
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a copy of this difference
		/// </summary>
		/// <returns>The copy</returns>
		/// ------------------------------------------------------------------------------------
		public Difference Clone()
		{
			List<Difference> clonedSubDiffsForORCs = null;
			if (HasORCSubDiffs)
			{
				clonedSubDiffsForORCs = new List<Difference>();
				foreach (Difference subdiff in SubDiffsForORCs)
					clonedSubDiffsForORCs.Add(subdiff.Clone());
			}

			List<Difference> clonedSubDiffsForParas = null;
			if (HasParaSubDiffs)
			{
				clonedSubDiffsForParas = new List<Difference>();
				foreach (Difference subdiff in SubDiffsForParas)
					clonedSubDiffsForParas.Add(subdiff.Clone());
			}

			ParaNodeMap mapCurr = null;
			ParaNodeMap mapRev = null;
			if (ParaNodeMapCurr != null)
				mapCurr = ParaNodeMapCurr.Clone();
			if (ParaNodeMapRev != null)
				mapRev = ParaNodeMapRev.Clone();

			Difference clonedDiff = new Difference(RefStart, RefEnd,
				ParaCurr, IchMinCurr, IchLimCurr,
				ParaRev, IchMinRev, IchLimRev,
				m_diffType, clonedSubDiffsForORCs, clonedSubDiffsForParas,
				StyleNameCurr, StyleNameRev, WsNameCurr, WsNameRev,
				mapCurr, mapRev);

			// MovedFrom info is not set in normal constructors, so set them now
			clonedDiff.m_MovedFrom = new DiffLocation(ParaMovedFrom, IchMovedFrom);

			if (SectionsCurr != null)
				clonedDiff.m_SectionsCurr = new List<IScrSection>(SectionsCurr);
			if (SectionsRev != null)
				clonedDiff.m_SectionsRev = new List<IScrSection>(SectionsRev);


			return clonedDiff;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override equals method to make an explicit implementation.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(Object other)
		{
			Difference otherDiff = other as Difference;
			return otherDiff != null &&
				RefStart.Equals(otherDiff.RefStart) &&
				RefEnd.Equals(otherDiff.RefEnd) &&
				ParaCurr == otherDiff.ParaCurr &&
				IchMinCurr == otherDiff.IchMinCurr &&
				IchLimCurr == otherDiff.IchLimCurr &&
				ParaRev == otherDiff.ParaRev &&
				IchMinRev == otherDiff.IchMinRev &&
				IchLimRev == otherDiff.IchLimRev &&
				ParaMovedFrom == otherDiff.ParaMovedFrom &&
				IchMovedFrom == otherDiff.IchMovedFrom &&
				m_diffType == otherDiff.m_diffType &&
				ArrayUtils.AreEqual(m_SectionsCurr, otherDiff.m_SectionsCurr) &&
				ArrayUtils.AreEqual(m_SectionsRev, otherDiff.m_SectionsRev) &&
				CompareParaNodeMaps(ParaNodeMapCurr, otherDiff.ParaNodeMapCurr) &&
				CompareParaNodeMaps(ParaNodeMapRev, otherDiff.ParaNodeMapRev);
				//Array.Equals(subDiffs, otherDiff.subDiffs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two ParaNodeMaps.
		/// </summary>
		/// <param name="map1"></param>
		/// <param name="map2"></param>
		/// <returns><c>true</c> if map1 and map2 are equal; otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private static bool CompareParaNodeMaps(ParaNodeMap map1, ParaNodeMap map2)
		{
			if (map1 == null && map2 == null)
				return true;
			if (map1 == null || map2 == null)
				return false;
			return map1.Equals(map2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required since we changed equals.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if two diffs are equivalent, for use when comparing recalculated diffs
		/// to those that were already marked as reviewed.
		/// Curr info may have been modified by various edits in the diff tool. Therefore
		/// the Current hvo and ichMin/Lim info may have changed. That curr info is not
		/// required to be compared to determine if the two diffs are equivalent.
		/// </summary>
		/// <param name="otherDiff"></param>
		/// <returns>true if the two diffs are equivalent</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsEquivalent(Difference otherDiff)
		{
			if (otherDiff == null)
				return false;

			if (m_diffType != otherDiff.m_diffType ||
				RefStart != otherDiff.RefStart ||
				RefEnd != otherDiff.RefEnd ||
				ParaRev != otherDiff.ParaRev ||
				IchMinRev != otherDiff.IchMinRev ||
				IchLimRev != otherDiff.IchLimRev ||
				!ArrayUtils.AreEqual(m_SectionsCurr, otherDiff.m_SectionsCurr) ||
				!ArrayUtils.AreEqual(m_SectionsRev, otherDiff.m_SectionsRev))
			{
				return false;
			}

			// For a "missing in current" diff, we want to NOT COMPARE the curr para hvo
			// because that hvo is just an "insertion location" and it gets adjusted as needed.
			if (DiffType != DifferenceType.ParagraphMissingInCurrent &&
				DiffType != DifferenceType.StanzaBreakMissingInCurrent &&
				DiffType != DifferenceType.SectionMissingInCurrent)
			{
				if (ParaCurr != otherDiff.ParaCurr)
					return false;
			}

			// Compare subDiffs? I think we have sufficiently identified a matching diff
			//  and we don't need to look at the subDiffs.

			return true;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Accumulate the ending reference from verses to cover a range in the difference.
		/// </summary>
		/// <remarks>On a subsequent call to Accumulate, a ScrVerse may be repeated. This
		/// is a little less efficient, but permissible.</remarks>
		/// --------------------------------------------------------------------------------
		public void Accumulate(ScrVerse verseCurr, ScrVerse verseRev)
		{
			if (verseCurr == null)
			{
				RefEnd = verseRev.EndRef;
				RevLocation = new DiffLocation(ParaRev, IchMinRev, verseRev.VerseStartIndex + verseRev.TextLength);
			}
			else if (verseRev == null)
			{
				RefEnd = verseCurr.EndRef;
				CurrLocation = new DiffLocation(ParaCurr, IchMinCurr, verseCurr.VerseStartIndex + verseCurr.TextLength);
			}
			else
			{ // accumulate information from both verses
				RefEnd = Math.Max(verseRev.EndRef, verseCurr.EndRef);
				CurrLocation = new DiffLocation(ParaCurr, IchMinCurr, verseCurr.VerseStartIndex + verseCurr.TextLength);
				RevLocation = new DiffLocation(ParaRev, IchMinRev, verseRev.VerseStartIndex + verseRev.TextLength);
			}
		}
		#endregion

		#region Get/Set Properties and Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets this difference's list of Current sections.
		/// (This is only relevant to section difference types. May return null.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IScrSection> SectionsCurr
		{
			get { return m_SectionsCurr; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets this difference's list of Revision sections.
		/// (This is only relevant to section difference types. May return null.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IScrSection> SectionsRev
		{
			get { return m_SectionsRev; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the list of hvos of the added sections for this difference.
		/// This is valid only for Missing/Added Section/SectionHead types.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal List<IScrSection> SectionsAdded
		{
			get
			{
				if (DiffType == DifferenceType.SectionAddedToCurrent || DiffType == DifferenceType.SectionHeadAddedToCurrent)
					return m_SectionsCurr;
				if (DiffType == DifferenceType.SectionMissingInCurrent || DiffType == DifferenceType.SectionHeadMissingInCurrent)
					return m_SectionsRev;
				throw new Exception("Difference.HvosSectionsAdded - DiffType must be Missing/Added Section/SectionHead");
			}
		}

		// not sure if we'll need this
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the destination paragraph where the added items could be inserted.
		///// This is valid only for Missing/Added Section or Para types.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private int paraDest
		//{
		//    get
		//    {
		//        if (diffType == (int)DifferenceType.SectionAddedToCurrent ||
		//            diffType == (int)DifferenceType.ParagraphAddedToCurrent)
		//        {
		//            return hvoRev;
		//        }
		//        else if (diffType == (int)DifferenceType.SectionMissingInCurrent ||
		//            diffType == (int)DifferenceType.ParagraphMissingInCurrent)
		//        {
		//            return hvoCurr;
		//        }
		//        else
		//            throw new Exception("Difference.paraDest - DiffType must be Added/Missing Section or Para");
		//    }
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the destination paragraph and char index where the 'Added' items could be inserted.
		/// This is valid only for Missing/Added Section or Para diff types.
		/// </summary>
		/// <param name="para">hvo of the paragraph</param>
		/// <param name="ich">character index in the paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void SetDestinationIP(IScrTxtPara para, int ich)
		{
			if (DiffType == DifferenceType.SectionAddedToCurrent ||
				DiffType == DifferenceType.SectionHeadAddedToCurrent ||
				DiffType == DifferenceType.ParagraphAddedToCurrent ||
				DiffType == DifferenceType.StanzaBreakAddedToCurrent)
			{
				m_RevLoc = new DiffLocation(para, ich);
				ParaNodeMapRev = new ParaNodeMap(para);
			}
			else if (DiffType == DifferenceType.SectionMissingInCurrent ||
				DiffType == DifferenceType.SectionHeadMissingInCurrent ||
				DiffType == DifferenceType.StanzaBreakMissingInCurrent ||
				DiffType == DifferenceType.ParagraphMissingInCurrent)
			{
				m_CurrLoc = new DiffLocation(para, ich);
				ParaNodeMapCurr = new ParaNodeMap(para);
			}
			else
				throw new Exception("Difference.SetDestinationIP - DiffType must be Added/Missing Section/SectionHead/Para");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the "moved from" paragraph and char index where the 'Moved' item had been in
		/// the same ScrBook.
		/// This is valid only for VerseMoved diff types.
		/// </summary>
		/// <param name="para">the paragraph</param>
		/// <param name="ich">character index in the paragraph</param>
		/// ------------------------------------------------------------------------------------
		public void SetMovedFromIP(IScrTxtPara para, int ich)
		{
			if (DiffType == DifferenceType.VerseMoved)
				m_MovedFrom = new DiffLocation(para, ich);
			else
				throw new Exception("Difference.SetMovedFromIP - DiffType must be VerseMoved");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has at least one sub-diff describing
		/// a difference in ORCs or the objects to which they refer.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has ORC sub diffs; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool HasORCSubDiffs
		{
			get { return (SubDiffsForORCs != null && SubDiffsForORCs.Count > 0); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has at least one sub-diff describing
		/// a paragraph split, merge or structure difference.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has paragraph sub diffs; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool HasParaSubDiffs
		{
			get { return (SubDiffsForParas != null && SubDiffsForParas.Count > 0); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the starting reference for this difference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef RefStart
		{
			get { return new BCVRef(m_refStart); }
			set { m_refStart = new BCVRef(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ending reference for this difference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef RefEnd
		{
			get { return new BCVRef(m_refEnd); }
			set { m_refEnd = new BCVRef(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the diffType as type DifferenceType.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DifferenceType DiffType
		{
			get { return m_diffType; }
			set { m_diffType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Current location.
		/// (This will be null for difference types that deal with one or more sections in the
		/// Current, and thus have a scope larger than a single Current paragraph.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DiffLocation CurrLocation
		{
			get { return m_CurrLoc; }
			set { m_CurrLoc = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Current paragraph that has the difference.
		/// (This will be null for difference types that deal with one or more sections in the
		/// Current, and thus have a scope larger than a single Current paragraph.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara ParaCurr
		{
			get { return m_CurrLoc != null ? m_CurrLoc.Para : null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the starting character index for the difference in the current paragraph,
		/// except when the hvoCurr is 0 in which case it is unused.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IchMinCurr
		{
			get { return m_CurrLoc != null ? m_CurrLoc.IchMin : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index past the end of the difference in the current paragraph,
		/// except when the hvoCurr is 0 in which case it is unused.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IchLimCurr
		{
			get { return m_CurrLoc != null ? m_CurrLoc.IchLim : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Revision location.
		/// (This will be null for difference types that deal with one or more sections in the
		/// Revision, and thus have a scope larger than a single Revision paragraph.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DiffLocation RevLocation
		{
			get { return m_RevLoc; }
			set { m_RevLoc = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Revision paragraph that has the difference.
		/// (This will be null for difference types that deal with one or more sections in the
		/// Revsion, and thus have a scope larger than a single Revision paragraph.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara ParaRev
		{
			get { return m_RevLoc != null ? m_RevLoc.Para : null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the starting character index for the difference in the revision paragraph,
		/// except when the hvoRev is 0 in which case it is unused.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IchMinRev
		{
			get { return m_RevLoc != null ? m_RevLoc.IchMin : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index past the end of the difference in the revision paragraph,
		/// except when the hvoRev is 0 in which case it is unused.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IchLimRev
		{
			get { return m_RevLoc != null ? m_RevLoc.IchLim : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the "Moved From" paragraph
		/// </summary>
		/// <remarks>For VerseMoved differences, we record where the verse was moved from in
		/// the same book</remarks>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara ParaMovedFrom
		{
			get { return m_MovedFrom != null ? m_MovedFrom.Para : null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the char position in the "Moved From" paragraph for VerseMoved
		/// differences.
		/// </summary>
		/// <seealso cref="ParaMovedFrom"/>
		/// ------------------------------------------------------------------------------------
		public int IchMovedFrom
		{
			get { return m_MovedFrom != null ? m_MovedFrom.IchMin : 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the style in the revision paragraph. For text differences,
		/// this would be a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleNameCurr
		{
			get { return m_styleNameCurr; }
			set { m_styleNameCurr = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the style in the revision paragraph. For text differences,
		/// this would be a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleNameRev
		{
			get { return m_styleNameRev; }
			set { m_styleNameRev = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the writing system in the current paragraph, if this difference
		/// represents a single writing system difference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string WsNameCurr
		{
			get { return m_WsNameCurr; }
			set { m_WsNameCurr = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the writing system in the revision paragraph, if this
		/// difference represents a single writing system difference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string WsNameRev
		{
			get { return m_WsNameRev; }
			set { m_WsNameRev = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ParaNodeMap for the Revision paragraph.
		/// </summary>
		/// <value>The para node map rev.</value>
		/// ------------------------------------------------------------------------------------
		public ParaNodeMap ParaNodeMapRev
		{
			get { return m_paraNodeMapRev; }
			set { m_paraNodeMapRev = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ParaNodeMap for the Current paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ParaNodeMap ParaNodeMapCurr
		{
			get { return m_paraNodeMapCurr; }
			set { m_paraNodeMapCurr = value; }
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets or sets the owning diff. If this is a paragraph subdifference, the owning diff
		///// is the root difference. The OwningDiff of a root difference will be null.
		///// </summary>
		///// <remarks>REVIEW: Any need to set the owning diff for ORC subdiffs?</remarks>
		///// ------------------------------------------------------------------------------------
		//public Difference OwningDiff
		//{
		//    get { return m_owningDiff; }
		//    set { m_owningDiff = value; }
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the sub differences that describe changes with ORCs (object replacement
		/// characters) or the objects to which they refer.
		/// </summary>
		/// <remarks>
		/// <para>SubDifferences are a sequence of Differences that are part of this root
		/// difference.</para>
		/// <para>For example:</para>
		/// <list type="bullet">
		///		<item>
		///			<description>for text and styles in footnotes that are different (root
		///			diff type FootnoteDifference),</description>
		///		</item>
		///		<item>
		///			<description>for entire footnotes that are added or missing (root diff
		///			type Footnote Added/Missing),</description>
		///		</item>
		///		<item>
		///			<description>for entire footnotes that are within text that is different
		///			(root diff type TextDifference),</description>
		///		</item>
		/// </list>
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public List<Difference> SubDiffsForORCs
		{
			get { return m_subDiffsORCs; }
			set { m_subDiffsORCs = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the sub differrences that describe paragraph merges, splits or
		/// structure changes. NOTE: This may also describe when verses are moved.
		/// </summary>
		/// <remarks>
		/// <para>SubDifferences are a sequence of Differences that are part of this root
		/// difference.</para>
		/// <para>For example:</para>
		/// <list type="bullet">
		///		<item>
		///			<description>for VerseMoved and VerseMissing (Para too?) diffs that must be
		///			processed with a root SectionAdded diff</description>
		///		</item>
		///		<item>
		///			<description>for ParagraphSplit, ParagraphMerged, and ParagraphStructure diffs,
		///			the subdifferences represent each paragraph in the root diff.</description>
		///		</item>
		/// </list>
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public List<Difference> SubDiffsForParas
		{
			get { return m_subDiffsParas; }
			set { m_subDiffsParas = value; }
		}
		#endregion

		#region Methods called by view constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the revision or current paragraph
		/// </summary>
		/// <param name="fRev"><c>true</c> for revision, <c>false</c> for current</param>
		/// <returns>ScrTxtPara</returns>
		/// ------------------------------------------------------------------------------------
		public IScrTxtPara GetPara(bool fRev)
		{
			if (fRev)
				return ParaRev;
			else
				return ParaCurr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets ichMin of the text range in revision or current
		/// </summary>
		/// <param name="fRev"><c>true</c> for revision, <c>false</c> for current</param>
		/// <returns>Minimum character index</returns>
		/// ------------------------------------------------------------------------------------
		public int GetIchMin(bool fRev)
		{
			if (fRev)
				return IchMinRev;
			else
				return IchMinCurr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets ichLim of the text range in revision or current
		/// </summary>
		/// <param name="fRev"><c>true</c> for revision, <c>false</c> for current</param>
		/// <returns>Maximum character index</returns>
		/// ------------------------------------------------------------------------------------
		public int GetIchLim(bool fRev)
		{
			if (fRev)
				return IchLimRev;
			else
				return IchLimCurr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this diff represents one or more entire sections or a section head
		/// (future: or one or more entire paragraphs)
		/// in the indicated Revision or Current book,
		/// determine if the given paragraph hvo is included in this diff.
		/// </summary>
		/// <param name="para">hvo of the given para</param>
		/// <param name="fRev">the indicated book: <c>true</c> for Revision,
		/// <c>false</c> for Current</param>
		/// <returns><c>true</c> if the whole para is included; otherwise<c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool IncludesWholePara(IScrTxtPara para, bool fRev)
		{
			// If this diff represents whole sections in the indicated book...
			if ((DiffType == DifferenceType.SectionAddedToCurrent && !fRev) ||
				(DiffType == DifferenceType.SectionMissingInCurrent && fRev) ||
				(DiffType == DifferenceType.SectionHeadAddedToCurrent && !fRev) ||
				(DiffType == DifferenceType.SectionHeadMissingInCurrent && fRev) )
			{
				// check that this para is owned by a ScrSection
				IStText text = (IStText)para.Owner;
				IScrSection owningSection = text.Owner as IScrSection;
				if (owningSection == null)
					return false; // para in the title, likely (so not part of this diff)

				// if diff is for the heading only, and the paragraph is not part of it,
				// return false
				if ((DiffType == DifferenceType.SectionHeadAddedToCurrent ||
					DiffType == DifferenceType.SectionHeadMissingInCurrent)
					&& text.OwningFlid != ScrSectionTags.kflidHeading)
					return false; // para is likely in the kflidContent, not the heading

				// look for a section match
				return fRev ? SectionsRev.Contains(owningSection) : SectionsCurr.Contains(owningSection);
			}

			// TODO: Someday we'll also check a diff representing a set of added paragraphs

			return false;
		}
		#endregion

		#region Methods for DiffDialog
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the first section for the indicated book.
		/// This is only relevant for the section difference types.
		/// </summary>
		/// <param name="fRev">the indicated book: <c>true</c> for Revision,
		/// <c>false</c> for Current</param>
		/// <remarks>We assume and require that the sections are in the same order
		/// as the sections that they represent</remarks>
		/// <returns>the first section for the book</returns>
		/// ------------------------------------------------------------------------------------
		public IScrSection GetFirstSection(bool fRev)
		{
			Debug.Assert((DiffType == DifferenceType.SectionAddedToCurrent && !fRev) ||
					(DiffType == DifferenceType.SectionMissingInCurrent && fRev) ||
					(DiffType == DifferenceType.SectionHeadAddedToCurrent && !fRev) ||
					(DiffType == DifferenceType.SectionHeadMissingInCurrent && fRev));

			return fRev ? m_SectionsRev[0] : m_SectionsCurr[0];
		}
		#endregion

		#region IComparable methods, support for sorting Differences

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the physical location of this Difference against that of the given Diff.
		/// Differences are first compared on the basis of their ParaNodeMaps.  If they are
		/// equal, differences are next compared based upon their ichMin (to determine which
		/// comes first in the same paragraph).
		/// If still equal, compare the ParaNodeMap and ichMin on the other side of the diffs.
		/// If still equal, compare on diff type to settle the matter.
		/// </summary>
		/// <param name="obj">The given Difference object to compare myself to</param>
		/// <returns>
		/// The integer value signifying this Difference's location relative to the one given.
		/// If this diff comes after the given diff, return 1, if before, -1, and if they
		/// are equal, 0
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(object obj)
		{
			// Cast the given object and store it, so we don't have to keep casting it
			Difference compareAgainst = (Difference)obj;

			// Declare a number of temporary variables to hold the ParaNodeMaps, ichMins
			// needed
			ParaNodeMap myParaNodeMap;
			ParaNodeMap hisParaNodeMap;
			ParaNodeMap myOtherParaNodeMap;
			ParaNodeMap hisOtherParaNodeMap;
			int myIchMin;
			int hisIchMin;
			int myOtherIchMin;
			int hisOtherIchMin;

			// Check to see whether we are comparing based upon the curr or the rev, and
			// set the variables accordingly
			if (s_sortByCurrent)
			{
				// The primary maps and ichs, used for our preferential comparison
				myParaNodeMap = this.ParaNodeMapCurr;
				hisParaNodeMap = compareAgainst.ParaNodeMapCurr;
				myIchMin = this.IchMinCurr;
				hisIchMin = compareAgainst.IchMinCurr;
				// The secondary maps and ichs (only used if the primaries match)
				myOtherParaNodeMap = this.ParaNodeMapRev;
				hisOtherParaNodeMap = compareAgainst.ParaNodeMapRev;
				myOtherIchMin = this.IchMinRev;
				hisOtherIchMin = compareAgainst.IchMinRev;
			}
			else
			{
				// The primary maps and ichs, used for our preferential comparison
				myParaNodeMap = this.ParaNodeMapRev;
				hisParaNodeMap = compareAgainst.ParaNodeMapRev;
				myIchMin = this.IchMinRev;
				hisIchMin = compareAgainst.IchMinRev;
				// The secondary maps and ichs (only used if the primaries match)
				myOtherParaNodeMap = this.ParaNodeMapCurr;
				hisOtherParaNodeMap = compareAgainst.ParaNodeMapCurr;
				myOtherIchMin = this.IchMinCurr;
				hisOtherIchMin = compareAgainst.IchMinCurr;
			}

			// Both diffs should have ParaNodeMaps, no matter what
			Debug.Assert(myParaNodeMap != null);
			Debug.Assert(hisParaNodeMap != null);
			Debug.Assert(myOtherParaNodeMap != null);
			Debug.Assert(hisOtherParaNodeMap != null);

			// Compare the two diffs on the basis of their ParaNodeMaps
			int result = myParaNodeMap.CompareTo(hisParaNodeMap);
			// If one came out ahead, use the result
			if (result != 0)
				return result;

			// If the ParaNodeMaps are equal, try using ichMin's
			// (only return a result if the ich's are not equal)
			if (myIchMin != hisIchMin)
			{
				if (myIchMin < hisIchMin)
					return -1;
				else if (myIchMin > hisIchMin)
					return 1;
			}

			// If the primary ParaNodeMaps and ich's are equal (which happens
			// when multiple differences on one side, such as a paragraph added,
			// point to one insertion point on the other), order by their
			// secondary ParaNodeMaps--the order in which they appear on the
			// side with many
			result = myOtherParaNodeMap.CompareTo(hisOtherParaNodeMap);
			// If one came out ahead, use the result given
			if (result != 0)
				return result;

			// Compare the other ich's in a last-ditch-effort to compare these diffs
			if (myOtherIchMin != hisOtherIchMin)
			{
				if (myOtherIchMin < hisOtherIchMin)
					return -1;
				else if (myOtherIchMin > hisOtherIchMin)
					return 1;
			}

			// If the two are still equal (indicating that the diffs start in the
			// exact same place, and are one-to-one), compare by diff type
			if (this.m_diffType != compareAgainst.m_diffType)
			{
				if (this.m_diffType > compareAgainst.m_diffType)
					return -1;
				else if (this.m_diffType < compareAgainst.m_diffType)
					return 1;
			}

			// If the whole thing has run through and the diffs are equal in every
			// respect (which shouldn't happen but is theoretically possible),
			// return 0
			return 0;
		}
		#endregion
	}
	#endregion

	#region Comparison class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A comparison object holds references to a Current and  Revision pair of ScrVerses.
	/// It is used when we want to keep a record of a comparison done, even if the comparison
	/// did not find a Difference.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Comparison
	{
		/// <summary>the ScrVerse in the current</summary>
		public ScrVerse verseCurr;
		/// <summary>the ScrVerse in the revision</summary>
		public ScrVerse verseRev;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="currVerse">The curr verse.</param>
		/// <param name="revVerse">The rev verse.</param>
		/// ------------------------------------------------------------------------------------
		public Comparison(ScrVerse currVerse, ScrVerse revVerse)
		{
			verseCurr = currVerse;
			verseRev = revVerse;
		}
	}
	#endregion

}
