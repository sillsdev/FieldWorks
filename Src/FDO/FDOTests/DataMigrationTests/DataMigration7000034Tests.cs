using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.FDO.Infrastructure;
using System.IO;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000033 to 7000034.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000034 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000032 to 7000033.
		/// Clean up uses of obsolete names "customN" in configuration files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000034Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000034.xml");

			//Now create all the Mock classes for the classes in my test data.
			//eg LangProject base class is CmProject which has a base class of CmObject
			//eg LexEntry base class is CmObject
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string>
													{
														"CmProject",
														"CmPicture",
														"CmFolder",
														"CmFile",
													});
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());

			//In the List, put each class in this test which derives from this class.
			mockMDC.AddClass(8, "CmPicture", "CmObject", new List<string>());
			mockMDC.AddClass(9, "CmFolder", "CmObject", new List<string>());
			mockMDC.AddClass(10, "CmFile", "CmObject", new List<string>());
			IDomainObjectDTORepository repoDto = new DomainObjectDtoRepository(7000033, dtos, mockMDC,
				Path.GetTempPath());

			// Do the migration.
			m_dataMigrationManager.PerformMigration(repoDto, 7000034);

			Assert.AreEqual(7000034, repoDto.CurrentModelVersion, "Wrong updated version.");

			var langProj = repoDto.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProj.Xml);

			// Not directly related to this migration, but fixed problem with DataMigrationServices.RemoveIncludingOwnedObjects
			// and couldn't find a better place to put the test. Need to make sure only empty properties on LangProject
			// get deleted.
			Assert.AreEqual("en", langProjElement.Element("AnalysisWss").Element("Uni").Value,
				"Analysis Writing systems should be preserved.");
			Assert.IsNull(langProjElement.Element("Styles"));  // empty styles in test data should have been removed.

			// Verify migration worked
			var pictures = langProjElement.Element("Pictures");
			Assert.AreEqual(1, pictures.Elements().Count(), "Should only be one folder in Pictures");
			var folder = repoDto.GetDTO(pictures.Elements().First().Attribute("guid").Value);
			var folderElement = XElement.Parse(folder.Xml);
			var files = folderElement.Element("Files");
			Assert.AreEqual(4, files.Elements().Count(), "Should be four files");
			Assert.AreEqual(4, repoDto.AllInstancesSansSubclasses("CmFile").Count(),
				"Should still be four files");
			// Verify ownership is correct
			foreach (var fileRef in files.Elements())
			{
				var file = repoDto.GetDTO(fileRef.Attribute("guid").Value);
				var fileElement = XElement.Parse(file.Xml);
				Assert.AreEqual(folder.Guid, fileElement.Attribute("ownerguid").Value, "All files should be owned by folder");
			}

			// the first CmFile is used in all except CmPictures in the test data
			var fileGuid = files.Elements().First().Attribute("guid").Value;
			Assert.AreEqual(4, repoDto.AllInstancesSansSubclasses("CmPicture").Count(),
				"Should still be four pictures");
			int matchCount = 0;
			foreach(var picture in repoDto.AllInstancesSansSubclasses("CmPicture"))
			{
				var pictureElement = XElement.Parse(picture.Xml);
				if (fileGuid ==
					pictureElement.Element("PictureFile").Element("objsur").Attribute("guid").Value)
					matchCount++;
			}
			Assert.AreEqual(3, matchCount, "Should be 3 pictures using first CmFile");
		}
	}
}