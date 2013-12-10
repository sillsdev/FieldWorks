using System.Collections.Generic;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000022 to 7000023.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000023 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000022 to 7000023.
		/// Shift case on all guids to lowwercase, so Chorus' diff/merge code doesn't get tripped
		/// up by one file using lowercase and another using uppercase.
		/// This happens because FW 6.0 used uppercase for guids, but FW 7.0+ uses lower.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000023Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000023.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "DsDiscourseData", "LexDb", "RnResearchNbk",
				"CmPossibilityList", "CmFilter", "UserView", "UserAppFeatAct", "CmResource", "ScrCheckRun" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "DsDiscourseData", "CmObject", new List<string>());
			mockMDC.AddClass(4, "LexDb", "CmObject", new List<string>());
			mockMDC.AddClass(5, "CmPossibilityList", "CmObject", new List<string>());
			mockMDC.AddClass(6, "CmFilter", "CmObject", new List<string>());
			mockMDC.AddClass(7, "UserView", "CmObject", new List<string>());
			mockMDC.AddClass(8, "UserAppFeatAct", "CmObject", new List<string>());
			mockMDC.AddClass(9, "CmResource", "CmObject", new List<string>());
			mockMDC.AddClass(10, "ScrCheckRun", "CmObject", new List<string>());
			mockMDC.AddClass(11, "RnResearchNbk", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000022, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			//SUT
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000023, new DummyProgressDlg());

			// Verification section
			var dto = dtoRepos.GetDTO("c1ecaa73-e382-11de-8a39-0800200c9a66");
			var rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Element("LexDb").Element("objsur").Attribute("guid").Value);
			dto = dtoRepos.GetDTO("c1ec5c4c-e382-11de-8a39-0800200c9a66");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Attribute("ownerguid").Value);
			dto = dtoRepos.GetDTO("c1Ecaa74-e382-11de-8a39-0800200c9a66");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Attribute("guid").Value);
			dto = dtoRepos.GetDTO("AF26D792-EA5E-11DE-8F7E-0013722F8DEC");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Attribute("guid").Value);
			dto = dtoRepos.GetDTO("1FBDC211-32E4-4203-9B7F-9CAACBF31DBD");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Attribute("guid").Value);
			CheckGuid(rtElement.Element("ListVersion").Attribute("val").Value);
			dto = dtoRepos.GetDTO("c1ecaa77-e382-11de-8a39-0800200c9a66");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Element("App").Attribute("val").Value);
			dto = dtoRepos.GetDTO("c1ecaa78-e382-11de-8a39-0800200c9a66");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Element("App").Attribute("val").Value);
			dto = dtoRepos.GetDTO("c1ecaa79-e382-11de-8a39-0800200c9a66");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Element("ApplicationId").Attribute("val").Value);
			dto = dtoRepos.GetDTO("c1ecaa7a-e382-11de-8a39-0800200c9a66");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Element("Version").Attribute("val").Value);
			dto = dtoRepos.GetDTO("c1ecd170-e382-11de-8a39-0800200c9a66");
			rtElement = XElement.Parse(dto.Xml);
			CheckGuid(rtElement.Element("CheckId").Attribute("val").Value);

			Assert.AreEqual(7000023, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		private static void CheckGuid(string value)
		{
			Assert.AreEqual(value, value.ToLowerInvariant());
		}
	}
}