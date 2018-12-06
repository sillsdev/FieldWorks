// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices.DataMigration;
using SIL.LCModel.Utils;
using SIL.WritingSystems.Migration;

namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	/// <summary>
	/// This program displays the list of FieldWorks SQL Server projects on the current machine,
	/// and allows the user to select any or all of them for conversion to the FieldWorks 7.0
	/// format.
	/// </summary>
	internal static class Program
	{
		static bool s_fDebug;
		static bool s_fAutoClose;
		static bool s_fMigrateChars;
		/// <summary>
		/// -1 means we couldn't even try to migrate anything,
		/// 0 means that either there was no data to migrate or that everything chosen migrated ok
		/// >0 gives the number of projects that failed to migrate
		/// </summary>
		internal static int s_ReturnValue = 0;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		private static int Main(string[] rgArgs)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			foreach (var arg in rgArgs)
			{
				switch (arg)
				{
					case "-debug":
						s_fDebug = true;
						break;
					case "-autoclose":
						s_fAutoClose = true;
						break;
					case "-chars":
						s_fMigrateChars = true;
						break;
				}
			}
			FwRegistryHelper.Initialize(); // needed to access proper registry values

			if (s_fMigrateChars && s_fDebug)
			{
				MessageBox.Show("Warning: MigrateSqlDbs called with no-longer valid argument, '-chars'. Run 'UnicodeCharEditor -i' instead.");
			}
			// TE-9422. If we had an older version of FW7 installed, ldml files are < verion 2, so will cause
			// a crash if we don't migrate the files to version 2 before opening a project with the current version.
			// TODO (WS_FIX): should we migrate all the way to version 3?
			var globalWsFolder = LcmFileHelper.OldGlobalWritingSystemStoreDirectory;
			var globalMigrator = new LdmlInFolderWritingSystemRepositoryMigrator(globalWsFolder, NoteMigration, 2);
			globalMigrator.Migrate();

			using (var threadHelper = new ThreadHelper())
			using (var progressDlg = new ProgressDialogWithTask(threadHelper))
			{
				var importer = new ImportFrom6_0(progressDlg, FwDirectoryFinder.ConverterConsoleExe, FwDirectoryFinder.DbExe, s_fDebug);
				if (!importer.IsFwSqlServerInstalled())
				{
					return -1;
				}
				string version;
				if (!importer.IsValidOldFwInstalled(out version))
				{
					if (!string.IsNullOrEmpty(version) && version.CompareTo("5.4") < 0)
					{
						string launchesFlex = "0";
						if (RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksRegistryKey, "Language Explorer"))
						{
							using (var keyFlex = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("Language Explorer"))
							{
								launchesFlex = keyFlex.GetValue("launches", "0") as string;
							}
						}
						if (launchesFlex == "0")
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
				var projects = GetProjectList();
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
			var projects = new List<string>();
			try
			{
				var sSql = $"Server={Environment.MachineName}\\SILFW; Database=master; User ID=FWDeveloper; Password=careful; Pooling=false;";
				using (var connection = new SqlConnection(sSql))
				{
					connection.Open();
					using (var commandProjectList = connection.CreateCommand())
					{
						commandProjectList.CommandText = "exec master..sp_GetFWDBs";
						using (var readerProjectList = commandProjectList.ExecuteReader(System.Data.CommandBehavior.SingleResult))
						{
							// Loop through the databases and add them to the projectList
							while (readerProjectList.Read())
							{
								var proj = readerProjectList.GetString(0);
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
					var msg = string.Format(
						"An exception was thrown while trying to get the list of projects:{1}{0}",
						e.Message, Environment.NewLine);
					MessageBox.Show(msg, "DEBUG!");
				}
				projects.Clear();
			}
			return projects;
		}

		internal static void NoteMigration(int toVersion, IEnumerable<LdmlMigrationInfo> migrationInfo)
		{
			// Do nothing here.
		}
	}
}