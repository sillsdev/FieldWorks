// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RegistryHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This provides a string enumeration for subkeys of HKCU\Software\SIL\FieldWorks.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwSubKey
	{
		/// <summary></summary>
		public const string FW = "";
		/// <summary></summary>
		public const string TE = "Translation Editor";
		/// <summary></summary>
		public const string LexText = "Language Explorer";
		/// <summary></summary>
		public const string LingCommonDialogs = "LingCmnDlgs";
		/// <summary></summary>
		public const string ProjectBackup = "ProjectBackup";

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a subkey of HKCU\Software\SIL\FieldWorks.
		/// </summary>
		/// <param name="subKey">The subkey under FieldWorks (typically an app name)</param>
		/// <returns></returns>
		/// ----------------------------------------------------------------------------------------
		public static RegistryKey SettingsKey(string subKey)
		{
			return Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\" +
				(subKey == null ? string.Empty : subKey));
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an application- and database-specific subkey of HKCU\Software\SIL\FieldWorks.
		/// </summary>
		/// <param name="subKey">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="sServerName">The name of the database server</param>
		/// <param name="sDbName">The name of the database</param>
		/// <returns>an application- and database-specific subkey of HKCU\Software\SIL\FieldWorks
		/// </returns>
		/// ----------------------------------------------------------------------------------------
		public static RegistryKey SettingsKey(string subKey, string sServerName, string sDbName)
		{
			if (subKey == string.Empty)
				return SettingsKey(sServerName + @"\" + sDbName);
			else
				return SettingsKey(subKey + @"\" + sServerName + @"\" + sDbName);
		}
	}

	#region RegistryHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Misc. static methods for dealing with the registry.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegistryHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if a registry key exists.
		/// </summary>
		/// <param name="appKey">The application key whose existence is being checked.</param>
		/// <param name="groupKeyName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool KeyExists(RegistryKey appKey, string groupKeyName)
		{
			bool exists = false;

			using (RegistryKey key = appKey.OpenSubKey(groupKeyName))
			{
				exists = (key != null);
			}

			return exists;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if a registry value exists.
		/// </summary>
		/// <param name="appKey">The application key whose existence is being checked.</param>
		/// <param name="groupKeyName">Name of the group key, or string.Empty if there is no
		/// groupKeyName.</param>
		/// <param name="regEntry">The name of the registry entry.</param>
		/// <returns><c>true</c> if the registry entry exists, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public static bool RegEntryExists(RegistryKey appKey, string groupKeyName, string regEntry)
		{
			if (groupKeyName != string.Empty)
			{
				using (RegistryKey key = appKey.OpenSubKey(groupKeyName))
				{
					if (key == null)
						return false;

					return key.GetValue(regEntry) != null;
				}
			}
			return appKey.GetValue(regEntry) != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes a float setting to the registry in the en-US culture. This is done to
		/// prevent problems with registry values when changing UI cultures.
		/// </summary>
		/// <param name="key">The registry key to write</param>
		/// <param name="name">Name of the item</param>
		/// <param name="floatValue">The float value to write</param>
		/// ------------------------------------------------------------------------------------
		public static void WriteFloatSetting(RegistryKey key, string name, float floatValue)
		{
			key.SetValue(name, floatValue.ToString(CultureInfo.GetCultureInfo("en-US")));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the float setting from the registry
		/// </summary>
		/// <param name="key">The key to read</param>
		/// <param name="name">Name of the value</param>
		/// <param name="defaultValue">default value to return if the value does not exist</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static float ReadFloatSetting(RegistryKey key, string name, float defaultValue)
		{
			string val = (string)key.GetValue(name);
			if (val == null)
				return defaultValue;

			try
			{
				CultureInfo enCi = CultureInfo.GetCultureInfo("en-US");
				string currDecSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
				if (currDecSep != enCi.NumberFormat.NumberDecimalSeparator)
					val = val.Replace(currDecSep, enCi.NumberFormat.NumberDecimalSeparator);

				return float.Parse(val, enCi);
			}
			catch
			{
				return defaultValue;
			}
		}
	}

	#endregion

	#region RegistryBoolSetting class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class manages a boolean setting in the registry with caching.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegistryBoolSetting
	{
		private bool m_value;
		private string m_keyName;
		private RegistryKey m_subKey;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a boolean registry
		/// value that is DB-specific.
		/// </summary>
		/// <remarks>Use the other version for settings that are both user- AND DB-specific.
		/// Use the other version of the constructor for user-specific settings.
		/// </remarks>
		/// <param name="subKeyName">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="sServerName">The name of the database server</param>
		/// <param name="sDbName">The name of the database</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting(string subKeyName, string sServerName, string sDbName,
			string keyName, bool defaultValue) :
			this(FwSubKey.SettingsKey(subKeyName, sServerName, sDbName), keyName, defaultValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a boolean registry
		/// value.
		/// </summary>
		/// <remarks>Use this version of the constructor for user-specific settings. Use the
		/// other version for settings that are both user- AND DB-specific.
		/// </remarks>
		/// <param name="subKeyName">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting(string subKeyName, string keyName, bool defaultValue) :
			this(FwSubKey.SettingsKey(subKeyName), keyName, defaultValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a boolean registry
		/// value.
		/// </summary>
		/// <param name="subKey">The subkey under FieldWorks (typically an app name).</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting(RegistryKey subKey, string keyName, bool defaultValue)
		{
			m_subKey = subKey;
			m_keyName = keyName;
			m_value = Convert.ToBoolean(m_subKey.GetValue(keyName, defaultValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a boolean registry
		/// value for use in views where we do not want to persist settings or in tests.
		/// </summary>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting(string keyName, bool defaultValue)
		{
			m_subKey = null;
			m_keyName = keyName;
			m_value = defaultValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the value of the setting
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool Value
		{
			get {return m_value;}
			set
			{
				m_value = value;
				if (m_subKey != null)
					m_subKey.SetValue(m_keyName, m_value);
			}
		}
	}
	#endregion

	#region RegistryIntSetting class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class manages a integer (DWORD) setting in the registry with caching.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegistryIntSetting
	{
		private int m_value;
		private string m_keyName;
		private RegistryKey m_subKey;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a DWORD registry
		/// value that is DB-specific.
		/// </summary>
		/// <remarks>Use the other version for settings that are both user- AND DB-specific.
		/// Use the other version of the constructor for user-specific settings.
		/// </remarks>
		/// <param name="subKeyName">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="sServerName">The name of the database server</param>
		/// <param name="sDbName">The name of the database</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryIntSetting(string subKeyName, string sServerName, string sDbName,
			string keyName, int defaultValue) :
			this(FwSubKey.SettingsKey(subKeyName, sServerName, sDbName), keyName, defaultValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a DWORD registry
		/// value.
		/// </summary>
		/// <remarks>Use this version of the constructor for user-specific settings. Use the
		/// other version for settings that are both user- AND DB-specific.
		/// </remarks>
		/// <param name="subKeyName">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryIntSetting(string subKeyName, string keyName, int defaultValue) :
			this(FwSubKey.SettingsKey(subKeyName), keyName, defaultValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving an integer registry
		/// value.
		/// </summary>
		/// <param name="subKey">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryIntSetting(RegistryKey subKey, string keyName, int defaultValue)
		{
			m_subKey = subKey;
			m_keyName = keyName;
			m_value = Convert.ToInt32(m_subKey.GetValue(keyName, defaultValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the value of the setting
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int Value
		{
			get {return m_value;}
			set
			{
				m_value = value;
				m_subKey.SetValue(m_keyName, m_value);
			}
		}
	}
	#endregion

	#region RegistryStringSetting class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class manages a string setting in the registry with caching.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegistryStringSetting
	{
		private string m_value;
		private string m_keyName;
		private RegistryKey m_subKey;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a string registry
		/// value that is DB-specific.
		/// </summary>
		/// <remarks>Use the other version for settings that are both user- AND DB-specific.
		/// Use the other version of the constructor for user-specific settings.
		/// </remarks>
		/// <param name="subKeyName">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="sServerName">The name of the database server</param>
		/// <param name="sDbName">The name of the database</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryStringSetting(string subKeyName, string sServerName, string sDbName,
			string keyName, string defaultValue) :
			this(FwSubKey.SettingsKey(subKeyName, sServerName, sDbName), keyName, defaultValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a string registry
		/// value.
		/// </summary>
		/// <remarks>Use this version of the constructor for user-specific settings. Use the
		/// other version for settings that are both user- AND DB-specific.
		/// </remarks>
		/// <param name="subKeyName">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// unitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryStringSetting(string subKeyName, string keyName, string defaultValue) :
			this(FwSubKey.SettingsKey(subKeyName), keyName, defaultValue)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving a string registry
		/// value.
		/// </summary>
		/// <param name="subKey">The subkey under FieldWorks (typically an app name)</param>
		/// <param name="keyName">The key whose value is to be stored/retrieved</param>
		/// <param name="defaultValue">The default value to use when retrieving the value of an
		/// uninitialized key</param>
		/// ------------------------------------------------------------------------------------
		public RegistryStringSetting(RegistryKey subKey, string keyName, string defaultValue)
		{
			m_subKey = subKey;
			m_keyName = keyName;
			m_value = (string)m_subKey.GetValue(keyName, defaultValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the value of the setting
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string Value
		{
			get {return m_value;}
			set
			{
				m_value = value;
				m_subKey.SetValue(m_keyName, m_value);
			}
		}
	}
	#endregion

	#region RegistryGroup class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class manages a group of registry settings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegistryGroup
	{
		/// <summary></summary>
		protected RegistryKey m_appKey;
		/// <summary></summary>
		protected RegistryKey m_groupKey;
		/// <summary></summary>
		protected string m_groupKeyName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RegistryGroup"/> class.
		/// </summary>
		/// <remarks>This version is used by the derived class InMemoryRegistryGroup which
		/// gets used in tests.</remarks>
		/// ------------------------------------------------------------------------------------
		protected RegistryGroup()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for instantiating an object for setting\retrieving values belonging to
		/// a registry subkey.
		/// </summary>
		/// <remarks>Use this version of the constructor for user-specific settings. Use the
		/// other version for settings that are both user- AND DB-specific.
		/// </remarks>
		/// <param name="appKey">The FieldWorks application's subkey below which,
		/// a group key will be added or retrieved.</param>
		/// <param name="groupKeyName">The key whose values are to be stored/retrieved</param>
		/// ------------------------------------------------------------------------------------
		public RegistryGroup(RegistryKey appKey, string groupKeyName)
		{
			Debug.Assert(appKey != null);
			Debug.Assert(groupKeyName != null && groupKeyName.Trim() != string.Empty);

			m_appKey = appKey;
			m_groupKeyName = groupKeyName.Trim();
			m_groupKey = appKey.CreateSubKey(m_groupKeyName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the registry group from from the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Delete()
		{
			m_appKey.DeleteSubKeyTree(m_groupKeyName);
			m_appKey = null;
			m_groupKey = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full group key.
		/// </summary>
		/// <value>The group key.</value>
		/// ------------------------------------------------------------------------------------
		public RegistryKey GroupKey
		{
			get { return m_groupKey; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string value from the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is retrieved.</param>
		/// <param name="defaultValue">Value to return if there is no value in keyName.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetStringValue(string valueName, string defaultValue)
		{
			return (m_groupKey == null ?
				defaultValue : (string)m_groupKey.GetValue(valueName, defaultValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a float value from the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is retrieved.</param>
		/// <param name="defaultValue">Value to return if there is no value in keyName.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual float GetFloatValue(string valueName, float defaultValue)
		{
			return (m_groupKey == null ?
				defaultValue : RegistryHelper.ReadFloatSetting(m_groupKey, valueName, defaultValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a boolean value from the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is retrieved.</param>
		/// <param name="defaultValue">Value to return if there is no value in keyName.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool GetBoolValue(string valueName, bool defaultValue)
		{
			try
			{
				return bool.Parse(m_groupKey.GetValue(valueName) as string);
			}
			catch
			{
				return defaultValue;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an integer value from the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is retrieved.</param>
		/// <param name="defaultValue">Value to return if there is no value in keyName.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetIntValue(string valueName, int defaultValue)
		{
			return (m_groupKey == null ?
				defaultValue : (int)m_groupKey.GetValue(valueName, defaultValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an string value in the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is stored.</param>
		/// <param name="newValue">Value to store.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetStringValue(string valueName, string newValue)
		{
			if (m_groupKey != null)
				m_groupKey.SetValue(valueName, (newValue == null ? string.Empty : newValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an float value in the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is stored.</param>
		/// <param name="newValue">Value to store.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetFloatValue(string valueName, float newValue)
		{
			if (m_groupKey != null)
				RegistryHelper.WriteFloatSetting(m_groupKey, valueName, newValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an boolean value in the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is stored.</param>
		/// <param name="newValue">Value to store.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetBoolValue(string valueName, bool newValue)
		{
			if (m_groupKey != null)
				m_groupKey.SetValue(valueName, newValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an integer value in the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is stored.</param>
		/// <param name="newValue">Value to store.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetIntValue(string valueName, int newValue)
		{
			if (m_groupKey != null)
				m_groupKey.SetValue(valueName, newValue);
		}
	}
	#endregion
}
