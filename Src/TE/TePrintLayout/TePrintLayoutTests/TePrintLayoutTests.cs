// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TePrintLayoutTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests for PrintLayout class.
// </remarks>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE
{
	#region TeDummyPublication class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeDummyPublication : DummyPublication, ITeView
	{
		private LocationTrackerImpl m_locationTracker;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeDummyPublication"/> class.
		/// </summary>
		/// <param name="pub">The pub.</param>
		/// <param name="div">The div.</param>
		/// <param name="printDateTime">The print date time.</param>
		/// ------------------------------------------------------------------------------------
		public TeDummyPublication(IPublication pub, DivisionLayoutMgr div, DateTime printDateTime) :
			base(pub, div, printDateTime)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a instance of the EditingHelper used to process editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override RootSiteEditingHelper GetInternalEditingHelper()
		{
			// Create a new virtual property - we don't need it here, but we use it in the view!
			m_publication.Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(0);

			m_locationTracker = new LocationTrackerImpl(m_publication.Cache, 0);

			return new TeEditingHelper(this, m_publication.Cache, 0, TeViewType.PrintLayout, null);
		}

		#region ITeView Members
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
	}
	#endregion

	#region class TePrintLayoutTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of layout of a printable page in a FieldWorks application.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TePrintLayoutTests: PrintLayoutTestBase
	{
		#region Member variables
		private DummyDivision m_division;
		private TeDummyPublication m_ScrPubCtrl;
		private IScrBook m_book;
		#endregion

		#region Setup and teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache and configure the publication if this hasn't already been done.
		/// We expect this to only happen once for the whole fixture. All tests after the first
		/// one re-use the existing publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_book = CreateBookData(1, "Genesis");
			ConfigurePublication();
			m_ScrPubCtrl.IsLeftBound = true;
			m_ScrPubCtrl.Zoom = 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the FDO cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public virtual void TearDown()
		{
			if (m_division != null)
			{
				m_division.m_hPagesBroken.Clear();
				m_division.Dispose();
				m_division = null;
			}

			// Make sure we close all the rootboxes
			if (m_ScrPubCtrl != null)
			{
				m_ScrPubCtrl.Dispose();
				m_ScrPubCtrl = null;
			}

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure a PublicationControl
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ConfigurePublication()
		{
			if (m_division != null)
				m_division.Dispose();
			if (m_ScrPubCtrl != null)
				m_ScrPubCtrl.Dispose();
			m_division = new DummyDivision(new DummyPrintConfigurer(Cache, null), 1);
			IPublication pub =
				Cache.LangProject.TranslatedScriptureOA.PublicationsOC.ToArray()[0];
			m_ScrPubCtrl = new TeDummyPublication(pub, m_division, DateTime.Now);
			m_ScrPubCtrl.Configure();

			// Check initial state
			Assert.AreEqual(m_division, m_ScrPubCtrl.Divisions[0]);
			Assert.IsNotNull(m_division.MainVc as StVc);
			IVwLayoutStream layoutStream = m_division.MainLayoutStream;
			Assert.IsNotNull(layoutStream);
			Assert.AreEqual(layoutStream, m_division.MainLayoutStream,
				"MainLayoutStream should not contruct a new stream each time");
			IVwRootBox rootbox = layoutStream as IVwRootBox;
			Assert.IsNotNull(rootbox);
			IVwSelection sel = rootbox.Selection;
			Assert.IsNotNull(sel);
			int ich, hvo, tag, ws; // dummies
			bool fAssocPrev;
			ITsString tss;
			sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			Assert.AreEqual("11Verse one.", tss.Text.Substring(0, 12));
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a range selection (adapted from TePrintLayout.cs).
		/// </summary>
		/// <param name="rootbox">The rootbox.</param>
		/// <param name="iPara">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <returns>The selection helper</returns>
		/// ------------------------------------------------------------------------------------
		private SelectionHelper SelectRangeOfChars(IVwRootBox rootbox, int iPara,
			int startCharacter, int endCharacter)
		{
			if (rootbox == null)
				return null;  // can't make a selection

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 1;

			selHelper.LevelInfo[0].ihvo = iPara;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.AssocPrev = true;

			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selHelper.LevelInfo);

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = startCharacter;
			selHelper.IchEnd = endCharacter;
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, StTxtParaTags.kflidContents);
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, StTxtParaTags.kflidContents);

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = selHelper.SetSelection(rootbox.Site, true, false,
				VwScrollSelOpts.kssoDefault);

			Assert.IsNotNull(vwsel);
			Application.DoEvents();

			return selHelper;
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting a footnote selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteSelection()
		{
			// Add a footnote to the last section in the book of Genesis. (The last section is
			// what is contained in our rootbox).
			Assert.AreEqual(3, m_book.SectionsOS.Count);
			IScrTxtPara para = (IScrTxtPara)m_book.SectionsOS[1].ContentOA.ParagraphsOS[0];
			AddFootnote(m_book, para, para.Contents.Length, "Footnote to delete");

			// Get number of footnotes in book.
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			Assert.AreEqual(1, scr.ScriptureBooksOS.Count);
			int numFootnotes = m_book.FootnotesOS.Count;

			m_ScrPubCtrl.PageHeight = 432000; // 6 inches
			m_ScrPubCtrl.PageWidth = 288000; // 4 inches
			m_division.TopMargin = 18000; // Half inch
			m_division.BottomMargin = 18000;
			m_division.InsideMargin = 18000;
			m_division.OutsideMargin = 18000;
			m_ScrPubCtrl.Width = 4 * 96; // represents a window that is 4" wide at 96 DPI
			m_ScrPubCtrl.CreatePages();
			m_ScrPubCtrl.PrepareToDrawPages(0, 2000);
			IVwLayoutStream layoutStream = m_division.MainLayoutStream;
			IVwRootBox rootbox = layoutStream as IVwRootBox;

			// Select the first footnote in the last section of the book.
			SelectRangeOfChars(rootbox, 0, para.Contents.Length - 2, para.Contents.Length - 1);

			// Delete the footnote marker selection.
			//m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Delete));
			m_lp.Cache.ActionHandlerAccessor.EndUndoTask(); // Delete key generates own UOW
			m_ScrPubCtrl.PressKey(new KeyEventArgs(Keys.Delete));
			m_lp.Cache.ActionHandlerAccessor.BeginUndoTask("nonsence", "redo nonsence");

			// Confirm that there is one less footnote in the book.
			Assert.AreEqual(numFootnotes - 1, m_book.FootnotesOS.Count);
		}
		#endregion
	}
	#endregion

	#region DummyScripturePublicationNoDb
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyScripturePublicationNoDb : ScripturePublication
	{
		#region Data members
		private Point m_scrollPosition;
		/// <summary>Allows for tests to override to test simplex printing, but note that this
		/// must be set before the publication is configured.</summary>
		public MultiPageLayout m_pageLayout = MultiPageLayout.Duplex;
		/// <summary></summary>
		public int BookHvo = -1;
		#endregion

		#region Constructors
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyPublication"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyScripturePublicationNoDb(IPublication pub, FwStyleSheet stylesheet,
			DivisionLayoutMgr div, DateTime printDateTime, int filterInstance)
			: base(stylesheet, filterInstance, pub, TeViewType.PrintLayout, printDateTime, null, null,
			pub.Cache.DefaultVernWs)
		{
			m_printerDpiX = 720.0f;
			m_printerDpiY = 1440.0f;
		}
		#endregion

		#region Exposed Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PublicationControl.AddsSharedSubstream.
		/// </summary>
		/// <param name="subStream">The substream.</param>
		/// ------------------------------------------------------------------------------------
		internal new void AddSharedSubstream(IVwLayoutStream subStream)
		{
			base.AddSharedSubstream(subStream);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PublicationControl.Configure method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void Configure()
		{
			CheckDisposed();

			base.Configure();
			// In real life this is done in the OnPaint method - but if we don't display the
			// view this isn't called, so we do it now.
			foreach (DivisionLayoutMgr div in m_divisions)
			{
				try
				{
					div.MainRootBox.MakeSimpleSel(true, true, false, true);
				}
				catch
				{
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PublicationControl.CreatePages method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void CreatePages()
		{
			CheckDisposed();

			base.CreatePages();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose overridden method in the base class.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void PressKey(KeyEventArgs e)
		{
			CheckDisposed();

			base.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the PageFromPrinterY method
		/// </summary>
		/// <param name="y">The y.</param>
		/// <param name="fUp">If the point is right at a page boundary, return the first
		/// page if this is true, the second if it is false.</param>
		/// <param name="strm">The STRM.</param>
		/// <param name="dyPageScreen">The dy page screen.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Page CallPageFromPrinterY(int y, bool fUp, IVwLayoutStream strm,
			out int dyPageScreen)
		{
			int iDiv; // not used in the tests
			bool layedOutPage; // not used in the tests
			return PageFromPrinterY(0, y, fUp, strm, out dyPageScreen, out iDiv, out layedOutPage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the version of RefreshDisplay that bypasses the code to postpone refresh when
		/// not visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void CallRefreshDisplay()
		{
			RefreshDisplay(false);
		}

		#endregion

		#region Exposed properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the PublicationControl.Divisions property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new List<DivisionLayoutMgr> Divisions
		{
			get
			{
				CheckDisposed();

				return base.Divisions;
			}
		}
		#endregion

		#region Overridden properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value that indicates the multiple-page option for this publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override MultiPageLayout PageLayoutMode
		{
			get
			{
				return m_pageLayout;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dpi X printer.
		/// </summary>
		/// <value>The dpi X printer.</value>
		/// ------------------------------------------------------------------------------------
		public new int DpiXPrinter
		{
			get
			{
				CheckDisposed();
				return (int)base.DpiXPrinter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dpi Y printer.
		/// </summary>
		/// <value>The dpi Y printer.</value>
		/// ------------------------------------------------------------------------------------
		public new int DpiYPrinter
		{
			get
			{
				CheckDisposed();
				return (int)base.DpiYPrinter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scroll position.
		/// </summary>
		/// <value>The scroll position.</value>
		/// ------------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			get
			{
				CheckDisposed();
				if (Parent != null)
					return base.ScrollPosition;
				return m_scrollPosition;
			}
			set
			{
				CheckDisposed();
				if (Parent != null)
					base.ScrollPosition = value;
				else
					m_scrollPosition = new Point(-value.X, -value.Y);
			}
		}

		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this so that the selection won't try to be shown (lays out pages)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void MakeSureSelectionIsVisible()
		{
			// do nothing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does nothing (makes the compiler happy :-).
		/// </summary>
		/// <param name="loc">The loc.</param>
		/// ------------------------------------------------------------------------------------
		public override void ShowContextMenu(Point loc)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the page.
		/// </summary>
		/// <param name="pub">Publication that owns this page</param>
		/// <param name="iFirstDivOnPage">Index (into array of divisions in the publication) of
		/// the first division expected to lay out on this page</param>
		/// <param name="ypOffsetFromTopOfDiv">Estimated number of pixels (in source/layout
		/// units) from the top of the division to the top of this page</param>
		/// <param name="pageNumber">The page number for this page. Page numbers can restart for
		/// different divisions, so this should not be regarded as an index into an array of
		/// pages.</param>
		/// <param name="dypTopMarginInPrinterPixels">The top margin in printer pixels.</param>
		/// <param name="dypBottomMarginInPrinterPixels">The bottom margin in printer pixels.</param>
		/// <returns>The new page</returns>
		/// <remarks>We do this in a virtual method so that tests can create special pages.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override Page CreatePage(PublicationControl pub, int iFirstDivOnPage,
			int ypOffsetFromTopOfDiv, int pageNumber, int dypTopMarginInPrinterPixels,
			int dypBottomMarginInPrinterPixels)
		{
			return new DummyPage(pub, iFirstDivOnPage, ypOffsetFromTopOfDiv, pageNumber,
				dypTopMarginInPrinterPixels, dypBottomMarginInPrinterPixels);
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
		public override IScrBook GetBook(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType)
		{
			if (BookHvo == -1)
				return base.GetBook(selHelper, selLimitType);
			return Cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(BookHvo);
		}
		#endregion
	}
	#endregion

	#region ScripturePrintLayoutTests without database
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests layout of Scripture without any real database
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScripturePrintLayoutTests : PrintLayoutTestBase
	{
		#region Data members
		private DummyDivision m_division;
		private DummyScripturePublicationNoDb m_ScrPubCtrl;
		private const int m_filterInstance = 345;
		private FilteredScrBooks m_BookFilter;
		private IPublication m_dbPub;
		#endregion

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the paragraph counter for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().RegisterViewTypeId<TeParaCounter>((int)TeViewGroup.Scripture);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache and configure the publication if this hasn't already been done.
		/// We expect this to only happen once for the whole fixture. All tests after the first
		/// one re-use the existing publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_BookFilter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);
			m_BookFilter.ShowAllBooks();

			ConfigurePublication(true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			if (m_division != null)
			{
				if (m_division.Configurer != null)
				{
					var disposable = m_division.Configurer.DependentRootVc as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
				m_division.m_hPagesBroken.Clear();
				m_division.Dispose();
				m_division = null;
			}

			// Make sure we close all the rootboxes
			if (m_ScrPubCtrl != null)
			{
				m_ScrPubCtrl.Dispose();
				m_ScrPubCtrl = null;
			}
			base.TestTearDown();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book with 2 sections with the following layout:
		/// bookName
		/// Heading 1
		/// (1)1Verse one.
		/// Heading 2
		/// (2)1Verse one.2Verse two.
		/// (3)1Verse one.
		/// 2This is a pretty long...
		/// ..
		/// 11This is a pretty long...
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <param name="nBookNumber">The book number.</param>
		/// <param name="bookName">Name of the book.</param>
		/// <param name="nParas">The number of paras to add.</param>
		/// <returns>the book for testing</returns>
		/// ------------------------------------------------------------------------------------
		private IScrBook CreateBook(int nBookNumber, string bookName, int nParas)
		{
			IScrBook book;
			IScrSection section;
			book = CreateSmallBook(nBookNumber, bookName, out section);

			for (int i = 0; i < nParas; i++)
			{
				IStTxtPara para = AddParaToMockedSectionContent(section,
					ScrStyleNames.NormalParagraph);
				AddRunToMockedPara(para, (i + 2).ToString(), ScrStyleNames.VerseNumber);
				AddRunToMockedPara(para,
					"This is a pretty long paragraph that doesn't say much " +
					"that would be worth saying if it wouldn't be for these test. In these tests " +
					"we simply need some long paragraphs with a lot of text so that we hopefully " +
					"fill more than one page full of text. Let's just pretend this is something " +
					"useful and let's hope we have enough text now so that we can stop here.", null);
			}

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			CreateBook(1, "Genesis", 10);
			CreateBook(2, "Exodus", 30);
			CreateBook(3, "Leviticus", 10);
			CreateBook(4, "Numeri", 10);
			CreateBook(5, "Deuteronomy", 10);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure a PublicationControl
		/// </summary>
		/// <param name="fAddSubStream">if set to <c>true</c> add subordinate stream.</param>
		/// <param name="fAddContent">if set to <c>true</c> add real content, otherwise add
		/// rectangle for paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void ConfigurePublication(bool fAddSubStream, bool fAddContent)
		{
			// When called for test setup, they will be null.
			// When called from within as test
			if (m_ScrPubCtrl != null)
				m_ScrPubCtrl.Dispose();
			if (m_division != null)
				m_division.Dispose();
			m_division = new DummyDivision(new DummyLazyPrintConfigurer(Cache, fAddSubStream,
				fAddContent), 1);

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo,
				ScriptureTags.kflidStyles);

			FootnoteVc footnoteVc = new FootnoteVc(TeStVc.LayoutViewTarget.targetPrint, -1);
			footnoteVc.Cache = Cache;
			footnoteVc.Stylesheet = styleSheet;

			DummyPrintConfigurer configurer = m_division.Configurer as DummyPrintConfigurer;
			configurer.RootOnEachPage = true;
			configurer.DependentRootFrag = (int)FootnoteFrags.kfrRootInPageSeq;
			configurer.DependentRootTag = ScrBookTags.kflidFootnotes;
			configurer.StyleSheet = styleSheet;
			configurer.DependentRootVc = footnoteVc;

			m_dbPub = Cache.LangProject.TranslatedScriptureOA.PublicationsOC.ToArray()[0];
			m_dbPub.BaseFontSize = 12000;
			m_dbPub.BaseLineSpacing = 15;
			IPubDivision pubDiv =
				Cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
			m_dbPub.DivisionsOS.Add(pubDiv);
			pubDiv.PageLayoutOA = Cache.ServiceLocator.GetInstance<IPubPageLayoutFactory>().Create();
			pubDiv.NumColumns = 1;
			pubDiv.StartAt = DivisionStartOption.NewPage;

			m_ScrPubCtrl = new DummyScripturePublicationNoDb(m_dbPub, styleSheet, m_division, DateTime.Now,
				m_filterInstance);

			m_ScrPubCtrl.IsLeftBound = true;

			// Set up the publication
			m_ScrPubCtrl.PageHeight = 72000 * 11; // 11 inches
			m_ScrPubCtrl.PageWidth = (int)(72000 * 8.5); // 8.5 inches
			m_division.TopMargin = 36000; // Half inch
			m_division.BottomMargin = 18000; // Quarter inch
			m_division.InsideMargin = 9000; // 1/8 inch
			m_division.OutsideMargin = 4500; // 1/16 inch
			m_ScrPubCtrl.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_ScrPubCtrl.Configure();
		}

		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetBookHvo returns the correct book Hvo if the selection is in a
		/// footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookHvoWithFootnotes()
		{
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			IScrBook genesis = scr.ScriptureBooksOS[0];
			// Add a footnote in the first book, first section, first paragraph
			IScrSection section = genesis.SectionsOS[0];
			IStFootnote footnote = AddFootnote(genesis,
				(IStTxtPara)section.ContentOA.ParagraphsOS[0], 2, "A footnote");

			m_ScrPubCtrl.CallRefreshDisplay();
			m_ScrPubCtrl.CreatePages();
			m_ScrPubCtrl.PrepareToDrawPages(0, m_ScrPubCtrl.AutoScrollMinSize.Height * 2);

			// Simulate setting the IP at the beginning of the footnote
			bool fFoundPageWithFootnotes = false;
			foreach (DummyPage page in m_ScrPubCtrl.Pages)
			{
				if (page.DependentObjectsRootStream != null)
				{
					fFoundPageWithFootnotes = true;
					((IVwRootBox)page.DependentObjectsRootStream).MakeSimpleSel(true, true, false, true);
					m_ScrPubCtrl.FocusedStream = page.DependentObjectsRootStream;
					break;
				}
			}
			Assert.IsTrue(fFoundPageWithFootnotes, "Can't run this test without finding any footnotes");

			SelectionHelper selHelper = SelectionHelper.Create(m_ScrPubCtrl);
			IScrBook book = m_ScrPubCtrl.GetBook(selHelper, SelectionHelper.SelLimitType.Anchor);

			Assert.AreEqual(genesis, book);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests layout of a single book laid out in 1-column mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OneColumnOneBook()
		{
			foreach (IPubDivision div in m_dbPub.DivisionsOS)
				div.NumColumns = 1;

			m_BookFilter.FilteredBooks = new [] {Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1]};
			Assert.AreEqual(1, m_BookFilter.BookCount);

			m_ScrPubCtrl.CallRefreshDisplay();
			m_ScrPubCtrl.Configure();
			m_ScrPubCtrl.CreatePages();

			// Verify that both divisions are one-column
			Assert.AreEqual(2, m_ScrPubCtrl.Divisions.Count);
			DivisionLayoutMgr divTitleIntro = m_ScrPubCtrl.Divisions[0];
			DivisionLayoutMgr divScripture = m_ScrPubCtrl.Divisions[1];
			Assert.AreEqual(divTitleIntro.AvailableMainStreamColumWidthInPrinterPixels,
				divScripture.AvailableMainStreamColumWidthInPrinterPixels);
			Assert.AreEqual(divTitleIntro.AvailableMainStreamColumWidthInPrinterPixels,
				divTitleIntro.AvailablePageWidthInPrinterPixels);
			Assert.AreNotEqual(DivisionStartOption.Continuous, divTitleIntro.StartAt);
			Assert.AreEqual(DivisionStartOption.Continuous, divScripture.StartAt);
			Assert.Greater(divTitleIntro.MainRootBox.Height, 0);
			Assert.Greater(divScripture.MainRootBox.Height, 0);
			Assert.Greater(divScripture.MainRootBox.Height, divTitleIntro.MainRootBox.Height);

			Assert.Greater(m_ScrPubCtrl.PageCount, 2);

			Assert.IsNotNull(divTitleIntro.MainVc as ScriptureBookIntroVc);
			Assert.IsNotNull(divScripture.MainVc as ScriptureBodyVc);

			foreach (DivisionLayoutMgr div in m_ScrPubCtrl.Divisions)
			{
				IVwLayoutStream layoutStream = div.MainLayoutStream;
				Assert.IsNotNull(layoutStream);
				Assert.IsNotNull(layoutStream as IVwRootBox);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests layout of a single book laid out in 2-column mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoColumnOneBook()
		{
			foreach (IPubDivision div in m_dbPub.DivisionsOS)
				div.NumColumns = 2;

			m_BookFilter.FilteredBooks =
				new [] {Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1]};
			Assert.AreEqual(1, m_BookFilter.BookCount);

			m_ScrPubCtrl.CallRefreshDisplay();
			m_ScrPubCtrl.Configure();
			m_ScrPubCtrl.CreatePages();

			Assert.AreEqual(2, m_ScrPubCtrl.Divisions.Count);
			DivisionLayoutMgr divTitleIntro = m_ScrPubCtrl.Divisions[0];
			DivisionLayoutMgr divScripture = m_ScrPubCtrl.Divisions[1];
			Assert.AreNotEqual(divTitleIntro.AvailableMainStreamColumWidthInPrinterPixels,
				divScripture.AvailableMainStreamColumWidthInPrinterPixels);
			Assert.AreEqual(divTitleIntro.AvailableMainStreamColumWidthInPrinterPixels,
				divTitleIntro.AvailablePageWidthInPrinterPixels);
			Assert.AreNotEqual(DivisionStartOption.Continuous, divTitleIntro.StartAt);
			Assert.AreEqual(DivisionStartOption.Continuous, divScripture.StartAt);
			Assert.Greater(divTitleIntro.MainRootBox.Height, 0);
			Assert.Greater(divScripture.MainRootBox.Height, 0);
			Assert.Greater(divScripture.MainRootBox.Height, divTitleIntro.MainRootBox.Height);

			Assert.Greater(m_ScrPubCtrl.PageCount, 1);

			Assert.IsNotNull(divTitleIntro.MainVc as ScriptureBookIntroVc);
			Assert.IsNotNull(divScripture.MainVc as ScriptureBodyVc);

			foreach (DivisionLayoutMgr div in m_ScrPubCtrl.Divisions)
			{
				IVwLayoutStream layoutStream = div.MainLayoutStream;
				Assert.IsNotNull(layoutStream);
				Assert.IsNotNull(layoutStream as IVwRootBox);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests layout of two books laid out in 2-column mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoColumnTwoBooks()
		{
			foreach (IPubDivision div in m_dbPub.DivisionsOS)
				div.NumColumns = 2;

			m_BookFilter.FilteredBooks =
				new [] {Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0],
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1]};
			Assert.AreEqual(2, m_BookFilter.BookCount);

			m_ScrPubCtrl.CallRefreshDisplay();
			m_ScrPubCtrl.Configure();
			m_ScrPubCtrl.CreatePages();

			Assert.AreEqual(4, m_ScrPubCtrl.Divisions.Count);
			DivisionLayoutMgr divTitleIntroFirstBook = m_ScrPubCtrl.Divisions[0];
			DivisionLayoutMgr divScriptureFirstBook = m_ScrPubCtrl.Divisions[1];
			DivisionLayoutMgr divTitleIntroSecondBook = m_ScrPubCtrl.Divisions[2];
			DivisionLayoutMgr divScriptureSecondBook = m_ScrPubCtrl.Divisions[3];
			Assert.Greater(divTitleIntroFirstBook.AvailableMainStreamColumWidthInPrinterPixels,
				divScriptureFirstBook.AvailableMainStreamColumWidthInPrinterPixels);
			Assert.AreEqual(divTitleIntroFirstBook.AvailableMainStreamColumWidthInPrinterPixels,
				divTitleIntroFirstBook.AvailablePageWidthInPrinterPixels);
			Assert.AreEqual(divTitleIntroFirstBook.AvailableMainStreamColumWidthInPrinterPixels,
				divTitleIntroSecondBook.AvailableMainStreamColumWidthInPrinterPixels);
			Assert.AreEqual(divScriptureFirstBook.AvailableMainStreamColumWidthInPrinterPixels,
				divScriptureSecondBook.AvailableMainStreamColumWidthInPrinterPixels);
			Assert.AreNotEqual(DivisionStartOption.Continuous, divTitleIntroFirstBook.StartAt);
			Assert.AreEqual(DivisionStartOption.Continuous, divScriptureFirstBook.StartAt);
			Assert.AreNotEqual(DivisionStartOption.Continuous, divTitleIntroSecondBook.StartAt);
			Assert.AreEqual(DivisionStartOption.Continuous, divScriptureSecondBook.StartAt);
			Assert.Greater(divTitleIntroFirstBook.MainRootBox.Height, 0);
			Assert.Greater(divScriptureFirstBook.MainRootBox.Height, 0);
			// Intro of Philemon is quite long, so we just test that the heights of the two division are not equal
			Assert.AreNotEqual(divScriptureFirstBook.MainRootBox.Height, divTitleIntroFirstBook.MainRootBox.Height);
			Assert.Greater(divTitleIntroSecondBook.MainRootBox.Height, 0);
			Assert.Greater(divScriptureSecondBook.MainRootBox.Height, 0);
			Assert.Greater(divScriptureSecondBook.MainRootBox.Height, divTitleIntroSecondBook.MainRootBox.Height);

			Assert.AreEqual(2, m_ScrPubCtrl.Pages[2].FirstDivOnPage,
				"Page 3 should start with second book (third division)");
			Assert.AreEqual(0, m_ScrPubCtrl.Pages[1].OffsetFromTopOfDiv(
				m_ScrPubCtrl.Divisions[m_ScrPubCtrl.Pages[2].FirstDivOnPage]),
				"Third division should start on top of page 2");

			Assert.Greater(m_ScrPubCtrl.PageCount, 2);

			Assert.IsNotNull(divTitleIntroFirstBook.MainVc as ScriptureBookIntroVc);
			Assert.IsNotNull(divScriptureFirstBook.MainVc as ScriptureBodyVc);
			Assert.IsNotNull(divTitleIntroSecondBook.MainVc as ScriptureBookIntroVc);
			Assert.IsNotNull(divScriptureSecondBook.MainVc as ScriptureBodyVc);

			foreach (DivisionLayoutMgr div in m_ScrPubCtrl.Divisions)
			{
				IVwLayoutStream layoutStream = div.MainLayoutStream;
				Assert.IsNotNull(layoutStream);
				Assert.IsNotNull(layoutStream as IVwRootBox);
			}
		}
		#endregion
	}
	#endregion
}
