// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Tests the CharEnumeratorForByteArray class
	/// </summary>
	[TestFixture]
	public class CharEnumeratorForByteArrayTests
	{
		/// <summary>
		/// Tests enumeration of an empty byte array
		/// </summary>
		[Test]
		public void EnumNoBytes()
		{
			var array = new CharEnumeratorForByteArray(new byte[0]);
			if (array.Any())
			{
				Assert.Fail("Shouldn't have gotten any characters for an empty byte array.");
			}
		}

		/// <summary>
		/// Tests enumeration of a byte array containing an odd number of bytes
		/// </summary>
		[Test]
		public void EnumOddBytes()
		{
			var array = new CharEnumeratorForByteArray(new byte[] { 0x01, 0x02, 0x03 });
			var expected = new[] { '\u0201', '\u0003' };
			var i = 0;
			foreach (var ch in array)
			{
				Assert.AreEqual(expected[i++], ch);
			}
			Assert.AreEqual(2, i);
		}

		/// <summary>
		/// Tests enumeration of a byte array containing an even number of bytes
		/// </summary>
		[Test]
		public void EnumEvenBytes()
		{
			var array = new CharEnumeratorForByteArray(new byte[] { 0x01, 0x02, 0x03, 0x06 });
			var expected = new[] { '\u0201', '\u0603' };
			var i = 0;
			foreach (var ch in array)
			{
				Assert.AreEqual(expected[i++], ch);
			}
			Assert.AreEqual(2, i);
		}

		/// <summary>
		/// Tests enumeration of a byte array containing an odd number of bytes
		/// </summary>
		[ExpectedException(typeof(ArgumentNullException))]
		[Test]
		public void NullConstructor()
		{
			var array = new CharEnumeratorForByteArray(null);
		}
	}
}