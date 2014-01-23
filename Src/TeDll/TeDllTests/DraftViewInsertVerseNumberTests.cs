// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DraftViewInsertVerseNumberTests.cs
// Responsibility: TE Team

using System;
using NMock;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE.DraftViews
{
	#region InsertVerseNumberTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for inserting verse numbers in DraftView. These tests use mock objects and
	/// so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class InsertVerseNumberTests : TeTestBase
	{
		#region Member variables
		IScrBook m_book;
		private DummyDraftView m_draftView;
		private DynamicMock m_styleSheet;
		private DynamicMock m_rootBox;
		private DynamicMock m_vwGraphics;
		private DynamicMock m_selHelper;
		private object[] m_TextSelInfoArgsAnchor;
		private object[] m_TextSelInfoArgsEnd;
		private string[] m_TextSelInfoTypes;
		private const int kflidParaContent = StTxtParaTags.kflidContents;
		private const int kflidTrans = CmTranslationTags.kflidTranslation;

		private string m_StartText = '\u0634' + '\u0677' + '\u0631' + '\u0677' + string.Empty;
		private string m_WordsText = '\u0622' + '\u0644' + '\u0641' + '\u0627' + '\u0632' + string.Empty;
		private IWritingSystem m_wsUrdu;
		#endregion

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suite setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ur", out m_wsUrdu);
			m_wsUrdu.RightToLeftScript = true;

			string intType = "System.Int32";
			string intRef = intType + "&";
			string boolType = typeof(bool).FullName;
			string boolRef = boolType + "&";

			m_TextSelInfoArgsAnchor = new object[] { false, null, null, null, null, null, null };
			m_TextSelInfoArgsEnd = new object[] { true, null, null, null, null, null, null };
			m_TextSelInfoTypes =
				new[] { boolType, "SIL.FieldWorks.Common.COMInterfaces.ITsString&", intRef,
								 boolRef, intRef, intRef, intRef};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			SelectionHelper.s_mockedSelectionHelper = null;
			base.FixtureTeardown();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_styleSheet = new DynamicMock(typeof(IVwStylesheet));
			m_styleSheet.Strict = true;

			// Set up a default selection helper. Tests may choose to create a different one.
			m_selHelper = new DynamicMock(typeof(SelectionHelper));
			m_selHelper.Strict = true;
			m_selHelper.SetupResult("NumberOfLevels", 4);
			m_selHelper.SetupResult("RestoreSelectionAndScrollPos", true);
			BTInsertVerseAndFootnoteTests.InitializeVwSelection(m_selHelper);
			SelectionHelper.s_mockedSelectionHelper = (SelectionHelper)m_selHelper.MockInstance;

			m_draftView = new DummyDraftView(Cache, false, 0);
			m_draftView.BookFilter.Add(m_book);

			m_draftView.RootBox = SetupRootBox();
			m_draftView.Graphics = SetupGraphics();
			m_draftView.MakeRoot();
			m_draftView.StyleSheet = (IVwStylesheet)m_styleSheet.MockInstance;
			m_draftView.ActivateView();

			m_rootBox.Strict = true;

			SelLevInfo[] selLevInfo = new SelLevInfo[4];
			selLevInfo[3].tag = m_draftView.BookFilter.Tag;
			selLevInfo[2].tag = ScrBookTags.kflidSections;
			selLevInfo[2].ihvo = 0;
			selLevInfo[0].ihvo = 0;
			selLevInfo[0].tag = StTextTags.kflidParagraphs;
			m_selHelper.SetupResult("LevelInfo", selLevInfo);
			m_selHelper.SetupResult("GetLevelInfo", selLevInfo,
				new Type[] { typeof(SelectionHelper.SelLimitType) });
			m_selHelper.Expect("SetIPAfterUOW");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			var disposable = m_draftView.Graphics as IDisposable;
			if (disposable != null)
				disposable.Dispose();
			m_draftView.Dispose();
			m_draftView = null;
			m_styleSheet = null;
			m_rootBox = null;
			m_vwGraphics = null;
			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_book = AddBookToMockedScripture(2, "Exodus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the mock rootbox
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IVwRootBox SetupRootBox()
		{
			m_rootBox = new DynamicMock(typeof(IVwRootBox));
			m_rootBox.SetupResult("SetSite", null, typeof(IVwRootSite));
			m_rootBox.SetupResult("DataAccess", Cache.DomainDataByFlid);
			m_rootBox.SetupResult("SetRootObject", null, typeof(int), typeof(IVwViewConstructor),
				typeof(int), typeof(IVwStylesheet));
			m_rootBox.SetupResult("Height", 200); // JT: arbitrary values.
			m_rootBox.SetupResult("Width", 200);
			m_rootBox.SetupResult("Site", m_draftView);
			m_rootBox.Ignore("Close");
			m_rootBox.Ignore("FlashInsertionPoint");
			m_rootBox.SetupResult("GetRootObject", null,
				new string[] {typeof(int).FullName + "&", typeof(IVwViewConstructor).FullName + "&", typeof(int).FullName + "&", typeof(IVwStylesheet).FullName + "&"},
				new object[] {0, null, 0, null});

			return (IVwRootBox)m_rootBox.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the mock graphics object
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IVwGraphics SetupGraphics()
		{
			//			m_vwGraphics = new DynamicMock(typeof(IVwGraphics));
			// JT: following three statements replace the one above, following the exmple
			// Eberhard set in InsertChapterNumberTests.
			m_vwGraphics = new DynamicMock(typeof(IVwGraphics), "MockIVwGraphics",
				typeof(DummyGraphics));
			m_vwGraphics.AdditionalReferences = new string[] { "TeDllTests.dll" };
			m_vwGraphics.Strict = true;
			m_vwGraphics.SetupResult("XUnitsPerInch", 96);
			m_vwGraphics.SetupResult("YUnitsPerInch", 96);

			return (IVwGraphics)m_vwGraphics.MockInstance;
		}
		#endregion

		#region Setup helper functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for selection and action handler
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="section">The section.</param>
		/// <param name="ich">The character offset of the IP in the para.</param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupInsertVerseSelection(IStTxtPara para, IScrSection section, int ich)
		{
			int hvoPara = para.Hvo;

#if DEBUG
			m_selHelper.SetupResult("IsRange", false);
#endif
			m_selHelper.SetupResult("IchAnchor", ich);
			m_selHelper.SetupResult("IchEnd", ich);
			m_selHelper.SetupResultForParams("GetIch", ich, new object[] { SelectionHelper.SelLimitType.Anchor });
			m_selHelper.SetupResultForParams("GetIch", ich, new object[] { SelectionHelper.SelLimitType.End });
			m_selHelper.SetupResultForParams("GetIch", ich, new object[] { SelectionHelper.SelLimitType.Top });
			m_selHelper.SetupResultForParams("GetIch", ich, new object[] { SelectionHelper.SelLimitType.Bottom });

			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			m_rootBox.SetupResult("Selection", sel.MockInstance);
			sel.Strict = true;
			sel.SetupResult("IsValid", true);

			m_selHelper.ExpectAndReturn("ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, false);
			m_selHelper.SetupResultForParams("GetTextPropId",
				StTextTags.kflidParagraphs, SelectionHelper.SelLimitType.Anchor);
			m_selHelper.ExpectAndReturn("GetTextPropId", StTxtParaTags.kflidContents,
				new object[] { SelectionHelper.SelLimitType.Top });

			ITsPropsBldr builder = TsPropsBldrClass.Create();
			ITsTextProps selProps = builder.GetTextProps();
			m_selHelper.ExpectAndReturn("SelProps", selProps);
			m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, false);
			m_selHelper.Expect("AssocPrev", true);
			m_selHelper.Expect("SetIPAfterUOW");
			m_selHelper.ExpectAndReturn("GetTextPropId", StTxtParaTags.kflidContents,
				new object[] { SelectionHelper.SelLimitType.Top });

			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsEnd, m_TextSelInfoTypes,
				new object[] { true, para.Contents, ich,
							 false, para.Hvo, kflidParaContent, 0 });
			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents, ich,
							 false, para.Hvo, kflidParaContent, 0 });
			m_selHelper.ExpectAndReturn("GetTextPropId", StTxtParaTags.kflidContents,
				new object[] { SelectionHelper.SelLimitType.Top });
			m_selHelper.SetupResultForParams("GetTss", para.Contents,
				new object[] { SelectionHelper.SelLimitType.Anchor });

			SetupSelection(sel, para, section);

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for selection and action handler when inserting verse isn't
		/// possible
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="section">The section.</param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupNoInsertVerseSelection(IStTxtPara para, IScrSection section)
		{
			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			m_rootBox.SetupResult("Selection", sel.MockInstance);
			sel.Strict = true;
			sel.SetupResult("IsValid", true);

			m_selHelper.ExpectAndReturn("ReduceSelectionToIp", m_selHelper.MockInstance,
				SelectionHelper.SelLimitType.Top, false, false);
			m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, false);
			m_selHelper.ExpectAndReturn(3, "GetTextPropId", StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });

			SetupSelection(sel, para, section);

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up expected calls to action handler
		/// </summary>
		/// <param name="sel">The selection mock</param>
		/// <param name="para">The para.</param>
		/// <param name="section">The section.</param>
		/// ------------------------------------------------------------------------------------
		private void SetupSelection(DynamicMock sel, IStTxtPara para, IScrSection section)
		{
			m_selHelper.SetupResult("Selection", sel.MockInstance);
			SelLevInfo[] info = new SelLevInfo[4];
			info[0] = new SelLevInfo();
			info[0].tag = StTextTags.kflidParagraphs;
			info[0].hvo = para.Hvo;
			info[0].ihvo = para.IndexInOwner;
			info[1] = new SelLevInfo();
			info[1].tag = ScrSectionTags.kflidContent;
			info[1].hvo = para.Owner.Hvo; // hvo of text. Who cares?
			info[2] = new SelLevInfo();
			info[2].tag = ScrBookTags.kflidSections;
			info[2].hvo = section.Hvo;
			info[2].ihvo = section.IndexInOwner;
			info[3] = new SelLevInfo();
			info[3].tag = m_draftView.BookFilter.Tag;
			info[3].hvo = m_book.Hvo;
			m_selHelper.SetupResult("LevelInfo", info);
			m_selHelper.SetupResult("GetLevelInfo", info,
				new Type[] { typeof(SelectionHelper.SelLimitType)});
			m_selHelper.ExpectAndReturn("GetTextPropId", StTxtParaTags.kflidContents,
				new object[] { SelectionHelper.SelLimitType.Top });
		}
		#endregion

		#region General helper functions

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function appends a run to the given string builder
		/// </summary>
		/// <param name="strBldr">The String builder.</param>
		/// <param name="text">The text.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="charStyle">The char style.</param>
		/// ------------------------------------------------------------------------------------
		private void AddRunToStrBldr(ITsStrBldr strBldr, string text, int ws, string charStyle)
		{
			strBldr.ReplaceRgch(strBldr.Length, strBldr.Length, text, text.Length,
				StyleUtils.CharStyleTextProps(charStyle, ws));
		}
		#endregion

		#region Basic Insert Verse Tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number. Try to insert verse number in title.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InTitle()
		{
			ITsPropsBldr builder = TsPropsBldrClass.Create();
			m_selHelper.ExpectAndReturn("SelProps", builder.GetTextProps());

			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			m_rootBox.SetupResult("Selection", sel.MockInstance);
			sel.SetupResult("IsValid", true);
			sel.Strict = true;

			m_selHelper.SetupResult("Selection", sel.MockInstance);
			m_selHelper.ExpectAndReturn("ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, false);
			m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, false);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// TE-2740 Tests inserting a verse number in an introduction. There should be a no-op.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InIntroduction()
		{
			m_selHelper.SetupResult("IchAnchor", 0);

			IScrSection introSection = AddSectionToMockedBook(m_book, true);

			IScrSection scriptureSection = AddSectionToMockedBook(m_book);
			IStTxtPara introPara = SetupParagraph(introSection, null, null);
			DynamicMock sel = SetupNoInsertVerseSelection(introPara, introSection);

			// InsertVerse should not allow a verse number to be inserted.
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// InsertVerse should not allow a verse number to be inserted.
			ITsString tssResult = introPara.Contents;

			// Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr,"This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				Cache.DefaultVernWs, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at a valid position (middle of paragraph).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MiddleOfParagraph()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, null, null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 5);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("This 2is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(3, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number in the first word of a paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InFirstWordOfParagraph()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, null, null);

			// set up a mocked selection with the IP within the first word
			DynamicMock sel = SetupInsertVerseSelection(para, section, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// we expect verse one is inserted at the beginning of the para
			ITsString tss = para.Contents;
			Assert.AreEqual("1This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(2, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number in the first word of a paragraph in the second
		/// section.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InFirstWordOfParagraphSecondSection()
		{
			IScrSection section1 = AddSectionToMockedBook(m_book);
			SetupParagraph(section1, "1", "1");

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section2, null, null);

			// set up the selection within first word in the second section
			DynamicMock sel = SetupInsertVerseSelection(para, section2, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// we expect that verse 2 is inserted at the beginning of the second section
			ITsString tss = para.Contents;
			Assert.AreEqual("2This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(2, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at a valid position (start of section).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void StartOfSection()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = SetupParagraph(section, null, null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("1This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(2, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at a valid position (right after a chapter number).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void AfterChapterNumber()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 1
			IStTxtPara para = SetupParagraph(section, "4", null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("41This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(3, tss.RunCount);

			// check the first run to see if it is a chapter number
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// test the second run to see if it is a verse number
			ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at a valid position (end of paragraph).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EndOfParagraph()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = SetupParagraph(section, "1", "1");

			DynamicMock sel = SetupInsertVerseSelection(para, section, kParagraphText.Length + 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("11This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.2",
				tss.Text);
			Assert.AreEqual(4, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at the end of a paragraph before the period.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EndOfParagraphBeforePeriod()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = SetupParagraph(section, "1", "1");

			DynamicMock sel = SetupInsertVerseSelection(para, section, kParagraphText.Length + 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("11This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.2",
				tss.Text);
			Assert.AreEqual(4, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at a valid position (End of para with no explicit
		/// verse number 1).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void Verse2ImplicitVerse1()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = SetupParagraph(section, "1", null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, kParagraphText.Length + 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("1This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.2", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(2);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at a valid position (in the second para of a
		/// section).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InSectionNotFirstPara()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = SetupParagraph(section, "1", "1");

			IStTxtPara para2 = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para2, "Another paragraph.", null);

			DynamicMock sel = SetupInsertVerseSelection(para2, section, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para2.Contents;
			Assert.AreEqual("2Another paragraph.", tss.Text);
			Assert.AreEqual(2, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number at a valid position (start of section, para, book).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EmptyBook()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");

			DynamicMock sel = SetupInsertVerseSelection(para, section, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("1", tss.Text);
			Assert.AreEqual(1, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number after a verse bridge.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SomewhereAfterVerseBridge()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = SetupParagraph(section, "1", "1-5");

			// mock up a selection at end of the para
			int len = para.Contents.Text.Length;
			DynamicMock sel = SetupInsertVerseSelection(para, section, len);

			m_draftView.InsertVerseNumber();

			// we expect that verse number 6 is inserted at the IP
			ITsString tss = para.Contents;
			Assert.AreEqual("11-5This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.6",
				tss.Text);
			Assert.AreEqual(4, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number after the last legal verse of the chapter.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void AfterLastVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Exodus 1 has 22 verses. Mark our text as that last legal verse.
			IStTxtPara para = SetupParagraph(section, "1", "22");

			// mock up a selection at end of the para
			int len = para.Contents.Text.Length;
			DynamicMock sel = SetupInsertVerseSelection(para, section, len);

			m_draftView.InsertVerseNumber();

			// we expect that verse number 23 is NOT inserted
			ITsString tss = para.Contents;
			Assert.AreEqual("122This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(3, tss.RunCount);

			sel.Verify();
			m_rootBox.Verify();
		}
		#endregion

		#region On Plain Number tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking at the leading edge of an existing plain number (default paragraph
		/// characters) to replace it with a real verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ReplacePlainNumberLeading()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IScrTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "1" + kParagraphText, null);

			// mock up a selection in a para at ich 1
			DynamicMock sel = SetupInsertVerseSelection(para, section, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("11This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("1", tss.get_RunText(0));
			ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("1", tss.get_RunText(1));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking at the trailing edge of an existing plain number (default paragraph
		/// characters) to replace it with a real verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ReplacePlainNumberTrailing()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IScrTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "1" + kParagraphText, null);

			// mock up a selection in a para at ich 2
			DynamicMock sel = SetupInsertVerseSelection(para, section, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("11This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("1", tss.get_RunText(0));
			ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("1", tss.get_RunText(1));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking at the leading edge of an existing plain number (default paragraph
		/// characters) to replace it with a real verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OnPlainNumber_Incorrect()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IScrTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "6" + kParagraphText, null);

			// mock up a selection in a para at ich 1
			DynamicMock sel = SetupInsertVerseSelection(para, section, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("116This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("1", tss.get_RunText(0));
			ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("1", tss.get_RunText(1));
		}
		#endregion

		#region On Verse Number tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking at the leading edge of an existing verse number to create a verse
		/// bridge.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OnVerseNumberLeading()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = SetupParagraph(section, "1", "6");

			// mock up a selection in a para at ich 1
			DynamicMock sel = SetupInsertVerseSelection(para, section, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("16-7This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking at the trailing edge of an existing verse number to create a verse
		/// bridge.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OnVerseNumberTrailing()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = SetupParagraph(section, "1", "6");

			// mock up a selection in a para at ich 2
			DynamicMock sel = SetupInsertVerseSelection(para, section, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("16-7This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region On Chapter Number tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking after the last verse number in a chapter and before the next chapter
		/// number, where the first verse in the following chapter is the same as the newly
		/// inserted verse number. Nothing after the chapter number should be affected.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void BeforeChapterNumber()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Create a paragraph with two chapters and verse numbers
			IStTxtPara para = SetupParagraph(section, "1", "6");
			ITsStrBldr strBldr = para.Contents.GetBldr();
			int bldrLength = strBldr.Length;
			strBldr.Replace(bldrLength, bldrLength, "2",
				StyleUtils.CharStyleTextProps("Chapter Number", Cache.DefaultVernWs));
			bldrLength++;
			strBldr.Replace(bldrLength, bldrLength, "7", StyleUtils.CharStyleTextProps("Verse Number",
				Cache.DefaultVernWs));
			para.Contents = strBldr.GetString();

			// mock up a selection in a para at ich 7
			DynamicMock sel = SetupInsertVerseSelection(para, section, 7);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("16This 7is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.27", tss.Text);
			Assert.AreEqual(7, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// TE-3350: Tests clicking at the beginning of a chapter number when a chapter 1,
		/// verse 1 occurs later in the paragraph. A chapter 1, verse 1 should be inserted and
		/// the following chapter 1, verse 1 should be deleted.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void BeforeDuplicateChapterNumber()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Create a paragraph with chapter 1, verse 1 at end of paragraph
			IStTxtPara para = SetupParagraph(section, null, null);
			ITsStrBldr strBldr = para.Contents.GetBldr();
			int bldrLength = strBldr.Length;
			strBldr.Replace(bldrLength, bldrLength, "1",
				StyleUtils.CharStyleTextProps("Chapter Number", Cache.DefaultVernWs));
			bldrLength++;
			strBldr.Replace(bldrLength, bldrLength, "1", StyleUtils.CharStyleTextProps("Verse Number",
				Cache.DefaultVernWs));
			para.Contents = strBldr.GetString();

			// mock up a selection in a para at ich 0
			DynamicMock sel = SetupInsertVerseSelection(para, section, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// We expect that verse 1 be inserted at the IP and chapter 1, verse 1 removed from
			// the end of the paragraph.
			ITsString tss = para.Contents;
			Assert.AreEqual("1This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(2, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking immediately before a chapter number in a paragraph, where the
		/// chapter number is the first thing in the paragraph, followed by verse text.
		/// Should insert verse number 1 immediately FOLLOWING the chapter number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ImmediatelyBeforeChapterNumber_StartofPara_NoPreExistingVerseNum()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Create a paragraph with two chapters and verse numbers
			IStTxtPara para = SetupParagraph(section, "1", null);

			// mock up a selection in a para at ich 0
			DynamicMock sel = SetupInsertVerseSelection(para, section, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("11This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking immediately before a chapter number in a paragraph, where the
		/// chapter number is the first thing in the paragraph, followed by verse number 1.
		/// Should turn existing verse number into a verse bridge.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ImmediatelyBeforeChapterNumber_StartofPara_PreExistingVerseNum()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Create a paragraph with two chapters and verse numbers
			IStTxtPara para = SetupParagraph(section, "1", "1");

			// mock up a selection in a para at ich 0
			DynamicMock sel = SetupInsertVerseSelection(para, section, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("11-2This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking immediately before a chapter number in a paragraph, where the
		/// chapter number is in the middle of the paragraph. Should insert a verse number
		/// BEFORE the chapter number, based on preceding verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ImmediatelyBeforeChapterNumber_MiddleOfPara()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Create a paragraph with two chapters and verse numbers
			IStTxtPara para = SetupParagraph(section, "1", "3");
			ITsStrBldr strBldr = para.Contents.GetBldr();
			strBldr.Replace(29, 29, "2",
				StyleUtils.CharStyleTextProps("Chapter Number", Cache.DefaultVernWs));
			para.Contents = strBldr.GetString();

			// mock up a selection in a para at ich 29
			DynamicMock sel = SetupInsertVerseSelection(para, section, 29);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("13This is a test paragraph.  42It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(6, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number in a paragraph before a chapter number 0. It
		/// shouldn't crash and nothing after the chapter number should be affected.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void BeforeChapterNumberZero()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Create a paragraph with two chapters and verse numbers
			IStTxtPara para = SetupParagraph(section, "1", "6");
			ITsStrBldr strBldr = para.Contents.GetBldr();
			strBldr.Append("0", StyleUtils.CharStyleTextProps("Chapter Number", Cache.DefaultVernWs));
			strBldr.Append("7", StyleUtils.CharStyleTextProps("Verse Number", Cache.DefaultVernWs));
			para.Contents = strBldr.GetString();

			// mock up a selection in para at ich 7
			DynamicMock sel = SetupInsertVerseSelection(para, section, 7);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("16This 7is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.07", tss.Text);
			Assert.AreEqual(7, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region Space Before Next Verse tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// TE-4415: Test inserting a verse number before a space character that preceeds an
		/// existing verse number, when another space is just to the left of the IP and a
		/// verse number is missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void	SpaceBeforeNextVerse_BetweenSpaces_MissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "  ", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);

			// Set up selection between the spaces between verse numbers 2 and 4
			DynamicMock sel = SetupInsertVerseSelection(para, section, 5);

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be inserted at the IP.
			ITsString tss = para.Contents;
			Assert.AreEqual("11 2 3 4", tss.Text);
			Assert.AreEqual(8, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(5);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempting to insert a verse number before a space character that preceeds an
		/// existing verse number, when another space is just to the left of the IP and a
		/// verse number is NOT missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void	SpaceBeforeNextVerse_BetweenSpaces_NoMissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "  ", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);

			// Set up selection between the spaces between verse numbers 2 and 4
			DynamicMock sel = SetupInsertVerseSelection(para, section, 5);
			sel.ExpectAndReturn("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents, 6,
								 false, para.Hvo, kflidParaContent, 0 });

			m_draftView.InsertVerseNumber();

			// We expect verse 3 to be updated to 3-4
			ITsString tss = para.Contents;
			Assert.AreEqual("11 2  3-4", tss.Text);
			Assert.AreEqual(6, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(5);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting a verse number before a space character that preceeds an
		/// existing verse number, when another verse number is just to the left of the IP and a
		/// verse number is missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SpaceBeforeNextVerse_IpNextToPrevVerse_MissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " ", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);

			// Set up selection immediately after verse 2
			DynamicMock sel = SetupInsertVerseSelection(para, section, 4);

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be inserted, which is acceptable.
			// We could instead expect a verse bridge to be created immediately after verse 2,
			//  but that would seem to be a very minor expectation from the user, and not worth
			//  adding complexity to the InsertVerseNumber code.
			ITsString tss = para.Contents;
			Assert.AreEqual("11 2 3 4", tss.Text);
			Assert.AreEqual(8, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(4);
			Assert.AreEqual(null,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			ttp = tss.get_Properties(5);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting a verse number before a space character that preceeds an
		/// existing verse number, when a chapter number is just to the left of the IP and
		/// verse number one is missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SpaceBeforeNextVerse_IpNextToChapter_MissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, " ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " ", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);

			// Set up selection immediately after the chapter number 1.
			DynamicMock sel = SetupInsertVerseSelection(para, section, 1);

			m_draftView.InsertVerseNumber();

			// We expect verse number 1 to be inserted after the chapter number,
			//  WITHOUT a leading space.
			ITsString tss = para.Contents;
			Assert.AreEqual("11 2 3", tss.Text);
			Assert.AreEqual(6, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting a verse number before a space character that preceeds an
		/// existing verse number, when text is just to the left of the IP and a
		/// verse number is missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SpaceBeforeNextVerse_IpNextToText_MissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "one ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "two ", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);

			// Set up selection just after "two"
			DynamicMock sel = SetupInsertVerseSelection(para, section, 10);

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be inserted at the IP with a leading space.
			ITsString tss = para.Contents;
			Assert.AreEqual("11one 2two 3 4", tss.Text);
			Assert.AreEqual(8, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(5);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempting to insert a verse number before a space character that preceeds an
		/// existing verse number, when text is just to the left of the IP and a
		/// verse number is NOT missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SpaceBeforeNextVerse_IpNextToText_NoMissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "one ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "two ", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);

			// Set up selection just after "two"
			DynamicMock sel = SetupInsertVerseSelection(para, section, 10);
			sel.ExpectAndReturn("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents, 11,
								 false, para.Hvo, kflidParaContent, 0 });

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be updated to 3-4
			ITsString tss = para.Contents;
			Assert.AreEqual("11one 2two 3-4", tss.Text);
			Assert.AreEqual(6, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(5);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting a verse number before a space character that preceeds an
		/// existing verse number, when at the beginning of a paragraph and a
		/// verse number is missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SpaceBeforeNextVerse_BegOfPara_MissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			//Build paragraph one
			IStTxtPara para1 = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "one", null);
			//Build paragraph two
			IStTxtPara para2 = AddParaToMockedSectionContent(section,
			ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, " ", null);
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "three ", null);

			// Set up selection at beginning of paragraph two
			DynamicMock sel = SetupInsertVerseSelection(para2, section, 0);

			m_draftView.InsertVerseNumber();

			// We expect verse number 2 to be inserted at the beginning of para2,
			//  WITHOUT a leading space.
			ITsString tss = para2.Contents;
			Assert.AreEqual("2 3three ", tss.Text);
			Assert.AreEqual(4, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting a verse number before a space character that preceeds an
		/// existing verse number, when at the beginning of the first scripture section and a
		/// verse number is missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SpaceBeforeNextVerse_BegOfSection_MissingVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, " ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "two ", null);

			// Set up selection at beginning of paragraph
			DynamicMock sel = SetupInsertVerseSelection(para, section, 0);

			m_draftView.InsertVerseNumber();

			// We expect verse number 1 to be inserted at the beginning of the para,
			//  WITHOUT a leading space.
			ITsString tss = para.Contents;
			Assert.AreEqual("1 2two ", tss.Text);
			Assert.AreEqual(4, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// TE-2749 Tests clicking on a space before an existing verse number. Previous bug was
		/// to create a verse bridge with identical verse numbers in the bridge. We want to
		/// update the verse with a valid bridge.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OnSpaceBeforeVerse()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = SetupParagraph(section, "1", "1");

			// add verse number 2 at ich 28
			ITsStrBldr strBldr = para.Contents.GetBldr();
			strBldr.Replace(28, 29, "2", StyleUtils.CharStyleTextProps(
				ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			para.Contents = strBldr.GetString();

			// mock up a selection at ich 27
			DynamicMock sel = SetupInsertVerseSelection(para, section, 27);

			sel.ExpectAndReturn("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents, 28,
								 false, para.Hvo, kflidParaContent, 0 });

			m_draftView.InsertVerseNumber();

			// We expect verse number 2 to be updated to 2-3
			ITsString tss = para.Contents;
			Assert.AreEqual("11This is a test paragraph. 2-3It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);

			sel.Verify();
			m_rootBox.Verify();
		}
		#endregion

		#region On Verse Bridge tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking an existing verse bridge to update the verse bridge.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OnVerseBridge()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = SetupParagraph(section, "1", "6-7");

			DynamicMock sel = SetupInsertVerseSelection(para, section, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("16-8This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(3, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking between a verse number and a small verse bridge (where the numbers
		/// are different by one as in 16-17).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void BeforeSmallVerseBridge()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 0
			IStTxtPara para = SetupParagraph(section, "1", "6");

			DynamicMock sel = SetupInsertVerseSelection(para, section, 29);

			// Use the Insert Verse command to set up the test scenario with a verse number
			// and a verse bridge.
			m_draftView.InsertVerseNumber();
			ITsString tss = para.Contents;
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			sel = SetupInsertVerseSelection(para, section, 29);
			m_draftView.InsertVerseNumber();
			tss = para.Contents;
			Assert.AreEqual("16This is a test paragraph.  7-8It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			// Now test reducing the verse bridge to a single verse.
			sel = SetupInsertVerseSelection(para, section, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			tss = para.Contents;
			Assert.AreEqual("16This 7is a test paragraph.  8It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(7, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking between a verse number and a large verse bridge (where the numbers
		/// are different by more than one as in 16-20).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void BeforeLargeVerseBridge()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// mock up a selection in a para at ich 29
			IStTxtPara para = SetupParagraph(section, "1", "6");
			DynamicMock sel = SetupInsertVerseSelection(para, section, 29);

			// Use the Insert Verse command to set up the test scenario with a verse number
			m_draftView.InsertVerseNumber();    // Insert verse 7
			sel.Verify();

			sel = SetupInsertVerseSelection(para, section, 29);
			m_draftView.InsertVerseNumber();    // Update verse 7 to 7-8
			ITsString tss = para.Contents;
			Assert.AreEqual("16This is a test paragraph.  7-8It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			sel.Verify();

			sel = SetupInsertVerseSelection(para, section, 29);
			m_draftView.InsertVerseNumber();    // Update verses 7-8 to 7-9
			tss = para.Contents;
			Assert.AreEqual("16This is a test paragraph.  7-9It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			// Now test reducing the verse bridge to a single verse.
			sel = SetupInsertVerseSelection(para, section, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			tss = para.Contents;
			Assert.AreEqual("16This 7is a test paragraph.  8-9It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(7, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region Move To Word Boundary tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting in the middle of a word to see if it moves to a word boundary.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MiddleOfWord()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, null, null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 6);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("This 2is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(3, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting in the middle of a word that is preceded by punctuation.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MiddleOfWordWithLeadingPunct()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, null, null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 42);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("This is a test paragraph.  It has more 2\"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(3, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting at the end of a word to see if it moves to the next word boundary.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EndOfWord()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, null, null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 7);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("This is 2a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(3, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting at the end of a word to see if it moves past the white space
		/// when the next word is preceded with punctuation.
		/// example:
		///     This is a test| "paragraph"
		/// result:
		///     This is a test 2"paragraph"
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EndOfWord_NextWordHasPunct()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, null, null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 38);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("This is a test paragraph.  It has more 2\"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(3, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting between the end of the word and the sentence-final punctuation
		/// whent the following sentence begins with punctuation.
		/// example:
		///     This is a test|. "Next sentence"
		/// result:
		///     This is a test. 2"Next sentence"
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void EndOfSentenceWithPunctuation()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, null, null);

			DynamicMock sel = SetupInsertVerseSelection(para, section, 52);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = para.Contents;
			Assert.AreEqual("This is a test paragraph.  It has more \"stuff\" in it. 2\"I wish,\" said John.",
				tss.Text);
			ITsTextProps ttp = tss.get_Properties(1);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(3, tss.RunCount);
		}
		#endregion

		#region Remove Following Duplicate Verse tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests clicking between verse numbers, to "move" the following verse number
		/// to the IP.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InBetweenVerses()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			IStTxtPara para = SetupParagraph(section, "1", "6");

			// Use the Insert Verse command to set up the test scenario with two verse numbers.
			DynamicMock sel = SetupInsertVerseSelection(para, section, 29);
			m_draftView.InsertVerseNumber(); //insert verse number 7
			ITsString tss = para.Contents;
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			// Now test "moving" the verse number.
			sel = SetupInsertVerseSelection(para, section, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// we expect that verse number 7 is inserted at ich of 7, and later duplicate one is removed
			tss = para.Contents;
			Assert.AreEqual("16This 7is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(5, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number where the next (duplicate) verse number is in the
		/// following paragraph. Duplicate should be removed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NextVerseInNextPara()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, "1", "6");
			IStTxtPara para2 = SetupParagraph(section, null, "7");

			// mock up a selection in a para at ich 29
			DynamicMock sel = SetupInsertVerseSelection(para, section, 29);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// Check the paragraph where the verse was inserted.
			ITsString tss = para.Contents;
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(5, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Check the following paragraph which should have the verse number removed.
			tss = para2.Contents;
			Assert.AreEqual("This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(1, tss.RunCount);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number where the next (duplicate) verse number is in the
		/// following section. Duplicate should be removed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NextVerseInNextSection()
		{
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IStTxtPara para_s1 = SetupParagraph(section1, "1", "6");

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IStTxtPara para_s2 = SetupParagraph(section2, null, "7");

			// mock up a selection in a para at ich 29
			DynamicMock sel = SetupInsertVerseSelection(para_s1, section1, 29);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// Check the paragraph where the verse was inserted.
			ITsString tss = para_s1.Contents;
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(5, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Check the following section which should have the verse number removed.
			tss = para_s2.Contents;
			Assert.AreEqual("This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(1, tss.RunCount);
		}
		#endregion

		#region Undo tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that undo InsertVerseNumbers creates two undo tasks for two inserted verse
		/// numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoCreatesSeparateUndoTasks()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = SetupParagraph(section, "1", "1");

			// set up our selection
			DynamicMock sel = SetupInsertVerseSelection(para, section, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();

			sel = SetupInsertVerseSelection(para, section, 22);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();
		}
		#endregion

		#region Right-to-left Tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests verse bridge insertion into the draft view for right to left languages.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void CreateVerseBridge_RightToLeft()
		{
			// Set the default writing system to a right-to-left writing system (Urdu)
			ChangeDefaultVernWs(m_wsUrdu);

			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddFormatTextToMockedPara(m_book, para, @"\v1\*" + m_WordsText, m_wsUrdu.Handle);

			// mock up a selection in a para at ich 1 and insert a verse number
			DynamicMock sel = SetupInsertVerseSelection(para, section, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// mock up another selection in a para at ich 1 and insert a verse number
			sel = SetupInsertVerseSelection(para, section, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// We expect there to be a right-to-left marker before and after the bridge.
			// Verse 1 should be extended into a "1-3" verse bridge.
			ITsString tss = para.Contents;
			Assert.AreEqual(2, tss.RunCount);
			string bridge = m_scr.BridgeForWs(Cache.DefaultVernWs);
			Assert.AreEqual("1" + bridge + "3", tss.get_RunText(0));
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region TODO later
		// ???Insert a verse number just before or after another verse number to make sure it
		// creates a verse bridge. What range?

		// Insert a verse number in the middle of a multi-digit verse number (e.g. click between
		// 1 and 0 in verse 10). Make sure it doesn't insert the verse number right at that
		// position - which will give verse 1110;-) What should happen?

		// ???Insert a verse number next to a verse bridge to make sure it adjusts the bridge
		// to include the new number. What range?

		// -- test what happens if selection is a range. For now, we assume that in
		//  Insert Verse Number mode we will act on clicks setting the IP, and not be
		//  acting on an existing range.
		// -- test inserting in case where numbers in script are not English (when implemented)
		#endregion
	}
	#endregion
}
