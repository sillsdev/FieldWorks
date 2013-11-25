// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ReferenceAdjusterService.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A class to provide services to ConstChartWordGroup and TextTag objects (IAnalysisReference)
	/// in order to adjust their references.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ReferenceAdjusterService : IReferenceAdjuster
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the end to the next Analysis, if possible.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <param name="reference"></param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GrowFromEnd(bool fignorePunct, IAnalysisReference reference)
		{
			var endPoint = reference.EndRef();
			endPoint = fignorePunct ? endPoint.NextWordform() : endPoint.NextAnalysisOccurrence();
			if (endPoint == null)
				return false;
			if (reference.EndRef().Segment.Hvo != endPoint.Segment.Hvo)
				reference.ChangeToDifferentSegment(endPoint.Segment, false, true);
			if (reference.EndRef().Index != endPoint.Index)
				reference.ChangeToDifferentIndex(endPoint.Index, false, true);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the reference in the text from the beginning to the previous Analysis, if
		/// not already at the beginning of the text.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep expanding until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <param name="reference"></param>
		/// <returns>False if there was no room to grow that direction in this text.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GrowFromBeginning(bool fignorePunct, IAnalysisReference reference)
		{
			var begPoint = reference.BegRef();
			begPoint = fignorePunct ? begPoint.PreviousWordform() : begPoint.PreviousAnalysisOccurrence();
			if (begPoint == null)
				return false;
			if (reference.BegRef().Segment.Hvo != begPoint.Segment.Hvo)
				reference.ChangeToDifferentSegment(begPoint.Segment, true, false);
			if (reference.BegRef().Index != begPoint.Index)
				reference.ChangeToDifferentIndex(begPoint.Index, true, false);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the end to the previous Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its endpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <param name="reference"></param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShrinkFromEnd(bool fignorePunct, IAnalysisReference reference)
		{
			var endPoint = reference.EndRef();
			endPoint = fignorePunct ? endPoint.PreviousWordform() : endPoint.PreviousAnalysisOccurrence();
			if (endPoint == null || reference.BegRef().IsAfter(endPoint))
				return false;
			if (reference.EndRef().Segment.Hvo != endPoint.Segment.Hvo)
				reference.ChangeToDifferentSegment(endPoint.Segment, false, true);
			if (reference.EndRef().Index != endPoint.Index)
				reference.ChangeToDifferentIndex(endPoint.Index, false, true);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shrinks the reference in the text from the beginning to the next Analysis. If it
		/// returns false, the reference should be deleted because it couldn't shrink anymore.
		/// </summary>
		/// <param name="fignorePunct">True if it should keep shrinking until its beginpoint
		/// reaches an Analysis that has a wordform.</param>
		/// <param name="reference"></param>
		/// <returns>False if this AnalysisReference should be deleted because it no longer
		/// refers to any analyses.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShrinkFromBeginning(bool fignorePunct, IAnalysisReference reference)
		{
			var begPoint = reference.BegRef();
			var endPoint = reference.EndRef();
			begPoint = fignorePunct ? begPoint.NextWordform() : begPoint.NextAnalysisOccurrence();
			if (begPoint == null || begPoint.IsAfter(endPoint))
				return false;
			if (reference.BegRef().Segment.Hvo != begPoint.Segment.Hvo)
				reference.ChangeToDifferentSegment(begPoint.Segment, true, false);
			if (reference.BegRef().Index != begPoint.Index)
				reference.ChangeToDifferentIndex(begPoint.Index, true, false);
			return true;
		}
	}
}
