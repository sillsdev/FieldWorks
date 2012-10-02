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
// File: PathwayUtils.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.IO;

namespace SIL.Utils
{
	public class PathwayUtils
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory for the Pathway application or string.Empty if the directory name
		/// is not in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string PathwayInstallDirectory
		{
			get
			{
				object regObj;
				if (RegistryHelper.RegEntryExists(RegistryHelper.CompanyKey, SilSubKey.Pathway,
					"PathwayDir", out regObj))
				{
					return (string)regObj;
				}
				if (RegistryHelper.RegEntryExists(RegistryHelper.CompanyKeyLocalMachine, SilSubKey.Pathway,
					"PathwayDir", out regObj))
				{
					return (string) regObj;
				}
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether SIL Pathway is installed for Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsPathwayForScrInstalled
		{
			get
			{
				return CheckPathwayInstallation(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether SIL Pathway is installed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsPathwayInstalled
		{
			get
			{
				return CheckPathwayInstallation(false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the Pathway installation for files.
		/// </summary>
		/// <param name="fSupportScripture">if set to <c>true</c> check the installation to contain
		/// files to support export of Scripture to Pathway; <c>false</c> to check for a
		/// standard Pathway installation.</param>
		/// <returns><c>true</c> if Pathway is installed as specified</returns>
		/// ------------------------------------------------------------------------------------
		private static bool CheckPathwayInstallation(bool fSupportScripture)
		{
			string pathwayDirectory = PathwayInstallDirectory;
			if (string.IsNullOrEmpty(pathwayDirectory))
				return false;

			string psExportDllPath = Path.Combine(pathwayDirectory, "PsExport.dll");
			if (!File.Exists(psExportDllPath))
				return false;

			string scrFilePath = Path.Combine(pathwayDirectory, "ScriptureStyleSettings.xml");
			if (fSupportScripture && !File.Exists(scrFilePath))
				return false;

			return true;
		}
	}
}
