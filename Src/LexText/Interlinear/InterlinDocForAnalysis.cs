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
			if(CanDeleteNote(e.Selection, out hvoNote))
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
			if (IsFocusBoxInstalled && tag == SegmentTags.kflidAnalyses && FocusBox.SelectedOccurrence != null
				&& FocusBox.SelectedOccurrence.Segment.Hvo == hvo)
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
			var segment = (ISegment) note.Owner;
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
		/// see: InterlinDocChild.DestroyFocusBoxAndSetFocus()
		/// </summary>
		private bool m_fSuppressLoseFocus = false;
		protected override void OnLostFocus(EventArgs e)
		{
			if (!m_fSuppressLoseFocus) // suppresses events while focusing self.
			{
				if (m_vc != null)
					m_vc.SetActiveFreeform(0, 0, 0, 0);
			}
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
			SimulateReplaceAnalysis(target);
			MoveFocusBoxIntoPlace();
			// Now it is the right size and place we can show it.
			TryShowFocusBox();
			// All this CAN hapen because we're editing in another window...for example,
			// if we edit something that deletes the current wordform in a concordance view.
			// In that case we don't want to steal the focus.
			if (ParentForm == Form.ActiveForm)
				FocusBox.Focus();

			if (fMakeDefaultSelection)
				m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, FocusBox.MakeDefaultSelection);
			//}
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
				this.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
				Update();
				if (sel == null)
				{
					Debug.WriteLine("could not select annotation");
					return;
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

		// Get the primary rectangle occupied by a selection (relative to the top left of the client rectangle).
		private Rect GetPrimarySelRect(IVwSelection sel)
		{
			Rect rcPrimary;
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot, rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				Rect rcSec;
				bool fSplit, fEndBeforeAnchor;
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
							 out rcSec, out fSplit, out fEndBeforeAnchor);
			}
			return rcPrimary;
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
				return (m_vc as InterlinDocForAnalysisVc).FocusBoxOccurrence;
			}
			set
			{
				(m_vc as InterlinDocForAnalysisVc).FocusBoxOccurrence = value;
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
			return m_mediator.PropertyTable.GetBoolProperty(InterlinDocForAnalysis.ksPropertyAddWordsToLexicon, false) ?
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
			// LT-9570 for the Tree Translation line, Susanna wanted Enter to copy Word Glosses
			// into the Free Translation line. Note: DotNetBar is not handling shortcut="Enter"
			// for the XML <command id="CmdAddWordGlossesToFreeTrans"...
			if (RootBox != null && e.KeyCode == Keys.Enter)
				OnAddWordGlossesToFreeTrans(null);
			base.OnKeyDown(e);
		}

		/// <summary>
		/// Enable the 'insert word glosses' command
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
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
		/// <param name="hvoSeg"></param>
		/// <returns></returns>
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
			ITsString space = StringUtils.MakeTss(" ", ws);
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
							StringUtils.TurnOwnedOrcIntoUnownedOrc(puncBldr, 0);
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
						wsField = StringUtils.GetWsAtOffset(tsi.TssAnchor, 0);
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
						handlerExtensions.PropChangedCompleted += handlerExtensions_PropChangedCompleted;
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

		private void handlerExtensions_PropChangedCompleted(object sender, bool fromUndoRedo)
		{
			// Only do it once!
			((IActionHandlerExtensions)(Cache.ActionHandlerAccessor)).PropChangedCompleted -= handlerExtensions_PropChangedCompleted;
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
			return new FocusBoxControllerForDisplay(m_mediator, m_styleSheet, LineChoices, m_vc.RightToLeft);
		}


		/// <summary>
		/// Hides the sandbox and removes it from the controls.
		/// </summary>
		/// <returns>true, if it could hide the sandbox. false, if it was not installed.</returns>
		internal override bool TryHideFocusBoxAndUninstall()
		{
			if (!IsFocusBoxInstalled)
				return false;
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

		// If something changes the size of the focus box, we need to adjust the size of the
		// box that takes up space for it in the view, so that other stuff moves.
		void FocusBox_SizeChanged(object sender, EventArgs e)
		{
			if (SetFocusBoxSizeForVc())
				m_rootb.PropChanged(FocusBox.SelectedOccurrence.Segment.Hvo, SegmentTags.kflidAnalyses, FocusBox.SelectedOccurrence.Index, 1, 1);
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
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (sel == null || !HandleClickSelection(sel, false, false))
					base.OnMouseDown(e);
			}
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
				vwselNew.Install();
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
			for (int i = rgvsli.Length; --i>=0; )
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
			SelLevInfo[] rgvsliNew = new SelLevInfo[rgvsli.Length - itagSegments + 1];
			for (int i = 1; i < rgvsliNew.Length; i++)
				rgvsliNew[i] = rgvsli[i + itagSegments - 1];
			rgvsliNew[0].ihvo = seg.NotesOS.Count - 1;
			rgvsliNew[0].tag = SegmentTags.kflidNotes;
			rgvsliNew[0].cpropPrevious = 0;
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
	}

	public class InterlinDocForAnalysisVc : InterlinVc
	{
		public InterlinDocForAnalysisVc(FdoCache cache) : base(cache)
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
			get; set;
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
						m_cache.LanguageWritingSystemFactoryAccessor)*9/10;
					uint transparent = 0xC0000000; // FwTextColor.kclrTransparent won't convert to uint
					vwenv.AddSimpleRect((int) transparent,
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
