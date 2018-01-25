// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// View constructor for InterlinView. Just to get something working, currently
	/// it is just a literal.
	/// </summary>
	internal class InterlinVc : FwBaseVc, IDisposable
	{
		#region Constants and other similar ints.

		internal int krgbNoteLabel = 100 + (100 << 8) + (100 << 16); // equal amounts of three colors produces a gray.
		internal const int kfragInterlinPara = 100000;
		protected internal const int kfragBundle = 100001;
		internal const int kfragMorphBundle = 100002;
		internal const int kfragAnalysis = 100003;
		internal const int kfragPostfix = 100004;
		internal const int kfragMorphForm = 100005;
		internal const int kfragPrefix = 100006;
		internal const int kfragCategory = 100007;
		internal const int kfragAnalysisSummary = 100009;
		internal const int kfragAnalysisMorphs = 100010;
		internal const int kfragSenseName = 100012;
		internal const int kfragSingleInterlinearAnalysisWithLabels = 100013; // Recycle int for: internal const int kfragMissingGloss = 100013;
		internal const int kfragDefaultSense = 100014; // Recycle int for: internal const int kfragMissingSenseobj = 100014;
		internal const int kfragBundleMissingSense = 100015;
		internal const int kfragAnalysisMissingPos = 100016;
		internal const int kfragMsa = 100017;
		internal const int kfragMissingAnalysis = 100019;
		internal const int kfragAnalysisMissingGloss = 100021;
		internal const int kfragWordformForm = 100022;
		internal const int kfragWordGlossGuess = 100023;
		internal const int kfragTxtSection = 100025;
		internal const int kfragStText = 100026;
		internal const int kfragParaSegment = 100027;
		internal const int kfragSegFf = 100028;
		internal const int kfragWordGloss = 100029;
		internal const int kfragIsolatedAnalysis = 100030;
		internal const int kfragMorphType = 100031;
		internal const int kfragPossibiltyAnalysisName = 100032;
		public const int kfragSingleInterlinearAnalysisWithLabelsLeftAlign = 100034;
		/// <summary>
		/// Bundle of all the freeform annotations, displayed as a fake property of 'this' to reduce
		/// what we have to regenerate when doing special prompts (Press Enter to ...).
		/// </summary>
		private const int kfragFreeformBundle = 100035;
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
		internal const int ktagGlossAppend = -50;
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
		internal int ktagSegmentForms;

		bool m_fIsAddingRealFormToView; // indicates we are in the context of adding real form string to the vwEnv.

		#endregion Constants and other similar ints.

		#region Data members

		protected bool m_fShowDefaultSense; // Use false to not change prior behavior.
		protected bool m_fHaveOpenedParagraph; // Use false to not change prior behavior.
		protected WritingSystemManager m_wsManager;
		protected ISegmentRepository m_segRepository;
		protected ICmObjectRepository m_coRepository;
		protected IWfiMorphBundleRepository m_wmbRepository;
		protected IWfiAnalysisRepository m_analRepository;
		protected int m_wsVernForDisplay;

		protected int m_wsAnalysis;
		protected int m_wsUi;
		internal WsListManager m_WsList;
		ITsString m_tssMissingAnalysis; // The whole analysis is missing. This shows up on the morphs line.
		ITsString m_tssMissingGloss; // A word gloss is missing.
		ITsString m_tssMissingGlossPrepend;
		ITsString m_tssMissingGlossAppend;
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
		ITsString m_tssPendingGlossAffix; // LexGloss line GlossAppend or GlossPrepend
		int m_mpBundleHeight = 0; // millipoint height of interlinear bundle.
		bool m_fShowMorphBundles = true;
		bool m_fRtl;
		IDictionary<ILgWritingSystem, ITsString> m_mapWsDirTss = new Dictionary<ILgWritingSystem, ITsString>();
		// AnnotationDefns we need
		int m_hvoAnnDefNote;
		MsaVc m_msaVc;
		InterlinLineChoices m_lineChoices;
		protected IVwStylesheet m_stylesheet;
		IParaDataLoader m_loader;
		private HashSet<int> m_vernWss; // all vernacular writing systems
		private int m_selfFlid;
		private int m_leftPadding;

		#endregion Data members

		/// <summary>
		/// Initializes a new instance of the <see cref="InterlinVc"/> class.
		/// </summary>
		/// <remarks>We use the default analysis writing system as the default, even though
		/// this view displays data in multiple writing systems. It's pretty arbitrary in this
		/// case, but we need a valid WS because if we get an ORC, we have to create a Ts String
		/// using some writing system.</remarks>
		public InterlinVc(LcmCache cache)
			: base(cache.DefaultAnalWs)
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
			m_tssMissingGloss = TsStringUtils.MakeString(ITextStrings.ksStars, m_wsAnalysis);
			m_tssMissingGlossPrepend = TsStringUtils.MakeString(ITextStrings.ksStars + MorphServices.kDefaultSeparatorLexEntryInflTypeGlossAffix, m_wsAnalysis);
			m_tssMissingGlossAppend = TsStringUtils.MakeString(MorphServices.kDefaultSeparatorLexEntryInflTypeGlossAffix + ITextStrings.ksStars, m_wsAnalysis);
			m_tssMissingSense = m_tssMissingGloss;
			m_tssMissingMsa = m_tssMissingGloss;
			m_tssMissingAnalysisPos = m_tssMissingGloss;
			m_tssEmptyAnalysis = TsStringUtils.EmptyString(m_wsAnalysis);
			m_WsList = new WsListManager(m_cache);
			m_tssEmptyPara = TsStringUtils.MakeString(ITextStrings.ksEmptyPara, m_wsAnalysis);
			m_tssSpace = TsStringUtils.MakeString(" ", m_wsAnalysis);
			m_msaVc = new MsaVc(m_cache);
			m_vernWss = WritingSystemServices.GetAllWritingSystems(m_cache, "all vernacular", null, 0, 0);
			// This usually gets overridden, but ensures default behavior if not.
			m_lineChoices = InterlinLineChoices.DefaultChoices(m_cache.LangProject, WritingSystemServices.kwsVernInParagraph, WritingSystemServices.kwsAnal);
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
		public int CurrentLine { get; private set; }

		/// <summary>
		/// Normally gets some virtual property tags we need for stuff above the bundle level.
		/// Code that is only using fragments at or below bundle may override this to do nothing,
		/// and then need not set up the virtual property handlers. See ConstChartVc.
		/// </summary>
		protected virtual void GetSegmentLevelTags(LcmCache cache)
		{
		}

		/// <summary>
		/// setups up the display to work with the given wsVern.
		/// </summary>
		private void SetupRealVernWsForDisplay(int wsVern)
		{
			if (wsVern <= 0)
			{
				throw new ArgumentException($"Expected a real vernacular ws (got {wsVern}).");
			}
			if (m_wsVernForDisplay == wsVern)
			{
				return;	// already setup
			}
			m_wsVernForDisplay = wsVern;
			m_tssEmptyVern = TsStringUtils.EmptyString(wsVern);
			m_fRtl = m_wsManager.Get(wsVern).RightToLeftScript;
			m_tssMissingAnalysis = TsStringUtils.MakeString(ITextStrings.ksStars, wsVern);
			m_tssMissingMorph = m_tssMissingAnalysis;
			m_tssMissingEntry = m_tssMissingAnalysis;
		}

		/// <summary>
		/// Answer true if the specified word can be analyzed. This is a further check after
		/// ensuring it has an InstanceOf. It is equivalent to the check made in case kfragBundle of
		/// Display(), but that already has access to the writing system of the Wordform.
		/// GJM - Jan 19,'10 Added check to see if this occurrence is actually Punctuation.
		/// Punctuation cannot be analyzed.
		/// </summary>
		internal bool CanBeAnalyzed(AnalysisOccurrence occurrence)
		{
			var occurrenceWs = occurrence.BaselineWs;
			if (occurrence.Analysis is IPunctuationForm)
			{
				return false;
			}
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
			{
				throw new ObjectDisposedException(GetType().ToString(), "This object is being used after it has been disposed: this is an Error.");
			}
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
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (IsDisposed)
			{
				// No need to do it more than once.
				return;
			}
			if (fDisposing)
			{
				// Dispose managed resources here.
				m_WsList?.Dispose();
				;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_msaVc = null;
			m_cache = null;

			m_tssMissingMorph = null; // Same as m_tssMissingAnalysis.
			m_tssMissingSense = null; // Same as m_tssMissingGloss.
			m_tssMissingMsa = null; // Same as m_tssMissingGloss.
			m_tssMissingAnalysisPos = null; // Same as m_tssMissingGloss.
			m_tssMissingEntry = null; // Same as m_tssEmptyAnalysis.
			m_tssMissingAnalysis = null;
			m_tssMissingGloss = null;
			m_tssEmptyAnalysis = null;
			m_tssEmptyVern = null;
			m_tssEmptyPara = null;
			m_tssSpace = null;
			m_tssCommaSpace = null;
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

		internal AnalysisGuessServices GuessServices => m_loader?.GuessServices ?? new AnalysisGuessServices(m_cache);

		public bool UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			if (m_loader != null)
			{
				CheckDisposed();
				return m_loader.UpdatingOccurrence(oldAnalysis, newAnalysis);
			}
			return false;
		}

		private ITsString CommaSpaceString => m_tssCommaSpace ?? (m_tssCommaSpace = TsStringUtils.MakeString(", ", m_wsAnalysis));

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
		public static int ApprovedGuessColor => (int)CmObjectUi.RGB(200, 255, 255);

		/// <summary>
		/// Background color indicating there are multiple possible human approved guesses
		/// </summary>
		public static int MultipleApprovedGuessColor => (int)CmObjectUi.RGB(255, 255, 50);

		/// <summary>
		/// Background color for a guess that no human has ever endorsed directly.
		/// </summary>
		public static int MachineGuessColor => (int)CmObjectUi.RGB(254, 240, 206);

		/// <summary>
		/// </summary>
		internal InterlinDocRootSiteBase RootSite { get; set; }

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

		protected virtual int LabelRGBFor(int choiceIndex)
		{
			return LabelRGBFor(m_lineChoices[choiceIndex]);
		}

		protected virtual int LabelRGBFor(InterlinLineSpec spec)
		{
			return m_lineChoices.LabelRGBFor(spec);
		}

		/// <summary>
		/// Called right before adding a string or opening a flow object, sets its color.
		/// </summary>
		protected virtual void SetColor(IVwEnv vwenv, int color)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, color);
		}

		/// <summary>
		/// Add the specified string in the specified color to the display, using the UI Writing system.
		/// </summary>
		protected void AddColoredString(IVwEnv vwenv, int color, string str)
		{
			SetColor(vwenv, color);
			vwenv.AddString(TsStringUtils.MakeString(str, m_wsUi));
		}

		/// <summary>
		/// Set the background color that we use to indicate a guess.
		/// </summary>
		private void SetGuessing(IVwEnv vwenv, int bgColor)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, bgColor);
			UsingGuess = true;
		}

		private void SetGuessing(IVwEnv vwenv)
		{
			SetGuessing(vwenv, ApprovedGuessColor);
			UsingGuess = true;
		}

		public bool UsingGuess { get; set; }

		/// <summary>
		/// Get a guess for the given word or analysis.
		/// </summary>
		internal int GetGuess(IAnalysis analysis)
		{
			if (Decorator.get_IsPropInCache(analysis.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid, (int)CellarPropertyType.ReferenceAtomic, 0))
			{
				var hvoResult = Decorator.get_ObjectProp(analysis.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid);
				if(hvoResult != 0 && Cache.ServiceLocator.IsValidObjectId(hvoResult))
				{
					return hvoResult;  // may have been cleared by setting to zero, or the Decorator could have stale data
				}
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
			CheckDisposed();
			if (hvo == 0)
			{
				return;     // Can't do anything without an hvo (except crash -- see LT-9348).
			}

			switch (frag)
			{
				case kfragStText:   // new root object for InterlinDocChild.
					SetupRealVernWsForDisplay(WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsVernInParagraph, hvo, StTextTags.kflidParagraphs));
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
					{
						break;
					}
					// This puts ten points between segments. There's always 5 points below each line of interlinear;
					// if there are no freeform annotations another 5 points makes 10 between segments.
					// If there are freeforms, we need the full 10 points after the last of them.
					var haveFreeform = seg.FreeTranslation != null || seg.LiteralTranslation != null || seg.NotesOS.Count > 0;
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint, !haveFreeform ? 5000 : 10000);
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
						vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
					}
					vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
					vwenv.OpenParagraph();
					AddSegmentReference(vwenv, hvo);    // Calculate and display the segment reference.
					AddLabelPile(vwenv, m_cache, true, m_fShowMorphBundles);
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
					// We'd get the same visual effect from just calling AddFreeformAnnotations here. But then a regenerate
					// such as happens when hiding or showing a prompt has to redisplay the whole segment. This initially
					// makes it lazy, then the lazy stuff gets expanded. In the process we may get undesired scrolling (LT-12248).
					// So we insert another layer of object, allowing just the freeforms to be regenerated.
					var flidSelf = Cache.MetaDataCacheAccessor.GetFieldId2(CmObjectTags.kClassId, "Self", false);
					vwenv.AddObjProp(flidSelf, this, kfragFreeformBundle);
					vwenv.CloseDiv();
					break;
				case kfragFreeformBundle:
					AddFreeformAnnotations(vwenv, hvo);
					break;
				case kfragBundle:
					// One annotated word bundle; hvo is the IAnalysis object.
					// checking AllowLayout (especially in context of Undo/Redo make/break phrase)
					// helps prevent us from rebuilding the display until we've finished
					// reconstructing the data and cache. Otherwise we can crash.
					if (RootSite != null && !RootSite.AllowLayout)
					{
						return;
					}
					AddWordBundleInternal(hvo, vwenv);
					break;
				case kfragIsolatedAnalysis: // This one is used for an isolated HVO that is surely an analysis.
					{
						var wa = m_analRepository.GetObject(hvo);
						vwenv.AddObj(wa.Owner.Hvo, this, kfragWordformForm);
						if (m_fShowMorphBundles)
						{
							vwenv.AddObj(hvo, this, kfragAnalysisMorphs);
						}

						var chvoGlosses = wa.MeaningsOC.Count;
						for (var i = 0; i < m_WsList.AnalysisWsIds.Length; ++i)
						{
							SetColor(vwenv, LabelRGBFor(m_lineChoices.IndexOf(InterlinLineChoices.kflidWordGloss, m_WsList.AnalysisWsIds[i])));
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
					var cmorphs = 0;
					var co = m_coRepository.GetObject(hvo);
					if (co is IWfiAnalysis)
					{
						cmorphs = ((IWfiAnalysis)co).MorphBundlesOS.Count;
					}
					// We really want a variable for this...there have been pathological cases where
					// m_fHaveOpenedParagraph changed during the construction of the paragraph, and we want to be
					// sure to close the paragraph if we opened it.
					var openedParagraph = !m_fHaveOpenedParagraph;
					if (openedParagraph)
					{
						vwenv.OpenParagraph();
					}
					if (cmorphs == 0)
					{
						DisplayMorphBundle(vwenv, 0);
					}
					else
					{
						vwenv.AddObjVecItems(WfiAnalysisTags.kflidMorphBundles, this, kfragMorphBundle);
					}

					if (openedParagraph)
					{
						vwenv.CloseParagraph();
					}
					break;
				case kfragMorphType: // for export only at present, display the
					vwenv.AddObjProp(MoFormTags.kflidMorphType, this, kfragPossibiltyAnalysisName);
					break;
				case kfragPossibiltyAnalysisName:
					vwenv.AddStringAltMember(CmPossibilityTags.kflidName, m_cache.DefaultAnalWs, this);
					break;

				case kfragMorphBundle:
					// the lines of morpheme information (hvo is a WfiMorphBundle)
					// Make an 'inner pile' to contain the bundle of morph information.
					// Give it 10 points of separation from whatever follows.
					DisplayMorphBundle(vwenv, hvo);
					break;
				case kfragSingleInterlinearAnalysisWithLabels:
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
				case kfragWordformForm: // The form of a WviWordform.
					vwenv.AddStringAltMember(WfiWordformTags.kflidForm, m_wsVernForDisplay, this);
					break;
				case kfragPrefix:
					vwenv.AddUnicodeProp(MoMorphTypeTags.kflidPrefix, m_wsVernForDisplay, this);
					break;
				case kfragPostfix:
					vwenv.AddUnicodeProp(MoMorphTypeTags.kflidPostfix, m_wsVernForDisplay, this);
					break;
				case kfragSenseName: // The name (gloss) of a LexSense.
					foreach (var wsId in m_WsList.AnalysisWsIds)
					{
						vwenv.AddStringAltMember(LexSenseTags.kflidGloss, wsId, this);
					}
					break;
				case kfragCategory:
					// the category of a WfiAnalysis, a part of speech;
					// display the Abbreviation property inherited from CmPossibility.
					foreach (var wsId in m_WsList.AnalysisWsIds)
					{
						vwenv.AddStringAltMember(CmPossibilityTags.kflidAbbreviation, wsId, this);
					}
					break;
				default:
					if (frag >= kfragWordGlossWs && frag < kfragWordGlossWs + m_WsList.AnalysisWsIds.Length)
					{
						// Displaying one ws of the  form of a WfiGloss.
						vwenv.AddStringAltMember(WfiGlossTags.kflidForm, m_WsList.AnalysisWsIds[frag - kfragWordGlossWs], this);
					}
					else if (frag >= kfragLineChoices && frag < kfragLineChoices + m_lineChoices.Count)
					{
						var spec = m_lineChoices[frag - kfragLineChoices];
						vwenv.AddStringAltMember(spec.StringFlid, GetRealWsOrBestWsForContext(hvo, spec), this);
					}
					else if (frag >= kfragAnalysisCategoryChoices && frag < kfragAnalysisCategoryChoices + m_lineChoices.Count)
					{
						AddAnalysisPos(vwenv, hvo, hvo, frag - kfragAnalysisCategoryChoices);
					}
					else if (frag >= kfragMorphFormChoices && frag < kfragMorphFormChoices + m_lineChoices.Count)
					{
						DisplayMorphForm(vwenv, hvo, GetRealWsOrBestWsForContext(hvo, m_lineChoices[frag - kfragMorphFormChoices]));
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
		}

		private void JoinGlossAffixesOfInflVariantTypes(ILexEntryRef entryRef1, int wsPreferred, out ITsIncStrBldr sbPrepend1, out ITsIncStrBldr sbAppend1)
		{
			var glossWs1 = Cache.ServiceLocator.WritingSystemManager.Get(wsPreferred);
			MorphServices.JoinGlossAffixesOfInflVariantTypes(entryRef1.VariantEntryTypesRS, glossWs1,
															 out sbPrepend1, out sbAppend1);
		}

		/// <summary />
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
		protected void DisplayAnalysisAndCloseInnerPile(IVwEnv vwenv, AnalysisOccurrence analysisOccurrence, bool showMultipleAnalyses)
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
		protected virtual void SetupAndOpenInnerPile(IVwEnv vwenv)
		{
			// Make an 'inner pile' to contain the wordform and annotations.
			// Give whatever box we make 10 points of separation from whatever follows.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			// 5 points below also helps space out the paragraph.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint, 5000);
			vwenv.OpenInnerPile();
		}

		protected virtual void AddFreeformComment(IVwEnv vwenv, int hvoSeg, int lineChoiceIndex)
		{
			var wssAnalysis = m_lineChoices.AdjacentWssAtIndex(lineChoiceIndex, hvoSeg);
			if (wssAnalysis.Length == 0)
			{
				return;
			}
			vwenv.OpenDiv();
			SetParaDirectionAndAlignment(vwenv, wssAnalysis[0]);
			vwenv.OpenMappedPara();
			string label;
			int flid;
			var exporter = vwenv as InterlinearExporter;
			var dummyFlid = m_lineChoices[lineChoiceIndex].Flid;
			switch (dummyFlid)
			{
				case InterlinLineChoices.kflidFreeTrans:
					label = ITextStrings.ksFree_;
					flid = SegmentTags.kflidFreeTranslation;
					if (exporter != null)
					{
						exporter.FreeAnnotationType = "gls";
					}
					break;
				case InterlinLineChoices.kflidLitTrans:
					label = ITextStrings.ksLit_;
					flid = SegmentTags.kflidLiteralTranslation;
					if (exporter != null)
					{
						exporter.FreeAnnotationType = "lit";
					}
					break;
				case InterlinLineChoices.kflidNote:
					label = ITextStrings.ksNote_;
					flid = NoteTags.kflidContent;
					if (exporter != null)
					{
						exporter.FreeAnnotationType = "note";
					}
					break;
				default:
					throw new Exception("Unexpected FF annotation type");
			}
			SetNoteLabelProps(vwenv);
			// REVIEW: Should we set the label to a special color as well?
			var tssLabel = MakeUiElementString(label, m_cache.DefaultUserWs, propsBldr => propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn));
			var labelBldr = tssLabel.GetBldr();
			AddLineIndexProperty(labelBldr, lineChoiceIndex);
			tssLabel = labelBldr.GetString();
			var labelWidth = 0;
			int labelHeight; // unused
			if (wssAnalysis.Length > 1)
			{
				vwenv.get_StringWidth(tssLabel, null, out labelWidth, out labelHeight);
			}
			if (IsWsRtl(wssAnalysis[0]) != m_fRtl)
			{
				var bldr = tssLabel.GetBldr();
				bldr.Replace(bldr.Length - 1, bldr.Length, null, null);
				var tssLabelNoSpace = bldr.GetString();
				// (First) analysis language is upstream; insert label at end.
				AddTssDirForWs(vwenv, wssAnalysis[0]);
				AddFreeformComment(vwenv, hvoSeg, wssAnalysis[0], flid);
				AddTssDirForWs(vwenv, wssAnalysis[0]);
				if (wssAnalysis.Length != 1)
				{
					// Insert WS label for first line
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					AddTssDirForVernWs(vwenv);
					SetNoteLabelProps(vwenv);
					vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[0]));
				}
				AddTssDirForVernWs(vwenv);
				vwenv.AddString(m_tssSpace);
				AddTssDirForVernWs(vwenv);
				vwenv.AddString(tssLabelNoSpace);
				AddTssDirForVernWs(vwenv);
			}
			else
			{
				AddTssDirForVernWs(vwenv);
				vwenv.AddString(tssLabel);
				AddTssDirForVernWs(vwenv);
				if (wssAnalysis.Length == 1)
				{
					AddTssDirForWs(vwenv, wssAnalysis[0]);
					AddFreeformComment(vwenv, hvoSeg, wssAnalysis[0], flid);
				}
				else
				{
					SetNoteLabelProps(vwenv);
					vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[0]));
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					// label width unfortunately does not include trailing space.
					AddTssDirForVernWs(vwenv);
					AddTssDirForWs(vwenv, wssAnalysis[0]);
					AddFreeformComment(vwenv, hvoSeg, wssAnalysis[0], flid);
				}
			}
			// Add any other lines, each in its appropriate direction.
			for (var i = 1; i < wssAnalysis.Length; i++)
			{
				vwenv.CloseParagraph();
				// Indent subsequent paragraphs by the width of the main label.
				if (IsWsRtl(wssAnalysis[i]) != m_fRtl)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptTrailingIndent, (int)FwTextPropVar.ktpvMilliPoint, labelWidth);
				}
				else
				{
					vwenv.set_IntProperty((int) FwTextPropType.ktptLeadingIndent, (int) FwTextPropVar.ktpvMilliPoint, labelWidth);
				}
				SetParaDirectionAndAlignment(vwenv, wssAnalysis[i]);
				vwenv.OpenParagraph();
				if (IsWsRtl(wssAnalysis[i]) != m_fRtl)
				{
					// upstream...reverse everything.
					AddTssDirForWs(vwenv, wssAnalysis[i]);
					AddFreeformComment(vwenv, hvoSeg, wssAnalysis[i], flid);
					AddTssDirForWs(vwenv, wssAnalysis[i]);
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					AddTssDirForVernWs(vwenv);
					AddTssDirForVernWs(vwenv);
					SetNoteLabelProps(vwenv);
					vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[i]));
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					AddTssDirForVernWs(vwenv);
				}
				else
				{
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					AddTssDirForVernWs(vwenv);
					SetNoteLabelProps(vwenv);
					vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[i]));
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					AddTssDirForVernWs(vwenv);
					AddTssDirForWs(vwenv, wssAnalysis[i]);
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
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
			}
			if (IsWsRtl(wsAnalysis))
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
			}
		}

		private bool IsWsRtl(int wsAnalysis)
		{
			return m_wsManager.Get(wsAnalysis).RightToLeftScript;
		}

		private void DisplaySingleInterlinearAnalysisWithLabels(IVwEnv vwenv, int hvo)
		{
			// The interlinear bundle is not editable.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			if (m_fRtl)
			{
				// This must not be on the outer paragraph or we get infinite width.
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
			}
			vwenv.OpenParagraph();
			m_fHaveOpenedParagraph = true;
			AddLabelPile(vwenv, m_cache, true, m_fShowMorphBundles);
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
		}

		/// <summary>
		/// If the analysis writing system has the opposite directionality to the vernacular
		/// writing system, we need to add a directionality code to the data stream for the
		/// bidirectional algorithm not to jerk the insertion point around at every space.
		/// See LT-7738.
		/// </summary>
		private void AddTssDirForWs(IVwEnv vwenv, int ws)
		{
			var wsObj = m_wsManager.Get(ws);
			// Graphite doesn't handle bidi markers
			if (wsObj.IsGraphiteEnabled)
			{
				return;
			}

			ITsString tssDirWs;
			if (!m_mapWsDirTss.TryGetValue(wsObj, out tssDirWs))
			{
				var fRtlWs = wsObj.RightToLeftScript;
				tssDirWs = TsStringUtils.MakeString(fRtlWs ? "\x200F" : "\x200E", ws);
				m_mapWsDirTss.Add(wsObj, tssDirWs);
			}
			vwenv.AddString(tssDirWs);
		}

		private void AddTssDirForVernWs(IVwEnv vwenv)
		{
			AddTssDirForWs(vwenv, m_wsVernForDisplay);
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
			if (hvo == m_hvoActiveFreeform && flid == ActiveFreeformFlid)
			{
				return; // no changes; don't want to generate spurious selection changes which may trigger unwanted WS changes.
			}
			var helper = SelectionHelper.Create(RootSite);
			var hvoOld = m_hvoActiveFreeform;
			var flidOld = ActiveFreeformFlid;
			m_hvoActiveFreeform = hvo;
			ActiveFreeformFlid = flid;
			ActiveFreeformWs = ws;
			// The cpropPrevious we get from the selection may be one off, if a previous line is displaying
			// the prompt for another WS of the same object.
			if (hvoOld == hvo && m_cpropActiveFreeform <= cpropPrevious)
			{
				m_cpropActiveFreeform = cpropPrevious + 1;
			}
			else
			{
				m_cpropActiveFreeform = cpropPrevious;
			}

			// The old one is easy to turn off because we have a NoteDependency on it.
			if (hvoOld != 0)
			{
				RootSite.RootBox.PropChanged(hvoOld, flidOld, 0, 0, 0);
			}
			if (m_hvoActiveFreeform != 0)
			{
				// Pretend the 'Self' property of the segment has been changed.
				// This will force it to be re-displayed, with different results now m_hvoActiveFreeform etc are set.
				var flidSelf = Cache.MetaDataCacheAccessor.GetFieldId2(CmObjectTags.kClassId, "Self", false);
				RootSite.RootBox.PropChanged(m_hvoActiveFreeform, flidSelf, 0, 1, 1);
			}

			helper?.SetSelection(true, false);
		}


		private void AddFreeformComment(IVwEnv vwenv, int hvo, int ws, int flidTarget)
		{
			if (flidTarget != ActiveFreeformFlid || hvo != m_hvoActiveFreeform || ws != ActiveFreeformWs)
			{
				vwenv.AddStringAltMember(flidTarget, ws, this); // display normally, not the current prop
				return;
			}
			var tssVal = vwenv.DataAccess.get_MultiStringAlt(hvo, flidTarget, ws);
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
			var wsObj = m_wsManager.Get(ws);
			return m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj) &&
			       !m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Contains(wsObj);
		}

		internal bool IsAddingSegmentReference { get; private set; } = false;

		/// <summary>
		/// Add a segment number appropriate to the current segment being displayed.
		/// (See LT-1236.)
		/// </summary>
		private void AddSegmentReference(IVwEnv vwenv, int hvo)
		{
			var sbSegNum = new StringBuilder();
			var flid = 0;
			var seg = m_segRepository.GetObject(hvo);
			var para = seg.Paragraph;
			if (para != null)
			{
				var cseg = para.SegmentsOS.Count;
				var idxSeg = para.SegmentsOS.IndexOf(seg); // sda.GetObjIndex(hvoStPara, ktagParaSegments, hvo);
				var stText = para.Owner as IStText;
				if (stText != null)
				{
					flid = stText.OwningFlid;
				}
				if (flid == ScrSectionTags.kflidContent)
				{
					var scrPara = para as IScrTxtPara;
					// With null book name and trimmed it should have just chapter:v{a,b}.
					// The {a,b} above would not be the segment identifiers we add for multiple segments in
					// a verse, but the letters indicating that the verse label is for only part of the verse.
					// There is therefore a pathological case where, say, verse 4a as labeled in the main text
					// gets another letter because 4a has multiple segments 4aa, 4ab, etc.
					var chapRef = ScriptureServices.FullScrRef(scrPara, seg.BeginOffset, "").Trim();
					sbSegNum.Append(chapRef + ScriptureServices.VerseSegLabel(scrPara, idxSeg));
				}
				else
				{
					var idxPara = para.OwnOrd;
					if (idxPara >= 0)
					{
						sbSegNum.AppendFormat("{0}", idxPara + 1);
						if (idxSeg >= 0 && cseg > 1)
						{
							sbSegNum.AppendFormat(".{0}", idxSeg + 1);
						}
					}
				}
			}
			var tsbSegNum = TsStringUtils.MakeStrBldr();
			tsbSegNum.ReplaceTsString(0, tsbSegNum.Length, TsStringUtils.MakeString(sbSegNum.ToString(), m_cache.DefaultUserWs));
			tsbSegNum.SetIntPropValues(0, tsbSegNum.Length, (int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			var tssSegNum = tsbSegNum.GetString();
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)CmObjectUi.RGB(SystemColors.ControlText));
			try
			{
				IsAddingSegmentReference = true;
				vwenv.OpenInnerPile();
				vwenv.AddString(tssSegNum);
				vwenv.CloseInnerPile();
			}
			finally
			{
				IsAddingSegmentReference = false;
			}
		}

		/// <summary>
		/// try to get the ws specified by spec.WritingSystem, otherwise
		/// get the default vernacular ws for the display (e.g. ws of paragraph).
		/// </summary>
		internal int GetRealWsOrBestWsForContext(int hvo, InterlinLineSpec spec)
		{
			if (spec != null && !spec.IsMagicWritingSystem && spec.WritingSystem > 0)
			{
				return GetRealWs(m_cache, hvo, spec, spec.WritingSystem);
			}
			return GetRealWs(m_cache, hvo, spec, m_wsVernForDisplay);
		}

		private static int GetRealWs(LcmCache cache, int hvo, InterlinLineSpec spec, int wsPreferred)
		{
			var ws = 0;
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
		/// Displays a MorphBundle, setting the colors of its parts.
		/// </summary>
		private void DisplayMorphBundle(IVwEnv vwenv, int hvo)
		{
			IWfiMorphBundle wmb = null;
			if (hvo != 0)
			{
				wmb = m_wmbRepository.GetObject(hvo);
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.OpenInnerPile();
			var first = m_lineChoices.FirstMorphemeIndex;
			var last = m_lineChoices.LastMorphemeIndex;
			IMoForm mf = null;
			if (wmb != null)
			{
				mf = wmb.MorphRA;
			}
			if (vwenv is CollectorEnv && mf != null)
			{
				// Collectors are given an extra initial chance to 'collect' the morph type, if any.
				vwenv.AddObjProp(WfiMorphBundleTags.kflidMorph, this, kfragMorphType);

			}
			for (var i = first; i <= last; i++)
			{
				var spec = m_lineChoices[i];
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
							var ws = 0;
							if (wmb.MorphRA != null)
							{
								Debug.Assert(spec.StringFlid == MoFormTags.kflidForm);
								ws = GetRealWsOrBestWsForContext(wmb.MorphRA.Hvo, spec);
							}
							// If no morph, use the form of the morph bundle (and the entry is of
							// course missing)
							if (ws == 0)
							{
								ws = WritingSystemServices.ActualWs(m_cache, spec.WritingSystem, wmb.Hvo, WfiMorphBundleTags.kflidForm);
							}
							vwenv.AddStringAltMember(WfiMorphBundleTags.kflidForm, ws, this);
						}
						else
						{
							// Got a morph, show it.
							vwenv.AddObjProp(WfiMorphBundleTags.kflidMorph, this, kfragMorphFormChoices + i);
							// And the LexEntry line.
						}
						break;
					case InterlinLineChoices.kflidLexEntries:
						if (mf == null)
						{
							if (hvo != 0)
							{
								vwenv.NoteDependency(new int[] { hvo }, new int[] { WfiMorphBundleTags.kflidMorph }, 1);
							}
							vwenv.AddString(m_tssMissingEntry);
						}
						else
						{
							var ws = GetRealWsOrBestWsForContext(mf.Hvo, spec);
							if (ws == 0)
							{
								ws = spec.WritingSystem;
							}

							var vcEntry = new LexEntryVc(m_cache)
							{
								WritingSystemCode = ws
							};
							vwenv.AddObj(hvo, vcEntry, LexEntryVc.kfragEntryAndVariant);
						}
						break;
					case InterlinLineChoices.kflidLexGloss:
						var flid = 0;
						if (wmb != null)
						{
							vwenv.NoteDependency(new[] { wmb.Hvo }, new[] { WfiMorphBundleTags.kflidMorph }, 1);
							vwenv.NoteDependency(new[] { wmb.Hvo }, new[] { WfiMorphBundleTags.kflidInflType }, 1);
							vwenv.NoteDependency(new[] { hvo }, new[] { WfiMorphBundleTags.kflidSense }, 1);
							if (wmb.SenseRA == null)
							{
								if (ShowDefaultSense && wmb.DefaultSense != null && UsingGuess)
								{
									flid = wmb.Cache.MetaDataCacheAccessor.GetFieldId2(WfiMorphBundleTags.kClassId, "DefaultSense", false);
								}
							}
							else
							{
								flid = WfiMorphBundleTags.kflidSense;
								if (wmb.MorphRA != null && DisplayLexGlossWithInflType(vwenv, wmb.MorphRA.Owner as ILexEntry, wmb.SenseRA, spec, wmb.InflTypeRA))
								{
									break;
								}

							}
						}

						if (flid == 0)
						{
							vwenv.AddString(m_tssMissingSense);
						}
						else
						{
							vwenv.AddObjProp(flid, this, kfragLineChoices + i);
						}
						break;

					case InterlinLineChoices.kflidLexPos:
						// LexPOS line:
						var hvoMsa = 0;
						if (wmb?.MsaRA != null)
						{
							hvoMsa = wmb.MsaRA.Hvo;
						}
						if (hvoMsa == 0)
						{
							if (hvo != 0)
							{
								vwenv.NoteDependency(new[] { hvo }, new[] { WfiMorphBundleTags.kflidMsa }, 1);
							}
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
							vwenv.AddObjProp(WfiMorphBundleTags.kflidMsa, m_msaVc, (int)VcFrags.kfragInterlinearAbbr);
						}
						break;
				}
			}
			vwenv.CloseInnerPile();
		}

		internal static bool TryGetLexGlossWithInflTypeTss(ILexEntry possibleVariant, ILexSense sense, InterlinLineSpec spec, InterlinLineChoices lineChoices, int vernWsContext, ILexEntryInflType inflType, out ITsString result)
		{
			using (var vcLexGlossFrag = new InterlinVc(possibleVariant.Cache))
			{
				vcLexGlossFrag.LineChoices = lineChoices;
				vcLexGlossFrag.PreferredVernWs = vernWsContext;

				result = null;
				var collector = new TsStringCollectorEnv(null, vcLexGlossFrag.Cache.MainCacheAccessor, possibleVariant.Hvo)
				{
					RequestAppendSpaceForFirstWordInNewParagraph = false
				};
				if (vcLexGlossFrag.DisplayLexGlossWithInflType(collector, possibleVariant, sense, spec, inflType))
				{
					result = collector.Result;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// NOTE: this routine is ignorant of calling context, so caller must provide NoteDependency to the possibleVariant and the sense
		/// (e.g. vwenv.NoteDependency(new[] { wfiMorphBundle.Hvo }, new[] { WfiMorphBundleTags.kflidSense }, 1);
		///  vwenv.NoteDependency(new[] { wfiMorphBundle.Hvo }, new[] { WfiMorphBundleTags.kflidInflType }, 1);
		/// </summary>
		internal bool DisplayLexGlossWithInflType(IVwEnv vwenv, ILexEntry possibleVariant, ILexSense sense, InterlinLineSpec spec, ILexEntryInflType inflType)
		{
			var iLineChoice = m_lineChoices.IndexOf(spec);
			ILexEntryRef ler;
			if (!possibleVariant.IsVariantOfSenseOrOwnerEntry(sense, out ler))
			{
				return false;
			}
			var wsPreferred = GetRealWsOrBestWsForContext(sense.Hvo, spec);
			var wsGloss = Cache.ServiceLocator.WritingSystemManager.Get(wsPreferred);
			var wsUser = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
			var testGloss = sense.Gloss.get_String(wsPreferred);
			// don't bother adding anything for an empty gloss.
			if (testGloss.Text == null || testGloss.Text.Length < 0)
			{
				return false;
			}
			vwenv.OpenParagraph();
			// see if we have an irregularly inflected form type reference
			var leitFirst = ler.VariantEntryTypesRS.FirstOrDefault(let => @let.ClassID == LexEntryInflTypeTags.kClassId);

			// add any GlossPrepend info
			if (leitFirst != null)
			{
				vwenv.OpenInnerPile();
				// TODO: add dependency to VariantType GlossPrepend/Append names
				vwenv.NoteDependency(new[] { ler.Hvo }, new[] { LexEntryRefTags.kflidVariantEntryTypes }, 1);
				vwenv.OpenParagraph();
				ITsString tssPrepend = null;
				if (inflType != null)
				{
					tssPrepend = MorphServices.AddTssGlossAffix(null, inflType.GlossPrepend, wsGloss, wsUser);
				}
				else
				{
					ITsIncStrBldr sbPrepend;
					ITsIncStrBldr sbAppend;
					JoinGlossAffixesOfInflVariantTypes(ler, wsPreferred, out sbPrepend, out sbAppend);
					if (sbPrepend.Text != null)
					{
						tssPrepend = sbPrepend.GetString();
					}
				}

				if (tssPrepend != null)
				{
					vwenv.AddString(tssPrepend);
				}
				vwenv.CloseParagraph();
				vwenv.CloseInnerPile();
			}
			// add gloss of main entry or sense
			{
				vwenv.OpenInnerPile();
				// NOTE: remember to NoteDependency from OuterObject
				vwenv.AddObj(sense.Hvo, this, kfragLineChoices + iLineChoice);
				vwenv.CloseInnerPile();
			}
			// now add variant type info
			if (leitFirst != null)
			{
				vwenv.OpenInnerPile();
				// TODO: add dependency to VariantType GlossPrepend/Append names
				vwenv.NoteDependency(new[] { ler.Hvo }, new[] { LexEntryRefTags.kflidVariantEntryTypes }, 1);
				vwenv.OpenParagraph();
				ITsString tssAppend = null;
				if (inflType != null)
				{
					tssAppend = MorphServices.AddTssGlossAffix(null, inflType.GlossAppend, wsGloss, wsUser);
				}
				else
				{
					ITsIncStrBldr sbPrepend;
					ITsIncStrBldr sbAppend;
					JoinGlossAffixesOfInflVariantTypes(ler, wsPreferred, out sbPrepend, out sbAppend);
					if (sbAppend.Text != null)
					{
						tssAppend = sbAppend.GetString();
					}
				}
				{
					// Use AddProp/DisplayVariant to store GlossAppend with m_tssPendingGlossAffix
					// this allows InterlinearExporter to know to export a glsAppend item
					try
					{
						m_tssPendingGlossAffix = tssAppend ?? m_tssMissingGlossAppend;
						vwenv.AddProp(ktagGlossAppend, this, 0);
					}
					finally
					{
						m_tssPendingGlossAffix = null;
					}
				}
				vwenv.CloseParagraph();
				vwenv.CloseInnerPile();
			}
			vwenv.CloseParagraph();
			return true;
		}

		/// <summary>
		/// Add the pile of labels used to identify the lines in interlinear text.
		/// </summary>
		public void AddLabelPile(IVwEnv vwenv, LcmCache cache, bool fWantMultipleSenseGloss, bool fShowMorphemes)
		{
			CheckDisposed();

			var wsUI = cache.DefaultUserWs;
			var spaceStr = TsStringUtils.MakeString(" ", wsUI);
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 10000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint, 5000); // default spacing is fine for all embedded paragraphs.
			vwenv.OpenInnerPile();
			for (var i = 0; i < m_lineChoices.Count; i++)
			{
				var spec = m_lineChoices[i];
				if (!spec.WordLevel)
				{
					break;
				}
				SetColor(vwenv, LabelRGBFor(spec));
				var tss = MakeUiElementString(m_lineChoices.LabelFor(spec.Flid), wsUI, null);
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
			bldr.SetIntPropValues(0, bldr.Length, (int) FwTextPropType.ktptBulNumStartAt, (int) FwTextPropVar.ktpvDefault, i);
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
			{
				vwenv.AddObjProp(MoFormTags.kflidMorphType, this, kfragPrefix);
			}
			vwenv.AddStringAltMember(MoFormTags.kflidForm, ws, this);
			if (morphType != null)
			{
				vwenv.AddObjProp(MoFormTags.kflidMorphType, this, kfragPostfix);
			}
			vwenv.CloseParagraph();
		}

		/// <summary>
		/// Implementation of displaying a word bundle as Method Object
		/// </summary>
		private sealed class DisplayWordBundleMethod
		{
			int m_hvoWordform;
			int m_hvoWfiAnalysis;
			int m_hvoDefault;
			ICmObject m_defaultObj;
			readonly IVwEnv m_vwenv;
			readonly AnalysisOccurrence m_analysisOccurrence;
			readonly int m_hvoWordBundleAnalysis;
			readonly InterlinVc m_this;
			readonly LcmCache m_cache;
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
					m_this.CurrentLine = i;
					var spec = m_choices[i];
					if (!spec.WordLevel)
					{
						break;
					}
					if (spec.MorphemeLevel)
					{
						DisplayMorphemes();
						while (i < m_choices.Count && m_choices[i].MorphemeLevel)
						{
							i++;
						}
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
				m_this.CurrentLine = 0;
			}

			/// <summary>
			/// If we are displaying the baseline, and should display a substitute string rather than
			/// the requested WS of the wordform, return the substitute string. Otherwise return null.
			/// </summary>
			private ITsString GetRealForm(int ws, int choiceIndex)
			{
				if (choiceIndex != 0)
				{
					return null; // only ever correct the baselin
				}
				if (ws != m_this.m_wsVernForDisplay)
				{
					return null; // only ever correct for the default vernacular WS.
				}
				return m_analysisOccurrence != null ? m_analysisOccurrence.BaselineText : null;
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
							var isHumanGuess = m_this.Decorator.get_IntProp(m_hvoDefault, InterlinViewDataCache.OpinionAgentFlid) !=
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
					if (m_hvoDefault != m_hvoWordBundleAnalysis)
					{
						// Real analysis isn't what we're displaying, so morph breakdown
						// is a guess. Is it a human-approved guess?
						var isHumanGuess = m_this.Decorator.get_IntProp(m_hvoDefault, InterlinViewDataCache.OpinionAgentFlid) !=
																		(int)AnalysisGuessServices.OpinionAgent.Parser;
						m_this.SetGuessing(m_vwenv, isHumanGuess ? ApprovedGuessColor : MachineGuessColor);
					}
					var wa = (IWfiAnalysis) m_defaultObj;
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
						{
							wsActual = GetRealWs(m_cache, m_hvoWordBundleAnalysis, spec, m_this.m_wsAnalysis);
						}
						// We're displaying properties of the current object, can do
						// straightforwardly
						m_this.FormatGloss(m_vwenv, wsActual);
						m_vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmForceCheck);
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
						var isHumanApproved = m_this.Decorator.get_IntProp(m_hvoDefault, InterlinViewDataCache.OpinionAgentFlid)
																			!= (int)AnalysisGuessServices.OpinionAgent.Parser;

						m_this.SetGuessing(m_vwenv, isHumanApproved ? ApprovedGuessColor : MachineGuessColor);
					}
					m_this.AddAnalysisPos(m_vwenv, m_hvoDefault, m_hvoWordBundleAnalysis, choiceIndex);
					break;
				case WfiGlossTags.kClassId:
					m_hvoWfiAnalysis = m_defaultObj.Owner.Hvo;
					if (m_hvoWordBundleAnalysis == m_hvoWordform) // then our analysis is a guess
					{
						m_this.SetGuessing(m_vwenv);
					}
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
					var wa = m_coRepository.GetObject(hvo) as IWfiAnalysis;
					var i = 0;
					foreach (var gloss in wa.MeaningsOC)
					{
						if (i != 0)
						{
							vwenv.AddString(CommaSpaceString);
						}
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
			for (var ispec = m_lineChoices.FirstFreeformIndex; ispec < m_lineChoices.Count; ispec += m_lineChoices.AdjacentWssAtIndex(ispec, hvoSeg).Length)
			{
				var flid = m_lineChoices[ispec].Flid;
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
						if (m_cache.GetManagedMetaDataCache().IsCustom(flid))
						{
							AddCustomFreeFormComment(vwenv, hvoSeg, ispec);
						}
						break; // unknown type, ignore it.

				}
			}
		}

		protected virtual void AddCustomFreeFormComment(IVwEnv vwenv, int hvoSeg, int lineChoiceIndex)
		{
			var wssAnalysis = m_lineChoices.AdjacentWssAtIndex(lineChoiceIndex, hvoSeg);
			if (wssAnalysis.Length == 0)
			{
				return;
			}

			var exporter = vwenv as InterlinearExporter;
			if (exporter != null)
			{
				exporter.FreeAnnotationType = "custom";
			}
			vwenv.OpenDiv();
			SetParaDirectionAndAlignment(vwenv, wssAnalysis[0]);
			vwenv.OpenMappedPara();
			var customCommentFlid = m_lineChoices[lineChoiceIndex].Flid;
			var label = m_cache.MetaDataCacheAccessor.GetFieldLabel(customCommentFlid) + " ";
			SetNoteLabelProps(vwenv);
			// REVIEW: Should we set the label to a special color as well?
			var tssLabel = MakeUiElementString(label, m_cache.DefaultUserWs, propsBldr => propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn));
			var labelBldr = tssLabel.GetBldr();
			AddLineIndexProperty(labelBldr, lineChoiceIndex);
			tssLabel = labelBldr.GetString();
			if (wssAnalysis.Length > 1)
			{
				int labelWidth; // unused
				int labelHeight; // unused
				vwenv.get_StringWidth(tssLabel, null, out labelWidth, out labelHeight);
			}
			if (IsWsRtl(wssAnalysis[0]) != m_fRtl)
			{
				var bldr = tssLabel.GetBldr();
				bldr.Replace(bldr.Length - 1, bldr.Length, null, null);
				var tssLabelNoSpace = bldr.GetString();
				// (First) analysis language is upstream; insert label at end.
				AddTssDirForWs(vwenv, wssAnalysis[0]);
				vwenv.AddStringProp(customCommentFlid, this);
				AddTssDirForWs(vwenv, wssAnalysis[0]);
				if (wssAnalysis.Length != 1)
				{
					// Insert WS label for first line
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					AddTssDirForVernWs(vwenv);
					SetNoteLabelProps(vwenv);
					vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[0]));
				}
				AddTssDirForVernWs(vwenv);
				vwenv.AddString(m_tssSpace);
				AddTssDirForVernWs(vwenv);
				vwenv.AddString(tssLabelNoSpace);
				AddTssDirForVernWs(vwenv);
			}
			else
			{
				AddTssDirForVernWs(vwenv);
				vwenv.AddString(tssLabel);
				AddTssDirForVernWs(vwenv);
				if (wssAnalysis.Length == 1)
				{
					AddTssDirForWs(vwenv, wssAnalysis[0]);
					vwenv.AddStringProp(customCommentFlid, this);
				}
				else
				{
					SetNoteLabelProps(vwenv);
					vwenv.AddString(WsListManager.WsLabel(m_cache, wssAnalysis[0]));
					AddTssDirForVernWs(vwenv);
					vwenv.AddString(m_tssSpace);
					// label width unfortunately does not include trailing space.
					AddTssDirForVernWs(vwenv);
					AddTssDirForWs(vwenv, wssAnalysis[0]);
					//AddFreeformComment(vwenv, hvoSeg, wssAnalysis[0], flid);
					vwenv.AddStringProp(customCommentFlid, this);
				}
			}

			vwenv.CloseParagraph();
			vwenv.CloseDiv();
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

			switch (tag)
			{
				case SimpleRootSite.kTagUserPrompt:
					// In this case, frag is the writing system we really want the user to type.
					// We put a zero-width space in that WS at the start of the string since that is the
					// WS the user will end up typing in.
					var bldr = TsStringUtils.MakeString(ITextStrings.ksEmptyFreeTransPrompt, m_cache.DefaultUserWs).GetBldr();
					bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
					bldr.Replace(0, 0, "\u200B", null);
					// This dummy property should always be set on a user prompt. It allows certain formatting commands to be
					// handled specially.
					bldr.SetIntPropValues(0, bldr.Length, SimpleRootSite.ktptUserPrompt, (int)FwTextPropVar.ktpvDefault, 1);
					bldr.SetIntPropValues(0, 1, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, frag);
					return bldr.GetString();
				case ktagGlossAppend:
					// not really a variant, per se. rather a kludge so InterlinearExport will export glsAppend item.
					return m_tssPendingGlossAffix;
			}

			switch (frag)
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

		// A small sample indicates that each character of input results in about 20 pixels of width in an interlinear display.
		// May want to adjust down for views without morphology.
		private const int kPixelsPerChar = 20;
		private const int kPixelsForRowLabels = 30; // very rough; may want to omit for views without them.
		private const int kPixelsPerLine = 16;

		/// <summary>
		/// Estimate the height of things we display lazily.
		/// </summary>
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
					return EstimateStTextHeight(section.HeadingOA, dxAvailWidth) + EstimateStTextHeight(section.ContentOA, dxAvailWidth);
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
			var width = length*kPixelsPerChar + kPixelsForRowLabels;
			var rows = dxAvailWidth/width + 1;
			return rows*m_lineChoices.Count*kPixelsPerLine;
		}

		/// <summary>
		/// Load data for a group of segments
		/// </summary>
		internal virtual void LoadDataForSegments(int[] rghvo, int hvoPara)
		{
			var segmentRepository = m_cache.ServiceLocator.GetInstance<ISegmentRepository>();
			EnsureLoader();
			foreach (var hvoSeg in rghvo)
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
				if (tag == StTxtParaTags.kflidSegments)
				{
					LoadDataForSegments(rghvo, hvoParent);
				}

				if (tag != StTextTags.kflidParagraphs)
				{
					return;
				}
				for (var ihvo = 0; ihvo < chvo; ihvo++)
				{
					LoadParaData(rghvo[ihvo]);
				}
			}
			catch (Exception)
			{
			}
		}

		private void LoadParaData(int hvoPara)
		{
			if (hvoPara == 0)
			{
				return;
			}
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
			{
				m_loader = CreateParaLoader();
			}
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
		internal static int GetAnnDefnId(LcmCache cache, string guid, ref int cachedVal)
		{
			//  and cn.Flid = 7001
			return GetAnnDefnId(cache, new Guid(guid), ref cachedVal);
		}

		internal static int GetAnnDefnId(LcmCache cache, Guid guid, ref int cachedVal)
		{
			if (cachedVal == 0)
			{
				cachedVal = GetAnnDefnId(cache, guid).Hvo;
			}
			return cachedVal;

		}

		internal static ICmAnnotationDefn GetAnnDefnId(LcmCache cache, Guid guid)
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
		protected void AddAnalysisPos(IVwEnv vwenv, int hvoAnalysis, int hvoCurrent, int choiceIndex)
		{
			var wa = m_coRepository.GetObject(hvoAnalysis) as IWfiAnalysis;
			var hvoPos = wa.CategoryRA?.Hvo ?? 0;
			SetColor(vwenv, LabelRGBFor(choiceIndex));
			if (hvoPos == 0)
			{
				vwenv.OpenParagraph();
				vwenv.NoteDependency(new int[] {hvoAnalysis}, new[] {WfiAnalysisTags.kflidCategory}, 1);
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
				{
					vwenv.AddObjProp(WfiAnalysisTags.kflidCategory, this, kfragLineChoices + choiceIndex);
				}
				else
				{
					vwenv.AddObj(hvoAnalysis, this, kfragAnalysisCategoryChoices + choiceIndex); // causes recursive call with right hvoCurrent
				}
			}
		}

		/// <summary>
		/// Format the gloss line for the specified ws in an interlinear text
		/// </summary>
		protected virtual void FormatGloss(IVwEnv vwenv, int ws)
		{
			SetColor(vwenv, LabelRGBFor(LineChoices.IndexOf(InterlinLineChoices.kflidWordGloss, ws)));
		}

		/// <summary>
		/// Display the specified object (from an ORC embedded in a string).
		/// Don't display any embedded objects in interlinear text.
		/// </summary>
		public override void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
		}
	}
}