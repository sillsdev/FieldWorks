using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Application;
using System.Linq;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.IText
{
	public partial class InterlinDocForAnalysis : InterlinDocRootSiteBase
	{
		/// <summary>
		/// Review(EricP) consider making a subclass of InterlinDocForAnalysis (i.e. InterlinDocForGlossing)
		/// so we can put all AddWordsToLexicon related code there rather than having this
		/// class do double duty.
		/// </summary>
		internal const string ksPropertyAddWordsToLexicon = "ITexts_AddWordsToLexicon";

		public InterlinDocForAnalysis()
		{
			InitializeComponent();
			RightMouseClickedEvent += InterlinDocForAnalysis_RightMouseClickedEvent;
			DoSpellCheck = true;
		}

		void InterlinDocForAnalysis_RightMouseClickedEvent(SimpleRootSite sender, FwRightMouseClickEventArgs e)
		{
			e.EventHandled = true;
			// for the moment we always claim to have handled it.
			ContextMenuStrip menu = new ContextMenuStrip();

			// Add spelling items if any (i.e., if we clicked a squiggle word).
			int hvoObj, tagAnchor;
			if (GetTagAndObjForOnePropSelection(e.Selection, out hvoObj, out tagAnchor) &&
				(tagAnchor == SegmentTags.kflidFreeTranslation || tagAnchor == SegmentTags.kflidLiteralTranslation ||
				tagAnchor == NoteTags.kflidContent))
			{
				var helper = new SpellCheckHelper(Cache);
				helper.MakeSpellCheckMenuOptions(e.MouseLocation, this, menu);
			}

			int hvoNote;
			if (CanDeleteNote(e.Selection, out hvoNote))
			{
				if (menu.Items.Count > 0)
				{
					menu.Items.Add(new ToolStripSeparator());
				}
				// Add the delete item.
				string sMenuText = ITextStrings.ksDeleteNote;
				ToolStripMenuItem item = new ToolStripMenuItem(sMenuText);
				item.Click += OnDeleteNote;
				menu.Items.Add(item);
			}
			if (menu.Items.Count > 0)
			{
				e.Selection.Install();
				menu.Show(this, e.MouseLocation);
			}
		}

		internal void SuppressResettingGuesses(Action task)
		{
			m_vc.Decorator.SuppressResettingGuesses(task);
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			if (IsFocusBoxInstalled && FocusBox.SelectedOccurrence != null
				&& tag == SegmentTags.kflidAnalyses && FocusBox.SelectedOccurrence.Segment.Hvo == hvo)
			{
				int index = FocusBox.SelectedOccurrence.Index;
				var seg = FocusBox.SelectedOccurrence.Segment;
				if (!seg.IsValidObject || index >= seg.AnalysesRS.Count ||
					(FocusBox.SelectedOccurrence.Analysis is IPunctuationForm))
				{
					// Somebody drastically changed things under us, maybe from another window.
					// Try to fend off a crash.
					TryHideFocusBoxAndUninstall();
					return;
				}
				if (seg.AnalysesRS[index] != FocusBox.InitialAnalysis.Analysis)
				{
					// Somebody made a less drastic change under us. Reset the focus box.
					FocusBox.SelectOccurrence(FocusBox.SelectedOccurrence);
					MoveFocusBoxIntoPlace();
				}
			}
		}

		protected override void UpdateWordforms(HashSet<IWfiWordform> wordforms)
		{
			base.UpdateWordforms(wordforms);
			// It's fairly pathological for the Analysis of an occurrence to be null, and while the Wordform of an analyis can
			// be null (if it's punctuation), the focus box shouldn't be pointing at punctuation. However, we've had reported
			// null ref exceptions in the absence of testing for this (LT-13702), and we certainly don't need to update the focus box
			// because its wordform has been updated if we can't find an actual wordform associated with the focus box.
			if (IsFocusBoxInstalled && FocusBox.SelectedOccurrence != null && FocusBox.SelectedOccurrence.Analysis != null
				&& FocusBox.SelectedOccurrence.Analysis.Wordform != null
				&& wordforms.Contains(FocusBox.SelectedOccurrence.Analysis.Wordform)
				&& !FocusBox.IsDirty)
			{
				// update focus box to display new guess
				FocusBox.SelectOccurrence(FocusBox.SelectedOccurrence);
				MoveFocusBoxIntoPlace();
			}
		}

		void OnDeleteNote(object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			UndoableUnitOfWorkHelper.Do(string.Format(ITextStrings.ksUndoCommand, item.Text),
				string.Format(ITextStrings.ksRedoCommand, item.Text), Cache.ActionHandlerAccessor,
				() => DeleteNote(RootBox.Selection));
		}

		private void DeleteNote(IVwSelection sel)
		{
			int hvoNote;
			if (!CanDeleteNote(sel, out hvoNote))
				return;
			var note = Cache.ServiceLocator.GetInstance<INoteRepository>().GetObject(hvoNote);
			var segment = (ISegment)note.Owner;
			segment.NotesOS.Remove(note);
		}
		/// <summary>
		///  Answer true if the indicated selection is within a single note we can delete.
		/// </summary>
		/// <param name="sel"></param>
		/// <returns></returns>
		private bool CanDeleteNote(IVwSelection sel, out int hvoNote)
		{
			hvoNote = 0;
			int tagAnchor, hvoObj;
			if (!GetTagAndObjForOnePropSelection(sel, out hvoObj, out tagAnchor))
				return false;
			if (tagAnchor != NoteTags.kflidContent)
				return false; // must be a selection in a note to be deletable.
			hvoNote = hvoObj;
			return true;
		}

		/// <summary>
		///  Answer true if the indicated selection is within a single note we can delete. Also obtain
		/// the object and property.
		/// </summary>
		private bool GetTagAndObjForOnePropSelection(IVwSelection sel, out int hvoObj, out int tagAnchor)
		{
			hvoObj = tagAnchor = 0;
			if (sel == null)
				return false;
			ITsString tss;
			int ichEnd, hvoEnd, tagEnd, wsEnd;
			bool fAssocPrev;
			sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoEnd, out tagEnd, out wsEnd);
			int ichAnchor, hvoAnchor, wsAnchor;
			sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoAnchor, out tagAnchor, out wsAnchor);
			if (hvoEnd != hvoAnchor || tagEnd != tagAnchor || wsEnd != wsAnchor)
				return false; // must be a one-property selection
			hvoObj = hvoAnchor;
			return true;
		}

		/// <summary>
		/// factory
		/// </summary>
		protected override void MakeVc()
		{
			m_vc = new InterlinDocForAnalysisVc(m_fdoCache);
		}

		#region Overrides of RootSite

		/// <summary>
		/// If you lost focus while processing a key or click, it may be because you are
		/// making a new selection before calling a method like TryHideFocusBoxAndUninstall().
		/// Hide focus and uninstall first!
		/// </summary>
		protected override void OnLostFocus(EventArgs e)
		{
			if (m_vc != null)
				m_vc.SetActiveFreeform(0, 0, 0, 0);
			base.OnLostFocus(e);
		}

		/// <summary>
		/// If we have an active focus box put the focus back there when this is focused.
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			if (ExistingFocusBox != null)
				ExistingFocusBox.Focus();
			base.OnGotFocus(e);
		}

		#endregion

		#region ISelectOccurrence

		/// <summary>
		/// Select the word indicated by the occurrence.
		/// Note that this does not save any changes made in the Sandbox. It is mainly used
		/// when the view is read-only.
		/// </summary>
		public override void SelectOccurrence(AnalysisOccurrence target)
		{
			if (target == null)
			{
				TryHideFocusBoxAndUninstall();
				return;
			}
			if (SelectedOccurrence == target && IsFocusBoxInstalled)
			{
				// Don't steal the focus from another window.  See FWR-1795.
				if (ParentForm == Form.ActiveForm)
				{
					if (ExistingFocusBox.CanFocus)
					{
						ExistingFocusBox.Focus(); // important when switching tabs with ctrl-tab.
					}
					else
					{
						VisibleChanged += FocusWhenVisible;
					}
				}
				return;
			}
			if (!m_vc.CanBeAnalyzed(target))
				return;
#if DEBUG
			// test preconditions.
			Debug.Assert(target.IsValid && !(target.Analysis is IPunctuationForm), "Given annotation type should not be punctuation"
				+ " but was " + target.Analysis.ShortName + ".");
#endif
			TriggerAnnotationSelected(target, true);
		}

		/// <summary>
		/// Move the sandbox (see main method), making the default selection.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		public void TriggerAnnotationSelected(AnalysisOccurrence target, bool fSaveGuess)
		{
			TriggerAnalysisSelected(target, fSaveGuess, true);
		}

		/// <summary>
		/// Move the sandbox to the AnalysisOccurrence, (which may be a WfiWordform, WfiAnalysis, or WfiGloss).
		/// </summary>
		/// <param name="target"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fMakeDefaultSelection">true to make the default selection within the new sandbox.</param>
		public virtual void TriggerAnalysisSelected(AnalysisOccurrence target, bool fSaveGuess, bool fMakeDefaultSelection)
		{
			TriggerAnalysisSelected(target, fSaveGuess, fMakeDefaultSelection, true);
		}

		/// <summary>
		/// Move the sandbox to the AnalysisOccurrence, (which may be a WfiWordform, WfiAnalysis, or WfiGloss).
		/// </summary>
		/// <param name="target"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fMakeDefaultSelection">true to make the default selection within the new sandbox.</param>
		/// <param name="fShow">true makes the focusbox visible.</param>
		public virtual void TriggerAnalysisSelected(AnalysisOccurrence target, bool fSaveGuess, bool fMakeDefaultSelection, bool fShow)
		{
			// This can happen, though it is rare...see LT-8193.
			if (!target.IsValid)
			{
				return;
			}
			if (IsFocusBoxInstalled)
				FocusBox.UpdateRealFromSandbox(null, fSaveGuess, target);
			TryHideFocusBoxAndUninstall();
			RecordGuessIfNotKnown(target);
			InstallFocusBox();
			RootBox.DestroySelection();
			FocusBox.SelectOccurrence(target);
			SetFocusBoxSizeForVc();
			SelectedOccurrence = target;

			if (fShow)
			{
				SimulateReplaceAnalysis(target);
				MoveFocusBoxIntoPlace();
				// Now it is the right size and place we can show it.
				TryShowFocusBox();
				// All this CAN hapen because we're editing in another window...for example,
				// if we edit something that deletes the current wordform in a concordance view.
				// In that case we don't want to steal the focus.
				if (ParentForm == Form.ActiveForm)
					FocusBox.FocusSandbox();
			}

			if (fMakeDefaultSelection)
				m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, FocusBox.MakeDefaultSelection);
		}

		// Set the VC size to match the FocusBox. Return true if it changed.
		bool SetFocusBoxSizeForVc()
		{
			if (m_vc == null || ExistingFocusBox == null)
				return false;
			var interlinDocForAnalysisVc = m_vc as InterlinDocForAnalysisVc;
			if (interlinDocForAnalysisVc == null)
				return false; // testing only? Anyway nothing can change.
			//FocusBox.PerformLayout();
			int dpiX, dpiY;
			using (Graphics g = CreateGraphics())
			{
				dpiX = (int)g.DpiX;
				dpiY = (int)g.DpiY;
			}
			int width = FocusBox.Width;
			if (width > 10000)
			{
				//				Debug.Assert(width < 10000); // Is something taking the full available width of MaxInt/2?
				width = 500; // arbitrary, may allow something to work more or less
			}
			Size newSize = new Size(width * 72000 / dpiX,
				FocusBox.Height * 72000 / dpiY);
			if (newSize.Width == interlinDocForAnalysisVc.FocusBoxSize.Width && newSize.Height == interlinDocForAnalysisVc.FocusBoxSize.Height)
				return false;

			interlinDocForAnalysisVc.FocusBoxSize = newSize;
			return true;
		}

		/// <summary>
		/// Something about the display of the AnalysisOccurrence has changed...perhaps it has become or ceased to
		/// be the current annotation displayed using the Sandbox, or the Sandbox changed size. Produce
		/// a PropChanged that makes the system think it has been replaced (with itself) to refresh the
		/// relevant part of the display.
		/// </summary>
		void SimulateReplaceAnalysis(AnalysisOccurrence occurrence)
		{
			UpdateDisplayForOccurrence(occurrence);
		}

		protected override void SetRootInternal(int hvo)
		{
			// If the focus box is showing when we change the root object, we must get rid of it,
			// otherwise strange things may happen as the pane is laid out and we get OnSizeChanged calls.
			// The existing focus box can't be in any state that is useful to a different text, anyway.
			TryHideFocusBoxAndUninstall();
			base.SetRootInternal(hvo);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (IsFocusBoxInstalled)
				MoveFocusBoxIntoPlace();
		}
		/// <summary>
		/// Move the sand box to the appropriate place.
		/// Note: if we're already in the process of MoveSandbox, let's not do anything. It may crash
		/// if we try it again (LT-5932).
		/// </summary>
		private bool m_fMovingSandbox;

		internal void MoveFocusBoxIntoPlace()
		{
			MoveFocusBoxIntoPlace(false);
		}

		internal void MoveFocusBoxIntoPlace(bool fJustChecking)
		{
			if (m_fMovingSandbox)
				return;
			try
			{
				m_fMovingSandbox = true;
				var sel = MakeSandboxSel();
				if (fJustChecking)
				{
					// Called during paint...don't want to force a scroll to show it (FWR-1711)
					if (ExistingFocusBox == null || sel == null)
						return;
					var desiredLocation = GetSandboxSelLocation(sel);
					if (desiredLocation == FocusBox.Location)
						return; // don't force a scroll.
				}
				// The sequence is important here. Even without doing this scroll, the sandbox is always
				// visible: I think .NET must automatically scroll to make the focused control visible,
				// or maybe we have some other code I've forgotten about that does it. But, if we don't
				// both scroll and update, the position we move the sandbox to may be wrong, after the
				// main window is fully painted, with possible position changes due to expanding lazy stuff.
				// If you change this, be sure to test that in a several-page interlinear text, with the
				// Sandbox near the bottom, you can turn 'show morphology' on and off and the sandbox
				// ends up in the right place.
				if (sel == null)
				{
					Debug.WriteLine("could not select annotation");
					return;
				}
				if (!fJustChecking)
				{
					// During paint we do NOT want to force another paint, still less to force the focus box
					// into view when it may have been purposely scrolled off.
					// At other times we need the part of the view that contains the focus box to be actually
					// painted (and hence lazy stuff expanded) before we make our final determination of the position.
					ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
					Update();
				}
				var ptLoc = GetSandboxSelLocation(sel);
				if (ExistingFocusBox != null && FocusBox.Location != ptLoc)
					FocusBox.Location = ptLoc;
			}
			finally
			{
				m_fMovingSandbox = false;
			}
		}

		/// <summary>
		/// If we try to scroll to show the focus box before we are Created, our attempt to set the scroll position is ignored.
		/// This is an attempt to recover and make sure that even the first time the view is being created, we are scrolled
		/// to show the focus box if we have set one up.
		/// </summary>
		protected override void OnCreateControl()
		{
			base.OnCreateControl();
			Debug.Assert(Created);
			if (IsFocusBoxInstalled)
				MoveFocusBoxIntoPlace(false);
		}

		/// <summary>
		/// As a last resort for making sure the focus box is where we think it should be,
		/// check every time we paint. A recursive call may well happen, since
		/// an Update() is called if MoveFocusBoxIntoPlace needs to scroll. However, it can't get
		/// infinitely recursive, since MoveFocusBoxIntoPlace is guarded against being called
		/// again while it is active.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
