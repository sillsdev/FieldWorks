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
// File: MigrateProjects.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This window displays the currently available FieldWorks 6.0 (or earlier) projects,
	/// allowing the user to choose any or all of them, and then converts the chosen projects
	/// to version 7.0.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class MigrateProjects : Form
	{
		ImportFrom6_0 m_importer;
		List<string> m_projects;
		bool m_fAutoClose = false;
		bool m_fTempMigrationDbExists = false;
		int m_cChecked = 0;

		/// <summary>
		/// This class encapsulates a project for the checked list display.
		/// </summary>
		internal class ProjectItem
		{
			string m_name;
			bool m_fMigrated;
			bool m_fFailed;
			bool m_fExists;

			/// <summary>
			/// Constructor.
			/// </summary>
			internal ProjectItem(string name)
			{
				m_name = name;
				string dir = Path.Combine(DirectoryFinder.ProjectsDirectory, name);
				if (Directory.Exists(dir))
				{
					if (File.Exists(Path.Combine(dir, name + FwFileExtensions.ksFwDataXmlFileExtension)) ||
						File.Exists(Path.Combine(dir, name + FwFileExtensions.ksFwDataDb4oFileExtension)))
					{
						m_fExists = true;
					}
				}
			}

			/// <summary>
			/// Get the display string for the project.
			/// </summary>
			public override string ToString()
			{
				if (m_fMigrated)
				{
					return String.Format(Properties.Resources.ksMarkAsMigrated, m_name);
				}
				else if (m_fFailed)
				{
					return String.Format(Properties.Resources.ksMarkAsFailed, m_name);
				}
				else if (m_fExists)
				{
					return String.Format(Properties.Resources.ksMarkAsExisting, m_name);
				}
				else
				{
					return m_name;
				}
			}

			/// <summary>
			/// Get the project's name.
			/// </summary>
			internal string Name
			{
				get { return m_name; }
			}

			/// <summary>
			/// Flag whether the project has been migrated during this run of the program.
			/// </summary>
			internal bool Migrated
			{
				get { return m_fMigrated; }
				set { m_fMigrated = value; }
			}

			/// <summary>
			/// Flag whether the project failed migration during this run of the program.
			/// </summary>
			internal bool Failed
			{
				get { return m_fFailed; }
				set { m_fFailed = value; }
			}

			/// <summary>
			/// Flag whether a 7.0+ project of this name already exists.
			/// </summary>
			internal bool Exists
			{
				get { return m_fExists; }
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MigrateProjects()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MigrateProjects(ImportFrom6_0 importer, string version, List<string> projects,
			bool fAutoClose)
			: this()
		{
			m_importer = importer;
			m_importer.ParentForm = this;
			m_projects = projects;
			m_fAutoClose = fAutoClose;
			if (m_fAutoClose)
				m_btnClose.Text = Properties.Resources.ksSkip;
			this.Text = String.Format(this.Text, version);
			foreach (string proj in m_projects)
			{
				if (proj == ImportFrom6_0.TempDatabaseName)
				{
					m_fTempMigrationDbExists = true;
				}
				else
				{
					if (proj != "Sena 2" && proj != "Sena 3" && proj != "Lela-Teli 2" && proj != "Lela-Teli 3")
						m_clbProjects.Items.Add(new ProjectItem(proj));
				}
			}
			m_btnConvert.Enabled = false;	// disable until the user picks a project
		}

		private void m_btnConvert_Click(object sender, EventArgs e)
		{
			MigrateStatus status;
			List<string> rgsFailed = new List<string>();
			for (int i = 0; i < m_clbProjects.Items.Count; ++i)
			{
				if (m_clbProjects.GetItemChecked(i))
				{
					ProjectItem proj = m_clbProjects.Items[i] as ProjectItem;
					status = ConvertProject(proj.Name);
					if (status == MigrateStatus.OK)
					{
						proj.Migrated = true;
					}
					else if (status == MigrateStatus.Failed)
					{
						proj.Failed = true;
						rgsFailed.Add(proj.Name);
					}
					m_clbProjects.SetItemChecked(i, false);
				}
			}
			if (rgsFailed.Count > 0)
			{
				StringBuilder bldr = new StringBuilder();
				bldr.Append(Properties.Resources.ksFollowingProjectsFailed);
				foreach (string proj in rgsFailed)
				{
					bldr.AppendLine();
					bldr.Append(proj);
				}
				MessageBox.Show(this, bldr.ToString(), Properties.Resources.ksWarning);
			}
			if (m_fAutoClose)
			{
				DialogResult = rgsFailed.Count > 0 ? DialogResult.No : DialogResult.Yes;
				Program.s_ReturnValue = rgsFailed.Count;
				this.Close();
			}
		}

		enum MigrateStatus
		{
			OK,
			Failed,
			Canceled
		}

		private MigrateStatus ConvertProject(string proj)
		{
			bool fOk;
			try
			{
				string dbName = proj;
				// 1. Check database version number.  If >= 200260, goto step 4.
				int version =  GetDbVersion(proj);
				if (version < 200260)
				{
					// 2. Make a temporary copy of the project.
					// 3. Migrate that temporary copy.
					if (m_fTempMigrationDbExists)
					{
						fOk = m_importer.DeleteTempDatabase();
						if (!fOk)
							return MigrateStatus.Failed;
						m_fTempMigrationDbExists = false;
					}
					string msg = String.Format(Properties.Resources.ksCreatingATemporaryCopy, proj);
					string sErrorMsgFmt = String.Format(Properties.Resources.ksCreatingATemporaryCopyFailed,
						proj, "{0}", "{1}");
					fOk = m_importer.CopyToTempDatabase(proj, msg, sErrorMsgFmt);
					if (!fOk)
						return MigrateStatus.Failed;
					m_fTempMigrationDbExists = true;
					string msg2 = String.Format(Properties.Resources.ksMigratingTheCopy, proj);
					string errMsgFmt2 = String.Format(Properties.Resources.ksMigratingTheCopyFailed,
						proj, "{0}", "{1}");
					fOk = m_importer.MigrateTempDatabase(msg2, errMsgFmt2);
					if (!fOk)
						return MigrateStatus.Failed;
					dbName = ImportFrom6_0.TempDatabaseName;
				}
				// 4. Dump XML for project (or for the temporary project copy)
				string projDir = Path.Combine(DirectoryFinder.ProjectsDirectory, proj);
				string projName = proj;
				if (Directory.Exists(projDir))
				{
					using (var dlg = new ExistingProjectDlg(proj))
					{
						if (dlg.ShowDialog(this) == DialogResult.Cancel)
							return MigrateStatus.Canceled;
						projName = dlg.TargetProjectName;
					}
					projDir = Path.Combine(DirectoryFinder.ProjectsDirectory, projName);
					if (!Directory.Exists(projDir))
						Directory.CreateDirectory(projDir);
				}
				else
				{
					Directory.CreateDirectory(projDir);
				}
				string projXml = Path.Combine(projDir, "tempProj.xml");
				string msgDump = String.Format(Properties.Resources.ksWritingFw60XML, proj);
				string msgDumpErrorFmt = String.Format(Properties.Resources.ksWritingFw60XMLFailed,
					proj, "{0}", "{1}");
				if (dbName != proj)
				{
					msgDump = String.Format(Properties.Resources.ksWritingCopyAsFw60XML, proj); ;
					msgDumpErrorFmt = String.Format(Properties.Resources.ksWritingCopyAsFw60XMLFailed,
					proj, "{0}", "{1}");
				}
				fOk = m_importer.DumpDatabaseAsXml(dbName, projXml, msgDump, msgDumpErrorFmt);
				if (!fOk)
					return MigrateStatus.Failed;
				// 5. Convert FW 6.0 XML to FW 7.0 XML
				string projFile = Path.Combine(projDir, projName + FwFileExtensions.ksFwDataXmlFileExtension);
				fOk = m_importer.ImportFrom6_0Xml(projXml, projDir, projFile);
			}
			catch (CannotConvertException e)
			{
				fOk = false;
				MessageBox.Show(e.Message, Properties.Resources.ksCannotConvert);
			}
			return fOk ? MigrateStatus.OK : MigrateStatus.Failed;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version of the specified database
		/// </summary>
		/// <param name="dbName">Name of the database</param>
		/// <returns>the version number of the specified database</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual int GetDbVersion(string dbName)
		{
			int version = -1;

			using (SqlConnection sqlConnection = new SqlConnection(
				string.Format("Server={0}\\SILFW; Database={1}; User ID = sa; Password=inscrutable;" +
					"Connect Timeout = 30; Pooling=false;", Environment.MachineName, dbName)))
			{
				try
				{
					sqlConnection.Open();
					using (SqlCommand sqlComm = sqlConnection.CreateCommand())
					{
						string sSql = "select DbVer from Version$";
						sqlComm.CommandText = sSql;
						using (var sqlreader = sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult))
						{
							if (sqlreader.Read())
								version = sqlreader.GetInt32(0);
						}
					}
				}
				catch
				{
					// ignore exceptions - returned version will be -1
				}
				finally
				{
					sqlConnection.Close();
				}
			}
			return version;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			string helpFile = Path.Combine(DirectoryFinder.FWCodeDirectory,
				"Helps\\FieldWorks_Language_Explorer_Help.chm");
			string helpTopic = "/Overview/Migrate_FieldWorks_6.0.4_(or_earlier)_Projects.htm";
			Help.ShowHelp(new Label(), helpFile, helpTopic);
		}

		private void m_btnClearAll_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < m_clbProjects.Items.Count; ++i)
				m_clbProjects.SetItemChecked(i, false);
		}

		private void m_btnSelectAll_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < m_clbProjects.Items.Count; ++i)
				m_clbProjects.SetItemChecked(i, true);
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void m_clbProjects_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (e.NewValue == CheckState.Checked)
			{
				++m_cChecked;
			}
			else if (e.NewValue == CheckState.Unchecked)
			{
				--m_cChecked;
			}
			m_btnConvert.Enabled = m_cChecked > 0;
		}

	}
}
