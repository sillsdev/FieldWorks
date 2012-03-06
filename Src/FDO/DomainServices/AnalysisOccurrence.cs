// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AnalysisOcurrence.cs
// Responsibility: thomson
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class is used by code that needs to identify a particular occurrence of an analysis
	/// within a segment.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AnalysisOccurrence
	{
		/// <summary>
		/// The segment in which the occurrence is found.
		/// </summary>
		public ISegment Segment { get; private set; }
		/// <summary>
		/// The index within the segment of the particular occurrence.
		/// </summary>
		public int Index { get; private set; }
		/// <summary>
		/// The actual analysis that occurs at this position.
		/// Setter modifies the segment, and must be used inside a UOW.
		/// Will return null if this Occurence is not valid.
		/// (callers should probably check IsValid rather than testing for a null return)
		/// </summary>
		public virtual IAnalysis Analysis
		{
			get
			{
				if(Index < Segment.AnalysesRS.Count)
					return Segment.AnalysesRS[Index];
				return null;
			}
			set
			{
				Segment.AnalysesRS.Replace(Index, 1, new ICmObject[] {value});
			}
		}
		/// <summary>
		/// Make one.
		/// Note: index is not the index of the segment in its owning paragraph.
		/// </summary>
		/// <param name="seg">A segment with at least index+1 analysis references.</param>
		/// <param name="index">The index of an analysis in the segment.</param>
		public AnalysisOccurrence(ISegment seg, int index)
		{
			Segment = seg;
			Index = index;
		}

		/// <summary>
		/// The paragraph to which the segment belongs.
		/// </summary>
		public IStTxtPara Paragraph
		{
			get { return (IStTxtPara) Segment.Owner; }
		}

		/// <summary>
		/// Answer true if this is a valid analysis occurrence for the current state of things.
		/// </summary>
		public bool IsValid
		{
			get
			{
				if (Segment == null || Index < 0)
					return false;
				return Segment.IsValidObject && Segment.AnalysesRS.Count > Index;
			}
		}

		/// <summary>
		/// The actual text which occurs in the paragraph at this occurrence.
		/// May differ in case from the associated Wordform.
		/// </summary>
		public ITsString BaselineText
		{
			get
			{
				return Segment.GetBaselineText(Index);
			}
		}

		/// <summary>
		/// Get the writing system which this occurrence has in the base text.
		/// Can return -1 if this is really not an occurance, but an empty translation line.
		/// Enhance JohnT: could make it slightly more efficient with a method on segment etc
		/// to get the WS without making a substring.
		/// </summary>
		public int BaselineWs
		{
			get
			{
				if (!IsValid)
					return -1; // might happen on an empty translation line.
				return TsStringUtils.GetWsAtOffset(Paragraph.Contents, GetMyBeginOffsetInPara());
			}
		}

		/// <summary>
		/// Answer whether this analysis has a wordform. This should be true for any analysis that is not
		/// punctuation and is equivalent to old tests for being a "Wfic".
		/// </summary>
		public bool HasWordform
		{
			get { return Analysis != null && Analysis.Wordform != null;}
		}

		/// <summary>
		/// Override equality.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is AnalysisOccurrence)
				return Equals((AnalysisOccurrence)obj);
			return false;
		}

		/// <summary>
		/// More efficient equality if the other argument is known to be an AnalysisOccurrence.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(AnalysisOccurrence other)
		{
			return other.Segment == Segment && other.Index == Index;
		}

		/// <summary>
		/// And we must override this if we do Equals...
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Segment.GetHashCode() ^ Index;
		}

		/// <summary>
		/// Makes == work properly for the class.
		/// </summary>
		public static bool operator ==(AnalysisOccurrence point1, AnalysisOccurrence point2)
		{
			return Object.Equals(point1, point2);
		}

		/// <summary>
		/// Makes != work properly for the class.
		/// </summary>
		public static bool operator !=(AnalysisOccurrence point1, AnalysisOccurrence point2)
		{
			return !Object.Equals(point1, point2);
		}

		/// <summary>
		/// Display the segment and index.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (Segment == null)
				return "A bad AnalysisOccurrence with null segment";
			else
				return Segment.ToString() + " item " + Index;
		}

		/// <summary>
		/// Returns a sequence of IAnalysis objects inclusive of the current occurrence
		/// and the end occurrence (otherAC). Parameter occurrence must occur AFTER 'this'
		/// occurrence.
		/// </summary>
		/// <param name="point2">An AnalysisOccurrence</param>
		/// <returns></returns>
		public IEnumerable<IAnalysis> GetAdvancingOccurrencesInclusiveOf(AnalysisOccurrence point2)
		{
			if (!point2.IsValid)
				throw new ArgumentException("Invalid Analysis Occurrence");
			if (Segment == point2.Segment)
			{
				// Case 1: Two points in same ISegment object
				if (Index > point2.Index)
					throw new ArgumentException("Second AnalysisOccurrence is before the first!");
				// Copy out sub-array of Analyses between the two ACs indices
				return Segment.AnalysesRS.ToList().GetRange(Index, point2.Index - Index + 1);
			}
			if (Segment.Owner == point2.Segment.Owner)
			{
				// Case 2: Two points in different ISegment objects, but in same StTxtPara
				if (Segment.IndexInOwner > point2.Segment.IndexInOwner)
					throw new ArgumentException("Second AnalysisOccurrence is before the first!");
				// Need to copy out end of first segment, any segments in between and beginning of second segment
				return CopyOutAnalysesFromMultipleSegments(point2);
			}
			var para1 = Segment.Owner as IStTxtPara;
			var para2 = point2.Segment.Owner as IStTxtPara;
			if (!(para1.Owner as IStText).ParagraphsOS.Contains(para2))
			{
				throw new ArgumentOutOfRangeException("point2", "AnalysisOccurrences are not within the same Text!");
			}
			throw new NotImplementedException("So far we only handle this in same Segment or Paragraph.");
		}

		/// <summary> What it says. </summary>
		public bool CanMakePhraseWithNextWord()
		{
			if (!this.HasWordform)
				return false;
			var nextOccurrence = NextAnalysisOccurrence();
			return (nextOccurrence != null && nextOccurrence.Segment == Segment && nextOccurrence.HasWordform
					&& nextOccurrence.BaselineWs == BaselineWs);

		}

		/// <summary>
		/// Combine this instance (which must have a wordform) with the next (which must also have one, in the same WS).
		/// Returns null if it can't be done.
		/// </summary>
		public AnalysisOccurrence MakePhraseWithNextWord()
		{
			if (!CanMakePhraseWithNextWord())
				return null;
			int beginOffset = GetMyBeginOffsetInPara();
			var oldWf1 = Analysis.Wordform;
			var next = NextAnalysisOccurrence();
			var oldWf2 = next.Analysis.Wordform;
			int endOffset = next.GetMyEndOffsetInPara();
			var newForm = Paragraph.Contents.Substring(beginOffset, endOffset - beginOffset);
			var wordform = WfiWordformServices.FindOrCreateWordform(Segment.Cache, newForm);
			var mainWs = newForm.get_WritingSystem(0);
			var otherWss =
				oldWf1.Form.AvailableWritingSystemIds.Union(oldWf2.Form.AvailableWritingSystemIds).Where(ws => ws != mainWs);
			// If the wordforms we're merging have other writing system information, and the destination wordform doesn't
			// already have values for those WSs, copy it over.
			foreach (var ws in otherWss)
			{
				if (wordform.Form.get_String(ws).Length == 0)
				{
					var combined = oldWf1.Form.get_String(ws);
					var other = oldWf2.Form.get_String(ws);
					if (combined.Length == 0)
						combined = other; // just copy oldWf2, nothing in oldWf1
					else if (other.Length > 0)
					{
						// concatenate them, with a space between
						var bldr = combined.GetBldr();
						bldr.Replace(bldr.Length, bldr.Length, " ", null);
						bldr.ReplaceTsString(bldr.Length, bldr.Length, other);
						combined = bldr.GetString();
					}
					// else combined is already the whole value, from oldWf1
					wordform.Form.set_String(ws, combined);
				}
			}
			Segment.AnalysesRS.Replace(Index, 2, new ICmObject[] {wordform});
			// We may be able to make guesses of this phrase elsewhere.
			// Review JohnT: this is needed to make a 6.0 test pass, but it's a bit strange: in principle,
			// this new phrase could be discovered and guessed anywhere, not just in this paragraph.
			Paragraph.ParseIsCurrent = false;
			// One of the old wordforms may now be able to be deleted (especially if it was a shorter phrase).
			DeleteWordformIfPossible(oldWf1);
			if (oldWf1 != oldWf2) // otherwise it may already have been deleted
				DeleteWordformIfPossible(oldWf2);
			return this; // still has the correct segment and index, no reason to make a new one.
		}

		/// <summary>
		/// Return true if this occurrence is a phrase (and so can be broken down to wordforms).
		/// </summary>
		/// <returns></returns>
		public bool CanBreakPhrase()
		{
			if (!HasWordform)
				return false;
			return ParagraphParser.IsPhrase(Segment.Cache, BaselineText);
		}

		/// <summary>
		/// Break an occurrence that is a phrase into its constituent wordforms.
		/// </summary>
		public void BreakPhrase()
		{
			using (var pp = new ParagraphParser(Paragraph))
			{
				// This is a new paragraph parser, and we haven't set up any pre-existing analyses, so it doesn't matter
				// what we pass for cWfAnalysisPrev.
				IList<IAnalysis> wordforms = pp.CollectSegmentForms(GetMyBeginOffsetInPara(), GetMyEndOffsetInPara(), 0, false);
				if (wordforms.Count > 1)
				{
					var oldWordform = Analysis.Wordform;
					Segment.AnalysesRS.Replace(Index, 1, wordforms.Cast<ICmObject>());
					// Enhance JohnT: for this sort of automatic deletion, I wonder whether we should make
					// stronger checks, such as that it has no analysis or glosses?
					DeleteWordformIfPossible(oldWordform);
				}
			}
		}

		private void DeleteWordformIfPossible(IWfiWordform oldWordform)
		{
			if (oldWordform.CanDelete)
				((ICmObjectInternal)oldWordform).DeleteObject();
		}

		/// <summary>
		/// Copy out IAnalysis objects from the end of this ISegment through the beginning of
		/// parameter's ISegment (inclusive). Also copies objects from intervening ISegments.
		/// Assumes parameter is in the same StTxtPara!
		/// </summary>
		/// <param name="point2"></param>
		/// <returns></returns>
		private IEnumerable<IAnalysis> CopyOutAnalysesFromMultipleSegments(AnalysisOccurrence point2)
		{
			// Need to copy out end of first segment, any segments in between and beginning of second segment
			var result = new List<IAnalysis>();
			var paraSegs = (Segment.Owner as IStTxtPara).SegmentsOS;
			if (paraSegs == null)
				throw new NullReferenceException("Unexpected error!");
			for (var i = Segment.IndexInOwner; i <= point2.Segment.IndexInOwner; i++)
			{
				if (i == Segment.IndexInOwner)
				{
					// Copy out end of this segment
					result.AddRange(paraSegs[i].AnalysesRS.ToList().GetRange(Index, Segment.AnalysesRS.Count - Index));
					continue;
				}
				if (i == point2.Segment.IndexInOwner)
				{
					// Copy out beginning of this segment
					result.AddRange(paraSegs[i].AnalysesRS.ToList().GetRange(0, point2.Index + 1));
					continue;
				}
				// Copy out all of this segment
				result.AddRange(paraSegs[i].AnalysesRS);
			}
			return result;
		}

		/// <summary>
		/// Returns a sequence of IAnalysis objects inclusive of the current occurrence
		/// and the end occurrence (otherAC).
		/// </summary>
		/// <param name="point2">An AnalysisOccurrence</param>
		public IEnumerable<IAnalysis> GetAdvancingWordformsInclusiveOf(AnalysisOccurrence point2)
		{
			return from occurrence in GetAdvancingOccurrencesInclusiveOf(point2)
				   where !(occurrence is IPunctuationForm)
				   select occurrence;
		}

		#region Segment/Paragraph jumping routines

		/// <summary>
		/// Returns the next ISegment in the sequence with the current one
		/// or null if there isn't one
		/// </summary>
		/// <param name="seg"></param>
		/// <returns></returns>
		private ISegment GetNextSegment(ISegment seg)
		{
			var para = seg.Owner as IStTxtPara;
			var iseg = seg.IndexInOwner;
			if (para == null || para.SegmentsOS == null)
				return null;
			if (iseg == para.SegmentsOS.Count - 1)
			{
				// Go to next paragraph (if there is one)
				// return null if there isn't a next paragraph
				// Found at least one case where one iteration of GetNextParagraph
				// wasn't enough! One paragraph had 0 segments! Thus added do-while.
				IStTxtPara nextPara;
				do
				{
					nextPara = GetNextParagraph(para) as IStTxtPara;
					if (nextPara == null || nextPara.SegmentsOS == null)
						return null;
					para = nextPara;
				} while (nextPara.SegmentsOS.Count == 0);
				return nextPara.SegmentsOS[0];
			}
			return para.SegmentsOS[iseg + 1];
		}

		/// <summary>
		/// Returns the previous ISegment in the sequence with the current one
		/// or null if there isn't one
		/// </summary>
		/// <param name="seg"></param>
		/// <returns></returns>
		private ISegment GetPreviousSegment(ISegment seg)
		{
			var iseg = seg.IndexInOwner;
			var para = seg.Owner as IStTxtPara;
			if (para == null || para.SegmentsOS == null)
				return null;
			if (iseg == 0)
			{
				// Go to previous paragraph (if there is one)
				// return null if there isn't a previous one
				// Found at least one case where one iteration of GetNextParagraph
				// wasn't enough! One paragraph had 0 segments! Thus added do-while to several methods
				IStTxtPara prevPara;
				do
				{
					prevPara = GetPreviousParagraph(para) as IStTxtPara;
					if (prevPara == null || prevPara.SegmentsOS == null)
						return null;
					para = prevPara;
				} while (prevPara.SegmentsOS.Count == 0);
				return prevPara.SegmentsOS[prevPara.SegmentsOS.Count - 1];
			}
			return para.SegmentsOS[iseg - 1];
		}

		/// <summary>
		/// Returns the next IStPara in the sequence with the current one
		/// or null if there isn't one
		/// </summary>
		/// <param name="para"></param>
		/// <returns></returns>
		private static IStPara GetNextParagraph(IStPara para)
		{
			var text = para.Owner as IStText;
			var ipara = para.IndexInOwner;
			if (text == null || text.ParagraphsOS == null)
				return null;
			if (ipara == text.ParagraphsOS.Count - 1)
				return null;

			return text.ParagraphsOS[ipara + 1];
		}

		/// <summary>
		/// Returns the previous IStPara in the sequence with the current one
		/// or null if there isn't one
		/// </summary>
		/// <param name="para"></param>
		/// <returns></returns>
		private static IStPara GetPreviousParagraph(IStPara para)
		{
			var ipara = para.IndexInOwner;
			var text = para.Owner as IStText;
			if (text == null || text.ParagraphsOS == null)
				return null;
			if (ipara == 0)
				return null;

			return text.ParagraphsOS[ipara - 1];
		}

		#endregion

		/// <summary>
		/// Returns an AnalysisOccurrence referencing the next IAnalysis object in the current
		/// sequence, or null if this is the last one in the text.
		/// </summary>
		public AnalysisOccurrence NextAnalysisOccurrence()
		{
			if (Index == Segment.AnalysesRS.Count - 1)
			{
				// Go to the next Segment
				// Adding do-while in case of a segment without Analyses between segments with them.
				ISegment nextSeg;
				ISegment currSeg = Segment;
				do
				{
					nextSeg = GetNextSegment(currSeg);
					if (nextSeg == null || nextSeg.AnalysesRS == null)
						return null;
					currSeg = nextSeg;
				} while (nextSeg.AnalysesRS.Count == 0);
				return new AnalysisOccurrence(nextSeg, 0);
			}
			if(Index + 1 < Segment.AnalysesRS.Count)
				return new AnalysisOccurrence(Segment, Index + 1);
			return null;
		}

		/// <summary>
		/// Returns an AnalysisOccurrence referencing the next IAnalysis object that has a wordform
		/// in the current sequence, or null if this is the last one in the text.
		/// </summary>
		/// <returns></returns>
		public AnalysisOccurrence NextWordform()
		{
			var result = NextAnalysisOccurrence();
			if (result == null)
				return null;
			return result.HasWordform ? result : result.NextWordform();
		}


		/// <summary>
		/// Returns an AnalysisOccurrence referencing the previous IAnalysis object in the current
		/// sequence, or null if this is the first one in the text.
		/// </summary>
		/// <returns></returns>
		public AnalysisOccurrence PreviousAnalysisOccurrence()
		{
			if (Index == 0)
			{
				// Go back to the previous Segment
				// Adding do-while in case of a segment without Analyses between segments with them.
				ISegment prevSeg;
				ISegment currSeg = Segment;
				do
				{
					prevSeg = GetPreviousSegment(currSeg);
					if (prevSeg == null || prevSeg.AnalysesRS == null)
						return null;
					currSeg = prevSeg;
				} while (prevSeg.AnalysesRS.Count == 0);
				return new AnalysisOccurrence(prevSeg, prevSeg.AnalysesRS.Count - 1);
			}
			return new AnalysisOccurrence(Segment, Index - 1);
		}

		/// <summary>
		/// Returns an AnalysisOccurrence referencing the previous IAnalysis object that has a wordform
		/// in the current sequence, or null if this is the first one in the text.
		/// </summary>
		/// <returns></returns>
		public AnalysisOccurrence PreviousWordform()
		{
			var result = PreviousAnalysisOccurrence();
			if (result == null)
				return null;
			return result.HasWordform ? result : result.PreviousWordform();
		}

		/// <summary>
		/// Returns the BeginOffset within the paragraph of this AnalysisOccurrence.
		/// Returns -1 if this point is not valid.
		/// </summary>
		/// <returns></returns>
		public virtual int GetMyBeginOffsetInPara()
		{
			if (!IsValid)
				return -1;
			return Segment.GetAnalysisBeginOffset(Index);
		}

		/// <summary>
		/// Answer the end of the occurrence in the underlying paragraph.
		/// </summary>
		/// <returns>-1 if not a valid analysis, otherwise the end index</returns>
		public virtual int GetMyEndOffsetInPara()
		{
			if (BaselineWs == -1) return -1; // happens with empty translation lines
			return GetMyBeginOffsetInPara() + Analysis.GetForm(BaselineWs).Length;
		}

		/// <summary>
		/// Set where it starts, relative to the paragraph.
		/// </summary>
		public virtual void SetMyBeginOffsetInPara(int begin)
		{
			if (IsValid)
			{
			}
		}
		/// <summary>
		/// Set where it ends, relative to the paragraph.
		/// </summary>
		public virtual void SetMyEndOffsetInPara(int end)
		{
			if (IsValid)
			{
			}
		}


		/// <summary>
		/// The reference typically used to display an occurrence in a concordance.
		/// </summary>
		public ITsString Reference
		{
			get
			{
				var para = Paragraph;
				if (para == null)
					return null; // paranoia; has been known to happen when deleting things, e.g., in respelling (FWR-3088).
				return para.Reference(Segment, GetMyBeginOffsetInPara());
			}
		}

		/// <summary>
		/// Tests to see if this AnalysisOccurrence is later in the text than the
		/// parameter.
		/// </summary>
		/// <param name="otherPoint"></param>
		/// <returns></returns>
		public bool IsAfter(AnalysisOccurrence otherPoint)
		{
			if(Paragraph.Owner.Hvo != otherPoint.Paragraph.Owner.Hvo)
				throw new ArgumentException("The two points are not from the same text!");
			var imyPara = Paragraph.IndexInOwner;
			var iotherPara = otherPoint.Paragraph.IndexInOwner;
			if (imyPara > iotherPara)
				return true;
			if (imyPara < iotherPara)
				return false;
			var imySeg = Segment.IndexInOwner;
			var iotherSeg = otherPoint.Segment.IndexInOwner;
			if (imySeg > iotherSeg)
				return true;
			if (imySeg < iotherSeg)
				return false;
			var iother = otherPoint.Index;
			return Index > iother;
		}
	}

	/// <summary>
	/// Interface implemented by classes used as paragraph fragments in concordance.
	/// </summary>
	public interface IParaFragment
	{
		/// <summary>
		/// Get the offset relative to the paragraph where it occurs.
		/// </summary>
		/// <returns></returns>
		int GetMyBeginOffsetInPara();
		/// <summary>
		/// Get the offset relative to the paragraph where it ends.
		/// </summary>
		int GetMyEndOffsetInPara();
		/// <summary>
		/// Set the offset relative to the paragraph where it occurs.
		/// </summary>
		void SetMyBeginOffsetInPara(int begin);
		/// <summary>
		/// Set the offset relative to the paragraph where it ends.
		/// </summary>
		void SetMyEndOffsetInPara(int end);

		/// <summary>
		/// The segment in which the occurrence is found.
		/// </summary>
		ISegment Segment { get; }

		/// <summary>
		/// Some sort of reference indicating the source of the fragment (typically a scripture CV ref, or
		/// text abbreviation plus para/segment index).
		/// </summary>
		ITsString Reference { get; }

		/// <summary>
		/// The paragraph to which the segment belongs.
		/// </summary>
		IStTxtPara Paragraph { get; }

		/// <summary>
		/// The object that has the text property in which the occurrence happens.
		/// This is usually the Paragraph but occasionally (see CaptionParaFragment) it is a picture caption.
		/// </summary>
		ICmObject TextObject { get; }

		/// <summary>
		/// The field in which the occurrene happens. This is usually StTxtParaTags.kflidContents
		/// but in CaptionParaFragment it is CmPictureTags.kflidCaption.
		/// </summary>
		int TextFlid { get; }

		/// <summary>
		/// Answer true if this is a valid paragraph fragment for the current state of things.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// The analysis itself, which this is an occurrence of, if any (otherwise null).
		/// </summary>
		IAnalysis Analysis { get; }

		/// <summary>
		/// The AnalysisOccurrence that best represents where the fragment is in the segment.
		/// May be the same object, if it IS an AnalysisOccurrence.
		/// May be null, if the paragraph hasn't been analyzed.
		/// </summary>
		AnalysisOccurrence BestOccurrence { get; }
	}

	/// <summary>
	/// This variation is initialized with its begin offset, which it can therefore return more efficiently.
	/// </summary>
	public class LocatedAnalysisOccurrence: AnalysisOccurrence, IParaFragment
	{
		int BeginOffset { get; set; }
		int EndOffset { get; set; }

		/// <summary>
		/// Make one.
		/// </summary>
		public LocatedAnalysisOccurrence(ISegment seg, int index, int beginOffset) : base(seg, index)
		{
			BeginOffset = beginOffset;
			EndOffset = beginOffset + Analysis.GetForm(BaselineWs).Length;
		}

		/// <summary>
		/// Same as base, but faster (and settable).
		/// </summary>
		/// <returns></returns>
		public override int GetMyBeginOffsetInPara()
		{
			return BeginOffset;
		}

		/// <summary>
		/// Same as base, but faster (and settable).
		/// </summary>
		public override int GetMyEndOffsetInPara()
		{
			return EndOffset;
		}
		/// <summary>
		/// Same as base, but works.
		/// </summary>
		public override void SetMyBeginOffsetInPara(int begin)
		{
			BeginOffset = begin;
		}

		/// <summary>
		/// Same as base, but works.
		/// </summary>
		public override void SetMyEndOffsetInPara(int end)
		{
			EndOffset = end;
		}

		/// <summary>
		/// For this standard implementation the text is always in the Paragraph
		/// </summary>
		public ICmObject TextObject
		{
			get { return Paragraph; }
		}

		/// <summary>
		/// For this standard implementation the text is always in the Paragraph contents
		/// </summary>
		public int TextFlid
		{
			get { return StTxtParaTags.kflidContents; }
		}

		/// <summary>
		/// This class IS an AnalysisOccurrence, so there is no need to search for a nearby one.
		/// </summary>
		public AnalysisOccurrence BestOccurrence { get { return this;} }
	}

	/// <summary>
	/// This is a minimal implementation of IParaFragment used for arbitrary string fragments in a concordance.
	/// </summary>
	public class ParaFragment : IParaFragment
	{
		int BeginOffset { get; set; }
		int EndOffset { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for ParaFragment
		/// </summary>
		/// <param name="seg">The segment.</param>
		/// <param name="ichBegin">The character offset to the start of the fragment, relative
		/// to the start of the paragraph.</param>
		/// <param name="ichEnd">The character offset to the end (actually the limit) of the
		/// fragment, relative to the start of the paragraph.</param>
		/// <param name="analyis">The analyis.</param>
		/// ------------------------------------------------------------------------------------
		public ParaFragment(ISegment seg, int ichBegin, int ichEnd, IAnalysis analyis)
		{
			Segment = seg;
			BeginOffset = ichBegin;
			EndOffset = ichEnd;
			Analysis = analyis;
		}

		/// <summary>
		/// Where it starts, relative to the paragraph.
		/// </summary>
		public int GetMyBeginOffsetInPara()
		{
			return BeginOffset;
		}

		/// <summary>
		/// The "Lim" of the fragment, relative to the paragraph.
		/// </summary>
		public int GetMyEndOffsetInPara()
		{
			return EndOffset;
		}

		/// <summary>
		/// Set where it starts, relative to the paragraph.
		/// </summary>
		public void SetMyBeginOffsetInPara(int begin)
		{
			BeginOffset = begin;
		}

		/// <summary>
		/// Set where it ends, relative to the paragraph.
		/// </summary>
		public void SetMyEndOffsetInPara(int end)
		{
			EndOffset = end;
		}

		/// <summary>
		/// The Segment it belongs to, which ties it to a paragraph.
		/// </summary>
		public ISegment Segment
		{
			get; private set;
		}

		/// <summary>
		/// Where to find it.
		/// </summary>
		public ITsString Reference
		{
			get
			{
				if (Paragraph == null)
					return null; // an empty string in the appropriate ws would be better, but not possible.
				return Paragraph.Reference(Segment, BeginOffset);
			}
		}

		/// <summary>
		/// Shortcut for containing paragraph.
		/// </summary>
		public IStTxtPara Paragraph
		{
			get { return Segment.Paragraph; }
		}

		/// <summary>
		/// For this standard implementation the text is always in the Paragraph
		/// </summary>
		public ICmObject TextObject
		{
			get { return Paragraph; }
		}

		/// <summary>
		/// For this standard implementation the text is always in the Paragraph contents
		/// </summary>
		public int TextFlid
		{
			get { return StTxtParaTags.kflidContents; }
		}

		/// <summary>
		/// Some sort of check that this one makes sense.
		/// </summary>
		public bool IsValid
		{
			get
			{
				return (Segment != null && Segment.IsValidObject && EndOffset <= Paragraph.Contents.Length);
			}
		}

		/// <summary>
		/// The analysis itself, which this is an occurrence of, if any (otherwise null).
		/// </summary>
		public IAnalysis Analysis { get; private set; }

		/// <summary>
		/// Find the nearest occurrence to the desired location.
		/// </summary>
		public AnalysisOccurrence BestOccurrence
		{
			get
			{
				if (!IsValid)
					return null;
				bool fExact; // dummy
				return Segment.FindWagform(GetMyBeginOffsetInPara() - Segment.BeginOffset,
					GetMyEndOffsetInPara() - Segment.BeginOffset, out fExact);
			}
		}

	}
}
