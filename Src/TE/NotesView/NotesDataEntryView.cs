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
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.FDO.Application;

using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

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
		private readonly FwMainWnd m_notesMainWnd;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly IScripture m_scr;
		internal TeNotesVc m_vc;
		private string m_baseInfoBarCaption;
		private bool m_pictureSelected; // indicates whether a picture was selected on mouse down
		private IScrScriptureNote m_prevNote;
		private IScrScriptureNote m_prevHightlightedNote;
		private const int kCurrentNotesTag = ScrBookAnnotationsTags.kflidNotes;
		private bool m_fSendSyncScrollMsg = true;
		private bool m_suspendHighlightChange;
		private bool m_ignoreSelChanged;

		/// <summary>Delegate to indicate a change in the filter</summary>
		public delegate void FilterChangedHandler(object sender, ICmFilter filter);

		/// <summary>Event to indicate a change in the filter</summary>
		public event FilterChangedHandler FilterChanged;
		#endregion

		#region Constructor and Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NotesDataEntryView"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="infoBarCaption">The info bar caption.</param>
		/// <param name="notesWnd">The notes window.</param>
		/// ------------------------------------------------------------------------------------
		public NotesDataEntryView(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			string infoBarCaption, FwMainWnd notesWnd) : base(cache)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_notesMainWnd = notesWnd;
			BaseInfoBarCaption = infoBarCaption;
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
		}

		#endregion

		#region Properties
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
		public IScrScriptureNote CurrentAnnotation
		{
			get
			{
				if (EditingHelper == null || EditingHelper.CurrentSelection == null)
				{
					// No currently selected annotation, but we can assume that the previously
					// selected annotation (the one in the VC) is still the one the user thinks
					// is selected. (TE-8889)
					var scrNoteRepo = m_fdoCache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>();
					IScrScriptureNote ann;
					scrNoteRepo.TryGetObject(m_vc.SelectedNoteHvo, out ann);
					return ann;
				}

				try
				{
					SelLevInfo info = EditingHelper.CurrentSelection.GetLevelInfoForTag(kCurrentNotesTag);
					return m_fdoCache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(info.hvo);
				}
				catch { }

				return null;
			}
		}
		#endregion

		#region Overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			return new NotesEditingHelper(m_fdoCache, this, m_helpTopicProvider);
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

			if (flid == ScrBookAnnotationsTags.kflidNotes)
				return kCurrentNotesTag;
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

			m_rootb.DataAccess =
				new FilteredDomainDataByFlidDecorator(m_fdoCache, null, ScrBookAnnotationsTags.kflidNotes);

			// Set up a new view constructor.
			HorizMargin = 10;
			Debug.Assert(IsHandleCreated);
			m_vc = new TeNotesVc(m_fdoCache, Zoom);
			OnChangeFilter(null);

