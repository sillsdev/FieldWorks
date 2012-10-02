// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TePrintLayoutTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests for PrintLayout class.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;

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
		public TeDummyPublication(Publication pub, DivisionLayoutMgr div, DateTime printDateTime) :
			base(pub, div, printDateTime)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a instance of the EditingHelper used to process editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper GetInternalEditingHelper()
		{
			CheckDisposed();

			// Create a new virtual property - we don't need it here, but we use it in the view!
			new FilteredScrBooks(m_publication.Cache, 0);

			m_locationTracker = new LocationTrackerImpl(m_publication.Cache, 0);

			return new TeEditingHelper(this, m_publication.Cache, 0, TeViewType.PrintLayout);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the USFM resource viewer is visible for this view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting UsfmResourceViewerVisible
		{
			get { return new RegistryBoolSetting("test", false); }
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
	public class TePrintLayoutTests: BaseTest
	{
		#region Member variables
		private FdoCache m_fdoCache;
		private DummyDivision m_division;
		private TeDummyPublication m_ScrPubCtrl;
		#endregion

		#region Setup and teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the Db connection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("db", "TestLangProj");
			m_fdoCache = FdoCache.Create(cacheOptions);
			// Make sure we don't call InstallLanguage during tests.
			m_fdoCache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_fdoCache != null)
					m_fdoCache.Dispose();
			}
			m_fdoCache = null;
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache and configure the publication if this hasn't already been done.
		/// We expect this to only happen once for the whole fixture. All tests after the first
		/// one re-use the existing publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
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
			if (m_fdoCache != null)
			{
				UndoResult ures = 0;
				while (m_fdoCache.CanUndo)
				{
					m_fdoCache.Undo(out ures);
					if (ures == UndoResult.kuresFailed || ures == UndoResult.kuresError)
						Assert.Fail("ures should not be == " + ures.ToString());
				}
			}
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
			m_division = new DummyDivision(new DummyPrintConfigurer(m_fdoCache, null), 1);
			Publication pub = new Publication(m_fdoCache,
				m_fdoCache.LangProject.TranslatedScriptureOA.PublicationsOC.HvoArray[0]);
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
			Assert.AreEqual("14", tss.Text.Substring(0, 2));
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
			selHelper.LevelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			selHelper.AssocPrev = true;

			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selHelper.LevelInfo);

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = startCharacter;
			selHelper.IchEnd = endCharacter;

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
			m_ScrPubCtrl.PageHeight = 432000; // 6 inches
			m_ScrPubCtrl.PageWidth = 288000; // 4 inches
			m_division.TopMargin = 18000; // Half inch
			m_division.BottomMargin = 18000;
			m_division.InsideMargin = 18000;
			m_division.OutsideMargin = 18000;
			m_ScrPubCtrl.Width = 4 * 96; // represents a window that is 4" wide at 96 DPI
			m_ScrPubCtrl.CreatePages();
			List<Page> pagesToBeDrawn = m_ScrPubCtrl.PrepareToDrawPages(0, 2000);
			IVwLayoutStream layoutStream = m_division.MainLayoutStream;
			IVwRootBox rootbox = layoutStream as IVwRootBox;

			// Get number of footnotes in book.
			IScripture scr = m_fdoCache.LangProject.TranslatedScriptureOA;
			Assert.AreEqual(3, scr.ScriptureBooksOS.Count);
			ScrBook james = (ScrBook)m_fdoCache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			int numFootnotes = james.FootnotesOS.Count;

			// Select the first footnote in James.
			SelectionHelper selHelper = SelectRangeOfChars(rootbox, 2, 2, 3);

			// Delete the footnote marker selection.
			//m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Delete));
			m_ScrPubCtrl.PressKey(new KeyEventArgs(Keys.Delete));

			// Confirm that there is one less footnote in the book.
			Assert.AreEqual(numFootnotes - 1, james.FootnotesOS.Count);
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
			: base(stylesheet, filterInstance, pub, TeViewType.PrintLayout, printDateTime)
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
			return PageFromPrinterY(0, y, fUp, strm, out dyPageScreen, out iDiv);
		}

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
		public override int GetBookHvo(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType)
		{
			if (BookHvo == -1)
				return base.GetBookHvo(selHelper, selLimitType);
			return BookHvo;
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
	public class ScripturePrintLayoutTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private DummyDivision m_division;
		private DummyScripturePublicationNoDb m_ScrPubCtrl;
		private const int m_filterInstance = 345;
		private FilteredScrBooks m_BookFilter;
		private Publication m_dbPub;
		#endregion

		#region Setup and Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache and configure the publication if this hasn't already been done.
		/// We expect this to only happen once for the whole fixture. All tests after the first
		/// one re-use the existing publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			base.Initialize();

			m_BookFilter = new FilteredScrBooks(Cache, m_filterInstance);
			m_BookFilter.ShowAllBooks();
			ParagraphCounterManager.ParagraphCounterType = typeof(TeParaCounter);

			ConfigurePublication(true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
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
			base.Exit();
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
				StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
					ScrStyleNames.NormalParagraph);
				m_scrInMemoryCache.AddRunToMockedPara(para, (i + 2).ToString(), ScrStyleNames.VerseNumber);
				m_scrInMemoryCache.AddRunToMockedPara(para,
					"This is a pretty long paragraph that doesn't say much " +
					"that would be worth saying if it wouldn't be for these test. In these tests " +
					"we simply need some long paragraphs with a lot of text so that we hopefully " +
					"fill more than one page full of text. Let's just pretend this is something " +
					"useful and let's hope we have enough text now so that we can stop here.", null);
			}
			section.AdjustReferences();

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the small book.
		/// </summary>
		/// <param name="nBookNumber">The book number.</param>
		/// <param name="bookName">Name of the book.</param>
		/// <param name="section2">The section2.</param>
		/// ------------------------------------------------------------------------------------
		private IScrBook CreateSmallBook(int nBookNumber, string bookName, out IScrSection section2)
		{
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(nBookNumber, bookName);
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, bookName);
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "Heading 1",
				ScrStyleNames.SectionHead);
			StTxtPara para11 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "Verse one.", null);
			section1.AdjustReferences();

			section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Heading 2",
				ScrStyleNames.SectionHead);
			StTxtPara para21 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "Verse one.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "Verse two.", null);
			StTxtPara para22 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "3", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "Verse one.", null);

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			base.InitializeCache();

			m_scrInMemoryCache.InitializeScrPublications();
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
			styleSheet.Init(Cache, Cache.LangProject.TranslatedScriptureOAHvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			FootnoteVc footnoteVc = new FootnoteVc(-1, TeStVc.LayoutViewTarget.targetPrint,
				Cache.DefaultVernWs);
			footnoteVc.Cache = Cache;
			footnoteVc.Stylesheet = styleSheet;

			DummyPrintConfigurer configurer = m_division.Configurer as DummyPrintConfigurer;
			configurer.RootOnEachPage = true;
			configurer.DependentRootFrag = (int)FootnoteFrags.kfrRootInPageSeq;
			configurer.DependentRootTag = DummyVirtualHandler.InstallDummyHandler(Cache.VwCacheDaAccessor,
					"Scripture", "FootnotesOnPage", (int)CellarModuleDefns.kcptReferenceSequence).Tag;
			configurer.StyleSheet = styleSheet;
			configurer.DependentRootVc = footnoteVc;

			m_dbPub = new Publication(Cache,
				Cache.LangProject.TranslatedScriptureOA.PublicationsOC.HvoArray[0]);
			m_dbPub.BaseFontSize = 12;
			m_dbPub.BaseLineSpacing = 15;
			PubDivision pubDiv = new PubDivision();
			m_dbPub.DivisionsOS.Append(pubDiv);
			pubDiv.PageLayoutOA = new PubPageLayout();
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
			ScrBook genesis = (ScrBook)scr.ScriptureBooksOS[0];
			// Add a footnote in the first book, first section, first paragraph
			IScrSection section = (IScrSection)genesis.SectionsOS[0];
			StFootnote footnote = m_scrInMemoryCache.AddFootnote(genesis,
				(StTxtPara)section.ContentOA.ParagraphsOS[0], 2);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(footnote.Hvo,
							ScrStyleNames.NormalFootnoteParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "A footnote", 0);

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
			int hvoBook = m_ScrPubCtrl.GetBookHvo(selHelper, SelectionHelper.SelLimitType.Anchor);

			Assert.AreEqual(genesis.Hvo, hvoBook);
		}

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests layout of a single book laid out in 1-column mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OneColumnOneBook()
		{
			foreach (PubDivision div in m_dbPub.DivisionsOS)
				div.NumColumns = 1;

			m_BookFilter.UpdateFilter(
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.HvoArray[1]);
			Assert.AreEqual(1, m_BookFilter.BookCount);

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
			foreach (PubDivision div in m_dbPub.DivisionsOS)
				div.NumColumns = 2;

			m_BookFilter.UpdateFilter(
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.HvoArray[1]);
			Assert.AreEqual(1, m_BookFilter.BookCount);

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
			foreach (PubDivision div in m_dbPub.DivisionsOS)
				div.NumColumns = 2;

			m_BookFilter.UpdateFilter(
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.HvoArray[0],
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.HvoArray[1]);
			Assert.AreEqual(2, m_BookFilter.BookCount);

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

			Assert.AreEqual(2, m_ScrPubCtrl.Pages[1].FirstDivOnPage,
				"Page 2 should start with second book (third division)");
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
