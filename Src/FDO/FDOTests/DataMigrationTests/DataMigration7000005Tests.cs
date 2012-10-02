using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000004 to 7000005.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000005 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000004 to 7000005.
		/// (Change model for Data Notebook to combine the RnEvent and RnAnalysis classes into RnGenericRec)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000005Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000005Tests.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "CmMajorObject",
																	 "RnGenericRec",
																	 "RnRoledPartic", "StText", "StPara",
																	 "CmPossibility" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "StText", "CmObject", new List<string>());
			mockMDC.AddClass(4, "CmMajorObject", "CmObject", new List<string> { "RnResearchNbk", "CmPossibilityList" });
			mockMDC.AddClass(5, "RnResearchNbk", "CmMajorObject", new List<string>());
			mockMDC.AddClass(6, "RnGenericRec", "CmObject", new List<string> { "RnEvent", "RnAnalysis" });
			mockMDC.AddClass(7, "RnRoledPartic", "CmObject", new List<string>());
			mockMDC.AddClass(8, "CmPossibility", "CmObject", new List<string> { "CmPerson", "CmAnthroItem" });
			mockMDC.AddClass(9, "StPara", "CmObject", new List<string> { "StTxtPara" });
			mockMDC.AddClass(10, "StTxtPara", "StPara", new List<string>());
			mockMDC.AddClass(11, "CmPossibilityList", "CmMajorObject", new List<string>());
			mockMDC.AddClass(12, "RnEvent", "RnGenericRec", new List<string>());
			mockMDC.AddClass(13, "RnAnalysis", "RnGenericRec", new List<string>());
			mockMDC.AddClass(14, "CmPerson", "CmPossibility", new List<string>());
			mockMDC.AddClass(15, "CmAnthroItem", "CmPossibility", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000004, dtos, mockMDC, null);
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000005);

			var nbkDto = dtoRepos.AllInstancesSansSubclasses("RnResearchNbk").First();
			var nbkElement = XElement.Parse(nbkDto.Xml);
			Assert.AreEqual(7000005, dtoRepos.CurrentModelVersion, "Wrong updated version.");
			// Get the RecTypes Element in RnResearchNbk
			Assert.IsNotNull(nbkElement.XPathSelectElement("RnResearchNbk/RecTypes"));
			// Get the EventTypes Element in RnResearchNbk (should be renamed to RecTypes)
			Assert.IsNull(nbkElement.XPathSelectElement("RnResearchNbk/EventTypes"));

			var typesDto = dtoRepos.GetDTO(nbkElement.XPathSelectElement("RnResearchNbk/RecTypes/objsur").Attribute("guid").Value);
			var typesElement = XElement.Parse(typesDto.Xml);
			// Should be 5 cmPossibility entries in the list
			Assert.AreEqual(5, typesElement.XPathSelectElements("CmPossibilityList/Possibilities/objsur").Count());

			var anaDto = dtoRepos.GetDTO("82290763-1633-4998-8317-0EC3F5027FBD");
			var anaElement = XElement.Parse(anaDto.Xml);
			Assert.AreEqual("Ana", anaElement.XPathSelectElement("CmPossibility/Abbreviation/AUni[@ws='en']").Value);

			foreach (var GenRecDto in dtoRepos.AllInstancesSansSubclasses("RnGenericRec"))
			{
				var recElement = XElement.Parse(GenRecDto.Xml);
				var typeSurElement = recElement.XPathSelectElement("RnGenericRec/Type/objsur");
				if (typeSurElement.Attribute("guid").Value == "82290763-1633-4998-8317-0EC3F5027FBD")
				{
					Assert.IsNotNull(recElement.XPathSelectElement("RnGenericRec/Conclusions"));
					Assert.AreEqual(19, recElement.Element("RnGenericRec").Elements().Count());
				}
				else
				{
					Assert.IsNotNull(recElement.XPathSelectElement("RnGenericRec/Description"));
					Assert.AreEqual(21, recElement.Elements("RnGenericRec").Elements().Count());
				}
			}

			// Look for RnEvent records -shouldn't be any
			Assert.AreEqual(0, dtoRepos.AllInstancesSansSubclasses("RnEvent").Count());
			// Look for RnAnalysis records -shouldn't be any
			Assert.AreEqual(0, dtoRepos.AllInstancesSansSubclasses("RnAnalysis").Count());
		}
	}
}