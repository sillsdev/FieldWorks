// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AlphaOutlineTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AlphaOutlineTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NumToAlphaOutline method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NumToAlphaOutline()
		{
			Assert.AreEqual("A", AlphaOutline.NumToAlphaOutline(1));
			Assert.AreEqual("Z", AlphaOutline.NumToAlphaOutline(26));
			Assert.AreEqual("AA", AlphaOutline.NumToAlphaOutline(27));
			Assert.AreEqual("ZZ", AlphaOutline.NumToAlphaOutline(52));
			Assert.AreEqual("AAA", AlphaOutline.NumToAlphaOutline(53));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AlphaToOutlineNum method with valid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AlphaToOutlineNum_Valid()
		{
			Assert.AreEqual(1, AlphaOutline.AlphaOutlineToNum("A"));
			Assert.AreEqual(1, AlphaOutline.AlphaOutlineToNum("a"));
			Assert.AreEqual(26, AlphaOutline.AlphaOutlineToNum("Z"));
			Assert.AreEqual(27, AlphaOutline.AlphaOutlineToNum("AA"));
			Assert.AreEqual(52, AlphaOutline.AlphaOutlineToNum("ZZ"));
			Assert.AreEqual(53, AlphaOutline.AlphaOutlineToNum("AAA"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AlphaToOutlineNum method with invalid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AlphaToOutlineNum_Invalid()
		{
			Assert.AreEqual(-1, AlphaOutline.AlphaOutlineToNum(string.Empty));
			Assert.AreEqual(-1, AlphaOutline.AlphaOutlineToNum(null));
			Assert.AreEqual(-1, AlphaOutline.AlphaOutlineToNum("7"));
			Assert.AreEqual(-1, AlphaOutline.AlphaOutlineToNum("A1"));
			Assert.AreEqual(-1, AlphaOutline.AlphaOutlineToNum("AB"));
			Assert.AreEqual(-1, AlphaOutline.AlphaOutlineToNum("AAC"));
			Assert.AreEqual(-1, AlphaOutline.AlphaOutlineToNum("?"));
		}
	}
}
