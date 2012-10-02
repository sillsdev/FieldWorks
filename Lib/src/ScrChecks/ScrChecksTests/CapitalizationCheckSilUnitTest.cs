using NUnit.Framework;
using SILUBS.SharedScrUtils;
using SIL.Utils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TE-style Unit tests for the CapitalizationCheck class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CapitalizationCheckSilUnitTest : ScrChecksTestBase
	{
		private TestChecksDataSource m_dataSource;

		// A subset of serialized style information for seven different classes of styles
		// that require capitalization:
		//  sentence intial styles, proper nouns, tables, lists, special, headings and titles.
		string stylesInfo =
			"<?xml version=\"1.0\" encoding=\"utf-16\"?><StylePropsInfo>" +
			"<SentenceInitial>" +
				"<StyleInfo StyleName=\"Caption\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"DefaultFootnoteCharacters\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"Intro_Paragraph\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Line1\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Mentioned\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"Normal\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Paragraph\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Quoted_Text\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"Refrain\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Speech_Line1\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Variant\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"Variant_Paragraph\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Words_Of_Christ\" StyleType=\"character\" /></SentenceInitial>" +
			"<ProperNouns>" +
				"<StyleInfo StyleName=\"Name_Of_God\" StyleType=\"character\" /></ProperNouns>" +
			"<Table>" +
				"<StyleInfo StyleName=\"Table_Cell_Head\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Table_Cell_Head_Last\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Table_Cell_Last\" StyleType=\"paragraph\" /></Table>" +
			"<List>" +
				"<StyleInfo StyleName=\"List_Item1\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"List_Item2\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"List_Item3\" StyleType=\"paragraph\" /></List>" +
			"<Special>" +
				"<StyleInfo StyleName=\"Embedded_Text_Opening\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Inscription\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"Interlude\" StyleType=\"paragraph\" /></Special>" +
			"<Heading>" +
				"<StyleInfo StyleName=\"Intro_Section_Head\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Section_Head\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Section_Head_Major\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Variant_Section_Head\" StyleType=\"paragraph\" /></Heading>" +
			"<Title>" +
				"<StyleInfo StyleName=\"Title_Main\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"Title_Secondary\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"Title_Tertiary\" StyleType=\"character\" />" +
				"</Title></StylePropsInfo>";

		#region Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up that happens before every test runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_dataSource = new TestChecksDataSource();
			m_dataSource.SetParameterValue("StylesInfo", stylesInfo);
			m_dataSource.SetParameterValue("SentenceFinalPunctuation", ".?!");
			m_check = new CapitalizationCheck(m_dataSource);
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// not capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_Uncapitalized()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("this is nice, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("And is this another nice sentence? ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Yes, this is nice.",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// not a capitalized letter with a diacritic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_UncapitalizedWithDiacritic_SeveralTokens()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"e\u0301 is small latin 'e' with acute in decomposed format, my friend! " +
				"a\u0301 is an 'a' with the same. i\u0301 is an 'i' with the same.",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("o\u0303 is small latin 'o' with tilde, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u00FC is small latin 'u' with diaeresis, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(5, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "e\u0301", "Sentence should begin with a capital letter");
			CheckError(1, m_dataSource.m_tokens[0].Text, 66, "a\u0301", "Sentence should begin with a capital letter");
			CheckError(2, m_dataSource.m_tokens[0].Text, 94, "i\u0301", "Sentence should begin with a capital letter");
			CheckError(3, m_dataSource.m_tokens[1].Text, 0, "o\u0303", "Sentence should begin with a capital letter");
			CheckError(4, m_dataSource.m_tokens[2].Text, 0, "\u00FC", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a list paragraph is
		/// not a capitalized letter with a diacritic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_UncapitalizedWithDiacritic_SeveralTokensInNotes()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"e\u0301 is small latin 'e' with acute in decomposed format, my friend! " +
				"a\u0301 is an 'a' with the same. i\u0301 is an 'i' with the same.",
				TextType.Verse, true, true, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("o\u0303 is small latin 'o' with tilde, my friend! ",
				TextType.Verse, true, true, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u00FC is small latin 'u' with diaeresis, my friend! ",
				TextType.Verse, true, false, "Line1"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(5, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "e\u0301", "Sentence should begin with a capital letter");
			CheckError(1, m_dataSource.m_tokens[0].Text, 66, "a\u0301", "Sentence should begin with a capital letter");
			CheckError(2, m_dataSource.m_tokens[0].Text, 94, "i\u0301", "Sentence should begin with a capital letter");
			CheckError(3, m_dataSource.m_tokens[1].Text, 0, "o\u0303", "Sentence should begin with a capital letter");
			CheckError(4, m_dataSource.m_tokens[2].Text, 0, "\u00FC", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// not capitalized letter with a diacritic that is preceeded by quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_UncapitalizedWithDiacritic_QuotesBefore()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"\"e\u0301 is small latin 'e' with acute in decomposed format, my friend! ",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 1, "e\u0301", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// not capitalized letter with multiple diacritics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_UncapitalizedWithMultipleDiacritics()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"u\u0301\u0302\u0327 is small latin 'u' with circumflex, acute accent and cedilla. ",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "u\u0301\u0302\u0327",
				"Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// uncapitalized letter with a diacritic made of two decomposed characters. TE-6862
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_UncapitalizedDecomposedLetter()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"\u0061\u0301 is small latin a with a combining acute accent, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "\u0061\u0301", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// a non-Roman character in a writing system that does not use capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithNoCaseNonRoman()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0E01 is the Thai letter Ko Kai.",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// a no case PUA.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithNoCasePUA()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"Character in next sentence is no case PUA character. \uEE00",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is a
		/// latin capital letter D with tsmall letter z with caron.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLatinExtendedCap()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("\u01C5 is a latin extended capital.",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a chapter
		/// number followed by verse number followed by lowercase letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterChapterVerse()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.ChapterNumber,
				true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a verse
		/// followed by lowercase letter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterVerse()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a chapter
		/// number followed by lowercase letter (verse number one is implied).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterChapter()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a chapter
		/// number, verse number and footnote marker followed by lowercase letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterChapterVerseAndNote()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Footnote Text", TextType.Note,
				false, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a verse number
		/// and footnote marker followed by lowercase letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterVerseAndNote()
		{
			// Check when the footnote marker run is considered a run that starts a paragraph.
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Footnote Text", TextType.Note,
				true, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);

			// Check when the footnote marker run is not considered
			// a run that starts a paragraph.
			m_errors.Clear();
			m_dataSource.m_tokens.Clear();
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Footnote Text", TextType.Note,
				false, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a chapter number
		/// and footnote marker followed by lowercase letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterChapterAndNote()
		{
			// Check when the footnote marker run is considered a run that starts a paragraph.
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Footnote Text", TextType.Note,
				true, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);

			// Check when the footnote marker run is not considered
			// a run that starts a paragraph.
			m_errors.Clear();
			m_dataSource.m_tokens.Clear();
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Footnote Text", TextType.Note,
				false, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins a footnote marker
		/// followed by lowercase letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterNote()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("Footnote Text", TextType.Note,
				true, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check one footnote ends with a period and the next
		/// footnote begins with lower case.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Footnotes_TreatedSeparately()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("This is footnote one.", TextType.Note,
				true, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("footnote two", TextType.Note,
				true, false, "Note General Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a picture ORC
		/// followed by lowercase letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterPicture()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("Picture Caption", TextType.PictureCaption,
				true, false, "Caption"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the paragraph begins with a verse number
		/// and a picture ORC followed by lowercase letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_StartsWithLCaseAfterVersePicture()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Picture Caption", TextType.PictureCaption,
				true, false, "Caption"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one", TextType.Verse,
				false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LCaseInRunAfterNote()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("This is before a footnote marker",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Footnote Text",
				TextType.Note, true, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("this is after a footnote marker",
				TextType.Verse,	false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LCaseInRunAfterPicture()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("This is before a picture",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("My picture caption",
				TextType.PictureCaption, true, true, "Caption"));
			m_dataSource.m_tokens.Add(new DummyTextToken("this is after the picture",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LCaseInRunAfterVerse()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("This is before a verse",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, true, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("this is after a verse",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the check catches the case where a verse starts with a lowercase letter
		/// when the preceding verse ended with sentence-end punctuation. TE-8050
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LCaseInRunAfterSentenceEndPunctAndVerse()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("This is verse one.",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, true, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("this is verse two.",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[3].Text, 0, "t", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// not capitalized and para begins with quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_UncapitalizedWithQuotes()
		{
			//201C = Left double quotation mark
			//2018 = Left single quotation mark
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"\u201C \u2018this is an uncaptialized para with quotes, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 3, "t", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first word-forming character is ' : 'tis so.
		/// The ' is the first character of the sentence and the 't' should be capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Sentence_UncapitalizedWithApostrophe()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"Yes! 'tis an uncaptialized sentence with apostrophe before the first lowercase letter!",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 6, "t", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first word-forming character is ' : 'Tis so.
		/// The ' is the first character of the sentence and the 'T' is capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Sentence_CapitalizedWithApostrophe()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"Yes! 'Tis an uncaptialized sentence with apostrophe before the first lowercase letter!",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// capitalized and para begins with quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph_CapitalizedWithQuotes()
		{
			//201C = Left double quotation mark
			//2018 = Left single quotation mark
			m_dataSource.m_tokens.Add(new DummyTextToken(
				"\u201C \u2018This is an uncaptialized para with quotes, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the proper
		/// name is capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CapitalizedProperName_ParaStart()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("This",
				TextType.Verse, true, false, "Paragraph", "Name Of God"));
			m_dataSource.m_tokens.Add(new DummyTextToken(
				" is a proper name, my friend! ",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the proper
		/// name is uncapitalized at the start of a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedProperName_ParaStart()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("this",
				TextType.Verse, true, false, "Paragraph", "Name Of God"));
			m_dataSource.m_tokens.Add(new DummyTextToken(
				" is a proper name, my friend! ",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			// This word should be capitalized for two reasons: it occurs sentence initially and it
			// is a proper noun.
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the proper
		/// name is uncapitalized at the start of the paragraph that does not have to be
		/// capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedProperName_ParaStart2()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("this",
				TextType.Verse, true, false, "UncapitalizedParaStyle", "Name Of God"));
			m_dataSource.m_tokens.Add(new DummyTextToken(
				" is a proper name, my friend! ",
				TextType.Verse, false, false, "UncapitalizedParaStyle"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			// This word should be capitalized for two reasons: it occurs sentence initially and it
			// is a proper noun.
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Proper nouns should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the proper
		/// name is uncapitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CapitalizedProperName_NotParaStart()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("The ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Lord",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" is ",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("God!",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the proper
		/// names are uncapitalized when they are not at the start of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedProperName_NotParaStart()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("The ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("lord",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" is ",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("god!",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[1].Text, 0, "l", "Proper nouns should begin with a capital letter");
			CheckError(1, m_dataSource.m_tokens[3].Text, 0, "g", "Proper nouns should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the proper
		/// names are uncapitalized when they are not at the start of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedParagraph_WithCapProperName()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("the ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Lord",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" is ",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("God!",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the proper
		/// names are uncapitalized when they are not at the start of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedParaStartAndProperName()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("the ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("lord",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" is ",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("god!",
				TextType.Verse, false, false, "Paragraph", "Name Of God"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(3, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Sentence should begin with a capital letter");
			CheckError(1, m_dataSource.m_tokens[1].Text, 0, "l", "Proper nouns should begin with a capital letter");
			CheckError(2, m_dataSource.m_tokens[3].Text, 0, "g", "Proper nouns should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// uncapitalized. A non-initial sentence is also not capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedPara_WithEmbeddedUncapitalizedSentence()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("this sentence isn't capitalized. " +
				"this one isn't either.", TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Sentence should begin with a capital letter");
			CheckError(1, m_dataSource.m_tokens[0].Text, 33, "t", "Sentence should begin with a capital letter");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of the paragraph is
		/// uncapitalized. Followed by an uncapitalized sentence containing the words of Christ,
		/// i.e. it should be capitalized for two reasons but we want only one error report
		/// from this uncapitalized letter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedPara_WithEmbeddedWordsOfChrist()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("and the Lord said! ", TextType.Verse,
				true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\"if you love me, you will obey my commands.\"", TextType.Verse,
							false, false, "Paragraph", "Words Of Christ"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "a", "Sentence should begin with a capital letter");
			CheckError(1, m_dataSource.m_tokens[1].Text, 1, "i", "Sentence should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a heading is
		/// capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CapitalizedHeading()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("The title of this section",
				TextType.Other, true, false, "Section Head"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a heading is not
		/// uncapitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedHeading()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("the title of this section",
				TextType.Other, true, false, "Section Head"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Heading should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a title is
		/// capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CapitalizedTitle()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("The title of this book",
				TextType.Other, true, false, "Title Main"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a title is not
		/// uncapitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedTitle()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("the title of this book",
				TextType.Other, true, false, "Title Main"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "t", "Title should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a list item is
		/// capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CapitalizedList()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("An item in a list",
				TextType.Other, true, false, "List Item1"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a list item is not
		/// uncapitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedList()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("an item in a list",
				TextType.Other, true, false, "List Item1"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "a", "List paragraphs should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a table entry is
		/// capitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CapitalizedTableCellHead()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("An entry in a table",
				TextType.Other, true, false, "Table Cell Head"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Uncapitalized styles check when the first letter of a table entry is not
		/// uncapitalized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UncapitalizedTableCellHead()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("an item in a list",
				TextType.Other, true, false, "Table Cell Head"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "a", "Table contents should begin with a capital letter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the length of a character (including diacritics) from a specified offset.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLengthOfChar()
		{
			CapitalizationProcessor processor = new CapitalizationProcessor(m_dataSource, null);

			Assert.AreEqual(1, ReflectionHelper.GetIntResult(processor, "GetLengthOfChar",
				new DummyTextToken("a has no diacritics.", TextType.Verse, true, false,
				"Paragraph"), 0));
			Assert.AreEqual(2, ReflectionHelper.GetIntResult(processor, "GetLengthOfChar",
				new DummyTextToken("a\u0303 has a tilde.", TextType.Verse, true, false,
				"Paragraph"), 0));
			Assert.AreEqual(3, ReflectionHelper.GetIntResult(processor, "GetLengthOfChar",
				new DummyTextToken("a\u0303\u0301 has a tilde and grave accent.",
				TextType.Verse, true, false, "Paragraph"), 0));
			Assert.AreEqual(4, ReflectionHelper.GetIntResult(processor, "GetLengthOfChar",
				new DummyTextToken("a\u0303\u0301\u0302 has a tilde, grave accent and circumflex accent.",
				TextType.Verse, true, false, "Paragraph"), 0));
		}
		#endregion
	}
}
