// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FootnoteEditingHelperTests.cs
// Responsibility: TE Team

using System;

using NMock;
using NMock.Constraints;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	#region DummyFootnoteEditingHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFootnoteEditingHelper : FootnoteEditingHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyFootnoteEditingHelper"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="rootsite">The rootsite.</param>
		/// ------------------------------------------------------------------------------------
		public DummyFootnoteEditingHelper(FdoCache cache, SimpleRootSite rootsite) :
			base(rootsite, cache, 0, null, TeViewType.FootnoteView | TeViewType.Horizontal, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a flag indicating whether to defer setting a selection until the end of the
		/// Unit of Work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool DeferSelectionUntilEndOfUOW
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the selection in footnote.
		/// </summary>
		/// <param name="footnote">The footnote.</param>
		/// <param name="book">The book.</param>
		/// <param name="iBook">The 0-based index of the book.</param>
		/// <param name="ichStart">The 0-based starting character index.</param>
		/// <param name="ichEnd">The 0-based ending character index.</param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionInFootnote(IStFootnote footnote, IScrBook book,
			int iBook, int ichStart, int ichEnd)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("GetTextPropId", StTxtParaTags.kflidContents, typeof(SelectionHelper.SelLimitType));
			fakeSelHelper.SetupResult("NumberOfLevels", 3);
			// Setup the anchor
			SelLevInfo[] topInfo = new SelLevInfo[3];
			IStTxtPara para = footnote[0];
			topInfo[0].tag = StTextTags.kflidParagraphs;
			topInfo[0].ihvo = 0;	// only one para per footnote allowed
			topInfo[0].hvo = para.Hvo;

			topInfo[1].tag = ScrBookTags.kflidFootnotes;
			topInfo[1].ihvo = footnote.IndexInOwner;
			topInfo[1].hvo = footnote.Hvo;

			topInfo[2].tag = BookFilter.Tag;
			topInfo[2].ihvo = iBook;
			topInfo[2].hvo = book.Hvo;

			// Setup the end
			SelLevInfo[] bottomInfo = new SelLevInfo[3];
			for(int i = 0; i < 3; i++)
				bottomInfo[i] = topInfo[i];

			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("IchAnchor", ichStart);
			fakeSelHelper.SetupResult("IchEnd", ichEnd);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetIch", ichStart,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetIch", ichEnd,
				SelectionHelper.SelLimitType.Bottom);
			m_currentSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is current selection out of date.
		/// In tests we always return false so we will use the selection set up in the tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool IsCurrentSelectionOutOfDate
		{
			get { return false; }
		}
	}
	#endregion

	#region FootnoteEditingHelperTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FootnoteEditingHelperTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FootnoteEditingHelperTests : ScrInMemoryFdoTestBase
	{
		/// <summary></summary>
		protected DummyFootnoteEditingHelper m_editingHelper;
		private SimpleRootSite m_RootSite;

		#region setup and teardown methods

		/// <summary>
		///
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			m_RootSite = new DummySimpleRootSite(Cache);
			m_editingHelper = new DummyFootnoteEditingHelper(Cache, m_RootSite);
		}

		/// <summary>
		///
		/// </summary>
		public override void TestTearDown()
		{
			m_editingHelper.Dispose();
			m_editingHelper = null;
			m_RootSite.Dispose();
			m_RootSite = null;

			base.TestTearDown();
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a book to the mock fdocache and adds it to the book filter.
		/// </summary>
		/// <param name="nBookNumber">the one-based canonical book number, eg 2 for Exodus</param>
		/// <param name="bookName">the English name of the book</param>
		/// <returns>A new ScrBook.</returns>
		/// ------------------------------------------------------------------------------------
		public override IScrBook AddBookToMockedScripture(int nBookNumber, string bookName)
		{
			var book = base.AddBookToMockedScripture(nBookNumber, bookName);
			m_editingHelper.BookFilter.Add(new IScrBook[] { book });
			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book with two footnotes.
		/// </summary>
		/// <param name="philemon">The book of philemon that will have one paragraph with two
		/// footnotes.</param>
		/// <param name="footnote1">first footnote to be inserted at character index 1</param>
		/// <param name="footnote2">second footnote to be inserted at character index 10</param>
		/// ------------------------------------------------------------------------------------
		private void CreateBookWithTwoFootnotes(out IScrBook philemon,
			out IStFootnote footnote1, out IStFootnote footnote2)
		{
			philemon = AddBookToMockedScripture(57, "Philemon");
			IScrSection section = AddSectionToMockedBook(philemon);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "this is more text", null);
			footnote1 = AddFootnote(philemon, para, 1);
			IStTxtPara footnotePara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				footnote1, ScrStyleNames.NormalFootnoteParagraph);
			footnotePara.Contents = Cache.TsStrFactory.MakeString("This is my first footnote",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			footnote2 = AddFootnote(philemon, para, 10);
			footnotePara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				footnote2, ScrStyleNames.NormalFootnoteParagraph);
			footnotePara.Contents = Cache.TsStrFactory.MakeString("This is my second footnote",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
		}
		#endregion

		#region Go to footnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to next footnote from the first footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextFootnote_FirstFootnote()
		{
			IScrBook philemon;
			IStFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			m_editingHelper.SetupSelectionInFootnote(footnote1, philemon, 0, 0, 0);
			IScrFootnote nextfootnote = m_editingHelper.GoToNextFootnote();
			Assert.IsNotNull(nextfootnote, "Couldn't find footnote");
			Assert.AreEqual(philemon.FootnotesOS[1], nextfootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to next footnote from the last footnote of the last book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextFootnote_AtLastFootnote()
		{
			IScrBook philemon;
			IStFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			m_editingHelper.SetupSelectionInFootnote(footnote2, philemon, 0, 0, 0);
			IScrFootnote nextfootnote = m_editingHelper.GoToNextFootnote();
			Assert.IsNull(nextfootnote, "Footnote was found when it should not have been");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to previous footnote from the last footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPreviousFootnote_LastFootnote()
		{
			IScrBook philemon;
			IStFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			// set the selection to the second footnote and go to previous. The footnote
			// should be the first footnote.
			m_editingHelper.SetupSelectionInFootnote(footnote2, philemon, 0, 0, 0);
			IScrFootnote prevfootnote = m_editingHelper.GoToPreviousFootnote();
			Assert.AreEqual(footnote1.Hvo, prevfootnote.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to previous footnote from the first footnote of the first book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPreviousFootnote_AtFirstFootnote()
		{
			IScrBook philemon;
			IStFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			// set the selection to the first footnote and go to previous. There are
			// none so it should return null
			m_editingHelper.SetupSelectionInFootnote(footnote1, philemon, 0, 0, 0);
			IScrFootnote prevfootnote = m_editingHelper.GoToPreviousFootnote();
			Assert.IsNull(prevfootnote);
		}
		#endregion
	}
	#endregion

	#region TeEditingHelper tests with mocked FDO cache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// More unit tests for <see cref="TeEditingHelper">TeEditingHelper</see> that use
	/// <see cref="DummyBasicView">DummyBasicView</see> view to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FootnoteEditingHelperTestsWithMockedFdoCache : BasicViewTestsBase
	{
		#region Dummy TE footnote view and footnote editing helper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class DummyTeFootnoteEditingHelper : FootnoteEditingHelper
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:DummyTeFootnoteEditingHelper"/> class.
			/// </summary>
			/// <param name="callbacks">The callbacks.</param>
			/// <param name="cache">The cache.</param>
			/// <param name="filterInstance">The filter instance.</param>
			/// <param name="draftView">The draft view.</param>
			/// --------------------------------------------------------------------------------
			public DummyTeFootnoteEditingHelper(IEditingCallbacks callbacks,
				FdoCache cache, int filterInstance, FwRootSite draftView)
				: base(callbacks, cache, filterInstance, draftView,
				TeViewType.FootnoteView | TeViewType.Horizontal, null)
			{
			}

			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Overriden so that it works if we don't display the view
			/// </summary>
			/// <returns>Returns <c>true</c> if pasting is possible.</returns>
			/// -----------------------------------------------------------------------------------
			public override bool CanPaste()
			{
				CheckDisposed();

				bool fVisible = Control.Visible;
				Control.Visible = true;
				bool fReturn = base.CanPaste();
				Control.Visible = fVisible;
				return fReturn;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class DummyTeFootnoteView : DummyBasicView, ILocationTracker
		{
			private LocationTrackerImpl m_locationTracker;

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DummyTeFootnoteView(FdoCache cache)
				: base()
			{
				m_fdoCache = cache;
				m_locationTracker = new LocationTrackerImpl(cache, 0);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Helper used for processing editing requests.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected override EditingHelper CreateEditingHelper()
			{
				return new DummyTeFootnoteEditingHelper(this, m_fdoCache, 0, null);
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
			/// Set up a new view constructor.
			/// </summary>
			/// <param name="flid">Not used.</param>
			/// ------------------------------------------------------------------------------------
			protected override VwBaseVc  CreateVc(int flid)
			{
				FootnoteVc vc = new FootnoteVc(TeStVc.LayoutViewTarget.targetDraft, 456);
				vc.Cache = m_fdoCache;
				return vc;
			}

			#region ILocationTracker Members

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the HVO of the current book, or -1 if there is no current book (e.g. no
			/// selection or empty view).
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>The book hvo.</returns>
			/// ------------------------------------------------------------------------------------
			public IScrBook GetBook(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_locationTracker.GetBook(selHelper, selLimitType);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Get the index of the current book (relative to RootBox), or -1 if there is no
			/// current book (e.g. no selection or empty view).
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>
			/// Index of the current book, or -1 if there is no current book.
			/// </returns>
			/// <remarks>The returned value is suitable for making a selection.</remarks>
			/// ------------------------------------------------------------------------------------
			public int GetBookIndex(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_locationTracker.GetBookIndex(selHelper, selLimitType);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the HVO of the current section, or -1 if we're not in a section (e.g. the IP is
			/// in a title).
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>The section hvo.</returns>
			/// ------------------------------------------------------------------------------------
			public IScrSection GetSection(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_locationTracker.GetSection(selHelper, selLimitType);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the index of the section relative to the book, or -1 if we're not in a section.
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>The section index in book.</returns>
			/// ------------------------------------------------------------------------------------
			public int GetSectionIndexInBook(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_locationTracker.GetSectionIndexInBook(selHelper, selLimitType);
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
			public int GetSectionIndexInView(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_locationTracker.GetSectionIndexInView(selHelper, selLimitType);
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
			public void SetBookAndSection(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType, int iBook, int iSection)
			{
				m_locationTracker.SetBookAndSection(selHelper, selLimitType, iBook, iSection);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the number of levels for the given tag.
			/// </summary>
			/// <param name="tag">The tag.</param>
			/// <returns>Number of levels</returns>
			/// ------------------------------------------------------------------------------------
			public int GetLevelCount(int tag)
			{
				return m_locationTracker.GetLevelCount(tag);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the index of the level for the given tag.
			/// </summary>
			/// <param name="tag">The tag.</param>
			/// <returns>Index of the level, or -1 if unknown level.</returns>
			/// ------------------------------------------------------------------------------------
			public int GetLevelIndex(int tag)
			{
				return m_locationTracker.GetLevelIndex(tag);
			}

			#endregion
		}
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			ClipboardUtils.Manager.SetClipboardAdapter(new ClipboardStub());
			m_flidContainingTexts = ScrBookTags.kflidFootnotes;
			m_frag = (int)FootnoteFrags.kfrScripture;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override DummyBasicView CreateDummyBasicView()
		{
			return new DummyTeFootnoteView(Cache);
		}
		#endregion

		#region PasteClipboard tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-5013: Tests that pasting the clipboard contents with multiple paragraphs into a
		/// footnote will not create a multi-paragraph footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteMultiParasIntoFootnote()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			IScrSection section = AddSectionToMockedBook(book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Scripture text", Cache.DefaultVernWs);
			IStFootnote footnote1 = AddFootnote(book, para, 9, "footnote 1");
			int footnote1Length = ((IStTxtPara)footnote1.ParagraphsOS[0]).Contents.Length;
			m_hvoRoot = Cache.LangProject.TranslatedScriptureOA.Hvo;

			ShowForm(DummyBasicViewVc.DisplayType.kNormal);
			IVwRootBox rootBox = m_basicView.RootBox;

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "New heading line1", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			strBldr.Replace(strBldr.Length, strBldr.Length, Environment.NewLine,
				StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph));
			strBldr.Replace(strBldr.Length, strBldr.Length, "heading line2" + Environment.NewLine + "New heading line3",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));

			EditingHelper.SetTsStringOnClipboard(strBldr.GetString(), false, Cache.WritingSystemFactory);

			// Make selection at the end of the footnote
			Assert.IsNotNull(rootBox.MakeSimpleSel(false, true, false, true));

			// Paste contents of clipboard at current selection.
			m_basicView.EditingHelper.PasteClipboard();

			// We expect that the footnote will only have one paragraph with the extra paragraphs
			// concatenated, separated with spaces.
			Assert.AreEqual(1, footnote1.ParagraphsOS.Count);
			Assert.AreEqual("footnote 1New heading line1 heading line2 New heading line3",
				((IStTxtPara)footnote1.ParagraphsOS[0]).Contents.Text);
		}
		#endregion
	}
	#endregion
}
