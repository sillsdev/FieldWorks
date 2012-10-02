// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DbVersionTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Based on NAnt's ReadRegistryTask
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Security.Permissions;
using Microsoft.Win32;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;

[assembly: RegistryPermissionAttribute(SecurityAction.RequestMinimum , Unrestricted=true)]

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// A task that writes a value to the Windows Registry
	/// </summary>
	/// <remarks>
	///     <p>
	///         Do not use a leading slash on the key value.
	///     </p>
	///     <p>
	///         Hive values can be one of the following values from the RegistryHive enum<see cref="Microsoft.Win32.RegistryHive"/>
	///         <table>
	///             <tr><td>LocalMachine</td><td></td></tr>
	///             <tr><td>CurrentUser</td><td></td></tr>
	///             <tr><td>Users</td><td></td></tr>
	///             <tr><td>ClassesRoot</td><td></td></tr>
	///         </table>
	///     </p>
	/// </remarks>
	/// <example>
	///     <para>Writes a single value from the registry</para>
	///     <code><![CDATA[<writeregistry value="sdkRoot" key="SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot" hive="LocalMachine" />]]></code>
	/// </example>
	[TaskName("writeregistry")]
	public class WriteRegistryTask : Task
	{
		private string m_Value = null;
		private string m_regKey = null;
		private string m_regKeyValueName = null;
		private RegistryHive m_regHive = RegistryHive.LocalMachine;
		private string m_regHiveString = RegistryHive.LocalMachine.ToString();


		/// <summary>
		/// <p>The registry key to read.</p>
		/// </summary>
		[TaskAttribute("key", Required=true)]
		public virtual string RegistryKey
		{
			set
			{
				string[] pathParts = value.Split("\\".ToCharArray(0,1)[0]);
				m_regKeyValueName = pathParts[pathParts.Length - 1];
				m_regKey = value.Substring(0, (value.Length - m_regKeyValueName.Length));
			}
		}

		/// <summary>The registry hive to use.</summary>
		/// <remarks>
		///     <seealso cref="Microsoft.Win32.RegistryHive"/>
		/// </remarks>
		/// <value>
		///     The enum of type <see cref="Microsoft.Win32.RegistryHive"/> values including LocalMachine, Users, CurrentUser and ClassesRoot.
		/// </value>
		[TaskAttribute("hive")]
		public virtual string RegistryHiveName
		{
			set
			{
				m_regHiveString = value;
				string[] tempRegHive = m_regHiveString.Split(" ".ToCharArray()[0]);
				if (tempRegHive.Length > 1)
					throw new BuildException("Only 1 hive is allowed");

				if (tempRegHive.Length == 1)
					m_regHive = (RegistryHive)System.Enum.Parse(typeof(RegistryHive),
						tempRegHive[0], true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value of the registry entry
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("value", Required=true)]
		public string Value
		{
			get { return m_Value; }
			set { m_Value = value; }
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			if(m_regKey == null)
				throw new BuildException("Missing registry key!");

			RegistryKey mykey = OpenRegKey(m_regKey, m_regHive);
			Log(Level.Verbose, "Setting {0} to {1}", m_regKeyValueName,
				m_Value);
			mykey.SetValue(m_regKeyValueName, m_Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the registry key. If the key doesn't exist it will be created.
		/// </summary>
		/// <param name="key">Registry sub key</param>
		/// <param name="hive">Registry hive</param>
		/// <returns>registry key</returns>
		/// ------------------------------------------------------------------------------------
		protected RegistryKey OpenRegKey(string key, RegistryHive hive)
		{
			Log(Level.Verbose, "Opening {0}:{1}", hive.ToString(), key);
			RegistryKey returnkey = GetHiveKey(hive).CreateSubKey(key);
			if(returnkey != null)
			{
				return returnkey;
			}
			throw new BuildException(String.Format(CultureInfo.InvariantCulture,
				"Registry Path Not Found! - key='{0}';hive='{1}';", key, hive));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the registry key that corresponds to the passed in hive
		/// </summary>
		/// <param name="hive">Hive</param>
		/// <returns>registry key</returns>
		/// ------------------------------------------------------------------------------------
		protected RegistryKey GetHiveKey(RegistryHive hive)
		{
			switch(hive)
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
					Log(Level.Info, "Registry not found for {0}!", hive.ToString());
					return null;
			}
		}
	}
}
