// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MoreStringUtilsTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;

using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

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
			List<string> validChars = TsStringUtils.ParseCharString("ch a b c", " ", cpe,
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
			List<string> validChars = TsStringUtils.ParseCharString("ch a c", " ", cpe);
			Assert.AreEqual(2, validChars.Count);
			Assert.AreEqual("a", validChars[0]);
			Assert.AreEqual("c", validChars[1]);
		}
	}
}
