// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwRegistryHelperTests.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------
using System.Linq;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Test the FwRegistryHelper class.
	/// </summary>
	[TestFixture]
	public class FwRegistryHelperTests : BaseTest
	{
		private DummyFwRegistryHelper m_helper;

		/// <summary>
		/// Setup for each test
		/// </summary>
		[SetUp]
		public void Setup()
		{
			m_helper = new DummyFwRegistryHelper();
			FwRegistryHelper.Manager.SetRegistryHelper(m_helper);
			m_helper.RemoveTestRegistryEntries();
		}

		/// <summary>
		/// Tear down after each test
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			m_helper.RemoveTestRegistryEntries();
			FwRegistryHelper.Manager.Reset();
		}

		#region Utility Methods

		private void AssertRegistrySubkeyNotPresent(RegistryKey key, string subKeyName)
		{
			Assert.IsFalse(RegistryHelper.KeyExists(key, subKeyName),
				"Registry subkey {0} should not be found in {1}.", subKeyName, key.Name);
		}

		private void AssertRegistrySubkeyPresent(RegistryKey key, string subKeyName)
		{
			Assert.IsTrue(RegistryHelper.KeyExists(key, subKeyName),
				"Registry subkey {0} was not found in {1}.", subKeyName, key.Name);
		}

		private void AssertRegistryValuePresent(RegistryKey key, string subKey, string entryName)
		{
			object valueObject;
			Assert.IsTrue(RegistryHelper.RegEntryValueExists(key, subKey, entryName, out valueObject),
				"Expected presence of entry {0} in subkey {1} of key {2}", entryName, subKey, key.Name);
		}

		private void AssertRegistryValueNotPresent(RegistryKey key, string subKey, string entryName)
		{
			object valueObject;
			Assert.IsFalse(RegistryHelper.RegEntryValueExists(key, subKey, entryName, out valueObject),
				"Expected absence of entry {0} in subkey {1} of key {2}", entryName, subKey, key.Name);
		}

		private void AssertRegistryStringValueEquals(RegistryKey key, string subKey, string entryName, string expectedValue)
		{
			object valueObject;
			Assert.IsTrue(RegistryHelper.RegEntryValueExists(key, subKey, entryName, out valueObject),
				"Expected presence of entry {0} in subkey {1} of key {2}", entryName, subKey, key.Name);
			Assert.AreEqual(expectedValue, (string)valueObject);
		}

		private void AssertRegistryIntValueEquals(RegistryKey key, string subKey, string entryName, int expectedValue)
		{
			object valueObject;
			Assert.IsTrue(RegistryHelper.RegEntryValueExists(key, subKey, entryName, out valueObject),
				"Expected presence of entry {0} in subkey {1} of key {2}", entryName, subKey, key.Name);
			Assert.AreEqual(expectedValue, (int)valueObject);
		}

		private void VerifyExpectedV9Results(RegistryKey version9Key)
		{
			AssertRegistrySubkeyPresent(version9Key, DummyFwRegistryHelper.FlexKeyName);
			AssertRegistryIntValueEquals(version9Key,
				DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.ValueName3, 20);
			AssertRegistryStringValueEquals(version9Key,
				DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.ValueName4, "somestring");
			Assert.IsTrue(version9Key.GetValueNames().Contains(DummyFwRegistryHelper.DirName));
			var dirNameFromKey = version9Key.GetValue(DummyFwRegistryHelper.DirName);
			Assert.AreEqual(dirNameFromKey, "Z:\\somedirectory\\subdir\\subdir\\DontUseThis");
		}

		#endregion

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with no upgrade necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpgradeUserSettingsIfNeeded_NotNeeded()
		{
			// If there's no version 7.0 or 8 key, the upgrade shouldn't happen

			// SUT
			Assert.IsFalse(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

			// Verification
			// The above upgrade shouldn't have done anything; verify at least that the version 9 key
			// is missing.
			AssertRegistrySubkeyNotPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
				FwRegistryHelper.FieldWorksRegistryKeyName);
		}

		/// <summary>
		/// Ensure selected V7 keys and values are upgraded to V9.
		/// </summary>
		[Test]
		public void ExpectedSettingsRetained_7_To_9_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Make sure select expected info was moved to v9.
				VerifyExpectedV9Results(version9Key);
			}
		}

		/// <summary>
		/// Ensure selected V8 keys and values are upgraded to V9.
		/// </summary>
		[Test]
		public void ExpectedSettingsRetained_8_To_9_Upgrade()
		{
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Make sure select expected info was moved to v9.
				VerifyExpectedV9Results(version9Key);
			}
		}

		/// <summary>
		/// Ensure selected V7 and V8 keys and values are upgraded to V9.
		/// </summary>
		[Test]
		public void ExpectedSettingsRetained_7_and_8_To_9_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Make sure select expected info was moved to v9.
				VerifyExpectedV9Results(version9Key);
			}
		}

		/// <summary>
		/// V7 Registry key removed on upgrade to V9.
		/// </summary>
		[Test]
		public void V7_KeyRemoved_7_To_9_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Is the version 7 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7),
					"Old version 7.0 subkey tree didn't get wiped out.");
			}
		}

		/// <summary>
		/// V8 Registry key removed on upgrade to V9.
		/// </summary>
		[Test]
		public void V8_KeyRemoved_8_To_9_Upgrade()
		{
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Is the version 8 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion8),
					"Old version 8 subkey tree didn't get wiped out.");
			}
		}

		/// <summary>
		/// V7 and V8 Registry keys removed on upgrade to V9.
		/// </summary>
		[Test]
		public void V8_and_V7_KeyRemoved_7_and_8_To_9_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Is the version 7 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7),
					"Old version 7.0 subkey tree didn't get wiped out.");

				// Is the version 8 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion8),
					"Old version 8 subkey tree didn't get wiped out.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with an upgrade where
		/// there already exists a v7 key and a value we don't want to overwrite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RetainExtantV9Setting_v7_Upgrade()
		{
			// Setup
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Check for version 9 key
				using (var versionlessKey = FwRegistryHelper.FieldWorksVersionlessRegistryKey)
				{
					// Verification
					// Check that UserWs didn't get overwritten
					// Version 7 had 'pt', but 9 already had it set to 'sp'.
					AssertRegistryStringValueEquals(versionlessKey,
						FwRegistryHelper.FieldWorksRegistryKeyName, DummyFwRegistryHelper.UserWs, "sp");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with an upgrade where
		/// there already exists a V9 value we don't want to overwrite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RetainExtantV9Setting_v8_Upgrade()
		{
			// Setup
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Check for version 9 key
				using (var versionlessKey = FwRegistryHelper.FieldWorksVersionlessRegistryKey)
				{
					// Verification
					// Check that UserWs didn't get overwritten
					// Version 8 had 'fr', but 9 already had it set to 'sp'.
					AssertRegistryStringValueEquals(versionlessKey,
						FwRegistryHelper.FieldWorksRegistryKeyName, DummyFwRegistryHelper.UserWs, "sp");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with an upgrade where
		/// there already exists V8 value we don't want to overwrite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RetainExtantV9Setting_v7_and_v8_Upgrade()
		{
			// Setup
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				using (var versionlessKey = FwRegistryHelper.FieldWorksVersionlessRegistryKey)
				{
					// Check that UserWs didn't get overwritten
					// Version 7 had 'pt', pre-existing Version 8 had 'fr', but 9 already had it set to 'sp'.
					AssertRegistryStringValueEquals(versionlessKey,
						FwRegistryHelper.FieldWorksRegistryKeyName, DummyFwRegistryHelper.UserWs, "sp");
				}
			}
		}

		/// <summary>
		/// Tests that TE key was removed from version 7 upgrade.
		/// </summary>
		[Test]
		public void UnlovedStuff_Removed_v7_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// In 7.
				AssertRegistrySubkeyPresent(version7Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version7Key, DummyFwRegistryHelper.FlexKeyName);
				// Not in 9.
				AssertRegistrySubkeyNotPresent(version9Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName);

				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Didn't make it into 9.
				AssertRegistrySubkeyNotPresent(version9Key, FwRegistryHelper.TranslationEditor);
				AssertRegistryValueNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Crashes);
				AssertRegistryValueNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Launches);
			}
		}

		/// <summary>
		/// Tests that TE key was removed from version 8 upgrade.
		/// </summary>
		[Test]
		public void UnlovedStuff_Removed_v8_Upgrade()
		{
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// In 8.
				AssertRegistrySubkeyPresent(version8Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version8Key, DummyFwRegistryHelper.FlexKeyName);
				// Not in 9.
				AssertRegistrySubkeyNotPresent(version9Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName);

				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Didn't make it into 9.
				AssertRegistrySubkeyNotPresent(version9Key, FwRegistryHelper.TranslationEditor);
				AssertRegistryValueNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Crashes);
				AssertRegistryValueNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Launches);
			}
		}

		/// <summary>
		/// Tests that TE key was removed from version 7 and v8 upgrade.
		/// </summary>
		[Test]
		public void UnlovedStuff_Removed_v7_and_v8_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version9Key = m_helper.SetupVersion9Settings())
			{
				// In 7.
				AssertRegistrySubkeyPresent(version7Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version7Key, DummyFwRegistryHelper.FlexKeyName);
				// In 8.
				AssertRegistrySubkeyPresent(version8Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version8Key, DummyFwRegistryHelper.FlexKeyName);
				// Not in 9.
				AssertRegistrySubkeyNotPresent(version9Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName);

				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Didn't make it into 9.
				AssertRegistrySubkeyNotPresent(version9Key, FwRegistryHelper.TranslationEditor);
				AssertRegistryValueNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Crashes);
				AssertRegistryValueNotPresent(version9Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Launches);
			}
		}

		/// <summary>
		/// Tests that ProjectShared key was removed from V7 upgrade to V9.
		/// </summary>
		[Test]
		public void HKCU_ProjectShared_Removed_7_To_9_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			{
				version7Key.SetValue(DummyFwRegistryHelper.ProjectShared, "False");
				// Verify ProjectShared is present in version7Key
				AssertRegistryValuePresent(version7Key, null, DummyFwRegistryHelper.ProjectShared);

				object projectsSharedValue;
				// Verify that the version 9 ProjectShared value is missing before migration
				AssertRegistryValueNotPresent(FwRegistryHelper.FieldWorksRegistryKey, null, DummyFwRegistryHelper.ProjectShared);

				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Verify that the version 9 ProjectShared key is still missing after migration.
				AssertRegistryValueNotPresent(FwRegistryHelper.FieldWorksRegistryKey, null, DummyFwRegistryHelper.ProjectShared);
			}
		}

		/// <summary>
		/// Tests that ProjectShared key was removed from V8 upgrade to V9.
		/// </summary>
		[Test]
		public void HKCU_ProjectShared_Removed_8_To_9_Upgrade()
		{
			using (var version8Key = m_helper.SetupVersion8Settings())
			{
				version8Key.SetValue(DummyFwRegistryHelper.ProjectShared, "False");
				// Verify ProjectShared is present in version8Key
				AssertRegistryValuePresent(version8Key, null, DummyFwRegistryHelper.ProjectShared);

				object projectsSharedValue;
				// Verify that the version 9 ProjectShared key is missing before migration
				AssertRegistryValueNotPresent(FwRegistryHelper.FieldWorksRegistryKey, null, DummyFwRegistryHelper.ProjectShared);

				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Verify that the version 9 ProjectShared key is still missing after migration.
				AssertRegistryValueNotPresent(FwRegistryHelper.FieldWorksRegistryKey, null, DummyFwRegistryHelper.ProjectShared);
			}
		}

		/// <summary>
		/// Tests that ProjectShared key was removed from V7 and V8 upgrade to V9.
		/// </summary>
		[Test]
		public void HKCU_ProjectShared_Removed_7_and_8_To_9_Upgrade()
		{
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version8Key = m_helper.SetupVersion8Settings())
			{
				version7Key.SetValue(DummyFwRegistryHelper.ProjectShared, "False");
				// Verify ProjectShared is present in version7Key
				AssertRegistryValuePresent(version7Key, null, DummyFwRegistryHelper.ProjectShared);
				version8Key.SetValue(DummyFwRegistryHelper.ProjectShared, "False");
				// Verify ProjectShared is present in version8Key
				AssertRegistryValuePresent(version8Key, null, DummyFwRegistryHelper.ProjectShared);

				object projectsSharedValue;
				// Verify that the version 9 ProjectShared key is missing before migration
				AssertRegistryValueNotPresent(FwRegistryHelper.FieldWorksRegistryKey, null, DummyFwRegistryHelper.ProjectShared);

				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Verify that the version 9 ProjectShared key is still missing after migration.
				AssertRegistryValueNotPresent(FwRegistryHelper.FieldWorksRegistryKey, null, DummyFwRegistryHelper.ProjectShared);
			}
		}
	}
}