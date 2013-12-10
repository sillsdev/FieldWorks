using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000010 to 7000011.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000011 : DataMigrationTestsBase
	{
		/// <summary>
		/// Test the migration from version 7000010 to 7000011.
		/// </summary>
		[Test]
		public void DataMigration7000011Test()
		{
			var dtos = new HashSet<DomainObjectDTO>();
			var sb = new StringBuilder();
			// 1. Add barebones Notebook.
			sb.Append("<rt class=\"RnResearchNbk\" guid=\"2E237D40-853C-49D5-AAC6-EFF01121AC25\">");
			sb.Append("<RnResearchNbk>");
			sb.Append("<RecTypes><objsur t=\"o\" guid=\"513B370D-8EFC-4C94-8192-7707677A6F98\" /></RecTypes>");
			sb.Append("<Records>");
			sb.Append("<objsur t=\"o\" guid=\"C84B721B-3617-43DE-A436-9E0538837A66\" />");
			sb.Append("</Records>");
			sb.Append("</RnResearchNbk>");
			sb.Append("</rt>");
			var nbkDto = new DomainObjectDTO("2E237D40-853C-49D5-AAC6-EFF01121AC25", "RnResearchNbk", sb.ToString());
			dtos.Add(nbkDto);

			sb = new StringBuilder();
			// 2. Add barebones RecTypes List
			sb.Append("<rt class=\"CmPossibilityList\" guid=\"513B370D-8EFC-4C94-8192-7707677A6F98\" ownerguid=\"2E237D40-853C-49D5-AAC6-EFF01121AC25\">");
			sb.Append("<CmPossibilityList>");
			sb.Append("<Possibilities>");
			sb.Append("<objsur t=\"o\" guid=\"27C32299-3B41-4FAD-A85C-F47657BCF95A\" />");
			sb.Append("<objsur t=\"o\" guid=\"5E3D9C56-404C-44C5-B3CB-99BF390E322E\" />");
			sb.Append("</Possibilities>");
			sb.Append("</CmPossibilityList>");
			sb.Append("</rt>");
			var recTypesDto = new DomainObjectDTO("513B370D-8EFC-4C94-8192-7707677A6F98", "CmPossibilityList", sb.ToString());
			dtos.Add(recTypesDto);

			sb = new StringBuilder();
			// 3. Add barebones Conversation
			sb.Append("<rt class=\"CmPossibility\" guid=\"27C32299-3B41-4FAD-A85C-F47657BCF95A\" ownerguid=\"513B370D-8EFC-4C94-8192-7707677A6F98\">");
			sb.Append("<CmPossibility>");
			sb.Append("<Abbreviation><AUni ws=\"en\">Con</AUni></Abbreviation>");
			sb.Append("</CmPossibility>");
			sb.Append("</rt>");
			var conDto = new DomainObjectDTO("27C32299-3B41-4FAD-A85C-F47657BCF95A", "CmPossibility", sb.ToString());
			dtos.Add(conDto);

			sb = new StringBuilder();
			// 4. Add barebones Observation
			sb.Append("<rt class=\"CmPossibility\" guid=\"5E3D9C56-404C-44C5-B3CB-99BF390E322E\" ownerguid=\"513B370D-8EFC-4C94-8192-7707677A6F98\">");
			sb.Append("<CmPossibility>");
			sb.Append("<Abbreviation><AUni ws=\"en\">Obs</AUni></Abbreviation>");
			sb.Append("<SubPossibilities>");
			sb.Append("<objsur t=\"o\" guid=\"9827CBE0-31F3-434E-80F7-5D5354C110B0\" />");
			sb.Append("</SubPossibilities>");
			sb.Append("</CmPossibility>");
			sb.Append("</rt>");
			var obsDto = new DomainObjectDTO("5E3D9C56-404C-44C5-B3CB-99BF390E322E", "CmPossibility", sb.ToString());
			dtos.Add(obsDto);

			sb = new StringBuilder();
			// 5. Add barebones Performance
			sb.Append("<rt class=\"CmPossibility\" guid=\"9827CBE0-31F3-434E-80F7-5D5354C110B0\" ownerguid=\"5E3D9C56-404C-44C5-B3CB-99BF390E322E\">");
			sb.Append("<CmPossibility>");
			sb.Append("<Abbreviation><AUni ws=\"en\">Per</AUni></Abbreviation>");
			sb.Append("</CmPossibility>");
			sb.Append("</rt>");
			var perDto = new DomainObjectDTO("9827CBE0-31F3-434E-80F7-5D5354C110B0", "CmPossibility", sb.ToString());
			dtos.Add(perDto);

			sb = new StringBuilder();
			// 6. Add barebones RnGenericRec
			sb.Append("<rt class=\"RnGenericRec\" guid=\"c84b721b-3617-43de-a436-9e0538837a66\" ownerguid=\"2E237D40-853C-49D5-AAC6-EFF01121AC25\">");
			sb.Append("<RnGenericRec>");
			sb.Append("<Type><objsur guid=\"27c32299-3b41-4fad-a85c-f47657bcf95a\" t=\"r\" /></Type>");
			sb.Append("</RnGenericRec>");
			sb.Append("</rt>");
			var recDto = new DomainObjectDTO("c84b721b-3617-43de-a436-9e0538837a66", "RnGenericRec", sb.ToString());
			dtos.Add(recDto);

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "RnResearchNbk", "CmPossibilityList", "CmPossibility", "RnGenericRec" });
			mockMDC.AddClass(2, "RnResearchNbk", "CmObject", new List<string>());
			mockMDC.AddClass(3, "CmPossibilityList", "CmObject", new List<string>());
			mockMDC.AddClass(4, "CmPossibility", "CmObject", new List<string>());
			mockMDC.AddClass(5, "RnGenericRec", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000010, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000011, new DummyProgressDlg());

			XElement nbkElem = XElement.Parse(nbkDto.Xml);
			Assert.AreEqual("D9D55B12-EA5E-11DE-95EF-0013722F8DEC",
				(string) nbkElem.XPathSelectElement("RnResearchNbk/RecTypes/objsur").Attribute("guid"));

			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, recTypesDto);
			recTypesDto = dtoRepos.GetDTO("D9D55B12-EA5E-11DE-95EF-0013722F8DEC");
			XElement recTypesElem = XElement.Parse(recTypesDto.Xml);
			List<XElement> objSurElems = recTypesElem.XPathSelectElements("CmPossibilityList/Possibilities/objsur").ToList();
			Assert.AreEqual(2, objSurElems.Count);
			Assert.AreEqual("B7B37B86-EA5E-11DE-80E9-0013722F8DEC", (string) objSurElems[0].Attribute("guid"));
			Assert.AreEqual("B7EA5156-EA5E-11DE-9F9C-0013722F8DEC", (string) objSurElems[1].Attribute("guid"));

			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, conDto);
			conDto = dtoRepos.GetDTO("B7B37B86-EA5E-11DE-80E9-0013722F8DEC");
			XElement conElem = XElement.Parse(conDto.Xml);
			Assert.AreEqual("Con", (string) conElem.XPathSelectElement("CmPossibility/Abbreviation/AUni[@ws='en']"));
			Assert.AreEqual("D9D55B12-EA5E-11DE-95EF-0013722F8DEC", (string) conElem.Attribute("ownerguid"));

			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, obsDto);
			obsDto = dtoRepos.GetDTO("B7EA5156-EA5E-11DE-9F9C-0013722F8DEC");
			XElement obsElem = XElement.Parse(obsDto.Xml);
			Assert.AreEqual("Obs", (string)obsElem.XPathSelectElement("CmPossibility/Abbreviation/AUni[@ws='en']"));
			Assert.AreEqual("B7F63D0E-EA5E-11DE-9F02-0013722F8DEC",
				(string) obsElem.XPathSelectElement("CmPossibility/SubPossibilities/objsur").Attribute("guid"));
			Assert.AreEqual("D9D55B12-EA5E-11DE-95EF-0013722F8DEC", (string) obsElem.Attribute("ownerguid"));

			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, perDto);
			perDto = dtoRepos.GetDTO("B7F63D0E-EA5E-11DE-9F02-0013722F8DEC");
			XElement perElem = XElement.Parse(perDto.Xml);
			Assert.AreEqual("Per", (string) perElem.XPathSelectElement("CmPossibility/Abbreviation/AUni[@ws='en']"));
			Assert.AreEqual("B7EA5156-EA5E-11DE-9F9C-0013722F8DEC", (string) perElem.Attribute("ownerguid"));

			XElement recElem = XElement.Parse(recDto.Xml);
			Assert.AreEqual("B7B37B86-EA5E-11DE-80E9-0013722F8DEC", (string) recElem.XPathSelectElement("RnGenericRec/Type/objsur").Attribute("guid"));
		}
	}
}
