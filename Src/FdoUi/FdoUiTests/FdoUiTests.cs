// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoUiTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.FdoUi
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoUiTests: InMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DisplayOrCreateEntry method with an empty string (TE-5916)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DisplayOrCreateEntry_EmptyString()
		{
			ITsStrBldr emptyString = TsStrBldrClass.Create();
			Cache.MainCacheAccessor.SetString(1, 2, emptyString.GetString());
			// We shouldn't get an exception if we call DisplayOrCreateEntry with an empty string
			LexEntryUi.DisplayOrCreateEntry(Cache, 1, 2, 3, 0, 0, null, null, null, null);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FindEntryForWordform with empty string (related to TE-5916)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindEntryForWordform_EmptyString()
		{
			ITsStrBldr emptyString = TsStrBldrClass.Create();
			Assert.IsNull(LexEntryUi.FindEntryForWordform(Cache, emptyString.GetString()));
		}
	}
}
