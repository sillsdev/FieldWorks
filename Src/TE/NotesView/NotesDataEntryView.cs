// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2005' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NotesDataEntryView.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NotesDataEntryView displays Scripture annotations as a tree with lots of editable stuff.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NotesDataEntryView : FwRootSite, ISelectableView
	{
		#region Data members
		private TeNotesVc m_vc;
		private FwMainWnd m_notesMainWnd;
		private string m_baseInfoBarCaption;
		private IScripture m_scr;
		private bool m_pictureSelected; // indicates whether a picture was selected on mouse down
		private IUserView m_UserView;
		private int m_prevNoteHvo;
		private int m_currentNotesTag;
		private bool m_fIgnoreTmStmpUpdate;
		private bool m_fSendSyncScrollMsg = true;
		private bool m_ignoreSelChanged;

		/// <summary>Delegate to indicate a change in the filter</summary>
		public delegate void FilterChangedHandler(object sender, CmFilter filter);

		/// <summary>Event to indicate a change in the filter</summary>
		public event FilterChangedHandler FilterChanged;
		#endregion

		#region Constructor and Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NotesDataEntryView"/> class.
		/// </summary>
		/// <param name="cache">Duh</param>
		/// <param name="userView">The UserView that this view displays</param>
		/// <param name="notesWnd">The notes window.</param>
		/// ------------------------------------------------------------------------------------
		public NotesDataEntryView(FdoCache cache, IUserView userView, FwMainWnd notesWnd) : base(cache)
		{
			m_scr = Cache.LangProject.TranslatedScriptureOA;
			m_UserView = userView;
			m_notesMainWnd = notesWnd;
			BaseInfoBarCaption = userView.ViewNameShort;
			ReadOnlyView = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			base.Dispose (disposing);

			if (disposing)
			{
				if (m_vc != null)
					m_vc.Dispose();
			}
			m_vc = null;
			m_baseInfoBarCaption = null;
			m_scr = null;
			m_UserView = null;
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether or not to ignore updates to the notes time stamp (i.e. ignore calls
		/// to the SetNoteUpdateTime() method)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IgnoreTimeStampUpdates
		{
			get
			{
				CheckDisposed();
				return m_fIgnoreTmStmpUpdate;
			}
			set
			{
				CheckDisposed();
				m_fIgnoreTmStmpUpdate = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not a synchronizing scrolling message
		/// is sent when the selection changes to a different annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool SendSyncScrollingMsgs
		{
			get { return m_fSendSyncScrollMsg; }
			set { m_fSendSyncScrollMsg = value; }
		}

		#endregion

		#region Overriden methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the Display :-)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void RefreshDisplay()
		{
			base.RefreshDisplay();
			if (m_vc != null && m_vc.NotesSequenceHandler != null)
				m_vc.NotesSequenceHandler.Reinitialize(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_editingHelper == null)
					m_editingHelper = new NotesEditingHelper(m_fdoCache, this, 0);
				return m_editingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the notes editing helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public NotesEditingHelper NotesEditingHelper
		{
			get { CheckDisposed(); return (NotesEditingHelper)m_editingHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the annotation in which is the current selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrScriptureNote CurrentAnnotation
		{
			get
			{
				if (EditingHelper == null || EditingHelper.CurrentSelection == null)
				{
					// No currently selected annotation, but we can assume that the previously
					// selected annotation (the one in the VC) is still the one the user thinks
					// is selected. (TE-8889)
					if (m_vc.SelectedNoteHvo > 0 && m_fdoCache.GetClassOfObject(m_vc.SelectedNoteHvo) == ScrScriptureNote.kClassId)
						return new ScrScriptureNote(m_fdoCache, m_vc.SelectedNoteHvo);
					return null;
				}

				try
				{
					SelLevInfo info = EditingHelper.CurrentSelection.GetLevelInfoForTag(m_currentNotesTag);
					return new ScrScriptureNote(m_fdoCache, info.hvo);
				}
				catch{}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the annotation if it isn't already.
		/// </summary>
		/// <param name="annHvo">The id of the annotation.</param>
		/// ------------------------------------------------------------------------------------
		internal void ExpandAnnotationIfNeeded(int annHvo)
		{
			if (!m_vc.IsExpanded(annHvo))
				m_vc.ExpandItem(annHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collapses all annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void CollapseAllAnnotations()
		{
			m_vc.CollapseAllAnnotations(m_rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the given flid is one that this view filters and/or sorts, return the
		/// corresponding virtual tag.
		/// It is "safe" to just use the flid if you're absolutely sure this view isn't using a
		/// virtual property to filter/sort it, but using this method is much safer.
		/// </summary>
		/// <param name="flid">The field identifier</param>
		/// <returns>The flid itself or a virtual tag that corresponds to it</returns>
		/// ------------------------------------------------------------------------------------
		public override int GetVirtualTagForFlid(int flid)
		{
			CheckDisposed();

			if (flid == (int)ScrBookAnnotations.ScrBookAnnotationsTags.kflidNotes)
				return m_currentNotesTag;
			return base.GetVirtualTagForFlid(flid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a problem deletion. No problem deletions currently handled, beep and
		/// return Abort so that default behavior is not tried.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="dpt"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();

			MiscUtils.ErrorBeep();
			return VwDelProbResponse.kdprAbort;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the root
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode)
				return;

			// Check for non-existing rootbox before creating a new one. By doing that we
			// can mock the rootbox in our tests. However, this might cause problems in our
			// real code - altough I think MakeRoot() should be called only once.
			if (m_rootb == null)
				m_rootb = VwRootBoxClass.Create();

			m_rootb.SetSite(this);

			HorizMargin = 10;

			// Set up a new view constructor.
			Debug.Assert(IsHandleCreated);
			m_vc = new TeNotesVc(m_fdoCache, Zoom);
			OnChangeFilter(null);

//			vc.HotLinkClick += new DraftViewVc.HotLinkClickHandler(DoHotLinkAction);

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			//EditingHelper.RootObjects = new int[]{ScriptureObj.Hvo};
			m_rootb.SetRootObject(m_scr.Hvo, m_vc, (int)ScrFrags.kfrScripture, m_styleSheet);

			base.MakeRoot();
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			Synchronize(m_rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle Mouse up
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right)
			{
				base.OnMouseUp(e);
				return;
			}

			NotesMainWnd notesMainWnd = TheMainWnd as NotesMainWnd;
			if (notesMainWnd == null || notesMainWnd.TMAdapter == null)
				return;

			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			GetCoordRects(out rcSrcRoot, out rcDstRoot);
			Point pt = new Point(e.X, e.Y);
			IVwSelection sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			int noteHvo = 0;
			if (sel != null)
			{
				SelectionHelper selHelper = SelectionHelper.Create(sel, this);
				SelLevInfo annInfo = selHelper.GetLevelInfoForTag(m_currentNotesTag);
				noteHvo = annInfo.hvo;
			}

			pt = PointToScreen(new Point(e.X, e.Y));
			notesMainWnd.TMAdapter.PopupMenu("cmnuNotesDataEntryView", pt.X, pt.Y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remember if a picture is selected so that it can be deselected on mouse up,
		/// if necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CallMouseDown(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			m_pictureSelected = (sel != null && sel.SelType == VwSelType.kstPicture);
			base.CallMouseDown (pt, rcSrcRoot, rcDstRoot);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseUp on the rootbox
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void CallMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			ScrScriptureNote ann = CurrentAnnotation;

			ScrScriptureNote.ScrScriptureNoteTags noteTagToSelect =
				ScrScriptureNote.ScrScriptureNoteTags.kflidDiscussion;

			bool fMakeSelInFirstResponse = false;

			if (sel != null && sel.SelType == VwSelType.kstPicture)
			{
				SelectionHelper selHelper = SelectionHelper.Create(sel, this);
				SelLevInfo[] info = selHelper.LevelInfo;

				if (info.Length >= 1 &&
					info[0].tag == (int)ScrScriptureNote.ScrScriptureNoteTags.kflidDiscussion ||
					info[0].tag == (int)ScrScriptureNote.ScrScriptureNoteTags.kflidRecommendation ||
					m_fdoCache.GetClassOfObject(info[0].hvo) == StJournalText.kClassId)
				{
					bool fExpanding = m_vc.ToggleItemExpansion(info[0].hvo, m_rootb);
					if (fExpanding)
					{
						// If  the tag is not a valid tag, the assumption
						// is we're expanding the responses.
						fMakeSelInFirstResponse = (info[0].tag < 0);
						noteTagToSelect = (ScrScriptureNote.ScrScriptureNoteTags)info[0].tag;
					}
				}
				else if (selHelper.GetTextPropId(SelectionHelper.SelLimitType.Anchor) ==
					-(int)NotesFrags.kfrConnotCategory) // This is not a real flid, just a unique number to match on.
				{
					SetAnnotationCategory(ann);
				}
				else if (info.Length >= 2 && info[1].tag == m_currentNotesTag)
				{
					m_ignoreSelChanged = !m_vc.ToggleItemExpansion(info[1].hvo, m_rootb);
				}
			}

			base.CallMouseUp(pt, rcSrcRoot, rcDstRoot);
			m_ignoreSelChanged = false;

			if (!m_pictureSelected)
				return;

			m_pictureSelected = false;

			if (ann == null || m_vc.NotesSequenceHandler == null)
				return;

			// Make a selection in the proper place in the annotation.
			int book = BCVRef.GetBookFromBcv(ann.BeginRef) - 1;
			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[book];
			int index = m_vc.NotesSequenceHandler.GetVirtualIndex(annotations.Hvo, ann.IndexInOwner);

			if (fMakeSelInFirstResponse)
			{
				NotesEditingHelper.MakeSelectionInNote(book, index, 0,
					ScrScriptureNote.ScrScriptureNoteTags.kflidResponses);
			}
			else if (m_vc.IsExpanded(ann.Hvo) &&
				m_vc.IsExpanded(Cache.GetObjProperty(ann.Hvo, (int)noteTagToSelect)))
			{
				NotesEditingHelper.MakeSelectionInNote(m_vc, false, book,
					index, 0, noteTagToSelect, this, true);
			}
			else
			{
				NotesEditingHelper.MakeSelectionInNoteRef(m_vc, book, index, this);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the category chooser dialog to let the user set the category for the
		/// specified annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetAnnotationCategory(ScrScriptureNote ann)
		{
			if (ann == null)
				return; // Not much we can do

			m_rootb.DestroySelection();
			string sUndo, sRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidSetAnnotationCategory", out sUndo, out sRedo);

			using (new UndoTaskHelper(this, sUndo, sRedo, true))
			using (new WaitCursor(this))
			using (CategoryChooserDlg dlg =
				new CategoryChooserDlg(m_scr.NoteCategoriesOA, ann.CategoriesRS.HvoArray))
			{
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK &&
					!dlg.GetPossibilities(ann.CategoriesRS))
				{
					ann.DateModified = DateTime.Now;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the view.
		/// </summary>
		/// <remarks>This overload is needed because the view is not read-only but nothing is
		/// expanded. When there are many notes, the view will try to expand all the notes
		/// to try to make a selection (which can make bringing up the view unacceptably long).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			ReadOnlyView = true;
			base.OnLoad(e);
			ReadOnlyView = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We override this method to make a selection in all of the views that are in a
		/// snynced group. This fixes problems where the user changes the selection in one of
		/// the slaves, but the master is not updated. Thus the view is not scrolled as the
		/// groups scroll position only scrolls the master's selection into view. (TE-3380)
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		/// ------------------------------------------------------------------------------------
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			if (s_fInSelectionChanged)
				return;
			base.SelectionChanged(prootb, vwselNew);

			if (m_ignoreSelChanged || EditingHelper.CurrentSelection == null)
				return;

			SelLevInfo noteInfo =
				EditingHelper.CurrentSelection.GetLevelInfoForTag(m_currentNotesTag);

			if (noteInfo.hvo == m_prevNoteHvo)
				return;

			ScrScriptureNote ann = new ScrScriptureNote(m_fdoCache, noteInfo.hvo);
			s_fInSelectionChanged = true;
			try
			{
				// Remove highlighting on previously selected annotation, and add highlighting to selected annotation.
				SelectionHelper sel = EditingHelper.CurrentSelection;
				m_vc.SelectedNoteHvo = noteInfo.hvo;
				if (m_prevNoteHvo != 0)
				{
					// Make sure we use the index in the virtual property and not
					// the index in the list of notes.
					int ownerHvo = m_fdoCache.GetOwnerOfObject(m_prevNoteHvo);
					int ihvo = m_fdoCache.GetObjIndex(ownerHvo, m_currentNotesTag, m_prevNoteHvo);
					if (ihvo >= 0) // Could be -1 if not in the filter anymore (TE-8891)
					{
						m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, ownerHvo,
							m_currentNotesTag, ihvo, 1, 1);
					}
				}
				m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, ann.OwnerHVO,
					m_currentNotesTag, noteInfo.ihvo, 1, 1);

				sel.SetSelection(false); // restore selection taken from PropChange
			}
			finally
			{
				s_fInSelectionChanged = false;
			}

			// Set text of caption to indicate which annotation is selected
			m_notesMainWnd.InformationBarText = BaseInfoBarCaption + " - " + GetRefAsString(ann) + "     " +
				GetQuotedText(ann.QuoteOA, 30);

			SetNoteUpdateTime(m_prevNoteHvo);
			m_prevNoteHvo = noteInfo.hvo;

			if (!m_fSendSyncScrollMsg)
				return;

			NotesMainWnd notesMainWnd = TheMainWnd as NotesMainWnd;
			if (notesMainWnd != null && notesMainWnd.SyncHandler != null)
			{
				Trace.WriteLine("Calling SyncToAnnotation from NotesDataEntryView: " + ann.CitedText);
				notesMainWnd.SyncHandler.SyncToAnnotation(this, ann, m_scr);
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified number of characters from the Quoted Text field of an annotation.
		/// </summary>
		/// <param name="quotedText">The quoted text.</param>
		/// <param name="textLength">The number of characters to take from the quoted text.
		/// Ellipses are added if the quoted text exceeds textLength.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetQuotedText(IStJournalText quotedText, int textLength)
		{
			string strQuote = ((StTxtPara)quotedText.ParagraphsOS[0]).Contents.UnderlyingTsString.Text;
			if (string.IsNullOrEmpty(strQuote))
				return string.Empty;

			int substrlen = Math.Min(textLength, strQuote.Length);
			if (substrlen < strQuote.Length)
				return strQuote.Substring(0, substrlen) + ResourceHelper.GetResourceString("ksEllipsis");
			return strQuote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture reference of the specified annotation as string.
		/// </summary>
		/// <param name="ann">The specified annotation.</param>
		/// ------------------------------------------------------------------------------------
		private string GetRefAsString(ScrScriptureNote ann)
		{
			BCVRef startRef = new BCVRef(ann.BeginRef);
			IScrBook book = m_scr.FindBook(startRef.Book);
			string titleText = ResourceHelper.GetResourceString("kstidScriptureTitle");
			string introText = ResourceHelper.GetResourceString("kstidScriptureIntro");
			return ScrReference.MakeReferenceString(book.BestUIName, startRef, new BCVRef(ann.EndRef),
				m_scr.ChapterVerseSepr, m_scr.Bridge, titleText, introText);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the updateTime on the specified note. This method deletes the mark in the
		/// undo stack. If fStartNewMark is true then a new mark is created.
		/// </summary>
		/// <param name="noteHvo">The hvo of the note to be updated, or 0 to ignore</param>
		/// ------------------------------------------------------------------------------------
		public void SetNoteUpdateTime(int noteHvo)
		{
			CheckDisposed();

			IActionHandler handler = m_fdoCache.ActionHandlerAccessor;
			if (m_fIgnoreTmStmpUpdate || handler == null || handler.CurrentDepth > 0 ||
				handler.TopMarkHandle == 0)
			{
				return;
			}

			if (!handler.get_TasksSinceMark(true))
				handler.DiscardToMark(0);
			else
			{
				 // Can happen... just continue on
				if (noteHvo != 0 && m_fdoCache.IsValidObject(noteHvo))
				{
					ScrScriptureNote ann = new ScrScriptureNote(m_fdoCache, noteHvo);
					ann.DateModified = DateTime.Now;
					m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, noteHvo,
						(int)CmAnnotation.CmAnnotationTags.kflidDateModified, 0, 1, 1);
				}

				string sUndo;
				string sRedo;
				TeResourceHelper.MakeUndoRedoLabels("kstidDiffDlgUndoRedoEditNote",
					out sUndo, out sRedo);
				handler.CollapseToMark(0, sUndo, sRedo);
				// If no tasks since the mark actually change the data, they should all get
				// deleted when we collapse.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of displayed notes.
		/// </summary>
		/// <value>The displayed notes count.</value>
		/// ------------------------------------------------------------------------------------
		private int DisplayedNotesCount
		{
			get
			{
				int count = 0;
				foreach (int hvo in m_scr.BookAnnotationsOS.HvoArray)
					count += m_fdoCache.GetVectorSize(hvo, m_currentNotesTag);
				return count;
			}
		}
		#endregion

		#region Internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggle the expansion box for the item with the given hvo.
		/// </summary>
		/// <param name="hvoItemToExpand">hvo of the item to expand</param>
		/// ------------------------------------------------------------------------------------
		internal void OpenExpansionBox(int hvoItemToExpand)
		{
			CheckDisposed();
			Trace.WriteLine("Expanding box for " + hvoItemToExpand);
			m_vc.ExpandItem(hvoItemToExpand);
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the prev note hvo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResetPrevNoteHvo()
		{
			CheckDisposed();
			m_prevNoteHvo = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for any annotations in the curtrently filtered set whose reference range
		/// covers the given Scripture reference. Any such annotations are expanded and the
		/// first on is scrolled to near the top of the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ScrollRefIntoView(ScrReference reference, string quotedText)
		{
			CheckDisposed();
			if (reference.Book <= 0)
				return;

			bool fSaveSendSyncScrollMsg = m_fSendSyncScrollMsg;

			try
			{
				m_fSendSyncScrollMsg = false;
				int bookHvo = m_scr.BookAnnotationsOS[reference.Book - 1].Hvo;
				int[] annHvos = m_fdoCache.GetVectorProperty(bookHvo,
					m_vc.NotesSequenceHandler.Tag, false);

				int exactMatchingAnnotation = -1;
				int firstMatchingAnnotation = -1;

				for (int i = 0; i < annHvos.Length; i++)
				{
					ScrScriptureNote ann = new ScrScriptureNote(m_fdoCache, annHvos[i]);
					if (ann.BeginRef <= reference && ann.EndRef >= reference)
					{
						if (firstMatchingAnnotation < 0)
							firstMatchingAnnotation = i;

						string qtext = ((StTxtPara)ann.QuoteOA.ParagraphsOS[0]).Contents.Text;
						if (!string.IsNullOrEmpty(qtext) && !string.IsNullOrEmpty(quotedText) &&
							qtext == quotedText && exactMatchingAnnotation < 0)
						{
							exactMatchingAnnotation = i;
						}
					}
					else if (firstMatchingAnnotation >= 0)
						break;
				}

				int idx = Math.Max(exactMatchingAnnotation, firstMatchingAnnotation);
				if (idx >= 0)
					NotesEditingHelper.MakeSelectionInNote(m_vc, reference.Book - 1, idx, this, m_vc.IsExpanded(annHvos[idx]));
			}
			finally
			{
				m_fSendSyncScrollMsg = fSaveSendSyncScrollMsg;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for any annotations in the curtrently filtered set whose reference range
		/// covers the given Scripture reference. Any such annotations are expanded and the
		/// first on is scrolled to near the top of the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ScrollRelevantAnnotationIntoView(TeEditingHelper editingHelper)
		{
			CheckDisposed();

			Debug.Assert(editingHelper != null);
			if (editingHelper.BookIndex < 0)
				return;

			bool fSaveSendSyncScrollMsg = m_fSendSyncScrollMsg;

			try
			{
				m_fSendSyncScrollMsg = false;
				ScrReference scrRef = editingHelper.CurrentStartRef;
				if (!scrRef.Valid)
					return; // No valid reference to scroll to

				ITsString tss = editingHelper.GetCleanSelectedText();
				string selectedText = (tss != null ? tss.Text : null);

				// If there's no range selection in scripture and the IP is in the
				// same reference as that of the current annotation, then don't scroll
				// to a different annotation, even if there is another one for the
				// same reference.
				if (selectedText == null && CurrentAnnotation != null &&
					scrRef == CurrentAnnotation.BeginRef)
				{
					return;
				}

				selectedText = (selectedText ?? editingHelper.CleanSelectedWord);

				// Try to find the exact annotation associated with the selection.
				if (ScrollToAnnotationByPara(editingHelper, selectedText,
					CurrentAnnotation != null && scrRef == CurrentAnnotation.BeginRef))
				{
					return;
				}

				// When the passed editing helper's selection is in a book title, section heading
				// or intro. material, then find the annotation for that selection's paragraph.
				if (editingHelper.InBookTitle || editingHelper.InSectionHead ||
					editingHelper.InIntroSection)
				{
					if (ScrollToNonScrAnnotationByBook(scrRef.Book, selectedText))
						return;
				}

				if (CurrentAnnotation == null || scrRef != CurrentAnnotation.BeginRef)
					ScrollRefIntoView(scrRef, selectedText);
			}
			finally
			{
				m_fSendSyncScrollMsg = fSaveSendSyncScrollMsg;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to scroll to an annotation whose BeginObjectRAHvo is the same as the hvo
		/// of the paragraph where the anchor is in the specified editing helper's selection.
		/// </summary>
		/// <param name="editingHelper">The TE editing helper with information about the
		/// selection in the Scripture pane.</param>
		/// <param name="selectedText">The selected text or the word containing the IP if the
		/// selection is not a range.</param>
		/// <param name="fExactMatchOnly">if set to <c>true</c> then only scroll to a found
		/// note if the cited text is an exact match.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool ScrollToAnnotationByPara(TeEditingHelper editingHelper,
			string selectedText, bool fExactMatchOnly)
		{
			if (editingHelper == null || editingHelper.CurrentSelection == null)
				return false;

			ScrReference scrRef = editingHelper.CurrentStartRef;
			SelLevInfo[] info = editingHelper.CurrentSelection.GetLevelInfo(
				SelectionHelper.SelLimitType.Anchor);

			if (info.Length == 0)
				return false;

			int bookHvo = m_scr.BookAnnotationsOS[scrRef.Book - 1].Hvo;
			int[] annHvos = m_fdoCache.GetVectorProperty(bookHvo,
				m_vc.NotesSequenceHandler.Tag, false);

			if (annHvos.Length == 0)
				return false;

			int ich = editingHelper.CurrentSelection.IchAnchor;

			// Go through the annotations for the book and find the one whose
			// begin object is in the same as the selection's paragraph.
			for (int i = 0; i < annHvos.Length; i++)
			{
				ScrScriptureNote ann = new ScrScriptureNote(m_fdoCache, annHvos[i]);
				if (ann.BeginObjectRAHvo == info[0].hvo)
				{
					// When matching on the cited text, allow for the possibility that the
					// begin offset is off by a little bit since leading spaces and/or ORCs
					// may have been trimmed and subsequent editing may have messed up the
					// offsets a little.
					int adjustedBeginOffset = ann.BeginOffset -
						(string.IsNullOrEmpty(ann.CitedText) ? 1 : ann.CitedText.Length);

					if ((!fExactMatchOnly && ich >= ann.BeginOffset && ich <= ann.EndOffset) ||
						(ich >= adjustedBeginOffset && selectedText == ann.CitedText))
					{
						m_vc.SelectedNoteHvo = ann.Hvo;
						NotesEditingHelper.MakeSelectionInNote(m_vc, scrRef.Book - 1, i, this,
							m_vc.IsExpanded(ann.Hvo));
						return true;
					}
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to scroll to the first annotation whose beginning reference's book is
		/// the same as the specified book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ScrollToNonScrAnnotationByBook(int book, string selectedText)
		{
			int bookHvo = m_scr.BookAnnotationsOS[book - 1].Hvo;
			int[] annHvos = m_fdoCache.GetVectorProperty(bookHvo,
				m_vc.NotesSequenceHandler.Tag, false);

			if (annHvos.Length == 0)
				return false;

			int ihvo = 0;
			ScrScriptureNote ann;
			BCVRef bcvRef;

			// Go through the annotations for the book and find the one whose
			// cited text is the same as the specified selected text.
			for (int i = 0; i < annHvos.Length; i++)
			{
				ann = new ScrScriptureNote(m_fdoCache, annHvos[i]);
				bcvRef = new BCVRef(ann.BeginRef);
				if (ann.CitedText == selectedText && bcvRef.Chapter == 0)
				{
					ihvo = i;
					break;
				}
			}

			ann = new ScrScriptureNote(m_fdoCache, annHvos[ihvo]);
			bcvRef = new BCVRef(ann.BeginRef);
			if (bcvRef.Chapter > 0)
				return false;

			NotesEditingHelper.MakeSelectionInNote(m_vc, book - 1, ihvo, this, m_vc.IsExpanded(ann.Hvo));
			return true;
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		/////
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//public void CreateMark()
		//{
		//    CheckDisposed();
		//
		//    if (m_markId == -1 && m_fdoCache.ActionHandlerAccessor != null)
		//        SetNoteUpdateTime(0, true);
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckForUpdates()
		{
			CheckDisposed();

			int noteHvo = 0;
			if (EditingHelper != null && EditingHelper.CurrentSelection != null)
			{
				SelLevInfo noteInfo =
					EditingHelper.CurrentSelection.GetLevelInfoForTag(m_currentNotesTag);
				noteHvo = noteInfo.hvo;
			}
			SetNoteUpdateTime(noteHvo);
		}
		#endregion

		#region ISelectableView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base caption string that this client window should display in
		/// the info bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BaseInfoBarCaption
		{
			get
			{
				CheckDisposed();
				return m_baseInfoBarCaption;
			}
			set
			{
				CheckDisposed();
				m_baseInfoBarCaption = value == null ? null :
					value.Replace(Environment.NewLine, " ");
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void ActivateView()
		{
			CheckDisposed();

			PerformLayout();
			Show();
			Focus();

			if (TheMainWnd != null)
			{
				TheMainWnd.InitStyleComboBox();
				TheMainWnd.UpdateWritingSystemSelectorForSelection(m_rootb);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deactivates the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeactivateView()
		{
			CheckDisposed();

			Hide();
		}
		#endregion

		#region Message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable Edit/Select All only if we have at least one note.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditSelectAll(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.ParentForm == Parent)
			{
				itemProps.Enabled = (DisplayedNotesCount > 0);
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply the filter the user clicked in the side bar or in the menu.
		/// </summary>
		/// <param name="sender">The side-bar button the user clicked</param>
		/// ------------------------------------------------------------------------------------
		public virtual void OnChangeFilter(object sender)
		{
			CheckDisposed();

			CmFilter userFilter = null;
			if (sender is SBTabItemProperties) // from sidebar
				userFilter = (sender != null) ? (CmFilter)((SBTabItemProperties)sender).Tag : null;
			else if (sender is TMItemProperties) // from menu
				userFilter = (sender != null) ? (CmFilter)((TMItemProperties)sender).Tag : null;

			if (userFilter != null && !GetFilterValuesFromUser(userFilter))
				return;

			NotesViewFilter annotationFilter = new NotesViewFilter(Cache, userFilter);
			FilteredSequenceHandler handler;
			try
			{
				handler = FilteredSequenceHandler.GetFilterInstance(m_fdoCache,
					ScrBookAnnotations.kClassId, annotationFilter, Handle.ToInt32());

				if (handler != null)
					handler.Reinitialize(false);
				else
				{
					if (userFilter != null)
						userFilter.UserView = m_UserView;

					handler = new FilteredSequenceHandler(m_fdoCache, ScrBookAnnotations.kClassId,
						Handle.ToInt32(), annotationFilter, null, m_UserView);
				}
			}
			catch
			{
				// User must have cancelled the filter, or something horrible happened.
				// Just revert back to the previous state.
				NotesMainWnd notesMainWnd = TheMainWnd as NotesMainWnd;
				Debug.Assert(notesMainWnd != null);
				notesMainWnd.SelectFilterButton(m_vc.NotesSequenceHandler != null ?
					m_vc.NotesSequenceHandler.Filter as CmFilter: null);
				return;
			}

			Debug.Assert(handler != null);
			m_currentNotesTag = handler.Tag;

			// Set up the view constructor with the filtered sequence handler corresponding to the
			// notes filter chosen by the user.
			m_vc.NotesSequenceHandler = handler;
			RefreshDisplay();
			if (FilterChanged != null)
				FilterChanged(this, userFilter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if a dialog box asking for user settings is necessary and, if so,
		/// shows the dialog box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool GetFilterValuesFromUser(CmFilter userFilter)
		{
			if (userFilter == null)
				return true;

			// Don't try create an undo task for this. The user won't expect to undo/redo this
			// action. (TE-8575)
			using (new SuppressSubTasks(Cache))
			{
				Cache.SetIntProperty(userFilter.Hvo,
					(int)SIL.FieldWorks.FDO.Cellar.CmFilter.CmFilterTags.kflidShowPrompt, 0);

				if (userFilter.ShortName == "kstidNoteMultiFilter")
				{
					using (MultipleFilterDlg dlg = new MultipleFilterDlg(Cache, userFilter))
					{
						if (dlg.ShowDialog() != DialogResult.OK)
							return false;
					}
				}
				else if (userFilter.ShortName == "kstidCategoryNoteFilter")
				{
					using (CategoryFilterDlg dlg = new CategoryFilterDlg(Cache, userFilter))
					{
						if (dlg.ShowDialog() != DialogResult.OK)
							return false;
					}
				}
			}

			return true;
		}

		#endregion
	}
}
