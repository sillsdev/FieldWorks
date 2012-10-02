// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportParatextTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using System.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using ScrVers = SILUBS.SharedScrUtils.ScrVers;

namespace SIL.FieldWorks.TE.ExportTests
{
	#region DummyUsfmStyEntry class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyUsfmStyEntry : UsfmStyEntry
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The description of the style (i.e., information about how to use it)
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal string Description
		{
			get { return base.Usage; }
			set { m_usage = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Context
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new ContextValues Context
		{
			get { return base.Context; }
			set { m_context = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Structure
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new StructureValues Structure
		{
			get { return base.Structure; }
			set { m_structure = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Function
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new FunctionValues Function
		{
			get { return base.Function; }
			set { m_function = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default font info.
		/// </summary>
		/// <value>The default font info.</value>
		/// ------------------------------------------------------------------------------------
		internal FontInfo DefaultFontInfo
		{
			get { return m_defaultFontInfo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the style
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new string Name
		{
			get { return base.Name; }
			set { m_name = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the space above paragraph in millipoints
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new int SpaceBefore
		{
			get { return base.SpaceBefore; }
			set { m_spaceBefore.ExplicitValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the space below paragraph in millipoints
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new int SpaceAfter
		{
			get { return base.SpaceAfter; }
			set { m_spaceAfter.ExplicitValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the indentation of first line in millipoints
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new int FirstLineIndent
		{
			get { return base.FirstLineIndent; }
			set { m_firstLineIndent.ExplicitValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the alignment
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new FwTextAlign Alignment
		{
			get { return base.Alignment; }
			set { m_alignment.ExplicitValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the indentation of paragraph from leading edge in millipoints
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new int LeadingIndent
		{
			get { return base.LeadingIndent; }
			set { m_leadingIndent.ExplicitValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the indentation of paragraph from trailing edge in millipoints
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		internal new int TrailingIndent
		{
			get { return base.TrailingIndent; }
			set { m_trailingIndent.ExplicitValue = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the text properties from sty file.
		/// </summary>
		/// <value>The text properties from sty file.</value>
		/// ------------------------------------------------------------------------------------
		internal string TextPropertiesFromStyFile
		{
			set { m_textPropertiesFromStyFile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the usfm sty property.
		/// </summary>
		/// <param name="marker">The marker.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		internal new void SetUsfmStyProperty(string marker, string value)
		{
			base.SetUsfmStyProperty(marker, value);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// dummy class for ExportUsfm for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyExportUsfm_NoStyleMapCreated : DummyExportUsfm
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyExportUsfm_NoStyleMapCreated(FdoCache cache, FilteredScrBooks filter) :
			base(cache, filter)
		{
			ParatextProjectShortName = "ABC";
			ParatextProjectFolder = Path.GetTempPath(); // @"C:\TEMP"; // Bad idea, since C:\TEMP might not exist.
			CreateStyleTables();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prevent loading of style table for this dummy class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void LoadStyleTables()
		{
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ParaText export
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportParatextTests : ScrInMemoryFdoTestBase
	{
		#region member data
		private IScrBook m_book;
		private DummyExportUsfm_NoStyleMapCreated m_exporter;
		private FilteredScrBooks m_filter;
		#endregion

		#region setup,teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this fixture for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			IWritingSystem wsUr;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ur", out wsUr);

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				// Set up additional vernacular writing systems.
				wsUr.RightToLeftScript = true;
				Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(wsUr);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(wsUr);
			});

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
		/// Create the in-memory cache data needed by the tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			// Create the book of Genesis in the database
			m_book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_book, "Genesis");

			IScrBook book = AddBookToMockedScripture(40, "Matthew");
			AddTitleToMockedBook(book, "Matthew");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			// initialize the exporter class
			m_filter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(123);
			m_filter.ShowAllBooks();
			m_exporter = new DummyExportUsfm_NoStyleMapCreated(Cache, m_filter);
			m_exporter.SetContext(m_book);
			m_exporter.ParatextProjectShortName = "ABC";
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
			m_exporter.Dispose();
			m_exporter = null;
			m_book = null;
			if (FileUtils.FileExists("test.lds"))
				FileUtils.Delete("test.lds");

			base.TestTearDown();
		}
		#endregion

		#region USFM Style Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to read the usfm sty file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		[SetCulture("en-US")]
		public void ReadUsfmStyFile()
		{
			m_exporter.CallReadUsfmStyFile();
			Assert.AreEqual(196, m_exporter.UsfmEntries.Count);
			// Test id marker properties
			UsfmStyEntry entry = (UsfmStyEntry)m_exporter.UsfmEntries["id"];
			Assert.AreEqual("File identification information (BOOKID, FILENAME, EDITOR, MODIFICATION DATE)", entry.Usage);
			Assert.AreEqual("Paragraph", entry.StyleType);
			Assert.AreEqual("Other", entry.TextType);
			Assert.AreEqual("paragraph nonpublishable nonvernacular book", entry.TextProperties);
			Assert.AreEqual(12000, entry.FontInfoForWs(-1).m_fontSize.Value);
			// Test ide marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["ide"];
			Assert.AreEqual("id", entry.OccursUnder);
			Assert.AreEqual(1, entry.Rank);
			// Test c marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["c"];
			FontInfo fontInfo = entry.FontInfoForWs(-1);
			Assert.AreEqual("c - Chapter Number", entry.P6Name);
			Assert.AreEqual("Paragraph", entry.StyleType);
			Assert.AreEqual("ChapterNumber", entry.TextType);
			Assert.AreEqual("chapter", entry.TextProperties);
			Assert.IsTrue(entry.FontInfoForWs(-1).m_bold.Value);
			Assert.AreEqual(8000, entry.SpaceBefore);
			Assert.AreEqual(4000, entry.SpaceAfter);
			// Test p marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["p"];
			Assert.AreEqual("p - Paragraph - Normal, First Line Indent", entry.P6Name);
			Assert.AreEqual("Paragraph text, with first line indent", entry.Usage);
			Assert.AreEqual("paragraph publishable vernacular", entry.TextProperties);
			Assert.AreEqual("VerseText", entry.TextType);
			Assert.AreEqual(4, entry.Rank);
			Assert.AreEqual(12000, entry.FontInfoForWs(-1).m_fontSize.Value);
			Assert.AreEqual((int)(.125 * 72000), entry.FirstLineIndent);
			Assert.AreEqual("c", entry.OccursUnder);
			Assert.AreEqual(0, entry.Level);
			// Test va marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["va"];
			Assert.AreEqual(2263842, ColorUtil.ConvertColorToBGR(entry.FontInfoForWs(-1).m_fontColor.Value));
			Assert.AreEqual("va*", entry.Endmarker);
			Assert.IsTrue(entry.FontInfoForWs(-1).m_fontName.IsInherited);
			// Test qs marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["qs"];
			Assert.AreEqual(4, entry.Rank);
			Assert.IsTrue(entry.FontInfoForWs(-1).m_italic.Value);
			Assert.AreEqual("Character", entry.StyleType);
			// Test qr marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["qr"];
			Assert.AreEqual(FwTextAlign.ktalRight, entry.Alignment);
			// Test mt marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["mt"];
			Assert.AreEqual(FwTextAlign.ktalCenter, entry.Alignment);
			Assert.AreEqual(1, entry.Level);
			// Test s3 marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["s3"];
			Assert.AreEqual(FwTextAlign.ktalLeft, entry.Alignment);
			Assert.IsFalse(entry.NotRepeatable);
			Assert.IsFalse(entry.Poetic);
			// Test ib marker properties
			Assert.IsTrue(((UsfmStyEntry)m_exporter.UsfmEntries["ib"]).Poetic);
			// Test io1 marker properties
			entry = ((UsfmStyEntry)m_exporter.UsfmEntries["io1"]);
			//this will be either leading or trailing (testing purposes) otherwise left or right indent
			Assert.AreEqual((int)(.5 * 72000), entry.LeadingIndent);
			// Test ipi marker properties
			entry = ((UsfmStyEntry)m_exporter.UsfmEntries["ipi"]);
			Assert.AreEqual((int)(.25 * 72000), entry.TrailingIndent);
			// Test v marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["v"];
			Assert.AreEqual(FwSuperscriptVal.kssvSuper, entry.FontInfoForWs(-1).m_superSub.Value);
			Assert.AreEqual("Character", entry.StyleType);
			Assert.AreEqual("VerseNumber", entry.TextType);
			Assert.AreEqual("verse", entry.TextProperties);
			Assert.AreEqual("li li1 li2 li3 li4 m mi nb p pc ph phi pi pi1 pi2 pi3 pr pmo pm pmc pmr q q1 q2 q3 q4 qc qr qm qm1 qm2 qm3 qm4 tc1 tc2 tc3 tc4 tcr1 tcr2 tcr3 tcr4 s3 d sp", entry.OccursUnder);
			// Test imt* level_ values
			Assert.AreEqual(1, ((UsfmStyEntry)m_exporter.UsfmEntries["imt1"]).Level);
			Assert.AreEqual(2, ((UsfmStyEntry)m_exporter.UsfmEntries["imt2"]).Level);
			Assert.AreEqual(3, ((UsfmStyEntry)m_exporter.UsfmEntries["imt3"]).Level);
			Assert.AreEqual(4, ((UsfmStyEntry)m_exporter.UsfmEntries["imt4"]).Level);
			// Test x marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["x"];
			Assert.AreEqual("publishable vernacular note crossreference", entry.TextProperties);
			// Test nd marker properties
			entry = ((UsfmStyEntry)m_exporter.UsfmEntries["nd"]);
			Assert.AreEqual(FwUnderlineType.kuntSingle, entry.FontInfoForWs(-1).m_underline.Value);
			// Test f marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["f"];
			Assert.AreEqual("publishable vernacular note", entry.TextProperties);
			Assert.AreEqual("NoteText", entry.TextType);
			Assert.AreEqual("Note", entry.StyleType);
			// Test li4 marker properties
			entry = (UsfmStyEntry)m_exporter.UsfmEntries["li4"];
			Assert.AreEqual((int)(-0.25 * 72000), entry.FirstLineIndent);
			Assert.AreEqual("Paragraph", entry.StyleType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to read the usfm sty file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		[Ignore("Reading .sty file behaves differently with non-English culture (TE-8701)")]
		[SetCulture("de-DE")]
		public void ReadUsfmStyFile_OtherCulture()
		{
			m_exporter.CallReadUsfmStyFile();
			UsfmStyEntry entry = (UsfmStyEntry)m_exporter.UsfmEntries["p"];
			Assert.AreEqual((int)(.125 * 72000), entry.FirstLineIndent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the usfm sty file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		[SetCulture("en-US")]
		public void WriteUsfmStyFile()
		{
			DummyUsfmStyEntry entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("id", entry);
			entry.P6Name = "(id) File - Identification";
			entry.Description = "File identification information (BOOKID, FILENAME, EDITOR, MODIFICATION DATE)";
			entry.IsParagraphStyle = true;
			entry.Context = ContextValues.Book;
			entry.Structure = StructureValues.Undefined;
			entry.TextPropertiesFromStyFile = "paragraph nonpublishable nonvernacular book";
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 12000;

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("c", entry);
			entry.Name = "Chapter Number";
			entry.P6Name = "(c) Chapter Number";
			entry.Description = "Chapter number (necessary for normal Paratext operation)";
			entry.IsParagraphStyle = false;
			entry.OccursUnder = "id";
			entry.Rank = 8;
			entry.Context = ContextValues.Text;
			entry.Structure = StructureValues.Body;
			entry.Function = FunctionValues.Chapter;
			entry.DefaultFontInfo.m_bold.ExplicitValue = true;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 18000;
			entry.SpaceBefore = 8000;
			entry.SpaceAfter = 4000;

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("v", entry);
			entry.Name = "Verse Number";
			entry.P6Name = "(v) Verse Number";
			entry.Description = "A verse number (Necessary for normal paratext operation)";
			entry.IsParagraphStyle = false;
			entry.OccursUnder = "li li1 li2 li3 li4 m mi mis nb p pc ph phi pi pr ps psi q q1 q2 q3 qc qr tc1 tc2 tc3 tc4 s3 d";
			entry.Context = ContextValues.Text;
			entry.Structure = StructureValues.Body;
			entry.Function = FunctionValues.Verse;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 12000;
			entry.DefaultFontInfo.m_superSub.ExplicitValue = FwSuperscriptVal.kssvSuper;

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("p", entry);
			entry.Name = "Paragraph";
			entry.P6Name = "(p) Paragraph - Normal, First Line Indent";
			entry.Description = "Paragraph text, with first line indent";
			entry.IsParagraphStyle = true;
			entry.OccursUnder = "c";
			entry.Rank = 4;
			entry.Context = ContextValues.Text;
			entry.Structure = StructureValues.Body;
			entry.Function = FunctionValues.Prose;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 12000;
			entry.FirstLineIndent = (int)(0.125 * 72000);

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("rem", entry);
			entry.Name = "Remark";
			entry.P6Name = "(rem) File - Remark";
			entry.Description = "Comments and remarks";
			entry.OccursUnder = "id ide";
			entry.IsParagraphStyle = true;
			entry.Context = ContextValues.Annotation;
			entry.Structure = StructureValues.Undefined;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 12000;
			entry.DefaultFontInfo.m_fontColor.ExplicitValue = ColorUtil.ConvertBGRtoColor((uint)16711680);

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("Custom_User-defined_Style", entry);
			entry.Name = "Custom User-defined Style";
			entry.IsParagraphStyle = true;
			entry.Context = ContextValues.Text;
			entry.Structure = StructureValues.Body;
			entry.Function = FunctionValues.Prose;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 10000;
			entry.DefaultFontInfo.m_fontName.ExplicitValue = "Courier New";
			entry.SpaceAfter = 6000;

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("is2", entry);
			entry.Name = "Intro Section Head Minor";
			entry.P6Name = "(is2) Introduction - Section Heading Level 2";
			entry.Description = "Introduction section heading, level 2 ";
			entry.IsParagraphStyle = true;
			entry.Context = ContextValues.Intro;
			entry.Structure = StructureValues.Heading;
			entry.Function = FunctionValues.Prose;
			entry.OccursUnder = "id";
			entry.Rank = 6;
			entry.Level = 2;
			entry.NotRepeatable = true;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 12000;
			entry.DefaultFontInfo.m_bold.ExplicitValue = true;
			entry.Alignment = FwTextAlign.ktalCenter;
			entry.SpaceBefore = 8000;
			entry.SpaceAfter = 4000;

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("f", entry);
			entry.P6Name = "(f...f*) Footnote ";
			entry.Endmarker = "f*";
			entry.IsParagraphStyle = true;
			entry.Context = ContextValues.Note;
			entry.OccursUnder = "c li li1 li2 li3 li4 m mi mis nb p pc ph phi pi pr ps psi q q1 q2 q3 qc qr sp tc1 tc2 tc3 tc4 ms ms1 ms2 s s1 s2 s3 spd d ip";
			entry.TextPropertiesFromStyFile = "publishable vernacular note";
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 12000;
			entry.XmlTag = "<usfm:f>";

			entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("b", entry);
			entry.P6Name = "(b) Poetry - Stanza Break (Blank Line)";
			entry.IsParagraphStyle = true;
			entry.Context = ContextValues.Text;
			entry.Structure = StructureValues.Body;
			entry.Function = FunctionValues.Prose;
			entry.TextPropertiesFromStyFile = "paragraph publishable vernacular poetic";
			entry.Poetic = true;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 5000;
			entry.DefaultFontInfo.m_italic.ExplicitValue = true;
			entry.DefaultFontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntSingle;
			entry.LeadingIndent = (int)(.25 * 72000);
			entry.TrailingIndent = (int)(.25 * 72000);

			using (DummyFileWriter writer = new DummyFileWriter())
			{
				m_exporter.UsfmStyFileAccessor.SaveStyFile("Test project", writer, -1);

				// Verify the .sty file
				string[] expectedSty = new string[]
				{
					"## Stylesheet for exported TE project Test project ##",
					"",
					@"\Marker b",
					@"\Name (b) Poetry - Stanza Break (Blank Line)",
					@"\TextType VerseText",
					@"\TextProperties paragraph publishable vernacular poetic",
					@"\StyleType Paragraph",
					@"\Italic",
					@"\Underline",
					@"\FontSize 5",
					@"\LeftMargin .250",
					@"\RightMargin .250",
					"",
					@"\Marker c",
					@"\TEStyleName Chapter Number",
					@"\Name (c) Chapter Number",
					@"\Description Chapter number (necessary for normal Paratext operation)",
					@"\OccursUnder id",
					@"\Rank 8",
					@"\TextType ChapterNumber",
					@"\TextProperties chapter",
					@"\StyleType Paragraph",
					@"\Bold",
					@"\FontSize 18",
					@"\SpaceBefore 8",
					@"\SpaceAfter 4",
					"",
					@"\Marker Custom_User-defined_Style",
					@"\TEStyleName Custom User-defined Style",
					@"\Name Custom User-defined Style",
					@"\TextType VerseText",
					@"\TextProperties paragraph publishable vernacular",
					@"\StyleType Paragraph",
					@"\FontName Courier New",
					@"\FontSize 10",
					@"\SpaceAfter 6",
					"",
					@"\Marker f",
					@"\Endmarker f*",
					@"\Name (f...f*) Footnote",
					@"\OccursUnder c li li1 li2 li3 li4 m mi mis nb p pc ph phi pi pr ps psi q q1 q2 q3 qc qr sp tc1 tc2 tc3 tc4 ms ms1 ms2 s s1 s2 s3 spd d ip",
					@"\TextType NoteText",
					@"\TextProperties publishable vernacular note",
					@"\StyleType Note",
					@"\FontSize 12",
					@"\XMLTag <usfm:f>",
					"",
					@"\Marker id",
					@"\Name (id) File - Identification",
					@"\Description File identification information (BOOKID, FILENAME, EDITOR, MODIFICATION DATE)",
					@"\TextType Other",
					@"\TextProperties paragraph nonpublishable nonvernacular book",
					@"\StyleType Paragraph",
					@"\FontSize 12",
					"",
					@"\Marker is2",
					@"\TEStyleName Intro Section Head Minor",
					@"\Name (is2) Introduction - Section Heading Level 2",
					@"\Description Introduction section heading, level 2",
					@"\OccursUnder id",
					@"\Rank 6",
					@"\NotRepeatable",
					@"\TextType Section",
					@"\TextProperties paragraph publishable vernacular level_2",
					@"\StyleType Paragraph",
					@"\Bold",
					@"\FontSize 12",
					@"\Justification Center",
					@"\SpaceBefore 8",
					@"\SpaceAfter 4",
					"",
					@"\Marker p",
					@"\TEStyleName Paragraph",
					@"\Name (p) Paragraph - Normal, First Line Indent",
					@"\Description Paragraph text, with first line indent",
					@"\OccursUnder c",
					@"\Rank 4",
					@"\TextType VerseText",
					@"\TextProperties paragraph publishable vernacular",
					@"\StyleType Paragraph",
					@"\FontSize 12",
					@"\FirstLineIndent .125",
					"",
					@"\Marker rem",
					@"\TEStyleName Remark",
					@"\Name (rem) File - Remark",
					@"\Description Comments and remarks",
					@"\OccursUnder id ide",
					@"\TextType Other",
					@"\TextProperties paragraph nonpublishable nonvernacular",
					@"\StyleType Paragraph",
					@"\FontSize 12",
					@"\Color 16711680",
					"",
					@"\Marker v",
					@"\TEStyleName Verse Number",
					@"\Name (v) Verse Number",
					@"\Description A verse number (Necessary for normal paratext operation)",
					@"\OccursUnder li li1 li2 li3 li4 m mi mis nb p pc ph phi pi pr ps psi q q1 q2 q3 qc qr tc1 tc2 tc3 tc4 s3 d",
					@"\TextType VerseNumber",
					@"\TextProperties verse",
					@"\StyleType Character",
					@"\Superscript",
					@"\FontSize 12",
					""
				};

				writer.VerifyOutput(expectedSty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the usfm sty file when the default culture is set to
		/// something other than English. The output should still be the same.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		[Ignore("Paratext export currently produces different results depending on the current culture (TE-7980)")]
		[SetCulture("de-DE")]
		public void WriteUsfmStyFile_OtherCulture()
		{
			DummyUsfmStyEntry entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("b", entry);
			entry.P6Name = "(b) Poetry - Stanza Break (Blank Line)";
			entry.IsParagraphStyle = true;
			entry.Context = ContextValues.Text;
			entry.Structure = StructureValues.Body;
			entry.Function = FunctionValues.Prose;
			entry.TextPropertiesFromStyFile = "paragraph publishable vernacular poetic";
			entry.Poetic = true;
			entry.DefaultFontInfo.m_fontSize.ExplicitValue = 5000;
			entry.DefaultFontInfo.m_italic.ExplicitValue = true;
			entry.DefaultFontInfo.m_underline.ExplicitValue = FwUnderlineType.kuntSingle;
			entry.LeadingIndent = (int)(.25 * 72000);
			entry.TrailingIndent = (int)(.25 * 72000);

			using (DummyFileWriter writer = new DummyFileWriter())
			{
				m_exporter.UsfmStyFileAccessor.SaveStyFile("Test project", writer, -1);

				// Verify the .sty file
				string[] expectedSty = new string[]
				{
					"## Stylesheet for exported TE project Test project ##",
					"",
					@"\Marker b",
					@"\Name (b) Poetry - Stanza Break (Blank Line)",
					@"\TextType VerseText",
					@"\TextProperties paragraph publishable vernacular poetic",
					@"\StyleType Paragraph",
					@"\Italic",
					@"\Underline",
					@"\FontSize 5",
					@"\LeftMargin .250",
					@"\RightMargin .250",
					""
				};

				writer.VerifyOutput(expectedSty);
			}
		}
		#endregion

		#region BooksPresent Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ParatextSsfFileAccessor.MergeBooksPresent method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void MergeBooksPresent()
		{
			// Test when existing == filtered set
			Assert.AreEqual("100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000",
				m_exporter.ParatextSsfFileAccessor.MergeBooksPresent("100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000"));

			// Test when existing is a subset of filtered set
			Assert.AreEqual("100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000",
				m_exporter.ParatextSsfFileAccessor.MergeBooksPresent("000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000"));

			// Test when existing is a superset of filtered set
			Assert.AreEqual("110000000000000000000000000000000000000101000000000000000000000000000000000000000000000000000000001",
				m_exporter.ParatextSsfFileAccessor.MergeBooksPresent("110000000000000000000000000000000000000101000000000000000000000000000000000000000000000000000000001"));

			// Test when existing set and filtered set have nothing in common
			m_filter.FilteredBooks = new IScrBook[] { m_book }; // Filter to only export Genesis
			Assert.AreEqual("110000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000",
				m_exporter.ParatextSsfFileAccessor.MergeBooksPresent("010000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ParatextSsfFileAccessor.BooksPresent property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void BooksPresent()
		{
			// Test adding Genesis
			m_filter.FilteredBooks = new IScrBook[] { m_book };
			Assert.AreEqual("100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
				m_exporter.ParatextSsfFileAccessor.BooksPresent);

			// Test adding Matthew
			m_filter.Add(new IScrBook[] { m_scr.FindBook(40) });
			Assert.AreEqual("100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000",
				m_exporter.ParatextSsfFileAccessor.BooksPresent);
		}
		#endregion

		#region WriteParatextSsfFile Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the Paratext ssf file using the default versification
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void WriteParatextSsfFile_DefaultVersification()
		{
			m_scr.Versification = 0;
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			using (DummyFileWriter writer = new DummyFileWriter())
			{
				FileNameFormat fileNameFormat = new FileNameFormat("pre",
					FileNameFormat.SchemeFormat.NNBBB, "suf", "ext");
				m_exporter.ParatextSsfFileAccessor.SaveSsfFile(fileNameFormat, "dummy", "styFile.sty",
					@"C:\My Paratext Projects\dummy", writer, Cache.DefaultVernWs);

				// Verify the .ssf file
				string[] expectedSsf = new string[]
				{
					@"<ScriptureText>",
					@"<BooksPresent>100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000</BooksPresent>",
					@"<Copyright></Copyright>",
					@"<Directory>C:\My Paratext Projects\dummy</Directory>",
					@"<Editable>T</Editable>",
					@"<Encoding>" + ParatextSsfFileAccessor.kUnicodeEncoding + "</Encoding>",
					@"<FileNameForm>41MAT</FileNameForm>",
					@"<FileNamePostPart>suf.ext</FileNamePostPart>",
					@"<FileNamePrePart>pre</FileNamePrePart>",
					@"<FullName>" + Cache.ProjectId.Name + "</FullName>",
					@"<Language>French</Language>",
					@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>",
					@"<Name>dummy</Name>",
					@"<StyleSheet>styFile.sty</StyleSheet>",
					@"<Versification>4</Versification>", // English, by default
					"<Naming PrePart=\"pre\" PostPart=\"suf.ext\" BookNameForm=\"41MAT\"></Naming>",
					@"</ScriptureText>"
				};

				writer.VerifyOutput(expectedSsf);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the Paratext ssf file with a non-default versification
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void WriteParatextSsfFile_SeptuagintVersification()
		{
			m_scr.Versification = ScrVers.Septuagint;
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			using (DummyFileWriter writer = new DummyFileWriter())
			{
				FileNameFormat fileNameFormat = new FileNameFormat("pre",
					FileNameFormat.SchemeFormat.NNBBB, "suf", "ext");
				m_exporter.ParatextSsfFileAccessor.SaveSsfFile(fileNameFormat, "dummy", "styFile.sty",
					@"C:\My Paratext Projects\dummy", writer, Cache.DefaultVernWs);

				// Verify the .ssf file
				string[] expectedSsf = new string[]
				{
					@"<ScriptureText>",
					@"<BooksPresent>100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000</BooksPresent>",
					@"<Copyright></Copyright>",
					@"<Directory>C:\My Paratext Projects\dummy</Directory>",
					@"<Editable>T</Editable>",
					@"<Encoding>" + ParatextSsfFileAccessor.kUnicodeEncoding + "</Encoding>",
					@"<FileNameForm>41MAT</FileNameForm>",
					@"<FileNamePostPart>suf.ext</FileNamePostPart>",
					@"<FileNamePrePart>pre</FileNamePrePart>",
					@"<FullName>" + Cache.ProjectId.Name + "</FullName>",
					@"<Language>French</Language>",
					@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>",
					@"<Name>dummy</Name>",
					@"<StyleSheet>styFile.sty</StyleSheet>",
					@"<Versification>2</Versification>", // Septuagint
					"<Naming PrePart=\"pre\" PostPart=\"suf.ext\" BookNameForm=\"41MAT\"></Naming>",
					@"</ScriptureText>"
				};

				writer.VerifyOutput(expectedSsf);
			}
		}
		#endregion

		#region UpdateParatextSsfFile Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update an existing Paratext ssf file when the destination
		/// folder, file specification, and encoding are the same.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextSsfFile_SameFolderAndSpec()
		{
			m_scr.Versification = 0; // Should default to English
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// The existing file has Exodus and Matthew.
			string existingSsf =
				@"<ScriptureText>" + Environment.NewLine +
				@"<Book_EXO>Exodus</Book_EXO>"  + Environment.NewLine +
				@"<Book_MAT>Matthew</Book_MAT>"  + Environment.NewLine +
				@"<BooksPresent>010000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000</BooksPresent>"  + Environment.NewLine +
				@"<ChapterMarker>c</ChapterMarker>"  + Environment.NewLine +
				@"<Copyright></Copyright>"  + Environment.NewLine +
				@"<Directory>C:\My Paratext Projects\dummy</Directory>" + Environment.NewLine +
				@"<DotAll>No</DotAll>" + Environment.NewLine +
				@"<Editable>T</Editable>" + Environment.NewLine +
				@"<Encoding>" + ParatextSsfFileAccessor.kUnicodeEncoding + "</Encoding>" + Environment.NewLine +
				@"<FileNameChapterNumberForm></FileNameChapterNumberForm>" + Environment.NewLine +
				@"<FileNameForm>41MAT</FileNameForm>" + Environment.NewLine +
				@"<FileNamePostPart>suf.ext</FileNamePostPart>" + Environment.NewLine +
				@"<FileNamePrePart>pre</FileNamePrePart>" + Environment.NewLine +
				@"<FullName>My project Name</FullName>" + Environment.NewLine +
				@"<Language>Gumbasian</Language>" + Environment.NewLine +
				@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>" + Environment.NewLine +
				@"<MatchCase>No</MatchCase>" + Environment.NewLine +
				@"<Name>dummy</Name>" + Environment.NewLine +
				@"<ResourceText>F</ResourceText>" + Environment.NewLine +
				@"<RunFromCD>F</RunFromCD>" + Environment.NewLine +
				@"<ShowReferences>No</ShowReferences>" + Environment.NewLine +
				@"<StyleSheet>dontOverWriteMe.sty</StyleSheet>" + Environment.NewLine +
				@"<VerseMarker>v</VerseMarker>" + Environment.NewLine +
				@"<Versification>2</Versification>" + Environment.NewLine +
				"<Naming PrePart=\"pre\" PostPart=\"suf.ext\" BookNameForm=\"41MAT\"></Naming>" + Environment.NewLine +
				@"<AssociatedLexicalProject>" + Cache.ProjectId.PipeHandle + @"</AssociatedLexicalProject>" + Environment.NewLine +
				@"</ScriptureText>";

			FileNameFormat fileNameFormat = new FileNameFormat("pre",
				FileNameFormat.SchemeFormat.NNBBB, "suf", "ext");
			XmlDocument resultSSF;

			m_filter.Add(new IScrBook[] { m_book } );

			using (StringReader sr = new StringReader(existingSsf))
			{
				resultSSF = m_exporter.ParatextSsfFileAccessor.UpdateSsfFile(sr, fileNameFormat, "dummy",
					"styFile.sty", @"C:\My Paratext Projects\dummy", vernWs.Handle);
			}

			// Verify the .ssf contents
			Dictionary<string, string> expectedSsfEntries = new Dictionary<string, string>();
			expectedSsfEntries["AssociatedLexicalProject"] = Cache.ProjectId.PipeHandle;
			expectedSsfEntries["Book_EXO"] = "Exodus";
			expectedSsfEntries["Book_MAT"] = "Matthew";
			expectedSsfEntries["BooksPresent"] = "110000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000";
			expectedSsfEntries["ChapterMarker"] = "c";
			expectedSsfEntries["Copyright"] = string.Empty;
			expectedSsfEntries["Directory"] = @"C:\My Paratext Projects\dummy";
			expectedSsfEntries["DotAll"] = "No";
			expectedSsfEntries["Editable"] = "T";
			expectedSsfEntries["Encoding"] = ParatextSsfFileAccessor.kUnicodeEncoding;
			expectedSsfEntries["FileNameChapterNumberForm"] = string.Empty;
			expectedSsfEntries["FileNameForm"] = "41MAT";
			expectedSsfEntries["FileNamePostPart"] = "suf.ext";
			expectedSsfEntries["FileNamePrePart"] = "pre";
			expectedSsfEntries["FullName"] = "My project Name";
			expectedSsfEntries["Language"] = "French";
			expectedSsfEntries["LeftToRight"] = (vernWs.RightToLeftScript ? "F" : "T");
			expectedSsfEntries["MatchCase"] = "No";
			expectedSsfEntries["Name"] = "dummy";
			expectedSsfEntries["ResourceText"] = "F";
			expectedSsfEntries["RunFromCD"] = "F";
			expectedSsfEntries["ShowReferences"] = "No";
			expectedSsfEntries["StyleSheet"] = "dontOverWriteMe.sty"; // Don't overwrite the .sty
			expectedSsfEntries["VerseMarker"] = "v";
			expectedSsfEntries["Versification"] = "2"; // Don't overwrite the versification.
			expectedSsfEntries["Naming"] = string.Empty;

			Assert.AreEqual(1, resultSSF.ChildNodes.Count, "Only node in document should be ScriptureText");
			XmlNode contents = resultSSF.SelectSingleNode("ScriptureText");
			Assert.AreEqual(expectedSsfEntries.Count, contents.ChildNodes.Count);
			foreach (string expectedElement in expectedSsfEntries.Keys)
			{
				XmlNode node = contents.SelectSingleNode(expectedElement);
				Assert.IsNotNull(node);
				Assert.AreEqual(expectedSsfEntries[expectedElement], node.InnerText);
			}
			// Check the Naiming node (only node that uses attributes)
			XmlNode namingNode = contents.SelectSingleNode("Naming");
			Assert.IsNotNull(namingNode);
			Assert.AreEqual("pre", namingNode.Attributes.GetNamedItem("PrePart").Value);
			Assert.AreEqual("suf.ext", namingNode.Attributes.GetNamedItem("PostPart").Value);
			Assert.AreEqual("41MAT", namingNode.Attributes.GetNamedItem("BookNameForm").Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update an existing Paratext ssf file when the destination
		/// folders are different, but the file specification and encoding are the same.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextSsfFile_DifferentFolderSameSpec()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// The existing file has a lot of books.
			string existingSsf =
				@"<ScriptureText>" + Environment.NewLine +
				@"<BooksPresent>111111111111111111111111111111111111111111111111111111111111111111111111111111111000000000000000000</BooksPresent>" + Environment.NewLine +
				@"<Copyright></Copyright>" + Environment.NewLine +
				@"<Directory>C:\My Documents\dummy</Directory>" + Environment.NewLine +
				@"<Editable>T</Editable>" + Environment.NewLine +
				@"<Encoding>" + ParatextSsfFileAccessor.kUnicodeEncoding + "</Encoding>" + Environment.NewLine +
				@"<FileNameForm>41MAT</FileNameForm>" + Environment.NewLine +
				@"<FileNamePostPart>suf.ext</FileNamePostPart>" + Environment.NewLine +
				@"<FileNamePrePart>pre</FileNamePrePart>" + Environment.NewLine +
				@"<FullName>My project Name</FullName>" + Environment.NewLine +
				@"<Language>Gumbasian</Language>" + Environment.NewLine +
				@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>" + Environment.NewLine +
				@"<Name>dummy</Name>" + Environment.NewLine +
				@"<StyleSheet>styFile.sty</StyleSheet>" + Environment.NewLine +
				@"<Versification>4</Versification>" + Environment.NewLine +
				"<Naming PrePart=\"pre\" PostPart=\"suf.ext\" BookNameForm=\"41MAT\"></Naming>" + Environment.NewLine +
				@"</ScriptureText>";

			FileNameFormat fileNameFormat = new FileNameFormat("pre",
				FileNameFormat.SchemeFormat.NNBBB, "suf", "ext");
			XmlDocument resultSSF;

			m_filter.Add(new IScrBook[] { m_book } );

			using (StringReader sr = new StringReader(existingSsf))
			{
				resultSSF = m_exporter.ParatextSsfFileAccessor.UpdateSsfFile(sr, fileNameFormat, "dummy",
					"styFile.sty", @"C:\My Paratext Projects\dummy", vernWs.Handle);
			}

			// Verify the .ssf contents
			Dictionary<string, string> expectedSsfEntries = new Dictionary<string, string>();
			expectedSsfEntries["BooksPresent"] = "100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000";
			expectedSsfEntries["Copyright"] = string.Empty;
			expectedSsfEntries["Directory"] = @"C:\My Paratext Projects\dummy";
			expectedSsfEntries["Editable"] = "T";
			expectedSsfEntries["Encoding"] = ParatextSsfFileAccessor.kUnicodeEncoding;
			expectedSsfEntries["FileNameForm"] = "41MAT";
			expectedSsfEntries["FileNamePostPart"] = "suf.ext";
			expectedSsfEntries["FileNamePrePart"] = "pre";
			expectedSsfEntries["FullName"] = "My project Name";
			expectedSsfEntries["Language"] = "French";
			expectedSsfEntries["LeftToRight"] = (vernWs.RightToLeftScript ? "F" : "T");
			expectedSsfEntries["Name"] = "dummy";
			expectedSsfEntries["StyleSheet"] = "styFile.sty";
			expectedSsfEntries["Versification"] = "4"; // Preserve the versification of their original project
			expectedSsfEntries["Naming"] = string.Empty;

			Assert.AreEqual(1, resultSSF.ChildNodes.Count, "Only node in document should be ScriptureText");
			XmlNode contents = resultSSF.SelectSingleNode("ScriptureText");
			Assert.AreEqual(expectedSsfEntries.Count, contents.ChildNodes.Count);
			foreach (string expectedElement in expectedSsfEntries.Keys)
			{
				XmlNode node = contents.SelectSingleNode(expectedElement);
				Assert.IsNotNull(node);
				Assert.AreEqual(expectedSsfEntries[expectedElement], node.InnerText);
			}
			// Check the Naiming node (only node that uses attributes)
			XmlNode namingNode = contents.SelectSingleNode("Naming");
			Assert.IsNotNull(namingNode);
			Assert.AreEqual("pre", namingNode.Attributes.GetNamedItem("PrePart").Value);
			Assert.AreEqual("suf.ext", namingNode.Attributes.GetNamedItem("PostPart").Value);
			Assert.AreEqual("41MAT", namingNode.Attributes.GetNamedItem("BookNameForm").Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update an existing Paratext ssf file when the language
		/// property is missing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextSsfFile_NoLanguageSpecified()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// The existing file has a lot of books.
			string existingSsf =
				@"<ScriptureText>" + Environment.NewLine +
				@"<BooksPresent>111111111111111111111111111111111111111111111111111111111111111111111111111111111000000000000000000</BooksPresent>" + Environment.NewLine +
				@"<Copyright></Copyright>" + Environment.NewLine +
				@"<Directory>C:\My Paratext Projects\dummy</Directory>" + Environment.NewLine +
				@"<Editable>T</Editable>" + Environment.NewLine +
				@"<Encoding>" + ParatextSsfFileAccessor.kUnicodeEncoding + "</Encoding>" + Environment.NewLine +
				@"<FileNameForm>41MAT</FileNameForm>" + Environment.NewLine +
				@"<FileNamePostPart>suf.ext</FileNamePostPart>" + Environment.NewLine +
				@"<FileNamePrePart></FileNamePrePart>" + Environment.NewLine +
				@"<FullName>My project Name</FullName>" + Environment.NewLine +
				@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>" + Environment.NewLine +
				@"<Name>dummy</Name>" + Environment.NewLine +
				@"<StyleSheet>styFile.sty</StyleSheet>" + Environment.NewLine +
				@"<Versification>4</Versification>" + Environment.NewLine +
				"<Naming PostPart=\"suf.ext\" BookNameForm=\"41MAT\"></Naming>" + Environment.NewLine +
				@"</ScriptureText>";

			FileNameFormat fileNameFormat = new FileNameFormat(string.Empty,
				FileNameFormat.SchemeFormat.NNBBB, "suf", "ext");
			XmlDocument resultSSF;

			m_filter.Add( new IScrBook[] { m_book });

			using (StringReader sr = new StringReader(existingSsf))
			{
				resultSSF = m_exporter.ParatextSsfFileAccessor.UpdateSsfFile(sr, fileNameFormat, "dummy",
					"styFile.sty", @"C:\My Paratext Projects\dummy", vernWs.Handle);
			}

			// Verify the .ssf contents
			Dictionary<string, string> expectedSsfEntries = new Dictionary<string, string>();
			expectedSsfEntries["BooksPresent"] = "111111111111111111111111111111111111111111111111111111111111111111111111111111111000000000000000000";
			expectedSsfEntries["Copyright"] = string.Empty;
			expectedSsfEntries["Directory"] = @"C:\My Paratext Projects\dummy";
			expectedSsfEntries["Editable"] = "T";
			expectedSsfEntries["Encoding"] = ParatextSsfFileAccessor.kUnicodeEncoding;
			expectedSsfEntries["FileNameForm"] = "41MAT";
			expectedSsfEntries["FileNamePostPart"] = "suf.ext";
			expectedSsfEntries["FileNamePrePart"] = string.Empty;
			expectedSsfEntries["FullName"] = "My project Name";
			expectedSsfEntries["Language"] = "French";
			expectedSsfEntries["LeftToRight"] = (vernWs.RightToLeftScript ? "F" : "T");
			expectedSsfEntries["Name"] = "dummy";
			expectedSsfEntries["StyleSheet"] = "styFile.sty";
			expectedSsfEntries["Versification"] = "4"; // Preserve the versification of their original project
			expectedSsfEntries["Naming"] = string.Empty;

			Assert.AreEqual(1, resultSSF.ChildNodes.Count, "Only node in document should be ScriptureText");
			XmlNode contents = resultSSF.SelectSingleNode("ScriptureText");
			Assert.AreEqual(expectedSsfEntries.Count, contents.ChildNodes.Count);
			foreach (string expectedElement in expectedSsfEntries.Keys)
			{
				XmlNode node = contents.SelectSingleNode(expectedElement);
				Assert.IsNotNull(node);
				Assert.AreEqual(expectedSsfEntries[expectedElement], node.InnerText);
			}
			// Check the Naiming node (only node that uses attributes)
			XmlNode namingNode = contents.SelectSingleNode("Naming");
			Assert.IsNotNull(namingNode);
			Assert.IsNull(namingNode.Attributes.GetNamedItem("PrePart"));
			Assert.AreEqual("suf.ext", namingNode.Attributes.GetNamedItem("PostPart").Value);
			Assert.AreEqual("41MAT", namingNode.Attributes.GetNamedItem("BookNameForm").Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update an existing Paratext ssf file when the destination
		/// folder and encoding are the same, but the file specifications are different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextSsfFile_SameFolderDifferentSpec()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// The existing file has a lot of books.
			string existingSsf =
				@"<ScriptureText>" + Environment.NewLine +
				@"<BooksPresent>111111111111111111111111111111111111111111111111111111111111111111111111111111111000000000000000000</BooksPresent>" + Environment.NewLine +
				@"<Copyright></Copyright>" + Environment.NewLine +
				@"<Directory>C:\My Paratext Projects\dummy</Directory>" + Environment.NewLine +
				@"<Editable>T</Editable>" + Environment.NewLine +
				@"<Encoding>" + ParatextSsfFileAccessor.kUnicodeEncoding + "</Encoding>" + Environment.NewLine +
				@"<FileNameForm>MAT</FileNameForm>" + Environment.NewLine +
				@"<FileNamePostPart>.sfm</FileNamePostPart>" + Environment.NewLine +
				@"<FileNamePrePart>pre</FileNamePrePart>" + Environment.NewLine +
				@"<FullName>My project Name</FullName>" + Environment.NewLine +
				@"<Language>Gumbasian</Language>" + Environment.NewLine +
				@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>" + Environment.NewLine +
				@"<Name>dummy</Name>" + Environment.NewLine +
				@"<StyleSheet>styFile.sty</StyleSheet>" + Environment.NewLine +
				@"<Versification>4</Versification>" + Environment.NewLine +
				"<Naming PrePart=\"pre\" PostPart=\".sfm\" BookNameForm=\"MAT\"></Naming>" + Environment.NewLine +
				@"</ScriptureText>";

			FileNameFormat fileNameFormat = new FileNameFormat("pre",
				FileNameFormat.SchemeFormat.NNBBB, "suf", "ext");
			XmlDocument resultSSF;

			m_filter.Add(new IScrBook[] { m_book } );

			using (StringReader sr = new StringReader(existingSsf))
			{
				resultSSF = m_exporter.ParatextSsfFileAccessor.UpdateSsfFile(sr, fileNameFormat, "dummy",
					"styFile.sty", @"C:\My Paratext Projects\dummy", vernWs.Handle);
			}

			// Verify the .ssf contents
			Dictionary<string, string> expectedSsfEntries = new Dictionary<string, string>();
			expectedSsfEntries["BooksPresent"] = "100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000";
			expectedSsfEntries["Copyright"] = string.Empty;
			expectedSsfEntries["Directory"] = @"C:\My Paratext Projects\dummy";
			expectedSsfEntries["Editable"] = "T";
			expectedSsfEntries["Encoding"] = ParatextSsfFileAccessor.kUnicodeEncoding;
			expectedSsfEntries["FileNameForm"] = "41MAT";
			expectedSsfEntries["FileNamePostPart"] = "suf.ext";
			expectedSsfEntries["FileNamePrePart"] = "pre";
			expectedSsfEntries["FullName"] = "My project Name";
			expectedSsfEntries["Language"] = "French";
			expectedSsfEntries["LeftToRight"] = (vernWs.RightToLeftScript ? "F" : "T");
			expectedSsfEntries["Name"] = "dummy";
			expectedSsfEntries["StyleSheet"] = "styFile.sty";
			expectedSsfEntries["Versification"] = "4"; // Preserve the versification of their original project
			expectedSsfEntries["Naming"] = string.Empty;

			Assert.AreEqual(1, resultSSF.ChildNodes.Count, "Only node in document should be ScriptureText");
			XmlNode contents = resultSSF.SelectSingleNode("ScriptureText");
			Assert.AreEqual(expectedSsfEntries.Count, contents.ChildNodes.Count);
			foreach (string expectedElement in expectedSsfEntries.Keys)
			{
				XmlNode node = contents.SelectSingleNode(expectedElement);
				Assert.IsNotNull(node);
				Assert.AreEqual(expectedSsfEntries[expectedElement], node.InnerText);
			}
			// Check the Naming node (only node that uses attributes)
			XmlNode namingNode = contents.SelectSingleNode("Naming");
			Assert.IsNotNull(namingNode);
			Assert.AreEqual("pre", namingNode.Attributes.GetNamedItem("PrePart").Value);
			Assert.AreEqual("suf.ext", namingNode.Attributes.GetNamedItem("PostPart").Value);
			Assert.AreEqual("41MAT", namingNode.Attributes.GetNamedItem("BookNameForm").Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update an existing Paratext ssf file when the destination
		/// folder and the file specifications are the same, but encodings are different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextSsfFile_DifferentEncoding()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// The existing file has a lot of books.
			string existingSsf =
				@"<ScriptureText>" + Environment.NewLine +
				@"<BooksPresent>111111111111111111111111111111111111111111111111111111111111111111111111111111111000000000000000000</BooksPresent>" + Environment.NewLine +
				@"<Copyright></Copyright>" + Environment.NewLine +
				@"<Directory>C:\My Paratext Projects\dummy</Directory>" + Environment.NewLine +
				@"<Editable>T</Editable>" + Environment.NewLine +
				@"<Encoding>1252</Encoding>" + Environment.NewLine +
				@"<FileNameForm>41</FileNameForm>" + Environment.NewLine +
				@"<FileNamePostPart>suf.ext</FileNamePostPart>" + Environment.NewLine +
				@"<FileNamePrePart>pre</FileNamePrePart>" + Environment.NewLine +
				@"<FullName>My project Name</FullName>" + Environment.NewLine +
				@"<Language>Gumbasian</Language>" + Environment.NewLine +
				@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>" + Environment.NewLine +
				@"<Name>dummy</Name>" + Environment.NewLine +
				@"<StyleSheet>styFile.sty</StyleSheet>" + Environment.NewLine +
				@"<Versification>4</Versification>" + Environment.NewLine +
				"<Naming PrePart=\"pre\" PostPart=\"suf.ext\" BookNameForm=\"41\"></Naming>" + Environment.NewLine +
				@"</ScriptureText>";

			FileNameFormat fileNameFormat = new FileNameFormat("pre",
				FileNameFormat.SchemeFormat.NN, "suf", "ext");
			XmlDocument resultSSF;

			m_filter.Add(new IScrBook[] { m_book });

			using (StringReader sr = new StringReader(existingSsf))
			{
				resultSSF = m_exporter.ParatextSsfFileAccessor.UpdateSsfFile(sr, fileNameFormat, "dummy",
					"styFile.sty", @"C:\My Paratext Projects\dummy", vernWs.Handle);
			}

			// Verify the .ssf contents
			Dictionary<string, string> expectedSsfEntries = new Dictionary<string, string>();
			expectedSsfEntries["BooksPresent"] = "100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000";
			expectedSsfEntries["Copyright"] = string.Empty;
			expectedSsfEntries["Directory"] = @"C:\My Paratext Projects\dummy";
			expectedSsfEntries["Editable"] = "T";
			expectedSsfEntries["Encoding"] = ParatextSsfFileAccessor.kUnicodeEncoding;
			expectedSsfEntries["FileNameForm"] = "41";
			expectedSsfEntries["FileNamePostPart"] = "suf.ext";
			expectedSsfEntries["FileNamePrePart"] = "pre";
			expectedSsfEntries["FullName"] = "My project Name";
			expectedSsfEntries["Language"] = "French";
			expectedSsfEntries["LeftToRight"] = (vernWs.RightToLeftScript ? "F" : "T");
			expectedSsfEntries["Name"] = "dummy";
			expectedSsfEntries["StyleSheet"] = "styFile.sty";
			expectedSsfEntries["Versification"] = "4"; // Preserve the versification of their original project
			expectedSsfEntries["Naming"] = string.Empty;

			Assert.AreEqual(1, resultSSF.ChildNodes.Count, "Only node in document should be ScriptureText");
			XmlNode contents = resultSSF.SelectSingleNode("ScriptureText");
			Assert.AreEqual(expectedSsfEntries.Count, contents.ChildNodes.Count);
			foreach (string expectedElement in expectedSsfEntries.Keys)
			{
				XmlNode node = contents.SelectSingleNode(expectedElement);
				Assert.IsNotNull(node);
				Assert.AreEqual(expectedSsfEntries[expectedElement], node.InnerText);
			}
			// Check the Naiming node (only node that uses attributes)
			XmlNode namingNode = contents.SelectSingleNode("Naming");
			Assert.IsNotNull(namingNode);
			Assert.AreEqual("pre", namingNode.Attributes.GetNamedItem("PrePart").Value);
			Assert.AreEqual("suf.ext", namingNode.Attributes.GetNamedItem("PostPart").Value);
			Assert.AreEqual("41", namingNode.Attributes.GetNamedItem("BookNameForm").Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update an existing Paratext ssf file when a bunch of required
		/// properties are missing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextSsfFile_BogusFile()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// The existing file has a lot of books.
			string existingSsf =
				@"<ScriptureText>" + Environment.NewLine +
				@"<BooksPresent>111111111111111111111111111111111111111111111111111111111111111111111111111111111000000000000000000</BooksPresent>" + Environment.NewLine +
				@"<Editable>T</Editable>" + Environment.NewLine +
				@"<Language>Gumbasian</Language>" + Environment.NewLine +
				@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>" + Environment.NewLine +
				@"<Name>dummy</Name>" + Environment.NewLine +
				@"<Versification>4</Versification>" + Environment.NewLine +
				@"</ScriptureText>";

			FileNameFormat fileNameFormat = new FileNameFormat("pre",
				FileNameFormat.SchemeFormat.NN, "suf", "ext");
			XmlDocument resultSSF;

			m_filter.Add(new IScrBook[] { m_book });

			using (StringReader sr = new StringReader(existingSsf))
			{
				resultSSF = m_exporter.ParatextSsfFileAccessor.UpdateSsfFile(sr, fileNameFormat, "dummy",
					"styFile.sty", @"C:\My Paratext Projects\dummy", vernWs.Handle);
			}

			// Verify the .ssf contents
			Dictionary<string, string> expectedSsfEntries = new Dictionary<string, string>();
			expectedSsfEntries["BooksPresent"] = "100000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000";
			expectedSsfEntries["Directory"] = @"C:\My Paratext Projects\dummy";
			expectedSsfEntries["Editable"] = "T";
			expectedSsfEntries["Encoding"] = ParatextSsfFileAccessor.kUnicodeEncoding;
			expectedSsfEntries["FileNameForm"] = "41";
			expectedSsfEntries["FileNamePostPart"] = "suf.ext";
			expectedSsfEntries["FileNamePrePart"] = "pre";
//			expectedSsfEntries["FullName"] = "My project Name";
			expectedSsfEntries["Language"] = "French";
			expectedSsfEntries["LeftToRight"] = (vernWs.RightToLeftScript ? "F" : "T");
			expectedSsfEntries["Name"] = "dummy";
			expectedSsfEntries["StyleSheet"] = "styFile.sty";
			expectedSsfEntries["Versification"] = "4"; // Preserve the versification of their original project
			expectedSsfEntries["Naming"] = string.Empty;

			Assert.AreEqual(1, resultSSF.ChildNodes.Count, "Only node in document should be ScriptureText");
			XmlNode contents = resultSSF.SelectSingleNode("ScriptureText");
			Assert.AreEqual(expectedSsfEntries.Count, contents.ChildNodes.Count);
			foreach (string expectedElement in expectedSsfEntries.Keys)
			{
				XmlNode node = contents.SelectSingleNode(expectedElement);
				Assert.IsNotNull(node);
				Assert.AreEqual(expectedSsfEntries[expectedElement], node.InnerText);
			}
			// Check the Naiming node (only node that uses attributes)
			XmlNode namingNode = contents.SelectSingleNode("Naming");
			Assert.IsNotNull(namingNode);
			Assert.AreEqual("pre", namingNode.Attributes.GetNamedItem("PrePart").Value);
			Assert.AreEqual("suf.ext", namingNode.Attributes.GetNamedItem("PostPart").Value);
			Assert.AreEqual("41", namingNode.Attributes.GetNamedItem("BookNameForm").Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update an existing Paratext ssf file when the original ssf file
		/// is missing the StyleSheet property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextSsfFile_MissingStylsheetSpecInOrig()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// The existing file has Exodus and Matthew.
			string existingSsf =
				@"<ScriptureText>" + Environment.NewLine +
				@"<BooksPresent>010000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000</BooksPresent>" + Environment.NewLine +
				@"<Directory>C:\My Paratext Projects\dummy</Directory>" + Environment.NewLine +
				@"<Editable>T</Editable>" + Environment.NewLine +
				@"<Encoding>" + ParatextSsfFileAccessor.kUnicodeEncoding + "</Encoding>" + Environment.NewLine +
				@"<FileNameForm>41MAT</FileNameForm>" + Environment.NewLine +
				@"<FileNamePostPart>suf.ext</FileNamePostPart>" + Environment.NewLine +
				@"<FileNamePrePart>pre</FileNamePrePart>" + Environment.NewLine +
				@"<FullName>My project Name</FullName>" + Environment.NewLine +
				@"<Language>Gumbasian</Language>" + Environment.NewLine +
				@"<LeftToRight>" + (vernWs.RightToLeftScript ? "F" : "T") + "</LeftToRight>" + Environment.NewLine +
				@"<Name>dummy</Name>" + Environment.NewLine +
				@"<Versification>4</Versification>" + Environment.NewLine +
				"<Naming PrePart=\"pre\" PostPart=\"suf.ext\" BookNameForm=\"41MAT\"></Naming>" + Environment.NewLine +
				@"</ScriptureText>";

			FileNameFormat fileNameFormat = new FileNameFormat("pre",
				FileNameFormat.SchemeFormat.NNBBB, "suf", "ext");
			XmlDocument resultSSF;

			m_filter.Add(new IScrBook[] { m_book });

			using (StringReader sr = new StringReader(existingSsf))
			{
				resultSSF = m_exporter.ParatextSsfFileAccessor.UpdateSsfFile(sr, fileNameFormat, "dummy",
					"styFile.sty", @"C:\My Paratext Projects\dummy", vernWs.Handle);
			}

			// Verify the .ssf contents
			Dictionary<string, string> expectedSsfEntries = new Dictionary<string, string>();
			expectedSsfEntries["BooksPresent"] = "110000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000";
			expectedSsfEntries["Directory"] = @"C:\My Paratext Projects\dummy";
			expectedSsfEntries["Editable"] = "T";
			expectedSsfEntries["Encoding"] = ParatextSsfFileAccessor.kUnicodeEncoding;
			expectedSsfEntries["FileNameForm"] = "41MAT";
			expectedSsfEntries["FileNamePostPart"] = "suf.ext";
			expectedSsfEntries["FileNamePrePart"] = "pre";
			expectedSsfEntries["FullName"] = "My project Name";
			expectedSsfEntries["Language"] = "French";
			expectedSsfEntries["LeftToRight"] = (vernWs.RightToLeftScript ? "F" : "T");
			expectedSsfEntries["Name"] = "dummy";
			expectedSsfEntries["StyleSheet"] = "styFile.sty";
			expectedSsfEntries["Versification"] = "4";
			expectedSsfEntries["Naming"] = string.Empty;

			Assert.AreEqual(1, resultSSF.ChildNodes.Count, "Only node in document should be ScriptureText");
			XmlNode contents = resultSSF.SelectSingleNode("ScriptureText");
			Assert.AreEqual(expectedSsfEntries.Count, contents.ChildNodes.Count);
			foreach (string expectedElement in expectedSsfEntries.Keys)
			{
				XmlNode node = contents.SelectSingleNode(expectedElement);
				Assert.IsNotNull(node);
				Assert.AreEqual(expectedSsfEntries[expectedElement], node.InnerText);
			}
			// Check the Naming node (only node that uses attributes)
			XmlNode namingNode = contents.SelectSingleNode("Naming");
			Assert.IsNotNull(namingNode);
			Assert.AreEqual("pre", namingNode.Attributes.GetNamedItem("PrePart").Value);
			Assert.AreEqual("suf.ext", namingNode.Attributes.GetNamedItem("PostPart").Value);
			Assert.AreEqual("41MAT", namingNode.Attributes.GetNamedItem("BookNameForm").Value);
		}
		#endregion

		#region WriteParatextLdsFile Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the Paratext lds file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void WriteParatextLdsFile()
		{

			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			m_exporter.UsfmEntries.Add(ScrStyleNames.Normal, normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			ldsAccessor.WriteParatextLdsFile("test.lds", Cache.DefaultVernWs, normalEntry);

			// Verify the .lds file
			string[] expectedLds =
			{
				"[General]",
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding,
				"RTL=F",
				"font=Wingdings",
				"name=French",
				"size=14",
				string.Empty,
				"[Checking]",
				string.Empty,
				"[Characters]",
				string.Empty,
				"[Punctuation]"
			};

			using (DummyFileWriter actualLds = new DummyFileWriter())
			{
				actualLds.Open("test.lds");
				actualLds.VerifyOutput(expectedLds);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to write the Paratext lds file when a back translation is being
		/// exported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void WriteParatextLdsFile_BT()
		{

			IWritingSystem wsUr = Cache.ServiceLocator.WritingSystemManager.Get("ur");
			wsUr.RightToLeftScript = true;
			string btWsName = wsUr.DisplayLabel;

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			m_exporter.UsfmEntries.Add(ScrStyleNames.Normal, normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			ldsAccessor.WriteParatextLdsFile("test.lds", wsUr.Handle, normalEntry);

			// Verify the .lds file
			string[] expectedLds =
			{
				"[General]",
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding,
				"RTL=T",
				"font=Wingdings",
				"name=" + btWsName,
				"size=14",
				string.Empty,
				"[Checking]",
				string.Empty,
				"[Characters]",
				string.Empty,
				"[Punctuation]"
			};

			using (DummyFileWriter actualLds = new DummyFileWriter())
			{
				actualLds.Open("test.lds");
				actualLds.VerifyOutput(expectedLds);
			}
		}
		#endregion

		#region UpdateParatextLdsFile Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where nothing changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextLdsFile_NoChange()
		{

			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			m_exporter.UsfmEntries.Add(ScrStyleNames.Normal, normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 10000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Times New Roman";

			string ldsContentsOrig =
				"[General]" + Environment.NewLine +
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding + Environment.NewLine +
				"DialogFontsize=0" + Environment.NewLine +
				"LowerCaseLetters=" + Environment.NewLine +
				"NoCaseLetters=" + Environment.NewLine +
				"RTL=F" + Environment.NewLine +
				"UpperCaseLetters=" + Environment.NewLine +
				"errors=" + Environment.NewLine +
				"font=Times New Roman" + Environment.NewLine +
				"name=French" + Environment.NewLine +
				"separator=" + Environment.NewLine +
				"size=10" + Environment.NewLine +
				Environment.NewLine +
				"[Checking]" + Environment.NewLine +
				Environment.NewLine +
				"[Characters]" + Environment.NewLine +
				Environment.NewLine +
				"[Punctuation]" + Environment.NewLine +
				"diacritics=" + Environment.NewLine +
				"medial=";

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			using (TextWriter tw = FileUtils.OpenFileForWrite("test.lds", Encoding.ASCII))
				ldsAccessor.UpdateLdsContents(ldsContentsOrig, normalEntry, Cache.DefaultVernWs, tw);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[General]",
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding,
				"DialogFontsize=0",
				"LowerCaseLetters=",
				"NoCaseLetters=",
				"RTL=F",
				"UpperCaseLetters=",
				"errors=",
				"font=Times New Roman",
				"name=French",
				"separator=",
				"size=10",
				"",
				"[Checking]",
				"",
				"[Characters]",
				"",
				"[Punctuation]",
				"diacritics=",
				"medial="
			};

			using (DummyFileWriter writer = new DummyFileWriter())
			{
				writer.Open("test.lds");
				writer.VerifyOutput(expectedLdsContents);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that UpdateParatextLdsFile returns false if given a filename for a non-existent
		/// file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextLdsFile_FileMissing()
		{
			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			bool actualReturn = ldsAccessor.UpdateLdsFile("c:\\whatever\\wherever\\whenever\\yeah.lds", new DummyUsfmStyEntry(),
				Cache.DefaultVernWs);

			Assert.IsFalse(actualReturn, "Updating file should have failed for missing file.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where the font and font size are
		/// different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextLdsFile_UpdateFontAndSizeAndAddRTL()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			m_exporter.UsfmEntries.Add(ScrStyleNames.Normal, normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			string ldsContentsOrig =
				"[General]" + Environment.NewLine +
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding + Environment.NewLine +
				"DialogFontsize=0" + Environment.NewLine +
				"LowerCaseLetters=" + Environment.NewLine +
				"NoCaseLetters=" + Environment.NewLine +
				"UpperCaseLetters=" + Environment.NewLine +
				"errors=" + Environment.NewLine +
				"font=Arial" + Environment.NewLine +
				"name=French" + Environment.NewLine +
				"separator=" + Environment.NewLine +
				"size=32" + Environment.NewLine +
				Environment.NewLine +
				"[Checking]" + Environment.NewLine +
				Environment.NewLine +
				"[Characters]" + Environment.NewLine +
				Environment.NewLine +
				"[Punctuation]" + Environment.NewLine +
				"diacritics=" + Environment.NewLine +
				"medial=";

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			using (TextWriter tw = FileUtils.OpenFileForWrite("test.lds", Encoding.ASCII))
				ldsAccessor.UpdateLdsContents(ldsContentsOrig, normalEntry, Cache.DefaultVernWs, tw);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[General]",
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding,
				"DialogFontsize=0",
				"LowerCaseLetters=",
				"NoCaseLetters=",
				"UpperCaseLetters=",
				"errors=",
				"font=Wingdings",
				"name=French",
				"separator=",
				"size=14",
				"RTL=F",
				"",
				"[Checking]",
				"",
				"[Characters]",
				"",
				"[Punctuation]",
				"diacritics=",
				"medial="
			};
			using (DummyFileWriter writer = new DummyFileWriter())
			{
				writer.Open("test.lds");
				writer.VerifyOutput(expectedLdsContents);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where the font and font size are
		/// different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextLdsFile_CriticalStuffMissing()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			m_exporter.UsfmEntries.Add(ScrStyleNames.Normal, normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			string ldsContentsOrig =
				"[General]";

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			using (TextWriter tw = FileUtils.OpenFileForWrite("test.lds", Encoding.ASCII))
				ldsAccessor.UpdateLdsContents(ldsContentsOrig, normalEntry, Cache.DefaultVernWs, tw);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[General]",
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding,
				"font=Wingdings",
				"size=14",
				"name=French",
				"RTL=F"
			};

			using (DummyFileWriter writer = new DummyFileWriter())
			{
				writer.Open("test.lds");
				writer.VerifyOutput(expectedLdsContents);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to update the Paratext lds file, where the font and font size are
		/// different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void UpdateParatextLdsFile_GeneralSectionMissing()
		{
			IWritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			DummyUsfmStyEntry normalEntry = new DummyUsfmStyEntry();
			m_exporter.UsfmEntries.Add(ScrStyleNames.Normal, normalEntry);
			normalEntry.DefaultFontInfo.m_fontSize.ExplicitValue = 14000;
			normalEntry.DefaultFontInfo.m_fontName.ExplicitValue = "Wingdings";

			string ldsContentsOrig =
				"[OtherStuff]";

			ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
			using (TextWriter tw = FileUtils.OpenFileForWrite("test.lds", Encoding.ASCII))
				ldsAccessor.UpdateLdsContents(ldsContentsOrig, normalEntry, Cache.DefaultVernWs, tw);

			// Verify the .lds file
			string[] expectedLdsContents =
			{
				"[OtherStuff]",
				"",
				"[General]",
				"codepage=" + ParatextSsfFileAccessor.kUnicodeEncoding,
				"font=Wingdings",
				"size=14",
				"name=French",
				"RTL=F"
			};

			using (DummyFileWriter writer = new DummyFileWriter())
			{
				writer.Open("test.lds");
				writer.VerifyOutput(expectedLdsContents);
			}
		}
		#endregion

		#region SetStyleMapping Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to set a new style mapping based on a TE Style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void SetStyleMapping_Insert()
		{
			ITsPropsBldr props;

			// The test base already creates all of the standard scripture styles. We just modify
			// one of them here for our test.
			IStStyle mainTitleStyle = m_scr.FindStyle(ScrStyleNames.MainBookTitle);
			Assert.IsNotNull(mainTitleStyle, "Problem in test setup - Main Title should exist");
			props = mainTitleStyle.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvForceOn);
			props.SetIntPropValues((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvDefault, (int)FwTextAlign.ktalCenter);
			props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 20000);
			mainTitleStyle.Rules = props.GetTextProps();

			m_exporter.SetStyleMapping(ScrStyleNames.MainBookTitle, @"\mt");
			UsfmStyEntry entry = (UsfmStyEntry)m_exporter.UsfmEntries[ScrStyleNames.MainBookTitle];
			Assert.IsNotNull(entry);
			Assert.AreEqual(FwTextAlign.ktalCenter, entry.Alignment);
			Assert.IsTrue(entry.FontInfoForWs(-1).m_bold.Value);
			Assert.AreEqual(20000, entry.FontInfoForWs(-1).m_fontSize.Value);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, entry.Name);
			Assert.AreEqual("Title", entry.TextType);
			Assert.AreEqual("Paragraph", entry.StyleType);
			Assert.AreEqual("paragraph publishable vernacular ", entry.TextProperties);
			Assert.IsNull(entry.XmlTag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to set the style mapping, modifying a USFM Sty entry with
		/// the properties from TE's stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
		public void SetStyleMapping_Update()
		{
			DummyUsfmStyEntry entry = new DummyUsfmStyEntry();
			m_exporter.UsfmStyFileAccessor.Add("mt", entry);
			entry.SetUsfmStyProperty(@"\TEStyleName", "Book Title");
			entry.SetUsfmStyProperty(@"\Name", "(mt) Title - Major Title Level 1");
			entry.SetUsfmStyProperty(@"\Description", "The main title of the book (if single level)");
			entry.SetUsfmStyProperty(@"\OccursUnder", "id");
			entry.SetUsfmStyProperty(@"\Rank", "3");
			entry.SetUsfmStyProperty(@"\TextProperties", "paragraph publishable vernacular level_1");
			entry.SetUsfmStyProperty(@"\TextType", "Title");
			entry.SetUsfmStyProperty(@"\StyleType", "Paragraph");
			entry.SetUsfmStyProperty(@"\FontSize", "20");
			entry.SetUsfmStyProperty(@"\Bold", "");
			entry.SetUsfmStyProperty(@"\Justification", "Center");
			entry.SetUsfmStyProperty(@"\SpaceBefore", "8");
			entry.SetUsfmStyProperty(@"\SpaceAfter", "4");

			// The test base already creates all of the standard scripture styles. We just modify
			// one of them here for our test.
			ITsPropsBldr props;
			IStStyle mainTitleStyle = m_scr.FindStyle(ScrStyleNames.MainBookTitle);
			Assert.IsNotNull(mainTitleStyle, "Problem in test setup - Main Title should exist");
			props = mainTitleStyle.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvOff);
			props.SetIntPropValues((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvDefault, (int)FwTextAlign.ktalLeading);
			props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 22000);
			props.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore,
				(int)FwTextPropVar.ktpvMilliPoint, 2000);
			mainTitleStyle.Rules = props.GetTextProps();

			m_exporter.SetStyleMapping(ScrStyleNames.MainBookTitle, @"\mt");
			entry = (DummyUsfmStyEntry)m_exporter.UsfmEntries[ScrStyleNames.MainBookTitle];
			Assert.IsNotNull(entry);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, entry.Name);
			Assert.AreEqual("(mt) Title - Major Title Level 1", entry.P6Name);
			Assert.AreEqual("The main title of the book (if single level)", entry.Description);
			Assert.AreEqual("id", entry.OccursUnder);
			Assert.AreEqual(3, entry.Rank);
			Assert.AreEqual("Title", entry.TextType);
			Assert.AreEqual("Paragraph", entry.StyleType);
			Assert.AreEqual(FwTextAlign.ktalLeading, entry.Alignment);
			Assert.IsFalse(entry.FontInfoForWs(-1).m_bold.Value);
			Assert.AreEqual(22000, entry.FontInfoForWs(-1).m_fontSize.Value);
			Assert.AreEqual("Title", entry.TextType);
			Assert.AreEqual("Paragraph", entry.StyleType);
			Assert.AreEqual("paragraph publishable vernacular level_1 ", entry.TextProperties);
			Assert.AreEqual(4000, entry.SpaceAfter);
			Assert.AreEqual(2000, entry.SpaceBefore);
		}
		#endregion
	}
}
