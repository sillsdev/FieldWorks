using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.IText
{

	/// <summary>
	/// Property for Segments in an StTxtPara.
	/// </summary>
	public class ParagraphSegmentsVirtualHandler : FDOSequencePropertyVirtualHandler
	{
		public ParagraphSegmentsVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
		}

		/// <summary>
		/// Loads segments for a paragraph (and dependent dummy annotations such as )
		/// </summary>
		/// <param name="hvoWfi"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvoPara, int ktagParaSegments, int ws, IVwCacheDa cda)
		{
			// The parser will create and cache our segments (and dependent dummy annotations).
			LoadParagraphInfoToCache(hvoPara);
			if (m_cache.VwCacheDaAccessor != cda)
			{
				int[] segments = m_cache.GetVectorProperty(hvoPara, ktagParaSegments, true);
				cda.CacheVecProp(hvoPara, ktagParaSegments, segments, segments.Length);
			}
		}

		protected virtual void LoadParagraphInfoToCache(int hvoPara)
		{
			ParagraphParser.LoadParagraphInfo(StTxtPara.CreateFromDBObject(m_cache, hvoPara),
				this.Tag, InterlinVc.SegmentFormsTag(Cache), true);
		}
	}

	/// <summary>
	/// Cache the segment information for the paragraph(s), but do not calculate or cache twfics/punctuations.
	/// </summary>
	public class ParagraphSegmentsIgnoreTwficsVirtualHandler : ParagraphSegmentsVirtualHandler
	{
		public ParagraphSegmentsIgnoreTwficsVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
		}
		protected override void LoadParagraphInfoToCache(int hvoPara)
		{
			ParagraphParser.LoadParagraphInfo(StTxtPara.CreateFromDBObject(m_cache, hvoPara),
				this.Tag, 0, false);
		}
	}

	/// <summary>
	/// This virtual handler expects the object to be a CmBaseAnnotation whose BeginObject is an
	/// StTxtPara whose owner's owner is a Text. It creates a reference like Abbrv n:m, where
	/// Abbrv is the first five letters of the Name of the Text (in the indicated writing system),
	/// n is the index of the owner of the StTxtPara in the owning StText, and m is the value of
	/// the virtual property SegNumber of the CmBaseAnnotation.
	/// NOTE: we now use StTxtPara.TwficSegmentLocation which depends upon ParagraphSegmentsVirtualHandler
	/// which depends upon IText.ParagraphParser.ParseParagraph.
	/// </summary>
	public class AnnotationRefHandler : BaseVirtualHandler
	{
		FdoCache m_cache;
		/// <summary>
		/// This is constructed in the standard way for a virtual handler invoked from XML.
		/// The configuration parameter is not currently used.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public AnnotationRefHandler(XmlNode configuration, FdoCache cache)
		{
			m_cache = cache;
			SetAndCheckNames(configuration, "CmBaseAnnotation", "Reference");
			Type = (int)CellarModuleDefns.kcptMultiString;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		public static string FullScrRef(ScrTxtPara scrPara, int hvoCba, string bookName)
		{
			FdoCache cache = scrPara.Cache;
			BCVRef startRef, endRef;
			scrPara.GetBCVRefAtPosition(
				cache.GetIntProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset),
				out startRef, out endRef);
			IScripture scripture = cache.LangProject.TranslatedScriptureOA;
			string fullRef = ScrReference.MakeReferenceString(bookName, startRef, endRef,
				scripture.ChapterVerseSepr, scripture.Bridge);
			return fullRef;

		}
		/// <summary>
		/// Compute the label that should be added to the verse reference for the indexed segment of the
		/// specified paragraph, assuming it is part of Scripture. Assumes the indexed segment is not itself
		/// a verse/chapter label. The idea is to add 'a', 'b', 'c' etc. if there is more than one segment
		/// in the same verse.
		/// </summary>
		/// <returns></returns>
		public static string VerseSegLabel(ScrTxtPara para, int idxSeg, int ktagParaSegments)
		{
			ISilDataAccess sda = para.Cache.MainCacheAccessor;
			StTxtPara curPara = para;
			int idxCurSeg = idxSeg;
			int cprev = 0; // number of previous segments in same verse
			for (; ; )
			{
				if (!GetPrevSeg(ref idxCurSeg, sda, ref curPara, ktagParaSegments))
					break; // at very start of text.
				int hvoSeg = sda.get_VecItem(curPara.Hvo, ktagParaSegments, idxCurSeg);
				if (SegmentBreaker.HasLabelText(curPara.Contents.UnderlyingTsString,
					para.Cache.GetIntProperty(hvoSeg, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset),
					para.Cache.GetIntProperty(hvoSeg, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset)))
				{
					break; // some sort of verse or chapter ID, previous seg will have different ref.
				}
				cprev++;
			}
			if (cprev == 0)
			{
				// See if the FOLLOWING segment is a label. We don't care how many following segments there
				// are, except that since there are no previous ones, if there are also no following ones
				// we don't need a label at all and can return an empty string.
				curPara = para;
				idxCurSeg = idxSeg;
				if (!GetNextSeg(ref idxCurSeg, sda, ref curPara, ktagParaSegments))
					return ""; // no more segments, and no previous ones in same verse.
				int hvoCba = sda.get_VecItem(curPara.Hvo, ktagParaSegments, idxCurSeg);
				if (SegmentBreaker.HasLabelText(curPara.Contents.UnderlyingTsString,
					para.Cache.GetIntProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset),
					para.Cache.GetIntProperty(hvoCba, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset)))
				{
					return ""; // some sort of verse or chapter ID, next seg will have different ref.
				}

			}
			return MakeLabelForSegAtIndex(cprev);

		}

		private static string MakeLabelForSegAtIndex(int cprev)
		{
			return Convert.ToChar(Convert.ToInt32('a') + cprev).ToString();
		}

		/// <summary>
		/// Given that idxCurSeg is the index of a segment of curPara, adjust both until it is the index of the
		/// previous segment (possibly in an earlier paragraph). Return false if no earlier segment exists in
		/// the current text.
		/// </summary
		private static bool GetPrevSeg(ref int idxCurSeg, ISilDataAccess sda,
			ref StTxtPara curPara, int ktagParaSegments)
		{
			idxCurSeg--;
			// This while usually has no iterations, and all we do in the method is decrement idxCurSeg.
			// It is even rarer to have more than one iteration but could happen if there is an empty paragraph.
			while (idxCurSeg < 0)
			{
				// set curPara to previous para in StText. If there is none, fail.
				int idxPara = sda.GetObjIndex(curPara.OwnerHVO, (int)StText.StTextTags.kflidParagraphs, curPara.Hvo);
				if (idxPara == 0)
					return false;
				int hvoPrev = sda.get_VecItem(curPara.OwnerHVO, (int)StText.StTextTags.kflidParagraphs, idxPara - 1);
				curPara = CmObject.CreateFromDBObject(curPara.Cache, hvoPrev, false) as StTxtPara;
				idxCurSeg = sda.get_VecSize(curPara.Hvo, ktagParaSegments) - 1;
			}
			return true;
		}
		/// <summary>
		/// Given that idxCurSeg is the index of a segment of curPara, adjust both until it is the index of the
		/// next segment (possibly in a later paragraph). Return false if no earlier segment exists in
		/// the current text.
		/// </summary
		private static bool GetNextSeg(ref int idxCurSeg, ISilDataAccess sda,
			ref StTxtPara curPara, int ktagParaSegments)
		{
			idxCurSeg++;
			// This for usually exits early in the first iteration, and all we do in the method is increment idxCurSeg.
			// It is even rarer to have more than one full iteration but could happen if there is an empty paragraph.
			for (; ; )
			{
				int csegs = sda.get_VecSize(curPara.Hvo, ktagParaSegments);
				if (idxCurSeg < csegs)
					return true;
				// set curPara to next para in StText. If there is none, fail.
				int idxPara = sda.GetObjIndex(curPara.OwnerHVO, (int)StText.StTextTags.kflidParagraphs, curPara.Hvo);
				int cpara = sda.get_VecSize(curPara.OwnerHVO, (int)StText.StTextTags.kflidParagraphs);
				if (idxPara >= cpara - 1)
					return false;
				int hvoNext = sda.get_VecItem(curPara.OwnerHVO, (int)StText.StTextTags.kflidParagraphs, idxPara + 1);
				curPara = CmObject.CreateFromDBObject(curPara.Cache, hvoNext, false) as StTxtPara;
				idxCurSeg = 0;
			}
		}
		/// <summary>
		/// Load the data.
		/// </summary>
		/// <param name="hvo">a Wfic.</param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ISilDataAccess sda = cda as ISilDataAccess;
			int hvoPara = sda.get_ObjectProp(hvo, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			int hvoStText = 0;
			if (hvoPara != 0)
				hvoStText = sda.get_ObjectProp(hvoPara, (int)CmObjectFields.kflidCmObject_Owner);
			if (hvoStText == 0)
			{
				// Unusual case, possibly hvoPara is not actually a para at all, for example, it may
				// be a picture caption. For now we make an empty reference so at least it won't crash.
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ITsString dummy = tsf.MakeString("", ws);
				cda.CacheStringAlt(hvo, tag, ws, dummy);
				cda.CacheStringProp(hvo, tag, dummy);
				return;
			}
			ITsString tssName = null;
			StText stText = new StText(Cache, hvoStText);
			int wsActual = 0;
			bool fUsingAbbreviation = false;
			if (stText.OwningFlid == (int)Text.TextTags.kflidContents)
			{
				// see if we can find an abbreviation.
				Text text = stText.Owner as Text;
				tssName = text.Abbreviation.GetAlternativeOrBestTss(ws, out wsActual);
				if (wsActual > 0)
					fUsingAbbreviation = true;
			}
			else if (stText.OwningFlid == (int)ScrSection.ScrSectionTags.kflidContent)
			{
				// Body of Scripture. Figure a book/chapter/verse
				ScrTxtPara scrPara = new ScrTxtPara(m_cache, hvoPara, false, false);
				ScrBook book = new ScrBook(m_cache, Cache.GetOwnerOfObject(Cache.GetOwnerOfObject(stText.Hvo)));
				string mainRef = FullScrRef(scrPara, hvo, book.BestUIAbbrev).Trim();
				int ktagParaSegments = StTxtPara.SegmentsFlid(Cache);
				int beginOffset = Cache.GetIntProperty(hvo, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
				int chvo = sda.get_VecSize(hvoPara, ktagParaSegments);
				int idxSeg = 0;
				while (idxSeg < chvo - 1)
				{
					// Stop if the FOLLOWING segment has a larger beginOffset than the target one.
					int hvoSeg = sda.get_VecItem(hvoPara, ktagParaSegments, idxSeg + 1);
					int segBeginOffset = sda.get_IntProp(hvoSeg, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
					if (segBeginOffset > beginOffset)
						break;
					idxSeg++;
				}
				ITsString tssRef1 = Cache.MakeUserTss(mainRef + VerseSegLabel(scrPara, idxSeg, ktagParaSegments));

				cda.CacheStringProp(hvo, tag, tssRef1);
				cda.CacheStringAlt(hvo, tag, m_cache.DefaultUserWs, tssRef1);
				return;
			}
			else if (stText.OwningFlid == (int)ScrSection.ScrSectionTags.kflidHeading)
			{
				// use the section title without qualifiers.
				ITsString tssRef2 = stText.Title.GetAlternativeOrBestTss(ws, out wsActual);
				cda.CacheStringProp(hvo, tag, tssRef2);
				cda.CacheStringAlt(hvo, tag, wsActual, tssRef2);
				return;
			}
			if (wsActual == 0)
			{
				tssName = stText.Title.GetAlternativeOrBestTss(ws, out wsActual);
			}
			// if we didn't find an alternative, we'll just revert to using the 'ws' we're loading for.
			string sNotFound = null;
			if (wsActual == 0)
			{
				wsActual = ws;
				sNotFound = tssName.Text;
			}

			ITsStrBldr bldr = tssName.GetBldr();
			// If we didn't find a "best", reset to an empty string.
			if (bldr.Length > 0 && bldr.Text == sNotFound)
				bldr.ReplaceTsString(0, bldr.Length, null);
			// Truncate to 8 chars, if the user hasn't specified an abbreviation.
			// Enhance JohnT: Eventually Text will have an abbreviation property, and we will do the
			// truncate title thing only if abbreviation is empty.
			if (!fUsingAbbreviation && bldr.Length > 8)
				bldr.ReplaceTsString(8, bldr.Length, null);

			// Make a TsTextProps specifying just the writing system.
			ITsPropsBldr propBldr = TsPropsBldrClass.Create();
			propBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsActual);
			ITsTextProps props = propBldr.GetTextProps();

			// Insert a space (if there was any title)
			if (bldr.Length > 0)
				bldr.Replace(bldr.Length, bldr.Length, " ", props);

			// if Scripture.IsResponsibleFor(stText) we should try to get the verse number of the annotation.
			//if (stText.OwningFlid == (int)Text.TextTags.kflidContents)
			//{

			// Insert paragraph number.
			int cparas = sda.get_VecSize(hvoStText, (int)StText.StTextTags.kflidParagraphs);
			int ipara = 0;
			for (; sda.get_VecItem(hvoStText, (int)StText.StTextTags.kflidParagraphs, ipara) != hvoPara; ipara++)
				;
			ipara++;
			bldr.Replace(bldr.Length, bldr.Length, ipara.ToString(), props);

			// And a colon...
			bldr.Replace(bldr.Length, bldr.Length, ":", props);

			// And now the segment number
			string segnumStr = GetSegmentRange(hvo);
			bldr.Replace(bldr.Length, bldr.Length, segnumStr, props);
			//}
			ITsString tssRef = bldr.GetString();
			cda.CacheStringAlt(hvo, tag, wsActual, tssRef);
			cda.CacheStringProp(hvo, tag, tssRef);
		}

		private string GetSegmentRange(int hvoCba)
		{
			int segnumBegin = 0;
			int segnumEnd = 0;

			int iBeginSeg = -1;
			int iEndSeg = -1;
			if (StTxtPara.SegmentInfo.TryGetSegmentRange(Cache, hvoCba, out iBeginSeg, out iEndSeg))
			{
				segnumBegin = iBeginSeg + 1;
				segnumEnd = iEndSeg + 1;
			}


			string segnumStr = "";
			if (segnumBegin > 0)
			{
				segnumStr = segnumBegin.ToString();
				if (segnumEnd > segnumBegin)
					segnumStr += "-" + segnumEnd.ToString();
			}
			else
			{
				segnumStr = "?-?"; // need to reparse the paragraph to get updated offsets.
			}
			return segnumStr;
		}

		private ITsString FirstAlternativeName(ISilDataAccess sda, int hvoText, int[] rgws)
		{
			ITsString tss;
			for (int i = 0; i < rgws.Length; ++i)
			{
				tss = sda.get_MultiStringAlt(hvoText,
					(int)CmMajorObject.CmMajorObjectTags.kflidName, rgws[i]);
				if (tss.Length > 0)
					return tss;
			}
			return null;
		}
	}

	/// <summary>
	/// View constructor for InterlinView. Just to get something working, currently
	/// it is just a literal.
	/// </summary>
	public class InterlinVc : VwBaseVc
	{
		#region Constants and other similar ints.

		internal int krgbNoteLabel = 100 + (100 << 8) + (100 << 16); // equal amounts of three colors produces a gray.
		internal const int kfragInterlinPara = 100000;
		internal protected const int kfragBundle = 100001;
		internal const int kfragMorphBundle = 100002;
		internal const int kfragAnalysis = 100003;
		internal const int kfragPostfix = 100004;
		internal const int kfragMorphForm = 100005;
		internal const int kfragPrefix = 100006;
		internal const int kfragCategory = 100007;
		internal protected const int kfragTwficAnalysis = 100008;
		internal const int kfragAnalysisSummary = 100009;
		internal const int kfragAnalysisMorphs = 100010;
		//internal const int kfragSummary = 100011;
		internal const int kfragSenseName = 100012;
		internal const int kfragSingleInterlinearAnalysisWithLabels = 100013; // Recycle int for: internal const int kfragMissingGloss = 100013;
		internal const int kfragDefaultSense = 100014; // Recycle int for: internal const int kfragMissingSenseobj = 100014;
		internal const int kfragBundleMissingSense = 100015;
		internal const int kfragAnalysisMissingPos = 100016;
		internal const int kfragMsa = 100017;
		//internal const int kfragMorphs = 100018;
		internal const int kfragMissingAnalysis = 100019;
		internal const int kfragAnalysisMissingGloss = 100021;
		internal const int kfragWordformForm = 100022;
		internal const int kfragWordGlossGuess = 100023;
		internal const int kfragText = 100024;
		internal const int kfragTxtSection = 100025;
		internal const int kfragStText = 100026;
		internal const int kfragParaSegment = 100027;
		internal const int kfragSegFf = 100028;
		internal const int kfragWordGloss = 100029;
		internal const int kfragIsolatedAnalysis = 100030;
		internal const int kfragMorphType = 100031;
		internal const int kfragPossibiltyAnalysisName = 100032;
		internal const int kfragEmptyFreeTransPrompt = 100033;
		internal const int kfragSingleInterlinearAnalysisWithLabelsLeftAlign = 100034;
		// These ones are special: we select one ws by adding its index in to this constant.
		// So a good-sized range of kfrags after this must be unused for each one.
		// This one is used for isolated wordforms (e.g., in Words area) using the current list of
		// analysis writing systems.
		internal const int kfragWordGlossWs = 1001000;
		// For this ones the flid and ws are determined by figuring the index and applying it to the line choice array
		internal const int kfragLineChoices = 1002000;
		// For this we follow kflidWfiAnalysis_Category and then use the ws and StringFlid indicated
		// by the offset.
		internal const int kfragAnalysisCategoryChoices = 1003000;
		// Display a morph form (including prefix/suffix info) in the WS indicated by the line choices.
		internal const int kfragMorphFormChoices = 1004000;
		// Display a group of Wss for the same Freeform annotation, starting with the ws indicated by the
		// index obtained from the offset from kfragSegFfchoices, and continuing for as many adjacent
		// specs as have the same flid.
		internal const int kfragSegFfChoices = 1005000;
		// Constants used to identify 'fake' properties to DisplayVariant.
		//internal const int ktagAnalysisSummary = -50;
		//internal const int ktagAnalysisMissing = -51;
		//internal const int ktagSummary = -52;
		internal const int ktagBundleMissingSense = -53;
		//internal const int ktagMissingGloss = -54;
		internal const int ktagAnalysisMissingPos = -55;
		internal const int ktagMissingAnalysis = -56;
		internal const int ktagAnalysisMissingGloss = -57;
		// And constsnts used for the 'fake' properties that that break paras into
		// segments and provide defaults for wordforms
		// These two used to be constants but were made variables with dummy virtual handlers so that
		// ClearInfoAbout can clear them out.
		internal const int ktagSegmentFree = -61;
		internal const int ktagSegmentLit = -62;
		internal const int ktagSegmentNote = -63;
		internal int ktagTwficDefault = 0;
		// flids for paragraph annotation sequences.
		internal int ktagParaSegments = 0;
		internal int ktagSegmentForms = 0;
		internal int ktagSegFF = 0;
		internal int vtagStTextTitle = 0;
		internal int vtagStTextSource = 0;

		internal const int ktagAnalysisHumanApproved = -66;
		internal int tagRealForm; // caches TwficRealFormTag
		bool m_fIsAddingRealFormToView = false; // indicates we are in the context of adding real form string to the vwEnv.

		#endregion Constants and other similar ints.

		#region Data members

		protected bool m_fShowDefaultSense = false; // Use false to not change prior behavior.
		protected bool m_fHaveOpenedParagraph = false; // Use false to not change prior behavior.
		protected FdoCache m_cache;
		protected int m_hvoCurrentTwfic;
		protected int m_wsCurrentTwfic;
		protected int m_wsCurrentWordBundleVern;
		protected int m_wsVernForDisplay;
		private int m_icurLine; // Keeps track of current interlinear line (see MaxStringWidthForChartColumn)

		protected int m_wsAnalysis;
		protected int m_wsUi;
		internal WsListManager m_WsList;
		ITsString m_tssMissingAnalysis; // The whole analysis is missing. This shows up on the morphs line.
		ITsString m_tssMissingGloss; // A word gloss is missing.
		ITsString m_tssMissingSense;
		ITsString m_tssMissingMsa;
		ITsString m_tssMissingAnalysisPos;
		ITsString m_tssMissingMorph; // Shown when an analysis has no morphs (on the morphs line).
		ITsString m_tssEmptyAnalysis;  // Shown on analysis language lines when we want nothing at all to appear.
		ITsString m_tssEmptyVern;
		ITsString m_tssMissingEntry;
		ITsString m_tssEmptyPara;
		ITsString m_tssSpace;
		ITsString m_tssCommaSpace;
		int m_mpBundleHeight = 0; // millipoint height of interlinear bundle.
		ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		bool m_fShowMorphBundles = true;
		bool m_fRtl = false;
		ITsString m_tssDir;
		Dictionary<int, ITsString> m_mapWsDirTss = new Dictionary<int, ITsString>();
		// AnnotationDefns we need
		int m_hvoAnnDefTextSeg;
		int m_hvoAnnDefFT;
		int m_hvoAnnDefLT;
		int m_hvoAnnDefNote;
		int m_hvoSandboxAnnotation; // if set, display a box for this annotation (to take place of Sandbox).
		Size m_sizeSandbox = new Size(100000, 50000); // If m_hvoSandboxAnnotation is set, this gives the size of box to make. (millipoints)
		protected int m_flidStringValue; // flid for the virtual property CmBaseAnnotation.StringValue.
		MoMorphSynAnalysisUi.MsaVc m_msaVc;
		InterlinLineChoices m_lineChoices;
		IVwStylesheet m_stylesheet;
		ParaDataLoader m_loader;
		ScrTxtPara m_scrPara = null;	// used for calculating chapter:verse based segment references
		InterlinDocChild m_rootsite = null;
		private Set<int> m_vernWss; // all vernacular writing systems

		private int m_leftPadding = 0;

		#endregion Data members

		public InterlinVc(FdoCache cache)
		{
			m_cache = cache;
			m_wsAnalysis = cache.LangProject.DefaultAnalysisWritingSystem;
			m_wsUi = cache.LanguageWritingSystemFactoryAccessor.UserWs;

			PreferredVernWs = cache.LangProject.DefaultVernacularWritingSystem;

			m_tssMissingGloss = m_tsf.MakeString(ITextStrings.ksStars, m_wsAnalysis);
			m_tssMissingSense = m_tssMissingGloss;
			m_tssMissingMsa = m_tssMissingGloss;
			m_tssMissingAnalysisPos = m_tssMissingGloss;
			m_tssEmptyAnalysis = m_tsf.MakeString("", m_wsAnalysis);
			m_WsList = new WsListManager(m_cache);
			m_tssEmptyPara = m_tsf.MakeString(ITextStrings.ksEmptyPara, m_wsAnalysis);
			m_tssSpace = m_tsf.MakeString(" ", m_wsAnalysis);
			m_flidStringValue = CmBaseAnnotation.StringValuePropId(m_cache);
			m_msaVc = new MoMorphSynAnalysisUi.MsaVc(m_cache);
			m_vernWss = LangProject.GetAllWritingSystems("all vernacular", m_cache, null, 0, 0);

			// This usually gets overridden, but ensures default behavior if not.
			m_lineChoices = InterlinLineChoices.DefaultChoices(0, m_cache.DefaultAnalWs, m_cache.LangProject);
			// These two used to be constants but were made variables with dummy virtual handlers so that
			// ClearInfoAbout can clear them out.
			ktagTwficDefault = TwficDefaultTag(m_cache);
			ktagSegmentForms = SegmentFormsTag(m_cache);
			tagRealForm = TwficRealFormTag(m_cache);
			GetSegmentLevelTags(cache);
		}

		/// <summary>
		/// Keeps track of the current interlinear line in a bundle being displayed.
		/// </summary>
		public int CurrentLine
		{
			get { return m_icurLine; }
		}

		/// <summary>
		/// Normally gets some virtual property tags we need for stuff above the bundle level.
		/// Code that is only using fragments at or below bundle may override this to do nothing,
		/// and then need not set up the virtual property handlers. See ConstChartVc.
		/// </summary>
		/// <param name="cache"></param>
		protected virtual void GetSegmentLevelTags(FdoCache cache)
		{
			vtagStTextTitle = BaseVirtualHandler.GetInstalledHandlerTag(cache, "StText", "Title");
			vtagStTextSource = BaseVirtualHandler.GetInstalledHandlerTag(cache, "StText", "SourceOfText");
			ktagParaSegments = ParaSegmentTag(m_cache);
			ktagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(m_cache);
		}

		/// <summary>
		/// setups up the display to work with the given wsVern.
		/// </summary>
		/// <param name="wsVern"></param>
		private void SetupRealVernWsForDisplay(int wsVern)
		{
			if (wsVern <= 0)
				throw new ArgumentException(String.Format("Expected a real vernacular ws (got {0}).", wsVern));
			if (m_wsVernForDisplay == wsVern)
				return;	// already setup
			m_wsVernForDisplay = wsVern;
			StringUtils.ReassignTss(ref m_tssEmptyVern, m_tsf.MakeString("", wsVern));
			SetupRightToLeft(wsVern);
			SetupVernForWordBundle(wsVern);
		}

		private void SetupRightToLeft(int wsVern)
		{
			IWritingSystem wsObj = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsVern);
			if (wsObj != null)
				m_fRtl = wsObj.RightToLeft;
			if (m_fRtl)
				m_tssDir = m_tsf.MakeString("\x200F", wsVern);	// RTL Mark.
			else
				m_tssDir = m_tsf.MakeString("\x200E", wsVern);	// LTR Mark.
		}

		private int CurrentTwficHvo
		{
			get { return m_hvoCurrentTwfic; }
		}

		private int CurrentTwficWs
		{
			get { return m_wsCurrentTwfic; }
		}

		/// <summary>
		/// Setup the display for the given twfic.
		/// Use SetupForTwfic(0) when the display is finished with this twfic context.
		/// </summary>
		/// <param name="hvoTwfic"></param>
		protected void SetupForTwfic(int hvoTwfic)
		{
			if (m_hvoCurrentTwfic == hvoTwfic)
				return; // already setup.
			m_hvoCurrentTwfic = hvoTwfic;
			int wsTwfic = 0;
			if (hvoTwfic != 0)
			{
				wsTwfic = m_cache.LangProject.ActualWs(LangProject.kwsVernInParagraph, hvoTwfic, InterlinDocChild.TagAnalysis);
				m_wsCurrentTwfic = wsTwfic;
				SetupVernForWordBundle(wsTwfic);
			}
			else
			{
				// revert back to default for this paragraph.
				SetupVernForWordBundle(m_wsVernForDisplay);
			}
		}

		/// <summary>
		/// Answer true if the specified word can be anlyzed. This is a further check after
		/// ensuring it has an InstanceOf. It is equivalent to the check made in case kfragBundle of
		/// Display(), but that already has access to the writing system of the Wfic.
		/// </summary>
		/// <param name="hvoWfic"></param>
		/// <returns></returns>
		internal bool CanBeAnalyzed(int hvoWfic)
		{
			int wficWs = StTxtPara.GetTwficWs(m_cache, hvoWfic);
			return wficWs == m_wsVernForDisplay || m_vernWss.Contains(wficWs);
		}

		/// <summary>
		/// Setup to display vernacular things in the context of the current twfic.
		/// </summary>
		/// <param name="wsVern"></param>
		private void SetupVernForWordBundle(int wsVern)
		{
			if (m_wsCurrentWordBundleVern == wsVern)
				return;	// already setup.
			m_wsCurrentWordBundleVern = wsVern;
			if (wsVern != 0)
			{
				StringUtils.ReassignTss(ref m_tssMissingAnalysis, m_tsf.MakeString(ITextStrings.ksStars, wsVern));
				m_tssMissingMorph = m_tssMissingAnalysis;
				m_tssMissingEntry = m_tssMissingAnalysis;
			}
		}

		/// <summary>
		/// This virtual property stores all the annotations in a segment
		/// (CmAnnotationDefn.Twfic and CmAnnotationDefn.Punctuation)
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		internal static int SegmentFormsTag(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"CmBaseAnnotation", "SegmentForms", (int)CellarModuleDefns.kcptReferenceSequence).Tag;
		}

		/// <summary>
		/// This dummy property is used to store the 'real' form of a Twfic, to display as the default
		/// vernacular baseline in place of the form of the WfiWordform. This property is cached if
		/// the wordform does not exactly match the text of the paragraph.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int TwficRealFormTag(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"CmBaseAnnotation", "RealForm", (int)CellarModuleDefns.kcptReferenceSequence).Tag;
		}

		/// <summary>
		/// This dummy property is used to store the lowercase wfiwordform matching a sentence
		/// initial annotation with insignificant analysis.  This is needed to allow guesses
		/// based on the lowercase form if no analyses exist matching the capitalized form.
		/// See LT-7020 for motivation.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int MatchingLowercaseWordForm(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"CmBaseAnnotation", "MatchingLowercaseWordForm", (int)CellarModuleDefns.kcptReferenceAtom).Tag;
		}

		internal static int ParaSegmentTag(FdoCache cache)
		{
			return StTxtPara.SegmentsFlid(cache);
		}

		internal static int TwficDefaultTag(FdoCache cache)
		{
			return StTxtPara.TwficDefaultFlid(cache);
		}

		internal IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();
				return m_stylesheet;
			}
			set
			{
				CheckDisposed();
				m_stylesheet = value;
			}
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_WsList != null)
					m_WsList.Dispose();
				if (m_msaVc != null)
					m_msaVc.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_msaVc = null;
			m_cache = null;

			m_tssMissingMorph = null; // Same as m_tssMissingAnalysis.
			StringUtils.ReassignTss(ref m_tssMissingAnalysis, null);

			m_tssMissingSense = null; // Same as m_tssMissingGloss.
			m_tssMissingMsa = null; // Same as m_tssMissingGloss.
			m_tssMissingAnalysisPos = null; // Same as m_tssMissingGloss.
			StringUtils.ReassignTss(ref m_tssMissingGloss, null);

			m_tssMissingEntry = null; // Same as m_tssEmptyAnalysis.
			StringUtils.ReassignTss(ref m_tssEmptyAnalysis, null);
			StringUtils.ReassignTss(ref m_tssEmptyVern, null);
			StringUtils.ReassignTss(ref m_tssEmptyPara, null);
			StringUtils.ReassignTss(ref m_tssSpace, null);

			Marshal.ReleaseComObject(m_tsf);
			m_tsf = null;

			StringUtils.ReassignTss(ref m_tssCommaSpace, null);
			m_WsList = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public InterlinLineChoices LineChoices
		{
			get
			{
				CheckDisposed();
				return m_lineChoices;
			}
			set
			{
				CheckDisposed();
				m_lineChoices = value;
			} // Note: caller responsible to Reconstruct if needed!
		}

		/// <summary>
		/// The direction of the paragraph.
		/// </summary>
		public bool RightToLeft
		{
			get
			{
				CheckDisposed();
				return m_fRtl;
			}
		}

		/// <summary>
		/// Gets or sets the left padding for a single interlin analysis that is always left-aligned.
		/// </summary>
		/// <value>The left padding.</value>
		public int LeftPadding
		{
			get
			{
				CheckDisposed();
				return m_leftPadding;
			}

			set
			{
				CheckDisposed();
				m_leftPadding = value;
			}
		}

		/// <summary>
		/// Indicates we are in the context of adding real form string to the vwEnv
		/// </summary>
		internal bool IsDoingRealWordForm
		{
			get
			{
				CheckDisposed();
				return m_fIsAddingRealFormToView;
			}
			set
			{
				CheckDisposed();
				m_fIsAddingRealFormToView = value;
			}
		}

		/// <summary>
		/// Call this to clear the temporary cache of analyses. Minimally do this when
		/// data changes. Ideally no more often than necessary.
		/// </summary>
		public void ResetAnalysisCache()
		{
			CheckDisposed();

			m_loader = null;
		}

		private ITsString CommaSpaceString
		{
			get
			{
				if (m_tssCommaSpace == null)
					m_tssCommaSpace = m_tsf.MakeString(", ", m_wsAnalysis);
				return m_tssCommaSpace;
			}
		}

		public WsListManager ListManager
		{
			get
			{
				CheckDisposed();
				return m_WsList;
			}
		}

		/// <summary>
		/// Background color indicating a guess that has been approved by a human for use somewhere.
		/// </summary>
		public static int ApprovedGuessColor
		{
			get { return (int)CmObjectUi.RGB(200, 255, 255); }
		}

		/// <summary>
		/// Background color for a guess that no human has ever endorsed directly.
		/// </summary>
		public static int MachineGuessColor
		{
			get { return (int)CmObjectUi.RGB(254, 240, 206); }
			//get { return (int)CmObjectUi.RGB(255, 219, 183); }
		}

		/// <summary>
		/// </summary>
		internal InterlinDocChild RootSite
		{
			get { return m_rootsite; }
			set { m_rootsite = value; }
		}

		/// <summary>
		/// Clients, can supply a real vernacular alternative ws to be used for this display
		/// for lines where we can't find an appropriate one. If none is provide, we'll use cache.DefaultVernWs.
		/// </summary>
		public int PreferredVernWs
		{
			get
			{
				CheckDisposed();
				return m_wsVernForDisplay;
			}
			set
			{
				CheckDisposed();
				SetupRealVernWsForDisplay(value);
			}
		}

		// Controls whether to display the morpheme bundles.
		public bool ShowMorphBundles
		{
			get
			{
				CheckDisposed();
				return m_fShowMorphBundles;
			}
			set
			{
				CheckDisposed();
				m_fShowMorphBundles = value;
			}
		}

		// Controls whether to display the default sense (true), or the normal '***' row.
		public bool ShowDefaultSense
		{
			get
			{
				CheckDisposed();
				return m_fShowDefaultSense;
			}
			set
			{
				CheckDisposed();
				m_fShowDefaultSense = value;
			}
		}

		/// <summary>
		/// Set the annotation that is displayed as a fix-size box on top of which the SandBox is overlayed.
		/// Client must also do PropChanged to produce visual effect.
		/// Size is in millipoints!
		/// </summary>
		public int SandboxAnnotation
		{
			get
			{
				CheckDisposed();
				return m_hvoSandboxAnnotation;
			}
			set
			{
				CheckDisposed();
				m_hvoSandboxAnnotation = value;
			}
		}

		/// <summary>
		/// Set the size of the space reserved for the Sandbox. Client must also do a Propchanged to trigger
		/// visual effect.
		/// </summary>
		public Size SandboxSize
		{
			get
			{
				CheckDisposed();
				return m_sizeSandbox;
			}
			set
			{
				CheckDisposed();
				m_sizeSandbox = value;
			}
		}

		virtual protected int LabelRGBFor(int choiceIndex)
		{
			return LabelRGBFor(m_lineChoices[choiceIndex]);
		}

		virtual protected int LabelRGBFor(InterlinLineSpec spec)
		{
			return m_lineChoices.LabelRGBFor(spec);
		}

		/// <summary>
		/// Called right before adding a string or opening a flow object, sets its color.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="color"></param>
		protected virtual void SetColor(IVwEnv vwenv, int color)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, color);
		}

		/// <summary>
		/// Add the specified string in the specified color to the display, using the UI Writing system.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="color"></param>
		/// <param name="str"></param>
		protected void AddColoredString(IVwEnv vwenv, int color, string str)
		{
			SetColor(vwenv, color);
			vwenv.AddString(m_tsf.MakeString(str, m_wsUi));
		}

		/// <summary>
		/// Set the background color that we use to indicate a guess.
		/// </summary>
		/// <param name="vwenv"></param>
		private void SetGuessing(IVwEnv vwenv, bool fHumanApproved)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(fHumanApproved ? ApprovedGuessColor : MachineGuessColor));
		}

		private void SetGuessing(IVwEnv vwenv)
		{
			SetGuessing(vwenv, true);
		}

		// Get a default analysis for the current twfic, which is the next object out
		// in the display hierarchy, if it's in the cache.
		// (If not don't try to read it, it's a fake property the cache doesn't know how to load.
		// Just return the hvoDefault, which is the current analysis of the twfic.)
		private int GetDefault(IVwEnv vwenv, int hvoDefault)
		{
			int hvoTwfic, tag, index;
			vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoTwfic, out tag, out index);
			if (m_cache.MainCacheAccessor.get_IsPropInCache(hvoTwfic, ktagTwficDefault,
				(int)CellarModuleDefns.kcptReferenceAtom, 0))
			{

				int hvoResult = m_cache.GetObjProperty(hvoTwfic, ktagTwficDefault);
				if (hvoResult != 0)
					return hvoResult;  // may have been cleared by setting to zero.
			}
			return hvoDefault;
		}

		// Set the properties that make the labels like "Note" 'in a fainter font" than the main text.
		private void SetNoteLabelProps(IVwEnv vwenv)
		{
			SetColor(vwenv, krgbNoteLabel);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();
			if (hvo == 0)
				return;		// Can't do anything without an hvo (except crash -- see LT-9348).

#if DEBUG
			//TimeRecorder.Begin("Display");
#endif
			switch (frag)
			{
			case kfragText: // The whole text, root object for the InterlinDocChild.
				vwenv.AddObjProp((int)Text.TextTags.kflidContents, this, kfragStText);
				break;
				//			case kfragTxtSection: // obsolete
				//				// Enhance JohnT: possibly some extra space, bold the heading, or whatever?
				//				vwenv.AddObjProp((int)TxtSection.TxtSectionTags.kflidHeading, this, kfragStText);
				//				vwenv.AddObjProp((int)TxtSection.TxtSectionTags.kflidContents, this, kfragStText);
				//				vwenv.AddLazyVecItems((int)TxtSection.TxtSectionTags.kflidSubsections, this, kfragTxtSection);
				//				break;
			case kfragStText:	// new root object for InterlinDocChild.
				SetupRealVernWsForDisplay(m_cache.LangProject.ActualWs(LangProject.kwsVernInParagraph,
					hvo, (int)StText.StTextTags.kflidParagraphs));
				vwenv.AddLazyVecItems((int)StText.StTextTags.kflidParagraphs, this, kfragInterlinPara);
				break;
			case kfragInterlinPara: // Whole StTxtPara. This can be the root fragment in DE view.
				if (vwenv.DataAccess.get_VecSize(hvo, ktagParaSegments) == 0)
				{
					vwenv.NoteDependency(new int[] {hvo}, new int[] {ktagParaSegments}, 1);
					vwenv.AddString(m_tssEmptyPara);
				}
				else
				{
					PreferredVernWs = m_cache.LangProject.ActualWs(LangProject.kwsVernInParagraph, hvo, ktagParaSegments);
					// Include the plain text version of the paragraph?
					vwenv.AddLazyVecItems(ktagParaSegments, this, kfragParaSegment);
				}
				break;
			case kfragParaSegment:
				// Don't put anything in this segment if it is a 'label' segment (typically containing a verse
				// number for TE).
				CmBaseAnnotation seg = CmObject.CreateFromDBObject(m_cache, hvo, false) as CmBaseAnnotation;
				StTxtPara para = seg.BeginObjectRA as StTxtPara;
				if (SegmentBreaker.HasLabelText(para.Contents.UnderlyingTsString, seg.BeginOffset, seg.EndOffset))
					break;
				// This puts ten points between segments. There's always 5 points below each line of interlinear;
				// if there are no freeform annotations another 5 points makes 10 between segments.
				// If there are freeforms, we need the full 10 points after the last of them.
				int cfreeform = vwenv.DataAccess.get_VecSize(hvo, ktagSegFF);
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
					(int)FwTextPropVar.ktpvMilliPoint, cfreeform == 0 ? 5000 : 10000);
				vwenv.OpenDiv();
				// Enhance JohnT: determine what the overall direction of the paragraph should
				// be and set it.
				if (m_mpBundleHeight == 0)
				{
					// First time...figure it out.
					int dmpx, dmpyAnal, dmpyVern;
					vwenv.get_StringWidth(m_tssEmptyAnalysis, null, out dmpx, out dmpyAnal);
					vwenv.get_StringWidth(m_tssEmptyVern, null, out dmpx, out dmpyVern);
					m_mpBundleHeight = dmpyAnal * 4 + dmpyVern * 3;
				}
				// The interlinear bundles are not editable.
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				if (m_fRtl)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
						(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
						(int)FwTextPropVar.ktpvEnum, (int) FwTextAlign.ktalRight);
				}
				vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
					(int)SpellingModes.ksmDoNotCheck);
				vwenv.OpenParagraph();
				AddSegmentReference(vwenv, hvo);	// Calculate and display the segment reference.
				AddLabelPile(vwenv, m_tsf, m_cache, true, m_fShowMorphBundles);
				vwenv.AddObjVecItems(ktagSegmentForms, this, kfragBundle);
				// JohnT, 1 Feb 2008. Took this out as I can see no reason for it; AddObjVecItems handles
				// the dependency already. Adding it just means that any change to the forms list
				// regenerates a higher level than needed, which contributes to a great deal of scrolling
				// and flashing (LT-7470).
				// Originally added by Eric in revision 72 on the trunk as part of handling phrases.
				// Eric can't see any reason we need it now, either. If you find a need to re-insert it,
				// please document carefully the reasons it is needed and what bad consequences follow
				// from removing it.
				//vwenv.NoteDependency(new int[] { hvo }, new int[] { ktagSegmentForms }, 1);
				vwenv.CloseParagraph();
				// This puts 3 points of margin on the first FF annotation, if any.
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop,
					(int)FwTextPropVar.ktpvMilliPoint, 0); // 3000
				vwenv.AddObjVec(ktagSegFF, this, kfragSegFf);
				vwenv.CloseDiv();
				break;
			case kfragBundle: // One annotated word bundle; hvo is CmBaseAnnotation.
				// checking AllowLayout (especially in context of Undo/Redo make/break phrase)
				// helps prevent us from rebuilding the display until we've finished
				// reconstructing the data and cache. Otherwise we can crash.
				if (m_rootsite != null && !m_rootsite.AllowLayout)
					return;
				// set the display WS here even though it is set in the paragraph frag, since this frag might
				// get called on its own during a prop update
				int paraHvo = m_cache.GetObjProperty(hvo, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
				if (paraHvo != 0)
					PreferredVernWs = m_cache.LangProject.ActualWs(LangProject.kwsVernInParagraph, paraHvo, ktagParaSegments);
				SetupForTwfic(hvo);
				// Give whatever box we make 10 points of separation from whatever follows.
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
					(int)FwTextPropVar.ktpvMilliPoint, 10000);
				if (hvo == m_hvoSandboxAnnotation)
				{
					// Leave room for the Sandbox instead of displaying the internlinear data.
					// The first argument makes it invisible in case a little bit of it shows around
					// the sandbox.
					// The last argument puts the 'Baseline' of the sandbox (which aligns with the base of the
					// first line of text) an appropriate distance from the top of the Sandbox. This aligns it's
					// top line of text properly.
					// Enhance JohnT: 90% of font height is not always exactly right, but it's the closest
					// I can get wihtout a new API to get the exact ascent of the font.
					int dympBaseline = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.GetFontHeightForStyle("Normal", m_stylesheet,
						m_wsCurrentTwfic, m_cache.LanguageWritingSystemFactoryAccessor) * 9 / 10;
					vwenv.AddSimpleRect(0xC0000000, // FwTextColor.kclrTransparent won't convert to uint
						SandboxSize.Width, SandboxSize.Height, -(SandboxSize.Height - dympBaseline));
					SetupForTwfic(0);
					break;
				}
				// Make an 'inner pile' to contain the wordform and annotations.
				// 10 points below also helps space out the paragraph.
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
					(int)FwTextPropVar.ktpvMilliPoint, 5000);
				vwenv.OpenInnerPile();
				// Get the instanceOf property of the annotation and see whether it exists. If not it is
				// just a punctuation annotation, and we just insert the form.
				vwenv.NoteDependency(new int[] { hvo }, new int[] { InterlinDocChild.TagAnalysis }, 1);
				int hvoInstanceOf = vwenv.DataAccess.get_ObjectProp(hvo, InterlinDocChild.TagAnalysis);
				// Treat a non-vernacular word as unconnected to any kind of analysis.
				if (m_wsCurrentTwfic != m_wsVernForDisplay || !m_vernWss.Contains(m_wsCurrentTwfic))
				{
					// Cf CanBeAnalyzed method.
					hvoInstanceOf = 0;
				}
				if (hvoInstanceOf == 0)
				{
					vwenv.AddStringProp(m_flidStringValue, this);
				}
				else
				{
					// It's a full Twfic annotation, display the full bundle.
					vwenv.AddObjProp(InterlinDocChild.TagAnalysis,	this, kfragTwficAnalysis);
				}
				AddExtraTwficRows(vwenv, hvo);
				//vwenv.AddObjProp(ktagTwficDefault, this, kfragTwficAnalysis);
				vwenv.CloseInnerPile();
				// revert back to the paragraph vernWs.
				SetupForTwfic(0);
				break;
			case kfragTwficAnalysis:
				new DisplayWordBundleMethod(vwenv, hvo, this, tagRealForm).Run();
				break;
			case kfragIsolatedAnalysis: // This one is used for an isolated HVO that is surely an analyis.
			{
				// In some ways this is a simplified kfragTwficAnalysis.
				vwenv.AddObj(m_cache.GetOwnerOfObject(hvo), this, kfragWordformForm);
				if (m_fShowMorphBundles)
					vwenv.AddObj(hvo, this, kfragAnalysisMorphs);

				int chvoGlosses = m_cache.GetVectorSize(hvo,
					(int)WfiAnalysis.WfiAnalysisTags.kflidMeanings);
				for (int i = 0; i < m_WsList.AnalysisWsIds.Length; ++i)
				{
					SetColor(vwenv, LabelRGBFor(m_lineChoices.IndexOf(InterlinLineChoices.kflidWordGloss,
						m_WsList.AnalysisWsIds[i])));
					if (chvoGlosses == 0)
					{
						// There are no glosses, display something indicating it is missing.
						vwenv.AddProp(ktagAnalysisMissingGloss, this, kfragAnalysisMissingGloss);
					}
					else
					{
						vwenv.AddObjVec((int)WfiAnalysis.WfiAnalysisTags.kflidMeanings, this, kfragWordGlossWs + i);
					}
				}
				AddAnalysisPos(vwenv, hvo, -1);
			}
				break;
			case kfragAnalysisMorphs:
				int cmorphs = m_cache.GetVectorSize(hvo, (int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles);
				if (!m_fHaveOpenedParagraph)
					vwenv.OpenParagraph();
				if (cmorphs == 0)
				{
					DisplayMorphBundle(vwenv, 0);
				}
				else
				{
					vwenv.AddObjVecItems((int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, this, kfragMorphBundle);
				}
				if (!m_fHaveOpenedParagraph)
					vwenv.CloseParagraph();
				break;

			case kfragWordGloss:	// displaying forms of a known WfiGloss.
				foreach (int wsId in m_WsList.AnalysisWsIds)
				{
					SetColor(vwenv, LabelRGBFor(m_lineChoices.IndexOf(InterlinLineChoices.kflidWordGloss, wsId)));
					vwenv.AddStringAltMember((int)WfiGloss.WfiGlossTags.kflidForm,
						wsId, this);
				}
				break;
			case kfragMorphType: // for export only at present, display the
				vwenv.AddObjProp((int)MoForm.MoFormTags.kflidMorphType, this, kfragPossibiltyAnalysisName);
				break;
			case kfragPossibiltyAnalysisName:
				vwenv.AddStringAltMember((int)CmPossibility.CmPossibilityTags.kflidName, m_cache.DefaultAnalWs, this);
				break;

			case kfragMorphBundle: // the lines of morpheme information (hvo is a WfiMorphBundle)
				// Make an 'inner pile' to contain the bundle of morph information.
				// Give it 10 points of separation from whatever follows.
				DisplayMorphBundle(vwenv, hvo);
				break;
			case kfragSingleInterlinearAnalysisWithLabels:
				/*
				// This puts ten points between segments. There's always 5 points below each line of interlinear;
				// if there are no freeform annotations another 5 points makes 10 between segments.
				// If there are freeforms, we need the full 10 points after the last of them.
				int cfreeform = vwenv.get_DataAccess().get_VecSize(hvo, ktagSegFF);
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
					(int)FwTextPropVar.ktpvMilliPoint, cfreeform == 0 ? 5000 : 10000);
				*/
				vwenv.OpenDiv();
				DisplaySingleInterlinearAnalysisWithLabels(vwenv, hvo);
				vwenv.CloseDiv();
				break;
			// This frag is used to display a single interlin analysis that is always left-aligned, even for RTL languages
			case kfragSingleInterlinearAnalysisWithLabelsLeftAlign:
				vwenv.OpenDiv();
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, m_leftPadding);
				vwenv.OpenParagraph();
				vwenv.OpenInnerPile();
				DisplaySingleInterlinearAnalysisWithLabels(vwenv, hvo);
				vwenv.CloseInnerPile();
				vwenv.CloseParagraph();
				vwenv.CloseDiv();
				break;
			//case kfragDefaultSense: // Some default sense
			//    // NB: If the hvo is zero, then we need to go back to the normal missing sense display, after all.
			//    // (hvo isn't zero, even for cases where there isn't even a default value.)
			//    if (hvo > 0)
			//    {
			//        // Show default sense, in some other 'guess' color.
			//        SetGuessing(vwenv, false);
			//        foreach (int wsId in m_WsList.AnalysisWsIds)
			//            vwenv.AddStringAltMember((int)LexSense.LexSenseTags.kflidGloss,
			//                wsId, this);
			//    }
			//    else
			//    {
			//        // Give up and show the missing sense row.
			//        vwenv.AddString(m_tssMissingSense);
			//    }
			//    break;
			case kfragWordformForm: // The form of a WviWordform.
				vwenv.AddStringAltMember((int)WfiWordform.WfiWordformTags.kflidForm,
					m_wsCurrentWordBundleVern, this);
				break;
			case kfragPrefix:
				vwenv.AddUnicodeProp((int)MoMorphType.MoMorphTypeTags.kflidPrefix, m_wsCurrentWordBundleVern, this);
				break;
			case kfragPostfix:
				vwenv.AddUnicodeProp((int)MoMorphType.MoMorphTypeTags.kflidPostfix, m_wsCurrentWordBundleVern, this);
				break;
			case kfragSenseName: // The name (gloss) of a LexSense.
				foreach (int wsId in m_WsList.AnalysisWsIds)
					vwenv.AddStringAltMember((int)LexSense.LexSenseTags.kflidGloss,
						wsId, this);
				break;
			case kfragCategory: // the category of a WfiAnalysis, a part of speech;
				// display the Abbreviation property inherited from CmPossibility.
				foreach(int wsId in m_WsList.AnalysisWsIds)
				{
					vwenv.AddStringAltMember(
						(int)CmPossibility.CmPossibilityTags.kflidAbbreviation,
						wsId, this);
				}
				break;
			default:
				if (frag >= kfragWordGlossWs && frag < kfragWordGlossWs + m_WsList.AnalysisWsIds.Length)
				{
					// Displaying one ws of the  form of a WfiGloss.
					int ws = m_WsList.AnalysisWsIds[frag - kfragWordGlossWs];
					vwenv.AddStringAltMember((int)WfiGloss.WfiGlossTags.kflidForm, ws, this);
				}
				else if (frag >= kfragLineChoices && frag < kfragLineChoices + m_lineChoices.Count)
				{
					InterlinLineSpec spec = m_lineChoices[frag - kfragLineChoices];
					int ws = GetRealWs(hvo, spec);
					// The wrong value can be displayed in at least the LexGloss and WordCat fields,
					// both of which are analysis fields (at least if vern and anal ws are the same).
					// See LT-8682.
					bool fVernWs = IsVernWs(ws, spec.WritingSystem);
					if (m_wsCurrentTwfic != 0 && ws == m_wsCurrentTwfic && fVernWs)
					{
						if (m_cache.MainCacheAccessor.get_IsPropInCache(m_hvoCurrentTwfic, tagRealForm,
							(int)CellarModuleDefns.kcptString, 0))
						{
							// overridden.
							vwenv.AddString(m_cache.MainCacheAccessor.get_StringProp(m_hvoCurrentTwfic, tagRealForm));
							break;
						}
					}
					vwenv.AddStringAltMember(spec.StringFlid, ws, this);
				}
				else if (frag >= kfragAnalysisCategoryChoices && frag < kfragAnalysisCategoryChoices + m_lineChoices.Count)
				{
					AddAnalysisPos(vwenv, hvo, frag - kfragAnalysisCategoryChoices);
				}
				else if (frag >= kfragMorphFormChoices && frag < kfragMorphFormChoices + m_lineChoices.Count)
				{
					InterlinLineSpec spec = m_lineChoices[frag - kfragMorphFormChoices];
					int wsActual = GetRealWs(hvo, spec);
					DisplayMorphForm(vwenv, hvo, wsActual);
				}
				else if (frag >= kfragSegFfChoices && frag < kfragSegFfChoices + m_lineChoices.Count)
				{
					int[] wssAnalysis = m_lineChoices.AdjacentWssAtIndex(frag - kfragSegFfChoices);
					if (wssAnalysis.Length == 0)
						break; // This is bizarre, but for the sake of paranoia...
					vwenv.OpenDiv();
					SetParaDirectionAndAlignment(vwenv, wssAnalysis[0]);
					vwenv.OpenParagraph();
					int hvoType = m_cache.MainCacheAccessor.get_ObjectProp(hvo,
																		   (int)
																		   CmAnnotation.CmAnnotationTags.
																			   kflidAnnotationType);
					string label = "";
					if (hvoType == NoteSegmentDefn)
						label = ITextStrings.ksNote_;
					else if (hvoType == FtSegmentDefn)
						label = ITextStrings.ksFree_;
					else if (hvoType == LtSegmentDefn)
						label = ITextStrings.ksLit_;
					else
						throw new Exception("Unexpected FF annotation type");
					InterlinearExporter exporter = vwenv as InterlinearExporter;
					if (exporter != null)
					{
						if (hvoType == NoteSegmentDefn)
							exporter.FreeAnnotationType = "note";
						else if (hvoType == FtSegmentDefn)
							exporter.FreeAnnotationType = "gls";
						else if (hvoType == LtSegmentDefn)
							exporter.FreeAnnotationType = "lit";
					}
					SetNoteLabelProps(vwenv);
					ITsStrBldr tsbLabel = m_tsf.GetBldr();
					tsbLabel.ReplaceTsString(0, tsbLabel.Length, m_cache.MakeUserTss(label));
					tsbLabel.SetIntPropValues(0, tsbLabel.Length, (int) FwTextPropType.ktptBold,
											  (int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn);
					// REVIEW: Should we set the label to a special color as well?
					ITsString tssLabel = tsbLabel.GetString();
					int labelWidth = 0;
					int labelHeight; // unused
					if (wssAnalysis.Length > 1)
						vwenv.get_StringWidth(tssLabel, null, out labelWidth, out labelHeight);
					if (IsWsRtl(wssAnalysis[0]) != m_fRtl)
					{
						ITsStrBldr bldr = tssLabel.GetBldr();
						bldr.Replace(bldr.Length - 1, bldr.Length, null, null);
						ITsString tssLabelNoSpace = bldr.GetString();
						// (First) analysis language is upstream; insert label at end.
						vwenv.AddString(GetTssDirForWs(wssAnalysis[0]));
						AddFreeformComment(vwenv, hvo, wssAnalysis[0], hvoType);
						vwenv.AddString(GetTssDirForWs(wssAnalysis[0]));
						if (wssAnalysis.Length != 1)
						{
							// Insert WS label for first line
							vwenv.AddString(m_tssDir);
							vwenv.AddString(m_tssSpace);
							vwenv.AddString(m_tssDir);
							SetNoteLabelProps(vwenv);
							vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[0]));
						}
						vwenv.AddString(m_tssDir);
						vwenv.AddString(m_tssSpace);
						vwenv.AddString(m_tssDir);
						vwenv.AddString(tssLabelNoSpace);
						vwenv.AddString(m_tssDir);
					}
					else
					{
						vwenv.AddString(m_tssDir);
						vwenv.AddString(tssLabel);
						vwenv.AddString(m_tssDir);
						if (wssAnalysis.Length == 1)
						{
							vwenv.AddString(GetTssDirForWs(wssAnalysis[0]));
							AddFreeformComment(vwenv, hvo, wssAnalysis[0], hvoType);
						}
						else
						{
							SetNoteLabelProps(vwenv);
							vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[0]));
							vwenv.AddString(m_tssDir);
							vwenv.AddString(m_tssSpace);
							// label width unfortunately does not include trailing space.
							vwenv.AddString(m_tssDir);
							vwenv.AddString(GetTssDirForWs(wssAnalysis[0]));
							AddFreeformComment(vwenv, hvo, wssAnalysis[0], hvoType);
						}
					}
					// Add any other lines, each in its appropriate direction.
					for (int i = 1; i < wssAnalysis.Length; i++)
					{
						vwenv.CloseParagraph();
						// Indent subsequent paragraphs by the width of the main label.
						if (IsWsRtl(wssAnalysis[i]) != m_fRtl)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptTrailingIndent,
												  (int)FwTextPropVar.ktpvMilliPoint, labelWidth);
						}
						else
						{
							vwenv.set_IntProperty((int) FwTextPropType.ktptLeadingIndent,
												  (int) FwTextPropVar.ktpvMilliPoint, labelWidth);
						}
						SetParaDirectionAndAlignment(vwenv, wssAnalysis[i]);
						vwenv.OpenParagraph();
						if (IsWsRtl(wssAnalysis[i]) != m_fRtl)
						{
							// upstream...reverse everything.
							vwenv.AddString(GetTssDirForWs(wssAnalysis[i]));
							AddFreeformComment(vwenv, hvo, wssAnalysis[i], hvoType);
							vwenv.AddString(GetTssDirForWs(wssAnalysis[i]));
							vwenv.AddString(m_tssDir);
							vwenv.AddString(m_tssSpace);
							vwenv.AddString(m_tssDir);
							vwenv.AddString(m_tssDir);
							SetNoteLabelProps(vwenv);
							vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[i]));
							vwenv.AddString(m_tssDir);
							vwenv.AddString(m_tssSpace);
							vwenv.AddString(m_tssDir);
						}
						else
						{
							vwenv.AddString(m_tssDir);
							vwenv.AddString(m_tssSpace);
							vwenv.AddString(m_tssDir);
							SetNoteLabelProps(vwenv);
							vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[i]));
							vwenv.AddString(m_tssDir);
							vwenv.AddString(m_tssSpace);
							vwenv.AddString(m_tssDir);
							vwenv.AddString(GetTssDirForWs(wssAnalysis[i]));
							AddFreeformComment(vwenv, hvo, wssAnalysis[i], hvoType);
						}
					}


					vwenv.CloseParagraph();
					vwenv.CloseDiv();
				}
				else
				{
					throw new Exception("Bad fragment ID in InterlinVc.Display");
				}
				break;
		}
