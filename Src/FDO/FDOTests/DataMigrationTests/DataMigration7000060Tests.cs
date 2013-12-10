using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000058 to 7000059.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000060 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000059 to 7000060.
		/// Rename configuration files from X_Layouts.xml to X.fwlayout
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000060Test()
		{

			var projectFolder = Path.GetTempPath();
			var settingsFolder = Path.Combine(projectFolder, FdoFileHelper.ksConfigurationSettingsDir);
			var sampleLayout = Path.Combine(settingsFolder, "Test_Layouts.xml");
			var otherFile = Path.Combine(settingsFolder, "other.xml");
			var newLayoutPath = Path.Combine(settingsFolder, "Test.fwlayout");
			// Delete any leftover data from incomplete test (or previous test).
			if (Directory.Exists(settingsFolder))
				Directory.Delete(settingsFolder, true);

			Directory.CreateDirectory(settingsFolder);
			File.WriteAllText(sampleLayout, "nonsence", Encoding.UTF8);
			File.WriteAllText(otherFile, "rubbish", Encoding.UTF8);

			var mockMDC = new MockMDCForDataMigration(); // no classes to migrate here
			var dtos = new HashSet<DomainObjectDTO>(); // no objects to migrate
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000059, dtos, mockMDC, projectFolder, FwDirectoryFinder.FdoDirectories);
			// Do the migration.
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000060, new DummyProgressDlg());

			Assert.That(File.Exists(newLayoutPath));
			Assert.That(File.Exists(otherFile));
			Assert.That(!File.Exists(sampleLayout));
			Directory.Delete(settingsFolder, true);

			Assert.AreEqual(7000060, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}
	}
}