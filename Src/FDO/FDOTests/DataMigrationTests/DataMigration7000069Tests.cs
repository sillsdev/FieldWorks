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
// ReSharper disable PossibleNullReferenceExeption -- Justification: If the exception is thrown, we'll know to fix the test.

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
			var multiStrElements = restrictionsElement.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			Assert.AreEqual(frWs, multiStrElements.FirstOrDefault().FirstAttribute.Value);
			var runElements = multiStrElements.FirstOrDefault().Descendants("Run").ToList();
			Assert.AreEqual(1, runElements.Count);
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
			multiStrElements = restrictionsElement.Descendants("AStr").ToList();
			Assert.AreEqual(2, multiStrElements.Count);
			runElements = multiStrElements.FirstOrDefault().Descendants("Run").ToList();
			Assert.AreEqual(1, runElements.Count);
			Assert.AreEqual(enWs, runElements.FirstOrDefault().FirstAttribute.Value);
			Assert.AreEqual("Sense restriction in English", runElements.FirstOrDefault().Value);
			runElements = multiStrElements.LastOrDefault().Descendants("Run").ToList();
			Assert.AreEqual(1, runElements.Count);
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
		/// Test the migration from version 7000068 to 7000069 to Remove Empty Complex Form Type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveEmptyLexEntryRefs()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "CmPossibility" });
			mockMdc.AddClass(2, "CmPossibility", "CmObject", new List<string> { "LexEntryRef" });
			mockMdc.AddClass(3, "LexEntryRef", "CmPossibility", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			Assert.AreEqual(2, dtoRepos.AllInstancesWithSubclasses("LexEntryRef").Count(), "The test data has changed");

			DataMigration7000069.RemoveEmptyLexEntryRefs(dtoRepos); // SUT

			// Make sure Empty complex form has been removed.
			var survivingRefs = dtoRepos.AllInstancesWithSubclasses("LexEntryRef").ToList();
			Assert.AreEqual(1, survivingRefs.Count, "empty ref should have been removed");
			var data = XElement.Parse(survivingRefs[0].Xml);
			var referees = data.Element("ComponentLexemes");
			Assert.That(referees != null && referees.HasElements, "Should have components (or variants)");
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
			mockMdc.AddClass(3, "LexEntryType", "CmPossibility", new List<string> {"LexEntryInflType"});
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

			DataMigration7000069.AddReverseNameAndSwapAbbreviationFields(dtoRepos); // SUT

			const string frWs = "fr";
			const string enWs = "en";
			var firstEntryType = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("LexEntryType").First().Xml);

			var nameElement = firstEntryType.Element("Name");
			var multiUniElements = nameElement.Descendants("AUni").ToList();
			Assert.AreEqual(1, multiUniElements.Count);
			var uniString = multiUniElements[0].Value;
			Assert.AreEqual("Dialectal Variant", uniString);

			var reversenameElement = firstEntryType.Element("ReverseName");
			multiUniElements = reversenameElement.Descendants("AUni").ToList();
			Assert.AreEqual(1, multiUniElements.Count);
			uniString = multiUniElements[0].Value;
			Assert.AreEqual("Dialectal Variant of", uniString);
			var attr = multiUniElements[0].FirstAttribute;
			Assert.AreEqual("ws", attr.Name.ToString());
			Assert.AreEqual(enWs, attr.Value);

			// Past is a subpossibility and also has multiple language strings.
			var pastEntry = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("LexEntryType").First(
											e => e.Guid.ToString()=="837ebe72-8c1d-4864-95d9-fa313c499d78").Xml);

			// We only test the English contents. Transforming "of" from any language and predicting the outcome
			// from the Name.Value would be near impossible.
			reversenameElement = pastEntry.Element("ReverseName");
			multiUniElements = reversenameElement.Descendants("AUni").ToList();
			Assert.AreEqual(1, multiUniElements.Count);
			uniString = multiUniElements[0].Value;
			Assert.AreEqual("Past of", uniString);
			attr = multiUniElements[0].FirstAttribute;
			Assert.AreEqual("ws", attr.Name.ToString());
			Assert.AreEqual(enWs, attr.Value);

			var abbrElement = pastEntry.Element("Abbreviation");
			multiUniElements = abbrElement.Descendants("AUni").ToList();
			Assert.AreEqual(2, multiUniElements.Count);
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == enWs).Value;
			Assert.AreEqual("pst.", uniString);
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == frWs).Value;
			Assert.AreEqual("pss.", uniString);
			var revAbbrElement = pastEntry.Element("ReverseAbbr");
			multiUniElements = revAbbrElement.Descendants("AUni").ToList();
			Assert.AreEqual(2, multiUniElements.Count);
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == enWs).Value;
			Assert.AreEqual("pst. of", uniString);
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == frWs).Value;
			Assert.AreEqual("pss. de", uniString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000068 to 7000069 adding DoNotPublishIn fields to
		/// both LexPronunciation and CmPicture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDoNotPublishInPropertyToLexPronunciationAndCmPicture()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexEntry", "LexSense", "LexPronunciation", "CmPicture" });
			mockMdc.AddClass(2, "LexEntry", "CmObject", new List<string>());
			mockMdc.AddClass(3, "LexSense", "CmObject", new List<string>());
			mockMdc.AddClass(4, "LexPronunciation", "CmObject", new List<string>());
			mockMdc.AddClass(5, "CmPicture", "CmObject", new List<string>());

			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "Senses", CellarPropertyType.OwningSequence, 2);
			mockMdc.AddField(++currentFlid, "Pronunciations", CellarPropertyType.OwningSequence, 2);
			mockMdc.AddField(++currentFlid, "Pictures", CellarPropertyType.OwningSequence, 3);
			mockMdc.AddField(++currentFlid, "Form", CellarPropertyType.MultiString, 4);
			mockMdc.AddField(++currentFlid, "Caption", CellarPropertyType.MultiUnicode, 5);
			mockMdc.AddField(++currentFlid, "DoNotPublishIn", CellarPropertyType.ReferenceSequence, 4);
			mockMdc.AddField(++currentFlid, "DoNotPublishIn", CellarPropertyType.ReferenceSequence, 5);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			// Make sure CmPicture and LexPronunciation do not have DoNotPublishIn prior to migration.
			foreach (var dto in dtoRepos.AllInstancesWithSubclasses("CmPicture"))
			{
				var elt = XElement.Parse(dto.Xml);
				Assert.IsNull(elt.Element("DoNotPublishIn"));
			}
			foreach (var dto in dtoRepos.AllInstancesWithSubclasses("LexPronunciation"))
			{
				var elt = XElement.Parse(dto.Xml);
				Assert.IsNull(elt.Element("DoNotPublishIn"));
			}

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000069, new DummyProgressDlg());

			// Since we're just adding two fields that will be empty initially,
			// we just need to verify that nothing in our data changed.
			const string frWs = "fr";
			const string frPhoneticWs = "fr-fonipa";
			const string frAudioWs = "fr-Zxxx-x-audio";
			const string entryGuid = "7ecbb299-bf35-4795-a5cc-8d38ce8b891c";
			const string senseGuid = "e3c2d179-3ccd-431e-ac2e-100bdb883680";

			// Verify Pronunciation
			var firstEntry = XElement.Parse(dtoRepos.GetDTO(entryGuid).Xml);
			var entryPronElement = firstEntry.Element("Pronunciations");
			var pronPointers = entryPronElement.Descendants("objsur").ToList();
			Assert.AreEqual(1, pronPointers.Count, "There should be one Pronunciation object");
			var pronGuid = pronPointers[0].Attribute("guid").Value;
			var allPronunciationDtos = dtoRepos.AllInstancesWithSubclasses("LexPronunciation").ToList();
			Assert.AreEqual(1, allPronunciationDtos.Count, "There should be one Pronunciation object");
			var pronElement = XElement.Parse(allPronunciationDtos[0].Xml);
			Assert.AreEqual(pronGuid, pronElement.Attribute("guid").Value, "LexEntry guid pointer doesn't point to right LexPronunciation object");
			var forms = pronElement.Descendants("Form").ToList();
			Assert.AreEqual(1, forms.Count);
			var multiUniElements = forms[0].Descendants("AUni").ToList();
			Assert.AreEqual(2, multiUniElements.Count);
			var uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == frPhoneticWs).Value;
			Assert.AreEqual("se pron\x00f5se k\x0259m sa", uniString);
			uniString = multiUniElements.First(wselt => wselt.Attribute("ws").Value == frAudioWs).Value;
			Assert.AreEqual("12345A LexPronunciation.wav", uniString);

			// Verify Picture
			var sense = XElement.Parse(dtoRepos.GetDTO(senseGuid).Xml);
			var sensePicElement = sense.Element("Pictures");
			var picPointers = sensePicElement.Descendants("objsur").ToList();
			Assert.AreEqual(1, picPointers.Count, "There should be one Picture object");
			var picGuid = picPointers[0].Attribute("guid").Value;
			var allPictureDtos = dtoRepos.AllInstancesWithSubclasses("CmPicture").ToList();
			Assert.AreEqual(1, allPictureDtos.Count, "There should be one Picture object");
			var picElement = XElement.Parse(allPictureDtos[0].Xml);
			Assert.AreEqual(picGuid, picElement.Attribute("guid").Value,
				"LexSense guid pointer doesn't point to right CmPicture object");
			Assert.AreEqual(1, picElement.Descendants("Caption").Count());
			var caption = picElement.Descendants("Caption").FirstOrDefault();
			multiUniElements = caption.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiUniElements.Count, "There should only be one caption");
			var astrElt = multiUniElements.First();
			Assert.AreEqual(frWs, astrElt.FirstAttribute.Value);
			var runs = astrElt.Descendants("Run").ToList();
			Assert.AreEqual(1, runs.Count);
			var runElt = runs[0];
			Assert.AreEqual(frWs, runElt.FirstAttribute.Value);
			Assert.AreEqual("grenouille", runElt.Value);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000068 to 7000069 to Create Exemplar field, which is a MuliString allowing
		/// for runs of multiple writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddExemplarFieldToLexSense()
		{
			var mockMdc = new MockMDCForDataMigration();
			const int lexSenseClid = 2;
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexSense", "CmPossibilityList" });
			mockMdc.AddClass(lexSenseClid, "LexSense", "CmObject", new List<string>());
			mockMdc.AddClass(3, "CmPossibilityList", "CmObject", new List<string>());

			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "Exemplar", CellarPropertyType.MultiString, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "Exemplar0", CellarPropertyType.MultiString, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "Exemplar", CellarPropertyType.MultiString, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "Exemplar", CellarPropertyType.MultiUnicode, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "Test Exemplar", CellarPropertyType.MultiString, lexSenseClid);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069_CustomExemplar.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			const string frWs = "fr";
			const string enWs = "en";

			// Make sure LexSense does not have Exemplar prior to migration.
			var lexSenseDtos = dtoRepos.AllInstancesSansSubclasses("LexSense").ToList();
			foreach (var dto in lexSenseDtos)
			{
				var elt = XElement.Parse(dto.Xml);
				Assert.IsNull(elt.Element("Exemplar"));
			}

			var firstSense = XElement.Parse(lexSenseDtos[0].Xml);

			var customElt = firstSense.Element("Custom");
			Assert.IsNotNull(customElt);
			var nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("Exemplar0", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			var secondSense = XElement.Parse(lexSenseDtos[1].Xml);

			customElt = secondSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("Exemplar", nameAttr.Value);
			Assert.AreEqual(2, customElt.Descendants("AUni").Count());

			var thirdSense = XElement.Parse(lexSenseDtos[2].Xml);

			customElt = thirdSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("Exemplar", nameAttr.Value);
			Assert.AreEqual(2, customElt.Descendants("AStr").Count());

			var fourthSense = XElement.Parse(lexSenseDtos[3].Xml);

			customElt = fourthSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("Test Exemplar", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			DataMigration7000069.MigrateIntoNewMultistringField(dtoRepos, "Exemplar"); // SUT

			lexSenseDtos = dtoRepos.AllInstancesSansSubclasses("LexSense").ToList(); // not that we expect them to change

			firstSense = XElement.Parse(lexSenseDtos[0].Xml);

			customElt = firstSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("Exemplar0", nameAttr.Value, "non-conflicting Custom Field data should not have been renamed");

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			var multiStrElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			var runElts = multiStrElements.Descendants("Run").ToList();
			Assert.AreEqual(1, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Exemplar using AStr in English", runElts[0].Value);

			Assert.IsNull(firstSense.Element("Exemplar"), "No Exemplar CF; nothing to migrate");

			secondSense = XElement.Parse(lexSenseDtos[1].Xml);

			customElt = secondSense.Element("Custom");
			Assert.IsNull(customElt);

			var exemplarElt = secondSense.Element("Exemplar");
			Assert.IsNotNull(exemplarElt, "Exemplar CF Exists; migrate data");

			CollectionAssert.IsEmpty(exemplarElt.Descendants("AUni"));
			multiStrElements = exemplarElt.Descendants("AStr").ToList();
			Assert.AreEqual(2, multiStrElements.Count);
			runElts = multiStrElements.Descendants("Run").ToList();
			Assert.AreEqual(2, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Exemplar in English", runElts[0].Value);
			Assert.AreEqual(frWs, runElts[1].FirstAttribute.Value);
			Assert.AreEqual("Exemplar du Usage Custome en Francais", runElts[1].Value);

			thirdSense = XElement.Parse(lexSenseDtos[2].Xml);

			customElt = thirdSense.Element("Custom");
			Assert.IsNull(customElt);

			exemplarElt = thirdSense.Element("Exemplar");
			Assert.IsNotNull(exemplarElt, "Exemplar CF Exists; migrate data");

			CollectionAssert.IsEmpty(exemplarElt.Descendants("AUni"));
			multiStrElements = exemplarElt.Descendants("AStr").ToList();
			Assert.AreEqual(2, multiStrElements.Count);
			runElts = multiStrElements.Descendants("Run").ToList();
			Assert.AreEqual(2, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Exemplar in English", runElts[0].Value);
			Assert.AreEqual(frWs, runElts[1].FirstAttribute.Value);
			Assert.AreEqual("Exemplar du Usage Custome en Francais", runElts[1].Value);

			fourthSense = XElement.Parse(lexSenseDtos[3].Xml);

			customElt = fourthSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.Attribute("name");
			Assert.IsNotNull(nameAttr);
			Assert.AreEqual("Test Exemplar", nameAttr.Value);

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			var multiUniElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiUniElements.Count);
			Assert.AreEqual(enWs, multiUniElements[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Test Exemplar using AStr in English", multiUniElements[0].Value);

			Assert.IsNull(fourthSense.Element("Exemplar"), "No Exemplar CF; nothing to migrate");
		}

			// ReSharper disable PossibleMultipleEnumeration TODO: fix multiple enumerations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000068 to 7000069 to Create UsageNote field, which is a MuliString allowing
		/// for a run of multiple writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddUsageNoteFieldToLexSense()
		{
			var mockMdc = new MockMDCForDataMigration();
			const int lexSenseClid = 2;
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexSense", "CmPossibilityList" });
			mockMdc.AddClass(lexSenseClid, "LexSense", "CmObject", new List<string>());
			mockMdc.AddClass(3, "CmPossibilityList", "CmObject", new List<string>());

			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "UsageNote", CellarPropertyType.MultiString, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "UsageNote", CellarPropertyType.MultiString, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "UsageNote1", CellarPropertyType.MultiString, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "UsageNote", CellarPropertyType.MultiUnicode, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "Test Note", CellarPropertyType.MultiString, lexSenseClid);
			mockMdc.AddCustomField("LexSense", "UsageNote", CellarPropertyType.Integer, lexSenseClid);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069_CustomUsageNote.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			const string frWs = "fr";
			const string enWs = "en";

			// Make sure LexSense does not have UsageNote prior to migration.
			var lexSenseDtos = dtoRepos.AllInstancesSansSubclasses("LexSense").ToList();
			foreach (var dto in lexSenseDtos)
			{
				var elt = XElement.Parse(dto.Xml);
				Assert.IsNull(elt.Element("UsageNote"));
			}

			var firstSense = XElement.Parse(lexSenseDtos[0].Xml);

			var customElt = firstSense.Elements("Custom");
			Assert.IsNotNull(customElt);
			var nameAttr = customElt.FirstOrDefault().FirstAttribute;
			Assert.AreEqual("UsageNote", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			var secondSense = XElement.Parse(lexSenseDtos[1].Xml);

			customElt = secondSense.Elements("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstOrDefault().FirstAttribute;
			Assert.AreEqual("UsageNote1", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			var thirdSense = XElement.Parse(lexSenseDtos[2].Xml);

			customElt = thirdSense.Elements("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstOrDefault().FirstAttribute;
			Assert.AreEqual("UsageNote", nameAttr.Value);
			Assert.AreEqual(2, customElt.Descendants("AUni").Count());

			var fourthSense = XElement.Parse(lexSenseDtos[3].Xml);

			customElt = fourthSense.Elements("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstOrDefault().FirstAttribute;
			Assert.AreEqual("Test Note", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			var fifthSense = XElement.Parse(lexSenseDtos[4].Xml);

			customElt = fifthSense.Elements("Custom");
			Assert.IsNotNull(customElt);

			nameAttr = customElt.Attributes("name").FirstOrDefault();
			Assert.IsNotNull(nameAttr);
			Assert.AreEqual("UsageNote", nameAttr.Value);
			var valAttr = customElt.Attributes("val").FirstOrDefault();
			Assert.IsNotNull(valAttr);
			Assert.AreEqual("42", valAttr.Value);

			DataMigration7000069.MigrateIntoNewMultistringField(dtoRepos, "UsageNote"); // SUT

			firstSense = XElement.Parse(lexSenseDtos[0].Xml);

			customElt = firstSense.Elements("Custom");
			Assert.IsEmpty(customElt);

			customElt = firstSense.Elements("UsageNote");
			Assert.IsNotNull(customElt);

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			var multiStrElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			var runElts = multiStrElements.Descendants("Run").ToList();
			Assert.AreEqual(1, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Usage Note using AStr in English", runElts[0].Value);

			secondSense = XElement.Parse(lexSenseDtos[1].Xml);

			customElt = secondSense.Elements("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstOrDefault().FirstAttribute;
			Assert.AreEqual("UsageNote1", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			multiStrElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			runElts = multiStrElements.FirstOrDefault().Descendants("Run").ToList();
			Assert.AreEqual(1, runElts.Count);
			Assert.AreEqual(enWs, runElts.FirstOrDefault().FirstAttribute.Value);
			Assert.AreEqual("Custom Usage Note using AStr in English", runElts.FirstOrDefault().Value);

			var usagenoteElt = secondSense.Elements("UsageNote");
			Assert.IsNotNull(usagenoteElt);

			thirdSense = XElement.Parse(lexSenseDtos[2].Xml);

			customElt = thirdSense.Elements("Custom");
			Assert.IsNotNull(customElt);

			usagenoteElt = thirdSense.Elements("UsageNote");
			Assert.IsNotNull(customElt);

			CollectionAssert.IsEmpty(usagenoteElt.Descendants("AUni"));
			multiStrElements = usagenoteElt.Descendants("AStr").ToList();
			Assert.AreEqual(2, multiStrElements.Count);
			runElts = multiStrElements.Descendants("Run").ToList();
			Assert.AreEqual(2, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Usage Note in English", runElts[0].Value);
			Assert.AreEqual(frWs, runElts[1].FirstAttribute.Value);
			Assert.AreEqual("Note du Usage Custome en Francais", runElts[1].Value);

			fourthSense = XElement.Parse(lexSenseDtos[3].Xml);

			customElt = fourthSense.Elements("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.Attributes("name").FirstOrDefault();
			Assert.IsNotNull(nameAttr);
			Assert.AreEqual("Test Note", nameAttr.Value);

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			multiStrElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			runElts = multiStrElements.FirstOrDefault().Descendants("Run").ToList();
			Assert.AreEqual(1, runElts.Count);
			Assert.AreEqual(enWs, runElts.FirstOrDefault().FirstAttribute.Value);
			Assert.AreEqual("Custom Test Note using AStr in English", runElts.FirstOrDefault().Value);

			usagenoteElt = fourthSense.Elements("UsageNote");
			Assert.IsNotNull(usagenoteElt);

			fifthSense = XElement.Parse(lexSenseDtos[4].Xml);

			customElt = fifthSense.Elements("Custom");
			Assert.IsNotNull(customElt);

			nameAttr = customElt.Attributes("name").FirstOrDefault();
			Assert.IsNotNull(nameAttr);
			Assert.AreEqual("UsageNote0", nameAttr.Value, "conflicting Custom Field should be renamed with 'UsageNote0' in this case");
			valAttr = customElt.Attributes("val").FirstOrDefault();
			Assert.IsNotNull(valAttr);
			Assert.AreEqual("42", valAttr.Value);

			usagenoteElt = fifthSense.Elements("UsageNote");
			Assert.IsNotNull(usagenoteElt);
		}

	}
}
