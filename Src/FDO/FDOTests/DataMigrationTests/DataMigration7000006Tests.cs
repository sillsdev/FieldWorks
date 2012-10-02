using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000005 to 7000006.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000006 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000005 to 7000006.
		/// (Change model for Data Notebook to combine the RnEvent and RnAnalysis classes into RnGenericRec)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000006Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000006Tests.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "CmProject" });
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000005, dtos, mockMDC, null);
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000006, new DummyProgressDlg());

			var cmProjDto = dtoRepos.AllInstancesSansSubclasses("LangProject").First();
			var cpElement = XElement.Parse(cmProjDto.Xml);
			Assert.AreEqual(7000006, dtoRepos.CurrentModelVersion, "Wrong updated version.");
			// Get the Description Element in cmProject
			Assert.IsNotNull(cpElement.XPathSelectElement("CmProject/Description"));
			// Get the Name Element in cmProject (should be gone)
			Assert.IsNull(cpElement.XPathSelectElement("CmProject/Name"));
		}
	}
}