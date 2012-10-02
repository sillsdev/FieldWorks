// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: OverridesLing_Disc.cs
// Responsibility: FW Team
//
// <remarks>
// This file holds the overrides of the generated classes for the Ling module related to
// discourse analysis.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Constituent Chart Row class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ConstChartRow
	{
		#region Overrides of CmObject

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);
			if (!Cache.ObjectsBeingDeleted.Contains(this) &&
				e.Flid == ConstChartRowTags.kflidCells && (e.DelaySideEffects || e.ForDeletion))
			{
				DeleteMyselfIfEmpty();
			}
		}

		private void DeleteMyselfIfEmpty()
		{
			var crows = CellsOS.Count; // is this before or after this object?
			if (crows == 0 && (Notes == null || Notes.Text == null))
			{
				((IDsConstChart) Owner).RowsOS.Remove(this);
			}
		}

		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Constituent Chart Word Group class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ConstChartWordGroup
	{
		partial void SetDefaultValuesInConstruction()
		{
			m_BeginAnalysisIndex = -1;
			m_EndAnalysisIndex = -1;
		}

		/// <summary>
		/// Get all of the Analyses associated with a single ConstChartWordGroup.
		/// Returns null if there is a problem finding them. Includes PunctuationForms.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IAnalysis> GetAllAnalyses()
		{
			if (!IsValidRef)
				return null;

			var point1 = new AnalysisOccurrence(BeginSegmentRA, BeginAnalysisIndex);
			var point2 = new AnalysisOccurrence(EndSegmentRA, EndAnalysisIndex);
			return point1.GetAdvancingOccurrencesInclusiveOf(point2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares WordGroups in two segments to see if they are similar enough to be considered
		/// the same. Mostly designed for testing. Tests wordforms tagged for same baseline text
		/// and checks to see that they both reference the same CmPossibility column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsAnalogousTo(IConstChartWordGroup otherWordGrp)
		{
			if (otherWordGrp == null)
				return false;
			if (this.ColumnRA != otherWordGrp.ColumnRA)
				return false;
			var myWordforms = this.GetOccurrences();
			var otherWordforms = otherWordGrp.GetOccurrences();
			if (myWordforms == null || otherWordforms == null || myWordforms.Count == 0 || otherWordforms.Count == 0)
				throw new ArgumentException("Found an invalid ConstChartWordGroup.");
			if (myWordforms.Count != otherWordforms.Count)
				return false;
			// Below LINQ returns false if it finds any tagged wordforms in the two lists
			// that have different baseline text (at the same index)
			return !myWordforms.Where((t, i) => t.BaselineText.Text != otherWordforms[i].BaselineText.Text).Any();
		}

		#region IAnalysisReference members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an AnalysisOccurrence equivalent to the begin point of this reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public AnalysisOccurrence BegRef()
		{
			return new AnalysisOccurrence(BeginSegmentRA, BeginAnalysisIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an AnalysisOccurrence equivalent to the end point of this reference.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public AnalysisOccurrence EndRef()
		{
			return new AnalysisOccurrence(EndSegmentRA, EndAnalysisIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the reference targets valid Segments and Analysis indices, and the
		/// beginning point of the reference is not after the ending point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsValidRef
		{
			get
			{
				if (BeginSegmentRA == null || EndSegmentRA == null ||
					BeginSegmentRA.AnalysesRS == null || EndSegmentRA.AnalysesRS == null ||
					BeginAnalysisIndex < 0 || EndAnalysisIndex < 0 ||
					BeginAnalysisIndex >= BeginSegmentRA.AnalysesRS.Count ||
					EndAnalysisIndex >= EndSegmentRA.AnalysesRS.Count)
					return false;
				// Enhance GJM: Someday we might check the occurrences to see if they have
				// Wordforms or not.
				return !BegRef().IsAfter(EndRef());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change reference to a different Segment object. Used by AnalysisAdjuster.
		/// </summary>
		/// <param name="newSeg"></param>
		/// <param name="fBegin">True if BeginSegment is affected.</param>
		/// <param name="fEnd">True if EndSegment is affected.</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeToDifferentSegment(ISegment newSeg, bool fBegin, bool fEnd)
		{
			if (newSeg == null)
				throw new ArgumentNullException();
			if (fBegin)
				BeginSegmentRA = newSeg;
			if (fEnd)
				EndSegmentRA = newSeg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change reference to a different AnalysisIndex. Used by AnalysisAdjuster.
		/// If AnalysisReference is multi-Segment, this presently only handles
		/// changes to one endpoint.
		/// </summary>
		/// <param name="newIndex">change index to this</param>
		/// <param name="fBegin">True if BeginAnalysisIndex is affected.</param>
		/// <param name="fEnd">True if EndAnalysisIndex is affected.</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeToDifferentIndex(int newIndex, bool fBegin, bool fEnd)
		{
			if (newIndex < 0)
				throw new ArgumentOutOfRangeException("newIndex", "Can't set index to a negative number.");
			if (fEnd && fBegin &&
				BeginSegmentRA != EndSegmentRA)
				throw new NotImplementedException();
			if (fBegin)
			{
				BeginAnalysisIndex = newIndex;
				var max = BeginSegmentRA.AnalysesRS.Count - 1;
				BeginAnalysisIndex = Math.Min(BeginAnalysisIndex, max);
				BeginAnalysisIndex = Math.Max(BeginAnalysisIndex, 0);
			}
			if (fEnd)
			{
				EndAnalysisIndex = newIndex;
				var max = EndSegmentRA.AnalysesRS.Count - 1;
				EndAnalysisIndex = Math.Min(EndAnalysisIndex, max);
				EndAnalysisIndex = Math.Max(EndAnalysisIndex, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all of the Words associated with a single ConstChartWordGroup.
		/// Returns an empty list if there is a problem finding them.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<AnalysisOccurrence> GetOccurrences()
		{
			var result = new List<AnalysisOccurrence>();
			if (!IsValidRef)
				return result;
			var point1 = new AnalysisOccurrence(BeginSegmentRA, BeginAnalysisIndex);
			var point2 = new AnalysisOccurrence(EndSegmentRA, EndAnalysisIndex);
			var curOccurrence = point1;
			while (curOccurrence.IsValid)
			{
				if (curOccurrence.HasWordform) // This is the new "Wfic" test (word as opposed to punctuation).
					result.Add(curOccurrence);
				if (curOccurrence == point2)
					break; // Reached endpoint.
				curOccurrence = curOccurrence.NextWordform();
				if (curOccurrence == null || curOccurrence.IsAfter(point2))
					// First part shouldn't happen... (means we hit the end of the text w/o hitting endpoint!)
					// Second part could happen if the endpoint is Punctuation.
					break;
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if this reference occurs after the parameter's reference in the text.
		/// </summary>
		/// <param name="otherReference"></param>
		/// ------------------------------------------------------------------------------------
		public bool IsAfter(IAnalysisReference otherReference)
		{
			var otherBegPoint = otherReference.BegRef();
			var myEndPoint = EndRef();
			// Test to see if we're at least in the same paragraph!
			if (myEndPoint.Segment.Owner.Hvo == otherBegPoint.Segment.Owner.Hvo)
			{
				return myEndPoint.GetMyBeginOffsetInPara() > otherBegPoint.GetMyEndOffsetInPara();
			}
			// Different paragraphs
			return myEndPoint.Segment.Owner.IndexInOwner > otherBegPoint.Segment.Owner.IndexInOwner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the end to the next Analysis, if possible.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GrowFromEnd(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().GrowFromEnd(
				fignorePunct, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the beginning to the previous Analysis, if
		/// not already at the beginning of the text.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GrowFromBeginning(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().GrowFromBeginning(
				fignorePunct, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the end to the previous Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShrinkFromEnd(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().ShrinkFromEnd(
				fignorePunct, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the beginning to the next Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShrinkFromBeginning(bool fignorePunct)
		{
			return Cache.ServiceLocator.GetInstance<IReferenceAdjuster>().ShrinkFromBeginning(
				fignorePunct, this);
		}

		#endregion

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Constituent Chart Clause Marker class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ConstChartClauseMarker
	{

		#region CmObject overrides

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			if (!Cache.ObjectsBeingDeleted.Contains(this) &&
				e.Flid == ConstChartClauseMarkerTags.kflidDependentClauses)
				HandleDepClauseChanges(e);
			base.RemoveObjectSideEffectsInternal(e);
		}

		private void HandleDepClauseChanges(RemoveObjectEventArgs e)
		{
			var crows = DependentClausesRS.Count; // Count is after we remove this row
			if (crows == 0)
				// Not referencing anymore rows, delete self!
				((IConstChartRow) Owner).CellsOS.Remove(this);
			else
				FixAffectedClauseMarker(this, e);
		}

		/// <summary>
		/// Fixes features in dependent clauses in preparation for deleting a row.
		/// Caller guarantees that the Clause Marker pointed to more than one row, since Markers
		/// go away automatically when their 'last' row is deleted.
		/// </summary>
		/// <param name="marker"></param>
		/// <param name="e">RemoveObjectEventArgs</param>
		private static void FixAffectedClauseMarker(IConstChartClauseMarker marker, RemoveObjectEventArgs e)
		{
			// Enhance GordonM: This is another place that will need to change in the unlikely event that
			// dependent clauses can someday be non-contiguous.
			var arrayMax = marker.DependentClausesRS.Count - 1; // the new array limit
			var idelRow = e.Index;

			// Of the 2 following conditionals, only one should match, if any.

			// If the deleted reference was the first in the property,
			// move the firstDep feature to the next row in the list.
			if (idelRow == 0)
				marker.DependentClausesRS[0].StartDependentClauseGroup = true;

			// If delRow was the last reference in the property,
			// move the endDep feature to the previous row in the list.
			if (idelRow > arrayMax)
				marker.DependentClausesRS[arrayMax].EndDependentClauseGroup = true;
		}

		public bool HasValidRefs
		{
			get
			{
				var crows = DependentClausesRS.Count;
				if (crows == 0)
					return false;
				for (var irow = 0; irow < crows; irow++)
				{
					var depRow = DependentClausesRS[irow];
					if (irow == 0 && !depRow.StartDependentClauseGroup)
						return false;
					if (irow == crows && !depRow.EndDependentClauseGroup)
						return false;
					if (depRow.ClauseType == ClauseTypes.Normal)
						return false;
				}
				return true;
			}
		}

		#endregion

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Constituent Chart Moved Text Marker class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ConstChartMovedTextMarker
	{

		#region CmObject overrides

		partial void WordGroupRASideEffects(IConstChartWordGroup oldObjValue, IConstChartWordGroup newObjValue)
		{
			if (newObjValue == null)
				DeleteMyself();
		}

		private void DeleteMyself()
		{
			if (!Cache.ObjectsBeingDeleted.Contains(this))
				((IConstChartRow)Owner).CellsOS.Remove(this);
		}

		///<summary>
		/// Returns true if WordGroup property contains a valid reference
		/// (i.e. is not null)
		///</summary>
		public bool HasValidRef
		{
			get { return WordGroupRA != null; }
		}

		#endregion

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Constituent Chart Tag class (also known as List Marker)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ConstChartTag
	{

		#region CmObject overrides

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			base.RemoveObjectSideEffectsInternal(e);
			if (e.Flid == ConstChartTagTags.kflidTag && e.ForDeletion)
				DeleteMyself();
		}

		private void DeleteMyself()
		{
			if (!Cache.ObjectsBeingDeleted.Contains(this))
				((IConstChartRow)Owner).CellsOS.Remove(this);
		}

		#endregion

	}
}
