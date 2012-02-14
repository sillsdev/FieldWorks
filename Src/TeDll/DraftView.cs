// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2002' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftView.cs
// Responsibility: TE Team
//
// <remarks>
// Implements the draft view editing pane, either vernacular or back translation.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of the draft view (formerly SeDraftWnd)
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class DraftView : DraftViewBase, ISelectableView
	{
		#region Data members
		private ViewWrapper m_draftViewWrapper;
		private SelectionHelper m_selectionHelper;
		/// <summary></summary>
		protected IHelpTopicProvider m_helpTopicProvider;

		/// <summary><c>true</c> to layout data in a table</summary>
		protected bool m_fShowInTable;

		/// <summary><c>true</c> if a picture is selected</summary>
		private bool m_pictureSelected = false;

		/// <summary>Hvo of selected translation, or 0 if no status box is selected</summary>
		private int m_selectedTransHvo;
		private int m_prevSelectedParagraph;
		private int m_prevAnchorPosition;

		/// <summary>
		/// base caption string that this client window should display in the info bar
		/// </summary>
		private string m_baseInfoBarCaption;
		private IParagraphCounter m_paraCounter;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DraftView class as a draft view editing pane,
		/// for either vernacular or back translation.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The tag that identifies the book filter instance.</param>
		/// <param name="app">The application.</param>
		/// <param name="viewName">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if view is to be editable.</param>
		/// <param name="fShowInTable"><c>true</c> if the paragraphs are to be displayed in a
		/// table.</param>
		/// <param name="fMakeRootAutomatically"><c>true</c> to make the root automatically
		/// when the window handle is created.</param>
		/// <param name="viewType">Bit-flags indicating type of view.</param>
		/// <param name="btWs">The back translation writing system (if needed).</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public DraftView(FdoCache cache, int filterInstance, IApp app, string viewName,
			bool fEditable, bool fShowInTable, bool fMakeRootAutomatically,
			TeViewType viewType, int btWs, IHelpTopicProvider helpTopicProvider) :
			base(cache, filterInstance, app, viewName, fEditable, viewType, btWs)
		{
			Debug.Assert((viewType & TeViewType.Draft) != 0);
			m_helpTopicProvider = helpTopicProvider;
			AutoScroll = false;
			m_fShowInTable = fShowInTable;
			m_fMakeRootWhenHandleIsCreated = fMakeRootAutomatically;
		}


		#endregion

		#region ISettings overrides
		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Load settings from the registry
		/// </summary>
		/// <param name="key">Location in the registry</param>
		///-------------------------------------------------------------------------------------
		protected override void OnLoadSettings(RegistryKey key)
		{
			base.OnLoadSettings(key);

			object objTemp = key.GetValue(Name);
			if (objTemp != null)
			{
				// Restore the selection
				try
				{
					using (MemoryStream stream = new MemoryStream((byte[])objTemp))
					{
						m_selectionHelper =
							(SelectionHelper)Persistence.DeserializeFromBinary(stream);
					}

					// Determine whether or not the FwMainWnd is a copy of another.
					bool fMainWndIsCopy =
						(TheMainWnd != null ? ((TeMainWnd)TheMainWnd).WindowIsCopy : false);

					if (m_selectionHelper != null)
					{
						// the selection helper book information needs to be converted to the
						// filter information
						ConvertBookTagAndIndex(m_selectionHelper, SelectionHelper.SelLimitType.Anchor);
						ConvertBookTagAndIndex(m_selectionHelper, SelectionHelper.SelLimitType.End);

						if (!fMainWndIsCopy && m_selectionHelper.NumberOfLevels > 2)
						{
							// Level where we find paragraph depends on type of content
							int paraLevel = 0;
							if (ContentType == StVc.ContentTypes.kctSimpleBT)
								paraLevel ++;
							else if (ContentType == StVc.ContentTypes.kctSegmentBT)
								paraLevel += 2;

							if (m_selectionHelper.LevelInfo[paraLevel].tag ==
								StTextTags.kflidParagraphs)
							{
								if (ContentType != StVc.ContentTypes.kctSegmentBT)
								{
									// Set IP to the beginning of the section contents
									m_selectionHelper.IchAnchor = 0;
									m_selectionHelper.LevelInfo[paraLevel].ihvo = 0;
									m_selectionHelper.LevelInfo[paraLevel + 1].ihvo = 0;
								}
								// In the segment BT view we don't currently force to top of section.
								// Doing so is tricky because we need the first editable segment, not
								// just the first segment.
							}
							else
							{
								// We are in a picture.  When in a picture the level
								// zero is not useful and seems to keep the views from
								// making the selection.
								SelectionHelper tempHelper = new SelectionHelper();
								tempHelper.TextPropId = StTxtParaTags.kflidContents;
								tempHelper.NumberOfLevels = 4;
								for (int i = 0; i < tempHelper.NumberOfLevels - 1; i++)
									tempHelper.LevelInfo[i] = m_selectionHelper.LevelInfo[i + 1];

								m_selectionHelper = tempHelper;
							}
						}

						m_selectionHelper.ReduceToIp(SelectionHelper.SelLimitType.Anchor);
					}
				}
				catch
				{
					// go on with life
				}

				// JohnT: I added this because, after some other changes I needed to make,
				// the selection was not getting restored, because the selection helper
				// was null during the only OnGotFocus call that happened (when not running
				// with a breakpoint in OnGotFocus!). It might be sufficient now to just
				// always do it here. However, it may also be that there are other
				// circumstances in which it won't work here, perhaps because the window's
				// handle has not yet been created.
				if (IsHandleCreated && (m_selectionHelper == null ||
					m_selectionHelper.MakeBest(this, true) == null))
				{
					try
					{
						m_rootb.MakeSimpleSel(true, true, false, true);
					}
					catch
					{
						// ignore any errors! This can happen if, for example, the view is empty.
					}
				}
				m_selectionHelper = null;
			}
			else
			{
				try
				{
					if (m_rootb != null)
						m_rootb.MakeSimpleSel(true, true, false, true);
				}
				catch
				{
					// ignore any errors! This can happen if, for example, the view is empty.
				}
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Save passage to the registry
		/// </summary>
		/// <param name="key">Location in the registry</param>
		///-------------------------------------------------------------------------------------
		protected override void OnSaveSettings(RegistryKey key)
		{
			base.OnSaveSettings(key);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, this);
			if (selectionHelper != null)
			{
				try
				{
					// Convert all of the filter tags to scripture book tags.
					ReplaceBookTagAndIndex(selectionHelper, SelectionHelper.SelLimitType.Anchor);
					ReplaceBookTagAndIndex(selectionHelper, SelectionHelper.SelLimitType.End);

					key.SetValue(Name, Persistence.SerializeToBinary(selectionHelper).ToArray());
				}
				catch
				{
					// Ignore any errors we get...
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the scripture book tag to a filter tag.  Also, convert book indices to
		/// filtered book indices.  This is done when loading a selection to make it work
		/// in the context of the book filter.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="selType"></param>
		/// ------------------------------------------------------------------------------------
		private void ConvertBookTagAndIndex(SelectionHelper helper,
			SelectionHelper.SelLimitType selType)
		{
			SelLevInfo[] info = helper.GetLevelInfo(selType);
			int bookPos = info.Length - 1;
			if (info[bookPos].tag == ScriptureTags.kflidScriptureBooks)
			{
				info[bookPos].tag = BookFilter.Tag;
				info[bookPos].ihvo = BookFilter.GetBookIndex(
					m_fdoCache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(info[bookPos].hvo));
				helper.SetLevelInfo(selType, info);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace the filter book tag with a scrpiture books tag.  This is done before
		/// persisting the selection because the filter tag is not valid across sessions.
		/// Also, any filtered book indices are replaced with real scripture book indices
		/// for the same reason.
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="selType"></param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceBookTagAndIndex(SelectionHelper helper,
			SelectionHelper.SelLimitType selType)
		{
			SelLevInfo[] info = helper.GetLevelInfo(selType);
			int bookPos = info.Length - 1;
			if (info[bookPos].tag == BookFilter.Tag)
			{
				info[bookPos].tag = ScriptureTags.kflidScriptureBooks;
				info[bookPos].ihvo = BookFilter.GetUnfilteredIndex(info[bookPos].ihvo);
				helper.SetLevelInfo(selType, info);
			}
		}
		#endregion

		#region ISelectableView implementation
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view and initializes the information bar and styles combo based on
		/// the draft view's selection.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public override void ActivateView()
		{
			base.ActivateView();

			try
			{
				if (m_rootb != null && m_rootb.Selection == null)
					m_rootb.MakeSimpleSel(true, true, false, true);
			}
			catch (COMException)
			{
				// ignore any errors! This can happen if, for example, the view is empty.
			}

			if (TheMainWnd != null)
			{
				TheMainWnd.InitStyleComboBox();
				TheMainWnd.UpdateWritingSystemSelectorForSelection(m_rootb);
			}

			if (EditingHelper != null && EditingHelper.CurrentSelection != null &&
				!IsSelectionVisible(EditingHelper.CurrentSelection.Selection))
			{
				MakeSelectionVisible(EditingHelper.CurrentSelection.Selection);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeactivateView()
		{
			CheckDisposed();

			Hide();
		}

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
		#endregion

		#region Event handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle hot link action - user clicked on hot link on the view.
		/// </summary>
		/// <param name="sender">Ignored. The view constructor that detected the click</param>
		/// <param name="strData">String-representation of the link, including the type of link
		/// and the GUID of the linked object</param>
		/// ------------------------------------------------------------------------------------
		public virtual void DoHotLinkAction(object sender, string strData)
		{
			CheckDisposed();

			// first char. of strData is type code - GUID will follow it.
			Guid objGuid = MiscUtils.GetGuidFromObjData(strData.Substring(1));

			IScrFootnote footnote;
			if (Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().TryGetObject(objGuid, out footnote))
				TheMainWnd.Mediator.SendMessage("FootnoteClick", footnote);
		}

		#endregion

		#region Menu and Toolbar Command Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool OnFilePrint(object args)
		{
			CheckDisposed();

			// We want the event to be handled by the main window
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Back Translation Unfinished menu option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationUnfinished(object args)
		{
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoChangeBackTransStatus", out undo, out redo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				Cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			{
				ICmTranslation trans = m_fdoCache.ServiceLocator.GetInstance<ICmTranslationRepository>().GetObject(
								TeEditingHelper.CurrentSelection.LevelInfo[0].hvo);
				SetBackTranslationStatus(trans, BackTranslationStatus.Unfinished);

				undoHelper.RollBack = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Back Translation Finished menu option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationFinished(object args)
		{
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoChangeBackTransStatus", out undo, out redo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				Cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			{
				ICmTranslation trans = m_fdoCache.ServiceLocator.GetInstance<ICmTranslationRepository>().GetObject(
					TeEditingHelper.CurrentSelection.LevelInfo[0].hvo);
				SetBackTranslationStatus(trans, BackTranslationStatus.Finished);

				undoHelper.RollBack = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Back Translation Checked menu option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationChecked(object args)
		{
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoChangeBackTransStatus", out undo, out redo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				Cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			{
				ICmTranslation trans = m_fdoCache.ServiceLocator.GetInstance<ICmTranslationRepository>().GetObject(
					TeEditingHelper.CurrentSelection.LevelInfo[0].hvo);
				SetBackTranslationStatus(trans, BackTranslationStatus.Checked);

				undoHelper.RollBack = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move to the next paragraph in the back translation that is unfinished.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationNextUnfinished(object args)
		{
			// Move to the next paragraph with an unfinished state and select it
			using (new WaitCursor(TheMainWnd))
			{
				MoveToNextTranslation(TeEditingHelper.CurrentSelection,
					trans => GetBackTranslationStatus(trans) == BackTranslationStatus.Unfinished);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move to the previous paragraph in the back translation that is unfinished.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationPrevUnfinished(object args)
		{
			// Move to the previous paragraph with an unfinished state and select it
			using (new WaitCursor(TheMainWnd))
			{
				MoveToPrevTranslation(TeEditingHelper.CurrentSelection,
					BackTranslationStatus.Unfinished);
			}
			return true;
		}
		#endregion

		#region Back translation helper utilites
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find and select the next translation meeting a given condition
		/// </summary>
		/// <param name="selection">The selection where to start the search.
		/// NOTE: The selection must have all of the info set in the LevelInfo (hvo, ihvo)</param>
		/// <param name="condition">Condition the cack translation must meet</param>
		/// ------------------------------------------------------------------------------------
		private void MoveToNextTranslation(SelectionHelper selection,
			Func<ICmTranslation, bool> condition)
		{
			SelLevInfo bookInfo;
			SelLevInfo paraInfo;
			SelLevInfo sectionInfo;
			bool fFoundBookLevel = selection.GetLevelInfoForTag(BookFilter.Tag, out bookInfo);
			bool fFoundSectionLevel = selection.GetLevelInfoForTag(
				ScrBookTags.kflidSections, out sectionInfo);
			int secLev = selection.GetLevelForTag(ScrBookTags.kflidSections);
			bool fFoundParaLevel = selection.GetLevelInfoForTag(
				StTextTags.kflidParagraphs, out paraInfo);

			if (!fFoundBookLevel || !fFoundParaLevel)
				return;

			// Look through all the books in the book filter
			int bookStartIndex = bookInfo.ihvo;
			int sectionStartIndex = 0;
			int sectionTag;
			int paraStartIndex = paraInfo.ihvo + 1;
			int paraIndex;

			if (fFoundSectionLevel)
			{
				// start with current section
				sectionStartIndex = sectionInfo.ihvo;
				sectionTag = selection.LevelInfo[secLev - 1].tag;
			}
			else
			{
				// no section, so this must be the title - Look through the title paragraphs
				IScrBook checkBook = BookFilter.GetBook(bookStartIndex);
				paraIndex = FindNextTranslationInText(checkBook.TitleOA, paraStartIndex, condition);
				if (paraIndex >= 0)
				{
					// select the title paragraph
					SetInsertionPoint(ScrBookTags.kflidTitle, bookStartIndex, 0, paraIndex);
					return;
				}
				// continue the search with the current book
				sectionTag = ScrSectionTags.kflidHeading;
				paraStartIndex = 0;
			}

			for (int bookIndex = bookStartIndex; bookIndex < BookFilter.BookCount; bookIndex++)
			{
				IScrBook checkBook = BookFilter.GetBook(bookIndex);
				if (bookIndex > bookStartIndex)
				{
					// Look through the title paragraphs
					paraIndex = FindNextTranslationInText(checkBook.TitleOA, 0, condition);
					if (paraIndex >= 0)
					{
						// select the title paragraph
						SetInsertionPoint(ScrBookTags.kflidTitle, bookIndex, 0, paraIndex);
						return;
					}
				}

				// Look through the sections in order.
				for (int sectionIndex = sectionStartIndex;
					sectionIndex < checkBook.SectionsOS.Count; sectionIndex++)
				{
					IScrSection checkSection = checkBook.SectionsOS[sectionIndex];

					// Look in the paragraphs (could be either content or heading)
					IStText text = (sectionTag == ScrSectionTags.kflidHeading) ?
						checkSection.HeadingOA : checkSection.ContentOA;
					paraIndex = FindNextTranslationInText(text, paraStartIndex, condition);
					if (paraIndex >= 0)
					{
						// select the paragraph
						SetInsertionPoint(sectionTag, bookIndex, sectionIndex, paraIndex);
						return;
					}

					// Look in the content paragraphs, if we haven't already
					if (sectionTag == ScrSectionTags.kflidHeading)
					{
						sectionTag = ScrSectionTags.kflidContent;
						paraIndex = FindNextTranslationInText(checkSection.ContentOA, 0, condition);
						if (paraIndex >= 0)
						{
							// select the content paragraph
							SetInsertionPoint(sectionTag, bookIndex, sectionIndex, paraIndex);
							return;
						}
					}

					sectionTag = ScrSectionTags.kflidHeading;
					paraStartIndex = 0;
				}
				sectionStartIndex = 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look for a translation with a matching status in a paragraph of an StText
		/// </summary>
		/// <param name="text">StText to search through</param>
		/// <param name="iParaStartSearch">Index of the paragraph where search starts</param>
		/// <param name="condition">Condition the CmTranslation must fulfill</param>
		/// <returns>an index of the found paragraph in the StText or -1 if not found</returns>
		/// ------------------------------------------------------------------------------------
		private static int FindNextTranslationInText(IStText text, int iParaStartSearch,
			Func<ICmTranslation, bool> condition)
		{
			// Look through all of the paragraphs in the StText.
			for (int paraIndex = iParaStartSearch; paraIndex < text.ParagraphsOS.Count; paraIndex++)
			{
				IStTxtPara para = (IStTxtPara)text.ParagraphsOS[paraIndex];

				// Get the translation for this paragraph. If it has the desired state then
				// return it.
				ICmTranslation trans = para.GetOrCreateBT();
				if (condition(trans))
					return paraIndex;
			}

			// It was not found in this StText
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find and select the previous translation with a given state
		/// </summary>
		/// <param name="selection">The selection where to start the search.
		/// NOTE: The selection must have all of the info set in the LevelInfo (hvo, ihvo)</param>
		/// <param name="searchStatus">Back translation status to search for</param>
		/// ------------------------------------------------------------------------------------
		private void MoveToPrevTranslation(SelectionHelper selection,
			BackTranslationStatus searchStatus)
		{
			SelLevInfo bookInfo;
			SelLevInfo paraInfo;
			SelLevInfo sectionInfo;
			bool fFoundBookLevel = selection.GetLevelInfoForTag(BookFilter.Tag, out bookInfo);
			bool fFoundSectionLevel = selection.GetLevelInfoForTag(
				ScrBookTags.kflidSections, out sectionInfo);
			int secLev = selection.GetLevelForTag(ScrBookTags.kflidSections);
			bool fFoundParaLevel = selection.GetLevelInfoForTag(
				StTextTags.kflidParagraphs, out paraInfo);

			if (!fFoundBookLevel || !fFoundParaLevel)
				return;

			// Look through all the books in the book filter
			int bookStartIndex = bookInfo.ihvo;
			int sectionStartIndex = -1;
			int sectionTag = ScrSectionTags.kflidContent;
			int paraStartIndex = paraInfo.ihvo - 1;
			int paraIndex;

			if (fFoundSectionLevel)
			{
				// start with current section
				sectionStartIndex = sectionInfo.ihvo;
				sectionTag = selection.LevelInfo[secLev - 1].tag;
			}
			else
			{
				// no section, so this must be the title - Look through the title paragraphs
				IScrBook checkBook = BookFilter.GetBook(bookStartIndex);
				paraIndex = FindPrevTranslationInText(checkBook.TitleOA, searchStatus,
					paraStartIndex);
				if (paraIndex >= 0)
				{
					// select the title paragraph
					SetInsertionPoint(ScrBookTags.kflidTitle, bookStartIndex, 0, paraIndex);
					return;
				}
				// continue the search with the previous book
				bookStartIndex--;
				paraStartIndex = -2;
			}

			for (int bookIndex = bookStartIndex; bookIndex >= 0 ; bookIndex--)
			{
				IScrBook checkBook = BookFilter.GetBook(bookIndex);
				if (sectionStartIndex == -1)
				{
					sectionStartIndex = checkBook.SectionsOS.Count - 1;
					sectionTag = ScrSectionTags.kflidContent;
				}

				// Look through the sections in reverse order.
				for (int sectionIndex = sectionStartIndex; sectionIndex >= 0; sectionIndex--)
				{
					IScrSection checkSection = checkBook.SectionsOS[sectionIndex];

					if (paraStartIndex == -2)
					{
						paraStartIndex = checkSection.ContentOA.ParagraphsOS.Count - 1;
						sectionTag = ScrSectionTags.kflidContent;
					}

					// Look in the paragraphs (could be either content or heading)
					IStText text = (sectionTag == ScrSectionTags.kflidHeading) ?
						checkSection.HeadingOA : checkSection.ContentOA;
					paraIndex = FindPrevTranslationInText(text, searchStatus, paraStartIndex);
					if (paraIndex >= 0)
					{
						// select the paragraph
						SetInsertionPoint(sectionTag, bookIndex, sectionIndex, paraIndex);
						return;
					}

					// Look in the heading paragraphs, if we haven't already
					if (sectionTag == ScrSectionTags.kflidContent)
					{
						sectionTag = ScrSectionTags.kflidHeading;
						int startHeadPara = checkSection.HeadingOA.ParagraphsOS.Count - 1;
						paraIndex = FindPrevTranslationInText(checkSection.HeadingOA, searchStatus,
							startHeadPara);
						if (paraIndex >= 0)
						{
							// select the heading paragraph
							SetInsertionPoint(sectionTag, bookIndex, sectionIndex, paraIndex);
							return;
						}
					}
					paraStartIndex = -2;
				}
				sectionStartIndex = -1;

				// Look through the title paragraphs
				int startTitlePara = checkBook.TitleOA.ParagraphsOS.Count - 1;
				paraIndex = FindPrevTranslationInText(checkBook.TitleOA, searchStatus, startTitlePara);
				if (paraIndex >= 0)
				{
					// select the title paragraph
					SetInsertionPoint(ScrBookTags.kflidTitle, bookIndex, 0, paraIndex);
					return;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look for a previous translation with a matching status in a paragraph of an StText
		/// </summary>
		/// <param name="text">StText to search through</param>
		/// <param name="searchStatus">status of the CmTranslation that we want to find</param>
		/// <param name="iParaStartSearch">Index of the paragraph where search starts</param>
		/// <returns>an index of the found paragraph in the StText or -1 if not found</returns>
		/// ------------------------------------------------------------------------------------
		private int FindPrevTranslationInText(IStText text, BackTranslationStatus searchStatus,
			int iParaStartSearch)
		{
			// Look through all of the paragraphs in the StText.
			for (int paraIndex = iParaStartSearch; paraIndex >= 0; paraIndex--)
			{
				IStTxtPara para = (IStTxtPara)text.ParagraphsOS[paraIndex];

				// Get the translation for this paragraph. If it has the desired state then
				// return it.
				ICmTranslation trans = para.GetOrCreateBT();
				if (GetBackTranslationStatus(trans) == searchStatus)
					return paraIndex;
			}

			// It was not found in this StText
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the status of a back translation object
		/// </summary>
		/// <param name="trans"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private BackTranslationStatus GetBackTranslationStatus(ICmTranslation trans)
		{
			Debug.Assert(trans.TypeRA.Guid == LangProjectTags.kguidTranBackTranslation);
			string status = trans.Status.get_String(ViewConstructorWS).Text;

			if (status == BackTranslationStatus.Checked.ToString())
				return BackTranslationStatus.Checked;
			if (status == BackTranslationStatus.Finished.ToString())
				return BackTranslationStatus.Finished;
			return BackTranslationStatus.Unfinished;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the status of a back translation object.
		/// </summary>
		/// <param name="trans"></param>
		/// <param name="status"></param>
		/// ------------------------------------------------------------------------------------
		protected void SetBackTranslationStatus(ICmTranslation trans, BackTranslationStatus status)
		{
			Debug.Assert(trans.TypeRA.Guid == LangProjectTags.kguidTranBackTranslation);
			trans.Status.set_String(ViewConstructorWS, status.ToString());
			// We shouldn't have to do this in the new FDO
			//m_fdoCache.MainCacheAccessor.PropChanged(null,
			//    (int)PropChangeType.kpctNotifyAll, trans.Hvo,
			//    CmTranslationTags.kflidStatus, 0, 1, 1);
		}
		#endregion

		#region Menu and toolbar command update handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBackTranslationUnfinished(object args)
		{
			ICmTranslation trans = GetBackTranslation;
			bool enable = (trans != null &&
				EditingHelper.Editable &&
				(GetBackTranslationStatus(trans) == BackTranslationStatus.Finished ||
				GetBackTranslationStatus(trans) == BackTranslationStatus.Checked));

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.ParentForm == FindForm())
			{
				itemProps.Enabled = enable;
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBackTranslationFinished(object args)
		{
			ICmTranslation trans = GetBackTranslation;
			bool enable = (trans != null &&
				GetBackTranslationStatus(trans) == BackTranslationStatus.Unfinished &&
				EditingHelper.Editable);

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.ParentForm == FindForm())
			{
				itemProps.Enabled = enable;
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBackTranslationChecked(object args)
		{
			ICmTranslation trans = GetBackTranslation;
			bool enable = (trans != null &&
				GetBackTranslationStatus(trans) == BackTranslationStatus.Finished &&
				EditingHelper.Editable);

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.ParentForm == FindForm())
			{
				itemProps.Enabled = enable;
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update handler for the Next Unfinished Paragraph button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBackTranslationNextUnfinished(object args)
		{
			bool enable = (GetBackTranslation != null && EditingHelper.Editable);

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.ParentForm == FindForm())
			{
				itemProps.Enabled = enable;
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update handler for the Prev Unfinished Paragraph button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBackTranslationPrevUnfinished(object args)
		{
			bool enable = (GetBackTranslation != null && EditingHelper.Editable);

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.ParentForm == FindForm())
			{
				itemProps.Enabled = enable;
				itemProps.Update = true;
				return true;
			}
			return false;
		}
		#endregion

		#region Overrides of Control methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the draft view is contained in a key terms wrapper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsKeyTermsView()
		{
			// Note: don't use commented line instead of GetType().Name... since this will cause
			// TeKeyTerms.dll to get loaded.
			//  if (window is KeyTermsViewWrapper)
			for (Control window = Parent; window != null; window = window.Parent)
				if (window.GetType().Name == "KeyTermsViewWrapper")
					return true;

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remember if a picture is selected so that it can be deselected on mouse up,
		/// if necessary.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// ------------------------------------------------------------------------------------
		protected override void CallMouseDown(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			if (sel != null && sel.SelType == VwSelType.kstPicture)
			{
				// If the picture selected is a translation status box, remember which picture is
				// selected (so that the status only changes if the same status box is selected
				// on mouse up and mouse down).
				SelectionHelper selHelper = SelectionHelper.Create(sel, this);
				SelLevInfo[] info = selHelper.LevelInfo;
				if (info[0].tag == StTxtParaTags.kflidTranslations)
				{
					m_selectedTransHvo = info[0].hvo;
					m_pictureSelected = true;
					m_selectionHelper = EditingHelper.CurrentSelection;
				}
			}
			else
			{
				m_pictureSelected = false;
				m_selectedTransHvo = 0;
			}
			base.CallMouseDown (pt, rcSrcRoot, rcDstRoot);
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
			base.CallMouseUp(pt, rcSrcRoot, rcDstRoot);

			// if a picture was selected on mouse down but not on mouse up,
			// make sure the selection is still cleared.
			if (m_pictureSelected)
			{
				if (m_selectionHelper == null)
					m_selectionHelper = EditingHelper.CurrentSelection;
				if (m_selectionHelper != null)
					m_selectionHelper.SetSelection(this, true, false);
				m_selectionHelper = null;
				m_pictureSelected = false;
				m_selectedTransHvo = 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the mouse up event.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right)
			{
				// If the selection is on the status box of a back translation...
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				IVwSelection sel = m_rootb.MakeSelAt(e.X, e.Y, rcSrcRoot, rcDstRoot, false);
				if (sel != null && sel.SelType == VwSelType.kstPicture)
				{
					SelectionHelper selHelper = SelectionHelper.Create(sel, this);
					SelLevInfo[] info = selHelper.LevelInfo;
					// (check the tag and confirm that the same translation status box is selected on
					//  mouse down and mouse up)
					if (info[0].tag == StTxtParaTags.kflidTranslations &&
						info[0].hvo == m_selectedTransHvo)
					{
						// ...toggle the back translation status to the next status.
						string undo;
						string redo;
						TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoChangeBackTransStatus", out undo, out redo);
						using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
							Cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
						{
							ICmTranslation trans = m_fdoCache.ServiceLocator.GetInstance<ICmTranslationRepository>().GetObject(m_selectedTransHvo);
							BackTranslationStatus btStatus = GetBackTranslationStatus(trans);
							switch (btStatus)
							{
								case BackTranslationStatus.Unfinished:
								default:
									SetBackTranslationStatus(trans, BackTranslationStatus.Finished);
									break;
								case BackTranslationStatus.Finished:
									SetBackTranslationStatus(trans, BackTranslationStatus.Checked);
									break;
								case BackTranslationStatus.Checked:
									SetBackTranslationStatus(trans, BackTranslationStatus.Unfinished);
									break;
							}
							undoHelper.RollBack = false;
						}
					}
				}

				base.OnMouseUp(e);
				return;
			}
			ShowContextMenu(new Point(e.X, e.Y));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the context menu.
		/// </summary>
		/// <param name="loc">The loc.</param>
		/// ------------------------------------------------------------------------------------
		protected void ShowContextMenu(Point loc)
		{
			TeMainWnd teMainWnd = TheMainWnd as TeMainWnd;
			if (teMainWnd == null || teMainWnd.TMAdapter == null)
				return;

			bool fShowSpellingOptsOnContextMenu = TeProjectSettings.ShowSpellingErrors;

			string menuName;
			string addToDictMenuName = "cmnuAddToDictDV";
			string insertBeforeMenuName = "cmnuAddToDictDV";

			if (TeEditingHelper.SelectFootnoteMarkNextToIP() || TeEditingHelper.IsFootnoteAnchorIconSelected())
			{
				menuName = "cmnuDraftViewFootnote";
				fShowSpellingOptsOnContextMenu = false;
			}
			else if (TeEditingHelper.IsPictureSelected)
			{
				menuName = "cmnuDraftViewPicture";
				fShowSpellingOptsOnContextMenu = false;
			}
			else if (IsKeyTermsView())
			{
				menuName = "cmnuDraftViewKeyTerms";
				addToDictMenuName = "cmnuAddToDictKTV";
				insertBeforeMenuName = "cmnuAddToDictKTV";
			}
			else if (IsBackTranslation)
			{
				menuName = "cmnuBackTranslation";
				addToDictMenuName = "cmnuAddToDictBTV";
				insertBeforeMenuName = "cmnuAddToDictBTV";
			}
			else
				menuName = "cmnuDraftViewNormal";

			TeEditingHelper.ShowContextMenu(loc, teMainWnd.TMAdapter, this, menuName,
				addToDictMenuName, "cmnuChangeMultiOccurencesDV", insertBeforeMenuName, fShowSpellingOptsOnContextMenu);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Focus got set to the draft view
		/// </summary>
		/// <param name="e">The event data</param>
		/// -----------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (DesignMode || !m_fRootboxMade)
				return;

			// Reset the previous selected paragraph value to reset the optimization
			// for scrolling the footnote pane.
			m_prevSelectedParagraph = 0;

			if (m_selectionHelper != null)
			{
				if (m_selectionHelper.SetSelection(this) == null)
				{
					// Set selection to beginning of first book if project has any books
					if (BookFilter.BookCount > 0)
						SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);
				}

				m_selectionHelper = null;
			}

			// A draft view which has focus cannot display selected-segment highlighting.
			if (m_vc != null)
				m_vc.SetupOverrides(null, 0, 0, null, RootBox);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.LostFocus"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			// This allows the regular prompt to re-appear after any update. See TE-7958.
			m_vc.HvoOfSegmentWhoseBtPromptIsToBeSupressed = 0;
			base.OnLostFocus(e);
		}
		#endregion

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the containing FwMainWnd.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FwMainWnd TheMainWnd
		{
			get
			{
				return base.TheMainWnd ??
					(TheViewWrapper != null ? TheViewWrapper.FindForm() as FwMainWnd : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the context menu for the specified root box at the location of
		/// its selection (typically an IP).
		/// </summary>
		/// <param name="rootb"></param>
		/// ------------------------------------------------------------------------------------
		public override void ShowContextMenuAtIp(IVwRootBox rootb)
		{
			CheckDisposed();
			ShowContextMenu(IPLocation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get average paragraph height for draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int AverageParaHeight
		{
			get
			{
				CheckDisposed();
				return (int)(21 * Zoom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate height for books and sections.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>The estimated height for the specified hvo in paragraphs</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			if (m_ParaHeightInPoints <= 0)
				m_ParaHeightInPoints = 1;
			return m_paraCounter.GetParagraphCount(hvo, frag) * m_ParaHeightInPoints;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			Debug.Assert(Cache != null);
			TeEditingHelper helper = CreateEditingHelper_Internal();
			helper.ContentType = ContentType;
			return helper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Internal version of CreateEditingHelper allows subclasses to override to create a
		/// subclass of TeEditingHelper without repeating the common initialization code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual TeEditingHelper CreateEditingHelper_Internal()
		{
			return new TeEditingHelper(this, Cache, m_filterInstance, m_viewType, m_app);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether automatic vertical scrolling to show the selection should
		/// occur. Usually this is only appropriate if the window autoscrolls and has a
		/// vertical scroll bar, but TE's draft view needs to allow it anyway, because in
		/// syncrhonized scrolling only one of the sync'd windows has a scroll bar.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected override bool DoAutoVScroll
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool AllowLayout
		{
			get { return base.AllowLayout; }
			set
			{
				if (!value)
					m_persistence.BeginInit();

				base.AllowLayout = value;

				if (value)
					m_persistence.EndInit();
			}
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

			if (TeEditingHelper == null)
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
				new ITsString[] { tssParas } , tssTrailing);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a view constructor suitable for this kind of view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override TeStVc CreateViewConstructor()
		{
			DraftViewVc vc = new DraftViewVc(TeStVc.LayoutViewTarget.targetDraft, m_filterInstance,
				m_styleSheet, m_fShowInTable);

			vc.HotLinkClick += DoHotLinkAction;

			return vc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a problem deletion - a complex selection crossing sections or other
		/// difficult cases such as BS/DEL at boundaries.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="dpt"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();

			return TeEditingHelper.OnProblemDeletion(sel, dpt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a queued event that will be processed during idle cycles.  This will update
		/// the information tool bar.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInformationBar(object args)
		{
			TeEditingHelper.SetInformationBarForSelection();

			// Update the the GoTo Reference control in the information tool bar.
			// !! This is found in the same method in TePrintLayout.cs
			TeEditingHelper.UpdateGotoPassageControl();
			return true;
		}

		/// <summary>
		/// Override to try to repair the situation if re-inserting a missing-data prompt causes there to be no selection.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			using (new PromptSelectionRestorer(RootBox))
				base.OnKeyPress(e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle a selection changed event.
		/// </summary>
		/// <param name="rootb">root box that has the selection change</param>
		/// <param name="vwselNew">Selection</param>
		/// -----------------------------------------------------------------------------------
		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.HandleSelectionChange(rootb, vwselNew);

			// It's possible that the base either changed the selection or invalidated it in
			// calling commit and made this selection no longer useable.
			SelectionHelper helper = EditingHelper.CurrentSelection;
			if (helper == null || !helper.Selection.IsValid)
				return;

			// scroll the footnote pane to be in synch with the draft view
			// (TimS): Focused was taken out because it was causing selection changes that
			// happended from a menu item wouldn't scroll to the footnote.
			if (TheDraftViewWrapper != null && TheDraftViewWrapper.FootnoteViewShowing &&
				Options.FootnoteSynchronousScrollingSetting)
			{
				// For performance on typing, don't try to scroll footnote pane on every update
				SelLevInfo paraInfo;
				if (helper.GetLevelInfoForTag(StTextTags.kflidParagraphs, out paraInfo))
				{
					IStTxtPara para = m_fdoCache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraInfo.hvo);
					ITsString tss = para.Contents;
					if (helper.IchAnchor <= tss.Length)
					{
						int iRun = (helper.IchAnchor >= 0) ? tss.get_RunAt(helper.IchAnchor) : -1;
						if (iRun != -1 && (m_prevSelectedParagraph != paraInfo.hvo ||
						iRun != m_prevAnchorPosition))
						{
							SynchFootnoteView(helper);
							// Save selection information
							m_prevSelectedParagraph = paraInfo.hvo;
							m_prevAnchorPosition = iRun;
						}
					}
				}
			}

			// Debug code was taken out. It was put in to help with testing using TestLangProj.
			// Most of our tests are using the InMemoryCache so this information is almost useless.
			// If you need it, just change the "DEBUG_not_exist" to "DEBUG" and then compile.
			#region Debug code
#if DEBUG_not_exist
			// This section of code will display selection information in the status bar when the
			// program is compiled in Debug mode. The information shown in the status bar is useful
			// when you want to make selections in tests.
			try
			{
				string text;
				SelLevInfo paraInfo = helper.GetLevelInfoForTag(StTextTags.kflidParagraphs);
				SelLevInfo secInfo = helper.GetLevelInfoForTag(ScrBookTags.kflidSections);
				SelLevInfo bookInfo = helper.GetLevelInfoForTag(BookFilter.Tag);

				bool inBookTitle = TeEditingHelper.InBookTitle;
				bool inSectionHead = TeEditingHelper.InSectionHead;

				text = "Book: " + bookInfo.ihvo +
					"  Section: " + (inBookTitle ? "Book Title" : secInfo.ihvo.ToString()) +
					"  Paragraph: " + paraInfo.ihvo +
					"  Anchor: " + helper.IchAnchor + "  End: " + helper.IchEnd +
					" AssocPrev: " + helper.AssocPrev;

				if (!inBookTitle && bookInfo.ihvo >= 0)
				{
					IStTxtPara para = m_fdoCache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraInfo.hvo);
					ITsString tss = para.Contents;
					if (helper.IchAnchor <= tss.Length)
						text += "  Run No.: " + tss.get_RunAt(helper.IchAnchor);
				}

				if (TheMainWnd != null)
					TheMainWnd.StatusStrip.Items[0].Text = text;
			}
			catch
			{
			}
#endif
			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Synchronizes the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void SynchFootnoteView()
		{
			CheckDisposed();

			SynchFootnoteView(EditingHelper.CurrentSelection);
			m_prevSelectedParagraph = 0;
			m_prevAnchorPosition = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Synchronizes the footnote view.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// ------------------------------------------------------------------------------------
		private void SynchFootnoteView(SelectionHelper helper)
		{
			IStFootnote footnote = TeEditingHelper.FindFootnoteNearSelection(helper);

			if (footnote != null)
			{
				FwEditingHelper.IgnoreSelectionChanges = true;
				TheDraftViewWrapper.FootnoteView.ScrollToFootnote(footnote, false);
				FwEditingHelper.IgnoreSelectionChanges = false;
			}
			else
			{
				TheDraftViewWrapper.FootnoteView.ScrollToTop();
				if (TheDraftViewWrapper.FootnoteView.EditingHelper.CurrentSelection == null)
				{
					try
					{
						TheDraftViewWrapper.FootnoteView.RootBox.MakeSimpleSel(true, true, false, true);
					}
					catch
					{
						// unable to make a selection
					}
				}
			}
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
		/// If the annotations window is gaining focus, then we don't want the draft view's
		/// range selections to be hidden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override VwSelectionState GetNonFocusedSelectionState(Control windowGainingFocus)
		{
			if (windowGainingFocus != null)
			{
				Form frm = windowGainingFocus.FindForm();
				if (frm is NotesMainWnd)
					return VwSelectionState.vssOutOfFocus;
			}

			return base.GetNonFocusedSelectionState(windowGainingFocus);
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
			get { return (int)ScrFrags.kfrScripture; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property that tells if the paragraphs are displayed in a table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool ShowInTable
		{
			get
			{
				CheckDisposed();
				return m_fShowInTable;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the DraftViewWrapper that owns this DraftView
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DraftViewWrapper TheDraftViewWrapper
		{
			get
			{
				CheckDisposed();
				return m_draftViewWrapper as DraftViewWrapper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the view wrapper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ViewWrapper TheViewWrapper
		{
			get
			{
				CheckDisposed();
				return m_draftViewWrapper;
			}
			set { m_draftViewWrapper = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper TeEditingHelper
		{
			get
			{
				CheckDisposed();
				return EditingHelper as TeEditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the BackTranslation.
		/// Enhance JohnT: this is currently used for the BT status which is stored on the
		/// CmTranslation both for simple and segment BTs. We probably want a different place
		/// to store status for segment BT, at least if we get rid of the old-style BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ICmTranslation GetBackTranslation
		{
			get
			{
				if (IsBackTranslation && TeEditingHelper.CurrentSelection != null)
				{
					// Find out the state of the paragraph where the selection is
					SelectionHelper sel = TeEditingHelper.CurrentSelection;
					if (sel.LevelInfo.Length > 0)
					{
						ICmObject obj = m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(
							sel.LevelInfo[0].hvo);
						return (obj is ICmTranslation) ? (ICmTranslation)obj : null;
					}
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view's selection helper object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelHelper
		{
			get
			{
				CheckDisposed();
				return m_selectionHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the paragraph where the insertion point is located.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParagraphIndex
		{
			get
			{
				CheckDisposed();
				return TeEditingHelper.ParagraphIndex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the character at the anchor point of a selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectionAnchorIndex
		{
			get
			{
				CheckDisposed();

				IVwSelection vwsel = RootBox.Selection;
				if (vwsel == null)
					return 0;

				SelectionHelper helper = SelectionHelper.GetSelectionInfo(vwsel, null);
				return helper.IchAnchor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the character at the end of a selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectionEndIndex
		{
			get
			{
				CheckDisposed();

				IVwSelection vwsel = RootBox.Selection;
				if (vwsel == null)
					return 0;

				SelectionHelper helper = SelectionHelper.GetSelectionInfo(vwsel, null);
				return helper.IchEnd;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A set of flags indicating various view properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeViewType ViewType
		{
			get { return m_viewType; }
		}
		#endregion

		#region Print-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If help is available for the print dialog, set ShowHelp to true,
		/// and add an event handler that can display some help.
		/// TE has such help available.
		/// </summary>
		/// <param name="dlg"></param>
		/// ------------------------------------------------------------------------------------
		protected override void SetupPrintHelp(PrintDialog dlg)
		{
			dlg.ShowHelp = true;
			dlg.HelpRequest += new EventHandler(ShowPrintDlgHelp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show help for the print dialog
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ShowPrintDlgHelp(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpPrint");
		}
		#endregion

		#region Section-related methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the section for the current selection. Returns null if the selection
		/// is not in a section.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private IScrSection Section
		{
			get
			{
				ILocationTracker tracker = EditingHelper as ILocationTracker;
				Debug.Assert(tracker != null);
				return tracker.GetSection(EditingHelper.CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
			}
		}
		#endregion

		#region Book-related methods
//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Gets a value indicating whether or not there are any scripture books in the cache.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		private bool BooksPresent
//		{
//			get {return BookFilter.BookCount > 0;}
//		}
		#endregion

		#region Selection Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int book, int section, int para,
			int character, bool fAssocPrev)
		{
			CheckDisposed();

			return TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				book, section, para, character, fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of any Scripture element: Title, Section Head, or Section
		/// Content.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBookTags.kflidTitle"/>
		/// </param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		protected SelectionHelper SetInsertionPoint(int tag, int book, int section)
		{
			return SetInsertionPoint(tag, book, section, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of any Scripture element: Title, Section Head, or Section
		/// Content.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBookTags.kflidTitle"/>
		/// </param>
		/// <param name="paragraph">The 0-based index of the paragraph which to put the
		/// insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		protected SelectionHelper SetInsertionPoint(int tag, int book, int section, int paragraph)
		{
			return TeEditingHelper.SetInsertionPoint(tag, book, section, paragraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection in this DraftView which is
		/// "installed" and scrolled into view.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// <remarks>This method is only used for tests</remarks>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int book, int section, int para,
			int startCharacter, int endCharacter)
		{
			CheckDisposed();

			return TeEditingHelper.SelectRangeOfChars(book, section,
				ScrSectionTags.kflidContent,
				para, startCharacter, endCharacter, true, true, true);
		}
		#endregion

		#region Footnote related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a footnote when the selection is on a footnote marker.
		/// Called from context menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDeleteFootnote(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoDelFootnote", out undo, out redo);
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(this, undo, redo))
			using (new DataUpdateMonitor(this, "DeleteFootnote"))
			{
				TeEditingHelper.OnDeleteFootnoteAux();
				undoHelper.RollBack = false;
			}

			return true;
		}
		#endregion
	}
}
