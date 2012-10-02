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
// File: DirectoryFinder.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// To find the current user's "My Documents" folder, use something like:
//		string sMyDocs = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
// See the MSDN documentation for the System.Environment.SpecialFolder enumeration for details.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using Microsoft.Win32;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	/// Summary description for DirectoryFinder.
	/// </summary>
	public class DirectoryFinder
	{
		/// <summary>
		/// A resource manager.
		/// </summary>
		protected static System.Resources.ResourceManager s_Resources;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a sub directory of the FW code directory,
		/// or return a tidied up version of the original path,
		/// if it is not in the FW code folder structure.
		/// </summary>
		/// <param name="subDirectory">examples: "WW\XAMPLE or \WW\XAMPLE"</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public string GetFWCodeSubDirectory(string subDirectory)
		{
			Debug.Assert(subDirectory != null);

			string retval = subDirectory.Trim();
			if (retval.StartsWith(@"\") || retval.StartsWith("/"))
				retval = retval.Remove(0, 1);
			string possiblePath = Path.Combine(DirectoryFinder.FWCodeDirectory, retval);
			if (Directory.Exists(possiblePath))
				retval = possiblePath;
			// Implicit 'else' assumes it to be a full path,
			// but not in the code folder structure.
			// Sure hope the caller can handle it.
			return retval;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a sub directory of the FW data directory,
		/// or return a tidied up version of the original path,
		/// if it is not in the FW data folder structure.
		/// </summary>
		/// <param name="subDirectory">examples: "Languages or \Languages"</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public string GetFWDataSubDirectory(string subDirectory)
		{
			Debug.Assert(subDirectory != null);

			string retval = subDirectory.Trim();
			if (retval.StartsWith(@"\") || retval.StartsWith("/"))
				retval = retval.Remove(0, 1);
			string possiblePath = Path.Combine(DirectoryFinder.FWDataDirectory, retval);
			if (Directory.Exists(possiblePath))
				retval = possiblePath;
			// Implicit 'else' assumes it to be a full path,
			// but not in the data folder structure.
			// Sure hope the caller can handle it.
			return retval;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a file in the FW code directory.
		/// </summary>
		/// <param name="filename">examples: "iso-8859-1.tec"</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public string GetFWCodeFile(string filename)
		{
			return System.IO.Path.Combine(FWCodeDirectory, filename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where FieldWorks code was installed,
		/// or the FWROOT environment variable, if it hasn't been installed.
		/// Will not return null.
		/// </summary>
		/// <exception cref="ApplicationException">
		/// If an installation directory could not be found.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		static public string FWCodeDirectory
		{
			get
			{
				string defaultDir = Path.Combine(Environment.ExpandEnvironmentVariables(@"%FWROOT%"),
					"DistFiles");
				object rootDir = null;
				if (FieldWorksLocalMachineRegistryKey != null)
					rootDir = FieldWorksLocalMachineRegistryKey.GetValue("RootCodeDir", defaultDir);
				if ((rootDir == null) || !(rootDir is string))
				{
					throw new ApplicationException(
						ResourceHelper.GetResourceString("kstidInvalidInstallation"));
				}
				return (string)rootDir;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where FieldWorks data was installed,
		/// or the FWROOT environment variable, if it hasn't been installed.
		/// Will not return null.
		/// </summary>
		/// <exception cref="ApplicationException">
		/// If an installation directory could not be found.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		static public string FWDataDirectory
		{
			get
			{
				string defaultDir = Path.Combine(Environment.ExpandEnvironmentVariables(@"%FWROOT%"),
					"DistFiles");
				object rootDir = null;
				if (FieldWorksLocalMachineRegistryKey != null)
					rootDir = FieldWorksLocalMachineRegistryKey.GetValue("RootDataDir", defaultDir);
				if ((rootDir == null) || !(rootDir is string))
				{
					throw new ApplicationException(
						ResourceHelper.GetResourceString("kstidInvalidInstallation"));
				}
				return (string)rootDir;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory from the registry where the ICU data is located
		/// Will not return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string GetIcuDirectory
		{
			get
			{
				string dir = Path.Combine(Environment.GetFolderPath(
					Environment.SpecialFolder.CommonProgramFiles), @"SIL\Icu40\icudt40l");
				RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\SIL");
				if (key != null)
				{
					string value = (string)key.GetValue("Icu40DataDir");
					if (value != null && value != string.Empty)
					{
						dir = value;
					}
				}
				if (dir[dir.Length-1] != '\\')
					dir += '\\';
				return dir;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the src dir (for running tests)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string FwSourceDirectory
		{
			get
			{
				object rootDir = null;
				if (FieldWorksLocalMachineRegistryKey != null)
					rootDir = FieldWorksLocalMachineRegistryKey.GetValue("RootCodeDir");
				if ((rootDir == null) || !(rootDir is string))
					throw new ApplicationException(@"You need to have the registry key LOCAL_MACHINE\SOftware\SIL\Fieldworks\RootCodeDir pointing at your Distfiles dir.");
				string fw = System.IO.Directory.GetParent((string)rootDir).ToString();
				string src = fw+@"\src";
				if(!System.IO.Directory.Exists(src))
					throw new ApplicationException (@"Could not find the src directory.  Was expecting it at: " + src);
				return src;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path name of the editorial checks directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string EditorialChecksDirectory
		{
			get
			{
				string directory = DirectoryFinder.GetFWCodeSubDirectory(@"Editorial Checks");
				if (!Directory.Exists(directory))
				{
					string msg = ResourceHelper.GetResourceString("kstidUnableToFindEdChecksFolder");
					throw new ApplicationException(string.Format(msg, directory));
				}
				return directory;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the basic editorial checks DLL. Note that this is currently the ScrChecks DLL,
		/// but if we ever split this DLL to separate Scripture-specific checks from more
		/// generic checks that are really based on the WS and could be used to check any text,
		/// then this property should be made to return the DLL containing the punctuation
		/// patterns and characters checks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string BasicEditorialChecksDll
		{
			get
			{
#if RELEASE
				try
				{
#endif
				string directory = DirectoryFinder.EditorialChecksDirectory;
				string checksDll = Path.Combine(directory, "ScrChecks.dll");
				if (!File.Exists(checksDll))
				{
					string msg = ResourceHelper.GetResourceString("kstidUnableToFindEditorialChecks");
					throw new ApplicationException(string.Format(msg, directory));
				}
				return checksDll;
#if RELEASE
				}
				catch (ApplicationException e)
				{
					throw new InstallationException(e);
				}
#endif
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the local machine Registry key for FieldWorks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public RegistryKey FieldWorksLocalMachineRegistryKey
		{
			get
			{
				// Note. We don't want to use CreateSubKey here because it will fail on
				// non-administrator logins. The user doesn't need to modify this setting.
				return Registry.LocalMachine.OpenSubKey(@"Software\SIL\FieldWorks");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dir where templates are installed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string TemplateDirectory
		{
			get
			{
				return GetFWCodeSubDirectory("Templates");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dir where data are stored
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string DataDirectory
		{
			get
			{
				return GetFWDataSubDirectory("Data");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dir where XML language definition files are stored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string LanguagesDirectory
		{
			get
			{
				return GetFWDataSubDirectory("Languages");
			}
		}
	}
}
