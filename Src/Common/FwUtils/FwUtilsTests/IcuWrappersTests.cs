// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IcuWrappersTests.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests ICU wrapper
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IcuWrappersTests
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsSymbol method.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsSymbol()
		{
			Assert.IsFalse(Icu.IsSymbol((int)'#'));
			Assert.IsFalse(Icu.IsSymbol((int)'a'));
			Assert.IsTrue(Icu.IsSymbol((int)'$'));
			Assert.IsTrue(Icu.IsSymbol((int)'+'));
			Assert.IsTrue(Icu.IsSymbol((int)'`'));
			Assert.IsTrue(Icu.IsSymbol(0x0385));
			Assert.IsTrue(Icu.IsSymbol(0x0B70));
		}
	}
}
