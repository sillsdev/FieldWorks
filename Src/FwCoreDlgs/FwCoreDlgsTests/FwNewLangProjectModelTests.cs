// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TestFwNewLangProject.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwNewLangProjectModelTests
	{
		/// <summary>
		/// Make sure a new DB gets created
		/// </summary>
		[Test]
		public void FwNewLangProjectModel_VerifyCreateNewLangProject()
		{
			const string dbName = "Maileingwij2025";

			LcmCache cache = null;
			if (DbExists(dbName))
				DestroyDb(dbName, true);

			var testProject = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				LoadAnalysisSetup = () => { },
				ProjectName = dbName,
				AnthroModel = new FwChooseAnthroListModel { CurrentList = FwChooseAnthroListModel.ListChoice.UserDef }
			};
			try
			{
				testProject.ProjectName = dbName;
				testProject.Next();
				testProject.SetDefaultWs(new LanguageInfo { LanguageTag = "fr" });
				testProject.Next();
				using (var threadHelper = new ThreadHelper())
				{
					testProject.CreateNewLangProj(new DummyProgressDlg(), threadHelper);
				}

				Assert.IsTrue(DbExists(dbName));

				// despite of the name is DummyProgressDlg no real dialog (doesn't derive from Control), so
				// we don't need a 'using'
				cache = LcmCache.CreateCacheFromExistingData(
					new TestProjectId(BackendProviderType.kXMLWithMemoryOnlyWsMgr, DbFilename(dbName)), "en", new DummyLcmUI(),
					FwDirectoryFinder.LcmDirectories, new LcmSettings(), new DummyProgressDlg());
				CheckInitialSetOfPartsOfSpeech(cache);

				Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Count);
				Assert.AreEqual("English", cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.First().LanguageName);
				Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Count);
				Assert.AreEqual("English", cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.LanguageName);
				Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count);
				Assert.AreEqual("French", cache.ServiceLocator.WritingSystems.VernacularWritingSystems.First().LanguageName);
				Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Count);
				Assert.AreEqual("French", cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.LanguageName);
			}
			finally
			{
				// Blow away the database to clean things up
				if (cache != null)
					cache.Dispose();
				DestroyDb(dbName, false);
			}
		}

		/// <summary/>
		[TestCase("abcdefghijklmnopqrstuvwxyz_-_ABCDEFGHIJKLMNOPQRSTUVWXYZ", true)]
		[TestCase(null, false)]
		[TestCase("", false)]
		[TestCase(":", false)]
		[TestCase("\\", false)]
		[TestCase("/", false)]
		[TestCase("<", false)]
		[TestCase(">", false)]
		[TestCase("\"", false)]
		[TestCase("|", false)]
		[TestCase("?", false)]
		[TestCase("*", false)]
		// [TestCase("franç", false)] Enable when Non-Ascii filtering is enabled
		public void FwNewLangProjectModel_ProjectNameIsValid(string projName, bool expectedResult)
		{
			var testModel = new FwNewLangProjectModel();
			testModel.LoadProjectNameSetup = () => { };
			testModel.ProjectName = projName;
			Assert.That(testModel.IsProjectNameValid, Is.EqualTo(expectedResult));
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanFinish_TrueIfAllComplete()
		{
			var testModel = new FwNewLangProjectModel();
			foreach (var step in testModel.Steps)
			{
				step.IsComplete = true;
			}
			Assert.True(testModel.CanFinish());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanFinish_FalseIfNoneComplete()
		{
			var testModel = new FwNewLangProjectModel();
			foreach (var step in testModel.Steps)
			{
				step.IsComplete = false;
			}
			Assert.False(testModel.CanFinish());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanFinish_TrueIfAllNonOptionalComplete()
		{
			var testModel = new FwNewLangProjectModel();
			Assert.True(testModel.Steps.Any(step => step.IsOptional), "Test data is invalid, no optional steps present");
			foreach (var step in testModel.Steps)
			{
				if (!step.IsOptional)
				{
					step.IsComplete = true;
				}
				else
				{
					step.IsComplete = false;
				}
			}
			Assert.True(testModel.CanFinish());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanGoBack()
		{
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.ProjectName = "a";
			Assert.False(model.CanGoBack());
			model.Next();
			Assert.True(model.CanGoBack());
			model.Back();
			Assert.False(model.CanGoBack());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_VernacularAndAnalysisSame_WarningIssued()
		{
			bool warningIssued = false;
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.LoadAnalysisSetup = () => { };
			model.LoadAnalysisSameAsVernacularWarning = () => { warningIssued = true; };
			model.ProjectName = "a";
			var fakeTestWs = new CoreWritingSystemDefinition("fr");
			model.WritingSystemContainer.CurrentVernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.VernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.CurrentAnalysisWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.AnalysisWritingSystems.Add(fakeTestWs);
			Assert.True(model.CanGoNext());
			model.Next(); // Move to choose default vernacular
			Assert.True(model.CanGoNext());
			model.Next(); // Move to choose default analysis
			model.SetDefaultWs(new LanguageInfo() {LanguageTag = "fr" });
			Assert.True(warningIssued, "Warning for analysis same as vernacular not triggered");
			Assert.True(model.CanGoNext()); // The user can ignore the warning
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanGoNext()
		{
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.LoadAnalysisSetup = () => { };
			model.LoadAdvancedWsSetup = () => { };
			model.LoadAnthropologySetup = () => { };
			model.ProjectName = "a";
			var fakeTestWs = new CoreWritingSystemDefinition("fr");
			model.WritingSystemContainer.CurrentVernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.VernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.CurrentAnalysisWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.AnalysisWritingSystems.Add(fakeTestWs);
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.False(model.CanGoNext());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CannotClickFinishWithBlankProjectName()
		{
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.LoadAnalysisSetup = () => { };
			model.LoadAdvancedWsSetup = () => { };
			model.LoadAnthropologySetup = () => { };
			model.ProjectName = "a";
			var fakeTestWs = new CoreWritingSystemDefinition("fr");
			model.WritingSystemContainer.CurrentVernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.VernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.CurrentAnalysisWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.AnalysisWritingSystems.Add(fakeTestWs);
			Assert.True(model.CanGoNext());
			model.Next(); // Vernacular
			model.Next(); // Analysis
			Assert.True(model.CanFinish());
			model.Back();
			model.Back();
			model.ProjectName = "";
			Assert.False(model.CanFinish());
		}

		private void CheckInitialSetOfPartsOfSpeech(LcmCache cache)
		{
			ILangProject lp = cache.LanguageProject;
			int iCount = 0;
			bool fAdverbFound = false;
			bool fNounFound = false;
			bool fProformFound = false;
			bool fVerbFound = false;
			foreach (IPartOfSpeech pos in lp.PartsOfSpeechOA.PossibilitiesOS)
			{
				iCount++;
				switch (pos.CatalogSourceId)
				{
					case "Adverb":
						fAdverbFound = true;
						break;
					case "Noun":
						fNounFound = true;
						break;
					case "Pro-form":
						fProformFound = true;
						break;
					case "Verb":
						fVerbFound = true;
						break;
					default:
						string sFmt = "Unexpected CatalogSourceId ({0}) found in PartOfSpeech {1}.";
						string sMsg = String.Format(sFmt, pos.CatalogSourceId, pos.Name.AnalysisDefaultWritingSystem);
						Assert.Fail(sMsg);
						break;
				}
			}
			Assert.AreEqual(4, iCount, "Expect four initial POSes.");
			Assert.IsTrue(fAdverbFound, "Did not find Adverb CatalogSourceId");
			Assert.IsTrue(fNounFound, "Did not find Noun CatalogSourceId");
			Assert.IsTrue(fProformFound, "Did not find Pro-form CatalogSourceId");
			Assert.IsTrue(fVerbFound, "Did not find Verb CatalogSourceId");
		}

		private static string DbDirectory(string dbName)
		{
			return Path.Combine(FwDirectoryFinder.ProjectsDirectory, dbName);
		}

		private static string DbFilename(string dbName)
		{
			return Path.Combine(DbDirectory(dbName), LcmFileHelper.GetXmlDataFileName(dbName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the given FW database exists by attempting to establish a connection to it.
		/// </summary>
		/// <param name="dbName">Name of the FW database to look for</param>
		/// <returns>true iff the underlying file exists.</returns>
		/// ------------------------------------------------------------------------------------
		private static bool DbExists(string dbName)
		{
			string sFile = DbFilename(dbName);
			return File.Exists(sFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Blow away the given FW database.
		/// </summary>
		/// <param name="dbName">Name of the FW database to smoke</param>
		/// <param name="failureIsFatal">If true, then failure to delete will fail tests.</param>
		/// ------------------------------------------------------------------------------------
		internal static void DestroyDb(string dbName, bool failureIsFatal)
		{
			try
			{
				string dir = DbDirectory(dbName);
				Directory.Delete(dir, true);
			}
			catch
			{
				string msg = "The test database " + dbName + " could not be deleted.";
				if (failureIsFatal)
					Assert.Fail(msg);
				else
					Debug.WriteLine(msg);
			}
		}
	}
}
