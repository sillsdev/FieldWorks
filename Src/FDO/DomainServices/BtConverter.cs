using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// This class handles converting the CmTranslation-style back translation to the interlinear.
	/// </summary>
	internal sealed class BtConverter
	{
		private List<SegGroup> m_segGroups;
		private IList<ISegment> m_segments; // The corresponding actual segments.
		private IList<TsStringSegment> m_BtSegs; // How the BT would break into segments if we did that.
		private readonly Set<int> m_labelSegIndexes = new Set<int>(); // indexes into m_segments that are (verse/chapter) labels.
		private readonly IStTxtPara m_para;
		private readonly FdoCache m_cache;
		private readonly ILgCharacterPropertyEngine m_cpe;
		private readonly IScripture m_scr;
		private readonly int m_wsBt;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one for converting the specified paragraph.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="wsBt">The writing system for which to do the conversion.</param>
		/// ------------------------------------------------------------------------------------
		private BtConverter(IStTxtPara para, int wsBt)
		{
			m_para = para;
			m_cache = para.Cache;
			m_cpe = m_cache.ServiceLocator.UnicodeCharProps;
			m_scr = para.Cache.LangProject.TranslatedScriptureOA;
			m_wsBt = wsBt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the CmTranslation version of the translation to the interlinear version.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="wsBt">The writing system for which to do the conversion.</param>
		/// ------------------------------------------------------------------------------------
		public static void ConvertCmTransToInterlin(IStTxtPara para, int wsBt)
		{
			BtConverter converter = new BtConverter(para, wsBt);
			BackTranslationAndFreeTranslationUpdateHelper.Do(para,
				converter.ConvertCmTransToInterlin);
		}

		private void ConvertCmTransToInterlin()
		{
			GetMainParaSegments();
			GetOriginalBtSegments();
			BuildLabelSegmentSet();
			MakeGroups();
			for (int igroup = 0; igroup < m_segGroups.Count; igroup++)
			{
				MakeSegmentBtsForGroup(igroup);
			}
		}

		/// <summary>
		/// Make the necessary Free Translation annotation comments for the specified group
		/// of roughly matching segments. In general, we transfer corresponding material from
		/// the BT segment to the FT annotation of the main paragraph segment, with any left-over
		/// paragraph segments blank, and any left-over BT segments added to the end of the
		/// last FT segment. As a special case, if some FTs at the end of the list already have the
		/// exact text of corresponding BT ones (counting from the end of the lists), we assume that
		/// those pairs correspond, even if not in corresponding positions counting from the start,
		/// and don't change them. This helps preserve alignment when something may merge or split
		/// a segment in the base text after much of it is annotated.
		/// </summary>
		/// <param name="igroup"></param>
		private void MakeSegmentBtsForGroup(int igroup)
		{
			SegGroup group = m_segGroups[igroup];
			// Discard any items at the end of both lists which already match exactly. Leave at least one in each group.
			DiscardMatchingSegsAtEnd(group);
			// The remaining (often all) segments are transferred one-for-one. (We don't need to check for
			// exactly matching ones at the start, because in that case, copying will be a no-op.)
			for (int iParaSeg = 0; iParaSeg < group.ParaSegs.Count; iParaSeg++)
			{
				// We may assume hvoFt is non-zero, because LoadSegmentFreeTranslations ensures every segment has one.
				var seg = group.ParaSegs[iParaSeg];
				if (iParaSeg >= group.BtSegs.Count)
				{
					// no more Bt segments, make the annotation FT empty (in case set previously).
					// But don't overwrite if unchanged, since PropChanged can do a good deal of work
					// and sometimes destroy selections we want to preserve.
					if (seg.FreeTranslation.get_String(m_wsBt).Length != 0)
						seg.FreeTranslation.set_String(m_wsBt, string.Empty);
					continue;
				}
				ITsString tssFt = group.BtSegs[iParaSeg].String;
				tssFt = InsertOrphanBtFromPreviousGroup(igroup, iParaSeg, tssFt);
				tssFt = AppendLeftoverBtToLastSeg(group, iParaSeg, tssFt);
				// But don't overwrite if unchanged, since PropChanged can do a good deal of work
				// and sometimes destroy selections we want to preserve.
				if (!seg.FreeTranslation.get_String(m_wsBt).Equals(tssFt))
					seg.FreeTranslation.set_String(m_wsBt, tssFt);
			}
		}

		// Discard any items at the end of both lists which already match exactly. Leave at least one in each group.
		private void DiscardMatchingSegsAtEnd(SegGroup group)
		{
			int ipara = group.ParaSegs.Count - 1;
			int itrans = group.BtSegs.Count - 1;
			while (ipara > 0 && itrans > 0)
			{
				// See if the existing FT matches
				string desiredFT = group.BtSegs[itrans].Text;
				var seg = group.ParaSegs[ipara];
				string currentFT = seg.FreeTranslation.get_String(m_wsBt).Text;
				if (desiredFT != currentFT)
					break;
				// The two last items are already identical, don't need to do anything more about them.
				group.BtSegs.RemoveAt(itrans);
				group.ParaSegs.RemoveAt(ipara);
				itrans--;
				ipara--;
			}
		}

		/// <summary>
		/// If this is the first segment in the group and NOT the first group, check for the possibility that
		/// there was no segment to attach the previous group's BTs to. I think this can only
		/// happen for the first group. Put the orphan BT at the start of this segment so it isn't lost.
		/// </summary>
		/// <param name="igroup"></param>
		/// <param name="iParaSeg"></param>
		/// <param name="tssFt"></param>
		/// <returns></returns>
		private ITsString InsertOrphanBtFromPreviousGroup(int igroup, int iParaSeg, ITsString tssFt)
		{
			if (iParaSeg == 0 && igroup > 0 && m_segGroups[igroup - 1].ParaSegs.Count == 0)
			{
				ITsStrBldr bldr = TsStrBldrClass.Create();
				SegGroup prevGroup = m_segGroups[igroup - 1];
				foreach (TsStringSegment seg in prevGroup.BtSegs)
					AppendWithOptionalSpace(bldr, seg.String);

				AppendWithOptionalSpace(bldr, tssFt);
				tssFt = bldr.GetString();
			}
			return tssFt;
		}

		/// <summary>
		/// If this paragraph segment is the last one for its group, but there are left-over BT segments,
		/// append them to this.
		/// </summary>
		/// <param name="group"></param>
		/// <param name="iParaSeg"></param>
		/// <param name="tssFt"></param>
		/// <returns></returns>
		private ITsString AppendLeftoverBtToLastSeg(SegGroup group, int iParaSeg, ITsString tssFt)
		{
			if (iParaSeg == group.ParaSegs.Count - 1 && iParaSeg < group.BtSegs.Count - 1)
			{
				// We have left over translations. Append them.
				ITsStrBldr bldr = tssFt.GetBldr();
				for (int j = iParaSeg + 1; j < group.BtSegs.Count; j++)
					AppendWithOptionalSpace(bldr, group.BtSegs[j].String);
				tssFt = bldr.GetString();
			}
			return tssFt;
		}

		/// <summary>
		/// Append tssApp to the builder. If both are non-empty and there is no white space
		/// at the end of bldr or the start of tssApp, also add a space.
		/// </summary>
		/// <param name="bldr"></param>
		/// <param name="tssApp"></param>
		private void AppendWithOptionalSpace(ITsStrBldr bldr, ITsString tssApp)
		{
			if (tssApp.Length == 0)
				return;
			// Insert a space if needed.
			if (bldr.Length > 0)
			{
				string lastChar = bldr.GetChars(bldr.Length - 1, bldr.Length);
				if (!IsWhite(lastChar[0]))
				{
					// No space at end of builder...check start of stuff to append
					string firstChar = tssApp.GetChars(0, 1);
					if(!IsWhite(firstChar[0]))
					{
						bldr.Replace(bldr.Length, bldr.Length, " ", null);
					}
				}
			}
			bldr.ReplaceTsString(bldr.Length, bldr.Length, tssApp);
		}

		bool IsWhite(char c)
		{
			LgGeneralCharCategory cc = m_cpe.get_GeneralCategory(c);
			return cc == LgGeneralCharCategory.kccZs;
		}

		// Get the segments of the paragraph, making sure they are real and have at least empty (but real)
		// FF annotations.
		private void GetMainParaSegments()
		{
			m_segments = SegmentServices.GetMainParaSegments(m_para);
		}

		private void BuildLabelSegmentSet()
		{
			for (int i = 0; i < m_segments.Count; i++)
			{
				if (m_segments[i].IsLabel)
					m_labelSegIndexes.Add(i);
			}
		}

		// Segment the current CmTranslation back translation that we want to convert, using the same algorithm
		// as for the main paragraph contents, but not actually making segment annotations.
		private void GetOriginalBtSegments()
		{
			ICmTranslation translation = m_para.GetBT();
			if (translation == null)
			{
				// No existing translation, can't have any segments.
				m_BtSegs = new List<TsStringSegment>();
			}
			else
			{
				ITsString trans = translation.Translation.get_String(m_wsBt);
				m_BtSegs = trans.GetSegments(m_para.Cache.WritingSystemFactory);
			}
		}

		// Break both lists of segments up into corresponding groups. Each group starts with the segment following
		// a CV label that is common to the paragraph and the BT, and continues (in each sequence) until another
		// such common label segment. (The two lists may therefore include some labels that don't match, and may
		// be unequal in length.) There may also be one group for the stuff before the first matching label.
		// If there is a non-trivial start group in the paragraph, it corresponds to the similar group in the BT.
		// If not, any leading BT is inserted at the start of the first group.
		private void MakeGroups()
		{
			m_segGroups = new List<SegGroup>();
			int iStartSegBt = 0;
			for (int iStartSegPara = 0; iStartSegPara < m_segments.Count;)
			{
				iStartSegPara = MakeGroup(iStartSegPara, iStartSegBt, out iStartSegBt);
			}
		}

		/// <summary>
		/// We have determined that corresponding groups start at iStartSegPara in m_paraSegs/m_segments and
		/// iStartSegBt in m_BtSegs. Make a SegGroup out of the corresponding segments and return the
		/// index of the indexes of the starts of the next group (in each case one more than the index of
		/// the matching verse segment which ended the group in the paragraph sequence). One or both indexes might
		/// be greater than the length of the corresponding array, indicating that the group extends to the end.
		/// One or both parts of the group might be empty.
		/// </summary>
		/// <returns>start index of next para segment (and next BT one in startOfNextBtSeg)</returns>
		int MakeGroup(int iStartSegPara, int iStartSegBt, out int startOfNextBtSeg)
		{
			SegGroup group = new SegGroup();
			m_segGroups.Add(group);
			int iLimSegPara = iStartSegPara;
			startOfNextBtSeg = -1;
			for (; iLimSegPara < m_segments.Count; iLimSegPara++)
			{
				// The group also ends here if it is a CV-styled run and we can find a matching one in the BT.
				if (m_labelSegIndexes.Contains(iLimSegPara))
				{
					var seg = m_segments[iLimSegPara];
					ITsString targetT = ConvertedBtLabel(seg);
					startOfNextBtSeg = IndexOfMatchingVerseSegInBt(targetT, iStartSegBt);
					if (startOfNextBtSeg < 0)
					{
						// We failed to find a match at the location we expected, so just try search the whole BT
						startOfNextBtSeg = IndexOfMatchingVerseSegInBt(targetT, 0);
					}
					if (startOfNextBtSeg >= 0)
					{
						startOfNextBtSeg++; // actual contents starts AFTER the common label
						break;
					}
				}
			}
			// Make the group's ParaSegs be the non-label segments from the range we decided.
			var paraSegs = new List<ISegment>(iLimSegPara - iStartSegPara);
			for (int i = iStartSegPara; i < iLimSegPara; i++)
			{
				if (!m_labelSegIndexes.Contains(i))
					paraSegs.Add(m_segments[i]);
			}
			group.ParaSegs = paraSegs;
			group.BtSegs = GetSegGroup(iStartSegBt);
			return iLimSegPara + 1;
		}

		/// <summary>
		/// Given a segment which points into your paragraph, return the string that should correspond to it in the BT.
		/// </summary>
		/// <param name="seg"></param>
		/// <returns></returns>
		ITsString ConvertedBtLabel(ISegment seg)
		{
			return m_scr.ConvertCVNumbersInStringForBT(seg.BaselineText, m_wsBt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a target verse label, see if there is a matching verse label in the BT. If so,
		/// return its index; otherwise, return -1.
		/// </summary>
		/// <param name="target">Text to search for</param>
		/// <param name="iSegMin">The segment where searching should begin</param>
		/// ------------------------------------------------------------------------------------
		private int IndexOfMatchingVerseSegInBt(ITsString target, int iSegMin)
		{
			for (int iTrans = iSegMin; iTrans < m_BtSegs.Count; iTrans++)
			{
				ITsString segText = m_BtSegs[iTrans].String;
				if (SegmentBreaker.HasLabelText(segText) && StringsEndWithSameWord(segText, target))
					return iTrans;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a group of BT segments starting at the specified index and continuing up to,
		/// but not including, the first label segment (e.g., a chapter or verse number) that
		/// corresponds to something in the main paragraph. This can return an empty list if
		/// segment iStartTrans itself is a label segment. Label segments are not included,
		/// though ones that don't match anything in
		/// the paragraph may be skipped over.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<TsStringSegment> GetSegGroup(int iStartTrans)
		{
			int iLimTrans;
			for (iLimTrans = iStartTrans; iLimTrans < m_BtSegs.Count; iLimTrans++)
			{
				ITsString tssSeg = m_BtSegs[iLimTrans].String;
				if (SegmentBreaker.HasLabelText(tssSeg) && MatchesSomeParaLabel(tssSeg))
				{
					break;
				}
			}
			List<TsStringSegment> result = new List<TsStringSegment>(iLimTrans - iStartTrans);
			for (int i = iStartTrans; i < iLimTrans; i++)
			{
				ITsString tssSeg = m_BtSegs[i].String;
				if (!SegmentBreaker.HasLabelText(tssSeg))
				{
					result.Add(m_BtSegs[i]);
				}
			}
			return result;
		}

		/// <summary>
		/// Answer true if the specified string is equal to some label segment of the paragraph
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		private bool MatchesSomeParaLabel(ITsString label)
		{
			for (int i = 0; i < m_segments.Count; i++)
			{
				if (m_labelSegIndexes.Contains(i))
				{
					if (StringsEndWithSameWord(ConvertedBtLabel(m_segments[i]), label))
						return true;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the two strings end with the same word (i.e., same text and same
		/// style). Any white-space-only runs are ignored. The fundamental purpose for this
		/// method is to find a segment in the BT marked with the same chapter and/or verse
		/// number as the vernacular. The entire label string won't necessarily match because:
		/// a) whitespace differences, b) writing system differences, and c) verse numbers
		/// combined in a single label segment because there is no verse text (usually in the BT).
		/// </summary>
		/// <param name="firstTss">The first string (typically the BT).</param>
		/// <param name="secondTss">The second string (typically the vernacular).</param>
		/// ------------------------------------------------------------------------------------
		internal static bool StringsEndWithSameWord(ITsString firstTss, ITsString secondTss)
		{
			TsRunPart lastWordOfFirst = firstTss.LastWord();
			TsRunPart lastWordOfSecond = secondTss.LastWord();
			return (lastWordOfFirst != null && lastWordOfSecond != null &&
				lastWordOfFirst.Text == lastWordOfSecond.Text &&
				lastWordOfFirst.Props.Style() == lastWordOfSecond.Props.Style());
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class represents the segments of a single verse (or chapter)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class SegGroup
	{
		internal IList<ISegment> ParaSegs; // The segments of the main paragraph.
		internal IList<TsStringSegment> BtSegs; // How the BT would break into segments if we did that.
	}
}