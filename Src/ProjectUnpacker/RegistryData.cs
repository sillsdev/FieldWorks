// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2003-2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RegistryData.cs
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

namespace SIL.FieldWorks.Test.ProjectUnpacker
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that deals with modifying the registry and restoring the previous state
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_rootRegKey is a reference only, so there is no need to call Dispose() on it")]
	public class RegistryData
	{
		private string m_keyPath;			// this is the path through the reg tree
		private string m_keyName;			// this is the actual element name
		private string m_savedValue;		// this is the previous value
		private RegistryKey m_rootRegKey;

		private bool m_FoundPath;			// the registry path
		private bool m_FoundKey;			// the registry element at 'path'
		private bool m_bHasBeenRestored;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Change or add the specified registry key with the specified value.
		/// </summary>
		/// <param name="root">Root key (i.e. should only be LocalMachine or CurrentUser)
		/// </param>
		/// <param name="keyPath">Key's path below root.</param>
		/// <param name="key">Key (or value) whose data will change. (Note: to set the
		/// Key's default value, use string.Empty)</param>
		/// <param name="desiredValue"></param>
		/// --------------------------------------------------------------------------------
		public RegistryData(RegistryKey root, string keyPath, string key, string desiredValue)
		{
			try
			{
				m_bHasBeenRestored = false;
				m_rootRegKey = root;
				m_keyPath = keyPath;
				m_keyName = key;

				// Try to find the key.
				using (var regKey = m_rootRegKey.OpenSubKey(m_keyPath, true))
				{
					if (regKey != null)
					{
						// The registry key and it's value exist so save the value for restoration later.
						const string noValueFound = "%No Value Found%";
						m_savedValue = (string)regKey.GetValue(m_keyName, noValueFound);
						m_FoundPath = true;
						m_FoundKey = (m_savedValue != noValueFound);
						if (desiredValue != null)
							regKey.SetValue(m_keyName, desiredValue);
						else if (m_FoundKey)
							regKey.DeleteValue(m_keyName);
					}
					else
					{
						// The registry key wasn't found so create it and set its value to the desired.
						m_FoundPath = false;
						m_FoundKey = false;
						if (desiredValue != null)
						{
							using (var newRegKey = m_rootRegKey.CreateSubKey(m_keyPath))
								newRegKey.SetValue(m_keyName, desiredValue);
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Remove the specified registry key.
		/// </summary>
		/// <param name="root">Root key (i.e. should only be LocalMachine or CurrentUser)
		/// </param>
		/// <param name="keyPath">Key's path below root.</param>
		/// <param name="key">Key (or value) whose data will change. (Note: to set the
		/// Key's default value, use string.Empty)</param>
		/// --------------------------------------------------------------------------------
		public RegistryData(RegistryKey root, string keyPath, string key) : this(root, keyPath, key, null)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void RestoreRegistryData()
		{
			if (m_bHasBeenRestored)
				return;

			m_bHasBeenRestored = true;
			try
			{
				if (m_FoundPath == false)		// case where the path and value didn't exist at start
				{
					m_rootRegKey.DeleteSubKey(m_keyPath);
				}
				else if (m_FoundKey == false)	// case where the value didn't exist at the start
				{
					using (var regKey = m_rootRegKey.OpenSubKey(m_keyPath,true))
					{
						if (regKey != null)
							regKey.DeleteValue(m_keyName);
						else
						{
							using (m_rootRegKey.CreateSubKey(m_keyPath))
							{
							}
						}
					}
				}
				else							// case where we have to restore the saved value
				{
					using (var regKey = m_rootRegKey.OpenSubKey(m_keyPath, true))
					{
						if (regKey == null)
						{
							using (var newRegKey = m_rootRegKey.CreateSubKey(m_keyPath))
								newRegKey.SetValue(m_keyName, m_savedValue);
						}
						else
							regKey.SetValue(m_keyName, m_savedValue);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="progID"></param>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		static public string GetRegisteredDLLPath(string progID)	// ECObjects.ECProject[.1]
		{
			string subKey = progID + "\\CLSID";
			string guid = string.Empty;
			using (var regKey = Registry.ClassesRoot.OpenSubKey(subKey))
			{
				if (regKey == null)
					return string.Empty;	// invalid or not registered progID
				guid = (string)regKey.GetValue(string.Empty);
				regKey.Close();
			}

			subKey = "CLSID\\" + guid + "\\InprocServer32";
			using (var regKey = Registry.ClassesRoot.OpenSubKey(subKey))
			{
				if (regKey == null)
					return string.Empty;	// registry is in a bad state [hope it's not our fault...]

				string registeredPath = (string)regKey.GetValue(string.Empty);
				regKey.Close();

				return registeredPath;
			}
		}
	}
}
