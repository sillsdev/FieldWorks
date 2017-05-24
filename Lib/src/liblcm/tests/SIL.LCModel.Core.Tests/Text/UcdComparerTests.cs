// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.LCModel.Core.Text
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the UCDComparer class and UCDComparer.Compare method
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class UcdComparerTest
	{
		private BidiCharacter m_bidi;
		private UCDComparer m_comparer;
		private PUACharacter m_pua;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the up the test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void SetUp()
		{
			m_comparer = new UCDComparer();
			m_bidi = new BidiCharacter("0669",
				"ARABIC-INDIC DIGIT NINE;Nd;0;AN;;9;9;9;N;;;;;");
			m_pua = new PUACharacter("0669",
				"ARABIC-INDIC DIGIT NINE;Nd;0;AN;;9;9;9;N;;;;;");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests UCDComparer.Compare when passed in a UcdCharacter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UcdWithUcd()
		{
			BidiCharacter otherBidi = m_bidi;
			Assert.AreEqual(0, m_comparer.Compare(m_bidi, otherBidi));

			otherBidi = new BidiCharacter("0669",
				"ARABIC-INDIC DIGIT NINE;Nd;0;AN;;9;9;9;N;;;;;");
			Assert.AreEqual(0, m_comparer.Compare(m_bidi, otherBidi));

			otherBidi = new BidiCharacter("004F", "LATIN CAPITAL LETTER O;Lu;0;L;;;;;N;;;;006F;");
			Assert.Less(m_comparer.Compare(m_bidi, otherBidi), 0);

			otherBidi = new BidiCharacter("0668",
				"ARABIC-INDIC DIGIT EIGHT;Nd;0;AN;;8;8;8;N;;;;;");
			Assert.Greater(m_comparer.Compare(m_bidi, otherBidi), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UCDComparer.Compare method when passed in string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UcdWithString()
		{
			Assert.AreEqual(0, m_comparer.Compare(m_bidi, "AN")); // region
			Assert.AreEqual(0, m_comparer.Compare("AN", m_bidi)); // region
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests UCDComparer.Compare when passed in a PuaCharacter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PuaWithPua()
		{
			PUACharacter otherPua = m_pua;
			Assert.AreEqual(0, m_comparer.Compare(m_pua, otherPua));

			otherPua = new PUACharacter("0669",
				"ARABIC-INDIC DIGIT NINE;Nd;0;AN;;9;9;9;N;;;;;");
			Assert.AreEqual(0, m_comparer.Compare(m_pua, otherPua));

			otherPua = new PUACharacter("004F", "LATIN CAPITAL LETTER O;Lu;0;L;;;;;N;;;;006F;");
			Assert.Greater(m_comparer.Compare(m_pua, otherPua), 0);

			otherPua = new PUACharacter("0668",
				"ARABIC-INDIC DIGIT EIGHT;Nd;0;AN;;8;8;8;N;;;;;");
			Assert.Greater(m_comparer.Compare(m_pua, otherPua), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UCDComparer.Compare method when passed in a string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PuaWithString()
		{
			Assert.Less(m_comparer.Compare(m_pua, "066A"), 0);
			Assert.Less(m_comparer.Compare(m_pua, "066a"), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests mixing UCD and PUA characters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UcdWithPua()
		{
			Assert.AreEqual(0, m_comparer.Compare(m_pua, m_bidi));
			Assert.AreEqual(0, m_comparer.Compare(m_bidi, m_pua));

			PUACharacter otherPua;
			otherPua = new PUACharacter("004F", "LATIN CAPITAL LETTER O;Lu;0;L;;;;;N;;;;006F;");
			Assert.Greater(m_comparer.Compare(m_bidi, otherPua), 0);

			BidiCharacter otherBidi;
			otherBidi = new BidiCharacter("004F", "LATIN CAPITAL LETTER O;Lu;0;L;;;;;N;;;;006F;");
			Assert.Greater(m_comparer.Compare(m_pua, otherBidi), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests calling Compare() with two strings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void StringWithString()
		{
			// The behavior used to be:
			//Assert.AreEqual(0, m_comparer.Compare("004F", "004F")); // strings are equal, no exception
			//m_comparer.Compare("004F", "004f"); // throws an exception

			// This doesn't make to much sense, so we now throw an exception in all cases
			m_comparer.Compare("004F", "004F");  // throws an exception
		}
	}
}
