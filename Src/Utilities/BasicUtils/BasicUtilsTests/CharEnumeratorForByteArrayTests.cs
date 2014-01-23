// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CharEnumeratorForByteArrayTests.cs
// Responsibility: TE Team

using System;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the CharEnumeratorForByteArray class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CharEnumeratorForByteArrayTests
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests enumeration of an empty byte array
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void EnumNoBytes()
		{
			CharEnumeratorForByteArray array = new CharEnumeratorForByteArray(new byte[0]);
			foreach (char ch in array)
				Assert.Fail("Shouldn't have gotten any characters for an empty byte array.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests enumeration of a byte array containing an odd number of bytes
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void EnumOddBytes()
		{
			CharEnumeratorForByteArray array = new CharEnumeratorForByteArray(new byte[] { 0x01, 0x02, 0x03 });
			char[] expected = new char[] { '\u0201', '\u0003' };
			int i = 0;
			foreach (char ch in array)
				Assert.AreEqual(expected[i++], ch);
			Assert.AreEqual(2, i);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests enumeration of a byte array containing an even number of bytes
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void EnumEvenBytes()
		{
			CharEnumeratorForByteArray array = new CharEnumeratorForByteArray(new byte[] { 0x01, 0x02, 0x03, 0x06 });
			char[] expected = new char[] { '\u0201', '\u0603' };
			int i = 0;
			foreach (char ch in array)
				Assert.AreEqual(expected[i++], ch);
			Assert.AreEqual(2, i);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests enumeration of a byte array containing an odd number of bytes
		/// </summary>
		///--------------------------------------------------------------------------------------
		[ExpectedException(typeof(ArgumentNullException))]
		[Test]
		public void NullConstructor()
		{
			CharEnumeratorForByteArray array = new CharEnumeratorForByteArray(null);
		}
	}
}
