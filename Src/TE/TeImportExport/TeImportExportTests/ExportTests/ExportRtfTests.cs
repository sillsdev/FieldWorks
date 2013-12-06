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
// File: ExportRtfTests.cs
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;
using System.Drawing;
using System.Text;
using System.IO;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE.ExportTests
{
	#region DummyRtfStyle class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy RTF style class to expose internal stuff for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyRtfStyle: RtfStyle
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF style with everything fully specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyRtfStyle(string styleName,
			StyleType styleType,
			bool bold,
			bool italic,
			FwSuperscriptVal superSub,
			FwTextAlign paraAlign,
			int lineSpacing,
			bool fLineSpacingRelative,
			int spaceBefore,
			int spaceAfter,
			int firstLineIndent,
			int leadingIndent,
			int trailingIndent,
			string fontName, // Pass null to leave as inherited
			int fontSize,
			Color fontColor, // Pass Color.Empty to leave as inherited
			int borderTop,
			int borderBottom,
			int borderLeading,
			int borderTrailing,
			Color borderColor, // Pass Color.Empty to leave as inherited
			string basedOnStyleName,
			string nextStyleName) :
			this(styleName, styleType, spaceBefore, spaceAfter, firstLineIndent, leadingIndent,
				trailingIndent, basedOnStyleName, nextStyleName)
		{
			m_defaultFontInfo.m_bold.ExplicitValue = bold;
			if (fontName != null)
				m_defaultFontInfo.m_fontName.ExplicitValue = fontName;
			if (fontColor != Color.Empty)
				m_defaultFontInfo.m_fontColor.ExplicitValue = fontColor;
			m_defaultFontInfo.m_fontSize.ExplicitValue = fontSize;
			m_defaultFontInfo.m_italic.ExplicitValue = italic;
			m_defaultFontInfo.m_superSub.ExplicitValue = superSub;
			// TODO(TE-4649): Implement unlerline property for RTF export
			m_defaultFontInfo.m_underline = new InheritableStyleProp<FwUnderlineType>(FwUnderlineType.kuntNone);
			m_alignment.ExplicitValue = paraAlign;
			m_lineSpacing.ExplicitValue = new LineHeightInfo(lineSpacing, fLineSpacingRelative);
			m_border.ExplicitValue = new BorderThicknesses(borderLeading, borderTrailing, borderTop, borderBottom);
			if (borderColor != Color.Empty)
				m_borderColor.ExplicitValue = borderColor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF style with the given name and type. Everything else is
		/// inherited/default.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyRtfStyle(string styleName, StyleType styleType)
			: base(-1)
		{
			m_name = styleName;
			m_usage = null;
			m_styleType = styleType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF style with the given name, type, based-on style and next
		/// style. Everything else is inherited/default.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyRtfStyle(string styleName, StyleType styleType,
			string basedOnStyleName, string nextStyleName)
			: base(-1)
		{
			m_name = styleName;
			m_usage = null;
			m_styleType = styleType;
			m_basedOnStyleName = basedOnStyleName;
			m_nextStyleName = nextStyleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF style with explicit overrides for most of the paragraph
		/// settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyRtfStyle(string styleName,
			StyleType styleType,
			int spaceBefore,
			int spaceAfter,
			int firstLineIndent,
			int leadingIndent,
			int trailingIndent,
			string basedOnStyleName,
			string nextStyleName) :
			this(styleName, styleType, basedOnStyleName, nextStyleName)
		{
			m_spaceBefore.ExplicitValue = spaceBefore;
			m_spaceAfter.ExplicitValue = spaceAfter;
			m_firstLineIndent.ExplicitValue = firstLineIndent;
			m_leadingIndent.ExplicitValue = leadingIndent;
			m_trailingIndent.ExplicitValue = trailingIndent;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF style with the given name, type, explicit bold value and an
		/// explicit font size. Everything else is inherited/default. This one is mainly useful
		/// for a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyRtfStyle(string styleName, StyleType styleType, bool bold, int fontSize)
			: this(styleName, styleType)
		{
			m_defaultFontInfo.m_bold.ExplicitValue = bold;
			m_defaultFontInfo.m_fontSize.ExplicitValue = fontSize;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF style with the given name, type, base-on stlye, next style,
		/// and an explicit line-spacing value. Everything else is inherited/default. This one
		/// is useful for creating paragraph styles with "exact" or "at-least" line-spacing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyRtfStyle(string styleName, StyleType styleType, int lineSpacing,
			bool fLineSpacingRelative, string basedOnStyleName, string nextStyleName)
			: this(styleName, styleType, basedOnStyleName, nextStyleName)
		{
			m_lineSpacing.ExplicitValue = new LineHeightInfo(lineSpacing, fLineSpacingRelative);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="RtfStyle.LineSpacingAsString"/>
		/// </summary>
		/// <value>The line spacing as an RTF string.</value>
		/// ------------------------------------------------------------------------------------
		public string GetLineSpacingAsString
		{
			get { return LineSpacingAsString; }
		}
	}
	#endregion

	#region DummyExportRtf class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exposes portions of ExportRtf for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyExportRtf : ExportRtf
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF exporter for vernacular Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyExportRtf(string fileName, FdoCache cache, FwStyleSheet styleSheet) :
			base(fileName, cache, null, ExportContent.Scripture, 0, styleSheet, null)
		{
			m_writer = FileUtils.OpenFileForWrite(fileName, Encoding.ASCII);
			BuildStyleTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a dummy RTF exporter for back translations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyExportRtf(string fileName, FdoCache cache, FwStyleSheet styleSheet, int wsBt) :
			base(fileName, cache, null, ExportContent.BackTranslation, wsBt, styleSheet, null)
		{
			m_writer = FileUtils.OpenFileForWrite(fileName, Encoding.ASCII);
			BuildStyleTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the ExportFootnote method
		/// </summary>
		/// <param name="footnote"></param>
		/// ------------------------------------------------------------------------------------
		public void CallExportFootnote(IScrFootnote footnote)
		{
			m_rtfStyleTable.ConnectStyles();
			base.ExportFootnote(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the ExportFootnote method
		/// </summary>
		/// <param name="footnote">The footnote.</param>
		/// <param name="wsBt">The writing system of the back translation to export.</param>
		/// ------------------------------------------------------------------------------------
		public void CallExportFootnote(IScrFootnote footnote, int wsBt)
		{
			m_rtfStyleTable.ConnectStyles();
			base.ExportFootnote(footnote, wsBt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the export paragraph method.
		/// </summary>
		/// <param name="para">The paragraph to export.</param>
		/// ------------------------------------------------------------------------------------
		public void CallExportParagraph(IScrTxtPara para)
		{
			base.ExportParagraph(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the output file so it can be checked
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseOutputFile()
		{
			m_writer.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the ExportRun method.
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		public void CallExportRun(string text)
		{
			ExportRun(text);
		}
	}
	#endregion

	#region ExportRtfTests test
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test fixture for RTF export
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportRtfTests : BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixtures the setup.
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

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting the string version of RTF styles in a left-to-right world
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RtfStyle_ToString_LeftToRight()
		{
			// Build up a style table for testing
			RtfStyleInfoTable styleTable = new RtfStyleInfoTable(null, false);
			BuildStyleTable(styleTable);

			// Create a "Paragraph" style that is based on Normal (#2)
			DummyRtfStyle paragraph = new DummyRtfStyle("\\MyFavoriteStyle", StyleType.kstParagraph,
				72000,		// space before
				36000,		// space after
				18000,		// first line indent
				9000,		// leading indent
				4500,		// trailing indent
				"Normal",	// based-on style name
				null);		// next style name
			styleTable.Add("\\MyFavoriteStyle", paragraph);
			styleTable.ConnectStyles();

			RtfStyle style = (RtfStyle)styleTable["Normal"];
			string rtfString = style.ToString("Normal", true);
			Assert.AreEqual(@"\s1\f1\fs20\snext1 Normal", rtfString);

			style = (RtfStyle)styleTable["Paragraph"];
			rtfString = style.ToString("Paragraph", true);
			Assert.AreEqual(@"\s2\fi360\lin180\rin90\sb1440\sa720\f1\fs20\sbasedon1\snext2 Paragraph", rtfString);

			style = (RtfStyle)styleTable["Heading"];
			rtfString = style.ToString("Heading", true);
			Assert.AreEqual(@"\s3\f1\fs20\sbasedon1\snext2 Heading", rtfString);

			style = (RtfStyle)styleTable["Emphasis"];
			rtfString = style.ToString("Emphasis", true);
			Assert.AreEqual(@"\*\cs4\b\f1\fs40\additive Emphasis", rtfString);

			style = (RtfStyle)styleTable["ExactSpacing"];
			rtfString = style.ToString("ExactSpacing", true);
			Assert.AreEqual(@"\s5\sl-360\slmult0\f1\fs20\sbasedon1\snext2 ExactSpacing", rtfString);

			style = (RtfStyle)styleTable["AtLeastSpacing"];
			rtfString = style.ToString("AtLeastSpacing", true);
			Assert.AreEqual(@"\s6\sl360\f1\fs20\sbasedon1\snext2 AtLeastSpacing", rtfString);

			style = (RtfStyle)styleTable["Border"];
			rtfString = style.ToString("Border", true);
			Assert.AreEqual(@"\s7\qj\f2\fs40" +
				@"\brdrt\brdrs\brdrw10\brsp20\brdrcf1" +
				@"\brdrb\brdrs\brdrw20\brsp20\brdrcf1" +
				@"\brdrl\brdrs\brdrw30\brsp80\brdrcf1" +
				@"\brdrr\brdrs\brdrw40\brsp80\brdrcf1\nosupersub\snext7" +
				" Border", rtfString);

			style = (RtfStyle)styleTable["\\MyFavoriteStyle"];
			rtfString = style.ToString("\\MyFavoriteStyle", true);
			Assert.AreEqual(@"\s8\fi360\lin180\rin90\sb1440\sa720\f1\fs20\sbasedon1\snext8 \\MyFavoriteStyle", rtfString);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting the string version of RTF styles in a right-to-left world
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RtfStyle_ToString_RightToLeft()
		{
			// Build up a font table and style table for testing
			RtfStyleInfoTable styleTable = new RtfStyleInfoTable(null, true);
			BuildStyleTable(styleTable);

			RtfStyle style = (RtfStyle)styleTable["Normal"];
			string rtfString = style.ToString("Normal", true);
			Assert.AreEqual(@"\s1\rtlpar\qr\f1\fs20\snext1 Normal", rtfString);

			style = (RtfStyle)styleTable["Paragraph"];
			rtfString = style.ToString("Paragraph", true);
			Assert.AreEqual(@"\s2\rtlpar\qr\fi360\lin180\rin90\sb1440\sa720\f1\fs20\sbasedon1\snext2 Paragraph", rtfString);

			style = (RtfStyle)styleTable["Heading"];
			rtfString = style.ToString("Heading", true);
			Assert.AreEqual(@"\s3\rtlpar\qr\f1\fs20\sbasedon1\snext2 Heading", rtfString);

			style = (RtfStyle)styleTable["Emphasis"];
			rtfString = style.ToString("Emphasis", true);
			Assert.AreEqual(@"\*\cs4\b\f1\fs40\additive Emphasis", rtfString);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting the line-spacing value as an RTF string
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RtfStyle_LineSpacingAsString()
		{
			DummyRtfStyle style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				10000, false, null, null);
			Assert.AreEqual(@"\sl200", style.GetLineSpacingAsString);
			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				-10000, false, null, null);
			Assert.AreEqual(@"\sl-200\slmult0", style.GetLineSpacingAsString);
			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				10000, true, null, null);
			Assert.AreEqual(string.Empty, style.GetLineSpacingAsString);

			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				15000, false, null, null);
			Assert.AreEqual(@"\sl300", style.GetLineSpacingAsString);
			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				-15000, false, null, null);
			Assert.AreEqual(@"\sl-300\slmult0", style.GetLineSpacingAsString);
			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				15000, true, null, null);
			Assert.AreEqual(@"\sl360\slmult1", style.GetLineSpacingAsString);

			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				20000, false, null, null);
			Assert.AreEqual(@"\sl400", style.GetLineSpacingAsString);
			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				-20000, false, null, null);
			Assert.AreEqual(@"\sl-400\slmult0", style.GetLineSpacingAsString);
			style = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				20000, true, null, null);
			Assert.AreEqual(@"\sl480\slmult1", style.GetLineSpacingAsString);
		}

		#region helpers
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Build a table of styles for testing
		/// </summary>
		///--------------------------------------------------------------------------------------
		private void BuildStyleTable(RtfStyleInfoTable styleTable)
		{
			// Create a Normal style for everything to be based on (#1)
			DummyRtfStyle normal = new DummyRtfStyle("Normal", StyleType.kstParagraph);
			normal.FontInfoForWs(-1).m_fontName.ExplicitValue = "Times New Roman";
			styleTable.Add("Normal", normal);

			// Create a "Paragraph" style that is based on Normal (#2)
			DummyRtfStyle paragraph = new DummyRtfStyle("Paragraph", StyleType.kstParagraph,
				72000,		// space before
				36000,		// space after
				18000,		// first line indent
				9000,		// leading indent
				4500,		// trailing indent
				"Normal",	// based-on style name
				null);		// next style name
			styleTable.Add("Paragraph", paragraph);

			// Create a Heading style that is based on "Normal" and has "Paragraph"
			// as a following style (#3)
			DummyRtfStyle heading = new DummyRtfStyle("Heading", StyleType.kstParagraph,
				"Normal",			// based-on style name
				"Paragraph");		// next style name
			styleTable.Add("Heading", heading);

			// Create a character style "Emphasis" (#4)
			DummyRtfStyle emphasis = new DummyRtfStyle("Emphasis", StyleType.kstCharacter, true, 20000);
			emphasis.FontInfoForWs(-1).m_fontName.ExplicitValue = "Times New Roman";
			styleTable.Add("Emphasis", emphasis);

			// Create a paragraph style that has exact spacing (#5)
			DummyRtfStyle exactSpacing = new DummyRtfStyle("ExactSpacing", StyleType.kstParagraph,
				-18000,				// line spacing
				false,				// line spacing relativity
				"Normal",			// based-on style name
				"Paragraph");		// next style name

			styleTable.Add("ExactSpacing", exactSpacing);

			// Create a paragraph style that has "at least n" spacing (#6)
			DummyRtfStyle atLeastSpacing = new DummyRtfStyle("AtLeastSpacing", StyleType.kstParagraph,
				18000,				// line spacing
				false,				// line spacing relativity
				"Normal",			// based-on style name
				"Paragraph");		// next style name

			styleTable.Add("AtLeastSpacing", atLeastSpacing);

			// Create a Border style (#7)
			DummyRtfStyle border = new DummyRtfStyle("Border",
				StyleType.kstParagraph,
				false,		// bold
				false,		// italic
				FwSuperscriptVal.kssvOff,
				FwTextAlign.ktalJustify,
				0,				// line spacing
				false,			// line spacing relativity
				0,				// space before
				0,				// space after
				0,				// first line indent
				0,				// leading indent
				0,				// trailing indent
				"Arial",			// font name
				20000,				// font size
				Color.Empty,	// font color
				500,			// border top
				1000,			// border bottom
				1500,			// border leading
				2000,			// border trailing
				Color.Black,	// border color
				null,			// based-on style name
				null);			// next style name
			styleTable.Add("Border", border);

			styleTable.ConnectStyles();
		}
		#endregion
	}
	#endregion

	#region ExportRtfTests_InMemCache tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ExportRtf tests that require an in-memory cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportRtfTests_InMemCache : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		private IScrSection m_section;
		private IScrTxtPara m_para;
		private string m_fileName;
		private FwStyleSheet m_styleSheet;
		private DummyExportRtf m_exporter;
		#endregion

		#region Initialization and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixtures the setup.
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
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_book = AddBookToMockedScripture(1, "Genesis");
			m_section = AddSectionToMockedBook(m_book);
			m_para = AddParaToMockedSectionContent(m_section, ScrStyleNames.NormalParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the stylesheet and exporter
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			// Create an RtfExport object
			m_fileName = FileUtils.GetTempFile("tmp");
			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo,
				ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);
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
			m_styleSheet = null;
			m_exporter = null;

			m_book = null;
			m_section = null;
			m_para = null;

			base.TestTearDown();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a footnote with Unicode text in the marker and text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportFootnote_Unicode()
		{
			int markerStyleNumber =
				m_styleSheet.FindStyle(ScrStyleNames.FootnoteMarker).IndexInOwner + 1;
			int footnoteStyleNumber =
				m_styleSheet.FindStyle(ScrStyleNames.NormalFootnoteParagraph).IndexInOwner + 1;

			ITsStrFactory stringFactory = TsStrFactoryClass.Create();

			// Create a book with a section that has a paragraph with some text and a footnote!
			AddRunToMockedPara(m_para, "My text for footnotes", string.Empty);
			IScrFootnote footnote = AddFootnote(m_book, m_para, 2);
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			m_scr.FootnoteMarkerSymbol = "\u00B6"; // U+00B6 PILCROW SIGN (¶)
			m_scr.DisplaySymbolInFootnote = true;
			footnote.FootnoteMarker = stringFactory.MakeString(null, Cache.DefaultVernWs);
			IScrTxtPara footnotePara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				footnote, ScrStyleNames.NormalFootnoteParagraph);
			string footnoteText = "C\u00f2\u00f3\u013a footnot\u0113";
			footnotePara.Contents = stringFactory.MakeString(footnoteText, Cache.DefaultVernWs);

			m_exporter = new DummyExportRtf(m_fileName, Cache, m_styleSheet);
			m_exporter.CallExportFootnote(footnote);

			// Verify the contents of the temp file
			m_exporter.CloseOutputFile();
			using (TextReader file = FileUtils.OpenFileForRead(m_fileName, Encoding.ASCII))
			{
				string line1 = file.ReadLine();
				string line2 = file.ReadLine();

				// the first line is the marker emitted into the text stream.
				// Footnote markers now have the footnote marker style applied
				Assert.AreEqual(@"\*\cs" + markerStyleNumber + @" \uc0\u182 }", line1);
				Assert.AreEqual(@"{\footnote \pard\plain \s" + footnoteStyleNumber +
					@"\f1\fs20{" + @"\*\cs" + markerStyleNumber + @" \uc0\u182 " +
					@"}{ }{Co\uc0\u768 o\uc0\u769 l\uc0\u769  footnote\uc0\u772 }",
					line2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a footnote that is a missing object. (TE-5501)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportFootnote_Missing()
		{
			int markerStyleNumber =
				m_styleSheet.FindStyle(ScrStyleNames.FootnoteMarker).IndexInOwner + 1;
			int footnoteStyleNumber =
				m_styleSheet.FindStyle(ScrStyleNames.NormalFootnoteParagraph).IndexInOwner + 1;

			ITsStrFactory stringFactory = TsStrFactoryClass.Create();

			// Create a book with a section that has a paragraph with some text and a footnote!
			AddRunToMockedPara(m_para, "My text for footnotes", string.Empty);
			IStFootnote testFootnote = AddFootnote(m_book, m_para, m_para.Contents.Length, string.Empty);

			m_exporter = new DummyExportRtf(m_fileName, Cache, m_styleSheet);
			m_exporter.CallExportParagraph(m_para);

			// Verify the contents of the temp file
			m_exporter.CloseOutputFile();
			using (TextReader file = FileUtils.OpenFileForRead(m_fileName, Encoding.ASCII))
			{
				string line = file.ReadLine();

				// the line is the paragraph without the footnote marker.
				Assert.AreEqual(@"\pard\plain\s2\f1\fs20{My text for footnotes}", line);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph that has a run beginning with a bogus ORC. (TE-6088)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportTextWithBraces()
		{
			m_exporter = new DummyExportRtf(m_fileName, Cache, m_styleSheet);
			m_exporter.CallExportRun("This {is} a (test}.");

			// Verify the contents of the temp file
			m_exporter.CloseOutputFile();
			using (TextReader file = FileUtils.OpenFileForRead(m_fileName, Encoding.ASCII))
			{
				string line = file.ReadLine();
				Assert.AreEqual(@"This \{is\} a (test\}.", line);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting back translations of footnotes to RTF. (TE-4831)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportRtfBtFootnotes()
		{
			// Set up the vernacular text.
			AddTitleToMockedBook(m_book, "Genesis");
			AddRunToMockedPara(m_para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(m_para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(m_para, "yi kuch batchit he.", null);
			IScrFootnote footnote1 = AddFootnote(
				m_book, m_para, 2, "pehla pao wala note");

			// Set up back translation.
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation transPara1 = AddBtToMockedParagraph(m_para, wsBt);
			AddRunToMockedTrans(transPara1, wsBt, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(transPara1, wsBt, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(transPara1, wsBt, "This is some text.", null);
			AddBtFootnote(transPara1, 4, wsBt, footnote1, "First footnote");

			m_scr.DisplaySymbolInFootnote = true;

			// Export the back translation of the footnote.
			m_exporter = new DummyExportRtf(m_fileName, Cache, m_styleSheet, wsBt);
			m_exporter.CallExportFootnote(footnote1, wsBt);
			m_exporter.CloseOutputFile();

			int markerStyleNumber =
				m_styleSheet.FindStyle(ScrStyleNames.FootnoteMarker).IndexInOwner + 1;
			int footnoteStyleNumber =
				m_styleSheet.FindStyle(ScrStyleNames.NormalFootnoteParagraph).IndexInOwner + 1;
			using (TextReader file = FileUtils.OpenFileForRead(m_fileName, Encoding.ASCII))
			{
				string line1 = file.ReadLine();
				string line2 = file.ReadLine();

				// the first line is the marker emitted into the text stream.
				// Footnote markers now have the footnote marker style applied
				Assert.AreEqual(@"\*\cs" + markerStyleNumber + @" a}", line1);
				Assert.AreEqual(@"{\footnote \pard\plain \s" + footnoteStyleNumber +
					@"\f1\fs20{" + @"\*\cs" + markerStyleNumber + @" a" + @"}{ }{First footnote}", line2);
			}
		}
		#endregion
	}
	#endregion
}
