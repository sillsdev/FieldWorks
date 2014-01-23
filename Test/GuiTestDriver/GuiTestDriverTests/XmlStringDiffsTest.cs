// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlStringDiffsTest.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using NUnit.Framework;

namespace GuiTestDriver
{
	[TestFixture]
	/// <summary>
	/// Summary description for XmlStringDiffsTest.
	/// </summary>
	public class XmlStringDiffsTest
	{
		public XmlStringDiffsTest()
		{
		}

		[Test]
		public void ConstructorBothNullTest()
		{
			string sBase = "";
			string sTarget = "";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			Assert.IsNotNull(xsDiff,"Constructor with null strings is null.");
		}

		[Test]
		public void ConstructorBaseNullTest()
		{
			string sBase = "";
			string sTarget = "<hi>this target is not null</hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			Assert.IsNotNull(xsDiff,"Constructor with null strings is null.");
		}

		[Test]
		public void ConstructorTargetNullTest()
		{
			string sBase = "<hi>this base is not null</hi>";
			string sTarget = "";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			Assert.IsNotNull(xsDiff,"Constructor with null strings is null.");
		}

		[Test]
		public void ConstructorTest()
		{
			string sBase = "<hi>this base is not null</hi>";
			string sTarget = "<hi>this target is not null</hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			Assert.IsNotNull(xsDiff,"Constructor with null strings is null.");
		}

		[Test]
		public void AreEqualTest()
		{
			string sBase = "<hi again = 'true'>So  what's up?<ans>The <sky color='blue'/>!</ans></hi>";
			string sTarget = "<hi again=\"true\">So what's up?<ans>The <sky color=\"blue\"/>!</ans></hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			Assert.AreEqual(true,xsDiff.AreEqual(),"Two equal xml strings fail to be identical");
		}

		[Test]
		public void AreNotEqualTest()
		{
			string sBase = "<hi again = 'false'>So  what's down?<ans>The <sky bgcolor='gray'/>!</ans></hi>";
			string sTarget = "<hi again=\"true\">So what's up?<ans>The <sky color=\"blue\"/>!</ans></hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			Assert.AreEqual(false,xsDiff.AreEqual(),"Two equal xml strings fail to be identical");
		}

		[Test]
		public void getDiffStringTest()
		{
			string sBase = "<hi again = 'true'>So  what's up?<ans>The <sky color='blue'/>!</ans></hi>";
			string sTarget = "<hi again=\"true\">So what's up?<ans>The <sky color=\"blue\"/>!</ans></hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			string diffs = xsDiff.getDiffString();
			string strExpect ="<equal/>";
			Assert.AreEqual(strExpect, diffs,"Diffgram of equal strings not correct");
		}

		[Test]
		public void getDiffStringFalseTest()
		{
			string sBase = "<hi again = 'false'>So  what's down?<ans>The <sky bgcolor='gray'/>!</ans></hi>";
			string sTarget = "<hi again=\"true\">So what's up?<ans>The <sky color=\"blue\"/>!</ans></hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			string diffs = xsDiff.getDiffString();
			string strExpect ="<xmldiff><node match=\"1\"><change match=\"@again\">true</change><change match=\"1\">So what's up?</change><node match=\"2\"><node match=\"2\"><add type=\"2\" name=\"color\">blue</add><remove match=\"@bgcolor\" /></node></node></node></xmldiff>";
			Assert.AreEqual(strExpect, diffs, "Diffgram of equal strings not correct");
		}

		[Test]
		public void DiffsEqualToExpectedTest()
		{
			string sBase = "<hi again = 'false'>So  what's down?<ans>The <sky bgcolor='gray'/>!</ans></hi>";
			string sTarget = "<hi again=\"true\">So what's up?<ans>The <sky color=\"blue\"/>!</ans></hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			string diffs = xsDiff.getDiffString();
			Assert.IsNotNull(diffs,"The base and target diffgram is null.");
			Assert.AreEqual(false,xsDiff.AreEqual());
			string strExpect = "<xmldiff><node match=\"1\"><change match=\"@again\">true</change><change match=\"1\">So what's up?</change><node match=\"2\"><node match=\"2\"><add type=\"2\" name=\"color\">blue</add><remove match=\"@bgcolor\" /></node></node></node></xmldiff>";
			XmlStringDiff xsDiffEx = new XmlStringDiff(strExpect, diffs);
			string strExpectDiff = xsDiffEx.getDiffString();
			Assert.IsNotNull(strExpectDiff,"The expected and 1st diffgram diffgram is null.");
			Assert.AreEqual(true,xsDiffEx.AreEqual());
			string strExpectical = "<equal/>";
			Assert.AreEqual(strExpectical, strExpectDiff, "Diffgram of diffgram and expected is wrong");
		}

		[Test]
		public void DiffsNotEqualToExpectedTest()
		{
			string sBase = "<hi again = 'false'>So  what's up?<ans>The <sky bgcolor='gray'/>!</ans></hi>";
			string sTarget = "<hi again=\"true\">So what's up?<ans>The <sky color=\"blue\"/>!</ans></hi>";
			XmlStringDiff xsDiff = new XmlStringDiff(sBase, sTarget);
			string diffs = xsDiff.getDiffString();
			Assert.IsNotNull(diffs,"The base and target diffgram is null.");
			Assert.AreEqual(false,xsDiff.AreEqual());
			string strExpect = "<xmldiff><node match=\"1\"><change match=\"@again\">true</change><change match=\"1\">So what's up?</change><node match=\"2\"><node match=\"2\"><add type=\"2\" name=\"color\">blue</add><remove match=\"@bgcolor\" /></node></node></node></xmldiff>";
			XmlStringDiff xsDiffEx = new XmlStringDiff(strExpect, diffs);
			string strExpectDiff = xsDiffEx.getDiffString();
			Assert.IsNotNull(strExpectDiff,"The expected and 1st diffgram diffgram is null.");
			Assert.AreEqual(false,xsDiffEx.AreEqual());
			string strExpectical = "<equal/>";
			Assert.IsTrue(!strExpectical.Equals(strExpectDiff));
		}
	}
}