#if !__MonoCS__ // FWNX-419
			if (!MouseMoveSuppressed && IsFocusBoxInstalled)
				MoveFocusBoxIntoPlace(true);
#endif
		}

		/// <summary>
		/// Return the selection that corresponds to the SandBox position.
		/// </summary>
		/// <returns></returns>
		internal IVwSelection MakeSandboxSel()
		{
			if (m_hvoRoot == 0 || SelectedOccurrence == null)
				return null;
			return SelectOccurrenceInIText(SelectedOccurrence);
		}

		/// <summary>
		/// Get the next segment with either a non-null annotation that is configured or
		/// a non-punctuation analysis. Also skip segments that are Scripture labels (like
		/// Chapter/Verse/Footnote numbers.
		/// It tries the next one after the SelectedOccurrence.Segment
		/// then tries the next paragraph, etc..
		/// Use this version if the calling code already has the actual para/seg objects.
		/// </summary>
		/// <param name="currentPara"></param>
		/// <param name="seg"></param>
		/// <param name="upward">true if moving up and left, false otherwise</param>
		/// <param name="realAnalysis">the first or last real analysis found in the next segment</param>
		/// <returns>A segment meeting the criteria or null if not found.</returns>
		private ISegment GetNextSegment(IStTxtPara currentPara, ISegment seg, bool upward,
				out AnalysisOccurrence realAnalysis)
		{
			ISegment nextSeg = null;
			realAnalysis = null;
			var currentText = currentPara.Owner as IStText;
			Debug.Assert(currentText != null, "Paragraph not owned by a text.");
			var lines = LineChoices.m_specs as IEnumerable<InterlinLineSpec>;
			var delta = upward ? -1 : 1;
			var nextSegIndex = delta + seg.IndexInOwner;
			do
			{
				if (0 <= nextSegIndex && nextSegIndex < currentPara.SegmentsOS.Count)
				{
					nextSeg = currentPara.SegmentsOS[nextSegIndex];
					nextSegIndex += delta; // increment for next loop in case it doesn't check out
				}
				else
				{   // try the first (last) segment in the next (previous) paragraph
					int nextParaIndex = delta + currentPara.IndexInOwner;
					nextSeg = null;
					IStTxtPara nextPara = null;
					if (0 <= nextParaIndex && nextParaIndex < currentText.ParagraphsOS.Count)
					{   // try to find this paragraph's first (last) segment
						currentPara = (IStTxtPara)currentText.ParagraphsOS[nextParaIndex];
						nextSegIndex = upward ? currentPara.SegmentsOS.Count - 1 : 0;
					}
					else
					{	// no more paragraphs in this text
						break;
					}
				}
				realAnalysis = FindRealAnalysisInSegment(nextSeg, !upward);
			} while (nextSeg == null || (realAnalysis == null && !HasVisibleTranslationOrNote(nextSeg, lines)));
			return nextSeg;
		}

		/// <summary>
		/// Get the next segment with either a non-null annotation that is configured or
		/// a non-punctuation analysis.
		/// It tries the next one after the SelectedOccurrence.Segment
		/// then tries the next paragraph, etc..
		/// </summary>
		/// <param name="paraIndex"></param>
		/// <param name="segIndex"></param>
		/// <param name="upward">true if moving up and left, false otherwise</param>
		/// <param name="realAnalysis">the first or last real analysis found in the next segment</param>
		/// <returns>A segment meeting the criteria or null if not found.</returns>
		internal ISegment GetNextSegment(int paraIndex, int segIndex, bool upward, out AnalysisOccurrence realAnalysis)
		{
			var currentPara = (IStTxtPara)RootStText.ParagraphsOS[paraIndex];
			Debug.Assert(currentPara != null, "Tried to use a null paragraph ind=" + paraIndex);
			ISegment currentSeg = currentPara.SegmentsOS[segIndex];
			Debug.Assert(currentSeg != null, "Tried to use a null segment ind=" + segIndex + " in para " + paraIndex);
			return GetNextSegment(currentPara, currentSeg, upward, out realAnalysis);
		}

		/// <summary>
		/// Gets the first visible (non-null and configured) translation or note line in the current segment.
		/// </summary>
		/// <param name="segment">The segment to get the translation or note flid from.</param>
		/// <param name="ws">The returned writing system for the line needed to identify it or -1.</param>
		/// <returns>The flid of the translation or note or 0 if none is found.</returns>
		internal int GetFirstVisibleTranslationOrNoteFlid(ISegment segment, out int ws)
		{
			var lines = LineChoices.m_specs as IEnumerable<InterlinLineSpec>;
			Debug.Assert(lines != null, "Interlinear line configurations not enumerable 2");
			var annotations = lines.SkipWhile(line => line.WordLevel);
			int tryAnnotationIndex = lines.Count() - annotations.Count();
			if (annotations.Count() > 0)
			{   // We want to select at the start of this translation or note if it is not a null note.
				bool isaNote = annotations.First().Flid == InterlinLineChoices.kflidNote;
				if (isaNote && segment.NotesOS.Count == 0)
				{   // this note is not visible - skip to the next non-note translation or note
					var otherAnnotations = annotations.SkipWhile(line => line.Flid == InterlinLineChoices.kflidNote);
					tryAnnotationIndex = lines.Count() - otherAnnotations.Count();
					if (otherAnnotations.Count() == 0)
						tryAnnotationIndex = -1; // no more translations or notes, go to an analysis in the next segment.
				}
			}
			else // no translations or notes to go to
				tryAnnotationIndex = -1;
			int tryAnnotationFlid = 0;
			ws = -1;
			if (tryAnnotationIndex > -1)
			{
				var lineSpec = lines.Skip(tryAnnotationIndex).First();
				tryAnnotationFlid = lineSpec.Flid;
				ws = lineSpec.WritingSystem;
			}
			return tryAnnotationFlid;
		}

		/// <summary>
		/// Select the first non-null translation or note in the current segment
		/// of the current analysis occurance.
		/// </summary>
		/// <returns>true if successful, false if there is no real translation or note</returns>
		internal bool SelectFirstTranslationOrNote()
		{
			int ws;
			int annotationFlid = GetFirstVisibleTranslationOrNoteFlid(SelectedOccurrence.Segment, out ws);
			if (annotationFlid == 0) return false;
			var sel = MakeSandboxSel();
			int clev = sel.CLevels(true);
			clev--; // result it returns is one more than what the AllTextSelInfo routine wants.
			SelLevInfo[] rgvsli;
			using (ArrayPtr rgvsliTemp = MarshalEx.ArrayToNative<SelLevInfo>(clev))
			{
				int ihvoRoot;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ihvoEnd1;
				int tag, ws1;
				bool fAssocPrev;
				ITsTextProps ttp;
				sel.AllTextSelInfo(out ihvoRoot, clev, rgvsliTemp, out tag, out cpropPrevious,
								   out ichAnchor, out ichEnd, out ws1, out fAssocPrev, out ihvoEnd1, out ttp);
				rgvsli = MarshalEx.NativeToArray<SelLevInfo>(rgvsliTemp, clev);
			}
			// What non-word "choice" ie., translation text or note is on this line?
			int tagTextProp = ConvertTranslationOrNoteFlidToSegmentFlid(annotationFlid, SelectedOccurrence.Segment, ws);
			int levels;
			SelLevInfo noteLevel = MakeInnerLevelForFreeformSelection(tagTextProp);
			var vsli = new SelLevInfo[3];
			vsli[0] = noteLevel; // note or translation line
			vsli[1] = rgvsli[0]; // segment
			vsli[2] = rgvsli[1]; // para
			int cPropPrevious = 0; // todo: other if not the first WS for tagTextProp
			TryHideFocusBoxAndUninstall();
			RootBox.MakeTextSelection(0, vsli.Length, vsli, tagTextProp, cPropPrevious,
									  0, 0, 0, false, -1, null, true);
			Focus();
			return true;
		}

		/// <summary>
		/// Return the first non-null translation or note selection in the specified segment.
		/// The segment does not need to be the current occurance.
		/// </summary>
		/// <param name="segment">A valid segment.</param>
		/// <returns>The selection or null if there is no real translation or note.</returns>
		internal IVwSelection SelectFirstTranslationOrNote(ISegment segment)
		{
			if (segment == null)
				return null;
			int ws;
			int annotationFlid = GetFirstVisibleTranslationOrNoteFlid(segment, out ws);
			if (annotationFlid == 0)
				return null;
			int tagTextProp = ConvertTranslationOrNoteFlidToSegmentFlid(annotationFlid, segment, ws);
			SelLevInfo noteLevel = MakeInnerLevelForFreeformSelection(tagTextProp);
			// notes and translation lines have 3 levels: 2:para, 1:seg, 0:content or self property
			var vsli = new SelLevInfo[3];
			vsli[0] = noteLevel;  // note or translation line
			vsli[1].ihvo = segment.IndexInOwner; // specifies where segment is in para
			vsli[1].tag = StTxtParaTags.kflidSegments;
			vsli[2].ihvo = segment.Paragraph.IndexInOwner; // specifies where para is in IStText.
			vsli[2].tag = StTextTags.kflidParagraphs;
			int cPropPrevious = 0; // todo: other if not the first WS for tagTextProp
			var sel = RootBox.MakeTextSelection(0, vsli.Length, vsli, tagTextProp, cPropPrevious,
												0, 0, 0, false, 0, null, true);
			Focus();
			TryHideFocusBoxAndUninstall();
			return sel;
		}

		/// <summary>
		/// Sets up the tags for the 0 level of a selection of a free translation or note.
		/// This will be the level "inside" the ones that select the paragraph and segment.
		/// For a note, we need to select the first note.
		/// For a free translation, we need to insert the level for the 'self' property
		/// which the VC inserts to isolate the free translations and make it easier to update them.
		/// </summary>
		/// <param name="tagTextProp">The segment or note tag of an annotation to be selected.</param>
		private SelLevInfo MakeInnerLevelForFreeformSelection(int tagTextProp)
		{
			var noteLevel = new SelLevInfo();
			noteLevel.ihvo = 0;
			if (tagTextProp == NoteTags.kflidContent)
			{
				noteLevel.tag = SegmentTags.kflidNotes;
			}
			else
			{
				noteLevel.tag = Cache.MetaDataCacheAccessor.GetFieldId2(CmObjectTags.kClassId, "Self", false);
			}
			return noteLevel;
		}

		/// <summary>
		/// Converts InterlinLineChoices flids to corresponding SegmentTags.
		/// or NoteTags.
		/// This is useful when making translation or note selections.
		/// </summary>
		/// <param name="annotationFlid">The translation or note Flid to be converted.</param>
		/// <param name="segment">The segment the flid applies to.</param>
		/// <param name="ws">The writing system of the text.</param>
		/// <returns>A flid suitable for making translation or note selections or -1 if unknown.</returns>
		internal int ConvertTranslationOrNoteFlidToSegmentFlid(int annotationFlid, ISegment segment, int ws)
		{
			int tagTextProp = -1;
			switch (annotationFlid)
			{
				case InterlinLineChoices.kflidFreeTrans:
					tagTextProp = SegmentTags.kflidFreeTranslation;
					//if (segment.FreeTranslation.StringOrNull(ws) == null)
					//    tagTextProp = kTagUserPrompt; // user prompt property for empty translation annotations
					break;
				case InterlinLineChoices.kflidLitTrans:
					tagTextProp = SegmentTags.kflidLiteralTranslation;
					//if (segment.LiteralTranslation.StringOrNull(ws) == null)
					//	tagTextProp = kTagUserPrompt; // user prompt property for empty translation annotations
					break;
				case InterlinLineChoices.kflidNote:
					tagTextProp = NoteTags.kflidContent;
					break;
				default:
					Debug.Assert(false, "An annotation flid was not converted for selection - flid = " + annotationFlid);
					break;
			}
			return tagTextProp;
		}

		/// summary>
		/// Get the location of the given selection, presumably that of a Sandbox.
		/// /summary>
		Point GetSandboxSelLocation(IVwSelection sel)
		{
			Debug.Assert(sel != null);
			Rect rcPrimary = GetPrimarySelRect(sel);
			// The location includes margins, so for RTL we need to adjust the
			// Sandbox so it isn't hard up against the next word.
			// Enhance JohnT: ideally we would probably figure this margin
			// to exactly match the margin between words set by the VC.
			int left = rcPrimary.left;
			if (m_vc.RightToLeft)
				left += 8;
			return new Point(left, rcPrimary.top);
		}

		/// <summary>
		/// Overridden for subclasses needing a Sandbox.
		/// </summary>
		/// <param name="rgvsli"></param>
		/// <returns></returns>
		protected override IVwSelection MakeWordformSelection(SelLevInfo[] rgvsli)
		{
			// top prop is atomic, leave index 0. Specifies displaying the contents of the Text.
			IVwSelection sel;
			try
			{
				// This is fine for InterlinDocForAnalysis, since it treats the area that the sandbox
				// will fill as a picture. Not so good for panes (see above).
				sel = RootBox.MakeSelInObj(0, rgvsli.Length, rgvsli, 0, false);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.StackTrace);
				return null;
			}
			return sel;
		}

		#endregion

		internal override AnalysisOccurrence OccurrenceContainingSelection()
		{
			if (m_rootb == null)
				return null;

			// This works fine for non-Sandbox panes,
			// Sandbox panes' selection may be in the Sandbox.
			if (ExistingFocusBox != null &&
				ExistingFocusBox.SelectedOccurrence != null &&
				ExistingFocusBox.SelectedOccurrence.IsValid)
			{
				return ExistingFocusBox.SelectedOccurrence;
			}

			// If the above didn't work, this probably won't either, but try anyway...
			return base.OccurrenceContainingSelection();
		}

		#region Properties

		/// <summary>
		/// Wordform currently being edited through the FocusBox overlay; null if none.
		/// </summary>
		internal AnalysisOccurrence SelectedOccurrence
		{
			get
			{
				return ((InterlinDocForAnalysisVc) m_vc).FocusBoxOccurrence;
			}
			set
			{
				((InterlinDocForAnalysisVc)m_vc).FocusBoxOccurrence = value;
				m_propertyTable.SetProperty("TextSelectedWord",
					value != null && value.HasWordform ? value.Analysis.Wordform : null,
					true);
				m_propertyTable.SetPropertyPersistence("TextSelectedWord", false);
			}
		}

		/// <summary>
		/// (LT-7807) true if this document is in the context/state for adding glossed words to lexicon.
		/// </summary>
		internal bool InModeForAddingGlossedWordsToLexicon
		{
			get
			{
				return LineChoices.Mode == InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon;
			}
		}

		#endregion

		#region AddWordsToLexicon

		public override void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch (name)
			{
				case ksPropertyAddWordsToLexicon:
					if (this.LineChoices != null)
					{
						// whenever we change this mode, we may also
						// need to show the proper line choice labels, so put the lineChoices in the right mode.
						InterlinLineChoices.InterlinMode newMode = GetSelectedLineChoiceMode();
						if (LineChoices.Mode != newMode)
						{
							var saved = SelectedOccurrence;
							this.TryHideFocusBoxAndUninstall();
							this.LineChoices.Mode = newMode;
							// the following reconstruct will destroy any valid selection (e.g. in Free line).
							// is there anyway to do a less drastic refresh (e.g. via PropChanged?)
							// that properly adjusts things?
							this.RefreshDisplay();
							if (saved != null)
								TriggerAnnotationSelected(saved, false);
						}
					}
					break;
				default:
					base.OnPropertyChanged(name);
					break;
			}
		}

		internal InterlinLineChoices.InterlinMode GetSelectedLineChoiceMode()
		{
			return m_propertyTable.GetBoolProperty(ksPropertyAddWordsToLexicon, false) ?
				InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon : InterlinLineChoices.InterlinMode.Gloss;
		}

		#endregion

		#region AddGlossesToFreeTranslation

		protected override void OnKeyDown(KeyEventArgs e)
		{
			// detect whether the user is doing a range selection with the keyboard within
			// a freeform annotation, and try to keep the selection within the bounds of the editable selection. (LT-2910)
			if (RootBox != null && (e.Modifiers & Keys.Shift) == Keys.Shift)
			{
				TextSelInfo tsi = new TextSelInfo(RootBox);
				int hvoAnchor = tsi.HvoAnchor;
				if (hvoAnchor != 0)
				{
					ICmObject coAnchor = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoAnchor);
					if ((coAnchor is ISegment && (tsi.TagAnchor == SegmentTags.kflidFreeTranslation || tsi.TagAnchor == SegmentTags.kflidLiteralTranslation))
						|| (coAnchor is INote && tsi.TagAnchor == NoteTags.kflidContent))
					{
						// we are in a segment-level annotation.
						if (e.KeyCode == Keys.Home)
						{
							// extend the selection to the beginning of the comment.
							SelectionHelper selHelper = SelectionHelper.GetSelectionInfo(tsi.Selection, this);
							selHelper.IchEnd = 0;
							selHelper.MakeRangeSelection(RootBox, true);
							return;
						}
					}
				}
			}
			// LT-9570 for the Tree Translation line, Susanna wanted Enter to copy Word Glsses
			// into the Free Translation line. Note: DotNetBar is not handling shortcut="Enter"
			// for the XML <command id="CmdAddWordGlossesToFreeTrans"...
			if (RootBox != null && e.KeyCode == Keys.Enter)
				OnAddWordGlossesToFreeTrans(null);

			// LT-4029 Capture arrow keys from inside the translation lines and notes.
			var change = HandleArrowKeys(e);
			// LT-12097 Right and left arrow keys from an empty translation line (part of the issue)
			// The up and down arrows work, so here we changed the event appropriately to up or down.
			if (change != ArrowChange.Handled)
			{
				KeyEventArgs e2;
				switch (change)
				{   // might need to change the key event so the base method will handle it right.
					case ArrowChange.Down:
						e2 = new System.Windows.Forms.KeyEventArgs(Keys.Down);
						break;
					case ArrowChange.Up:
						e2 = new System.Windows.Forms.KeyEventArgs(Keys.Up);
						break;
					case ArrowChange.None:
						e2 = e;
						break;
					default:
						e2 = e;
						break;
				}
				base.OnKeyDown(e2);
			}
		}

		enum ArrowChange {None, Up, Down, Handled}

		/// <summary>
		/// Performs a change in IP when arrow keys should take the IP from a translation or note
		/// to an analysis. Also handles right and left arrow for empty translation lines via the
		/// output enum.
		/// two directions of concern for Left to Right(LTR) and Right To Left(RTL):
		/// 1: up from the first translation or note in a paragraph after a word line possibly
		///  in another paragraph via up arrow or a left (right if RTL) arrow from the first (last)
		///  character of the annotaton
		/// 2: down from the last translation or note in a paragraph before a word line possibly
		///  in another paragraph via down arrow or a right (left if RTL) arrow from the last (right)
		///  character of the annotaton
		/// The following logic accounts for the configured position of notes and whether the user added them.
		/// The idea here is to eliminate as many default cases as possible as early as possible to be handled by
		/// the old annotation OnKeyDown() method.
		/// </summary>
		/// <param name="e">The keyboard event</param>
		/// <returns>handled if it handled the situation, None if not and Up or Down
		/// when that is what's needed from the base method.</returns>
		private ArrowChange HandleArrowKeys(KeyEventArgs e)
		{
			if (SelectedOccurrence == null &&
				(e.KeyCode == Keys.Down || e.KeyCode == Keys.Up ||
				 e.KeyCode == Keys.Right || e.KeyCode == Keys.Left) &&
				((e.KeyCode & Keys.Shift) != Keys.Shift) && ((e.KeyCode & Keys.Control) != Keys.Control))
			{
				//MessageBox.Show("Processing arrow keys!", "What's Going On Here??");
				// It's an arrow key, but is it in a translation line or note?
				// Get the current selection so we can obtain the actual translation or note objects
				SelLevInfo[] rgvsli;
				int clev, tag, ichAnchor, ichEnd, ws;
				bool haveSelection = GetCurrentSelection(out clev, out rgvsli, out tag, out ichAnchor, out ichEnd, out ws);
				if (!haveSelection)
					return ArrowChange.None;

				// get the text, paragraph, segment and note, if there is one
				int curSegIndex, curParaIndex, curNoteIndex;
				ISegment curSeg;
				INote curNote;
				GetCurrentTextObjects(clev, rgvsli, tag,
					out curParaIndex, out curSegIndex, out curNoteIndex, out curSeg, out curNote);

				// what kind of line is it and where is the selection (ie., IP) in the text?
				int id, lineNum;
				WhichEnd where;
				bool isRightToLeft;
				bool hasPrompt;
				bool haveLineInfo = GetLineInfo(curSeg, curNote, tag, ichAnchor, ichEnd, ws,
					out id, out lineNum, out where, out isRightToLeft, out hasPrompt);
				if (!haveLineInfo)
					return ArrowChange.None;

				var lines = LineChoices.m_specs as IEnumerable<InterlinLineSpec>; // so we can use linq
				Debug.Assert(lines != null, "Interlinear line configurations not enumerable");
				bool isUpNewSeg;
				bool isUpMove = DetectUpMove(e, lines, lineNum, curSeg, curNoteIndex, where, isRightToLeft, out isUpNewSeg);

				bool isDownNewSeg = false;
				if (!isUpMove)
				{   // might be a downward move
					isDownNewSeg = DetectDownMove(e, lines, lineNum, curSeg, curNoteIndex, isRightToLeft, where);
					// Should = isDownMove since hasFollowingAnalysis should be false
				}
				if (isUpNewSeg || isDownNewSeg)
				{	// Get the next segment in direction with a real analysis or a real translation or note
					if (IsTranslationOrNoteNext(curParaIndex, curSeg, isUpNewSeg))
						if (hasPrompt && (id == InterlinLineChoices.kflidFreeTrans ||
										  id == InterlinLineChoices.kflidLitTrans))
						{   // moving from an empty translation line to another translation line
							return isUpNewSeg ? ArrowChange.Up : ArrowChange.Down;
						}
						else return ArrowChange.None; // let default handle it
					// a real analysis is next or no more segments
					var occurrence = MoveVerticallyToNextAnalysis(curParaIndex, curSegIndex, isUpNewSeg);
					if (occurrence == null)
						return ArrowChange.None; // only a real translation or note, or it couldn't find a suitable segment
					SelectOccurrence(occurrence); // only works for analyses, not annotations
					return ArrowChange.Handled;
				}
				if (isUpMove)
				{   // Need to move up to a real analysis in the same segment
					IAnalysis nextAnalysis = null;
					int index = 0;
					foreach (var an in curSeg.AnalysesRS.Reverse())
					{	// need to count because an.IndexInOwner == 0 for all an - go figure
						index++;
						if (!an.HasWordform) continue;
						break; // found the last real analysis
					}
					SelectOccurrence(new AnalysisOccurrence(curSeg, curSeg.AnalysesRS.Count - index));
					return ArrowChange.Handled;
				}
				if (hasPrompt && (id == InterlinLineChoices.kflidFreeTrans ||
								  id == InterlinLineChoices.kflidLitTrans)
							  && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
				{   // moving from an empty translation line to right or left
					if (TextIsRightToLeft ? e.KeyCode == Keys.Right : e.KeyCode == Keys.Left)
						return ArrowChange.Up;
					return ArrowChange.Down;
				}
			}
			return ArrowChange.None;
		}

		/// <summary>
		/// Determines if a visible translation or note is the next line in the indicated direction (up or down).
		/// </summary>
		/// <param name="paragraphInd">The index of the current paragraph in the text.</param>
		/// <param name="seg">The current segment in the paragraph.</param>
		/// <param name="moveUp">true if moving up and left, false otherwise</param>
		/// <returns>true when an translation or note is the next line, false otherwise</returns>
		private bool IsTranslationOrNoteNext(int paragraphInd, ISegment seg, bool moveUp)
		{
			var para = (IStTxtPara)RootStText.ParagraphsOS[paragraphInd];
			Debug.Assert(para != null, "Tried to move to a null paragraph ind=" + paragraphInd);
			Debug.Assert(seg != null, "Tried to move to a null segment ind=" + seg.IndexInOwner + " in para " + paragraphInd);
			// get the "next" segment with a real analysis or real translation or note
			var lines = LineChoices.m_specs as IEnumerable<InterlinLineSpec>; // so we can use linq
			while (true)
			{
				AnalysisOccurrence realAnalysis;
				seg = GetNextSegment(para, seg, moveUp, out realAnalysis);
				if (seg == null)
					return false;
				bool hasVisibleAnnotations = HasVisibleTranslationOrNote(seg, lines);
				bool hasRealAnalysis = realAnalysis != null;
				if (moveUp)
				{
					if (hasVisibleAnnotations) // check translation or note first
						return true;
					if (hasRealAnalysis) // then analyses
						return false; // if there is a real one, don't go to an annotation
				}
				else
				{   // moving down
					if (hasRealAnalysis) // check analyses first
						return false; // if there is a real one, don't go to a translation or note
					if (hasVisibleAnnotations) // then check translations and notes
						return true;
				}
				// no translation or note, no real analyses, try the next segment
			}
		}

		/// <summary>
		/// Determines if there are visible translation or note - non null and configured.
		/// </summary>
		/// <param name="seg">The segment to check.</param>
		/// <param name="lines">The configuration line specs.</param>
		/// <returns>true if the segment has at least one visible translation or note.</returns>
		private bool HasVisibleTranslationOrNote(ISegment seg, IEnumerable<InterlinLineSpec> lines)
		{
			return lines.Any(line =>
							 line.Flid == InterlinLineChoices.kflidFreeTrans ||
							 line.Flid == InterlinLineChoices.kflidLitTrans ||
							 line.Flid == InterlinLineChoices.kflidNote && seg.NotesOS.Count > 0);
		}

		/// <summary>
		/// Detect that upward movement out of this segment or to an analysis
		/// in this same segment is needed.
		/// Considerations:
		/// Configured analysis lines preceed translation or note lines (currently).
		/// Analyses are stored as a sequence in the segement.
		/// Some analyses are punctuation that are skipped by the IP.
		/// Only analyses that have a word in them are considered "real".
		/// "Annotation" lines include translation lines (free and literal) and notes.
		/// Each translation or note in a diffeerent ws is a different line.
		/// Translations are stored in a segment as a multistring while notes are
		/// in a sequence of multistrings.
		/// Each note is repeated in each note line of a different ws.
		/// So, the IP may be in the first configured note, but not in the first note.
		/// </summary>
		/// <param name="e">The keyboard event being handled.</param>
		/// <param name="lines">The configured interlinear lines in display order.</param>
		/// <param name="lineNum">The current line in lines that has the selection (or IP).</param>
		/// <param name="curSeg">The segment that has the selection.</param>
		/// <param name="curNoteIndex">The note that is selected in the sequence.</param>
		/// <param name="where">Indicates where the IP is in the selected text if any.</param>
		/// <param name="isRightToLeft">true if the current line is RTL</param>
		/// <param name="isUpNewSeg">Output set to true if moving out of this segment.</param>
		/// <returns>true if the IP should be moved upward to an analysis in this segment.</returns>
		private bool DetectUpMove(KeyEventArgs e, IEnumerable<InterlinLineSpec> lines, int lineNum,
			ISegment curSeg, int curNoteIndex, WhichEnd where, bool isRightToLeft,
			out bool isUpNewSeg)
		{
			var linesBefore = lines.Take(lineNum);
			var annotationsBefore = linesBefore;
			bool hasPreviousAnalysis = false;
			if (linesBefore.Any()) // will have some lines if there are analyses
			{
				annotationsBefore = linesBefore.SkipWhile(line => line.WordLevel);
				hasPreviousAnalysis = linesBefore.Any(line => line.WordLevel);
			}
			bool hasPrevAnnotation = false;
			if (annotationsBefore.Any()) // if this is the first annotation, annotationsBefore is empty
			{
				bool hasNotesBefore = annotationsBefore.Any(line => line.Flid == InterlinLineChoices.kflidNote);
				hasPrevAnnotation = HasVisibleTranslationOrNote(curSeg, annotationsBefore);
			}
			else
			{	// this is the first translation or note and it can't be a null note because it was selected
				bool noteIsFirstAnnotation = lines.ToArray()[lineNum].Flid == InterlinLineChoices.kflidNote;
				hasPrevAnnotation = noteIsFirstAnnotation && curNoteIndex > 0; // can have notes or empty notes before it
			}
			bool hasUpMotion = (e.KeyCode == Keys.Up) ||
				(TextIsRightToLeft ?
					(e.KeyCode == Keys.Right && (where == WhichEnd.Right || where == WhichEnd.Both)) :
					(e.KeyCode == Keys.Left && (where == WhichEnd.Left || where == WhichEnd.Both)));
			bool isUpMove = hasUpMotion && !hasPrevAnnotation;
			isUpNewSeg = isUpMove && !isThereRealAnalysisInSegment(curSeg); // no puctuation, or analysis ws
			return isUpMove;
		}

		/// <summary>
		/// Detect that downward movement out of this segment is needed.
		/// Considerations:
		/// Configured analysis lines preceed translation or note lines (currently).
		/// "Annotation" lines include translation lines (free and literal) and notes.
		/// Each translation or note in a diffeerent ws is a different line.
		/// Translations are stored in a segment as a multistring while notes are
		/// in a sequence of multistrings.
		/// Each note is repeated in each note line of a different ws.
		/// So, the IP may be in the last configured note, but not in the last note.
		/// </summary>
		/// <param name="e">The keyboard event being handled.</param>
		/// <param name="lines">The configured interlinear lines in display order.</param>
		/// <param name="lineNum">The current line in lines that has the selection (or IP).</param>
		/// <param name="curSeg">The segment that has the selection.</param>
		/// <param name="curNoteIndex">The note that is selected in the sequence.</param>
		/// <param name="isRightToLeft"></param>
		/// <param name="where">Indicates where the IP is in the selected text if any.</param>
		/// <returns>true if the IP should be moved upward to an analysis in this segment.</returns>
		private bool DetectDownMove(KeyEventArgs e, IEnumerable<InterlinLineSpec> lines, int lineNum,
			ISegment curSeg, int curNoteIndex, bool isRightToLeft, WhichEnd where)
		{
			var annotationsAfter = lines.Skip(lineNum + 1);
			bool hasFollowingAnnotation = false;
			if (annotationsAfter.Any()) // might not have any
			{
				bool hasNotesAfter = annotationsAfter.Any(line => line.Flid == InterlinLineChoices.kflidNote);
				hasFollowingAnnotation = HasVisibleTranslationOrNote(curSeg, annotationsAfter);
			}
			else
			{	// this is the last translation or note and it can't be a null note because it was selected
				bool noteIsLastAnnotation = LineChoices[LineChoices.Count - 1].Flid == InterlinLineChoices.kflidNote;
				hasFollowingAnnotation = noteIsLastAnnotation && curNoteIndex < curSeg.NotesOS.Count - 1;
			}
			bool hasDownMotion = (e.KeyCode == Keys.Down) ||
								 (TextIsRightToLeft ?
									(e.KeyCode == Keys.Left && (where == WhichEnd.Left || where == WhichEnd.Both)) :
									(e.KeyCode == Keys.Right && (where == WhichEnd.Right || where == WhichEnd.Both)));
			return hasDownMotion && !hasFollowingAnnotation;
		}

		/// <summary>
		/// Assumes the selection data belongs to a translation or note!
		/// Gets the InterlinLineChoices flid (id) and a meaningful interpretation of
		/// where the IP is in the translation or note text.
		/// If an empty translation note was selected, its tag is kTagUserPrompt.
		/// </summary>
		/// <param name="curSeg">The selected segment to get the translation or note text from.</param>
		/// <param name="curNote">null or the selected note</param>
		/// <param name="tag">The SegmentTags or NoteTags or kTagUserPrompt selected.</param>
		/// <param name="ichAnchor">The start index of the text selection.</param>
		/// <param name="ichEnd">The end index of the text selection.</param>
		/// <param name="wid">Index of the writing system of the selection.</param>
		/// <param name="id">The returned InterlinLineChoices flid.</param>
		/// <param name="lineNum">Configured line number of the translation or note.</param>
		/// <param name="where">The returned meaningful interpretation of where the IP is in the translation or note text.</param>
		/// <param name="isRightToLeft">is set to <c>true</c> if the Configured line is right to left, false otherwise.</param>
		/// <param name="hasPrompt">is set to <c>true</c> if the line is an empty translation.</param>
		/// <returns>
		/// true if the information was found, false otherwise.
		/// </returns>
		private bool GetLineInfo(ISegment curSeg, INote curNote, int tag, int ichAnchor, int ichEnd, int wid,
						out int id, out int lineNum, out WhichEnd where, out bool isRightToLeft, out bool hasPrompt)
		{
			isRightToLeft = false;
			hasPrompt = false;
			var wsf = Cache.WritingSystemFactory;
			var ws = wsf.get_EngineOrNull(wid);
			if (ws != null)
				isRightToLeft = ws.RightToLeftScript;

			id = 0;
			lineNum = -1;
			where = WhichEnd.Neither;
			switch (tag)
			{
				case SegmentTags.kflidFreeTranslation:
					id = InterlinLineChoices.kflidFreeTrans;
					where = ExtremePositionInString(ichAnchor, ichEnd, curSeg.FreeTranslation.get_String(wid).Length, isRightToLeft);
					break;
				case SegmentTags.kflidLiteralTranslation:
					id = InterlinLineChoices.kflidLitTrans;
					where = ExtremePositionInString(ichAnchor, ichEnd, curSeg.LiteralTranslation.get_String(wid).Length, isRightToLeft);
					break;
				case NoteTags.kflidContent:
					Debug.Assert(curNote != null, "Moving from a non-exisiting note in interlinear Doc.");
					id = InterlinLineChoices.kflidNote;
					where = ExtremePositionInString(ichAnchor, ichEnd, curNote.Content.get_String(wid).Length, isRightToLeft);
					break;
				case kTagUserPrompt: // user prompt property for empty translation annotations
					// Is this free or literal?
					hasPrompt = true;
					id = m_vc.ActiveFreeformFlid;
					id = (id == SegmentTags.kflidLiteralTranslation) ?
						InterlinLineChoices.kflidLitTrans : InterlinLineChoices.kflidFreeTrans;
					if (wid == 0)
						wid = m_vc.ActiveFreeformWs;
					where = WhichEnd.Both;
					break;
				default: // not expected
					return false;
			}
			if (wid > 0)
				lineNum = LineChoices.IndexOf(id, wid);
			if (lineNum == -1)
				lineNum = LineChoices.IndexOf(id);
			return true;
		}

		/// <summary>
		/// Retrieves the selected objects and data from the selection range.
		/// </summary>
		/// <param name="clev">The number of levels of selection range results.</param>
		/// <param name="rgvsli">The selection range with clev levels of structure.</param>
		/// <param name="tag">The property of the bottom-level [0] object that is of interest.</param>
		/// <param name="curParaIndex">The index of the paragraph containing the selected text.</param>
		/// <param name="curSegIndex">The index of the segment containing the selected text.</param>
		/// <param name="curNoteIndex">if tag indicates a note, the note index in its segment sequence otherwise -1.</param>
		/// <param name="curSeg">The selected segment object</param>
		/// <param name="curNote">The selected note object or null if curNoteIndex is -1.</param>
		private void GetCurrentTextObjects(int clev, SelLevInfo[] rgvsli, int tag, out int curParaIndex, out int curSegIndex, out int curNoteIndex, out ISegment curSeg, out INote curNote)
		{
			curParaIndex = rgvsli[clev - 2].ihvo;
			var curPara = (IStTxtPara)RootStText.ParagraphsOS[curParaIndex];
			Debug.Assert(curPara != null, "Moving from a non-exisiting paragraph in interlinear Doc.");
			curSegIndex = rgvsli[clev - 3].ihvo;
			curSeg = curPara.SegmentsOS[curSegIndex];
			Debug.Assert(curSeg != null, "Moving from a non-exisiting segment in interlinear Doc.");
			curNote = null;
			curNoteIndex = -1;
			if (tag == NoteTags.kflidContent)
			{
				//if clev == 5 then we have both a Free Translation and some number of Notes
				//otherwise I assume we have only a Free Translation if clev == 4
				if (clev == 5)
				{
					curNoteIndex = rgvsli[0].ihvo; //if there are multiple Notes the index could be more than 0
					curNote = curSeg.NotesOS[curNoteIndex];
				}
			}
		}

		/// <summary>
		/// Gets the current selection and returns enough data to move the IP.
		/// </summary>
		/// <param name="clev">The number of levels of selection range results.</param>
		/// <param name="rgvsli">The selection range with clev levels of structure.</param>
		/// <param name="tag">The property of the bottom-level [0] object that is of interest.</param>
		/// <param name="ichAnchor">The start index of the text selection.</param>
		/// <param name="ichEnd">The end index of the text selection.</param>
		/// <param name="ws">Index of the writing system of the selection.</param>
		/// <returns>true if a selection was made, false if something prevented it.</returns>
		private bool GetCurrentSelection(out int clev, out SelLevInfo[] rgvsli, out int tag, out int ichAnchor, out int ichEnd, out int ws)
		{
			clev = -1;
			rgvsli = null;
			tag = -1;
			ichAnchor = -1;
			ichEnd = -1;
			ws = -1;
			var sel = EditingHelper.RootBoxSelection;
			if (sel == null)
				return false;
			// which "line choice" is active in this segment?
			if (sel.SelType != VwSelType.kstText || !sel.IsValid || !sel.IsEditable)
				return false;
			clev = sel.CLevels(true);
			ITsTextProps ttp;
			using (ArrayPtr rgvsliTemp = MarshalEx.ArrayToNative<SelLevInfo>(clev))
			{
				int ihvoRoot;
				int ihvoEnd1;
				int cpropPrevious;
				bool fAssocPrev;
				sel.AllTextSelInfo(out ihvoRoot, clev, rgvsliTemp, out tag, out cpropPrevious,
								   out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd1, out ttp);
				rgvsli = MarshalEx.NativeToArray<SelLevInfo>(rgvsliTemp, clev);
			}
			return true;
		}

		/// <summary>
		/// Moves from the current segment to the next that has a real word line or
		/// a real translation or note depending on the direction.
		/// If none, try the appropriate segment in the next paragraph.
		/// Continue until a suitable analysis or translation or note is found or there are no more to check.
		/// </summary>
		/// <param name="paragraphInd">The index of the current paragraph in the text.</param>
		/// <param name="segmentInd">The index of the current section in the paragraph.</param>
		/// <param name="moveUpward">true if moving up and left, false otherwise</param>
		/// <returns>A segment contatining an analysis or translation or note or null if none was found.</returns>
		private AnalysisOccurrence MoveVerticallyToNextAnalysis(int paragraphInd, int segmentInd, bool moveUpward)
		{
			var para = (IStTxtPara)RootStText.ParagraphsOS[paragraphInd];
			Debug.Assert(para != null, "Tried to move to a null paragraph ind=" + paragraphInd);
			ISegment seg = para.SegmentsOS[segmentInd];
			Debug.Assert(seg != null, "Tried to move to a null segment ind=" + segmentInd + " in para " + paragraphInd);
			// get the "next" segment with a real analysis or real translation or note
			AnalysisOccurrence realAnalysis;
			GetNextSegment(para, seg, moveUpward, out realAnalysis);
			return realAnalysis;
		}

		/// <summary>
		/// Answers true if there is a "real" analysis in the segment.
		/// </summary>
		/// <param name="seg">The seg.</param>
		/// <returns>True if a "real" analysis was found</returns>
		private bool isThereRealAnalysisInSegment(ISegment seg)
		{
			return FindRealAnalysisInSegment(seg, true) != null;
		}

		/// <summary>
		/// Finds a real analysis in the segment from the indicated direction.
		/// When used just to check if there is an analysis, the direction doesn't matter.
		/// </summary>
		/// <param name="seg">The seg.</param>
		/// <param name="forward">if set to <c>true</c> find a real analysis looking forward.</param>
		/// <returns>The analysis found or null</returns>
		private AnalysisOccurrence FindRealAnalysisInSegment(ISegment seg, bool forward)
		{
			if (seg == null) return null;
			int index = -1;
			AnalysisOccurrence realAnalysis = null;
			bool found = false;
			for (int i = 0; i < seg.AnalysesRS.Count; i++)
			{	// need to count to create occurances
				index++;
				int ind = forward ? index : seg.AnalysesRS.Count - index;
				realAnalysis = new AnalysisOccurrence(seg, ind);
				if (m_vc.CanBeAnalyzed(realAnalysis))
				{
					found = true;
					break; // found the first or last real analysis
				}
			}
			return found ? realAnalysis : null;
		}

		/// <summary>
		/// {CC2D43FA-BBC4-448A-9D0B-7B57ADF2655C}
		/// </summary>
		enum WhichEnd { Left, Neither, Right, Both } // Both if the string is null or empty

		/// <summary>
		/// Determines if the selection is at the start, end or other position in the string.
		/// Accounts for writing system direction based on the line spec.
		/// </summary>
		/// <param name="selStart">the starting position of the selection</param>
		/// <param name="selEnd">the end position of the selection</param>
		/// <param name="selLength">the length of the string the selection is in</param>
		/// <param name="isRightToLeft">if set to <c>true</c> the writing system of this line is right to left, otherwise ltr.</param>
		/// <returns>
		/// An enum indicating where the selection is in the string.
		/// </returns>
		private static WhichEnd ExtremePositionInString(int selStart, int selEnd, int selLength, bool isRightToLeft)
		{
			if (0 == selLength)
				return WhichEnd.Both;
			//if (isRightToLeft ? selEnd >= selLength - 1 : selStart <= 0)
			if (selStart <= 0)
				return WhichEnd.Left;
			//if (isRightToLeft ? selStart <= 0 : selEnd >= selLength - 1)
			if (selEnd >= selLength)
				return WhichEnd.Right; // this happens with punctuation at end!
			return WhichEnd.Neither;
		}

		/// <summary>
		/// Enable the 'insert word glosses' command
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <param name="display">The display properties.</param>
		/// <returns>true if this command is enabled</returns>
		public bool OnDisplayAddWordGlossesToFreeTrans(object commandObject, ref UIItemDisplayProperties display)
		{
			ISegment dummy1;
			int dummy2;
			display.Visible = display.Enabled = CanAddWordGlosses(out dummy1, out dummy2);
			return true;
		}

		/// <summary>
		/// Answer whether the AddWordGlossesToFreeTranslation menu option should be enabled.
		/// Also get the Segment to which they can be added.
		/// </summary>
		/// <param name="seg">The seg.</param>
		/// <param name="ws">The ws.</param>
		/// <returns>
		/// 	<c>true</c> if this instance [can add word glosses] the specified seg; otherwise, <c>false</c>.
		/// </returns>
		private bool CanAddWordGlosses(out ISegment seg, out int ws)
		{
			seg = null;
			ws = 0; // actually meaningless unless it returns true, but make compiler happy.
			if (RootBox.Selection == null)
				return false;
			if (!Focused)
				return false;
			ITsString tss;
			int ich;
			int tag;
			bool fAssocPrev;
			int hvoSeg;
			RootBox.Selection.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvoSeg, out tag, out ws);
			if (tag != kTagUserPrompt)
				return false; // no good if not in a prompt for an empty translation.
			seg = Cache.ServiceLocator.GetInstance<ISegmentRepository>().GetObject(hvoSeg);
			if (seg == null)
				return false; // And must be a property of a segment (we only use these prompts for the two translation props)
			int dummy;
			if (ws == 0) // a prompt, use ws of first character.
				ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
			return true;
		}

		/// <summary>
		/// Make a free translation line out of the current glosses.
		/// Note that this is sometimes called by reflection; the parameter of type object is required to match
		/// the expected signature even though it is not used.
		/// </summary>
		public void OnAddWordGlossesToFreeTrans(object arg)
		{
			int ws;
			ISegment seg;
			if (!CanAddWordGlosses(out seg, out ws))
				return;
			int wsText = WritingSystemServices.ActualWs(Cache, WritingSystemServices.kwsVernInParagraph,
				m_hvoRoot, StTextTags.kflidParagraphs);

			ITsStrBldr bldr = TsStrBldrClass.Create();
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			bool fOpenPunc = false;
			ITsString space = TsStringUtils.MakeTss(" ", ws);
			foreach (var analysis in seg.AnalysesRS)
			{
				ITsString insert = null;
				if (analysis.Wordform == null)
				{
					// PunctForm...insert its text.
					ITsStrBldr puncBldr = analysis.GetForm(ws).GetBldr();
					fOpenPunc = false;
					if (puncBldr.Length > 0)
					{
						char ch = puncBldr.Text[0];
						if (ch == StringUtils.kChObject)
							TsStringUtils.TurnOwnedOrcIntoUnownedOrc(puncBldr, 0);
						else
						{
							System.Globalization.UnicodeCategory ucat = Char.GetUnicodeCategory(ch);
							if (ucat == System.Globalization.UnicodeCategory.InitialQuotePunctuation ||
								ucat == System.Globalization.UnicodeCategory.OpenPunctuation)
							{
								puncBldr.ReplaceTsString(0, 0, space);
								fOpenPunc = true;
							}
						}
					}
					// Ensure the punctuation is in the proper analysis writing system.  See LT-9971.
					puncBldr.SetIntPropValues(0, puncBldr.Length, (int)FwTextPropType.ktptWs,
						(int)FwTextPropVar.ktpvDefault, ws);
					insert = puncBldr.GetString();
				}
				else
				{
					if (analysis is IWfiGloss)
					{
						insert = ((IWfiGloss)analysis).Form.get_String(ws);
					}
					else if (analysis is IWfiAnalysis || analysis is IWfiWordform)
					{
						// check if we have a guess cached with a gloss. (LT-9973)
						int guessHvo = m_vc.GetGuess(analysis);
						if (guessHvo != 0)
						{
							var guess = Cache.ServiceLocator.ObjectRepository.GetObject(guessHvo) as IWfiGloss;
							if (guess != null)
								insert = guess.Form.get_String(ws);
						}
					}
					else
					{
						continue;
					}
					if (bldr.Length > 0 && insert != null && insert.Length > 0 && !fOpenPunc)
						bldr.ReplaceTsString(bldr.Length, bldr.Length, space);
					fOpenPunc = false;
				}
				if (insert == null || insert.Length == 0)
					continue;
				bldr.ReplaceTsString(bldr.Length, bldr.Length, insert);
			}
			// Replacing the string when the new one is empty is useless, and may cause problems,
			// e.g., LT-9416, though I have not been able to reproduce that.
			if (bldr.Length == 0)
				return;
			// if we're trying to replace a user prompt, record which line of a multilingual annotation
			// is being changed.  See LT-9421.
			SetCpropPreviousForInsert();
			var helper = SelectionHelper.Create(this);
			int flid = m_vc.ActiveFreeformFlid;
			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoSetTransFromWordGlosses,
				ITextStrings.ksRedoSetTransFromWordGlosses,
				Cache.ActionHandlerAccessor,
				() =>
				{
					RootBox.DataAccess.SetMultiStringAlt(seg.Hvo, flid, ws, bldr.GetString());
				});
			helper.TextPropId = flid;
			helper.SetTextPropId(SelectionHelper.SelLimitType.End, flid);
			helper.IchAnchor = bldr.Length;
			helper.IchEnd = bldr.Length;
			helper.NumberOfPreviousProps = m_cpropPrevForInsert;
			helper.MakeRangeSelection(RootBox, true);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			// if we're trying to replace a user prompt, record which line of a multilingual annotation
			// is being changed.  See LT-9421.
			SetCpropPreviousForInsert();
			base.OnKeyPress(e);
		}

		public override void PrePasteProcessing()
		{
			CheckDisposed();
			// if we're trying to replace a user prompt, record which line of a multilingual annotation
			// is being changed.  See LT-9421.
			SetCpropPreviousForInsert();
		}

		/// <summary>
		/// This variable records the information needed to ensure the insertion point is placed
		/// on the correct line of a multilingual annotation when replacing a user prompt.
		/// See LT-9421.
		/// </summary>
		int m_cpropPrevForInsert = -1;
		/// <summary>
		/// This computes and saves the information needed to ensure the insertion point is
		/// placed on the correct line of a multilingual annotation when replacing a user
		/// prompt.  See LT-9421.
		/// </summary>
		private void SetCpropPreviousForInsert()
		{
			m_cpropPrevForInsert = -1;
			if (RootBox != null)
			{
				var tsi = new TextSelInfo(RootBox);
				if (tsi.Selection == null)
					return;
				var co = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(tsi.HvoAnchor);
				var freeAnn = co as ICmIndirectAnnotation;
				if (tsi.TagAnchor == SimpleRootSite.kTagUserPrompt
					&& freeAnn != null)
				{
					var helper = SelectionHelper.GetSelectionInfo(tsi.Selection, this);
					int wsField = 0;
					if (tsi.TssAnchor != null && tsi.TssAnchor.Length > 0)
						wsField = TsStringUtils.GetWsAtOffset(tsi.TssAnchor, 0);
					else
						return;
					var rgsli = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
					var itagSegments = -1;
					for (var i = rgsli.Length; --i >= 0; )
					{
						if (rgsli[i].tag == StTxtParaTags.kflidSegments)
						{
							itagSegments = i;
							break;
						}
					}
					if (itagSegments >= 0)
					{
						int hvoSeg = rgsli[itagSegments].hvo;
						var annType = freeAnn.AnnotationTypeRA;
						int idx = 0;
						var choices = m_vc.LineChoices;
						for (int i = choices.FirstFreeformIndex; i < choices.Count; )
						{
							var ffAannType = m_vc.SegDefnFromFfFlid(choices[i].Flid);
							if (ffAannType == annType)
							{
								idx = i;
								break; // And that's where we want our selection!!
							}
							// Adjacent WSS of the same annotation count as only ONE object in the display.
							// So we advance i over as many items in m_choices as there are adjacent Wss
							// of the same flid.
							i += choices.AdjacentWssAtIndex(i).Length;
						}
						int[] rgws = choices.AdjacentWssAtIndex(idx);
						for (int i = 0; i < rgws.Length; ++i)
						{
							if (rgws[i] == wsField)
							{
								m_cpropPrevForInsert = i;
								break;
							}
						}
					}
				}
			}
		}

		private bool m_fInSelectionChanged; // true while executing SelectionChanged.
		private SelectionHelper m_setupPromptHelper;
		private int m_setupPromptFlid;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// <remarks>When overriding you should call the base class first.</remarks>
		/// -----------------------------------------------------------------------------------
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			if (m_fInSelectionChanged)
				return; // don't need to reprocess our own changes.
			m_fInSelectionChanged = true;
			try
			{
				base.HandleSelectionChange(prootb, vwselNew);
				IVwSelection sel = vwselNew;
				if (!sel.IsValid)
					sel = prootb.Selection;
				if (sel == null)
					return;
				SelectionHelper helper = SelectionHelper.Create(sel, prootb.Site);
				// Check whether the selection is on the proper line of a multilingual
				// annotation and, if not, fix it.  See LT-9421.
				if (m_cpropPrevForInsert > 0 && !sel.IsRange &&
					(helper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor) == 0 ||
					 helper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.End) == 0))
				{
					try
					{
						helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor, m_cpropPrevForInsert);
						helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.End, m_cpropPrevForInsert);
						helper.MakeBest(true);
						m_cpropPrevForInsert = -1;	// we've used this the one time it was needed.
					}
					catch (Exception exc)
					{
						if (exc != null)
							Debug.WriteLine(String.Format(
								"InterlinDocChild.SelectionChanged() trying to display prompt in proper line of annotation: {0}", exc.Message));
					}
				}
				int flid = helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
				//If the flid is -2 and it is an insertion point then we may have encountered a case where the selection has landed at the boundary between our (possibly empty)
				//translation field and a literal string containing our magic Bidi marker character that helps keep things in the right order.
				//Sometimes AssocPrev gets set so that we read the (non-existent) flid of the literal string and miss the fact that on the other side
				//of the insertion point is the field we're looking for. The following code will attempt to make a selection that associates in
				//the other direction to see if the flid we want is on the other side. [LT-10568]
				if (flid == -2 && !sel.IsRange && sel.SelType == VwSelType.kstText)
				{
					helper.AssocPrev = !helper.AssocPrev;
					try
					{
						var newSel = helper.MakeRangeSelection(this.RootBox, false);
						helper = SelectionHelper.Create(newSel, this);
						flid = helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
					}
					catch (COMException)
					{
						// Ignore HResult E_Fail caused by Extended Keys (PgUp/PgDown) in non-editable text (LT-13500)
					}
				}
				//Fixes LT-9884 Crash when clicking on the blank space in Text & Words--->Print view area!
				if (helper.LevelInfo.Length == 0)
					return;
				int hvo = helper.LevelInfo[0].hvo;

				// If the selection is in a freeform or literal translation that is empty, display the prompt.
				if (SelIsInEmptyTranslation(helper, flid, hvo) && !m_rootb.IsCompositionInProgress)
				{
					var handlerExtensions = Cache.ActionHandlerAccessor as IActionHandlerExtensions;
					if (handlerExtensions != null && handlerExtensions.IsUndoTaskActive)
					{
						// Wait to make the changes until the task (typically typing backspace) completes.
						m_setupPromptHelper = helper;
						m_setupPromptFlid = flid;
						handlerExtensions.DoAtEndOfPropChanged(handlerExtensions_PropChangedCompleted);
					}
					else
					{
						// No undo task to tag on the end of, so do it now.
						SetupTranslationPrompt(helper, flid);
					}
				}
				else if (flid != kTagUserPrompt)
				{
					m_vc.SetActiveFreeform(0, 0, 0, 0); // clear any current prompt.
				}
				// do not extend the selection for a user prompt if the user is currently entering an IME composition,
				// since we are about to switch the prompt to a real comment field
				else if (helper.GetTextPropId(SelectionHelper.SelLimitType.End) == SimpleRootSite.kTagUserPrompt
					&& !m_rootb.IsCompositionInProgress)
				{
					// If the selection is entirely in a user prompt then extend the selection to cover the
					// entire prompt. This covers changes within the prompt, like clicking within it or continuing
					// a drag while making it.
					sel.ExtendToStringBoundaries();
					EditingHelper.SetKeyboardForSelection(sel);
				}
			}
			finally
			{
				m_fInSelectionChanged = false;
			}
		}

		private void handlerExtensions_PropChangedCompleted()
		{
			SetupTranslationPrompt(m_setupPromptHelper, m_setupPromptFlid);
		}

		private void SetupTranslationPrompt(SelectionHelper helper, int flid)
		{
			IVwSelection sel;
			m_vc.SetActiveFreeform(helper.LevelInfo[0].hvo, flid, helper.Ws, helper.NumberOfPreviousProps);
			helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, kTagUserPrompt);
			helper.SetTextPropId(SelectionHelper.SelLimitType.End, kTagUserPrompt);
			helper.NumberOfPreviousProps = 0; // only ever one occurrence of prompt.
			helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.End, 0);
			// Even though the helper method is called MakeRangeSelection, it will initially make
			// an IP, because we haven't set any different offset for the end.
			// Since it's at the start of the prompt, we need it to associate with the prompt,
			// not the preceding (zero width direction-control) character.
			helper.AssocPrev = false;
			try
			{
				sel = helper.MakeRangeSelection(m_rootb, true);
				sel.ExtendToStringBoundaries();
			}
			// Prevent the crash described in LT-9399 by swallowing the exception.
			catch (Exception exc)
			{
				if (exc != null)
					Debug.WriteLine(String.Format(
						"InterlinDocChild.SelectionChanged() trying to display prompt for empty translation: {0}", exc.Message));
			}
		}

		private bool SelIsInEmptyTranslation(SelectionHelper helper, int flid, int hvo)
		{
			if (helper.IsRange)
				return false; // range can't be in empty comment.
			if (flid != SegmentTags.kflidFreeTranslation && flid != SegmentTags.kflidLiteralTranslation)
				return false; // translation is always a comment.
			if (helper.GetTss(SelectionHelper.SelLimitType.Anchor).Length != 0)
				return false; // translation is non-empty.
			return true;
		}

		#endregion

		#region FocusBox

		/// <summary>
		/// indicates whether the focus box exists and is in our controls.
		/// </summary>
		public bool IsFocusBoxInstalled
		{
			get { return ExistingFocusBox != null && this.Controls.Contains(FocusBox); }
		}

		/// <summary>
		/// Return focus box if it exists.
		/// </summary>
		private FocusBoxController ExistingFocusBox
		{
			get;
			set;
		}

		internal override void UpdateForNewLineChoices(InterlinLineChoices newChoices)
		{
			base.UpdateForNewLineChoices(newChoices);
			if (ExistingFocusBox != null)
			{
				ExistingFocusBox.UpdateLineChoices(newChoices);
				if (IsFocusBoxInstalled)
					MoveFocusBoxIntoPlace();
			}
		}

		/// <summary>
		/// returns the focus box for the interlinDoc if it exists or can be created.
		/// </summary>
		internal FocusBoxController FocusBox
		{
			get
			{
				if (ExistingFocusBox == null && ForEditing)
				{
					CreateFocusBox();
				}
				return ExistingFocusBox;
			}
			set
			{
				ExistingFocusBox = value;
			}
		}

		internal override void CreateFocusBox()
		{
			ExistingFocusBox = CreateFocusBoxInternal();
		}

		protected virtual FocusBoxController CreateFocusBoxInternal()
		{
			return new FocusBoxControllerForDisplay(m_mediator, m_propertyTable, m_styleSheet, LineChoices, m_vc.RightToLeft);
		}

		/// <summary>
		/// Hides the sandbox and removes it from the controls.
		/// </summary>
		/// <returns>true, if it could hide the sandbox. false, if it was not installed.</returns>
		internal override bool TryHideFocusBoxAndUninstall()
		{
			if (!IsFocusBoxInstalled)
			{
				SelectedOccurrence = null;
				return false;
			}
			var oldAnnotation = SelectedOccurrence;
			SelectedOccurrence = null;
			SimulateReplaceAnalysis(oldAnnotation);
			bool fFocus = this.Focused || ExistingFocusBox.ContainsFocus;
			FocusBox.SizeChanged -= FocusBox_SizeChanged;
			ExistingFocusBox.SuspendLayout();
			ExistingFocusBox.Visible = false;
			this.SuspendLayout();
			this.Controls.Remove(ExistingFocusBox);
			this.ResumeLayout();
			ExistingFocusBox.ResumeLayout();
			// hiding the ExistingFocusBox can sometimes leave the focus on one of its controls,
			// believe it or not!  (See FWR-3188.)
			if (fFocus && !this.Focused)
				this.Focus();
			return true;
		}

		/// <summary>
		/// Adds the sandbox to the control and makes it visible.
		/// </summary>
		/// <returns>true, if we made the sandbox visible, false, if we couldn't.</returns>
		bool TryShowFocusBox()
		{
			Debug.Assert(FocusBox != null, "make sure sandbox is setup before trying to show it.");
			if (FocusBox == null)
				return false;
			InstallFocusBox();
			FocusBox.Visible = true;
			// Refresh seems to prevent the sandbox from blanking out (LT-9922)
			FocusBox.Refresh();
			return true;
		}

		protected void InstallFocusBox()
		{
			if (!Controls.Contains(FocusBox))
			{
				Controls.Add(FocusBox); // Makes it real and gives it a root box.
				FocusBox.SizeChanged += FocusBox_SizeChanged;
			}
		}

		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			Debug.WriteLine("scrolled interlinear view to " + AutoScrollPosition + " in range " + AutoScrollMinSize + " (focus box at " + FocusBox.Location + ")");
		}

		// If something changes the size of the focus box, we need to adjust the size of the
		// box that takes up space for it in the view, so that other stuff moves.
		void FocusBox_SizeChanged(object sender, EventArgs e)
		{
			if (SetFocusBoxSizeForVc())
				m_rootb.PropChanged(FocusBox.SelectedOccurrence.Segment.Hvo, SegmentTags.kflidAnalyses, FocusBox.SelectedOccurrence.Index, 1, 1);
		}

		private bool m_fEnableScrollControlIntoView;

		/// <summary>
		/// Windows.Forms is way too enthusiastic about trying to make the focused child control visible.
		/// For example it does it any time we change AutoScrollMinSize, such as when scrolling up and expanding
		/// lazy boxes. This has bad effects (LT-LT-11692). Returning the control's current location prevents
		/// ScrollControlIntoView from making any changes. However, in some cases we may want to make it visible.
		/// </summary>
		/// <param name="activeControl"></param>
		/// <returns></returns>
		override protected Point ScrollToControl(Control activeControl)
		{
			if (m_fEnableScrollControlIntoView)
				return base.ScrollToControl(activeControl);
			else
				return DisplayRectangle.Location; // various posts say to use this to disable autoscrolling the focus control
		}

		internal void ReallyScrollControlIntoView(Control c)
		{
			m_fEnableScrollControlIntoView = true;
			ScrollControlIntoView(c);
			m_fEnableScrollControlIntoView = false;
		}


		#endregion

		#region UserSelection

		/// <summary>
		/// don't try to make a default cursor selection, since
		/// that's what setting the FocusBox is typically for.
		/// </summary>
		public override bool WantInitialSelection
		{
			get { return false; }
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			// The base method does this too, but some paths in this method don't go through the base!
			RemoveContextButtonIfPresent();
			if (e.Button == MouseButtons.Right)
			{
				base.OnMouseDown(e);
				return;
			}

			if (m_rootb == null || DataUpdateMonitor.IsUpdateInProgress())
				return;

			// Convert to box coords and see what selection it produces.
			Point pt;
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				pt = PixelToView(new Point(e.X, e.Y));
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
#if __MonoCS__
				// Adjust the destination to the original scroll position.  This completes
				// the fix for FWNX-794/851.
				rcDstRoot.Location = m_ptScrollPos;
#endif
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (sel == null || !HandleClickSelection(sel, false, false))
					base.OnMouseDown(e);
			}
		}

