// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MarshalExTests
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
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
		/// <summary/>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			// Set stub for messagebox so that we don't pop up a message box when running tests.
			MessageBoxUtils.Manager.SetMessageBoxAdapter(new MessageBoxStub());
		}

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
