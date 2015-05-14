// --------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BCVRefTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using NUnit.Framework;
using System.Reflection;

namespace SILUBS.SharedScrUtils
{
	#region BCVRefTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the BCVRef class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BCVRefTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the VersificationTable class for tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void InitializeVersificationTable()
		{
			string vrsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			File.WriteAllBytes(Path.Combine(vrsPath,
				VersificationTable.GetFileNameForVersification(ScrVers.English)), Properties.Resource.eng);
			File.WriteAllBytes(Path.Combine(vrsPath,
				VersificationTable.GetFileNameForVersification(ScrVers.Septuagint)), Properties.Resource.lxx);
			File.WriteAllBytes(Path.Combine(vrsPath,
				VersificationTable.GetFileNameForVersification(ScrVers.Original)), Properties.Resource.org);
			VersificationTable.Initialize(vrsPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up to initialize VersificationTable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			InitializeVersificationTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			BCVRef.SupportDeuterocanon = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the constructors of BCVRef
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidBCVRefs()
		{
			BCVRef bcvRef = new BCVRef(1, 2, 3, 0);
			Assert.IsTrue(bcvRef.Valid);
			Assert.AreEqual(1002003, (int)bcvRef);
			Assert.AreEqual(1, bcvRef.Book);
			Assert.AreEqual(2, bcvRef.Chapter);
			Assert.AreEqual(3, bcvRef.Verse);

			bcvRef = new BCVRef(4005006);
			Assert.IsTrue(bcvRef.Valid);
			Assert.AreEqual(4005006, (int)bcvRef);
			Assert.AreEqual(4, bcvRef.Book);
			Assert.AreEqual(5, bcvRef.Chapter);
			Assert.AreEqual(6, bcvRef.Verse);

			bcvRef = new BCVRef();
			Assert.IsFalse(bcvRef.Valid);
			Assert.AreEqual(0, (int)bcvRef);
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(0, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Verse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test building a BCVRef by setting individual properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildBcvRefByProps()
		{
			// Build a bcvRef by individual properties
			BCVRef bcvRef = new BCVRef();

			bcvRef.Book = 13;
			Assert.IsFalse(bcvRef.Valid); // 0 not allowed for chapter
			Assert.AreEqual(13000000, bcvRef.BBCCCVVV);
			Assert.AreEqual(13, bcvRef.Book);
			Assert.AreEqual(0, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Verse);

			bcvRef.Chapter = 1;
			// a zero verse is considered valid for introduction, etc, but only for chapter 1
			Assert.IsTrue(bcvRef.Valid);
			Assert.AreEqual(13001000, (int)bcvRef);
			Assert.AreEqual(13, bcvRef.Book);
			Assert.AreEqual(1, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Verse);

			bcvRef.Chapter = 14;
			bcvRef.Verse = 15;
			Assert.IsTrue(bcvRef.Valid);
			Assert.AreEqual(13014015, (int)bcvRef);
			Assert.AreEqual(13, bcvRef.Book);
			Assert.AreEqual(14, bcvRef.Chapter);
			Assert.AreEqual(15, bcvRef.Verse);

			bcvRef = new BCVRef();
			bcvRef.Chapter = 16;
			Assert.IsFalse(bcvRef.Valid, "Invalid because 0 is not valid for the book number");
			Assert.AreEqual(00016000, (int)bcvRef);
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(16, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Verse);

			bcvRef = new BCVRef();
			bcvRef.Verse = 17;
			Assert.IsFalse(bcvRef.Valid, "Invalid because 0 is not valid for the book and chapter numbers");
			Assert.AreEqual(00000017, (int)bcvRef);
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(0, bcvRef.Chapter);
			Assert.AreEqual(17, bcvRef.Verse);

			// same tests as above except that we test individual properties first
			// the BCVRef object operates differently in this circumstance
			bcvRef = new BCVRef();
			bcvRef.Book = 21;
			Assert.AreEqual(0, bcvRef.Verse);
			Assert.AreEqual(0, bcvRef.Chapter);
			Assert.AreEqual(21, bcvRef.Book);
			Assert.AreEqual(21000000, (int)bcvRef);
			Assert.IsFalse(bcvRef.Valid, "Invalid because 0 is not valid for the chapter number");

			bcvRef = new BCVRef();
			bcvRef.Chapter = 22;
			Assert.AreEqual(0, bcvRef.Verse);
			Assert.AreEqual(22, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(00022000, (int)bcvRef);
			Assert.IsFalse(bcvRef.Valid, "Invalid because 0 is not valid for the book number");

			bcvRef = new BCVRef();
			bcvRef.Verse = 23;
			Assert.AreEqual(23, bcvRef.Verse);
			Assert.AreEqual(0, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(00000023, (int)bcvRef);
			Assert.IsFalse(bcvRef.Valid, "Invalid because 0 is not valid for the book and chapter numbers");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test updating the Book, Chapter and Verse of a BCVRef that is created and converted
		/// using the implicit operators.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateBcvRef()
		{
			// Test updating book
			BCVRef bcvRef = new BCVRef(1001001);
			bcvRef.Book = 2;
			int encodedBCV = bcvRef;
			Assert.AreEqual(2001001, encodedBCV);

			// Test updating chapter
			bcvRef = new BCVRef(1001001);
			bcvRef.Chapter = 2;
			encodedBCV = bcvRef;
			Assert.AreEqual(1002001, encodedBCV);

			// Test updating verse
			bcvRef = new BCVRef(1001001);
			bcvRef.Verse = 2;
			encodedBCV = bcvRef;
			Assert.AreEqual(1001002, encodedBCV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_Valid()
		{
			BCVRef reference = new BCVRef("Gen 1:1");
			Assert.IsTrue(reference.Valid);
			Assert.AreEqual(01001001, reference.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_ValidNoSpace()
		{
			BCVRef reference = new BCVRef("GEN1:1");
			Assert.IsTrue(reference.Valid);
			Assert.AreEqual(01001001, reference.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_ValidExtraSpace()
		{
			BCVRef reference = new BCVRef("GEN 1 : 1");
			Assert.IsTrue(reference.Valid);
			Assert.AreEqual(01001001, reference.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_OnlyChapter()
		{
			BCVRef reference = new BCVRef("EXO 1");
			Assert.IsTrue(reference.Valid);
			Assert.AreEqual(02001001, reference.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_OnlyBook()
		{
			BCVRef reference = new BCVRef("LEV");
			Assert.IsTrue(reference.Valid);
			Assert.AreEqual(03001001, reference.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_Bridge()
		{
			BCVRef reference = new BCVRef("NUM 5:1-5");
			Assert.IsFalse(reference.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_Intro()
		{
			BCVRef reference = new BCVRef("JOS 1:0");
			Assert.IsTrue(reference.Valid);
			Assert.AreEqual(06001000, reference.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_IntroInvalid()
		{
			BCVRef reference = new BCVRef("JOS 2:0");
			Assert.IsFalse(reference.Valid, "Verse cannot be 0 except in chapter 1.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_InvalidBook()
		{
			BCVRef reference = new BCVRef("BLA 1:1");
			Assert.IsFalse(reference.Valid);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the &lt; operator
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Less()
		{
			Assert.IsTrue(new BCVRef(1, 1, 1) < new BCVRef(2, 1, 1));
			Assert.IsFalse(new BCVRef(10, 1, 1) < new BCVRef(1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) < new BCVRef(1, 1, 1, 2));
			Assert.IsTrue(new BCVRef(1, 1, 1) < new BCVRef(1, 1, 1, 1));
			Assert.IsFalse(new BCVRef(1, 1, 1, 1) < new BCVRef(1, 1, 1));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the &lt;= operator
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void LessOrEqual()
		{
			Assert.IsTrue(new BCVRef(1, 1, 1) <= new BCVRef(2, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1) <= new BCVRef(1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) <= new BCVRef(1, 1, 1, 2));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) <= new BCVRef(1, 1, 1, 1));
			Assert.IsFalse(new BCVRef(10, 1, 1) <= new BCVRef(1, 1, 1));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the &gt; operator
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Greater()
		{
			Assert.IsTrue(new BCVRef(2, 1, 1) > new BCVRef(1, 1, 1));
			Assert.IsFalse(new BCVRef(1, 1, 1) > new BCVRef(10, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 2) > new BCVRef(1, 1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) > new BCVRef(1, 1, 1));
			Assert.IsFalse(new BCVRef(1, 1, 1) > new BCVRef(1, 1, 1, 1));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the &gt;= operator
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GreaterOrEqual()
		{
			Assert.IsTrue(new BCVRef(2, 1, 1) >= new BCVRef(1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1) >= new BCVRef(1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 2) >= new BCVRef(1, 1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) >= new BCVRef(1, 1, 1, 1));
			Assert.IsFalse(new BCVRef(1, 1, 1) >= new BCVRef(10, 1, 1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the == operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equal()
		{
			Assert.IsTrue(new BCVRef(1, 1, 1) == new BCVRef(1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) == new BCVRef(1, 1, 1, 1));
			Assert.IsFalse(new BCVRef(1, 1, 1, 1) == new BCVRef(1, 1, 1, 2));
			Assert.IsFalse(new BCVRef(1, 1, 1, 1) == new BCVRef(1, 1, 1));
			Assert.IsTrue(01001001 == new BCVRef(1, 1, 1));
			Assert.IsFalse(01001001 == new BCVRef(1, 1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1) == 01001001);
			Assert.IsFalse(new BCVRef(1, 1, 1, 1) == 01001001);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the != operator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NotEqual()
		{
			Assert.IsFalse(new BCVRef(1, 1, 1) != new BCVRef(1, 1, 1));
			Assert.IsFalse(new BCVRef(1, 1, 1, 1) != new BCVRef(1, 1, 1, 1));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) != new BCVRef(1, 1, 1, 2));
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) != new BCVRef(1, 1, 1));
			Assert.IsFalse(01001001 != new BCVRef(1, 1, 1));
			Assert.IsTrue(01001001 != new BCVRef(1, 1, 1, 1));
			Assert.IsFalse(new BCVRef(1, 1, 1) != 01001001);
			Assert.IsTrue(new BCVRef(1, 1, 1, 1) != 01001001);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that passing in invalid verse numbers doesn't cause an overflow in the chapter
		/// number in the BCV ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Invalid()
		{
			BCVRef scrRef = new BCVRef(6542, 1023, 5051);
			Assert.IsFalse(scrRef.Valid);
			Assert.AreEqual(42023051, scrRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the MakeReferenceString() method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MakeReferenceString()
		{
			// The first test passes chap./vrs. separator and bridge characters that we know
			// would never be hardcoded in order to make sure those characters are not hardcoded.
			Assert.AreEqual("MAT 5#7", BCVRef.MakeReferenceString(new BCVRef(40, 5, 7),
				new BCVRef(40, 5, 7), "#", "!"));

			Assert.AreEqual("MAT 5:7-9", BCVRef.MakeReferenceString(
				new BCVRef(40, 5, 7), new BCVRef(40, 5, 9), ":", "-"));

			Assert.AreEqual("GEN 1:1", BCVRef.MakeReferenceString(
				new BCVRef(1, 1, 1), new BCVRef(), ":", "-"));

			Assert.AreEqual("GEN 5:5", BCVRef.MakeReferenceString(
				new BCVRef(1, 5, 5), new BCVRef(1, 5, 5), ":", "-"));

			Assert.AreEqual("REV 22:58", BCVRef.MakeReferenceString(
				new BCVRef(66, 22, 58), new BCVRef(66, 22, 58), ":", "-"));

			Assert.AreEqual("MRK 12:34-14:56", BCVRef.MakeReferenceString(
				new BCVRef(41, 12, 34), new BCVRef(41, 14, 56), ":", "-"));

			// Chapter-only references
			Assert.AreEqual("MRK 10", BCVRef.MakeReferenceString(
				new BCVRef(41, 10, 0), new BCVRef(), ":", "-", true));

			Assert.AreEqual("MRK 10", BCVRef.MakeReferenceString(
				new BCVRef(41, 10, 0), null, ":", "-", true));

			Assert.AreEqual("MRK 1-2", BCVRef.MakeReferenceString(
				new BCVRef(41, 1, 0), new BCVRef(41, 2, 0), ":", "-"));

			Assert.AreEqual("MRK 5-10", BCVRef.MakeReferenceString(
				new BCVRef(41, 5, 0), new BCVRef(41, 10, 0), ":", "-"));

			// Bridges where once verse is 0 (ill-formed)
			Assert.AreEqual("MRK 1:2-2:0", BCVRef.MakeReferenceString(
				new BCVRef(41, 1, 2), new BCVRef(41, 2, 0), ":", "-"));

			Assert.AreEqual("MRK 10:0-12:34", BCVRef.MakeReferenceString(
				new BCVRef(41, 10, 0), new BCVRef(41, 12, 34), ":", "-"));

			// Title references
			Assert.AreEqual("MRK Title", BCVRef.MakeReferenceString(
				new BCVRef(41, 0, 0), null, ":", "-", "Title", "Intro"));

			Assert.AreEqual("MRK 0", BCVRef.MakeReferenceString(
				new BCVRef(41, 0, 0), null, ":", "-", null, null));

			// Intro references
			Assert.AreEqual("MRK", BCVRef.MakeReferenceString(
				new BCVRef(41, 1, 0), new BCVRef(41, 1, 0), ":", "-", true));

			Assert.AreEqual("MRK", BCVRef.MakeReferenceString(
				new BCVRef(41, 1, 0), new BCVRef(41, 1, 0), ":", "-", null, string.Empty));

			Assert.AreEqual("MRK Intro", BCVRef.MakeReferenceString(
				new BCVRef(41, 1, 0), new BCVRef(41, 1, 0), ":", "-", "Title", "Intro"));

			Assert.AreEqual("MRK 1", BCVRef.MakeReferenceString(
				new BCVRef(41, 1, 0), null, ":", "-", false));

			Assert.AreEqual("MRK 1", BCVRef.MakeReferenceString(
				new BCVRef(41, 1, 0), new BCVRef(41, 1, 0), ":", "-", null, null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the LastBook property if we don't support deuterocanonical books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LastBook_NoDeuterocanon()
		{
			Assert.AreEqual(66, BCVRef.LastBook);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the LastBook property if we do support deuterocanonical books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LastBook_Deuterocanon()
		{
			BCVRef.SupportDeuterocanon = true;
			Assert.AreEqual(92, BCVRef.LastBook);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BookToNumber method if we don't support deuterocanonical books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BookToNumber_NoDeuterocanon()
		{
			Assert.AreEqual(1, BCVRef.BookToNumber("GEN"));
			Assert.AreEqual(66, BCVRef.BookToNumber("REV"));
			Assert.AreEqual(-1, BCVRef.BookToNumber("TOB"));
			Assert.AreEqual(-1, BCVRef.BookToNumber("BLT"));
			Assert.AreEqual(0, BCVRef.BookToNumber("XYZ"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BookToNumber method if we do support deuterocanonical books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BookToNumber_Deuterocanon()
		{
			BCVRef.SupportDeuterocanon = true;
			Assert.AreEqual(1, BCVRef.BookToNumber("GEN"));
			Assert.AreEqual(66, BCVRef.BookToNumber("REV"));
			Assert.AreEqual(67, BCVRef.BookToNumber("TOB"));
			Assert.AreEqual(92, BCVRef.BookToNumber("BLT"));
			Assert.AreEqual(0, BCVRef.BookToNumber("XYZ"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NumberToBookCode method if we don't support deuterocanonical books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NumberToBookCode_NoDeuterocanon()
		{
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(-1));
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(0));
			Assert.AreEqual("GEN", BCVRef.NumberToBookCode(1));
			Assert.AreEqual("REV", BCVRef.NumberToBookCode(66));
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(67));
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(92));
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(93));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NumberToBookCode method if we do support deuterocanonical books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NumberToBookCode_Deuterocanon()
		{
			BCVRef.SupportDeuterocanon = true;
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(-1));
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(0));
			Assert.AreEqual("GEN", BCVRef.NumberToBookCode(1));
			Assert.AreEqual("REV", BCVRef.NumberToBookCode(66));
			Assert.AreEqual("TOB", BCVRef.NumberToBookCode(67));
			Assert.AreEqual("BLT", BCVRef.NumberToBookCode(92));
			Assert.AreEqual(string.Empty, BCVRef.NumberToBookCode(93));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString method for a book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ToString_Title()
		{
			BCVRef genesisTitleRef = new BCVRef(1000000);
			Assert.AreEqual("GEN 0:0", genesisTitleRef.ToString(BCVRef.RefStringFormat.General));
			Assert.AreEqual("GEN Title", genesisTitleRef.ToString(BCVRef.RefStringFormat.Exchange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString method for intro material.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ToString_Intro()
		{
			BCVRef genesisTitleRef = new BCVRef(1001000);
			Assert.AreEqual("GEN 1:0", genesisTitleRef.ToString(BCVRef.RefStringFormat.General));
			Assert.AreEqual("GEN Intro", genesisTitleRef.ToString(BCVRef.RefStringFormat.Exchange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString method for a bogus reference (intro material on chapter 2?).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ToString_BogusIntro()
		{
			BCVRef genesisTitleRef = new BCVRef(1002000);
			Assert.AreEqual("GEN 2:0", genesisTitleRef.ToString(BCVRef.RefStringFormat.General));
			Assert.AreEqual("GEN 2:0", genesisTitleRef.ToString(BCVRef.RefStringFormat.Exchange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString method for a bogus reference (intro material on chapter 2?).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ToString_NormalVerse()
		{
			BCVRef exodusTitleRef = new BCVRef(2004006);
			Assert.AreEqual("EXO 4:6", exodusTitleRef.ToString(BCVRef.RefStringFormat.General));
			Assert.AreEqual("EXO 4:6", exodusTitleRef.ToString(BCVRef.RefStringFormat.Exchange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to instantiate a BCVRef from a string representation of a book
		/// title that uses the Exchange format.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Parse_Title()
		{
			BCVRef genesisTitleRef = new BCVRef("GEN Title");
			Assert.AreEqual(1, genesisTitleRef.Book);
			Assert.AreEqual(0, genesisTitleRef.Chapter);
			Assert.AreEqual(0, genesisTitleRef.Verse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to instantiate a BCVRef from a string representation of a book
		/// introduction that uses the Exchange format.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Parse_Intro()
		{
			BCVRef genesisTitleRef = new BCVRef("GEN Intro");
			Assert.AreEqual(1, genesisTitleRef.Book);
			Assert.AreEqual(1, genesisTitleRef.Chapter);
			Assert.AreEqual(0, genesisTitleRef.Verse);
		}
	}
	#endregion

	#region Parsing Tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for methods on BCVRef that parse references, chapter numbers, and verse numbers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BCVRef_ParseChapterVerseNumberTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up to initialize VersificationTable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			BCVRefTests.InitializeVersificationTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the VerseToScrRef method when dealing with large verse numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseToScrRefTest_LargeVerseNumber()
		{
			BCVRef startVerse = new BCVRef();
			BCVRef endVerse = new BCVRef();
			string literal, remaining;
			bool convertSuccessful;

			// Test a really large number that will pass the Int16.MaxValue
			convertSuccessful = BCVRef.VerseToScrRef("5200000000000", out literal,
				out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(0, startVerse.Verse);
			Assert.AreEqual(0, endVerse.Verse);
			Assert.IsFalse(convertSuccessful);

			// Test a verse number just under the limit
			convertSuccessful = BCVRef.VerseToScrRef("32766", out literal,
				out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(32766, startVerse.Verse);
			Assert.AreEqual(32766, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Parse method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseTest()
		{
			BCVRef bcvRef = new BCVRef();

			// Note: Don't break into individual unit tests because this test also makes sure
			// that the results of a previous parse don't have unintended consequences for a
			// subsequent parse.

			// Test a normal reference
			bcvRef.Parse("EXO 9:32");
			Assert.AreEqual(2, bcvRef.Book);
			Assert.AreEqual(9, bcvRef.Chapter);
			Assert.AreEqual(32, bcvRef.Verse);

			// Test a bogus book
			bcvRef.Parse("GYQ 8:12");
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(9, bcvRef.Chapter);
			Assert.AreEqual(32, bcvRef.Verse);

			// Test large chapter and verse numbers
			bcvRef.Parse("MAT 1000:2500");
			Assert.AreEqual(40, bcvRef.Book);
			Assert.AreEqual(1000, bcvRef.Chapter);
			Assert.AreEqual(2500, bcvRef.Verse);

			// Test no chapter or verse number
			bcvRef.Parse("REV");
			Assert.AreEqual(66, bcvRef.Book);
			Assert.AreEqual(1, bcvRef.Chapter);
			Assert.AreEqual(1, bcvRef.Verse);

			// Test empty string - should not crash
			bcvRef.Parse("");
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(1, bcvRef.Chapter);
			Assert.AreEqual(1, bcvRef.Verse);

			// Test no verse number
			bcvRef.Parse("LUK 5");
			Assert.AreEqual(42, bcvRef.Book);
			Assert.AreEqual(5, bcvRef.Chapter);
			Assert.AreEqual(1, bcvRef.Verse);

			// Test invalid format
			bcvRef.Parse("ROM 5!3@4");
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(5, bcvRef.Chapter);
			Assert.AreEqual(3, bcvRef.Verse);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for converting verse number strings to BCVRefs. These are similar
		/// scenarios tested in ExportUsfm:VerseNumParse. All of them are successfully parsed
		/// there. The scenarios which have problems converting verse number strings, are
		/// commented out with a description of the incorrect result.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseToScrRefTest()
		{
			BCVRef startVerse = new BCVRef();
			BCVRef endVerse = new BCVRef();
			string literal, remaining = string.Empty;
			bool convertSuccessful;

			// Test invalid verse number strings
			convertSuccessful = BCVRef.VerseToScrRef("-12", out literal, out remaining, ref startVerse, ref endVerse);
			//Assert.AreEqual(12, startVerse.Verse); // start verse set to 0 instead of 12
			Assert.AreEqual(12, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful);	// Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("12-", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(12, startVerse.Verse);
			Assert.AreEqual(12, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("a3", out literal, out remaining, ref startVerse, ref endVerse);
			//Assert.AreEqual(3, startVerse.Verse); // does not set starting verse value
			//Assert.AreEqual(3, endVerse.Verse); // does not set end verse value
			Assert.IsFalse(convertSuccessful);
			convertSuccessful = BCVRef.VerseToScrRef("15b-a", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(15, startVerse.Verse);
			Assert.AreEqual(15, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("3bb", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(3, startVerse.Verse);
			Assert.AreEqual(3, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("0", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(0, startVerse.Verse);
			Assert.AreEqual(0, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef(" 12", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(12, startVerse.Verse);
			Assert.AreEqual(12, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("12 ", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(12, startVerse.Verse);
			Assert.AreEqual(12, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("12-10", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(12, startVerse.Verse); // out of order
			//Assert.AreEqual(12, endVerse.Verse); // end verse set to 12 instead of 10
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("139-1140", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(139, startVerse.Verse); // 1140 is out of range of valid verse numbers
			//Assert.AreEqual(139, endVerse.Verse); // end verse set to 1140 instead of 139
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef("177-140", out literal, out remaining, ref startVerse, ref endVerse);
			//Assert.AreEqual(140, startVerse.Verse); // 177 is out of range of valid verse numbers
			Assert.AreEqual(140, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			//Review: should this be a requirement?
			//			convertSuccessful = BCVRef.VerseToScrRef("177", out literal, out remaining, ref startVerse, ref endVerse);
			//			Assert.AreEqual(0, startVerse.Verse); // 177 is out of range of valid verse numbers
			//			Assert.AreEqual(0, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful);
			convertSuccessful = BCVRef.VerseToScrRef(String.Empty, out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(0, startVerse.Verse);
			Assert.AreEqual(0, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.
			convertSuccessful = BCVRef.VerseToScrRef(String.Empty, out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(0, startVerse.Verse);
			Assert.AreEqual(0, endVerse.Verse);
			//Assert.IsFalse(convertSuccessful); // Does not detect invalid verse number format.

			// Test valid verse number strings
			convertSuccessful = BCVRef.VerseToScrRef("1a", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(1, startVerse.Verse);
			Assert.AreEqual(1, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
			convertSuccessful = BCVRef.VerseToScrRef("2a-3b", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(2, startVerse.Verse);
			Assert.AreEqual(3, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
			convertSuccessful = BCVRef.VerseToScrRef("4-5d", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(4, startVerse.Verse);
			Assert.AreEqual(5, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
			convertSuccessful = BCVRef.VerseToScrRef("6", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(6, startVerse.Verse);
			Assert.AreEqual(6, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
			convertSuccessful = BCVRef.VerseToScrRef("66", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(66, startVerse.Verse);
			Assert.AreEqual(66, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
			convertSuccessful = BCVRef.VerseToScrRef("176", out literal, out remaining, ref startVerse, ref endVerse);
			Assert.AreEqual(176, startVerse.Verse);
			Assert.AreEqual(176, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
			//We expect this test to pass
			//RTL verse bridge should be valid syntax
			convertSuccessful = BCVRef.VerseToScrRef("6" + '\u200f' + "-" + '\u200f' + "8", out literal, out remaining,
				ref startVerse, ref endVerse);
			Assert.AreEqual(6, startVerse.Verse);
			Assert.AreEqual(8, endVerse.Verse);
			Assert.IsTrue(convertSuccessful);
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Tests the BCVRef.ParseRefRange method when given a USFM-style chapter
		///// range.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//[Test]
		//public void ParseRefRange_USFMStyleChapterRange()
		//{
		//    BCVRef bcvRefStart = new BCVRef();
		//    BCVRef bcvRefEnd = new BCVRef();
		//    Assert.IsTrue(BCVRef.ParseRefRange("MRK 1--2", ref bcvRefStart, ref bcvRefEnd, Ver));
		//    Assert.AreEqual(new BCVRef(41, 1, 1), bcvRefStart);
		//    ScrReference refMax = new ScrReference(41, 2, 1, m_scr.Versification);
		//    refMax.Verse = refMax.LastVerse;
		//    Assert.AreEqual(refMax, bcvRef.LocationMax);
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range with the book ID
		/// specified only in the first reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_NormalVerseRange_BookSpecifiedOnce()
		{
			BCVRef bcvRefStart = new BCVRef();
			BCVRef bcvRefEnd = new BCVRef();
			Assert.IsTrue(BCVRef.ParseRefRange("MRK 1:8-2:15", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 1, 8), bcvRefStart);
			Assert.AreEqual(new BCVRef(41, 2, 15), bcvRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range with the
		/// book ID specified both references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_NormalVerseRange_BookSpecifiedTwice()
		{
			BCVRef bcvRefStart = new BCVRef();
			BCVRef bcvRefEnd = new BCVRef();
			Assert.IsTrue(BCVRef.ParseRefRange("MRK 1:8-MRK 2:15", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 1, 8), bcvRefStart);
			Assert.AreEqual(new BCVRef(41, 2, 15), bcvRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range consisting of two
		/// BBCCCVVV-format integers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_ValidBBCCCVVVRange()
		{
			BCVRef bcvRefStart = new BCVRef();
			BCVRef bcvRefEnd = new BCVRef();
			Assert.IsTrue(BCVRef.ParseRefRange("40001002-41002006", ref bcvRefStart, ref bcvRefEnd, true));
			Assert.AreEqual(new BCVRef(40, 1, 2), bcvRefStart);
			Assert.AreEqual(new BCVRef(41, 2, 6), bcvRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range consisting of two
		/// BBCCCVVV-format integers that have values that are bogusly out of range.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_InvalidBBCCCVVVRange()
		{
			BCVRef bcvRefStart = new BCVRef();
			BCVRef bcvRefEnd = new BCVRef();
			Assert.IsFalse(BCVRef.ParseRefRange("40029001-67001001", ref bcvRefStart, ref bcvRefEnd, true));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range without a book ID.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_NormalVerseRange_BookNotSpecified()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(41, 2, 3);
			Assert.IsTrue(BCVRef.ParseRefRange("1:8-2:15", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 1, 8), bcvRefStart, "Book of Mark is inferred from incoming reference.");
			Assert.AreEqual(new BCVRef(41, 2, 15), bcvRefEnd, "Book of Mark is inferred from incoming reference.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range without a book ID
		/// and the incoming references are for different books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_VerseRange_BookNotSpecified_BooksAreDifferent()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(66, 2, 3);
			Assert.IsFalse(BCVRef.ParseRefRange("1:8-2:15", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefStart, "Since ParseRefRange failed, the reference should be unchanged");
			Assert.AreEqual(new BCVRef(66, 2, 3), bcvRefEnd, "Since ParseRefRange failed, the reference should be unchanged");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range without
		/// chapter numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_NormalVerseRange_ChapterNotSpecified()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(41, 2, 3);
			Assert.IsTrue(BCVRef.ParseRefRange("2-15", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 2), bcvRefStart);
			Assert.AreEqual(new BCVRef(41, 2, 15), bcvRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range without chapter
		/// numbers and the incoming references have chapter numbers that don't match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_ChapterNotSpecified_ChaptersAreDifferent()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(41, 5, 3);
			Assert.IsFalse(BCVRef.ParseRefRange("8-15", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefStart, "Since ParseRefRange failed, the reference should be unchanged");
			Assert.AreEqual(new BCVRef(41, 5, 3), bcvRefEnd, "Since ParseRefRange failed, the reference should be unchanged");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range without chapter
		/// numbers and the incoming references have book numbers that don't match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_ChapterNotSpecified_BooksAreDifferent()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(43, 2, 9);
			Assert.IsFalse(BCVRef.ParseRefRange("8-15", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefStart, "Since ParseRefRange failed, the reference should be unchanged");
			Assert.AreEqual(new BCVRef(43, 2, 9), bcvRefEnd, "Since ParseRefRange failed, the reference should be unchanged");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a single verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_SingleReference()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(41, 2, 3);
			Assert.IsTrue(BCVRef.ParseRefRange("MRK 2:3", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefStart);
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a bogus string consisting of a
		/// single number (treat as a verse number).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_BogusReferenceRange_SingleNumber()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(41, 2, 3);
			Assert.IsTrue(BCVRef.ParseRefRange("7", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 7), bcvRefStart);
			Assert.AreEqual(new BCVRef(41, 2, 7), bcvRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a bogus string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_BogusReferenceRange_Unintelligible()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(41, 2, 3);
			Assert.IsFalse(BCVRef.ParseRefRange("XYZ 3:4&6:7", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefStart, "Since ParseRefRange failed, the reference should be unchanged");
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefEnd, "Since ParseRefRange failed, the reference should be unchanged");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given more than two references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_BogusReferenceRange_MoreThanTwoRefs()
		{
			BCVRef bcvRefStart = new BCVRef(41, 2, 3);
			BCVRef bcvRefEnd = new BCVRef(41, 2, 3);
			Assert.IsFalse(BCVRef.ParseRefRange("GEN 4:5 - MAT 6:8 - REV 1:9", ref bcvRefStart, ref bcvRefEnd));
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefStart, "Since ParseRefRange failed, the reference should be unchanged");
			Assert.AreEqual(new BCVRef(41, 2, 3), bcvRefEnd, "Since ParseRefRange failed, the reference should be unchanged");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range that
		/// covers multiple books and caller is okay with that.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_NormalReferenceRange_MultipleBooks()
		{
			BCVRef bcvRefStart = new BCVRef();
			BCVRef bcvRefEnd = new BCVRef();
			Assert.IsTrue(BCVRef.ParseRefRange("MAT 1:5-REV 12:2", ref bcvRefStart, ref bcvRefEnd, true));
			Assert.AreEqual(new BCVRef(40, 1, 5), bcvRefStart);
			Assert.AreEqual(new BCVRef(66, 12, 2), bcvRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range that
		/// covers multiple books but caller is requesting a range for a single book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_BogusReferenceRange_MultipleBooks()
		{
			BCVRef bcvRefStart = new BCVRef();
			BCVRef bcvRefEnd = new BCVRef();
			Assert.IsFalse(BCVRef.ParseRefRange("MAT 1:5-REV 12:2", ref bcvRefStart, ref bcvRefEnd, false));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BCVRef.ParseRefRange method when given a verse range whose
		/// start is after its end (should be rejected).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefRange_BogusReferenceRange_Backwards()
		{
			BCVRef bcvRefStart = new BCVRef();
			BCVRef bcvRefEnd = new BCVRef();
			Assert.IsFalse(BCVRef.ParseRefRange("MRK 2:5-MRK 1:2", ref bcvRefStart, ref bcvRefEnd));
		}
	}
	#endregion
}