#if __MonoCS__
		/// <summary>
		/// The Mono runtime changes the scroll position to the currently existing control
		/// before passing on to the OnMouseDown method.  This works fine for statically defined
		/// controls, as the static control appears to be selected before the scrolling occurs.
		/// However, when the desired control doesn't exist yet, we end up with FWNX-794 (aka
		/// FWNX-851) because the old Focus Box is the only control available to scroll to.  If
		/// the internal variable auto_select_child in ContainerControl were protected instead
		/// of internal, setting it to false in our constructor would be enough to block this
		/// unwanted scrolling.  But that is rather implementation dependent. (But then, so is
		/// this fix!)
		/// </summary>
		/// <remarks>
		/// This bug has been reported as https://bugzilla.xamarin.com/show_bug.cgi?id=4969, so
		/// this fix can be removed after that bug has been fixed in the version of Mono used
		/// to compile FieldWorks.
		/// </remarks>
		private Point m_ptScrollPos;
#endif

		public override void OriginalWndProc(ref Message msg)
		{
#if __MonoCS__
			// When handling a left mouse button down event, save the original scroll position.
			if (msg.Msg == (int)Win32.WinMsgs.WM_LBUTTONDOWN)
				m_ptScrollPos = AutoScrollPosition;
#endif
			base.OriginalWndProc(ref msg);
		}

		/// <summary>
		/// Handles a view selection produced by a click. Return true to suppress normal
		/// mouse down handling, indicating that an interlinear bundle has been clicked and the Sandbox
		/// moved.
		/// </summary>
		/// <param name="vwselNew"></param>
		/// <param name="fBundleOnly"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <returns></returns>
		protected virtual bool HandleClickSelection(IVwSelection vwselNew, bool fBundleOnly, bool fSaveGuess)
		{
			if (vwselNew == null)
				return false; // couldn't select a bundle!
			// The basic idea is to find the level at which we are displaying the TagAnalysis property.
			int cvsli = vwselNew.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.

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
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

			if (tagTextProp == SegmentTags.kflidFreeTranslation || tagTextProp == SegmentTags.kflidLiteralTranslation
				|| tagTextProp == NoteTags.kflidContent)
			{
				bool fWasFocusBoxInstalled = IsFocusBoxInstalled;
				Rect oldSelLoc = GetPrimarySelRect(vwselNew);
				if (!fBundleOnly)
				{
					if (IsFocusBoxInstalled)
						FocusBox.UpdateRealFromSandbox(null, fSaveGuess, null);
					TryHideFocusBoxAndUninstall();
				}

				// If the selection resulting from the click is still valid, and we just closed the focus box, go ahead and install it;
				// continuing to process the click may not produce the intended result, because
				// removing the focus box can re-arrange things substantially (LT-9220).
				// (However, if we didn't change anything it is necesary to process it normally, otherwise, dragging
				// and shift-clicking in the free translation don't work.)
				if (!vwselNew.IsValid || !fWasFocusBoxInstalled)
					return false;
				// We have destroyed a focus box...but we may not have moved the free translation we clicked enough
				// to cause problems. If not, we'd rather do a normal click, because installing a selection that
				// the root box doesn't think is from mouse down does not allow dragging.
				Rect selLoc = GetPrimarySelRect(vwselNew);
				if (selLoc.top == oldSelLoc.top)
					return false;
				//The following line could quite possibly invalidate the selection as in the case where it creates
				//a translation prompt.
				vwselNew.Install();
				//scroll the current selection into view (don't use vwselNew, it might be invalid now)
				ScrollSelectionIntoView(this.RootBox.Selection, VwScrollSelOpts.kssoDefault);
				return true;
			}

			// Identify the analysis, and the position in m_rgvsli of the property holding it.
			// It is also possible that the analysis is the root object.
			// This is important because although we are currently displaying just an StTxtPara,
			// eventually it might be part of a higher level structure. We want to be able to
			// reproduce everything that gets us down to the analysis.
			int itagAnalysis = -1;
			for (int i = rgvsli.Length; --i >= 0; )
			{
				if (rgvsli[i].tag == SegmentTags.kflidAnalyses)
				{
					itagAnalysis = i;
					break;
				}
			}
			if (itagAnalysis < 0)
			{
				if (!fBundleOnly)
				{
					if (IsFocusBoxInstalled)
						FocusBox.UpdateRealFromSandbox(null, fSaveGuess, null);
					TryHideFocusBoxAndUninstall();
				}

				return false; // Selection is somewhere we can't handle.
			}
			int ianalysis = rgvsli[itagAnalysis].ihvo;
			Debug.Assert(itagAnalysis < rgvsli.Length - 1); // Need different approach if the analysis is the root.
			int hvoSeg = rgvsli[itagAnalysis + 1].hvo;
			var seg = Cache.ServiceLocator.GetObject(hvoSeg) as ISegment;
			Debug.Assert(seg != null);
			// If the mouse click lands on a punctuation form, move to the preceding
			// wordform (if any).  See FWR-815.
			while (seg.AnalysesRS[ianalysis] is IPunctuationForm && ianalysis > 0)
				--ianalysis;
			if (ianalysis == 0 && seg.AnalysesRS[0] is IPunctuationForm)
			{
				if (!fBundleOnly)
					TryHideFocusBoxAndUninstall();
				return false;
			}
			TriggerAnnotationSelected(new AnalysisOccurrence(seg, ianalysis), fSaveGuess);
			return true;
		}

		#endregion

		#region IxCoreColleague

		public override IxCoreColleague[] GetMessageTargets()
		{
			if (IsFocusBoxInstalled && FocusBox is FocusBoxControllerForDisplay)
				return new IxCoreColleague[] { (FocusBox as FocusBoxControllerForDisplay), this };
			return base.GetMessageTargets();
		}

		#endregion

		public void AddNote(Command command)
		{
			IVwSelection sel = MakeSandboxSel();
			// If there's no sandbox selection, there may be one in the site itself, perhaps in another
			// free translation.
			if (sel == null && RootBox != null)
				sel = RootBox.Selection;
			if (sel == null)
				return; // Enhance JohnT: give an error, or disable the command.
			int cvsli = sel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.

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
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

			// Identify the segment.
			// This is important because although we are currently displaying just an StTxtPara,
			// eventually it might be part of a higher level structure. We want this to work
			// no matter how much higher level structure there is.
			int itagSegments = -1;
			for (int i = rgvsli.Length; --i >= 0; )
			{
				if (rgvsli[i].tag == StTxtParaTags.kflidSegments)
				{
					itagSegments = i;
					break;
				}
			}
			if (itagSegments == -1)
				return; // Enhance JohnT: throw? disable command? Give an error?

			int hvoSeg = rgvsli[itagSegments].hvo;
			var seg = Cache.ServiceLocator.GetObject(hvoSeg) as ISegment;
			UndoableUnitOfWorkHelper.Do(command.UndoText, command.RedoText, Cache.ActionHandlerAccessor,
				() =>
				{
					var note = Cache.ServiceLocator.GetInstance<INoteFactory>().Create();
					seg.NotesOS.Add(note);
				});

			TryHideFocusBoxAndUninstall();
			if (m_vc.LineChoices.IndexOf(InterlinLineChoices.kflidNote) < 0)
			{
				m_vc.LineChoices.Add(InterlinLineChoices.kflidNote);
				PersistAndDisplayChangedLineChoices();
			}

			// Now try to make a new selection in the note we just made.
			// The elements of rgvsli from itagSegments onwards form a path to the segment.
			// In the segment we want the note propery, specifically the new one we just made.
			// We want to select at the start of it.
			// LT-12613: We're adding an extra segment here:
			SelLevInfo[] rgvsliNew = new SelLevInfo[rgvsli.Length - itagSegments + 2];
			for (int i = 2; i < rgvsliNew.Length; i++)
				rgvsliNew[i] = rgvsli[i + itagSegments - 2];
			rgvsliNew[0].ihvo = seg.NotesOS.Count - 1;
			rgvsliNew[0].tag = SegmentTags.kflidNotes;
			rgvsliNew[0].cpropPrevious = 0;
			// LT-12613: Define extra segment here:
			rgvsliNew[1].ihvo = 0;
			rgvsliNew[1].tag = Cache.MetaDataCacheAccessor.GetFieldId2(CmObjectTags.kClassId, "Self", false);
			rgvsliNew[1].cpropPrevious = 0;
			RootBox.MakeTextSelInObj(0, rgvsliNew.Length, rgvsliNew, 0, null, true, true, false, false, true);
			// Don't steal the focus from another window.  See FWR-1795.
			if (ParentForm == Form.ActiveForm)
				Focus(); // So we can actually see the selection we just made.
		}

		internal void RecordGuessIfNotKnown(AnalysisOccurrence selected)
		{
			if (m_vc != null) // I think this only happens in tests.
				m_vc.RecordGuessIfNotKnown(selected);
		}

		internal IAnalysis GetGuessForWordform(IWfiWordform wf, int ws)
		{
			if (m_vc != null)
				return m_vc.GetGuessForWordform(wf, ws);
			return null;
		}

		internal bool PrepareToGoAway()
		{
			if (IsFocusBoxInstalled)
				FocusBox.UpdateRealFromSandbox(null, false, null);
			return true;
		}

		public bool OnApproveAll(object cmd)
		{
			ApproveAllSuggestedAnalyses(cmd as Command);
			return false;
		}

		/// <summary>
		/// Approve all the suggested analyses in this text. See LT-4312.
		/// </summary>
		/// <param name="cmd">The command object from the selection event.</param>
		public void ApproveAllSuggestedAnalyses(Command cmd)
		{   // Go through the entire text looking for suggested analyses that can be approved.
			// remember where the focus box or ip is
			// might be on an analysis, labels or translation text
			var helper = SelectionHelper.Create(RootBox.Site); // only helps restore translation and note line selections
			AnalysisOccurrence focusedWf = SelectedOccurrence; // need to restore focus box if selected

			// find the very first analysis
			ISegment firstRealSeg = null;
			IAnalysis firstRealOcc = null;
			int occInd = 0;
			foreach (IStPara p in RootStText.ParagraphsOS)
			{
				var para = (IStTxtPara) p;
				foreach (ISegment seg in para.SegmentsOS)
				{
					firstRealSeg = seg;
					occInd = 0;
					foreach(IAnalysis an in seg.AnalysesRS)
					{
						if (an.HasWordform && an.IsValidObject)
						{
							firstRealOcc = an;
							break;
						}
						occInd++;
					}
					if (firstRealOcc != null) break;
				}
				if (firstRealOcc != null) break;
			}
			// Set it as the current segment and recurse
			if (firstRealOcc == null)
				return; // punctuation only or nothing to analyze
			AnalysisOccurrence ao = null;
			if (focusedWf != null && focusedWf.Analysis == firstRealOcc)
				ao = new AnalysisOccurrence(focusedWf.Segment, focusedWf.Index);
			else
				ao = new AnalysisOccurrence(firstRealSeg, occInd);
			TriggerAnalysisSelected(ao, true, true, false);
			var navigator = new SegmentServices.StTextAnnotationNavigator(ao);

			// This needs to be outside the block for the UOW, since what we are suppressing
			// happens at the completion of the UOW.
			SuppressResettingGuesses(
				() =>
				{
					// Needs to include GetRealAnalysis, since it might create a new one.
					UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, Cache.ActionHandlerAccessor,
					() =>
					{
						var nav = new SegmentServices.StTextAnnotationNavigator(SelectedOccurrence);
						AnalysisOccurrence lastOccurrence;
						var analyses = navigator.GetAnalysisOccurrencesAdvancingInStText().ToList();
						foreach (var occ in analyses)
						{   // This could be punctuation or any kind of analysis.
							IAnalysis occAn = occ.Analysis; // averts “Access to the modified closure” warning in resharper
							if (occAn is IWfiAnalysis || occAn is IWfiWordform)
							{   // this is an analysis or a wordform
								int hvo = m_vc.GetGuess(occAn);
								if (occAn.Hvo != hvo)
								{   // this is a guess, so approve it
									// 1) A second occurence of a word that has had a lexicon entry or sense created for it.
									// 2) A parser result - not sure which gets picked if multiple.
									// #2 May take a while to "percolate" through to become a "guess".
									var guess = Cache.ServiceLocator.ObjectRepository.GetObject(hvo);
									if (guess != null && guess is IAnalysis)
										occ.Segment.AnalysesRS[occ.Index] = (IAnalysis) guess;
									else
									{
										occ.Segment.AnalysesRS[occ.Index] = occAn.Wordform.AnalysesOC.FirstOrDefault();
									}
								}
							/*	else if (occAn.HasWordform && occAn.Wordform.ParserCount > 0)
								{   // this doesn't seem to be needed (and may not be correct) - always caught above
									bool isHumanNoOpinion = occAn.Wordform.HumanNoOpinionParses.Cast<IWfiWordform>().Any(wf => wf.Hvo == occAn.Hvo);
									if (isHumanNoOpinion)
									{
										occ.Segment.AnalysesRS[occ.Index] = occAn.Wordform.AnalysesOC.FirstOrDefault();
									}
								} */
							}
						}
					});
				}
			);
			// MoveFocusBoxIntoPlace();
			if (focusedWf != null)
				SelectOccurrence(focusedWf);
			else if (helper != null)
				helper.SetSelection(true, true);
			Update();
		}
	}

	public class InterlinDocForAnalysisVc : InterlinVc
	{
		public InterlinDocForAnalysisVc(FdoCache cache)
			: base(cache)
		{
			FocusBoxSize = new Size(100000, 50000); // If FocusBoxAnnotation is set, this gives the size of box to make. (millipoints)
		}

		AnalysisOccurrence m_focusBoxOccurrence;
		/// <summary>
		/// Set the annotation that is displayed as a fix-size box on top of which the SandBox is overlayed.
		/// Client must also do PropChanged to produce visual effect.
		/// Size is in millipoints!
		/// </summary>
		/// <remarks>This can become invalid if the user deletes some text.  See FWR-3003.</remarks>
		internal AnalysisOccurrence FocusBoxOccurrence
		{
			get
			{
				if (m_focusBoxOccurrence != null && m_focusBoxOccurrence.IsValid)
					return m_focusBoxOccurrence;
				m_focusBoxOccurrence = null;
				return null;
			}
			set { m_focusBoxOccurrence = value; }
		}

		/// <summary>
		/// Set the size of the space reserved for the Sandbox. Client must also do a Propchanged to trigger
		/// visual effect.
		/// </summary>
		internal Size FocusBoxSize
		{
			get;
			set;
		}

		protected override void AddWordBundleInternal(int hvo, IVwEnv vwenv)
		{
			// Determine whether it is the focus box occurrence.
			if (FocusBoxOccurrence != null)
			{
				int hvoSeg, tag, ihvo;
				vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoSeg, out tag, out ihvo);
				if (hvoSeg == FocusBoxOccurrence.Segment.Hvo && ihvo == FocusBoxOccurrence.Index)
				{
					// Leave room for the Sandbox instead of displaying the internlinear data.
					// The first argument makes it invisible in case a little bit of it shows around
					// the sandbox.
					// The last argument puts the 'Baseline' of the sandbox (which aligns with the base of the
					// first line of text) an appropriate distance from the top of the Sandbox. This aligns it's
					// top line of text properly.
					// Enhance JohnT: 90% of font height is not always exactly right, but it's the closest
					// I can get wihtout a new API to get the exact ascent of the font.
					int dympBaseline = Common.Widgets.FontHeightAdjuster.
						GetFontHeightForStyle("Normal", m_stylesheet, m_wsVernForDisplay,
						m_cache.LanguageWritingSystemFactoryAccessor) * 9 / 10;
					uint transparent = 0xC0000000; // FwTextColor.kclrTransparent won't convert to uint
					vwenv.AddSimpleRect((int)transparent,
										FocusBoxSize.Width, FocusBoxSize.Height, -(FocusBoxSize.Height - dympBaseline));
					return;
				}
			}
			base.AddWordBundleInternal(hvo, vwenv);
		}

		/// <summary>
		/// The only property we update is a user prompt. We need to switch things back to normal if
		/// anything was typed there, otherwise, the string has the wrong properties, and with all of it
		/// selected, we keep typing over things.
		/// </summary>
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			if (tag != SimpleRootSite.kTagUserPrompt)
				return tssVal;

			// wait until an IME composition is completed before switching the user prompt to a comment
			// field, otherwise setting the comment will terminate the composition (LT-9929)
			if (RootSite.RootBox.IsCompositionInProgress)
				return tssVal;

			if (tssVal.Length == 0)
			{
				// User typed something (return?) which didn't actually put any text over the prompt.
				// No good replacing it because we'll just get the prompt string back and won't be
				// able to make our new selection.
				return tssVal;
			}

			// Get information about current selection
			SelectionHelper helper = SelectionHelper.Create(vwsel, RootSite);

			var seg = (ISegment)m_coRepository.GetObject(hvo);

			ITsStrBldr bldr = tssVal.GetBldr();
			// Clear special prompt properties
			bldr.SetIntPropValues(0, bldr.Length, SimpleRootSite.ktptUserPrompt, -1, -1);
			bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptSpellCheck, -1, -1);

			// Add the text the user just typed to the translatin - this destroys the selection
			// because we replace the user prompt. We use the frag to note the WS of interest.
			RootSite.RootBox.DataAccess.SetMultiStringAlt(seg.Hvo, ActiveFreeformFlid, frag, bldr.GetString());

			// arrange to restore the selection (in the new property) at the end of the UOW (when the
			// required property will have been re-established by various PropChanged calls).
			RootSite.RequestSelectionAtEndOfUow(RootSite.RootBox, 0, helper.LevelInfo.Length, helper.LevelInfo, ActiveFreeformFlid,
				m_cpropActiveFreeform, helper.IchAnchor, helper.Ws, helper.AssocPrev,
				helper.GetSelProps(SelectionHelper.SelLimitType.Anchor));
			SetActiveFreeform(0, 0, 0, 0); // AFTER request selection, since it clears ActiveFreeformFlid.
			return tssVal;
		}
	}
}
