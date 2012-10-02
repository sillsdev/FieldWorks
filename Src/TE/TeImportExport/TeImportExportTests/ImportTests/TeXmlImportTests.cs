// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeXmlImportTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

using NUnit.Framework;
using NMock.Dynamic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using System.IO;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using NMock;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.TE.ImportTests
{
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
		protected TeImportNoUi m_teImportUi = new TeImportNoUi();
		/// <summary>The "m_stackSectionType" member of the m_importer (gotten by reflection)</summary>
		Stack<string> m_stackSectionType;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the importer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			base.Initialize();

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);
			InitWsInfo();
			m_undoImportManager = new UndoImportManager(Cache, null);
			m_importer = ReflectionHelper.CreateObject("TeImportExport.dll",
				"SIL.FieldWorks.TE.TeXmlImporter", BindingFlags.NonPublic,
				new object[] {Cache, m_styleSheet, " ", m_undoImportManager,
				m_teImportUi}) as TeXmlImporter;

			ReflectionHelper.CallMethod(m_importer, "Initialize", null);
			m_stackSectionType = ReflectionHelper.GetField(m_importer, "m_stackSectionType") as Stack<string>;

			StyleProxyListManager.Initialize(m_styleSheet);
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
				if (m_importer != null)
					m_importer.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_styleSheet = null; // FwStyleSheet should implement IDisposable.
			m_importer = null; // TeImporter should implement IDisposable.

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_importer.Dispose();
			m_importer = null;
			m_styleSheet = null;
			StyleProxyListManager.Cleanup();
			ScrNoteImportManager.Cleanup();

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			// setup the default vernacular WS
			m_scrInMemoryCache.CacheAccessor.CacheVecProp(Cache.LangProject.Hvo,
				(int)LangProject.LangProjectTags.kflidCurVernWss,
				new int[] { InMemoryFdoCache.s_wsHvos.XKal }, 1);
			Cache.LangProject.CacheDefaultWritingSystems();
			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_scrInMemoryCache.InitializeScrAnnotationCategories();
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			base.InitializeCache();
			Cache.MapType(typeof(StTxtPara), typeof(ScrTxtPara));
			Cache.MapType(typeof(StFootnote), typeof(ScrFootnote));
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection sect = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sect.Hvo, "This is My Head",
				ScrStyleNames.SectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<p><chapterStart ID=\"JUD.1\" n=\"1\" />" +
				"<verseStart ID=\"JUD.1.1\" n=\"1\" /><trGroup><tr>Nice book!</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(3, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "Nice book!", null, m_wsVern);
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection sect = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sect.Hvo, "This is My Head",
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
			StTxtPara para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Custom user style"), para.StyleRules);
			IStStyle newStyle = m_styleSheet.FindStyle("Custom user style");
			Assert.AreEqual(ContextValues.Text, newStyle.Context);
			Assert.AreEqual(StructureValues.Body, newStyle.Structure);
			Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
			Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.NormalParagraph), newStyle.BasedOnRA);
			Assert.AreEqual(newStyle, newStyle.NextRA);
			Assert.AreEqual(3, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "Nice book!", null, m_wsVern);
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection sect = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo, true);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sect.Hvo, "This is My Head",
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
			StTxtPara para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Custom intro style"), para.StyleRules);
			IStStyle newStyle = m_styleSheet.FindStyle("Custom intro style");
			Assert.AreEqual(ContextValues.Intro, newStyle.Context);
			Assert.AreEqual(StructureValues.Body, newStyle.Structure);
			Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
			Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.IntroParagraph), newStyle.BasedOnRA);
			Assert.AreEqual(newStyle, newStyle.NextRA);
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Read this book.", null, m_wsVern);
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection sect = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo, true);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sect.Hvo, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = "<p><trGroup><tr>This is actually useful text according to Tim" +
				"<emphasis><foreign xml:lang=\"fr\">Ah, oui!</foreign></emphasis>" +
				"<foreign><foreign xml:lang=\"fr\">Ah, non!</foreign></foreign>" +
				"</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(3, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "This is actually useful text according to Tim", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "Ah, oui!", ScrStyleNames.Emphasis,
				Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("fr"));
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "Ah, non!", ScrStyleNames.Foreign,
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection sect = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo, true);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sect.Hvo, "This is My Head",
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
			StTxtPara para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(2, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "This is actually useful text according to Tim", null, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "Ah, oui!", "Commentary of Joel",
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection sect = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo, true);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sect.Hvo, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = "<p><trGroup><tr>This is actually useful text according to Tim.\r\n\r\n</tr></trGroup>" +
				"<trGroup><tr>This might dissappear though</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0,
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection sect = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo, true);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sect.Hvo, "This is My Head",
				ScrStyleNames.IntroSectionHead);
			XmlDocument doc = new XmlDocument();
			ReflectionHelper.CallMethod(m_stackSectionType, "Push", "");
			m_teImportUi.Maximum = 0;
			doc.InnerXml = "<p><trGroup><tr>This is useful? &#x2;&#x5;&#x3;&#xA;&#xD;&#xE;&#xFFFF;&#xFFFE;</tr></trGroup></p>";
			XmlNode paraNode = doc.SelectSingleNode("p");
			ReflectionHelper.CallMethod(m_importer, "ProcessParagraphNode", paraNode, sect);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0,
				"This is useful? ", null, m_wsVern);
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
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
			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section"));
			ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

			// Verify the section head
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection sect = book.SectionsOS[0];
			Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead), para.StyleRules);
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Warnings and Instructions", null, m_wsVern);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(3, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "2", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "Nice book!", null, m_wsVern);
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
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
			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section"));
			ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

			// Verify the section head
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection sect = book.SectionsOS[0];
			Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroSectionHead), para.StyleRules);
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Intro section head", null, m_wsVern);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph), para.StyleRules);
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Nice book!", null, m_wsVern);
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
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
			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section"));
			ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

			// Verify the section head
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection sect = book.SectionsOS[0];
			Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Moderately Major Head", para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Warnings and Instructions", null, m_wsVern);

			IStStyle newStyle = m_styleSheet.FindStyle("Moderately Major Head");
			Assert.AreEqual(ContextValues.Text, newStyle.Context);
			Assert.AreEqual(StructureValues.Heading, newStyle.Structure);
			Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
			Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.SectionHead), newStyle.BasedOnRA);
			Assert.AreEqual(newStyle, newStyle.NextRA);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph), para.StyleRules);
			Assert.AreEqual(3, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "1", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "2", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "Nice book!", null, m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a reader that is ready to read a Scripture section node with a user-created
		/// Section Head style to ProcessBookSection. The Section Head is not in valid format.
		/// See TE-7258.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
		ExpectedMessage = "Unexpected paragraph element.\r\n\r\nLine 0 of file  :\r\n<p type=\"userPS\" subType=\"Moderately Major Head\">\r\nAttempting to read JUD  Chapter: 1  Verse: 16")]
		public void ProcessBookSection_UserCreatedScrStyle_BogusSectionHeadStyle()
		{
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			ReflectionHelper.SetField(m_importer, "m_fCurrentSectionIsIntro", false);
			ReflectionHelper.SetField(m_importer, "m_scrBook", book);
			ReflectionHelper.SetField(m_importer, "m_currSection", null);
			ReflectionHelper.SetField(m_importer, "m_iCurrSection", -1);
			ReflectionHelper.SetField(m_importer, "m_nBookNumber", 65);
			ReflectionHelper.SetField(m_importer, "m_currentRef", new BCVRef(65, 1, 16));

			XmlDocument doc = new XmlDocument();
			m_teImportUi.Maximum = 1;
			doc.InnerXml = "<section><sectionHead type=\"userDefined\" subType=\"Moderately Major Head\">" +
				"<p type=\"userPS\" subType=\"Moderately Major Head\">" +
				"<trGroup><tr>A new section heading</tr></trGroup></p></sectionHead>" +
				"<p><verseStart ID=\"JUD.1.17\" n=\"17\" /><trGroup>" +
				"<tr>Here is some section content.</tr></trGroup><verseEnd ID=\"JUD.1.17\" /></p></section>";

			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section"));
			try
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
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
				"<note type=\"speaker\" canonical=\"true\"><trGroup><tr>He said so!</tr></trGroup></note>" +
				"<trGroup><tr> From Jude, servant of Jesus Christ</tr></trGroup></p></section>";

			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section"));
			ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

			// Verify the footnote
			Assert.AreEqual(1, book.FootnotesOS.Count);
			ScrFootnote footnote = (ScrFootnote)book.FootnotesOS[0];
			Assert.AreEqual(1, footnote.ParagraphsOS.Count);
			StTxtPara footnotePara = (StTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph,
				footnotePara.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			AssertEx.RunIsCorrect(footnotePara.Contents.UnderlyingTsString, 0, "He said so!", null, m_wsVern);
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
			CheckDisposed();
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
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
			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("introduction"));
			ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

			// Verify the section head
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection sect = book.SectionsOS[0];
			Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Moderately Major Intro Head", para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Before You Read this Book...", null, m_wsVern);

			IStStyle newStyle = m_styleSheet.FindStyle("Moderately Major Intro Head");
			Assert.AreEqual(ContextValues.Intro, newStyle.Context);
			Assert.AreEqual(StructureValues.Heading, newStyle.Structure);
			Assert.AreEqual(FunctionValues.Prose, newStyle.Function);
			Assert.AreEqual(m_styleSheet.FindStyle(ScrStyleNames.IntroSectionHead), newStyle.BasedOnRA);
			Assert.AreEqual(newStyle, newStyle.NextRA);

			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph), para.StyleRules);
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Nice book!", null, m_wsVern);
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
			CheckDisposed();
			const int nJud = 65;
			ScrNoteImportManager.Initialize(m_scr, nJud);
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(nJud, "Jude");
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
			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section"));
			ScrBookAnnotations judAnnotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[nJud - 1];
			ReflectionHelper.SetField(m_undoImportManager, "m_annotations", judAnnotations);
			ReflectionHelper.CallMethod(m_importer, "SetBookAnnotations");
			ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

			// Verify the section head
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection sect = book.SectionsOS[0];
			Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "My section head", null, m_wsVern);

			// Verify the section contents
			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(2, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "1", ScrStyleNames.VerseNumber, m_wsVern);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1,
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
			StTxtPara discPara = (StTxtPara)annotation.DiscussionOA.ParagraphsOS[0];
			ITsString tssDiscussion = discPara.Contents.UnderlyingTsString;
			int wsEng = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			AssertEx.RunIsCorrect(tssDiscussion, 0, "Go ", null, wsEng);
			AssertEx.RunIsCorrect(tssDiscussion, 1, "here", ScrStyleNames.Emphasis, wsEng);
			AssertEx.RunIsCorrect(tssDiscussion, 2, "!", null, wsEng);
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
			CheckDisposed();
			ScrNoteImportManager.Initialize(m_scr, 1);

			string xml = "<para><span>span 1. </span>Plus extra text.</para>";
			XmlNotePara para = XmlSerializationHelper.DeserializeFromString<XmlNotePara>(xml);
			StTxtParaBldr bldr = para.BuildParagraph(m_styleSheet, Cache.DefaultVernWs);

			Assert.AreEqual("span 1. Plus extra text.", bldr.StringBuilder.Text);
			Assert.AreEqual(ScrStyleNames.Remark,
				bldr.ParaStylePropsProxy.Props.GetStrPropValue(
				(int)FwTextStringProp.kstpNamedStyle));

			Assert.AreEqual(1, bldr.StringBuilder.RunCount);
			int nDummy;
			Assert.AreEqual(Cache.DefaultVernWs,
				bldr.StringBuilder.get_Properties(0).GetIntPropValues(
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
			CheckDisposed();
			ScrNoteImportManager.Initialize(m_scr, 1);

			string xml = "<para><span>span 1. </span><span xml:lang=\"es\">span 2</span></para>";
			XmlNotePara para = XmlSerializationHelper.DeserializeFromString<XmlNotePara>(xml);
			StTxtParaBldr bldr = para.BuildParagraph(m_styleSheet, Cache.DefaultAnalWs);

			Assert.AreEqual("span 1. span 2", bldr.StringBuilder.Text);
			Assert.AreEqual(ScrStyleNames.Remark,
				bldr.ParaStylePropsProxy.Props.GetStrPropValue(
				(int)FwTextStringProp.kstpNamedStyle));

			Assert.AreEqual(2, bldr.StringBuilder.RunCount);
			int nDummy;
			Assert.AreEqual(Cache.DefaultAnalWs,
				bldr.StringBuilder.get_Properties(0).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out nDummy));
			Assert.AreEqual(InMemoryFdoCache.s_wsHvos.Es,
				bldr.StringBuilder.get_Properties(1).GetIntPropValues(
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
			CheckDisposed();
			const int nJud = 65;
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(nJud, "Jude");
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
			XmlNodeReader reader = new XmlNodeReader(doc.SelectSingleNode("section"));
			ScrBookAnnotations judAnnotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[nJud - 1];
			ReflectionHelper.SetField(m_undoImportManager, "m_annotations", judAnnotations);
			ReflectionHelper.CallMethod(m_importer, "SetBookAnnotations");
			ReflectionHelper.CallMethod(m_importer, "ProcessBookSection", reader, true);

			// Verify the section head
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection sect = book.SectionsOS[0];
			Assert.AreEqual(1, sect.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)sect.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Intro section head", null, m_wsVern);

			// Verify the intro section contents
			Assert.AreEqual(1, sect.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)sect.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "Nice book!", null, m_wsVern);

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
				((StTxtPara)annotation.QuoteOA.ParagraphsOS[0]).Contents.UnderlyingTsString;
			Assert.AreEqual(1, tssQuote.RunCount);
			AssertEx.RunIsCorrect(tssQuote, 0, "section", null, wsEng);
			Assert.AreEqual(6, annotation.BeginOffset, "Note begin offset should point to start of the word 'section'");
			Assert.AreEqual(13, annotation.EndOffset, "Note end offset should point to end of the word 'section'");

			Assert.AreEqual(1, annotation.DiscussionOA.ParagraphsOS.Count);
			ITsString tssDiscussion =
				((StTxtPara)annotation.DiscussionOA.ParagraphsOS[0]).Contents.UnderlyingTsString;
			Assert.AreEqual(3, tssDiscussion.RunCount);
			AssertEx.RunIsCorrect(tssDiscussion, 0, "Go ", null, wsEng);
			StStyleTests.AssertHyperlinkPropsAreCorrect(tssDiscussion.get_Properties(1),
				Cache.DefaultAnalWs, "http://www.myspace.com");
			Assert.AreEqual("here", tssDiscussion.get_RunText(1));
			AssertEx.RunIsCorrect(tssDiscussion, 2, "!", null, wsEng);
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
			CheckDisposed();
			ReflectionHelper.SetField(m_importer, "m_sOXESFile", "testFile.xml");

			m_teImportUi.Maximum = 1;
			string strToImport =
				"<canon ID=\"nt\"><book ID=\"TIT\"><titleGroup short=\"Titus\" /><section>"+
				"<sectionHead><trGroup /></sectionHead><p><chapterStart ID=\"TIT.1\" n=\"1\" />"+
				"<trGroup><tr>Text to be annotated.</tr></trGroup><chapterEnd ID=\"TIT.1\" />"+
				"</p><annotation type=\"invalidID\" status=\"0\" oxesRef=\"TIT.1.1\" "+
				"beginOffset=\"12\" endOffset=\"21\"><created>2009-02-12 20:02:11.40</created>"+
				"<modified>2009-02-12 20:02:20.65</modified><resolved /><notationQuote>"+
				"<para xml:lang=\"x-kal\"><span>annotated</span></para></notationQuote></annotation>"+
				"</section></book></canon>";

			XmlDocument toImport = new XmlDocument();
			toImport.Load(new StringReader(strToImport));

			XmlNodeReader reader = new XmlNodeReader(toImport.SelectSingleNode("canon"));
			try
			{
				ReflectionHelper.CallMethod(m_importer, "ProcessCanons", reader);
			}
			catch (TargetInvocationException e)
			{
				throw e.InnerException;
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
			CheckDisposed();
			ReflectionHelper.SetField(m_importer, "m_sOXESFile", "testFile.xml");

			m_teImportUi.Maximum = 1;
			string strToImport =
				"<canon ID=\"nt\"><book ID=\"TIT\"><titleGroup short=\"Titus\" /><section><sectionHead>"+
				"<trGroup /></sectionHead><p><chapterStart ID=\"TIT.1\" n=\"1\" />"+
				"<verseStart ID=\"TIT.1.1\" n=\"1\" /><annotation type=\"invalidType\" status=\"0\" "+
				"oxesRef=\"TIT.1.1\" beginOffset=\"13\" endOffset=\"22\">"+
				"<created>2009-02-12 20:02:11.40</created><modified>2009-02-12 20:02:20.65</modified>"+
				"<resolved /><notationQuote><para xml:lang=\"x-kal\"><span>annotated</span></para>"+
				"</notationQuote></annotation><trGroup><tr>Text to be annotated.</tr></trGroup>"+
				"<verseEnd ID=\"TIT.1.1\" /><chapterEnd ID=\"TIT.1\" /></p></section></book></canon>";

			XmlDocument toImport = new XmlDocument();
			toImport.Load(new StringReader(strToImport));

			XmlNodeReader reader = new XmlNodeReader(toImport.SelectSingleNode("canon"));
			ReflectionHelper.CallMethod(m_importer, "ProcessCanons", reader);

			// Make sure that import has correctly created Scripture data for the book of Titus.
			ScrBook titus = (ScrBook)ReflectionHelper.GetField(m_importer, "m_scrBook");
			Assert.AreEqual(1, titus.SectionsOS.Count);
			ScrSection section = (ScrSection)titus.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Text to be annotated.", para.Contents.Text);
			// Now check that we have an annotation of type "consultant"
			Assert.AreEqual(1, m_scr.BookAnnotationsOS[55].NotesOS.Count);
			ScrScriptureNote note = (ScrScriptureNote)m_scr.BookAnnotationsOS[55].NotesOS[0];
			Assert.AreEqual(LangProject.kguidAnnConsultantNote, note.AnnotationTypeRA.Guid,
				"The annotation should default to a consultant note.");
			Assert.AreEqual(13, note.BeginOffset);
			Assert.AreEqual(22, note.EndOffset);
			Assert.AreEqual(56001001, note.BeginRef);
			Assert.AreEqual(56001001, note.EndRef);
			Assert.AreEqual("annotated", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an XML header for testing.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string CreateXmlHeader()
		{
			System.Security.Principal.WindowsIdentity whoami = System.Security.Principal.WindowsIdentity.GetCurrent();
			string sWho = whoami.Name.Normalize();
			string sWhoAbbr = AbbreviateUserName(sWho);

			return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<oxes xmlns=\"http://www.wycliffe.net/scripture/namespace/version_1.1.2\">" +
				"<oxesText type=\"Wycliffe-1.1.2\" oxesIDWork=\"WBT.x-kal\" xml:lang=\"x-kal\" " +
				"canonical=\"true\">" +
				"<header><revisionDesc resp=\"sha\"><date>" + String.Format("{0:yyyy.MM.dd}", DateTime.Now) +
				"</date><para xml:lang=\"en\">TestLangProj</para></revisionDesc>" +
				"<work oxesWork=\"WBT.x-kal\"><titleGroup><title type=\"main\">" +
				"<trGroup><tr>TODO: title of New Testament or Bible goes here</tr></trGroup>" +
				"</title></titleGroup><contributor role=\"Translator\" ID=\"" + sWhoAbbr + "\">" +
				sWho + "</contributor></work></header>" +
				"<titlePage><titleGroup><title type=\"main\"><trGroup>" +
				"<tr>TODO: Title of New Testament or Bible goes here</tr></trGroup></title></titleGroup>" +
				"</titlePage>";
		}
		#endregion
	}
	#endregion
}
