	// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2006' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChapterVerseTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System.Runtime.InteropServices;
using SILUBS.SharedScrUtils;
using System.Reflection;
using SIL.FieldWorks.Common.Utils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TeCheckingToolTests class, runs tests on ChapterVerseCheck.cs
	///	Tests are done by creating a version of Haggai with chapter or verse problems in the
	/// text and then verifying the the problems are correctly reported by the checking code.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ChapterVerseTests : ScrChecksTestBase
	{
		private TestChecksDataSource m_dataSource;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the versification tables.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			BCVRefTests.InitializeVersificationTable();
		}

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
			m_check = new ChapterVerseCheck(m_dataSource, RecordError);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the instance of the Chapter-Verse check that we're testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ChapterVerseCheck Check
		{
			get { return m_check as ChapterVerseCheck; }
		}

		// Future test cases to account for:
		//		Allowing an app to regard missing chapter 1 and/or missing verse 1 as an error
		//		errors in verse bridges: 6-5, 5- ,... worry about it? 5- returns 5 right now
		//		duplicate chapter error?
		//		case: 7a-8, ok for now, will expect another verse 8, get 1 missing verse error, user then able to find problem
		//		it is just not optimized to tell exactly that "part b" is missing
		//		Unique Case: 2:1515 returns out of range error if located following 14 where verse 15
		//			  would normally occur and this error could occur. But if a verse number out of range is found elsewhere
		//			  it logs an out of range error and a duplicate error, which it should not log a duplicate error
		//		Overlapping verse bridges: 1-5 3-8
		//
		//  Special test case examples that are done, accounted for:
		//		Case: 1 2 6  multiple missing verses
		//		Case: verse # > last verse for that chapter (out of range)
		//		Case: no chapter 1 (not error, chapter 1 is optional)
		//		Case: chapter # > chapter total for the book
		//		Case: 1 2-3 5 allow bridges
		//			  still catches last verse out of range, if bridge out of range
		//		Case: what about just missing then out of order 1 2 4 5 3 . Look back through errorList
		//		Case: verse bridge: 1 2 3-45 6 7 or 1 2 3-8 6 7 , treats same as repeated verse numbers with an out of range error
		//		Case: missing verse at end of chapter
		//		Case: when expected verse reaches last verse, say 26 and then hit verse 27, now assumes
		//			  all verses before are done and sets next expected verse to 1 for next chapter
		//		Case: 19 20 chap1 1 2 where verse total should have reached 21 or mor, catch missing verses of previous chap
		//		Case: 1 2 45 3  vs.  1 2 45 1 (45 out of range)
		//		Case: 2:1515 returns out of range error when at verse 15 where error would most likely occur
		//		Case: 19a  19b 20 allow verse parts, correctly does not allow verse part c
		//		Case: 10a at end of chapter, still catches missing 10b verse, logs missing verse error
		//		Case: 1 2 7 3 4 5 6 8 out of order and missing
		//		Case: 1 2 7 3 4 5 6 7 out of order and repeat
		//		Case: Missing chapter number following a verse bridge
		//		Note: 1 2 3 5 6 7... 15 3-4 16 ?? throws a duplicate. If bridge, 3-4 go together

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the AnyOverlappingVerses method with only one verse in both ranges.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OverlappingSingleVerse1()
		{
			object[] retVerses;
			Assert.IsTrue(Check.AnyOverlappingVerses(5, 5, 5, 5, out retVerses));
			Assert.AreEqual(1, retVerses.Length);
			Assert.AreEqual(5, retVerses[0]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the AnyOverlappingVerses method with one verse in one range and a real
		/// range in the other.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OverlappingSingleVerse2()
		{
			object[] retVerses;
			Assert.IsTrue(Check.AnyOverlappingVerses(6, 6, 5, 8, out retVerses));
			Assert.AreEqual(1, retVerses.Length);
			Assert.AreEqual(6, retVerses[0]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the AnyOverlappingVerses method with one verse in one range and a real
		/// range in the other.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OverlappingSingleVerse3()
		{
			object[] retVerses;
			Assert.IsTrue(Check.AnyOverlappingVerses(5, 8, 6, 6, out retVerses));
			Assert.AreEqual(1, retVerses.Length);
			Assert.AreEqual(6, retVerses[0]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the AnyOverlappingVerses method with many verses in one range and many in
		/// the other.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OverlappingVerseRange1()
		{
			object[] retVerses;
			Assert.IsTrue(Check.AnyOverlappingVerses(5, 8, 3, 6, out retVerses));
			Assert.AreEqual(2, retVerses.Length);
			Assert.AreEqual(5, retVerses[0]);
			Assert.AreEqual(6, retVerses[1]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the AnyOverlappingVerses method with many verses in one range and many in
		/// the other.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void OverlappingVerseRange2()
		{
			object[] retVerses;
			Assert.IsTrue(Check.AnyOverlappingVerses(5, 20, 10, 100, out retVerses));
			Assert.AreEqual(2, retVerses.Length);
			Assert.AreEqual(10, retVerses[0]);
			Assert.AreEqual(20, retVerses[1]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the CheckForMissingVerses method to make sure detects single
		/// (i.e. non ranges) missing verses.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void CheckForMissingVerses_Singles()
		{
			ITextToken[] versesFound = new ITextToken[7] {
				new DummyTextToken("0"), null, new DummyTextToken("2"),
				new DummyTextToken("003"), null, new DummyTextToken("05"), null };

			object[] args = new object[] { versesFound, 2, 5 };

			BindingFlags flags = BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.InvokeMethod;

			typeof(ChapterVerseCheck).InvokeMember("CheckForMissingVerses",
				flags, null, m_check, args);

			Assert.AreEqual(3, m_errors.Count);

			CheckError(0, versesFound[0].Text, 1, String.Empty, "Missing verse number 1");
			Assert.AreEqual(new BCVRef(2005001), m_errors[0].Tts.MissingStartRef);
			Assert.AreEqual(null, m_errors[0].Tts.MissingEndRef);

			CheckError(1, versesFound[3].Text, 3, String.Empty, "Missing verse number 4");
			Assert.AreEqual(new BCVRef(2005004), m_errors[1].Tts.MissingStartRef);
			Assert.AreEqual(null, m_errors[1].Tts.MissingEndRef);

			CheckError(2, versesFound[5].Text, 2, String.Empty, "Missing verse number 6");
			Assert.AreEqual(new BCVRef(2005006), m_errors[2].Tts.MissingStartRef);
			Assert.AreEqual(null, m_errors[2].Tts.MissingEndRef);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the CheckForMissingVerses method to make sure detects ranges of missing
		/// verses.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void CheckForMissingVerses_Ranges()
		{
			ITextToken[] versesFound = new ITextToken[12] {
				new DummyTextToken("0"), null, null,
				new DummyTextToken("003"), null, null, null,
				new DummyTextToken("7"), new DummyTextToken("8"),
				new DummyTextToken("09"), null, null };

			object[] args = new object[] { versesFound, 2, 5 };

			BindingFlags flags = BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.InvokeMethod;

			typeof(ChapterVerseCheck).InvokeMember("CheckForMissingVerses",
				flags, null, m_check, args);

			Assert.AreEqual(3, m_errors.Count);

			CheckError(0, versesFound[0].Text, 1, String.Empty, "Missing verse numbers 1-2");
			Assert.AreEqual(new BCVRef(2005001), m_errors[0].Tts.MissingStartRef);
			Assert.AreEqual(new BCVRef(2005002), m_errors[0].Tts.MissingEndRef);

			CheckError(1, versesFound[3].Text, 3, String.Empty, "Missing verse numbers 4-6");
			Assert.AreEqual(new BCVRef(2005004), m_errors[1].Tts.MissingStartRef);
			Assert.AreEqual(new BCVRef(2005006), m_errors[1].Tts.MissingEndRef);

			CheckError(2, versesFound[9].Text, 2, String.Empty, "Missing verse numbers 10-11");
			Assert.AreEqual(new BCVRef(2005010), m_errors[2].Tts.MissingStartRef);
			Assert.AreEqual(new BCVRef(2005011), m_errors[2].Tts.MissingEndRef);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find no chapter or verse errors
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NoChapterVerseErrors()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find no chapter or verse errors when script digits are used,
		/// selected Arabic-Indic for this test.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NoChapterVerseErrors_ScriptDigits()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");
			m_dataSource.SetParameterValue("Script Digit Zero", "\u0660");
			m_dataSource.SetParameterValue("Verse Bridge", "\u200F-\u200f");

			// \u0660-\u0669 are Arabic-Indic digits, \u200F is the RTL Mark.
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0661",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0661",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0662",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0663\u200F-\u200f\u0661\u0665",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0662",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0661\u200F-\u200f\u0662\u0663",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find an error when script digits are used but not expected.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void FormatErrors_UnexpectedScriptDigits()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");
			m_dataSource.SetParameterValue("Script Digit Zero", "0");
			m_dataSource.SetParameterValue("Verse Bridge", "\u200F-\u200f");

			// \u0660-\u0669 are Arabic-Indic digits, \u200F is the RTL Mark.
			DummyTextToken badToken1 = new DummyTextToken("\u0661",
				TextType.ChapterNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(badToken1);
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3\u200F-\u200f15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			DummyTextToken badToken2 = new DummyTextToken("1\u200f-\u200f2\u0663",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(badToken2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, badToken1.Text, 0, badToken1.Text, "Invalid chapter number");
			CheckError(1, badToken2.Text, 0, badToken2.Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find an error when script digits are expected but not used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void FormatErrors_ExpectedScriptDigits()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");
			m_dataSource.SetParameterValue("Script Digit Zero", "\u0660");
			m_dataSource.SetParameterValue("Verse Bridge", "\u200F-\u200f");

			// \u0660-\u0669 are Arabic-Indic digits, \u200F is the RTL Mark.
			DummyTextToken badToken1 = new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(badToken1);
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0661",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0662",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0663\u200F-\u200f\u0661\u0665",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u0662",
				TextType.ChapterNumber, false, false, "Paragraph"));
			DummyTextToken badToken2 = new DummyTextToken("\u0661\u200F-\u200f\u06623",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(badToken2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, badToken1.Text, 0, badToken1.Text, "Invalid chapter number");
			CheckError(1, badToken2.Text, 0, badToken2.Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that formatting error is reported when verse bridge contains unexpected
		/// right-to-left marks.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void FormatErrors_UnexpectedRtoLMarksInVerseBridge()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "JUD");
			m_dataSource.SetParameterValue("Chapter Number", "0");
			m_dataSource.SetParameterValue("Verse Bridge", "-");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			DummyTextToken badToken = new DummyTextToken("3\u200f-\u200f25",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(badToken);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, badToken.Text, 0, badToken.Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that formatting error is reported when verse number contains a medial letter.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void FormatErrors_UnexpectedLetterInVerseNumber()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "JUD");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-24",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			DummyTextToken badToken = new DummyTextToken("2a5",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(badToken);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, badToken.Text, 0, badToken.Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that formatting error is reported when wrong verse bridge character is used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void FormatErrors_UnexpectedBridgeCharacter()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "JUD");
			m_dataSource.SetParameterValue("Chapter Number", "0");
			m_dataSource.SetParameterValue("Verse Bridge", "~");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			DummyTextToken badToken = new DummyTextToken("1-25",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(badToken);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, badToken.Text, 0, badToken.Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test use of versification info
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NoChapterVerseErrors_DifferentVersifications()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "NAM");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("1-15",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			ITextToken TempTok2 = new DummyTextToken("1-13",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-19",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);

			m_dataSource.SetParameterValue("Versification Scheme", "Septuagint");

			((DummyTextToken)TempTok).Text = "1-14";
			((DummyTextToken)TempTok2).Text = "1-14";
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find no chapter error when there is no Chapter 1.
		/// Chapter number 1 is optional.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NoErrorWhenMissingChapterOne()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			// Missing chapter number 1
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a chapter error when there is a Chapter 0.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterZeroError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("0",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, m_dataSource.m_tokens[0].Text, "Invalid chapter number");
			CheckError(1, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing chapter number 1");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find errors when Chapter and verse numbers start with a leading 0.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void LeadingZeroErrors()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("01",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("002",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, m_dataSource.m_tokens[0].Text, "Invalid chapter number");
			CheckError(1, m_dataSource.m_tokens[3].Text, 0, m_dataSource.m_tokens[3].Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find no chapter or verse errors when checking only a single
		/// chapter
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void NoChapterVerseErrors_CheckingSingleChapter()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "1");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing chapter number error
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberMissingError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing chapter number 2
			ITextToken TempTok = new DummyTextToken("1-23",
				TextType.VerseNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, TempTok.Text, 0, TempTok.Text, "Duplicate verse numbers");
			CheckError(1, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing chapter number 2");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing chapter number error following a verse bridge
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberMissingError_FollowingVerseBridge()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing chapter number 2
			ITextToken TempTok = new DummyTextToken("1-23",
				TextType.VerseNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, TempTok.Text, 0, TempTok.Text, "Duplicate verse numbers");
			CheckError(1, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing chapter number 2");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing chapter number error when missing final chapters
		/// with no data after the missing chapter number
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberMissingFinalError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing entire chapter 2

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing chapter number 2");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing chapter number error when previous chapter has
		/// no verses
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberMissingNoVerses()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "2TH");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			// error sequence - chapter 1 with verse, chapter 2 no verse, chapter 3 with verse
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-18",
				TextType.VerseNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(2, m_errors.Count);
			CheckError(1, "1", 1, String.Empty, "Missing chapter number 2");
			Assert.AreEqual(m_errors[1].Tts.MissingStartRef.BBCCCVVV, 2000);
			Assert.IsNull(m_errors[1].Tts.MissingEndRef);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a duplicated chapter two
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberDuplicated()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			// error sequence - chapter 1 & 2 with verse, chapter 2 duplicated
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-15",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			DummyTextToken dupChapter = new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(dupChapter);

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, dupChapter.Text, 0, dupChapter.Text, "Duplicate chapter number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing chapter one
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberOneMissing()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "2TH");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			// error sequence - chapter 1 skipped, chapter 2 & 3 fully present
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-17",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-18",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, "2", 0, String.Empty, "Missing chapter number 1");
			Assert.AreEqual(m_errors[0].Tts.MissingStartRef.BBCCCVVV, 1000);
			Assert.IsNull(m_errors[0].Tts.MissingEndRef);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing verse number error
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumberMissingError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing verse 2
			ITextToken TempTok = new DummyTextToken("3-14",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing verse 15
			ITextToken TempTok2 = new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			// Missing verse 1
			ITextToken TempTok3 = new DummyTextToken("2-22",
				TextType.VerseNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok3);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing verse 23

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(4, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[1].Text, 1, String.Empty, "Missing verse number 2");
			CheckError(1, TempTok.Text, 4, String.Empty, "Missing verse number 15");
			CheckError(2, TempTok2.Text, 1, String.Empty, "Missing verse number 1");
			CheckError(3, TempTok3.Text, 4, String.Empty, "Missing verse number 23");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a chapter number outside bounds (too large) error
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberOutOfRangeError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("5",
				TextType.ChapterNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, TempTok.Text, 0, TempTok.Text, "Chapter number out of range");
			CheckError(1, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing chapter number 2");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find verse numbers out of range
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumbersOutOfRangeError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("16",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			ITextToken TempTok2 = new DummyTextToken("1-24",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, TempTok.Text, 0, TempTok.Text, "Verse number out of range");
			CheckError(1, TempTok2.Text, 0, TempTok2.Text, "Verse number out of range");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find verse "number" (i.e. text that's marked with the verse
		/// style) at beyond last valid verse.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumbersBeyondLastValidInChapter()
		{
			m_dataSource.SetParameterValue("InvalidAtEnd", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("aa",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, TempTok.Text, 0, TempTok.Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find multiple missing verse numbers error
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MultipleVerseNumbersMissingError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing verses 2-5
			m_dataSource.m_tokens.Add(new DummyTextToken("6-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("1-20",
				TextType.VerseNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing verses 21-23

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[1].Text, 1, String.Empty, "Missing verse numbers 2-5");
			CheckError(1, TempTok.Text, 4, String.Empty, "Missing verse numbers 21-23");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find duplicate verse number error
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void DuplicateVerseNumberError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			// Chapter 1
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Duplicate verse number 1
			ITextToken TempTok = new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Chapter 2
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Duplicate verse number 13
			ITextToken TempTok2 = new DummyTextToken("13",
				TextType.VerseNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Duplicate verse numbers 21-23
			ITextToken TempTok3 = new DummyTextToken("21-23",
				TextType.VerseNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok3);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(3, m_errors.Count);

			CheckError(0, TempTok.Text, 0, TempTok.Text, "Duplicate verse number");
			CheckError(1, TempTok2.Text, 0, TempTok2.Text, "Duplicate verse number");
			CheckError(2, TempTok3.Text, 0, TempTok3.Text, "Unexpected verse numbers");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find verse numbers out of order (there will also be missing verses)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumbersOutOfOrderError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("4-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-9",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("12-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok2 = new DummyTextToken("10-11",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);

			CheckError(0, TempTok.Text, 0, TempTok.Text, "Verse number out of order; expected verse 4");
			CheckError(1, TempTok2.Text, 0, TempTok2.Text, "Verse numbers out of order");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find verse number out of range for 999 and still find missing
		/// verse next
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumberGreaterThan999()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("1-14",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok2 = new DummyTextToken("1515",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);

			CheckError(0, TempTok2.Text, 0, TempTok2.Text, "Verse number out of range");
			CheckError(1, TempTok.Text, 4, String.Empty, "Missing verse numbers 15-16");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find no error for verse parts a and b
		/// If there were a 2a with no 2b will throw missing verse for verse 5
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumberPartsAandB()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2a",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("First part of verse two",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Section Head Text",
				TextType.Other, true, false, "Section Head"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2b",
				TextType.VerseNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Second part of verse two",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-22",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("23a",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("23b",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that errors are flagged when only part a or part b of a verse couplet are
		/// missing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumberPartAOrB()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");

			// Currently we don't catch any of these invalid cases...
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1a-3",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("4-6b",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("7-8a",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("9-10",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("11-13",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("14b-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			// These cases are valid...
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2a",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2b",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-4b",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("5",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			// These cases are not valid (should produce errors)...
			ITextToken TempTok = new DummyTextToken("6a",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("7",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok2 = new DummyTextToken("8b",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok3 = new DummyTextToken("9b",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok3);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok4 = new DummyTextToken("10a",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok4);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok5 = new DummyTextToken("11b",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok5);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok6 = new DummyTextToken("12-13a",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok6);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("14",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("15-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(6, m_errors.Count);

			CheckError(0, TempTok.Text, 2, String.Empty, "Missing verse number 6b");
			CheckError(1, TempTok2.Text, 0, String.Empty, "Missing verse number 8a");
			CheckError(2, TempTok3.Text, 0, String.Empty, "Missing verse number 9a");
			CheckError(3, TempTok4.Text, 3, String.Empty, "Missing verse number 10b");
			CheckError(4, TempTok5.Text, 0, String.Empty, "Missing verse number 11a");
			CheckError(5, TempTok6.Text, 6, String.Empty, "Missing verse number 13b");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find error for verse part c
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseNumberPartCError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, false, false, "Paragraph"));
			ITextToken tempTok1 = new DummyTextToken("1-23a",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(tempTok1);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken tempTok2 = new DummyTextToken("23c",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(tempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);

			CheckError(0, tempTok1.Text, 5, string.Empty, "Missing verse number 23b");
			CheckError(1, tempTok2.Text, 0, tempTok2.Text, "Invalid verse number");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests the case when chapter 1 and verse 1 of a book is missing at the same time.
		///
		/// NOTE: The following comment was written before DavidO & CoreyW made significant
		/// changes. I'm not sure exactly what the comment means and is probably no
		/// longer relevant. -- DDO
		///
		/// (If it has just finished last verse of previous chapter, it will assume new
		/// chapter and only throw the two errors, missing chapter number and missing verse
		/// number. It will not throw duplicate verse error for all following verses as it
		/// would normally. Now it will only do so if verse 1 and 2 are missing after a
		/// chapter number missing. Since it could be a more common error to lose chapter
		/// number and verse one together, we allow for this unique case.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MissingChapterOneandVerseOneError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, String.Empty, "Missing verse number 1");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests the case when chapter 2 and verse 1 of a book is missing at the same time.
		/// The assumption for this test is that we'll get the same behavior when any chapter
		/// greater than 2 is missing when that chapter's verse one is missing. That is why
		/// we only check the case for chapter 2. See MissingChapterTwoandVerseOneError for
		/// testing the case when chapter 1, verse 1 is missing at the same time.
		/// (If it has just finished last verse of previous chapter, it will assume new
		/// chapter and only throw the two errors, missing chapter number and missing verse
		/// number. It will not throw duplicate verse error for all following verses as it
		/// would normally. Now it will only do so if verse 1 and 2 are missing after a
		/// chapter number missing. Since it could be a more common error to lose chapter
		/// number and verse one together, we allow for this unique case.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MissingChapterTwoandVerseOneError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("2",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok2 = new DummyTextToken("3-23",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(3, m_errors.Count);

			CheckError(0, TempTok.Text, 0, TempTok.Text, "Unexpected verse number");
			CheckError(1, TempTok2.Text, 0, TempTok2.Text, "Unexpected verse numbers");
			CheckError(2, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing chapter number 2");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Space before or after verse number or bridge. Space is not easily visible
		/// if it is formatted as a verse style.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InvalidVerse_SpaceError()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" 1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("2-22 ",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[1].Text, 0, m_dataSource.m_tokens[1].Text,
				"Space found in verse number");
			CheckError(1, TempTok.Text, 0, TempTok.Text, "Space found in verse bridge");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Checks for a verse number that only consists of letters.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InvalidVerse_InvalidCharacters()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("zv",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2-13",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("14z7a",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("text",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("more text",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));
			ITextToken TempTok2 = new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok2);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok3 = new DummyTextToken("u-r-an-idot",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok3);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(7, m_errors.Count);

			CheckError(0, TempTok.Text, 0, TempTok.Text, "Invalid verse number");
			CheckError(1, m_dataSource.m_tokens[5].Text, 0, m_dataSource.m_tokens[5].Text, "Verse number out of range");
			CheckError(2, m_dataSource.m_tokens[5].Text, 0, m_dataSource.m_tokens[5].Text, "Invalid verse number");
			CheckError(3, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing verse number 1");
			CheckError(4, m_dataSource.m_tokens[3].Text, 4, String.Empty, "Missing verse number 14");
			CheckError(5, TempTok3.Text, 0, TempTok3.Text, "Invalid verse number");
			CheckError(6, TempTok2.Text, 1, String.Empty, "Missing verse numbers 2-22");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Checks for a chapter number that only consists of letters.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InvalidChapter_InvalidCharacters()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2-15",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			ITextToken TempTok = new DummyTextToken("jfuo",
				TextType.ChapterNumber, true, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);

			CheckError(0, TempTok.Text, 0, TempTok.Text, "Invalid chapter number");
			CheckError(1, TempTok.Text, 4, String.Empty, "Missing chapter number 2");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Checks for a missing verse number at the end of a chapter
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MissingVerse_AtEndOfChapter()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "HAG");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1-14",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing verse 15, Missing chapter 2
			ITextToken TempTok = new DummyTextToken("1-23",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(3, m_errors.Count);

			CheckError(0, TempTok.Text, 0, TempTok.Text, "Duplicate verse numbers");
			CheckError(1, m_dataSource.m_tokens[1].Text, 4, String.Empty, "Missing verse number 15");
			CheckError(2, m_dataSource.m_tokens[0].Text, 1, String.Empty, "Missing chapter number 2");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Checks for a missing verse number at the end of a chapter
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MissingChapter_Multiple()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			// TODO: Figure out where to put vrs files for tests. Probably want to just include
			// copies here locally with the tests.
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "JAS");
			m_dataSource.SetParameterValue("Chapter Number", "0");

			// Missing chapter 1 (ignored)
			m_dataSource.m_tokens.Add(new DummyTextToken("1-27",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing chapter 2
			ITextToken TempTok = new DummyTextToken("1-26",
				TextType.VerseNumber, false, false, "Paragraph");
			m_dataSource.m_tokens.Add(TempTok);
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));
			// Missing chapters 3-5
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(5, m_errors.Count);

			CheckError(0, TempTok.Text, 0, TempTok.Text, "Duplicate verse numbers");

			for (int i = 2; i < 6; i++)
				CheckError(i - 1, m_dataSource.m_tokens[0].Text, 0, String.Empty, string.Format("Missing chapter number {0}", i));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing verse text without white space
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseTextMissingText()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "2TH");
			m_dataSource.SetParameterValue("Chapter Number", "0");


			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-12",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2-17",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-18",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(4, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing verse text with white space
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseTextMissingTextWithWhiteSpace()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "2TH");
			m_dataSource.SetParameterValue("Chapter Number", "0");


			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-12",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken(" ",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("\r\n",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2-17",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("\t",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-18",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken(" ",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(4, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that check should assume missing chapter one and verse one present with
		/// first text token.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseTextAssumeChapterOneVerseOne()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "2TH");
			m_dataSource.SetParameterValue("Chapter Number", "0");


			// no chapter one - assumed
			// no verse one - assumed
			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2-12",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-17",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-18",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing chapter one
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseTextMissingChapterOne()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "2TH");
			m_dataSource.SetParameterValue("Chapter Number", "0");


			// no chapter one - assumed
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2-12",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-17",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("1-18",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that should find a missing verse one
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void VerseTextMissingVerseOne()
		{
			m_dataSource.SetParameterValue("OmittedVerses", "");
			m_dataSource.SetParameterValue("Versification Scheme", "English");
			m_dataSource.SetParameterValue("Book ID", "2TH");
			m_dataSource.SetParameterValue("Chapter Number", "0");


			// no verse one - assumed
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2-12",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2-17",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("3",
				TextType.ChapterNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("2-18",
				TextType.VerseNumber, true, false, "Paragraph"));

			m_dataSource.m_tokens.Add(new DummyTextToken("verse body",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}

		//Need test for chapter number out of range with verses following it (because valid chapter missing).
		//The errors thrown currently are correct,but need a test for that, for those errors to be optimized in the future.
		//Currently handles out of range chapter, by incrementing to next valid chapter, assuming that is what's wanted
	}
}
