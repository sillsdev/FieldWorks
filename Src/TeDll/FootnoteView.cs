// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FootnoteView.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Implements the footnote view (formerly SeDraftWnd in file DraftWnd.cpp/h).
// </remarks>

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.UIAdapters;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of the footnote view
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class FootnoteView : DraftViewBase
	{
		#region Data members
		private DraftView m_draftView;
		#endregion

		#region Constructor, Dispose, InitializeComponent
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FootnoteView class
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The tag that identifies the book filter instance.</param>
		/// <param name="app">The application.</param>
		/// <param name="viewName">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if view is to be editable.</param>
		/// <param name="viewType">Bit-flags indicating type of view.</param>
		/// <param name="btWs">The back translation writing system (if needed).</param>
		/// <param name="draftView">The corresponding draftview pane</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteView(FdoCache cache, int filterInstance, IApp app, string viewName,
			bool fEditable, TeViewType viewType, int btWs, DraftView draftView) :
			base(cache, filterInstance, app, viewName, fEditable, viewType, btWs)
		{
			Debug.Assert((viewType & TeViewType.FootnoteView) != 0);
			m_draftView = draftView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			m_draftView = null;
		}

		#endregion

		#region Event handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse button up event
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right)
			{
				base.OnMouseUp(e);
				return;
			}

			FwMainWnd mainWnd = TheMainWnd;
			if (mainWnd != null && mainWnd.TMAdapter != null)
			{
				FootnoteEditingHelper.ShowContextMenu(e.Location, mainWnd.TMAdapter, this,
					"cmnuFootnoteView", "cmnuAddToDictFV", "cmnuChangeMultiOccurencesFV", "cmnuAddToDictFV",
					TeProjectSettings.ShowSpellingErrors);
			}
		}
		#endregion

		#region Other methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the selected footnote.
		/// </summary>
		/// <param name="tag">The flid of the selected footnote</param>
		/// <param name="hvoSel">The hvo of the selected footnote</param>
		/// <returns>True, if a footnote is found at the current selection</returns>
		/// -----------------------------------------------------------------------------------
		protected bool GetSelectedFootnote(out int tag, out int hvoSel)
		{
			hvoSel = 0;
			tag = 0;
			if (m_rootb == null)
				return false;
			IVwSelection vwsel = m_rootb.Selection;
			if (vwsel == null)
				return false;
			return GetSelectedFootnote(vwsel, out tag, out hvoSel);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the flid and hvo corresponding to the current Scripture element (e.g., section
		/// heading, section contents, or title) selected.
		/// </summary>
		/// <param name="vwsel">The current selection</param>
		/// <param name="tag">The flid of the selected footnote</param>
		/// <param name="hvoSel">The hvo of the selected footnote</param>
		/// <returns>True, if a footnote is found at the current selection</returns>
		/// -----------------------------------------------------------------------------------
		protected bool GetSelectedFootnote(IVwSelection vwsel, out int tag, out int hvoSel)
		{
			hvoSel = 0;
			tag = 0;
			int hvoPrevLevel = 0;
			int tagPrev = 0;
			try
			{
				if (vwsel != null)
				{
					// If we look more than 10 levels then something is wrong.
					for (int ilev = 0; ilev < 10; ilev++)
					{
						int ihvo, cpropPrev;
						IVwPropertyStore qvps;
						hvoPrevLevel = hvoSel;
						tagPrev = tag;
						vwsel.PropInfo(false, ilev, out hvoSel, out tag, out ihvo,
							out cpropPrev, out qvps);
						switch (tag)
						{
							case ScrBookTags.kflidFootnotes:
							{
								hvoSel = hvoPrevLevel;
								tag = tagPrev;
								return true;
							}
							default:
								break;
						}
					}
				}
			}
			catch
			{
				// REVIEW (TimS): Why are we catching all exceptions here?
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls the requested footnote to the top of the view
		/// </summary>
		/// <param name="footnote">The target footnote</param>
		/// <param name="fPutInsertionPtAtEnd">if set to <c>true</c> and showing the view,
		/// the insertion point will be put at the end of the footnote text instead of at the
		/// beginning, as would be appropriate in the case of a newly inserted footnote that has
		/// Reference Text. This parameter is ignored if footnote is null.</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToFootnote(IStFootnote footnote, bool fPutInsertionPtAtEnd)
		{
			CheckDisposed();

			// find book owning this footnote
			int iBook = m_bookFilter.GetBookIndex((IScrBook)footnote.Owner);

			// find index of this footnote
			int iFootnote = footnote.IndexInOwner;

			// create selection pointing to this footnote
			// TODO (FWR-2270): This won't work correctly in BT or segmented BT footnote views whe
			// attempting to put the IP at the end of the footnote.
			FootnoteEditingHelper.ScrollToFootnote(iBook, iFootnote, (fPutInsertionPtAtEnd ?
				((IStTxtPara)footnote.ParagraphsOS[0]).Contents.Length: 0));
		}
		#endregion

		#region Overrides of RootSite
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether automatic vertical scrolling to show the selection should
		/// occur. Usually this is only appropriate if the window autoscrolls and has a
		/// vertical scroll bar, but TE's footnote view needs to allow it anyway, because in
		/// synchronized scrolling only one of the sync'd windows has a scroll bar.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected override bool DoAutoVScroll
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the average paragraph height for the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int AverageParaHeight
		{
			get
			{
				CheckDisposed();
				return (int)(12 * Zoom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate height for footnotes.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>The estimated height for the specified hvo in paragraphs</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			// This is done in the TeParaCounter class.
			return m_ParaHeightInPoints;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			Debug.Assert(Cache != null);
			FootnoteEditingHelper editingHelper = new FootnoteEditingHelper(this, Cache,
				m_filterInstance, DraftView, m_viewType, m_app);
			editingHelper.ContentType = ContentType;
			editingHelper.Editable = m_initialEditableState;
			editingHelper.InternalContext = ContextValues.Note;
			return editingHelper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a Footnote specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FootnoteEditingHelper FootnoteEditingHelper
		{
			get
			{
				CheckDisposed();
				return (FootnoteEditingHelper)EditingHelper;
			}
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
		///
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="vwselNew"></param>
		/// ------------------------------------------------------------------------------------
		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.HandleSelectionChange(rootb, vwselNew);

			if (EditingHelper is TeEditingHelper)
				((TeEditingHelper)EditingHelper).SetInformationBarForSelection();

			#region Debug code
#if DEBUG_not_exist
			if (TheMainWnd == null)
				return;
			// This section of code will display selection information in the status bar when the
			// program is compiled in Debug mode. The information shown in the status bar is useful
			// when you want to make selections in tests.
			try
			{
				SelectionHelper helper = EditingHelper.CurrentSelection;

				string text = "Book: " + helper.LevelInfo[2].ihvo +
					"  Footnote: " + helper.LevelInfo[1].ihvo +
					"  Paragraph: " + helper.LevelInfo[0].ihvo +
					"  Anchor: " + helper.IchAnchor + "  End: " + helper.IchEnd +
					"  AssocPrev: " + helper.AssocPrev;

				((FwMainWnd)TheMainWnd).StatusBar.Panels[0].Text = text;
			}
			catch
			{
			}
#endif
			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			base.MakeRoot();

			if (FwEditingHelper.ApplicableStyleContexts != null)
			{
				FwEditingHelper.ApplicableStyleContexts = new List<ContextValues>(2);
				FwEditingHelper.ApplicableStyleContexts.Add(ContextValues.Note);
				FwEditingHelper.ApplicableStyleContexts.Add(ContextValues.General);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a view constructor suitable for this kind of view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override TeStVc CreateViewConstructor()
		{
			return new FootnoteVc(TeStVc.LayoutViewTarget.targetDraft, m_filterInstance);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow user to paste wacky stuff in the footnote pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox prootb,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();
			return VwInsertDiffParaResponse.kidprFail;
		}

		/// <summary> see OnInsertDiffParas </summary>
		public override VwInsertDiffParaResponse OnInsertDiffPara(IVwRootBox prootb,
			ITsTextProps ttpDest, ITsTextProps ttpSrc, ITsString tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();
			return VwInsertDiffParaResponse.kidprFail;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public override int GetWritingSystemForHvo(int hvo)
		{
			CheckDisposed();

			return ViewConstructorWS;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The view constructor "fragment" associated with the root object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int RootFrag
		{
			get { return (int)FootnoteFrags.kfrScripture; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view associated with the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DraftView DraftView
		{
			get
			{
				CheckDisposed();
				return m_draftView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the status of the selection in the footnote view.</summary>
		/// ------------------------------------------------------------------------------------
		private bool ValidFootnoteSelection
		{
			get
			{
				CheckDisposed();
				SelectionHelper helper = SelectionHelper.Create(this);
				return (helper == null ? false :
					helper.GetLevelForTag(ScrBookTags.kflidFootnotes) >= 0);
			}
		}
		#endregion

		#region Update handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables the toolbar item
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		private void DisableTMItem(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = false;
				itemProps.Update = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a footnote inside of a footnote isn't possible
		/// </summary>
		/// <returns>false if this view did not have focus; <c>true</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertFootnote(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a footnote from the footnote view isn't possible
		/// </summary>
		/// <returns>true if we handled the update message; false if we didn't handle it</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertGeneralFootnote(object args)
		{
			return OnUpdateInsertFootnote(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a cross-reference from the footnote view isn't possible
		/// </summary>
		/// <returns>true if we handled the update message; false if we didn't handle it</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertCrossRefFootnote(object args)
		{
			return OnUpdateInsertFootnote(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a section inside of a footnote isn't possible
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertSection(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting an intro section inside of a footnote isn't possible
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertIntroSection(object args)
		{
			CheckDisposed();

			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting verse numbers inside of a footnote isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertVerseNumbers(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting chapters inside of a footnote isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertChapterNumber(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a book inside of a footnote isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertBook(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Only allow deleting of a footnote if we aren't in a BT footnote view and we have
		/// a valid selection.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateDeleteFootnote(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;

			if (itemProps == null || !Focused)
				return false;

			itemProps.Enabled = !IsBackTranslation && ValidFootnoteSelection;
			itemProps.Update = true;
			return true;
		}
		#endregion

		#region Menu handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a footnote
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDeleteFootnote(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress() || !ValidFootnoteSelection)
				return true; //discard this event

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoDelFootnote", out undo, out redo);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(this, undo, redo))
			using (new DataUpdateMonitor(this, "DeleteFootnote"))
			{
				// Put code to do work in separate method for testing
				DeleteFootnoteAux();
				undoTaskHelper.RollBack = false;
			}

			// If there are no more footnotes, then give focus back to the main draft view
			if (RootBox.Height <= 0 && DraftView != null)
				DraftView.Focus();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a footnote or footnotes within a text selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void DeleteFootnoteAux()
		{
			SelectionHelper helper = SelectionHelper.Create(this);
			if (helper == null)
				return; // Better then crashing :)

			int fnLevel = helper.GetLevelForTag(ScrBookTags.kflidFootnotes);

			if (helper.Selection.IsRange)
				DeleteFootnoteRange(helper);
			else
			{
				// There's no range selection, so delete only one footnote
				IScrFootnote footnote =
					Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetObject(helper.LevelInfo[fnLevel].hvo);
				((IScrBook)footnote.Owner).FootnotesOS.Remove(footnote);
			}

			if (RootBox.Height <= 0)
				DraftView.Focus();
			else
			{
				int iBook = helper.LevelInfo[fnLevel + 1].ihvo;
				IScrBook book = m_bookFilter.GetBook(iBook);
				int iFootnote = helper.LevelInfo[fnLevel].ihvo;

				// If the last footnote in the book was deleted find a footnote to move to
				if (iFootnote >= book.FootnotesOS.Count)
					FindNearestFootnote(ref iBook, ref iFootnote);

				FootnoteEditingHelper.ScrollToFootnote(iBook, iFootnote, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a footnote is deleted and there are no more in the book, find the book index
		/// and footnote index of the nearest footnote.
		/// </summary>
		/// <param name="iBook">The book index in the filter.</param>
		/// <param name="iFootnote">The footnote index.</param>
		/// ------------------------------------------------------------------------------------
		protected void FindNearestFootnote(ref int iBook, ref int iFootnote)
		{
			// first, try to move to the first footnote in a following book
			for (int iFindBook = iBook + 1; iFindBook < m_bookFilter.BookCount; iFindBook++)
			{
				IScrBook book = m_bookFilter.GetBook(iFindBook);
				if (book.FootnotesOS.Count > 0)
				{
					iBook = iFindBook;
					iFootnote = 0;
					return;
				}
			}

			// we did not find a footnote in any following books, so look at the previous books
			for (int iFindBook = iBook; iFindBook >= 0; iFindBook--)
			{
				IScrBook book = m_bookFilter.GetBook(iFindBook);
				if (book.FootnotesOS.Count > 0)
				{
					iBook = iFindBook;
					iFootnote = book.FootnotesOS.Count - 1;
					return;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes footnotes when there is a range selection.
		/// </summary>
		/// <param name="helper"></param>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnoteRange(SelectionHelper helper)
		{
			int nTopLevels = helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Top);
			int nBottomLevels = helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Bottom);

			// Get the index of the book containing the first footnote in the selection.
			// Then get the index of the footnote within that book.
			int iFirstBook =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[nTopLevels-1].ihvo;
			int iFirstFootnote =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[nTopLevels-2].ihvo;

			// Get the index of the book containing the last footnote in the selection.
			// Then get the index of the footnote within that book.
			int iLastBook =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom)[nBottomLevels-1].ihvo;
			int iLastFootnote =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom)[nBottomLevels-2].ihvo;

			// Loop through the books containing footnotes in the selection.
			for (int iBook = iFirstBook; iBook <= iLastBook; iBook++)
			{
				IScrBook book = BookFilter.GetBook(iBook);

				int iBeg = iFirstFootnote;
				if (iFirstBook != iLastBook && iBook > iFirstBook)
					iBeg = 0;

				int iEnd = iLastFootnote;
				if (iFirstBook != iLastBook && iBook < iLastBook)
					iEnd = book.FootnotesOS.Count - 1;

				// Loop through the footnotes from the selection that are in the
				// current book. Go in reverse order through the collection.
				for (int i = iEnd; i >= iBeg; i--)
				{
					// TODO: check filter for each HVO
					IScrFootnote footnote =
						Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetObject(book.FootnotesOS[i].Hvo);
					book.FootnotesOS.Remove(footnote);
				}
			}
		}
		#endregion
	}
}
