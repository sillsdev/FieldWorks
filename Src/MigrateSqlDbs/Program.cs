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
using System.Windows.Forms;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.IO;

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
					Process proc = Process.Start(program, "-i");
					proc.WaitForExit();
				}
				catch (Exception e)
				{
					if (s_fDebug)
					{
						var msg = String.Format("Cannot migrate the custom character definitions:\r\n{0}",
							e.Message);
						MessageBox.Show(msg);
					}
				}
			}
			ImportFrom6_0 importer = new ImportFrom6_0(s_fDebug);
			if (!importer.IsFwSqlServerInstalled())
				return -1;
			string version;
			if (!importer.IsValidOldFwInstalled(out version))
				return -1;
			List<string> projects = GetProjectList();
			if (projects.Count > 0)
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MigrateProjects(importer, version, projects, s_fAutoClose));
			}
			else if (s_fDebug)
			{
				MessageBox.Show("No FieldWorks (SQL) projects were detected.", "DEBUG!");
			}
			return s_ReturnValue;
		}

		private static List<string> GetProjectList()
		{
			List<string> projects = new List<string>();
			SqlConnection connection = null;
			try
			{
				string sSql = String.Format("Server={0}\\SILFW; Database=master; User ID=FWDeveloper;" +
					" Password=careful; Pooling=false;", Environment.MachineName);
				connection = new SqlConnection(sSql);
				connection.Open();
				SqlCommand commandProjectList = connection.CreateCommand();
				commandProjectList.CommandText = "exec master..sp_GetFWDBs";
				SqlDataReader readerProjectList = null;
				try
				{
					readerProjectList =
						commandProjectList.ExecuteReader(System.Data.CommandBehavior.SingleResult);
					// Loop through the databases and add them to the projectList
					while (readerProjectList.Read())
					{
						string proj =  readerProjectList.GetString(0);
						projects.Add(proj);
					}
				}
				finally
				{
					if (readerProjectList != null)
						readerProjectList.Close();
				}
			}
			catch (Exception e)
			{
				if (s_fDebug)
				{
					string msg = String.Format(
						"An exception was thrown while trying to get the list of projects:\r\n{0}",
						e.Message);
					MessageBox.Show(msg, "DEBUG!");
				}
				projects.Clear();
			}
			finally
			{
				if (connection != null)
					connection.Close();
			}
			return projects;
		}
	}
}