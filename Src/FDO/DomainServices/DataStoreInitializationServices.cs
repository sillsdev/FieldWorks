// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2010' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataStoreInitializationServices.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Paratext;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Services for a starting data store that are used to initialize the data to get it useable
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class DataStoreInitializationServices
	{
		#region JoinedSegmentSplitter class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This class remembers the state of analyses, notes, and translations before a segment
		/// is merged with another (following) segment. It's whole purpose in life is to
		/// facilitate a subsequent split at a point just beyond the intervening ORCs and/or
		/// punctuation. That's why we call it a "splitter", but it doesn't feel like the
		/// world's best name, since often this class will be instantiated, but the Split method
		/// will never actually be called. Them's the breaks (or lack of breaks, as the case
		/// may be).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class JoinedSegmentSplitter
		{
			private readonly int m_countOfAnalysesBeforeJoin;
			private readonly int m_countOfNotesBeforeJoin;
			private readonly Dictionary<int, int> m_ftLengthsBeforeJoin;
			private readonly Dictionary<int, int> m_ltLengthsBeforeJoin;
			private readonly ISegment m_prevSegmentBeforeJoin;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="JoinedSegmentSplitter"/> class.
			/// </summary>
			/// <param name="prevSegmentBeforeJoin">The "source" segment (this must be called
			/// before combining this segment with the following segment.</param>
			/// --------------------------------------------------------------------------------
			public JoinedSegmentSplitter(ISegment prevSegmentBeforeJoin)
			{
				m_prevSegmentBeforeJoin = prevSegmentBeforeJoin;
				m_countOfAnalysesBeforeJoin = prevSegmentBeforeJoin.AnalysesRS.Count;
				m_countOfNotesBeforeJoin = prevSegmentBeforeJoin.NotesOS.Count;

				m_ftLengthsBeforeJoin = new Dictionary<int, int>();
				foreach (int ws in prevSegmentBeforeJoin.FreeTranslation.AvailableWritingSystemIds)
					m_ftLengthsBeforeJoin[ws] = prevSegmentBeforeJoin.FreeTranslation.get_String(ws).Length;

				m_ltLengthsBeforeJoin = new Dictionary<int, int>();
				foreach (int ws in prevSegmentBeforeJoin.LiteralTranslation.AvailableWritingSystemIds)
					m_ltLengthsBeforeJoin[ws] = prevSegmentBeforeJoin.LiteralTranslation.get_String(ws).Length;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Splits the analyses, notes, and translations, moving the pieces following the
			/// previously saved join locations into the specified destination segment.
			/// </summary>
			/// <param name="destSeg">The destination segment.</param>
			/// --------------------------------------------------------------------------------
			public void Split(ISegment destSeg)
			{
				Debug.Assert(m_prevSegmentBeforeJoin.IsValidObject);
				Debug.Assert(m_prevSegmentBeforeJoin.AnalysesRS.Count >= m_countOfAnalysesBeforeJoin);
				Debug.Assert(m_prevSegmentBeforeJoin.NotesOS.Count >= m_countOfNotesBeforeJoin);

				int iStartMove;
				for (iStartMove = m_countOfAnalysesBeforeJoin; iStartMove < m_prevSegmentBeforeJoin.AnalysesRS.Count; iStartMove++)
				{
					if (!(m_prevSegmentBeforeJoin.AnalysesRS[iStartMove] is IPunctuationForm))
						break;
				}

				for (int i = m_prevSegmentBeforeJoin.AnalysesRS.Count - 1; i >= iStartMove; i--)
				{
					destSeg.AnalysesRS.Insert(0, m_prevSegmentBeforeJoin.AnalysesRS[i]);
					m_prevSegmentBeforeJoin.AnalysesRS.RemoveAt(i);
				}
				for (int i = m_prevSegmentBeforeJoin.NotesOS.Count - 1; i >= m_countOfNotesBeforeJoin; i--)
					destSeg.NotesOS.Insert(0, m_prevSegmentBeforeJoin.NotesOS[i]);

				SplitTranslations(m_ftLengthsBeforeJoin, m_prevSegmentBeforeJoin.FreeTranslation, destSeg.FreeTranslation);
				SplitTranslations(m_ltLengthsBeforeJoin, m_prevSegmentBeforeJoin.LiteralTranslation, destSeg.LiteralTranslation);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Splits the translation for each writing system at the given locations, moving
			/// any portion after the split location to the destination translation.
			/// </summary>
			/// <param name="splitLocations">The split locations (one per WS).</param>
			/// <param name="srcTrans">The source translation.</param>
			/// <param name="destTrans">The destination translation.</param>
			/// --------------------------------------------------------------------------------
			private static void SplitTranslations(Dictionary<int, int> splitLocations,
				IMultiString srcTrans, IMultiString destTrans)
			{
				foreach (KeyValuePair<int, int> info in splitLocations)
				{
					int ws = info.Key;
					int prevLength = info.Value;
					ITsString tss = srcTrans.get_String(ws);
					srcTrans.set_String(ws, tss.GetSubstring(0, prevLength));
					ITsStrBldr bldr = destTrans.get_String(ws).GetBldr();
					bldr.ReplaceTsString(0, 0, tss.GetSubstring(prevLength, tss.Length));
					destTrans.set_String(ws, bldr.GetString());
				}
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares the cache by ensuring that the data is in a valid state.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		internal static void PrepareCache(FdoCache cache)
		{
			EnsureBtForScrParas(cache); // Must happen before EnsureSegmentsForScrParas
			EnsureSegmentsForScrParas(cache); // Must happen before FixSegmentsForScriptureParas
			FixSegmentsForScriptureParas(cache);
			EnsureValidVersification(cache);
			EnsureStylesInUseSetForScrParas(cache);
		}

		#region Methods for cleaning up Scripture
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that Scripture has a valid versification.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		private static void EnsureValidVersification(FdoCache cache)
		{
			IScripture scr = cache.LanguageProject.TranslatedScriptureOA;
			if (scr != null && scr.Versification == ScrVers.Unknown)
				scr.Versification = ScrVers.English;
		}
		#endregion

		#region Methods for cleaning up segments
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that all the Scripture paragrahs have all required segments.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		private static void EnsureSegmentsForScrParas(FdoCache cache)
		{
			IScripture scr = cache.LanguageProject.TranslatedScriptureOA;
			if (scr == null || scr.FixedParasWithoutSegments)
				return; // Has already been done or not needed

			foreach (IScrTxtPara para in cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().AllInstances())
			{
				BackTranslationAndFreeTranslationUpdateHelper.Do(para,
					() => EnsureSegmentsForPara(para));
			}

			((Scripture)scr).FixedParasWithoutSegments = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that a paragrah has all required segments.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private static void EnsureSegmentsForPara(IScrTxtPara para)
		{
			// Check to make sure we have segments for this paragraph. Even an empty paragraph
			// should have one segment. A paragraph with no segments means no parsing has ever
			// been done.
			if (para.SegmentsOS.Count == 0 && para.Contents.Length > 0)
			{
				ICmTranslation trans = para.GetBT();
				int[] availWs = (trans != null) ? trans.Translation.AvailableWritingSystemIds : null;
				if (availWs != null && availWs.Length > 0)
				{
					// There is an existing CmTranslation that has the text we want so
					// convert the CmTranslation to segments.
					foreach (int ws in availWs)
						BtConverter.ConvertCmTransToInterlin(para, ws);
				}
				else
				{
					// No existing CmTranslation, so just make empty segments
					SegmentServices.EnsureMainParaSegments(para);
				}
			}

		}
		#endregion

		#region Methods for cleaning up paragraph BTs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that a back translation CmTranslation exists for all Scripture paragraphs
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		private static void EnsureBtForScrParas(FdoCache cache)
		{
			IScripture scr = cache.LanguageProject.TranslatedScriptureOA;
			if (scr == null || scr.FixedParasWithoutBt)
				return; // Has already been done or not needed

			foreach (IScrTxtPara para in cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().AllInstances())
			{
				BackTranslationAndFreeTranslationUpdateHelper.Do(para,
					() => EnsureBtForPara(para));
			}

			((Scripture)scr).FixedParasWithoutBt = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that a back translation CmTranslation exists for the specified paragraph
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private static void EnsureBtForPara(IScrTxtPara para)
		{
			ICmPossibility btType = para.Services.GetInstance<ICmPossibilityRepository>().GetObject(
				CmPossibilityTags.kguidTranBackTranslation);

			ICmTranslation btTrans = para.TranslationsOC.FirstOrDefault(t => t.TypeRA == btType);

			if (para.TranslationsOC.Count > 0)
			{
				// The paragraphs already have some CmTranslations. Pick one then make it the CmTranslation
				// for the back translation. Any extra CmTranslations will be deleted.
				List<ICmTranslation> translationsToDelete = new List<ICmTranslation>();
				foreach (ICmTranslation trans in para.TranslationsOC)
				{
					if (trans != btTrans && trans.TypeRA == btType)
					{
						MergeCmTranslations(trans, btTrans);
						translationsToDelete.Add(trans);
					}
					else if (trans.TypeRA != btType)
					{
						if (btTrans != null)
							translationsToDelete.Add(trans);
						else
						{
							trans.TypeRA = btType;
							btTrans = trans;
						}
					}
				}

				// Remove any other translations that didn't have a type set
				foreach (ICmTranslation trans in translationsToDelete)
					para.TranslationsOC.Remove(trans);
			}

			if (btTrans == null)
			{
				// Still no CmTranslation for the back translation, so create a new one.
				para.Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(para, btType);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the CmTranslations selecting the longest alternative for each of the available
		/// writing systems.
		/// </summary>
		/// <param name="trans">The translation to merge into the existing translation.</param>
		/// <param name="existingBtTrans">The existing translation.</param>
		/// ------------------------------------------------------------------------------------
		private static void MergeCmTranslations(ICmTranslation trans, ICmTranslation existingBtTrans)
		{
			foreach (int wsBt in trans.Translation.AvailableWritingSystemIds)
			{
				ITsString tssTrans = trans.Translation.get_String(wsBt);
				ITsString tssExistingTrans = existingBtTrans.Translation.get_String(wsBt);

				if (tssTrans.Length > tssExistingTrans.Length)
					existingBtTrans.Translation.set_String(wsBt, tssTrans);
			}
		}
		#endregion

		#region Methods for fixing segments of a paragraph with ORCs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resegments any Scripture paragraphs that contain footnote or picture ORCs because
		/// of the change in the segmentation code to no longer break segments on ORCs. Also,
		/// leading punctuation for any label segment at the start of a paragraph is now treated
		/// as a separate segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void FixSegmentsForScriptureParas(FdoCache cache)
		{
			// As of the time the segmentation code was changed to not break at footnote/picture ORCs,
			// only Scripture had them, so if this project doesn't have Scripture, there's nothing to do.
			IScripture scr = cache.LanguageProject.TranslatedScriptureOA;
			if (scr == null || scr.ResegmentedParasWithOrcs)
				return; // Resegmenting has already been done or not needed

			bool hadError = false;
			IScrTxtParaRepository paraRepository = cache.ServiceLocator.GetInstance<IScrTxtParaRepository>();

			foreach (IScrTxtPara para in paraRepository.AllInstances())
			{
				if (!ParaSegmentsValid(para, true, true))
					BlowAwaySegments(para);
				else if (para.Contents.Length > 0 && para.Contents.Text.IndexOf(StringUtils.kChObject) >= 0)
				{
					BackTranslationAndFreeTranslationUpdateHelper.Do(para,
						() => FixSegmentsForPara(para));

					if (!ParaSegmentsValid(para, false, false))
					{
						// We failed to create a valid segmented BT (this shouldn't happen)
						IScrDraft owningDraft = para.OwnerOfClass<IScrDraft>();
						IScrBook owningBook = para.OwnerOfClass<IScrBook>();
						if (owningDraft != null)
							Logger.WriteEvent(string.Format("Paragraph resegment failed in book {0} in revision '{1}':", owningBook.BookId, owningDraft.Description));
						else
							Logger.WriteEvent(string.Format("Paragraph resegment failed in book {0}:", owningBook.BookId));
						Logger.WriteEvent(string.Format("Paragraph text: '{0}'", para.Contents.Text));
						Logger.WriteEvent(string.Format("Paragraph segments (total {0}): ", para.SegmentsOS.Count));
						foreach (ISegment seg in para.SegmentsOS)
							Logger.WriteEvent(string.Format("Begin: {0}, End: {1}, Text: '{2}'", seg.BeginOffset, seg.EndOffset, seg.BaselineText.Text));

						BlowAwaySegments(para); // Just do our best-guess fallback logic
						hadError = true;
					}
				}
			}

			foreach (IScrTxtPara para in (from p in paraRepository.AllInstances() where p.SegmentsOS.Count == 1 select p))
				FixParaAnalysis(para);

			((Scripture)scr).ResegmentedParasWithOrcs = true;

			if (hadError)
			{
				// Tell the user that something bad happened
				ErrorReporter.ReportException(new Exception("Error during resegmentation"), null, null, null, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the specified paragraph contains segments that are messed
		/// up so bad that we can't (or won't) try fix them.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="fCheckForEmptySegments">True to check for segments that have no
		/// translation when there exists a CmTranslation that contains a translation.</param>
		/// <param name="fTreatOrcsAsLabels">True to treat ORCs as label text (FW 6.0 style),
		/// false otherwise.</param>
		/// <returns>
		/// True if the specified paragraph contains invalid segments, false otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static bool ParaSegmentsValid(IScrTxtPara para, bool fCheckForEmptySegments,
			bool fTreatOrcsAsLabels)
		{
			List<TsStringSegment> expectedSegs = para.Contents.GetSegments(
				para.Cache.WritingSystemFactory, fTreatOrcsAsLabels);

			if (expectedSegs.Count != para.SegmentsOS.Count)
				return false; // invalid number of segments

			bool foundNonLabelSegment = false;
			bool foundSegmentWithTranslation = false;

			for (int i = 0; i < para.SegmentsOS.Count; i++)
			{
				ISegment seg = para.SegmentsOS[i];
				if (seg.BeginOffset != expectedSegs[i].IchMin || seg.BeginOffset >= seg.EndOffset)
					return false; // offset at unexpected location
				if (fCheckForEmptySegments && !seg.IsLabel)
				{
					foundNonLabelSegment = true;
					foundSegmentWithTranslation |= seg.FreeTranslation.AvailableWritingSystemIds.Any(
						ws => seg.FreeTranslation.get_String(ws).Length > 0);
				}
			}

			// If there is a CmTranslation for the paragraph, but no translation for any segment,
			// then it's invalid and we'll need to rebuild the segments using the CmTranslation.
			if (foundNonLabelSegment && !foundSegmentWithTranslation)
			{
				IMultiString trans = para.GetBT().Translation;
				if (trans.AvailableWritingSystemIds.Any(ws => trans.get_String(ws).Length > 0))
					return false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Blows away any segments in the specified paragraph and re-segments with the correct
		/// segments. Any back translation contained in the CmTranslation back translation is
		/// then copied over to the new segments to minimize data loss.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private static void BlowAwaySegments(IScrTxtPara para)
		{
			para.ParseIsCurrent = false; // Make sure we do a full parse if we have analyses

			BackTranslationAndFreeTranslationUpdateHelper.Do(para,
				() => FixParaAnalysis(para)); // Force a re-segment, but try to keep analyses

			foreach (int btWs in para.GetBT().Translation.AvailableWritingSystemIds)
				BtConverter.ConvertCmTransToInterlin(para, btWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixes the paragraph's analysis.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private static void FixParaAnalysis(IScrTxtPara para)
		{
			// If it has any word-level analysis, we need to reparse the whole text.
			if ((from segment in para.SegmentsOS where segment.AnalysesRS.Count > 0 select segment).FirstOrDefault() == null)
			{
				// No analyses; just resegment it.
				using (ParagraphParser parser = new ParagraphParser(para))
				{
					parser.CollectPreExistingParaAnnotations();
					SegmentMaker segmentMaker = new SegmentMaker(para.Contents, para.Cache.WritingSystemFactory, parser);
					segmentMaker.Run();
					if (segmentMaker.Segments.Count < para.SegmentsOS.Count)
					{
						// The paragraph has more segments than it should have, so remove any
						// extras that are floating around.
						for (int i = para.SegmentsOS.Count - 1; i >= segmentMaker.Segments.Count; i--)
							para.SegmentsOS.RemoveAt(i);
					}
				}
			}
			else
			{
				// Reparse the whole thing.
				ParagraphParser.ParseParagraph(para);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resegments the specified paragraph for the change in the segmentation code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void FixSegmentsForPara(IScrTxtPara para)
		{
			List<TsStringSegment> expectedSegs = para.Contents.GetSegments(para.Cache.WritingSystemFactory);
			int iSegment = 0;
			int iSegmentExpected = 0;
			ISegmentFactory segFactory = para.Cache.ServiceLocator.GetInstance<ISegmentFactory>();
			JoinedSegmentSplitter prevSegmentSplitter = null;

			while (iSegmentExpected < expectedSegs.Count || iSegment < para.SegmentsOS.Count)
			{
				ISegment segCurr = iSegment < para.SegmentsOS.Count ? para.SegmentsOS[iSegment] : null;
				TsStringSegment segExpected = iSegmentExpected < expectedSegs.Count ? expectedSegs[iSegmentExpected] : null;
				ITsString segOldBaseLine = null;
				if (segCurr != null)
				{
					segOldBaseLine = segCurr.BaselineText;
					if (segCurr.IsLabel)
					{
						// We don't care about the analyses for label segments (and they're probably wrong anyways)
						segCurr.AnalysesRS.Clear();
					}
				}

				// Determine if there is a difference between what we have in our 6.0 segment division versus what
				// we know we need for the 7.0 segment division. Fix any mismatched segments by removing segments
				// or adding segments depending on the type of mismatch.
				if (segCurr != null && segExpected != null && segCurr.BeginOffset == segExpected.IchMin)
				{
					// The segment division is already in the correct location, so move on to the next
					// segment division.
					if (!segCurr.IsLabel && segOldBaseLine.Text[0] == StringUtils.kChObject)
					{
						// The segment is only ORC(s) so copy the ORC(s) to the free translation
						// and, if needed, make puctuation forms for them.
						AddOrcs(segOldBaseLine, segCurr.FreeTranslation, (iSegment < para.SegmentsOS.Count - 1) ?
							para.SegmentsOS[iSegment + 1] : null, false, false);
						segCurr.AnalysesRS.Clear();
						CreatePuncFormsInSegmentForOrcs(segCurr, segOldBaseLine, para, 0);
					}
					iSegment++;
					iSegmentExpected++;
					prevSegmentSplitter = null; // Make sure we don't split a segment we are past
				}
				else if (segCurr != null && (segExpected == null || segCurr.BeginOffset < segExpected.IchMin))
				{
					// The segment division is expected to be after the division we found. To handle this
					// case we remove this segment, effectively moving the segment break to where the next
					// segment begins.
					prevSegmentSplitter = null;
					bool fAppend = (iSegment > 0); // Always assume appending if not the first segment
					ISegment segDest = (fAppend) ? para.SegmentsOS[iSegment - 1] : para.SegmentsOS[iSegment + 1];

					if (segOldBaseLine.Text.Contains(StringUtils.kChObject))
					{
						// The segment contains ORC(s), which need to be moved into the preceding or following segment.
						if (fAppend)
						{
							ITsString tssOrcs = (segExpected == null || segExpected.IchMin - segCurr.BeginOffset >= segOldBaseLine.Length) ? segOldBaseLine :
								segOldBaseLine.GetSubstring(0, segExpected.IchMin - segCurr.BeginOffset);
							JoinOrcToPrecedingSegment(para, tssOrcs, segDest, segCurr,
								(iSegment < para.SegmentsOS.Count - 1) ? para.SegmentsOS[iSegment + 1] : null);
						}
						else
							JoinOrcToFollowingSegment(para, segOldBaseLine, null, segCurr, (Segment)segDest);
					}
					else
					{
						// Prepare for the possibility of splitting this merged segment in our next pass
						prevSegmentSplitter = new JoinedSegmentSplitter(segDest);
						TransferSegmentInfo(segCurr, segDest, fAppend);
						para.SegmentsOS.RemoveAt(iSegment);
					}
				}
				else // (segCurr == null || segCurr.BeginOffset > segExpected.IchMin)
				{
					// The segment division is expected to be before the division we found.
					// Insert the missing segment. Nine times out of 10, this is a situation where we are
					// "moving" a segment break -- the old one was removed in our previous pass.
					Debug.Assert(segExpected != null);

					int iSegmentInsert = iSegment;
					Segment insertedSegment = (Segment)segFactory.Create();
					para.SegmentsOS.Insert(iSegmentInsert, insertedSegment);
					insertedSegment.BeginOffset = segExpected.IchMin;
					ITsString insertedSegBaseLine = insertedSegment.BaselineText;
					if (insertedSegBaseLine.Text[0] == StringUtils.kChObject)
					{
						// In this case we inserted a segment that starts with an ORC. In most (if not all)
						// cases, this means the segment, at least in its current state, contains only ORC(s).
						// Copy the ORC(s) to the free translation, and, if needed, create puctuation forms for them.
						ITsString tssOrcs = (segExpected.IchLim - segExpected.IchMin >= insertedSegBaseLine.Length) ?
							insertedSegBaseLine : insertedSegBaseLine.GetSubstring(0, segExpected.IchLim - segExpected.IchMin);
						AddOrcs(tssOrcs, insertedSegment.FreeTranslation, segCurr, false, false);
						CreatePuncFormsInSegmentForOrcs(insertedSegment, tssOrcs, para, 0);
					}
					else if (iSegmentExpected > 0)
					{
						ISegment prevSeg = para.SegmentsOS[iSegmentExpected - 1];
						int cAnalyses = prevSeg.AnalysesRS.Count;
						IPunctuationForm punctForm = cAnalyses == 0 ? null : prevSeg.AnalysesRS[cAnalyses - 1] as IPunctuationForm;
						if (punctForm != null && insertedSegBaseLine.Text == punctForm.Form.Text)
						{
							// The inserted segment moved the segment boundary because of only non word-forming
							// characters. Move the punctuation form for the moved characters to the inserted segment.
							insertedSegment.AnalysesRS.Insert(0, punctForm);
							prevSeg.AnalysesRS.RemoveAt(cAnalyses - 1);
						}
					}
					if (prevSegmentSplitter != null)
					{
						// Split the translations, analyses, notes, etc. where the new segment boundary is
						// located if they were previously joined with the current segment.
						prevSegmentSplitter.Split(insertedSegment);
						prevSegmentSplitter = null;
					}

					iSegment++;
					iSegmentExpected++;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Transfers all of the segment information from the specifed source segment to the
		/// specified destination segment.
		/// </summary>
		/// <param name="source">The source segment.</param>
		/// <param name="dest">The destination segment.</param>
		/// <param name="fAppend">If set to <c>true</c> append the source to the destination,
		/// otherwise prepend to the destination.</param>
		/// ------------------------------------------------------------------------------------
		private static void TransferSegmentInfo(ISegment source, ISegment dest, bool fAppend)
		{
			CopyTranslations(source.FreeTranslation, source.BaselineText.Text, dest.FreeTranslation, dest.BaselineText.Text, fAppend);
			CopyTranslations(source.LiteralTranslation, source.BaselineText.Text, dest.LiteralTranslation, dest.BaselineText.Text, fAppend);

			// Move the notes
			int cNoteSource = source.NotesOS.Count;
			for (int i = 0; i < cNoteSource; i++)
			{
				if (fAppend)
					dest.NotesOS.Add(source.NotesOS[0]);
				else
					dest.NotesOS.Insert(i, source.NotesOS[0]);
			}

			// Copy the analyses
			int cOrigDestAnalCount = dest.AnalysesRS.Count;
			for (int i = 0; i < source.AnalysesRS.Count; i++)
			{
				if (fAppend)
					dest.AnalysesRS.Add(source.AnalysesRS[i]);
				else
					dest.AnalysesRS.Insert(i, source.AnalysesRS[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the translations from the specified source to the specified destination.
		/// </summary>
		/// <param name="transSource">The source translation.</param>
		/// <param name="sourceBaseline">The source baseline.</param>
		/// <param name="transDest">The destination translation.</param>
		/// <param name="destBaseline">The baseline of the destination.</param>
		/// <param name="fAppend">If set to <c>true</c> append the source to the destination,
		/// otherwise prepend to the destination.</param>
		/// ------------------------------------------------------------------------------------
		private static void CopyTranslations(IMultiString transSource, string sourceBaseline,
			IMultiString transDest, string destBaseline, bool fAppend)
		{
			foreach (int ws in transSource.AvailableWritingSystemIds)
			{
				ITsStrBldr transBldr = transDest.get_String(ws).GetBldr();

				ITsString tssSourceTrans = transSource.get_String(ws);
				// Determine if a space needs to be added to the text where the joining is happening
				// and add it if needed.
				if (fAppend && destBaseline.EndsWith(" ", StringComparison.Ordinal) && tssSourceTrans.Length > 0 &&
					!tssSourceTrans.Text.StartsWith(" ", StringComparison.Ordinal) && transBldr.Text != null &&
					!transBldr.Text.EndsWith(" ", StringComparison.Ordinal))
				{
					transBldr.Replace(transBldr.Length, transBldr.Length, " ", StyleUtils.CharStyleTextProps(null, ws));
				}
				else if (!fAppend && sourceBaseline.EndsWith(" ", StringComparison.Ordinal) &&
					!tssSourceTrans.Text.StartsWith(" ", StringComparison.Ordinal) && transBldr.Length > 0)
				{
					transBldr.Replace(0, 0, " ", StyleUtils.CharStyleTextProps(null, ws));
				}

				// Update the values in the multistrings
				int ich = fAppend ? transBldr.Length : 0;
				transBldr.ReplaceTsString(ich, ich, tssSourceTrans);
				transDest.set_String(ws, transBldr.GetString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the ORC in the current segment to the end of the preceding segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void JoinOrcToPrecedingSegment(IScrTxtPara para, ITsString curSegBaseline,
			ISegment precedingSegment, ISegment currSegment, ISegment followingSegment)
		{
			if (precedingSegment.IsHardLineBreak)
				return; // no need to join (FWR-2843)

			// Add ORC to free translation
			bool fNeedLeadingSpace = precedingSegment.BaselineText.Text.EndsWith(" ", StringComparison.Ordinal);
			AddOrcs(curSegBaseline, precedingSegment.FreeTranslation, followingSegment, true, fNeedLeadingSpace);

			if (fNeedLeadingSpace)
			{
				// Also need to add a space to literal translation of the preceding segment if the
				// following segment doesn't start with one. (ORCs don't go into the literal translation.)
				foreach (int ws in precedingSegment.LiteralTranslation.AvailableWritingSystemIds)
				{
					ITsString destTss = precedingSegment.LiteralTranslation.get_String(ws);
					ITsString srcTss = (followingSegment == null) ? null : followingSegment.LiteralTranslation.get_String(ws);
					if (destTss.Text != null && !destTss.Text.EndsWith(" ", StringComparison.Ordinal) &&
						srcTss != null && srcTss.Length > 0 && !srcTss.Text.StartsWith(" ", StringComparison.Ordinal))
					{
						ITsStrBldr bldr = destTss.GetBldr();
						bldr.Replace(bldr.Length, bldr.Length, " ", null);
						precedingSegment.LiteralTranslation.set_String(ws, bldr.GetString());
					}
				}
			}

			// Create a puncuation form for the ORC and move the rest of the analyses
			CreatePuncFormsInSegmentForOrcs(precedingSegment, curSegBaseline, para,
				precedingSegment.AnalysesRS.Count);

			// Delete the segment for the ORC
			para.SegmentsOS.Remove(currSegment);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the ORC in the current segment to the beginning of the following segment
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void JoinOrcToFollowingSegment(IScrTxtPara para, ITsString curSegBaseLine,
			ISegment prevSegment, ISegment currSegment, Segment followingSegment)
		{
			if (followingSegment.IsHardLineBreak)
				return; // no need to join (FWR-2843)

			// Add ORC to free translation
			AddOrcs(curSegBaseLine, followingSegment.FreeTranslation, prevSegment, false, false);

			// Adjust beginning offset of following segment to include this segment
			followingSegment.BeginOffset = currSegment.BeginOffset;

			// Create a puncuation form for the ORC and move the rest of the analyses
			CreatePuncFormsInSegmentForOrcs(followingSegment, curSegBaseLine, para, 0);

			// Delete the segment for the ORC
			para.SegmentsOS.Remove(currSegment);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds any ORCs to the beginning or end of each of the translations.
		/// </summary>
		/// <param name="curSegBaseLine">The baseline text of the current segment.</param>
		/// <param name="transDestSeg">The translation of the destination segment.</param>
		/// <param name="adjacentSegment">An adjacent segment (usually the following one,
		/// but sometimes the preceding one) that can be used to determine which writing
		/// systems have free translations that should get the ORCs in the event that the
		/// destination segement doesn't have any free translations.</param>
		/// <param name="appendOrc">if set to <c>true</c> the ORCs in the baseline will be
		/// appended to the translation, <c>false</c> to prepend to the translation.</param>
		/// <param name="fNeedLeadingSpace">If set to <c>true</c> add a space before the ORC
		/// if there isn't already a space there.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddOrcs(ITsString curSegBaseLine, IMultiString transDestSeg,
			ISegment adjacentSegment, bool appendOrc, bool fNeedLeadingSpace)
		{
			int[] availableWsIds = (transDestSeg.StringCount > 0 || adjacentSegment == null) ?
				transDestSeg.AvailableWritingSystemIds : adjacentSegment.FreeTranslation.AvailableWritingSystemIds;
			foreach (int ws in availableWsIds)
			{
				ITsStrBldr transBldr = transDestSeg.get_String(ws).GetBldr();
				if (fNeedLeadingSpace && transBldr.Length > 0 && !transBldr.Text.EndsWith(" ", StringComparison.Ordinal))
					transBldr.Replace(transBldr.Length, transBldr.Length, " ", null);
				AddOrcsInBaseLineToBldr(transBldr, curSegBaseLine, ws, appendOrc);
				transDestSeg.set_String(ws, transBldr.GetString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates punctuation form analyses for all of the ORCs in the specified baseLine string.
		/// </summary>
		/// <param name="segment">The segment to which we are going to add the analyses.</param>
		/// <param name="baseLine">The base line of the original segment from which we are moving
		/// the ORCs.</param>
		/// <param name="para">The paragraph that owns the given segment.</param>
		/// <param name="iInsertPos">Index (0-based) in the sequence of analyses where the ORC
		/// punctuation form(s) should be inserted.</param>
		/// ------------------------------------------------------------------------------------
		private static void CreatePuncFormsInSegmentForOrcs(ISegment segment, ITsString baseLine,
			IScrTxtPara para, int iInsertPos)
		{
			if (!para.SegmentsOS.Any(seg => seg.AnalysesRS.Count > 0))
				return;

			for (int i = 0; i < baseLine.RunCount; i++)
			{
				if (baseLine.get_IsRunOrc(i))
				{
					IPunctuationForm orcForm = para.Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
					orcForm.Form = baseLine.GetSubstring(baseLine.get_MinOfRun(i), baseLine.get_LimOfRun(i));
					segment.AnalysesRS.Insert(iInsertPos++, orcForm);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds any ORCs that are found in the specified baseline string to the specified
		/// builder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void AddOrcsInBaseLineToBldr(ITsStrBldr bldr, ITsString baseLine,
			int newOrcWs, bool appendOrc)
		{
			int insertedOrcCount = 0;
			for (int i = 0; i < baseLine.RunCount; i++)
			{
				if (baseLine.get_IsRunOrc(i))
				{
					// Copy the ORC making sure to change any owned footnotes to unowned and
					// changing the WS to the WS of the back translation.
					ITsPropsBldr propsBldr = baseLine.get_Properties(i).GetBldr();
					propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, newOrcWs);
					int runContainingNewOrc = (appendOrc && bldr.Length > 0) ? bldr.RunCount :
						insertedOrcCount;
					int orcPos = appendOrc ? bldr.Length : insertedOrcCount;
					bldr.Replace(orcPos, orcPos, StringUtils.kszObject, propsBldr.GetTextProps());
					StringUtils.TurnOwnedOrcIntoUnownedOrc(bldr, runContainingNewOrc);
					insertedOrcCount++;
				}
			}
		}
		#endregion

		#region Methods to mark styles in use
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that all styles that are used in paragraphs are marked as InUse
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		private static void EnsureStylesInUseSetForScrParas(FdoCache cache)
		{
			IScripture scr = cache.LanguageProject.TranslatedScriptureOA;
			if (scr == null || scr.FixedStylesInUse)
				return; // Has already been done or not needed

			foreach (IScrTxtPara para in cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().AllInstances())
				EnsureStylesInUseSetForPara(para, scr);

			((Scripture)scr).FixedStylesInUse = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that all styles that are used in the specified paragraph are marked as InUse
		/// </summary>
		/// <param name="para">The paragraph to scan for used styles.</param>
		/// <param name="scr">Scripture.</param>
		/// ------------------------------------------------------------------------------------
		private static void EnsureStylesInUseSetForPara(IScrTxtPara para, IScripture scr)
		{
			IStStyle paraStyle = scr.FindStyle(para.StyleName);
			((StStyle)paraStyle).InUse = true;

			ITsString paraContents = para.Contents;
			for (int iRun = 0; iRun < paraContents.RunCount; iRun++)
			{
				string charStyleName = paraContents.get_StringProperty(iRun, (int)FwTextPropType.ktptNamedStyle);
				if (!string.IsNullOrEmpty(charStyleName))
				{
					IStStyle charStyle = scr.FindStyle(charStyleName);
					if (charStyle == null)
					{
						// FWR-2594: Converted FW 6.0 project can contain runs with styles that were deleted
						// cleanest fix was to recreate the missing style and allow the user to clean up the text
						// later.
						Logger.WriteEvent("EnsureStylesInUseSetForPara: Deleted style (" + charStyleName + ") still in use, recreated it.");
						charStyle = para.Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create(scr.StylesOC, charStyleName,
							ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true, 0, false);
					}
					((StStyle)charStyle).InUse = true;
				}
			}
		}
		#endregion
	}
}
