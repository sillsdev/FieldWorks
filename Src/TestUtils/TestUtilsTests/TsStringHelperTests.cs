// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TsStringHelperTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

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
		/// Test for equality of two identical strings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsSame()
		{
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Test", TsPropsFactoryClass.Create().MakeProps(null, 5, 0));
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 45);
			ITsTextProps propsS2 = propsBldr.GetTextProps();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, 1);
			ITsTextProps propsS1 = propsBldr.GetTextProps();

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
				"TsStrings differ in format of run 1." + Environment.NewLine +
				"\tProps differ in intProp type " + (int)FwTextPropType.ktptSuperscript + ". Expected <1,3>, but was <-1,-1>.",
				s, "Got incorrect explanation of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for equality of two empty strings, where actual and expected strings differ
		/// in the value of the writing system (int) prop.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TsStringsTwoEmpty_WritingSystemsDiffer()
		{
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
				"TsStrings differ in format of run 1." + Environment.NewLine +
				"\tProps differ in ktptWs property. Expected ws <45> and var <0>, but was ws <15> and var <0>.",
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsFactory propsFact = TsPropsFactoryClass.Create();
			strBldr.Replace(0, 0, "Test", propsFact.MakeProps(null, 5, 0));
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(null, strBldr.GetString(),
				out s));
			Assert.AreEqual("TsStrings differ." + Environment.NewLine + "\tExpected <null>, but was <Test>.", s,
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsFactory propsFact = TsPropsFactoryClass.Create();
			strBldr.Replace(0, 0, "Test", propsFact.MakeProps(null, 5, 0));
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(strBldr.GetString(), null,
				out s));
			Assert.AreEqual("TsStrings differ." + Environment.NewLine + "\tExpected <Test>, but was <null>.", s,
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsFactory propsFact = TsPropsFactoryClass.Create();
			ITsTextProps props = propsFact.MakeProps(null, 5, 0);
			strBldr.Replace(0, 0, "Test", props);
			ITsString tssExpected = strBldr.GetString();
			strBldr.Replace(0, 0, "Bad ", props);
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsString lengths differ." + Environment.NewLine + "\tExpected <Test>, but was <Bad Test>.", s,
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsFactory propsFact = TsPropsFactoryClass.Create();
			ITsTextProps props = propsFact.MakeProps(null, 5, 0);
			strBldr.Replace(0, 0, "Test", props);
			ITsString tssExpected = strBldr.GetString();
			strBldr.Replace(0, 4, "Crud", props);
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				"TsString text differs." + Environment.NewLine + "\tExpected <Test>, but was <Crud>.", s,
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsFactory propsFact = TsPropsFactoryClass.Create();
			strBldr.Replace(0, 0, "Bad Test", propsFact.MakeProps(null, 5, 0));
			ITsString tssExpected = strBldr.GetString();
			strBldr.Replace(0, 3, "Bad", propsFact.MakeProps("Bogus", 5, 0));
			string s;
			Assert.IsFalse(TsStringHelper.TsStringsAreEqual(tssExpected, strBldr.GetString(),
				out s));
			Assert.AreEqual(
				string.Format("TsStrings have different number of runs.{0}\tExpected 1 runs, but was 2 runs.{0}\t" +
				"Expected run 1:<Bad Test>, but was:<Bad>{0}\t" +
				"Expected run 2:<>, but was:< Test>", Environment.NewLine), s,
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 1);
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
				string.Format("TsStrings differ in length of run 2.{0}\tExpected length=4, but was length=3.{0}\t" +
				"expected run:<Test>{0}\t" +
				"     but was:<Tes>", Environment.NewLine), s,
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
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 45);
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
				"TsStrings differ in format of run 2." + Environment.NewLine +
				"\tProps differ in ktptNamedStyle property. Expected <Bogus>, but was <Bogle>.",
				s, "Got incorrect explanation of difference");
		}
	}
}
