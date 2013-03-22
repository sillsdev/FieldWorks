// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwRegistryHelper.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class for accessing FieldWorks-specific registry settings
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FwRegistryHelper
	{
		private static IFwRegistryHelper RegistryHelperImpl = new FwRegistryHelperImpl();

		/// <summary/>
		public static class Manager
		{
			/// <summary>
			/// Resets the registry helper. NOTE: should only be used from unit tests!
			/// </summary>
			public static void Reset()
			{
				RegistryHelperImpl = new FwRegistryHelperImpl();
			}

			/// <summary>
			/// Sets the registry helper. NOTE: Should only be used from unit tests!
			/// </summary>
			public static void SetRegistryHelper(IFwRegistryHelper helper)
			{
				RegistryHelperImpl = helper;
			}
		}

		/// <summary>Default implementation of registry helper</summary>
		private class FwRegistryHelperImpl: IFwRegistryHelper
		{
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the read-only local machine Registry key for FieldWorks.
			/// NOTE: This key is not opened for write access because it will fail on
			/// non-administrator logins.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "We're returning an object")]
			public RegistryKey FieldWorksRegistryKeyLocalMachine
			{
				get
				{
					return RegistryHelper.SettingsKeyLocalMachine(FieldWorksRegistryKeyName);
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the read-only local machine Registry key for FieldWorksBridge.
			/// NOTE: This key is not opened for write access because it will fail on
			/// non-administrator logins.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "We're returning an object")]
			public RegistryKey FieldWorksBridgeRegistryKeyLocalMachine
			{
				get
				{
					return Registry.LocalMachine.OpenSubKey("Software\\SIL\\FLEx Bridge\\7");
				}
			}

			private static string FieldWorksRegistryKeyName
			{
				get { return string.Format("{0}.0", FwUtils.SuiteVersion); }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the local machine Registry key for FieldWorks.
			/// NOTE: This will throw with non-administrative logons! Be ready for that.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "We're returning an object")]
			public RegistryKey FieldWorksRegistryKeyLocalMachineForWriting
			{
				get
				{
					return RegistryHelper.SettingsKeyLocalMachineForWriting(FieldWorksRegistryKeyName);
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the default (current user) Registry key for FieldWorks.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "We're returning an object")]
			public RegistryKey FieldWorksRegistryKey
			{
				get { return RegistryHelper.SettingsKey(FieldWorksRegistryKeyName); }
			}

			/// <summary>
			/// The value we look up in the FieldWorksRegistryKey to get(or set) the persisted user locale.
			/// </summary>
			public string UserLocaleValueName
			{
				get
				{
					return "UserWs";
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determines the installation or absence of the Paratext program by checking for the
			/// existence of the registry key that that application uses to store its program files
			/// directory in the local machine settings.
			/// This is 'HKLM\Software\ScrChecks\1.0\Program_Files_Directory_Ptw(7,8,9)'
			/// NOTE: This key is not opened for write access because it will fail on
			/// non-administrator logins.
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public bool Paratext7orLaterInstalled()
			{
				using (RegistryKey ParatextKey = Registry.LocalMachine.OpenSubKey("Software\\ScrChecks\\1.0"))
				{
					if (ParatextKey == null)
						return false;
					for (var i = 7; i < 10; i++) // Check for Paratext version 7, 8, or 9
					{
						object dummy;
						if (RegistryHelper.KeyExists(ParatextKey, "Program_Files_Directory_Ptw" + i))
							return true;
					}
					return false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the read-only local machine Registry key for FieldWorks.
		/// NOTE: This key is not opened for write access because it will fail on
		/// non-administrator logins.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static RegistryKey FieldWorksRegistryKeyLocalMachine
		{
			get { return RegistryHelperImpl.FieldWorksRegistryKeyLocalMachine; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the read-only local machine Registry key for FieldWorksBridge.
		/// NOTE: This key is not opened for write access because it will fail on
		/// non-administrator logins.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static RegistryKey FieldWorksBridgeRegistryKeyLocalMachine
		{
			get { return RegistryHelperImpl.FieldWorksBridgeRegistryKeyLocalMachine; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the local machine Registry key for FieldWorks.
		/// NOTE: This will throw with non-administrative logons! Be ready for that.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static RegistryKey FieldWorksRegistryKeyLocalMachineForWriting
		{
			get { return RegistryHelperImpl.FieldWorksRegistryKeyLocalMachineForWriting; }
		}

		/// <summary>
		/// Extension method to write a registry key to somewhere in HKLM hopfully with
		/// eleverating privileges. This method can cause the UAC dialog to be shown to the user
		/// (on Vista or later).
		/// Can throw SecurityException on permissions problems.
		/// </summary>
		public static void SetValueAsAdmin(this RegistryKey key, string name, string value)
		{
			Debug.Assert(key.Name.Substring(0, key.Name.IndexOf("\\")) == "HKEY_LOCAL_MACHINE",
				"SetValueAsAdmin should only be used for writing hklm values.");

			if (MiscUtils.IsUnix)
			{
				key.SetValue(name, value);
				return;
			}

			int startOfKey = key.Name.IndexOf("\\") + "\\".Length;
			string location = key.Name.Substring(startOfKey, key.Name.Length - startOfKey);
			location = location.Trim('\\');

			// .NET cmd processing treats \" as a single ", not part of a delimiter.
			// This can mess up closing " delimiters when the string ends with backslash.
			// To get around this, you need to add an extra \ to the end.  "D:\"  -> D:"	 "D:\\" -> D:\
			// Cmd line with 4 args: "Software\SIL\"7.0" "Projects\\Dir\" "I:\" "e:\\"
			// Interpreted as 3 args: 1)"Software\\SIL\\FieldWorks\"7.0"  2)"Projects\\\\Dir\" I:\""  3)"e:\\"
			// We'll hack the final value here to put in an extra \ for final \. "c:\\" will come through as c:\.
			string path = value;
			if (value.EndsWith("\\"))
				path = value + "\\";

			using (var process = new Process())
			{
				// Have to show window to get UAC message to allow admin action.
				//process.StartInfo.CreateNoWindow = true;
				process.StartInfo.FileName = "WriteKey.exe";
				process.StartInfo.Arguments = String.Format("LM \"{0}\" \"{1}\" \"{2}\"", location, name, path);
				// NOTE: According to information I found, these last 2 values have to be set as they are
				// (Verb='runas' and UseShellExecute=true) in order to get the UAC dialog to show.
				// On Xp (Verb='runas' and UseShellExecute=true) causes crash.
				if (MiscUtils.IsWinVistaOrNewer)
				{
					process.StartInfo.Verb = "runas";
					process.StartInfo.UseShellExecute = true;
				}
				else
				{
					process.StartInfo.UseShellExecute = false;
				}
				// Make sure the shell window is not shown (FWR-3361)
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

				// Can throw a SecurityException.
				process.Start();
				process.WaitForExit();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default (current user) Registry key for FieldWorks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static RegistryKey FieldWorksRegistryKey
		{
			get { return RegistryHelperImpl.FieldWorksRegistryKey; }
		}

		/// <summary>
		/// The value we look up in the FieldWorksRegistryKey to get(or set) the persisted user locale.
		/// </summary>
		public static string UserLocaleValueName
		{
			get { return RegistryHelperImpl.UserLocaleValueName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the installation or absence of the Paratext program by checking for the
		/// existence of the registry key that that application uses to store its program files
		/// directory in the local machine settings.
		/// This is 'HKLM\Software\ScrChecks\1.0\Program_Files_Directory_Ptw(7,8,9)'
		/// NOTE: This key is not opened for write access because it will fail on
		/// non-administrator logins.
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool Paratext7orLaterInstalled()
		{
			return RegistryHelperImpl.Paratext7orLaterInstalled();
		}
	}

}
