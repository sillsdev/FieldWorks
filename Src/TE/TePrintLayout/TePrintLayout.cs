// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TePrintLayout.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.FDO.DomainServices;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScripturePublication : PublicationControl, IxCoreColleague, ILocationTracker,
		ITeView, IBtAwareView
	{
		#region Constants
		/// <summary>Number of levels in a selection for a book title</summary>
		private const int kBookTitleLevelCount = 2;
		/// <summary>Number of levels in a selection for a section</summary>
		private const int kSectionLevelCount = 3;
		/// <summary>Normal font size in millipoints for a 1-column publication laid out on a
		/// standard page size. This constant shoould probably be defined in TePublications.xml
		/// </summary>
		private const int kStdOneColumnFontSize = 11000;
		/// <summary>Normal line height in millipoints (negative for exact)for a 1-column
		/// publication laid out on a standard page size. This constant shoould probably be
		/// defined in TePublications.xml
		/// </summary>
		private const int kStdOneColumnLineHeight = -13000;
		#endregion

		#region Data members
		/// <summary></summary>
		protected int m_filterInstance;
		/// <summary></summary>
		protected FilteredScrBooks m_bookFilter;
		/// <summary></summary>
		protected TeViewType m_viewType;
		/// <summary></summary>
		protected IApp m_app;
		/// <summary>The back translation WS</summary>
		protected int m_BackTranslationWS;
		/// <summary/>
		protected FootnoteVc m_footnoteVc;
		#endregion

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
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_bookFilter = null;
			m_footnoteVc = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Constructor & helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a general-purpose publication for printing Scripture
		/// </summary>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="filterInstance">number used to make filters unique per main window</param>
		/// <param name="publication">The publication to get the information from (or
		/// null to keep the defaults)</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="printDateTime">Date/Time of the printing</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="btWs">Backtranslation WS</param>
		/// ------------------------------------------------------------------------------------
		public ScripturePublication(FwStyleSheet stylesheet, int filterInstance,
			IPublication publication, TeViewType viewType, DateTime printDateTime,
			IHelpTopicProvider helpTopicProvider, IApp app, int btWs) :
			base(stylesheet, publication, printDateTime, true, viewType == TeViewType.Correction,
			helpTopicProvider)
		{
			AccessibleName = TeEditingHelper.ViewTypeString(viewType);

			m_filterInstance = filterInstance;
			m_app = app;
			m_viewType = viewType;

			m_BackTranslationWS = btWs;

			ApplyBookFilterAndCreateDivisions();
			//if (IsBackTranslation && Options.UseInterlinearBackTranslation)
			//    BackColor = TeResourceHelper.ReadOnlyTextBackgroundColor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the book filter and creates divisions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ApplyBookFilterAndCreateDivisions()
		{
			m_bookFilter = m_cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);
			AddDivisionsForBooks();
		}
		#endregion

		#region  Support for segment BT
		/// <summary>
		/// Override to restore prompt selection if relevant (for segmented BT).
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			using (new PromptSelectionRestorer(RootBox))
				base.OnKeyPress(e);
		}

		#endregion

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the main stream of the first division
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwLayoutStream FirstMainStream
		{
			get
			{
				DivisionLayoutMgr div = m_divisions.Count > 0 ? m_divisions[0] : null;
				return (div != null) ? div.MainLayoutStream : null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the divisions for books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddDivisionsForBooks()
		{
			// remember which stream had focus
			int iFocusedStream = DivisionIndexForMainStream(FocusedStream);
			if (iFocusedStream == -1)
				iFocusedStream = DivisionIndexForMainStream(FirstMainStream);

			// ENHANCE: keep divisions for books that are still in the filter
			foreach (DivisionLayoutMgr div in m_divisions)
			{
				//int hvoRoot, frag;
				//IVwViewConstructor vc;
				//IVwStylesheet stylesheet;
				//div.MainRootBox.GetRootObject(out hvoRoot, out vc, out frag, out stylesheet);
				div.Dispose();
			}
			m_divisions.Clear();
			if (m_sharedStreams.Count > 0 && m_bookFilter.BookHvos.Count == 0)
			{
				foreach (IVwRootBox rootb in m_sharedStreams)
					rootb.Close();
				m_sharedStreams.Clear();
			}
			FocusedStream = null;

			// We better "show" something -- really nothing
			if (m_bookFilter.BookHvos.Count == 0)
			{
				DivisionLayoutMgr div = new TeDivisionLayoutMgr(
					new EmptyTePrintLayoutConfigurer(m_cache, m_stylesheet, m_viewType),
					m_publication.DivisionsOS[0], m_filterInstance, 1, false);
				div.Name = "Empty";
				AddDivision(div);
				iFocusedStream = 0;
			}
			else
			{
				int iBook = 0;
				foreach (int bookHvo in m_bookFilter.BookHvos)
				{
					// These strings are for debugging/crash handling only, so there is no need to
					// localize them.
					string name = string.Format("{0}_{1}", AccessibleName, iBook);
					var portion = IsBackTranslation ? PrintLayoutPortion.AllContent : PrintLayoutPortion.TitleAndIntro;
					DivisionLayoutMgr div = GetDivisionLayoutMgr(portion, bookHvo);
					div.Name = IsBackTranslation ? name : name + "_Intro";
					AddDivision(div);

					// for back translations we use only one division per book
					if (!IsBackTranslation)
					{
						div = GetDivisionLayoutMgr(PrintLayoutPortion.ScriptureSections, bookHvo);
						div.Name = name + "_Main";
						AddDivision(div);
					}
					iBook++;
				}
			}

			// restore the focused stream
			if (iFocusedStream > -1 && iFocusedStream < m_divisions.Count)
				FocusedStream = m_divisions[iFocusedStream].MainLayoutStream;

			//// Add new books
			//foreach (int bookId in m_bookFilter.BookIds)
			//{
			//    int i;
			//    for (i = 0; i < Divisions.Count; i++)
			//    {
			//        TeDivisionLayoutMgr div = Divisions[i] as TeDivisionLayoutMgr;
			//        if (div.HvoBook == bookId)
			//            break;
			//    }

			//    if (i >= Divisions.Count)
			//    {
			//        // we didn't find divisions for this book, so we have to add it.
			//        AddDivision(GetDivisionLayoutMgr(true));
			//        AddDivision(GetDivisionLayoutMgr(false));
			//    }
			//}

			//// Remove any divisions of books that were removed from the book filter
			//foreach (int bookId in m_previousBookList)
			//{
			//    if (!m_bookFilter.BookIds.Contains(bookId))
			//    {
			//    }
			//}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a DivisionLayoutMgr for a division.
		/// </summary>
		/// <param name="divisionPortion">portion of book to be layed out in divsion</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <returns></returns>
		/// <remarks>
		/// Allows sub classes to override the type of DivisionLayoutMgr that is created.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual DivisionLayoutMgr GetDivisionLayoutMgr(PrintLayoutPortion divisionPortion,
			int hvoBook)
		{
			// An intro division always has just one column
			IPubDivision pubDivision = m_publication.DivisionsOS[0];
			int numberOfColumns = divisionPortion == PrintLayoutPortion.TitleAndIntro ? 1 : pubDivision.NumColumns;

			IVwLayoutStream sharedStream = GetSharedSubstream();

			return new TeDivisionLayoutMgr(
				GetPrintLayoutConfigurer(divisionPortion, hvoBook, sharedStream, m_BackTranslationWS),
				pubDivision, m_filterInstance, numberOfColumns, divisionPortion == PrintLayoutPortion.TitleAndIntro);
		}

		/// <summary>
		/// Indicates the kind of content (normal, BT, or segmented BT) contained in the view.
		/// </summary>
		public StVc.ContentTypes ContentType
		{
			get
			{
				if ((m_viewType & TeViewType.BackTranslation) == 0)
					return StVc.ContentTypes.kctNormal;
				return Options.UseInterlinearBackTranslation ? StVc.ContentTypes.kctSegmentBT : StVc.ContentTypes.kctSimpleBT;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the shared substream.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IVwLayoutStream GetSharedSubstream()
		{
			IVwLayoutStream sharedStream;
			if (m_sharedStreams.Count == 0)
			{
				// Add shared footnote stream
				int hvoScr = m_cache.LangProject.TranslatedScriptureOA.Hvo;
				sharedStream = VwLayoutStreamClass.Create();
				SetAccessibleStreamName(sharedStream, AccessibleName + "_SharedStream");

				IVwRootBox rootbox = (IVwRootBox)sharedStream;
				rootbox.DataAccess = new ScrBookFilterDecorator(m_cache, m_filterInstance);

				m_footnoteVc = new FootnoteVc(TeStVc.LayoutViewTarget.targetPrint,
					m_filterInstance);
				m_footnoteVc.Cache = m_cache;
				m_footnoteVc.DefaultWs = ((m_viewType & TeViewType.BackTranslation) != 0 ?
					BackTranslationWS : ViewConstructorWS);
				m_footnoteVc.ContentType = ContentType;
				rootbox.SetRootObject(hvoScr, m_footnoteVc, (int)FootnoteFrags.kfrScripture,
					m_stylesheet);

				AddSharedSubstream(sharedStream);
			}

			// TODO: handle multiple shared streams
			sharedStream = m_sharedStreams[0];
			return sharedStream;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print layout configurer.
		/// </summary>
		/// <param name="divisionPortion">portion of book to be layed out in division</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="ws">The writing system.</param>
		/// <returns>A print layout configurer</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual TePrintLayoutConfig GetPrintLayoutConfigurer(PrintLayoutPortion divisionPortion,
			int hvoBook, IVwLayoutStream sharedStream, int ws)
		{
			return new TePrintLayoutConfig(m_cache, m_stylesheet, m_publication,
				m_viewType, m_filterInstance, m_printDateTime, divisionPortion, hvoBook,
				sharedStream, ws);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view type for this ScripturePublication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeViewType ViewType
		{
			get
			{
				CheckDisposed();
				return m_viewType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the EditingHelper as a TeEditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper TeEditingHelper
		{
			get
			{
				CheckDisposed();
				return ((PubEditingHelper)EditingHelper).DecoratedEditingHelper as TeEditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default WS for the view constructors in the publication
		///
		/// NOTE: If we ever support a publication that has view constructors that need to have
		/// different writing systems then this won't work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ViewConstructorWS
		{
			get
			{
				CheckDisposed();
				return m_divisions.Count > 0 ? ((TeDivisionLayoutMgr)m_divisions[0]).ViewConstructorWS
					: m_cache.DefaultVernWs;
			}
			set
			{
				CheckDisposed();

				foreach (TeDivisionLayoutMgr mgr in m_divisions)
					mgr.ViewConstructorWS = value;
				RefreshDisplay();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the back translation WS for the view constructors in the publication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BackTranslationWS
		{
			get
			{
				CheckDisposed();
				return m_BackTranslationWS;
			}
			set
			{
				CheckDisposed();

				m_BackTranslationWS = value;

				Configure();
				foreach (TeDivisionLayoutMgr mgr in m_divisions)
					mgr.BackTranslationWS = value;
				RefreshDisplay();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font size (in millipoints) to be used when the publication doesn't specify
		/// it explicitly.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override int DefaultFontSize
		{
			get
			{
				if (m_publication.DivisionsOS[0].NumColumns == 2)
					return base.DefaultFontSize;

				if (m_publication.PageHeight != 0)
					return kStdOneColumnFontSize;

				if (m_publication.PaperHeight == 0)
					return base.DefaultFontSize;

				CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				List<PubPageInfo> pageSizes = TePublicationsInit.GetPubPageSizes(m_publication.Name,
					ws.Id);
				foreach (PubPageInfo pageInfo in pageSizes)
				{
					if (m_publication.PaperHeight == pageInfo.Height &&
						m_publication.PaperWidth == pageInfo.Width)
					{
						return kStdOneColumnFontSize;
					}
				}
				return base.DefaultFontSize;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the line height (in millipoints) to be used when the publication doesn't
		/// specify it explicitly. (Value is negative for "exact" line spacing.)
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override int DefaultLineHeight
		{
			get
			{
				if (m_publication.DivisionsOS[0].NumColumns == 2)
					return base.DefaultLineHeight;

				if (m_publication.PageHeight != 0)
					return kStdOneColumnLineHeight;

				if (m_publication.PaperHeight == 0)
					return base.DefaultLineHeight;

				CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				List<PubPageInfo> pageSizes = TePublicationsInit.GetPubPageSizes(m_publication.Name,
					ws.Id);
				foreach (PubPageInfo pageInfo in pageSizes)
				{
					if (m_publication.PaperHeight == pageInfo.Height &&
						m_publication.PaperWidth == pageInfo.Width)
					{
						return kStdOneColumnLineHeight;
					}
				}
				return base.DefaultLineHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location tracker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILocationTracker LocationTracker
		{
			get { return this; }
		}
		#endregion

		#region Overridden methods (of PublicationControl)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display.
		/// </summary>
		/// <param name="fPreserveSelection">True to save the selection and restore it afterwards,
		/// false otherwise</param>
		/// ------------------------------------------------------------------------------------
		protected override void RefreshDisplay(bool fPreserveSelection)
		{
			SelectionHelper selHelper = null;
			int bookIndex = -1, sectionIndex = -1;
			if (fPreserveSelection)
			{
				selHelper = TeEditingHelper.CurrentSelection;
				bookIndex = TeEditingHelper.BookIndex;
				sectionIndex = TeEditingHelper.SectionIndex;
			}

			// Save where the selection is so we can try to restore it after reconstructing.
			using (new SuspendDrawing(this))
				AddDivisionsForBooks();

			base.RefreshDisplay(false); // Keep the base implementation from attempting to preserve the selection

			if (selHelper != null && bookIndex >= 0 && sectionIndex >= 0)
			{
				LocationTracker.SetBookAndSection(selHelper,
					SelectionHelper.SelLimitType.Anchor, bookIndex, sectionIndex);
				selHelper.RestoreSelectionAndScrollPos();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override of UserControl.OnPaint
		/// </summary>
		/// <param name="e">Info needed to paint</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_bookFilter.BookCount > 0)
				base.OnPaint(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a instance of the EditingHelper used to process editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override RootSiteEditingHelper GetInternalEditingHelper()
		{
			TeEditingHelper helper = new TeEditingHelper(this, m_cache, m_filterInstance,
				m_viewType, m_app);
			helper.ContentType = ContentType;
			return helper;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Perform TE-specific user view activation.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void ActivateView()
		{
			CheckDisposed();

			base.ActivateView();

			if (TheMainWnd != null)
			{
				TheMainWnd.InitStyleComboBox();
				TheMainWnd.UpdateWritingSystemSelectorForSelection(FocusedRootBox);
			}

//			// If this user view is a back translation, then it needs to be refreshed to update the
//			// user prompts for anything that has changed in the previous.
//			if ((m_viewType & TeViewType.BackTranslation) != 0)
//				RefreshDisplay();
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to pass handling of problem deletion on to editing helper.
		/// </summary>
		/// <param name="sel">The sel.</param>
		/// <param name="dpt">The DPT.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			CheckDisposed();

			return TeEditingHelper.OnProblemDeletion(sel, dpt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle insertion of paragraphs (i.e., from clipboard) with properties that don't
		/// match the properties of the paragraph where they are being inserted. This gives us
		/// the opportunity to create/modify the DB structure to recieve the paragraphs being
		/// inserted and to reject certain types of paste operations (such as attempting to
		/// paste a book).
		/// </summary>
		/// <param name="rootBox">the sender</param>
		/// <param name="ttpDest">properties of destination paragraph</param>
		/// <param name="cPara">number of paragraphs to be inserted</param>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="tssTrailing">Text of an incomplete paragraph to insert at end (with
		/// the properties of the destination paragraph.</param>
		/// <returns>One of the following:
		/// kidprDefault - causes the base implementation to insert the material as part of the
		/// current StText in the usual way;
		/// kidprFail - indicates that we have decided that this text should not be pasted at
		/// this location at all, causing entire operation to roll back;
		/// kidprDone - indicates that we have handled the paste ourselves, inserting the data
		/// wherever it ought to go and creating any necessary new structure.</returns>
		/// ------------------------------------------------------------------------------------
		public override VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox rootBox,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrcArray, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			if (TeEditingHelper == null || DivisionIndexForMainStream(rootBox as IVwLayoutStream) < 0)
				return VwInsertDiffParaResponse.kidprFail;

			return TeEditingHelper.InsertDiffParas(rootBox, ttpDest, cPara, ttpSrcArray,
				tssParas, tssTrailing);
		}

		/// <summary> see OnInsertDiffParas </summary>
		public override VwInsertDiffParaResponse OnInsertDiffPara(IVwRootBox rootBox,
			ITsTextProps ttpDest, ITsTextProps ttpSrc, ITsString tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			return OnInsertDiffParas(rootBox, ttpDest, 1, new ITsTextProps[] { ttpSrc },
				new ITsString[] { tssParas }, tssTrailing);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the annotations window is gaining focus, then we don't want the print layout's
		/// range selections to be hidden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override VwSelectionState GetNonFocusedSelectionState(Control windowGainingFocus)
		{
			if (windowGainingFocus != null)
			{
				// It's not the greatest thing to check if the notes window is gaining focus
				// by checking the type's name, but it works and it saves having to add
				// a reference to that DLL just to compare the form's type.
				Form frm = windowGainingFocus.FindForm();
				if (frm != null && frm.GetType().Name == "NotesMainWnd")
					return VwSelectionState.vssOutOfFocus;
			}

			return base.GetNonFocusedSelectionState(windowGainingFocus);
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates any of the Edit/Find,replace menu items based on the selected view
		/// </summary>
		/// <param name="itemProps">The item props.</param>
		/// ------------------------------------------------------------------------------------
		private bool UpdateFindReplaceMenuItem(TMItemProperties itemProps)
		{
			if (itemProps != null)
			{
				itemProps.Update = true;
				itemProps.Enabled = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Find menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditFind(object args)
		{
			return UpdateFindReplaceMenuItem(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Replace menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditReplace(object args)
		{
			return UpdateFindReplaceMenuItem(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Find Next menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditFindNext(object args)
		{
			return UpdateFindReplaceMenuItem(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Find Prev menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditFindPrev(object args)
		{
			return UpdateFindReplaceMenuItem(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInformationBar(object args)
		{
			TeEditingHelper editingHelper = TeEditingHelper;
			SelLevInfo selLevInfo;
			if (editingHelper.CurrentSelection != null &&
				editingHelper.CurrentSelection.GetLevelInfoForTag(
				TePrintLayoutConfig.GetDependentRootTag(m_cache), out selLevInfo))
			{
				// we're in a footnote. Let's pretend we're in a title.
				editingHelper.SetInformationBarForSelection(ScrBookTags.kflidTitle,
					m_cache.ServiceLocator.GetObject(selLevInfo.hvo).Owner.Hvo);
			}
			else
				editingHelper.SetInformationBarForSelection();

			// Update the the GoTo Reference control in the information tool bar.
			// !! This is found in the same method in DraftView.cs
			editingHelper.UpdateGotoPassageControl();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse button up event
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (e.Button == MouseButtons.Right)
				ShowContextMenu(e.Location);
		}
		#endregion

		#region Implementation of abstract methods from PublicationControl
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the context menu.
		/// </summary>
		/// <param name="loc">The loc.</param>
		/// ------------------------------------------------------------------------------------
		public override void ShowContextMenu(Point loc)
		{
			FwMainWnd teMainWnd = TheMainWnd;
			if (teMainWnd == null || teMainWnd.TMAdapter == null)
				return;

			string menuName;
			if (TeEditingHelper.IsPictureSelected)
				menuName = "cmnuDraftViewPicture";
			else
				menuName = "cmnuDraftViewNormal";

			Point pt = PointToScreen(loc);
			teMainWnd.TMAdapter.PopupMenu(menuName, pt.X, pt.Y);
		}
		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Not used
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the possible message targets, i.e. the view(s) we are showing
		/// </summary>
		/// <returns>Message targets</returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			// FwMainWnd returns our window as a view, so no targets are added at this point.
			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			targets.Add(this);
			return targets.ToArray();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Message handling priority
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion

		#region ILocationTracker Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current book, or null if there is no current book (e.g. no
		/// selection or empty view).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The current book, or null</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IScrBook GetBook(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			if (selHelper == TeEditingHelper.CurrentSelection && FocusedDivision != null)
			{
				int hvo = ((DivisionLayoutMgr)FocusedDivision).Configurer.MainObjectId;
				// book may have been deleted - stale reference can happen if this view is not active when book
				// is deleted
				ICmObject book;
				if (Cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out book))
					return book as IScrBook;
				return null;
			}

			Debug.Assert(selHelper.Selection == null || selHelper.Selection.IsValid,
				"Selection isn't valid");
			if (selHelper.Selection != null && selHelper.Selection.RootBox != null)
			{
				int hvo;
				IVwViewConstructor vc;
				int frag;
				IVwStylesheet styleSheet;
				selHelper.Selection.RootBox.GetRootObject(out hvo, out vc, out frag, out styleSheet);

				// If the selection is in a smushed footnote, hvo is < 0, vc is FootnoteVc and
				// frag is FootnoteFrags.kfrRootInPageSeq
				if (frag == (int)FootnoteFrags.kfrRootInPageSeq && m_divisions.Count > 0)
				{
					// All divisions use a similar TePrintLayoutConfig, so it doesn't matter
					// which division we use to get it.
					TePrintLayoutConfig configurer = (TePrintLayoutConfig)m_divisions[0].Configurer;
					// get the footnotes that are displayed in this smushed footnote
					int nbrNotes = configurer.DataAccess.get_VecSize(hvo, configurer.DependentRootTag);
					int[] footnoteHvos;
					using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(nbrNotes))
					{
						configurer.DataAccess.VecProp(hvo, configurer.DependentRootTag,
							nbrNotes, out nbrNotes, arrayPtr);
						footnoteHvos = MarshalEx.NativeToArray<int>(arrayPtr, nbrNotes);
					}

					// currently we don't have multiple books on the same page, so it doesn't
					// matter which footnote we use. Just use the first one and get its
					// parent and that is the book!
					// ENHANCE (EberhardB): This code doesn't work if we have more than one
					// book on a page.
					if (footnoteHvos.Length > 0)
					{
						IScrFootnote footnote =
							m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetObject(footnoteHvos[0]);
						return footnote.Owner as IScrBook;
					}
				}
				// book may have been deleted - stale reference can happen if this view is not active when book
				// is deleted
				ICmObject book;
				if (Cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out book))
					return book as IScrBook;
				return null;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the current book (relative to book filter), or -1 if there is no
		/// current book (e.g. no selection or empty view).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>
		/// Index of the current book, or -1 if there is no current book.
		/// </returns>
		/// <remarks>The returned value is suitable for making a selection.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int GetBookIndex(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			try
			{
				IScrBook book = GetBook(selHelper, selLimitType);
				if (book == null)
					return -1;

				return m_bookFilter.GetBookIndex(book);
			}
			catch (KeyNotFoundException)
			{
				// This can happen if the book was deleted but the selection doesn't know it yet.
				return -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current section, or null if we're not in a section (e.g. the IP is in a title).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The current section, or null</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IScrSection GetSection(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			int sectionLevel = GetSectionLevel(selHelper, selLimitType);
			int hvoSection = (sectionLevel >= 0) ?
				selHelper.GetLevelInfo(selLimitType)[sectionLevel].hvo : -1;
			// section may have been deleted - stale reference can happen if this view is not active when book
			// is deleted
			ICmObject section;
			if (hvoSection >= 0 && Cache.ServiceLocator.ObjectRepository.TryGetObject(hvoSection, out section))
				return section as IScrSection;
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section relative to the book, or -1 if we're not in a section.
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The section index in book.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetSectionIndexInBook(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			IScrSection section = GetSection(selHelper, selLimitType);
			if (section == null)
				return -1;

			return section.IndexInOwner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the section (relative to RootBox), or -1 if we're not in a section
		/// (e.g. the IP is in a title).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>
		/// Index of the section, or -1 if we're not in a section.
		/// </returns>
		/// <remarks>The returned value is suitable for making a selection.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int GetSectionIndexInView(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			int sectionLevel = GetSectionLevel(selHelper, selLimitType);
			return sectionLevel >= 0 ? selHelper.GetLevelInfo(selLimitType)[sectionLevel].ihvo : -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set both book and section. Don't make a selection; typically the caller will proceed
		/// to do that.
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <param name="iBook">The index of the book (in the book filter).</param>
		/// <param name="iSection">The index of the section (relative to
		/// <paramref name="iBook"/>), or -1 for a selection that is not in a section (e.g.
		/// title).</param>
		/// <remarks>This method should change only the book and section levels of the
		/// selection, but not any other level.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void SetBookAndSection(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType, int iBook, int iSection)
		{
			if (selHelper == null || iBook < 0 || !Visible)
				return;

			IScrBook book = m_bookFilter.GetBook(iBook);
			int[] hvoAllSections = book.SectionsOS.ToHvoArray();
			if (iSection < 0 || iSection >= hvoAllSections.Length)
				return; // We won't be able to find our section (TE-8242)

			foreach (DivisionLayoutMgr div in Divisions)
			{
				if (div.Configurer.MainObjectId != book.Hvo)
					continue; // We don't care about the division if it doesn't use our book

				FocusedStream = div.MainLayoutStream;

				TePrintLayoutConfig configurer = div.Configurer as TePrintLayoutConfig;
				if (configurer == null)
					continue;

				int hvoSection = hvoAllSections[iSection];
				if (!configurer.MatchesCriteria(hvoSection))
				{
					// This division doesn't contain the desired section, so we try
					// the following section
					continue;
				}

				// Find the index of the section in the filtered section list,
				// i.e. in the view
				int iSectionInView = -1;
				int iSectionLim = Math.Min(hvoAllSections.Length, iSection + 1);
				for (int iSectionInBook = 0; iSectionInBook < iSectionLim;
					iSectionInBook++)
				{
					if (configurer.MatchesCriteria(hvoAllSections[iSectionInBook]))
						iSectionInView++;
				}

				if (iSectionInView < 0)
					continue;

				selHelper.GetLevelInfo(selLimitType)[selHelper.GetNumberOfLevels(selLimitType) - 1].tag =
					ScrBookTags.kflidSections;
				selHelper.GetLevelInfo(selLimitType)[selHelper.GetNumberOfLevels(selLimitType) - 1].ihvo =
					iSectionInView;

				// We found the division that used the book we care about so lets quit
				return;
			}

			ApplicationException e = new ApplicationException(
				"Can't find division for given book and index");
			e.Data.Add("iBook", iBook);
			e.Data.Add("iSection", iSection);
			e.Data.Add("View name", Name);
			throw e;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of levels for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>Number of levels</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetLevelCount(int tag)
		{
			// Print layout lays out each book (or part of a book) in a separate division, so
			// the rootsites in the print layout don't have a level for the books. Therefore,
			// we subtract 1 to account for that missing level.
			return LocationTrackerImpl.GetLevelCountForTag(tag, TeEditingHelper.ContentType) - 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the level for the given tag based on the current selection.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>Index of the level, or -1 if unknown level.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetLevelIndex(int tag)
		{
			return LocationTrackerImpl.GetLevelIndexForTag(tag, TeEditingHelper.ContentType);
		}
		#endregion

		#region Other methods
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the index of the level for the given tag based on <paramref name="selHelper"/>.
		///// </summary>
		///// <param name="selHelper">The selection helper.</param>
		///// <param name="tag">The tag.</param>
		///// <returns>
		///// Index of the level, or -1 if unknown level.
		///// </returns>
		///// ------------------------------------------------------------------------------------
		//public int GetLevelIndex(SelectionHelper selHelper, int tag)
		//{
		//    int levelIndex = LocationTrackerImpl.GetLevelIndexForTag(tag, IsBackTranslation);

		//    //if (selHelper != null &&
		//    //    m_cache.GetClassOfObject(selHelper.LevelInfo[levelIndex].hvo) == CmTranslation.kClassId)
		//    //{
		//    //    levelIndex++;
		//    //}
		//    return levelIndex;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the containing FwMainWnd.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "ctrl is a reference")]
		private FwMainWnd TheMainWnd
		{
			get
			{
				CheckDisposed();

				Control ctrl = Parent;
				while (ctrl != null)
				{
					if (ctrl is FwMainWnd)
						return (FwMainWnd)ctrl;

					ctrl = ctrl.Parent;
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this print layout view displays back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsBackTranslation
		{
			get { return (m_viewType & TeViewType.BackTranslation) != 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the level for the section tag.
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The level for the section, or -1 if there is no section tag (e.g. in a
		/// title)</returns>
		/// ------------------------------------------------------------------------------------
		private int GetSectionLevel(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			if (selHelper == null)
				return -1;

			int sectionTag = -1;
			if (selHelper == TeEditingHelper.CurrentSelection && FocusedDivision != null)
			{
				TePrintLayoutConfig configurer =
					((DivisionLayoutMgr)FocusedDivision).Configurer as TePrintLayoutConfig;
				if (configurer != null)
					sectionTag = ScrBookTags.kflidSections;
			}

			if (sectionTag < 0 && selHelper.Selection != null && selHelper.Selection.RootBox != null)
			{
				int iDiv = DivisionIndexForMainStream(selHelper.Selection.RootBox as IVwLayoutStream);
				if (iDiv >= 0)
					sectionTag = ScrBookTags.kflidSections;
			}

			if (sectionTag < 0)
				return -1;
			return selHelper.GetLevelForTag(sectionTag, selLimitType);
		}
		#endregion
	}
}
