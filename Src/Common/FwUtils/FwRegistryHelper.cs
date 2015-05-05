// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwRegistryHelper.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using Microsoft.Win32;
using SIL.FieldWorks.FDO;
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
		/// <summary>
		/// TE string
		/// </summary>
		public static readonly string TranslationEditor = "Translation Editor";
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
			public FwRegistryHelperImpl()
			{
				// FWNX-1235 Mono's implementation of the "Windows Registry" on Unix uses XML files in separate folders for
				// each user and each software publisher.  We need to read Paratext's entries, so we copy theirs into ours.
				if (MiscUtils.IsUnix)
				{
					const string ptRegKey = "LocalMachine/software/scrchecks";

					var ptRegLoc = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".config/paratext/registry", ptRegKey);

#if DEBUG
					// On a developer Linux machine these are kept under output/registry. Since the program is running at output/{debug|release},
					// one level up should find the registry folder.
					var fwRegLoc = Path.Combine(
						Path.GetDirectoryName(FileUtils.StripFilePrefix(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)) ?? ".",
						"../registry", ptRegKey);
#else
					var fwRegLoc = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".config/fieldworks/registry", ptRegKey);
#endif

					if (Directory.Exists(ptRegLoc))
						DirectoryUtils.CopyDirectory(ptRegLoc, fwRegLoc, true, true);
				}
			}

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

			/// <summary>
			/// Get LocalMachine hive. (Overridable for unit tests.)
			/// </summary>
			public RegistryKey LocalMachineHive
			{
				get { return Registry.LocalMachine; }
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
					return Registry.LocalMachine.OpenSubKey("Software\\SIL\\FLEx Bridge\\" + FwUtils.SuiteVersion);
				}
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

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the default (current user) Registry key for FieldWorks without the version number.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "We're returning an object")]
			public RegistryKey FieldWorksVersionlessRegistryKey
			{
				get { return RegistryHelper.SettingsKey(); }
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


		/// <summary>
		/// Get LocalMachine hive. (Overridable for unit tests.)
		/// </summary>
		public static RegistryKey LocalMachineHive
		{
			get { return RegistryHelperImpl.LocalMachineHive; }
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
			// Cmd line with 4 args: "Software\SIL\"8" "Projects\\Dir\" "I:\" "e:\\"
			// Interpreted as 3 args: 1)"Software\\SIL\\FieldWorks\"8"  2)"Projects\\\\Dir\" I:\""  3)"e:\\"
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default (current user) Registry key for FieldWorks without the version number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static RegistryKey FieldWorksVersionlessRegistryKey
		{
			get { return RegistryHelperImpl.FieldWorksVersionlessRegistryKey; }
		}

		/// <summary>
		/// Gets the current SuiteVersion as a string
		/// </summary>
		public static string FieldWorksRegistryKeyName
		{
			get { return FwUtils.SuiteVersion.ToString(CultureInfo.InvariantCulture); }
		}

		/// <summary>
		/// It's probably a good idea to keep around the name of the old versions' keys
		/// for upgrading purposes. See UpgradeUserSettingsIfNeeded().
		/// </summary>
		internal const string OldFieldWorksRegistryKeyNameVersion7 = "7.0";

		/// <summary>
		/// It's probably a good idea to keep around the name of the old versions' keys
		/// for upgrading purposes. See UpgradeUserSettingsIfNeeded().
		/// </summary>
		internal const string OldFieldWorksRegistryKeyNameVersion8 = "8";

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
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool Paratext7orLaterInstalled()
		{
			return RegistryHelperImpl.Paratext7orLaterInstalled();
		}

		/// <summary>
		/// E.g. the first time the user runs FW9, we need to copy a bunch of registry keys
		/// from HKCU/Software/SIL/FieldWorks/7.0 -> FieldWorks/9 or
		/// from HKCU/Software/SIL/FieldWorks/8 -> FieldWorks/9.
		/// </summary>
		/// <returns>'true' if upgrade was done from any earlier version, otherwise, 'false'.</returns>
		public static bool UpgradeUserSettingsIfNeeded()
		{
			try
			{
				using (var fieldWorksVersionlessRegistryKey = FieldWorksVersionlessRegistryKey)
				{
					var v7Exists = RegistryHelper.KeyExists(fieldWorksVersionlessRegistryKey,
						OldFieldWorksRegistryKeyNameVersion7);
					var v8Exists = RegistryHelper.KeyExists(fieldWorksVersionlessRegistryKey,
						OldFieldWorksRegistryKeyNameVersion8);

					// With 'false' it won't throw an exception if the key doesn't exist.
					fieldWorksVersionlessRegistryKey.DeleteSubKeyTree(TranslationEditor, false);
					fieldWorksVersionlessRegistryKey.DeleteSubKeyTree(TranslationEditor.ToLowerInvariant(), false);

					// Go from extant settings (7 and/or 8) to 9 settings.
					if (v7Exists && v8Exists)
					{
						// Both exist? How odd.
						using (var version7Key = fieldWorksVersionlessRegistryKey.OpenSubKey(OldFieldWorksRegistryKeyNameVersion7))
						using (var version8Key = fieldWorksVersionlessRegistryKey.OpenSubKey(OldFieldWorksRegistryKeyNameVersion8, true))
						using (var version9Key = fieldWorksVersionlessRegistryKey.CreateSubKey(FieldWorksRegistryKeyName))
						{
							// Copy over almost everything from 7.0 to 8 and then to 9.
							CopyFilteredSubKeyTree(version7Key, version8Key);
							CopyFilteredSubKeyTree(version8Key, version9Key);
						}
						// After copying everything delete the old v7 & v8 keys.
						fieldWorksVersionlessRegistryKey.DeleteSubKeyTree(OldFieldWorksRegistryKeyNameVersion7);
						fieldWorksVersionlessRegistryKey.DeleteSubKeyTree(OldFieldWorksRegistryKeyNameVersion8);
						return true;
					}

					if (v7Exists)
					{
						// 7 exists, but not 8, so move from 7->9.
						using (var version7Key = fieldWorksVersionlessRegistryKey.OpenSubKey(OldFieldWorksRegistryKeyNameVersion7))
						using (var version9Key = fieldWorksVersionlessRegistryKey.CreateSubKey(FieldWorksRegistryKeyName))
						{
							// Copy over almost everything from 7.0 to 9
							// Don't copy the "launches" key or keys starting with "NumberOf"
							CopyFilteredSubKeyTree(version7Key, version9Key);
						}
						// After copying everything delete the old v7 key.
						fieldWorksVersionlessRegistryKey.DeleteSubKeyTree(OldFieldWorksRegistryKeyNameVersion7);
						return true; // Done, so quit.
					}

					if (v8Exists)
					{
						// 7 not present, but 8 is.
						using (var version8Key = fieldWorksVersionlessRegistryKey.OpenSubKey(OldFieldWorksRegistryKeyNameVersion8))
						using (var version9Key = fieldWorksVersionlessRegistryKey.CreateSubKey(FieldWorksRegistryKeyName))
						{
							// Copy over almost everything from 8 to 9
							// Don't copy the "launches" key or keys starting with "NumberOf"
							CopyFilteredSubKeyTree(version8Key, version9Key);
						}
						// After copying everything delete the old v8 key.
						fieldWorksVersionlessRegistryKey.DeleteSubKeyTree(OldFieldWorksRegistryKeyNameVersion8);
						return true;
					}
				}
			}
			catch (SecurityException se)
			{
				// What to do here? Punt!
			}
			return false;
		}

		/// <summary>
		/// Copies filtered list of keys and values from src to dest subKey recursively.
		/// </summary>
		private static void CopyFilteredSubKeyTree(RegistryKey srcSubKey, RegistryKey destSubKey)
		{
			CopyFilteredValues(srcSubKey, destSubKey);

			// Copy subkeys, except these that are not copied.
			var subkeysToRemove = new HashSet<string>
			{
				TranslationEditor.ToLowerInvariant()
			};
			foreach (var subKeyName in srcSubKey.GetSubKeyNames())
			{
				if (subkeysToRemove.Contains(subKeyName.ToLowerInvariant()))
					continue;

				using (var srcKey = srcSubKey.OpenSubKey(subKeyName))
				using (var newDestKey = destSubKey.CreateSubKey(subKeyName))
				{
					CopyFilteredSubKeyTree(srcKey, newDestKey);
				}
			}
		}

		private static void CopyFilteredValues(RegistryKey srcSubKey, RegistryKey destSubKey)
		{
			// Copy all values, except these that are not copied.
			const string NumberPrefix = "numberof";
			var valuesToBeRemoved = new HashSet<string>
			{
				"launches",
				"projectshared"
			};
			foreach (var valueName in srcSubKey.GetValueNames())
			{
				var lcValueName = valueName.ToLowerInvariant();
				if (lcValueName.StartsWith(NumberPrefix) || valuesToBeRemoved.Contains(lcValueName))
					continue;

				// Don't overwrite the value if it exists already!
				object dummyValue;
				if (RegistryHelper.RegEntryValueExists(destSubKey, string.Empty, valueName, out dummyValue))
					continue;

				var valueObject = srcSubKey.GetValue(valueName);
				destSubKey.SetValue(valueName, valueObject);
			}
		}
	}

}
