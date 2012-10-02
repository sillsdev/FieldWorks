// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrReferenceTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Microsoft.Win32;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	#region ScrReferenceTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ScrReference class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrReferenceTests: BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the ScrReference class for tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void InitializeScrReferenceForTests()
		{
			BCVRefTests.InitializeVersificationTable();
			BCVRef.SupportDeuterocanon = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up to initialize ScrReference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			InitializeScrReferenceForTests();
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
		/// Test the constructors of ScrReference and the Valid and IsBookTitle properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidScrReferences()
		{
			ScrReference bcvRef = new ScrReference(1, 2, 3, Paratext.ScrVers.English);
			Assert.IsTrue(bcvRef.Valid);
			Assert.IsFalse(bcvRef.IsBookTitle);
			Assert.AreEqual(1002003, (int)bcvRef);
			Assert.AreEqual(1, bcvRef.Book);
			Assert.AreEqual(2, bcvRef.Chapter);
			Assert.AreEqual(3, bcvRef.Verse);
			Assert.AreEqual(Paratext.ScrVers.English, bcvRef.Versification);

			bcvRef = new ScrReference(4005006, Paratext.ScrVers.Original);
			Assert.IsTrue(bcvRef.Valid);
			Assert.IsFalse(bcvRef.IsBookTitle);
			Assert.AreEqual(4005006, (int)bcvRef);
			Assert.AreEqual(4, bcvRef.Book);
			Assert.AreEqual(5, bcvRef.Chapter);
			Assert.AreEqual(6, bcvRef.Verse);
			Assert.AreEqual(Paratext.ScrVers.Original, bcvRef.Versification);

			bcvRef = new ScrReference();
			Assert.IsFalse(bcvRef.Valid);
			Assert.IsFalse(bcvRef.IsBookTitle);
			Assert.AreEqual(0, (int)bcvRef);
			Assert.AreEqual(0, bcvRef.Book);
			Assert.AreEqual(0, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Verse);

			bcvRef = new ScrReference(5, 0, 0, Paratext.ScrVers.English);
			Assert.IsFalse(bcvRef.Valid);
			Assert.IsTrue(bcvRef.IsBookTitle);
			Assert.AreEqual(5000000, (int)bcvRef);
			Assert.AreEqual(5, bcvRef.Book);
			Assert.AreEqual(0, bcvRef.Chapter);
			Assert.AreEqual(0, bcvRef.Verse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test invalid ScrReference values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidScrReferences()
		{
			// Invalid BCVs
			ScrReference scrRef = new ScrReference(7, 8, 1001, Paratext.ScrVers.English);
			Assert.IsFalse(scrRef.Valid);
			scrRef.MakeValid();
			Assert.AreEqual(7008035, (int)scrRef);
			Assert.AreEqual(7, scrRef.Book);
			Assert.AreEqual(8, scrRef.Chapter);
			Assert.AreEqual(35, scrRef.Verse);

			scrRef = new ScrReference(9, 1002, 10, Paratext.ScrVers.English);
			Assert.IsFalse(scrRef.Valid);
			scrRef.MakeValid();
			Assert.AreEqual(9031010, (int)scrRef);
			Assert.AreEqual(9, scrRef.Book);
			Assert.AreEqual(31, scrRef.Chapter);
			Assert.AreEqual(10, scrRef.Verse);

			scrRef = new ScrReference(101, 11, 12, Paratext.ScrVers.English);
			Assert.IsFalse(scrRef.Valid);
			scrRef.MakeValid();
			Assert.AreEqual(66011012, (int)scrRef);
			Assert.AreEqual(66, scrRef.Book);
			Assert.AreEqual(11, scrRef.Chapter);
			Assert.AreEqual(12, scrRef.Verse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ScrReference.LastChapter property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastChapterForBook()
		{
			ScrReference scrRef = new ScrReference("HAG 1:1", Paratext.ScrVers.English);
			Assert.AreEqual(2, scrRef.LastChapter);
			scrRef.Book = ScrReference.BookToNumber("PSA");
			Assert.AreEqual(150, scrRef.LastChapter);
			scrRef.Book = 456;
			Assert.AreEqual(0, scrRef.LastChapter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the overload of the constructor that takes a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_InvalidVersification()
		{
			ScrReference reference = new ScrReference("GEN 1:1", Paratext.ScrVers.Unknown);
			Assert.IsFalse(reference.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing 2 references with different versifications
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equal_DifferentVersification()
		{
			ScrReference ref1 = new ScrReference("GEN 31:55", Paratext.ScrVers.English);
			ScrReference ref2 = new ScrReference("GEN 32:1", Paratext.ScrVers.Original);
			Assert.IsTrue(ref1 == ref2);

			ref1 = new ScrReference("JOB 41:9", Paratext.ScrVers.English);
			ref2 = new ScrReference("JOB 41:1", Paratext.ScrVers.Original);
			Assert.IsTrue(ref1 == ref2);

			ref1 = new ScrReference("JOB 41:9", Paratext.ScrVers.English);
			ref2 = new ScrReference("JOB 41:2", Paratext.ScrVers.Original);
			Assert.IsFalse(ref1 == ref2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing 2 references with different versifications when one is of an unknown
		/// versification
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equal_UnknownVersification()
		{
			ScrReference ref1 = new ScrReference("GEN 30:1", Paratext.ScrVers.Unknown);
			ScrReference ref2 = new ScrReference("GEN 30:1", Paratext.ScrVers.Original);
			Assert.IsTrue(ref1 == ref2);

			ref1 = new ScrReference("GEN 19:1", Paratext.ScrVers.Unknown);
			ref2 = new ScrReference("GEN 21:1", Paratext.ScrVers.English);
			Assert.IsFalse(ref1 == ref2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the less than with 2 references with different versifications when one is of
		/// an unknown versification
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LessThan_UnknownVersification()
		{
			ScrReference ref1 = new ScrReference("GEN 30:1", Paratext.ScrVers.Unknown);
			ScrReference ref2 = new ScrReference("GEN 30:1", Paratext.ScrVers.Original);
			Assert.IsFalse(ref1 < ref2);

			ref1 = new ScrReference("GEN 19:1", Paratext.ScrVers.Unknown);
			ref2 = new ScrReference("GEN 21:1", Paratext.ScrVers.English);
			Assert.IsTrue(ref1 < ref2);

			ref1 = new ScrReference("GEN 21:1", Paratext.ScrVers.Unknown);
			ref2 = new ScrReference("GEN 19:1", Paratext.ScrVers.English);
			Assert.IsFalse(ref1 < ref2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the less than operator with an integer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LessThan_int()
		{
			ScrReference ref2 = new ScrReference("GEN 30:2", Paratext.ScrVers.Original);
			Assert.IsTrue(1030001 < ref2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CompareTo method with a ScrReference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareTo_ScrReference()
		{
			ScrReference ref1 = new ScrReference("GEN 30:1", Paratext.ScrVers.Original);
			ScrReference ref2 = new ScrReference("GEN 30:1", Paratext.ScrVers.Original);
			Assert.AreEqual(0, ref1.CompareTo(ref2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CompareTo method with a BCVRef
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareTo_BCVRef()
		{
			ScrReference ref1 = new ScrReference("GEN 30:1", Paratext.ScrVers.Original);
			BCVRef ref2 = new BCVRef("GEN 30:1");
			Assert.AreEqual(0, ref1.CompareTo(ref2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CompareTo method with an integer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareTo_int()
		{
			ScrReference ref1 = new ScrReference("GEN 30:1", Paratext.ScrVers.Original);
			Assert.AreEqual(0, ref1.CompareTo(1030001));
		}
	}
	#endregion

	#region ParseChapterVerseNumberTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for static methods on ScrReference that parse chapter and verse numbers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ParseChapterVerseNumberTests: BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up to initialize ScrReference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			ScrReferenceTests.InitializeScrReferenceForTests();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ChapterToInt method when dealing with large chapter numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterToInt_LargeChapterNumber()
		{
			// Test a really large number that will pass the Int16.MaxValue
			Assert.AreEqual(Int16.MaxValue, ScrReference.ChapterToInt("5200000000000"));

			// Test a verse number just under the limit
			Assert.AreEqual(32766, ScrReference.ChapterToInt("32766"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the VerseToInt method when dealing with large verse numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseToInt_LargeVerseNumber()
		{
			// Test a really large number that will pass the Int16.MaxValue
			int firstVerse, secondVerse;
			ScrReference.VerseToInt("5200000000000", out firstVerse, out secondVerse);
			Assert.AreEqual(999, firstVerse);
			Assert.AreEqual(999, secondVerse);

			// Test a verse number just under the limit
			ScrReference.VerseToInt("998", out firstVerse, out secondVerse);
			Assert.AreEqual(998, firstVerse);
			Assert.AreEqual(998, secondVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for converting verse number strings to verse number integers. These are similar
		/// scenarios tested in ExportUsfm:VerseNumParse. All of them are successfully parsed
		/// there. The scenarios which have problems converting verse number strings, are
		/// commented out with a description of the incorrect result.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseToIntTest()
		{
			int nVerseStart, nVerseEnd;

			// Test invalid verse number strings
			ScrReference.VerseToInt("-12",out nVerseStart, out nVerseEnd);
			Assert.AreEqual(12, nVerseStart);
			Assert.AreEqual(12, nVerseEnd);
			ScrReference.VerseToInt("14-", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(14, nVerseStart);
			Assert.AreEqual(14, nVerseEnd);
			ScrReference.VerseToInt("a3", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(3, nVerseStart);
			Assert.AreEqual(3, nVerseEnd);
			ScrReference.VerseToInt("15b-a", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(15, nVerseStart);
			Assert.AreEqual(15, nVerseEnd);
			ScrReference.VerseToInt("3bb", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(3, nVerseStart);
			Assert.AreEqual(3, nVerseEnd);
			ScrReference.VerseToInt("0", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(0, nVerseStart);
			Assert.AreEqual(0, nVerseEnd);
			ScrReference.VerseToInt(" 12", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(12, nVerseStart);
			Assert.AreEqual(12, nVerseEnd);
			ScrReference.VerseToInt("14 ", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(14, nVerseStart);
			Assert.AreEqual(14, nVerseEnd);
			ScrReference.VerseToInt("12-10", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(12, nVerseStart);
			//Assert.AreEqual(12, nVerseEnd); // end verse set to 12 instead of 10
			ScrReference.VerseToInt("139-1140", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(139, nVerseStart);
			//Assert.AreEqual(139, nVerseEnd); // end verse set to 999 instead of 139
			ScrReference.VerseToInt("177-140", out nVerseStart, out nVerseEnd);
			//Assert.AreEqual(140, nVerseStart); // start verse set to 177 instead of 140
			Assert.AreEqual(140, nVerseEnd);
			//Review: should this be a requirement?
			//			ScrReference.VerseToInt("177", out nVerseStart, out nVerseEnd);
			//			Assert.AreEqual(0, nVerseStart); // 177 is out of range of valid verse numbers
			//			Assert.AreEqual(0, nVerseEnd);
			ScrReference.VerseToInt(String.Empty, out nVerseStart, out nVerseEnd);
			Assert.AreEqual(0, nVerseStart);
			Assert.AreEqual(0, nVerseEnd);
			ScrReference.VerseToInt(String.Empty, out nVerseStart, out nVerseEnd);
			Assert.AreEqual(0, nVerseStart);
			Assert.AreEqual(0, nVerseEnd);

			// Test valid verse number strings
			ScrReference.VerseToInt("1a", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(1, nVerseStart);
			Assert.AreEqual(1, nVerseEnd);
			ScrReference.VerseToInt("2a-3b", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(2, nVerseStart);
			Assert.AreEqual(3, nVerseEnd);
			ScrReference.VerseToInt("4-5d", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(4, nVerseStart);
			Assert.AreEqual(5, nVerseEnd);
			ScrReference.VerseToInt("6", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(6, nVerseStart);
			Assert.AreEqual(6, nVerseEnd);
			ScrReference.VerseToInt("66", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(66, nVerseStart);
			Assert.AreEqual(66, nVerseEnd);
			ScrReference.VerseToInt("176", out nVerseStart, out nVerseEnd);
			Assert.AreEqual(176, nVerseStart);
			Assert.AreEqual(176, nVerseEnd);
			//We expect this test to pass
			//RTL verse bridge should be valid syntax
			ScrReference.VerseToInt("6" + '\u200f' + "-" + '\u200f' + "8", out nVerseStart,
				out nVerseEnd);
			Assert.AreEqual(6, nVerseStart);
			Assert.AreEqual(8, nVerseEnd);
		}
	}
	#endregion
}
