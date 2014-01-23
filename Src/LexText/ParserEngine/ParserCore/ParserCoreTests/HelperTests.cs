// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: HelperTests.cs
// Responsibility: AndyBlack

using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class HelperTests : BaseTest
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method Foo.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertNameToUseAnsiCharactersTest()
		{
			// plain, simple ASCII
			string name = "abc 123";
			string convertedName = ParserHelper.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("abc 123", convertedName);
			// Using upper ANSI characters as well as ASCII
			name = "ÿýúadctl";
			convertedName = ParserHelper.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("ÿýúadctl", convertedName);
			// Using characters just above ANSI as well as ASCII
			name = "ąćălex";
			convertedName = ParserHelper.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("010501070103lex", convertedName);
			// Using Cyrillic characters as well as ASCII
			name = "Английский для семинараgram";
			convertedName = ParserHelper.ConvertNameToUseAnsiCharacters(name);
			Assert.AreEqual("0410043D0433043B043804390441043A04380439 0434043B044F 04410435043C0438043D043004400430gram", convertedName);
		}
	}
}
