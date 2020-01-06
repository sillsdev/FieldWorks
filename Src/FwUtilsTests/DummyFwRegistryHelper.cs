// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Microsoft.Win32;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Alternative implementation for unit tests
	/// </summary>
	internal class DummyFwRegistryHelper : IFwRegistryHelper
	{
		internal const string FlexKeyName = "Language Explorer";
		internal const string DirName = "TestDir";
		internal const string DirNameValue = @"Z:\somedirectory\subdir\subdir\DontUseThis";
		internal const string Crashes = "NumberOfHorrendousCrashes";
		internal const string ValueName3 = "FlexTestValue1";
		internal const int Value3 = 20;
		internal const string ValueName4 = "FlexTestValue2";
		internal const string Value4 = "somestring";
		internal const string ExtraValue = "NotSetInSharedSetupMethod";
		internal const string Launches = "launches";
		internal const string UserWs = "UserWs";
		internal const string UserWsValue = "pt";
		internal const string ProjectShared = "ProjectShared";

		#region IFwRegistryHelper implementation

		/// <inheritdoc />
		public bool Paratext7Installed()
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public RegistryKey FieldWorksRegistryKeyLocalMachine => GetTestKey("FieldWorksRegistryKLM");

		/// <inheritdoc />
		public RegistryKey LocalMachineHive => GetTestKey("HKLM");

		/// <summary />
		private RegistryKey GetTestKey(string keyName)
		{
			return Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\UnitTests\HelperFW\" + keyName);
		}

		/// <inheritdoc />
		public RegistryKey FieldWorksBridgeRegistryKeyLocalMachine
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <inheritdoc />
		public RegistryKey FieldWorksRegistryKeyLocalMachineForWriting
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <inheritdoc />
		public RegistryKey FieldWorksRegistryKey => FieldWorksVersionlessRegistryKey.CreateSubKey(FwRegistryHelper.FieldWorksRegistryKeyName);

		/// <inheritdoc />
		public RegistryKey FieldWorksVersionlessRegistryKey => Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\UnitTests");

		/// <inheritdoc />
		public RegistryKey FieldWorksVersionlessOld32BitRegistryKey => FieldWorksVersionlessRegistryKey.OpenSubKey("WOW6432Node");

		/// <summary>
		/// For testing the upgrade of user registry keys from under WOW6432Node
		/// </summary>
		public RegistryKey FieldWorksVersionlessOld32BitRegistryKeyForWriting => FieldWorksVersionlessRegistryKey.CreateSubKey("WOW6432Node");

		/// <inheritdoc />
		public string UserLocaleValueName
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		#endregion

		/// <summary>
		/// For testing the upgrade of user registry keys from FW7 to FW8
		/// </summary>
		public RegistryKey SetupVersion7Settings()
		{
			var version7Key = CreateSettingsSubKeyForVersion(FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7);

			SetBasicKeysAndValues(version7Key);

			return version7Key;
		}

		/// <summary>
		/// For testing the upgrade of user registry keys from FW7 to FW8
		/// </summary>
		public RegistryKey CreateSettingsSubKeyForVersion(string versionKey)
		{
			return FieldWorksVersionlessRegistryKey.CreateSubKey(versionKey);
		}

		/// <summary>
		/// For testing the upgrade of user registry keys from under WOW6432Node
		/// </summary>
		public RegistryKey CreateSettingsSubKeyForOld32BitVersion(string versionKey)
		{
			return FieldWorksVersionlessOld32BitRegistryKeyForWriting.CreateSubKey(versionKey);
		}

		private static void SetBasicKeysAndValues(RegistryKey versionKey)
		{
			// add some test keys and values here

			versionKey.SetValue(DirName, DirNameValue);
			versionKey.SetValue(UserWs, UserWsValue);

			using (var flexKey = versionKey.CreateSubKey(FlexKeyName))
			{
				Assert.That(flexKey != null, $"{nameof(flexKey)} should not be null");
				flexKey.SetValue(Crashes, 5);
				flexKey.SetValue(ValueName3, Value3);
				flexKey.SetValue(ValueName4, Value4);
				flexKey.SetValue(Launches, 44);
			}
			using (var teKey = versionKey.CreateSubKey(FwRegistryHelper.TranslationEditor))
			{
				Assert.That(teKey != null, $"{nameof(teKey)} should not be null");
				teKey.SetValue(Crashes, 10);
				teKey.SetValue(DirName, @"Z:\somedirectory");
			}
		}

		/// <summary>
		/// For testing upgrade of user settings where some version 8 keys already exist.
		/// </summary>
		public RegistryKey SetupVersion8Settings()
		{
			var version8Key = CreateSettingsSubKeyForVersion(FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion8);

			SetBasicKeysAndValues(version8Key);

			version8Key.SetValue(UserWs, "fr");

			return version8Key;
		}

		/// <summary>
		/// For testing upgrade of user settings where version 8 keys exist in the 32-bit space.
		/// </summary>
		public RegistryKey SetupVersion8Old32BitSettings()
		{
			var version8Key = CreateSettingsSubKeyForOld32BitVersion(FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion8);

			version8Key.SetValue(ExtraValue, "From32Bit8");

			return version8Key;
		}

		/// <summary>
		/// For testing upgrade of user settings where version 9 keys already exist in the 32-bit space.
		/// </summary>
		public RegistryKey SetupVersion9Old32BitSettings()
		{
			var version9Key = CreateSettingsSubKeyForOld32BitVersion(FwRegistryHelper.FieldWorksRegistryKeyName);

			SetBasicKeysAndValues(version9Key);

			return version9Key;
		}

		/// <summary>
		/// For testing upgrade of user settings where some version 9 keys already exist.
		/// </summary>
		public RegistryKey SetupVersion10Settings()
		{
			Assert.AreEqual("10", FwRegistryHelper.FieldWorksRegistryKeyName, $"Please update the migration code and tests to handle migration to version {FwRegistryHelper.FieldWorksRegistryKey}");
			var version10Key = CreateSettingsSubKeyForVersion(FwRegistryHelper.FieldWorksRegistryKeyName);

			version10Key.SetValue(UserWs, "sp");

			return version10Key;
		}

		/// <summary>
		/// Removes the "Software\SIL\FieldWorks\UnitTests" key and everything in it.
		/// </summary>
		internal void RemoveTestRegistryEntries()
		{
			using (var fwKey = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks"))
			{
				Assert.That(fwKey != null, $"{nameof(fwKey)} had better not be null");
				fwKey.DeleteSubKeyTree("UnitTests", false);
			}
		}
	}
}