// ---------------------------------------------------------------------------------------------
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BCVRef.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Static class for getting info about the SIL Book codes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class SilBooks
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of the 3-letter codes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string[] Codes_3Letter
		{
			get
			{
				return new string[] {
					"GEN", "EXO", "LEV", "NUM", "DEU", "JOS", "JDG", "RUT", "1SA", "2SA",
					"1KI", "2KI", "1CH", "2CH", "EZR", "NEH", "EST", "JOB", "PSA", "PRO",
					"ECC", "SNG", "ISA", "JER", "LAM", "EZK", "DAN", "HOS", "JOL", "AMO",
					"OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL", "MAT",
					"MRK", "LUK", "JHN", "ACT", "ROM", "1CO", "2CO", "GAL", "EPH", "PHP",
					"COL", "1TH", "2TH", "1TI", "2TI", "TIT", "PHM", "HEB", "JAS", "1PE",
					"2PE", "1JN", "2JN", "3JN", "JUD", "REV"};
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of the 2-letter codes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string[] Codes_2Letter
		{
			get
			{
				return new string[] {"GE","EX","LV","NU","DU","JO","JD",
					"RU","1S","2S","1K","2K","3K","4K","EZ","NE","ES","JB","PA","PR",
					"EC","SO","IS","JR","LA","EK","DA","HO","JL","AM","OB","JH","MI",
					"NA","HA","ZP","HG","ZC","MA",
					"MT","MK","LK","JN","AC","RO","1C","2C","GA","EP","PP","CO",
					"1T","2T","3T","4T","5T","PM","HE","JA","1P","2P","1J","2J","3J",
					"JU","RE"};
			}
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a scripture reference
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BCVRef : IComparable
	{
		#region Member variables
		protected static readonly List<string> s_SIL_BookCodes =
			new List<string>(SilBooks.Codes_3Letter);

		protected static readonly List<string> s_DeuterocanonicalBookCodes =
			new List<string>(new string[]
		{
			"TOB", "JDT", "ESG", "WIS", "SIR", "BAR", "LJE", "S3Y", "SUS", "BEL",
			"1MA", "2MA", "3MA", "4MA", "1ES", "2ES", "MAN", "PS2", "ODA", "PSS",
			"JSA", "JDB", "TBS", "SST", "DNT", "BLT"
		});

		/// <summary>Whether or not deuterocanonical books are supported</summary>
		protected static bool s_fSupportDeuterocanon = false;

		private static string ksTitleRef = " Title";
		private static string ksIntroRef = " Intro";

		protected int m_book;
		protected int m_chapter;
		protected int m_verse;
		protected int m_segment;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a reference with an initial BBCCCVVV value
		/// </summary>
		/// <param name="initialRef"></param>
		/// ------------------------------------------------------------------------------------
		public BCVRef(int initialRef) : this(BCVRef.GetBookFromBcv(initialRef),
			BCVRef.GetChapterFromBcv(initialRef), BCVRef.GetVerseFromBcv(initialRef))
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a reference with a book, chapter, and verse
		/// </summary>
		/// <param name="book">The book.</param>
		/// <param name="chapter">The chapter.</param>
		/// <param name="verse">The verse.</param>
		/// ------------------------------------------------------------------------------------
		public BCVRef(int book, int chapter, int verse)	: this(book, chapter, verse, 0)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy a reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef(BCVRef from) : this(from.Book, from.Chapter, from.Verse, from.Segment)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a reference - utility for the constructors
		/// </summary>
		/// <param name="book">The book.</param>
		/// <param name="chapter">The chapter.</param>
		/// <param name="verse">The verse.</param>
		/// <param name="segment">The segment.</param>
		/// ------------------------------------------------------------------------------------
		public BCVRef(int book, int chapter, int verse, int segment)
		{
			m_book = book;
			m_chapter = chapter;
			m_verse = verse;
			m_segment = segment;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BCVRef"/> class based on a string
		/// that can be parsed.
		/// </summary>
		/// <param name="strReference">The reference as a string.</param>
		/// ------------------------------------------------------------------------------------
		public BCVRef(string strReference) : this(0, 0, 0, 0)
		{
			Parse(strReference);
		}
		#endregion

		#region Operators
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implicit conversion of a <see cref="BCVRef"/> to an integer
		/// </summary>
		/// <param name="scrRef">The <see cref="BCVRef"/> to be converted</param>
		/// <returns>An integer representing a Scripture Reference as a BBCCCVVV value</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator int(BCVRef scrRef)
		{
			return scrRef.BBCCCVVV;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implicit conversion of an integer to a <see cref="BCVRef"/>.
		/// </summary>
		/// <param name="nBCV">The integer representing a Scripture Reference as a BBCCVVV value
		/// </param>
		/// <returns>A <see cref="BCVRef"/></returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator BCVRef(int nBCV)
		{
			return new BCVRef(nBCV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <(BCVRef left, BCVRef right)
		{
			return left.CompareTo(right) < 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <=(BCVRef left, BCVRef right)
		{
			return left.CompareTo(right) <= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >=(BCVRef left, BCVRef right)
		{
			return left.CompareTo(right) >= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >(BCVRef left, BCVRef right)
		{
			return left.CompareTo(right) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(BCVRef left, BCVRef right)
		{
			return AreEqual(left, right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper for equals operator that can be used by derived classes as well.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected static bool AreEqual(BCVRef left, BCVRef right)
		{
			if ((object)left == null && (object)right == null)
				return true;
			else if ((object)left == null || (object)right == null)
				return false;
			return left.CompareTo(right) == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator. This implementation implicitly assumes that the integer
		/// representation of the left reference is in the same versification as the right
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(int left, BCVRef right)
		{
			return AreEqual(new BCVRef(left), right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator This implementation implicitly assumes that the integer
		/// representation of the right reference is in the same versification as the left
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(BCVRef left, int right)
		{
			return AreEqual(left, new BCVRef(right));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(BCVRef left, BCVRef right)
		{
			return !AreEqual(left, right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(int left, BCVRef right)
		{
			return !(right == left);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(BCVRef left, int right)
		{
			return !(left == right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Equals is same as ==</summary>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object o)
		{
			if (o == null)
				return false;
			return CompareTo(o) == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// GetHashCode uses the BBCCCVVV as the hash code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return BBCCCVVV;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether the deuterocanon (apocrypha, etc.) is allowed. This
		/// should be initialized before this class is used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool SupportDeuterocanon
		{
			set { s_fSupportDeuterocanon = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the book portion of the reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Book
		{
			get { return m_book; }
			set { m_book = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the chapter portion of the reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Chapter
		{
			get { return m_chapter; }
			set { m_chapter = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the verse portion of the reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Verse
		{
			get { return m_verse; }
			set { m_verse = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the segment portion of the reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Segment
		{
			get { return m_segment; }
			set { m_segment = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number for the last book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int LastBook
		{
			get
			{
				return s_SIL_BookCodes.Count + ((s_fSupportDeuterocanon) ?
					s_DeuterocanonicalBookCodes.Count : 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse reference as a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AsString
		{
			get { return ToString(BBCCCVVV); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the reference is valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool Valid
		{
			get
			{
				if (!BookIsValid)
					return false;
				return (m_chapter >= 1 && (m_verse > 0 || (m_verse == 0 && m_chapter == 1)));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the book is valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool BookIsValid
		{
			get
			{
				return (m_book >= 1 && m_book <= LastBook);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the reference as a book/chapter/verse integer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BBCCCVVV
		{
			get
			{
				return (m_book % 100 * 1000000 + m_chapter % 1000 * 1000 + m_verse % 1000);
			}
			set
			{
				Book = GetBookFromBcv(value);
				Chapter = GetChapterFromBcv(value);
				Verse = GetVerseFromBcv(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if a reference is empty
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsEmpty
		{
			get
			{
				return m_book == 0 && m_chapter == 0 && m_verse == 0 && m_segment == 0;
			}
		}
		#endregion

		#region ToString methods
		/// ------------------------------------------------------------------------------------
		/// <summary>Format options for use with the ToString methods</summary>
		/// ------------------------------------------------------------------------------------
		public enum RefStringFormat
		{
			/// <summary>general purpose format</summary>
			General,
			/// <summary>format to facilitate exchange (e.g., XML)</summary>
			Exchange,
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse reference as a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return ToString(BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse reference as a string
		/// </summary>
		/// <param name="format">The format.</param>
		/// ------------------------------------------------------------------------------------
		public string ToString(RefStringFormat format)
		{
			return ToString(BBCCCVVV, format);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse reference as a string
		/// </summary>
		/// <param name="bcv">The book-chapter-verse representation of a reference</param>
		/// ------------------------------------------------------------------------------------
		public static string ToString(int bcv)
		{
			return ToString(bcv, RefStringFormat.General);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse reference as a string
		/// </summary>
		/// <param name="bcv">The book-chapter-verse representation of a reference</param>
		/// <param name="format">Indicates whether to format the reference for general purposes
		/// of using a format to facilitate exchange (e.g., XML).</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string ToString(int bcv, RefStringFormat format)
		{
			string book = NumberToBookCode(GetBookFromBcv(bcv));
			int chapter = GetChapterFromBcv(bcv);
			int verse = GetVerseFromBcv(bcv);
			switch (format)
			{
				case RefStringFormat.Exchange:
					if (chapter == 0)
						return book + ksTitleRef;
					if (verse == 0 && chapter == 1)
						return book + ksIntroRef;
					goto default;

				default:
					return string.Format("{0} {1}:{2}", book, chapter, verse);
			}
		}
		#endregion

		#region Other public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handy little utility function for making a Reference string using the SIL book code
		/// for the book name.
		/// </summary>
		/// <param name="startRef">The beginning Scripture reference</param>
		/// <param name="endRef">The ending Scripture reference</param>
		/// <param name="chapterVerseSeparator">Character(s) used to delimit the chapter and verse
		/// number</param>
		/// <param name="verseBridge">Character(s) used to connect two references, indicating a
		/// range</param>
		/// <returns>The reference range as a formatted string.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeReferenceString(BCVRef startRef, BCVRef endRef,
			string chapterVerseSeparator, string verseBridge)
		{
			return MakeReferenceString(NumberToBookCode(startRef.Book), startRef, endRef,
				chapterVerseSeparator, verseBridge, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handy little utility function for making a Reference string using the SIL book code
		/// for the book name.
		/// </summary>
		/// <param name="startRef">The beginning Scripture reference</param>
		/// <param name="endRef">The ending Scripture reference</param>
		/// <param name="chapterVerseSeparator">Character(s) used to delimit the chapter and verse
		/// number</param>
		/// <param name="verseBridge">Character(s) used to connect two references, indicating a
		/// range</param>
		/// <param name="supressChapterForIntroMatter">Does not include the chapter number
		/// when the start and end reference chapter and verse are the same and that
		/// chapter and verse is 1:0</param>
		/// <returns>The reference range as a formatted string.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeReferenceString(BCVRef startRef, BCVRef endRef,
			string chapterVerseSeparator, string verseBridge, bool supressChapterForIntroMatter)
		{
			return MakeReferenceString(NumberToBookCode(startRef.Book), startRef, endRef,
				chapterVerseSeparator, verseBridge, supressChapterForIntroMatter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handy little utility function for making a Reference string. When the end reference
		/// is not specified, it is treated as though it is the same as the start reference.
		/// </summary>
		/// <param name="bookName">The Scripture book name</param>
		/// <param name="startRef">The beginning Scripture reference</param>
		/// <param name="endRef">The ending Scripture reference</param>
		/// <param name="chapterVerseSeparator">Character(s) used to delimit the chapter and verse
		/// number</param>
		/// <param name="verseBridge">Character(s) used to connect two references, indicating a
		/// range</param>
		/// <returns>The reference range as a formatted string.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeReferenceString(string bookName, BCVRef startRef,
			BCVRef endRef, string chapterVerseSeparator, string verseBridge)
		{
			return MakeReferenceString(bookName, startRef, endRef, chapterVerseSeparator,
				verseBridge, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handy little utility function for making a Reference string. When the end reference
		/// is not specified, it is treated as though it is the same as the start reference.
		/// </summary>
		/// <param name="bookName">The Scripture book name</param>
		/// <param name="startRef">The beginning Scripture reference</param>
		/// <param name="endRef">The ending Scripture reference</param>
		/// <param name="chapterVerseSeparator">Character(s) used to delimit the chapter and verse
		/// number</param>
		/// <param name="verseBridge">Character(s) used to connect two references, indicating a
		/// range</param>
		/// <param name="supressChapterForIntroMatter">Does not include the chapter number
		/// when the start and end reference chapter and verse are the same and that
		/// chapter and verse is 1:0</param>
		/// <returns>The reference range as a formatted string.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeReferenceString(string bookName, BCVRef startRef,
			BCVRef endRef, string chapterVerseSeparator, string verseBridge,
			bool supressChapterForIntroMatter)
		{
			return MakeReferenceString(bookName, startRef, endRef, chapterVerseSeparator,
				verseBridge, null, supressChapterForIntroMatter ? String.Empty : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handy little utility function for making a Reference string. When the end reference
		/// is not specified, it is treated as though it is the same as the start reference.
		/// </summary>
		/// <param name="startRef">The beginning Scripture reference</param>
		/// <param name="endRef">The ending Scripture reference</param>
		/// <param name="chapterVerseSeparator">Character(s) used to delimit the chapter and verse
		/// number</param>
		/// <param name="verseBridge">Character(s) used to connect two references, indicating a
		/// range</param>
		/// <param name="literalTitleText">Literal text to use in place of chapter/verse number
		/// when the start and end reference chapter indicate a book title reference (i.e.,
		/// chapter is 0 and verse is 0). Pass an empty string to suppress the chapter number or
		/// <c>null</c> to just output the 0</param>
		/// <param name="literalIntroText">Literal text to use in place of chapter/verse number
		/// when the start and end reference chapter indicate an introduction reference (i.e.,
		/// chapter is 1 and verse is 0). Pass an empty string to suppress the chapter number or
		/// <c>null</c> to treat this as a chapter-only reference</param>
		/// <returns>The reference range as a formatted string.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeReferenceString(BCVRef startRef, BCVRef endRef,
			string chapterVerseSeparator, string verseBridge,
			string literalTitleText, string literalIntroText)
		{
			return MakeReferenceString(NumberToBookCode(startRef.Book), startRef, endRef,
				chapterVerseSeparator, verseBridge, literalTitleText, literalIntroText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handy little utility function for making a Reference string. When the end reference
		/// is not specified, it is treated as though it is the same as the start reference.
		/// </summary>
		/// <param name="bookName">The Scripture book name</param>
		/// <param name="startRef">The beginning Scripture reference</param>
		/// <param name="endRef">The ending Scripture reference</param>
		/// <param name="chapterVerseSeparator">Character(s) used to delimit the chapter and verse
		/// number</param>
		/// <param name="verseBridge">Character(s) used to connect two references, indicating a
		/// range</param>
		/// <param name="literalTitleText">Literal text to use in place of chapter/verse number
		/// when the start and end reference chapter indicate a book title reference (i.e.,
		/// chapter is 0 and verse is 0). Pass an empty string to suppress the chapter number or
		/// <c>null</c> to just output the 0</param>
		/// <param name="literalIntroText">Literal text to use in place of chapter/verse number
		/// when the start and end reference chapter indicate an introduction reference (i.e.,
		/// chapter is 1 and verse is 0). Pass an empty string to suppress the chapter number or
		/// <c>null</c> to treat this as a chapter-only reference</param>
		/// <returns>The reference range as a formatted string.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeReferenceString(string bookName, BCVRef startRef,
			BCVRef endRef, string chapterVerseSeparator, string verseBridge,
			string literalTitleText, string literalIntroText)
		{
			bookName = bookName.Trim();

			if (endRef == null || endRef.IsEmpty)
				endRef = startRef;

			// Build strings to use for the chapter/verse separator and the verse bridge.
			// This method is always used for displaying references in the UI and the separator
			// strings may be surrounded by direction characters. So, we want to get rid of the
			// direction characters before using the strings.
			if (chapterVerseSeparator.Length == 3)
				chapterVerseSeparator = chapterVerseSeparator.Substring(1, 1);
			if (verseBridge.Length == 3)
				verseBridge = verseBridge.Substring(1, 1);

			if (startRef.Chapter != endRef.Chapter)
			{
				string sref = startRef.Chapter.ToString();
				if (startRef.Verse > 0 || endRef.Verse > 0)
					sref += (chapterVerseSeparator + startRef.Verse.ToString());

				sref += (verseBridge + endRef.Chapter.ToString());
				if (startRef.Verse > 0 || endRef.Verse > 0)
					sref += (chapterVerseSeparator + endRef.Verse.ToString());

				return bookName + " " + sref;
			}

			if (startRef.Verse != endRef.Verse)
			{
				return bookName + " " + startRef.Chapter.ToString() + chapterVerseSeparator +
					startRef.Verse.ToString() + verseBridge + endRef.Verse.ToString();
			}

			if (startRef.Verse != 0)
			{
				return bookName + " " + startRef.Chapter.ToString() + chapterVerseSeparator +
					startRef.Verse.ToString();
			}

			string sLiteral = null;
			switch (startRef.Chapter)
			{
				case 0: sLiteral =  literalTitleText; break;
				case 1: sLiteral = literalIntroText; break;
			}

			return bookName + FormatChapterOnlyRef(startRef.Chapter, sLiteral);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generate a string representing a Scripture reference for a single chapter.
		/// </summary>
		/// <param name="chapter">The chapter number.</param>
		/// <param name="literal">The literal string to use in place of the chapter number or
		/// <c>null</c> to use the chapter number.</param>
		/// ------------------------------------------------------------------------------------
		private static string FormatChapterOnlyRef(int chapter, string literal)
		{
			if (literal == String.Empty)
				return String.Empty;

			return " " + (literal != null ? literal : chapter.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine which BBCCCVVV reference is closest to this reference. For this method
		/// a reference is always considered "closer" if it is in the same chapter as this
		/// reference and the other reference is in a different chapter.
		/// </summary>
		/// <param name="left">first reference to check</param>
		/// <param name="right">second reference to check</param>
		/// <returns>0 if the left item is closest (or if they are equally close), 1 if the
		/// right item is closest</returns>
		/// ------------------------------------------------------------------------------------
		public int ClosestTo(int left, int right)
		{
			int bbcccvvv = BBCCCVVV;
			if (Math.Abs(left - bbcccvvv) <= Math.Abs(right - bbcccvvv))
				return 0;
			return 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses Scripture reference string.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <remarks>This method is pretty similar to MultilingScrBooks.ParseRefString, but
		/// it deals only with SIL codes.</remarks>
		/// ------------------------------------------------------------------------------------
		public void Parse(string sTextToBeParsed)
		{
			// Get first token
			string sTrimedText = sTextToBeParsed.TrimStart(null); // Trim all std white space

			if (sTrimedText.Length < 3)
			{
				// can't be a valid reference - set book to 0 and return
				Book = 0;
				return;
			}

			string sToken = sTrimedText.Substring(0, 3); // 3 letter SIL code
			string sAfterToken = sTrimedText.Substring(3);

			// Determine book number
			m_book = BookToNumber(sToken);
			if (!BookIsValid)
				return;

			if (sAfterToken == ksTitleRef)
			{
				Chapter = 0;
				Verse = 0;
				return;
			}
			if (sAfterToken == ksIntroRef)
			{
				Chapter = 1;
				Verse = 0;
				return;
			}

			// Break out the chapter and verse numbers
			bool inChapter = true;

			// If there is no chapter:verse portion then just set 1:1
			if (sAfterToken == string.Empty)
				m_chapter = m_verse = 1;
			else
			{
				m_chapter = 0;
				m_verse = -1;
			}

			foreach (char ch in sAfterToken)
			{
				if (Char.IsDigit(ch))
				{
					if (inChapter)
					{
						m_chapter *= 10;
						m_chapter += (int)Char.GetNumericValue(ch);
					}
					else
					{
						if (m_verse < 0)
							m_verse = (int)Char.GetNumericValue(ch);
						else
						{
							m_verse *= 10;
							m_verse += (int)Char.GetNumericValue(ch);
						}
					}
				}
				else if (!char.IsWhiteSpace(ch))
				{
					if (inChapter)
						inChapter = false;
					else
					{
						// got an invalid character
						m_book = 0;
						return;
					}
				}
			}

			// If there was no verse specified, then make it 1
			if (m_verse == -1)
				m_verse = 1;
		}
		#endregion

		#region Book/Chapter/Verse conversions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Map an SIL book code to a book number (1=GEN, 66=REV)
		/// </summary>
		/// <param name="book"></param>
		/// <returns>The book code if the string passed is a valid SIL book code; -1 if it is a
		/// Deuterocanonical book code; 0 if it is a totally bogus string</returns>
		/// ------------------------------------------------------------------------------------
		public static int BookToNumber(string book)
		{
			string key = (book.Length > 3) ? book.Substring(0, 3) : book;
			key = key.ToUpper();

			int i = s_SIL_BookCodes.IndexOf(key);
			if (i == -1)
			{
				i = s_DeuterocanonicalBookCodes.IndexOf(key);
				if (i < 0)
					return 0; // neither in SIL nor Deuterocan. book codes
				else if (s_fSupportDeuterocanon)
					i += s_SIL_BookCodes.Count;
				else
					return -1;
			}
			return i + 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Map a book number to an SIL book code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string NumberToBookCode(int bookNumber)
		{
			if (bookNumber < 1 || bookNumber > LastBook)
				return string.Empty;
			if (bookNumber > s_SIL_BookCodes.Count)
			{
				Debug.Assert(s_fSupportDeuterocanon);
				return s_DeuterocanonicalBookCodes[bookNumber - 1 - s_SIL_BookCodes.Count];
			}
			return s_SIL_BookCodes[bookNumber - 1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse part of the specified bcv
		/// </summary>
		/// <param name="bcv">The bcv to parse</param>
		/// <returns>The verse part of the specified bcv</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetVerseFromBcv(int bcv)
		{
			return (bcv % 1000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the chapter part of the specified bcv
		/// </summary>
		/// <param name="bcv">The bcv to parse</param>
		/// <returns>The chapter part of the specified bcv</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetChapterFromBcv(int bcv)
		{
			return (bcv / 1000) % 1000;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the book part of the specified bcv
		/// </summary>
		/// <param name="bcv">The bcv to parse</param>
		/// <returns>The book part of the specified bcv</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetBookFromBcv(int bcv)
		{
			return (bcv / 1000000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overloaded version of VerseToScrRef that ignores some parameters
		/// </summary>
		/// <param name="sourceString">string containing the verse number</param>
		/// <param name="firstRef">first reference that will have the verse portion adjusted</param>
		/// <param name="lastRef">last reference that will have the verse portion adjusted</param>
		/// <returns>true if converted successfully</returns>
		/// ------------------------------------------------------------------------------------
		public static bool VerseToScrRef(string sourceString, ref BCVRef firstRef,
			ref BCVRef lastRef)
		{
			string dummy1, dummy2;

			return VerseToScrRef(sourceString, out dummy1, out dummy2, ref firstRef, ref lastRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extract the verse numbers from a verse string. Determine verse number begin and end
		/// values as well as verse segment values. Ignore any unusual syntax.
		/// Limitations: This class does not have access to the FDO Scripture bridge character
		/// or to a character property engine with PUA character information.
		/// </summary>
		/// <param name="sourceString">string from which to attempt extracting verse numbers
		/// </param>
		/// <param name="literalVerse">returns the text that was converted to a verse number
		/// </param>
		/// <param name="remainingText">returns the remaining text after the verse number
		/// </param>
		/// <param name="firstRef">returns the first reference</param>
		/// <param name="lastRef">returns the last reference in the case of a verse bridge
		/// </param>
		/// <returns>true if converted successfully</returns>
		/// ------------------------------------------------------------------------------------
		public static bool VerseToScrRef(string sourceString, out string literalVerse,
			out string remainingText, ref BCVRef firstRef, ref BCVRef lastRef)
		{
			int firstVerse = 0;
			int lastVerse = 0;
			bool inFirst = true;
			int stringSplitPos = 0;
			int iDashCount = 0;
			sourceString = sourceString.TrimStart();

			// break the string out into the verse number portion and the following text portion.
			char prevChar = '\0';
			int i = 0;
			while (i < sourceString.Length)
			{
				char ch = sourceString[i];
				if (Char.IsDigit(ch))
				{
					stringSplitPos = i + 1;
				}
				else if (Char.IsLetter(ch))
				{
					if (prevChar == '.')
					{
						stringSplitPos = i;
						break;
					}
					if (Char.IsLetter(prevChar) || Char.IsPunctuation(prevChar))
					{
						stringSplitPos = i - 1;
						break;
					}
				}
				else if (ch != '.' && Char.IsPunctuation(ch))
				{
					if (iDashCount > 0)
					{
						if (prevChar == '-')
							stringSplitPos = i - 1;
						else
							stringSplitPos = i;
						break;
					}
					iDashCount++;
				}
				else if (ch == '\u200f' || ch == '\u200e')
				{
					// RTL and LTR marks
					// don't let these characters be saved as prevChar
					i++;
					continue;
				}
				else if (ch == '.')
				{
				}
				else
				{
					// all other characters (including space) terminate the verse number
					stringSplitPos = i;
					break;
				}
				prevChar = ch;
				i++;
			}

			literalVerse = sourceString.Substring(0, stringSplitPos);
			remainingText = sourceString.Substring(stringSplitPos);
			if (remainingText == null)
				remainingText = string.Empty;

			// parse the verse string to get the verse numbers out.
			prevChar = '\0';
			int firstSegment = 0;
			int lastSegment = 0;
			foreach (char ch in literalVerse)
			{
				if (Char.IsDigit(ch))
				{
					// Add the digit to either the first or last verse in the bridge
					if (inFirst)
					{
						firstVerse = firstVerse * 10 + (int)Char.GetNumericValue(ch);
						if (firstVerse > Int16.MaxValue)
							return false; // whoops, we got too big!
					}
					else
					{
						lastVerse = lastVerse * 10 + (int)Char.GetNumericValue(ch);
						if (lastVerse > Int16.MaxValue)
							return false; // whoops, we got too big!
					}
				}
				else if (Char.IsLetter(ch))
				{
					// letters are used for segments
					if (Char.IsDigit(prevChar))
					{
						if (inFirst)
						{
							// for the first verse in the segment, look at the old
							// reference and increment the segment if the verse
							// number has not changed. Otherwise, start the segment
							// at 1.
							if (firstVerse == lastRef.Verse)
								firstSegment = lastRef.Segment + 1;
							else
								firstSegment = 1;
						}
						else
						{
							// If the verses are the same in the bridge, then the second segment
							// will be one greater than the first segment, otherwise it will be 1.
							if (firstVerse == lastVerse)
								lastSegment = firstSegment + 1;
							else
								lastSegment = 1;
						}
					}
					else
					{
						// if there is not a digit preceding a segment letter, this is an
						// error so quit.
						return false;
					}
				}
				else
				{
					// any other character will switch to the second verse in the bridge
					inFirst = false;
				}
				prevChar = ch;
			}

			if (lastVerse == 0)
			{
				lastVerse = firstVerse;
				lastSegment = firstSegment;
			}
			firstRef.Verse = firstVerse;
			firstRef.Segment = firstSegment;
			lastRef.Verse = lastVerse;
			lastRef.Segment = lastSegment;
			return true;
		}
		#endregion

		#region ParseRefRange
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the given string representing the reference range.
		/// </summary>
		/// <param name="sRefRng">The string representing the reference range.</param>
		/// <param name="bcvRefStart">The BCV ref start.</param>
		/// <param name="bcvRefEnd">The BCV ref end.</param>
		/// <returns><c>true</c> if successfully parsed; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool ParseRefRange(string sRefRng, ref BCVRef bcvRefStart, ref BCVRef bcvRefEnd)
		{
			return ParseRefRange(sRefRng, ref bcvRefStart, ref bcvRefEnd, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the given string representing the reference range.
		/// </summary>
		/// <param name="sRefRng">The string representing the reference range.</param>
		/// <param name="bcvRefStart">The BCV ref start.</param>
		/// <param name="bcvRefEnd">The BCV ref end.</param>
		/// <param name="fAllowDifferentBooks">if set to <c>true</c> range is allowed to span books.</param>
		/// <returns>
		/// 	<c>true</c> if successfully parsed; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool ParseRefRange(string sRefRng, ref BCVRef bcvRefStart,
			ref BCVRef bcvRefEnd, bool fAllowDifferentBooks)
		{
			if (string.IsNullOrEmpty(sRefRng))
				return false;
			sRefRng = sRefRng.Trim();
			string[] pieces = sRefRng.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
			if (pieces.Length > 2 || pieces.Length == 0)
				return false;

			string sFirstRef = pieces[0];
			int bbcccvvvStart = bcvRefStart.BBCCCVVV;
			int intVal;

			if (Int32.TryParse(sFirstRef, out intVal))
			{
				if (intVal > 150)
				{
					bcvRefStart.BBCCCVVV = intVal;
					if (!bcvRefStart.Valid)
					{
						bcvRefStart.BBCCCVVV = bbcccvvvStart;
						return false;
					}
				}
				else
				{
					if (bcvRefStart.Book != bcvRefEnd.Book || bcvRefStart.Chapter != bcvRefEnd.Chapter)
						return false;
					bcvRefStart.Verse = intVal;
				}
			}
			else
			{
				// have to check *second* character because first character in a book code
				//can be a number; e.g. 2JN
				if (sFirstRef.Length < 3 || !Char.IsLetter(sFirstRef[1]))
				{
					if (bcvRefStart.Book != bcvRefEnd.Book)
						return false;
					sFirstRef = BCVRef.NumberToBookCode(bcvRefStart.Book) + " " + sFirstRef;
				}

				bcvRefStart.Parse(sFirstRef);
				if (!bcvRefStart.Valid)
				{
					bcvRefStart.BBCCCVVV = bbcccvvvStart;
					return false;
				}
			}
			if (pieces.Length == 1)
			{
				bcvRefEnd.BBCCCVVV = bcvRefStart.BBCCCVVV;
				return true;
			}

			string sEndRef = pieces[1];
			int bbcccvvvEnd = bcvRefEnd.BBCCCVVV;

			if (Int32.TryParse(sEndRef, out intVal))
			{
				if (intVal > 150)
				{
					bcvRefEnd.BBCCCVVV = intVal;
					if (!bcvRefEnd.Valid)
					{
						bcvRefStart.BBCCCVVV = bbcccvvvStart;
						bcvRefEnd.BBCCCVVV = bbcccvvvEnd;
						return false;
					}
				}
				else
				{
					bcvRefEnd.BBCCCVVV = bcvRefStart.BBCCCVVV;
					bcvRefEnd.Verse = intVal;
				}
			}
			else
			{
				if (sEndRef.Length < 3 || !Char.IsLetter(sEndRef[1]))
					sEndRef = BCVRef.NumberToBookCode(bcvRefStart.Book) + " " + sEndRef;

				bcvRefEnd.Parse(sEndRef);
				if (!bcvRefEnd.Valid || bcvRefStart > bcvRefEnd ||
					(bcvRefStart.Book != bcvRefEnd.Book && !fAllowDifferentBooks))
				{
					bcvRefStart.BBCCCVVV = bbcccvvvStart;
					bcvRefEnd.BBCCCVVV = bbcccvvvEnd;
					return false;
				}
			}
			return true;
		}
		#endregion

		#region IComparable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being
		/// compared. The return value has these meanings:
		///
		/// Value				Meaning
		/// Less than zero		This instance is less than <paramref name="obj"/>.
		/// Zero				This instance is equal to <paramref name="obj"/>.
		/// Greater than zero	This instance is greater than <paramref name="obj"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// 	<paramref name="obj"/> is not the same type as this instance. </exception>
		/// ------------------------------------------------------------------------------------
		public virtual int CompareTo(object obj)
		{
			if (!(obj is BCVRef))
				throw new ArgumentException();

			BCVRef right = (BCVRef)obj;

			if (BBCCCVVV == right.BBCCCVVV)
				return Segment.CompareTo(right.Segment);
			return BBCCCVVV.CompareTo(right.BBCCCVVV);
		}
		#endregion
	}

	#region RefRange class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a reference range
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RefRange
	{
		/// <summary>Location info that hasn't been set</summary>
		public static readonly RefRange EMPTY = new RefRange(0, 0);

		/// <summary>Starting Scripture reference</summary>
		private BCVRef m_startRef;
		/// <summary>Ending Scripture reference</summary>
		private BCVRef m_endRef;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startRef">The start reference</param>
		/// <param name="endRef">The end reference</param>
		/// ------------------------------------------------------------------------------------
		public RefRange(BCVRef startRef, BCVRef endRef)
		{
			m_startRef = new BCVRef(startRef);
			m_endRef = new BCVRef(endRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef StartRef
		{
			get { return new BCVRef(m_startRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the end reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef EndRef
		{
			get { return new BCVRef(m_endRef); }
		}
	}
	#endregion
}