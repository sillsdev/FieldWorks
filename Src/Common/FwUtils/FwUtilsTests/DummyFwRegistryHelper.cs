// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Microsoft.Win32;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region DummyFwRegistryHelper class
	/// <summary>
	/// Alternative implementation for unit tests
	/// </summary>
	public class DummyFwRegistryHelper : IFwRegistryHelper
	{
		private Dictionary<string, RegistryKey> FakeKeyMap = new Dictionary<string, RegistryKey>();

		#region IFwRegistryHelper implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool Paratext7orLaterInstalled()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool ParatextSettingsDirectoryExists()
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public string ParatextSettingsDirectory()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksRegistryKeyLocalMachine
		{
			get { return GetTestKey("FieldWorksRegistryKLM"); }
		}

		/// <summary></summary>
		public RegistryKey LocalMachineHive
		{
			get { return GetTestKey("HKLM"); }
		}

		/// <summary>
		///
		/// </summary>
		private RegistryKey GetTestKey(string keyName)
		{
			return Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\UnitTests\HelperFW\" + keyName);
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksBridgeRegistryKeyLocalMachine
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksRegistryKeyLocalMachineForWriting
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksRegistryKey
		{
			get
			{
				return Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\UnitTests\DirectoryFinderTests");
			}
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksVersionlessRegistryKey
		{
			get
			{
				return Registry.CurrentUser.CreateSubKey(
					@"Software\SIL\FieldWorks\UnitTests");
			}
		}

		/// <summary>
		///
		/// </summary>
		public string UserLocaleValueName
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		/// <summary>
		/// For testing the upgrade of user registry keys from FW7 to FW8
		/// </summary>
		public RegistryKey SetupVersion7Settings()
		{
			var version7Key = FieldWorksVersionlessRegistryKey.CreateSubKey(
				FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7);
			// add some test keys and values here
			const string flexKeyName = "LanguageExplorer";
			const string teKeyName = "TE";
			const string dirName = "TestDir";
			const string crashes = "NumberOfHorrendousCrashes";
			const string valueName3 = "FlexTestValue1";
			const string valueName4 = "FlexTestValue2";
			const string launches = "launches";
			const string userWs = "UserWs";
			var flexKey = version7Key.CreateSubKey(flexKeyName);
			var teKey = version7Key.CreateSubKey(teKeyName);
			version7Key.SetValue(dirName, "Z:\\somedirectory\\subdir\\subdir\\DontUseThis");
			version7Key.SetValue(crashes, 200);
			version7Key.SetValue(userWs, "pt");
			flexKey.SetValue(valueName3, 20);
			flexKey.SetValue(valueName4, "somestring");
			flexKey.SetValue(launches, 44);
			teKey.SetValue(crashes, 10);
			teKey.SetValue(dirName, "Z:\\somedirectory");
			return version7Key;
		}

		/// <summary>
		/// For testing key migration on upgrade.
		/// </summary>
		public RegistryKey SetupVersion7ProjectSharedSetting()
		{
			var hklmFw7 = SetupVersion7ProjectSharedSettingLocation();
			hklmFw7.SetValue("ProjectShared", "True");
			return hklmFw7;
		}

		/// <summary>
		/// For testing key migration on upgrade.
		/// </summary>
		public RegistryKey SetupVersion7ProjectSharedSettingLocation()
		{
			return LocalMachineHive.CreateSubKey(@"SOFTWARE\SIL\FieldWorks\7.0");
		}

		/// <summary>
		/// For testing upgrade of user settings where some version 8 keys already exist.
		/// </summary>
		/// <returns></returns>
		public RegistryKey SetupVersion8Settings()
		{
			var version8Key = FieldWorksVersionlessRegistryKey.CreateSubKey(FwRegistryHelper.FieldWorksRegistryKeyName);
			const string userWs = "UserWs";
			version8Key.SetValue(userWs, "fr");

			return version8Key;
		}

		/// <summary>
		/// Removes all SubTrees from registry, for test SetUp or Teardown
		/// </summary>
		public void DeleteAllSubTreesIfPresent()
		{
			DeleteRegistrySubkeyTreeIfPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
				FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7);
			DeleteRegistrySubkeyTreeIfPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey,
				FwRegistryHelper.FieldWorksRegistryKeyName);
			DeleteRegistrySubkeyTreeIfPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey, "DirectoryFinderTests");
			DeleteRegistrySubkeyTreeIfPresent(FwRegistryHelper.FieldWorksVersionlessRegistryKey, "HelperFW");
		}

		private void DeleteRegistrySubkeyTreeIfPresent(RegistryKey key, string subKeyName)
		{
			if(RegistryHelper.KeyExists(key, subKeyName))
			{
				key.DeleteSubKeyTree(subKeyName);
			}
		}
	}
	#endregion
}
