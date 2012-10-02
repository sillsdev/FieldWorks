// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrStyleNames.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class encapsulates several named styles which have special meaning for Scripture
	/// handling. Stylenames that would otherwise need to have hard-coded values in code should
	/// instead be added as properties to this class. This will facilitate renaming and also
	/// make it easier to switch to integer-based IDs some day.
	/// </summary>
	/// <remarks>
	/// The stylename properties represented by this class may eventually change to return
	/// an integer ID instead of a string representation of the style name. In any event,
	/// these values are NOT localizable. They are the absolute representation of the styles
	/// and will not necessarily be viewable by the user (though at least for now they are).
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public static class ScrStyleNames
	{
		/// <summary>Normal Scripture Paragraph</summary>
		public const string NormalParagraph = "Paragraph";

		/// <summary>Scripture chapter number</summary>
		public const string ChapterNumber = "Chapter Number";

		/// <summary>Scripture verse number</summary>
		public const string VerseNumber = "Verse Number";

		/// <summary>Scripture book title paragraph</summary>
		public const string MainBookTitle = "Title Main";

		/// <summary>Scripture secondary book title paragraph</summary>
		public const string SecondaryBookTitle = "Title Secondary";

		/// <summary>Scripture tertiary book title paragraph</summary>
		public const string TertiaryBookTitle = "Title Tertiary";

		/// <summary>normal Scripture section head paragraph</summary>
		public const string SectionHead = "Section Head";

		/// <summary>normal Scripture book introduction paragraph</summary>
		public const string IntroParagraph = "Intro Paragraph";

		/// <summary>Scripture book introduction section head paragraph</summary>
		public const string IntroSectionHead = "Intro Section Head";

		/// <summary>Scripture footnote marker</summary>
		public const string FootnoteMarker = "Note Marker";

		/// <summary>Normal Scripture footnote</summary>
		public const string NormalFootnoteParagraph = "Note General Paragraph";

		/// <summary>Cross-reference Scripture footnote</summary>
		public const string CrossRefFootnoteParagraph = "Note Cross-Reference Paragraph";

		/// <summary>Footnote target reference</summary>
		public const string FootnoteTargetRef = "Note Target Reference";

		/// <summary>default style for the Annotation context</summary>
		public const string Remark = "Remark";

		/// <summary>Style ID of the Untranslated Word for the back translation</summary>
		public const string UntranslatedWord = "Untranslated Word";

		/// <summary>label to use in the annotation view (internal style)</summary>
		public const string NotationTag = "Notation Tag";

		/// <summary>Style ID of the Caption style</summary>
		public const string Figure = "Caption";

		/// <summary>Style ID of the Canonical Reference style, needed for import mapping</summary>
		public const string CanonicalRef = "Canonical Reference";

		/// <summary>Style ID of the base style: "Normal"</summary>
		public const string Normal = "Normal";

		/// <summary>Style ID of the Header style, to be used (see TE-2576) for headers and footers</summary>
		public const string Header = "Header";

		/// <summary>Identifies a first-level item in a list</summary>
		public const string ListItem1 = "List Item1";

		/// <summary>Identifies a second-level item in a list</summary>
		public const string ListItem2 = "List Item2";

		/// <summary>Identifies a third-level item in a list</summary>
		public const string ListItem3 = "List Item3";

		/// <summary>Identifies an additional paragraph of prose in a first-level item in a list</summary>
		public const string ListItem1Additional = "List Item1 Additional";

		/// <summary>Identifies an additional paragraph of prose in a second-level item in a list</summary>
		public const string ListItem2Additional = "List Item2 Additional";

		/// <summary>Identifies less than a complete line or paragraph that is quoted, usually from the Old Testament</summary>
		public const string QuotedText = "Quoted Text";

		/// <summary>Identifies words or phrases that are spoken by Jesus</summary>
		public const string WordsOfChrist = "Words Of Christ";

		/// <summary>Identifies the start of a major section</summary>
		public const string SectionHeadMajor = "Section Head Major";

		/// <summary>Identifies the start of a subsection</summary>
		public const string SectionHeadMinor = "Section Head Minor";

		/// <summary>Identifies a first-level line</summary>
		public const string Line1 = "Line1";

		/// <summary>Identifies a second-level line</summary>
		public const string Line2 = "Line2";

		/// <summary>Identifies a third-level line</summary>
		public const string Line3 = "Line3";

		/// <summary>Identifies the continuation of a paragraph of prose</summary>
		public const string ParagraphContinuation = "Paragraph Continuation";

		/// <summary>Identifies a first-level item in a list in the introduction</summary>
		public const string IntroListItem1 = "Intro List Item1";

		/// <summary>Identifies a second-level item in a list in the introduction</summary>
		public const string IntroListItem2 = "Intro List Item2";

		/// <summary>Identifies a third-level item in a list in the introduction</summary>
		public const string IntroListItem3 = "Intro List Item3";

		/// <summary>Identifies canonical (book-chapter-verse) references to parallel passages</summary>
		public const string ParallelPassageReference = "Parallel Passage Reference";

		/// <summary>Identifies words or phrases that are stressed or emphasized for linguistic or rhetorical effect</summary>
		public const string Emphasis = "Emphasis";

		/// <summary>Identifies an alternate translation for a passage in a note</summary>
		public const string AlternateReading = "Alternate Reading";

		/// <summary>Identifies text that is an allusion to a passage, usually from the Old Testament</summary>
		public const string AlludedText = "Alluded Text";

		/// <summary>Identifies the name of God when the original Hebrew text is YHWH</summary>
		public const string NameOfGod = "Name Of God";

		/// <summary>Identifies the text that is referenced in a note (that is, words from the text that the note is about)</summary>
		public const string ReferencedText = "Referenced Text";

		/// <summary>Identifies a paragraph of prose that is embedded in the main text</summary>
		public const string EmbeddedTextParagraph = "Embedded Text Paragraph";

		/// <summary>Identifies the continuation of a paragraph of prose that is embedded in the main text</summary>
		public const string EmbeddedTextParagraphContinuation = "Embedded Text Paragraph Continuation";

		/// <summary>Identifies a line that glorifies God</summary>
		public const string Doxology = "Doxology";

		/// <summary>Identifies the chapter-verse range of a major section on a separate line</summary>
		public const string SectionRangeParagraph = "Section Range Paragraph";

		/// <summary>Identifies the speaker (usually in drama) in a heading when it is not in the original text</summary>
		public const string SpeechSpeaker = "Speech Speaker";

		/// <summary>Identifies the start of a verse in a note</summary>
		public const string VerseNumberInNote = "Verse Number In Note";

		/// <summary>Identifies the start of a section in a list of sections</summary>
		public const string SectionHeadSeries = "Section Head Series";

		/// <summary>Identifies the start of a row in a table</summary>
		public const string TableRow = "Table Row";

		/// <summary>Identifies a cell that contains a heading in a table</summary>
		public const string TableCellHead = "Table Cell Head";

		/// <summary>Identifies the last cell in a row that contains a heading in a table</summary>
		public const string TableCellHeadLast = "Table Cell Head Last";

		/// <summary>Identifies a cell in a table</summary>
		public const string TableCell = "Table Cell";

		/// <summary>Identifies the last cell in a row in a table</summary>
		public const string TableCellLast = "Table Cell Last";

		/// <summary>Identifies the text of an inscription</summary>
		public const string Inscription = "Inscription";

		/// <summary>Identifies an abbreviation for the ending of an ordinal number</summary>
		public const string OrdinalNumberEnding = "Ordinal Number Ending";

		/// <summary>Identifies the break (blank line) between stanzas of poetry</summary>
		public const string StanzaBreak = "Stanza Break";

		/// <summary>Identifies a title that is in the original Hebrew text</summary>
		public const string HebrewTitle = "Hebrew Title";

		/// <summary>Identifies the speaker (usually the Lord) when it is in the original text</summary>
		public const string Attribution = "Attribution";

		/// <summary>Identifies a heading that contains the chapter number and the word for Chapter or Psalm</summary>
		public const string ChapterHead = "Chapter Head";

		/// <summary>Identifies a paragraph of prose that is quoted, usually from the Old Testament</summary>
		public const string CitationParagraph = "Citation Paragraph";

		/// <summary>Identifies a paragraph that names the author of a communication</summary>
		public const string Closing = "Closing";

		/// <summary>Identifies the part that the congregation speaks in a liturgical work</summary>
		public const string CongregationalResponse = "Congregational Response";

		/// <summary>Identifies canonical (book-chapter-verse) cross-references for a passage</summary>
		public const string CrossReference = "Cross-Reference";

		/// <summary>Identifies a paragraph that names the author of a communication that is embedded in the main text</summary>
		public const string EmbeddedTextClosing = "Embedded Text Closing";

		/// <summary>Identifies a first-level line that is embedded in the main text</summary>
		public const string EmbeddedTextLine1 = "Embedded Text Line1";

		/// <summary>Identifies a second-level line that is embedded in the main text</summary>
		public const string EmbeddedTextLine2 = "Embedded Text Line2";

		/// <summary>Identifies a third-level line that is embedded in the main text</summary>
		public const string EmbeddedTextLine3 = "Embedded Text Line3";

		/// <summary>Identifies the formal beginning of a letter that is embedded in the main text</summary>
		public const string EmbeddedTextOpening = "Embedded Text Opening";

		/// <summary>Identifies repeated text that is embedded in the main text</summary>
		public const string EmbeddedTextRefrain = "Embedded Text Refrain";

		/// <summary>Identifies a paragraph of an inscription</summary>
		public const string InscriptionParagraph = "Inscription Paragraph";

		/// <summary>Identifies a line that suggests a pause to reflect</summary>
		public const string Interlude = "Interlude";

		/// <summary>Identifies a first-level line that is quoted in the introduction</summary>
		public const string IntroCitationLine1 = "Intro Citation Line1";

		/// <summary>Identifies a second-level line that is quoted in the introduction</summary>
		public const string IntroCitationLine2 = "Intro Citation Line2";

		/// <summary>Identifies a paragraph of prose that is quoted in the introduction</summary>
		public const string IntroCitationParagraph = "Intro Citation Paragraph";

		/// <summary>Identifies the canonical (book-chapter-verse) reference for a quoted passage in the introduction</summary>
		public const string IntroCrossReference = "Intro Cross-Reference";

		/// <summary>Identifies repeated text that is embedded in the main text</summary>
		public const string Refrain = "Refrain";

		/// <summary>Identifies a first-level line that is spoken by a particular speaker, usually in drama (for example, in Job and Song of Solomon)</summary>
		public const string SpeechLine1 = "Speech Line1";

		/// <summary>Identifies a second-level line that is spoken by a particular speaker, usually in drama (for example, in Job and Song of Solomon)</summary>
		public const string SpeechLine2 = "Speech Line2";

		/// <summary>Identifies a textual reading that can be regarded as part of the text, but textual scholarship cannot be taken as certain</summary>
		public const string VariantParagraph = "Variant Paragraph";

		/// <summary>Identifies the start of a long variant passage</summary>
		public const string VariantSectionHead = "Variant Section Head";

		/// <summary>Identifies the end of a long variant passage</summary>
		public const string VariantSectionTail = "Variant Section Tail";

		/// <summary>Identifies an abbreviation (usually in a note or introduction)</summary>
		public const string Abbreviation = "Abbreviation";

		/// <summary>Identifies a book title in the text or in the introduction</summary>
		public const string BookTitleInText = "Book Title In Text";

		/// <summary>Identifies the start of a chapter using an alternate numbering system (for example, Hebrew)</summary>
		public const string ChapterNumberAlternate = "Chapter Number Alternate";

		/// <summary>Identifies text in a different language that has no other identifying feature</summary>
		public const string Foreign = "Foreign";

		/// <summary>Identifies a simple definition of a word or phrase in the text</summary>
		public const string Gloss = "Gloss";

		/// <summary>Identifies text that is written by a person different from the scribe writing the letter</summary>
		public const string Hand = "Hand";

		/// <summary>Identifies a word or phrase that is important for translation consistency</summary>
		public const string KeyWord = "Key Word";

		/// <summary>Identifies text that is a type of heading</summary>
		public const string Label = "Label";

		/// <summary>Identifies words, phrases, or letters that are mentioned, not used</summary>
		public const string Mentioned = "Mentioned";

		/// <summary>Identifies words that are defined in the Glossary</summary>
		public const string SeeInGlossary = "See In Glossary";

		/// <summary>Identifies text where the author or narrator of the original text distances himself from the words in question without attributing them to any other voice in particular</summary>
		public const string SoCalled = "So Called";

		/// <summary>Identifies words that are not literally in the original text, but that are supplied to make the original meaning clear</summary>
		public const string Supplied = "Supplied";

		/// <summary>Identifies a textual reading in a note that can be regarded as part of the text, but textual scholarship cannot be taken as certain</summary>
		public const string Variant = "Variant";

		/// <summary>Identifies the start of a verse using an alternate numbering system (for example, Hebrew)</summary>
		public const string VerseNumberAlternate = "Verse Number Alternate";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collection of known internal styles. Thses styles can never be applied
		/// directly by the user. The program controls the contexts where they are appropriate.
		/// These styles are guaranteed to exist in the Scripture stylesheet, whether or not they
		/// are in TeStyles.xml.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static StringCollection InternalStyles
		{
			get
			{
				StringCollection styles = new StringCollection();
				styles.AddRange(new string[] { Normal, FootnoteMarker, Header, NotationTag });
				return styles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collection of known internal mappable styles. Thses styles can never be applied
		/// directly by the user. The program controls the contexts where they are appropriate.
		/// They will appear in the import mapping list, so the import code must treat them speacial.
		/// These styles are guaranteed to exist in the Scripture stylesheet, whether or not they
		/// are in TeStyles.xml.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static StringCollection InternalMappableStyles
		{
			get
			{
				StringCollection styles = new StringCollection();
				styles.AddRange(new string[] { Figure, FootnoteTargetRef, CanonicalRef });
				return styles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collection of special styles used for marking chapter and verse numbers.
		/// </summary>
		/// <remarks>REVIEW: Should we also include ChapterNumberAlternate?</remarks>
		/// ------------------------------------------------------------------------------------
		public static readonly string[] ChapterAndVerse = new [] {ChapterNumber, VerseNumber};
	}
}