#if DEBUG
			//TimeRecorder.End("Display");
#endif
		}

		/// <summary>
		/// Set the paragraph direction to match wsAnalysis and the paragraph alignment to match the overall
		/// direction of the text.
		/// </summary>
		private void SetParaDirectionAndAlignment(IVwEnv vwenv, int wsAnalysis)
		{
			if (m_fRtl)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
									  (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
									  (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
			}
			if (IsWsRtl(wsAnalysis))
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
									  (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
									  (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
			}
		}

		private bool IsWsRtl(int wsAnalysis)
		{
			return GetTssDirForWs(wsAnalysis).GetChars(0, 1) == "\x200F";
		}

		private void DisplaySingleInterlinearAnalysisWithLabels(IVwEnv vwenv, int hvo)
		{
			/*
			// Enhance JohnT: determine what the overall direction of the paragraph should
			// be and set it.
			if (m_mpBundleHeight == 0)
			{
				// First time...figure it out.
				int dmpx, dmpyAnal, dmpyVern;
				vwenv.get_StringWidth(m_tssEmptyAnalysis, null, out dmpx, out dmpyAnal);
				vwenv.get_StringWidth(m_tssEmptyVern, null, out dmpx, out dmpyVern);
				m_mpBundleHeight = dmpyAnal * 4 + dmpyVern * 3;
			}
			*/
			// The interlinear bundle is not editable.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			if (m_fRtl)
			{
				// This must not be on the outer paragraph or we get infinite width.
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
					(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
					(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
			}
			vwenv.OpenParagraph();
			m_fHaveOpenedParagraph = true;
			AddLabelPile(vwenv, m_tsf, m_cache, true, m_fShowMorphBundles);
			try
			{
				// We use this rather than AddObj(hvo) so we can easily identify this object and select
				// it using MakeObjSel.
				int tagMe = MeVirtualHandler.InstallMe(m_cache.VwCacheDaAccessor).Tag;
				vwenv.AddObjProp(tagMe, this, kfragAnalysisMorphs);
				vwenv.CloseParagraph();
			}
			finally
			{
				m_fHaveOpenedParagraph = false;
			}
			/*
			// This puts 3 points of margin on the first FF annotation, if any.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop,
				(int)FwTextPropVar.ktpvMilliPoint, 0); // 3000
			vwenv.AddObjVec(ktagSegFF, this, kfragSegFf);
			*/
		}

		/// <summary>
		/// If the analysis writing system has the opposite directionality to the vernacular
		/// writing system, we need to add a directionality code to the data stream for the
		/// bidirectional algorithm not to jerk the insertion point around at every space.
		/// See LT-7738.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		private ITsString GetTssDirForWs(int ws)
		{
			ITsString tssDirWs;
			if (!m_mapWsDirTss.TryGetValue(ws, out tssDirWs))
			{
				bool fRtlWs = m_cache.GetBoolProperty(ws,
					(int)LgWritingSystem.LgWritingSystemTags.kflidRightToLeft);
				if (fRtlWs)
					tssDirWs = m_tsf.MakeString("\x200F", ws);	// RTL Marker
				else
					tssDirWs = m_tsf.MakeString("\x200E", ws);	// LTR Marker
				m_mapWsDirTss.Add(ws, tssDirWs);
			}
			return tssDirWs;
		}

		/// <summary>
		/// Add any extra material after the main bundles. See override on InterlinTaggingVc.
		/// </summary>
		internal virtual void AddExtraTwficRows(IVwEnv vwenv, int hvo)
		{
		}

		private int m_hvoActiveFreeform;
		private int m_wsActiveFreeform;
		private int m_cpropActiveFreeform;

		private const int kflidComment = (int) CmAnnotation.CmAnnotationTags.kflidComment;

		internal void SetActiveFreeform(int hvo, int ws, int cpropPrevious)
		{
			int hvoOld = m_hvoActiveFreeform;
			m_hvoActiveFreeform = hvo;
			m_wsActiveFreeform = ws;
			// The cpropPrevious we get from the selection may be one off, if a previous line is displaying
			// the prompt for another WS of the same object.
			if (hvoOld == hvo && m_cpropActiveFreeform <= cpropPrevious)
				m_cpropActiveFreeform = cpropPrevious + 1;
			else
				m_cpropActiveFreeform = cpropPrevious;
			// The old one is easy to turn off because we have a NoteDependency on it.
			if (hvoOld != 0)
				m_cache.PropChanged(hvoOld, kflidComment, 0, 0, 0);
			if (m_hvoActiveFreeform != 0)
			{
				int hvoSeg = m_cache.GetVectorItem(hvo, (int) CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0);
				int cann = m_cache.GetVectorSize(hvoSeg, ktagSegFF);
				// In principle, we could try to figure out which one changed, but usually there are only one or two,
				// and the are displayed using DisplayVec, so we have no ability to change one at a time anyway.
				m_cache.PropChanged(hvoSeg, ktagSegFF, 0, cann, cann);
			}
		}

		private void AddFreeformComment(IVwEnv vwenv, int hvo, int ws, int hvoType)
		{
			if (hvoType == NoteSegmentDefn || hvo != m_hvoActiveFreeform || ws != m_wsActiveFreeform)
			{
				vwenv.AddStringAltMember((int) CmAnnotation.CmAnnotationTags.kflidComment, ws, this);
				return;
			}
			ITsString tssVal = vwenv.DataAccess.get_MultiStringAlt(hvo, (int) CmAnnotation.CmAnnotationTags.kflidComment, ws);
			if (tssVal.Length != 0)
			{
				//ITsStrFactory tsf = TsStrFactoryClass.Create();
				// This allows us to put the prompt back if all the text is deleted. But doing that is dubious...will have to
				// do a lot of other work to get the selection restored appropriately.
				// vwenv.NoteStringValDependency(hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, ws, tsf.MakeString("", ws));
				vwenv.AddStringAltMember((int) CmAnnotation.CmAnnotationTags.kflidComment, ws, this);
				return;
			}
			// If anything causes the comment to change, get rid of the prompt.
			vwenv.NoteDependency(new int[] { hvo },
				new int[] { (int)CmAnnotation.CmAnnotationTags.kflidComment }, 1);
			// Passing the ws where we normally pass a tag, but DisplayVariant doesn't need the tag and does need to
			// know which writing system.
			vwenv.AddProp(SimpleRootSite.kTagUserPrompt, this, ws);
		}

		/// <summary>
		/// Check whether we're looking at vernacular data.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="wsSpec"></param>
		/// <returns></returns>
		private bool IsVernWs(int ws, int wsSpec)
		{
			switch (wsSpec)
			{
				case LangProject.kwsVern:
				case LangProject.kwsVerns:
				case LangProject.kwsFirstVern:
				case LangProject.kwsVernInParagraph:
					return true;
				case LangProject.kwsAnal:
				case LangProject.kwsAnals:
				case LangProject.kwsFirstAnal:
				case LangProject.kwsFirstPronunciation:
				case LangProject.kwsAllReversalIndex:
				case LangProject.kwsPronunciation:
				case LangProject.kwsPronunciations:
				case LangProject.kwsReversalIndex:
					return false;
			}
			if (m_cache.LangProject.VernWssRC.Contains(ws))
				return !m_cache.LangProject.AnalysisWssRC.Contains(ws);
			else
				return false;
		}

		bool m_fIsAddingSegmentReference = false;

		internal bool IsAddingSegmentReference
		{
			get { return m_fIsAddingSegmentReference; }
		}

		/// <summary>
		/// Add a segment number appropriate to the current segment being displayed.
		/// (See LT-1236.)
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		private void AddSegmentReference(IVwEnv vwenv, int hvo)
		{
			ITsString tssSegNum;
			StringBuilder sbSegNum = new StringBuilder();
			int flid = 0;
			int hvoStPara = m_cache.GetObjProperty(hvo,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			if (hvoStPara != 0)
			{
				ISilDataAccess sda = vwenv.DataAccess;
				int cseg = sda.get_VecSize(hvoStPara, ktagParaSegments);
				int idxSeg = sda.GetObjIndex(hvoStPara, ktagParaSegments, hvo);
				int hvoStText = m_cache.GetOwnerOfObject(hvoStPara);
				if (hvoStText != 0)
					flid = m_cache.GetOwningFlidOfObject(hvoStText);
				if (flid == (int)ScrSection.ScrSectionTags.kflidContent)
				{
					if (m_scrPara == null || m_scrPara.Hvo != hvoStPara)
						m_scrPara = new ScrTxtPara(m_cache, hvoStPara);
					// With null book name and trimmed it should have just chapter:v{a,b}.
					// The {a,b} above would not be the segment identifiers we add for multiple segments in
					// a verse, but the letters indicating that the verse label is for only part of the verse.
					// There is therefore a pathological case where, say, verse 4a as labeled in the main text
					// gets another letter because 4a has multiple segments 4aa, 4ab, etc.
					string chapRef = AnnotationRefHandler.FullScrRef(m_scrPara, hvo, "").Trim();
					sbSegNum.Append(chapRef + AnnotationRefHandler.VerseSegLabel(m_scrPara, idxSeg, ktagParaSegments));
				}
				else
				{
					int idxPara = m_cache.GetObjIndex(hvoStText, (int)StText.StTextTags.kflidParagraphs, hvoStPara);
					if (idxPara >= 0)
					{
						sbSegNum.AppendFormat("{0}", idxPara + 1);
						if (idxSeg >= 0 && cseg > 1)
							sbSegNum.AppendFormat(".{0}", idxSeg + 1);
					}
				}
			}
			ITsStrBldr tsbSegNum = m_tsf.GetBldr();
			tsbSegNum.ReplaceTsString(0, tsbSegNum.Length, m_cache.MakeUserTss(sbSegNum.ToString()));
			tsbSegNum.SetIntPropValues(0, tsbSegNum.Length, (int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			tssSegNum = tsbSegNum.GetString();
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault, (int)CmObjectUi.RGB(SystemColors.ControlText));
			try
			{
				m_fIsAddingSegmentReference = true;
				vwenv.OpenInnerPile();
				vwenv.AddString(tssSegNum);
				vwenv.CloseInnerPile();
			}
			finally
			{
				m_fIsAddingSegmentReference = false;
			}
		}

		internal int GetRealWs(int hvo, InterlinLineSpec spec)
		{
			int wsPreferred = m_wsCurrentTwfic;
			if (wsPreferred == 0)
				wsPreferred = m_wsVernForDisplay;
			return GetRealWs(m_cache, hvo, spec, wsPreferred);
		}

		static private int GetRealWs(FdoCache cache, int hvo, InterlinLineSpec spec, int wsPreferred)
		{
			int ws = 0;
			switch (spec.WritingSystem)
			{
				case LangProject.kwsVernInParagraph:
					// we want to display the wordform using the twfic ws.
					ws = wsPreferred;
					break;
				default:
					ws = spec.GetActualWs(cache, hvo, wsPreferred);
					// ws = cache.LangProject.ActualWs(spec.WritingSystem, hvo, spec.StringFlid);
					break;
			}
			return ws;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo">WfiMorphBundle</param>
		private void DisplayMorphBundle(IVwEnv vwenv, int hvo)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.OpenInnerPile();
			int first = m_lineChoices.FirstMorphemeIndex;
			int last = m_lineChoices.LastMorphemeIndex;
			int hvoMf = 0;
			if (hvo != 0)
			{
				hvoMf = m_cache.GetObjProperty(hvo,
					(int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph);
			}
			if (vwenv is CollectorEnv && hvoMf != 0)
			{
				// Collectors are given an extra initial chance to 'collect' the morph type, if any.
				vwenv.AddObjProp((int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph,
					this, kfragMorphType);

			}
			for (int i = first; i <= last; i++)
			{
				InterlinLineSpec spec = m_lineChoices[i];
				int ws = 0;
				if (hvo != 0)
				{
					ws = GetRealWs(hvo, spec);
				}
				SetColor(vwenv, LabelRGBFor(spec));
				switch (spec.Flid)
				{
					case InterlinLineChoices.kflidMorphemes:
						if (hvo == 0)
						{
							vwenv.AddString(m_tssMissingMorph);
						}
						else if (hvoMf == 0)
						{
							// If no morph, use the form of the morph bundle (and the entry is of
							// course missing)
							if (ws == 0)
							{
								ws = m_cache.LangProject.ActualWs(spec.WritingSystem, hvo,
									(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm);
							}
							vwenv.AddStringAltMember(
								(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm, ws, this);
						}
						else
						{
							// Got a morph, show it.
							vwenv.AddObjProp((int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph,
								this, kfragMorphFormChoices + i);
							// And the LexEntry line.
						}
						break;
					case InterlinLineChoices.kflidLexEntries:
						if (hvoMf == 0)
						{
							if (hvo != 0)
								vwenv.NoteDependency(new int[] { hvo }, new int[] { (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph }, 1);
							vwenv.AddString(m_tssMissingEntry);
						}
						else
						{
							if (ws == 0)
								ws = spec.WritingSystem;
							LexEntryVc vcEntry = new LexEntryVc(m_cache);
							vcEntry.WritingSystemCode = ws;
							vwenv.AddObj(hvo, vcEntry, LexEntryVc.kfragEntryAndVariant);
						}
						break;
					case InterlinLineChoices.kflidLexGloss:
						int hvoSense = 0;
						if (hvo != 0)
						{
							hvoSense = m_cache.GetObjProperty(hvo,
								(int)WfiMorphBundle.WfiMorphBundleTags.kflidSense);
						}
						if (hvoSense == 0)
						{
							int virtFlid = 0;
							if (hvo != 0)
							{
								vwenv.NoteDependency(new int[] { hvo }, new int[] { (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense }, 1);
								virtFlid = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiMorphBundle", "DefaultSense");
							}
							if (hvo != 0 && ShowDefaultSense && m_cache.GetObjProperty(hvo, virtFlid) > 0)
							{
								// Switch values when using the default sense, rather than the missing row '***'.
								SetGuessing(vwenv, false);
								vwenv.AddObjProp(virtFlid, this, kfragLineChoices + i);
							}
							else
							{
								vwenv.AddProp(ktagBundleMissingSense, this, kfragBundleMissingSense);
							}
						}
						else
						{
							vwenv.AddObjProp((int)WfiMorphBundle.WfiMorphBundleTags.kflidSense,
								this, kfragLineChoices + i);
						}
						break;

					case InterlinLineChoices.kflidLexPos:
						// LexPOS line:
						int hvoMsa = 0;
						if (hvo != 0)
							hvoMsa = m_cache.GetObjProperty(hvo, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa);
						if (hvoMsa == 0)
						{
							if (hvo != 0)
								vwenv.NoteDependency(new int[] { hvo }, new int[] { (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa }, 1);
							vwenv.AddString(m_tssMissingMsa);
						}
						else
						{
							// Use a special view constructor that knows how to display the
							// interlinear view of whatever kind of MSA it is.
							// Enhance JohnT: ideally we would have one of these VCs for each writing system,
							// perhaps stored in the InterlinLineSpec. Currently displaying multiple Wss of LexPos
							// is not useful, though it is possible.
							// Enhancement RickM: we set the m_msaVc.WritingSystemCode to the selected writing system
							//		of each LexPos line in interlinear. This is used extract the LexPos abbreviation
							//		for the specific writing system.
							m_msaVc.WritingSystemCode = spec.WritingSystem;
							vwenv.AddObjProp((int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa,
								m_msaVc, (int)VcFrags.kfragInterlinearAbbr);
						}
						break;
				}
			}
			vwenv.CloseInnerPile();
		}

		/// <summary>
		/// Add the pile of labels used to identify the lines in interlinear text.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="tsf"></param>
		/// <param name="cache"></param>
		/// <param name="wsList">Null if don't want multiple writing systems.</param>
		/// <param name="fShowMutlilingGlosses"></param>
		public void AddLabelPile(IVwEnv vwenv, ITsStrFactory tsf, FdoCache cache,
			bool fWantMultipleSenseGloss, bool fShowMorphemes)
		{
			CheckDisposed();

			int wsUI = cache.DefaultUserWs;
			int wsAnalysis = cache.DefaultAnalWs;
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvForceOn);
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
				(int)FwTextPropVar.ktpvMilliPoint,
				5000); // default spacing is fine for all embedded paragraphs.
			vwenv.OpenInnerPile();
			foreach (InterlinLineSpec spec in m_lineChoices)
			{
				if (!spec.WordLevel)
					break;
				SetColor(vwenv, LabelRGBFor(spec));
				ITsString tss = tsf.MakeString(m_lineChoices.LabelFor(spec.Flid), wsUI);
				if (m_lineChoices.RepetitionsOfFlid(spec.Flid) > 1)
				{
					vwenv.OpenParagraph();
					vwenv.AddString(tss);
					vwenv.AddString(m_cache.MakeUserTss(" "));
					vwenv.AddString(spec.WsLabel(m_cache));
					vwenv.CloseParagraph();
				}
				else
				{
					vwenv.AddString(tss);
				}

			}
			vwenv.CloseInnerPile();
		}


		private void DisplayMorphForm(IVwEnv vwenv, int hvo, int ws)
		{
			// The form of an MoForm. Hvo is some sort of MoMorph. Display includes its prefix
			// and suffix.
			// Todo: make prefix and suffix read-only.
			vwenv.OpenParagraph(); // group prefix, form, suffix on one line.
			// It may not have a morph type at all.
			int typeID = m_cache.GetObjProperty(hvo,
				(int)MoForm.MoFormTags.kflidMorphType);
			if (typeID > 0)
				vwenv.AddObjProp((int)MoForm.MoFormTags.kflidMorphType, this, kfragPrefix);
			vwenv.AddStringAltMember((int)MoForm.MoFormTags.kflidForm,
				ws, this);
			if (typeID > 0)
				vwenv.AddObjProp((int)MoForm.MoFormTags.kflidMorphType, this, kfragPostfix);
			vwenv.CloseParagraph();
		}

		/// <summary>
		/// Implementation of kfragTwficAnalysis as Method Object
		/// </summary>
		class DisplayWordBundleMethod
		{
			int hvoWordform;
			int m_hvoWfiAnalysis = 0;
			int hvoDefault = 0;
			int tagRealForm;
			IVwEnv vwenv;
			int m_hvoTwfic;
			int m_hvoWordBundleAnalysis;
			int m_wsTwfic = 0;
			InterlinVc m_this;
			FdoCache m_cache;
			InterlinLineChoices m_choices;
			public DisplayWordBundleMethod(IVwEnv vwenv1, int hvoWordBundleAnalysis, InterlinVc owner, int tagRF)
			{
				vwenv = vwenv1;
				m_hvoWordBundleAnalysis = hvoWordBundleAnalysis;
				m_this = owner;
				m_cache = m_this.m_cache;
				m_choices = m_this.LineChoices;
				tagRealForm = tagRF;
				m_hvoTwfic = m_this.CurrentTwficHvo;
				m_wsTwfic = m_this.CurrentTwficWs;
			}
			public void Run()
			{
				switch(m_cache.GetClassOfObject(m_hvoWordBundleAnalysis))
				{
				case WfiWordform.kclsidWfiWordform:
					hvoWordform = m_hvoWordBundleAnalysis;
					hvoDefault = m_this.GetDefault(vwenv, hvoWordform);
					break;
				case WfiAnalysis.kclsidWfiAnalysis:
					hvoWordform = m_cache.GetOwnerOfObject(m_hvoWordBundleAnalysis);
					m_hvoWfiAnalysis = m_hvoWordBundleAnalysis;
					hvoDefault = m_this.GetDefault(vwenv, m_hvoWfiAnalysis);
					break;
				case WfiGloss.kclsidWfiGloss:
					m_hvoWfiAnalysis = m_cache.GetOwnerOfObject(m_hvoWordBundleAnalysis);
					hvoWordform = m_cache.GetOwnerOfObject(m_hvoWfiAnalysis);
					hvoDefault = m_hvoWordBundleAnalysis; // complete analysis. no point in searching for a default!
					break;
				default:
					throw new Exception("invalid type used for word analysis");
				}
				for (int i = 0; i < m_choices.Count; )
				{
					m_this.m_icurLine = i;
					InterlinLineSpec spec = m_choices[i];
					if (!spec.WordLevel)
						break;
					if (spec.MorphemeLevel)
					{
						DisplayMorphemes();
						while (i < m_choices.Count && m_choices[i].MorphemeLevel)
							i++;
					}
					else
					{
						int wsActual = m_this.GetRealWs(hvoDefault, spec);
						switch(spec.Flid)
						{
						case InterlinLineChoices.kflidWord:
							DisplayWord(wsActual, i);
							break;
						case InterlinLineChoices.kflidWordGloss:
							DisplayWordGloss(wsActual, i);
							break;
						case InterlinLineChoices.kflidWordPos:
							DisplayWordPOS(wsActual, i);
							break;
						}
						i++;
					}
				}
				m_this.m_icurLine = 0;
			}

			/// <summary>
			/// This says whether or not to display the real underlying wordform.
			/// If so, it returns the hvo for the wordform to display.
			/// </summary>
			/// <param name="cache"></param>
			/// <param name="vwenv"></param>
			/// <param name="tagRealForm"></param>
			/// <param name="ws"></param>
			/// <returns></returns>
			static private bool ShouldDisplayRealForm(FdoCache cache, IVwEnv vwenv, int hvoTwfic, int tagRealForm, int ws, int wsTwfic)
			{
				if (ws == wsTwfic)
				{
					// Base text; may be modified from original text.
					//vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoTwfic, out tag, out index);
					return (cache.MainCacheAccessor.get_IsPropInCache(hvoTwfic, tagRealForm,
						(int)CellarModuleDefns.kcptString, 0));
				}
				return false;
			}

			private ITsString GetRealForm(int ws)
			{
				ITsString tssRealForm = null;
				if (ShouldDisplayRealForm(m_cache, vwenv, m_hvoTwfic, tagRealForm, ws, m_wsTwfic))
					tssRealForm = m_cache.MainCacheAccessor.get_StringProp(m_hvoTwfic, tagRealForm);
				return tssRealForm;
			}

			private void DisplayWord(int ws, int choiceIndex)
			{
				ITsString tssRealForm = GetRealForm(ws);
				if (tssRealForm != null && tssRealForm.Length > 0)
				{
					m_this.IsDoingRealWordForm = true;
					vwenv.AddString(tssRealForm);
					m_this.IsDoingRealWordForm = false;
					return;
				}
				switch (m_cache.GetClassOfObject(hvoDefault))
				{
				case WfiWordform.kclsidWfiWordform:
				case WfiAnalysis.kclsidWfiAnalysis:
				case WfiGloss.kclsidWfiGloss:
					vwenv.AddObj(hvoWordform, m_this, kfragLineChoices + choiceIndex);
					break;
				default:
					throw new Exception("Invalid type found in Twfic analysis");
				}
			}

			private void DisplayMorphemes()
			{
				switch(m_cache.GetClassOfObject(hvoDefault))
				{
				case WfiWordform.kclsidWfiWordform:
				case WfiAnalysis.kclsidWfiAnalysis:
					if (m_this.m_fShowMorphBundles)
					{
						// Display the morpheme bundles.
						if (hvoDefault != m_hvoWordBundleAnalysis)
						{
							// Real analysis isn't what we're displaying, so morph breakdown
							// is a guess. Is it a human-approved guess?
							m_this.SetGuessing(vwenv, m_cache.MainCacheAccessor.get_IntProp(hvoDefault, ktagAnalysisHumanApproved) != 0);
						}
						vwenv.AddObj(hvoDefault, m_this, kfragAnalysisMorphs);
					}
					break;
				case WfiGloss.kclsidWfiGloss:

					if (m_this.m_fShowMorphBundles)
					{
						m_hvoWfiAnalysis = m_cache.GetOwnerOfObject(hvoDefault);
						// Display all the morpheme stuff.
						if (m_hvoWordBundleAnalysis == hvoWordform)
						{
							// Real analysis is just word, one we're displaying is a default
							m_this.SetGuessing(vwenv);
						}
						vwenv.AddObj(m_hvoWfiAnalysis, m_this, kfragAnalysisMorphs);
					}
					break;
				default:
					throw new Exception("Invalid type found in Twfic analysis");
				}
			}

			private void DisplayWordGloss(int ws, int choiceIndex)
			{
				switch(m_cache.GetClassOfObject(hvoDefault))
				{
				case WfiWordform.kclsidWfiWordform:
					m_this.SetColor(vwenv, m_this.LabelRGBFor(choiceIndex)); // looks like missing word gloss.
					vwenv.AddProp(ktagAnalysisMissingGloss, m_this, kfragAnalysisMissingGloss);
					break;
				case WfiAnalysis.kclsidWfiAnalysis:
					int[] rghvoGlosses = m_cache.GetVectorProperty(hvoDefault,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMeanings, true);
					if (rghvoGlosses.Length == 0)
					{
						// There's no gloss, display something indicating it is missing.
						m_this.SetColor(vwenv, m_this.LabelRGBFor(choiceIndex));
						vwenv.AddProp(ktagAnalysisMissingGloss, m_this, kfragAnalysisMissingGloss);
					}
					else
					{
						vwenv.AddObj(rghvoGlosses[0], m_this, kfragLineChoices + choiceIndex);
					}
					break;
				case WfiGloss.kclsidWfiGloss:
					if (m_hvoWordBundleAnalysis == hvoDefault)
					{
						// We're displaying properties of the current object, can do
						// straightforwardly
						m_this.FormatGloss(vwenv, ws);
						vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
							(int)SpellingModes.ksmForceCheck);
						vwenv.AddStringAltMember((int)WfiGloss.WfiGlossTags.kflidForm, ws, m_this);
					}
					else
					{
						m_this.SetGuessing(vwenv);
						vwenv.AddObj(hvoDefault, m_this, kfragLineChoices + choiceIndex);
					}
					break;
				default:
					throw new Exception("Invalid type found in Twfic analysis");
				}
			}

			private void DisplayWordPOS(int ws, int choiceIndex)
			{
				switch(m_cache.GetClassOfObject(hvoDefault))
				{
				case WfiWordform.kclsidWfiWordform:
					m_this.SetColor(vwenv, m_this.LabelRGBFor(choiceIndex)); // looks like missing word POS.
					vwenv.AddProp(ktagAnalysisMissingPos, m_this, kfragAnalysisMissingPos);
					break;
				case WfiAnalysis.kclsidWfiAnalysis:
					if (hvoDefault != m_hvoWordBundleAnalysis)
					{
						// Real analysis isn't what we're displaying, so POS is a guess.
						m_this.SetGuessing(vwenv, m_cache.MainCacheAccessor.get_IntProp(hvoDefault, ktagAnalysisHumanApproved) != 0);
					}
					m_this.AddAnalysisPos(vwenv, hvoDefault, choiceIndex);
					break;
				case WfiGloss.kclsidWfiGloss:
					m_hvoWfiAnalysis = m_cache.GetOwnerOfObject(hvoDefault);
					if (m_hvoWordBundleAnalysis == hvoWordform) // then our analysis is a guess
						m_this.SetGuessing(vwenv);
					vwenv.AddObj(m_hvoWfiAnalysis, m_this, kfragAnalysisCategoryChoices + choiceIndex);
					break;
				default:
					throw new Exception("Invalid type found in Twfic analysis");
				}
			}
		}

		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();

			switch (frag)
			{
			case kfragSegFf: // freeform annotations. (Cf override in InterlinPrintVc)
			{
				// Note that changes here may need to be refleced in FreeformAdder's code
				// for selecting a newly created annotation.
				Dictionary<int, List<int>> dict = OrganizeFfAnnotations(hvo);
				// Add them in the order specified. Each iteration adds a group with the same flid but (typically)
				// different writing systems.
				for (int ispec = m_lineChoices.FirstFreeformIndex;
					ispec < m_lineChoices.Count;
					ispec += m_lineChoices.AdjacentWssAtIndex(ispec).Length)
				{
					int hvoType;
					int flid = m_lineChoices[ispec].Flid;
					hvoType = SegDefnFromFfFlid(flid);
					// if we didn't store this type in our dictionary, skip it.
					if (!dict.ContainsKey(hvoType))
						continue;

					// Add each comment annotation belonging to this type to the display.
					foreach (int hvoAnn in dict[hvoType])
					{
						vwenv.AddObj(hvoAnn, this, kfragSegFfChoices + ispec);
					}
				}
				break;
			}
			default:
				if (frag >= kfragWordGlossWs && frag < kfragWordGlossWs + m_WsList.AnalysisWsIds.Length)
				{
					// Displaying one ws of all the glosses of an analysis, separated by commas.
					vwenv.OpenParagraph();
					int[] rghvo = m_cache.GetVectorProperty(hvo, tag, false);
					for (int i = 0; i < rghvo.Length; i++)
					{
						if (i != 0)
							vwenv.AddString(CommaSpaceString);
						vwenv.AddObj(rghvo[i], this, frag);
					}
					vwenv.CloseParagraph();
				}
				else
				{
					base.DisplayVec (vwenv, hvo, tag, frag);
				}
				break;
			}
		}

		/// <summary>
		/// Organize the Freeform annotations of the specified object into a hash table
		/// keyed by annotation type containing an arraylist of the annotation HVOs
		/// of that type which the input segment possesses.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		internal Dictionary<int, List<int>> OrganizeFfAnnotations(int hvo)
		{
			CheckDisposed();

			Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
			int chvo = m_cache.MainCacheAccessor.get_VecSize(hvo, ktagSegFF);
			// Store each comment annotation in a Dictionary, so we can order them later by type.
			for (int i = 0; i < chvo; i++)
			{
				int hvoAnn = m_cache.MainCacheAccessor.get_VecItem(hvo, ktagSegFF, i);
				int hvoType = m_cache.MainCacheAccessor.get_ObjectProp(hvoAnn,
					(int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);

				// Add the comment annotation to an existing list in the Dictionary, keyed on the type.
				// Otherwise, create a new list for this hvoType and add the annotation to that list.
				if (!dict.ContainsKey(hvoType))
					dict.Add(hvoType, new List<int>());
				dict[hvoType].Add(hvoAnn);
			}
			return dict;
		}

		internal int SegDefnFromFfFlid(int flid)
		{
			CheckDisposed();

			int hvoType;
			switch(flid)
			{
			case InterlinLineChoices.kflidFreeTrans:
				hvoType = FtSegmentDefn;
				break;
			case InterlinLineChoices.kflidLitTrans:
				hvoType = LtSegmentDefn;
				break;
			case InterlinLineChoices.kflidNote:
				hvoType = NoteSegmentDefn;
				break;
			default:
				hvoType = 0;
				break; // unknown type, ignore it.
			}
			return hvoType;
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
		{
			CheckDisposed();

			if (tag == SimpleRootSite.kTagUserPrompt)
			{
				// In this case, frag is the writing system we really want the user to type.
				// We put a zero-width space in that WS at the start of the string since that is the
				// WS the user will end up typing in.
				ITsStrBldr bldr = m_cache.MakeUserTss(ITextStrings.ksEmptyFreeTransPrompt).GetBldr();
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptSpellCheck,
										 (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
				bldr.Replace(0, 0, "\u200B", null);
				// This dummy property should always be set on a user prompt. It allows certain formatting commands to be
				// handled specially.
				bldr.SetIntPropValues(0, bldr.Length, SimpleRootSite.ktptUserPrompt, (int)FwTextPropVar.ktpvDefault, 1);
				bldr.SetIntPropValues(0, 1, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, frag);
				return bldr.GetString();
			}
			switch(frag)
			{
			case kfragAnalysisMissingGloss:
				return m_tssMissingGloss;
			case kfragBundleMissingSense:
				return m_tssMissingSense;
			case kfragAnalysisMissingPos:
				return m_tssMissingAnalysisPos;
			case kfragMissingAnalysis:
				return m_tssMissingAnalysis;
			default:
				return null;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			CheckDisposed();

			if(tag != SimpleRootSite.kTagUserPrompt)
				return tssVal;

			// wait until an IME composition is completed before switching the user prompt to a comment
			// field, otherwise setting the comment will terminate the composition (LT-9929)
			if (m_rootsite.RootBox.IsCompositionInProgress)
				return tssVal;

			if (tssVal.Length == 0)
			{
				// User typed something (return?) which didn't actually put any text over the prompt.
				// No good replacing it because we'll just get the prompt string back and won't be
				// able to make our new selection.
				return tssVal;
			}

			// Get information about current selection
			SelectionHelper helper = SelectionHelper.Create(vwsel, m_rootsite);

			CmAnnotation ann = CmObject.CreateFromDBObject(m_cache, hvo) as CmAnnotation;

			ITsStrBldr bldr = tssVal.GetBldr();
			bldr.SetIntPropValues(0, bldr.Length, SimpleRootSite.ktptUserPrompt, -1, -1);
			bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptSpellCheck, -1, -1);

			// Add the text the user just typed to the comment - this destroys the selection
			// because we replace the user prompt. We use the frag to note the WS of interest.
			ann.Comment.SetAlternative(bldr.GetString(), frag);

			// now restore the selection (in the new property).
			helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, (int)CmAnnotation.CmAnnotationTags.kflidComment);
			helper.SetTextPropId(SelectionHelper.SelLimitType.End, (int)CmAnnotation.CmAnnotationTags.kflidComment);
			helper.NumberOfPreviousProps = m_cpropActiveFreeform;
			helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.End, m_cpropActiveFreeform);
			helper.MakeRangeSelection(m_rootsite.RootBox, true);
			SetActiveFreeform(0, 0, 0);
			return tssVal;
		}
		/// <summary>
		/// Estimate the height of things we display lazily.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns></returns>
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			switch(frag)
			{
					// a paragraph might really be smaller than this, but if it's much BIGGER, which is
					// very possible, we do a LOT of extra work loading and laying out multiple paragraphs
					// that we don't need. Best to make an estimate bigger than a screen, so it always does
					// them one at a time. Conceivably we could improve this by actually using info about
					// the paragraph contents, but do so with care: this gets called for EVERY paragraph in
					// the text, it needs to run fast.
			case kfragInterlinPara:
				return 1200;
			case kfragTxtSection: // well-and-truly likely to fill a window.
				return 2000;
			default:
				if (frag == kfragParaSegment)
				{
					// Can't be a case because it isn't constant.
					// A paragraph segment should at least be smaller than a paragraph. We want to guess on the
					// high side, because the cost of laying out a segment we don't need is much higher than
					// the cost of an extra iteration of generating segments.
					return 400;
				}
				return 500; // large makes for over-long scroll bars but avoids excess layout work.
			}
		}

		/// <summary>
		/// Load data for a group of segments
		/// </summary>
		/// <param name="rghvo"></param>
		/// <param name="hvoPara"></param>
		internal virtual void LoadDataForSegments(int[] rghvo, int hvoPara)
		{
			int wsVern = StTxtPara.GetWsAtParaOffset(m_cache, hvoPara, 0);
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			int ichMin = sda.get_IntProp(rghvo[0], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
			int ichLim = sda.get_IntProp(rghvo[rghvo.Length - 1], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
			string sql = "select wa.id, wmb.id, wmb.Morph, wmb.Sense, wmb.Msa, mff.txt, lsg.txt from CmBaseAnnotation cba"
				+ " join CmAnnotation ca on cba.id = ca.id and cba.BeginObject = " + hvoPara + " and cba.BeginOffset >= " + ichMin + " and cba.BeginOffset <= " + ichLim
				+ " join CmObject wg on wg.id = ca.InstanceOf"
				+ " join WfiAnalysis wa on wg.owner$ = wa.id"
				+ " join WfiMorphBundle_ wmb on wmb.owner$ = wa.id"
				+ " left outer join MoForm_Form mff on mff.obj = wmb.Morph and mff.Ws = " + wsVern
				+ " left outer join LexSense_Gloss lsg on lsg.obj = wmb.Sense and lsg.Ws = " + m_cache.DefaultAnalWs
				+ " group by wa.id, wmb.id, wmb.ownord$, wmb.Morph, wmb.Sense, wmb.Msa, mff.txt, lsg.txt"
				+ " order by wa.id, wmb.ownord$";
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0); // First column contains IDs that are the base of the vector prop
			dcs.Push((int)DbColType.koctObjVec, 1, (int) WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, 0); // Second contains sequences of morph bundles of analyses
			dcs.Push((int)DbColType.koctObj, 2, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph, 0); // Third contains Morph ID of MB.
			dcs.Push((int)DbColType.koctObj, 2, (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense, 0); // Fourth contains Sense ID of MB.
			dcs.Push((int)DbColType.koctObj, 2, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa, 0); // Third contains Msa ID of MB.
			dcs.Push((int)DbColType.koctMltAlt, 3, (int)MoForm.MoFormTags.kflidForm, wsVern); // Fifth contains text of morph form.
			dcs.Push((int)DbColType.koctMltAlt, 4, (int)LexSense.LexSenseTags.kflidGloss, m_cache.DefaultAnalWs); // Sixth contains text of sense gloss.

			m_cache.VwOleDbDaAccessor.Load(sql, dcs, 0, 0, null, false);
		}

		// Load data needed for a particular lazy display.
		// In most cases we allow the cache autoload to do things for us, but when loading the paragraphs of an StText,
		// we must load the segment and word annotations (and create minimal forms if they don't exist), since these
		// are properties computed in non-trivial ways from backreferences, and the cache can't do it automatically.
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
		{
			CheckDisposed();

			try
			{
#if DEBUG
				//TimeRecorder.Begin("LoadParaData");
#endif
				if (tag == this.ktagParaSegments)
				{
					LoadDataForSegments(rghvo, hvoParent);
				}
				if (tag != (int)StText.StTextTags.kflidParagraphs)
					return;
				for (int ihvo = 0; ihvo < chvo; ihvo++)
					LoadParaData(rghvo[ihvo]);
			}
			catch (Exception)
			{
			}
			finally
			{
#if DEBUG
				//TimeRecorder.End("LoadParaData");
#endif
			}
		}

		public void LoadParaData(int hvoPara)
		{
			CheckDisposed();

			if (m_loader == null)
				m_loader = new ParaDataLoader(m_cache);
			m_loader.LoadParaData(hvoPara);
		}

		/// <summary>
		/// Get an AnnotationDefn with the (English) name specified, and cache it in cachedVal...unless it is already cached,
		/// in which case, just return it.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal static int GetAnnDefnId(FdoCache cache, string guid, ref int cachedVal)
		{
			//  and cn.Flid = 7001
			return GetAnnDefnId(cache, new Guid(guid), ref cachedVal);
		}

		internal static int GetAnnDefnId(FdoCache cache, Guid guid, ref int cachedVal)
		{
			if (cachedVal == 0)
			{
				cachedVal = cache.GetIdFromGuid(guid);
			}
			return cachedVal;

		}


		/// <summary>
		/// Get the annotation defn for free translations.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="cachedVal"></param>
		/// <returns></returns>
		internal static int GetFtAnnDefn(FdoCache cache, ref int cachedVal)
		{
			return GetAnnDefnId(cache, LangProject.kguidAnnFreeTranslation, ref cachedVal);
		}

		/// <summary>
		/// Obtain the ID of the AnnotationDefn called (in English) 'Free Translation'.
		/// </summary>
		internal int FtSegmentDefn
		{
			get
			{
				CheckDisposed();
				return GetFtAnnDefn(m_cache, ref m_hvoAnnDefFT);
			}
		}
		/// <summary>
		/// Obtain the ID of the AnnotationDefn called (in English) 'Text Segment'.
		/// </summary>
		internal int TextSegmentDefn
		{
			get
			{
				CheckDisposed();
				return GetAnnDefnId(m_cache, LangProject.kguidAnnTextSegment, ref m_hvoAnnDefTextSeg);
			}
		}
		/// <summary>
		/// Obtain the ID of the AnnotationDefn called (in English) 'Literal Translation'.
		/// </summary>
		internal int LtSegmentDefn
		{
			get
			{
				CheckDisposed();
				return GetAnnDefnId(m_cache, LangProject.kguidAnnLiteralTranslation, ref m_hvoAnnDefLT);
			}
		}
		/// <summary>
		/// Obtain the ID of the AnnotationDefn called (in English) 'Note'.
		/// </summary>
		internal int NoteSegmentDefn
		{
			get
			{
				CheckDisposed();
				return GetAnnDefnId(m_cache, LangProject.kguidAnnNote, ref m_hvoAnnDefNote);
			}
		}

		/// <summary>
		/// Assuming the current object is hvoAnalysis, add a display of its Category property,
		/// the WordPOS line of the interlinear display.
		/// If choiceOffset is -1, display the current analysis writing systems, otherwise,
		/// display the one indicated.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoAnalysis"></param>
		protected void AddAnalysisPos(IVwEnv vwenv, int hvoAnalysis, int choiceIndex)
		{
			int hvoPos = m_cache.GetObjProperty(hvoAnalysis, (int)WfiAnalysis.WfiAnalysisTags.kflidCategory);
			SetColor(vwenv, LabelRGBFor(choiceIndex));
			if (hvoPos == 0)
			{
				vwenv.OpenParagraph();
				vwenv.NoteDependency(new int[] {hvoAnalysis},
					new int[] {(int)WfiAnalysis.WfiAnalysisTags.kflidCategory}, 1);
				vwenv.AddProp(ktagAnalysisMissingPos, this, kfragAnalysisMissingPos);
				vwenv.CloseParagraph();
			}
			else if (choiceIndex < 0)
			{
				vwenv.AddObjProp((int)WfiAnalysis.WfiAnalysisTags.kflidCategory, this, kfragCategory);
			}
			else
			{
				vwenv.AddObjProp((int)WfiAnalysis.WfiAnalysisTags.kflidCategory, this, kfragLineChoices + choiceIndex);
			}
		}

		/// <summary>
		/// Format the gloss line for the specified ws in an interlinear text
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="ws"></param>
		protected virtual void FormatGloss(IVwEnv vwenv, int ws)
		{
			SetColor(vwenv,
				LabelRGBFor(LineChoices.IndexOf(InterlinLineChoices.kflidWordGloss, ws)));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Display the specified object (from an ORC embedded in a string).
		/// Don't display any embedded objects in interlinear text.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// -----------------------------------------------------------------------------------
		public override void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
			return;
		}
	}

	public class ParaDataLoader
	{
		WsListManager m_WsList;
		protected FdoCache m_cache;
		protected IVwCacheDa m_cda;
		Dictionary<int, int> m_analysisApprovalTable;
		Dictionary<int, int> m_guessTable;
		Dictionary<int, int> m_ownerTable;

		// Indexes in the wordforminfo query and segment info query where each kind of info is found.
		static int iwfiTwfic = 0;
		static int iwfiCurrent = 1;
		static int iwfiOwner = 2;
		static int iwfiOwnFlid = 3;
		static int iwfiBeginOffset = 4;
		static int iwfiEndOffset = 5;

		int ktagParaSegments = 0;
		int ktagSegmentForms = 0;
		int ktagTwficDefault = 0;
		int ktagMatchingLowercaseForm = 0;

		public ParaDataLoader(FdoCache cache)
		{
			m_cache = cache;
			ktagParaSegments = StTxtPara.SegmentsFlid(m_cache);
			ktagSegmentForms = InterlinVc.SegmentFormsTag(m_cache);
			ktagTwficDefault = InterlinVc.TwficDefaultTag(m_cache);
			ktagMatchingLowercaseForm = InterlinVc.MatchingLowercaseWordForm(m_cache);
			m_WsList = new WsListManager(m_cache);
			m_cda = m_cache.MainCacheAccessor as IVwCacheDa;
		}

		private void CacheOwningInfo(int hvo, int hvoOwner, int flid)
		{
			CacheObjProp(hvo, (int)CmObjectFields.kflidCmObject_Owner, hvoOwner);
			CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid, flid);
		}

		// Retrieve an integer from a dictionary, or -1 if not present.
		int ValOrMinusOne(Dictionary<int, int> dict, int key)
		{
			int result = 0;
			if (!dict.TryGetValue(key, out result))
				result = -1;
			return result;
		}

		public void LoadParaData(int hvoPara)
		{
			if (m_cache.MainCacheAccessor.get_VecSize(hvoPara, ktagParaSegments) == 0)
				return;
			LoadAnalysisData(hvoPara);
			StTxtPara.LoadSegmentFreeformAnnotationData(m_cache, hvoPara, new Set<int>(m_WsList.AnalysisWsIds));
			if (ScrTxtPara.IsScripturePara(hvoPara, m_cache) && ParaHasNoSegmentBt(hvoPara))
			{
				ConvertOldBtToNew(hvoPara);
			}
		}

		/// <summary>
		/// Convert the old (paragraph-level) back translation of a Scripture paragraph to a segment-level back (free) translation.
		/// </summary>
		/// <param name="hvoPara"></param>
		private void ConvertOldBtToNew(int hvoPara)
		{
			IStTxtPara para = CmObject.CreateFromDBObject(m_cache, hvoPara, false) as StTxtPara;
			new BtConverter(para).ConvertCmTransToInterlin(m_cache.DefaultAnalWs);
		}

		/// <summary>
		/// Answer true if the paragraph has no segments with non-empty free translations (in the default analysis WS).
		/// Assumes StTxtPara.LoadSegmentFreeformAnnotationData has been called to set up the SegmentsFlid and
		/// SegmentFreeformAnnotationsFlid properties.
		/// </summary>
		/// <param name="hvoPara"></param>
		/// <returns></returns>
		private bool ParaHasNoSegmentBt(int hvoPara)
		{
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int ktagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(m_cache);
			int ftSegmentDefn = m_cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);

			ISilDataAccess sda = m_cache.MainCacheAccessor;
			int cseg = sda.get_VecSize(hvoPara, kflidSegments);
			for (int iseg = 0; iseg < cseg; iseg++)
			{
				int hvoSeg = sda.get_VecItem(hvoPara, kflidSegments, iseg);
				int cff = sda.get_VecSize(hvoSeg, ktagSegFF);
				for (int iff = 0; iff < cff; iff++)
				{
					int hvoFf = sda.get_VecItem(hvoSeg, ktagSegFF, iff);
					int hvoType = sda.get_ObjectProp(hvoFf, (int) CmAnnotation.CmAnnotationTags.kflidAnnotationType);
					if (hvoType != ftSegmentDefn)
						continue;
					if (sda.get_MultiStringAlt(hvoFf, (int)CmAnnotation.CmAnnotationTags.kflidComment, m_cache.DefaultAnalWs).Length != 0)
						return false; // found a non-empty free translation.
				}
			}
			return true;
		}

		internal void LoadAnalysisData(int hvoPara)
		{
			if (m_analysisApprovalTable == null)
				GetAnalysisTables();

			// Now get the actual annotations for our paragraph.
			string sQuery3 = string.Format("select cba.id, instanceOf, co.owner$, co.ownflid$, BeginOffset, EndOffset from CmBaseAnnotation_ cba "
				+ "left outer join CmObject co on co.id = cba.InstanceOf "
				+ "where BeginObject = {0} and cba.AnnotationType in ({1}, {2})"
				+ " order by BeginOffset", hvoPara, CmAnnotationDefn.Twfic(m_cache).Hvo,
				CmAnnotationDefn.Punctuation(m_cache).Hvo);

			foreach (int[] wfinfo in DbOps.ReadIntArray(m_cache, sQuery3, null, 6))
			{
				int hvoAnnotation = wfinfo[iwfiTwfic]; // id of the CmAnnotation (twfic or punct)
				NoteCurrentAnnotation(hvoAnnotation);
				int hvoCurrent = wfinfo[iwfiCurrent]; // id of the wordform, analysis, or gloss it points to
				int hvoOwner = wfinfo[iwfiOwner]; // id of the owner of hvoCurrent
				int flid = wfinfo[iwfiOwnFlid]; // flid in which hvoOwner owns hvoCurrent
				int hvoAnalysis = 0;
				int fHumanApproved = 0; // 1 if true.
				if (flid == (int)WfiAnalysis.WfiAnalysisTags.kflidMeanings
					|| flid == (int)WfiWordform.WfiWordformTags.kflidAnalyses)
				{
					// Cache ownership of the gloss or analysis.

					if (flid == (int)WfiAnalysis.WfiAnalysisTags.kflidMeanings)
					{
						hvoAnalysis = hvoOwner;
						// Cache two levels of ownership.
						int hvoWordform = 0;
						if (!m_ownerTable.TryGetValue(hvoOwner, out hvoWordform))
						{
							// Somehow not loaded into owner table...this can happen on Undo/Redo.
							hvoWordform = m_cache.GetOwnerOfObject(hvoOwner); // get less efficiently.
							m_ownerTable[hvoOwner] = hvoWordform; // fix it up
						}
						CacheOwningInfo(hvoOwner, hvoWordform, (int)WfiWordform.WfiWordformTags.kflidAnalyses);
						CacheOwningInfo(hvoCurrent, hvoOwner, flid);
						fHumanApproved = 1; // gloss is always approved.
					}
					else
					{
						if (flid == (int)WfiWordform.WfiWordformTags.kflidAnalyses)
						{
							CacheOwningInfo(hvoCurrent, hvoOwner, flid);
							hvoAnalysis = hvoCurrent;
							fHumanApproved = 1; // analysis actually recorded on annotation is approved.
						}
						// current value is analysis or wordform, may have a useful guess.
						fHumanApproved = RecordGuessIfAvailable(hvoAnnotation, hvoCurrent, fHumanApproved);
					}
				}
				if (hvoAnalysis != 0)
				{
					// If we have an analysis, record whether the human approved it.
					CacheIntProp(hvoAnalysis, InterlinVc.ktagAnalysisHumanApproved, fHumanApproved);
				}
				if (hvoCurrent == 0)
				{
					// punctuation annotation: need offsets.
					CacheIntProp(hvoAnnotation, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset,
								 wfinfo[iwfiBeginOffset]);
					CacheIntProp(hvoAnnotation, (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset,
								 wfinfo[iwfiEndOffset]);
				}
			}
			// In addition to real annotations, there may be fake ones, which might however point at real wordforms
			// that can have guesses.
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			int cseg = sda.get_VecSize(hvoPara, ktagParaSegments);
			for (int iseg = 0; iseg < cseg; iseg++)
			{
				int hvoSeg = sda.get_VecItem(hvoPara, ktagParaSegments, iseg);
				int cann = sda.get_VecSize(hvoSeg, ktagSegmentForms);
				for (int iann = 0; iann < cann; iann++)
				{
					int hvoAnn = sda.get_VecItem(hvoSeg, ktagSegmentForms, iann);
					NoteCurrentAnnotation(hvoAnn);
					int hvoCurrent = sda.get_ObjectProp(hvoAnn, InterlinDocChild.TagAnalysis);
					RecordGuessIfAvailable(hvoAnn, hvoCurrent, 0);
				}
			}
		}

		protected virtual void CacheIntProp(int hvo, int flid, int value)
		{
			m_cda.CacheIntProp(hvo, flid, value);
		}

		protected virtual void NoteCurrentAnnotation(int hvoAnnotation)
		{
			// override if needed
		}

		private int RecordGuessIfAvailable(int hvoAnnotation, int hvoCurrent, int fHumanApproved)
		{
			int hvoGuess = -1;
			if (m_cache.MainCacheAccessor.get_IsPropInCache(hvoAnnotation, ktagMatchingLowercaseForm, (int)CellarModuleDefns.kcptReferenceAtom, 0))
			{
				int hvoWfiLower = m_cache.GetObjProperty(hvoAnnotation, ktagMatchingLowercaseForm);
				if (hvoWfiLower != -1)
					hvoGuess = ValOrMinusOne(m_guessTable, hvoWfiLower);
			}
			if (hvoGuess == -1)
			{
				hvoGuess = ValOrMinusOne(m_guessTable, hvoCurrent);
			}
			if (hvoGuess != -1)
			{
				// We have a guess for this wordform or analysis. We certainly know one level of owner
				int hvoOwner1 = m_ownerTable[hvoGuess];
				// and if it's a gloss we know two:
				int hvoOwner2 = ValOrMinusOne(m_ownerTable, hvoOwner1);
				if (hvoOwner2 != -1)
				{
					// gloss
					CacheOwningInfo(hvoGuess, hvoOwner1, (int)WfiAnalysis.WfiAnalysisTags.kflidMeanings);
					CacheOwningInfo(hvoOwner1, hvoOwner2, (int)WfiWordform.WfiWordformTags.kflidAnalyses);
					fHumanApproved = 1; // glosses are always produced by humans.
				}
				else
				{
					// analysis
					CacheOwningInfo(hvoGuess, hvoOwner1, (int)WfiWordform.WfiWordformTags.kflidAnalyses);
					fHumanApproved = ValOrMinusOne(m_analysisApprovalTable, hvoOwner1);
					if (fHumanApproved == -1)
						fHumanApproved = 0; // treat unknown as not approved for this purpose.
				}
				// Record the guess itself
				CacheObjProp(hvoAnnotation, ktagTwficDefault, hvoGuess);
			}
			return fHumanApproved;
		}

		protected virtual void CacheObjProp(int hvo, int flid, int obj)
		{
			m_cda.CacheObjProp(hvo, flid, obj);
		}


		private void GetAnalysisTables()
		{
#if DEBUG
			//TimeRecorder.Begin("Get Twfics");
#endif
			// Get human approval status for all known analyses that have a human evaluation
			string sQuery1 = "select wa.id, ae.Accepted from WfiAnalysis wa "
				+ "left outer join CmAgentEvaluation_ ae on ae.target = wa.id "
				+ "left outer join CmAgent_ ag on ag.id = ae.owner$ and ag.Human = 1";
			m_analysisApprovalTable = new Dictionary<int, int>();
			foreach(int[] analysisApprovalInfo in DbOps.ReadIntArray(m_cache, sQuery1, null, 2))
			{
				m_analysisApprovalTable[analysisApprovalInfo[0]] = analysisApprovalInfo[1];
			}

			// Get all known WfiAnalysis, and WfiGloss
			string sQuery2 = string.Format("select wa.id, wa.owner$, wa.class$ from CmObject wa "
				+ "left outer join CmBaseAnnotation_ cb on cb.InstanceOf = wa.id and cb.AnnotationType in ({0},{1}) "
				+ "where wa.class$ in (5059, 5060) " // WfiAnalysis, WfiGloss
				+ "group by wa.id, wa.owner$, wa.class$ "
				+ "order by wa.class$, count(cb.InstanceOf)",
				CmAnnotationDefn.Twfic(m_cache).Hvo, CmAnnotationDefn.Punctuation(m_cache).Hvo);

			List<int[]> analysisGlossRows = DbOps.ReadIntArray(m_cache, sQuery2, null, 3);
			m_guessTable = new Dictionary<int, int>(analysisGlossRows.Count); // hvoWordform/Analysis to hvoBestGuess
			m_ownerTable = new Dictionary<int, int>(); // hvoAnalysis/Guess to owner
			int i = 0;
			for (;i < analysisGlossRows.Count;i++)
			{
				int[] analysisInfo = analysisGlossRows[i];
				int classId = analysisInfo[2];
				if (classId != 5059)
					break; // got to WfiGlosses

				int hvoAnalysis = analysisInfo[0];
				int hvoWordform = analysisInfo[1];

				// Unless we have a recorded human disapproval of the analysis, note it as a possible guess.
				// It may be superseded by a later analysis that is used more often, or (later still) by a
				// WfiGloss for this wordform.
				if (ValOrMinusOne(m_analysisApprovalTable, hvoAnalysis) != 0) // no opinion or approved
					m_guessTable[hvoWordform] = hvoAnalysis; // record as possible guess;
				m_ownerTable[hvoAnalysis] = hvoWordform;
			}
			for (;i < analysisGlossRows.Count; i++)
			{
				int[] analysisInfo = (int[])analysisGlossRows[i];
				int hvoAnalysis = analysisInfo[1];
				int hvoGloss = analysisInfo[0];
				int hvoWordform = m_ownerTable[hvoAnalysis];
				m_guessTable[hvoWordform] = hvoGloss;
				m_guessTable[hvoAnalysis] = hvoGloss;
				m_ownerTable[hvoGloss] = hvoAnalysis;
			}
		}
	}

	/// <summary>
	/// Updates the paragraphs interlinear data and collects which annotations
	/// have been affected so we can update the display appropriately.
	/// </summary>
	internal class ParaDataUpdateTracker : ParaDataLoader
	{
		private Set<int> m_annotationsChanged = new Set<int>();
		private int m_hvoCurrentAnnotation = 0;
		internal ParaDataUpdateTracker(FdoCache cache)
			: base(cache)
		{

		}

		protected override void NoteCurrentAnnotation(int hvoAnnotation)
		{
			m_hvoCurrentAnnotation = hvoAnnotation;
			base.NoteCurrentAnnotation(hvoAnnotation);
		}

		private void MarkCurrentAnnotationAsChanged()
		{
			// something has changed in the cache for the annotation or its analysis,
			// so mark it as changed.
			m_annotationsChanged.Add(m_hvoCurrentAnnotation);
		}

		/// <summary>
		/// the annotations that have changed, or their analysis, in the cache
		/// and for which we need to do propchanges to update the display
		/// </summary>
		internal IList<int> ChangedAnnotations
		{
			get { return m_annotationsChanged.ToArray(); }
		}

		protected override void CacheIntProp(int hvo, int flid, int newValue)
		{
			bool fValueNeedsUpdate = TestValueNeedsToBeCached(hvo, flid, newValue);
			if (fValueNeedsUpdate)
			{
				base.CacheIntProp(hvo, flid, newValue);
				MarkCurrentAnnotationAsChanged();
			}
		}

		private bool TestValueNeedsToBeCached(int hvo, int flid, int newValue)
		{
			bool fValueNeedsUpdate = false;
			FieldType flidType = m_cache.GetFieldType(flid);
			if (flidType == FieldType.kcptNil)
			{
				// treat as a dummy int prop
				int oldValue = m_cache.MainCacheAccessor.get_IntProp(hvo, flid);
				if (oldValue != newValue)
					fValueNeedsUpdate = true;
			}
			else if (m_cache.MainCacheAccessor.get_IsPropInCache(hvo, flid, (int)flidType, 0))
			{
				int oldValue;
				switch (flidType)
				{
					case FieldType.kcptInteger:
						oldValue = m_cache.GetIntProperty(hvo, flid);
						break;
					case FieldType.kcptOwningAtom:
					case FieldType.kcptReferenceAtom:
						oldValue = m_cache.GetObjProperty(hvo, flid);
						break;
					default:
						throw new ArgumentException(String.Format("FlidType {0} not yet supported here.",
							flidType.ToString()));
				}
				if (oldValue != newValue)
					fValueNeedsUpdate = true;
			}
			else
			{
				// not yet in the cache.
				fValueNeedsUpdate = true;
			}
			return fValueNeedsUpdate;
		}

		protected override void CacheObjProp(int hvo, int flid, int obj)
		{
			bool fValueNeedsUpdate = TestValueNeedsToBeCached(hvo, flid, obj);
			if (fValueNeedsUpdate)
			{
				base.CacheObjProp(hvo, flid, obj);
				MarkCurrentAnnotationAsChanged();
			}
		}

	}

}
