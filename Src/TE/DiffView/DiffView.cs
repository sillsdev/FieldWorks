// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DiffView.cs
// Responsibility: TE Team
//
// <remarks>
// Implements the Diff view
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of the diff view
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	internal class DiffView : FwRootSite, ISelectableView, ITeView
	{
		#region Constants
		/// <summary>
		/// Number of levels to be traversed to get to the book title in a diff view.
		/// </summary>
		const int kBookTitleLevelCount = 2;
		/// <summary>
		/// Number of levels to be traversed to get to Scripture sections in a diff view.
		/// </summary>
		const int kSectionLevelCount = 3;
		#endregion

		#region Data members
		private ILocationTracker m_locationTracker;
		private DiffViewVc m_diffViewVc;
		protected readonly IScrBook m_scrBook;
		private readonly IApp m_app;
		/// <summary>Context menu</summary>
		protected System.Windows.Forms.ContextMenu m_contextMenu;
		private readonly DifferenceList m_Differences;
		private System.Windows.Forms.MenuItem m_mnuCopy;
		private System.Windows.Forms.MenuItem m_mnuPaste;
		private System.Windows.Forms.MenuItem m_mnuCut;
		private readonly bool m_fRev;
		private bool m_fSizeChangedSuppression;
		private readonly int m_filterInstance;
		#endregion

		#region Constructor, Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DiffView class
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="book">Scripture book to be displayed as the root in this view</param>
		/// <param name="differences">List of differences</param>
		/// <param name="fRev"><c>true</c> if we display the revision, <c>false</c> if we
		/// display the current version.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public DiffView(FdoCache cache, IScrBook book, DifferenceList differences, bool fRev,
			int filterInstance, IApp app)
			: base(cache)
		{
			m_filterInstance = filterInstance;
			m_scrBook = book;
			m_Differences = differences;
			m_fRev = fRev;
			m_app = app;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			BackColor = SystemColors.Window;
			Editable = false;
			HorizMargin = 10;
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			if (disposing && !IsDisposed)
			{
				if (m_diffViewVc != null)
					m_diffViewVc.Dispose();
			}
			m_diffViewVc = null;
			base.Dispose(disposing);
		}
		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DiffView));
			this.m_contextMenu = new System.Windows.Forms.ContextMenu();
			this.m_mnuCopy = new System.Windows.Forms.MenuItem();
			this.m_mnuPaste = new System.Windows.Forms.MenuItem();
			this.m_mnuCut = new System.Windows.Forms.MenuItem();
			//
			// m_contextMenu
			//
			this.m_contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.m_mnuCut,
																						  this.m_mnuCopy,
																						  this.m_mnuPaste});
			this.m_contextMenu.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_contextMenu.RightToLeft")));
			//
			// m_mnuCopy
			//
			this.m_mnuCopy.Enabled = ((bool)(resources.GetObject("m_mnuCopy.Enabled")));
			this.m_mnuCopy.Index = 1;
			this.m_mnuCopy.Shortcut = ((System.Windows.Forms.Shortcut)(resources.GetObject("m_mnuCopy.Shortcut")));
			this.m_mnuCopy.ShowShortcut = ((bool)(resources.GetObject("m_mnuCopy.ShowShortcut")));
			this.m_mnuCopy.Text = resources.GetString("m_mnuCopy.Text");
			this.m_mnuCopy.Visible = ((bool)(resources.GetObject("m_mnuCopy.Visible")));
			//
			// m_mnuPaste
			//
			this.m_mnuPaste.Enabled = ((bool)(resources.GetObject("m_mnuPaste.Enabled")));
			this.m_mnuPaste.Index = 2;
			this.m_mnuPaste.Shortcut = ((System.Windows.Forms.Shortcut)(resources.GetObject("m_mnuPaste.Shortcut")));
			this.m_mnuPaste.ShowShortcut = ((bool)(resources.GetObject("m_mnuPaste.ShowShortcut")));
			this.m_mnuPaste.Text = resources.GetString("m_mnuPaste.Text");
			this.m_mnuPaste.Visible = ((bool)(resources.GetObject("m_mnuPaste.Visible")));
			//
			// m_mnuCut
			//
			this.m_mnuCut.Enabled = ((bool)(resources.GetObject("m_mnuCut.Enabled")));
			this.m_mnuCut.Index = 0;
			this.m_mnuCut.Shortcut = ((System.Windows.Forms.Shortcut)(resources.GetObject("m_mnuCut.Shortcut")));
			this.m_mnuCut.ShowShortcut = ((bool)(resources.GetObject("m_mnuCut.ShowShortcut")));
			this.m_mnuCut.Text = resources.GetString("m_mnuCut.Text");
			this.m_mnuCut.Visible = ((bool)(resources.GetObject("m_mnuCut.Visible")));
			//
			// DiffView
			//
			this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
			this.AccessibleName = resources.GetString("$this.AccessibleName");
			this.AutoScroll = ((bool)(resources.GetObject("$this.AutoScroll")));
			this.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMargin")));
			this.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMinSize")));
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.Enabled = ((bool)(resources.GetObject("$this.Enabled")));
			this.Font = ((System.Drawing.Font)(resources.GetObject("$this.Font")));
			this.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("$this.ImeMode")));
			this.Location = ((System.Drawing.Point)(resources.GetObject("$this.Location")));
			this.Name = "DiffView";
			this.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("$this.RightToLeft")));
			this.Size = ((System.Drawing.Size)(resources.GetObject("$this.Size")));

		}
		#endregion

		#region Overrides of Control methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the client size changed we have to recalculate the average paragraph height
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (m_fdoCache == null)
				return;
			SelectionHelper selHelper = EditingHelper.CurrentSelection;
			if (selHelper == null)
				return;

			if (SizeChangedSuppression)
				return;

			// We shouldn't have to do PropChanged in the new FDO
			//int paraIndex = selHelper.GetLevelForTag(StTextTags.kflidParagraphs);
			//int hvoPara = selHelper.LevelInfo[paraIndex].hvo;
			//if (m_fdoCache.IsRealObject(hvoPara, StTxtParaTags.kClassId))
			//{
			//    // don't attempt a prop change for an empty paragraph
			//    IStTxtPara para = new StTxtPara(m_fdoCache, hvoPara);
			//    if (para.Contents.Length != 0)
			//    {
			//        m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoPara,
			//            (int)StTxtPara.StParaTags.kflidStyleRules, 0, 1, 1);
			//    }
			//}
			selHelper.SetSelection(this, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="charCode"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool IsInputChar(char charCode)
		{
			return true;
		}
		#endregion

		#region Overrides of RootSite
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
				m_rootb = VwRootBoxClass.Create();

			m_rootb.SetSite(this);

			// Set up a new view constructor.
			m_diffViewVc = new DiffViewVc(m_Differences, m_fRev, m_fdoCache);
			// Change the background colors
			m_diffViewVc.BackColor = BackColor;

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			m_rootb.SetRootObject(m_scrBook.Hvo, m_diffViewVc, (int)ScrFrags.kfrBook,
				m_styleSheet);

			base.MakeRoot();

			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			//TODO:
			//ptmw->RegisterRootBox(qrootb);

			Synchronize(m_rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to always show the selection when we gain focus
		/// </summary>
		/// <param name="vss">The selection state</param>
		/// ------------------------------------------------------------------------------------
		protected override void Activate(VwSelectionState vss)
		{
			m_rootb.Activate(vss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE-specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			Debug.Assert(Cache != null);
			return new DiffViewEditingHelper(this, Cache, m_filterInstance,
				TeViewType.Scripture | TeViewType.DiffView, m_scrBook, m_app);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Root site slaves sometimes need to suppress the effects of OnSizeChanged.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool SizeChangedSuppression
		{
			get { return m_fSizeChangedSuppression; }
			set { m_fSizeChangedSuppression = value; }
		}
		#endregion

		#region Properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the FDO cache
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			set
			{
				CheckDisposed();

				Debug.Assert(m_fdoCache == null || m_fdoCache == value,
					"Changing the cache after its already been set is bad!");
				base.Cache = value;
				m_locationTracker = new DiffViewLocationTracker(this, m_fdoCache);
			}
		}

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
				if (m_diffViewVc != null)
					m_diffViewVc.NeedHighlight = !EditingHelper.Editable;

				BackColor = value ? SystemColors.Window : TeResourceHelper.NonEditableColor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location tracker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILocationTracker LocationTracker
		{
			get { return m_locationTracker; }
		}
		#endregion

		#region Verse-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Bring the paragraph difference into view in the diff view.
		/// </summary>
		/// <param name="para">paragraph containing the difference</param>
		/// <param name="ichMin">starting character position of the difference</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToParaDiff(IScrTxtPara para, int ichMin)
		{
			CheckDisposed();

			int iPara = para.IndexInOwner;
			IStText text = (IStText)para.Owner;
			if (text.Owner is IScrSection)
				SetInsertionPoint(text.OwningFlid, 0, text.Owner.IndexInOwner, iPara, ichMin, false);
			else
				SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0, iPara, ichMin, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to a section difference.
		/// </summary>
		/// <param name="sectionIndex">index of the section to scroll to. If the index
		/// equals the section count, then scroll to the end</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToSectionDiff(int sectionIndex)
		{
			CheckDisposed();

			if (sectionIndex < m_scrBook.SectionsOS.Count)
			{
				// scroll to the beginning of the given section
				SetInsertionPoint(0, sectionIndex, 0, 0, false);
			}
			else
			{
				// scroll to the end of the last section
				IScrSection section = m_scrBook.LastSection;
				IStTxtPara para = section.ContentOA[section.ContentOA.ParagraphsOS.Count - 1];
				SetInsertionPoint(0, sectionIndex - 1, para.IndexInOwner, para.Contents.Length - 1, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a Scripture section, this method returns the index of the paragraph containing
		/// the requested verse and the position of the character immediately following the
		/// verse number in that paragraph. If an exact match isn't found, the closest
		/// approximate place is found.
		/// </summary>
		/// <param name="section">The section whose paragraphs will be searched</param>
		/// <param name="targetRef">The reference being sought</param>
		/// <param name="iPara">The index of the paragraph where the verse was found</param>
		/// <param name="ichPosition">The index of the character immediately following the
		/// verse number in that paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected void FindVerseNumber(IScrSection section, ScrReference targetRef, out int iPara,
			out int ichPosition)
		{
			iPara = 0;
			ichPosition = 0;

			bool fChapterFound = (BCVRef.GetChapterFromBcv(section.VerseRefMin) == targetRef.Chapter);
			foreach (IStTxtPara para in section.ContentOA.ParagraphsOS)
			{
				if (para.Contents.Text == null)
					continue;
				TsRunInfo tsi;
				ITsTextProps ttpRun;
				int ich = 0;
				while (ich < para.Contents.Length)
				{
					// Get props of current run.
					ttpRun = para.Contents.FetchRunInfoAt(ich, out tsi);
					// See if it is our verse number style.
					if (fChapterFound)
					{
						if (ttpRun.Style() == ScrStyleNames.VerseNumber)
						{
							// The whole run is the verse number. Extract it.
							string sVerseNum = para.Contents.Text.Substring(tsi.ichMin,
								tsi.ichLim - tsi.ichMin);
							int startVerse, endVerse;
							ScrReference.VerseToInt(sVerseNum, out startVerse, out endVerse);
							if ((targetRef.Verse >= startVerse && targetRef.Verse <= endVerse)
								|| targetRef.Verse < startVerse)
							{
								ichPosition = tsi.ichLim;
								return;
							}
						}
					}
					// See if it is our chapter number style.
					else if (ttpRun.Style() == ScrStyleNames.ChapterNumber)
					{
						try
						{
							// Assume the whole run is the chapter number. Extract it.
							string sChapterNum = para.Contents.Text.Substring(tsi.ichMin,
								tsi.ichLim - tsi.ichMin);
							fChapterFound = (ScrReference.ChapterToInt(sChapterNum) == targetRef.Chapter);
						}
						catch (ArgumentException)
						{
							// ignore runs with invalid Chapter numbers
						}
					}
					ich = tsi.ichLim;
				}
				iPara++;
			}
			iPara = 0; // Couldn't find it.
		}
		#endregion

		#region SetInsertionPoint methods
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
		/// <returns>True if it succeeded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool SetInsertionPoint(int book, int section, int para, int character,
			bool fAssocPrev)
		{
			return SetInsertionPoint(ScrSectionTags.kflidContent, book, section,
				para, character, fAssocPrev);
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
		/// insertion point. Ignored if tag is <see cref="T:ScrBookTags.kflidTitle"/></param>
		/// <param name="paragraph">The 0-based index of the paragraph which to put the
		/// insertion point.</param>
		/// <param name="character">index of character where insertion point will be set</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <returns>True if it succeeded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool SetInsertionPoint(int tag, int book, int section, int paragraph, int character,
			bool fAssocPrev)
		{
			CheckDisposed();

			SelectionHelper selHelper = new SelectionHelper(); //SelectionHelper.Create(Callbacks.EditedRootBox.Site);// new SelectionHelper();
			int cLev;
			if (tag == ScrBookTags.kflidTitle)
			{
				cLev = kBookTitleLevelCount;
				selHelper.NumberOfLevels = cLev;
				selHelper.LevelInfo[cLev - 1].tag = tag;
			}
			else
			{
				cLev = kSectionLevelCount;
				selHelper.NumberOfLevels = cLev;
				selHelper.LevelInfo[cLev - 1].tag = ScrBookTags.kflidSections;
				selHelper.LevelInfo[cLev - 1].ihvo = section;
				selHelper.LevelInfo[cLev - 2].tag = tag;
			}
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = paragraph;
			selHelper.AssocPrev = fAssocPrev;
			selHelper.TextPropId = StTxtParaTags.kflidContents;

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = character;

			selHelper.SetIhvoRoot(SelectionHelper.SelLimitType.Anchor, book);

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = selHelper.SetSelection(this, true, true);

			// Now set the scroll position where the IP will be centered vertically.
			AutoScrollPosition = new Point(-AutoScrollPosition.X,
				-(AutoScrollPosition.Y - IPDistanceFromWindowTop(vwsel) + Height / 2));

			// If the selection is still not visible (which should only be the case if
			// we're at the end of the view), just take whatever MakeSelectionVisible()
			// gives us.
			if (!IsSelectionVisible(vwsel))
				ScrollSelectionIntoView(vwsel, VwScrollSelOpts.kssoDefault);

			if (vwsel == null)
				Debug.WriteLine("SetSelection failed in DiffView.SetInsertionPoint()");
			return (vwsel != null);
		}
		#endregion

		#region ISelectableView implementation
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ActivateView()
		{
			CheckDisposed();

			PerformLayout();
			Show();
			Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BaseInfoBarCaption
		{
			get
			{
				CheckDisposed();
				return null;
			}
			set
			{
				CheckDisposed();
				/* do nothing */
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeactivateView()
		{
			CheckDisposed();

			Hide();
		}

		#endregion

		#region DiffViewLocationTracker class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// ILocationTracker interface for the DiffView
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DiffViewLocationTracker : LocationTrackerImpl
		{
			private DiffView m_diffView;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:LocationTrackerImpl"/> class.
			/// </summary>
			/// <param name="diffView">The diff view</param>
			/// <param name="cache">The cache.</param>
			/// ------------------------------------------------------------------------------------
			public DiffViewLocationTracker(DiffView diffView, FdoCache cache) : base(cache, 0)
			{
				m_diffView = diffView;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the HVO of the current book, or -1 if there is no current book (e.g. no
			/// selection or empty view).
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>The book hvo.</returns>
			/// ------------------------------------------------------------------------------------
			public override IScrBook GetBook(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_diffView.m_scrBook;
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
			public override int GetBookIndex(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{

				return m_cache.DomainDataByFlid.GetObjIndex(m_diffView.m_scrBook.Owner.Hvo,
					m_diffView.m_scrBook.OwningFlid, m_diffView.m_scrBook.Hvo);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the number of levels for the given tag.
			/// </summary>
			/// <param name="tag">The tag.</param>
			/// <returns>Number of levels</returns>
			/// ------------------------------------------------------------------------------------
			public override int GetLevelCount(int tag)
			{
				return base.GetLevelCount(tag) - 1;
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
			public override void SetBookAndSection(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType, int iBook, int iSection)
			{
				if (selHelper == null)
					return;

				// we can only deal with one book
				if (iBook != GetBookIndex(null, selLimitType) || iSection < 0)
					return;

				int nLevels = selHelper.GetNumberOfLevels(selLimitType);
				selHelper.GetLevelInfo(selLimitType)[nLevels - 1].tag = ScrBookTags.kflidSections;
				selHelper.GetLevelInfo(selLimitType)[nLevels - 1].ihvo = iSection;
			}
		}

		#endregion
	}
}
