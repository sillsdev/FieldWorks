using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000023 to 7000024.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000024 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000023 to 7000024.
		/// (Merge the Sense Status list into the Status list)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000024Test()
		{
			int count = 0;
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000024Tests.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();

			mockMDC.AddClass(1, "CmObject", null, new List<string>
													{
														"CmProject",
														"CmMajorObject",
														"RnGenericRec",
														"LexEntry",
														"LexSense",
														"CmPossibility"
													});
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> {"LangProject"});
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());
			mockMDC.AddClass(4, "CmMajorObject", "CmObject", new List<string> {"RnResearchNbk", "CmPossibilityList", "LexDb"});
			mockMDC.AddClass(5, "RnResearchNbk", "CmMajorObject", new List<string>());
			mockMDC.AddClass(6, "RnGenericRec", "CmObject", new List<string>());
			mockMDC.AddClass(7, "CmPossibilityList", "CmMajorObject", new List<string>());
			mockMDC.AddClass(8, "LexDb", "CmMajorObject", new List<string> {});
			mockMDC.AddClass(9, "LexEntry", "CmObject", new List<string>());
			mockMDC.AddClass(10, "LexSense", "CmObject", new List<string>());
			mockMDC.AddClass(11, "CmPossibility", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000023, dtos, mockMDC, null);
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000024, new DummyProgressDlg());

			//This object should contain a 'Status' property
			var langProjDto = dtoRepos.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProjDto.Xml);
			var langProjStatus = langProjElement.XPathSelectElement("Status/objsur");
			Assert.That(langProjStatus, Is.Not.Null, "We should now have a 'Status' element on LangProj");
			//This object should not contain an 'AnalysysStatus' property
			var langProjAnalysisStatus = langProjElement.XPathSelectElement("AnalysisStatus");
			Assert.That(langProjAnalysisStatus, Is.Null, "LangProject AnalysisStatus Property should not exist any more");

			Assert.That(langProjStatus.Attribute("guid").Value.ToLowerInvariant(),
						Is.EqualTo("0084b4d9-6c1e-4d63-9c66-ff22764ef171"),
						"Status element should preserve objsur guid of old Analysis Status");
			//This LexDb element should not contain a 'Status' property
			var lexDbDto = dtoRepos.AllInstancesSansSubclasses("LexDb").First();
			var lexDbElement = XElement.Parse(lexDbDto.Xml);
			var lexDbStatus = lexDbElement.XPathSelectElement("Status");
			Assert.That(lexDbStatus, Is.Null, "LexDb Status Property exists but should have been deleted");

			//The 5 resulting possibilities should be 'Confirmed', 'Disproved', 'Pending', ''Tentative', and 'Ann'.
			var possListDto = dtoRepos.GetDirectlyOwnedDTOs("0084B4D9-6C1E-4D63-9C66-FF22764EF171");

			Assert.That(possListDto.Count(), Is.EqualTo(6), "We should have exactly six status items in the PL");
			var names = new HashSet<string>();
			foreach (var possibility in possListDto)
			{
				var possElement = XElement.Parse(possibility.Xml);
				var nameElt = possElement.XPathSelectElement("Name/AUni[@ws='en']");
				if (nameElt == null)
				{
					nameElt = possElement.XPathSelectElement("Name/AUni[@ws='id']");
				}
				var ttype = nameElt.Value;
				names.Add(ttype);
				switch (ttype)
				{
					case "Confirmed":
					case "Tentative":
					case "Disproved":
					case "Pending":
					case "Ann":
					case "Foreign":
						break;
					default:
						Assert.Fail(ttype + " is in the CmPossibility List");
						break;
				}
			}
			Assert.That(names, Has.Count.EqualTo(6), "One of the expected possibilities is missing!");

			// Verify that the LexSense Entries point to the new status entries.
			var lexSenseDto = dtoRepos.GetDirectlyOwnedDTOs("fd6bb890-bb84-4920-954d-40d1e987b683");

			Assert.That(lexSenseDto.Count(), Is.EqualTo(5), "There should be exactly five senses");
			foreach (var lexSense in lexSenseDto)
			{
				var lexSenseElement = XElement.Parse(lexSense.Xml);
				var ttype = lexSenseElement.XPathSelectElement("Gloss/AUni[@ws='en']").Value;
				switch (ttype)
				{
					case "Aardvark":
						//Make sure the Status's Guid this LexSense status is the one owned by LangProject.
						Assert.That(lexSenseElement.XPathSelectElement("Status/objsur").Attribute("guid").Value,
									Is.EqualTo("8D87FC8A-593E-4C84-A9ED-193879D08585"),
									ttype + " doesn''t point to the correct possibility.");
						break;
					case "Aardvark2":
						//Make sure the ownerguid of the Possibility for this LexSense status is the cmPossibilityList owned by langProject
						var aardDto = dtoRepos.GetDTO(lexSenseElement.XPathSelectElement("Status/objsur").Attribute("guid").Value);
						var aardStatusElement = XElement.Parse(aardDto.Xml);
						Assert.That(aardStatusElement.Attribute("ownerguid").Value.ToUpperInvariant(),
									Is.EqualTo("0084B4D9-6C1E-4D63-9C66-FF22764EF171"),
									ttype + " ownerguid isn't correct.");
						Assert.AreEqual(aardStatusElement.XPathSelectElement("Name/AUni[@ws='en']").Value,
										"Ann",
										ttype + " Possibility pointed to has the wrong name.");
						break;
					case "Aardvark3":
						//Make sure the Status's Guid this LexSense status is the one owned by LangProject.
						Assert.That(lexSenseElement.XPathSelectElement("Status/objsur").Attribute("guid").Value.ToUpperInvariant,
									Is.EqualTo("8D87FC8A-593E-4C84-A9ED-193879D08585"),
									ttype + " doesn''t point to the correct possibility.");
						break;
					case "Aardvark4":
						//Make sure the Status's Guid this LexSense status is the one owned by LangProject.
						Assert.That(lexSenseElement.XPathSelectElement("Status"), Is.Null,
									ttype + " does have a status.");
						break;
					default:
						Assert.Pass(ttype + " is in the LexSense List");
						break;
				}
			}
			// Verify that the RnGenericRec Entries point to the new status entries.
			var rnGenericRecDto = dtoRepos.GetDirectlyOwnedDTOs("4e3802af-98cd-48c4-b6ff-3cb0a5fd1310");

			Assert.That(rnGenericRecDto.Count(), Is.EqualTo(7), "There should be exactly seven RnGeneric records");
			foreach (var rnRec in rnGenericRecDto)
			{
				var rnGenericRecElement = XElement.Parse(rnRec.Xml);
				var rnStatusElem = rnGenericRecElement.XPathSelectElement("Status/objsur");
				if (rnStatusElem == null)
					continue;
				var rnStatusGuid = rnStatusElem.Attribute("guid").Value;

				var rnPossibilityDto = dtoRepos.GetDTO(rnStatusGuid);
				var rnPossibilityElement = XElement.Parse(rnPossibilityDto.Xml);
				var rnNameElem = rnPossibilityElement.XPathSelectElement("Name/AUni[@ws='en']").Value;
				var rnPossibilityOwnerGuid = rnPossibilityElement.Attribute("ownerguid").Value;

				switch (rnGenericRecElement.Attribute("guid").Value.ToLowerInvariant())
				{
					case "611739fe-8fe2-4d16-8570-b9d46c339e6e":
						VerifyOwnerguidAndStatus(rnPossibilityOwnerGuid, "RnGenericRec record 1 status record isn't owned by LangProj.",
												 rnNameElem, "Pending", "RnGenericRec record 1 doesn''t point to a status of ''Pending''.");
						break;
					case "612739fe-8fe2-4d16-8570-b9d46c339e6e":
						VerifyOwnerguidAndStatus(rnPossibilityOwnerGuid, "RnGenericRec record 2 status record isn't owned by LangProj.",
												 rnNameElem, "Confirmed",
												 "RnGenericRec record 2 doesn''t point to a status of ''Confirmed''.");
						break;
					case "613739fe-8fe2-4d16-8570-b9d46c339e6e":
						VerifyOwnerguidAndStatus(rnPossibilityOwnerGuid, "RnGenericRec record 3 status record isn't owned by LangProj.",
												 rnNameElem, "Tentative",
												 "RnGenericRec record 3 doesn''t point to a status of ''Tentative''.");
						break;
					case "614739fe-8fe2-4d16-8570-b9d46c339e6e":
						VerifyOwnerguidAndStatus(rnPossibilityOwnerGuid, "RnGenericRec record 4 status record isn't owned by LangProj.",
												 rnNameElem, "Confirmed",
												 "RnGenericRec record 4 doesn''t point to a status of ''Approved''.");
						break;
					case "615739fe-8fe2-4d16-8570-b9d46c339e6e":
						VerifyOwnerguidAndStatus(rnPossibilityOwnerGuid, "RnGenericRec record 5 status record isn't owned by LangProj.",
												 rnNameElem, "Disproved",
												 "RnGenericRec record 5 doesn''t point to a status of ''Disproved''.");
						break;
					case "616739fe-8fe2-4d16-8570-b9d46c339e6e":
						VerifyOwnerguidAndStatus(rnPossibilityOwnerGuid, "RnGenericRec record 6 status record isn't owned by LangProj.",
												 rnNameElem, "Disproved", "RnGenericRec record 6 doesn''t point to a status of ''Ann''.");
						break;
					default:
						Assert.Fail("There is no RnGenericRec with a guid of: " +
									rnGenericRecElement.Attribute("guid").Value.ToLowerInvariant());
						break;
				}
			}
			Assert.AreEqual(7000024, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		private void VerifyOwnerguidAndStatus(string rnOwnerGuid, string errorMsg1, string rnStatus, string status, string errorMsg2)
		{
			Assert.AreEqual(rnOwnerGuid.ToUpperInvariant(), "0084B4D9-6C1E-4D63-9C66-FF22764EF171", errorMsg1);
			Assert.AreEqual(rnStatus, status, errorMsg2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000023 to 7000024.
		/// (Merge the Sense Status list into the Status list)
		/// This time the destination status list doesn't exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000024Test1()
		{
			int count = 0;
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000024Tests1.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();

			mockMDC.AddClass(1, "CmObject", null, new List<string>
													{
														"CmProject",
														"CmMajorObject",
														"RnGenericRec"
													});

			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> {"LangProject"});
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());
			mockMDC.AddClass(4, "CmMajorObject", "CmObject", new List<string> {"RnResearchNbk"});
			mockMDC.AddClass(5, "RnResearchNbk", "CmMajorObject", new List<string>());
			mockMDC.AddClass(6, "RnGenericRec", "CmObject", new List<string>());

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000023, dtos, mockMDC, null);
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000024, new DummyProgressDlg());

			//This object should contain a 'Status' property
			var langProjDto = dtoRepos.AllInstancesSansSubclasses("LangProject").First();
			var langProjElement = XElement.Parse(langProjDto.Xml);
			var langProjStatus = langProjElement.XPathSelectElement("Status/objsur");
			Assert.That(langProjStatus, Is.Not.Null, "We should now have a 'Status' element on LangProj");

			//This object should not contain an 'AnalysysStatus' property
			var langProjAnalysisStatus = langProjElement.XPathSelectElement("AnalysisStatus");
			Assert.That(langProjAnalysisStatus, Is.Null, "LangProject AnalysisStatus Property should not exist any more");

			var langPossListGuid = langProjStatus.Attribute("guid").Value;
			var langPossListDto = dtoRepos.GetDTO(langPossListGuid);
			var langPossListElement = XElement.Parse(langPossListDto.Xml);
			Assert.That(langPossListElement.Attribute("ownerguid").Value.ToLowerInvariant(),
				Is.EqualTo("b5a90c21-d8b2-4d4a-94f6-1b1fbeac3388"),
				"Status element should be owned by LangProject");

			//There should be 1 possibility in the status list; 'Confirmed'.
			var possibilities = langPossListElement.XPathSelectElement("Possibilities");
			var possibilitiesDto = possibilities.Descendants();
			Assert.That(possibilitiesDto.Count(), Is.EqualTo(1), "We should have exactly one status item in the PL");

			// Verify that the RnGenericRec Entries are copied over (they won't have statuses).
			var rnGenericRecDto = dtoRepos.GetDirectlyOwnedDTOs("4e3802af-98cd-48c4-b6ff-3cb0a5fd1310");

			Assert.That(rnGenericRecDto.Count(), Is.EqualTo(7), "There should be exactly seven RnGeneric records");
			foreach (var rnRec in rnGenericRecDto)
			{
				var rnGenericRecElement = XElement.Parse(rnRec.Xml);
				var rnStatusElem = rnGenericRecElement.XPathSelectElement("Status/objsur");
				Assert.That(rnStatusElem, Is.Null, "None of the RnGeneric records should have statuses");
			}
			Assert.AreEqual(7000024, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}
	}
}