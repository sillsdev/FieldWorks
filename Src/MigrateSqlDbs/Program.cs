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
// File: Program.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using Palaso.WritingSystems.Migration;
using Palaso.WritingSystems.Migration.WritingSystemsLdmlV0To1Migration;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

// we can't use an exit code < -1 on Linux. However, this app won't work on Linux anyways
// since we don't have MS SQL Server there.
[assembly:SuppressMessage("Gendarme.Rules.Portability", "ExitCodeIsLimitedOnUnixRule",
		Justification="Not intended to be run on Linux")]

namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This program displays the list of FieldWorks SQL Server projects on the current machine,
	/// and allows the user to select any or all of them for conversion to the FieldWorks 7.0
	/// format.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	static class Program
	{
		static bool s_fDebug = false;
		static bool s_fAutoClose = false;
		static bool s_fMigrateChars = false;
		/// <summary>
		/// -1 means we couldn't even try to migrate anything,
		/// 0 means that either there was no data to migrate or that everything chosen migrated ok
		/// >0 gives the number of projects that failed to migrate
		/// </summary>
		internal static int s_ReturnValue = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[STAThread]
		static int Main(string[] rgArgs)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			for (int i = 0; i < rgArgs.Length; ++i)
			{
				if (rgArgs[i] == "-debug")
					s_fDebug = true;
				else if (rgArgs[i] == "-autoclose")
					s_fAutoClose = true;
				else if (rgArgs[i] == "-chars")
					s_fMigrateChars = true;
			}
			RegistryHelper.ProductName = "FieldWorks";	// needed to access proper registry values

			if (s_fMigrateChars || s_fAutoClose)
			{
				try
				{
					string location = Assembly.GetExecutingAssembly().Location;
					string program = Path.Combine(Path.GetDirectoryName(location), "UnicodeCharEditor.exe");
					using (Process proc = Process.Start(program, "-i"))
					{
						proc.WaitForExit();
					}
				}
				catch (Exception e)
				{
					if (s_fDebug)
					{
						var msg = String.Format("Cannot migrate the custom character definitions:{1}{0}",
							e.Message, Environment.NewLine);
						MessageBox.Show(msg);
					}
				}
			}

			// TE-9422. If we had an older version of FW7 installed, ldml files are < verion 2, so will cause
			// a crash if we don't migrate the files to version 2 before opening a project with the current version.
			string globalWsFolder = DirectoryFinder.GlobalWritingSystemStoreDirectory;
			var globalMigrator = new LdmlInFolderWritingSystemRepositoryMigrator(globalWsFolder, NoteMigration);
			globalMigrator.Migrate();

			using (var threadHelper = new ThreadHelper())
			using (var progressDlg = new ProgressDialogWithTask(threadHelper))
			{
				ImportFrom6_0 importer = new ImportFrom6_0(progressDlg, FwDirectoryFinder.ConverterConsoleExe, FwDirectoryFinder.DbExe, s_fDebug);
				if (!importer.IsFwSqlServerInstalled())
					return -1;
				string version;
				if (!importer.IsValidOldFwInstalled(out version))
				{
					if (!String.IsNullOrEmpty(version) && version.CompareTo("5.4") < 0)
					{
						string launchesFlex = "0";
						string launchesTE = "0";
						if (RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksRegistryKey, "Language Explorer"))
						{
							using (RegistryKey keyFlex = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("Language Explorer"))
								launchesFlex = keyFlex.GetValue("launches", "0") as string;
						}
						if (RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksRegistryKey, FwSubKey.TE))
						{
							using (RegistryKey keyTE = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey(FwSubKey.TE))
								launchesTE = keyTE.GetValue("launches", "0") as string;
						}
						if (launchesFlex == "0" && launchesTE == "0")
						{
							FwRegistryHelper.FieldWorksRegistryKey.SetValue("MigrationTo7Needed", "true");
						}
						using (var dlg = new FWVersionTooOld(version))
						{
							dlg.ShowDialog();
						}
					}
					return -1;
				}
				List<string> projects = GetProjectList();
				if (projects.Count > 0)
				{
					using (var migrateProjects = new MigrateProjects(importer, version, projects, s_fAutoClose))
					{
						Application.Run(migrateProjects);
					}
				}
				else if (s_fDebug)
				{
					MessageBox.Show("No FieldWorks (SQL) projects were detected.", "DEBUG!");
				}
			}
			return s_ReturnValue;
		}

		private static List<string> GetProjectList()
		{
			List<string> projects = new List<string>();
			try
			{
				string sSql = String.Format("Server={0}\\SILFW; Database=master; User ID=FWDeveloper;" +
					" Password=careful; Pooling=false;", Environment.MachineName);
				using (var connection = new SqlConnection(sSql))
				{
					connection.Open();
					using (SqlCommand commandProjectList = connection.CreateCommand())
					{
						commandProjectList.CommandText = "exec master..sp_GetFWDBs";
						using (SqlDataReader readerProjectList =
							commandProjectList.ExecuteReader(System.Data.CommandBehavior.SingleResult))
						{
							// Loop through the databases and add them to the projectList
							while (readerProjectList.Read())
							{
								string proj = readerProjectList.GetString(0);
								projects.Add(proj);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				if (s_fDebug)
				{
					string msg = String.Format(
						"An exception was thrown while trying to get the list of projects:{1}{0}",
						e.Message, Environment.NewLine);
					MessageBox.Show(msg, "DEBUG!");
				}
				projects.Clear();
			}
			return projects;
		}

		internal static void NoteMigration(IEnumerable<LdmlVersion0MigrationStrategy.MigrationInfo> migrationInfo)
		{
			foreach (var info in migrationInfo)
			{
				// Do nothing here.
			}
		}

	}
}