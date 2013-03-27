using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000058 to 7000059.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000059 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000058 to 7000059.
		/// RnGenericRec.Text is now reference atomic and LangProject.Texts is now non-existent
		/// (i.e. Texts are now unowned).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000059Test()
		{
			//Bring in data from xml file.
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000059.xml");


			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "CmProject" });
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());
			mockMDC.AddClass(4, "CmMajorObject", "CmObject", new List<string>() { "RnResearchNbk", "Text" });
			mockMDC.AddClass(5, "RnResearchNbk", "CmMajorObject", new List<string>());
			mockMDC.AddClass(6, "Text", "CmMajorObject", new List<string>());
			mockMDC.AddClass(7, "RnGenericRec", "CmObject", new List<string> ());

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000058, dtos, mockMDC, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000059, new DummyProgressDlg());

			// The Texts property of the LangProject should be gone.
			var lp = dtoRepos.AllInstancesSansSubclasses("LangProject").First();
			var lpElement = XElement.Parse(lp.Xml);
			var textsElt = lpElement.Element("Texts");
			Assert.That(lpElement.Name.LocalName, Is.EqualTo("rt"));
			Assert.That(textsElt, Is.Null);

			// The Text property of the RnGenericRec should be Reference Atomic instead of owned.
			var rnRec = dtoRepos.AllInstancesSansSubclasses("RnGenericRec").First();
			var rnRecElement = XElement.Parse(rnRec.Xml);
			var textElt = rnRecElement.Element("Text");
			Assert.That(rnRecElement.Name.LocalName, Is.EqualTo("rt"));
			Assert.That(textElt, Is.Not.Null);
			var objsurType = GetObjsurType(textElt);
			Assert.AreEqual("r", objsurType, "Should have changed from owned to reference type");

			// Texts should no longer know owners.
			// First test the one that was owned by the LangProject
			var allTextDtos = dtoRepos.AllInstancesSansSubclasses("Text");
			var textDto = allTextDtos.First();
			textElt = XElement.Parse(textDto.Xml);
			var ownerAttr = textElt.Attribute("ownerguid");
			Assert.That(ownerAttr, Is.Null);

			// Then test the one that was owned by the DN record
			textDto = allTextDtos.Last();
			textElt = XElement.Parse(textDto.Xml);
			ownerAttr = textElt.Attribute("ownerguid");
			Assert.That(ownerAttr, Is.Null);

			Assert.AreEqual(7000059, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		private string GetObjsurType(XElement textElt)
		{
			var objsurElt = textElt.Element("objsur");
			if (objsurElt == null)
				return "";
			var typeAttr = objsurElt.Attribute("t");
			if (typeAttr == null)
				return "";
			return typeAttr.Value;
		}
	}
}