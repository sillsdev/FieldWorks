using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region DummyFwRegistryHelper class
	/// <summary>
	/// Alternative implementation for unit tests
	/// </summary>
	public class DummyFwRegistryHelper : IFwRegistryHelper
	{
		internal static readonly string FlexKeyName = "Language Explorer";
		internal static readonly string DirName = "TestDir";
		internal static readonly string Crashes = "NumberOfHorrendousCrashes";
		internal static readonly string ValueName3 = "FlexTestValue1";
		internal static readonly string ValueName4 = "FlexTestValue2";
		internal static readonly string Launches = "launches";
		internal static readonly string UserWs = "UserWs";
		internal static readonly string ProjectShared = "ProjectShared";

		private Dictionary<string, RegistryKey> FakeKeyMap = new Dictionary<string, RegistryKey>();

		#region IFwRegistryHelper implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool Paratext7orLaterInstalled()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool ParatextSettingsDirectoryExists()
		{
			throw new NotSupportedException();
		}

		/// <summary></summary>
		public string ParatextSettingsDirectory()
		{
			throw new NotSupportedException();
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
				throw new NotSupportedException();
			}
		}

		/// <summary>
		///
		/// </summary>
		public RegistryKey FieldWorksRegistryKeyLocalMachineForWriting
		{
			get
			{
				throw new NotSupportedException();
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
				throw new NotSupportedException();
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

			SetBasicKeysAndValues(version7Key);

			return version7Key;
		}

		private static void SetBasicKeysAndValues(RegistryKey versionKey)
		{
			// add some test keys and values here

			versionKey.SetValue(DirName, "Z:\\somedirectory\\subdir\\subdir\\DontUseThis");
			versionKey.SetValue(UserWs, "pt");

			using (var flexKey = versionKey.CreateSubKey(FlexKeyName))
			{
				flexKey.SetValue(Crashes, 5);
				flexKey.SetValue(ValueName3, 20);
				flexKey.SetValue(ValueName4, "somestring");
				flexKey.SetValue(Launches, 44);
			}
			using (var teKey = versionKey.CreateSubKey(FwRegistryHelper.TranslationEditor))
			{
				teKey.SetValue(Crashes, 10);
				teKey.SetValue(DirName, "Z:\\somedirectory");
			}
		}

		/// <summary>
		/// For testing key migration on upgrade.
		/// </summary>
		public RegistryKey SetupVersion7ProjectSharedSettingInHKLM()
		{
			var hklmFw7 = SetupVersion7ProjectSharedSettingLocation();
			hklmFw7.SetValue(ProjectShared, "True");
			return hklmFw7;
		}

		/// <summary>
		/// For testing key migration on upgrade.
		/// </summary>
		public RegistryKey SetupVersion7ProjectSharedSettingLocation()
		{
			return LocalMachineHive.CreateSubKey(@"SOFTWARE\SIL\FieldWorks\" + FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion7);
		}

		/// <summary>
		/// For testing upgrade of user settings where some version 8 keys already exist.
		/// </summary>
		/// <returns></returns>
		public RegistryKey SetupVersion8Settings()
		{
			var version8Key = FieldWorksVersionlessRegistryKey.CreateSubKey(FwRegistryHelper.OldFieldWorksRegistryKeyNameVersion8);

			SetBasicKeysAndValues(version8Key);

			version8Key.SetValue(UserWs, "fr");

			return version8Key;
		}

		/// <summary>
		/// For testing upgrade of user settings where some version 8 keys already exist.
		/// </summary>
		/// <returns></returns>
		public RegistryKey SetupVersion9Settings()
		{
			var version9Key = FieldWorksVersionlessRegistryKey.CreateSubKey(FwRegistryHelper.FieldWorksRegistryKeyName);

			version9Key.SetValue(UserWs, "sp");

			return version9Key;
		}

		/// <summary>
		/// Removes the "Software\SIL\FieldWorks\UnitTests" key and everything in it.
		/// </summary>
		internal void RemoveTestRegistryEntries()
		{
			using (var fwKey = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks"))
			{
				fwKey.DeleteSubKeyTree("UnitTests", false);
			}
		}
	}
	#endregion
}
