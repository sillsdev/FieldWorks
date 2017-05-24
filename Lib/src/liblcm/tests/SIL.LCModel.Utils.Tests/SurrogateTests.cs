// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SurrogateTests.cs
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;

namespace SIL.LCModel.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the the Surrogates util class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SurrogateTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Surrogates.NextChar() method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestCase("ab\xD800\xDC00c", 0, Result = 1)]
		[TestCase("ab\xD800\xDC00c", 1, Result = 2)]
		[TestCase("ab\xD800\xDC00c", 2, Result = 4)]
		[TestCase("ab\xD800\xDC00c", 4, Result = 5)]
		[TestCase("ab\xD800\xDC00", 2, Result = 4)]
		public int NextChar(string st, int ich)
		{
			return Surrogates.NextChar(st, ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Surrogates.NextChar() method with invalid data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestCase(new[] { 'a', 'b', '\xD800'}, 2, Result = 3)] // Badly formed pair at end...don't go too far
		[TestCase(new[] { 'a', 'b', '\xD800', 'c' }, 2, Result = 3)] // Badly formed pair in middle...don't go too far
		public int NextChar_InvalidData(char[] st, int ich)
		{
			// NUnit doesn't allow tests with invalid strings in a testcase, so we create the
			// string from the character array when running the test
			return Surrogates.NextChar(new string(st), ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Surrogates.PrevChar() method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestCase("ab\xD800\xDC00c", 1, Result = 0)]
		[TestCase("ab\xD800\xDC00c", 2, Result = 1)]
		[TestCase("ab\xD800\xDC00c", 5, Result = 4)]
		[TestCase("ab\xD800\xDC00c", 3, Result = 2)] // initial ich at a bad position, move back normally to sync
		[TestCase("ab\xD800\xDC00c", 4, Result = 2)] // double move succeeds
		[TestCase("\xD800\xDC00c", 2, Result = 0)] // double move succeeds at start (and end)
		public int PrevChar(string st, int ich)
		{
			return Surrogates.PrevChar(st, ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Surrogates.PrevChar() method with invalid input
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestCase(new[] { 'a', 'b', '\xD800', 'c' }, 4, Result = 3)] // no double move on bad pair
		[TestCase(new[] { '\xDC00', 'c' }, 1, Result = 0)] // no double move on bad trailer at start
		[TestCase(new[] { '\xD800', 'c' }, 1, Result = 0)] // no double move on bad leader at start
		public int PrevChar_InvalidChar(char[] st, int ich)
		{
			// NUnit doesn't allow tests with invalid strings in a testcase, so we create the
			// string from the character array when running the test
			return Surrogates.PrevChar(new string(st), ich);
		}
	}
}
