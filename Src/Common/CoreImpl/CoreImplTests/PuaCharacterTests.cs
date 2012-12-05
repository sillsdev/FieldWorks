// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LanguageDefinitionTest.cs
// Responsibility: Erik Freund, Tres London, Zachariah Yoder
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// These test test the PuaCharacterDlg dialog and the PuaCharacter tab on the
	/// WritingSystemPropertiesDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "Unit tests, gets disposed in FixtureTearDown()")]
	public class PuaCharacterTests
		// can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private DebugProcs m_DebugProcs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			m_DebugProcs = new DebugProcs();
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
		/// Tests the compareHex method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PuaCharacterCompareHex()
		{
			AssertComparisonWorks("E","E",0);
			AssertComparisonWorks("100A","1009",1);
			AssertComparisonWorks("1001","10001",-1);
			AssertComparisonWorks("01","1",0);
			AssertComparisonWorks("0001","1",0);
			AssertComparisonWorks("000E","E",0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Asserts the comparison works.
		/// </summary>
		/// <param name="hex1">The hex1.</param>
		/// <param name="hex2">The hex2.</param>
		/// <param name="expectedComparison">The expected comparison.</param>
		/// ------------------------------------------------------------------------------------
		private void AssertComparisonWorks(string hex1, string hex2, int expectedComparison)
		{
			int comparison;
			comparison = MiscUtils.CompareHex(hex1, hex2);
			string NL = Environment.NewLine;
			Assert.AreEqual(expectedComparison, comparison, "CompareHex did not compare correctly:" + NL +
				"values: " + hex1 + " ? " + hex2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UnicodeData.txt style "ToString" method of PUACharacter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PuaCharacterToString()
		{
			string unicodeData =
				"VULGAR FRACTION ONE THIRD;No;0;ON;<fraction> 0031 2044 0033;;;1/3;N;FRACTION ONE THIRD;;;;";
			PUACharacter puaChar = new PUACharacter("2153",unicodeData);
			Assert.AreEqual("2153;" + unicodeData, puaChar.ToString(), "Error while writing PUACharacter");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the pua character text constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PuaCharacterTextConstructor()
		{
			PUACharacter puaCharacter = new PUACharacter("0669", "ARABIC-INDIC DIGIT NINE;Nd;0;AN;;9;9;9;N;;;;;");
		}
	}
}
