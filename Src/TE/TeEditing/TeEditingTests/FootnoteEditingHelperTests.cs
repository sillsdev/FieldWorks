// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FootnoteEditingHelperTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NMock;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;

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
			base(rootsite, cache, 0, null, false)
		{
			InTestMode = true;
		}

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

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
		public void SetupSelectionInFootnote(StFootnote footnote, IScrBook book,
			int iBook, int ichStart, int ichEnd)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 3);
			// Setup the anchor
			SelLevInfo[] topInfo = new SelLevInfo[3];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];
			topInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			topInfo[0].ihvo = 0;	// only one para per footnote allowed
			topInfo[0].hvo = para.Hvo;

			topInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
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
			m_viewSelection = (SelectionHelper)fakeSelHelper.MockInstance;
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_RootSite = new DummySimpleRootSite(Cache);
			m_editingHelper = new DummyFootnoteEditingHelper(Cache, m_RootSite);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_editingHelper.Dispose();
			m_editingHelper = null;
			m_RootSite.Dispose();
			m_RootSite = null;

			base.Exit();
		}
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
			Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_editingHelper != null)
					m_editingHelper.Dispose();
				if (m_RootSite != null)
					m_RootSite.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_editingHelper = null;
			m_RootSite = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Helper methods

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
			out StFootnote footnote1, out StFootnote footnote2)
		{
			philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			footnote1 = m_scrInMemoryCache.AddFootnote(philemon, para, 1);
			StTxtPara footnotePara = new StTxtPara();
			footnote1.ParagraphsOS.Append(footnotePara);
			footnotePara.Contents.Text = "This is my first footnote";
			footnote2 = m_scrInMemoryCache.AddFootnote(philemon, para, 10);
			footnotePara = new StTxtPara();
			footnote2.ParagraphsOS.Append(footnotePara);
			footnotePara.Contents.Text = "This is my second footnote";
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
			CheckDisposed();

			IScrBook philemon;
			StFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			m_editingHelper.SetupSelectionInFootnote(footnote1, philemon, 0, 0, 0);
			ScrFootnote nextfootnote = m_editingHelper.GoToNextFootnote();
			Assert.IsNotNull(nextfootnote, "Couldn't find footnote");
			Assert.AreEqual(philemon.FootnotesOS.HvoArray[1], nextfootnote.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to next footnote from the last footnote of the last book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextFootnote_AtLastFootnote()
		{
			CheckDisposed();

			IScrBook philemon;
			StFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			m_editingHelper.SetupSelectionInFootnote(footnote2, philemon, 0, 0, 0);
			ScrFootnote nextfootnote = m_editingHelper.GoToNextFootnote();
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
			CheckDisposed();

			IScrBook philemon;
			StFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			// set the selection to the second footnote and go to previous. The footnote
			// should be the first footnote.
			m_editingHelper.SetupSelectionInFootnote(footnote2, philemon, 0, 0, 0);
			ScrFootnote prevfootnote = m_editingHelper.GoToPreviousFootnote();
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
			CheckDisposed();

			IScrBook philemon;
			StFootnote footnote1, footnote2;
			CreateBookWithTwoFootnotes(out philemon, out footnote1, out footnote2);

			// set the selection to the first footnote and go to previous. There are
			// none so it should return null
			m_editingHelper.SetupSelectionInFootnote(footnote1, philemon, 0, 0, 0);
			ScrFootnote prevfootnote = m_editingHelper.GoToPreviousFootnote();
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
				: base(callbacks, cache, filterInstance, draftView, false)
			{
				InTestMode = true;
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
				// Add virtual property to cache
				if (FilteredScrBooks.GetFilterInstance(cache, 0) == null)
					new FilteredScrBooks(cache, 0);

				m_locationTracker = new LocationTrackerImpl(cache, 0);
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
						m_editingHelper = new DummyTeFootnoteEditingHelper(this, m_fdoCache,
							0, null);
					return m_editingHelper;
				}
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
			public int GetBookHvo(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_locationTracker.GetBookHvo(selHelper, selLimitType);
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
			public int GetSectionHvo(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_locationTracker.GetSectionHvo(selHelper, selLimitType);
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

		#region Data members
		private int m_hvoSection;
		/// <summary>Pointer to the data object stored on the clipboard</summary>
		private static ILgTsDataObject m_dobjClipboard;
		#endregion

		#region Test setup
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a book and section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_flidContainingTexts = (int)ScrBook.ScrBookTags.kflidSections;
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Philemon");
			m_hvoRoot = book.Hvo;

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_hvoSection = section.Hvo;
			section.AdjustReferences();

			m_frag = 7;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_dobjClipboard = null;

			base.Exit();
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Scripture text", Cache.DefaultVernWs);
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(book, para, 9, "footnote 1");
			int footnote1Length = ((StTxtPara)footnote1.ParagraphsOS[0]).Contents.Length;
			m_hvoRoot = book.Hvo;

			ShowForm(DummyBasicViewVc.DisplayType.kNormal);
			IVwRootBox rootBox = m_basicView.RootBox;

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "New heading line1", StyleUtils.CharStyleTextProps(null, 1));
			strBldr.Replace(strBldr.Length, strBldr.Length, "\r\n",
				StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph));
			strBldr.Replace(strBldr.Length, strBldr.Length, "heading line2\r\nNew heading line3",
				StyleUtils.CharStyleTextProps(null, 1));

			ILgWritingSystemFactory wsf = rootBox.DataAccess.WritingSystemFactory;

			ILgTsStringPlusWss tssencs = LgTsStringPlusWssClass.Create();
			tssencs.set_String(wsf, strBldr.GetString());
			m_dobjClipboard = LgTsDataObjectClass.Create();
			m_dobjClipboard.Init(tssencs);
			Clipboard.SetDataObject(m_dobjClipboard, false);

			// Make selection at the end of the footnote
			rootBox.MakeSimpleSel(false, true, false, true);

			// Paste contents of clipboard at current selection.
			m_basicView.EditingHelper.PasteClipboard(true);

			// We expect that the footnote will only have one paragraph with the extra paragraphs
			// in the paragraph ignored.
			Assert.AreEqual(1, footnote1.ParagraphsOS.Count);
			Assert.AreEqual("footnote 1New heading line1", ((StTxtPara)footnote1.ParagraphsOS[0]).Contents.Text);
		}
		#endregion
	}
	#endregion
}
