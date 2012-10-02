using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000028 to 7000029.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000029 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000028 to 7000029.
		/// Change any CmFile filePaths to relative paths which are actually relative to the LinkedFilesRootDir
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000029Test()
		{
			//Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000029Tests.xml");

			//Now create all the Mock classes for the classes in my test data.
			//eg LangProject base class is CmProject which has a base class of CmObject
			//eg LexEntry base class is CmObject
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string>
													{
														"CmProject",
														"CmMajorObject",
														"LexEntry",
														"LexSense",
														"CmPicture",
														"CmFolder",
														"CmFile"
													});
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());

			mockMDC.AddClass(4, "CmMajorObject", "CmObject", new List<string> { "LexDb" });
			mockMDC.AddClass(5, "LexDb", "CmMajorObject", new List<string>());

			mockMDC.AddClass(6, "LexEntry", "CmObject", new List<string>());
			mockMDC.AddClass(7, "LexSense", "CmObject", new List<string>());
			mockMDC.AddClass(8, "CmPicture", "CmObject", new List<string>());
			mockMDC.AddClass(9, "CmFolder", "CmObject", new List<string>());
			mockMDC.AddClass(10, "CmFile", "CmObject", new List<string>());
			//-------------------+++++++++++++++++++++++++=

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000028, dtos, mockMDC, @"C:\FwWW\DistFiles\Projects\Sena 3");

			//Get the Element <rt guid="b8bdad3d-9006-46f0-83e8-ae1d1726f2ad" class="LangProject">
			var langProjDto = dtoRepos.AllInstancesSansSubclasses("LangProject").First();

			var langProjElement = XElement.Parse(langProjDto.Xml);
			var langProjLinkedFilesRootDir = langProjElement.XPathSelectElement("LinkedFilesRootDir");
			Assert.That(langProjLinkedFilesRootDir, Is.Not.Null, "Before the migration we should have a 'LinkedFilesRootDir' element on LangProj");
			var langProjPictures = langProjElement.XPathSelectElement("Pictures");
			Assert.That(langProjPictures, Is.Not.Null, "Before the migration we should have a 'Pictures' element on LangProj");
			var langProjMedia = langProjElement.XPathSelectElement("Media");
			Assert.That(langProjMedia, Is.Not.Null, "Before the migration we should have a 'Media' element on LangProj");

			//Get the Elements  for class="CmFile"
			var CmFileDtosBeforeMigration = dtoRepos.AllInstancesSansSubclasses("CmFile");
			//Get all the file paths (as strings) for the CmFile's in the project
			var filesPathsBeforeMigration = new List<String>();
			foreach (var fileDto in CmFileDtosBeforeMigration)
			{
				filesPathsBeforeMigration.Add(GetCmFilePath(fileDto));
			}
			Assert.That(filesPathsBeforeMigration.ElementAt(0), Is.EqualTo(@"Pictures\Jude1.jpg"));
			Assert.That(filesPathsBeforeMigration.ElementAt(1), Is.EqualTo(@"Pictures\Rick USVI-extraOne.jpg"));
			Assert.That(filesPathsBeforeMigration.ElementAt(2), Is.EqualTo(@"C:\FwWW\DistFiles\Projects\Sena 3\LinkedFiles\RickKayak.jpg"));
			Assert.That(filesPathsBeforeMigration.ElementAt(3), Is.EqualTo(@"C:\FwWW\DistFiles\RickKayak.jpg"));
			Assert.That(filesPathsBeforeMigration.ElementAt(4), Is.EqualTo(@"C:\FwWW\DistFiles\Projects\Sena 3\LinkedFiles\Pictures\Rick USVI.jpg"));
			Assert.That(filesPathsBeforeMigration.ElementAt(5), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
			Assert.That(filesPathsBeforeMigration.ElementAt(6), Is.EqualTo(@"C:\FwWW\DistFiles\Projects\Sena 3\LinkedFiles\AudioVisual\MacLeanKidsMovie.WMV"));
			Assert.That(filesPathsBeforeMigration.ElementAt(7), Is.EqualTo(@"C:\FwWW\DistFiles\AudioVisual\NotInLinkedFilesPath.WMV"));

			//=====================================================================================================
			//Do Migration
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000029, new DummyProgressDlg());
			//=====================================================================================================

			//make sure the version was updated.
			Assert.AreEqual(7000029, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			//Now check that all the CmFile's have paths which are correct.
			//Get the Elements  for class="CmFile"
			var CmFileDtosAfterMigration = dtoRepos.AllInstancesSansSubclasses("CmFile");
			//Get all the file paths (as strings) for the CmFile's in the project
			var filesPathsAfterMigration = new List<String>();
			foreach (var fileDto in CmFileDtosAfterMigration)
			{
				filesPathsAfterMigration.Add(GetCmFilePath(fileDto));
			}
			//Now check to ensure path are corrected relative to LinkedFiles
			//Also ensure ones that are not relative to the LinkedFiles path are not changed.
			Assert.That(filesPathsAfterMigration.ElementAt(0), Is.EqualTo(FileUtils.ChangePathToPlatform(@"Pictures\Jude1.jpg")));
			Assert.That(filesPathsAfterMigration.ElementAt(1), Is.EqualTo(FileUtils.ChangePathToPlatform(@"Pictures\Rick USVI-extraOne.jpg")));
			Assert.That(filesPathsAfterMigration.ElementAt(2), Is.EqualTo(FileUtils.ChangePathToPlatform(@"RickKayak.jpg")));
			Assert.That(filesPathsAfterMigration.ElementAt(3), Is.EqualTo(FileUtils.ChangePathToPlatform(@"C:\FwWW\DistFiles\RickKayak.jpg")));
			Assert.That(filesPathsAfterMigration.ElementAt(4), Is.EqualTo(FileUtils.ChangePathToPlatform(@"Pictures\Rick USVI.jpg")));
			Assert.That(filesPathsAfterMigration.ElementAt(5), Is.EqualTo(FileUtils.ChangePathToPlatform(@"AudioVisual\Untitled5.WMV")));
			Assert.That(filesPathsAfterMigration.ElementAt(6), Is.EqualTo(FileUtils.ChangePathToPlatform(@"AudioVisual\MacLeanKidsMovie.WMV")));
			Assert.That(filesPathsAfterMigration.ElementAt(7), Is.EqualTo(FileUtils.ChangePathToPlatform(@"C:\FwWW\DistFiles\AudioVisual\NotInLinkedFilesPath.WMV")));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fileDto">This should be a CmFile object.</param>
		/// <returns></returns>
		private string GetCmFilePath(DomainObjectDTO fileDto)
		{
			XElement cmFileXML = XElement.Parse(fileDto.Xml);
			var InternalPath = cmFileXML.XPathSelectElement("InternalPath");
			var filePathFromCmFile = cmFileXML.XPathSelectElement("InternalPath").XPathSelectElement("Uni").Value;
			return filePathFromCmFile;
		}
	}
}