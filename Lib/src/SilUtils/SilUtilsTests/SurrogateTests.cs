// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SurrogateTests.cs
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;

namespace SIL.Utils
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
		/// Tests the Surrogates util class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestSurrogates()
		{
			Assert.AreEqual(1, Surrogates.NextChar("ab\xD800\xDC00c", 0));
			Assert.AreEqual(2, Surrogates.NextChar("ab\xD800\xDC00c", 1));
			Assert.AreEqual(4, Surrogates.NextChar("ab\xD800\xDC00c", 2));
			Assert.AreEqual(5, Surrogates.NextChar("ab\xD800\xDC00c", 4));
			Assert.AreEqual(4, Surrogates.NextChar("ab\xD800\xDC00", 2));
			Assert.AreEqual(3, Surrogates.NextChar("ab\xD800", 2), "Badly formed pair at end...don't go too far");
			Assert.AreEqual(3, Surrogates.NextChar("ab\xD800c", 2), "Badly formed pair in middle...don't go too far");
			Assert.AreEqual(0, Surrogates.PrevChar("ab\xD800\xDC00c", 1));
			Assert.AreEqual(1, Surrogates.PrevChar("ab\xD800\xDC00c", 2));
			Assert.AreEqual(2, Surrogates.PrevChar("ab\xD800\xDC00c", 3), "initial ich at a bad position, move back normally to sync");
			Assert.AreEqual(2, Surrogates.PrevChar("ab\xD800\xDC00c", 4), "double move succeeds");
			Assert.AreEqual(4, Surrogates.PrevChar("ab\xD800\xDC00c", 5));
			Assert.AreEqual(3, Surrogates.PrevChar("ab\xD800c", 4), "no double move on bad pair");
			Assert.AreEqual(0, Surrogates.PrevChar("\xD800\xDC00c", 2), "double move succeeds at start (and end)");
			Assert.AreEqual(0, Surrogates.PrevChar("\xDC00c", 1), "no double move on bad trailer at start");
			Assert.AreEqual(0, Surrogates.PrevChar("\xD800c", 1), "no double move on bad leader at start");
		}
	}
}
