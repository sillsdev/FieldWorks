// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UcdComparerTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the UCDComparer class and UCDComparer.Compare method
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "Unit tests, gets disposed in FixtureTearDown()")]
	public class UcdComparerTest
		// can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private DebugProcs m_DebugProcs;
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
			m_DebugProcs = new DebugProcs();
			m_comparer = new UCDComparer();
			m_bidi = new BidiCharacter("0669",
				"ARABIC-INDIC DIGIT NINE;Nd;0;AN;;9;9;9;N;;;;;");
			m_pua = new PUACharacter("0669",
				"ARABIC-INDIC DIGIT NINE;Nd;0;AN;;9;9;9;N;;;;;");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			m_DebugProcs.Dispose();
			m_DebugProcs = null;
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
