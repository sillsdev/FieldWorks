using System.Collections.Generic;
using SIL.FieldWorks.Common.Utils;
using System.Data.SqlClient;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region ProjectInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Keeps track of information about a FieldWorks project
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ProjectInfo
	{
		#region Member variables
		private string m_databaseName;
		private bool m_inUse = false;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a new ProjectInfo object
		/// </summary>
		/// <param name="databaseName"></param>
		/// ------------------------------------------------------------------------------------
		public ProjectInfo(string databaseName)
		{
			m_databaseName = databaseName;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set the database name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DatabaseName
		{
			get { return m_databaseName; }
			set { m_databaseName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set the indicator that tells if the database is in use
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InUse
		{
			get { return m_inUse; }
			set { m_inUse = value; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a string representation of the project
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return DatabaseName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a set of projects on the current server.
		/// </summary>
		/// <returns>Set of ProjectInfo objects, representing the projects on server</returns>
		/// ------------------------------------------------------------------------------------
		public static List<ProjectInfo> GetProjectInfo()
		{
			List<ProjectInfo> projectList = new List<ProjectInfo>(8);

			string sSql = string.Format("Server={0}; Database=master; User ID=FWDeveloper;" +
				" Password=careful; Pooling=false;", MiscUtils.LocalServerName);
			SqlConnection connection = new SqlConnection(sSql);
			connection.Open();
			try
			{
				SqlCommand commandProjectList = connection.CreateCommand();
				commandProjectList.CommandText = "exec master..sp_GetFWDBs";

				SqlDataReader readerProjectList =
					commandProjectList.ExecuteReader(System.Data.CommandBehavior.SingleResult);
				// Loop through the databases and add them to the projectList
				while (readerProjectList.Read())
					projectList.Add(new ProjectInfo(readerProjectList.GetString(0)));

				readerProjectList.Close();

				// Get information for all of the databases
				foreach (ProjectInfo info in projectList)
				{
					SqlCommand commandUserList = connection.CreateCommand();
					commandUserList.CommandText = @"select rtrim([sproc].[hostname]), " +
						@"   rtrim([sproc].[nt_domain]) + '\\' + rtrim([sproc].[nt_username]) " +
						@"from sysprocesses [sproc] " +
						@"join sysdatabases [sdb] " +
						@"   on [sdb].[dbid] = [sproc].[dbid] and [name] = @DB" +
						@" where sproc.spid != @@spid";
					// It's safest to pass the database name as a parameter, because it may contain
					// apostrophes.  See LT-8910.
					commandUserList.Parameters.Add(new SqlParameter("@DB", info.DatabaseName));
					SqlDataReader readerUsedProjects =
						commandUserList.ExecuteReader(System.Data.CommandBehavior.SingleResult);

					info.InUse = readerUsedProjects.Read();

					readerUsedProjects.Close();
				}
			}
			finally
			{
				connection.Close();
			}

			return projectList;
		}
		#endregion
	}
	#endregion
}