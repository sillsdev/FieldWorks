// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2006' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InMemoryRegistryGroup.cs
// Responsibility: TE team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Win32;
using SIL.Utils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Registry group for testing that stores all values in in-memory dictionaries.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InMemoryRegistryGroup : RegistryGroup
	{
		private Dictionary<string, string> m_StringRegistry = new Dictionary<string, string>();
		private Dictionary<string, bool> m_BoolRegistry = new Dictionary<string, bool>();
		private Dictionary<string, int> m_IntRegistry = new Dictionary<string, int>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:InMemoryRegistryGroup"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InMemoryRegistryGroup(): this(null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyRegGroup"/> class.
		/// </summary>
		/// <param name="appKey">The app key.</param>
		/// <param name="groupKeyName">Name of the group key.</param>
		/// ------------------------------------------------------------------------------------
		public InMemoryRegistryGroup(RegistryKey appKey, string groupKeyName)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a boolean value from the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is retrieved.</param>
		/// <param name="defaultValue">Value to return if there is no value in keyName.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool GetBoolValue(string valueName, bool defaultValue)
		{
			if (m_BoolRegistry.ContainsKey(valueName))
				return m_BoolRegistry[valueName];
			return defaultValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an integer value from the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is retrieved.</param>
		/// <param name="defaultValue">Value to return if there is no value in keyName.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetIntValue(string valueName, int defaultValue)
		{
			if (m_IntRegistry.ContainsKey(valueName))
				return m_IntRegistry[valueName];
			return defaultValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string value from the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is retrieved.</param>
		/// <param name="defaultValue">Value to return if there is no value in keyName.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string GetStringValue(string valueName, string defaultValue)
		{
			if (m_StringRegistry.ContainsKey(valueName))
				return m_StringRegistry[valueName];
			return defaultValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an boolean value in the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is stored.</param>
		/// <param name="newValue">Value to store.</param>
		/// ------------------------------------------------------------------------------------
		public override void SetBoolValue(string valueName, bool newValue)
		{
			m_BoolRegistry[valueName] = newValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an integer value in the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is stored.</param>
		/// <param name="newValue">Value to store.</param>
		/// ------------------------------------------------------------------------------------
		public override void SetIntValue(string valueName, int newValue)
		{
			m_IntRegistry[valueName] = newValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets an string value in the registry group.
		/// </summary>
		/// <param name="valueName">Name of item in group whose value is stored.</param>
		/// <param name="newValue">Value to store.</param>
		/// ------------------------------------------------------------------------------------
		public override void SetStringValue(string valueName, string newValue)
		{
			m_StringRegistry[valueName] = newValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the registry group from from the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Delete()
		{
			m_StringRegistry.Clear();
			m_BoolRegistry.Clear();
			m_IntRegistry.Clear();
		}
	}
}