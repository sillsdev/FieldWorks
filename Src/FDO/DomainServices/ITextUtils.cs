// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2003' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ITextUtils.cs
// Responsibility: John Thomson
// --------------------------------------------------------------------------------------------
#define PROFILING
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region ParagraphParserOptions class
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
		public ParagraphParserOptions(bool fBuildConcordance, bool fResetConcordance)
		{
			CollectWordformOccurrencesInTexts = fBuildConcordance;
			ResetConcordance = fResetConcordance;
		}
		/// <summary>
		/// if true, collect wordform occurrences
		/// </summary>
		public bool CollectWordformOccurrencesInTexts;
		/// <summary>
		/// if true, reset the wordform inventory concordance before starting.
		/// </summary>
		public bool ResetConcordance;
	}
	#endregion

	#region ReusableCbaItem class
	internal class ReusableCbaItem
	{
		internal ReusableCbaItem(ICmBaseAnnotation cba)
		{
			Item = cba;
		}
		internal ICmBaseAnnotation Item { get; private set; }
		internal void Reuse()
		{
			Reused = true;
		}
		internal static void MarkToRemove(ICmBaseAnnotation cbaToRemove, IList<ReusableCbaItem> reusableItems)
		{
			foreach (ReusableCbaItem item in reusableItems)
			{
				if (item == cbaToRemove)
				{
					item.MarkedToRemove = true;
					return;
				}
			}

		}
		/// <summary>
		/// indicate that we want to delete this.
		/// </summary>
		internal bool MarkedToRemove { get; private set; }
		internal bool Reused { get; private set; }
	}
	#endregion

	#region ParagraphParser class
	/// <summary>
	/// For tokenizing StTxtParas with segments, words and punctuation.
	/// </summary>
	public class ParagraphParser : IFWDisposable
	{
		int m_hvoText = 0; // set if processing whole text.
		int m_cparas = -1; // set if processing whole text.
		/// <summary> The paragraph to be parsed. </summary>
		protected IStTxtPara m_para;
		private ITsString m_tssPara;
		private WordMaker m_wordMaker;
		/// <summary> </summary>
		protected FdoCache m_cache;

		IWfiWordformRepository m_wfr;
		IWfiWordformFactory m_wordfactory;
		ICmBaseAnnotationFactory m_cbaf;
		/// <summary> the repository for getting annotations </summary>
		protected ICmBaseAnnotationRepository m_cbar;
		int m_paraWs = 0;
		// keeps track of the paragraph ids we've parsed.
		// If not null, list of existing annotation objects not yet reused.
		// When one is reused, it is changed to zero.
		Dictionary<string, HashSet<ITsString>> m_possiblePhrases;
		static FdoCache s_cacheForRealPhrases = null;
		bool m_fSegmentFormCollectionMode;  // used to collect (and restore state of ParagraphParser).
		// NB: Order is important to these three lists.
		readonly List<IAnalysis> m_preExistingAnalyses = new List<IAnalysis>();
		readonly List<ISegment> m_preExistingSegs = new List<ISegment>();
		WordMaker m_paragraphTextScanner;
		IDictionary<string, IList<int>> m_wordformAnnotationPossibilities = new Dictionary<string, IList<int>>();
		// Variables used for profiling
#if PROFILING
		int m_cAnnotations = 0;
		long m_cTicksMakingAnnotations = 0;
		static int s_cTotalAnnotationsMade = 0;
#endif
		bool m_fRebuildingConcordanceWordforms;  // true when we're rebuilding ConcordanceWordforms
		bool m_fAddOccurrencesToWordforms;  // true when recording the occurrences of a wordform in text(s).

		// This Set is used to keep track of the wordformIds we've identified through a parse.
		// On 12/2/2006, when I (RandyR) switched it to use a Set,
		// the values (also ints) weren't being used at all.
		static readonly HashSet<int> s_wordformIdOccurrencesTable = new HashSet<int>();
		static FdoCache s_cache;

		FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
		}

		// Caches the WF repository's collection of possible phrases organized by first word.
		Dictionary<string, HashSet<ITsString>> PossiblePhrases
		{
			get
			{
				if (m_possiblePhrases == null)
					m_possiblePhrases = ((IWfiWordformRepositoryInternal)m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>())
						.FirstWordToPhrases;
				return m_possiblePhrases;
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
		internal HashSet<int> WordformIdOccurrencesTable
		{
			get
			{
				CheckDisposed();

				if (s_cache == null || s_cache != m_cache)
					return new HashSet<int>();
				return s_wordformIdOccurrencesTable;
			}
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
				CollectWordformOccurrencesInTexts = value;
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

		/// <summary>
		/// Session that parses through a paragraph, and collects information for it.
		/// </summary>
		public static void ParseParagraph(IStTxtPara para)
		{
			ParseParagraph(para, false);
		}

		/// <summary>
		///
		/// </summary>
		public static void ParseParagraph(IStTxtPara para, bool fBuildConcordance)
		{
			ParseParagraph(para, fBuildConcordance, false);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="para"></param>
		/// <param name="fBuildConcordance">if true, collect wordform occurrences</param>
		/// <param name="fResetConcordance">if true, reset the wordform inventory concordance</param>
		public static void ParseParagraph(IStTxtPara para, bool fBuildConcordance, bool fResetConcordance)
		{
			ParseParagraph(para, new ParagraphParserOptions(fBuildConcordance, fResetConcordance));
		}

		/// <summary>
		/// Parse a single paragraph with the specified options.
		/// </summary>
		public static void ParseParagraph(IStTxtPara para, ParagraphParserOptions options)
		{
			if (para.ParseIsCurrent)
				return;
			using (var pp = new ParagraphParser(para.Cache))
			{
				pp.ParseWithOptions(para, options);
			}
		}

		private void ParseWithOptions(IStTxtPara para, ParagraphParserOptions options)
		{
			ParseWithOptionsCore(para, options);
		}

		private void ParseWithOptionsCore(IStTxtPara para, ParagraphParserOptions options)
		{
			ResetParseSessionDependentStaticData();
			CollectWordformOccurrencesInTexts = options.CollectWordformOccurrencesInTexts;
			Parse(para);
		}

		/// <summary>
		/// Parse all the paragraphs in the text.
		/// </summary>
		public static void ParseText(IStText sttext)
		{
			using (var parser = new ParagraphParser(sttext.Cache))
			{
				foreach (IStTxtPara para in sttext.ParagraphsOS)
					parser.Parse(para);
			}
		}

		/// <summary>
		/// tokenize the paragraph with segments and analyses (wordforms generally, though we try to preserve other existing ones).
		/// </summary>
		/// <param name="para"></param>
		public void Parse(IStTxtPara para)
		{
			if (para.ParseIsCurrent)
				return; // not needed.
			ParseCore(para);
		}

		/// <summary>
		/// tokenize the paragraph with segments and analyses (wordforms generally, though we try to preserve other existing ones).
		/// </summary>
		/// <param name="para"></param>
		public void ForceParse(IStTxtPara para)
		{
			ParseCore(para);
		}

		private void ParseCore(IStTxtPara para)
		{

			Setup(para);
			// Collect pre-existing annotations for paragraph.
			CollectPreExistingParaAnnotations();
			//BuildAnalysisList(new NullProgressState());	// load any existing data from the database.
			Parse();
			para.ParseIsCurrent = true;
		}

		internal void CollectPreExistingParaAnnotations()
		{
			m_preExistingAnalyses.Clear();
			m_preExistingSegs.Clear();

			foreach (var seg in m_para.SegmentsOS)
			{
				m_preExistingSegs.Add(seg);
				m_preExistingAnalyses.AddRange(from analysis in seg.AnalysesRS where analysis.Wordform != null select analysis);
			}
		}

		/// <summary>
		///
		/// </summary>
		public ParagraphParser(FdoCache cache)
		{
			Init(cache);
		}

		/// <summary>
		///
		/// </summary>
		public ParagraphParser(IStTxtPara para)
			: this(para.Cache)
		{
			Setup(para);
		}

		private void Init(FdoCache cache)
		{
			m_cache = cache;
			m_paragraphTextScanner = new WordMaker(null, cache.WritingSystemFactory);
			m_wfr = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			m_wordfactory = m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>();
			m_cbaf = m_cache.ServiceLocator.GetInstance<ICmBaseAnnotationFactory>();
			m_cbar = m_cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>();
		}

		/// <summary>
		/// if parsing over multiple paragraphs, use this to setup the state before
		/// </summary>
		/// <param name="para"></param>
		private void Setup(IStTxtPara para)
		{
			m_para = para;
			m_tssPara = para.Contents;

			// must prevent a first para.seg.word in an analysis ws from corrupting the parse
			// only word forms in this ws will have analyses, the rest are turned into punctuation! LT-12304
			// Until a model change is made to store the user's preffered vernacular ws,
			// the user will always be able to defeat whatever vern ws we use here.
			// For now, look for the first vern ws in the baseline text
			m_paraWs = TsStringUtils.GetFirstVernacularWs(m_para.Cache.LanguageProject.VernWss, m_para.Services.WritingSystemFactory, m_para.Contents);
			if (m_paraWs <= 0)
				m_paraWs = m_cache.DefaultVernWs;
			m_wordMaker = new WordMaker(m_tssPara, para.Cache.WritingSystemFactory);
			m_paragraphTextScanner.Tss = m_tssPara;
		}

		/// <summary>
		/// Creates a single punctuation annotation for the specified range.
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		private IAnalysis CreatePunctAnnotation(int ichMin, int ichLim)
		{
			return WfiWordformServices.FindOrCreatePunctuationform(m_cache, m_para.Contents.GetSubstring(ichMin, ichLim));
		}

		/// <summary>
		/// Here ichMin..Lim indicates a (possibly empty) range of characters between two words,
		/// or before the first word or after the last. If this range contains anything other than
		/// white space (typically punctuation), make one or more extra annotations for each
		/// group of white-space-separated characters in the range.
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="annotationIds">Append ids of new annotations here.</param>
		private void CreatePunctAnnotations(int ichMin, int ichLim, IList<IAnalysis> annotationIds)
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

		// Do the actual parsing.
		private void Parse()
		{
			s_cache = m_cache;
			// track the ids for this paragraph that we'll try to reuse.
			SetupPossibleIndicesForWordform();

			// Create (or reuse if possible) segment annotations.
			List<int> segBreaksDummy;
			IList<ISegment> segments = CollectSegmentsOfPara(out segBreaksDummy);
			int ichLimLast = 0;
			int ichLimCurSeg = Int32.MaxValue;
			ITsString tssFirstWordOfNextSegment = null;
			int cWfanalysis = 0;
			foreach (var seg in segments)
			{
				ichLimCurSeg = seg.EndOffset;
				var newAnalyses = (from  analysis in CollectSegmentForms(m_wordMaker.CurrentCharOffset,
					ichLimCurSeg, ref cWfanalysis, ref ichLimLast, ref tssFirstWordOfNextSegment) select analysis as ICmObject).ToArray();
				if (AnalysesChanged(seg, newAnalyses))
					seg.AnalysesRS.Replace(0, seg.AnalysesRS.Count, newAnalyses);
			}
		}

		/// <summary>
		/// Return true if the newly computed list of analyses is different from the current list.
		/// </summary>
		private bool AnalysesChanged(ISegment seg, ICmObject[] newAnalyses)
		{
			if (seg.AnalysesRS.Count != newAnalyses.Length)
				return true;
			for (int i = 0; i < newAnalyses.Length; i++)
			{
				if (seg.AnalysesRS[i] != newAnalyses[i])
					return true;
			}
			return false;
		}


		/// <summary>
		/// develop a map for wordform to corresponding indices in the paragraph where that wordform existed in the old
		/// list of wordform Analyses of the paragraph's segments. The indices represent a position in the list of wordforms
		/// (not counting punctuation). It's important not to count punctuation, because the paragraph parser is used
		/// to verify that all is well after data migration. The old FieldWorks (6.0 and before) did not have persistent
		/// punctuation annotations, so if we count them in determining the expected position of an annotation, we end
		/// up looking too late in the list, when migrating something that doesn't have them to start with. OTOH, if we
		/// for some reason re-parse a paragraph that does have punctuation annotations included, if we counted them here,
		/// that would throw us off in the other direction.
		/// </summary>
		private void SetupPossibleIndicesForWordform()
		{
			m_wordformAnnotationPossibilities.Clear();
			for (int i = 0; i < m_preExistingAnalyses.Count; i++ )
			{
				var analysis = m_preExistingAnalyses[i];

				// NOTE: for phrase annotations, use the first word as a key (LT-5856)
				string annFirstWordformLowered;
				ITsString firstWord = FirstWord(analysis.Wordform.Form.get_String(m_paraWs), Cache.WritingSystemFactory,
												out annFirstWordformLowered);
				if (firstWord != null)
				{
					string key = annFirstWordformLowered;
					IList<int> possibleIndices = null;
					if (!m_wordformAnnotationPossibilities.TryGetValue(key, out possibleIndices))
					{
						// create a list of indices for wordform-annotations in this paragraph.
						possibleIndices = new List<int>();
						m_wordformAnnotationPossibilities.Add(key, possibleIndices);
					}
					possibleIndices.Add(i);
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
			int wsTxtWord = TsStringUtils.GetWsAtOffset(tssTxtWord, 0);
			if (hvoMatchingWordform == 0)
			{
				// return a candidate if it matches the ws we're trying to query.
				return wsMatchQuery == wsTxtWord ? tssWordAnn : null;
			}
			// if the ws of the matcher doesn't match the ws of the baseline
			// find the wordform in an alternative ws.
			if (wsMatchQuery != wsTxtWord)
			{
				tssWff = sda.get_MultiStringAlt(hvoMatchingWordform, WfiWordformTags.kflidForm, wsMatchQuery);
			}
			return tssWff;
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
		internal IList<ISegment> CollectSegments(ITsString tssText, out List<int> ichMinSegBreaks)
		{
			Debug.Assert(m_para != null);
			// Get the information we need to reuse existing annotations if possible.
			m_preExistingSegs.Clear();
			m_preExistingSegs.AddRange(m_para.SegmentsOS);
			var collector = new SegmentMaker(tssText, m_cache.WritingSystemFactory, this);
			collector.Run();
			ichMinSegBreaks = collector.EosPositions;
			if (m_preExistingSegs.Count > 0)
			{
				// Delete left-over segments.
				// Enhance JohnT: should we copy their annotations into the last surviving segment if any?
				m_para.SegmentsOS.Replace(m_para.SegmentsOS.Count - m_preExistingSegs.Count, m_preExistingSegs.Count, new ICmObject[0]);
				m_preExistingSegs.Clear(); // I (JohnT) don't think it will be used again, but play safe
			}
			return collector.Segments;
		}

		/// <summary>
		/// This is very similar to CollectSegments on the base class, but does not make
		/// even dummy annotations, just TsStringSegments.
		/// </summary>
		/// <param name="tssText"></param>
		/// <param name="ichMinSegBreaks"></param>
		/// <returns></returns>
		internal List<TsStringSegment> CollectTempSegmentAnnotations(ITsString tssText, out List<int> ichMinSegBreaks)
		{
			SegmentCollector collector = new SegmentCollector(tssText, m_cache.WritingSystemFactory);
			collector.Run();
			ichMinSegBreaks = collector.EosPositions;
			return collector.Segments;
		}


		/// <summary>
		/// Collect existing segments, if possible reusing existing ones, for the paragraph passed to the constructor.
		/// This is now just a pseudonym, since the main routine also needs to reuse existing segments.
		/// </summary>
		internal IList<ISegment> CollectSegmentsOfPara(out List<int> ichMinSegBreaks)
		{
			return CollectSegments(m_para.Contents, out ichMinSegBreaks);
			}

		/// <summary>
		/// Returns the first word in the given tssWordAnn and its lower case form.
		/// </summary>
		/// <param name="tssWordAnn"></param>
		/// <param name="wsf"></param>
		/// <param name="firstFormLowered"></param>
		/// <returns>null if we couldn't find a word in the given tssWordAnn</returns>
		static internal ITsString FirstWord(ITsString tssWordAnn, ILgWritingSystemFactory wsf, out string firstFormLowered)
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
											cache.WritingSystemFactory, out firstFormLowered);
			// Handle null values without crashing.  See LT-6309 for how this can happen.
			return firstWord != null && firstWord.Length < tssWordAnn.Length;
		}

		/// <summary>
		/// Collects SegmentForms between ichMinCurSeg to ichLimCurSeg.
		/// </summary>
		/// <param name="ichMinCurSeg"></param>
		/// <param name="ichLimCurSeg"></param>
		/// <param name="cWfAnalysisPrev">number of previous wordform analyses in paragraph (indicates where to start looking in reuse list)</param>
		/// <param name="fUpdateRealData">if false, ParagraphParser only creates dummy annotations for the segment,
		/// but doesn't modify any real annotation or change the state of ParagraphParser.</param>
		/// <returns></returns>
		internal IList<IAnalysis> CollectSegmentForms(int ichMinCurSeg, int ichLimCurSeg, int cWfAnalysisPrev, bool fUpdateRealData)
		{
			// if we don't want to modify real data, then put ParagraphParser in SegmentFormCollectionMode.
			SegmentFormCollectionMode = !fUpdateRealData;
			// Save current state of ParagraphParser.
			int originalParagraphOffset = m_wordMaker.CurrentCharOffset;

			try
			{
				int ichLimLast = ichMinCurSeg;
				ITsString tssFirstWordOfNextSegment = null;
				int copyOfcWfAnalysisPrev = cWfAnalysisPrev;
				return CollectSegmentForms(ichMinCurSeg, ichLimCurSeg, ref copyOfcWfAnalysisPrev, ref ichLimLast, ref tssFirstWordOfNextSegment);
			}
			finally
			{
				if (SegmentFormCollectionMode)
				{
					// Restore state of ParagraphParser.
					m_wordMaker.CurrentCharOffset = originalParagraphOffset;
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
		/// <param name="cwfAnalysisPrev">number of previous wordform analyses in paragraph (indicates where to start looking in reuse list)</param>
		/// <param name="phraseSegmentForms">SegmentForms that match in this paragraph.</param>
		/// <param name="iFirstSegmentFormWithNonTrivialAnalyis">index of the first segmentForm with a real analysis in phraseSegmentForms.
		/// -1 if they're all wordform analyses.</param>
		/// <returns>true, if phraseSegmentForms contains an annotation with a significant analysis (i.e. other than wordform).</returns>
		internal bool TryRealSegmentFormsInPhrase(int ichMinCurPhrase, int ichLimCurPhrase, int cwfAnalysisPrev, out IList<IAnalysis> phraseSegmentForms,
												  out int iFirstSegmentFormWithNonTrivialAnalyis)
		{
			phraseSegmentForms = CollectSegmentForms(ichMinCurPhrase, ichLimCurPhrase, cwfAnalysisPrev, false);
			// search for the first non-wordform analyses.
			return HasNonTrivialAnalysis(phraseSegmentForms, out iFirstSegmentFormWithNonTrivialAnalyis);
		}

		/// <summary>
		/// Find the first
		/// </summary>
		/// <param name="segmentForms"></param>
		/// <param name="iFirstSegmentFormWithNonTrivialAnalyis"></param>
		/// <returns></returns>
		private bool HasNonTrivialAnalysis(IList<IAnalysis> segmentForms, out int iFirstSegmentFormWithNonTrivialAnalyis)
		{
			iFirstSegmentFormWithNonTrivialAnalyis = -1;
			int iSegForm = 0;
			foreach (IAnalysis analysis in segmentForms)
			{
				if (!HasTrivialAnalysis(analysis))
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


		private IList<IAnalysis> CollectSegmentForms(int ichMinCurSeg, int ichLimCurSeg, ref int cWfAnalysisPrev, ref int ichLimLast,
											  ref ITsString tssFirstWordOfNextSegment)
		{
			var formsInSegment = new List<IAnalysis>();
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
			do
			{
				if (tssWord == null)
				{
					// we've run out of non-punctuation text. collect the last remaining punctuation annotations.
					//Debug.Assert(m_tssPara.Length == ichLimCurSeg);
					CreatePunctAnnotations(ichLimLast, ichLimCurSeg, formsInSegment);
					break;
				}

				if (ichLimLast != ichMin)
				{
					// we need to add punctuations to the current segment.
					CreatePunctAnnotations(ichLimLast, Math.Min(ichMin, ichLimCurSeg), formsInSegment);
					if (ichMin >= ichLimCurSeg)
					{
						// we need to add this wordform to the next segment.
						tssFirstWordOfNextSegment = tssWord;
						ichLimLast = ichLimCurSeg;
						break;
					}
				}
				if (TsStringUtils.GetWsAtOffset(m_tssPara, ichMin) != m_paraWs)
				{
					formsInSegment.Add(CreatePunctAnnotation(ichMin, ichLim));
					ichLimLast = ichLim; // We've done something with text up to here.
				}
				else
				{
					// Make a reference to a wordform; or if we can match it in the original analyses,
					// preserve the reference to one of its analyses or glosses.
					ITsString tssWordAnn;
					formsInSegment.Add(CreateOrReuseAnnotation(tssWord, ichMin, ichLim, cWfAnalysisPrev, out tssWordAnn));
					cWfAnalysisPrev++;
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
				}
				tssWord = m_wordMaker.NextWord(out ichMin, out ichLim);
			} while (true);
			return formsInSegment;
		}

		internal ISegment CreateSegment(int ichMin, int ichLim)
		{
			// NOTE: This code is similar to ParagraphParserForEditMonitoring.TryReuseFirstUnusedAnnotation
			// but is not as stricted at handling the conditions
			Segment unusedSeg = (Segment)m_preExistingSegs.FirstOrDefault();
			if (unusedSeg != null)
			{
				// Reuse it.
				// It's conceivable that it belongs to a later sentence, but we have AnnotationAdjuster to try to avoid that.
				m_preExistingSegs.RemoveAt(0);
				unusedSeg.BeginOffset = ichMin;
				return unusedSeg;
			}
			// Review JohnT: do we always have a current para when calling this?
			// Do we always want to put the new segment at the end of it?
			return ((SegmentFactory)m_para.Services.GetInstance<ISegmentFactory>()).Create(m_para, ichMin);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="reusableCbaItems"></param>
		/// <param name="ichMin">begin offset in actual text</param>
		/// <param name="ichLim">end offset in actual text</param>
		/// <param name="cbaFirstUnused">the first unused cba, whether or not we could reuse it.</param>
		/// <returns>true if we reused it.</returns>
		internal bool TryReuseFirstUnusedCbaMatchingText(IList<ReusableCbaItem> reusableCbaItems, int ichMin, int ichLim,
														  out ICmBaseAnnotation cbaFirstUnused)
		{
			cbaFirstUnused = null;
			ReusableCbaItem unusedCbaItem = UnusedCbaItems(reusableCbaItems).FirstOrDefault();
			if (unusedCbaItem != null)
			{
				ICmBaseAnnotation unusedCba = unusedCbaItem.Item;
				if (unusedCba.BeginOffset == ichMin && unusedCba.EndOffset == ichLim)
				{
					cbaFirstUnused = unusedCba;
					unusedCbaItem.Reuse();
					return true;
				}
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="reusableCbas"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="cbaUsed"></param>
		/// <returns></returns>
		internal virtual bool TryReuseFirstUnusedAnnotation(IList<ReusableCbaItem> reusableCbas, int ichMin, int ichLim, out ICmBaseAnnotation cbaUsed)
		{
			cbaUsed = null;
			// see if we can reuse an annotation from the current paragraph.
			ReusableCbaItem rci = UnusedCbaItems(reusableCbas).FirstOrDefault();
			if (rci != null)
			{
				rci.Reuse();
				cbaUsed = rci.Item;
			}
			return cbaUsed != null;
		}

		// Verify that the annotation matches the offsets. They should have been cached in BuildAnalysisList();
		//
		bool HasValidOffsets(int hvoAnnotation, int ichMin, int ichLim)
		{
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			Debug.Assert(sda.get_IsPropInCache(hvoAnnotation, CmBaseAnnotationTags.kflidBeginOffset,
				(int)CellarPropertyType.Integer, 0), "We expect BuildAnalysisList() to cache the annotation offsets.");
			return ichMin == sda.get_IntProp(hvoAnnotation, CmBaseAnnotationTags.kflidBeginOffset) &&
				   ichLim == sda.get_IntProp(hvoAnnotation, CmBaseAnnotationTags.kflidEndOffset) &&
				   m_para.Hvo == sda.get_ObjectProp(hvoAnnotation, CmBaseAnnotationTags.kflidBeginObject);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		static protected int IndexOfFirstUnusedId(List<int> ids)
		{
			Debug.Assert(ids != null);
			return IndexOfFirstUnusedId(ids.ToArray());
		}

		static internal IEnumerable<ReusableCbaItem> UnusedCbaItems(IList<ReusableCbaItem> cbaItems)
		{
			return cbaItems.Where(cbaItem => !cbaItem.Reused);
		}

		static int IndexOfFirstUnusedId(int[] ids)
		{
			var i = 0;
			for (; i < ids.Length && ids[i] == 0; ++i)
			{}
			return i;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		/// <param name="cbaItems"></param>
		/// <returns></returns>
		internal ICmBaseAnnotation UseCba(int index, IList<ReusableCbaItem> cbaItems)
		{
			Debug.Assert(index >= 0 && index < cbaItems.Count);
			ICmBaseAnnotation cba = cbaItems[index].Item;
			if (!SegmentFormCollectionMode)
				cbaItems[index].Reuse();
			return cba;
		}

		bool TryReuseAnalysis(ITsString tssTxtWord, int ichMin, int ichLim, int ianalysis, out IWfiWordform wf, out ITsString tssWordAnn, out IAnalysis analysis)
		{
			analysis = null;
			tssWordAnn = tssTxtWord;
			wf = null;	// default
			if (m_preExistingAnalyses.Count == 0)
			{
				// Enhance: This is a new paragraph that may have resulted in breaking
				// up a previously existing paragraph. In that case we'd probably want to
				// try to preserve the annotations/analyses.
				return false;
			}

			// First, see if we have cached annotation ids for the wordform in this paragraph.
			// the wordform and its lowercase form may already be in the cache.
			string key = m_paragraphTextScanner.ToLower(tssTxtWord);
			IList<int> possibleIndices;
			if (!m_wordformAnnotationPossibilities.TryGetValue(key, out possibleIndices) || possibleIndices.Count == 0)
				return false; // we don't have any remaining annotations matching this wordform.
				possibleIndices = m_wordformAnnotationPossibilities[key];

			int iAnnClosest;
			GetBestPossibleAnnotation(ianalysis, possibleIndices, ichMin, ichLim - ichMin, out iAnnClosest);
			if (iAnnClosest == -1)
				return false;	// can happen, if all the possible reuseable ones are actually phrases.

			bool fUsedBestPossible = false;
			try
			{
				wf = m_preExistingAnalyses[iAnnClosest].Wordform;

				int wsTxtWord = TsStringUtils.GetWsAtOffset(tssTxtWord, 0);
				tssWordAnn = wf.Form.get_String(wsTxtWord);

				// Did we find it at the exact expected place in the sequence?
				if (ianalysis != iAnnClosest)
				{
					// No, we didn't. Apply various heuristics to see whether we should use it.

					// Enhance: If the character offsets in this text paragraph overlap
					// the user probably deleted a paragraph break.
					// in that case we could be smarter about looking  for matches at the end of the paragraph
					// rather than at the beginning.

					// Verify the closest is within reasonable bounds.
					if (Math.Abs(ianalysis - iAnnClosest) > 100)
					{
						// someone may have significantly altered the text,
						// or it belongs to a wordform later in the text.
						// Either case, safe not to guess a match.
						tssWordAnn = tssTxtWord;
						return false;
					}

					if (tssWordAnn.Length == 0)
					{
						// There are certain cases (e.g. during import) where
						// the paragraph text will not have a default vernacular writing system for the current
						// wordform. So, instead of trying to find the best vernacular form (again), we'll
						// just assume that it matches some form of the tssWordTxt in this context.
						tssWordAnn = tssTxtWord;
					}

					// JohnT: don't see any equivalent for this optimization, we don't know where in the
					// text the closest occurrence used to be.
					//// see if we can match on the target offset, so we don't have to search more through the text.
					//if (m_paragraphTextScanner.MatchesWordInText(tssWordAnn, ichMinAnnClosest))
					//{
					//    tssWordAnn = tssTxtWord;
					//    return false;
					//}
					// Otherwise, verify there isn't another place in the text closer to the
					// offsets for the annotation's wordform.
					if (ianalysis < iAnnClosest)
					{
						int intermediateWordCount = m_paragraphTextScanner.NextOccurrenceOfWord(tssWordAnn, ichLim,
																								ichMin +
																								tssWordAnn.Length);
						if (intermediateWordCount != -1 && intermediateWordCount < iAnnClosest - ianalysis)
						{
							// we found a closer possible occurrence.
							tssWordAnn = tssTxtWord;
							return false;
						}
					}
					else
					{
						// the match is earlier in the text than expected; it will certainly be closer
						// to this occurrence than any later one.
					}
				}
				else
				{
					// The same wordform occurred at the same position in the old analysis of the paragraph.
					int ichLimAnnClosest = m_preExistingAnalyses[iAnnClosest].GetForm(m_paraWs).Length + ichMin;
					if (ichLimAnnClosest > ichLim)
					{
						// annotation must be for a phrase. See if it matches our text before using the id, otherwise we can crash (LT-6244).
						if (!m_paragraphTextScanner.MatchesWordInText(tssWordAnn, ichMin))
						{
							// annotation didn't actually match our text, so return.
							tssWordAnn = tssTxtWord;
							return false;
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
							return false;
						}
					}
				}
				// must have found an annotation we can reuse.
				fUsedBestPossible = true;
				analysis = m_preExistingAnalyses[iAnnClosest];
				return true;
			}
			finally
			{
				// remove any annotation possibilities that no longer make sense.
				if (!SegmentFormCollectionMode)
				{
					int iBestPossible = possibleIndices.IndexOf(iAnnClosest);
					int cRemove = iBestPossible + (fUsedBestPossible ? 1 : 0);
					if (cRemove > 0)
						(possibleIndices as List<int>).RemoveRange(0, cRemove);
					if (possibleIndices.Count == 0)
					{
						// remove its key from the Dictionary.
						m_wordformAnnotationPossibilities.Remove(key);
					}
				}
			}
		}

		/// <summary>
		/// Get the best possible analysis for some wordform, given that we will put this analysis at index ianalysis
		/// in the paragraph as a whole, and that in a previous parse, analyses of the same wordform were found at
		/// possibleIndices.
		/// May return -1, if all the possible analyses for this word are actually phrases that don't match.
		/// </summary>
		/// <param name="ianalysis"></param>
		/// <param name="possibleIndices"></param>
		/// <param name="ichMin">The location in m_paragraphTextScanner where we need a match. A candidate wordform
		/// which is a phrase must be checked to see whether all its text occurs there.</param>
		/// <param name="length">of the wordform used as a key. We may match a longer wordform (a phrase), but only
		/// with great care to make sure it matches the input text properly.</param>
		/// <param name="iAnnClosest">One of possibleIndices, an index into m_preexistingAnalyses as close as possible to ianalysis</param>
		private void GetBestPossibleAnnotation(int ianalysis, IList<int> possibleIndices, int ichMin, int length, out int iAnnClosest)
		{
			iAnnClosest = -1;
			foreach (int i in possibleIndices ) // i lie ianalysis is an index into m_prexistingAnalyses
			{
				if (! IsAPossibleMatch(i, ichMin, length))
					continue; // wordform at i is a phrase that doesn't actually match the text here.
				// find a wordform occurrence analysis in the right paragraph that matches the current wordform in our text.
				// Best case: if the text offset matches the annotation offset, we found a match.
				if (ianalysis == i)
				{
					// exact match.
					iAnnClosest = i;
					break;
				}
				if (ianalysis > i)
				{
					// either someone 1) inserted some text, or 2) someone has deleted an earlier occurrence of this wordform.
					// if we find another annotation that has a closer offset, in the first case, we might accidentally
					// assign an analysis that belongs to another later in the text, to an earlier one.
					// In the second case, we might accidentally reassign the deleted wordform's analysis to a subsequent
					// matching wordform.

					// for now, we'll just try to find an annotation that has a closer offset match.
					iAnnClosest = i;
					continue;
				}
				else
				{
					// someone may have deleted a significant amount of text
					// or this annotation belongs to another occurrence of the wordform later
					// in the text.
					if (iAnnClosest == -1 ||
						Math.Abs(ianalysis - i) < Math.Abs(ianalysis - iAnnClosest))
					{
						iAnnClosest = i;
					}
					break;	// other annotations are further away so quit.
				}
			}
		}

		/// <summary>
		/// Answer whether the wordform a index i in m_preExistingAnalyses is a possible match for the text
		/// at ichMin in m_paragraphTextScanner. We have come up with these indices by matching a complete
		/// wordform scanned against possible wordforms to reuse, and the one at i is one of them. Therefore,
		/// if the wordform is not a phrase, we can reuse it unconditionally. However, if it is a phrase,
		/// we need to check both that its full text occurs, and that the following input character if any
		/// is not wordforming.
		/// Optimize JohnT:
		/// </summary>
		private bool IsAPossibleMatch(int i, int ichMin, int length)
		{
			var wf = m_preExistingAnalyses[i];
			var ws = m_paragraphTextScanner.WsAtOffset(ichMin);
			var wordformText = wf.Wordform.Form.get_String(ws);
			if (wordformText.Length == length)
				return true; // not a phrase, no need to check further.
			return m_paragraphTextScanner.MatchesWordInText(wordformText, ichMin);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tssWordTextIn"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="iWfAnalysis">index of the analysis we will create in the overall list of wordform analyses for the paragraph.</param>
		/// <param name="tssWordAnn">the wordform of the matching annotation (could be a phrase).</param>
		/// <returns>the new or reused annotation id</returns>
		private IAnalysis CreateOrReuseAnnotation(ITsString tssWordTextIn, int ichMin, int ichLim, int iWfAnalysis, out ITsString tssWordAnn)
		{
			ITsString tssWordText = tssWordTextIn.ToWsOnlyString();
			// First see if we can find a real annotation we can reuse for the wordform or alternative case.
			IWfiWordform wf = null;
			IAnalysis analysis;
			TryReuseAnalysis(tssWordText, ichMin, ichLim, iWfAnalysis, out wf, out tssWordAnn, out analysis);
			if (tssWordAnn.Length == 0)
				tssWordAnn = tssWordText;
			// check to see if we can match a user-confirmed phrase to establish a user based guess.
			// if we didn't find a real annotation or it's only an annotation for a WfiWordform.
			if (!SegmentFormCollectionMode && PossiblePhrases.Count > 0 && HasTrivialAnalysis(analysis))
			{
				ITsString tssPhrase;
				if (TryCreatePhrase(tssWordText, ichMin, iWfAnalysis, tssWordAnn.Length, out tssPhrase))
				{
					tssWordAnn = tssPhrase;
					analysis = null;	// indicates the need to create a new annotation.
				}
			}

			if (analysis == null)
			{
				// couldn't find an existing annotation to use, so try to create one.
				wf = FindOrCreateWordform(tssWordAnn);
				analysis = wf;
			}
			return analysis;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="ichMin"></param>
		/// <param name="tssWordAnn"></param>
		protected void AdjustCbaFields(ICmBaseAnnotation cba, int ichMin, ITsString tssWordAnn)
		{
			AdjustCbaFields(cba, ichMin, ichMin + tssWordAnn.Length);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		protected virtual void AdjustCbaFields(ICmBaseAnnotation cba, int ichMin, int ichLim)
		{
			SegmentServices.SetCbaFields(cba, m_para, ichMin, ichLim);
		}

		private bool HasTrivialAnalysis(IAnalysis analysis)
		{
			return (!(analysis is IWfiAnalysis) && !(analysis is IWfiGloss));
		}

		/// <summary>
		/// Returns a phrase wordform if it can create one without overwriting other annotations with
		/// significant (ie. non-wordform) analyses.
		/// </summary>
		/// <param name="tssWordText"></param>
		/// <param name="ichMin"></param>
		/// <param name="cWfAnalysisPrev">number of previous wordforming analyses in paragraph (indicates where to start looking in reuse list)</param>
		/// <param name="lengthToBeat">the size of a matched phrase must be greater than this.</param>
		/// <param name="tssPhrase">new phrase. tssWordText, by default.</param>
		/// <returns>true, if we could create a phrase.</returns>
		private bool TryCreatePhrase(ITsString tssWordText, int ichMin, int cWfAnalysisPrev, int lengthToBeat, out ITsString tssPhrase)
		{
			tssPhrase = tssWordText;
			// Check to see if we can match the longest real-phrase-form in the text.
			// If so, give preference to that form.
			HashSet<ITsString> phrases;
			if (PossiblePhrases.TryGetValue(m_paragraphTextScanner.ToLower(tssWordText), out phrases))
			{
				// Enhance JohnT: while matching one possible phrase, we may encounter a 'real' analysis.
				// A possible optimization is to remember a corresponding position in the text, and not try to match
				// any possible phrases which would overlap it.
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
						tssPhrase.Length > tssTryPhrase.Length ||
						!m_paragraphTextScanner.MatchesWordInText(tssTryPhrase, ichMin))
					{
						continue;
					}

					IList<IAnalysis> phraseSegmentForms;
					int iSegFormBoundary = -1;
					if (!TryRealSegmentFormsInPhrase(ichMin, ichMin + tssTryPhrase.Length, cWfAnalysisPrev, out phraseSegmentForms, out iSegFormBoundary))
					{
						// extend annotation wordform to this phrase.
						tssPhrase = tssTryPhrase;
					}
				}
			}
			return tssWordText.Length < tssPhrase.Length;
		}

		private IWfiWordform FindOrCreateWordform(ITsString tssWordAnn)
		{
			// give preference to alternative case forms that have exact annotation offset matches.
			IWfiWordform wf;
			if (m_wfr.TryGetObject(tssWordAnn, out wf))
			{
				return wf;
			}
			else
			{
				//Debug.Assert((m_wfi as WordformInventory).SuspendUpdatingConcordanceWordforms == this.RebuildingConcordanceWordforms,
				//	"Make sure that the WordformInventory is still in sync with us RebuildingConcordanceWordforms");
				// allows ParserConnection to generate Analyses.
				return m_wordfactory.Create(tssWordAnn);
			}
		}

#if PROFILING
		private void BeginProfilingCreateAnnotation(out long ticksBeforeCreate)
		{
			ticksBeforeCreate = DateTime.Now.Ticks;
			m_cAnnotations++;
		}

		private void EndProfilingCreateAnnotation(long ticksBeforeCreate)
		{
			m_cTicksMakingAnnotations += DateTime.Now.Ticks - ticksBeforeCreate;
			s_cTotalAnnotationsMade++;
		}
#endif

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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (RebuildingConcordanceWordforms)
					RebuildingConcordanceWordforms = false;	// don't get rid of m_wfi until we do this.
			}
			m_wordformAnnotationPossibilities = null;
			m_paragraphTextScanner = null;
			m_wordfactory = null;
			m_wfr = null;
			m_cbaf = null;
			m_cbar = null;
			m_wordMaker = null;
			m_tssPara = null;
			m_para = null;
			m_cache = null;

			m_isDisposed = true;
		}
		#endregion
	}
	#endregion

	#region WordMaker class
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
		/// <param name="wsf"></param>
		public WordMaker(ITsString tss, ILgWritingSystemFactory wsf)
		{
			Init(tss);
			m_tracker = new CpeTracker(wsf, tss);
			m_cpe = m_tracker.CharPropEngine(0); // a default for functions that don't depend on wordforming.
		}

		/// <summary>
		/// Return the Ws at the specified offset.
		/// </summary>
		public int WsAtOffset(int ich)
		{
			return TsStringUtils.GetWsAtOffset(m_tss, ich);
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
			return TsStringUtils.GetWsAtOffset(tss, 0);
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
		/// Finds the number of words before the next occurrence of wordform (tssWord) up to ichLim, or -1 if not found.
		/// </summary>
		/// <param name="tssWordTarget">form of the word/phrase to search for the next occurrence.</param>
		/// <param name="ichMinBase">place in text to start searching for another matching word.</param>
		/// <param name="ichLim">stop searching when the search ichMinNext reaches this point.</param>
		public int NextOccurrenceOfWord(ITsString tssWordTarget, int ichMinBase, int ichLim)
		{
			int count = 0;
			if (tssWordTarget.Length == 0 || ichMinBase >= ichLim || ichMinBase >= m_tss.Length)
				return -1;
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
					return count;
				}
				count++;
			}
			return -1;
		}

		/// <summary>
		/// Give the index of the next (full) character starting at ich.
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		public int NextChar(int ich)
		{
			return Surrogates.NextChar(m_st, ich);
		}

		/// <summary>
		/// Tells whether the full character (starting) at ich is a white-space character.
		/// Checks for zero width spaces here to handle the unusual writing systems which have no space
		/// between words.
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		public bool IsWhite(int ich)
		{
			return TsStringUtils.IsWhite(m_cpe, m_st, ich) || StringUtils.FullCharAt(m_st, ich) == AnalysisOccurrence.KchZws;
		}

		/// <summary>
		/// Tells whether the full character starting at ich is wordforming.
		/// Numbers will be considered word forming as of 9-21-2011 due to LT-10746
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		public bool IsWordforming(int ich)
		{
			//return m_tracker.CharPropEngine(ich).get_IsWordForming(StringUtils.FullCharAt(m_st, ich));
			bool isLabel = false;
			string sStyleName = m_tss.get_StringPropertyAt(ich, (int)FwTextPropType.ktptNamedStyle);
			if (sStyleName == "Chapter Number" || sStyleName == "Verse Number")
				isLabel = true;
			//The character is wordforming if it is not a label And it is either word forming or a number (numbers added for LT-10746)
			return !isLabel && (m_tracker.CharPropEngine(ich).get_IsWordForming(StringUtils.FullCharAt(m_st, ich)) ||
								  m_tracker.CharPropEngine(ich).get_IsNumber(StringUtils.FullCharAt(m_st, ich)));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <returns></returns>
		public ITsString NextWord(out int ichMin, out int ichLim)
		{
			// m_ich is always left one character position after a non-wordforming character.
			// This is considered implicitly true at the start of the string.
			bool fPrevWordForming = false;
			int ichStartWord = -1;
			ichMin = ichLim = 0;
			for (; m_ich < m_cch; m_ich = NextChar(m_ich))
			{
				// Whether the character is wordforming or not depends on two things:
				// 1) whether it's part of a "chapter number" or "verse number" run. (See LT-6972.)
				// 2) whether it's considered a "word forming" character by the Unicode standard.
				bool fThisWordForming = IsWordforming(m_ich);

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
	#endregion

	#region SegmentBreaker class
	/// <summary>
	/// This class is mainly the implementation of ParagraphParser.CollectSegments.
	/// It is broken out both because the algorithm is too complex for a single method, and so we
	/// can subclass depending on whether we want to actually create segments, or just note
	/// where they would be.
	/// </summary>
	internal abstract class SegmentBreaker
	{
		private readonly List<int> m_ichMinSegBreaks = new List<int>();
		private readonly CpeTracker m_cpeTracker;
		private readonly string m_paraText;
		/// <summary></summary>
		protected readonly ITsString m_tssText;
		private ILgCharacterPropertyEngine m_cpe;
		private int m_csegs = 0;
		private int m_prevCh;

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
		/// <summary>
		///
		/// </summary>
		/// <param name="text"></param>
		/// <param name="wsf"></param>
		protected SegmentBreaker(ITsString text, ILgWritingSystemFactory wsf)
		{
			m_tssText = text;
			m_cpeTracker = new CpeTracker(wsf, text);
			// Make sure this is always something.
			m_cpe = m_cpeTracker.CharPropEngine(0);

			m_paraText = (m_tssText == null || m_tssText.Text == null) ? string.Empty : m_tssText.Text;
		}

		/// <summary>
		/// This returns the positions of the first EOS character in each segment, or (if a segment ends for some other
		/// reason) the index of the first character of the next segment. It may be one less than the length of the list
		/// of segments, if the last segment has no EOS character.
		/// </summary>
		public List<int> EosPositions { get { return m_ichMinSegBreaks; } }

		/// <summary>
		///
		/// </summary>
		public void Run()
		{
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

			for (int ich = 0; ich < m_paraText.Length; ich = Surrogates.NextChar(m_paraText, ich))
			{
				m_prevCh = ch;
				ch = StringUtils.FullCharAt(m_paraText, ich);
				m_cpe = m_cpeTracker.CharPropEngine(ich);
				cc = m_cpe.get_GeneralCategory(ch);

				// don't try to deduce this from cc, it can be overiden.
				bool fIsLetter = m_cpe.get_IsWordForming(ch);// || m_cpe.get_IsNumber(ch); //Numbers are now wordforming in Analysis [LT-10746]
				bool fIsLabel = IsLabelText(m_tssText, m_tssText.get_RunAt(ich), TreatOrcsAsLabel);
				if (ch == StringUtils.kChHardLB)
				{
					// Hard line break, always its own segment.
					if (ich > ichStartSeg)
					{
						// If we've already recorded an EOS for the preceding segment, don't record another.
						if (m_ichMinSegBreaks.Count <= m_csegs)
							m_ichMinSegBreaks.Add(ich);
						CreateSegment(ichStartSeg, ich);
					}
					CreateSegment(ich, ich + 1);
					m_ichMinSegBreaks.Add(ich + 1);
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
							m_ichMinSegBreaks.Add(ich);
							ichLimSeg = ich;
							CreateSegment(ichStartSeg, ichLimSeg);
							ichStartSeg = ichLimSeg;
							state = SegParseState.ProcessingLabel;
							break;
						}
						if (IsEosChar(ch, cc, ich))
						{
							m_ichMinSegBreaks.Add(ich);
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
						m_ichMinSegBreaks.Add(ich);
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
			return TsStringUtils.IsEndOfSentenceChar(ch, cc);
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
			int chNext = StringUtils.FullCharAt(m_paraText, ich + 1); // +1 is safe because ch at ich is period (not half of surrogate)
			if (chNext == 0x002E)
			{
				// At least two periods...do we have three?
				if (ich >= m_paraText.Length - 2)
					return false; // only 2, not ellipsis
				if (StringUtils.FullCharAt(m_paraText, ich + 2) != 0x002E)
					return false; // exactly two periods is never special
				if (ich >= m_paraText.Length - 3)
					return true; // exactly 3 by end of string, this is the start of an ellipsis.
				// If the fourth character is a period, we do NOT have a proper ellipsis, so the first period is NOT special
				// If it IS NOT a period, we have exactly three, so it IS special.
				return StringUtils.FullCharAt(m_paraText, ich + 3) != 0x002E;
			}
			// No following period, so not an ellipsis. Special exactly if numbers on both sides.
			// No need currently to ensure correct cpe, category is not yet ws-dependent.
			return m_cpe.get_GeneralCategory(m_prevCh) == LgGeneralCharCategory.kccNd &&
				   m_cpe.get_GeneralCategory(chNext) == LgGeneralCharCategory.kccNd;
		}

		/// <summary>
		/// Test whether a run of text in a TsString contains 'label' text which should cause a segment break.
		/// (Current definition is that there are characters in the range with the Scripture styles VerseNumber
		/// or ChapterNumber, or the range being tested is a single character (possibly part of a longer run)
		/// which is a hard line break.
		/// </summary>
		internal static bool HasLabelText(ITsString tss)
		{
			return HasLabelText(tss, 0, tss.Length);
		}

		/// <summary>
		/// Test whether a run of text in a TsString contains 'label' text which should cause a segment break.
		/// (Current definition is that there are characters in the range with the Scripture styles VerseNumber
		/// or ChapterNumber, or the range being tested is a single character (possibly part of a longer run)
		/// which is a hard line break.
		/// </summary>
		internal static bool HasLabelText(ITsString tss, int ichMin, int ichLim)
		{
			// True if the run at ichMin has one of the interesting styles or is an ORC.
			int irun = tss.get_RunAt(ichMin);
			if (IsLabelText(tss, irun, false))
				return true;
			// See if any later run in the character range has the required style or is an ORC.
			int crun = tss.RunCount;
			for (irun++; irun < crun && tss.get_MinOfRun(irun) < ichLim; irun++)
			{
				if (IsLabelText(tss, irun, false))
					return true;
			}
			// All other ways it might be treated as a label have failed, so return false
			// unless it is a one-character range containing a hard line break.
			if (ichLim == ichMin + 1)
				return tss.GetChars(ichMin, ichLim) == StringUtils.kChHardLB.ToString();
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test whether a run in a TsString contains 'label' text which should cause a segment break.
		/// True if it has one of the interesting styles or the whole run is an ORC and
		/// fTreatOrcsAsLabels is true.
		/// Nb: this method won't detect hard line breaks. Hard line breaks are also forced
		/// (elsewhere) to be their own segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool IsLabelText(ITsString tss, int irun, bool fTreatOrcsAsLabels)
		{
			return (IsLabelStyle(tss.Style(irun)) || (fTreatOrcsAsLabels && tss.get_IsRunOrc(irun)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified style name is a special style that causes a label
		/// segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsLabelStyle(string styleName)
		{
			return styleName == ScrStyleNames.VerseNumber || styleName == ScrStyleNames.ChapterNumber;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new segment for the specified range of text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CreateSegment(int ichMin, int ichLim)
		{
			m_csegs++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to treat ORCs as label text (FW 6.0 style).
		/// </summary>
		/// <remarks>Normally this is false, but this property can be overridden by subclasses
		/// to return true.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool TreatOrcsAsLabel
		{
			get { return false; }
		}
	}
	#endregion

	#region SegmentMaker class
	/// <summary>
	/// This class actually creates the segments that would are needed for a chunk of text.
	/// </summary>
	internal class SegmentMaker : SegmentBreaker
	{
		private readonly ParagraphParser m_paraParser;
		private readonly IList<ISegment> m_segments = new List<ISegment>();

		internal SegmentMaker(ITsString text, ILgWritingSystemFactory wsf, ParagraphParser pp)
			: base(text, wsf)
		{
			m_paraParser = pp;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new segment for the specified range of text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateSegment(int ichMin, int ichLim)
		{
			base.CreateSegment(ichMin, ichLim);
			m_segments.Add(m_paraParser.CreateSegment(ichMin, ichLim));
		}

		/// <summary>
		/// The HVOs of the CmBaseAnntations created to be the segments.
		/// </summary>
		internal IList<ISegment> Segments
		{
			get { return m_segments; }
		}
	}
	#endregion

	#region SegmentCollector class
	/// <summary>
	/// This class collects up the segments that would be needed for a chunk of text, but does not actually
	/// create ISegments for them. It is typically used to parse CmTranslations rather than
	/// actual paragraphs.
	/// </summary>
	internal class SegmentCollector : SegmentBreaker
	{
		private readonly List<TsStringSegment> m_segments = new List<TsStringSegment>();
		private readonly bool m_fTreatOrcsAsLabels;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentCollector"/> class.
		/// </summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="wsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		public SegmentCollector(ITsString text, ILgWritingSystemFactory wsf) : this(text, wsf, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentCollector"/> class.
		/// </summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="wsf">The writing system factory.</param>
		/// <param name="fTreatOrcsAsLabels"><c>true</c> to treat ORCs as label text
		/// (FW 6.0 style), false otherwise.</param>
		/// ------------------------------------------------------------------------------------
		public SegmentCollector(ITsString text, ILgWritingSystemFactory wsf, bool fTreatOrcsAsLabels)
			: base(text, wsf)
		{
			m_fTreatOrcsAsLabels = fTreatOrcsAsLabels;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the segments calculated after doing a parse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<TsStringSegment> Segments
		{
			get { return m_segments; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new segment for the specified range of text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateSegment(int ichMin, int ichLim)
		{
			base.CreateSegment(ichMin, ichLim);
			m_segments.Add(new TsStringSegment(m_tssText, ichMin, ichLim));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to treat ORCs as label text (FW 6.0 style).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool TreatOrcsAsLabel
		{
			get { return m_fTreatOrcsAsLabels; }
		}
	}
	#endregion

	#region TextSource class
	/// <summary>
	/// Utility (static) functions for determining what kind of StText we're dealing with.
	/// </summary>
	public class TextSource
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the given StText belongs to scripture or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public bool IsScriptureText(IStText stText)
		{
			if (stText == null)
				return false;
			return IsScriptureTextFlid(stText.OwningFlid);
		}

		/// <summary>
		/// Given the flid that owns an StText, answer true if it is part of Scripture.
		/// </summary>
		static public bool IsScriptureTextFlid(int owningFlid)
		{
			return owningFlid == ScrSectionTags.kflidHeading ||
				   owningFlid == ScrSectionTags.kflidContent ||
				   owningFlid == ScrBookTags.kflidFootnotes ||
				   owningFlid == ScrBookTags.kflidTitle;
		}
	}
	#endregion
}
