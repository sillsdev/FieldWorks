using System;
using System.IO;
using System.Reflection;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This class is used to find files and directories for an SIL app.
	/// </summary>
	public static class DirectoryFinder
	{
		private static string s_CommonAppDataFolder;

		/// <summary>
		/// Resets the static variables. Used for unit tests.
		/// </summary>
		internal static void ResetStaticVars()
		{
			s_CommonAppDataFolder = null;
		}


		/// <summary>
		/// Gets the company name (should be SIL).
		/// </summary>
		public static string CompanyName
		{
			get
			{
				return ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
					Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false))
				   .Company;
			}
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
		/// Gets the global writing system store directory. The directory is guaranteed to exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GlobalWritingSystemStoreDirectory
		{
			get { return GlobalWritingSystemRepository.CurrentVersionPath(GlobalWritingSystemRepository.DefaultBasePath); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the old global writing system store directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string OldGlobalWritingSystemStoreDirectory
		{
			get { return CommonAppDataFolder("WritingSystemStore"); }
		}
	}
}
