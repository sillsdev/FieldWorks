using System;
using System.IO;
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
		private List<string> errors = new List<string>();
		private void LogErrors(string guid, string date, string message)
		{
			errors.Add(message);
		}

		private string basePath;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			basePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(DirectoryFinder.FwSourceDirectory, "Utilities"), "FixFwDataDll"), "FixFwDataDllTests"), "TestData/");
			File.Copy(basePath + "DuplicateGuid/Test.fwdata", basePath + "DuplicateGuid/BasicFixup.fwdata", true);
			File.SetAttributes(basePath + "DuplicateGuid/BasicFixup.fwdata", FileAttributes.Normal);

			File.Copy(basePath + "DanglingReference/Test.fwdata", basePath + "DanglingReference/BasicFixup.fwdata", true);
			File.SetAttributes(basePath + "DanglingReference/BasicFixup.fwdata", FileAttributes.Normal);

			File.Copy(basePath + "DuplicateWs/Test.fwdata", basePath + "DuplicateWs/BasicFixup.fwdata", true);
			File.SetAttributes(basePath + "DuplicateWs/BasicFixup.fwdata", FileAttributes.Normal);
		}

		[TestFixtureTearDown]
		public void AllTestTearDown()
		{

			File.Delete(basePath + "DuplicateGuid/BasicFixup.fwdata");
			File.Delete(basePath + "DuplicateGuid/BasicFixup.bak");

			File.Delete(basePath + "DanglingReference/BasicFixup.fwdata");
			File.Delete(basePath + "DanglingReference/BasicFixup.bak");
		}

		[SetUp]
		public void Setup()
		{
			errors.Clear();
		}

		[Test]
		public void TestDuplicateGuids()
		{
			//This test checks that duplicate guids are identified and that an error message is produced for them.
			string testGuid = "2110cf83-ad6c-47fe-91f8-8bf789473792";
			FwDataFixer data = new FwDataFixer(basePath + "DuplicateGuid/BasicFixup.fwdata", new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File(basePath + "DuplicateGuid/BasicFixup.fwdata").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @guid=\"" + testGuid + "\"]", 2, false);
			Assert.AreEqual(errors.Count, 1, "Unexpected number of errors found");
			Assert.True(errors[0].EndsWith("Object with guid '" + testGuid + "' already exists! (not fixed)"), "Error message is incorrect.");//FwDataFixer.ksObjectWithGuidAlreadyExists
			AssertThatXmlIn.File(basePath + "DuplicateGuid/BasicFixup.bak").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @guid=\"" + testGuid + "\"]", 2, false);
		}

		[Test]
		public void TestDanglingReferences()
		{	//This test checks that dangling references guids are identified and removed
			// and that an error message is produced for them.
			string testGuid = "cccccccc-a7d4-4e1e-a403-deec87c34455";
			string testObjsurGuid = "aaaaaaaa-e15a-448e-a618-3855f93bd3c2";
			FwDataFixer data = new FwDataFixer(basePath + "DanglingReference/BasicFixup.fwdata", new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File(basePath + "DanglingReference/BasicFixup.fwdata").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @ownerguid=\"" + testGuid + "\"]", 0, false);
			AssertThatXmlIn.File(basePath + "DanglingReference/BasicFixup.fwdata").HasSpecifiedNumberOfMatchesForXpath("//objsur[@guid=\"" + testObjsurGuid + "\"]", 0, false);
			Assert.AreEqual(errors.Count, 2, "Unexpected number of errors found.");
			Assert.True(errors[0].EndsWith("Removing link to nonexistent ownerguid='" + testGuid + "' (class='LexSense', guid='2210cf83-ad6c-47fe-91f8-8bf789473792')."), "Error message is incorrect.");//FwDataFixer.ksObjectWithGuidAlreadyExists
			Assert.True(errors[1].StartsWith("Removing dangling link to '" + testObjsurGuid + "' (class='LexEntry'"));
			AssertThatXmlIn.File(basePath + "DanglingReference/BasicFixup.bak").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @ownerguid=\"" + testGuid + "\"]", 1, false);
			AssertThatXmlIn.File(basePath + "DanglingReference/BasicFixup.bak").HasSpecifiedNumberOfMatchesForXpath("//objsur[@guid=\"" + testObjsurGuid + "\"]", 1, false);
		}

		[Test]
		public void TestDuplicateWritingSystems()
		{
			//Looks for duplicate AStr elements with the same writing system (english) and makes sure the Fixer fixes 'em up.
			string testGuid = "00041516-72d1-4e56-9ed8-fe235a9b1a68";
			FwDataFixer data = new FwDataFixer(basePath + "DuplicateWs/BasicFixup.fwdata", new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File(basePath + "DuplicateWs/BasicFixup.fwdata").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"CmSemanticDomain\" and @guid=\"" + testGuid + "\"]//Description/AStr[@ws=\"en\"]", 1, false);
			Assert.AreEqual(errors.Count, 1, "Incorrect number of errors.");
			AssertThatXmlIn.File(basePath + "DuplicateWs/BasicFixup.bak").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"CmSemanticDomain\" and @guid=\"" + testGuid + "\"]//Description/AStr[@ws=\"en\"]", 2, false);
		}
	}
}
