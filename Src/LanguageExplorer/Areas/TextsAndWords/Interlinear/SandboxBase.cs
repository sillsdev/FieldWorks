//#define TraceMouseCalls		// uncomment this line to trace mouse messages
// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;
using Rect = SIL.FieldWorks.Common.ViewsInterfaces.Rect;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal partial class SandboxBase : RootSite, IUndoRedoHandler
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
		protected internal const int ktagSbWordGloss = 6902021;

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
		protected internal const int ktagWordGlossIcon = 6905023;
		protected internal const int ktagWordPosIcon = 6905024;
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
		internal const int kSbWord = 10000007;

		#endregion Constants

		#region Data members

		private int m_hvoLastSelEntry; // HVO in real cache of last selected lex entry.

		// This object monitors property changes in the sandbox, primarily for edits to the morpheme breakdown.
		// It can also be used to implement some problem deletions.

		/// <summary>
		/// This object monitors property changes in the sandbox, primarily for edits to the morpheme breakdown.
		/// It can also be used to implement some problem deletions.
		/// </summary>
		internal SandboxEditMonitor EditMonitor { get; private set; }

		protected int m_hvoInitialWag;
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
		// Indicates the case status of the wordform.
		// If m_hvoWordform is set to zero, this should be set to the actual text that should be
		// assigned to the new Wordform that will be created if GetRealAnalysis is called.
		// The original Gloss we started with. ReviewP: Can we get rid of this?

		private bool m_fSuppressShowCombo = true; // set to prevent SelectionChanged displaying combo.

		internal IComboHandler m_ComboHandler; // handles most kinds of combo box.
		protected SandboxVc m_vc;
		private Point m_LastMouseMovePos;
		// Rectangle containing last selection passed to ShowComboForSelection.
		private Rect m_locLastShowCombo;
		// True during calls to MakeCombo to suppress selected index changed effects.
		private bool m_fMakingCombo;
		// True when the user has started editing text in the combo. Blocks moving and
		// reinitializing the combo on mouse move, and ensures that we do something with what
		// the user has typed on OK and mousedown elsewhere.
		private bool m_fLockCombo;
		// True to lay out with infinite width, expecting to be fully visible.
		private bool m_fSizeToContent;
		// We'd like to just use the VC's copy, but it may not get made in time.
		private bool m_fShowMorphBundles = true;

		// Flag used to prevent mouse move events from entering CallMouseMoveDrag multiple
		// times before prior ones have exited.  Otherwise we get lines displayed multiple
		// times while scrolling during a selection.
		private bool m_fMouseInProcess;

		// This is set true by CallMouseMoveDrag if m_fNewSelection is true, and set false by
		// either CallMouseDown or CallMouseUp.  It controls whether CallMouseUp creates a
		// range selection, and also controls whether ShowComboForSelection actually creates
		// and shows the dropdown list.
		private bool m_fInMouseDrag;

		// This is set true by CallMouseDown after a new selection is created, and reset to
		// false by CallMouseUp.
		private bool m_fNewSelection;

		// This flag handles keeping the combo dropdown list open after you click on the arrow
		// but then proceed to drag before letting up on the mouse button.
		private bool m_fMouseDownActivatedCombo;

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
		protected virtual bool IsMorphemeFormEditable => true;

		public void UpdateLineChoices(InterlinLineChoices choices)
		{
			InterlinLineChoices = choices;
			m_vc?.UpdateLineChoices(choices);
			m_rootb.Reconstruct();
		}

		/// <summary>
		/// When sandbox is visible, it should be the same as the InterlinDocForAnalysis m_hvoAnnotation.
		/// However, when the Sandbox is not visible the parent is setting/sizing things up for a new annotation.
		/// </summary>
		public virtual int HvoAnnotation => m_occurrenceSelected.Analysis.Hvo;

		/// <summary>
		/// The writing system of the wordform in this analysis.
		/// </summary>
		int m_wsRawWordform;
		protected internal virtual int RawWordformWs
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

		internal InterlinLineChoices InterlinLineChoices { get; set; }

		/// <summary>
		/// Returns the count of Sandbox.WordGlossHvo the used (across records) in the Text area.
		/// (cf. LT-1428)
		/// </summary>
		internal virtual int WordGlossReferenceCount => 0;

		protected CaseFunctions VernCaseFuncs(ITsString tss)
		{
			var locale = Caches.MainCache.ServiceLocator.WritingSystemManager.Get(TsStringUtils.GetWsAtOffset(tss, 0)).IcuLocale;
			return new CaseFunctions(locale);
		}

		protected bool ComboOnMouseHover => false;

		protected bool IconsForAnalysisChoices => true;

		protected bool IsIconSelected => new TextSelInfo(RootBox).IsPicture;

		/// <summary>
		/// the given word is a phrase if it has any word breaking space characters
		/// </summary>
		internal static bool IsPhrase(string word)
		{
			return !string.IsNullOrEmpty(word) && word.IndexOfAny(Unicode.SpaceChars) != -1;
		}

		/// <summary>
		/// Return true if there is no analysis worth saving.
		/// </summary>
		internal bool IsAnalysisEmpty
		{
			get
			{
				var sda = Caches.DataAccess;
				// See if any alternate writing systems of word line are filled in.
				var wordformWss = InterlinLineChoices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidWord, 0);
				foreach (int wsId in wordformWss)
				{
					if (sda.get_MultiStringAlt(kSbWord, ktagSbWordForm, wsId).Length > 0)
					{
						return false;
					}
				}

				if (!IsMorphFormLineEmpty)
				{
					return false;
				}

				if (HasWordGloss())
				{
					return false;
				}
				// If we found nothing yet, it's non-empty if a word POS has been chosen.
				return !HasWordCat;
			}
		}

		/// <summary>
		/// LT-7807. Controls whether or not confirming the analyses will try to update the Lexicon.
		/// Only true for monomorphemic analyses.
		/// </summary>
		internal virtual bool ShouldAddWordGlossToLexicon
		{
			get
			{
				if (InterlinDoc == null)
				{
					return false;
				}
				return InterlinDoc.InModeForAddingGlossedWordsToLexicon && MorphCount == 1;
			}
		}

		/// <summary>
		/// True if user is in gloss tab.
		/// </summary>
		private bool IsInGlossMode()
		{
			var master = InterlinDoc?.GetMaster();
			return master?.InterlinearTab == TabPageSelection.Gloss;
		}

		internal bool HasWordCat => Caches.DataAccess.get_ObjectProp(kSbWord, ktagSbWordPos) != 0;

		internal bool HasWordGloss()
		{
			var sda = Caches.DataAccess;
			foreach (var wsId in InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss))
			{
				// some analysis exists if any gloss multistring has content.
				if (sda.get_MultiStringAlt(kSbWord, ktagSbWordGloss, wsId).Length > 0)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This is useful for detecting whether or not the user has deleted the entire morpheme line
		/// (cf. LT-1621).
		/// </summary>
		internal bool IsMorphFormLineEmpty
		{
			get
			{
				var cmorphs = MorphCount;
				if (cmorphs == 0)
				{
					//Debug.Assert(!ShowMorphBundles); // if showing should always have one.
					// JohnT: except when the user turned on morphology while the Sandbox was active...
					return true;
				}

				if (MorphCount != 1)
				{
					return false;
				}
				var hvoMorph = Caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, 0);
				var tssFullForm = GetFullMorphForm(hvoMorph);
				return tssFullForm.Length == 0;
			}
		}

		/// <summary>
		/// Return the count of morphemes.
		/// </summary>
		public int MorphCount => Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);

		/// <summary>
		/// Return the list of msas as hvos
		/// </summary>
		public List<int> MsaHvoList
		{
			get
			{
				var chvo = MorphCount;
				using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
				{
					Caches.DataAccess.VecProp(kSbWord, ktagSbWordMorphs, chvo, out chvo, arrayPtr);
					var morphsHvoList = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
					var msas = new List<int>(morphsHvoList.Length);
					msas.AddRange(morphsHvoList.Select((t, i) => (int) morphsHvoList.GetValue(i)).Where(hvo => hvo != 0)
						.Select(hvo => Caches.DataAccess.get_ObjectProp(hvo, ktagSbMorphPos)).Select(msaSecHvo => Caches.RealHvo(msaSecHvo)));
					return msas;
				}
			}
		}

		protected internal CachePair Caches { get; protected set; } = new CachePair();

		public virtual ITsString RawWordform
		{
			get
			{
				if (m_rawWordform != null && m_rawWordform.Length != 0)
				{
					return m_rawWordform;
				}
				var wf = CurrentAnalysisTree.Wordform;
				m_rawWordform = m_wsRawWordform != 0 ? wf.Form.get_String(m_wsRawWordform) : wf.Form.BestVernacularAlternative;
				return m_rawWordform;
			}
			set
			{
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
		public bool TreatAsSentenceInitial { get; set; } = true;

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
				if (m_multipleAnalysisColor == value)
				{
					// Avoid unnecessary side affects if no actual change is occurring.
					return;
				}
				m_multipleAnalysisColor = value;
				//if we are having our information about multiple analysis state update,
				//then update the vc if it already exists so that it will agree.
				if (m_vc != null)
				{
					m_vc.MultipleOptionBGColor = m_multipleAnalysisColor;
				}
			}
		}

		protected static int NoGuessColor => (int)CmObjectUi.RGB(DefaultBackColor);


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
		public bool RightToLeftWritingSystem => m_vc != null && m_vc.RightToLeft;

		// Controls whether to display the morpheme bundles.
		public bool ShowMorphBundles
		{
			get
			{
				return m_fShowMorphBundles;
			}
			set
			{
				m_fShowMorphBundles = value;
				if (m_vc != null)
				{
					m_vc.ShowMorphBundles = value;
				}
			}
		}

		/// <summary>
		/// Finds the interlinDoc that this Sandbox is embedded in.
		/// </summary>
		internal virtual InterlinDocForAnalysis InterlinDoc => null;

		internal int RootWordHvo => kSbWord;

		/// <summary>
		/// True if the combo on the Wordform line is wanted (there are known analyses).
		/// </summary>
		internal bool ShowAnalysisCombo { get; private set; } = true;

		/// <summary>
		/// The index of the word we're editing among the context words we're showing.
		/// Currently this is also it's index in the list of root objects.
		/// </summary>
		internal int IndexOfCurrentItem => 0;

		internal SandboxEditMonitor SandboxEditMonitor => EditMonitor;

		public bool SizeToContent
		{
			get
			{
				return m_fSizeToContent;
			}
			set
			{
				m_fSizeToContent = value;
				// If we are changing the window size to match the content, we don't want to autoscroll.
				AutoScroll = !m_fSizeToContent;
			}
		}
		internal ChooseAnalysisHandler FirstLineHandler { get; set; }

		/// <summary>
		/// Triggered to tell clients that the Sandbox has changed (e.g. been edited from
		/// its initial state) to help determine whether we should allow trying to save or
		/// undo the changes.
		///
		/// Currently triggered by SandboxEditMonitor.PropChanged whenever a property changes on the cache
		/// </summary>
		internal void OnUpdateEdited()
		{
			var fIsEdited = Caches.DataAccess.IsDirty();
			//The user has now approved this candidate, remove any ambiguity indicating color.
			if (fIsEdited)
			{
				MultipleAnalysisColor = NoGuessColor;
			}

			SandboxChangedEvent?.Invoke(this, new SandboxChangedEventArgs(fIsEdited));
		}

		protected void ReconstructForWordBundleAnalysis(int hvoWag)
		{
			m_fHaveUndone = false;
			HideCombos(); // Usually redundant, but MUST not have one around hooked to old data.
			LoadForWordBundleAnalysis(hvoWag);
			if (m_rootb == null)
			{
				MakeRoot();
			}
			else
			{
				m_rootb.Reconstruct();
			}
		}

		internal void MarkAsInitialState()
		{
			// As well as noting that this IS the initial state, we want to record some things about the initial state,
			// for possible use when resetting the focus box.
			// Generally m_hvoInitialWag is more reliably set in LoadForWordBundleAnalysis, since by the time
			// MarkInitialState is called, CurrentAnalysisTree.Analysis may have been cleared (e.g., if we are
			// glossing a capitalized word at the start of a segment). It is important to set it here when saving
			// an updated analysis, so that a subsequent undo will restore it to the saved value.
			m_hvoAnalysisGuess = m_hvoInitialWag = CurrentAnalysisTree.Analysis?.Hvo ?? 0;
			Caches.DataAccess.ClearDirty(); // indicate we've loaded or saved.
			OnUpdateEdited();	// tell client we've updated the state of the sandbox.
		}

		/// <summary>
		/// Indicates the case of the Wordform we're dealing with.
		/// </summary>
		public StringCaseStatus CaseStatus { get; private set; }

		/// <summary>
		/// The analysis object (in the real cache) that we started out looking at.
		/// </summary>
		public int Analysis
		{
			get
			{
				if (CurrentAnalysisTree == null || CurrentAnalysisTree.Analysis == null)
				{
					return 0;
				}
				return CurrentAnalysisTree.Analysis.Hvo;
			}
		}

		/// <summary>
		/// Used to save the appropriate form to use for the new Wordform
		/// that will be created if an alternate-case wordform that does not already exist is confirmed.
		/// Also used as the first menu item in the morphemes menu when m_hvoWordform is zero.
		/// </summary>
		internal ITsString FormOfWordform { get; private set; }

		/// <summary>
		/// This is the WordGloss that the Sandbox was initialized with, either from the initial WAG or
		/// from a guess.
		/// </summary>
		internal int WordGlossHvo { get; set; }

		/// <summary>
		/// if the (anchor) selection is inside the display of a morpheme, return the index of that morpheme.
		/// Otherwise, return -1.
		/// </summary>
		protected internal int MorphIndex
		{
			get
			{
				var tsi = new TextSelInfo(RootBox);
				if (tsi.ContainingObjectTag(tsi.Levels(false) - 1) != ktagSbWordMorphs || tsi.TagAnchor == ktagMorphFormIcon) // don't count the morpheme dropdown icon.
				{
					return -1;
				}
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
				var tsi = new TextSelInfo(RootBox);
				if (tsi.IsRange || tsi.IchEnd != 0 || tsi.Selection == null)
				{
					return false;
				}

				if (tsi.TagAnchor == ktagSbMorphPrefix)
				{
					return true;
				}

				if (tsi.TagAnchor != ktagSbNamedObjName)
				{
					return false;
				}
				// only the first Morpheme line is currently displaying prefix/postfix.
				var currentLine = GetLineOfCurrentSelection();
				if (currentLine != -1 && InterlinLineChoices.IsFirstOccurrenceOfFlid(currentLine))
				{
					return (tsi.ContainingObjectTag(1) == ktagSbMorphForm && Caches.DataAccess.get_StringProp(tsi.ContainingObject(1), ktagSbMorphPrefix).Length == 0);
				}
				return tsi.IchAnchor == 0;
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
				var tsi = new TextSelInfo(RootBox);
				if (tsi.IsRange || tsi.IchEnd != tsi.AnchorLength || tsi.Selection == null)
				{
					return false;
				}

				if (tsi.TagAnchor == ktagSbMorphPostfix)
				{
					return true;
				}

				if (tsi.TagAnchor != ktagSbNamedObjName)
				{
					return false;
				}
				// only the first Morpheme line is currently displaying prefix/postfix.
				var currentLine = GetLineOfCurrentSelection();
				if (currentLine != -1 && InterlinLineChoices.IsFirstOccurrenceOfFlid(currentLine))
				{
					return (tsi.ContainingObjectTag(1) == ktagSbMorphForm && Caches.DataAccess.get_StringProp(tsi.ContainingObject(1), ktagSbMorphPostfix).Length == 0);
				}
				return tsi.AnchorLength == tsi.IchEnd;
			}
		}

		/// <summary>
		/// True if the selection is on the right edge of the morpheme.
		/// </summary>
		private bool IsSelAtRightOfMorph => m_vc.RightToLeft ? IsSelAtStartOfMorph : IsSelAtEndOfMorph;

		/// <summary>
		/// True if the selection is on the left edge of the morpheme.
		/// </summary>
		private bool IsSelAtLeftOfMorph => m_vc.RightToLeft ? IsSelAtEndOfMorph : IsSelAtStartOfMorph;

		protected bool IsWordPosIconSelected
		{
			get
			{
				var tsi = new TextSelInfo(RootBox);
				return tsi.IsPicture && tsi.TagAnchor == ktagWordPosIcon;
			}
		}

		#endregion Properties

		#region Construction, initialization & Disposal

		public SandboxBase()
		{
			SubscribeToRootSiteEventHandlerEvents();
			InitializeComponent();
			CurrentAnalysisTree = new AnalysisTree();
			// Tab should move between the piles inside the focus box!  See LT-9228.
			AcceptsTab = true;
			SuppressPrintHandling = true; // The SandBox is never the control that should print.
		}

		public SandboxBase(LcmCache cache, IVwStylesheet ss, InterlinLineChoices choices)
			: this()
		{
			// Override things from InitializeComponent()
			BackColor = Color.FromKnownColor(KnownColor.Control);

			// Setup member variables.
			Caches.MainCache = cache;
			Cache = cache;
			// We need to set this for various inherited things to work,
			// for example, automatically setting the correct keyboard.
			WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			Caches.CreateSecCache();
			InterlinLineChoices = choices;
			m_stylesheet = ss; // this is really redundant now it inherits a StyleSheet property.
			StyleSheet = ss;
			EditMonitor = new SandboxEditMonitor(this); // after creating sec cache.
		}

		public SandboxBase(LcmCache cache, IVwStylesheet ss, InterlinLineChoices choices, int hvoAnalysis)
			: this(cache, ss, choices)
		{
			// finish setup with the WordBundleAnalysis
			LoadForWordBundleAnalysis(hvoAnalysis);
		}

		#region Overrides of SimpleRootSite

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			PropertyTable.SetProperty("FirstControlToHandleMessages", this, settingsGroup: SettingsGroup.LocalSettings);
		}

		#endregion

		private void OpenComboBox(IVwSelection selection)
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
			if (MiscUtils.IsMono && (Form.ActiveForm as IFwMainWnd) != null)
			{
#if RANDYTODO
				(Form.ActiveForm as IFwMainWnd).DesiredControl = this;
#endif
			}
		}

		/// <summary>
		/// When a sandbox is destroyed, inform the main window that it no longer
		/// exists to receive keyboard input.  (See FWNX-785.)
		/// </summary>
		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			if (MiscUtils.IsMono && (Form.ActiveForm as IFwMainWnd) != null)
			{
#if RANDYTODO
				(Form.ActiveForm as IFwMainWnd).DesiredControl = null;
#endif
			}
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (EditMonitor == null)
			{
				return;
			}

			if (Visible)
			{
				EditMonitor.StartMonitoring();
			}
			else
			{
				EditMonitor.StopMonitoring();
			}
		}

		// Cf. the SandboxBase.Designer.cs file for this method.
		//protected override void Dispose(bool disposing)

		#endregion Construction, initialization & Disposal

		private void SubscribeToRootSiteEventHandlerEvents()
		{
#if __MonoCS__
			var ibusRootSiteEventHandler = m_rootSiteEventHandler as IbusRootSiteEventHandler;
			if (ibusRootSiteEventHandler != null)
			{
				ibusRootSiteEventHandler.PreeditOpened += OnPreeditOpened;
				ibusRootSiteEventHandler.PreeditClosed += OnPreeditClosed;
			}
#endif
		}

		private void OnPreeditOpened (object sender, EventArgs e)
		{
			// While the pre-edit window is open we don't want to check for new morpheme breaks
			// (see LT-16237)
			SandboxEditMonitor.StopMonitoring();
		}

		private void OnPreeditClosed (object sender, EventArgs e)
		{
			SandboxEditMonitor.StartMonitoring();
		}

		private bool PassKeysToKeyboardHandler => !SandboxEditMonitor.IsMonitoring;


		#region Other methods

		/// <summary>
		/// Load the real data into the secondary cache.
		/// </summary>
		/// <param name="fAdjustCase">If true, may adjust case of morpheme when
		/// proposing whole word as default morpheme breakdown.</param>
		/// <param name="fLookForDefaults">If true, will try to guess most likely analysis.</param>
		/// <param name="fClearDirty">if true, establishes the loaded cache state for the sandbox,
		/// so that subsequent changes can be undone or saved with respect to this initial state.</param>
		private void LoadRealDataIntoSec(bool fLookForDefaults, bool fAdjustCase, bool fClearDirty = true)
		{
			var cda = (IVwCacheDa)Caches.DataAccess;
			cda.ClearAllData();
			Caches.ClearMaps();

			// If we don't have a real root object yet, we can't set anything up.
			if (CurrentAnalysisTree.Analysis == null)
			{
				// This probably paranoid, but it's safe.
				Debug.WriteLine("loading Sandbox for missing analysis");
				m_wordformOriginal = null;
				CaseStatus = StringCaseStatus.allLower;
				return;
			}

			// stop monitoring cache since we are about to make some drastic changes.
			using (new SandboxEditMonitorHelper(EditMonitor, true))
			{
				UsingGuess = LoadRealDataIntoSec1(kSbWord, fLookForDefaults, fAdjustCase);
				Debug.Assert(CurrentAnalysisTree.Wordform != null || FormOfWordform != null);

				// At this point the only reason to force the current displayed analysis
				// to be returned instead of the original is if we're guessing.
				//m_fForceReturnNewAnalysis = fGuessing;

				// Treat initial state (including guessing) as something you can leave without saving.
				// Make sure it doesn't think any edits have happened, even if reusing from some other word.
				if (fClearDirty)
				{
					MarkAsInitialState();
				}
			}
		}

		///  <summary />
		/// <param name="hvoSbWord">either m_hvoSbWord, m_hvoPrevSbWordb, or m_hvoNextSbWord
		/// </param>
		/// <param name="fLookForDefaults"></param>
		/// <param name="fAdjustCase">If true, may adjust case of morpheme when
		/// proposing whole word as default morpheme breakdown.</param>
		/// <returns>true if any guessing is involved.</returns>
		private bool LoadRealDataIntoSec1(int hvoSbWord, bool fLookForDefaults, bool fAdjustCase)
		{
			var cda = (IVwCacheDa)Caches.DataAccess;
			if (CurrentAnalysisTree.Analysis == null)
			{
				// should we empty the cache of any stale data?
				return false;
			}
			m_hvoLastSelEntry = 0;	// forget last Lex Entry user selection. We're resync'ing everything.
			var analysis = CurrentAnalysisTree.WfiAnalysis;
			var gloss = CurrentAnalysisTree.Gloss;
			WordGlossHvo = gloss?.Hvo ?? 0;
			var fGuessing = 0;  // Use 0 or 1, as we store it in an int dummy property.

			RawWordform = null; // recompute based upon wordform.
			var wsVern = RawWordformWs;
			Caches.Map(hvoSbWord, CurrentAnalysisTree.Wordform.Hvo); // Review: any reason to map these?
			var sdaMain = Caches.MainCache.MainCacheAccessor;
			CopyStringsToSecondary(InterlinLineChoices.kflidWord, sdaMain, CurrentAnalysisTree.Wordform.Hvo, WfiWordformTags.kflidForm, cda, hvoSbWord, ktagSbWordForm);
			var cf = VernCaseFuncs(RawWordform);
			CaseStatus = cf.StringCase(RawWordform.Text);
			// empty it in case we're redoing after choose from combo.
			cda.CacheVecProp(hvoSbWord, ktagSbWordMorphs, new int[0], 0);
			if (gloss == null || analysis == null)
			{
				if (fLookForDefaults)
				{
					if (InterlinDoc != null) // can be null in Wordform Analyses tool and some unit tests, and we don't want to clear an existing analysis.
					{
						GetDefaults(CurrentAnalysisTree.Wordform, ref analysis, out gloss);
					}
					WordGlossHvo = gloss?.Hvo ?? 0;
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
							CopyStringsToSecondary(InterlinLineChoices.kflidWord, sdaMain, CurrentAnalysisTree.Wordform.Hvo, WfiWordformTags.kflidForm, cda, hvoSbWord, ktagSbWordForm);
						}
					}
					// Hide the analysis combo if there's no default analysis (which means there are
					// no options to list).
					ShowAnalysisCombo = (analysis != null);
					fGuessing = 1;
				}
				else if (CurrentAnalysisTree.Wordform != null)
				{
					// Need to check whether there are any options to list.
					ShowAnalysisCombo = CurrentAnalysisTree.Wordform.AnalysesOC.Count > 0;
				}
			}
			else
			{
				ShowAnalysisCombo = true; // there's a real analysis!
			}
			m_hvoAnalysisGuess = analysis?.Hvo ?? 0;
			if (WordGlossHvo != 0)
			{
				m_hvoAnalysisGuess = WordGlossHvo;
			}

			// make the wordform corresponding to the baseline ws, match RawWordform
			Caches.DataAccess.SetMultiStringAlt(kSbWord, ktagSbWordForm, RawWordformWs, RawWordform);
			// Set every alternative of the word gloss, whether or not we have one...this
			// ensures clearing it out if we once had something but do no longer.
			CopyStringsToSecondary(InterlinLineChoices.kflidWordGloss, sdaMain, WordGlossHvo, WfiGlossTags.kflidForm, cda, hvoSbWord, ktagSbWordGloss);
			cda.CacheIntProp(hvoSbWord, ktagSbWordGlossGuess, fGuessing);
			cda.CacheObjProp(hvoSbWord, ktagSbWordPos, 0); // default.
			if (analysis != null) // Might still be, if no default is available.
			{
				var category = analysis.CategoryRA;
				if (category != null)
				{
					var hvoWordPos = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidWordPos, category.Hvo, CmPossibilityTags.kflidAbbreviation, hvoSbWord, sdaMain, cda);
					cda.CacheObjProp(hvoSbWord, ktagSbWordPos, hvoWordPos);
					cda.CacheIntProp(hvoWordPos, ktagSbNamedObjGuess, fGuessing);
				}

				if (!ShowMorphBundles)
				{
					return fGuessing != 0;
				}
				var bldrError = new StringBuilder();
				foreach (var mb in analysis.MorphBundlesOS)
				{
					// Create the corresponding SbMorph.
					var hvoMbSec = Caches.DataAccess.MakeNewObject(kclsidSbMorph, hvoSbWord, ktagSbWordMorphs, mb.IndexInOwner);
					Caches.Map(hvoMbSec, mb.Hvo);

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
						hvoMorphForm = Caches.DataAccess.MakeNewObject(kclsidSbNamedObj, mb.Hvo, ktagSbMorphForm, -2); // -2 for atomic
						CopyStringsToSecondary(InterlinLineChoices.kflidMorphemes, sdaMain, mb.Hvo, WfiMorphBundleTags.kflidForm, cda, hvoMorphForm, ktagSbNamedObjName);
						// We will slightly adjust the form we display in the default vernacular WS.
						var specMorphemes = InterlinLineChoices.GetPrimarySpec(InterlinLineChoices.kflidMorphemes);
						int wsForm;
						if (specMorphemes == null || !mb.Form.TryWs(specMorphemes.WritingSystem, out wsForm))
						{
							wsForm = RawWordformWs;
						}
						var tssForm = sdaMain.get_MultiStringAlt(mb.Hvo, WfiMorphBundleTags.kflidForm, wsForm);
						// currently (unfortunately) Text returns 'null' from COM for empty strings.
						var realForm = tssForm.Text ?? string.Empty;

						// if it's not an empty string, then we can find its form type, and separate the
						// morpheme markers into separate properties.
						if (realForm != string.Empty)
						{
							try
							{
								int clsidForm;
								var mmt = MorphServices.FindMorphType(Caches.MainCache, ref realForm, out clsidForm);
								sPrefix = mmt.Prefix;
								sPostfix = mmt.Postfix;
							}
							catch (Exception e)
							{
								bldrError.AppendLine(e.Message);
							}
						}
						tssForm = TsStringUtils.MakeString(realForm, RawWordformWs);
						cda.CacheStringAlt(hvoMorphForm, ktagSbNamedObjName, wsVern, tssForm);
					}
					else
					{
						// Create the secondary object corresponding to the MoForm in the usual way from the form object.
						hvoMorphForm = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidMorphemes, mf.Hvo, MoFormTags.kflidForm, hvoSbWord, sdaMain, cda);
						// Store the prefix and postfix markers from the MoMorphType object.
						var hvoMorphType = sdaMain.get_ObjectProp(mf.Hvo, MoFormTags.kflidMorphType);
						if (hvoMorphType != 0)
						{
							sPrefix = sdaMain.get_UnicodeProp(hvoMorphType, MoMorphTypeTags.kflidPrefix);
							sPostfix = sdaMain.get_UnicodeProp(hvoMorphType, MoMorphTypeTags.kflidPostfix);
						}
					}
					if (!string.IsNullOrEmpty(sPrefix))
					{
						cda.CacheStringProp(hvoMbSec, ktagSbMorphPrefix, TsStringUtils.MakeString(sPrefix, wsVern));
					}
					if (!string.IsNullOrEmpty(sPostfix))
					{
						cda.CacheStringProp(hvoMbSec, ktagSbMorphPostfix, TsStringUtils.MakeString(sPostfix, wsVern));
					}

					// Link the SbMorph to its form object, noting if it is a guess.
					cda.CacheObjProp(hvoMbSec, ktagSbMorphForm, hvoMorphForm);
					cda.CacheIntProp(hvoMorphForm, ktagSbNamedObjGuess, fGuessing);

					// Get the real Sense that supplies the gloss, if any.
					var senseReal = mb.SenseRA;
					if (senseReal == null && fGuessing != 0)
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
						{
							possibleVariant = mf.Owner as ILexEntry;
						}
						if (possibleVariant != null && possibleVariant.IsVariantOfSenseOrOwnerEntry(senseReal, out lerTest))
						{
							hvoLexSenseSec = Caches.FindOrCreateSec(senseReal.Hvo, kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
							CacheLexGlossWithInflTypeForAllCurrentWs(possibleVariant, hvoLexSenseSec, wsVern, cda, mb.InflTypeRA);
						}
						else
						{
							// add normal LexGloss without variant info
							hvoLexSenseSec = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidLexGloss, senseReal.Hvo, LexSenseTags.kflidGloss, hvoSbWord, sdaMain, cda);
						}
						cda.CacheObjProp(hvoMbSec, ktagSbMorphGloss, hvoLexSenseSec);
						cda.CacheIntProp(hvoLexSenseSec, ktagSbNamedObjGuess, fGuessing);

						var hvoInflType = 0;
						if (mb.InflTypeRA != null)
						{
							hvoInflType = Caches.FindOrCreateSec(mb.InflTypeRA.Hvo, kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
						}
						cda.CacheObjProp(hvoMbSec, ktagSbNamedObjInflType, hvoInflType);
					}

					// Get the MSA, if any.
					var msaReal = mb.MsaRA;
					if (msaReal != null)
					{
						var hvoPos = Caches.FindOrCreateSec(msaReal.Hvo, kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);

						foreach (var ws in InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidLexPos, true))
						{
							// Since ws maybe ksFirstAnal/ksFirstVern, we need to get what is actually
							// used in order to retrieve the data in Vc.Display().  See LT_7976.
							// Use InterlinAbbrTss to get an appropriate different name for each ws
							var tssLexPos = msaReal.InterlinAbbrTSS(ws);
							var wsActual = TsStringUtils.GetWsAtOffset(tssLexPos, 0);
							cda.CacheStringAlt(hvoPos, ktagSbNamedObjName, wsActual, tssLexPos);
						}
						cda.CacheObjProp(hvoMbSec, ktagSbMorphPos, hvoPos);
						cda.CacheIntProp(hvoPos, ktagSbNamedObjGuess, fGuessing);
					}

					// If we have a form, we can get its owner and set the info for the Entry
					// line.
					// Enhance JohnT: attempt a guess if we have a form but no entry.
					if (mf == null)
					{
						continue;
					}
					var entryReal = mf.Owner as ILexEntry;
					// We can assume the owner is a LexEntry as that is the only type of object
					// that can own MoForms. We don't actually create the LexEntry, to
					// improve performance. All the relevant data should already have
					// been loaded while creating the main interlinear view.
					LoadSecDataForEntry(entryReal, senseReal, hvoSbWord, cda, wsVern, hvoMbSec, fGuessing, sdaMain);
				}

				if (bldrError.Length <= 0)
				{
					return fGuessing != 0;
				}
				var msg = bldrError.ToString().Trim();
				var wnd = FindForm() ?? PropertyTable.GetValue<IWin32Window>("window");
				MessageBox.Show(wnd, msg, ITextStrings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else
			{
				// No analysis, default or otherwise. We immediately, however, fill in a single
				// dummy morpheme, if showing morphology.
				fGuessing = 0;	// distinguish between a 'guess' (defaults) and courtesy filler info (cf. LT-5858).
				if (!ShowMorphBundles)
				{
					return fGuessing != 0;
				}
				var hvoMbSec = Caches.DataAccess.MakeNewObject(kclsidSbMorph, hvoSbWord, ktagSbWordMorphs, 0);
				var tssForm = Caches.DataAccess.get_MultiStringAlt(hvoSbWord, ktagSbWordForm, RawWordformWs);
				// Possibly adjust case of tssForm.
				if (fAdjustCase && CaseStatus == StringCaseStatus.title && tssForm != null && tssForm.Length > 0)
				{
					tssForm = TsStringUtils.MakeString(cf.ToLower(tssForm.Text), RawWordformWs);
					FormOfWordform = tssForm; // need this to be set in case hvoWordformRef set to zero.
					// If we adjust the case of the form, we must adjust the hvo as well,
					// or any analyses created will go to the wrong WfiWordform.
					CurrentAnalysisTree.Analysis = GetWordform(tssForm);
					if (CurrentAnalysisTree.Wordform != null)
					{
						ShowAnalysisCombo = CurrentAnalysisTree.Wordform.AnalysesOC.Count > 0;
					}
				}
				else
				{
					// just use the wfi wordform form for our dummy morph form.
					tssForm = CurrentAnalysisTree.Wordform.Form.get_String(RawWordformWs);
				}
				var hvoMorphForm = Caches.FindOrCreateSec(0, kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
				cda.CacheStringAlt(hvoMorphForm, ktagSbNamedObjName, wsVern, tssForm);
				cda.CacheObjProp(hvoMbSec, ktagSbMorphForm, hvoMorphForm);
				cda.CacheIntProp(hvoMorphForm, ktagSbNamedObjGuess, fGuessing);
			}
			return fGuessing != 0;
		}

		public static bool GetHasMultipleRelevantAnalyses(IWfiWordform analysis)
		{
			return analysis.HumanApprovedAnalyses.Count() + analysis.HumanNoOpinionParses.Count() > 1;
		}

		internal static bool IsAnalysisHumanApproved(LcmCache cache, IWfiAnalysis analysis)
		{
			if (analysis == null)
			{
				// non-existent analysis can't be approved.
				return false;
			}
			return (analysis.EvaluationsRC.Where(ae => ae.Approves && (ae.Owner as ICmAgent).Human)).FirstOrDefault() != null;
		}

		/// <summary>
		/// Select the indicated icon of the word.
		/// </summary>
		internal void SelectIcon(int tag)
		{
			MoveSelectionIcon(new SelLevInfo[0], tag);
		}

		internal int CreateSecondaryAndCopyStrings(int flidChoices, int hvoMain, int flidMain, int hvoSbWord, ISilDataAccess sdaMain, IVwCacheDa cda)
		{
			var hvoSec = Caches.FindOrCreateSec(hvoMain, kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
			CopyStringsToSecondary(flidChoices, sdaMain, hvoMain, flidMain, cda, hvoSec, ktagSbNamedObjName);
			return hvoSec;
		}

		internal int CreateSecondaryAndCopyStrings(int flidChoices, int hvoMain, int flidMain)
		{
			return CreateSecondaryAndCopyStrings(flidChoices, hvoMain, flidMain, kSbWord, Caches.MainCache.MainCacheAccessor, Caches.DataAccess as IVwCacheDa);
		}

		/// <summary>
		/// Set the (real) LexEntry that is considered current. Broadcast a notification
		/// to delegates if it changed.
		/// </summary>
		public void SetSelectedEntry(ILexEntry entryReal)
		{
			if (entryReal.Hvo == m_hvoLastSelEntry)
			{
				return;
			}
			m_hvoLastSelEntry = entryReal.Hvo;
			SelectionChangedEvent?.Invoke(this, new FwObjectSelectionEventArgs(entryReal.Hvo));
		}


		/// <summary>
		/// Get the string that should be stored in the MorphBundle Form.
		/// This will include any prefix and/or postfix markers.
		/// </summary>
		internal ITsString GetFullMorphForm(int hvoSbMorph)
		{
			var sda = Caches.DataAccess;
			var hvoSecMorph = sda.get_ObjectProp(hvoSbMorph, ktagSbMorphForm);
			var tss = sda.get_MultiStringAlt(hvoSecMorph, ktagSbNamedObjName, RawWordformWs);
			// Add any prefix or postfix info to the form
			var tsb = tss.GetBldr();
			tsb.ReplaceTsString(0, 0, sda.get_StringProp(hvoSbMorph, ktagSbMorphPrefix));
			tsb.ReplaceTsString(tsb.Length, tsb.Length, sda.get_StringProp(hvoSbMorph, ktagSbMorphPostfix));
			return tsb.GetString();
		}

		private void CopyStringsToSecondary(IList<int> writingSystems, ISilDataAccess sdaMain, int hvoMain, int flidMain, IVwCacheDa cda, int hvoSec, int flidSec)
		{
			foreach (var ws in writingSystems)
			{
				var wsActual = 0;
				if (ws > 0)
				{
					wsActual = ws;
				}
				else
				{
					switch (ws)
					{
						case WritingSystemServices.kwsFirstAnal:
							IList<int> currentAnalysisWsList = Caches.MainCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle).ToArray();
							CacheStringAltForAllCurrentWs(currentAnalysisWsList, cda, hvoSec, flidSec, sdaMain, hvoMain, flidMain);
							continue;
						case WritingSystemServices.kwsFirstVern:
							IList<int> currentVernWsList = Caches.MainCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Select(wsObj => wsObj.Handle).ToArray();
							CacheStringAltForAllCurrentWs(currentVernWsList, cda, hvoSec, flidSec, sdaMain, hvoMain, flidMain);
							continue;
						case WritingSystemServices.kwsVernInParagraph:
							wsActual = RawWordformWs;
							break;
					}
				}

				if (wsActual <= 0)
				{
					throw new ArgumentException($"magic ws {ws} not yet supported.");
				}

				var tss = hvoMain == 0 ? TsStringUtils.EmptyString(wsActual) : sdaMain.get_MultiStringAlt(hvoMain, flidMain, wsActual);
				cda.CacheStringAlt(hvoSec, flidSec, wsActual, tss);
			}
		}

		/// <summary>
		/// Copy to the secondary cache NamedObect hvoSec all alternatives of property flid of
		/// real object hvoMain.  The user may change the actual writing system assigned to a
		/// given line at any time, so we need all the possibilities available in the local
		/// cache.
		/// </summary>
		internal void CopyStringsToSecondary(int flidChoices, ISilDataAccess sdaMain, int hvoMain, int flidMain, IVwCacheDa cda, int hvoSec, int flidSec)
		{
			var writingSystems = Caches.MainCache.ServiceLocator.WritingSystems.AllWritingSystems.Select(ws => ws.Handle).ToList();
			CopyStringsToSecondary(writingSystems, sdaMain, hvoMain, flidMain, cda, hvoSec, flidSec);
		}

		private static void CacheStringAltForAllCurrentWs(IEnumerable<int> currentWsList, IVwCacheDa cda, int hvoSec, int flidSec, ISilDataAccess sdaMain, int hvoMain, int flidMain)
		{
			CacheStringAltForAllCurrentWs(currentWsList, cda, hvoSec, flidSec,
				ws1 => hvoMain != 0 ? sdaMain.get_MultiStringAlt(hvoMain, flidMain, ws1) : TsStringUtils.MakeString(string.Empty, ws1));
		}

		private static void CacheStringAltForAllCurrentWs(IEnumerable<int> currentWsList, IVwCacheDa cda, int hvoSec, int flidSec, Func<int, ITsString> createStringAlt)
		{
			foreach (var ws1 in currentWsList)
			{
				ITsString tssMain = null;
				if (createStringAlt != null)
				{
					tssMain = createStringAlt(ws1);
				}

				if (tssMain == null)
				{
					tssMain = TsStringUtils.MakeString("", ws1);
				}
				cda.CacheStringAlt(hvoSec, flidSec, ws1, tssMain);
			}
		}

		/// <summary>
		/// Obtain the HVO of the most desirable default annotation to use for a particular
		/// wordform.
		/// </summary>
		private void GetDefaults(IWfiWordform wordform, ref IWfiAnalysis analysis, out IWfiGloss gloss)
		{
			gloss = null;
			if (wordform == null || !wordform.IsValidObject)
			{
				return;
			}

			if (InterlinDoc == null) // In Wordform Analyses tool and some unit tests, InterlinDoc is null
			{
				return;
			}
			var sda = InterlinDoc.RootBox.DataAccess;

			// If we're calling from the context of SetWordform(), we may be trying to establish
			// an alternative wordform/form/analysis. In that case, or if we don't have a default cached,
			// try to get one. Otherwise, if we've already cached a default, use it...it's surprising for the
			// user if we move the focus box to something and the default changes. (LT-4643 etc.)
			var hvoDefault = 0;
			if (analysis != null)
			{
				hvoDefault = analysis.Hvo;
			}
			else if (m_occurrenceSelected != null && m_occurrenceSelected.Analysis == wordform)
			{
				// Try to establish a default based on the current occurrence.
				if (m_fSetWordformInProgress || !sda.get_IsPropInCache(HvoAnnotation, InterlinViewDataCache.AnalysisMostApprovedFlid, (int) CellarPropertyType.ReferenceAtomic, 0))
				{
					InterlinDoc.RecordGuessIfNotKnown(m_occurrenceSelected);
				}
				hvoDefault = sda.get_ObjectProp(HvoAnnotation, InterlinViewDataCache.AnalysisMostApprovedFlid);
				// In certain cases like during an undo the Decorator data might be stale, so validate the result before we continue
				// to prevent using data that does not exist anymore
				if (!Cache.ServiceLocator.IsValidObjectId(hvoDefault))
				{
					hvoDefault = 0;
				}
			}
			else
			{
				// Try to establish a default based on the wordform itself.
				var ws = wordform.Cache.DefaultVernWs;
				if (m_occurrenceSelected != null)
				{
					ws = m_occurrenceSelected.BaselineWs;
				}
				var analysisDefault = InterlinDoc.GetGuessForWordform(wordform, ws);
				if (analysisDefault != null)
				{
					hvoDefault = analysisDefault.Hvo;
				}
			}

			if (hvoDefault == 0)
			{
				return;
			}
			var obj = Caches.MainCache.ServiceLocator.GetObject(hvoDefault);
			switch (obj.ClassID)
			{
				case WfiAnalysisTags.kClassId:
					analysis = (IWfiAnalysis) obj;
					gloss = analysis.MeaningsOC.FirstOrDefault();
					break;
				case WfiGlossTags.kClassId:
					gloss = (IWfiGloss) obj;
					analysis = obj.OwnerOfClass<IWfiAnalysis>();
					break;
			}
		}

		/// <summary>
		/// Make a selection at the end of the specified word gloss line.
		/// </summary>
		/// <param name="lineIndex">-1 if you want to select the first WordGlossLine.</param>
		/// <returns>true, if selection was successful.</returns>
		internal bool SelectAtEndOfWordGloss(int lineIndex)
		{
			// get first line index if it isn't specified.
			if (lineIndex < 0)
			{
				lineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordGloss);
				if (lineIndex < 0)
				{
					return false;
				}
			}

			int glossLength;
			int cpropPrevious;
			GetWordGlossInfo(lineIndex, out glossLength, out cpropPrevious);
			// select at the end
			return MoveSelection(new SelLevInfo[0], ktagSbWordGloss, cpropPrevious, glossLength, glossLength);
		}

		private void GetWordGlossInfo(int lineIndex, out int glossLength, out int cpropPrevious)
		{
			var ws = InterlinLineChoices[lineIndex].WritingSystem;
			glossLength = 0;
			// InterlinLineChoices.kflidWordGloss, ktagSbWordGloss
			if (WordGlossHvo != 0)
			{
				var tss = Caches.MainCache.MainCacheAccessor.get_MultiStringAlt(WordGlossHvo, WfiGlossTags.kflidForm, ws);
				glossLength = tss.Length;
			}
			else
			{
				glossLength = 0;
			}

			cpropPrevious = InterlinLineChoices.PreviousOccurrences(lineIndex);
		}

		/// <summary>
		/// Make a selection at the start of the indicated morpheme in the morphs line.
		/// That is, at the start of the prefix if there is one, otherwise, the start of the form.
		/// </summary>
		internal void SelectEntryIconOfMorph(int index)
		{
			SelectIconOfMorph(index, ktagMorphEntryIcon);
		}

		internal void LoadSecDataForEntry(ILexEntry entryReal, ILexSense senseReal, int hvoSbWord, IVwCacheDa cda, int wsVern, int hvoMbSec, int fGuessing, ISilDataAccess sdaMain)
		{
			var hvoEntry = Caches.FindOrCreateSec(entryReal.Hvo, kclsidSbNamedObj, hvoSbWord, ktagSbWordDummy);
			// try to determine if the given entry is a variant of the sense we passed in (ie. not an owner)
			ILexEntryRef ler = null;
			var hvoEntryToDisplay = entryReal.Hvo;
			if (senseReal != null)
			{
				if (entryReal.IsVariantOfSenseOrOwnerEntry(senseReal, out ler))
				{
					hvoEntryToDisplay = senseReal.EntryID;
				}
			}

			var tssLexEntry = LexEntryVc.GetLexEntryTss(Cache, hvoEntryToDisplay, wsVern, ler);
			cda.CacheStringAlt(hvoEntry, ktagSbNamedObjName, wsVern, tssLexEntry);
			cda.CacheObjProp(hvoMbSec, ktagSbMorphEntry, hvoEntry);
			cda.CacheIntProp(hvoEntry, ktagSbNamedObjGuess, fGuessing);
			var writingSystems = InterlinLineChoices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidLexEntries, 0);
			if (!writingSystems.Any())
			{
				return;
			}
			// Sigh. We're trying for some reason to display other alternatives of the entry.
			var hvoLf = sdaMain.get_ObjectProp(hvoEntryToDisplay, LexEntryTags.kflidLexemeForm);
			if (hvoLf != 0)
			{
				CopyStringsToSecondary(writingSystems, sdaMain, hvoLf, MoFormTags.kflidForm, cda, hvoEntry, ktagSbNamedObjName);
			}
			else
			{
				CopyStringsToSecondary(writingSystems, sdaMain, hvoEntryToDisplay, LexEntryTags.kflidCitationForm, cda, hvoEntry, ktagSbNamedObjName);
			}
		}

		private void CacheLexGlossWithInflTypeForAllCurrentWs(ILexEntry possibleVariant, int hvoLexSenseSec, int wsVern, IVwCacheDa cda, ILexEntryInflType inflType)
		{
			IList<int> currentAnalysisWsList = Caches.MainCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle).ToArray();
			CacheStringAltForAllCurrentWs(currentAnalysisWsList, cda, hvoLexSenseSec, ktagSbNamedObjName,
				delegate(int wsLexGloss)
					{
						var hvoSenseReal = Caches.RealHvo(hvoLexSenseSec);
						var sense = Cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSenseReal);
						var spec = InterlinLineChoices.CreateSpec(InterlinLineChoices.kflidLexGloss, wsLexGloss);
						var choices = new InterlinLineChoices(Cache, InterlinLineChoices.m_wsDefVern,
															InterlinLineChoices.m_wsDefAnal);
						choices.Add(spec);
						ITsString tssResult;
						return InterlinVc.TryGetLexGlossWithInflTypeTss(possibleVariant, sense, spec, choices, wsVern, inflType, out tssResult) ? tssResult : null;
					});
		}

		/// <summary>
		/// return the wordform that corresponds to the given form, or zero if none.
		/// </summary>
		private IWfiWordform GetWordform(ITsString form)
		{
			IWfiWordform wordform;
			return Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(form, out wordform) ? wordform : null;
		}

		/// <summary>
		/// Make and install the selection indicated by the array and a following atomic property
		/// that contains an icon.
		/// </summary>
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
		/// Make and install the selection indicated by the array of objects on the first (and only root),
		/// an IP at the start of the property.
		/// </summary>
		/// <returns>true, if selection was successful.</returns>
		private bool MoveSelection(SelLevInfo[] rgvsli, int tag, int cpropPrevious)
		{
			return MoveSelection(rgvsli, tag, cpropPrevious, 0, 0);
		}
		/// <summary>
		/// Make and install the selection indicated by the array of objects on the first (and only root),
		/// an IP at the start of the property.
		/// </summary>
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
		internal void SelectIconOfMorph(int index, int tag)
		{
			var selectIndexMorph = new SelLevInfo[1];
			selectIndexMorph[0].tag = ktagSbWordMorphs;
			selectIndexMorph[0].ihvo = index;
			MoveSelectionIcon(selectIndexMorph, tag);
		}

		/// <summary>
		/// Given that we have set the form of hvoMorph (in the sandbox cache) to the given
		/// form, figure defaults for the LexEntry, LexGloss, and LexPos lines as far as
		/// possible. It is assumed that all three following lines are empty to begin with.
		/// Also set matching text for any other WSs that are displayed for MoForm.
		/// (This is important because if the user confirms the analysis, we will write that
		/// information back to the MoForm. If we leave the lines blank that process could
		/// remove the other alternatives.)
		/// </summary>
		internal void EstablishDefaultEntry(int hvoMorph, string form, IMoMorphType mmt, bool fMonoMorphemic)
		{
			var hvoFormSec = Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphForm);
			// remove any existing mapping for this morph form, which might exist
			// from a previous analysis
			Caches.RemoveSec(hvoFormSec);
			var defFormReal = DefaultMorph(form, mmt);
			if (defFormReal == null)
			{
				return; // this form never occurs anywhere, can't supply any default.
			}
			var otherWritingSystemsForMorphForm = InterlinLineChoices.OtherWritingSystemsForFlid(InterlinLineChoices.kflidMorphemes, RawWordformWs);
			if (otherWritingSystemsForMorphForm.Any())
			{
				var hvoSbForm = Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphForm);
				foreach (var ws in otherWritingSystemsForMorphForm)
				{
					try
					{
						Caches.DataAccess.SetMultiStringAlt(hvoSbForm, ktagSbNamedObjName, ws, WritingSystemServices.GetMagicStringAlt(Cache, ws, defFormReal.Hvo, MoFormTags.kflidForm));
					}
					catch (Exception e)
					{
						if (e is ArgumentException && e.Message.StartsWith("Magic writing system invalid in string"))
						{
							// probably using TryAWord and the ws is WritingSystemServices.kwsFirstVern
							// is OK so continue (yes, this is a hack)
							continue;
						}
						throw;
					}
				}
			}
			var le = defFormReal.Owner as ILexEntry;
			var hvoEntry = Caches.FindOrCreateSec(le.Hvo, kclsidSbNamedObj, kSbWord, ktagSbWordDummy);
			var wsVern = RawWordformWs;
			var hvoEntryToDisplay = le.Hvo;
			var ler = DomainObjectServices.GetVariantRef(le, fMonoMorphemic);
			if (ler != null)
			{
				var coRef = ler.ComponentLexemesRS[0];
				hvoEntryToDisplay = (coRef as ILexSense)?.EntryID ?? coRef.Hvo;
			}
			var tssName = LexEntryVc.GetLexEntryTss(Cache, hvoEntryToDisplay, wsVern, ler);
			Caches.DataAccess.SetMultiStringAlt(hvoEntry, ktagSbNamedObjName, RawWordformWs, tssName);
			Caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphEntry, hvoEntry);
			Caches.DataAccess.SetInt(hvoEntry, ktagSbNamedObjGuess, 1);
			Caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, hvoMorph, ktagSbMorphGloss, 0, 1, 0);
			// Establish the link between the SbNamedObj that represents the MoForm, and the
			// selected MoForm.  (This is used when building the real WfiAnalysis.)
			Caches.Map(hvoFormSec, defFormReal.Hvo);
			// This takes too long! Wait at least for a click in the bundle.
			//SetSelectedEntry(hvoEntryReal);
			EstablishDefaultSense(hvoMorph, le, null, null);
		}

		/// <summary>
		/// Find the MoForm, from among those whose form (in some ws) is the given one
		/// starting from the most frequently referenced by the MorphBundles property of a WfiAnalysis.
		/// If there is no wordform analysis, then fall back to selecting any matching MoForm.
		/// </summary>
		internal IMoForm DefaultMorph(string form, IMoMorphType mmt)
		{
			// Find all the matching morphs and count how often used in WfiAnalyses
			var ws = RawWordformWs;
			// Fix FWR-2098 GJM: The definition of 'IsAmbiguousWith' seems not to include 'IsSameAs'.
			var morphs = (Cache.ServiceLocator.GetInstance<IMoFormRepository>().AllInstances().Where(mf => mf.Form.get_String(ws).Text == form && mf.MorphTypeRA != null && (mf.MorphTypeRA == mmt || mf.MorphTypeRA.IsAmbiguousWith(mmt)))).ToList();
			if (morphs.Count == 1)
			{
				return morphs.First(); // special case: we can avoid the cost of figuring ReferringObjects.
			}
			IMoForm bestMorph = null;
			var bestMorphCount = -1;
			foreach (var mf in morphs)
			{
				var count = (mf.ReferringObjects.Where(source => source is IWfiMorphBundle)).Count();
				if (count <= bestMorphCount)
				{
					continue;
				}
				bestMorphCount = count;
				bestMorph = mf;
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
		internal ILexSense EstablishDefaultSense(int hvoMorph, ILexEntry entryReal, ILexSense senseReal, ILexEntryInflType inflType)
		{
			ILexSense variantSense = null;
			// If the entry has no sense we can't do anything.
			if (entryReal.SensesOS.Count == 0)
			{
				variantSense = senseReal ?? GetSenseForVariantIfPossible(entryReal);
				if (variantSense == null)
				{
					return null; // nothing useful we can do.
				}
			}
			// If we already have a gloss for this entry, don't overwrite it with a default.
			var hvoMorphGloss = Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
			if (hvoMorphGloss != 0 && entryReal.Hvo == m_hvoLastSelEntry && senseReal == null)
			{
				return null;
			}

			var defSenseReal = variantSense ?? (senseReal ?? entryReal.SensesOS[0]);
			int hvoDefSense;
			if (variantSense != null && defSenseReal == variantSense)
			{
				hvoDefSense = Caches.FindOrCreateSec(defSenseReal.Hvo, kclsidSbNamedObj, kSbWord, ktagSbWordDummy);
				var cda = (IVwCacheDa)Caches.DataAccess;
				var wsVern = RawWordformWs;
				CacheLexGlossWithInflTypeForAllCurrentWs(entryReal, hvoDefSense, wsVern, cda, inflType);
				var hvoInflType = 0;
				if (inflType != null)
				{
					hvoInflType = Caches.FindOrCreateSec(inflType.Hvo, kclsidSbNamedObj, kSbWord, ktagSbWordDummy);
				}
				cda.CacheObjProp(hvoMorph, ktagSbNamedObjInflType, hvoInflType);
				Caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, hvoMorph, ktagSbNamedObjInflType, 0, 1, 0);
			}
			else
			{
				// add normal LexGloss without variant info
				hvoDefSense = CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidLexGloss, defSenseReal.Hvo, LexSenseTags.kflidGloss);
			}

			// We're guessing the gloss if we just took the first sense, but if the user chose
			// one it is definite.
			Caches.DataAccess.SetInt(hvoDefSense, ktagSbNamedObjGuess, senseReal == null ? 1 : 0);

			Caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphGloss, hvoDefSense);
			Caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, hvoMorph, ktagSbMorphGloss, 0, 1, 0);

			// Now if the sense has an MSA, set that up as a default too.
			var defMsaReal = defSenseReal.MorphoSyntaxAnalysisRA;
			var cOldMsa = 0;
			if (Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphPos) != 0)
			{
				cOldMsa = 1;
			}
			if (defMsaReal != null)
			{
				var hvoNewPos = Caches.FindOrCreateSec(defMsaReal.Hvo, kclsidSbNamedObj, kSbWord, ktagSbWordDummy);
				foreach (var ws in InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidLexPos, true))
				{
					// Since ws maybe ksFirstAnal/ksFirstVern, we need to get what is actually
					// used in order to retrieve the data in Vc.Display().  See LT_7976.
					var tssNew = defMsaReal.InterlinAbbrTSS(ws);
					var wsActual = TsStringUtils.GetWsAtOffset(tssNew, 0);
					Caches.DataAccess.SetMultiStringAlt(hvoNewPos, ktagSbNamedObjName, wsActual, tssNew);
				}
				Caches.DataAccess.SetInt(hvoNewPos, ktagSbNamedObjGuess, senseReal == null ? 1 : 0);
				Caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphPos, hvoNewPos);
				Caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, hvoMorph, ktagSbMorphPos, 0, 1, cOldMsa);
			}
			else
			{
				// Going to null MSA, we still need to record the value and propagate the
				// change!  See LT-4246.
				Caches.DataAccess.SetObjProp(hvoMorph, ktagSbMorphPos, 0);
				Caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, hvoMorph, ktagSbMorphPos, 0, 0, cOldMsa);
			}
			return defSenseReal;
		}

		/// <summary>
		/// If hvoEntry is the id of a variant, try to find an entry it's a variant of that
		/// has a sense.  Return the corresponding ILexEntryRef for the first such entry.
		/// If this is being called to establish a default monomorphemic guess, skip over
		/// any bound root or bound stem entries that hvoEntry may be a variant of.
		/// </summary>
		public ILexEntryRef GetVariantRef(LcmCache cache, int hvoEntry, bool fMonoMorphemic)
		{
			var sda = cache.MainCacheAccessor;
			var cRef = sda.get_VecSize(hvoEntry, LexEntryTags.kflidEntryRefs);
			for (var i = 0; i < cRef; ++i)
			{
				var hvoRef = sda.get_VecItem(hvoEntry, LexEntryTags.kflidEntryRefs, i);
				var refType = sda.get_IntProp(hvoRef, LexEntryRefTags.kflidRefType);
				if (refType != LexEntryRefTags.krtVariant)
				{
					continue;
				}
				var cEntries = sda.get_VecSize(hvoRef, LexEntryRefTags.kflidComponentLexemes);
				if (cEntries != 1)
				{
					continue;
				}
				var hvoComponent = sda.get_VecItem(hvoRef, LexEntryRefTags.kflidComponentLexemes, 0);
				var clid = Caches.MainCache.ServiceLocator.ObjectRepository.GetObject(hvoComponent).ClassID;
				if (fMonoMorphemic && IsEntryBound(cache, hvoComponent, clid))
				{
					continue;
				}
				if (clid == LexSenseTags.kClassId || sda.get_VecSize(hvoComponent, LexEntryTags.kflidSenses) > 0)
				{
					return Caches.MainCache.ServiceLocator.GetInstance<ILexEntryRefRepository>().GetObject(hvoRef);
				}
				else
				{
					// Should we check for a variant of a variant of a ...?
				}
			}
			return null; // nothing useful we can do.
		}

		private static ILexSense GetSenseForVariantIfPossible(ILexEntry entryReal)
		{
			var ler = DomainObjectServices.GetVariantRef(entryReal, false);
			if (ler == null)
			{
				return null;
			}

			if (ler.ComponentLexemesRS[0] is ILexEntry)
			{
				return ((ILexEntry)ler.ComponentLexemesRS[0]).SensesOS[0];
			}
			return (ILexSense)ler.ComponentLexemesRS[0];	// must be a sense!
		}

		/// <summary>
		/// Check whether the given entry (or entry owning the given sense) is either a bound
		/// root or a bound stem.  We don't want to use those as guesses for monomorphemic
		/// words.  See LT-10323.
		/// </summary>
		private static bool IsEntryBound(LcmCache cache, int hvoComponent, int clid)
		{
			int hvoTargetEntry;
			if (clid == LexSenseTags.kClassId)
			{
				var ls = cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoComponent);
				hvoTargetEntry = ls.Entry.Hvo;
				if (!(ls.MorphoSyntaxAnalysisRA is IMoStemMsa))
				{
					return true;		// must be an affix, so it's bound by definition.
				}
			}
			else
			{
				hvoTargetEntry = hvoComponent;
			}
			var hvoMorph = cache.MainCacheAccessor.get_ObjectProp(hvoTargetEntry, LexEntryTags.kflidLexemeForm);
			if (hvoMorph == 0)
			{
				return false;
			}
			var hvoMorphType = cache.MainCacheAccessor.get_ObjectProp(hvoMorph, MoFormTags.kflidMorphType);
			if (hvoMorphType == 0)
			{
				return false;
			}

			if (MorphServices.IsAffixType(cache, hvoMorphType))
			{
				return true;
			}
			var guid = cache.ServiceLocator.ObjectRepository.GetObject(hvoMorphType).Guid;
			return guid == MoMorphTypeTags.kguidMorphBoundRoot || guid == MoMorphTypeTags.kguidMorphBoundStem;
		}

		/// <summary>
		/// If hvoEntryReal refers to a variant, try for the first sense of the entry it's
		/// a variant of.  Otherwise, give up and return 0.
		/// </summary>
		private int GetSenseForVariantIfPossible(int hvoEntryReal)
		{
			var ler = GetVariantRef(Caches.MainCache, hvoEntryReal, false);
			if (ler == null)
			{
				return 0;
			}
			return (ler.ComponentLexemesRS[0] as ILexEntry)?.SensesOS[0].Hvo ?? ler.ComponentLexemesRS[0].Hvo;
		}

		// Handles a change in the item selected in the combo box.
		internal void HandleComboSelChange(object sender, EventArgs ea)
		{
			if (m_fMakingCombo)
			{
				return; // some spurious notification while initializing the combo.
			}
			// Anything we do removes the combo box...it is put back only if we make a new
			// selection that requires one.
			HideCombos();
			m_ComboHandler.HandleSelectIfActive();

		}

		// Hide either kind of combo, if it is present and visible. Also the combo list, if any.
		private void HideCombos()
		{
			m_ComboHandler?.Hide();
			FirstLineHandler?.Hide();
		}

		/// <summary>
		/// Make a combo box appropriate for the specified selection. If fMouseDown is true,
		/// do so unconditionally...otherwise (mousemove) only if the new selection is on a different thing.
		/// </summary>
		private void ShowComboForSelection(IVwSelection vwselNew, bool fMouseDown)
		{
			// It's a good idea to get this first...it's possible for MakeCombo to leave the selection invalid.
			Rect loc;
			vwselNew.GetParaLocation(out loc);
			if (!fMouseDown)
			{
				// It's a mouse move.
				// If we've moved to somewhere outside any paragraph get rid of the combos.
				// But, allow somewhere close, since otherwise it's almost impossible to get
				// a combo on an empty string.
				var locExpanded = loc;
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
			{
				return;
			}

			m_locLastShowCombo = loc;
			m_fMakingCombo = true;
			HideCombos();
			// No matter what, we are fixin to get rid of the old value.
			DisposeComboHandler();
			m_ComboHandler = !m_fInMouseDrag ? InterlinComboHandler.MakeCombo(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), vwselNew, this, fMouseDown) : null;
			m_fMakingCombo = false;
			m_fLockCombo = false; // nothing typed in it yet.
			if (m_ComboHandler == null)
			{
				return;
			}
			// Set the position of the combo and display it. Do this before synchronizing
			// the LexEntry display, which can take a while.
			m_ComboHandler.Activate(loc);
			m_fMouseDownActivatedCombo = true;
			// If the selection moved to a different morpheme, and we know a corresponding
			// LexEntry, switch to it.
			if (m_ComboHandler.SelectedMorphHvo == 0)
			{
				return;
			}
			var hvoSbEntry = Caches.DataAccess.get_ObjectProp(m_ComboHandler.SelectedMorphHvo, ktagSbMorphEntry);
			if (hvoSbEntry != 0)
			{
				//SetSelectedEntry(m_caches.RealHvo(hvoSbEntry)); // seems to be buggy.
			}
		}

		public void OnOpenCombo()
		{
			var selOrig = RootBox.Selection;
			if (selOrig == null)
			{
				MakeDefaultSelection();
				selOrig = RootBox.Selection;
				if (selOrig == null)
				{
					return;
				}
			}
			var selArrow = ScanForIcon(selOrig);
			if (selArrow == null)
			{
				return;
			}
			// Ensure the sandbox is visible so the menu displays in a meaningful
			// position.  See LT-7671.
			InterlinDoc?.ReallyScrollControlIntoView(this);
			// Simulate a mouse down on the arrow.
			ShowComboForSelection(selArrow, true);
		}

		/// <summary>
		/// given a (text) selection, scan the beginning of its paragraph box and then immediately before its
		/// paragraph box in search of the icon.
		/// </summary>
		private IVwSelection ScanForIcon(IVwSelection selOrig)
		{
			IVwSelection selArrow;
			const int dxPixelIncrement = 3;
			const uint iconParagraphWidth = 10;
			Rect rect;
			selOrig.GetParaLocation(out rect);
			if (m_vc.RightToLeft)
			{
				// Right to Left:
				selArrow = FindNearestSelectionType(selOrig, VwSelType.kstPicture, (uint)rect.right - iconParagraphWidth, iconParagraphWidth * 2, dxPixelIncrement) ??
				           FindNearestSelectionType(selOrig, VwSelType.kstPicture, (uint)RootBox.Width - iconParagraphWidth, iconParagraphWidth, dxPixelIncrement);
			}
			else
			{
				// Left to Right
				selArrow = FindNearestSelectionType(selOrig, VwSelType.kstPicture, (uint)rect.left + iconParagraphWidth, iconParagraphWidth * 2, -dxPixelIncrement) ??
				           FindNearestSelectionType(selOrig, VwSelType.kstPicture, 0, iconParagraphWidth, dxPixelIncrement);
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
		/// <param name="dxPixelIncrement">number and direction of pixels to probe for selType. a positive value
		/// will scan right, and a negative value will scan left. must be nonzero.</param>
		/// <returns></returns>
		private IVwSelection FindNearestSelectionType(IVwSelection selOrig, VwSelType selType, uint xMin, uint xMaxCountOfPixels, int dxPixelIncrement)
		{
			if (dxPixelIncrement == 0)
			{
				throw new ArgumentException($"dxPixelIncrement({dxPixelIncrement}) must be nonzero");
			}
			IVwSelection sel = null;
			Rect rect;
			selOrig.GetParaLocation(out rect);
			var y = rect.top + (rect.bottom - rect.top) / 2;
			var pt = new Point((int)xMin, y);
			uint xLim;
			if (dxPixelIncrement > 0)
			{
				// set our bounds for searching forward.
				xLim = xMin + xMaxCountOfPixels;
				// truncate if necessary.
				if (xLim > RootBox.Width)
				{
					xLim = (uint)RootBox.Width;
				}
			}
			else
			{
				// set our bounds for searching backward.
				// truncate if necessary.
				xLim = xMin > xMaxCountOfPixels ? xMin - xMaxCountOfPixels : 0;
			}
			while (dxPixelIncrement < 0 && pt.X > xLim || dxPixelIncrement > 0 && pt.X < xLim)
			{
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
					if (sel != null && sel.SelType == selType)
					{
						break;
					}
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
			var tsi = new TextSelInfo(selOrig);
			if (!tsi.IsPicture)
			{
				return tsi.Selection;
			}
			Debug.Assert(tsi.IsPicture);
			IVwSelection selStartOfText;
			const int dxPixelIncrement = 1;
			Rect rect;
			selOrig.GetParaLocation(out rect);
			var widthIconPara = (rect.right - rect.left);
			var xMaxCountOfPixels = (widthIconPara) * 2;
			selStartOfText = m_vc.RightToLeft ?
				FindNearestSelectionType(selOrig, VwSelType.kstText, (uint)(rect.left - dxPixelIncrement), (uint)xMaxCountOfPixels, -dxPixelIncrement)
				: FindNearestSelectionType(selOrig, VwSelType.kstText, (uint)(rect.right + dxPixelIncrement), (uint)xMaxCountOfPixels, dxPixelIncrement);
			// install at beginning of text selection, if there is a selection.
			selStartOfText?.Install();
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
		/// </summary>
		/// <param name="fShift">If true, reverse sequence.</param>
		internal void HandleTab(bool fShift)
		{
			int startLineIndex;
			int currentLineIndex;
			int increment;
			bool fSkipIcon;
			int iNextMorphIndex;
			GetLineOfCurrentSelectionAndNextTabStop(fShift, out currentLineIndex, out startLineIndex, out increment, out fSkipIcon, out iNextMorphIndex);
			if (currentLineIndex < 0)
			{
				return;
			}

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
			startLineIndex = -1;
			currentLineIndex = -1;
			increment = fShift ? -1 : 1;
			fSkipIcon = false;
			iNextMorphIndex = -1;

			// Out variables for AllTextSelInfo.
			int tagTextProp;
			int ws;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli;
			bool fIsPictureSel; // icon selected.

			try
			{
				var sel = RootBox.Selection;
				if (sel == null)
				{
					// Select first icon
					MoveAnalysisIconOrNext();
					return;
				}
				fIsPictureSel = sel.SelType == VwSelType.kstPicture;
				var cvsli = sel.CLevels(false) - 1;
				// more out variables for AllTextSelInfo.
				int ihvoRoot;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli, out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
			}
			catch
			{
				// If anything goes wrong just give up.
				return;
			}

			// Find our next morpheme index if our current selection is in a morpheme field
			bool fOnNextLine;
			var cMorph = CheckMorphs();
			iNextMorphIndex = NextMorphIndex(increment, out fOnNextLine);
			// Handle special cases where our current selection is not in a morpheme index.
			if (iNextMorphIndex < 0 && increment < 0)
			{
				iNextMorphIndex = cMorph - 1;
			}

			switch (tagTextProp)
			{
				case ktagAnalysisIcon: // Wordform icon
					currentLineIndex = 0;
					break;
				case ktagSbWordForm: // Wordform
					currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWord, ws);
					// try selecting the icon.
					if (increment < 0 && InterlinLineChoices.IsFirstOccurrenceOfFlid(currentLineIndex))
					{
						startLineIndex = currentLineIndex;
					}
					break;
				case ktagMissingMorphs: // line 2, morpheme forms, no guess.
					// It's not supposed to be possible to get a selection into this line.
					// (Maybe the end-point, but not the anchor.)
					Debug.Assert(false);
					break;
				case ktagMorphFormIcon:
					currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidMorphemes);
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
					currentLineIndex = InterlinLineChoices.FirstLexEntryIndex;
					if (!fOnNextLine)
					{
						startLineIndex = currentLineIndex; 	// try on the same line.
					}
					break;
				case ktagSbWordGloss:
					currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordGloss, ws);
					if (increment < 0 && m_vc.ShowWordGlossIcon && InterlinLineChoices.PreviousOccurrences(currentLineIndex) == 0)
					{
						startLineIndex = currentLineIndex;
					}
					break;
				case ktagWordGlossIcon: // line 6, word gloss.
					currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordGloss);
					if (increment > 0)
					{
						fSkipIcon = true;
						startLineIndex = currentLineIndex;
					}
					break;
				case ktagMissingWordPos: // line 7, word POS, missing
					currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordPos);
					if (increment < 0)
					{
						startLineIndex = currentLineIndex;
					}
					break;
				case ktagWordPosIcon:
					currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordPos);
					break;
				case ktagSbMorphPrefix:
				case ktagSbMorphPostfix:
					currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidMorphemes);
					if (!fOnNextLine)
					{
						startLineIndex = currentLineIndex;
					}
					break;
				case ktagSbNamedObjName:
					{
						// This could be any of several non-missing objects.
						// Need to further subdivide.
						var tagObjProp = rgvsli[0].tag;
						switch (tagObjProp)
						{
							case ktagSbMorphForm:
								currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidMorphemes, ws);
								if (fOnNextLine)
								{
									if (increment < 0 && InterlinLineChoices.IsFirstOccurrenceOfFlid(currentLineIndex))
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
								currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordPos);
								// try selecting the icon.
								if (increment < 0 && !fIsPictureSel)
								{
									startLineIndex = currentLineIndex;
								}
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
			{
				startLineIndex = currentLineIndex + increment;
			}

			// Skip icon for text field, if going back a line
			if (increment >= 0 || startLineIndex == currentLineIndex)
			{
				return;
			}
			// only skip icon for editable fields.
			var nextLine = startLineIndex;
			if (startLineIndex < 0)
			{
				nextLine = InterlinLineChoices.Count - 1;
			}
			if (InterlinLineChoices[nextLine].Flid == InterlinLineChoices.kflidWordGloss || InterlinLineChoices[nextLine].Flid == InterlinLineChoices.kflidMorphemes)
			{
				fSkipIcon = true;
			}
		}

		/// <summary>
		/// Handle the End key (Ctrl-End if fControl is true).
		/// Return true to suppress normal End-Key processing
		/// </summary>
		private bool HandleEndKey(bool fControl)
		{
			if (fControl)
			{
				return false; // Ctrl+End is now processed as a shortcut
			}
			var sel = RootBox.Selection;
			if (sel == null)
			{
				return false;
			}
			if (sel.SelType == VwSelType.kstText)
			{
				// For a text selection, unless it's an IP at the end of the string, handle it normally.
				ITsString tss;
				bool fAssocPrev;
				int ichAnchor, ichEnd, hvoObjA, tagA, hvoObjE, tagE, wsE, wsA;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjE, out tagE, out wsE);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObjA, out tagA, out wsA);
				if (hvoObjE != hvoObjA || tagE != tagA || wsE != wsA || ichEnd != ichAnchor || ichEnd != tss.Length)
				{
					return false;
				}
			}
			// Move to the last property, which is the icon of the word POS
			SelectIcon(ktagWordPosIcon);
			return true;
		}

		/// <summary>
		/// Handle the Home key (Ctrl-Home if fControl is true).
		/// Return true to suppress normal Home-Key processing
		/// </summary>
		private bool HandleHomeKey(bool fControl)
		{
			if (fControl)
			{
				return false; // Ctrl+Home is now processed as a shortcut
			}
			var sel = RootBox.Selection;
			if (sel == null)
			{
				return false;
			}
			if (sel.SelType == VwSelType.kstText)
			{
				// Unless it's an IP at the start of the string, handle it normally.
				ITsString tss;
				bool fAssocPrev;
				int ichAnchor, ichEnd, hvoObjA, tagA, hvoObjE, tagE, wsE, wsA;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjE, out tagE, out wsE);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObjA, out tagA, out wsA);
				if (hvoObjE != hvoObjA || tagE != tagA || wsE != wsA || ichEnd != ichAnchor || ichEnd != 0)
				{
					return false;
				}
			}
			// Move to the first property, which is the icon of the word analysis
			MoveAnalysisIconOrNext();
			return true;
		}

		/// <summary>
		/// Handle a press of the left arrow key.
		/// </summary>
		private bool HandleLeftKey()
		{
			var tsi = new TextSelInfo(RootBox);
			var parent = InterlinDoc;
			if (parent == null)
			{
				return false;
			}
			if (tsi.IsPicture)
			{
				var tagTextProp = tsi.TagAnchor;
				if (tagTextProp >= ktagMinIcon && tagTextProp < ktagLimIcon)
				{
					if (m_vc.RightToLeft)
					{
						// selection is on an icon: move to next adjacent text.
						SelectFirstAssociatedText();
						return true;
					}
					// we want to go the text next to the previous icon.
					return SelectTextNearestToNextIcon(false);
				}
			}

			if (tsi.IsRange)
			{
				return false;
			}
			if (tsi.TagAnchor == ktagSbWordGloss)
			{
				return tsi.IchEnd <= 0;
			}
			if (IsSelAtLeftOfMorph)
			{
				var currentLineIndex = GetLineOfCurrentSelection();
				if (m_vc.RightToLeft)
				{
					var index = MorphIndex;
					var cMorphs = Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
					if (index >= cMorphs - 1)
					{
						return true;
					}
					// move to the start of the next morpheme.
					SelectAtStartOfMorph(index + 1, InterlinLineChoices.PreviousOccurrences(currentLineIndex));
				}
				else
				{
					var index = MorphIndex;
					if (index == 0)
					{
						// no more morphemes in this word. move to previous word, selecting last morpheme.
						return true;
					}
					// move to the end of the previous morpheme.
					SelectAtEndOfMorph(index - 1, InterlinLineChoices.PreviousOccurrences(currentLineIndex));
				}

				return true;
			}
			if (tsi.TagAnchor == 0)
			{
				return true;
			}
			return !tsi.Selection.IsEditable && SelectTextNearestToNextIcon(m_vc.RightToLeft);
		}

		bool m_fHandlingRightClickMenu;
		private bool HandleRightClickOnObject(int hvoReal)
		{
			using (var rightClickUiObj = CmObjectUi.MakeUi(Cache, hvoReal))
			{
				rightClickUiObj.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				m_fHandlingRightClickMenu = true;
				try
				{
					return rightClickUiObj.HandleRightClick(this, true, adjustMenu: CmObjectUi.MarkCtrlClickItem);
				}
				finally
				{
					m_fHandlingRightClickMenu = false;
				}
			}
		}

		/// <summary>
		/// The Sandbox is about to close, or move to another word...if there are pending edits,
		/// save them. This is done by simulating a Return key press in the combo.
		/// </summary>
		public void FinishUpOk()
		{
			HideCombos();
		}

		private void DisposeComboHandler()
		{
			m_ComboHandler?.Dispose();
			m_ComboHandler = null;
		}

		/// <summary>
		/// Create the default selection that is wanted when we move to a new word
		/// in a way that does not predetermine what is selected. (For example, left and
		/// right arrow can move to a specific corresponding field.)
		/// </summary>
		public void MakeDefaultSelection()
		{
			if (IsInGlossMode())
			{
				// since we're in the gloss tab first try to select the text of the word gloss,
				// for speed-glossing (LT-8371)
				var startingIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordGloss);
				if (startingIndex != -1)
				{
					SelectOnOrBeyondLine(startingIndex, 1, -1, true, false);
					return;
				}
				// next try for WordPos
				startingIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidWordPos);
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
		private int CheckMorphs()
		{
			var cmorphs = Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
			// Commenting out the following line solves LT-9090 for baseline capitalization changes
			//Debug.Assert(cmorphs != 0, "We should have already initialized our first morpheme.");
			if (cmorphs != 0)
			{
				return cmorphs;
			}
			// Tabbing into a missing morphemes line.
			MakeFirstMorph();
			return 1;
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
			var cMorph = CheckMorphs(); // Create if needed
			var iStartingMorph = MorphIndex;
			if (iStartingMorph < 0)
			{
				return -1;	// our current selection is not a morpheme field.
			}

			// our current selection is a morpheme index, so calculate the next position.
			var iNextMorph = iStartingMorph + increment;
			if (iNextMorph >= 0 && iNextMorph < cMorph)
			{
				return iNextMorph;
			}
			iNextMorph %= cMorph;
			if (iNextMorph < 0)
			{
				iNextMorph += cMorph;
			}
			fOnNextLine = true;
			return iNextMorph;
		}

		private void NextPositionForLexEntryText(int increment, bool fOnNextLine, bool fIsPictureSel, out int currentLineIndex, out int startLineIndex, ref int iNextMorphIndex)
		{
			currentLineIndex = InterlinLineChoices.IndexOf(InterlinLineChoices.kflidLexEntries);
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

		///  <summary />
		/// <param name="startLine"></param>
		/// <param name="increment">(the direction is indicated by increment, which should be 1 or -1)</param>
		/// <param name="iMorph"></param>
		/// <param name="fSkipIconToTextField"></param>
		/// <param name="fWrapToNextLine"></param>
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
		private bool SelectOnOrBeyondLine(int startLine, int limitLine, int increment, int iMorph, bool fSkipIconToTextField, bool fWrapToNextLine)
		{
			if (ParentForm == Form.ActiveForm)
			{
				Focus();
			}
			if (!fWrapToNextLine)
			{
				limitLine = increment > 0 ? InterlinLineChoices.Count : -1;
			}
			var fFirstTime = fWrapToNextLine;
			for (var ispec = startLine; fFirstTime || ispec != limitLine; ispec += increment, fFirstTime = false)
			{
				var ispecOrig = ispec;
				if (ispec == InterlinLineChoices.Count)
				{
					ispec = 0; // wrap around to top
				}
				if (ispec < 0)
				{
					ispec = InterlinLineChoices.Count - 1; // wrap around to bottom.
				}
				if (ispec != ispecOrig)
				{
					// we wrapped lines, test to see if we equal limitLine before continuing.
					if (ispec == limitLine && !fFirstTime)
					{
						return false;
					}
				}
				var spec = InterlinLineChoices[ispec];
				switch (spec.Flid)
				{
					case InterlinLineChoices.kflidWord:
						if (!fSkipIconToTextField && InterlinLineChoices.PreviousOccurrences(ispec) == 0)
						{
							if (ShowAnalysisCombo)
							{
								SelectIcon(ktagAnalysisIcon);
								return true;
							}
							break; // can't do anything else on line 0.
						}
						else
						{
							// make a selection in an alternative writing system.
							MoveSelection(new SelLevInfo[0], ktagSbWordForm, InterlinLineChoices.PreviousOccurrences(ispec));
							return true;
						}
					case InterlinLineChoices.kflidMorphemes:
						var cMorphs = CheckMorphs();
						if (fSkipIconToTextField && iMorph < 0)
						{
							iMorph = 0;
						}

						if (iMorph >= 0 && iMorph < cMorphs)
						{
							SelectAtStartOfMorph(iMorph, InterlinLineChoices.PreviousOccurrences(ispec));
						}
						else
						{
							SelectIconOfMorph(0, ktagMorphFormIcon);
						}
						return true;
					case InterlinLineChoices.kflidLexEntries:
					case InterlinLineChoices.kflidLexGloss:
					case InterlinLineChoices.kflidLexPos:
						if (ispec != InterlinLineChoices.FirstLexEntryIndex)
						{
							break;
						}
						// Move to one of the lex entry icons.
						cMorphs = CheckMorphs();
						var iNextMorph = iMorph >= 0 && iMorph < cMorphs ? iMorph : 0;
						var selIcon = MakeSelectionIcon(AddMorphInfo(new SelLevInfo[0], iNextMorph), ktagMorphEntryIcon, !fSkipIconToTextField);
						if (fSkipIconToTextField)
						{
							// try to select the text next to the icon.
							SelectFirstAssociatedText(selIcon);
						}
						return true;
					case InterlinLineChoices.kflidWordGloss:
						if (!fSkipIconToTextField && m_vc.ShowWordGlossIcon && InterlinLineChoices.PreviousOccurrences(ispec) == 0)
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
						if (InterlinLineChoices.PreviousOccurrences(ispec) != 0)
						{
							break;
						}
						selIcon = MakeSelectionIcon(new SelLevInfo[0], ktagWordPosIcon, !fSkipIconToTextField);
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
		/// <returns>true, if selection was made.</returns>
		private bool SelectAtStartOfMorph(int index)
		{
			return SelectAtStartOfMorph(index, 0);
		}
		private bool SelectAtStartOfMorph(int index, int cprevOccurrences)
		{
			if (InterlinLineChoices.IndexOf(InterlinLineChoices.kflidMorphemes) < 0)
			{
				return false;
			}
			CheckMorphs();	// make sure we have at least one morph to select.
			var hvoMorph = Caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, index);
			if (Caches.DataAccess.get_StringProp(hvoMorph, ktagSbMorphPrefix).Length == 0 || cprevOccurrences > 0)
			{
				// Select at the start of the name of the form
				var selectIndexMorph = new SelLevInfo[2];
				selectIndexMorph[0].tag = ktagSbMorphForm;
				selectIndexMorph[0].ihvo = 0;
				selectIndexMorph[0].cpropPrevious = cprevOccurrences;
				selectIndexMorph[1].tag = ktagSbWordMorphs;
				selectIndexMorph[1].ihvo = index;
				RootBox.MakeTextSelection(0, 2, selectIndexMorph, ktagSbNamedObjName, 0, 0, 0, RawWordformWs, false, -1, null, true);
			}
			else
			{
				// Select at the start of the prefix
				var selectIndexMorph = new SelLevInfo[1];
				selectIndexMorph[0].tag = ktagSbWordMorphs;
				selectIndexMorph[0].ihvo = index;
				RootBox.MakeTextSelection(0, 1, selectIndexMorph, ktagSbMorphPrefix, 0, 0, 0, 0, false, -1, null, true);
			}
			return true;
		}

		private void SelectAtEndOfMorph(int index, int cPrevOccurrences)
		{
			var sda = Caches.DataAccess;
			var hvoMorph = sda.get_VecItem(kSbWord, ktagSbWordMorphs, index);
			var cchPostfix = sda.get_StringProp(hvoMorph, ktagSbMorphPostfix).Length;
			if (cchPostfix == 0 || cPrevOccurrences > 0)
			{
				// Select at the end of the name of the form
				var selectIndexMorph = new SelLevInfo[2];
				selectIndexMorph[0].tag = ktagSbMorphForm;
				selectIndexMorph[0].ihvo = 0;
				selectIndexMorph[0].cpropPrevious = cPrevOccurrences;
				selectIndexMorph[1].tag = ktagSbWordMorphs;
				selectIndexMorph[1].ihvo = index;
				var hvoNamedObj = sda.get_ObjectProp(hvoMorph, ktagSbMorphForm);
				var matchingSpecs = InterlinLineChoices.ItemsWithFlids(new[] { InterlinLineChoices.kflidMorphemes });
				var specMorphemes = matchingSpecs[cPrevOccurrences];
				var ws = specMorphemes.WritingSystem;
				if (specMorphemes.IsMagicWritingSystem)
				{
					ws = RawWordformWs;
				}
				var cchName = sda.get_MultiStringAlt(hvoNamedObj, ktagSbNamedObjName, ws).Length;
				RootBox.MakeTextSelection(0, 2, selectIndexMorph, ktagSbNamedObjName, 0, cchName, cchName, RawWordformWs, false, -1, null, true);
			}
			else
			{
				// Select at the end of the postfix
				var selectIndexMorph = new SelLevInfo[1];
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
			var sda = Caches.DataAccess;
			var cda = (IVwCacheDa)sda;
			var hvoSbMorph = sda.MakeNewObject(kclsidSbMorph, kSbWord, ktagSbWordMorphs, 0);
			var hvoSbForm = sda.MakeNewObject(kclsidSbNamedObj, hvoSbMorph, ktagSbMorphForm, -2); // -2 for atomic
			var wsVern = RawWordformWs;
			var tssForm = SbWordForm(wsVern);
			cda.CacheStringAlt(hvoSbForm, ktagSbNamedObjName, wsVern, tssForm);
			// the morpheme is not a guess, since the user is now working on it.
			cda.CacheIntProp(hvoSbForm, ktagSbNamedObjGuess, 0);
			// Now make a notification to get it redrawn. (A PropChanged doesn't
			// work...   we don't have enough NoteDependency calls.)
			RootBox.Reconstruct();
		}

		internal ITsString SbWordForm(int wsVern)
		{
			var sda = Caches.DataAccess;
			var tssForm = sda.get_MultiStringAlt(kSbWord, ktagSbWordForm, wsVern);
			return tssForm;
		}

		/// <summary>
		/// Add to rgvsli an initial item that selects the indicated morpheme
		/// </summary>
		private static SelLevInfo[] AddMorphInfo(SelLevInfo[] rgvsli, int isense)
		{
			var result = new SelLevInfo[rgvsli.Length + 1];
			rgvsli.CopyTo(result, 1);
			result[0].tag = ktagSbWordMorphs;
			result[0].ihvo = isense;
			return result;
		}

		/// <summary>
		/// Handle a press of the right arrow key.
		/// </summary>
		private bool HandleRightKey()
		{
			var tsi = new TextSelInfo(RootBox);
			if (tsi.IsPicture)
			{
				var tagTextProp = tsi.TagAnchor;
				if (tagTextProp >= ktagMinIcon && tagTextProp < ktagLimIcon)
				{
					if (m_vc.RightToLeft)
					{
						// we want to go the text next to the previous icon.
						return SelectTextNearestToNextIcon(false);
					}
					// selection is on an icon: move to next adjacent text.
					SelectFirstAssociatedText();
					return true;
				}
			}

			if (tsi.IsRange)
			{
				return false;
			}
			if (tsi.TagAnchor == ktagSbWordGloss)
			{
				if (tsi.IchEnd == tsi.AnchorLength)
				{
					return true;	// don't wrap.
				}
			}
			else if (IsSelAtRightOfMorph)
			{
				var currentLineIndex = GetLineOfCurrentSelection();
				if (m_vc.RightToLeft)
				{
					var index = MorphIndex;
					if (index == 0)
					{
						return true;
					}
					// move to the end of the previous morpheme.
					SelectAtEndOfMorph(index - 1, InterlinLineChoices.PreviousOccurrences(currentLineIndex));
				}
				else
				{
					var index = MorphIndex;
					var cMorphs = Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
					if (index >= cMorphs - 1)
					{
						return true;
					}
					// move to the start of the next morpheme.
					SelectAtStartOfMorph(index + 1, InterlinLineChoices.PreviousOccurrences(currentLineIndex));
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
		private bool SelectTextNearestToNextIcon(bool fForward)
		{
			var fShift = !fForward;
			int currentLineIndex;
			int startLineIndex;
			int increment;
			bool fSkipIcon;
			int iNextMorphIndex;
			GetLineOfCurrentSelectionAndNextTabStop(fShift, out currentLineIndex, out startLineIndex, out increment, out fSkipIcon, out iNextMorphIndex);
			var currentMorphIndex = MorphIndex;
			var cMorphs = MorphCount;
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
			var tsi = new TextSelInfo(RootBox);
			if (tsi.IsText)
			{
				var line = GetLineOfCurrentSelection();
				var direction = (fForward ? 1 : -1);
				var nextLine = line + direction;
				var index = MorphIndex;
				SelectOnOrBeyondLine(nextLine, direction, index, true, false);
				return true;
			}
			if (tsi.TagAnchor == 0)
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
			return CurrentAnalysisTree.WfiAnalysis;
		}

		/// <summary>
		/// The wfi analysis currently used to setup the sandbox. Could have possibly come from a guess,
		/// not simply the current annotations InstanceOf.
		/// </summary>
		internal IWfiAnalysis GetWfiAnalysisInUse()
		{
			var wa = GetWfiAnalysisOfAnalysis();
			if (wa != null)
			{
				return wa;
			}
			IWfiGloss tempHvoWordGloss;
			GetDefaults(GetWordformOfAnalysis(), ref wa, out tempHvoWordGloss);
			return wa;
		}


		/// <summary>
		/// Gets the WfiAnalysis HVO of the given analysis.
		/// </summary>
		/// <returns>This will return 0 if the analysis is on the wordform.</returns>
		internal IWfiAnalysis GetWfiAnalysisOfAnalysisObject(ICmObject analysisValueObject)
		{
			if (analysisValueObject == null || !analysisValueObject.IsValidObject)
			{
				return null;
			}
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
		private ICmObject CurrentGuess
		{
			get
			{
				if (m_currentGuess != null && m_currentGuess.Hvo == m_hvoAnalysisGuess)
				{
					return m_currentGuess;
				}
				m_currentGuess = m_hvoAnalysisGuess == 0 ? null : Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoAnalysisGuess);
				return m_currentGuess;
			}
		}

		/// <summary>
		/// This handles the user choosing an analysis from the pull-down in the top ("Word") line of
		/// the Sandbox. The sender must be a ChooseAnalysisHandler (CAH). The Sandbox switches to showing
		/// the analysis indicated by the 'Analysis' property of that CAH.
		/// </summary>
		internal void Handle_AnalysisChosen(object sender, EventArgs e)
		{
			var handler = (ChooseAnalysisHandler)sender;
			var chosenAnalysis = handler.GetAnalysisTree();
			CurrentAnalysisTree = chosenAnalysis;
			var fLookForDefaults = true;
			if (CurrentAnalysisTree.Analysis == null)
			{
				// 'Use default analysis'. This can normally be achieved by loading data
				// after setting m_hvoAnalysis to m_hvoWordformOriginal.
				// But possibly no default is loaded into the cache. (This can happen if
				// all visible occurrences of the wordform were analyzed before the sandbox was
				// displayed.)
				CurrentAnalysisTree.Analysis = m_wordformOriginal;
			}
			else
			{
				// If the user chose an analysis we do not want to fill content in with defaults, use what they picked
				fLookForDefaults = false;
			}

			// REVIEW: do we need to worry about changing the previous and next words?
			LoadRealDataIntoSec(fLookForDefaults, false, false);
			OnUpdateEdited();
			ShowAnalysisCombo = true; // we must want this icon, because we were previously showing it!
			m_rootb.Reconstruct();
			// if the user has selected a special item, such as "New word gloss",
			// set our selection in the most helpful location, if it is available.
			var selectedItem = handler.SelectedItem;
			var fMadeSelection = false;
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
			{
				MakeDefaultSelection();
			}
		}

		// This just makes the combo visible again. It is more common to tell the ComboHandler
		// to Activate.
		internal void ShowCombo()
		{
		}

		/// <summary>
		/// Find the actual original form of the current wordform.
		/// (For more information see definition for Sandbox.FormOfWordForm.)
		/// </summary>
		internal ITsString FindAFullWordForm(IWfiWordform realWordform)
		{
			return realWordform == null ? FormOfWordform : realWordform.Form.get_String(RawWordformWs);
		}

		/// <summary>
		/// Handle setting the wordform shown in the sandbox when the user chooses an alternate
		/// case form in the Morpheme combo.
		/// </summary>
		bool m_fSetWordformInProgress;

		/// <summary>
		/// store the color used to indicate if the wordform has multiple analyses
		/// </summary>
		private int m_multipleAnalysisColor = NoGuessColor;

		private string _undoText;
		private bool _undoEnabled;
		private string _redoText;
		private bool _redoEnabled;

		protected override void Select(bool directed, bool forward)
		{
			MakeDefaultSelection();
		}

		internal void SetWordform(ITsString form, bool fLookForDefaults)
		{
			// stop monitoring edits
			using (new SandboxEditMonitorHelper(EditMonitor, true))
			{
				m_fSetWordformInProgress = true;
				try
				{
					FormOfWordform = form;
					CurrentAnalysisTree.Analysis = GetWordform(form);
					var sda = Caches.DataAccess;
					var cda = (IVwCacheDa)Caches.DataAccess;
					// Now erase the current morph bundles.
					cda.CacheVecProp(kSbWord, ktagSbWordMorphs, new int[0], 0);
					if (CurrentAnalysisTree.Analysis == null)
					{
						Debug.Assert(form != null);
						// No wordform exists corresponding to this case-form.
						// Put the sandbox in a special state where there is just one morpheme, the specified form.
						var hvoMorph0 = sda.MakeNewObject(kclsidSbMorph, kSbWord, ktagSbWordMorphs, 0); // make just one new morpheme.
						var hvoNewForm = sda.MakeNewObject(kclsidSbNamedObj, kSbWord, ktagSbWordDummy, 0);
						// make the object to be the form of the morpheme
						sda.SetMultiStringAlt(hvoNewForm, ktagSbNamedObjName, RawWordformWs, FormOfWordform);
						// set its text
						sda.PropChanged(null, (int) PropChangeType.kpctNotifyAll, hvoNewForm, ktagSbNamedObjName, 0, 1, 1);
						sda.SetObjProp(hvoMorph0, ktagSbMorphForm, hvoNewForm); // and set the reference.
						ClearAllGlosses();
						// and clear the POS.
						sda.SetObjProp(kSbWord, ktagSbWordPos, 0);
						ShowAnalysisCombo = false; // no multiple analyses available for this dummy wordform
					}
					else
					{
						// Set the DataAccess.IsDirty() to true, so this will affect the real analysis when switching words.
						sda.SetMultiStringAlt(kSbWord, ktagSbWordForm, RawWordformWs, FormOfWordform);
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

		internal int CurrentPos(int hvoMorph)
		{
			return Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphPos);
		}

		/// <summary>
		/// Return the hvo best describing the state of the secondary cache for LexEntry fields
		/// (by MSA, LexSense, or LexEntry).
		/// </summary>
		internal int CurrentLexEntriesAnalysis(int hvoMorph)
		{
			// Return LexSense if found
			var hvoMorphSense = Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
			if (hvoMorphSense > 0)
			{
				return hvoMorphSense;
			}
			// Return MSA if found
			var hvoMSA = Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphPos);
			if (hvoMSA > 0)
			{
				return hvoMSA;
			}
			// Return LexEntry.
			var hvoMorphEntry = Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphEntry);
			return hvoMorphEntry;
		}

		/// <summary>
		/// Get's the real lex senses for each current morph.
		/// </summary>
		protected List<int> LexSensesForCurrentMorphs()
		{
			var lexSensesForMorphs = new List<int>();

			var cmorphs = Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
			for (var i = 0; i < cmorphs; i++)
			{
				var hvoMorph = Caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, i);
				var hvoMorphSense = Caches.DataAccess.get_ObjectProp(hvoMorph, ktagSbMorphGloss);
				lexSensesForMorphs.Add(Caches.RealHvo(hvoMorphSense));
			}
			return lexSensesForMorphs;
		}

		/// <summary>
		/// get the current dummy sandbox morphs
		/// </summary>
		protected List<int> CurrentMorphs()
		{
			var hvoMorphs = new List<int>();
			var cmorphs = Caches.DataAccess.get_VecSize(kSbWord, ktagSbWordMorphs);
			for (var i = 0; i < cmorphs; i++)
			{
				var hvoMorph = Caches.DataAccess.get_VecItem(kSbWord, ktagSbWordMorphs, i);
				hvoMorphs.Add(hvoMorph);
			}
			return hvoMorphs;
		}

		// Erase all word level annotations.
		internal void ClearAllGlosses()
		{
			var cda = (IVwCacheDa)Caches.DataAccess;
			foreach (var wsId in InterlinLineChoices.WritingSystemsForFlid(InterlinLineChoices.kflidWordGloss, true))
			{
				var tss = TsStringUtils.EmptyString(wsId);
				cda.CacheStringAlt(kSbWord, ktagSbWordGloss, wsId, tss);
			}
		}

		/// <summary>
		/// indicates whether the sandbox is in a state that can or should be saved.
		/// </summary>
		public bool ShouldSave(bool fSaveGuess)
		{
			return Caches.DataAccess.IsDirty() || fSaveGuess && UsingGuess;
		}

		/// <summary />
		public AnalysisTree GetRealAnalysis(bool fSaveGuess, out IWfiAnalysis obsoleteAna)
		{
			obsoleteAna = null;
			if (!ShouldSave(fSaveGuess))
			{
				return CurrentAnalysisTree;
			}
			FinishUpOk();
			var analMethod = CreateRealAnalysisMethod();
			CurrentAnalysisTree.Analysis = analMethod.Run();
			obsoleteAna = analMethod.ObsoleteAnalysis;
			UsingGuess = false;
			MarkAsInitialState();
			return CurrentAnalysisTree;
		}

		internal GetRealAnalysisMethod CreateRealWfiAnalysisMethod()
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
			if (WordGlossHvo != 0)
			{
				existingGloss = Caches.MainCache.ServiceLocator.GetInstance<IWfiGlossRepository>().GetObject(WordGlossHvo);
			}
			return new GetRealAnalysisMethod(
				PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), this, Caches,
				kSbWord, CurrentAnalysisTree, GetWfiAnalysisOfAnalysis(), existingGloss,
				InterlinLineChoices, FormOfWordform, fWantOnlyWfiAnalysis);
		}

		protected virtual void LoadForWordBundleAnalysis(int hvoWag)
		{
			CurrentAnalysisTree.Analysis = (IAnalysis)Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoWag);
			LoadRealDataIntoSec(true, TreatAsSentenceInitial);
			Debug.Assert(CurrentAnalysisTree.Wordform != null || FormOfWordform != null);
			m_wordformOriginal = CurrentAnalysisTree.Wordform;
			m_hvoInitialWag = hvoWag; // if we reset the focus box, this value we were passed is what we should reset it to.
		}

		#endregion Other methods

		#region Overrides of RootSite
		/// <summary>
		/// Make the root box.
		/// </summary>
		public override void MakeRoot()
		{
			if (Caches.MainCache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			m_vc = new SandboxVc(Caches, InterlinLineChoices, IconsForAnalysisChoices, this)
			{
				ShowMorphBundles = m_fShowMorphBundles,
				MultipleOptionBGColor = MultipleAnalysisColor,
				BackColor = (int) CmObjectUi.RGB(BackColor),
				IsMorphemeFormEditable = IsMorphemeFormEditable
			};
			// Pass through value to VC.

			m_rootb.DataAccess = Caches.DataAccess;

			m_rootb.SetRootObject(kSbWord, m_vc, SandboxVc.kfragBundle, m_stylesheet);

			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			// For some reason, we don't always initialize our control size to be the same as our rootbox.
			Margin = new Padding(3, 0, 3, 1);
			SyncControlSizeToRootBoxSize();
			if (RightToLeftWritingSystem)
			{
				Anchor = AnchorStyles.Right | AnchorStyles.Top;
			}

			//TODO:
			//ptmw->RegisterRootBox(qrootb);
		}

		private void SyncControlSizeToRootBoxSize()
		{
			if (Size.Width != RootBox.Width + Margin.Horizontal || Size.Height != RootBox.Height + Margin.Vertical)
			{
				Size = new Size(RootBox.Width + Margin.Horizontal, RootBox.Height + Margin.Vertical);
			}
		}

		/// <summary>
		/// If sizing to content, a change of the size of the root box requires us to resize the window.
		/// </summary>
		public override void RootBoxSizeChanged(IVwRootBox prootb)
		{
			if (!m_fSizeToContent)
			{
				base.RootBoxSizeChanged(prootb);
				return;
			}
			SyncControlSizeToRootBoxSize();
		}

		/// <summary>
		/// Overide this to provide a context menu for some subclass.
		/// </summary>
		protected override bool DoContextMenu(IVwSelection invSel, Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			return SpellCheckHelper.ShowContextMenu(pt, this) || base.DoContextMenu(invSel, pt, rcSrcRoot, rcDstRoot);
		}

		/// <summary>
		/// Gets (creating if necessary) the SpellCheckHelper
		/// </summary>
		private SpellCheckHelper SpellCheckHelper => m_spellCheckHelper ?? (m_spellCheckHelper = new SpellCheckHelper(Cache));

		/// <summary>
		/// Handle a problem deleting some selection in the sandbox. So far, the only cases we
		/// handle are backspace and delete merging morphemes.
		/// Enhance JohnT: could also handle deleting a range that merges morphemes.
		/// </summary>
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			ITsString tss;
			bool fAssocPrev;
			int ichSel;
			int hvoObj;
			int tag;
			int ws;
			sel.TextSelInfo(false, out tss, out ichSel, out fAssocPrev, out hvoObj, out tag, out ws);
			if (!EditMonitor.IsPropMorphBreak(hvoObj, tag, ws))
			{
				return VwDelProbResponse.kdprFail;
			}
			switch (dpt)
			{
				case VwDelProbType.kdptBsAtStartPara:
				case VwDelProbType.kdptBsReadOnly:
					return EditMonitor.HandleBackspace() ? VwDelProbResponse.kdprDone : VwDelProbResponse.kdprFail;
				case VwDelProbType.kdptDelAtEndPara:
				case VwDelProbType.kdptDelReadOnly:
					return EditMonitor.HandleDelete() ? VwDelProbResponse.kdprDone : VwDelProbResponse.kdprFail;
				default:
					return VwDelProbResponse.kdprFail;
			}
		}

		// Handles a change in the view selection.
		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			base.HandleSelectionChange(rootb, vwselNew);
			if (!vwselNew.IsValid)
			{
				return;
			}
			EditMonitor.DoPendingMorphemeUpdates();
			if (!vwselNew.IsValid)
			{
				return;
			}
			DoActionOnIconSelection(vwselNew);
		}

		private bool IsSelectionOnIcon(IVwSelection vwselNew)
		{
			var selInfo = new TextSelInfo(vwselNew);
			return selInfo.TagAnchor >= ktagMinIcon && selInfo.TagAnchor < ktagLimIcon;
		}

		/// <summary>
		/// If we have an action installed for this icon selection, do it.
		/// </summary>
		private bool DoActionOnIconSelection(IVwSelection vwselNew)
		{
			// See if this is an icon selection.
			var selInfo = new TextSelInfo(vwselNew);
			if (!IsSelectionOnIcon(vwselNew))
			{
				return false;
			}
			switch (selInfo.TagAnchor)
			{
				case ktagMorphFormIcon:
				case ktagMorphEntryIcon:
				case ktagWordPosIcon:
				case ktagAnalysisIcon:
				case ktagWordGlossIcon:
					// Combo Icons
					if (!m_fSuppressShowCombo)
					{
						ShowComboForSelection(vwselNew, true);
					}
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
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (PassKeysToKeyboardHandler)
			{
				base.OnKeyDown(e);
				return;
			}

			switch (e.KeyCode)
			{
				case Keys.Up:
					if (e.Alt || IsIconSelected)
					{
						OnOpenCombo();
						e.Handled = true;
						return;
					}
					if (!e.Control && !e.Shift)
					{
						if (HandleUpKey())
						{
							return;
						}
					}
					break;
				case Keys.Down:
					if (e.Alt || IsIconSelected)
					{
						OnOpenCombo();
						e.Handled = true;
						return;
					}
					if (!e.Control && !e.Shift)
					{
						if (HandleDownKey())
						{
							return;
						}
					}
					break;
				case Keys.Space:
					if (IsIconSelected)
					{
						OnOpenCombo();
						e.Handled = true;
						return;
					}
					break;
				case Keys.Tab:
					if (!e.Control && !e.Alt)
					{
						HandleTab(e.Shift);
						// skip base.OnKeyDown, so RootSite will not try to move our cursor
						// to another field in addition to HandleTab causing Tab to advance
						// past the expected icon/field.
						return;
					}
					break;
				case Keys.Enter:
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
				case Keys.End:
					if (HandleEndKey(e.Control))
					{
						return;
					}
					break;
				case Keys.Home:
					if (HandleHomeKey(e.Control))
					{
						return;
					}
					break;
				case Keys.Right:
					if (!e.Control && !e.Shift)
					{
						if (HandleRightKey())
						{
							return;
						}
					}
					break;
				case Keys.Left:
					if (!e.Control && !e.Shift)
					{
						if (HandleLeftKey())
						{
							return;
						}
					}
					break;
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
			if (!PassKeysToKeyboardHandler)
			{
				if (IsWordPosIconSelected && !char.IsControl(e.KeyChar))
				{
					OnOpenCombo();
					var tree = ((IhMissingWordPos)m_ComboHandler).Tree;
					tree.SelectNodeStartingWith(Surrogates.StringFromCodePoint(e.KeyChar));
				}

				if (e.KeyChar == '\t' || e.KeyChar == '\r')
				{
					// gobble these up in Sandbox. so the base.OnKeyPress()
					// does not duplicate what we've handled in OnKeyDown().
					e.Handled = true;
					return;
				}
			}

			base.OnKeyPress(e);
		}

		/// <summary>
		/// Handle the mouse moving...remember where it was last in case it turns into a hover.
		/// </summary>
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
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
#if TraceMouseCalls
				Debug.WriteLine("SandboxBase.OnMouseMove(" + m_LastMouseMovePos.ToString() + "): rcSrcRoot = " + rcSrcRoot.ToString() + ", rcDstRoot = " + rcDstRoot.ToString());
#endif
				var pt = PixelToView(m_LastMouseMovePos);
				var vwsel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (vwsel != null)
				{
#if TraceMouseCalls
					Debug.WriteLine("SandboxBase.OnMouseMove(): pt = " + pt.ToString() + ", vwsel.SelType = " + vwsel.SelType.ToString());
#endif
					// If we're over one of the icons we want an arrow cursor, to indicate that
					// clicking will do something other than make a text selection.
					// Review: should we just have an arrow cursor everywhere in this view?
					if (vwsel.SelType == VwSelType.kstPicture)
					{
						Cursor = Cursors.Arrow;
					}
					// If we want hover effects and there isn't some editing in progress,
					// display a combo.
					if (ComboOnMouseHover && !m_fLockCombo)
					{
						ShowComboForSelection(vwsel, false);
					}
				}
#if TraceMouseCalls
				else
					Debug.WriteLine("SandboxBase.OnMouseMove(): pt = " + pt.ToString() + ", vwsel = null");
#endif
			}
		}

		/// <summary>
		/// Process right mouse button down.  In particular, handle CTRL+Right Mouse Click.
		/// </summary>
		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
			{
				return true; //discard this event
			}

			if (m_rootb == null)
			{
				return false;
			}
			try
			{
				// Create a selection where we right clicked
				var sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				// Figure what property is selected and create a suitable class if
				// appropriate.  (CLevels includes the string property itself, but
				// AllTextSelInfo doesn't need it.)
				var cvsli = sel.CLevels(false) - 1;

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
				int hvoReal;
				tagRightClickTextProp = GetInfoForJumpToTool(sel, out hvoReal);
				if (hvoReal != 0)
				{
					//IxCoreColleague spellingColleague = null;
					if (tagRightClickTextProp == ktagSbWordGloss)
					{
						if (SpellCheckHelper.ShowContextMenu(pt, this))
						{
							return true;
						}
						// This is an alternative approach, currently not fully implmented, which allows the spell check
						// menu items to be added to a menu that has further options.
						//spellingColleague = EditingHelper.MakeSpellCheckColleague(pt, m_rootb, rcSrcRoot, rcDstRoot);
					}

					if (HandleRightClickOnObject(hvoReal))
					{
						return true;
					}
				}
				else
				{
					return false;
				}
			}
			catch (Exception)
			{
				throw;
			}
			return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
		}

		/// <summary>
		/// Given a selection (typically from a click), determine the object that should be the target for jumping to,
		/// and return the property that was clicked (which in the case of a right-click may generate a spelling menu instead).
		/// </summary>
		private int GetInfoForJumpToTool(IVwSelection sel, out int hvoReal)
		{
			int ws;
			int tagRightClickTextProp;
			bool fAssocPrev;
			ITsString tss;
			int ichAnchorDum;
			int hvoRightClickObject;
			sel.TextSelInfo(false, out tss, out ichAnchorDum, out fAssocPrev, out hvoRightClickObject, out tagRightClickTextProp, out ws);
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
					if (tagOuter == ktagSbMorphGloss || tagOuter == ktagSbMorphPos || tagOuter == ktagSbMorphForm || tagOuter == ktagSbMorphEntry)
					{
						m_hvoRightClickMorph = hvoOuterObj;
					}
					break;
				default:
					m_hvoRightClickMorph = 0;
					break;
			}

			hvoReal = Caches.RealHvo(hvoRightClickObject);
			return tagRightClickTextProp;
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (e.Button != MouseButtons.Left || (ModifierKeys & Keys.Control) != Keys.Control)
			{
				return;
			}
			// Control-click: take the first jump-to-tool command from the right-click menu for this location.
			// Create a selection where we right clicked
			var sel = GetSelectionAtPoint(new Point(e.X, e.Y), false);
			int hvoTarget;
			GetInfoForJumpToTool(sel, out hvoTarget);
			if (hvoTarget == 0)
			{
				return; // LT-13878: User may have 'Ctrl+Click'ed on an arrow or off in space somewhere
			}

			using (var targetUiObj = CmObjectUi.MakeUi(Cache, hvoTarget))
			{
				targetUiObj.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				targetUiObj.HandleCtrlClick(this);
			}
		}

#if RANDYTODO
		public virtual bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			if (!m_fHandlingRightClickMenu)
				return false;
			XCore.Command cmd = (XCore.Command)commandObject;
			string tool = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "tool");
			string className = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "className");

			// The menu item CmdPOSJumpToDefault is used in the Sandbox for jumping to morpheme POS,
			// and we don't want it to show up if we don't have a morpheme. But, although we don't
			// enable it (GetHvoForJumpToToolClass will return zero for class "PartOfSpeech" if there's no
			// morpheme), when clicking ON the POS for the word, the PartOfSpeechUi object will enable it.
			// So in this special case we have to claim to have handled the task but should NOT enable it.
			if (tool == AreaServices.PosEditMachineName && className == "PartOfSpeech" && m_hvoRightClickMorph == 0)
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
#endif

		// This is common code for OnJumpToTool and OnDisplayJumpToTool, both called while displaying
		// a context menu and dealing with a menu item. ClassName is taken from the item XML
		// and indicates what sort of thing we want to jump to. Based on either information about what
		// was clicked, or general information about what the Sandbox is showing, figure out which
		// object we should jump to (if any) for this type of jump.
		private int GetHvoForJumpToToolClass(string className)
		{
			var clid = 0;
			if (CurrentGuess != null)
			{
				clid = CurrentGuess.ClassID;
			}
			int hvoMsa;
			switch (className)
			{
				case "WfiWordform":
					return CurrentAnalysisTree.Wordform.Hvo;
				case "WfiAnalysis":
				{
					switch (clid)
					{
						case WfiAnalysisTags.kClassId:
							return m_hvoAnalysisGuess;
						case WfiGlossTags.kClassId:
							return CurrentGuess.OwnerOfClass(WfiAnalysisTags.kClassId).Hvo;
					}
				}
					break;
				case "WfiGloss":
					{
						if (clid == WfiGlossTags.kClassId)
						{
							return m_hvoAnalysisGuess;
						}
					}
					break;
				case "MoForm":
					return GetObjectFromRightClickMorph(ktagSbMorphForm);
				case "LexEntry":
					var result = GetObjectFromRightClickMorph(ktagSbMorphEntry);
					if (result != 0)
					{
						return result;
					}
					return GetMostPromisingEntry();
				case "LexSense":
					return GetObjectFromRightClickMorph(ktagSbMorphGloss);
				case "PartOfSpeech":
					hvoMsa = GetObjectFromRightClickMorph(ktagSbMorphPos);
					if (hvoMsa == 0)
					{
						return 0;
					}
					// TODO: We really want the guid, and it's usually just as accessible as
					// the hvo, so methods like have been migrating to returning the guid.
					// This method should do likewise...
					using (var ui = CmObjectUi.MakeUi(Caches.MainCache, hvoMsa))
					{
						ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
						var guid = ui.GuidForJumping(null);
						return guid == Guid.Empty ? 0 : Cache.ServiceLocator.GetObject(guid).Hvo;
					}
				//LT-12195 Change Show Concordance of Category right click menu item for Lex Gram. Info. line of Interlinear.
				case "PartOfSpeechGramInfo":
					hvoMsa = GetObjectFromRightClickMorph(ktagSbMorphPos);
					return hvoMsa;
				case "WordPartOfSpeech":
					hvoMsa = GetObjectFromRightClickMorph(ktagSbWordPos);
					if (hvoMsa != 0)
					{
						return hvoMsa;
					}
					IWfiAnalysis realAnalysis = null;
					switch (clid)
					{
						case WfiAnalysisTags.kClassId:
							realAnalysis = CurrentGuess as IWfiAnalysis;
							break;
						case WfiGlossTags.kClassId:
							realAnalysis = CurrentGuess.OwnerOfClass(WfiAnalysisTags.kClassId) as IWfiAnalysis;
							break;
					}

					if (realAnalysis?.CategoryRA != null)
					{
						// JohnT: not sure it CAN be null, but play safe.
						return realAnalysis.CategoryRA.Hvo;
					}
					break;
			}
			return 0;
		}

		/// <summary>
		/// Return the HVO of the most promising LexEntry to jump to. This is a fall-back when
		/// the user has not clicked on a morpheme.
		/// </summary>
		private int GetMostPromisingEntry()
		{
			var wordform = Caches.DataAccess.get_MultiStringAlt(kSbWord, ktagSbWordForm, RawWordformWs);
			List<ILexEntry> homographs = null;
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () => { homographs =
				Cache.ServiceLocator.GetInstance<ILexEntryRepository>().CollectHomographs(wordform.Text,
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem));
				});
			return homographs.Count == 0 ? 0 : homographs[0].Hvo;
			// Enhance JohnT: possibly if there is more than one homograph we could try to match the word gloss
			// against one of its senses?
		}

		private int GetObjectFromRightClickMorph(int tag)
		{
			if (m_hvoRightClickMorph == 0)
			{
				return 0;
			}
			var hvoTarget = Caches.DataAccess.get_ObjectProp(m_hvoRightClickMorph, tag);
			return hvoTarget == 0 ? 0 : Caches.RealHvo(hvoTarget);
		}

		private FocusBoxController Controller
		{
			get
			{
				var container = Parent;
				while (container != null)
				{
					if (!(container is FocusBoxController))
					{
						container = container.Parent;
					}
					else
					{
						return (FocusBoxController)container;
					}
				}
				return null;
			}
		}

		public virtual bool OnJumpToTool(object commandObject)
		{
#if RANDYTODO
			XCore.Command cmd = (XCore.Command)commandObject;
			string tool = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "tool");
			string className = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "className");
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
					LcmCache cache = m_caches.MainCache;
					ICmObject co = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					var fwLink = new FwLinkArgs(tool, co.Guid);
					List<Property> additionalProps = fwLink.LinkProperties;
					if (!string.IsNullOrEmpty(concordOn))
					{
						additionalProps.Add(new Property("ConcordOn", concordOn));
					}
					LinkHandler.PublishFollowLinkMessage(Publisher, fwLink);
					return true;
				}
			}
