using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000012 to 7000013.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000013 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000012 to 7000013.
		/// (Change class for certain ConstChartWordGroup objects to ConstChartTag)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000013Test()
		{
			var cexpectedWordGrps = 1; // number of ConstChartWordGroups after migration
			var cexpectedTags = 3; // number of ConstChartTags after migration

			var dtoRepos = DoCommonBasics(
				new List<string> { "DataMigration7000013_Discourse.xml" });

			// Record number of WordGroup objects before migration
			var cbeforeWordGrps = dtoRepos.AllInstancesSansSubclasses("ConstChartWordGroup").Count();

			// SUT; Do the migration.
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000013, new DummyProgressDlg());

			// Verification Phase
			Assert.AreEqual(7000013, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Check the WordGroup objects
			var wordGrpList = dtoRepos.AllInstancesSansSubclasses("ConstChartWordGroup");
			var cafterWordGrps = wordGrpList.Count();
			Assert.AreEqual(cexpectedWordGrps, cafterWordGrps, "Wrong number of WordGroup objects left.");

			foreach (var wordGrpDto in wordGrpList)
			{
				// verify that ALL WordGroups have a valid BeginSegment
				var rtElement = XElement.Parse(wordGrpDto.Xml);
				var wordGrpElement = rtElement.Element("ConstChartWordGroup");
				Assert.IsNotNull(wordGrpElement, "WordGroup after migration has no WordGroup element!");
				var begSegSurElement = wordGrpElement.XPathSelectElement("BeginSegment/objsur");
				Assert.IsNotNull(begSegSurElement, "WordGroup after migration has no BeginSegment!");
				// verify that they DON'T have a ConstChartTag element or Tag element
				var tagElement = rtElement.Element("ConstChartTag");
				Assert.IsNull(tagElement, "WordGroup after migration has a ConstChartTag element!");
				var tagSurElement = wordGrpElement.XPathSelectElement("Tag/objsur");
				Assert.IsNull(tagSurElement, "WordGroup after migration has a Tag!");
			}

			// Check the Tag objects
			var tagList = dtoRepos.AllInstancesSansSubclasses("ConstChartTag");
			Assert.AreEqual(cexpectedTags, tagList.Count(), "Wrong number of Tag objects now.");

			// Count new 'missing' marker tags and compare with missing WordGroup objects
			var cnewTags = 0;
			foreach (var tagDto in tagList)
			{
				var rtElement = XElement.Parse(tagDto.Xml);
				var tagElement = rtElement.Element("ConstChartTag");
				// verify missing elements
				var wordGrpElement = rtElement.Element("ConstChartWordGroup");
				Assert.IsNull(wordGrpElement, "Tag object has a WordGroup element!");
				var begSegSurElement = tagElement.XPathSelectElement("BeginSegment/objsur");
				Assert.IsNull(begSegSurElement, "Tag object has BeginSegment!");
				var endSegSurElement = tagElement.XPathSelectElement("EndSegment/objsur");
				Assert.IsNull(endSegSurElement, "Tag object has EndSegment!");
				var begIdxSurElement = tagElement.XPathSelectElement("BeginAnalysisIndex/objsur");
				Assert.IsNull(begIdxSurElement, "Tag object has BeginIndex!");
				var endIdxSurElement = tagElement.XPathSelectElement("EndAnalysisIndex/objsur");
				Assert.IsNull(endIdxSurElement, "Tag object has EndIndex!");
				// verify new elements (check for certain tags that HAVE possibilities and
				// others that DON'T have them.
				var tagSurElement = tagElement.XPathSelectElement("Tag/objsur");
				if (tagSurElement == null)
					cnewTags++;
			}
			Assert.AreEqual(cbeforeWordGrps - cafterWordGrps, cnewTags, "Wrong number of objects converted!");
		}

		private static IDomainObjectDTORepository DoCommonBasics(IEnumerable<string> extraDataFiles)
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000013_CommonData.xml");
			foreach (var extraDataFile in extraDataFiles)
				dtos.UnionWith(DataMigrationTestServices.ParseProjectFile(extraDataFile));

			var mockMDC = SetupMDC();

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000012, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);
			return dtoRepos;
		}

		private static MockMDCForDataMigration SetupMDC()
		{
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "DsDiscourseData", "DsConstChart",
				"ConstChartRow", "CmPossibility", "ConstituentChartCellPart" });
			mockMDC.AddClass(2, "CmPossibility", "CmObject", new List<string>());
			mockMDC.AddClass(3, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(4, "DsDiscourseData", "CmObject", new List<string>());
			mockMDC.AddClass(5, "DsConstChart", "CmObject", new List<string>());
			mockMDC.AddClass(6, "ConstChartRow", "CmObject", new List<string>());
			mockMDC.AddClass(14, "ConstituentChartCellPart", "CmObject", new List<string> { "ConstChartWordGroup", "ConstChartMovedTextMarker", "ConstChartClauseMarker", "ConstChartTag" });
			mockMDC.AddClass(15, "ConstChartWordGroup", "ConstituentChartCellPart", new List<string>());
			mockMDC.AddClass(16, "ConstChartMovedTextMarker", "ConstituentChartCellPart", new List<string>());
			mockMDC.AddClass(17, "ConstChartClauseMarker", "ConstituentChartCellPart", new List<string>());
			mockMDC.AddClass(18, "ConstChartTag", "ConstituentChartCellPart", new List<string>());
			return mockMDC;
		}
	}
}