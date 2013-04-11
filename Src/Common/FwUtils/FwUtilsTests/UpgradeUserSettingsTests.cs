using Microsoft.Win32;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	class UpgradeUserSettingsTests
	{
		#region UpgradeUserSettingsTests

		private DummyFwRegistryHelper m_helper;

		/// <summary>
		///
		/// </summary>
		[SetUp]
		public void Setup()
		{
			m_helper = new DummyFwRegistryHelper();
			FwRegistryHelper.Manager.SetRegistryHelper(m_helper);
			DeleteRegistrySubkeyTreeIfPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
				FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7);
			DeleteRegistrySubkeyTreeIfPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
				FwRegistryHelper.FieldWorksRegistryKeyName);
		}

		/// <summary>
		/// Resets the registry helper
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			FwRegistryHelper.Manager.Reset();
		}

		#region Utility Methods

		private void DeleteRegistrySubkeyTreeIfPresent(RegistryKey key, string subKeyName)
		{
			if (RegistryHelper.KeyExists(key, subKeyName))
			{
				key.DeleteSubKeyTree(subKeyName);
			}
		}

		private void AssertIfRegistrySubkeyPresent(RegistryKey key, string subKeyName)
		{
			Assert.IsFalse(RegistryHelper.KeyExists(key, subKeyName),
				"Registry subkey {0} should not be found in {1}.", subKeyName, key.Name);
		}

		private RegistryKey AssertIfRegistrySubkeyNotPresent(RegistryKey key, string subKeyName)
		{
			Assert.IsTrue(RegistryHelper.KeyExists(key, subKeyName),
				"Registry subkey {0} was not found in {1}.", subKeyName, key.Name);
			return key.CreateSubKey(subKeyName);
		}

		private void AssertIfRegistryValuePresent(RegistryKey key, string subKey, string entryName)
		{
			object valueObject;
			Assert.IsFalse(RegistryHelper.RegEntryExists(key, subKey, entryName, out valueObject),
				"Expected absence of entry {0} in subkey {1} of key {2}", entryName, subKey, key.Name);
		}

		private void CheckForRegistryStringValue(RegistryKey key, string subKey, string entryName, string value)
		{
			object valueObject;
			Assert.IsTrue(RegistryHelper.RegEntryExists(key, subKey, entryName, out valueObject),
				"Expected presence of entry {0} in subkey {1} of key {2}", entryName, subKey, key.Name);
			Assert.AreEqual(value, (string)valueObject);
		}

		private void CheckForRegistryIntValue(RegistryKey key, string subKey, string entryName, int value)
		{
			object valueObject;
			Assert.IsTrue(RegistryHelper.RegEntryExists(key, subKey, entryName, out valueObject),
				"Expected presence of entry {0} in subkey {1} of key {2}", entryName, subKey, key.Name);
			Assert.AreEqual(value, (int)valueObject);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with no upgrade necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpgradeUserSettingsIfNeeded_NotNeeded()
		{
			// If there's no version 7.0 key, the upgrade shouldn't happen

			// SUT
			FwRegistryHelper.UpgradeUserSettingsIfNeeded();

			// Verification
			// The above upgrade shouldn't have done anything; verify at least that the version 8 key
			// is missing.
			AssertIfRegistrySubkeyPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
				FwRegistryHelper.FieldWorksRegistryKeyName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with an upgrade.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpgradeUserSettingsIfNeeded_Needed()
		{
			// Setup
			const string flexKeyName = "LanguageExplorer";
			const string teKeyName = "TE";
			const string dirName = "TestDir";
			const string crashes = "NumberOfHorrendousCrashes";
			const string valueName3 = "FlexTestValue1";
			const string valueName4 = "FlexTestValue2";
			const string launches = "launches";
			using(var version7Key = m_helper.SetupVersion7Settings())
			{

				// SUT
				FwRegistryHelper.UpgradeUserSettingsIfNeeded();

				// Verification
				// first and foremost, is the version 7 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7),
					"Old version 7.0 subkey tree didn't get wiped out.");

				// Check for version 8 key
				using(var version8Key = AssertIfRegistrySubkeyNotPresent(
					FwRegistryHelper.FieldWorksVersionlessRegistryKey, FwRegistryHelper.FieldWorksRegistryKeyName))
				{
					// Check for flex key
					using(var flexKey = AssertIfRegistrySubkeyNotPresent(version8Key, flexKeyName))
					{
						// Check for TE key
						using(var teKey = AssertIfRegistrySubkeyNotPresent(version8Key, teKeyName))
						{
							// Check for absense of crash value
							AssertIfRegistryValuePresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
								FwRegistryHelper.FieldWorksRegistryKeyName, crashes);
							// Check for absense of launches value
							AssertIfRegistryValuePresent(version8Key, flexKeyName, launches);
							CheckForRegistryIntValue(version8Key, flexKeyName, valueName3, 20);
							CheckForRegistryStringValue(version8Key, teKeyName, dirName, "Z:\\somedirectory");
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpgradeUserSettingsIfNeeded method on FieldWorks with an upgrade where
		/// there already exists a Version8 key and a value we don't want to overwrite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpgradeUserSettingsIfNeeded_ExistingV8Key_DontOverwrite()
		{
			// Setup
			const string flexKeyName = "LanguageExplorer";
			const string teKeyName = "TE";
			const string dirName = "TestDir";
			const string crashes = "NumberOfHorrendousCrashes";
			const string valueName3 = "FlexTestValue1";
			const string valueName4 = "FlexTestValue2";
			const string launches = "launches";
			const string userWs = "UserWs";
			using (var version7Key = m_helper.SetupVersion7Settings())
			using (var version8Key = m_helper.SetupVersion8Settings())
			{

				// SUT
				FwRegistryHelper.UpgradeUserSettingsIfNeeded();

				// Verification
				// first and foremost, is the version 7 key gone?
				Assert.IsFalse(RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7),
					"Old version 7.0 subkey tree didn't get wiped out.");

				// Check for version 8 key
				using(AssertIfRegistrySubkeyNotPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.FieldWorksRegistryKeyName))
					{} // dispose of local RegistryKey for Gendarme
				// Check that UserWs didn't get overwritten
				// Version 7 had 'pt', pre-existing Version 8 had 'fr', which we should have kept.
				CheckForRegistryStringValue(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
					FwRegistryHelper.FieldWorksRegistryKeyName, userWs, "fr");
			}
		}

		#endregion
	}
}
