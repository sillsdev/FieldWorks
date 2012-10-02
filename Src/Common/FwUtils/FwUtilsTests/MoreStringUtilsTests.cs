// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2006' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MoreStringUtilsTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// More tests for the StringUtils class (could not be done in COMInterfacesTests because
	/// of additional dependencies).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MoreStringUtilsTests : BaseTest
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

		#region Tests moved from StringUtilsTests because they rely on stuff that isn't in COMInterfaces
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharString method works when when passed a string of space-
		/// delimited letters that contains an illegal digraph
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ParseCharString_BogusDigraph()
		{
			DummyCharPropEngine cpe = new DummyCharPropEngine();
			List<string> invalidChars;
			List<string> validChars = StringUtils.ParseCharString("ch a b c", " ", cpe,
				out invalidChars);
			Assert.AreEqual(3, validChars.Count);
			Assert.AreEqual("a", validChars[0]);
			Assert.AreEqual("b", validChars[1]);
			Assert.AreEqual("c", validChars[2]);
			Assert.AreEqual(1, invalidChars.Count);
			Assert.AreEqual("ch", invalidChars[0]);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharString method works when passed a string of space-
		/// delimited letters that contains an illegal digraph in the mode where we ignore
		/// bogus characters (i.e. when we don't pass an empty list of invalid characters).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ParseCharString_IgnoreDigraph()
		{
			DummyCharPropEngine cpe = new DummyCharPropEngine();
			List<string> validChars = StringUtils.ParseCharString("ch a c", " ", cpe);
			Assert.AreEqual(2, validChars.Count);
			Assert.AreEqual("a", validChars[0]);
			Assert.AreEqual("c", validChars[1]);
		}
		#endregion
	}
}
