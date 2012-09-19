using System;
using Microsoft.Win32;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class WriteRegistry : Task
	{
		/// <summary>
		/// The name of the registry hive to use.
		/// </summary>
		/// <value>
		/// The enum of type values <see cref="T:Microsoft.Win32.RegistryHive"/> including LocalMachine, Users, CurrentUser and ClassesRoot.
		/// </value>
		[Required]
		public string Hive { get; set; }

		/// <summary>
		/// The registry key to write.
		/// </summary>
		[Required]
		public string Key { get; set; }

		/// <summary>
		/// The value to write to the registry.
		/// </summary>
		public string Value { get; set; }

		public override bool Execute()
		{
			if (string.IsNullOrEmpty(Hive))
				Hive = RegistryHive.LocalMachine.ToString();
			var tempRegHive = Hive.Split(" ".ToCharArray()[0]);
			if (tempRegHive.Length != 1)
			{
				Log.LogError("WriteRegistry: Only 1 hive is allowed.");
				return false;
			}
			RegistryHive regHive = (RegistryHive)System.Enum.Parse(typeof(RegistryHive), tempRegHive[0], true);

			// xbuild helpfully converts \ to / for us, even when we don't want it to
			var keyTmp = Key.Replace('/', '\\');
			var pathParts = keyTmp.Split("\\".ToCharArray(0,1)[0]);
			var regKeyValueName = pathParts[pathParts.Length - 1];
			var regKey = keyTmp.Substring(0, (keyTmp.Length - regKeyValueName.Length));
			if (String.IsNullOrEmpty(regKey))
			{
				Log.LogError("WriteRegistry: missing registry key.");
				return false;
			}
			RegistryKey mykey = OpenRegKey(regKey, regHive);
			if (mykey == null)
			{
				Log.LogError("WriteRegistry: registry path not found - key='{0}'; hive='{1}';", regKey, regHive);
				return false;
			}
			Log.LogMessage(MessageImportance.Low, "Setting {0} to {1}", regKeyValueName, Value);
			mykey.SetValue(regKeyValueName, Value);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the registry key. If the key doesn't exist it will be created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected RegistryKey OpenRegKey(string key, RegistryHive hive)
		{
			Log.LogMessage(MessageImportance.Low, "Opening {0}:{1}", hive.ToString(), key);
			return GetHiveKey(hive).CreateSubKey(key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the registry key that corresponds to the passed in hive
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected RegistryKey GetHiveKey(RegistryHive hive)
		{
			switch (hive)
			{
				case RegistryHive.LocalMachine:
					return Registry.LocalMachine;
				case RegistryHive.Users:
					return Registry.Users;
				case RegistryHive.CurrentUser:
					return Registry.CurrentUser;
				case RegistryHive.ClassesRoot:
					return Registry.ClassesRoot;
				default:
					Log.LogWarning("WriteRegistry: registry not found for hive='{0}'.", hive.ToString());
					return null;
			}
		}
	}
}
