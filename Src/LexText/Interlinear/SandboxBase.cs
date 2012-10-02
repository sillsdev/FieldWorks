//#define TraceMouseCalls		// uncomment this line to trace mouse messages
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using XCore;
using System.Reflection;

namespace SIL.FieldWorks.IText
{
	#region SandboxBase class

	public partial class SandboxBase : SimpleRootSite
	{
		#region Model

		// In some ways it would be nice for these to be enumerations, but it is too infuriating
		// to have to keep casting them to ints. They are quite arbitrary constants; I have
		// used largish numbers so they can easily be recognized when found somewhere in the
		// debugger.

		// An SbWord corresponds to a WfiWordform in the real model, though some of its
		// properties store information from WfiAnalysis and WfiGloss as well.
		internal const int kclsidSbWord = 6901;
		// An SbMorph corresponds to a bundle of properties that are aligned with the morphs
		// of a WfiAnalysis in the real model. The information comes from the WfiMorphBundles
		// of the WfiAnalysis.
		internal const int kclsidSbMorph = 6902;
		// An SbNamedObject may be a LexEntry, LexSense, various kinds of CmPossibility...
		// it is used anywhere that we need a reference to an object, and all we will display
		// of it is its name.
		// It was a tough call to know whether to make these properties refer to a
		// CmNamedObject, or whether to just give the owning object a string property to hold
		// the name.  The advantage of this approach is that we know unambiguously when
		// something is missing (object prop is 0), and also, we can more readily record
		// correspondence between the dummy object and the real one which the name stands for.
		internal const int kclsidSbNamedObj = 6903;

		// String, the surface form of the word, the current vernacular ws alternative of
		// the WfiWordform.Form; line 1.
		internal const int ktagSbWordForm = 6901001;
		// owning obj seq, the morpheme bundles, lines 2-5
		public const int ktagSbWordMorphs = 6902002;
		// Dummy tag used if there are no morphs.
		internal const int ktagMissingMorphs = 6902003;
		// The Form property of the Morph object (SbNamedObj), line 2. (Real obj is an MoForm.)
		internal const int ktagSbMorphForm = 6902003;
		// The 'Text' property of the SbMorph, taken from the real MoForm if any, otherwise
		// from the real WfiMorphBundle
		//internal const int ktagSbMorphText = 6902021;
		// The LexEntry property of the Morph object, an SbNamedObj, line 3.
		internal const int ktagSbMorphEntry = 6902004;
		// The Name property of the NamedObj object, a string, used on several lines.
		internal const int ktagSbNamedObjName = 6903005;
		// Dummy tag used if the morph has no LexEntry
		internal const int ktagMissingEntry = 6902006;
		// The Gloss property of the Morph object, an SbNamedObj, line 4. (Real obj is a
		// LexSense.)
		internal const int ktagSbMorphGloss = 6902007;
		// Dummy tag used if the morph has no Gloss.
		internal const int ktagMissingMorphGloss = 6902008;
		// The Pos property of the Morph object, an SbNamedObj, line 5. (Real obj is an MSA.)
		internal const int ktagSbMorphPos = 6902009;
		// Dummy tag used if the morph has no Pos.
		internal const int ktagMissingMorphPos = 6902010;
		// The POS of the word, an SbNameObj, line 7. (Real object is a PartOfSpeech.)
		protected internal const int ktagSbWordPos = 6901012;
		// Dummy tag used if the word has no Pos.
		internal const int ktagMissingWordPos = 6901013;
		// Dummy tag used to own SbNamedObjects we only want to refer to.
		internal const int ktagSbWordDummy = 6901014;
		// And this one if the word gloss is a guess.
		internal const int ktagSbWordGlossGuess = 6901020;
		// The word gloss.
		internal protected const int ktagSbWordGloss = 6902021;

		// Preceding punctuation for a morpheme.
		internal const int ktagSbMorphPrefix = 6902015;
		// Trailing punctuation for a morpheme.
		internal const int ktagSbMorphPostfix = 6902016;
		//// This is the hvo in the REAL cache of the MoMorphType that we should link
		//// to if we have to create a new MoMorph. It isn't currently used.
		//internal const int ktagSbMorphRealType = 6902017;
		//// This is the clsid we should use to make a real MoMorph subclass if we
		//// need to.
		//internal const int ktagSbMorphClsid = 6902018;
		// This is true if the named object is a guess.
		internal const int ktagSbNamedObjGuess = 6903019;


		// This group identify the pull-down icons. They must be the only tags in the range
		// 6905000-6905999.
		internal const int ktagMinIcon = 6905000;
		internal const int ktagMorphFormIcon = 6905021;
		internal const int ktagMorphEntryIcon = 6905022;
		// The gloss of the word, a multi string (line 6):
		internal protected const int ktagWordGlossIcon = 6905023;
		internal protected const int ktagWordPosIcon = 6905024;
		internal const int ktagAnalysisIcon = 6905025; // not yet used.
		internal const int ktagWordLinkIcon = 6905026;
		internal const int ktagLimIcon = 6906000;

		#endregion Model

		#region events

		public event FwSelectionChangedEventHandler SelectionChangedEvent;
		internal event SandboxChangedEventHandler SandboxChangedEvent;

		#endregion events

		#region Constants

		// In an in-memory cache, we can 'create' a totally fake object by just inventing
		// a number.
		protected const int kSbWord = 10000007;

		#endregion Constants

		#region Data members

		private int m_hvoLastSelEntry; // HVO in real cache of last selected lex entry.
		protected CachePair m_caches = new CachePair();
		protected InterlinLineChoices m_choices; // Keeps track of which lines to show.

		// This object monitors property changes in the sandbox, primarily for edits to the morpheme breakdown.
		// It can also be used to implement some problem deletions.
		private MorphManager m_morphManager;

		// The Wfi{Wordform, Analysis, [??MorphBundle,] Gloss} object in the main cache
		// that is initially the original analysis we're working on and possibly replacing.
		// Choosing an analysis on the Words line may change it to some other analysis (or gloss, etc).
		// Choosing a different case form of the word (in the morphemes line) may change it to
		// a Wordform for that case form, or zero if there is no Wordform yet for that case form.
		protected int m_hvoAnalysis;
		// This is used to store the initial state of our sandbox, so we can revert to it in case of an undo action.
		protected int m_hvoInitialAnalysis = 0;
		private IVwStylesheet m_stylesheet;
		// The WfiWordform that we are currently displaying. Originally the wordform related to the
		// the original m_hvoAnalysis. May become zero, if the user chooses an alternate case form
		// that currently does not exist as a WfiWordform.
		private int m_hvoWordform;
		// The original value of m_hvoWordform, to which we return if the user chooses
		// 'Use default analysis' in the line-one chooser.
		private int m_hvoWordformOriginal;
		// The text that appears in the word line, the original casing from the paragraph.
		protected ITsString m_rawWordform;
		// The annotation context for the sandbox.
		protected int m_hvoAnnotation = 0;
		// This flag controls behavior that depends on whether the word being analyzed should be treated
		// as at the start of a sentence. Currently this affects the behavior for words with initial
		// capitalization only.
		private bool m_fTreatAsSentenceInitial = true;
		// Indicates the case status of the wordform.
		private StringCaseStatus m_case;
		// If m_hvoWordform is set to zero, this should be set to the actual text that should be
		// assigned to the new Wordform that will be created if GetRealAnalysis is called.
		private ITsString m_tssWordform;
		// The original Gloss we started with.
		private int m_hvoWordGloss;

		private bool m_fSuppressShowCombo = false; // set temporarily to prevent SelectionChanged displaying combo.
		private bool m_fShowAnalysisCombo = true; // false to hide Wordform-line combo (if no analyses).

		protected IComboHandler m_ComboHandler; // handles most kinds of combo box.
		private ChooseAnalysisHandler m_caHandler; // handles the one on the base line.
		protected SandboxVc m_vc;
		private Point m_LastMouseMovePos;
		// Rectangle containing last selection passed to ShowComboForSelection.
		private SIL.FieldWorks.Common.Utils.Rect m_locLastShowCombo;
		// True during calls to MakeCombo to suppress selected index changed effects.
		private bool m_fMakingCombo = false;
		// True when the user has started editing text in the combo. Blocks moving and
		// reinitializing the combo on mouse move, and ensures that we do something with what
		// the user has typed on OK and mousedown elsewhere.
		private bool m_fLockCombo = false;
		// True to lay out with infinite width, expecting to be fully visible.
		private bool m_fSizeToContent;
		// We'd like to just use the VC's copy, but it may not get made in time.
		private bool m_fShowMorphBundles = true;

		// Flag used to prevent mouse move events from entering CallMouseMoveDrag multiple
		// times before prior ones have exited.  Otherwise we get lines displayed multiple
		// times while scrolling during a selection.
		private bool m_fMouseInProcess = false;

		// This is set true by CallMouseMoveDrag if m_fNewSelection is true, and set false by
		// either CallMouseDown or CallMouseUp.  It controls whether CallMouseUp creates a
		// range selection, and also controls whether ShowComboForSelection actually creates
		// and shows the dropdown list.
		private bool m_fInMouseDrag = false;

		// This is set true by CallMouseDown after a new selection is created, and reset to
		// false by CallMouseUp.
		private bool m_fNewSelection = false;

		// This flag handles keeping the combo dropdown list open after you click on the arrow
		// but then proceed to drag before letting up on the mouse button.
		private bool m_fMouseDownActivatedCombo = false;

		// Normally we return a 'real' analysis in GetRealAnalysis() only if something in the
		// cache changed (the user made some edit). In certain cases (guessing, choosing a
		// different base form) we must return it even if nothing in the cache changed.
		//private bool m_fForceReturnNewAnalysis;
		private bool m_fGuessing = false;

		private bool m_fHaveUndone; // tells whether an Undo has occurred since Sandbox started on this word.

		private int m_rgbGuess = NoGuessColor;

		// During processing of a right-click menu item, this is the morpheme the user clicked on
		// (from the sandbox cache).
		int m_hvoRightClickMorph;
		// The analysis we guessed (may actually be a WfiGloss). If we didn't guess, it's the actual
		// analysis we started with.
		int m_hvoAnalysisGuess;

		#endregion Data members

		#region Properties

		/// <summary>
		///  Pass through to the VC.
		/// </summary>
		protected virtual bool IsMorphemeFormEditable
		{
			get { return true; }
		}

		/// <summary>
		/// When sandbox is visible, it should be the same as the InterlinDocChild m_hvoAnnotation.
		/// However, when the Sandbox is not visible the parent is setting/sizing things up for a new annotation.
		/// </summary>
		public virtual int HvoAnnotation
		{
			get { return m_hvoAnnotation; }
			set { m_hvoAnnotation = value; }
		}

		/// <summary>
		/// The writing system of the wordform in this analysis.
		/// </summary>
		int m_wsRawWordform = 0;
		internal protected virtual int RawWordformWs
		{
			get
			{
				if (m_wsRawWordform == 0)
				{
					m_wsRawWordform = StringUtils.GetWsAtOffset(RawWordform, 0);
				}
				return m_wsRawWordform;
			}

			set
			{
				m_wsRawWordform = value;
				// we want to establish a new RawWordform with the given ws.
				m_rawWordform = null;
			}
		}

		internal protected InterlinLineChoices InterlinLineChoices
		{
			get { return m_choices; }
		}

		/// <summary>
		/// Returns the count of Sandbox.WordGlossHvo the used (across records) in the Text area.
		/// (cf. LT-1428)
		/// </summary>
		protected virtual int WordGlossReferenceCount
		{
			get
			{
				return 0;
			}
		}

		protected CaseFunctions VernCaseFuncs(ITsString tss)
		{
			string locale = m_caches.MainCache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(
					StringUtils.GetWsAtOffset(tss, 0)).IcuLocale;
			return new CaseFunctions(locale);
		}

		protected bool ComboOnMouseHover
		{
			get
			{
				return false;
			}
		}

		protected bool IconsForAnalysisChoices
		{
			get
			{
				return true;
			}
		}

		protected bool IsIconSelected
		{
			get
			{
				return new TextSelInfo(RootBox).IsPicture;
			}
		}

		/// <summary>
		/// the given word is a phrase if it has any word breaking space characters
		/// </summary>
		/// <param name="word"></param>
		/// <returns></returns>
		static internal bool IsPhrase(string word)
		{
			return !String.IsNullOrEmpty(word) && word.IndexOfAny(Unicode.SpaceChars) != -1;
		}

		/// <summary>
		/// Return true if there is no analysis worth saving.
		/// </summary>
		protected bool IsAnalysisEmpty
		{
			get
			{
				ISilDataAccess sda = m_caches.DataAccess;
				// See if any alternate writing systems of word line are filled in.
				List<int> wordformWss = m_choices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidWord, 0);
				foreach (int wsId in wordformWss)
				{
					if (sda.get_MultiStringAlt(kSbWord, ktagSbWordForm, wsId).Length > 0)
						return false;
				}
				if (!IsMorphFormLineEmpty)
					return false;
				if (HasWordGloss())
					return false;
				// If we found nothing yet, it's non-empty if a word POS has been chosen.
				return !HasWordCat();
			}
		}

		/// <summary>
		/// LT-7807. Controls whether or not confirming the analyses will try to update the Lexicon.
		/// Only true for monomorphemic analyses.
		/// </summary>
		protected virtual bool ShouldAddWordGlossToLexicon
		{
			get
			{
				if (InterlinDoc == null)
					return false;
				return InterlinDoc.InModeForAddingGlossedWordsToLexicon && MorphCount == 1;
			}
		}

		/// <summary>
		/// True if user is in gloss tab.
		/// </summary>
		private bool IsInGlossMode()
		{
			if (InterlinDoc == null)
				return false;
			InterlinMaster master = InterlinDoc.GetMaster();
			if (master == null)
				return false;
			return master.InterlinearTab == InterlinMaster.TabPageSelection.Gloss;
		}

		internal bool HasWordCat()
		{
			ISilDataAccess sda = m_caches.DataAccess;
			return sda.get_ObjectProp(kSbWord, ktagSbWordPos) != 0;
		}

		internal bool HasWordGloss()
		{
			ISilDataAccess sda = m_caches.DataAccess;
			foreach (int wsId in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
			{
				// some analysis exists if any gloss multistring has content.
				if (sda.get_MultiStringAlt(kSbWord, ktagSbWordGloss, wsId).Length > 0)
					return true;
			}
			return false;
		}

		/// <summary>
		/// This is useful for detecting whether or not the user has deleted the entire morpheme line
		/// (cf. LT-1621).
		/// </summary>
		protected bool IsMorphFormLineEmpty
		{
			get
			{
				int cmorphs = MorphCount;
				if (cmorphs == 0)
				{
					//Debug.Assert(!ShowMorphBundles); // if showing should always have one.
					// JohnT: except when the user turned on morphology while the Sandbox was active...
					return true;
				}
				if (MorphCount == 1)
				{
					int hvoMorph = m_caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, 0);
					ITsString tssFullForm = GetFullMorphForm(hvoMorph);
					if (tssFullForm.Length == 0)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Return the count of morphemes.
		/// </summary>
		/// <returns></returns>
		public int MorphCount
		{
			get
			{
				CheckDisposed();

				return Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
			}
		}
		/// <summary>
		/// Return the list of msas as hvos
		/// </summary>
		public List<int> MsaHvoList
		{
			get
			{
				CheckDisposed();

				ISilDataAccess sda = m_caches.DataAccess;
				int chvo = MorphCount;
				ArrayPtr arrayPtr = MarshalEx.ArrayToNative(chvo, typeof(int));
				Caches.DataAccess.VecProp(kSbWord, ktagSbWordMorphs, chvo, out chvo, arrayPtr);
				int[] morphsHvoList = (int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int));
				List<int> msas = new List<int>(morphsHvoList.Length);
				for (int i = 0; i < morphsHvoList.Length; i++)
				{
					int hvo = (int)morphsHvoList.GetValue(i);
					if (hvo != 0)
					{
						int msaSecHvo = m_caches.DataAccess.get_ObjectProp(hvo, ktagSbMorphPos);
						int msaHvo = m_caches.RealHvo(msaSecHvo);
						msas.Add(msaHvo);
					}
				}
				return msas;
			}
		}

		internal CachePair Caches
		{
			get
			{
				CheckDisposed();
				return m_caches;
			}
		}

		public virtual ITsString RawWordform
		{
			get
			{
				CheckDisposed();
				if (m_rawWordform == null || m_rawWordform.Length == 0)
				{
					IWfiWordform wf = WfiWordform.CreateFromDBObject(Cache, m_hvoWordform);
					if (m_wsRawWordform != 0)
						m_rawWordform = wf.Form.GetAlternativeTss(m_wsRawWordform);
					else
						m_rawWordform = wf.Form.BestVernacularAlternative;
				}
				return m_rawWordform;
			}
			set
			{
				CheckDisposed();
				m_rawWordform = value;
				// we want RawWordformWs to be set to the ws of the new RawWordform.
				m_wsRawWordform = 0;
			}
		}

		/// <summary>
		/// This controls behavior that depends on whether the word being analyzed should be treated
		/// as at the start of a sentence. Currently this affects the behavior for words with initial
		/// capitalization only.
		/// </summary>
		public bool TreatAsSentenceInitial
		{
			get
			{
				CheckDisposed();
				return m_fTreatAsSentenceInitial;
			}
			set
			{
				CheckDisposed();
				m_fTreatAsSentenceInitial = value;
			}
		}

		/// <summary>
		/// (Background) color to use for guesses.
		/// </summary>
		protected int GuessColor
		{
			get { return m_rgbGuess; }
			set
			{
				m_rgbGuess = value;
				if (m_vc != null)
					m_vc.GuessColor = value;
			}
		}

		static protected int NoGuessColor
		{
			get { return (int)CmObjectUi.RGB(Color.LightGray); }
		}


		/// <summary>
		/// indicates whether the current state of the sandbox is using a guessed analysis.
		/// </summary>
		public bool UsingGuess
		{
			get
			{
				return m_fGuessing;
			}
		}

		/// <summary>
		/// Indicates the direction of flow of baseline that Sandbox is in.
		/// (Only available after MakeRoot)
		/// </summary>
		public bool RightToLeftWritingSystem
		{
			get
			{
				return m_vc != null && m_vc.RightToLeft;
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
				if (m_vc != null)
					m_vc.ShowMorphBundles = value;
			}
		}

		/// <summary>
		/// Finds the interlinDoc that this Sandbox is embedded in.
		/// </summary>
		internal virtual InterlinDocChild InterlinDoc
		{
			get
			{
				return null;
			}
		}

		internal int RootWordHvo
		{
			get
			{
				CheckDisposed();
				return kSbWord;
			}
		}

		/// <summary>
		/// True if the combo on the Wordform line is wanted (there are known analyses).
		/// </summary>
		protected bool ShowAnalysisCombo
		{
			get { return m_fShowAnalysisCombo; }
		}

		/// <summary>
		/// The index of the word we're editing among the context words we're showing.
		/// Currently this is also it's index in the list of root objects.
		/// </summary>
		internal int IndexOfCurrentItem
		{
			get
			{
				CheckDisposed();
				return 0;
			}
		}

		internal MorphManager MorphManager
		{
			get
			{
				CheckDisposed();
				return m_morphManager;
			}
		}

		public bool SizeToContent
		{
			get
			{
				CheckDisposed();
				return m_fSizeToContent;
			}
			set
			{
				CheckDisposed();

				m_fSizeToContent = value;
				// If we are changing the window size to match the content, we don't want to autoscroll.
				this.AutoScroll = !m_fSizeToContent;
			}
		}
		internal ChooseAnalysisHandler FirstLineHandler
		{
			get
			{
				CheckDisposed();
				return m_caHandler;
			}
			set
			{
				CheckDisposed();
				m_caHandler = value;
			}
		}

		/// <summary>
		/// Mainly used to set the main cache, when not initialized from the constructor.
		/// </summary>
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_caches.MainCache;
			}
			set
			{
				CheckDisposed();

				Debug.Assert(m_caches.MainCache == null);
				m_caches.MainCache = value;
				m_caches.CreateSecCache();
				if (m_morphManager != null)
				{
					// We really need to make sure the old one is disposed manually,
					// otherwise it will be getting PropChanged calls, even though it is dead.
					m_morphManager.Dispose();
					m_morphManager = null;
				}
				m_morphManager = new MorphManager(this); // after creating sec cache.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Undo menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayUndo(object commandObject, ref UIItemDisplayProperties display)
		{
			if (m_caches.DataAccess.IsDirty())
			{
				display.Enabled = true;
				display.Text = ITextStrings.ksUndoAllChangesHere;
				return true;
			}
			else
			{
				return false; // we don't want to handle the command.
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will undo the last changes done to the project.
		/// This function is executed when the user clicks the undo menu item.
		/// </summary>
		/// <param name="args">Unused</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		internal bool OnUndo(object args)
		{
			if (m_caches.DataAccess.IsDirty())
			{
				ResyncSandboxToDatabase();
				m_fHaveUndone = true;
				return true;
			}
			else
			{
				// Some deeper Undo is happening; we should allow the user to Redo it.
				m_fHaveUndone = false;
				// see if our parent can handle the undo.
				InterlinDocChild parent = InterlinDoc;
				if (parent == null)
					return false;
				return parent.OnUndo(args);
			}
		}

		/// <summary>
		/// Triggered to tell clients that the Sandbox has changed (e.g. been edited from
		/// its initial state) to help determine whether we should allow trying to save or
		/// undo the changes.
		///
		/// Currently triggered by MorphManager.PropChanged whenever a property changes on the cache
		/// </summary>
		internal void OnUpdateEdited()
		{
			bool fIsEdited = m_caches.DataAccess.IsDirty();
			if (fIsEdited)
				GuessColor = NoGuessColor;
			if (SandboxChangedEvent != null)
				SandboxChangedEvent(this, new SandboxChangedEventArgs(fIsEdited));
		}

		/// <summary>
		/// Resync the sandbox and reconstruct the rootbox to match the current state
		/// of the database.
		/// </summary>
		internal void ResyncSandboxToDatabase()
		{
			CheckDisposed();
			// hvoAnnotation should be a constant
			ReconstructForWordBundleAnalysis(m_hvoInitialAnalysis);
		}

		protected void ReconstructForWordBundleAnalysis(int hvoAnalysis)
		{
			m_fHaveUndone = false;
			HideCombos(); // Usually redundant, but MUST not have one around hooked to old data.
			LoadForWordBundleAnalysis(hvoAnalysis);
			if (m_rootb == null)
				MakeRoot();
			else
				m_rootb.Reconstruct();
		}

		internal void MarkAsInitialState()
		{
			m_hvoAnalysisGuess = m_hvoInitialAnalysis = m_hvoAnalysis;	// save our initial analysis.
			m_caches.DataAccess.ClearDirty(); // indicate we've loaded or saved.
			OnUpdateEdited();	// tell client we've updated the state of the sandbox.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Undo menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayRedo(object commandObject, ref UIItemDisplayProperties display)
		{
			// if the cache isn't dirty and we've done an Undo inside the annotation, we want
			// a special message saying we can't redo. If the cache IS dirty, the user has been
			// doing something since the Undo, so shouldn't expect Redo;
			if (m_caches.DataAccess.IsDirty() || m_fHaveUndone)
			{
				display.Enabled = false;
				if (m_fHaveUndone)
					display.Text = ITextStrings.ksCannotRedoChangesHere;
				// Otherwise just leave it a disabled 'Redo'; the user isn't expecting to be
				// able to redo anything since he's 'done' something most recently.
				return true;
			}
			else
				return false; // we don't want to handle the command.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Consistent with OnDisplayRedo.
		/// </summary>
		/// <param name="args">Unused</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnRedo(object args)
		{
			if (m_caches.DataAccess.IsDirty() || m_fHaveUndone)
			{
				return true; // We (didn't) do it.
			}
			else
			{
				// see if our parent can handle the redo.
				InterlinDocChild parent = InterlinDoc;
				if (parent == null)
					return false;
				return parent.OnRedo(args);
			}
		}

		/// <summary>
		/// Indicates the case of the Wordform we're dealing with.
		/// </summary>
		public StringCaseStatus CaseStatus
		{
			get
			{
				CheckDisposed();
				return m_case;
			}
		}

		/// <summary>
		/// The analysis object (in the real cache) that we started out looking at.
		/// </summary>
		public int Analysis
		{
			get
			{
				CheckDisposed();
				return m_hvoAnalysis;
			}
		}

		/// <summary>
		/// The main object that determines the form and contents of the Sandbox.
		/// </summary>
		protected internal virtual int RootObjHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoAnalysis;
			}
		}

		/// <summary>
		/// Used to save the appropriate form to use for the new Wordform
		/// that will be created if an alternate-case wordform that does not already exist is confirmed.
		/// Also used as the first menu item in the morphemes menu when m_hvoWordform is zero.
		/// </summary>
		internal ITsString FormOfWordform
		{
			get
			{
				CheckDisposed();
				return m_tssWordform;
			}
		}

		/// <summary>
		/// We're making a new word gloss, so forget the one we have stored as 'current'.
		/// </summary>
		internal int WordGlossHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoWordGloss;
			}
			set
			{
				CheckDisposed();
				m_hvoWordGloss = value;
			}
		}

		/// <summary>
		/// if the (anchor) selection is inside the display of a morpheme, return the index of that morpheme.
		/// Otherwise, return -1.
		/// </summary>
		protected int MorphIndex
		{
			get
			{
				TextSelInfo tsi = new TextSelInfo(RootBox);
				if (tsi.ContainingObjectTag(tsi.Levels(false) - 1) != ktagSbWordMorphs ||
					tsi.TagAnchor == ktagMorphFormIcon)	// don't count the morpheme dropdown icon.
					return -1;
				return tsi.ContainingObjectIndex(tsi.Levels(false) - 1);
			}
		}

		/// <summary>
		/// Return true if there is a selection that is at the start of the morpheme line for a particular morpheme
		/// (that is, it's at the start of the prefix of the morpheme, or at the start of the name of the morpheme's form
		///  AND it has no prefix)
		/// </summary>
		protected bool IsSelAtStartOfMorph
		{
			get
			{
				TextSelInfo tsi = new TextSelInfo(RootBox);
				if (tsi.IsRange || tsi.IchEnd != 0 || tsi.Selection == null)
					return false;
				if (tsi.TagAnchor == ktagSbMorphPrefix)
					return true;
				if (tsi.TagAnchor != ktagSbNamedObjName)
					return false;
				// only the first Morpheme line is currently displaying prefix/postfix.
				int currentLine = GetLineOfCurrentSelection();
				if (currentLine != -1 && m_choices.IsFirstOccurrenceOfFlid(currentLine))
				{
					return (tsi.ContainingObjectTag(1) == ktagSbMorphForm
						&& m_caches.DataAccess.get_StringProp(tsi.ContainingObject(1), ktagSbMorphPrefix).Length == 0);
				}
				else
				{
					return tsi.IchAnchor == 0;
				}
			}
		}

		/// <summary>
		/// Return true if there is a selection that is at the end of the morpheme line for a particular morpheme
		/// (that is, it's at the end of the postfix of the morpheme, or at the end of the name of the morpheme's form
		///  AND it has no postfix)
		/// </summary>
		protected bool IsSelAtEndOfMorph
		{
			get
			{
				TextSelInfo tsi = new TextSelInfo(RootBox);
				if (tsi.IsRange || tsi.IchEnd != tsi.AnchorLength || tsi.Selection == null)
					return false;
				if (tsi.TagAnchor == ktagSbMorphPostfix)
					return true;
				if (tsi.TagAnchor != ktagSbNamedObjName)
					return false;
				// only the first Morpheme line is currently displaying prefix/postfix.
				int currentLine = GetLineOfCurrentSelection();
				if (currentLine != -1 && m_choices.IsFirstOccurrenceOfFlid(currentLine))
				{
					return (tsi.ContainingObjectTag(1) == ktagSbMorphForm
						&& m_caches.DataAccess.get_StringProp(tsi.ContainingObject(1), ktagSbMorphPostfix).Length == 0);
				}
				else
				{
					return tsi.AnchorLength == tsi.IchEnd;
				}
			}
		}

		/// <summary>
		/// True if the selection is on the right edge of the morpheme.
		/// </summary>
		private bool IsSelAtRightOfMorph
		{
			get
			{
				if (m_vc.RightToLeft)
					return IsSelAtStartOfMorph;
				else
					return IsSelAtEndOfMorph;
			}
		}

		/// <summary>
		/// True if the selection is on the left edge of the morpheme.
		/// </summary>
		private bool IsSelAtLeftOfMorph
		{
			get
			{
				if (m_vc.RightToLeft)
					return IsSelAtEndOfMorph;
				else
					return IsSelAtStartOfMorph;
			}
		}

		protected bool IsWordPosIconSelected
		{
			get
			{
				TextSelInfo tsi = new TextSelInfo(RootBox);
				return tsi.IsPicture && tsi.TagAnchor == ktagWordPosIcon;
			}
		}

		#endregion Properties

		#region Construction, initiliazation & Disposal

		public SandboxBase()
		{
			InitializeComponent();
		}

		public SandboxBase(FdoCache cache, Mediator mediator, IVwStylesheet ss, InterlinLineChoices choices)
			: this()
		{
			// Override things from InitializeComponent()
			BackColor = Color.FromKnownColor(KnownColor.Control);

			// Setup member variables.
			m_caches.MainCache = cache;
			m_mediator = mediator;
			// We need to set this for various inherited things to work,
			// for example, automatically setting the correct keyboard.
			WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			m_caches.CreateSecCache();
			m_choices = choices;
			m_stylesheet = ss; // this is really redundant now it inherits a StyleSheet property.
			StyleSheet = ss;
			// setup morph manager
			if (m_morphManager != null)
			{
				// We really need to make sure the old one is disposed manually,
				// otherwise it will be getting PropChanged calls, even though it is dead.
				m_morphManager.Dispose();
				m_morphManager = null;
			}
			m_morphManager = new MorphManager(this); // after creating sec cache.
			DoSpellCheck = true;
			if (mediator != null && mediator.PropertyTable != null)
			{
				mediator.PropertyTable.SetProperty("FirstControlToHandleMessages", this, false, PropertyTable.SettingsGroup.LocalSettings);
				mediator.PropertyTable.SetPropertyPersistence("FirstControlToHandleMessages", false);
			}
		}

		public SandboxBase(FdoCache cache, Mediator mediator, IVwStylesheet ss, InterlinLineChoices choices, int hvoAnalysis)
			: this(cache, mediator, ss, choices)
		{
			// finish setup with the WordBundleAnalysis
			LoadForWordBundleAnalysis(hvoAnalysis);
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (m_morphManager != null)
				m_morphManager.ResetNotification(Visible);
		}

		// Cf. the SandboxBase.Designer.cs file for this method.
		//protected override void Dispose(bool disposing)

		#endregion Construction, initiliazation & Disposal

		#region Other methods

		/// <summary>
		/// Load the real data into the secondary cache, and establish this as the state we'd
		/// return to if we undo all changes in the analysis (i.e. does fClearDirty).
		/// </summary>
		/// <param name="fAdjustCase">If true, may adjust case of morpheme when
		/// proposing whole word as default morpheme breakdown.</param>
		/// <param name="fLookForDefaults">If true, will try to guess most likely analysis.</param>
		private void LoadRealDataIntoSec(bool fLookForDefaults, bool fAdjustCase)
		{
			LoadRealDataIntoSec(fLookForDefaults, fAdjustCase, true);
		}
		/// <summary>
		/// Load the real data into the secondary cache.
		/// </summary>
		/// <param name="fAdjustCase">If true, may adjust case of morpheme when
		/// proposing whole word as default morpheme breakdown.</param>
		/// <param name="fLookForDefaults">If true, will try to guess most likely analysis.</param>
		/// <param name="fClearDirty">if true, establishes the loaded cache state for the sandbox,
		/// so that subsequent changes can be undone or saved with respect to this initial state.</param>
		private void LoadRealDataIntoSec(bool fLookForDefaults, bool fAdjustCase, bool fClearDirty)
		{
			IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
			cda.ClearAllData();
			m_caches.ClearMaps();

			// If we don't have a real root object yet, we can't set anything up.
			if (m_hvoAnalysis == 0)
			{
				// This probably paranoid, but it's safe.
				Debug.WriteLine("loading Sandbox for missing analysis");
				m_hvoWordform = 0;
				m_hvoWordformOriginal = 0;
				m_case = StringCaseStatus.allLower;
				return;
			}

			m_fGuessing = LoadRealDataIntoSec1(ref m_hvoAnalysis, out m_hvoWordform,
				kSbWord, fLookForDefaults, fAdjustCase);
			Debug.Assert(m_hvoWordform != 0 || m_tssWordform != null);

			// At this point the only reason to force the current displayed analysis
			// to be returned instead of the original is if we're guessing.
			//m_fForceReturnNewAnalysis = fGuessing;

			// Treat initial state (including guessing) as something you can leave without saving.
			if (fClearDirty)
				MarkAsInitialState(); // Make sure it doesn't think any edits have happened, even if reusing from some other word.
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoAnalysisIn">either m_hvoAnalysis, m_hvoPrevAnal, or m_hvoNextAnal
		/// </param>
		/// <param name="hvoWordformRef">reference to either m_hvoWordform, m_hvoPrevWordform,
		/// or m_hvoNextWordform</param>
		/// <param name="hvoSbWord">either m_hvoSbWord, m_hvoPrevSbWordb, or m_hvoNextSbWord
		/// </param>
		/// <param name="fAdjustCase">If true, may adjust case of morpheme when
		/// proposing whole word as default morpheme breakdown.</param>
		/// <returns>true if any guessing is involved.</returns>
		private bool LoadRealDataIntoSec1(ref int hvoAnalysisRef, out int hvoWordformRef,
			int hvoSbWord, bool fLookForDefaults, bool fAdjustCase)
		{
			int wsAnalysis = m_caches.MainCache.DefaultAnalWs;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
			if (hvoAnalysisRef == 0)
			{
				hvoWordformRef = 0;
				// should we empty the cache of any stale data?
				return false;
			}
			m_hvoLastSelEntry = 0;	// forget last Lex Entry user selection. We're resync'ing everything.
			int hvoAnalysis = 0;
			m_hvoWordGloss = 0;
			int fGuessing = 0;  // Use 0 or 1, as we store it in an int dummy property.


			switch (m_caches.MainCache.GetClassOfObject(hvoAnalysisRef))
			{
				case WfiWordform.kclsidWfiWordform:
					hvoWordformRef = hvoAnalysisRef;
					break;
				case WfiAnalysis.kclsidWfiAnalysis:
					hvoAnalysis = hvoAnalysisRef;
					hvoWordformRef = m_caches.MainCache.GetOwnerOfObject(hvoAnalysis);
					break;
				case WfiGloss.kclsidWfiGloss:
					m_hvoWordGloss = hvoAnalysisRef;
					hvoAnalysis = m_caches.MainCache.GetOwnerOfObject(m_hvoWordGloss);
					hvoWordformRef = m_caches.MainCache.GetOwnerOfObject(hvoAnalysis);
					break;
				default:
					Debug.Assert(false, "analysis must be wordform, wfianalysis, or wfigloss");
					hvoWordformRef = 0;
					break;
			}
			RawWordform = null; // recompute based upon wordform.
			int wsVern = RawWordformWs;
			m_caches.Map(hvoSbWord, hvoWordformRef); // Review: any reason to map these?
			ISilDataAccess sdaMain = m_caches.MainCache.MainCacheAccessor;
			CopyStringsToSecondary(InterlinLineChoices.kflidWord, sdaMain, hvoWordformRef,
				(int)WfiWordform.WfiWordformTags.kflidForm, cda, hvoSbWord, ktagSbWordForm, tsf);
			CaseFunctions cf = VernCaseFuncs(RawWordform);
			m_case = cf.StringCase(RawWordform.Text);
			// empty it in case we're redoing after choose from combo.
			cda.CacheVecProp(hvoSbWord, ktagSbWordMorphs, new int[0], 0);
			if (hvoAnalysis == 0)
			{
				if (fLookForDefaults)
				{
					GetDefaults(hvoWordformRef, out hvoAnalysis, out m_hvoWordGloss, fAdjustCase);
					// Make sure the wordform ID is consistent with the analysis we located.
					if (hvoAnalysis != 0)
					{
						int hvoFixedWordform = m_caches.MainCache.GetOwnerOfObject(hvoAnalysis);
						if (hvoFixedWordform != hvoWordformRef)
						{
							hvoWordformRef = hvoFixedWordform;
							// Update the actual form.
							// Enhance: may NOT want to do this, when we get the baseline consistently
							// keeping original case.
							CopyStringsToSecondary(InterlinLineChoices.kflidWord, sdaMain, hvoWordformRef,
								(int)WfiWordform.WfiWordformTags.kflidForm, cda, hvoSbWord, ktagSbWordForm, tsf);
							hvoAnalysisRef = hvoFixedWordform;
						}
					}
					// Hide the analysis combo if there's no default analysis (which means there are
					// no options to list).
					m_fShowAnalysisCombo = (hvoAnalysis != 0);
					fGuessing = 1;
					// If we found a word gloss treat as human-approved.
					bool fHumanApproved = (m_hvoWordGloss != 0);
					if (!fHumanApproved)
					{
						// Human may have approved the analysis anyway.
						string sql = string.Format("select count(ag.id) " +
							"from CmAgentEvaluation_ ae " +
							"join CmAgent ag on ae.owner$ = ag.id and ae.target = {0} and ag.human = 1",
							hvoAnalysis);
						int nHumanApprovals;
						DbOps.ReadOneIntFromCommand(m_caches.MainCache, sql, null, out nHumanApprovals);
						fHumanApproved = (nHumanApprovals != 0);
					}
					this.GuessColor = fHumanApproved ? InterlinVc.ApprovedGuessColor : InterlinVc.MachineGuessColor;
				}
				else if (hvoWordformRef != 0)
				{
					// Need to check whether there are any options to list.
					m_fShowAnalysisCombo = m_caches.MainCache.GetVectorSize(hvoWordformRef,
						(int)WfiWordform.WfiWordformTags.kflidAnalyses) > 0;
				}
			}
			else
			{
				// If we got a definite analysis, at most we're guessing a gloss, which is always human-approved.
				this.GuessColor = InterlinVc.ApprovedGuessColor;
				m_fShowAnalysisCombo = true; // there's a real analysis!
			}
			m_hvoAnalysisGuess = hvoAnalysis;
			if (m_hvoWordGloss != 0)
				m_hvoAnalysisGuess = m_hvoWordGloss;

			// make the wordform corresponding to the baseline ws, match RawWordform
			m_caches.DataAccess.SetMultiStringAlt(kSbWord, ktagSbWordForm, this.RawWordformWs, RawWordform);
			// Set every alternative of the word gloss, whether or not we have one...this
			// ensures clearing it out if we once had something but do no longer.
			CopyStringsToSecondary(InterlinLineChoices.kflidWordGloss, sdaMain, m_hvoWordGloss,
				(int)WfiGloss.WfiGlossTags.kflidForm, cda, hvoSbWord, ktagSbWordGloss, tsf);
			cda.CacheIntProp(hvoSbWord, ktagSbWordGlossGuess, fGuessing);
			cda.CacheObjProp(hvoSbWord, ktagSbWordPos, 0); // default.
			if (hvoAnalysis != 0) // Might still be, if no default is available.
			{
				int hvoCategory = sdaMain.get_ObjectProp(hvoAnalysis,
					(int)WfiAnalysis.WfiAnalysisTags.kflidCategory);
				if (hvoCategory != 0)
				{
					int hvoWordPos = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvoCategory,
						(int)CmPossibility.CmPossibilityTags.kflidAbbreviation, hvoSbWord, sdaMain, cda, tsf);
					cda.CacheObjProp(hvoSbWord, ktagSbWordPos, hvoWordPos);
					cda.CacheIntProp(hvoWordPos, ktagSbNamedObjGuess, fGuessing);
				}
				int cmorphs = 0;
				if (this.ShowMorphBundles)
					cmorphs = sdaMain.get_VecSize(hvoAnalysis,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles);
				MoMorphTypeCollection morphtypes = new MoMorphTypeCollection(m_caches.MainCache);
				for (int imorph = 0; imorph < cmorphs; ++imorph)
				{
					// Get the real morpheme bundle.
					int hvoMb = sdaMain.get_VecItem(hvoAnalysis,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, imorph);
					// Create the corresponding SbMorph.
					int hvoMbSec = m_caches.DataAccess.MakeNewObject(kclsidSbMorph, hvoSbWord,
						ktagSbWordMorphs, imorph);
					m_caches.Map(hvoMbSec, hvoMb);

					// Get the real MoForm, if any.
					int hvoMorphReal = sdaMain.get_ObjectProp(hvoMb,
						(int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph);
					// Get the text we will display on the first line of the morpheme bundle.
					// Taken from the MoForm if any, otherwise the form of the MB.
					int hvoMorphForm;
					string sPrefix = null;
					string sPostfix = null;
					if (hvoMorphReal == 0)
					{
						// Create the secondary object corresponding to the MoForm. We create one
						// even though there isn't a real MoForm. It doesn't correspond to anything
						// in the real database.
						hvoMorphForm = m_caches.DataAccess.MakeNewObject(kclsidSbNamedObj, hvoMb,
							ktagSbMorphForm, -2); // -2 for atomic
						CopyStringsToSecondary(InterlinLineChoices.kflidMorphemes, sdaMain, hvoMb,
							(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm, cda, hvoMorphForm, ktagSbNamedObjName, tsf);
						// We will slightly adjust the form we display in the default vernacular WS.
						InterlinLineSpec specMorphemes = m_choices.GetPrimarySpec(InterlinLineChoices.kflidMorphemes);
						int wsForm = RawWordformWs;
						if (specMorphemes != null)
							wsForm = specMorphemes.GetActualWs(Cache, hvoMb, wsForm);
						ITsString tssForm = sdaMain.get_MultiStringAlt(hvoMb,
							(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm,
							wsForm);
						string realForm = tssForm.Text;
						// currently (unfortunately) Text returns 'null' from COM for empty strings.
						if (realForm == null)
							realForm = string.Empty;

						// if it's not an empty string, then we can find its form type, and separate the
						// morpheme markers into separate properties.
						if (realForm != string.Empty)
						{
							IMoMorphType mmt = null;
							try
							{
								int clsidForm;
								mmt = MoMorphType.FindMorphType(m_caches.MainCache, morphtypes,
									ref realForm, out clsidForm);
								sPrefix = mmt.Prefix;
								sPostfix = mmt.Postfix;
							}
							catch (Exception e)
							{
								MessageBox.Show(null, e.Message, ITextStrings.ksWarning,
									MessageBoxButtons.OK);
							}
						}
						tssForm = StringUtils.MakeTss(realForm, RawWordformWs);
						cda.CacheStringAlt(hvoMorphForm, ktagSbNamedObjName, wsVern, tssForm);
					}
					else
					{
						// Create the secondary object corresponding to the MoForm in the usual way from the form object.
						hvoMorphForm = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidMorphemes, hvoMorphReal,
							(int)MoForm.MoFormTags.kflidForm, hvoSbWord, sdaMain, cda, tsf);
						// Store the prefix and postfix markers from the MoMorphType object.
						int hvoMorphType = sdaMain.get_ObjectProp(hvoMorphReal,
							(int)MoForm.MoFormTags.kflidMorphType);
						if (hvoMorphType != 0)
						{
							sPrefix = sdaMain.get_UnicodeProp(hvoMorphType,
								(int)MoMorphType.MoMorphTypeTags.kflidPrefix);
							sPostfix = sdaMain.get_UnicodeProp(hvoMorphType,
								(int)MoMorphType.MoMorphTypeTags.kflidPostfix);
						}
					}
					if (sPrefix != null && sPrefix != "")
						cda.CacheStringProp(hvoMbSec, ktagSbMorphPrefix,
							tsf.MakeString(sPrefix, wsVern));
					if (sPostfix != null && sPostfix != "")
						cda.CacheStringProp(hvoMbSec, ktagSbMorphPostfix,
							tsf.MakeString(sPostfix, wsVern));

					// Link the SbMorph to its form object, noting if it is a guess.
					cda.CacheObjProp(hvoMbSec, ktagSbMorphForm, hvoMorphForm);
					cda.CacheIntProp(hvoMorphForm, ktagSbNamedObjGuess, fGuessing);

					// Get the real Sense that supplies the gloss, if any.
					int hvoSenseReal = sdaMain.get_ObjectProp(hvoMb,
						(int)WfiMorphBundle.WfiMorphBundleTags.kflidSense);
					if (hvoSenseReal == 0)
					{
						// Guess a default
						int virtFlid = BaseVirtualHandler.GetInstalledHandlerTag(m_caches.MainCache, "WfiMorphBundle", "DefaultSense");
						hvoSenseReal = sdaMain.get_ObjectProp(hvoMb, virtFlid);
						this.GuessColor = InterlinVc.MachineGuessColor;
					}
					if (hvoSenseReal != 0) // either all-the-way real, or default.
					{
						// Create the corresponding dummy.
						int hvoSense = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidLexGloss, hvoSenseReal,
							(int)LexSense.LexSenseTags.kflidGloss, hvoSbWord, sdaMain, cda, tsf);
						cda.CacheObjProp(hvoMbSec, ktagSbMorphGloss, hvoSense);
						cda.CacheIntProp(hvoSense, ktagSbNamedObjGuess, fGuessing);
					}

					// Get the MSA, if any.
					int hvoMsaReal = sdaMain.get_ObjectProp(hvoMb,
						(int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa);
					if (hvoMsaReal != 0)
					{
						MoMorphSynAnalysis msa = ((MoMorphSynAnalysis)CmObject.CreateFromDBObject(
							m_caches.MainCache, hvoMsaReal, false));
						int hvoPos = m_caches.FindOrCreateSec(hvoMsaReal,
							kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);

						// Enhance JohnT: we'd really rather be able to get an appropriate different name
						// for each ws, but not possible yet.
						// Enhancement RickM: now we can do this with InterlinAbbrTSS(ws)
						foreach (int ws in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexPos, true))
						{
							// Since ws maybe ksFirstAnal/ksFirstVern, we need to get what is actually
							// used in order to retrieve the data in Vc.Display().  See LT_7976.
							ITsString tssLexPos = msa.InterlinAbbrTSS(ws);
							int wsActual = StringUtils.GetWsAtOffset(tssLexPos, 0);
							cda.CacheStringAlt(hvoPos, ktagSbNamedObjName, wsActual, tssLexPos);
						}
						cda.CacheObjProp(hvoMbSec, ktagSbMorphPos, hvoPos);
						cda.CacheIntProp(hvoPos, ktagSbNamedObjGuess, fGuessing);
					}

					// If we have a form, we can get its owner and set the info for the Entry
					// line.
					// Enhance JohnT: attempt a guess if we have a form but no entry.
					if (hvoMorphReal != 0)
					{
						int hvoEntryReal = m_caches.MainCache.GetOwnerOfObject(hvoMorphReal);
						// We can assume the owner is a LexEntry as that is the only type of object
						// that can own MoForms. We don't actually create the LexEntry, to
						// improve performance. All the relevant data should already have
						// been loaded while creating the main interlinear view.
						LoadSecDataForEntry(hvoEntryReal, hvoSenseReal, hvoSbWord, cda, wsVern, hvoMbSec, fGuessing, sdaMain, tsf);
					}
				}
			}
			else
			{
				// No analysis, default or otherwise. We immediately, however, fill in a single
				// dummy morpheme, if showing morphology.
				fGuessing = 0;	// distinguish between a 'guess' (defaults) and courtesy filler info (cf. LT-5858).
				GuessColor = NoGuessColor;
				if (ShowMorphBundles)
				{
					int hvoMbSec = m_caches.DataAccess.MakeNewObject(kclsidSbMorph, hvoSbWord,
						ktagSbWordMorphs, 0);
					ITsString tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoSbWord, ktagSbWordForm, this.RawWordformWs);
					// Possibly adjust case of tssForm.
					if (fAdjustCase && CaseStatus == StringCaseStatus.title &&
						tssForm != null && tssForm.Length > 0)
					{
						tssForm = StringUtils.MakeTss(cf.ToLower(tssForm.Text), this.RawWordformWs);
						if (m_tssWordform != null)
							Marshal.ReleaseComObject(m_tssWordform);
						m_tssWordform = tssForm; // need this to be set in case hvoWordformRef set to zero.
						// If we adjust the case of the form, we must adjust the hvo as well,
						// or any analyses created will go to the wrong WfiWordform.
						hvoWordformRef = GetWordform(tssForm);
						if (hvoWordformRef != 0)
							m_fShowAnalysisCombo = m_caches.MainCache.GetVectorSize(hvoWordformRef,
								(int)WfiWordform.WfiWordformTags.kflidAnalyses) > 0;
					}
					else
					{
						// just use the wfi wordform form for our dummy morph form.
						tssForm = m_caches.MainCache.GetMultiStringAlt(hvoWordformRef, (int)WfiWordform.WfiWordformTags.kflidForm, this.RawWordformWs);
					}
					int hvoMorphForm = m_caches.FindOrCreateSec(0, kclsidSbNamedObj,
						hvoSbWord, ktagSbWordDummy);
					cda.CacheStringAlt(hvoMorphForm, ktagSbNamedObjName, wsVern, tssForm);
					cda.CacheObjProp(hvoMbSec, ktagSbMorphForm, hvoMorphForm);
					cda.CacheIntProp(hvoMorphForm, ktagSbNamedObjGuess, fGuessing);
				}
			}
			return fGuessing != 0;
		}

		/// <summary>
		/// Select the indicated icon of the word.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="tag"></param>
		protected void SelectIcon(int tag)
		{
			MoveSelectionIcon(new SelLevInfo[0], tag);
		}

		private int CreateSecondaryAndCopyStrings(int flidChoices, int hvoCategory, int flidMain, int hvoSbWord,
			ISilDataAccess sdaMain, IVwCacheDa cda, ITsStrFactory tsf)
		{
			int hvoWordPos = m_caches.FindOrCreateSec(hvoCategory,
				kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
			CopyStringsToSecondary(flidChoices, sdaMain, hvoCategory, flidMain, cda, hvoWordPos, ktagSbNamedObjName, tsf);
			return hvoWordPos;
		}

		private int CreateSecondaryAndCopyStrings(int flidChoices, int hvoMain, int flidMain)
		{
			return CreateSecondaryAndCopyStrings(flidChoices, hvoMain, flidMain, kSbWord,
				m_caches.MainCache.MainCacheAccessor, m_caches.DataAccess as IVwCacheDa, null);
		}

		/// <summary>
		/// Set the (real) LexEntry that is considered current. Broadcast a notification
		/// to delegates if it changed.
		/// </summary>
		/// <param name="hvoEntryReal"></param>
		public void SetSelectedEntry(int hvoEntryReal)
		{
			CheckDisposed();

			if (hvoEntryReal != m_hvoLastSelEntry)
			{
				m_hvoLastSelEntry = hvoEntryReal;
				if (SelectionChangedEvent != null)
					SelectionChangedEvent(this, new SIL.FieldWorks.Common.Utils.FwObjectSelectionEventArgs(hvoEntryReal));
			}
		}


		/// <summary>
		/// Get the string that should be stored in the MorphBundle Form.
		/// This will include any prefix and/or postfix markers.
		/// </summary>
		/// <param name="hvoSbMorph"></param>
		/// <returns></returns>
		private ITsString GetFullMorphForm(int hvoSbMorph)
		{
			ISilDataAccess sda = m_caches.DataAccess;
			int hvoSecMorph = sda.get_ObjectProp(hvoSbMorph, ktagSbMorphForm);
			ITsString tss = sda.get_MultiStringAlt(hvoSecMorph, ktagSbNamedObjName, this.RawWordformWs);
			// Add any prefix or postfix info to the form
			ITsStrBldr tsb = tss.GetBldr();
			tsb.ReplaceTsString(0, 0, sda.get_StringProp(hvoSbMorph, ktagSbMorphPrefix));
			tsb.ReplaceTsString(tsb.Length, tsb.Length,
				sda.get_StringProp(hvoSbMorph, ktagSbMorphPostfix));
			return tsb.GetString();
		}

		private void CopyStringsToSecondary(List<int> writingSystems, ISilDataAccess sdaMain, int hvoMain,
			int flidMain, IVwCacheDa cda, int hvoSec, int flidSec, ITsStrFactory tsf)
		{
			CheckDisposed();
			foreach (int ws in writingSystems)
			{
				int wsActual = 0;
				ITsString tss;
				if (ws > 0)
				{
					wsActual = ws;
				}
				else if (ws == LangProject.kwsFirstAnal)
				{
					int[] currentAnalysisWsList = m_caches.MainCache.LangProject.CurAnalysisWssRS.HvoArray;
					CacheStringAltForAllCurrentWs(currentAnalysisWsList, cda, hvoSec, flidSec, sdaMain, hvoMain, flidMain);
					continue;
				}
				else if (ws == LangProject.kwsFirstVern)
				{
					int[] currentVernWsList = m_caches.MainCache.LangProject.CurVernWssRS.HvoArray;
					CacheStringAltForAllCurrentWs(currentVernWsList, cda, hvoSec, flidSec, sdaMain, hvoMain, flidMain);
					continue;
				}
				else if (ws == LangProject.kwsVernInParagraph)
				{
					wsActual = this.RawWordformWs;
				}
				if (wsActual <= 0)
					throw new ArgumentException(String.Format("magic ws {0} not yet supported.", ws));

				if (hvoMain == 0)
				{
					tss = MakeTss("", wsActual, tsf);
				}
				else
				{
					tss = sdaMain.get_MultiStringAlt(hvoMain, flidMain, wsActual);
				}
				cda.CacheStringAlt(hvoSec, flidSec, wsActual, tss);
			}
		}

		/// <summary>
		/// Copy to the secondary cache NamedObect hvoSec relevant alternatives of
		/// property flid of real object hvoMain. Relevant alterantives are those belonging to
		/// flidChoices.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="sdaMain"></param>
		/// <param name="hvoMain"></param>
		/// <param name="cda"></param>
		/// <param name="hvoSec"></param>
		internal void CopyStringsToSecondary(int flidChoices, ISilDataAccess sdaMain, int hvoMain,
			int flidMain, IVwCacheDa cda, int hvoSec, int flidSec, ITsStrFactory tsf)
		{
			List<int> writingSystems = m_choices.WritingSystemsForFlid(flidChoices, true);
			CopyStringsToSecondary(writingSystems, sdaMain, hvoMain, flidMain, cda, hvoSec, flidSec, tsf);
		}

		private void CacheStringAltForAllCurrentWs(int[] currentWsList, IVwCacheDa cda, int hvoSec, int flidSec,
			ISilDataAccess sdaMain, int hvoMain, int flidMain)
		{
			foreach (int ws1 in currentWsList)
				cda.CacheStringAlt(hvoSec, flidSec, ws1, sdaMain.get_MultiStringAlt(hvoMain, flidMain, ws1));
		}

		private ITsString GetBestVernWordform(int hvoWordform)
		{
			// first we'll try getting vernacular ws directly, since it'll be true in most cases.
			ITsString tssForm = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(
				hvoWordform, (int)WfiWordform.WfiWordformTags.kflidForm, this.RawWordformWs);
			if (tssForm == null || tssForm.Length == 0)
			{
				// see if we can find a best vernacular form to use instead.
				// (Currently dummy wordforms only support default vernacular.)
				if (!m_caches.MainCache.IsDummyObject(hvoWordform))
				{
					IWfiWordform wf = new WfiWordform(m_caches.MainCache, hvoWordform);
					tssForm = wf.Form.BestVernacularAlternative;
				}
			}
			return tssForm;
		}

		/// <summary>
		/// Obtain the HVO of the most desirable default annotation to use for a particular
		/// wordform.
		/// </summary>
		/// <param name="hvoWordform"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="hvoGloss"></param>
		private void GetDefaults(int hvoWordform, out int hvoAnalysis, out int hvoGloss, bool fAdjustCase)
		{
			hvoAnalysis = hvoGloss = 0; // default
			if (hvoWordform == 0)
				return;
			ISilDataAccess sda = m_caches.MainCache.MainCacheAccessor;
			int ktagTwficDefault = InterlinVc.TwficDefaultTag(m_caches.MainCache);
			// If we're calling from the context of SetWordform(), we may be trying to establish
			// an alternative wordform/form/analysis. In that case, we don't want to change the
			// analysis/gloss based upon what was cached for the annotation.
			if (!m_fSetWordformInProgress && sda.get_IsPropInCache(m_hvoAnnotation, ktagTwficDefault,
				(int)CellarModuleDefns.kcptReferenceAtom, 0))
			{
				// If we've already cached a default, use it...it's surprising for the
				// user if we move the focus box to something and the default changes. (LT-4643 etc.)
				int hvoDefault1 = m_caches.MainCache.GetObjProperty(m_hvoAnnotation, ktagTwficDefault);
				if (hvoDefault1 != 0)
				{
					switch (m_caches.MainCache.GetClassOfObject(hvoDefault1))
					{
						case WfiAnalysis.kclsidWfiAnalysis:
							hvoAnalysis = hvoDefault1;
							hvoGloss = 0;
							return;
						case WfiGloss.kclsidWfiGloss:
							hvoGloss = hvoDefault1;
							hvoAnalysis = m_caches.MainCache.GetOwnerOfObject(hvoDefault1);
							return;
						// If its a wordform, or something weird, have a fresh go at getting one.
					}
				}
			}
			// Otherwise (no current default) do check for one...may have been an earlier occurrence
			// recently analyzed.
			string sql = string.Format("select top 1 AnalysisId, GlossId, [Score] "
				+ "from dbo.fnGetDefaultAnalysisGloss({0}) order by Score desc, GlossId",
				hvoWordform);
			int[] results = DbOps.ReadIntsFromRow(m_caches.MainCache, sql, null, 3);
			// If we got a default cache it, preferring a WfiGloss if we got that.
			int hvoDefault = 0;
			int score = 0;
			if (results.Length > 0)
			{
				score = results[2];
				hvoAnalysis = results[0];
				hvoGloss = results[1];
				hvoDefault = results[1];
				if (hvoDefault == 0)
				{
					// we just got a WfiAnalysis
					hvoDefault = results[0];
				}
			}
			if (fAdjustCase && CaseStatus != StringCaseStatus.allLower)
			{
				ITsString tssForm = GetBestVernWordform(hvoWordform);
				if (tssForm == null || tssForm.Length == 0)
				{
					return;	// not sure how to find the corresponding wordform form.
				}
				string form = tssForm.Text;
				string formLower = VernCaseFuncs(tssForm).ToLower(form);
				int hvoLower = GetWordform(StringUtils.MakeTss(formLower, this.RawWordformWs));
				if (hvoLower == 0)
					return;
				sql = string.Format("select top 1 AnalysisId, GlossId, [Score] "
					+ "from dbo.fnGetDefaultAnalysisGloss({0})", hvoLower);
				results = DbOps.ReadIntsFromRow(m_caches.MainCache, sql, null, 3);
				if (results.Length == 0 || results[2] < score)
					return; // guess we already made is more popular.
				hvoAnalysis = results[0];
				hvoGloss = results[1];
				// Enhance: possibly cache this as default for hvoLower?
			}
		}

		/// <summary>
		/// Make a selection at the end of the specified word gloss line.
		/// </summary>
		/// <param name="lineIndex">-1 if you want to select the first WordGlossLine.</param>
		/// <returns>true, if selection was successful.</returns>
		private bool SelectAtEndOfWordGloss(int lineIndex)
		{
			// get first line index if it isn't specified.
			if (lineIndex < 0)
			{
				lineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordGloss);
				if (lineIndex < 0)
					return false;
			}

			int glossLength;
			int cpropPrevious;
			GetWordGlossInfo(lineIndex, out glossLength, out cpropPrevious);
			// select at the end
			return MoveSelection(new SelLevInfo[0], ktagSbWordGloss, cpropPrevious, glossLength, glossLength);
		}

		private void GetWordGlossInfo(int lineIndex, out int glossLength, out int cpropPrevious)
		{
			int ws = m_choices[lineIndex].WritingSystem;
			ITsString tss;
			glossLength = 0;
			// InterlinLineChoices.kflidWordGloss, ktagSbWordGloss
			if (this.WordGlossHvo != 0)
			{
				tss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(this.WordGlossHvo,
					(int)WfiGloss.WfiGlossTags.kflidForm, ws);
				glossLength = tss.Length;
			}
			else
			{
				glossLength = 0;
			}

			cpropPrevious = m_choices.PreviousOccurrences(lineIndex);
		}

		/// <summary>
		/// Make a selection at the start of the indicated morpheme in the morphs line.
		/// That is, at the start of the prefix if there is one, otherwise, the start of the form.
		/// </summary>
		/// <param name="index"></param>
		private void SelectEntryIconOfMorph(int index)
		{
			SelectIconOfMorph(index, ktagMorphEntryIcon);
		}

		private void LoadSecDataForEntry(int hvoEntryReal, int hvoSenseReal, int hvoSbWord, IVwCacheDa cda, int wsVern,
			int hvoMbSec, int fGuessing, ISilDataAccess sdaMain, ITsStrFactory tsf)
		{
			int hvoEntry = m_caches.FindOrCreateSec(hvoEntryReal, kclsidSbNamedObj,
				hvoSbWord, ktagSbWordDummy);
			// try to determine if the given entry is a variant of the sense we passed in (ie. not an owner)
			ILexEntryRef ler = null;
			int hvoEntryToDisplay = hvoEntryReal;
			if (hvoSenseReal != 0)
			{
				ILexSense sense = LexSense.CreateFromDBObject(Cache, hvoSenseReal);
				ILexEntry variant = LexEntry.CreateFromDBObject(Cache, hvoEntryReal);
				if ((variant as LexEntry).IsVariantOfSenseOrOwnerEntry(sense, out ler))
					hvoEntryToDisplay = sense.EntryID;
			}

			ITsString tssLexEntry = InterlinDocChild.GetLexEntryTss(Cache, hvoEntryToDisplay, wsVern, ler);
			cda.CacheStringAlt(hvoEntry, ktagSbNamedObjName, wsVern, tssLexEntry);
			cda.CacheObjProp(hvoMbSec, ktagSbMorphEntry, hvoEntry);
			cda.CacheIntProp(hvoEntry, ktagSbNamedObjGuess, fGuessing);
			List<int> writingSystems = m_choices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidLexEntries, 0);
			if (writingSystems.Count > 0)
			{
				// Sigh. We're trying for some reason to display other alternatives of the entry.
				int hvoLf = sdaMain.get_ObjectProp(hvoEntryToDisplay, (int)LexEntry.LexEntryTags.kflidLexemeForm);
				if (hvoLf != 0)
					CopyStringsToSecondary(writingSystems, sdaMain, hvoLf,
						(int)MoForm.MoFormTags.kflidForm, cda, hvoEntry, ktagSbNamedObjName, tsf);
				else
					CopyStringsToSecondary(writingSystems, sdaMain, hvoEntryToDisplay,
						(int)LexEntry.LexEntryTags.kflidCitationForm, cda, hvoEntry, ktagSbNamedObjName, tsf);
			}
		}

		/// <summary>
		/// return the hvo of the wordform that corresponds to the given form, or zero if none.
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		private int GetWordform(ITsString form)
		{
			//			string sql = "select Obj from WfiWordform_Form where Txt = ? and ws = "
			//				+ this.RawWordformWs;
			//			int hvoWordform;
			//			DbOps.ReadOneIntFromCommand(m_caches.MainCache, sql, form.get_Text(), out hvoWordform);
			return Cache.LangProject.WordformInventoryOA.GetWordformId(form);
		}

		/// <summary>
		/// Make and install the selection indicated by the array and a following atomic property
		/// that contains an icon.
		/// </summary>
		/// <param name="rgvsli"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		private void MoveSelectionIcon(SelLevInfo[] rgvsli, int tag)
		{
			MakeSelectionIcon(rgvsli, tag, true);
		}

		private IVwSelection MakeSelectionIcon(SelLevInfo[] rgvsli, int tag, bool fInstall)
		{
			IVwSelection sel = null;
			try
			{
				m_fSuppressShowCombo = true;
				sel = RootBox.MakeSelInObj(0, rgvsli.Length, rgvsli, tag, fInstall);
			}
			catch (Exception)
			{
				// Ignore any problems
			}
			finally
			{
				m_fSuppressShowCombo = false;
			}
			return sel;
		}

		/// <summary>
		/// Make a string in the specified ws, using the provided TSF if possible,
		/// if passed null, make one.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="ws"></param>
		/// <param name="tsf"></param>
		/// <returns></returns>
		private ITsString MakeTss(string text, int ws, ITsStrFactory tsf)
		{
			ITsStrFactory tsfT = tsf;
			if (tsfT == null)
				tsfT = TsStrFactoryClass.Create();
			return tsfT.MakeString(text, ws);
		}

		/// <summary>
		/// Make and install the selection indicated by the array of objects on the first (and only root),
		/// an IP at the start of the property.
		/// </summary>
		/// <param name="rgvsli"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cpropPrevious"></param>
		/// <returns>true, if selection was successful.</returns>
		private bool MoveSelection(SelLevInfo[] rgvsli, int tag, int cpropPrevious)
		{
			return MoveSelection(rgvsli, tag, cpropPrevious, 0, 0);
		}
		/// <summary>
		/// Make and install the selection indicated by the array of objects on the first (and only root),
		/// an IP at the start of the property.
		/// </summary>
		/// <param name="rgvsli"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cpropPrevious"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// <returns>true, if selection was successful.</returns>
		private bool MoveSelection(SelLevInfo[] rgvsli, int tag, int cpropPrevious, int ichAnchor, int ichEnd)
		{
			bool fSuccessful;
			try
			{
				m_fSuppressShowCombo = true;
				RootBox.MakeTextSelection(0, rgvsli.Length, rgvsli, tag, cpropPrevious, ichAnchor, ichEnd, 0, false, -1, null, true);
				fSuccessful = true;
			}
			catch (Exception)
			{
				fSuccessful = false;
			}
			finally
			{
				m_fSuppressShowCombo = false;
			}
			return fSuccessful;
		}

		/// <summary>
		/// Select the indicated icon of the indicated morpheme.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="tag"></param>
		private void SelectIconOfMorph(int index, int tag)
		{
			SelLevInfo[] selectIndexMorph = new SelLevInfo[1];
			selectIndexMorph[0].tag = ktagSbWordMorphs;
			selectIndexMorph[0].ihvo = index;
			MoveSelectionIcon(selectIndexMorph, tag);
		}

		/// <summary>
		/// Given that we have set the form of hvoMorph (in the sandbox cache) to the given
		/// form, figure defaults for the LexEntry, LexGloss, and LexPos lines as far as
		/// possible. It is assumed that all three following lines are empty to begin with.
		/// </summary>
		public void EstablishDefaultEntry(int hvoMorph, string form, int hvoType,
			bool fMonoMorphemic)
		{
			CheckDisposed();
			int hvoFormSec = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphForm);
			// remove any existing mapping for this morph form, which might exist
			// from a previous analysis
			m_caches.RemoveSec(hvoFormSec);
			int hvoDefFormReal = DefaultMorph(form, hvoType);
			if (hvoDefFormReal == 0)
				return; // this form never occurs anywhere, can't supply any default.
			int hvoEntryReal = m_caches.MainCache.GetOwnerOfObject(hvoDefFormReal);
			ICmObject co = CmObject.CreateFromDBObject(m_caches.MainCache, hvoEntryReal);
			ILexEntry le = co as ILexEntry;
			int hvoEntry = m_caches.FindOrCreateSec(hvoEntryReal, kclsidSbNamedObj,
				kSbWord, ktagSbWordDummy);
			ITsString tssName;
			if (le == null)
			{
				tssName = StringUtils.MakeTss(co.ShortName, this.RawWordformWs);
			}
			else
			{
				int wsVern = RawWordformWs;
				int hvoEntryToDisplay = le.Hvo;
				ILexEntryRef ler = GetVariantRef(m_caches.MainCache, le.Hvo, fMonoMorphemic);
				if (ler != null)
				{
					ICmObject coRef = ler.ComponentLexemesRS[0];
					if (coRef is ILexSense)
						hvoEntryToDisplay = (coRef as ILexSense).EntryID;
					else
						hvoEntryToDisplay = coRef.Hvo;
				}
				tssName = InterlinDocChild.GetLexEntryTss(Cache, hvoEntryToDisplay, wsVern, ler);
			}
			m_caches.DataAccess.SetMultiStringAlt(hvoEntry, ktagSbNamedObjName, this.RawWordformWs, tssName);
			m_caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphEntry, hvoEntry);
			m_caches.DataAccess.SetInt(hvoEntry, ktagSbNamedObjGuess, 1);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll,
				hvoMorph, ktagSbMorphGloss, 0, 1, 0);
			// Establish the link between the SbNamedObj that represents the MoForm, and the
			// selected MoForm.  (This is used when building the real WfiAnalysis.)
			m_caches.Map(hvoFormSec, hvoDefFormReal);
			// This takes too long! Wait at least for a click in the bundle.
			//SetSelectedEntry(hvoEntryReal);
			EstablishDefaultSense(hvoMorph, hvoEntryReal, 0);
		}

		/// <summary>
		/// Find the MoForm, from among those whose form (in some ws) is the given one
		/// starting from the most frequently referenced by the MorphBundles property of a WfiAnalysis.
		/// If there is no wordform analysis, then fall back to selecting any matching MoForm.
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		public int DefaultMorph(string form, int hvoType)
		{
			CheckDisposed();

			List<int> ambiguousTypes = (CmObject.CreateFromDBObject(m_caches.MainCache, hvoType) as MoMorphType).AmbiguousTypes;
			int idum = 0;
			string ambiguousTypesList = DbOps.MakePartialIdList(ref idum, ambiguousTypes.ToArray());

			string sql = string.Format("select top 1 wmb.Morph, count(wmb.Morph) freq"
				+ " from WfiAnalysis_MorphBundles wamb"
				+ " join WfiMorphBundle wmb on wmb.Id=wamb.Dst"
				+ " join MoForm_Form mff on mff.Obj=wmb.Morph and mff.Txt=? and mff.Ws={0}"
				+ " join MoForm mf on mf.Id=wmb.Morph and mf.MorphType in ({1})"
				+ " group by wmb.Morph"
				+ " order by freq desc", this.RawWordformWs, ambiguousTypesList);

			int hvoMorphReal;
			DbOps.ReadOneIntFromCommand(m_caches.MainCache, sql, form, out hvoMorphReal);
			if (hvoMorphReal != 0)
				return hvoMorphReal;

			// Fall back to any matching MoForm
			string sql2 = string.Format("select top 1 mff.obj"
				+ " from MoForm_Form mff"
				+ " join MoForm mf on mf.id=mff.Obj and mf.MorphType in ({0})"
				+ " where mff.Txt=? and mff.Ws={1}", ambiguousTypesList, this.RawWordformWs);
			DbOps.ReadOneIntFromCommand(m_caches.MainCache, sql2, form, out hvoMorphReal);
			return hvoMorphReal;
		}

		/// <summary>
		/// Given that we have made hvoEntryReal the lex entry for the (sandbox) morpheme
		/// hvoMorph, look for the sense given by hvoSenseReal and fill it in.
		/// </summary>
		/// <param name="hvoMorph">the sandbox id of the Morph object</param>
		/// <param name="hvoEntryReal">the real database id of the lex entry</param>
		/// <param name="hvoSenseReal">
		/// The real database id of the sense to use.  If zero, use the first sense of the entry
		/// (if there is one) as a default.
		/// </param>
		/// <returns>default (real) sense if we found one, 0 otherwise.</returns>
		public int EstablishDefaultSense(int hvoMorph, int hvoEntryReal, int hvoSenseReal)
		{
			CheckDisposed();

			// If the entry has no sense we can't do anything.
			int hvoVariantSense = 0;
			if (m_caches.MainCache.MainCacheAccessor.get_VecSize(hvoEntryReal,
				(int)LexEntry.LexEntryTags.kflidSenses) == 0)
			{
				Debug.Assert(hvoSenseReal == 0);
				hvoVariantSense = GetSenseForVariantIfPossible(hvoEntryReal);
				if (hvoVariantSense == 0)
					return 0;
			}
			// If we already have a gloss for this entry, don't overwrite it with a default.
			int hvoMorphGloss = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
			if (hvoMorphGloss != 0 && hvoEntryReal == m_hvoLastSelEntry && hvoSenseReal == 0)
				return 0;

			int hvoDefSenseReal;
			if (hvoVariantSense != 0)
				hvoDefSenseReal = hvoVariantSense;
			else if (hvoSenseReal == 0)
				hvoDefSenseReal = m_caches.MainCache.MainCacheAccessor.get_VecItem(hvoEntryReal,
					(int)LexEntry.LexEntryTags.kflidSenses, 0);
			else
				hvoDefSenseReal = hvoSenseReal;
			string gloss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(
				hvoDefSenseReal, (int)LexSense.LexSenseTags.kflidGloss,
				m_caches.MainCache.DefaultAnalWs).Text;
			int hvoDefSense = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidLexGloss, hvoDefSenseReal,
				(int)LexSense.LexSenseTags.kflidGloss);

			// We're guessing the gloss if we just took the first sense, but if the user chose
			// one it is definite.
			m_caches.DataAccess.SetInt(hvoDefSense, ktagSbNamedObjGuess, hvoSenseReal == 0 ? 1 : 0);

			m_caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphGloss, hvoDefSense);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll,
				hvoMorph, ktagSbMorphGloss, 0, 1, 0);

			// Now if the sense has an MSA, set that up as a default too.
			int hvoDefMsaReal = m_caches.MainCache.MainCacheAccessor.get_ObjectProp(
				hvoDefSenseReal,
				(int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis);
			int cOldMsa = 0;
			if (m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphPos) != 0)
				cOldMsa = 1;
			if (hvoDefMsaReal != 0)
			{
				IMoMorphSynAnalysis msa = MoMorphSynAnalysis.CreateFromDBObject(
					m_caches.MainCache, hvoDefMsaReal);
				int hvoNewPos = m_caches.FindOrCreateSec(hvoDefMsaReal, kclsidSbNamedObj,
					kSbWord, ktagSbWordDummy);
				foreach (int ws in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexPos, true))
				{
					// Since ws maybe ksFirstAnal/ksFirstVern, we need to get what is actually
					// used in order to retrieve the data in Vc.Display().  See LT_7976.
					ITsString tssNew = msa.InterlinAbbrTSS(ws);
					int wsActual = StringUtils.GetWsAtOffset(tssNew, 0);
					m_caches.DataAccess.SetMultiStringAlt(hvoNewPos, ktagSbNamedObjName, wsActual, tssNew);
				}
				m_caches.DataAccess.SetInt(hvoNewPos, ktagSbNamedObjGuess, hvoSenseReal == 0 ? 1 : 0);
				m_caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphPos, hvoNewPos);
				m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll,
					hvoMorph, ktagSbMorphPos, 0, 1, cOldMsa);
			}
			else
			{
				// Going to null MSA, we still need to record the value and propagate the
				// change!  See LT-4246.
				m_caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphPos, 0);
				m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll,
					hvoMorph, ktagSbMorphPos, 0, 0, cOldMsa);
			}
			return hvoDefSenseReal;
		}

		/// <summary>
		/// If hvoEntry is the id of a variant, try to find an entry it's a variant of that
		/// has a sense.  Return the corresponding ILexEntryRef for the first such entry.
		/// If this is being called to establish a default monomorphemic guess, skip over
		/// any bound root or bound stem entries that hvoEntry may be a variant of.
		/// </summary>
		public static ILexEntryRef GetVariantRef(FdoCache cache, int hvoEntry, bool fMonoMorphemic)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			int cRef = sda.get_VecSize(hvoEntry, (int)LexEntry.LexEntryTags.kflidEntryRefs);
			for (int i = 0; i < cRef; ++i)
			{
				int hvoRef = sda.get_VecItem(hvoEntry,
					(int)LexEntry.LexEntryTags.kflidEntryRefs, i);
				int refType = sda.get_IntProp(hvoRef,
					(int)LexEntryRef.LexEntryRefTags.kflidRefType);
				if (refType == LexEntryRef.krtVariant)
				{
					int cEntries = sda.get_VecSize(hvoRef,
						(int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes);
					if (cEntries != 1)
						continue;
					int hvoComponent = sda.get_VecItem(hvoRef,
						(int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes, 0);
					int clid = cache.GetClassOfObject(hvoComponent);
					if (fMonoMorphemic && IsEntryBound(cache, hvoComponent, clid))
						continue;
					if (clid == LexSense.kclsidLexSense ||
						sda.get_VecSize(hvoComponent, (int)LexEntry.LexEntryTags.kflidSenses) > 0)
					{
						return LexEntryRef.CreateFromDBObject(cache, hvoRef);
					}
					else
					{
						// Should we check for a variant of a variant of a ...?
					}
				}
			}
			return null; // nothing useful we can do.
		}

		/// <summary>
		/// Check whether the given entry (or entry owning the given sense) is either a bound
		/// root or a bound stem.  We don't want to use those as guesses for monomorphemic
		/// words.  See LT-10323.
		/// </summary>
		private static bool IsEntryBound(FdoCache cache, int hvoComponent, int clid)
		{
			int hvoTargetEntry;
			if (clid == LexSense.kclsidLexSense)
			{
				ILexSense ls = LexSense.CreateFromDBObject(cache, hvoComponent);
				hvoTargetEntry = ls.Entry.Hvo;
				if (!(ls.MorphoSyntaxAnalysisRA is IMoStemMsa))
					return true;		// must be an affix, so it's bound by definition.
			}
			else
			{
				hvoTargetEntry = hvoComponent;
			}
			int hvoMorph = cache.MainCacheAccessor.get_ObjectProp(hvoTargetEntry,
				(int)LexEntry.LexEntryTags.kflidLexemeForm);
			if (hvoMorph != 0)
			{
				int hvoMorphType = cache.MainCacheAccessor.get_ObjectProp(hvoMorph,
					(int)MoForm.MoFormTags.kflidMorphType);
				if (hvoMorphType != 0)
				{
					if (MoMorphType.IsAffixType(cache, hvoMorphType))
						return true;
					Guid guid = cache.GetGuidFromId(hvoMorphType);
					if (guid == new Guid(MoMorphType.kguidMorphBoundRoot) ||
						guid == new Guid(MoMorphType.kguidMorphBoundStem))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// If hvoEntryReal refers to a variant, try for the first sense of the entry it's
		/// a variant of.  Otherwise, give up and return 0.
		/// </summary>
		private int GetSenseForVariantIfPossible(int hvoEntryReal)
		{
			ILexEntryRef ler = GetVariantRef(m_caches.MainCache, hvoEntryReal, false);
			if (ler != null)
			{
				if (ler.ComponentLexemesRS[0] is ILexEntry)
					return (ler.ComponentLexemesRS[0] as ILexEntry).SensesOS[0].Hvo;
				else
					return ler.ComponentLexemesRS[0].Hvo;	// must be a sense!
			}
			return 0;
		}

		// Handles a change in the item selected in the combo box.
		private void HandleComboSelChange(object sender, EventArgs ea)
		{
			if (m_fMakingCombo)
				return; // some spurious notification while initializing the combo.
			// Anything we do removes the combo box...it is put back only if we make a new
			// selection that requires one.
			HideCombos();
			m_ComboHandler.HandleSelectIfActive();

		}

		// Hide either kind of combo, if it is present and visible. Also the combo list, if any.
		private void HideCombos()
		{
			if (m_ComboHandler != null)
			{
				m_ComboHandler.Hide();
			}
			if (FirstLineHandler != null)
			{
				FirstLineHandler.Hide();
				// JohnT: we used to do this, but it could prevent the handler getting disposed.
				//FirstLineHandler = null;
			}
		}

		/// <summary>
		/// Make a combo box appropriate for the specified selection. If fMouseDown is true,
		/// do so unconditionally...otherwise (mousemove) only if the new selection is on a different thing.
		/// </summary>
		/// <param name="vwselNew"></param>
		/// <param name="fForce"></param>
		private void ShowComboForSelection(IVwSelection vwselNew, bool fMouseDown)
		{
			// It's a good idea to get this first...it's possible for MakeCombo to leave the selection invalid.
			SIL.FieldWorks.Common.Utils.Rect loc;
			vwselNew.GetParaLocation(out loc);
			if (!fMouseDown)
			{
				// It's a mouse move.
				// If we've moved to somewhere outside any paragraph get rid of the combos.
				// But, allow somewhere close, since otherwise it's almost impossible to get
				// a combo on an empty string.
				SIL.FieldWorks.Common.Utils.Rect locExpanded = loc;
				locExpanded.right += 50;
				locExpanded.left -= 5;
				locExpanded.top -= 2;
				locExpanded.bottom += 2;
				if (!locExpanded.Contains(m_LastMouseMovePos))
				{
					HideCombos();
					return;
				}
				// Don't do anything if the current mouse position is in the same paragraph
				// as before. Things tend to flicker if we continually create and remove it.
				// But, if we've hidden all the combos, go ahead even if at the same position as before...
				// otherwise, when we drag off outside the text and return, we may not get any combo.
				if (loc.Equals(m_locLastShowCombo) && FirstLineHandler != null)
				{
					return;
				}
			}
			FinishUpOk(); // Just like OK, if there are pending edits in the combo, do them.
			// Changing a different item may result in changes to this one also. This could invalidate
			// the selection, in which case, we can't use it.
			// Enhance JohnT: might consider trying the current selection, if any, if called from
			// MouseDown...that would not be useful if called from hover. But there probably isn't
			// a current selection in that case. Could try a selection at the saved mouse position.
			if (!vwselNew.IsValid)
				return;

			m_locLastShowCombo = loc;

			m_fMakingCombo = true;
			HideCombos();
			// No matter what, we are fixin to get rid of the old value.
			DisposeComboHandler();
			if (!m_fInMouseDrag)
			{
				m_ComboHandler = InterlinComboHandler.MakeCombo(vwselNew,
					this, fMouseDown);
			}
			else
			{
				m_ComboHandler = null;
			}
			m_fMakingCombo = false;
			m_fLockCombo = false; // nothing typed in it yet.
			if (m_ComboHandler != null)
			{
				// Set the position of the combo and display it. Do this before synchronizing
				// the LexEntry display, which can take a while.
				m_ComboHandler.Activate(loc);
				m_fMouseDownActivatedCombo = true;
				// If the selection moved to a different morpheme, and we know a corresponding
				// LexEntry, switch to it.
				if (m_ComboHandler.SelectedMorphHvo != 0)
				{
					int hvoSbEntry = m_caches.DataAccess.get_ObjectProp(
						m_ComboHandler.SelectedMorphHvo, ktagSbMorphEntry);
					if (hvoSbEntry != 0)
					{
						//SetSelectedEntry(m_caches.RealHvo(hvoSbEntry)); // seems to be buggy.
					}
				}
			}
		}

		public void OnOpenCombo()
		{
			CheckDisposed();

			IVwSelection selOrig = RootBox.Selection;
			if (selOrig == null)
			{
				MakeDefaultSelection();
				selOrig = RootBox.Selection;
				if (selOrig == null)
					return;
			}
			IVwSelection selArrow = ScanForIcon(selOrig);
			if (selArrow != null)
			{
				// Ensure the sandbox is visible so the menu displays in a meaningful
				// position.  See LT-7671.
				this.ParentDocChild.ScrollControlIntoView(this);
				// Simulate a mouse down on the arrow.
				ShowComboForSelection(selArrow, true);
			}

			// another approach that didn't work out because the boxes we want to navigate through
			// are INSIDE the paragraph that is the lowest level we can get at.
			//			int level = selOrig.get_BoxDepth(false);
			//			int index = selOrig.get_BoxIndex(false, level - 1);
			//			if (index == 0)
			//			{
			//				return true; // can't do anything, no pull-down arrow associated.
			//				// Enhance JohnT: when we eliminate the extra arrows in the Morph line,
			//				// this will get more complicated.
			//			}
			//			IVwSelection selArrow = RootBox.MakeSelInBox(selOrig, false, level - 1, 0, true, false, false);
			//			if (selArrow == null)
			//				return true; // again nothing we can do.
			//			if (selArrow.SelType != VwSelType.kstPicture)
			//				return true; // no arrow, nothing to do.
			//
			//			// Simulate a mouse down on the arrow.
			//			ShowComboForSelection(selArrow, true);
		}

		/// <summary>
		/// given a (text) selection, scan the beginning of its paragraph box and then immediately before its
		/// paragraph box in search of the icon.
		/// </summary>
		/// <param name="selOrig"></param>
		/// <returns></returns>
		private IVwSelection ScanForIcon(IVwSelection selOrig)
		{
			IVwSelection selArrow;
			int dxPixelIncrement = 3;
			uint iconParagraphWidth = 10;
			SIL.FieldWorks.Common.Utils.Rect rect;
			selOrig.GetParaLocation(out rect);
			if (m_vc.RightToLeft)
			{
				// Right to Left:
				selArrow = FindNearestSelectionType(selOrig, VwSelType.kstPicture,
					(uint)rect.right - iconParagraphWidth, iconParagraphWidth * 2, dxPixelIncrement);
				if (selArrow == null)
				{
					// we didn't find it next to our paragraph box. see if we can
					// find it at the beginning of the RootBox.
					selArrow = FindNearestSelectionType(selOrig, VwSelType.kstPicture,
						(uint)RootBox.Width - iconParagraphWidth, iconParagraphWidth, dxPixelIncrement);
				}
			}
			else
			{
				// Left to Right
				selArrow = FindNearestSelectionType(selOrig, VwSelType.kstPicture,
					(uint)rect.left + iconParagraphWidth, iconParagraphWidth * 2, -dxPixelIncrement);
				if (selArrow == null)
				{
					// we didn't find it next to our paragraph box. see if we can
					// find it at the beginning of the RootBox.
					selArrow = FindNearestSelectionType(selOrig, VwSelType.kstPicture,
						0, iconParagraphWidth, dxPixelIncrement);
				}
			}
			return selArrow;
		}


		/// <summary>
		/// find a selection of the given selType in the RootBox starting at xMin coordinate and spanning xMaxCountOfPixels along the mid y-coordinate
		/// of the given selOrig, by the direction and increment of dxPixelIncrement.
		/// </summary>
		/// <param name="selOrig">the selection from which we calculate the y-coordinate along which to scan.</param>
		/// <param name="selType">the type of selection we're looking for.</param>
		/// <param name="xMin">the starting x coordinate from which to scan.</param>
		/// <param name="xMaxCountOfPixels">the number of x units to scan from xMin.</param>
		/// <param name="dx">number and direction of pixels to probe for selType. a positive value
		/// will scan right, and a negative value will scan left. must be nonzero.</param>
		/// <returns></returns>
		private IVwSelection FindNearestSelectionType(IVwSelection selOrig, VwSelType selType, uint xMin, uint xMaxCountOfPixels, int dxPixelIncrement)
		{
			IVwSelection sel = null;
			if (dxPixelIncrement == 0)
				throw new ArgumentException(String.Format("dxPixelIncrement({0}) must be nonzero", dxPixelIncrement));

			SIL.FieldWorks.Common.Utils.Rect rect;
			selOrig.GetParaLocation(out rect);
			int y = rect.top + (rect.bottom - rect.top) / 2;
			Point pt = new Point((int)xMin, y);
			uint xLim = 0;
			if (dxPixelIncrement > 0)
			{
				// set our bounds for searching forward.
				xLim = xMin + xMaxCountOfPixels;
				// truncate if necessary.
				if (xLim > RootBox.Width)
					xLim = (uint)RootBox.Width;
			}
			else
			{
				// set our bounds for searching backward.
				// truncate if necessary.
				if (xMin > xMaxCountOfPixels)
					xLim = xMin - xMaxCountOfPixels;
				else
					xLim = 0;
			}
			while (dxPixelIncrement < 0 && pt.X > xLim ||
				dxPixelIncrement > 0 && pt.X < xLim)
			{
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
					if (sel != null && sel.SelType == selType)
						break;
					else
						sel = null;
				}
				pt.X += dxPixelIncrement;
			}
			return sel;
		}

		private IVwSelection SelectFirstAssociatedText()
		{
			return SelectFirstAssociatedText(RootBox.Selection);
		}

		/// <summary>
		/// Given an icon selection, find the nearest associated text.
		/// In left to right, that's the first text to right of the picture.
		/// In right to left, it's the first text to the left of the picture;
		/// </summary>
		/// <param name="selOrig"></param>
		/// <returns></returns>
		private IVwSelection SelectFirstAssociatedText(IVwSelection selOrig)
		{
			TextSelInfo tsi = new TextSelInfo(selOrig);
			if (!tsi.IsPicture)
			{
				return tsi.Selection;
			}
			Debug.Assert(tsi.IsPicture);
			IVwSelection selStartOfText = null;
			int dxPixelIncrement = 1;
			SIL.FieldWorks.Common.Utils.Rect rect;
			selOrig.GetParaLocation(out rect);
			int widthIconPara = (rect.right - rect.left);
			int xMaxCountOfPixels = (widthIconPara) * 2;
			if (m_vc.RightToLeft)
			{
				// Right to Left:
				// start at the left of the icon and move left to find the text.
				// let the limit be 2x the width of the icon.
				selStartOfText = FindNearestSelectionType(selOrig, VwSelType.kstText, (uint)(rect.left - dxPixelIncrement), (uint)xMaxCountOfPixels, -dxPixelIncrement);
			}
			else
			{
				// Left to Right
				// start at right of icon and move right to find text.
				selStartOfText = FindNearestSelectionType(selOrig, VwSelType.kstText, (uint)(rect.right + dxPixelIncrement), (uint)xMaxCountOfPixels, dxPixelIncrement);
			}
			if (selStartOfText != null)
			{
				// install at beginning of text selection
				selStartOfText.Install();
			}
			return selStartOfText;
		}

		/// <summary>
		/// Handle a tab character. If there is no selection in the sandbox,
		/// we go to the first line. If there is one, we go
		/// Wordform -> Morph (or word gloss, if morphs not showing)
		/// Morph ->LexEntry
		/// LexEntry, LexGloss, LexPOS->Next LexEntry, or word gloss.
		/// Word gloss->WordPOS
		/// WordPOS -> Wordform.
		///
		/// </summary>
		/// <param name="fShift">If true, reverse sequence.</param>
		internal void HandleTab(bool fShift)
		{
			CheckDisposed();

			int startLineIndex;
			int currentLineIndex;
			int increment;
			bool fSkipIcon;
			int iNextMorphIndex;
			GetLineOfCurrentSelectionAndNextTabStop(fShift, out currentLineIndex, out startLineIndex, out increment, out fSkipIcon, out iNextMorphIndex);
			if (currentLineIndex < 0)
				return;

			SelectOnOrBeyondLine(startLineIndex, currentLineIndex, increment, iNextMorphIndex, fSkipIcon, true);
		}

		internal int GetLineOfCurrentSelection()
		{
			int startLineIndex;
			int currentLineIndex;
			int increment;
			bool fSkipIcon;
			int iNextMorphIndex;
			GetLineOfCurrentSelectionAndNextTabStop(false, out currentLineIndex, out startLineIndex, out increment, out fSkipIcon, out iNextMorphIndex);
			return currentLineIndex;
		}

		private void GetLineOfCurrentSelectionAndNextTabStop(bool fShift, out int currentLineIndex, out int startLineIndex, out int increment, out bool fSkipIcon, out int iNextMorphIndex)
		{
			CheckDisposed();

			startLineIndex = -1;
			currentLineIndex = -1;
			increment = fShift ? -1 : 1;
			fSkipIcon = false;
			iNextMorphIndex = -1;

			ISilDataAccess sda = Caches.DataAccess;
			// Out variables for AllTextSelInfo.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttpBogus;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli;

			try
			{
				IVwSelection sel = RootBox.Selection;
				if (sel == null)
				{
					// Select first icon
					MoveAnalysisIconOrNext();
					return;
				}
				int cvsli = sel.CLevels(false) - 1;
				rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
					out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
			}
			catch
			{
				// If anything goes wrong just give up.
				return;
			}

			// Find our next morpheme index if our current selection is in a morpheme field
			bool fOnNextLine;
			int cMorph = CheckMorphs();
			iNextMorphIndex = NextMorphIndex(increment, out fOnNextLine);
			// Handle special cases where our current selection is not in a morpheme index.
			if (iNextMorphIndex < 0 && increment < 0)
				iNextMorphIndex = cMorph - 1;

			switch (tagTextProp)
			{
				case ktagAnalysisIcon: // Wordform icon
					currentLineIndex = 0;
					break;
				case ktagSbWordForm: // Wordform
					currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWord, ws);
					// try selecting the icon.
					if (increment < 0 && m_choices.IsFirstOccurrenceOfFlid(currentLineIndex))
						startLineIndex = currentLineIndex;
					break;
				case ktagMissingMorphs: // line 2, morpheme forms, no guess.
					// It's not supposed to be possible to get a selection into this line.
					// (Maybe the end-point, but not the anchor.)
					Debug.Assert(false);
					break;
				case ktagMorphFormIcon:
					currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidMorphemes);
					if (increment > 0 && !fOnNextLine)
					{
						iNextMorphIndex = 0;
						startLineIndex = currentLineIndex;  // try on the same line.
						fSkipIcon = true;
					}
					break;
				case ktagMissingMorphPos: // line 5, LexPos, missing.
				case ktagMissingMorphGloss: // line 4, LexGloss, missing.
				case ktagMissingEntry: // line 3, LexEntry, missing
					NextPositionForLexEntryText(increment, fOnNextLine, out currentLineIndex, out startLineIndex, ref iNextMorphIndex);
					break;
				case ktagMorphEntryIcon:
					currentLineIndex = m_choices.FirstLexEntryIndex;
					if (!fOnNextLine)
						startLineIndex = currentLineIndex; 	// try on the same line.
					break;
				case ktagSbWordGloss:
					currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordGloss, ws);
					if (increment < 0 && m_vc.ShowWordGlossIcon &&
						m_choices.PreviousOccurrences(currentLineIndex) == 0)
					{
						startLineIndex = currentLineIndex;
					}
					break;
				case ktagWordGlossIcon: // line 6, word gloss.
					currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordGloss);
					if (increment > 0)
					{
						fSkipIcon = true;
						startLineIndex = currentLineIndex;
					}
					break;
				case ktagMissingWordPos: // line 7, word POS, missing
					currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordPos);
					if (increment < 0)
						startLineIndex = currentLineIndex;
					break;
				case ktagWordPosIcon:
					currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordPos);
					break;
				case ktagSbMorphPrefix:
				case ktagSbMorphPostfix:
					currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidMorphemes);
					if (!fOnNextLine)
						startLineIndex = currentLineIndex;
					break;
				case ktagSbNamedObjName:
					{
						// This could be any of several non-missing objects.
						// Need to further subdivide.
						int tagObjProp = rgvsli[0].tag;
						switch (tagObjProp)
						{
							case ktagSbMorphForm:
								currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidMorphemes, ws);
								if (fOnNextLine)
								{
									if (increment < 0 && m_choices.IsFirstOccurrenceOfFlid(currentLineIndex))
									{
										// try selecting the icon.
										iNextMorphIndex = -1;
										startLineIndex = currentLineIndex;
									}
								}
								else
								{
									startLineIndex = currentLineIndex;
								}
								break;
							case ktagSbMorphPos: // line 5, LexPos.
							case ktagSbMorphGloss: // line 4, LexGloss
							case ktagSbMorphEntry: // line 3, LexEntry
								NextPositionForLexEntryText(increment, fOnNextLine, out currentLineIndex, out startLineIndex, ref iNextMorphIndex);
								break;
							case ktagSbWordPos: // line 7, WordPos.
								currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordPos);
								// try selecting the icon.
								if (increment < 0)
									startLineIndex = currentLineIndex;
								break;
						}
					}
					break;
			}

			// If we can't figure out where we are, tab to the master select icon.
			if (currentLineIndex < 0)
			{
				MakeDefaultSelection();
				return;
			}

			// By default, tab will go to the next (or previous) line;
			if (startLineIndex < 0)
				startLineIndex = currentLineIndex + increment;

			//Skip icon for text field, if going back a line
			if (increment < 0 && startLineIndex != currentLineIndex)
			{
				// only skip icon for editable fields.
				int nextLine = startLineIndex;
				if (startLineIndex < 0)
					nextLine = m_choices.Count - 1;
				if (m_choices[nextLine].Flid == InterlinLineChoices.kflidWordGloss ||
					m_choices[nextLine].Flid == InterlinLineChoices.kflidMorphemes)
				{
					fSkipIcon = true;
				}
			}
			return;
		}

		/// <summary>
		/// Handle the End key (Ctrl-End if fControl is true).
		/// Return true to suppress normal End-Key processing
		/// </summary>
		/// <param name="fControl"></param>
		/// <returns></returns>
		private bool HandleEndKey(bool fControl)
		{
			if (fControl)
			{
				return false; // Ctrl+End is now processed as a shortcut
			}
			IVwSelection sel = RootBox.Selection;
			if (sel == null)
				return false;
			if (sel.SelType == VwSelType.kstText)
			{
				// For a text selection, unless it's an IP at the end of the string, handle it normally.
				ITsString tss;
				bool fAssocPrev;
				int ichAnchor, ichEnd, hvoObjA, tagA, hvoObjE, tagE, wsE, wsA;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjE, out tagE, out wsE);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObjA, out tagA, out wsA);
				if (hvoObjE != hvoObjA || tagE != tagA || wsE != wsA || ichEnd != ichAnchor || ichEnd != tss.Length)
					return false;
			}
			// Move to the last property, which is the icon of the word POS
			SelectIcon(ktagWordPosIcon);
			return true;
		}

		/// <summary>
		/// Handle the Home key (Ctrl-Home if fControl is true).
		/// Return true to suppress normal Home-Key processing
		/// </summary>
		/// <param name="fControl"></param>
		/// <returns></returns>
		private bool HandleHomeKey(bool fControl)
		{
			if (fControl)
			{
				return false; // Ctrl+Home is now processed as a shortcut
			}
			IVwSelection sel = RootBox.Selection;
			if (sel == null)
				return false;
			if (sel.SelType == VwSelType.kstText)
			{
				// Unless it's an IP at the start of the string, handle it normally.
				ITsString tss;
				bool fAssocPrev;
				int ichAnchor, ichEnd, hvoObjA, tagA, hvoObjE, tagE, wsE, wsA;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjE, out tagE, out wsE);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObjA, out tagA, out wsA);
				if (hvoObjE != hvoObjA || tagE != tagA || wsE != wsA || ichEnd != ichAnchor || ichEnd != 0)
					return false;
			}
			// Move to the first property, which is the icon of the word analysis
			MoveAnalysisIconOrNext();
			return true;
		}

		/// <summary>
		/// Handle a press of the left arrow key.
		/// </summary>
		/// <returns></returns>
		private bool HandleLeftKey()
		{
			TextSelInfo tsi = new TextSelInfo(RootBox);
			InterlinDocChild parent = InterlinDoc;
			if (parent == null)
				return false;
			if (tsi.IsPicture)
			{
				int tagTextProp = tsi.TagAnchor;
				if (tagTextProp >= ktagMinIcon && tagTextProp < ktagLimIcon)
				{
					if (m_vc.RightToLeft)
					{
						// selection is on an icon: move to next adjacent text.
						SelectFirstAssociatedText();
						return true;
					}
					else
					{
						// we want to go the text next to the previous icon.
						return SelectTextNearestToNextIcon(false);
					}
				}
			}
			if (tsi.IsRange)
				return false;
			if (tsi.TagAnchor == ktagSbWordGloss)
			{
				if (tsi.IchEnd > 0 || parent == null)
					return false;
				return true;
			}
			else if (IsSelAtLeftOfMorph)
			{
				int currentLineIndex = GetLineOfCurrentSelection();
				if (m_vc.RightToLeft)
				{
					int index = this.MorphIndex;
					int cMorphs = m_caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
					if (index >= cMorphs - 1)
					{
						return true;
					}
					else
					{
						// move to the start of the next morpheme.
						SelectAtStartOfMorph(index + 1, m_choices.PreviousOccurrences(currentLineIndex));
					}
				}
				else
				{
					int index = this.MorphIndex;
					if (index == 0)
					{
						// no more morphemes in this word. move to previous word, selecting last morpheme.
						return true;
					}
					else
					{
						// move to the end of the previous morpheme.
						SelectAtEndOfMorph(index - 1, m_choices.PreviousOccurrences(currentLineIndex));
					}
				}

				return true;
			}
			else if (tsi.TagAnchor == 0)
			{
				return true;
			}
			else if (!tsi.Selection.IsEditable)
			{
				return SelectTextNearestToNextIcon(m_vc.RightToLeft);
			}
			return false;
		}

		bool m_fHandlingRightClickMenu = false;
		private bool HandleRightClickOnObject(int hvoReal, IxCoreColleague additionalTarget)
		{
			Debug.Assert(Mediator != null);
			CmObjectUi rightClickUiObj = CmObjectUi.MakeUi(Cache, hvoReal);
			if (rightClickUiObj != null)
			{
				rightClickUiObj.AdditionalColleague = additionalTarget;
				m_fHandlingRightClickMenu = true;
				try
				{
					//Debug.WriteLine("hvoReal=" + hvoReal.ToString() + " " + ui.Object.ShortName + "  " + ui.Object.ToString());
					return rightClickUiObj.HandleRightClick(Mediator, this, true);
				}
				finally
				{
					m_fHandlingRightClickMenu = false;
				}
			}
			return false;
		}

		/// <summary>
		/// The Sandbox is about to close, or move to another word...if there are pending edits,
		/// save them.  This is done by simulating a Return key press in the combo.
		/// </summary>
		public void FinishUpOk()
		{
			CheckDisposed();

			HideCombos();
		}

		private void DisposeComboHandler()
		{
			if (m_ComboHandler != null)
			{
				(m_ComboHandler as IDisposable).Dispose();
				m_ComboHandler = null;
			}
		}

		/// <summary>
		/// Create the default selection that is wanted when we move to a new word
		/// in a way that does not predetermine what is selected. (For example, left and
		/// right arrow can move to a specific corresponding field.)
		/// </summary>
		public void MakeDefaultSelection()
		{
			CheckDisposed();

			if (IsInGlossMode())
			{
				// since we're in the gloss tab first try to select the text of the word gloss,
				// for speed-glossing (LT-8371)
				int startingIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordGloss);
				if (startingIndex != -1)
				{
					SelectOnOrBeyondLine(startingIndex, 1, -1, true, false);
					return;
				}
				// next try for WordPos
				startingIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordPos);
				if (startingIndex != -1)
				{
					SelectOnOrBeyondLine(startingIndex, 1, -1, false, false);
					return;
				}
			}
			// by default try to select the top line/icon.
			MoveAnalysisIconOrNext();
		}

		/// <summary>
		/// Move to the analysis icon if we're showing one, otherwise, to the following line.
		/// </summary>
		private void MoveAnalysisIconOrNext()
		{
			SelectOnOrBeyondLine(0, 1);
		}

		/// <summary>
		/// Return the count of morphemes, after creating the initial one if there are none.
		/// </summary>
		/// <returns></returns>
		private int CheckMorphs()
		{
			int cmorphs = Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
			// Commenting out the following line solves LT-9090 for baseline capitalization changes
			//Debug.Assert(cmorphs != 0, "We should have already initialized our first morpheme.");
			if (cmorphs == 0)
			{
				// Tabbing into a missing morphemes line.
				MakeFirstMorph();
				cmorphs = 1;
			}
			return cmorphs;
		}

		/// <summary>
		/// Return the index of the next morpheme, based on the current selection and increment (direction).
		/// </summary>
		/// <param name="increment">the direction of morpheme advancement. +1 is forward, -1 backwards.</param>
		/// <param name="fOnNextLine">true, if moving the morpheme index would put us on another line.</param>
		/// <returns>index of the next morpheme.</returns>
		private int NextMorphIndex(int increment, out bool fOnNextLine)
		{
			fOnNextLine = false; // by default
			int cMorph = CheckMorphs(); // Create if needed
			int iStartingMorph = MorphIndex;
			if (iStartingMorph < 0)
				return -1;	// our current selection is not a morpheme field.

			// our current selection is a morpheme index, so calculate the next position.
			int iNextMorph = iStartingMorph + increment;
			if (iNextMorph < 0 || iNextMorph >= cMorph)
			{
				iNextMorph %= cMorph;
				if (iNextMorph < 0)
					iNextMorph += cMorph;
				fOnNextLine = true;
			}
			return iNextMorph;
		}

		private void NextPositionForLexEntryText(int increment, bool fOnNextLine,
			out int currentLineIndex, out int startLineIndex, ref int iNextMorphIndex)
		{
			currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidLexEntries);
			if (increment < 0)
			{
				// try selecting the icon.
				iNextMorphIndex = MorphIndex;
				startLineIndex = currentLineIndex;
			}
			else if (!fOnNextLine)
			{
				startLineIndex = currentLineIndex;
			}
			else
			{
				startLineIndex = currentLineIndex + increment;
			}
		}

		/// <summary>
		/// Try to make a selection on the specified line, or some subsequent line in the
		/// direction indicated by increment.
		/// </summary>
		/// <param name="startLine"></param>
		/// <param name="increment">(the direction is indicated by increment, which should be 1 or -1)</param>
		/// <returns></returns>
		internal bool SelectOnOrBeyondLine(int startLine, int increment)
		{
			return SelectOnOrBeyondLine(startLine, increment, 0, false, true);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="startLine"></param>
		/// <param name="increment">(the direction is indicated by increment, which should be 1 or -1)</param>
		/// <param name="iMorph"></param>
		/// <returns></returns>
		internal bool SelectOnOrBeyondLine(int startLine, int increment, int iMorph, bool fSkipIconToTextField, bool fWrapToNextLine)
		{
			return SelectOnOrBeyondLine(startLine, startLine, increment, iMorph, fSkipIconToTextField, fWrapToNextLine);
		}

		/// <summary>
		/// Try to make a selection on startLine. If unsuccessful, try successive lines
		/// (the direction is indicated by increment, which should be 1 or -1).
		/// If the increment process reaches the beginning or end, wrap around.
		/// If it reaches limitLine, give up and return false.
		/// (However, it WILL try startLine once, even if it is equal to limitLine.
		/// </summary>
		/// <param name="startLine"></param>
		/// <param name="limitLine"></param>
		/// <param name="increment"></param>
		/// <param name="iMorph"></param>
		/// <param name="fSkipIconToTextField">set to true if you want to skip the
		/// combo icon when trying to select something on a line with an editable
		/// text field (Morpheme or WordGloss line)</param>
		/// <param name="fWrapToNextLine">if true, we'll try wrapping back around to limitLine,
		/// if false, we'll stop at the natural line boundaries.</param>
		/// <returns></returns>
		///
		private bool SelectOnOrBeyondLine(int startLine, int limitLine, int increment, int iMorph,
			bool fSkipIconToTextField, bool fWrapToNextLine)
		{
			if (!fWrapToNextLine)
			{
				if (increment > 0)
					limitLine = m_choices.Count;
				else
					limitLine = -1;
			}
			bool fFirstTime = fWrapToNextLine;
			for (int ispec = startLine; fFirstTime || ispec != limitLine; ispec += increment, fFirstTime = false)
			{
				int ispecOrig = ispec;
				if (ispec == m_choices.Count)
					ispec = 0; // wrap around to top
				if (ispec < 0)
					ispec = m_choices.Count - 1; // wrap around to bottom.
				if (ispec != ispecOrig)
				{
					// we wrapped lines, test to see if we equal limitLine before continuing.
					if (ispec == limitLine && !fFirstTime)
						return false;
				}
				InterlinLineSpec spec = m_choices[ispec];
				switch (spec.Flid)
				{
					case InterlinLineChoices.kflidWord:
						if (!fSkipIconToTextField && m_choices.PreviousOccurrences(ispec) == 0)
						{
							if (ShowAnalysisCombo)
							{
								SelectIcon(ktagAnalysisIcon);
								return true;
							}
							else
								break; // can't do anything else on line 0.
						}
						else
						{
							// make a selection in an alternative writing system.
							MoveSelection(new SelLevInfo[0], ktagSbWordForm,
								m_choices.PreviousOccurrences(ispec));
							return true;
						}
					case InterlinLineChoices.kflidMorphemes:
						int cMorphs = CheckMorphs();
						if (fSkipIconToTextField && iMorph < 0)
							iMorph = 0;
						if (iMorph >= 0 && iMorph < cMorphs)
							SelectAtStartOfMorph(iMorph, m_choices.PreviousOccurrences(ispec));
						else
							SelectIconOfMorph(0, ktagMorphFormIcon);
						return true;
					case InterlinLineChoices.kflidLexEntries:
					case InterlinLineChoices.kflidLexGloss:
					case InterlinLineChoices.kflidLexPos:
						if (ispec != m_choices.FirstLexEntryIndex)
							break;
						// Move to one of the lex entry icons.
						cMorphs = CheckMorphs();
						int iNextMorph = -1;
						if (iMorph >= 0 && iMorph < cMorphs)
							iNextMorph = iMorph;
						else
							iNextMorph = 0;
						IVwSelection selIcon = MakeSelectionIcon(AddMorphInfo(new SelLevInfo[0], iNextMorph),
							ktagMorphEntryIcon, !fSkipIconToTextField);
						if (fSkipIconToTextField)
						{
							// try to select the text next to the icon.
							SelectFirstAssociatedText(selIcon);
						}
						return true;
					case InterlinLineChoices.kflidWordGloss:
						if (!fSkipIconToTextField && m_vc.ShowWordGlossIcon && m_choices.PreviousOccurrences(ispec) == 0)
						{
							SelectIcon(ktagWordGlossIcon);
						}
						else if (IsInGlossMode())
						{
							// in gloss mode, we want to select the whole gloss, since the user may want to type over it.
							int glossLength;
							int cpropPrevious;
							GetWordGlossInfo(ispec, out glossLength, out cpropPrevious);
							return MoveSelection(new SelLevInfo[0], ktagSbWordGloss, cpropPrevious, 0, glossLength);
						}
						else
						{
							SelectAtEndOfWordGloss(ispec);
						}
						return true;
					case InterlinLineChoices.kflidWordPos:
						if (m_choices.PreviousOccurrences(ispec) != 0)
							break;
						selIcon = MakeSelectionIcon(new SelLevInfo[0],
							ktagWordPosIcon, !fSkipIconToTextField);
						if (fSkipIconToTextField)
						{
							// try to select the text next to the icon.
							SelectFirstAssociatedText(selIcon);
						}
						return true;
				}
			}
			return false; // unable to make any selection.
		}

		/// <summary>
		/// Make a selection at the start of the indicated morpheme in the morphs line.
		/// That is, at the start of the prefix if there is one, otherwise, the start of the form.
		/// </summary>
		/// <param name="index"></param>
		/// <returns>true, if selection was made.</returns>
		private bool SelectAtStartOfMorph(int index)
		{
			return SelectAtStartOfMorph(index, 0);
		}
		private bool SelectAtStartOfMorph(int index, int cprevOccurrences)
		{
			if (m_choices.IndexOf(InterlinLineChoices.kflidMorphemes) < 0)
			{
				return false;
			}
			CheckMorphs();	// make sure we have at least one morph to select.
			int hvoMorph = m_caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, index);
			if (m_caches.DataAccess.get_StringProp(hvoMorph, ktagSbMorphPrefix).Length == 0 ||
				cprevOccurrences > 0)
			{
				// Select at the start of the name of the form
				SelLevInfo[] selectIndexMorph = new SelLevInfo[2];
				selectIndexMorph[0].tag = ktagSbMorphForm;
				selectIndexMorph[0].ihvo = 0;
				selectIndexMorph[0].cpropPrevious = cprevOccurrences;
				selectIndexMorph[1].tag = ktagSbWordMorphs;
				selectIndexMorph[1].ihvo = index;
				RootBox.MakeTextSelection(0, 2, selectIndexMorph, ktagSbNamedObjName, 0, 0, 0, this.RawWordformWs, false, -1, null, true);
			}
			else
			{
				// Select at the start of the prefix
				SelLevInfo[] selectIndexMorph = new SelLevInfo[1];
				selectIndexMorph[0].tag = ktagSbWordMorphs;
				selectIndexMorph[0].ihvo = index;
				RootBox.MakeTextSelection(0, 1, selectIndexMorph, ktagSbMorphPrefix, 0, 0, 0, 0, false, -1, null, true);
			}
			return true;
		}

		/// <summary>
		/// Make a selection at the end of the indicated morpheme in the morphs line.
		/// That is, at the end of the postfix if there is one, otherwise, the end of the form.
		/// </summary>
		/// <param name="index"></param>
		private void SelectAtEndOfMorph(int index)
		{
			SelectAtEndOfMorph(index, 0);
		}

		private void SelectAtEndOfMorph(int index, int cPrevOccurrences)
		{
			ISilDataAccess sda = m_caches.DataAccess;
			int hvoMorph = sda.get_VecItem(kSbWord, ktagSbWordMorphs, index);
			int cchPostfix = sda.get_StringProp(hvoMorph, ktagSbMorphPostfix).Length;
			if (cchPostfix == 0 || cPrevOccurrences > 0)
			{
				// Select at the end of the name of the form
				SelLevInfo[] selectIndexMorph = new SelLevInfo[2];
				selectIndexMorph[0].tag = ktagSbMorphForm;
				selectIndexMorph[0].ihvo = 0;
				selectIndexMorph[0].cpropPrevious = cPrevOccurrences;
				selectIndexMorph[1].tag = ktagSbWordMorphs;
				selectIndexMorph[1].ihvo = index;
				int hvoNamedObj = sda.get_ObjectProp(hvoMorph, ktagSbMorphForm);
				List<InterlinLineSpec> matchingSpecs = m_choices.ItemsWithFlids(new int[] { InterlinLineChoices.kflidMorphemes });
				InterlinLineSpec specMorphemes = matchingSpecs[cPrevOccurrences];
				int ws = specMorphemes.WritingSystem;
				if (specMorphemes.IsMagicWritingSystem)
					ws = this.RawWordformWs;
				int cchName = sda.get_MultiStringAlt(hvoNamedObj, ktagSbNamedObjName, ws).Length;
				RootBox.MakeTextSelection(0, 2, selectIndexMorph, ktagSbNamedObjName, 0, cchName, cchName, this.RawWordformWs, false, -1, null, true);
			}
			else
			{
				// Select at the end of the postfix
				SelLevInfo[] selectIndexMorph = new SelLevInfo[1];
				selectIndexMorph[0].tag = ktagSbWordMorphs;
				selectIndexMorph[0].ihvo = index;
				RootBox.MakeTextSelection(0, 1, selectIndexMorph, ktagSbMorphPostfix, 0, cchPostfix, cchPostfix, 0, false, -1, null, true);
			}
		}

		/// <summary>
		/// Create a first morpheme whose content matches the word.
		/// Typically the user clicked or tabbed to an empty morpheme line.
		/// </summary>
		internal void MakeFirstMorph()
		{
			CheckDisposed();

			ISilDataAccess sda = Caches.DataAccess;
			IVwCacheDa cda = sda as IVwCacheDa;
			int hvoSbMorph = sda.MakeNewObject(Sandbox.kclsidSbMorph, kSbWord,
				ktagSbWordMorphs, 0);
			int hvoSbForm = sda.MakeNewObject(kclsidSbNamedObj, hvoSbMorph,
				ktagSbMorphForm, -2); // -2 for atomic
			int wsVern = this.RawWordformWs;
			ITsString tssForm = SbWordForm(wsVern);
			cda.CacheStringAlt(hvoSbForm, ktagSbNamedObjName, wsVern, tssForm);
			// the morpheme is not a guess, since the user is now working on it.
			cda.CacheIntProp(hvoSbForm, ktagSbNamedObjGuess, 0);
			// Now make a notification to get it redrawn. (A PropChanged doesn't
			// work...   we don't have enough NoteDependency calls.)
			RootBox.Reconstruct();
		}

		internal ITsString SbWordForm(int wsVern)
		{
			ISilDataAccess sda = Caches.DataAccess;
			ITsString tssForm = sda.get_MultiStringAlt(kSbWord, Sandbox.ktagSbWordForm, wsVern);
			return tssForm;
		}

		/// <summary>
		/// Add to rgvsli an initial item that selects the indicated morpheme
		/// </summary>
		/// <param name="rgvsli"></param>
		/// <param name="isense"></param>
		/// <returns></returns>
		private SelLevInfo[] AddMorphInfo(SelLevInfo[] rgvsli, int isense)
		{
			SelLevInfo[] result = new SelLevInfo[rgvsli.Length + 1];
			rgvsli.CopyTo(result, 1);
			result[0].tag = ktagSbWordMorphs;
			result[0].ihvo = isense;
			return result;
		}

		/// <summary>
		/// Handle a press of the right arrow key.
		/// </summary>
		/// <returns></returns>
		private bool HandleRightKey()
		{
			TextSelInfo tsi = new TextSelInfo(RootBox);
			if (tsi.IsPicture)
			{
				int tagTextProp = tsi.TagAnchor;
				if (tagTextProp >= ktagMinIcon && tagTextProp < ktagLimIcon)
				{
					if (m_vc.RightToLeft)
					{
						// we want to go the text next to the previous icon.
						return SelectTextNearestToNextIcon(false);
					}
					else
					{
						// selection is on an icon: move to next adjacent text.
						SelectFirstAssociatedText();
						return true;
					}
				}
			}
			if (tsi.IsRange)
				return false;
			if (tsi.TagAnchor == ktagSbWordGloss)
			{
				if (tsi.IchEnd == tsi.AnchorLength)
					return true;	// don't wrap.
			}
			else if (IsSelAtRightOfMorph)
			{
				int currentLineIndex = GetLineOfCurrentSelection();
				if (m_vc.RightToLeft)
				{
					int index = this.MorphIndex;
					if (index == 0)
					{
						return true;
					}
					else
					{
						// move to the end of the previous morpheme.
						SelectAtEndOfMorph(index - 1, m_choices.PreviousOccurrences(currentLineIndex));
					}
				}
				else
				{
					int index = this.MorphIndex;
					int cMorphs = m_caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
					if (index >= cMorphs - 1)
					{
						return true;
					}
					else
					{
						// move to the start of the next morpheme.
						SelectAtStartOfMorph(index + 1, m_choices.PreviousOccurrences(currentLineIndex));
					}
				}

				return true;
			}
			else if (tsi.TagAnchor == 0)
			{
				return true;
			}
			else if (!tsi.Selection.IsEditable)
			{
				return SelectTextNearestToNextIcon(!m_vc.RightToLeft);
			}
			return false;
		}

		/// <summary>
		/// Find the next icon to jump to, and then select nearest text
		/// </summary>
		/// <param name="fForward"></param>
		/// <returns></returns>
		private bool SelectTextNearestToNextIcon(bool fForward)
		{
			bool fShift = !fForward;
			int currentLineIndex;
			int startLineIndex;
			int increment;
			bool fSkipIcon;
			int iNextMorphIndex;
			GetLineOfCurrentSelectionAndNextTabStop(fShift, out currentLineIndex, out startLineIndex, out increment, out fSkipIcon, out iNextMorphIndex);
			int currentMorphIndex = this.MorphIndex;
			int cMorphs = this.MorphCount;
			if (fForward && currentMorphIndex == (cMorphs - 1))
			{
				// don't wrap.
				iNextMorphIndex = currentMorphIndex;
			}
			else if (!fForward)
			{
				iNextMorphIndex = currentMorphIndex - 1;
			}
			if (currentLineIndex >= 0)
			{
				SelectOnOrBeyondLine(currentLineIndex, -1, increment, iNextMorphIndex, true, false);
				return true;
			}
			return false;
		}

		private bool HandleUpKey()
		{
			return HandleMovingSelectionToTextInAdjacentLine(false);
		}

		private bool HandleDownKey()
		{
			return HandleMovingSelectionToTextInAdjacentLine(true);
		}

		private bool HandleMovingSelectionToTextInAdjacentLine(bool fForward)
		{
			TextSelInfo tsi = new TextSelInfo(RootBox);
			if (tsi.IsText)
			{
				int line = GetLineOfCurrentSelection();
				int direction = (fForward ? 1 : -1);
				int nextLine = line + direction;
				int index = this.MorphIndex;
				SelectOnOrBeyondLine(nextLine, direction, index, true, false);
				return true;
			}
			else if (tsi.TagAnchor == 0)
			{
				return true;
			}
			else
			{
				// The remaining cases apply to uneditable fields.
				// Find the nearest icon to jump to.
			}
			return false;
		}

		/// <summary>
		/// Gets the WfiAnalysis HVO of the analysis we're displaying (if any).
		/// </summary>
		/// <returns>This will return 0 if the analysis is on the wordform.</returns>
		internal int GetWfiAnalysisHvoOfAnalysis()
		{
			CheckDisposed();

			return GetWfiAnalysisHvoOfAnalysis(m_hvoAnalysis);
		}

		/// <summary>
		/// The wfi analysis currently used to setup the sandbox. Could have possibly come from a guess,
		/// not simply the current annotations InstanceOf.
		/// </summary>
		internal int GetWfiAnalysisHvoInUse()
		{
			CheckDisposed();
			int hvoWa = this.GetWfiAnalysisHvoOfAnalysis();
			if (hvoWa == 0)
			{
				int temp_hvoWordGloss;
				this.GetDefaults(this.GetWordformHvoOfAnalysis(), out hvoWa, out temp_hvoWordGloss, false);
			}
			return hvoWa;
		}


		/// <summary>
		/// Gets the WfiAnalysis HVO of the given analysis.
		/// </summary>
		/// <returns>This will return 0 if the analysis is on the wordform.</returns>
		internal int GetWfiAnalysisHvoOfAnalysis(int hvoAnalysis)
		{
			CheckDisposed();

			if (hvoAnalysis == 0 || !m_caches.MainCache.IsValidObject(hvoAnalysis))
				return 0;
			switch (m_caches.MainCache.GetClassOfObject(hvoAnalysis))
			{
				case WfiWordform.kclsidWfiWordform:
					return 0;
				case WfiAnalysis.kclsidWfiAnalysis:
					return hvoAnalysis;
				case WfiGloss.kclsidWfiGloss:
					return m_caches.MainCache.GetOwnerOfObject(hvoAnalysis);
				default:
					throw new Exception("Invalid type found in word analysis annotation");
			}
		}

		/// <summary>
		/// Copied from ChooseAnalysisHandler.
		/// Gets the HVO of the wordform (in the real cache) that owns the analysis we're
		/// displaying. (If there's no current analysis, or it is an object which someone
		/// has pathologically deleted in another window, return the current wordform.)
		/// </summary>
		/// <returns></returns>
		internal int GetWordformHvoOfAnalysis()
		{
			CheckDisposed();

			if (m_hvoAnalysis == 0 || !m_caches.MainCache.IsValidObject(m_hvoAnalysis))
				return m_hvoWordform;
			switch (m_caches.MainCache.GetClassOfObject(m_hvoAnalysis))
			{
				case WfiWordform.kclsidWfiWordform:
					return m_hvoAnalysis;
				case WfiAnalysis.kclsidWfiAnalysis:
					return m_caches.MainCache.GetOwnerOfObject(m_hvoAnalysis);
				case WfiGloss.kclsidWfiGloss:
					return m_caches.MainCache.GetOwnerOfObject(m_caches.MainCache.GetOwnerOfObject(m_hvoAnalysis));
				default:
					throw new Exception("Invalid type found in word analysis annotation");
			}
		}

		/// <summary>
		/// This handles the user choosing an analysis from the pull-down in the top ("Word") line of
		/// the Sandbox. The sender must be a ChooseAnalysisHandler (CAH). The Sandbox switches to showing
		/// the analysis indicated by the 'Analysis' property of that CAH.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void Handle_AnalysisChosen(object sender, EventArgs e)
		{
			CheckDisposed();

			ChooseAnalysisHandler handler = (ChooseAnalysisHandler)sender;
			m_hvoAnalysis = handler.Analysis;
			Debug.Assert(m_hvoAnalysis >= 0);
			bool fLookForDefaults = true;
			if (m_hvoAnalysis == 0)
			{
				// 'Use default analysis'. This can normally be achieved by loading data
				// after setting m_hvoAnalysis to m_hvoWordformOriginal.
				// But possibly no default is loaded into the cache. (This can happen if
				// all visible occurrences of the wordform were analyzed before the sandbox was
				// displayed.)
				m_hvoAnalysis = m_hvoWordformOriginal;
			}
			else if (m_hvoAnalysis == m_hvoWordform)
			{
				// 'New analysis'
				// We want to force no default to be filled in.
				fLookForDefaults = false;
			}

			// REVIEW: do we need to worry about changing the previous and next words?
			LoadRealDataIntoSec(fLookForDefaults, false, false);
			OnUpdateEdited();
			m_fShowAnalysisCombo = true; // we must want this icon, because we were previously showing it!
			m_rootb.Reconstruct();
			// if the user has selected a special item, such as "New word gloss",
			// set our selection in the most helpful location, if it is available.
			HvoTssComboItem selectedItem = handler.SelectedItem;
			bool fMadeSelection = false;
			switch (selectedItem.Tag)
			{
				case WfiWordform.kclsidWfiWordform:
					fMadeSelection = SelectAtStartOfMorph(0);
					break;
				case WfiGloss.kclsidWfiGloss:
					fMadeSelection = SelectAtEndOfWordGloss(-1);
					break;
			}
			if (!fMadeSelection)
				MakeDefaultSelection();
		}

		// This just makes the combo visible again. It is more common to tell the ComboHandler
		// to Activate.
		internal void ShowCombo()
		{
			CheckDisposed();
		}

		/// <summary>
		/// Find the actual original form of the current wordform.
		/// (For more information see definition for Sandbox.FormOfWordForm.)
		/// </summary>
		/// <param name="hvoRealWordform"></param>
		/// <returns></returns>
		internal ITsString FindAFullWordForm(int hvoRealWordform)
		{
			CheckDisposed();

			ITsString tssForm;
			if (hvoRealWordform == 0)
			{
				tssForm = this.FormOfWordform; // use the one we saved.
			}
			else
			{
				tssForm = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(
					hvoRealWordform, (int)WfiWordform.WfiWordformTags.kflidForm,
					this.RawWordformWs);
			}
			return tssForm;
		}

		/// <summary>
		/// Handle setting the wordform shown in the sandbox when the user chooses an alternate
		/// case form in the Morpheme combo.
		/// </summary>
		/// <param name="form"></param>
		bool m_fSetWordformInProgress = false;
		internal void SetWordform(ITsString form, bool fLookForDefaults)
		{
			CheckDisposed();

			m_fSetWordformInProgress = true;
			try
			{
				if (m_tssWordform != null)
					Marshal.ReleaseComObject(m_tssWordform);
				m_tssWordform = form;
				m_hvoWordform = GetWordform(form);
				m_hvoAnalysis = m_hvoWordform;
				ISilDataAccess sda = m_caches.DataAccess;
				IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
				// Now erase the current morph bundles.
				cda.CacheVecProp(kSbWord, ktagSbWordMorphs, new int[0], 0);
				if (m_hvoAnalysis == 0)
				{
					Debug.Assert(form != null);
					// No wordform exists corresponding to this case-form.
					// Put the sandbox in a special state where there is just one morpheme, the specified form.
					int hvoMorph0 = sda.MakeNewObject(kclsidSbMorph,
						kSbWord, ktagSbWordMorphs, 0); // make just one new morpheme.
					int hvoNewForm = sda.MakeNewObject(kclsidSbNamedObj,
						kSbWord, ktagSbWordDummy, 0); // make the object to be the form of the morpheme
					sda.SetMultiStringAlt(hvoNewForm, ktagSbNamedObjName, this.RawWordformWs, m_tssWordform); // set its text
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoNewForm, ktagSbNamedObjName, 0, 1, 1);
					sda.SetObjProp(hvoMorph0, ktagSbMorphForm, hvoNewForm); // and set the reference.
					ClearAllGlosses();
					// and clear the POS.
					sda.SetObjProp(kSbWord, ktagSbWordPos, 0);
					m_fShowAnalysisCombo = false; // no multiple analyses available for this dummy wordform
				}
				else
				{
					// Set the DataAccess.IsDirty() to true, so this will affect the real analysis when switching words.
					sda.SetMultiStringAlt(kSbWord, ktagSbWordForm, this.RawWordformWs, m_tssWordform);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, kSbWord, ktagSbWordForm, 0, 1, 1);
					// Just pretend the alternate wordform is our starting point.
					LoadRealDataIntoSec1(ref m_hvoWordform, out m_hvoWordform, kSbWord, fLookForDefaults, false);
					Debug.Assert(m_hvoWordform != 0);
				}
				//m_fForceReturnNewAnalysis = false;
				RootBox.Reconstruct();
			}
			finally
			{
				m_fSetWordformInProgress = false;
			}
		}

		/// <summary>
		/// Return the hvo best describing the state of the secondary cache for LexEntry fields
		/// (by MSA, LexSense, or LexEntry).
		/// </summary>
		internal int CurrentLexEntriesAnalysis(int hvoMorph)
		{
			CheckDisposed();

			// Return LexSense if found
			int hvoMorphSense = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
			if (hvoMorphSense > 0)
				return hvoMorphSense;
			// Return MSA if found
			int hvoMSA = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphPos);
			if (hvoMSA > 0)
				return hvoMSA;
			// Return LexEntry.
			int hvoMorphEntry = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphEntry);
			return hvoMorphEntry;
		}

		/// <summary>
		/// Get's the real lex senses for each current morph.
		/// </summary>
		/// <returns></returns>
		protected List<int> LexSensesForCurrentMorphs()
		{
			List<int> lexSensesForMorphs = new List<int>();

			int cmorphs = m_caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
			for (int i = 0; i < cmorphs; i++)
			{
				int hvoMorph = m_caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, i);
				int hvoMorphSense = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
				lexSensesForMorphs.Add(m_caches.RealHvo(hvoMorphSense));
			}
			return lexSensesForMorphs;
		}

		/// <summary>
		/// get the current dummy sandbox morphs
		/// </summary>
		/// <returns></returns>
		protected List<int> CurrentMorphs()
		{
			List<int> hvoMorphs = new List<int>();
			int cmorphs = m_caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
			for (int i = 0; i < cmorphs; i++)
			{
				int hvoMorph = m_caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, i);
				hvoMorphs.Add(hvoMorph);
			}
			return hvoMorphs;
		}

		// Erase all word level annotations.
		internal void ClearAllGlosses()
		{
			CheckDisposed();

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			IVwCacheDa cda = m_caches.DataAccess as IVwCacheDa;
			foreach (int wsId in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss, true))
			{
				ITsString tss;
				tss = tsf.MakeString("", wsId);
				cda.CacheStringAlt(kSbWord, ktagSbWordGloss, wsId, tss);
			}
		}

		/// <summary>
		/// indicates whether the sandbox is in a state that can or should be saved.
		/// </summary>
		/// <param name="fSaveGuess"></param>
		/// <returns></returns>
		internal bool ShouldSave(bool fSaveGuess)
		{
			return m_caches.DataAccess.IsDirty() || fSaveGuess && this.UsingGuess;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// NB: The hvo returned here may be for any one of these classes:
		/// 1. WfiWordform
		/// 2. WfiAnalysis, or
		/// 3. WfiGloss
		/// </remarks>
		public int GetRealAnalysis(bool fSaveGuess)
		{
			CheckDisposed();

			if (ShouldSave(fSaveGuess))
			{
				FinishUpOk();
				m_hvoAnalysis = CreateRealAnalysisMethod().Run();
				MarkAsInitialState();
			}
			return m_hvoAnalysis;
		}

		protected GetRealAnalysisMethod CreateRealWfiAnalysisMethod()
		{
			return new GetRealAnalysisMethod(
					this, m_caches, kSbWord, m_hvoWordform, GetWfiAnalysisHvoOfAnalysis(),
					m_hvoWordGloss, m_choices, m_tssWordform, true);
		}

		protected GetRealAnalysisMethod CreateRealAnalysisMethod()
		{
			return new GetRealAnalysisMethod(
					this, m_caches, kSbWord, m_hvoWordform, GetWfiAnalysisHvoOfAnalysis(),
					m_hvoWordGloss, m_choices, m_tssWordform, false);
		}

		protected virtual void LoadForWordBundleAnalysis(int hvoAnalysis)
		{
			m_hvoAnalysis = hvoAnalysis;
			LoadRealDataIntoSec(true, TreatAsSentenceInitial);
			Debug.Assert(m_hvoWordform != 0 || m_tssWordform != null);
			m_hvoWordformOriginal = m_hvoWordform;
		}

		#endregion Other methods

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_caches.MainCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			m_vc = new SandboxVc(m_caches, m_choices, IconsForAnalysisChoices, this);
			m_vc.ShowMorphBundles = m_fShowMorphBundles;
			m_vc.GuessColor = this.GuessColor;
			m_vc.BackColor = (int)CmObjectUi.RGB(this.BackColor);
			m_vc.IsMorphemeFormEditable = IsMorphemeFormEditable; // Pass through value to VC.

			m_rootb.DataAccess = m_caches.DataAccess;

			m_rootb.SetRootObject(kSbWord, m_vc, SandboxVc.kfragBundle, m_stylesheet);

			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			base.MakeRoot();
			// For some reason, we don't always initialize our control size to be the same as our rootbox.
			this.Margin = new Padding(3, 0, 3, 1);
			SyncControlSizeToRootBoxSize();
			if (RightToLeftWritingSystem)
				this.Anchor = AnchorStyles.Right | AnchorStyles.Top;

			//TODO:
			//ptmw->RegisterRootBox(qrootb);
		}

		private void SyncControlSizeToRootBoxSize()
		{
			if (this.Size.Width != RootBox.Width + Margin.Horizontal ||
				this.Size.Height != RootBox.Height + Margin.Vertical)
			{
				this.Size = new Size(RootBox.Width + Margin.Horizontal, RootBox.Height + Margin.Vertical);
			}
		}

		/// <summary>
		/// The SandBox is never the control that should print.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public override bool OnPrint(object args)
		{
			CheckDisposed();

			return false; // didn't handle it.
		}

		/// <summary>
		/// If sizing to content, a change of the size of the root box requires us to resize the window.
		/// </summary>
		/// <param name="prootb"></param>
		public override void RootBoxSizeChanged(IVwRootBox prootb)
		{
			CheckDisposed();

			if (!m_fSizeToContent)
			{
				base.RootBoxSizeChanged(prootb);
				return;
			}
			SyncControlSizeToRootBoxSize();
		}


		/// <summary>
		/// Handle a problem deleting some selection in the sandbox. So far, the only cases we
		/// handle are backspace and delete merging morphemes.
		/// Enhance JohnT: could also handle deleting a range that merges morphemes.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="dpt"></param>
		/// <returns></returns>
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			CheckDisposed();

			ITsString tss = null;
			bool fAssocPrev = false;
			int ichSel = -1;
			int hvoObj = 0;
			int tag = 0;
			int ws;
			sel.TextSelInfo(false, out tss, out ichSel, out fAssocPrev, out hvoObj, out tag, out ws);
			if (!m_morphManager.IsPropMorphBreak(hvoObj, tag, ws))
				return VwDelProbResponse.kdprFail;
			switch (dpt)
			{
				case VwDelProbType.kdptBsAtStartPara:
				case VwDelProbType.kdptBsReadOnly:
					return m_morphManager.HandleBackspace() ?
						VwDelProbResponse.kdprDone : VwDelProbResponse.kdprFail;
				case VwDelProbType.kdptDelAtEndPara:
				case VwDelProbType.kdptDelReadOnly:
					return m_morphManager.HandleDelete() ?
						VwDelProbResponse.kdprDone : VwDelProbResponse.kdprFail;
				default:
					return VwDelProbResponse.kdprFail;
			}
		}

		// Handles a change in the view selection.
		public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.SelectionChanged(rootb, vwselNew);
			if (!vwselNew.IsValid)
				return;
			m_morphManager.DoUpdates();
			if (!vwselNew.IsValid)
				return;
			DoActionOnIconSelection(vwselNew);
		}

		/// <summary>
		/// Tab should move between the piles inside the focus box!  See LT-9228.
		/// </summary>
		public override bool HandleTabAsControl
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// <summary>
		/// Calling it through xCore doesn't seem to work, maybe this isn't a target?
		/// Try forcing it this way.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if ((e.KeyCode == Keys.Down || e.KeyCode == Keys.Up) &&
				(e.Alt || IsIconSelected))
			{
				OnOpenCombo();
				e.Handled = true;
				return;
			}
			else if (e.KeyCode == Keys.Space && IsIconSelected)
			{
				OnOpenCombo();
				e.Handled = true;
				return;
			}
			else if (e.KeyCode == Keys.Tab && !e.Control && !e.Alt)
			{
				HandleTab(e.Shift);
				// skip base.OnKeyDown, so RootSite will not try to move our cursor
				// to another field in addition to HandleTab causing Tab to advance
				// past the expected icon/field.
				return;
			}
			else if (e.KeyCode == Keys.Enter)
			{
				if (e.Alt)
				{
					OnOpenCombo();
					e.Handled = true;
				}
				else if (InterlinDoc != null)
				{
					InterlinDoc.OnNextBundle(!e.Shift, false, !e.Control, true);
				}
				return;
			}
			else if (e.KeyCode == Keys.End)
			{
				if (HandleEndKey(e.Control))
					return;
			}
			else if (e.KeyCode == Keys.Home)
			{
				if (HandleHomeKey(e.Control))
					return;
			}
			else if (e.KeyCode == Keys.Right && !e.Control && !e.Shift)
			{
				if (HandleRightKey())
					return;
			}
			else if (e.KeyCode == Keys.Left && !e.Control && !e.Shift)
			{
				if (HandleLeftKey())
					return;
			}
			else if (e.KeyCode == Keys.Up && !e.Control && !e.Shift)
			{
				if (HandleUpKey())
					return;
			}
			else if (e.KeyCode == Keys.Down && !e.Control && !e.Shift)
			{
				if (HandleDownKey())
					return;
			}
			base.OnKeyDown(e);
		}

		/// <summary>
		/// This is overridden so that key presses on the word POS icon not only pull it down,
		/// but select the indicated item.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (IsWordPosIconSelected && !Char.IsControl(e.KeyChar))
			{
				OnOpenCombo();
				PopupTree tree = (m_ComboHandler as IhMissingWordPos).Tree;
				tree.SelectNodeStartingWith(Surrogates.StringFromCodePoint(e.KeyChar));
			}

			if (e.KeyChar == '\t' || e.KeyChar == '\r')
			{
				// gobble these up in Sandbox. so the base.OnKeyPress()
				// does not duplicate what we've handled in OnKeyDown().
				e.Handled = true;
				return;
			}
			else
			{
				base.OnKeyPress(e);
			}
		}

		/// <summary>
		/// Handle the mouse moving...remember where it was last in case it turns into a hover.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.X != m_LastMouseMovePos.X || e.Y != m_LastMouseMovePos.Y)
			{
				// Don't go through all the issues related to moving unless we're actually
				// moving!  See LT-1134 for what can happen.
				base.OnMouseMove(e);
			}
			// Save the position to be used by hover.
			m_LastMouseMovePos = new Point(e.X, e.Y);
			if (m_rootb == null)
				return;
#if TraceMouseCalls
			Debug.WriteLine("Sandbox.OnMouseMove(e.X,Y = {" + e.X +", " + e.Y + "})" +
				" - fInDrag = " + m_fInMouseDrag + ", fNewSel = " + m_fNewSelection);
#endif
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				Point pt = PixelToView(m_LastMouseMovePos);
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
#if TraceMouseCalls
				Debug.WriteLine("SandboxBase.OnMouseMove(" + m_LastMouseMovePos.ToString() + "): rcSrcRoot = " + rcSrcRoot.ToString() + ", rcDstRoot = " + rcDstRoot.ToString());
#endif
				IVwSelection vwsel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (vwsel != null)
				{
#if TraceMouseCalls
					Debug.WriteLine("SandboxBase.OnMouseMove(): pt = " + pt.ToString() + ", vwsel.SelType = " + vwsel.SelType.ToString());
#endif
					// If we're over one of the icons we want an arrow cursor, to indicate that
					// clicking will do something other than make a text selection.
					// Review: should we just have an arrow cursor everywhere in this view?
					if (vwsel.SelType == VwSelType.kstPicture)
						Cursor = Cursors.Arrow;
					// If we want hover effects and there isn't some editing in progress,
					// display a combo.
					if (ComboOnMouseHover && !m_fLockCombo)
						ShowComboForSelection(vwsel, false);
				}
#if TraceMouseCalls
				else
					Debug.WriteLine("SandboxBase.OnMouseMove(): pt = " + pt.ToString() + ", vwsel = null");
#endif
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process right mouse button down.  In particular, handle CTRL+Right Mouse Click.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns>true if handled, false otherwise</returns>
		/// -----------------------------------------------------------------------------------
		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			if (DataUpdateMonitor.IsUpdateInProgress(DataAccess))
				return true; //discard this event
			//			if ((ModifierKeys & Keys.Control) == Keys.Control)
			//			{
			if (m_rootb == null)
				return false;
			try
			{
				// Create a selection where we right clicked
				IVwSelection sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot,
					false);
				// Figure what property is selected and create a suitable class if
				// appropriate.  (CLevels includes the string property itself, but
				// AllTextSelInfo doesn't need it.)
				int cvsli = sel.CLevels(false) - 1;

				// Out variables for AllTextSelInfo.
				int ihvoRoot;
				int tagRightClickTextProp;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				// Main array of information retrived from sel that made combo.
				SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
					out ihvoRoot, out tagRightClickTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
				if (tagRightClickTextProp >= ktagMinIcon && tagRightClickTextProp < ktagLimIcon) // it's an icon
				{
					// don't bother doing anything for clicks on the icons.
					return false;
				}
				else
				{
					ITsString tss;
					int ichAnchorDum;
					int hvoRightClickObject = 0;
					sel.TextSelInfo(false, out tss, out ichAnchorDum, out fAssocPrev,
						out hvoRightClickObject, out tagRightClickTextProp, out ws);
					switch (tagRightClickTextProp)
					{
						case ktagSbMorphPrefix:
						case ktagSbMorphPostfix:
							m_hvoRightClickMorph = hvoRightClickObject;
							// Pretend we clicked on the morph form.  (See LT-7590.)
							hvoRightClickObject = Caches.DataAccess.get_ObjectProp(hvoRightClickObject, ktagSbMorphForm);
							break;
						case ktagSbNamedObjName:
							if (sel.CLevels(false) < 2)
								break;
							int hvoOuterObj, tagOuter, ihvoOuter, cpropPreviousOuter;
							IVwPropertyStore vpsDummy;
							sel.PropInfo(false, 1, out hvoOuterObj, out tagOuter, out ihvoOuter, out cpropPreviousOuter, out vpsDummy);
							if (tagOuter == ktagSbMorphGloss || tagOuter == ktagSbMorphPos || tagOuter == ktagSbMorphForm
								|| tagOuter == ktagSbMorphEntry)
							{
								m_hvoRightClickMorph = hvoOuterObj;
							}
							break;
						default:
							m_hvoRightClickMorph = 0;
							break;
					}

					int hvoReal = m_caches.RealHvo(hvoRightClickObject);
					if (hvoReal != 0)
					{
						//IxCoreColleague spellingColleague = null;
						if (tagRightClickTextProp == ktagSbWordGloss)
						{
							ContextMenuStrip menu = new ContextMenuStrip();
							EditingHelper.MakeSpellCheckMenuOptions(pt, m_rootb, rcSrcRoot, rcDstRoot, menu);
							if (menu.Items.Count > 0)
							{
								// Skip the normal menu items and display the spelling menu.
								menu.Show(this, pt);
								return true;
							}
							// This is an alternative approach, currently not fully implmented, which allows the spell check
							// menu items to be added to a menu that has further options.
							//spellingColleague = EditingHelper.MakeSpellCheckColleague(pt, m_rootb, rcSrcRoot, rcDstRoot);
						}
						if (HandleRightClickOnObject(hvoReal, null))
							return true;
					}
					else
					{
						return false;
					}
				}
			}
			catch (Exception)
			{
				throw;
			}
			//			}
			return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
		}

		public virtual bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			if (!m_fHandlingRightClickMenu)
				return false;
			XCore.Command cmd = (XCore.Command)commandObject;
			string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");

			// The menu item CmdPOSJumpToDefault is used in the Sandbox for jumping to morpheme POS,
			// and we don't want it to show up if we don't have a morpheme. But, although we don't
			// enable it (GetHvoForJumpToToolClass will return zero for class "PartOfSpeech" if there's no
			// morpheme), when clicking ON the POS for the word, the PartOfSpeechUi object will enable it.
			// So in this special case we have to claim to have handled the task but should NOT enable it.
			if (tool == "posEdit" && className == "PartOfSpeech" && m_hvoRightClickMorph == 0)
			{
				display.Visible = display.Enabled = false;
				return true;
			}
			if (m_hvoAnalysis > 0)
			{
				int hvo = GetHvoForJumpToToolClass(className);
				if (hvo != 0)
				{
					display.Visible = display.Enabled = true;
					return true;
				}
			}
			return false;
		}

		// This is common code for OnJumpToTool and OnDisplayJumpToTool, both called while displaying
		// a context menu and dealing with a menu item. ClassName is taken from the item XML
		// and indicates what sort of thing we want to jump to. Based on either information about what
		// was clicked, or general information about what the Sandbox is showing, figure out which
		// object we should jump to (if any) for this type of jump.
		private int GetHvoForJumpToToolClass(string className)
		{
			FdoCache cache = m_caches.MainCache;
			int clid = 0;
			if (m_hvoAnalysisGuess != 0)
				clid = cache.GetClassOfObject(m_hvoAnalysisGuess);
			switch (className)
			{
				case "WfiWordform":
					return m_hvoWordform;
				case "WfiAnalysis":
					{
						if (clid == WfiAnalysis.kclsidWfiAnalysis)
							return m_hvoAnalysisGuess;
						else if (clid == WfiGloss.kclsidWfiGloss)
							return cache.GetOwnerOfObjectOfClass(m_hvoAnalysisGuess, WfiAnalysis.kclsidWfiAnalysis);
					}
					break;
				case "WfiGloss":
					{
						if (clid == WfiGloss.kclsidWfiGloss)
							return m_hvoAnalysisGuess;
					}
					break;
				case "MoForm":
					return GetObjectFromRightClickMorph(ktagSbMorphForm);
				case "LexEntry":
					int result = GetObjectFromRightClickMorph(ktagSbMorphEntry);
					if (result != 0)
						return result;
					return GetMostPromisingEntry();
				case "LexSense":
					return GetObjectFromRightClickMorph(ktagSbMorphGloss);
				case "PartOfSpeech":
					int hvoMsa = GetObjectFromRightClickMorph(ktagSbMorphPos);
					if (hvoMsa == 0)
						return 0;
					CmObjectUi ui = CmObjectUi.MakeUi(m_caches.MainCache, hvoMsa);
					return ui.HvoForJumping(null);
				case "WordPartOfSpeech":
					int hvoRealAnalysis = 0;
					if (clid == WfiAnalysis.kclsidWfiAnalysis)
						hvoRealAnalysis = m_hvoAnalysisGuess;
					else if (clid == WfiGloss.kclsidWfiGloss)
						hvoRealAnalysis = cache.GetOwnerOfObjectOfClass(m_hvoAnalysisGuess, WfiAnalysis.kclsidWfiAnalysis);
					if (hvoRealAnalysis != 0) // JohnT: not sure it CAN be zero, but play safe.
						return cache.GetObjProperty(hvoRealAnalysis, (int)WfiAnalysis.WfiAnalysisTags.kflidCategory);
					break;
			}
			return 0;
		}

		/// <summary>
		/// Return the HVO of the most promising LexEntry to jump to. This is a fall-back when
		/// the user has not clicked on a morpheme.
		/// </summary>
		/// <returns></returns>
		private int GetMostPromisingEntry()
		{
			ITsString wordform = m_caches.DataAccess.get_MultiStringAlt(kSbWord, ktagSbWordForm, RawWordformWs);
			List<ILexEntry> homographs = LexEntry.CollectHomographs(m_caches.MainCache, wordform.Text, MoMorphType.kmtStem);
			if (homographs.Count == 0)
				return 0;
			else
				return homographs[0].Hvo; // arbitrarily pick first.
			// Enhance JohnT: possibly if there is more than one homograph we could try to match the word gloss
			// against one of its senses?
		}

		private int GetObjectFromRightClickMorph(int tag)
		{
			if (m_hvoRightClickMorph == 0)
				return 0;
			int hvoTarget = m_caches.DataAccess.get_ObjectProp(m_hvoRightClickMorph, tag);
			if (hvoTarget == 0)
				return 0;
			return m_caches.RealHvo(hvoTarget);
		}

		InterlinDocChild ParentDocChild
		{
			get
			{
				Control aParent = this.Parent;
				while (aParent != null)
				{
					if (aParent is InterlinDocChild)
						return aParent as InterlinDocChild;
					aParent = aParent.Parent;
				}
				return null;
			}
		}

		public virtual bool OnJumpToTool(object commandObject)
		{
			XCore.Command cmd = (XCore.Command)commandObject;
			string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			if (m_hvoAnalysis > 0)
			{
				// If the user selects a concordance on gloss or analysis, we want the current one,
				// not what we started with. We would save anyway as we switched views, so do it now.
				InterlinDocChild parent = ParentDocChild;
				if (parent != null)
					parent.UpdateRealFromSandbox();
				// This leaves the parent in a bad state, but maybe it would be good if all this is
				// happening in some other parent, such as the words analysis view?
				//m_hvoAnalysisGuess = GetRealAnalysis(false);
				int hvo = GetHvoForJumpToToolClass(className);
				if (hvo != 0)
				{
					FdoCache cache = m_caches.MainCache;
					m_mediator.PostMessage("FollowLink",
						SIL.FieldWorks.FdoUi.FwLink.Create(tool, cache.GetGuidFromId(hvo), cache.ServerName, cache.DatabaseName));
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Do nothing if the Sandbox somehow gets refreshed directly...its parent window destroys and
		/// recreates it.
		/// </summary>
		public override void RefreshDisplay()
		{
			CheckDisposed();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Simulate MouseDown on the rootbox, but allow selections in read-only text.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected override void CallMouseDown(Point point, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			m_fInMouseDrag = false;
			if (m_rootb != null)
			{
#if TraceMouseCalls
				Debug.WriteLine("Sandbox.CallMouseDown(pt = {"+ point.X +", "+ point.Y +"})" +
					" - fInDrag = " + m_fInMouseDrag + ", fNewSel = " + m_fNewSelection);
#endif
				m_wsPending = -1;
				try
				{
					m_rootb.MakeSelAt(point.X, point.Y, rcSrcRoot, rcDstRoot, true);
				}
				catch (Exception)
				{
					// Ignore not being able to make a selection.
				}
				EditingHelper.HandleMouseDown();
				m_fNewSelection = true;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Simulate MouseDownExtended on the rootbox, but allow selections in read-only text.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected override void CallMouseDownExtended(Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			if (m_rootb != null)
			{
#if TraceMouseCalls
				Debug.WriteLine("Sandbox.CallMouseDownExtended(pt = {"+pt.X+", "+pt.Y+"})" +
					" - fInDrag = " + m_fInMouseDrag + ", fNewSel = " + m_fNewSelection);
#endif
				m_wsPending = -1;
				IVwSelection vwsel = m_rootb.Selection;
				if (vwsel == null)
				{
					m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, true);
					EditingHelper.HandleMouseDown();
				}
				else
				{
					IVwSelection vwsel2 = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot,
						rcDstRoot, true);
					if (vwsel.SelType == vwsel2.SelType &&
						vwsel.SelType == VwSelType.kstText)
					{
						m_rootb.MakeRangeSelection(vwsel, vwsel2, true);
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseMoveDrag on the rootbox
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected override void CallMouseMoveDrag(Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			if (m_rootb != null && m_fMouseInProcess == false &&
				m_fMouseDownActivatedCombo == false)
			{
#if TraceMouseCalls
				Debug.WriteLine("Sandbox.CallMouseMoveDrag(pt = {"+ pt.X +", " + pt.Y +"})" +
					" - fInDrag = " + m_fInMouseDrag + ", fNewSel = " + m_fNewSelection);
#endif
				if (m_fNewSelection)
					m_fInMouseDrag = true;

				m_fMouseInProcess = true;

				// The VScroll is 'false' for the Sandbox now.  So the old code
				// is being removed.  It's very much like the SimpleRootSite code
				// and should be revisited if the sandbox ever gets a vertical
				// scroll bar.

				if (m_fNewSelection)
					CallMouseDownExtended(pt, rcSrcRoot, rcDstRoot);

				m_fMouseInProcess = false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseUp on the rootbox
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected override void CallMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (m_rootb != null && m_fInMouseDrag)
			{
#if TraceMouseCalls
				Debug.WriteLine("Sandbox.CallMouseUp(pt = {" + pt.X +", " + pt.Y + "})" +
					" - fInDrag = " + m_fInMouseDrag + ", fNewSel = " + m_fNewSelection);
#endif
				// if we're dragging to create or extend a selection
				// don't select text that is currently above or below current viewable area
				if (pt.Y < 0)  // if mouse is above viewable area
				{
					//m_rootb.MouseUp(pt.X, 0, rcSrcRoot, rcDstRoot);
					CallMouseDownExtended(new Point(pt.X, 0), rcSrcRoot, rcDstRoot);
				}
				else if (pt.Y > Bottom)  // if mouse is below viewable area
				{
					//m_rootb.MouseUp(pt.X, Bottom, rcSrcRoot, rcDstRoot);
					CallMouseDownExtended(new Point(pt.X, Bottom), rcSrcRoot, rcDstRoot);
				}
				else  // mouse is inside viewable area
				{
					//m_rootb.MouseUp(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
					CallMouseDownExtended(pt, rcSrcRoot, rcDstRoot);
				}
			}
			m_fInMouseDrag = false;
			m_fNewSelection = false;
			m_fMouseDownActivatedCombo = false;
		}

		public override int GetAvailWidth(IVwRootBox prootb)
		{
			CheckDisposed();
			// Displaying Right-To-Left Graphite behaves badly if available width gets up to
			// one billion (10**9) or so.  See LT-6077.  One million (10**6) should be ample
			// for simulating infinite width.
			if (m_fSizeToContent)
				return 10000000;	// return Int32.MaxValue / 2;
			else
				return base.GetAvailWidth(prootb);
		}

		// We absolutely don't ever want the Sandbox to scroll.
		public override void ScrollSelectionIntoView(IVwSelection sel, VwScrollSelOpts scrollOption)
		{
			CheckDisposed();
		}

		// We never want to change writing systems within the Sandbox.
		public override bool OnDisplayWritingSystemHvo(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			if (!Focused)
				return false;
			display.Enabled = false;
			return true;//we handled this, no need to ask anyone else.
		}

		#endregion Overrides of RootSite

		#region Nested Classes

		/// <summary>
		/// Determine the real analysis corresponding to the current state of the sandbox.
		/// The 'real analysis' may be a WfiWordform (if it hasn't been analyzed at all), a WfiAnalysis,
		/// or a WfiGloss.
		/// </summary>
		public class GetRealAnalysisMethod
		{
			protected CachePair m_caches;
			protected int m_hvoSbWord;
			int m_hvoWordform;
			int m_hvoWordGloss;
			protected SandboxBase m_sandbox; // The sandbox we're working from.

			protected int[] m_analysisMorphs;
			protected int[] m_analysisMsas;
			protected int[] m_analysisSenses;

			int m_hvoCategoryReal;
			protected int m_hvoWfiAnalysis;
			protected bool m_fWantOnlyWfiAnalysis;
			protected InterlinLineChoices m_choices;

			protected ISilDataAccess m_sda;
			protected ISilDataAccess m_sdaMain;
			protected int m_cmorphs;

			ITsString m_tssForm; // the form to use if we have to create a new WfiWordform.

			// These variables get filled in by CheckItOut. The Long message is suitable for a
			// MessageBox, the short one should fit in a status line. Currently always null.
			string m_LongMessage;
			string m_ShortMessage;

			/// <summary>
			/// Only used to make the UpdateRealAnalysisMethod construcotr happy. Do not use directly.
			/// </summary>
			public GetRealAnalysisMethod()
			{
			}

			public GetRealAnalysisMethod(SandboxBase owner, CachePair caches, int hvoSbWord,
				int hvoWordform, int hvoWfiAnalysis, int hvoWordGloss, InterlinLineChoices choices,
				ITsString tssForm, bool fWantOnlyWfiAnalysis)
			{
				m_sandbox = owner;
				m_caches = caches;
				m_hvoSbWord = hvoSbWord;
				m_hvoWordform = hvoWordform;
				m_hvoWfiAnalysis = hvoWfiAnalysis;
				m_hvoWordGloss = hvoWordGloss;
				m_sda = m_caches.DataAccess;
				m_sdaMain = m_caches.MainCache.MainCacheAccessor;
				m_cmorphs = m_sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				m_choices = choices;
				m_tssForm = tssForm;
				m_fWantOnlyWfiAnalysis = fWantOnlyWfiAnalysis;
			}

			public string ShortMessage
			{
				get { return m_ShortMessage; }
			}

			/// <summary>
			/// Run the algorithm, returning the 'analysis' hvo (WfiWordform, WfiAnalysis, or WfiGloss).
			/// </summary>
			/// <returns></returns>
			public int Run()
			{
				CheckItOut();
				if (m_LongMessage != null)
				{
					MessageBox.Show(m_LongMessage, ITextStrings.ksProblem);
					return 0;
				}
				return FinishItOff();
			}

			public void CheckItOut()
			{
				m_LongMessage = null;
				m_ShortMessage = null;
			}

			/// <summary>
			/// Do the bulk of the computation, everything after initial error checking, which is now nonexistent.
			/// </summary>
			/// <returns>HVO of analysis (WfiWordform, WfiAnalyis, or WfiGloss)</returns>
			private int FinishItOff()
			{
				FdoCache fdoCache = m_caches.MainCache;
				IWordformInventory wfi = fdoCache.LangProject.WordformInventoryOA;
				// Enhance JohnT: this detects temporary, dummy objects at present, but it would be better
				// to make an interface method on ISilDataAccess (or possibly IVwCacheDa) to test for dummy IDs.
				// Note that IsValidObject considers dummy IDs to be valid, so it won't work here.
				if (fdoCache.IsDummyObject(m_hvoWordform))
				{
					// The 'wordform' we were analyzing was a dummy one made by the parser. Now it needs to
					// become real.
					Debug.Fail("Any basic wordform in the FocusBox should now already be a real wordform.");
					// Note: We currently don't use OnRequestConversionToReal here because that will currently
					// only do the conversion if our DummyRecordList has been created.
					// The side effect is that our DummyRecordList will do a full reload if we are in the Words area.
					m_hvoWordform = (wfi as IDummy).ConvertDummyToReal(
						WordformInventory.ConcordanceWordformsFlid(fdoCache), m_hvoWordform).Hvo;
				}
				if (m_hvoWordform == 0)
				{
					// first see if we can find a matching form
					m_hvoWordform = wfi.GetWordformId(m_tssForm);
					// The user selected a case form that did not previously have a WfiWordform.
					// Since he is confirming this analysis, we now need to create one.
					// Note: if in context of the wordforms DummyRecordList, the RecordList is
					// smart enough to handle inserting one object without having to Reload the whole list.
					if (m_hvoWordform == 0)
						m_hvoWordform = wfi.AddRealWordform(m_tssForm).Hvo;
				}
				CleanupCurrentAnnotation();	// if user confirmed alternate case, we may need to readjust InstanceOf.
				// If sandbox contains only an empty morpheme string, don't consider this to be a true analysis.
				// Assume that the user was not finished with his analysis (cf. LT-1621).
				if (m_sandbox.IsAnalysisEmpty)
				{
					return m_hvoWordform;
				}

				// Update the wordform with any additional wss.
				List<int> wordformWss = m_choices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidWord, 0);
				// we need another way to detect the static ws for kflidWord.
				foreach (int wsId in wordformWss)
				{
					UpdateMlaIfDifferent(m_hvoSbWord, ktagSbWordForm, wsId, m_hvoWordform, (int)WfiWordform.WfiWordformTags.kflidForm);
				}

				// (LT-7807)
				// if we're in a special mode for adding monomorphemic words to lexicon
				// if we don't have a lexeme form match, create a new lex entry and sense OR
				// if we have a lexeme form, but no gloss match, create a new sense for the matching LexEntry OR
				// if we have a lexeme form and gloss match, just use it in the analysis.
				if (m_sandbox.ShouldAddWordGlossToLexicon)
				{
					IhMorphEntry.MorphItem matchingMorphItem = new IhMissingEntry.MorphItem(0, null);
					WfiWordform wf = new WfiWordform(fdoCache, m_hvoWordform);
					ITsString tssWf = wf.Form.GetAlternativeTss(m_sandbox.RawWordformWs);
					// go through the combo options for lex entries / senses to see if we can find any existing matches.
					int hvoSbMorph = m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, 0);
					using (SandboxBase.IhMorphEntry handler = InterlinComboHandler.MakeCombo(Sandbox.ktagWordGlossIcon, m_sandbox, hvoSbMorph) as SandboxBase.IhMorphEntry)
					{
						List<IhMorphEntry.MorphItem> morphItems = handler.MorphItems;
						// see if we can use an existing Sense, if it matches the word gloss and word MSA
						foreach (IhMorphEntry.MorphItem morphItem in morphItems)
						{
							// skip lex senses that do not match word gloss and pos in the Sandbox
							if (!SbWordGlossMatchesSenseGloss(morphItem))
								continue;
							if (!SbWordPosMatchesSenseMsaPos(morphItem))
								continue;
							// found a LexSense matching our Word Gloss and MSA POS
							matchingMorphItem = morphItem;
							break;
						}
						// if we couldn't use an existing sense but we match a LexEntry form,
						// add a new sense to an existing entry.
						ILexEntry bestEntry = null;
						if (morphItems.Count > 0 && matchingMorphItem.m_hvoSense == 0)
						{
							// Tried using FindBestLexEntryAmongstHomographs() but it matches
							// only CitationForm which MorphItems doesn't know anything about,
							// and doesn't match against Allomorphs which MorphItems do track
							// so this could lead to a crash (LT-9430).
							//
							// Solution: if the user specified a category, see if we can find an entry
							// with a sense using that same category
							// otherwise just add the new sense to the first entry in MorphItems.
							IhMorphEntry.MorphItem bestMorphItem = morphItems[0];
							foreach (IhMorphEntry.MorphItem morphItem in morphItems)
							{
								// skip items that do not match word main pos in the Sandbox
								if (!SbWordMainPosMatchesSenseMsaMainPos(morphItem))
									continue;
								bestMorphItem = morphItem;
								break;
							}

							int hvoEntry = bestMorphItem.GetPrimaryOrOwningEntry(m_caches.MainCache);
							if (hvoEntry != 0)
								bestEntry = LexEntry.CreateFromDBObject(m_caches.MainCache, hvoEntry);
							// lookup this entry;
							matchingMorphItem = FindLexEntryMorphItem(morphItems, bestEntry);
						}

						if (matchingMorphItem.m_hvoMorph == 0)
						{
							// we didn't find a matching lex entry, so create a new entry
							ILexEntry newEntry;
							ILexSense newSense;
							IMoForm allomorph;
							handler.CreateNewEntry(true, out newEntry, out allomorph, out newSense);
						}
						else if (bestEntry != null)
						{
							// we found matching lex entry, so create a new sense for it
							ILexSense newSense = LexSense.CreateSense(bestEntry, new DummyGenericMSA(), "");
							// copy over any word glosses we're showing.
							foreach (int wsId in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
							{
								UpdateMlaIfDifferent(m_hvoSbWord, ktagSbWordGloss, wsId, newSense.Hvo, (int)LexSense.LexSenseTags.kflidGloss);
							}
							// copy over the Word POS
							(newSense.MorphoSyntaxAnalysisRA as MoStemMsa).PartOfSpeechRAHvo =
								m_caches.RealHvo(m_sda.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));
							handler.UpdateMorphEntry(matchingMorphItem.m_hvoMorph, bestEntry.Hvo, newSense.Hvo);
						}
						else
						{
							// we found a matching lex entry and sense, so select it.
							int iMorphItem = morphItems.IndexOf(matchingMorphItem);
							handler.HandleSelect(iMorphItem);
						}
					}
				}

				BuildMorphLists(); // Used later on in the code.
				m_hvoCategoryReal = m_caches.RealHvo(m_sda.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));

				// We may need to create a new WfiAnalysis based on whether we have any sandbox gloss content.
				bool fNeedGloss = false;
				bool fWordGlossLineIsShowing = false; // Set to 'true' if the wrod gloss line is included in the m_choices fields.
				foreach (InterlinLineSpec ilc in m_choices)
				{
					if (ilc.Flid == InterlinLineChoices.kflidWordGloss)
					{
						fWordGlossLineIsShowing = true;
						break;
					}
				}
				if (fWordGlossLineIsShowing)
				{
					// flag that we need to create wfi gloss information if any configured word gloss lines have content.
					foreach (int wsId in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
					{
						if (m_sda.get_MultiStringAlt(m_hvoSbWord, ktagSbWordGloss, wsId).Length > 0)
						{
							fNeedGloss = true;
							break;
						}
					}
				}

				// We decided to take this logic out (see LT-1653) because it is annoying when
				// deliberately removing a wrong morpheme breakdown guess.
				//				// We need to create (or find an existing) WfiAnalysis if we have any morphemes,
				//				// a word gloss, or a word category.
				//				bool fNeedAnalysis = fNeedGloss || m_cmorphs > 1 || m_hvoCategoryReal != 0;
				//
				//				// If we have exactly one morpheme, see if it has some non-trivial information
				//				// associated. If not, don't make an analysis.
				//				if (!fNeedGloss && m_cmorphs == 1 && m_hvoCategoryReal == 0 && m_analysisMsas[0] == 0
				//					&& m_analysisSenses[0] == 0 && m_analysisMorphs[0] == 0)
				//				{
				//					fNeedAnalysis = false;
				//				}
				//				// If there's no information at all, the 'analysis' is just the original wordform.
				//				if (!fNeedAnalysis)
				//					return m_hvoWordform;
				// OK, we have some information that corresponds to an analysis. Find or create
				// an analysis that matches.
				int wsVern = m_sandbox.RawWordformWs;
				m_hvoWfiAnalysis = FindMatchingAnalysis(true);
				bool fFoundAnalysis = m_hvoWfiAnalysis != 0;
				if (!fFoundAnalysis)
				{
					// Clear the checksum on the wordform. This allows the parser filer to re-evaluate it and
					// delete the old analysis if it is just a simpler, parser-generated form of the one we're now making.
					m_sdaMain.SetInt(m_hvoWordform, (int)WfiWordform.WfiWordformTags.kflidChecksum, 0);
					// Check whether there's a parser-generated analysis that the current settings
					// subsume.  If so, reuse that analysis by filling in the missing data (word gloss,
					// word category, and senses).
					int hvoPartialWfiAnal = FindMatchingAnalysis(false);
					bool fNewAnal = hvoPartialWfiAnal == 0;
					if (fNewAnal)
					{
						// Create one.
						m_hvoWfiAnalysis = m_sdaMain.MakeNewObject(WfiAnalysis.kclsidWfiAnalysis, m_hvoWordform,
							(int)WfiWordform.WfiWordformTags.kflidAnalyses, -1);
						// Use the following instead of -1 if the collection is changed to a sequence.
						//	m_sdaMain.get_VecSize(m_hvoWordform, (int)WfiWordform.WfiWordformTags.kflidAnalyses));
					}
					else
					{
						m_hvoWfiAnalysis = hvoPartialWfiAnal;
					}
					m_sdaMain.SetObjProp(m_hvoWfiAnalysis, (int)WfiAnalysis.WfiAnalysisTags.kflidCategory, m_hvoCategoryReal);
					for (int imorph = 0; imorph < m_cmorphs; imorph++)
					{
						int hvoMb;
						if (fNewAnal)
						{
							hvoMb = m_sdaMain.MakeNewObject(WfiMorphBundle.kclsidWfiMorphBundle, m_hvoWfiAnalysis,
								(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, imorph);
						}
						else
						{
							hvoMb = m_sdaMain.get_VecItem(m_hvoWfiAnalysis,
								(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, imorph);
						}
						// An undo operation can leave stale information in the sandbox.  If
						// that happens, the stored database id values are invalid.  Set them
						// all to zero if the morph is invalid.  (See LT-3824 for a crash
						// scenario.)  This fix prevents a crash, but doesn't do anything for
						// restoring the original values before the operation that is undone.
						if (m_analysisMorphs[imorph] != 0 &&
							!m_sdaMain.get_IsValidObject(m_analysisMorphs[imorph]))
						{
							m_analysisMorphs[imorph] = 0;
							m_analysisMsas[imorph] = 0;
							m_analysisSenses[imorph] = 0;
						}
						// Set the Morph of the bundle if we know a real one; otherwise, just set its Form
						if (m_analysisMorphs[imorph] == 0)
						{
							int hvoSbMorph = m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
							m_sdaMain.SetMultiStringAlt(hvoMb,
								(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm, wsVern,
								m_sandbox.GetFullMorphForm(hvoSbMorph));
							// Copy any other wss over, without any funny business about morpheme breaks
							foreach (int ws in m_choices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidMorphemes, 0))
							{
								m_sdaMain.SetMultiStringAlt(hvoMb,
									(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm, ws,
									m_caches.DataAccess.get_MultiStringAlt(hvoSbMorph, ktagSbNamedObjName, ws));
							}
						}
						else
						{
							m_sdaMain.SetObjProp(hvoMb, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph, m_analysisMorphs[imorph]);
						}
						// Set the MSA if we have one. Note that it is (pathologically) possible that the user has done
						// something in another window to destroy the MSA we remember, so don't try to set it if so.
						if (m_analysisMsas[imorph] != 0 && m_sdaMain.get_IsValidObject(m_analysisMsas[imorph]))
						{
							m_sdaMain.SetObjProp(hvoMb, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa, m_analysisMsas[imorph]);
						}
						// Likewise the Sense
						if (m_analysisSenses[imorph] != 0)
						{
							m_sdaMain.SetObjProp(hvoMb, (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense, m_analysisSenses[imorph]);
						}
					}
				}
				else if (fWordGlossLineIsShowing) // If the line is not showing at all, don't bother.
				{
					// (LT-1428) Since we're using an existing WfiAnalysis,
					// We will find or create a new WfiGloss (even for blank lines)
					// if WfiAnalysis already has WfiGlosses
					//	or m_hvoWordGloss is nonzero
					//	or Sandbox has gloss content.
					bool fSbGlossContent = fNeedGloss;
					int cGloss = m_sdaMain.get_VecSize(m_hvoWfiAnalysis,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMeanings);
					fNeedGloss = cGloss > 0 || m_hvoWordGloss != 0 || fSbGlossContent;
				}

				if (m_hvoWfiAnalysis != 0)
					EnsureCorrectMorphForms();

				if (!fNeedGloss || m_fWantOnlyWfiAnalysis)
					return m_hvoWfiAnalysis;

				if (m_hvoWordGloss != 0)
				{
					// We may consider editing it instead of making a new one.
					// But ONLY if it belongs to the right analysis!!
					if (fdoCache.GetOwnerOfObject(m_hvoWordGloss) != m_hvoWfiAnalysis)
						m_hvoWordGloss = 0;
				}
				/* These are the types of things we are trying to accomplish here.
				Problem 1 -- Correcting a spelling mistake.
					Gloss1: mn <-
				User corrects spelling to men
				Desired:
					Gloss1: men <-
				Bad result:
					Gloss1: mn
					Gloss2: men <-

				Problem 2 -- Switching to a different gloss via typing.
					Gloss1: he <-
					Gloss2: she
				User types in she rather than using dropdown box to select it
				Desired:
					Gloss1: he
					Gloss2: she <-
				Bad result:
					Gloss1: she <-
					Gloss2: she

				Problem 2A
							Gloss1: he <-
				User types in she without first using dropdown box to select "add new gloss"
				Desired:
							Gloss1: he (still used for N other occurrences)
							Gloss2: she <-
				Bad (at least dubious) result:
							Gloss1: she <- (used for this and all other occurrences)

				Problem 3 -- Adding a missing alternative when there are not duplications.
					Gloss1: en=green <-
				User adds the French equivalent
				Desired:
					Gloss1: en=green, fr=vert <-
				Bad result:
					Gloss1: en=green
					Gloss2: en=green, fr=vert <-

				The logic used to be to look for glosses with all alternatives matching or else it
				creates a new one. So 2 would actually work, but 1 and 3 were both bad.

				New logic: keep track of the starting WfiAnalysis and WfiGloss.
				Assuming we haven't changed to a new WfiAnalysis based on other changes, if there
				is a WfiGloss that matches any of the existing alternatives, we switch to that.
				Otherwise we set the alternatives of the starting WfiGloss to whatever the user
				entered. This logic would work for all three examples above, but has problems
				with the following.

				Problem -- add a missing gloss where there are identical glosses in another ws.
					Gloss1: en=them <-
				User adds Spanish gloss
				Desired:
					Gloss1: en=them, es=ellas <-
				This works ok with above logic. But now in another location the user needs to
				use the masculine them in Spanish, so changes ellas to ellos
				Desired:
					Gloss1: en=them, es=ellas
					Gloss2: en=them, es=ellos <-
				Bad result:
					Gloss1: en=them, es=ellos <-

				Eventually, we'll probably want to display a dialog box to ask users what they really want.
				"There are 15 places where "XXX" analyzed as 3pp is currently glossed
				en->"them".  Would you like to
				<radio button, selected> change them all to en->"them" sp->"ellas"?
				<radio button> leave the others glossed en->"them" and let just this one
				be en->"them" sp->"ellas"?
				<radio button> see a concordance and choose which ones to change to
				en->"them" sp->"ellas"?
				*/

				// (LT-1428)
				// -----------------------------
				// When the user edits a gloss,
				// (1) If there is an existing gloss matching what they just changed it to
				//		then switch this instance to point to that one.
				// (2) Else if the gloss is used only in this instance
				//		then apply the edits directly to the gloss.
				// (3) Else, create a new gloss.
				//-------------------------------
				int hvoGloss = fFoundAnalysis ? FindMatchingGloss() : 0;

				if (hvoGloss == 0 && m_sandbox.WordGlossReferenceCount == 1)
				{
					hvoGloss = m_hvoWordGloss; // update the existing gloss.
				}

				if (hvoGloss == 0)
				{
					// Create one.
					hvoGloss = m_sdaMain.MakeNewObject(WfiGloss.kclsidWfiGloss, m_hvoWfiAnalysis,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMeanings, -1);
					// use the following if changed to sequence.
					//	m_sdaMain.get_VecSize(m_hvoWfiAnalysis, (int)WfiAnalysis.WfiAnalysisTags.kflidMeanings));

				}
				foreach (int wsId in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
				{
					ITsString tssGloss = m_sda.get_MultiStringAlt(m_hvoSbWord, ktagSbWordGloss, wsId);
					if (!tssGloss.Equals(m_sdaMain.get_MultiStringAlt(hvoGloss,
						(int)WfiGloss.WfiGlossTags.kflidForm, wsId)))
					{
						m_sdaMain.SetMultiStringAlt(hvoGloss, (int)WfiGloss.WfiGlossTags.kflidForm,
							wsId, tssGloss);
					}
				}
				return hvoGloss;
			}

			/// <summary>
			/// Find the morph item referring to the LexEntry (only), not
			/// a sense or msa.
			/// </summary>
			/// <param name="morphItems"></param>
			/// <param name="leTarget"></param>
			/// <returns></returns>
			private static IhMorphEntry.MorphItem FindLexEntryMorphItem(List<IhMorphEntry.MorphItem> morphItems, ILexEntry leTarget)
			{
				if (leTarget != null)
				{
					foreach (IhMorphEntry.MorphItem mi in morphItems)
					{
						if (mi.m_hvoSense == 0 && mi.m_hvoMorph != 0)
						{
							// return the
							int hvoEntryCandidate = mi.GetPrimaryOrOwningEntry(leTarget.Cache);
							if (hvoEntryCandidate == leTarget.Hvo)
								return mi;
						}
					}
				}
				return new IhMissingEntry.MorphItem(0, null);
			}

			private bool SbWordPosMatchesSenseMsaPos(IhMorphEntry.MorphItem morphItem)
			{
				int hvoWordPos = m_caches.RealHvo(m_sda.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));
				// currently only support MoStemMsa, since that is what a WordPos expects to match against.
				if (morphItem.m_hvoMsa != 0)
				{
					IMoStemMsa msa = CmObject.CreateFromDBObject(m_caches.MainCache, morphItem.m_hvoMsa) as IMoStemMsa;
					return msa != null && msa.PartOfSpeechRAHvo == hvoWordPos;
				}
				return hvoWordPos == 0;
			}

			/// <summary>
			/// see if the MainPossibilities match for the given morphItem and
			/// the Word Part of Speech in the sandbox
			/// </summary>
			/// <param name="morphItem"></param>
			/// <returns></returns>
			private bool SbWordMainPosMatchesSenseMsaMainPos(IhMorphEntry.MorphItem morphItem)
			{
				int hvoWordPos = m_caches.RealHvo(m_sda.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));
				int hvoMainPosCandidate = 0;
				int hvoMainPosTarget = 0;
				// currently only support MoStemMsa, since that is what a WordPos expects to match against.
				IPartOfSpeech posCandidate = null;
				if (morphItem.m_hvoMsa != 0)
				{
					IMoStemMsa msa = CmObject.CreateFromDBObject(m_caches.MainCache, morphItem.m_hvoMsa) as IMoStemMsa;
					if (msa != null && msa.PartOfSpeechRA != null)
					{
						posCandidate = msa.PartOfSpeechRA;
						ICmPossibility mainPosCandidate = posCandidate.MainPossibility;
						if (mainPosCandidate != null)
							hvoMainPosCandidate = mainPosCandidate.Hvo;
					}
				}
				if (hvoWordPos != 0)
				{
					IPartOfSpeech targetPos = PartOfSpeech.CreateFromDBObject(m_caches.MainCache, hvoWordPos);
					if (targetPos != null)
					{
						ICmPossibility mainPosTarget = targetPos.MainPossibility;
						if (mainPosTarget != null)
							hvoMainPosTarget = mainPosTarget.Hvo;
					}
				}
				return hvoMainPosCandidate == hvoMainPosTarget;
			}

			private bool SbWordGlossMatchesSenseGloss(IhMorphEntry.MorphItem morphItem)
			{
				if (morphItem.m_hvoSense <= 0)
					return false;
				// compare our gloss information.
				List<int> wordGlossWss = m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss);
				foreach (int wsId in wordGlossWss)
				{
					if (!IsMlSame(m_hvoSbWord, ktagSbWordGloss, wsId, morphItem.m_hvoSense, (int)LexSense.LexSenseTags.kflidGloss))
					{
						// the sandbox word gloss differs from the sense gloss, so go to the next morphItem.
						return false;
					}
				}
				return true;
			}

			protected void BuildMorphLists()
			{
				// Build lists of morphs, msas, and senses, that we can use in subsequent code.
				m_analysisMorphs = new int[m_cmorphs];
				m_analysisMsas = new int[m_cmorphs];
				m_analysisSenses = new int[m_cmorphs];
				for (int imorph = m_cmorphs; --imorph >= 0; )
				{
					int hvoMorph = m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
					int hvoMorphForm = m_sda.get_ObjectProp(hvoMorph, ktagSbMorphForm);
					m_analysisMorphs[imorph] = m_caches.RealHvo(hvoMorphForm);
					m_analysisMsas[imorph] = m_caches.RealHvo(m_sda.get_ObjectProp(hvoMorph, ktagSbMorphPos));
					m_analysisSenses[imorph] = m_caches.RealHvo(m_sda.get_ObjectProp(hvoMorph, ktagSbMorphGloss));
				}
			}

			/// <summary>
			/// Check to see if we need to readjust the current annotation wordform reference.
			/// </summary>
			private void CleanupCurrentAnnotation()
			{
				int hvoAnnotation = m_sandbox.HvoAnnotation;
				if (hvoAnnotation == 0)
					return; // Nothing to do if there is no annotation.

				FdoCache fdoCache = m_caches.MainCache;
				int hvoRealWordform = m_hvoWordform; // should be real by now.
				Debug.Assert(fdoCache.IsRealObject(hvoRealWordform, (int)WfiWordform.kClassId));
				ISilDataAccess sda = fdoCache.MainCacheAccessor;
				int kflidOccurrences = WfiWordform.OccurrencesFlid(fdoCache);
				IVwCacheDa cda = fdoCache.VwCacheDaAccessor;
				int hvoWordformOld = sda.get_ObjectProp(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf);
				if (fdoCache.IsDummyObject(hvoWordformOld))
				{
					Debug.Assert(fdoCache.GetClassOfObject(hvoWordformOld) == WfiWordform.kclsidWfiWordform);
					// If the current annotation is pointing at a dummy wordform reference, we need to remove this annotation
					// from that wordform reference, since this annotation has become disassociated from that wordform.
					//					// 1) Delete the current annotation from the referenced wordform.
					//					List<int> occurrencesList = new List<int>(fdoCache.GetVectorProperty(hvoWordformOld, kflidOccurrences, true));
					//					int iAnn = occurrencesList.IndexOf(m_sandbox.HvoAnnotation);
					//					if (iAnn >= 0)
					//					{
					//						cda.CacheReplace(hvoWordformOld, kflidOccurrences, iAnn, iAnn + 1, new int[0], 0); // delete annotation.
					//						sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoWordformOld, kflidOccurrences, iAnn, 0, 1);
					//					}
					// 2) Then modify the current annotation so that it becomes an InstanceOf the new real case selection.
					cda.CacheObjProp(hvoAnnotation, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, hvoRealWordform);
					// This is value is going to get overwritten in UpdateRealFromSandbox(), but we'll do a prop change here also just to be safe.
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoAnnotation, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, 0, 1, 1);
				}
			}

			/// <summary>
			/// Ensure that the specified writing system of property flidDest in object hvoDst in m_sdaMain
			/// is the same as property flidSrc in object hvoSrc in m_sda. If not, update and issue a
			/// PropChanged.
			/// </summary>
			/// <param name="hvoSrc"></param>
			/// <param name="flidSrc"></param>
			/// <param name="wsId"></param>
			/// <param name="hvoDst"></param>
			/// <param name="flidDest"></param>
			void UpdateMlaIfDifferent(int hvoSrc, int flidSrc, int wsId, int hvoDst, int flidDest)
			{
				ITsString tss;
				ITsString tssOld;
				if (IsMlSame(hvoSrc, flidSrc, wsId, hvoDst, flidDest, out tss, out tssOld))
					return;
				m_sdaMain.SetMultiStringAlt(hvoDst, flidDest, wsId, tss);
				m_sdaMain.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoDst,
					flidDest, wsId, 0, 0);
			}

			private bool IsMlSame(int hvoSrc, int flidSrc, int wsId, int hvoDst, int flidDest)
			{
				ITsString tss;
				ITsString tssOld;
				return IsMlSame(hvoSrc, flidSrc, wsId, hvoDst, flidDest, out tss, out tssOld);
			}
			private bool IsMlSame(int hvoSrc, int flidSrc, int wsId, int hvoDst, int flidDest, out ITsString tss, out ITsString tssOld)
			{
				tss = m_sda.get_MultiStringAlt(hvoSrc, flidSrc, wsId);
				tssOld = m_sdaMain.get_MultiStringAlt(hvoDst, flidDest, wsId);
				return tss.Equals(tssOld);
			}

			/// <summary>
			/// m_hvoAnalysis is the selected analysis. However, we did not consider writing systems
			/// of the morpheme line except the default vernacular one in deciding to use it.
			/// If additional WS information has been supplied, save it.
			/// </summary>
			void EnsureCorrectMorphForms()
			{
				List<int> otherWss = m_choices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidMorphemes, 0);
				foreach (int wsId in otherWss)
				{
					for (int imorph = 0; imorph < m_cmorphs; imorph++)
					{
						int hvoMb = m_sdaMain.get_VecItem(m_hvoWfiAnalysis,
							(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, imorph);
						int hvoSbMorph = m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
						int hvoSecMorph = m_sda.get_ObjectProp(hvoSbMorph, ktagSbMorphForm);

						if (m_analysisMorphs[imorph] == 0)
						{
							// We have no morph, set it on the WfiMorphBundle.
							UpdateMlaIfDifferent(hvoSecMorph, ktagSbNamedObjName, wsId, hvoMb, (int)WfiMorphBundle.WfiMorphBundleTags.kflidForm);
						}
						else
						{
							// Set it on the MoForm.
							UpdateMlaIfDifferent(hvoSecMorph, ktagSbNamedObjName, wsId, m_analysisMorphs[imorph], (int)MoForm.MoFormTags.kflidForm);
						}
					}
				}
			}

			/// <summary>
			/// Find one of the WfiGlosses of m_hvoWfiAnalysis where the form matches for each analysis writing system.
			/// Review: We probably want to find a WfiGloss that matches any (non-zero length) alternative since we don't want to
			/// force the user to type in every alternative each time they enter a gloss.
			/// Otherwise, if we match one with all non-zero length, return that one. (LT-1428)
			/// </summary>
			/// <returns></returns>
			public int FindMatchingGloss()
			{
				int cGloss = m_sdaMain.get_VecSize(m_hvoWfiAnalysis,
					(int)WfiAnalysis.WfiAnalysisTags.kflidMeanings);
				for (int i = 0; i < cGloss; ++i)
				{
					int hvoPossibleGloss = m_sdaMain.get_VecItem(m_hvoWfiAnalysis,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMeanings, i);
					bool fAlternativeFound = false; // OK if any gloss alternative matches.
					bool fAllBlankMatched = true; // True until we find a non-blank line
					foreach (int wsId in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
					{
						ITsString tssGloss = m_sda.get_MultiStringAlt(m_hvoSbWord, ktagSbWordGloss, wsId);
						ITsString tssMainGloss = m_sdaMain.get_MultiStringAlt(hvoPossibleGloss,
							(int)WfiGloss.WfiGlossTags.kflidForm, wsId);
						string sdaGloss = tssGloss.Text;
						string smainGloss = tssMainGloss.Text;

						if (tssGloss.Length != 0 || tssMainGloss.Length != 0)
						{
							fAllBlankMatched = false;
						}
						if (tssGloss.Length > 0 && tssGloss.Equals(tssMainGloss))
						{
							fAlternativeFound = true;
							break;
						}
					}
					if (fAlternativeFound || fAllBlankMatched)
						return hvoPossibleGloss;
				}
				return 0;
			}

			/// <summary>
			/// Find the analysis that matches the info in the secondary cache.
			/// Optimize JohnT: there may be a more efficient way to do this using sql.
			/// </summary>
			/// <param name="fExactMatch"></param>
			/// <returns></returns>
			public int FindMatchingAnalysis(bool fExactMatch)
			{
				int cAnalysis = m_sdaMain.get_VecSize(m_hvoWordform, (int)WfiWordform.WfiWordformTags.kflidAnalyses);
				for (int ia = 0; ia < cAnalysis; ++ia)
				{
					int hvoPossibleAnalysis = m_sdaMain.get_VecItem(m_hvoWordform,
						(int)WfiWordform.WfiWordformTags.kflidAnalyses, ia);
					if (fExactMatch)
					{
						if (CheckAnalysis(hvoPossibleAnalysis, true))
							return hvoPossibleAnalysis;
					}
					else
					{
						// If this possibility is Human evaluated, it must match exactly regardless
						// of the input parameter.
						int cEval = 0;
						string sSql = "SELECT COUNT(*) FROM CmAgentEvaluation e " +
							"JOIN CmAgent_Evaluations ce ON ce.Dst=e.Id " +
							"JOIN CmAgent a on a.Id=ce.Src " +
							"WHERE e.Target=? AND a.Human<>0";
						DbOps.ReadOneIntFromCommand(m_caches.MainCache, sSql,
							hvoPossibleAnalysis, out cEval);
						if (CheckAnalysis(hvoPossibleAnalysis, cEval != 0))
							return hvoPossibleAnalysis;
					}
				}
				return 0; // no match found.
			}

			/// <summary>
			/// Evaluate the given possible analysis to see whether it matches the current Sandbox data.
			/// Review: This is not testing word gloss at all. Is this right?
			/// </summary>
			/// <param name="hvoPossibleAnalysis"></param>
			/// <param name="fExactMatch"></param>
			/// <returns></returns>
			private bool CheckAnalysis(int hvoPossibleAnalysis, bool fExactMatch)
			{
				// First, check that the analysis has the right word category.
				int hvoWordCat = m_sdaMain.get_ObjectProp(hvoPossibleAnalysis,
					(int)WfiAnalysis.WfiAnalysisTags.kflidCategory);
				bool fCheck = fExactMatch || hvoWordCat != 0;
				if (fCheck && m_hvoCategoryReal != hvoWordCat)
				{
					return false;
				}
				// Next, it must at least have the right number of morphemes.
				int cmorphs = m_sdaMain.get_VecSize(hvoPossibleAnalysis,
					(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles);
				if (cmorphs != m_cmorphs)
					return false;
				// Each morpheme must have the right data.
				for (int imorph = 0; imorph < cmorphs; imorph++)
				{
					int hvoMb = m_sdaMain.get_VecItem(hvoPossibleAnalysis,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, imorph);
					// the morpheme must have the right MSA.
					int hvoMsa = m_sdaMain.get_ObjectProp(hvoMb,
						(int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa);
					if (hvoMsa != m_analysisMsas[imorph])
						return false;
					// and also the right sense
					int hvoSense = m_sdaMain.get_ObjectProp(hvoMb,
						(int)WfiMorphBundle.WfiMorphBundleTags.kflidSense);
					fCheck = fExactMatch || hvoSense != 0;
					if (fCheck && hvoSense != m_analysisSenses[imorph])
						return false;
					// and finally the right form...either by pointing to a MoForm in its Morph property,
					// or by actually storing the string in its Form property.
					if (m_analysisMorphs[imorph] == 0)
					{
						// The form of the morph bundle must match the name of the dummy MoForm object.
						// No to this blocked line, since this gets a ts string with a null underlying string, as it is not in the cache with the '0' hvo.
						// This was the source of the multiple duplicate analyses in LT-837 "IText creating identical parses?"
						// ITsString tssForm = m_sda.get_StringProp(m_analysisMorphs[imorph], ktagSbNamedObjName);
						// The fix for LT-837 is to get the string for the real dummy hvo.
						int hvoMorph = m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
						ITsString tssForm = m_sandbox.GetFullMorphForm(hvoMorph);
						ITsString tssMb = m_sdaMain.get_MultiStringAlt(hvoMb,
							(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm,
							m_sandbox.RawWordformWs);
						if (!tssForm.Equals(tssMb))
						{
							return false;
						}
						// Enhance JohnT: if we are showing other alternatives of the moform, must they match too?
						// My inclination is, not.
					}
					else
					{
						int hvoMorph = m_sdaMain.get_ObjectProp(hvoMb,
							(int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph);
						if (hvoMorph != m_analysisMorphs[imorph])
							return false;
					}
				}
				return true;
			}
		}

		/// <summary>
		/// Analyze the string. We're looking for something like un- except -ion -al -ly.
		/// The one with spaces on both sides is a root, the others are prefixes or suffixes.
		/// Todo WW(JohnT): enhance to handle other morpheme break characters.
		/// Todo WW (JohnT): enhance to look for trailing ;POS and handle appropriately.
		/// </summary>
		public class MorphemeBreaker
		{
			string m_input; // string being processed into morphemes.
			ISilDataAccess m_sda; // cache to update with new objects etc (m_caches.DataAccess).
			IVwCacheDa m_cda; // another interface on same cache.
			CachePair m_caches; // Both the caches we are working with.
			int m_hvoSbWord; // HVO of the Sandbox word that will own the new morphs
			int m_cOldMorphs;
			int m_cNewMorphs;
			ITsStrFactory m_tsf = TsStrFactoryClass.Create();
			int m_wsVern = 0;
			MoMorphTypeCollection m_types;
			int m_imorph = 0;
			SandboxBase m_sandbox;

			// These variables are used to re-establish a selection in the morpheme break line
			// after rebuilding the morphemes.
			int m_ichSelInput; // The character position in m_input where we want the selection.
			int m_tagSel = -1; // The property we want the selection in (or -1 for none).
			int m_ihvoSelMorph; // The index of the morpheme we want the selection in.
			int m_ichSelOutput; // The character offset where we want the selection to be made.
			int m_cchPrevMorphemes; // Total length of morphemes before m_imorph.

			public MorphemeBreaker(CachePair caches, string input, int hvoSbWord, int wsVern,
				SandboxBase sandbox)
			{
				m_caches = caches;
				m_sda = caches.DataAccess;
				m_cda = (IVwCacheDa)m_sda;
				m_input = input;
				m_hvoSbWord = hvoSbWord;
				m_cOldMorphs = m_sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				ITsStrFactory m_tsf = TsStrFactoryClass.Create();
				m_wsVern = wsVern;
				m_types = new MoMorphTypeCollection(m_caches.MainCache);
				m_sandbox = sandbox;
			}

			/// <summary>
			/// Should be called with a non-empty sequence of characters from m_ichStartMorpheme
			/// to m_ich as a morpheme.
			/// </summary>
			/// <param name="stMorph">the morph form to store in the sandbox</param>
			/// <param name="ccTrailing">number of trailing spaces for the morph</param>
			/// <param name="fMonoMorphemic">flag whether we're processing a monomorphemic word</param>
			void HandleMorpheme(string stMorph, int ccTrailing, bool fMonoMorphemic)
			{
				int clsidForm; // The subclass of MoMorph to create if we need a new object.
				string realForm = stMorph; // Gets stripped of morpheme-type characters.
				IMoMorphType mmt;
				try
				{
					mmt = MoMorphType.FindMorphType(m_caches.MainCache, m_types,
						ref realForm, out clsidForm);
				}
				catch (Exception e)
				{
					MessageBox.Show(null, e.Message, ITextStrings.ksWarning, MessageBoxButtons.OK);
					mmt = m_types.Item(MoMorphType.kmtStem);
					clsidForm = MoStemAllomorph.kclsidMoStemAllomorph;
				}
				int hvoSbForm = 0; // hvo of the SbNamedObj that is the form of the morph.
				int hvoSbMorph = 0;
				bool fCanReuseOldMorphData = false;
				int maxSkip = m_cOldMorphs - m_cNewMorphs;
				if (m_imorph < m_cOldMorphs)
				{
					// If there's existing analysis and any morphs match, keep the analysis of
					// the existing morph. It's probably the best guess.
					string sSbForm = GetExistingMorphForm(out hvoSbMorph, out hvoSbForm, m_imorph);
					if (sSbForm != realForm && maxSkip > 0)
					{
						// If we're deleting morph breaks, we may need to skip over a morph to
						// find the matching existing morph.
						int hvoSbFormT = 0;
						int hvoSbMorphT = 0;
						string sSbFormT = null;
						List<int> skippedMorphs = new List<int>();
						skippedMorphs.Add(hvoSbMorph);
						for (int skip = 1; skip <= maxSkip; ++skip)
						{
							sSbFormT = GetExistingMorphForm(out hvoSbMorphT, out hvoSbFormT, m_imorph + skip);
							if (sSbFormT == realForm)
							{
								hvoSbForm = hvoSbFormT;
								hvoSbMorph = hvoSbMorphT;
								sSbForm = sSbFormT;
								foreach (int hvo in skippedMorphs)
									m_sda.DeleteObjOwner(m_hvoSbWord, hvo, ktagSbWordMorphs, m_imorph);
								m_cOldMorphs -= skippedMorphs.Count;
							break;
							}
							skippedMorphs.Add(hvoSbMorphT);
						}
					}
					if (sSbForm != realForm)
					{
						// Clear out the old analysis. Can't be relevant to a different form.
						m_cda.CacheObjProp(hvoSbMorph, ktagSbMorphEntry, 0);
						m_cda.CacheObjProp(hvoSbMorph, ktagSbMorphGloss, 0);
						m_cda.CacheObjProp(hvoSbMorph, ktagSbMorphPos, 0);
					}
					else
					{
						fCanReuseOldMorphData = m_sda.get_StringProp(hvoSbMorph, ktagSbMorphPrefix).Text == mmt.Prefix
						&& m_sda.get_StringProp(hvoSbMorph, ktagSbMorphPostfix).Text == mmt.Postfix;
						//&& m_sda.get_IntProp(hvoSbMorph, ktagSbMorphClsid) == clsidForm
						//&& m_sda.get_IntProp(hvoSbMorph, ktagSbMorphRealType) == mmt.Hvo;
					}
				}
				else
				{
					// Make a new morph, and an SbNamedObj to go with it.
					hvoSbMorph = m_sda.MakeNewObject(kclsidSbMorph, m_hvoSbWord,
						ktagSbWordMorphs, m_imorph);
					hvoSbForm = m_sda.MakeNewObject(kclsidSbNamedObj, hvoSbMorph,
						ktagSbMorphForm, -2); // -2 for atomic
				}
				if (!fCanReuseOldMorphData)
				{
					// This might be redundant, but it isn't expensive.
					m_cda.CacheStringAlt(hvoSbForm, ktagSbNamedObjName, m_wsVern,
						m_tsf.MakeString(realForm, m_wsVern));
					m_cda.CacheStringProp(hvoSbMorph, ktagSbMorphPrefix,
						m_tsf.MakeString(mmt.Prefix, m_wsVern));
					m_cda.CacheStringProp(hvoSbMorph, ktagSbMorphPostfix,
						m_tsf.MakeString(mmt.Postfix, m_wsVern));
					//m_cda.CacheIntProp(hvoSbMorph, ktagSbMorphClsid, clsidForm);
					//m_cda.CacheIntProp(hvoSbMorph, ktagSbMorphRealType, mmt.Hvo);
					// Fill in defaults.
					m_sandbox.EstablishDefaultEntry(hvoSbMorph, realForm, mmt.Hvo, fMonoMorphemic);
				}
				// the morpheme is not a guess.
				m_cda.CacheIntProp(hvoSbForm, ktagSbNamedObjGuess, 0);
				// Figure whether selection is in this morpheme.
				int ichSelMorph = m_ichSelInput - m_cchPrevMorphemes;

				int cchPrefix = (mmt.Prefix == null) ? 0 : mmt.Prefix.Length;
				int cchPostfix = (mmt.Postfix == null) ? 0 : mmt.Postfix.Length;
				int cchMorph = realForm.Length;
				// If this is < 0, we must be in a later morpheme and should have already
				// established m_ichSelOutput.
				if (ichSelMorph >= 0 && ichSelMorph <= cchPrefix + cchPostfix + cchMorph)
				{
					m_ihvoSelMorph = m_imorph;
					m_ichSelOutput = ichSelMorph - cchPrefix;
					m_tagSel = ktagSbNamedObjName;
					if (m_ichSelOutput < 0)
					{
						// in the prefix
						m_tagSel = ktagSbMorphPrefix;
						m_ichSelOutput = ichSelMorph;
					}
					else if (m_ichSelOutput > cchMorph)
					{
						if (cchPostfix > 0)
						{
							// in the postfix
							m_ichSelOutput = cchPostfix;
							m_tagSel = ktagSbMorphPostfix;
						}
						else
						{
							m_ichSelOutput = cchMorph;
						}
					}
				}
				m_cchPrevMorphemes += cchPrefix + cchPostfix + cchMorph + ccTrailing;
				m_imorph++;
			}

			private string GetExistingMorphForm(out int hvoSbMorph, out int hvoSbForm, int imorph)
			{
				hvoSbMorph = m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
				hvoSbForm = m_sda.get_ObjectProp(hvoSbMorph, ktagSbMorphForm);
				Debug.Assert(hvoSbForm != 0); /// We always have one of these for each form.
				return m_sda.get_MultiStringAlt(hvoSbForm, ktagSbNamedObjName, m_wsVern).Text;
			}

			/// <summary>
			/// Handle basic work on finding the morpheme breaks.
			/// </summary>
			/// <param name="input"></param>
			/// <param name="breakMarkers"></param>
			/// <param name="prefixMarkers"></param>
			/// <param name="postfixMarkers"></param>
			/// <returns>A string suitable for followup processing by client.</returns>
			public static string DoBasicFinding(string input, string[] breakMarkers, string[] prefixMarkers, string[] postfixMarkers)
			{
				string fullForm = input.Trim();
				int iMatched = -1;
				// the morphBreakSpace should be the last item.
				string morphBreakSpace = breakMarkers[breakMarkers.Length - 1];
				Debug.Assert(morphBreakSpace == " " || morphBreakSpace == "  ",
					"expected a morphbreak space at last index");

				// First, find the segment boundaries.
				List<int> vichMin = new List<int>();
				List<int> vichLim = new List<int>();
				int ccchSeg = 0;
				vichMin.Add(0);
				for (int ichStart = 0; ichStart < fullForm.Length; )
				{
					int ichBrk = MiscUtils.IndexOfAnyString(fullForm, breakMarkers,
						ichStart, out iMatched);
					if (ichBrk < 0)
						break;
					vichLim.Add(ichBrk);
					// Skip over consecutive markers
					for (ichBrk += breakMarkers[iMatched].Length;
						ichBrk < fullForm.Length;
						ichBrk += breakMarkers[iMatched].Length)
					{
						if (MiscUtils.IndexOfAnyString(fullForm, breakMarkers, ichBrk, out iMatched) != ichBrk)
							break;
					}
					vichMin.Add(ichBrk);
					ichStart = ichBrk;
				}
				vichLim.Add(fullForm.Length);
				Debug.Assert(vichMin.Count == vichLim.Count);
				int ieRoot = 0;
				int cchRoot = 0;
				int cLongest = 0;
				for (int i = 0; i < vichMin.Count; ++i)
				{
					int ichMin = vichMin[i];
					int ichLim = vichLim[i];
					int cchSeg = ichLim - ichMin;
					ccchSeg++;
					if (cchRoot < cchSeg)
					{
						cchRoot = cchSeg;
						ieRoot = i;
						cLongest = 1;
					}
					else if (cchRoot == cchSeg)
					{
						++cLongest;
					}
				}
				if (cLongest == ccchSeg && cLongest > 2)
				{
					// All equal length, 3 or more segments.
					ieRoot = 1;		// Pure speculation at this point, based on lengths.
				}
				// Look for a root that's delimited by spaces fore and aft.
				for (int i = 0; i < vichMin.Count; ++i)
				{
					int ichMin = vichMin[i];
					int ichLim = vichLim[i];
					if ((ichMin == 0 || fullForm[ichMin - 1] == ' ') &&
						(ichLim == fullForm.Length || fullForm[ichLim] == ' '))
					{
						ieRoot = i;
						cchRoot = ichLim - ichMin;
						break;
					}
				}
				// Here it is: what we've been computing towards up to this point!
				int ichRootMin = vichMin[ieRoot];

				int iMatchedPrefix, iMatchedPostfix;
				// The code to insert spaces is problematic. After all, some words composed of compounded roots are hyphenated,
				// but we like to use hyphens to mark affixes. Automatically inserting a space in order to handle affixes prevents
				// hyphenated roots from being properly handled.
				for (bool fFixedProblem = true; fFixedProblem; )
				{
					fFixedProblem = false;
					for (int ichStart = 0; ; )
					{
						int indexPrefix = MiscUtils.IndexOfAnyString(fullForm, prefixMarkers, ichStart, out iMatchedPrefix);
						int indexPostfix = MiscUtils.IndexOfAnyString(fullForm, postfixMarkers, ichStart, out iMatchedPostfix);
						if (indexPrefix < 0 && indexPostfix < 0)
							break; // no (remaining) problems!!
						int index;
						int ichNext;
						int cchMarker;
						if (indexPostfix < 0)
						{
							index = indexPrefix;
							cchMarker = prefixMarkers[iMatchedPrefix].Length;
							ichNext = indexPrefix + cchMarker;
						}
						else if (indexPrefix < 0)
						{
							index = indexPostfix;
							cchMarker = postfixMarkers[iMatchedPostfix].Length;
							ichNext = indexPostfix + cchMarker;
						}
						else if (indexPostfix < indexPrefix)
						{
							index = indexPostfix;
							cchMarker = postfixMarkers[iMatchedPostfix].Length;
							ichNext = indexPostfix + cchMarker;
						}
						else
						{
							index = indexPrefix;
							cchMarker = prefixMarkers[iMatchedPrefix].Length;
							ichNext = indexPrefix + cchMarker;
						}
						int cchWordPreceding = 0;
						bool fFoundProblemPreceding = false;
						for (int ich = index - 1; ich >= ichStart; --ich, ++cchWordPreceding)
						{
							if (ich > index - morphBreakSpace.Length)
							{
								// we'll assume we found a problem if we found a space here
								// unless we match the morphBreakSpace next iteration.
								if (fullForm[ich] == ' ')
									fFoundProblemPreceding = true;
								continue;	// we can't match the substring (yet)
							}
							if (fullForm.Substring(ich, morphBreakSpace.Length) == morphBreakSpace)
							{
								// after having enough room to check the morphBreakSpace,
								// we found one, so we don't really have ccWordPreceding.
								if (ich + morphBreakSpace.Length == index)
								{
									cchWordPreceding = 0;
									fFoundProblemPreceding = false;
								}
								break;
							}
						}

						indexPrefix = MiscUtils.IndexOfAnyString(fullForm, prefixMarkers, ichNext, out iMatchedPrefix);
						indexPostfix = MiscUtils.IndexOfAnyString(fullForm, postfixMarkers, ichNext, out iMatchedPostfix);
						int index2;
						if (indexPrefix < 0 && indexPostfix < 0)
							index2 = fullForm.Length;
						else if (indexPrefix < 0)
							index2 = indexPostfix;
						else if (indexPostfix < 0)
							index2 = indexPrefix;
						else
							index2 = Math.Min(indexPrefix, indexPostfix);

						int cchWordFollowing = 0;
						for (int ich = ichNext; ich < index2; ++ich, ++cchWordFollowing)
						{
							if (ich + morphBreakSpace.Length > fullForm.Length)
								continue;	// we can't match the substring
							if (fullForm.Substring(ich, morphBreakSpace.Length) == morphBreakSpace)
								break;
						}
						ichStart = ichNext; // for next iteration of inner loop, if any
						if (cchWordFollowing > 0 && cchWordPreceding > 0 || fFoundProblemPreceding)
						{
							// We will fix a problem! Insert a space at index or index + cchMarker.
							fFixedProblem = true;
							string morphBreakSpaceAdjusted = morphBreakSpace;
							if (fFoundProblemPreceding)
							{
								// we found a preceding space, we need to just
								// add one more space before the affix.
								morphBreakSpaceAdjusted = " ";
							}
							else if (index < ichRootMin)
							{
								// if before the root, guess a prefix.  Otherwise, guess a suffix.
								index += cchMarker;	// adjust for the marker (can be > 1)
								ichRootMin += morphBreakSpace.Length;	// adjust for inserted space.
							}
							Debug.Assert(index < input.Length && fullForm.Length <= input.Length);
							fullForm = fullForm.Substring(0, index) + morphBreakSpaceAdjusted +
								fullForm.Substring(index);
							break; // from inner loop, continue outer.
						}
					}
				}

				return fullForm;
			}

			public static List<string> BreakIntoMorphs(string morphFormWithMarkers, string baseWord)
			{
				List<int> ichMinsOfNextMorph;
				return BreakIntoMorphs(morphFormWithMarkers, baseWord, out ichMinsOfNextMorph);
			}

			/// <summary>
			/// Split the string into morphs respecting existing spaces in base word
			/// </summary>
			/// <param name="morphFormWithMarkers"></param>
			/// <param name="baseWord"></param>
			/// <param name="ccTrailingMorphs">character count of number of trailing whitespaces for each morph</param>
			/// <returns>list of morphs for the given word</returns>
			private static List<string> BreakIntoMorphs(string morphFormWithMarkers, string baseWord, out List<int> ccTrailingMorphs)
			{
				ccTrailingMorphs = new List<int>();
				// if the morphForm break down matches the base word, just return this string.
				// the user hasn't done anything to change the morphbreaks.
				if (morphFormWithMarkers == baseWord)
				{
					ccTrailingMorphs.Add(0);
					return new List<string>(new string[] { morphFormWithMarkers });
				}
				// find any existing white spaces in the baseWord.
				List<string> morphs = new List<string>();
				// we're dealing with a phrase if there are spaces in the word.
				bool fBaseWordIsPhrase = SandboxBase.IsPhrase(baseWord);
				List<int> morphEndOffsets = IchLimOfMorphs(morphFormWithMarkers, fBaseWordIsPhrase);
				int prevEndOffset = 0;
				foreach (int morphEndOffset in morphEndOffsets)
				{
					string morph = morphFormWithMarkers.Substring(prevEndOffset, morphEndOffset - prevEndOffset);
					morph = morph.Trim();
					// figure the trailing characters following the previous morph by the difference betweeen
					// the current morphEndOffset the length of the trimmed morph and prevEndOffset
					if (prevEndOffset > 0)
						ccTrailingMorphs.Add(morphEndOffset - prevEndOffset - morph.Length);
					if (!String.IsNullOrEmpty(morph))
						morphs.Add(morph);
					prevEndOffset = morphEndOffset;
				}
				// add the count of the final trailing space characters
				ccTrailingMorphs.Add(morphFormWithMarkers.Length - morphEndOffsets[morphEndOffsets.Count - 1]);
				return morphs;
			}

			/// <summary>
			/// get the end offsets for potential morphs based upon whitespace delimiters
			/// </summary>
			/// <param name="sourceString"></param>
			/// <returns></returns>
			private static List<int> IchLimOfMorphs(string sourceString, bool fBaseWordIsPhrase)
			{
				List<int> whiteSpaceOffsets = WhiteSpaceOffsets(sourceString);
				List<int> morphEndOffsets = new List<int>(whiteSpaceOffsets);
				int prevOffset = -1;
				int cOffsets = whiteSpaceOffsets.Count;
				foreach (int offset in whiteSpaceOffsets)
				{
					// we always want to remove spaces following a previous space
					// or if we're in a a phrase, always remove the last offset, since
					// it cannot be followed by a second one.
					if (prevOffset != -1 && offset == prevOffset + 1 ||
						fBaseWordIsPhrase && offset == whiteSpaceOffsets[whiteSpaceOffsets.Count - 1])
					{
						morphEndOffsets.Remove(offset);
					}

					if (fBaseWordIsPhrase)
					{
						// for a phrase, we always want to remove previous offsets
						// that are not followed by a space offset
						if (prevOffset != -1 && prevOffset != offset - 1)
						{
							morphEndOffsets.Remove(prevOffset);
						}
					}
					prevOffset = offset;
				}
				// finally add the end of the sourcestring to the offsets.
				morphEndOffsets.Add(sourceString.Length);
				return morphEndOffsets;
			}

			private static List<int> WhiteSpaceOffsets(string sourceString)
			{
				List<int> whiteSpaceOffsets = new List<int>();
				int ichMatch = 0;
				do
				{
					ichMatch = sourceString.IndexOfAny(Unicode.SpaceChars, ichMatch);
					if (ichMatch != -1)
					{
						whiteSpaceOffsets.Add(ichMatch);
						ichMatch++;
					}
				} while (ichMatch != -1);
				return whiteSpaceOffsets;
			}


			/// <summary>
			/// Run the morpheme breaking algorithm.
			/// </summary>
			public void Run()
			{
				// If morpheme break characters occur in invalid positions, try to fix things.
				// This is most often due to the user inserting hyphens but neglecting to also
				// insert spaces, thus leading to ambiguity.  Sometimes inserting spaces and
				// sometimes not makes our life even more complicated, if not impossible.
				// The basic heuristic is this:
				// 1) find the root, which is defined as the longest contiguous stretch of
				//    characters not containing a break character or a space.  If all segments
				//    are the same length, the first of two segments is the root, or the second
				//    of more than two segments is the root.  However, if one or more roots are
				//    marked by surrounding spaces, they can be shorter than the prefixes or
				//    suffixes.
				// 2) everything before the root is a prefix.
				// 3) everything after the root is a suffix.
				// Of course, if the user sometimes inserts spaces, and sometimes doesn't, all
				// bets are off!  The same is true if he doubles (or worse, triples...) the
				// break characters.

				string[] prefixMarkers = MoMorphType.PrefixMarkers(m_caches.MainCache);
				string[] postfixMarkers = MoMorphType.PostfixMarkers(m_caches.MainCache);

				StringCollection allMarkers = new StringCollection();
				foreach (string s in prefixMarkers)
				{
					allMarkers.Add(s);
				}

				foreach (string s in postfixMarkers)
				{
					if (!allMarkers.Contains(s))
						allMarkers.Add(s);
				}
				ITsString tssBaseWordform = m_sda.get_MultiStringAlt(kSbWord, ktagSbWordForm, m_sandbox.RawWordformWs);
				// for phrases, the breaking character is a double-space. for normal words, it's simply a space.
				bool fBaseWordIsPhrase = SandboxBase.IsPhrase(tssBaseWordform.Text);
				allMarkers.Add(fBaseWordIsPhrase ? "  " : " ");

				string[] breakMarkers = new string[allMarkers.Count];
				allMarkers.CopyTo(breakMarkers, 0);
				// if we trim our input string, be sure to readjust our selection pointer.
				string fullForm = DoBasicFinding(TrimInputString(), breakMarkers, prefixMarkers, postfixMarkers);

				List<int> ccTrailingMorphs;
				List<string> morphs = BreakIntoMorphs(fullForm, tssBaseWordform.Text, out ccTrailingMorphs);
				int imorph = 0;
				m_cNewMorphs = morphs.Count;
				foreach (string morph in morphs)
				{
					HandleMorpheme(morph, ccTrailingMorphs[imorph], morphs.Count == 1);
					++imorph;
				}
				// Delete any leftover old morphemes.
				List<int> oldMorphHvos = new List<int>();
				imorph = m_imorph;
				for (; m_imorph < m_cOldMorphs; m_imorph++)
				{
					oldMorphHvos.Add(m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, m_imorph));
				}
				foreach (int hvo in oldMorphHvos)
					m_sda.DeleteObjOwner(m_hvoSbWord, hvo, ktagSbWordMorphs, imorph);
			}

			private string TrimInputString()
			{
				string origInput = m_input;
				m_input = m_input.Trim();
				// first see if the selection was at the end of the input string on a whitespace
				if (origInput.LastIndexOfAny(Unicode.SpaceChars) == (origInput.Length - 1) &&
					m_ichSelInput == origInput.Length)
				{
					m_ichSelInput = m_input.Length;	// adjust to the new length
				}
				else if (origInput.IndexOfAny(Unicode.SpaceChars) == 0 &&
					m_ichSelInput >= 0)
				{
					// if we trimmed something from the start of our input string
					// then adjust the selection offset by the the amount trimmed.
					m_ichSelInput -= origInput.IndexOf(m_input);
				}
				return m_input;
			}

			/// <summary>
			/// The selection (in m_sinput) where we'd like to restore the selection
			/// (by means of MakeSel, after calling Run).
			/// </summary>
			public int IchSel
			{
				get { return m_ichSelInput; }
				set { m_ichSelInput = value; }
			}

			/// <summary>
			/// Reestablish a selection, if possible.
			/// </summary>
			public void MakeSel()
			{
				if (m_tagSel == -1)
					return;
				int clev = 2; // typically two level
				if (m_tagSel != ktagSbNamedObjName)
					clev--; // prefix and postfix are one level less embedded
				SelLevInfo[] rgsli = new SelLevInfo[clev];
				// The selection is in the morphemes of the root object
				rgsli[clev - 1].ihvo = m_ihvoSelMorph;
				rgsli[clev - 1].tag = ktagSbWordMorphs;
				if (clev > 1)
					rgsli[0].tag = ktagSbMorphForm; // leave other slots zero
				try
				{
					m_sandbox.RootBox.MakeTextSelection(
						m_sandbox.IndexOfCurrentItem, // which root,
						clev, rgsli,
						m_tagSel,
						0, // no previous occurrence
						m_ichSelOutput, m_ichSelOutput, m_wsVern,
						false, // needs to be false here to associate with trailing character
						// esp. for when the cursor is at the beginning of the morpheme (LT-7773)
						-1, // end not in different object
						null, // no special props to use for typing
						true); // install it.
				}
				catch (Exception)
				{
					// Ignore anything that goes wrong making a selection. At worst we just don't have one.
				}
			}
		}

		public class SandboxVc : VwBaseVc
		{
			internal int krgbBackground = (int)CmObjectUi.RGB(235, 235, 220); //235,235,220);
			//internal int krgbBackground = (int) CmObjectUi.RGB(Color.FromKnownColor(KnownColor.ControlLight)); //235,235,220);
			internal int krgbBorder = (int)CmObjectUi.RGB(Color.Blue);
			internal int krgbEditable = (int)CmObjectUi.RGB(Color.White);
			internal int krgbGuess = (int)CmObjectUi.RGB(Color.LightGray);
			const int kmpIconMargin = 3000; // gap between pull-down icon and morph (also word gloss and boundary)
			internal const int kfragBundle = 100001;
			internal const int kfragMorph = 100002;
			internal const int kfragFirstMorph = 1000014;
			internal const int kfragMissingMorphs = 100003;
			internal const int kfragMissingEntry = 100005;
			internal const int kfragMissingMorphGloss = 100006;
			internal const int kfragMissingMorphPos = 100007;
			internal const int kfragMissingWordPos = 100008;
			//internal const int kfragMlAnalysisNames = 100013;
			// 14 is used above
			// This one needs a free range following it. It displays the name of an SbNamedObject,
			// using the writing system indicated by m_choices[frag - kfragNamedObjectNameChoices.
			internal const int kfragNamedObjectNameChoices = 1001000;


			protected int m_wsVern;
			protected int m_wsAnalysis;
			protected int m_wsUi;
			protected CachePair m_caches;
			ITsString m_tssMissingMorphs;
			ITsString m_tssMissingEntry;
			ITsString m_tssMissingMorphGloss;
			ITsString m_tssMissingMorphPos;
			ITsString m_tssMissingWordPos;
			ITsString m_tssEmptyAnalysis;
			ITsString m_tssEmptyVern;
			ITsStrFactory m_tsf = TsStrFactoryClass.Create();
			InterlinLineChoices m_choices;
			stdole.IPicture m_PulldownArrowPic;

			// width in millipoints of the arrow picture.
			int m_dxmpArrowPicWidth;
			bool m_fIconsForAnalysisChoices;
			bool m_fShowMorphBundles = true;
			bool m_fIconForWordGloss = false;
			bool m_fIsMorphemeFormEditable = true;
			bool m_fRtl = false;
			SandboxBase m_sandbox;

			public SandboxVc(CachePair caches, InterlinLineChoices choices, bool fIconsForAnalysisChoices, SandboxBase sandbox)
			{
				m_caches = caches;
				m_choices = choices;
				m_sandbox = sandbox;
				m_fIconsForAnalysisChoices = fIconsForAnalysisChoices;
				m_wsAnalysis = caches.MainCache.LangProject.DefaultAnalysisWritingSystem;
				m_wsUi = caches.MainCache.LanguageWritingSystemFactoryAccessor.UserWs;
				m_tssMissingMorphs = m_tsf.MakeString(ITextStrings.ksStars, m_sandbox.RawWordformWs);
				m_tssEmptyAnalysis = m_tsf.MakeString("", m_wsAnalysis);
				m_tssEmptyVern = m_tsf.MakeString("", m_sandbox.RawWordformWs);
				m_tssMissingEntry = m_tssMissingMorphs;
				// It's tempting to re-use m_tssMissingMorphs, but the analysis and vernacular default
				// fonts may have different sizes, requiring differnt line heights to align things well.
				m_tssMissingMorphGloss = m_tsf.MakeString(ITextStrings.ksStars, m_wsAnalysis);
				m_tssMissingMorphPos = m_tsf.MakeString(ITextStrings.ksStars, m_wsAnalysis);
				m_tssMissingWordPos = m_tssMissingMorphPos;
				m_PulldownArrowPic = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(ResourceHelper.InterlinPopupArrow);
				m_dxmpArrowPicWidth = ConvertPictureWidthToMillipoints(m_PulldownArrowPic);
				IWritingSystem wsObj = caches.MainCache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(m_sandbox.RawWordformWs);
				if (wsObj != null)
					m_fRtl = wsObj.RightToLeft;

			}

			/// <summary>
			/// We want a width in millipoints (72000/inch). Value we have is in 100/mm. There are 25.4 mm/inch.
			/// </summary>
			/// <param name="picture"></param>
			/// <returns></returns>
			private int ConvertPictureWidthToMillipoints(stdole.IPicture picture)
			{
				const int kMillipointsPerInch = 72000 / 2540;
				return picture.Width * kMillipointsPerInch;
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
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_sandbox = null; // Client gave it to us, so has to deal with it.
				m_caches = null; // Client gave it to us, so has to deal with it.
				Marshal.ReleaseComObject(m_PulldownArrowPic);
				m_PulldownArrowPic = null;
				Marshal.ReleaseComObject(m_tsf);
				m_tsf = null;
				m_tssMissingEntry = null; // Same as m_tssMissingMorphs, so just null it.
				m_tssMissingWordPos = null; // Same as m_tssMissingMorphPos, so just null it.
				Marshal.ReleaseComObject(m_tssMissingMorphs);
				m_tssMissingMorphs = null;
				Marshal.ReleaseComObject(m_tssEmptyAnalysis);
				m_tssEmptyAnalysis = null;
				Marshal.ReleaseComObject(m_tssEmptyVern);
				m_tssEmptyVern = null;
				Marshal.ReleaseComObject(m_tssMissingMorphGloss);
				m_tssMissingMorphGloss = null;
				Marshal.ReleaseComObject(m_tssMissingMorphPos);
				m_tssMissingMorphPos = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			/// <summary>
			/// Get or set the editability for the moprhem form row.
			/// </summary>
			/// <remarks>
			/// 'False' means to not show the icon and to not make the form editable.
			/// 'True' means to show the icon under certain conditions, and to allow the form to be edited.
			/// </remarks>
			public bool IsMorphemeFormEditable
			{
				get
				{
					CheckDisposed();
					return m_fIsMorphemeFormEditable;
				}
				set
				{
					CheckDisposed();
					m_fIsMorphemeFormEditable = value;
				}
			}

			/// <summary>
			/// Color to use for guessing.
			/// </summary>
			internal int GuessColor
			{
				get
				{
					CheckDisposed();
					return krgbGuess;
				}
				set
				{
					CheckDisposed();
					krgbGuess = value;
				}
			}

			internal int BackColor
			{
				get
				{
					CheckDisposed();
					return krgbBackground;
				}
				set
				{
					CheckDisposed();
					krgbBackground = value;
				}
			}

			/// <summary>
			/// Get/set whether the sandbox is RTL
			/// </summary>
			internal bool RightToLeft
			{
				get
				{
					CheckDisposed();
					return m_fRtl;
				}
				set
				{
					CheckDisposed();

					if (value == m_fRtl)
						return;
					m_fRtl = value;
					if (m_sandbox.RootBox != null)
						m_sandbox.RootBox.Reconstruct();
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

			public bool ShowWordGlossIcon
			{
				get
				{
					CheckDisposed();
					return m_fIconForWordGloss;
				}
			}

			/// <summary>
			/// Called right before adding a string or opening a flow object, sets its color.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="color"></param>
			protected static void SetColor(IVwEnv vwenv, int color)
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
			protected static void AddColoredString(IVwEnv vwenv, int color, ITsStrFactory tsf, int ws, string str)
			{
				SetColor(vwenv, color);
				vwenv.AddString(tsf.MakeString(str, ws));
			}

			/// <summary>
			/// Add to the vwenv the label(s) for a gloss line.
			/// If multiple glosses are wanted, it generates a set of labels
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="tsf"></param>
			/// <param name="baseLabel"></param>
			/// <param name="cache"></param>
			/// <param name="fShowMultilingGlosses"></param>
			public static void AddGlossLabels(IVwEnv vwenv, ITsStrFactory tsf, int color, string baseLabel,
				FdoCache cache, WsListManager wsList)
			{
				if (wsList != null && wsList.AnalysisWsLabels.Length > 1)
				{
					ITsString tssBase = tsf.MakeString(baseLabel, cache.DefaultUserWs);
					ITsString space = tsf.MakeString(" ", cache.DefaultUserWs);
					foreach (ITsString tssLabel in wsList.AnalysisWsLabels)
					{
						SetColor(vwenv, color);
						vwenv.OpenParagraph();
						vwenv.AddString(tssBase);
						vwenv.AddString(space);
						vwenv.AddString(tssLabel);
						vwenv.CloseParagraph();
					}
				}
				else
				{
					AddColoredString(vwenv, color, tsf, cache.DefaultAnalWs, baseLabel);
				}
			}

			private void AddPullDownIcon(IVwEnv vwenv, int tag)
			{
				if (m_fIconsForAnalysisChoices)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
						(int)FwTextPropVar.ktpvMilliPoint, kmpIconMargin);
					vwenv.set_IntProperty((int)FwTextPropType.ktptOffset,
						(int)FwTextPropVar.ktpvMilliPoint, -2500);
					vwenv.AddPicture(m_PulldownArrowPic, tag, 0, 0);
				}
			}

			/// <summary>
			/// Set the indent needed when the icon is missing.
			/// </summary>
			/// <param name="vwenv"></param>
			private void SetIndentForMissingIcon(IVwEnv vwenv)
			{
				vwenv.set_IntProperty(
					(int)FwTextPropType.ktptLeadingIndent,
					(int)FwTextPropVar.ktpvMilliPoint,
					m_dxmpArrowPicWidth + kmpIconMargin);
			}

			/// <summary>
			/// If fWantIcon is true, add a pull-down icon; otherwise, set enough indent so the
			/// next thing in the paragraph will line up with things that have icons.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="fWantIcon"></param>
			private void SetIndentOrDisplayPullDown(IVwEnv vwenv, int tag, bool fWantIcon)
			{
				if (fWantIcon)
					AddPullDownIcon(vwenv, tag);
				else
					SetIndentForMissingIcon(vwenv);
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				try
				{
					switch (frag)
					{
						case kfragBundle: // One annotated word bundle, in this case, the whole view.
							if (hvo == 0)
								return;
							vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
								(int)SpellingModes.ksmDoNotCheck);
							vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
								(int)FwTextPropVar.ktpvDefault, krgbBackground);
							vwenv.OpenDiv();
							vwenv.OpenParagraph();
							// Since embedded in a pile with context, we need another layer of pile here,.
							// It's an overlay sandbox: draw a box around it. To do this
							// we need to open a division. (Just putting the border on the paragraph
							// produces a paragraph of near-infinite width and messes up the size of the
							// sandbox.)
							//vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop,
							//    (int)FwTextPropVar.ktpvMilliPoint, 1000);
							//vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom,
							//    (int)FwTextPropVar.ktpvMilliPoint, 1000);
							//vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading,
							//    (int)FwTextPropVar.ktpvMilliPoint, 1000);
							//vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
							//    (int)FwTextPropVar.ktpvMilliPoint, 1000);

							//// This seems to be needed to make the border show up.
							//vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
							//    (int)FwTextPropVar.ktpvMilliPoint, 1000);
							//vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
							//    (int)FwTextPropVar.ktpvDefault, krgbBorder);
							//vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
							//    (int)FwTextPropVar.ktpvDefault, krgbBackground);
							vwenv.OpenInnerPile();
							//vwenv.OpenDiv();
							// Inside that division we need a paragraph which does not have any border
							// or background. This suppresses the 'infinite width' behavior for the
							// nested paragraphs that may have grey border.
							vwenv.OpenParagraph();

							// This makes a little separation between left border and arrows.
							vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
								(int)FwTextPropVar.ktpvMilliPoint, 1000);
							if (m_fRtl)
							{
								// This must not be on the outer paragraph or we get infinite width behavior.
								vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
									(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
								vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
									(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
							}
							vwenv.OpenInnerPile();
							for (int ispec = 0; ispec < m_choices.Count; )
							{
								InterlinLineSpec spec = m_choices[ispec];
								if (!spec.WordLevel)
									break;
								if (spec.MorphemeLevel)
								{
									DisplayMorphBundles(vwenv, hvo);
									ispec = m_choices.LastMorphemeIndex + 1;
									continue;
								}
								switch (spec.Flid)
								{
									case InterlinLineChoices.kflidWord:
										int ws = GetActualWs(hvo, spec.StringFlid, spec.WritingSystem);
										DisplayWordform(vwenv, ws, ispec);
										break;
									case InterlinLineChoices.kflidWordGloss:
										DisplayWordGloss(vwenv, hvo, spec.WritingSystem, ispec);
										break;
									case InterlinLineChoices.kflidWordPos:
										DisplayWordPOS(vwenv, hvo, spec.WritingSystem, ispec);
										break;
								}
								ispec++;
							}
							vwenv.CloseInnerPile();

							vwenv.CloseParagraph();
							vwenv.CloseInnerPile();

							vwenv.CloseParagraph();
							vwenv.CloseDiv();
							break;
						case kfragFirstMorph: // first morpheme in word
						case kfragMorph: // The bundle of 4 lines representing a morpheme.
							vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
								(int)FwTextPropVar.ktpvMilliPoint, 10000);
							vwenv.OpenInnerPile();
							for (int ispec = m_choices.FirstMorphemeIndex; ispec <= m_choices.LastMorphemeIndex; ispec++)
							{
								int tagLexEntryIcon = 0;
								if (m_choices.FirstLexEntryIndex == ispec)
									tagLexEntryIcon = ktagMorphEntryIcon;
								InterlinLineSpec spec = m_choices[ispec];
								switch (spec.Flid)
								{
									case InterlinLineChoices.kflidMorphemes:
										DisplayMorphForm(vwenv, hvo, frag, spec.WritingSystem, ispec);
										break;
									case InterlinLineChoices.kflidLexEntries:
										AddOptionalNamedObj(vwenv, hvo, ktagSbMorphEntry, ktagMissingEntry,
											kfragMissingEntry, tagLexEntryIcon, spec.WritingSystem, ispec);
										break;
									case InterlinLineChoices.kflidLexGloss:
										AddOptionalNamedObj(vwenv, hvo, ktagSbMorphGloss, ktagMissingMorphGloss,
											kfragMissingMorphGloss, tagLexEntryIcon, spec.WritingSystem, ispec);
										break;
									case InterlinLineChoices.kflidLexPos:
										AddOptionalNamedObj(vwenv, hvo, ktagSbMorphPos, ktagMissingMorphPos,
											kfragMissingMorphPos, tagLexEntryIcon, spec.WritingSystem, ispec);
										break;
								}
							}
							vwenv.CloseInnerPile();

							break;
						default:
							if (frag >= kfragNamedObjectNameChoices && frag < kfragNamedObjectNameChoices + m_choices.Count)
							{
								InterlinLineSpec spec = m_choices[frag - kfragNamedObjectNameChoices];
								int wsActual = GetActualWs(hvo, ktagSbNamedObjName, spec.WritingSystem);
								vwenv.AddStringAltMember(ktagSbNamedObjName, wsActual, this);
							}
							else
							{
								throw new Exception("Bad fragment ID in SandboxVc.Display");
							}
							break;
					}
				}
				catch
				{
				}
			}

			private int GetActualWs(int hvo, int tag, int ws)
			{
				switch (ws)
				{
					case LangProject.kwsVernInParagraph:
						ws = m_sandbox.RawWordformWs;
						break;
					case LangProject.kwsFirstAnal:
						ws = GetBestAlt(hvo, tag, m_caches.MainCache.DefaultAnalWs, m_caches.MainCache.DefaultAnalWs,
							m_caches.MainCache.LangProject.CurAnalysisWssRS.HvoArray);
						break;
					case LangProject.kwsFirstVern:
						// for best vernacular in Sandbox, we prefer to use the ws of the wordform
						// over the standard 'default vernacular'
						int wsPreferred = m_sandbox.RawWordformWs;
						ws = GetBestAlt(hvo, tag, wsPreferred, m_caches.MainCache.DefaultVernWs,
							m_caches.MainCache.LangProject.CurVernWssRS.HvoArray);
						break;
					default:
						if (ws < 0)
						{
							throw new ArgumentException(String.Format("magic ws {0} not yet supported.", ws));
						}
						break;
				}
				return ws;
			}

			private int GetBestAlt(int hvo, int tag, int wsPreferred, int wsDefault, int[] wsList)
			{
				Set<int> wsSet = new Set<int>();
				if (wsPreferred != 0)
					wsSet.Add(wsPreferred);
				wsSet.AddRange(wsList);
				// We're not dealing with a real cache, so can't call something like this:
				//ws = LangProject.InterpretWsLabel(m_caches.MainCache,
				//    LangProject.GetMagicWsNameFromId(ws),
				//    m_caches.MainCache.DefaultAnalWs,
				//    hvo, spec.StringFlid, null);
				int wsActual = 0;
				foreach (int ws1 in wsSet.ToArray())
				{
					ITsString tssTest = m_caches.DataAccess.get_MultiStringAlt(hvo, tag, ws1);
					if (tssTest != null && tssTest.Length != 0)
					{
						wsActual = ws1;
						break;
					}
				}
				// Enhance JohnT: to be really picky here we should do like the real InterpretWsLabel
				// and fall back to default UI language, then English.
				// But we probably aren't even copying those alternatives to the sandbox cache.
				if (wsActual == 0)
					wsActual = wsDefault;
				return wsActual;
			}

			private void DisplayLexGloss(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
			{
				int hvoNo = vwenv.DataAccess.get_ObjectProp(hvo, ktagSbMorphGloss);
				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
				if (m_fIconsForAnalysisChoices)
				{
					// This line does not have one, but add some white space to line things up.
					vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
						(int)FwTextPropVar.ktpvMilliPoint,
						m_dxmpArrowPicWidth + kmpIconMargin);
				}
				if (hvoNo == 0)
				{
					// One of these is enough, the regeneration will redo an outer object and get
					// all the alternatives.
					vwenv.NoteDependency(new int[] { hvo }, new int[] { ktagSbMorphGloss }, 1);
					vwenv.AddProp(ktagMissingMorphGloss, this, kfragMissingMorphGloss);
				}
				else
				{
					SetGuessing(vwenv, hvo, ktagSbNamedObjGuess);
					vwenv.AddObjProp(ktagSbMorphGloss, this, kfragNamedObjectNameChoices + choiceIndex);
				}
			}

			private void DisplayMorphForm(IVwEnv vwenv, int hvo, int frag, int ws, int choiceIndex)
			{
				int hvoMorphForm = vwenv.DataAccess.get_ObjectProp(hvo, ktagSbMorphForm);

				SetGuessing(vwenv, hvoMorphForm, ktagSbNamedObjGuess);
				// Allow editing of the morpheme breakdown line.
				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
				// On this line we want an icon only for the first column (and only if it is the first
				// occurrence of the flid).
				bool fWantIcon = m_fIsMorphemeFormEditable && (frag == kfragFirstMorph) && m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
				if (!fWantIcon)
					SetIndentForMissingIcon(vwenv);
				vwenv.OpenParagraph();
				bool fFirstMorphLine = (m_choices.IndexOf(InterlinLineChoices.kflidMorphemes) == choiceIndex);
				if (fWantIcon) // Review JohnT: should we do the 'edit box' for all first columns?
				{
					AddPullDownIcon(vwenv, ktagMorphFormIcon);
					// Create an edit box that stays visible when the user deletes
					// the first morpheme (like the WordGloss box).
					// This is especially useful if the user wants to
					// delete the entire MorphForm line (cf. LT-1621).
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
						(int)FwTextPropVar.ktpvMilliPoint,
						kmpIconMargin);
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
						(int)FwTextPropVar.ktpvMilliPoint,
						2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
						(int)FwTextPropVar.ktpvMilliPoint,
						2000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
						(int)FwTextPropVar.ktpvDefault, krgbEditable);
				}
				if (m_fIsMorphemeFormEditable)
					MakeNextFlowObjectEditable(vwenv);
				else
					MakeNextFlowObjectReadOnly(vwenv);
				vwenv.OpenInnerPile();
				vwenv.OpenParagraph();
				if (fFirstMorphLine)
					vwenv.AddStringProp(ktagSbMorphPrefix, this);
				// This is never missing, but may, or may not, be editable.
				vwenv.AddObjProp(ktagSbMorphForm, this, kfragNamedObjectNameChoices + choiceIndex);
				if (fFirstMorphLine)
					vwenv.AddStringProp(ktagSbMorphPostfix, this);
				// close the special edit box we opened for the first morpheme.
				vwenv.CloseParagraph();
				vwenv.CloseInnerPile();
				vwenv.CloseParagraph();
			}

			private void DisplayWordPOS(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				AddOptionalNamedObj(vwenv, hvo, ktagSbWordPos, ktagMissingWordPos,
					kfragMissingWordPos, ktagWordPosIcon, ws, choiceIndex);
			}

			private void DisplayWordGloss(IVwEnv vwenv, int hvo, int ws, int choiceIndex)
			{
				// Count how many glosses there are for the current analysis:
				int cGlosses = 0;
				// Find a wfi analysis (existing or guess) to determine whether to provide an icon for selecting
				// multiple word glosses for IhWordGloss.SetupCombo (cf. LT-1428)
				int hvoWa = m_sandbox.GetWfiAnalysisHvoInUse();
				if (hvoWa != 0)
				{
					IWfiAnalysis wa = WfiAnalysis.CreateFromDBObject(m_caches.MainCache, hvoWa);
					cGlosses = wa.MeaningsOC.Count;
				}

				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
				SetGuessing(vwenv, hvo, ktagSbWordGlossGuess);

				// Icon only if we want icons at all (currently always true) and there is at least one WfiGloss to choose
				// and this is the first word gloss line.
				bool fWantIcon = m_fIconsForAnalysisChoices &&
					(cGlosses > 0 || m_sandbox.ShouldAddWordGlossToLexicon) &&
					m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
				// If there isn't going to be an icon, add an indent.
				if (!fWantIcon)
				{
					SetIndentForMissingIcon(vwenv);
				}
				vwenv.OpenParagraph();
				if (fWantIcon)
				{
					AddPullDownIcon(vwenv, ktagWordGlossIcon);
					m_fIconForWordGloss = true;
				}
				else if (m_fIconForWordGloss == true && cGlosses == 0)
				{
					// reset
					m_fIconForWordGloss = false;
				}
				//							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop,
				//								(int)FwTextPropVar.ktpvMilliPoint,
				//								1000);
				//							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom,
				//								(int)FwTextPropVar.ktpvMilliPoint,
				//								1000);
				//							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading,
				//								(int)FwTextPropVar.ktpvMilliPoint,
				//								1000);
				//							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
				//								(int)FwTextPropVar.ktpvMilliPoint,
				//								1000);
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
					(int)FwTextPropVar.ktpvMilliPoint,
					kmpIconMargin);
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
					(int)FwTextPropVar.ktpvMilliPoint,
					2000);
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
					(int)FwTextPropVar.ktpvMilliPoint,
					2000);
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
					(int)FwTextPropVar.ktpvDefault, krgbEditable);
				vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
					(int)SpellingModes.ksmNormalCheck);
				vwenv.OpenInnerPile();

				// Set the appropriate paragraph direction for the writing system.
				bool fWsRtl = m_caches.MainCache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws).RightToLeft;
				if (fWsRtl != RightToLeft)
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
						(int)FwTextPropVar.ktpvEnum,
						fWsRtl ? (int)FwTextToggleVal.kttvForceOn : (int)FwTextToggleVal.kttvOff);

				vwenv.AddStringAltMember(ktagSbWordGloss, ws, this);
				vwenv.CloseInnerPile();
				vwenv.CloseParagraph();
			}

			private void DisplayMorphBundles(IVwEnv vwenv, int hvo)
			{
				if (m_fShowMorphBundles)
				{
					// Don't allow direct editing of the morph bundle lines.
					MakeNextFlowObjectReadOnly(vwenv);
					if (vwenv.DataAccess.get_VecSize(hvo, ktagSbWordMorphs) == 0)
					{
						SetColor(vwenv, m_choices.LabelRGBFor(m_choices.IndexOf(InterlinLineChoices.kflidMorphemes)));
						vwenv.AddProp(ktagMissingMorphs, this, kfragMissingMorphs);
						// Blank lines to fill up the gap; LexEntry line
						vwenv.AddString(m_tssEmptyVern);
						vwenv.AddString(m_tssEmptyAnalysis); // LexGloss line
						vwenv.AddString(m_tssEmptyAnalysis); // LexPos line
					}
					else
					{
						vwenv.OpenParagraph();
						vwenv.AddObjVec(ktagSbWordMorphs, this, kfragMorph);
						vwenv.CloseParagraph();
					}
				}
			}

			private void DisplayWordform(IVwEnv vwenv, int ws, int choiceIndex)
			{
				// For the wordform line we only want an icon on the first line (which is always wordform).
				bool fWantIcon = m_sandbox.ShowAnalysisCombo && choiceIndex == 0;
				// This has to be BEFORE we open the paragraph, so the indent applies to the whole
				// paragraph, and not some string box inside it.
				if (!fWantIcon)
					SetIndentForMissingIcon(vwenv);
				vwenv.OpenParagraph();
				// The actual icon, if present, has to be INSIDE the paragraph.
				if (fWantIcon)
					AddPullDownIcon(vwenv, ktagAnalysisIcon);
				if (ws != m_sandbox.RawWordformWs)
				{
					// Any other Ws we can edit.
					MakeNextFlowObjectEditable(vwenv);
					vwenv.OpenInnerPile(); // So white background occupies full width
					vwenv.AddStringAltMember(ktagSbWordForm, ws, this);
					vwenv.CloseInnerPile();
				}
				else
				{
					MakeNextFlowObjectReadOnly(vwenv);
					//vwenv.AddString(m_sandbox.RawWordform);
					vwenv.AddStringAltMember(ktagSbWordForm, ws, this);
				}
				vwenv.CloseParagraph();
			}

			private void SetEditabilityOfNextFlowObject(IVwEnv vwenv, bool fEditable)
			{
				if (fEditable)
					MakeNextFlowObjectEditable(vwenv);
				else
					MakeNextFlowObjectReadOnly(vwenv);
			}

			private void MakeNextFlowObjectReadOnly(IVwEnv vwenv)
			{
				vwenv.set_IntProperty(
					(int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
			}

			// Allow the next flow object to be edited (and give it a background color that
			// makes it look editable)
			private void MakeNextFlowObjectEditable(IVwEnv vwenv)
			{
				vwenv.set_IntProperty(
					(int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptIsEditable);
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
					(int)FwTextPropVar.ktpvDefault, krgbEditable);
			}

			public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
			{
				CheckDisposed();

				switch (frag)
				{
					case kfragMorph: // The bundle of 4 lines representing a morpheme.
						ISilDataAccess sda = vwenv.DataAccess;
						int cmorph = sda.get_VecSize(hvo, tag);
						for (int i = 0; i < cmorph; ++i)
						{
							int hvoMorph = sda.get_VecItem(hvo, tag, i);
							vwenv.AddObj(hvoMorph, this, i == 0 ? kfragFirstMorph : kfragMorph);
						}
						break;
				}
			}

			/// <summary>
			/// Disable guess colors until we figure out a better scheme. (LT-8498)
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="hvo"></param>
			/// <param name="tag"></param>
			private void SetGuessing(IVwEnv vwenv, int hvo, int tag)
			{
			}

			public override ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
			{
				CheckDisposed();

				switch (frag)
				{
					case kfragMissingMorphs:
						return m_tssMissingMorphs;
					case kfragMissingEntry:
						return m_tssMissingEntry;
					case kfragMissingMorphGloss:
						return m_tssMissingMorphGloss;
					case kfragMissingMorphPos:
						return m_tssMissingMorphPos;
					case kfragMissingWordPos:
						return m_tssMissingWordPos;
					default:
						throw new Exception("Bad fragment ID in SandboxVc.DisplayVariant");
				}
			}

			// Return the width of the arrow picture (in mm, unfortunately).
			internal int ArrowPicWidth
			{
				get
				{
					CheckDisposed();
					return m_PulldownArrowPic.Width;
				}
			}


			/// <summary>
			/// Add to the vwenv a display of property tag of object hvo, which stores an
			/// SbNamedObj.  If the property is non-null, display the name of the SbNamedObj.
			/// If not, display the dummyTag 'property' using the dummyFrag.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="hvo"></param>
			/// <param name="tag"></param>
			/// <param name="dummyTag"></param>
			/// <param name="dummyFrag"></param>
			/// <param name="tagIcon">If non-zero, display a pull-down icon before the item, marked with this tag.</param>
			/// <param name="ws">which alternative of the name to display</param>
			/// <param name="choiceIndex">which item in m_choices this comes from. The icon is displayed
			/// only if it is the first one for its flid.</param>
			protected void AddOptionalNamedObj(IVwEnv vwenv, int hvo, int tag, int dummyTag,
				int dummyFrag, int tagIcon, int ws, int choiceIndex)
			{
				int hvoNo = vwenv.DataAccess.get_ObjectProp(hvo, tag);
				SetColor(vwenv, m_choices.LabelRGBFor(choiceIndex));
				bool fWantIcon = false;
				fWantIcon = tagIcon != 0 && m_choices.IsFirstOccurrenceOfFlid(choiceIndex);
				if (m_fIconsForAnalysisChoices && !fWantIcon)
				{
					// This line does not have one, but add some white space to line things up.
					vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
						(int)FwTextPropVar.ktpvMilliPoint,
						m_dxmpArrowPicWidth + kmpIconMargin);
				}
				vwenv.OpenParagraph();
				if (fWantIcon)
					AddPullDownIcon(vwenv, tagIcon);
				// The NoteDependency is needed whether or not hvoNo is set, in case we update
				// to a sense which has a null MSA.  See LT-4246.
				vwenv.NoteDependency(new int[] { hvo }, new int[] { tag }, 1);
				if (hvoNo == 0)
					vwenv.AddProp(dummyTag, this, dummyFrag);
				else
					vwenv.AddObjProp(tag, this, kfragNamedObjectNameChoices + choiceIndex);
				vwenv.CloseParagraph();
			}
		}

		private bool IsSelectionOnIcon(IVwSelection vwselNew)
		{
			TextSelInfo selInfo = new TextSelInfo(vwselNew);
			return selInfo.TagAnchor >= ktagMinIcon &&
					selInfo.TagAnchor < ktagLimIcon;
		}

		/// <summary>
		/// If we have an action installed for this icon selection, do it.
		/// </summary>
		/// <param name="vwselNew"></param>
		/// <returns></returns>
		private bool DoActionOnIconSelection(IVwSelection vwselNew)
		{
			// See if this is an icon selection.
			TextSelInfo selInfo = new TextSelInfo(vwselNew);
			if (!IsSelectionOnIcon(vwselNew))
				return false;
			switch (selInfo.TagAnchor)
			{
				case ktagMorphFormIcon:
				case ktagMorphEntryIcon:
				case ktagWordPosIcon:
				case ktagAnalysisIcon:
				case ktagWordGlossIcon:
					// Combo Icons
					if (!m_fSuppressShowCombo)
						ShowComboForSelection(vwselNew, true);
					break;
				default:
					break;
			}
			return false;
		}

		/// <summary>
		/// This class and its subclasses handles the events that can happen in the course of
		/// the use of a combo box or popup list box in the Sandbox. Actually, a collection of
		/// subclasses, one for each kind of place the combo can be in the annotation hierarchy,
		/// handles the events.  For most of the primary events, the default here is to do
		/// nothing.
		/// </summary>
		public class InterlinComboHandler : IComboHandler, IFWDisposable
		{
			// Main array of information retrieved from sel that made combo.
			protected SelLevInfo[] m_rgvsli;
			protected int m_hvoSbWord; // Hvo of the root word.
			protected int m_hvoSelObject; // lowest level object selected.
			// selected morph, if any...may be zero if not in morph, or equal to m_hvoSelObject.
			protected int m_hvoMorph;
			// int for all classes, except IhMissingEntry, which studds MorphItem data into it.
			// So, that ill-behaved class has to make its own m_items data member.
			protected List<int> m_items = new List<int>();
			protected IComboList m_comboList;
			// More parallel data for the comboList items.
			protected IVwRootBox m_rootb;
			protected int m_wsVern;  // HVO of default vernacular writing system.
			protected int m_wsAnal;
			protected int m_wsUser;
			protected CachePair m_caches;
			protected bool m_fUnderConstruction; // True during SetupCombo.
			protected SandboxBase m_sandbox; // the sandbox we're manipulating.

			public InterlinComboHandler()
				: base()
			{
			}

			internal InterlinComboHandler(SandboxBase sandbox)
				: this()
			{
				m_sandbox = sandbox;
				m_caches = sandbox.Caches;
				m_wsVern = m_sandbox.RawWordformWs;
				m_wsAnal = m_caches.MainCache.DefaultAnalWs;
				m_wsUser = m_caches.MainCache.DefaultUserWs;
				m_hvoSbWord = kSbWord;
				m_rootb = sandbox.RootBox;
			}

			#region IDisposable & Co. implementation
			// Region last reviewed: never

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
			~InterlinComboHandler()
			{
				Dispose(false);
				// The base class finalizer is called automatically.
			}

			/// <summary>
			///
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
					// Dispose managed resources here.
					if (m_comboList != null && (m_comboList is IDisposable) && (m_comboList as Control).Parent == null)
						(m_comboList as IDisposable).Dispose();
					else if (m_comboList is ComboListBox)
					{
						// It typically has a parent, the special form used to display it, so will not
						// get disposed by the above, but we do want to dispose it.
						(m_comboList as IDisposable).Dispose();
					}
					if (m_items != null)
						m_items.Clear(); // I've seen it contain ints or MorphItems.
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_rgvsli = null;
				m_caches = null;
				m_sandbox = null;
				m_rootb = null;
				m_items = null;
				m_comboList = null;

				m_isDisposed = true;
			}

			#endregion IDisposable & Co. implementation

			/// <summary>
			/// encapsulates the common behavior of items in an InterlinComboHandler combo list.
			/// </summary>
			internal class InterlinComboHandlerActionComboItem : HvoTssComboItem
			{
				EventHandler OnSelect;

				/// <summary>
				///
				/// </summary>
				/// <param name="tssDisplay">the tss used to display the text of the combo item.</param>
				/// <param name="select">the event delegate to be executed when this item is selected. By default,
				/// we send "this" InterlinComboHandlerActionComboItem as the event sender.</param>
				internal InterlinComboHandlerActionComboItem(ITsString tssDisplay, EventHandler select)
					: this(tssDisplay, select, 0, 0)
				{
				}

				/// <summary>
				///
				/// </summary>
				/// <param name="tssDisplay">the tss to display in the combo box.</param>
				/// <param name="select">the event to fire when this is selected</param>
				/// <param name="hvoPrimary">the hvo most closely associated with this item, 0 if none.</param>
				/// <param name="tag">id to resolve any further ambiguity associated with this item's hvo.</param>
				internal InterlinComboHandlerActionComboItem(ITsString tssDisplay, EventHandler select, int hvoPrimary, int tag)
					: base(hvoPrimary, tssDisplay, tag)
				{
					OnSelect = select;
				}

				/// <summary>
				/// If enabled, will do something if clicked.
				/// </summary>
				internal bool IsEnabled
				{
					get { return OnSelect != null; }
				}

				/// <summary>
				/// Do OnSelect if defined, and this item is enabled.
				/// By default, we send "this" InterlinComboHandlerActionComboItem as the event sender.
				/// </summary>
				internal protected virtual void OnSelectItem()
				{
					if (OnSelect != null && IsEnabled)
						OnSelect(this, EventArgs.Empty);
				}
			}

			/// <summary>
			/// Setup the properties for combo items that should appear disabled.
			/// </summary>
			/// <returns></returns>
			protected static ITsTextProps DisabledItemProperties()
			{
				return HighlightProperty(Color.LightGray);
			}

			/// <summary>
			/// Setup a property for a specified color.
			/// </summary>
			/// <returns></returns>
			protected static ITsTextProps HighlightProperty(System.Drawing.Color highlightColor)
			{
				int color = (int)CmObjectUi.RGB(highlightColor);
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
					(int)FwTextPropVar.ktpvDefault, color);
				return bldr.GetTextProps();
			}

			// Call this to create the appropriate subclass and set up the combo and return it.
			// May return null if no appropriate combo can be created at the current position.
			// Caller should hide all combos before calling, then
			// call Activate to add the combo to its controls (thus making it visible)
			// or display the ComboListBox if a non-null value
			// is returned.
			static internal IComboHandler MakeCombo(IVwSelection vwselNew, SandboxBase sandbox, bool fMouseDown)
			{
				// Figure what property is selected and create a suitable class if appropriate.
				int cvsli = vwselNew.CLevels(false);
				// CLevels includes the string property itself, but AllTextSelInfo doesn't need
				// it.
				cvsli--;

				// Out variables for AllTextSelInfo.
				int ihvoRoot;
				int tagTextProp;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				// Main array of information retrived from sel that made combo.
				SelLevInfo[] rgvsli;

				// Analysis can now be zero (e.g., displaying alterate case form for non-existent WfiWordform)
				// and I don't believe it's a problem for the code below (JohnT).
				//				if (sandbox.Analysis == 0)
				//				{
				//					// We aren't fully initialized yet, so don't do anything.
				//					return null;
				//				}
				if (cvsli < 0)
					return null;
				try
				{
					rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
						out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
				}
				catch
				{
					// If anything goes wrong just give up.
					return null;
				}

				int hvoMorph = 0;
				int hvoSelObject = 0;
				if (tagTextProp >= ktagMinIcon && tagTextProp < ktagLimIcon) // its an icon
				{
					// If we're just hovering don't launch the pull-down.
					if (!fMouseDown)
						return null;
					if (rgvsli.Length >= 1)
						hvoMorph = hvoSelObject = rgvsli[0].hvo;
					return MakeCombo(tagTextProp, sandbox, hvoMorph, rgvsli, hvoSelObject);
				}
				return null;
			}

			/// <summary>
			/// make a combo handler based upon the given comboIcon and morph
			/// </summary>
			/// <param name="tagComboIcon"></param>
			/// <param name="sandbox"></param>
			/// <param name="hvoMorph"></param>
			/// <returns></returns>
			public static IComboHandler MakeCombo(int tagComboIcon, SandboxBase sandbox, int hvoMorph)
			{
				return MakeCombo(tagComboIcon, sandbox, hvoMorph, null, 0);
			}

			private static IComboHandler MakeCombo(int tagComboIcon, SandboxBase sandbox, int hvoMorph, SelLevInfo[] rgvsli, int hvoSelObject)
			{
				IVwRootBox rootb = sandbox.RootBox;
				int hvoSbWord = sandbox.RootWordHvo;
				InterlinComboHandler handler = null;
				CachePair caches = sandbox.Caches;
				switch (tagComboIcon)
				{
					case ktagMorphFormIcon:
						handler = new IhMorphForm();
						break;
					case ktagMorphEntryIcon:
						handler = new IhMorphEntry();
						break;
					case ktagWordPosIcon:
						handler = new IhWordPos();
						break;
					case ktagAnalysisIcon:
						ComboListBox clb2 = new ComboListBox();
						clb2.StyleSheet = sandbox.StyleSheet;
						ChooseAnalysisHandler caHandler = new ChooseAnalysisHandler(
							caches.MainCache, hvoSbWord, sandbox.Analysis, clb2);
						caHandler.Owner = sandbox;
						caHandler.AnalysisChosen += new EventHandler(
							sandbox.Handle_AnalysisChosen);
						caHandler.SetupCombo();
						return caHandler;
					case ktagWordGlossIcon: // line 6, word gloss.
						if (sandbox.ShouldAddWordGlossToLexicon)
						{
							if (hvoMorph == 0)
							{
								// setup the first hvoMorph
								hvoMorph = caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, 0);
							}
							handler = new IhLexWordGloss();
						}
						else
						{
							handler = new IhWordGloss();
						}
						break;
					default:
						return null;
				}
				// Use the base class handler for most handlers. Override where needed.
				if (!(handler is IhWordPos))
				{
					ComboListBox clb = new ComboListBox();
					handler.m_comboList = clb;
					clb.SelectedIndexChanged += new EventHandler(
						handler.HandleComboSelChange);
					clb.SameItemSelected += new EventHandler(
						handler.HandleComboSelSame);
					// Since we may initialize with TsStrings, need to set WSF.
					handler.m_comboList.WritingSystemFactory =
						caches.MainCache.LanguageWritingSystemFactoryAccessor;
				}
				else
				{
					// REVIEW: Do we need to handle wsf for word POS combo?
				}
				handler.m_caches = caches;
				handler.m_hvoSelObject = hvoSelObject;
				handler.m_hvoSbWord = hvoSbWord;
				handler.m_hvoMorph = hvoMorph;
				//handler.m_iRoot = itwfic;
				handler.m_rgvsli = rgvsli;
				handler.m_rootb = rootb;
				handler.m_wsVern = sandbox.RawWordformWs;
				handler.m_wsAnal = caches.MainCache.DefaultAnalWs;
				handler.m_wsUser = caches.MainCache.DefaultUserWs;
				handler.m_sandbox = sandbox;
				handler.m_fUnderConstruction = true;
				handler.SetupCombo();
				if (handler.m_comboList != null)
					handler.m_comboList.StyleSheet = sandbox.StyleSheet;
				handler.m_fUnderConstruction = false;
				return handler;
			}

			/// <summary>
			/// Hide yourself.
			/// </summary>
			public void Hide()
			{
				CheckDisposed();

				HideCombo();
			}

			// If the handler is managing a combo box and it is visible hide it.
			// Likewise if it is a combo list.
			internal void HideCombo()
			{
				CheckDisposed();

				m_sandbox.Focus();
				ComboListBox clb = m_comboList as ComboListBox;
				if (clb != null)
					clb.HideForm();
			}

			// Activate the combo-handler's control.
			// If the control is a combo make it visible at the indicated location.
			// If it is a ComboListBox pop it up at the relevant place for the indicated
			// location.
			public virtual void Activate(SIL.FieldWorks.Common.Utils.Rect loc)
			{
				CheckDisposed();

				AdjustListBoxSize();
				ComboListBox c = (m_comboList as ComboListBox);
				c.AdjustSize(500, 400); // these are maximums!
				c.Launch(m_sandbox.RectangleToScreen(loc),
					Screen.GetWorkingArea(m_sandbox));
			}

			internal void AdjustListBoxSize()
			{
				CheckDisposed();

				if (m_comboList is ComboListBox)
				{
					ComboListBox clb = m_comboList as ComboListBox;
					System.Drawing.Graphics g = m_sandbox.CreateGraphics();
					int nMaxWidth = 0;
					int nHeight = 0;
					IEnumerator ie = clb.Items.GetEnumerator();
					while (ie.MoveNext())
					{
						string s = null;
						if (ie.Current is ITsString)
						{
							ITsString tss = ie.Current as ITsString;
							s = tss.Text;
						}
						else if (ie.Current is String)
						{
							s = ie.Current as string;
						}
						if (s != null)
						{
							SizeF szf = g.MeasureString(s, clb.Font);
							int nWidth = (int)szf.Width + 2;
							if (nMaxWidth < nWidth)
								// 2 is not quite enough for height if you have homograph
								// subscripts.
								nMaxWidth = nWidth;
							nHeight += (int)szf.Height + 3;
						}
					}
					clb.Form.Width = Math.Max(clb.Form.Width, nMaxWidth);
					clb.Form.Height = Math.Max(clb.Form.Height, nHeight);
					g.Dispose();
					g = null;
				}
			}

			public int SelectedMorphHvo
			{
				get
				{
					CheckDisposed();
					return m_hvoMorph;
				}
			}

			// Return true if handled, otherwise, default behavior.
			public virtual bool HandleReturnKey()
			{
				CheckDisposed();

				return false;
			}

			// Handles a change in the item selected in the combo box.
			// Sub-classes can override where needed.
			internal virtual void HandleComboSelChange(object sender, EventArgs ea)
			{
				CheckDisposed();

				// Revisit (EricP): we could reimplement m_sandbox.HandleComboSelChange
				// here, but I suppose duplicating the logic here isn't necessary.
				// For now just use that one.
				m_sandbox.HandleComboSelChange(sender, ea);
				// Alternative re-implementation:
				//	if (m_fUnderConstruction)
				//		return;
				//	this.HideCombo();
				//	HandleSelectIfActive();
			}

			// Handles an item in the combo box when it is the same.
			// Sub-classes can override where needed.
			internal virtual void HandleComboSelSame(object sender, EventArgs ea)
			{
				CheckDisposed();

				// by default, just do the same as when item selected has changed.
				this.HandleComboSelChange(sender, ea);
			}

			/// <summary>
			/// Handle the user selecting an item in the control.
			/// </summary>
			public virtual void HandleSelectIfActive()
			{
				CheckDisposed();

				if (!m_fUnderConstruction)
					HandleSelect(m_comboList.SelectedIndex);
				m_sandbox.Focus();
			}
			// Handle the user selecting an item in the combo box.
			// Todo JohnT: many of the overrides should probably create a new selection.
			// The caller first hides the combo, so it can be manipulated in various
			// ways and possibly shown in a new place. Method should redisplay it if
			// appropriate.
			public virtual void HandleSelect(int index)
			{
				CheckDisposed();

			}

			/// <summary>
			/// select the combo list item matching the given string
			/// </summary>
			/// <param name="target"></param>
			public virtual void SelectComboItem(string target)
			{
				int index;
				object foundItem = GetComboItem(target, out index);
				if (foundItem != null)
				{
					HandleSelect(index);
				}
			}

			internal object GetComboItem(string target, out int index)
			{
				object foundItem = null;
				index = 0;
				if (m_comboList != null)
				{
					foreach (object item in m_comboList.Items)
					{
						if (((item is ITsString) && (item as ITsString).Text == target) ||
							(item is ITssValue) && (item as ITssValue).AsTss.Text == target)
						{
							foundItem = item;
							break;
						}
						else if (item.Equals(target))
						{
							foundItem = item;
							break;
						}
						index++;
					}
				}
				else if (Items != null)
				{
					// if Items is a list of Possibility hvos, you can check against names.
					foreach (int hvo in Items)
					{
						ICmPossibility possibility =
							CmPossibility.CreateFromDBObject(m_caches.MainCache, hvo) as ICmPossibility;
						if (possibility != null && possibility.Name.BestAnalysisVernacularAlternative.Text == target)
						{
							foundItem = hvo;
							break;
						}
						index++;
					}
				}
				return foundItem;
			}

			/// <summary>
			/// select the combo item matching the given hvoTarget
			/// </summary>
			/// <param name="hvoTarget"></param>
			public virtual void SelectComboItem(int hvoTarget)
			{
				int index = 0;
				foreach (int item in Items)
				{
					if (item == hvoTarget)
					{
						HandleSelect(index);
						break;
					}
					index++;
				}
			}


			// This method contains the default SetupCombo functions, for the benefit of
			// classes that need to override without calling the immediate superclass,
			// but do want the general default behavior.
			internal void InitCombo()
			{
				CheckDisposed();

				m_items.Clear();
				m_comboList.Items.Clear();
				// Some SetupCombo methods alter this to DropDownList, which prevents editing,
				// but it's useful to have a set default. Note that this needs to be done each
				// time, because we reuse the combo, and changes in one location can affect others.
				m_comboList.DropDownStyle = ComboBoxStyle.DropDown;
			}

			/// <summary>
			/// Return the index of the currently selected item. Subclasses can override this
			/// method for finding the sandbox setting, so it can select and highlight
			/// that item in the list, rather than the default.
			/// </summary>
			/// <returns></returns>
			public virtual int IndexOfCurrentItem
			{
				get
				{
					if (m_comboList != null)
						return m_comboList.SelectedIndex;
					return -1;
				}
			}

			// Save extra information needed for other commands, and set the combo items.
			// Or, change m_comboList to a new ComboListBox. This will result in no combo box
			// being displayed.
			public virtual void SetupCombo()
			{
				CheckDisposed();

				InitCombo();
			}

			/// <summary>
			/// the hvos related to the parallel items in m_comboList.Items
			/// </summary>
			public virtual List<int> Items
			{
				get { return m_items; }
			}

			/// <summary>
			/// Handles the problem that an ITsString returns null (which works fine as a BSTR) when there
			/// are no characters. But in C#, null is not the same as an empty string.
			/// Also handles the possibility that the ITsString itself is null.
			/// </summary>
			/// <param name="tss"></param>
			/// <returns></returns>
			public static string StrFromTss(ITsString tss)
			{
				if (tss == null)
					return string.Empty;
				string result = tss.Text;
				if (result != null)
					return result;
				return string.Empty;
			}

			// Change the selection, keeping the higher levels of the current spec
			// from isliCopy onwards, and adding a new lowest level that has
			// cpropPrevious 0, and the specified tag and ihvo.
			// The selection made is an IP at the start of the property tagTextProp,
			// writing system ws, of the object thus specified.
			// (The selection is in the first and usually only root object.)
			internal void MakeNewSelection(int isliCopy, int tag, int ihvo, int tagTextProp, int ws)
			{
				CheckDisposed();

				SelLevInfo[] rgvsli = new SelLevInfo[m_rgvsli.Length - isliCopy + 1];
				for (int i = isliCopy; i < m_rgvsli.Length; i++)
					rgvsli[i - isliCopy + 1] = m_rgvsli[i];
				rgvsli[0].cpropPrevious = 0;
				rgvsli[0].ihvo = ihvo;
				rgvsli[0].tag = tag;

				// first and only root object; length and array of path to target object;
				// property, no previous occurrences, range 0 to 0, no ws, not assocPrev,
				// no other object for the other end,
				// no override text props, do make it the current active selection.
				m_rootb.MakeTextSelection(0, rgvsli.Length, rgvsli, tagTextProp,
					0, 0, 0, ws, false, -1, null, true);
			}

			/// <summary>
			///  Add to the combo list the items in property flidVec of object hvoOwner in the main cache.
			///  Add to m_comboList.items the ShortName of each item, and to m_items the hvo.
			/// </summary>
			/// <param name="hvoOwner"></param>
			/// <param name="flidVec"></param>
			internal void AddVectorToComboItems(int hvoOwner, int flidVec)
			{
				CheckDisposed();

				int citem = m_caches.MainCache.GetVectorSize(hvoOwner, flidVec);

				for (int i = 0; i < citem; i++)
				{
					int hvoItem = m_caches.MainCache.GetVectorItem(hvoOwner, flidVec, i);
					m_items.Add(hvoItem);
					m_comboList.Items.Add(CmObject.CreateFromDBObject(m_caches.MainCache, hvoItem).ShortName);
				}
			}

			internal void AddPartsOfSpeechToComboItems()
			{
				CheckDisposed();

				AddVectorToComboItems(m_caches.MainCache.LangProject.PartsOfSpeechOA.Hvo,
					(int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities);
			}

			internal int MorphCount
			{
				get
				{
					CheckDisposed();
					return m_sandbox.MorphCount;
				}

			}
			internal int MorphHvo(int i)
			{
				CheckDisposed();

				return m_caches.DataAccess.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, i);
			}

			internal ITsString NewAnalysisString(string str)
			{
				CheckDisposed();

				return TsStrFactoryClass.Create().
					MakeString(str, m_caches.MainCache.DefaultAnalWs);
			}
			/// <summary>
			/// Synchronize the word gloss and POS with the morpheme gloss and MSA info, to the extent possible.
			/// Currently works FROM the morpheme TO the Word, but going the other way may be useful, too.
			///
			/// for the word gloss:
			///		- if only one morpheme, copy sense gloss to word gloss
			///		- if multiple morphemes, copy first stem gloss to word gloss, but only if word gloss is empty.
			///	for the POS:
			///		- if there is more than one stem and they have different parts of speech, do nothing.
			///		- if there is more than one derivational affix (DA), do nothing.
			///		- otherwise, if there is no DA, use the POS of the stem.
			///		- if there is no stem, do nothing.
			///		- if there is a DA, use its 'to' POS.
			///			(currently we don't insist that the 'from' POS matches the stem)
			/// </summary>
			internal void SyncMonomorphemicGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
			{
				CheckDisposed();
				if (!fCopyToWordGloss && !fCopyToWordPos)
					return;

				ISilDataAccess sda = m_caches.DataAccess;
				int cmorphs = sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				int hvoSbRootSense = 0;
				int hvoStemPos = 0; // ID in real database of part-of-speech of stem.
				bool fGiveUpOnPOS = false;
				int hvoDerivedPos = 0; // real ID of POS output of derivational MSA.
				for (int imorph = 0; imorph < cmorphs; imorph++)
				{
					int hvoMorph = sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
					int hvoSbSense = sda.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
					if (hvoSbSense == 0)
						continue; // Can't sync from morph sense to word if we don't have  morph sense.
					ILexSense sense = LexSense.CreateFromDBObject(m_caches.MainCache, m_caches.RealHvo(hvoSbSense));
					IMoMorphSynAnalysis msa = sense.MorphoSyntaxAnalysisRA;

					//					ITsString prefix = sda.get_StringProp(hvoMorph, ktagSbMorphPrefix);
					//					ITsString suffix = sda.get_StringProp(hvoMorph, ktagSbMorphPostfix);
					//					bool fStem = prefix.Length == 0 && suffix.Length == 0;

					bool fStem = msa is IMoStemMsa;

					// If we have only one morpheme, treat it as the stem from which we will copy the gloss.
					// otherwise, use the first stem we find, if any.
					if ((fStem && hvoSbRootSense == 0) || cmorphs == 1)
						hvoSbRootSense = hvoSbSense;

					if (fStem)
					{
						int hvoPOS = (msa as IMoStemMsa).PartOfSpeechRAHvo;
						if (hvoPOS != hvoStemPos && hvoStemPos != 0)
						{
							// found conflicting stems
							fGiveUpOnPOS = true;
						}
						else
							hvoStemPos = hvoPOS;
					}
					else if (msa is IMoDerivAffMsa)
					{
						if (hvoDerivedPos != 0)
							fGiveUpOnPOS = true; // more than one DA
						else
							hvoDerivedPos = (msa as IMoDerivAffMsa).ToPartOfSpeechRAHvo;
					}
				}

				// If we found a sense to copy from, do it.  Replace the word gloss even there already is
				// one, since users get confused/frustrated if we don't.  (See LT-6141.)  It's marked as a
				// guess after all!
				CopySenseToWordGloss(fCopyToWordGloss, hvoSbRootSense);

				// If we didn't find a stem, we don't have enough information to find a POS.
				if (hvoStemPos == 0)
					fGiveUpOnPOS = true;

				int hvoLexPos = 0;
				if (!fGiveUpOnPOS)
				{
					if (hvoDerivedPos != 0)
						hvoLexPos = hvoDerivedPos;
					else
						hvoLexPos = hvoStemPos;
				}
				CopyLexPosToWordPos(fCopyToWordPos, hvoLexPos);
			}

			protected virtual void CopySenseToWordGloss(bool fCopyWordGloss, int hvoSbRootSense)
			{
				if (hvoSbRootSense != 0 && fCopyWordGloss)
				{
					ISilDataAccess sda = m_caches.DataAccess;
					m_caches.DataAccess.SetInt(m_hvoSbWord, ktagSbWordGlossGuess, 1);
					int hvoRealSense = m_caches.RealHvo(hvoSbRootSense);
					foreach (int wsId in m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
					{
						// Update the guess, by copying the glosses of the SbNamedObj representing the sense
						// to the word gloss property.
						//ITsString tssGloss = sda.get_MultiStringAlt(hvoSbRootSense, ktagSbNamedObjName, wsId);
						// No, it is safer to copy from the real sense. We may be displaying more WSS for the word than the sense.
						ITsString tssGloss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(hvoRealSense, (int)LexSense.LexSenseTags.kflidGloss, wsId);
						sda.SetMultiStringAlt(m_hvoSbWord, ktagSbWordGloss, wsId, tssGloss);
						sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, ktagSbWordGloss,
							wsId, 0, 0);
					}
				}
			}
			protected virtual int CopyLexPosToWordPos(bool fCopyToWordCat, int hvoMsaPos)
			{
				int hvoPos = 0;
				if (fCopyToWordCat && hvoMsaPos != 0)
				{
					// got the one we want, in the real database. Make a corresponding sandbox one
					// and install it as a guess
					hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvoMsaPos,
						(int)CmPossibility.CmPossibilityTags.kflidAbbreviation);
					int hvoSbWordPos = m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos);
					m_caches.DataAccess.SetObjProp(m_hvoSbWord, ktagSbWordPos, hvoPos);
					m_caches.DataAccess.SetInt(hvoPos, ktagSbNamedObjGuess, 1);
					m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
						ktagSbWordPos, 0, 1, (hvoSbWordPos == 0 ? 0 : 1));
				}
				return hvoPos;
			}
		}

		/// <summary>
		/// The actual form of the word. Eventually we will offer a popup representing all the
		/// currently known possible analyses, and other options.
		/// </summary>
		internal class IhSbWordForm : InterlinComboHandler
		{
			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();
				m_comboList.Items.Add(ITextStrings.ksAcceptEntireAnalysis);
				m_comboList.Items.Add(ITextStrings.ksEditThisWordform);
				m_comboList.Items.Add(ITextStrings.ksDeleteThisWordform);
				// These aren't likely to get implemented soon.
				//m_comboList.Items.Add("Change spelling of occurrences");
				//m_comboList.Items.Add("Concordance");
				//// following not valid, don't know how in .NET, maybe Add("-")?
				//m_comboList.Items.AddSeparator();
				//m_comboList.Add("Interlinear help");

				m_comboList.DropDownStyle = ComboBoxStyle.DropDownList; // Prevents direct editing.
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();

				switch (index)
				{
					case 0: // Accept entire analysis
						// Todo: figure how to implement.
						break;
					case 1: // Edit this wordform.
						// Allows direct editing.
						m_comboList.DropDownStyle = ComboBoxStyle.DropDown;
						// restore the combo to visibility so we can do the editing.
						m_sandbox.ShowCombo();
						break;
					case 2: // Delete this wordform.
						// Todo: figure implementation
						//					int ihvoTwfic = m_rgvsli[m_iRoot].ihvo;
						//					int [] itemsToInsert = new int[0];
						//					m_fdoCache.ReplaceReferenceProperty(m_hvoSbWord,
						//						(int)StTxtPara.StTxtParaTags.kflidAnalyzedTextObjects,
						//						ihvoTwfic, ihvoTwfic + 1, ref itemsToInsert);
						// Enhance JohnT: consider removing the WfiWordform, if there are no
						// analyses and no other references.
						// Comment: RandyR: Please don't delete it.
						break;
				}
			}

			public override bool HandleReturnKey()
			{
				CheckDisposed();

				// If it hasn't changed don't do anything.
				string newval = m_comboList.Text;
				if (newval == StrFromTss(m_caches.DataAccess.get_MultiStringAlt(m_hvoSbWord, ktagSbWordForm, m_sandbox.RawWordformWs)))
				{
					return true;
				}
				ITsString tssWord = TsStrFactoryClass.Create().MakeString(newval,
					m_sandbox.RawWordformWs);
				// Todo JohnT: clean out old analysis, come up with new defaults.
				//SetAnalysisTo(DbOps.FindOrCreateWordform(m_fdoCache, tssWord));
				// Enhance JohnT: consider removing the old WfiWordform, if there are no
				// analyses and no other references.
				return true;
			}
		}

		internal class IhMorphForm : InterlinComboHandler
		{
			internal IhMorphForm()
				: base()
			{
			}
			internal IhMorphForm(SandboxBase sandbox)
				: base(sandbox)
			{
			}

			public override int IndexOfCurrentItem
			{
				get
				{
					return 0; // Treat the first item as the selected item.
				}
			}

			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();
				// Any time we pop this up, the text in the box is the text form of the current
				// analysis, as a starting point.
				ITsStrBldr builder = TsStrBldrClass.Create();
				int cmorphs = MorphCount;
				Debug.Assert(cmorphs != 0); // we're supposed to be building on one of them!

				int hvoWordform = m_sandbox.GetWordformHvoOfAnalysis();
				int hvoAnalysis = m_sandbox.GetWfiAnalysisHvoInUse();

				// Find the actual original form of the current wordform
				ITsString tssForm = m_sandbox.FindAFullWordForm(hvoWordform);
				string form = StrFromTss(tssForm);
				bool fBaseWordIsPhrase = SandboxBase.IsPhrase(form);

				// First, store the current morph breakdown if we have one,
				// Otherwise, if the user has deleted all the morphemes on the morpheme line
				// (per LT-1621) simply use the original wordform.
				// NOTE: Normally we would use Sandbox.IsMorphFormLineEmpty for this condition
				// but since we're already using the variable(s) needed for this check,
				// here we'll use those variables for economy/performance instead.
				string currentBreakdown = m_sandbox.MorphManager.BuildCurrentMorphsString();
				if (currentBreakdown != string.Empty)
				{
					m_comboList.Text = currentBreakdown;
					// The above and every other distinct morpheme breakdown from owned
					// WfiAnalyses are possible choices.
					ITsString tssText = TsStrFactoryClass.Create().
						MakeString(currentBreakdown, m_wsVern);
					m_comboList.Items.Add(tssText);
				}
				else
				{
					m_comboList.Text = form;
					m_comboList.Items.Add(tssForm);
				}
				// if we added the fullWordform (or the current breakdown is somehow empty although we may have an analysis), then add the
				// wordform HVO; otherwise, add the analysis HVO.
				if (currentBreakdown == string.Empty || (hvoAnalysis == 0 && tssForm != null && tssForm.Equals(m_comboList.Items[0] as ITsString)))
					m_items.Add(hvoWordform);
				else
					m_items.Add(hvoAnalysis);	// [wfi] hvoAnalysis may equal '0' (for annotations that are instances of Wordform).
				Debug.Assert(m_items.Count == m_comboList.Items.Count,
					"combo list (m_comboList) should contain the same count as the m_items list (hvos)");
				AddAnalysesOf(hvoWordform, fBaseWordIsPhrase);
				// Add the original wordform, if not already present.
				AddIfNotPresent(tssForm, hvoWordform);
				m_comboList.SelectedIndex = this.IndexOfCurrentItem;

				// Add any relevant 'other case' forms.
				int wsVern = m_sandbox.RawWordformWs;
				string locale = m_caches.MainCache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsVern).IcuLocale;
				CaseFunctions cf = new CaseFunctions(locale);
				switch (m_sandbox.CaseStatus)
				{
					case StringCaseStatus.allLower:
						break; // no more to add
					case StringCaseStatus.title:
						AddOtherCase(cf.SwitchTitleAndLower(form));
						break;
					case StringCaseStatus.mixed:
						switch (cf.StringCase(form))
						{
							case StringCaseStatus.allLower:
								AddOtherCase(cf.ToTitle(form));
								AddOtherCase(m_sandbox.RawWordform.Text);
								break;
							case StringCaseStatus.title:
								AddOtherCase(cf.ToLower(form));
								AddOtherCase(m_sandbox.RawWordform.Text);
								break;
							case StringCaseStatus.mixed:
								AddOtherCase(cf.ToLower(form));
								AddOtherCase(cf.ToTitle(form));
								break;
						}
						break;
				}
				Debug.Assert(m_items.Count == m_comboList.Items.Count,
					"combo list (m_comboList) should contain the same count as the m_items list (hvos)");
				m_comboList.Items.Add(ITextStrings.ksEditMorphBreaks_);
			}

			/// <summary>
			/// Add to the combo the specified alternate-case form of the word.
			/// </summary>
			/// <param name="other"></param>
			void AddOtherCase(string other)
			{
				// 0 is a reserved value for other case wordform
				AddIfNotPresent(StringUtils.MakeTss(other, m_sandbox.RawWordformWs), 0);
			}

			/// <summary>
			/// Add to the combo the analyses of the specified wordform (that don't already occur).
			/// REFACTOR : possibly could refactor with MorphManager.BuildCurrentMorphsString
			/// </summary>
			/// <param name="hvoWordform"></param>
			private void AddAnalysesOf(int hvoWordform, bool fBaseWordIsPhrase)
			{
				if (hvoWordform == 0)
					return; // no real wordform, can't have analyses.
				ITsStrBldr builder = TsStrBldrClass.Create();
				ITsString space = TsStrFactoryClass.Create().
					MakeString(fBaseWordIsPhrase ? "  " : " ", m_wsVern);
				foreach (int hvoAnal in m_caches.MainCache.GetVectorProperty(hvoWordform,
					(int)WfiWordform.WfiWordformTags.kflidAnalyses, false))
				{
					IWfiAnalysis wa = (IWfiAnalysis)WfiAnalysis.CreateFromDBObject(
						m_caches.MainCache, hvoAnal,
						CmObject.GetTypeFromFullClassName(m_caches.MainCache,
						"SIL.FieldWorks.FDO.Ling.WfiAnalysis"), false, false);
					Opinions o = wa.GetAgentOpinion(
						m_caches.MainCache.LangProject.DefaultUserAgent);
					if (o == Opinions.disapproves)
						continue;	// skip any analysis the user has disapproved.
					int cmorphs = m_caches.MainCache.GetVectorSize(hvoAnal,
						(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles);
					if (cmorphs == 0)
						continue;
					builder.Clear();
					for (int imorph = 0; imorph < cmorphs; ++imorph)
					{
						if (imorph != 0)
							builder.ReplaceTsString(builder.Length, builder.Length,
								space);
						int hvoMorphBundle = m_caches.MainCache.GetVectorItem(hvoAnal,
							(int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, imorph);
						int hvoMorph = m_caches.MainCache.GetObjProperty(hvoMorphBundle,
							(int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph);
						if (hvoMorph != 0)
						{
							ITsString tss =
								m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(
								hvoMorph, (int)MoForm.MoFormTags.kflidForm,
								m_sandbox.RawWordformWs);
							int hvoType = m_caches.MainCache.GetObjProperty(hvoMorph,
								(int)MoForm.MoFormTags.kflidMorphType);
							string sPrefix =
								m_caches.MainCache.MainCacheAccessor.get_UnicodeProp(hvoType,
								(int)MoMorphType.MoMorphTypeTags.kflidPrefix);
							string sPostfix =
								m_caches.MainCache.MainCacheAccessor.get_UnicodeProp(hvoType,
								(int)MoMorphType.MoMorphTypeTags.kflidPostfix);
							int ich = builder.Length;
							builder.ReplaceTsString(ich, ich, tss);
							if (sPrefix != null && sPrefix.Length != 0)
								builder.Replace(ich, ich, sPrefix, null);
							if (sPostfix != null && sPostfix.Length != 0)
								builder.Replace(builder.Length, builder.Length,
									sPostfix, null);
						}
						else
						{
							// No MoMorph object?  must be the Form string.
							ITsString tss =
								m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(
								hvoMorphBundle,
								(int)WfiMorphBundle.WfiMorphBundleTags.kflidForm,
								m_sandbox.RawWordformWs);
							builder.ReplaceTsString(builder.Length, builder.Length,
								tss);
						}
					}
					ITsString tssAnal = builder.GetString();
					// Add only non-whitespace morpheme breakdowns.
					if (tssAnal.Length > 0 && tssAnal.Text.Trim().Length > 0)
						AddIfNotPresent(tssAnal, hvoAnal);
				}
			}

			/// <summary>
			/// Add an item to the combo unless it is already present.
			/// </summary>
			/// <param name="tssAnal"></param>
			/// <param name="hvoAnal"></param>
			void AddIfNotPresent(ITsString tssAnal, int hvoAnal)
			{
				// Can't use m_comboList.Items.Contains() because it doesn't use our Equals
				// function and just notes that all the TsStrings are different objects.
				bool fFound = false;
				foreach (ITsString tss in m_comboList.Items)
				{
					if (tss.Equals(tssAnal))
					{
						fFound = true;
						break;
					}
				}
				if (!fFound)
				{
					m_comboList.Items.Add(tssAnal);
					m_items.Add(hvoAnal);
				}

			}

			public override bool HandleReturnKey()
			{
				CheckDisposed();

				IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
				ISilDataAccess sda = m_caches.DataAccess;
				int cmorphs = MorphCount;
				// JohnT: 0 is fine, that's what we see for a word which has no known analyses and
				// shows up as *** on the morphs line.
				//Debug.Assert(cmorphs != 0);
				for (int imorph = 0; imorph < cmorphs; ++imorph)
				{
					int hvoMbSec = MorphHvo(imorph);
					// Erase all the information.
					cda.CacheObjProp(hvoMbSec, ktagSbMorphForm, 0);
					cda.CacheObjProp(hvoMbSec, ktagSbMorphEntry, 0);
					cda.CacheObjProp(hvoMbSec, ktagSbMorphGloss, 0);
					cda.CacheObjProp(hvoMbSec, ktagSbMorphPos, 0);
					cda.CacheStringProp(hvoMbSec, ktagSbMorphPrefix, null);
					cda.CacheStringProp(hvoMbSec, ktagSbMorphPostfix, null);
					// Send notifiers for each of these deleted items.
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphForm, 0, 1, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphEntry, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphGloss, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphPos, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphPrefix, 0, 0, 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoMbSec, ktagSbMorphPostfix, 0, 0, 1);
				}
				// Now erase the morph bundles themselves.
				cda.CacheVecProp(m_hvoSbWord, ktagSbWordMorphs, new int[0], 0);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoSbWord, ktagSbWordMorphs, 0, 0, 1);

				MorphemeBreaker mb = new MorphemeBreaker(m_caches, m_comboList.Text,
					m_hvoSbWord, m_wsVern, m_sandbox);
				mb.Run();
				m_rootb.Reconstruct(); // Everything changed, more or less.
				// Todo: having called reconstruct, selection is invalid, may have to do
				// something special about making a new one.
				return true;
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();

				string sMorphs = null;
				if (index >= m_items.Count)
				{
					// The user did not choose an existing set of morph breaks, which means that
					// he wants to bring up a dialog to edit the morph breaks manually.
					sMorphs = EditMorphBreaks();
				}
				else
				{
					// user selected an existing set of morph breaks.
					ITsString menuItemForm = (m_comboList.Items[index]) as ITsString;
					Debug.Assert(menuItemForm != null, "menu item should be TsString");
					int hvoAnal = m_items[index];
					if (hvoAnal == 0)
					{
						// We're looking at an alternate case form of the whole word.
						// Switch the sandbox to the corresponding form.
						m_sandbox.SetWordform(menuItemForm, true);
						return;
					}
					else
					{
						// use the new morph break down.
						sMorphs = Sandbox.InterlinComboHandler.StrFromTss(menuItemForm);
					}
				}
				UpdateMorphBreaks(sMorphs);
				m_sandbox.SelectIconOfMorph(0, ktagMorphFormIcon);
			}

			internal void UpdateMorphBreaks(string sMorphs)
			{
				if (sMorphs != null && sMorphs.Trim().Length > 0)
					sMorphs = sMorphs.Trim();
				else
					return;

				ISilDataAccess sda = m_caches.DataAccess;
				IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;

				// Compare to the actual original form of the sandbox wordform
				int hvoWordform = m_sandbox.GetWordformHvoOfAnalysis();
				ITsString tssWordform = m_sandbox.FindAFullWordForm(hvoWordform);
				string wordform = StrFromTss(tssWordform);
				if (wordform == sMorphs)
				{
					// The only wordform choice in the list is the wordform of
					// some current analysis. We want to switch to that original wordform.
					// We do NOT want to look up the default, because that could well have an
					// existing morpheme breakdown, preventing us from getting back to the original
					// whole word.
					m_sandbox.SetWordform(tssWordform, false);
					return;
				}
				// We want to try to break this down into morphemes.
				// nb: use sda.Replace rather than cds.CachVecProp so that this registers as a change
				// in need of saving.
				int coldMorphs = sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				sda.Replace(m_hvoSbWord, ktagSbWordMorphs, 0, coldMorphs, new int[0], 0);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoSbWord, ktagSbWordMorphs, 0, 0, coldMorphs);
				MorphemeBreaker mb = new MorphemeBreaker(m_caches, sMorphs, m_hvoSbWord,
					m_wsVern, m_sandbox);
				mb.Run();
				m_rootb.Reconstruct(); // Everything changed, more or less.
				// We've changed properties that the morph manager cares about, but we don't want it
				// to fire when we fix the selection.
				m_sandbox.m_morphManager.NeedUpdate = false;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns>string of new morph breaks</returns>
			internal string EditMorphBreaks()
			{
				string sMorphs = null;
				ISilDataAccess sda = m_caches.DataAccess;
				using (EditMorphBreaksDlg dlg = new EditMorphBreaksDlg())
				{
					ITsString tssWord = m_sandbox.SbWordForm(m_sandbox.RawWordformWs);
					sMorphs = m_sandbox.MorphManager.BuildCurrentMorphsString();
					dlg.Initialize(tssWord, sMorphs, sda.WritingSystemFactory,
						m_caches.MainCache, m_sandbox.Mediator.StringTbl, m_sandbox.StyleSheet);
					Form mainWnd = m_sandbox.FindForm();
					// Making the form active fixes problems like LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
						sMorphs = dlg.GetMorphs();
					else
						sMorphs = null;
				}
				return sMorphs;
			}
		}

		/// <summary>
		/// This combo box appears in the same place in the view as the IhMorphForm one, but
		/// when an analysis is missing. Currently it has the same options
		/// as the IhMorphForm, but the process of building the combo is slightly different
		/// because the initial text is taken from the word form, not the morph forms. There
		/// may eventually be other differences, such as subtracting an item to delete the
		/// current analysis.
		/// </summary>
		internal class IhMissingMorphs : IhMorphForm
		{
			public override void SetupCombo()
			{
				CheckDisposed();

				InitCombo();
				m_comboList.Text = StrFromTss(m_caches.DataAccess.get_MultiStringAlt(m_hvoSbWord,
					ktagSbWordForm, m_sandbox.RawWordformWs));
				m_comboList.Items.Add(ITextStrings.ksEditMorphBreaks_);
			}
		}


		/// <summary>
		/// Handles the morpheme entry (LexEntry) line when none is known.
		/// </summary>
		internal class IhMissingEntry : InterlinComboHandler
		{
			ITsString m_tssMorphForm; // form of the morpheme when the combo was initialized.
			bool m_fHideCombo = true; // flag to HideCombo after HandleSelect.
			// int for all classes, except IhMissingEntry, which stuffs MorphItem data into it.
			// So, that ill-behaved class has to make its own m_items data member.
			List<MorphItem> m_morphItems = new List<MorphItem>();

			internal struct MorphItem : IComparable
			{
				public int m_hvoMorph;
				public int m_hvoEntry;
				public int m_hvoSense;
				public int m_hvoMsa;
				public ITsString m_name;
				public string m_nameSense;
				public string m_nameMsa;

				public MorphItem(int hvoMorph, ITsString tssName)
					: this(hvoMorph, 0, tssName)
				{
				}

				public MorphItem(int hvoMorph, int hvoEntry, ITsString tssName)
					: this(hvoMorph, hvoEntry, tssName, 0, null, 0, null)
				{
				}

				public MorphItem(int hvoMorph, ITsString tssName, int hvoSense, string nameSense, int hvoMsa, string nameMsa)
					: this(hvoMorph, 0, tssName, hvoSense, nameSense, hvoMsa, nameMsa)
				{
				}

				/// <summary>
				///
				/// </summary>
				/// <param name="hvoMorph"></param>
				/// <param name="hvoEntry">typically the owner of hvoMorph (or 0 if that's the case),
				/// but for variant specs, this could be hvoMorph's Entry.VariantEntryRef.ComponentLexeme target.</param>
				/// <param name="tssName"></param>
				/// <param name="hvoSense"></param>
				/// <param name="nameSense"></param>
				/// <param name="hvoMsa"></param>
				/// <param name="nameMsa"></param>
				public MorphItem(int hvoMorph, int hvoEntry, ITsString tssName, int hvoSense, string nameSense, int hvoMsa, string nameMsa)
				{
					m_hvoMorph = hvoMorph;
					m_hvoEntry = hvoEntry;
					m_name = tssName;
					m_hvoSense = hvoSense;
					m_nameSense = nameSense;
					m_hvoMsa = hvoMsa;
					m_nameMsa = nameMsa;
				}

				/// <summary>
				/// for variant relationships, return the primary entry
				/// (of which this morph is a variant). Otherwise,
				/// return the owning entry of the morph.
				/// </summary>
				/// <param name="cache"></param>
				/// <returns></returns>
				public int GetPrimaryOrOwningEntry(FdoCache cache)
				{
					int hvoMorphEntryReal;
					if (m_hvoEntry != 0)
					{
						// for variant relationships, we want to allow trying to create a
						// new sense on the entry of which we are a variant.
						hvoMorphEntryReal = m_hvoEntry;
					}
					else
					{
						hvoMorphEntryReal = cache.GetOwnerOfObject(m_hvoMorph);
					}
					return hvoMorphEntryReal;
				}

				#region IComparer Members

				/// <summary>
				/// make sure SetupCombo groups morph items according to lex name, sense,
				/// and msa names in that order. (LT-5848).
				/// </summary>
				/// <param name="x"></param>
				/// <param name="y"></param>
				/// <returns></returns>
				public int Compare(object x, object y)
				{
					MorphItem miX = (MorphItem)x;
					MorphItem miY = (MorphItem)y;

					// first compare the lex and sense names.
					int compareLexNames = String.Compare(miX.m_name.Text, miY.m_name.Text);
					if (compareLexNames != 0)
						return compareLexNames;
					// otherwise if the hvo's are the same, then we want the ones with senses to be higher.
					// when m_hvoSense equals '0' we want to insert "Add New Sense" for that lexEntry,
					// following all the other senses for that lexEntry.
					if (miX.m_hvoMorph == miY.m_hvoMorph)
					{
						if (miX.m_hvoSense == 0)
							return 1;
						else if (miY.m_hvoSense == 0)
							return -1;
					}
					// only compare sense names for the same morph
					if (miX.m_hvoMorph == miY.m_hvoMorph)
					{
						int compareSenseNames = String.Compare(miX.m_nameSense, miY.m_nameSense);
						if (compareSenseNames != 0)
							return compareSenseNames;
						return String.Compare(miX.m_nameMsa, miY.m_nameMsa);
					}
					// otherwise, try to regroup common lex morphs together.
					return miX.m_hvoMorph.CompareTo(miY.m_hvoMorph);
				}

				#endregion

				#region IComparable Members

				public int CompareTo(object obj)
				{
					return Compare(this, obj);
				}

				#endregion
			};

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
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				if (m_tssMorphForm != null)
					Marshal.ReleaseComObject(m_tssMorphForm);
				m_tssMorphForm = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			public override List<int> Items
			{
				get
				{
					if (m_items.Count == 0)
						SyncItemsToMorphItems();
					return base.Items;
				}
			}

			internal List<MorphItem> MorphItems
			{
				get { return m_morphItems; }
			}

			private void SyncItemsToMorphItems()
			{
				// re-populate the items with the most specific levels of analysis.
				m_items.Clear();
				foreach (MorphItem mi in m_morphItems)
				{
					if (mi.m_hvoSense > 0)
						m_items.Add(mi.m_hvoSense);
					else if (mi.m_hvoMorph > 0)
						m_items.Add(mi.m_hvoMorph);	// should be owned by LexEntry
					else
						throw new ArgumentException("invalid morphItem");
				}
			}

			void LoadMorphItems()
			{
				ISilDataAccess sda = m_caches.DataAccess;
				int hvoForm = sda.get_ObjectProp(m_hvoMorph, ktagSbMorphForm);
				m_tssMorphForm = sda.get_MultiStringAlt(hvoForm, ktagSbNamedObjName, m_sandbox.RawWordformWs);
				string sPrefix = StrFromTss(sda.get_StringProp(m_hvoMorph, ktagSbMorphPrefix));
				string sPostfix =
					StrFromTss(sda.get_StringProp(m_hvoMorph, ktagSbMorphPostfix));
				// If the morph is either a proclitic or an enclitic, then it can stand alone; it does not have to have any
				// prefix or postfix even when such is defined for proclitic and/or enclitc.  So we augment the query to allow
				// these two types to be found without the appropriate prefix or postfix.  See LT-8124.
				string sEnclitic = " OR mmt.Guid$='" + MoMorphType.kguidMorphEnclitic + "'";
				string sProclitic = " OR mmt.Guid$='" + MoMorphType.kguidMorphProclitic + "'";
				string sql = string.Format("select mf.[id] from LexEntry le" +
					" join MoForm_ mf" +
					" on le.[id] = mf.Owner$ and mf.OwnFlid$ in ({0},{1})" +
					" join MoForm_Form mff" +
					" on mff.obj = mf.[id] and mff.Txt = ? and mff.Ws = {2}" +
					// Restrict by Morph Type as well as Morph Form.
					" join MoMorphType_ mmt on mmt.[Id] = mf.MorphType " +
					"AND (mmt.Prefix = N'{3}'{4}) AND (mmt.Postfix = N'{5}'{6})",
					(int)LexEntry.LexEntryTags.kflidAlternateForms, (int)LexEntry.LexEntryTags.kflidLexemeForm,
					m_wsVern, sPrefix, (sPrefix == "") ? " OR mmt.Prefix IS NULL " + sEnclitic + sProclitic : "",
					sPostfix, (sPostfix == "") ? " OR mmt.Postfix IS NULL" + sEnclitic + sProclitic : "");
				List<int> hvoMorphs = DbOps.ReadIntsFromCommand(m_caches.MainCache, sql,
					StrFromTss(m_tssMorphForm));
				m_morphItems.Clear();
				foreach (int hvo in hvoMorphs)
				{
					int hvoLexEntry = m_caches.MainCache.GetOwnerOfObject(hvo);
					ILexEntry parentEntry = CmObject.CreateFromDBObject(m_caches.MainCache, hvoLexEntry) as ILexEntry;
					BuildMorphItemsFromEntry(hvo, parentEntry, null);

					int variantRefsFlid = FDOSequencePropertyVirtualHandler.GetInstalledHandlerTag(m_caches.MainCache, "LexEntry", "VariantEntryRefs");
					int[] variantRefHvos = m_caches.MainCache.GetVectorProperty(parentEntry.Hvo, variantRefsFlid, false);
					// next add morph items based on any variant entry references.
					foreach (LexEntryRef lef in new FdoObjectSet<LexEntryRef>(m_caches.MainCache, variantRefHvos, false))
					{
						// for now, just build morph items for variant EntryRefs having only one component
						// otherwise, it's ambiguous which component to use to build a WfiAnalysis with.
						if (lef.ComponentLexemesRS.Count != 1)
							continue;
						IVariantComponentLexeme component = lef.ComponentLexemesRS[0] as IVariantComponentLexeme;
						ILexEntry entryForMorphBundle = null;
						if (component.ClassID == LexEntry.kclsidLexEntry)
							entryForMorphBundle = component as ILexEntry;
						else if (component.ClassID == LexSense.kclsidLexSense)
						{
							int entryHvo = m_caches.MainCache.GetOwnerOfObjectOfClass(component.Hvo, LexEntry.kclsidLexEntry);
							entryForMorphBundle = LexEntry.CreateFromDBObject(m_caches.MainCache, entryHvo);
						}
						BuildMorphItemsFromEntry(hvo, entryForMorphBundle, lef);
					}
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="hvoMorph"></param>
			/// <param name="le">the entry used in the morph bundle (for sense info). typically
			/// this is an owner of hvoMorph, but if not, it most likely has hvoMorph linked as its variant.</param>
			/// <param name="fMorphIsVariantOfEntry">indicates whether the given morph is a variant of the given entry(le) or
			/// one of its senses</param>
			private void BuildMorphItemsFromEntry(int hvoMorph, ILexEntry le, ILexEntryRef ler)
			{
				IMoForm mf = MoForm.CreateFromDBObject(m_caches.MainCache, hvoMorph);
				int hvoLexEntry = 0;
				if (le != null)
					hvoLexEntry = le.Hvo;
				ITsString tssName = null;
				if (le != null)
				{
					tssName = InterlinDocChild.GetLexEntryTss(m_caches.MainCache, le.Hvo, m_wsVern, ler);
				}
				else
				{
					// looks like we're not in a good state, so just use the form for the name.
					int wsActual;
					tssName = mf.Form.GetAlternativeOrBestTss(m_wsVern, out wsActual);
				}
				string sql2 = string.Format("select SenseId from fnGetSensesInEntry$({0})", hvoLexEntry);
				List<int> hvoSenses = DbOps.ReadIntsFromCommand(m_caches.MainCache,
					sql2, null);
				if (hvoSenses.Count > 0)
				{
					// Populate morphItems with Sense/Msa level specifics
					for (int i = 0; i < hvoSenses.Count; ++i)
					{
						int hvoSense = hvoSenses[i];

						ITsString tssSense =
							m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(
							(int)hvoSense,
							(int)LexSense.LexSenseTags.kflidGloss,
							m_caches.MainCache.DefaultAnalWs);
						if (tssSense.Length == 0)
						{
							// If it doesn't have a gloss (e.g., from Categorised Entry), use the definition.
							tssSense = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(
								(int)hvoSense,
								(int)LexSense.LexSenseTags.kflidDefinition,
								m_caches.MainCache.DefaultAnalWs);
						}

						ILexSense sense = LexSense.CreateFromDBObject(
							m_caches.MainCache, (int)hvoSense);
						IMoMorphSynAnalysis msa = sense.MorphoSyntaxAnalysisRA;
						string msaText = null;
						if (msa != null)
							msaText = msa.InterlinearName;
						MorphItem mi = new MorphItem(hvoMorph, ler != null ? hvoLexEntry : 0, tssName,
							hvoSense, tssSense.Text, sense.MorphoSyntaxAnalysisRAHvo, msaText);
						m_morphItems.Add(mi);
					}
				}
				// Make a LexEntry level item
				m_morphItems.Add(new MorphItem(hvoMorph, ler != null ? hvoLexEntry : 0, tssName));
			}

			internal class MorphComboItem : InterlinComboHandlerActionComboItem
			{
				MorphItem m_mi;
				internal MorphComboItem(MorphItem mi, ITsString tssDisplay, EventHandler handleMorphComboItem, int hvoPrimary)
					: base(tssDisplay, handleMorphComboItem, hvoPrimary, 0)
				{
					m_mi = mi;
				}

				/// <summary>
				///
				/// </summary>
				internal MorphItem MorphItem
				{
					get { return m_mi; }
				}

			}

			// m_morphItems is a list of MorphItems, which contain both the main-cache hvo of the
			// MoForm with the right text in the m_wsVern alternative of its MoForm_Form, and
			// the main-cache hvo of each sense of that MoForm.  A sense hvo of 0 is used to
			// flag the "Add New Sense" line which ends each MoForm's list of sense.
			//
			// Items in the menu are the shortnames of the owning LexEntries, followed by.
			//
			/// <summary>
			///
			/// </summary>
			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();

				LoadMorphItems();
				AddMorphItemsToComboList();
				// DON'T ADD ANY MORE MorphItem OBJECTS TO m_morphItems AT THIS POINT!
				// The order of items added to m_comboList.Items below must match exactly the
				// switch statement at the beginning of HandleSelect().
				AddUnknownLexEntryToComboList();
				// If morphemes line is empty then make the Create New Entry
				// appear disabled (cf. LT-6480). If user tries to select this index,
				// we prevent the selection in our HandleSelect override.
				ITsTextProps disabledItemProperties = null;
				if (m_sandbox.IsMorphFormLineEmpty)
					disabledItemProperties = DisabledItemProperties();
				AddItemToComboList(ITextStrings.ksCreateNewEntry_,
					new EventHandler(OnSelectCreateNewEntry),
					disabledItemProperties,
					disabledItemProperties == null);
				AddItemToComboList(ITextStrings.ksVariantOf_,
					new EventHandler(OnSelectVariantOf),
					disabledItemProperties,
					disabledItemProperties == null);

				// If morphemes line is empty then make the allomorph selection,
				// appear disabled (cf. LT-1621). If user tries to select this index,
				// we prevent the selection in our HandleComboSelChange override.
				AddItemToComboList(ITextStrings.ksAllomorphOf_,
					new EventHandler(OnSelectAllomorphOf),
					disabledItemProperties,
					disabledItemProperties == null);

				// If the morpheme line is hidden, give the user the option to edit morph breaks.
				if (m_sandbox.m_choices.IndexOf(InterlinLineChoices.kflidMorphemes) < 0)
				{
					AddItemToComboList("-------", null, null, false);
					AddItemToComboList(ITextStrings.ksEditMorphBreaks_,
						new EventHandler(OnSelectEditMorphBreaks),
						null,
						true);
				}

				// Set combo selection to current selection.
				m_comboList.SelectedIndex = this.IndexOfCurrentItem;
			}

			private void AddMorphItemsToComboList()
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				ITsString tssHead = null;
				MorphItem miPrev = new MorphItem();
				m_morphItems.Sort();
				foreach (MorphItem mi in m_morphItems)
				{
					ITsString tssToDisplay = null;
					int hvoPrimary = 0; // the key hvo associated with the combo item.
					tisb.Clear();
					int hvoLexEntry = m_caches.MainCache.GetOwnerOfObject(mi.m_hvoMorph);
					ILexEntry le = new LexEntry(m_caches.MainCache, hvoLexEntry);
					tssHead = tisb.GetString();
					if (mi.m_hvoSense > 0)
					{
						int hvoSense = mi.m_hvoSense;
						tisb = tssHead.GetIncBldr();
						tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwSuperscriptVal.kssvOff);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
							m_wsAnal);
						tisb.Append("  ");

						ITsString tssSense = StringUtils.MakeTss(mi.m_nameSense,
							m_caches.MainCache.DefaultAnalWs);

						tisb.AppendTsString(tssSense);
						tisb.Append(", ");

						string sPos = mi.m_nameMsa;
						if (sPos == null)
							sPos = ITextStrings.ksQuestions;	// was "??", not "???"
						tisb.Append(sPos);
						tisb.Append(", ");

						// append lex entry form info
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
							m_wsVern);
						tisb.AppendTsString(mi.m_name);

						tssToDisplay = tisb.GetString();
						hvoPrimary = mi.m_hvoSense;
						tisb.Clear();
					}
					else
					{
						hvoPrimary = mi.m_hvoMorph;
						// mi.m_hvoSense == 0
						// Make a comboList item for adding a new sense to the LexEntry
						if (miPrev.m_hvoMorph != 0 && mi.m_hvoMorph == miPrev.m_hvoMorph &&
							miPrev.m_hvoSense > 0)
						{
							// "Add New Sense..."
							// the comboList has already added selections for senses and lexEntry form
							// thus establishing the LexEntry the user may wish to "Add New Sense..." to.
							tisb.Clear();
							tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwSuperscriptVal.kssvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwTextToggleVal.kttvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
								m_wsUser);
							tisb.Append(ITextStrings.ksAddNewSense_);
							tssToDisplay = tisb.GetString();

						}
						else
						{
							// "Add New Sense for {0}"
							// (EricP) This path means the current form matches an entry that (strangely enough)
							// doesn't have any senses so we need to add the LexEntry form into the string,
							// so the user knows what Entry they'll be adding the new sense to.
							Debug.Assert(le.SensesOS.Count == 0, "Expected LexEntry to have no senses.");
							string sFmt = ITextStrings.ksAddNewSenseForX_;
							tisb.Clear();
							tisb.SetIntPropValues(
								(int)FwTextPropType.ktptSuperscript,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwSuperscriptVal.kssvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptBold,
								(int)FwTextPropVar.ktpvEnum,
								(int)FwTextToggleVal.kttvOff);
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
								m_wsUser);
							tisb.Append(sFmt);
							ITsString tss = tisb.GetString();
							int ich = sFmt.IndexOf("{0}");
							if (ich >= 0)
							{
								ITsStrBldr tsbT = tss.GetBldr();
								tsbT.ReplaceTsString(ich, ich + "{0}".Length, mi.m_name);
								tss = tsbT.GetString();
							}
							tssToDisplay = tss;
						}
					}
					// keep track of the previous MorphItem to track context.
					m_comboList.Items.Add(new MorphComboItem(mi, tssToDisplay,
						new EventHandler(HandleSelectMorphComboItem), hvoPrimary));
					miPrev = mi;
				}
				SyncItemsToMorphItems();
			}

			private void AddUnknownLexEntryToComboList()
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.Clear();
				tisb.SetIntPropValues(
					(int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwSuperscriptVal.kssvOff);
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsUser);
				tisb.Append(ITextStrings.ksUnknown);
				m_comboList.Items.Add(new InterlinComboHandlerActionComboItem(
					tisb.GetString(), new EventHandler(SetLexEntryToUnknown)));
			}

			private void AddItemToComboList(string itemName, EventHandler onSelect, ITsTextProps itemProperties, bool enableItem)
			{
				ITsStrBldr tsb = TsStrBldrClass.Create();
				tsb.Replace(tsb.Length, tsb.Length, itemName, itemProperties);
				tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptWs, 0, m_wsUser);
				m_comboList.Items.Add(new InterlinComboHandlerActionComboItem(
					tsb.GetString(),
					enableItem ? onSelect : null));
			}

			/// <summary>
			/// Return the index corresponding to the current LexEntry/Sense state of the Sandbox.
			/// </summary>
			public override int IndexOfCurrentItem
			{
				get
				{
					// See if we can find the real hvo corresponding to the LexEntry/Sense currently
					// selected in the Sandbox.
					int sbHvo = m_sandbox.CurrentLexEntriesAnalysis(m_hvoMorph);
					int realHvo = m_sandbox.Caches.RealHvo(sbHvo);
					if (realHvo <= 0)
						return base.IndexOfCurrentItem;

					MoMorphSynAnalysis msa = null;
					// save the class id
					int classid = m_caches.MainCache.GetClassOfObject(realHvo);
					if (classid != LexSense.kclsidLexSense && classid != LexEntry.kclsidLexEntry)
						msa = new MoMorphSynAnalysis(m_caches.MainCache, realHvo);

					// Look through our relevant list items to see if we find a match.
					for (int i = 0; i < m_morphItems.Count; ++i)
					{
						MorphItem mi = m_morphItems[i];
						switch (classid)
						{
							case LexSense.kclsidLexSense:
								// See if we match the LexSense
								if (mi.m_hvoSense == realHvo)
									return i;
								break;
							case LexEntry.kclsidLexEntry:
								// Otherwise, see if our LexEntry matches MoForm's owner (also a LexEntry)
								int hvoMoFormReal = mi.m_hvoMorph;
								int hvoEntryReal = m_caches.MainCache.GetOwnerOfObject(hvoMoFormReal);
								if (hvoEntryReal == realHvo)
									return i;
								break;
							default:
								// See if we can match on the MSA
								if (msa != null && mi.m_hvoMsa == realHvo)
								{
									// verify the item sense is its owner
									ILexSense ls = new LexSense(m_caches.MainCache, mi.m_hvoSense);
									if (mi.m_hvoMsa == ls.MorphoSyntaxAnalysisRAHvo)
										return i;
								}
								break;
						}
					}
					return base.IndexOfCurrentItem;
				}
			}

			// This indicates there was a previous real LexEntry recorded. The 'real' subclass
			// overrides to answer 1. The value signifies the number of objects stored in the
			// ktagMorphEntry property before the user made a selection in the menu.
			internal virtual int WasReal()
			{
				CheckDisposed();

				return 0;
			}

			/// <summary>
			/// Run the dialog that allows the user to create a new LexEntry.
			/// </summary>
			private void RunCreateEntryDlg()
			{
				ILexEntry le;
				IMoForm allomorph;
				ILexSense sense;
				CreateNewEntry(false, out le, out allomorph, out sense);
			}

			internal void CreateNewEntry(bool fCreateNow, out ILexEntry le, out IMoForm allomorph, out ILexSense sense)
			{
				CheckDisposed();

				int hvoMorph = m_caches.DataAccess.get_ObjectProp(m_hvoMorph, ktagSbMorphForm);
				ITsString tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph,
					ktagSbNamedObjName, m_sandbox.RawWordformWs);
				ITsString tssFullForm = m_sandbox.GetFullMorphForm(m_hvoMorph);
				string form = StrFromTss(tssForm);
				string fullForm = StrFromTss(tssFullForm);

				le = null;
				allomorph = null;
				sense = null;
				int entryID = 0;
				int hvoSense = 0;
				FdoCache cache = m_caches.MainCache;
				// If we don't have a form or it isn't in a current vernacular writing system, give up.
				if (tssForm == null || tssForm.Length == 0 ||
					!LangProject.GetAllWritingSystems("all vernacular", m_caches.MainCache, null, 0, 0).Contains(StringUtils.GetWsOfRun(tssForm,0)))
				{
					return;
				}
				ITsString tssHeadWord = null;
				using (InsertEntryDlg dlg = InsertEntryNow.CreateInsertEntryDlg(fCreateNow))
				{
					SetupDlgToCreateEntry(dlg, tssFullForm, cache);
					dlg.ChangeUseSimilarToCreateAllomorph();
					bool fCreatedEntry = false;
					bool fCreateAllomorph = false;
					if (fCreateNow)
					{
						// just create a new entry based on the given information.
						dlg.CreateNewEntry();
					}
					else
					{
						// bring up the dialog so the user can make further decisions.
						Form mainWnd = m_sandbox.FindForm();
						// Making the form active fixes LT-2344 & LT-2345.
						// I'm (RandyR) not sure what adverse impact might show up by doing this.
						mainWnd.Activate();
						dlg.SetHelpTopic("khtpInsertEntryFromInterlinear");
						if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
							fCreateAllomorph = true;
					}
					dlg.GetDialogInfo(out entryID, out fCreatedEntry);
					if (!fCreatedEntry && !fCreateAllomorph)
						return;

					// Get the appropriate MoForm?
					FdoCache mainCache = m_caches.MainCache;
					le = LexEntry.CreateFromDBObject(mainCache, entryID);
					hvoSense = dlg.NewSenseId;
					if (hvoSense > 0)
						sense = new LexSense(mainCache, hvoSense);
					else if (fCreateAllomorph && le.SensesOS.Count > 0)
						sense = le.SensesOS.FirstItem;

					allomorph = le.FindMatchingAllomorph(tssForm);
					if (allomorph == null)
					{
						try
						{
							allomorph = MoForm.MakeMorph(mainCache, le, tssFullForm);
						}
						catch
						{
							// Try it without any reserved markers.
							allomorph = MoForm.MakeMorph(mainCache, le,
								StringUtils.MakeTss(MoForm.EnsureNoMarkers(fullForm, mainCache),
								m_sandbox.RawWordformWs));
						}
					}
					tssHeadWord = le.HeadWord;
					if (allomorph != null)
						UpdateMorphEntry(allomorph.Hvo, entryID, hvoSense);
				}
			}

			private void SetupDlgToCreateEntry(InsertEntryDlg dlg, ITsString tssFullForm, FdoCache cache)
			{
				dlg.SetDlgInfo(cache, tssFullForm, m_sandbox.Mediator);
				int cMorphs = m_caches.DataAccess.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				if (cMorphs == 1)
				{
					// Make this string the gloss of the dlg.
					ITsString tssGloss = m_sandbox.Caches.DataAccess.get_MultiStringAlt(
						m_sandbox.RootWordHvo, ktagSbWordGloss,
						m_sandbox.Caches.MainCache.DefaultAnalWs);
					int hvoSbPos = m_sandbox.Caches.DataAccess.get_ObjectProp(m_sandbox.RootWordHvo,
						ktagSbWordPos);
					int hvoRealPos = m_sandbox.Caches.RealHvo(hvoSbPos);
					dlg.Pos = hvoRealPos;
					dlg.TssGloss = tssGloss;
					// Also copy any other glosses we have.
					foreach (int ws in m_sandbox.Caches.MainCache.LangProject.CurAnalysisWssRS.HvoArray)
					{
						ITsString tss = m_sandbox.Caches.DataAccess.get_MultiStringAlt(m_sandbox.RootWordHvo, ktagSbWordGloss, ws);
						dlg.SetInitialGloss(ws, tss);
					}
				}
			}

			/// <summary>
			/// this dialog, dumbs down InsertEntryDlg, to use its states and logic for
			/// creating a new entry immediately without trying to do matching Entries.
			/// </summary>
			class InsertEntryNow : InsertEntryDlg
			{
				static internal InsertEntryDlg CreateInsertEntryDlg(bool fCreateEntryNow)
				{
					if (fCreateEntryNow)
						return new InsertEntryNow();
					else
						return new InsertEntryDlg();
				}

				/// <summary>
				/// skip updating matches, since this dialog is just for inserting a new entry.
				/// </summary>
				protected override void UpdateMatches()
				{
					// skip matchingEntries.ResetSearch
				}

				protected override void ReplaceMatchingEntriesControl()
				{
					// just remove the existing control (since we don't care about searching for matched items)
					ReplaceMatchingEntriesControl(null);
				}
			}

			internal void RunAddNewAllomorphDlg()
			{
				CheckDisposed();

				ITsString tssForm;
				ITsString tssFullForm;
				int hvoType;
				GetMorphInfo(out tssForm, out tssFullForm, out hvoType);

				using (AddAllomorphDlg dlg = new AddAllomorphDlg())
				{
					FdoCache cache = m_caches.MainCache;
					dlg.SetDlgInfo(cache, null, m_sandbox.Mediator, tssForm, hvoType);
					Form mainWnd = m_sandbox.FindForm();
					// Making the form active fixes LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
					{
						int entryID = dlg.SelectedID;
						Debug.Assert(entryID > 0);
						// OK, they chose an entry, but does it have an appropriate MoForm?
						ILexEntry le = LexEntry.CreateFromDBObject(cache, entryID);
						if (dlg.InconsistentType && le.LexemeFormOA != null)
						{
							IMoForm morphLe = le.LexemeFormOA;
							IMoMorphType mmtLe = morphLe.MorphTypeRA;
							IMoMorphType mmtNew = null;
							if (hvoType != 0)
							{
								mmtNew = MoMorphType.CreateFromDBObject(cache, hvoType);
							}
							string entryForm = null;
							ITsString tssHeadword = le.HeadWord;
							if (tssHeadword != null)
								entryForm = tssHeadword.Text;
							if (entryForm == null || entryForm == "")
								entryForm = ITextStrings.ksNoForm;
							string sNoMorphType = m_sandbox.Mediator.StringTbl.GetString(
								"NoMorphType", "DialogStrings");
							string sTypeLe;
							if (mmtLe != null)
								sTypeLe = mmtLe.Name.BestAnalysisAlternative.Text;
							else
								sTypeLe = sNoMorphType;
							string sTypeNew;
							if (mmtNew != null)
								sTypeNew = mmtNew.Name.BestAnalysisAlternative.Text;
							else
								sTypeNew = sNoMorphType;
							string msg1 = String.Format(ITextStrings.ksSelectedLexEntryXisaY,
								entryForm, sTypeLe);
							string msg2 = String.Format(ITextStrings.ksAreYouSureAddZtoX,
								sTypeNew, tssForm.Text);
							CreateAllomorphTypeMismatchDlg warnDlg = new CreateAllomorphTypeMismatchDlg();
							warnDlg.Warning = msg1;
							warnDlg.Question = msg2;
							switch (warnDlg.ShowDialog(mainWnd))
							{
								case DialogResult.No:
									return; // cancelled.
								case DialogResult.Yes:
									// Go ahead and create allomorph.
									// But first, we have to ensure an appropriate MSA exists.
									bool haveStemMSA = false;
									bool haveUnclassifiedMSA = false;
									foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
									{
										if (msa is IMoStemMsa)
											haveStemMSA = true;
										if (msa is IMoUnclassifiedAffixMsa)
											haveUnclassifiedMSA = true;
									}
									switch (MoMorphType.FindMorphTypeIndex(cache, mmtNew))
									{
										case MoMorphType.kmtBoundRoot:
										case MoMorphType.kmtBoundStem:
										case MoMorphType.kmtClitic:
										case MoMorphType.kmtEnclitic:
										case MoMorphType.kmtProclitic:
										case MoMorphType.kmtStem:
										case MoMorphType.kmtRoot:
										case MoMorphType.kmtParticle:
										case MoMorphType.kmtPhrase:
										case MoMorphType.kmtDiscontiguousPhrase:
											// Add a MoStemMsa, if needed.
											if (!haveStemMSA)
												le.MorphoSyntaxAnalysesOC.Add(new MoStemMsa());
											break;
										default:
											// Add a MoUnclassifiedAffixMsa, if needed.
											if (!haveUnclassifiedMSA)
												le.MorphoSyntaxAnalysesOC.Add(new MoUnclassifiedAffixMsa());
											break;
									}
									break;
								case DialogResult.Retry:
									// Rather arbitrarily we use this dialog result for the
									// Create New option.
									this.RunCreateEntryDlg();
									return;
								default:
									// treat as cancelled
									return;
							}
						}
						IMoForm allomorph = null;
						if (dlg.MatchingForm && !dlg.InconsistentType)
						{
							allomorph = le.FindMatchingAllomorph(tssForm);
							if (allomorph == null)
							{
								// We matched on the Lexeme Form, not on an alternate form.
								allomorph = MoForm.MakeMorph(cache, le, tssFullForm);
							}
						}
						else
						{
							allomorph = MoForm.MakeMorph(cache, le, tssFullForm);
						}
						Debug.Assert(allomorph != null);
						UpdateMorphEntry(allomorph.Hvo, entryID, 0);
					}
				}
			}


			private void GetMorphInfo(out ITsString tssForm, out ITsString tssFullForm, out int hvoType)
			{
				int hvoMorphReal;
				GetMorphInfo(out tssForm, out tssFullForm, out hvoMorphReal, out hvoType);
			}
			private void GetMorphInfo(out ITsString tssForm, out ITsString tssFullForm, out int hvoMorphReal, out int hvoType)
			{
				int hvoMorph = m_caches.DataAccess.get_ObjectProp(m_hvoMorph, ktagSbMorphForm);
				hvoMorphReal = m_caches.RealHvo(hvoMorph);
				ISilDataAccess sda = m_caches.DataAccess;
				tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph, ktagSbNamedObjName, m_sandbox.RawWordformWs);
				tssFullForm = m_sandbox.GetFullMorphForm(m_hvoMorph);
				string fullForm = tssFullForm.Text;
				hvoType = 0;
				if (hvoMorphReal != 0)
				{
					hvoType = m_caches.MainCache.GetObjProperty(hvoMorphReal,
						(int)MoForm.MoFormTags.kflidMorphType);
				}
				else
				{
					// if we don't have a form then we can't derive a type. (cf. LT-1621)
					if (fullForm == null || fullForm == string.Empty)
					{
						hvoType = 0;
					}
					else
					{
						// Find the type for this morpheme
						int clsidForm;
						string fullFormTmp = fullForm;
						MoMorphTypeCollection morphtypes = new MoMorphTypeCollection(m_caches.MainCache);
						IMoMorphType mmt = MoMorphType.FindMorphType(m_caches.MainCache, morphtypes,
							ref fullFormTmp, out clsidForm);
						hvoType = mmt.Hvo;
					}
				}
			}

			internal int RunAddNewSenseDlg(ITsString tssForm, int hvoEntry)
			{
				CheckDisposed();

				if (tssForm == null)
				{
					int hvoForm = m_caches.DataAccess.get_ObjectProp(m_hvoMorph,
						ktagSbMorphForm);
					tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoForm, ktagSbNamedObjName, m_sandbox.RawWordformWs);
				}
				int newSenseID = 0;
				// This 'using' system is important,
				// because it calls Dispose on the dlg,
				// when it goes out of scope.
				// Otherwise, it gets disposed when the GC gets around to it,
				// and that may not happen until the app closes,
				// which causes bad problems.
				using (AddNewSenseDlg dlg = new AddNewSenseDlg())
				{
					ILexEntry le = LexEntry.CreateFromDBObject(m_caches.MainCache, hvoEntry);
					dlg.SetDlgInfo(tssForm, le, m_sandbox.Mediator);
					Form mainWnd = m_sandbox.FindForm();
					// Making the form active fixes problems like LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
						dlg.GetDlgInfo(out newSenseID);
				}
				return newSenseID;
			}

			// Handles a change in the item selected in the combo box.
			// Some combo items (like "Allomorph of...") can be disabled under
			// certain conditions (cf. LT-1621), but still appear in the dropdown.
			// In those cases, we don't want to hide the combo, so let the HandleSelect
			// set m_fHideCombo (to false) for those items that user tried to select.
			internal override void HandleComboSelChange(object sender, EventArgs ea)
			{
				CheckDisposed();

				if (m_fUnderConstruction)
					return;
				this.HandleSelect(m_comboList.SelectedIndex);
				if (m_fHideCombo)
				{
					this.HideCombo();
				}
				else
				{
					// After skipping HideCombo, reset the flag
					m_fHideCombo = true;
				}
			}

			/// <summary>
			/// Return true if it is necessary to call HandleSelect even though we selected the
			/// current item.
			/// </summary>
			/// <returns></returns>
			bool NeedSelectSame()
			{
				if (m_comboList.SelectedIndex >= m_morphItems.Count || m_comboList.SelectedIndex < 0)
				{
					// This happens, for a reason I (JohnT) don't understand, when we launch a dialog from
					// one of these menu options using Enter, and then the dialog is also closed using enter.
					// If we return true the dialog can be launched twice.
					return false;
				}
				int sbHvo = m_sandbox.CurrentLexEntriesAnalysis(m_hvoMorph);
				int realHvo = m_sandbox.Caches.RealHvo(sbHvo);
				if (realHvo <= 0)
					return true; // nothing currently set, set whatever is current.
				int classid = m_caches.MainCache.GetClassOfObject(realHvo);
				MorphItem mi = m_morphItems[m_comboList.SelectedIndex];
				if (classid != LexSense.kclsidLexSense && mi.m_hvoSense != 0)
					return true; // item is a sense, and current value is not!
				// Review JohnT: are there any other cases where we should do it anyway?
				return false;
			}

			internal override void HandleComboSelSame(object sender, EventArgs ea)
			{
				CheckDisposed();

				// Just close the ComboBox, since nothing changed...unless we selected a sense item and all we
				// had was an entry or msa, or some similar special case.
				if (NeedSelectSame())
					this.HandleSelect(m_comboList.SelectedIndex);
				this.HideCombo();
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();
				int morphIndex = GetMorphIndex();
				// NOTE: m_comboList.SelectedItem does not get automatically set in (some) tests.
				// so we use index here.
				InterlinComboHandlerActionComboItem comboItem = m_comboList.Items[index] as InterlinComboHandlerActionComboItem;
				if (comboItem != null)
				{
					if (!comboItem.IsEnabled)
					{
						m_fHideCombo = false;
						return;
					}
					comboItem.OnSelectItem();
					if (!(comboItem is MorphComboItem))
						CopyLexEntryInfoToMonomorphemicWordGlossAndPos();
					SelectEntryIcon(morphIndex);
					return;
				}
			}

			private int GetMorphIndex()
			{
				int morphIndex = 0;
				ISilDataAccess sda = m_caches.DataAccess;
				int cmorphs = sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				for (; morphIndex < cmorphs; morphIndex++)
					if (sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, morphIndex) == m_hvoMorph)
						break;
				Debug.Assert(morphIndex < cmorphs);
				return morphIndex;
			}

			private void OnSelectCreateNewEntry(object sender, EventArgs args)
			{
				try
				{
					RunCreateEntryDlg();
				}
				catch (Exception exc)
				{
					MessageBox.Show(exc.Message, ITextStrings.ksCannotCreateNewEntry);
				}
			}

			private void OnSelectAllomorphOf(object sender, EventArgs args)
			{
				try
				{
					RunAddNewAllomorphDlg();
				}
				catch (Exception exc)
				{
					MessageBox.Show(exc.Message, ITextStrings.ksCannotAddAllomorph);
				}
			}

			private void OnSelectEditMorphBreaks(object sender, EventArgs args)
			{
				IhMorphForm handler = new IhMorphForm(m_sandbox);
				handler.UpdateMorphBreaks(handler.EditMorphBreaks()); // this should launch the dialog.
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="sender">should be the selected combo item.</param>
			/// <param name="args"></param>
			private void HandleSelectMorphComboItem(object sender, EventArgs args)
			{
				MorphComboItem mci = (MorphComboItem)sender;
				MorphItem mi = mci.MorphItem;
				int hvoMoFormReal = mi.m_hvoMorph;
				int hvoMorphEntryReal = 0;
				hvoMorphEntryReal = mi.GetPrimaryOrOwningEntry(m_caches.MainCache);
				ITsString tss = mi.m_name;
				bool fUpdateMorphEntry = true;
				if (mi.m_hvoSense == 0)
				{
					mi.m_hvoSense = RunAddNewSenseDlg(tss, hvoMorphEntryReal);
					if (mi.m_hvoSense == 0)
					{
						// must have canceled from the dlg.
						fUpdateMorphEntry = false;
					}
				}
				if (fUpdateMorphEntry)
					UpdateMorphEntry(hvoMoFormReal, hvoMorphEntryReal, mi.m_hvoSense);
			}

			private void SetLexEntryToUnknown(object sender, EventArgs args)
			{
				ISilDataAccess sda = m_caches.DataAccess;
				IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
				cda.CacheObjProp(m_hvoMorph, ktagSbMorphEntry, 0);
				cda.CacheObjProp(m_hvoMorph, ktagSbMorphGloss, 0);
				cda.CacheObjProp(m_hvoMorph, ktagSbMorphPos, 0);
				// Forget we had an existing wordform; otherwise, the program considers
				// all changes to be editing the wordform, and since it belongs to the
				// old analysis, the old analysis gets resurrected.
				m_sandbox.m_hvoWordGloss = 0;
				// The current ktagSbMorphForm property is for an SbNamedObject that
				// is associated with an MoForm belonging to the LexEntry that we are
				// trying to dissociate from. If we leave it that way, it will resurrect
				// the LexEntry connection when we update the real cache.
				// Instead make a new named object for the form.
				ITsString tssForm = sda.get_MultiStringAlt(sda.get_ObjectProp(m_hvoMorph, ktagSbMorphForm),
					ktagSbNamedObjName, m_sandbox.RawWordformWs);
				int hvoNewForm = sda.MakeNewObject(kclsidSbNamedObj, m_hvoMorph, ktagSbMorphForm, -2);
				sda.SetMultiStringAlt(hvoNewForm, ktagSbNamedObjName,
					m_sandbox.RawWordformWs, tssForm);
				//cda.CacheStringProp(m_hvoMorph, ktagSbMorphPrefix, null);
				//cda.CacheStringProp(m_hvoMorph, ktagSbMorphPostfix, null);
				// Send notifiers for each of these deleted items.
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoMorph, ktagSbMorphEntry, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoMorph, ktagSbMorphGloss, 0, 0, 1);
				sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvoMorph, ktagSbMorphPos, 0, 0, 1);
				//sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				//	m_hvoMorph, ktagSbMorphPrefix, 0, 0, 1);
				//sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				//	m_hvoMorph, ktagSbMorphPostfix, 0, 0, 1);
			}

			private void OnSelectVariantOf(object sender, EventArgs args)
			{
				try
				{
					using (LinkVariantToEntryOrSense dlg = new LinkVariantToEntryOrSense())
					{
						ILexEntry variantEntry = null;
						// if no previous variant relationship has been defined,
						// then don't try to fill initial information,
						ITsString tssForm;
						ITsString tssFullForm;
						int hvoType;
						int hvoMorphReal;
						GetMorphInfo(out tssForm, out tssFullForm, out hvoMorphReal, out hvoType);
						if (m_caches.MainCache.IsValidObject(hvoMorphReal))
						{
							IMoForm mf = MoForm.CreateFromDBObject(m_sandbox.Cache, hvoMorphReal);
							variantEntry = (mf as CmObject).Owner as ILexEntry;
							dlg.SetDlgInfo(m_sandbox.Cache, m_sandbox.Mediator, variantEntry);
						}
						else
						{
							// since we didn't start with an entry,
							// set up the dialog using the form of the variant
							dlg.SetDlgInfo(m_sandbox.Cache, m_sandbox.Mediator, tssForm);
						}
						dlg.SetHelpTopic("khtpAddVariantFromInterlinear");
						Form mainWnd = m_sandbox.FindForm();
						// Making the form active fixes problems like LT-2619.
						// I'm (RandyR) not sure what adverse impact might show up by doing this.
						mainWnd.Activate();
						if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
						{
							if (dlg.SelectedID == 0)
								return; // odd. nothing more to do.

							ILexEntryRef variantEntryRef = dlg.VariantEntryRefResult;
							// if we didn't have a starting entry, create one now.
							ILexEntry variantResult = (variantEntryRef as CmObject).Owner as ILexEntry;
							int classOfSelectedId = m_sandbox.Cache.GetClassOfObject(dlg.SelectedID);
							// we need to create a new LexEntryRef.
							int morphBundleEntryHvo = 0;
							int morphBundleSenseHvo = 0;
							if (classOfSelectedId == LexEntry.kclsidLexEntry)
							{
								morphBundleEntryHvo = dlg.SelectedID;
								morphBundleSenseHvo = 0; // establish default sense.
							}
							else if (classOfSelectedId == LexSense.kclsidLexSense)
							{
								morphBundleSenseHvo = dlg.SelectedID;
								morphBundleEntryHvo = m_sandbox.Cache.GetOwnerOfObjectOfClass(morphBundleSenseHvo, LexEntry.kclsidLexEntry);
							}
							UpdateMorphEntry(variantResult.LexemeFormOAHvo, morphBundleEntryHvo, morphBundleSenseHvo);
						}
					}

				}
				catch (Exception exc)
				{
					MessageBox.Show(exc.Message, ITextStrings.ksCannotAddVariant);
				}
			}

			protected virtual void SelectEntryIcon(int morphIndex)
			{
				m_sandbox.SelectEntryIconOfMorph(morphIndex);
			}

			/// <summary>
			/// Update the sandbox cache to reflect a choice of the real MoForm and the
			/// entry indicated by the FdoCache hvos passed.
			/// </summary>
			/// <param name="hvoMoFormReal"></param>
			/// <param name="hvoEntryReal"></param>
			/// <param name="hvoSenseReal"></param>
			internal void UpdateMorphEntry(int hvoMoFormReal, int hvoEntryReal, int hvoSenseReal)
			{
				CheckDisposed();

				bool fDirty = m_sandbox.Caches.DataAccess.IsDirty();
				bool fApproved = !m_sandbox.UsingGuess;
				bool fHasApprovedWordGloss = m_sandbox.HasWordGloss() && (fDirty || fApproved);
				bool fHasApprovedWordCat = m_sandbox.HasWordCat() && (fDirty || fApproved);

				// Make a new morph, if one does not already exist, corresponding to the
				// selected item.  Its form must match what is already displayed.  Store it as
				// the new value.
				int hvoMorph = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidMorphemes, hvoMoFormReal,
					(int)MoForm.MoFormTags.kflidForm);
				m_caches.DataAccess.SetObjProp(m_hvoMorph, ktagSbMorphForm, hvoMorph);
				m_caches.DataAccess.PropChanged(m_rootb,
					(int)PropChangeType.kpctNotifyAll, m_hvoMorph, ktagSbMorphForm, 0,
					1, 1);

				// Try to establish the sense.  Call this before SetSelectedEntry and LoadSecDataForEntry.
				// reset cached gloss, since we should establish the sense according to the real sense or real entry.
				m_caches.DataAccess.SetObjProp(m_hvoMorph, ktagSbMorphGloss, 0);
				int realDefaultSense = m_sandbox.EstablishDefaultSense(m_hvoMorph, hvoEntryReal, hvoSenseReal);
				// Make and install a secondary object to correspond to the real LexEntry.
				// (The zero says we are not guessing any more, since the user selected this entry.)
				int hvoMorphEntry = m_caches.MainCache.GetOwnerOfObject(hvoMoFormReal);
				m_sandbox.LoadSecDataForEntry(hvoMorphEntry, hvoSenseReal != 0 ? hvoSenseReal : realDefaultSense,
					m_hvoSbWord, m_caches.DataAccess as IVwCacheDa,
					m_wsVern, m_hvoMorph, 0, m_caches.MainCache.MainCacheAccessor, null);
				m_caches.DataAccess.PropChanged(m_rootb,
					(int)PropChangeType.kpctNotifyAll, m_hvoMorph, ktagSbMorphEntry, 0,
					1, WasReal());

				// Notify any delegates that the selected Entry changed.
				m_sandbox.SetSelectedEntry(hvoEntryReal);
				// fHasApprovedWordGloss: if an approved word gloss already exists -- don't replace it
				// fHasApprovedWordCat: if an approved word category already exists -- don't replace it
				CopyLexEntryInfoToMonomorphemicWordGlossAndPos(!fHasApprovedWordGloss, !fHasApprovedWordCat);
				return;
			}
			protected virtual void CopyLexEntryInfoToMonomorphemicWordGlossAndPos()
			{
				// do nothing in general.
			}

			protected virtual void CopyLexEntryInfoToMonomorphemicWordGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
			{
				// conditionally set up the word gloss and POS to correspond to monomorphemic lex morph entry info.
				SyncMonomorphemicGlossAndPos(fCopyToWordGloss, fCopyToWordPos);
				// Forget we had an existing wordform; otherwise, the program considers
				// all changes to be editing the wordform, and since it belongs to the
				// old analysis, the old analysis gets resurrected.
				m_sandbox.m_hvoWordGloss = 0;
			}
		}



		/// <summary>
		/// This class handles the MorphEntry line when there is a current entry. Currently it
		/// is very nearly the same.
		/// </summary>
		internal class IhMorphEntry : IhMissingEntry
		{
			internal override int WasReal()
			{
				return 1;
			}
		}

		internal class IhLexWordGloss : IhMorphEntry
		{
			public IhLexWordGloss()
				: base()
			{
			}

			protected override void SelectEntryIcon(int morphIndex)
			{
				m_sandbox.SelectIcon(ktagWordGlossIcon);
			}

			protected override void CopyLexEntryInfoToMonomorphemicWordGlossAndPos()
			{
				CopyLexEntryInfoToMonomorphemicWordGlossAndPos(true, true);
			}

			/// <summary>
			/// In the context of a LexWordGloss handler, the user is making a selection in the word combo list
			/// that should fill in the Word Gloss. So, make sure we copy the selected lex information.
			/// </summary>
			/// <param name="fCopyToWordGloss"></param>
			/// <param name="fCopyToWordPos"></param>
			protected override void CopyLexEntryInfoToMonomorphemicWordGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
			{
				base.CopyLexEntryInfoToMonomorphemicWordGlossAndPos(true, true);
			}

			protected override void CopySenseToWordGloss(bool fCopyWordGloss, int hvoSbRootSense)
			{
				if (hvoSbRootSense == 0 && fCopyWordGloss)
				{
					// clear out the WordGloss line(s).
					ISilDataAccess sda = m_caches.DataAccess;
					foreach (int wsId in m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
					{
						ITsString tssGloss = StringUtils.MakeTss("", wsId);
						sda.SetMultiStringAlt(m_hvoSbWord, ktagSbWordGloss, wsId, tssGloss);
						sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord, ktagSbWordGloss,
							wsId, 0, 0);
					}
				}
				else
				{
					base.CopySenseToWordGloss(fCopyWordGloss, hvoSbRootSense);
				}
				// treat as a deliberate user selection, not a guess.
				if (fCopyWordGloss)
					m_caches.DataAccess.SetInt(m_hvoSbWord, ktagSbWordGlossGuess, 0);
			}

			protected override int CopyLexPosToWordPos(bool fCopyToWordCat, int hvoLexPos)
			{

				int hvoPos = 0;
				if (fCopyToWordCat && hvoLexPos == 0)
				{
					// clear out the existing POS
					hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvoLexPos,
						(int)CmPossibility.CmPossibilityTags.kflidAbbreviation);
					int hvoSbWordPos = m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos);
					m_caches.DataAccess.SetObjProp(m_hvoSbWord, ktagSbWordPos, hvoPos);
					m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
						ktagSbWordPos, 0, 1, (hvoSbWordPos == 0 ? 0 : 1));
				}
				else
				{
					hvoPos = base.CopyLexPosToWordPos(fCopyToWordCat, hvoLexPos);
				}
				// treat as a deliberate user selection, not a guess.
				if (fCopyToWordCat)
					m_caches.DataAccess.SetInt(hvoPos, ktagSbNamedObjGuess, 0);
				return hvoPos;

			}
		}

		// The WordGloss has no interesting menu for now. Just allow the text to be edited.
		internal class IhWordGloss : InterlinComboHandler
		{
			public IhWordGloss()
				: base()
			{
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();

				int fGuessingOld = m_caches.DataAccess.get_IntProp(m_hvoSbWord,
					ktagSbWordGlossGuess);

				HvoTssComboItem item = m_comboList.SelectedItem as HvoTssComboItem;
				if (item == null)
					return;
				m_sandbox.WordGlossHvo = item.Hvo;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				foreach (int ws in m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
				{
					ITsString tss;
					if (item.Hvo == 0)
					{
						// Make an empty string in the specified ws.
						tss = tsf.MakeString("", ws);
					}
					else
					{
						tss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(item.Hvo,
							(int)WfiGloss.WfiGlossTags.kflidForm, ws);
					}
					m_caches.DataAccess.SetMultiStringAlt(m_hvoSbWord, ktagSbWordGloss, ws, tss);
					// Regenerate the string regardless.  (See LT-10456)
					m_caches.DataAccess.PropChanged(m_rootb,
						(int)PropChangeType.kpctNotifyAll, m_hvoSbWord, ktagSbWordGloss,
						ws, tss.Length, tss.Length);
					// If it used to be a guess, mark it as no longer a guess.
					if (fGuessingOld != 0)
					{
						m_caches.DataAccess.SetInt(m_hvoSbWord, ktagSbWordGlossGuess, 0);
						m_caches.DataAccess.PropChanged(m_rootb,
							(int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
							ktagSbWordGlossGuess, 0, 1, 1);
					}
				}

				m_sandbox.SelectAtEndOfWordGloss(-1);
				return;
			}

			// Not much to do except to initialize the edit box embedded in the combobox with
			// the proper writing system factory, writing system, and TsString.
			public override void SetupCombo()
			{
				CheckDisposed();

				base.SetupCombo();
				int hvoEmptyGloss = 0;
				ITsStrBldr tsb = TsStrBldrClass.Create();

				m_comboList.WritingSystemFactory =
					m_caches.MainCache.LanguageWritingSystemFactoryAccessor;
				// Find the WfiAnalysis (from existing analysis or guess) to provide its word glosses as options (cf. LT-1428)
				int hvoWa = m_sandbox.GetWfiAnalysisHvoInUse();
				if (hvoWa != 0)
				{
					IWfiAnalysis wa = WfiAnalysis.CreateFromDBObject(m_caches.MainCache, hvoWa);
					AddComboItems(ref hvoEmptyGloss, tsb, wa);
				}
				int hvoNewWa = m_sandbox.CreateRealWfiAnalysisMethod().Run();
				if (hvoNewWa != 0 && hvoNewWa != hvoWa)
				{
					IWfiAnalysis wa = WfiAnalysis.CreateFromDBObject(m_caches.MainCache, hvoNewWa);
					AddComboItems(ref hvoEmptyGloss, tsb, wa);
				}
				m_comboList.Items.Add(new HvoTssComboItem(hvoEmptyGloss, m_caches.MainCache.MakeUserTss(ITextStrings.ksNewWordGloss2)));
				// Set combo selection to current selection.
				m_comboList.SelectedIndex = this.IndexOfCurrentItem;

				// Enhance JohnT: if the analysts decide so, here we add all the other glosses from other analyses.

			}

			private void AddComboItems(ref int hvoEmptyGloss, ITsStrBldr tsb, IWfiAnalysis wa)
			{
				foreach (int hvoGloss in wa.MeaningsOC.HvoArray)
				{
					int[] wsids = DbOps.ListToIntArray(m_sandbox.m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss));
					int glossCount = 0;

					for (int i = 0; i < wsids.Length; i++)
					{
						int ws = wsids[i];
						ITsString nextWsGloss = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(hvoGloss,
							(int)WfiGloss.WfiGlossTags.kflidForm, ws);

						if (nextWsGloss.Length > 0)
						{
							// Append a comma if there are more glosses.
							if (glossCount > 0)
								tsb.Replace(tsb.Length, tsb.Length, ", ", null);

							// Append a Ws label if there are more than one Ws.
							if (wsids.Length > 1)
							{

								tsb.ReplaceTsString(tsb.Length, tsb.Length, WsListManager.WsLabel(m_caches.MainCache, ws));
								tsb.Replace(tsb.Length, tsb.Length, " ", null);
							}
							int oldLen = tsb.Length;
							tsb.ReplaceTsString(oldLen, oldLen, nextWsGloss);
							int color = (int)CmObjectUi.RGB(Color.Blue);
							tsb.SetIntPropValues(oldLen, tsb.Length, (int)FwTextPropType.ktptForeColor,
								(int)FwTextPropVar.ktpvDefault, color);
							glossCount++;
						}
					}
					// (LT-1428) If we find an empty gloss, use this hvo for "New word gloss" instead of 0.
					if (glossCount == 0 && wsids.Length > 0)
					{
						hvoEmptyGloss = hvoGloss;
						tsb.Replace(tsb.Length, tsb.Length, ITextStrings.ksEmpty, null);
					}

					m_comboList.Items.Add(new HvoTssComboItem(hvoGloss, tsb.GetString()));
					tsb.Clear();
				}
			}

			/// <summary>
			/// Return the index corresponding to the current WordGloss state of the Sandbox.
			/// </summary>
			public override int IndexOfCurrentItem
			{
				get
				{
					for (int i = 0; i < m_comboList.Items.Count; ++i)
					{
						HvoTssComboItem item = m_comboList.Items[i] as HvoTssComboItem;
						if (item.Hvo == m_sandbox.WordGlossHvo)
							return i;
					}
					return -1;
				}
			}
		}

		/// <summary>
		/// The SbWord object has no Pos set.
		/// </summary>
		internal class IhMissingWordPos : InterlinComboHandler
		{
			POSPopupTreeManager m_pOSPopupTreeManager;
			PopupTree m_tree;

			internal PopupTree Tree
			{
				get
				{
					CheckDisposed();
					return m_tree;
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
					if (m_pOSPopupTreeManager != null)
					{
						m_pOSPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
						m_pOSPopupTreeManager.Dispose();
					}
					if (m_tree != null)
					{
						m_tree.Load -= new EventHandler(m_tree_Load);
						m_tree.Dispose();
					}
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_pOSPopupTreeManager = null;
				m_tree = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			public override void SetupCombo()
			{
				CheckDisposed();

				m_tree = new PopupTree();
				// Try a bigger size here only for Sandbox POS editing (GordonM) [LT-7529]
				// Enhance: It would be better to know what size we need for the data,
				// but it gets displayed before we know what data goes in it!
				// PopupTree.DefaultSize was (120, 200)
				m_tree.Size = new Size(180, 220);
				m_tree.Load += new EventHandler(m_tree_Load);
				// Handle AfterSelect events through POSPopupTreeManager in m_tree_Load().
			}
			public override void Activate(SIL.FieldWorks.Common.Utils.Rect loc)
			{
				CheckDisposed();

				if (m_tree == null)
				{
					base.Activate(loc);
				}
				else
				{
					m_tree.Launch(m_sandbox.RectangleToScreen(loc),
						Screen.GetWorkingArea(m_sandbox));
				}
			}

			// This indicates there was not a previous real word POS recorded. The 'real' subclass
			// overrides to answer 1. The value signifies the number of objects stored in the
			// ktagSbWordPos property before the user made a selection in the menu.
			internal virtual int WasReal()
			{
				CheckDisposed();

				return 0;
			}

			public override List<int> Items
			{
				get
				{
					LoadItemsIfNeeded();
					return base.Items;
				}
			}

			private List<int> LoadItemsIfNeeded()
			{
				List<int> items = new List<int>();
				if (m_pOSPopupTreeManager == null || !m_pOSPopupTreeManager.IsTreeLoaded)
				{
					m_tree_Load(null, null);
					m_items = null;
					// not sure if this is guarranteed to be in the same order each time, but worth a try.
					foreach (ICmPossibility possibility in m_caches.MainCache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities)
					{
						items.Add(possibility.Hvo);
					}
					m_items = items;
				}
				return items;
			}

			public override int IndexOfCurrentItem
			{
				get
				{
					LoadItemsIfNeeded();
					// get currently selected item.
					int hvoLastCategory = m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));
					// look it up in the items.
					return Items.IndexOf(hvoLastCategory);
				}
			}

			public override void HandleSelect(int index)
			{
				CheckDisposed();
				int hvoPos = Items[index];
				ICmPossibility possibility = new CmPossibility(m_caches.MainCache, hvoPos);

				// Called only if it's a combo box.
				SelectItem(Items[index], possibility.Name.BestVernacularAnalysisAlternative.Text);
			}

			// We can't add the items until the form loads, or we get a spurious horizontal scroll bar.
			private void m_tree_Load(object sender, EventArgs e)
			{
				if (m_pOSPopupTreeManager == null)
				{
					FdoCache cache = m_caches.MainCache;
					m_pOSPopupTreeManager = new POSPopupTreeManager(m_tree, cache, cache.LangProject.PartsOfSpeechOA, cache.LangProject.DefaultAnalysisWritingSystem, false, m_sandbox.Mediator, m_sandbox.FindForm());
					m_pOSPopupTreeManager.AfterSelect += new TreeViewEventHandler(m_pOSPopupTreeManager_AfterSelect);
				}
				m_pOSPopupTreeManager.LoadPopupTree(m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos)));
			}

			private void m_pOSPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
			{
				// we only want to actually select the item if we have clicked on it
				// or if we are simulating a click (e.g. by pressing Enter).
				if (!m_fUnderConstruction && e.Action == TreeViewAction.ByMouse)
				{
					SelectItem((e.Node as HvoTreeNode).Hvo, e.Node.Text);
				}
			}

			internal void SelectItem(int hvo, string label)
			{
				CheckDisposed();

				// if we haven't changed the selection, we don't need to change anything in the cache.
				int hvoLastCategory = m_caches.RealHvo(m_caches.DataAccess.get_ObjectProp(m_hvoSbWord, ktagSbWordPos));
				if (hvoLastCategory != hvo)
				{
					int hvoPos = 0;
					if (hvo > 0)
					{
						ITsString tssAbbr = m_caches.MainCache.MainCacheAccessor.get_MultiStringAlt(hvo,
							(int)CmPossibility.CmPossibilityTags.kflidName, m_caches.MainCache.DefaultAnalWs);
						hvoPos = m_caches.FindOrCreateSec(hvo, kclsidSbNamedObj,
							m_hvoSbWord, ktagSbWordDummy, ktagSbNamedObjName, tssAbbr);
						hvoPos = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, hvo,
							(int)CmPossibility.CmPossibilityTags.kflidAbbreviation);
						m_caches.DataAccess.SetInt(hvoPos, ktagSbNamedObjGuess, 0);
					}
					m_caches.DataAccess.SetObjProp(m_hvoSbWord, ktagSbWordPos, hvoPos);
					m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, m_hvoSbWord,
						ktagSbWordPos, 0, 1, WasReal());
				}
				m_sandbox.SelectIcon(ktagWordPosIcon);
			}
		}

		internal class IhWordPos : IhMissingWordPos
		{
			internal override int WasReal()
			{
				CheckDisposed();

				return 1;
			}
		}

		#endregion Nested Classes
	}

	#endregion SandboxBase class

	internal class SandboxChangedEventArgs : EventArgs
	{
		bool m_fEdited = false;
		internal SandboxChangedEventArgs(bool fHasBeenEdited)
			: base()
		{
			m_fEdited = fHasBeenEdited;
		}

		internal bool Edited
		{
			get { return m_fEdited; }
		}
	}

	internal delegate void SandboxChangedEventHandler(object sender, SandboxChangedEventArgs e);

	#region MorphManager class
	/// <summary>
	/// This class performs various operations on the sequence of morphemes, to do
	/// with altering the morpheme breakdown.
	///
	/// 1. Provides an IVwNotifyChange implementation that updates all the rest of the fields when
	/// the morpheme text (or ktagSbMorphPostfix or ktagSbMorphPrefix) is edited.
	/// For example: institutionally -- originally one morpheme, probably no matches, *** below.
	/// institution ally -- breaks into two morphemes, look up both, nothing found
	/// institution -ally -- hyphen breaks out to ktagSbMorphPrefix.
	/// institution -al ly -- make third morpheme
	/// institution -al -ly -- move hyphen to ktagSbMorphPrefix
	/// in-stitution -al -ly -- I think we treat this as an odd morpheme.
	/// in- stitution -al -ly -- break up, make ktabSbMorphSuffix

	/// All these cases are handled by calling the routine that collapses the
	/// morphemes into a single string, then the one that regenerates them (any time a relevant
	/// property changes), while keeping track of how to restore the selection.

	/// When backspace or del forward tries to delete a space, we need to collapse morphemes.
	/// The root site will receive OnProblemDeletion(sel, kdptBsAtStartPara\kdptDelAtEndPara).
	/// Basically we need to be able to collapse the morphemes to a string, keeping track
	/// of the position, make the change, recompute morphemes etc, and restore the selection.
	/// Again, this is basically done by figuring the combined morphemes, deleting the space,
	/// then figuring the resulting morphemes (and restoring the selection).
	/// </summary>
	internal class MorphManager : IVwNotifyChange, IFWDisposable
	{
		SandboxBase m_sandbox; // The sandbox we're working from.
		string m_morphString; // The representation of the current morphemes as a simple string.
		int m_ichSel = -1; // The index of the selection within that string, or -1 if we don't know it.
		ISilDataAccess m_sda;
		int m_hvoSbWord;
		int m_hvoMorph;
		bool m_fInUpdate = false; // true during UpdateMorphemes.
		bool m_fNeedUpdate = false; // Set true if a property we care about changes.
		private bool m_hasBeenAdded = false;

		internal MorphManager(SandboxBase sandbox)
		{
			m_sandbox = sandbox;
			m_sda = sandbox.Caches.DataAccess;
			m_hvoSbWord = m_sandbox.RootWordHvo;
			m_sda.AddNotification(this);
			m_hasBeenAdded = true;
		}

		internal bool NeedUpdate
		{
			get
			{
				CheckDisposed();
				return m_fNeedUpdate;
			}
			set
			{
				CheckDisposed();
				m_fNeedUpdate = value;
			}
		}

		internal string BuildCurrentMorphsString()
		{
			CheckDisposed();

			return BuildCurrentMorphsString(null);
		}

		/// <summary>
		/// If we can't get the ws from a selection, we should get it from the choice line.
		/// </summary>
		int VernWsForPrimaryMorphemeLine
		{
			get
			{
				// For now, use the real default vernacular ws for the sandbox.
				// This only becomes available after the sandbox has been initialized.
				return m_sandbox.RawWordformWs;
			}
		}

		internal string BuildCurrentMorphsString(IVwSelection sel)
		{
			CheckDisposed();

			int ichSel = -1;
			int hvoObj = 0;
			int tag = 0;
			int ws = 0;
			if (sel != null)
			{
				TextSelInfo selInfo = new TextSelInfo(sel);
				ws = selInfo.WsAltAnchor;
				ichSel = selInfo.IchAnchor;
				hvoObj = selInfo.HvoAnchor;
				tag = selInfo.TagAnchor;
			}
			// for now, we'll just configure getting the string for the primary morpheme line.
			ws = this.VernWsForPrimaryMorphemeLine;
			m_ichSel = -1;

			ITsStrBldr builder = TsStrBldrClass.Create();
			ITsString space = StringUtils.MakeTss(" ", ws);
			ISilDataAccess sda = m_sandbox.Caches.DataAccess;

			ITsString tssWordform = m_sandbox.SbWordForm(ws);
			// we're dealing with a phrase if there are spaces in the word.
			bool fBaseWordIsPhrase = SandboxBase.IsPhrase(tssWordform.Text);
			int cmorphs = m_sda.get_VecSize(m_hvoSbWord, Sandbox.ktagSbWordMorphs);
			for (int imorph = 0; imorph < cmorphs; ++imorph)
			{
				int hvoMorph = m_sda.get_VecItem(m_hvoSbWord, Sandbox.ktagSbWordMorphs, imorph);
				if (imorph != 0)
				{
					builder.ReplaceTsString(builder.Length, builder.Length, space);
					// add a second space to separate morphs in a phrase.
					if (fBaseWordIsPhrase)
						builder.ReplaceTsString(builder.Length, builder.Length, space);
				}
				int hvoMorphForm = sda.get_ObjectProp(hvoMorph, Sandbox.ktagSbMorphForm);
				if (hvoMorph == hvoObj && tag == Sandbox.ktagSbMorphPrefix)
					m_ichSel = builder.Length + ichSel;
				builder.ReplaceTsString(builder.Length, builder.Length,
					sda.get_StringProp(hvoMorph, Sandbox.ktagSbMorphPrefix));
				if (hvoMorphForm == hvoObj && tag == Sandbox.ktagSbNamedObjName)
					m_ichSel = builder.Length + ichSel;
				builder.ReplaceTsString(builder.Length, builder.Length,
					sda.get_MultiStringAlt(hvoMorphForm, Sandbox.ktagSbNamedObjName, ws));
				if (hvoMorph == hvoObj && tag == Sandbox.ktagSbMorphPostfix)
					m_ichSel = builder.Length + ichSel;
				builder.ReplaceTsString(builder.Length, builder.Length,
					sda.get_StringProp(hvoMorph, Sandbox.ktagSbMorphPostfix));
			}
			if (cmorphs == 0)
			{
				if (m_hvoSbWord == hvoObj && tag == Sandbox.ktagMissingMorphs)
					m_ichSel = ichSel;
				m_morphString = Sandbox.InterlinComboHandler.StrFromTss(tssWordform);
			}
			else
			{
				m_morphString = Sandbox.InterlinComboHandler.StrFromTss(builder.GetString());
			}
			return m_morphString;
		}

		private static bool IsBaseWordPhrase(string baseWord)
		{

			bool fBaseWordIsPhrase = baseWord.IndexOfAny(Unicode.SpaceChars) != -1;
			return fBaseWordIsPhrase;
		}

		/// <summary>
		/// Handle an otherwise-difficult backspace (joining morphemes by deleting a 'space')
		/// Return true if successful.
		/// </summary>
		/// <returns></returns>
		public bool HandleBackspace()
		{
			CheckDisposed();

			string currentMorphemes = BuildCurrentMorphsString(m_sandbox.RootBox.Selection);
			if (m_ichSel <= 0)
				return false;
			// This would be risky if we might be deleting a diacritic or surrogate, but we're certainly
			// deleting a space.
			currentMorphemes = currentMorphemes.Substring(0, m_ichSel - 1)
				+ currentMorphemes.Substring(m_ichSel);
			m_ichSel--;
			SetMorphemes(currentMorphemes);
			return true;
		}

		/// <summary>
		/// Handle an otherwise-difficult delete (joining morphemes by deleting a 'space').
		/// </summary>
		/// <returns></returns>
		public bool HandleDelete()
		{
			CheckDisposed();

			string currentMorphemes = BuildCurrentMorphsString(m_sandbox.RootBox.Selection);
			if (m_ichSel < 0 || m_ichSel >= currentMorphemes.Length)
				return false;
			// This would be risky if we might be deleting a diacritic or surrogate, but we're certainly
			// deleting a space.
			currentMorphemes = currentMorphemes.Substring(0, m_ichSel)
				+ currentMorphemes.Substring(m_ichSel + 1);
			SetMorphemes(currentMorphemes);
			return true;
		}

		#region IVwNotifyChange Members

		/// <summary>
		/// A property changed. Is it one of the ones that requires us to update the morpheme list?
		/// Even if so, we shouldn't do it now, because it's dangerous to issue new PropChanged
		/// messages for the same property during a PropChanged. Instead we wait for a DoUpdates call.
		/// Also don't do it if we're in the middle of processing such an update already.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if (m_fInUpdate)
				return;

			if (IsPropMorphBreak(hvo, tag, ivMin))
			{
				m_fNeedUpdate = true;
			}
			// notify the parent sandbox that something has changed its cache.
			m_sandbox.OnUpdateEdited();
		}

		public void DoUpdates()
		{
			CheckDisposed();

			if (!m_fNeedUpdate)
				return; // Nothing we care about has changed.
			// This needs to be set BEFORE we call UpdateMorphemes...otherwise, UpdateMorphemes eventually
			// changes the selection, which triggers another call, making an infinite loop until the
			// stack overflows.
			m_fNeedUpdate = false;
			try
			{
				if (m_hvoMorph != 0)
				{
					// The actual form of the morpheme changed. Any current analysis can't be
					// relevant any more. (We might expect the morpheme breaker to fix this, but
					// in fact it thinks the morpheme hasn't changed, because the cache value
					// has already been updated.)
					IVwCacheDa cda = m_sda as IVwCacheDa;
					cda.CacheObjProp(m_hvoMorph, Sandbox.ktagSbMorphEntry, 0);
					cda.CacheObjProp(m_hvoMorph, Sandbox.ktagSbMorphGloss, 0);
					cda.CacheObjProp(m_hvoMorph, Sandbox.ktagSbMorphPos, 0);
				}
				UpdateMorphemes();
			}
			finally
			{
				// We also do this as a way of making quite sure that it doesn't get set again
				// as a side effect of UpdateMorphemes...another way we could get an infinite
				// loop.
				m_fNeedUpdate = false;
			}

		}

		/// <summary>
		/// Is the property one of the ones that represents a morpheme breakdown?
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public bool IsPropMorphBreak(int hvo, int tag, int ws)
		{
			CheckDisposed();

			switch (tag)
			{
				case Sandbox.ktagSbMorphPostfix:
				case Sandbox.ktagSbMorphPrefix:
					m_hvoMorph = 0;
					return true;
				case Sandbox.ktagSbNamedObjName:
					if (ws != VernWsForPrimaryMorphemeLine)
						return false;
					// Name of some object: is it a morph?
					int cmorphs = m_sda.get_VecSize(m_hvoSbWord, Sandbox.ktagSbWordMorphs);
					for (int imorph = 0; imorph < cmorphs; ++imorph)
					{
						m_hvoMorph = m_sda.get_VecItem(m_hvoSbWord, Sandbox.ktagSbWordMorphs,
							imorph);
						if (hvo == m_sda.get_ObjectProp(m_hvoMorph, Sandbox.ktagSbMorphForm))
						{
							return true;
						}
					}
					m_hvoMorph = 0;
					break;
				case Sandbox.ktagSbWordMorphs:
					return true;
				default:
					// Some property we don't care about.
					return false;
			}
			return false;
		}

		void UpdateMorphemes()
		{
			SetMorphemes(BuildCurrentMorphsString(m_sandbox.RootBox.Selection));
		}

		void SetMorphemes(string currentMorphemes)
		{
			if (currentMorphemes.Length == 0)
			{
				// Reconstructing the sandbox rootbox after deleting all morpheme characters
				// will cause the user to lose the ability to type in the morpheme line (cf. LT-1621).
				// So just return here, since there are no morphemes to process.
				return;
			}
			try
			{
				// This code largely duplicates that found in UpdateMorphBreaks() following the call
				// to the EditMorphBreaksDlg, with addition of the m_fInUpdate flag and setting
				// the selection to stay in synch with the typing.  Modifying the code to more
				// closely follow that code fixed LT-1023.
				m_fInUpdate = true;
				IVwCacheDa cda = (IVwCacheDa)m_sda;
				Sandbox.MorphemeBreaker mb = new Sandbox.MorphemeBreaker(m_sandbox.Caches,
					currentMorphemes, m_hvoSbWord, VernWsForPrimaryMorphemeLine, m_sandbox);
				mb.IchSel = m_ichSel;
				mb.Run();
				m_fNeedUpdate = false;
				m_sandbox.RootBox.Reconstruct(); // Everything changed, more or less.
				mb.MakeSel();
				m_sandbox.OnUpdateEdited();
			}
			finally
			{
				m_fInUpdate = false;
			}
		}

		#endregion

		internal void ResetNotification(bool add)
		{
			if (add)
			{
				if (!m_hasBeenAdded)
				{
					m_sda.AddNotification(this);
					m_hasBeenAdded = true;
				}
				return;
			}
			m_sda.RemoveNotification(this);
			m_hasBeenAdded = false;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
		~MorphManager()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
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

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_sda != null && m_hasBeenAdded)
				{
					m_sda.RemoveNotification(this);
					m_hasBeenAdded = false;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_sandbox = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}

	#endregion MorphManager class
}
