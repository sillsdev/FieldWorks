// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrReference.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a scripture reference
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrReference : BCVRef, IVerseReference
	{
		#region Member variables
		private Paratext.ScrVers m_versification;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference() : base()
		{
			m_versification = Paratext.ScrVers.Unknown;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a reference with an initial BBCCCVVV value
		/// </summary>
		/// <param name="initialRef"></param>
		/// <param name="versification">The versification scheme</param>
		/// ------------------------------------------------------------------------------------
		public ScrReference(int initialRef, Paratext.ScrVers versification)
			: this(ScrReference.GetBookFromBcv(initialRef),
			ScrReference.GetChapterFromBcv(initialRef), ScrReference.GetVerseFromBcv(initialRef),
			versification)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a reference with a book, chapter, and verse
		/// </summary>
		/// <param name="book">The book.</param>
		/// <param name="chapter">The chapter.</param>
		/// <param name="verse">The verse.</param>
		/// <param name="versification">The versification scheme</param>
		/// ------------------------------------------------------------------------------------
		public ScrReference(int book, int chapter, int verse, Paratext.ScrVers versification)
			: this(book, chapter, verse, 0, versification)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Promote a simple BCV reference to a ScrReference
		/// </summary>
		/// <param name="from">The BCVRef to promote to a ScrReference.</param>
		/// <param name="versification">The versification.</param>
		/// ------------------------------------------------------------------------------------
		public ScrReference(BCVRef from, Paratext.ScrVers versification)
			: this(from.Book, from.Chapter, from.Verse, from.Segment, versification)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy a reference
		/// </summary>
		/// <param name="from">The ScrReference to copy.</param>
		/// ------------------------------------------------------------------------------------
		public ScrReference(ScrReference from)
			: this(from.Book, from.Chapter, from.Verse, from.Segment, from.m_versification)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy a reference, converting it to the specified versification if necessary
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference(ScrReference from, Paratext.ScrVers targetVersification)
			: this(from.Book, from.Chapter, from.Verse, from.Segment, from.m_versification)
		{
			VersificationTable.Get(targetVersification).ChangeVersification(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a reference with an initial BBCCCVVV value, a source versification, and a
		/// target versification
		/// </summary>
		/// <param name="initialRef">An integer representation of a scripture reference, where
		/// the first two digits are the canonical book number, the next three hold the chapter
		/// and the last three hold the verse number</param>
		/// <param name="srcVersification">The versification scheme assumed to be that of the
		/// initial BBCCCVVV value</param>
		/// <param name="targetVersification">The versification scheme to convert to. This will
		/// be the versification of the constructed ScrReference</param>
		/// ------------------------------------------------------------------------------------
		public ScrReference(int initialRef, Paratext.ScrVers srcVersification,
			Paratext.ScrVers targetVersification)
			: this(new ScrReference(initialRef, srcVersification), targetVersification)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a reference, given all the pieces
		/// </summary>
		/// <param name="book">The book.</param>
		/// <param name="chapter">The chapter.</param>
		/// <param name="verse">The verse.</param>
		/// <param name="segment">The segment.</param>
		/// <param name="versification">The versification scheme.</param>
		/// ------------------------------------------------------------------------------------
		public ScrReference(int book, int chapter, int verse, int segment,
			Paratext.ScrVers versification) : base(book, chapter, verse, segment)
		{
			m_versification = versification;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ScrReference"/> class.
		/// </summary>
		/// <param name="strReference">The reference as a string.</param>
		/// <param name="versification">The versification scheme.</param>
		/// ------------------------------------------------------------------------------------
		public ScrReference(string strReference, Paratext.ScrVers versification) :
			this(0, 0, 0, 0, versification)
		{
			Parse(strReference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs and returns a new ScrReference representing Genesis 1:1
		/// </summary>
		/// <param name="versification">The versification scheme.</param>
		/// ------------------------------------------------------------------------------------
		public static ScrReference StartOfBible(Paratext.ScrVers versification)
		{
			return new ScrReference(1, 1, 1, versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs and returns a new ScrReference representing Revelation 22:21 (or whatver
		/// the last verse is in Revelation for the current default versification scheme).
		/// </summary>
		/// <param name="versification">The versification scheme.</param>
		/// ------------------------------------------------------------------------------------
		public static ScrReference EndOfBible(Paratext.ScrVers versification)
		{
			VersificationTable versificationTable = VersificationTable.Get(versification);
			int lastChapter = versificationTable.LastChapter(LastBook);
			return new ScrReference(LastBook, lastChapter,
				versificationTable.LastVerse(LastBook, lastChapter), versification);
		}
		#endregion

		#region operators
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implicit conversion of a <see cref="ScrReference"/> to an integer
		/// </summary>
		/// <param name="scrRef">The <see cref="ScrReference"/> to be converted</param>
		/// <returns>An integer representing a Scripture Reference as a BBCCCVVV value</returns>
		/// ------------------------------------------------------------------------------------
		public static implicit operator int(ScrReference scrRef)
		{
			return scrRef.BBCCCVVV;
		}

		#region comparison of ScrReference and ScrReference
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(ScrReference left, ScrReference right)
		{
			return AreEqual(left, right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(ScrReference left, ScrReference right)
		{
			return !AreEqual(left, right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <(ScrReference left, ScrReference right)
		{
			return left.CompareTo(right) < 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >(ScrReference left, ScrReference right)
		{
			return left.CompareTo(right) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than or equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <=(ScrReference left, ScrReference right)
		{
			return left.CompareTo(right) <= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than or equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >=(ScrReference left, ScrReference right)
		{
			return left.CompareTo(right) >= 0;
		}
		#endregion

		#region comparison of ScrReference and int
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator. This implementation implicitly assumes that the integer
		/// representation of the left reference is in the same versification as the right
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(int left, ScrReference right)
		{
			return right.CompareTo(new ScrReference(left, right.Versification)) == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator This implementation implicitly assumes that the integer
		/// representation of the right reference is in the same versification as the left
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(ScrReference left, int right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(int left, ScrReference right)
		{
			return !(right == new ScrReference(left, right.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(ScrReference left, int right)
		{
			return !(left == new ScrReference(right, left.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <(ScrReference left, int right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) < 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <=(ScrReference left, int right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) <= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >(ScrReference left, int right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >=(ScrReference left, int right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) >= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <(int left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) < 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <=(int left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) <= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >(int left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >=(int left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) >= 0;
		}
		#endregion

		#region comparison of ScrReference and BCVRef
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator. This implementation implicitly assumes that the integer
		/// representation of the left reference is in the same versification as the right
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(BCVRef left, ScrReference right)
		{
			return (right == new ScrReference(left, right.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// equals operator This implementation implicitly assumes that the integer
		/// representation of the right reference is in the same versification as the left
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(ScrReference left, BCVRef right)
		{
			return (left == new ScrReference(right, left.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(BCVRef left, ScrReference right)
		{
			return !(right == new ScrReference(left, right.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// not equals operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(ScrReference left, BCVRef right)
		{
			return !(left == new ScrReference(right, left.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <(ScrReference left, BCVRef right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) < 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <=(ScrReference left, BCVRef right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) <= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >(ScrReference left, BCVRef right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >=(ScrReference left, BCVRef right)
		{
			return left.CompareTo(new ScrReference(right, left.Versification)) >= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <(BCVRef left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) < 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// less than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator <=(BCVRef left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) <= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >(BCVRef left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// greater than or equal operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator >=(BCVRef left, ScrReference right)
		{
			return new ScrReference(left, right.Versification).CompareTo(right) >= 0;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>Equals is same as ==</summary>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object o)
		{
			if (o == null)
				return false;
			return base.Equals(o);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// GetHashCode uses the BBCCCVVV as the hash code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion

		#region GetNumberOfChaptersInRange
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the total number of chapters from the specified start ref to the end reference.
		/// </summary>
		/// <remarks>Note: This is based on the total number of chapters for the current
		/// versification.  If there are some chapters missing in a book present those will
		/// not be accounted for.</remarks>
		/// <param name="booksPresent">a bool array indicating the presence of each book.</param>
		/// <param name="refStart">Scripture reference where importing begins.</param>
		/// <param name="refEnd">Scripture reference where importing ends.</param>
		/// <returns>The total number of chapters between the start and end references
		/// (inclusively) in books that are present.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static int GetNumberOfChaptersInRange(List<int> booksPresent, ScrReference refStart,
			ScrReference refEnd)
		{
			Debug.Assert(refStart.Versification == refEnd.Versification);

			// Determine which chapter number to use in the start reference
			int startChapter = (refStart.Chapter == 0) ? 1 : refStart.Chapter;

			// Consider the case where the start and end references are in the same book.
			if (refStart.Book == refEnd.Book)
				return booksPresent.Contains(refStart.Book) ? refEnd.Chapter - startChapter + 1 : 0;

			// Add up the number of chapters for the books from the start to the end.
			int expectedChapters = 0;
			for (int book = refStart.Book; book <= refEnd.Book; book++)
			{
				if (booksPresent.Contains(book))
				{
					ScrReference scRef = new ScrReference(book, 1, 1, refStart.Versification);
					if (book == refStart.Book)
						expectedChapters += refStart.LastChapter - startChapter + 1;
					else if (book == refEnd.Book)
						expectedChapters += refEnd.Chapter;
					else
						expectedChapters += scRef.LastChapter;
				}
			}

			return expectedChapters;
		}
		#endregion

		#region Versification Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the versification files into static tables
		/// </summary>
		/// <param name="vrsFolder">Path to the folder containing the .vrs files</param>
		/// <param name="fSupportDeuterocanon">set to <c>true</c> to support deuterocanonical
		/// books, otherwise <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		public static void InitializeVersification(string vrsFolder, bool fSupportDeuterocanon)
		{
			VersificationTable.Initialize(vrsFolder);
			SupportDeuterocanon = fSupportDeuterocanon;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last valid chapter number for the book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LastChapter
		{
			get
			{
				try
				{
					if (m_book > LastBook)
						return 0;
					return VersificationTable.Get(m_versification).LastChapter(m_book);
				}
				catch
				{
					return 0;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last valid verse number for the chapter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LastVerse
		{
			get
			{
				try
				{
					if (m_book > LastBook || m_chapter > LastChapter)
						return 0;

					return VersificationTable.Get(m_versification).LastVerse(m_book, m_chapter);
				}
				catch { return 0; }
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next verse, following this verse.
		/// </summary>
		/// <exception cref="InvalidOperationException">Invalid call to NextVerse for last verse
		/// in book</exception>
		/// ------------------------------------------------------------------------------------
		public ScrReference NextVerse
		{
			get
			{
				ScrReference next = new ScrReference(this);
				next.Verse += 1;
				if (next.Verse > LastVerse)
				{
					if (Chapter == LastChapter)
						throw new InvalidOperationException("Invalid call to NextVerse for last verse in " + BCVRef.NumberToBookCode(Book));
					next.Verse = 1;
					next.Chapter += 1;
				}
				return next;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the previous verse, before this verse.
		/// </summary>
		/// <exception cref="InvalidOperationException">Invalid call to PrevVerse for first
		/// verse in book</exception>
		/// ------------------------------------------------------------------------------------
		public ScrReference PrevVerse
		{
			get
			{
				ScrReference prev = new ScrReference(this);
				prev.Verse -= 1;
				if (prev.Verse == 0)
				{
					if (Chapter == 1)
						throw new InvalidOperationException("Invalid call to PrevVerse for first verse in " + BCVRef.NumberToBookCode(Book));
					prev.Chapter -= 1;
					prev.Verse = prev.LastVerse;
				}
				return prev;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current versification scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Paratext.ScrVers Versification
		{
			get { return m_versification; }
			set { m_versification = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the reference is valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Valid
		{
			get
			{
				if (m_versification == Paratext.ScrVers.Unknown)
					return false;

				if (!base.Valid)
					return false;
				if (m_chapter > LastChapter)
					return false;
				if (m_verse > LastVerse)
					return false;
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this reference represents a book title (book is set, but chapter and
		/// verse are both 0).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsBookTitle
		{
			get	{ return BookIsValid && Chapter == 0 && Verse == 0;	}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an instance representing an "empty" reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ScrReference Empty
		{
			get { return new ScrReference(); }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the last valid reference for a book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference LastReferenceForBook
		{
			get
			{
				try
				{
					VersificationTable versification = VersificationTable.Get(Versification);
					int chapter = versification.LastChapter(m_book);
					int verse = versification.LastVerse(m_book, chapter);
					return new ScrReference(m_book, chapter, verse, m_versification);
				}
				catch
				{
					return ScrReference.Empty;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate all of the fields to make sure they are in a valid range.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MakeValid()
		{
			if (m_book < 1)
				m_book = 1;
			if (m_book > LastBook)
				m_book = LastBook;

			if (m_chapter < 1)
				m_chapter = 1;
			if (m_chapter > LastChapter)
				m_chapter = LastChapter;

			//REVIEW: Chapter 2 Verse 0 should be invalid.
			if (m_verse < 0)
				m_verse = 0;
			if (m_verse > LastVerse)
				m_verse = LastVerse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the given string representing the reference range.
		/// </summary>
		/// <param name="sRefRng">The string representing the reference range.</param>
		/// <param name="bcvRefStart">The start reference (passed by ref because we use it to
		/// infer any components of the reference that are misisng in sRefRng).</param>
		/// <param name="bcvRefEnd">The end reference.</param>
		/// <param name="versification">The versification.</param>
		/// <returns>
		/// 	<c>true</c> if successfully parsed; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool ParseRefRange(string sRefRng, ref BCVRef bcvRefStart,
			ref BCVRef bcvRefEnd, Paratext.ScrVers versification)
		{
			if (string.IsNullOrEmpty(sRefRng))
				return false;

			if (!sRefRng.Contains("--"))
				return BCVRef.ParseRefRange(sRefRng, ref bcvRefStart, ref bcvRefEnd, false);

			sRefRng = sRefRng.Trim();
			string[] pieces = sRefRng.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
			if (pieces.Length != 2)
				return false;

			string sFirstRef = pieces[0];
			int bbcccvvvStart = bcvRefStart.BBCCCVVV;
			bcvRefStart.Parse(sFirstRef);
			if (!bcvRefStart.Valid)
			{
				bcvRefStart.BBCCCVVV = bbcccvvvStart;
				return false;
			}
			string sEndRef = pieces[1];
			int chapter;
			if (Int32.TryParse(sEndRef, out chapter))
			{
				ScrReference scrRefEnd = new ScrReference(bcvRefStart.Book, chapter, 1, versification);
				scrRefEnd.Verse = scrRefEnd.LastVerse;
				bcvRefEnd.BBCCCVVV = scrRefEnd.BBCCCVVV;
				return true;
			}

			return false;
		}
		#endregion

		#region Book/Chapter/Verse conversions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a chapter number string into an integer and ignore the remaining text
		/// </summary>
		/// <param name="chapterString"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int ChapterToInt(string chapterString)
		{
			string dummy;

			return ChapterToInt(chapterString, out dummy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a chapter number string into an integer
		/// </summary>
		/// <param name="chapterString">string representing the chapter number</param>
		/// <param name="remainingText">returns the remaining non-number portion of the string</param>
		/// <returns>The chapter number</returns>
		/// ------------------------------------------------------------------------------------
		public static int ChapterToInt(string chapterString, out string remainingText)
		{
			remainingText = string.Empty;
			if (chapterString == null)
				throw new ArgumentNullException("chapterString");
			chapterString = chapterString.TrimStart();
			if (chapterString == string.Empty)
				throw new ArgumentException("The chapter string was empty");
			if (!Char.IsDigit(chapterString[0]))
				throw new ArgumentException("The chapter string does not start with a digit");

			int chapter = 0;
			for (int i = 0; i < chapterString.Length; i++)
			{
				char ch = chapterString[i];
				if (Char.IsDigit(ch))
				{
					chapter = chapter * 10 + (int)Char.GetNumericValue(ch);
					if (chapter > Int16.MaxValue)
						chapter = Int16.MaxValue;
				}
				else
				{
					remainingText = chapterString.Substring(i);
					break;
				}
			}
			if (chapter == 0)
				throw new ArgumentException("The chapter number evaluated to 0");
			return chapter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A version of VerseToInt that returns the starting verse value.
		/// </summary>
		/// <param name="sourceString"></param>
		/// <returns>the starting verse value</returns>
		/// ------------------------------------------------------------------------------------
		public static int VerseToIntStart(string sourceString)
		{
			int startVerse, endVerse;
			VerseToInt(sourceString, out startVerse, out endVerse);
			return startVerse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A version of VerseToInt that returns the ending verse value.
		/// </summary>
		/// <param name="sourceString"></param>
		/// <returns>the ending verse value</returns>
		/// ------------------------------------------------------------------------------------
		public static int VerseToIntEnd(string sourceString)
		{
			int startVerse, endVerse;
			VerseToInt(sourceString, out startVerse, out endVerse);
			return endVerse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a helper function to get a starting and ending verse number from a string
		/// which may or may not represent a verse bridge. Ignore any unusual syntax.
		/// </summary>
		/// <param name="sVerseNum">the string representing the verse number(s).</param>
		/// <param name="nVerseStart">the starting verse number in sVerseNum.</param>
		/// <param name="nVerseEnd">the ending verse number in sVerseNum (will be different from
		/// startRef if sVerseNum represents a verse bridge).</param>
		/// ------------------------------------------------------------------------------------
		public static void VerseToInt(string sVerseNum, out int nVerseStart, out int nVerseEnd)
		{
			int nFactor = 1;
			int nVerseT = 0;
			nVerseStart = nVerseEnd = 0;
			// nVerseFirst is the left-most (or right-most if R2L) non-zero number found.
			int nVerseFirst = nVerseT;
			bool fVerseBridge = false;
			if (sVerseNum == null)
				return;
			// REVIEW JohnW (TomB): For robustness, our initial implementation will assume
			// that the first set of contiguous numbers is the starting verse number and
			// the last set of contiguous numbers is the ending verse number. This way, we
			// don't have to know what all the legal possibilities of bridge markers and
			// sub-verse segment indicators are.
			for (int i = sVerseNum.Length - 1; i >= 0; i--)
			{
				int numVal = -1;
				if (Char.IsDigit(sVerseNum[i]))
					numVal = (int)Char.GetNumericValue(sVerseNum[i]);

				if (numVal >= 0 && numVal <= 9)
				{
					if (nFactor > 100) // verse number greater than 999
					{
						// REVIEW JohnW (TomB): Need to decide how we want to display this.
						nVerseT = 999;
					}
					else
					{
						nVerseT += nFactor * numVal;
						nFactor *= 10;
					}
					nVerseFirst = nVerseT;
				}
				else if (nVerseT > 0)
				{
					if (!fVerseBridge)
					{
						fVerseBridge = true;
						nVerseFirst = nVerseEnd = nVerseT;
					}
					nVerseT = 0;
					nFactor = 1;
				}
			}
			nVerseStart = nVerseFirst;
			if (!fVerseBridge)
				nVerseEnd = nVerseFirst;

			// Don't want to use an assertion for this because it could happen due to bad input data.
			// If this causes problems, just pick one ref and use it for both or something.
			// TODO TomB: Later, we need to catch this and flag it as an error.
			//Assert(nVerseStart <= nVerseEnd);
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
		public override int CompareTo(object obj)
		{
			if (obj == null)
			if (!(obj is ScrReference))
				throw new ArgumentException();

			ScrReference right = (ScrReference)obj;

			// If versifications don't match, make a new one, converted to correct versification
			if (m_versification != right.m_versification &&
				m_versification != Paratext.ScrVers.Unknown &&
				right.m_versification != Paratext.ScrVers.Unknown)
			{
				// Neither of the versifications involved are unknown, so do a normal conversion
				right = new ScrReference(right, m_versification);
			}
			else if (m_versification != right.m_versification)
			{
				// one of the versifications involved is unknown, so just treat it as the same
				// versification as the known one.
				if (right.m_versification != Paratext.ScrVers.Unknown)
				{
					ScrReference newThis = new ScrReference(BBCCCVVV, right.m_versification);
					return newThis.CompareTo(right);
				}

				right = new ScrReference(right.BBCCCVVV, m_versification);
			}

			return base.CompareTo(right);
		}

		#endregion
	}
}
