// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftView.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Implements the draft view editing pane, either vernacular or back translation.
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.Utils;
using Microsoft.Win32;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.FdoUi;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of the draft view (formerly SeDraftWnd)
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class DraftView : FwRootSite, ISelectableView, ISettings, ITeView, IGetTeStVc,
		IBtAwareView
	{
		#region Data members
		private ViewWrapper m_draftViewWrapper;
		/// <summary>Exposed for tests</summary>
		protected DraftViewVc m_draftViewVc;
		private SelectionHelper m_SelectionHelper;

		/// <summary>
		/// protected ONLY so tests can set it.
		/// </summary>
		protected StVc.ContentTypes m_contentType;

		/// <summary><c>true</c> to layout data in a table</summary>
		protected bool m_fShowInTable;

		/// <summary><c>true</c> if a picture is selected</summary>
		private bool m_pictureSelected = false;

		/// <summary>Hvo of selected translation, or 0 if no status box is selected</summary>
		private int m_selectedTransHvo;
		private int m_prevSelectedParagraph;
		private int m_prevAnchorPosition;
		private readonly int m_filterInstance;
		private readonly bool m_persistSettings;
		private readonly FilteredScrBooks m_bookFilter;
		private readonly LocationTrackerImpl m_locationTracker;
		private readonly TeViewType m_viewType;

		/// <summary>
		/// base caption string that this client window should display in the info bar
		/// </summary>
		private string m_baseInfoBarCaption;
		private Persistence m_persistence;
		private IContainer components;
		private IParagraphCounter m_paraCounter;
		private FreeTransEditMonitor m_ftMonitor;
		private DraftView m_mainTrans;
		private CmTranslationEditMonitor m_cmtMonitor;
		#endregion

		#region Constructor
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Initializes a new instance of the DraftView class for Scripture.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//public DraftView() : this(false, false, 0)
		//{
		//    //AllowScrolling = false;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DraftView class for Scripture.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fIsBackTrans"><c>true</c> if the draft view is a BT</param>
		/// <param name="showInTable">True to show the text layed out in a table, false
		/// otherwise</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public DraftView(FdoCache cache, bool fIsBackTrans, bool showInTable, int filterInstance) :
			this(cache, fIsBackTrans, showInTable, true, true,
			TeViewType.DraftView | (fIsBackTrans ? TeViewType.BackTranslation : TeViewType.Scripture),
			filterInstance)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DraftView class as a draft view editing pane,
		/// for either vernacular or back translation.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="isBackTranslation">specifies back translation if true, otherwise
		/// vernacular</param>
		/// <param name="showInTable">True to show the text layed out in a table, false
		/// otherwise</param>
		/// <param name="makeRootAutomatically">attempt to construct the rootbox automatically
		/// when the window handle is created</param>
		/// <param name="persistSettings">true to save and load settings for the window</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public DraftView(FdoCache cache, bool isBackTranslation, bool showInTable,
			bool makeRootAutomatically, bool persistSettings, TeViewType viewType,
			int filterInstance)
			: this(cache, (isBackTranslation ? StVc.ContentTypes.kctSimpleBT : StVc.ContentTypes.kctNormal), showInTable,
			makeRootAutomatically, persistSettings, viewType, filterInstance)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DraftView class as a draft view editing pane,
		/// for either vernacular or back translation.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="contentType">specifies normal or some kind of BT</param>
		/// <param name="showInTable">True to show the text layed out in a table, false
		/// otherwise</param>
		/// <param name="makeRootAutomatically">attempt to construct the rootbox automatically
		/// when the window handle is created</param>
		/// <param name="persistSettings">true to save and load settings for the window</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public DraftView(FdoCache cache, StVc.ContentTypes contentType, bool showInTable,
			bool makeRootAutomatically, bool persistSettings, TeViewType viewType,
			int filterInstance) : base(cache)
		{
			if (contentType == StVc.ContentTypes.kctSegmentBT)
				LexEntryUi.EnsureFlexVirtuals(Cache, Mediator);

			AutoScroll = false;
			m_contentType = contentType;
			m_fShowInTable = showInTable;
			m_fMakeRootWhenHandleIsCreated = makeRootAutomatically;
			m_persistSettings = persistSettings;
			m_filterInstance = filterInstance;

			m_bookFilter = FilteredScrBooks.GetFilterInstance(cache, m_filterInstance);
			// if the filter is still null then make one for testing
			if (m_bookFilter == null && TheMainWnd == null)
				m_bookFilter = new FilteredScrBooks(cache, m_filterInstance);

			m_locationTracker = new LocationTrackerImpl(cache, m_filterInstance,
				m_contentType);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			BackColor = EditableColor;
			m_viewType = viewType;
			DoSpellCheck = (EditingHelper as TeEditingHelper).ShowSpellingErrors;
		}

		// The color the window should be if it is editable.
		Color EditableColor
		{
			get
			{
				return m_contentType == StVc.ContentTypes.kctSegmentBT ? TeResourceHelper.ReadOnlyTextBackgroundColor : SystemColors.Window;
			}
		}

		#endregion

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_draftViewVc != null)
					m_draftViewVc.Dispose();
				DisposeFtMonitor();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftViewVc = null;
			m_SelectionHelper = null;
		}

		#endregion IDisposable override

		#region Component Designer generated code (i.e. InitializeComponent)
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DraftView));
			this.m_persistence = new SIL.FieldWorks.Common.Controls.Persistence(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).BeginInit();
			this.SuspendLayout();
			//
			// m_persistence
			//
			this.m_persistence.Parent = this;
			this.m_persistence.SaveSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.OnSaveSettings);
			this.m_persistence.LoadSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.OnLoadSettings);
			//
			// DraftView
			//
			resources.ApplyResources(this, "$this");
			this.Name = "DraftView";
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region ISettings implementation
		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the parent's SettingsKey if parent implements ISettings, otherwise null.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				if (TheMainWnd is ISettings)
					return ((ISettings)TheMainWnd).SettingsKey;
				return null;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a window creation option.
		/// </summary>
		/// <value>By default, returns false</value>
		///-------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement this method to save the persistent settings for your class. Normally, this
		/// should also result in recursively calling SaveSettingsNow for all children which
		/// also implement ISettings.
		/// </summary>
		/// <remarks>Note: A form's implementation of <see cref="M:SIL.FieldWorks.Common.Controls.ISettings.SaveSettingsNow"/> normally
		/// calls <see cref="M:SIL.FieldWorks.Common.Controls.Persistence.SaveSettingsNow(System.Windows.Forms.Control)"/> (after optionally saving any
		/// class-specific properties).</remarks>
		/// ------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();
			m_persistence.SaveSettingsNow(this);
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Load settings from the registry
		/// </summary>
		/// <param name="key">Location in the registry</param>
		///-------------------------------------------------------------------------------------
		public void OnLoadSettings(RegistryKey key)
		{
			CheckDisposed();

			object objTemp;

			Zoom = RegistryHelper.ReadFloatSetting(key, "ZoomFactor" + Name, 1.5f);

			if (TheMainWnd != null)
				key = TheMainWnd.ModifyKey(key, false);

			objTemp = key.GetValue(Name);
			if (objTemp != null)
			{
				// Restore the selection
				try
				{
					using (MemoryStream stream = new MemoryStream((byte[])objTemp))
					{
						m_SelectionHelper =
							(SelectionHelper)Persistence.DeserializeFromBinary(stream);
					}

					// Determine whether or not the FwMainWnd is a copy of another.
					bool fMainWndIsCopy =
						(TheMainWnd != null ? ((TeMainWnd)TheMainWnd).WindowIsCopy : false);

					if (m_SelectionHelper != null)
					{
						// the selection helper book information needs to be converted to the
						// filter information
						ConvertBookTagAndIndex(m_SelectionHelper, SelectionHelper.SelLimitType.Anchor);
						ConvertBookTagAndIndex(m_SelectionHelper, SelectionHelper.SelLimitType.End);

						if (!fMainWndIsCopy && m_SelectionHelper.NumberOfLevels > 2)
						{
							// Level where we find paragraph depends on type of content
							int paraLevel = 0;
							if (m_contentType == StVc.ContentTypes.kctSimpleBT)
								paraLevel ++;
							else if (m_contentType == StVc.ContentTypes.kctSegmentBT)
								paraLevel += 2;

							if (m_SelectionHelper.LevelInfo[paraLevel].tag ==
								(int)StText.StTextTags.kflidParagraphs)
							{
								if (m_contentType != StVc.ContentTypes.kctSegmentBT)
								{
									// Set IP to the beginning of the section contents
									m_SelectionHelper.IchAnchor = 0;
									m_SelectionHelper.LevelInfo[paraLevel].ihvo = 0;
									m_SelectionHelper.LevelInfo[paraLevel + 1].ihvo = 0;
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
								tempHelper.NumberOfLevels = 4;
								for (int i = 0; i < tempHelper.NumberOfLevels - 1; i++)
									tempHelper.LevelInfo[i] = m_SelectionHelper.LevelInfo[i + 1];

								m_SelectionHelper = tempHelper;
							}
						}

						m_SelectionHelper.ReduceToIp(SelectionHelper.SelLimitType.Anchor);
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
				if (IsHandleCreated && (m_SelectionHelper == null ||
					m_SelectionHelper.MakeBest(this, true) == null))
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
				m_SelectionHelper = null;
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
			if (info[bookPos].tag == (int)Scripture.ScriptureTags.kflidScriptureBooks)
			{
				info[bookPos].tag = BookFilter.Tag;
				info[bookPos].ihvo = BookFilter.GetBookIndex(info[bookPos].hvo);
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
				info[bookPos].tag = (int)Scripture.ScriptureTags.kflidScriptureBooks;
				info[bookPos].ihvo = BookFilter.GetUnfilteredIndex(info[bookPos].ihvo);
				helper.SetLevelInfo(selType, info);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Save passage to the registry
		/// </summary>
		/// <param name="key">Location in the registry</param>
		///-------------------------------------------------------------------------------------
		public void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();

			if (key == null || !m_persistSettings)
				return;

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, this);

			if (selectionHelper != null)
			{
				try
				{
					// Convert all of the filter tags to scripture book tags.
					ReplaceBookTagAndIndex(selectionHelper, SelectionHelper.SelLimitType.Anchor);
					ReplaceBookTagAndIndex(selectionHelper, SelectionHelper.SelLimitType.End);

					key = TheMainWnd.ModifyKey(key, false);
					key.SetValue(Name, Persistence.SerializeToBinary(selectionHelper).ToArray());
				}
				catch
				{
					// Ignore any errors we get...
				}
			}

			RegistryHelper.WriteFloatSetting(key, "ZoomFactor" + Name, Zoom);
		}

		#endregion

		#region ISelectableView implementation
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view and initializes the information bar and styles combo based on
		/// the draft view's selection.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public virtual void ActivateView()
		{
			CheckDisposed();

			PerformLayout();
			Show();

			Focus();
			try
			{
				if (m_rootb.Selection == null)
					m_rootb.MakeSimpleSel(true, true, false, true);
			}
			catch
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
		/// <param name="hvoOwner">Ignored</param>
		/// <param name="tag">Ignored</param>
		/// <param name="tss">Ignored</param>
		/// <param name="ichObj">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public virtual void DoHotLinkAction(object sender, string strData, int hvoOwner, int tag,
			ITsString tss, int ichObj)
		{
			CheckDisposed();

			// first char. of strData is type code - GUID will follow it.
			Guid objGuid = MiscUtils.GetGuidFromObjData(strData.Substring(1));

			int hvo = Cache.GetIdFromGuid(objGuid);

			if (hvo > 0)
			{
				ICmObject obj = null;
				try
				{
					obj = CmObject.CreateFromDBObject(Cache, hvo);
				}
				catch
				{
					// If the GUID is for an object that no longer exists but is still in the cache,
					// the above call could fail. We try to prevent this, but in case we have a
					// loophole, better to just do nothing.
				}
				if (obj is StFootnote)
					TheMainWnd.Mediator.SendMessage("FootnoteClick", obj);
			}
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
			CmTranslation trans = new CmTranslation(m_fdoCache,
				TeEditingHelper.CurrentSelection.LevelInfo[0].hvo);
			SetBackTranslationStatus(trans, BackTranslationStatus.Unfinished);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Back Translation Finished menu option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationFinished(object args)
		{
			CmTranslation trans = new CmTranslation(m_fdoCache,
				TeEditingHelper.CurrentSelection.LevelInfo[0].hvo);
			SetBackTranslationStatus(trans, BackTranslationStatus.Finished);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Back Translation Checked menu option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationChecked(object args)
		{
			CmTranslation trans = new CmTranslation(m_fdoCache,
				TeEditingHelper.CurrentSelection.LevelInfo[0].hvo);
			SetBackTranslationStatus(trans, BackTranslationStatus.Checked);
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
					BackTranslationStatus.Unfinished);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
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
		/// Find and select the next translation with a given state
		/// </summary>
		/// <param name="selection">The selection where to start the search.
		/// NOTE: The selection must have all of the info set in the LevelInfo (hvo, ihvo)</param>
		/// <param name="searchStatus">Back translation status to search for</param>
		/// ------------------------------------------------------------------------------------
		private void MoveToNextTranslation(SelectionHelper selection,
			BackTranslationStatus searchStatus)
		{
			SelLevInfo bookInfo;
			SelLevInfo paraInfo;
			SelLevInfo sectionInfo;
			bool fFoundBookLevel = selection.GetLevelInfoForTag(BookFilter.Tag, out bookInfo);
			bool fFoundSectionLevel = selection.GetLevelInfoForTag(
				(int)ScrBook.ScrBookTags.kflidSections, out sectionInfo);
			int secLev = selection.GetLevelForTag((int)ScrBook.ScrBookTags.kflidSections);
			bool fFoundParaLevel = selection.GetLevelInfoForTag(
				(int)StText.StTextTags.kflidParagraphs, out paraInfo);

			if (!fFoundBookLevel || !fFoundParaLevel)
				return;

			// Look through all the books in the book filter
			int bookStartIndex = bookInfo.ihvo;
			int sectionStartIndex = 0;
			int sectionTag = (int)ScrSection.ScrSectionTags.kflidContent;
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
				ScrBook checkBook = BookFilter.GetBook(bookStartIndex);
				paraIndex = FindNextTranslationInText(checkBook.TitleOA, searchStatus,
					paraStartIndex);
				if (paraIndex >= 0)
				{
					// select the title paragraph
					SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, bookStartIndex,
						0, paraIndex);
					return;
				}
				// continue the search with the current book
				sectionTag = (int)ScrSection.ScrSectionTags.kflidHeading;
				paraStartIndex = 0;
			}

			for (int bookIndex = bookStartIndex; bookIndex < BookFilter.BookCount; bookIndex++)
			{
				ScrBook checkBook = BookFilter.GetBook(bookIndex);
				if (bookIndex > bookStartIndex)
				{
					// Look through the title paragraphs
					paraIndex = FindNextTranslationInText(checkBook.TitleOA, searchStatus, 0);
					if (paraIndex >= 0)
					{
						// select the title paragraph
						SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, bookIndex, 0, paraIndex);
						return;
					}
				}

				// Look through the sections in order.
				for (int sectionIndex = sectionStartIndex;
					sectionIndex < checkBook.SectionsOS.Count; sectionIndex++)
				{
					IScrSection checkSection = checkBook.SectionsOS[sectionIndex];

					// Look in the paragraphs (could be either content or heading)
					IStText text = (sectionTag == (int)ScrSection.ScrSectionTags.kflidHeading) ?
						checkSection.HeadingOA : checkSection.ContentOA;
					paraIndex = FindNextTranslationInText(text, searchStatus, paraStartIndex);
					if (paraIndex >= 0)
					{
						// select the paragraph
						SetInsertionPoint(sectionTag, bookIndex, sectionIndex, paraIndex);
						return;
					}

					// Look in the heading paragraphs, if we haven't already
					if (sectionTag == (int)ScrSection.ScrSectionTags.kflidHeading)
					{
						sectionTag = (int)ScrSection.ScrSectionTags.kflidContent;
						paraIndex = FindNextTranslationInText(checkSection.ContentOA,
							searchStatus, 0);
						if (paraIndex >= 0)
						{
							// select the heading paragraph
							SetInsertionPoint(sectionTag, bookIndex, sectionIndex, paraIndex);
							return;
						}
					}

					sectionTag = (int)ScrSection.ScrSectionTags.kflidHeading;
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
		/// <param name="searchStatus">status of the CmTranslation that we want to find</param>
		/// <param name="iParaStartSearch">Index of the paragraph where search starts</param>
		/// <returns>an index of the found paragraph in the StText or -1 if not found</returns>
		/// ------------------------------------------------------------------------------------
		private int FindNextTranslationInText(IStText text, BackTranslationStatus searchStatus,
			int iParaStartSearch)
		{
			// Look through all of the paragraphs in the StText.
			for (int paraIndex = iParaStartSearch; paraIndex < text.ParagraphsOS.Count; paraIndex++)
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
				(int)ScrBook.ScrBookTags.kflidSections, out sectionInfo);
			int secLev = selection.GetLevelForTag((int)ScrBook.ScrBookTags.kflidSections);
			bool fFoundParaLevel = selection.GetLevelInfoForTag(
				(int)StText.StTextTags.kflidParagraphs, out paraInfo);

			if (!fFoundBookLevel || !fFoundParaLevel)
				return;

			// Look through all the books in the book filter
			int bookStartIndex = bookInfo.ihvo;
			int sectionStartIndex = -1;
			int sectionTag = (int)ScrSection.ScrSectionTags.kflidContent;
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
				ScrBook checkBook = BookFilter.GetBook(bookStartIndex);
				paraIndex = FindPrevTranslationInText(checkBook.TitleOA, searchStatus,
					paraStartIndex);
				if (paraIndex >= 0)
				{
					// select the title paragraph
					SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, bookStartIndex,
						0, paraIndex);
					return;
				}
				// continue the search with the previous book
				bookStartIndex--;
				paraStartIndex = -2;
			}

			for (int bookIndex = bookStartIndex; bookIndex >= 0 ; bookIndex--)
			{
				ScrBook checkBook = BookFilter.GetBook(bookIndex);
				if (sectionStartIndex == -1)
				{
					sectionStartIndex = checkBook.SectionsOS.Count - 1;
					sectionTag = (int)ScrSection.ScrSectionTags.kflidContent;
				}

				// Look through the sections in reverse order.
				for (int sectionIndex = sectionStartIndex; sectionIndex >= 0; sectionIndex--)
				{
					IScrSection checkSection = checkBook.SectionsOS[sectionIndex];

					if (paraStartIndex == -2)
					{
						paraStartIndex = checkSection.ContentOA.ParagraphsOS.Count - 1;
						sectionTag = (int)ScrSection.ScrSectionTags.kflidContent;
					}

					// Look in the paragraphs (could be either content or heading)
					IStText text = (sectionTag == (int)ScrSection.ScrSectionTags.kflidHeading) ?
						checkSection.HeadingOA : checkSection.ContentOA;
					paraIndex = FindPrevTranslationInText(text, searchStatus, paraStartIndex);
					if (paraIndex >= 0)
					{
						// select the paragraph
						SetInsertionPoint(sectionTag, bookIndex, sectionIndex, paraIndex);
						return;
					}

					// Look in the heading paragraphs, if we haven't already
					if (sectionTag == (int)ScrSection.ScrSectionTags.kflidContent)
					{
						sectionTag = (int)ScrSection.ScrSectionTags.kflidHeading;
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
					SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, bookIndex, 0, paraIndex);
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
			Debug.Assert(trans.TypeRA.Guid == LangProject.kguidTranBackTranslation);
			string status = trans.Status.GetAlternative(ViewConstructorWS);

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
			Debug.Assert(trans.TypeRA.Guid == LangProject.kguidTranBackTranslation);
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoChangeBackTransStatus", out undo, out redo);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(this,
					  undo, redo, false))
			{
				trans.Status.SetAlternative(status.ToString(), ViewConstructorWS);
				m_fdoCache.MainCacheAccessor.PropChanged(null,
					(int)PropChangeType.kpctNotifyAll, trans.Hvo,
					(int)CmTranslation.CmTranslationTags.kflidStatus, 0, 1, 1);
			}
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
			CmTranslation trans = GetBackTranslation;
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
			CmTranslation trans = GetBackTranslation;
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
			CmTranslation trans = GetBackTranslation;
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
				if (info[0].tag == (int)StTxtPara.StTxtParaTags.kflidTranslations)
				{
					m_selectedTransHvo = info[0].hvo;
					m_pictureSelected = true;
					m_SelectionHelper = EditingHelper.CurrentSelection;
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
				if (m_SelectionHelper == null)
					m_SelectionHelper = EditingHelper.CurrentSelection;
				if (m_SelectionHelper != null)
					m_SelectionHelper.SetSelection(this, true, false);
				m_SelectionHelper = null;
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
					if (info[0].tag == (int)StTxtPara.StTxtParaTags.kflidTranslations &&
						info[0].hvo == m_selectedTransHvo)
					{
						// ...toggle the back translation status to the next status.
						CmTranslation trans = new CmTranslation(m_fdoCache, info[0].hvo);
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
					}
				}

				base.OnMouseUp(e);
				return;
			}
			ShowContextMenu(new Point(e.X, e.Y));
		}

		internal bool IsBackTranslation
		{
			get { return m_contentType != StVc.ContentTypes.kctNormal; }
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

			bool fShowSpellingOptsOnContextMenu =
				((TeEditingHelper)EditingHelper).ShowSpellingErrors;

			string menuName;
			string addToDictMenuName = "cmnuAddToDictDV";
			string insertBeforeMenuName = "cmnuAddToDictDV";

			if (IsFootnoteMarkNextToIP())
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

			EditingHelper.ShowContextMenu(loc, teMainWnd.TMAdapter, this, menuName,
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

			if (m_SelectionHelper != null)
			{
				if (m_SelectionHelper.SetSelection(this) == null)
				{
					// Set selection to beginning of first book if project has any books
					if (BookFilter.BookCount > 0)
						SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);
				}

				m_SelectionHelper = null;
			}
			// Supposedly, m_ftMonitor should always be null, since it only gets set in OnGetFocus
			// and it gets cleared in OnLostFocus. However there have been odd cases. If we're already
			// tracking a change we don't want to lose it.
			if (m_contentType == StVc.ContentTypes.kctSegmentBT && m_ftMonitor == null)
			{
				m_ftMonitor = new FreeTransEditMonitor(Cache, BackTranslationWS);
				// Unfortunately, when the main window closes, both our Dispose() method and our OnLostFocus() method
				// get called during the Dispose() of the main window, which is AFTER the FdoCache gets disposed.
				// We need to dispose our FreeTransEditMonitor before the cache is disposed, so we can update the
				// CmTranslation if necessary.
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing += new FormClosingEventHandler(DraftView_FormClosing);
			}
			if (m_contentType == StVc.ContentTypes.kctSimpleBT && m_cmtMonitor == null)
			{
				m_cmtMonitor = new CmTranslationEditMonitor(Cache, BackTranslationWS);
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing += new FormClosingEventHandler(DraftView_FormClosing);
			}

			// A draft view which has focus cannot display selected-segment highlighting.
			if (m_draftViewVc != null)
				m_draftViewVc.SetupOverrides(null, 0, 0, null, null);
		}

		void DraftView_FormClosing(object sender, FormClosingEventArgs e)
		{
			DisposeFtMonitor();
		}

		/// <summary>
		/// Any pending changes to the CmTranslation BT required to match changes to the segmented BT
		/// should now be done.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLostFocus(EventArgs e)
		{
			DisposeFtMonitor();
			// This allows the regular prompt to re-appear after any update. See TE-7958.
			m_draftViewVc.SuppressCommentPromptHvo = 0;
			base.OnLostFocus(e);
		}

		private void DisposeFtMonitor()
		{
			if (m_ftMonitor != null)
			{
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing -= new FormClosingEventHandler(DraftView_FormClosing);
				m_ftMonitor.Dispose();
				m_ftMonitor = null;
			}
			if (m_cmtMonitor != null)
			{
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing -= new FormClosingEventHandler(DraftView_FormClosing);
				m_cmtMonitor.Dispose();
				m_cmtMonitor = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:HandleCreated"/> event. If this is not a back translation
		/// draft view, then attempt to set up its editing helper to update BT segments for
		/// the corresponding BT writing system, if any.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			if (IsBackTranslation || TeEditingHelper == null) // might fail in tests
				return;

			int wsBt = -1;
			if (TheViewWrapper != null)
				wsBt = TheViewWrapper.BackTranslationWS;

			if (wsBt <= 0)
			{
				TeMainWnd mainWnd = TheMainWnd as TeMainWnd;
				if (mainWnd != null)
					wsBt = mainWnd.DefaultBackTranslationWs;
			}

			if (wsBt > 0)
				TeEditingHelper.EnableBtSegmentUpdate(wsBt);
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FDO cache
		/// </summary>
		/// <value>A <see cref="FdoCache"/></value>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override FdoCache Cache
		{
			set
			{
				CheckDisposed();

				Debug.Assert(m_fdoCache == null || m_fdoCache == value,
					"Changing the cache after its already been set is bad!");
				m_paraCounter = ParagraphCounterManager.GetParaCounter(value,
					(int)TeViewGroup.Scripture);
				base.Cache = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to update the spell check status.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void RefreshDisplay()
		{
			DoSpellCheck = (EditingHelper as TeEditingHelper).ShowSpellingErrors;
			m_draftViewVc.SuppressCommentPromptHvo = 0;
			base.RefreshDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_editingHelper == null)
				{
					Debug.Assert(Cache != null);
					TeEditingHelper helper = new TeEditingHelper(this, Cache, FilterInstance, m_viewType);
					m_editingHelper = helper;
					helper.ContentType = m_contentType;
					if (MainTrans != null)
						helper.MainTransView = MainTrans; // just in case the helper gets created after it is set.
				}
				return m_editingHelper;
			}
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
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
				m_rootb = MakeRootBox();

			m_rootb.SetSite(this);

			// Setup a notifier for object-replacement character deletions (which are footnote
			// markers)
			m_rootb.RequestObjCharDeleteNotification(EditingHelper);

			this.HorizMargin = 10;

			// Set hvo to the ID of the dummy vector that stores the root objects that have been
			// filtered and sorted.
			// TODO
			//			int wid = m_pdcwParent->GetWindowId();
			//			AfMdiClientWnd * pmdic = dynamic_cast<AfMdiClientWnd *>(m_pdcwParent->Parent());
			//			AssertPtr(pmdic);
			//			int iview;
			//			iview = pmdic->GetChildIndexFromWid(wid);
			//			UserViewSpecVec & vuvs = plpi->GetDbInfo()->GetUserViewSpecs();
			//			ClsLevel clevKey(kclidScripture, 0);
			//			vuvs[iview]->m_hmclevrsp.Retrieve(clevKey, m_qrsp);

			// Partial implementation of the above blocked out code.
			// Get all UserView objects that relate to this application.
			//			UserViewCollection uvc =
			//				m_fdoCache.UserViewSpecs.GetUserViews(FwApp.App.AppGuid);
			//			System.Console.WriteLine("UserViewCollection.Count={0}", uvc.Count);
			//if (uvc.Count == 0)	// Allow this for real code?
			//	return;

			// Set up a new view constructor.
			Debug.Assert(m_draftViewVc == null, "View constructor set up twice for " + Name);
			if (m_draftViewVc != null)
				m_draftViewVc.Dispose();

			m_draftViewVc = CreateDraftViewVC();
			m_draftViewVc.HotLinkClick += DoHotLinkAction;

			if (IsBackTranslation)
			{
				m_draftViewVc.ContentType = m_contentType;
				m_draftViewVc.DefaultWs = (TheMainWnd is TeMainWnd ?
					((TeMainWnd)TheMainWnd).GetBackTranslationWsForView(TheViewWrapper.Name) : m_fdoCache.DefaultAnalWs);
			}

			m_draftViewVc.Cache = m_fdoCache;
			m_draftViewVc.BackColor = BackColor;
			m_draftViewVc.Editable = EditingHelper.Editable;

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			//EditingHelper.RootObjects = new int[]{ScriptureObj.Hvo};
			MakeRootObject();

			base.MakeRoot();

			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			//TODO:
			//ptmw->RegisterRootBox(qrootb);

			Synchronize(m_rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the root box.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal virtual IVwRootBox MakeRootBox()
		{
			return VwRootBoxClass.Create();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This part of MakeRoot is isolated so that it can be overridden in vertical draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal virtual void MakeRootObject()
		{
			m_rootb.SetRootObject(m_fdoCache.LangProject.TranslatedScriptureOAHvo,
				m_draftViewVc, (int)ScrFrags.kfrScripture, m_styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual DraftViewVc CreateDraftViewVC()
		{
			CheckDisposed();

			DraftViewVc vc = new DraftViewVc(TeStVc.LayoutViewTarget.targetDraft,
				m_filterInstance, m_styleSheet, m_fShowInTable);
			vc.Name = Name + " VC";
			if (Group == null)
				vc.HeightEstimator = this;
			else
				vc.HeightEstimator = Group as IHeightEstimator;
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
		public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.SelectionChanged(rootb, vwselNew);

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
				SelLevInfo paraInfo =
					helper.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs);

				StTxtPara para = new StTxtPara(m_fdoCache, paraInfo.hvo);
				ITsString tss = para.Contents.UnderlyingTsString;
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
				SelLevInfo paraInfo = helper.GetLevelInfoForTag((int)StText.StTextTags.kflidParagraphs);
				SelLevInfo secInfo = helper.GetLevelInfoForTag((int)ScrBook.ScrBookTags.kflidSections);
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
					ScrBook book = BookFilter.GetBook(bookInfo.ihvo);
					ScrSection section = new ScrSection(m_fdoCache, secInfo.hvo);
					StTxtPara para = new StTxtPara(m_fdoCache, paraInfo.hvo);
					ITsString tss = para.Contents.UnderlyingTsString;
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
			StFootnote footnote = TeEditingHelper.FindFootnoteNearSelection(helper);
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
		/// Checks to see if there's a footnote on either side of the IP. If there is, then
		/// it's selected.
		/// </summary>
		/// <returns><c>true</c> if there's a footnote marker next to the IP. Otherwise,
		/// <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsFootnoteMarkNextToIP()
		{
			if (EditingHelper.CurrentSelection == null)
				return false;

			SelectionHelper helper = new SelectionHelper(EditingHelper.CurrentSelection);

			// If the selection is a range then just return what SelectionIsFootnoteMarker determines.
			if (helper.Selection.IsRange)
				return SelectionIsFootnoteMarker(helper.Selection);

			// Now that we know the selection is not a range, look to both sides of the IP
			// to see if we are next to a footnote marker.

			// First, look on the side where the IP is associated. If there's a marker there,
			// select it and get out.
			if (SelectionIsFootnoteMarker(helper.Selection))
			{
				helper.IchEnd = helper.AssocPrev ? (helper.IchAnchor - 1) : (helper.IchAnchor + 1);
				helper.SetSelection(true);
				return true;
			}

			// Check the other side of the IP and select the marker if there's one there.
			helper.AssocPrev = !helper.AssocPrev;
			helper.SetSelection(false);
			if (SelectionIsFootnoteMarker(helper.Selection))
			{
				helper.IchEnd = helper.AssocPrev ? (helper.IchAnchor - 1) : (helper.IchAnchor + 1);
				helper.SetSelection(true);
				return true;
			}

			return false;
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
		/// Get/set the editable state of the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Editable
		{
			get
			{
				CheckDisposed();
				return EditingHelper.Editable;
			}
			set
			{
				CheckDisposed();

				EditingHelper.Editable = value;
				BackColor = value ? EditableColor : TeResourceHelper.NonEditableColor;
			}
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
		/// Gets/sets the default WS of the view constructor. If the view has not yet been
		/// constructed, this returns -1.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ViewConstructorWS
		{
			get
			{
				CheckDisposed();
				return m_draftViewVc == null ? -1 : m_draftViewVc.DefaultWs;
			}
			set
			{
				CheckDisposed();

				m_draftViewVc.DefaultWs = value;
				RefreshDisplay();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the back translation writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BackTranslationWS
		{
			get
			{
				CheckDisposed();
				return !IsBackTranslation ? 0 : ViewConstructorWS;
			}
			set
			{
				CheckDisposed();
				if (IsBackTranslation)
					ViewConstructorWS = value;
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
		private CmTranslation GetBackTranslation
		{
			get
			{
				if (IsBackTranslation && TeEditingHelper.CurrentSelection != null)
				{
					// Find out the state of the paragraph where the selection is
					SelectionHelper sel = TeEditingHelper.CurrentSelection;

					if (sel.LevelInfo.Length > 0 &&
						m_fdoCache.GetClassOfObject(sel.LevelInfo[0].hvo) == CmTranslation.kClassId)
						return new CmTranslation (m_fdoCache, sel.LevelInfo[0].hvo);
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
				return m_SelectionHelper;
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
		/// Gets/sets value of filter instance - used to make filters unique per main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FilterInstance
		{
			get
			{
				CheckDisposed();
				return m_filterInstance;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current book filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FilteredScrBooks BookFilter
		{
			get
			{
				CheckDisposed();
				return m_bookFilter;
			}
		}

		#region ITeView Implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location tracker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILocationTracker LocationTracker
		{
			get { return m_locationTracker; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a registry setting that indicates whether the USFM resource viewer is visible
		/// for this view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting UsfmResourceViewerVisible
		{
			get
			{
				return new RegistryBoolSetting(FwSubKey.TE, m_fdoCache.ServerName,
					m_fdoCache.DatabaseName, "USFMResourcesVisible" + Name, false);
			}
		}
		#endregion

		/// <summary>
		/// A set of flags indicating various view properties.
		/// </summary>
		public TeViewType ViewType
		{
			get { return m_viewType; }
		}

		/// <summary>
		/// If this is a back translation view, this is a link to its main view.
		/// </summary>
		public DraftView MainTrans
		{
			get { return m_mainTrans; }
			set
			{
				m_mainTrans = value;
				Debug.Assert(MainTrans != this && MainTrans.RootBox != this.RootBox);
				if (m_editingHelper is TeEditingHelper)
					(m_editingHelper as TeEditingHelper).MainTransView = MainTrans;
			}
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
			ShowHelp.ShowHelpTopic(TeApp.App, "khtpPrint");
		}
		#endregion

		#region Section-related methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the section for the current selection. Returns -1 if the selection
		/// is not in a section.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private int SectionHvo
		{
			get
			{
				ILocationTracker tracker = EditingHelper as ILocationTracker;
				Debug.Assert(tracker != null);
				return tracker.GetSectionHvo(EditingHelper.CurrentSelection,
					SelectionHelper.SelLimitType.Anchor);
			}
		}
		#endregion

		#region Book-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not there are any scripture books in the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool BooksPresent
		{
			get {return BookFilter.BookCount > 0;}
		}
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

			return TeEditingHelper.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
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
		/// insertion point. Ignored if tag is <see cref="ScrBook.ScrBookTags.kflidTitle"/>
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected void SetInsertionPoint(int tag, int book, int section)
		{
			SetInsertionPoint(tag, book, section, 0);
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
		/// insertion point. Ignored if tag is <see cref="ScrBook.ScrBookTags.kflidTitle"/>
		/// </param>
		/// <param name="paragraph">The 0-based index of the paragraph which to put the
		/// insertion point.</param>
		/// ------------------------------------------------------------------------------------
		protected void SetInsertionPoint(int tag, int book, int section, int paragraph)
		{
			TeEditingHelper.SetInsertionPoint(tag, book, section, paragraph);
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
				(int)ScrSection.ScrSectionTags.kflidContent,
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
			if (DataUpdateMonitor.IsUpdateInProgress(DataAccess))
				return true; //discard this event

			IStFootnote footnoteToDel =
				GetFootnoteFromMarkerSelection(EditingHelper.CurrentSelection.Selection);

			if (footnoteToDel == null)
				return true;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoDelFootnote", out undo, out redo);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(this, undo, redo, false))
			using (new DataUpdateMonitor(this, RootBox.DataAccess, this, "DeleteFootnote"))
			{
				IVwSelection sel = RootBox.Selection;
				if (!sel.IsRange)
				{
					// selection is an IP; extend it to contain the ORC footnote marker
					if (!EditingHelper.CurrentSelection.AssocPrev)
						EditingHelper.CurrentSelection.IchEnd++;
					else
					{
						EditingHelper.CurrentSelection.IchAnchor--; //must move end to establish a range
						EditingHelper.CurrentSelection.IchEnd = EditingHelper.CurrentSelection.IchEnd;
					}

					EditingHelper.CurrentSelection.SetLevelInfo(SelectionHelper.SelLimitType.End,
						EditingHelper.CurrentSelection.LevelInfo);
					sel = EditingHelper.CurrentSelection.SetSelection(this, true, false);
				}

				Debug.Assert(m_graphicsManager.VwGraphics != null);
				EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Delete), m_graphicsManager.VwGraphics);
			}

			return true;
		}

		#endregion

		#region IGetTeStVc Members

		/// <summary>
		/// Provide access to our VC so the editing helper can set the annotation whose comment should
		/// not be expanded.
		/// </summary>
		public TeStVc Vc
		{
			get { return m_draftViewVc; }
		}

		#endregion
	}

	#region KeyTermsDraftView class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Draft view for use in the Key terms split view
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermsDraftView : DraftView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new KeyTermsDraftView
		/// </summary>
		/// <param name="cache">The FdoCache</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public KeyTermsDraftView(FdoCache cache, int filterInstance) : base(cache, false,
			false, true, false, TeViewType.KeyTerms, filterInstance)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether or not to show range selections when focus is lost
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool ShowRangeSelAfterLostFocus
		{
			get { return m_fShowRangeSelAfterLostFocus || FindForm().ContainsFocus; }
			set { base.ShowRangeSelAfterLostFocus = value; }
		}
	}
	#endregion
}
