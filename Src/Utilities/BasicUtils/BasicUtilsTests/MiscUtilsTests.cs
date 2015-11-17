// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary>
	/// Test (some of) the routines in MiscUtils
	/// </summary>
	[TestFixture]
	public class MiscUtilsTests
	{
		/// <summary>
		/// Test the indicated routine
		/// </summary>
		[Test]
		public void NumbersAlphabeticKey()
		{
			Assert.That(MiscUtils.NumbersAlphabeticKey(""), Is.EqualTo(""));
			Assert.That(MiscUtils.NumbersAlphabeticKey("abc"), Is.EqualTo("abc"));
			Assert.That(MiscUtils.NumbersAlphabeticKey("1"), Is.EqualTo("0000000001"));
			Assert.That(MiscUtils.NumbersAlphabeticKey("a1"), Is.EqualTo("a0000000001"));
			Assert.That(MiscUtils.NumbersAlphabeticKey("a1b"), Is.EqualTo("a0000000001b"));
			Assert.That(MiscUtils.NumbersAlphabeticKey("1b"), Is.EqualTo("0000000001b"));
			Assert.That(MiscUtils.NumbersAlphabeticKey("12"), Is.EqualTo("0000000012"));
			Assert.That(MiscUtils.NumbersAlphabeticKey("1.2.3"), Is.EqualTo("0000000001.0000000002.0000000003"));
			// This is really the point!
			Assert.That(MiscUtils.NumbersAlphabeticKey("12"), Is.GreaterThan(MiscUtils.NumbersAlphabeticKey("2")));
			Assert.That(MiscUtils.NumbersAlphabeticKey("12.12abcd"), Is.GreaterThan(MiscUtils.NumbersAlphabeticKey("12.2abcd")));
		}
	}
}
