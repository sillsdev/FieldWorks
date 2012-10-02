using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000029 to 7000030.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000030 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000029 to 7000030.
		/// (Merge the Sense Status list into the Status list)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000030Test()
		{
			//Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000030.xml");

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
														"CmFile",
														"CmPossibility",
														"StText",
														"StPara"
													});
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());

			//In the List, put each class in this test which derives from this class.
			mockMDC.AddClass(4, "CmMajorObject", "CmObject", new List<string> { "LexDb", "CmPossibilityList" });
			mockMDC.AddClass(5, "LexDb", "CmMajorObject", new List<string>());

			mockMDC.AddClass(6, "LexEntry", "CmObject", new List<string>());
			mockMDC.AddClass(7, "LexSense", "CmObject", new List<string>());
			mockMDC.AddClass(8, "CmPicture", "CmObject", new List<string>());
			mockMDC.AddClass(9, "CmFolder", "CmObject", new List<string>());
			mockMDC.AddClass(10, "CmFile", "CmObject", new List<string>());

			//This class CmPossibilityList needs to be in the List of the CmMajorObject class
			mockMDC.AddClass(11, "CmPossibilityList", "CmMajorObject", new List<string>());

			mockMDC.AddClass(12, "CmPossibility", "CmObject", new List<string> { "MoMorphType" });
			mockMDC.AddClass(13, "MoMorphType", "CmPossibility", new List<string>());

			mockMDC.AddClass(14, "StText", "CmObject", new List<string>());
			mockMDC.AddClass(15, "StPara", "CmObject", new List<string> { "StTxtPara" });
			mockMDC.AddClass(16, "StTxtPara", "StPara", new List<string>());

			//-------------------+++++++++++++++++++++++++=

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000029, dtos, mockMDC, @"C:\FwWW\DistFiles\Projects\Sena 3");


			//Get the Element <rt guid="b8bdad3d-9006-46f0-83e8-ae1d1726f2ad" class="LangProject">
			var langProjDto = dtoRepos.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProjDto.Xml);

			var langProjLinkedFilesRootDir = langProjElement.XPathSelectElement("LinkedFilesRootDir");
			Assert.That(langProjLinkedFilesRootDir, Is.Not.Null, "Before the migration we should have a 'ExtLinkRootDir' element on LangProj");

			var langProjPictures = langProjElement.XPathSelectElement("Pictures");
			Assert.That(langProjPictures, Is.Not.Null, "Before the migration we should have a 'Pictures' element on LangProj");
			var langProjMedia = langProjElement.XPathSelectElement("Media");
			Assert.That(langProjMedia, Is.Not.Null, "Before the migration we should have a 'Media' element on LangProj");
			var langProjFilePathsInTsStrings = langProjElement.XPathSelectElement("FilePathsInTsStrings");
			Assert.That(langProjFilePathsInTsStrings, Is.Null, "Before the migration we should NOT have a 'FilePathsInTsStrings' element on LangProj");

			//Get the Elements  for class="CmFolder"
			var CmFolderDtos = dtoRepos.AllInstancesSansSubclasses("CmFolder");  //should we check for FilePathsInTsStrings CmFolder???
			Assert.True(CmFolderDtos.Count() == 2, "The number of CmFolders should be 2.");
			var CmFolders = new Dictionary<string, HashSet<String>>();
			GetCmFolderNamesAndObjsurs(CmFolderDtos, CmFolders);
			CheckCmFoldersAndCountsBeforeMigration(CmFolders);

			//Get the Elements  for class="CmFile"
			var CmFileDtosBeforeMigration = dtoRepos.AllInstancesSansSubclasses("CmFile");
			//Get all the file paths (as strings) for the CmFile's in the project
			var filesPathsBeforeMigration = new List<String>();
			foreach (var fileDto in CmFileDtosBeforeMigration)
			{
				filesPathsBeforeMigration.Add(GetCmFilePath(fileDto));
			}
			CheckCmFilePathsBeforeMigration(filesPathsBeforeMigration);


			var filesPathsInTsStringsBeforeMigration = GetAllExternalLinksInTsStrings(dtoRepos);
			CheckExternalLinksInTsStringsBeforeMigration(filesPathsInTsStringsBeforeMigration);

			//=====================================================================================================
			//Do Migration
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000030);
			//=====================================================================================================

			//make sure the version was updated.
			Assert.AreEqual(7000030, dtoRepos.CurrentModelVersion, "Wrong updated version.");


			//Make sure all file paths in TsStrings are now corrected if they are relative to LinkedFilesRootDir
			var filesPathsInTsStringsAfterMigration = GetAllExternalLinksInTsStrings(dtoRepos);
			CheckExternalLinksInTsStringsAfterMigration(filesPathsInTsStringsAfterMigration);

			//Now make sure we have a CmFolder called FilePathsInTsStrings as part of LangProject
			langProjDto = dtoRepos.AllInstancesSansSubclasses("LangProject").First();
			langProjElement = XElement.Parse(langProjDto.Xml);

			langProjPictures = langProjElement.XPathSelectElement("Pictures");
			Assert.That(langProjPictures, Is.Not.Null, "After the migration we should have a 'Pictures' element on LangProj");
			langProjMedia = langProjElement.XPathSelectElement("Media");
			Assert.That(langProjMedia, Is.Not.Null, "After the migration we should have a 'Media' element on LangProj");
			langProjFilePathsInTsStrings = langProjElement.XPathSelectElement("FilePathsInTsStrings");
			Assert.That(langProjFilePathsInTsStrings, Is.Not.Null, "After the migration we should  have a 'FilePathsInTsStrings' element on LangProj");

			//Get the Elements  for class="CmFolder"
			CmFolderDtos = dtoRepos.AllInstancesSansSubclasses("CmFolder");  //should we check for FilePathsInTsStrings CmFolder???
			Assert.True(CmFolderDtos.Count() == 3, "The number of CmFolders should be 3.");


			CmFolders.Clear();
			GetCmFolderNamesAndObjsurs(CmFolderDtos, CmFolders);
			CheckCmFoldersAndCountsAfterMigration(CmFolders);

			//Now check that all the CmFile's have paths which are correct.
			//Get the Elements  for class="CmFile"
			var CmFileDtosAfterMigration = dtoRepos.AllInstancesSansSubclasses("CmFile");

			//Get all the file paths (as strings) for the CmFile's in the project
			var filesPathsAfterMigration = new List<String>();
			var CmFileGuidsAfterMigration = new List<String>();
			foreach (var fileDto in CmFileDtosAfterMigration)
			{
				filesPathsAfterMigration.Add(GetCmFilePath(fileDto));
				CmFileGuidsAfterMigration.Add(GetCmFileGuid(fileDto));
			}
			CheckCmFilePathsAfterMigration(filesPathsAfterMigration);
			CheckThereIsACmFileForEachGuidInNewCmFolder(CmFolders, CmFileGuidsAfterMigration);
		}

		/// <summary>
		/// Go through the list of guids found in the new CmFolder named "File paths in TsStrings" and make
		/// sure there is a CmFile with the same guid.
		/// </summary>
		/// <param name="CmFolders"></param>
		/// <param name="CmFileGuidsAfterMigration"></param>
		private void CheckThereIsACmFileForEachGuidInNewCmFolder(Dictionary<string, HashSet<string>> CmFolders, List<string> CmFileGuidsAfterMigration)
		{
			var newCmFolderObjsurGuids = CmFolders[StringUtils.LocalFilePathsInTsStrings];
			foreach (var newCmFolderObjsurGuid in newCmFolderObjsurGuids)
			{
				Assert.That(CmFileGuidsAfterMigration.Contains(newCmFolderObjsurGuid), "On of the guids found in the new CmFolder named 'File paths in TsStrings'  has no matching CmFile.");
			}
		}

		private void CheckCmFoldersAndCountsBeforeMigration(Dictionary<string, HashSet<string>> CmFolders)
		{
			Assert.That(CmFolders.Count, Is.EqualTo(2), "The number of CmFolders should be 2.");
			Assert.AreEqual(5, CmFolders[StringUtils.LocalPictures].Count, "The number of references to CmFiles is incorrect in this CmFolder.");
			Assert.AreEqual(2, CmFolders[StringUtils.LocalMedia].Count, "The number of references to CmFiles is incorrect in this CmFolder.");
		}

		private void CheckCmFoldersAndCountsAfterMigration(Dictionary<string, HashSet<string>> CmFolders)
		{
			Assert.That(CmFolders.Count, Is.EqualTo(3), "The number of CmFolders should be 3.");
			Assert.AreEqual(5, CmFolders[StringUtils.LocalPictures].Count, "The number of references to CmFiles is incorrect in this CmFolder.");
			Assert.AreEqual(2, CmFolders[StringUtils.LocalMedia].Count, "The number of references to CmFiles is incorrect in this CmFolder.");
			Assert.AreEqual(9, CmFolders[StringUtils.LocalFilePathsInTsStrings].Count, "The number of references to CmFiles is incorrect in this CmFolder.");
		}

		private void GetCmFolderNamesAndObjsurs(IEnumerable<DomainObjectDTO> CmFolderDtos, Dictionary<string, HashSet<string>> CmFolders)
		{
			foreach (var folderDto in CmFolderDtos)
			{
				XElement cmFolderXML = XElement.Parse(folderDto.Xml);
				var nameOfCmFolder = cmFolderXML.XPathSelectElement("Name").XPathSelectElement("AUni").Value;
				var objsurElements = cmFolderXML.XPathSelectElement("Files").XPathSelectElements("objsur");
				var fileGuids = new HashSet<string>();
				foreach (var objsurElement in objsurElements)
				{
					if (objsurElement != null)
						fileGuids.Add(objsurElement.Attribute("guid").Value);
				}
				CmFolders.Add(nameOfCmFolder, fileGuids);
			}
		}
		private List<String> GetAllExternalLinksInTsStrings(IDomainObjectDTORepository dtoRepos)
		{
			var filesPathsInTsStrings = new List<String>();
			foreach (var dto in dtoRepos.AllInstancesWithSubclasses("CmObject"))  //Get the Elements for all CmObjects
			{
				if (!dto.Xml.Contains("externalLink"))
					continue;
				//byte[] matchString = Encoding.UTF8.GetBytes("externalLink");
				//if (!dto.XmlBytes.Contains(matchString))
				//    continue;
				var dtoXML = XElement.Parse(dto.Xml);
				foreach (var runXMLElement in dtoXML.XPathSelectElements("//Run"))
				{
					var externalLinkAttributeForThisRun = runXMLElement.Attribute("externalLink");
					if (externalLinkAttributeForThisRun != null)
					{
						filesPathsInTsStrings.Add(externalLinkAttributeForThisRun.Value);
						//Check the path and if it is a rooted path which is relative to the LinkedFilesRootDir
						//then we will have to confirm that is was changed to a relative path after the migration.
					}
				}
			}
			return filesPathsInTsStrings;
		}

		private void CheckExternalLinksInTsStringsBeforeMigration(List<string> filesPathsInTsStringsBeforeMigration)
		{
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(0), Is.EqualTo(@"http:\www.efccm.ca\wordpress\"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(1), Is.EqualTo(@"silfw:\localhost\link?app%3dflex%26database%3dc%3a%5cFwWW%5cDistFiles%5cProjects%5cSena+3%5cSena+3.fwdata%26tool%3dlexiconEdit%26guid%3da15b8ae6-207c-4999-b1e0-f95a348d1bdc%26tag%3d"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(2), Is.EqualTo(@"Pictures\RickKayak.jpg"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(3), Is.EqualTo(@"Pictures\Jude1.jpg"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(4), Is.EqualTo(@"Others\LukeGrad-BW.pdf"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(5), Is.EqualTo(@"Pictures\Rick USVI.jpg"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(6), Is.EqualTo(@"C:\FwWW\DistFiles\Projects\Sena 3\LinkedFiles\Others\LukeGrad-BW.pdf"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(7), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(8), Is.EqualTo(@"C:\FwWW\Users\CuriousGeorge\Babar\NotInLinkedFilesPath.mov"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(9), Is.EqualTo(@"http://icoder.wordpress.com/2008/11/12/how-to-collapse-all-projects-in-a-visual-studio-2008-solution/"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(10), Is.EqualTo(@" http://icoder.wordpress.com/2008/11/12/how-to-collapse-all-projects-in-a-visual-studio-2008-solution/"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(11), Is.EqualTo(@"C:\FwWW\DistFiles\Projects\Sena 3\LinkedFiles\Pictures\RickKayak.jpg"));
			Assert.That(filesPathsInTsStringsBeforeMigration.ElementAt(12), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
		}

		private void CheckExternalLinksInTsStringsAfterMigration(List<string> filesPathsInTsStringsAfterMigration)
		{
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(0), Is.EqualTo(@"http:\www.efccm.ca\wordpress\"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(1), Is.EqualTo(@"silfw:\localhost\link?app%3dflex%26database%3dc%3a%5cFwWW%5cDistFiles%5cProjects%5cSena+3%5cSena+3.fwdata%26tool%3dlexiconEdit%26guid%3da15b8ae6-207c-4999-b1e0-f95a348d1bdc%26tag%3d"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(2), Is.EqualTo(@"Pictures\RickKayak.jpg"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(3), Is.EqualTo(@"Pictures\Jude1.jpg"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(4), Is.EqualTo(@"Others\LukeGrad-BW.pdf"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(5), Is.EqualTo(@"Pictures\Rick USVI.jpg"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(6), Is.EqualTo(FileUtils.ChangeWindowsPathIfLinux(@"Others\LukeGrad-BW.pdf")));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(7), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(8), Is.EqualTo(@"C:\FwWW\Users\CuriousGeorge\Babar\NotInLinkedFilesPath.mov"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(9), Is.EqualTo(@"http://icoder.wordpress.com/2008/11/12/how-to-collapse-all-projects-in-a-visual-studio-2008-solution/"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(10), Is.EqualTo(@" http://icoder.wordpress.com/2008/11/12/how-to-collapse-all-projects-in-a-visual-studio-2008-solution/"));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(11), Is.EqualTo(FileUtils.ChangeWindowsPathIfLinux(@"Pictures\RickKayak.jpg")));
			Assert.That(filesPathsInTsStringsAfterMigration.ElementAt(12), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
		}
		private void CheckCmFilePathsAfterMigration(List<string> filesPaths)
		{
			//Now check to ensure CmFile paths that existed before the migration are still there.

			Assert.That(filesPaths.ElementAt(0), Is.EqualTo(@"Pictures\RickKayak.jpg"));
			Assert.That(filesPaths.ElementAt(1), Is.EqualTo(@"Pictures\Jude1.jpg"));
			Assert.That(filesPaths.ElementAt(2), Is.EqualTo(@"RickKayak.jpg"));
			Assert.That(filesPaths.ElementAt(3), Is.EqualTo(@"C:\FwWW\DistFiles\RickKayak.jpg"));
			Assert.That(filesPaths.ElementAt(4), Is.EqualTo(@"Pictures\Rick USVI.jpg"));
			Assert.That(filesPaths.ElementAt(5), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
			Assert.That(filesPaths.ElementAt(6), Is.EqualTo(@"C:\FwWW\DistFiles\AudioVisual\NotInLinkedFilesPath.WMV"));

			//Also ensure new CmFile paths are created based on all the file paths found.  This should be a new test really since they will be found
			//inside the FilePathsInTsStrings LangProject element.
			Assert.That(filesPaths.ElementAt(7), Is.EqualTo(@"Pictures\RickKayak.jpg"));
			Assert.That(filesPaths.ElementAt(8), Is.EqualTo(@"Pictures\Jude1.jpg"));
			Assert.That(filesPaths.ElementAt(9), Is.EqualTo(@"Others\LukeGrad-BW.pdf"));
			Assert.That(filesPaths.ElementAt(10), Is.EqualTo(@"Pictures\Rick USVI.jpg"));
			Assert.That(filesPaths.ElementAt(11), Is.EqualTo(FileUtils.ChangeWindowsPathIfLinux(@"Others\LukeGrad-BW.pdf")));
			Assert.That(filesPaths.ElementAt(12), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
			Assert.That(filesPaths.ElementAt(13), Is.EqualTo(FileUtils.ChangeWindowsPathIfLinux(@"C:\FwWW\Users\CuriousGeorge\Babar\NotInLinkedFilesPath.mov")));
			Assert.That(filesPaths.ElementAt(14), Is.EqualTo(FileUtils.ChangeWindowsPathIfLinux(@"Pictures\RickKayak.jpg")));
			Assert.That(filesPaths.ElementAt(15), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
		}

		private void CheckCmFilePathsBeforeMigration(List<string> filesPaths)
		{
			Assert.That(filesPaths.ElementAt(0), Is.EqualTo(@"Pictures\RickKayak.jpg"));
			Assert.That(filesPaths.ElementAt(1), Is.EqualTo(@"Pictures\Jude1.jpg"));
			Assert.That(filesPaths.ElementAt(2), Is.EqualTo(@"RickKayak.jpg"));
			Assert.That(filesPaths.ElementAt(3), Is.EqualTo(@"C:\FwWW\DistFiles\RickKayak.jpg"));
			Assert.That(filesPaths.ElementAt(4), Is.EqualTo(@"Pictures\Rick USVI.jpg"));
			Assert.That(filesPaths.ElementAt(5), Is.EqualTo(@"AudioVisual\Untitled5.WMV"));
			Assert.That(filesPaths.ElementAt(6), Is.EqualTo(@"C:\FwWW\DistFiles\AudioVisual\NotInLinkedFilesPath.WMV"));
		}

		private string GetCmFilePath(DomainObjectDTO fileDto)
		{
			XElement cmFileXML = XElement.Parse(fileDto.Xml);
			var filePathMoreDirect = cmFileXML.XPathSelectElement("InternalPath").XPathSelectElement("Uni").Value;
			return filePathMoreDirect;
		}

		private string GetCmFileGuid(DomainObjectDTO fileDto)
		{
			XElement cmFileXML = XElement.Parse(fileDto.Xml);
			var cmFileGuid = cmFileXML.Attribute("guid").Value;
			return cmFileGuid;
		}
	}
}