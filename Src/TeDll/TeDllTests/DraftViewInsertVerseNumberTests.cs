// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewInsertVerseNumberTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using NMock;
using NMock.Constraints;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
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
	public class InsertVerseNumberTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private DummyDraftView m_draftView;
		private DynamicMock m_styleSheet;
		private DynamicMock m_rootBox;
		private DynamicMock m_vwGraphics;
		private DynamicMock m_selHelper;
		private object[] m_PropInfoArgs;
		private string[] m_PropInfoTypes;
		private object[] m_TextSelInfoArgsAnchor;
		private object[] m_TextSelInfoArgsEnd;
		private string[] m_TextSelInfoTypes;
		private object[] m_LocationArgs;
		private string[] m_LocationTypes;
		private object[] m_AllSelEndInfoArgs;
		private string[] m_AllSelEndInfoTypes;
		private const int kflidParaContent = (int)StTxtPara.StTxtParaTags.kflidContents;
		private const int kflidTrans = (int)CmTranslation.CmTranslationTags.kflidTranslation;
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
			CheckDisposed();
			base.FixtureSetup();

			string intType = "System.Int32";
			string intRef = intType + "&";
			string boolType = typeof(bool).FullName;
			string boolRef = boolType + "&";
			string rectType = "SIL.FieldWorks.Common.Utils.Rect";
			string rectRef = rectType + "&";

			m_PropInfoArgs = new object[] { false, 0, null, null, null, null, null };
			m_PropInfoTypes =
				new string[] { boolType, intType, intRef, intRef, intRef, intRef,
								 "SIL.FieldWorks.Common.COMInterfaces.IVwPropertyStore&" };
			m_TextSelInfoArgsAnchor = new object[] { false, null, null, null, null, null, null };
			m_TextSelInfoArgsEnd = new object[] { true, null, null, null, null, null, null };
			m_TextSelInfoTypes =
				new string[] { boolType, "SIL.FieldWorks.Common.COMInterfaces.ITsString&", intRef,
								 boolRef, intRef, intRef, intRef};

			m_LocationArgs =
				new object[] { new IsAnything(), new IsAnything(), new IsAnything(), null,
								 null, null, null };
			m_LocationTypes =
				new string[] { typeof(IVwGraphics).FullName, rectType, rectType, rectRef,
								 rectRef, boolRef, boolRef };

			m_AllSelEndInfoArgs =
				new object[] { new IsAnything(), null, 3, new IsAnything(), null, null,
								 null, null, null, null };
			m_AllSelEndInfoTypes =
				new string[] { boolType, intRef, intType, "SIL.FieldWorks.Common.COMInterfaces.ArrayPtr",
								 intRef, intRef, intRef, intRef, boolRef,
								 "SIL.FieldWorks.Common.COMInterfaces.ITsTextProps&" };
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			SelectionHelper.s_mockedSelectionHelper = null;
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
				if (m_draftView != null)
					m_draftView.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftView = null;
			m_styleSheet = null;
			m_rootBox = null;
			m_vwGraphics = null;
			m_selHelper = null;
			m_PropInfoArgs = null;
			m_PropInfoTypes = null;
			m_TextSelInfoArgsAnchor = null;
			m_TextSelInfoArgsEnd = null;
			m_TextSelInfoTypes = null;
			m_LocationArgs = null;
			m_LocationTypes = null;
			m_AllSelEndInfoArgs = null;
			m_AllSelEndInfoTypes = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_styleSheet = new DynamicMock(typeof(FwStyleSheet));
			m_styleSheet.Strict = true;

			// Set up a default selection helper. Tests may choose to create a different one.
			m_selHelper = new DynamicMock(typeof(SelectionHelper));
			m_selHelper.Strict = true;
			m_selHelper.SetupResult("NumberOfLevels", 4);
			m_selHelper.SetupResult("RestoreSelectionAndScrollPos", true);
			BTInsertVerseAndFootnoteTests.InitializeVwSelection(m_selHelper);
			SelectionHelper.s_mockedSelectionHelper = (SelectionHelper)m_selHelper.MockInstance;

			m_draftView = new DummyDraftView(Cache, false, 0);
			m_draftView.RootBox = SetupRootBox();
			m_draftView.Graphics = SetupGraphics();
			m_draftView.MakeRoot();
			m_draftView.StyleSheet = (FwStyleSheet)m_styleSheet.MockInstance;
			m_draftView.ActivateView();
			m_draftView.TeEditingHelper.InTestMode = true;

			m_rootBox.Strict = true;

			SelLevInfo[] selLevInfo = new SelLevInfo[4];
			selLevInfo[3].tag = m_draftView.BookFilter.Tag;
			selLevInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			selLevInfo[2].ihvo = 0;
			selLevInfo[0].ihvo = 0;
			selLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			m_selHelper.SetupResult("LevelInfo", selLevInfo);
			m_selHelper.SetupResult("GetLevelInfo", selLevInfo,
				new Type[] { typeof(SelectionHelper.SelLimitType) });
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_draftView.Dispose();
			m_draftView = null;
			m_styleSheet = null;
			m_rootBox = null;
			m_vwGraphics = null;

			base.Exit();
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
			m_rootBox.SetupResult("DataAccess", m_scrInMemoryCache.CacheAccessor);
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
		/// Sets up expected values for simple selection and action handler
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="hvoSection">The hvo of the section.</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="ich">The character offset of the IP in the para.</param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupInsertVerseSelection(StTxtPara para, int hvoSection,
			int hvoBook, int ich)
		{
			return SetupInsertVerseSelection(para, 0, hvoSection, 0, hvoBook, ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for selection and action handler
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="iPara">The index of the para.</param>
		/// <param name="hvoSection">The hvo of the section.</param>
		/// <param name="iSection">The index of the section.</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="ich">The character offset of the IP in the para.</param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupInsertVerseSelection(StTxtPara para, int iPara, int hvoSection,
			int iSection, int hvoBook, int ich)
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
			sel.Strict = true;
			sel.SetupResult("IsValid", true);

			sel.ExpectAndReturn("Commit", true);
			m_rootBox.ExpectAndReturn(3, "Selection", sel.MockInstance);

			m_selHelper.ExpectAndReturn("ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, true);
			m_selHelper.SetupResultForParams("GetTextPropId",
				(int)StText.StTextTags.kflidParagraphs, SelectionHelper.SelLimitType.Anchor);
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });

			ITsPropsBldr builder = TsPropsBldrClass.Create();
			ITsTextProps selProps = builder.GetTextProps();
			m_selHelper.ExpectAndReturn("SelProps", selProps);
			m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, false);
			m_selHelper.Expect("AssocPrev", true);
			m_selHelper.Expect("SetSelection", true);
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });

			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsEnd, m_TextSelInfoTypes,
				new object[] { true, para.Contents.UnderlyingTsString, ich,
							 false, para.Hvo, kflidParaContent, 0 });
			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents.UnderlyingTsString, ich,
							 false, para.Hvo, kflidParaContent, 0 });
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });
			m_selHelper.SetupResultForParams("GetTss", para.Contents.UnderlyingTsString,
				new object[] { SelectionHelper.SelLimitType.Anchor });

			SetupSelection(sel, hvoPara, iPara, hvoSection, iSection, hvoBook);

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for selection and action handler when inserting verse isn't
		/// possible
		/// </summary>
		/// <param name="hvoPara"></param>
		/// <param name="iPara"></param>
		/// <param name="hvoSection"></param>
		/// <param name="iSection"></param>
		/// <param name="hvoBook"></param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupNoInsertVerseSelection(int hvoPara, int iPara, int hvoSection,
			int iSection, int hvoBook)
		{
			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.Strict = true;
			sel.SetupResult("IsValid", true);

			m_selHelper.ExpectAndReturn("ReduceSelectionToIp", m_selHelper.MockInstance,
				SelectionHelper.SelLimitType.Top, false, true);
			m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, false);
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });

			SetupSelection(sel, hvoPara, iPara, hvoSection, iSection, hvoBook);

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up expected calls to action handler
		/// </summary>
		/// <param name="sel">The selection mock</param>
		/// <param name="hvoPara"></param>
		/// <param name="iPara"></param>
		/// <param name="hvoSection"></param>
		/// <param name="iSection"></param>
		/// <param name="hvoBook"></param>
		/// ------------------------------------------------------------------------------------
		private void SetupSelection(DynamicMock sel, int hvoPara, int iPara, int hvoSection,
			int iSection, int hvoBook)
		{
			m_selHelper.SetupResult("Selection", sel.MockInstance);
			SelLevInfo[] info = new SelLevInfo[4];
			info[0] = new SelLevInfo();
			info[0].tag = (int)StText.StTextTags.kflidParagraphs;
			info[0].hvo = hvoPara;
			info[0].ihvo = iPara;
			info[1] = new SelLevInfo();
			info[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			info[1].hvo = Cache.LangProject.Hvo; // hvo of text. Who cares?
			info[2] = new SelLevInfo();
			info[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			info[2].hvo = hvoSection;
			info[2].ihvo = iSection;
			info[3] = new SelLevInfo();
			info[3].tag = m_draftView.BookFilter.Tag;
			info[3].hvo = hvoBook;
			m_selHelper.SetupResult("LevelInfo", info);
			m_selHelper.SetupResult("GetLevelInfo", info,
				new Type[] { typeof(SelectionHelper.SelLimitType)});
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });
		}
		#endregion

		#region General helper functions

		// Helper function appends a run to the given string builder
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
			CheckDisposed();

			ITsPropsBldr builder = TsPropsBldrClass.Create();
			m_selHelper.ExpectAndReturn("SelProps", builder.GetTextProps());

			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.SetupResult("IsValid", true);
			sel.Strict = true;

			m_selHelper.SetupResult("Selection", sel.MockInstance);
			m_selHelper.ExpectAndReturn("ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, true);
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
			CheckDisposed();

			m_selHelper.SetupResult("IchAnchor", 0);

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection introSection = m_scrInMemoryCache.AddIntroSectionToMockedBook(book.Hvo);
			introSection.AdjustReferences();

			IScrSection scriptureSection = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara introPara = SetupParagraph(introSection.Hvo, null, null);
			DynamicMock sel = SetupNoInsertVerseSelection(introPara.Hvo, 0, introSection.Hvo, 0, book.Hvo);
			scriptureSection.AdjustReferences();

			// InsertVerse should not allow a verse number to be inserted.
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// InsertVerse should not allow a verse number to be inserted.
			ITsString tssResult = m_scrInMemoryCache.CacheAccessor.get_StringProp(introPara.Hvo, kflidParaContent);

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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 5);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			// set up a mocked selection with the IP within the first word
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// we expect verse one is inserted at the beginning of the para
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);

			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			SetupParagraph(section1.Hvo, "1", "1");
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section2.Hvo, null, null);
			section2.AdjustReferences();

			// set up the selection within first word in the second section
			DynamicMock sel = SetupInsertVerseSelection(para, 0, section2.Hvo, 1, book.Hvo, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// we expect that verse 2 is inserted at the beginning of the second section
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(38, "Zechariah");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 1
			StTxtPara para = SetupParagraph(section.Hvo, "4", null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(38, "Zechariah");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", "1");
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo,
				kParagraphText.Length + 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(38, "Zechariah");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", "1");
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo,
				kParagraphText.Length + 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(38, "Zechariah");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo,
				kParagraphText.Length + 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", "1");
			section.AdjustReferences();

			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "Another paragraph.", null);

			DynamicMock sel = SetupInsertVerseSelection(para2, 1, section.Hvo, 0, book.Hvo, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para2.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = SetupParagraph(section.Hvo, "1", "1-5");
			section.AdjustReferences();

			// mock up a selection at end of the para
			int len = para.Contents.Text.Length;
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, len);

			m_draftView.InsertVerseNumber();

			// we expect that verse number 6 is inserted at the IP
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Exodus 1 has 22 verses. Mark our text as that last legal verse.
			StTxtPara para = SetupParagraph(section.Hvo, "1", "22");
			section.AdjustReferences();

			// mock up a selection at end of the para
			int len = para.Contents.Text.Length;
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, len);

			m_draftView.InsertVerseNumber();

			// we expect that verse number 23 is NOT inserted
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("122This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(3, tss.RunCount);

			sel.Verify();
			m_rootBox.Verify();
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", "6");
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = SetupParagraph(section.Hvo, "1", "6");
			section.AdjustReferences();

			// mock up a selection in a para at ich 2
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Create a paragraph with two chapters and verse numbers
			StTxtPara para = SetupParagraph(section.Hvo, "1", "6");
			ITsStrBldr strBldr = para.Contents.UnderlyingTsString.GetBldr();
			int bldrLength = strBldr.Length;
			strBldr.Replace(bldrLength, bldrLength, "2",
				StyleUtils.CharStyleTextProps("Chapter Number", m_scrInMemoryCache.Cache.DefaultVernWs));
			bldrLength++;
			strBldr.Replace(bldrLength, bldrLength, "7", StyleUtils.CharStyleTextProps("Verse Number",
				m_scrInMemoryCache.Cache.DefaultVernWs));
			para.Contents.UnderlyingTsString = strBldr.GetString();
			section.AdjustReferences();

			// mock up a selection in a para at ich 7
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 7);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Create a paragraph with chapter 1, verse 1 at end of paragraph
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			ITsStrBldr strBldr = para.Contents.UnderlyingTsString.GetBldr();
			int bldrLength = strBldr.Length;
			strBldr.Replace(bldrLength, bldrLength, "1",
				StyleUtils.CharStyleTextProps("Chapter Number", m_scrInMemoryCache.Cache.DefaultVernWs));
			bldrLength++;
			strBldr.Replace(bldrLength, bldrLength, "1", StyleUtils.CharStyleTextProps("Verse Number",
				m_scrInMemoryCache.Cache.DefaultVernWs));
			para.Contents.UnderlyingTsString = strBldr.GetString();
			section.AdjustReferences();

			// mock up a selection in a para at ich 0
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// We expect that verse 1 be inserted at the IP and chapter 1, verse 1 removed from
			// the end of the paragraph.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Create a paragraph with two chapters and verse numbers
			StTxtPara para = SetupParagraph(section.Hvo, "1", null);
			section.AdjustReferences();

			// mock up a selection in a para at ich 0
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Create a paragraph with two chapters and verse numbers
			StTxtPara para = SetupParagraph(section.Hvo, "1", "1");
			section.AdjustReferences();

			// mock up a selection in a para at ich 0
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 0);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// Create a paragraph with two chapters and verse numbers
			StTxtPara para = SetupParagraph(section.Hvo, "1", "3");
			ITsStrBldr strBldr = para.Contents.UnderlyingTsString.GetBldr();
			strBldr.Replace(29, 29, "2",
				StyleUtils.CharStyleTextProps("Chapter Number", m_scrInMemoryCache.Cache.DefaultVernWs));
			para.Contents.UnderlyingTsString = strBldr.GetString();
			section.AdjustReferences();

			// mock up a selection in a para at ich 29
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("13This is a test paragraph.  42It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			Assert.AreEqual(6, tss.RunCount);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "  ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			// Set up selection between the spaces between verse numbers 2 and 4
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 5);

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be inserted at the IP.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "  ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			// Set up selection between the spaces between verse numbers 2 and 4
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 5);
			sel.ExpectAndReturn("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents.UnderlyingTsString, 6,
								 false, para.Hvo, kflidParaContent, 0 });

			m_draftView.InsertVerseNumber();

			// We expect verse 3 to be updated to 3-4
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			// Set up selection immediately after verse 2
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 4);

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be inserted, which is acceptable.
			// We could instead expect a verse bridge to be created immediately after verse 2,
			//  but that would seem to be a very minor expectation from the user, and not worth
			//  adding complexity to the InsertVerseNumber code.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			// Set up selection immediately after the chapter number 1.
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 1);

			m_draftView.InsertVerseNumber();

			// We expect verse number 1 to be inserted after the chapter number,
			//  WITHOUT a leading space.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "one ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "two ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			// Set up selection just after "two"
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 10);

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be inserted at the IP with a leading space.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "one ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "two ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			// Set up selection just after "two"
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 10);
			sel.ExpectAndReturn("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents.UnderlyingTsString, 11,
								 false, para.Hvo, kflidParaContent, 0 });

			m_draftView.InsertVerseNumber();

			// We expect verse number 3 to be updated to 3-4
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			//Build paragraph one
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "one", null);
			//Build paragraph two
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
			ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "three ", null);
			section.AdjustReferences();

			// Set up selection at beginning of paragraph two
			DynamicMock sel = SetupInsertVerseSelection(para2, 1, section.Hvo, 0, book.Hvo, 0);

			m_draftView.InsertVerseNumber();

			// We expect verse number 2 to be inserted at the beginning of para2,
			//  WITHOUT a leading space.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para2.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, " ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "two ", null);
			section.AdjustReferences();

			// Set up selection at beginning of paragraph
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 0);

			m_draftView.InsertVerseNumber();

			// We expect verse number 1 to be inserted at the beginning of the para,
			//  WITHOUT a leading space.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = SetupParagraph(section.Hvo, "1", "1");
			section.AdjustReferences();

			// add verse number 2 at ich 28
			ITsStrBldr strBldr = para.Contents.UnderlyingTsString.GetBldr();
			strBldr.Replace(28, 29, "2", StyleUtils.CharStyleTextProps(
				ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			para.Contents.UnderlyingTsString = strBldr.GetString();

			// mock up a selection at ich 27
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 27);

			sel.ExpectAndReturn("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents.UnderlyingTsString, 28,
								 false, para.Hvo, kflidParaContent, 0 });

			m_draftView.InsertVerseNumber();

			// We expect verse number 2 to be updated to 2-3
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", "6-7");
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 2);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 0
			StTxtPara para = SetupParagraph(section.Hvo, "1", "6");
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);

			// Use the Insert Verse command to set up the test scenario with a verse number
			// and a verse bridge.
			m_draftView.InsertVerseNumber();
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);
			m_draftView.InsertVerseNumber();
			tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("16This is a test paragraph.  7-8It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			// Now test reducing the verse bridge to a single verse.
			sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			// mock up a selection in a para at ich 29
			StTxtPara para = SetupParagraph(section.Hvo, "1", "6");
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);
			section.AdjustReferences();

			// Use the Insert Verse command to set up the test scenario with a verse number
			m_draftView.InsertVerseNumber();    // Insert verse 7
			sel.Verify();

			sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);
			m_draftView.InsertVerseNumber();    // Update verse 7 to 7-8
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("16This is a test paragraph.  7-8It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			sel.Verify();

			sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);
			m_draftView.InsertVerseNumber();    // Update verses 7-8 to 7-9
			tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("16This is a test paragraph.  7-9It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			// Now test reducing the verse bridge to a single verse.
			sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 6);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 42);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 7);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 38);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, null, null);
			section.AdjustReferences();

			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 52);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			StTxtPara para = SetupParagraph(section.Hvo, "1", "6");
			section.AdjustReferences();

			// Use the Insert Verse command to set up the test scenario with two verse numbers.
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);
			m_draftView.InsertVerseNumber(); //insert verse number 7
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.", tss.Text);
			sel.Verify();

			// Now test "moving" the verse number.
			sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// we expect that verse number 7 is inserted at ich of 7, and later duplicate one is removed
			tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = SetupParagraph(section.Hvo, "1", "6");
			StTxtPara para2 = SetupParagraph(section.Hvo, null, "7");
			section.AdjustReferences();

			// mock up a selection in a para at ich 29
			DynamicMock sel = SetupInsertVerseSelection(para, section.Hvo, book.Hvo, 29);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// Check the paragraph where the verse was inserted.
			ITsString tss = para.Contents.UnderlyingTsString;
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(5, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Check the following paragraph which should have the verse number removed.
			tss = para2.Contents.UnderlyingTsString;
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para_s1 = SetupParagraph(section1.Hvo, "1", "6");
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para_s2 = SetupParagraph(section2.Hvo, null, "7");
			section2.AdjustReferences();

			// mock up a selection in a para at ich 29
			DynamicMock sel = SetupInsertVerseSelection(para_s1, section1.Hvo, book.Hvo, 29);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// Check the paragraph where the verse was inserted.
			ITsString tss = para_s1.Contents.UnderlyingTsString;
			Assert.AreEqual("16This is a test paragraph.  7It has more \"stuff\" in it. \"I wish,\" said John.",
				tss.Text);
			Assert.AreEqual(5, tss.RunCount);
			ITsTextProps ttp = tss.get_Properties(3);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Check the following section which should have the verse number removed.
			tss = para_s2.Contents.UnderlyingTsString;
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_draftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			int hvoSection = section.Hvo;
			StTxtPara para = SetupParagraph(hvoSection, "1", "1");
			section.AdjustReferences();

			// set up our selection
			DynamicMock sel = SetupInsertVerseSelection(para, hvoSection, book.Hvo, 7);
			m_draftView.InsertVerseNumber();
			sel.Verify();

			sel = SetupInsertVerseSelection(para, hvoSection, book.Hvo, 22);
			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();
			m_scrInMemoryCache.MockActionHandler.Verify();
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

	#region InsertVerseNumberRtoLTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for inserting verse numbers in DraftView with a right-to-left writing system.
	/// These tests use mock objects and so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class InsertVerseNumberRtoLTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private DummyDraftView m_draftView;
		private DynamicMock m_styleSheet;
		private DynamicMock m_rootBox;
		private DynamicMock m_vwGraphics;
		private DynamicMock m_selHelper;
		private object[] m_PropInfoArgs;
		private string[] m_PropInfoTypes;
		private object[] m_TextSelInfoArgsAnchor;
		private object[] m_TextSelInfoArgsEnd;
		private string[] m_TextSelInfoTypes;
		private object[] m_LocationArgs;
		private string[] m_LocationTypes;
		private object[] m_AllSelEndInfoArgs;
		private string[] m_AllSelEndInfoTypes;
		private const int kflidParaContent = (int)StTxtPara.StTxtParaTags.kflidContents;
		private const int kflidTrans = (int)CmTranslation.CmTranslationTags.kflidTranslation;

		private string m_StartText = '\u0634' + '\u0677' + '\u0631' + '\u0677' + string.Empty;
		private string m_WordsText = '\u0622' + '\u0644' + '\u0641' + '\u0627' + '\u0632' + string.Empty;

		private int m_wsUrdu;
		private IScrBook m_genesis;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InsertVerseNumberRtoLTests()
		{
		}
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
			CheckDisposed();
			base.FixtureSetup();

			string intType = "System.Int32";
			string intRef = intType + "&";
			string boolType = typeof(bool).FullName;
			string boolRef = boolType + "&";
			string rectType = "SIL.FieldWorks.Common.Utils.Rect";
			string rectRef = rectType + "&";

			m_PropInfoArgs = new object[] { false, 0, null, null, null, null, null };
			m_PropInfoTypes =
				new string[] { boolType, intType, intRef, intRef, intRef, intRef,
								 "SIL.FieldWorks.Common.COMInterfaces.IVwPropertyStore&" };
			m_TextSelInfoArgsAnchor = new object[] { false, null, null, null, null, null, null };
			m_TextSelInfoArgsEnd = new object[] { true, null, null, null, null, null, null };
			m_TextSelInfoTypes =
				new string[] { boolType, "SIL.FieldWorks.Common.COMInterfaces.ITsString&", intRef,
								 boolRef, intRef, intRef, intRef};

			m_LocationArgs =
				new object[] { new IsAnything(), new IsAnything(), new IsAnything(), null,
								 null, null, null };
			m_LocationTypes =
				new string[] { typeof(IVwGraphics).FullName, rectType, rectType, rectRef,
								 rectRef, boolRef, boolRef };

			m_AllSelEndInfoArgs =
				new object[] { new IsAnything(), null, 3, new IsAnything(), null, null,
								 null, null, null, null };
			m_AllSelEndInfoTypes =
				new string[] { boolType, intRef, intType, "SIL.FieldWorks.Common.COMInterfaces.ArrayPtr",
								 intRef, intRef, intRef, intRef, boolRef,
								 "SIL.FieldWorks.Common.COMInterfaces.ITsTextProps&" };
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			SelectionHelper.s_mockedSelectionHelper = null;
		}

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_draftView != null)
					m_draftView.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftView = null;
			m_styleSheet = null;
			m_rootBox = null;
			m_vwGraphics = null;
			m_selHelper = null;
			m_PropInfoArgs = null;
			m_PropInfoTypes = null;
			m_TextSelInfoArgsAnchor = null;
			m_TextSelInfoArgsEnd = null;
			m_TextSelInfoTypes = null;
			m_LocationArgs = null;
			m_LocationTypes = null;
			m_AllSelEndInfoArgs = null;
			m_AllSelEndInfoTypes = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_styleSheet = new DynamicMock(typeof(FwStyleSheet));
			m_styleSheet.Strict = true;

			m_draftView = new DummyDraftView(Cache, false, 0);
			m_draftView.RootBox = SetupRootBox();
			m_draftView.Graphics = SetupGraphics();
			m_draftView.MakeRoot();
			m_draftView.StyleSheet = (FwStyleSheet)m_styleSheet.MockInstance;
			m_draftView.ActivateView();
			m_draftView.TeEditingHelper.InTestMode = true;

			m_rootBox.Strict = true;

			// Set up a default selection helper. Tests may choose to create a different one.
			m_selHelper = new DynamicMock(typeof(SelectionHelper));
			m_selHelper.Strict = true;
			m_selHelper.SetupResult("NumberOfLevels", 4);
			SelectionHelper.s_mockedSelectionHelper = (SelectionHelper)m_selHelper.MockInstance;

			SelLevInfo[] selLevInfo = new SelLevInfo[4];
			selLevInfo[3].tag = m_draftView.BookFilter.Tag;
			selLevInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			selLevInfo[2].ihvo = 0;
			selLevInfo[0].ihvo = 0;
			selLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			m_selHelper.SetupResult("LevelInfo", selLevInfo);
			m_selHelper.SetupResult("GetLevelInfo", selLevInfo,
				new Type[] { typeof(SelectionHelper.SelLimitType) });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data in a right-to-left script.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			// Set the default writing system to a right-to-left writing system (Urdu)
			m_wsUrdu = InMemoryFdoCache.s_wsHvos.Ur;
			m_scrInMemoryCache.ChangeDefaultVernWs(m_wsUrdu);
			LgWritingSystem defWs = new LgWritingSystem(Cache, Cache.DefaultVernWs);

			// Add basic data for Genesis
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(1, m_StartText);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, null);
			m_scrInMemoryCache.AddFormatTextToMockedPara(m_genesis, para,
				@"\v1\*" + m_WordsText, m_wsUrdu);
			section.AdjustReferences();
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
			m_rootBox.SetupResult("DataAccess", m_scrInMemoryCache.CacheAccessor);
			m_rootBox.SetupResult("SetRootObject", null, typeof(int), typeof(IVwViewConstructor),
				typeof(int), typeof(IVwStylesheet));
			m_rootBox.SetupResult("Height", 200); // JT: arbitrary values.
			m_rootBox.SetupResult("Width", 200);
			m_rootBox.SetupResult("Site", m_draftView);
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
			m_vwGraphics = new DynamicMock(typeof(IVwGraphics), "MockIVwGraphics",
				typeof(DummyGraphics));
			m_vwGraphics.AdditionalReferences = new string[] { "TeDllTests.dll" };
			m_vwGraphics.Strict = true;
			m_vwGraphics.SetupResult("XUnitsPerInch", 96);
			m_vwGraphics.SetupResult("YUnitsPerInch", 96);

			return (IVwGraphics)m_vwGraphics.MockInstance;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_draftView.Dispose();
			m_draftView = null;
			m_styleSheet = null;
			m_rootBox = null;
			m_vwGraphics = null;

			base.Exit();
		}
		#endregion

		#region Setup helper functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for selection and action handler
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="iPara">The index of the para.</param>
		/// <param name="hvoSection">The hvo of the section.</param>
		/// <param name="iSection">The index of the section.</param>
		/// <param name="hvoBook">The hvo of the book.</param>
		/// <param name="ich">The character offset of the IP in the para.</param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupInsertVerseSelection(StTxtPara para, int iPara, int hvoSection,
			int iSection, int hvoBook, int ich)
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
			sel.Strict = true;
			sel.SetupResult("IsValid", true);

			sel.ExpectAndReturn("Commit", true);
			m_rootBox.ExpectAndReturn(3, "Selection", sel.MockInstance);

			m_selHelper.ExpectAndReturn("ReduceSelectionToIp",
				m_selHelper.MockInstance, SelectionHelper.SelLimitType.Top, false, true);
			ITsPropsBldr builder = TsPropsBldrClass.Create();
			ITsTextProps selProps = builder.GetTextProps();
			m_selHelper.ExpectAndReturn("SelProps", selProps);
			m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, false);
			m_selHelper.Expect("AssocPrev", true);
			m_selHelper.Expect("SetSelection", true);
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });

			m_selHelper.SetupResultForParams("GetTextPropId",
				(int)StText.StTextTags.kflidParagraphs, SelectionHelper.SelLimitType.Anchor);

			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsEnd, m_TextSelInfoTypes,
				new object[] { true, para.Contents.UnderlyingTsString, ich,
								 false, para.Hvo, kflidParaContent, 0 });
			sel.SetupResultForParams("TextSelInfo", null, m_TextSelInfoArgsAnchor, m_TextSelInfoTypes,
				new object[] { false, para.Contents.UnderlyingTsString, ich,
								 false, para.Hvo, kflidParaContent, 0 });
			SetupSelection(sel, hvoPara, iPara, hvoSection, iSection, hvoBook);
			m_selHelper.SetupResultForParams("GetTss", para.Contents.UnderlyingTsString,
				new object[] { SelectionHelper.SelLimitType.Anchor });

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up expected values for selection and action handler when inserting verse isn't
		/// possible
		/// </summary>
		/// <param name="hvoPara"></param>
		/// <param name="iPara"></param>
		/// <param name="hvoSection"></param>
		/// <param name="iSection"></param>
		/// <param name="hvoBook"></param>
		/// <returns>Mock selection</returns>
		/// ------------------------------------------------------------------------------------
		private DynamicMock SetupNoInsertVerseSelection(int hvoPara, int iPara, int hvoSection,
			int iSection, int hvoBook)
		{
			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.Strict = true;
			sel.SetupResult("IsValid", true);

			m_selHelper.ExpectAndReturn("ReduceSelectionToIp", m_selHelper.MockInstance,
				SelectionHelper.SelLimitType.Top, false, true);
			m_selHelper.ExpectAndReturn("SetSelection", sel.MockInstance, false);
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });

			SetupSelection(sel, hvoPara, iPara, hvoSection, iSection, hvoBook);

			return sel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up expected calls to action handler
		/// </summary>
		/// <param name="sel">The selection mock</param>
		/// <param name="hvoPara"></param>
		/// <param name="iPara"></param>
		/// <param name="hvoSection"></param>
		/// <param name="iSection"></param>
		/// <param name="hvoBook"></param>
		/// ------------------------------------------------------------------------------------
		private void SetupSelection(DynamicMock sel, int hvoPara, int iPara, int hvoSection,
			int iSection, int hvoBook)
		{
			m_selHelper.SetupResult("Selection", sel.MockInstance);
			SelLevInfo[] info = new SelLevInfo[4];
			info[0] = new SelLevInfo();
			info[0].tag = (int)StText.StTextTags.kflidParagraphs;
			info[0].hvo = hvoPara;
			info[0].ihvo = iPara;
			info[1] = new SelLevInfo();
			info[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			info[1].hvo = Cache.LangProject.Hvo; // hvo of text. Who cares?
			info[2] = new SelLevInfo();
			info[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			info[2].hvo = hvoSection;
			info[2].ihvo = iSection;
			info[3] = new SelLevInfo();
			info[3].tag = m_draftView.BookFilter.Tag;
			info[3].hvo = hvoBook;
			m_selHelper.SetupResult("LevelInfo", info);
			m_selHelper.SetupResult("GetLevelInfo", info,
				new Type[] { typeof(SelectionHelper.SelLimitType) });
			m_selHelper.ExpectAndReturn("GetTextPropId", (int)StTxtPara.StTxtParaTags.kflidContents, new object[] { SelectionHelper.SelLimitType.Top });
		}
		#endregion

		#region Tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests verse bridge insertion into the draft view for right to left languages.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void CreateVerseBridge()
		{
			CheckDisposed();

			IScrSection section = new ScrSection(Cache, m_genesis.SectionsOS[0].Hvo);
			StTxtPara para = new StTxtPara(Cache, section.ContentOA.ParagraphsOS[0].Hvo);

			// mock up a selection in a para at ich 1 and insert a verse number
			DynamicMock sel = SetupInsertVerseSelection(para, 0, section.Hvo, 0, m_genesis.Hvo, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// mock up another selection in a para at ich 1 and insert a verse number
			sel = SetupInsertVerseSelection(para, 0, section.Hvo, 0, m_genesis.Hvo, 1);

			m_draftView.InsertVerseNumber();
			sel.Verify();
			m_rootBox.Verify();

			// We expect there to be a right-to-left marker before and after the bridge.
			// Verse 1 should be extended into a "1-3" verse bridge.
			ITsString tss = m_scrInMemoryCache.CacheAccessor.get_StringProp(para.Hvo,
				kflidParaContent);
			Assert.AreEqual(2, tss.RunCount);
			string bridge = m_scr.BridgeForWs(Cache.DefaultVernWs);
			Assert.AreEqual("1" + bridge + "3", tss.get_RunText(0));
			ITsTextProps ttp = tss.get_Properties(0);
			Assert.AreEqual(ScrStyleNames.VerseNumber,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion
	}
	#endregion
}
