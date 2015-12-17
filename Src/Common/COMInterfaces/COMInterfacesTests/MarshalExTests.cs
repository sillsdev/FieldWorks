// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MarshalExTests
// Responsibility:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description of MarshalExTests class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MarshalExTests // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MarshalEx.UShortToString method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UShortToString()
		{
			ushort[] source = new ushort[] { (ushort)'a', (ushort)'b', (ushort)'c', (ushort)0,
				(ushort)'x', (ushort)'y', (ushort)'z' };

			string str = MarshalEx.UShortToString(source);
			Assert.AreEqual("abc", str);
		}
	}
}
