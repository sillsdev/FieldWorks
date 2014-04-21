using System.Collections.Specialized;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// TeImportExportExtensions test fixture
	/// </summary>
	[TestFixture]
	public class TeImportExportExtensionsTests : ScrInMemoryFdoTestBase
	{
		#region ImportProjectIsAccessible tests

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test adding a file that is locked
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFileAndCheckAccessibility_Locked()
		{
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create(ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			importSettings.ImportTypeEnum = TypeOfImport.Other;

			var fileOs = new MockFileOS();
			try
			{
				FileUtils.Manager.SetFileAdapter(fileOs);
				string filename = fileOs.MakeSfFile("EPH", @"\c 1", @"\v 1");
				fileOs.LockFile(filename);

				IScrImportFileInfo info = importSettings.AddFile(filename, ImportDomain.Main, null, null);
				Assert.AreEqual(Encoding.ASCII, info.FileEncoding);
				Assert.AreEqual(1, importSettings.GetImportFiles(ImportDomain.Main).Count);
				StringCollection notFound;
				Assert.IsFalse(importSettings.ImportProjectIsAccessible(out notFound));
				Assert.AreEqual(1, notFound.Count);
				Assert.AreEqual(filename, notFound[0]);
			}
			finally
			{
				FileUtils.Manager.Reset();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for Paratext 6 projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportProjectIsAccessible_Paratext6()
		{
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create(ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			importSettings.ImportTypeEnum = TypeOfImport.Paratext6;

			var fileOs = new MockFileOS();
			var mockParatextHelper = MockRepository.GenerateMock<IParatextHelper>();
			MockParatextHelper ptHelper = null;
			try
			{
				FileUtils.Manager.SetFileAdapter(fileOs);
				ptHelper = new MockParatextHelper();
				ParatextHelper.Manager.SetParatextHelperAdapter(ptHelper);
				string paratextDir = ParatextHelper.ProjectsDirectory;

				ptHelper.AddProject("TEV", null, null, true, false, "100001");

				fileOs.AddExistingFile(Path.Combine(paratextDir, "TEV.ssf"));

				importSettings.ParatextScrProj = "KAM";
				importSettings.ParatextBTProj = "TEV";

				StringCollection projectsNotFound;
				Assert.IsFalse(importSettings.ImportProjectIsAccessible(out projectsNotFound));
				Assert.AreEqual(1, projectsNotFound.Count);
				Assert.AreEqual("KAM", projectsNotFound[0]);

				fileOs.AddExistingFile(Path.Combine(paratextDir, "KAM.ssf"));
				ptHelper.AddProject("KAM", null, null, true, false, "000101");
				Assert.IsTrue(importSettings.ImportProjectIsAccessible(out projectsNotFound));
			}
			finally
			{
				ParatextHelper.Manager.Reset();
				if (ptHelper != null)
					ptHelper.Dispose();
				FileUtils.Manager.Reset();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for Paratext 5 projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportProjectIsAccessible_Paratext5()
		{
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create(ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			importSettings.ImportTypeEnum = TypeOfImport.Paratext5;
			ImportProjectIsAccessible_helper(importSettings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for Other projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportProjectIsAccessible_Other()
		{
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create(ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			importSettings.ImportTypeEnum = TypeOfImport.Other;
			ImportProjectIsAccessible_helper(importSettings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the ImportProjectIsAccessible method works for projects other than
		/// Paratext 6.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ImportProjectIsAccessible_helper(IScrImportSet importSettings)
		{
			var fileOs = new MockFileOS();
			try
			{
				FileUtils.Manager.SetFileAdapter(fileOs);
				string scrFile1 = fileOs.MakeSfFile("GEN", @"\p", @"\c 1", @"\v 1", @"\v 2");
				string scrFile2 = fileOs.MakeSfFile("EXO", @"\p", @"\c 1", @"\v 1", @"\v 2");
				string scrFile3 = fileOs.MakeSfFile("LEV", @"\p", @"\c 1", @"\v 1", @"\v 2");
				string btFileDef = fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
				string btFileSpan = fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
				string annotFileCons = fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");
				string annotFileTrans = fileOs.MakeSfFile("GEN", @"\p", @"\c 3", @"\v 1");

				importSettings.AddFile(scrFile1, ImportDomain.Main, null, null);
				importSettings.AddFile(scrFile2, ImportDomain.Main, null, null);
				importSettings.AddFile(scrFile3, ImportDomain.Main, null, null);
				importSettings.AddFile(btFileDef, ImportDomain.BackTrans, null, null);
				importSettings.AddFile(btFileSpan, ImportDomain.BackTrans, "es", null);
				var annDefnRepo = Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>();
				importSettings.AddFile(annotFileCons, ImportDomain.Annotations, null,
					annDefnRepo.ConsultantAnnotationDefn);
				importSettings.AddFile(annotFileTrans, ImportDomain.Annotations, null,
					annDefnRepo.TranslatorAnnotationDefn);

				StringCollection filesNotFound;
				Assert.IsTrue(importSettings.ImportProjectIsAccessible(out filesNotFound));
				Assert.AreEqual(0, filesNotFound.Count);
				importSettings.SaveSettings();

				// Blow away some project files: should still return true, but should
				// report missing files.
				FileUtils.Delete(scrFile2);
				FileUtils.Delete(scrFile3);
				FileUtils.Delete(btFileDef);
				FileUtils.Delete(annotFileCons);
				FileUtils.Delete(annotFileTrans);

				// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
				importSettings.RevertToSaved();

				Assert.IsTrue(importSettings.ImportProjectIsAccessible(out filesNotFound));
				Assert.AreEqual(5, filesNotFound.Count);

				Assert.IsTrue(filesNotFound.Contains(scrFile2));
				Assert.IsTrue(filesNotFound.Contains(scrFile3));
				Assert.IsTrue(filesNotFound.Contains(btFileDef));
				Assert.IsTrue(filesNotFound.Contains(annotFileCons));
				Assert.IsTrue(filesNotFound.Contains(annotFileTrans));

				importSettings.SaveSettings();

				// Blow away the rest of the project files: should return false and report
				// missing files.
				FileUtils.Delete(scrFile1);
				FileUtils.Delete(btFileSpan);

				// Now that we've saved the settings, we'll "revert" in order to re-load from the DB
				importSettings.RevertToSaved();

				Assert.IsFalse(importSettings.ImportProjectIsAccessible(out filesNotFound));
				Assert.AreEqual(7, filesNotFound.Count);

				Assert.IsTrue(filesNotFound.Contains(scrFile1));
				Assert.IsTrue(filesNotFound.Contains(scrFile2));
				Assert.IsTrue(filesNotFound.Contains(scrFile3));
				Assert.IsTrue(filesNotFound.Contains(btFileDef));
				Assert.IsTrue(filesNotFound.Contains(btFileSpan));
				Assert.IsTrue(filesNotFound.Contains(annotFileCons));
				Assert.IsTrue(filesNotFound.Contains(annotFileTrans));
			}
			finally
			{
				FileUtils.Manager.Reset();
			}
		}

		#endregion
	}
}
