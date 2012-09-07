using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary>
	/// The general scenario is that an StText is being edited and we wish to preserve as much as possible
	/// of the analysis: that is its segments, their translations and notes, and the analyses that have been
	/// assigned to the words of the paragraph. For example if the user makes a small change to one word we
	/// do not want to lose all the analysis that the user has already done on the rest of the paragraph.
	///
	/// 1) Any segment whose text is unaffected by edits should be unmodified in every other way, except that
	/// its begin offset should be adjusted if text has been inserted or deleted before it.
	///
	/// 2) If two segments are combined (eg. period removed): we want to concatenate their free translations,
	/// and keep all their notes as separate notes.
	///
	/// 3) If a segment is split into two segments: We will keep the free translations, literal translations and
	/// notes on the first new segment.
	/// Enhance JohnT: Would it be better to put them on both segments or on the longer one?
	///
	/// 4) If the text of a particular wordform has not changed then it should still have the same analysis.
	///
	/// 5) It is particularly important that if the paragraph has a valid analysis beforehand, then it still
	/// should after edits.
	///
	/// More specific notes on how translations and notes are handled:
	///
	/// a. translations and notes are discarded for any original segment that is completely destroyed, that is,
	/// it is entirely within the range of characters deleted.
	///
	/// b. translations and notes are preserved somewhere for any segment (there can be at most two) which
	/// overlaps the range deleted, that is, any segment which at least partly survives.
	///
	/// c. material from the first of the partly preserved segments goes to the first of the resulting new
	/// segments, and from the second (last) partly preserved segment goes to the last of the resulting new
	/// segments.
	/// e.g. if we delete the material between the slashes in the following five segments, and insert two
	/// complete sentences A and B, we get the following segments and free translations
	/// ----0---- ---1a---/---1b--- ----2---- --3a--/--3b-- ---4----
	///   TN0            TN1          TN2           TN3       TN4
	///
	/// ----0---- ---1a---/---A---    ----B----    --3b-- ---4----
	///   TN0            TN1                         TN3    TN4
	///
	/// d. If there is only one new segment and more than one partly surviving segment, we concatenate
	/// translations, and concatenate lists of notes (not the contents of the notes).
	///
	/// e. If all the characters in the paragraph 'changed' but actually remained identical then the users
	/// would prefer that the analysis stick around (LT-12403) so we'll pretend it didn't change for adjustment purposes.
	/// </summary>
	internal sealed class AnalysisAdjuster
	{
		#region Member variables
		private readonly IStTxtPara m_para;
		private readonly ITsString m_oldContents;
		private readonly ITsString m_newContents;
		private readonly TsStringDiffInfo m_paraDiffInfo;
		private int[] m_oldBeginOffsets; // Taken from actual old segments before we modify them.
		private int[] m_oldEndOffsets; // Taken from actual old segments before we modify them.
		private ParagraphAnalysisFinder m_wordFinder;
		/// <summary>
		/// The necessary begin offsets for the new paragraph contents (determined from the actual text).
		/// </summary>
		private int[] m_newBeginOffsets;

		//These indicate the range of segments modified in old and new versions of the paragraph.
		int m_iSegFirstModified;
		int m_iOldSegLastModified;
		int m_iNewSegLastModified;
		int m_iFirstDeletedSegment;
		//When segments need to be merged this is used to ensure the correct two segments are merged.
		int m_cSegsDeleted;

		private ITsString m_baseline; //This is the concatenated baseline text of the modified segments.
		private List<IAnalysis> m_oldAnalyses; // Analyses of all the modified segments.
		private List<IAnalysis> m_punctRemainingFromLastDeletedSegment;
		// This is set up to hold, for each token (wordform or punctuation form) that we need after
		// XXXX in m_baseline, the offset into m_baseline where it occurs and its text.
		private List<Tuple<int, ITsString>> m_trailingTokens;
		// A list of the count of analyses from m_newAnalyses that should go in each new modified segment.
		private List<int> m_newAnalysisGroups;
		private List<IAnalysis> m_newAnalyses;
		// index into m_oldAnalyses (and m_newAnalyses) of the first analyis that needs to be changed.
		// (strictly one more than the last that doesn't need to change; there may be none that do.)
		private int m_iFirstAnalysisToFix;
		/// <summary>
		/// TextTags and ConstChartWordGroups referencing the current paragraph before the edit.
		/// </summary>
		private Set<IAnalysisReference> m_oldParaRefs;
		private int m_cRemovedAnalyses; // The number of Analyses removed from the current paragraph.
		private int m_cAddedAnalyses; // The number of Analyses added to the current paragraph.

		private List<ISegment> m_segsToMove; // In HandleMoveDest, the segments to move entirely to the destination.
		// In HandleMoveDest, the analyses to move to the destination (from the incompletely moved seg).
		private List<IAnalysis> m_analysesToMove;
		private ISegment m_segPartlyMoved; // the sourceof m_analysesToMove, if any incomplete segment overlaps the move.
		private bool m_changedFontOnly;
		#endregion

		#region DebuggingUtility
		/// <summary>
		/// This method is available to help display information about how the adjustments are being calculated,
		/// it uses several other helper methods to fill out the debug display
		/// </summary>
		private void PrintDebugInfo()
		{
			Debug.Print("************Begin Offsets********************");
			if(m_oldContents != null)
			{
				Debug.Print("oldContents string:");
				PrintOffsetLines(m_oldContents.Text, m_oldBeginOffsets, m_oldEndOffsets);
			}
			if(m_newContents != null)
			{
				Debug.Print("newContents string:");
				PrintOffsetLines(m_newContents.Text, m_newBeginOffsets, null);
			}
			Debug.Print("**************End Offsets********************");

			Debug.Print("********Begin Analysis Info**************");
			Debug.Print("First modified segment index: {0}", m_iSegFirstModified);
			Debug.Print("Old last modified segment index: {0}", m_iOldSegLastModified);
			Debug.Print("Is punct Remaining: {0}", m_punctRemainingFromLastDeletedSegment == null);
			PrintTrailingTokens();
			Debug.Print("Old Analyses info:");
			PrintAnalysesInfo(m_oldAnalyses);
			Debug.Print("New Analyses info:");
			PrintAnalysesInfo(m_newAnalyses);
			Debug.Print("**********End Analysis Info**************");
		}

		private void PrintTrailingTokens()
		{
			Debug.Print("Trailing Tokens:");
			foreach (Tuple<int, ITsString> mTrailingToken in m_trailingTokens)
			{
				Debug.Print("\t{0}\t{1}", mTrailingToken.Item1, mTrailingToken.Item2.Text);
			}
		}

		private void PrintAnalysesInfo(List<IAnalysis> analyses)
		{
			if (analyses != null && analyses.Count > 0)
			{
				foreach (IAnalysis mOldAnalysis in analyses)
				{
					if (mOldAnalysis.Analysis != null)
					{
						Debug.Print(mOldAnalysis.Analysis.ShortName + ":" + ((IWfiAnalysis)mOldAnalysis.Analysis).GetForm(mOldAnalysis.Analysis.Cache.DefaultVernWs).Text);
					}
					else if (mOldAnalysis.Wordform != null)
					{
						Debug.Print("WfiWordForm:" + mOldAnalysis.Wordform.Form.UserDefaultWritingSystem.Text + " or " + mOldAnalysis.Wordform.Form.RawUserDefaultWritingSystem.Text);
					}
					else
					{
						Debug.Print(mOldAnalysis.ShortName + ":" + ((IPunctuationForm)mOldAnalysis).Form.Text);
					}
				}
			}
		}

		private static void PrintOffsetLines(string text, int[] begin, int[] end)
		{
			Debug.Print(text);
			PrintOneOffsetLine(text, begin, ": begin offsets");
			if(end != null)
				PrintOneOffsetLine(text, end, ": end offsets");
		}

		private static void PrintOneOffsetLine(string text, int[] offsetArray, string beginOffsets)
		{
			string offsets = "";
			int lastOffset = 0;
			for (int oo = 0; oo < offsetArray.Length; ++oo)
			{
				for (int ooo = lastOffset; ooo < offsetArray[oo]; ++ooo)
				{
					offsets += " ";
				}
				lastOffset = offsetArray[oo] - (oo - 1);
				offsets += offsetArray[oo];
			}
			offsets += beginOffsets;
			Debug.Print(offsets);
		}

		#endregion
		#region Constructor
		/// <summary>
		/// Make one for adjusting the specified paragraph to its current contents from the specified
		/// old contents
		/// </summary>
		private AnalysisAdjuster(IStTxtPara para, ITsString oldContents, TsStringDiffInfo diffInfo)
		{
			m_para = para;
			m_oldContents = oldContents;
			m_newContents = m_para.Contents;
			m_changedFontOnly = false;
			if(diffInfo != null && ( diffInfo.IchFirstDiff != 0 || diffInfo.CchDeleteFromOld != diffInfo.CchInsert ||
				m_oldContents == null || m_newContents == null || m_oldContents.Length == 0 ||
				!m_oldContents.GetChars(0, m_oldContents.Length).Equals(m_newContents.GetChars(0, m_newContents.Length))))
			{
				m_paraDiffInfo = diffInfo;
			}
			else
			{
				//We didn't really change it, maybe the ws changed, but all the characters are identical
				//let's let the user keep their analysis
				m_paraDiffInfo = new TsStringDiffInfo(0, 0, 0);
				m_changedFontOnly = true; // a flag to let later adjustment ops remember this
			}
		}
		#endregion

		#region Public methods
		/// <summary>
		/// This is the main entry point called by the setter on the StTxtPara.Contents
		/// </summary>
		public static void AdjustAnalysis(IStTxtPara para, ITsString oldContents)
		{
			int ichFirstDiff, cchInsert, cchDeleteFromOld;
			TsStringDiffInfo diffInfo = TsStringUtils.GetDiffsInTsStrings(oldContents, para.Contents);
			if (diffInfo != null)
				AdjustAnalysis(para, oldContents, diffInfo);
		}

		/// <summary>
		/// This is the main entry point called by the setter on the StTxtPara.Contents used when the
		/// GetDiffsInTsStrings is also needed by another method that is called.
		/// </summary>
		public static void AdjustAnalysis(IStTxtPara para, ITsString oldContents, TsStringDiffInfo diffInfo)
		{
			AnalysisAdjuster adjuster = new AnalysisAdjuster(para, oldContents, diffInfo);
			BackTranslationAndFreeTranslationUpdateHelper.Do(para, adjuster.AdjustAnalysisInternal);
		}

		/// <summary>
		/// This is a primary entry point for IStTxtPara.SetContentsForMoveDest. It is similar to the
		/// top-level AdjustAnalysis, except that we know exactly what was inserted where, AND,
		/// we know it came from another paragraph. If the source paragraph is analysed, we can transfer any relevant
		/// analysis.
		/// Enhance JohnT: Currently this only supports moving from the end of one paragraph to the end of another.
		/// This is sufficient for all current usages of the MoveString method which this supports.
		/// </summary>
		public static void HandleMoveDest(IStTxtPara destPara, ITsString oldContents, int ichInsert,
			int cchInsert, IStTxtPara sourcePara, int ichSource, bool fDestParaIsNew)
		{
			if (ichInsert != destPara.Contents.Length - cchInsert)
				throw new NotImplementedException("We do not yet support moving strings to destinations other than paragraph end.");
			if (ichSource + cchInsert != sourcePara.Contents.Length)
				throw new NotImplementedException("We do not yet support moving strings from sources other than paragraph end.");

			AnalysisAdjuster adjuster = new AnalysisAdjuster(destPara, oldContents, new TsStringDiffInfo(ichInsert));
			BackTranslationAndFreeTranslationUpdateHelper.Do(destPara, () =>
				adjuster.HandleMoveDest(cchInsert, sourcePara, ichSource, fDestParaIsNew));
		}
		#endregion

		#region Private methods
		/// <summary>
		/// This is the main workhorse for adjusting analyses.
		/// </summary>
		private void AdjustAnalysisInternal()
		{
			if (!ChangeCanAffectSegments())
				return;

			m_oldParaRefs = new Set<IAnalysisReference>();
			// Enhance: Someday we may need to deal with refs that cross paragraph boundaries?!
			// This adds TextTags that only reference this paragraph
			m_oldParaRefs.AddRange(m_para.GetTags());
			// This adds ConstChartWordGroups that only reference this paragraph
			m_oldParaRefs.AddRange(m_para.GetChartCellRefs());

			//At this point m_para.Contents.Text contains the modified text but the Segments[i].BeginOffset's
			//line up with the positions corresponding to the text before the edit.
			//We need to compute a separate set of old end offsets because Segment.EndOffset uses the current
			//paragraph length for the last segment.
			m_oldBeginOffsets = (from seg in m_para.SegmentsOS select seg.BeginOffset).ToArray();
			m_oldEndOffsets = new int[m_para.SegmentsOS.Count];
			for (int iSeg = 1; iSeg < m_para.SegmentsOS.Count; iSeg++)
			{
				m_oldEndOffsets[iSeg - 1] = m_oldBeginOffsets[iSeg];
			}
			if (m_oldEndOffsets.Length > 0 && m_oldContents != null) //There may be no old contents, if so m_oldEndOffsets will not be adjusted
				m_oldEndOffsets[m_oldEndOffsets.Length - 1] = m_oldContents.Length;

			if (m_paraDiffInfo.CchDeleteFromOld > 0)
				DiscardDeletedSegments();

			// If the length of the text has changed then the begin offsets of segments after the change need to be adjusted.
			// Note that the number of segments may change, and even a segment entirely after the change may end up being
			// merged with a previous one. However, the best guess we can make for the offset of a following segment, if it does
			// survive, is that it will be changed by the difference in length between what was inserted and what was deleted.
			// To fix FWR-1350, we also adjust the offset of any segment whose begin offset is exactly equal to the place where
			// the edit began. In some cases, this might be too agressive, but later code seems to fix it. See that issue for
			// additional interesting edge cases (some of which may not have unit tests yet).
			if (m_paraDiffInfo.CchInsert != m_paraDiffInfo.CchDeleteFromOld)
			{
				int delta = m_paraDiffInfo.CchInsert - m_paraDiffInfo.CchDeleteFromOld;
				foreach (Segment seg in m_para.SegmentsOS)
				{
					if (seg.BeginOffset >= m_paraDiffInfo.IchFirstDiff)
						seg.BeginOffset += delta;
				}
			}
			ComputeRequiredBeginOffsets();

			ComputeRangeOfModifiedSegments();
			DoTheAdjustment();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether the changes to process could change segments or their analyses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ChangeCanAffectSegments()
		{
			if (m_oldContents == null || m_newContents == null || m_newContents.Text != m_oldContents.Text)
				return true; // Normal text change (common case)

			TsRunInfo newRunInfo, oldRunInfo;
			ITsTextProps newRunProps, oldRunProps;
			for (int ich = 0; ich < m_newContents.Length;)
			{
				newRunProps = m_newContents.FetchRunInfoAt(ich, out newRunInfo);
				oldRunProps = m_oldContents.FetchRunInfoAt(ich, out oldRunInfo);
				if (SegmentBreaker.IsLabelStyle(newRunProps.Style()) != SegmentBreaker.IsLabelStyle(oldRunProps.Style()))
					return true; // Label text was removed or added
				if (newRunProps.ObjData() != oldRunProps.ObjData() || newRunProps.GetWs() != oldRunProps.GetWs())
					return true; // Footnote or writing system changed
				ich = Math.Min(newRunInfo.ichLim, oldRunInfo.ichLim);
			}
			return false;
		}

		/// <summary>
		/// Common to various ways of initializing the algorithm, this actually figures out what the new
		/// state of the paragraph analysis should be.
		/// </summary>
		private void DoTheAdjustment()
		{
			if (m_para.ParseIsCurrent && m_newBeginOffsets.Length > 0)
			{
				SaveAnalysesOfModifiedSegments();
				GetNewAnalysisGroups();
				m_oldParaRefs = TrimModifiedAnnotationSet(m_oldParaRefs);
			}

			AdjustSegments();
			CleanupSpuriousWordforms();
			// Despite our best efforts, the ParagraphParser may still be able to do a better job of getting the
			// paragraph ready to analyze. In particular, the changed text may contain character sequences which
			// could be analyzed as phrases, and the adjuster will not detect that. We should have gotten it
			// close enough so that there is no danger of anything being lost by the paragraph parser's work.
			// Before Aug 24, 2010, we had the following line here to force a reparse the next time we wanted
			// to work on the paragraph. This caused problems (see e.g. FWR-1832) because the reparse might
			// indeed create a phrase, changing the index of later wordforms with unpredictable effects.
			// We decided to parse for guessed phrases only when actually annotating a paragraph, and therefore
			// now force a reparse when doing that, even if the paragraph is already parsed.
			//m_para.ParseIsCurrent = false;

		}

		/// <summary>
		/// If the adjustment resulted in wordforms that are not worth keeping, get rid of them.
		/// A wordform is not worth keeping if it
		/// - has no analyses
		/// - has no incoming references
		/// - has a spelling status of unknown
		/// </summary>
		private void CleanupSpuriousWordforms()
		{
			for (int i = m_iFirstAnalysisToFix; i < m_iFirstAnalysisToFix + m_cRemovedAnalyses; i++)
			{
				var wf = m_oldAnalyses[i] as IWfiWordform;
				if (wf != null)
					wf.DeleteIfSpurious();
			}
		}

		/// <summary>
		/// Add any necessary new segments, or delete any unwanted ones.
		/// (Todo: salvage free translations etc from deleted ones).
		/// Assign analyses to segments if paragraph has them.
		/// </summary>
		private void AdjustSegments()
		{
			// Work out how many new segments we need and where to insert.
			// Since segments after the old last-modified variables must not change, and their new positions
			// must start at the new last-modified position, the number we need is the difference between the two,
			var cnewSeg = m_iNewSegLastModified - m_iOldSegLastModified;
			// The position to insert is trickier. We typically want to keep, if possible, the first and last
			// of the old modified segments as the first and last of the new ones, so as to keep their TN material
			// in the right place. Inserting after the first modified one usually achieves this.
			var iInsertAt = m_iSegFirstModified + 1;

			// However, suppose we inserted one or more complete segments, without actually changing any existing ones.
			// In that case, we want to insert before the first 'modified' segment. Also, if the first 'modified'
			// segment started at or after the change, it isn't really modified, so we want to insert before it.
			int oldIndexOfFirstModifiedSegment = (m_iSegFirstModified >= m_iFirstDeletedSegment) ?
				m_iSegFirstModified + m_cSegsDeleted : m_iSegFirstModified;
			if (oldIndexOfFirstModifiedSegment < m_oldBeginOffsets.Length && m_oldBeginOffsets[oldIndexOfFirstModifiedSegment] >=
				m_paraDiffInfo.IchFirstDiff + m_paraDiffInfo.CchDeleteFromOld)
			{
				iInsertAt--;
			}
			else if (ChangedToLabelAtBeginSegment())
				iInsertAt--;

			iInsertAt = Math.Min(iInsertAt, m_para.SegmentsOS.Count);
			// Make new segments (zero iterations if new number is <= old number)
			for (var i = iInsertAt; i < iInsertAt + cnewSeg; i++)
			{
				m_para.SegmentsOS.Insert(i, m_para.Services.GetInstance<ISegmentFactory>().Create());
			}

			// Since we already deleted any segments that are entirely within the deleted range,
			// if we still have too many segments to match the new text, we need to merge
			// the two that at least partly survive, keeping their TN material.
			// There should be at most two that partly survive, since one continuous range
			// is considered to be deleted.
			if (m_newBeginOffsets.Length < m_oldBeginOffsets.Length - m_cSegsDeleted)
			{
				Debug.Assert(m_newBeginOffsets.Length + 1 == m_oldBeginOffsets.Length - m_cSegsDeleted,
					"When we are merging segments we should have already deleted any segments between these two.");
				ConcatenateTranslationsAndNotes(m_para.SegmentsOS[m_iSegFirstModified],
												m_para.SegmentsOS[m_iSegFirstModified + 1]);
				m_para.SegmentsOS.Replace(m_iSegFirstModified + 1, 1, new ISegment[0]);
			}

			if (m_para.SegmentsOS.Count == 0)
				return; // can happen if the paragraph we are moving from has no segments.

			// Any modified segments need their BeginOffsets set to the correct values.
			for (var i = m_iSegFirstModified; i <= m_iNewSegLastModified; i++)
				((Segment)m_para.SegmentsOS[i]).BeginOffset = m_newBeginOffsets[i];

			if (m_para.ParseIsCurrent)
			{
				// Assign analysis groups previously figured to the segments
				int ianalysis = 0; // index into m_newAnalyses
				int igroup = 0; // index into m_newAnalysisGroups
				for (int i = m_iSegFirstModified; i <= m_iNewSegLastModified; i++)
				{
					var seg = m_para.SegmentsOS[i];
					int canalysis = 0;
					if (igroup < m_newAnalysisGroups.Count)
						canalysis = m_newAnalysisGroups[igroup];
					int cOldAnalyses = seg.AnalysesRS.Count;
					seg.AnalysesRS.Replace(0, seg.AnalysesRS.Count, m_newAnalyses.Skip(ianalysis).Take(canalysis).ToArray());
					if (i == m_iSegFirstModified && m_iNewSegLastModified < m_iOldSegLastModified)
						FixHangingReferences(m_iNewSegLastModified + 1, cOldAnalyses);
					UpdateAffectedReferences(seg, ianalysis, m_iSegFirstModified == i);
					ianalysis += canalysis;
					igroup++;
				}
			}
		}

		/// <summary>
		/// Checks changed text to determine if text at the beginning of a segment was changed to a label - will happen if
		/// a run of text is changed to verse number or chapter number style.
		/// </summary>
		private bool ChangedToLabelAtBeginSegment()
		{
			return m_oldContents != null &&
				m_oldBeginOffsets.Length > m_iSegFirstModified &&
				m_oldBeginOffsets[m_iSegFirstModified] == m_paraDiffInfo.IchFirstDiff &&
				m_paraDiffInfo.CchDeleteFromOld == m_paraDiffInfo.CchInsert &&
				!SegmentBreaker.HasLabelText(m_oldContents, m_paraDiffInfo.IchFirstDiff, m_paraDiffInfo.CchDeleteFromOld) &&
				SegmentBreaker.HasLabelText(m_newContents, m_paraDiffInfo.IchFirstDiff,
					m_paraDiffInfo.IchFirstDiff + m_paraDiffInfo.CchInsert);
		}

		/// <summary>
		/// Update any IAnalysisReference objects pointing to the just modified Segment.
		/// </summary>
		/// <param name="seg">current Segment</param>
		/// <param name="ianalysis">first analysis in this Segment(relative to the Paragraph)</param>
		/// <param name="ffirstSeg">true if seg is the first one modified</param>
		private void UpdateAffectedReferences(ISegment seg, int ianalysis, bool ffirstSeg)
		{
			var ifirstAnalysisInSegToFix = 0;
			if (ffirstSeg)
				ifirstAnalysisInSegToFix = m_iFirstAnalysisToFix - ianalysis;
			var refsToDelete = new List<IAnalysisReference>();
			// use m_cRemovedAnalyses and m_cAddedAnalyses
			// to process each IAnalysisReference
			// N.B. I (Gordon) tried calculating a delta analyses from the difference of the two
			// above variables, but got confused trying to sort out the edge cases. I believe this works.
			foreach (var iar in m_oldParaRefs)
			{
				var endSeg = iar.EndRef().Segment;
				var iend = iar.EndRef().Index;
				if (endSeg == seg && iend < ifirstAnalysisInSegToFix)
					continue; // The whole IAnalysisReference is before the change, so no change.
				var begSeg = iar.BegRef().Segment;
				var ibeg = iar.BegRef().Index;

				// Reset flags
				var fbegIndexMoved = false;
				var fbegExact = true;
				var fendExact = true;

				// Delete section
				if (m_cRemovedAnalyses > 0)
				{
					// BegRef section
					if (begSeg == seg && ibeg >= ifirstAnalysisInSegToFix)
					{
						if(CheckForDeleteableIAR(seg, iar, ifirstAnalysisInSegToFix, m_cRemovedAnalyses, m_cAddedAnalyses))
						{
							refsToDelete.Add(iar);
							continue;
						}
						if (ibeg >= ifirstAnalysisInSegToFix + m_cRemovedAnalyses)
						{
							ibeg -= m_cRemovedAnalyses;
							iar.ChangeToDifferentIndex(ibeg, true, false);
							fbegIndexMoved = true;
						}
						else
						{
							fbegExact = false;
							if (ibeg > ifirstAnalysisInSegToFix)
							{
								fbegIndexMoved = true;
								iar.ChangeToDifferentIndex(ifirstAnalysisInSegToFix, true, false);
								ibeg = ifirstAnalysisInSegToFix;
							}
						}
					}
					// EndRef section
					if (endSeg == seg)
					{
						if (iend >= ifirstAnalysisInSegToFix + m_cRemovedAnalyses)
						{
							iend -= m_cRemovedAnalyses;
							iar.ChangeToDifferentIndex(iend, false, true);
						}
						else
						{
							fendExact = false;
							if (iend > ifirstAnalysisInSegToFix)
							{
								iend = ifirstAnalysisInSegToFix;
								iar.ChangeToDifferentIndex(iend, false, true);
							}
						}
					}
				}
				// Insert section
				if (m_cAddedAnalyses > 0)
				{
					if (begSeg == seg && ibeg >= ifirstAnalysisInSegToFix)
					{
						if (m_cRemovedAnalyses == 0 || fbegIndexMoved)
						{
							ibeg += m_cAddedAnalyses;
							iar.ChangeToDifferentIndex(ibeg, true, false);
						}
					}
					if (endSeg == seg)
					{
						iend += m_cAddedAnalyses;
						iar.ChangeToDifferentIndex(iend, false, true);
					}
				}
				// Cleanup Section for cases where we're not sure we're pointing at an ocurrence.
				if (!fbegExact)
				{
					// This actually works even if there isn't a previous point.
					// One step backward and possibly multiple steps forward if we put in punctuation
					var actualPoint = new AnalysisOccurrence(seg, ibeg - 1).NextWordform();
					if (actualPoint == null)
					{
						refsToDelete.Add(iar); // There IS no next wordform in this text!
						continue;
					}
					ibeg = actualPoint.Index;
					iar.ChangeToDifferentIndex(ibeg, true, false);
					if (actualPoint.Segment != seg)
						iar.ChangeToDifferentSegment(actualPoint.Segment, true, false);
				}
				if (!fendExact)
				{
					// This actually works even if there isn't a point at the current Index.
					// One step forward and possibly multiple steps backward if we put in punctuation
					var actualPoint = new AnalysisOccurrence(seg, iend).PreviousWordform();
					if (actualPoint == null)
					{
						refsToDelete.Add(iar); // There IS no previous wordform in this text!
						continue;
					}
					iend = actualPoint.Index;
					iar.ChangeToDifferentIndex(iend, false, true);
					if (actualPoint.Segment != seg)
						iar.ChangeToDifferentSegment(actualPoint.Segment, false, true);
				}
			}
			// Delete unneeded IAnalysisReference objects
			if (refsToDelete.Count > 0)
				DeleteReferences(refsToDelete);
		}

		/// <summary>
		/// Delete any IAnalysisReference objects whose begin and end references fall within
		/// the range of indices removed and not added back in.
		/// Example: Old text: the big red pickup truck
		///          New text: the VW
		///  Index in Segment:  0   1   2   3      4
		/// This method will delete any objects whose begin AND end references fall within
		/// the range 2 to 4 (inclusive). In this example, ifirstAnalysisInSegToFix would be 1.
		/// </summary>
		/// <param name="seg"></param>
		/// <param name="iar"></param>
		/// <param name="ifirstAnalysisInSegToFix"></param>
		/// <param name="cRemovedAnalyses"></param>
		/// <param name="cAddedAnalyses"></param>
		/// <returns></returns>
		private static bool CheckForDeleteableIAR(ISegment seg, IAnalysisReference iar,
			int ifirstAnalysisInSegToFix, int cRemovedAnalyses, int cAddedAnalyses)
		{
			if (cAddedAnalyses >= cRemovedAnalyses)
				return false; // In order to qualify we need more bits deleted than added.
			if (iar.BegRef().Segment != seg || iar.EndRef().Segment != seg)
				return false; // One of our endpoints isn't even in this Segment!
			var imax = ifirstAnalysisInSegToFix + cRemovedAnalyses - 1;
			var imin = ifirstAnalysisInSegToFix + cAddedAnalyses;
			var ibeg = iar.BegRef().Index;
			var iend = iar.EndRef().Index;
			if (ibeg < imin || ibeg > imax || iend < imin || iend > imax)
				return false;
			return true; // Delete this feller!
		}

		private void DeleteReferences(IEnumerable<IAnalysisReference> iarList)
		{
			foreach (var iar in iarList)
			{
				if (iar is ITextTag)
				{
					var owner = iar.Owner as IStText;
					owner.TagsOC.Remove(iar as ITextTag);
				}
				else
				{
					var owner = iar.Owner as IConstChartRow;
					owner.CellsOS.Remove(iar as IConstituentChartCellPart);
				}
				m_oldParaRefs.Remove(iar);
			}
		}

		private static void ConcatenateTranslationsAndNotes(ISegment destination, ISegment source)
		{
			destination.FreeTranslation.AppendAlternatives(source.FreeTranslation, true);
			destination.LiteralTranslation.AppendAlternatives(source.LiteralTranslation, true);
			destination.NotesOS.Replace(destination.NotesOS.Count, 0, source.NotesOS.ToArray());
		}

		/// <summary>
		/// Make a first pass in which we delete any segment entirely contained in the deleted text.
		/// </summary>
		private void DiscardDeletedSegments()
		{
			int firstDeleted = -1; //marks the index into the paragraph(m_para) segments of the first segment which is deleted
			m_cSegsDeleted = 0;
			int iSeg = 0;
			int iJustPastEditInOldContents = m_paraDiffInfo.IchFirstDiff + m_paraDiffInfo.CchDeleteFromOld;
			foreach (ISegment seg in m_para.SegmentsOS)
			{
				// segment boundaries are within the portion of text deleted.
				if (seg.BeginOffset >= m_paraDiffInfo.IchFirstDiff && m_oldEndOffsets[iSeg] <= iJustPastEditInOldContents)
				{
					//delete this segment
					if (m_cSegsDeleted == 0)
						firstDeleted = iSeg;
					m_cSegsDeleted++;
					DiscardDeletedRefs(seg); // Discard any IAnalysisReferences wholly contained in deleted text
				}
				else if (seg.BeginOffset >= m_paraDiffInfo.IchFirstDiff &&
						 seg.BeginOffset < iJustPastEditInOldContents &&
						 m_oldEndOffsets[iSeg] >= iJustPastEditInOldContents)
				{
					//The seg starts in the range of characters deleted. The segment extends past this range of
					//characters.
					//However, if the portion of the segment within the deletion area contains an EOS character
					//then delete the segment
					//and
					//then check the portion after the deleted portion
					//If this portion contains any nonWhite characters
					//then they should be part of a punctuation analysis char of the previous segment
					//If the portion remaining contains an EndOfSentence character
					//then do not delete the segment

					//If there is only WhiteSpace characters in the part of this segment that is outside of the
					//deleted text then all non-WhiteSpace characters in the segment is deleted so
					//delete the segment.
					var textPastEOS = m_oldContents.Substring(iJustPastEditInOldContents,
															m_oldEndOffsets[iSeg] - iJustPastEditInOldContents);
					var tsStrBldr = textPastEOS.GetBldr();
					tsStrBldr.Replace(0, textPastEOS.Length, textPastEOS.Text.Trim(), null); //remove all whiteSpace
					var punctTsStr = tsStrBldr.GetString();

					if (punctTsStr.Length == 0)
					{
						if (m_cSegsDeleted == 0)
							firstDeleted = iSeg;
						m_cSegsDeleted++;
						DiscardDeletedRefs(seg); // Discard any IAnalysisReferences wholly contained in deleted text
					}
					else
					{
						var segBaseline = m_oldContents.Substring(seg.BeginOffset, m_oldEndOffsets[iSeg] - seg.BeginOffset);
						var collectorSeg = new SegmentCollector(segBaseline, m_para.Cache.WritingSystemFactory);
						//Note: this will not choose TE footnote makers for EOSPositions. TsStringUtils.kChObject
						collectorSeg.Run();
						int iInSegOfEndOfDeletion = iJustPastEditInOldContents - seg.BeginOffset - 1;

						//if (collectorSeg.EosPositions.Count == 0)
						//there are no seg break characters which means we are examining the
						//last segment of a paragraph and this segment has no segment break character
						//if (collectorSeg.EosPositions[0] > iInSegOfEndOfDeletion
						//then do not delete the segment. It could be merged or kept.
						if (collectorSeg.EosPositions.Count > 0 &&
							collectorSeg.EosPositions[0] <= iInSegOfEndOfDeletion)
						{
							//delete this segment
							if (m_cSegsDeleted == 0)
								firstDeleted = iSeg;
							m_cSegsDeleted++;
							DiscardDeletedRefs(seg); // Discard any IAnalysisReferences wholly contained in deleted text

							//Before we delete the segment we need to deal with any remaining text past
							//the EOS character.
							//If there is any text in this segment past the segment break character
							//(i.e. any text past collectorSeg.EosPositions[0])
							//we need to add this as a PunctuationForm analysis to the merged segments.
							//For example the test where we have .) and ) is not deleted.  Therefore save this text
							//and when creating m_oldAnalyses we need to insert a PunctuationForm for ')' in this list.

							//var textPastEOS = segBaseline.Substring(collectorSeg.EosPositions[0] + 1, segBaseline.Length - collectorSeg.EosPositions[0] - 1);
							if (textPastEOS.Length > 0)
							{
								//var tsStrBldr = textPastEOS.GetBldr();
								//tsStrBldr.Replace(0, textPastEOS.Length, textPastEOS.Text.Trim(), null);
								//var punctTsStr = tsStrBldr.GetString();

								if (punctTsStr.Length > 0)
								{
									IPunctuationForm pf = WfiWordformServices.FindOrCreatePunctuationform(m_para.Cache, punctTsStr);
									m_punctRemainingFromLastDeletedSegment = new List<IAnalysis> {pf};
								}
							}
						}
					}
				}
				iSeg++;
			}
			if (m_cSegsDeleted == 0)
				return;

			m_iFirstDeletedSegment = firstDeleted;

			//removes all the segments which were completely within the deleted range of characters from the paragraph
			m_para.SegmentsOS.Replace(m_iFirstDeletedSegment, m_cSegsDeleted, new ICmObject[0]);

			// Fix any tags that just lost their Begin or End Segment (won't be both)
			// after removing the segments above firstDeleted now points to the first segment following the deleted range.
			FixSingleHangingReference(firstDeleted);
		}

		private void FixSingleHangingReference(int firstSurvivor)
		{
			FixHangingReferences(firstSurvivor, -1);
		}

		/// <summary>
		/// Fix a reference where one (or both of the end points' Segments just got deleted
		/// </summary>
		/// <param name="iFirstSegmentSurviving">This is the index of the first surviving segment following a change.</param>
		/// <param name="cOldAnalyses">This is typically the count of analyses that the first modified segment used to have
		/// before the adjustment. Another caller passes -1, when previous segments are being deleted entirely (but not yet modified).</param>
		private void FixHangingReferences(int iFirstSegmentSurviving, int cOldAnalyses)
		{
			ISegment newSeg;
			int cNewAnalyses;
			foreach (var iar in m_oldParaRefs)
			{
				if (iar.IsValidRef)
					continue; // not a hanging reference

				if (!iar.BegRef().IsValid && !iar.EndRef().IsValid)
				{
					// Both end points need updating. A Segment break disappeared!?
					if (cOldAnalyses <= 0)
						Debug.Assert(false, "Shouldn't be able to happen!");

					// Try to put it on the current segment
					var iseg = Math.Max(0, iFirstSegmentSurviving - 1);
					newSeg = m_para.SegmentsOS[iseg]; // Get segment before deleted one or first one
					iar.ChangeToDifferentSegment(newSeg, true, true); // put both end points in new Segment
					cNewAnalyses = newSeg.AnalysesRS.Count;
					if (cNewAnalyses > cOldAnalyses) // confirms theory of segment break disappearing!
					{
						// This is not supposed to happen. LT-11049 documents a very complex sequence of actions
						// which can bring it about. It's no good 'correcting' the index to something still invalid,
						// so if that is what we're about to do, we delete it instead, as unrepairable.
						// Eventually we may figure out something better to do.
						if(iar.BegRef().Index + cOldAnalyses < 0 || iar.BegRef().Index + cOldAnalyses < 0)
						{
							DiscardDeletedRefs(newSeg); //It should be gone, we can't fix it, get rid of it.
							continue;
						}
						//maybe we can fix it, try and change the indices.
						iar.ChangeToDifferentIndex(iar.BegRef().Index + cOldAnalyses, true, false);
						iar.ChangeToDifferentIndex(iar.EndRef().Index + cOldAnalyses, false, true);
					}
					else
						Debug.Assert(false, "Gordon needs to figure out what's going on!");
					continue;
				}

				// This now becomes FixSingleHangingReference, either Begin or End point is invalid (not both)
				if (!iar.BegRef().IsValid)
				{
					if (cOldAnalyses > 0 && iFirstSegmentSurviving > 0)
					{
						// segment break deleted, try to preserve position in previous segment
						newSeg = m_para.SegmentsOS[iFirstSegmentSurviving - 1];
						iar.ChangeToDifferentSegment(newSeg, true, false);
						iar.ChangeToDifferentIndex(iar.BegRef().Index + cOldAnalyses, true, false);
					}
					else
					{
						// try to push BegSegment forward
						newSeg = m_para.SegmentsOS[iFirstSegmentSurviving]; // Get next Segment after deleted one
						iar.ChangeToDifferentSegment(newSeg, true, false);
						// Start with an invalid AnalysisOccurrence, but apply NextWordform(). It works!
						var newBegPoint = new AnalysisOccurrence(newSeg, -1).NextWordform();
						iar.ChangeToDifferentIndex(newBegPoint.Index, true, false);
					}
					continue;
				}

				// EndReference is invalid, but BeginRef was good. Try to pull EndSegment backward
				if (iFirstSegmentSurviving == 0)
				{
					// TODO: This reference needs to go to previous paragraph!?
					// Can't repair this reference. Delete it?!
					Debug.Assert(false, "Reached an error condition.");
				}
				else
				{
					newSeg = m_para.SegmentsOS[iFirstSegmentSurviving - 1]; // Get segment before deleted one
					cNewAnalyses = newSeg.AnalysesRS.Count;
					iar.ChangeToDifferentSegment(m_para.SegmentsOS[iFirstSegmentSurviving - 1], false, true);
					if(cOldAnalyses > 0 && cNewAnalyses > cOldAnalyses)
					{
						// segment break deleted, try to preserve position in new segment
						iar.ChangeToDifferentIndex(iar.EndRef().Index + cOldAnalyses, false, true);
					}
					else
					{
						// Start with an invalid AnalysisOccurrence, but apply PreviousWordform(). It works!
						var newEndPoint = new AnalysisOccurrence(newSeg, newSeg.AnalysesRS.Count).PreviousWordform();
						iar.ChangeToDifferentIndex(newEndPoint.Index, false, true);
					}
				}
			} // Do we need to update m_oldParaTags? If so, we need to use for() instead of foreach()
		}

		/// <summary>
		/// Get rid of IAnalysisReferences that only reference a segment that is being deleted
		/// (i.e. reference end points are wholly contained within deleted text)
		/// </summary>
		/// <param name="seg"></param>
		private void DiscardDeletedRefs(ISegment seg)
		{
			var result = m_oldParaRefs.Where(
				iar => iar.BegRef().Segment == seg && iar.EndRef().Segment == seg).ToList();
			if (result.Count < 1)
				return;
			// This list is ICmObject since we use Replace() to remove all from one owner.
			var tagList = new List<ICmObject>();
			// This list is ConstituentChartCellParts since we don't know that they all
			// have the same owner.
			var cellList = new List<IConstituentChartCellPart>();
			foreach (var iar in result)
			{
				if (iar is ITextTag)
					tagList.Add(iar);
				else
					cellList.Add((IConstituentChartCellPart)iar);
				m_oldParaRefs.Remove(iar);
			}
			// Actually delete them from their appropriate owners
			if (tagList.Count > 0)
				DeleteTextTags(tagList);
			if (cellList.Count > 0)
				DeleteChartRefs(cellList);
		}

		private static void DeleteChartRefs(IEnumerable<IConstituentChartCellPart> cellList)
		{
			foreach (var cellPart in cellList)
				((IConstChartRow) cellPart.Owner).CellsOS.Remove(cellPart);
		}

		private void DeleteTextTags(IEnumerable<ICmObject> tagList)
		{
			((IStText)m_para.Owner).TagsOC.Replace(tagList, new ICmObject[0]);
		}

		/// <summary>
		/// Trim down set of AnalysisReference objects to only those referencing segments that are
		/// modified. (We aren't deleting them, just narrowing down the ones to possibly update.)
		/// NB.: Use this BEFORE modifying the old Segments on the paragraph!
		/// </summary>
		/// <param name="annObjectsToCheck"></param>
		/// <returns></returns>
		private Set<IAnalysisReference> TrimModifiedAnnotationSet(Set<IAnalysisReference> annObjectsToCheck)
		{
			if (annObjectsToCheck == null)
				return null; // none to remove.
			List<IAnalysisReference> result;
			result = annObjectsToCheck.Where(segmentIsOutsideOfRange).ToList();
			if (result.Count > 0)
				foreach (var iar in result)
					annObjectsToCheck.Remove(iar);
			return annObjectsToCheck;
		}

		private bool segmentIsOutsideOfRange(IAnalysisReference refObj)
		{
			ISegment seg = null;
			var endRef = refObj.EndRef();
			if (endRef != null) // added for LT-13414 - one segment had a null endRef
				seg = refObj.EndRef().Segment;
			return seg != null && (seg.IndexInOwner < m_iSegFirstModified || refObj.BegRef().Segment.IndexInOwner > m_iOldSegLastModified);
		}

		// Compute segment offsets as required for new paragraph contents.
		private void ComputeRequiredBeginOffsets()
		{
			List<TsStringSegment> segs = m_para.Contents.GetSegments(m_para.Cache.WritingSystemFactory);
			m_newBeginOffsets = (from seg in segs select seg.IchMin).ToArray();
		}

		// This routine is responsible to initialize the member variables m_iSegFirstModified, m_iOldSegLastModified,
		// and m_iNewSegLastModified, by identifying segments before and after the change whose positions and lengths
		// have not been affected.
		private void ComputeRangeOfModifiedSegments()
		{
			m_iSegFirstModified = 0;
			for (; m_iSegFirstModified < m_para.SegmentsOS.Count - 1; m_iSegFirstModified++)
			{
				//This segment must be involved in the change unless there is a following segment in the new paragraph.
				//There may be fewer segments in the new paragraph than in the old paragraph.
				if (m_iSegFirstModified >= m_newBeginOffsets.Length - 1)
					break;

				//If the old ending position of this segment extends  past the position of the first change then we have found the
				//firstModifiedSegment.
				var oldEndOfSeg = m_oldBeginOffsets[m_iSegFirstModified + 1];
				if (oldEndOfSeg > m_paraDiffInfo.IchFirstDiff)
					break;

				//If the new ending position of this segment extends beyond the change then we need to treat it as modified.
				var newEndofSeg = m_newBeginOffsets[m_iSegFirstModified + 1];
				if (newEndofSeg > m_paraDiffInfo.IchFirstDiff)
					break;
			}

			m_iOldSegLastModified = m_para.SegmentsOS.Count - 1;
			m_iNewSegLastModified = m_newBeginOffsets.Length - 1;
			for (; m_iOldSegLastModified > m_iSegFirstModified && m_iNewSegLastModified > m_iSegFirstModified;
				m_iOldSegLastModified--, m_iNewSegLastModified--)
			{
				if (m_newBeginOffsets[m_iNewSegLastModified] != m_oldBeginOffsets[m_iOldSegLastModified] +
					m_paraDiffInfo.CchInsert - m_paraDiffInfo.CchDeleteFromOld)
				{
					// If the number of characters before the segment's begin offset changed, but the
					// segment's offset didn't change accordingly, then something must have happened to move
					// this segment's offset relative to the location in the original text. (FWR-1903)
					break;
				}
			}
		}

		/// <summary>
		/// Remember the original analyses of segments we may modify.
		/// </summary>
		void SaveAnalysesOfModifiedSegments()
		{
			m_oldAnalyses = new List<IAnalysis>();
			for (int i = m_iSegFirstModified; i <= m_iOldSegLastModified; i++)
			{
				//add in any punctuation that occurs at the end of the last deleted segment.
				if (m_punctRemainingFromLastDeletedSegment != null &&
					i == m_iSegFirstModified + 1 &&
					m_punctRemainingFromLastDeletedSegment.Count > 0)
					m_oldAnalyses.AddRange(m_punctRemainingFromLastDeletedSegment);
				m_oldAnalyses.AddRange(m_para.SegmentsOS[i].AnalysesRS);
			}
		}

		/// <summary>
		/// Get the end of the specified segment from m_newBeginOffsets or the length of the paragraph if it is the last
		/// </summary>
		/// <param name="iseg"></param>
		/// <returns></returns>
		int EndOfNewSeg(int iseg)
		{
			if (iseg == m_newBeginOffsets.Length - 1)
				return m_para.Contents.Length;
			return m_newBeginOffsets[iseg + 1];
		}

		/// <summary>
		/// This loop reparses m_baseline, determining m_iFirstAnalysisToFix, initializing m_trailingTokens
		/// and setting up m_newAnalysisGroups (which gives new Segment boundaries) it also initializes m_newAnalyses with
		/// the contents of m_oldAnalyses
		/// </summary>
		void AnalyzeNewText()
		{
			// Beginning of diff relative to the baseline text we are re-analyzing.
			int ichFirstDiffBL = m_paraDiffInfo.IchFirstDiff - m_newBeginOffsets[m_iSegFirstModified];

			m_iFirstAnalysisToFix = 0;
			m_trailingTokens = new List<Tuple<int, ITsString>>();
			int canalysis = 0; // counts analyses that should go in the current segment group
			int isegment = m_iSegFirstModified;
			int ichLimSegBL = EndOfNewSeg(isegment) - m_newBeginOffsets[m_iSegFirstModified];
			m_newAnalysisGroups = new List<int>();
			m_newAnalyses = new List<IAnalysis>(m_oldAnalyses);
			while (m_wordFinder.Position < m_baseline.Length)
			{
				m_wordFinder.AdvanceToAnalysis();
				int ichStartWordBL = m_wordFinder.Position;
				m_wordFinder.AdvancePastWord(ichLimSegBL);
				int ichEndWordBL = m_wordFinder.Position;
				if (ichStartWordBL == m_baseline.Length)
					break;
				//Debug.Assert(startWord < endWord);
				if (ichStartWordBL >= ichLimSegBL)
				{
					isegment++;
					ichLimSegBL = EndOfNewSeg(isegment) - m_newBeginOffsets[m_iSegFirstModified];
					m_newAnalysisGroups.Add(canalysis);
					canalysis = 0;
					if (ichStartWordBL == ichEndWordBL)
						continue;
				}
				canalysis++;

				if (m_iFirstAnalysisToFix < m_oldAnalyses.Count && ichEndWordBL <= ichFirstDiffBL)
				{
					// We may possibly be able to increment m_iFirstAnalysisToFix, thus reusing one more
					// leading analysis, instead of adding the current wordform to m_trailingTokens.
					if (!m_wordFinder.IsWordforming(ichStartWordBL))
					{
						// Punctuation is relatively easy: don't have to worry about phrases, don't really care if we reuse.
						if (ichEndWordBL < ichFirstDiffBL)
						{
							// Definitely unaffected, leave with preceding tokens
							m_iFirstAnalysisToFix++;
							continue;
						}
						// otherwise, since we tested above that endWord <= ichFirstDiff, this bit of
						// punctuation comes right up to the start of the edit. We might be able to reuse it,
						// but it isn't worth trying to figure out whether we can; there won't be any real
						// words after it before the edit. So just go ahead and end the preceding tokens here.
					}
					else if (m_oldAnalyses[m_iFirstAnalysisToFix].Wordform != null)
					{
						// it's a wordform. We have to be careful here. There is (now!) a word break at endWord.
						// However, the next input analysis might be a phrase, and might extend into the edit.
						// Since it's before the edit, if the length matches we can go ahead and reuse it.
						// Even if it ends exactly at ichFirstDiff, since there's still a boundary at its end it is
						// good.
						var wsAtOffset = TsStringUtils.GetWsAtOffset(m_baseline, ichStartWordBL);
						var oldWordform = m_oldAnalyses[m_iFirstAnalysisToFix].Wordform.Form.get_String(wsAtOffset);
						if (oldWordform.Length == ichEndWordBL - ichStartWordBL)
						{
							// Reuse it: it's unmodified text before the edit that matches in every important way.
							m_iFirstAnalysisToFix++;
							continue;
						}
						// It's a different length: presumably a phrase.
						// We can reuse it if it ends before the change.
						// It's trickier if it ends AT the change. We can reuse something that ends at the start of the edit
						// only if that is still a word boundary. We have to think about cases like these:
						// 1. Inserted white space or punctuation at the end of a word. We'd like to keep the word analysis.
						// 2. Inserted white space or punctuation in the middle of a word. Discard the analysis.
						// 3. Deleted tail-end of word. Discard.
						// 4. Deleted something following the end of the word, leaving the word unchanged. Keep the analysis.
						// 5. Replaced tail end of word with something starting with white space or punctuation. Discard.
						// 6. Replaced stuff following word with something starting with white space or punctuation. Keep.

						// We know nothing changed for the length of the phrase, so it can't have been shortened or modified.
						// The only danger is that it was extended, so there's no longer a word boundary at the end.
						// So, if it ends exactly at the edit, we can reuse if if there is now no following text,
						// or if the next character isn't wordforming.
						int limOldWordform = ichStartWordBL + oldWordform.Length;
						if (limOldWordform < ichFirstDiffBL
							|| (limOldWordform == ichFirstDiffBL && IsAWordBreak(limOldWordform)))
						{
							m_iFirstAnalysisToFix++;
							// But we must advance the word maker: we don't want to see the remaining words
							// of the phrase.
							m_wordFinder.Position = limOldWordform;
							continue;
						}
					}
				}

				m_trailingTokens.Add(new Tuple<int, ITsString>(ichStartWordBL,
									 m_baseline.Substring(ichStartWordBL, ichEndWordBL - ichStartWordBL)));
			}
			m_newAnalysisGroups.Add(canalysis); // note last group size
		}

		/// <summary>
		/// Answer true if a word must end at the given character index: either the end of the text
		/// or a non-wordforming character.
		/// </summary>
		private bool IsAWordBreak(int limOldWordform)
		{
			return (limOldWordform == m_baseline.Length || !m_wordFinder.IsWordforming(limOldWordform));
		}

		/// <summary>
		/// Initialize m_newAnalyses and m_newAnalysisGroups, the Analyses that should go on each new segment.
		/// Also initializes m_cRemovedAnalyses and m_cAddedAnalyses.
		/// </summary>
		private void GetNewAnalysisGroups()
		{
			GetBaselineTexts();
			m_wordFinder = new ParagraphAnalysisFinder(m_baseline,
				m_para.Services.WritingSystemFactory);

			AnalyzeNewText();

			int iTokenIndex;
			int iLastAnalysisToFix;
			if (!m_changedFontOnly)
			{
				iLastAnalysisToFix = GetLastAnalysisToFix(out iTokenIndex);
			}
			else
			{	// if the font changed, but text did not change, we want the wordform analyses to be completely replaced,
				// but the segment annotations to remain. GetLastAnalysisToFix was retaining some of the old
				// analyses (typically a wordform and end punctuation). Later this causes problems in
				// OverridesCellar ParsedParagraphOffsetsMethod.AdvancePastWord() which returns 0 for the
				// length of the old wordform. This means methods like FindAnnotation() can't progress to the
				// next character - stuck in an endless loop. LT-13472
				iTokenIndex = m_trailingTokens.Count - 1;
				iLastAnalysisToFix = m_oldAnalyses.Count - 1;
			}

			List<IAnalysis> newAnalyses = GetNewWordformAnalyses(iTokenIndex);

			// These 2 counts are used in UpdateAffectedReferences()
			if (m_oldAnalyses.Count > 0)
				m_cRemovedAnalyses = iLastAnalysisToFix - m_iFirstAnalysisToFix + 1;
			else
				m_cRemovedAnalyses = 0;
			m_cAddedAnalyses = newAnalyses.Count;
			m_newAnalyses.RemoveRange(m_iFirstAnalysisToFix, m_cRemovedAnalyses);
			m_newAnalyses.InsertRange(m_iFirstAnalysisToFix, newAnalyses);
		}

		/// <summary>
		/// For each trailing token up to iTokenIndex, find or create a corresponding WfiWordform or PunctuationForm.
		/// </summary>
		private List<IAnalysis> GetNewWordformAnalyses(int iTokenIndex)
		{
			var wwfRepo = m_para.Services.GetInstance<IWfiWordformRepository>();
			var wwfFactory = m_para.Services.GetInstance<IWfiWordformFactory>();
			var newAnalyses = new List<IAnalysis>();
			for (int i = 0; i <= iTokenIndex; i++)
			{
				var token = m_trailingTokens[i].Item2;
				var vernWid = TsStringUtils.GetFirstVernacularWs(m_para.Cache.LanguageProject.VernWss, m_para.Services.WritingSystemFactory, token);

				//Decide whether we need to make a wordform or a punctuationForm)
				// must be in a vernacular ws and be word forming characters
				if (vernWid > -1 && m_wordFinder.IsWordforming(m_trailingTokens[i].Item1))
				{
					IWfiWordform wf;
					if (!wwfRepo.TryGetObject(token, out wf))
					{
						wf = wwfFactory.Create(token.ToWsOnlyString());
					}
					newAnalyses.Add(wf);
				}
				else
				{
					IPunctuationForm pf = WfiWordformServices.FindOrCreatePunctuationform(m_para.Cache, token);
					newAnalyses.Add(pf);
				}
			}
			return newAnalyses;
		}

		/// <summary>
		/// Scan the trailing tokens to see which ones occur in the input analysis list. Determine the last one that does not.
		/// Also determine the corresponding position in m_trailingTokens.
		/// </summary>
		/// <param name="iTokenIndex"></param>
		/// <returns></returns>
		private int GetLastAnalysisToFix(out int iTokenIndex)
		{
			int ichFirstDiff = m_paraDiffInfo.IchFirstDiff - m_newBeginOffsets[m_iSegFirstModified];

			iTokenIndex = m_trailingTokens.Count - 1;
			if (m_oldAnalyses.Count == 0)
				return 0;
			// Typically the index of the last analysis for which we need to create a new wordform analysis.
			// More precisely, all analyses with larger indexes are correct and should be kept.
			// It is possible (e.g., when inserting white space where there is already white space) that ALL analyses
			// are kept and NO new ones are needed; in this case, iLastAnalysisToFix will end up being one LESS
			// than iFirstAnalysisToFix, and iTokenIndex will be -1.
			int iLastAnalysisToFix;
			for (iLastAnalysisToFix = m_oldAnalyses.Count - 1; iLastAnalysisToFix >= m_iFirstAnalysisToFix;)
			{
				if (iTokenIndex < 0)
					break; // run out of trailing tokens, can't be any more matches.
				int startWord = m_trailingTokens[iTokenIndex].Item1;
				if (startWord < ichFirstDiff + m_paraDiffInfo.CchInsert)
					break; // change definitely overlaps this word, discard the annotation.
				IAnalysis analysis = m_oldAnalyses[iLastAnalysisToFix];
				if (analysis.HasWordform)
				{
					var oldWordform = analysis.Wordform.Form.get_String(TsStringUtils.GetWsAtOffset(m_baseline, startWord));
					var trailingTokenLength = m_trailingTokens[iTokenIndex].Item2.Length;
					if (oldWordform.Length > trailingTokenLength)
					{
						// We have a phrase to deal with in the old analyses.
						int startPhrase = startWord + trailingTokenLength - oldWordform.Length;
						if (startPhrase < ichFirstDiff + m_paraDiffInfo.CchInsert)
							break; // the phrase overlaps the edit, we can't reuse it.
						int iTokenStartPhrase = iTokenIndex - 1;
						int startToken = startWord;
						for (; iTokenStartPhrase >= 0; iTokenStartPhrase--)
						{
							startToken = m_trailingTokens[iTokenStartPhrase].Item1;
							if (startToken <= startPhrase)
								break;
						}
						if (startToken != startPhrase)
						{
							// we can't match the phrase to current tokens, perhaps because wordforming characters
							// were inserted or punctuation deleted at the start of the phrase.
							// End the matches with the analysis after it, so it isn't reused.
							break;
						}
						// We need to replace the tokens from iTokenStartPhrase to iTokenIndex with the single
						// phrase we are reusing, and then adjust everything so the loop can go on.
						m_trailingTokens[iTokenStartPhrase] = new Tuple<int, ITsString>(startPhrase, oldWordform);
						m_trailingTokens.RemoveRange(iTokenStartPhrase, iTokenIndex - iTokenStartPhrase);
						iTokenIndex = iTokenStartPhrase;
					}
					if (startWord == ichFirstDiff + m_paraDiffInfo.CchInsert)
					{
						// word starts exactly at the end of the edit. As above, we can keep the annotation if the lengths match.
						if (oldWordform.Length != trailingTokenLength)
						{
							break; // not the same wordform, discard the annotation. (Typically the edit shortened the word.)
						}
					}
				}
				else if (analysis is IPunctuationForm &&
					((IPunctuationForm)analysis).Form.Text != m_trailingTokens[iTokenIndex].Item2.Text)
				{
					break;
				}
				iTokenIndex--;
				iLastAnalysisToFix--;
			}
			return iLastAnalysisToFix;
		}

		/// <summary>
		/// Initialize m_baseline to the contents of the segments we have determined need to be processed.
		/// </summary>
		private void GetBaselineTexts()
		{
			//TODO: take the paragraph.contents.text and extract the substring from the
			//newbeginoffsets of m_iSegFirstModified to the end of m_iNewSegLastModified
			// (which is either the start of the following segment, if any, or the end of the whole paragraph).
			// Review: GJM 3/31/10: Is this still a TODO, or is it a DONE?

			int ichMin = m_newBeginOffsets[m_iSegFirstModified];
			int ichLim = EndOfNewSeg(m_iNewSegLastModified);
			m_baseline = m_para.Contents.Substring(ichMin, ichLim - ichMin);
		}
		#endregion

		#region Methods related to HandleMoveDest
		/// <summary>
		/// This is a primary entry point for IStTxtPara.SetContentsForMoveDest. It is similar to the
		/// top-level AdjustAnalysis, except that we know exactly what was inserted where, AND,
		/// we know it came from another paragraph. If the source paragraph is analysed, we can transfer any relevant
		/// analysis.
		/// Enhance JohnT: Currently this only supports moving from the end of one paragraph to the end of another.
		/// This is sufficient for all current usages of the MoveString method which this supports.
		/// </summary>
		private void HandleMoveDest(int cchInsert, IStTxtPara source, int ichSource, bool fParaIsNew)
		{
			// We will consider the last existing segment, if any, suspect, since it may not have an explicit segment-end
			// character, or we may do something bizarre like merging a paragraph that ends with a period with one that starts
			// with two.
			m_iSegFirstModified = Math.Max(m_para.SegmentsOS.Count - 1, 0);
			DetermineWhatToMove(ichSource, cchInsert, source);
			var refsMoved = MoveAnalysisAndReferencesFromOldPara(cchInsert, source, fParaIsNew);
			InitializeAdjustmentForMove();
			m_oldParaRefs = new Set<IAnalysisReference>();
			m_oldParaRefs.AddRange(m_para.GetTags());
			m_oldParaRefs.AddRange(m_para.GetChartCellRefs());
			if (refsMoved != null)
				m_oldParaRefs.AddRange(refsMoved);
			DoTheAdjustment();
		}

		/// <summary>
		/// Initialize m_segsToMove and m_analysesToMove from the given source information.
		/// Don't modify the source paragraph until this is done.
		/// </summary>
		private void DetermineWhatToMove(int ichSource, int cchInsert, IStTxtPara source)
		{
			// Find entire segments to move.
			m_segsToMove = new List<ISegment>();
			m_segPartlyMoved = null;
			foreach(ISegment seg in source.SegmentsOS)
			{
				if (seg.BeginOffset >= ichSource)
					m_segsToMove.Add(seg);
				else if (cchInsert > 0 && seg.EndOffset <= ichSource + cchInsert)
					m_segPartlyMoved = seg;
			}
			if (m_segPartlyMoved != null && m_segPartlyMoved.EndOffset == ichSource)
			{
				// We are splitting the paragraph right at a segment boundary so there is
				// no need to copy this segment (as if it was split in the middle of this segment).
				m_segPartlyMoved = null;
			}

			m_analysesToMove = new List<IAnalysis>();
			if (m_segPartlyMoved != null && source.ParseIsCurrent)
			{
				var analyses = new ParsedParagraphOffsetsMethod(m_segPartlyMoved).GetAnalysesAndOffsets();
				foreach (var pair in analyses)
				{
					if (pair.Item2 >= ichSource)
						m_analysesToMove.Add(pair.Item1);
				}
			}
		}

		private List<IAnalysisReference> MoveAnalysisAndReferencesFromOldPara(int cchInsert,
			IStTxtPara source, bool fParaIsNew)
		{
			List<IAnalysisReference> refsMoved = null;
			// Make stuff we add look as if it started at the end of the original para.
			int endOfOldContents = m_para.Contents.Length - cchInsert;
			if (m_segPartlyMoved != null)
			{
				// Make a new segment to hold the moved stuff. It may eventually get merged with the previous segment.
				ISegment newSeg = ((SegmentFactory)m_para.Services.GetInstance<ISegmentFactory>()).Create(m_para, endOfOldContents);
				if (!fParaIsNew)
				{
					// When merging paragraphs (i.e. when the destination paragraph is not new), the new
					// segment we create needs to contain the translation of the partially moved
					// segment from the source paragraph. Otherwise the translation of that
					// segment will be lost (since the source paragraph will be deleted).
					newSeg.FreeTranslation.CopyAlternatives(m_segPartlyMoved.FreeTranslation);
					newSeg.LiteralTranslation.CopyAlternatives(m_segPartlyMoved.LiteralTranslation);
				}
				foreach (var analysis in m_analysesToMove)
					newSeg.AnalysesRS.Add(analysis);
				refsMoved = MoveReferencesFromOldPara(newSeg, m_segPartlyMoved.AnalysesRS.Count - m_analysesToMove.Count);
			}
			foreach (Segment seg in m_segsToMove)
			{
				m_para.SegmentsOS.Add(seg);
				// Since we are moving from the end of one paragraph to the end of the other, we can get a good
				// beginOffset for the moved segment by adjusting by the difference in length of the paragraphs.
				seg.BeginOffset += m_para.Contents.Length - source.Contents.Length;
			}
			return refsMoved;
		}

		private List<IAnalysisReference> MoveReferencesFromOldPara(ISegment newSeg, int canalysesLeftBehind)
		{
			Debug.Assert(m_segPartlyMoved != null, "Nothing to do! I shouldn't have been called.");
			var refsMoved = new List<IAnalysisReference>();
			var oldPara = (IStTxtPara)m_segPartlyMoved.Owner;
			var oldReferences = oldPara.GetTags().Cast<IAnalysisReference>().ToList();
			oldReferences.AddRange(oldPara.GetChartCellRefs().Cast<IAnalysisReference>());
			foreach (var iar in oldReferences)
			{
				var fchgMade = false;
				if (iar.BegRef().Segment == m_segPartlyMoved && iar.BegRef().Index >= canalysesLeftBehind)
				{
					iar.ChangeToDifferentSegment(newSeg, true, false);
					iar.ChangeToDifferentIndex(iar.BegRef().Index - canalysesLeftBehind, true, false);
					fchgMade = true;
				}
				if (iar.EndRef().Segment == m_segPartlyMoved && iar.EndRef().Index >= canalysesLeftBehind)
				{
					iar.ChangeToDifferentSegment(newSeg, false, true);
					iar.ChangeToDifferentIndex(iar.EndRef().Index - canalysesLeftBehind, false, true);
					fchgMade = true;
				}
				if (fchgMade)
					refsMoved.Add(iar);
			}
			return refsMoved;
		}

		private void InitializeAdjustmentForMove()
		{
			// At this point, we have a reasonable set of segments for this paragraph, with all the analysis we want
			// to salvage from the source paragraph. We basically want to run the AdjustAnalysis algorithm to make
			// sure any merging and re-arranging that is needed happens. However, we can simplify some of the early
			// stages, because we know none of the segment BeginOffsets need adjusting, and we know what was inserted
			// where.

			// At this point m_para.Contents.Text contains the modified text but the Segments[i].BeginOffset's
			// line up with the positions corresponding to the text before the edit.
			// We need to compute a separate set of old end offsets because Segment.EndOffset uses the current
			// paragraph length for the last segment.
			m_oldBeginOffsets = (from seg in m_para.SegmentsOS select seg.BeginOffset).ToArray();
			m_oldEndOffsets = new int[m_para.SegmentsOS.Count];
			int iSeg;
			for (iSeg = 1; iSeg < m_para.SegmentsOS.Count; iSeg++)
			{
				m_oldEndOffsets[iSeg - 1] = m_oldBeginOffsets[iSeg];
			}
			if (m_oldEndOffsets.Length > 0)
				m_oldEndOffsets[m_oldEndOffsets.Length - 1] = m_para.Contents.Length;

			ComputeRequiredBeginOffsets();

			// The first segment we moved is suspect, because it might merge with the previous one
			// if the previous one didn't have a definite end character.
			m_iOldSegLastModified = Math.Max(Math.Min(m_iSegFirstModified + 1, m_para.SegmentsOS.Count - 1), 0);
			m_iNewSegLastModified = m_iOldSegLastModified;
			if (m_iNewSegLastModified > m_iSegFirstModified && m_newBeginOffsets.Length < m_para.SegmentsOS.Count)
				m_iNewSegLastModified--; // this will typically cause a merge.
		}
		#endregion
	}
}
