// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeXmlImportTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.TE.TeEditorialChecks;

namespace SIL.FieldWorks.TE.ImportTests
{
	#region DummyTeXmlImporter
	/// <summary>
	///
	/// </summary>
	internal class DummyTeXmlImporter : TeXmlImporter
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyTeXmlImporter"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="styleSheet"></param>
		/// <param name="sOXESFile">name of the XML (OXES) file</param>
		/// <param name="undoManager"></param>
		/// <param name="importCallbacks"></param>
		/// -----------------------------------------------------------------------------------
		public DummyTeXmlImporter(FdoCache cache, FwStyleSheet styleSheet, string sOXESFile,
			UndoImportManager undoManager, TeImportUi importCallbacks) :
			base(cache, styleSheet, sOXESFile, undoManager, importCallbacks)
		{

		}
	}
	#endregion

	#region TE Import Tests (in-memory cache)
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// TeXmlImportTest tests TeXmlImport using in-memory cache
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class TeXmlImportTests : TeTestBase
	{
		#region Member variables
		/// <summary></summary>
		protected TeXmlImporter m_importer;
		/// <summary>Stylesheet</summary>
		protected FwStyleSheet m_styleSheet;
		/// <summary>This undo import manager may not be fully initialized for all purposes</summary>
		protected UndoImportManager m_undoImportManager;
		/// <summary>ID of vernacular writing system</summary>
		protected int m_wsVern;
		/// <summary>ID of analysis writing system</summary>
		protected int m_wsAnal;
		/// <summary>class to prevent UI during tests</summary>
		protected TeImportNoUi m_teImportUi;
		/// <summary>The "m_stackSectionType" member of the m_importer (gotten by reflection)</summary>
		Stack<string> m_stackSectionType;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup - define Scripture Check IDs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_teImportUi = new TeImportNoUi();
			// force scripture check definitions to be created - don't want this done as
			// part of the normal undoable work.
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				InstalledScriptureChecks.GetChecks(new ScrChecksDataSource(m_scr.Cache));
			});
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			m_teImportUi.Dispose();
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the importer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			InitWsInfo();
			m_actionHandler.EndUndoTask(); // Import expects to handle undo

			m_undoImportManager = new UndoImportManager(Cache);
			m_undoImportManager.StartImportingFiles();
			m_importer = new DummyTeXmlImporter(Cache, m_styleSheet, " ", m_undoImportManager,
				m_teImportUi);

			ReflectionHelper.CallMethod(m_importer, "Initialize", null);
			m_stackSectionType = ReflectionHelper.GetField(m_importer, "m_stackSectionType") as Stack<string>;

			StyleProxyListManager.Initialize(m_styleSheet);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_undoImportManager.DoneImportingFiles(true);
			m_importer.Dispose();
			m_importer = null;
			m_styleSheet = null;
			StyleProxyListManager.Cleanup();
			ScrNoteImportManager.Cleanup();
			m_actionHandler.BeginUndoTask("bogus", "bogus");
			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init writing system info and some props needed by some tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void InitWsInfo()
		{
			Debug.Assert(Cache != null);

			// get writing system info needed by tests
			m_wsVern = Cache.DefaultVernWs;
			m_wsAnal = Cache.DefaultAnalWs;
		}
		#endregion

		#region ProcessParagraphNode tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a normal paragraph node to ProcessParagraphNode, and verify the results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_Basic()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(sect, "This is My Head", ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<p><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.1\" n=\"1\" /><trGroup><tr>Nice book!</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, "Nice book!", null, m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node that contains a figure to ProcessParagraphNode, and verify
		/// the results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_WithFigure()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(sect, "This is My Head", ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<p><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.1\" n=\"1\" /><trGroup><tr>Nice book!" +
				"<figure src=\"filename.jpg\" oxesRef=\"GEN\" alt=\"photo picture caption (Genesis)\">" +
				"<!--path=\"C:\\notThere\\filename.jpg\"-->" +
				"<caption><trGroup><tr><foreign xml:lang=\"de\">photo</foreign>" +
				" picture caption</tr></trGroup></caption></figure>" +
				"</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.SetField(m_importer, "m_sOXESDir", ".");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(4, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, "Nice book!", null, m_wsVern);
			FwObjDataTypes odt;
			Guid picGuid = TsStringUtils.GetGuidFromProps(para.Contents.get_Properties(3), null, out odt);
			Assert.AreEqual(FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
			ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(picGuid);
			ITsString captionTss = picture.Caption.BestVernacularAlternative;
			Assert.AreEqual(2, captionTss.RunCount);
			AssertEx.RunIsCorrect(captionTss, 0, "photo", null, m_wsDe);
			AssertEx.RunIsCorrect(captionTss, 1, " picture caption", null, Cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node to ProcessParagraphNode, and verify the results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_WithLabelTrElement()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(sect, "This is My Head", ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<p><labelTr> </labelTr><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<labelTr><foreign xml:lang=\"de\"> </foreign></labelTr><verseStart ID=\"JUD.1.1\" n=\"1\" />" +
				"<labelTr> <emphasis> </emphasis></labelTr><verseEnd ID=\"JUD.1.1\" />" +
				"<verseStart ID=\"JUD.1.2\" n=\"2\" /><labelTr> </labelTr></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(8, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, " ", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", ScrStyleNames.ChapterNumber, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, " ", null, m_wsDe);
			AssertEx.RunIsCorrect(para.Contents, 3, "1", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 4, " ", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 5, " ", ScrStyleNames.Emphasis, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 6, "2", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 7, " ", null, m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node to ProcessParagraphNode that contains a back translation where
		/// the footnotes are in a different order than in the vernacular because of grammatical
		/// word order differences between the vernacular and analysis languages. Verify the
		/// results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_BtFootnotesInDifferentOrder()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(sect, "This is My Head", ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<p><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.1\" n=\"1\" /><trGroup><tr>Me dijo" +
				"<note noteID=\"JUD1\" type=\"general\" canonical=\"true\">" +
				"<trGroup><tr>informo</tr><bt>inform</bt></trGroup></note> mi mama" +
				"<note noteID=\"JUD2\"><trGroup><tr>madre</tr>" +
				"<bt xml:lang=\"en\">mother</bt></trGroup></note> que si.</tr>" +
				"<bt xml:lang=\"en\" segmented=\"true\">My mom<note noteRef=\"JUD2\" /> " +
				"told<note noteRef=\"JUD1\" /> me that yes.</bt></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			// Verify contents of the vernacular paragraph.
			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(7, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, "Me dijo", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 4, " mi mama", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 6, " que si.", null, m_wsVern);
			VerifyFootnote(book.FootnotesOS[0], para, "11Me dijo".Length);
			VerifyFootnote(book.FootnotesOS[1], para, "11Me dijoX mi mama".Length); // X represents position of prior footnote
			// Verify contents of the paragraph's back translation.
			Assert.IsNotNull(para.GetBT(), "Back translation of paragraph should not be null.");
			ITsString tssTrans = para.GetBT().Translation.get_String(m_wsAnal);
			Assert.IsNotNull(tssTrans);
			Assert.AreEqual(7, tssTrans.RunCount);
			AssertEx.RunIsCorrect(tssTrans, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 2, "My mom", null, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 4, " told", null, m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 6, " me that yes.", null, m_wsAnal);
			// Verify second footnote (which should be the first caller in the back translation)
			FdoTestHelper.VerifyBtFootnote(book.FootnotesOS[1], para, m_wsAnal, "11My mom".Length);
			ICmTranslation transFootnote = ((IStTxtPara)book.FootnotesOS[1].ParagraphsOS[0]).GetBT();
			Assert.IsNotNull(transFootnote, "Second footnote should have a BT");
			ITsString tssFootnote2Bt = transFootnote.Translation.get_String(m_wsAnal);
			AssertEx.RunIsCorrect(tssFootnote2Bt, 0, "mother", null, m_wsAnal);
			// Verify first footnote (which should be the second caller in the back translation)
			FdoTestHelper.VerifyBtFootnote(book.FootnotesOS[0], para, m_wsAnal, "11My momX told".Length); // X represents position of prior footnote
			transFootnote = ((IStTxtPara)book.FootnotesOS[0].ParagraphsOS[0]).GetBT();
			Assert.IsNotNull(transFootnote, "First footnote should have a BT");
			ITsString tssFootnote1Bt = transFootnote.Translation.get_String(m_wsAnal);
			AssertEx.RunIsCorrect(tssFootnote1Bt, 0, "inform", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node to ProcessParagraphNode when the first segment does not have
		/// a translation. Verify the results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_UntranslatedSegments()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(sect, "This is My Head", ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<p><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.1\" n=\"1\" /><trGroup><tr>Yih ek jumla he. </tr></trGroup>" +
				"<trGroup><tr>Is jumla xubsurit he. </tr><bt xml:lang=\"en\" segmented=\"true\">" +
				"This sentence is beautiful. </bt></trGroup>" +
				"<trGroup><tr>Is jumle dekho. </tr></trGroup>" +
				"<verseEnd ID=\"JUD.1.1\" /><chapterEnd ID=\"JUD.1\" /></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			// Verify contents of the vernacular paragraph.
			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, "Yih ek jumla he. Is jumla xubsurit he. " +
				"Is jumle dekho. ", null, m_wsVern);
			// Verify contents of the paragraph's back translation.
			Assert.IsNotNull(para.GetBT(), "Back translation of paragraph should not be null.");
			ITsString tssTrans = para.GetBT().Translation.get_String(m_wsAnal);
			Assert.IsNotNull(tssTrans);
			Assert.AreEqual(3, tssTrans.RunCount);
			AssertEx.RunIsCorrect(tssTrans, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssTrans, 2, "This sentence is beautiful. ", null, m_wsAnal);
			// Verify the segments.
			Assert.AreEqual(4, para.SegmentsOS.Count);
			Assert.AreEqual("11", para.SegmentsOS[0].BaselineText.Text);
			Assert.IsNull(para.SegmentsOS[0].FreeTranslation.get_String(m_wsAnal).Text);
			Assert.AreEqual("Yih ek jumla he. ", para.SegmentsOS[1].BaselineText.Text);
			Assert.IsNull(para.SegmentsOS[1].FreeTranslation.get_String(m_wsAnal).Text);
			Assert.AreEqual("Is jumla xubsurit he. ", para.SegmentsOS[2].BaselineText.Text);
			Assert.AreEqual("This sentence is beautiful. ",
				para.SegmentsOS[2].FreeTranslation.get_String(m_wsAnal).Text);
			Assert.AreEqual("Is jumle dekho. ", para.SegmentsOS[3].BaselineText.Text);
			Assert.IsNull(para.SegmentsOS[3].FreeTranslation.get_String(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a body text (i.e., Scripture) paragraph node with a custom style to
		/// ProcessParagraphNode, and verify the results.
		/// TE-7123
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_CustomizedBodyTextStyle()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(sect, "This is My Head",
				ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<p type=\"userPS\" subType=\"Custom user style\">" +
				"<chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.1\" n=\"1\" /><trGroup><tr>Nice book!</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Custom user style"), para.StyleRules);
			IStStyle newStyle = m_styleSheet.FindStyle("Custom user style");
			Assert.AreEqual(ContextValues.Text, newStyle.Context);
			Assert.AreEqual(StructureValues.Body, newStyle.Structure);
			Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
			Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.NormalParagraph), newStyle.BasedOnRA);
			Assert.AreEqual(newStyle, newStyle.NextRA);
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, "Nice book!", null, m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send an intro paragraph node with a custom style to ProcessParagraphNode, and
		/// verify the results.
		/// TE-7123
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_CustomizedIntroParaStyle()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book, true);
			AddSectionHeadParaToSection(sect, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", true);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = "<p type=\"userPS\" subType=\"Custom intro style\"><trGroup>" +
				"<tr>Read this book.</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Custom intro style"), para.StyleRules);
			IStStyle newStyle = m_styleSheet.FindStyle("Custom intro style");
			Assert.AreEqual(ContextValues.Intro, newStyle.Context);
			Assert.AreEqual(StructureValues.Body, newStyle.Structure);
			Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
			Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.IntroParagraph), newStyle.BasedOnRA);
			Assert.AreEqual(newStyle, newStyle.NextRA);
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "Read this book.", null, m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node with a character style with a nested writing system to
		/// ProcessParagraphNode, and verify the results.
		/// TE-8524
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_CharacterStyle_NestedWs()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book, true);
			AddSectionHeadParaToSection(sect, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = "<p><trGroup><tr>This is actually useful text according to Tim" +
				"<emphasis><foreign xml:lang=\"fr\">Ah, oui!</foreign></emphasis></tr></trGroup>" +
				"<trGroup><tr><foreign><foreign xml:lang=\"fr\">Ah, non!</foreign></foreign>" +
				"</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "This is actually useful text according to Tim", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "Ah, oui!", ScrStyleNames.Emphasis,
				Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("fr"));
			AssertEx.RunIsCorrect(para.Contents, 2, "Ah, non!", ScrStyleNames.Foreign,
				Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("fr"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node with a custom character style with a nested writing system to
		/// ProcessParagraphNode, and verify the results.
		/// TE-8524
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_CustomCharacterStyle_NestedWs()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book, true);
			AddSectionHeadParaToSection(sect, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = "<p><trGroup><tr>This is actually useful text according to Tim" +
				"<userCS type=\"Commentary of Joel\"><foreign xml:lang=\"fr\">Ah, oui!</foreign>" +
				"</userCS></tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(2, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "This is actually useful text according to Tim", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "Ah, oui!", "Commentary of Joel",
				Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("fr"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node with a character style with a nested writing system to
		/// ProcessParagraphNode, and verify the results.
		/// TE-8524
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_WithLf()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book, true);
			AddSectionHeadParaToSection(sect, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = string.Format("<p><trGroup><tr>This is actually useful text according to Tim.{0}{0}</tr></trGroup>" +
				"<trGroup><tr>This might dissappear though</tr></trGroup></p>", Environment.NewLine);
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0,
				"This is actually useful text according to Tim.This might dissappear though", null, m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a paragraph node with a character style with a nested writing system to
		/// ProcessParagraphNode, and verify the results.
		/// TE-8524
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_WithInvalidXmlChars()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book, true);
			AddSectionHeadParaToSection(sect, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = "<p><trGroup><tr>This is useful? &#x2;&#x5;&#x3;&#xA;&#xD;&#xE;&#xFFFF;&#xFFFE;</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0,
				"This is useful? ", null, m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a "line" node with "citation" type to ProcessParagraphNode, and verify that
		/// it matches to the appropriate line.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessParagraphNode_MatchCitationLineXToLineX()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			IScrSection sect = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(sect, "This is My Head", ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<l level=\"2\" type=\"citation\"><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.1\" n=\"1\" /><trGroup><tr>Should come out as Line 2.</tr></trGroup></l>";
			XmlNode paraNode = doc.SelectSingleNode("l");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.Line2), para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, "Should come out as Line 2.", null, m_wsVern);
		}

		#endregion

		#region ProcessBookSection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a reader that is ready to read a normal Scripture section node to
		/// ProcessBookSection, and verify the results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessBookSection_Basic()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", 65);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(65, 1, 1));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<section><sectionHead><trGroup><tr>Warnings and Instructions</tr>" +
				"</trGroup></sectionHead>" +
				"<p><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.2\" n=\"2\" /><trGroup><tr>Nice book!</tr></trGroup></p>" +
				"</section>";
			using (XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section")))
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

				// Verify the section head
				Assert.AreEqual(1, book.SectionsOS.Count);
				IScrSection sect = book.SectionsOS[0];
				Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
				Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead), para.StyleRules);
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Warnings and Instructions", null, m_wsVern);

				Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
				Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
				Assert.AreEqual(3, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
				AssertEx.RunIsCorrect(para.Contents, 1, "2", "Verse Number", m_wsVern);
				AssertEx.RunIsCorrect(para.Contents, 2, "Nice book!", null, m_wsVern);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a reader that is ready to read an introduction section node to
		/// ProcessBookSection, and verify the results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessBookSection_IntroSection()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", true);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", 65);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(65, 1, 0));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<section><sectionHead><trGroup><tr>Intro section head</tr>" +
				"</trGroup></sectionHead>" +
				"<p><trGroup><tr>Nice book!</tr></trGroup></p>" +
				"</section>";
			using (XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section")))
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

				// Verify the section head
				Assert.AreEqual(1, book.SectionsOS.Count);
				IScrSection sect = book.SectionsOS[0];
				Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
				Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroSectionHead), para.StyleRules);
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Intro section head", null, m_wsVern);

				Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
				Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph), para.StyleRules);
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Nice book!", null, m_wsVern);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a reader that is ready to read a Scripture section node with a user-created
		/// Section Head style to ProcessBookSection, and verify the results.
		/// TE-7123, TE-7271
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessBookSection_UserCreatedScrStyle()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", 65);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(65, 1, 1));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<section><sectionHead type=\"userDefined\" subType=\"Moderately Major Head\">" +
				"<trGroup><tr>Warnings and Instructions</tr>" +
				"</trGroup></sectionHead>" +
				"<p><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.2\" n=\"2\" /><trGroup><tr>Nice book!</tr></trGroup></p>" +
				"</section>";
			using (XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section")))
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

				// Verify the section head
				Assert.AreEqual(1, book.SectionsOS.Count);
				IScrSection sect = book.SectionsOS[0];
				Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
				Assert.AreEqual("Moderately Major Head", para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Warnings and Instructions", null, m_wsVern);

				IStStyle newStyle = m_styleSheet.FindStyle("Moderately Major Head");
				Assert.AreEqual(ContextValues.Text, newStyle.Context);
				Assert.AreEqual(StructureValues.Heading, newStyle.Structure);
				Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
				Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.SectionHead), newStyle.BasedOnRA);
				Assert.AreEqual(newStyle, newStyle.NextRA);

				Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
				Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
				Assert.AreEqual(3, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "1", "Chapter Number", m_wsVern);
				AssertEx.RunIsCorrect(para.Contents, 1, "2", "Verse Number", m_wsVern);
				AssertEx.RunIsCorrect(para.Contents, 2, "Nice book!", null, m_wsVern);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a reader that is ready to read a Scripture section node with a footnote that
		/// is not General or CrossReference to ProcessBookSection. TE-7269.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessBookSection_NonStandardFootnote()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", 65);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(65, 1, 16));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<section><sectionHead><trGroup><tr>This is just one</tr></trGroup>" +
				"</sectionHead><p><chapterStart ID=\"JUD.1\" n=\"1\" /><verseStart ID=\"JUD.1.1\" n=\"1\" />" +
				"<trGroup><tr><note noteID=\"JUD1\" type=\"speaker\" canonical=\"true\"><trGroup>" +
				"<tr>He said so!</tr></trGroup></note> From Jude, servant of Jesus Christ</tr></trGroup></p></section>";

			using (XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section")))
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

				// Verify the footnote
				Assert.AreEqual(1, book.FootnotesOS.Count);
				IScrFootnote footnote = book.FootnotesOS[0];
				Assert.AreEqual(1, footnote.ParagraphsOS.Count);
				IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
				Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph,
					footnotePara.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				AssertEx.RunIsCorrect(footnotePara.Contents, 0, "He said so!", null, m_wsVern);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a reader that is ready to read an inroduction section node with a user-created
		/// Intro Section Head style to ProcessBookSection, and verify the results.
		/// TE-7123, TE-7270
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessBookSection_UserCreatedIntroStyle()
		{
			IScrBook book = AddBookToMockedScripture(65, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", true);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", 65);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(65, 1, 0));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<introduction><sectionHead type=\"userDefined\" subType=\"Moderately Major Intro Head\">" +
				"<trGroup><tr>Before You Read this Book...</tr>" +
				"</trGroup></sectionHead>" +
				"<p><trGroup><tr>Nice book!</tr></trGroup></p>" +
				"</introduction>";
			using (XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("introduction")))
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

				// Verify the section head
				Assert.AreEqual(1, book.SectionsOS.Count);
				IScrSection sect = book.SectionsOS[0];
				Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
				Assert.AreEqual("Moderately Major Intro Head", para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Before You Read this Book...", null, m_wsVern);

				IStStyle newStyle = m_styleSheet.FindStyle("Moderately Major Intro Head");
				Assert.AreEqual(ContextValues.Intro, newStyle.Context);
				Assert.AreEqual(StructureValues.Heading, newStyle.Structure);
				Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
				Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.IntroSectionHead), newStyle.BasedOnRA);
				Assert.AreEqual(newStyle, newStyle.NextRA);

				Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
				Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph), para.StyleRules);
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Nice book!", null, m_wsVern);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ProcessBookSection with a Scripture section that has an annotation with a
		/// character style in it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessBookSection_AnnotationInScriptureWithCharStyle()
		{
			const int nJud = 65;
			ScrNoteImportManager.Initialize(m_scr, nJud);
			IScrBook book = AddBookToMockedScripture(nJud, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", nJud);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(nJud, 1, 1));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<section><sectionHead><trGroup><tr>My section head</tr>" +
				"</trGroup></sectionHead>" +
				"<p><verseStart ID=\"JUD.1.1\" n=\"1\" />" +
				"<trGroup><tr>This is the only time I'm going to write to you, so listen up!</tr></trGroup>" +
				"<verseEnd ID=\"JUD.1.1\" />" +
				"<annotation type=\"translatorNote\" status=\"1\" oxesRef=\"JUD.1.1\">" +
				"<created>2008-11-18 19:17:50.14</created>" +
				"<modified>2008-11-18 19:20:23.73</modified>" +
				"<resolved>2008-11-18 19:17:50.14</resolved>" +
				"<notationDiscussion><para xml:lang=\"en\">" +
				"<span>Go </span>" +
				"<span type=\"" + ScrStyleNames.Emphasis + "\">here</span>" +
				"<span>!</span>" +
				"</para></notationDiscussion></annotation>" +
				"</p></section>";
			using (XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section")))
			{
				IScrBookAnnotations judAnnotations = m_scr.BookAnnotationsOS[nJud - 1];
				ReflectionHelper.SetField(m_undoImportManager, "m_annotations", judAnnotations);
				ReflectionHelper.CallMethod(m_importer, "SetBookAnnotations");
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

				// Verify the section head
				Assert.AreEqual(1, book.SectionsOS.Count);
				IScrSection sect = book.SectionsOS[0];
				Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
				Assert.AreEqual(ScrStyleNames.SectionHead,
					para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "My section head", null, m_wsVern);

				// Verify the section contents
				Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
				Assert.AreEqual(ScrStyleNames.NormalParagraph,
					para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(2, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "1", ScrStyleNames.VerseNumber, m_wsVern);
				AssertEx.RunIsCorrect(para.Contents, 1,
					"This is the only time I'm going to write to you, so listen up!", null, m_wsVern);

				// Verify the annotation
				Assert.AreEqual(1, judAnnotations.NotesOS.Count, "Expected the book of Jude to have 1 annotation");
				IScrScriptureNote annotation = judAnnotations.NotesOS[0];
				Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
				Assert.AreEqual(065001001, annotation.BeginRef);
				Assert.AreEqual(065001001, annotation.EndRef);
				Assert.AreEqual(NoteStatus.Closed, annotation.ResolutionStatus);
				Assert.AreEqual(para, annotation.BeginObjectRA);
				Assert.AreEqual(para, annotation.EndObjectRA);

				Assert.AreEqual(1, annotation.DiscussionOA.ParagraphsOS.Count);
				IStTxtPara discPara = (IStTxtPara)annotation.DiscussionOA.ParagraphsOS[0];
				ITsString tssDiscussion = discPara.Contents;
				int wsEng = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
				AssertEx.RunIsCorrect(tssDiscussion, 0, "Go ", null, wsEng);
				AssertEx.RunIsCorrect(tssDiscussion, 1, "here", ScrStyleNames.Emphasis, wsEng);
				AssertEx.RunIsCorrect(tssDiscussion, 2, "!", null, wsEng);
			}
		}
	#endregion

		#region ProcessAnnotationParagraphs tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ProcessAnnotationParas method when a paragraph contains mixed text - OXES
		/// schema allow this, though export code does not produce paragraphs in this format.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessAnnotationParas_MixedText()
		{
			ScrNoteImportManager.Initialize(m_scr, 1);

			string xml = "<para><span>span 1. </span>Plus extra text.</para>";
			XmlNotePara para = XmlSerializationHelper.DeserializeFromString<XmlNotePara>(xml);
			StTxtParaBldr bldr = para.BuildParagraph(m_styleSheet, Cache.DefaultVernWs);

			Assert.AreEqual("span 1. Plus extra text.", bldr.StringBuilder.Text);
			Assert.AreEqual(ScrStyleNames.Remark, bldr.ParaStylePropsProxy.StyleId);

			Assert.AreEqual(1, bldr.StringBuilder.RunCount);
			int nDummy;
			Assert.AreEqual(Cache.DefaultVernWs, bldr.StringBuilder.get_Properties(0).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out nDummy));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ProcessAnnotationParas method when a paragraph contains a run with only a
		/// writing system. (TE-8328)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessAnnotationParas_WsRun()
		{
			ScrNoteImportManager.Initialize(m_scr, 1);

			string xml = "<para><span>span 1. </span><span xml:lang=\"es\">span 2</span></para>";
			XmlNotePara para = XmlSerializationHelper.DeserializeFromString<XmlNotePara>(xml);
			StTxtParaBldr bldr = para.BuildParagraph(m_styleSheet, Cache.DefaultAnalWs);

			Assert.AreEqual("span 1. span 2", bldr.StringBuilder.Text);
			Assert.AreEqual(ScrStyleNames.Remark, bldr.ParaStylePropsProxy.StyleId);

			Assert.AreEqual(2, bldr.StringBuilder.RunCount);
			int nDummy;
			Assert.AreEqual(Cache.DefaultAnalWs,
				bldr.StringBuilder.get_Properties(0).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out nDummy));
			int wsEs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("es");
			Assert.AreEqual(wsEs, bldr.StringBuilder.get_Properties(1).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out nDummy));
		}

		#endregion

		#region ProcessBookIntroduction tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ProcessBookSection with an introduction section that has an annotation with
		/// a hyperlink in it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Requires implementation of fix for TE-7509")]
		public void ProcessBookSection_AnnotationInIntroWithHyperlink()
		{
			const int nJud = 65;
			IScrBook book = AddBookToMockedScripture(nJud, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", true);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", nJud);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(nJud, 1, 0));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<section><sectionHead><trGroup><tr>Intro section head</tr>" +
				"</trGroup></sectionHead>" +
				"<p>" +
				"<annotation type=\"translatorNote\" status=\"0\" oxesRef=\"JUD.1.0\" offset=\"6\">" +
				"<created>2008-11-18 19:17:50.14</created>" +
				"<modified>2008-11-18 19:20:23.73</modified>" +
				"<resolved>2008-11-18 19:17:50.14</resolved>" +
				"<notationQuote><para xml:lang=\"en\"><span>section</span></para></notationQuote>" +
				"<notationDiscussion><para xml:lang=\"en\">" +
				"<span>Go </span>" +
				"<a href=\"http://www.myspace.com\">here</a>" +
				"<span>!</span>" +
				"</para></notationDiscussion></annotation>" +
				"<trGroup><tr>Nice book!</tr></trGroup>" +
				"</p></section>";
			using (XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section")))
			{
				IScrBookAnnotations judAnnotations = m_scr.BookAnnotationsOS[nJud - 1];
				ReflectionHelper.SetField(m_undoImportManager, "m_annotations", judAnnotations);
				ReflectionHelper.CallMethod(m_importer, "SetBookAnnotations");
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

				// Verify the section head
				Assert.AreEqual(1, book.SectionsOS.Count);
				IScrSection sect = book.SectionsOS[0];
				Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)sect.HeadingOA.ParagraphsOS[0];
				Assert.AreEqual(ScrStyleNames.IntroSectionHead,
					para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Intro section head", null, m_wsVern);

				// Verify the intro section contents
				Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
				para = (IStTxtPara)sect.ContentOA.ParagraphsOS[0];
				Assert.AreEqual(ScrStyleNames.IntroParagraph,
					para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
				Assert.AreEqual(1, para.Contents.RunCount);
				AssertEx.RunIsCorrect(para.Contents, 0, "Nice book!", null, m_wsVern);

				// Verify the annotation
				IScrScriptureNote annotation = judAnnotations.NotesOS[0];
				int wsEng = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
				Assert.AreEqual(1, judAnnotations.NotesOS.Count,
					"Expected the book of Jude to have 1 annotation");
				Assert.AreEqual(NoteType.Translator, annotation.AnnotationType);
				Assert.AreEqual(065001000, annotation.BeginRef);
				Assert.AreEqual(065001000, annotation.EndRef);
				Assert.AreEqual(NoteStatus.Open, annotation.ResolutionStatus);
				Assert.AreEqual(para, annotation.BeginObjectRA);
				Assert.AreEqual(para, annotation.EndObjectRA);

				Assert.AreEqual(1, annotation.QuoteOA.ParagraphsOS.Count);
				ITsString tssQuote =
					((IStTxtPara)annotation.QuoteOA.ParagraphsOS[0]).Contents;
				Assert.AreEqual(1, tssQuote.RunCount);
				AssertEx.RunIsCorrect(tssQuote, 0, "section", null, wsEng);
				Assert.AreEqual(6, annotation.BeginOffset, "Note begin offset should point to start of the word 'section'");
				Assert.AreEqual(13, annotation.EndOffset, "Note end offset should point to end of the word 'section'");

				Assert.AreEqual(1, annotation.DiscussionOA.ParagraphsOS.Count);
				ITsString tssDiscussion =
					((IStTxtPara)annotation.DiscussionOA.ParagraphsOS[0]).Contents;
				Assert.AreEqual(3, tssDiscussion.RunCount);
				AssertEx.RunIsCorrect(tssDiscussion, 0, "Go ", null, wsEng);
				FdoTestHelper.VerifyHyperlinkPropsAreCorrect(tssDiscussion.get_Properties(1),
					Cache.DefaultAnalWs, "http://www.myspace.com");
				Assert.AreEqual("here", tssDiscussion.get_RunText(1));
				AssertEx.RunIsCorrect(tssDiscussion, 2, "!", null, wsEng);
			}
		}
		#endregion

		#region Processing error conditions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ProcessCanons with an annotation that has an invalid type attribute. There is
		/// no verse number. A test for TE-7719.
		/// TODO for this test: confirm that Scripture/annotation is created correctly. This
		/// test was originally used to test TE-7280.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-7719: Fix needs to be done for this problem.")]
		public void ProcessCanons_InvalidAnnotationTypeAttribute_NoVerseNum()
		{
			ReflectionHelper.SetField(m_importer, "m_sOXESFile", "testFile.xml");

			m_teImportUi.Maximum = 1;
			string strToImport =
				"<canon ID=\"nt\"><book ID=\"TIT\"><titleGroup short=\"Titus\" /><section>" +
				"<sectionHead><trGroup /></sectionHead><p><chapterStart ID=\"TIT.1\" n=\"1\" />" +
				"<trGroup><tr>Text to be annotated.</tr></trGroup><chapterEnd ID=\"TIT.1\" />" +
				"</p><annotation type=\"invalidID\" status=\"0\" oxesRef=\"TIT.1.1\" " +
				"beginOffset=\"12\" endOffset=\"21\"><created>2009-02-12 20:02:11.40</created>" +
				"<modified>2009-02-12 20:02:20.65</modified><resolved /><notationQuote>" +
				"<para xml:lang=\"fr\"><span>annotated</span></para></notationQuote></annotation>" +
				"</section></book></canon>";

			XmlDocument toImport = new XmlDocument();
			using (var stringReader = new StringReader(strToImport))
				toImport.Load(stringReader);

			using (XmlNodeReader reader = new XmlNodeReader(toImport.SelectSingleNode("canon")))
			{
				try
				{
					ReflectionHelper.CallMethod(m_importer, "ProcessCanons", reader);
				}
				catch (TargetInvocationException e)
				{
					throw e.InnerException;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ProcessCanons with an annotation that has an invalid type attribute. We want
		/// an annotation to be created with a default type of "consultant".
		/// See TE-7280.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ProcessCanons_InvalidAnnotationTypeAttribute()
		{
			ReflectionHelper.SetField(m_importer, "m_sOXESFile", "testFile.xml");

			m_teImportUi.Maximum = 1;
			string strToImport =
				"<canon ID=\"nt\"><book ID=\"TIT\"><titleGroup short=\"Titus\" /><section><sectionHead>"+
				"<trGroup /></sectionHead><p><chapterStart ID=\"TIT.1\" n=\"1\" />"+
				"<verseStart ID=\"TIT.1.1\" n=\"1\" /><annotation type=\"invalidType\" status=\"0\" "+
				"oxesRef=\"TIT.1.1\" beginOffset=\"13\" endOffset=\"22\">"+
				"<created>2009-02-12 20:02:11.40</created><modified>2009-02-12 20:02:20.65</modified>"+
				"<resolved /><notationQuote><para xml:lang=\"fr\"><span>annotated</span></para>"+
				"</notationQuote></annotation><trGroup><tr>Text to be annotated.</tr></trGroup>"+
				"<verseEnd ID=\"TIT.1.1\" /><chapterEnd ID=\"TIT.1\" /></p></section></book></canon>";

			XmlDocument toImport = new XmlDocument();
			using (var stringReader = new StringReader(strToImport))
				toImport.Load(stringReader);

			using (XmlNodeReader reader = new XmlNodeReader(toImport.SelectSingleNode("canon")))
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessCanons", reader);

				// Make sure that import has correctly created Scripture data for the book of Titus.
				IScrBook titus = (IScrBook)ReflectionHelper.GetField(m_importer, "m_scrBook");
				Assert.AreEqual(1, titus.SectionsOS.Count);
				IScrSection section = titus.SectionsOS[0];
				Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
				Assert.AreEqual("11Text to be annotated.", para.Contents.Text);
				// Now check that we have an annotation of type "consultant"
				Assert.AreEqual(1, m_scr.BookAnnotationsOS[55].NotesOS.Count);
				IScrScriptureNote note = m_scr.BookAnnotationsOS[55].NotesOS[0];
				Assert.AreEqual(CmAnnotationDefnTags.kguidAnnConsultantNote, note.AnnotationTypeRA.Guid,
					"The annotation should default to a consultant note.");
				Assert.AreEqual(13, note.BeginOffset);
				Assert.AreEqual(22, note.EndOffset);
				Assert.AreEqual(56001001, note.BeginRef);
				Assert.AreEqual(56001001, note.EndRef);
				Assert.AreEqual("annotated", ((IStTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);
			}
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since we don't have any better source of usernames, we're being passed in the system
		/// login name, which may well have a domain followed by the user.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string AbbreviateUserName(string sWho)
		{
			string[] rgsDomainUser = sWho.Split(new char[] { '\\' });
			string[] rgsName = rgsDomainUser[rgsDomainUser.Length - 1].Split(new char[] { ' ' });
			if (rgsName.Length > 1)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < rgsName.Length; ++i)
					sb.Append(rgsName[i].ToCharArray()[0]);
				return sb.ToString().ToLower();
			}
			else
			{
				if (rgsName[0].Length > 3)
					return rgsName[0].Substring(0, 3).ToLower();
				else
					return rgsName[0].ToLower();
			}
		}
		#endregion
	}
	#endregion
}
