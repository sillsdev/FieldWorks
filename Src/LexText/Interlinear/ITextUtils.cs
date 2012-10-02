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
// File: ITextUtils.cs
// Responsibility: John Thomson
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
#define PROFILING
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar; // For StTxtPara etc.
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FdoUi;
using XCore;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// An interface common to classes that 'handle' combo boxes that appear when something in
	/// IText is clicked.
	/// </summary>
	public interface IComboHandler
	{
		/// <summary>
		/// Initialize the combo contents.
		/// </summary>
		void SetupCombo();
		/// <summary>
		/// Get rid of the combo, typically when the user clicks outside it.
		/// </summary>
		void Hide();
		/// <summary>
		/// Handle a return key press in an editable combo.
		/// </summary>
		/// <returns></returns>
		bool HandleReturnKey();
		/// <summary>
		/// Activate the combo-handler's control.
		/// If the control is a combo make it visible at the indicated location.
		/// If it is a ComboListBox pop it up at the relevant place for the indicated location.
		/// </summary>
		/// <param name="loc"></param>
		void Activate(SIL.FieldWorks.Common.Utils.Rect loc);

		/// <summary>
		/// This one is a bit awkward in this interface, but it simplifies things. It's OK to
		/// just answer zero if the handler has no particular morpheme selected.
		/// </summary>
		int SelectedMorphHvo { get; }

		/// <summary>
		/// Act as if the user selected the current item.
		/// </summary>
		void HandleSelectIfActive();

	}

	/// <summary>
	/// Property for loading words into the concordance.
	/// </summary>
	public class ConcordanceWordsVirtualHandler : FDOSequencePropertyVirtualHandler
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public ConcordanceWordsVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration, cache)
		{
		}

		private List<int> UnionOfLists(List<int> a, List<int> b)
		{
			Dictionary<int, bool> uniqueIds = new Dictionary<int, bool>(a.Count + b.Count);

			// add the a-keys
			for (int i = 0; i < a.Count; ++i)
			{
				uniqueIds[a[i]] = true;
			}

			for (int i = 0; i < b.Count; ++i)
			{
				uniqueIds[b[i]] = true;
			}
			List<int> fullList = new List<int>(uniqueIds.Keys);
			return fullList;
		}

		/// <summary>
		/// Load all wordforms from ConcordanceTexts into cache.
		/// </summary>
		/// <param name="hvoWfi"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvoWfi, int ktagConcordanceWordforms, int ws, IVwCacheDa cda)
		{
			Debug.Assert(hvoWfi == m_cache.LangProject.WordformInventoryOAHvo);
			List<int> concTexts = new List<int>(m_cache.GetVectorProperty(m_cache.LangProject.Hvo, LangProject.InterlinearTextsFlid(Cache), false));
			// initialize our ConcordanceWords property.
			cda.CacheVecProp(hvoWfi, ktagConcordanceWordforms, new int[0], 0);
			// load all the text structure annotations.
			ParagraphParser.ConcordTexts(m_cache, concTexts.ToArray(), Progress);
			Set<int> wordformsFromSession = new Set<int>(ParagraphParser.WordformsFromLastParseSession(m_cache).ToArray());
			// get all real wordforms and add the ones that have no occurrences in a text.
			Set<int> wfiWordforms = new Set<int>(m_cache.LangProject.WordformInventoryOA.WordformsOC.HvoArray);
			Set<int> fullConcordanceWordList = wordformsFromSession.Union(wfiWordforms); //UnionOfLists(wordformsFromSession, wfiWordforms);
			Debug.Assert(m_cache.MainCacheAccessor.get_VecSize(hvoWfi, ktagConcordanceWordforms) == 0,
				"The parse session should not have already added to our ConconcordanceWordforms");
			// reload into our vector property
			cda.CacheVecProp(hvoWfi, ktagConcordanceWordforms, fullConcordanceWordList.ToArray(), fullConcordanceWordList.Count);
		}

		/// <summary>
		/// Answer true if the result of the virtual property depends on the value. We answer true at least for the
		/// wordform inventory so things get cleaned up properly when manually deleting a wordform. Technically it could also change
		/// when adding texts or modifying them, but that is less crucial, and we have a more sophisticated test for that.
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="hvoChange"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public override bool DoesResultDependOnProp(int hvoObj, int hvoChange, int tag, int ws)
		{
			if (tag == (int)WordformInventory.WordformInventoryTags.kflidWordforms)
				return true;
			return base.DoesResultDependOnProp(hvoObj, hvoChange, tag, ws);
		}
	}

	/// <summary>
	/// Property for occurrences of a WfiWordform.
	/// </summary>
	public class OccurrencesInTextsVirtualHandler : FDOSequencePropertyVirtualHandler
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public OccurrencesInTextsVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration, cache)
		{
		}

		/// <summary>
		/// Loads occurrences of this word into the cache.
		/// </summary>
		/// <param name="hvoWfi"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvoWfiWordform, int ktagOccurrences, int ws, IVwCacheDa cda)
		{
			// JohnT: this test is quite expensive and distorts optimization efforts.
			//Debug.Assert(m_cache.IsValidObject(hvoWfiWordform), "looks like our object (" + hvoWfiWordform + ") got vaporized.");
			// just to be safe initialize the property if it doesn't exist yet.
			if (!m_cache.MainCacheAccessor.get_IsPropInCache(hvoWfiWordform, ktagOccurrences, this.Type, ws))
				cda.CacheVecProp(hvoWfiWordform, ktagOccurrences, new int[0], 0);
		}
	}

	/// <summary>
	/// This class provides a relatively extensible way to pass options to the ParagraphParser.
	/// </summary>
	public class ParagraphParserOptions
	{
		/// <summary>
		///  Make one with all the default settings.
		/// </summary>
		public ParagraphParserOptions()
		{
		}

		/// <summary>
		/// Make one, controlling the indicated options and taking defaults for others.
		/// </summary>
		public ParagraphParserOptions(bool fBuildConcordance, bool fResetConcordance, bool fCreateRealWordforms, bool fUseRealData)
		{
			CollectWordformOccurrencesInTexts = fBuildConcordance;
			ResetConcordance = fResetConcordance;
			CreateRealWordforms = fCreateRealWordforms;
			UseRealData = fUseRealData;
		}
		/// <summary>
		/// if true, collect wordform occurrences
		/// </summary>
		public bool CollectWordformOccurrencesInTexts;
		/// <summary>
		/// if true, reset the wordform inventory concordance before starting.
		/// </summary>
		public bool ResetConcordance;
		/// <summary>
		/// True if any wordforms we need to create should be real; false if dummies are OK.
		/// </summary>
		public bool CreateRealWordforms = true;
		/// <summary>
		/// JohnT: this didn't have a comment; I think this is right but am not completely sure.
		/// True to use any existing database annotations, wordforms, etc; false to work entirely with dummies (for testing?)
		/// </summary>
		public bool UseRealData = true;
		/// <summary>
		/// True to suppress Undo tasks for the objects created during parsing (normal when parsing takes
		/// place as a preparation for displaying a view, not when it is a side effect of a real data change).
		/// </summary>
		public bool SuppressSubTasks = true;
		/// <summary>
		/// True to create real segments (roughly sentences), false to create dummy ones.
		/// </summary>
		public bool CreateRealSegments;
	}

	public class ParagraphParser : IFWDisposable
	{
		int[] m_hvosStText = null; // set if processing multiple texts;
		int[] m_hvosStTxtPara = null; // set if processing multiple paragraphs.
		int m_hvoText = 0; // set if processing whole text.
		int m_ihvoPara = -1; // set if processing whole text.
		int m_cparas = -1; // set if processing whole text.
		protected IStTxtPara m_para;
		private ITsString m_tssPara;
		private WordMaker m_wordMaker;
		protected FdoCache m_cache;
		private IMatcher m_matcher;
		private ConcordanceControl.ConcordanceLines m_line;
		private List<int> m_matchingAnnotations = null;
		ILangProject m_lp;
		IWordformInventory m_wfi;
		int m_paraWs = 0;
		// keeps track of the paragraph ids we've parsed.
		Set<int> m_paraIdsParsed = new Set<int>();
		// If not null, list of existing annotation objects not yet reused.
		// When one is reused, it is changed to zero.
		int[] m_annotations = new int[0];
		Dictionary<int, List<int>> m_realParagraphTwficAnnotations = new Dictionary<int, List<int>>();
		Dictionary<int, List<int>> m_realParagraphWordforms = new Dictionary<int, List<int>>();
		Dictionary<int, List<int>> m_realParagraphPunctuationAnnotations = new Dictionary<int, List<int>>();
		Dictionary<int, List<int>> m_realParagraphSegmentAnnotations = new Dictionary<int, List<int>>();
		static Dictionary<string, Set<ITsString>> s_realPhrases = new Dictionary<string, Set<ITsString>>();
		static FdoCache s_cacheForRealPhrases = null;
		bool m_fSegmentFormCollectionMode = false;  // used to collect (and restore state of ParagraphParser).
		protected Set<int> m_unusedSegmentAnnotations = new Set<int>();
		protected Set<int> m_unusedTwficAnnotations = new Set<int>();
		protected Set<int> m_unusedPunctuationAnnotations = new Set<int>();
		Set<int> m_orphanedAnnotations = new Set<int>();
		List<int> m_paraRealTwfics = null;
		int[] m_paraRealWfIds = new int[0];
		int[] m_paraRealAnnIds = new int[0];
		WordMaker m_paragraphTextScanner = null;
		Dictionary<string, List<int>> m_wordformAnnotationPossibilities = new Dictionary<string, List<int>>();

		// If not null, list of corresponding wordform objects.
		int[] m_wordforms = new int[0];
		// Punctuation annotations already in use on the segment; changed to zero as reused.
		int[] m_punctAnnotations = new int[0];
		//int m_iPunctAnnotation = 0; // index of next available punct annotation to reuse.
		//int m_iseg = 0; // Number of segments reused so far.
		int m_hvoAnnDefTextSeg; // Annotation type value for text segment annotations.
		int m_hvoAnnDefnTwfic; // Annotation type value for Twfic annotations.
		int m_tagPunct; // ID of virtual property used for form of punctuation annotation.
		ICmAnnotationDefn m_cadPunct; // punctuation annotations.
		ICmAnnotationDefn m_cadTwfic; // Used for Twfic annotations.
		bool m_fMadeDummyAnnotations = false; // true if we had to create dummy annotations to parse the text.
		private bool m_fSuppressSubTasks = true;
		private bool m_fCreateRealSegments;
		// Variables used for profiling
#if PROFILING
		int m_cDummyAnnotations = 0;
		int m_cWficsMade = 0;
		int m_cPficsMade = 0;
		int m_cSegmentsMade = 0;
		long m_cTicksMakingDummies = 0;
		long m_cTicksResettingDummies = 0;
		static int s_cTotalDummiesMade = 0;
		int m_cTotalDummiesReset = 0;
#endif
		bool m_fRebuildingConcordanceWordforms = false;  // true when we're rebuilding ConcordanceWordforms
		bool m_fAddOccurrencesToWordforms = false;  // true when recording the occurrences of a wordform in text(s).
		bool m_fCreateDummyWordforms = false; // true when we want to add dummy wordforms instead of real ones.
		bool m_fFinishedBuildAnalysisList = false; // try to run BuildAnalysisList only once per parse session.
		// Virtual fields initialized in Init(cache);
		int kflidParaSegments;
		int kflidSegmentForms;
		int kflidConcordanceWordforms;
		int kflidOccurrences;
		int m_tagRealForm;
		int m_tagLowercaseForm;
		List<int> m_dummyAnnotationsToReuse = new List<int>();
		int m_iNextDummyAnnotationToReuse;

		// This Set is used to keep track of the wordformIds we've identified through a parse.
		// On 12/2/2006, when I (RandyR) switched it to use a Set,
		// the values (also ints) weren't being used at all.
		static Set<int> s_wordformIdOccurrencesTable = new Set<int>();
		static FdoCache s_cache;

		FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
		}

		/// <summary>
		/// There are certain static tables built during a parse session that may overlap data found from
		/// subsequent parse sessions. Reset this information here.
		/// </summary>
		private static void ResetParseSessionDependentStaticData()
		{
			// Clear out the table that keeps track of our wordform instances during a parse session.
			s_cache = null;
			s_wordformIdOccurrencesTable.Clear();
		}

		/// <summary>
		/// Retrieve the wordforms collected during the last parsing session.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static Set<int> WordformsFromLastParseSession(FdoCache cache)
		{
			Set<int> parsedWordforms = null;
			using (ParagraphParser pp = new ParagraphParser(cache))
			{
				parsedWordforms = new Set<int>(pp.WordformIdOccurrencesTable);
			}
			return parsedWordforms;
		}

		/// <summary>
		/// Table collects the occurences of a wordform found during a parse.
		/// </summary>
		internal Set<int> WordformIdOccurrencesTable
		{
			get
			{
				CheckDisposed();

				if (s_cache == null || s_cache != m_cache)
					return new Set<int>();
				return s_wordformIdOccurrencesTable;
			}
		}

		/// <summary>
		/// Enables a mode that creates/uses dummy wordforms instead of real ones.
		/// </summary>
		protected bool CreateDummyWordforms
		{
			get { return m_fCreateDummyWordforms || SegmentFormCollectionMode; }
			set { m_fCreateDummyWordforms = value; }
		}

		/// <summary>
		/// Indicates that we're in the process of rebuilding the concordance
		/// from scratch, both the wordforms and their occurrences in the given texts.
		/// </summary>
		private bool RebuildingConcordanceWordforms
		{
			get { return m_fRebuildingConcordanceWordforms; }
			set
			{
				m_fRebuildingConcordanceWordforms = value;
				(m_wfi as WordformInventory).SuspendUpdatingConcordanceWordforms = value;
				CollectWordformOccurrencesInTexts = value;
				CreateDummyWordforms = value;
			}

		}

		/// <summary>
		/// if true during parsing we'll add occurrences to wordforms.
		/// </summary>
		private bool CollectWordformOccurrencesInTexts
		{
			get { return m_fAddOccurrencesToWordforms; }
			set
			{
				m_fAddOccurrencesToWordforms = value;
			}
		}

		private static int[] appendIntArray(int[] first, int[] second)
		{
			if (first.Length == 0)
				return second;
			if (second.Length == 0)
				return first;
			List<int> result = new List<int>(first);
			result.AddRange(second);
			return result.ToArray();
		}

		/// <summary>
		/// Parse through all the given texts, even if they've been fully analyzed.
		/// Collect occurrences of words and cache all paragraph and wordform related virtual properties.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvosStText">list of hvos for StText objects</param>
		public static void ConcordTexts(FdoCache cache, int[] hvosStText, ProgressState progress)
		{
			if (progress == null)
				progress = new NullProgressState();
			using (ParagraphParser pp = new ParagraphParser(cache))
			{
#if PROFILING
				long ticks = DateTime.Now.Ticks;
#endif
				// Ensure all info about paragraphs of texts and contents of paragraphs is in cache and current.
				// Enhance JohnT: possibly performance would be helped, especially in cases where we have a lot
				// of archived Scripture versions, by restricting this to just the texts in hvosStText.
				cache.LoadAllOfAnOwningVectorProp((int)StText.StTextTags.kflidParagraphs, "StText");
				cache.LoadAllOfAStringProp((int)StTxtPara.StTxtParaTags.kflidContents);
#if PROFILING
				Debug.WriteLine("Time to end of loading text data = " + (DateTime.Now.Ticks - ticks));
#endif
				pp.m_hvosStText = hvosStText;
				//// Get a list of all the paragraphs.
				//List<int> targetParagraphs = new List<int>();
				//foreach (IStText text in new FdoObjectSet<IStText>(cache, pp.m_hvosStText, true))
				//{
				//    targetParagraphs.AddRange(text.ParagraphsOS.HvoArray);
				//}

				pp.RebuildingConcordanceWordforms = true;
				WordformInventory wfi =	(cache.LangProject.WordformInventoryOA as WordformInventory);
				wfi.ResetConcordanceWordformsAndOccurrences();

#if PROFILING
				Debug.WriteLine("Time to end of reset occurrenes = " + (DateTime.Now.Ticks - ticks));
#endif

				ParagraphParser.ResetParseSessionDependentStaticData();

				// Estimate the number of total number of milestones we'll set.
				// Enhance: we could construct a way to set percentage done based upon
				// number of texts and paragraphs in each text.
				if (progress is MilestoneProgressState)
				{
					MilestoneProgressState mp = progress as SIL.FieldWorks.Common.Controls.MilestoneProgressState;
					for (int i = 0; i < pp.m_hvosStText.Length; ++i)
					{
						AddParseTextMilestones(mp);
					}
				}

				// Parse each text to load our paragraph and wordform segment annotations.
				using (SuppressSubTasks suppressor = new SuppressSubTasks(cache, true))
				{
					List<IStText> texts = new List<IStText>(new FdoObjectSet<IStText>(cache, pp.m_hvosStText, false));
					// Anything like this is currently redundant, we loaded the contents of ALL paragraphs above.
					//List<IStText> parsedTexts = texts.FindAll(HasLastParsedTimestamp);
					//if (parsedTexts.Count != 0)
					//{
					//    // We actually have parsed some texts before...yet we have to again. Possibly another program changed
					//    // the data. Reload it as efficiently as possible.
					//    int[] parsedHvos = new int[parsedTexts.Count];
					//    for (int i = 0; i < parsedHvos.Length; i++)
					//        parsedHvos[i] = parsedTexts[i].Hvo;
					//    int index = 0;
					//    string Hvos = DbOps.MakePartialIdList(ref index, parsedHvos);
					//    string whereClause = "";
					//    if (index == parsedHvos.Length)
					//    {
					//        // If we can make a single where clause we'll do it; otherwise do them all
					//        whereClause = " where Owner$ in (" + Hvos + ")";
					//    }
					//    string sql = "select Owner$, Id, UpdStmp, Contents, Contents_Fmt from StTxtPara_ " + whereClause + " order by owner$, OwnOrd$";
					//    IDbColSpec dcs = DbColSpecClass.Create();
					//    dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
					//    dcs.Push((int)DbColType.koctObjVecOwn, 1, (int)StText.StTextTags.kflidParagraphs, 0);
					//    dcs.Push((int)DbColType.koctTimeStamp, 2, 0, 0);
					//    dcs.Push((int)DbColType.koctString, 2, (int)StTxtPara.StTxtParaTags.kflidContents, 0);
					//    dcs.Push((int)DbColType.koctFmt, 2, (int)StTxtPara.StTxtParaTags.kflidContents, 0);
					//    cache.VwOleDbDaAccessor.Load(sql, dcs, 0, 0, null, false);
					//}

					// Need a separate loop for these, otherwise things get confused as we start to reuse
					// annotations in pp.Parse() and then re-encounter them in later attempts to salvage Pfics and segments.
#if PROFILING
					Debug.WriteLine("Time to end of preliminaries = " + (DateTime.Now.Ticks - ticks));
#endif
					foreach (IStText text in texts)
						pp.SalvageDummyAnnotations(text);
#if PROFILING
					Debug.WriteLine("Time to start of main parse loop = " + (DateTime.Now.Ticks - ticks));
#endif
					foreach (IStText text in texts)
					{
						pp.Parse(text, progress);
					}
#if PROFILING
					Debug.WriteLine("Time to end of main parse loop = " + (DateTime.Now.Ticks - ticks));
#endif
					StText.RecordParseTimestamps(texts);
					pp.CleanupLeftoverAnnotations(progress);
				}
				//Debug.WriteLine("Time for whole ConcordTexts = " + (DateTime.Now.Ticks - ticks));
				progress.SetMilestone();
				progress.Breath();
#if PROFILING
				Debug.WriteLine("Parse required " + pp.m_cDummyAnnotations + " dummy annotations"
					+ " but could only reuse " + pp.m_dummyAnnotationsToReuse.Count);
				Debug.WriteLine("  Parse created " + pp.m_cWficsMade + " Wfics, " + pp.m_cPficsMade + " Pfics, and "
					+ pp.m_cSegmentsMade + " Segments");
				Debug.WriteLine("  So far we made a total of " + s_cTotalDummiesMade + "; this parse making dummies took " + pp.m_cTicksMakingDummies);
				Debug.WriteLine("  This parse we reset " + pp.m_cTotalDummiesReset + " in a time of " + pp.m_cTicksResettingDummies);
#endif
			}
		}

		/// <summary>
		/// Making modifications to the paragraph contents will invalidate its virtual annotation properties (e.g. Segments, SegmentForms).
		/// Invalidating the Segments virtual property will allow ParagraphParser to recompute them.
		/// (EricP) Unfortunately, we currently don't have an interface for invalidating them so that they will get reloaded.
		/// Recover dummy annotations to reuse.
		/// </summary>
		private void SalvageDummyAnnotations(IStTxtPara para)
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			IVwCacheDa cda = Cache.VwCacheDaAccessor;
			int kflidParaSegments = StTxtPara.SegmentsFlid(Cache);
			int hvoPara = para.Hvo;
			if (sda.get_IsPropInCache(hvoPara, kflidParaSegments,
				(int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				int cseg = sda.get_VecSize(hvoPara, kflidParaSegments);
				for (int iseg = 0; iseg < cseg; iseg++)
				{
					int hvoSeg = sda.get_VecItem(hvoPara, kflidParaSegments, iseg);
					int cxfic = sda.get_VecSize(hvoSeg, kflidSegmentForms);
					for (int iform = 0; iform < cxfic; iform++)
					{
						int hvoForm = sda.get_VecItem(hvoSeg, kflidSegmentForms, iform);
						if (Cache.IsDummyObject(hvoForm))
							m_dummyAnnotationsToReuse.Add(hvoForm);
					}

					// AFTER we possibly use info from it, since we might clear that info!
					// And NOT if we require real segments...since that mode is used when fininishing an
					// annotation adjustment, and that could get Undone, restoring the dummy segments,
					// so they'd better be in their original state if they are put back (TE-8193).
					if (Cache.IsDummyObject(hvoSeg) && !m_fCreateRealSegments)
						m_dummyAnnotationsToReuse.Add(hvoSeg);
				}
				// In case we reprocess this para, or somehow else don't reset this, we want to make
				// the salvaged hvos definitely unreachable.
				cda.CacheVecProp(hvoPara, kflidParaSegments, new int[0], 0);
			}
			// (EricP) Don't ClearInfoAbout hvoPara, because it may blow away information cached
			// by ParagraphParser.ConcordTexts when trying to PreloadIfMissing the paragraph texts.
			//cda.ClearInfoAbout(hvoPara, VwClearInfoAction.kciaRemoveObjectInfoOnly);
		}
		private static bool HasLastParsedTimestamp(IStText text)
		{
			return text.LastParsedTimestamp != 0;
		}
		internal static List<int> ConcordParagraphsOfTexts(FdoCache cache, int[] hvosStText, ProgressState progress,
			IMatcher matcher, ConcordanceControl.ConcordanceLines line)
		{
			using (ParagraphParser pp = new ParagraphParser(cache))
			{
				// this will effectively clear ConcordanceWordforms, which seems overkill, but
				// since we are changing the occurrences on those wordforms,
				// and also possibly adding many new wordforms, we should just allow RecordLists that use
				// ConcordanceWordforms to reload the list.
				// (Enhance: is there any way we can make those lists be smart about when they need to reload,
				// rather than forcing them to?)
				(pp.m_wfi as WordformInventory).SuspendUpdatingConcordanceWordforms = true;
				pp.CreateDummyWordforms = true;
				pp.m_hvosStText = hvosStText;
				if (matcher != null)
					pp.m_matchingAnnotations = new List<int>();
				ParagraphParser.ResetParseSessionDependentStaticData();

				List<int> hvosParas = new List<int>();
				ISilDataAccess sda = cache.MainCacheAccessor;
				foreach (int hvo in hvosStText)
				{
					int chvoMax = sda.get_VecSize(hvo, (int)StText.StTextTags.kflidParagraphs);
					using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(chvoMax, typeof(int)))
					{
						int chvo;
						sda.VecProp(hvo, (int)StText.StTextTags.kflidParagraphs, chvoMax, out chvo, arrayPtr);
						Debug.Assert(chvo == chvoMax);
						hvosParas.AddRange((int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int)));
					}
				}
				int[] hvosStTxtPara = hvosParas.ToArray();
				// Estimate the number of total number of milestones we'll set.
				// Enhance: we could construct a way to set percentage done based upon
				// number of texts and paragraphs in each text.
				if (progress is MilestoneProgressState)
				{
					MilestoneProgressState mp = progress as SIL.FieldWorks.Common.Controls.MilestoneProgressState;
					for (int i = 0; i < hvosStTxtPara.Length; ++i)
					{
						mp.AddMilestone(1);
					}
				}
				// Preload all the paragraphs.
				cache.PreloadIfMissing(hvosStTxtPara, (int)StTxtPara.StTxtParaTags.kflidContents, 0, false);

				// Parse each text to load our paragraph and wordform segment annotations.
				int cPara = 0;
				using (SuppressSubTasks suppressor = new SuppressSubTasks(cache, true))
				{
					foreach (IStTxtPara para in new FdoObjectSet<IStTxtPara>(cache, hvosStTxtPara, false))
					{
						++cPara;
						pp.Parse(para, matcher, line);
						progress.SetMilestone();
						progress.Breath();
						if (pp.m_matchingAnnotations != null &&
							pp.m_matchingAnnotations.Count >= ConcordanceControl.MaxConcordanceMatches())
						{
							MessageBox.Show(String.Format(ITextStrings.ksShowingOnlyTheFirstXXXMatches,
								pp.m_matchingAnnotations.Count, cPara, hvosStTxtPara.Length),
								ITextStrings.ksNotice, MessageBoxButtons.OK, MessageBoxIcon.Information);
							break;
						}
					}
					pp.CleanupLeftoverAnnotations(progress);
				}
				progress.SetMilestone();
				progress.Breath();
				(pp.m_wfi as WordformInventory).SuspendUpdatingConcordanceWordforms = false;
				return pp.m_matchingAnnotations;
			}
		}

		internal static List<int> ConcordParagraphs(FdoCache cache, int[] hvosStTxtPara, ProgressState progress,
			IMatcher matcher, ConcordanceControl.ConcordanceLines line)
		{
			using (ParagraphParser pp = new ParagraphParser(cache))
			{
				// this will effectively clear ConcordanceWordforms, which seems overkill, but
				// since we are changing the occurrences on those wordforms,
				// and also possibly adding many new wordforms, we should just allow RecordLists that use
				// ConcordanceWordforms to reload the list.
				// (Enhance: is there any way we can make those lists be smart about when they need to reload,
				// rather than forcing them to?)
				(pp.m_wfi as WordformInventory).SuspendUpdatingConcordanceWordforms = true;
				pp.CreateDummyWordforms = true;
				pp.m_hvosStTxtPara = hvosStTxtPara;
				if (matcher != null)
					pp.m_matchingAnnotations = new List<int>();
				ParagraphParser.ResetParseSessionDependentStaticData();

				// Estimate the number of total number of milestones we'll set.
				// Enhance: we could construct a way to set percentage done based upon
				// number of texts and paragraphs in each text.
				if (progress is MilestoneProgressState)
				{
					MilestoneProgressState mp = progress as SIL.FieldWorks.Common.Controls.MilestoneProgressState;
					for (int i = 0; i < pp.m_hvosStTxtPara.Length; ++i)
					{
						mp.AddMilestone(1);
					}
				}

				// Preload all the paragraphs.
				cache.PreloadIfMissing(hvosStTxtPara, (int)StTxtPara.StTxtParaTags.kflidContents, 0, false);

				// Parse each text to load our paragraph and wordform segment annotations.
				int cPara = 0;
				using (SuppressSubTasks suppressor = new SuppressSubTasks(cache, true))
				{
					foreach (IStTxtPara para in new FdoObjectSet<IStTxtPara>(cache, pp.m_hvosStTxtPara, false))
					{
						++cPara;
						pp.Parse(para, matcher, line);
						progress.SetMilestone();
						progress.Breath();
						if (pp.m_matchingAnnotations != null &&
							pp.m_matchingAnnotations.Count >= ConcordanceControl.MaxConcordanceMatches())
						{
							MessageBox.Show(String.Format(ITextStrings.ksShowingOnlyTheFirstXXXMatches,
								pp.m_matchingAnnotations.Count, cPara, pp.m_hvosStTxtPara.Length),
								ITextStrings.ksNotice, MessageBoxButtons.OK, MessageBoxIcon.Information);
							break;
						}
					}
					pp.CleanupLeftoverAnnotations(progress);
				}
				progress.SetMilestone();
				progress.Breath();
				(pp.m_wfi as WordformInventory).SuspendUpdatingConcordanceWordforms = false;
				return pp.m_matchingAnnotations;
			}
		}

		public static void ConcordParagraphs(FdoCache cache, int[] hvosStTxtPara, ProgressState progress)
		{
			ConcordParagraphs(cache, hvosStTxtPara, progress, null,
				ConcordanceControl.ConcordanceLines.kBaseline);
		}

		private static void AddParseTextMilestones(MilestoneProgressState mp)
		{
			// we currently have 5 milestones per text.
			mp.AddMilestone(1);
			mp.AddMilestone(1);
			mp.AddMilestone(1);
			mp.AddMilestone(1);
			mp.AddMilestone(1);
		}

		public static void LoadParagraphInfo(IStTxtPara para, int tagSegments, int tagSegForms, bool fUseRealData)
		{
			ParseParagraph(para, tagSegments, tagSegForms, new ParagraphParserOptions(false, false, false, fUseRealData));
		}

		/// <summary>
		/// Session that parses through a paragraph, and collects information for it.
		/// </summary>
		/// <param name="para">The paragraph to work on</param>
		public static void ParseParagraph(IStTxtPara para)
		{
			ParseParagraph(para, false);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="para"></param>
		/// <param name="fCreateDummyWordforms">when true, we'll build our annotations using
		/// dummy wordforms instead of real ones, for new words, and each of those wordforms will reference
		/// their Occurrences in the text.  We also do not reset existing concordance.</param>
		public static void ParseParagraph(IStTxtPara para, bool fBuildConcordance)
		{
			ParseParagraph(para, fBuildConcordance, false, false);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="para"></param>
		/// <param name="fBuildConcordance">if true, collect wordform occurrences</param>
		/// <param name="fResetConcordance">if true, reset the wordform inventory concordance</param>
		/// <param name="fCreateRealWordforms">if true, create real wordforms, otherwise use dummy ones where possible.</param>
		public static void ParseParagraph(IStTxtPara para, bool fBuildConcordance, bool fResetConcordance, bool fCreateRealWordforms)
		{
			ParseParagraph(para, InterlinVc.ParaSegmentTag(para.Cache), InterlinVc.SegmentFormsTag(para.Cache),
				new ParagraphParserOptions(fBuildConcordance, fResetConcordance, fCreateRealWordforms, true));
		}

		/// <summary>
		/// Parse a single paragraph with the specified options.
		/// </summary>
		public static void ParseParagraph(IStTxtPara para, ParagraphParserOptions options)
		{
			ParseParagraph(para, InterlinVc.ParaSegmentTag(para.Cache), InterlinVc.SegmentFormsTag(para.Cache), options);
		}

		private static void ParseParagraph(IStTxtPara para, int tagSegments, int tagSegForms, ParagraphParserOptions options)
		{
			using (ParagraphParser pp = new ParagraphParser(para.Cache, tagSegments, tagSegForms))
			{
				pp.ParseWithOptions(para, options);
			}
		}

		private void ParseWithOptions(IStTxtPara para, ParagraphParserOptions options)
		{
			m_fSuppressSubTasks = options.SuppressSubTasks;
			m_fCreateRealSegments = options.CreateRealSegments;
			if (options.SuppressSubTasks)
			{
				using (SuppressSubTasks suppressor = new SuppressSubTasks(Cache, true))
				{
					ParseWithOptionsCore(para, options);
				}
			}
			else
			{
				ParseWithOptionsCore(para, options);
			}
		}

		private void ParseWithOptionsCore(IStTxtPara para, ParagraphParserOptions options)
		{
			ParagraphParser.ResetParseSessionDependentStaticData();
			if (options.ResetConcordance)
				(Cache.LangProject.WordformInventoryOA as WordformInventory).ResetAllWordformOccurrences();
			CollectWordformOccurrencesInTexts = options.CollectWordformOccurrencesInTexts;
			CreateDummyWordforms = !options.CreateRealWordforms;
			Parse(para, options.UseRealData);
		}

		/// <summary>
		/// Parse a text. Return true if successful. May fail because (a) no contents; (b) too long.
		/// Determining that the text doesn't need parsing counts as success.
		/// Todo: arrange to be able to Undo this, restoring the previous annotations.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fDidParse">True if an actual parse occurred and we need to reload.</param>
		/// <returns>True if successful</returns>
		public static bool ParseText(IStText text, ProgressState progress, out bool fDidParse)
		{
			return ParseText(text, false, progress, out fDidParse);
		}

		/// <summary>
		/// Parse a text. Return true if successful. May fail because (a) no contents; (b) too long.
		/// Determining that the text doesn't need parsing counts as success.
		/// Todo: arrange to be able to Undo this, restoring the previous annotations.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fSuppressSubTasks">true if we don't want to be able to Undo.</param>
		/// <param name="fDidParse">True if an actual parse occurred and we need to reload.</param>
		/// <returns>True if successful</returns>
		public static bool ParseText(IStText text, bool fSuppressSubTasks, ProgressState progress, out bool fDidParse)
		{
			ParagraphParserOptions options = new ParagraphParserOptions();
			options.SuppressSubTasks = fSuppressSubTasks;
			return ParseText(text, options, progress, out fDidParse);
		}

		public static bool ParseText(IStText text, ParagraphParserOptions options, ProgressState progress, out bool fDidParse)
		{
			fDidParse = false;
			if (text == null || !text.IsValidObject())
				return false;
			if (text.ParagraphsOS.Count == 0)
				return false;
#if DEBUG
			//TimeRecorder.Begin("deciding whether to parse para");
#endif
			if (text.IsUpToDate())
				return true;
#if DEBUG
			//TimeRecorder.End("deciding whether to parse para");
#endif
			fDidParse = true; // we're going to parse!
#if DEBUG
			//TimeRecorder.Begin("parse text");
#endif
			if (progress is MilestoneProgressState)
			{
				MilestoneProgressState mp = progress as SIL.FieldWorks.Common.Controls.MilestoneProgressState;
				AddParseTextMilestones(mp);
			}

			// Reparsing a whole text, words may have moved from one paragraph to another.
			// Do the whole with a single ParagraphParser.
#if DEBUG
			//TimeRecorder.Begin("setup");
#endif
			ParagraphParser.ResetParseSessionDependentStaticData();
			WordformInventory wfi = (text.Cache.LangProject.WordformInventoryOA as WordformInventory);

			if (options.SuppressSubTasks)
			{
				using (SuppressSubTasks suppressor = new SuppressSubTasks(text.Cache, true))
				{
					ParseTextCore(text, options, progress);
				}
			}
			else
			{
				ParseTextCore(text, options, progress);
			}
			return true; // succeeded.
#if DEBUG
			//TimeRecorder.End("note parse times");
			//TimeRecorder.Report();
#endif
		}

		private static void ParseTextCore(IStText text, ParagraphParserOptions options, ProgressState progress)
		{
			using (ParagraphParser pp = new ParagraphParser(text.Cache))
			{
				pp.SetOptions(options);
				if (options.ResetConcordance)
					(text.Cache.LangProject.WordformInventoryOA as WordformInventory).ResetAllWordformOccurrences();
				if (text.LastParsedTimestamp != 0)
				{
					// We actually have parsed before...yet we have to again. Possibly another program changed
					// the data. Reload it as efficiently as possible.
					string sql = "select Id, UpdStmp, Contents, Contents_Fmt from StTxtPara_ where Owner$ = " + text.Hvo + " order by OwnOrd$";
					IDbColSpec dcs = DbColSpecClass.Create();
					dcs.Push((int)DbColType.koctObjVecOwn, 0, (int)StText.StTextTags.kflidParagraphs, 0);
					dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
					dcs.Push((int)DbColType.koctString, 1, (int)StTxtPara.StTxtParaTags.kflidContents, 0);
					dcs.Push((int)DbColType.koctFmt, 1, (int)StTxtPara.StTxtParaTags.kflidContents, 0);
					text.Cache.VwOleDbDaAccessor.Load(sql, dcs, text.Hvo, 0, null, false);
				}
				pp.SalvageDummyAnnotations(text);
				pp.Parse(text, progress);
				text.RecordParseTimestamp();
				pp.AddEntryGuesses(progress);
				pp.CleanupLeftoverAnnotations(progress);
			}
		}

		private void SetOptions(ParagraphParserOptions options)
		{
			CollectWordformOccurrencesInTexts = options.CollectWordformOccurrencesInTexts;
			m_fCreateRealSegments = options.CreateRealSegments;
			CreateDummyWordforms = !options.CreateRealWordforms;
			m_fSuppressSubTasks = options.SuppressSubTasks;
			// Enhance: figure how to obey options.UseRealData;
			// options.ResetConcordance is used in the calling method to decide whether to ResetAllWordformOccurrences on the Wfi
		}

		private void Parse(IStText text, ProgressState progress)
		{
			progress.SetMilestone(ITextStrings.ksPreparingInterlinear1);
			progress.Breath();

			m_ihvoPara = 0;
			m_hvoText = text.Hvo;
			m_cparas = text.ParagraphsOS.Count;
#if DEBUG
			//TimeRecorder.End("setup");
			//TimeRecorder.Begin("parse");
#endif
			progress.SetMilestone(ITextStrings.ksPreparingInterlinear2);
			progress.Breath();
			// we only need to load our real database information once, since it's for all paragraphs.
			if (!m_fFinishedBuildAnalysisList)
			{
				BuildAnalysisList(progress);
			}

			progress.SetMilestone(ITextStrings.ksPreparingInterlinear3);
			progress.Breath();
			Cache.PreloadIfMissing(text.ParagraphsOS.HvoArray, (int)StTxtPara.StTxtParaTags.kflidContents, 0, false);
			foreach (StTxtPara para in text.ParagraphsOS)
			{
				Setup(para);
				Parse();
				m_ihvoPara++;
				//progress.Breath();
			}

#if DEBUG
			//TimeRecorder.End("parse");
			//TimeRecorder.End("parse text");
#endif
			progress.SetMilestone(ITextStrings.ksPreparingInterlinear4);
			progress.Breath();

#if DEBUG
			//TimeRecorder.Begin("note parse times");
#endif
			// If we didn't have to make any dummy annotations, this text is sufficiently analyzed not to need
			// any. Therefore we can record a time when this was done, and won't have to run the parser on it
			// again until it gets modified. If we made dummy ones, they will get thrown away when the
			// program exits or the cache is cleared, so we need to reparse the text the next time we see it
			// in order to make a new set of dummy annotations to display.
			// Enhance JohnT: possibly we could record a time in the cache to avoid reparsing if we come back
			// to this text and have NOT modified it and the dummy annotations are still in memory.
			// Enhance JohnT: currently nothing ever makes real punctuation annotations. Therefore hardly any
			// text will ever be fully annotated (no dummy annotations). We may want to enhance so that
			// if the only dummy annotations in a text are punctuation, we make real punctuation annotations
			// so we can record a complete annotation timestamp.
			// But: currently we need to parse every text when first brought into memory, because that is
			// what makes the dummy TwficRealForm property values that let us get case right. Unless we change
			// that, the whole mechanism for tracking whether it is up to date in the database is irrelevant.
			IOleDbCommand odc = null;
			try
			{
				text.Cache.DatabaseAccessor.CreateCommand(out odc);
				if (!m_fMadeDummyAnnotations)
				{
					string sql2 = string.Format("declare @res nvarchar(4000);" +
						" exec NoteInterlinProcessTime {0}, {1}, @res OUTPUT; select @res",
						CmAnnotationDefn.ProcessTime(text.Cache).Hvo, text.Hvo);
					uint cbSpaceTaken;
					bool fMoreRows;
					bool fIsNull;
					byte[] rgbTemp = null;
					using (ArrayPtr rgchMarshalled = MarshalEx.ArrayToNative(4000, typeof(char)))
					{
						progress.Breath();
						odc.ExecCommand(sql2, (int)SqlStmtType.knSqlStmtStoredProcedure);
						odc.GetRowset(0);
						// We need to update the cache if one or more CmBaseAnnotation objects were
						// created in LangProject_Annotations by the NoteInterlinProcessTime stored
						// procedure.
						odc.NextRow(out fMoreRows);
						odc.GetColValue(1, rgchMarshalled, rgchMarshalled.Size, out cbSpaceTaken, out fIsNull, 0);
						rgbTemp = (byte[])MarshalEx.NativeToArray(rgchMarshalled,
							(int)cbSpaceTaken, typeof(byte));
					}
					char[] delim = { ',' };
					string[] sVals = Encoding.Unicode.GetString(rgbTemp).Split(delim);
					List<int> newAnns = new List<int>();
					for (int i = 0; i < sVals.Length; ++i)
					{
						if (sVals[i].Length != 0)
						{
							int hvo = Convert.ToInt32(sVals[i]);
							newAnns.Add(hvo);
						}
					}
					int[] newAnnotations = DbOps.ListToIntArray(newAnns);
					if (newAnnotations.Length != 0)
					{
						progress.Breath();
						// update the cache.
						IVwCacheDa csda = text.Cache.MainCacheAccessor as IVwCacheDa;
						Debug.Assert(csda != null);
						int hvoObj = text.Cache.LangProject.Hvo;
						int ihvoLim = text.Cache.LangProject.AnnotationsOC.Count;
						csda.CacheReplace(hvoObj,
							(int)LangProject.LangProjectTags.kflidAnnotations,
							ihvoLim, ihvoLim,
							newAnnotations, newAnnotations.Length);
					}
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
				progress.Breath();
			}
			progress.SetMilestone(ITextStrings.ksPreparingInterlinear5);
			progress.Breath();
		}

		private void Parse(IStTxtPara para, IMatcher matcher, ConcordanceControl.ConcordanceLines line)
		{
			Parse(para, matcher, line, true);
		}

		private void Parse(IStTxtPara para, IMatcher matcher, ConcordanceControl.ConcordanceLines line, bool fUseRealData)
		{
			if (m_fSuppressSubTasks)
			{
				using (SuppressSubTasks suppressor = new SuppressSubTasks(para.Cache, true))
				{
					ParseCore(para, matcher, line, fUseRealData);
				}
			}
			else
			{
				ParseCore(para, matcher, line, fUseRealData);
			}
		}

		private void ParseCore(IStTxtPara para, IMatcher matcher, ConcordanceControl.ConcordanceLines line, bool fUseRealData)
		{
			Setup(para, matcher, line);
			if (fUseRealData && !m_fFinishedBuildAnalysisList)
			{
				BuildAnalysisList(new NullProgressState());	// load any existing data from the database.
			}
			Parse();
			CleanupLeftoverAnnotations(new NullProgressState());
		}

		private void Parse(IStTxtPara para, bool fUseRealData)
		{
			Parse(para, null, ConcordanceControl.ConcordanceLines.kBaseline, fUseRealData);
		}

		/// <summary>
		/// Salvage dummy Pfics and Segments from the specified text and add any dummies to m_dummiesToReuse.
		/// Be careful to make all calls to this before starting any parsing that would actually reuse dummies.
		/// We depend on the old annotation data to isolate Wfics and NOT add them to the list AGAIN.
		/// </summary>
		/// <param name="text"></param>
		internal void SalvageDummyAnnotations(IStText text)
		{
			foreach (StTxtPara para in text.ParagraphsOS)
			{
				SalvageDummyAnnotations(para);
			}
		}

		private ParagraphParser()
		{
		}

		public ParagraphParser(FdoCache cache) : this(cache, InterlinVc.ParaSegmentTag(cache), InterlinVc.SegmentFormsTag(cache))
		{
		}

		private ParagraphParser(FdoCache cache, int tagParaSegments, int tagSegmentForms)
		{
			kflidSegmentForms = tagSegmentForms;
			kflidParaSegments = tagParaSegments;
			Init(cache);
		}

		public ParagraphParser(IStTxtPara para)
			: this(para.Cache)
		{
			Setup(para);
		}

		/// <summary>
		/// Reloads the Segment/SegForm properties using real annotations.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="fDontReadjustReusedCbaOffsets">if true, the parser will not try to AdjustCbaFields
		/// to the text. It assumes the real annotations are accurrate, and complains if they aren't.</param>
		public static void ReconstructSegmentsAndXfics(IStTxtPara para, bool fDontReadjustReusedCbaOffsets)
		{
			using (ParagraphParser pp = ParagraphParser.Create(para.Cache, fDontReadjustReusedCbaOffsets))
			{
				pp.ParseWithOptions(para, new ParagraphParserOptions(false, false, false, true));
			}
		}

		/// <summary>
		/// Paragraph parser factory.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="fDontReadjustReusedCbaOffsets">if true, the parser will not try to AdjustCbaFields
		/// to the text. It assumes the real annotations are accurrate, and complains if they aren't.</param>
		/// <returns></returns>
		static private ParagraphParser Create(FdoCache cache, bool fDontReadjustReusedCbaOffsets)
		{
			if (fDontReadjustReusedCbaOffsets)
				return new ParagraphParserForEditMonitoring(cache);
			else
				return new ParagraphParser(cache);
		}

		private void Init(FdoCache cache)
		{
			m_cache = cache;
			kflidOccurrences = WfiWordform.OccurrencesFlid(m_cache);
			kflidConcordanceWordforms = WordformInventory.ConcordanceWordformsFlid(m_cache);
			m_lp = Cache.LangProject;
			m_paragraphTextScanner = new WordMaker(null, cache.LanguageWritingSystemFactoryAccessor);
			m_wfi = m_lp.WordformInventoryOA;
			// Identify the tag used for the virtual property that is the form of a punctuation
			// annotation.
			m_tagPunct = CmBaseAnnotation.StringValuePropId(m_cache);
			m_cadPunct = CmAnnotationDefn.Punctuation(m_cache);
			m_cadTwfic = CmAnnotationDefn.Twfic(m_cache);
			m_tagRealForm = InterlinVc.TwficRealFormTag(cache);
			m_tagLowercaseForm = InterlinVc.MatchingLowercaseWordForm(cache);
		}

		private void Setup(IStTxtPara para, IMatcher matcher, ConcordanceControl.ConcordanceLines line)
		{
			m_para = para;
			m_tssPara = para.Contents.UnderlyingTsString;
			m_paraWs = StringUtils.GetWsAtOffset(m_tssPara, 0);
			m_wordMaker = new WordMaker(m_tssPara, para.Cache.LanguageWritingSystemFactoryAccessor);
			m_paragraphTextScanner.Tss = m_tssPara;
			m_matcher = matcher;
			m_line = line;
		}

		private void Setup(IStTxtPara para)
		{
			Setup(para, null, ConcordanceControl.ConcordanceLines.kBaseline);
		}

		/// <summary>
		/// NOTE: we only need to load our real database information once, since it's for all paragraphs (in all texts).
		/// Retrieve from the database the existing annotations we will try to reuse, if any.
		/// The idea is to build a list of wordforms that exist in the old analysis, and reuse
		/// as many as possible. Assumes that any existing analysis has been loaded.
		/// </summary>
		void BuildAnalysisList(ProgressState progress)
		{
			Debug.Assert(m_fFinishedBuildAnalysisList == false, "We should try to avoid running this more than once.");
			progress.Breath();
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			// First the segment-level annotations.
			ReadAnnotationInfo(cda, TextSegmentDefn, m_realParagraphSegmentAnnotations);
			progress.Breath();

			// Now get the information about annotations and wordforms.
			// This retrieves an array of 8-element arrays,
			//	[0] cba.id			-- id of the CmBaseAnnotation
			//	[1] wf.id			-- id of its WfiWordform
			//	[2] ann.class$		-- class of the CmBaseAnnotation.InstanceOf (WfiWordform, WfiAnalysis, or WfiGloss)
			//	[3] cba.BeginOffset -- paragraph begin offset
			//  [4] cba.EndOffset	-- paragraph end offset
			//  [5] cba.BeginObject -- paragraph id
			//	[6] stp.Src			-- StText
			//  [7] stp.Ord			-- order in sequence of paragraphs for the StText
			//  [8] ann.id			-- the id of CmBaseAnnotation.InstanceOf
			string wordQuery = "select cba.id, wf.id, ann.class$, cba.BeginOffset, cba.EndOffset, cba.BeginObject, stp.Src, stp.Ord, ann.id"
				+ " from CmBaseAnnotation cba"
				+ " join CmAnnotation ca on ca.id = cba.id"
				+ " and ca.AnnotationType = {0}"
				+ " left outer join CmObject ann on ca.InstanceOf = ann.id"
				+ " left outer join WfiGloss_ wg on wg.id = ann.id"
				+ " left outer join WfiAnalysis_ wa on wa.id = ann.id or wa.id = wg.Owner$"
				+ " left outer join WfiWordform_ wf on wf.id = ann.id or wf.id = wa.Owner$"
				+ " left outer join StText_Paragraphs stp on stp.Dst=cba.BeginObject"
				+ " left outer join StTxtPara para on para.Id = stp.Dst"
				+ " {1}"
				+ " order by stp.Src, stp.Ord, cba.BeginObject, cba.BeginOffset";

			// limit the twfic query to the scope of the parse session.
			bool fResetRealPhrases = false;
			string queryFilterFmt = "";
			string joinedIds;
			if (m_hvosStTxtPara != null)
			{
				joinedIds = CmObject.JoinIds(m_hvosStTxtPara, ",");
				queryFilterFmt = "where stp.Dst in ({0}) or stp.Src is NULL";
				fResetRealPhrases = true;
			}
			else if (m_hvosStText != null)
			{
				// limit the query to the texts
				joinedIds = CmObject.JoinIds(m_hvosStText, ",");
				queryFilterFmt = "where stp.Src in ({0}) or stp.Src is NULL";
				fResetRealPhrases = true;
			}
			else if (m_hvoText != 0)
			{
				// limit the query to the text
				joinedIds = m_hvoText.ToString();
				queryFilterFmt = "where stp.Src = {0} or stp.Src is NULL";
				fResetRealPhrases = true;
			}
			else
			{
				// limit the query to the paragraph.
				joinedIds = m_para.Hvo.ToString();
				queryFilterFmt = "where stp.Dst = {0}";
				// typically ParseParagraph() will be called within the context of a text already
				// having been parsed. In that case we'll simply want to reuse the phrases found
				// by those parses.
				// In CollectWordformOccurrencesInTexts, however, (currently, only in tests), we are treating the
				// current parse session as atomic, and so, we trust that it's real data is all we need.
				fResetRealPhrases = CollectWordformOccurrencesInTexts;
			}

			List<int[]> wordInfoAl;
			if (m_cache.DatabaseAccessor == null)
				wordInfoAl = new List<int[]>(); // running tests, assume no existing ones.
			else
			{
				wordInfoAl = RunAnalysisListQuery(wordQuery, queryFilterFmt, joinedIds);
			}
			int[][] wordInfo = wordInfoAl.ToArray();

			if (fResetRealPhrases || s_realPhrases.Count == 0 || s_cacheForRealPhrases != m_cache)
				ClearPhraseWordforms();
			else
				RemoveObsoletePhraseWordforms();

			Dictionary<int, Set<int>> wsToWordforms = new Dictionary<int, Set<int>>();
			m_annotations = new int[wordInfo.Length];
			m_wordforms = new int[m_annotations.Length];
			m_realParagraphTwficAnnotations.Clear();
			m_orphanedAnnotations.Clear();
			int hvoPara = 0;
			int hvoParaPrev = -1;
			List<int> paragraphTwficAnnotations = null;
			List<int> paragraphWordforms = null;
			Set<int> annotationsToReserve = new Set<int>();
			int hvoWordform = 0;
			for (int i = 0; i < m_annotations.Length; ++i)
			{
				int hvoAnnotation = wordInfo[i][0];
				m_annotations[i] = hvoAnnotation;

				hvoPara = wordInfo[i][5];
				// first cache any orphaned annotations.
				if (hvoPara == CmBaseAnnotation.kHvoBeginObjectOrphanage)
				{
					m_orphanedAnnotations.Add(hvoAnnotation);
					continue;
				}
				else if (hvoPara != hvoParaPrev)
				{
					// start building a new set of ids.
					paragraphTwficAnnotations = new List<int>();
					paragraphWordforms = new List<int>();
					m_realParagraphTwficAnnotations[hvoPara] = paragraphTwficAnnotations;
					m_realParagraphWordforms[hvoPara] = paragraphWordforms;
					hvoParaPrev = hvoPara;
					m_paraWs = 0;
					if (m_cache.IsValidObject(hvoPara))
						m_paraWs = StTxtPara.GetWsAtParaOffset(m_cache, hvoPara, 0);
				}
				// process the wordform information
				hvoWordform = wordInfo[i][1];
				if (!IsValidAnnotation(wordInfo, i))
				{
					// Bad annotation!
					// This can quite plausibly happen if something deletes the object (say an analysis)
					// that is the InstanceOf of an annotation. Our last-resort cleanup code changes
					// the InstanceOf to zero. Unfortuately, such annotations still show up in
					// IText unless something like this cleans up.
					// (EricP) Instead of deleting the annotation,
					// we can simply reserve the annotation to be reused later.
					//m_lp.AnnotationsOC.Remove(m_annotations[i]);
					annotationsToReserve.Add(m_annotations[i]);
					m_wordforms[i] = 0;	// never consider using this one.
					continue;
				}
				m_wordforms[i] = hvoWordform;
				paragraphWordforms.Add(hvoWordform);
				paragraphTwficAnnotations.Add(hvoAnnotation);

				int cbaBeginOffset = wordInfo[i][3];
				int cbaInstanceOf = wordInfo[i][8];
				// This greatly improves later performance of SetCbaFields and reusing annotations.
				cda.CacheObjProp(hvoAnnotation,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject, hvoPara);
				cda.CacheIntProp(hvoAnnotation,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset, cbaBeginOffset);
				cda.CacheIntProp(hvoAnnotation,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset, wordInfo[i][4]);
				cda.CacheObjProp(hvoAnnotation,
					(int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf, cbaInstanceOf);

				// establish a default ws for the paragraph, if we haven't already done so.
				int wsActual = 0;
				ITsString tssWordform = null;
				if (m_paraWs == 0)
				{
					if (TryBestWordform(hvoWordform, hvoPara, cbaBeginOffset, out tssWordform, out wsActual))
						m_paraWs = wsActual;
					else
						continue;	// nothing else we can do with this wordform.
				}
				// See if we can update our phrase forms for ws-wordform pairs we haven't already added.
				//if (cbaInstanceOf == hvoWordform)
				//	continue; // we only want to add wordforms with non-trivial analyses.
				Set<int> hvoWordforms;
				if (!wsToWordforms.TryGetValue(m_paraWs, out hvoWordforms) || !hvoWordforms.Contains(hvoWordform))
				{
					if (tssWordform != null && tssWordform.Length > 0 || TryBestWordform(hvoWordform, hvoPara, cbaBeginOffset, out tssWordform, out wsActual))
					{
						if (m_paraWs != wsActual)
						{
							// lookup in our dictionary for the actual ws.
							if (wsToWordforms.TryGetValue(wsActual, out hvoWordforms) && hvoWordforms.Contains(hvoWordform))
							{
								// we've already added this one, so skip it.
								continue;
							}
						}
						TryAddPhraseWordform(tssWordform);
						// update our ws-wordforms dictionary
						if (hvoWordforms == null)
							wsToWordforms[wsActual] = new Set<int>();
						wsToWordforms[wsActual].Add(hvoWordform);
					}
				}

				// Cache the ws of the twfic for its wordform
				//cda.CacheIntProp(hvoAnnotation,
				//	(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem, wsWordform);
			}
			if (annotationsToReserve.Count > 0)
			{
				ReserveAnnotations(annotationsToReserve, false);
			}
			progress.Breath();

			// Now get any existing punctuation annotations.
			m_punctAnnotations = ReadAnnotationInfo(cda, m_cadPunct.Hvo, m_realParagraphPunctuationAnnotations);
			m_fFinishedBuildAnalysisList = true;
		}

		private int maxQueryLength = 3900; // some safety margin
		private List<int[]> RunAnalysisListQuery(string wordQuery, string queryFilterFmt, string joinedIds)
		{
			if (joinedIds.Length + wordQuery.Length + queryFilterFmt.Length < maxQueryLength)
			{
				// Original implementation with single query.
				string queryFilter = string.Format(queryFilterFmt, joinedIds);
				List<int[]> wordInfoAl;
				string wordQueryComplete = String.Format(wordQuery, CmAnnotationDefn.Twfic(m_cache).Hvo, queryFilter);
				wordInfoAl = DbOps.ReadIntArray(m_cache, wordQueryComplete, null, 9);
				return wordInfoAl;
			}
			// Need repeat calls to avoid crashing with over-long query.
			string remainingIds = joinedIds;
			List<int[]> result = new List<int[]>();

			while (remainingIds.Length > 0)
			{
				int maxIdsLength = maxQueryLength - wordQuery.Length - queryFilterFmt.Length;
				string thisTimeIds = remainingIds;
				if (remainingIds.Length > maxIdsLength)
				{
					int index = remainingIds.LastIndexOf(',', maxIdsLength);
					thisTimeIds = remainingIds.Substring(0, index);
					remainingIds = remainingIds.Substring(index + 1);// skip comma
				}
				else
				{
					remainingIds = ""; // all in thisTimeIds
				}
				string queryFilter = string.Format(queryFilterFmt, thisTimeIds);
				string wordQueryComplete = String.Format(wordQuery, CmAnnotationDefn.Twfic(m_cache).Hvo, queryFilter);
				result.AddRange(DbOps.ReadIntArray(m_cache, wordQueryComplete, null, 9));
			}
			return result;
		}

		private bool IsValidAnnotation(int[][] wordInfo, int i)
		{
			// First determine whether we have a valid class for the InstanceOf.
			switch (wordInfo[i][2])
			{
				case WfiWordform.kclsidWfiWordform:
				case WfiAnalysis.kclsidWfiAnalysis:
				case WfiGloss.kclsidWfiGloss:
					break;
				default:
					// bad annotation
					return false;

			}
			// Next determine whether we have valid offsets
			int cbaBeginOffset = wordInfo[i][3];
			int cbaEndOffset = wordInfo[i][4];
			if (cbaBeginOffset < 0 || cbaEndOffset < 0)
				return false;
			return true;
		}


		private void ReserveAnnotations(Set<int> annotationsToReserve, bool fDeleteUnreferencedInstanceOf)
		{
			CmBaseAnnotation.ReserveAnnotations(m_cache, annotationsToReserve, fDeleteUnreferencedInstanceOf);
			// add the orphaned annotations to the available twfics to be reused.
			m_orphanedAnnotations.AddRange(annotationsToReserve);
		}

		bool TryBestWordform(int hvoWordform, int hvoPara,  int ichMin, out ITsString tssWordAnn, out int actualWs)
		{
			int wsPreferred = m_paraWs;
			ITsString tssPara = m_cache.GetTsStringProperty(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents);
			if (ichMin >= 0 && ichMin < tssPara.Length)
				wsPreferred = StringUtils.GetWsAtOffset(tssPara, ichMin);
			tssWordAnn = null;
			actualWs = 0;
			return (Cache.LangProject as LangProject).TryWs(wsPreferred, LangProject.kwsFirstVern,
				hvoWordform, (int)WfiWordform.WfiWordformTags.kflidForm, out actualWs, out tssWordAnn);
		}

		/// <summary>
		/// Re-Populates s_realPhrases, a dictionary of phrase forms keyed by the lower cased form of the first word.
		/// </summary>
		/// <param name="wordformIds"></param>
		void ClearPhraseWordforms()
		{
			// Preload WfiWordform_Form ws and text if we haven't already done so.
			(m_wfi as WordformInventory).PreLoadFormIdTable();
			s_cacheForRealPhrases = m_cache;
			s_realPhrases.Clear();
		}

		private bool TryAddPhraseWordform(ITsString tssWordform)
		{
			// (LT-5856) for phrase annotations, use the first word as a key
			string annFirstWordformLowered;
			ITsString firstWord = FirstWord(tssWordform, Cache.LanguageWritingSystemFactoryAccessor, out annFirstWordformLowered);
			if (firstWord != null && firstWord.Length < tssWordform.Length)
			{
				// keep track of this phrase so we can bundle subsequent matching guesses.
				if (!s_realPhrases.ContainsKey(annFirstWordformLowered))
					s_realPhrases[annFirstWordformLowered] = new Set<ITsString>();
				s_realPhrases[annFirstWordformLowered].Add(tssWordform);
				return true;
			}
			return false;
		}

		/// <summary>
		/// look through our cached realPhrases and remove the ones that don't exist in our wordform inventory.
		/// </summary>
		void RemoveObsoletePhraseWordforms()
		{
			WordformInventory.OnChangedWordformsOC(); // load fresh real wordform ids
			(m_wfi as WordformInventory).PreLoadFormIdTable();
			List<string> keysToDel = new List<string>(s_realPhrases.Count);
			foreach (string key in s_realPhrases.Keys)
			{
				Set<ITsString> phraseSet = s_realPhrases[key];
				Set<ITsString> phraseSetCopy = new Set<ITsString>(phraseSet);
				foreach (ITsString phrase in phraseSetCopy)
				{
					int hvoWordform = m_wfi.GetWordformId(phrase, true);
					if (hvoWordform == 0)
						phraseSet.Remove(phrase);
					if (phraseSet.Count == 0)
						keysToDel.Add(key);
				}
			}
			foreach (string key in keysToDel)
			{
				s_realPhrases.Remove(key);
			}
		}


		/// <summary>
		/// Query is a bit of sql that returns a set of triples, a CmBaseAnnotation id, begin offset, and end offset.
		/// Run the query and return an array of the ids.
		/// Also cache the offsets and that these annotations have m_para.Hvo as their BeginObject.
		/// </summary>
		private int[] ReadAnnotationInfo(IVwCacheDa cda, int hvoAnnType, Dictionary<int, List<int>> paragraphAnnotationMap)
		{
			string query = "select cba.id, cba.BeginOffset, cba.EndOffset, cba.BeginObject " +
				"from CmBaseAnnotation cba " +
				"join CmAnnotation ca on cba.id = ca.id and ca.AnnotationType = {0} " +
				"{1} " +
				"order by cba.BeginObject, cba.BeginOffset";
			string queryFilter = "";
			string joinedIds = "";
			if (m_hvosStTxtPara != null)
			{
				joinedIds = CmObject.JoinIds(m_hvosStTxtPara, ",");
			}
			else if (m_hvosStText != null)
			{
				joinedIds = ""; // in this case, we'll just load for all texts.
			}
			else if (m_hvoText != 0)
			{
				// limit the query to the paragraphs in the given StText
				StText stText = new StText(Cache, m_hvoText);
				joinedIds = CmObject.JoinIds(stText.ParagraphsOS.HvoArray, ",");
			}
			else
			{
				// limit the query to the paragraph.
				joinedIds = m_para.Hvo.ToString();
			}
			if (!String.IsNullOrEmpty(joinedIds))
				queryFilter = string.Format("where cba.BeginObject in ({0})", joinedIds);
			query = String.Format(query, hvoAnnType, queryFilter);
			int[] annotations;
			List<int[]> annotationInfo;
			if (m_cache.DatabaseAccessor == null)
			{
				// testing without a database: no existing annotations (unless pre-created by test)
				annotationInfo = new List<int[]>();
			}
			else
			{
				// normal case
				annotationInfo = DbOps.ReadIntArray(m_cache, query, null, 4);
			}
			annotations = new int[annotationInfo.Count];
			if (annotations.Length == 0)
				return annotations;

			paragraphAnnotationMap.Clear();
			List<int> paragraphAnnotations = null;
			int hvoPara = 0;
			int hvoParaPrev = 0;
			for (int i = 0; i < annotations.Length; i++)
			{
				int[] rowInfo = annotationInfo[i];
				int hvoAnnotation = rowInfo[0];
				hvoPara = rowInfo[3];
				if (hvoPara != hvoParaPrev || hvoPara == 0)
				{
					// start building a new set of ids.
					paragraphAnnotations = new List<int>();
					paragraphAnnotationMap[hvoPara] = paragraphAnnotations;
					hvoParaPrev = hvoPara;
				}
				paragraphAnnotations.Add(hvoAnnotation);
				annotations[i] = hvoAnnotation;
				cda.CacheObjProp(hvoAnnotation,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject, hvoPara);
				cda.CacheIntProp(hvoAnnotation,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset, rowInfo[1]);
				cda.CacheIntProp(hvoAnnotation,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset, rowInfo[2]);
			}

			return annotations;
		}

		private void CleanupLeftoverAnnotations(ProgressState progress)
		{
			CleanupLeftoverRealAnnotations(progress);
			CleanupLeftoverDummyAnnotations(progress);
		}

		/// <summary>
		/// There might not be very much harm about leaving these around...now we're reusing them
		/// there shouldn't usually be very many left over...but we may as well save what we can.
		/// </summary>
		/// <param name="progress"></param>
		private void CleanupLeftoverDummyAnnotations(ProgressState progress)
		{
			if (m_dummyAnnotationsToReuse == null)
				return;
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			for (int i = m_iNextDummyAnnotationToReuse; i < m_dummyAnnotationsToReuse.Count; i++)
				cda.ClearInfoAbout(m_dummyAnnotationsToReuse[i], VwClearInfoAction.kciaRemoveObjectInfoOnly);

		}

		protected virtual void CleanupLeftoverRealAnnotations(ProgressState progress)
		{
			if (m_matcher != null)
				return;		// we're just doing a partial parse, so keep all real annotations.
			Set<int> idsToDel = new Set<int>();

			// Add to the document all the things we want to delete.
			// (For each paragraph we've parsed...)
			// Any existing annotations we haven't reused need to be done away with.
			// Instead of deleting unused twfics, we'll simply reserve them to be reused later.
			CmBaseAnnotation.ReserveAnnotations(m_cache, m_unusedTwficAnnotations, false);
			// AddIdsToDel(progress, ref idsToDel, m_unusedTwficAnnotations);
			AddIdsToDel(progress, idsToDel, m_unusedPunctuationAnnotations);
			AddIdsToDel(progress, idsToDel, m_unusedSegmentAnnotations);

			if (idsToDel.Count > 0)
			{
				if (progress is MilestoneProgressState)
				{
					MilestoneProgressState mp = progress as MilestoneProgressState;
					mp.AddMilestone(1);
				}
				Debug.WriteLine("Removing leftover annotations: " + CmObject.JoinIds(idsToDel.ToArray(), ","));
				// Enhance JohnT: should we do something (delete or move?) about any freeform annotations
				// attached to these segments? For now they will become orphans...
				CmObject.DeleteObjects(idsToDel, Cache, VwClearInfoAction.kciaRemoveObjectInfoOnly);
				progress.SetMilestone();
				progress.Breath();
			}

			m_unusedTwficAnnotations.Clear();
			m_unusedPunctuationAnnotations.Clear();
			m_unusedSegmentAnnotations.Clear();
		}

		private static int AddIdsToDel(ProgressState progress, Set<int> idsToDel, Set<int> unusedIds)
		{
			Debug.Assert(unusedIds != null);
			idsToDel.AddRange(unusedIds);
			progress.Breath();

			return unusedIds.Count;
		}

		/// <summary>
		/// Creates a single punctuation annotation for the specified range.
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		private int CreatePunctAnnotation(int ichMin, int ichLim)
		{
			int hvoAnn = ReuseFirstUnusedAnnotation(m_realParagraphPunctuationAnnotations,
				m_unusedPunctuationAnnotations, ichMin, ichLim, m_cadPunct.Hvo);
			if (hvoAnn == 0)
			{
				// We've run out of ones to reuse, make a new one.
				hvoAnn = CreateDummyAnnotation(0, ichMin, ichLim, m_cadPunct.Hvo);
				//				cba = new CmBaseAnnotation();
				//				m_lp.AnnotationsOC.Add(cba);
				//				cba.Flid = (int)StTxtPara.StTxtParaTags.kflidContents;
				//				cba.AnnotationTypeRA = m_cadPunct;
				//				hvoAnn = cba.Hvo;
				//				SetCbaFields(hvoAnn, ichMin, ichLim, m_para.Hvo, cba != null);
			}
			else
			{
				AdjustCbaFields(hvoAnn, ichMin, ichLim);
			}
			// In case this ID has previously been used for a different bit of
			// punctuation, cache the new value to be sure.
			(m_cache.MainCacheAccessor as IVwCacheDa).CacheStringProp(hvoAnn, m_tagPunct,
				m_tssPara.GetSubstring(ichMin, ichLim));
			return hvoAnn;
		}

		/// <summary>
		/// Here ichMin..Lim indicates a (possibly empty) range of characters between two words,
		/// or before the first word or after the last. If this range contains anything other than
		/// white space (typically punctuation), make one or more extra annotations for each
		/// group of white-space-separated characters in the range. If possible reuse an existing
		/// punctuation annotation.
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="annotationIds">Append ids of new annotations here.</param>
		private void CreatePunctAnnotations(int ichMin, int ichLim, List<int> annotationIds)
		{
			int ichStart = ichMin;
			bool fPrevIsWhite = true; // for current purpose imagine white space before ich.
			for (int ich = ichMin; ich < ichLim; ich = m_wordMaker.NextChar(ich))
			{
				bool fIsWhite = m_wordMaker.IsWhite(ich);
				// Transition from non-white to white: make an annotation
				if (fIsWhite && !fPrevIsWhite)
					annotationIds.Add(CreatePunctAnnotation(ichStart, ich));
					// Transition from white to non-white: note start of punctuation group.
				else if (!fIsWhite && fPrevIsWhite)
					ichStart = ich;
				fPrevIsWhite = fIsWhite;
			}
			// If last character is non-white, make an annotation for it.
			if(!fPrevIsWhite)
				annotationIds.Add(CreatePunctAnnotation(ichStart, ichLim));
		}

		// Do the actual parsing. m_annotations may be empty, or may contain HVOs of twfic
		// annotations that point at this paragraph and should be resued if possible.  If there
		// are existing twfic annotations, m_wordforms contains the corresponding wordform HVOs.
		// m_segments, if not empty, contains a row for each segment-level annotation on this
		// paragraph, each row having the ID and BeginOffset of the annotation.
		private void Parse()
		{
			LexEntryUi.EnsureFlexVirtuals(Cache, null);
			s_cache = m_cache;
			int hvoPara = m_para.Hvo;
			m_paraRealTwfics = null;
			m_realParagraphTwficAnnotations.TryGetValue(hvoPara, out m_paraRealTwfics);
			if (!m_paraIdsParsed.Contains(hvoPara))
			{
				// Keep track of the paragraphs we've parsed.
				m_paraIdsParsed.Add(hvoPara);
				// load the real ids for this paragraph we'll try to reuse.
				SetupRealIdsForParagraph(hvoPara);
				if (m_paraRealTwfics != null)
					m_unusedTwficAnnotations.AddRange(m_paraRealTwfics);
			}

			ISilDataAccess sda = m_cache.MainCacheAccessor;
			if (m_paraRealTwfics != null)
			{
				// for performance in ReuseRealReuseRealTwficAnnotation:
				// (e.g. List[i] and List.Count can become too expensive).
				List<int> wordforms = m_realParagraphWordforms[hvoPara];
				// Not needed, since Dictionaries will throw an exception, when the key is missing.
				//Debug.Assert(wordforms != null, "we should have real wordforms corresponding to real paragraph annotations.");
				m_paraRealWfIds = wordforms.ToArray();
				m_paraRealAnnIds = m_paraRealTwfics.ToArray();

				// Cache the wordform instance for later use.
				// The following indexing should accelerate matches found ReuseRealTwficAnnotation.
				// Basically we map the lower case form of the annotation's wordform to its index in its paragraph.
				for (int i = 0; i < m_paraRealWfIds.Length; ++i)
				{
					int hvoWordform = m_paraRealWfIds[i];
					ITsString tssWordAnn;
					int wsActual;
					int hvoAnnotation = m_paraRealAnnIds[i];
					int ichBegin = sda.get_IntProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
					if (!TryBestWordform(hvoWordform, hvoPara, ichBegin, out tssWordAnn, out wsActual))
					{
						continue;	// skip this wordform; not sure what writing system to use to get a string.
					}
					// (LT-5856) for phrase annotations, use the first word as a key
					string annFirstWordformLowered;
					ITsString firstWord = FirstWord(tssWordAnn, Cache.LanguageWritingSystemFactoryAccessor, out annFirstWordformLowered);
					if (firstWord != null)
					{
						string key = hvoPara.ToString() + annFirstWordformLowered;
						List<int> possibleIndices = null;
						if (m_wordformAnnotationPossibilities.ContainsKey(key))
							possibleIndices = m_wordformAnnotationPossibilities[key];
						if (possibleIndices == null)
						{
							// create a list of indices for wordform-annotations in this paragraph.
							possibleIndices = new List<int>();
							m_wordformAnnotationPossibilities[key] = possibleIndices;
						}
						possibleIndices.Add(i);
					}
				}
			}
			else
			{
				m_paraRealWfIds = new int[0];
				m_paraRealAnnIds = new int[0];
			}

			// if we have a matcher for the Word line, we only care about collecting twfics for matching wordforms
			if (m_matcher != null && m_line == ConcordanceControl.ConcordanceLines.kWord)
			{
				// simply want to create or reuse annotations that match m_matcher for m_line.
				if (m_matchingAnnotations == null)
					m_matchingAnnotations = new List<int>();
				// needed so some kinds of matcher can figure the writing system.
				m_matcher.WritingSystemFactory = Cache.LanguageWritingSystemFactoryAccessor;
				int wsMatchQuery = m_matcher.WritingSystem;
				do
				{
					int ichMin;
					int ichLim;
					ITsString tssTxtWord = m_wordMaker.NextWord(out ichMin, out ichLim);
					if (tssTxtWord == null)
						break;
					ITsString tssWordAnn;
					int hvoMatchingWordform = 0;
					// Note: this is a trimmed version of CreateOrReuseAnnotation()
					int hvoMatchingAnn = ReuseRealTwficAnnotation(tssTxtWord, ichMin, ichLim, out hvoMatchingWordform, out tssWordAnn);
					ITsString tssWff = GetTssWffCandidate(sda, tssTxtWord, tssWordAnn, hvoMatchingWordform, wsMatchQuery);
					if (tssWff != null && m_matcher.Accept(tssWff))
					{
						if (hvoMatchingAnn == 0)
							hvoMatchingAnn = CreateDummyAnnotation(tssWordAnn, ichMin, ichLim, out hvoMatchingWordform);
						else
						{
							// adjust the offsets for this annotation, if we need to.
							AdjustCbaFields(hvoMatchingAnn, ichMin, tssWordAnn, hvoMatchingWordform);
						}
						m_matchingAnnotations.Add(hvoMatchingAnn);
					}
					if (tssWordAnn != null && tssTxtWord.Length < tssWordAnn.Length)
					{
						// this must be a phrase, so advance appropriately in the text.
						m_wordMaker.CurrentCharOffset = ichMin + tssWordAnn.Length;
					}
					if (m_matchingAnnotations.Count >= ConcordanceControl.MaxConcordanceMatches())
						break;
				} while (true);
				return;
			}

			// Create (or reuse if possible) segment annotations.
			using (new IgnorePropChanged(Cache, PropChangedHandling.SuppressAll))
			{
				List<int> segments = CollectSegmentAnnotations();
				// Cache the segments of the paragraph.
				int[] segmentsArray = segments.ToArray();
				if (kflidParaSegments != 0)
				{
					IVwCacheDa cda = Cache.VwCacheDaAccessor;
					cda.CacheVecProp(m_para.Hvo, kflidParaSegments, segmentsArray, segmentsArray.Length);

					// Build the segment forms for each segment, iff we were given a subfield to cache it into.
					if (kflidSegmentForms != 0)
					{
						int ichLimLast = 0;
						int ichLimCurSeg = Int32.MaxValue;
						ITsString tssFirstWordOfNextSegment = null;
						for (int iseg = 0; iseg < segmentsArray.Length; ++iseg)
						{
							ichLimCurSeg = sda.get_IntProp(segmentsArray[iseg], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
							List<int> formsInSegment = CollectSegmentForms(m_wordMaker.CurrentCharOffset, ichLimCurSeg, ref ichLimLast, ref tssFirstWordOfNextSegment);
							// if any of the forms are real, non-trivial analyses, then make sure its segment is also real.
							EnsureRealSegmentsForNonTrivialAnalyses(segments, segmentsArray, iseg, formsInSegment);
							cda.CacheVecProp(segmentsArray[iseg], kflidSegmentForms, formsInSegment.ToArray(), formsInSegment.Count);
							formsInSegment.Clear();
						}
					}
				}
			}
		}

		/// <summary>
		/// the tss of the wordform form of the given hvoMatchingWordform in the ws we are querying for.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="tssTxtWord">the tss of the baseline at current word boundaries</param>
		/// <param name="tssWordAnn">the tss of the wordform form (in the baseline ws) of the annotation.</param>
		/// <param name="hvoMatchingWordform"></param>
		/// <param name="wsMatchQuery">the ws of the wordform we're looking for</param>
		/// <returns></returns>
		private ITsString GetTssWffCandidate(ISilDataAccess sda, ITsString tssTxtWord, ITsString tssWordAnn,
			int hvoMatchingWordform, int wsMatchQuery)
		{
			ITsString tssWff = tssWordAnn;
			int wsTxtWord = StringUtils.GetWsAtOffset(tssTxtWord, 0);
			if (hvoMatchingWordform == 0)
			{
				// return a candidate if it matches the ws we're trying to query.
				return wsMatchQuery == wsTxtWord ? tssWordAnn : null;
			}
			// if the ws of the matcher doesn't match the ws of the baseline
			// find the wordform in an alternative ws.
			if (wsMatchQuery != wsTxtWord)
			{
				try
				{
					// bulk load wordform forms in the alternative ws if we get any misses.
					m_cache.EnableBulkLoadingIfPossible(true);
					tssWff = sda.get_MultiStringAlt(hvoMatchingWordform, (int)WfiWordform.WfiWordformTags.kflidForm, wsMatchQuery);
				}
				finally
				{
					m_cache.EnableBulkLoadingIfPossible(false);
				}
			}
			return tssWff;
		}

		protected virtual void EnsureRealSegmentsForNonTrivialAnalyses(List<int> segments, int[] segmentsArray, int iseg, List<int> formsInSegment)
		{
			if (m_cache.IsDummyObject(segmentsArray[iseg]) && HasNonTrivialAnalysis(formsInSegment))
			{
				using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(Cache, true))
				{
					// replace the dummy segment with a real one.
					segmentsArray[iseg] = CmBaseAnnotation.ConvertBaseAnnotationToReal(Cache, segmentsArray[iseg]).Hvo;
					segments.RemoveAt(iseg);
					segments.Insert(iseg, segmentsArray[iseg]);
				}
			}
		}

		private List<int> CollectSegmentAnnotations()
		{
			ITsString paraContents = m_para.Contents.UnderlyingTsString;
			// track the beginning and ending offsets for segment breaks;
			List<int> ichMinSegBreaks;
			return CollectSegmentAnnotations(paraContents, out ichMinSegBreaks);
		}


		/// <summary>
		/// This routine is responsible for breaking a paragraph into segments. This is primarily done by looking for
		/// 'EOS' (end-of-segment) characters, which are various characters that usually end sentences, plus a special one
		/// which the user can insert into the text to force smaller segments. Also, we make separate segments for chapter
		/// and verse numbers (or whatever 'IsLabelText' identifies as labels).
		///
		/// Although we typically end a segment when we find an EOS character, things are actually a bit more complex.
		/// There may be various punctuation following the EOS character, such as quotes and parentheses. We don't actually make
		/// a segment break unless we find some word-forming characters (or label text) after the EOS, so the last segment
		/// can include any amount of trailing non-letter data. There might also be a good deal of non-letter data between
		/// the EOS and the following letter. The current algorithm, partly because numbers are likely to be labels of what
		/// follows and belong with it, is that (once we find a letter and decide to make a following segment) the break is
		/// at the end of the first run of white space following the EOS. If there is no white space, the segment break is
		/// right at the first letter that follows the EOS.
		///
		/// For label text, the segment break is always exactly at the start of a run that has the label style. White space
		/// following a label run is included in its segment, and multiple label-style runs (possibly separated by white
		/// space and including following white space) are merged into a single segment.
		///
		/// The algorithm also returns, for each segment except possibly the last, the character index of the first EOS
		/// character in the segment (or, for label segments or segments that end because of a label rather than an EOS
		/// character, the index of the character following the segment). This is helpful in adjusting segment boundaries
		/// because material inserted into a segment before the EOS is less likely to change the way the segments break
		/// up (unless of course it includes an EOS).
		/// </summary>
		/// <param name="tssText"></param>
		/// <param name="ichMinSegBreaks"></param>
		/// <returns></returns>
		internal List<int> CollectSegmentAnnotations(ITsString tssText, out List<int> ichMinSegBreaks)
		{
			SegmentMaker collector = new SegmentMaker(tssText, m_cache.LanguageWritingSystemFactoryAccessor, this);
			collector.Run();
			ichMinSegBreaks = collector.EosPositions;
			return collector.Segments;
		}

		/// <summary>
		/// This is very similar to CollectSegmentAnnotations on the base class, but does not make
		/// even dummy annotations, just TsStringSegments.
		/// </summary>
		/// <param name="tssText"></param>
		/// <param name="ichMinSegBreaks"></param>
		/// <returns></returns>
		internal List<TsStringSegment> CollectTempSegmentAnnotations(ITsString tssText, out List<int> ichMinSegBreaks)
		{
			SegmentCollector collector = new SegmentCollector(tssText, m_cache.LanguageWritingSystemFactoryAccessor);
			collector.Run();
			ichMinSegBreaks = collector.EosPositions;
			return collector.Segments;
		}

		/// <summary>
		/// Collect existing segments, if possible reusing existing ones, for the paragraph passed to the constructor.
		/// </summary>
		internal List<int> CollectSegmentAnnotationsOfPara(out List<int> ichMinSegBreaks)
		{
			Debug.Assert(m_para != null);
			// Get the information we need to reuse existing annotations if possible.
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			List<int> cachedSegments = new List<int>();
			if (m_cache.MainCacheAccessor.get_IsPropInCache(m_para.Hvo, kflidSegments, (int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				// We already know some segments for this paragraph...may have important BT information attached...reuse
				// for now even if not real.
				cachedSegments.AddRange(m_cache.GetVectorProperty(m_para.Hvo, kflidSegments, true));
			}
			// There may be real segments already in the database that are not in the cache (e.g., from merging paragraphs),
			// or we may not have previously loaded the cache version of the property at all. This routine
			ReadAnnotationInfo(m_cache.VwCacheDaAccessor, TextSegmentDefn, m_realParagraphSegmentAnnotations);
			List<int> paraSegments;
			m_realParagraphSegmentAnnotations.TryGetValue(m_para.Hvo, out paraSegments);
			if (paraSegments != null)
				m_unusedSegmentAnnotations.AddRange(paraSegments);
			else
				paraSegments = new List<int>();
			// Any items already associated in memory but not found by ReadAnnotationInfo?
			m_unusedSegmentAnnotations.AddRange(cachedSegments);
			List<int> combinedSegs = CombineSegs(cachedSegments, paraSegments);
			m_realParagraphSegmentAnnotations[m_para.Hvo] = combinedSegs;

			return CollectSegmentAnnotations(m_para.Contents.UnderlyingTsString, out ichMinSegBreaks);
		}

		/// <summary>
		/// Return a list of the combined segments in the two lists, ordered by BeginOffset.
		/// </summary>
		/// <param name="cachedSegments"></param>
		/// <param name="paraSegments"></param>
		/// <returns></returns>
		private List<int> CombineSegs(List<int> first, List<int> second)
		{
			List<int> result = new List<int>(first.Count + second.Count);
			int index1 = 0;
			int index2 = 0;
			while (index1 < first.Count && index2 < second.Count)
			{
				if (first[index1] == second[index2])
				{
					// same segment in both...transfer
					result.Add(first[index1]);
					index1++;
					index2++;
					continue;
				}
				int offset1 = m_cache.GetIntProperty(first[index1], (int) CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
				int offset2 = m_cache.GetIntProperty(second[index2], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
				if (offset1 < offset2)
				{
					result.Add(first[index1]);
					index1++;
				}
				else
				{
					result.Add(second[index2]);
					index2++;
				}
			}
			if (index1 < first.Count)
				result.AddRange(first.GetRange(index1, first.Count - index1));
			else if (index2 < second.Count)
				result.AddRange(second.GetRange(index2, second.Count - index2));
			return result;
		}

		/// <summary>
		/// load the real ids for this paragraph we'll try to reuse.
		/// </summary>
		/// <param name="hvoPara"></param>
		private void SetupRealIdsForParagraph(int hvoPara)
		{
			List<int> paraSegments = null;
			if (m_realParagraphSegmentAnnotations.TryGetValue(hvoPara, out paraSegments))
				m_unusedSegmentAnnotations.AddRange(paraSegments);
			List<int> paraPunctuations = null;
			if (m_realParagraphPunctuationAnnotations.TryGetValue(hvoPara, out paraPunctuations))
				m_unusedPunctuationAnnotations.AddRange(paraPunctuations);
		}

		/// <summary>
		/// Returns the first word in the given tssWordAnn and its lower case form.
		/// </summary>
		/// <param name="tssWordAnn"></param>
		/// <param name="cpe"></param>
		/// <param name="firstFormLowered"></param>
		/// <returns>null if we couldn't find a word in the given tssWordAnn</returns>
		static private ITsString FirstWord(ITsString tssWordAnn, ILgWritingSystemFactory wsf, out string firstFormLowered)
		{
			WordMaker wordScanner = new WordMaker(tssWordAnn, wsf);
			int ichMinFirstWord;
			int ichLimFirstWord;
			ITsString firstWord = wordScanner.NextWord(out ichMinFirstWord, out ichLimFirstWord);
			// Handle null values without crashing.  See LT-6309 for how this can happen.
			if (firstWord != null)
				firstFormLowered = wordScanner.ToLower(firstWord);
			else
				firstFormLowered = null;
			return firstWord;
		}

		/// <summary>
		/// Identifies whether the given tssWordAnn is a phrase (ie. contains multiple forms that count as 'words').
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssWordAnn"></param>
		/// <returns></returns>
		static internal bool IsPhrase(FdoCache cache, ITsString tssWordAnn)
		{
			string firstFormLowered;
			ITsString firstWord = FirstWord(tssWordAnn,
				cache.LanguageWritingSystemFactoryAccessor, out firstFormLowered);
			// Handle null values without crashing.  See LT-6309 for how this can happen.
			return firstWord != null && firstWord.Length < tssWordAnn.Length;
		}

		/// <summary>
		/// Breaks the given (phrase-wordform) annotation to its basic wordforms.
		/// </summary>
		/// <param name="hvoCbaPhrase"></param>
		/// <param name="hvoParasInView">the paragraphs in the view doing the break phrase operation</param>
		/// <returns>ids of the new wordforms</returns>
		public List<int> BreakPhraseAnnotation(int hvoCbaPhrase, int[] hvoParasInView)
		{
			List<int> segForms;
			ICmBaseAnnotation phraseAnnotation = CmBaseAnnotation.CreateFromDBObject(Cache, hvoCbaPhrase);
			int phraseBeginOffset = phraseAnnotation.BeginOffset;
			int phraseEndOffset = phraseAnnotation.EndOffset;
			segForms = CollectSegmentForms(phraseBeginOffset, phraseEndOffset, false);
			StTxtPara.TwficInfo phraseInfo = new StTxtPara.TwficInfo(Cache, hvoCbaPhrase);
			int iSegForm = phraseInfo.SegmentFormIndex;
			int hvoParaSeg = phraseInfo.SegmentHvo;
			IVwCacheDa cda = Cache.VwCacheDaAccessor;
			ISilDataAccess sda = Cache.MainCacheAccessor;
			// convert dummy segForms to real ones.
			// This conversion is currently important to do before we exit BreakPhraseAnnotation because if we try to
			// convert a dummy annotation that references a dummy wordform, that dummy wordform
			// may be removed from the WordformInventory by DeleteUnderlyingObject()->OnChangedWordformsOC below.
			List<int> realSegForms = new List<int>(segForms.Count);
			foreach (int hvoSegForm in segForms)
			{
				int hvoNewWordform;
				WfiWordform.TryGetWfiWordformFromInstanceOf(Cache, hvoSegForm, out hvoNewWordform);
				if (hvoNewWordform != 0)
				{
					// This allows it to be converted to real, by specifying its owner and owning flid.
					WfiWordform.AddDummyAnnotation(Cache, hvoNewWordform, hvoSegForm);
					ICmBaseAnnotation realSegForm = CmObject.ConvertDummyToReal(Cache, hvoSegForm) as ICmBaseAnnotation;
					realSegForms.Add(realSegForm.Hvo);
					WfiWordform newWordform = WfiWordform.CreateFromDBObject(Cache, hvoNewWordform) as WfiWordform;
					// now add this occurrence to the wordform.
					newWordform.TryAddOccurrence(realSegForm.Hvo);
				}
				else
				{
					realSegForms.Add(hvoSegForm); // punctuation does not need to be real.
				}
			}
			// insert new forms.
			CacheReplaceOneUndoAction cacheReplaceSegmentsFormsAction = new CacheReplaceOneUndoAction(Cache, hvoParaSeg,
				kflidSegmentForms, iSegForm, iSegForm, realSegForms.ToArray());
			cacheReplaceSegmentsFormsAction.DoIt();
			if (Cache.ActionHandlerAccessor != null)
			{
				Cache.ActionHandlerAccessor.AddAction(cacheReplaceSegmentsFormsAction);
			}
			// Get the phrase wordform form the phraseAnnotation.
			int hvoOldWordform = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoCbaPhrase);
			// delete old annotation. (In the future, this may be orphaned so that it may be reused.)
			WfiWordform wffOld = WfiWordform.CreateFromDBObject(Cache, hvoOldWordform, false) as WfiWordform;
			phraseAnnotation.DeleteUnderlyingObject();
			// see if we can delete the phrase wordform from our wordinventory.
			Set<int> delObjIds;
			StTxtPara.TryDeleteWordforms(Cache, new int[] { wffOld.Hvo }, hvoParasInView, out delObjIds);
			return realSegForms;
		}

		/// <summary>
		/// Collects SegmentForms between ichMinCurSeg to ichLimCurSeg.
		/// </summary>
		/// <param name="ichMinCurSeg"></param>
		/// <param name="ichLimCurSeg"></param>
		/// <param name="fUpdateRealData">if false, ParagraphParser only creates dummy annotations for the segment,
		/// but doesn't modify any real annotation or change the state of ParagraphParser.</param>
		/// <returns></returns>
		internal List<int> CollectSegmentForms(int ichMinCurSeg, int ichLimCurSeg, bool fUpdateRealData)
		{
			// if we don't want to modify real data, then put ParagraphParser in SegmentFormCollectionMode.
			SegmentFormCollectionMode = !fUpdateRealData;
			// Save current state of ParagraphParser.
			int originalParagraphOffset = m_wordMaker.CurrentCharOffset;
			int cUnusedRealAnn = m_unusedTwficAnnotations.Count +
				m_unusedSegmentAnnotations.Count +
				m_unusedPunctuationAnnotations.Count;

			try
			{
				int ichLimLast = ichMinCurSeg;
				ITsString tssFirstWordOfNextSegment = null;
				return CollectSegmentForms(ichMinCurSeg, ichLimCurSeg, ref ichLimLast, ref tssFirstWordOfNextSegment);
			}
			finally
			{
				if (SegmentFormCollectionMode)
				{
					// Restore state of ParagraphParser.
					m_wordMaker.CurrentCharOffset = originalParagraphOffset;
					Debug.Assert(cUnusedRealAnn == m_unusedTwficAnnotations.Count +
						m_unusedSegmentAnnotations.Count +
						m_unusedPunctuationAnnotations.Count,
						"CollectSegmentForms should not change number of unused real ids.");

					SegmentFormCollectionMode = false;
				}
			}
		}

		/// <summary>
		/// Collects the SegmentForms in a paragraph phrase marked by ichMinCurSeg and ichLimCurSeg,
		/// without changing the current state of the ParagraphParser.
		/// (e.g. We will try to match real forms but won't actually 'UseId' or change the state of paragraph or wordform annotations.
		/// Also, we will return m_wordMaker back to its original state.)
		/// </summary>
		/// <param name="ichMinCurPhrase">beginning of the phrase in the paragraph.</param>
		/// <param name="ichLimCurPhrase">ending of the phrase in the paragraph.</param>
		/// <param name="phraseSegmentForms">SegmentForms that match in this paragraph.</param>
		/// <param name="iFirstSegmentFormWithNonTrivialAnalyis">index of the first segmentForm with a real analysis in phraseSegmentForms.
		/// -1 if they're all wordform analyses.</param>
		/// <returns>true, if phraseSegmentForms contains an annotation with a significant analysis (i.e. other than wordform).</returns>
		internal bool TryRealSegmentFormsInPhrase(int ichMinCurPhrase, int ichLimCurPhrase, out List<int> phraseSegmentForms,
			out int iFirstSegmentFormWithNonTrivialAnalyis)
		{
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			phraseSegmentForms = CollectSegmentForms(ichMinCurPhrase, ichLimCurPhrase, false);
			// search for the first non-wordform analyses.
			return HasNonTrivialAnalysis(phraseSegmentForms, out iFirstSegmentFormWithNonTrivialAnalyis);
		}

		private bool HasNonTrivialAnalysis(List<int> segmentForms)
		{
			int iFirstSegmentFormWithNonTrivialAnalyis = -1;
			return HasNonTrivialAnalysis(segmentForms, out iFirstSegmentFormWithNonTrivialAnalyis);
		}

		/// <summary>
		/// Find the first
		/// </summary>
		/// <param name="segmentForms"></param>
		/// <returns></returns>
		private bool HasNonTrivialAnalysis(List<int> segmentForms, out int iFirstSegmentFormWithNonTrivialAnalyis)
		{
			iFirstSegmentFormWithNonTrivialAnalyis = -1;
			int iSegForm = 0;
			foreach (int hvoSegForm in segmentForms)
			{
				if (!HasTrivialAnalysis(hvoSegForm))
				{
					iFirstSegmentFormWithNonTrivialAnalyis = iSegForm;
					break;
				}
				iSegForm++;
			}
			return iFirstSegmentFormWithNonTrivialAnalyis != -1;
		}

		/// <summary>
		/// Enabling this will put the ParagraphParser in a 'real only' mode
		/// used to collect/create dummy annotations describing the given text.
		/// No real annotation
		/// </summary>
		bool SegmentFormCollectionMode
		{
			get { return m_fSegmentFormCollectionMode; }
			set { m_fSegmentFormCollectionMode = value; }
		}


		private List<int> CollectSegmentForms(int ichMinCurSeg, int ichLimCurSeg, ref int ichLimLast,
			ref ITsString tssFirstWordOfNextSegment)
		{
			List<int> formsInSegment = new List<int>();
			List<int> punctuationAnnotations = new List<int>();
			int ichMin, ichLim;
			m_wordMaker.CurrentCharOffset = ichMinCurSeg;
			ITsString tssWord;
			if (tssFirstWordOfNextSegment != null)
			{
				tssWord = tssFirstWordOfNextSegment;
				// back up by this word.
				ichLim = ichMinCurSeg;
				ichMin = ichLim - tssWord.Length;
			}
			else
			{
				tssWord = m_wordMaker.NextWord(out ichMin, out ichLim);
			}
			int ctwfics = 0;
			do
			{
				if (tssWord == null)
				{
					// we've run out of twfics. collect the last remaining punctuation annotations.
					//Debug.Assert(m_tssPara.Length == ichLimCurSeg);
					CreatePunctAnnotations(ichLimLast, ichLimCurSeg, punctuationAnnotations);
					break;
				}

				if (ichLimLast != ichMin)
				{
					// we need to add punctuations to the current segment.
					CreatePunctAnnotations(ichLimLast, Math.Min(ichMin, ichLimCurSeg), punctuationAnnotations);
					formsInSegment.AddRange(punctuationAnnotations);
					punctuationAnnotations.Clear();
					if (ichMin >= ichLimCurSeg)
					{
						// we need to add this twfic to the next segment.
						tssFirstWordOfNextSegment = tssWord;
						ichLimLast = ichLimCurSeg;
						break;
					}
				}
				// Create (or reuse if possible) twfic annotations.
				ITsString tssWordAnn;
				formsInSegment.Add(CreateOrReuseAnnotation(tssWord, ichMin, ichLim, ctwfics, out tssWordAnn));
				if (tssWordAnn != null && tssWord.Length < tssWordAnn.Length)
				{
					// this must be a phrase, so advance appropriately in the text.
					ichLimLast = ichMin + tssWordAnn.Length;
					m_wordMaker.CurrentCharOffset = ichLimLast;
				}
				else
				{
					// still stepping by the word boundary.
					ichLimLast = ichLim;
				}
				ctwfics++;
				tssWord = m_wordMaker.NextWord(out ichMin, out ichLim);
			} while (true);
			formsInSegment.AddRange(punctuationAnnotations);
			return formsInSegment;
		}

		internal int TextSegmentDefn
		{
			get
			{
				CheckDisposed();

				return InterlinVc.GetAnnDefnId(m_cache, LangProject.kguidAnnTextSegment, ref m_hvoAnnDefTextSeg);
			}
		}

		// Don't call this yet! Most databases don't yet have any such annotation defn.
		internal int TwficDefn
		{
			get
			{
				CheckDisposed();

				return InterlinVc.GetAnnDefnId(m_cache, LangProject.kguidAnnWordformInContext,
					ref m_hvoAnnDefnTwfic);
			}
		}

		int GetParagraph(int ihvoPara)
		{
			if (m_hvoText == 0 || !(ihvoPara < m_cparas))
				return 0;
			return m_cache.MainCacheAccessor.get_VecItem(m_hvoText,
					(int)StText.StTextTags.kflidParagraphs, ihvoPara);
		}
		int GetNextParagraph(int ihvoPara)
		{
			return GetParagraph(ihvoPara + 1);
		}

		internal int CreateSegment(int ichMin, int ichLim)
		{
			int hvoAnnotation = 0;
			CmBaseAnnotation cbaFirstUnused;
			List<int> annotations;
			int iann;
			if (TryReuseFirstUnusedCbaMatchingText(m_realParagraphSegmentAnnotations, m_unusedSegmentAnnotations,
				ichMin, ichLim, out cbaFirstUnused, out annotations, out iann))
			{
				return cbaFirstUnused.Hvo;
			}
			if (cbaFirstUnused != null)
			{
				if (cbaFirstUnused.BeginOffset > ichMin && cbaFirstUnused.EndOffset > ichLim)
				{
					// this annotation (and subsequent) should be (re)used later in the text.
					// so, keep cbaFirstUnused unused for later.
					hvoAnnotation = 0;
				}
				else
				{
					// Somehow our text position has gotten ahead of our first unused real annotation
					// probably because the user edited the text, and AnnotatedTextEditingHelper couldn't quite
					// adjust things perfectly.
					// So, just reuse and readjust the segment here and hope for the best.
					hvoAnnotation = cbaFirstUnused.Hvo;
					UseId(iann, annotations, m_unusedSegmentAnnotations);
				}
			}

			if (hvoAnnotation == 0)
			{
				// We will make a dummy segment annotation. Either there are no more to reuse, or the next
				// one to use belongs to a later paragraph.
				//this.CreateAnnotation(0, ichMin, ichLim, TextSegmentDefn);
				if (m_fCreateRealSegments)
				{
					hvoAnnotation = CmBaseAnnotation.CreateRealAnnotation(m_cache, TextSegmentDefn, 0, m_para.Hvo,
						(int)StTxtPara.StTxtParaTags.kflidContents, ichMin, ichLim).Hvo;
				}
				else
				{
					hvoAnnotation = CreateDummyAnnotation(0, ichMin, ichLim, TextSegmentDefn);
				}
			}
			else
			{
				// Reusing...just set a few fields.
				AdjustCbaFields(hvoAnnotation, ichMin, ichLim);
			}
			return hvoAnnotation;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="realParagraphAnnotations"></param>
		/// <param name="unusedAnnotations"></param>
		/// <param name="ichMin">begin offset in actual text</param>
		/// <param name="ichLim">end offset in actual text</param>
		/// <param name="cbaFirstUnused">the first unused cba, whether or not we could reuse it.</param>
		/// <returns>true if we reused it.</returns>
		protected bool TryReuseFirstUnusedCbaMatchingText(Dictionary<int, List<int>> realParagraphAnnotations, Set<int> unusedAnnotations, int ichMin, int ichLim,
			out CmBaseAnnotation cbaFirstUnused, out List<int> annotations, out int iann)
		{
			cbaFirstUnused = null;
			annotations = null;
			iann = -1;
			if (realParagraphAnnotations.TryGetValue(m_para.Hvo, out annotations))
			{
				iann = IndexOfFirstUnusedId(annotations);
				if (iann >= annotations.Count)
					return false;
				// we need to assume everything we will be accessing has already been cached
				// NOTE: for performance purposes, class id has not been cached, so we need to explicitly
				// tell the CmObject constructor not to validate or load data for this object, otherwise
				// otherwise it will try to load data for each annotation, and that takes too much time! (LT-8038)
				cbaFirstUnused = new CmBaseAnnotation(m_cache, annotations[iann], false, false);
				if (cbaFirstUnused.BeginOffset == ichMin && cbaFirstUnused.EndOffset == ichLim)
				{
					// Reuse this annotation
					UseId(iann, annotations, unusedAnnotations);
					return true;
				}
			}
			return false;
		}

		protected virtual int ReuseFirstUnusedAnnotation(Dictionary<int, List<int>> realParagraphAnnotations, Set<int> unusedAnnotations,
			int ichMin, int ichLim, int hvoAnnType)
		{
			// see if we can reuse an annotation from the current paragraph.
			int hvoAnnotation = ReuseFirstUnusedAnnotationForParagraph(realParagraphAnnotations, unusedAnnotations,
				m_para.Hvo);
			if (hvoAnnotation == 0)
			{
				// see if we can reuse an orphaned annotation.
				hvoAnnotation = ReuseFirstUnusedAnnotationForParagraph(realParagraphAnnotations, null,
					CmBaseAnnotation.kHvoBeginObjectOrphanage);
			}

			// Next Try to reuse the first unused annotation from a previously parsed paragraph.
			if (hvoAnnotation == 0)
			{
				hvoAnnotation = ReuseFirstUnusedAnnotationFromPreviouslyParsedParagraph(
					realParagraphAnnotations, unusedAnnotations);
			}

			// We could try to reuse orphan Annotation here, but it would probably be
			// better for performance and concordance to reserve these for twfics use.
			//if (m_orphanedAnnotations.Count > 0)
			//{
			//    ReuseOrphanedAnnotation(ichMin, ichLim, 0, hvoAnnType);
			//}
			return hvoAnnotation;
		}

		private int ReuseFirstUnusedAnnotationForParagraph(Dictionary<int, List<int>> realParagraphAnnotations, Set<int> unusedAnnotations,
			int hvoPara)
		{
			int hvoAnnotation = 0; // default to not found.
			int iseg = -1;
			List<int> annotations = null;
			// First try to find an annotation to reuse in the current paragraph.
			if (realParagraphAnnotations.TryGetValue(hvoPara, out annotations))
			{
				iseg = IndexOfFirstUnusedId(annotations);
				if (iseg < annotations.Count)
				{
					// Reuse this annotation
					hvoAnnotation = annotations[iseg];
					UseId(iseg, annotations, unusedAnnotations);
				}
			}
			return hvoAnnotation;
		}

		private int ReuseFirstUnusedAnnotationFromPreviouslyParsedParagraph(
			Dictionary<int, List<int>> realParagraphAnnotations, Set<int> unusedAnnotations)
		{
			int hvoAnnotation = 0;
			int iseg = -1;
			List<int> annotations = null;
			if (unusedAnnotations.Count == 0)
				return 0;

			int firstUnusedAnnId = unusedAnnotations.ToArray()[0];
			int hvoBeginObject = Cache.MainCacheAccessor.get_ObjectProp(firstUnusedAnnId,
							(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			// reuse the first unused annotation that doesn't belong to our current paragraph.
			if (hvoBeginObject != m_para.Hvo)
			{
				annotations = realParagraphAnnotations[hvoBeginObject];
				iseg = IndexOfFirstUnusedId(annotations);
				// This seems to be covered in the code. For some reason we are getting lists
				// in reverse order which cause this assertion. This was discovered when
				// switching to Words after a LinguaLinks import from Tagakaulo.
				// Debug.Assert(annotations[iseg] == firstUnusedAnnId);
				hvoAnnotation = annotations[iseg];
				UseId(iseg, annotations, unusedAnnotations);
			}

			return hvoAnnotation;
		}

		// Verify that the annotation matches the offsets. They should have been cached in BuildAnalysisList();
		//
		bool HasValidOffsets(int hvoAnnotation, int ichMin, int ichLim)
		{
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			Debug.Assert(sda.get_IsPropInCache(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset,
				(int)CellarModuleDefns.kcptInteger, 0), "We expect BuildAnalysisList() to cache the annotation offsets.");
			return ichMin == sda.get_IntProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset) &&
				ichLim == sda.get_IntProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset) &&
				m_para.Hvo == sda.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
		}

		static protected int IndexOfFirstUnusedId(List<int> ids)
		{
			Debug.Assert(ids != null);
			return IndexOfFirstUnusedId(ids.ToArray());
		}

		static int IndexOfFirstUnusedId(int[] ids)
		{
			int i = 0;
			for (; i < ids.Length && (int)ids[i] == 0; ++i) ;
			return i;
		}

		protected int UseId(int index, List<int> ids, Set<int> unusedIds)
		{
			Debug.Assert(index >= 0 && index < ids.Count);
			Debug.Assert(unusedIds == null || unusedIds.Contains(ids[index]));
			int hvoAnnotation = ids[index];
			if (!SegmentFormCollectionMode)
			{
				if (unusedIds != null)
					unusedIds.Remove(hvoAnnotation);
				ids[index] = 0;
			}
			return hvoAnnotation;
		}

		int ReuseRealTwficAnnotation(ITsString tssTxtWord, int ichMin, int ichLim, out int hvoWf, out ITsString tssWordAnn)
		{
			tssWordAnn = tssTxtWord;
			hvoWf = 0;	// default
			int cWfIds = m_paraRealWfIds.Length;
			int cAnnIds = m_paraRealAnnIds.Length;
			if (cWfIds == 0)
			{
				// Enhance: This is a new paragraph that may have resulted in breaking
				// up a previously existing paragraph. In that case we'd probably want to
				// try to preserve the annotations/analyses.
				return 0;
			}
			Debug.Assert(cAnnIds != 0);

			// First, see if we have cached annotation ids for the wordform in this paragraph.
			ISilDataAccess sda = Cache.MainCacheAccessor;
			// the wordform and its lowercase form may already be in the cache.
			string key = m_para.Hvo.ToString() +
				TruncateOverlongWordform(m_paragraphTextScanner.ToLower(tssTxtWord));
			List<int> possibleIndices = null;
			if (m_wordformAnnotationPossibilities.ContainsKey(key))
				possibleIndices = m_wordformAnnotationPossibilities[key];
			if (possibleIndices == null || possibleIndices.Count == 0)
			{
				// we don't have any remaining annotations matching this wordform.
				return 0;
			}

			int ichMinAnnClosest = -1;
			int iAnnClosest = -1;
			GetBestPossibleAnnotation(ichMin, possibleIndices, out ichMinAnnClosest, out iAnnClosest);
			if (ichMinAnnClosest == -1 || iAnnClosest == -1)
				return 0;	// this shouldn't happen, but just in case.

			bool fUsedBestPossible = false;
			try
			{
				hvoWf = m_paraRealWfIds[iAnnClosest];
				Debug.Assert(hvoWf != 0, "real wordform hvo should not be 0.");

				int wsTxtWord = StringUtils.GetWsAtOffset(tssTxtWord, 0);
				tssWordAnn = sda.get_MultiStringAlt(hvoWf, (int)WfiWordform.WfiWordformTags.kflidForm, wsTxtWord);
				if (ichMin != ichMinAnnClosest)
				{
					// Look for an existing occurrence of the wordform in the same paragraph.
					// Enhance: If the character offsets in this text paragraph overlap
					// the user probably deleted a paragraph break.
					// in that case we could be smarter about looking  for matches at the end of the paragraph
					// rather than at the beginning.

					// Verify the closest is within reasonable bounds.
					if (Math.Abs(ichMin - ichMinAnnClosest) > 1000)
					{
						// someone may have significantly altered the text,
						// or it belongs to a wordform later in the text.
						// Either case, safe not to guess further.
						tssWordAnn = tssTxtWord;
						return 0;
					}

					if (tssWordAnn.Length == 0)
					{
						// There are certain cases (e.g. during import) where
						// the paragraph text will not have a default vernacular writing system for the current
						// wordform. So, instead of trying to find the best vernacular form (again), we'll
						// just assume that it matches some form of the tssWordTxt in this context.
						tssWordAnn = tssTxtWord;
					}

					// see if we can match on the target offset, so we don't have to search more through the text.
					if (m_paragraphTextScanner.MatchesWordInText(tssWordAnn, ichMinAnnClosest))
					{
						tssWordAnn = tssTxtWord;
						return 0;
					}
					// Otherwise, verify there isn't another place in the text closer to the
					// offsets for the annotation's wordform.
					int nextBeginOffset = -1;
					m_paragraphTextScanner.NextOccurrenceOfWord(
						tssWordAnn,
						ichLim,
						ichMinAnnClosest,
						out nextBeginOffset);
					if (nextBeginOffset != -1)
					{
						// we found a closer possible occurrence.
						tssWordAnn = tssTxtWord;
						return 0;
					}
					else if (ichMin < ichMinAnnClosest)
					{
						// we didn't find an occurrence between ichMin and ichMinAnnClosest, but
						// it's possible that the next occurrence of the word comes after the annotation's offset.
						m_paragraphTextScanner.NextOccurrenceOfWord(
							tssWordAnn,
							ichMinAnnClosest + tssWordAnn.Length,
							m_tssPara.Length,
							out nextBeginOffset);
						// if the current place in the text is not closer to our annotation's offset, we won't link them.
						if (nextBeginOffset != -1 &&
							Math.Abs(ichMinAnnClosest - ichMin) > Math.Abs(ichMinAnnClosest - nextBeginOffset))
						{
							tssWordAnn = tssTxtWord;
							return 0;
						}
					}
					else
					{
						// ichMin > ichMinAnnClosest, so we're the closest matching wordform since
						// we've already matched or created annotations for previous words in the text.
					}
				}
				else
				{
					int ichLimAnnClosest = sda.get_IntProp(m_paraRealAnnIds[iAnnClosest], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
					if (ichLimAnnClosest > ichLim)
					{
						// annotation must be for a phrase. See if it matches our text before using the id, otherwise we can crash (LT-6244).
						if (!m_paragraphTextScanner.MatchesWordInText(tssWordAnn, ichMinAnnClosest))
						{
							// annotation didn't actually match our text, so return.
							tssWordAnn = tssTxtWord;
							return 0;
						}
					}
					else
					{
						// verify tssWordAnn is actual compatible with our spot in the text.
						if (!m_paragraphTextScanner.MatchesWord(tssWordAnn, tssTxtWord))
						{
							// perhaps they changed the writing system on this word?
							// in any case, we don't want to use it.
							tssWordAnn = tssTxtWord;
							return 0;
						}
					}
				}
				// must have found an annotation we can reuse.
				fUsedBestPossible = true;
				return UseId(iAnnClosest, m_paraRealTwfics, m_unusedTwficAnnotations); // we won't reuse this twfic annotation again.
			}
			finally
			{
				// remove any annotation possibilities that no longer make sense.
				if (!SegmentFormCollectionMode)
				{
					int iBestPossible = possibleIndices.IndexOf(iAnnClosest);
					int cRemove = iBestPossible + (fUsedBestPossible ? 1 : 0);
					if (cRemove > 0)
						possibleIndices.RemoveRange(0, cRemove);

					if (possibleIndices.Count == 0)
					{
						// remove its key from the Dictionary.
						m_wordformAnnotationPossibilities.Remove(key);
					}
				}
			}
		}

		/// <summary>
		/// Guard against ridiculously long words so that we don't crash in the database.
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		private string TruncateOverlongWordform(string form)
		{
			int cchMax = m_cache.MaxFieldLength((int)WfiWordform.WfiWordformTags.kflidForm);
			if (!String.IsNullOrEmpty(form) && form.Length > cchMax)
				return form.Substring(0, cchMax);
			else
				return form;
		}

		private void GetBestPossibleAnnotation(int ichMin, List<int> possibleIndices, out int ichMinAnnClosest, out int iAnnClosest)
		{
			ichMinAnnClosest = -1;
			iAnnClosest = -1;
			ISilDataAccess sda = Cache.MainCacheAccessor;
			foreach (int i in possibleIndices)
			{
				// find a wordform occurrence analysis in the right paragraph that matches the current wordform in our text.
				// Best case: if the text offset matches the annotation offset, we found a match.
				int annBeginOffset = sda.get_IntProp(m_paraRealAnnIds[i], (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
				if (ichMin == annBeginOffset)
				{
					// exact match.
					ichMinAnnClosest = annBeginOffset;
					iAnnClosest = i;
					break;
				}
				if (ichMin > annBeginOffset)
				{
					// either someone 1) inserted some text, or 2) someone has deleted an earlier occurrence of this wordform.
					// if we find another annotation that has a closer offset, in the first case, we might accidentally
					// assign an analysis that belongs to another later in the text, to an earlier one.
					// In the second case, we might accidentally reassign the deleted wordform's analysis to a subsequent
					// matching wordform.

					// for now, we'll just try to find an annotation that has a closer offset match.
					ichMinAnnClosest = annBeginOffset;
					iAnnClosest = i;
					continue;
				}
				else
				{
					// someone may have deleted a significant amount of text
					// or this annotation belongs to another occurrence of the wordform later
					// in the text.
					if (ichMinAnnClosest == -1 ||
						Math.Abs(ichMin - annBeginOffset) < Math.Abs(ichMin - ichMinAnnClosest))
					{
						ichMinAnnClosest = annBeginOffset;
						iAnnClosest = i;
					}
					break;	// other annotations are further away so quit.
				}
			}
			// we should have found at least one best annotation to use.
			Debug.Assert(ichMinAnnClosest != -1 && iAnnClosest != -1);
		}


		private void CacheRealForm(int hvoAnnotation, int ichMin, ITsString tssWordText, ITsString tssWordAnn)
		{
			// Handle null values without crashing.  See LT-6309 for how this can happen.
			string sWordAnn = tssWordAnn.Text;
			if (sWordAnn == null)
				sWordAnn = "";
			if (hvoAnnotation != 0 && !sWordAnn.StartsWith(tssWordText.Text))
			{
				ITsString tssRealForm;
				if (tssWordText.Length < tssWordAnn.Length)
				{
					// this must be a phrase, so extract the exact phrase form from the text.
					tssRealForm = m_para.Contents.UnderlyingTsString.GetSubstring(ichMin, ichMin + tssWordAnn.Length);
				}
				else
				{
					// this must be a word
					tssRealForm = tssWordText;
				}
				m_cache.VwCacheDaAccessor.CacheStringProp(hvoAnnotation, m_tagRealForm, tssRealForm);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tssWordText"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="segmentWordformIndex"></param>
		/// <param name="tssWordAnn">the wordform of the matching annotation (could be a phrase).</param>
		/// <returns>the new or reused annotation id</returns>
		private int CreateOrReuseAnnotation(ITsString tssWordText, int ichMin, int ichLim, int segmentWordformIndex, out ITsString tssWordAnn)
		{
			// First see if we can find a real annotation we can reuse for the wordform or alternative case.
			int hvoWordform = 0;
			int hvoAnnotation = ReuseRealTwficAnnotation(tssWordText, ichMin, ichLim, out hvoWordform, out tssWordAnn);
			if (tssWordAnn.Length == 0)
				tssWordAnn = tssWordText;

			// check to see if we can match a user-confirmed phrase to establish a user based guess.
			// if we didn't find a real annotation or it's only an annotation for a WfiWordform.
			if (!SegmentFormCollectionMode && s_realPhrases.Count > 0 &&
				HasTrivialAnalysis(hvoAnnotation) &&
				m_matcher == null)
			{
				ITsString tssPhrase;
				if (TryCreatePhrase(tssWordText, ichMin, tssWordAnn.Length, out tssPhrase))
				{
					tssWordAnn = tssPhrase;
					if (hvoAnnotation != 0)
					{
						// we need to reserve this annotation because it contains a reference to a different wordform.
						Set<int> twficsToOrphan = new Set<int>(new int[] { hvoAnnotation });
						ReserveAnnotations(twficsToOrphan, true);
						hvoAnnotation = 0;	// indicates the need to create a new dummy annotation.
					}
				}
			}
			else if (SegmentFormCollectionMode && m_matcher == null)
			{
				// in SegmentFormCollectionMode, we only really care about the offsets and the InstanceOf (analysis).
				// we don't want to change the real annotation (yet).
				if (hvoAnnotation != 0)
				{
					int hvoAnalysis = m_cache.MainCacheAccessor.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf);
					// Create a dummy annotation, and set the instanceOf to the real annotation, if we found one.
					hvoAnnotation = CreateDummyAnnotation(hvoAnalysis, ichMin, ichMin + tssWordAnn.Length, m_cadTwfic.Hvo);
				}
				else
				{
					// Create an annotation with a dummy wordform.
					hvoAnnotation = CreateDummyAnnotation(tssWordAnn, ichMin, ichMin + tssWordAnn.Length, out hvoWordform);
				}
				return hvoAnnotation;
			}

			// See if the word/phrase in the text has a different capitalization form.
			CacheRealForm(hvoAnnotation, ichMin, tssWordText, tssWordAnn);
			if (hvoAnnotation == 0)
			{
				// couldn't find a real annotation, so try to create one or a dummy.
				hvoAnnotation = CreateDummyAnnotation(tssWordAnn, ichMin, ichLim, out hvoWordform);
			}
			else
			{
				// reuse hvoAnnotation
				AdjustCbaFields(hvoAnnotation, ichMin, tssWordAnn, hvoWordform);
			}

			string sLower = m_paragraphTextScanner.ToLower(tssWordAnn);
			if (segmentWordformIndex == 0 && HasTrivialAnalysis(hvoAnnotation) && sLower != tssWordAnn.Text)
			{
				// Let's store an alternative value with the lowercase form.
				int hvoWordformLower = m_wfi.GetWordformId(sLower, StringUtils.GetWsAtOffset(tssWordAnn, 0));
				if (hvoWordformLower != 0 && !Cache.IsDummyObject(hvoWordformLower))
				{
					m_cache.VwCacheDaAccessor.CacheObjProp(hvoAnnotation,
						m_tagLowercaseForm, hvoWordformLower);
				}
			}

			if (CollectWordformOccurrencesInTexts)
			{
				int cOccurrences = m_cache.MainCacheAccessor.get_VecSize(hvoWordform, kflidOccurrences);
				m_cache.VwCacheDaAccessor.CacheReplace(hvoWordform, kflidOccurrences,
					cOccurrences, cOccurrences, new int[] { hvoAnnotation }, 1); // insert at end
			}
			s_wordformIdOccurrencesTable.Add(hvoWordform);
			return hvoAnnotation;
		}

		protected void AdjustCbaFields(int hvoAnnotation, int ichMin, ITsString tssWordAnn, int hvoWordform)
		{
			if (HasTrivialAnalysis(hvoAnnotation))
			{
				int ws = StringUtils.GetWsAtOffset(tssWordAnn, 0);
				m_rgEmptyWfis.Add(new EmptyWwfAnno(hvoWordform, hvoAnnotation, ws));
			}
			AdjustCbaFields(hvoAnnotation, ichMin, ichMin + tssWordAnn.Length);
		}

		protected virtual void AdjustCbaFields(int hvoAnnotation, int ichMin, int ichLim)
		{
			using (new IgnorePropChanged(Cache, PropChangedHandling.SuppressAll))
			{
				CmBaseAnnotation.SetCbaFields(Cache, hvoAnnotation, ichMin, ichLim, m_para.Hvo, false);
			}
		}

		private bool HasTrivialAnalysis(int hvoAnnotation)
		{
			int hvoInstanceOf = 0;
			if (hvoAnnotation != 0)
			{
				hvoInstanceOf = m_cache.MainCacheAccessor.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf);
			}
			return (hvoInstanceOf == 0 || m_cache.GetClassOfObject(hvoInstanceOf) == WfiWordform.kclsidWfiWordform);
		}

		/// <summary>
		/// Returns a phrase wordform if it can create one without overwriting other annotations with
		/// significant (ie. non-wordform) analyses.
		/// </summary>
		/// <param name="tssWordText"></param>
		/// <param name="ichMin"></param>
		/// <param name="lengthToBeat">the size of a matched phrase must be greater than this.</param>
		/// <param name="tssPhrase">new phrase. tssWordText, by default.</param>
		/// <returns>true, if we could create a phrase.</returns>
		private bool TryCreatePhrase(ITsString tssWordText, int ichMin, int lengthToBeat, out ITsString tssPhrase)
		{
			tssPhrase = tssWordText;
			// Check to see if we can match the longest real-phrase-form in the text.
			// If so, give preference to that form.
			Set<ITsString> phrases;
			if (s_realPhrases.TryGetValue(m_paragraphTextScanner.ToLower(tssWordText), out phrases))
			{
				int phraseBoundaryOffset = -1;
				foreach (ITsString tssTryPhrase in phrases)
				{
					// look for the longest match. (we're greedy that way.)
					// *** However, we don't want to overwrite an existing annotation for the next word.
					// Only be as greedy as the real annotations allow.
					// we could collect segment forms on this phrase in the paragraph and verify
					// that all the things that get returned are dummy annotations.
					// if not, then we have an a real annotation we don't want to overwrite.
					// make sure CollectSegmentForms has a collection mode that doesn't actually
					// use any of the real ids. once we know where the first real annotation is,
					// we know the limit of our phrase length.
					if (tssTryPhrase.Length <= lengthToBeat ||
						(phraseBoundaryOffset != -1 && (ichMin + tssTryPhrase.Length) > phraseBoundaryOffset) ||
						tssPhrase.Length > tssTryPhrase.Length ||
						!m_paragraphTextScanner.MatchesWordInText(tssTryPhrase, ichMin))
					{
						continue;
					}

					List<int> phraseSegmentForms;
					int iSegFormBoundary = -1;
					if (!TryRealSegmentFormsInPhrase(ichMin, ichMin + tssTryPhrase.Length, out phraseSegmentForms, out iSegFormBoundary))
					{
						// extend annotation wordform to this phrase.
						tssPhrase = tssTryPhrase;
					}
					else if (phraseBoundaryOffset == -1)
					{
						// we found segmentForm in that phrase that has an analysis we don't want to overwrite.
						// make sure subsequent phrase searches are limited by that boundary.
						int hvoSegForm = phraseSegmentForms[iSegFormBoundary];
						int segFormBeginOffset = Cache.GetIntProperty(hvoSegForm, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
						phraseBoundaryOffset = segFormBeginOffset;
					}
				}
			}
			return tssWordText.Length < tssPhrase.Length;
		}

		private int CreateDummyAnnotation(ITsString tssWordAnn, int ichMin, int ichLim, out int hvoWordform)
		{
			int hvoAnnotation = 0;
			hvoWordform = 0;
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			// give preference to alternative case forms that have exact annotation offset matches.
			int wfId = m_wfi.GetWordformId(tssWordAnn);
			if (wfId == 0)
			{
				//Debug.Assert((m_wfi as WordformInventory).SuspendUpdatingConcordanceWordforms == this.RebuildingConcordanceWordforms,
				//	"Make sure that the WordformInventory is still in sync with us RebuildingConcordanceWordforms");
				if (CreateDummyWordforms)
				{
					// Make a dummy wordform (faster loading new texts)
					hvoWordform = m_wfi.AddDummyWordform(tssWordAnn);
				}
				else
				{
					using (new IgnorePropChanged(Cache, PropChangedHandling.SuppressNone))
					{
						// allows ParserConnection to generate Analyses.
						hvoWordform = m_wfi.AddRealWordform(tssWordAnn).Hvo;
					}
				}
			}
			else
			{
				hvoWordform = wfId;
				if (!CreateDummyWordforms && m_cache.IsDummyObject(hvoWordform))
				{
					// convert dummy wordforms made building ConcordanceWords list
					// so ParserConnection can generate Analyses of these.
					hvoWordform = ConvertToRealWordform(hvoWordform);
					Debug.Assert(hvoWordform != 0);
				}
			}
			// if we have a real wordform, try to reuse a leftover annotation from a previous parsing session.
			if (!SegmentFormCollectionMode && !m_cache.IsDummyObject(hvoWordform) &&
				m_orphanedAnnotations.Count > 0)
			{
				using (new IgnorePropChanged(Cache, PropChangedHandling.SuppressAll))
				{
					hvoAnnotation = ReuseOrphanedAnnotation(ichMin, ichMin + tssWordAnn.Length, hvoWordform, m_cadTwfic.Hvo);
					int ws = StringUtils.GetWsAtOffset(tssWordAnn, 0);
					m_rgEmptyWfis.Add(new EmptyWwfAnno(hvoWordform, hvoAnnotation, ws));
				}
			}
			else
			{
				// Make a dummy annotation.
				hvoAnnotation = CreateDummyAnnotation(hvoWordform, ichMin, ichMin + tssWordAnn.Length, m_cadTwfic.Hvo);
			}
			// Enhance: if hvoWordform.FullConcordanceIds is already in memory, add hvoAnn to it.
			return hvoAnnotation;
		}

		private int ReuseOrphanedAnnotation(int ichMin, int ichLim, int hvoInstanceOf, int hvoAnnType)
		{
			List<int> annotations = new List<int>(m_orphanedAnnotations);
			int hvoAnnotation = annotations[0];
			UseId(0, annotations, m_orphanedAnnotations);
			// Disable prop changed, since this is essentially a new annotation.
			using (new IgnorePropChanged(Cache, PropChangedHandling.SuppressAll))
			{
				ISilDataAccess sda = Cache.MainCacheAccessor;
				if (hvoAnnType != m_cadTwfic.Hvo) // currently only loading twfics into m_orphanedAnnotations.
				{
					sda.SetObjProp(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidAnnotationType, hvoAnnType);
				}
				// assume, we'll need to reset all the fields.
				CmBaseAnnotation.SetCbaFields(Cache, hvoAnnotation, ichMin, ichLim, m_para.Hvo, hvoInstanceOf, true);
			}
			return hvoAnnotation;
		}

		int CreateDummyAnnotation(int hvoInstanceOf, int ichMin, int ichLim, int hvoAnnType)
		{
			m_fMadeDummyAnnotations = true;
#if PROFILING
			m_cDummyAnnotations++;
			if (hvoAnnType == TextSegmentDefn)
				m_cSegmentsMade++;
			else if (hvoAnnType == m_cadTwfic.Hvo)
				m_cWficsMade++;
			else if (hvoAnnType == m_cadPunct.Hvo)
				m_cPficsMade++;
#endif
			int hvoAnn;
			if (m_dummyAnnotationsToReuse != null && m_iNextDummyAnnotationToReuse < m_dummyAnnotationsToReuse.Count)
			{
				hvoAnn = m_dummyAnnotationsToReuse[m_iNextDummyAnnotationToReuse];
				m_iNextDummyAnnotationToReuse++;
#if PROFILING
				long ticks = DateTime.Now.Ticks;
#endif
				// Before reusing the dummy annotation, we need to make sure we clear out any other virtual information
				// stored on the original annotation...otherwise it can show up on this guy unexpectedly. (LT-8467)
				// For performance purposes, only ClearInfoAbout this dummy annotation, if something significant has changed,
				// but not if the offsets alone have changed.
				int flidObjDiff = 0;
				if (CmBaseAnnotation.TryFindFirstSignificantDiffInCbaObjLinkInfo(Cache, hvoAnn, hvoInstanceOf, hvoAnnType, m_para.Hvo, out flidObjDiff))
					Cache.VwCacheDaAccessor.ClearInfoAbout(hvoAnn, VwClearInfoAction.kciaRemoveObjectInfoOnly);
				CmBaseAnnotation.SetDummyAnnotationInfo(Cache, hvoAnn, m_para.Hvo, hvoAnnType, ichMin, ichLim, hvoInstanceOf);

#if PROFILING
				m_cTicksResettingDummies += DateTime.Now.Ticks - ticks;
				m_cTotalDummiesReset++;
#endif
			}
			else
			{
#if PROFILING
				long ticks = DateTime.Now.Ticks;
#endif
				hvoAnn = CmBaseAnnotation.CreateDummyAnnotation(Cache, m_para.Hvo, hvoAnnType, ichMin, ichLim, hvoInstanceOf);
#if PROFILING
				m_cTicksMakingDummies += DateTime.Now.Ticks - ticks;
				s_cTotalDummiesMade++;
#endif
			}
			if (hvoAnnType == m_cadTwfic.Hvo)
			{
				// It would not normally make much sense to have a dummy annotation with an instanceOf
				// that is some non-trivial analysis, since if we know a non-trivial analaysis we want
				// to remember it in the database. However, there is a somewhat pathological case when
				// parsing a phrase (SegmentFormsCollectionMode) where we want a dummy annotation for
				// each wordform even if we already have a real one, and it's important that its instanceOf
				// points at the real analysis, not just the wordform.
				// Enhance JohnT or EricP: it would be nice if we didn't have to make these extra
				// dummy annotations, especially when we find a nontrivial analaysis, which means we
				// are NOT going to analyze it as a phrase after all, though the only reason for making
				// the dummy annotations is so we can test to see whether one of them has a real one.
				// But the layers are getting too complex to change confidently. It is possible these
				// extra dummy annotations we make while checking for phrases become memory leaks.
				int hvoWordform = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoAnn);
				WfiWordform.AddDummyAnnotation(m_cache, hvoWordform, hvoAnn);
				// JohnT: this was using IsValidObject, but that is a database query for every one!
				if (!m_cache.IsDummyObject(hvoWordform))
				{
					int cAnal = m_cache.GetVectorSize(hvoWordform, (int)WfiWordform.WfiWordformTags.kflidAnalyses);
					if (cAnal == 0)
					{
						// Save the information we need to later possibly generate a guess for this unanalyzed wordform.
						int ws = m_cache.GetObjProperty(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem);
						m_rgEmptyWfis.Add(new EmptyWwfAnno(hvoWordform, hvoAnn, ws));
					}
				}
			}
			return hvoAnn;
		}

		private int ConvertToRealWordform(int hvoDummyWordform)
		{
			IWfiWordform wf = CmObject.ConvertDummyToReal(Cache, hvoDummyWordform) as IWfiWordform;
			return wf != null ? wf.Hvo : 0;
		}

		/// <summary>
		/// This class stores the information used by AddEntryGuesses().
		/// </summary>
		private class EmptyWwfAnno
		{
			public int m_hvoWwf;	// database id of an unanalyzed WfiWordform.
			public int m_hvoAnn;	// database id of a (probably dummy) CmAnnotation
			public int m_ws;		// writing system id of the WfiWordform's form.
			public EmptyWwfAnno(int hvoWfi, int hvoAnn, int ws)
			{
				m_hvoWwf = hvoWfi;
				m_hvoAnn = hvoAnn;
				m_ws = ws;
			}
		}

		/// <summary>
		/// This class stores the database Id (and the writing system id) for a WfiWordform that
		/// has no analyses, but whose form exactly matches a LexemeForm or an AlternativeForm
		/// of a LexEntry.
		/// </summary>
		private class EmptyWwfKey
		{
			public int m_hvoWwf;
			public int m_wsWwf;
			public EmptyWwfKey(int hvo, int ws)
			{
				m_hvoWwf = hvo;
				m_wsWwf = ws;
			}

			public override int GetHashCode()
			{
				return m_hvoWwf * m_wsWwf;
			}

			public override bool Equals(object obj)
			{
				EmptyWwfKey that = obj as EmptyWwfKey;
				if (that == null)
					return false;
				else
					return this.m_hvoWwf == that.m_hvoWwf && this.m_wsWwf == that.m_wsWwf;
			}
		}
		/// <summary>
		/// This class stores the relevant database ids for information which can generate a
		/// default analysis for a WfiWordform that has no analyses, but whose form exactly
		/// matches a LexemeForm or an AlternateForm of a LexEntry.
		/// </summary>
		private class EmptyWwfInfo
		{
			public int m_hvoEntry;
			public int m_hvoForm;
			public int m_hvoMsa;
			public int m_hvoPOS;
			public int m_hvoSense;
			public EmptyWwfInfo(int hvoEntry, int hvoForm, int hvoMsa, int hvoPOS, int hvoSense)
			{
				m_hvoEntry = hvoEntry;
				m_hvoForm = hvoForm;
				m_hvoMsa = hvoMsa;
				m_hvoPOS = hvoPOS;
				m_hvoSense = hvoSense;
			}
		}
		List<EmptyWwfAnno> m_rgEmptyWfis = new List<EmptyWwfAnno>();
		Dictionary<EmptyWwfKey, EmptyWwfInfo> m_mapEmptyWfInfo = new Dictionary<EmptyWwfKey, EmptyWwfInfo>();

		/// <summary>
		/// This goes through the collected list of WfiWordforms that do not have any analyses, and generates
		/// a guess for any whose forms exactly match a LexemeForm (or AlternateForm) of a stem/root entry.
		/// </summary>
		/// <param name="progress"></param>
		private void AddEntryGuesses(ProgressState progress)
		{
			if (m_rgEmptyWfis.Count == 0)
				return;
			MapEmptyWfToInfo();
			progress.Breath();
			if (m_mapEmptyWfInfo.Count == 0)
				return;
			foreach (EmptyWwfAnno ewa in m_rgEmptyWfis)
			{
				IWfiWordform ww = WfiWordform.CreateFromDBObject(m_cache, ewa.m_hvoWwf);
				EmptyWwfKey key = new EmptyWwfKey(ww.Hvo, ewa.m_ws);
				EmptyWwfInfo info;
				if (!m_mapEmptyWfInfo.TryGetValue(key, out info))
					continue;
				IWfiAnalysis wa;
				if (ww.AnalysesOC.Count == 0)
				{
					ITsString tssName = null;
					int wsVern = 0;
					ILexEntryRef ler = SandboxBase.GetVariantRef(m_cache, info.m_hvoEntry, true);
					int hvoEntryToDisplay = info.m_hvoEntry;
					if (ler != null)
					{
						ICmObject coRef = ler.ComponentLexemesRS[0];
						if (coRef is ILexSense)
							hvoEntryToDisplay = (coRef as ILexSense).EntryID;
						else
							hvoEntryToDisplay = coRef.Hvo;
						wsVern = StringUtils.GetWsAtOffset(m_tssPara, 0);
						tssName = InterlinDocChild.GetLexEntryTss(Cache, hvoEntryToDisplay, wsVern, ler);
						info.m_hvoSense = m_cache.MainCacheAccessor.get_VecItem(hvoEntryToDisplay,
							(int)LexEntry.LexEntryTags.kflidSenses, 0);
						info.m_hvoMsa = m_cache.MainCacheAccessor.get_ObjectProp(info.m_hvoSense,
							(int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis);
						int clidMsa = m_cache.GetClassOfObject(info.m_hvoMsa);
						if (info.m_hvoPOS == 0)
						{
							info.m_hvoPOS = m_cache.MainCacheAccessor.get_ObjectProp(info.m_hvoMsa,
								(int)MoStemMsa.MoStemMsaTags.kflidPartOfSpeech);
						}
					}
					wa = new WfiAnalysis();
					ww.AnalysesOC.Add(wa);
					wa.CategoryRAHvo = info.m_hvoPOS;
					WfiGloss wg = new WfiGloss();
					wa.MeaningsOC.Add(wg);
					// Not all entries have senses.
					if (info.m_hvoSense != 0)
					{
						MultiUnicodeAccessor muaGloss = new MultiUnicodeAccessor(m_cache, info.m_hvoSense,	/* ls.Id */
							(int)LexSense.LexSenseTags.kflidGloss, "LexSense_Gloss");
						wg.Form.MergeAlternatives(muaGloss);
					}
					WfiMorphBundle wmb = new WfiMorphBundle();
					wa.MorphBundlesOS.Append(wmb);
					wmb.MorphRAHvo = info.m_hvoForm;
					if (tssName != null && wsVern != 0)
						wmb.Form.SetAlternative(tssName, wsVern);
					wmb.MsaRAHvo = info.m_hvoMsa;
					wmb.SenseRAHvo = info.m_hvoSense;

					// Now, set up an approved "Computer" evaluation of this generated analysis
					wa.SetAgentOpinion(m_cache.LangProject.DefaultComputerAgent, Opinions.approves);
				}
				else
				{
					// The same unanalyzed word may occur twice in a paragraph...
					wa = ww.AnalysesOC.ToArray()[0];
					Debug.Assert(ww.AnalysesOC.Count == 1);
					Debug.Assert(wa.CategoryRAHvo == info.m_hvoPOS);
					Debug.Assert(wa.MorphBundlesOS.Count == 1);
					Debug.Assert(wa.MorphBundlesOS[0].MorphRAHvo == info.m_hvoForm);
					Debug.Assert(wa.MorphBundlesOS[0].MsaRAHvo == info.m_hvoMsa);
					Debug.Assert(wa.MorphBundlesOS[0].SenseRAHvo == info.m_hvoSense);
				}
			}
			progress.Breath();
		}

		private void MapEmptyWfToInfo()
		{
			m_mapEmptyWfInfo.Clear();
			IOleDbCommand odc = null;
			try
			{
				StringBuilder sbSql = new StringBuilder();
				sbSql.AppendLine("SELECT wwf.Obj, wwf.Ws, le.Id, mf.Id, msa.Id, msa.PartOfSpeech, ls.Id");
				sbSql.AppendLine(" FROM LexEntry le");
				sbSql.AppendLine(" JOIN MoStemAllomorph_ mf ON mf.Owner$=le.Id AND mf.OwnFlid$ IN (5002029,5002030)");
				sbSql.Append(" JOIN MoMorphType_ mmt ON mmt.Id=mf.MorphType");
				sbSql.AppendFormat(" AND mmt.Guid$ <> '{0}' AND mmt.Guid$ <> '{1}'",
					MoMorphType.kguidMorphBoundRoot, MoMorphType.kguidMorphBoundStem);
				sbSql.AppendLine();
				sbSql.AppendLine(" JOIN MoForm_Form mff ON mff.Obj=mf.Id");
				sbSql.AppendLine(" JOIN WfiWordform_Form wwf ON wwf.Txt=mff.Txt AND wwf.Ws=mff.Ws");
				sbSql.AppendLine(" LEFT OUTER JOIN MoStemMsa_ msa ON msa.Owner$=le.Id");
				sbSql.AppendLine(" LEFT OUTER JOIN LexSense_ ls ON ls.MorphoSyntaxAnalysis=msa.Id");
				sbSql.AppendLine(" LEFT OUTER JOIN WfiWordform_Analyses wwa ON wwa.Src=wwf.Obj");
				sbSql.AppendLine(" WHERE wwa.Dst IS NULL");
				sbSql.AppendLine(" ORDER BY le.HomographNumber, mf.OwnFlid$, ls.OwnOrd$;");
				string sQry = sbSql.ToString();
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				odc.ExecCommand(sQry, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				for (; fMoreRows; odc.NextRow(out fMoreRows))
				{
					int hvoWwf = DbOps.ReadInt(odc, 0);
					int wsWwf = DbOps.ReadInt(odc, 1);
					if (hvoWwf == 0 || wsWwf == 0)
						continue;
					int hvoEntry = DbOps.ReadInt(odc, 2);
					int hvoForm = DbOps.ReadInt(odc, 3);
					if (hvoEntry == 0 || hvoForm == 0)
						continue;
					int hvoMsa = DbOps.ReadInt(odc, 4);
					int hvoPOS = DbOps.ReadInt(odc, 5);
					int hvoSense = DbOps.ReadInt(odc, 6);
					EmptyWwfKey key = new EmptyWwfKey(hvoWwf, wsWwf);
					EmptyWwfInfo info;
					if (!m_mapEmptyWfInfo.TryGetValue(key, out info))
					{
						info = new EmptyWwfInfo(hvoEntry, hvoForm, hvoMsa, hvoPOS, hvoSense);
						m_mapEmptyWfInfo.Add(key, info);
					}
				}
			}
			catch (Exception exc)
			{
				// We shouldn't have any errors thrown, but absorb any that are.
				Debug.WriteLine("Exception ignored in ParagraphParser.MapEmptyWfToInfo(): {0}", exc.Message);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
		}

		#region IDisposable implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ParagraphParser()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// Dispose of unmanaged resources hidden in member variables.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (RebuildingConcordanceWordforms)
					RebuildingConcordanceWordforms = false;	// don't get rid of m_wfi until we do this.
			}
			// Bad idea to do this here, since one can't access C# objects in this context,
			// which is what the property will try to do.
			//if (RebuildingConcordanceWordforms)
			//	RebuildingConcordanceWordforms = false;	// don't get rid of m_wfi until we do this.
			m_cadTwfic = null;
			m_cadPunct = null;
			m_punctAnnotations = null;
			m_wordforms = null;
			m_wordformAnnotationPossibilities = null;
			m_paragraphTextScanner = null;
			m_paraRealAnnIds = null;
			m_paraRealWfIds = null;
			m_paraRealTwfics = null;
			m_unusedPunctuationAnnotations = null;
			m_unusedTwficAnnotations = null;
			m_unusedSegmentAnnotations = null;
			m_realParagraphSegmentAnnotations = null;
			m_realParagraphPunctuationAnnotations = null;
			m_realParagraphWordforms = null;
			m_realParagraphTwficAnnotations = null;
			m_annotations = null;
			m_paraIdsParsed = null;
			m_wfi = null;
			m_lp = null;
			m_wordMaker = null;
			m_tssPara = null;
			m_para = null;
			m_hvosStText = null;
			m_hvosStTxtPara = null;
			m_cache = null;

			m_isDisposed = true;
		}
		#endregion
	}

	/// <summary>
	/// This class helps to load Segment/SegForms properties validating
	/// that their offsets match the actual text, without trying to change them.
	/// It will fail/throw if it cannot succeed.
	/// </summary>
	public class ParagraphParserForEditMonitoring : ParagraphParser
	{
		int m_cUndoActionsOrig = -1;
		int m_cUndoSequenceOrig = -1;

		public ParagraphParserForEditMonitoring(FdoCache cache)
			: base(cache)
		{
			RecordUndoActionState();
		}

		public ParagraphParserForEditMonitoring(IStTxtPara para)
			: base(para)
		{
			RecordUndoActionState();
		}

		void RecordUndoActionState()
		{
			// this should not change by more than one.
			m_cUndoActionsOrig = m_cache.ActionHandlerAccessor.UndoableActionCount;
			m_cUndoSequenceOrig = m_cache.ActionHandlerAccessor.UndoableSequenceCount;
		}

		/// <summary>
		/// when monitoring edits, we should not need to readjust real offsets
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		protected override void AdjustCbaFields(int hvoAnnotation, int ichMin, int ichLim)
		{
			ICmBaseAnnotation cba = CmBaseAnnotation.CreateFromDBObject(m_cache, hvoAnnotation);
			Debug.Assert(ichMin == cba.BeginOffset);
			Debug.Assert(ichLim == cba.EndOffset);
			// base.AdjustCbaFields(hvoAnnotation, ichMin, tssWordAnn);
		}

		protected override void Dispose(bool disposing)
		{
			if (m_cUndoActionsOrig < m_cache.ActionHandlerAccessor.UndoableActionCount)
			{
				// it's possible that in tests, we add an undo action, but shouldn't have
				// changed anything else in the database.
				if (m_cUndoActionsOrig == m_cache.ActionHandlerAccessor.UndoableActionCount + 1)
					m_cache.Undo();
			}
			if (m_cUndoActionsOrig != m_cache.ActionHandlerAccessor.UndoableActionCount)
			{
				string msg = "Parsing should not have changed anything in the database.";
				Debug.Fail(msg);
				throw new ApplicationException(msg);
			}
			base.Dispose(disposing);
			m_cUndoActionsOrig = -1;
		}

		/// <summary>
		/// Don't need to do any dummy conversions during edit monitoring.
		/// </summary>
		/// <param name="segments"></param>
		/// <param name="segmentsArray"></param>
		/// <param name="iseg"></param>
		/// <param name="formsInSegment"></param>
		protected override void EnsureRealSegmentsForNonTrivialAnalyses(List<int> segments, int[] segmentsArray, int iseg, List<int> formsInSegment)
		{
			//base.EnsureRealSegmentsForNonTrivialAnalyses(segments, segmentsArray, iseg, formsInSegment);
		}

		/// <summary>
		/// only reuse exact matches
		/// </summary>
		/// <param name="realParagraphAnnotations"></param>
		/// <param name="unusedAnnotations"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="hvoAnnType"></param>
		/// <returns></returns>
		protected override int ReuseFirstUnusedAnnotation(Dictionary<int, List<int>> realParagraphAnnotations, Set<int> unusedAnnotations, int ichMin, int ichLim, int hvoAnnType)
		{
			CmBaseAnnotation cbaFirstUnused;
			int iann;
			List<int> annotations;
			if (TryReuseFirstUnusedCbaMatchingText(realParagraphAnnotations, unusedAnnotations, ichMin, ichLim, out cbaFirstUnused, out annotations, out iann))
			{
				return cbaFirstUnused.Hvo;
			}
			if (cbaFirstUnused != null)
			{
				if (cbaFirstUnused.BeginOffset > ichMin && cbaFirstUnused.EndOffset > ichLim)
				{
					// this annotation (and subsequent) should be (re)used later in the text.
					// so, keep this one for later.
				}
				else
				{
					string msg = String.Format("oops...somehow our text position ({0},{1}) has gotten ahead of our first unused real annotation {2}({3},{4}).",
						new object[] { ichMin, ichLim, cbaFirstUnused.Hvo, cbaFirstUnused.BeginOffset, cbaFirstUnused.EndOffset });
					Debug.Fail(msg);
					throw new ApplicationException(msg);
				}
			}
			return 0;
		}

		/// <summary>
		/// Expect that we reused everything, and have no unused annotations to delete/reserve.
		/// </summary>
		/// <param name="progress"></param>
		protected override void CleanupLeftoverRealAnnotations(ProgressState progress)
		{
			if (m_unusedPunctuationAnnotations.Count != 0 ||
				m_unusedSegmentAnnotations.Count != 0 ||
				m_unusedTwficAnnotations.Count != 0)
			{
				throw new ApplicationException(String.Format("We shouldn't have leftover annotations: twfics({0}) segments({1}) puncts({2})",
					new object[] { m_unusedTwficAnnotations.Count, m_unusedSegmentAnnotations.Count, m_unusedPunctuationAnnotations.Count }));
			}
		}

	}

#if DEBUG
	/// <summary>
	/// Use like this:
	/// try
	/// {
	/// #if DEBUG
	///		TimeRecorder.Begin("BlockName");
	/// #endif
	///		...
	///	}
	///	finally
	///	{
	/// #if DEBUG
	///		TimeRecorder.End("BlockName");
	/// #endif
	///	}
	///
	///	(If there are no other exists from the relevant code, you can just use begin and end.)
	///	...somewhere it's appropriate to make the report
	///	TimeRecorder.Report();
	/// </summary>
	public class TimeRecorder
	{
		static Dictionary<string, TimeVal> s_dict = new Dictionary<string, TimeVal>();
		static string s_blockname = "";

		class TimeVal
		{
			public int start;
			public int duration = 0;
		}

		public static void Begin(string blockname)
		{

			s_blockname += "." + blockname;
			if (!s_dict.ContainsKey(s_blockname))
			{
				s_dict[s_blockname] = new TimeVal();
			}
			TimeVal tv = s_dict[s_blockname];
			tv.start = Environment.TickCount;
		}
		public static void End(string blockname)
		{
			int end = Environment.TickCount; // exclude lookup from time.
			if (blockname != s_blockname.Substring(s_blockname.Length - blockname.Length, blockname.Length))
			{
				Debug.WriteLine("unmatched end for block " + blockname);
			}
			if (s_dict.ContainsKey(s_blockname))
			{
				TimeVal tv = s_dict[s_blockname];
				tv.duration += end - tv.start;
			}
			else
			{
				Debug.WriteLine("missing begin for block " + blockname);
			}
			s_blockname = s_blockname.Substring(0, s_blockname.Length - blockname.Length - 1);
		}

		public static void Report()
		{
			// Can't use StringCollection, because it can't sort.
			List<string> items = new List<string>();
			foreach (KeyValuePair<string, TimeVal> kvp in s_dict)
			{
				items.Add(kvp.Key);
			}
			items.Sort();
			foreach(string key in items)
			{
				Debug.WriteLine(key + ": " + s_dict[key].duration.ToString());
			}
			s_dict.Clear();
			s_blockname = ""; // just to be sure
		}
	}
#endif

	/// <summary>
	/// Caches on first demand an FDO object for each of the standard MoMorphTypes.
	/// </summary>
	public class MorphTypes
	{
		FdoCache m_cache;
		int m_wsEng;
		IMoMorphType m_root;
		IMoMorphType m_prefix;
		IMoMorphType m_suffix;

		public MorphTypes(FdoCache cache)
		{
			m_cache = cache;
			// We specifically want the english writing system in order to look them up
			// using the known English names.
			m_wsEng = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
		}

		/// <summary>
		/// Get the MoMorphType whose (English) name is given.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public IMoMorphType GetTypeNamed(string name)
		{
			int hvo = DbOps.FindObjectWithStringInFlid(name,
				(int)CmPossibility.CmPossibilityTags.kflidName, m_wsEng, false, m_cache);
			return MoMorphType.CreateFromDBObject(m_cache, hvo);
		}

		public IMoMorphType Root
		{
			get
			{
				if (m_root == null)
					m_root = GetTypeNamed("root");
				return m_root;
			}
		}

		public IMoMorphType Prefix
		{
			get
			{
				if (m_prefix == null)
					m_prefix = GetTypeNamed("prefix");
				return m_prefix;
			}
		}

		public IMoMorphType Suffix
		{
			get
			{
				if (m_suffix == null)
					m_suffix = GetTypeNamed("suffix");
				return m_suffix;
			}
		}
	}

	/// <summary>
	/// This class is initialized with an ITsString, then NextWord may be called until it returns
	/// null to obtain a breakdown into words.
	/// </summary>
	public class WordMaker
	{
		ITsString m_tss;
		int m_ich; // current position in string, initially 0, advances to char after word or end.
		int m_cch; // length of string
		string m_st; // text of m_tss
		private CpeTracker m_tracker;
		// Range of characters for which m_cpe is known to be valid. Don't use if ich is outside
		// this range except for things like white-space testing that don't depend on WS.
		ILgCharacterPropertyEngine m_cpe;
		static Dictionary<string, string> s_wordformToLower = new Dictionary<string, string>();

		/// <summary>
		/// Start it off analyzing a string.
		/// </summary>
		/// <param name="tss"></param>
		/// <param name="cpe">engine to use.</param>
		public WordMaker(ITsString tss, ILgWritingSystemFactory wsf)
		{
			Init(tss);
			m_tracker = new CpeTracker(wsf, tss);
			m_cpe = m_tracker.CharPropEngine(0); // a default for functions that don't depend on wordforming.
		}

		private void Init(ITsString tss)
		{
			m_tss = tss;
			if (tss != null)
			{
				m_ich = 0;
				m_st = tss.Text;
				if (m_st == null)
					m_st = "";
				m_cch = m_st.Length;
			}
			else
			{
				m_ich = -1;
				m_st = null;
				m_cch = -1;
			}
		}

		internal ITsString Tss
		{
			get
			{
				return m_tss;
			}
			set
			{
				Init(value);
			}
		}

		internal int CurrentCharOffset
		{
			get
			{
				return m_ich;
			}
			set
			{
				Debug.Assert(value <= m_tss.Length, String.Format("Character offset {0} exceeds string limit {1}", value, m_tss.Length));
				if (value <= m_tss.Length)
				{
					m_ich = value;
				}
			}
		}

		/// <summary>
		/// Compares tssWord to the word/phrase at the offset ichMinText. Case insensitive.
		/// </summary>
		/// <param name="tssWord"></param>
		/// <param name="ichMinText"></param>
		/// <returns></returns>
		internal bool MatchesWordInText(ITsString tssWord, int ichMinText)
		{
			int lenTssWord = tssWord.Length;
			if (lenTssWord == 0)
				return false;
			int ichLimText = ichMinText + lenTssWord;
			if (ichLimText > m_st.Length)
				return false;
			if (ichLimText < m_st.Length)
			{
				// Since ichLimText can refer to an index of character in m_st
				// Verify that tssWord matches the word/phrase boundary in the text.
				// (assume ichMinText is start of a word in the text)
				if (IsWordforming(ichLimText))
					return false;	// we didn't match the end of the word in the text.
			}
			ITsString tssTxtWord = m_tss.GetSubstring(ichMinText, ichLimText);
			return this.MatchesWord(tssWord, tssTxtWord);
		}

		/// <summary>
		/// Case insensitive comparison of two ITsStrings.
		/// </summary>
		/// <param name="tssA"></param>
		/// <param name="tssB"></param>
		/// <returns></returns>
		internal bool MatchesWord(ITsString tssA, ITsString tssB)
		{
			int lenA = tssA != null ? tssA.Length : 0;
			return tssB != null && lenA > 0 &&
					lenA == tssB.Length &&
					(tssA.Equals(tssB) ||
					(this.ToLower(tssA) == this.ToLower(tssB) &&
					WordWs(tssA) == WordWs(tssB)));
		}

		static internal int WordWs(ITsString tss)
		{
			return StringUtils.GetWsAtOffset(tss, 0);
		}

		internal string ToLower(ITsString tss)
		{
			string str = tss.Text;
			if (str == null)
				return null;

			string strLower = null;
			if (!s_wordformToLower.TryGetValue(str, out strLower))
			{
				// add the lowercase form to this dictionary.
				strLower = m_cpe.ToLower(str);
				s_wordformToLower[str] = strLower;
			}
			return strLower;
		}

		/// <summary>
		/// Finds the beginning offset of the next occurrence of wordform (tssWord) up to ichLim.
		/// </summary>
		/// <param name="tssWord">form of the word/phrase to search for the next occurrence.</param>
		/// <param name="ichMinBase">place in text to start searching for another matching word.</param>
		/// <param name="ichLim">stop searching when the search ichMinNext reaches this point.</param>
		/// <param name="ichMinNextMatch">beginning paragraph offset of the next wordform match to tssWordTarget, -1 if no match was found in bounds.</param>
		public void NextOccurrenceOfWord(ITsString tssWordTarget, int ichMinBase, int ichLim, out int ichMinNextMatch)
		{
			ichMinNextMatch = -1;
			if (tssWordTarget.Length == 0 || ichMinBase >= ichLim || ichMinBase >= m_tss.Length)
				return;
			int ichMinNext = 0;
			int ichLimNext = 0;

			// advance through the text to find next occurrence of word/phrase in text.
			m_ich = ichMinBase;
			while (ichLimNext < m_tss.Length && m_ich < m_tss.Length)
			{
				ITsString tssNextWord = NextWord(out ichMinNext, out ichLimNext);
				if (tssNextWord == null)
					break;	// we didn't find another word in the text.
				if (ichMinNext >= ichLim)
				{
					break;
				}
				else if (MatchesWordInText(tssWordTarget, ichMinNext))
				{
					ichMinNextMatch = ichMinNext;
					break;
				}
			}
		}

		public static bool IsLeadSurrogate(char ch)
		{
			const char minLeadSurrogate = '\xD800';
			const char maxLeadSurrogate = '\xDBFF';
			return ch >= minLeadSurrogate && ch <= maxLeadSurrogate;
		}
		/// <summary>
		/// Increment an index into a string, allowing for surrogates.
		/// Refactor JohnT: there should be some more shareable place to put this...
		/// a member function of string would be ideal...
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		public static int NextChar(string st, int ich)
		{
			if (IsLeadSurrogate(st[ich]))
				return ich + 2;
			return ich + 1;
		}

		/// <summary>
		/// Give the index of the next (full) character starting at ich.
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		public int NextChar(int ich)
		{
			return NextChar(m_st, ich);
		}

		/// <summary>
		/// Tells whether the full character (starting) at ich is a white-space character.
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		public bool IsWhite(int ich)
		{
			return m_cpe.get_GeneralCategory(FullCharAt(m_st, ich)) == LgGeneralCharCategory.kccZs;
		}

		/// <summary>
		/// Tells whether the full character starting at ich is wordforming.
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		public bool IsWordforming(int ich)
		{
			return m_tracker.CharPropEngine(ich).get_IsWordForming(FullCharAt(m_st, ich));
		}

		/// <summary>
		/// Return a full 32-bit character value from the surrogate pair.
		/// </summary>
		/// <param name="ch1"></param>
		/// <param name="ch2"></param>
		/// <returns></returns>
		public static int Int32FromSurrogates(char ch1, char ch2)
		{
			Debug.Assert(IsLeadSurrogate(ch1));
			return ((ch1 - 0xD800) << 10) + ch2 + 0x2400;
		}
		/// <summary>
		/// Return the full 32-bit character starting at position ich in st
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		public static int FullCharAt(string st, int ich)
		{
			if (IsLeadSurrogate(st[ich]))
				return Int32FromSurrogates(st[ich], st[ich + 1]);
			else return Convert.ToInt32(st[ich]);
		}

		public ITsString NextWord(out int ichMin, out int ichLim)
		{
			// m_ich is always left one character position after a non-wordforming character.
			// This is considered implicitly true at the start of the string.
			bool fPrevWordForming = false;
			int ichStartWord = -1;
			ichMin = ichLim = 0;
			for (; m_ich < m_cch; m_ich = NextChar(m_st, m_ich))
			{
				// Whether the character is wordforming or not depends on two things:
				// 1) whether it's part of a "chapter number" or "verse number" run. (See LT-6972.)
				// 2) whether it's considered a "word forming" character by the Unicode standard.
				bool fThisWordForming = IsWordforming(m_ich);
				if (fThisWordForming)
				{
					ITsTextProps ttp = m_tss.get_PropertiesAt(m_ich);
					string sStyleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					if (sStyleName == "Chapter Number" || sStyleName == "Verse Number")
						fThisWordForming = false;
				}
				if (fThisWordForming && !fPrevWordForming)
				{
					// Start of word.
					ichStartWord = m_ich;
				}
				else if (fPrevWordForming && !fThisWordForming)
				{
					// End of word
					Debug.Assert(ichStartWord >= 0);
					ichMin = ichStartWord;
					ichLim = m_ich;
					return m_tss.GetSubstring(ichStartWord, m_ich);
				}
				fPrevWordForming = fThisWordForming;
			}
			if (fPrevWordForming)
			{
				ichMin = ichStartWord;
				ichLim = m_ich;
				return m_tss.GetSubstring(ichStartWord, m_ich);
			}
			else
				return null; // didn't find any more words.
		}
	}

	/// <summary>
	/// This class is mainly the implementation of ParagraphParser.CollectSegmentAnnotations.
	/// It is broken out both because the algorithm is too complex for a single method, and so we
	/// can subclass depending on whether we want to actually create segments, or just note
	/// where they would be.
	/// </summary>
	public abstract class SegmentBreaker
	{
		List<int> ichMinSegBreaks = new List<int>();
		protected ITsString tssText;
		private CpeTracker m_cpeTracker;
		ILgCharacterPropertyEngine m_cpe;

		// The idea here is that certain characters more-or-less mark the end of a segment:
		// basically, sentence-terminating characters like period, question-mark, and so forth.
		// However, there may be trailing punctuation (closing quote, etc) after such a
		// character, which should be part of the preceding segment. So any non-word-forming stuff
		// following a segment-divider up to a blank is made part of the preceding segment.
		// Furthermore, a segment must contain some actual letters; any sequence of non-word-forming
		// characters following the last segment break character will just be appended to that
		// segment if there are no words.
		// A tricky special case is numbered segments, as in 1. This is a sentence. 2. This is another.
		// We don't want to make segments out of the numbers, though they are followed by periods.
		// Currently we detect this by the fact that numbers are not word-forming. Thus, the "1." segment
		// does not qualify (no word-forming characters). The "2." likewise can't be its own segment,
		// and since it follows a space after the "sentence.", it is attached to the next segment.
		// This needs extensive enhancement. Should be configurable.  User should be
		// able to configure at least the segment break character list and whether an
		// upper-case letter is required for a new segment.
		// It might be worth reading, understanding, and implementing Unicode Standard
		// Annex #29 "Text Boundaries".
		enum SegParseState
		{
			AwaitingFirstLetter, // have not yet found a word-forming character
			BuildingSegment, // have found word-forming in current segment, have not found EOS char.
			FoundEosChar, // found a segment-boundary character (after a word-forming)
			FoundBlankAfterEos, // got the position where we will end the segment, if we find another letter
			FoundNonBlankAfterBlankAfterEos, // got what will start next segment.
			ProcessingLabel, // have found some label text, and not yet anything that definitely terminates the label segment
		}
		public SegmentBreaker(ITsString text, ILgWritingSystemFactory wsf)
		{
			tssText = text;
			m_cpeTracker = new CpeTracker(wsf, text);
			// Make sure this is always something.
			m_cpe = m_cpeTracker.CharPropEngine(0);
		}

		private int m_prevCh;
		private string m_paraText;

		/// <summary>
		/// This returns the positions of the first EOS character in each segment, or (if a segment ends for some other
		/// reason) the index of the first character of the next segment. It may be one less than the length of the list
		/// of segments, if the last segment has no EOS character.
		/// </summary>
		public List<int> EosPositions { get { return ichMinSegBreaks; } }

		public void Run()
		{
			if (tssText != null)
			{
				m_paraText = tssText.Text;
				if (m_paraText == null)
					m_paraText = "";
			}
			else
				m_paraText = "";

			int ichStartSeg = 0; // First segment always starts at zero.
			SegParseState state = SegParseState.AwaitingFirstLetter;

			// This is the position where we will end a segment if we decide to make another one.
			// When we find a segment-terminating character, we set it to a position one greater.
			// If we find subsequent punctuation, we keep incrementing it till we find a space.
			// If it is equal to ichStartSeg, we haven't found a segment-terminating character.
			int ichLimSeg = 0;

			int ch = 0;
			LgGeneralCharCategory cc = 0;
			if (String.IsNullOrEmpty(m_paraText))
				return;
			m_prevCh = 0; // not numeric or period

			for (int ich = 0; ich < m_paraText.Length; ich = WordMaker.NextChar(m_paraText, ich))
			{
				m_prevCh = ch;
				ch = WordMaker.FullCharAt(m_paraText, ich);
				m_cpe = m_cpeTracker.CharPropEngine(ich);
				cc = m_cpe.get_GeneralCategory(ch);
				// don't try to deduce this from cc, it can be overiden.
				bool fIsLetter = m_cpe.get_IsWordForming(ch);
				bool fIsLabel = IsLabelText(tssText, tssText.get_RunAt(ich));
				if (ch == 0x2028)
				{
					// Hard line break, always its own segment.
					if (ich > ichStartSeg)
					{
						// If we've already recorded an EOS for the preceding segment, don't record another.
						if (ichMinSegBreaks.Count <= m_csegs)
							ichMinSegBreaks.Add(ich);
						CreateSegment(ichStartSeg, ich);
					}
					CreateSegment(ich, ich + 1);
					ichMinSegBreaks.Add(ich + 1);
					ichStartSeg = ich + 1;
					state = SegParseState.AwaitingFirstLetter;
					continue;
				}
				switch (state)
				{
					case SegParseState.AwaitingFirstLetter:
						if (fIsLabel)
							state = SegParseState.ProcessingLabel;
						else if (fIsLetter)
							state = SegParseState.BuildingSegment;
						break;
					case SegParseState.BuildingSegment:
						if (fIsLabel)
						{
							ichMinSegBreaks.Add(ich);
							ichLimSeg = ich;
							CreateSegment(ichStartSeg, ichLimSeg);
							ichStartSeg = ichLimSeg;
							state = SegParseState.ProcessingLabel;
							break;
						}
						if (IsEosChar(ch, cc, ich))
						{
							ichMinSegBreaks.Add(ich);
							state = SegParseState.FoundEosChar;
						}
						break;
					case SegParseState.FoundEosChar:
						if (fIsLabel)
						{
							ichLimSeg = ich;
							CreateSegment(ichStartSeg, ichLimSeg);
							ichStartSeg = ichLimSeg;
							state = SegParseState.ProcessingLabel;
							break;
						}
						if (cc == LgGeneralCharCategory.kccZs)
						{
							// We will end the segment here, provided we find valid content for
							// a following segment.
							state = SegParseState.FoundBlankAfterEos;
							ichLimSeg = ich + 1;
						}
						else if (fIsLetter)
						{
							// If a letter happens after a segment break, assume it's a new sentence
							// even if the preceding characters form an ellipsis (...)
							// This is the simplest way to handle the case where the user has
							// deleted a paragraph break resulting in two sentences separated
							// only by a segment break character (e.g. "first sentence.second sentence.")
							// as tested by TextEditingTexts.DeleteParagraphBreak().
							ichLimSeg = ich;
							CreateSegment(ichStartSeg, ichLimSeg);
							ichStartSeg = ichLimSeg;
							state = SegParseState.BuildingSegment;
						}
						break;
					case SegParseState.FoundBlankAfterEos:
					case SegParseState.FoundNonBlankAfterBlankAfterEos:
						if (fIsLabel)
						{
							ichLimSeg = ich;
							CreateSegment(ichStartSeg, ichLimSeg);
							ichStartSeg = ichLimSeg;
							state = SegParseState.ProcessingLabel;
							break;
						}
						if (fIsLetter)
						{
							// We found a segment break character, a following blank,
							// and something to make a following segment from.
							// Make the previous segment as determined.
							CreateSegment(ichStartSeg, ichLimSeg);
							ichStartSeg = ichLimSeg;
							state = SegParseState.BuildingSegment;
						}
						else if (cc == LgGeneralCharCategory.kccZs)
						{
							// found sequence of trailing spaces, put all in prev segment,
							// but only if we haven't seen a non-blank.
							if (state == SegParseState.FoundBlankAfterEos)
								ichLimSeg = ich + 1;
						}
						else
						{
							// non-letter non-blank, we'll stop incrementing ichLimSeg.
							state = SegParseState.FoundNonBlankAfterBlankAfterEos;
						}
						break;
					case SegParseState.ProcessingLabel:
						// A label segment is allowed to absorb following white space, but anything else non-label
						// will break it.
						if (fIsLabel || cc == LgGeneralCharCategory.kccZs)
							break;
						ichMinSegBreaks.Add(ich);
						ichLimSeg = ich;
						CreateSegment(ichStartSeg, ichLimSeg);
						ichStartSeg = ichLimSeg;
						state = SegParseState.BuildingSegment;
						break;
				}
			}
			// We reached the end of the loop. Make a segment out of anything left over.
			if (ichStartSeg < m_paraText.Length)
				CreateSegment(ichStartSeg, m_paraText.Length);

		}

		private bool IsEosChar(int ch, LgGeneralCharCategory cc, int ich)
		{
			if (ch == 0x002E) // full stop
				return !IsSpecialPeriod(ich);
			// The preliminary check of cc is just for efficiency. All these characters have this property.
			return StringUtils.IsEndOfSentenceChar(ch, cc);
		}

		/// <summary>
		/// Answer true if the period at ich (don't call otherwise) is a special one that should not
		/// be treated as end of segment.
		/// In a group of periods, this will be called for the first one, and determine whether it is
		/// in a group of three. If it is NOT a group of three, the first one is EOS, and the others
		/// will not be evaluated, so it does not matter what answer we give for such periods.
		/// So, if the previous character was a period, we may assume it was NOT an EOS, and therefore
		/// this one also is not. That is, a period may only be an EOS if it is the first in a sequence
		/// of periods.
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		private bool IsSpecialPeriod(int ich)
		{
			// As described above, a period that is not the first in a sequence can't end a segment.
			if (m_prevCh == 0x002E)
				return true;
			// Can't be special if no following character.
			if (ich >= m_paraText.Length - 1)
				return false;
			int chNext = WordMaker.FullCharAt(m_paraText, ich + 1); // +1 is safe because ch at ich is period (not half of surrogate)
			if (chNext == 0x002E)
			{
				// At least two periods...do we have three?
				if (ich >= m_paraText.Length - 2)
					return false; // only 2, not ellipsis
				if (WordMaker.FullCharAt(m_paraText, ich + 2) != 0x002E)
					return false; // exactly two periods is never special
				if (ich >= m_paraText.Length - 3)
					return true; // exactly 3 by end of string, this is the start of an ellipsis.
				// If the fourth character is a period, we do NOT have a proper ellipsis, so the first period is NOT special
				// If it IS NOT a period, we have exactly three, so it IS special.
				return WordMaker.FullCharAt(m_paraText, ich + 3) != 0x002E;
			}
			// No following period, so not an ellipsis. Special exactly if numbers on both sides.
			// No need currently to ensure correct cpe, category is not yet ws-dependent.
			return m_cpe.get_GeneralCategory(m_prevCh) == LgGeneralCharCategory.kccNd &&
				   m_cpe.get_GeneralCategory(chNext) == LgGeneralCharCategory.kccNd;
		}

		/// <summary>
		/// Test whether a run of text in a TsString contains 'label' text which should cause a segment break.
		/// (Current definition is that there are characters in the range with the Scripture styles VerseNumber
		/// or ChapterNumber, there is a single-character run which contains an ORC (0fffc), or the range being
		/// tested is a single character (possibly part of a longer run) which is a hard line break.
		/// </summary>
		public static bool HasLabelText(ITsString tss, int ichMin, int ichLim)
		{
			// True if the run at ichMin has one of the interesting styles or is an ORC.
			int irun = tss.get_RunAt(ichMin);
			if (IsLabelText(tss, irun))
				return true;
			// See if any later run in the character range has the required style or is an ORC.
			int crun = tss.RunCount;
			for (irun++; irun < crun && tss.get_MinOfRun(irun) < ichLim; irun++)
			{
				if (IsLabelText(tss, irun))
					return true;
			}
			// All other ways it might be treated as a label have failed, so return false
			// unless it is a one-character range containing a hard line break.
			if (ichLim == ichMin + 1)
				return tss.GetChars(ichMin, ichLim) == "\x2028";
			return false;
		}

		/// <summary>
		/// Test whether a run in a TsString contains 'label' text which should cause a segment break.
		/// True if it has one of the interesting styles or the whole run is an ORC.
		/// Nb: this method won't detect hard line breaks.
		/// </summary>
		private static bool IsLabelText(ITsString tss, int irun)
		{
			ITsTextProps ttp = tss.get_Properties(irun);
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			if (styleName == ScrStyleNames.VerseNumber || styleName == ScrStyleNames.ChapterNumber)
				return true;
			// Even with no interesting style, an ORC is always considered 'label' text.
			// Hard line breaks are also forced to be their own segment.
			string runText = tss.get_RunText(irun);
			return (runText == "\xfffc");
		}

		int m_csegs = 0;
		internal virtual void CreateSegment(int ichMin, int ichLim)
		{
			m_csegs++;
		}
	}

	internal class SegmentMaker: SegmentBreaker
	{
		private ParagraphParser m_paraParaser;
		List<int> m_segments = new List<int>();

		internal SegmentMaker(ITsString text, ILgWritingSystemFactory wsf, ParagraphParser pp)
			: base(text, wsf)
		{
			m_paraParaser = pp;
		}

		internal override void CreateSegment(int ichMin, int ichLim)
		{
			base.CreateSegment(ichMin, ichLim);
			m_segments.Add(m_paraParaser.CreateSegment(ichMin, ichLim));
		}

		/// <summary>
		/// The HVOs of the CmBaseAnntations created to be the segments.
		/// </summary>
		internal List<int> Segments { get { return m_segments; } }
	}

	/// <summary>
	/// This class collects up the segments that would be needed for a chunk of text, but does not actuallly
	/// create CmBaseAnnotation segments for them. It is typically used to parse CmTranslations rather than
	/// actual paragraphs.
	/// </summary>
	public class SegmentCollector : SegmentBreaker
	{
		private List<TsStringSegment> m_segments = new List<TsStringSegment>();
		public SegmentCollector(ITsString text, ILgWritingSystemFactory wsf) : base(text, wsf)
		{
		}

		public List<TsStringSegment> Segments { get { return m_segments; } }
		internal override void CreateSegment(int ichMin, int ichLim)
		{
			base.CreateSegment(ichMin, ichLim);
			m_segments.Add(new TsStringSegment(ichMin, ichLim, tssText));
		}
	}

	/// <summary>
	/// Stores a TsString and an indication of an interesting range within it.
	/// </summary>
	public class TsStringSegment
	{
		private int m_ichMin;
		private int m_ichLim;
		private ITsString m_text;

		public TsStringSegment(int ichMin, int ichLim, ITsString baseText)
		{
			m_ichMin = ichMin;
			m_ichLim = ichLim;
			ITsStrBldr bldr = baseText.GetBldr();
			if (m_ichLim < bldr.Length)
				bldr.ReplaceTsString(m_ichLim, bldr.Length, null);
			if (m_ichMin > 0)
				bldr.ReplaceTsString(0, m_ichMin, null);
			m_text = bldr.GetString();
		}

		public int BeginOffset { get { return m_ichMin; } }
		public int EndOffset { get { return m_ichLim; } }
		public ITsString Text
		{
			get
			{
				return m_text;
			}
		}
	}
}
