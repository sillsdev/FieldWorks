// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportXmlTests.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using NUnit.Framework;
using Commons.Xml.Relaxng;

using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Cellar;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE
{
	#region ExportXml tests with FDO cache -- DO NOT ADD TO THIS
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ExportXml
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportXmlTestsWithRealDb : FdoTestBase
	{
		private ExportXml m_exporter;
		string m_fileName;
		FdoCache m_cache;
		FilteredScrBooks m_filter;

		#region setup,teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			m_cache = FdoCache.Create("TestLangProj");
			m_filter = new FilteredScrBooks(m_cache, 123);
			m_filter.ShowAllBooks();
			m_fileName = Path.GetTempFileName();
			File.Delete(m_fileName);	// exporter pops up dialog if file exists!
			m_exporter = new ExportXml(m_fileName, m_cache, m_filter,
				ExportWhat.AllBooks, 0, 0, 0, "This is a test");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test teardown
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Deinitialize()
		{
			CheckDisposed();
			m_exporter = null;
			m_cache.Dispose();
			m_cache = null;
			m_filter = null;
			File.Delete(m_fileName);
			m_fileName = null;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required accessor for the cache.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
		}

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a file, then validate it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportAndValidate()
		{
			CheckDisposed();
			// Export.
			m_exporter.Run();
			// Validate.
			string errors = OxesIO.Validator.GetAnyValidationErrors(m_fileName);
			Assert.IsNull(errors);
		}
		#endregion
	}
	#endregion

	#region ExportXml tests with in-memory cache
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ExportXml using an in-memory cache
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportXmlTests : ScrInMemoryFdoTestBase
	{
		private ExportXml m_exporter;
		string m_fileName;
		FilteredScrBooks m_filter;
		IScrBook m_book;
		FwStyleSheet m_stylesheet;
		bool m_saveSegmentedBT;

		#region setup,teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_fileName = Path.GetTempFileName();
			File.Delete(m_fileName);	// exporter pops up dialog if file exists!
			m_scrInMemoryCache.InitializeScripture();
			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_stylesheet = new FwStyleSheet();
			m_stylesheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);
			m_scrInMemoryCache.InitializeAnnotationDefs();

			m_book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo, "Genesis");

			m_filter = new FilteredScrBooks(Cache, 123);
			m_filter.ShowAllBooks();

			m_saveSegmentedBT = Options.UseInterlinearBackTranslation;
			Options.UseInterlinearBackTranslation = false;

			m_exporter = new ExportXml(m_fileName, Cache, m_filter, ExportWhat.AllBooks, 0, 0, 0,
				string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// lears member variables, cleans up temp files, shuts down the cache, etc.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_exporter = null;
			m_filter = null;
			if (m_fileName != null)
				File.Delete(m_fileName);
			m_stylesheet = null;
			m_fileName = null;

			Options.UseInterlinearBackTranslation = m_saveSegmentedBT;

			base.Exit();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export an OXES file with user-defined section head styles, then validate it.
		/// TE-7267, TE-7268
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHead_UserDefined()
		{
			CheckDisposed();

			IStStyle sectionHead = m_scr.FindStyle("Section Head");
			int hvoNewStyle = m_stylesheet.MakeNewStyle();
			m_stylesheet.PutStyle("Major Massive Huge Section Head", "Use for little tiny sections",
				hvoNewStyle, sectionHead.Hvo, hvoNewStyle, (int)StyleType.kstParagraph, false,
				false, sectionHead.Rules);

			// Create section head with this "user-defined" style.
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				"This is a user-defined section head", "Major Massive Huge Section Head");
			m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");

			// Export.
			m_exporter.Run();

			// Validate.
			string errors = OxesIO.Validator.GetAnyValidationErrors(m_fileName);
			Assert.IsNull(errors);

			// Read file and compare to expected results. The first node inside sectionHead should
			// be trGroup.
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(m_fileName);
			XmlNode nodeOxes = xmlDoc.ChildNodes[1];
			Assert.AreEqual("oxes", nodeOxes.Name);
			XmlNode nodeOxesText = nodeOxes.FirstChild;
			Assert.AreEqual("oxesText", nodeOxesText.Name);
			XmlNode nodeCanon = FindChildNode(nodeOxesText, "canon");
			Assert.IsNotNull(nodeCanon);
			XmlNode nodeBook = nodeCanon.FirstChild;
			Assert.AreEqual("book", nodeBook.Name);
			Assert.AreEqual("GEN", nodeBook.Attributes[0].Value);
			XmlNode nodeSection = FindChildNode(nodeBook, "section");
			XmlNode nodeSectionHead = nodeSection.FirstChild;
			Assert.AreEqual("sectionHead", nodeSectionHead.Name);
			XmlNode nodeTrGroup = nodeSectionHead.FirstChild;
			Assert.AreEqual("trGroup", nodeTrGroup.Name);
			Assert.AreEqual("This is a user-defined section head", nodeTrGroup.FirstChild.InnerText);
		}
		#endregion

		#region Tests for ExportNotationParagraphs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportNotationParagraphs for a basic annotation text field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportNotationParagraphs_Basic()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some basic text.");
			TsStringAccessor tssaDisc = ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents;

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportNotationParagraphs_Basic");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.CallMethod(m_exporter, "ExportNotationParagraphs", note.DiscussionOA,
				"notationDiscussion");
			Assert.AreEqual("<notationDiscussion><para xml:lang=\"en\">" +
				"<span>This is some basic text.</span>" +
				"</para></notationDiscussion>", stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportNotationParagraphs when the text contains a character style (TE-7461).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportNotationParagraphs_CharStyle()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some text with emphasis style.");
			TsStringAccessor tssaDisc = ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents;
			ITsStrBldr bldr = tssaDisc.UnderlyingTsString.GetBldr();
			bldr.SetProperties(bldr.Length - 15, bldr.Length - 1,
				StyleUtils.CharStyleTextProps(ScrStyleNames.Emphasis, Cache.DefaultAnalWs));
			tssaDisc.UnderlyingTsString = bldr.GetString();

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportNotationParagraphs_CharStyle");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.CallMethod(m_exporter, "ExportNotationParagraphs", note.DiscussionOA,
				"notationDiscussion");
			Assert.AreEqual("<notationDiscussion><para xml:lang=\"en\">" +
				"<span>This is some text with </span>" +
				"<span type=\"Emphasis\">emphasis style</span>" +
				"<span>.</span>" +
				"</para></notationDiscussion>", stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportNotationParagraphs when the text contains a run with a different
		/// writing system (TE-7461).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportNotationParagraphs_WritingSystems()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some text with espa\u00F1ol.");
			TsStringAccessor tssaDisc = ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents;
			ITsStrBldr bldr = tssaDisc.UnderlyingTsString.GetBldr();
			bldr.SetProperties(bldr.Length - 8, bldr.Length - 1,
				StyleUtils.CharStyleTextProps(null, InMemoryFdoCache.s_wsHvos.Es));
			tssaDisc.UnderlyingTsString = bldr.GetString();

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportNotationParagraphs_WritingSystems");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.CallMethod(m_exporter, "ExportNotationParagraphs", note.DiscussionOA,
				"notationDiscussion");
			Assert.AreEqual("<notationDiscussion><para xml:lang=\"en\">" +
				"<span>This is some text with </span>" +
				"<span xml:lang=\"es\">espa\u00F1ol</span>" +
				"<span>.</span>" +
				"</para></notationDiscussion>", stream.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportNotationParagraphs when the text contains a hyperlink (TE-7461).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportNotationParagraphs_Hyperlink()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some text with a hyperlink!");
			TsStringAccessor tssaDisc = ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents;
			tssaDisc.MarkTextAsHyperlink(tssaDisc.Length - 10, tssaDisc.Length - 1,
				"http://www.myspace.com", m_stylesheet);

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportNotationParagraphs_Hyperlink");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.CallMethod(m_exporter, "ExportNotationParagraphs", note.DiscussionOA,
				"notationDiscussion");
			Assert.AreEqual("<notationDiscussion><para xml:lang=\"en\">" +
				"<span>This is some text with a </span>" +
				"<a href=\"http://www.myspace.com\">hyperlink</a>" +
				"<span>!</span>" +
				"</para></notationDiscussion>", stream.ToString());
		}
		#endregion

		#region ExportScriptureNote
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportScriptureNote for an unresolved annotation, verifying it doesn't export
		/// resolved date (TE-7511).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportScriptureNote_Unresolved()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = AddPara(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(para, new BCVRef(01001001),
				NoteType.Consultant, "This is some basic text.");
			note.ResolutionStatus = NoteStatus.Open;
			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;
			//note.DateResolved = now;

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportScriptureNote_Unresolved");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.CallMethod(m_exporter, "ExportScriptureNote", note);

			string strExpected = "<annotation type=\"consultantNote\" status=\"0\" oxesRef=\"GEN.1.1\" " +
							"beginOffset=\"0\" endOffset=\"0\">" +
							"<created>" + strNow + "</created>" +
							"<modified>" + strNow + "</modified>" +
							"<resolved />" +
							"<notationDiscussion>" +
								"<para xml:lang=\"en\">" +
									"<span>This is some basic text.</span>" +
								"</para>" +
							"</notationDiscussion>" +
						"</annotation>";

			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));

			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportScriptureNote for an Resolved annotation, verifying it exports the
		/// correct resolved date (TE-7511).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportScriptureNote_Resolved()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = AddPara(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(para, new BCVRef(01001001),
				NoteType.Consultant, "This is some basic text.");
			note.ResolutionStatus = NoteStatus.Closed;
			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;
			note.DateResolved = now;

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportScriptureNote_Resolved");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.CallMethod(m_exporter, "ExportScriptureNote", note);
			string strExpected = "<annotation type=\"consultantNote\" status=\"1\" oxesRef=\"GEN.1.1\" " +
							"beginOffset=\"0\" endOffset=\"0\">" +
							"<created>" + strNow + "</created>" +
							"<modified>" + strNow + "</modified>" +
							"<resolved>" + strNow + "</resolved>" +
							"<notationDiscussion>" +
								"<para xml:lang=\"en\">" +
									"<span>This is some basic text.</span>" +
								"</para>" +
							"</notationDiscussion>" +
						"</annotation>";

			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));

			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportScriptureNote when the annotations are found to be out of order
		/// (i.e. annotations are in their collection in a different order from the order in
		/// which their associated paragraphs are found in scripture. This is most likely
		/// possible when annotations are found in intro. material (TE-7509).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportScriptureNotesOutOfOrder()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);

			// Create 2 paragraphs
			StTxtPara para1 = AddPara(section, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = AddPara(section, ScrStyleNames.NormalParagraph);

			// Add an annotation to the second paragraph. Ref.
			// GEN 1 means annotation is in an introduction paragraph.
			IScrScriptureNote note1 = m_scrInMemoryCache.AddAnnotation(
				para2, new BCVRef(01001000), NoteType.Consultant, "Annotation1 for Para2");

			// Add an annotation to the first paragraph.
			// GEN 1 means annotation is in an introduction paragraph.
			IScrScriptureNote note2 = m_scrInMemoryCache.AddAnnotation(
				para1, new BCVRef(01001000), NoteType.Consultant, "Annotation2 for Para1");

			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note1.DateCreated = note2.DateCreated = now;
			note1.DateModified = note2.DateModified = now;
			note1.DateResolved = note2.DateResolved = now;

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);

			writer.WriteStartElement("TestData");

			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportScriptureNotesOutOfOrder");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.SetField(m_exporter, "m_iCurrentBook", 1);
			ReflectionHelper.CallMethod(m_exporter, "ExportAnnotationsForPara", para1.Hvo);
			ReflectionHelper.CallMethod(m_exporter, "ExportAnnotationsForPara", para2.Hvo);

			writer.WriteEndElement();

			string strExpected = "<TestData>" +
				"<annotation type=\"consultantNote\" status=\"0\" oxesRef=\"GEN.1.0\" " +
							"beginOffset=\"0\" endOffset=\"0\">" +
							"<created>" + strNow + "</created>" +
							"<modified>" + strNow + "</modified>" +
							"<resolved />" +
							"<notationDiscussion>" +
								"<para xml:lang=\"en\">" +
									"<span>Annotation2 for Para1</span>" +
								"</para>" +
							"</notationDiscussion>" +
						"</annotation>" +
						"<annotation type=\"consultantNote\" status=\"0\" oxesRef=\"GEN.1.0\" " +
							"beginOffset=\"0\" endOffset=\"0\">" +
							"<created>" + strNow + "</created>" +
							"<modified>" + strNow + "</modified>" +
							"<resolved />" +
							"<notationDiscussion>" +
								"<para xml:lang=\"en\">" +
									"<span>Annotation1 for Para2</span>" +
								"</para>" +
							"</notationDiscussion>" +
						"</annotation></TestData>";

			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));

			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportScriptureNote for an ignored checking error, verifying it exports the
		/// correctly (TE-8292).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportScriptureNote_IgnoredCheckingError()
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = AddPara(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(para, new BCVRef(01001001),
				NoteType.CheckingError, "This is some basic text.");
			note.AnnotationTypeRA.Name.SetAlternative("Repeated Words", Cache.DefaultAnalWs);
			note.AnnotationTypeRA.Guid = new Guid(StandardCheckIds.kguidRepeatedWords.ToString());
			note.ResolutionStatus = NoteStatus.Closed;
			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;
			note.DateResolved = now;

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportScriptureNote_Resolved");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			ReflectionHelper.CallMethod(m_exporter, "ExportScriptureNote", note);
			string strExpected = "<annotation subType=\"repeatedWordsCheck\" status=\"1\" oxesRef=\"GEN.1.1\" " +
							"beginOffset=\"0\" endOffset=\"0\" type=\"pre-typesettingCheck\">" +
							"<created>" + strNow + "</created>" +
							"<modified>" + strNow + "</modified>" +
							"<resolved>" + strNow + "</resolved>" +
							"<notationDiscussion>" +
								"<para xml:lang=\"en\">" +
									"<span>This is some basic text.</span>" +
								"</para>" +
							"</notationDiscussion>" +
						"</annotation>";

			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));

			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}
		#endregion

		#region ExportBookSection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportBookSection for an introduction section that includes annotations
		/// (TE-7647).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBookSection_IntroWithNotes()
		{
			IScrSection introSection = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(introSection.Hvo, "Introduction",
				ScrStyleNames.IntroSectionHead);
			StTxtPara introPara = AddPara(introSection, ScrStyleNames.NormalParagraph);
			AddVerse(introPara, 0, 0, "Some intro text");
			IScrScriptureNote note = m_scrInMemoryCache.AddAnnotation(introPara, new BCVRef(01001000),
				NoteType.Consultant, "This is a basic note about the intro.");
			note.ResolutionStatus = NoteStatus.Open;
			note.BeginOffset = 5;
			note.EndOffset = 10;

			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			writer.Formatting = Formatting.None;
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportBookSection_IntroWithNotes");

			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			string sCanon = "ot";
			ReflectionHelper.CallMethod(m_exporter, "ExportBook", sCanon, m_book, null);


			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));
			string strExpected = "<book ID=\"GEN\"><titleGroup short=\"Genesis\"><title type=\"main\">" +
				"<trGroup><tr>Genesis</tr></trGroup></title></titleGroup>" +
				"<introduction><section><sectionHead><trGroup><tr>Introduction</tr></trGroup></sectionHead>" +
				"<p><annotation type=\"consultantNote\" status=\"0\" oxesRef=\"GEN.1.0\" beginOffset=\"5\" endOffset=\"10\">" +
				"<created>" + strNow + "</created><modified>" + strNow + "</modified>" +
				"<resolved /><notationDiscussion><para xml:lang=\"en\">" +
				"<run>This is a basic note about the intro.</run></para></notationDiscussion></annotation>" +
				"<trGroup><tr><userCS type=\"Paragraph\">Some intro text</userCS></tr></trGroup>" +
				"<chapterEnd ID=\"GEN.0\"/></p></section></introduction></book>";

			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportBookSection for a back translation of an introduction section that
		/// includes annotations (TE-7647).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBookSection_BTIntroWithNotes()
		{
			int wsAnal = Cache.DefaultAnalWs;
			IScrSection introSection = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(introSection.Hvo, "Introduccion",
				ScrStyleNames.IntroSectionHead);
			StTxtPara introPara = AddPara(introSection, ScrStyleNames.NormalParagraph);
			AddVerse(introPara, 0, 0, "Texto de introduccion");
			CmTranslation trans = (CmTranslation)m_scrInMemoryCache.AddTransToMockedParagraph(introPara,
				LangProject.kguidTranBackTranslation, wsAnal);
			trans.Translation.SetAlternative("Introduction text", wsAnal);
			ScrScriptureNote note = (ScrScriptureNote)m_scrInMemoryCache.AddAnnotation(trans,
				01001000, NoteType.Consultant, "This is a basic note about the BT of an intro.");
			note.ResolutionStatus = NoteStatus.Open;
			note.BeginOffset = 0;
			note.EndOffset = 5;
			note.WsSelector = wsAnal;

			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			writer.Formatting = Formatting.None;
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportBookSection_IntroWithNotes");
			// Initialize writing systems for export (required to export back translations), but
			// called from ExportTE.
			ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			string sCanon = "ot";
			ReflectionHelper.CallMethod(m_exporter, "ExportBook", sCanon, m_book, null);

			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));
			string strExpected = "<book ID=\"GEN\"><titleGroup short=\"Genesis\"><title type=\"main\">" +
				"<trGroup><tr>Genesis</tr></trGroup></title></titleGroup>" +
				"<introduction><section><sectionHead><trGroup><tr>Introduction</tr></trGroup></sectionHead>" +
				"<p><annotation type=\"consultantNote\" status=\"0\" oxesRef=\"GEN.1.0\" beginOffset=\"0\" " +
				"endOffset=\"5\" languageInFocus=\"en\">" +
				"<created>" + strNow + "</created><modified>" + strNow + "</modified>" +
				"<resolved /><notationDiscussion><para xml:lang=\"en\">" +
				"<run>This is a basic note about the BT of an intro.</run></para></notationDiscussion></annotation>" +
				"<trGroup><tr><userCS type=\"Paragraph\">Some intro text</userCS></tr>" +
				"<bt xml:lang=\"en\">Texto de introduccion</bt></trGroup>" +
				"<chapterEnd ID=\"GEN.0\"/></p></section></introduction></book>";

			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportBookSection for multiple back translation with different statuses (TE-8342).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBookSection_MultipleBT()
		{
			Cache.LangProject.AnalysisWssRC.Add(InMemoryFdoCache.s_wsHvos.Hbo);
			Cache.LangProject.CurAnalysisWssRS.Append(InMemoryFdoCache.s_wsHvos.Hbo);
			int wsAnal = Cache.DefaultAnalWs;
			IScrSection introSection = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(introSection.Hvo, "Introduccion",
				ScrStyleNames.IntroSectionHead);
			StTxtPara introPara = AddPara(introSection, ScrStyleNames.NormalParagraph);
			AddVerse(introPara, 0, 0, "Texto de introduccion");

			// Add a back translation for the default analysis language.
			CmTranslation trans = (CmTranslation)m_scrInMemoryCache.AddTransToMockedParagraph(introPara,
				LangProject.kguidTranBackTranslation, wsAnal);
			trans.Translation.SetAlternative("Default BT", wsAnal);
			trans.Status.SetAlternative("finished", wsAnal);

			// Add a back translation for Hebrew.
			trans.Translation.SetAlternative("Hbo BT", InMemoryFdoCache.s_wsHvos.Hbo);

			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			writer.Formatting = Formatting.None;
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportBookSection_IntroWithNotes");
			// Initialize writing systems for export (required to export back translations), but
			// called from ExportTE.
			ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
			ReflectionHelper.SetField(m_exporter, "m_writer", writer);
			string sCanon = "ot";
			ReflectionHelper.CallMethod(m_exporter, "ExportBook", sCanon, m_book, null);

			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));
			string strExpected = "<book ID=\"GEN\"><titleGroup short=\"Genesis\"><title type=\"main\">" +
				"<trGroup><tr>Genesis</tr></trGroup></title></titleGroup>" +
				"<introduction><section><sectionHead><trGroup><tr>Introduction</tr></trGroup></sectionHead>" +
				"<p><trGroup><tr><userCS type=\"Paragraph\">Some intro text</userCS></tr>" +
				"<bt xml:lang=\"en\" status=\"finished\">Default BT</bt><bt xml:lang=\"hbo\">Hbo BT</bt></trGroup>" +
				"<chapterEnd ID=\"GEN.0\"/></p></section></introduction></book>";

			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}
		#endregion

		#region ExportPicture Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportPicture when the caption uses multiple writing systems
		/// (TE-7674).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPicture_MultipleWS()
		{
			// Create a picture that has a caption with three different writing systems.
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, InMemoryFdoCache.s_wsHvos.De);
			tssBldr.ReplaceRgch(0, 0, "photo", 5, propsBldr.GetTextProps());
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, InMemoryFdoCache.s_wsHvos.Ur);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " tasvir", 7, propsBldr.GetTextProps());
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, Cache.DefaultVernWs);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " picture caption", 16, propsBldr.GetTextProps());
			CmPicture picture = new CmPicture(m_inMemoryCache.Cache, @"c:\filename.jpg", tssBldr.GetString(), "folder");

			// Set up for export
			StringWriter stream = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(stream);
			writer.Formatting = Formatting.None;
			m_exporter = new ExportXml(null, Cache, null, ExportWhat.AllBooks, 0, 0, 0,
				"ExportPicture_MultipleWS");
			ReflectionHelper.SetField(m_exporter, "m_writer", writer);

			// Export the picture to XML
			ReflectionHelper.CallMethod(m_exporter, "ExportPicture", picture);

			// Check the results of the exported picture.
			//   Set up expected results.
			string defaultDir = DirectoryFinder.FWDataDirectory;
			// TODO (TE-7756): Support OXES export and import of new properties that have been
			// added to the CmPicture model
			string strExpected = "<figure src=\"filename.jpg\"><!--path=\"" + defaultDir + "\\filename\"-->" +
				"<caption><trGroup><tr><foreign xml:lang=\"de\">photo</foreign>" +
				"<foreign xml:lang=\"ur\"> tasvir</foreign> picture caption</tr></trGroup></caption></figure>";
			XmlDocument expected = new XmlDocument();
			expected.Load(new StringReader(strExpected));

			//   Get actual results.
			XmlDocument actual = new XmlDocument();
			actual.Load(new StringReader(stream.ToString()));

			string strDifference;
			if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
				Assert.Fail(strDifference);
		}


		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the given child node.
		/// </summary>
		/// <param name="parentNode">The parent node of the child to look in.</param>
		/// <param name="sNodeName">Name of the child node to find.</param>
		/// <returns>The requested node</returns>
		/// ------------------------------------------------------------------------------------
		private static XmlNode FindChildNode(XmlNode parentNode, string sNodeName)
		{
			foreach (XmlNode node in parentNode.ChildNodes)
			{
				if (node.Name == sNodeName)
					return node;
			}
			throw new Exception("Node " + sNodeName + " not found in node " + parentNode.Name);
		}
		#endregion
	}
	#endregion
}
