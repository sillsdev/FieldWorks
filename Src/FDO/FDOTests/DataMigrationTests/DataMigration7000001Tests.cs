using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000000 to 7000001,
	/// as well as the Delint method of DataMigrationServices.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000001 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000000 to 7000001.
		///
		/// Also test the Delint mechanism, while we are at it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000001_and_Delint_Tests()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			// 1. Add barebones LP.
			// LP will have one extra property to make sure it isnt; affected.
			// LP will also have a couple 'dangling' references for when 'Delint' is tested.
			// LP will also have an empty Name element that ought to be removed.
			var xml =
				string.Format("<rt class=\"LangProject\" guid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\">{0}" +
					"<CmObject></CmObject>{0}" +
					"<LangProject>{0}" +
						"<Name>{0}" +
						"</Name>{0}" +
						"<FakeBoolProperty val=\"True\" />{0}" +
						"<FakeProperty>bogus content</FakeProperty>{0}" +
						"<EthnologueCode>{0}" +
							"<Uni>ZPI</Uni>{0}" +
						"</EthnologueCode>{0}" +
						"<WordformInventory>{0}" +
							"<objsur guid=\"6C84F84A-5B99-4CF5-A7D5-A308DDC604E0\" t=\"o\"/>{0}" +
						"</WordformInventory>{0}" +
						"<AnalysisStatus>{0}" +
							"<objsur guid=\"44AF225F-964C-4F7B-BE51-1AE05995D38C\" t=\"o\" />{0}" +
						"</AnalysisStatus>{0}" +
						"<CurVernWss>{0}" +
							"<objsur guid=\"D75F7FB5-BABD-4D60-B57F-E188BEF264B7\" t=\"r\"/>{0}" +
						"</CurVernWss>{0}" +
					"</LangProject>{0}" +
				"</rt>", Environment.NewLine);
			var lpDto = new DomainObjectDTO("9719A466-2240-4DEA-9722-9FE0746A30A6",
											"LangProject",
											xml);
			dtos.Add(lpDto);

			xml = string.Format(
				"<rt class=\"WordformInventory\" guid=\"6C84F84A-5B99-4CF5-A7D5-A308DDC604E0\" ownerguid=\"9719A466-2240-4DEA-9722-9FE0746A30A6\" owningflid=\"6001013\" owningord=\"1\">{0}" +
					"<CmObject></CmObject>{0}" +
					"<WordformInventory>{0}" +
						"<Wordforms>{0}" +
							"<objsur guid=\"88304983-CDB2-460B-B3D5-5F95C66F27FF\" t=\"o\" />{0}" +
							"<objsur guid=\"59821DAB-AB03-470E-B430-5696A0503A08\" t=\"o\" />{0}" +
						"</Wordforms>{0}" +
					"</WordformInventory>{0}" +
				"</rt>", Environment.NewLine);
			var wfiDto = new DomainObjectDTO("6C84F84A-5B99-4CF5-A7D5-A308DDC604E0",
											 "WordformInventory",
											 xml);
			dtos.Add(wfiDto);

			// 3. Add two wordforms
			// First wordform.
			xml = string.Format(
				"<rt class=\"WfiWordform\" guid=\"88304983-CDB2-460B-B3D5-5F95C66F27FF\" ownerguid=\"6C84F84A-5B99-4CF5-A7D5-A308DDC604E0\" owningflid=\"5063001\" owningord=\"1\">{0}" +
					"<CmObject></CmObject>{0}" +
					"<WfiWordform>{0}" +
						"<Checksum val=\"1722980789\"/>{0}" +
						"<Form>{0}" +
							"<AUni ws=\"eZPI\">aerekondixyonada</AUni>{0}" +
						"</Form>{0}" +
						"<SpellingStatus val=\"1\"/>{0}" +
					"</WfiWordform>{0}" +
				"</rt>", Environment.NewLine);
			var wf1Dto = new DomainObjectDTO("88304983-CDB2-460B-B3D5-5F95C66F27FF",
											 "WfiWordform",
											 xml);
			dtos.Add(wf1Dto);

			// Second wordform.
			xml = string.Format(
				"<rt class=\"WfiWordform\" guid=\"59821DAB-AB03-470E-B430-5696A0503A08\" ownerguid=\"6C84F84A-5B99-4CF5-A7D5-A308DDC604E0\" owningflid=\"5063001\" owningord=\"2\">{0}" +
					"<CmObject></CmObject>{0}" +
					"<WfiWordform>{0}" +
						"<Checksum val=\"-1933028922\"/>{0}" +
						"<Form>{0}" +
							"<AUni ws=\"eZPI\">aeropwerto</AUni>{0}" +
						"</Form>{0}" +
						"<SpellingStatus val=\"1\"/>{0}" +
					"</WfiWordform>{0}" +
				"</rt>", Environment.NewLine);
			var wf2Dto = new DomainObjectDTO("59821DAB-AB03-470E-B430-5696A0503A08",
											 "WfiWordform",
											 xml);
			dtos.Add(wf2Dto);

			// Add zombie, which is where an object's owner does not exist.
			xml = @"<rt class=""LexSense"" guid=""3462BE3E-4817-4BBE-B2B9-30828B48E2C7"" ownerguid=""0875E978-79C5-4F87-95FE-A4235C0711C1"" owningflid=""5002011"" owningord=""1"" />";
			var zombie = new DomainObjectDTO("3462BE3E-4817-4BBE-B2B9-30828B48E2C7",
											 "LexSense",
											 xml);
			dtos.Add(zombie);
			// Add another zombie,
			// which is where an object's owner *does* exist,
			// but it doesn't know it owns the zombie.
			xml = @"<rt class=""Text"" guid=""c1ecaa72-e382-11de-8a39-0800200c9a66"" ownerguid=""9719A466-2240-4DEA-9722-9FE0746A30A6"" owningflid=""6001006"" owningord=""1"" />";
			var zombie2 = new DomainObjectDTO("c1ecaa72-e382-11de-8a39-0800200c9a66",
											 "Text",
											 xml);
			dtos.Add(zombie2);

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "Text", "WfiWordform", "LexSense" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "Text", "CmObject", new List<string>());
			mockMDC.AddClass(4, "WfiWordform", "CmObject", new List<string>());
			mockMDC.AddClass(5, "LexSense", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000000, dtos, mockMDC, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000001);

			// Make sure version number is correct.
			Assert.AreEqual(7000001, dtoRepos.CurrentModelVersion, "Wrong updated version.");
			// Make sure <rt class=\"LangProject\" ...> has no WFI property.
			var lpElement = XElement.Parse(lpDto.Xml);
			var lpInnerLpElement = lpElement.Element("LangProject");
			Assert.IsNotNull(lpInnerLpElement, "Oops. The 'LangProject' node was also eaten :-(.");

			Assert.IsNull(lpInnerLpElement.Element("WordformInventory"), "Still has WFI in the LangProj element.");

			// Sanity checks.
			Assert.IsNotNull(lpInnerLpElement.Element("EthnologueCode"), "Oops. The 'EthnologueCode' was also eaten :-(.");

			// Make sure there is no WordformInventory <rt> element
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, wfiDto);

			// Make sure the two wordforms have no owning-related attrs.
			var wfRtElements = new List<XElement> { XElement.Parse(wf1Dto.Xml), XElement.Parse(wf2Dto.Xml) };
			foreach (var wfElement in wfRtElements)
			{
				Assert.IsNull(wfElement.Attribute("ownerguid"), "Still has 'ownerguid'attr.");
				Assert.IsNull(wfElement.Attribute("owningflid"), "Still has 'owningflid'attr.");
				Assert.IsNull(wfElement.Attribute("owningord"), "Still has 'owningord'attr.");
				// Sanity checks.
				Assert.IsNotNull(wfElement.Descendants("WfiWordform").FirstOrDefault(), "Oops. The 'WfiWordform' element was also eaten :-(.");
				Assert.IsNotNull(wfElement.Descendants("Checksum").FirstOrDefault(), "Oops. The 'Checksum' element was also eaten :-(.");
			}

			// [NB: Other unit tests need not check Delint, as once is good enough. :-)]
			// Make sure Delint worked.
			// Make sure dangling owned object was removed.
			var analStatusElement = lpElement.Descendants("AnalysisStatus").FirstOrDefault();
			Assert.IsNull(analStatusElement, "Now empty element was not removed.");
			// Make sure dangling regular reference was removed.
			var curVernWssElement = lpElement.Descendants("CurVernWss").FirstOrDefault();
			Assert.IsNull(curVernWssElement, "Now empty element was not removed.");
			// Make sure zombie was removed.
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, zombie);
			// Make sure zombie2 was removed.
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, zombie2);
			// Make sure Delint handled emtpy properties correctly.
			Assert.IsNull(lpInnerLpElement.Element("Name"), "Empty 'Name' property not removed.");
			Assert.IsNotNull(lpInnerLpElement.Element("FakeBoolProperty"), "Oops. 'FakeBoolProperty' removed.");
			Assert.IsNull(lpInnerLpElement.Element("FakeProperty"), "'FakeProperty' survived.");
		}
	}
}