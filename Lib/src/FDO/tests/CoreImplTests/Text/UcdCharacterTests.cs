// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.CoreImpl.Text
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class UcdCharacterTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CompareTo() method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareTo()
		{
			UCDCharacter ucd = new BidiCharacter("004F", "LATIN CAPITAL LETTER O;Lu;0;L;;;;;N;;;;006F;");
			Assert.AreEqual(0, ucd.CompareTo("L"));
			Assert.Greater(0, ucd.CompareTo("R"));
			Assert.Less(0, ucd.CompareTo("AL"));
		}
	}
}
