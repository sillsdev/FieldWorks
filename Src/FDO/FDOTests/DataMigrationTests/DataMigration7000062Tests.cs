// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000061 to 7000062.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000062 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000061 to 7000062.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000062Test()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LangProject", "StStyle", "CmResource" });
			mockMdc.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMdc.AddClass(3, "StStyle", "CmObject", new List<string>());
			mockMdc.AddClass(4, "CmResource", "CmObject", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000062.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000061, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000062, new DummyProgressDlg());

			// Check Step 1.A.
			// <rt class="StStyle" guid ="bb68f6bc-f233-4cd4-8894-c33b4b4c43ba">
			DomainObjectDTO dto;
			dtoRepos.TryGetValue("bb68f6bc-f233-4cd4-8894-c33b4b4c43ba", out dto);
			Assert.IsNull(dto);
			// Step 1.A. Control
			// <rt class="StStyle" guid ="9d28219c-6185-416e-828b-b9e304de141c" ownerguid="88ddebd1-dfad-4033-8b1c-896081469f66">
			dtoRepos.TryGetValue("9d28219c-6185-416e-828b-b9e304de141c", out dto);
			Assert.IsNotNull(dto);

			// Check Step 1.B. (Real + control)
			foreach (var resourceDto in dtoRepos.AllInstancesSansSubclasses("CmResource"))
			{
				var resourceElement = XElement.Parse(resourceDto.Xml);
				var name = resourceElement.Element("Name").Element("Uni").Value;
				var actualVersion = resourceElement.Element("Version").Attribute("val").Value;
				var expectedVersion = "";
				switch (name)
				{
					case "TeStyles":
						expectedVersion = "700176e1-4f42-4abd-8fb5-3c586670085d";
						break;
					case "FlexStyles":
						expectedVersion = "13c213b9-e409-41fc-8782-7ca0ee983b2c";
						break;
					case "ControlResource":
						expectedVersion = "c1ede2e2-e382-11de-8a39-0800200c9a66";
						break;
				}
				Assert.AreEqual(expectedVersion, actualVersion);
			}

			Assert.AreEqual(7000062, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}
	}
}