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
// File: PhonEnvValidatorTest.cs
// Responsibility: AndyBlack
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.Validation
{
	[TestFixture]
	public class PhonEnvRecognizerTest: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		PhonEnvRecognizer m_per;
		string[] m_saSegments = { "a", "ai", "b", "c", "d", "e", "f", "fl", "fr",
									"a", // test duplicate
									"\u00ED",  // single combined Unicode acute i (í)
									"H"
								};
		string[] m_saNaturalClasses = { "V", "Vowels", "C", "+son", "C", "+lab, +vd", "+ant, -cor, -vd" };

		public PhonEnvRecognizerTest()
		{
		}

		/// <summary>
		/// This method is called once, before any test is run.
		/// </summary>
		[TestFixtureSetUp]
		public virtual void FixtureSetUp()
		{
			m_per = new PhonEnvRecognizer(m_saSegments, m_saNaturalClasses);
		}

		/// <summary>
		/// Test that can recognize a valid phonlogical environment
		/// </summary>
		[Test]
		public void ValidEnvironments()
		{
			DoValidTest("/ # _");   // word boundary
			DoValidTest("/ _ #");
			DoValidTest("/ # _ #");
			DoValidTest("/ # _ a");
			DoValidTest("/ a _ #");
			DoValidTest("/ a _");   // segment
			DoValidTest("/ _ b");
			DoValidTest("/ a _ b");
			DoValidTest("/ a b _ ai d #");
			DoValidTest("/ a b _");   // segments
			DoValidTest("/ _ a b");
			DoValidTest("/ a b _ a b");
			DoValidTest("/ ab _");   // segments without intervening spaces
			DoValidTest("/ _ ab");
			DoValidTest("/ ab _ ab");
			DoValidTest("/ flaid _");
			DoValidTest("/ _ flaid");
			DoValidTest("/ flaid _ flaid");
			DoValidTest("/ (a) b _");  // optionality
			DoValidTest("/ _ (a) b");
			DoValidTest("/(c) d _ (a) b");
			DoValidTest("/ (f) (fl) _ ");
			DoValidTest("/ _ (d) (c)");
			DoValidTest("/ (f) (fl) _ (d) (c)");
			DoValidTest("/ [V] _ ");   // Natural Classes
			DoValidTest("/ _ [V]");
			DoValidTest("/ [V] _ [V]");
			DoValidTest("/ [+lab, +vd] _ ");   // Natural Classes with spaces in the name
			DoValidTest("/ _ [+lab, +vd]");
			DoValidTest("/ [+lab, +vd] _ [+lab, +vd]");
			DoValidTest("/ [+lab,  +vd] _ ");
			DoValidTest("/ _ [+lab,  +vd]");
			DoValidTest("/ [+lab,  +vd] _ [+lab,  +vd]");
			DoValidTest("/ [+ant, -cor, -vd] _ ");
			DoValidTest("/ _ [+ant, -cor, -vd]");
			DoValidTest("/ [+ant, -cor, -vd] _ [+ant, -cor, -vd]");
			DoValidTest("/ ([C]) [V] _ ");   // Natural Classes with optionality
			DoValidTest("/ _ ([C]) [V]");
			DoValidTest("/ [V] ([C]) _ ([C]) [V]");
			DoValidTest("/# ([V]) b fr [C] _ a (c) #");  // Combo
			DoValidTest("/ _ [C^1]");   // Reduplication
			DoValidTest("/ _ [V^1]");
			DoValidTest("/ _ [C^1][V^1]");
			DoValidTest("/ _ [C^1] [V^1]");
			DoValidTest("/ \u00ED _"); // single combined Unicode acute i (í)
			DoValidTest("/ i\u0301 _"); // Unicode i followed by combining acute (í)
			DoValidTest("/ H _");
		}
		/// <summary>
		/// Test that can recognize a valid phonlogical environment
		/// </summary>
		[Test]
		public void InvalidEnvironments()
		{
			DoInvalidTest("/ /_", "two slashes", 3);
			DoInvalidTest("/ a _ _", "two underscores", 7);
			DoInvalidTest("/ _ _ a", "two underscores", 5);
			DoInvalidTest("/ # #_", "two word boundaries before underscore", 5);
			DoInvalidTest("/ _##", "two word boundaries after underscore", 5);
			DoInvalidTest("/ _# a", "letter after word boundary", 6);
			DoInvalidTest("/a # _", "letter before word boundary", 4);
			DoInvalidTest("/ _# _ #", "two word boundaries and two underscores", 6);
			DoInvalidTest("/ (a _", "missing closing paren before underscore", 5);
			DoInvalidTest("/ a) _", "missing opening paren before underscore", 2	);
			DoInvalidTest("/ _ (a", "missing closing paren after underscore", 0);
			DoInvalidTest("/ _ a)", "missing opening paren after underscore", 4);
			DoInvalidTest("/ [C _", "missing closing bracket before underscore", 5);
			DoInvalidTest("/  C] _", "missing opening bracket before underscore", 3);
			DoInvalidTest("/ _ [C", "missing closing bracket after underscore", 0);
			DoInvalidTest("/ _ C]", "missing opening bracket after underscore", 4);
			DoInvalidTest("/ chr _", "chr not in segment list (before underscore)", 3);
			DoInvalidTest("/ _ chr", "chr not in segment list (after underscore)", 5);
			DoInvalidTest("/ frage _", "g is not in segment list (before underscore)", 6);
			DoInvalidTest("/ _ frage", "g is not in segment list (after underscore)", 8);
			DoInvalidTest("/ [+lab] _", "+lab not in class list (before underscore)", 3);
			DoInvalidTest("/ _ [+lab]", "+lab not in class list (after underscore)", 5);
			DoInvalidTest("/ [+lab, -vd] _", "+lab, -vd not in class list (before underscore)", 3);
			DoInvalidTest("/ _ [+lab, -vd]", "+lab, -vd not in class list (after underscore)", 5);
			DoInvalidTest("/ [+lab, +vd, -cor] _", "+lab, -vd not in class list (before underscore)", 3);
			DoInvalidTest("/ _ [+lab, +vd, -cor]", "+lab, -vd not in class list (after underscore)", 5);
			DoInvalidTest("/ _ [C^]", "wedge used as part of a class name: C^", 5);
			DoInvalidTest("/ _ [C^C]", "wedge used as part of a class name: C^C", 5);
			DoInvalidTest("/ _ [C^1C]", "wedge used as part of a class name: C^1C", 5);
			DoInvalidTest("/ _ [X][Y]", "X not in class list", 5);
		}
		void DoValidTest(string sEnv)
		{
			Assert.IsTrue(m_per.Recognize(sEnv), sEnv + " failed");
		}
		void DoInvalidTest(string sEnv, string sFailedPortion)
		{
			Assert.IsFalse(m_per.Recognize(sEnv), sEnv + " should fail: " + sFailedPortion);
		}
		void DoInvalidTest(string sEnv, string sFailedPortion, int iExpectedPos)
		{
			Assert.IsFalse(m_per.Recognize(sEnv), sEnv + " should fail: " + sFailedPortion);
			string sPos = m_per.ErrorMessage;
			int i = sPos.IndexOf("pos=");
			if (i > -1)
			{
				string s = sPos.Substring(i+5);
				int j = s.IndexOf('"');
				int iPos = Convert.ToInt32(s.Substring(0, j));
				Assert.AreEqual(iExpectedPos, iPos, "Different position: for " + sFailedPortion);
			}
		}
		[Test]
		public void RetrieveErrorMessage()
		{
			// invalid segment
			DoInvalidTest("/ _ chr", "chr not in segment list");
			Assert.AreEqual("<phonEnv status=\"segment\" pos=\"5\">/ _ chr</phonEnv>",
				m_per.ErrorMessage,
				"Invalid segment Error Message returned incorrectly");
			// invalid natural class
			DoInvalidTest("/ _ [+lab]", "+lab not in class list");
			Assert.AreEqual("<phonEnv status=\"class\" pos=\"5\">/ _ [+lab]</phonEnv>",
				m_per.ErrorMessage,
				"Invalid class Error Message returned incorrectly");
			// invalid segment
			DoInvalidTest("/ _ a _", "cannot have two underscores");
			Assert.AreEqual("<phonEnv status=\"syntax\" pos=\"7\" syntaxErrType=\"unknown\">/ _ a _</phonEnv>",
				m_per.ErrorMessage,
				"Syntax Error Message returned incorrectly");
			DoInvalidTest("/ (a _", "missing closing paren before underscore");
			Assert.AreEqual("<phonEnv status=\"missingClosingParen\" pos=\"5\">/ (a _</phonEnv>",
				m_per.ErrorMessage,
				"Syntax Error Message returned incorrectly");
			DoInvalidTest("/ a) _", "missing opening paren before underscore");
			Assert.AreEqual("<phonEnv status=\"missingOpeningParen\" pos=\"2\">/ a) _</phonEnv>",
				m_per.ErrorMessage,
				"Syntax Error Message returned incorrectly");
			DoInvalidTest("/ [C _", "missing closing bracket before underscore");
			Assert.AreEqual("<phonEnv status=\"missingClosingSquareBracket\" pos=\"5\">/ [C _</phonEnv>",
				m_per.ErrorMessage,
				"Syntax Error Message returned incorrectly");
			DoInvalidTest("/  C] _", "missing opening bracket before underscore");
			Assert.AreEqual("<phonEnv status=\"missingOpeningSquareBracket\" pos=\"3\">/  C] _</phonEnv>",
				m_per.ErrorMessage,
				"Syntax Error Message returned incorrectly");
		}
		/// <summary>
		/// Test that can correclty recognize or fail sequences of valid and invalid phonlogical environments
		/// (tests init state)
		/// </summary>
		[Test]
		public void ValidInvalidCombos()
		{
			DoValidTest("/ # _");   // word boundary
			DoInvalidTest("/ /_", "two slashes");
			DoValidTest("/ _ #");
			DoInvalidTest("/ a _ _", "two underscores");
			DoValidTest("/ # _ #");
		}
	}
}
