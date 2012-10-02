// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2008' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportXmlTests.cs
// ---------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using SIL.OxesIO;

namespace SIL.FieldWorks.TE.ExportTests
{
	#region ExportXmlTestsWithValidation
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ExportXml
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportXmlTestsWithValidation : ScrInMemoryFdoTestBase
	{
		private ExportXml m_exporter;
		string m_fileName;

		#region setup,teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the ScrReference for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FileUtils.Manager.SetFileAdapter(new MockFileOS());
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			FileUtils.Manager.Reset();
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_fileName = FileUtils.GetTempFile("tmp");
			FilteredScrBooks filter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(123);
			FileUtils.Delete(m_fileName);	// exporter pops up dialog if file exists!
			m_exporter = new ExportXml(m_fileName, Cache, filter, null, ExportWhat.AllBooks, 0, 0, 0,
				"This is a test");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			CreateBookData(57, "Philemon");
			CreateBookData(59, "James");
			CreateBookData(65, "Jude");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test teardown
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_exporter = null;
			FileUtils.Delete(m_fileName);
			m_fileName = null;
			base.TestTearDown();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a file, then validate it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportAndValidate()
		{
			// Export.
			m_exporter.Run(null);
			// Validate.
			string errors = Validator.GetAnyValidationErrors(m_fileName);
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

		#region setup,teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FileUtils.Manager.SetFileAdapter(new MockFileOS());

			// Define writing systems.
			IWritingSystem wsHbo;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("hbo", out wsHbo);
			IWritingSystem wsDe;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out wsDe);
			IWritingSystem wsUr;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ur", out wsUr);

			// Define Scripture checks.
			ICmAnnotationDefn annDefnChkError =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().CheckingError;

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				// Set up additional analysis writing systems.
				wsHbo.RightToLeftScript = true;
				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsHbo);
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Add(wsHbo);

