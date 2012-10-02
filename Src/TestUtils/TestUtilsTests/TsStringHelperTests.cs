// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TsStringHelperTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TsStringHelperTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TsStringHelperTests : BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringHelperTests"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsStringHelperTests()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for equality of two identical strings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsSame()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Test", null);
			string s;
			Assert.IsTrue(TsStringHelper.TsStringsAreEqual(strBldr.GetString(),
				strBldr.GetString(), out s));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for equality of two empty strings with no integer or string props set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsTwoEmpty_NoProps()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "", null);
			string s;
			Assert.IsTrue(TsStringHelper.TsStringsAreEqual(strBldr.GetString(),
				strBldr.GetString(), out s));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for equality of two empty strings, where expected string has an integer prop
		/// set, but actual does not
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsTwoEmpty_ExpectedIntProp()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 45);
			ITsTextProps propsS1 = propsBldr.GetTextProps();

			// Create TsString #1
			strBldr.Replace(0, 0, "", propsS1);
			ITsString tssExpected = strBldr.GetString();

			// Create TsString #2
			strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "", null);

			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsStrings differ in format of run 1.\n\t" +
				"Props differ in ktptWs property. Expected <45>, but was <-1>.",
				s, "Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for equality of two empty strings, where actual and expected strings differ
		/// in the value of an integer prop.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsTwoEmpty_IntPropsDiffer()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 45);
			ITsTextProps propsS1 = propsBldr.GetTextProps();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 15);
			ITsTextProps propsS2 = propsBldr.GetTextProps();

			// Create TsString #1
			strBldr.Replace(0, 0, "", propsS1);
			ITsString tssExpected = strBldr.GetString();

			// Create TsString #2
			strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "", propsS2);

			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsStrings differ in format of run 1.\n\t" +
				"Props differ in ktptWs property. Expected <45>, but was <15>.",
				s, "Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare a valid TsString (actual) to a null TsString (expected)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringDiffersFromNullString()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Test", null);
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(null, strBldr.GetString(),
				out s));
			Assert.AreEqual("TsStrings differ.\n\tExpected <null>, but was <Test>.", s,
				"Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare a null TsString (expected) to a valid TsString (actual)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NullStringDiffersFromTsString()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Test", null);
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(strBldr.GetString(), null,
				out s));
			Assert.AreEqual("TsStrings differ.\n\tExpected <Test>, but was <null>.", s,
				"Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for equality of two null strings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NullStringsSame()
		{
			CheckDisposed();
			string s;
			Assert.IsTrue(TsStringHelper.TsStringsAreEqual(null, null, out s));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two TsStrings having different lengths
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsDifferByLength()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Test", null);
			ITsString tssExpected = strBldr.GetString();
			strBldr.Replace(0, 0, "Bad ", null);
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsString lengths differ.\n\tExpected <Test>, but was <Bad Test>.", s,
				"Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two TsStrings having different text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsDifferByText()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Test", null);
			ITsString tssExpected = strBldr.GetString();
			strBldr.Replace(0, 4, "Crud", null);
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsString text differs.\n\tExpected <Test>, but was <Crud>.", s,
				"Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two TsStrings having different run counts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsDifferByRunCount()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Bad Test", null);
			ITsString tssExpected = strBldr.GetString();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Bogus");
			strBldr.Replace(0, 3, "Bad", propsBldr.GetTextProps());
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsStrings have different number of runs.\n\tExpected 1 runs, but was 2 runs.\n\t" +
				"Expected run 1:<Bad Test>, but was:<Bad>\n\t" +
				"Expected run 2:<>, but was:< Test>", s,
				"Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two TsStrings having different run break positions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsDifferByRunBreaks()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "S1");
			ITsTextProps propsS1 = propsBldr.GetTextProps();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "S2");
			ITsTextProps propsS2 = propsBldr.GetTextProps();

			// Create TsString #1: "Weird/Test/Dude"
			strBldr.Replace(0, 0, "Dude", propsS1);
			strBldr.Replace(0, 0, "Test", propsS2);
			strBldr.Replace(0, 0, "Weird", propsS1);
			ITsString tssExpected = strBldr.GetString();

			// Create TsString #2: "Weird/Tes/tDude"
			strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "tDude", propsS1);
			strBldr.Replace(0, 0, "Tes", propsS2);
			strBldr.Replace(0, 0, "Weird", propsS1);

			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsStrings differ in length of run 2.\n\tExpected length=4, but was length=3.\n\t" +
				"expected run:<Test>\n\t" +
				"but was:<Tes>", s,
				"Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two TsStrings having different formatting on run 2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsDifferByRunFormat()
		{
			CheckDisposed();
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Bogus");
			ITsTextProps propsBogus = propsBldr.GetTextProps();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Bogle");
			ITsTextProps propsBogle = propsBldr.GetTextProps();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Bugle");
			ITsTextProps propsBugle = propsBldr.GetTextProps();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Bungle");
			ITsTextProps propsBungle = propsBldr.GetTextProps();

			// Create TsString #1: "Weird /|Bogus{Test} /Dude"
			strBldr.Replace(0, 0, "Dude", propsBungle);
			strBldr.Replace(0, 0, "Test ", propsBogus);
			strBldr.Replace(0, 0, "Weird ", propsBugle);
			ITsString tssExpected = strBldr.GetString();

			// Create TsString #2: "Weird /|Bogle{Test} /Dude"
			strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Dude", propsBungle);
			strBldr.Replace(0, 0, "Test ", propsBogle);
			strBldr.Replace(0, 0, "Weird ", propsBugle);

			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsStrings differ in format of run 2.\n\t" +
				"Props differ in ktptNamedStyle property. Expected <Bogus>, but was <Bogle>.",
				s, "Got incorrect explanation of difference");
		}
	}
}
