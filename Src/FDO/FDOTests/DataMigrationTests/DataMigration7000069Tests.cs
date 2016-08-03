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
// ReSharper disable PossibleNullReferenceException -- Justification: If the exception is thrown, we'll know to fix the test.

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000068 to 7000069.
	/// </summary>
	[TestFixture]
	public sealed class DataMigration7000069Tests : DataMigrationTestsBase
	{
		// ReSharper disable InconsistentNaming
		private const string enWs = "en";
		private const string frWs = "fr";
		// ReSharper restore InconsistentNaming
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000068 to 7000069 for the Restrictions field.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestrictionsFieldChangedFromMultiUnicodeToMultiString()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexEntry", "LexSense", "CmPossibilityList" });
			mockMdc.AddClass(2, "LexEntry", "CmObject", new List<string>());
			mockMdc.AddClass(3, "LexSense", "CmObject", new List<string>());
			mockMdc.AddClass(4, "CmPossibilityList", "CmObject", new List<string>());

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
		/// Test the migration from version 7000068 to 7000069 to add default type for Complex form type and Variant Type.
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

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069_UnspecComplexAndVariantType.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			Assert.AreEqual(2, dtoRepos.AllInstancesWithSubclasses("LexEntryRef").Count(), "The LexEntryRef test data has changed");
			Assert.AreEqual(2, dtoRepos.AllInstancesWithSubclasses("CmPossibilityList").Count(), "The CmPossibilityList test data has changed");

			DataMigration7000069.AddDefaultLexEntryRefType(dtoRepos); // SUT

			// Make sure new default types are added.
			var defaultRefs = dtoRepos.AllInstancesWithSubclasses("LexEntryRef").ToList();
			XElement data = XElement.Parse(defaultRefs[0].Xml);

			var defTypeElt = data.Element("ComplexEntryTypes");
			Assert.IsNotNull(defTypeElt);
			Assert.That(defTypeElt != null && defTypeElt.HasElements, "Should have components (or variants)");
			var objSurAttr = defTypeElt.Element("objsur");
			Assert.IsNotNull(objSurAttr);
			Assert.AreEqual("fec038ed-6a8c-4fa5-bc96-a4f515a98c50", objSurAttr.FirstAttribute.Value);

			data = XElement.Parse(defaultRefs[1].Xml);
			defTypeElt = data.Element("VariantEntryTypes");
			Assert.IsNotNull(defTypeElt);
			Assert.That(defTypeElt != null && defTypeElt.HasElements, "Should have components (or variants)");
			objSurAttr = defTypeElt.Element("objsur");
			Assert.IsNotNull(objSurAttr);
			Assert.AreEqual("3942addb-99fd-43e9-ab7d-99025ceb0d4e", objSurAttr.FirstAttribute.Value);

			// Make sure new default types are added in possiblities

			var possibilityObjs = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("CmPossibilityList").First(
											e => e.Guid.ToString() == "bb372467-5230-43ef-9cc7-4d40b053fb94").Xml);

			var nameElt = possibilityObjs.Element("Name");
			Assert.IsNotNull(nameElt);
			var objAUniAttr = nameElt.Element("AUni");
			Assert.IsNotNull(objAUniAttr);
			Assert.AreEqual("Variant Types", objAUniAttr.Value);

			var possElt = possibilityObjs.Element("Possibilities");
			Assert.IsNotNull(possElt);
			var objSurInPossAttr = possElt.Descendants("objsur").ToList();
			Assert.AreEqual(2, objSurInPossAttr.Count);
			var uniString1 = objSurInPossAttr.First(e => e.Attribute("guid").Value == "3942addb-99fd-43e9-ab7d-99025ceb0d4e");
			Assert.IsNotNull(uniString1);

			possibilityObjs = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("CmPossibilityList").First(
											e => e.Guid.ToString() == "1ee09905-63dd-4c7a-a9bd-1d496743ccd6").Xml);

			nameElt = possibilityObjs.Element("Name");
			Assert.IsNotNull(nameElt);
			objAUniAttr = nameElt.Element("AUni");
			Assert.IsNotNull(objAUniAttr);
			Assert.AreEqual("Complex Form Types", objAUniAttr.Value);

			possElt = possibilityObjs.Element("Possibilities");
			Assert.IsNotNull(possElt);
			objSurInPossAttr = possElt.Descendants("objsur").ToList();
			Assert.AreEqual(2, objSurInPossAttr.Count);
			uniString1 = objSurInPossAttr.First(e => e.Attribute("guid").Value == "fec038ed-6a8c-4fa5-bc96-a4f515a98c50");
			Assert.IsNotNull(uniString1);

			// Make sure new default types are added in LexEntryType

			var lexEntryObjs = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("LexEntryType").First(
											e => e.Guid.ToString() == "3942addb-99fd-43e9-ab7d-99025ceb0d4e").Xml);

			nameElt = lexEntryObjs.Element("Abbreviation");
			Assert.IsNotNull(nameElt);
			objAUniAttr = nameElt.Element("AUni");
			Assert.IsNotNull(objAUniAttr);
			Assert.AreEqual("unspec. var. of", objAUniAttr.Value);

			nameElt = lexEntryObjs.Element("Name");
			Assert.IsNotNull(nameElt);
			objAUniAttr = nameElt.Element("AUni");
			Assert.IsNotNull(objAUniAttr);
			Assert.AreEqual("<Unspecified Variant>", objAUniAttr.Value);

			lexEntryObjs = XElement.Parse(dtoRepos.AllInstancesWithSubclasses("LexEntryType").First(
											e => e.Guid.ToString() == "fec038ed-6a8c-4fa5-bc96-a4f515a98c50").Xml);

			nameElt = lexEntryObjs.Element("Abbreviation");
			Assert.IsNotNull(nameElt);
			objAUniAttr = nameElt.Element("AUni");
			Assert.IsNotNull(objAUniAttr);
			Assert.AreEqual("unspec. comp. form of", objAUniAttr.Value);

			nameElt = lexEntryObjs.Element("Name");
			Assert.IsNotNull(nameElt);
			objAUniAttr = nameElt.Element("AUni");
			Assert.IsNotNull(objAUniAttr);
			Assert.AreEqual("<Unspecified Complex Form>", objAUniAttr.Value);
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
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexEntry", "LexSense", "LexPronunciation", "CmPicture", "CmPossibilityList" });
			mockMdc.AddClass(2, "LexEntry", "CmObject", new List<string>());
			mockMdc.AddClass(3, "LexSense", "CmObject", new List<string>());
			mockMdc.AddClass(4, "LexPronunciation", "CmObject", new List<string>());
			mockMdc.AddClass(5, "CmPicture", "CmObject", new List<string>());
			mockMdc.AddClass(6, "CmPossibilityList", "CmObject", new List<string>());

			var currentFlid = 2000;
			mockMdc.AddField(++currentFlid, "Senses", CellarPropertyType.OwningSequence, 2);
			mockMdc.AddField(++currentFlid, "Pronunciations", CellarPropertyType.OwningSequence, 2);
			mockMdc.AddField(++currentFlid, "Pictures", CellarPropertyType.OwningSequence, 3);
			mockMdc.AddField(++currentFlid, "Form", CellarPropertyType.MultiString, 4);
			mockMdc.AddField(++currentFlid, "Caption", CellarPropertyType.MultiUnicode, 5);

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
		/// Test the migration from version 7000068 to 7000069 adding 3 fields to LexEtymology
		/// and changing LexEntry->Etymology from atomic to sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddLexEtymologyFieldsMakeSequence()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexEntry", "LexEtymology", "LangProject" });
			mockMdc.AddClass(2, "LexEntry", "CmObject", new List<string>());
			mockMdc.AddClass(3, "LexEtymology", "CmObject", new List<string>());
			mockMdc.AddClass(4, "LangProject", "CmObject", new List<string>());

			var currentFlid = 2000;
			// These represent the pre-migration state
			mockMdc.AddField(++currentFlid, "Etymology", CellarPropertyType.OwningAtomic, 2);

			mockMdc.AddField(++currentFlid, "Comment", CellarPropertyType.MultiString, 3);
			mockMdc.AddField(++currentFlid, "Form", CellarPropertyType.MultiUnicode, 3);
			mockMdc.AddField(++currentFlid, "Gloss", CellarPropertyType.MultiUnicode, 3);
			mockMdc.AddField(++currentFlid, "Source", CellarPropertyType.Unicode, 3);
			mockMdc.AddField(++currentFlid, "LiftResidue", CellarPropertyType.Unicode, 3);

			mockMdc.AddField(++currentFlid, "CurAnalysisWss", CellarPropertyType.Unicode, 4);
			mockMdc.AddField(++currentFlid, "CurVernWss", CellarPropertyType.Unicode, 4);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069_Etymology.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			// Old model
			// LexEntry.Etymology atomic						(-> sequence)
			//<basic num="1" id="Comment" sig="MultiString"/>	(no change)
			//<basic num="2" id="Form" sig="MultiUnicode"/>		(-> MultiString)
			//<basic num="3" id="Gloss" sig="MultiUnicode"/>	(-> MultiString)
			//<basic num="4" id="Source" sig="Unicode"/>		(-> "Language" MultiString)
			//<basic num="5" id="LiftResidue" sig="Unicode"/>	(no change)

			// New model
			//<basic num="1" id="Comment" sig="MultiString"/>
			//<basic num="2" id="Form" sig="MultiString"/>
			//<basic num="3" id="Gloss" sig="MultiString"/>
			//<basic num="4" id="Language" sig="MultiString"/>
			//<basic num="5" id="LiftResidue" sig="Unicode"/>
			//<basic num="6" id="PrecComment" sig="MultiString"/>	(new)
			//<basic num="7" id="Note" sig="MultiString"/>			(new)
			//<basic num="8" id="Bibliography" sig="MultiString"/>	(new)

			// Make sure LexEtymology objects do not have Language, PrecComment, Note or Bibliography prior to migration.
			foreach (var dto in dtoRepos.AllInstancesSansSubclasses("LexEtymology"))
			{
				var elt = XElement.Parse(dto.Xml);
				Assert.IsNull(elt.Element("Language"));
				Assert.IsNull(elt.Element("PrecComment"));
				Assert.IsNull(elt.Element("Note"));
				Assert.IsNull(elt.Element("Bibliography"));
			}

			DataMigration7000069.AugmentEtymologyCluster(dtoRepos);

			// Since we're just adding two fields that will be empty initially,
			// we just need to verify that nothing in our data changed.
			const string frPhoneticWs = "fr-fonipa";
			const string idWs = "id";
			const string entry1Guid = "7ecbb299-bf35-4795-a5cc-8d38ce8b891c";
			const string entry2Guid = "7ecbb299-bf35-4795-a5cc-8d38ce8b891e";
			const string emptyEntryGuid = "7ecbb299-bf35-4795-a5cc-8d38ce8b891f";

			// Verification
			var primaryAnalysisWs = enWs;
			var allEtymologyDtos = dtoRepos.AllInstancesSansSubclasses("LexEtymology");
			Assert.AreEqual(2, allEtymologyDtos.Count(), "There should be two Etymology objects");
			var firstEntry = XElement.Parse(dtoRepos.GetDTO(entry1Guid).Xml);
			var secondEntry = XElement.Parse(dtoRepos.GetDTO(entry2Guid).Xml);
			var emptyEntry = XElement.Parse(dtoRepos.GetDTO(emptyEntryGuid).Xml);
			var entryEtymElt = emptyEntry.Element("Etymology");
			Assert.IsNull(entryEtymElt, "Empty entry should not have an Etymology object");
			var firstEtymElt = GetEntryEtymologyElement(dtoRepos, firstEntry);
			var secondEtymElt = GetEntryEtymologyElement(dtoRepos, secondEntry);

			// Verify contents of firstEtymElt
			var comment = firstEtymElt.Element("Comment");
			VerifyMultiString(comment, new[] { enWs }, new[] { "Odd comment." }, false);
			var form = firstEtymElt.Element("Form");
			VerifyMultiString(form, new[] { frWs }, new[] { "alcool" }, false);
			var gloss = firstEtymElt.Element("Gloss");
			VerifyMultiString(gloss, new[] { enWs }, new[] { "alcohol" }, true);
			VerifyMultiString(gloss, new[] { idWs }, new[] { "minuman keras" }, true);
			var language = firstEtymElt.Element("Language");
			VerifyMultiString(language, new []{primaryAnalysisWs}, new []{"l'arabe"}, false);
			var source = firstEtymElt.Elements("Source");
			CollectionAssert.IsEmpty(source, "Should not be any Etymology Source left.");
			var liftRes = firstEtymElt.Elements("LiftResidue");
			CollectionAssert.IsEmpty(liftRes, "Should not create any LiftResidue at this point.");

			// Verify contents of secondEtymElt
			comment = secondEtymElt.Element("Comment");
			VerifyMultiString(comment, new[] { enWs, frWs, enWs }, new[] { "some comment", " avec ", "embedded French" }, true);
			VerifyMultiString(comment, new[] { idWs }, new[] { "Indonesian comment" }, true);
			form = secondEtymElt.Element("Form");
			VerifyMultiString(form, new[] { frWs }, new[] { "coleus" }, true);
			VerifyMultiString(form, new[] { frPhoneticWs }, new[] { "kolius" }, true);
			VerifyMultiString(form, new[] { enWs }, new[] { "koleus" }, true);
			VerifyMultiString(form, new[] { idWs }, new[] { "kolele" }, true);
			gloss = secondEtymElt.Element("Gloss");
			VerifyMultiString(gloss, new[] { enWs }, new[] { "coleus flower" }, true);
			VerifyMultiString(gloss, new[] { idWs }, new[] { "indonesian gloss" }, true);
			language = secondEtymElt.Element("Language");
			VerifyMultiString(language, new[] { primaryAnalysisWs }, new[] { "All made up" }, false);
			source = secondEtymElt.Elements("Source");
			CollectionAssert.IsEmpty(source, "Should not be any Etymology Source left.");
			liftRes = secondEtymElt.Elements("LiftResidue");
			CollectionAssert.IsEmpty(liftRes, "Should not create any LiftResidue at this point.");
		}

		/// <summary>
		/// Verify that elt contains at least one AStr element containing as many Run elements
		/// as the size of wsArray, each having ws attribute set to the corresponding
		/// value in wsArray and each containing the corresponding string in runContentArray.
		/// The AStr element is verified to have its ws attribute set to the first value in
		/// wsArray. If allowOtherAstr is false, the matching AStr element will be verified
		/// to be the only AStr in elt.
		/// </summary>
		private static void VerifyMultiString(XElement elt, string[] wsArray, string[] runContentArray, bool allowOtherAstr)
		{
			Assert.NotNull(elt, "Empty element fed to VerifyMultiString()");
			Assert.AreEqual(wsArray.Length, runContentArray.Length, "VerifyMultiString fed two arrays of different length");

			var astrElts = elt.Elements("AStr").ToList();
			if (!allowOtherAstr)
				Assert.AreEqual(1, astrElts.Count, "Did not find unique AStr element in {0}", elt.Name);
			var astrElt = astrElts.FirstOrDefault(elem => elem.Attribute("ws").Value == wsArray[0]);
			Assert.NotNull(astrElt, "AStr element has wrong ws attribute value in {0}.", elt.Name);

			var runElts = astrElt.Elements("Run").ToList();
			Assert.AreEqual(wsArray.Length, runElts.Count, "MultiString {0} has the wrong number of Run elements", elt.Name);
			var i = 0;
			foreach (var runElt in runElts)
			{
				Assert.That(runElt.Attribute("ws").Value, Is.EqualTo(wsArray[i]), "Run element has wrong ws attribute value.");
				Assert.That(runElt.Value, Is.EqualTo(runContentArray[i]), "Run element contains the wrong data.");
				i++;
			}
		}

		private static XElement GetEntryEtymologyElement(IDomainObjectDTORepository dtoRepos, XElement entryElement)
		{
			var entryEtymElt = entryElement.Element("Etymology");
			var etyPointers = entryEtymElt.Descendants("objsur").ToList();
			Assert.AreEqual(1, etyPointers.Count, "There should be one Etymology object");
			var etyGuid = etyPointers.First().Attribute("guid").Value;
			return XElement.Parse(dtoRepos.GetDTO(etyGuid).Xml);
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

			// Make sure LexSense does not have UsageNote prior to migration.
			var lexSenseDtos = dtoRepos.AllInstancesSansSubclasses("LexSense").ToList();
			foreach (var dto in lexSenseDtos)
			{
				var elt = XElement.Parse(dto.Xml);
				Assert.IsNull(elt.Element("UsageNote"));
			}

			var firstSense = XElement.Parse(lexSenseDtos[0].Xml);

			var customElt = firstSense.Element("Custom");
			var nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("UsageNote", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			var secondSense = XElement.Parse(lexSenseDtos[1].Xml);

			customElt = secondSense.Element("Custom");
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("UsageNote1", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			var thirdSense = XElement.Parse(lexSenseDtos[2].Xml);

			customElt = thirdSense.Element("Custom");
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("UsageNote", nameAttr.Value);
			Assert.AreEqual(2, customElt.Descendants("AUni").Count());

			var fourthSense = XElement.Parse(lexSenseDtos[3].Xml);

			customElt = fourthSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("Test Note", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			var fifthSense = XElement.Parse(lexSenseDtos[4].Xml);

			customElt = fifthSense.Element("Custom");

			nameAttr = customElt.Attributes("name").FirstOrDefault();
			Assert.IsNotNull(nameAttr);
			Assert.AreEqual("UsageNote", nameAttr.Value);
			var valAttr = customElt.Attributes("val").FirstOrDefault();
			Assert.IsNotNull(valAttr);
			Assert.AreEqual("42", valAttr.Value);

			DataMigration7000069.MigrateIntoNewMultistringField(dtoRepos, "UsageNote"); // SUT

			firstSense = XElement.Parse(lexSenseDtos[0].Xml);

			customElt = firstSense.Element("Custom");
			Assert.IsNull(customElt);

			customElt = firstSense.Element("UsageNote");

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			var multiStrElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			var runElts = multiStrElements.Descendants("Run").ToList();
			Assert.AreEqual(1, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Usage Note using AStr in English", runElts[0].Value);

			secondSense = XElement.Parse(lexSenseDtos[1].Xml);

			customElt = secondSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.FirstAttribute;
			Assert.AreEqual("UsageNote1", nameAttr.Value);
			Assert.AreEqual(1, customElt.Descendants("AStr").Count());

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			multiStrElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			runElts = multiStrElements[0].Descendants("Run").ToList();
			Assert.AreEqual(1, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Usage Note using AStr in English", runElts[0].Value);

			Assert.IsNull(secondSense.Element("UsageNote"));

			thirdSense = XElement.Parse(lexSenseDtos[2].Xml);

			customElt = thirdSense.Element("Custom");
			Assert.IsNull(customElt);

			var usagenoteElt = thirdSense.Element("UsageNote");

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

			customElt = fourthSense.Element("Custom");
			Assert.IsNotNull(customElt);
			nameAttr = customElt.Attributes("name").FirstOrDefault();
			Assert.IsNotNull(nameAttr);
			Assert.AreEqual("Test Note", nameAttr.Value);

			CollectionAssert.IsEmpty(customElt.Descendants("AUni"));
			multiStrElements = customElt.Descendants("AStr").ToList();
			Assert.AreEqual(1, multiStrElements.Count);
			runElts = multiStrElements[0].Descendants("Run").ToList();
			Assert.AreEqual(1, runElts.Count);
			Assert.AreEqual(enWs, runElts[0].FirstAttribute.Value);
			Assert.AreEqual("Custom Test Note using AStr in English", runElts[0].Value);

			Assert.IsNull(fourthSense.Element("UsageNote"));

			fifthSense = XElement.Parse(lexSenseDtos[4].Xml);

			customElt = fifthSense.Element("Custom");
			Assert.IsNotNull(customElt);

			nameAttr = customElt.Attribute("name");
			Assert.IsNotNull(nameAttr);
			Assert.AreEqual("UsageNote0", nameAttr.Value, "conflicting Custom Field should be renamed with 'UsageNote0' in this case");
			valAttr = customElt.Attributes("val").FirstOrDefault();
			Assert.IsNotNull(valAttr);
			Assert.AreEqual("42", valAttr.Value);

			Assert.IsNull(fifthSense.Element("UsageNote"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000068 to 7000069 to Create ExtendedNote field,
		/// which is a owning sequence of LexExtendedNote, a new class.
		/// Also tests the addition of a new CmPossibilityList attached to LexDb with five
		/// shipping defaults.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAddingExtendedNoteToLexSense()
		{
			const string extNoteListGuid = "ed6b2dcc-e82f-4631-b61a-6b630de332d0";
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "LexSense", "LexDb", "LexExampleSentence", "CmPossibilityList" });
			mockMdc.AddClass(2, "CmPossibilityList", "CmObject", new List<string>());
			mockMdc.AddClass(3, "LexDb", "CmObject", new List<string>());
			mockMdc.AddClass(4, "LexSense", "CmObject", new List<string>());
			mockMdc.AddClass(5, "LexExampleSentence", "CmObject", new List<string>());

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069_ExtendedNote.xml");
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, null, FwDirectoryFinder.FdoDirectories);

			// Make sure LexSense does not have ExtendedNotes prior to migration.
			var lexSenseDtos = dtoRepos.AllInstancesSansSubclasses("LexSense").ToList();
			foreach (var elt in lexSenseDtos.Select(dto => XElement.Parse(dto.Xml)))
			{
				Assert.IsNull(elt.Element("ExtendedNote"));
			}

			// Make sure LexDb does not have ExtendedNoteTypes prior to migration.
			var lexDbDtos = dtoRepos.AllInstancesSansSubclasses("LexDb").ToList();
			foreach (var elt in lexDbDtos.Select(dto => XElement.Parse(dto.Xml)))
			{
				Assert.IsNull(elt.Element("ExtendedNoteTypes"));
			}

			//SUT
			DataMigration7000069.AddNewExtendedNoteCluster(dtoRepos);

			var firstSense = XElement.Parse(lexSenseDtos.First().Xml);

			var noteElt = firstSense.Element("ExtendedNote");
			Assert.IsNull(noteElt, "ExtendedNote property should still be null");

			var lexDb = XElement.Parse(lexDbDtos.First().Xml);
			var extNoteTypElt = lexDb.Element("ExtendedNoteTypes");
			Assert.IsNotNull(extNoteTypElt, "There should be a possibility list for ExtendedNoteTypes");

			var guids = GetOwnedGuidStringsFromPropertyElement(extNoteTypElt).ToList();
			Assert.AreEqual(1, guids.Count, "Found too many or too few possibility lists.");
			var possListElt = XElement.Parse(dtoRepos.GetDTO(guids[0]).Xml);
			Assert.AreEqual(lexDb.Attribute("guid").Value, possListElt.Attribute("ownerguid").Value,
				"Reverse link from list to LexDb not set correctly");
			Assert.AreEqual(extNoteListGuid, possListElt.Attribute("guid").Value,
				"ExtendedNoteTypes possibility list has the wrong guid.");

			// Should look something like this:
			//<rt class="CmPossibilityList" guid="ed6b2dcc-e82f-4631-b61a-6b630de332d0" ownerguid="66b37dff-779c-4d81-b359-f5878b5c69f0">
			//  <Name>
			//    <AUni ws="en">Extended Note Types</AUni>
			//  </Name>
			//  <Abbreviation>
			//    <AUni ws="en">ExtNoteTyp</AUni>
			//  </Abbreviation>
			//  <Depth val="1"/>
			//  <IsSorted val="True"/>
			//  <ItemClsid val="7"/>
			//  <Possibilities>
			//    <objsur guid="2f06d436-b1e0-47ae-a42e-1f7b893c5fc2" t="o" />
			//    <objsur guid="7ad06e7d-15d1-42b0-ae19-9c05b7c0b181" t="o" />
			//    <objsur guid="d3d28628-60c9-4917-8185-ba64c59f20c3" t="o" />
			//    <objsur guid="30115b33-608a-4506-9f9c-2457cab4f4a8" t="o" />
			//    <objsur guid="5dd29371-fdb0-497a-a2fb-7ca69b00ad4f" t="o" />
			//  </Possibilities>
			//  <PreventDuplicates val="True"/>
			//  <WsSelector val="-3" />
			//</rt>

			var name = possListElt.Element("Name").Element("AUni");
			Assert.AreEqual(enWs, name.Attribute("ws").Value, "ws attribute not set on Name element");
			Assert.AreEqual("Extended Note Types", name.Value);
			var abbr = possListElt.Element("Abbreviation").Element("AUni");
			Assert.AreEqual(enWs, name.Attribute("ws").Value, "ws attribute not set on Abbreviation element");
			Assert.AreEqual("ExtNoteTyp", abbr.Value);
			Assert.AreEqual("1", possListElt.Element("Depth").Attribute("val").Value);
			Assert.AreEqual("True", possListElt.Element("IsSorted").Attribute("val").Value);
			Assert.AreEqual("7", possListElt.Element("ItemClsid").Attribute("val").Value);
			Assert.AreEqual("True", possListElt.Element("PreventDuplicates").Attribute("val").Value);
			Assert.AreEqual("-3", possListElt.Element("WsSelector").Attribute("val").Value);
			var possibilitiesElt = possListElt.Element("Possibilities");
			Assert.AreEqual(4, possibilitiesElt.Elements("objsur").Count());
			VerifyExtNotePossibility(possibilitiesElt, dtoRepos, "2f06d436-b1e0-47ae-a42e-1f7b893c5fc2", "Collocation", "Coll.");
			VerifyExtNotePossibility(possibilitiesElt, dtoRepos, "7ad06e7d-15d1-42b0-ae19-9c05b7c0b181", "Cultural", "Cult.");
			VerifyExtNotePossibility(possibilitiesElt, dtoRepos, "30115b33-608a-4506-9f9c-2457cab4f4a8", "Grammar", "Gram.");
			VerifyExtNotePossibility(possibilitiesElt, dtoRepos, "d3d28628-60c9-4917-8185-ba64c59f20c4", "Inflectional", "Infl.");
		}

		private static void VerifyExtNotePossibility(XElement possibilitiesElt, IDomainObjectDTORepository dtoRepos, string guidStr,
			string name, string abbreviation)
		{
			var guids = GetOwnedGuidStringsFromPropertyElement(possibilitiesElt);
			CollectionAssert.Contains(guids, guidStr, "Didn't find guid in list of pointers in CmPossibilityList");
			var possElt = XElement.Parse(dtoRepos.GetDTO(guidStr).Xml);
			var nameStrElt = possElt.Element("Name");
			var abbrStrElt = possElt.Element("Abbreviation");
			VerifySingleMultiUnicodeStringFromPropertyElement(nameStrElt, name);
			VerifySingleMultiUnicodeStringFromPropertyElement(abbrStrElt, abbreviation);
			var protElt = possElt.Element("IsProtected");
			Assert.IsNotNull(protElt);
			Assert.AreEqual("True", protElt.Attribute("val").Value, "IsProtected property not correctly set for {0}.", name);
		}

		private static IEnumerable<string> GetOwnedGuidStringsFromPropertyElement(XElement propElement)
		{
			return propElement.Elements("objsur").Select(objPointer => objPointer.Attribute("guid").Value).ToList();
		}

		private static void VerifySingleMultiUnicodeStringFromPropertyElement(XElement propElement, string value)
		{
			Assert.IsNotNull(propElement, "MultiUnicode property {0} should not be null.", value);
			var aUniStr = propElement.Elements("AUni").ToList();
			Assert.AreEqual(1, aUniStr.Count, "Wrong number of AUni elements in MultiUnicode property.");
			Assert.AreEqual(enWs, aUniStr[0].Attribute("ws").Value, "Unicode string ws attribute is wrong.");
			Assert.AreEqual(value, aUniStr[0].Value, "Unicode string has wrong value.");
		}
	}
}
