using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using System.Linq;

namespace CustomActions
{
	/// <summary>
	/// Slightly simplified registry utilities.
	/// </summary>
	public static class RegistryU
	{		
		public static RegistryKey ParseBaseKey(string baseKey)
		{
			switch (baseKey.ToLowerInvariant())
			{
				case "hkey_local_machine":
				case "hklm":
				case "localmachine":
					return Registry.LocalMachine;
				case "hkey_current_user":
				case "hkcu":
				case "currentuser":
					return Registry.CurrentUser;
				case "hkey_classes_root":
				case "hkcr":
					return Registry.ClassesRoot;
				case "hkey_current_config":
				case "hkcc":
					return Registry.CurrentConfig;
				case "hkey_user":
				case "hku":
					return Registry.Users;
				case "hkpd":
					return Registry.PerformanceData;
				default:
					throw new ApplicationException(
					  String.Format("Application error. Tried to get non-existent registry key: {0}.", baseKey));
			}
		}

		/// <summary>
		/// Takes a textual form of the registry path and returns it in usable parts:
		/// HKLM\SOFTWARE\Apple Computer, Inc.\iPod\ :points to the subkey, Key="" to access default value.
		/// HKLM\SOFTWARE\Apple Computer, Inc.\iPod\ID :points to the named key, Key="ID" to access named value.
		/// </summary>
		/// <param name="registryPath">Full path to registry item, root tree will be parsed with ParseBaseKey().</param>
		/// <param name="baseKey">Base key abbreviation</param>
		/// <param name="subKey">Subkey path</param>
		/// <param name="key">Named value, "" if RegistryPath ends in a '\'</param>
		/// <returns></returns>
		public static void ParseFullKey(string registryPath, out string baseKey, out string subKey, out string key)
		{
			ExtractRegistryParts(registryPath, out baseKey, out subKey, out key);
		}

		public static RegistryKey GetKey(string baseKey, string subKey)
		{
			return ParseBaseKey(baseKey).OpenSubKey(subKey);
		}

		public static bool ValueExists(string registryPath)
		{
			string baseKey, subKey, key;
			ParseFullKey(registryPath, out baseKey, out subKey, out key);
			RegistryKey theKey = GetKey(baseKey, subKey);
			if (theKey == null) return false; //Can't open subkey, so false
			if (theKey.GetValue(key) == null) return false; //Can't get value, so false
			return true;
		}