//			m_vc.HotLinkClick += new DraftViewVc.HotLinkClickHandler(DoHotLinkAction);

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

			Point pt = PointToScreen(new Point(e.X, e.Y));
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
			m_suspendHighlightChange = true;
			try
			{
				base.CallMouseDown(pt, rcSrcRoot, rcDstRoot);
			}
			finally
			{
				m_suspendHighlightChange = false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseUp on the rootbox
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void CallMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			IScrScriptureNote ann = CurrentAnnotation;

			int noteTagToSelect = ScrScriptureNoteTags.kflidDiscussion;
			bool fMakeSelInFirstResponse = false;

			if (sel != null && sel.SelType == VwSelType.kstPicture)
			{
				SelectionHelper selHelper = SelectionHelper.Create(sel, this);
				SelLevInfo[] info = selHelper.LevelInfo;

				if (info.Length >= 1 &&
					info[0].tag == ScrScriptureNoteTags.kflidDiscussion ||
					info[0].tag == ScrScriptureNoteTags.kflidRecommendation ||
					m_fdoCache.ServiceLocator.ObjectRepository.GetClsid(info[0].hvo) == StJournalTextTags.kClassId)
				{
					if (m_vc.ToggleItemExpansion(info[0].hvo, m_rootb))
					{
						// If the tag is not a valid tag, the assumption
						// is we're expanding the responses.
						fMakeSelInFirstResponse = (info[0].tag < 0);
						noteTagToSelect = info[0].tag;
					}
				}
				else if (selHelper.GetTextPropId(SelectionHelper.SelLimitType.Anchor) ==
					-(int)NotesFrags.kfrConnotCategory) // This is not a real flid, just a unique number to match on.
				{
					SetAnnotationCategory(ann);
				}
				else if (info.Length >= 2 && info[1].tag == kCurrentNotesTag)
				{
					m_vc.ToggleItemExpansion(info[1].hvo, m_rootb);
					m_ignoreSelChanged = true;
				}
			}

			m_suspendHighlightChange = true;
			try
			{
				base.CallMouseUp(pt, rcSrcRoot, rcDstRoot);
			}
			finally
			{
				m_ignoreSelChanged = false;
				m_suspendHighlightChange = false;
			}

			UpdateNoteHighlight(ann);

			if (!m_pictureSelected)
				return;

			m_pictureSelected = false;

			if (ann == null)
				return;

			// Make a selection in the proper place in the annotation.
			int book = BCVRef.GetBookFromBcv(ann.BeginRef) - 1;
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[book];
			int index = m_rootb.DataAccess.GetObjIndex(annotations.Hvo, ScrBookAnnotationsTags.kflidNotes, ann.Hvo);

			if (fMakeSelInFirstResponse)
			{
				NotesEditingHelper.MakeSelectionInNote(book, index, 0, ScrScriptureNoteTags.kflidResponses);
			}
			else if (m_vc.IsExpanded(ann.Hvo) &&
				m_vc.IsExpanded(m_rootb.DataAccess.get_ObjectProp(ann.Hvo, noteTagToSelect)))
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
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			base.HandleSelectionChange(prootb, vwselNew);

			if (m_ignoreSelChanged || EditingHelper.CurrentSelection == null)
				return;

			SelLevInfo noteInfo = EditingHelper.CurrentSelection.GetLevelInfoForTag(kCurrentNotesTag);
			if (m_prevNote != null && noteInfo.hvo == m_prevNote.Hvo)
				return;

			// Set text of caption to indicate which annotation is selected
			IScrScriptureNote ann = Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(noteInfo.hvo);
			// Remove highlighting on previously selected annotation, and add highlighting to selected annotation.
			UpdateNoteHighlight(ann);

			m_notesMainWnd.InformationBarText = BaseInfoBarCaption + " - " + GetRefAsString(ann) + "     " +
				GetQuotedText(ann.QuoteOA, 30);

			m_prevNote = ann;

			if (!m_fSendSyncScrollMsg)
				return;

			NotesMainWnd notesMainWnd = TheMainWnd as NotesMainWnd;
			if (notesMainWnd != null && notesMainWnd.SyncHandler != null)
			{
				Trace.WriteLine("Calling SyncToAnnotation from NotesDataEntryView: " + ann.CitedText);
				notesMainWnd.SyncHandler.SyncToAnnotation(this, ann, m_scr);
			}
		}
		#endregion

		#region Helper methods
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
				return m_scr.BookAnnotationsOS.Sum(bookAnnotations =>
					m_rootb.DataAccess.get_VecSize(bookAnnotations.Hvo, kCurrentNotesTag));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the category chooser dialog to let the user set the category for the
		/// specified annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetAnnotationCategory(IScrScriptureNote ann)
		{
			if (ann == null)
				return; // Not much we can do

			m_rootb.DestroySelection();
			string sUndo, sRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidSetAnnotationCategory", out sUndo, out sRedo);

			using (new WaitCursor(this))
			using (CategoryChooserDlg dlg = new CategoryChooserDlg(m_scr.NoteCategoriesOA,
				ann.CategoriesRS.ToHvoArray(), m_helpTopicProvider, TheMainWnd.App))
			{
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					using (UndoTaskHelper undoHelper = new UndoTaskHelper(this, sUndo, sRedo))
					{
						dlg.GetPossibilities(ann.CategoriesRS);
						ann.DateModified = DateTime.Now;
						undoHelper.RollBack = false;
					}
				}
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
		private static string GetQuotedText(IStJournalText quotedText, int textLength)
		{
			string strQuote = ((IStTxtPara)quotedText.ParagraphsOS[0]).Contents.Text;
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
		private string GetRefAsString(IScrScriptureNote ann)
		{
			BCVRef startRef = new BCVRef(ann.BeginRef);
			IScrBook book = m_scr.FindBook(startRef.Book);
			string bookName;
			// Book for note may not be in the project.
			if (book != null)
				bookName = book.BestUIName;
			else
			{
				IScrBookRef bookRef = Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().Singleton.BooksOS[startRef.Book - 1];
				ITsString tsName = bookRef.BookName.get_String(Cache.DefaultUserWs);
				if (tsName.Length == 0)
					tsName = bookRef.BookName.BestAnalysisAlternative;
				bookName = tsName.Text;
			}

			string titleText = ResourceHelper.GetResourceString("kstidScriptureTitle");
			string introText = ResourceHelper.GetResourceString("kstidScriptureIntro");
			return BCVRef.MakeReferenceString(bookName, startRef, new BCVRef(ann.EndRef),
				m_scr.ChapterVerseSepr, m_scr.Bridge, titleText, introText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the highlighted note to be the specified note.
		/// </summary>
		/// <param name="ann">The note to highlight.</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateNoteHighlight(IScrScriptureNote ann)
		{
			if (ann == null || m_prevHightlightedNote == ann || m_suspendHighlightChange)
				return;

			SelectionHelper sel = EditingHelper.CurrentSelection;

			// Only selection that can get updated is a re-install of a previous selection so
			// don't bother with any selection update handling.
			m_ignoreSelChanged = true;
			try
			{
				// Need to user real object index when doing a prop change - the GetDisplayIndex method on the filter will change
				// this to the filtered index.
				m_vc.SelectedNoteHvo = ann.Hvo;
				if (m_prevHightlightedNote != null && m_prevHightlightedNote.IsValidObject)
				{
					int ownerHvo = m_prevHightlightedNote.Owner.Hvo;
					RootBox.PropChanged(ownerHvo, ScrBookAnnotationsTags.kflidNotes, m_prevHightlightedNote.IndexInOwner, 1, 1);
				}

				RootBox.PropChanged(ann.Owner.Hvo, ScrBookAnnotationsTags.kflidNotes, ann.IndexInOwner, 1, 1);

				m_prevHightlightedNote = ann;

				if (sel != null)
					sel.SetSelection(false); // restore selection taken from PropChange
			}
			finally
			{
				m_ignoreSelChanged = false;
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
			m_vc.ExpandItem(hvoItemToExpand, m_rootb);
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
				m_vc.ExpandItem(annHvo, RootBox);
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
			m_prevNote = null;
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
				int[] annHvos = ((ISilDataAccessManaged)m_rootb.DataAccess).VecProp(bookHvo,
					kCurrentNotesTag);

				int exactMatchingAnnotation = -1;
				int firstMatchingAnnotation = -1;

				for (int i = 0; i < annHvos.Length; i++)
				{
					IScrScriptureNote ann =
						Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(annHvos[i]);
					if (ann.BeginRef <= reference && ann.EndRef >= reference)
					{
						if (firstMatchingAnnotation < 0)
							firstMatchingAnnotation = i;

						string qtext = ((IStTxtPara)ann.QuoteOA[0]).Contents.Text;
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
			int[] annHvos = ((ISilDataAccessManaged)m_rootb.DataAccess).VecProp(bookHvo,
					kCurrentNotesTag);

			if (annHvos.Length == 0)
				return false;

			int ich = editingHelper.CurrentSelection.IchAnchor;

			// Go through the annotations for the book and find the one whose
			// begin object is in the same as the selection's paragraph.
			for (int i = 0; i < annHvos.Length; i++)
			{
				IScrScriptureNote ann = Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(annHvos[i]);
				if (ann.BeginObjectRA != null && ann.BeginObjectRA.Hvo == info[0].hvo)
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
			int[] annHvos = ((ISilDataAccessManaged)m_rootb.DataAccess).VecProp(bookHvo,
					kCurrentNotesTag);

			if (annHvos.Length == 0)
				return false;

			int ihvo = 0;
			IScrScriptureNote ann;
			BCVRef bcvRef;

			// Go through the annotations for the book and find the one whose
			// cited text is the same as the specified selected text.
			for (int i = 0; i < annHvos.Length; i++)
			{
				ann = Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(annHvos[i]);
				bcvRef = new BCVRef(ann.BeginRef);
				if (ann.CitedText == selectedText && bcvRef.Chapter == 0)
				{
					ihvo = i;
					break;
				}
			}

			ann = Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(annHvos[ihvo]);
			bcvRef = new BCVRef(ann.BeginRef);
			if (bcvRef.Chapter > 0)
				return false;

			NotesEditingHelper.MakeSelectionInNote(m_vc, book - 1, ihvo, this, m_vc.IsExpanded(ann.Hvo));
			return true;
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
				m_baseInfoBarCaption = (value == null) ? null : value.Replace(Environment.NewLine, " ");
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

			ICmFilter userFilter = null;
			if (sender is SBTabItemProperties) // from sidebar
				userFilter = (ICmFilter)((SBTabItemProperties)sender).Tag;
			else if (sender is TMItemProperties) // from menu
				userFilter = (ICmFilter)((TMItemProperties)sender).Tag;

			if (userFilter != null && !GetFilterValuesFromUser(userFilter))
				return;

			NotesViewFilter annotationFilter = new NotesViewFilter(Cache, userFilter);
			IFilter prevFilter = ((FilteredDomainDataByFlidDecorator)m_rootb.DataAccess).Filter;
			try
			{
				annotationFilter.InitCriteria();
				((FilteredDomainDataByFlidDecorator)m_rootb.DataAccess).Filter = annotationFilter;
			}
			catch
			{
				// User must have cancelled the filter, or something horrible happened.
				// Just revert back to the previous state.
				NotesMainWnd notesMainWnd = TheMainWnd as NotesMainWnd;
				Debug.Assert(notesMainWnd != null);
				notesMainWnd.SelectFilterButton(prevFilter);
				return;
			}

			// Set up the view constructor with the filtered sequence handler corresponding to the
			// notes filter chosen by the user.
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
		private bool GetFilterValuesFromUser(ICmFilter userFilter)
		{
			if (userFilter == null)
				return true;

			using (NonUndoableUnitOfWorkHelper undoHelper = new NonUndoableUnitOfWorkHelper(
				Cache.ServiceLocator.GetInstance<IActionHandler>()))
			{
				userFilter.ShowPrompt = 0;
				undoHelper.RollBack = false;
			}

			switch (userFilter.FilterName)
			{
				case "kstidNoteMultiFilter":
					using (MultipleFilterDlg dlg = new MultipleFilterDlg(Cache,
						m_helpTopicProvider, userFilter))
					{
						if (dlg.ShowDialog() != DialogResult.OK)
							return false;
					}
					break;
				case "kstidCategoryNoteFilter":
					using (CategoryFilterDlg dlg = new CategoryFilterDlg(Cache,
						m_helpTopicProvider, userFilter))
					{
						if (dlg.ShowDialog() != DialogResult.OK)
							return false;
					}
					break;
			}
			return true;
		}

		#endregion
	}
}
