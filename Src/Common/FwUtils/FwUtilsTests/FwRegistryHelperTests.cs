// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
			using (var registryKey = FwRegistryHelper.FieldWorksRegistryKeyLocalMachineForWriting)
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