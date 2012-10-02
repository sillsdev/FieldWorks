// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2005' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NotesEditingHelperTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NMock;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region DummyNotesEditingHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyNotesEditingHelper : NotesEditingHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public DummyNotesEditingHelper(FdoCache cache, SimpleRootSite rootsite) :
			base(cache, rootsite, null)
		{
		}

		#region Setup selections
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a simulated range selection
		/// </summary>
		/// <param name="hvoPara1"></param>
		/// <param name="hvoPara2"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionForParas(int hvoPara1, int hvoPara2, int ichAnchor, int ichEnd)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 1);
			SelLevInfo[] topInfo = new SelLevInfo[1];
			topInfo[0].tag = StTextTags.kflidParagraphs;
			topInfo[0].hvo = hvoPara1;
			SelLevInfo[] bottomInfo = new SelLevInfo[1];
			bottomInfo[0].tag = StTextTags.kflidParagraphs;
			bottomInfo[0].hvo = hvoPara2;
			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("IchAnchor", ichAnchor);
			fakeSelHelper.SetupResult("IchEnd", ichEnd);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetIch", ichAnchor,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetIch", ichEnd,
				SelectionHelper.SelLimitType.Bottom);
			m_currentSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="para"></param>
		/// <param name="iPara"></param>
		/// <param name="section"></param>
		/// <param name="iSec"></param>
		/// <param name="book"></param>
		/// <param name="iBook"></param>
		/// <param name="setupForHeading"></param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionInPara(IStTxtPara para, int iPara, IScrSection section,
			int iSec, IScrBook book, int iBook, int ich, bool setupForHeading)
		{
			CheckDisposed();

			SetupRangeSelection(book, iBook, section, iSec, para, iPara, ich, setupForHeading,
				book, iBook, section, iSec, para, iPara, ich, setupForHeading);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bookStart"></param>
		/// <param name="iBookStart"></param>
		/// <param name="sectionStart"></param>
		/// <param name="iSecStart"></param>
		/// <param name="paraStart"></param>
		/// <param name="iParaStart"></param>
		/// <param name="ichStart"></param>
		/// <param name="setupForHeadingStart"></param>
		/// <param name="bookEnd"></param>
		/// <param name="iBookEnd"></param>
		/// <param name="sectionEnd"></param>
		/// <param name="iSecEnd"></param>
		/// <param name="paraEnd"></param>
		/// <param name="iParaEnd"></param>
		/// <param name="ichEnd"></param>
		/// <param name="setupForHeadingEnd"></param>
		/// ------------------------------------------------------------------------------------
		public void SetupRangeSelection(IScrBook bookStart, int iBookStart, IScrSection sectionStart,
			int iSecStart, IStTxtPara paraStart, int iParaStart, int ichStart,
			bool setupForHeadingStart, IScrBook bookEnd, int iBookEnd, IScrSection sectionEnd,
			int iSecEnd, IStTxtPara paraEnd, int iParaEnd, int ichEnd, bool setupForHeadingEnd)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 4);
			// Setup the anchor
			SelLevInfo[] topInfo = new SelLevInfo[4];
			topInfo[0].tag = StTextTags.kflidParagraphs;
			topInfo[0].ihvo = iParaStart;
			topInfo[0].hvo = paraStart.Hvo;
			topInfo[1].tag = setupForHeadingStart ? ScrSectionTags.kflidHeading :
				ScrSectionTags.kflidContent;
			topInfo[1].ihvo = 0;
			topInfo[1].hvo = setupForHeadingStart ? sectionStart.HeadingOA.Hvo :
				sectionStart.ContentOA.Hvo;
			topInfo[2].tag = ScrBookTags.kflidSections;
			topInfo[2].ihvo = iSecStart;
			topInfo[2].hvo = sectionStart.Hvo;
			topInfo[3].tag = ScriptureTags.kflidScriptureBooks;
			topInfo[3].ihvo = iBookStart;
			topInfo[3].hvo = bookStart.Hvo;

			// Setup the end
			SelLevInfo[] bottomInfo = new SelLevInfo[4];
			bottomInfo[0].tag = StTextTags.kflidParagraphs;
			bottomInfo[0].ihvo = iParaEnd;
			bottomInfo[0].hvo = paraEnd.Hvo;
			bottomInfo[1].tag = setupForHeadingEnd ? ScrSectionTags.kflidHeading :
				ScrSectionTags.kflidContent;
			bottomInfo[1].ihvo = 0;
			bottomInfo[1].hvo = setupForHeadingEnd ? sectionEnd.HeadingOA.Hvo :
				sectionEnd.ContentOA.Hvo;
			bottomInfo[2].tag = ScrBookTags.kflidSections;
			bottomInfo[2].ihvo = iSecEnd;
			bottomInfo[2].hvo = sectionEnd.Hvo;
			bottomInfo[3].tag = ScriptureTags.kflidScriptureBooks;
			bottomInfo[3].ihvo = iBookEnd;
			bottomInfo[3].hvo = bookEnd.Hvo;

			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("IchAnchor", ichStart);
			fakeSelHelper.SetupResult("IchEnd", ichEnd);
			fakeSelHelper.SetupResult("IsValid", true);
			fakeSelHelper.SetupResult("AssocPrev", false);
			fakeSelHelper.Ignore("get_IsRange");
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
		///
		/// </summary>
		/// <param name="para"></param>
		/// <param name="iPara"></param>
		/// <param name="book"></param>
		/// <param name="iBook"></param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionInTitlePara(IStTxtPara para, int iPara, IScrBook book, int iBook,
			int ich)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 3);
			// Setup the anchor
			SelLevInfo[] topInfo = new SelLevInfo[3];
			topInfo[0].tag = StTextTags.kflidParagraphs;
			topInfo[0].ihvo = iPara;
			topInfo[0].hvo = para.Hvo;
			topInfo[1].tag = ScrBookTags.kflidTitle;
			topInfo[1].ihvo = 0;
			topInfo[1].hvo = book.TitleOA.Hvo;
			topInfo[2].tag = ScriptureTags.kflidScriptureBooks;
			topInfo[2].ihvo = iBook;
			topInfo[2].hvo = book.Hvo;

			// Setup the end
			SelLevInfo[] bottomInfo = new SelLevInfo[3];
			for(int i = 0; i < 3; i++)
				bottomInfo[i] = topInfo[i];

			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("IchAnchor", ich);
			fakeSelHelper.SetupResult("IchEnd", ich);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetIch", ich,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetIch", ich,
				SelectionHelper.SelLimitType.Bottom);
			m_currentSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a simulated selection anywhere in the given section (Does NOT set para and
		/// character info)
		/// </summary>
		/// <param name="hvoSection">The selection</param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionForSection(int hvoSection)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 1);
			SelLevInfo[] topInfo = new SelLevInfo[1];
			topInfo[0].tag = ScrBookTags.kflidSections;
			topInfo[0].hvo = hvoSection;
			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("GetLevelInfo", topInfo, typeof(SelectionHelper.SelLimitType));
			m_currentSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a simulated selection anywhere in the given title (Does NOT set para and
		/// character info)
		/// </summary>
		/// <param name="hvoTitle">The StText of the book title</param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionForTitle(int hvoTitle)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 1);
			SelLevInfo[] topInfo = new SelLevInfo[1];
			topInfo[0].tag = ScrBookTags.kflidTitle;
			topInfo[0].hvo = hvoTitle;
			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("GetLevelInfo", topInfo, typeof(SelectionHelper.SelLimitType));
			m_currentSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// NotesEditingHelperTests are nice.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class NotesEditingHelperTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private DummyNotesEditingHelper m_notesEditingHelper;
		#endregion

		#region Setup and teardown methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a collection of annotations for Genesis (in a real DB, this would be done for
		/// all 66 books in TeScrInitializer).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			IScrBookAnnotations notes = Cache.ServiceLocator.GetInstance<IScrBookAnnotationsFactory>().Create();
			m_scr.BookAnnotationsOS.Add(notes);
			m_notesEditingHelper = new DummyNotesEditingHelper(Cache, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{

			m_notesEditingHelper.Dispose();
			m_notesEditingHelper = null;

			base.TestTearDown();
		}
		#endregion

		#region Insert annotation tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the InsertNote method with a range selection in an intro paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_RangeSelInIntroPara()
		{
			IScrBook philemon = AddBookToMockedScripture(57, "Philemon");
			IScrSection section = AddSectionToMockedBook(philemon, true);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "This is the paragraph", null);

			CreateAndCheckAnnotation(para, para, 57001000, 57001000, 0, 3, "Thi");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the InsertNote method with a simple IP in a verse paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_IpSelInVersePara()
		{
			IScrBook philemon = AddBookToMockedScripture(57, "Philemon");
			IScrSection section = AddSectionToMockedBook(philemon);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is text", null);

			CreateAndCheckAnnotation(para, para, 57001003, 57001003, 4, 4, string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the InsertNote method with a range selection in a verse paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_RangeSelInVersePara()
		{
			IScrBook philemon = AddBookToMockedScripture(57, "Philemon");
			IScrSection section = AddSectionToMockedBook(philemon);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is text", null);

			CreateAndCheckAnnotation(para, para, 57001001, 57001003, 7, 35,
				"is text2This is text3This is");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the InsertNote method with a range selection that covers multiple verse
		/// paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_RangeSelInMultiVersePara()
		{
			IScrBook philemon = AddBookToMockedScripture(57, "Philemon");
			IScrSection section = AddSectionToMockedBook(philemon);
			IStTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is text", null);
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is text", null);
			IStTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is text", null);

			CreateAndCheckAnnotation(para1, para2, 57001001, 57001003, 0, 0, string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.InsertNote from both the vernacular and back translation.
		/// TE-8080.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_FromBtAndVern()
		{
			// Create vernacular Scripture data.
			IScrBook philemon = AddBookToMockedScripture(57, "Philemon");
			IScrSection section = AddSectionToMockedBook(philemon);
			IStTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is text", null);
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is text", null);
			IStTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is text", null);

			// Create back translation paras.
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans1 = AddBtToMockedParagraph(para1, wsBt);
			AddRunToMockedTrans(trans1, wsBt, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans1, wsBt, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans1, wsBt, "Verse one BT. ", null);
			AddRunToMockedTrans(trans1, wsBt, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans1, wsBt, "Verse two BT. ", null);
			AddBtToMockedParagraph(para2, wsBt);
			AddRunToMockedTrans(trans1, wsBt, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans1, wsBt, "Verse three BT. ", null);

			// Add annotations at beginning and end of vernacular text.
			CreateAndCheckAnnotation(para1, para1, 57001001, 57001001, 2, 6, "This");
			CreateAndCheckAnnotation(para2, para2, 57001003, 57001003, 6, 8, "is");
			// Add an annotation in verse 2 of the back translation.
			CreateAndCheckAnnotation(trans1, trans1, 57001002, 57001002, 23, 26, "two");

			// Confirm that the notes are in the expected order.
			Assert.AreEqual(para1, m_scr.BookAnnotationsOS[56].NotesOS[0].BeginObjectRA,
			 "The first note should be on the first para of the vernacular.");
			Assert.AreEqual(trans1, m_scr.BookAnnotationsOS[56].NotesOS[1].BeginObjectRA,
			 "The second note should be on the translation for the first para of the vernacular.");
			Assert.AreEqual(para2, m_scr.BookAnnotationsOS[56].NotesOS[2].BeginObjectRA,
			 "The third note should be on the third para of the vernacular.");
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create and Check an Annotation
		/// </summary>
		/// <param name="hvoParaStart">The hvo para start.</param>
		/// <param name="hvoParaEnd">The hvo para end.</param>
		/// <param name="startRef">The start ref.</param>
		/// <param name="endRef">The end ref.</param>
		/// <param name="startOffset">The start offset.</param>
		/// <param name="endOffset">The end offset.</param>
		/// <param name="tssQuote">The text in the quote.</param>
		/// <param name="sel">selection in main window</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrScriptureNote CreateAndCheckAnnotation(ICmObject topObj, ICmObject bottomObj,
			int startRef, int endRef, int startOffset, int endOffset, string strQuote)
		{
			ILangProject lp = Cache.LangProject;
			ICmAnnotationDefn transNoteAnnDefn =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().TranslatorAnnotationDefn;

			// Get information from the selection about the location of the annotation.
			ITsStrBldr tssBldrQuote = TsStrBldrClass.Create();
			tssBldrQuote.Replace(0, 0, strQuote, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));

			IScrScriptureNote ann =
				m_notesEditingHelper.InsertNote(transNoteAnnDefn, startRef, endRef,
					topObj, bottomObj, startOffset, endOffset, tssBldrQuote.GetString());
			Assert.IsNotNull(ann);
			Assert.AreEqual(transNoteAnnDefn, ann.AnnotationTypeRA, "Wrong type of note created");
			Assert.AreEqual(topObj, ann.BeginObjectRA, "Wrong paragraph annotated");
			Assert.AreEqual(bottomObj, ann.EndObjectRA, "Wrong paragraph annotated");
			Assert.AreEqual(startRef, ann.BeginRef, "Should have the correct start para reference");
			Assert.AreEqual(endRef, ann.EndRef, "Should have the correct end para reference");
			Assert.AreEqual(startOffset, ann.BeginOffset, "Should have the correct starting char offset");
			Assert.AreEqual(endOffset, ann.EndOffset, "Should have the correct ending char offset");
			Assert.AreEqual(lp.DefaultUserAgent, ann.SourceRA, "Wrong agent");
			VerifyInitializedText(ann.DiscussionOA, "Discussion");
			Assert.AreEqual(tssBldrQuote.Text, ((IStTxtPara)ann.QuoteOA.ParagraphsOS[0]).Contents.Text);
			VerifyInitializedText(ann.RecommendationOA, "Recommendation");
			VerifyInitializedText(ann.ResolutionOA, "Resolution");
			return ann;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that StText field for an annotation was initialized properly.
		/// </summary>
		/// <param name="text">given StText</param>
		/// <param name="fieldName">name of field</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyInitializedText(IStText text, string fieldName)
		{
			Assert.AreEqual(1, text.ParagraphsOS.Count, fieldName + " should have 1 empty para");
			IStTxtPara para = (IStTxtPara)text.ParagraphsOS[0];
			Assert.IsNotNull(para.StyleRules, fieldName + " should have a para style.");
			Assert.AreEqual(ScrStyleNames.Remark,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				fieldName + " should use Remark style.");
			Assert.IsNull(para.Contents.Text, fieldName + " should have 1 empty para.");
		}
		#endregion
	}
}
