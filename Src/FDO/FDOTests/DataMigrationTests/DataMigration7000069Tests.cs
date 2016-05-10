// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// Test framework for migration from version 7000068 to 7000069.
	/// </summary>
	[TestFixture]
	public sealed class DataMigration7000069Tests : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000068 to 7000069 for the Restrictions field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestrictionsFieldChangedFromMultiUnicodeToMultiString()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexEntry", "LexSense" });
			mockMdc.AddClass(2, "LexEntry", "CmObject", new List<string>());
			mockMdc.AddClass(3, "LexSense", "CmObject", new List<string>());

			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "Restrictions", CellarPropertyType.MultiUnicode, 2);
			mockMdc.AddField(++currentFlid, "Restrictions", CellarPropertyType.MultiUnicode, 3);
			mockMdc.AddField(++currentFlid, "CitationForm", CellarPropertyType.MultiUnicode, 2);
			mockMdc.AddField(++currentFlid, "Gloss", CellarPropertyType.MultiUnicode, 3);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			// Make sure Restrictions has no AStr elements prior to migration.
			foreach (var dto in dtoRepos.AllInstancesSansSubclasses("LexEntry").Union(dtoRepos.AllInstancesSansSubclasses("LexSense")))
			{
				var elt = XElement.Parse(dto.Xml);
				var restrictionsElt = elt.Element("Restrictions");
				// Some of the LexEntry and LexSense test classes do not contain Restrictions.
				if (restrictionsElt != null)
				{
					CollectionAssert.IsNotEmpty(restrictionsElt.Elements("AUni"));
					CollectionAssert.IsEmpty(restrictionsElt.Elements("AStr"));
				}
			}

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000069, new DummyProgressDlg());

			const string frWs = "fr";
			const string enWs = "en";
			var firstEntry = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("LexEntry").First().Xml);
			var restrictionsElement = firstEntry.Element("Restrictions");
			CollectionAssert.IsEmpty(restrictionsElement.Descendants("AUni"));
			var multiStrElements = restrictionsElement.Descendants("AStr");
			Assert.AreEqual(1, multiStrElements.Count());
			Assert.AreEqual(frWs, multiStrElements.FirstOrDefault().FirstAttribute.Value);
			var runElements = multiStrElements.FirstOrDefault().Descendants("Run");
			Assert.AreEqual(1, runElements.Count());
			Assert.AreEqual(frWs, runElements.FirstOrDefault().FirstAttribute.Value);
			Assert.AreEqual("Restrictions sur une entrée", runElements.FirstOrDefault().Value);

			var lastEntry = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("LexEntry").Last().Xml);
			restrictionsElement = lastEntry.Element("Restrictions");
			Assert.IsNull(restrictionsElement);
			var citationForm = lastEntry.Element("CitationForm");
			Assert.AreEqual(1, citationForm.Descendants("AUni").Count()); // didn't change other AUnis

			var firstSense = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("LexSense").First().Xml);
			restrictionsElement = firstSense.Element("Restrictions");
			CollectionAssert.IsEmpty(restrictionsElement.Descendants("AUni"));
			multiStrElements = restrictionsElement.Descendants("AStr");
			Assert.AreEqual(2, multiStrElements.Count());
			runElements = multiStrElements.FirstOrDefault().Descendants("Run");
			Assert.AreEqual(1, runElements.Count());
			Assert.AreEqual(enWs, runElements.FirstOrDefault().FirstAttribute.Value);
			Assert.AreEqual("Sense restriction in English", runElements.FirstOrDefault().Value);
			runElements = multiStrElements.LastOrDefault().Descendants("Run");
			Assert.AreEqual(1, runElements.Count());
			Assert.AreEqual(frWs, runElements.FirstOrDefault().FirstAttribute.Value);
			Assert.AreEqual("Restriction en français sur un sens", runElements.FirstOrDefault().Value);

			var lastSense = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("LexSense").Last().Xml);
			restrictionsElement = lastSense.Element("Restrictions");
			Assert.IsNull(restrictionsElement);
			var gloss = lastSense.Element("Gloss");
			Assert.AreEqual(1, gloss.Descendants("AUni").Count()); // didn't change other AUnis
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000068 to 7000069 from Name to ReverseName field, and swapping of the
		/// Abbreviation and ReverseAbbr fields.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddReverseNamePropertyToLexEntryType()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> {"CmPossibility"});
			mockMdc.AddClass(2, "CmPossibility", "CmObject", new List<string>{"LexEntryType"});
			mockMdc.AddClass(3, "LexEntryType", "CmPossibility", new List<string> { "LexEntryInflType" });
			mockMdc.AddClass(4, "LexEntryInflType", "LexEntryType", new List<string>());

			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "Name", CellarPropertyType.MultiUnicode, 2);
			mockMdc.AddField(++currentFlid, "Abbreviation", CellarPropertyType.MultiUnicode, 2);
			mockMdc.AddField(++currentFlid, "SubPossibilities", CellarPropertyType.OwningSequence, 2);
			mockMdc.AddField(++currentFlid, "ReverseAbbr", CellarPropertyType.MultiUnicode, 3);
			mockMdc.AddField(++currentFlid, "ReverseName", CellarPropertyType.MultiUnicode, 3);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			// Make sure Variant and Complex Form Type do not have ReverseName prior to migration.
			foreach (var dto in dtoRepos.AllInstancesWithSubclasses("LexEntryType"))
			{
				var elt = XElement.Parse(dto.Xml);
				Assert.IsNull(elt.Element("ReverseName"));
			}

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000069, new DummyProgressDlg());

			const string frWs = "fr";
			const string enWs = "en";
			var firstEntry = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("LexEntryType").First().Xml);

			var nameElement = firstEntry.Element("Name");
			var multiUniElements = nameElement.Descendants("AUni");
			Assert.AreEqual(1, multiUniElements.Count());
			var uniString = multiUniElements.FirstOrDefault().Value;
			Assert.AreEqual("Dialectal Variant", uniString);

			var reversenameElement = firstEntry.Element("ReverseName");
			multiUniElements = reversenameElement.Descendants("AUni");
			Assert.AreEqual(1, multiUniElements.Count());
			uniString = multiUniElements.FirstOrDefault().Value;
			Assert.AreEqual("Dialectal Variant of", uniString);
			var attr = multiUniElements.FirstOrDefault().FirstAttribute;
			Assert.AreEqual("ws", attr.Name.ToString());
			Assert.AreEqual(enWs, attr.Value);

			// Past is a subpossibility and also has multiple language strings.
			var pastEntry = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("LexEntryType").First(
											e => e.Guid.ToString()=="837ebe72-8c1d-4864-95d9-fa313c499d78").Xml);

			// We only test the English contents. Transforming "of" from any language and predicting the outcome
			// from the Name.Value would be near impossible.
			reversenameElement = pastEntry.Element("ReverseName");
			multiUniElements = reversenameElement.Descendants("AUni");
			Assert.AreEqual(1, multiUniElements.Count());
			uniString = multiUniElements.FirstOrDefault().Value;
			Assert.AreEqual("Past of", uniString);
			attr = multiUniElements.FirstOrDefault().FirstAttribute;
			Assert.AreEqual("ws", attr.Name.ToString());
			Assert.AreEqual(enWs, attr.Value);

			var abbrElement = pastEntry.Element("Abbreviation");
			multiUniElements = abbrElement.Descendants("AUni");
			Assert.AreEqual(2, multiUniElements.Count());
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == enWs).Value;
			Assert.AreEqual("pst.", uniString);
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == frWs).Value;
			Assert.AreEqual("pss.", uniString);
			var revAbbrElement = pastEntry.Element("ReverseAbbr");
			multiUniElements = revAbbrElement.Descendants("AUni");
			Assert.AreEqual(2, multiUniElements.Count());
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == enWs).Value;
			Assert.AreEqual("pst. of", uniString);
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == frWs).Value;
			Assert.AreEqual("pss. de", uniString);
		}
	}
}
