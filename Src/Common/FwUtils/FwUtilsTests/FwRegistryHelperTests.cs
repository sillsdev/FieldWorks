// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwRegistryHelperTests.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Test the FwRegistryHelperTests class.
	/// </summary>
	[TestFixture]
	public class FwRegistryHelperTests : BaseTest
	{
		/// <summary>
		/// Tests that hklm registry keys can be written correctly.
		/// Marked as ByHand as it should show a UAC dialog on Vista and Windows7.
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void SetValueAsAdmin()
		{
			using (var registryKey = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				registryKey.SetValueAsAdmin("keyname", "value");
			}

			using (var registryKey = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				Assert.AreEqual("value", registryKey.GetValue("keyname") as string);
			}
		}
	}
}