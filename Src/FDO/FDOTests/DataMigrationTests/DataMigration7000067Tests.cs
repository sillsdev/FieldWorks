using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000066 to 7000067.
	/// </summary>
	[TestFixture]
	public sealed class DataMigration7000067Tests : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000066 to 7000067 for "Uni" element attributes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UniElementPropertiesAreRemoved()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "ClassWithUnicodeProperties", "AbstractClassWithUnicodeProperties" });
			mockMdc.AddClass(2, "ClassWithUnicodeProperties", "CmObject", new List<string>());
			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "UnicodePropWithAttrs", CellarPropertyType.Unicode, 0);
			mockMdc.AddField(++currentFlid, "UnicodePropWithoutAttrs", CellarPropertyType.Unicode, 0);

			mockMdc.AddClass(3, "AbstractClassWithUnicodeProperties", "CmObject", new List<string> { "ClassWithInheritedUnicodeProperties" });
			currentFlid = 3000;
			mockMdc.AddField(++currentFlid, "UnicodePropWithAttrs", CellarPropertyType.Unicode, 0);
			mockMdc.AddField(++currentFlid, "UnicodePropWithoutAttrs", CellarPropertyType.Unicode, 0);
			mockMdc.AddClass(4, "ClassWithInheritedUnicodeProperties", "AbstractClassWithUnicodeProperties", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000067TestData.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000066, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000067, new DummyProgressDlg());

			var instance = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("ClassWithUnicodeProperties").First().Xml);
			var uniElement = instance.Element("UnicodePropWithAttrs").Element("Uni");
			Assert.IsFalse(uniElement.HasAttributes);
			Assert.AreEqual("With Attrs", uniElement.Value);
			uniElement = instance.Element("UnicodePropWithoutAttrs").Element("Uni");
			Assert.IsFalse(uniElement.HasAttributes);
			Assert.AreEqual("Without Attrs", uniElement.Value);

			instance = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("ClassWithInheritedUnicodeProperties").First().Xml);
			uniElement = instance.Element("UnicodePropWithAttrs").Element("Uni");
			Assert.IsFalse(uniElement.HasAttributes);
			Assert.AreEqual("Inherited With Attrs", uniElement.Value);
			uniElement = instance.Element("UnicodePropWithoutAttrs").Element("Uni");
			Assert.IsFalse(uniElement.HasAttributes);
			Assert.AreEqual("Inherited Without Attrs", uniElement.Value);
		}
	}
}
