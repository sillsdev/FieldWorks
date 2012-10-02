// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2005' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RegistryGroup.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class manages a group of registry settings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegistryGroup: IDisposable
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

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~RegistryGroup()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_groupKey != null)
					((IDisposable)m_groupKey).Dispose();
			}
			m_groupKey = null;
			IsDisposed = true;
		}
		#endregion
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
				return bool.Parse((string)m_groupKey.GetValue(valueName));
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
				m_groupKey.SetValue(valueName, (newValue ?? string.Empty));
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
}
