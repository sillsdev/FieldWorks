//#define TraceMouseCalls		// uncomment this line to trace mouse messages
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Widgets;
using XCore;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.IText
{
	#region SandboxBase class

	public partial class SandboxBase : RootSite
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
		// This is used to store an object corresponding to WfiMorphBundle.InflType.
		internal const int ktagSbNamedObjInflType = 6903020;


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
		private SandboxEditMonitor m_editMonitor;


		// This is used to store the initial state of our sandbox, so we can revert to it in case of an undo action.
		protected int m_hvoInitialWag = 0;
		private IVwStylesheet m_stylesheet;
		// The original value of m_hvoWordform, to which we return if the user chooses
		// 'Use default analysis' in the line-one chooser.
		private IWfiWordform m_wordformOriginal;
		// The text that appears in the word line, the original casing from the paragraph.
		protected ITsString m_rawWordform;
		// The annotation context for the sandbox.
		protected AnalysisOccurrence m_occurrenceSelected;
		// This flag controls behavior that depends on whether the word being analyzed should be treated
		// as at the start of a sentence. Currently this affects the behavior for words with initial
		// capitalization only.
		private bool m_fTreatAsSentenceInitial = true;
		// Indicates the case status of the wordform.
		private StringCaseStatus m_case;
		// If m_hvoWordform is set to zero, this should be set to the actual text that should be
		// assigned to the new Wordform that will be created if GetRealAnalysis is called.
		private ITsString m_tssWordform;
		// The original Gloss we started with. ReviewP: Can we get rid of this?
		private int m_hvoWordGloss;

		private bool m_fSuppressShowCombo = true; // set to prevent SelectionChanged displaying combo.
		private bool m_fShowAnalysisCombo = true; // false to hide Wordform-line combo (if no analyses).

		internal IComboHandler m_ComboHandler; // handles most kinds of combo box.
		private ChooseAnalysisHandler m_caHandler; // handles the one on the base line.
		protected SandboxVc m_vc;
		private Point m_LastMouseMovePos;
		// Rectangle containing last selection passed to ShowComboForSelection.
		private SIL.Utils.Rect m_locLastShowCombo;
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

		private bool m_fHaveUndone; // tells whether an Undo has occurred since Sandbox started on this word.

		private int m_rgbGuess = NoGuessColor;

		// During processing of a right-click menu item, this is the morpheme the user clicked on
		// (from the sandbox cache).
		int m_hvoRightClickMorph;
		// The analysis we guessed (may actually be a WfiGloss). If we didn't guess, it's the actual
		// analysis we started with.
		int m_hvoAnalysisGuess;

		private SpellCheckHelper m_spellCheckHelper;
		#endregion Data members

		#region Properties

		/// <summary>
		///  Pass through to the VC.
		/// </summary>
		protected virtual bool IsMorphemeFormEditable
		{
			get { return true; }
		}

		public void UpdateLineChoices(InterlinLineChoices choices)
		{
			m_choices = choices;
			if (m_vc != null)
				m_vc.UpdateLineChoices(choices);
			m_rootb.Reconstruct();
		}

		/// <summary>
		/// When sandbox is visible, it should be the same as the InterlinDocForAnalysis m_hvoAnnotation.
		/// However, when the Sandbox is not visible the parent is setting/sizing things up for a new annotation.
		/// </summary>
		public virtual int HvoAnnotation
		{
			get { return m_occurrenceSelected.Analysis.Hvo; }
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
					m_wsRawWordform = TsStringUtils.GetWsAtOffset(RawWordform, 0);
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
			string locale = m_caches.MainCache.ServiceLocator.WritingSystemManager.Get(TsStringUtils.GetWsAtOffset(tss, 0)).IcuLocale;
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

				int chvo = MorphCount;
				using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
				{
					Caches.DataAccess.VecProp(kSbWord, ktagSbWordMorphs, chvo, out chvo, arrayPtr);
					int[] morphsHvoList = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
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
					IWfiWordform wf = CurrentAnalysisTree.Wordform;
					if (m_wsRawWordform != 0)
						m_rawWordform = wf.Form.get_String(m_wsRawWordform);
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
		/// This property holds the color to use for any display that indicates multiple
		/// analysis options are available. It should be set to the standard background color
		/// if there is nothing to report.
		/// </summary>
		public int MultipleAnalysisColor
		{
			get { return m_multipleAnalysisColor;  }
			set
			{
				//avoid unnecessary side affects if no actual change is occurring.
				if (m_multipleAnalysisColor != value)
				{
					m_multipleAnalysisColor = value;
					//if we are having our information about multiple analysis state update,
					//then update the vc if it already exists so that it will agree.
					if (m_vc != null)
					{
						m_vc.MultipleOptionBGColor = m_multipleAnalysisColor;
					}
				}
			}
		}

		static protected int NoGuessColor
		{
			get { return (int)CmObjectUi.RGB(DefaultBackColor); }
		}


		/// <summary>
		/// indicates whether the current state of the sandbox is using a guessed analysis.
		/// </summary>
		public bool UsingGuess
		{
			get; set;
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
		internal virtual InterlinDocForAnalysis InterlinDoc
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

		internal SandboxEditMonitor SandboxEditMonitor
		{
			get
			{
				CheckDisposed();
				return m_editMonitor;
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
		protected bool OnUndo(object args)
		{
			if (m_caches.DataAccess.IsDirty())
			{
				ResyncSandboxToDatabase();
				m_fHaveUndone = true;
				return true;
			}
			// We didn't handle it; some other colleague may be able to undo something we don't know about.
			return false;
		}

		/// <summary>
		/// Triggered to tell clients that the Sandbox has changed (e.g. been edited from
		/// its initial state) to help determine whether we should allow trying to save or
		/// undo the changes.
		///
		/// Currently triggered by SandboxEditMonitor.PropChanged whenever a property changes on the cache
		/// </summary>
		internal void OnUpdateEdited()
		{
			bool fIsEdited = m_caches.DataAccess.IsDirty();
			//The user has now approved this candidate, remove any ambiguity indicating color.
			if (fIsEdited)
				MultipleAnalysisColor = NoGuessColor;
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
			ReconstructForWordBundleAnalysis(m_hvoInitialWag);
		}

		protected void ReconstructForWordBundleAnalysis(int hvoWag)
		{
			m_fHaveUndone = false;
			HideCombos(); // Usually redundant, but MUST not have one around hooked to old data.
			LoadForWordBundleAnalysis(hvoWag);
			if (m_rootb == null)
				MakeRoot();
			else
				m_rootb.Reconstruct();
		}

		internal void MarkAsInitialState()
		{
			// As well as noting that this IS the initial state, we want to record some things about the initial state,
			// for possible use when resetting the focus box.
			// Generally m_hvoInitialWag is more reliably set in LoadForWordBundleAnalysis, since by the time
			// MarkInitialState is called, CurrentAnalysisTree.Analysis may have been cleared (e.g., if we are
			// glossing a capitalized word at the start of a segment). It is important to set it here when saving
			// an updated analysis, so that a subsequent undo will restore it to the saved value.
			m_hvoAnalysisGuess = m_hvoInitialWag = CurrentAnalysisTree.Analysis != null ?
				CurrentAnalysisTree.Analysis.Hvo : 0;
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
			// We didn't handle it; some other colleague may be able to undo something we don't know about.
			return false;
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
				if (CurrentAnalysisTree == null || CurrentAnalysisTree.Analysis == null)
					return 0;
				return CurrentAnalysisTree.Analysis.Hvo;
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
		/// This is the WordGloss that the Sandbox was initialized with, either from the initial WAG or
		/// from a guess.
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
		internal protected int MorphIndex
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
			CurrentAnalysisTree = new AnalysisTree();
			// Tab should move between the piles inside the focus box!  See LT-9228.
			AcceptsTab = true;
		}

		public SandboxBase(FdoCache cache, Mediator mediator, IVwStylesheet ss, InterlinLineChoices choices)
			: this()
		{
			// Override things from InitializeComponent()
			BackColor = Color.FromKnownColor(KnownColor.Control);

			// Setup member variables.
			m_caches.MainCache = cache;
			Cache = cache;
			m_mediator = mediator;
			// We need to set this for various inherited things to work,
			// for example, automatically setting the correct keyboard.
			WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			m_caches.CreateSecCache();
			m_choices = choices;
			m_stylesheet = ss; // this is really redundant now it inherits a StyleSheet property.
			StyleSheet = ss;
			m_editMonitor = new SandboxEditMonitor(this); // after creating sec cache.
			if (mediator != null && mediator.PropertyTable != null)
			{
				mediator.PropertyTable.SetProperty("FirstControlToHandleMessages", this, false, PropertyTable.SettingsGroup.LocalSettings);
				mediator.PropertyTable.SetPropertyPersistence("FirstControlToHandleMessages", false);
			}

			UIAutomationServerProviderFactory = () => new SimpleRootSiteDataProvider(this,
				fragmentRoot => RootSiteServices.CreateUIAutomationInvokeButtons(fragmentRoot, RootBox, OpenComboBox));
		}

		public SandboxBase(FdoCache cache, Mediator mediator, IVwStylesheet ss, InterlinLineChoices choices, int hvoAnalysis)
			: this(cache, mediator, ss, choices)
		{
			// finish setup with the WordBundleAnalysis
			LoadForWordBundleAnalysis(hvoAnalysis);
		}

		void OpenComboBox(IVwSelection selection)
		{
			ShowComboForSelection(selection, true);
		}

		/// <summary>
		/// When a sandbox is created, inform the main window that it needs to receive
		/// keyboard input until further notice.  (See FWNX-785.)
		/// </summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			if (MiscUtils.IsMono && (Form.ActiveForm as XWorks.FwXWindow) != null)
			{
				(Form.ActiveForm as XWorks.FwXWindow).DesiredControl = this;
			}
		}

		/// <summary>
		/// When a sandbox is destroyed, inform the main window that it no longer
		/// exists to receive keyboard input.  (See FWNX-785.)
		/// </summary>
		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			if (MiscUtils.IsMono && (Form.ActiveForm as XWorks.FwXWindow) != null)
			{
				(Form.ActiveForm as XWorks.FwXWindow).DesiredControl = null;
			}
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (m_editMonitor != null)
			{
				if (Visible)
					m_editMonitor.StartMonitoring();
				else
					m_editMonitor.StopMonitoring();
			}
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
			if (CurrentAnalysisTree.Analysis == null)
			{
				// This probably paranoid, but it's safe.
				Debug.WriteLine("loading Sandbox for missing analysis");
				m_wordformOriginal = null;
				m_case = StringCaseStatus.allLower;
				return;
			}

			// stop monitoring cache since we are about to make some drastic changes.
			using (new SandboxEditMonitorHelper(m_editMonitor, true))
			{
				UsingGuess = LoadRealDataIntoSec1(kSbWord, fLookForDefaults, fAdjustCase);
				Debug.Assert(CurrentAnalysisTree.Wordform != null || m_tssWordform != null);

				// At this point the only reason to force the current displayed analysis
				// to be returned instead of the original is if we're guessing.
				//m_fForceReturnNewAnalysis = fGuessing;

				// Treat initial state (including guessing) as something you can leave without saving.
				// Make sure it doesn't think any edits have happened, even if reusing from some other word.
				if (fClearDirty)
					MarkAsInitialState();
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoSbWord">either m_hvoSbWord, m_hvoPrevSbWordb, or m_hvoNextSbWord
		/// </param>
		/// <param name="fAdjustCase">If true, may adjust case of morpheme when
		/// proposing whole word as default morpheme breakdown.</param>
		/// <returns>true if any guessing is involved.</returns>
		private bool LoadRealDataIntoSec1(int hvoSbWord, bool fLookForDefaults, bool fAdjustCase)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess;
			if (CurrentAnalysisTree.Analysis == null)
			{
				// should we empty the cache of any stale data?
				return false;
			}
			m_hvoLastSelEntry = 0;	// forget last Lex Entry user selection. We're resync'ing everything.
			IWfiAnalysis analysis = CurrentAnalysisTree.WfiAnalysis;
			IWfiGloss gloss = CurrentAnalysisTree.Gloss;
			m_hvoWordGloss = gloss != null ? gloss.Hvo : 0;
			int fGuessing = 0;  // Use 0 or 1, as we store it in an int dummy property.

			RawWordform = null; // recompute based upon wordform.
			int wsVern = RawWordformWs;
			m_caches.Map(hvoSbWord, CurrentAnalysisTree.Wordform.Hvo); // Review: any reason to map these?
			ISilDataAccess sdaMain = m_caches.MainCache.MainCacheAccessor;
			CopyStringsToSecondary(InterlinLineChoices.kflidWord, sdaMain, CurrentAnalysisTree.Wordform.Hvo,
				WfiWordformTags.kflidForm, cda, hvoSbWord, ktagSbWordForm, tsf);
			CaseFunctions cf = VernCaseFuncs(RawWordform);
			m_case = cf.StringCase(RawWordform.Text);
			// empty it in case we're redoing after choose from combo.
			cda.CacheVecProp(hvoSbWord, ktagSbWordMorphs, new int[0], 0);
			if (analysis == null)
			{
				if (fLookForDefaults)
				{
					GetDefaults(CurrentAnalysisTree.Wordform, out analysis, out gloss, fAdjustCase);
					m_hvoWordGloss = gloss != null ? gloss.Hvo : 0;
					// Make sure the wordform ID is consistent with the analysis we located.
					if (analysis != null)
					{
						//set the color before we fidle with our the wordform, it right for this purpose now.
						if (GetHasMultipleRelevantAnalyses(CurrentAnalysisTree.Wordform))
						{
							MultipleAnalysisColor = InterlinVc.MultipleApprovedGuessColor;
						}
						var fixedWordform = analysis.Owner as IWfiWordform;
						if (fixedWordform != CurrentAnalysisTree.Wordform)
						{
							CurrentAnalysisTree.Analysis = fixedWordform;
							// Update the actual form.
							// Enhance: may NOT want to do this, when we get the baseline consistently
							// keeping original case.
							CopyStringsToSecondary(InterlinLineChoices.kflidWord, sdaMain, CurrentAnalysisTree.Wordform.Hvo,
								WfiWordformTags.kflidForm, cda, hvoSbWord, ktagSbWordForm, tsf);
						}
					}
					// Hide the analysis combo if there's no default analysis (which means there are
					// no options to list).
					m_fShowAnalysisCombo = (analysis != null);
					fGuessing = 1;
				}
				else if (CurrentAnalysisTree.Wordform != null)
				{
					// Need to check whether there are any options to list.
					m_fShowAnalysisCombo = CurrentAnalysisTree.Wordform.AnalysesOC.Count > 0;
				}
			}
			else
			{
				m_fShowAnalysisCombo = true; // there's a real analysis!
			}
			m_hvoAnalysisGuess = analysis != null ? analysis.Hvo : 0;
			if (m_hvoWordGloss != 0)
				m_hvoAnalysisGuess = m_hvoWordGloss;

			// make the wordform corresponding to the baseline ws, match RawWordform
			m_caches.DataAccess.SetMultiStringAlt(kSbWord, ktagSbWordForm, this.RawWordformWs, RawWordform);
			// Set every alternative of the word gloss, whether or not we have one...this
			// ensures clearing it out if we once had something but do no longer.
			CopyStringsToSecondary(InterlinLineChoices.kflidWordGloss, sdaMain, m_hvoWordGloss,
				WfiGlossTags.kflidForm, cda, hvoSbWord, ktagSbWordGloss, tsf);
			cda.CacheIntProp(hvoSbWord, ktagSbWordGlossGuess, fGuessing);
			cda.CacheObjProp(hvoSbWord, ktagSbWordPos, 0); // default.
			if (analysis != null) // Might still be, if no default is available.
			{
				var category = analysis.CategoryRA;
				if (category != null)
				{
					int hvoWordPos = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, category.Hvo,
																   CmPossibilityTags.kflidAbbreviation, hvoSbWord, sdaMain, cda, tsf);
					cda.CacheObjProp(hvoSbWord, ktagSbWordPos, hvoWordPos);
					cda.CacheIntProp(hvoWordPos, ktagSbNamedObjGuess, fGuessing);
				}
				if (this.ShowMorphBundles)
				{
					var bldrError = new StringBuilder();
					foreach (var mb in analysis.MorphBundlesOS)
					{
						// Create the corresponding SbMorph.
						int hvoMbSec = m_caches.DataAccess.MakeNewObject(kclsidSbMorph, hvoSbWord,
																		 ktagSbWordMorphs, mb.IndexInOwner);
						m_caches.Map(hvoMbSec, mb.Hvo);

						// Get the real MoForm, if any.
						var mf = mb.MorphRA;
						// Get the text we will display on the first line of the morpheme bundle.
						// Taken from the MoForm if any, otherwise the form of the MB.
						int hvoMorphForm;
						string sPrefix = null;
						string sPostfix = null;
						if (mf == null)
						{
							// Create the secondary object corresponding to the MoForm. We create one
							// even though there isn't a real MoForm. It doesn't correspond to anything
							// in the real database.
							hvoMorphForm = m_caches.DataAccess.MakeNewObject(kclsidSbNamedObj, mb.Hvo,
																			 ktagSbMorphForm, -2); // -2 for atomic
							CopyStringsToSecondary(InterlinLineChoices.kflidMorphemes, sdaMain, mb.Hvo,
												   WfiMorphBundleTags.kflidForm, cda, hvoMorphForm, ktagSbNamedObjName, tsf);
							// We will slightly adjust the form we display in the default vernacular WS.
							InterlinLineSpec specMorphemes = m_choices.GetPrimarySpec(InterlinLineChoices.kflidMorphemes);
							int wsForm;
							if (specMorphemes == null || !mb.Form.TryWs(specMorphemes.WritingSystem, out wsForm))
								wsForm = RawWordformWs;
							ITsString tssForm = sdaMain.get_MultiStringAlt(mb.Hvo,
																		   WfiMorphBundleTags.kflidForm,
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
									mmt = MorphServices.FindMorphType(m_caches.MainCache, ref realForm, out clsidForm);
									sPrefix = mmt.Prefix;
									sPostfix = mmt.Postfix;
								}
								catch (Exception e)
								{
									bldrError.AppendLine(e.Message);
								}
							}
							tssForm = TsStringUtils.MakeTss(realForm, RawWordformWs);
							cda.CacheStringAlt(hvoMorphForm, ktagSbNamedObjName, wsVern, tssForm);
						}
						else
						{
							// Create the secondary object corresponding to the MoForm in the usual way from the form object.
							hvoMorphForm = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidMorphemes, mf.Hvo,
																		 MoFormTags.kflidForm, hvoSbWord, sdaMain, cda, tsf);
							// Store the prefix and postfix markers from the MoMorphType object.
							int hvoMorphType = sdaMain.get_ObjectProp(mf.Hvo,
																	  MoFormTags.kflidMorphType);
							if (hvoMorphType != 0)
							{
								sPrefix = sdaMain.get_UnicodeProp(hvoMorphType,
																  MoMorphTypeTags.kflidPrefix);
								sPostfix = sdaMain.get_UnicodeProp(hvoMorphType,
																   MoMorphTypeTags.kflidPostfix);
							}
						}
						if (!String.IsNullOrEmpty(sPrefix))
							cda.CacheStringProp(hvoMbSec, ktagSbMorphPrefix,
												tsf.MakeString(sPrefix, wsVern));
						if (!String.IsNullOrEmpty(sPostfix))
							cda.CacheStringProp(hvoMbSec, ktagSbMorphPostfix,
												tsf.MakeString(sPostfix, wsVern));

						// Link the SbMorph to its form object, noting if it is a guess.
						cda.CacheObjProp(hvoMbSec, ktagSbMorphForm, hvoMorphForm);
						cda.CacheIntProp(hvoMorphForm, ktagSbNamedObjGuess, fGuessing);

						// Get the real Sense that supplies the gloss, if any.
						var senseReal = mb.SenseRA;
						if (senseReal == null)
						{
							// Guess a default
							senseReal = mb.DefaultSense;
						}
						if (senseReal != null) // either all-the-way real, or default.
						{
							// Create the corresponding dummy.
							int hvoLexSenseSec;
							// Add any irregularly inflected form type info to the LexGloss.
							ILexEntryRef lerTest;
							ILexEntry possibleVariant = null;
							if (mf != null)
								possibleVariant = mf.Owner as ILexEntry;
							if (possibleVariant != null && possibleVariant.IsVariantOfSenseOrOwnerEntry(senseReal, out lerTest))
							{
								hvoLexSenseSec = m_caches.FindOrCreateSec(senseReal.Hvo, kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
								CacheLexGlossWithInflTypeForAllCurrentWs(possibleVariant, hvoLexSenseSec, wsVern, cda, mb.InflTypeRA);
							}
							else
							{
								// add normal LexGloss without variant info
								hvoLexSenseSec = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidLexGloss, senseReal.Hvo,
											 LexSenseTags.kflidGloss, hvoSbWord, sdaMain, cda, tsf);
							}
							cda.CacheObjProp(hvoMbSec, ktagSbMorphGloss, hvoLexSenseSec);
							cda.CacheIntProp(hvoLexSenseSec, ktagSbNamedObjGuess, fGuessing);

							int hvoInflType = 0;
							if (mb.InflTypeRA != null)
							{
								hvoInflType = m_caches.FindOrCreateSec(mb.InflTypeRA.Hvo,
														 kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
							}
							cda.CacheObjProp(hvoMbSec, ktagSbNamedObjInflType, hvoInflType);
						}

						// Get the MSA, if any.
						var msaReal = mb.MsaRA;
						if (msaReal != null)
						{
							int hvoPos = m_caches.FindOrCreateSec(msaReal.Hvo,
																  kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);

							foreach (int ws in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexPos, true))
							{
								// Since ws maybe ksFirstAnal/ksFirstVern, we need to get what is actually
								// used in order to retrieve the data in Vc.Display().  See LT_7976.
								// Use InterlinAbbrTss to get an appropriate different name for each ws
								ITsString tssLexPos = msaReal.InterlinAbbrTSS(ws);
								int wsActual = TsStringUtils.GetWsAtOffset(tssLexPos, 0);
								cda.CacheStringAlt(hvoPos, ktagSbNamedObjName, wsActual, tssLexPos);
							}
							cda.CacheObjProp(hvoMbSec, ktagSbMorphPos, hvoPos);
							cda.CacheIntProp(hvoPos, ktagSbNamedObjGuess, fGuessing);
						}

						// If we have a form, we can get its owner and set the info for the Entry
						// line.
						// Enhance JohnT: attempt a guess if we have a form but no entry.
						if (mf != null)
						{
							var entryReal = mf.Owner as ILexEntry;
							// We can assume the owner is a LexEntry as that is the only type of object
							// that can own MoForms. We don't actually create the LexEntry, to
							// improve performance. All the relevant data should already have
							// been loaded while creating the main interlinear view.
							LoadSecDataForEntry(entryReal, senseReal, hvoSbWord, cda, wsVern, hvoMbSec, fGuessing, sdaMain, tsf);
						}
					}
					if (bldrError.Length > 0)
					{
						var msg = bldrError.ToString().Trim();
						var wnd = (FindForm() ?? Mediator.PropertyTable.GetValue("window")) as IWin32Window;
						MessageBox.Show(wnd, msg, ITextStrings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}
			else
			{
				// No analysis, default or otherwise. We immediately, however, fill in a single
				// dummy morpheme, if showing morphology.
				fGuessing = 0;	// distinguish between a 'guess' (defaults) and courtesy filler info (cf. LT-5858).
				if (ShowMorphBundles)
				{
					int hvoMbSec = m_caches.DataAccess.MakeNewObject(kclsidSbMorph, hvoSbWord,
						ktagSbWordMorphs, 0);
					ITsString tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoSbWord, ktagSbWordForm, this.RawWordformWs);
					// Possibly adjust case of tssForm.
					if (fAdjustCase && CaseStatus == StringCaseStatus.title &&
						tssForm != null && tssForm.Length > 0)
					{
						tssForm = TsStringUtils.MakeTss(cf.ToLower(tssForm.Text), this.RawWordformWs);
						m_tssWordform = tssForm; // need this to be set in case hvoWordformRef set to zero.
						// If we adjust the case of the form, we must adjust the hvo as well,
						// or any analyses created will go to the wrong WfiWordform.
						CurrentAnalysisTree.Analysis = GetWordform(tssForm);
						if (CurrentAnalysisTree.Wordform != null)
							m_fShowAnalysisCombo = CurrentAnalysisTree.Wordform.AnalysesOC.Count > 0;
					}
					else
					{
						// just use the wfi wordform form for our dummy morph form.
						tssForm = CurrentAnalysisTree.Wordform.Form.get_String(this.RawWordformWs);
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

		public static bool GetHasMultipleRelevantAnalyses(IWfiWordform analysis)
		{
			int humanCount = analysis.HumanApprovedAnalyses.Count();
			int machineCount = analysis.HumanNoOpinionParses.Count();
			return humanCount + machineCount > 1;
		}

		private static bool IsAnalysisHumanApproved(FdoCache cache, IWfiAnalysis analysis)
		{
			if (analysis == null)
				return false; // non-existent analysis can't be approved.
			return (from ae in analysis.EvaluationsRC
							  where ae.Approves &&  (ae.Owner as ICmAgent).Human
							  select ae).FirstOrDefault() != null;
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

		private int CreateSecondaryAndCopyStrings(int flidChoices, int hvoMain, int flidMain, int hvoSbWord,
			ISilDataAccess sdaMain, IVwCacheDa cda, ITsStrFactory tsf)
		{
			int hvoSec = m_caches.FindOrCreateSec(hvoMain,
				kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
			CopyStringsToSecondary(flidChoices, sdaMain, hvoMain, flidMain, cda, hvoSec, ktagSbNamedObjName, tsf);
			return hvoSec;
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
		public void SetSelectedEntry(ILexEntry entryReal)
		{
			CheckDisposed();

			if (entryReal.Hvo != m_hvoLastSelEntry)
			{
				m_hvoLastSelEntry = entryReal.Hvo;
				if (SelectionChangedEvent != null)
					SelectionChangedEvent(this, new FwObjectSelectionEventArgs(entryReal.Hvo));
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

		private void CopyStringsToSecondary(IList<int> writingSystems, ISilDataAccess sdaMain, int hvoMain,
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
				else if (ws == WritingSystemServices.kwsFirstAnal)
				{
					IList<int> currentAnalysisWsList = m_caches.MainCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle).ToArray();
					CacheStringAltForAllCurrentWs(currentAnalysisWsList, cda, hvoSec, flidSec, sdaMain, hvoMain, flidMain);
					continue;
				}
				else if (ws == WritingSystemServices.kwsFirstVern)
				{
					IList<int> currentVernWsList = m_caches.MainCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(wsObj => wsObj.Handle).ToArray();
					CacheStringAltForAllCurrentWs(currentVernWsList, cda, hvoSec, flidSec, sdaMain, hvoMain, flidMain);
					continue;
				}
				else if (ws == WritingSystemServices.kwsVernInParagraph)
				{
					wsActual = RawWordformWs;
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
		/// Copy to the secondary cache NamedObect hvoSec all alternatives of property flid of
		/// real object hvoMain.  The user may change the actual writing system assigned to a
		/// given line at any time, so we need all the possibilities available in the local
		/// cache.
		/// </summary>
		internal void CopyStringsToSecondary(int flidChoices, ISilDataAccess sdaMain, int hvoMain,
			int flidMain, IVwCacheDa cda, int hvoSec, int flidSec, ITsStrFactory tsf)
		{
			var writingSystems = m_caches.MainCache.ServiceLocator.WritingSystems.AllWritingSystems.Select(ws => ws.Handle).ToList();
			CopyStringsToSecondary(writingSystems, sdaMain, hvoMain, flidMain, cda, hvoSec, flidSec, tsf);
		}

		private static void CacheStringAltForAllCurrentWs(IEnumerable<int> currentWsList, IVwCacheDa cda, int hvoSec, int flidSec,
			ISilDataAccess sdaMain, int hvoMain, int flidMain)
		{
			CacheStringAltForAllCurrentWs(currentWsList, cda, hvoSec, flidSec,
				delegate(int ws1)
				{
					ITsString tssMain;
					if (hvoMain != 0)
						tssMain = sdaMain.get_MultiStringAlt(hvoMain, flidMain, ws1);
					else
						tssMain = TsStringUtils.MakeTss("", ws1);
					return tssMain;
				});
		}

		private static void CacheStringAltForAllCurrentWs(IEnumerable<int> currentWsList, IVwCacheDa cda, int hvoSec, int flidSec,
			Func<int, ITsString> createStringAlt)
		{
			foreach (int ws1 in currentWsList)
			{
				ITsString tssMain = null;
				if (createStringAlt != null)
					tssMain = createStringAlt(ws1);
				if (tssMain == null)
					tssMain = TsStringUtils.MakeTss("", ws1);
				cda.CacheStringAlt(hvoSec, flidSec, ws1, tssMain);
			}
		}

		private ITsString GetBestVernWordform(IWfiWordform wf)
		{
			// first we'll try getting vernacular ws directly, since it'll be true in most cases.
			ITsString tssForm = wf.Form.get_String(this.RawWordformWs);
			if (tssForm == null || tssForm.Length == 0)
				tssForm = wf.Form.BestVernacularAlternative;
			return tssForm;
		}

		/// <summary>
		/// Obtain the HVO of the most desirable default annotation to use for a particular
		/// wordform.
		/// </summary>
		private void GetDefaults(IWfiWordform wordform, out IWfiAnalysis analysis, out IWfiGloss gloss, bool fAdjustCase)
		{
			analysis = null; // default
			gloss = null;
			if (wordform == null || !wordform.IsValidObject)
				return;

			if (InterlinDoc == null) //when running some tests this is null
				return;
			ISilDataAccess sda = InterlinDoc.RootBox.DataAccess;

			// If we're calling from the context of SetWordform(), we may be trying to establish
			// an alternative wordform/form/analysis. In that case, or if we don't have a default cached,
			// try to get one. Otherwise, if we've already cached a default, use it...it's surprising for the
			// user if we move the focus box to something and the default changes. (LT-4643 etc.)
			int hvoDefault = 0;
			if (m_occurrenceSelected != null && m_occurrenceSelected.Analysis == wordform)
			{
				// Try to establish a default based on the current occurrence.
				if (m_fSetWordformInProgress ||
					!sda.get_IsPropInCache(HvoAnnotation, InterlinViewDataCache.AnalysisMostApprovedFlid,
						(int) CellarPropertyType.ReferenceAtomic, 0))
				{
					InterlinDoc.RecordGuessIfNotKnown(m_occurrenceSelected);
				}
				hvoDefault = sda.get_ObjectProp(HvoAnnotation, InterlinViewDataCache.AnalysisMostApprovedFlid);
			}
			else
			{
				// Try to establish a default based on the wordform itself.
				int ws = wordform.Cache.DefaultVernWs;
				if (m_occurrenceSelected != null)
					ws = m_occurrenceSelected.BaselineWs;
				var analysisDefault = InterlinDoc.GetGuessForWordform(wordform, ws);
				if (analysisDefault != null)
					hvoDefault = analysisDefault.Hvo;
			}

			if (hvoDefault != 0)
			{
				var obj = m_caches.MainCache.ServiceLocator.GetObject(hvoDefault);
				switch (obj.ClassID)
				{
					case WfiAnalysisTags.kClassId:
						analysis = (IWfiAnalysis) obj;
						gloss = null;
						return;
					case WfiGlossTags.kClassId:
						gloss = (IWfiGloss) obj;
						analysis = obj.OwnerOfClass<IWfiAnalysis>();
						return;
				}
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
					WfiGlossTags.kflidForm, ws);
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

		private void LoadSecDataForEntry(ILexEntry entryReal, ILexSense senseReal, int hvoSbWord, IVwCacheDa cda, int wsVern,
			int hvoMbSec, int fGuessing, ISilDataAccess sdaMain, ITsStrFactory tsf)
		{
			int hvoEntry = m_caches.FindOrCreateSec(entryReal.Hvo, kclsidSbNamedObj,
				hvoSbWord, ktagSbWordDummy);
			// try to determine if the given entry is a variant of the sense we passed in (ie. not an owner)
			ILexEntryRef ler = null;
			int hvoEntryToDisplay = entryReal.Hvo;
			if (senseReal != null)
			{
				if ((entryReal as ILexEntry).IsVariantOfSenseOrOwnerEntry(senseReal, out ler))
					hvoEntryToDisplay = senseReal.EntryID;
			}

			ITsString tssLexEntry = LexEntryVc.GetLexEntryTss(Cache, hvoEntryToDisplay, wsVern, ler);
			cda.CacheStringAlt(hvoEntry, ktagSbNamedObjName, wsVern, tssLexEntry);
			cda.CacheObjProp(hvoMbSec, ktagSbMorphEntry, hvoEntry);
			cda.CacheIntProp(hvoEntry, ktagSbNamedObjGuess, fGuessing);
			List<int> writingSystems = m_choices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidLexEntries, 0);
			if (writingSystems.Count > 0)
			{
				// Sigh. We're trying for some reason to display other alternatives of the entry.
				int hvoLf = sdaMain.get_ObjectProp(hvoEntryToDisplay, LexEntryTags.kflidLexemeForm);
				if (hvoLf != 0)
					CopyStringsToSecondary(writingSystems, sdaMain, hvoLf,
						MoFormTags.kflidForm, cda, hvoEntry, ktagSbNamedObjName, tsf);
				else
					CopyStringsToSecondary(writingSystems, sdaMain, hvoEntryToDisplay,
						LexEntryTags.kflidCitationForm, cda, hvoEntry, ktagSbNamedObjName, tsf);
			}
		}

		private void CacheLexGlossWithInflTypeForAllCurrentWs(ILexEntry possibleVariant, int hvoLexSenseSec, int wsVern, IVwCacheDa cda,
			ILexEntryInflType inflType)
		{
			IList<int> currentAnalysisWsList = m_caches.MainCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle).ToArray();
			CacheStringAltForAllCurrentWs(currentAnalysisWsList, cda, hvoLexSenseSec, ktagSbNamedObjName,
				delegate(int wsLexGloss)
					{
						var hvoSenseReal = m_caches.RealHvo(hvoLexSenseSec);
						var sense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSenseReal);
						var spec = m_choices.CreateSpec(InterlinLineChoices.kflidLexGloss, wsLexGloss);
						var choices = new InterlinLineChoices(Cache, m_choices.m_wsDefVern,
															m_choices.m_wsDefAnal);
						choices.Add(spec);
						ITsString tssResult;
						return InterlinVc.TryGetLexGlossWithInflTypeTss(possibleVariant, sense, spec, choices, wsVern, inflType, out tssResult) ?
									tssResult : null;
					});
		}

		/// <summary>
		/// return the wordform that corresponds to the given form, or zero if none.
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		private IWfiWordform GetWordform(ITsString form)
		{
			IWfiWordform wordform;
			if (Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(form, out wordform))
				return wordform;
			return null;
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
				sel = RootBox.MakeSelInObj(0, rgvsli.Length, rgvsli, tag, fInstall);
			}
			catch (Exception)
			{
				// Ignore any problems
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
		/// <param name="cpropPrevious"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// <returns>true, if selection was successful.</returns>
		private bool MoveSelection(SelLevInfo[] rgvsli, int tag, int cpropPrevious, int ichAnchor, int ichEnd)
		{
			bool fSuccessful;
			try
			{
				RootBox.MakeTextSelection(0, rgvsli.Length, rgvsli, tag, cpropPrevious, ichAnchor, ichEnd, 0, false, -1, null, true);
				fSuccessful = true;
			}
			catch (Exception)
			{
				fSuccessful = false;
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
		/// <param name="hvoMorph"></param>
		/// <param name="form"></param>
		internal void EstablishDefaultEntry(int hvoMorph, string form, IMoMorphType mmt, bool fMonoMorphemic)
		{
			CheckDisposed();
			int hvoFormSec = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphForm);
			// remove any existing mapping for this morph form, which might exist
			// from a previous analysis
			m_caches.RemoveSec(hvoFormSec);
			var defFormReal = DefaultMorph(form, mmt);
			if (defFormReal == null)
				return; // this form never occurs anywhere, can't supply any default.
			var le = defFormReal.Owner as ILexEntry;
			int hvoEntry = m_caches.FindOrCreateSec(le.Hvo, kclsidSbNamedObj,
				kSbWord, ktagSbWordDummy);
			ITsString tssName;
			tssName = le.HeadWord;
			int wsVern = RawWordformWs;
			int hvoEntryToDisplay = le.Hvo;
			ILexEntryRef ler = DomainObjectServices.GetVariantRef(le, fMonoMorphemic);
			if (ler != null)
			{
				ICmObject coRef = ler.ComponentLexemesRS[0];
				if (coRef is ILexSense)
					hvoEntryToDisplay = (coRef as ILexSense).EntryID;
				else
					hvoEntryToDisplay = coRef.Hvo;
			}
			tssName = LexEntryVc.GetLexEntryTss(Cache, hvoEntryToDisplay, wsVern, ler);
			m_caches.DataAccess.SetMultiStringAlt(hvoEntry, ktagSbNamedObjName, this.RawWordformWs, tssName);
			m_caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphEntry, hvoEntry);
			m_caches.DataAccess.SetInt(hvoEntry, ktagSbNamedObjGuess, 1);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll,
				hvoMorph, ktagSbMorphGloss, 0, 1, 0);
			// Establish the link between the SbNamedObj that represents the MoForm, and the
			// selected MoForm.  (This is used when building the real WfiAnalysis.)
			m_caches.Map(hvoFormSec, defFormReal.Hvo);
			// This takes too long! Wait at least for a click in the bundle.
			//SetSelectedEntry(hvoEntryReal);
			EstablishDefaultSense(hvoMorph, le, null, null);
		}

		/// <summary>
		/// Find the MoForm, from among those whose form (in some ws) is the given one
		/// starting from the most frequently referenced by the MorphBundles property of a WfiAnalysis.
		/// If there is no wordform analysis, then fall back to selecting any matching MoForm.
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		internal IMoForm DefaultMorph(string form, IMoMorphType mmt)
		{
			CheckDisposed();
			// Find all the matching morphs and count how often used in WfiAnalyses
			int ws = RawWordformWs;
			// Fix FWR-2098 GJM: The definition of 'IsAmbiguousWith' seems not to include 'IsSameAs'.
			var morphs = (from mf in Cache.ServiceLocator.GetInstance<IMoFormRepository>().AllInstances()
						  where mf.Form.get_String(ws).Text == form && mf.MorphTypeRA != null
							&& (mf.MorphTypeRA == mmt || mf.MorphTypeRA.IsAmbiguousWith(mmt))
						  select mf).ToList();
			if (morphs.Count == 1)
				return morphs.First(); // special case: we can avoid the cost of figuring ReferringObjects.
			IMoForm bestMorph = null;
			var bestMorphCount = -1;
			foreach (var mf in morphs)
			{
				int count = (from source in mf.ReferringObjects where source is IWfiMorphBundle select source).Count();
				if (count > bestMorphCount)
				{
					bestMorphCount = count;
					bestMorph = mf;
				}
			}
			return bestMorph;
		}

		/// <summary>
		/// Given that we have made hvoEntryReal the lex entry for the (sandbox) morpheme
		/// hvoMorph, look for the sense given by hvoSenseReal and fill it in.
		/// </summary>
		/// <param name="hvoMorph">the sandbox id of the Morph object</param>
		/// <param name="entryReal">the real database id of the lex entry</param>
		/// <param name="senseReal">
		/// The real database id of the sense to use.  If zero, use the first sense of the entry
		/// (if there is one) as a default.
		/// </param>
		/// <param name="inflType"></param>
		/// <returns>default (real) sense if we found one, null otherwise.</returns>
		private ILexSense EstablishDefaultSense(int hvoMorph, ILexEntry entryReal, ILexSense senseReal, ILexEntryInflType inflType)
		{
			CheckDisposed();

			ILexSense variantSense = null;
			// If the entry has no sense we can't do anything.
			if (entryReal.SensesOS.Count == 0)
			{
				if (senseReal != null)
				{
					//if ((entryReal as ILexEntry).IsVariantOfSenseOrOwnerEntry(senseReal, out ler))
					variantSense = senseReal;
				}
				else
				{
					variantSense = GetSenseForVariantIfPossible(entryReal);
				}

				if (variantSense == null)
					return null; // nothing useful we can do.
			}
			// If we already have a gloss for this entry, don't overwrite it with a default.
			int hvoMorphGloss = m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
			if (hvoMorphGloss != 0 && entryReal.Hvo == m_hvoLastSelEntry && senseReal == null)
				return null;

			ILexSense defSenseReal;
			if (variantSense != null)
				defSenseReal = variantSense;
			else
			{
				if (senseReal == null)
					defSenseReal = entryReal.SensesOS[0];
				else
					defSenseReal = senseReal;
			}
			int hvoDefSense;
			if (variantSense != null && defSenseReal == variantSense)
			{
				hvoDefSense = m_caches.FindOrCreateSec(defSenseReal.Hvo, kclsidSbNamedObj, kSbWord, ktagSbWordDummy);
				var cda = (IVwCacheDa)m_caches.DataAccess;
				int wsVern = RawWordformWs;
				CacheLexGlossWithInflTypeForAllCurrentWs(entryReal, hvoDefSense, wsVern, cda, inflType);
				int hvoInflType = 0;
				if (inflType != null)
					hvoInflType = m_caches.FindOrCreateSec(inflType.Hvo, kclsidSbNamedObj, kSbWord, ktagSbWordDummy);
				cda.CacheObjProp(hvoMorph, ktagSbNamedObjInflType, hvoInflType);
				m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll,
						hvoMorph, ktagSbNamedObjInflType, 0, 1, 0);
			}
			else
			{
				// add normal LexGloss without variant info
				hvoDefSense = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidLexGloss, defSenseReal.Hvo,
					LexSenseTags.kflidGloss);
			}

			// We're guessing the gloss if we just took the first sense, but if the user chose
			// one it is definite.
			m_caches.DataAccess.SetInt(hvoDefSense, ktagSbNamedObjGuess, senseReal == null ? 1 : 0);

			m_caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphGloss, hvoDefSense);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll,
				hvoMorph, ktagSbMorphGloss, 0, 1, 0);

			// Now if the sense has an MSA, set that up as a default too.
			var defMsaReal = defSenseReal.MorphoSyntaxAnalysisRA;
			int cOldMsa = 0;
			if (m_caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphPos) != 0)
				cOldMsa = 1;
			if (defMsaReal != null)
			{
				int hvoNewPos = m_caches.FindOrCreateSec(defMsaReal.Hvo, kclsidSbNamedObj,
					kSbWord, ktagSbWordDummy);
				foreach (int ws in m_choices.WritingSystemsForFlid(InterlinLineChoices.kflidLexPos, true))
				{
					// Since ws maybe ksFirstAnal/ksFirstVern, we need to get what is actually
					// used in order to retrieve the data in Vc.Display().  See LT_7976.
					ITsString tssNew = defMsaReal.InterlinAbbrTSS(ws);
					int wsActual = TsStringUtils.GetWsAtOffset(tssNew, 0);
					m_caches.DataAccess.SetMultiStringAlt(hvoNewPos, ktagSbNamedObjName, wsActual, tssNew);
				}
				m_caches.DataAccess.SetInt(hvoNewPos, ktagSbNamedObjGuess, senseReal == null ? 1 : 0);
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
			return defSenseReal;
		}

		/// <summary>
		/// If hvoEntry is the id of a variant, try to find an entry it's a variant of that
		/// has a sense.  Return the corresponding ILexEntryRef for the first such entry.
		/// If this is being called to establish a default monomorphemic guess, skip over
		/// any bound root or bound stem entries that hvoEntry may be a variant of.
		/// </summary>
		public ILexEntryRef GetVariantRef(FdoCache cache, int hvoEntry, bool fMonoMorphemic)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			int cRef = sda.get_VecSize(hvoEntry, LexEntryTags.kflidEntryRefs);
			for (int i = 0; i < cRef; ++i)
			{
				int hvoRef = sda.get_VecItem(hvoEntry,
					LexEntryTags.kflidEntryRefs, i);
				int refType = sda.get_IntProp(hvoRef,
					LexEntryRefTags.kflidRefType);
				if (refType == LexEntryRefTags.krtVariant)
				{
					int cEntries = sda.get_VecSize(hvoRef,
						LexEntryRefTags.kflidComponentLexemes);
					if (cEntries != 1)
						continue;
					int hvoComponent = sda.get_VecItem(hvoRef,
						LexEntryRefTags.kflidComponentLexemes, 0);
					int clid = Caches.MainCache.ServiceLocator.ObjectRepository.GetObject(hvoComponent).ClassID;
					if (fMonoMorphemic && IsEntryBound(cache, hvoComponent, clid))
						continue;
					if (clid == LexSenseTags.kClassId ||
						sda.get_VecSize(hvoComponent, LexEntryTags.kflidSenses) > 0)
					{
						return Caches.MainCache.ServiceLocator.GetInstance<ILexEntryRefRepository>().GetObject(hvoRef);
					}
					else
					{
						// Should we check for a variant of a variant of a ...?
					}
				}
			}
			return null; // nothing useful we can do.
		}

		private ILexSense GetSenseForVariantIfPossible(ILexEntry entryReal)
		{
			ILexEntryRef ler = DomainObjectServices.GetVariantRef(entryReal, false);
			if (ler != null)
			{
				if (ler.ComponentLexemesRS[0] is ILexEntry)
					return (ler.ComponentLexemesRS[0] as ILexEntry).SensesOS[0];
				return ler.ComponentLexemesRS[0] as ILexSense;	// must be a sense!
			}
			return null;
		}

		/// <summary>
		/// Check whether the given entry (or entry owning the given sense) is either a bound
		/// root or a bound stem.  We don't want to use those as guesses for monomorphemic
		/// words.  See LT-10323.
		/// </summary>
		private static bool IsEntryBound(FdoCache cache, int hvoComponent, int clid)
		{
			int hvoTargetEntry;
			if (clid == LexSenseTags.kClassId)
			{
				ILexSense ls = cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoComponent);
				hvoTargetEntry = ls.Entry.Hvo;
				if (!(ls.MorphoSyntaxAnalysisRA is IMoStemMsa))
					return true;		// must be an affix, so it's bound by definition.
			}
			else
			{
				hvoTargetEntry = hvoComponent;
			}
			int hvoMorph = cache.MainCacheAccessor.get_ObjectProp(hvoTargetEntry,
				LexEntryTags.kflidLexemeForm);
			if (hvoMorph != 0)
			{
				int hvoMorphType = cache.MainCacheAccessor.get_ObjectProp(hvoMorph,
					MoFormTags.kflidMorphType);
				if (hvoMorphType != 0)
				{
					if (MorphServices.IsAffixType(cache, hvoMorphType))
						return true;
					Guid guid = cache.ServiceLocator.ObjectRepository.GetObject(hvoMorphType).Guid;
					if (guid == MoMorphTypeTags.kguidMorphBoundRoot ||
						guid == MoMorphTypeTags.kguidMorphBoundStem)
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
			SIL.Utils.Rect loc;
			vwselNew.GetParaLocation(out loc);
			if (!fMouseDown)
			{
				// It's a mouse move.
				// If we've moved to somewhere outside any paragraph get rid of the combos.
				// But, allow somewhere close, since otherwise it's almost impossible to get
				// a combo on an empty string.
				SIL.Utils.Rect locExpanded = loc;
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
				m_ComboHandler = InterlinComboHandler.MakeCombo(m_mediator.HelpTopicProvider,
					vwselNew, this, fMouseDown);
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
				if (InterlinDoc != null)
					InterlinDoc.ReallyScrollControlIntoView(this);
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
			SIL.Utils.Rect rect;
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

			SIL.Utils.Rect rect;
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
			SIL.Utils.Rect rect;
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

		/// <summary>
		/// Return the index of the line that contains the selection.
		/// </summary>
		public int GetLineOfCurrentSelection()
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

			// ISilDataAccess sda = Caches.DataAccess; // CS2019
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
			bool fIsPictureSel; // icon selected.

			try
			{
				IVwSelection sel = RootBox.Selection;
				if (sel == null)
				{
					// Select first icon
					MoveAnalysisIconOrNext();
					return;
				}
				fIsPictureSel = sel.SelType == VwSelType.kstPicture;
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
					NextPositionForLexEntryText(increment, fOnNextLine, fIsPictureSel, out currentLineIndex, out startLineIndex, ref iNextMorphIndex);
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
								NextPositionForLexEntryText(increment, fOnNextLine, fIsPictureSel, out currentLineIndex, out startLineIndex, ref iNextMorphIndex);
								break;
							case ktagSbWordPos: // line 7, WordPos.
								currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidWordPos);
								// try selecting the icon.
								if (increment < 0 && !fIsPictureSel)
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
			InterlinDocForAnalysis parent = InterlinDoc;
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
					return rightClickUiObj.HandleRightClick(Mediator, this, true, CmObjectUi.MarkCtrlClickItem);
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

		private void NextPositionForLexEntryText(int increment, bool fOnNextLine, bool fIsPictureSel,
			out int currentLineIndex, out int startLineIndex, ref int iNextMorphIndex)
		{
			currentLineIndex = m_choices.IndexOf(InterlinLineChoices.kflidLexEntries);
			if (increment < 0 && !fIsPictureSel)
			{
				// try selecting the icon just before the current selection.
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
		public bool SelectOnOrBeyondLine(int startLine, int increment)
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
			if (ParentForm == Form.ActiveForm)
				Focus();
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
			int hvoSbMorph = sda.MakeNewObject(SandboxBase.kclsidSbMorph, kSbWord,
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
			ITsString tssForm = sda.get_MultiStringAlt(kSbWord, SandboxBase.ktagSbWordForm, wsVern);
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
		internal IWfiAnalysis GetWfiAnalysisOfAnalysis()
		{
			CheckDisposed();
			return CurrentAnalysisTree.WfiAnalysis;
		}

		/// <summary>
		/// The wfi analysis currently used to setup the sandbox. Could have possibly come from a guess,
		/// not simply the current annotations InstanceOf.
		/// </summary>
		internal IWfiAnalysis GetWfiAnalysisInUse()
		{
			CheckDisposed();
			var wa = this.GetWfiAnalysisOfAnalysis();
			if (wa == null)
			{
				IWfiGloss temp_hvoWordGloss;
				this.GetDefaults(this.GetWordformOfAnalysis(), out wa, out temp_hvoWordGloss, false);
			}
			return wa;
		}


		/// <summary>
		/// Gets the WfiAnalysis HVO of the given analysis.
		/// </summary>
		/// <returns>This will return 0 if the analysis is on the wordform.</returns>
		internal IWfiAnalysis GetWfiAnalysisOfAnalysisObject(ICmObject analysisValueObject)
		{
			CheckDisposed();

			if (analysisValueObject == null || !analysisValueObject.IsValidObject)
				return null;
			switch (analysisValueObject.ClassID)
			{
				case WfiWordformTags.kClassId:
					return null;
				case WfiAnalysisTags.kClassId:
					return analysisValueObject as IWfiAnalysis;
				case WfiGlossTags.kClassId:
					return analysisValueObject.Owner as IWfiAnalysis;
				default:
					throw new Exception("Invalid type found in word analysis annotation");
			}
		}

		/// <summary>
		/// Copied from ChooseAnalysisHandler.
		/// Gets the wordform that owns the analysis we're
		/// displaying. (If there's no current analysis, or it is an object which someone
		/// has pathologically deleted in another window, return the current wordform.)
		/// It's possible for this to return null, if we're displaying a case-adjusted morpheme
		/// at the start of a sentence, and no wordform has been created for that case form.
		/// </summary>
		/// <returns></returns>
		internal IWfiWordform GetWordformOfAnalysis()
		{
			CheckDisposed();
			return CurrentAnalysisTree.Wordform;
		}

		/// <summary>
		/// The Wfi{Wordform, Analysis, [??MorphBundle,] Gloss} object in the main cache
		/// that is initially the original analysis we're working on and possibly replacing.
		/// Choosing an analysis on the Words line may change it to some other analysis (or gloss, etc).
		/// Choosing a different case form of the word (in the morphemes line) may change it to
		/// a Wordform for that case form, or zero if there is no Wordform yet for that case form.
		///
		/// The WfiWordform that we are currently displaying. Originally the wordform related to the
		/// the original m_hvoAnalysis. May become zero, if the user chooses an alternate case form
		/// that currently does not exist as a WfiWordform.
		/// </summary>
		internal AnalysisTree CurrentAnalysisTree
		{
			get; set;
		}

		private ICmObject m_currentGuess;
		/// <summary>
		/// The analysis we guessed (may actually be a WfiGloss). If we didn't guess, it's the actual
		/// analysis we started with.
		/// </summary>
		ICmObject CurrentGuess
		{
			get
			{
				if (m_currentGuess == null || m_currentGuess.Hvo != m_hvoAnalysisGuess)
				{
					if (m_hvoAnalysisGuess == 0)
						m_currentGuess = null;
					else
						m_currentGuess =
							Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoAnalysisGuess);
				}
				return m_currentGuess;
			}

			set
			{
				m_currentGuess = value;
				m_hvoAnalysisGuess = m_currentGuess != null ? m_currentGuess.Hvo : 0;
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
			AnalysisTree chosenAnalysis = handler.GetAnalysisTree();
			CurrentAnalysisTree = chosenAnalysis;
			bool fLookForDefaults = true;
			if (CurrentAnalysisTree.Analysis == null)
			{
				// 'Use default analysis'. This can normally be achieved by loading data
				// after setting m_hvoAnalysis to m_hvoWordformOriginal.
				// But possibly no default is loaded into the cache. (This can happen if
				// all visible occurrences of the wordform were analyzed before the sandbox was
				// displayed.)
				CurrentAnalysisTree.Analysis = m_wordformOriginal;
			}
			else if (CurrentAnalysisTree.Analysis == CurrentAnalysisTree.Wordform)
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
				case WfiWordformTags.kClassId:
					fMadeSelection = SelectAtStartOfMorph(0);
					break;
				case WfiGlossTags.kClassId:
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
		/// <returns></returns>
		internal ITsString FindAFullWordForm(IWfiWordform realWordform)
		{
			CheckDisposed();

			ITsString tssForm;
			if (realWordform == null)
			{
				tssForm = this.FormOfWordform; // use the one we saved.
			}
			else
			{
				tssForm = realWordform.Form.get_String(this.RawWordformWs);
			}
			return tssForm;
		}

		/// <summary>
		/// Handle setting the wordform shown in the sandbox when the user chooses an alternate
		/// case form in the Morpheme combo.
		/// </summary>
		/// <param name="form"></param>
		bool m_fSetWordformInProgress = false;

		/// <summary>
		/// store the color used to indicate if the wordform has multiple analyses
		/// </summary>
		private int m_multipleAnalysisColor = NoGuessColor;

		protected override void Select(bool directed, bool forward)
		{
			MakeDefaultSelection();
		}

		internal void SetWordform(ITsString form, bool fLookForDefaults)
		{
			CheckDisposed();
			// stop monitoring edits
			using (new SandboxEditMonitorHelper(m_editMonitor, true))
			{
				m_fSetWordformInProgress = true;
				try
				{
					m_tssWordform = form;
					CurrentAnalysisTree.Analysis = GetWordform(form);
					ISilDataAccess sda = m_caches.DataAccess;
					IVwCacheDa cda = (IVwCacheDa) m_caches.DataAccess;
					// Now erase the current morph bundles.
					cda.CacheVecProp(kSbWord, ktagSbWordMorphs, new int[0], 0);
					if (CurrentAnalysisTree.Analysis == null)
					{
						Debug.Assert(form != null);
						// No wordform exists corresponding to this case-form.
						// Put the sandbox in a special state where there is just one morpheme, the specified form.
						int hvoMorph0 = sda.MakeNewObject(kclsidSbMorph,
														  kSbWord, ktagSbWordMorphs, 0); // make just one new morpheme.
						int hvoNewForm = sda.MakeNewObject(kclsidSbNamedObj,
														   kSbWord, ktagSbWordDummy, 0);
							// make the object to be the form of the morpheme
						sda.SetMultiStringAlt(hvoNewForm, ktagSbNamedObjName, this.RawWordformWs, m_tssWordform);
							// set its text
						sda.PropChanged(null, (int) PropChangeType.kpctNotifyAll, hvoNewForm, ktagSbNamedObjName, 0, 1,
										1);
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
						sda.PropChanged(null, (int) PropChangeType.kpctNotifyAll, kSbWord, ktagSbWordForm, 0, 1, 1);
						// Just pretend the alternate wordform is our starting point.
						CurrentAnalysisTree.Analysis = CurrentAnalysisTree.Wordform;
						LoadRealDataIntoSec1(kSbWord, fLookForDefaults, false);
						Debug.Assert(CurrentAnalysisTree.Wordform != null);
					}
					//m_fForceReturnNewAnalysis = false;
					RootBox.Reconstruct();
				}
				finally
				{
					m_fSetWordformInProgress = false;
				}
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
		public bool ShouldSave(bool fSaveGuess)
		{
			return m_caches.DataAccess.IsDirty() || fSaveGuess && this.UsingGuess;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public AnalysisTree GetRealAnalysis(bool fSaveGuess, out IWfiAnalysis obsoleteAna)
		{
			CheckDisposed();

			obsoleteAna = null;
			if (ShouldSave(fSaveGuess))
			{
				FinishUpOk();
				var analMethod = CreateRealAnalysisMethod();
				CurrentAnalysisTree.Analysis = analMethod.Run();
				obsoleteAna = analMethod.ObsoleteAnalysis;
				UsingGuess = false;
				MarkAsInitialState();
			}
			return CurrentAnalysisTree;
		}

		protected GetRealAnalysisMethod CreateRealWfiAnalysisMethod()
		{
			return CreateRealAnalysisMethod(true);
		}

		protected GetRealAnalysisMethod CreateRealAnalysisMethod()
		{
			return CreateRealAnalysisMethod(false);
		}

		private GetRealAnalysisMethod CreateRealAnalysisMethod(bool fWantOnlyWfiAnalysis)
		{
			// NOTE: m_hvoWordform could come from a guess.
			IWfiGloss existingGloss = null;
			if (m_hvoWordGloss != 0)
				existingGloss = m_caches.MainCache.ServiceLocator.GetInstance<IWfiGlossRepository>().GetObject(m_hvoWordGloss);
			return new GetRealAnalysisMethod(
				m_mediator != null ? m_mediator.HelpTopicProvider : null, this, m_caches,
				kSbWord, CurrentAnalysisTree, GetWfiAnalysisOfAnalysis(), existingGloss,
				m_choices, m_tssWordform, fWantOnlyWfiAnalysis);
		}

		protected virtual void LoadForWordBundleAnalysis(int hvoWag)
		{
			CurrentAnalysisTree.Analysis = (IAnalysis)Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoWag);
			LoadRealDataIntoSec(true, TreatAsSentenceInitial);
			Debug.Assert(CurrentAnalysisTree.Wordform != null || m_tssWordform != null);
			m_wordformOriginal = CurrentAnalysisTree.Wordform;
			m_hvoInitialWag = hvoWag; // if we reset the focus box, this value we were passed is what we should reset it to.
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
			m_vc.MultipleOptionBGColor = MultipleAnalysisColor;
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Overide this to provide a context menu for some subclass.
		/// </summary>
		/// <param name="invSel"></param>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected override bool DoContextMenu(IVwSelection invSel, Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			if (SpellCheckHelper.ShowContextMenu(pt, this))
				return true;
			return base.DoContextMenu(invSel, pt, rcSrcRoot, rcDstRoot);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets (creating if necessary) the SpellCheckHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private SpellCheckHelper SpellCheckHelper
		{
			get
			{
				if (m_spellCheckHelper == null)
					m_spellCheckHelper = new SpellCheckHelper(Cache);
				return m_spellCheckHelper;
			}
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
			if (!m_editMonitor.IsPropMorphBreak(hvoObj, tag, ws))
				return VwDelProbResponse.kdprFail;
			switch (dpt)
			{
				case VwDelProbType.kdptBsAtStartPara:
				case VwDelProbType.kdptBsReadOnly:
					return m_editMonitor.HandleBackspace() ?
						VwDelProbResponse.kdprDone : VwDelProbResponse.kdprFail;
				case VwDelProbType.kdptDelAtEndPara:
				case VwDelProbType.kdptDelReadOnly:
					return m_editMonitor.HandleDelete() ?
						VwDelProbResponse.kdprDone : VwDelProbResponse.kdprFail;
				default:
					return VwDelProbResponse.kdprFail;
			}
		}

		// Handles a change in the view selection.
		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.HandleSelectionChange(rootb, vwselNew);
			if (!vwselNew.IsValid)
				return;
			m_editMonitor.DoPendingMorphemeUpdates();
			if (!vwselNew.IsValid)
				return;
			DoActionOnIconSelection(vwselNew);
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
				else
				{
					OnHandleEnter();
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

		protected virtual void OnHandleEnter()
		{
			// base implementation doesn't do anything.
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
			if (DataUpdateMonitor.IsUpdateInProgress())
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
				SelLevInfo.AllTextSelInfo(sel, cvsli,
					out ihvoRoot, out tagRightClickTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
				if (tagRightClickTextProp >= ktagMinIcon && tagRightClickTextProp < ktagLimIcon) // it's an icon
				{
					// don't bother doing anything for clicks on the icons.
					return false;
				}
				else
				{
					int hvoReal;
					tagRightClickTextProp = GetInfoForJumpToTool(sel, out hvoReal);
					if (hvoReal != 0)
					{
						//IxCoreColleague spellingColleague = null;
						if (tagRightClickTextProp == ktagSbWordGloss)
						{
							if (SpellCheckHelper.ShowContextMenu(pt, this))
								return true;
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

		/// <summary>
		/// Given a selection (typically from a click), determine the object that should be the target for jumping to,
		/// and return the property that was clicked (which in the case of a right-click may generate a spelling menu instead).
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="hvoReal"></param>
		/// <returns></returns>
		private int GetInfoForJumpToTool(IVwSelection sel, out int hvoReal)
		{
			int ws;
			int tagRightClickTextProp;
			bool fAssocPrev;
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

			hvoReal = m_caches.RealHvo(hvoRightClickObject);
			return tagRightClickTextProp;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="CmObjectUi.HandleCtrlClick disposes itself when its done")]
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (e.Button == MouseButtons.Left && (ModifierKeys & Keys.Control) == Keys.Control)
			{
				// Control-click: take the first jump-to-tool command from the right-click menu for this location.
				// Create a selection where we right clicked
				IVwSelection sel = GetSelectionAtPoint(new Point(e.X, e.Y), false);
				int hvoTarget;
				GetInfoForJumpToTool(sel, out hvoTarget);
				if (hvoTarget == 0)
					return; // LT-13878: User may have 'Ctrl+Click'ed on an arrow or off in space somewhere
				CmObjectUi targetUiObj = CmObjectUi.MakeUi(Cache, hvoTarget);
				targetUiObj.HandleCtrlClick(Mediator, this);
			}
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
			if (CurrentAnalysisTree.Analysis != null)
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
			if (CurrentGuess != null)
				clid = CurrentGuess.ClassID;
			int hvoMsa;
			switch (className)
			{
				case "WfiWordform":
					return CurrentAnalysisTree.Wordform.Hvo;
				case "WfiAnalysis":
					{
						if (clid == WfiAnalysisTags.kClassId)
							return m_hvoAnalysisGuess;
						else if (clid == WfiGlossTags.kClassId)
							return CurrentGuess.OwnerOfClass(WfiAnalysisTags.kClassId).Hvo;
					}
					break;
				case "WfiGloss":
					{
						if (clid == WfiGlossTags.kClassId)
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
					hvoMsa = GetObjectFromRightClickMorph(ktagSbMorphPos);
					if (hvoMsa == 0)
						return 0;
					// TODO: We really want the guid, and it's usually just as accessible as
					// the hvo, so methods like have been migrating to returning the guid.
					// This method should do likewise...
					using (CmObjectUi ui = CmObjectUi.MakeUi(m_caches.MainCache, hvoMsa))
					{
						Guid guid = ui.GuidForJumping(null);
						if (guid == Guid.Empty)
							return 0;
						else
							return Cache.ServiceLocator.GetObject(guid).Hvo;
					}
				//LT-12195 Change Show Concordance of Category right click menu item for Lex Gram. Info. line of Interlinear.
				case "PartOfSpeechGramInfo":
					hvoMsa = GetObjectFromRightClickMorph(ktagSbMorphPos);
					return hvoMsa;
				case "WordPartOfSpeech":
					hvoMsa = GetObjectFromRightClickMorph(ktagSbWordPos);
					if (hvoMsa != 0)
						return hvoMsa;
					IWfiAnalysis realAnalysis = null;
					if (clid == WfiAnalysisTags.kClassId)
						realAnalysis = CurrentGuess as IWfiAnalysis;
					else if (clid == WfiGlossTags.kClassId)
						realAnalysis = CurrentGuess.OwnerOfClass(WfiAnalysisTags.kClassId) as IWfiAnalysis;
					if (realAnalysis != null && realAnalysis.CategoryRA != null)
						// JohnT: not sure it CAN be null, but play safe.
						return realAnalysis.CategoryRA.Hvo;
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
			List<ILexEntry> homographs = null;
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () => { homographs =
				Cache.ServiceLocator.GetInstance<ILexEntryRepository>().CollectHomographs(wordform.Text,
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem));
				});
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

		private FocusBoxController Controller
		{
			get
			{
				var container = Parent;
				while (container != null)
				{
					if (container is FocusBoxController)
						return (FocusBoxController) container;
					container = container.Parent;
				}
				return null;
			}
		}

		public virtual bool OnJumpToTool(object commandObject)
		{
			XCore.Command cmd = (XCore.Command)commandObject;
			string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			string concordOn = SIL.Utils.XmlUtils.GetOptionalAttributeValue(cmd.Parameters[0], "concordOn", "");

			if (CurrentAnalysisTree.Analysis != null)
			{
				// If the user selects a concordance on gloss or analysis, we want the current one,
				// not what we started with. We would save anyway as we switched views, so do it now.
				var parent = Controller;
				if (parent != null)
					parent.UpdateRealFromSandbox(null, false, null);
				// This leaves the parent in a bad state, but maybe it would be good if all this is
				// happening in some other parent, such as the words analysis view?
				//m_hvoAnalysisGuess = GetRealAnalysis(false);
				int hvo = GetHvoForJumpToToolClass(className);
				if (hvo != 0)
				{
					FdoCache cache = m_caches.MainCache;
					ICmObject co = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					var fwLink = new FwLinkArgs(tool, co.Guid);
					List<Property> additionalProps = fwLink.PropertyTableEntries;
					if (!String.IsNullOrEmpty(concordOn))
						additionalProps.Add(new Property("ConcordOn", concordOn));
					m_mediator.PostMessage("FollowLink", fwLink);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Do nothing if the Sandbox somehow gets refreshed directly...its parent window destroys and
		/// recreates it.
		/// </summary>
		public override bool RefreshDisplay()
		{
			CheckDisposed();
			return false;
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
					m_fSuppressShowCombo = false;
					m_rootb.MakeSelAt(point.X, point.Y, rcSrcRoot, rcDstRoot, true);
					m_fSuppressShowCombo = true;
				}
				catch (Exception e)
				{
					Debug.WriteLine(e.Message);
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
		public override bool ScrollSelectionIntoView(IVwSelection sel, VwScrollSelOpts scrollOption)
		{
			CheckDisposed();
			return false;
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

		/// <summary>
		/// Never show the style combo box in the toolbar while focused in the Sandbox.
		/// </summary>
		public override bool OnDisplayBestStyleName(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			if (!Focused)
				return false;
			display.Enabled = false;
			display.Text = SIL.FieldWorks.Resources.ResourceHelper.DefaultParaCharsStyleName;
			return true;//we handled this, no need to ask anyone else.
		}

		#endregion Overrides of RootSite
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
}
