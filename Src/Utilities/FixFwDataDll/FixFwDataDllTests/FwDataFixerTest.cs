using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FixData;
using System.Collections.Generic;
using Palaso.TestUtilities;
using SIL.FieldWorks.Test.TestUtils;

namespace FixFwDataDllTests
{
	class FwDataFixerTest : BaseTest
	{
		private const string WordformswithsameformTestDir = "WordformsWithSameForm";
		private List<string> errors = new List<string>();
		private void LogErrors(string guid, string date, string message)
		{
			errors.Add(message);
		}

		private static XmlDocument GetResult(string filePath)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(filePath);
			//Don't write this to the console. It's currently 209,000 lines that takes about 5 minutes to write.
			//Console.WriteLine(File.ReadAllText(filePath));
			return doc;
		}

		internal XmlNodeList VerifyEntryExists(XmlDocument xmlDoc, string xPath)
		{
			XmlNodeList selectedEntries = xmlDoc.SelectNodes(xPath);
			Assert.IsNotNull(selectedEntries);
			Assert.AreEqual(1, selectedEntries.Count, String.Format("An entry with the following criteria should exist:{0}", xPath));
			return selectedEntries;
		}

		private string basePath;

		private readonly string[] m_testFileDirectories =
			{
				"DuplicateGuid", "DanglingReference", "DuplicateWs", "SequenceFixer", "EntryWithExtraMSA",
				"TagAndCellRefs", "GenericDates", "HomographFixer", WordformswithsameformTestDir
			};

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			basePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(DirectoryFinder.FwSourceDirectory, "Utilities"), "FixFwDataDll"), "FixFwDataDllTests"), "TestData");
			foreach (var testDir in m_testFileDirectories)
				CopyTestData(testDir);
		}

		private void CopyTestData(string testFolder)
		{
			var testPath = Path.Combine(basePath, testFolder);
			File.Copy(Path.Combine(testPath, "Test.fwdata"), Path.Combine(testPath, "BasicFixup.fwdata"), true);
			File.SetAttributes(Path.Combine(testPath, "BasicFixup.fwdata"), FileAttributes.Normal);
		}

		[TestFixtureTearDown]
		public void AllTestTearDown()
		{
			foreach (var testDir in m_testFileDirectories)
				CleanupTestDir(testDir);
		}

		private void CleanupTestDir(string testDir)
		{
			var testPath = Path.Combine(basePath, testDir);
			File.Delete(Path.Combine(testPath, "BasicFixup.fwdata"));
			File.Delete(Path.Combine(testPath, "BasicFixup.bak"));
		}

		[SetUp]
		public void Setup()
		{
			errors.Clear();
		}

		[Test]
		public void DuplicateWordforms_AreMerged()
		{
			var testPath = Path.Combine(basePath, WordformswithsameformTestDir);

			var fixedDataPath = Path.Combine(testPath, "BasicFixup.fwdata");
			var data = new FwDataFixer(fixedDataPath, new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();

			// Test data has:
			// - Two wordforms with French form "dup". The second goes away, and its two analyses are moved to the first.
			// - a wordfom with a different French form ("other"). It is unaffected.
			// - a wordform with French form "dup", but also Spanish form "other". It is unaffected.
			// - two wordforms  with French form "dupFr" and Spanish form "dupSp". The alternatives are in the opposite order.
			//		Despite this they are merged, and the one Analysis of the second one is moved to the first, which
			//		previously had none.
			// - a segment which references one of the deleted wordforms, and is changed to reference the replacement.
			var firstdupGuid = "64cf9708-a7d4-4e1e-a403-deec87c34455"; // First wordform with simple form "dup"
			var secondDupGuid = "0964665E-BB56-4406-8310-ADE04A7A23C7"; // Second with simple form "dup"
			var analysis2_1Guid = "86DB9E97-2E6C-4AAC-B78B-9EDA834254E7"; // first analysis of deleted wordform
			var analysis2_2Guid = "BCD9971A-D871-472D-8843-9B5392AAA57F"; // second analysis of deleted wordform


			VerifyElementCount(fixedDataPath, "WfiWordform", firstdupGuid, 1); // First "dup" should survive
			VerifyElementCount(fixedDataPath, "WfiWordform", secondDupGuid, 0); // Second "dup" should go away
			VerifyElementCount(fixedDataPath, "WfiWordform", "31EBCDB7-8274-4776-A6E0-1AB523AA9E1E", 1); // Non-dup should survive
			VerifyElementCount(fixedDataPath, "WfiWordform", "D596FD07-A4E6-4ED1-A859-7601ACD2CD36", 1); // Partial duplicate should survive
			VerifyElementCount(fixedDataPath, "WfiAnalysis", analysis2_1Guid, 1); // First analysis of deleted wordform should survive
			VerifyElementCount(fixedDataPath, "WfiAnalysis", analysis2_2Guid, 1); // Second analysis of deleted wordform should survive

			// The surviving analyses should have their owners corrected. I think it's enough to check one.
			AssertThatXmlIn.File(fixedDataPath).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class='WfiAnalysis' and @guid='" + analysis2_2Guid + "' and @ownerguid='" + firstdupGuid + "']", 1, false);

			// The merged "dup" wordform should have four analyses
			AssertThatXmlIn.File(fixedDataPath).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class='WfiWordform' and @guid='" + firstdupGuid + "']/Analyses/objsur", 4, false);

			// One of which should be the last one from the deleted wordform.
			AssertThatXmlIn.File(fixedDataPath).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class='WfiWordform' and @guid='" + firstdupGuid + "']/Analyses/objsur[@guid='BCD9971A-D871-472D-8843-9B5392AAA57F']", 1, false);

			// The segment which refers to the deleted wordform should be fixed.
			AssertThatXmlIn.File(fixedDataPath).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class='Segment' and @guid='b405f3c0-58e1-4492-8a40-e955774a6911']/Analyses/objsur[@guid='" + firstdupGuid + "']", 1, false);

			// Merging these two verifies that it does the right things when (a) the second word of a set is the only one with analyses
			// and (b) the writing systems are in a different order
			var firstDupFrSp = "83A4D906-3ED1-49D1-AA5D-FC2DB938B6A4";
			VerifyElementCount(fixedDataPath, "WfiWordform", firstDupFrSp, 1); // first "dupFr/dupSp" should survive
			var secondDupSpFr = "D8444A90-A5CF-4163-B312-AFF577B0452E";
			VerifyElementCount(fixedDataPath, "WfiWordform", secondDupSpFr, 0); // second "dupFr/dupSp" should go away

			// The second merged wordform should have one analysis
			AssertThatXmlIn.File(fixedDataPath).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class='WfiWordform' and @guid='" + firstDupFrSp + "']/Analyses/objsur", 1, false);

			// The surviving analyses should have their owners corrected. I think it's enough to check one.
			AssertThatXmlIn.File(fixedDataPath).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class='WfiAnalysis' and @guid='AE2BA69A-42BA-4582-AFA4-B8AC3E5567C2' and @ownerguid='" + firstDupFrSp + "']", 1, false);


			Assert.AreEqual(2, errors.Count, "Unexpected number of errors found");

			Assert.That(errors[0], Is.EqualTo("Wordform with guid '" + secondDupGuid + "' has same form (fr>dup) as '" + firstdupGuid + "' and was merged"),
				"Error message is incorrect for dup.");
			Assert.That(errors[1], Is.EqualTo("Wordform with guid '" + secondDupSpFr + "' has same form (fr>dupFr&sp>dupSp) as '" + firstDupFrSp + "' and was merged"),
				"Error message is incorrect for dupSpFr.");

			// Check original errors. I think it's enough to verify that the two elements the merger was supposed to delete
			// were originally present. If the properties that allowed them to be merged were missing, it wouldn't happen.
			// If the components that get moved were not present, they would not show up in the fixed data.
			string backupPath = Path.Combine(testPath, "BasicFixup.bak"); // the original data we corrected
			VerifyElementCount(backupPath, "WfiWordform", secondDupGuid, 1); // Second "dup" was there originally.
			VerifyElementCount(backupPath, "WfiWordform", secondDupSpFr, 1); // second "dupFr/dupSp" was there originally.
		}

		/// <summary>
		/// Verify that the object with the specified class and guid occurs the expected number of times (0 or 1)
		/// </summary>
		/// <param name="testPath"></param>
		/// <param name="className"></param>
		/// <param name="guid"></param>
		/// <param name="expectedCount"></param>
		private static void VerifyElementCount(string testPath, string className, string guid, int expectedCount)
		{
			AssertThatXmlIn.File(testPath).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"" + className + "\" and @guid=\"" + guid + "\"]", expectedCount, false);
		}

		[Test]
		public void TestDuplicateGuids()
		{
			var testPath = Path.Combine(basePath, "DuplicateGuid");
			// This test checks that duplicate guids are identified and that an error message is produced for them.
			string testGuid = "2110cf83-ad6c-47fe-91f8-8bf789473792";
			var data = new FwDataFixer(Path.Combine(testPath, "BasicFixup.fwdata"), new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexSense\" and @guid=\"" + testGuid + "\"]", 2, false);
			Assert.AreEqual(1, errors.Count, "Unexpected number of errors found");
			Assert.True(errors[0].EndsWith("Object with guid '" + testGuid + "' already exists! (not fixed)"),
				"Error message is incorrect."); // OriginalFixer--ksObjectWithGuidAlreadyExists
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexSense\" and @guid=\"" + testGuid + "\"]", 2, false);
		}

		[Test]
		public void TestDanglingReferences()
		{
			var testPath = Path.Combine(basePath, "DanglingReference");
			// This test checks that dangling references guids are identified and removed
			// and that an error message is produced for them.
			string testGuid = "dddddddd-a7d4-4e1e-a403-deec87c34455";
			string testObjsurGuid = "aaaaaaaa-e15a-448e-a618-3855f93bd3c2";
			string lexSenseGuid = "2210cf83-ad6c-47fe-91f8-8bf789473792";
			string lexEntryGuid = "64cf9708-a7d4-4e1e-a403-deec87c34455";
			string testChangeGuid = "cccccccc-a7d4-4e1e-a403-deec87c34455";
			string moStemMsaGuid = "508ba7ca-e15a-448e-a618-3855f93bd3c2";
			var data = new FwDataFixer(Path.Combine(testPath, "BasicFixup.fwdata"), new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexSense\" and @ownerguid=\"" + testGuid + "\"]", 0, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + testObjsurGuid + "\"]", 0, false);
			Assert.AreEqual(3, errors.Count, "Unexpected number of errors found.");
			Assert.True(errors[0].StartsWith("Removing dangling link to '" + testObjsurGuid + "' (class='LexEntry'"),
				"Error message is incorrect."); // OriginalFixer--ksRemovingLinkToNonexistingObject

			Assert.True(errors[1].StartsWith("Changing ownerguid value from '" + testChangeGuid + "' to '" + lexEntryGuid
				+ "' (class='LexSense', guid='" + lexSenseGuid),
				"Error message is incorrect."); // OriginalFixer--ksRemovingLinkToNonexistingObject

			Assert.True(errors[2].EndsWith("Removing link to nonexistent ownerguid='" + testGuid
				+ "' (class='MoStemMsa', guid='" + moStemMsaGuid + "')."),
				"Error message is incorrect."); // OriginalFixer--ksRemovingLinkToNonexistentOwner

			// Check original errors
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"MoStemMsa\" and @ownerguid=\"" + testGuid + "\"]", 1, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexSense\" and @ownerguid=\"" + testChangeGuid + "\"]", 1, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + testObjsurGuid + "\"]", 1, false);
			// Check that they were fixed
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"MoStemMsa\" and @ownerguid=\"" + testGuid + "\"]", 0, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexSense\" and @ownerguid=\"" + lexEntryGuid + "\"]", 2, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + testObjsurGuid + "\"]", 0, false);
		}

		[Test]
		public void TestDuplicateWritingSystems()
		{
			var testPath = Path.Combine(basePath, "DuplicateWs");
			// Looks for duplicate AStr elements with the same writing system (english) and makes sure the Fixer fixes 'em up.
			const string testGuid = "00041516-72d1-4e56-9ed8-fe235a9b1a68";
			var data = new FwDataFixer(Path.Combine(testPath, "BasicFixup.fwdata"), new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"CmSemanticDomain\" and @guid=\"" + testGuid + "\"]//Description/AStr[@ws=\"en\"]", 1, false);
			Assert.AreEqual(1, errors.Count, "Incorrect number of errors.");
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"CmSemanticDomain\" and @guid=\"" + testGuid + "\"]//Description/AStr[@ws=\"en\"]", 2, false);
		}

		/// <summary>
		/// This test checks that sequences that should not be empty are identified and their parents removed
		/// and that an error message is produced for them.
		/// </summary>
		[Test]
		public void TestForEmptySequences()
		{
			// Setup
			var testPath = Path.Combine(basePath, "SequenceFixer");
			// This rt element is a clause marker that has no dependent clauses
			// and so should be deleted from its chart.
			const string clauseMarkerGuid = "c4e487c6-bbbe-4b8f-8137-7d5fa7d2dc09";
			// This rt element will have no component cells after the above clause marker is deleted
			// and so it also should be deleted from its chart.
			const string chartRowGuid = "6d9fe079-df9c-40c6-9cec-8e1dc1bbda92";
			// This is the row's chart (owner).
			const string chartGuid = "8fa53cdf-9950-4a23-ba1c-844723c2342d";
			// This rt element holds a sequence of phonetic contexts that is empty
			// and so should be deleted from its rule.
			const string sequenceContextGuid = "09acafc4-33fd-4c12-a96d-af0d87c343d0";
			// This is the sequence context's owner.
			const string segmentRuleRhsGuid = "bd72b1c5-3067-433d-980d-5aae9271556d";
			Assert.DoesNotThrow(() =>
									{
										var data = new FwDataFixer(Path.Combine(testPath, "BasicFixup.fwdata"), new DummyProgressDlg(),
																   LogErrors);

										// SUT
										data.FixErrorsAndSave();
									}, "Exception running the data fixer on the sequence test data.");

			// Verification
			// check that the clause marker was there originally
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartClauseMarker\" and @guid=\"" + clauseMarkerGuid + "\"]", 1, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + chartRowGuid + "\"]", 1, false);

			// check that the clause marker has been deleted
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartClauseMarker\" and @guid=\"" + clauseMarkerGuid + "\"]", 0, false);

			// check that the row is no longer in the chart
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + chartRowGuid + "\"]", 0, false);

			// check that the row has been deleted
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartRow\" and @guid=\"" + chartRowGuid + "\"]", 0, false);

			// check that the phone rule sequence context was there originally
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"PhSequenceContext\" and @guid=\"" + sequenceContextGuid + "\"]", 1, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + segmentRuleRhsGuid + "\"]", 1, false);

			// check that the phone rule sequence context has been deleted
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"PhSequenceContext\" and @guid=\"" + sequenceContextGuid + "\"]", 0, false);

			Assert.AreEqual(3, errors.Count, "Unexpected number of errors found.");
			Assert.AreEqual("Removing owner of empty sequence (guid='" + chartRowGuid +
				"' class='ConstChartRow') from its owner (guid='" + chartGuid + "').", errors[0],
				"Error message is incorrect.");//SequenceFixer--ksRemovingOwnerOfEmptySequence
			Assert.AreEqual("Removing owner of empty sequence (guid='" + clauseMarkerGuid +
				"' class='ConstChartClauseMarker') from its owner (guid='" + chartRowGuid + "').", errors[1],
				"Error message is incorrect.");//SequenceFixer--ksRemovingOwnerOfEmptySequence
			Assert.AreEqual("Removing owner of empty sequence (guid='" + sequenceContextGuid +
				"' class='PhSequenceContext') from its owner (guid='" + segmentRuleRhsGuid + "').", errors[2],
				"Error message is incorrect.");//SequenceFixer--ksRemovingOwnerOfEmptySequence
		}

		[Test]
		public void TestEntryWithExtraMSA()
		{
			var testPath = Path.Combine(basePath, "EntryWithExtraMSA");
			Assert.DoesNotThrow(() =>
			{
				var data = new FwDataFixer(Path.Combine(testPath, "BasicFixup.fwdata"), new DummyProgressDlg(),
										   LogErrors);

				// SUT
				data.FixErrorsAndSave();
			}, "Exception running the data fixer on the entry with extra MSA test data.");
			// check that the clause marker was there originally
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexEntry\"]/MorphoSyntaxAnalyses/objsur", 2, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexEntry\"]/MorphoSyntaxAnalyses/objsur", 1, false);
		}

		[Test]
		public void TestDanglingTextTagAndChartReferences()
		{
			var testPath = Path.Combine(basePath, "TagAndCellRefs");
			// This test checks that dangling reference guids are identified and removed
			// and that an error message is produced for them.
			// It also checks that TextTags and ChartCells with missing references have been cleaned up.
			const string segmentGuid = "0157b3fd-b464-4983-a865-3eb9dbc7fa72"; // this Segment was deleted by the merge.
			// This ConstChartWordGroup references the Segment that went away.
			// Both BeginSegment and EndSegment are null (after Dangling Reference repair).
			// Delete the word group.
			const string chartCellGuid = "f864b36d-ecf0-4c22-9fac-ff91b009a8f8";
			// This TextTag references the Segment that went away.
			// Its BeginSegment is still okay, but its EndSegment is bad. Dangling Reference repair will
			// delete the reference. Repair the tag.
			// At this point, the UI can't make a tag that references more than one Segment, but it may someday.
			const string textTagGuid = "fa0c3376-1dbc-42c0-b4ff-cd6bf0372b13";
			const string chartRowGuid = "d2e52268-71bc-427e-a666-dbe66751b132";
			const string chartGuid = "8fa53cdf-9950-4a23-ba1c-844723c2342d";
			var data = new FwDataFixer(Path.Combine(testPath, "BasicFixup.fwdata"), new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			// Check initial state of the test file
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"TextTag\"]", 1, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartWordGroup\"]", 3, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + segmentGuid + "\"]", 3, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.bak")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartRow\" and @guid=\"" + chartRowGuid + "\"]", 1, false);
			// Check the repaired state of the test file
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartRow\" and @guid=\"" + chartRowGuid + "\"]", 0, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"TextTag\"]", 1, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartWordGroup\"]", 2, false);
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//objsur[@guid=\"" + segmentGuid + "\"]", 0, false);
			// check that the row has been deleted
			AssertThatXmlIn.File(Path.Combine(testPath, "BasicFixup.fwdata")).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"ConstChartRow\" and @guid=\"" + chartRowGuid + "\"]", 0, false);
			Assert.AreEqual(6, errors.Count, "Unexpected number of errors found.");
			Assert.True(errors[0].StartsWith("Removing dangling link to '" + segmentGuid + "' (class='ConstChartWordGroup'"),
				"Error message is incorrect."); // OriginalFixer--ksRemovingLinkToNonexistingObject
			Assert.True(errors[1].StartsWith("Removing dangling link to '" + segmentGuid + "' (class='ConstChartWordGroup'"),
				"Error message is incorrect."); // OriginalFixer--ksRemovingLinkToNonexistingObject
			Assert.True(errors[2].StartsWith("Removing reference to missing Segment by deleting analysis object guid='" +
				chartCellGuid + "', class='ConstChartWordGroup'"),
				"Error message is incorrect."); // SequenceFixer--ksRemovingBadAnalysisRefObj
			Assert.True(errors[3].StartsWith("Removing dangling link to '" + segmentGuid + "' (class='TextTag'"),
				"Error message is incorrect."); // OriginalFixer--ksRemovingLinkToNonexistingObject
			Assert.True(errors[4].EndsWith("changing analysis object guid='" + textTagGuid +
				"', class='TextTag', field='EndSegment'."),
				"Error message is incorrect."); // SequenceFixer--ksAdjustingAnalysisRefObj
			Assert.AreEqual("Removing owner of empty sequence (guid='" + chartRowGuid +
				"' class='ConstChartRow') from its owner (guid='" + chartGuid + "').", errors[5],
				"Error message is incorrect.");//SequenceFixer--ksRemovingOwnerOfEmptySequence
		}

		[Test]
		public void TestGenericDateFixup()
		{
			var fileLoc = Path.Combine(Path.Combine(basePath, "GenericDates"), "BasicFixup.fwdata");
			var data = new FwDataFixer(fileLoc, new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();

			AssertThatXmlIn.File(fileLoc).HasSpecifiedNumberOfMatchesForXpath("//rt[@class='RnGenericRec']/DateOfEvent", 3);
			AssertThatXmlIn.File(fileLoc).HasAtLeastOneMatchForXpath("//rt[@class='RnGenericRec']/DateOfEvent[@val='0']");
		}

		/// <summary>
		/// LT-13509 Identical entries homograph numbering inconsistency.
		/// </summary>
		[Test]
		public void TestForHomographNumberInconsistency()
		{
			// Setup
			var testPath = Path.Combine(basePath, "HomographFixer");
			// LexEntries needing homograph number set to 1 or 2
			const string lexEntry_dinding1Guid = "a39f2112-b82c-46ba-9f69-6b46e45efff4";
			const string lexEntry_dinding2Guid = "b35e8d52-e74d-47b4-b300-82e8c45cdfb7";

			Assert.DoesNotThrow(() =>
			{
				var data = new FwDataFixer(Path.Combine(testPath, "BasicFixup.fwdata"), new DummyProgressDlg(),
										   LogErrors);

				// SUT
				data.FixErrorsAndSave();
			}, "Exception running the data fixer on the sequence test data.");

			// Verification
			// check the LexEntries are there.
			var testFile = Path.Combine(testPath, "BasicFixup.fwdata");
			AssertThatXmlIn.File(testFile).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexEntry\" and @guid=\"" + lexEntry_dinding1Guid + "\"]", 1, false);
			AssertThatXmlIn.File(testFile).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class=\"LexEntry\" and @guid=\"" + lexEntry_dinding2Guid + "\"]", 1, false);

			AssertThatXmlIn.File(testFile).HasSpecifiedNumberOfMatchesForXpath(
				"//rt[@class='LexEntry' and @guid='" + lexEntry_dinding1Guid + "']", 1, false);

			var xmlDoc = GetResult(testFile);
			var entries = VerifyEntryExists(xmlDoc, "//rt[@class='LexEntry' and @guid='" + lexEntry_dinding1Guid + "']");
			XmlNode entry = entries[0];
			var homographEl = entry.SelectSingleNode("HomographNumber");
			Assert.IsNotNull(homographEl);
			var homographAttribute = homographEl.Attributes[0];
			Assert.IsTrue(homographAttribute.Name.ToString().Equals("val"));
			var homographVal1 = homographAttribute.Value;

			entries = VerifyEntryExists(xmlDoc, "//rt[@class='LexEntry' and @guid='" + lexEntry_dinding2Guid + "']");
			entry = entries[0];
			homographEl = entry.SelectSingleNode("HomographNumber");
			Assert.IsNotNull(homographEl);
			homographAttribute = homographEl.Attributes[0];
			Assert.IsTrue(homographAttribute.Name.ToString().Equals("val"));

			var homographVal2 = homographAttribute.Value;

			Assert.That((homographVal1 == "1" && homographVal2 == "2") || (homographVal1 == "2" && homographVal2 == "1"),"The homograph numbers were both zero for these LexEntries and should now be 1 and 2");
		}
	}
}