		public static bool KeyExists(string registryPath)
		{
			string baseKey, subKey, key;
			ParseFullKey(registryPath, out baseKey, out subKey, out key);
			RegistryKey theKey = GetKey(baseKey, subKey);
			if (null == theKey) return false; //Can't open subkey, so false
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if a registry key exists.
		/// </summary>
		/// <param name="key">The base registry key of the key to check</param>
		/// <param name="subKey">The key to check</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool KeyExists(RegistryKey key, string subKey)
		{
			if (key == null)
				return false;

			foreach (string s in key.GetSubKeyNames())
			{
				if (String.Compare(s, subKey, true) == 0)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if a registry value exists.
		/// </summary>
		/// <param name="key">The base registry key of the key to check</param>
		/// <param name="subKey">Name of the group key, or string.Empty if there is no 
		/// groupKeyName.</param>
		/// <param name="regEntry">The name of the registry entry.</param>
		/// <param name="value">[out] value of the registry entry if it exists; null otherwise.</param>
		/// <returns><c>true</c> if the registry entry exists, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public static bool RegEntryExists(RegistryKey key, string subKey, string regEntry, out object value)
		{
			value = null;

			if (key == null)
				return false;

			if (!KeyExists(key, subKey))
				return false;

			if (string.IsNullOrEmpty(subKey))
			{
				value = key.GetValue(regEntry);
				if (value != null)
					return true;
				return false;
			}

			using (RegistryKey regSubKey = key.OpenSubKey(subKey))
			{
				Debug.Assert(regSubKey != null, "Should have caught this in the KeyExists call above");
				if (Array.IndexOf(regSubKey.GetValueNames(), regEntry) >= 0)
				{
					value = regSubKey.GetValue(regEntry);
					return true;
				}

				return false;
			}
		}	

		public static object GetVal(string baseKey, string subKey, string key)
		{
			return (GetKey(baseKey, subKey).GetValue(key));
		}

		public static object GetVal(string registryPath)
		{
			string baseKey, subKey, key;
			ParseFullKey(registryPath, out baseKey, out subKey, out key);
			return GetVal(baseKey, subKey, key);
		}

		/// <summary>
		/// Gets the specified registry key value if it exists. Returns null if doesn't exist
		/// </summary>
		public static object GetValIfExists(string registryPath)
		{
			if (!ValueExists(registryPath))
				return null;
			return GetVal(registryPath);
		}

		public static string GetString(string basekey, string path, string key)
		{
			return GetVal(basekey, path, key).ToString();
		}

		/// <summary>
		/// Gets the specified registry key as a string.
		/// </summary>
		/// <param name="registryPath">path to key</param>
		/// <returns>key as string, null if doesn't exist</returns>
		public static string GetString(string registryPath)
		{
			//string BaseKey, SubKey, Key;
			object val = GetValIfExists(registryPath);
			return (val != null) ? val.ToString() : null;
		}

		public static void DelKey(string baseKey, string subKey)
		{
			RegistryKey k = ParseBaseKey(baseKey);
			if (null == k.OpenSubKey(subKey)) return;
			k.DeleteSubKeyTree(subKey);
		}

		public static void DelKey(string registryPath)
		{
			string baseKey, subKey, key;
			ParseFullKey(registryPath, out baseKey, out subKey, out key);
			DelKey(baseKey, Combine(subKey, key));
		}

		public static RegistryKey MakeKey(string baseKey, string subKey)
		{
			return ParseBaseKey(baseKey).CreateSubKey(subKey);
		}

		public static void SetVal(string baseKey, string subKey, string key, object theValue)
		{
			RegistryKey k = MakeKey(baseKey, subKey);
			k.SetValue(key, theValue);
		}

		public static void SetVal(string registryPath, object theValue)
		{
			string baseKey, subKey, key;
			ParseFullKey(registryPath, out baseKey, out subKey, out key);
			SetVal(baseKey, subKey, key, theValue);
		}

        public static bool HasWritePermission(string registryPath)
        {
            try
            {
                string baseKey, subKey, key;
                ParseFullKey(registryPath, out baseKey, out subKey, out key);
                ParseBaseKey(baseKey).CreateSubKey(subKey);
            }
            catch (Exception)
            {
                return false; // user does not have write permission to the registry
            }

            return true;
        }

		#region helper methods

		/// <summary>
		/// Join two registry Parts
		/// </summary>
		/// <param name="key1"></param>
		/// <param name="key2"></param>
		private static string Combine(string key1, string key2)
		{
			var result = new StringBuilder();
			result.Append(key1);
			if (!key1.EndsWith("\\"))
				result.Append("\\");

			result.Append(key2);

			return result.ToString();
		}

		/// <summary>
		/// Split a RegistryPath into parts.
		/// If registryPath ends with a registry key seperator, then returned key should be String.Empty.
		/// </summary>
		/// <param name="registryPath">The Path the split</param>
		/// <param name="baseKey">eg. HKEY_CURRENT_USER</param>
		/// <param name="subKey">Rest of path, excluding key</param>
		/// <param name="key"></param>
		private static void ExtractRegistryParts(string registryPath, out string baseKey, out string subKey, out string key)
		{
			const char keySeperator = '\\';
			key = String.Empty;
			string[] values = registryPath.Split(new[] { keySeperator }, StringSplitOptions.RemoveEmptyEntries);
			baseKey = values.First();
			if (!registryPath.EndsWith(keySeperator.ToString()))
			{
				key = values.Last();
				subKey = String.Join(keySeperator.ToString(), values.Skip(1).Take(values.Count() - 2));
			}
			else
			{
				subKey = String.Join(keySeperator.ToString(), values.Skip(1));
			}
		}

		#endregion
	}
}
