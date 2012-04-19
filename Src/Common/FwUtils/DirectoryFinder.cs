// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DirectoryFinder.cs
//
// <remarks>
// To find the current user's "My Documents" folder, use something like:
//		string sMyDocs = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
// See the MSDN documentation for the System.Environment.SpecialFolder enumeration for details.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Summary description for DirectoryFinder.
	/// </summary>
	public static class DirectoryFinder
	{
		private static string s_CommonAppDataFolder;

		/// <summary>
		/// The name of the Translation Editor folder (Even though this is the same as
		/// FwUtils.ksTeAppName and FwSubKey.TE, PLEASE do not use them interchangeably. Use
		/// the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string ksTeFolderName = FwUtils.ksTeAppName;
		/// <summary>
		/// The name of the Language Explorer folder (Even though this is the same as
		/// FwUtils.ksFlexAppName and FwSubKey.LexText, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string ksFlexFolderName = FwUtils.ksFlexAppName;

		/// <summary>The name of the folder containing FLEx configuration settings</summary>
		public const string ksConfigurationSettingsDir = "ConfigurationSettings";
		/// <summary>The name of the folder containing FLEx backup settings</summary>
		public const string ksBackupSettingsDir = "BackupSettings";
		/// <summary>The name of the folder where the user can copy files for backup such as fonts and keyboards</summary>
		public const string ksSupportingFilesDir = "SupportingFiles";
		/// <summary>The default name of the folder containing LinkedFiles (media, pictures, etc) for a project</summary>
		public const string ksLinkedFilesDir = "LinkedFiles";
		/// <summary>The name of the subfolder containing media for a project</summary>
		public const string ksMediaDir = "AudioVisual";
		/// <summary>The name of the subfolder containing pictures for a project</summary>
		public const string ksPicturesDir = "Pictures";
		/// <summary>The name of the subfolder containing other LinkedFiles for a project</summary>
		public const string ksOtherLinkedFilesDir = "Others";
		/// <summary>The name of the folder containing writing systems for a project</summary>
		public const string ksWritingSystemsDir = "WritingSystemStore";
		/// <summary>The name of the folder containing temporary persisted sort sequence info for a project</summary>
		public const string ksSortSequenceTempDir = "Temp";
		/// <summary>The Scripture-specific stylesheet (ideally, this would be in a TE-specific place, but FDO needs it)</summary>
		public const string kTeStylesFilename = "TeStyles.xml";
		/// <summary>The filename of the backup settings file</summary>
		public const string kBackupSettingsFilename = "BackupSettings.xml";

		private const string ksBiblicaltermsLocFilePrefix = "BiblicalTerms-";
		private const string ksBiblicaltermsLocFileExtension = ".xml";

		/// <summary>
		/// Resets the static variables. Used for unit tests.
		/// </summary>
		internal static void ResetStaticVars()
		{
			s_CommonAppDataFolder = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the Scripture-specific stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string TeStylesPath
		{
			get { return Path.Combine(TeFolder, kTeStylesFilename); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the folder where TE-specific files are installed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string TeFolder
		{
			get { return GetFWCodeSubDirectory(ksTeFolderName); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the folder where FLEx-specific files are installed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string FlexFolder
		{
			get { return GetFWCodeSubDirectory(ksFlexFolderName); }
		}

		/// <summary>
		/// Return the folder in which FlexBridge resides, or empty string if it is not installed.
		/// </summary>
		public static string FlexBridgeFolder
		{
			get { return GetFLExBridgeFolderPath(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path for storing user-specific application data.
		/// </summary>
		/// <param name="appName">Name of the application.</param>
		/// ------------------------------------------------------------------------------------
		public static string UserAppDataFolder(string appName)
		{
			string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			return Path.Combine(Path.Combine(path, CompanyName), appName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path for storing common application data that might be shared between
		/// multiple applications and multiple users on the same machine.
		///
		/// On Windows this returns Environment.SpecialFolder.CommonApplicationData
		/// (C:\ProgramData),on Linux /var/lib/fieldworks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string CommonApplicationData
		{
			get
			{
				if (s_CommonAppDataFolder == null)
				{
					if (MiscUtils.IsUnix)
					{
						// allow to override the /var/lib/fieldworks path by setting the
						// environment variable FW_CommonAppData. Is this is needed on our CI
						// build machines.
						s_CommonAppDataFolder =
							Environment.GetEnvironmentVariable("FW_CommonAppData") ??
							"/var/lib/fieldworks";
					}
					else
					{
						s_CommonAppDataFolder =
							Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
					}
				}
				return s_CommonAppDataFolder;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a special folder, very similar to Environment.GetFolderPath. The main
		/// difference is that this method works cross-platform and does some translations.
		/// For example CommonApplicationData (/usr/share) is not writeable on Linux, so we
		/// translate that to /var/lib/fieldworks instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetFolderPath(Environment.SpecialFolder folder)
		{
			if (folder == Environment.SpecialFolder.CommonApplicationData)
				return CommonApplicationData;
			return Environment.GetFolderPath(folder);
		}

		static string s_companyName = Application.CompanyName; // default for real use; tests may override.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the name of the company used for registry settings (replaces
		/// Application.CompanyName)
		/// NOTE: THIS SHOULD ONLY BE SET IN TESTS AS THE DEFAULT Application.CompanyName IN
		/// TESTS WILL BE "nunit.org" or jetbrains.something!!!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string CompanyName
		{
			set { s_companyName = value; }
			private get
			{
				// This might be a good idea but will require all unit tests that depend on these functions to set one. Many of them
				// don't seem to affected by using an NUnit or JetBrains application name.
				//if (s_companyName.IndexOf("nunit", StringComparison.InvariantCultureIgnoreCase) >= 0 || s_companyName.IndexOf("jetbrains", StringComparison.InvariantCultureIgnoreCase) >= 0)
				//    throw new ArgumentException("CompanyName can not be NUnit.org or some variant of NUnit or jetbrains!" +
				//        " Make sure the test is overriding this property in RegistryHelper");
				return s_companyName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path for storing common application data that might be shared between
		/// multiple applications and multiple users on the same machine.
		///
		/// On Windows this returns a subdirectory of
		/// Environment.SpecialFolder.CommonApplicationData (C:\ProgramData),on Linux
		/// /var/lib/fieldworks.
		/// </summary>
		/// <param name="appName">Name of the application.</param>
		/// ------------------------------------------------------------------------------------
		public static string CommonAppDataFolder(string appName)
		{
			return Path.Combine(Path.Combine(CommonApplicationData, CompanyName), appName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the Translation Editor executable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string TeExe
		{
			get { return ExeOrDllPath("TE.exe"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the Translation Editor dynamic load library.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string TeDll
		{
			get { return ExeOrDllPath("TeDll.dll"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the FW Language Explorer executable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string FlexExe
		{
			get { return ExeOrDllPath("Flex.exe"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the FW Language Explorer dynamic load library.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string FlexDll
		{
			get { return ExeOrDllPath("LexTextDll.dll"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the Migrate SQL databases executable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string MigrateSqlDbsExe
		{
			get { return ExeOrDllPath("MigrateSqlDbs.exe"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the config file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string RemotingTcpServerConfigFile
		{
			get { return ExeOrDllPath("remoting_tcp_server.config"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the requested executable/dll file in the folder from which FW
		/// is being executed.
		/// </summary>
		/// <param name="file">Name of the file (case-sensistive, with the extension)</param>
		/// ------------------------------------------------------------------------------------
		private static string ExeOrDllPath(string file)
		{
			if (file == null)
				throw new ArgumentNullException("file");

			if (Assembly.GetEntryAssembly() == null)
			{
				// This seems to happen when tests call this method when run from NUnit
				// for some reason.

				// The following code should only run by unittests.
#if DEBUG
				const string arch = "Debug";
#else
				const string arch = "Release";
#endif
				return Path.Combine(Path.Combine(Path.Combine(Path.GetDirectoryName(FwSourceDirectory), "Output"), arch), file);
			}

			return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), file);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a sub directory of the given <paramref name="directory"/>,
		/// or return a tidied up version of the original path,
		/// if it is not in the FW code folder structure.
		/// </summary>
		/// <param name="directory">Base directory.</param>
		/// <param name="subDirectory">examples: "WW\XAMPLE or \WW\XAMPLE"</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string GetSubDirectory(string directory, string subDirectory)
		{
			Debug.Assert(subDirectory != null);

			string retval = subDirectory.Trim();
			if (retval.Length > 0 && (retval[0] == Path.DirectorySeparatorChar
				|| retval[0] == Path.AltDirectorySeparatorChar))
			{
				// remove leading directory separator from subdirectory
				retval = retval.Substring(1);
			}
			string possiblePath = Path.Combine(directory, retval);
			if (Directory.Exists(possiblePath))
				retval = possiblePath;
			// Implicit 'else' assumes it to be a full path,
			// but not in the code folder structure.
			// Sure hope the caller can handle it.

#if __MonoCS__
			else if (!Directory.Exists(retval)) // previous Substring(1) causes problem for 'full path' in Linux
				return subDirectory;
#endif

			return retval;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a sub directory of the FW code directory,
		/// or return a tidied up version of the original path,
		/// if it is not in the FW code folder structure.
		/// </summary>
		/// <param name="subDirectory">examples: "WW\XAMPLE or \WW\XAMPLE"</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetFWCodeSubDirectory(string subDirectory)
		{
			return GetSubDirectory(FWCodeDirectory, subDirectory);
		}

		private static string GetFLExBridgeFolderPath()
		{
			var key = FwRegistryHelper.FieldWorksBridgeRegistryKeyLocalMachine;
			if(key != null)
				return GetDirectory(key, "InstallationDir", "");
			return "";
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
		public static string GetFWDataSubDirectory(string subDirectory)
		{
			return GetSubDirectory(FWDataDirectory, subDirectory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a file in the FW code directory.
		/// </summary>
		/// <param name="filename">examples: "iso-8859-1.tec"</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetFWCodeFile(string filename)
		{
			return Path.Combine(FWCodeDirectory, filename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the configuration settings for the specified project.
		/// </summary>
		/// <param name="projectFolder">The path to the project folder.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetConfigSettingsDir(string projectFolder)
		{
			return Path.Combine(projectFolder, ksConfigurationSettingsDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the backup settings for the specified project
		/// </summary>
		/// <param name="projectFolder">The path to the project folder.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetBackupSettingsDir(string projectFolder)
		{
			return Path.Combine(projectFolder, ksBackupSettingsDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the fonts for the specified project.
		/// </summary>
		/// <param name="projectFolder">The path to the project folder.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetSupportingFilesDir(string projectFolder)
		{
			return Path.Combine(projectFolder, ksSupportingFilesDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the writing systems for the specified project.
		/// </summary>
		/// <param name="projectFolder">The path to the project folder.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetWritingSystemDir(string projectFolder)
		{
			return Path.Combine(projectFolder, ksWritingSystemsDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path without the root directory (i.e. make it un-rooted).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetPathWithoutRoot(string pathWithRoot)
		{
			string pathRoot = Path.GetPathRoot(pathWithRoot);
			return pathWithRoot.Substring(pathRoot.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Takes a windows path and returns it in the format which our backup zip files
		/// stores them in.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetZipfileFormatedPath(string path)
		{
			StringBuilder strBldr = new StringBuilder(path);
			string pathRoot = Path.GetPathRoot(path);
			strBldr.Remove(0, pathRoot.Length);
			// replace back slashes with forward slashes (for Windows)
			strBldr.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return strBldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the XML data file, given a project name (basically just adds the
		/// FW data XML file extension).
		/// </summary>
		/// <param name="projectName">Name of the project (not a filename).</param>
		/// ------------------------------------------------------------------------------------
		public static string GetXmlDataFileName(string projectName)
		{
			Debug.Assert(Path.GetExtension(projectName) != FwFileExtensions.ksFwDataXmlFileExtension,
				String.Format("There is a faint chance the user might have specified a real project name ending in {0} (in which case, sorry, but we're going to trim it off), but probably this is a programming error", FwFileExtensions.ksFwDataXmlFileExtension));
			// Do not use Path.ChangeExtension because it will strip off anything following a period in the project name!
			return projectName.EndsWith(FwFileExtensions.ksFwDataXmlFileExtension) ? projectName :
				projectName + FwFileExtensions.ksFwDataXmlFileExtension;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the DB4O data file, given a project name (basically just adds the
		/// FW db4o file extension).
		/// </summary>
		/// <param name="projectName">Name of the project (not a filename).</param>
		/// ------------------------------------------------------------------------------------
		public static string GetDb4oDataFileName(string projectName)
		{
			Debug.Assert(Path.GetExtension(projectName) != FwFileExtensions.ksFwDataDb4oFileExtension,
				String.Format("There is a faint chance the user might have specified a real project name ending in {0} (in which case, sorry, but we're going to trim it off), but probably this is a programming error", FwFileExtensions.ksFwDataDb4oFileExtension));
			// Do not use Path.ChangeExtension because it will strip off anything following a period in the project name!
			return projectName.EndsWith(FwFileExtensions.ksFwDataDb4oFileExtension) ? projectName :
				projectName + FwFileExtensions.ksFwDataDb4oFileExtension;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a subdirectory of FieldWorks either by reading the
		/// <paramref name="registryValue"/> or by getting <paramref name="defaultDir"/>.
		/// Will not return <c>null</c>.
		/// </summary>
		/// <param name="registryValue">The name of the registry value to read from the FW root
		/// key in HKLM.</param>
		/// <param name="defaultDir">The default directory to use if there is no value in the
		/// registry.</param>
		/// <returns>
		/// The desired subdirectory of FieldWorks (without trailing directory separator).
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static string GetDirectory(string registryValue, string defaultDir)
		{
			using (RegistryKey registryKey = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				return GetDirectory(registryKey, registryValue, defaultDir);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a subdirectory of FieldWorks either by reading the
		/// <paramref name="registryValue"/> or by getting <paramref name="defaultDir"/>.
		/// Will not return <c>null</c>.
		/// </summary>
		/// <param name="registryKey">The registry key where the value is stored.</param>
		/// <param name="registryValue">The name of the registry value under the given key that
		/// contains the desired directory.</param>
		/// <param name="defaultDir">The default directory to use if there is no value in the
		/// registry.</param>
		/// <returns>
		/// The desired subdirectory of FieldWorks (without trailing directory separator).
		/// </returns>
		/// <exception cref="ApplicationException">If the desired directory could not be found.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		private static string GetDirectory(RegistryKey registryKey, string registryValue,
			string defaultDir)
		{
			string rootDir = (registryKey == null) ? null : registryKey.GetValue(registryValue, null) as string;

			if (string.IsNullOrEmpty(rootDir) && !string.IsNullOrEmpty(defaultDir))
				rootDir = defaultDir;
			if (string.IsNullOrEmpty(rootDir))
			{
				throw new ApplicationException(
					ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
			// Hundreds of callers of this method are using Path.Combine with the results.
			// Combine only works with a root directory if it is followed by \ (e.g., c:\)
			// so we don't want to trim the \ in this situation.
			string dir = rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return dir.Length > 2 ? dir : dir + Path.DirectorySeparatorChar;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where FieldWorks code was installed (usually
		/// C:\Program Files\SIL\FieldWorks n).
		/// Will not return <c>null</c>.
		/// </summary>
		/// <exception cref="ApplicationException">If an installation directory could not be
		/// found.</exception>
		/// ------------------------------------------------------------------------------------
		public static string FWCodeDirectory
		{
			get
			{
				string defaultDir = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), CompanyName),
					string.Format("FieldWorks {0}", FwUtils.SuiteVersion));
				return GetDirectory("RootCodeDir", defaultDir);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where FieldWorks data was installed (i.e. under AppData).
		/// </summary>
		/// <exception cref="ApplicationException">If an installation directory could not be
		/// found.</exception>
		/// ------------------------------------------------------------------------------------
		public static string FWDataDirectory
		{
			get { return GetDirectory("RootDataDir", CommonAppDataFolder(string.Format("FieldWorks {0}", FwUtils.SuiteVersion))); }
		}

		private static string m_srcdir;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the src dir (for running tests)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string FwSourceDirectory
		{
			get
			{
				if (!String.IsNullOrEmpty(m_srcdir))
					return m_srcdir;
				if (MiscUtils.IsUnix)
				{
					// Linux doesn't have the registry setting, at least while running tests,
					// so we'll assume the executing assembly is $FW/Output/Debug/FwUtils.dll,
					// and the source dir is $FW/Src.
					Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
					var dir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
					dir = Path.GetDirectoryName(dir);		// strip the parent directory name (Debug)
					dir = Path.GetDirectoryName(dir);		// strip the parent directory again (Output)
					dir = Path.Combine(dir, "Src");
					if (!Directory.Exists(dir))
						throw new ApplicationException("Could not find the Src directory.  Was expecting it at: " + dir);
					m_srcdir = dir;
				}
				else
				{
				string rootDir = null;
				if (FwRegistryHelper.FieldWorksRegistryKeyLocalMachine != null)
					rootDir = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.GetValue("RootCodeDir") as string;
				if (string.IsNullOrEmpty(rootDir))
				{
					throw new ApplicationException(
						string.Format(@"You need to have the registry key {0}\RootCodeDir pointing at your DistFiles dir.",
						FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.Name));
				}
				string fw = Directory.GetParent(rootDir).FullName;
				string src = Path.Combine(fw, "Src");
				if (!Directory.Exists(src))
					throw new ApplicationException(@"Could not find the Src directory.  Was expecting it at: " + src);
					m_srcdir = src;
				}
				return m_srcdir;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path name of the editorial checks directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string EditorialChecksDirectory
		{
			get
			{
				string directory = GetFWCodeSubDirectory(@"Editorial Checks");
				if (!Directory.Exists(directory))
				{
					string msg = ResourceHelper.GetResourceString("kstidUnableToFindEditorialChecks");
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
		public static string BasicEditorialChecksDll
		{
			get
			{
#if RELEASE
				try
				{
#endif
				string directory = EditorialChecksDirectory;
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
		/// Gets the dir where templates are installed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string TemplateDirectory
		{
			get { return GetFWCodeSubDirectory("Templates"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the dir where projects are stored
		/// </summary>
		/// <exception cref="SecurityException">If user does not have permission to write to HKLM
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public static string ProjectsDirectory
		{
			get { return GetDirectory("ProjectsDir", Path.Combine(FWDataDirectory, "Projects")); }
			set
			{
				if (ProjectsDirectory == value)
					return; // no change.

				// On Vista or later OS's Writing HLKM registry keys can, by processes not running with eleverated privileges,
				// silently redirect to a virtual location and not the intended HKLM location, rather than throwing a security
				// exception. The SetValueAsAdmin method writes the registry key in a processes with hopfully elevated
				// privileges.

				using (var registryKey = FwRegistryHelper.FieldWorksRegistryKeyLocalMachineForWriting)
				{
					// We don't want unittests showing the UAC on Vista or later OS's.
					if (!MiscUtils.RunningTests && !MiscUtils.IsUnix)
						registryKey.SetValueAsAdmin("ProjectsDir", value);
					else
						registryKey.SetValue("ProjectsDir", value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given path is a direct sub folder of the projects directory.
		/// (This is typically true for the a project-specific folder.)
		/// </summary>
		/// <param name="path">The path.</param>
		/// ------------------------------------------------------------------------------------
		public static bool IsSubFolderOfProjectsDirectory(string path)
		{
			return !string.IsNullOrEmpty(path) && Path.GetDirectoryName(path) == ProjectsDirectory;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default directory for Backup files. This is per-user.
		/// </summary>
		/// <exception cref="SecurityException">If setting this value and the user does not have
		/// permission to write to HKCU (probably can never happen)</exception>
		/// ------------------------------------------------------------------------------------
		public static string DefaultBackupDirectory
		{
			get
			{
				string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				// FWNX-501: use slightly different default path on Linux
				string defaultDir = MiscUtils.IsUnix ?
					Path.Combine(Path.Combine(myDocs, "fieldworks"), "backups") :
					Path.Combine(Path.Combine(myDocs, "My FieldWorks"), "Backups");

				using (RegistryKey registryKey = FwRegistryHelper.FieldWorksRegistryKey.OpenSubKey("ProjectBackup"))
					return GetDirectory(registryKey, "DefaultBackupDirectory", defaultDir);
			}
			set
			{
				using (RegistryKey key = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("ProjectBackup"))
				{
					if (key != null)
						key.SetValue("DefaultBackupDirectory", value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the global writing system store directory. The directory is guaranteed to exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="Offending code is not executed on Linux")]
		public static string GlobalWritingSystemStoreDirectory
		{
			get
			{
				string path = CommonAppDataFolder(ksWritingSystemsDir);
				if (!Directory.Exists(path))
				{
					DirectoryInfo di = Directory.CreateDirectory(path);
					if (!MiscUtils.IsUnix)
					{
						// We don't set the permission on Linux. That is done outside of this app.
						// On Linux the run-app script checks the permissions on startup; the
						// correct permissions are set when installing the package or by the
						// system administrator.

						// NOTE: GetAccessControl/ModifyAccessRule/SetAccessControl is not implemented in Mono
						DirectorySecurity ds = di.GetAccessControl();
						var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
						AccessRule rule = new FileSystemAccessRule(sid, FileSystemRights.Write | FileSystemRights.ReadAndExecute
							| FileSystemRights.Modify, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
							PropagationFlags.InheritOnly, AccessControlType.Allow);
						bool modified;
						ds.ModifyAccessRule(AccessControlModification.Add, rule, out modified);
						di.SetAccessControl(ds);
					}
				}
				return path;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the biblical key terms localization files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string[] KeyTermsLocalizationFiles
		{
			get
			{
				// SE version doesn't install the TE folder.
				if (!Directory.Exists(TeFolder))
					return new string[]{""};
				return Directory.GetFiles(TeFolder, ksBiblicaltermsLocFilePrefix + "*" +
					ksBiblicaltermsLocFileExtension, SearchOption.TopDirectoryOnly);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the file name containing the localization of the key terms list for the
		/// given ICU locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string GetKeyTermsLocFilename(string locale)
		{
			return Path.Combine(TeFolder, ksBiblicaltermsLocFilePrefix + locale +
				ksBiblicaltermsLocFileExtension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts the locale identifier (string) from a key terms localization file name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static public string GetLocaleFromKeyTermsLocFile(string locFilename)
		{
			return Path.GetFileName(locFilename).Replace(ksBiblicaltermsLocFilePrefix,
				String.Empty).Replace(ksBiblicaltermsLocFileExtension, String.Empty);
		}

		#region ExternalLinks folders
		//This region has all methods which return the values for the ExternalLinks files associated with a project.
		//This includes .../ProjectName/LinkedFiles and all subfolders. i.e. Pictures, AudioVisual and Others.


		/// <summary>
		/// Gets the path to the standard eternal linked files directory for the specified project.
		/// </summary>
		/// <param name="projectPath">The path to the project.</param>
		/// <returns></returns>
		public static string GetDefaultLinkedFilesDir(string projectPath)
		{
			return Path.Combine(projectPath, ksLinkedFilesDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the standard media files directory for the specified project. Note
		/// that if this project keepes its externally linked files in a separate folder from
		/// the rest of the project files (such as a shared folder common to multiple projects
		/// on a server), the directory returned by this method will not actually contain any
		/// files.
		/// </summary>
		/// <param name="projectPath">The path to the project.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetDefaultMediaDir(string projectPath)
		{
			return Path.Combine(projectPath, Path.Combine(ksLinkedFilesDir, ksMediaDir));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the standard pictures directory for the specified project. Note
		/// that if this project keepes its externally linked files in a separate folder from
		/// the rest of the project files (such as a shared folder common to multiple projects
		/// on a server), the directory returned by this method will not actually contain any
		/// files.
		/// </summary>
		/// <param name="projectPath">The path to the project.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetDefaultPicturesDir(string projectPath)
		{
			return Path.Combine(projectPath, Path.Combine(ksLinkedFilesDir, ksPicturesDir));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the standard directory for other externally linked project files.
		/// Note that if this project keepes its externally linked files in a separate folder
		/// from the rest of the project files (such as a shared folder common to multiple
		/// projects on a server), the directory returned by this method will not actually
		/// contain any files.
		/// </summary>
		/// <param name="projectPath">The path to the project.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetDefaultOtherExternalFilesDir(string projectPath)
		{
			return Path.Combine(projectPath, Path.Combine(ksLinkedFilesDir, ksOtherLinkedFilesDir));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the media files directory for the project.
		/// </summary>
		/// <param name="projectLinkedFilesPath">The project's LinkedFiles path. (eg. m_cache.LangProject.LinkedFilesRootDir)</param>
		/// ------------------------------------------------------------------------------------
		public static string GetMediaDir(string projectLinkedFilesPath)
		{
			return Path.Combine(projectLinkedFilesPath, ksMediaDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the pictures directory for the project.
		/// </summary>
		/// <param name="projectLinkedFilesPath">The project's LinkedFiles path. (eg. m_cache.LangProject.LinkedFilesRootDir)</param>
		/// ------------------------------------------------------------------------------------
		public static string GetPicturesDir(string projectLinkedFilesPath)
		{
			return Path.Combine(projectLinkedFilesPath, ksPicturesDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the directory for other externally linked project files.
		/// </summary>
		/// <param name="projectLinkedFilesPath">The project's LinkedFiles path. (eg. m_cache.LangProject.LinkedFilesRootDir)</param>
		/// ------------------------------------------------------------------------------------
		public static string GetOtherExternalFilesDir(string projectLinkedFilesPath)
		{
			return Path.Combine(projectLinkedFilesPath, ksOtherLinkedFilesDir);
		}

		#endregion
	}

	/// <summary>
	/// This class is designed for converting between relative paths and full paths for the LinkedFiles of a FW project
	/// </summary>
	public class DirectoryFinderRelativePaths
	{
		/// <summary>Substitution string for a path that is under the LinkedFiles directory.</summary>
		public const string ksLFrelPath = "%lf%";
		/// <summary>Substitution string for a path that is under the project's directory.</summary>
		public const string ksProjectRelPath = "%proj%";
		/// <summary>Substitution string for a path that is under the default directory for projects.</summary>
		public const string ksProjectsRelPath = "%Projects%";
		/// <summary>Substitution string for a path that is under the My Documents directory.</summary>
		public const string ksMyDocsRelPath = "%MyDocuments%";
		/// <summary>Substitution string for a path that is under the Shared Application Data directory.</summary>
		public const string ksCommonAppDataRelPath = "%CommonApplicationData%";

		#region Methods to covert between RelativePaths and FullPaths

		/// <summary>
		/// If a filePath is stored in the format  %lf%\path\filename then this method returns the full path.
		/// Otherwise return null
		/// </summary>
		/// <param name="relativePath"></param>
		/// <param name="projectLinkedFilesPath"></param>
		/// <returns></returns>
		public static String GetFullFilePathFromRelativeLFPath(string relativePath, string projectLinkedFilesPath)
		{
			String fullfilePath = null;
			fullfilePath = GetFullPathForRelativePath(relativePath, ksLFrelPath, projectLinkedFilesPath);

			if (String.IsNullOrEmpty(fullfilePath))
				return null;
			return fullfilePath;
		}

		/// <summary>
		/// If a file path is non rooted then return combination of the linkedFiledRootDir and the relative
		/// path.  Otherwise just return the full path passed in as an arguement.
		/// </summary>
		/// <param name="relativeLFPath"></param>
		/// <param name="linkedFilesRootDir"></param>
		/// <returns></returns>
		public static String GetFullPathFromRelativeLFPath(string relativeLFPath, string linkedFilesRootDir)
		{
			if (Path.IsPathRooted(relativeLFPath))
				return relativeLFPath;
			else
				return Path.Combine(linkedFilesRootDir, relativeLFPath);
		}


		/// <summary>
		/// If the path is relative to the project's linkedFiles path then substitute %lf%
		/// and return it. Otherwise return an empty string
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="projectLinkedFilesPath"></param>
		/// <returns></returns>
		public static string GetRelativeLFPathFromFullFilePath(string filePath,
			string projectLinkedFilesPath)
		{
			if (string.IsNullOrEmpty(projectLinkedFilesPath))
				return string.Empty;

			var linkedFilesPathLowercaseRoot = GetPathWithLowercaseRoot(filePath);

			var relativePath = GetRelativePathIfExists(ksLFrelPath, linkedFilesPathLowercaseRoot,
				projectLinkedFilesPath);
			if (!string.IsNullOrEmpty(relativePath))
				return relativePath;


			//Just return the complete path if we cannot find a relative path.
			return string.Empty;
		}

		/// <summary>
		/// If the specified path starts with the LinkedFiles root directory then return
		/// the part after the linkedFilesRootDir;
		/// otherwise if it is a file path at all convert it to the current platform and return it;
		/// otherwise (it's a URL, determined by containing a colon after more than one initial character)
		/// return null to indicate no change made.
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="linkedFilesRootDir"></param>
		/// <returns></returns>
		public static string GetRelativeLinkedFilesPath(string filePath,
			string linkedFilesRootDir)
		{
			if (filePath.IndexOf(':') > 1)
			{
				// It's a URL, not a path at all; don't mess with it.
				return null;
			}
			string directory = FileUtils.ChangePathToPlatform(linkedFilesRootDir);
			string relativePath = FileUtils.ChangePathToPlatform(filePath);

			// Does the specified path start with the LinkedFiles root directory?
			if (relativePath.StartsWith(directory, true, System.Globalization.CultureInfo.InvariantCulture) &&
				relativePath.Length > directory.Length + 1)
			{
				// Keep the portion of the specified path that is a subfolder of
				// the LinkedFiles folder and make sure to strip off an initial
				// path separator if there is one.
				relativePath = relativePath.Substring(directory.Length);
				if (relativePath[0] == Path.DirectorySeparatorChar)
					relativePath = relativePath.Substring(1);
			}
			return relativePath;
		}


		/// <summary>
		/// Return the fullPath for a project's LinkedFiles based on the relative path that was persisted.
		/// If no match on a relativePath match is made then return the relativePath passed in assuming it
		/// is actually a full path.
		/// </summary>
		/// <param name="relativePath"></param>
		/// <param name="projectPath"></param>
		/// <returns></returns>
		public static String GetLinkedFilesFullPathFromRelativePath(string relativePath, String projectPath)
		{
			String fullPath = null;
			fullPath = GetFullPathForRelativePath(relativePath, ksProjectRelPath, projectPath);

			if (String.IsNullOrEmpty(fullPath))
				fullPath = GetFullPathForRelativePath(relativePath, ksProjectsRelPath,
					DirectoryFinder.ProjectsDirectory);
			if (String.IsNullOrEmpty(fullPath))
				fullPath = GetFullPathForRelativePath(relativePath, ksCommonAppDataRelPath,
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			if (String.IsNullOrEmpty(fullPath))
				fullPath = GetFullPathForRelativePath(relativePath, ksMyDocsRelPath,
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.MyDocuments));
			if (String.IsNullOrEmpty(fullPath))
				return relativePath;
			return fullPath;
		}

		private static String GetFullPathForRelativePath(String relativePath, String relativePart, String fullPathReplacement)
		{
			if (relativePath.StartsWith(relativePart))
			{
				var length = relativePart.Length;
				var restOfPath = relativePath.Substring(length, relativePath.Length - length);
				return fullPathReplacement + restOfPath;
			}
			else
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// Get a relative path for the LinkedFilesPath which we will persist to be used when
		/// restoring a project.
		/// </summary>
		/// <param name="linkedFilesFullPath"></param>
		/// <param name="projectPath"></param>
		/// <param name="projectName"></param>
		/// <returns></returns>
		public static string GetLinkedFilesRelativePathFromFullPath(string linkedFilesFullPath,
			string projectPath, string projectName)
		{
			var linkedFilesPathLowercaseRoot = GetPathWithLowercaseRoot(linkedFilesFullPath);
			// Case where the ExternalLinks folder is located somewhere under the project folder.
			// This is the default location.
			var relativePath = GetRelativePathIfExists(ksProjectRelPath, linkedFilesPathLowercaseRoot,
				projectPath);
			if (!string.IsNullOrEmpty(relativePath))
				return relativePath;
			// GetRelativePathIfExists may miss a case where, say, projectPath is
			// \\ls-thomson-0910.dallas.sil.org\Projects\MyProj, and linkedFilesFullPath is
			// C:\Documents and settings\All Users\SIL\FieldWorks\Projects\MyProj\LinkedFiles
			// Even though the MyProj directory in both paths is the same directory.
			// It's important to catch this case and return a relative path.
			var projectFolderName = Path.GetFileName(projectPath);
			var projectsPath = Path.GetDirectoryName(projectPath);
			var allProjectsName = Path.GetFileName(projectsPath);
			var match = Path.Combine(allProjectsName, projectFolderName);
			int index = linkedFilesFullPath.IndexOf(match, StringComparison.InvariantCultureIgnoreCase);
			if (index >= 0)
			{
				// There's a very good chance these are the same folders!
				var alternateProjectPath = linkedFilesFullPath.Substring(0, index + match.Length);
				if (Directory.Exists(alternateProjectPath) &&
					Directory.GetLastWriteTime(alternateProjectPath) == Directory.GetLastWriteTime(projectPath))
				{
					// They ARE the same directory! (I suppose we could miss if someone wrote to it at the
					// exact wrong moment, but we shouldn't be changing this setting while shared, anyway.)
					return ksProjectRelPath + linkedFilesFullPath.Substring(index + match.Length);
				}
			}

			//See if linkedFilesPath begins with one of the other standard paths.

			// Case where user is presumably having a LinkedFiles folder shared among a number
			// of projects under the Projects folder. That would be a good reason to put it in
			// the projects folder common to all projects.
			relativePath = GetRelativePathIfExists(ksProjectsRelPath, linkedFilesPathLowercaseRoot,
				DirectoryFinder.ProjectsDirectory);
			if (!String.IsNullOrEmpty(relativePath))
				return relativePath;

			// Case where the user has the LinkedFiles folder in a shared folder.
			relativePath = GetRelativePathIfExists(ksCommonAppDataRelPath,
				linkedFilesPathLowercaseRoot, DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			if (!string.IsNullOrEmpty(relativePath))
				return relativePath;

			// Case where the user has the LinkedFiles folder in their MyDocuments folder
			relativePath = GetRelativePathIfExists(ksMyDocsRelPath,
				linkedFilesPathLowercaseRoot,
				DirectoryFinder.GetFolderPath(Environment.SpecialFolder.MyDocuments));
			if (!string.IsNullOrEmpty(relativePath))
				return relativePath;

			//Just return the complete path if we cannot find a relative path.
			return linkedFilesFullPath;
		}

		private static string GetRelativePathIfExists(string relativePart, string fullPath,
			string parentPath)
		{
			var parentPathLowerCaseRoot = GetPathWithLowercaseRoot(parentPath);
			if (!string.IsNullOrEmpty(parentPathLowerCaseRoot) &&
				fullPath.StartsWith(parentPathLowerCaseRoot))
			{
				var length = parentPath.Length;
				var restOfPath = fullPath.Substring(length, fullPath.Length - length);
				return relativePart + restOfPath;
			}
			return string.Empty;
		}

		private static string GetPathWithLowercaseRoot(string path)
		{
			try
			{
				var rootOfPath = Path.GetPathRoot(path);
				return rootOfPath.ToLowerInvariant() +
					   path.Substring(rootOfPath.Length, path.Length - rootOfPath.Length);
			}
			catch (ArgumentException e)
			{
				return path.ToLowerInvariant();
			}
		}


		#endregion
	}
}
