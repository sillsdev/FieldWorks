using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Resources;
using System.IO;
using System.Collections;
using System.Reflection;
using Microsoft.Win32;
using System.Xml;

namespace HelpTopicsChecker
{
	static class FieldWorksDirectoryFinder
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the local machine Registry key for FieldWorks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static internal RegistryKey FieldWorksLocalMachineRegistryKey
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
		/// Gets the src dir (for running tests)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static internal string FwSourceDirectory
		{
			get
			{
				string src = RootDir+@"\src";
				if(!System.IO.Directory.Exists(src))
					throw new ApplicationException (@"Could not find the src directory.  Was expecting it at: "+src);
				return src;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where FieldWorks was installed,
		/// or the FWROOT environment variable, if it hasn't been installed.
		/// Will not return null.
		/// </summary>
		/// <exception cref="ApplicationException">
		/// If an installation directory could not be found.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		static internal string FWInstallDirectory
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
					throw new ApplicationException (@"Could not find the Install directory.");
				}
				string installDir = rootDir.ToString();
				return System.IO.Path.GetFullPath(installDir);
			}
		}

		static internal string FWProgramDirectory
		{
			get
			{
				string installDir = FWInstallDirectory;
				if (installDir.ToLower().EndsWith("distfiles"))
				{
					// On a Debug build, the program directory is the Output directory.
					return System.IO.Directory.GetParent(installDir).ToString() + @"\Output\Debug";
				}
				else
				{
					return installDir;
				}
			}
		}

		static internal string RootDir
		{
			get
			{
				return System.IO.Directory.GetParent(FWInstallDirectory).ToString();
			}
		}
	}

	class HelpTopicsChecker
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new HelpTopicsCheckerSetupDlg());
		}
	}
}