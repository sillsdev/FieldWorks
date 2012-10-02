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
// File: StTxtPara.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Cellar
{
	#region Interface IObjectMetaInfoProvider
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is a cool little interface needed by CreateOwnedObjects to provide some info that
	/// could be mildy application-specific
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IObjectMetaInfoProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the picture folder to use for any copied pictures
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string PictureFolder {get;}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the next 0-based footnote index to use for the first footnote being created
		/// in the given paragraph at or after the given char location.
		/// This should be called only for the first foonote; the caller is expected to properly
		/// insert subsequent footnotes correctly if multiple contiguous footnotes are to be
		/// inserted.
		/// </summary>
		/// <param name="para">paragraph to start looking in</param>
		/// <param name="ich">offset in paragraph to start looking before</param>
		/// ------------------------------------------------------------------------------------
		int NextFootnoteIndex(StTxtPara para, int ich);
		// NB: Using IStTxtPara causes Mocks to fail on tests.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the character style to use for footnote markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string FootnoteMarkerStyle {get;}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Structured Text Paragraph.
	/// </summary>
	/// <remarks>For now, at least, this manually implemented subclass of the generated base
	/// class is needed only to support error beeping.</remarks>
	/// ----------------------------------------------------------------------------------------
	public partial class StTxtPara
	{
		/// <summary>
		/// </summary>
		public class CmBaseAnnotationInfo
		{
			/// <summary>
			/// </summary>
			protected FdoCache m_fdoCache;
			/// <summary>
			/// </summary>
			protected int m_hvoCba = 0;
			/// <summary>
			/// </summary>
			protected ICmBaseAnnotation m_cbaObj = null;

			/// <summary>
			/// state information for annotation.
			/// </summary>
			protected int m_hvoPara = 0;
			/// <summary>
			/// </summary>
			protected int m_flid = 0;
			/// <summary>
			/// </summary>
			protected int m_hvoInstanceOf = 0;
			/// <summary>
			/// </summary>
			protected int m_beginOffset = -1;
			/// <summary>
			/// </summary>
			protected int m_endOffset = -1;

			/// <summary>
			/// </summary>
			/// <param name="cache"></param>
			/// <param name="hvoCba"></param>
			public CmBaseAnnotationInfo(FdoCache cache, int hvoCba)
			{
				m_fdoCache = cache;
				m_hvoCba = hvoCba;
			}

			/// <summary>
			/// Returns the annotation as an object.
			/// </summary>
			public ICmBaseAnnotation Object
			{
				get
				{
					if (m_cbaObj == null)
						m_cbaObj = CmBaseAnnotation.CreateFromDBObject(m_fdoCache, m_hvoCba);
					return m_cbaObj;
				}
			}
		}

		/// <summary>
		/// </summary>
		public class SegmentInfo : CmBaseAnnotationInfo
		{
			/// <summary>
			/// Get segment information for the given annotation.
			/// </summary>
			/// <param name="cache"></param>
			/// <param name="hvoCba"></param>
			public SegmentInfo(FdoCache cache, int hvoCba)
				: base(cache, hvoCba)
			{
			}

			/// <summary>
			/// Return the segment range in terms of the segment indexes of the BeginObject segment property of the given hvoCba.
			/// </summary>
			/// <param name="cache"></param>
			/// <param name="hvoCba"></param>
			/// <param name="iBeginSegment">zero-based index into segments</param>
			/// <param name="iEndSegment">zero-based into segments</param>
			/// <returns></returns>
			static public bool TryGetSegmentRange(FdoCache cache, int hvoCba,
				out int iBeginSegment, out int iEndSegment)
			{
				iBeginSegment = -1;
				iEndSegment = -1;
				int[] segments = null;
				// if it's a twfic type, then use twfic info, otherwise find a segment range.
				ICmBaseAnnotation cba = CmBaseAnnotation.CreateFromDBObject(cache, hvoCba);
				int hvoPara = cba.BeginObjectRAHvo;
				IVwVirtualHandler vh;
				// first try to get the segments from our Segments virtual handler
				// if that has not been loaded for the paragraph, try SegmentsIgnoreTwfics
				// to save time.
				if (cache.TryGetVirtualHandler(StTxtPara.SegmentsFlid(cache), out vh) &&
					(vh as BaseFDOPropertyVirtualHandler).IsPropInCache(cache.MainCacheAccessor, hvoPara, 0))
				{
					if (cba.AnnotationTypeRAHvo != 0)
					{
						StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(cache, hvoCba);
						if (twficInfo.SegmentIndex >= 0)
						{
							iBeginSegment = twficInfo.SegmentIndex;		// return zero-based index.
						}
						else if (!cache.IsDummyObject(hvoCba))
						{
							int segIndexLogical;
							string sql = string.Format("exec GetSegmentIndex {0}, {1}", hvoCba, CmAnnotationDefn.TextSegment(cache).Hvo);
							DbOps.ReadOneIntFromCommand(cache, sql, null, out segIndexLogical);
							if (segIndexLogical > 0)
								iBeginSegment = segIndexLogical - 1;	 // subtract 1 to get a zero-based index.
						}
					}
					if (iBeginSegment < 0)
						segments = cache.GetVectorProperty(hvoPara, vh.Tag, true);
				}
				else if (cache.TryGetVirtualHandler(StTxtPara.SegmentsIgnoreTwficsFlid(cache), out vh))
				{
					segments = cache.GetVectorProperty(hvoPara, vh.Tag, true);
				}
				if (iBeginSegment < 0)
				{
					// this is annotation into a paragraph (but may not be a twfic or exist in SegForms).
					// if it's not a twfic, find a segment range.
					ISilDataAccess sda = cache.MainCacheAccessor;
					int ihvoSeg = 0;
					int cbaBeginOffset = cba.BeginOffset;
					int cbaEndOffset = cba.EndOffset;
					for (; ihvoSeg < segments.Length; ihvoSeg++)
					{
						int segEndOffset = sda.get_IntProp(segments[ihvoSeg], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
						if (segEndOffset <= cbaBeginOffset)
						{
							continue;
						}
						else if (iBeginSegment < 0)
						{
							iBeginSegment = ihvoSeg;
						}
						if (segEndOffset > cbaEndOffset)
						{
							iEndSegment = ihvoSeg;
							break; // we've passed our annotation's EndOffset, so the previous segment is the last one in range.
						}
					}
					// ihvoSeg should now be one index passed the segment in our range. this eguals the logical segment number.
					if (iBeginSegment >= 0 && iEndSegment == -1)
					{
						iEndSegment = ihvoSeg < segments.Length ? ihvoSeg - 1 : segments.Length - 1;
					}
				}
				return iBeginSegment >= 0;
			}
		}

		/// <summary>
		/// Encapsulate common twfic information related to the paragraph.
		/// </summary>
		public class TwficInfo : CmBaseAnnotationInfo
		{
			int m_hvoSegment = 0;
			int m_iSegment = -1;
			int m_iSegmentForm = -1;
			bool m_fIsFirstTwfic = false;

			/// <summary>
			///
			/// </summary>
			/// <param name="fdoCache"></param>
			/// <param name="hvoTwficCba"></param>
			public TwficInfo(FdoCache fdoCache, int hvoTwficCba)
				: base(fdoCache, hvoTwficCba)
			{
			}

			/// <summary>
			/// reload information for twfic
			/// </summary>
			public void ReloadInfo()
			{
				m_iSegmentForm = StTxtPara.TwficSegmentLocation(m_fdoCache, m_hvoCba, out m_hvoSegment, out m_iSegment, out m_fIsFirstTwfic);
				if (m_hvoPara != 0 && m_hvoSegment != 0)
					CaptureObjectInfo();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Reloads the info.
			/// </summary>
			/// <param name="hvoTwficCba">The HVO twfic cba.</param>
			/// --------------------------------------------------------------------------------
			public void ReloadInfo(int hvoTwficCba)
			{
				m_hvoCba = hvoTwficCba;
				m_cbaObj = null;
				ReloadInfo();
			}

			/// <summary>
			/// capture the basic information from the object, that may become invalid later.
			/// </summary>
			public void CaptureObjectInfo()
			{
				LoadTwficLocationInfoIfNeeded();
				m_hvoPara = Object.BeginObjectRAHvo;
				m_flid = Object.Flid;
				m_hvoInstanceOf = Object.InstanceOfRAHvo;
				m_beginOffset = Object.BeginOffset;
				m_endOffset = Object.EndOffset;
			}

			/// <summary>
			/// Paragraph of the twfic
			/// </summary>
			public int HvoPara
			{
				get
				{
					if (m_hvoPara == 0)
						m_hvoPara = Object.BeginObjectRAHvo;
					return m_hvoPara;
				}
			}

			/// <summary>
			///
			/// </summary>
			public int BeginOffset
			{
				get
				{
					if (m_beginOffset == -1)
						m_beginOffset = Object.BeginOffset;
					return m_beginOffset;
				}
			}

			/// <summary>
			///
			/// </summary>
			public int EndOffset
			{
				get
				{
					if (m_endOffset == -1)
						m_endOffset = Object.EndOffset;
					return m_endOffset;
				}
			}



			/// <summary>
			/// return the WfiWordform related to the InstanceOf in this twfic.
			/// </summary>
			public int HvoWfiWordform
			{
				get
				{
					if (m_hvoCba != 0)
						return WfiWordform.GetWfiWordformFromInstanceOf(m_fdoCache, m_hvoCba);
					return 0;
				}
			}

			/// <summary>
			/// make sure the basic information for our annotation still corresponds to
			/// the current state of the paragraph and its annotations.
			/// </summary>
			/// <returns></returns>
			public bool IsCapturedObjectInfoValid()
			{
				return IsIdenticalObjectInfo(this) && IsObjectValid();
			}

			/// <summary>
			/// Verifies not only is the twfic valid, but instanceOf and BeginObject.
			/// </summary>
			/// <returns></returns>
			public bool IsObjectValid()
			{
				return CmObject.IsValidObject(m_fdoCache, m_hvoCba);
			}

			private bool IsIdenticalObjectInfo(TwficInfo twficInfo)
			{
				return m_hvoPara != 0 && m_hvoInstanceOf != 0 &&
					twficInfo.Object.BeginObjectRAHvo == m_hvoPara &&
					twficInfo.Object.InstanceOfRAHvo == m_hvoInstanceOf &&
					twficInfo.Object.BeginOffset == m_beginOffset &&
					twficInfo.Object.EndOffset == m_endOffset;
			}

			/// <summary>
			/// get the guess associated with this twfic.
			/// </summary>
			/// <returns></returns>
			public int GetGuess()
			{
				return m_fdoCache.GetObjProperty(m_hvoCba, StTxtPara.TwficDefaultFlid(m_fdoCache));
			}

			/// <summary>
			/// looks in the paragraph to find an annotation that matches the information
			/// captured in CaptureObjectInfo().
			/// </summary>
			/// <returns>0 if none found</returns>
			public int FindIdenticalTwfic()
			{
				if (IsCapturedObjectInfoValid())
					return Object.Hvo;
				// otherwise look through our paragraph annotations and see
				// if we can find one corresponding to the one we've saved.
				// Enhance: we could start with the offsets in the paragraph
				// and look for an equivalent segform independent of segment
				// boundaries.
				StTxtPara para = new StTxtPara(m_fdoCache, m_hvoPara);
				List<int> segments = para.Segments;
				if (m_iSegment < 0 || m_iSegment >= segments.Count)
					return 0;
				int hvoSegment = segments[m_iSegment];
				List<int> segforms = para.SegmentForms(hvoSegment);
				if (m_iSegmentForm < 0 || m_iSegmentForm >= segforms.Count)
					return 0;
				int hvoSegform = segforms[m_iSegmentForm];
				StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(m_fdoCache, hvoSegform);
				if (this.IsIdenticalObjectInfo(twficInfo))
					return hvoSegform;
				else
					return 0;
			}

			private void LoadTwficLocationInfoIfNeeded()
			{
				if (m_hvoSegment == 0)
				{
					ReloadInfo();
				}
			}

			/// <summary>
			/// The id of the segment containing this twfic.
			/// </summary>
			public int SegmentHvo
			{
				get
				{
					LoadTwficLocationInfoIfNeeded();
					return m_hvoSegment;
				}
			}

			/// <summary>
			/// The index of the segment containing this twfic.
			/// </summary>
			public int SegmentIndex
			{
				get
				{
					LoadTwficLocationInfoIfNeeded();
					return m_iSegment;
				}
			}

			/// <summary>
			/// The index of the segmentform contained in paragraph Segments.
			/// </summary>
			public int SegmentFormIndex
			{
				get
				{
					LoadTwficLocationInfoIfNeeded();
					return m_iSegmentForm;
				}
			}

			/// <summary>
			/// True if the twfic is the first one in the segment.
			/// Useful for determining the significance of titled cases.
			/// </summary>
			public bool IsFirstTwficInSegment
			{
				get
				{
					LoadTwficLocationInfoIfNeeded();
					return m_fIsFirstTwfic;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property is used when chapter or verse numbers change in the contents of
		/// a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public const int ktagVerseNumbers = kclsidStTxtPara * 1000 + 998;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is used for making a beep when an error (e.g., bogus verse number) is
		/// introduced into a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public const int ktagErrorInPara = kclsidStTxtPara * 1000 + 999;

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a TEMPORARY property to allow beeping when we get an error
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ErrorInPara
		{
			get
			{
				bool fInCache = false;
				int val = m_cache.VwCacheDaAccessor.get_CachedIntProp(Hvo, ktagErrorInPara,
					out fInCache);
				return (fInCache ? val : 0);
			}
			set
			{
				m_cache.VwCacheDaAccessor.CacheIntProp(Hvo, ktagErrorInPara, value);
				m_cache.MainCacheAccessor.PropChanged(null,
					(int)PropChangeType.kpctNotifyAll,
					Hvo,
					ktagErrorInPara,
					0, 0, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the previous paragraph within the StText, else null if there is not one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtPara PreviousParagraph
		{
			get
			{
				int index = IndexInOwner;
				if (index == 0) // if this is the first paragraph
					return null;
				StText owningText = new StText(this.Cache, OwnerHVO);
				if (owningText == null) // if owned by a non-StText?
					return null;
				return (StTxtPara)owningText.ParagraphsOS[index - 1];
			}
		}

		/// <summary>
		/// Returns the tss of the given annotation.
		/// </summary>
		/// <param name="cba">a paragraph annotation.</param>
		/// <returns></returns>
		static public ITsString TssSubstring(ICmBaseAnnotation cba)
		{
			IStTxtPara paragraph = cba.BeginObjectRA as IStTxtPara;
			Debug.Assert(paragraph != null, String.Format("We expect cba{0} to be a paragraph annotation.", cba.Hvo));
			if (paragraph == null)
				return null;
			FdoCache cache = cba.Cache;
			return cache.MainCacheAccessor.get_StringProp(cba.Hvo, CmBaseAnnotation.StringValuePropId(cache));
		}

		/// <summary>
		/// Virtual Property: a List of CmBaseAnnotation ids denoting segment (e.g. sentences) in the paragraph.
		/// </summary>
		public List<int> Segments
		{
			get
			{
				return new List<int>(this.Cache.GetVectorProperty(this.Hvo, SegmentsFlid(Cache), true));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert all dummy segments preceeding hvoSegLim to real ones.
		/// this helps to ensure the freeform annotations will stay in place
		/// after reparsing the texts. (LT-7318)
		/// </summary>
		/// <param name="hvoSegLim"></param>
		/// ------------------------------------------------------------------------------------
		public void EnsurePreceedingSegmentsAreReal(int hvoSegLim)
		{
			foreach (int hvoParaSeg in this.Segments)
			{
				if (hvoParaSeg == hvoSegLim)
					break;	// we've arrived up to the final sentence that can have a freeform.
				if (Cache.IsDummyObject(hvoParaSeg))
				{
					CmBaseAnnotation.ConvertBaseAnnotationToReal(Cache, hvoParaSeg);
				}
			}
		}

		/// <summary>
		/// </summary>
		public List<int> SegmentsIgnoreTwfics
		{
			get
			{
				return new List<int>(this.Cache.GetVectorProperty(this.Hvo, SegmentsIgnoreTwficsFlid(Cache), true));
			}
		}

		/// <summary>
		/// get the ws at the twfic's BeginOffset in its paragraph.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="hvoTwfic">The hvo twfic.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int GetTwficWs(FdoCache cache, int hvoTwfic)
		{
			Debug.Assert(cache.IsValidObject(hvoTwfic, CmBaseAnnotation.kClassId),
				String.Format("expected valid hvo({0}).", hvoTwfic));
			if (hvoTwfic <= 0 && !cache.IsDummyObject(hvoTwfic))
				throw new ArgumentException(String.Format("expected valid hvo({0}).", hvoTwfic));
			int hvoPara = cache.GetObjProperty(hvoTwfic,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			int ichBeginOffset = cache.GetIntProperty(hvoTwfic,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
			int ws = GetWsAtParaOffset(cache, hvoPara, ichBeginOffset);
			return ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws at para offset.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="hvoPara">The hvo para.</param>
		/// <param name="ichBeginOffset">The ich begin offset.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int GetWsAtParaOffset(FdoCache cache, int hvoPara, int ichBeginOffset)
		{
			//Debug.Assert(cache.IsValidObject(hvoPara, StTxtPara.kClassId),
			//    String.Format("expected valid hvo({0}).", hvoPara));
			//if (hvoPara <= 0 && !cache.IsDummyObject(hvoPara))
			//    throw new ArgumentException(String.Format("expected valid hvo({0}).", hvoPara));
			ITsString tssPara = cache.GetTsStringProperty(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents);
			int ws = StringUtils.GetWsAtOffset(tssPara, ichBeginOffset);
			return ws;
		}

		/// <summary>
		/// Return the matching segments for the given twfics in order.
		/// We could enhance the performance of this routine, but we currently expect the user
		/// to be trying to identify segments for tons of twfics at one time (for LexExampleSentences).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="twfics"></param>
		static public List<int> TwficSegments(FdoCache cache, List<int> twfics)
		{
			/*
			 * //Enhancement 1: do an SQL query to find the real segment ides for the given (real) twfics.
			 * // return the segment ids, corresponding to given twfic(s). (null ids are given for those not found).
			 * // for each twfic with a null segment, load/parse its paragraph.
			 * select segment.Id, segment.BeginOffset, segment.EndOffset from CmBaseAnnotation_ twfic
					left outer join CmBaseAnnotation_ segment on segment.AnnotationType={}
					and twfic.BeginObject = segment.BeginObject
					and twfic.BeginOffset >= segment.BeginOffset
					and twfic.EndOffset <= segment.EndOffset
					where twfic.AnnotationType={} and twfic.Id in ()
			 */
			// Enhance: Preload/parse any out-of-date paragraphs for these twfics.
			// Enhance: Preload basic twfic info. (e.g. BeginObject)
			List<int> twficSegments = new List<int>(twfics.Count);
			bool fPreloaded = false;
			// first try to find existing real
			foreach (CmBaseAnnotation twfic in new FdoObjectSet<CmBaseAnnotation>(cache, twfics.ToArray(), !fPreloaded))
			{
				// get the segment for this twfic.
				TwficInfo twficInfo = new TwficInfo(cache, twfic.Hvo);
				twficSegments.Add(twficInfo.SegmentHvo);
			}
			return twficSegments;
		}

		/// <summary>
		/// Cache all the different types of freeform annotations for each segment in the given hvoPara.
		/// Stores using freeform annotations with SegmentFreeformAnnotationsFlid.
		/// Specifically, after calling this,
		/// - for each segment in hvoPara.Segments, property SegmentFreeformAnnotationsFlid(cache) is set
		///		to a list of all the indirect annotations which applyTo that segment and which are of
		///		type literal translation, free translation,or note
		/// - for each such indirect annotation, its type is cached, and also its Comment in all the
		///		specified writing systems.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoPara"></param>
		/// <param name="wsIds"></param>
		public static void LoadSegmentFreeformAnnotationData(FdoCache cache, int hvoPara, Set<int> wsIds)
		{
			int[] segIds = cache.GetVectorProperty(hvoPara, SegmentsFlid(cache), true);
			string whereStmt = String.Format("where cts.BeginObject = {0}", hvoPara);
			string orderByStmt = "order by cts.BeginOffset, cts.id, ca.AnnotationType, ca.id";
			LoadSegmentFreeformAnnotationData(cache, whereStmt, orderByStmt, segIds, wsIds.ToArray());
		}

		/// <summary>
		/// Cache all the different types of freeform annotations for each segment in the given segmentIds.
		/// Stores using freeform annotations with SegmentFreeformAnnotationsFlid
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="segmentIds"></param>
		/// <param name="wsIds"></param>
		public static void LoadSegmentFreeformAnnotationData(FdoCache cache, Set<int> segmentIds, Set<int> wsIds)
		{
			// If all texts are gone, we want to avoid an illegal query "where cts.id in ()".
			if (segmentIds.Count == 0)
				return;
			int[] segIds = segmentIds.ToArray();
			string whereStmt = String.Format("where cts.id in ({0})", CmObject.JoinIds(segIds, ","));
			string orderByStmt = "order by cts.BeginObject, cts.BeginOffset, cts.id, ca.AnnotationType, ca.id";
			LoadSegmentFreeformAnnotationData(cache, whereStmt, orderByStmt, segIds, wsIds.ToArray());
		}

		private static void LoadSegmentFreeformAnnotationData(FdoCache cache, string whereStatement, string orderByStatement, int[] segIds, int[] wsIds)
		{
			int ktagSegFF = SegmentFreeformAnnotationsFlid(cache);

			int textSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnTextSegment);
			int ltSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnLiteralTranslation);
			int ftSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			int noteSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnNote);
			// Retrieve CmAnnotations (which are really CmIndirectAnnotations, but there's no interesting
			// information in those tables) which
			// 1. ApplyTo the segment annotations above (cts), that is, the ones whose BeginObject is the StTxtPara
			// and whose type is Text Segment;
			// 2. Have one of the three types we're interested in.
			// The result columns are the ID of the text segment, the id of the ff annotation, the ID of the annotation type, and a txt, flid, ws, fmt set that
			// gives one of the alternatives of the Comment of the annotation.
			// We include cts.id in the order by so that, on the offchance that two segments begin at the same offset,
			// at least the annotations based on the same segment will be together.
			// We retrieve the contents of the annotations in a separate query as it is possible that the annotation
			// has been created but has no entry in CmAnnotation_Comment.
			string sQryFf = String.Format("select cts.id, ca.id, ca.AnnotationType from CmAnnotation ca"
				+ " join CmBaseAnnotation_ cts on cts.AnnotationType = " + textSegmentDefn
				+ " join CmIndirectAnnotation_AppliesTo caRef on caRef.Src = ca.id and caRef.Dst = cts.id "
				+ " and ca.AnnotationType in (" + ltSegmentDefn + "," + ftSegmentDefn + "," + noteSegmentDefn + ")"
				+ " {0} {1}", whereStatement, orderByStatement);
			IVwOleDbDa dba = cache.MainCacheAccessor as IVwOleDbDa;
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0); // First column contains IDs that are the base of properties (the segments).
			dcs.Push((int)DbColType.koctObjVec, 1, ktagSegFF, 0); // Second contains sequences of ff annotations of those objects.
			dcs.Push((int)DbColType.koctObj, 2, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType, 0); // Third contains type of ff annotation.

			dba.Load(sQryFf, dcs, 0, 0, null, false);

			LoadSegmentFreeformAnnotationComments(cache, whereStatement, orderByStatement, wsIds);
			RemoveDuplicateSegmentFreeformAnnotations(cache, segIds, wsIds);
		}

		private static void LoadSegmentFreeformAnnotationComments(FdoCache cache, string whereStatement, string orderByStatement, int[] wsIds)
		{
			int textSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnTextSegment);
			int ltSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnLiteralTranslation);
			int ftSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			int noteSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnNote);

			string sQryFf = String.Format("select ca.id,cc.Txt,34002,cc.ws,cc.Fmt from CmAnnotation ca"
				+ " join CmBaseAnnotation_ cts on cts.AnnotationType = " + textSegmentDefn
				+ " join CmIndirectAnnotation_AppliesTo caRef on caRef.Src = ca.id and caRef.Dst = cts.id "
				+ " join CmAnnotation_Comment cc on cc.obj = ca.id and cc.ws in (" + CmObject.JoinIds(wsIds, ",") + ")"
				+ " and ca.AnnotationType in (" + ltSegmentDefn + "," + ftSegmentDefn + "," + noteSegmentDefn + ") "
				+ " {0} {1}", whereStatement, orderByStatement);

			IVwOleDbDa dba = cache.MainCacheAccessor as IVwOleDbDa;
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0); // First contains base IDs (of ff annotations).
			dcs.Push((int)DbColType.koctMlaAlt, 1, 0, 0); // Next contains the text of an ML prop of object in column 1.
			dcs.Push((int)DbColType.koctFlid, 1, 0, 0); // Next contains the flid of that ML prop (constant at 34002).
			dcs.Push((int)DbColType.koctEnc, 1, 0, 0); // Next contains the ws of that ML prop.
			dcs.Push((int)DbColType.koctFmt, 1, 0, 0); // Next contains the fmt info for the ML prop.

			dba.Load(sQryFf, dcs, 0, 0, null, false);
		}

		/// <summary>
		/// If there are multiple analysis writing systems, a particular freeform annotation appears in the result
		/// set multiple times, and hence, appears multiple times in its segment's ktagSegFF list. Clean this up.
		/// At the same time, make sure we actually have an FF list for each segment (even if empty).
		/// Enhance JohnT: Eventually we may want to fiddle with the order and make sure certain ones always occur.
		/// Enhance the VC to handle an arbitrary list of FF annotations with multiple writing systems.		/// </summary>
		/// <param name="cache"></param>
		/// <param name="segIds"></param>
		/// <param name="wsIds"></param>
		private static void RemoveDuplicateSegmentFreeformAnnotations(FdoCache cache, int[] segIds, int[] wsIds)
		{
			int ktagSegFF = SegmentFreeformAnnotationsFlid(cache);
			ISilDataAccess sda = cache.MainCacheAccessor;
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			for (int iSeg = 0; iSeg < segIds.Length; ++iSeg)
			{
				int hvoSeg = segIds[iSeg];
				if (cache.MainCacheAccessor.get_IsPropInCache(hvoSeg, ktagSegFF,
					(int)CellarModuleDefns.kcptReferenceSequence, 0))
				{
					// We got some...eliminate duplicates, if there's a danger of that.
					if (wsIds.Length > 1)
					{
						int chvo = cache.MainCacheAccessor.get_VecSize(hvoSeg, ktagSegFF);
						int hvoPrev = 0; // never used as real ID
						int[] annIds = new int[chvo]; // maybe longer than needed.
						int ihvoOut = 0;
						for (int ihvo = 0; ihvo < chvo; ihvo++)
						{
							int hvoAnn = cache.MainCacheAccessor.get_VecItem(hvoSeg, ktagSegFF, ihvo);
							if (hvoAnn != hvoPrev)
							{
								annIds[ihvoOut] = hvoAnn;
								ihvoOut++;
								hvoPrev = hvoAnn;
							}
						}
						cda.CacheVecProp(hvoSeg, ktagSegFF, annIds, ihvoOut);
					}
				}
				else
				{
					// We didn't get any...note collection is empty.
					cda.CacheVecProp(hvoSeg, ktagSegFF, new int[0], 0);
				}
			}
		}

		/// <summary>
		/// Returns a List of CmBaseAnnotation ids denoting twfic and punctuation forms found in a Segment.
		/// </summary>
		/// <param name="hvoAnnotationSegment"></param>
		/// <returns></returns>
		public List<int> SegmentForms(int hvoAnnotationSegment)
		{
			return new List<int>(this.Cache.GetVectorProperty(hvoAnnotationSegment, CmBaseAnnotation.SegmentFormsFlid(Cache), true));
		}

		/// <summary>
		/// get the segment form at the given segment index and segment form index.
		/// </summary>
		/// <param name="iSeg">index of the segment</param>
		/// <param name="iSegForm">index of the xfic (word or punctuation)</param>
		/// <returns></returns>
		public int SegmentForm(int iSeg, int iSegForm)
		{
			// Enhance: should we care whether or not Segments will trigger loading/parsing the paragraph
			// if it's not already cached?
			List<int> segments = Segments;
			if (iSeg < 0 || iSeg > (segments.Count - 1))
				throw new ArgumentException(String.Format("segment index({}) out of bounds({})",
					iSeg, segments.Count - 1));
			List<int> segmentForms = SegmentForms(Segments[iSeg]);
			if (iSegForm < 0 || iSegForm > (segmentForms.Count - 1))
				throw new ArgumentException(String.Format("segment form index({}) out of bounds({})",
					iSegForm, segmentForms.Count - 1));
			return segmentForms[iSegForm];
		}


		/// <summary>
		/// Virtual property for paragraph segments (e.g. sentences).
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int SegmentsFlid(FdoCache cache)
		{
			int result = BaseVirtualHandler.GetInstalledHandlerTag(cache, "StTxtPara", "Segments");
			if (result != 0)
				return result;
			// This should be kept consistent with the definition in LanguageExplorer\Configuration\Words\AreaConfiguration.xml
			return BaseVirtualHandler.InstallVirtual(cache, "<virtual modelclass=\"StTxtPara\" virtualfield=\"Segments\">" +
						"<dynamicloaderinfo assemblyPath=\"ITextDll.dll\" class=\"SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler\"/>" +
						"</virtual>");
		}

		/// <summary>
		/// Virtual property for getting paragraph segments, without trying to parse/cache twfic/punctuation forms.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int SegmentsIgnoreTwficsFlid(FdoCache cache)
		{
			return BaseVirtualHandler.GetInstalledHandlerTag(cache, "StTxtPara", "SegmentsIgnoreTwfics");
		}

		/// <summary>
		/// This virtual property stores all the freeform annotations in a StTxtPara.Segment (free, literal, and note annotations).
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int SegmentFreeformAnnotationsFlid(FdoCache cache)
		{
			return BaseFDOPropertyVirtualHandler.GetInstalledHandlerTag(cache, "CmBaseAnnotation", "SegmentFreeformAnnotations");
		}

		/// <summary>
		/// This virtual property stores (for a CmBaseAnnotation which identifies a paragraph segment)
		/// the CmIndirctAnnotation which holds the free translation (in its comment). This is used for TE back translations,
		/// not in interlinear views, which can be configured to show a variety of freeform annotations.
		/// Call LoadSegmentFreeTranslations to load this property (and also to make
		/// sure that each segment actually has a (real) free translation annotation).
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int SegmentFreeTranslationFlid(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor, "CmBaseAnnotation", "FreeTranslation",
														   (int)CellarModuleDefns.kcptReferenceAtom).Tag;
		}

		/// <summary>
		/// Load the information we need to display a paragraph of free (back) translations: for each paragraph
		/// load its segments, for each segment load the free translation. We assume the segments
		/// property of each paragraph already exists, and want the BeginOffset, EndOffset, BeginObject, and Free Translation
		/// properties of each segment, and the Comment of the FT.
		/// Also ensures all the segments are real and have an FT annotation.
		/// </summary>
		/// <param name="paragraphs"></param>
		/// <param name="cache"></param>
		/// <param name="ws"></param>
		public static void LoadSegmentFreeTranslations(int[] paragraphs, FdoCache cache, int ws)
		{
			int kflidSegments = SegmentsFlid(cache);
			List<int> segments = new List<int>();
			foreach (int hvoPara in paragraphs)
			{
				int[] segs = cache.GetVectorProperty(hvoPara, kflidSegments, true);
				int iseg = 0;
				foreach (int hvoSeg in segs)
				{
					int hvoRealSeg = hvoSeg;
					if (cache.IsDummyObject(hvoSeg))
						hvoRealSeg = CmBaseAnnotation.ConvertBaseAnnotationToReal(cache, hvoSeg).Hvo;
					segments.Add(hvoRealSeg);
					segs[iseg++] = hvoRealSeg;
				}
				// Update the list.
				cache.VwCacheDaAccessor.CacheVecProp(hvoPara, kflidSegments, segs, segs.Length);
			}

			if (segments.Count == 0)
				return; // somehow we can have none, and then the query fails.

			int hvoFt = cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			string ids = JoinIds(segments.ToArray(), ",");
			int kflidFT = SegmentFreeTranslationFlid(cache);

			if (cache.DatabaseAccessor != null)
			{
				string sql =
					@"select seg.id, seg.BeginObject, seg.BeginOffset, seg.EndOffset, ft.id, ft.class$, ft.UpdStmp, ftc.Txt, ftc.Fmt from CmBaseAnnotation seg
					join CmIndirectAnnotation_AppliesTo ftseg on ftseg.Dst = seg.id
					join CmIndirectAnnotation_ ft on ft.id = ftseg.Src and ft.AnnotationType = " +
					hvoFt + @"
					left outer join CmAnnotation_Comment ftc on ftc.Obj = ft.id and ftc.ws = " + ws +
					@"
					where seg.id in (" + ids + ")";
				IDbColSpec dcs = DbColSpecClass.Create();
				dcs.Push((int) DbColType.koctBaseId, 0, 0, 0); // ID (of segment)
				dcs.Push((int) DbColType.koctObj, 1, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject, 0);
				dcs.Push((int) DbColType.koctInt, 1, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset, 0);
				dcs.Push((int) DbColType.koctInt, 1, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset, 0);
				dcs.Push((int) DbColType.koctObj, 1, kflidFT, 0); // Free translation indirect annotation
				dcs.Push((int) DbColType.koctInt, 5, (int) CmObjectFields.kflidCmObject_Class, 0); // class of FT annotation
				dcs.Push((int) DbColType.koctTimeStamp, 5, 0, 0); // timestamp of FT annotation
				dcs.Push((int)DbColType.koctMlsAlt, 5, (int)CmAnnotation.CmAnnotationTags.kflidComment, ws);
				dcs.Push((int)DbColType.koctFmt, 5, (int)CmAnnotation.CmAnnotationTags.kflidComment, ws);
				cache.LoadData(sql, dcs, 0);
			}

			// Make sure each segment has a free translation.
			foreach (int hvoSeg in segments)
			{
				int hvoFT = cache.GetObjProperty(hvoSeg, kflidFT);
				if (hvoFT == 0)
				{
					ICmIndirectAnnotation ann = CmIndirectAnnotation.CreateUnownedIndirectAnnotation(cache);
					ann.AppliesToRS.Append(hvoSeg);
					ann.AnnotationTypeRAHvo = hvoFt;
					cache.VwCacheDaAccessor.CacheObjProp(hvoSeg, kflidFT, ann.Hvo);
				}
			}
		}

		/// <summary>
		/// The default (guess) analysis for a twfic.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int TwficDefaultFlid(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"CmBaseAnnotation", "TwficDefault", (int)CellarModuleDefns.kcptReferenceAtom).Tag;
		}

		/// <summary>
		/// True, if the annotation is the first wordform in its segment.
		/// </summary>
		public static bool IsFirstWordformInSegment(FdoCache fdoCache, int hvoAnnotation)
		{
			bool fIsFirstTwfic = false;
			int hvoParaSeg = 0;
			int iParaSeg = -1;
			int iSegForm = StTxtPara.TwficSegmentLocation(fdoCache, hvoAnnotation, out hvoParaSeg, out iParaSeg, out fIsFirstTwfic);
			Debug.Assert(iSegForm != -1,
				String.Format("annotation {0} being searched for not found", hvoAnnotation));
			return fIsFirstTwfic;
		}

		/// <summary>
		/// Return segment information about the twfic annotation.
		/// Returns the index of the annotation in its SegmentForms.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnnotation"></param>
		/// <param name="hvoParaSeg"></param>
		/// <param name="iParaSeg"></param>
		/// <param name="fIsFirstTwfic">true if it's the first twfic in its segment.</param>
		/// <returns>The index of the annotation in its SegmentForms</returns>
		public static int TwficSegmentLocation(FdoCache cache, int hvoAnnotation, out int hvoParaSeg, out int iParaSeg, out bool fIsFirstTwfic)
		{
			Debug.Assert(hvoAnnotation != 0);
			hvoParaSeg = 0;
			iParaSeg = -1;
			fIsFirstTwfic = false;
			int ktagParaSegments = StTxtPara.SegmentsFlid(cache);
			int ktagSegmentForms = CmBaseAnnotation.SegmentFormsFlid(cache);
			ISilDataAccess sda = cache.MainCacheAccessor;
			int hvoPara = sda.get_ObjectProp(hvoAnnotation,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			// If the text is deleted while in interlinear view, the hvoPara value for the
			// annotation becomes zero because the paragraph no longer exists.  See LT-4620
			// for the crash that can occur if we try to use the hvoPara when it equals zero.
			if (hvoPara != 0)
			{
				int cseg = sda.get_VecSize(hvoPara, ktagParaSegments);
				for (iParaSeg = 0; iParaSeg < cseg; iParaSeg++)
				{
					hvoParaSeg = sda.get_VecItem(hvoPara, ktagParaSegments, iParaSeg);
					int hvoFirstTwfic = 0;
					int cann = sda.get_VecSize(hvoParaSeg, ktagSegmentForms);
					for (int iSegForm = 0; iSegForm < cann; iSegForm++)
					{
						int hvoSegForm = sda.get_VecItem(hvoParaSeg, ktagSegmentForms, iSegForm);
						if (hvoFirstTwfic == 0)
						{
							int hvoAnalysis = sda.get_ObjectProp(hvoSegForm,
									(int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
							if (hvoAnalysis != 0)
								hvoFirstTwfic = hvoSegForm;
						}
						if (hvoAnnotation == hvoSegForm)
						{
							if (hvoFirstTwfic == hvoAnnotation)
								fIsFirstTwfic = true;
							return iSegForm;
						}
					}
				}
				//Debug.Assert(false, "annotation being searched for not found");
			}
			hvoParaSeg = 0;
			iParaSeg = -1;
			return -1;
		}

		/// <summary>
		/// Take a twfic annotation and merge it with the following twfic annotation, creating a new
		/// annotation to replace those two twfics. Also deletes any unreferenced wordforms as a result of
		/// the merge.
		/// </summary>
		/// <param name="hvoStartingAnnotation">segmentForm twfic annotation from which to merge and replace
		/// with the next twfic annotation. </param>
		/// <returns>the resulting merged (real) annotation form.</returns>
		/// <param name="hvoParasInView">the paragraphs used in the current display</param>
		public ICmBaseAnnotation MergeAdjacentSegmentForms(int hvoStartingAnnotation, int[] hvoParasInView)
		{
			// 1) Merge the current annotation with next twfic.
			int hvoSeg;
			int iSegForm;
			ICmBaseAnnotation currentAnnotation;
			ICmBaseAnnotation nextAnnotation;
			int hvoOldWordform1;
			int hvoOldWordform2;
			GetAdjacentWficInfo(hvoStartingAnnotation, out hvoSeg, out iSegForm, out currentAnnotation, out nextAnnotation, out hvoOldWordform1, out hvoOldWordform2);

			// Create a new wordform for this annotation.
			ITsString tssOldPhrase = this.Contents.UnderlyingTsString.GetSubstring(
				currentAnnotation.BeginOffset, currentAnnotation.EndOffset);
			ITsString tssNewPhrase = this.Contents.UnderlyingTsString.GetSubstring(
				currentAnnotation.BeginOffset, nextAnnotation.EndOffset);
			int hvoDummyWff = WfiWordform.FindOrCreateWordform(Cache, tssNewPhrase, false);
			// Create a new CmBaseAnnotation with merged field information.
			// set the analysis level for the next annotation to be the new wordform.
			int hvoNewAnn = CmBaseAnnotation.CreateDummyAnnotation(Cache, currentAnnotation.BeginObjectRAHvo,
				currentAnnotation.AnnotationTypeRAHvo, currentAnnotation.BeginOffset, nextAnnotation.EndOffset, hvoDummyWff);
			WfiWordform dummyWff = WfiWordform.CreateFromDBObject(Cache, hvoDummyWff) as WfiWordform;
			// Add the occurrence before we convert to real, so that our prop changes will have enough
			// information for listeners to update their lists.
			dummyWff.TryAddOccurrence(hvoNewAnn);
			ICmBaseAnnotation newAnnotation = CmObject.ConvertDummyToReal(Cache, hvoNewAnn) as ICmBaseAnnotation;
			CacheReplaceOneUndoAction cacheReplaceSegmentsFormAction = new CacheReplaceOneUndoAction(Cache,
				hvoSeg, CmBaseAnnotation.SegmentFormsFlid(Cache), iSegForm, iSegForm, new int[] { newAnnotation.Hvo });
			cacheReplaceSegmentsFormAction.DoIt();
			if (Cache.ActionHandlerAccessor != null)
			{
				Cache.ActionHandlerAccessor.AddAction(cacheReplaceSegmentsFormAction);
			}

			// Remove the two annotations we just replaced.
			// Enhance: make CmBaseAnnotation.DeleteUnderlyingObject add to annotation orphanage.
			currentAnnotation.DeleteUnderlyingObject();	// side-effects will remove annotation from paragraph SegForms.
			nextAnnotation.DeleteUnderlyingObject();
			// See if we can remove any wordforms that are no longer refereneced by annotations/analyses.
			Set<int> delObjIds;
			TryDeleteWordforms(Cache, new int[] { hvoOldWordform1, hvoOldWordform2 }, hvoParasInView, out delObjIds);
			return newAnnotation;
		}

		/// <summary>
		/// given a wfic annotation, return information about the current wfic and the adjacent wfic
		/// </summary>
		/// <param name="hvoWfic"></param>
		/// <param name="nextWfic"></param>
		/// <param name="hvoOldWordform1">the wordform of the given wfic</param>
		/// <param name="hvoOldWordform2">the wordform of the adjacent wfic</param>
		public void GetAdjacentWficInfo(int hvoWfic, out ICmBaseAnnotation nextWfic, out int hvoOldWordform1, out int hvoOldWordform2)
		{
			int hvoSeg = 0;
			int iSegForm = 0;
			ICmBaseAnnotation currentWfic = null;
			GetAdjacentWficInfo(hvoWfic, out hvoSeg, out iSegForm, out currentWfic, out nextWfic, out hvoOldWordform1, out hvoOldWordform2);
		}

		private void GetAdjacentWficInfo(int hvoStartingAnnotation, out int hvoSeg, out int iSegForm, out ICmBaseAnnotation currentAnnotation, out ICmBaseAnnotation nextAnnotation, out int hvoOldWordform1, out int hvoOldWordform2)
		{
			TwficInfo twficInfo = new TwficInfo(Cache, hvoStartingAnnotation);
			hvoSeg = twficInfo.SegmentHvo;
			iSegForm = twficInfo.SegmentFormIndex;
			List<int> segForms = this.SegmentForms(hvoSeg);
			if (iSegForm < 0 || iSegForm + 1 >= segForms.Count)
				throw new ArgumentException(String.Format("Can't find adjacent annotation to merge with {0}.", hvoStartingAnnotation));
			int hvoNextAnnotation = segForms[iSegForm + 1];
			currentAnnotation = CmBaseAnnotation.CreateFromDBObject(Cache, hvoStartingAnnotation, false) as CmBaseAnnotation;
			nextAnnotation = CmBaseAnnotation.CreateFromDBObject(Cache, hvoNextAnnotation, false) as CmBaseAnnotation;
			int twficType = CmAnnotationDefn.Twfic(Cache).Hvo;
			if (currentAnnotation.AnnotationTypeRAHvo != twficType ||
				nextAnnotation.AnnotationTypeRAHvo != twficType)
			{
				throw new ArgumentException(String.Format("Can only support merging annotations of twfic type{0}.\nAnnotation({1}).Type({2}) Annotation({3}).Type({4})",
						twficType, currentAnnotation.Hvo, currentAnnotation.AnnotationTypeRAHvo, nextAnnotation.Hvo, nextAnnotation.AnnotationTypeRAHvo));
			}
			hvoOldWordform1 = WfiWordform.GetWfiWordformFromInstanceOf(Cache, currentAnnotation.Hvo);
			hvoOldWordform2 = WfiWordform.GetWfiWordformFromInstanceOf(Cache, nextAnnotation.Hvo);
		}

		/// <summary>
		/// Try deleting wordforms when none of the given paragraphs have annotations (including dummies)
		/// with instances of the wordforms.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wfIdsToTry">wordform ids to try to delete</param>
		/// <param name="hvoParasInView">the paragraphs that may have dummy annotations with instances of those wordforms</param>
		/// <param name="delObjIds">the wordforms that actually got deleted.</param>
		/// <returns>true if we deleted a wordform</returns>
		public static bool TryDeleteWordforms(FdoCache cache, int[] wfIdsToTry, int[] hvoParasInView, out Set<int> delObjIds)
		{
			int vtagSegments = StTxtPara.SegmentsFlid(cache);
			IVwVirtualHandler vh;
			cache.TryGetVirtualHandler(vtagSegments, out vh);
			FDOSequencePropertyVirtualHandler segmentsVh = vh as FDOSequencePropertyVirtualHandler;
			delObjIds = new Set<int>(wfIdsToTry.Length);
			foreach (int wfid in wfIdsToTry)
			{
				if (!cache.IsValidObject(wfid))
					continue;	// already been deleted, probably as a consequence of annotation.DeleteUnderlyingObject.
				ICmObject obj = CmObject.CreateFromDBObject(cache, wfid, false);
				if (obj.CanDelete)
				{
					// make a final check to see if any of the segment forms in the given paragraphs contain an
					// instance to this wordform. If not, it's safe to delete it.
					if (!WordformHasOccurrenceInParas(cache, wfid, hvoParasInView, segmentsVh))
						delObjIds.Add(wfid);
				}
			}
			foreach (WfiWordform wf in new FdoObjectSet<WfiWordform>(cache, delObjIds.ToArray(), false, typeof(WfiWordform)))
			{
				wf.DeleteUnderlyingObject();
			}
			return delObjIds.Count != 0;
		}

		private static bool WordformHasOccurrenceInParas(FdoCache cache, int wfid, int[] hvoParasInView, FDOSequencePropertyVirtualHandler segmentsVh)
		{
			foreach (StTxtPara para in new FdoObjectSet<StTxtPara>(cache, hvoParasInView, false, typeof(StTxtPara)))
			{
				// we don't want to reload/parse paragraphs that haven't been loaded in this context. We are just
				// trying to keep the cached data consistent in the view.
				if (!segmentsVh.IsPropInCache(cache.MainCacheAccessor, para.Hvo, 0))
					continue;
				foreach (int hvoSeg in para.Segments)
				{
					foreach (int hvoSegForm in para.SegmentForms(hvoSeg))
					{
						int hvoInstanceOf = cache.GetObjProperty(hvoSegForm, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
						if (wfid == hvoInstanceOf)
							return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Find a dummy paragraph segment annotation and replace it with a real annotation in the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOldAnnotation"></param>
		/// <param name="hvoNewAnnotation">if nonzero, this will be inserted in place of the old one.
		/// if zero, we'll simply delete the old one.</param>
		internal static void CacheReplaceTextSegmentAnnotation(FdoCache cache, int hvoOldAnnotation, int hvoNewAnnotation)
		{
			int kflidParaSegments = StTxtPara.SegmentsFlid(cache);
			int ktagSegmentForms = CmBaseAnnotation.SegmentFormsFlid(cache);
			ISilDataAccess sda = cache.MainCacheAccessor;
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			// find the paraSegment for the old annotation
			int hvoPara = sda.get_ObjectProp(hvoOldAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			List<int> paraSegments = new List<int>(cache.GetVectorProperty(hvoPara, kflidParaSegments, true));
			int iParaSeg = paraSegments.IndexOf(hvoOldAnnotation);
			Debug.Assert(iParaSeg != -1);
			if (iParaSeg >= 0)
			{
				int[] segmentForms = cache.GetVectorProperty(hvoOldAnnotation, ktagSegmentForms, true);
				if (hvoNewAnnotation != 0)
				{
					// first, transfer the old annotation's WordformSegments vector.
					cda.CacheVecProp(hvoNewAnnotation, ktagSegmentForms, segmentForms, segmentForms.Length);
				}
				int[] newItems = hvoNewAnnotation != 0 ? new int[] { hvoNewAnnotation } : new int[0];
				// now, replace the old paraSegment with the new.
				CacheReplaceOneUndoAction cacheReplaceParaSegmentAction = new CacheReplaceOneUndoAction(
					cache, hvoPara, kflidParaSegments, iParaSeg, iParaSeg + 1, newItems);
				cacheReplaceParaSegmentAction.DoIt();
				if (cache.ActionHandlerAccessor != null)
				{
					cache.ActionHandlerAccessor.AddAction(cacheReplaceParaSegmentAction);
				}
			}
		}

		/// <summary>
		/// Remove a Twfic from its paragraph segments.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOldTwficAnnotation"></param>
		internal static void RemoveTwficAnnotation(FdoCache cache, int hvoOldTwficAnnotation)
		{
			StTxtPara.CacheReplaceTWFICAnnotation(cache, hvoOldTwficAnnotation, 0);
		}

		/// <summary>
		/// Find a dummy segment wordform annotation, and replace with a real annotation it in the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOldAnnotation"></param>
		/// <param name="hvoNewAnnotation"></param>
		/// <returns>if nonzero, this will be inserted in place of the old one.
		/// if zero, we'll simply delete the old one.</returns>
		internal static void CacheReplaceTWFICAnnotation(FdoCache cache, int hvoOldAnnotation, int hvoNewAnnotation)
		{
			int ktagSegmentForms = CmBaseAnnotation.SegmentFormsFlid(cache);
			int kflidOccurrences = WfiWordform.OccurrencesFlid(cache);
			ISilDataAccess sda = cache.MainCacheAccessor;
			int hvoParaSeg = 0;
			int iParaSeg = -1;
			bool fIsFirstTwfic = false;
			int iSegForm = StTxtPara.TwficSegmentLocation(cache, hvoOldAnnotation, out hvoParaSeg, out iParaSeg, out fIsFirstTwfic);
			IVwCacheDa cda = cache.VwCacheDaAccessor;
			int[] newItems = hvoNewAnnotation != 0 ? new int[] { hvoNewAnnotation } : new int[0];
			if (hvoParaSeg != 0 && iSegForm >= 0)
			{
				CacheReplaceOneUndoAction cacheReplaceSegmentFormAction = new CacheReplaceOneUndoAction(cache, hvoParaSeg,
					ktagSegmentForms, iSegForm, iSegForm + 1, newItems);
				cacheReplaceSegmentFormAction.DoIt();
				if (cache.ActionHandlerAccessor != null)
				{
					cache.ActionHandlerAccessor.AddAction(cacheReplaceSegmentFormAction);
				}
			}
			int hvoOldWordform = 0;
			if (WfiWordform.TryGetWfiWordformFromInstanceOf(cache, hvoOldAnnotation, out hvoOldWordform))
			{
				// replace the dummy annotation on its wordform, if we haven't already.
				List<int> dummyAnnotations = new List<int>(cache.GetVectorProperty(hvoOldWordform, kflidOccurrences, true));
				int iAnn = dummyAnnotations.IndexOf(hvoOldAnnotation);
				if (iAnn >= 0)
				{
					CacheReplaceOneUndoAction cacheReplaceOccurrenceAction = new CacheReplaceOneUndoAction(cache, hvoOldWordform,
						kflidOccurrences, iAnn, iAnn + 1, newItems);
					cacheReplaceOccurrenceAction.DoIt();
					if (cache.ActionHandlerAccessor != null)
					{
						cache.ActionHandlerAccessor.AddAction(cacheReplaceOccurrenceAction);
					}
				}
			}
			// try converting the owning segment to real, if we're converting the twfic to real.
			if (hvoOldAnnotation != 0 && hvoNewAnnotation != 0 &&
				cache.IsDummyObject(hvoOldAnnotation) && !cache.IsDummyObject(hvoNewAnnotation) &&
				cache.IsDummyObject(hvoParaSeg))
			{
				hvoParaSeg = CmBaseAnnotation.ConvertBaseAnnotationToReal(cache, hvoParaSeg).Hvo;
			}
		}

		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies all fields in the current StTxtPara to the given destination paragraph.
		/// Note: Caller should normally also call CreateOwnedObjects() on the destination
		/// paragraph so that ORCs will be properly updated.
		/// </summary>
		/// <param name="destPara">the destination paragraph.</param>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(StTxtPara destPara)
		{
			// Wish we could call some form of m_cache.CopyObject to copy all the fields
			// automatically, but currently CopyObject is insufficient because it does not copy
			// fields to an existing object.

			// prevent side effects from ChangeWatchers, and don't notify the view at this time
			using (new IgnorePropChanged(m_cache, PropChangedHandling.SuppressAll))
			{
				// Copy all the fields of the StTxtPara ...one by one
				destPara.StyleRules = StyleRules;
				destPara.Contents.UnderlyingTsString = Contents.UnderlyingTsString;
				foreach (int hvoTrans in TranslationsOC.HvoArray)
					m_cache.CopyObject(hvoTrans, destPara.Hvo, (int)StTxtPara.StTxtParaTags.kflidTranslations);
				// attempt to copy all remaining fields, though we don't currently use nor test these
				destPara.Label.UnderlyingTsString = Label.UnderlyingTsString;
				m_cache.CopyOwningSequence(AnalyzedTextObjectsOS, destPara.Hvo);
				TextObjectsRS.HvoArray.CopyTo(destPara.TextObjectsRS.HvoArray, 0);
				ObjRefsRC.HvoArray.CopyTo(destPara.ObjRefsRC.HvoArray, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete any objects whose guids are owned by the given portion of the paragraph
		/// Contents. Use this when part of a string is about to be deleted (except when being
		/// done thru a VwSelection).
		/// </summary>
		/// <param name="ichMin">The 0-based index of the first character to be deleted</param>
		/// <param name="ichLim">The 0-based index of the character following the last character
		/// to be deleted</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveOwnedObjectsForString(int ichMin, int ichLim)
		{
			if (ichLim <= ichMin)
				return; // nothing to do

			ITsString contents = Contents.UnderlyingTsString;
			int firstRun = contents.get_RunAt(ichMin);
			int lastRun = contents.get_RunAt(ichLim - 1);

			// Check each run, and delete owned objects
			for (int iRun = firstRun; iRun <= lastRun; iRun++)
			{
				FwObjDataTypes odt;
				Guid guid = StringUtils.GetOwnedGuidFromRun(contents, iRun, out odt);
				if (guid != Guid.Empty)
				{
					int hvo = m_cache.GetIdFromGuid(guid);
					if (hvo != 0)
					{
						// remove the owned object from the db, if hvo located
						m_cache.DeleteObject(hvo);
					}
					DeleteAnyBtMarkersForFootnote(guid);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all footnote reference ORCs that reference the specified footnote in all
		/// writing systems for the back translation of the given (vernacular) paragraph.
		/// </summary>
		/// <param name="footnoteGuid">guid for the specified footnote</param>
		/// ------------------------------------------------------------------------------------
		public void DeleteAnyBtMarkersForFootnote(Guid footnoteGuid)
		{
			// ENHANCE: Someday may need to do this for other translations, not just BT (will
			// need to rename this method).
			ICmTranslation trans = GetBT();

			if (trans != null)
			{
				// Delete the reference ORCs for this footnote in the back translation
				//TODO: TE-5047 update this loop control to iterate the alt strings that actually exist in this CmTranslation
				//eg  for (int i = 0; i < trans.StringCount; i++) etc
				foreach (ILgWritingSystem ws in m_cache.LangProject.AnalysisWssRC)
				{
					ITsString btTss = trans.Translation.GetAlternative(ws.Hvo).UnderlyingTsString;
					if (btTss.RunCount > 0 && btTss.Text != null)
					{
						ITsStrBldr btTssBldr = btTss.GetBldr();
						DeleteBtFootnoteMarker(btTssBldr, footnoteGuid);
						trans.Translation.SetAlternative(btTssBldr.GetString(), ws.Hvo);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove all reference ORCs for the specified footnote in the given back translation
		/// ITsStrBldr.
		/// </summary>
		/// <param name="btTssBldr">string builder for the back translation of the paragraph
		/// </param>
		/// <param name="footnoteGuid">guid for the specified footnote</param>
		/// <returns>the ich location of the marker that was deleted, or -1 if no marker was
		/// deleted</returns>
		/// ------------------------------------------------------------------------------------
		public static int DeleteBtFootnoteMarker(ITsStrBldr btTssBldr, Guid footnoteGuid)
		{
			Debug.Assert(footnoteGuid != Guid.Empty);
			if (footnoteGuid == Guid.Empty)
				return -1;

			int iRun = 0;
			Guid guid;
			while (iRun < btTssBldr.RunCount)
			{
				guid = StringUtils.GetGuidFromRun(btTssBldr.GetString(), iRun);

				if (guid == footnoteGuid)
				{
					// Footnote ORC with same Guid found. Remove it.
					int ichMin, ichLim;
					TsRunInfo info;
					btTssBldr.FetchRunInfo(iRun, out info);
					btTssBldr.GetBoundsOfRun(iRun, out ichMin, out ichLim);
					ITsPropsBldr propsBldr = btTssBldr.get_Properties(iRun).GetBldr();
					propsBldr.SetStrPropValue((int)FwTextPropType.ktptObjData, null);
					btTssBldr.Replace(ichMin, ichLim, null, propsBldr.GetTextProps());
					return info.ichMin;
				}
				iRun++;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create copies of any objects whose guids are owned by the given portion of the
		/// paragraph Contents. Use this when part of a string has just been replaced with a
		/// previous revision that may have object references.
		/// </summary>
		/// <param name="ichMin">The 0-based index of the first character to search for ORCs
		/// </param>
		/// <param name="ichLim">The 0-based index of the character following the last character
		/// to be searched</param>
		/// <param name="objInfoProvider">Object that can provide the correct footnote index or
		/// picture folder to use for copied objects
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void CreateOwnedObjects(int ichMin, int ichLim,
			IObjectMetaInfoProvider objInfoProvider)
		{
			if (ichLim <= ichMin)
				return; // nothing to do

			ITsString paraContents = Contents.UnderlyingTsString;
			ITsTextProps ttp;
			TsRunInfo tri;
			int firstRun = paraContents.get_RunAt(ichMin);
			int lastRun = paraContents.get_RunAt(ichLim - 1);

			// these variables are initialized if needed as we process the runs
			ITsStrBldr paraBldr = null;
			int hvoFollowingFootnote = 0;
			int iFirstFootnote = -1;
			int footnoteCount = 0; // number of footnotes we have encountered in the given range
			ICmObject footnoteOwner = null;
			int footnoteFlid = 0;

			// Check each run, and create copies of the owned objects
			for (int iRun = firstRun; iRun <= lastRun; iRun++)
			{
				FwObjDataTypes odt;
				Guid guidOfObjToCopy = StringUtils.GetOwnedGuidFromRun(paraContents, iRun, out odt,
					out tri, out ttp);

				// if this run is an owning ORC...
				if (guidOfObjToCopy != Guid.Empty)
				{
					Guid guidOfNewObj = Guid.Empty;

					if (odt == FwObjDataTypes.kodtOwnNameGuidHot)
					{
						// If this is the first footnote created, get the correct starting index to use
						// TODO (TimS): if there are two footnotes together, we noticed some problems with getting the
						// correct footnote index
						if (iFirstFootnote == -1)
						{
							iFirstFootnote = objInfoProvider.NextFootnoteIndex(this, ichMin);
							GetFootnoteOwnerAndFlid(out footnoteOwner, out footnoteFlid);
							if (Cache.GetVectorSize(footnoteOwner.Hvo, footnoteFlid) > iFirstFootnote)
								hvoFollowingFootnote = Cache.GetVectorItem(footnoteOwner.Hvo, footnoteFlid, iFirstFootnote);
							else
								hvoFollowingFootnote = -1;
						}
						Debug.Assert(hvoFollowingFootnote != 0);
						Debug.Assert(iFirstFootnote > -1);
						// Create the new copy of the footnote.
						int hvoNewFootnote;
						int hvoFootnote = m_cache.GetIdFromGuid(guidOfObjToCopy);
						if (m_cache.IsValidObject(hvoFootnote))
						{
							hvoNewFootnote = m_cache.CopyObject(hvoFootnote, footnoteOwner.Hvo,
								footnoteFlid, hvoFollowingFootnote);
						}
						else
						{
							int iFootnote = iFirstFootnote + footnoteCount;
							// Unable to find footnote with this guid, so create a blank footnote.
							hvoNewFootnote = CreateBlankDummyFootnote(footnoteOwner, iFootnote,
								paraContents, iRun).Hvo;
						}
						guidOfNewObj = m_cache.GetGuidFromId(hvoNewFootnote);
						Debug.Assert(guidOfNewObj != guidOfObjToCopy);
						footnoteCount++;
					}
					else if (odt == FwObjDataTypes.kodtGuidMoveableObjDisp)
					{
						// Create the new copy of the picture.
						string textRep = m_cache.TextRepOfObj(guidOfObjToCopy);
						//REVIEW: when we support BT of a caption, be sure we copy it!
						CmPicture pict = new CmPicture(m_cache, textRep,
							objInfoProvider.PictureFolder);
						guidOfNewObj = m_cache.GetGuidFromId(pict.Hvo);
					}

					// If a new object was created, update all the ORCs for it
					if (guidOfNewObj != Guid.Empty)
					{
						// We re-use the same string builder for the paragraph contents.
						//  Just get it if this is the first time thru.
						if (paraBldr == null)
							paraBldr = paraContents.GetBldr();

						UpdateORCforNewObjData(paraBldr, ttp, tri, odt, guidOfNewObj);

						// In each translation, update any ORC from the old object, to the new
						UpdateOrcsInTranslations(guidOfObjToCopy, guidOfNewObj);
					}
				}
			}
			// save the updated paragraph string
			if (paraBldr != null)
			{
				// Finally, set the paragraph Contents to the new value
				// (but prevent side effects from ChangeWatchers)
				using (new IgnorePropChanged(m_cache, PropChangedHandling.SuppressChangeWatcher))
				{
					Contents.UnderlyingTsString = paraBldr.GetString();
				}
			}

			// if we inserted footnotes, do a PropChanged now that the footnotes and ORCs are in the db
			if (iFirstFootnote > -1)
			{
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, footnoteOwner.Hvo,
					footnoteFlid, iFirstFootnote, footnoteCount, 0);
			}
		}

		/// <summary>
		/// Answer true if the paragraph has no segment-level back translation.
		/// Assumes that something (e.g., LoadSegmentFreeTranslations) has loaded the Segments property
		/// and the segments free translation property.
		/// </summary>
		/// <returns></returns>
		public bool HasNoSegmentBt(int btWs)
		{
			int kflidSegments = StTxtPara.SegmentsFlid(Cache);
			int kflidFt = StTxtPara.SegmentFreeTranslationFlid(Cache);
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int cseg = sda.get_VecSize(Hvo, kflidSegments);
			for (int iseg = 0; iseg < cseg; iseg++)
			{
				int hvoSeg = sda.get_VecItem(Hvo, kflidSegments, iseg);
				int hvoFt = sda.get_ObjectProp(hvoSeg, kflidFt);
				if (hvoFt != 0 && sda.get_MultiStringAlt(hvoFt, (int)CmAnnotation.CmAnnotationTags.kflidComment, btWs).Length != 0)
					return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="owner">The owner to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual StFootnote CreateBlankDummyFootnote(ICmObject owner, int iFootnote,
			ITsString paraContents, int iRun)
		{
			// Make a dummy blank footnote is handled in ScrTextPara subclass. Since this isn't
			// a regular subclass but hopefully has been mapped, we use a hack: If StTxtPara is
			// mapped, CreateFromDBObject will return a ScrTextPara on which we can call
			// CreateBlankDummyFootnoteNoRecursion. Otherwise we call our implementation
			// (which currently just throws an exception).
			// If "this" is already a ScrTxtPara, we override CreateBlankDummyFootnote and
			// don't come here.
			StTxtPara newPara = (StTxtPara)CmObject.CreateFromDBObject(m_cache, Hvo);
			// ok, it's mapped, so use the implementation provided there instead.
			return newPara.CreateBlankDummyFootnoteNoRecursion(owner, iFootnote,
				paraContents, iRun);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a blank dummy footnote. Used when a footnote object is missing.
		/// Note: This does not insert an ORC into the paragraph. The caller is fixing the ORC
		/// with a missing object.
		/// </summary>
		/// <param name="owner">The owner to which we will add a footnote.</param>
		/// <param name="iFootnote">The 0-based index where the footnote will be inserted in the
		/// owner.</param>
		/// <param name="paraContents">The paragraph string where the ORC is being fixed.</param>
		/// <param name="iRun">The 0-based index of the run from which we will get the writing
		/// system used in the footnote.</param>
		/// <returns>a blank general footnote</returns>
		/// <remarks>NOTE: Don't call this version directly - always call
		/// CreateBlankDummyFootnote!</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual StFootnote CreateBlankDummyFootnoteNoRecursion(ICmObject owner,
			int iFootnote, ITsString paraContents, int iRun)
		{
			//  Make a dummy blank footnote
			if (owner is IScrBook)
			{
				throw new InvalidOperationException("To make CreateBlankDummyFootnote work with " +
					"Scripture it needs to have StTxtPara mapped to ScrTxtPara");
			}
			else
				throw new NotImplementedException("Not yet implemented for non-ScrBook owners");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Search the ownership hierarchy for this paragraph to find the object that owns a
		/// collection/sequence of footnotes. Return that object and the flid of the field in
		/// which the footnotes are owned.
		/// </summary>
		/// <param name="owner">Object that owns the footnotes for this paragraph</param>
		/// <param name="flid">Field in which the owner owns the footnotes</param>
		/// <returns>true if found; false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool GetFootnoteOwnerAndFlid(out ICmObject owner, out int flid)
		{
			owner = this;
			flid = 0;
			while (owner != null)
			{
				foreach (ClassAndPropInfo cpi in m_cache.GetFieldsOfClass((uint)owner.ClassID))
				{
					if (cpi.fieldType == (int)FieldType.kcptOwningCollection ||
						cpi.fieldType == (int)FieldType.kcptOwningSequence)
					{
						uint clsidFldDst = m_cache.MetaDataCacheAccessor.GetDstClsId(cpi.flid);
						if (clsidFldDst == (int)StFootnote.kclsidStFootnote)
						{
							flid = (int)cpi.flid;
							return true;
						}
					}
				}
				if (owner.OwnerHVO == 0)
					return false;
				owner = CmObject.CreateFromDBObject(m_cache, owner.OwnerHVO);
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the ORCs (Object Replacement Characters) in translations of this paragraph.
		/// </summary>
		/// <param name="guidOfOldObj">The GUID of the old object</param>
		/// <param name="guidOfNewObj">The GUID of the new object</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateOrcsInTranslations(Guid guidOfOldObj, Guid guidOfNewObj)
		{
			List<int> transWs = m_cache.GetUsedScriptureTransWsForPara(Hvo);

			// Look in each writing system of every translation for ORCs that need to be updated
			// from the GUID of the original object to the GUID of the new copy.
			FwObjDataTypes odt;
			ITsTextProps ttp;
			TsRunInfo tri;
			foreach (CmTranslation trans in TranslationsOC)
			{
				//TODO: TE-5047 update this loop control to iterate the alt strings that actually exist in this CmTranslation
				//eg  for (int i = 0; i < trans.StringCount; i++) etc
				foreach (int ws in transWs)
				{
					ITsString tss = trans.Translation.GetAlternativeTss(ws);
					if (tss != null)
					{
						// Scan through ITsString for reference ORC to the specified old object.
						// Check each run
						for (int iRun = 0; iRun < tss.RunCount; iRun++)
						{
							// Attempt to find the old GUID in the current run.
							Guid guidInTrans = StringUtils.GetGuidFromRun(tss, iRun, out odt,
								out tri, out ttp, null);

							// If we found the GUID of the old object, we need to update this
							//  ORC to the new GUID.
							if (guidInTrans == guidOfOldObj)
							{
								ITsStrBldr strBldr = tss.GetBldr();
								UpdateORCforNewObjData(strBldr, ttp, tri, odt, guidOfNewObj);
								// Set the translation alt string to the new value
								// (but prevent side effects from ChangeWatchers)
								using (new IgnorePropChanged(m_cache, PropChangedHandling.SuppressChangeWatcher))
								{
									trans.Translation.SetAlternative(strBldr.GetString(), ws);
								}
								break; // updated matching ORC, check next writing system
							}
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// In a string builder, updates the given ORC (Object Replacement Character) run
		/// with new object data.
		/// </summary>
		/// <param name="bldr">tss builder holding the ORC run</param>
		/// <param name="ttp">text properties of the ORC run</param>
		/// <param name="tri">The info for the ORC run, including min/lim character indices.</param>
		/// <param name="odt">object data type indicating to which type of object ORC points</param>
		/// <param name="guidOfNewObj">The GUID of new object, to update the ORC</param>
		/// ------------------------------------------------------------------------------------
		private static void UpdateORCforNewObjData(ITsStrBldr bldr, ITsTextProps ttp,
			TsRunInfo tri, FwObjDataTypes odt, Guid guidOfNewObj)
		{
			// build new ObjData properties of the ORC for the new object
			byte[] objData = MiscUtils.GetObjData(guidOfNewObj, (byte)odt);
			ITsPropsBldr propsBldr = ttp.GetBldr();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);

			// update the run props in the string builder
			bldr.SetProperties(tri.ichMin, tri.ichLim, propsBldr.GetTextProps());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mark all of the back translations for this paragraph as unfinished.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MarkBackTranslationsAsUnfinished()
		{
			// Mark all of the back translations
			// ENHANCE: Someday may need to do this for other translations, not just BT (will
			// need to rename this method).
			ICmTranslation translation = this.GetOrCreateBT();

			//TODO: TE-5047 update this loop control to iterate all alt strings that actually exist in this CmTranslation
			//eg  for (int i = 0; i < trans.StringCount; i++) etc
			foreach (LgWritingSystem ws in m_cache.LanguageEncodings)
			{
				// set all of the alternates to unfinished
				string state = translation.Status.GetAlternative(ws.Hvo);
				if (state != null)
					translation.Status.SetAlternative(BackTranslationStatus.Unfinished.ToString(), ws.Hvo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks to see if we currently have a back translation, returning it if we do.  If no
		/// BT exists, a new one is created and returned.
		/// </summary>
		/// <returns>CmTranslation for the BT</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation GetOrCreateBT()
		{
			return GetOrCreateTrans(Cache.LangProject.TranslationTagsOA.LookupPossibilityByGuid(
				LangProject.kguidTranBackTranslation));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks to see if we currently have a translation of the given type, returning it if
		/// we do.  If no such CmTranslation exists, a new one is created and returned.
		/// </summary>
		/// <param name="transType">The CmPossibility representing the type of translation to
		/// get or create</param>
		/// <returns>CmTranslation of the given type</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation GetOrCreateTrans(ICmPossibility transType)
		{
			Debug.Assert(transType != null);
			ICmTranslation trans = GetTrans(transType.Guid);
			if (trans == null)
			{
				// we need to create an empty translation if one does not exist.
				IActionHandler acth = m_cache.ActionHandlerAccessor;
				SuppressSubTasks suppressSubTasks = null;

				// We don't want to create an undo task if there isn't one already open, so we
				// set the ActionHandler to null while we create the back translation.
				bool fNewTransaction = false;
				string sSavePointName = string.Empty;
				IOleDbEncap dbAccess = m_cache.DatabaseAccessor;
				if (acth != null && acth.CurrentDepth == 0)
				{
					suppressSubTasks = new SuppressSubTasks(m_cache);
				}
				else if (dbAccess != null && !dbAccess.IsTransactionOpen())
				{
					// Set a save point in case there is a problem with the database server.
					// If there is a problem adding the translation or setting the type, this
					// transaction will be rolled back rather than not completely initializing
					// the translation.
					dbAccess.SetSavePointOrBeginTrans(out sSavePointName);
					fNewTransaction = true;
				}

				try
				{
					trans = new CmTranslation();
					TranslationsOC.Add(trans);
					trans.TypeRA = transType;
				}
				finally
				{
					if (suppressSubTasks != null)
						suppressSubTasks.Dispose();
				}
				if (fNewTransaction)
					dbAccess.CommitTrans();
			}
			return trans;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks to see if we currently have a back translation, returning it if we do.
		/// </summary>
		/// <param name="transType">The type of translation to get</param>
		/// <returns>CmTranslation for the BT or null if none exists</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation GetTrans(Guid transType)
		{
			CmTranslation validTrans = null;
			List<CmTranslation> transList = new List<CmTranslation>();
			foreach (ICmTranslation trans in TranslationsOC)
			{
				Debug.Assert(trans.TypeRA != null || Cache.m_fTestMode,
					"Found translation with unspecified type.");
				if (trans.TypeRA == null)
					transList.Add((CmTranslation)trans); // found translation w/o type specified
				else if (trans.TypeRA.Guid == transType)
					validTrans = (CmTranslation)trans;
			}

			if (transList.Count > 0 && validTrans == null)
			{
				// We found translation(s) without a specified type, but no valid translations.
				// Set first translation to back translation type and remove from list.
				transList[0].TypeRA =
						Cache.LangProject.TranslationTagsOA.LookupPossibilityByGuid(
							LangProject.kguidTranBackTranslation);
				transList.Remove(transList[0]);

				// Delete the rest
				foreach (CmTranslation trans in transList)
					Cache.DeleteObject(trans.Hvo);
			}
			else if (transList.Count > 0)
			{
				// We found a valid translation but also one or more bogus translations.
				// Delete bogus translation(s)
				foreach (CmTranslation trans in transList)
					Cache.DeleteObject(trans.Hvo);
			}

			return validTrans;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks to see if we currently have a back translation, returning it if we do.
		/// </summary>
		/// <returns>CmTranslation for the BT or null if none exists</returns>
		/// ------------------------------------------------------------------------------------
		public ICmTranslation GetBT()
		{
			return GetTrans(LangProject.kguidTranBackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all footnotes "owned" by this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<FootnoteInfo> GetFootnotes()
		{
			ITsString contents = Contents.UnderlyingTsString;
			List<FootnoteInfo> footnotes = new List<FootnoteInfo>();

			for (int iRun = 0; iRun < contents.RunCount; iRun++)
			{
				Guid guidOfObj = StringUtils.GetGuidFromRun(contents, iRun,
					FwObjDataTypes.kodtOwnNameGuidHot);
				if (guidOfObj != Guid.Empty)
				{
					try
					{
						int hvo = m_cache.GetIdFromGuid(guidOfObj);
						if (hvo > 0)
						{
							StFootnote footnote = new StFootnote(m_cache, hvo);
							footnotes.Add(new FootnoteInfo(footnote));
						}
					}
					catch
					{
					}
				}
			}

			return footnotes;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of all pictures "owned" by this paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ICmPicture> GetPictures()
		{
			ITsString contents = Contents.UnderlyingTsString;
			List<ICmPicture> pictures = new List<ICmPicture>();

			for (int iRun = 0; iRun < contents.RunCount; iRun++)
			{
				Guid guidOfObj = StringUtils.GetGuidFromRun(contents, iRun,
					FwObjDataTypes.kodtGuidMoveableObjDisp);
				if (guidOfObj != Guid.Empty)
				{
					try
					{
						int hvo = m_cache.GetIdFromGuid(guidOfObj);
						if (hvo > 0)
							pictures.Add(new CmPicture(m_cache, hvo));
					}
					catch { }
				}
			}

			return pictures;
		}
		#endregion
		/// <summary>
		/// Get set of unique wordforms in this paragraph
		/// </summary>
		/// <returns>A List that contains zero, or more, integers (hvos) for the unique wordforms occurring in this paragraph.</returns>
		public void CollectUniqueWordforms(Set<int> wordforms)
		{
			foreach (int hvoSegment in Segments)
			{
				GetWordformsFromSegment(hvoSegment, wordforms);
			}
			return;
		}

		private void GetWordformsFromSegment(int hvoSegment, Set<int> wordforms)
		{
			List<int> xfics = SegmentForms(hvoSegment);
			int hvoWficDefn = CmAnnotationDefn.Twfic(Cache).Hvo;
			foreach (int hvoXfic in xfics)
			{
				if (Cache.GetObjProperty(hvoXfic, (int) CmAnnotation.CmAnnotationTags.kflidAnnotationType) == hvoWficDefn)
				{
					int hvoInstanceOf =
						Cache.GetObjProperty(hvoXfic, (int) CmAnnotation.CmAnnotationTags.kflidInstanceOf);
					int hvoWordform = GetHvoWordformFromWfic(hvoInstanceOf);
					hvoWordform = GetRealHvoWordform(hvoWordform);
					if (hvoWordform != 0)
						wordforms.Add(hvoWordform);
				}
			}
		}

		private int GetRealHvoWordform(int hvoWordform)
		{
			if (Cache.IsDummyObject(hvoWordform))
			{
				IWfiWordform wf = CmObject.ConvertDummyToReal(Cache, hvoWordform) as IWfiWordform;
				if (wf != null)
					hvoWordform = wf.Hvo;
				else
					hvoWordform = 0;
			}
			return hvoWordform;
		}

		private int GetHvoWordformFromWfic(int hvoInstanceOf)
		{
			int hvoWordform = 0;
			switch (Cache.GetClassOfObject(hvoInstanceOf))
			{
				case WfiGloss.kclsidWfiGloss:
					int hvoOwner = Cache.GetOwnerOfObject(hvoInstanceOf);
					hvoWordform = Cache.GetOwnerOfObject(hvoOwner);
					break;
				case WfiAnalysis.kclsidWfiAnalysis:
					hvoWordform = Cache.GetOwnerOfObject(hvoInstanceOf);
					break;
				case WfiWordform.kclsidWfiWordform:
					hvoWordform = hvoInstanceOf;
					break;
			}
			return hvoWordform;
		}
		/// <summary>
		/// Get set of unique wordforms in this paragraph
		/// </summary>
		/// <returns>A List that contains zero, or more, integers (hvos) for the unique wordforms occurring in this paragraph.</returns>
		public void CreateTextObjects()
		{
			while (TextObjectsRS.Count > 0)
				TextObjectsRS.RemoveAt(0);

			foreach (int hvoSegment in Segments)
			{
				List<int> xfics = SegmentForms(hvoSegment);
				int hvoWficDefn = CmAnnotationDefn.Twfic(Cache).Hvo;
				int hvoPficDefn = CmAnnotationDefn.Punctuation(Cache).Hvo;
				foreach (int hvoXfic in xfics)
				{
					int hvo = 0;
					int hvoType = Cache.GetObjProperty(hvoXfic, (int) CmAnnotation.CmAnnotationTags.kflidAnnotationType);
					if (hvoType == hvoWficDefn)
					{
						int hvoInstanceOf =
							Cache.GetObjProperty(hvoXfic, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
						hvo = GetHvoWordformFromWfic(hvoInstanceOf);
						hvo = GetRealHvoWordform(hvo);
					}
					else if (hvoType == hvoPficDefn)
					{
						hvo = GetRealHvoPunctuation(hvoXfic);
					}
					else
						hvo = hvoXfic;
					if (hvo > 0)
						TextObjectsRS.Append(hvo);
				}
			}
			return;
		}
		private int GetRealHvoPunctuation(int hvoPunctuation)
		{
			if (Cache.IsDummyObject(hvoPunctuation))
			{
				ICmBaseAnnotation punct = CmBaseAnnotation.ConvertBaseAnnotationToReal(Cache, hvoPunctuation);
				if (punct != null)
					hvoPunctuation = punct.Hvo;
				else
					hvoPunctuation = 0;
			}
			return hvoPunctuation;
		}

	}
}
