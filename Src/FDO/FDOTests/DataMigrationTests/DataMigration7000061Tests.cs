using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000060 to 7000061.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000061 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000060 to 7000061.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000061Test()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LangProject", "StStyle", "CmResource", "LexEntry", "MoStemAllomorph", "MoStemName", "MoStemMsa", "CmIndirectAnnotation", "CmBaseAnnotation", "MoMorphAdhocProhib", "StText", "StTxtPara" });
			mockMdc.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMdc.AddClass(3, "StStyle", "CmObject", new List<string>());
			mockMdc.AddClass(4, "CmResource", "CmObject", new List<string>());
			mockMdc.AddClass(5, "LexEntry", "CmObject", new List<string>());
			mockMdc.AddClass(6, "MoStemAllomorph", "CmObject", new List<string>());
			mockMdc.AddClass(7, "MoStemName", "CmObject", new List<string>());
			mockMdc.AddClass(8, "MoStemMsa", "CmObject", new List<string>());
			mockMdc.AddClass(9, "CmIndirectAnnotation", "CmObject", new List<string>());
			mockMdc.AddClass(10, "CmBaseAnnotation", "CmObject", new List<string>());
			mockMdc.AddClass(11, "MoMorphAdhocProhib", "CmObject", new List<string>());
			mockMdc.AddClass(12, "StText", "CmObject", new List<string>());
			mockMdc.AddClass(13, "StTxtPara", "CmObject", new List<string>());

			mockMdc.AddField(1001, "Name", CellarPropertyType.Unicode, 0);
			mockMdc.AddField(5001, "LexemeForm", CellarPropertyType.OwningAtomic, 6);
			mockMdc.AddField(6001, "StemName", CellarPropertyType.ReferenceAtomic, 7);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000061.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000060, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000061, new DummyProgressDlg());

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

			// Step 2. (atomic owning and ref props with multiple elements)
			dto = dtoRepos.AllInstancesSansSubclasses("LexEntry").First();
			var element = XElement.Parse(dto.Xml);
			// Atomic owning prop
			var propertyElement = element.Element("LexemeForm");
			Assert.AreEqual(1, propertyElement.Elements().Count());
			Assert.AreEqual("c1ede2e7-e382-11de-8a39-0800200c9a66", propertyElement.Element("objsur").Attribute("guid").Value);
			Assert.Throws<ArgumentException>(() => dtoRepos.GetDTO("5aedaa5f-6f71-4859-953b-a9bb1b78a813"));
			// Atomic reference prop
			dto = dtoRepos.GetDTO("c1ede2e7-e382-11de-8a39-0800200c9a66");
			element = XElement.Parse(dto.Xml);
			propertyElement = element.Element("StemName");
			Assert.AreEqual(1, propertyElement.Elements().Count());
			Assert.AreEqual("c1ede2e4-e382-11de-8a39-0800200c9a66", propertyElement.Element("objsur").Attribute("guid").Value);

			// Step 3.
			Assert.Throws<ArgumentException>(() => dtoRepos.GetDTO("003018c0-eba6-43b7-b6e2-5a71ac049f6a"));
			Assert.Throws<ArgumentException>(() => dtoRepos.GetDTO("d06d329f-9dc5-4c1c-aecd-b447cd010bdb"));
			Assert.Throws<ArgumentException>(() => dtoRepos.GetDTO("968caa2b-fae0-479a-9f4a-45d2c6827aa5"));
			Assert.Throws<ArgumentException>(() => dtoRepos.GetDTO("7a62eb69-4738-4514-a94e-b29237d5c188"));

			// Step 4.
			dto = dtoRepos.AllInstancesSansSubclasses("LexEntry").First();
			element = XElement.Parse(dto.Xml);
			propertyElement = element.Element("MorphoSyntaxAnalyses");
			Assert.AreEqual(1, propertyElement.Elements().Count());
			Assert.AreEqual("c1ede2e5-e382-11de-8a39-0800200c9a66", propertyElement.Element("objsur").Attribute("guid").Value);
			Assert.Throws<ArgumentException>(() => dtoRepos.GetDTO("c1ede2e6-e382-11de-8a39-0800200c9a66"));
			Assert.Throws<ArgumentException>(() => dtoRepos.GetDTO("304b0aea-dccd-4865-9fad-923e89871b7e"));

			Assert.AreEqual(7000061, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}
	}
}