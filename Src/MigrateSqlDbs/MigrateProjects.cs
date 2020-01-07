// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices.DataMigration;

namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	/// <summary>
	/// This window displays the currently available FieldWorks 6.0 (or earlier) projects,
	/// allowing the user to choose any or all of them, and then converts the chosen projects
	/// to version 7.0.
	/// </summary>
	public partial class MigrateProjects : Form
	{
		ImportFrom6_0 m_importer;
		List<string> m_projects;
		bool m_fAutoClose;
		bool m_fTempMigrationDbExists;
		int m_cChecked;

		/// <summary>
		/// This class encapsulates a project for the checked list display.
		/// </summary>
		private sealed class ProjectItem
		{
			/// <summary />
			internal ProjectItem(string name)
			{
				Name = name;
				var dir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, name);
				if (Directory.Exists(dir))
				{
					if (File.Exists(Path.Combine(dir, name + LcmFileHelper.ksFwDataXmlFileExtension)))
					{
						Exists = true;
					}
				}
			}

			/// <summary>
			/// Get the display string for the project.
			/// </summary>
			public override string ToString()
			{
				return Migrated ? string.Format(Properties.Resources.ksMarkAsMigrated, Name)
					: Failed ? string.Format(Properties.Resources.ksMarkAsFailed, Name)
					: Exists ? string.Format(Properties.Resources.ksMarkAsExisting, Name)
					: Name;
			}

			/// <summary>
			/// Get the project's name.
			/// </summary>
			internal string Name { get; }

			/// <summary>
			/// Flag whether the project has been migrated during this run of the program.
			/// </summary>
			internal bool Migrated { private get; set; }

			/// <summary>
			/// Flag whether the project failed migration during this run of the program.
			/// </summary>
			internal bool Failed { private get; set; }

			/// <summary>
			/// Flag whether a 7.0+ project of this name already exists.
			/// </summary>
			private bool Exists { get; }
		}

		/// <summary />
		public MigrateProjects()
		{
			InitializeComponent();
		}

		/// <summary />
		public MigrateProjects(ImportFrom6_0 importer, string version, List<string> projects, bool fAutoClose)
			: this()
		{
			m_importer = importer;
			m_projects = projects;
			m_fAutoClose = fAutoClose;
			if (m_fAutoClose)
			{
				m_btnClose.Text = Properties.Resources.ksSkip;
			}
			Text = string.Format(Text, version);
			foreach (var proj in m_projects)
			{
				if (proj == ImportFrom6_0.TempDatabaseName)
				{
					m_fTempMigrationDbExists = true;
				}
				else
				{
					if (proj != "Sena 2" && proj != "Sena 3" && proj != "Lela-Teli 2" && proj != "Lela-Teli 3")
					{
						m_clbProjects.Items.Add(new ProjectItem(proj));
					}
				}
			}
			m_btnConvert.Enabled = false;   // disable until the user picks a project
		}

		private void m_btnConvert_Click(object sender, EventArgs e)
		{
			var rgsFailed = new List<string>();
			for (var i = 0; i < m_clbProjects.Items.Count; ++i)
			{
				if (m_clbProjects.GetItemChecked(i))
				{
					var proj = m_clbProjects.Items[i] as ProjectItem;
					var status = ConvertProject(proj.Name);
					switch (status)
					{
						case MigrateStatus.OK:
							proj.Migrated = true;
							break;
						case MigrateStatus.Failed:
							proj.Failed = true;
							rgsFailed.Add(proj.Name);
							break;
					}
					m_clbProjects.SetItemChecked(i, false);
				}
			}
			if (rgsFailed.Count > 0)
			{
				var bldr = new StringBuilder();
				bldr.Append(Properties.Resources.ksFollowingProjectsFailed);
				foreach (var proj in rgsFailed)
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
				Close();
			}
		}

		private enum MigrateStatus
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
				var dbName = proj;
				// 1. Check database version number.  If >= 200260, goto step 4.
				var version = GetDbVersion(proj);
				if (version < 200260)
				{
					// 2. Make a temporary copy of the project.
					// 3. Migrate that temporary copy.
					if (m_fTempMigrationDbExists)
					{
						fOk = m_importer.DeleteTempDatabase();
						if (!fOk)
						{
							return MigrateStatus.Failed;
						}
						m_fTempMigrationDbExists = false;
					}
					var msg = string.Format(Properties.Resources.ksCreatingATemporaryCopy, proj);
					var sErrorMsgFmt = String.Format(Properties.Resources.ksCreatingATemporaryCopyFailed, proj, "{0}", "{1}");
					fOk = m_importer.CopyToTempDatabase(proj, msg, sErrorMsgFmt);
					if (!fOk)
					{
						return MigrateStatus.Failed;
					}
					m_fTempMigrationDbExists = true;
					var msg2 = string.Format(Properties.Resources.ksMigratingTheCopy, proj);
					var errMsgFmt2 = string.Format(Properties.Resources.ksMigratingTheCopyFailed, proj, "{0}", "{1}");
					fOk = m_importer.MigrateTempDatabase(msg2, errMsgFmt2);
					if (!fOk)
					{
						return MigrateStatus.Failed;
					}
					dbName = ImportFrom6_0.TempDatabaseName;
				}
				// 4. Dump XML for project (or for the temporary project copy)
				var projDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, proj);
				var projName = proj;
				if (Directory.Exists(projDir))
				{
					using (var dlg = new ExistingProjectDlg(proj))
					{
						if (dlg.ShowDialog(this) == DialogResult.Cancel)
						{
							return MigrateStatus.Canceled;
						}
						projName = dlg.TargetProjectName;
					}
					projDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, projName);
					if (!Directory.Exists(projDir))
					{
						Directory.CreateDirectory(projDir);
					}
				}
				else
				{
					Directory.CreateDirectory(projDir);
				}
				var projXml = Path.Combine(projDir, "tempProj.xml");
				var msgDump = string.Format(Properties.Resources.ksWritingFw60XML, proj);
				var msgDumpErrorFmt = string.Format(Properties.Resources.ksWritingFw60XMLFailed, proj, "{0}", "{1}");
				if (dbName != proj)
				{
					msgDump = string.Format(Properties.Resources.ksWritingCopyAsFw60XML, proj);
					msgDumpErrorFmt = string.Format(Properties.Resources.ksWritingCopyAsFw60XMLFailed,
					proj, "{0}", "{1}");
				}
				fOk = m_importer.DumpDatabaseAsXml(dbName, projXml, msgDump, msgDumpErrorFmt);
				if (!fOk)
				{
					return MigrateStatus.Failed;
				}
				// 5. Convert FW 6.0 XML to FW 7.0 XML
				var projFile = Path.Combine(projDir, projName + LcmFileHelper.ksFwDataXmlFileExtension);
				fOk = m_importer.ImportFrom6_0Xml(projXml, projDir, projFile);
			}
			catch (CannotConvertException e)
			{
				fOk = false;
				MessageBox.Show(e.Message, Properties.Resources.ksCannotConvert);
			}
			return fOk ? MigrateStatus.OK : MigrateStatus.Failed;
		}

		/// <summary>
		/// Gets the version of the specified database
		/// </summary>
		/// <param name="dbName">Name of the database</param>
		/// <returns>the version number of the specified database</returns>
		protected virtual int GetDbVersion(string dbName)
		{
			var version = -1;
			using (var sqlConnection = new SqlConnection($"Server={Environment.MachineName}\\SILFW; Database={dbName}; User ID = sa; Password=inscrutable;" +
				"Connect Timeout = 30; Pooling=false;"))
			{
				try
				{
					sqlConnection.Open();
					using (var sqlComm = sqlConnection.CreateCommand())
					{
						var sSql = "select DbVer from Version$";
						sqlComm.CommandText = sSql;
						using (var sqlreader = sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult))
						{
							if (sqlreader.Read())
							{
								version = sqlreader.GetInt32(0);
							}
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
			var helpFile = Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "FieldWorks_Language_Explorer_Help.chm");
			var helpTopic = "/Overview/Migrate_FieldWorks_6.0.4_(or_earlier)_Projects.htm";
			Help.ShowHelp(new Label(), helpFile, helpTopic);
		}

		private void m_btnClearAll_Click(object sender, EventArgs e)
		{
			for (var i = 0; i < m_clbProjects.Items.Count; ++i)
			{
				m_clbProjects.SetItemChecked(i, false);
			}
		}

		private void m_btnSelectAll_Click(object sender, EventArgs e)
		{
			for (var i = 0; i < m_clbProjects.Items.Count; ++i)
			{
				m_clbProjects.SetItemChecked(i, true);
			}
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_clbProjects_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			switch (e.NewValue)
			{
				case CheckState.Checked:
					++m_cChecked;
					break;
				case CheckState.Unchecked:
					--m_cChecked;
					break;
			}
			m_btnConvert.Enabled = m_cChecked > 0;
		}
	}
}