#endif
			return false;
		}

		/// <summary>
		/// Do nothing if the Sandbox somehow gets refreshed directly...its parent window destroys and
		/// recreates it.
		/// </summary>
		public override bool RefreshDisplay()
		{
			return false;
		}

		/// <summary>
		/// Simulate MouseDown on the rootbox, but allow selections in read-only text.
		/// </summary>
		protected override void CallMouseDown(Point point, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			m_fInMouseDrag = false;
			if (m_rootb == null)
			{
				return;
			}
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

		/// <summary>
		/// Simulate MouseDownExtended on the rootbox, but allow selections in read-only text.
		/// </summary>
		protected override void CallMouseDownExtended(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (m_rootb == null)
			{
				return;
			}
#if TraceMouseCalls
				Debug.WriteLine("Sandbox.CallMouseDownExtended(pt = {"+pt.X+", "+pt.Y+"})" +
					" - fInDrag = " + m_fInMouseDrag + ", fNewSel = " + m_fNewSelection);
#endif
			m_wsPending = -1;
			var vwsel = m_rootb.Selection;
			if (vwsel == null)
			{
				m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, true);
				EditingHelper.HandleMouseDown();
			}
			else
			{
				var vwsel2 = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, true);
				if (vwsel.SelType == vwsel2.SelType && vwsel.SelType == VwSelType.kstText)
				{
					m_rootb.MakeRangeSelection(vwsel, vwsel2, true);
				}
			}
		}

		/// <summary>
		/// Call MouseMoveDrag on the rootbox
		/// </summary>
		protected override void CallMouseMoveDrag(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (m_rootb == null || m_fMouseInProcess || m_fMouseDownActivatedCombo)
			{
				return;
			}
#if TraceMouseCalls
				Debug.WriteLine("Sandbox.CallMouseMoveDrag(pt = {"+ pt.X +", " + pt.Y +"})" +
					" - fInDrag = " + m_fInMouseDrag + ", fNewSel = " + m_fNewSelection);
#endif
			if (m_fNewSelection)
			{
				m_fInMouseDrag = true;
			}

			m_fMouseInProcess = true;

			try
			{
				// The VScroll is 'false' for the Sandbox now.  So the old code
				// is being removed.  It's very much like the SimpleRootSite code
				// and should be revisited if the sandbox ever gets a vertical
				// scroll bar.
				if (m_fNewSelection)
				{
					CallMouseDownExtended(pt, rcSrcRoot, rcDstRoot);
				}
			}
			finally
			{
				m_fMouseInProcess = false;
			}
		}

		/// <summary>
		/// Call MouseUp on the rootbox
		/// </summary>
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
			// Displaying Right-To-Left Graphite behaves badly if available width gets up to
			// one billion (10**9) or so.  See LT-6077.  One million (10**6) should be ample
			// for simulating infinite width.
			return m_fSizeToContent ? 10000000 : base.GetAvailWidth(prootb);
		}

		// We absolutely don't ever want the Sandbox to scroll.
		public override bool ScrollSelectionIntoView(IVwSelection sel, VwScrollSelOpts scrollOption)
		{
			return false;
		}

		#endregion Overrides of RootSite

		#region Implementation of IUndoRedoHandler

		/// <summary>
		/// Get the text for the Undo menu.
		/// </summary>
		string IUndoRedoHandler.UndoText => ITextStrings.ksUndoAllChangesHere;

		/// <summary>
		/// Get the enabled condition for the Undo menu.
		/// </summary>
		bool IUndoRedoHandler.UndoEnabled(bool callerEnableOpinion)
		{
			return Caches.DataAccess.IsDirty() || callerEnableOpinion;
		}

		/// <summary>
		/// Handle Undo event
		/// </summary>
		public bool HandleUndo(object sender, EventArgs e)
		{
			if (!Caches.DataAccess.IsDirty())
			{
				// We didn't handle it. Caller may be able to undo something we don't know about.
				return false;
			}
			ReconstructForWordBundleAnalysis(m_hvoInitialWag);
			m_fHaveUndone = true;
			return true;
		}

		/// <summary>
		/// Get the text for the Redo menu.
		/// </summary>
		string IUndoRedoHandler.RedoText => (Caches.DataAccess.IsDirty() || m_fHaveUndone) && m_fHaveUndone ? ITextStrings.ksCannotRedoChangesHere : LanguageExplorerResources.Redo;

		/// <summary>
		/// Get the enabled condition for the Undo menu.
		/// </summary>
		bool IUndoRedoHandler.RedoEnabled(bool callerEnableOpinion)
		{
			// if the cache isn't dirty and we've done an Undo inside the annotation, we want
			// a special message saying we can't redo. If the cache IS dirty, the user has been
			// doing something since the Undo, so shouldn't expect Redo;
			return !Caches.DataAccess.IsDirty() && !m_fHaveUndone && callerEnableOpinion;
		}

		/// <summary>
		/// Handle Redo event
		/// </summary>
		bool IUndoRedoHandler.HandleRedo(object sender, EventArgs e)
		{
			if (Caches.DataAccess.IsDirty() || m_fHaveUndone)
			{
				return true; // We (didn't) do it.
			}
			// We didn't handle it. Caller may be able to undo something we don't know about.
			return false;
		}

		#endregion
	}
}