using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FixData;
using System.Collections.Generic;
using Palaso.TestUtilities;

namespace FixFwDataTest
{
	class FwDataFixerTest
	{
		private List<string> errors = new List<string>();
		private void LogErrors(string guid, string date, string message)
		{
			errors.Add(message);
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
			FwDataFixer data = new FwDataFixer("../../TestData/DuplicateGuid/BasicFixup.fwdata", new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File("../../TestData/DuplicateGuid/BasicFixup.fwdata").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @guid=\"" + testGuid + "\"]", 2);
			Assert.True(errors.Count == 1 && errors[0].EndsWith("Object with guid '" + testGuid + "' already exists! (not fixed)"));//FwDataFixer.ksObjectWithGuidAlreadyExists));)
			AssertThatXmlIn.File("../../TestData/DuplicateGuid/BasicFixup.bak").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @guid=\"" + testGuid + "\"]", 2);
		}

		[Test]
		public void TestDanglingReferences()
		{			//This test checks that duplicate guids are identified and that an error message is produced for them.
			string testGuid = "cccccccc-a7d4-4e1e-a403-deec87c34455";
			FwDataFixer data = new FwDataFixer("../../TestData/DanglingReference/BasicFixup.fwdata", new DummyProgressDlg(), LogErrors);
			data.FixErrorsAndSave();
			AssertThatXmlIn.File("../../TestData/DanglingReference/BasicFixup.fwdata").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @ownerguid=\"" + testGuid + "\"]", 0);
			Assert.True(errors.Count == 1 && errors[0].EndsWith("Object with guid '" + testGuid + "' already exists! (not fixed)"));//FwDataFixer.ksObjectWithGuidAlreadyExists));)
			AssertThatXmlIn.File("../../TestData/DanglingReference/BasicFixup.bak").HasSpecifiedNumberOfMatchesForXpath("//rt[@class=\"LexSense\" and @ownerguid=\"" + testGuid + "\"]", 1);
		}

		[Test]
		public void TestDuplicateWritingSystems()
		{
			;
		}

	}
}
