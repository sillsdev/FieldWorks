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
	}
}
