// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Actually this class is used both to Make and Remove MovedText Markers/Features.
	/// </summary>
	internal class MakeMovedTextMethod
	{
		readonly ConstituentChartLogic m_logic;
		readonly ChartLocation m_movedTextCell;
		readonly ChartLocation m_markerCell;
		readonly bool m_fPrepose;
		readonly IAnalysis[] m_wordformsToMark;
		private IConstChartMovedTextMarker m_existingMarker; // only used for RemoveMovedFrom()

		public MakeMovedTextMethod(ConstituentChartLogic logic, ChartLocation movedTextCell, ChartLocation markerCell, AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
			: this(logic, movedTextCell, markerCell)
		{
			if (!begPoint.IsValid || !endPoint.IsValid)
			{
				throw new ArgumentException("Bad begin or end point.");
			}
			m_wordformsToMark = begPoint.GetAdvancingWordformsInclusiveOf(endPoint).ToArray();
		}

		public MakeMovedTextMethod(ConstituentChartLogic logic, ChartLocation movedTextCell, IConstChartMovedTextMarker marker)
		{
			m_logic = logic;
			m_movedTextCell = movedTextCell;
			m_existingMarker = marker;
			m_markerCell = new ChartLocation((IConstChartRow)marker.Owner, m_logic.IndexOfColumnForCellPart(marker));
			m_fPrepose = marker.Preposed;
		}

		public MakeMovedTextMethod(ConstituentChartLogic logic, ChartLocation movedTextCell, ChartLocation markerCell)
		{
			m_logic = logic;
			m_movedTextCell = movedTextCell;
			m_markerCell = markerCell;
			m_fPrepose = m_logic.IsPreposed(movedTextCell, markerCell);
		}

		#region PropertiesAndConstants

		private LcmCache Cache => m_logic.Cache;

		private IConstChartRow MTRow => m_movedTextCell.Row;

		private IConstChartRow MarkerRow => m_markerCell.Row;

		private int MarkerColIndex => m_markerCell.ColIndex;

		#endregion

		#region Method Object methods

		/// <summary>
		/// One of two main entry points for this method object. The other is RemoveMovedFrom().
		/// Perhaps this should be deprecated? Does it properly handle multiple WordGroups in a cell?
		/// </summary>
		/// <returns></returns>
		internal IConstChartMovedTextMarker MakeMovedFrom()
		{
			// Making a MovedFrom marker creates a Feature on the Actual row that needs to be updated.
			// Removed ExtraPropChangedInserter that caused a crash on Undo in certain contexts. (LT-9442)
			// Not real sure why everything still works!
			// Get rid of any empty marker in the cell where we will insert the marker.
			m_logic.RemoveMissingMarker(m_markerCell);

			if (m_wordformsToMark == null || m_wordformsToMark.Length == 0)
			{
				return MarkEntireCell();
			}
			return MarkPartialCell();
		}

		/// <summary>
		/// This handles the situation where we mark only the user-selected words as moved.
		/// m_hvosToMark member array contains the words to mark as moved
		///		(for now they should be contiguous)
		/// Deals with four cases:
		///		Case 1: the new WordGroup is at the beginning of the old, create a new one to hold the remainder
		///		Case 2: the new WordGroup is embedded within the old, create 2 new ones, first for the movedText
		///			and second for the remainder
		///		Case 3: the new WordGroup is at the end of the old, create a new one for the movedText
		///		Case 4: the new WordGroup IS the old WordGroup, just set the feature and marker
		///			(actually this last case should be handled by the dialog; it'll return no hvos)
		/// </summary>
		/// <returns></returns>
		private IConstChartMovedTextMarker MarkPartialCell()
		{
			// find the first WordGroup in this cell
			var cellContents = m_logic.PartsInCell(m_movedTextCell);
			var movedTextWordGrp = ConstituentChartLogic.FindFirstWordGroup(cellContents);
			Debug.Assert(movedTextWordGrp != null);

			// Does this WordGroup contain all our hvos and are they "in order" (no skips or unorderliness)
			int ilastHvo;
			var ifirstHvo = CheckWordGroupContainsAllOccurrenceHvos(movedTextWordGrp, out ilastHvo);
			if (ifirstHvo > -1)
			{
				var indexOfWordGrpInRow = movedTextWordGrp.IndexInOwner;
				// At this point we need to deal with our four cases:
				if (ifirstHvo == 0)
				{
					if (ilastHvo < movedTextWordGrp.GetOccurrences().Count - 1)
					{
						// Case 1: Add new WordGroup, put remainder of wordforms in it
						PullOutRemainderWordformsToNewWordGroup(movedTextWordGrp, indexOfWordGrpInRow, ilastHvo + 1);
					}
					// Case 4: just drops through here, sets Marker
				}
				else
				{
					if (ilastHvo < movedTextWordGrp.GetOccurrences().Count - 1)
					{
						// Case 2: Take MT wordforms and remainder wordforms out of original,
						// add new WordGroups for MT and for remainder

						// For now, just take out remainder wordforms and add a WordGroup for them.
						// The rest will be done when we drop through to Case 3
						PullOutRemainderWordformsToNewWordGroup(movedTextWordGrp, indexOfWordGrpInRow, ilastHvo + 1);
					}
					// Case 3: Take MT wordforms out of original, add a new WordGroup for the MT
					movedTextWordGrp = PullOutRemainderWordformsToNewWordGroup(movedTextWordGrp, indexOfWordGrpInRow, ifirstHvo);
				}
			}
			else
			{
				return null; // Bail out because of a problem in our array of hvos (misordered or non-contiguous)
			}

			// Now figure out where to insert the MTmarker
			// Insert the Marker
			return m_logic.MakeMTMarker(m_markerCell, movedTextWordGrp, FindWhereToInsertMTMarker(), m_fPrepose);
		}

		/// <summary>
		/// Pulls trailing wordforms out of a WordGroup and creates a new one for them.
		/// Then we return the new WordGroup.
		/// </summary>
		/// <param name="srcMovedText">The original WordGroup from which we will remove the moved
		/// 	text wordforms.</param>
		/// <param name="indexInRow"></param>
		/// <param name="ifirstWord"></param>
		/// <returns>The new WordGroup that will now contain the moved text wordforms.</returns>
		private IConstChartWordGroup PullOutRemainderWordformsToNewWordGroup(IConstChartWordGroup srcMovedText, int indexInRow, int ifirstWord)
		{
			var srcWordforms = srcMovedText.GetOccurrences();
			if (ifirstWord >= srcWordforms.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(ifirstWord));
			}

			var oldSrcEnd = srcWordforms[srcWordforms.Count - 1]; // EndPoint of source becomes endPoint of new WordGroup
			var dstBegin = srcWordforms[ifirstWord];
			var newSrcEnd = dstBegin.PreviousWordform();
			// Move source EndPoint up to Analysis previous to first destination Analysis
			srcMovedText.EndSegmentRA = newSrcEnd.Segment;
			srcMovedText.EndAnalysisIndex = newSrcEnd.Index;

			return m_logic.MakeWordGroup(m_movedTextCell, indexInRow + 1, dstBegin, oldSrcEnd);
		}

		/// <summary>
		/// Checks our array of hvos to see that all are referenced by this WordGroup and that
		/// they are in order. For now they must be contigous too. Returns the index of the
		/// first hvo within Begin/EndAnalysisIndex for this WordGroup or -1 if check fails.
		/// The out variable returns the index of the last hvo to mark within this WordGroup.
		/// </summary>
		private int CheckWordGroupContainsAllOccurrenceHvos(IAnalysisReference movedTextWordGrp, out int ilastHvo)
		{
			Debug.Assert(m_wordformsToMark.Length > 0, "No words to mark!");
			Debug.Assert(movedTextWordGrp != null && movedTextWordGrp.IsValidRef, "No wordforms in this WordGroup!");
			var movedTextWordformList = movedTextWordGrp.GetOccurrences();
			Debug.Assert(movedTextWordformList != null, "No wordforms in this WordGroup!");
			var cwordforms = movedTextWordformList.Count;
			Debug.Assert(cwordforms > 0, "No wordforms in this WordGroup!");
			var ifirstHvo = -1;
			ilastHvo = -1;
			var ihvosToMark = 0;
			var chvosToMark = m_wordformsToMark.Length;
			for (var iwordform = 0; iwordform < cwordforms; iwordform++)
			{
				// TODO: Check this! Does it work?!
				if (movedTextWordformList[iwordform].Analysis == m_wordformsToMark[ihvosToMark])
				{
					// Found a live one!
					if (ihvosToMark == 0)
					{
						ifirstHvo = iwordform; // found the first one!
					}
					ilastHvo = iwordform;
					ihvosToMark++;

					// If we've found them all, get out before we have an array subscript error!
					if (ihvosToMark == chvosToMark)
					{
						return ifirstHvo;
					}
				}
				else
				{
					if (ifirstHvo > -1 && ihvosToMark < chvosToMark)
					{
						// We had found some, but we aren't finding anymore
						// and we haven't found all of them yet! Error!
						return -1;
					}
				}
			}
			// The only way to really hit the end of the 'for' loop is by failing to finish finding
			// all of the hvosToMark array, so this is an error too.
			return -1;
		}

		private IConstChartMovedTextMarker MarkEntireCell()
		{
			// This handles the case where we mark the entire cell (CCWordGroup) as moved.
			// Review: What happens if there is more than one WordGroup in the cell already?
			// At this point, only the first will get marked as moved.
			var wordGroupMovedText = (IConstChartWordGroup)m_logic.FindCellPartInColumn(m_movedTextCell, true);
			Debug.Assert(wordGroupMovedText != null);

			// Now figure out where to insert the MTmarker
			var icellPartInsertAt = FindWhereToInsertMTMarker();

			return m_logic.MakeMTMarker(m_markerCell, wordGroupMovedText, icellPartInsertAt);
		}

		/// <summary>
		/// Insert BEFORE anything already in column, if Preposed; otherwise, after.
		/// </summary>
		/// <returns></returns>
		private int FindWhereToInsertMTMarker()
		{
			var markerCell = new ChartLocation(MarkerRow, (m_fPrepose ? MarkerColIndex - 1: MarkerColIndex));
			return m_logic.FindIndexOfCellPartInLaterColumn(markerCell);
		}

		internal void RemoveMovedFrom()
		{
			if (m_existingMarker == null)
			{
				FindMarkerAndDelete();
			}
			else
			{
				m_markerCell.Row.CellsOS.Remove(m_existingMarker);
			}

			// Handle cases where after removing, there are multiple WordGroups that can be merged.
			// If the removed movedFeature was part of the cell,
			// there could easily be 3 WordGroups to merge after removing.
			CollapseRedundantWordGroups();
		}

		private void FindMarkerAndDelete()
		{
			foreach (var part in m_logic.PartsInCell(m_markerCell).Where(cellPart => m_logic.IsMarkerOfMovedFrom(cellPart, m_movedTextCell)))
			{
				// Remove Marker from Row
				MarkerRow.CellsOS.Remove(part);
			}
		}

		private void CollapseRedundantWordGroups()
		{
			// Enhance GordonM: May need to do something different here if we can put markers between WordGroups.

			// Get ALL the cellParts
			var cellPartList = m_logic.PartsInCell(m_movedTextCell);

			for (var icellPart = 0; icellPart < cellPartList.Count - 1; icellPart++)
			{
				var currCellPart = cellPartList[icellPart];
				if (!(currCellPart is IConstChartWordGroup) || ConstituentChartLogic.IsMovedText(currCellPart))
				{
					continue;
				}
				while (icellPart < cellPartList.Count - 1) // Allows swallowing multiple WordGroups if conditions are right.
				{
					var nextCellPart = cellPartList[icellPart + 1];
					if (!(nextCellPart is IConstChartWordGroup))
					{
						break;
					}
					if (ConstituentChartLogic.IsMovedText(nextCellPart))
					{
						icellPart++; // Skip current AND next, since next is MovedText
						continue;
					}
					// Conditions are right! Swallow the next WordGroup into the current one!
					SwallowRedundantWordGroup(cellPartList, icellPart);
				}
			}
		}

		private void SwallowRedundantWordGroup(List<IConstituentChartCellPart> cellPartList, int icellPart)
		{
			// Move all analyses from cellPartList[icellPart+1] to end of cellPartList[icellPart].
			var srcWordGrp = cellPartList[icellPart+1] as IConstChartWordGroup;
			var dstWordGrp = cellPartList[icellPart] as IConstChartWordGroup;
			m_logic.MoveAnalysesBetweenWordGroups(srcWordGrp, dstWordGrp);

			// Clean up
			cellPartList.Remove(srcWordGrp); // remove swallowed WordGroup from list
			MTRow.CellsOS.Remove(srcWordGrp); // remove swallowed WordGroup from row (& therefore delete it)
		}

		#endregion
	}
}