				// Set up Scripture check.
				ICmAnnotationDefn repeatedWordsCheck =
					Cache.ServiceLocator.GetInstance<ICmAnnotationDefnFactory>().Create(
					StandardCheckIds.kguidRepeatedWords, annDefnChkError);
			});
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			FileUtils.Manager.Reset();
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_fileName = FileUtils.GetTempFile("tmp");
			FileUtils.Delete(m_fileName);	// exporter pops up dialog if file exists!
			m_stylesheet = new FwStyleSheet();
			m_stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			m_book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_book, "Genesis");

			m_filter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(123);

			m_filter.ShowAllBooks();

			m_exporter = new ExportXml(m_fileName, Cache, m_filter, null, ExportWhat.AllBooks, 1, 0, 0,
				string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// lears member variables, cleans up temp files, shuts down the cache, etc.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_exporter = null;
			m_filter = null;
			if (m_fileName != null)
				FileUtils.Delete(m_fileName);
			m_stylesheet = null;
			m_fileName = null;

			base.TestTearDown();
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
			IStStyle sectionHead = m_scr.FindStyle("Section Head");
			int hvoNewStyle = m_stylesheet.MakeNewStyle();
			m_stylesheet.PutStyle("Major Massive Huge Section Head", "Use for little tiny sections",
				hvoNewStyle, sectionHead.Hvo, hvoNewStyle, (int)StyleType.kstParagraph, false,
				false, sectionHead.Rules);

			// Create section head with this "user-defined" style.
			IScrSection section = AddSectionToMockedBook(m_book);
			AddSectionHeadParaToSection(section,
				"This is a user-defined section head", "Major Massive Huge Section Head");
			AddParaToMockedSectionContent(section, "Paragraph");

			// Export.
			m_exporter.Run(null);

			// Validate.
			string errors = Validator.GetAnyValidationErrors(m_fileName);
			Assert.IsNull(errors);

			// Read file and compare to expected results. The first node inside sectionHead should
			// be trGroup.
			XmlDocument xmlDoc = new XmlDocument();
			using (TextReader tr = FileUtils.OpenFileForRead(m_fileName, Encoding.UTF8))
				xmlDoc.Load(tr);
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
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some basic text.");

			using (StringWriter stream = new StringWriter())
			{
				using (XmlTextWriter writer = new XmlTextWriter(stream))
				{
					m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
						"ExportNotationParagraphs_Basic");

					ReflectionHelper.SetField(m_exporter, "m_writer", writer);
					ReflectionHelper.CallMethod(m_exporter, "ExportNotationParagraphs", note.DiscussionOA,
						"notationDiscussion");
					Assert.AreEqual("<notationDiscussion><para xml:lang=\"en\">" +
						"<span>This is some basic text.</span>" +
						"</para></notationDiscussion>", stream.ToString());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportNotationParagraphs when the text contains a character style (TE-7461).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportNotationParagraphs_CharStyle()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some text with emphasis style.");
			ITsStrBldr bldr = ((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.GetBldr();
			bldr.SetProperties(bldr.Length - 15, bldr.Length - 1,
				StyleUtils.CharStyleTextProps(ScrStyleNames.Emphasis, Cache.DefaultAnalWs));
			((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents = bldr.GetString();

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
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
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some text with espa\u00F1ol.");
			ITsStrBldr bldr = ((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.GetBldr();
			int wsEs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("es");
			bldr.SetProperties(bldr.Length - 9, bldr.Length - 1,
				StyleUtils.CharStyleTextProps(null, wsEs));
			((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents = bldr.GetString();

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportNotationParagraphs when the text contains a hyperlink (TE-7461).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportNotationParagraphs_Hyperlink()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = AddAnnotation(para, new BCVRef(01001001),
				NoteType.Translator, "This is some text with a hyperlink!");
			ITsStrBldr bldr = ((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.GetBldr();

			var hyperlinkStyle = m_stylesheet.FindStyle(StyleServices.Hyperlink);
			Assert.IsNotNull(hyperlinkStyle);
			StringServices.MarkTextInBldrAsHyperlink(bldr, bldr.Length - 10, bldr.Length - 1,
				"http://www.myspace.com", hyperlinkStyle);
			((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents = bldr.GetString();

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: mono bug https://bugzilla.novell.com/show_bug.cgi?id=594490")]
		public void ExportScriptureNote_Unresolved()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = AddAnnotation(para, new BCVRef(01001001),
				NoteType.Consultant, "This is some basic text.");
			note.ResolutionStatus = NoteStatus.Open;
			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;
			//note.DateResolved = now;

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportScriptureNote for an Resolved annotation, verifying it exports the
		/// correct resolved date (TE-7511).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: mono bug https://bugzilla.novell.com/show_bug.cgi?id=594490")]
		public void ExportScriptureNote_Resolved()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = AddAnnotation(para, new BCVRef(01001001),
				NoteType.Consultant, "This is some basic text.");
			note.ResolutionStatus = NoteStatus.Closed;
			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;
			note.DateResolved = now;

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: mono bug https://bugzilla.novell.com/show_bug.cgi?id=594490")]
		public void ExportScripture_NotesOutOfOrder()
		{
			IScrSection section = AddSectionToMockedBook(m_book);

			// Create 2 paragraphs
			IStTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IStTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			// Add an annotation to the second paragraph. Ref.
			// GEN 1 means annotation is in an introduction paragraph.
			IScrScriptureNote note1 = AddAnnotation(
				para2, new BCVRef(01001000), NoteType.Consultant, "Annotation1 for Para2");

			// Add an annotation to the first paragraph.
			// GEN 1 means annotation is in an introduction paragraph.
			IScrScriptureNote note2 = AddAnnotation(
				para1, new BCVRef(01001000), NoteType.Consultant, "Annotation2 for Para1");

			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note1.DateCreated = note2.DateCreated = now;
			note1.DateModified = note2.DateModified = now;
			note1.DateResolved = note2.DateResolved = now;

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.WriteStartElement("TestData");

				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportScripture_NotesOutOfOrder");

				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				ReflectionHelper.SetField(m_exporter, "m_iCurrentBook", 1);
				ReflectionHelper.CallMethod(m_exporter, "ExportAnnotationsForObj", para1.Hvo);
				ReflectionHelper.CallMethod(m_exporter, "ExportAnnotationsForObj", para2.Hvo);

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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportScriptureNote for an ignored checking error, verifying it exports the
		/// correctly (TE-8292).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: mono bug https://bugzilla.novell.com/show_bug.cgi?id=594490")]
		public void ExportScriptureNote_IgnoredCheckingError()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IScrScriptureNote note = AddAnnotation(para, new BCVRef(01001001),
				NoteType.CheckingError, "This is some basic text.");

			note.AnnotationTypeRA =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(StandardCheckIds.kguidRepeatedWords);
			note.ResolutionStatus = NoteStatus.Closed;
			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;
			note.DateResolved = now;

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportScriptureNote_IgnoredCheckingError");

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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportScriptureNote for an annotation with an unknown writing system
		/// (FWR-2203).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: mono bug https://bugzilla.novell.com/show_bug.cgi?id=594490")]
		public void ExportScriptureNote_UnknownWsForBt()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			ICmTranslation trans = AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			IScrScriptureNote note = AddAnnotation(trans, new BCVRef(01001001),
				NoteType.Consultant, "This is some basic text.");
			note.ResolutionStatus = NoteStatus.Open;
			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;
			note.WsSelector = -1;
			// invalid writing system id

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportScriptureNote_UnknownWsForBt");

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
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: mono bug https://bugzilla.novell.com/show_bug.cgi?id=594490")]
		public void ExportBookSection_IntroWithNotes()
		{
			IScrSection introSection = AddSectionToMockedBook(m_book, true);
			AddSectionHeadParaToSection(introSection, "Introduction",
				ScrStyleNames.IntroSectionHead);
			IScrTxtPara introPara = AddParaToMockedSectionContent(introSection, ScrStyleNames.NormalParagraph);
			AddVerse(introPara, 0, 0, "Some intro text");
			IScrScriptureNote note = AddAnnotation(introPara, new BCVRef(01001000),
				NoteType.Consultant, "This is a basic note about the intro.");
			note.ResolutionStatus = NoteStatus.Open;
			note.BeginOffset = 5;
			note.EndOffset = 10;

			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;

			using (StringWriter stream = new StringWriter())
			{
				using (XmlTextWriter writer = new XmlTextWriter(stream))
				{
					writer.Formatting = Formatting.None;
					m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
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
							"<p><annotation type=\"consultantNote\" status=\"0\" oxesRef=\"GEN.1.0\" beginOffset=\"5\" endOffset=\"10\">" +
							"<created>" + strNow + "</created><modified>" + strNow + "</modified>" +
							"<resolved /><notationDiscussion><para xml:lang=\"en\">" +
							"<span>This is a basic note about the intro.</span></para></notationDiscussion></annotation>" +
							"<trGroup><tr>Some intro text</tr></trGroup>" +
							"</p></section></introduction></book>";

					XmlDocument expected = new XmlDocument();
					expected.Load(new StringReader(strExpected));

					string strDifference;
					if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
						Assert.Fail(strDifference);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportBookSection for a back translation of an introduction section that
		/// includes annotations (TE-7647).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: mono bug https://bugzilla.novell.com/show_bug.cgi?id=594490")]
		public void ExportBookSection_BTIntroWithNotes()
		{
			int wsAnal = Cache.DefaultAnalWs;
			IScrSection introSection = AddSectionToMockedBook(m_book, true);
			AddSectionHeadParaToSection(introSection, "Introduccion", ScrStyleNames.IntroSectionHead);
			IScrTxtPara introPara = AddParaToMockedSectionContent(introSection, ScrStyleNames.NormalParagraph);
			AddVerse(introPara, 0, 0, "Texto de introduccion");
			ICmTranslation trans = AddBtToMockedParagraph(introPara, wsAnal);
			trans.Translation.set_String(wsAnal, TsStringUtils.MakeTss("Introduction text", wsAnal));
			IScrScriptureNote note = AddAnnotation(trans, 01001000, NoteType.Consultant,
				"This is a basic note about the BT of an intro.");
			note.ResolutionStatus = NoteStatus.Open;
			note.BeginOffset = 0;
			note.EndOffset = 5;
			note.WsSelector = wsAnal;

			DateTime now = DateTime.Now;
			string strNow = now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff");
			note.DateCreated = now;
			note.DateModified = now;

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportBookSection_BTIntroWithNotes");
				// Initialize writing systems for export (required to export back translations), but
				// called from ExportTE.
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				string sCanon = "ot";
				ReflectionHelper.CallMethod(m_exporter, "ExportBook", sCanon, m_book, null);

				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));
				// Why is the expected text for the translation in English?
				// The BT is in English, but it is Spanish text that was added as a vernacular.
				string strExpected = "<book ID=\"GEN\"><titleGroup short=\"Genesis\"><title type=\"main\">" +
					"<trGroup><tr>Genesis</tr></trGroup></title></titleGroup>" +
					"<introduction><section><sectionHead><trGroup><tr>Introduccion</tr></trGroup></sectionHead>" +
					"<p><trGroup><tr>Texto de introduccion</tr>" +
					"<bt xml:lang=\"en\" segmented=\"true\">Introduction text</bt></trGroup>" +
					"<annotation type=\"consultantNote\" status=\"0\" oxesRef=\"GEN.1.0\" beginOffset=\"0\" " +
					"endOffset=\"5\">" +
					"<created>" + strNow + "</created><modified>" + strNow + "</modified>" +
					"<resolved /><notationDiscussion><para xml:lang=\"en\">" +
					"<span>This is a basic note about the BT of an intro.</span></para></notationDiscussion></annotation>" +
					"</p></section></introduction></book>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportBookSection for multiple back translation with different statuses (TE-8342).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBookSection_MultipleBT()
		{
			IWritingSystem lgHbo = Cache.ServiceLocator.WritingSystemManager.Get("hbo");
			int wsAnal = Cache.DefaultAnalWs;
			IScrSection introSection = AddSectionToMockedBook(m_book, true);
			AddSectionHeadParaToSection(introSection, "Introduccion", ScrStyleNames.IntroSectionHead);
			IScrTxtPara introPara = AddParaToMockedSectionContent(introSection, ScrStyleNames.NormalParagraph);
			AddVerse(introPara, 0, 0, "Texto de introduccion");

			// Add a back translation for the default analysis language.
			ICmTranslation trans = AddBtToMockedParagraph(introPara, wsAnal);
			trans.Translation.set_String(wsAnal, TsStringUtils.MakeTss("Default BT", wsAnal));
			trans.Status.set_String(wsAnal, TsStringUtils.MakeTss("finished", wsAnal));

			// Add a back translation for Hebrew.
			trans.Translation.set_String(lgHbo.Handle, TsStringUtils.MakeTss("Hbo BT", lgHbo.Handle));

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportBookSection_MultipleBT");
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
						"<introduction><section><sectionHead><trGroup><tr>Introduccion</tr></trGroup></sectionHead>" +
						"<p><trGroup><tr>Texto de introduccion</tr>" +
						"<bt xml:lang=\"en\" segmented =\"true\" status=\"finished\">Default BT</bt>" +
						"<bt xml:lang=\"hbo\" segmented =\"true\">Hbo BT</bt></trGroup>" +
						"</p></section></introduction></book>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}
		#endregion

		#region ExportParagraph tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ExportsParagraph with footnotes in vernacular and back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportParagraph_WithFootnotes()
		{
			// Set up vernacular with translation with footnotes that are in the same order as in the
			// back translation.
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "Mera bhai ne mudj se bataya.");
			IScrFootnote footnote1 = AddFootnote(m_book, para, "11Mera bhai".Length, "Yohanna");
			IScrFootnote footnote2 = AddFootnote(m_book, para, para.Contents.Length, "bat ki");
			// Set up back translation.
			ICmTranslation trans = AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "My brother told me.", null);
			ITsStrBldr tssTransBldr = trans.Translation.get_String(Cache.DefaultAnalWs).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				"11My brother told".Length, "11My brother told".Length, Cache.DefaultAnalWs);
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				"11My brother".Length, "11My brother".Length, Cache.DefaultAnalWs);
			trans.Translation.set_String(Cache.DefaultAnalWs, tssTransBldr.GetString());
			ICmTranslation transFootnote = ((IStTxtPara)footnote1.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "John");
			transFootnote = ((IStTxtPara)footnote2.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "said");

			// Export the paragraph
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportParagraph_WithFootnotes");
				// Initialize writing systems for export (required to export back translations), but
				// called from ExportTE.
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);
				ReflectionHelper.CallMethod(m_exporter, "ExportParagraph", para, null, true);

				// Confirm the results.
				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));
				string strExpected = "<p><chapterStart ID=\"GEN.1\" n=\"1\" /><verseStart ID=\"GEN.1.1\" n=\"1\" />" +
					"<trGroup><tr>Mera bhai<note noteID=\"fGEN1\" type=\"general\" canonical=\"true\">" +
						"<trGroup><tr>Yohanna</tr><bt xml:lang=\"en\">John</bt></trGroup></note> ne mudj se bataya." +
						"<note noteID=\"fGEN2\" type=\"general\" canonical=\"true\">" +
						"<trGroup><tr>bat ki</tr><bt xml:lang=\"en\">said</bt>" +
						"</trGroup></note></tr>" +
						"<bt xml:lang=\"en\" segmented =\"true\">My brother<note noteRef=\"fGEN1\"/> told" +
						"<note noteRef=\"fGEN2\"/> me.</bt></trGroup>" +
						"<verseEnd ID=\"GEN.1.1\" /><chapterEnd ID=\"GEN.1\" /></p>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ExportsParagraph with footnotes in vernacular and back translation when the footnotes
		/// are in a different order in the back translation because of grammatical word order
		/// differences between the vernacular and analysis languages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportParagraph_FootnotesInBtWithDifferentOrder()
		{
			// Set up vernacular with translation with footnotes that are in a different order
			// than in the back translation.
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "Me dijo mi mama que si.");
			IScrFootnote footnote1 = AddFootnote(m_book, para, "11Me dijo".Length, "informo");
			IScrFootnote footnote2 = AddFootnote(m_book, para, "11Me dijoX mi mama".Length, "madre");
			// Set up back translation.
			ICmTranslation trans = AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "My mom told me that yes.", null);
			ITsStrBldr tssTransBldr = trans.Translation.get_String(Cache.DefaultAnalWs).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				"11My mom told".Length, "11My mom told".Length, Cache.DefaultAnalWs);
			TsStringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				"11My mom".Length, "11My mom".Length, Cache.DefaultAnalWs);
			trans.Translation.set_String(Cache.DefaultAnalWs, tssTransBldr.GetString());
			ICmTranslation transFootnote = ((IStTxtPara)footnote1.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "inform");
			transFootnote = ((IStTxtPara)footnote2.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "mother");

			// Export the paragraph
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportParagraph_FootnotesInBtWithDifferentOrder");
				// Initialize writing systems for export (required to export back translations), but
				// called from ExportTE.
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);
				ReflectionHelper.CallMethod(m_exporter, "ExportParagraph", para, null, true);

				// Confirm the results.
				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));
				string strExpected = "<p><chapterStart ID=\"GEN.1\" n=\"1\" /><verseStart ID=\"GEN.1.1\" n=\"1\" />" +
					"<trGroup><tr>Me dijo<note noteID=\"fGEN1\" type=\"general\" canonical=\"true\">" +
						"<trGroup><tr>informo</tr><bt xml:lang=\"en\">inform</bt></trGroup></note> mi mama" +
						"<note noteID=\"fGEN2\" type=\"general\" canonical=\"true\">" +
						"<trGroup><tr>madre</tr><bt xml:lang=\"en\">mother</bt>" +
						"</trGroup></note> que si.</tr>" +
						"<bt xml:lang=\"en\" segmented =\"true\">My mom<note noteRef=\"fGEN2\"/> told" +
						"<note noteRef=\"fGEN1\"/> me that yes.</bt></trGroup>" +
						"<verseEnd ID=\"GEN.1.1\" /><chapterEnd ID=\"GEN.1\" /></p>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// With a segmented back translation, test ExportsParagraph with footnotes in
		/// vernacular and back translation when the footnotes are in a different order in the
		/// back translation because of grammatical word order differences between the
		/// vernacular and analysis languages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportParagraph_FootnotesInBtWithDifferentOrder_Segmented()
		{
			// Set up vernacular with translation with footnotes that are in a different order
			// than in the back translation.
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "Me dijo mi mama que si.");
			IScrFootnote footnote1 = AddFootnote(m_book, para, "11Me dijo".Length, "informo");
			IScrFootnote footnote2 = AddFootnote(m_book, para, "11Me dijoX mi mama".Length, "madre");
			// Set up back translation.
			ICmTranslation trans = AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "My mom told me that yes.", null);
			ITsStrBldr tssTransBldr = trans.Translation.get_String(Cache.DefaultAnalWs).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				"11My mom told".Length, "11My mom told".Length, Cache.DefaultAnalWs);
			TsStringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				"11My mom".Length, "11My mom".Length, Cache.DefaultAnalWs);
			trans.Translation.set_String(Cache.DefaultAnalWs, tssTransBldr.GetString());
			ICmTranslation transFootnote = ((IStTxtPara)footnote1.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "inform");
			transFootnote = ((IStTxtPara)footnote2.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "mother");

			// Export the paragraph with segmented back translation turned on.
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportParagraph_FootnotesInBtWithDifferentOrder_Segmented");
				// Initialize writing systems for export (required to export back translations), but
				// called from ExportTE.
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);
				ReflectionHelper.CallMethod(m_exporter, "ExportParagraph", para, null, true);

				// Confirm the results.
				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));
				string strExpected = "<p><chapterStart ID=\"GEN.1\" n=\"1\" /><verseStart ID=\"GEN.1.1\" n=\"1\" />" +
					"<trGroup><tr>Me dijo<note noteID=\"fGEN1\" type=\"general\" canonical=\"true\">" +
						"<trGroup><tr>informo</tr><bt xml:lang=\"en\">inform</bt></trGroup></note> mi mama" +
						"<note noteID=\"fGEN2\" type=\"general\" canonical=\"true\">" +
						"<trGroup><tr>madre</tr><bt xml:lang=\"en\">mother</bt>" +
						"</trGroup></note> que si.</tr>" +
						"<bt xml:lang=\"en\" segmented=\"true\">My mom<note noteRef=\"fGEN2\"/> told" +
						"<note noteRef=\"fGEN1\"/> me that yes.</bt></trGroup>" +
						"<verseEnd ID=\"GEN.1.1\" /><chapterEnd ID=\"GEN.1\" /></p>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// With a segmented back translation, test ExportsParagraph with multiple segments
		/// in a single verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportParagraph_MultipleSegmentsInVerse()
		{
			// Set up vernacular with translation with multiple translations.
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "is jumle me kam alfaz he. us jumla ke alava, ek aur jumla mojud he.");
			// Set up back translation.
			ICmTranslation trans = AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.VerseNumber);
			AddSegmentFt(para, 1, "In this sentence, there are few words. ", Cache.DefaultAnalWs);
			AddSegmentFt(para, 2, "Besides that sentence, one more sentence is present.", Cache.DefaultAnalWs);

			// Export the paragraph with segmented back translation turned on.
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportParagraph_MultipleSegmentsInVerse");
				// Initialize writing systems for export (required to export back translations), but
				// called from ExportTE.
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);
				ReflectionHelper.CallMethod(m_exporter, "ExportParagraph", para, null, true);

				// Confirm the results.
				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));
				string strExpected = "<p><chapterStart ID=\"GEN.1\" n=\"1\" /><verseStart ID=\"GEN.1.1\" n=\"1\" />" +
					"<trGroup><tr>is jumle me kam alfaz he. </tr>" +
						"<bt xml:lang=\"en\" segmented=\"true\">In this sentence, there are few words. </bt></trGroup>" +
						"<trGroup><tr>us jumla ke alava, ek aur jumla mojud he.</tr>" +
						"<bt xml:lang=\"en\" segmented=\"true\">Besides that sentence, one more sentence is present.</bt></trGroup>" +
						"<verseEnd ID=\"GEN.1.1\" /><chapterEnd ID=\"GEN.1\" /></p>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// With a segmented back translation, test ExportsParagraph with text that begins with
		/// a space and is formatted with a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportParagraph_CharStyleStartingWithSpace()
		{
			// Set up vernacular with translation with multiple translations.
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 5, 14, string.Empty);
			AddRunToMockedPara(para, " Imwe ndimwe cedza ca pa dziko. ", ScrStyleNames.Emphasis);
			// Set up back translation.
			ICmTranslation trans = AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.VerseNumber);
			AddSegmentFt(para, 1, "Voces sois a luz do mundo. ", Cache.DefaultAnalWs);

			// Export the paragraph with segmented back translation turned on.
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportParagraph_CharStyleStartingWithSpace");
				// Initialize writing systems for export (required to export back translations), but
				// called from ExportTE.
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);
				ReflectionHelper.CallMethod(m_exporter, "ExportParagraph", para, null, true);

				// Confirm the results.
				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));
				string strExpected = "<p><chapterStart ID=\"GEN.5\" n=\"5\" /><verseStart ID=\"GEN.5.14\" n=\"14\" />" +
					"<labelTr><emphasis> </emphasis></labelTr>" +
						"<trGroup><tr><emphasis>Imwe ndimwe cedza ca pa dziko. </emphasis></tr>" +
						"<bt xml:lang=\"en\" segmented=\"true\">Voces sois a luz do mundo. </bt></trGroup>" +
						"<verseEnd ID=\"GEN.5.14\" /><chapterEnd ID=\"GEN.5\" /></p>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// With a segmented back translation, test ExportsParagraph with multiple segments
		/// in a single verse when there are footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportParagraph_MultipleSegmentsInVerseWithFootnotes()
		{
			// Set up vernacular with translation with multiple translations.
			IScrBook james = AddBookToMockedScripture(59, "James");
			IScrSection section = AddSectionToMockedBook(james);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "From James, a servant of the Lord Jesus Christ. Greetings to all God's people. ");
			IScrFootnote footnote1 = AddFootnote(james, para, 2, "footnote 1");
			// add after chapter verse
			IScrFootnote footnote2 = AddFootnote(james, para, "11XFrom James".Length, "footnote 2");
			// X is a spaceholder for footnote

			// Set up back translation.
			ICmTranslation trans = AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "1", ScrStyleNames.VerseNumber);
			AddSegmentFt(para, 1, "Von Jakobus, ein Knecht des Herrn Jesus Christus. ", Cache.DefaultAnalWs);
			AddSegmentFt(para, 2, "GruBe an das ganze Volk Gottes. ", Cache.DefaultAnalWs);
			ITsStrBldr tssTransBldr = trans.Translation.get_String(Cache.DefaultAnalWs).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				2, 2, Cache.DefaultAnalWs);
			TsStringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtNameGuidHot, tssTransBldr,
				"11XVon Jakobus".Length, "11XVon Jakobus".Length, Cache.DefaultAnalWs);
			trans.Translation.set_String(Cache.DefaultAnalWs, tssTransBldr.GetString());
			ICmTranslation transFootnote = ((IStTxtPara)footnote1.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "fuBnote 1");
			transFootnote = ((IStTxtPara)footnote2.ParagraphsOS[0]).GetOrCreateBT();
			transFootnote.Translation.set_String(Cache.DefaultAnalWs, "fuBnote 2");

			// Export the paragraph with segmented back translation turned on.
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.SingleBook, 59, 0, 0,
					"ExportParagraph_MultipleSegmentsInVerseWithFootnotes");
				// Initialize writing systems for export (required to export back translations), but
				// called from ExportTE.
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(james);
				ReflectionHelper.CallMethod(m_exporter, "ExportParagraph", para, null, true);

				// Confirm the results.
				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));
				string strExpected = "<p><chapterStart ID=\"JAS.1\" n=\"1\" /><verseStart ID=\"JAS.1.1\" n=\"1\" />" +
					"<trGroup><tr><note noteID=\"fJAS1\" type=\"general\" canonical=\"true\"><trGroup><tr>footnote 1</tr>" +
						"<bt xml:lang=\"en\">fuBnote 1</bt></trGroup></note>" +
						"From James<note noteID=\"fJAS2\" type=\"general\" canonical=\"true\"><trGroup><tr>footnote 2</tr>" +
						"<bt xml:lang=\"en\">fuBnote 2</bt></trGroup></note>" +
						", a servant of the Lord Jesus Christ. </tr>" +
						"<bt xml:lang=\"en\" segmented=\"true\"><note noteRef=\"fJAS1\"/>Von Jakobus" +
						"<note noteRef=\"fJAS2\"/>, " +
						"ein Knecht des Herrn Jesus Christus. </bt></trGroup>" +
						"<trGroup><tr>Greetings to all God's people. </tr>" +
						"<bt xml:lang=\"en\" segmented=\"true\">GruBe an das ganze Volk Gottes. </bt></trGroup>" +
						"<verseEnd ID=\"JAS.1.1\" /><chapterEnd ID=\"JAS.1\" /></p>";

				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
		}
		#endregion

		#region WriteVernSegment tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests WriteVernSegment when there is a space in the middle of a label segment (i.e.
		/// when there is a space between verses).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteVernSegment_SpaceBetweenVerses()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append("1", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			bldr.Append(" ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Append("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);

				ReflectionHelper.CallMethod(m_exporter, "WriteVernSegment", bldr.GetString(), true);

				// Confirm the results.
				string strExpected = "<verseStart ID=\"GEN.1.1\" n=\"1\" /><labelTr> </labelTr>" + Environment.NewLine +
					"<verseEnd ID=\"GEN.1.1\" />" + Environment.NewLine + "<verseStart ID=\"GEN.1.2\" n=\"2\" />";
				Assert.AreEqual(strExpected, stream.ToString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests WriteVernSegment when there is a space at the start of a label segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteVernSegment_SpaceAtStartOfLabel()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append(" ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Append("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);

				ReflectionHelper.CallMethod(m_exporter, "WriteVernSegment", bldr.GetString(), true);

				// Confirm the results.
				string strExpected = "<labelTr> </labelTr>" + Environment.NewLine +
					"<verseStart ID=\"GEN.1.2\" n=\"2\" />";
				Assert.AreEqual(strExpected, stream.ToString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests WriteVernSegment when there is a non-wordforming character at the start of a
		/// label segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteVernSegment_NonWordFormingAtStartOfLabel()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append("(^%$#@[", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Append("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);

				ReflectionHelper.CallMethod(m_exporter, "WriteVernSegment", bldr.GetString(), true);

				// Confirm the results.
				string strExpected = "<labelTr>(^%$#@[</labelTr>" + Environment.NewLine +
					"<verseStart ID=\"GEN.1.2\" n=\"2\" />";
				Assert.AreEqual(strExpected, stream.ToString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests WriteVernSegment when there are multiple spaces at the end of a label segment
		/// in different character styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteVernSegment_MultipleSpacesMultipleCharStyles()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append("1", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			bldr.Append(" ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.Append(" ", StyleUtils.CharStyleTextProps(ScrStyleNames.Emphasis, Cache.DefaultVernWs));
			bldr.Append(" ", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));

			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);

				ReflectionHelper.CallMethod(m_exporter, "WriteVernSegment", bldr.GetString(), true);

				// Confirm the results.
				string strExpected = "<verseStart ID=\"GEN.1.1\" n=\"1\" /><labelTr> <emphasis> </emphasis>" +
					"<foreign xml:lang=\"en\"> </foreign></labelTr>";
				Assert.AreEqual(strExpected, stream.ToString());
			}
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
			IWritingSystem wsDe = Cache.ServiceLocator.WritingSystemManager.Get("de");
			IWritingSystem wsUr = Cache.ServiceLocator.WritingSystemManager.Get("ur");

			// Create a picture that has a caption with three different writing systems.
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsDe.Handle);
			tssBldr.ReplaceRgch(0, 0, "photo", 5, propsBldr.GetTextProps());
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsUr.Handle);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " tasvir", 7, propsBldr.GetTextProps());
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, Cache.DefaultVernWs);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " picture caption", 16, propsBldr.GetTextProps());
			ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(
				Path.Combine(Path.GetTempPath(), "filename.jpg"), tssBldr.GetString(), "folder");

			// Set up for export
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportPicture_MultipleWS");
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);

				// Export the picture to XML
				ReflectionHelper.CallMethod(m_exporter, "ExportPicture", picture);

				// Check the results of the exported picture.
				//   Set up expected results.
				string defaultDir = Path.Combine(Path.GetTempPath(), "filename.jpg");
				// TODO (TE-7756): Support OXES export and import of new properties that have been
				// added to the CmPicture model
				string strExpected = "<figure src=\"filename.jpg\"><!--path=\"" + defaultDir + "\"-->" +
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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ExportPicture within the context of a TrGroup for the containing paragraph
		/// (FWR-2983).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPicture_InsideParaTrGroup()
		{
			IWritingSystem wsDe = Cache.ServiceLocator.WritingSystemManager.Get("de");

			// Create a picture that has a caption with three different writing systems.
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsDe.Handle);
			tssBldr.ReplaceRgch(0, 0, "photo", 5, propsBldr.GetTextProps());
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, Cache.DefaultVernWs);
			tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, " picture caption", 16, propsBldr.GetTextProps());
			ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(
				Path.Combine(Path.GetTempPath(), "filename.jpg"), tssBldr.GetString(), "folder");
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 0, "Before");
			para.Contents = picture.InsertORCAt(para.Contents, para.Contents.Length);
			AddVerse(para, 0, 0, "After");

			// Set up for export
			using (StringWriter stream = new StringWriter())
			using (XmlTextWriter writer = new XmlTextWriter(stream))
			{
				writer.Formatting = Formatting.None;
				m_exporter = new ExportXml(null, Cache, null, null, ExportWhat.AllBooks, 0, 0, 0,
					"ExportPicture_InsideParaTrGroup");
				ReflectionHelper.CallMethod(m_exporter, "InitializeExportWs", null);
				ReflectionHelper.SetField(m_exporter, "m_writer", writer);
				SetBookInfo(m_book);

				// Export the picture to XML
				ReflectionHelper.CallMethod(m_exporter, "ExportParagraph", para, null, true);

				// Check the results of the exported picture.
				//   Set up expected results.
				string defaultDir = Path.Combine(Path.GetTempPath(), "filename.jpg");
				// TODO (TE-7756): Support OXES export and import of new properties that have been
				// added to the CmPicture model
				string strExpected = "<p><trGroup><tr>Before<figure src=\"filename.jpg\" oxesRef=\"GEN\" alt=\"photo picture caption (Genesis)\">" +
					"<!--path=\"" + defaultDir + "\"-->" +
						"<caption><trGroup><tr><foreign xml:lang=\"de\">photo</foreign>" +
						" picture caption</tr></trGroup></caption></figure>After</tr></trGroup></p>";
				XmlDocument expected = new XmlDocument();
				expected.Load(new StringReader(strExpected));

				//   Get actual results.
				XmlDocument actual = new XmlDocument();
				actual.Load(new StringReader(stream.ToString()));

				string strDifference;
				if (!XmlHelper.CompareXmlNodes(expected.ChildNodes, actual.ChildNodes, out strDifference))
					Assert.Fail(strDifference);
			}
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the information about a book.
		/// </summary>
		/// <param name="book">The book.</param>
		/// ------------------------------------------------------------------------------------
		private void SetBookInfo(IScrBook book)
		{
			ReflectionHelper.SetField(m_exporter, "m_iCurrentBook", book.CanonicalNum);
			ReflectionHelper.SetField(m_exporter, "m_sCurrentBookId", book.BookId.Normalize());
			ReflectionHelper.SetField(m_exporter, "m_sCurrentBookName",
				ReflectionHelper.GetStrResult(m_exporter, "GetProperBookName",
				book.Name.BestVernacularAlternative));
			ReflectionHelper.SetField(m_exporter, "m_iCurrentChapter", 0);
			ReflectionHelper.SetField(m_exporter, "m_sCurrentChapterNumber", String.Empty);
			ReflectionHelper.SetField(m_exporter, "m_iCurrentVerse", 0);
			ReflectionHelper.SetField(m_exporter, "m_sCurrentVerseNumber", String.Empty);
		}
		#endregion
	}
	#endregion
}
