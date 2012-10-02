using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class handles converting the CmTranslation-style back translation to the interlinear.
	/// </summary>
	public class BtConverter
	{
		private IStTxtPara m_para;
		private int kflidFT;
		private int kflidSegments;
		private FdoCache m_cache;

		private int[] m_paraSegs; // The segments (HVOs) of the main paragraph.
		private ICmBaseAnnotation[] m_segments; // The corresponding actual segments.
		private List<TsStringSegment> m_BtSegs; // How the BT would break into segments if we did that.
		Set<int> m_labelSegIndexes = new Set<int>(); // indexes into m_paraSegs of segments that are (verse/chapter) labels.

		private List<SegGroup> m_segGroups;

		private ILgCharacterPropertyEngine m_cpe;

		private IScripture m_scr;

		private int m_wsBt;
		/// <summary>
		/// Make one for converting the specified paragraph.
		/// </summary>
		/// <param name="para"></param>
		public BtConverter(IStTxtPara para)
		{
			m_para = para;
			m_cache = para.Cache;
			kflidFT = StTxtPara.SegmentFreeTranslationFlid(m_cache);
			kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			m_cpe = m_cache.LanguageWritingSystemFactoryAccessor.UnicodeCharProps;
			m_scr = para.Cache.LangProject.TranslatedScriptureOA;
		}

		/// <summary>
		/// Convert the CmTranslation version of the translation to the interlinear version.
		/// </summary>
		public void ConvertCmTransToInterlin(int wsBt)
		{
			using (new EditMonitor.DisableEditMonitors())
			{
				m_wsBt = wsBt;
				GetMainParaSegments();
				GetOriginalBtSegments();
				BuildLabelSegmentSet();
				MakeGroups();
				for (int igroup = 0; igroup < m_segGroups.Count; igroup++)
				{
					MakeSegmentBtsForGroup(igroup);
				}
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
				int hvoFt = m_cache.GetObjProperty(group.ParaSegs[iParaSeg], kflidFT);
				// We may assume hvoFt is non-zero, because LoadSegmentFreeTranslations ensures every segment has one.
				CmIndirectAnnotation ft = CmObject.CreateFromDBObject(m_cache, hvoFt) as CmIndirectAnnotation;
				if (iParaSeg >= group.BtSegs.Count)
				{
					// no more Bt segments, make the annotation FT empty (in case set previously).
					// But don't overwrite if unchanged, since PropChanged can do a good deal of work
					// and sometimes destroy selections we want to preserve.
					if (ft.Comment.GetAlternative(m_wsBt).Length != 0)
						ft.Comment.SetAlternative("", m_wsBt);
					continue;
				}
				ITsString tssFt = group.BtSegs[iParaSeg].Text;
				tssFt = InsertOrphanBtFromPreviousGroup(igroup, iParaSeg, tssFt);
				tssFt = AppendLeftoverBtToLastSeg(group, iParaSeg, tssFt);
				// But don't overwrite if unchanged, since PropChanged can do a good deal of work
				// and sometimes destroy selections we want to preserve.
				if (!ft.Comment.GetAlternativeTss(m_wsBt).Equals(tssFt))
					ft.Comment.SetAlternative(tssFt, m_wsBt);
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
				string desiredFT = group.BtSegs[itrans].Text.Text;
				int hvoFt = m_cache.GetObjProperty(group.ParaSegs[ipara], kflidFT);
				CmIndirectAnnotation ft = CmObject.CreateFromDBObject(m_cache, hvoFt) as CmIndirectAnnotation;
				string currentFT = ft.Comment.GetAlternative(m_wsBt).Text;
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
				for (int j = 0; j < prevGroup.BtSegs.Count; j++)
				{
					AppendWithOptionalSpace(bldr, prevGroup.BtSegs[j].Text);
				}
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
				{
					ITsString tssApp = group.BtSegs[j].Text;
					AppendWithOptionalSpace(bldr, tssApp);
				}
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
			m_segments = GetMainParaSegments(m_para, m_wsBt, out m_paraSegs);
		}

		/// <summary>
		/// Get the segments of the paragraph.  This is public static to allow others to use
		/// the same code.  This will actually parse the text of the paragraph, create any
		/// segments that do not yet exist, and create any needed free translation annotations
		/// to go with them.  It also sets the kflidSegments (virtual) property of the paragraph,
		/// and the kflidFT (virtual) property of the segments.
		/// </summary>
		/// <returns>array of ICmBaseAnnotation objects for the segments</returns>
		public static ICmBaseAnnotation[] GetMainParaSegments(IStTxtPara para, int wsBt, out int[] paraSegs)
		{
			FdoCache cache = EnsureMainParaSegments(para, wsBt);
			int kflidSegments = StTxtPara.SegmentsFlid(cache);
			paraSegs = cache.GetVectorProperty(para.Hvo, kflidSegments, true);
			ICmBaseAnnotation[] segments = new ICmBaseAnnotation[paraSegs.Length];
			for (int i = 0; i < paraSegs.Length; i++)
			{
				// This prevents trying to really load it from the database, which is typically not
				// useful and actully causes failures of some tests when using a memory cache that considers all
				// objects to be non-dummies.
				cache.VwCacheDaAccessor.CacheIntProp(paraSegs[i], (int)CmObjectFields.kflidCmObject_Class,
					(int)CmBaseAnnotation.kclsidCmBaseAnnotation);
				segments[i] = new CmBaseAnnotation(cache, paraSegs[i]);
			}
			return segments;
		}

		/// <summary>
		/// Ensure that the segments property of the paragraph is consistent with its contents and consists of real
		/// database objects.
		/// </summary>
		internal static FdoCache EnsureMainParaSegments(IStTxtPara para, int wsBt)
		{
			ParagraphParser pp = new ParagraphParser(para);
			List<int> EosOffsets;
			List<int> segs = pp.CollectSegmentAnnotationsOfPara(out EosOffsets);
			// Make sure the segments list is up to date.
			FdoCache cache = para.Cache;
			cache.VwCacheDaAccessor.CacheVecProp(para.Hvo, StTxtPara.SegmentsFlid(cache), segs.ToArray(), segs.Count);
			// This further makes sure all are real.
			StTxtPara.LoadSegmentFreeTranslations(new int[] { para.Hvo }, cache, wsBt);
			return cache;
		}

		private void BuildLabelSegmentSet()
		{
			for (int i = 0; i < m_segments.Length; i++)
			{
				if (IsLabelSeg(m_segments[i]))
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
				ITsString trans = translation.Translation.GetAlternativeTss(m_wsBt);
				SegmentCollector collector = new SegmentCollector(trans, m_para.Cache.LanguageWritingSystemFactoryAccessor);
				collector.Run();
				m_BtSegs = collector.Segments;
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
			for (int iStartSegPara = 0; iStartSegPara < m_segments.Length;)
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
			for (; iLimSegPara < m_paraSegs.Length; iLimSegPara++)
			{
				// The group also ends here if it is a CV-styled run and we can find a matching one in the BT.
				if (m_labelSegIndexes.Contains(iLimSegPara))
				{
					ICmBaseAnnotation seg = m_segments[iLimSegPara];
					string targetT = ConvertedBtLabel(seg);
					startOfNextBtSeg = IndexOfMatchingVerseInBt(targetT);
					if (startOfNextBtSeg >= 0)
					{
						startOfNextBtSeg++; // actual contents starts AFTER the common label
						break;
					}
				}
			}
			// Make the group's ParaSegs be the non-label segments from the range we decided.
			List<int> paraSegs = new List<int>(iLimSegPara - iStartSegPara);
			for (int i = iStartSegPara; i < iLimSegPara; i++)
			{
				if (!m_labelSegIndexes.Contains(i))
					paraSegs.Add(m_paraSegs[i]);
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
		string ConvertedBtLabel(ICmBaseAnnotation seg)
		{
			ITsString tssResult = m_scr.ConvertCVNumbersInStringForBT((seg as CmBaseAnnotation).TextAnnotated, m_wsBt);
			if (tssResult == null)
				return null; // paranoia.
			return tssResult.Text;
		}

		/// <summary>
		/// Given a target verse label, see if there is a matching verse label in the BT. If so, return its index;
		/// otherwise, return -1.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		int IndexOfMatchingVerseInBt(string target)
		{
			for (int iTrans = 0; iTrans < m_BtSegs.Count; iTrans++)
			{
				ITsString segText = m_BtSegs[iTrans].Text;
				if (StringsEqualExceptSpace(segText.Text, target) && SegmentBreaker.HasLabelText(segText, 0, segText.Length))
					return iTrans;
			}
			return -1;
		}

		/// <summary>
		/// Find a group of BT segments starting at the specified index and continuing up to, but not including,
		/// the first CV-style segment that corresponds to something in the main paragraph. May return an empty
		/// list if segment iStartTrans itself is such a segment. Label segments are not included, though ones
		/// that don't match anything in the paragraph may be skipped over.
		/// </summary>
		private List<TsStringSegment> GetSegGroup(int iStartTrans)
		{
			int iLimTrans = iStartTrans;
			for (; iLimTrans < m_BtSegs.Count; iLimTrans++)
			{
				ITsString tssSeg = m_BtSegs[iLimTrans].Text;
				if (SegmentBreaker.HasLabelText(tssSeg, 0, tssSeg.Length) && MatchesSomeParaLabel(tssSeg.Text))
				{
					break;
				}
			}
			List<TsStringSegment> result = new List<TsStringSegment>(iLimTrans - iStartTrans);
			for (int i = iStartTrans; i < iLimTrans; i++)
			{
				ITsString tssSeg = m_BtSegs[i].Text;
				if (!SegmentBreaker.HasLabelText(tssSeg, 0, tssSeg.Length))
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
		private bool MatchesSomeParaLabel(string label)
		{
			for (int i = 0; i < m_segments.Length; i++)
			{
				if (m_labelSegIndexes.Contains(i))
				{
					if (StringsEqualExceptSpace(ConvertedBtLabel(m_segments[i]), label))
						return true;
				}
			}
			return false;
		}

		// Answer true if the strings are equal, except for white space.
		private bool StringsEqualExceptSpace(string first, string second)
		{
			return StringsEqualExceptSpace(first, second, m_cpe);
		}
		/// <summary>
		/// Pull out as internal static for testing.
		/// </summary>
		internal static bool StringsEqualExceptSpace(string first, string second, ILgCharacterPropertyEngine cpe)
		{
			int ichFirst = 0;
			int ichSecond = 0;
			for ( ; ; )
			{
				char c1 = '\0';
				if (ichFirst < first.Length)
				{
					c1 = first[ichFirst];
					if (cpe.get_GeneralCategory(c1) == LgGeneralCharCategory.kccZs)
					{
						ichFirst++;
						continue; // skip space in first
					}
				}
				if (ichSecond < second.Length)
				{
					char c2 = second[ichSecond];
					if (cpe.get_GeneralCategory(c2) == LgGeneralCharCategory.kccZs)
					{
						ichSecond++;
						continue; // skip space in second
					}
					if (ichFirst >= first.Length)
						return false; // second has non-white, first has no more

					if (c1 != c2)
						return false; // corresponding non-white characters not equal
					ichFirst++;
					ichSecond++;
					continue; // current characters match, move on.
				}
				if (ichFirst < first.Length)
					return false; // at end of second, and first has a non-white character left
				return true; // at end of both
			}
		}

		private bool IsLabelSeg(ICmBaseAnnotation seg)
		{
			return SegmentBreaker.HasLabelText(m_para.Contents.UnderlyingTsString, seg.BeginOffset, seg.EndOffset);
		}

	}

	/// <summary>
	/// This class represents the segments of a single verse (or chapter)
	/// </summary>
	class SegGroup
	{
		internal List<int> ParaSegs; // The segments (HVOs) of the main paragraph.
		internal List<TsStringSegment> BtSegs; // How the BT would break into segments if we did that.
	}
}
