// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Test the FwRegistryHelper class.
	/// </summary>
	[TestFixture]
	public class FwRegistryHelperTests
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
			Assert.Greater(key.SubKeyCount, 0, "Registry key {0} does not have any subkeys, can't find {1}", key.Name, subKeyName);
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

		private void VerifyExpectedMigrationResults(RegistryKey version9Key)
		{
			AssertRegistrySubkeyPresent(version9Key, DummyFwRegistryHelper.FlexKeyName);
			AssertRegistryIntValueEquals(version9Key,
				DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.ValueName3, DummyFwRegistryHelper.Value3);
			AssertRegistryStringValueEquals(version9Key,
				DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.ValueName4, DummyFwRegistryHelper.Value4);
			Assert.IsTrue(version9Key.GetValueNames().Contains(DummyFwRegistryHelper.DirName));
			var dirNameFromKey = version9Key.GetValue(DummyFwRegistryHelper.DirName);
			Assert.AreEqual(DummyFwRegistryHelper.DirNameValue, dirNameFromKey);
		}

		#endregion

		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with no upgrade necessary (no previous keys exist).
		/// </summary>
		[Test]
		public void UpgradeUserSettingsIfNeeded_NotNeeded()
		{
			// SUT
			Assert.IsFalse(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

			// Verification
			// The above upgrade shouldn't have done anything; verify at least that the version 9 key is empty.
			using (var version9Key = FwRegistryHelper.FieldWorksRegistryKey)
			{
				Assert.AreEqual(0, version9Key.SubKeyCount, "There was nothing to migrate, so no subkeys should have been created");
				Assert.AreEqual(0, version9Key.ValueCount, "There was nothing to migrate, so no values should have been created");
			}
		}

		/// <summary>
		/// Ensure selected V7 keys and values are upgraded to V9.
		/// </summary>
		[Test]
		public void ExpectedSettingsRetained_7_To_9_Upgrade()
		{
			using (m_helper.SetupVersion7Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				using (var version9Key = m_helper.FieldWorksRegistryKey)
				{
					VerifyExpectedMigrationResults(version9Key);
					Assert.AreEqual(DummyFwRegistryHelper.UserWsValue, version9Key.GetValue(DummyFwRegistryHelper.UserWs));
				}
			}
		}

		/// <summary>
		/// Ensure selected V8 keys and values are upgraded to V9.
		/// </summary>
		[Test]
		public void ExpectedSettingsRetained_8_To_9_Upgrade()
		{
			using (m_helper.SetupVersion8Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				using (var version9Key = m_helper.FieldWorksRegistryKey)
				{
					VerifyExpectedMigrationResults(version9Key);
					Assert.AreEqual("fr", version9Key.GetValue(DummyFwRegistryHelper.UserWs));
				}
			}
		}

		/// <summary>
		/// Ensure selected V7 and V8 keys and values are upgraded to V9.
		/// </summary>
		[Test]
		public void ExpectedSettingsRetained_7_and_8_To_9_Upgrade()
		{
			using (m_helper.SetupVersion7Settings())
			using (m_helper.SetupVersion8Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				using (var version9Key = m_helper.FieldWorksRegistryKey)
				{
					VerifyExpectedMigrationResults(version9Key);
					Assert.AreEqual("fr", version9Key.GetValue(DummyFwRegistryHelper.UserWs));
				}
			}
		}

		/// <summary>
		/// V7 Registry key removed on upgrade to V10, and extant V10 WS preserved.
		/// </summary>
		[Test]
		public void V7_KeyRemoved_7_To_10_Upgrade()
		{
			using (m_helper.SetupVersion7Settings())
			using (m_helper.SetupVersion10Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Is the version 7 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7),
					"Old version 7.0 subkey tree didn't get wiped out.");

				using (var version10Key = m_helper.FieldWorksRegistryKey)
				{
					Assert.AreEqual("sp", version10Key.GetValue(DummyFwRegistryHelper.UserWs));
				}
			}
		}

		/// <summary>
		/// V8 Registry key removed on upgrade to V9
		/// </summary>
		[Test]
		public void V8_KeyRemoved_8_To_10_Upgrade()
		{
			using (m_helper.SetupVersion8Settings())
			using (m_helper.SetupVersion10Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Is the version 8 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
						FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion8),
					"Old version 8 subkey tree didn't get wiped out.");

				using (var version10Key = m_helper.FieldWorksRegistryKey)
				{
					Assert.AreEqual("sp", version10Key.GetValue(DummyFwRegistryHelper.UserWs));
				}
			}
		}

		/// <summary>
		/// V7 and V8 Registry keys removed on upgrade to V10, and extant V10 WS preserved.
		/// </summary>
		[Test]
		public void V8_and_V7_KeyRemoved_7_and_8_To_10_Upgrade()
		{
			using (m_helper.SetupVersion7Settings())
			using (m_helper.SetupVersion8Settings())
			using (m_helper.SetupVersion10Settings())
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

				using (var version10Key = m_helper.FieldWorksRegistryKey)
				{
					VerifyExpectedMigrationResults(version10Key);
					Assert.AreEqual("sp", version10Key.GetValue(DummyFwRegistryHelper.UserWs));
				}
			}
		}

		/// <summary>
		/// Settings are properly migrated from old 32-bit installations, and keys are removed from the 32-bit space
		/// </summary>
		[Test]
		public void TestUpgradeFrom32BitTo64Bit()
		{
			using (m_helper.SetupVersion8Old32BitSettings())
			using (m_helper.SetupVersion9Old32BitSettings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Is the key under WOW6432Node gone?
				Assert.IsNull(FwRegistryHelper.FieldWorksVersionlessOld32BitRegistryKey, "Old 32-bit key tree didn't get wiped out.");

				using (var version9Key = m_helper.FieldWorksRegistryKey)
				{
					Assert.AreEqual(DummyFwRegistryHelper.UserWsValue, version9Key.GetValue(DummyFwRegistryHelper.UserWs),
						"Values from 32-bit version 9 did not get migrated");
					Assert.AreEqual("From32Bit8", version9Key.GetValue(DummyFwRegistryHelper.ExtraValue),
						"Values from 32-bit version 8 did not get migrated");
					VerifyExpectedMigrationResults(version9Key);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with an upgrade where
		/// there already exists a v7 key and a value we don't want to overwrite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RetainExtantV10Setting_v7_Upgrade()
		{
			// Setup
			using (m_helper.SetupVersion7Settings())
			using (m_helper.SetupVersion10Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Check for version 10 key
				using (var versionlessKey = FwRegistryHelper.FieldWorksVersionlessRegistryKey)
				{
					// Verification
					// Check that UserWs didn't get overwritten
					// Version 7 had 'pt', but 10 already had it set to 'sp'.
					AssertRegistryStringValueEquals(versionlessKey,
						FwRegistryHelper.FieldWorksRegistryKeyName, DummyFwRegistryHelper.UserWs, "sp");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with an upgrade where
		/// there already exists a V10 value we don't want to overwrite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RetainExtantV10Setting_v8_Upgrade()
		{
			// Setup
			using (m_helper.SetupVersion8Settings())
			using (m_helper.SetupVersion10Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Check for version 9 key
				using (var versionlessKey = FwRegistryHelper.FieldWorksVersionlessRegistryKey)
				{
					// Verification
					// Check that UserWs didn't get overwritten
					// Version 8 had 'fr', but 10 already had it set to 'sp'.
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
		public void RetainExtantV10Setting_v7_and_v8_Upgrade()
		{
			// Setup
			using (m_helper.SetupVersion7Settings())
			using (m_helper.SetupVersion8Settings())
			using (m_helper.SetupVersion10Settings())
			{
				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				using (var versionlessKey = FwRegistryHelper.FieldWorksVersionlessRegistryKey)
				{
					// Check that UserWs didn't get overwritten
					// Version 7 had 'pt', pre-existing Version 8 had 'fr', but 10 already had it set to 'sp'.
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
			using (var version10Key = m_helper.SetupVersion10Settings())
			{
				// In 7.
				AssertRegistrySubkeyPresent(version7Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version7Key, DummyFwRegistryHelper.FlexKeyName);
				// Not in 10.
				AssertRegistrySubkeyNotPresent(version10Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName);

				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Didn't make it into 10.
				AssertRegistrySubkeyNotPresent(version10Key, FwRegistryHelper.TranslationEditor);
				AssertRegistryValueNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Crashes);
				AssertRegistryValueNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Launches);
			}
		}

		/// <summary>
		/// Tests that TE key was removed from version 8 upgrade.
		/// </summary>
		[Test]
		public void UnlovedStuff_Removed_v8_Upgrade()
		{
			using (var version8Key = m_helper.SetupVersion8Settings())
			using (var version10Key = m_helper.SetupVersion10Settings())
			{
				// In 8.
				AssertRegistrySubkeyPresent(version8Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version8Key, DummyFwRegistryHelper.FlexKeyName);
				// Not in 10.
				AssertRegistrySubkeyNotPresent(version10Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName);

				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Didn't make it into 10.
				AssertRegistrySubkeyNotPresent(version10Key, FwRegistryHelper.TranslationEditor);
				AssertRegistryValueNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Crashes);
				AssertRegistryValueNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Launches);
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
			using (var version10Key = m_helper.SetupVersion10Settings())
			{
				// In 7.
				AssertRegistrySubkeyPresent(version7Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version7Key, DummyFwRegistryHelper.FlexKeyName);
				// In 8.
				AssertRegistrySubkeyPresent(version8Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyPresent(version8Key, DummyFwRegistryHelper.FlexKeyName);
				// Not in 10.
				AssertRegistrySubkeyNotPresent(version10Key, FwRegistryHelper.TranslationEditor);
				AssertRegistrySubkeyNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName);

				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Didn't make it into 10.
				AssertRegistrySubkeyNotPresent(version10Key, FwRegistryHelper.TranslationEditor);
				AssertRegistryValueNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Crashes);
				AssertRegistryValueNotPresent(version10Key, DummyFwRegistryHelper.FlexKeyName, DummyFwRegistryHelper.Launches);
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

				// SUT
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

				//SUT
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

				// SUT
				Assert.IsTrue(FwRegistryHelper.UpgradeUserSettingsIfNeeded());

				// Verification
				// Verify that the version 9 ProjectShared key is still missing after migration.
				AssertRegistryValueNotPresent(FwRegistryHelper.FieldWorksRegistryKey, null, DummyFwRegistryHelper.ProjectShared);
			}
		}
	}
}