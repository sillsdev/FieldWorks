// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
// ReSharper disable PossibleNullReferenceException -- Justification: If the exception is thrown, we'll know to fix the test.

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000069 to 7000070.
	/// </summary>
	[TestFixture]
	public sealed class DataMigration7000070Tests : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000069 to 7000070 to clean up extra and wrong data inserted in DM69
		/// which hides in dusty little corners of the model
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyDefaultTypeInLexEntryRefs()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexEntryRef", "CmPossibilityList", "LanguageProject", "LexEntryType" });
			mockMdc.AddClass(2, "LexEntryRef", "CmPossibility", new List<string>());
			mockMdc.AddClass(3, "CmPossibilityList", "CmObject", new List<string>());
			mockMdc.AddClass(4, "LanguageProject", "CmObject", new List<string>());
			mockMdc.AddClass(5, "LexEntryType", "CmObject", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000070.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000069, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			Assert.AreEqual(4, dtoRepos.AllInstancesWithSubclasses("LexEntryRef").Count(), "The LexEntryRef test data has changed");
			Assert.AreEqual(2, dtoRepos.AllInstancesWithSubclasses("CmPossibilityList").Count(), "The CmPossibilityList test data has changed");

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000070, new DummyProgressDlg()); // SUT

			// Make sure new default types are added.
			var lexEntryRefs = dtoRepos.AllInstancesWithSubclasses("LexEntryRef").ToList();
			var data = XElement.Parse(lexEntryRefs[0].Xml);
			var defTypeElt = data.Element("VariantEntryTypes");
			Assert.IsNotNull(defTypeElt);
			Assert.That(defTypeElt != null && defTypeElt.HasElements, "Should have components (or variants)");
			var objSurElem = defTypeElt.Element("objsur");
			Assert.IsNotNull(objSurElem);
			Assert.AreEqual("3942addb-99fd-43e9-ab7d-99025ceb0d4e", objSurElem.FirstAttribute.Value);
			var refTypeAttr = objSurElem.Attribute("t");
			Assert.IsNotNull(refTypeAttr, "The type attribute should be set on the 'objsur' element for the default c.f.");
			Assert.AreEqual(refTypeAttr.Value, "r");
			data = XElement.Parse(lexEntryRefs[1].Xml);
			defTypeElt = data.Element("ComplexEntryTypes");
			Assert.IsNotNull(defTypeElt);
			Assert.That(defTypeElt != null && defTypeElt.HasElements, "Should have components (or variants)");
			objSurElem = defTypeElt.Element("objsur");
			Assert.IsNotNull(objSurElem);
			Assert.AreEqual("fec038ed-6a8c-4fa5-bc96-a4f515a98c50", objSurElem.FirstAttribute.Value);
			refTypeAttr = objSurElem.Attribute("t");
			Assert.IsNotNull(refTypeAttr, "The type attribute should be set on the 'objsur' element for the default variant");
			Assert.AreEqual(refTypeAttr.Value, "r");
			// Make sure that the complex form ref which had bogus VariantTypes was cleaned up
			data = XElement.Parse(lexEntryRefs[2].Xml);
			defTypeElt = data.Element("VariantEntryTypes");
			Assert.IsNull(defTypeElt);
			defTypeElt = data.Element("ComplexEntryTypes");
			Assert.IsNotNull(defTypeElt);
			Assert.That(defTypeElt != null && defTypeElt.HasElements, "Should have components (or variants)");
			objSurElem = defTypeElt.Element("objsur");
			Assert.IsNotNull(objSurElem);
			Assert.AreEqual("1f6ae209-141a-40db-983c-bee93af0ca3c", objSurElem.FirstAttribute.Value);
			refTypeAttr = objSurElem.Attribute("t");
			Assert.IsNotNull(refTypeAttr, "The type attribute should be set on the 'objsur' element for the default variant");
			Assert.AreEqual(refTypeAttr.Value, "r");
			// Make sure that a variant which had bogus ComplexFormType was cleaned up
			data = XElement.Parse(lexEntryRefs[3].Xml);
			defTypeElt = data.Element("ComplexEntryTypes");
			Assert.IsNull(defTypeElt, "Complex form types should have been removed.");
			defTypeElt = data.Element("VariantEntryTypes");
			Assert.IsNotNull(defTypeElt, "default variant type should have been added");
			Assert.That(defTypeElt != null && defTypeElt.HasElements, "Should have components (or variants)");
			objSurElem = defTypeElt.Element("objsur");
			Assert.IsNotNull(objSurElem);
			Assert.AreEqual("3942addb-99fd-43e9-ab7d-99025ceb0d4e", objSurElem.FirstAttribute.Value);
			refTypeAttr = objSurElem.Attribute("t");
			Assert.IsNotNull(refTypeAttr, "The type attribute should be set on the 'objsur' element for the default variant");
			Assert.AreEqual(refTypeAttr.Value, "r");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000069 to 7000070 when there are duplicated lists (custom and DM generated) to
		/// prove that custom lists get their titles changed to include -Custom
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DuplicatedListsAreMarkedAsCustom()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "CmPossibilityList", "LanguageProject", "CmCustomItem", "LexDb", "LexEntryRef" });
			mockMdc.AddClass(2, "CmPossibilityList", "CmObject", new List<string>());
			mockMdc.AddClass(3, "CmCustomItem", "CmObject", new List<string>());
			mockMdc.AddClass(4, "LanguageProject", "CmObject", new List<string>());
			mockMdc.AddClass(5, "LexDb", "CmObject", new List<string>());
			mockMdc.AddClass(6, "LexEntryRef", "CmObject", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000070_DoubledList.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000069, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			Assert.AreEqual(2, dtoRepos.AllInstancesWithSubclasses("CmPossibilityList").Count(), "The CmPossibilityList test data has changed");

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000070, new DummyProgressDlg()); // SUT

			var resultingLists = dtoRepos.AllInstancesWithSubclasses("CmPossibilityList").ToList();
			Assert.AreEqual(2, resultingLists.Count, "The Custom list and new replacement should be all there is");
			// Make sure that the custom list got a custom name and the 'real' owned list kept the original name
			foreach (var list in resultingLists)
			{
				var listElem = XElement.Parse(list.Xml);
				var ownerGuid = listElem.Attribute("ownerguid");
				var firstName = listElem.Element("Name").Elements("AUni").First().Value;
				Assert.That(firstName, ownerGuid == null ? Is.StringMatching("Languages-Custom") : Is.StringMatching("Languages"));
			}
		}
	}
}
