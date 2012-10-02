using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test framework for migration from version 7000040 to 7000041. This migration replaces
	/// ExcludeAsHeadword with DoNotShowMainEntryIn in LexEntries.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	class DataMigration7000041Tests : DataMigrationTestsBase
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		///  Test the migration from version 7000040 to 7000041.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000041Test()
		{
			// Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000041.xml");

			// Create all the Mock classes for the classes in my test data.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LexEntry", "CmMajorObject" });
			mockMDC.AddClass(2, "LexEntry", "CmObject", new List<string>());
			mockMDC.AddClass(3, "CmMajorObject", "CmObject", new List<string> { "CmPossibilityList" });
			mockMDC.AddClass(4, "CmPossibilityList", "CmMajorObject", new List<string>());

			TryThisProject(dtos, mockMDC, 3); // with Publications posibility list

			dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000041a.xml");

			TryThisProject(dtos, mockMDC, 1); // no Publications posibility list
		}

		private void TryThisProject(HashSet<DomainObjectDTO> dtos, MockMDCForDataMigration mockMDC, int numOfPubs)
		{
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000040, dtos, mockMDC,
				@"C:\Path\Not\Used");
			// Do Migration
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000041, new DummyProgressDlg());

			// Check that the version was updated.
			Assert.AreEqual(7000041, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// The LexEntry with ExcludeAsHeadword val="True" should be replaced by
			// DoNotShowMainEntryIn with the entire publist as objsur elements.
			VerifyReplace(dtoRepos, "7ecbb299-bf35-4795-a5cc-8d38ce8b891c", true, numOfPubs);

			// The LexEntry with no ExcludeAsHeadword should have no
			// DoNotShowMainEntryIn element.
			VerifyReplace(dtoRepos, "77b2397d-19c9-4321-b581-7a4ccfa9fc0e", false, numOfPubs);

			// The LexEntry with ExcludeAsHeadword val="False" should have no
			// DoNotShowMainEntryIn element.
			VerifyReplace(dtoRepos, "a78bb1d7-a86c-40c5-8ad1-8d75b71c4962", false, numOfPubs);

			// The LexEntry with ExcludeAsHeadword val="garbled" should have no
			// DoNotShowMainEntryIn element.
			VerifyReplace(dtoRepos, "1e4ae08a-2672-4df2-9e2d-980be367e893", false, numOfPubs);
		}

		private void VerifyReplace(IDomainObjectDTORepository dtoRepos, string guid, bool expectReplace, int numOfPubs)
		{
			var dto = dtoRepos.GetDTO(guid);
			var xElt = XElement.Parse(dto.Xml);
			Assert.That(xElt.Attribute("class").Value == "LexEntry", "Giud not representing a LexEntry after migration to 7000041.");
			var hasDoNotShow = xElt.Element("DoNotShowMainEntryIn");
			string expect = "";
			if (!expectReplace)
				expect = "not ";
			Assert.That(expectReplace == (hasDoNotShow != null),
				"LexEntry " + guid + " did " + expect + "expect ExcludeAsHeadword to be replaced by DoNotShowMainEntryIn, but it didn't happen in migration to 7000041.");
			if (expectReplace)
			{ // Check that each publication is referenced
				var noShows = SurrogatesIn(xElt, "DoNotShowMainEntryIn", numOfPubs);
				var dtoPubist = dtoRepos.GetDTO(CmPossibilityListTags.kguidPublicationsList.ToString());
				Assert.That(dtoPubist != null, "This project has no Publications list after migration 7000041");
				var xPubist = XElement.Parse(dtoPubist.Xml);
				var cfElements = SurrogatesIn(xPubist, "Possibilities", numOfPubs);
				foreach (var xObj in cfElements)
				{	// there must be 1 to 1 map of objsur's in hasDoNotShow and xPubist
					string pubGuid = xObj.Attribute("guid").Value;
					int n = 0;
					foreach (var yObj in noShows)
					{
						if (pubGuid == yObj.Attribute("guid").Value)
						{
							Assert.That(yObj.Attribute("t").Value == "r", "7000041 Migrated test XML pub list items are not references");
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Look for a child of xElt of the given name. If count is zero, it is allowed not to exist.
		/// Otherwise, it must have the specified number of children of name objsur; return them.
		/// </summary>
		IEnumerable<XElement> SurrogatesIn(XElement xElt, string name, int count)
		{
			var parent = xElt.Elements(name);
			if (parent.Count() == 0)
			{
				Assert.That(0, Is.EqualTo(count));
				return new XElement[0];
			}
			var objSurElts = parent.Elements("objsur");
			Assert.That(objSurElts.Count(), Is.EqualTo(count));
			return objSurElts;
		}
	}
}
