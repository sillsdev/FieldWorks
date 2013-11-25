// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000065Tests.cs
// Responsibility: gordonm

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000064 to 7000065.
	/// </summary>
	[TestFixture]
	public sealed class DataMigration7000065Tests : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000064 to 7000065.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000065Test()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "CmMajorObject", "CmPossibility" });
			mockMdc.AddClass(2, "CmMajorObject", "CmObject", new List<string>() { "LexDb", "CmPossibilityList", "ReversalIndex" });
			mockMdc.AddClass(3, "LexDb", "CmMajorObject", new List<string>());
			mockMdc.AddClass(4, "CmPossibilityList", "CmMajorObject", new List<string>());
			mockMdc.AddClass(5, "ReversalIndex", "CmMajorObject", new List<string>());
			mockMdc.AddClass(6, "CmPossibility", "CmObject", new List<string>() {"PartOfSpeech"});
			mockMdc.AddClass(7, "PartOfSpeech", "CmPossibility", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000065.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000064, dtos, mockMdc, null);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000065, new DummyProgressDlg());

			var possListsList = dtoRepos.AllInstancesSansSubclasses("CmPossibilityList").ToArray();
			Assert.AreEqual(2, possListsList.Length, "Wrong number of Possibility Lists");
			var enPoslist = possListsList[0];
			var frPoslist = possListsList[1];
			var enPoslistElt = XElement.Parse(enPoslist.Xml);
			var frPoslistElt = XElement.Parse(frPoslist.Xml);

			// Check that English Parts of Speech list's Name didn't change
			var enName = enPoslistElt.Element("Name"); // this one shouldn't change
			var enNameStrings = enName.Elements("AUni");
			Assert.AreEqual(1, enNameStrings.Count(), "Changed number of Name strings.");
			var enNameStr = enNameStrings.First().Value;
			Assert.AreEqual("RandomEnglish", enNameStr, "Name of list changed");

			// Check that French Parts of Speech list's Name got created.
			var frName = frPoslistElt.Element("Name"); // should have been created
			var frNameStrings = frName.Elements("AUni");
			Assert.AreEqual(2, frNameStrings.Count(), "Should have created 2 Name strings.");
			var frNameStr1 = frNameStrings.First().Value;
			var frNameStr2 = frNameStrings.Last().Value;
			Assert.AreEqual(string.Format(Strings.ksReversalIndexPOSListName, "French"), frNameStr1,
				"Name1 was not created correctly.");
			Assert.AreEqual(string.Format(Strings.ksReversalIndexPOSListName, "German"), frNameStr2,
				"Name2 was not created correctly.");

			// Check that ItemClsId was added to French parts of speech list
			var frItemClsId = frPoslistElt.Element("ItemClsid").Attribute("val").Value;
			Assert.AreEqual("5049", frItemClsId, "Migration added wrong ItemClsid.");

			// Check that both IsSorted were changed to 'True'
			var enIsSorted = enPoslistElt.Element("IsSorted").Attribute("val").Value;
			Assert.AreEqual("True", enIsSorted, "English IsSorted didn't get changed.");
			var frIsSorted = frPoslistElt.Element("IsSorted").Attribute("val").Value;
			Assert.AreEqual("True", frIsSorted, "French IsSorted didn't get changed.");

			// Check that both Depths were changed to '127'
			var enDepth = enPoslistElt.Element("Depth").Attribute("val").Value;
			Assert.AreEqual("127", enDepth, "English Depth didn't get changed.");
			var frDepth = frPoslistElt.Element("Depth").Attribute("val").Value;
			Assert.AreEqual("127", frDepth, "French Depth didn't get changed.");

			Assert.AreEqual(7000065, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}
	}
}
