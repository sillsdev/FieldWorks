// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DraftViewInsertVerseNumberBtTests.cs
// Responsibility: TE Team

using System;

using NMock;
using NMock.Constraints;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for inserting chapter and verse numbers and footnotes in a back translation.
	/// These tests use mock objects and so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BTInsertVerseAndFootnoteTests : TeTestBase
	{
		#region Member variables
		private DummyDraftView m_btView; // mimick back translation view using DummyDraftView
		private DynamicMock m_styleSheet;
		private DynamicMock m_rootBox;
		private DynamicMock m_vwGraphics;
		private DynamicMock m_selHelper;
		private DynamicMock m_sel;
		private object[] m_TextSelInfoArgs_anchor;
		private object[] m_TextSelInfoArgs_end;
		private string[] m_TextSelInfoTypes;
		private const int kflidParaContent = StTxtParaTags.kflidContents;
		private const int kflidTrans = CmTranslationTags.kflidTranslation;
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

			string intType = "System.Int32";
			string intRef = intType + "&";
			string boolType = typeof(bool).FullName;
			string boolRef = boolType + "&";

			m_TextSelInfoArgs_anchor = new object[] { true, null, null, null, null, null, null };
			m_TextSelInfoArgs_end = new object[] { false, null, null, null, null, null, null };
			m_TextSelInfoTypes =
				new string[] { boolType, "SIL.FieldWorks.Common.COMInterfaces.ITsString&", intRef,
							 boolRef, intRef, intRef, intRef};

			m_selHelper = new DynamicMock(typeof(SelectionHelper));
			m_selHelper.Strict = true;
			m_selHelper.SetupResult("RestoreSelectionAndScrollPos", true);
			SelectionHelper.s_mockedSelectionHelper = (SelectionHelper)m_selHelper.MockInstance;
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
		/// Create a dummy Back Translation view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_styleSheet = new DynamicMock(typeof(IVwStylesheet));
			m_styleSheet.Strict = true;

			InitializeVwSelection(m_selHelper);

			m_btView = new DummyDraftView(Cache, true, 0);
			m_btView.RootBox = SetupRootBox();
			m_btView.Graphics = SetupGraphics();
			m_btView.MakeRoot();
			m_btView.StyleSheet = (IVwStylesheet)m_styleSheet.MockInstance;
			m_btView.ActivateView();
			m_rootBox.Strict = true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();

			m_styleSheet = null;
			m_rootBox = null;
			var disposable = m_btView.Graphics as IDisposable;
			if (disposable != null)
				disposable.Dispose();
			m_btView.Dispose();
			m_btView = null;
			m_vwGraphics = null;
			m_sel = null;
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
			m_rootBox.SetupResult("Site", m_btView);
			m_rootBox.Ignore("Close");
			m_rootBox.Ignore("FlashInsertionPoint");
			m_rootBox.SetupResult("GetRootObject", null,
				new string[] { typeof(int).FullName + "&", typeof(IVwViewConstructor).FullName + "&", typeof(int).FullName + "&", typeof(IVwStylesheet).FullName + "&" },
				new object[] { 0, null, 0, null });

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the VwSelection mock and adds it as result of the
		/// SelectionHelper.Selection property.
		/// </summary>
		/// <param name="selHelper">The sel helper mock.</param>
		/// ------------------------------------------------------------------------------------
		public static void InitializeVwSelection(DynamicMock selHelper)
		{
			string[] kLocationArgs = new string[]{typeof(IVwGraphics).FullName,
				typeof(Rect).FullName, typeof(Rect).FullName, typeof(Rect).FullName + "&",
				typeof(Rect).FullName + "&", typeof(bool).FullName + "&", typeof(bool).FullName + "&"};

			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.Strict = false;
			sel.SetupResult("SelType", VwSelType.kstText);
			sel.SetupResult("IsValid", true);
			sel.SetupResult("IsRange", false);
			sel.SetupResult("Location", null, kLocationArgs,
				new object[]{null, null, null, new Rect(10, 10, 10, 10),
					new Rect(0, 0, 0, 0), false, false });
			selHelper.SetupResult("Selection", sel.MockInstance);
		}

		#endregion

		#region Helper functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for mocked back translation selection etc.
		/// </summary>
		/// <param name="nInsertVerseNumbersCalled">Number of times InsertVerseNumber gets
		/// called in the test.</param>
		/// <param name="hvoTrans"></param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupMockedBtSelection(int nInsertVerseNumbersCalled, int hvoTrans)
		{
			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.Strict = true;
			sel.SetupResult("SelType", VwSelType.kstText);
			sel.SetupResult("IsValid", true);

			m_selHelper.ExpectAndReturn(nInsertVerseNumbersCalled, "ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, true);

			ITsPropsBldr builder = TsPropsBldrClass.Create();
			ITsTextProps selProps = builder.GetTextProps();
			for (int i = 0; i < nInsertVerseNumbersCalled; i++)
			{
				m_selHelper.ExpectAndReturn("SelProps", selProps);
				m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, true);
				m_selHelper.Expect(3, "AssocPrev", true); // 1
				m_selHelper.Expect("SetSelection", true);
			}
			m_selHelper.SetupResult("Selection", sel.MockInstance);

			m_selHelper.SetupResult("AssocPrev", true);
			m_selHelper.Expect("SetIPAfterUOW");

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the mocked selection helper with needed level info for a translation.
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="trans">The trans.</param>
		/// <param name="wsTrans">The ws trans.</param>
		/// <param name="section">The section.</param>
		/// <param name="book">The book.</param>
		/// ------------------------------------------------------------------------------------
		private void SetupSelHelperLevelInfo(IStTxtPara para, ICmTranslation trans, int wsTrans,
			IScrSection section, IScrBook book)
		{
			SelLevInfo[] selLevInfo = new SelLevInfo[5];
			selLevInfo[4].tag = m_btView.BookFilter.Tag;
			selLevInfo[4].ihvo = 0;
			selLevInfo[4].hvo = book.Hvo;
			selLevInfo[3].tag = ScrBookTags.kflidSections;
			selLevInfo[3].ihvo = 0;
			selLevInfo[3].hvo = section.Hvo;
			selLevInfo[2].ihvo = 0;
			selLevInfo[2].tag = ScrSectionTags.kflidContent;
			selLevInfo[1].ihvo = 0;
			selLevInfo[1].hvo = para.Hvo;
			selLevInfo[1].tag = StTextTags.kflidParagraphs;
			selLevInfo[0].ihvo = 0;
			selLevInfo[0].hvo = trans.Hvo;
			selLevInfo[0].ws = 0; //not useful, but that's what actual selLevelInfo[0] has
			selLevInfo[0].tag = -1; //not useful, but that's what actual selLevelInfo[0] has
			m_selHelper.SetupResult("LevelInfo", selLevInfo);
			m_selHelper.SetupResult("GetLevelInfo", selLevInfo,
				new Type[] { typeof(SelectionHelper.SelLimitType) });
			m_selHelper.SetupResult("NumberOfLevels", 5);
		}

		private void SetupSelForVerseInsert(int ichInitial, ICmTranslation trans, int wsBT)
		{
#if DEBUG
			m_selHelper.SetupResult("IsRange", false);
#endif
			m_selHelper.SetupResult("IchAnchor", ichInitial);
			m_selHelper.SetupResult("IchEnd", ichInitial);
			m_selHelper.Expect("SetIPAfterUOW");

			ITsString tssTrans = trans.Translation.get_String(wsBT);
			m_sel.ExpectAndReturn("TextSelInfo", null, m_TextSelInfoArgs_anchor, m_TextSelInfoTypes,
				new object[] { true, tssTrans, ichInitial, false, trans.Hvo, kflidTrans, wsBT });

		}

		// Helper function appends a run to the given string builder
		private void AddRunToStrBldr(ITsStrBldr strBldr, string text, int ws, string charStyle)
		{
			strBldr.ReplaceRgch(strBldr.Length, strBldr.Length, text, text.Length,
				StyleUtils.CharStyleTextProps(charStyle, ws));
		}
		#endregion

		#region Verse insertion where chapter number is necessary (TE-2278)

		#region Tests with vernacular: (C1)(V1)text (V2)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is at the start of the BT (and vernacular has chapter and verse at start).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1V1Txt_BtTxt_IpInFirstWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			//set IP in first word
			SetupSelForVerseInsert(2, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect C1V1 to be inserted at the start.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("11one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the second word of the BT (and vernacular has chapter and verse at start).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1V1Txt_BtTxt_IpAtSecondWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP on second verse and insert first verse number
			SetupSelForVerseInsert(4, trans, wsBT); //set IP at start of second word
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect V2 to be inserted at "two"
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("one 2two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "one ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is at the beginning of the BT and the BT has a bogus initial verse number
		/// (and vernacular has chapter and verse at start).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1V1Txt_BtV3Txt_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at beginning of BT and insert (really does an update) first verse number
			SetupSelForVerseInsert(0, trans, wsBT); //set IP at start
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V3 updated to C1V1.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("11one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that has a bogus chapter number at the start.
		/// The vernacular has chapter 1 and verse 1 at start.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1V1Txt_BtC2Txt_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			//set IP at start
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result.  We expect to see C2 updated to C1V1.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("11one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that has a bogus chapter and verse at the start.
		/// The vernacular has chapter 1 and verse 1 at start.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1V1Txt_BtC2V3Txt_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			//set IP at start
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result.  We expect to see C2V3 updated to C1V1.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("11one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when
		/// the BT has a bogus mid-para chapter 2, verse 3. The IP is at the C2.
		/// The vernacular has chapter 1 and verse 1 at start.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1V1Txt_BtTxtC2V3_IpAtC2()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "one ", null);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at the C2V3 in the BT
			SetupSelForVerseInsert(4, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see C2V3 to be updated to V2.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("one 2two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "one ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting chapter/verse number at the beginning of a back translation
		/// paragraph when the back translation contains the same chapter and verse later
		/// in the paragraph.
		/// The vernacular is (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC2V1Txt_BtTxtC2V1_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "text", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at beginning of Bt paragraph
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see C2V1 inserted at the start, and the
			// following chapter/verse runs removed.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("21first text", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "first text", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting chapter/verse number at the beginning of a back translation
		/// paragraph when the back translation contains a chapter and verse later
		/// in the paragraph which is not in the vernacular range.
		/// The vernacular is (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC2V1Txt_BtTxtC3V1_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at beginning of Bt paragraph
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see C2V1 inserted at the start, and the
			// following C3V1 runs removed because they aren't contained in the vernacular range.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("21first one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "first one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test inserting chapter/verse number at the beginning of a back translation
		/// paragraph when the back translation contains a subsequent chapter and verse later
		/// in the paragraph.
		/// The vernacular is (C2)(V1)text (C3) (V1).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC2V1TxtC3V1_BtTxtC3V1_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at beginning of Bt paragraph
			SetupSelForVerseInsert(0, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see chapter 2, verse one at the start and the
			// following chapter/verse runs remain.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("21first 31one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test updating chapter/verse numbers within a back translation paragraph when the
		/// back translation contains a chapter and verse later in the paragraph which is not
		/// in the vernacular.
		/// The vernacular is (C2)(V1)text (V2)Text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void UpdateChapter_VernC2V1TxtV2Txt_BtTxtC2V3_IpAtC2()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "second", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at chapter 2 of BT
			SetupSelForVerseInsert(6, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect Chapter 2 and Verse 3 in the BT to be replaced by
			//  Verse 2 from the vernacular.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 2second", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "second", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion

		#region Tests with vernacular: (C1)text (V2)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that is all text.
		/// The vernacular has chapter 1 at start with implied verse 1.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1Txt_BtTxt_IpInFirstWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			//set IP in the first word of the BT
			SetupSelForVerseInsert(2, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see chapter 1 inserted at the start
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the second word of the BT that is all text.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1Txt_BtTxt_IpInSecondWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			//set IP in the second word of the BT
			SetupSelForVerseInsert(5, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to insert verse number 2 at the second word.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("one 2two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "one ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is at the start of the BT that begins with a bogus verse 3.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1Txt_BtV3Txt_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at the start of the BT
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see the V3 updated to C1.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that begins with a bogus chapter 2.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1Txt_BtC2_IpInFirstWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP in the second word of the BT
			SetupSelForVerseInsert(2, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see the C2 updated to C1.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is at the start of the BT that begins with a bogus chapter 2 and verse 1.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1Txt_BtC2V1Txt_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at the start of the BT
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see the C2V1 updated to C1.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1one two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when
		/// the BT has a bogus mid-para chapter 2. The IP is at the C2.
		/// The vernacular has chapter 1 at start with implied verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernC1Txt_BtTxtC2Txt_IpAtC2()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "one ", null);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "two", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP after the C2
			SetupSelForVerseInsert(5, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see C2 to be updated to V2.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("one 2two", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "one ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "two", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion

		#region Tests with vernacular: (V2)text (V3)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is at the beginning of the BT that begins with bogus chapter 2.
		/// The vernacular has verses two and three.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernV2TxtV3_BtC2Txt_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "tres ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "two three", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at start of BT
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to update chapter 2 to verse 2
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2two three", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "two three", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that begins with a bogus chapter 2 and verse 1.
		/// The vernacular has verses two and three.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernV2TxtV3_BtC2V1_IpInFirstWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "tres ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "two three", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP in first word, after the C2V1
			SetupSelForVerseInsert(4, trans, wsBT); //set IP on second word
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to update the C2V1 to V2.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2two three", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "two three", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion

		#region Tests with vernacular: text (C2)(V1)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2V1_BtTxt_IpInFirstWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			//Set IP on second character (first word)
			SetupSelForVerseInsert(1, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see no change.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the second word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2V1_BtTxt_IPInSecondWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			//Set IP on second word
			SetupSelForVerseInsert(8, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect C2V1 to be inserted before the second word.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 21one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the second word of the BT after a mid-para bogus verse number 3.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2V1_BtTxtV3_IpAfterV3()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP after V3
			SetupSelForVerseInsert(7, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to update the V3 to C2V1 at "one".
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 21one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the second word of the BT after a mid-para bogus chapter number 3.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2V1_BtTxtC3_IpInSecondWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP in the second word, after chapter 3
			SetupSelForVerseInsert(9, trans, wsBT);

			// insert first verse number
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to update chapter 3 to chapter 2, verse number 1
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 21one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP in the BT is after a mid-para bogus chapter 3 and verse 3.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2V1_BtTxtC3V3_IpOnC3V3()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP between C3 and V3
			SetupSelForVerseInsert(7, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to update C3V3 to C2V1
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 21one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that begins with a bogus chapter 3 and verse 1.
		/// The vernacular begins with text and is followed by (C2)(V1)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2V1_BtC3V1_IpBetweenC3V1()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP between C3V1
			SetupSelForVerseInsert(1, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see no change.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("31one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion

		#region Tests with vernacular: text (C2)text
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the first word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2_BtTxt_IpInFirstWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP on first word in BT
			SetupSelForVerseInsert(1, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no changes.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the second word of the BT that is all text.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2_BtTxt_IpInSecondWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP on second word in BT
			SetupSelForVerseInsert(8, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect C2 to be inserted before the second word.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on a bogus verse number 3 in the middle of the BT para.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2_BtTxtV3_IpAtV3()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP just before the V3
			SetupSelForVerseInsert(6, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see the V3 updated to C2.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on the second word of the BT just after a bogus C3.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2_BtTxtC3_IpInSecondWord()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP in the second word, after the C3
			SetupSelForVerseInsert(8, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see the C3 updated to C2.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// IP is on a bogus C3V3 in the middle of the BT para.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2_BtTxtC3V3_IpBetweenC3V3()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "first ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP in between the C3 and V3 in the BT
			SetupSelForVerseInsert(7, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see the C3V3 updated to C2.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("first 2one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "first ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse number into a back translation paragraph when the
		/// back translation begins with bogus chapter 3 and verse 3 numbers.
		/// The IP is placed between the C3 and V1.
		/// The vernacular begins with text and is followed by (C2)text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerse_VernTxtC2_BtC3V1Txt_IpBetweenC3V1()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "principio ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "uno ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "one", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP between the C3 and V1 in the BT.
			SetupSelForVerseInsert(1, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see no change.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("31one", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results.
			// We expect no change, because there are no CV numbers at the start of the vernacular.
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "one", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion
		#endregion

		#region Tests for space before vernacular verse (TE-4789)
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse reference at the start of a back translation paragraph when
		/// the corresponding verse is preceeded by a space in the vernacular.
		/// The vernacular is a sequence of verse numbers and verse text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStartsWithSpace_Bt_IpAtStart()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, " ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "3-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. four. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "dos. ", null);
			AddRunToMockedTrans(trans, wsBT, "tres. quatro. ", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP at the start of the back translation.
			SetupSelForVerseInsert(0, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 2 inserted at the beginning of the BT.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. tres. quatro. ", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos. tres. quatro. ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion

		#region Tests for SpaceBeforeVerseNumber (TE-4415)
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse reference into the back translation when the corresponding
		/// verse (or bridge) is missing in the back translation.
		/// The IP is at a space just before existing text.
		/// The vernacular is a sequence of verse numbers and verse text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernSeq_BtV2Txt_IpAtSpaceBeforeText()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "3-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos. ", null);
			AddRunToMockedTrans(trans, wsBT, "tres. quatro. cinco. ", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP after "dos.", where it is followed by a space and then "tres".
			SetupSelForVerseInsert(5, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V3-4 inserted just before "tres".
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. 3-4tres. quatro. cinco. ", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos. ", wsBT, null);
			AddRunToStrBldr(strBldr, "3-4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres. quatro. cinco. ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating a verse reference in the back translation when the vernacular has
		/// a verse bridge but the corresponding verse in the back translation has only the
		/// end number of the bridge.
		/// The insertion point is at a space just before the existing "end number" in the BT.
		/// The vernacular is a sequence of verse numbers and verse text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernSeq_BtV2TxtV4_IpAtSpaceBeforeVerse()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "3-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos. ", null);
			AddRunToMockedTrans(trans, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP after "dos.", where it is followed by a space and then V4.
			SetupSelForVerseInsert(5, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V4 updated to V3-4.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. 3-4quatro.", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos. ", wsBT, null);
			AddRunToStrBldr(strBldr, "3-4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro.", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating a verse reference in the back translation to the corresponding
		/// reference in the vernacular (a mid-para chapter).
		/// The insertion point is at a space just before an existing number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernV2TxtC2_BtV2TxtV5_IpAtSpaceBeforeVerse()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "one. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos. ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "uno. ", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);


			// Set IP after "dos.", where it is followed by a space and then V5.
			SetupSelForVerseInsert(5, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V5 updated to C2.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. 2uno. ", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos. ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "uno. ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating a verse reference in the back translation to the corresponding
		/// reference in the vernacular (a mid-para chapter and verse).
		/// The insertion point is at a space just before an existing number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernV2TxtC2V1_BtV2TxtV5_IpAtSpaceBeforeVerse()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "one. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos. ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "uno. ", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP after "dos.", where it is followed by a space and then V5.
			SetupSelForVerseInsert(5, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V5 updated to C2V1.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. 21uno. ", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos. ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "uno. ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert a verse reference into the back translation when a mismatch
		/// is encountered in prior verse numbers (therefore it is not possible to insert/
		/// update the verse number near the IP).
		/// The insertion point is at a space just before existing text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void PriorMismatch_VernV23Bridge_BtV2Txt_IpAtSpaceBeforeText()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2-3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. three. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos. tres. quatro.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP after "dos.", where it is followed by a space and then "tres".
			SetupSelForVerseInsert(5, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no change because verse numbers prior to the IP
			//  (prior but not adjacent) are mismatched.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. tres. quatro.", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos. tres. quatro.", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion

		#region Basic verse insertion tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test attempts to insert verse numbers into a text-only back translation paragraph
		/// when the vernacular paragraph is all text (no verse numbers).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernAllTxt_BtTxt_InsSeq_NoChg()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "Verse three. Verses four to six. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "tres quatro cinco seis", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Set IP and attempt to insert verse reference at beginning of back translation.
			SetupSelForVerseInsert(0, trans, wsBT); // Insert at ich 0
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no changes.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("tres quatro cinco seis", tssResult.Text);

			// Set IP and attempt to insert in middle of back translation string
			SetupSelForVerseInsert(5, trans, wsBT); // mock will return prior result
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no changes.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("tres quatro cinco seis", tssResult.Text);

			// Set IP and attempt to insert at the end of back translation string.
			SetupSelForVerseInsert(tssResult.Length, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no changes.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("tres quatro cinco seis", tssResult.Text);

			// verify some mocks
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "tres quatro cinco seis", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse reference in a back translation paragraph when the following
		/// section does not have back translations (TE-5282).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NextVernSectionHasNoBT()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. ", null); // Verse number 4 missing
			AddRunToMockedPara(parentPara, "5-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses five to six. ", null);

			IScrSection nextSection = AddSectionToMockedBook(book);
			IStTxtPara nextParentPara = AddParaToMockedSectionContent(
				nextSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(nextParentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(nextParentPara, "seven. ", null);
			AddRunToMockedPara(nextParentPara, "8", ScrStyleNames.VerseNumber);


			// Construct the back translation for the first section.
			// The second section does not have a back translation.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "verses", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(4, trans.Hvo);

			// Set IP and Insert first verse number
			SetupSelForVerseInsert(0, trans, wsBT); //set IP at ich 0
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 3 at the IchAnchor position (1).
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3verses", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting three verse references in a back translation paragraph.
		/// Some verse numbers in the vernacular paragraph are missing or not in sequence.
		/// The first verse in the vernacular is at start of the paragraph. We also insert
		/// starting at the beginning of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernBadSeq_BtTxt_InsSeq()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. ", null); // Verse number 4 missing
			AddRunToMockedPara(parentPara, "5-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses five to six. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "tres cinco seis quatro dos", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(4, trans.Hvo);

			// Set IP and Insert first verse number
			SetupSelForVerseInsert(0, trans, wsBT); //set IP at ich 0
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 3 at the IchAnchor position (1).
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres cinco seis quatro dos", tssResult.Text);

			// Set IP and Insert second verse numbers
			SetupSelForVerseInsert(6, trans, wsBT); // IP set before "cinco"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 5-6 at the IchAnchor position.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres 5-6cinco seis quatro dos",
				tssResult.Text);

			// Set IP and Insert third verse reference
			SetupSelForVerseInsert(22, trans, wsBT); // IP set within "quatro"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 4 at the IchAnchor position.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres 5-6cinco seis 4quatro dos",
				tssResult.Text);

			// Set IP and Insert fourth verse reference
			SetupSelForVerseInsert(28, trans, wsBT); // IP set before "dos"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 2 at the IchAnchor position.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres 5-6cinco seis 4quatro 2dos",
				tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			AddRunToStrBldr(strBldr, "5-6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco seis ", wsBT, null);
			AddRunToStrBldr(strBldr, "4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating verse references in a back translation paragraph.
		/// Some verse numbers in the vernacular paragraph are missing or not in sequence, but
		/// verse numbers in the BT are in sequence.
		/// The first verse in the vernacular is at start of the paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernBadSeq_BtNormSeq_UpdSeq()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "seven. ", null);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos ", null);
			AddRunToMockedTrans(trans, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "cinco ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro ", null);
			AddRunToMockedTrans(trans, wsBT, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "siete ", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(5, trans.Hvo);

			// Set IP and Insert first verse number
			SetupSelForVerseInsert(0, trans, wsBT); //set IP at ich 0
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V3 updated to V2 the starting position.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 4cinco 5quatro 6siete ", tssResult.Text);

			// Set IP and Insert second verse numbers
			SetupSelForVerseInsert(8, trans, wsBT); // set IP within "cinco"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V4 updated to V5 before "cinco"
			//  and the second "5" removed because it appears to be a duplicate!
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 5cinco quatro 6siete ", tssResult.Text);

			// Set IP and Insert third verse reference
			SetupSelForVerseInsert(12, trans, wsBT); // set IP before "quatro"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse "4" inserted before "quatro"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 5cinco 4quatro 6siete ", tssResult.Text);

			// Set IP and Insert fourth verse reference
			SetupSelForVerseInsert(20, trans, wsBT); // set IP before "6"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see V6 updated to V7 before "siete"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 5cinco 4quatro 7siete ", tssResult.Text);

			// Set IP and Insert fifth verse reference
			SetupSelForVerseInsert(tssResult.Length, trans, wsBT); // set IP at end of string
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect verse "1" inserted at the end of the string.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 5cinco 4quatro 7siete 1", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco ", wsBT, null);
			AddRunToStrBldr(strBldr, "4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro ", wsBT, null);
			AddRunToStrBldr(strBldr, "7", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "siete ", wsBT, null);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests attempts to insert verse number into a back translation paragraph when the
		/// vernacular paragraph is empty.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernEmpty_BtTxt_InsSeq_NoChg()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "tres quatro cinco seis", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Setup and Attempt to insert verse reference at beginning of back translation.
			SetupSelForVerseInsert(0, trans, wsBT); // Insert at ich 0
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no changes.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("tres quatro cinco seis", tssResult.Text);

			// Set IP and attempt to insert in middle of back translation string
			SetupSelForVerseInsert(5, trans, wsBT); // mock will return prior result
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no changes.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("tres quatro cinco seis", tssResult.Text);

			// Set IP and attempt to insert at the end of back translation string.
			SetupSelForVerseInsert(tssResult.Length, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no changes.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("tres quatro cinco seis", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "tres quatro cinco seis", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting three consecutive verse references in a back translation paragraph.
		/// We insert beginning at the start of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertConsecutiveVerses()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses four to six. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "tres quatro-seis siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Set IP and Insert first verse number
			SetupSelForVerseInsert(0, trans, wsBT); //set IP at start
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 3 at "tres"
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres quatro-seis siete ocho", tssResult.Text);

			// Set IP and Insert second verse number
			SetupSelForVerseInsert(11, trans, wsBT); // set IP within "quatro"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 4-6 at "quatro"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres 4-6quatro-seis siete ocho",
				tssResult.Text);

			// Set IP and Insert third verse reference
			SetupSelForVerseInsert(21, trans, wsBT); // set IP before "siete"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 7 at "siete"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres 4-6quatro-seis 7siete ocho",
				tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			AddRunToStrBldr(strBldr, "4-6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro-seis ", wsBT, null);
			AddRunToStrBldr(strBldr, "7", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting three consecutive verse references in a back translation paragraph.
		/// The first verse in the vernacular is in the middle of the paragraph. We also insert
		/// in the middle of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertConsecutiveVerses_FirstVerseMidPara()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "End of two.", null);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses four to six. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "Some text. ", null);
			AddRunToMockedTrans(trans, wsBT, "tres quatro-seis siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Set IP and Insert first verse number
			SetupSelForVerseInsert(11, trans, wsBT); //set IP at start of "tres"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 3 at "tres"
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("Some text. 3tres quatro-seis siete ocho",
				tssResult.Text);

			// Set IP and Insert second verse number
			SetupSelForVerseInsert(17, trans, wsBT); //set IP at start of "quatro"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 4-6 at "quatro"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("Some text. 3tres 4-6quatro-seis siete ocho",
				tssResult.Text);

			// Set IP and Insert third verse reference
			SetupSelForVerseInsert(32, trans, wsBT); // set IP at start of "siete"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 7 at "siete"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("Some text. 3tres 4-6quatro-seis 7siete ocho",
				tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "Some text. ", wsBT, null);
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			AddRunToStrBldr(strBldr, "4-6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro-seis ", wsBT, null);
			AddRunToStrBldr(strBldr, "7", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting one verse number in a back translation paragraph.
		/// The first verse in the vernacular is at start of the paragraph. We also insert
		/// at the beginning of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernSeq_BtText_IpInFirstWord_InsV3()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses four to six. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "tres quatro-seis siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP in the first word.
			SetupSelForVerseInsert(1, trans, wsBT);

			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the results
			// We expect to see verse 3 at the start.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres quatro-seis siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();

			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating one verse number at the beginning of a back translation paragraph.
		/// The first verse in the vernacular is at start of the paragraph. We also update
		/// at the beginning of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernV23Bridge_BtV2Text_IpAfterV2_UpdToBridge()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2-3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. three.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Setup and update first verse number.
			SetupSelForVerseInsert(1, trans, wsBT); //Ip at beg
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see verse 2-3 at the IchAnchor position, also "3" deleted.
			ITsString tssResult = trans.Translation.get_String(wsBT);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2-3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos tres", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating one verse number at the middle of a back translation paragraph.
		/// It will update a bridge to a single verse.
		/// The first verse in the vernacular is at start of the paragraph. We also update
		/// at the beginning of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdMidToSingle()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos ", null);
			AddRunToMockedTrans(trans, wsBT, "3-4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres quatro ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "cinco", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(2, trans.Hvo);

			// Setup and update 3-4 verse number reference.
			SetupSelForVerseInsert(8, trans, wsBT); // IP at end of 3-4 bridge
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect "3-4" to become "3"
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 3tres quatro 5cinco", tssResult.Text);

			// Set IP and Insert verse 4 verse number reference.
			SetupSelForVerseInsert(14, trans, wsBT); // IP in middle of "quatro"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see verse 4 at the IchAnchor position.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 3tres 4quatro 5cinco", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos ", wsBT, null);
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			AddRunToStrBldr(strBldr, "4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating one verse number at the middle of a back translation paragraph.
		/// It will update a single verse to a bridge.
		/// The first verse in the vernacular is at start of the paragraph. That's also
		/// true of the back translation.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdMidToBridge()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "3-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres ", null);
			AddRunToMockedTrans(trans, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "cinco", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(2, trans.Hvo);

			// Set IP and attempt to update verse number "4".
			SetupSelForVerseInsert(12, trans, wsBT); // IP after verse 4
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect no change.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 3tres 4quatro 5cinco", tssResult.Text);

			// Setup and update verse number "3".
			SetupSelForVerseInsert(5, trans, wsBT); // IP before verse 3
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see a bridge "3-4" in place of "3", and "4" deleted.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 3-4tres quatro 5cinco", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos ", wsBT, null);
			AddRunToStrBldr(strBldr, "3-4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres quatro ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating one verse number at the middle of a back translation paragraph.
		/// It will update a single verse to a three-verse bridge and remove all duplicate
		/// verse numbers following the update.
		/// The first verse in the vernacular is at start of the paragraph. That's also
		/// true of the back translation.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdMidTo3VerseBridge()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "one. ", null);
			AddRunToMockedPara(parentPara, "2-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. three. four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "uno ", null);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres ", null);
			AddRunToMockedTrans(trans, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "cinco", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Set IP and attempt to update verse number "3".
			SetupSelForVerseInsert(10, trans, wsBT); // IP before verse 3
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect no change.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2dos 3tres 4quatro 5cinco", tssResult.Text);

			// Set IP and attempt to update verse number "4".
			SetupSelForVerseInsert(17, trans, wsBT); // IP after verse 4
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect no change.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2dos 3tres 4quatro 5cinco", tssResult.Text);

			// Setup and update verse number "2".
			SetupSelForVerseInsert(5, trans, wsBT); // IP before verse 2
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see a bridge "2-4" in place of "2", and "3" and "4" deleted.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2-4dos tres quatro 5cinco", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "uno ", wsBT, null);
			AddRunToStrBldr(strBldr, "2-4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos tres quatro ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting one verse number at the end of a back translation paragraph.
		/// It will insert a single verse and not crash when looking for a duplicate verse
		/// number in a following para.
		/// The first verse in the vernacular is at start of the paragraph. That's also
		/// true of the back translation.
		/// </summary>
		/// <remarks>Jira task is TE-4802</remarks>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerseInBtWhichIsLongerThanVernPara()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "one. ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "Este es el primer versiculo ", null);
			int ichLimOfBt = trans.Translation.get_String(wsBT).Length;

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP and attempt to insert verse number "2".
			SetupSelForVerseInsert(ichLimOfBt, trans, wsBT); // IP at end of para
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see a verse "2" added at the end of the BT.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1Este es el primer versiculo 2", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "Este es el primer versiculo ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting one verse number at the end of a back translation paragraph.
		/// It will insert a single verse and then remove a duplicate verse
		/// number in a following section.
		/// The first verse in the vernacular is at start of the paragraph. That's also
		/// true of the back translation.
		/// </summary>
		/// <remarks>Jira task is TE-4802</remarks>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerseInBtAndRemoveDupInNextSection()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section1 = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara1, "one. ", null);
			AddRunToMockedPara(parentPara1, "2", ScrStyleNames.VerseNumber);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara1, wsBT);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "Este es el primer versiculo ", null);
			int ichLimOfBt = trans.Translation.get_String(wsBT).Length;

			IScrSection section2 = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara2, "3", ScrStyleNames.VerseNumber);

			// Construct the initial back translation for the para in section 2
			ICmTranslation trans2 = AddBtToMockedParagraph(parentPara2, wsBT);
			AddRunToMockedTrans(trans2, wsBT, "2", ScrStyleNames.VerseNumber);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara1, trans, wsBT, section1, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP and attempt to insert verse number "2".
			SetupSelForVerseInsert(ichLimOfBt, trans, wsBT); // IP at end of para
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see a verse "2" added at the end of the BT.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1Este es el primer versiculo 2", tssResult.Text);

			// We expect to see a verse "2" removed from the next section.
			tssResult = trans2.Translation.get_String(wsBT);
			Assert.IsNull(tssResult.Text, "Failed to remove duplicate verse number.");

			m_sel.Verify();
			m_rootBox.Verify();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating one verse number at the middle of a back translation paragraph.
		/// It will update a single verse to a three-verse bridge and remove all duplicate
		/// verse numbers following the update. The last verse number removed is the last run
		/// of the paragraph.
		/// The first verse in the vernacular is at start of the paragraph. That's also
		/// true of the back translation.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdMidTo3VerseBridgeEnd()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "one. ", null);
			AddRunToMockedPara(parentPara, "2-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. three. four. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "uno ", null);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres ", null);
			AddRunToMockedTrans(trans, wsBT, "4", ScrStyleNames.VerseNumber);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Set IP and attempt to update verse number "3".
			SetupSelForVerseInsert(10, trans, wsBT); // IP before verse 3
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect no change.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2dos 3tres 4", tssResult.Text);

			// Set IP and attempt to update verse number "4".
			SetupSelForVerseInsert(17, trans, wsBT); // IP after verse 4
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect no change.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2dos 3tres 4", tssResult.Text);

			// Setup and update verse number "2".
			SetupSelForVerseInsert(5, trans, wsBT); // IP before verse 2
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see a bridge "2-4" in place of "2", and "3" and "4" deleted.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2-4dos tres ", tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "uno ", wsBT, null);
			AddRunToStrBldr(strBldr, "2-4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos tres ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating one verse number at the middle of a back translation paragraph.
		/// It will update a single verse to a three-verse bridge and remove all duplicate
		/// verse numbers following the update. Some of the removed verse numbers are in
		/// following paragraphs.
		/// The first verse in the vernacular is at start of the paragraph. That's also
		/// true of the back translation.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdMidToBridgeMultiPara()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara1 = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara1, "one. ", null);
			AddRunToMockedPara(parentPara1, "2-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara1, "two. three. four. ", null);

			// also following vernacular paragraphs
			IStTxtPara parentPara2 = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);

			IStTxtPara parentPara3 = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara3, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara3, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans1 = AddBtToMockedParagraph(parentPara1, wsBT);
			AddRunToMockedTrans(trans1, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans1, wsBT, "uno ", null);
			AddRunToMockedTrans(trans1, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans1, wsBT, "dos ", null);

			ICmTranslation trans2 = AddBtToMockedParagraph(parentPara2, wsBT);
			AddRunToMockedTrans(trans2, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans2, wsBT, "tres ", null);

			ICmTranslation trans3 = AddBtToMockedParagraph(parentPara3, wsBT);
			AddRunToMockedTrans(trans3, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans3, wsBT, "quatro ", null);
			AddRunToMockedTrans(trans3, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans3, wsBT, "cinco", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara1, trans1, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans1.Hvo);

			// Setup and update verse number "2".
			SetupSelForVerseInsert(6, trans1, wsBT); // IP after verse 2
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see a bridge "2-4" in place of "2", and "3" and "4" deleted.
			ITsString tssResult1 = trans1.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2-4dos ", tssResult1.Text);

			ITsString tssResult2 = trans2.Translation.get_String(wsBT);
			Assert.AreEqual("tres ", tssResult2.Text);

			ITsString tssResult3 = trans3.Translation.get_String(wsBT);
			Assert.AreEqual("quatro 5cinco", tssResult3.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "uno ", wsBT, null);
			AddRunToStrBldr(strBldr, "2-4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult1,
				out difference);
			Assert.IsTrue(fEqual, difference);

			strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			tssExpected = strBldr.GetString();
			fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult2,
				out difference);
			Assert.IsTrue(fEqual, difference);

			strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "quatro ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco", wsBT, null);
			tssExpected = strBldr.GetString();
			fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult3,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number in the middle of a back translation paragraph.
		/// It will insert a three-verse bridge and remove all duplicate verse numbers
		/// following the insertion. Some of the removed verse numbers are in following
		/// paragraphs.
		/// The first verse in the vernacular is at start of the paragraph. That's also
		/// true of the back translation.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseInsMidToBridgeMultiPara()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara1 = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara1, "one. ", null);
			AddRunToMockedPara(parentPara1, "2-4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara1, "two. three. four. ", null);

			// also following vernacular paragraphs
			IStTxtPara parentPara2 = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);

			IStTxtPara parentPara3 = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara3, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara3, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans1 = AddBtToMockedParagraph(parentPara1, wsBT);
			AddRunToMockedTrans(trans1, wsBT, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans1, wsBT, "uno part b ", null);
			AddRunToMockedTrans(trans1, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans1, wsBT, "dos ", null);

			ICmTranslation trans2 = AddBtToMockedParagraph(parentPara2, wsBT);
			AddRunToMockedTrans(trans2, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans2, wsBT, "tres ", null);

			ICmTranslation trans3 = AddBtToMockedParagraph(parentPara3, wsBT);
			AddRunToMockedTrans(trans3, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans3, wsBT, "quatro ", null);
			AddRunToMockedTrans(trans3, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans3, wsBT, "cinco", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara1, trans1, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans1.Hvo);

			// Setup and update verse number "2".
			SetupSelForVerseInsert(7, trans1, wsBT); // IP in middle of "part"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see a bridge "2-4" in front of "part" and "2", "3" and "4" deleted.
			ITsString tssResult1 = trans1.Translation.get_String(wsBT);
			Assert.AreEqual("1uno 2-4part b dos ", tssResult1.Text);

			ITsString tssResult2 = trans2.Translation.get_String(wsBT);
			Assert.AreEqual("tres ", tssResult2.Text);

			ITsString tssResult3 = trans3.Translation.get_String(wsBT);
			Assert.AreEqual("quatro 5cinco", tssResult3.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "uno ", wsBT, null);
			AddRunToStrBldr(strBldr, "2-4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "part b dos ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult1,
				out difference);
			Assert.IsTrue(fEqual, difference);

			strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			tssExpected = strBldr.GetString();
			fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult2,
				out difference);
			Assert.IsTrue(fEqual, difference);

			strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "quatro ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco", wsBT, null);
			tssExpected = strBldr.GetString();
			fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult3,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting one verse number into an empty back translation paragraph.
		/// The first verse in the vernacular is at start of the paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtEmpty()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses four to six. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, string.Empty, null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Insert verse reference into empty back translation
			SetupSelForVerseInsert(0, trans, wsBT); // Insert at ich 0
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			m_sel.Verify();
			m_rootBox.Verify();

			// We expect to see verse 3 at the IchAnchor position.
			ITsString tssResult = trans.Translation.get_String(wsBT);

			//  Verify the results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting three consecutive verse references in a back translation paragraph.
		/// The first verse in the vernacular is at start of the paragraph. We also insert
		/// starting at the beginning of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtTextInsBegSeq()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses four to six. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "tres quatro-seis siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(2, trans.Hvo);

			// Set IP and Insert first verse number
			SetupSelForVerseInsert(1, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 3 at the IchAnchor position (1).
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres quatro-seis siete ocho",
				tssResult.Text);

			// Set IP and Insert second verse numbers
			SetupSelForVerseInsert(6, trans, wsBT); // mock will return prior result
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 4-6 at the IchAnchor position (6).
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres 4-6quatro-seis siete ocho", tssResult.Text);

			// Set IP and Insert third verse reference
			SetupSelForVerseInsert(21, trans, wsBT); // mock will return prior result
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 7 at the IchAnchor position (21).
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres 4-6quatro-seis 7siete ocho",
				tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			AddRunToStrBldr(strBldr, "4-6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro-seis ", wsBT, null);
			AddRunToStrBldr(strBldr, "7", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse reference in a back translation paragraph.
		/// The first verse in the vernacular is at the start of the paragraph. However, we
		/// will insert in the middle of the back translation paragraph. Result should be to
		/// insert the first verse number in the vernacular.</summary>
		/// <remarks>A result we considered is to do two actual insertions: one at the start
		/// and one at IP.</remarks>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtTextInsMid()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses four to six. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "- tres quatro-seis siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP and Insert verse number
			SetupSelForVerseInsert(2, trans, wsBT); // IP on "tres"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see verse 3 at "tres"
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("- 4-6tres quatro-seis siete ocho",
				tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "- ", wsBT, null);
			AddRunToStrBldr(strBldr, "4-6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres quatro-seis siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting one more verse reference in the back translation than is available
		/// in the vernacular.
		/// The first verse in the vernacular is at start of the paragraph. We also insert
		/// starting at the beginning of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseInsExtra()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. Verses four to six. Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres quatro-seis siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP and attempt to insert verse reference in the middle of the BT string
			// when verse doesn't exist in the vernacular.
			SetupSelForVerseInsert(6, trans, wsBT); // IP at start of "quatro"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We don't expect to see any changes.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres quatro-seis siete ocho", tssResult.Text);

			// Set IP and attempt to insert a verse reference at the end of the BT string
			// that doesn't exist in the vernacular.
			SetupSelForVerseInsert(tssResult.Length, trans, wsBT);
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We don't expect to see any changes.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres quatro-seis siete ocho", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres quatro-seis siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse reference into the back translation when the corresponding
		/// verse has been deleted from the vernacular.
		/// The first verse in the vernacular is at start of the paragraph. We insert into the
		/// middle of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseInsMidVernMissing()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos part b ", null);
			AddRunToMockedTrans(trans, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro. ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "cinco.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(1, trans.Hvo);

			// Set IP and attempt to insert a verse reference in the middle of the BT string
			// that doesn't exist in the vernacular but a subsequent number does exist.
			SetupSelForVerseInsert(6, trans, wsBT);   // IP before "part b"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see "4" inserted at the IP and the following "4"
			// to be deleted.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos 4part b quatro. 5cinco.", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos ", wsBT, null);
			AddRunToStrBldr(strBldr, "4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "part b quatro. ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco.", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating a verse reference in the back translation when the corresponding
		/// verse has been deleted from the vernacular.
		/// The first verse in the vernacular is at start of the paragraph. We update
		/// sequentially through the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdBegSeq()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres. ", null);
			AddRunToMockedTrans(trans, wsBT, "4-5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro. cinco.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Set IP and Insert Verse
			SetupSelForVerseInsert(1, trans, wsBT);   // IP after "2"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see a "3" replace verse num "2" at the IP
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres. 4-5quatro. cinco.", tssResult.Text);

			// Set IP and Insert Verse
			SetupSelForVerseInsert(10, trans, wsBT);   // IP after "4-5"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see a "4" replace verse "4-5" at the IP
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres. 4quatro. cinco.", tssResult.Text);

			// Set IP and Insert Verse
			SetupSelForVerseInsert(20, trans, wsBT);   // IP in "cinco"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see a "5" inserted before "cinco"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("3tres. 4quatro. 5cinco.", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres. ", wsBT, null);
			AddRunToStrBldr(strBldr, "4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro. ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco.", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating a verse reference in the back translation, at the beginning of
		/// BT para, when the verse numbers in the vernacular are smaller.
		/// verse has been deleted from the vernacular.
		/// The first verse in the vernacular is mid-paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdBegSeqSmaller()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "-", null);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres. ", null);
			AddRunToMockedTrans(trans, wsBT, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro. ", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(2, trans.Hvo);

			// Set IP and Insert Verse
			// No-op: attempting to insert verse number at BT start when vern doesn't start
			//   with verse numbers.
			SetupSelForVerseInsert(4, trans, wsBT);   // IP in middle of "tres"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see a "3" replace verse num "5" before "tres"
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("5tres. 6quatro. ", tssResult.Text);

			// Set IP and Insert Verse
			// No-op: previous verse number is greater than corresponding verse number in vern
			SetupSelForVerseInsert(8, trans, wsBT);   // IP after "6"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see a "4" replace verse "6" at the IP
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("5tres. 6quatro. ", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres. ", wsBT, null);
			AddRunToStrBldr(strBldr, "6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro. ", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests updating a verse reference in the back translation when the corresponding
		/// verse has been deleted from the vernacular.
		/// The first verse in the vernacular is at start of the paragraph. We insert into the
		/// middle of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtVerseBtVerseUpdMidVernMissing()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two. three. ", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four. ", null);
			AddRunToMockedPara(parentPara, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "five. ", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "dos. ", null);
			AddRunToMockedTrans(trans, wsBT, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "tres. ", null);
			AddRunToMockedTrans(trans, wsBT, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro. ", null);
			AddRunToMockedTrans(trans, wsBT, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "cinco.", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(2, trans.Hvo);

			// Set IP and attempt to insert a verse reference in the middle of the BT string
			// that doesn't exist in the vernacular but a subsequent number does exist.
			SetupSelForVerseInsert(14, trans, wsBT);   // IP after "4"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect no change since "3" is missing in vernacular.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. 3tres. 4quatro. 5cinco.", tssResult.Text);

			// Set IP and attempt to insert a verse reference in the middle of the BT string
			// that doesn't exist in the vernacular but a subsequent number does exist.
			SetupSelForVerseInsert(7, trans, wsBT);   // IP after "3"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see a "4" replace verse "3" at the IP and the
			// following "4" to be deleted.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("2dos. 4tres. quatro. 5cinco.", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos. ", wsBT, null);
			AddRunToStrBldr(strBldr, "4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres. quatro. ", wsBT, null);
			AddRunToStrBldr(strBldr, "5", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "cinco.", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number into the start of a BT when the vernacular begins
		/// with a chapter and verse number.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VernStrtChapterVerseBtTextInsBeg()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "one. ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "uno dos", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(2, trans.Hvo);

			// Set IP and insert at the start of the BT string
			SetupSelForVerseInsert(2, trans, wsBT); // IP within "uno"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see chapter "1" and verse "1" inserted.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("11uno dos", tssResult.Text);

			// Set IP and insert a verse.
			SetupSelForVerseInsert(6, trans, wsBT); // IP set at start of "dos"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify the result. We expect to see "2" before "dos"
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("11uno 2dos", tssResult.Text);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr, "1", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "uno ", wsBT, null);
			AddRunToStrBldr(strBldr, "2", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting verse numbers in a back translation paragraph. We're inserting
		/// verse numbers from the vernacular that duplicate part of the verse bridges in the
		/// back translation. We must remove duplicate verse numbers from parts of three verse
		/// bridges.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertThenRemoveDupsInBridge()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "two.", null);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "four.", null);
			AddRunToMockedPara(parentPara, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "six.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "- dos ", null);
			AddRunToMockedTrans(trans, wsBT, "1-2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "more tres ", null);
			AddRunToMockedTrans(trans, wsBT, "3-4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "quatro cinco seis ", null);
			AddRunToMockedTrans(trans, wsBT, "5-8", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsBT, "siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Setup and insert first verse number.
			SetupSelForVerseInsert(2, trans, wsBT); //IP before "dos"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see verse "3" at the IchAnchor position, also "1-2" deleted.
			ITsString tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("- 3dos more tres 4quatro cinco seis 5-8siete ocho",
				tssResult.Text);

			// Setup and insert second verse number.
			SetupSelForVerseInsert(12, trans, wsBT); //IP before "tres"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see verse "4" at the IchAnchor position, also "4" deleted.
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("- 3dos more 4tres quatro cinco seis 5-8siete ocho",
				tssResult.Text);

			// Setup and insert third verse number.
			SetupSelForVerseInsert(32, trans, wsBT); //IP before "seis"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// We expect to see verse "5" at the IchAnchor position,
			//   also "5-8" changed to "6-8".
			tssResult = trans.Translation.get_String(wsBT);
			Assert.AreEqual("- 3dos more 4tres quatro cinco 6seis 7-8siete ocho",
				tssResult.Text);

			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "- ", wsBT, null);
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "dos more ", wsBT, null);
			AddRunToStrBldr(strBldr, "4", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres quatro cinco ", wsBT, null);
			AddRunToStrBldr(strBldr, "6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "seis ", wsBT, null);
			AddRunToStrBldr(strBldr, "7-8", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssResult,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion

		#region Test Conversion of plain numbers to verse numbers
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting three consecutive verse references in a back translation paragraph.
		/// We insert beginning at the start of the back translation paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ReplacePlainNumbersWithVerseNumber()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			m_btView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "three.", null);
			AddRunToMockedPara(parentPara, "4-6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verses four to six. ", null);
			AddRunToMockedPara(parentPara, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "Verse seven.", null);

			// Construct the initial back translation
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBT);
			AddRunToMockedTrans(trans, wsBT, "3tres 4-6quatro-seis 8siete ocho", null);

			// set up additional mocks
			SetupSelHelperLevelInfo(parentPara, trans, wsBT, section, book);
			m_sel = SetupMockedBtSelection(3, trans.Hvo);

			// Set IP and Insert first verse number
			SetupSelForVerseInsert(0, trans, wsBT); //set IP at start
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Set IP and Insert second verse number
			SetupSelForVerseInsert(11, trans, wsBT); // set IP within "quatro"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Set IP and Insert third verse reference
			SetupSelForVerseInsert(21, trans, wsBT); // set IP before "siete"
			m_btView.InsertVerseNumber((SelectionHelper)m_selHelper.MockInstance);

			// Verify some of the mock calls
			m_sel.Verify();
			m_rootBox.Verify();

			//  Verify the detailed tss results
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr, "3", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "tres ", wsBT, null);
			AddRunToStrBldr(strBldr, "4-6", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "quatro-seis ", wsBT, null);
			AddRunToStrBldr(strBldr, "7", wsBT, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr, "8siete ocho", wsBT, null);
			ITsString tssExpected = strBldr.GetString();
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected,
				trans.Translation.get_String(wsBT), out difference);
			Assert.IsTrue(fEqual, difference);
		}
		#endregion
	}
}
