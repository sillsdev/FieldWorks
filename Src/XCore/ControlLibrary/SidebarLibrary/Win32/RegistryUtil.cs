// Original author or copyright holder unknown.

using System;
using Microsoft.Win32;

namespace SidebarLibrary.Win32
{
	/// <summary>
	/// Summary description for Registry.
	/// </summary>
	public class RegistryUtil
	{
		#region Constructors
		// No need to constructo this object
		private RegistryUtil()
		{
		}
		#endregion

		#region Implementation
		static public void WriteToRegistry(RegistryKey RegHive, string RegPath, string KeyName, string KeyValue)
		{
			// Split the registry path
			string[] regStrings;
			regStrings = RegPath.Split('\\');
			// First item of array will be the base key, so be carefull iterating below
			RegistryKey[] RegKey = new RegistryKey[regStrings.Length + 1];
			RegKey[0] = RegHive;

			for( int i = 0; i < regStrings.Length; i++ )
			{
				RegKey[i + 1] = RegKey[i].OpenSubKey(regStrings[i], true);
				// If key does not exist, create it
				if (RegKey[i + 1] == null)
				{
					RegKey[i + 1] = RegKey[i].CreateSubKey(regStrings[i]);
				}
			}

			// Write the value to the registry
			try
			{
				RegKey[regStrings.Length].SetValue(KeyName, KeyValue);
			}
			catch (System.NullReferenceException)
			{
				throw(new Exception("Null Reference"));
			}
			catch (System.UnauthorizedAccessException)
			{
				throw(new Exception("Unauthorized Access"));
			}
		}

		static public string ReadFromRegistry(RegistryKey RegHive, string RegPath, string KeyName, string DefaultValue)
		{
			string[] regStrings;
			string result = "";

			regStrings = RegPath.Split('\\');
			//First item of array will be the base key, so be carefull iterating below
			RegistryKey[] RegKey = new RegistryKey[regStrings.Length + 1];
			RegKey[0] = RegHive;

			for( int i = 0; i < regStrings.Length; i++ )
			{
				RegKey[i + 1] = RegKey[i].OpenSubKey(regStrings[i]);
				if (i  == regStrings.Length - 1 )
				{
					result = (string)RegKey[i + 1].GetValue(KeyName, DefaultValue);
				}
			}
			return result;
		}

		#endregion
	}
}
