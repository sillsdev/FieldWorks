// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace ImportExport
{
	public partial class ImportExport : Form
	{
		private string m_path;

		private string m_originalXml;

		private string m_xmlTestFile;
		private string m_dB4oTestFile;
		private string m_MySQLMyISAMCSTestFile;
		private string m_MySQLInnoDBCSTestFile;
		private string m_gitTestFile;

		private bool m_timingRun;

		public ImportExport()
		{
			InitializeComponent();
			m_path = Directory.GetCurrentDirectory();
			m_originalXml = Path.Combine(m_path, @"Zpi_7-0.xml");
			m_xmlTestFile = Path.Combine(m_path, @"ZPI_Test_Source.xml");
			m_dB4oTestFile = Path.Combine(m_path, @"ZPI_Test_Source.db4o");
#if USING_MYSQL
			m_MySQLMyISAMCSTestFile = @"ZPI_Test_Source_MyISAM";
			m_MySQLInnoDBCSTestFile = @"ZPI_Test_Source_InnoDB";
#endif
			m_gitTestFile = Path.Combine(m_path, @"ZPI_Test_Source_Git");
			m_txtSourceFile.Text = m_originalXml;
		}

		/// <summary>
		/// Starting with the original ZPI data file,
		/// produce source files for each BEP
		/// These source files are then used for the two types of tests.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BasicConversion(object sender, EventArgs e)
		{
			UpdateFilePaths();
			// Basic load of source BEP.
			using (var originalCache = FdoCache.CreateCacheFromExistingData(FDOBackendProviderType.kXML,
				new object[] {m_originalXml}, "en"))
			{
				var originalDataSetup = originalCache.ServiceLocator.GetInstance<IDataSetup>();
				var start = DateTime.Now;
				originalDataSetup.StartupExtantLanguageProject(new object[] { m_txtSourceFile.Text }, false);
				var elapsedTime = DateTime.Now - start;
				MessageBox.Show(this, "Time to load original XML: " + elapsedTime.TotalMilliseconds);

				// Convert to test XML BEP.
				ConvertToTargetBEP(originalCache, FDOBackendProviderType.kXML, m_xmlTestFile,
					"Time to convert original XML to new XML BEP: ");
				// Convert to test DB4o BEP.
				ConvertToTargetBEP(originalCache, FDOBackendProviderType.kDb4o, m_dB4oTestFile,
					"Time to convert original XML to new DB4o BEP: ");
				// Convert to test MySQL MyISAM BEP
#if USING_MYSQL
				ConvertToTargetBEP(originalCache, FDOBackendProviderType.kMySqlClientServer, m_MySQLMyISAMCSTestFile,
					"Time to convert original XML to new MySQL MyISAM CS BEP: ");
				// Convert to test MySQL MyInnoDB BEP
				ConvertToTargetBEP(originalCache, FDOBackendProviderType.kMySqlClientServerInnoDB, m_MySQLInnoDBCSTestFile,
					"Time to convert original XML to new MySQL InnoDB CS BEP: ");
#endif
			}
		}

		private void UpdateFilePaths()
		{
			m_originalXml = m_txtSourceFile.Text;
			m_path = Path.GetDirectoryName(m_originalXml);
			var filename = Path.GetFileNameWithoutExtension(m_originalXml);
			m_xmlTestFile = Path.ChangeExtension(Path.Combine(m_path, filename), "xml");
			m_dB4oTestFile = Path.ChangeExtension(Path.Combine(m_path, filename), "db4o");
			m_MySQLMyISAMCSTestFile = filename + "_MyISAM";
			m_MySQLInnoDBCSTestFile = filename + "_InnoDB";
			m_gitTestFile = Path.Combine(m_path, filename + "_Git");
		}

		private void ConvertToTargetBEP(FdoCache originalCache, FDOBackendProviderType bepType, string targetPathname, string msg)
		{
			// Convert to test target BEP.
			TimeSpan elapsedTime;
			DeleteBEP(bepType, targetPathname);
			var start = DateTime.Now;
			using (var targetCache = FdoCache.CreateCacheCopy(bepType, new object[] { targetPathname }, "en", originalCache))
			{
				//var dataSetup = targetCache.ServiceLocator.GetInstance<IDataSetup>();
				//dataSetup.CreateNewLanguageProject(new object[] { targetPathname }, originalCache);
				elapsedTime = DateTime.Now - start;
			}
			MessageBox.Show(this, msg + elapsedTime.TotalMilliseconds);
		}

		/// <summary>
		/// Load the source data file the selected number of times.
		///
		/// For now, only do the minimal load, to test basic load times.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LoadSource(object sender, EventArgs e)
		{
			var bepType = FDOBackendProviderType.kDb4oClientServer;
			var sourcePathname = m_MySQLMyISAMCSTestFile;
			var msg = "MySQL ISAM";
			if (m_rbSourceXml.Checked)
			{
				bepType = FDOBackendProviderType.kXML;
				sourcePathname = m_xmlTestFile;
				msg = "XML";
			}
			else if (m_rbSourceDB4o.Checked)
			{
				bepType = FDOBackendProviderType.kDb4o;
				sourcePathname = m_dB4oTestFile;
				msg = "DB4o";
			}
#if USING_MYSQL
			else if (m_rbSourceMySQLMyISAMCS.Checked)
			{
				bepType = FDOBackendProviderType.kMySqlClientServer;
				sourcePathname = m_MySQLMyISAMCSTestFile;
				msg = "MySQL MyISAM Client/Server";
			}
#endif
			else if (m_rbSourceGit.Checked)
			{
				bepType = FDOBackendProviderType.kGit;
				sourcePathname = m_gitTestFile;
				msg = "Git";
			}
#if USING_MYSQL
			else if (m_rbSourceMySQLInnoDBCS.Checked)
			{
				bepType = FDOBackendProviderType.kMySqlClientServerInnoDB;
				sourcePathname = m_MySQLInnoDBCSTestFile;
				msg = "MySQL InnoDB Client/Server";
			}
#endif

			LoadSource(bepType, sourcePathname, msg);
		}

		private void LoadSource(FDOBackendProviderType bepType, string sourcePathname, string msg)
		{
			// Do a dry run, to get it precompiled.
			using (var cache = FdoCache.CreateCacheFromExistingData(bepType, new object[] {sourcePathname}, "en"))
			{
			}

			// Do a load for each count.
			var elapsedTime = new TimeSpan(0);
			for (var i = 0; i < numericUpDown1.Value; ++i)
			{
				var start = DateTime.Now;
				using (var cache = FdoCache.CreateCacheFromExistingData(bepType, new object[] {sourcePathname}, "en"))
				{
					//var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
					//dataSetup.StartupExtantLanguageProject(new object[] { sourcePathname });
					elapsedTime += DateTime.Now - start;
				}
			}
			MessageBox.Show(this, string.Format("Average time to load {0}: {1}",
				msg, elapsedTime.TotalMilliseconds / (int)numericUpDown1.Value));
		}

		/// <summary>
		/// Import from the original XML file into the selected BEP type (Target System)
		/// the selected number of times.
		///
		/// The original file will only be loaded once.
		/// The target will be done once to get it going,
		/// and then again the selected number of times with the results
		/// averaged over the selected number of times.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Import(object sender, EventArgs e)
		{
			m_timingRun = true;
			Import();
		}

		private void Import()
		{
			var bepType = FDOBackendProviderType.kDb4oClientServer;
			var msg = "MySQL";
			var exportedPathname = m_MySQLMyISAMCSTestFile;
			if (m_rbTargetDB4o.Checked)
			{
				bepType = FDOBackendProviderType.kDb4o;
				msg = "DB4o";
				exportedPathname = Path.Combine(m_path, @"Exported.db4o");
			}
			else if (m_rbTargetXml.Checked)
			{
				bepType = FDOBackendProviderType.kXML;
				msg = "XML";
				exportedPathname = Path.Combine(m_path, @"Exported.xml");
			}
			else if (m_rbTargetGit.Checked)
			{
				bepType = FDOBackendProviderType.kGit;
				exportedPathname = Path.Combine(m_path, @"ExportedGit");
				msg = "Git";
			}
#if USING_MYSQL
			else if (m_rbTargetMySQLMyISAMCS.Checked)
			{
				bepType = FDOBackendProviderType.kMySqlClientServer;
				exportedPathname = @"MySQLMyISAMExported";
				msg = "MySQL MyISAM CS";
			}
			else if (m_rbTargetMySQLInnoDBCS.Checked)
			{
				bepType = FDOBackendProviderType.kMySqlClientServerInnoDB;
				exportedPathname = @"MySQLInnoDBExported";
				msg = "MySQL InnoDB CS";
			}
#endif

			// Basic load of source BEP.
			using (var originalCache = FdoCache.CreateCacheFromExistingData(FDOBackendProviderType.kXML, new object[] {m_txtSourceFile.Text}, "en"))
			{
				// Import to test XML BEP.
				Import(originalCache, bepType, msg, exportedPathname);
			}
		}

		private void Import(FdoCache originalCache, FDOBackendProviderType bepType, string msg, string exportedPathname)
		{
			DeleteBEP(bepType, exportedPathname);

			if (m_timingRun)
			{
				// Do a dry run, to get it precomplied.
				using (var cache = FdoCache.CreateCacheCopy(bepType, new object[] {exportedPathname}, "en", originalCache))
				{
					//var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
					//dataSetup.CreateNewLanguageProject(new object[] {exportedPathname}, originalCache);
				}
			}

			// Do an export for each count.
			var elapsedTime = new TimeSpan(0);
			int runCount = m_timingRun ? (int)numericUpDown1.Value : 1;
			for (var i = 0; i < runCount; ++i)
			{
				DeleteBEP(bepType, exportedPathname);
				var start = DateTime.Now;
				using (var cache = FdoCache.CreateCacheCopy(bepType, new object[] {exportedPathname}, "en", originalCache))
				{
					//var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
					//dataSetup.CreateNewLanguageProject(new object[] { exportedPathname }, originalCache);
					elapsedTime += DateTime.Now - start;
				}
			}

			if (m_timingRun)
				DeleteBEP(bepType, exportedPathname);
			MessageBox.Show(this, string.Format("Average time to import from XML to {0}: {1}", msg, elapsedTime.TotalMilliseconds / runCount));
		}

		/// <summary>
		/// Delete a backend provider.
		/// </summary>
		/// <param name="bepType"></Type of parameter>
		/// <param name="fileName"></File name to delete>
		// TODO (SteveMiller/RandyR): Replace this method with the RemoveBackend() method.
		// TODO This will properly remove files as each back end provider prefers.
		// TODO Currently this works only because of a MySQL capability that doesn't need
		// TODO the file to be deleted ahead of time.
		private static void DeleteBEP(
			FDOBackendProviderType bepType, string fileName)
		{
#if USING_MYSQL

			if (bepType != FDOBackendProviderType.kMySqlClientServer &&
				bepType != FDOBackendProviderType.kMySqlClientServerInnoDB)
			{
				if (File.Exists(fileName))
					File.Delete(fileName);
			}
#else
			if (File.Exists(fileName))
				File.Delete(fileName);
#endif
		}

		/// <summary>
		/// Export to XML the selected number of times.
		///
		/// The Db4o data will be the source file and will only be loaded once.
		/// The target XML will be done once to get it going,
		/// and then again the selected number of times with the results
		/// averaged over the selected number of times.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Export(object sender, EventArgs e)
		{
			// Basic load of source BEP.
			//using (var originalCache = FdoCache.CreateCache(FDOBackendProviderType.kDb4o))
			//{
			//    var originalDataSetup = originalCache.ServiceLocator.GetInstance<IDataSetup>();
			//    originalDataSetup.StartupExtantLanguageProject(new object[] { m_dB4oTestFile });

			//    // Export to test XML BEP.
			//    Export(originalCache);
			//}
		}

		//private void Export(FdoCache originalCache)
		//{
		//    const string exportedXmlPathname = "Exported.xml";

		//    // Do a dry run, to get it precomplied.
		//    using (var cache = FdoCache.CreateCache(FDOBackendProviderType.kXML))
		//    {
		//        var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
		//        dataSetup.CreateNewLanguageProject(new object[] { exportedXmlPathname }, originalCache);
		//    }

		//    // Do an export for each count.
		//    var elapsedTime = new TimeSpan(0);
		//    for (var i = 0; i < numericUpDown1.Value; ++i)
		//    {
		//        File.Delete(exportedXmlPathname);
		//        using (var cache = FdoCache.CreateCache(FDOBackendProviderType.kXML))
		//        {
		//            var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
		//            var start = DateTime.Now;
		//            dataSetup.CreateNewLanguageProject(new object[] { exportedXmlPathname }, originalCache);
		//            elapsedTime += DateTime.Now - start;
		//        }
		//    }
		//    File.Delete(exportedXmlPathname);
		//    MessageBox.Show(this, string.Format("Average time to export to XML: {0}", elapsedTime.TotalMilliseconds / (int)numericUpDown1.Value));

		//}

		private void ConvertSelected(object sender, EventArgs e)
		{
			m_timingRun = false;
			Import();
		}
	}
}
