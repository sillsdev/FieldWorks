// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportRegistryTask.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NAnt.Core;
using NAnt.Core.Attributes;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Import registry entires from a file. This is similar to what regedit.exe does, but it
	/// doesn't require
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("importregistry")]
	public class ImportRegistryTask: Task
	{
		/// <summary>Contains information about a key for delete</summary>
		private struct DeleteKeyInfo
		{
			/// <summary></summary>
			public RegistryKey Hive;
			/// <summary></summary>
			public string KeyName;
			/// <summary></summary>
			public bool IsCreated;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DeleteKeyInfo"/> struct.
			/// </summary>
			/// <param name="hive">The hive.</param>
			/// <param name="keyName">Name of the key.</param>
			/// <param name="fCreated">created</param>
			/// --------------------------------------------------------------------------------
			public DeleteKeyInfo(RegistryKey hive, string keyName, bool fCreated)
			{
				Hive = hive;
				KeyName = keyName;
				IsCreated = fCreated;
			}
		}

		private string m_RegistryFile;
		private bool m_fUnregister;
		private bool m_fPerUser;
		private StreamReader m_reader;
		private string m_nextLine;
		private Dictionary<string, DeleteKeyInfo> m_KeysToDelete = new Dictionary<string, DeleteKeyInfo>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to register or unregister.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("unregister")]
		public bool Unregister
		{
			get { return m_fUnregister; }
			set { m_fUnregister = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the registry file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("regfile", Required=true)]
		public string RegistryFile
		{
			get { return m_RegistryFile; }
			set { m_RegistryFile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the PerUser flag. If this value is <c>true</c> all references to
		/// HKEY_CLASSES_ROOT are replaced with HKEY_CURRENT_USER\Software\Classes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("peruser")]
		public bool PerUser
		{
			get { return m_fPerUser; }
			set { m_fPerUser = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			using (m_reader = new StreamReader(m_RegistryFile))
			{
				for (string line = ReadLine(); line != null; line = ReadLine())
				{
					if (line.StartsWith("["))
						ProcessKey(line);
					else
						Debug.Fail("Line doesn't start with [: " + line);
				}
			}

			DeleteKeys();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the keys.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DeleteKeys()
		{
			List<string> keys = new List<string>(m_KeysToDelete.Count);
			keys.AddRange(m_KeysToDelete.Keys);
			keys.Sort();
			for (int i = keys.Count - 1; i >= 0; i--)
			{
				DeleteKeyInfo delKeyInfo = m_KeysToDelete[keys[i]];
				string keyName = delKeyInfo.KeyName;
				RegistryKey hiveKey = delKeyInfo.Hive;
				RegistryKey parentKey;
				int iLastBackslash = keyName.LastIndexOf('\\');
				if (iLastBackslash >= 0)
				{
					parentKey = hiveKey.OpenSubKey(keyName.Substring(0, iLastBackslash), true);
					keyName = keyName.Substring(iLastBackslash + 1);
				}
				else
					parentKey = hiveKey;

				if (delKeyInfo.IsCreated)
					parentKey.DeleteSubKeyTree(keyName);
				else
					parentKey.DeleteSubKey(keyName, FailOnError);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the next line.
		/// </summary>
		/// <returns>The next line, or <c>null</c> if EOF</returns>
		/// ------------------------------------------------------------------------------------
		private string ReadLine()
		{
			if (m_nextLine != null)
			{
				string nextLine = m_nextLine;
				m_nextLine = null;
				return nextLine;
			}

			StringBuilder bldr = new StringBuilder();
			while (!m_reader.EndOfStream)
			{
				string line = m_reader.ReadLine();
				if ((line.Trim() == string.Empty || line == "REGEDIT4") && bldr.ToString().Trim().Length == 0)
					continue;
				bldr.Append(line);
				if (line.EndsWith("\\"))
					continue;
				return bldr.ToString().Trim();
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unreads a line.
		/// </summary>
		/// <param name="line">The line.</param>
		/// ------------------------------------------------------------------------------------
		private void UnreadLine(string line)
		{
			Debug.Assert(m_nextLine == null);
			m_nextLine = line;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hive.
		/// </summary>
		/// <param name="hiveName">Name of the hive.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private RegistryKey GetHive(string hiveName)
		{
			switch (hiveName)
			{
				case "HKEY_CLASSES_ROOT":
					return Registry.ClassesRoot;
				case "HKEY_CURRENT_USER":
					return Registry.CurrentUser;
				case "HKEY_LOCAL_MACHINE":
					return Registry.LocalMachine;
				default:
					Debug.Fail("Unsupported hive: " + hiveName);
					throw new BuildException(string.Format("Invalid hive: {0} in file {1}",
						hiveName, m_RegistryFile));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the key.
		/// </summary>
		/// <param name="line">The line.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessKey(string line)
		{
			line = line.Trim('[', ']');

			StringBuilder bldr = new StringBuilder();
			string[] parts = line.Split('\\');
			RegistryKey baseKey = GetHive(parts[0]);
			if (baseKey == Registry.ClassesRoot && PerUser)
						{
							baseKey = Registry.CurrentUser;
							bldr.Append(@"Software\Classes\");
						}

			bldr.Append(line.Substring(parts[0].Length + 1));

			RegistryKey key = baseKey.CreateSubKey(bldr.ToString());
			bool fCreatedKey = false;
			for (line = ReadLine(); line != null; line = ReadLine())
			{
				if (line.StartsWith("["))
				{
					UnreadLine(line);
					break;
				}
				fCreatedKey |= ProcessValue(key, line);
			}
			if (m_fUnregister && key.ValueCount == 0)
			{
				m_KeysToDelete.Add(bldr.ToString(), new DeleteKeyInfo(baseKey,
					bldr.ToString(), fCreatedKey));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="line">The line.</param>
		/// <returns><c>true</c> if we set the default value. This is an indication that we
		/// created the key, otherwise we might just set values on an existing key.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ProcessValue(RegistryKey key, string line)
		{
			if (key == null)
				return false;

			bool fDefaultValue = false;

			int iEqual = line.IndexOf("=");
			if (iEqual < 0)
				throw new BuildException(string.Format("Invalid value: {0} in file {1}", line, m_RegistryFile));

			string valueName = line.Substring(0, iEqual).Trim('"');
			string value = line.Substring(iEqual + 1);
			if (valueName == "@")
			{
				valueName = m_fUnregister ? "" : null;
				fDefaultValue = true;
			}
			if (value.StartsWith("\""))
			{
				if (m_fUnregister)
				{
					key.DeleteValue(valueName);
				}
				else
					key.SetValue(valueName, value.Trim('"'));
			}
			else
				throw new BuildException(string.Format("Unhandled value format: {0} in file {1}", value, m_RegistryFile));
			return fDefaultValue;
		}
	}
}
