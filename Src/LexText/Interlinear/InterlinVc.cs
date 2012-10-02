using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.CoreImpl;


namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// View constructor for InterlinView. Just to get something working, currently
	/// it is just a literal.
	/// </summary>
	public class InterlinVc : FwBaseVc, IDisposable
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
		//internal const int kfragText = 100024;
		internal const int kfragTxtSection = 100025;
		internal const int kfragStText = 100026;
		internal const int kfragParaSegment = 100027;
		internal const int kfragSegFf = 100028;
		internal const int kfragWordGloss = 100029;
		internal const int kfragIsolatedAnalysis = 100030;
		internal const int kfragMorphType = 100031;
		internal const int kfragPossibiltyAnalysisName = 100032;
		//internal const int kfragEmptyFreeTransPrompt = 100033;
		public const int kfragSingleInterlinearAnalysisWithLabelsLeftAlign = 100034;
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
		// And constants used for the 'fake' properties that break paras into
		// segments and provide defaults for wordforms
		// These two used to be constants but were made variables with dummy virtual handlers so that
		// ClearInfoAbout can clear them out.
		internal const int ktagSegmentFree = -61;
		internal const int ktagSegmentLit = -62;
		internal const int ktagSegmentNote = -63;
		// flids for paragraph annotation sequences.
		internal int ktagSegmentForms = 0;

		bool m_fIsAddingRealFormToView = false; // indicates we are in the context of adding real form string to the vwEnv.

		#endregion Constants and other similar ints.

		#region Data members

		protected bool m_fShowDefaultSense = false; // Use false to not change prior behavior.
		protected bool m_fHaveOpenedParagraph = false; // Use false to not change prior behavior.
		protected IWritingSystemManager m_wsManager;
		protected ISegmentRepository m_segRepository;
		protected ICmObjectRepository m_coRepository;
		protected IWfiMorphBundleRepository m_wmbRepository;
		protected IWfiAnalysisRepository m_analRepository;
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
		bool m_fShowMorphBundles = true;
		bool m_fRtl = false;
		ITsString m_tssDir;
		IDictionary<ILgWritingSystem, ITsString> m_mapWsDirTss = new Dictionary<ILgWritingSystem, ITsString>();
		// AnnotationDefns we need
		int m_hvoAnnDefNote;
		MoMorphSynAnalysisUi.MsaVc m_msaVc;
		InterlinLineChoices m_lineChoices;
		protected IVwStylesheet m_stylesheet;
		IParaDataLoader m_loader;
		InterlinDocRootSiteBase m_rootsite;
		private HashSet<int> m_vernWss; // all vernacular writing systems
		private int m_selfFlid;

		private int m_leftPadding = 0;

		#endregion Data members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="InterlinVc"/> class.
		/// </summary>
		/// <remarks>We use the default analysis writing system as the default, even though
		/// this view displays data in multiple writing systems. It's pretty arbitrary in this
		/// case, but we need a valid WS because if we get an ORC, we have to create a Ts String
		/// using some writing system.</remarks>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public InterlinVc(FdoCache cache) : base(cache.DefaultAnalWs)
		{
			Cache = cache;
			m_wsManager = m_cache.ServiceLocator.WritingSystemManager;
			m_segRepository = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
			m_coRepository = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			m_wmbRepository = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>();
			m_analRepository = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>();
			StTxtParaRepository = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>();
			m_wsAnalysis = cache.DefaultAnalWs;
			m_wsUi = cache.LanguageWritingSystemFactoryAccessor.UserWs;
			Decorator = new InterlinViewDataCache(m_cache);
			PreferredVernWs = cache.DefaultVernWs;
			m_selfFlid = m_cache.MetaDataCacheAccessor.GetFieldId2(CmObjectTags.kClassId, "Self", false);
			m_tssMissingGloss = m_tsf.MakeString(ITextStrings.ksStars, m_wsAnalysis);
			m_tssMissingSense = m_tssMissingGloss;
			m_tssMissingMsa = m_tssMissingGloss;
			m_tssMissingAnalysisPos = m_tssMissingGloss;
			m_tssEmptyAnalysis = m_tsf.MakeString("", m_wsAnalysis);
			m_WsList = new WsListManager(m_cache);
			m_tssEmptyPara = m_tsf.MakeString(ITextStrings.ksEmptyPara, m_wsAnalysis);
			m_tssSpace = m_tsf.MakeString(" ", m_wsAnalysis);
			m_msaVc = new MoMorphSynAnalysisUi.MsaVc(m_cache);
			m_vernWss = WritingSystemServices.GetAllWritingSystems(m_cache, "all vernacular",
				null, 0, 0);
			// This usually gets overridden, but ensures default behavior if not.
			m_lineChoices = InterlinLineChoices.DefaultChoices(m_cache.LangProject,
				WritingSystemServices.kwsVernInParagraph, WritingSystemServices.kwsAnal);
			// This used to be a constant but was made variables with dummy virtual handlers so that
			// ClearInfoAbout can clear them out.
			// load guesses
			ktagSegmentForms = SegmentTags.kflidAnalyses;
			GetSegmentLevelTags(cache);
			LangProjectHvo = m_cache.LangProject.Hvo;
		}

		internal InterlinViewDataCache Decorator { get; set; }

		private IStTxtParaRepository StTxtParaRepository { get; set; }

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
			TsStringUtils.ReassignTss(ref m_tssEmptyVern, m_tsf.MakeString("", wsVern));
			SetupRightToLeft(wsVern);
			TsStringUtils.ReassignTss(ref m_tssMissingAnalysis, m_tsf.MakeString(ITextStrings.ksStars, wsVern));
			m_tssMissingMorph = m_tssMissingAnalysis;
			m_tssMissingEntry = m_tssMissingAnalysis;
		}

		private void SetupRightToLeft(int wsVern)
		{
			IWritingSystem wsObj = m_wsManager.Get(wsVern);
			if (wsObj != null)
				m_fRtl = wsObj.RightToLeftScript;
			if (m_fRtl)
				m_tssDir = m_tsf.MakeString("\x200F", wsVern);	// RTL Mark.
			else
				m_tssDir = m_tsf.MakeString("\x200E", wsVern);	// LTR Mark.
		}

		/// <summary>
		/// Answer true if the specified word can be analyzed. This is a further check after
		/// ensuring it has an InstanceOf. It is equivalent to the check made in case kfragBundle of
		/// Display(), but that already has access to the writing system of the Wordform.
		/// GJM - Jan 19,'10 Added check to see if this occurrence is actually Punctuation.
		/// Punctuation cannot be analyzed.
		/// </summary>
		/// <returns></returns>
		internal bool CanBeAnalyzed(AnalysisOccurrence occurrence)
		{
			int occurrenceWs = occurrence.BaselineWs;
			if (occurrence.Analysis is IPunctuationForm)
				return false;
			return occurrenceWs == m_wsVernForDisplay || m_vernWss.Contains(occurrenceWs);
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

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~InterlinVc()
		{
			Dispose(false);
		}
		#endif

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(GetType().ToString(), "This object is being used after it has been disposed: this is an Error.");
		}

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				// Dispose managed resources here.
				if (m_WsList != null)
					m_WsList.Dispose();

				TsStringUtils.ReassignTss(ref m_tssMissingAnalysis, null);
				TsStringUtils.ReassignTss(ref m_tssMissingGloss, null);
				TsStringUtils.ReassignTss(ref m_tssEmptyAnalysis, null);
				TsStringUtils.ReassignTss(ref m_tssEmptyVern, null);
				TsStringUtils.ReassignTss(ref m_tssEmptyPara, null);
				TsStringUtils.ReassignTss(ref m_tssSpace, null);
				TsStringUtils.ReassignTss(ref m_tssCommaSpace, null);

				if (m_tsf != null)
					Marshal.ReleaseComObject(m_tsf);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_msaVc = null;
			m_cache = null;

			m_tssMissingMorph = null; // Same as m_tssMissingAnalysis.
			m_tssMissingSense = null; // Same as m_tssMissingGloss.
			m_tssMissingMsa = null; // Same as m_tssMissingGloss.
			m_tssMissingAnalysisPos = null; // Same as m_tssMissingGloss.
			m_tssMissingEntry = null; // Same as m_tssEmptyAnalysis.
			m_tsf = null;
			m_WsList = null;

			IsDisposed = true;
		}
		#endregion

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
		/// Made public for DiscourseExporter
		/// </summary>
		public bool IsDoingRealWordForm
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
			if (m_loader != null)
			{
				CheckDisposed();
				m_loader.ResetGuessCache();
			}
		}

		internal AnalysisGuessServices GuessServices
		{
			get
			{
				if (m_loader != null && m_loader.GuessServices != null)
					return m_loader.GuessServices;
				return new AnalysisGuessServices(m_cache);
			}
		}

		public bool UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			if (m_loader != null)
			{
				CheckDisposed();
				return m_loader.UpdatingOccurrence(oldAnalysis, newAnalysis);
			}
			return false;
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
		/// Background color indicating there are multiple possible human approved guesses
		/// </summary>
		public static int MultipleApprovedGuessColor
		{
			get { return (int)CmObjectUi.RGB(255, 255, 50); }
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
		internal InterlinDocRootSiteBase RootSite
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
		private void SetGuessing(IVwEnv vwenv, int bgColor)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				bgColor);
		}

		private void SetGuessing(IVwEnv vwenv)
		{
			SetGuessing(vwenv, ApprovedGuessColor);
		}

		/// <summary>
		/// Get a guess for the given word or analysis.
		/// </summary>
		/// <param name="analysis"></param>
		/// <returns></returns>
		internal int GetGuess(IAnalysis analysis)
		{
			if (Decorator.get_IsPropInCache(analysis.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid,
				(int)CellarPropertyType.ReferenceAtomic, 0))
			{
				int hvoResult = Decorator.get_ObjectProp(analysis.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid);
				if (hvoResult != 0)
					return hvoResult;  // may have been cleared by setting to zero.
			}
			return analysis.Hvo;
		}

		// Set the properties that make the labels like "Note" 'in a fainter font" than the main text.
		private void SetNoteLabelProps(IVwEnv vwenv)
		{
			SetColor(vwenv, krgbNoteLabel);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
#if __MonoCS__
		// TODO-Linux: Randomly m_tsf seem to have been Release.
		// eg Marshal.ReleaseComObject(m_tsf);
		// However the Dispose method isn't called (which calls the Release)
		// Currently unsure what is doing this need to find out - very concerning
		// Hack - just recreate a new TsStrFactory each time... for now
		// seems to stop the problem.
		m_tsf = TsStrFactoryClass.Create();
#endif
			CheckDisposed();
			if (hvo == 0)
				return;		// Can't do anything without an hvo (except crash -- see LT-9348).

#if DEBUG
			//TimeRecorder.Begin("Display");
#endif
			switch (frag)
			{
			case kfragStText:	// new root object for InterlinDocChild.
				SetupRealVernWsForDisplay(WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsVernInParagraph,
					hvo, StTextTags.kflidParagraphs));
				vwenv.AddLazyVecItems(StTextTags.kflidParagraphs, this, kfragInterlinPara);
				break;
			case kfragInterlinPara: // Whole StTxtPara. This can be the root fragment in DE view.
				if (vwenv.DataAccess.get_VecSize(hvo, StTxtParaTags.kflidSegments) == 0)
				{
					vwenv.NoteDependency(new int[] { hvo }, new int[] { StTxtParaTags.kflidSegments }, 1);
					vwenv.AddString(m_tssEmptyPara);
				}
				else
				{
					PreferredVernWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsVernInParagraph, hvo, StTxtParaTags.kflidSegments);
					// Include the plain text version of the paragraph?
					vwenv.AddLazyVecItems(StTxtParaTags.kflidSegments, this, kfragParaSegment);
				}
				break;
			case kfragParaSegment:
				// Don't put anything in this segment if it is a 'label' segment (typically containing a verse
				// number for TE).
				var seg = m_segRepository.GetObject(hvo);
				if (seg.IsLabel)
					break;
				// This puts ten points between segments. There's always 5 points below each line of interlinear;
				// if there are no freeform annotations another 5 points makes 10 between segments.
				// If there are freeforms, we need the full 10 points after the last of them.
				var haveFreeform = seg.FreeTranslation != null || seg.LiteralTranslation != null || seg.NotesOS.Count > 0;
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
					(int)FwTextPropVar.ktpvMilliPoint, !haveFreeform ? 5000 : 10000);
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
				vwenv.AddObjVecItems(SegmentTags.kflidAnalyses, this, kfragBundle);
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
				AddFreeformAnnotations(vwenv, hvo);
				vwenv.CloseDiv();
				break;
			case kfragBundle: // One annotated word bundle; hvo is the IAnalysis object.
				// checking AllowLayout (especially in context of Undo/Redo make/break phrase)
				// helps prevent us from rebuilding the display until we've finished
				// reconstructing the data and cache. Otherwise we can crash.
				if (m_rootsite != null && !m_rootsite.AllowLayout)
					return;
				AddWordBundleInternal(hvo, vwenv);
				break;
			case kfragIsolatedAnalysis: // This one is used for an isolated HVO that is surely an analysis.
			{
				var wa = m_analRepository.GetObject(hvo);
				vwenv.AddObj(wa.Owner.Hvo, this, kfragWordformForm);
				if (m_fShowMorphBundles)
					vwenv.AddObj(hvo, this, kfragAnalysisMorphs);

				int chvoGlosses = wa.MeaningsOC.Count;
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
						vwenv.AddObjVec(WfiAnalysisTags.kflidMeanings, this, kfragWordGlossWs + i);
					}
				}
				AddAnalysisPos(vwenv, hvo, hvo, -1);
			}
				break;
			case kfragAnalysisMorphs:
				int cmorphs = 0;
				ICmObject co = m_coRepository.GetObject(hvo);
				if (co is IWfiAnalysis)
					cmorphs = (co as IWfiAnalysis).MorphBundlesOS.Count;
				// We really want a variable for this...there have been pathological cases where
				// m_fHaveOpenedParagraph changed during the construction of the paragraph, and we want to be
				// sure to close the paragraph if we opened it.
				var openedParagraph = !m_fHaveOpenedParagraph;
				if (openedParagraph)
					vwenv.OpenParagraph();
				if (cmorphs == 0)
				{
					DisplayMorphBundle(vwenv, 0);
				}
				else
				{
					vwenv.AddObjVecItems(WfiAnalysisTags.kflidMorphBundles, this, kfragMorphBundle);
				}
				if (openedParagraph)
					vwenv.CloseParagraph();
				break;

			case kfragWordGloss:	// displaying forms of a known WfiGloss.
				foreach (int wsId in m_WsList.AnalysisWsIds)
				{
					int idx = m_lineChoices.IndexOf(InterlinLineChoices.kflidWordGloss, wsId);
					if (idx >= 0)
					{
						SetColor(vwenv, LabelRGBFor(idx));
						vwenv.AddStringAltMember(WfiGlossTags.kflidForm, wsId, this);
					}
				}
				break;
			case kfragMorphType: // for export only at present, display the
				vwenv.AddObjProp(MoFormTags.kflidMorphType, this, kfragPossibiltyAnalysisName);
				break;
			case kfragPossibiltyAnalysisName:
				vwenv.AddStringAltMember(CmPossibilityTags.kflidName, m_cache.DefaultAnalWs, this);
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
			//	// NB: If the hvo is zero, then we need to go back to the normal missing sense display, after all.
			//	// (hvo isn't zero, even for cases where there isn't even a default value.)
			//	if (hvo > 0)
			//	{
			//		// Show default sense, in some other 'guess' color.
			//		SetGuessing(vwenv, false);
			//		foreach (int wsId in m_WsList.AnalysisWsIds)
			//			vwenv.AddStringAltMember(LexSenseTags.kflidGloss,
			//				wsId, this);
			//	}
			//	else
			//	{
			//		// Give up and show the missing sense row.
			//		vwenv.AddString(m_tssMissingSense);
			//	}
			//	break;
			case kfragWordformForm: // The form of a WviWordform.
				vwenv.AddStringAltMember(WfiWordformTags.kflidForm,
					m_wsVernForDisplay, this);
				break;
			case kfragPrefix:
				vwenv.AddUnicodeProp(MoMorphTypeTags.kflidPrefix, m_wsVernForDisplay, this);
				break;
			case kfragPostfix:
				vwenv.AddUnicodeProp(MoMorphTypeTags.kflidPostfix, m_wsVernForDisplay, this);
				break;
			case kfragSenseName: // The name (gloss) of a LexSense.
				foreach (int wsId in m_WsList.AnalysisWsIds)
					vwenv.AddStringAltMember(LexSenseTags.kflidGloss,
						wsId, this);
				break;
			case kfragCategory: // the category of a WfiAnalysis, a part of speech;
				// display the Abbreviation property inherited from CmPossibility.
				foreach(var wsId in m_WsList.AnalysisWsIds)
				{
					vwenv.AddStringAltMember(CmPossibilityTags.kflidAbbreviation, wsId, this);
				}
				break;
			default:
				if (frag >= kfragWordGlossWs && frag < kfragWordGlossWs + m_WsList.AnalysisWsIds.Length)
				{
					// Displaying one ws of the  form of a WfiGloss.
					int ws = m_WsList.AnalysisWsIds[frag - kfragWordGlossWs];
					vwenv.AddStringAltMember(WfiGlossTags.kflidForm, ws, this);
				}
				else if (frag >= kfragLineChoices && frag < kfragLineChoices + m_lineChoices.Count)
				{
					var spec = m_lineChoices[frag - kfragLineChoices];
					int ws = GetRealWsOrBestWsForContext(hvo, spec);
					vwenv.AddStringAltMember(spec.StringFlid, ws, this);
				}
				else if (frag >= kfragAnalysisCategoryChoices && frag < kfragAnalysisCategoryChoices + m_lineChoices.Count)
				{
					AddAnalysisPos(vwenv, hvo, hvo, frag - kfragAnalysisCategoryChoices);
				}
				else if (frag >= kfragMorphFormChoices && frag < kfragMorphFormChoices + m_lineChoices.Count)
				{
					InterlinLineSpec spec = m_lineChoices[frag - kfragMorphFormChoices];
					int wsActual = GetRealWsOrBestWsForContext(hvo, spec);
					DisplayMorphForm(vwenv, hvo, wsActual);
				}
				else if (frag >= kfragSegFfChoices && frag < kfragSegFfChoices + m_lineChoices.Count)
				{
					AddFreeformComment(vwenv, hvo, frag - kfragSegFfChoices);
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
		///
		/// </summary>
		/// <param name="hvo">the IAnalysis object</param>
		/// <param name="vwenv"></param>
		protected virtual void AddWordBundleInternal(int hvo, IVwEnv vwenv)
		{
			SetupAndOpenInnerPile(vwenv);
			// we assume we're in the context of a segment with analyses here.
			// we'll need this info down in DisplayAnalysisAndCloseInnerPile()
			int hvoSeg;
			int tagDummy;
			int index;
			vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoSeg, out tagDummy, out index);
			var analysisOccurrence = new AnalysisOccurrence(m_segRepository.GetObject(hvoSeg), index);
			DisplayAnalysisAndCloseInnerPile(vwenv, analysisOccurrence, true);
		}

		/// <summary>
		/// Displays Analysis using DisplayWordBundleMethod and closes the views Inner Pile.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="analysisOccurrence"></param>
		/// <param name="showMultipleAnalyses">Tells DisplayWordBundleMethod whether or not to show
		/// the colored highlighting if a word has multiple analyses</param>
		protected void DisplayAnalysisAndCloseInnerPile(IVwEnv vwenv, AnalysisOccurrence analysisOccurrence,
			bool showMultipleAnalyses)
		{
			// if it is just a punctuation annotation, we just insert the form.
			var analysis = analysisOccurrence.Analysis;
			if (analysis is IPunctuationForm)
			{
				vwenv.AddStringProp(PunctuationFormTags.kflidForm, this);
			}
			else
			{
				// It's a full wordform-possessing annotation, display the full bundle.
				new DisplayWordBundleMethod(vwenv, analysisOccurrence, this).Run(showMultipleAnalyses);
			}
			AddExtraBundleRows(vwenv, analysisOccurrence);
			vwenv.CloseInnerPile();
		}

		/// <summary>
		/// Setup a box with 10 points behind and 5 under and open the inner pile
		/// </summary>
		/// <param name="vwenv"></param>
		protected virtual void SetupAndOpenInnerPile(IVwEnv vwenv)
		{
			// Make an 'inner pile' to contain the wordform and annotations.
			// Give whatever box we make 10 points of separation from whatever follows.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
								  (int)FwTextPropVar.ktpvMilliPoint, 10000);
			// 5 points below also helps space out the paragraph.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
								  (int)FwTextPropVar.ktpvMilliPoint, 5000);
			vwenv.OpenInnerPile();
		}

		protected virtual void AddFreeformComment(IVwEnv vwenv, int hvoSeg, int lineChoiceIndex)
		{
			int[] wssAnalysis = m_lineChoices.AdjacentWssAtIndex(lineChoiceIndex);
			if (wssAnalysis.Length == 0)
				return;
			vwenv.OpenDiv();
			SetParaDirectionAndAlignment(vwenv, wssAnalysis[0]);
			vwenv.OpenMappedPara();
			string label;
			int flid;
			InterlinearExporter exporter = vwenv as InterlinearExporter;
			int dummyFlid = m_lineChoices[lineChoiceIndex].Flid;
			switch (dummyFlid)
			{
				case InterlinLineChoices.kflidFreeTrans:
					label = ITextStrings.ksFree_;
					flid = SegmentTags.kflidFreeTranslation;
					if (exporter != null)
						exporter.FreeAnnotationType = "gls";
					break;
				case InterlinLineChoices.kflidLitTrans:
					label = ITextStrings.ksLit_;
					flid = SegmentTags.kflidLiteralTranslation;
					if (exporter != null)
						exporter.FreeAnnotationType = "lit";
					break;
				case InterlinLineChoices.kflidNote:
					label = ITextStrings.ksNote_;
					flid = NoteTags.kflidContent;
					if (exporter != null)
						exporter.FreeAnnotationType = "note";
					break;
				default:
					throw new Exception("Unexpected FF annotation type");
			}
			SetNoteLabelProps(vwenv);
			// REVIEW: Should we set the label to a special color as well?
			ITsString tssLabel = MakeUiElementString(label, m_cache.DefaultUserWs,
				propsBldr => propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn));
			ITsStrBldr labelBldr = tssLabel.GetBldr();
			AddLineIndexProperty(labelBldr, lineChoiceIndex);
			tssLabel = labelBldr.GetString();
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
				AddFreeformComment(vwenv, hvoSeg, wssAnalysis[0], flid);
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
					AddFreeformComment(vwenv, hvoSeg, wssAnalysis[0], flid);
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
					AddFreeformComment(vwenv, hvoSeg, wssAnalysis[0], flid);
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
					AddFreeformComment(vwenv, hvoSeg, wssAnalysis[i], flid);
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
					AddFreeformComment(vwenv, hvoSeg, wssAnalysis[i], flid);
				}
			}


			vwenv.CloseParagraph();
			vwenv.CloseDiv();
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
				vwenv.AddObjProp(m_selfFlid, this, kfragAnalysisMorphs);
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
			IWritingSystem wsObj = m_wsManager.Get(ws);
			ITsString tssDirWs;
			if (!m_mapWsDirTss.TryGetValue(wsObj, out tssDirWs))
			{
				bool fRtlWs = wsObj.RightToLeftScript;
				if (fRtlWs)
					tssDirWs = m_tsf.MakeString("\x200F", ws);	// RTL Marker
				else
					tssDirWs = m_tsf.MakeString("\x200E", ws);	// LTR Marker
				m_mapWsDirTss.Add(wsObj, tssDirWs);
			}
			return tssDirWs;
		}

		/// <summary>
		/// Add any extra material after the main bundles. See override on InterlinTaggingVc.
		/// </summary>
		internal virtual void AddExtraBundleRows(IVwEnv vwenv, AnalysisOccurrence point)
		{
		}

		private int m_hvoActiveFreeform;
		internal int ActiveFreeformFlid { get; private set; }
		/// <summary>
		/// LT-12230: Needed in InterlinDocForAnalysis.GetLineInfo().
		/// </summary>
		internal int ActiveFreeformWs { get; private set; }
		internal int m_cpropActiveFreeform;

		internal void SetActiveFreeform(int hvo, int flid, int ws, int cpropPrevious)
		{
			var helper = SelectionHelper.Create(m_rootsite);
			int hvoOld = m_hvoActiveFreeform;
			int flidOld = ActiveFreeformFlid;
			m_hvoActiveFreeform = hvo;
			ActiveFreeformFlid = flid;
			ActiveFreeformWs = ws;
			// The cpropPrevious we get from the selection may be one off, if a previous line is displaying
			// the prompt for another WS of the same object.
			if (hvoOld == hvo && m_cpropActiveFreeform <= cpropPrevious)
				m_cpropActiveFreeform = cpropPrevious + 1;
			else
				m_cpropActiveFreeform = cpropPrevious;

			// The old one is easy to turn off because we have a NoteDependency on it.
			if (hvoOld != 0)
				m_rootsite.RootBox.PropChanged(hvoOld, flidOld, 0, 0, 0);
			if (m_hvoActiveFreeform != 0)
			{
				var seg = m_cache.ServiceLocator.GetInstance<ISegmentRepository>().GetObject(m_hvoActiveFreeform);
				var para = (IStTxtPara) seg.Owner;
				int hvoPara = para.Hvo;
				int index = para.SegmentsOS.IndexOf(seg);
				// Pretend the segment affected has been deleted and re-inserted.
				// This will force it to be re-displayed, with different results now m_hvoActiveFreeform etc are set.
				m_rootsite.RootBox.PropChanged(hvoPara, StTxtParaTags.kflidSegments, index, 1, 1);
			}
			if (helper != null)
			{
				helper.SetSelection(true, false);
			}
		}


		private void AddFreeformComment(IVwEnv vwenv, int hvo, int ws, int flidTarget)
		{
			if (flidTarget != ActiveFreeformFlid || hvo != m_hvoActiveFreeform || ws != ActiveFreeformWs)
			{
				vwenv.AddStringAltMember(flidTarget, ws, this); // display normally, not the current prop
				return;
			}
			ITsString tssVal = vwenv.DataAccess.get_MultiStringAlt(hvo, flidTarget, ws);
			if (tssVal.Length != 0)
			{
				// Display normally, length is not zero. This is probably redundant, we don't set m_hvoActiveFreeform etc
				// if the length is zero. For that reason, putting in the following note dependency doesn't help.
				// Even if we did set them for a non-empty string, we'd have to
				// do a lot of other work to get the selection restored appropriately when the length goes to zero.
				// vwenv.NoteStringValDependency(hvo, CmAnnotationTags.kflidComment, ws, tsf.MakeString("", ws));
				vwenv.AddStringAltMember(flidTarget, ws, this);
				return;
			}
			// If anything causes the comment to change, get rid of the prompt.
			vwenv.NoteDependency(new [] { hvo }, new [] { flidTarget }, 1);
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
				case WritingSystemServices.kwsVern:
				case WritingSystemServices.kwsVerns:
				case WritingSystemServices.kwsFirstVern:
				case WritingSystemServices.kwsVernInParagraph:
					return true;
				case WritingSystemServices.kwsAnal:
				case WritingSystemServices.kwsAnals:
				case WritingSystemServices.kwsFirstAnal:
				case WritingSystemServices.kwsFirstPronunciation:
				case WritingSystemServices.kwsAllReversalIndex:
				case WritingSystemServices.kwsPronunciation:
				case WritingSystemServices.kwsPronunciations:
				case WritingSystemServices.kwsReversalIndex:
					return false;
			}
			IWritingSystem wsObj = m_wsManager.Get(ws);
			if (m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj))
				return !m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Contains(wsObj);
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
			var seg = m_segRepository.GetObject(hvo);
			var para = seg.Paragraph;
			if (para != null)
			{
				ISilDataAccess sda = vwenv.DataAccess;
				int cseg = para.SegmentsOS.Count;
				int idxSeg = para.SegmentsOS.IndexOf(seg); // sda.GetObjIndex(hvoStPara, ktagParaSegments, hvo);
				IStText stText = para.Owner as IStText;
				if (stText != null)
					flid = stText.OwningFlid;
				if (flid == ScrSectionTags.kflidContent)
				{
					var scrPara = para as IScrTxtPara;
					// With null book name and trimmed it should have just chapter:v{a,b}.
					// The {a,b} above would not be the segment identifiers we add for multiple segments in
					// a verse, but the letters indicating that the verse label is for only part of the verse.
					// There is therefore a pathological case where, say, verse 4a as labeled in the main text
					// gets another letter because 4a has multiple segments 4aa, 4ab, etc.
					string chapRef = ScriptureServices.FullScrRef(scrPara, seg.BeginOffset, "").Trim();
					sbSegNum.Append(chapRef + ScriptureServices.VerseSegLabel(scrPara, idxSeg));
				}
				else
				{
					int idxPara = para.OwnOrd;
					if (idxPara >= 0)
					{
						sbSegNum.AppendFormat("{0}", idxPara + 1);
						if (idxSeg >= 0 && cseg > 1)
							sbSegNum.AppendFormat(".{0}", idxSeg + 1);
					}
				}
			}
			ITsStrBldr tsbSegNum = m_tsf.GetBldr();
			tsbSegNum.ReplaceTsString(0, tsbSegNum.Length, TsStringUtils.MakeTss(sbSegNum.ToString(), m_cache.DefaultUserWs));
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

		/// <summary>
		/// try to get the ws specified by spec.WritingSystem, otherwise
		/// get the default vernacular ws for the display (e.g. ws of paragraph).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="spec"></param>
		/// <returns></returns>
		internal int GetRealWsOrBestWsForContext(int hvo, InterlinLineSpec spec)
		{
			return GetRealWs(m_cache, hvo, spec, m_wsVernForDisplay);
		}

		static private int GetRealWs(FdoCache cache, int hvo, InterlinLineSpec spec, int wsPreferred)
		{
			int ws = 0;
			switch (spec.WritingSystem)
			{
				case WritingSystemServices.kwsVernInParagraph:
					// we want to display the wordform using its own ws.
					ws = wsPreferred;
					break;
				default:
					ws = spec.GetActualWs(cache, hvo, wsPreferred);
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
			IWfiMorphBundle wmb = null;
			if (hvo != 0)
				wmb = m_wmbRepository.GetObject(hvo);
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.OpenInnerPile();
			int first = m_lineChoices.FirstMorphemeIndex;
			int last = m_lineChoices.LastMorphemeIndex;
			IMoForm mf = null;
			if (wmb != null)
				mf = wmb.MorphRA;
			if (vwenv is CollectorEnv && mf != null)
			{
				// Collectors are given an extra initial chance to 'collect' the morph type, if any.
				vwenv.AddObjProp(WfiMorphBundleTags.kflidMorph,
					this, kfragMorphType);

			}
			for (int i = first; i <= last; i++)
			{
				InterlinLineSpec spec = m_lineChoices[i];
				SetColor(vwenv, LabelRGBFor(spec));
				switch (spec.Flid)
				{
					case InterlinLineChoices.kflidMorphemes:
						if (wmb == null)
						{
							vwenv.AddString(m_tssMissingMorph);
						}
						else if (mf == null)
						{
							// displaying morphemes should be
							int ws = 0;
							if (wmb.MorphRA != null)
							{
								Debug.Assert(spec.StringFlid == MoFormTags.kflidForm);
								ws = GetRealWsOrBestWsForContext(wmb.MorphRA.Hvo, spec);
							}
							// If no morph, use the form of the morph bundle (and the entry is of
							// course missing)
							if (ws == 0)
							{
								ws = WritingSystemServices.ActualWs(m_cache, spec.WritingSystem, wmb.Hvo,
									WfiMorphBundleTags.kflidForm);
							}
							vwenv.AddStringAltMember(
								WfiMorphBundleTags.kflidForm, ws, this);
						}
						else
						{
							// Got a morph, show it.
							vwenv.AddObjProp(WfiMorphBundleTags.kflidMorph,
								this, kfragMorphFormChoices + i);
							// And the LexEntry line.
						}
						break;
					case InterlinLineChoices.kflidLexEntries:
						if (mf == null)
						{
							if (hvo != 0)
								vwenv.NoteDependency(new int[] { hvo }, new int[] { WfiMorphBundleTags.kflidMorph }, 1);
							vwenv.AddString(m_tssMissingEntry);
						}
						else
						{
							int ws = GetRealWsOrBestWsForContext(mf.Hvo, spec);
							if (ws == 0)
								ws = spec.WritingSystem;
							LexEntryVc vcEntry = new LexEntryVc(m_cache);
							vcEntry.WritingSystemCode = ws;
							vwenv.AddObj(hvo, vcEntry, LexEntryVc.kfragEntryAndVariant);
						}
						break;
					case InterlinLineChoices.kflidLexGloss:
						ILexSense sense = null;
						if (wmb != null)
						{
							sense = wmb.SenseRA;
							if (sense == null)
							{
								vwenv.NoteDependency(new int[] {hvo}, new int[] {WfiMorphBundleTags.kflidSense}, 1);
								if (ShowDefaultSense)
								{
									if (wmb.DefaultSense != null)
									{
										//apparently we believe this to be a parser generated guess
										SetGuessing(vwenv, MachineGuessColor);
										var virtFlid =
											wmb.Cache.MetaDataCacheAccessor.GetFieldId2(WfiMorphBundleTags.kClassId,
												"DefaultSense", false);
										vwenv.AddObjProp(virtFlid, this, kfragLineChoices + i);
									}
								}
							}
						}
						if (sense == null)
						{
								vwenv.AddString(m_tssMissingSense);
						}
						else
						{
							vwenv.AddObjProp(WfiMorphBundleTags.kflidSense,
								this, kfragLineChoices + i);
						}
						break;

					case InterlinLineChoices.kflidLexPos:
						// LexPOS line:
						int hvoMsa = 0;
						if (wmb != null && wmb.MsaRA != null)
							hvoMsa = wmb.MsaRA.Hvo;
						if (hvoMsa == 0)
						{
							if (hvo != 0)
								vwenv.NoteDependency(new int[] { hvo }, new int[] { WfiMorphBundleTags.kflidMsa }, 1);
							vwenv.AddString(m_tssMissingMsa);
						}
						else
						{
							// Use a special view constructor that knows how to display the
							// interlinear view of whatever kind of MSA it is.
							// Enhance JohnT: ideally we would have one of these VCs for each writing system,
							// perhaps stored in the InterlinLineSpec. Currently displaying multiple Wss of LexPos
							// is not useful, though it is possible.
							// Enhancement RickM: we set the m_msaVc.DefaultWs to the selected writing system
							//		of each LexPos line in interlinear. This is used extract the LexPos abbreviation
							//		for the specific writing system.
							m_msaVc.DefaultWs = spec.WritingSystem;
							vwenv.AddObjProp(WfiMorphBundleTags.kflidMsa,
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
			var spaceStr = TsStringUtils.MakeTss(" ", wsUI);
			// int wsAnalysis = cache.DefaultAnalWs; // CS0219
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvForceOn);
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
				(int)FwTextPropVar.ktpvMilliPoint,
				5000); // default spacing is fine for all embedded paragraphs.
			vwenv.OpenInnerPile();
			for (var i = 0; i < m_lineChoices.Count; i++)
			{
				InterlinLineSpec spec = m_lineChoices[i];
				if (!spec.WordLevel)
					break;
				SetColor(vwenv, LabelRGBFor(spec));
				ITsString tss = MakeUiElementString(m_lineChoices.LabelFor(spec.Flid), wsUI, null);
				var bldr = tss.GetBldr();
				if (m_lineChoices.RepetitionsOfFlid(spec.Flid) > 1)
				{
					bldr.Append(spaceStr);
					bldr.Append(spec.WsLabel(cache));
					AddLineIndexProperty(bldr, i);
					// Enhance GJM: Might be able to do without paragraph now?
					vwenv.OpenParagraph();
					vwenv.AddString(bldr.GetString());
					vwenv.CloseParagraph();
				}
				else
				{
					AddLineIndexProperty(bldr, i);
					vwenv.AddString(bldr.GetString());
				}

			}
			vwenv.CloseInnerPile();
		}

		private static void AddLineIndexProperty(ITsStrBldr bldr, int i)
		{
			// BulletNumStartAt is a kludge because ktptObjData is ALSO ktptFontSize!
			bldr.SetIntPropValues(0, bldr.Length, (int) FwTextPropType.ktptBulNumStartAt,
								  (int) FwTextPropVar.ktpvDefault, i);
		}


		private void DisplayMorphForm(IVwEnv vwenv, int hvo, int ws)
		{
			var mf = (IMoForm)m_coRepository.GetObject(hvo);
			// The form of an MoForm. Hvo is some sort of MoMorph. Display includes its prefix
			// and suffix.
			// Todo: make prefix and suffix read-only.
			vwenv.OpenParagraph(); // group prefix, form, suffix on one line.
			// It may not have a morph type at all.
			// RBR says: "So why take the chance of a null ref exception (which I ran into, in my ZPI data, of course)? :-)
			// int typeID = mf.MorphTypeRA.Hvo;
			var morphType = mf.MorphTypeRA;
			if (morphType != null)
				vwenv.AddObjProp(MoFormTags.kflidMorphType, this, kfragPrefix);
			vwenv.AddStringAltMember(MoFormTags.kflidForm,
				ws, this);
			if (morphType != null)
				vwenv.AddObjProp(MoFormTags.kflidMorphType, this, kfragPostfix);
			vwenv.CloseParagraph();
		}

		/// <summary>
		/// Implementation of displaying a word bundle as Method Object
		/// </summary>
		class DisplayWordBundleMethod
		{
			int m_hvoWordform;
			int m_hvoWfiAnalysis;
			int m_hvoDefault;
			ICmObject m_defaultObj;
			readonly IVwEnv m_vwenv;
			readonly AnalysisOccurrence m_analysisOccurrence;
			readonly int m_hvoWordBundleAnalysis;
			readonly InterlinVc m_this;
			readonly FdoCache m_cache;
			readonly InterlinLineChoices m_choices;
			private bool m_fshowMultipleAnalyses;

			private DisplayWordBundleMethod(IVwEnv vwenv1, int hvoWordBundleAnalysis, InterlinVc owner)
			{
				m_vwenv = vwenv1;
				m_hvoWordBundleAnalysis = hvoWordBundleAnalysis;
				m_this = owner;
				m_cache = m_this.m_cache;
				m_choices = m_this.LineChoices;
			}

			public DisplayWordBundleMethod(IVwEnv vwenv1, AnalysisOccurrence analysisOccurrence, InterlinVc owner)
				: this(vwenv1, analysisOccurrence.Analysis.Hvo, owner)
			{
				m_analysisOccurrence = analysisOccurrence;
			}

			public void Run(bool showMultipleAnalyses)
			{
				m_fshowMultipleAnalyses = showMultipleAnalyses;
				var coRepository = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
				var wag = coRepository.GetObject(m_hvoWordBundleAnalysis) as IAnalysis;
				switch (wag.ClassID)
				{
				case WfiWordformTags.kClassId:
					m_hvoWordform = wag.Wordform.Hvo;
					m_hvoDefault = m_this.GetGuess(wag.Wordform);
					break;
				case WfiAnalysisTags.kClassId:
					m_hvoWordform = wag.Wordform.Hvo;
					m_hvoWfiAnalysis = wag.Analysis.Hvo;
					m_hvoDefault = m_this.GetGuess(wag.Analysis);
					break;
				case WfiGlossTags.kClassId:
					m_hvoWfiAnalysis = wag.Analysis.Hvo;
					m_hvoWordform = wag.Wordform.Hvo;
					m_hvoDefault = wag.Hvo; // complete analysis. no point in searching for a default!
					break;
				default:
					throw new Exception("invalid type used for word analysis");
				}
				m_defaultObj = coRepository.GetObject(m_hvoDefault);
				for (var i = 0; i < m_choices.Count; )
				{
					m_this.m_icurLine = i;
					var spec = m_choices[i];
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
						switch(spec.Flid)
						{
						case InterlinLineChoices.kflidWord:
								DisplayWord(spec, i, wag);
								break;
						case InterlinLineChoices.kflidWordGloss:
								DisplayWordGloss(spec, i);
								break;
						case InterlinLineChoices.kflidWordPos:
								DisplayWordPOS(i);
								break;
						}
						i++;
					}
				}
				m_this.m_icurLine = 0;
			}

			/// <summary>
			/// If we are displaying the baseline, and should display a substitute string rather than
			/// the requested WS of the wordform, return the substitute string. Otherwise return null.
			/// </summary>
			private ITsString GetRealForm(int ws, int choiceIndex)
			{
				if (choiceIndex != 0)
					return null; // only ever correct the baselin
				if (ws != m_this.m_wsVernForDisplay)
					return null; // only ever correct for the default vernacular WS.
				if (m_analysisOccurrence != null)
					return m_analysisOccurrence.BaselineText;
				return null;
			}


			private void DisplayWord(InterlinLineSpec spec, int choiceIndex, IAnalysis wag)
			{
				var wsActual = m_this.GetRealWsOrBestWsForContext(m_hvoWordform, spec);
				var tssRealForm = GetRealForm(wsActual, choiceIndex);
				if (tssRealForm != null && tssRealForm.Length > 0)
				{
					m_this.IsDoingRealWordForm = true;
					// LT-12203 Text chart doesn't want multiple analyses highlighting
					if (m_fshowMultipleAnalyses)
					{
						//identify those words the user has yet to approve which have multiple possible
						//guesses user or machine, and set the background to a special color
						var word = wag as IWfiWordform;
						if (word != null)
						{
							//test if there are multiple analyses that a user might choose from
							if (SandboxBase.GetHasMultipleRelevantAnalyses(word))
							{
								m_this.SetGuessing(m_vwenv, MultipleApprovedGuessColor); //There are multiple options, set the color
							}
						}
					}
					m_vwenv.AddString(tssRealForm);
					m_this.IsDoingRealWordForm = false;
					return;
				}
				switch (m_defaultObj.ClassID)
				{
				case WfiWordformTags.kClassId:
				case WfiAnalysisTags.kClassId:
				case WfiGlossTags.kClassId:
					m_vwenv.AddObj(m_hvoWordform, m_this, kfragLineChoices + choiceIndex);
					break;
				default:
					throw new Exception("Invalid type found in Segment analysis");
				}
			}

			private void DisplayMorphemes()
			{
				switch(m_defaultObj.ClassID)
				{
				case WfiWordformTags.kClassId:
				case WfiAnalysisTags.kClassId:
					if (m_this.m_fShowMorphBundles)
					{
						// Display the morpheme bundles.
						if (m_hvoDefault != m_hvoWordBundleAnalysis)
						{
							// Real analysis isn't what we're displaying, so morph breakdown
							// is a guess. Is it a human-approved guess?
							bool isHumanGuess = m_this.Decorator.get_IntProp(m_hvoDefault, InterlinViewDataCache.OpinionAgentFlid) !=
																			(int) AnalysisGuessServices.OpinionAgent.Parser;
							m_this.SetGuessing(m_vwenv, isHumanGuess ? ApprovedGuessColor : MachineGuessColor);
						}
						m_vwenv.AddObj(m_hvoDefault, m_this, kfragAnalysisMorphs);
					}
					break;
				case WfiGlossTags.kClassId:

					if (m_this.m_fShowMorphBundles)
					{
						m_hvoWfiAnalysis = m_defaultObj.Owner.Hvo;
						// Display all the morpheme stuff.
						if (m_hvoWordBundleAnalysis == m_hvoWordform)
						{
							// Real analysis is just word, one we're displaying is a default
							m_this.SetGuessing(m_vwenv);
						}
						m_vwenv.AddObj(m_hvoWfiAnalysis, m_this, kfragAnalysisMorphs);
					}
					break;
				default:
					throw new Exception("Invalid type found in Segment analysis");
				}
			}

			private void DisplayWordGloss(InterlinLineSpec spec, int choiceIndex)
			{

				switch(m_defaultObj.ClassID)
				{
				case WfiWordformTags.kClassId:
					m_this.SetColor(m_vwenv, m_this.LabelRGBFor(choiceIndex)); // looks like missing word gloss.
					m_vwenv.AddString(m_this.m_tssMissingGloss);
					break;
				case WfiAnalysisTags.kClassId:
					var wa = m_defaultObj as IWfiAnalysis;
					if (wa.MeaningsOC.Count == 0)
					{
						// There's no gloss, display something indicating it is missing.
						m_this.SetColor(m_vwenv, m_this.LabelRGBFor(choiceIndex));
						m_vwenv.AddString(m_this.m_tssMissingGloss);
					}
					else
					{
						m_vwenv.AddObj(wa.MeaningsOC.First().Hvo, m_this, kfragLineChoices + choiceIndex);
					}
					break;
				case WfiGlossTags.kClassId:
					if (m_hvoWordBundleAnalysis == m_hvoDefault)
					{
						var wsActual = spec.WritingSystem;
						if (spec.IsMagicWritingSystem)
							wsActual = GetRealWs(m_cache, m_hvoWordBundleAnalysis, spec, m_this.m_wsAnalysis);
						// We're displaying properties of the current object, can do
						// straightforwardly
						m_this.FormatGloss(m_vwenv, wsActual);
						m_vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
							(int)SpellingModes.ksmForceCheck);
						m_vwenv.AddObj(m_hvoWordBundleAnalysis, m_this, kfragLineChoices + choiceIndex);
					}
					else
					{
						m_this.SetGuessing(m_vwenv);
						m_vwenv.AddObj(m_hvoDefault, m_this, kfragLineChoices + choiceIndex);
					}
					break;
				default:
					throw new Exception("Invalid type found in Segment analysis");
				}
			}

			private void DisplayWordPOS(int choiceIndex)
			{
				switch(m_defaultObj.ClassID)
				{
				case WfiWordformTags.kClassId:
					m_this.SetColor(m_vwenv, m_this.LabelRGBFor(choiceIndex)); // looks like missing word POS.
					m_vwenv.AddString(m_this.m_tssMissingAnalysisPos);
					break;
				case WfiAnalysisTags.kClassId:
					if (m_hvoDefault != m_hvoWordBundleAnalysis)
					{
						// Real analysis isn't what we're displaying, so POS is a guess.
						bool isHumanApproved = m_this.Decorator.get_IntProp(m_hvoDefault, InterlinViewDataCache.OpinionAgentFlid)
																			!= (int)AnalysisGuessServices.OpinionAgent.Parser;

						m_this.SetGuessing(m_vwenv, isHumanApproved ? ApprovedGuessColor : MachineGuessColor);
					}
					m_this.AddAnalysisPos(m_vwenv, m_hvoDefault, m_hvoWordBundleAnalysis, choiceIndex);
					break;
				case WfiGlossTags.kClassId:
					m_hvoWfiAnalysis = m_defaultObj.Owner.Hvo;
					if (m_hvoWordBundleAnalysis == m_hvoWordform) // then our analysis is a guess
						m_this.SetGuessing(m_vwenv);
					m_vwenv.AddObj(m_hvoWfiAnalysis, m_this, kfragAnalysisCategoryChoices + choiceIndex);
					break;
				default:
					throw new Exception("Invalid type found in Segment analysis");
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
						AddFreeformAnnotations(vwenv, hvo);
						break;
					}
				default:
				if (frag >= kfragWordGlossWs && frag < kfragWordGlossWs + m_WsList.AnalysisWsIds.Length)
				{
					// Displaying one ws of all the glosses of an analysis, separated by commas.
					vwenv.OpenParagraph();
					IWfiAnalysis wa = m_coRepository.GetObject(hvo) as IWfiAnalysis;
					int i = 0;
					foreach (IWfiGloss gloss in wa.MeaningsOC)
					{
						if (i != 0)
							vwenv.AddString(CommaSpaceString);
						vwenv.AddObj(gloss.Hvo, this, frag);
						i++;
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

		protected virtual void AddFreeformAnnotations(IVwEnv vwenv, int hvoSeg)
		{
			// Add them in the order specified. Each iteration adds a group with the same flid but (typically)
			// different writing systems.
			for (int ispec = m_lineChoices.FirstFreeformIndex;
				 ispec < m_lineChoices.Count;
				 ispec += m_lineChoices.AdjacentWssAtIndex(ispec).Length)
			{
				int flid = m_lineChoices[ispec].Flid;
				switch(flid)
				{
					case InterlinLineChoices.kflidFreeTrans:
					case InterlinLineChoices.kflidLitTrans:
						// These are properties of the current object.
						AddFreeformComment(vwenv, hvoSeg, ispec);
						break;
					case InterlinLineChoices.kflidNote:
						// There's a sequence of these, we use a trick with the frag to indicate which
						// index into line choices we want to use to display each of them.
						vwenv.AddObjVecItems(SegmentTags.kflidNotes, this, kfragSegFfChoices + ispec);
						break;
					default:
						break; // unknown type, ignore it.

				}
			}
		}

		internal ICmAnnotationDefn SegDefnFromFfFlid(int flid)
		{
			CheckDisposed();
			switch(flid)
			{
			case InterlinLineChoices.kflidFreeTrans:
					throw new InvalidOperationException("Uses obsolete FT annotation defn that is no longer in system.");
			case InterlinLineChoices.kflidLitTrans:
					throw new InvalidOperationException("Uses obsolete FT annotation defn that is no longer in system.");
			case InterlinLineChoices.kflidNote:
					return GetAnnDefnId(m_cache, CmAnnotationDefnTags.kguidAnnNote);
			default:
				break; // unknown type, ignore it.
			}
			return null;
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			CheckDisposed();

			if (tag == SimpleRootSite.kTagUserPrompt)
			{
				// In this case, frag is the writing system we really want the user to type.
				// We put a zero-width space in that WS at the start of the string since that is the
				// WS the user will end up typing in.
				ITsStrBldr bldr = TsStringUtils.MakeTss(ITextStrings.ksEmptyFreeTransPrompt, m_cache.DefaultUserWs).GetBldr();
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

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Is this still used? GJM -- 3/29/10
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		//{
		//	CheckDisposed();

		//	if(tag != SimpleRootSite.kTagUserPrompt)
		//		return tssVal;

		//	// wait until an IME composition is completed before switching the user prompt to a comment
		//	// field, otherwise setting the comment will terminate the composition (LT-9929)
		//	if (m_rootsite.RootBox.IsCompositionInProgress)
		//		return tssVal;

		//	if (tssVal.Length == 0)
		//	{
		//		// User typed something (return?) which didn't actually put any text over the prompt.
		//		// No good replacing it because we'll just get the prompt string back and won't be
		//		// able to make our new selection.
		//		return tssVal;
		//	}

		//	// Get information about current selection
		//	SelectionHelper helper = SelectionHelper.Create(vwsel, m_rootsite);

		//	ICmAnnotation ann = m_coRepository.GetObject(hvo) as ICmAnnotation;

		//	ITsStrBldr bldr = tssVal.GetBldr();
		//	bldr.SetIntPropValues(0, bldr.Length, SimpleRootSite.ktptUserPrompt, -1, -1);
		//	bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptSpellCheck, -1, -1);

		//	// Add the text the user just typed to the comment - this destroys the selection
		//	// because we replace the user prompt. We use the frag to note the WS of interest.
		//	ann.Comment.set_String(frag, bldr.GetString());

		//	// now restore the selection (in the new property).
		//	helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, CmAnnotationTags.kflidComment);
		//	helper.SetTextPropId(SelectionHelper.SelLimitType.End, CmAnnotationTags.kflidComment);
		//	helper.NumberOfPreviousProps = m_cpropActiveFreeform;
		//	helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.End, m_cpropActiveFreeform);
		//	helper.MakeRangeSelection(m_rootsite.RootBox, true);
		//	SetActiveFreeform(0, 0, 0);
		//	return tssVal;
		//}

		// A small sample indicates that each character of input results in about 20 pixels of width in an interlinear display.
		// May want to adjust down for views without morphology.
		private const int kPixelsPerChar = 20;
		private const int kPixelsForRowLabels = 30; // very rough; may want to omit for views without them.
		private const int kPixelsPerLine = 16;

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

			var obj = m_cache.ServiceLocator.ObjectRepository.GetObject(hvo);

			switch (frag)
			{
					// If you change this, remember that an estimate that is too HIGH just means it takes a few iterations
					// to fill the screen, and the thumb on the scroll bar is a bit small. But if it is too SMALL,
					// we will expand more objects than we need, which is much more expensive.
					// Also remember: this gets called for EVERY paragraph and segment in
					// the text, it needs to run pretty fast.
				case kfragInterlinPara:
					var para = obj as IStTxtPara;
					if (para == null)
					{
						Debug.Assert(obj is IStTxtPara);
						return 1200;
					}
					return EstimateParaHeight(para, dxAvailWidth);
				case kfragTxtSection: // Is this even used??
					var section = obj as IScrSection;
					if (section == null)
					{
						Debug.Assert(obj is IScrSection);
						return 2000;
					}
					return EstimateStTextHeight(section.HeadingOA, dxAvailWidth) +
						EstimateStTextHeight(section.ContentOA, dxAvailWidth);
				case kfragParaSegment:
					var seg = obj as ISegment;
					if (seg == null)
					{
						Debug.Assert(obj is ISegment);
						return 400;
					}
					return EstimateSegmentHeight(seg, dxAvailWidth);
				default:
					return 500;
					// Not possible AFAIK; in case we missed one, large makes for over-long scroll bars but avoids excess layout work.
			}
		}

		private int EstimateStTextHeight(IStText text, int dxAvailWidth)
		{
			return text.ParagraphsOS.Sum(p => EstimateParaHeight((IStTxtPara)p, dxAvailWidth));
		}

		private int EstimateParaHeight(IStTxtPara para, int dxAvailWidth)
		{
			return para.SegmentsOS.Sum(s => EstimateSegmentHeight(s, dxAvailWidth));
		}

		private int EstimateSegmentHeight(ISegment seg, int dxAvailWidth)
		{
			var length = seg.BaselineText.Length;
			int width = length*kPixelsPerChar + kPixelsForRowLabels;
			var rows = dxAvailWidth/width + 1;
			return rows*m_lineChoices.Count*kPixelsPerLine;
		}

		/// <summary>
		/// Load data for a group of segments
		/// </summary>
		/// <param name="rghvo"></param>
		/// <param name="hvoPara"></param>
		internal virtual void LoadDataForSegments(int[] rghvo, int hvoPara)
		{
			var segmentRepository = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
			EnsureLoader();
			foreach (int hvoSeg in rghvo)
			{
				var seg = segmentRepository.GetObject(hvoSeg);
				m_loader.LoadSegmentData(seg);
			}
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
				if (tag == StTxtParaTags.kflidSegments)
				{
					LoadDataForSegments(rghvo, hvoParent);
				}
				if (tag != StTextTags.kflidParagraphs)
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

		private void LoadParaData(int hvoPara)
		{
			if (hvoPara == 0)
				return;
			LoadParaData(StTxtParaRepository.GetObject(hvoPara));
		}

		public void LoadParaData(IStTxtPara para)
		{
			CheckDisposed();
			EnsureLoader();
			m_loader.LoadParaData(para);
		}

		private void EnsureLoader()
		{
			if (m_loader == null)
				m_loader = CreateParaLoader();
		}

		internal virtual IParaDataLoader CreateParaLoader()
		{
			return new InterlinViewCacheLoader(new AnalysisGuessServices(m_cache), Decorator);
		}

		internal void RecordGuessIfNotKnown(AnalysisOccurrence selected)
		{
			EnsureLoader();
			m_loader.RecordGuessIfNotKnown(selected);
		}

		public IAnalysis GetGuessForWordform(IWfiWordform wf, int ws)
		{
			EnsureLoader();
			return m_loader.GetGuessForWordform(wf, ws);
		}

		/// <summary>
		/// Get an AnnotationDefn with the (English) name specified, and cache it in cachedVal...unless it is already cached,
		/// in which case, just return it.
		/// </summary>
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
				cachedVal = GetAnnDefnId(cache, guid).Hvo;
			}
			return cachedVal;

		}

		internal static ICmAnnotationDefn GetAnnDefnId(FdoCache cache, Guid guid)
		{
			return cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(guid);
		}

		/// <summary>
		/// Obtain the ID of the AnnotationDefn called (in English) 'Note'.
		/// </summary>
		internal int NoteSegmentDefn
		{
			get
			{
				CheckDisposed();
				return GetAnnDefnId(m_cache, CmAnnotationDefnTags.kguidAnnNote, ref m_hvoAnnDefNote);
			}
		}

		/// <summary>
		/// Add a display of the category of hvoAnalysis.
		/// If choiceOffset is -1, display the current analysis writing systems, otherwise,
		/// display the one indicated (choiceIndex is an index into line choices).
		/// When choice index is not -1, hvoAnalysis may not be the current object.
		/// In that case, we invoke AddObj with a special flid which results in a recursive
		/// call to this, but with the correct current object.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoAnalysis"></param>
		protected void AddAnalysisPos(IVwEnv vwenv, int hvoAnalysis, int hvoCurrent, int choiceIndex)
		{
			IWfiAnalysis wa = m_coRepository.GetObject(hvoAnalysis) as IWfiAnalysis;
			int hvoPos = wa.CategoryRA != null ? wa.CategoryRA.Hvo : 0;
			SetColor(vwenv, LabelRGBFor(choiceIndex));
			if (hvoPos == 0)
			{
				vwenv.OpenParagraph();
				vwenv.NoteDependency(new int[] {hvoAnalysis},
					new int[] {WfiAnalysisTags.kflidCategory}, 1);
				vwenv.AddString(m_tssMissingAnalysisPos);
				vwenv.CloseParagraph();
			}
			else if (choiceIndex < 0)
			{
				vwenv.AddObjProp(WfiAnalysisTags.kflidCategory, this, kfragCategory);
			}
			else
			{
			if (hvoCurrent == hvoAnalysis)
				vwenv.AddObjProp(WfiAnalysisTags.kflidCategory, this, kfragLineChoices + choiceIndex);
			else
				vwenv.AddObj(hvoAnalysis, this, kfragAnalysisCategoryChoices + choiceIndex); // causes recursive call with right hvoCurrent
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

	/// <summary>
	/// interface for loading the decorator.
	/// </summary>
	public interface IParaDataLoader
	{
		void LoadParaData(IStTxtPara para);
		void LoadSegmentData(ISegment seg);
		void ResetGuessCache();
		bool UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis);
		void RecordGuessIfNotKnown(AnalysisOccurrence occurrence);
		IAnalysis GetGuessForWordform(IWfiWordform wf, int ws);
		AnalysisGuessServices GuessServices { get; }
	}

	public class InterlinViewCacheLoader : IParaDataLoader
	{
		private InterlinViewDataCache m_sdaDecorator;
		public InterlinViewCacheLoader(AnalysisGuessServices guessServices,
			InterlinViewDataCache sdaDecorator)
		{
			GuessServices = guessServices;
			m_sdaDecorator = sdaDecorator;
		}

		/// <summary>
		///
		/// </summary>
		public AnalysisGuessServices GuessServices { get; private set; }
		protected InterlinViewDataCache Decorator { get { return m_sdaDecorator; }  }

		#region IParaDataLoader Members

		public void LoadParaData(IStTxtPara para)
		{
			if (para.SegmentsOS.Count == 0)
				return;
			LoadAnalysisData(para, null);
		}

		#endregion

		protected virtual void NoteCurrentAnnotation(AnalysisOccurrence occurrence)
		{
			// override to track changes.
		}

		/// <summary>
		/// Load guesses for the paragraph.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="wordforms"></param>
		internal void LoadAnalysisData(IStTxtPara para, HashSet<IWfiWordform> wordforms)
		{
			if (para.SegmentsOS.Count == 0 || para.SegmentsOS[0].AnalysesRS.Count == 0)
				return;
			// TODO: reload decorator at the appropriate time.
			foreach (var occurrence in SegmentServices.StTextAnnotationNavigator.GetWordformOccurrencesAdvancingInPara(para))
			{
				var wag = new AnalysisTree(occurrence.Analysis);
				if (wordforms == null || wordforms.Contains(wag.Wordform))
				{
					NoteCurrentAnnotation(occurrence);
					RecordGuessIfAvailable(occurrence);
				}
			}
		}

		public void RecordGuessIfNotKnown(AnalysisOccurrence occurrence)
		{
			if (m_sdaDecorator.get_ObjectProp(occurrence.Analysis.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid) == 0)
				RecordGuessIfAvailable(occurrence);
		}

		public void LoadSegmentData(ISegment seg)
		{
			for (int i = 0; i < seg.AnalysesRS.Count; i++)
			{
				var occurrence = new AnalysisOccurrence(seg, i);
				if (occurrence.HasWordform)
					RecordGuessIfAvailable(occurrence);
			}
		}

		private void RecordGuessIfAvailable(AnalysisOccurrence occurrence)
		{
			// TODO: deal with lowercase forms of sentence initial occurrences.

			// we don't provide guesses for glosses
			if (occurrence.Analysis is IWfiGloss)
				return;
			// next get the best guess for wordform or analysis

			IAnalysis wag = occurrence.Analysis;
			IAnalysis wagGuess;
			// now record the guess in the decorator.
			// Todo JohnT: if occurrence.Indx is 0, record using DefaultStartSentenceFlid.
			if (GuessServices.TryGetBestGuess(occurrence, out wagGuess))
			{
				SetObjProp(wag.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid, wagGuess.Hvo);
				SetInt(wagGuess.Analysis.Hvo, InterlinViewDataCache.OpinionAgentFlid, (int)GuessServices.GetOpinionAgent(wagGuess.Analysis));
			}
			else
			{
				SetObjProp(wag.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid, 0);
			}
		}

		public IAnalysis GetGuessForWordform(IWfiWordform wf, int ws)
		{
			return GuessServices.GetBestGuess(wf, ws);
		}

		/// <summary>
		/// this is so we can subclass the loader to test whether values have actually changed.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="objValue"></param>
		protected virtual void SetObjProp(int hvo, int flid, int objValue)
		{
			m_sdaDecorator.SetObjProp(hvo, flid, objValue);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="n"></param>
		protected virtual void SetInt(int hvo, int flid, int n)
		{
			m_sdaDecorator.SetInt(hvo, flid, n);
		}

		#region IParaDataLoader Members


		public void ResetGuessCache()
		{
			// recreate the guess services, so they will use the latest FDO data.
			GuessServices.ClearGuessData();
			// clear the Decorator cache for the guesses, so it won't have any stale data.
			m_sdaDecorator.ClearPropFromCache(InterlinViewDataCache.AnalysisMostApprovedFlid);
		}

		/// <summary>
		/// Replacing a single occurrence, we MIGHT need to reset the guess cache.
		/// </summary>
		public bool UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			var result = GuessServices.UpdatingOccurrence(oldAnalysis, newAnalysis);
			if (result)
				m_sdaDecorator.ClearPropFromCache(InterlinViewDataCache.AnalysisMostApprovedFlid);
			return result;
		}

		#endregion
	}

	/// <summary>
	/// Updates the paragraphs interlinear data and collects which annotations
	/// have been affected so we can update the display appropriately.
	/// </summary>
	internal class ParaDataUpdateTracker : InterlinViewCacheLoader
	{
		private HashSet<AnalysisOccurrence> m_annotationsChanged = new HashSet<AnalysisOccurrence>();
		private AnalysisOccurrence m_currentAnnotation;
		HashSet<int> m_analysesWithNewGuesses = new HashSet<int>();

		public ParaDataUpdateTracker(AnalysisGuessServices guessServices, InterlinViewDataCache sdaDecorator) :
			base(guessServices, sdaDecorator)
		{
		}

		protected override void NoteCurrentAnnotation(AnalysisOccurrence occurrence)
		{
			m_currentAnnotation = occurrence;
			base.NoteCurrentAnnotation(occurrence);
		}

		private void MarkCurrentAnnotationAsChanged()
		{
			// something has changed in the cache for the annotation or its analysis,
			// so mark it as changed.
			m_annotationsChanged.Add(m_currentAnnotation);
		}

		/// <summary>
		/// the annotations that have changed, or their analysis, in the cache
		/// and for which we need to do propchanges to update the display
		/// </summary>
		internal IList<AnalysisOccurrence> ChangedAnnotations
		{
			get { return m_annotationsChanged.ToArray(); }
		}

		protected override void SetObjProp(int hvo, int flid, int newObjValue)
		{
			int oldObjValue = Decorator.get_ObjectProp(hvo, flid);
			if (oldObjValue != newObjValue)
			{
				base.SetObjProp(hvo, flid, newObjValue);
				m_analysesWithNewGuesses.Add(hvo);
				MarkCurrentAnnotationAsChanged();
				return;
			}
			// If we find more than one occurrence of the same analysis, only the first time
			// will its guess change. But all of them need to be updated! So any occurrence whose
			// guess has changed needs to be marked as changed.
			if (m_currentAnnotation != null && m_currentAnnotation.Analysis !=null
				&& m_analysesWithNewGuesses.Contains(m_currentAnnotation.Analysis.Hvo))
			{
				MarkCurrentAnnotationAsChanged();
			}
		}

		protected override void SetInt(int hvo, int flid, int newValue)
		{
			int oldValue = Decorator.get_IntProp(hvo, flid);
			if (oldValue != newValue)
			{
				base.SetInt(hvo, flid, newValue);
				MarkCurrentAnnotationAsChanged();
			}
		}

	}
}
