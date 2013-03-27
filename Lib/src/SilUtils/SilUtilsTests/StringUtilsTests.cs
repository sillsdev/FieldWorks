// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StringUtilsTests.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TsStringUtils tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StringUtilsTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the StripWhitespace method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StripWhitespace()
		{
			Assert.IsNull(StringUtils.StripWhitespace(null));
			Assert.IsEmpty(StringUtils.StripWhitespace(string.Empty));
			Assert.AreEqual("abcd", StringUtils.StripWhitespace(" a b c d "));
			Assert.AreEqual("ab", StringUtils.StripWhitespace("a\tb"));
			Assert.AreEqual("ab", StringUtils.StripWhitespace("a " + Environment.NewLine + " b"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we get a valid filename when the filename contains invalid characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Unit test")]
		public void TestFilterForFileName()
		{
			Assert.AreEqual("My__File__Dude_____.'[];funny()___",
				StringUtils.FilterForFileName(@"My?|File<>Dude\?*:/.'[];funny()" + "\n\t" + '"',
				"?|<>\\?*:/\n\t\""));
		}

		#region FindStringDifference tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when the two
		/// strings are identical.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_IdenticalStrings()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsFalse(StringUtils.FindStringDifference("A simple string", "A simple string", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(-1, ichMin);
			Assert.AreEqual(-1, ichLim1);
			Assert.AreEqual(-1, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when the two
		/// strings have different beginnings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_DifferentBeginning()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsTrue(StringUtils.FindStringDifference("Not a simple string", "A simple string", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(0, ichMin);
			Assert.AreEqual(5, ichLim1);
			Assert.AreEqual(1, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when one of the
		/// strings is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_OneEmptyString()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsTrue(StringUtils.FindStringDifference(string.Empty, "ABC", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(0, ichMin);
			Assert.AreEqual(0, ichLim1);
			Assert.AreEqual(3, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when the two
		/// strings have different endings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_DifferentEnding()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsTrue(StringUtils.FindStringDifference("A simple", "A simple string", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(8, ichMin);
			Assert.AreEqual(8, ichLim1);
			Assert.AreEqual(15, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when the two
		/// strings have different endings (flipped from the DifferentEnding test).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_DifferentEnding2()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsTrue(StringUtils.FindStringDifference("A simple string", "A simple", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(8, ichMin);
			Assert.AreEqual(15, ichLim1);
			Assert.AreEqual(8, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when the two
		/// strings have different endings by one extra space character.
		/// </summary>
		/// <remarks>Regression test for TE-4170</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_DifferentEnding3()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsTrue(StringUtils.FindStringDifference("A simple  ", "A simple ", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(9, ichMin);
			Assert.AreEqual(10, ichLim1);
			Assert.AreEqual(9, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when the two
		/// strings have different middles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_DifferentMiddle()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsTrue(StringUtils.FindStringDifference("ABC", "ADFC", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(1, ichMin);
			Assert.AreEqual(2, ichLim1);
			Assert.AreEqual(3, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="StringUtils.FindStringDifference"/> method when the two
		/// strings are totally different.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_DifferentEverything()
		{
			int ichMin, ichLim1, ichLim2;

			Assert.IsTrue(StringUtils.FindStringDifference("DEF", "ABC", out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(0, ichMin);
			Assert.AreEqual(3, ichLim1);
			Assert.AreEqual(3, ichLim2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test FindStringDifference when there are characters with combining diacritics.
		/// The difference should include the base character, not just the diacritic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStringDifference_DiffInDiacritics()
		{
			int ichMin, ichLim1, ichLim2;
			string s1 = "konnen";
			string s2 = "ko\u0308nnen";

			Assert.IsTrue(StringUtils.FindStringDifference(s1, s2, out ichMin,
				out ichLim1, out ichLim2));

			Assert.AreEqual(1, ichMin);
			Assert.AreEqual(2, ichLim1);
			Assert.AreEqual(3, ichLim2);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_Basic()
		{
			bool fWholeWord;

			// two equal strings
			Assert.AreEqual("Hello", StringUtils.LongestUsefulCommonSubstring("Hello", "Hello", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			// LCS at the start
			Assert.AreEqual("Hello over", StringUtils.LongestUsefulCommonSubstring("Hello over there", "Hello over here", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			// LCS in the middle
			Assert.AreEqual("want to be over", StringUtils.LongestUsefulCommonSubstring("I want to be over there",
				"You want to be over here", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			// LCS at the end
			Assert.AreEqual("visit my relatives?", StringUtils.LongestUsefulCommonSubstring("Will you come to visit my relatives?",
				"Do I ever visit my relatives?", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			// two common strings, find the longest
			Assert.AreEqual("has common words", StringUtils.LongestUsefulCommonSubstring("This sentence has common words",
				"This paragraph has common words", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			// repeated words
			Assert.AreEqual("frog frog snake frog frog", StringUtils.LongestUsefulCommonSubstring("frog frog snake frog frog frog frog",
				"frog frog frog snake frog frog", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			// nothing at all in common
			Assert.AreEqual(string.Empty, StringUtils.LongestUsefulCommonSubstring("We have nothing in common",
				"absolutely nill items", false, out fWholeWord));

			// pathological cases
			Assert.AreEqual(string.Empty, StringUtils.LongestUsefulCommonSubstring(string.Empty, string.Empty, false, out fWholeWord));
			Assert.AreEqual(string.Empty, StringUtils.LongestUsefulCommonSubstring(null, string.Empty, false, out fWholeWord));
			Assert.AreEqual(string.Empty, StringUtils.LongestUsefulCommonSubstring(string.Empty, "Hello there", false, out fWholeWord));
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Test the LongestUsefulCommonSubstring method that takes a max length
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//[Test]
		//public void LongestUsefulCommonSubstring_MaxLength()
		//{
		//    bool fWholeWord;

		//    // two equal strings
		//    Assert.AreEqual("He", StringUtils.LongestUsefulCommonSubstring("Hello", "Hello", 2, false, out fWholeWord));
		//    Assert.IsFalse(fWholeWord);

		//    // LCS at the start
		//    Assert.AreEqual("Hello", StringUtils.LongestUsefulCommonSubstring("Hello over there", "Hello over here", 8, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);

		//    // LCS in the middle
		//    Assert.AreEqual("to", StringUtils.LongestUsefulCommonSubstring("I want to be over there",
		//        "You want to be over here", 3, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);

		//    Assert.AreEqual("here", StringUtils.LongestUsefulCommonSubstring("I want to be hover here ",
		//        "You wish to be over here ", 4, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);

		//    // LCS in the middle
		//    Assert.AreEqual("to", StringUtils.LongestUsefulCommonSubstring("I want to be over there",
		//        "You want to be over here", 2, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);

		//    // LCS at the end
		//    Assert.AreEqual("visit my relatives?", StringUtils.LongestUsefulCommonSubstring("Will you come to visit my relatives?",
		//        "Do I ever visit my relatives?", "visit my relatives?".Length, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);

		//    // LCS at the end
		//    Assert.AreEqual("visit my relatives", StringUtils.LongestUsefulCommonSubstring("Will you come to visit my relatives?",
		//        "Do I ever visit my relatives?", "visit my relatives?".Length - 1, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);

		//    // LCS at the end
		//    Assert.AreEqual("my relatives?", StringUtils.LongestUsefulCommonSubstring("Will you come to visit my relatives?",
		//        "Do I ever visit my relatives?", "visit my relatives?".Length - 2, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);

		//    // LCS at the end
		//    Assert.AreEqual("visit my", StringUtils.LongestUsefulCommonSubstring("Will you come to visit my relatives?",
		//        "Do I ever visit my relatives?", "visit my".Length, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test ensures that if a useful substring
		/// contains one or more whole words that parts of adjacent words will not be included.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_AvoidPartialWordsInIsolatingLanguages()
		{
			bool fWholeWord;

			Assert.AreEqual("over here", StringUtils.LongestUsefulCommonSubstring("Jello over here",
				"Hello over here", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			Assert.AreEqual("over here", StringUtils.LongestUsefulCommonSubstring("Come over heretic over here.",
				"cover here over here", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			Assert.AreEqual("thumb", StringUtils.LongestUsefulCommonSubstring("Come over heretic thumb mover here.",
				"cover here over here thumb", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test ensures that if a useful substring
		/// contains only part of one word, the match will be returned as long as there are no
		/// whole-word matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_OnlyConsiderWholeWords()
		{
			bool fWholeWord;

			Assert.AreEqual(string.Empty, StringUtils.LongestUsefulCommonSubstring(
				"Mimamamedijoquenofueraafuera", "Mipamamedijoquenofueraalaiglesia", true, out fWholeWord));

			Assert.AreEqual("over", StringUtils.LongestUsefulCommonSubstring("Come over heretic big thumbsucker mover here.",
				"cover here over here big thumbelina", true, out fWholeWord));
			Assert.IsTrue(fWholeWord);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test ensures that if a useful substring
		/// contains only part of one word, the match will be returned as long as there are no
		/// whole-word matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_AllowPartialWordInAgglutinativeLanguages()
		{
			bool fWholeWord;

			Assert.AreEqual("amamedijoquenofueraa", StringUtils.LongestUsefulCommonSubstring(
				"Mimamamedijoquenofueraafuera", "Mipamamedijoquenofueraalaiglesia", false, out fWholeWord));
			Assert.IsFalse(fWholeWord);

			Assert.AreEqual("over", StringUtils.LongestUsefulCommonSubstring("Come over heretic big thumbsucker mover here.",
				"cover here over here big thumbelina", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test ensures that if a useful substring
		/// contains only part of one word, the match will be returned as long as there are no
		/// whole-word matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_Punctuation()
		{
			bool fWholeWord;

			Assert.AreEqual("best friends", StringUtils.LongestUsefulCommonSubstring(
				"\"best friends!\"", "best friends.", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);

			Assert.AreEqual("friends", StringUtils.LongestUsefulCommonSubstring(
				"We are best friends.", "I am the best; friends are worthless.", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test ensures that ORCs don't match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_PreventOrcMatching_WholeWordMatch()
		{
			bool fWholeWord;

			Assert.AreEqual("friends", StringUtils.LongestUsefulCommonSubstring(
				"best " + StringUtils.kszObject + " friends",
				"best " + StringUtils.kszObject + " friends", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Test the LongestUsefulCommonSubstring method. This test ensures that ORCs don't match.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//[Test]
		//public void LongestUsefulCommonSubstring_PreventOrcMatching_WholeWordMatch_MaxLength()
		//{
		//    bool fWholeWord;

		//    Assert.AreEqual("best", StringUtils.LongestUsefulCommonSubstring(
		//        "best " + StringUtils.kszObject + " friendos",
		//        "best " + StringUtils.kszObject + " friendos", 7, false, out fWholeWord));
		//    Assert.IsTrue(fWholeWord);
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test tests that leading punctuation is
		/// included in the match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_LeadingPunctuationWithOrc()
		{
			bool fWholeWord;

			Assert.AreEqual("\u00BFEntonces que\u0301", StringUtils.LongestUsefulCommonSubstring(
				"\u00BFEntonces que\u0301 " + StringUtils.kszObject + "?",
				"\u00BFEntonces que\u0301 " + StringUtils.kszObject + "?", false, out fWholeWord));
			Assert.IsTrue(fWholeWord);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test ensures that ORCs don't match
		/// when matching partial words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_PreventOrcMatching_PartialWordMatch()
		{
			bool fWholeWord;

			Assert.AreEqual(" f", StringUtils.LongestUsefulCommonSubstring(
				"floppy " + StringUtils.kszObject + " friends",
				"best " + StringUtils.kszObject + " forks", false, out fWholeWord));
			Assert.IsFalse(fWholeWord);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulCommonSubstring method. This test ensures that ORCs don't match
		/// when matching partial words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulCommonSubstring_PreventOrcMatching_PartialWordMatch_OnlyOrcsAndWhitespaceMatch()
		{
			bool fWholeWord;

			Assert.AreEqual(string.Empty, StringUtils.LongestUsefulCommonSubstring(
				StringUtils.kszObject + " " + StringUtils.kszObject + "ab?",
				StringUtils.kszObject + " " + StringUtils.kszObject + "?", false, out fWholeWord));
			Assert.IsFalse(fWholeWord);
		}
	}
}
