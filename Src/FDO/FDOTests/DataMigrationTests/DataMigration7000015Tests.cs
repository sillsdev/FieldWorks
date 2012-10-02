using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000014 to 7000015.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000015 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000014 to 7000015.
		/// (Remove class elements in xml.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000015Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000015.xml");

			var mockMdc = SetupMdc();

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000014, dtos, mockMdc, null);

			// SUT: Do the migration.
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000015, new DummyProgressDlg());

			// Verification Phase
			Assert.AreEqual(7000015, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// PhSimpleContextBdry
			var oldClassElementNames = new List<string> { "CmObject", "PhContextOrVar", "PhPhonContext", "PhSimpleContext", "PhSimpleContextBdry" };
			var expectedPropertyElements = new List<string> { "Name", "Description", "FeatureStructure" };
			VerifyObject(dtoRepos.GetDTO("9719A466-2240-4DEA-9722-9FE0746A30A6"), oldClassElementNames, expectedPropertyElements);
			// StText
			oldClassElementNames = new List<string> { "CmObject", "StText" };
			expectedPropertyElements = new List<string>();
			VerifyObject(dtoRepos.GetDTO("C83E33DD-2A79-4A4B-84F6-92343C4F5324"), oldClassElementNames, expectedPropertyElements);
			// PhBdryMarker
			oldClassElementNames = new List<string> { "CmObject", "PhTerminalUnit", "PhBdryMarker" };
			expectedPropertyElements = new List<string>();
			VerifyObject(dtoRepos.GetDTO("C83E33DD-2A79-4A4B-84F6-92343C4F5325"), oldClassElementNames, expectedPropertyElements);
		}

		private static void VerifyObject(DomainObjectDTO dto, IEnumerable<string> oldClassElements, ICollection<string> expectedPropertyElements)
		{
			// Make sure the old class elements are gone.
			var rtElement = XElement.Parse(dto.Xml);
			foreach (var oldClassElement in oldClassElements)
				Assert.IsNull(rtElement.Element(oldClassElement));

			// Make sure the prop elements are child elements of <rt> element.
			var propElements = rtElement.Elements();
			Assert.AreEqual(expectedPropertyElements.Count, propElements.Count(), "Wrong number of property child elements.");
			foreach (var propElement in propElements)
				Assert.IsTrue(expectedPropertyElements.Contains(propElement.Name.LocalName));
		}

		private static MockMDCForDataMigration SetupMdc()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "PhContextOrVar", "PhTerminalUnit", "StText" });
			mockMdc.AddClass(2, "PhContextOrVar", "CmObject", new List<string> { "PhPhonContext" });
			mockMdc.AddClass(3, "PhPhonContext", "PhContextOrVar", new List<string> { "PhSimpleContext" });
			mockMdc.AddClass(4, "PhSimpleContext", "PhPhonContext", new List<string> { "PhSimpleContextBdry" });
			mockMdc.AddClass(5, "PhSimpleContextBdry", "PhSimpleContext", new List<string>());
			mockMdc.AddClass(6, "PhTerminalUnit", "CmObject", new List<string> { "PhBdryMarker" });
			mockMdc.AddClass(7, "PhBdryMarker", "PhTerminalUnit", new List<string>());
			mockMdc.AddClass(8, "StText", "CmObject", new List<string>());

			return mockMdc;
		}
	}
}