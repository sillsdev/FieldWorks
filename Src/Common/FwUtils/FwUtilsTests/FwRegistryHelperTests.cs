// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwRegistryHelperTests.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Test the FwRegistryHelperTests class.
	/// </summary>
	[TestFixture]
	public class FwRegistryHelperTests : BaseTest
	{
		private DummyFwRegistryHelper m_helper;

		[SetUp]
		public void Setup()
		{
			m_helper = new DummyFwRegistryHelper();
			FwRegistryHelper.Manager.SetRegistryHelper(m_helper);
			m_helper.DeleteAllSubTreesIfPresent();
		}

		[TearDown]
		public void TearDown()
		{
			FwRegistryHelper.Manager.Reset();
		}

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

		/// <summary>
		/// Tests how ProjectShared key was migrated from version 7.
		/// </summary>
		[Test]
		public void MigrateVersion7ValueIfNeeded_7Unset_NotMigrated()
		{
			// Setup
			using (var version7Key = m_helper.SetupVersion7Settings())
			{
				FwRegistryHelper.MigrateVersion7ValueIfNeeded();

				// Verification
				// Verify that the version 8 ProjectShared key is missing.
				object dummy;
				Assert.IsFalse(RegistryHelper.RegEntryExists(FwRegistryHelper.FieldWorksRegistryKey, null, "ProjectShared", out dummy));
			}
		}

		/// <summary>
		/// Tests how ProjectShared key was migrated from version 7.
		/// </summary>
		[Test]
		public void MigrateVersion7ValueIfNeeded_7UnsetDespiteExistingPath_NotMigrated()
		{
			// Setup
			using (m_helper.SetupVersion7ProjectSharedSettingLocation())
			using (var version7Key = m_helper.SetupVersion7Settings())
			{
				FwRegistryHelper.MigrateVersion7ValueIfNeeded();

				// Verification
				// Verify that the version 8 ProjectShared key is missing.
				object dummy;
				Assert.IsFalse(RegistryHelper.RegEntryExists(FwRegistryHelper.FieldWorksRegistryKey, null, "ProjectShared", out dummy));
			}
		}

		/// <summary>
		/// Tests how ProjectShared key was migrated from version 7.
		/// </summary>
		[Test]
		public void MigrateVersion7ValueIfNeeded_7Set_Migrated()
		{
			// Setup
			using (m_helper.SetupVersion7ProjectSharedSetting())
			using (var version7Key = m_helper.SetupVersion7Settings())
			{
				object projectsSharedValue;
				// Verify that the version 8 ProjectShared key is missing before migration
				Assert.IsFalse(RegistryHelper.RegEntryExists(FwRegistryHelper.FieldWorksRegistryKey, null, "ProjectShared", out projectsSharedValue));

				FwRegistryHelper.MigrateVersion7ValueIfNeeded();

				// Verification
				// Verify that the version 8 ProjectShared key is set after migration.
				Assert.IsTrue(RegistryHelper.RegEntryExists(FwRegistryHelper.FieldWorksRegistryKey, null, "ProjectShared", out projectsSharedValue));
				Assert.IsTrue(bool.Parse((string)projectsSharedValue));
			}
		}
	}
}