// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000009 to 7000010.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000010 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test makes sure the relevant annotation defns are deleted,
		/// but no others.
		///
		/// The full listing is:
		///
		/// EA346C01-022F-4F34-B938-219CE7B65B73 (list) (not universal in all DBs)
		/// 	7FFC4EAB-856A-43CC-BC11-0DB55738C15B (Nt)-Del (But, keep it for now, since it owns 'keepers'.)
		/// 		56DE9B1A-1CE7-42A1-AA76-512EBEFF0DDA (Consultant Nt)
		/// 		80AE5729-9CD8-424D-8E71-96C1A8FD5821 (Translator Nt)
		/// 	8D4CBD80-0DCA-4A83-8A1F-9DB3AA4CFF54 (Text)
		/// 		B63F0702-32F7-4ABB-B005-C1D2265636AD (Text Segment)-Del
		/// 		9AC9637A-56B9-4F05-A0E1-4243FBFB57DB (Free Translation)-Del
		/// 		B0B1BB21-724D-470A-BE94-3D9A436008B8 (Literal Translation)-Del
		/// 		20CF6C1C-9389-4380-91F5-DFA057003D51 (Process Time)
		/// 		EB92E50F-BA96-4D1D-B632-057B5C274132 (Wordform In Context)-Del
		/// 		CFECB1FE-037A-452D-A35B-59E06D15F4DF (Punctuation In Context)-Del
		/// 		084A3AFE-0D00-41DA-BFCF-5D8DEAFA0296 (Text Tag)-Del
		/// 	F094A0B0-01B8-4621-97F1-4D775BC29CE7 (Comment)
		/// 	82E2FD92-48D8-43C9-BA84-CC4A2A5BEEAD (Errors)
		/// 		BABCB400-F274-4498-92C5-77E99C90F75C ((TE-6917): Merge this with the other capitialization check)
		///			DCC8D4D2-13B2-46E4-8FB3-29C166D189EA (Punctuation)
		/// 		72ABB400-F274-4498-92C5-77E99C90F75B (Repeated Words)
		///			DDCCB400-F274-4498-92C5-77E99C90F75B (Matched Pairs)
		///			BABCB400-F274-4498-92C5-77E99C90F75B (Capitalization)
		///			F17A054B-D21E-4298-A1A5-0D79C4AF6F0F (Chapter and Verse Numbers)
		/// 		6558A579-B9C4-4EFD-8728-F994D0561293 (Characters)
		/// 		DDCCB400-F274-4498-92C5-77E99C90F75C (Quotations)
		/// 		BABCB400-F274-4498-92C5-77E99C90F75D (Mixed Capitalization)
		/// 	A39A1272-38A0-4354-BDAC-8636D64C1EEC (Discourse)-Del (including two sub-points)
		/// 		50C1A53D-925D-4F55-8ED7-64A297905346 (Constituent Chart Row)
		/// 		EC0A4DAD-7E90-4E73-901A-21D25F0692E3 (Constituent Chart Annotation)
		///
		/// The ones slated for destruction are:
		///
		/// 	B63F0702-32F7-4ABB-B005-C1D2265636AD (Text Segment)-Del
		/// 	9AC9637A-56B9-4F05-A0E1-4243FBFB57DB (Free Translation)-Del
		/// 	B0B1BB21-724D-470A-BE94-3D9A436008B8 (Literal Translation)-Del
		/// 	EB92E50F-BA96-4D1D-B632-057B5C274132 (Wordform In Context)-Del
		/// 	CFECB1FE-037A-452D-A35B-59E06D15F4DF (Punctuation In Context)-Del
		/// 	084A3AFE-0D00-41DA-BFCF-5D8DEAFA0296 (Text Tag)-Del
		/// 	A39A1272-38A0-4354-BDAC-8636D64C1EEC (Discourse)-Del (including two sub-points)
		/// 	50C1A53D-925D-4F55-8ED7-64A297905346 (Constituent Chart Row)
		/// 	EC0A4DAD-7E90-4E73-901A-21D25F0692E3 (Constituent Chart Annotation)
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_AnnotationDefns_Removed_Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000010_AnnotationDefns.xml");
			dtos.UnionWith(DataMigrationTestServices.ParseProjectFile("DataMigration7000010_CommonData.xml"));

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "CmPossibilityList", "CmAnnotationDefn", "StText", "StTxtPara" });
			mockMDC.AddClass(2, "CmPossibilityList", "CmObject", new List<string>());
			mockMDC.AddClass(3, "CmAnnotationDefn", "CmObject", new List<string>());
			mockMDC.AddClass(4, "StText", "CmObject", new List<string>());
			mockMDC.AddClass(5, "StTxtPara", "CmObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000009, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			// Collect the various annotation defns.
			var annDefnDtos = new Dictionary<string, DomainObjectDTO>();
			foreach (var annDto in dtoRepos.AllInstancesSansSubclasses("CmAnnotationDefn"))
				annDefnDtos.Add(annDto.Guid.ToUpper(), annDto);
			var stTextDtos = new Dictionary<string, DomainObjectDTO>();
			foreach (var annDto in dtoRepos.AllInstancesSansSubclasses("StText"))
				stTextDtos.Add(annDto.Guid.ToUpper(), annDto);
			var stTxtParaDtos = new Dictionary<string, DomainObjectDTO>();
			foreach (var annDto in dtoRepos.AllInstancesSansSubclasses("StTxtPara"))
				stTxtParaDtos.Add(annDto.Guid.ToUpper(), annDto);

			m_dataMigrationManager.PerformMigration(dtoRepos, 7000010, new DummyProgressDlg());

			Assert.AreEqual(16, dtoRepos.AllInstancesSansSubclasses("CmAnnotationDefn").Count(), "Wrong number of AnnDefns remaining.");
			Assert.AreEqual(1, dtoRepos.AllInstancesSansSubclasses("StText").Count(), "Wrong number of StTexts remaining.");
			Assert.AreEqual(1, dtoRepos.AllInstancesSansSubclasses("StTxtPara").Count(), "Wrong number of StTxtParas remaining.");

			// Make sure correct annDefns were removed.
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["B63F0702-32F7-4ABB-B005-C1D2265636AD"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["9AC9637A-56B9-4F05-A0E1-4243FBFB57DB"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["B0B1BB21-724D-470A-BE94-3D9A436008B8"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["EB92E50F-BA96-4D1D-B632-057B5C274132"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["CFECB1FE-037A-452D-A35B-59E06D15F4DF"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["084A3AFE-0D00-41DA-BFCF-5D8DEAFA0296"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["A39A1272-38A0-4354-BDAC-8636D64C1EEC"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["50C1A53D-925D-4F55-8ED7-64A297905346"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, annDefnDtos["EC0A4DAD-7E90-4E73-901A-21D25F0692E3"]);
			// Make sure owned StText and StTxtPara in deleted annDefn were removed.
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, stTextDtos["70CA84D7-211C-4548-8274-004640B3CA5D"]);
			DataMigrationTestServices.CheckDtoRemoved(dtoRepos, stTxtParaDtos["7C02332C-1F8D-4208-B15D-CA8AA89B9324"]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over the basic Segment migration,
		/// without worrying about discourse/syntax part of the migration.
		///
		/// This test will include the free/back trans migration, notes migration,
		/// and make sure a segment is created, if a para has xfics (in this case one twfic),
		/// but no extant segment annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_BasicSegmentMigration_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> {"DataMigration7000010_BasicSegments.xml"},
				2);

			// Make sure LP has no more owned anns.
			var lpDto = dtoRepos.GetDTO("9719A466-2240-4DEA-9722-9FE0746A30A6");
			Assert.IsNotNull(lpDto, "Missing LP dto.");
			var lpElement = XElement.Parse(lpDto.Xml);
			Assert.IsNull(lpElement.Element("LangProject").Element("Annotations"), "Still has Annotations.");

			// Make sure the one new PunctuationForm was created.
			var punctFormDtos = dtoRepos.AllInstancesSansSubclasses("PunctuationForm").ToList();
			Assert.AreEqual(1, punctFormDtos.Count, "Wrong number of new PunctuationForm objects.");
			var punctFormDto = punctFormDtos[0];
			var punctFormElement = XElement.Parse(punctFormDto.Xml);
			var run = punctFormElement.Descendants("Run").First();
			Assert.AreEqual("en", run.Attribute("ws").Value, "Wrong IcuLocale.");
			Assert.AreEqual(".", run.Value, "Wrong punctuation form.");

			var newNotesDtos = new List<DomainObjectDTO>(dtoRepos.AllInstancesSansSubclasses("Note"));
			Assert.AreEqual(2, newNotesDtos.Count, "Wrong number of new Notes.");
			var noteAltValues = new List<string> { "Note1.", "Note2.", "Nota1.", "Nota2." };
			var noteGuids = new List<string>(2);
			foreach (var noteDto in newNotesDtos)
			{
				noteGuids.Add(noteDto.Guid);
				var noteRtElement = XElement.Parse(noteDto.Xml);
				var contentNodes = noteRtElement.Element("Note").Elements("Content").Elements("AStr");
				Assert.AreEqual(2, contentNodes.Count(), "Wrong number of Nt content alternatives.");
				foreach (var alt in contentNodes)
					Assert.Contains(alt.Value, noteAltValues);
			}

			// Check good para (60BF79A0-9579-4486-A32F-944490F0C024).
			List<XElement> newSegmentObjSurElements;
			var currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-4486-A32F-944490F0C024", 2, false, false, out newSegmentObjSurElements);
			//Check first new segment.
			// This list provides the Analyses in the expected order.
			var analysesGuids = new List<string>(3)
									{
										"068DC680-CD40-4C47-BBE6-00E572EE2602",
										"068DC680-CD40-4C47-BBE7-00E572EE2602",
										punctFormDto.Guid
									};
			DomainObjectDTO newSegmentDto;
			var newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", true, 2);
			CheckComment(newSegmentInnerElement, "FreeTranslation", true, 2);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, true, 2);
			CheckXfics(dtoRepos, newSegmentInnerElement, 3, analysesGuids); // 2 twfics and 1 pfic.
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);
			//Check second new segment.
			analysesGuids.Clear();
			// This list provides the Analyses in the expected order.
			analysesGuids.Add("068DC680-CD40-4C47-BBE6-00E572EE2602");
			analysesGuids.Add(punctFormDto.Guid);
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 27, currentParaDto, newSegmentObjSurElements[1], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 2, analysesGuids); // 1 twfic and 1 pfic.
			Assert.AreEqual("27", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test checks that segments and their segment-level annotations are migrated
		/// even if they have no xfic-level annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_SegmentsNoXfics_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> { "DataMigration7000010_SegmentsNoXfics.xml" },
				2);

			// Make sure LP has no more owned anns.
			var lpDto = dtoRepos.GetDTO("9719A466-2240-4DEA-9722-9FE0746A30A6");
			Assert.IsNotNull(lpDto, "Missing LP dto.");
			var lpElement = XElement.Parse(lpDto.Xml);
			Assert.IsNull(lpElement.Element("LangProject").Element("Annotations"), "Still has Annotations.");

			var newNotesDtos = new List<DomainObjectDTO>(dtoRepos.AllInstancesSansSubclasses("Note"));
			Assert.AreEqual(2, newNotesDtos.Count, "Wrong number of new Notes.");
			var noteAltValues = new List<string> { "Note1.", "Note2.", "Nota1.", "Nota2." };
			var noteGuids = new List<string>(2);
			foreach (var noteDto in newNotesDtos)
			{
				noteGuids.Add(noteDto.Guid);
				var noteRtElement = XElement.Parse(noteDto.Xml);
				var contentNodes = noteRtElement.Element("Note").Elements("Content").Elements("AStr");
				Assert.AreEqual(2, contentNodes.Count(), "Wrong number of Nt content alternatives.");
				foreach (var alt in contentNodes)
					Assert.Contains(alt.Value, noteAltValues);
			}

			// Check good para (60BF79A0-9579-4486-A32F-944490F0C024).
			List<XElement> newSegmentObjSurElements;
			var currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-4486-A32F-944490F0C024", 2, false, false, out newSegmentObjSurElements);
			//Check first new segment.
			// This list provides the Analyses in the expected order.
			var analysesGuids = new List<string>()
									{
									};
			DomainObjectDTO newSegmentDto;
			var newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", true, 2);
			CheckComment(newSegmentInnerElement, "FreeTranslation", true, 2);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, true, 2);
			CheckXfics(dtoRepos, newSegmentInnerElement, 0, analysesGuids); // no xfics.
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);
			//Check second new segment.
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 27, currentParaDto, newSegmentObjSurElements[1], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 0, analysesGuids); // no analysis
			Assert.AreEqual("27", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over the Text Tag migration,
		/// without worrying about the discourse part of the migration.
		///
		/// This test will include one paragraph with two segments,
		/// each of which has two text tag indirect annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_TextTagMigration_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> {"DataMigration7000010_TextTags.xml"},
				2);

			// Make sure the one new PunctuationForm was created.
			var punctFormDtos = dtoRepos.AllInstancesSansSubclasses("PunctuationForm").ToList();
			Assert.AreEqual(1, punctFormDtos.Count, "Wrong number of new PunctuationForm objects.");
			var punctFormDto = punctFormDtos[0];
			var punctFormElement = XElement.Parse(punctFormDto.Xml);
			var run = punctFormElement.Descendants("Run").First();
			Assert.AreEqual("en", run.Attribute("ws").Value, "Wrong IcuLocale.");
			Assert.AreEqual(".", run.Value, "Wrong punctuation form.");

			// Check para (c1ec5c4f-e382-11de-8a39-0800200c9a66).
			var noteGuids = new List<string>();
			List<XElement> newSegmentObjSurElements;
			var currentParaDto = CheckPara(dtoRepos, "c1ec5c4f-e382-11de-8a39-0800200c9a66", 2, false, false, out newSegmentObjSurElements);
			//Check first new segment.
			// This list provides the Analyses in the expected order.
			var analysesGuids = new List<string>(6)
									{
										"068DC680-CD40-4C47-BBE6-00E572EE2602",
										"068DC680-CD40-4C47-BBE6-00E572EE2602",
										"068DC680-CD40-4C47-BBE6-00E572EE2602",
										"068DC680-CD40-4C47-BBE6-00E572EE2602",
										"068DC680-CD40-4C47-BBE6-00E572EE2602",
										punctFormDto.Guid
									};
			DomainObjectDTO newSegmentDto;
			var newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 6, analysesGuids); // 5 twfics and 1 pfic.
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);
			//Check second new segment.
			analysesGuids.Clear();
			// This list provides the Analyses in the expected order.
			analysesGuids.Add("068DC680-CD40-4C47-BBE6-00E572EE2602");
			analysesGuids.Add("068DC680-CD40-4C47-BBE6-00E572EE2602");
			analysesGuids.Add("068DC680-CD40-4C47-BBE6-00E572EE2602");
			analysesGuids.Add("068DC680-CD40-4C47-BBE6-00E572EE2602");
			analysesGuids.Add("068DC680-CD40-4C47-BBE6-00E572EE2602");
			analysesGuids.Add(punctFormDto.Guid);
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 27, currentParaDto, newSegmentObjSurElements[1], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 6, analysesGuids); // 5 twfics and 1 pfic.
			Assert.AreEqual("27", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Check StText
			var stTextDto = dtoRepos.GetOwningDTO(currentParaDto);
			var stTextElement = XElement.Parse(stTextDto.Xml);
			// It should now own four new TextTag objects.
			var textTagElements = (from textTagElement in stTextElement.Element("StText").Element("Tags").Elements()
								  select textTagElement).ToList();
			Assert.AreEqual(3, textTagElements.Count, "Wrong number of TextTags.");
			foreach (var newTextTagDto in dtoRepos.AllInstancesSansSubclasses("TextTag"))
			{
				var newTextTagElement = XElement.Parse(newTextTagDto.Xml);
				var innerTextTagElement = newTextTagElement.Element("TextTag");

				// Check that BeginSegment and EndSegment are the same new Segment.
				var beginSegmentObjSurElement = innerTextTagElement.Element("BeginSegment").Element("objsur");
				Assert.AreEqual("r", beginSegmentObjSurElement.Attribute("t").Value, "Not a reference property.");
				var endSegmentObjSurElement = innerTextTagElement.Element("EndSegment").Element("objsur");
				Assert.AreEqual("r", endSegmentObjSurElement.Attribute("t").Value, "Not a reference property.");
				var beginSegmentGuid = beginSegmentObjSurElement.Attribute("guid").Value;
				var endSegmentGuid = endSegmentObjSurElement.Attribute("guid").Value;
				Assert.AreEqual(beginSegmentGuid, endSegmentGuid, "Begin and End segments are not the same.");

				// Check for proper CmPossibilty.
				var tagObjSurElement = innerTextTagElement.Element("Tag").Element("objsur");
				Assert.AreEqual("r", tagObjSurElement.Attribute("t").Value, "Not a reference property.");
				var tagGuid = tagObjSurElement.Attribute("guid").Value;

				// Get Begin and End indices.
				var beginIdx = int.Parse(innerTextTagElement.Element("BeginAnalysisIndex").Attribute("val").Value);
				var endIdx = int.Parse(innerTextTagElement.Element("EndAnalysisIndex").Attribute("val").Value);

				switch (tagGuid)
				{
					default:
						Assert.Fail("CmPossibility not recognized.");
						break;
					case "c1ec5c47-e382-11de-8a39-0800200c9a66": // XP2
						Assert.Fail("This defective TextTag should be eliminated.");
						break;
					case "c1ec5c43-e382-11de-8a39-0800200c9a66": // XP
						Assert.AreEqual(0, beginIdx, "Wrong BeginAnalysisIndex.");
						Assert.AreEqual(1, endIdx, "Wrong EndAnalysisIndex.");
						break;
					case "c1ec5c48-e382-11de-8a39-0800200c9a66": // YP2
					case "c1ec5c44-e382-11de-8a39-0800200c9a66": // YP
						Assert.AreEqual(2, beginIdx, "Wrong BeginAnalysisIndex.");
						Assert.AreEqual(4, endIdx, "Wrong EndAnalysisIndex.");
						break;
				}
			}

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over the pathological migration cases at the Segment level.
		/// It does not treat discourse or syntax cases.
		///
		/// This test will include:
		///		1. Make sure a segment is created, if a para has xfics (in this case one twfic),
		///			but no extant segment annotation.
		///		2. Make sure a twfic that comes at the end of a sentence (but is past the
		///			range EndOffset of the last Segment) is added to the last Segment.
		///		3. Make sure a segment is created, if a para has xfics and at least one twfic
		///			is in a section of the paragraph that has no segment.
		///
		/// Paragraphs with these problems will have their ParseIsCurrent set to 'False',
		/// so they will be retokenized to be put back in order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_PathologicalSegmentMigration_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> {"DataMigration7000010_PathologicalSegments.xml"},
				9);

			// ----------------------------------------------------------------
			// Check defective para (60BF79A0-9579-4486-A32F-944490F0C023)
			// This one had no segment.
			// Make sure the para is not current on tokenization (with true, false parms).
			var noteGuids = new List<string>();
			List<XElement> newSegmentObjSurElements;
			var currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-4486-A32F-944490F0C023", 1, true, false, out newSegmentObjSurElements);
			//Check new segment. (It only has one.)
			// This list provides the Analyses in the expected order.
			var analysesGuids = new List<string>(1)
									{
										"068DC680-CD40-4C47-BBE6-00E572EE2602"
									};
			DomainObjectDTO newSegmentDto;
			var newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// ----------------------------------------------------------------
			// Check defective para (60BF79A0-9579-5486-A32F-944490F0C023)
			// This one had a segment, but its twfic was way beyond the end offset of the segment.
			// Make sure the para is not current on tokenization (with true, false parms).
			currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-5486-A32F-944490F0C023", 1, true, false, out newSegmentObjSurElements);
			analysesGuids.Clear();
			// This list provides the Analyses in the expected order.
			analysesGuids.Add("068DC680-CD40-4C47-BBE7-00E572EE2602");
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Check defective para (c1ec5c45-e382-11de-8a39-0800200c9a66)
			// This one had a segment, but *no* xfics.
			// It replicated (and fixed) a problem found in TLP data.
			// Make sure the para is not current on tokenization (with true, false parms).
			CheckPara(dtoRepos, "c1ec5c45-e382-11de-8a39-0800200c9a66", 1, true, false, out newSegmentObjSurElements);
			// More defective data found in TLP.
			// Make sure pfic testing paras are reset to 'False'.
			CheckPara(dtoRepos, "c1ec8363-e382-11de-8a39-0800200c9a66", 1, true, false, out newSegmentObjSurElements);
			CheckPara(dtoRepos, "c1ec8366-e382-11de-8a39-0800200c9a66", 1, true, false, out newSegmentObjSurElements);
			CheckPara(dtoRepos, "c1ecaa61-e382-11de-8a39-0800200c9a66", 1, true, false, out newSegmentObjSurElements);

			// ----------------------------------------------------------------
			// Check pathological para (14f703d9-c849-4251-9b0f-09d964b6b69c)
			// This one has one segment and one wfic, but the segment begins with a space,
			// so the first wfic's offset and the segment's offset don't match.
			// The previous version of the migration code ended up with "ChangeME" as the
			// segment in this case when converting a CCA that referenced that wfic.
			var oneSegmentParaDto = CheckPara(dtoRepos, "14f703d9-c849-4251-9b0f-09d964b6b69c", 1, true, false, out newSegmentObjSurElements); // all paragraphs now get reparsed on load
			// This list provides the Analyses in the expected order.
			var analysesGuid = new List<string>(1) { "068DC680-CD40-4C47-BBE6-00E572EE2602" };
			DomainObjectDTO oneSegmentDto;
			var oneSegmentInnerElement = CheckNewSegment(dtoRepos, 0, oneSegmentParaDto,
				newSegmentObjSurElements[0], out oneSegmentDto);
			Assert.AreEqual("0", oneSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);

			// Check on the number of converted CCRs.
			var chartElement = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("DsConstChart").First().Xml);
			var rowsObjSurElements = chartElement.Element("DsConstChart").Element("Rows").Elements().ToList();
			var rowsCount = rowsObjSurElements.Count;
			Assert.AreEqual(1, rowsCount, "Wrong number of rows");
			Assert.AreEqual(rowsCount, (from objSurElement in rowsObjSurElements
										where objSurElement.Attribute("t").Value == "o"
										select objSurElement).Count(), "Wrong number of owned rows");

			// The first (and only) row has one CCA to convert,
			// Check first CCR conversion.
			var currentObjsurElement = rowsObjSurElements[0];
			CheckCCR(dtoRepos, currentObjsurElement, null, "1a", "0", "false", "false", "false", "false", 2);
			// Check our one cell in our one CCR conversion.
			var currentRowElement = XElement.Parse(dtoRepos.GetDTO(currentObjsurElement.Attribute("guid").Value).Xml);
			var allCellObjsurElements = currentRowElement.Element("ConstChartRow").Element("Cells").Elements("objsur").ToList();
			var currentCellDto = dtoRepos.GetDTO(allCellObjsurElements[0].Attribute("guid").Value);
			var currentCellElement = XElement.Parse(currentCellDto.Xml);
			//	Cell 1 of 1: One twfic -> ConstChartWordGroup
			 var constituentChartCellPartInnerElement = currentCellElement.Element("ConstChartWordGroup");
			// Get Segment number one
			var firstSegmentGuid = newSegmentObjSurElements[0].Attribute("guid").Value;
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("0",
				constituentChartCellPartInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			Assert.AreEqual("0",
				constituentChartCellPartInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");

			// ----------------------------------------------------------------
			// Check pathological para (40ccde9a-cb33-444b-a30b-8728d2c7d7ee)
			// This one has two twfics and one segment, but the segment doesn't begin until
			// after the two twfics. (problem from FWR-3081)
			// The previous version of the migration code ended up with "ChangeME" as the
			// segment in this case when converting a wfic in that para.
			var twoSegmentParaDto = CheckPara(dtoRepos, "40ccde9a-cb33-444b-a30b-8728d2c7d7ee", 2, true,
				false, out newSegmentObjSurElements); // all paragraphs now get reparsed on load
			// This list provides the Analyses in the expected order.
			var analysisGuids = new List<string>
									{
										"068DC680-CD40-4C47-BBE6-00E572EE2602",
										"068DC680-CD40-4C47-BBE7-00E572EE2602"
									};
			var firstSegmentInnerElement = CheckNewSegment(dtoRepos, 0, twoSegmentParaDto,
				newSegmentObjSurElements[0], out oneSegmentDto);
			CheckXfics(dtoRepos, firstSegmentInnerElement, 2, analysisGuids); // it won't make a pfic, will it?
			var secondSegmentInnerElement = CheckNewSegment(dtoRepos, 11, twoSegmentParaDto,
				newSegmentObjSurElements[1], out oneSegmentDto);

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over the pathological migration cases at the Segment level.
		/// It does not treat discourse or syntax cases.
		///
		/// This test will include:
		///		1. Duplicate xfics for some BeginOffset.
		///
		/// Paragraphs with this problem will have their ParseIsCurrent
		/// set to 'False', so they will be retokenized to be put back in order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_PathologicalSegmentMigration_Duplicates_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> {"DataMigration7000010_PathologicalSegments_Duplicates.xml"},
				6);

			// Check defective para (60BF79A0-9579-5486-A32F-944490F0C024)
			// This one had a segment and two duplicate twfics (both glosses).
			// the first gloss should 'win'.
			// Make sure the para is not current on tokenization (with true, false parms).
			var noteGuids = new List<string>();
			List<XElement> newSegmentObjSurElements;
			var currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-5486-A32F-944490F0C024", 1, true, false, out newSegmentObjSurElements);
			// Check new segment. (It only has one.)
			// This list provides the Analyses in the expected order.
			var analysesGuids = new List<string>(1)
									{
										"068DC680-CD40-4C47-BBE6-00E572EE2602"
									};
			DomainObjectDTO newSegmentDto;
			var newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Check defective para (60BF79A0-9579-5486-A32F-944490F0C025)
			// One segment with two duplicates (analysis & gloss).
			// The gloss should win.
			// Make sure the para is not current on tokenization (with true, false parms).
			currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-5486-A32F-944490F0C025", 1, true, false, out newSegmentObjSurElements);
			analysesGuids.Clear();
			// This list provides the Analyses in the expected order.
			analysesGuids.Add("068DC680-CD40-4C47-BBE7-00E572EE2602");
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Check defective para (60BF79A0-9579-5486-A32F-944490F0C026)
			// One segment with two duplicates (wordform + analysis).
			// The analysis should win.
			// Make sure the para is not current on tokenization (with true, false parms).
			currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-5486-A32F-944490F0C026", 1, true, false, out newSegmentObjSurElements);
			analysesGuids.Clear();
			// This list provides the Analyses in the expected order.
			analysesGuids.Add("FC5E167A-203B-4A86-BCBA-45E8643ACC0A");
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Check defective para (60BF79A0-9579-5486-A32F-944490F0C027)
			// One segment with three duplicates (wordform + analysis + gloss).
			// The gloss should win.
			// Make sure the para is not current on tokenization (with true, false parms).
			currentParaDto = CheckPara(dtoRepos, "60BF79A0-9579-5486-A32F-944490F0C027", 1, true, false, out newSegmentObjSurElements);
			analysesGuids.Clear();
			// This list provides the Analyses in the expected order.
			analysesGuids.Add("068DC680-CD40-4C47-BBE7-00E572EE2602");
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Check defective para (c1ecaa65-e382-11de-8a39-0800200c9a66)
			// This one had a segment and two duplicate twfics (both glosses).
			// the second gloss should 'win', since it has inbound refs from a text tag.
			// Make sure the para is not current on tokenization (with true, false parms).
			currentParaDto = CheckPara(dtoRepos, "c1ecaa65-e382-11de-8a39-0800200c9a66", 1, true, false, out newSegmentObjSurElements);
			analysesGuids.Clear();
			// This list provides the Analyses in the expected order.
			analysesGuids.Add("068DC680-CD40-4C47-BBE7-00E572EE2602");
			newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, currentParaDto, newSegmentObjSurElements[0], out newSegmentDto);
			CheckComment(newSegmentInnerElement, "LiteralTranslation", false, 0);
			CheckComment(newSegmentInnerElement, "FreeTranslation", false, 0);
			CheckNotes(dtoRepos, newSegmentDto, noteGuids, newSegmentInnerElement, false, 0);
			CheckXfics(dtoRepos, newSegmentInnerElement, 1, analysesGuids);
			Assert.AreEqual("0", newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value);

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over the pathological migration cases at the CCR level.
		///
		/// This test will include:
		///		1. Make sure a CCR is deleted, and not converted,
		///			if it is not referenced by a chart.
		///		2. Make sure a CCA is deleted, and not converted,
		///			if it is referenced by a defective CCR.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_PathologicalCCRMigration_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> {"DataMigration7000010_PathologicalDiscourseChartRows.xml"},
				1);

			// Make sure there is only one ConstChartRows or any of the new classes that replaced the old CCAs.
			Assert.AreEqual(1, dtoRepos.AllInstancesSansSubclasses("ConstChartRow").Count());
			Assert.AreEqual(0, dtoRepos.AllInstancesWithSubclasses("ConstituentChartCellPart").Count());

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test exposes a bug where the migration crashed if a moved text marker's comment
		/// was in another ws than 'en'.
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_WrongMovedTextMarkerWsTest()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> { "DataMigration7000010_WrongMovedTextMarkerWs.xml" }, 2);

			// Make sure there is only one ConstChartRow and only two of the new classes that replaced the old CCAs.
			Assert.AreEqual(1, dtoRepos.AllInstancesSansSubclasses("ConstChartRow").Count());
			var ccwgDto = dtoRepos.AllInstancesSansSubclasses("ConstChartWordGroup");
			Assert.AreEqual(1, ccwgDto.Count());
			var ccwgElement = XElement.Parse(ccwgDto.First().Xml);
			var ccwgGuid = ccwgElement.Attribute("guid").Value;

			var mtmarkerDto = dtoRepos.AllInstancesSansSubclasses("ConstChartMovedTextMarker");
			Assert.AreEqual(1, mtmarkerDto.Count());
			var mtmElement = XElement.Parse(mtmarkerDto.First().Xml).Element("ConstChartMovedTextMarker");
			var mtmWordGrpElement = mtmElement.Element("WordGroup");
			Assert.AreEqual(ccwgGuid, mtmWordGrpElement.Element("objsur").Attribute("guid").Value,
				"Moved Text Marker doesn't point to the right WordGroup.");
			Assert.AreEqual("True", mtmElement.Element("Preposed").Attribute("val").Value,
				"Moved Text Marker should be Preposed.");

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over a normal migration case regarding "duplicate" twfics,
		/// where two identical wordforms in the same segment were not previously
		/// processed correctly.
		///
		/// This test will include:
		///		1. Make sure a CCA is converted to a WordGroup correctly,
		///			even if the CCA has internal punctuation.
		///		2. Make sure a CCA is converted to a WordGroup correctly,
		///			even if the CCA has two identical words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_DiscoursePunctuation_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> { "DataMigration7000010_DiscoursePunct.xml" },
				2); // one is from a surviving annotationdefn, one from test

			// Make sure there is only one ConstChartRows or any of the new classes that replaced the old CCAs.
			var chartElement = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("DsConstChart").First().Xml);
			var rowsObjSurElements = chartElement.Element("DsConstChart").Element("Rows").Elements().ToList();
			Assert.AreEqual(1, rowsObjSurElements.Count, "Wrong number of rows");
			var onlyRow = rowsObjSurElements[0];
			CheckCCR(dtoRepos, onlyRow, null, "NoNotesOrCompDetails", "0", "false", "false", "false", "false", 2);

			// Get the info for the first cellpart -- a ConstChartWordGroup
			var currentRowElement = XElement.Parse(dtoRepos.GetDTO(onlyRow.Attribute("guid").Value).Xml);
			var allCellObjsurElements = currentRowElement.Element("ConstChartRow").Element("Cells").Elements("objsur").ToList();
			Assert.AreEqual(2, allCellObjsurElements.Count, "Wrong number of cells.");
			var firstCellDto = dtoRepos.GetDTO(allCellObjsurElements[0].Attribute("guid").Value);
			var firstCellElement = XElement.Parse(firstCellDto.Xml);

			// The first CCWordGroup should cover one segment with BeginAnalysisIndex = 0
			// and EndAnalysisIndex = 1. Index = 2 is a comma (punctuation) and won't be included,
			// while index 1 refers to the word 'paragraph'.
			var constituentChartCellPartInnerElement = firstCellElement.Element("ConstChartWordGroup");
			// Get the para with this text and segment number one of two.
			var paraDto = dtoRepos.GetDTO("025a8737-c9ac-4453-9afd-178cf90d231d");
			var paraElement = XElement.Parse(paraDto.Xml);
			var firstSegmentObjsur = paraElement.Element("StTxtPara").Element("Segments").Element("objsur");
			var firstSegmentGuid = firstSegmentObjsur.Attribute("guid").Value;
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("0",
				constituentChartCellPartInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			Assert.AreEqual("1",
				constituentChartCellPartInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");

			var secondCellDto = dtoRepos.GetDTO(allCellObjsurElements[1].Attribute("guid").Value);
			var secondCellElement = XElement.Parse(secondCellDto.Xml);

			// The second CCWordGroup should cover one segment with BeginAnalysisIndex = 3
			// and EndAnalysisIndex = 3. Index = 3 refers to second occurrence of the word 'paragraph'.
			var secondChartCellPartInnerElement = secondCellElement.Element("ConstChartWordGroup");
			// Get the para with this text and segment number one of two.
			Assert.AreEqual(firstSegmentGuid,
				secondChartCellPartInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(firstSegmentGuid,
				secondChartCellPartInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("3",
				secondChartCellPartInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			Assert.AreEqual("3",
				secondChartCellPartInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");

			// Check segment Analyses sequence
			var segmentElement = XElement.Parse(dtoRepos.GetDTO(firstSegmentGuid).Xml);
			var allAnalysesObjsurElements = segmentElement.Element("Segment").Element("Analyses").Elements("objsur").ToList();
			Assert.AreEqual(6, allAnalysesObjsurElements.Count, "Wrong number of analyses");

			// Make sure the new PunctuationForm was created (the comma is created on load).
			var punctFormDtos = dtoRepos.AllInstancesSansSubclasses("PunctuationForm").ToList();
			Assert.AreEqual(1, punctFormDtos.Count, "Wrong number of new PunctuationForm objects.");
			var punctPeriodDto = punctFormDtos[0];

			//Check first new segment.
			// This list provides the Analyses in the expected order.
			var analysesGuids = new List<string>(6)
									{
										"79041d50-092a-4a4f-aae1-fc99815e23e5",
										"96cc7e7b-b13e-4d3b-9d8d-712a25b89ca8",
										"96cc7e7b-b13e-4d3b-9d8d-712a25b89ca8",
										"c9d335d9-d0bd-49c8-b4be-8ba8ea32e2f8",
										"d0c52468-ceb3-4860-8cbf-2e68ac5b68ad",
										punctPeriodDto.Guid
									};
			DomainObjectDTO newSegmentDto;
			var newSegmentInnerElement = CheckNewSegment(dtoRepos, 0, paraDto, firstSegmentObjsur, out newSegmentDto);
			// 4 different twfics (one repeated) and 2 pfics.
			CheckXfics(dtoRepos, newSegmentInnerElement, 6, analysesGuids);

			// Check our two TextTags too. They should have the same references as the new CCWordGroups above.
			var tagDtoList = dtoRepos.AllInstancesSansSubclasses("TextTag").ToList();
			Assert.AreEqual(2, tagDtoList.Count, "Wrong number of TextTag objects.");

			// First Tag
			var firstTagInnerElement = XElement.Parse(tagDtoList[0].Xml).Element("TextTag");
			Assert.AreEqual(firstSegmentGuid,
				firstTagInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(firstSegmentGuid,
				firstTagInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("0",
				firstTagInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			// Previous UI didn't allow tagging punctuation, so migration disallowed it too.
			Assert.AreEqual("1",
				firstTagInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");

			// Second Tag
			var secondTagInnerElement = XElement.Parse(tagDtoList[1].Xml).Element("TextTag");
			Assert.AreEqual(firstSegmentGuid,
				secondTagInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(firstSegmentGuid,
				secondTagInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("3",
				secondTagInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			Assert.AreEqual("3",
				secondTagInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");


			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over a migration case that repairs (by deletion) a chart,
		/// where two CCAs in different charts (on different texts) reference the same
		/// twfic (shouldn't be possible in stable releases, but apparently some version in
		/// the past allowed this). The migration figures out which CCA points to a twfic that
		/// isn't on its own chart's text and deletes the CCA.
		///
		/// This test will include:
		///		1. Make sure a CCA is converted to a WordGroup correctly,
		///			even if the CCA has internal punctuation.
		///		2. Make sure a CCA is converted to a WordGroup correctly,
		///			even if the CCA has two identical words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_BadCcaReference_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> { "DataMigration7000010_Discourse_BadCCARef.xml" },
				3); // one is from a surviving annotationdefn, two from test

			// Get charts
			var chartDtoList = dtoRepos.AllInstancesSansSubclasses("DsConstChart").ToList();
			Assert.AreEqual(2, chartDtoList.Count, "Should be two charts.");
			// Get 1st chart
			var chartOneElement = XElement.Parse(chartDtoList[0].Xml);
			// Get 2nd chart
			var chartTwoElement = XElement.Parse(chartDtoList[1].Xml);
			// Make sure first chart has only one CCR and only one CCA.
			// Check chart1 row
			var chart1row1ObjSurElements = chartOneElement.Element("DsConstChart").Element("Rows").Elements().ToList();
			Assert.AreEqual(1, chart1row1ObjSurElements.Count, "Wrong number of rows");
			var ccr1Dto = dtoRepos.GetDTO(chart1row1ObjSurElements[0].Attribute("guid").Value);
			var ccr1 = XElement.Parse(ccr1Dto.Xml);
			CheckCCR(dtoRepos, ccr1, null, "1", "0", "false", "false", "false", "false", 1);

			var chart2row1ObjSurElements = chartTwoElement.Element("DsConstChart").Element("Rows").Elements().ToList();
			// after DeLint, this may not even survive! (which would be good)
			Assert.AreEqual(1, chart2row1ObjSurElements.Count, "Wrong number of rows");
			var ccr2Dto = dtoRepos.GetDTO(chart2row1ObjSurElements[0].Attribute("guid").Value);
			var ccr2 = XElement.Parse(ccr2Dto.Xml);
			CheckCCR(dtoRepos, ccr2, null, "1", "0", "false", "false", "false", "false", 0);

			// not done!!!
			// Get the info for the first cellpart -- a ConstChartWordGroup
			var currentRowElement = ccr1;
			var allCellObjsurElements = currentRowElement.Element("ConstChartRow").Element("Cells").Elements("objsur").ToList();
			Assert.AreEqual(1, allCellObjsurElements.Count, "Wrong number of cells.");
			var firstCellDto = dtoRepos.GetDTO(allCellObjsurElements[0].Attribute("guid").Value);
			var firstCellElement = XElement.Parse(firstCellDto.Xml);

			// The first CCWordGroup should cover one segment with BeginAnalysisIndex = 0
			// and EndAnalysisIndex = 0.
			var constituentChartCellPartInnerElement = firstCellElement.Element("ConstChartWordGroup");
			// Get the para with this text and segment number one of one.
			var paraDto = dtoRepos.GetDTO("c1ec835d-e382-11de-8a39-0800200c9a66");
			var paraElement = XElement.Parse(paraDto.Xml);
			var firstSegmentObjsur = paraElement.Element("StTxtPara").Element("Segments").Element("objsur");
			var firstSegmentGuid = firstSegmentObjsur.Attribute("guid").Value;
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("0",
				constituentChartCellPartInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			Assert.AreEqual("0",
				constituentChartCellPartInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");
			// should only have one CCWG left
			Assert.AreEqual(1, dtoRepos.AllInstancesSansSubclasses("ConstChartWordGroup").Count(),
				"Wrong number of Word Group objects.");

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000009 to 7000010.
		///
		/// This test works over the normal discourse migration cases.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000010_DiscourseMigration_Test()
		{
			var dtoRepos = DoCommonBasics(
				new List<string> { "DataMigration7000010_Discourse.xml", "DataMigration7000010_TextTags.xml" }, 2);

			// Check on the number of converted CCRs.
			var chartElement = XElement.Parse(dtoRepos.AllInstancesSansSubclasses("DsConstChart").First().Xml);
			var rowsObjSurElements = chartElement.Element("DsConstChart").Element("Rows").Elements().ToList();
			var rowsCount = rowsObjSurElements.Count;
			Assert.AreEqual(6, rowsCount, "Wrong number of rows");
			Assert.AreEqual(rowsCount, (from objSurElement in rowsObjSurElements
										   where objSurElement.Attribute("t").Value == "o"
										   select objSurElement).Count(), "Wrong number of owned rows");

			// The first five have no CCAs to convert,
			// since they are used to check all of the other properties of the new CCR,
			// Check first CCR conversion.
			CheckCCR(dtoRepos, rowsObjSurElements[0],
				null, "NoNotesOrCompDetails", "0", "false", "false", "false", "false", 0);
			// Check second CCR conversion.
			CheckCCR(dtoRepos, rowsObjSurElements[1],
				"Blah, blah.", "NotesAndAllNonEnumCompDetails", "0", "true", "true", "true", "true", 0);
			// Check third CCR conversion.
			CheckCCR(dtoRepos, rowsObjSurElements[2],
				null, "dependent", "1", "false", "false", "false", "false", 0);
			// Check fourth CCR conversion.
			CheckCCR(dtoRepos, rowsObjSurElements[3],
				null, "speech", "3", "false", "false", "false", "false", 0);
			// Check fifth CCR conversion.
			CheckCCR(dtoRepos, rowsObjSurElements[4],
				null, "song", "2",
				"false", "false", "false", "false", 0);
			// Check sixth CCR conversion.
			var currentObjsurElement = rowsObjSurElements[5];
			CheckCCR(dtoRepos, currentObjsurElement,
				null, "normal", "0",
				"false", "false", "false", "false", 6);
			// Check each cell in sixth CCR conversion.
			var currentRowElement = XElement.Parse(dtoRepos.GetDTO(currentObjsurElement.Attribute("guid").Value).Xml);
			var allCellObjsurElements = currentRowElement.Element("ConstChartRow").Element("Cells").Elements("objsur").ToList();
			var currentCellDto = dtoRepos.GetDTO(allCellObjsurElements[0].Attribute("guid").Value);
			var currentCellElement = XElement.Parse(currentCellDto.Xml);
			//	1. CmBaseAnnotations -> ConstChartTag
			//			Cell 1 of 6: (CmBaseAnnotation -> ConstChartTag)
			Assert.AreEqual("ConstChartTag", currentCellDto.Classname, "Wrong class name in cell dto.");
			Assert.AreEqual("ConstChartTag", currentCellElement.Attribute("class").Value, "Wrong class name in cell element.");
			var constituentChartCellPartInnerElement = currentCellElement.Element("ConstituentChartCellPart");
			Assert.AreEqual("c1ec835f-e382-11de-8a39-0800200c9a66",
				constituentChartCellPartInnerElement.Element("Column").Element("objsur").Attribute("guid").Value,
				"Wrong Column value.");
			Assert.AreEqual("r",
				constituentChartCellPartInnerElement.Element("Column").Element("objsur").Attribute("t").Value, "Wrong type.");
			Assert.AreEqual("True",
				constituentChartCellPartInnerElement.Element("MergesAfter").Attribute("val").Value,
				"Wrong MergesAfter value.");
			Assert.AreEqual("True",
				constituentChartCellPartInnerElement.Element("MergesBefore").Attribute("val").Value,
				"Wrong MergesBefore value.");
			var mainClassInnerElement = currentCellElement.Element("ConstChartTag");
			Assert.AreEqual("c1ec8360-e382-11de-8a39-0800200c9a66",
				mainClassInnerElement.Element("Tag").Element("objsur").Attribute("guid").Value,
				"Wrong Tag value.");
			Assert.AreEqual("r",
				mainClassInnerElement.Element("Tag").Element("objsur").Attribute("t").Value, "Wrong type.");
			//	2. CmIndirectAnnotation
			//		A. Cell 2 of 6: One (or more) CCRs in AppliesTo-> ConstChartClauseMarker
			// Should have two DependentClauses.
			currentCellDto = dtoRepos.GetDTO(allCellObjsurElements[1].Attribute("guid").Value);
			currentCellElement = XElement.Parse(currentCellDto.Xml);
			Assert.AreEqual("ConstChartClauseMarker", currentCellDto.Classname, "Wrong class name in cell dto.");
			Assert.AreEqual("ConstChartClauseMarker", currentCellElement.Attribute("class").Value, "Wrong class name in cell element.");
			constituentChartCellPartInnerElement = currentCellElement.Element("ConstituentChartCellPart");
			Assert.AreEqual("c1ec835f-e382-11de-8a39-0800200c9a66",
				constituentChartCellPartInnerElement.Element("Column").Element("objsur").Attribute("guid").Value,
				"Wrong Column value.");
			Assert.AreEqual("False",
				constituentChartCellPartInnerElement.Element("MergesAfter").Attribute("val").Value,
				"Wrong MergesAfter value.");
			Assert.AreEqual("False",
				constituentChartCellPartInnerElement.Element("MergesBefore").Attribute("val").Value,
				"Wrong MergesBefore value.");
			constituentChartCellPartInnerElement = currentCellElement.Element("ConstChartClauseMarker");
			var dependentClauseObjSurElements =
				constituentChartCellPartInnerElement.Element("DependentClauses").Elements("objsur").ToList();
			Assert.AreEqual(2, dependentClauseObjSurElements.Count, "Wrong number of dependent clause elements.");
			Assert.AreEqual(rowsObjSurElements[0].Attribute("guid").Value,
				dependentClauseObjSurElements[0].Attribute("guid").Value, "Wrong first CCR guid.");
			Assert.AreEqual("r", dependentClauseObjSurElements[0].Attribute("t").Value, "Wrong first type.");
			Assert.AreEqual(rowsObjSurElements[1].Attribute("guid").Value,
				dependentClauseObjSurElements[1].Attribute("guid").Value, "Wrong second CCR guid.");
			Assert.AreEqual("r", dependentClauseObjSurElements[1].Attribute("t").Value, "Wrong second type.");
			//	2. CmIndirectAnnotation
			//		B. Cell 3 of 6: One (only) CCA in AppliesTo -> ConstChartMovedTextMarker
			currentCellDto = dtoRepos.GetDTO(allCellObjsurElements[2].Attribute("guid").Value);
			currentCellElement = XElement.Parse(currentCellDto.Xml);
			constituentChartCellPartInnerElement = currentCellElement.Element("ConstChartMovedTextMarker");
			Assert.AreEqual("True", constituentChartCellPartInnerElement.Element("Preposed").Attribute("val").Value, "Wrong Preposed value.");
			Assert.AreEqual(1, constituentChartCellPartInnerElement.Element("WordGroup").Elements("objsur").Count(), "Wrong WordGroup count.");
			Assert.AreEqual("r", constituentChartCellPartInnerElement.Element("WordGroup").Elements("objsur").ToList()[0].Attribute("t").Value, "Wrong type.");
			//	2. CmIndirectAnnotation
			//		C. Cell 4 of 6: null AppliesTo -> ConstChartWordGroup
			currentCellDto = dtoRepos.GetDTO(allCellObjsurElements[3].Attribute("guid").Value);
			currentCellElement = XElement.Parse(currentCellDto.Xml);
			constituentChartCellPartInnerElement = currentCellElement.Element("ConstChartWordGroup");
			Assert.AreEqual("-1", constituentChartCellPartInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value, "Wrong BeginAnalysisIndex value.");
			Assert.AreEqual("-1", constituentChartCellPartInnerElement.Element("EndAnalysisIndex").Attribute("val").Value, "Wrong EndAnalysisIndex value.");
			Assert.IsNull(constituentChartCellPartInnerElement.Element("BeginSegment"), "Found an BeginSegment elment.");
			Assert.IsNull(constituentChartCellPartInnerElement.Element("EndSegment"), "Found an EndSegment elment.");
			//	2. CmIndirectAnnotation
			//		D. Cell 5 of 6: One (or more) twfics (pfics?) -> ConstChartWordGroup
			currentCellDto = dtoRepos.GetDTO(allCellObjsurElements[4].Attribute("guid").Value);
			currentCellElement = XElement.Parse(currentCellDto.Xml);
			constituentChartCellPartInnerElement = currentCellElement.Element("ConstChartWordGroup");
			// Get the para with this text: Segment number one of two. Segment number two of two.
			var paraElement = XElement.Parse(dtoRepos.GetDTO("c1ec5c4f-e382-11de-8a39-0800200c9a66").Xml);
			var firstSegmentGuid = paraElement.Element("StTxtPara").Element("Segments").Element("objsur").Attribute("guid").Value;
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(firstSegmentGuid,
				constituentChartCellPartInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("0",
				constituentChartCellPartInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			Assert.AreEqual("5",
				constituentChartCellPartInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");
			//	2. CmIndirectAnnotation
			//		D. Cell 6 of 6: One (or more) twfics (pfics?) -> ConstChartWordGroup in second Segment
			currentCellDto = dtoRepos.GetDTO(allCellObjsurElements[5].Attribute("guid").Value);
			currentCellElement = XElement.Parse(currentCellDto.Xml);
			constituentChartCellPartInnerElement = currentCellElement.Element("ConstChartWordGroup");
			// Get the second segment with this text: Segment number two of two.
			var secondSegmentGuid = paraElement.Element("StTxtPara").Element("Segments").Element("objsur").ElementsAfterSelf().First().Attribute("guid").Value;
			Assert.AreEqual(secondSegmentGuid,
				constituentChartCellPartInnerElement.Element("BeginSegment").Element("objsur").Attribute("guid").Value,
				"Wrong BeginSegment.");
			Assert.AreEqual(secondSegmentGuid,
				constituentChartCellPartInnerElement.Element("EndSegment").Element("objsur").Attribute("guid").Value,
				"Wrong EndSegment.");
			Assert.AreEqual("0",
				constituentChartCellPartInnerElement.Element("BeginAnalysisIndex").Attribute("val").Value,
				"Wrong BeginAnalysisIndex.");
			Assert.AreEqual("4",
				constituentChartCellPartInnerElement.Element("EndAnalysisIndex").Attribute("val").Value,
				"Wrong EndAnalysisIndex.");

			// Make sure version number is correct.
			Assert.AreEqual(7000010, dtoRepos.CurrentModelVersion, "Wrong updated version.");
		}

		private static void CheckCCR(
			IDomainObjectDTORepository dtoRepos,
			XElement rowObjSurElement,
			string notes,
			string label,
			string clauseType,
			string endParagraph,
			string endSentence,
			string startDep,
			string endDep,
			int cellsCount)
		{
			var currentCCRElement = XElement.Parse(
				dtoRepos.GetDTO(rowObjSurElement.Attribute("guid").Value).Xml);
			var innerConstChartRowElement = currentCCRElement.Element("ConstChartRow");
			if (notes == null)
				Assert.IsNull(innerConstChartRowElement.Element("Notes"), "Has Notes.");
			else
				Assert.AreEqual(notes, innerConstChartRowElement.Element("Notes").Value, "Wrong Notes.");
			Assert.AreEqual(label, innerConstChartRowElement.Element("Label").Value, "Wrong Label.");
			Assert.AreEqual(clauseType, innerConstChartRowElement.Element("ClauseType").Attribute("val").Value, "Wrong ClauseType.");
			Assert.AreEqual(endParagraph, innerConstChartRowElement.Element("EndParagraph").Attribute("val").Value.ToLower(), "Wrong EndParagraph.");
			Assert.AreEqual(endSentence, innerConstChartRowElement.Element("EndSentence").Attribute("val").Value.ToLower(), "Wrong EndSentence.");
			Assert.AreEqual(startDep, innerConstChartRowElement.Element("StartDependentClauseGroup").Attribute("val").Value.ToLower(), "Wrong StartDependentClauseGroup.");
			Assert.AreEqual(endDep, innerConstChartRowElement.Element("EndDependentClauseGroup").Attribute("val").Value.ToLower(), "Wrong EndDependentClauseGroup.");
			var cellsElement = innerConstChartRowElement.Element("Cells");
			if (cellsCount == 0)
				Assert.IsNull(cellsElement, "Has Cells.");
			else
				Assert.AreEqual(cellsCount, cellsElement.Elements("objsur").Count(), "Wrong cell count.");
		}

		private IDomainObjectDTORepository DoCommonBasics(IEnumerable<string> extraDataFiles, int expectedParagraphCount)
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000010_AnnotationDefns.xml");
			dtos.UnionWith(DataMigrationTestServices.ParseProjectFile("DataMigration7000010_CommonData.xml"));
			foreach (var extraDataFile in extraDataFiles)
				dtos.UnionWith(DataMigrationTestServices.ParseProjectFile(extraDataFile));
			var dtoRepos = SetupMDC(dtos);

			// Should be deleted.
			var oldBaseAnnDtos = dtoRepos.AllInstancesSansSubclasses("CmBaseAnnotation").ToList();
			// Should be deleted.
			var oldIndirectAnnDtos = dtoRepos.AllInstancesSansSubclasses("CmIndirectAnnotation").ToList();

			// Do the migration.
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000010, new DummyProgressDlg());

			foreach (var oldBaseAnnDto in oldBaseAnnDtos)
				DataMigrationTestServices.CheckDtoRemoved(dtoRepos, oldBaseAnnDto);
			foreach (var oldIndirectAnnDto in oldIndirectAnnDtos)
				DataMigrationTestServices.CheckDtoRemoved(dtoRepos, oldIndirectAnnDto);

			// One is from a surviving AnnDefn, with the other two from the real world.
			Assert.AreEqual(expectedParagraphCount, dtoRepos.AllInstancesSansSubclasses("StTxtPara").Count(), "Wrong ending para count.");

			return dtoRepos;
		}

		private static IDomainObjectDTORepository SetupMDC(HashSet<DomainObjectDTO> dtos)
		{
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "CmProject", "StPara", "CmAnnotation",
				"Segment", "Note", "PunctuationForm",
				"TextTag",
				"ConstChartRow", "ConstituentChartCellPart"
				});
			mockMDC.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMDC.AddClass(3, "LangProject", "CmProject", new List<string>());
			mockMDC.AddClass(4, "StPara", "CmObject", new List<string> { "StTxtPara" });
			mockMDC.AddClass(5, "StTxtPara", "StPara", new List<string>());
			mockMDC.AddClass(6, "CmAnnotation", "CmObject", new List<string> { "CmBaseAnnotation", "CmIndirectAnnotation" });
			mockMDC.AddClass(7, "CmBaseAnnotation", "CmAnnotation", new List<string>());
			mockMDC.AddClass(8, "CmIndirectAnnotation", "CmAnnotation", new List<string>());
			// Add new model classes.
			mockMDC.AddClass(9, "Segment", "CmObject", new List<string>());
			mockMDC.AddClass(10, "Note", "CmObject", new List<string>());
			mockMDC.AddClass(11, "PunctuationForm", "CmObject", new List<string>());
			mockMDC.AddClass(12, "TextTag", "CmObject", new List<string>());
			mockMDC.AddClass(13, "ConstChartRow", "CmObject", new List<string>());
			mockMDC.AddClass(14, "ConstituentChartCellPart", "CmObject", new List<string> { "ConstChartWordGroup", "ConstChartMovedTextMarker", "ConstChartClauseMarker", "ConstChartTag" });
			mockMDC.AddClass(15, "ConstChartWordGroup", "ConstituentChartCellPart", new List<string>());
			mockMDC.AddClass(16, "ConstChartMovedTextMarker", "ConstituentChartCellPart", new List<string>());
			mockMDC.AddClass(17, "ConstChartClauseMarker", "ConstituentChartCellPart", new List<string>());
			mockMDC.AddClass(18, "ConstChartTag", "ConstituentChartCellPart", new List<string>());
			return new DomainObjectDtoRepository(7000009, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);
		}

		private static void CheckXfics(IDomainObjectDTORepository dtoRepos,
			XContainer newSegmentInnerElement, int expectedAnalysesCount,
			IList<string> analysesGuids)
		{
			var analysesElement = newSegmentInnerElement.Element("Analyses");
			if (expectedAnalysesCount == 0)
			{
				if (analysesElement != null)
					Assert.AreEqual(expectedAnalysesCount, analysesElement.Elements("objsur").Count(), "Unexpected analyses.");
				return; // nothing else to check (and it's OK if the element is null
			}
			Assert.IsNotNull(analysesElement, "Missing Analyses Element in the new Segment object.");
			var analObjSurElements = analysesElement.Elements("objsur");
			Assert.AreEqual(expectedAnalysesCount, analObjSurElements.Count(), "Wrong Analyses count.");
			for (var i = 0; i < expectedAnalysesCount; ++i)
			{
				var newAnalObjsurElement = analObjSurElements.ElementAt(i);
				Assert.IsNotNull(newAnalObjsurElement, "Missing objsur Element in the new Analyses element.");
				Assert.AreEqual("r", newAnalObjsurElement.Attribute("t").Value, "Not a reference property.");
				var analysisDto = dtoRepos.GetDTO(newAnalObjsurElement.Attribute("guid").Value);
				Assert.IsNotNull(analysisDto, "Missing analysis dto.");

				// Make sure the right analysis guid is used.
				Assert.AreEqual(analysesGuids[i].ToLower(), analysisDto.Guid.ToLower(), "Wrong guid.");
			}
		}

		private static void CheckNotes(IDomainObjectDTORepository dtoRepos, DomainObjectDTO newSegmentDto, ICollection noteGuids, XContainer newSegmentInnerElement, bool expectedToHaveNotes, int expectedNumberOfNotes)
		{
			// Check Nts.
			var notesElements = newSegmentInnerElement.Elements("Notes");
			if (!expectedToHaveNotes)
			{
				Assert.AreEqual(0, notesElements.Count(), "Had unexpected number of notes.");
				return;
			}

			var objsurElements = notesElements.Elements("objsur");
			Assert.AreEqual(expectedNumberOfNotes, objsurElements.Count(), "Wrong number of Notes.");
			foreach (var objsurElement in objsurElements)
			{
				// Make sure the objsur guid is in 'noteGuids'.
				var objsurGuid = objsurElement.Attribute("guid").Value;
				Assert.Contains(objsurGuid, noteGuids);

				// Make sure the objsur element is t="o" (owning).
				var type = objsurElement.Attribute("t").Value;
				Assert.AreEqual("o", type, "Not an owning property.");

				// Make sure the owner of the objsur guid is the new Segment.
				var noteDto = dtoRepos.GetDTO(objsurGuid);
				Assert.AreSame(newSegmentDto, dtoRepos.GetOwningDTO(noteDto));

				// Each Nt should have two alts.
				var noteElement = XElement.Parse(noteDto.Xml);
				var contentElement = noteElement.Element("Note").Element("Content");
				Assert.IsNotNull(contentElement, "No Nt Content element.");
				CheckAlternatives(contentElement, 2);
			}
		}

		private static void CheckComment(XContainer newSegmentInnerElement, string elementName, bool expectToHaveTranslation, int altenativeCount)
		{
			var commentNode = newSegmentInnerElement.Element(elementName);
			if (expectToHaveTranslation)
			{
				CheckAlternatives(commentNode, altenativeCount);
			}
			else
			{
				Assert.IsNull(commentNode, "Found unexpected comment node.");
			}
		}

		private static void CheckAlternatives(XContainer multiStringElement, int altenativeCount)
		{
			Assert.IsNotNull(multiStringElement, "No MultiString element.");
			var alternativesNodes = multiStringElement.Elements("AStr");
			Assert.AreEqual(altenativeCount, alternativesNodes.Count(), "Wrong number of alternatives.");
		}

		private static DomainObjectDTO CheckPara(IDomainObjectDTORepository dtoRepos, string paraGuid,
			int segmentCount, bool checkForParseIsCurrent, bool expectedParseIsCurrentValue,
			out List<XElement> newSegmentObjSurElements)
		{
			var currentParaDto = dtoRepos.GetDTO(paraGuid);
			var newParaElement = XElement.Parse(currentParaDto.Xml);
			var stTxtParaInnerElement = newParaElement.Element("StTxtPara");
			var segmentsElement = stTxtParaInnerElement.Element("Segments");
			if (segmentCount == 0)
			{
				newSegmentObjSurElements = null;
				Assert.IsNull(segmentsElement, "Existing Segments element.");
			}
			else
			{
				Assert.IsNotNull(segmentsElement, "Missing Segments element.");
				// Check that it has correct number of new segments.
				newSegmentObjSurElements = segmentsElement.Elements("objsur").ToList();
				Assert.IsNotNull(newSegmentObjSurElements, "Missing objsur elements.");
				Assert.AreEqual(segmentCount, newSegmentObjSurElements.Count(), "Wrong number of new segments.");
			}

			if (checkForParseIsCurrent)
			{
				Assert.AreEqual(expectedParseIsCurrentValue,
					bool.Parse(stTxtParaInnerElement.Element("ParseIsCurrent").Attribute("val").Value),
					"Wrong value for ParseIsCurrent.");
			}

			return currentParaDto;
		}

		private static XElement CheckNewSegment(IDomainObjectDTORepository dtoRepos, int beginOffset, DomainObjectDTO owningPara, XElement newSegmentObjSurElement, out DomainObjectDTO newSegmentDto)
		{
			newSegmentDto = dtoRepos.GetDTO(newSegmentObjSurElement.Attribute("guid").Value);
			Assert.IsNotNull(newSegmentDto, "Missing new Segment DTO.");
			var newSegmentElement = XElement.Parse(newSegmentDto.Xml);
			// Make sure it is owned by the expected para.
			Assert.AreSame(owningPara, dtoRepos.GetOwningDTO(newSegmentDto), "Wrong paragraph owner.");
			var newSegmentInnerElement = newSegmentElement.Element("Segment");
			Assert.IsNotNull(newSegmentInnerElement, "Missing new inner Segment Element in the new Segment object.");
			Assert.AreEqual(beginOffset, int.Parse(newSegmentInnerElement.Element("BeginOffset").Attribute("val").Value));

			return newSegmentInnerElement;
		}
	}
}